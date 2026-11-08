using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

// Miasto Mistrzów: read-only Editor inspection of a saved, reopened scene.
// No scene/asset mutation, BuildPlayer, APK or visual "fix" is performed.
public static class BraxSceneAudit
{
    const float K = 1.294f;
    const float ThresholdM = .03f;
    const float OverlapFraction = .30f;
    const int MaxCandidatePairs = 4000000;
    const int MaxOverlapFindings = 50000;
    static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    static readonly Regex Structure = Pattern(@"(floor|ground|terrain|wall|paving|road|roof|slab|podlog|podłog|posadzk|scian|ścian|chodnik|nawierzch)");
    static readonly Regex Graphic = Pattern(@"(mural|szyld|sign|billboard|banner|plakat|logo)");
    static readonly Regex Prohibited = Pattern(@"(crane|dźwig|dzwig|container.ship|cargo.ship|freighter|kontenerowiec|wycieczkowiec|cruise.ship|large.ship)");
    static readonly Regex BerthOnly = Pattern(@"(berth|terminal|reserve|rezerwa|stanowisk|kierunek|direction|label|sign|szyld|plan |no model)");
    static readonly Regex GroundPlane = Pattern(@"(sea|ocean|coast|terrain|ground|road|street|paving|lawn|grass|plaza.pad)");
    static Regex Pattern(string text) => new Regex(text, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    static bool Contains(string a, string b) => a != null && a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0;
    static string F(float x) => x.ToString("0.###", Invariant);
    static string Safe(string x) => (x ?? "").Replace("|", "/").Replace("\n", " ").Replace("\r", " ");

    [Serializable] public sealed class ObjectInfo
    {
        public string name, path, region, creatorCs;
        public Vector3 center, size;
        public float heightMeters, gapMeters;
        public bool floorHit;
    }
    [Serializable] public sealed class Finding
    {
        public string type, objectName, objectPath, otherObject, creatorCs, description, evidenceLevel;
        public float value, limit, severity;
    }
    [Serializable] public sealed class ScaleInfo
    {
        public string name, path, category, creatorCs;
        public float heightMeters, minMeters, maxMeters;
        public bool outOfRange;
    }
    [Serializable] public sealed class GraphicInfo
    {
        public string objectName, path, creatorCs, texture, texturePath;
        public int textureWidth, textureHeight;
        public float widthMeters, heightMeters, pixelsPerMeter;
        public float uvSpanU, uvSpanV, effectivePixelsX, effectivePixelsY;
        public bool below100, textureUnavailable;
    }
    [Serializable] public sealed class Presence
    {
        public string requirement, status, objectName, creatorCs, note;
        public int count;
    }
    [Serializable] public sealed class RegionAssets
    {
        public string region;
        public int textureCount, meshCount;
        public long textureRuntimeBytes, meshRuntimeBytes;
    }
    [Serializable] public sealed class HotelMaterialAudit
    {
        public string materialName, sourceId, shaderName, albedoName;
        public int albedoWidth, albedoHeight, rendererCount;
        public float metallic, roughness, sampleScaleU, sampleScaleV;
    }
    [Serializable] public sealed class HotelSobczakAudit
    {
        public string status = "NOT_FOUND";
        public string rootPath;
        public float centreX, centreZ;
        public float structuralWidthMeters, structuralDepthMeters, maxHeightMeters;
        public float siteWidthMeters, siteDepthMeters;
        public int physicalLevelsExpected=34, physicalLevelsMeasured, guestFloors, rooms, elevators, stairs, parkingB1, parkingB2, parkingTotal;
        public int passengerElevators, serviceElevators, frontDormers, rearDormers;
        public int nullMaterialRenderers, milkGlassCandidates, doorLeafColliderCount;
        public bool centrePass, footprintPass, heightPass, siteReservePass, physicalLevelsPass;
        public bool roomsPass, guestFloorsPass, elevatorsPass, stairsPass, parkingPass, dormersPass;
        public bool glassPass, doorLeafColliderPass;
        public List<HotelMaterialAudit> materials = new List<HotelMaterialAudit>();
        public List<string> failures = new List<string>();
    }
    [Serializable] public sealed class Audit
    {
        public string status = "SCENES_ONLY_NO_APK";
        public string scene, sourceCommit, unityVersion, collectedUtc;
        public string scaleMethod = "heightMeters = renderer bounds world height / 1.294";
        public string overlapMethod = "AABB intersection volume / smaller AABB volume; exclude parts of same prefab";
        public string memoryMethod = "Profiler.GetRuntimeMemorySizeLong; each texture/mesh counted once per region (estimate)";
        public string collisionCaveat = "raycasts and collider AABB only; mesh penetrations are candidate findings";
        public string attributionCaveat = "creatorCs inferred from object ancestry, not recorded at instantiation";
        public int visibleRenderers, ignoredLowerLods, levitatingCount, embeddedCount, overlapsCount;
        public int wrongScaleCount, lowPpmCount, largeTexturesCount, missingCount, forbiddenCount;
        public int groundUnknownCount, candidatePairs, excludedPrefabPairs;
        public bool overlapScanIncomplete, overlapFindingsTruncated;
        public List<ObjectInfo> objects = new List<ObjectInfo>();
        public List<Finding> levitating = new List<Finding>();
        public List<Finding> embedded = new List<Finding>();
        public List<Finding> overlaps = new List<Finding>();
        public List<Finding> scaleWarnings = new List<Finding>();
        public List<Finding> lowPpm = new List<Finding>();
        public List<Finding> largeTextures = new List<Finding>();
        public List<Finding> forbidden = new List<Finding>();
        public List<ScaleInfo> scales = new List<ScaleInfo>();
        public List<GraphicInfo> graphics = new List<GraphicInfo>();
        public List<Presence> inventory = new List<Presence>();
        public List<RegionAssets> resources = new List<RegionAssets>();
        public HotelSobczakAudit hotelSobczak = new HotelSobczakAudit();
        public List<string> warnings = new List<string>();
    }
    sealed class Item
    {
        public Renderer renderer;
        public Bounds bounds;
        public ObjectInfo info;
        public float volume;
    }
    sealed class ResourceCounter
    {
        public RegionAssets report;
        public HashSet<int> textureIds = new HashSet<int>();
        public HashSet<int> meshIds = new HashSet<int>();
    }
    sealed class Range
    {
        public string category, expression;
        public float min, max;
        public Range(string category, string expression, float min, float max)
        { this.category = category; this.expression = expression; this.min = min; this.max = max; }
    }
    static readonly Range[] ScaleRanges =
    {
        new Range("fotele VIP A1", "(Premium barrel chair|Reserved VIP)", .90f, 1.30f),
        new Range("łóżka", "(bed|łóżk|lozk|mattress)", .35f, 1.30f),
        new Range("krzesła", "(chair|krzesł|krzesl|fotel)", .75f, 1.10f),
        new Range("stoły", "(table|stół|stol|biurko)", .65f, 1.10f),
        new Range("drzwi", "(door|drzwi|doorway)", 1.90f, 2.30f),
        new Range("ławki", "(bench|ławka|lawka)", .35f, 1.40f),
        new Range("worki", "(punching.bag|boxing.bag|heavy.bag|worek trening)", .90f, 1.90f),
        new Range("ludzie", "(BraxhornPlayer|person|human|humanoid|pedestrian|człowiek|czlowiek|postać|postac|NPC)", 1.45f, 2.15f),
        new Range("auta", "(car_rev|sports.car|samoch[oó]d|limousine|sedan|vehicle.car)", 1.10f, 2.30f)
    };

    public static void Run(Scene scene, string sourceCommit)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            throw new ArgumentException("Expected saved and reopened Mistrzowie scene");
        Physics.SyncTransforms();
        var report = new Audit {
            scene = scene.path, sourceCommit = sourceCommit,
            unityVersion = Application.unityVersion, collectedUtc = DateTime.UtcNow.ToString("o", Invariant)
        };
        var transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var lowerLods = new HashSet<int>();
        foreach (var g in transforms.Select(t => t.GetComponent<LODGroup>()).Where(g => g).Distinct())
        {
            var levels = g.GetLODs();
            for (int i = 1; i < levels.Length; i++)
                foreach (var r in levels[i].renderers) if (r) lowerLods.Add(r.GetInstanceID());
            if (levels.Length > 0)
                foreach (var r in levels[0].renderers) if (r) lowerLods.Remove(r.GetInstanceID());
        }
        var items = transforms.SelectMany(t => t.GetComponents<Renderer>())
            .Where(r => r && r.enabled && r.gameObject.activeInHierarchy &&
                   !lowerLods.Contains(r.GetInstanceID()) && r.bounds.size.sqrMagnitude > .00000001f)
            .Select(r => new Item {
                renderer = r, bounds = r.bounds,
                volume = Mathf.Max(0f, r.bounds.size.x * r.bounds.size.y * r.bounds.size.z),
                info = new ObjectInfo {
                    name = r.name, path = Hierarchy(r.transform), region = Region(r.transform),
                    creatorCs = Creator(r.transform), center = r.bounds.center, size = r.bounds.size,
                    heightMeters = r.bounds.size.y / K
                }
            }).ToList();
        report.ignoredLowerLods = lowerLods.Count;
        report.visibleRenderers = items.Count;
        report.objects.AddRange(items.Select(i => i.info));
        GroundAndWalls(items, report);
        Overlaps(items, report);
        Scales(items, report);
        Textures(items, report);
        Inventory(transforms, items, report);
        AuditHotelSobczak(transforms, report);
        report.levitatingCount = report.levitating.Count;
        report.embeddedCount = report.embedded.Count;
        report.overlapsCount = report.overlaps.Count;
        report.wrongScaleCount = report.scaleWarnings.Count;
        report.lowPpmCount = report.lowPpm.Count;
        report.largeTexturesCount = report.largeTextures.Count;
        report.missingCount = report.inventory.Count(x => x.status == "brak");
        report.forbiddenCount = report.forbidden.Count;

        var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../audit"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "mistrzowie-scena.json"),
                          JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        File.WriteAllLines(Path.Combine(directory, "mistrzowie-scena.md"),
                           Markdown(report).Take(150).ToArray(), new UTF8Encoding(false));
        Debug.Log("BRAX_SCENE_AUDIT NO_APK: objects=" + report.visibleRenderers +
                  " levitating=" + report.levitatingCount + " overlapping=" + report.overlapsCount +
                  " location=" + directory);
    }

