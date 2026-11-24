using System;
using System.Collections.Generic;
using UnityEngine;

// Owner 23.11 19:12 (the GTA beach: "chcę taki piasek i fale na morzu i woda jak prawdziwa"), 19:27 ("fale niech się
// wylewają na plażę jak normalnie"): what the sea shader (ChampionsMorze) needs to know about the shore, worked out once
// from the beach's own triangles (BraxChampionsPlaza gathers them; this part is plain arithmetic, the region's tools run it
// on the real beach without Unity: unity-tools/mistrzowie/morze_sim).
//   d   - the signed distance to the waterline (the line where the sand crosses the sea level), in game units:
//         positive over the sea, negative over the sand (where a wave's run-up climbs). The waves' fronts run along it,
//         the surf breaks by it, the water deepens and turns turquoise, then blue, with it;
//   dno - the height of the bed: the sand itself where there is sand (also above the sea, for the run-up), beyond it the
//         beach's slope (2.25 %) going on and getting deeper, started from the sand's own last height (no step).
// The shader sees the bed through the water (the shallow water is clear, the sand under it shows) and puts the white lip
// where the water over the sand gets thin.
public sealed class BraxChampionsMorzeBrzeg
{
    public const float Spadek = .0225f, Glebienie = .00035f;
    public readonly int w, h;
    public readonly float x0, z0, x1, z1, sea, cw, ch;
    public readonly float[] d, dno;
    readonly float[] piasek;
    readonly List<Vector4> odcinki = new List<Vector4>();
    public int trojkaty, komorekPiasku, komorekLadu;
    public int Odcinki => odcinki.Count;

    public BraxChampionsMorzeBrzeg(float x0, float z0, float x1, float z1, int w, int h, float sea)
    {
        this.x0 = x0; this.z0 = z0; this.x1 = x1; this.z1 = z1; this.w = w; this.h = h; this.sea = sea;
        cw = (x1 - x0) / w; ch = (z1 - z0) / h;
        d = new float[w * h]; dno = new float[w * h]; piasek = new float[w * h];
        for (int i = 0; i < piasek.Length; i++) piasek[i] = float.NaN;
    }

