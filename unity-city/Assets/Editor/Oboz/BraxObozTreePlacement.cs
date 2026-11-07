using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// A whole authored tree shares one vertical placement across all LODs. The
// transparent camera margin of a distant image is not a second tree foundation.
public static class BraxObozTreePlacement
{
    const float K = BraxObozPlacement.K;
    const string GroundMeshName = "Teren kompaktowy 280 x 280 m; strumien zewnetrzny";
    [Serializable] public sealed class Row
    {
        public string name, sourceNear, sourceMid, support, warning;
        public bool measured, geometryAndScaleUnchanged = true, midGeometryPresent, billboardCanvasMeasured;
        public int sourceLodCount;
        public Vector3 beforePosition, afterPosition, worldScale;
        public float nearBottomBefore, nearBottomAfter, midBottomAfter;
        public float supportY, nearGapMetres, midGapMetres, lodBaseDifferenceMetres;
        public float billboardCanvasBottomAfter;
        public string billboardMetric = "transparent canvas bounds; not the source trunk base";
    }
    [Serializable] public sealed class Report
    {
        public string title = "OBÓZ TRENINGOWY — podparcie kompletnych drzew i zgodność LOD";
        public string revision;
        public bool apkBuilt = false, geometryDecimated = false, renderedAppearanceVerified = false;
        public int trees, measured, missingSupport, lodMismatch;
        public Row[] rows;
        public string[] warnings;
    }
    static readonly List<Row> Rows = new List<Row>();
    static readonly List<string> Warnings = new List<string>();
    static MeshCollider ground;

    static void Warn(string text)
    {
        Warnings.Add(text);
        BraxBuildQuality.Warn("OBÓZ TRENINGOWY — las: " + text);
    }

    public static void Begin(Transform camp)
    {
        Rows.Clear(); Warnings.Clear(); ground = null;
        // Exact generated ground, not a roof, foliage collider or stream water.
        // A changed ground generator must supply/update this explicit contract.
        var matches = camp.GetComponentsInChildren<MeshCollider>(true)
            .Where(c => c && c.enabled && c.sharedMesh && c.sharedMesh.name == GroundMeshName).ToArray();
        if (matches.Length == 1) ground = matches[0];
        else Warn("niejednoznaczny lub brak rzeczywistego collidera terenu: " + matches.Length);
        Physics.SyncTransforms();
    }

    public static bool Place(Transform tree)
    {
        var row = new Row { name = tree.name, beforePosition = tree.position,
            afterPosition = tree.position, worldScale = tree.lossyScale };
        Rows.Add(row);
        var record = BraxObozPlacementRecord.For(tree);
        record.label = "Całe źródłowe drzewo — " + tree.name;
        record.support = BraxObozPlacementRecord.SupportKind.Ground;
        record.contactPoint = tree; // Authoritative trunk x/z, not canopy centre.
        try
        {
            var group = tree.GetComponent<LODGroup>();
            var levels = group ? group.GetLODs() : Array.Empty<LOD>();
            var geometry=levels.Select(level=>level.renderers.Where(r=>r &&
                ModelSource(r).EndsWith(".glb",StringComparison.OrdinalIgnoreCase)).ToArray()).Where(r=>r.Length>0).ToArray();
            if (geometry.Length == 0)throw new InvalidOperationException("brak rzeczywistego źródłowego LOD drzewa");
            var near = geometry[0];
            var mid = geometry.Length>1?geometry[1]:Array.Empty<Renderer>();
            row.sourceLodCount=geometry.Length;row.midGeometryPresent=mid.Length>0;
            BraxObozTreeLodDistances.ConfigureNamed(tree);
            record.SetMembers(levels.SelectMany(level => level.renderers).Where(r => r).Distinct().ToArray());
            record.contactRenderers = near;
            var before = BraxObozPlacement.Bounds(near);
            row.nearBottomBefore = before.min.y;
            row.sourceNear = ModelSource(near[0]); row.sourceMid = mid.Length>0?ModelSource(mid[0]):"no authored LOD2; source LOD1 plus billboard";
            if (!ground) throw new InvalidOperationException("brak zweryfikowanego terenu; pozostawiono pozycję bez zgadywania");
            // Keep the source trunk's x/z and all existing scale/yaw. A canopy
            // may be asymmetric, so its bounding-box centre is not the trunk.
            var from = new Vector3(tree.position.x, before.max.y + K, tree.position.z);
            float distance = Mathf.Max(0, from.y - ground.bounds.min.y) + 2 * K;
            if (!ground.Raycast(new Ray(from, Vector3.down), out RaycastHit hit, distance))
                throw new InvalidOperationException("raycast spod osi pnia nie trafił w teren; pozycja pozostała niezmieniona");
            tree.position += Vector3.up * (hit.point.y - before.min.y);
            record.verticalSupport = ground;
            Physics.SyncTransforms();
            row.afterPosition = tree.position; row.support = ground.name; row.supportY = hit.point.y;
            row.nearBottomAfter = BraxObozPlacement.Bounds(near).min.y;
            row.nearGapMetres = (row.nearBottomAfter - hit.point.y) / K;
            if(mid.Length>0)
            {
                row.midBottomAfter = BraxObozPlacement.Bounds(mid).min.y;
                row.midGapMetres = (row.midBottomAfter - hit.point.y) / K;
                row.lodBaseDifferenceMetres = (row.midBottomAfter - row.nearBottomAfter) / K;
            }
            var far=levels.SelectMany(level=>level.renderers).Where(r=>r && r.sharedMaterial &&
                r.sharedMaterial.shader && r.sharedMaterial.shader.name=="Braxhorn/ObozTreeBillboard").ToArray();
            if(far.Length>0)
            {
                row.billboardCanvasBottomAfter = BraxObozPlacement.Bounds(far).min.y;
                row.billboardCanvasMeasured=true;
            }
            row.measured = true;
            if (Mathf.Abs(row.lodBaseDifferenceMetres) > .03f)
            {
                row.warning = "źródłowe LOD1/LOD2 mają różne podstawy > 3 cm; nie przesunięto poszczególnych LOD";
                Warn(tree.name + ": " + row.warning);
            }
            return true;
        }
        catch (Exception ex)
        {
            row.warning = ex.ToString(); Warn(tree.name + ": " + ex); return false;
        }
    }

    static string ModelSource(Renderer renderer)
    {
        var mesh = renderer.GetComponent<MeshFilter>();
        return mesh && mesh.sharedMesh ? AssetDatabase.GetAssetPath(mesh.sharedMesh) : "unknown";
    }

    public static void Write()
    {
        var report = new Report { revision = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
            trees = Rows.Count, measured = Rows.Count(r => r.measured), missingSupport = Rows.Count(r => !r.measured),
            lodMismatch = Rows.Count(r => r.measured && Mathf.Abs(r.lodBaseDifferenceMetres) > .03f),
            rows = Rows.ToArray(), warnings = Warnings.ToArray() };
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/CampLifeReview"));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "tree-lod-grounding.json"), JsonUtility.ToJson(report, true));
        Debug.Log("BRAXHORN_OBOZ_TREE_GROUNDING trees=" + report.trees + " measured=" + report.measured +
            " missing=" + report.missingSupport + " lod_mismatch=" + report.lodMismatch + " apk=false");
    }
}
