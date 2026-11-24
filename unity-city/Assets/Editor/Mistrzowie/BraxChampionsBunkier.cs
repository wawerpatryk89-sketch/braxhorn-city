using System;
using System.Collections.Generic;
using UnityEngine;
using T = BraxChampionsTorKladow;
using W = BraxWnetrzaKit;

/// <summary>
/// THE CLASSICS' BUNKER under the quad track's hill (owner 24.11.2026 23:09-23:13: "Musimy zrobić jakąś miejscówę dla tych
/// klasyków. Tak bardziej trochę ukrytą ... Albo w podziemiu"; his pick "Bunkier w górce przy torze", "ładny, oświetlony pod
/// ziemią, z wjazdem", the entrance "A: ukryty od lasu"). Hidden from the track and the paddock:
///   - a forest road (gravel, 5 m) from the freestyle zone's west edge along z 958 to the site's west fence, south inside it
///     (x -499, 5 m from the fence, 12 m from the woods' trail) and down into a cut in the hill's west flank;
///   - the cut: a concrete floor between concrete retaining walls that grow with the hill (their tops under the forest's
///     floor), ending at the portal's headwall, where the hill stands over 6 m above the floor;
///   - a tunnel (6 x 4.5 m) east under the hill into the hall: 52 x 30 m, 6 m high, its roof 1.5 m+ under the ground (the
///     woods' trail rides over it), lit (LED strips along its ceiling, lamps), steel beams across its span, the bays painted;
///   - the owner's classics in two rows facing the aisle (BraxChampionsKlasyki: the cars and trucks driveable out through the
///     tunnel, the motorcycles on their stands).
/// The track's ground (BraxChampionsTorKladow.Grunt) sinks under the cut's floor, the far land under it too; the forest keeps
/// off the road and the cut (BraxChampionsLasKladow.Wolne).
/// </summary>
public static class BraxChampionsBunkier
{
    static float K => BraxClaudeOsiedle.K;
    // the road's line (x, z): from the freestyle zone (FX0 -300) west along z 958, south inside the west fence (x -504),
    // into the cut; the portal's x found on the hill (where it stands PortalNad over the floor)
    static readonly Vector2[] Wezly = { new Vector2(-300f, 958f), new Vector2(-468f, 958f), new Vector2(-499f, 932f), new Vector2(-499f, 846f), new Vector2(-490f, 820f), new Vector2(-478f, 812.5f), new Vector2(-470f, 812f) };
    // the cut: its floor 6 m, the terrain's cells out over 5.5 m each side (BraxChampionsTorKladow.Teren), its walls' caps
    // to 7.4 m (over the cells' halves: 1.25 x sqrt 2), the portal's headwall 6.6 m deep over the tunnel's mouth
    public const float DrogaPol = 2.5f, WykopPol = 3f, Sciana = 2.5f, Czapa = 7.4f, Tunel = 6f, TunelH = 4.5f, PortalNad = 6.6f, Glowica = 6.6f;
    public const float HallX0 = -445f, HallX1 = -393f, HallZ0 = 797f, HallZ1 = 827f, HallH = 6f, Strop = .4f, OsZ = 812f;
    const float Krok = .5f;

    static List<Vector2> p; static List<float> s, y; static float sD, sF, podloga = float.NaN, portalX;
    static Dictionary<long, List<int>> siatka;
    public static float Podloga { get { Przygotuj(); return podloga; } }
    public static float PortalX { get { Przygotuj(); return portalX; } }

