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
        AddExact("Arena: nowa lekka gablota szklana",showcaseFront,1,
            "BraxChampionsBeltShowcase.cs","1.84x0.72x2.95");
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
            "", "## 30 najgorszych przypadków"
        };
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
        if (a.warnings.Count > 0)
        {
            lines.Add(""); lines.Add("## Ograniczenia i ostrzeżenia");
            foreach (var w in a.warnings.Take(12)) lines.Add("- " + Safe(w));
        }
        return lines;
    }
}
