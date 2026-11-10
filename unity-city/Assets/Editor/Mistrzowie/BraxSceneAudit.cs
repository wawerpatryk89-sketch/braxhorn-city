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
        public string materialName, sourceId, shaderName, albedoName, normalName, armName, zones;
        public int albedoWidth, albedoHeight, normalWidth, normalHeight, armWidth, armHeight, rendererCount;
        public float metallic, roughness, normalStrength, tilingMeters, sampleScaleU, sampleScaleV;
    }
    [Serializable] public sealed class HotelSobczakAudit
    {
        public string status = "NOT_FOUND";
        public string rootPath;
        public string design = "rebuild 9.11: podium 42x38 granite + brick tower 34x32, 110 m, 26 guest floors x 10 rooms";
        public float centreX, centreZ, rootScale;
        public float podiumWidthMeters, podiumDepthMeters, towerWidthMeters, towerDepthMeters, maxHeightMeters;
        public float siteWidthMeters, siteDepthMeters;
        public int guestFloors, streamedFloors, rooms, roomNumbersDistinct, elevators, liftStops, liftLandingDoors, stairs;
        public int automaticDoors, automaticDoorLeavesWithCollision, signBodies, treesInPlot, parkingPlanned, polyHavenModels;
        public int nullMaterialRenderers, milkGlassCandidates, unmappedMaterialCount, oversizedRuntimeTextureCount;
        public bool centrePass, scaleOncePass, footprintPass, heightPass, siteReservePass;
        public bool roomsPass, guestFloorsPass, elevatorsPass, stairsPass, entrancePass, signPass, noTreesPass;
        public bool glassPass, materialMappingPass, runtimeTexturePass;
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
        // Owner 10.11 12:55 (APK 37): "Wszędzie za mną ta rysa chodzi" - a broken white line at eye level on every wall.
        // Cause: TextMesh labels (stops, wayfinding, berths) used the built-in font material (GUI/Text Shader, ZTest
        // Always): distant white text drew THROUGH every wall at the horizon once the view reached the whole region.
        // The region builds no TextMesh any more (3D letters); this catches any text that still ignores the depth.
        int throughWalls = 0;
        foreach (var t in transforms)
        {
            var mr = t.GetComponent<MeshRenderer>(); if (!mr) continue;
            foreach (var m in mr.sharedMaterials)
                if (m && m.shader && (m.shader.name == "GUI/Text Shader" || t.GetComponent("TextMesh")))
                { throughWalls++; BraxBuildQuality.Warn("DO POPRAWKI REGION_TEXT_THROUGH_WALLS " + Hierarchy(t)); break; }
        }
        Debug.Log("REGION_TEXT_THROUGH_WALLS count=" + throughWalls + " (GUI/Text Shader = ZTest Always; must be 0)");
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
        var frontSigns=all.Where(t=>t.name=="A1 facade | original premium sign 0 | owner artwork on the 3D body").ToArray();
        AddExact("Arena: frontowy CHAMPION ARENA dokładnie raz",frontSigns,1,
            "BraxChampionsArenaSigns.cs","oryginalny owner PNG 1536x256 na bryle 39x6.5x1.5; brak duplikatu");
        // Owner 9.11: every sign is a 3D body (artwork outline extruded).
        var signBodies=all.Where(t=>Contains(t.name,"black metal body extruded from the artwork outline")).ToArray();
        // Owner 9.11 23:45: the two hall banners (exterior signs hung inside) are removed: four facade signs.
        AddExact("Arena: szyldy jako bryły 3D",signBodies,4,
            "BraxChampionsArenaSigns.cs","front 1.5 m na zadaszeniu, 3 elewacje 0.32 m; w hali żadnych szyldów z zewnątrz");
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
            "BraxChampionsVIPSeats.cs","real model size (cm->m) x K; uniform scale");

        // 9.11 owner photo vitrine (lobby): measured sizes, not the old low-base case.
        var vitrinePillar=all.Where(t=>Contains(t.name,"A1 VITRINE | black marble pillar")).ToArray();
        AddExact("Arena: filar z czarnego marmuru pod gablotą",vitrinePillar,1,
            "BraxChampionsBeltShowcase.cs","1.06x3.00x0.45 m x K; marble ambientCG Marble016");
        var showcaseFront=all.Where(t=>Contains(t.name,"A1 VITRINE | FRONT glass")).ToArray();
        AddExact("Arena: front gabloty 12mm",showcaseFront,1,
            "BraxChampionsBeltShowcase.cs","0.63x1.79 m (zmierzone na zdjęciu); true mesh thickness 0.012m");
        var showcaseGlass=all.Where(t=>Contains(t.name,"A1 VITRINE |")&&Contains(t.name," glass")&&
            t.GetComponent<MeshFilter>()).ToArray();
        AddExact("Arena: komplet tafli gabloty 12mm",showcaseGlass,6,
            "BraxChampionsBeltShowcase.cs","front+2 sides+top+bottom+shelf");
        var showcaseBrackets=all.Where(t=>Contains(t.name,"A1 VITRINE | brass side bracket")).ToArray();
        AddExact("Arena: mosiężne uchwyty boczne gabloty",showcaseBrackets,4,
            "BraxChampionsBeltShowcase.cs","as on the owner photo");
        var beltSupports=all.Where(t=>Contains(t.name,"A1 VITRINE | gold belt pin")).ToArray();
        AddExact("Arena: złote kołki pasa",beltSupports,4,
            "BraxChampionsBeltShowcase.cs","2 top + 2 bottom, photo ratios");
        // Owner render 9.11: gold cantilever canopy, no posts.
        var canopySupports=all.Where(t=>Contains(t.name,
            "A1 PUBLIC | ISSUE27 round gold support x=")).ToArray();
        AddExact("Arena: stare złote słupy zadaszenia usunięte",canopySupports,0,
            "BraxChampionsArenaFront.cs","render: zadaszenie wspornikowe bez słupów");
        var goldCanopy=all.Where(t=>t.name=="A1 owner render canopy gold fascia").ToArray();
        AddExact("Arena: złote zadaszenie jak na renderze",goldCanopy,1,
            "BraxChampionsArenaFront.cs","40 m, spód 6.20, fascia 1.20 m, oczka światła");
        var lobbySofas=all.Where(t=>Contains(t.name,"A1 LOBBY | black tufted leather sofa")).ToArray();
        AddExact("Arena: sofy w lobby za szybą wejścia",lobbySofas,4,
            "BraxChampionsArenaFront.cs","Poly Haven sofa_02 2K, realny rozmiar x K");
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
        var forbiddenOldBack=all.Where(t=>Contains(t.name,"black leather back panel")).ToArray();
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
        // 9.11 owner: pool P1 removed completely.
        var pool=all.Where(t=>Contains(t.name,"P1 | BASEN")).ToArray();
        if(pool.Length>0)output.warnings.Add("P1_POOL_STILL_IN_SCENE count="+pool.Length);
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
        Add("Plaża", Find(t => Contains(t.name, "LIFEGUARD + BEACH CLUB")),
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
    // HOTEL SOBCZAK T03 rebuilt 9.11 (owner board "MUROWANY MODERN BRICK"): every value below is measured
    // from the reopened scene, never copied from the generator constants.
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
        h.rootScale = root.lossyScale.x;
        h.scaleOncePass = Mathf.Abs(h.rootScale-hk)<=.0005f;

        Transform Group(string prefix)=>root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(x=>x && x.name.StartsWith(prefix,StringComparison.Ordinal));
        Bounds? Extent(Transform g,Func<Renderer,bool> keep)
        {
            if(!g)return null;
            var rs=g.GetComponentsInChildren<Renderer>(true).Where(r=>r && keep(r)).ToArray();
            if(rs.Length==0)return null;
            var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        // Structural masses only (granite of the podium, brick of the tower); stone relief may add <=0.35 m per side.
        var podium=Extent(Group("T03 PODIUM /"),r=>Contains(r.name,"podium_granite"));
        var tower=Extent(Group("T03 TOWER /"),r=>Contains(r.name,"brick_facade"));
        if(podium.HasValue){h.podiumWidthMeters=podium.Value.size.x/hk;h.podiumDepthMeters=podium.Value.size.z/hk;}
        if(tower.HasValue){h.towerWidthMeters=tower.Value.size.x/hk;h.towerDepthMeters=tower.Value.size.z/hk;}
        h.footprintPass=h.podiumWidthMeters>=42f && h.podiumWidthMeters<=42.7f && h.podiumDepthMeters>=38f && h.podiumDepthMeters<=38.7f &&
                        h.towerWidthMeters>=34f && h.towerWidthMeters<=34.7f && h.towerDepthMeters>=32f && h.towerDepthMeters<=32.7f;

        // Height above the ground floor (+0.30) to the top of the crown, site excluded.
        bool Under(Transform child,Transform ancestor)
        {
            if(!ancestor)return false;
            for(var p=child;p;p=p.parent)if(p==ancestor)return true;
            return false;
        }
        var site=Group("T03 SITE /");
        var heightRenderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r && !Under(r.transform,site)).ToArray();
        if(heightRenderers.Length>0)
            h.maxHeightMeters=(heightRenderers.Max(r=>r.bounds.max.y)-root.position.y)/hk-.30f;
        h.heightPass=h.maxHeightMeters>=109.9f && h.maxHeightMeters<=110.6f;

        var tr=root.GetComponentsInChildren<Transform>(true);
        h.guestFloors=tr.Count(x=>x.name.StartsWith("GUEST INTERIOR FLOOR ",StringComparison.Ordinal) && Contains(x.name,"10 ROOMS"));
        h.rooms=tr.Count(x=>x.name.StartsWith("ROOM ",StringComparison.Ordinal) && Contains(x.name,"6.80x10.50"));
        h.roomNumbersDistinct=tr.Where(x=>x.name.StartsWith("ROOM ",StringComparison.Ordinal)).Select(x=>x.name.Split('|')[0].Trim()).Distinct().Count();
        var lifts=root.GetComponentsInChildren<BraxWinda>(true);
        h.elevators=lifts.Length;
        h.liftStops=lifts.Length>0?lifts.Min(w=>w.pietraY.Length):0;
        h.liftLandingDoors=lifts.Sum(w=>w.przystanekL.Count(x=>x)+w.przystanekP.Count(x=>x));
        h.stairs=tr.Count(x=>x.name.StartsWith("STAIR ",StringComparison.Ordinal) && Contains(x.name,"PARTER TO TARAS"));
        var doors=root.GetComponentsInChildren<BraxDrzwiAutomatyczne>(true);
        h.automaticDoors=doors.Length;
        h.automaticDoorLeavesWithCollision=doors.Sum(d=>(d.skrzydloL&&d.skrzydloL.GetComponentInChildren<Collider>(true)?1:0)+
                                                        (d.skrzydloP&&d.skrzydloP.GetComponentInChildren<Collider>(true)?1:0));
        h.polyHavenModels=tr.Count(x=>Contains(x.name,"| Poly Haven CC0"));
        h.signBodies=tr.Count(x=>x.name.StartsWith("SOBCZAK HOTEL 3D sign",StringComparison.Ordinal) && x.GetComponentsInChildren<MeshRenderer>(true).Length>=3);
        h.treesInPlot=root.GetComponentsInChildren<Renderer>(true).Count(r=>r && (Contains(r.name,"tree")||Contains(r.name,"palm")||Contains(r.name,"foliage")));
        var streamer=root.GetComponent<BraxHotelSobczakFloorStreamer>();
        h.streamedFloors=streamer?streamer.guestInteriors.Count(x=>x):0;
        h.parkingPlanned=60;   // owner board: B1+B2 "docelowo 60 miejsc" — built after the floors (not in this rebuild)

        h.guestFloorsPass=h.guestFloors==26 && h.streamedFloors==26;
        h.roomsPass=h.rooms==260 && h.roomNumbersDistinct==260;
        h.elevatorsPass=h.elevators==3 && h.liftStops==29 && h.liftLandingDoors==3*29*2;
        h.stairsPass=h.stairs==2;
        h.entrancePass=h.automaticDoors==2 && h.automaticDoorLeavesWithCollision==4;
        h.signPass=h.signBodies==2;                                    // owner 10.11 20:09: no roof signs
        h.noTreesPass=h.treesInPlot==0;

        // Whole site (plaza, drop-off, island) stays inside the plot + frontage to the road sidewalk.
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
        h.siteReservePass=h.siteWidthMeters<=60.02f && h.siteDepthMeters<=73.3f;

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

        string SourceId(Material m,Renderer user)
        {
            string n=m?m.name:"";
            string shader=m&&m.shader?m.shader.name:"";
            if(Contains(n,"font")||Contains(n,"LegacyRuntime")||Contains(n,"napisy")||Contains(shader,"GUI/Text")||Contains(shader,"CitySignText"))return "CUSTOM_TEXT";
            if(user&&user.transform.GetComponentsInParent<Transform>(true).Any(p=>Contains(p.name,"| Poly Haven CC0")))return "POLYHAVEN_MODEL";
            if(Contains(n,"brick_facade"))return "exterior_wall_cladding_02";
            if(Contains(n,"podium_travertine"))return "Travertine009";
            if(Contains(n,"podium_granite"))return "granite_tile_03";
            if(Contains(n,"dark_granite")||Contains(n,"granite_kerb"))return "granite_tile";
            if(Contains(n,"sidewalk_paving"))return "concrete_pavement_02";
            if(Contains(n,"drive_asphalt"))return "clean_asphalt";
            if(Contains(n,"teak_veneer")||Contains(n,"teak_dark"))return "teak_veneer";
            if(Contains(n,"white_marble")||Contains(n,"bath_marble_floor"))return "Marble021";
            if(Contains(n,"cream_marble"))return "marble_01";
            if(Contains(n,"black_marble"))return "Marble016";
            if(Contains(n,"room_oak_floor"))return "laminate_floor_02";
            if(Contains(n,"corridor_carpet")||Contains(n,"room_rug_jacquard")||Contains(n,"lobby_rug_jacquard"))return "floral_jacquard";
            if(Contains(n,"room_rug"))return "poly_wool_herringbone";
            if(Contains(n,"brushed_stainless"))return "Metal048A";
            if(Contains(n,"spa_terrazzo"))return "terrazzo_tiles";
            if(Contains(n,"kitchen_tile"))return "tiled_floor_001";
            if(Contains(n,"wall_paint"))return "beige_wall_001";
            if(Contains(n,"ceiling_stucco"))return "white_stucco_02";
            if(Contains(n,"technical_concrete"))return "concrete_wall_003";
            if(Contains(n,"brown_leather"))return "brown_leather";
            if(Contains(n,"white_leather"))return "leather_white";
            if(Contains(n,"bed_linen")||Contains(n,"sheer_curtain"))return "fabric_pattern_05";
            if(Contains(n,"fitness_rubber"))return "rubber_tiles";
            if(Contains(n,"linen_wallcovering"))return "terlenka";
            if(Contains(n,"boucle_upholstery"))return "curly_teddy_natural";
            if(Contains(n,"taupe_upholstery"))return "cotton_jersey";
            if(Contains(n,"linen_drapes")||Contains(n,"white_bed_linen"))return "terlenka";
            if(Contains(n,"champagne_satin"))return "crepe_satin";
            if(Contains(n,"burgundy_velvet"))return "velour_velvet";
            if(Contains(n,"room_rug_jacquard"))return "floral_jacquard";
            if(Contains(n,"lobby_rug_jacquard"))return "floral_jacquard";
            if(Contains(n,"smoked_walnut"))return "smoked_walnut_veneer";
            if(Contains(n,"cognac_leather"))return "fabric_leather_02";
            if(Contains(n,"restaurant_parquet"))return "herringbone_parquet";
            if(Contains(n,"kitchen_wall_tiles"))return "long_white_tiles";
            if(Contains(n,"kitchen_floor_tiles"))return "anti_skid_tiles";
            if(Contains(n,"grass")||Contains(n,"planter_lawn"))return "REGION_GRASS";
            if(Contains(n,"brass")||Contains(n,"bronze")||Contains(n,"stainless")||Contains(n,"glass")||Contains(n,"mirror")||
               Contains(n,"porcelain")||Contains(n,"warm_architectural_light")||Contains(n,"led_strip")||Contains(n,"pool_water")||
               Contains(n,"chrome")||Contains(n,"tv_black_glass")||Contains(n,"frosted_glass"))return "CUSTOM_PBR";
            return "UNMAPPED";
        }
        float TileMeters(string sourceId)
        {
            switch(sourceId)
            {
                case "exterior_wall_cladding_02": return 2.0f;
                case "granite_tile_03": return 1.8f;
                case "granite_tile": return 1.2f;
                case "concrete_pavement_02": return 1.8f;
                case "clean_asphalt": return 2.5f;
                case "teak_veneer": return 1.2f;
                case "marble_01": return 1.5f;
                case "Marble016": return 2.5f;
                case "Marble021": return 2.4f;
                case "Travertine009": return 2.2f;
                case "Metal048A": return .65f;
                case "laminate_floor_02": return 1.8f;
                case "oak_wood_planks": return 1.6f;
                case "poly_wool_herringbone": return .40f;
                case "terrazzo_tiles": return 1.6f;
                case "tiled_floor_001": return .8f;
                case "beige_wall_001": return 2.2f;
                case "white_stucco_02": return 2.0f;
                case "concrete_wall_003": return 3.0f;
                case "brown_leather": return .35f;
                case "leather_white": return .35f;
                case "fabric_pattern_05": return .6f;
                case "rubber_tiles": return 2.0f;
                case "hessian_230": return .55f;
                case "curly_teddy_natural": return .45f;
                case "cotton_jersey": return .55f;
                case "terlenka": return .65f;
                case "crepe_satin": return .60f;
                case "velour_velvet": return .50f;
                case "floral_jacquard": return .90f;
                case "smoked_walnut_veneer": return 1.2f;
                case "fabric_leather_02": return .45f;
                case "herringbone_parquet": return 1.8f;
                case "long_white_tiles": return 1.0f;
                case "anti_skid_tiles": return 1.0f;
                default:return 0f;
            }
        }
        string HotelZone(Transform t)
        {
            for(var p=t;p && p!=root;p=p.parent)
            {
                if(p.name.StartsWith("T03 SITE /",StringComparison.Ordinal))return "EXTERIOR_SITE";
                if(p.name.StartsWith("GUEST INTERIOR FLOOR ",StringComparison.Ordinal))return "GUEST_FLOORS";
                if(p.name.StartsWith("T03 PUBLIC FLOORS /",StringComparison.Ordinal))return "PARTER_PIETRO_1";
                if(p.name.StartsWith("T03 VERTICAL CORES /",StringComparison.Ordinal))return "VERTICAL_CORES";
                if(p.name.StartsWith("T03 ROOF /",StringComparison.Ordinal))return "ROOF";
                if(p.name.StartsWith("T03 SIGN /",StringComparison.Ordinal))return "SIGN";
                if(p.name.StartsWith("T03 PODIUM /",StringComparison.Ordinal))return "PODIUM";
                if(p.name.StartsWith("T03 TOWER /",StringComparison.Ordinal))return "TOWER";
                if(p.name.StartsWith("T03 DETAILS /",StringComparison.Ordinal))return "FACADE_DETAILS";
            }
            return "T03_OTHER";
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
            var normal=mat.HasProperty("_BumpMap")?mat.GetTexture("_BumpMap") as Texture2D:null;
            var arm=mat.HasProperty("_ARM")?mat.GetTexture("_ARM") as Texture2D:null;
            string sourceId=SourceId(mat,sample);
            h.materials.Add(new HotelMaterialAudit{
                materialName=mat.name,sourceId=sourceId,shaderName=mat.shader?mat.shader.name:"MISSING",
                albedoName=tex?tex.name:"NONE",albedoWidth=tex?tex.width:0,albedoHeight=tex?tex.height:0,
                normalName=normal?normal.name:"NONE",normalWidth=normal?normal.width:0,normalHeight=normal?normal.height:0,
                armName=arm?arm.name:"NONE",armWidth=arm?arm.width:0,armHeight=arm?arm.height:0,
                rendererCount=users.Length,
                metallic=mat.HasProperty("_Metallic")?mat.GetFloat("_Metallic"):0f,
                roughness=mat.HasProperty("_Roughness")?mat.GetFloat("_Roughness"):0f,
                normalStrength=mat.HasProperty("_BumpScale")?mat.GetFloat("_BumpScale"):0f,
                tilingMeters=TileMeters(sourceId),
                sampleScaleU=st.x,sampleScaleV=st.y,
                zones=string.Join(",",users.Select(r=>HotelZone(r.transform)).Distinct().OrderBy(z=>z))
            });
        }
        h.unmappedMaterialCount=h.materials.Count(x=>x.sourceId=="UNMAPPED");
        h.materialMappingPass=h.unmappedMaterialCount==0;
        h.oversizedRuntimeTextureCount=h.materials.Count(x=>
            x.albedoWidth>2048||x.albedoHeight>2048||x.normalWidth>2048||x.normalHeight>2048||x.armWidth>2048||x.armHeight>2048);
        h.runtimeTexturePass=h.oversizedRuntimeTextureCount==0;

        void Fail(bool pass,string text){ if(!pass)h.failures.Add(text); }
        Fail(h.centrePass,"centre expected 1030/710 measured "+F(h.centreX)+"/"+F(h.centreZ));
        Fail(h.scaleOncePass,"root scale expected K once ("+F(hk)+") measured "+F(h.rootScale));
        Fail(h.footprintPass,"podium expected 42x38 measured "+F(h.podiumWidthMeters)+"x"+F(h.podiumDepthMeters)+
                             "; tower expected 34x32 measured "+F(h.towerWidthMeters)+"x"+F(h.towerDepthMeters));
        Fail(h.heightPass,"height above the ground floor expected 110 m measured "+F(h.maxHeightMeters));
        Fail(h.siteReservePass,"site expected inside plot + frontage 60x73.2 measured "+F(h.siteWidthMeters)+"x"+F(h.siteDepthMeters));
        Fail(h.guestFloorsPass,"guest floors expected 26 (streamed 26) found "+h.guestFloors+" (streamed "+h.streamedFloors+")");
        Fail(h.roomsPass,"rooms expected 260 distinct found "+h.rooms+" ("+h.roomNumbersDistinct+" distinct)");
        Fail(h.elevatorsPass,"lifts expected 3 x 29 stops with 174 landing door leaves; found "+h.elevators+" x "+h.liftStops+", "+h.liftLandingDoors);
        Fail(h.stairsPass,"stairs expected 2 found "+h.stairs);
        Fail(h.entrancePass,"entrance expected 2 automatic door pairs, 4 leaves with collision; found "+h.automaticDoors+"/"+h.automaticDoorLeavesWithCollision);
        Fail(h.signPass,"3D SOBCZAK HOTEL sign bodies expected 2 (facade, reception; no roof signs, owner 10.11 20:09) found "+h.signBodies);
        Fail(h.noTreesPass,"owner 9.11: no trees in front of the hotel; tree/foliage renderers found "+h.treesInPlot);
        Fail(h.glassPass,"glass/material check failed nullRenderers="+h.nullMaterialRenderers+" milkCandidates="+h.milkGlassCandidates);
        Fail(h.materialMappingPass,"visible hotel materials without source mapping "+h.unmappedMaterialCount);
        Fail(h.runtimeTexturePass,"hotel runtime material maps above 2048px "+h.oversizedRuntimeTextureCount);
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
              "; podium: " + F(a.hotelSobczak.podiumWidthMeters) + " × " + F(a.hotelSobczak.podiumDepthMeters) +
              " m; wieża: " + F(a.hotelSobczak.towerWidthMeters) + " × " + F(a.hotelSobczak.towerDepthMeters) +
              " m; wysokość od parteru: " + F(a.hotelSobczak.maxHeightMeters) + " m",
            "- pokoje: " + a.hotelSobczak.rooms + " (różnych numerów " + a.hotelSobczak.roomNumbersDistinct +
              "); piętra pokojowe: " + a.hotelSobczak.guestFloors + "; windy: " + a.hotelSobczak.elevators +
              " × " + a.hotelSobczak.liftStops + " przystanków; klatki: " + a.hotelSobczak.stairs +
              "; parking B1+B2: planowany " + a.hotelSobczak.parkingPlanned + " (jeszcze nie zbudowany)",
            "- wejście: drzwi automatyczne " + a.hotelSobczak.automaticDoors + " pary, skrzydła z kolizją " +
              a.hotelSobczak.automaticDoorLeavesWithCollision + "; szyld 3D: " + a.hotelSobczak.signBodies +
              "; drzewa na działce: " + a.hotelSobczak.treesInPlot + "; modele Poly Haven: " + a.hotelSobczak.polyHavenModels,
            "- teren: " + F(a.hotelSobczak.siteWidthMeters) + " × " + F(a.hotelSobczak.siteDepthMeters) +
              " m; szkło: " + (a.hotelSobczak.glassPass?"PASS":"FAIL"),
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
        foreach(var m in a.hotelSobczak.materials.Take(24))
            lines.Add("- " + Safe(m.sourceId) + " | " + Safe(m.materialName) +
                " | zones=" + Safe(m.zones) + " | shader=" + Safe(m.shaderName) +
                " | albedo=" + Safe(m.albedoName) + " " + m.albedoWidth + "x" + m.albedoHeight +
                " | normalMap=" + Safe(m.normalName) + " " + m.normalWidth + "x" + m.normalHeight +
                " | ARM=" + Safe(m.armName) + " " + m.armWidth + "x" + m.armHeight +
                " | tile=" + F(m.tilingMeters) + "m | normalStrength=" + F(m.normalStrength) +
                " | metallic=" + F(m.metallic) + " roughness=" + F(m.roughness) +
                " | sample ST=" + F(m.sampleScaleU) + "x" + F(m.sampleScaleV) +
                " | renderers=" + m.rendererCount);
        if(a.hotelSobczak.materials.Count>24)
            lines.Add("- pełna macierz " + a.hotelSobczak.materials.Count + " materiałów jest w JSON; Markdown ograniczony dla raportu <=150 linii.");
        if (a.warnings.Count > 0)
        {
            lines.Add(""); lines.Add("## Ograniczenia i ostrzeżenia");
            foreach (var w in a.warnings.Take(12)) lines.Add("- " + Safe(w));
        }
        return lines;
    }
}