    static Vector2 CR(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return .5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t2 + (3f * b - 3f * c + d - a) * t3);
    }
    static long Klucz(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

    /// <summary>The line sampled every 0.5 m (a Catmull-Rom spline through the knots, then straight east from the last into
    /// the hill to the portal), the floor's height and the road's along it.</summary>
    static void Przygotuj()
    {
        if (p != null) return;
        var w = new List<Vector2>(Wezly);
        p = new List<Vector2>(); s = new List<float>();
        for (int i = 0; i + 1 < w.Count; i++)
        {
            Vector2 a = w[Mathf.Max(i - 1, 0)], b = w[i], c = w[i + 1], d = w[Mathf.Min(i + 2, w.Count - 1)];
            int n = Mathf.Max(2, Mathf.CeilToInt((c - b).magnitude / Krok));
            for (int k = 0; k < n; k++) p.Add(CR(a, b, c, d, k / (float)n));
        }
        p.Add(w[w.Count - 1]);
        int iD = 0; float best = 1e9f;
        for (int i = 0; i < p.Count; i++) { float dd = (p[i] - Wezly[3]).sqrMagnitude; if (dd < best) { best = dd; iD = i; } }
        // the floor: half a metre under the ground where the cut starts - lower if the hall would not lie 1.5 m under the
        // ground (the trail over it, its cuts); the portal where the hill first stands PortalNad over it
        float yD = T.Grunt(p[iD].x, p[iD].y, out _, false);
        float nad = float.PositiveInfinity;
        for (float x = HallX0 - 2f; x <= HallX1 + 2f; x += 2f)
            for (float z = HallZ0 - 2f; z <= HallZ1 + 2f; z += 2f) nad = Mathf.Min(nad, T.Grunt(x, z, out _, false));
        podloga = Mathf.Min(yD - .5f, nad - (HallH + Strop + 1.5f));
        var ost = Wezly[Wezly.Length - 1];
        portalX = HallX0 - Glowica - 4f;
        for (float x = ost.x; x < HallX0 - Glowica - 4f; x += .5f)
            if (T.Grunt(x, OsZ, out _, false) - podloga >= PortalNad) { portalX = x; break; }
        for (float x = ost.x + .5f; x <= portalX + Glowica + 1e-3f; x += Krok) p.Add(new Vector2(x, OsZ));
        float acc = 0f; s.Add(0f);
        for (int i = 1; i < p.Count; i++) { acc += (p[i] - p[i - 1]).magnitude; s.Add(acc); }
        sD = s[iD];
        int iF = p.FindIndex(q => q.y == OsZ && q.x >= portalX - 1e-3f); sF = iF >= 0 ? s[iF] : acc;
        // the road's height: the ground's own (smoothed over 10 m), down to the floor over its last stretch (at most 9 %)
        var nat = new float[p.Count];
        for (int i = 0; i < p.Count; i++) nat[i] = T.Grunt(p[i].x, p[i].y, out _, false);
        y = new List<float>(p.Count);
        float spad = Mathf.Max(15f, (nat[iD] - podloga) / .09f);
        for (int i = 0; i < p.Count; i++)
        {
            float sum = 0f; int c = 0;
            for (int k = Mathf.Max(0, i - 10); k <= Mathf.Min(p.Count - 1, i + 10); k++) { sum += nat[k]; c++; }
            float yy = s[i] >= sD ? podloga : Mathf.Lerp(sum / c, podloga, T.Smooth(sD - spad, sD, s[i]));
            y.Add(yy);
        }
        siatka = new Dictionary<long, List<int>>();
        for (int i = 0; i < p.Count; i++)
        {
            long k = Klucz(Mathf.FloorToInt(p[i].x / 8f), Mathf.FloorToInt(p[i].y / 8f));
            if (!siatka.TryGetValue(k, out var l)) siatka[k] = l = new List<int>();
            l.Add(i);
        }
    }

    /// <summary>The nearest point of the line: its length along (s), the signed offset across (left +), the road's height.</summary>
    public static bool Najblizszy(float x, float z, float zasieg, out float sw, out float off, out float yd)
    {
        sw = off = yd = 0f;
        if (x < -520f || x > -280f || z < 790f || z > 980f) return false;
        Przygotuj();
        int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f), r = Mathf.CeilToInt(zasieg / 8f); int bi = -1; float bd = zasieg * zasieg;
        for (int a = -r; a <= r; a++)
            for (int b = -r; b <= r; b++)
                if (siatka.TryGetValue(Klucz(cx + a, cz + b), out var l))
                    foreach (int i in l) { float dd = (p[i].x - x) * (p[i].x - x) + (p[i].y - z) * (p[i].y - z); if (dd < bd) { bd = dd; bi = i; } }
        if (bi < 0) return false;
        int j = Mathf.Min(bi, p.Count - 2); if (bi > 0 && j == bi) { var q0 = new Vector2(x, z); if ((q0 - p[bi - 1]).sqrMagnitude < (q0 - p[Mathf.Min(bi + 1, p.Count - 1)]).sqrMagnitude) j = bi - 1; }
        Vector2 A = p[j], B = p[j + 1], d = B - A; float L = d.magnitude; if (L < 1e-5f) return false;
        var u = d / L; var q = new Vector2(x, z) - A; float t = Mathf.Clamp(Vector2.Dot(q, u), 0f, L);
        sw = s[j] + t; off = u.x * q.y - u.y * q.x; yd = Mathf.Lerp(y[j], y[j + 1], t / L);
        return true;
    }

    /// <summary>A terrain cell whose centre is here is left out (BraxChampionsTorKladow.Teren): the cut's floor and walls and
    /// the portal's headwall stand in the hole, their caps over its edges.</summary>
    public static bool Dziura(float x, float z)
    {
        if (!Najblizszy(x, z, 10f, out float sw, out float off, out _)) return false;
        return sw >= sD && sw <= sF + Glowica - 2f && Mathf.Abs(off) < WykopPol + Sciana;
    }
    /// <summary>Round the bunker (its cut, tunnel and hall, 50 m more: the far land's cells are up to 44 m here) the far land
    /// goes under the floor - none of it ever crosses the tunnel or the hall. Not in the site's rim band.</summary>
    public static bool Strefa(float x, float z) => x > -505f && x < HallX1 + 50f && z > HallZ0 - 50f && z < 896f;

    /// <summary>The ground there for BraxChampionsTorKladow.Grunt along the forest road: NaN = the bunker leaves it; else the
    /// height (the road's bed 0.3 m under its gravel, the banks to the land 6 m out).</summary>
    public static float Grunt(float x, float z, float h)
    {
        if (!Najblizszy(x, z, DrogaPol + 8f, out float sw, out float off, out float yd)) return float.NaN;
        if (sw > sD) return float.NaN;
        float e = Mathf.Abs(off);
        if (e < DrogaPol + 1f + 6f) return Mathf.Lerp(yd - .3f, h, T.Smooth(0f, 6f, e - DrogaPol - 1f));
        return float.NaN;
    }
    /// <summary>The forest keeps off: the road (its verge 4 m), the cut and round the portal.</summary>
    public static bool Wolne(float x, float z)
    {
        if (!Najblizszy(x, z, 14f, out float sw, out float off, out _)) return false;
        return Mathf.Abs(off) < DrogaPol + 4f || (sw >= sD - 6f && Mathf.Abs(off) < Czapa + 4f);
    }

    // ---------------------------------------------------------------- the build
    static Material beton, betonWnetrze, posadzka, linie, sufit;
    static BraxChampionsMeshBatch napisy;
    static float tunelX;
    public static int Trojkaty, Klasykow;
    static Vector3 S(float x, float yy, float z) => new Vector3(x, yy, z) * K;

    public static void Zbuduj(Transform g)
    {
        Przygotuj();
        napisy = new BraxChampionsMeshBatch();
        beton = T.beton;
        betonWnetrze = BraxChampionsFoundation.SourceMaterial("bunkier_sciany", "concrete_floor_02", 2.4f, new Color(1.18f, 1.17f, 1.14f));
        posadzka = BraxChampionsFoundation.SourceMaterial("bunkier_posadzka", "concrete_floor_02", 1.6f, new Color(.80f, .82f, .86f));
        sufit = BraxChampionsFoundation.SourceMaterial("bunkier_strop", "concrete_floor_02", 3.0f, new Color(.62f, .62f, .62f));
        linie = BraxChampionsHarbour.Surface("bunkier_linie", new Color(.95f, .76f, .10f), .55f, 0f);
        foreach (var m in new[] { betonWnetrze, posadzka, sufit, linie }) BraxChampionsInteriorLight.Apply(m, .9f);
        var grupa = new GameObject("TOR DO KŁADÓW | BUNKIER KLASYKÓW (pod górką, wjazd od lasu)").transform; grupa.SetParent(g, false);
        Droga(grupa);
        bool wn = W.Wnetrze; W.Wnetrze = false;
        Wykop();
        W.Wnetrze = true; W.Otoczenie = 1f;
        Hala(grupa);
        W.Wnetrze = wn;
        Klasyki(grupa);
        BraxChampionsLetters3D.Build(napisy, grupa, "BUNKIER KLASYKÓW", K);
        float nad = float.PositiveInfinity;
        for (float x = HallX0; x <= HallX1; x += 2f) for (float z = HallZ0; z <= HallZ1; z += 2f) nad = Mathf.Min(nad, T.Grunt(x, z, out _, false));
        Debug.Log("MISTRZOWIE_BUNKIER floor=" + Podloga.ToString("F2") + " portalX=" + PortalX.ToString("F1") + " road=" + sD.ToString("F0") +
                  "m cut=" + (sF - sD).ToString("F0") + "m tunnel=" + (HallX0 - PortalX).ToString("F0") + "m hall=" + (HallX1 - HallX0) + "x" + (HallZ1 - HallZ0) +
                  "m ground over the hall >= " + (nad - Podloga - HallH - Strop).ToString("F1") + "m classics=" + Klasykow + " tri=" + Trojkaty);
        if (nad - Podloga - HallH - Strop < 1f) BraxBuildQuality.Warn("DO POPRAWKI BUNKIER: the hall's roof lies " + (nad - Podloga - HallH - Strop).ToString("F1") + " m under the ground");
    }

    // the forest road: gravel 5 m wide on its bed, its skirts down 0.45 m (the banks meet it 0.3 m under), to the cut's floor
    static void Droga(Transform g)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int iD = s.FindIndex(q => q >= sD);
        for (int i = 0; i <= iD; i++)
        {
            int a = Mathf.Max(i - 1, 0), b = Mathf.Min(i + 1, p.Count - 1);
            var d = (p[b] - p[a]).normalized; var l = new Vector2(-d.y, d.x);
            float ya = y[b], yb = y[a], dl = Mathf.Max(.01f, s[b] - s[a]);
            var nn = new Vector3(-d.x * (ya - yb) / dl, 1f, -d.y * (ya - yb) / dl).normalized;
            foreach (float o in new[] { -DrogaPol, DrogaPol }) { var q = p[i] + l * o; v.Add(S(q.x, y[i] + .02f, q.y)); n.Add(nn); uv.Add(new Vector2(o / 3f, s[i] / 3f)); }
            foreach (float o in new[] { -DrogaPol, DrogaPol }) { var q = p[i] + l * o; v.Add(S(q.x, y[i] - .45f, q.y)); n.Add(new Vector3(l.x, 0f, l.y) * Mathf.Sign(o)); uv.Add(new Vector2(o / 3f, s[i] / 3f - .15f)); }
        }
        var top = new List<int>(); var bok = new List<int>();
        for (int i = 0; i < iD; i++)
        {
            int a = i * 4, c = a + 4;
            top.AddRange(new[] { a, a + 1, c, c, a + 1, c + 1 });
            bok.AddRange(new[] { a, c, a + 2, c, c + 2, a + 2, a + 1, a + 3, c + 1, c + 1, a + 3, c + 3 });
        }
        // the top faces up (the winding checked on its first triangle); the skirts both ways (seen from the banks)
        var e0 = v[4] - v[0]; var e1 = v[1] - v[0];
        if (Vector3.Cross(e1, e0).y < 0f) for (int k = 0; k + 2 < top.Count; k += 3) { int x = top[k + 1]; top[k + 1] = top[k + 2]; top[k + 2] = x; }
        t.AddRange(top); t.AddRange(bok);
        for (int k = 0; k + 2 < bok.Count; k += 3) { t.Add(bok[k]); t.Add(bok[k + 2]); t.Add(bok[k + 1]); }
        T.Zapisz(g, "BUNKIER | leśna droga (szuter) | kolizja", "bunkier_droga", v.ToArray(), n.ToArray(), uv.ToArray(), t, T.szuter, true);
    }

    // the cut: its floor, its walls (inner faces up to the ground, a 0.4 m coping, the forest's floor on the cap behind -
    // 10 cm under the ground where the terrain's kept cells lie over it, the cap seen only in the hole), the portal's
    // headwall over the tunnel's mouth, its open steel gate
    static float Wierzch(Vector2 q) => Math.Max(T.Grunt(q.x, q.y), Podloga + .3f) - .1f;
    static void Wykop()
    {
        // to the headwall's face (0.4 m before the portal's line: the tunnel's floor starts there - no two floors on one plane)
        int i0 = Mathf.Max(0, s.FindIndex(q => q >= sD - 2f)), i1 = s.FindIndex(q => q >= sF - .4f); if (i1 < 0) i1 = p.Count - 1;
        tunelX = p[i1].x;                                                       // the tunnel's floor starts exactly there
        // each sample's own across (the mean of its two segments): the neighbouring pieces share their edges round the bend
        // (on the straight into the hill exactly across x: the floor's last edge meets the tunnel's edge to edge)
        float xProsta = Wezly[Wezly.Length - 1].x - 1e-3f;
        Vector2 Lw(int i) { if (p[i].x >= xProsta && Mathf.Abs(p[i].y - OsZ) < 1e-4f) return new Vector2(0f, 1f); var d0 = (p[Mathf.Min(i + 1, p.Count - 1)] - p[Mathf.Max(i - 1, 0)]).normalized; return new Vector2(-d0.y, d0.x); }
        for (int i = i0; i < i1; i++)
        {
            Vector2 a = p[i], b = p[i + 1], d = (b - a).normalized, la = Lw(i), lb = Lw(i + 1);
            if (i + 1 == i1) { b = new Vector2(tunelX, OsZ); lb = new Vector2(0f, 1f); }      // square on the tunnel's floor
            Quad(beton, a - la * WykopPol, a + la * WykopPol, b + lb * WykopPol, b - lb * WykopPol, Podloga, true);
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 wa = a + la * WykopPol * sg, wb = b + lb * WykopPol * sg, ka = a + la * (WykopPol + .4f) * sg, kb = b + lb * (WykopPol + .4f) * sg;
                Vector2 ca = a + la * Czapa * sg, cb = b + lb * Czapa * sg;
                float ta = Wierzch(wa) + .1f, tb = Wierzch(wb) + .1f;
                Sciana4(beton, wa, wb, Podloga, Podloga, ta, tb, sg > 0f);                // its face toward the road
                Gora(beton, wa, wb, kb, ka, ta, tb, tb, ta);
                Gora(T.sciolka, ka, kb, cb, ca, Wierzch(ka), Wierzch(kb), Wierzch(cb), Wierzch(ca));
                Sciana4(T.sciolka, ca, cb, Wierzch(ca) - 1.5f, Wierzch(cb) - 1.5f, Wierzch(ca), Wierzch(cb), sg < 0f);   // a skirt under the cap's edge, outward
                W.Kolizja(S((wa.x + wb.x + ca.x + cb.x) * .25f, (Podloga + Mathf.Max(ta, tb)) * .5f, (wa.y + wb.y + ca.y + cb.y) * .25f),
                          new Vector3(Czapa - WykopPol, Mathf.Max(ta, tb) - Podloga, (b - a).magnitude + .05f) * K, Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
            }
        }
        for (int i = i0; i < i1; i += 8)
        {
            int j = Mathf.Min(i + 8, i1); var a = p[i]; var b = p[j]; var d = b - a;
            if (d.sqrMagnitude < 1e-4f) continue;
            W.Kolizja(S((a.x + b.x) * .5f, Podloga - .15f, (a.y + b.y) * .5f), new Vector3(WykopPol * 2f, .3f, d.magnitude + .2f) * K, Quaternion.LookRotation(new Vector3(d.x, 0f, d.y)));
        }
        // the cut's start: the walls' ends, down to the floor
        {
            var a = p[i0]; var d = (p[i0 + 1] - a).normalized; var l = new Vector2(-d.y, d.x);
            foreach (float sg in new[] { -1f, 1f })
            {
                Vector2 w0 = a + l * WykopPol * sg, w1 = a + l * Czapa * sg;
                Sciana4(beton, w0, w1, Podloga - .3f, Podloga - .3f, Wierzch(w0) + .1f, Wierzch(w1), sg < 0f);   // facing the road coming in
            }
        }
        // the portal's headwall: over the tunnel's lintel to the ground, its sides down to the floor, Glowica deep
        float px = PortalX, x1 = px + Glowica, top = Math.Max(T.Grunt(px, OsZ), Podloga + TunelH + .6f) + .3f, hw = Czapa;
        W.MM(beton, (px - .4f) * K, x1 * K, (Podloga + TunelH + Strop) * K, top * K, (OsZ - Tunel * .5f - .5f) * K, (OsZ + Tunel * .5f + .5f) * K, true);
        foreach (float sg in new[] { -1f, 1f })
        {
            float z0 = OsZ + sg * (Tunel * .5f + .5f), z1 = OsZ + sg * hw;
            W.MM(beton, (px - .4f) * K, (px + .6f) * K, (Podloga - .3f) * K, top * K, Mathf.Min(z0, z1) * K, Mathf.Max(z0, z1) * K, true);
        }
        // its cap and the cut's caps round it: the forest's floor 10 cm under the ground
        Gora(T.sciolka, new Vector2(px + .6f, OsZ - hw), new Vector2(px + .6f, OsZ + hw), new Vector2(x1, OsZ + hw), new Vector2(x1, OsZ - hw),
             Wierzch(new Vector2(px + .6f, OsZ - hw)), Wierzch(new Vector2(px + .6f, OsZ + hw)), Wierzch(new Vector2(x1, OsZ + hw)), Wierzch(new Vector2(x1, OsZ - hw)));
        W.MM(beton, (px - .4f) * K, (px + .6f) * K, top * K, (top + .25f) * K, (OsZ - hw) * K, (OsZ + hw) * K);          // the coping
        // the steel gate's frame, open (its leaves folded back against the cut's walls)
        W.MM(T.stal, (px - .55f) * K, (px - .4f) * K, (Podloga) * K, (Podloga + TunelH) * K, (OsZ - Tunel * .5f - .2f) * K, (OsZ - Tunel * .5f) * K);
        W.MM(T.stal, (px - .55f) * K, (px - .4f) * K, (Podloga) * K, (Podloga + TunelH) * K, (OsZ + Tunel * .5f) * K, (OsZ + Tunel * .5f + .2f) * K);
        W.MM(T.stal, (px - .55f) * K, (px - .4f) * K, (Podloga + TunelH - .2f) * K, (Podloga + TunelH) * K, (OsZ - Tunel * .5f) * K, (OsZ + Tunel * .5f) * K);
        foreach (float sg in new[] { -1f, 1f })
            W.MM(T.stal, (px - 3.6f) * K, (px - .6f) * K, (Podloga + .1f) * K, (Podloga + TunelH - .3f) * K, (OsZ + sg * (WykopPol - .12f)) * K, (OsZ + sg * (WykopPol - .02f)) * K);
        // its name over the mouth: the region's 3D letters (yellow faces, black returns) on the headwall
        BraxChampionsLetters3D.Text(napisy, linie, T.czern, "BUNKIER KLASYKI", new Vector3(px - .4f, Podloga + TunelH + .45f, OsZ),
                                    BraxChampionsLetters3D.YawFacing(Vector3.left), .55f, .06f, .12f, Tunel + 2f);
    }

    // a quad on four points of the plan at its heights (the floor flat, a cap sloping); up = its face up
    static void Quad(Material m, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float h, bool up)
    {
        Vector3 A = S(a.x, h, a.y), B = S(b.x, h, b.y), C = S(c.x, h, c.y), D = S(d.x, h, d.y);
        if (Vector3.Cross(B - A, C - A).y < 0f == up) W.Quad(m, A, D, C, B); else W.Quad(m, A, B, C, D);
        Trojkaty += 2;
    }
    static void Gora(Material m, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float ha, float hb, float hc, float hd)
    {
        Vector3 A = S(a.x, ha, a.y), B = S(b.x, hb, b.y), C = S(c.x, hc, c.y), D = S(d.x, hd, d.y);
        if (Vector3.Cross(B - A, C - A).y < 0f) W.Quad(m, A, D, C, B); else W.Quad(m, A, B, C, D);
        Trojkaty += 2;
    }
    // a vertical face from a to b, bottom ya / yb, top ta / tb; its face toward the left of a->b unless prawa
    static void Sciana4(Material m, Vector2 a, Vector2 b, float ya, float yb, float ta, float tb, bool prawa)
    {
        Vector3 A = S(a.x, ya, a.y), B = S(b.x, yb, b.y), C = S(b.x, tb, b.y), D = S(a.x, ta, a.y);
        if (prawa) W.Quad(m, B, A, D, C); else W.Quad(m, A, B, C, D);
        Trojkaty += 2;
    }

    // the tunnel and the hall: a closed concrete shell (no sun inside: the kit's interior flag), lit
    static void Hala(Transform g)
    {
        // the tunnel: floor, walls, ceiling - from where the cut's floor ends to the hall's west wall's outer face (no two
        // faces on one plane: the cut's floor meets the tunnel's edge to edge, the wall over the mouth sits on its end)
        float f = Podloga, px = tunelX, hz = Tunel * .5f, sc = .5f, xk = HallX0 - sc;
        W.MM(posadzka, px * K, xk * K, (f - .3f) * K, f * K, (OsZ - hz) * K, (OsZ + hz) * K, true);
        W.MM(betonWnetrze, px * K, xk * K, f * K, (f + TunelH) * K, (OsZ - hz - sc) * K, (OsZ - hz) * K, true);
        W.MM(betonWnetrze, px * K, xk * K, f * K, (f + TunelH) * K, (OsZ + hz) * K, (OsZ + hz + sc) * K, true);
        W.MM(sufit, px * K, xk * K, (f + TunelH) * K, (f + TunelH + Strop) * K, (OsZ - hz - sc) * K, (OsZ + hz + sc) * K, true);
        for (float x = px + 2f; x < xk - 1f; x += 5f) W.MM(T.led, (x - 1.2f) * K, (x + 1.2f) * K, (f + TunelH - .06f) * K, (f + TunelH) * K, (OsZ - .12f) * K, (OsZ + .12f) * K);
        // the hall: floor, the four walls (the tunnel's mouth in the west one), the ceiling
        W.MM(posadzka, HallX0 * K, HallX1 * K, (f - .3f) * K, f * K, HallZ0 * K, HallZ1 * K, true);
        W.MM(betonWnetrze, (HallX0 - sc) * K, HallX0 * K, f * K, (f + HallH) * K, HallZ0 * K, (OsZ - hz) * K, true);
        W.MM(betonWnetrze, (HallX0 - sc) * K, HallX0 * K, f * K, (f + HallH) * K, (OsZ + hz) * K, HallZ1 * K, true);
        W.MM(betonWnetrze, (HallX0 - sc) * K, HallX0 * K, (f + TunelH) * K, (f + HallH) * K, (OsZ - hz) * K, (OsZ + hz) * K, true);
        W.MM(betonWnetrze, HallX1 * K, (HallX1 + sc) * K, f * K, (f + HallH) * K, (HallZ0 - sc) * K, (HallZ1 + sc) * K, true);
        W.MM(betonWnetrze, (HallX0 - sc) * K, (HallX1 + sc) * K, f * K, (f + HallH) * K, (HallZ0 - sc) * K, HallZ0 * K, true);
        W.MM(betonWnetrze, (HallX0 - sc) * K, (HallX1 + sc) * K, f * K, (f + HallH) * K, HallZ1 * K, (HallZ1 + sc) * K, true);
        W.MM(sufit, (HallX0 - sc) * K, (HallX1 + sc) * K, (f + HallH) * K, (f + HallH + Strop) * K, (HallZ0 - sc) * K, (HallZ1 + sc) * K, true);
        // steel beams across the span every 7 m, the LED strips along the hall between them (three rows), the lamps
        for (float x = HallX0 + 3.5f; x < HallX1; x += 7f)
            W.MM(T.stal, (x - .2f) * K, (x + .2f) * K, (f + HallH - .7f) * K, (f + HallH) * K, HallZ0 * K, HallZ1 * K);
        foreach (float z in new[] { HallZ0 + 6f, OsZ, HallZ1 - 6f })
            for (float x = HallX0 + 7f; x < HallX1 - 3f; x += 7f)
                W.MM(T.led, (x - 2.6f) * K, (x + 2.6f) * K, (f + HallH - .06f) * K, (f + HallH) * K, (z - .14f) * K, (z + .14f) * K);
        for (float x = HallX0 + 10f; x < HallX1; x += 20f)
            foreach (float z in new[] { HallZ0 + 8f, HallZ1 - 8f }) W.Lampa("bunkier lampa", S(x, f + HallH - 1f, z), 1.6f, 22f * K);
        W.Lampa("bunkier lampa tunelu", S((px + HallX0) * .5f, f + TunelH - .6f, OsZ), 1.2f, 14f * K);
        // the yellow lines: the aisle's edges and the bays (2 cm proud, no bottom: never on the floor's plane)
        foreach (float z in new[] { OsZ - 6f, OsZ + 6f })
            W.OBox(linie, S((HallX0 + HallX1) * .5f, f + .01f, z), new Vector3((HallX1 - HallX0 - 2f) * K, .02f * K, .12f * K), Quaternion.identity, false, true);
        // the name on the east wall, facing the tunnel
        BraxChampionsLetters3D.Text(napisy, linie, T.czern, "KLASYKI BRAXHORN", new Vector3(HallX1, f + HallH - 2.1f, OsZ),
                                    BraxChampionsLetters3D.YawFacing(Vector3.left), .8f, .08f, .12f, HallZ1 - HallZ0 - 6f);
    }

    // the classics: two rows facing the aisle (z 812 +- 6), their fronts on the aisle's lines; the trucks first, the motorcycles last
    static readonly string[] Poludnie = { "star_zielony", "star_pomaranczowy", "nysa_milicja", "milicja", "polonez_milicja", "bosna", "syrena", "maluch" };
    static readonly string[] Polnoc = { "fiat125p", "fiat125p_tuning", "polonez", "polonez_tuning", "junak", "jawa", "romet", "komar" };
    public static readonly List<Vector3> Miejsca = new List<Vector3>();
    static void Klasyki(Transform g)
    {
        Klasykow = 0; Miejsca.Clear();
        if (!BraxChampionsKlasyki.Jest) { Debug.Log("BRAXHORN_MIASTO_ASSETY bunkier: brak paczki " + BraxChampionsKlasyki.Paczka + " - hala bez klasyków"); return; }
        foreach (var (rzad, polnoc) in new[] { (Poludnie, false), (Polnoc, true) })
        {
            // the bays spread evenly along the row (a collection's hall: room round each), each its vehicle's width + 1.5 m
            var lista = new List<(string model, Vector2 wym, float szer)>();
            foreach (var model in rzad)
            {
                var wym = BraxChampionsKlasyki.Wymiar(model); if (wym.x <= 0f) continue;
                lista.Add((model, wym, wym.y + (BraxChampionsKlasyki.Typ(model) == "motor" ? 1.0f : 1.5f)));
            }
            float suma = 0f; foreach (var q in lista) suma += q.szer;
            float luz = lista.Count > 0 ? Mathf.Max(0f, (HallX1 - HallX0 - 4f - suma) / lista.Count) : 0f, x = HallX0 + 2f;
            if (suma > HallX1 - HallX0 - 4f) BraxBuildQuality.Warn("DO POPRAWKI BUNKIER: the row is " + suma.ToString("F1") + " m wide");
            foreach (var (model, wym, szer0) in lista)
            {
                float szer = szer0 + luz;
                float cx = x + szer * .5f; x += szer;
                // the front on the aisle's line: the south row faces north (+z), the north row south
                float cz = polnoc ? OsZ + 6f + wym.x * .5f : OsZ - 6f - wym.x * .5f;
                var o = BraxChampionsKlasyki.Postaw(g, model, new Vector3(cx, Podloga, cz), polnoc ? 180f : 0f);
                if (o) { Klasykow++; Miejsca.Add(new Vector3(cx, Podloga, cz)); }
                // the bay's lines either side (2 cm proud, no bottom)
                foreach (float bx in new[] { cx - szer * .5f, cx + szer * .5f })
                    W.OBox(linie, S(bx, Podloga + .01f, polnoc ? OsZ + 6f + 2.6f : OsZ - 6f - 2.6f), new Vector3(.1f * K, .02f * K, 5.2f * K), Quaternion.identity, false, true);
            }
        }
    }
}
