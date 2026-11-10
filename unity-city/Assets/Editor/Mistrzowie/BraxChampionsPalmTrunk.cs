using System;
using System.Collections.Generic;
using UnityEngine;

// Owner 10.11 12:39 ("palma na chodniku", "palma w kostce rośnie") and 15:17-15:18 ("uważaj na te palmy, które sadzasz ...
// na chodnikach", "na bank tutaj są palmy na chodnikach na 100%"). BraxMiastoAssety.Postaw (R1, unchanged) puts the
// model's ORIGIN on the planting spot. The owner's palms lean: when the origin of a pack model is not under its trunk,
// the trunk stands a metre or two beside its bed - on the paving. Every palm of the region is therefore moved after
// Postaw so the centre of its trunk foot (the LOD0 vertices in the lowest 0.6 m of the model) is exactly on the spot the
// generator chose (the centre of the bed / lawn / planter). Yaw and height are not changed.
public static class BraxChampionsPalmTrunk
{
    static readonly Dictionary<Mesh, Vector3> footCache = new Dictionary<Mesh, Vector3>();
    public static float MaxShift { get; private set; }   // nominal metres
    public static int Moved { get; private set; }
    public static int Unreadable { get; private set; }
    public static void Reset() { footCache.Clear(); MaxShift = 0f; Moved = 0; Unreadable = 0; }

    public static void OnSpot(GameObject o, Vector3 spot)
    {
        if (!o) return;
        var filters = LodZero(o);
        if (filters.Length == 0 || !filters[0].sharedMesh) return;
        var key = filters[0].sharedMesh;
        if (!footCache.TryGetValue(key, out var foot))
        {
            var pts = new List<Vector3>(); float minY = float.MaxValue;
            try
            {
                foreach (var mf in filters)
                {
                    var mesh = mf.sharedMesh; if (!mesh) continue;
                    foreach (var v in mesh.vertices)
                    {
                        var p = o.transform.InverseTransformPoint(mf.transform.TransformPoint(v));   // root-local = model metres
                        pts.Add(p); if (p.y < minY) minY = p.y;
                    }
                }
            }
            catch (Exception e)
            {
                Unreadable++;
                BraxBuildQuality.Warn("Miasto Mistrzów: palma " + o.name + " - siatka nieczytelna, pień nie wyrównany: " + e.Message);
                return;
            }
            if (pts.Count == 0) return;
            Vector3 s = Vector3.zero; int n = 0;
            foreach (var p in pts) if (p.y < minY + .6f) { s += p; n++; }      // root-local = model metres
            foot = s / Mathf.Max(1, n); foot.y = 0f;
            footCache[key] = foot;
        }
        var w = o.transform.TransformPoint(foot);
        var d = new Vector3(spot.x - w.x, 0f, spot.z - w.z);
        if (d.sqrMagnitude < 1e-8f) return;
        o.transform.position += d;
        Moved++;
        MaxShift = Mathf.Max(MaxShift, d.magnitude / BraxClaudeOsiedle.K);
    }

    static MeshFilter[] LodZero(GameObject o)
    {
        foreach (var t in o.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("_LOD0")) return t.GetComponentsInChildren<MeshFilter>(true);
        return o.GetComponentsInChildren<MeshFilter>(true);
    }
}
