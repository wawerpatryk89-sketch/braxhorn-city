using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Miejsca do siedzenia dla Braxa (decyzja właściciela 31.10): każda sofa, ławka, hoker, krzesło w mieście dostaje
/// punkt BraxSeat (wysokość siedziska, kierunek siedzenia). Kierunek: strona bez oparcia (najniższa średnia wysokość
/// wierzchołków w tej połowie bryły). Łóżka („łóżko”, „bed”) → leżenie. Runtime: BraxCityWorld „USIĄDŹ · …” / „WSTAŃ”.
/// </summary>
public static class BraxRegionSeats
{
    static readonly string[] SitKeys = { "sofa", "ławka", "lawka", "bench", "hoker", "bar_chair", "stołek", "stolek", "krzesło", "krzeslo", "chair", "seating", "loża", "loza" };
    static readonly string[] LieKeys = { "łóżko", "lozko", "bed_", "bed ", "leżak", "lezak" };

    public static int Apply(Transform cityRoot)
    {
        var done = new HashSet<Transform>();
        int n = 0;
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!r || !r.enabled) continue;
            var owner = Owner(r.transform, out bool lie);
            if (!owner || done.Contains(owner)) continue;
            done.Add(owner);
            var rs = owner.GetComponentsInChildren<MeshRenderer>(true).Where(x => x.enabled).ToArray();
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.x > 5f || b.size.z > 5f || b.size.y > 2.2f || b.size.y < .3f) continue;
            if (owner.GetComponentInChildren<BraxSeat>()) continue;
            string low = owner.name.ToLowerInvariant();
            float surface;
            if (lie) surface = b.min.y + Mathf.Clamp(b.size.y * .85f, .35f, .75f);
            else if (low.Contains("hoker") || low.Contains("bar_chair") || low.Contains("stołek") || low.Contains("stolek")) surface = b.max.y - .03f;
            else if (low.Contains("ławka") || low.Contains("lawka") || low.Contains("bench") || low.Contains("seating")) surface = b.min.y + Mathf.Clamp(b.size.y * .55f, .38f, .5f);
            else surface = b.min.y + Mathf.Clamp(b.size.y * .42f, .36f, .52f);
            var facing = Facing(rs, b);
            var seat = new GameObject("Siedzisko: " + owner.name);
            seat.transform.SetParent(owner, true);
            seat.transform.position = new Vector3(b.center.x, surface, b.center.z);
            seat.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            var s = seat.AddComponent<BraxSeat>();
            s.lie = lie; s.surfaceY = surface;
            s.label = lie ? "ŁÓŻKO" : low.Contains("sofa") ? "SOFA" : (low.Contains("hoker") || low.Contains("bar_chair") || low.Contains("stołek")) ? "HOKER" : (low.Contains("krzes") || low.Contains("chair")) ? "KRZESŁO" : "ŁAWKA";
            n++;
        }
        BraxRegion.Note("siedziska: " + n);
        return n;
    }

    // Najwyższy przodek o nazwie siedziska, którego bryła ma ≤ 3 m w planie (cały leżak, nie jego deska; nie cały zestaw z parasolem).
    static Transform Owner(Transform t, out bool lie)
    {
        lie = false; Transform best = null; bool bestLie = false;
        for (var c = t; c; c = c.parent)
        {
            string low = c.name.ToLowerInvariant();
            if (low.StartsWith("claude") || low.StartsWith("braxhorn") || low.Contains("renovation")) break;
            bool isLie = LieKeys.Any(k => low.Contains(k)), isSit = SitKeys.Any(k => low.Contains(k));
            if (!isLie && !isSit) continue;
            var rs = c.GetComponentsInChildren<MeshRenderer>(true);
            if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.x > 3f || b.size.z > 3f) break;
            best = c; bestLie = isLie;
        }
        lie = bestLie; return best;
    }

    /// <summary>Strona bez oparcia: z czterech kierunków (±X, ±Z) ta, w której połowie bryły wierzchołki są najniżej.</summary>
    static Vector3 Facing(MeshRenderer[] rs, Bounds b)
    {
        var dirs = new[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
        var sum = new float[4]; var cnt = new int[4];
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || !mf.sharedMesh.isReadable) continue;
            var v = mf.sharedMesh.vertices; int step = Mathf.Max(1, v.Length / 4000);
            for (int i = 0; i < v.Length; i += step)
            {
                var w = r.transform.TransformPoint(v[i]);
                var d = w - b.center;
                int k = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? (d.x > 0 ? 2 : 3) : (d.z > 0 ? 0 : 1);
                sum[k] += w.y - b.min.y; cnt[k]++;
            }
        }
        int best = 0; float bestMean = float.MaxValue;
        for (int k = 0; k < 4; k++) { if (cnt[k] == 0) continue; float m = sum[k] / cnt[k]; if (m < bestMean) { bestMean = m; best = k; } }
        return dirs[best];
    }
}