    // one triangle of the beach's sand (world)
    public void Piasek(Vector3 a, Vector3 b, Vector3 c)
    {
        trojkaty++;
        // the waterline: the sand's crossing of the sea level
        var p = new Vector3[] { a, b, c };
        float px0 = 0, pz0 = 0, px1 = 0, pz1 = 0; int n = 0;
        for (int e = 0; e < 3; e++)
        {
            Vector3 u = p[e], v = p[(e + 1) % 3];
            float hu = u.y - sea, hv = v.y - sea;
            if ((hu < 0f) == (hv < 0f)) continue;
            float s = hu / (hu - hv), x = u.x + (v.x - u.x) * s, z = u.z + (v.z - u.z) * s;
            if (n == 0) { px0 = x; pz0 = z; } else { px1 = x; pz1 = z; }
            n++;
        }
        if (n == 2) odcinki.Add(new Vector4(px0, pz0, px1, pz1));
        // the sand's height at the texel centres inside the triangle (the highest, if two cover one)
        float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
        if (Mathf.Abs(den) < 1e-9f) return;
        int ix0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - x0) / cw - .5f));
        int ix1 = Mathf.Min(w - 1, Mathf.CeilToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - x0) / cw - .5f));
        int iz0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - z0) / ch - .5f));
        int iz1 = Mathf.Min(h - 1, Mathf.CeilToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - z0) / ch - .5f));
        for (int iz = iz0; iz <= iz1; iz++)
            for (int ix = ix0; ix <= ix1; ix++)
            {
                float x = x0 + (ix + .5f) * cw, z = z0 + (iz + .5f) * ch;
                float l1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den;
                float l2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den;
                float l3 = 1f - l1 - l2;
                if (l1 < -1e-6f || l2 < -1e-6f || l3 < -1e-6f) continue;
                float y = l1 * a.y + l2 * b.y + l3 * c.y;
                int i = iz * w + ix;
                if (float.IsNaN(piasek[i])) komorekPiasku++;
                if (float.IsNaN(piasek[i]) || y > piasek[i]) piasek[i] = y;
            }
    }

    /// <summary>24.11 the waterline thinned to one point per krok x krok cell (world, at the sea level), chained along the
    /// shore (from its westmost point, each to the nearest one left): the beach's sounds play from it
    /// (BraxChampionsSzumMorza), a point's neighbours in the list are its neighbours on the shore.</summary>
    public List<Vector3> Brzeg(float krok)
    {
        var punkty = new List<Vector3>(); var zajete = new HashSet<long>();
        foreach (var o in odcinki)
        {
            float x = (o.x + o.z) * .5f, z = (o.y + o.w) * .5f;
            long key = ((long)Mathf.FloorToInt(x / krok) << 32) ^ (uint)Mathf.FloorToInt(z / krok);
            if (zajete.Add(key)) punkty.Add(new Vector3(x, sea, z));
        }
        var wynik = new List<Vector3>(punkty.Count);
        if (punkty.Count == 0) return wynik;
        int start = 0;
        for (int i = 1; i < punkty.Count; i++) if (punkty[i].x < punkty[start].x) start = i;
        var uzyty = new bool[punkty.Count]; int c = start;
        for (int n = 0; n < punkty.Count; n++)
        {
            uzyty[c] = true; wynik.Add(punkty[c]);
            int nast = -1; float best = float.MaxValue;
            for (int i = 0; i < punkty.Count; i++)
            {
                if (uzyty[i]) continue;
                float dx = punkty[i].x - punkty[c].x, dz = punkty[i].z - punkty[c].z, q = dx * dx + dz * dz;
                if (q < best) { best = q; nast = i; }
            }
            if (nast < 0) break;
            c = nast;
        }
        return wynik;
    }

    public void Licz()
    {
        int n = odcinki.Count;
        var seg = odcinki.ToArray();
        var model = new float[w * h];
        komorekLadu = 0;
        for (int iz = 0; iz < h; iz++)
        {
            float z = z0 + (iz + .5f) * ch;
            for (int ix = 0; ix < w; ix++)
            {
                float x = x0 + (ix + .5f) * cw, best = float.MaxValue;
                for (int s = 0; s < n; s++)
                {
                    var o = seg[s];
                    float abx = o.z - o.x, abz = o.w - o.y, L = abx * abx + abz * abz;
                    float t = L > 1e-12f ? Mathf.Clamp01(((x - o.x) * abx + (z - o.y) * abz) / L) : 0f;
                    float dx = x - (o.x + t * abx), dz = z - (o.y + t * abz), q = dx * dx + dz * dz;
                    if (q < best) best = q;
                }
                int i = iz * w + ix;
                float dist = n > 0 ? Mathf.Sqrt(best) : 1e4f;
                bool lad = !float.IsNaN(piasek[i]) && piasek[i] > sea;
                if (lad) komorekLadu++;
                d[i] = lad ? -dist : dist;
                float dd = Mathf.Max(d[i], 0f);
                model[i] = d[i] < 0f ? sea - d[i] * Spadek : sea - (Spadek * dd + Glebienie * dd * dd);
            }
        }
        // beyond the sand the model starts from the sand's own last height: the offset spread out a few texels, then faded
        var off = new float[w * h]; var wgt = new float[w * h];
        for (int i = 0; i < off.Length; i++) if (!float.IsNaN(piasek[i])) { off[i] = piasek[i] - model[i]; wgt[i] = 1f; }
        var o2 = new float[w * h]; var w2 = new float[w * h];
        for (int it = 0; it < 6; it++)
        {
            for (int iz = 0; iz < h; iz++)
                for (int ix = 0; ix < w; ix++)
                {
                    int i = iz * w + ix;
                    int l = iz * w + Math.Max(ix - 1, 0), r = iz * w + Math.Min(ix + 1, w - 1), dn = Math.Max(iz - 1, 0) * w + ix, up = Math.Min(iz + 1, h - 1) * w + ix;
                    o2[i] = (off[l] + off[r] + off[dn] + off[up] + off[i] * 4f) / 8f;
                    w2[i] = (wgt[l] + wgt[r] + wgt[dn] + wgt[up] + wgt[i] * 4f) / 8f;
                }
            var t1 = off; off = o2; o2 = t1; var t2 = wgt; wgt = w2; w2 = t2;
        }
        for (int i = 0; i < dno.Length; i++)
        {
            if (!float.IsNaN(piasek[i])) { dno[i] = piasek[i]; continue; }
            float k = wgt[i] > 1e-3f ? off[i] / wgt[i] * Mathf.Clamp01(wgt[i] * 3f) : 0f;
            dno[i] = model[i] + k;
        }
    }
}