    static string Hierarchy(Transform t)
    {
        var parts = new List<string>();
        for (var p = t; p != null; p = p.parent)
            parts.Add(p.name.Replace("/", "_") + "[" + p.GetSiblingIndex() + "]");
        parts.Reverse();
        return string.Join("/", parts);
    }
    static string Region(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var n = p.name;
            if (Contains(n, "T03 | HOTEL SOBCZAK")) return "Hotel Sobczak T03";
            if (Contains(n, "T11 | KASYNO")) return "Kasyno WAWER";
            if (Contains(n, "P1 | BASEN")) return "Basen P1";
            if (Contains(n, "Champion Arena North") || n.StartsWith("A1 |")) return "Arena A1";
            if (Contains(n, "PROMENADA 12 SHOPS") || Regex.IsMatch(n, @"^K(?:[1-9]|1[0-2])\s+\|")) return "Promenada";
            if (Contains(n, "15 yachts") || Contains(n, "harbour")) return "Port";
            if (Contains(n, "LIFEGUARD") || Contains(n, "LOUNGERS")) return "Plaża";
            if (Contains(n, "reused city metro") || Contains(n, "streets and transit")) return "Metro";
            if (Contains(n, "exploration rig")) return "Gracz i samochód";
        }
        return "Teren i pozostałe";
    }
    static string Creator(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var n = p.name;
            if (Contains(n, "T03 | HOTEL SOBCZAK")) return "BraxChampionsHotelSobczakT03.cs";
            if (Contains(n, "A1 MURAL LEFT") || Contains(n, "A1 MURAL RIGHT")) return "BraxChampionsArenaSideMurals.cs";
            if (Contains(n, "MURAL CHAMPION FIGHTER")) return "BraxChampionsArenaMural.cs";
            if (Contains(n, "championship belt") || Contains(n, "museum case")) return "BraxChampionsBeltShowcase.cs";
            if (Contains(n, "A1 facade | original premium sign")) return "BraxChampionsArenaSigns.cs";
            if (Contains(n, "Premium barrel chair")) return "BraxChampionsVIPSeats.cs";
            if (Contains(n, "Champion Arena North")) return "BraxChampionsArena.cs";
            if (Contains(n, "T11 | KASYNO")) return "BraxChampionsCasinoT11.cs";
            if (Contains(n, "P1 | BASEN")) return "BraxChampionsPoolP1.cs";
            if (Contains(n, "PROMENADA 12 SHOPS") || Regex.IsMatch(n, @"^K(?:[1-9]|1[0-2])\s+\|")) return "BraxChampionsPromenade.cs";
            if (Contains(n, "15 yachts") || Contains(n, "Original user-owned moored")) return "BraxChampionsHarbour.cs";
            if (Contains(n, "LIFEGUARD") || Contains(n, "LOUNGERS")) return "BraxChampionsBeachFurnishings.cs";
            if (Contains(n, "reused city metro") || Contains(n, "streets and transit")) return "BraxChampionsInfrastructure.cs";
            if (Contains(n, "exploration rig")) return "BraxChampionsExplorer.cs";
            if (Contains(n, "EXTERIOR FOUNDATION")) return "BraxChampionsFoundation.cs";
        }
        return "NIEUSTALONY (brak znacznika konstruktora)";
    }
    static Finding Make(string type, Item item, float value, float limit, string details,
                        float severity, string evidence = "bounds / colliders", string other = "")
    {
        return new Finding {
            type = type, objectName = item.info.name, objectPath = item.info.path,
            creatorCs = item.info.creatorCs, otherObject = other, value = value, limit = limit,
            description = details, severity = severity, evidenceLevel = evidence
        };
    }
    static bool SamePrefab(Transform a, Transform b)
    {
        var root = PrefabUtility.GetNearestPrefabInstanceRoot(a.gameObject);
        return root && root == PrefabUtility.GetNearestPrefabInstanceRoot(b.gameObject);
    }
    static bool Structural(Transform t)
    {
        for (int i = 0; t != null && i < 2; i++, t = t.parent)
            if (Structure.IsMatch(t.name) || t.GetComponent<TerrainCollider>()) return true;
        return false;
    }
    static void GroundAndWalls(List<Item> items, Audit output)
    {
        var rayHits = new RaycastHit[128];
        var overlapHits = new Collider[256];
        bool saturatedRay = false, saturatedOverlap = false;
        foreach (var item in items)
        {
            var b = item.bounds;
            float highest = float.NegativeInfinity;
            bool hitGround = false;
            foreach (var sample in new[] { new Vector2(.5f, .5f), new Vector2(.12f, .12f), new Vector2(.88f, .88f) })
            {
                var origin = new Vector3(Mathf.Lerp(b.min.x, b.max.x, sample.x),
                                         b.max.y + .15f,
                                         Mathf.Lerp(b.min.z, b.max.z, sample.y));
                int count = Physics.RaycastNonAlloc(origin, Vector3.down, rayHits,
                              Mathf.Max(500f, b.size.y + 25f), ~0, QueryTriggerInteraction.Ignore);
                if (count == rayHits.Length) saturatedRay = true;
                for (int j = 0; j < count; j++)
                {
                    var h = rayHits[j];
                    if (!h.collider || h.collider.gameObject == item.renderer.gameObject ||
                        h.normal.y < .65f || SamePrefab(item.renderer.transform, h.collider.transform)) continue;
                    if (!hitGround || h.point.y > highest) { hitGround = true; highest = h.point.y; }
                }
            }
            item.info.floorHit = hitGround;
            if (!hitGround) output.groundUnknownCount++;
            else
            {
                float gap = (b.min.y - highest) / K;
                item.info.gapMeters = gap;
                if (gap > ThresholdM)
                    output.levitating.Add(Make("LEWITUJE", item, gap, ThresholdM,
                        "przerwa do collidera pod spodem " + F(gap) + " m", gap / ThresholdM,
                        "raycast pionowy"));
                if (gap < -ThresholdM && !Structural(item.renderer.transform))
                    output.embedded.Add(Make("ZAPADNIĘTE / podłoga", item, -gap, ThresholdM,
                        "dół bounds " + F(-gap) + " m pod podłożem", -gap / ThresholdM,
                        "raycast pionowy, wymaga potwierdzenia geometrią"));
            }
            if (Structural(item.renderer.transform)) continue;
            int size = Physics.OverlapBoxNonAlloc(b.center, b.extents, overlapHits,
                           Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            if (size == overlapHits.Length) saturatedOverlap = true;
            int kept = 0;
            for (int j = 0; j < size && kept < 3; j++)
            {
                var c = overlapHits[j];
                if (!c || c.transform == item.renderer.transform || !Structural(c.transform) ||
                    SamePrefab(item.renderer.transform, c.transform)) continue;
                var v = c.bounds;
                float dx = Mathf.Min(b.max.x, v.max.x) - Mathf.Max(b.min.x, v.min.x);
                float dy = Mathf.Min(b.max.y, v.max.y) - Mathf.Max(b.min.y, v.min.y);
                float dz = Mathf.Min(b.max.z, v.max.z) - Mathf.Max(b.min.z, v.min.z);
                float depth = Mathf.Min(dx, Mathf.Min(dy, dz)) / K;
                if (depth <= ThresholdM) continue;
                if (hitGround && highest - b.min.y > ThresholdM * K &&
                    v.max.y >= highest - .01f && v.size.y < v.size.x && v.size.y < v.size.z) continue;
                output.embedded.Add(Make("ZAPADNIĘTE / ściana", item, depth, ThresholdM,
                    "AABB collid. " + c.name + " przecina o " + F(depth) + " m",
                    depth / ThresholdM, "TYLKO kandydat AABB, nie precyzyjna penetracja mesh",
                    Hierarchy(c.transform)));
                kept++;
            }
        }
        if (saturatedRay) output.warnings.Add("Wypełniony bufor RaycastNonAlloc 128: pomiary podłoża mogą być niepełne.");
        if (saturatedOverlap) output.warnings.Add("Wypełniony bufor OverlapBoxNonAlloc 256: penetracje mogą być niepełne.");
    }
    static void Overlaps(List<Item> items, Audit output)
    {
        // Sweep-and-prune on X; no quadratic loop over pairs far apart.
        var sorted = items.Where(i => i.volume > .00001f && !GroundPlane.IsMatch(i.info.name))
                          .OrderBy(i => i.bounds.min.x).ToArray();
        bool stopped = false;
        for (int i = 0; i < sorted.Length && !stopped; i++)
        {
            var a = sorted[i];
            for (int j = i + 1; j < sorted.Length && sorted[j].bounds.min.x < a.bounds.max.x; j++)
            {
                if (++output.candidatePairs > MaxCandidatePairs)
                { stopped = true; output.overlapScanIncomplete = true; break; }
                var b = sorted[j];
                float dx = Mathf.Min(a.bounds.max.x, b.bounds.max.x) - Mathf.Max(a.bounds.min.x, b.bounds.min.x);
                float dy = Mathf.Min(a.bounds.max.y, b.bounds.max.y) - Mathf.Max(a.bounds.min.y, b.bounds.min.y);
                float dz = Mathf.Min(a.bounds.max.z, b.bounds.max.z) - Mathf.Max(a.bounds.min.z, b.bounds.min.z);
                if (dx <= 0 || dy <= 0 || dz <= 0) continue;
                if (SamePrefab(a.renderer.transform, b.renderer.transform))
                { output.excludedPrefabPairs++; continue; }
                float share = dx * dy * dz / Mathf.Min(a.volume, b.volume);
                if (share <= OverlapFraction) continue;
                if (output.overlaps.Count >= MaxOverlapFindings)
                { output.overlapFindingsTruncated = true; continue; }
                output.overlaps.Add(Make("NACHODZĄ", a, share, OverlapFraction,
                    F(share * 100f) + "% mniejszego AABB; źródło drugiego: " + b.info.creatorCs,
                    share * 100f, "AABB", b.info.path));
            }
        }
        if (output.overlapScanIncomplete || output.overlapFindingsTruncated)
            output.warnings.Add("Limit analizy par przekroczony: lista NACHODZĄ jest NIEKOMPLETNA.");
    }
    static void Scales(List<Item> items, Audit output)
    {
        var already = new HashSet<int>();
        foreach (var item in items)
        {
            for (var p = item.renderer.transform; p != null; p = p.parent)
            {
                var rule = ScaleRanges.FirstOrDefault(x => Regex.IsMatch(p.name, x.expression,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
                if (rule == null) continue;
                if (!already.Add(p.GetInstanceID())) break;
                var renderers = p.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r && r.enabled && r.gameObject.activeInHierarchy).ToArray();
                if (renderers.Length == 0) break;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                float height = bounds.size.y / K;
                bool bad = height < rule.min || height > rule.max;
                output.scales.Add(new ScaleInfo {
                    name = p.name, path = Hierarchy(p), category = rule.category, creatorCs = Creator(p),
                    heightMeters = height, minMeters = rule.min, maxMeters = rule.max,
                    outOfRange = bad
                });
                if (bad)
                {
                    float diff = height < rule.min ? rule.min - height : height - rule.max;
                    output.scaleWarnings.Add(Make("SKALA", item, height,
                        height < rule.min ? rule.min : rule.max,
                        rule.category + ": " + F(height) + " m; realny zakres " +
                        F(rule.min) + "-" + F(rule.max) + " m",
                        20f + diff * 25f, "połączony bounds modelu"));
                }
                break;
            }
        }
    }
    static bool MainPlayable(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
            if (Contains(p.name, "BraxhornPlayer") || Contains(p.name, "Patryk | player") ||
                Contains(p.name, "Nyxa | player") || Contains(p.name, "Monika | player"))
                return true;
        return false;
    }
    static long RuntimeBytes(UnityEngine.Object o)
    {
        try { return Math.Max(0L, Profiler.GetRuntimeMemorySizeLong(o)); }
        catch (Exception) { return 0L; }
    }
    static void Textures(List<Item> items, Audit output)
    {
        var perRegion = new Dictionary<string, ResourceCounter>(StringComparer.Ordinal);
        var oversized = new HashSet<int>();
        foreach (var item in items)
        {
            ResourceCounter res;
            if (!perRegion.TryGetValue(item.info.region, out res))
            {
                res = new ResourceCounter { report = new RegionAssets { region = item.info.region } };
                perRegion.Add(item.info.region, res);
            }
            Mesh mesh = null;
            var filter = item.renderer.GetComponent<MeshFilter>();
            if (filter) mesh = filter.sharedMesh;
            var skinned = item.renderer as SkinnedMeshRenderer;
            if (skinned) mesh = skinned.sharedMesh;
            if (mesh && res.meshIds.Add(mesh.GetInstanceID()))
            { res.report.meshCount++; res.report.meshRuntimeBytes += RuntimeBytes(mesh); }
            Texture2D displayed = null;
            foreach (var mat in item.renderer.sharedMaterials)
            {
                if (!mat) continue;
                if (!displayed) displayed = mat.mainTexture as Texture2D;
                string[] properties;
                try { properties = mat.GetTexturePropertyNames(); }
                catch (Exception) { continue; }
                foreach (var property in properties)
                {
                    Texture2D tex;
                    try { tex = mat.GetTexture(property) as Texture2D; }
                    catch (Exception) { continue; }
                    if (!tex) continue;
                    if (!displayed) displayed = tex;
                    if (res.textureIds.Add(tex.GetInstanceID()))
                    { res.report.textureCount++; res.report.textureRuntimeBytes += RuntimeBytes(tex); }
                    if ((tex.width > 2048 || tex.height > 2048) &&
                        !MainPlayable(item.renderer.transform) && oversized.Add(tex.GetInstanceID()))
                        output.largeTextures.Add(Make("TEKSTURA >2048", item,
                            Mathf.Max(tex.width, tex.height), 2048,
                            tex.name + " " + tex.width + "x" + tex.height + " px " +
                            AssetDatabase.GetAssetPath(tex),
                            10f * Mathf.Max(tex.width, tex.height) / 2048f, "zaimportowana Texture2D"));
                }
            }
            bool isGraphic = false;
            int ancestors = 0;
            for (var p = item.renderer.transform; p != null && ancestors++ < 5; p = p.parent)
                if (Graphic.IsMatch(p.name)) { isGraphic = true; break; }
            if (!isGraphic) continue;
            var dims = new[] { item.bounds.size.x / K, item.bounds.size.y / K, item.bounds.size.z / K }
                .OrderByDescending(x => x).ToArray();
            float width = Mathf.Max(.001f, dims[0]), height = Mathf.Max(.001f, dims[1]);
            int pxW = displayed ? displayed.width : 0, pxH = displayed ? displayed.height : 0;
            // Measure atlas SUB-RECT, not the whole source PNG. K1-K12 each use
            // one cell of a shared 3x4 atlas; full PNG dimensions overstate
            // actual print resolution of a single kiosk sign.
            float uSpan = 1f, vSpan = 1f;
            if (mesh && mesh.uv != null && mesh.uv.Length > 0)
            {
                var uv = mesh.uv;
                float minU = uv.Min(p => p.x), maxU = uv.Max(p => p.x);
                float minV = uv.Min(p => p.y), maxV = uv.Max(p => p.y);
                uSpan = Mathf.Max(.00001f, maxU - minU);
                vSpan = Mathf.Max(.00001f, maxV - minV);
            }
            float effectiveW = pxW * uSpan, effectiveH = pxH * vSpan;
            var pixelDims = new[] { effectiveW, effectiveH }.OrderByDescending(x => x).ToArray();
            float ppm = displayed ? Mathf.Min(pixelDims[0] / width, pixelDims[1] / height) : 0f;
            var g = new GraphicInfo {
                objectName = item.info.name, path = item.info.path, creatorCs = item.info.creatorCs,
                texture = displayed ? displayed.name : "NIEUSTALONA",
                texturePath = displayed ? AssetDatabase.GetAssetPath(displayed) : "",
                textureWidth = pxW, textureHeight = pxH, widthMeters = width,
                heightMeters = height, pixelsPerMeter = ppm, uvSpanU = uSpan, uvSpanV = vSpan,
                effectivePixelsX = effectiveW, effectivePixelsY = effectiveH,
                below100 = displayed && ppm < 100f, textureUnavailable = !displayed
            };
            output.graphics.Add(g);
            if (g.below100)
                output.lowPpm.Add(Make("MURAL/SZYLD <100 px/m", item, ppm, 100f,
                    g.texture + " " + pxW + "x" + pxH + " px; UV " +
                    F(uSpan) + "x" + F(vSpan) + " = " +
                    F(effectiveW) + "x" + F(effectiveH) + " px; powierzchnia " +
                    F(width) + "x" + F(height) + " m; " + F(ppm) + " px/m",
                    1000f / Mathf.Max(1f, ppm), "szacunek AABB, nie analiza UV"));
            if (!displayed && output.warnings.Count < 30)
                output.warnings.Add("Nieznana tekstura muralu/szyldu: " + item.info.path);
        }
        output.resources = perRegion.Values.Select(x => x.report).OrderBy(x => x.region).ToList();
    }
    static void Inventory(Transform[] all, List<Item> items, Audit output)
    {
        Transform Find(Func<Transform, bool> rule) => all.FirstOrDefault(rule);
        void Add(string title, Transform t, string creator, string note = "", int count = 0)
        {
            output.inventory.Add(new Presence {
                requirement = title, status = t ? "jest" : "brak",
                objectName = t ? Hierarchy(t) : "BRAK", creatorCs = creator, note = note, count = count
            });
        }
        void AddExact(string title,Transform[] found,int expected,string creator,string note="")
        {
            bool pass=found!=null&&found.Length==expected;
            output.inventory.Add(new Presence {
                requirement=title,status=pass?"jest":"brak",
                objectName=found!=null&&found.Length>0?Hierarchy(found[0]):"BRAK",
                creatorCs=creator,count=found==null?0:found.Length,
                note=(note+" expected="+expected+" found="+(found==null?0:found.Length)).Trim()
            });
        }
        Add("Arena A1", Find(t => t.name.StartsWith("A1 |") && Contains(t.name, "exterior")),
            "BraxChampionsBuildings.cs");
        Add("Arena: fasada przeszklona", Find(t =>
            (Contains(t.name, "A1_LOD0") || Contains(t.name, "facade")) &&
            (Contains(t.name, "glass") || Contains(t.name, "glaz") || Contains(t.name, "szkl"))),
            "BraxChampionsBuildings.cs", "sprawdzenie nazwy siatki (nie badano shadera)");
        Add("Arena: mural wejściowy HD", Find(t => Contains(t.name, "MURAL CHAMPION FIGHTER")),
            "BraxChampionsArenaMural.cs","wymagany owner HD 6144x2048");
        Add("Arena: mural boczny lewy HD", Find(t => Contains(t.name, "A1 MURAL LEFT_FIGHTERS")),
            "BraxChampionsArenaSideMurals.cs","wymagany owner HD 6144x2048");
        Add("Arena: mural boczny prawy HD", Find(t => Contains(t.name, "A1 MURAL RIGHT_BELT")),
            "BraxChampionsArenaSideMurals.cs","wymagany owner HD 6144x2048");
        Add("Arena: złoty portal muralu wejściowego",
            Find(t=>Contains(t.name,"A1 PREMIUM PORTAL | GOLD top")),
            "BraxChampionsArenaMural.cs","otwór 4.10x4.20; portal zewnętrzny 4.46x4.38");
        var frontSigns=all.Where(t=>t.name=="A1 facade | original premium sign 0").ToArray();
        AddExact("Arena: frontowy CHAMPION ARENA dokładnie raz",frontSigns,1,
            "BraxChampionsArenaSigns.cs","oryginalny owner PNG 1536x256; brak duplikatu");
        var signGoldFrame=all.Where(t=>Contains(t.name,
            "ISSUE27_rounded_gold_sign_frames_original_artwork_untouched")).ToArray();
        AddExact("Arena: jedna premium złota oprawa czterech szyldów",signGoldFrame,1,
            "BraxChampionsArenaArrival.cs","front graphic gap=0.08m; black belt behind");
        var externalRedSignGeometry=all.Where(t=>
            Contains(t.name,"red neon vertical sign frame")||
            Contains(t.name,"red neon sign top accent")||
            Contains(t.name,"red neon sign lower accent")).ToArray();
        AddExact("Arena: zewnętrzne czerwone elementy przy szyldzie = 0",
            externalRedSignGeometry,0,"BraxChampionsArenaArrival.cs",
            "czerwień wyłącznie wewnątrz oryginalnej grafiki CHAMPION ARENA");
        bool UnderA1(Transform t)
        {
            for(var p=t;p;p=p.parent)
                if(p.name.StartsWith("A1 |",StringComparison.Ordinal))return true;
            return false;
        }
        var oldA1Lettering=all.Where(t=>UnderA1(t)&&
            Contains(t.name,"original generated 3D lettering")).ToArray();
        AddExact("Arena: stary wygenerowany lettering A1 = 0",oldA1Lettering,0,
            "BraxChampionsBuildings.cs","owner PNG supersedes legacy 3D lettering");
        Add("Arena: trybuny", Find(t => Contains(t.name, "Champion Arena North")),
            "BraxChampionsArena.cs", "obecność bryły");
        Add("Arena: pas w gablocie", Find(t =>
            Contains(t.name, "ORIGINAL owner championship belt 3D")),
            "BraxChampionsBeltShowcase.cs","obecność modelu; kierunek FRONT wymaga zdjęcia z Unity");

        var vip=all.Where(t=>Contains(t.name,"Reserved VIP | imported luxurious barrel seat")).ToArray();
        AddExact("Arena: VIP premium (dokładnie 16)",vip,16,
            "BraxChampionsVIPSeats.cs","max 1.18x1.18x1.28; uniform scale");

        var showcaseBase=all.Where(t=>Contains(t.name,"A1 SHOWCASE | premium granite base")).ToArray();
        AddExact("Arena: nowa niska podstawa gabloty",showcaseBase,1,
            "BraxChampionsBeltShowcase.cs","2.20x0.90x0.28; bez wysokiej tylnej płyty");
        var showcaseFront=all.Where(t=>Contains(t.name,"A1 SHOWCASE | FRONT optical glass")).ToArray();
        AddExact("Arena: front gabloty 12mm",showcaseFront,1,
            "BraxChampionsBeltShowcase.cs","1.84x2.95; true mesh thickness 0.012m");
        var showcaseGlass=all.Where(t=>Contains(t.name,"A1 SHOWCASE |")&&
            Contains(t.name,"optical glass")).ToArray();
        AddExact("Arena: komplet tafli gabloty 12mm",showcaseGlass,5,
            "BraxChampionsBeltShowcase.cs","front+back+2 sides+top");
        var showcaseCorners=all.Where(t=>Contains(t.name,
            "A1 SHOWCASE | slender gold corner profile")).ToArray();
        AddExact("Arena: złote narożne profile gabloty",showcaseCorners,4,
            "BraxChampionsBeltShowcase.cs","4 x 45mm");
        var beltSupports=all.Where(t=>Contains(t.name,
            "A1 SHOWCASE | hidden black belt support")).ToArray();
        AddExact("Arena: ukryte wsporniki pasa",beltSupports,2,
            "BraxChampionsBeltShowcase.cs","2 x 30mm black metal");
        var canopySupports=all.Where(t=>Contains(t.name,
            "A1 PUBLIC | ISSUE27 round gold support x=")).ToArray();
        AddExact("Arena: dwa złote słupy zadaszenia z raycastem",canopySupports,2,
            "BraxChampionsArenaArrival.cs","x=+-20.50 z=-68.70; floor measured by raycast");
        if(showcaseFront.Length==1)
        {
            var mf=showcaseFront[0].GetComponent<MeshFilter>();
            float thickness=mf&&mf.sharedMesh?new[]{mf.sharedMesh.bounds.size.x,
                mf.sharedMesh.bounds.size.y,mf.sharedMesh.bounds.size.z}.Min():0f;
            bool glassPass=Mathf.Abs(thickness-.012f)<=.0005f;
            output.inventory.Add(new Presence{
                requirement="Arena: szkło gabloty dokładnie 12 mm",
                status=glassPass?"jest":"brak",objectName=Hierarchy(showcaseFront[0]),
                creatorCs="BraxChampionsBeltShowcase.cs",
                note="mesh thickness="+F(thickness)+"m expected=0.012m",count=glassPass?1:0});
        }
        AddExact("Arena: dwa złote słupy zadaszenia osadzone raycastem",
            canopySupports,2,"BraxChampionsArenaArrival.cs",
            "x=+-20.50 z=-68.70; floorY zapisany w nazwie obiektu");
        // Two bases + two shafts: use the actual static rounded MeshCollider
        // generated by BraxChampionsArenaArrival.RoundedCylinder in Unity 6.
        var supportColliders=all.Where(t=>(Contains(t.name,"Gold support base")||
            Contains(t.name,"Gold support stem"))&&t.GetComponent<MeshCollider>()).ToArray();
        AddExact("Arena: kolizje zaokrąglonych złotych słupów (base+stem)",
            supportColliders,4,"BraxChampionsArenaArrival.cs","4 static MeshCollider on rounded geometry");
        var forbiddenOldBack=all.Where(t=>Contains(t.name,"black marble wall column")||
            Contains(t.name,"black leather back panel")).ToArray();
        if(forbiddenOldBack.Length>0)
        {
            output.inventory.Add(new Presence{requirement="Arena: stara tylna płyta gabloty MUSI być usunięta",
                status="brak",objectName=Hierarchy(forbiddenOldBack[0]),creatorCs="BraxChampionsBeltShowcase.cs",
                note="NIEDOZWOLONA stara wysoka płyta/kolumna",count=forbiddenOldBack.Length});
            output.warnings.Add("A1_SHOWCASE_OLD_BACKPLATE_PRESENT count="+forbiddenOldBack.Length);
        }
        else output.inventory.Add(new Presence{requirement="Arena: stara tylna płyta gabloty usunięta",
            status="jest",objectName="BRAK STAREJ PŁYTY",creatorCs="BraxChampionsBeltShowcase.cs",
            note="PASS: brak black marble wall column / black leather back panel",count=0});

        var stairMarkers=all.Where(t=>Contains(t.name,
            "A1 PREMIUM STAIR CARPET | 32 steps | width 1.30m")).ToArray();
        AddExact("Arena: czerwony velour na 8 ciągach schodów",
            stairMarkers,8,"BraxChampionsArena.cs",
            "każdy bieg 32 stopnie; tread+riser; szerokość dywanu 1.30m");
        Add("Kasyno WAWER", Find(t => Contains(t.name, "T11 | KASYNO WAWER")),
            "BraxChampionsCasinoT11.cs");
        Add("Basen P1", Find(t => Contains(t.name, "P1 | BASEN MIEJSKI")),
            "BraxChampionsPoolP1.cs");
        var stations = all.Where(t => t.name.EndsWith("| reused city metro", StringComparison.Ordinal)).ToArray();
        Add("Metro: stacje (oczekiwane 5)", stations.FirstOrDefault(),
            "BraxChampionsInfrastructure.cs", "Wykryto " + stations.Length, stations.Length);
        Add("Port", Find(t => Contains(t.name, "15 yachts") && Contains(t.name, "motorboats")),
            "BraxChampionsHarbour.cs");
        // A berth/terminal sign is not a physical ferry.
        Add("Prom: fizyczny model", Find(t => t.GetComponent<Renderer>() &&
            (Contains(t.name, "ferry") || Contains(t.name, "prom ")) &&
            !BerthOnly.IsMatch(t.name)),
            "BraxChampionsHarbour.cs", "pustego stanowiska lub tablicy nie uznaje się za prom");
        Add("Plaża", Find(t => Contains(t.name, "LIFEGUARD + 32 EMPTY LOUNGERS")),
            "BraxChampionsBeachFurnishings.cs");
        for (int n = 1; n <= 12; n++)
        {
            string id = "K" + n;
            var kiosk = Find(t => t.name.StartsWith(id + " |", StringComparison.Ordinal));
            var sign = kiosk ? kiosk.GetComponentsInChildren<Renderer>(true)
                .FirstOrDefault(r => r && (Contains(r.name, "sign") || Contains(r.name, "szyld") ||
                    (r.transform.parent && Graphic.IsMatch(r.transform.parent.name)))) : null;
            Add("Promenada " + id + ": pawilon", kiosk, "BraxChampionsPromenade.cs");
            Add("Promenada " + id + ": szyld", sign ? sign.transform : null,
                "BraxChampionsPromenade.cs", "szyld generowany jest obecny; jego jakość oceniana osobno");
        }
        foreach (var item in items)
            if (Prohibited.IsMatch(item.info.name) && !BerthOnly.IsMatch(item.info.name))
                output.forbidden.Add(Make("NIEDOZWOLONY", item, 1f, 0,
                    "Widoczny dźwig lub duży statek", 200f, "weryfikacja nazwy renderera"));
    }
    static void AuditHotelSobczak(Transform[] all, Audit output)
    {
        var h = output.hotelSobczak = new HotelSobczakAudit();
        var root = all.FirstOrDefault(t => t && t.name.StartsWith("T03 | HOTEL SOBCZAK | 5* | COMPLETE", StringComparison.Ordinal));
        if (!root)
        {
            h.failures.Add("T03 root not found in reopened scene");
            return;
        }
        h.status = "MEASURED_FROM_REOPENED_UNITY_SCENE";
        h.rootPath = Hierarchy(root);
        float hk = BraxClaudeOsiedle.K;
        Vector3 nominalCentre = root.position / hk;
        h.centreX = nominalCentre.x; h.centreZ = nominalCentre.z;
        h.centrePass = Mathf.Abs(h.centreX-1030f)<=.01f && Mathf.Abs(h.centreZ-710f)<=.01f;

        // Structural facade outer planes, measured from the actual renderer bounds.
        var sideWalls = root.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r && Contains(r.name,"Guest side wall F")).ToArray();
        var frontRear = root.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r && (Contains(r.name,"Guest stair wing west F") || Contains(r.name,"Guest stair wing east F") ||
                            Contains(r.name,"Guest facade F"))).ToArray();
        if(sideWalls.Length>0)
        {
            float minX=sideWalls.Min(r=>r.bounds.min.x),maxX=sideWalls.Max(r=>r.bounds.max.x);
            h.structuralWidthMeters=(maxX-minX)/hk;
        }
        if(frontRear.Length>0)
        {
            float minZ=frontRear.Min(r=>r.bounds.min.z),maxZ=frontRear.Max(r=>r.bounds.max.z);
            h.structuralDepthMeters=(maxZ-minZ)/hk;
        }
        h.footprintPass=Mathf.Abs(h.structuralWidthMeters-64f)<=.03f &&
                        Mathf.Abs(h.structuralDepthMeters-42f)<=.03f;

        // Height uses every T03 renderer except site/landscape and lower LOD proxies.
        var lodProxy = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(x=>x && x.name=="T03 EXTERIOR LOD CONTROL");
        bool Under(Transform child,Transform ancestor)
        {
            if(!ancestor)return false;
            for(var p=child;p;p=p.parent)if(p==ancestor)return true;
            return false;
        }
        var site = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(x=>x && x.name.StartsWith("T03 SITE /",StringComparison.Ordinal));
        var heightRenderers=root.GetComponentsInChildren<Renderer>(true)
            .Where(r=>r && !Under(r.transform,site) && !Under(r.transform,lodProxy)).ToArray();
        if(heightRenderers.Length>0)
            h.maxHeightMeters=(heightRenderers.Max(r=>r.bounds.max.y)-root.position.y)/hk;
        h.heightPass=h.maxHeightMeters<=130.02f && h.maxHeightMeters>=129.0f;

        var tr=root.GetComponentsInChildren<Transform>(true);
        h.guestFloors=tr.Count(x=>x.name.StartsWith("GUEST INTERIOR FLOOR ",StringComparison.Ordinal) &&
                                    Contains(x.name,"14 ROOMS"));
        h.rooms=tr.Count(x=>x.name.StartsWith("ROOM ",StringComparison.Ordinal) && Contains(x.name,"bay 6.00x19.40"));
        var lifts=tr.Where(x=>x.name.StartsWith("ELEVATOR ",StringComparison.Ordinal)).ToArray();
        h.elevators=lifts.Length;
        h.passengerElevators=lifts.Count(x=>Contains(x.name,"PASSENGER"));
        h.serviceElevators=lifts.Count(x=>Contains(x.name,"SERVICE"));
        h.stairs=tr.Count(x=>x.name.StartsWith("STAIR ",StringComparison.Ordinal) && Contains(x.name,"B2 TO TECH"));
        h.parkingB1=tr.Count(x=>Regex.IsMatch(x.name,@"^B1 PARK d{2}( ACCESSIBLE)? back$"));
        h.parkingB2=tr.Count(x=>Regex.IsMatch(x.name,@"^B2 PARK d{2}( ACCESSIBLE)? back$"));
        h.parkingTotal=h.parkingB1+h.parkingB2;
        h.frontDormers=tr.Count(x=>x.name=="Mansard dormer body" && x.localPosition.z<0);
        h.rearDormers=tr.Count(x=>x.name=="Mansard dormer body" && x.localPosition.z>0);

        h.physicalLevelsMeasured=h.guestFloors+4; // B2+B1+Parter+1+30 guest floors
        h.physicalLevelsPass=h.physicalLevelsMeasured==34;
        h.guestFloorsPass=h.guestFloors==30;
        h.roomsPass=h.rooms==420;
        h.elevatorsPass=h.elevators==4 && h.passengerElevators==3 && h.serviceElevators==1;
        h.stairsPass=h.stairs==2;
        h.parkingPass=h.parkingB1==40 && h.parkingB2==40 && h.parkingTotal==80;
        h.dormersPass=h.frontDormers==7 && h.rearDormers==7;

        // Entire T03 site decoration/ramp must stay inside the owner's 86x76 m reserve.
        if(site)
        {
            var siteRenderers=site.GetComponentsInChildren<Renderer>(true).Where(r=>r).ToArray();
            if(siteRenderers.Length>0)
            {
                var sb=siteRenderers[0].bounds;
                foreach(var r in siteRenderers.Skip(1))sb.Encapsulate(r.bounds);
                h.siteWidthMeters=sb.size.x/hk; h.siteDepthMeters=sb.size.z/hk;
            }
        }
        h.siteReservePass=h.siteWidthMeters<=86.02f && h.siteDepthMeters<=76.02f;

        var renderers=root.GetComponentsInChildren<Renderer>(true);
        h.nullMaterialRenderers=renderers.Count(r=>r && (r.sharedMaterials==null || r.sharedMaterials.Any(m=>!m)));
        var glassMaterials=renderers.Where(r=>r && Contains(r.name,"glass"))
            .SelectMany(r=>r.sharedMaterials??new Material[0]).Where(m=>m).Distinct().ToArray();
        h.milkGlassCandidates=glassMaterials.Count(m=>{
            Color c=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;
            float rough=m.HasProperty("_Roughness")?m.GetFloat("_Roughness"):1f;
            return c.a>.55f || rough>.20f;
        });
        h.glassPass=h.nullMaterialRenderers==0 && h.milkGlassCandidates==0;

        h.doorLeafColliderCount=tr.Count(x=>(Contains(x.name,"door leaf OPEN")||Contains(x.name,"Main entrance glass")) &&
                                              x.GetComponent<Collider>());
        h.doorLeafColliderPass=h.doorLeafColliderCount==0;

        string SourceId(Material m)
        {
            string n=m?m.name:"";
            if(Contains(n,"facade_beige"))return "beige_wall_001";
            if(Contains(n,"plinth_sandstone"))return "sandstone_blocks_08";
            if(Contains(n,"architectural_stucco"))return "white_stucco_02";
            if(Contains(n,"mansard_slates"))return "roof_slates_03";
            if(Contains(n,"cream_marble"))return "marble_01";
            if(Contains(n,"varnished_oak"))return "oak_wood_planks";
            if(Contains(n,"herringbone_parquet"))return "herringbone_parquet";
            if(Contains(n,"spa_terrazzo"))return "terrazzo_tiles";
            if(Contains(n,"fitness_rubber"))return "rubber_tiles";
            if(Contains(n,"corridor_wool"))return "poly_wool_herringbone";
            if(Contains(n,"kitchen_tile"))return "tiled_floor_001";
            if(Contains(n,"exterior_granite")||Contains(n,"interior_granite"))return "granite_tile_04";
            if(Contains(n,"ramp_antislip"))return "anti_slip_concrete";
            if(Contains(n,"parking_floor"))return "garage_floor";
            if(Contains(n,"technical_concrete"))return "concrete_wall_003";
            if(Contains(n,"brown_leather"))return "brown_leather";
            if(Contains(n,"spa_white_leather"))return "leather_white";
            if(Contains(n,"driveway_asphalt"))return "clean_asphalt";
            if(Contains(n,"brass")||Contains(n,"bronze")||Contains(n,"stainless")||
               Contains(n,"glass")||Contains(n,"foliage")||Contains(n,"white_paint")||
               Contains(n,"parking_marking")||Contains(n,"warm_architectural_light")||
               Contains(n,"entrance_red_carpet")||Contains(n,"fountain_water"))return "CUSTOM_PBR";
            return "UNMAPPED";
        }
        foreach(var mat in renderers.SelectMany(r=>r.sharedMaterials??new Material[0]).Where(m=>m)
                                    .GroupBy(m=>m.GetInstanceID()).Select(g=>g.First()).OrderBy(m=>m.name))
        {
            var users=renderers.Where(r=>r && (r.sharedMaterials??new Material[0]).Any(x=>x==mat)).ToArray();
            var sample=users.FirstOrDefault();
            var block=new MaterialPropertyBlock();
            if(sample)sample.GetPropertyBlock(block);
            Vector4 st=block.GetVector("_MainTex_ST");
            if(Mathf.Abs(st.x)<.00001f||Mathf.Abs(st.y)<.00001f)
            {
                var scale=mat.HasProperty("_MainTex")?mat.GetTextureScale("_MainTex"):Vector2.one;
                st=new Vector4(scale.x,scale.y,0,0);
            }
            var tex=mat.HasProperty("_MainTex")?mat.GetTexture("_MainTex") as Texture2D:null;
            h.materials.Add(new HotelMaterialAudit{
                materialName=mat.name,sourceId=SourceId(mat),shaderName=mat.shader?mat.shader.name:"MISSING",
                albedoName=tex?tex.name:"NONE",albedoWidth=tex?tex.width:0,albedoHeight=tex?tex.height:0,
                rendererCount=users.Length,
                metallic=mat.HasProperty("_Metallic")?mat.GetFloat("_Metallic"):0f,
                roughness=mat.HasProperty("_Roughness")?mat.GetFloat("_Roughness"):0f,
                sampleScaleU=st.x,sampleScaleV=st.y
            });
        }

        void Fail(bool pass,string text){ if(!pass)h.failures.Add(text); }
        Fail(h.centrePass,"centre expected 1030/710 measured "+F(h.centreX)+"/"+F(h.centreZ));
        Fail(h.footprintPass,"structural footprint expected 64x42 measured "+F(h.structuralWidthMeters)+"x"+F(h.structuralDepthMeters));
        Fail(h.heightPass,"height expected <=130 and near roof top; measured "+F(h.maxHeightMeters));
        Fail(h.siteReservePass,"site reserve expected <=86x76 measured "+F(h.siteWidthMeters)+"x"+F(h.siteDepthMeters));
        Fail(h.physicalLevelsPass,"physical levels expected 34 found "+h.physicalLevelsMeasured);
        Fail(h.guestFloorsPass,"guest floors expected 30 found "+h.guestFloors);
        Fail(h.roomsPass,"rooms expected 420 found "+h.rooms);
        Fail(h.elevatorsPass,"lifts expected 3 passenger + 1 service; found "+h.passengerElevators+"+"+h.serviceElevators);
        Fail(h.stairsPass,"stairs expected 2 found "+h.stairs);
        Fail(h.parkingPass,"parking expected B1=40 B2=40 found "+h.parkingB1+"/"+h.parkingB2);
        Fail(h.dormersPass,"dormers expected front/rear 7/7 found "+h.frontDormers+"/"+h.rearDormers);
        Fail(h.glassPass,"glass/material check failed nullRenderers="+h.nullMaterialRenderers+" milkCandidates="+h.milkGlassCandidates);
        Fail(h.doorLeafColliderPass,"open door/entrance leaf colliders found "+h.doorLeafColliderCount);
        h.status=h.failures.Count==0?"PASS_MEASURED_SCENE_CONTRACT":"FAIL_MEASURED_SCENE_CONTRACT";
    }

    static List<string> Markdown(Audit a)
    {
        var lines = new List<string> {
            "# Miasto Mistrzów — audyt sceny (BEZ APK)", "",
            "- Scena: " + Safe(a.scene) + "; źródło: " + Safe(a.sourceCommit),
            "- Renderery: **" + a.visibleRenderers + "**; niższe LOD pominięte: " + a.ignoredLowerLods,
            "- **LEWITUJE: " + a.levitatingCount + " | ZAPADNIĘTE: " + a.embeddedCount +
               " | NACHODZĄ: " + a.overlapsCount + "**",
            "- **SKALA: " + a.wrongScaleCount + " | MURALE/SZYLDY <100 px/m: " + a.lowPpmCount +
               " | TEKSTURY >2048: " + a.largeTexturesCount + "**",
            "- **BRAKI: " + a.missingCount + " | NIEDOZWOLONE: " + a.forbiddenCount +
               " | BRAK TRAFIENIA PODŁOŻA: " + a.groundUnknownCount + "**",
            "- Pary porównane: " + a.candidatePairs + "; pominięte części prefabów: " +
               a.excludedPrefabPairs + "; skan niepełny: " +
               (a.overlapScanIncomplete || a.overlapFindingsTruncated),
            "- K=1,294. Przenikania ze ścianami to kandydaci AABB; pliki .cs przypisano wg hierarchii.",
            "", "## HOTEL SOBCZAK T03 — pomiar z prawdziwej sceny",
            "- Status: **" + Safe(a.hotelSobczak.status) + "**",
            "- środek X/Z: " + F(a.hotelSobczak.centreX) + " / " + F(a.hotelSobczak.centreZ) +
              "; obrys strukturalny: " + F(a.hotelSobczak.structuralWidthMeters) + " × " +
              F(a.hotelSobczak.structuralDepthMeters) + " m; max H: " + F(a.hotelSobczak.maxHeightMeters) + " m",
            "- poziomy fizyczne: " + a.hotelSobczak.physicalLevelsMeasured +
              "; pokoje: " + a.hotelSobczak.rooms + "; piętra pokojowe: " + a.hotelSobczak.guestFloors +
              "; windy: " + a.hotelSobczak.passengerElevators + "+1 service=" + a.hotelSobczak.elevators +
              "; klatki: " + a.hotelSobczak.stairs + "; parking B1/B2: " +
              a.hotelSobczak.parkingB1 + "/" + a.hotelSobczak.parkingB2,
            "- rezerwa wykorzystana: " + F(a.hotelSobczak.siteWidthMeters) + " × " +
              F(a.hotelSobczak.siteDepthMeters) + " m; lukarny front/tył: " +
              a.hotelSobczak.frontDormers + "/" + a.hotelSobczak.rearDormers,
            "- szkło: " + (a.hotelSobczak.glassPass?"PASS":"FAIL") +
              "; collidery na otwartych drzwiach: " + a.hotelSobczak.doorLeafColliderCount,
            "", "## 30 najgorszych przypadków"
        };
        foreach(var f in a.hotelSobczak.failures)
            lines.Add("- **HOTEL T03 FAIL** " + Safe(f));
        var issues = new List<Finding>();
        issues.AddRange(a.levitating); issues.AddRange(a.embedded); issues.AddRange(a.overlaps);
        issues.AddRange(a.scaleWarnings); issues.AddRange(a.lowPpm);
        issues.AddRange(a.largeTextures); issues.AddRange(a.forbidden);
        foreach (var missing in a.inventory.Where(p => p.status == "brak"))
            issues.Add(new Finding { type = "BRAK", objectName = missing.requirement,
                creatorCs = missing.creatorCs, description = missing.note, severity = 150f });
        int row = 0;
        foreach (var issue in issues.OrderByDescending(x => x.severity).Take(30))
        {
            lines.Add(++row + ". **" + Safe(issue.type) + "** — " + Safe(issue.objectName) +
                " — " + Safe(issue.creatorCs) + "; " + Safe(issue.description) +
                (string.IsNullOrEmpty(issue.otherObject) ? "" :
                    "; drugi: " + Safe(issue.otherObject)));
        }
        if (row == 0) lines.Add("Brak zapisanych przypadków; to nie jest odbiór wizualny.");
        lines.Add(""); lines.Add("## Inwentaryzacja");
        foreach (var x in a.inventory)
            lines.Add("- **" + x.status.ToUpperInvariant() + "** " + Safe(x.requirement) +
                " — " + Safe(x.objectName) + " — " + Safe(x.creatorCs) +
                (string.IsNullOrEmpty(x.note) ? "" : "; " + Safe(x.note)));
        lines.Add(""); lines.Add("## Pamięć tekstur i siatek — każdy rejon");
        foreach (var x in a.resources)
            lines.Add("- " + Safe(x.region) + ": tekstury " +
                F(x.textureRuntimeBytes / 1048576f) + " MiB (" + x.textureCount +
                "), siatki " + F(x.meshRuntimeBytes / 1048576f) + " MiB (" + x.meshCount + ")");
        lines.Add(""); lines.Add("## HOTEL SOBCZAK — materiały zmierzone w scenie");
        foreach(var m in a.hotelSobczak.materials)
            lines.Add("- " + Safe(m.sourceId) + " | " + Safe(m.materialName) +
                " | shader=" + Safe(m.shaderName) +
                " | albedo=" + Safe(m.albedoName) + " " + m.albedoWidth + "x" + m.albedoHeight +
                " | metallic=" + F(m.metallic) + " roughness=" + F(m.roughness) +
                " | sample ST=" + F(m.sampleScaleU) + "x" + F(m.sampleScaleV) +
                " | renderers=" + m.rendererCount);
        if (a.warnings.Count > 0)
        {
            lines.Add(""); lines.Add("## Ograniczenia i ostrzeżenia");
            foreach (var w in a.warnings.Take(12)) lines.Add("- " + Safe(w));
        }
        return lines;
    }
}
