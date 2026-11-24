using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using W = BraxWnetrzaKit;

/// <summary>THE BRAXHORN QUAD TRACK (owner 24.11.2026 11:31-11:45: "tor do kładów gdzieś z boku ... jakiś zajebisty", "Zrób tam,
/// gdzie poleciłeś, zachód", "dobry zrobić ... jakieś górki, pagórki, wyskoki, coś fajnego", "budkę trzeba zrobić ładną.
/// Wypożyczanie z tych kładów ... minimum 4 kłady").
/// West of the city, north of the D2 railway hill (its ellipse ends at z ~587): a 1.7 km loop, 8 m wide (FIM motocross:
/// 1.5-2 km, at least 6 m of riding width, a start straight of 80-120 m with no jump, rolling waves 8-10 m apart and at most
/// 0.8 m high - FIM 65.200.03 Circuits), on the land's own lie (the far land's height under it, smoothed along the track).
/// Along it, in the riding order: the start straight from the gate past the paddock and the grandstand, the first right
/// hander, a short straight north, a left hander into the big TABLE-TOP and a DOUBLE, the long west straight down the HILL
/// (a 10 % climb, a steep drop) and through the WAVES, a small table, the south straight's WHOOPS, the STEP-UP / step-down,
/// another double, rollers, the FORD (a muddy dip with water), a small table, the east straight's waves and the last big
/// table before the paddock. Every turn a BERM (the outer edge raised, up to 13 degrees). The surface: Poly Haven Muddy
/// Tracks (tyre ruts along the riding line), its shoulders Brown Mud 02, the paddock and the road Gravel Ground 01 (none of
/// the owner's list is dirt - ZASADY 5.8: off the list only when nothing on it fits).
/// The paddock: the rental booth (WYPOŻYCZALNIA KŁADÓW BRAXHORN) with the six quads in their bays (BraxChampionsKlady), the
/// grandstand over the start straight, the start gate, the START / META arch, light masts; the road from the city's
/// north-west corner (the street mouth at x 60, z 935). The lap timer: BraxChampionsTorCzas. All in nominal metres x K.</summary>
public static class BraxChampionsTorKladow
{
    static float K => BraxClaudeOsiedle.K;
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/TorKladow";

    // ---------------------------------------------------------------- the site (nominal metres)
    public const float X0 = -530f, X1 = 0f, Z0 = 650f, Z1 = 1050f;
    public const float Szer = 8f, Pobocze = 1.2f, RimBand = 25f, RimDip = 1.5f, FenceInset = 26f, PlotWschodX = -8f;
    public const float PX0 = -160f, PX1 = -30f, PZ0 = 936f, PZ1 = 1008f;              // the paddock (inside the fence: z 1024)
    public const float DrogaZ = 942f, DrogaX0 = -30f, DrogaX1 = 50f, DrogaPol = 3.5f, DrogaMiasto = .33f;   // the road east to the city's sidewalk (x 53)
    public const float MetaX = -130f, BramkaX = -72f;                                    // the finish line, the start gate
    // the loop's corners in the riding order (x, z, fillet radius): the start straight runs west along z 915 into corner 0
    public static readonly Vector3[] Rogi =
    {
        new Vector3(-280f, 915f, 18f), new Vector3(-280f, 1010f, 18f), new Vector3(-490f, 1010f, 22f), new Vector3(-490f, 690f, 24f),
        new Vector3(-360f, 690f, 18f), new Vector3(-360f, 840f, 16f), new Vector3(-260f, 860f, 16f), new Vector3(-250f, 760f, 16f),
        new Vector3(-170f, 700f, 20f), new Vector3(-60f, 700f, 20f), new Vector3(-40f, 915f, 20f),
    };

    // ---------------------------------------------------------------- the obstacles
    public enum Typ { Stol, Podwojna, Schodek, Fale, Muldy, Gorka, Brod }
    public struct Przeszkoda
    {
        public int prosta; public float od; public Typ typ; public float h; public int n;
        public float s0, dl;                                       // set from the centre line: its start along the loop, its length
        public Przeszkoda(int prosta, float od, Typ typ, float h, int n = 0) { this.prosta = prosta; this.od = od; this.typ = typ; this.h = h; this.n = n; s0 = 0f; dl = 0f; }
    }
    // the straight i runs from corner i to corner i+1; od = metres from the straight's start
    public static readonly Przeszkoda[] Przeszkody =
    {
        new Przeszkoda(1, 28f, Typ.Stol, 2.4f),          // the big table-top: 8 m face (~19 deg to the lip), 16 m top, 11 m landing
        new Przeszkoda(1, 100f, Typ.Podwojna, 2.0f),     // a double: 15 m between the faces
        new Przeszkoda(2, 18f, Typ.Gorka, 4.5f),         // the hill: 40 m at 11 %, 8 m crest, an 18 m drop (25 %)
        new Przeszkoda(2, 118f, Typ.Fale, .7f, 7),       // the waves: 7 x 9 m, 0.7 m (FIM: 8-10 m apart, <= 0.8 m)
        new Przeszkoda(2, 205f, Typ.Stol, 1.3f),         // a small table
        new Przeszkoda(3, 18f, Typ.Muldy, .45f, 12),     // the whoops: 12 x 4.5 m, 0.45 m
        new Przeszkoda(4, 22f, Typ.Schodek, 1.5f),       // step up 1.5 m, 22 m plateau, step down onto a 9 m landing
        new Przeszkoda(5, 14f, Typ.Podwojna, 1.5f),      // another double
        new Przeszkoda(6, 18f, Typ.Fale, .6f, 4),        // rollers
        new Przeszkoda(7, 30f, Typ.Brod, .8f),           // the ford: an 18 m dip, 0.8 m, water 0.42 m deep
        new Przeszkoda(8, 26f, Typ.Stol, 1.3f),          // a small table
        new Przeszkoda(9, 18f, Typ.Fale, .7f, 4),        // waves
        new Przeszkoda(9, 95f, Typ.Stol, 2.4f),          // the last big table before the paddock
    };
    public const float WodaGlebokosc = .42f;

    // ---------------------------------------------------------------- the centre line: fillets, sampled every 0.5 m
    public sealed class Os
    {
        public float[] x, z, tx, tz, kr, s; public float dlugosc;
        public float[] prostaOd, prostaDo;                           // each straight's s range
        public int N => x.Length;
    }
    static Os os;
    static Przeszkoda[] ulozone;
    /// <summary>The obstacles placed on the loop (their s0 and length).</summary>
    public static Przeszkoda[] Ulozone { get { Linia(); return ulozone; } }
    public static Os Linia()
    {
        if (os != null) return os;
        int n = Rogi.Length;
        var px = new List<float>(); var pz = new List<float>(); var pk = new List<float>();
        var prostaOd = new float[n]; var prostaDo = new float[n];
        // tangent points of each corner's fillet
        var t1 = new Vector2[n]; var t2 = new Vector2[n]; var ctr = new Vector2[n]; var a0 = new float[n]; var da = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 a = P(Rogi[(i + n - 1) % n]), b = P(Rogi[i]), c = P(Rogi[(i + 1) % n]); float R = Rogi[i].z;
            var d1 = (b - a).normalized; var d2 = (c - b).normalized;
            float ang = (float)Math.Acos(Mathf.Clamp(Vector2.Dot(d1, d2), -1f, 1f));
            float t = R * Mathf.Tan(ang * .5f);
            t1[i] = b - d1 * t; t2[i] = b + d2 * t;
            float cross = d1.x * d2.y - d1.y * d2.x;
            var nrm = new Vector2(-d1.y, d1.x) * Mathf.Sign(cross);
            ctr[i] = t1[i] + nrm * R;
            a0[i] = Mathf.Atan2(t1[i].y - ctr[i].y, t1[i].x - ctr[i].x);
            float a1 = Mathf.Atan2(t2[i].y - ctr[i].y, t2[i].x - ctr[i].x);
            da[i] = Mathf.DeltaAngle(a0[i] * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        }
        float s = 0f; const float krok = .5f;
        void Dodaj(Vector2 p, float k) { px.Add(p.x); pz.Add(p.y); pk.Add(k); }
        for (int i = 0; i < n; i++)
        {
            // the arc of corner i (curvature + for a left turn: x east, z north, left = counter-clockwise)
            float R = Rogi[i].z, len = Mathf.Abs(da[i]) * R; int m = Mathf.Max(2, Mathf.CeilToInt(len / krok));
            for (int k = 0; k < m; k++) { float a = a0[i] + da[i] * k / m; Dodaj(ctr[i] + R * new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Mathf.Sign(da[i]) / R); }
            s += len;
            // the straight to corner i+1
            var b = t2[i]; var c = t1[(i + 1) % n]; float L = Vector2.Distance(b, c); int q = Mathf.Max(1, Mathf.CeilToInt(L / krok));
            prostaOd[i] = s; prostaDo[i] = s + L;
            for (int k = 0; k < q; k++) Dodaj(Vector2.Lerp(b, c, k / (float)q), 0f);
            s += L;
        }
        var o = new Os { x = px.ToArray(), z = pz.ToArray(), kr = pk.ToArray(), prostaOd = prostaOd, prostaDo = prostaDo };
        int N = o.x.Length; o.s = new float[N]; o.tx = new float[N]; o.tz = new float[N];
        for (int i = 1; i < N; i++) o.s[i] = o.s[i - 1] + Mathf.Sqrt((o.x[i] - o.x[i - 1]) * (o.x[i] - o.x[i - 1]) + (o.z[i] - o.z[i - 1]) * (o.z[i] - o.z[i - 1]));
        o.dlugosc = o.s[N - 1] + Mathf.Sqrt((o.x[0] - o.x[N - 1]) * (o.x[0] - o.x[N - 1]) + (o.z[0] - o.z[N - 1]) * (o.z[0] - o.z[N - 1]));
        for (int i = 0; i < N; i++)
        {
            int a = (i + N - 1) % N, b = (i + 1) % N; float dx = o.x[b] - o.x[a], dz = o.z[b] - o.z[a], l = Mathf.Sqrt(dx * dx + dz * dz);
            o.tx[i] = dx / l; o.tz[i] = dz / l;
        }
        // the smoothed curvature (the berms rise over ~10 m before a turn and fall after it)
        var sk = new float[N]; int w = 20;
        for (int i = 0; i < N; i++) { float sum = 0f; for (int k = -w; k <= w; k++) sum += o.kr[(i + k + N) % N]; sk[i] = sum / (2 * w + 1); }
        o.kr = sk;
        // the straights' s from the arcs' lengths (recomputed on the sampled line)
        for (int i = 0; i < n; i++) { o.prostaOd[i] = Mathf.Min(prostaOd[i], o.dlugosc); o.prostaDo[i] = Mathf.Min(prostaDo[i], o.dlugosc); }
        os = o;
        ulozone = new Przeszkoda[Przeszkody.Length];
        for (int i = 0; i < Przeszkody.Length; i++)
        {
            var p = Przeszkody[i]; p.s0 = o.prostaOd[p.prosta] + p.od; p.dl = Dlugosc(p);
            if (p.s0 + p.dl > o.prostaDo[p.prosta] - 6f) throw new InvalidOperationException("quad track: obstacle " + i + " runs into a turn");
            ulozone[i] = p;
        }
        return os;
    }
    static Vector2 P(Vector3 r) => new Vector2(r.x, r.y);

    public static float Dlugosc(Przeszkoda p)
    {
        switch (p.typ)
        {
            case Typ.Stol: return p.h >= 1.8f ? 8f + 16f + 11f : 6f + 9f + 8f;
            case Typ.Podwojna: return 6f + .4f + 5f + 4f + 5f + .4f + 8f;
            case Typ.Schodek: return 4.5f + 22f + 9f;
            case Typ.Fale: return p.n * 9f;
            case Typ.Muldy: return p.n * 4.5f;
            case Typ.Gorka: return 40f + 8f + 18f;
            case Typ.Brod: return 18f;
        }
        return 0f;
    }
    // a take-off face: a rounded toe, then straight to a sharp lip (a real jump's kick)
    static float Rampa(float x) { const float toe = .3f; x = Mathf.Clamp01(x); float y = x < toe ? x * x / (2f * toe) : x - toe * .5f; return y / (1f - toe * .5f); }
    // a landing / a back face: smooth at both ends
    static float Esa(float x) { x = Mathf.Clamp01(x); return x * x * x * (x * (x * 6f - 15f) + 10f); }
    static float Jeden(Przeszkoda p, float t)
    {
        if (t <= 0f || t >= p.dl) return 0f;
        switch (p.typ)
        {
            case Typ.Stol:
            {
                bool duzy = p.h >= 1.8f; float up = duzy ? 8f : 6f, top = duzy ? 16f : 9f, dn = duzy ? 11f : 8f;
                if (t < up) return p.h * Rampa(t / up);
                if (t < up + top) return p.h;
                return p.h * (1f - Esa((t - up - top) / dn));
            }
            case Typ.Podwojna:
            {
                // the take-off (6 m, ~18 deg to its lip), its back (5 m), a 4 m gap, the second's face (5 m), the landing (8 m, ~22 deg)
                float a = 6f, b = a + .4f, c = b + 5f, d = c + 4f, e = d + 5f, f = e + .4f;
                if (t < a) return p.h * Rampa(t / a);
                if (t < b) return p.h;
                if (t < c) return p.h * (1f - Esa((t - b) / 5f));
                if (t < d) return 0f;
                if (t < e) return p.h * Esa((t - d) / 5f);
                if (t < f) return p.h;
                return p.h * (1f - Esa((t - f) / 8f));
            }
            case Typ.Schodek:
                if (t < 4.5f) return p.h * Rampa(t / 4.5f);
                if (t < 26.5f) return p.h;
                return p.h * (1f - Esa((t - 26.5f) / 9f));
            case Typ.Fale: return p.h * .5f * (1f - Mathf.Cos(2f * Mathf.PI * t / 9f));
            case Typ.Muldy: return p.h * .5f * (1f - Mathf.Cos(2f * Mathf.PI * t / 4.5f));
            case Typ.Gorka:
                if (t < 40f) return p.h * Esa(t / 40f);
                if (t < 48f) return p.h;
                return p.h * (1f - Esa((t - 48f) / 18f));
            case Typ.Brod: return -p.h * .5f * (1f - Mathf.Cos(2f * Mathf.PI * t / 18f));
        }
        return 0f;
    }
    /// <summary>The obstacles' height over the track's base at s.</summary>
    public static float Profil(float s)
    {
        Linia(); float h = 0f;
        foreach (var p in ulozone) h += Jeden(p, s - p.s0);
        return h;
    }

    // ---------------------------------------------------------------- heights
    static float[] baza;                                            // the base along the line: the land's lie, smoothed (80 m)
    static float paddockY = float.NaN;
    public static float PaddockY { get { if (float.IsNaN(paddockY)) paddockY = BraxChampionsHorizon.FarHeight((PX0 + PX1) * .5f, (PZ0 + PZ1) * .5f) + .05f; return paddockY; } }
    static float Baza(int i)
    {
        if (baza != null) return baza[i];
        var o = Linia(); int N = o.N; var raw = new float[N];
        for (int k = 0; k < N; k++) raw[k] = BraxChampionsHorizon.FarHeight(o.x[k], o.z[k]);
        // the start straight in front of the paddock at the paddock's own level
        for (int k = 0; k < N; k++) if (o.z[k] > 905f && o.x[k] > -260f && o.x[k] < -40f) raw[k] = PaddockY - .1f;
        baza = new float[N]; int w = 160;                            // +-80 m (0.5 m samples), a cosine window
        for (int k = 0; k < N; k++)
        {
            float sum = 0f, ws = 0f;
            for (int j = -w; j <= w; j++) { float q = .5f * (1f + Mathf.Cos(Mathf.PI * j / w)); sum += raw[(k + j + N) % N] * q; ws += q; }
            baza[k] = sum / ws;
        }
        return baza[i];
    }
    static int Indeks(float s) { var o = Linia(); s = ((s % o.dlugosc) + o.dlugosc) % o.dlugosc; int lo = 0, hi = o.N - 1; while (lo < hi) { int m = (lo + hi + 1) / 2; if (o.s[m] <= s) lo = m; else hi = m - 1; } return lo; }
    /// <summary>The berm: tan of the bank at sample i (+ the outer edge is to the right for a left turn).</summary>
    static float Przechyl(int i) { var o = Linia(); float k = o.kr[i]; return Mathf.Sign(k) * Mathf.Tan(13f * Mathf.Deg2Rad) * Mathf.Clamp01(Mathf.Abs(k) * 18f); }
    /// <summary>The riding surface at sample i, u metres to the left of the centre line (the shoulders continue it).</summary>
    public static float Tor(int i, float u)
    {
        var o = Linia(); float b = Przechyl(i);
        return Baza(i) + Profil(o.s[i]) + Mathf.Abs(b) * (Szer * .5f - Mathf.Sign(b) * u);
    }

    // the nearest centre-line sample (a 6 m grid of the samples)
    static Dictionary<long, List<int>> siatka;
    static long Klucz(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;
    public static bool Najblizszy(float x, float z, float zasieg, out int idx, out float u)
    {
        var o = Linia();
        if (siatka == null)
        {
            siatka = new Dictionary<long, List<int>>();
            for (int i = 0; i < o.N; i++) { long kk = Klucz(Mathf.FloorToInt(o.x[i] / 6f), Mathf.FloorToInt(o.z[i] / 6f)); if (!siatka.TryGetValue(kk, out var l)) siatka[kk] = l = new List<int>(); l.Add(i); }
        }
        idx = -1; u = 0f; float bd = zasieg * zasieg; int r = Mathf.CeilToInt(zasieg / 6f), cx0 = Mathf.FloorToInt(x / 6f), cz0 = Mathf.FloorToInt(z / 6f);
        for (int a = -r; a <= r; a++)
            for (int b = -r; b <= r; b++)
            {
                if (!siatka.TryGetValue(Klucz(cx0 + a, cz0 + b), out var l)) continue;
                foreach (int i in l) { float dx = x - o.x[i], dz = z - o.z[i], d = dx * dx + dz * dz; if (d < bd) { bd = d; idx = i; } }
            }
        if (idx < 0) return false;
        u = (x - o.x[idx]) * -o.tz[idx] + (z - o.z[idx]) * o.tx[idx];           // the left normal (-tz, tx)
        return true;
    }
    static float Smooth(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }
    static float DoKrawedzi(float x, float z) => Mathf.Min(Mathf.Min(x - X0, X1 - x), Mathf.Min(z - Z0, Z1 - z));
    public static bool WTorze(float x, float z) => x > X0 && x < X1 && z > Z0 && z < Z1;
    static float DoPaddocku(float x, float z) { float dx = Mathf.Max(Mathf.Max(PX0 - x, x - PX1), 0f), dz = Mathf.Max(Mathf.Max(PZ0 - z, z - PZ1), 0f); return Mathf.Sqrt(dx * dx + dz * dz); }
    /// <summary>The road: its height at x (the region's grass 0.21 + 6 cm at the city's end, the paddock at the other), and the
    /// distance across it.</summary>
    public static float Droga(float x, float z, out float od)
    {
        od = x > DrogaX1 ? Mathf.Sqrt((x - DrogaX1) * (x - DrogaX1) + (z - DrogaZ) * (z - DrogaZ)) : x < DrogaX0 ? 99f : Mathf.Abs(z - DrogaZ);
        // 12 cm over the city's grass (0.21) on its side, up to the paddock over the 30 m to it (~6 %)
        return Mathf.Lerp(DrogaMiasto, PaddockY, Smooth(0f, -DrogaX0, -x));
    }

    /// <summary>The site's ground at (x, z), nominal: the land's own lie, the paddock flat, the road's bed, the track's
    /// embankments and cuts (1:1.6 fill, 1:1 cut), under the riding surface 0.35 m down, at the rim 1.5 m under the far land.</summary>
    public static float Grunt(float x, float z) => Grunt(x, z, out _);
    /// <summary>The ground, and how far it is from the land's own lie there (the fill and the cut of the track: bare earth).</summary>
    public static float Grunt(float x, float z, out float ziemia)
    {
        float h = BraxChampionsHorizon.FarHeight(x, z);
        ziemia = -1f;
        float dp = DoPaddocku(x, z);
        if (dp < 14f) h = Mathf.Lerp(PaddockY - .3f, h, Smooth(0f, 14f, dp));
        float y = Droga(x, z, out float dr);
        if (dr < DrogaPol + 1f + 8f) h = Mathf.Lerp(y - .3f, h, Smooth(0f, 8f, dr - DrogaPol - 1f));
        // at the city's edge (x 0) down to its grass, where the far land is pressed (along the road): no step, no gap
        if (dr <= 35f && x > -15f) h = Mathf.Lerp(.19f, h, Smooth(0f, 15f, -x));
        if (Najblizszy(x, z, 26f, out int i, out float u))
        {
            float e = Mathf.Abs(u), brzeg = Szer * .5f + Pobocze, nat = h;
            if (e <= brzeg) h = Tor(i, u) - .35f;
            else
            {
                // the fill 1:1.25 (a jump's, a berm's side), the cut 1:1 (the ford's banks): built of earth, so they read as jumps
                float he = Tor(i, Mathf.Sign(u) * brzeg) - .1f, dd = e - brzeg;
                h = he + Mathf.Clamp(h - he, -dd / 1.25f, dd / 1.0f);
            }
            ziemia = Mathf.Max(Mathf.Abs(h - nat) - .2f, brzeg + 1.5f - e);
        }
        if (dr > 35f) h -= RimDip * (1f - Smooth(0f, RimBand, DoKrawedzi(x, z)));   // under the far land at the rim (not at the road)
        return h;
    }
    /// <summary>For BraxChampionsHorizon.Height: inside the site the far land goes 1 m under its ground (not in the rim band,
    /// except along the road).</summary>
    public static float Docisk(float x, float z)
    {
        if (!WTorze(x, z)) return float.PositiveInfinity;
        Droga(x, z, out float dr);
        if (DoKrawedzi(x, z) < RimBand && dr > 35f) return float.PositiveInfinity;
        return Grunt(x, z) - 1f;
    }

    // ---------------------------------------------------------------- materials
    static Material nawierzchnia, pobocze, trawa, szuter, woda, beton, drewno, stal, czern, zloto, biel, pomarancz, czerwien, szyba, led, siedzisko;
    static void Materialy()
    {
        nawierzchnia = BraxChampionsFoundation.SourceMaterial("tor_kladow_koleiny", "muddy_tracks", 2.25f, new Color(.96f, .9f, .86f));
        pobocze = BraxChampionsFoundation.SourceMaterial("tor_kladow_pobocze", "brown_mud_02", 1.3f, new Color(1.12f, 1.0f, .86f));   // warm packed earth
        szuter = BraxChampionsFoundation.SourceMaterial("tor_kladow_szuter", "gravel_ground_01", 3.0f, Color.white);
        trawa = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/grass.mat");
        if (!trawa) trawa = BraxChampionsFoundation.SourceMaterial("tor_kladow_trawa", "sparse_grass", 2.0f, Color.white);
        beton = BraxChampionsFoundation.SourceMaterial("tor_kladow_beton", "concrete_floor_02", 2.0f, new Color(1.05f, 1.04f, 1.02f));
        drewno = BraxChampionsFoundation.SourceMaterial("tor_kladow_budka_drewno", "dark_paneled_wood", 1.2f, Color.white);
        woda = BraxChampionsHarbour.Surface("tor_kladow_brod_woda", new Color(.20f, .19f, .13f, .86f), .03f, 0f, null, true);   // muddy water
        stal = BraxChampionsHarbour.Surface("tor_kladow_stal", new Color(.55f, .57f, .60f), .35f, .85f);
        czern = BraxChampionsHarbour.Surface("tor_kladow_czern", new Color(.05f, .05f, .055f), .45f, .3f);
        zloto = BraxChampionsHarbour.Surface("tor_kladow_zloto", new Color(.93f, .74f, .34f), .3f, .9f);
        biel = BraxChampionsHarbour.Surface("tor_kladow_biel", new Color(.92f, .92f, .90f), .6f, 0f);
        pomarancz = BraxChampionsHarbour.Surface("tor_kladow_pomarancz", new Color(1f, .42f, .04f), .55f, 0f);
        czerwien = BraxChampionsHarbour.Surface("tor_kladow_czerwien", new Color(.78f, .08f, .06f), .55f, 0f);
        szyba = BraxChampionsHarbour.Surface("tor_kladow_szyba", new Color(.10f, .13f, .16f, .55f), .05f, .1f, null, true);
        led = BraxChampionsHarbour.Surface("tor_kladow_led", new Color(1f, .96f, .88f), .4f, 0f, new Color(2.2f, 2.05f, 1.8f));
        siedzisko = BraxChampionsHarbour.Surface("tor_kladow_siedzisko", new Color(.10f, .12f, .14f), .5f, 0f);
    }

    // ---------------------------------------------------------------- the build
    public static int Trojkaty, Przeszkod, Kolizje, Klady;
    public static float Dlugosc() => Linia().dlugosc;
    /// <summary>The whole site under parent (or the scene's root); after the villas, before the far land (it joins the press).</summary>
    public static Transform Zbuduj(Transform parent)
    {
        Materialy();
        Directory.CreateDirectory(Folder);
        var o = Linia();
        var g = new GameObject("MIASTO MISTRZÓW | TOR DO KŁADÓW BRAXHORN | zachód (owner 24.11)").transform;
        g.SetParent(parent, false); g.position = Vector3.zero;
        int k0 = W.Kolizje; W.Start(g); W.Grupa = g; W.Wnetrze = false;
        Trojkaty = 0;
        Teren(g);
        Wstega(g);
        Woda(g);
        DrogaIPaddock(g);
        var b = new BraxChampionsMeshBatch();
        Tyczki(b);
        Plot(b);
        var czas = Paddock(g, b);
        b.Build(g, "TOR DO KŁADÓW | detale", Folder, true, K);
        Trojkaty += b.Triangles;
        W.Zapisz("mistrzowie_tor_kladow");
        Kolizje = W.Kolizje - k0;
        // the far land goes under the site too (with the villas' press)
        var poprzedni = BraxChampionsHorizon.Docisk;
        BraxChampionsHorizon.Docisk = (x, z) => Mathf.Min(poprzedni != null ? poprzedni(x, z) : float.PositiveInfinity, Docisk(x, z));
        Przeszkod = Przeszkody.Length;
        Debug.Log("MISTRZOWIE_TOR_KLADOW length=" + o.dlugosc.ToString("F0") + "m width=" + Szer + "m obstacles=" + Przeszkod + " quads=" + Klady +
                  " triangles=" + Trojkaty + " colliders=" + Kolizje + " paddockY=" + PaddockY.ToString("F2") + " timer=" + (czas ? czas.punkty.Length : 0));
        return g;
    }

    // the ground: a 2.5 m grid over the site (cells wholly under the riding surface left out), its own collider
    static void Teren(Transform g)
    {
        const float krok = 2.5f;
        int nx = Mathf.CeilToInt((X1 - X0) / krok), nz = Mathf.CeilToInt((Z1 - Z0) / krok);
        var v = new Vector3[(nx + 1) * (nz + 1)]; var uv = new Vector2[v.Length]; var pod = new bool[v.Length]; var pole = new float[v.Length];
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float x = Mathf.Min(X0 + i * krok, X1), z = Mathf.Min(Z0 + j * krok, Z1); int q = i * (nz + 1) + j;
                float h = Grunt(x, z, out pole[q]); v[q] = new Vector3(x, h, z) * K; uv[q] = new Vector2(x, z);
                pod[q] = (Najblizszy(x, z, 8f, out _, out float u) && Mathf.Abs(u) < Szer * .5f - .5f) || (x > PX0 + .5f && x < PX1 - .5f && z > PZ0 + .5f && z < PZ1 - .5f);
            }
        // grass and bare earth (the track's fills, cuts and its edge) over the same vertices, the line between them cut exactly
        // where the field crosses zero (as the villas' grass / sand), the cut points shared: one closed surface
        var vl = new List<Vector3>(v); var uvl = new List<Vector2>(uv); var pl = new List<float>(pole);
        var tg = new List<int>(); var tz = new List<int>(); var ciecia = new Dictionary<long, int>();
        int Ciecie(int p, int q)
        {
            long key = p < q ? (long)p * 4000000L + q : (long)q * 4000000L + p;
            if (ciecia.TryGetValue(key, out int r)) return r;
            float t = Mathf.Clamp01(pl[p] / (pl[p] - pl[q]));
            vl.Add(Vector3.Lerp(vl[p], vl[q], t)); uvl.Add(Vector2.Lerp(uvl[p], uvl[q], t)); pl.Add(0f);
            return ciecia[key] = vl.Count - 1;
        }
        void Trojkat(int p0, int p1, int p2)
        {
            bool g0 = pl[p0] < 0f, g1 = pl[p1] < 0f, g2 = pl[p2] < 0f;
            if (g0 == g1 && g1 == g2) { var l = g0 ? tg : tz; l.Add(p0); l.Add(p1); l.Add(p2); return; }
            int A, B, C; bool gA;
            if (g0 != g1 && g0 != g2) { A = p0; B = p1; C = p2; gA = g0; }
            else if (g1 != g0 && g1 != g2) { A = p1; B = p2; C = p0; gA = g1; }
            else { A = p2; B = p0; C = p1; gA = g2; }
            int ab = Ciecie(A, B), ac = Ciecie(A, C);
            var la = gA ? tg : tz; var lo = gA ? tz : tg;
            la.Add(A); la.Add(ab); la.Add(ac);
            lo.Add(ab); lo.Add(B); lo.Add(C); lo.Add(ab); lo.Add(C); lo.Add(ac);
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                int a = i * (nz + 1) + j, bq = (i + 1) * (nz + 1) + j, c = a + 1, e = bq + 1;
                if (pod[a] && pod[bq] && pod[c] && pod[e]) continue;
                Trojkat(a, c, bq); Trojkat(c, e, bq);
            }
        var nrm = new Vector3[vl.Count];
        for (int q = 0; q < vl.Count; q++)
        {
            float x = vl[q].x / K, z = vl[q].z / K, ep = 1.25f;
            float gx = (Grunt(x + ep, z) - Grunt(x - ep, z)) / (2f * ep), gzz = (Grunt(x, z + ep) - Grunt(x, z - ep)) / (2f * ep);
            nrm[q] = new Vector3(-gx, 1f, -gzz).normalized;
        }
        var va = vl.ToArray(); var ua = uvl.ToArray();
        Zapisz(g, "TOR DO KŁADÓW | grunt: trawa | kolizja", "tor_kladow_grunt_trawa", va, nrm, ua, tg, trawa, true);
        Zapisz(g, "TOR DO KŁADÓW | grunt: ziemia (nasypy, przekopy) | kolizja", "tor_kladow_grunt_ziemia", va, nrm, ua, tz, pobocze, true);
    }

    // the riding surface and its shoulders: a ribbon along the line (0.5 m steps in the obstacles, 1 m in the turns, 2 m on
    // the plain straights), u across in 1 m; UVs along the track (the ruts run with the riding), a skirt under each edge
    static void Wstega(Transform g)
    {
        var o = Linia();
        float[] us = { Szer * .5f + Pobocze, Szer * .5f, 3f, 2f, 1f, 0f, -1f, -2f, -3f, -Szer * .5f, -Szer * .5f - Pobocze };
        var rzedy = new List<int>(); float ostatni = -99f;
        for (int i = 0; i < o.N; i++)
        {
            float s = o.s[i]; bool w = false;
            foreach (var p in ulozone) if (s > p.s0 - 2f && s < p.s0 + p.dl + 2f) { w = true; break; }
            float krok = w ? .5f : Mathf.Abs(o.kr[i]) > .004f ? 1f : 2f;
            if (s - ostatni >= krok - 1e-3f) { rzedy.Add(i); ostatni = s; }
        }
        rzedy.Add(0);                                                  // closed: the last row is the first
        int nu = us.Length;
        var vT = new List<Vector3>(); var nT = new List<Vector3>(); var uvT = new List<Vector2>(); var tT = new List<int>(); var tP = new List<int>(); var tS = new List<int>();
        for (int r = 0; r < rzedy.Count; r++)
        {
            int i = rzedy[r]; float s = r == rzedy.Count - 1 ? o.dlugosc : o.s[i];
            float lx = -o.tz[i], lz = o.tx[i];
            // the surface's normal from its own slope along and across
            int ia = (i + o.N - 2) % o.N, ib = (i + 2) % o.N; float ds = 1f;
            float dAlong = (Tor(ib, 0f) - Tor(ia, 0f)) / (2f * ds);
            float dAcross = Tor(i, .5f) - Tor(i, -.5f);
            var tan = new Vector3(o.tx[i], dAlong, o.tz[i]).normalized; var lew = new Vector3(lx, dAcross, lz).normalized;
            var n = Vector3.Cross(tan, lew).normalized; if (n.y < 0f) n = -n;
            for (int c = 0; c < nu; c++)
            {
                float u = us[c];
                vT.Add(new Vector3(o.x[i] + lx * u, Tor(i, u), o.z[i] + lz * u) * K); nT.Add(n); uvT.Add(new Vector2(u / 2.25f, s / 2.25f));
            }
            // the skirts: from each edge 0.6 m down (no gap to the ground under any angle)
            foreach (int c in new[] { 0, nu - 1 })
            {
                var top = vT[r * (nu + 2) + c]; vT.Add(top - Vector3.up * .6f * K); nT.Add(new Vector3(c == 0 ? lx : -lx, 0f, c == 0 ? lz : -lz)); uvT.Add(new Vector2(c == 0 ? 2f : -2f, s / 2.25f));
            }
        }
        int st = nu + 2;
        for (int r = 0; r + 1 < rzedy.Count; r++)
        {
            int a = r * st, b = (r + 1) * st;
            for (int c = 0; c + 1 < nu; c++)
            {
                                // facing up: start -> ahead -> right (the villas' grid winding; u falls with c: c+1 is to the right)
                var lista = (c == 0 || c == nu - 2) ? tP : tT;
                lista.Add(a + c); lista.Add(b + c); lista.Add(a + c + 1);
                lista.Add(b + c); lista.Add(b + c + 1); lista.Add(a + c + 1);
            }
            // the skirts (left edge c=0 -> skirt nu; right edge nu-1 -> skirt nu+1), outward
            tS.Add(b + 0); tS.Add(a + 0); tS.Add(a + nu); tS.Add(b + 0); tS.Add(a + nu); tS.Add(b + nu);
            tS.Add(a + nu - 1); tS.Add(b + nu - 1); tS.Add(b + nu + 1); tS.Add(a + nu - 1); tS.Add(b + nu + 1); tS.Add(a + nu + 1);
        }
        var v = vT.ToArray(); var nn = nT.ToArray(); var uv = uvT.ToArray();
        Zapisz(g, "TOR DO KŁADÓW | nawierzchnia (koleiny) | kolizja", "tor_kladow_nawierzchnia", v, nn, uv, tT, nawierzchnia, true);
        Zapisz(g, "TOR DO KŁADÓW | pobocza | kolizja", "tor_kladow_pobocza", v, nn, uv, tP, pobocze, true);
        tP.Clear(); tP.AddRange(tS);
        Zapisz(g, "TOR DO KŁADÓW | skarpy pod krawędzią", "tor_kladow_skarpy", v, nn, uv, tP, pobocze, false);
    }

    // the ford's water: a strip over the dip, wider than the track (the cut banks rise out of it)
    static void Woda(Transform g)
    {
        var o = Linia();
        foreach (var p in ulozone)
        {
            if (p.typ != Typ.Brod) continue;
            int i0 = Indeks(p.s0), i1 = Indeks(p.s0 + p.dl);
            float y = Baza(Indeks(p.s0 + p.dl * .5f)) - p.h + WodaGlebokosc;
            var b = new BraxChampionsMeshBatch();
            float half = Szer * .5f + Pobocze + 1.4f;
            for (int i = i0; i < i1; i += 2)
            {
                int j = Mathf.Min(i + 2, i1);
                Vector3 A(int k, float u) => new Vector3(o.x[k] - o.tz[k] * u, y, o.z[k] + o.tx[k] * u);
                b.QuadUV(woda, A(i, half), A(j, half), A(j, -half), A(i, -half), new Vector2(0, o.s[i]), new Vector2(0, o.s[j]), new Vector2(1, o.s[j]), new Vector2(1, o.s[i]));
            }
            b.Build(g, "TOR DO KŁADÓW | bród (woda)", Folder, false, K);
            Trojkaty += b.Triangles;
        }
    }

    static void Zapisz(Transform g, string nazwa, string plik, Vector3[] v, Vector3[] n, Vector2[] uv, List<int> t, Material mat, bool kolizja)
    {
        if (t.Count == 0) return;
        // only the vertices the triangles use
        var mapa = new Dictionary<int, int>(); var vv = new List<Vector3>(); var nv = new List<Vector3>(); var uu = new List<Vector2>(); var tt = new int[t.Count];
        for (int i = 0; i < t.Count; i++)
        {
            if (!mapa.TryGetValue(t[i], out int q)) { q = vv.Count; mapa[t[i]] = q; vv.Add(v[t[i]]); nv.Add(n[t[i]]); uu.Add(uv[t[i]]); }
            tt[i] = q;
        }
        var mesh = new Mesh { name = plik, indexFormat = vv.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(vv); mesh.SetNormals(nv); mesh.SetUVs(0, uu); mesh.SetTriangles(tt, 0);
        mesh.RecalculateTangents(); mesh.RecalculateBounds();
        string path = Folder + "/" + plik + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject(nazwa); go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
        if (kolizja) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        Trojkaty += tt.Length / 3;
    }

    // the road (gravel, 12 cm over the city's grass, its edges down under it) and the paddock's gravel (the ground cut out
    // under it, a skirt round it): meshes with their own colliders
    static void DrogaIPaddock(Transform g)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        void Pas(float x0, float x1, float z0, float z1, Func<float, float> y, float krok)
        {
            int nx = Mathf.Max(1, Mathf.CeilToInt((x0 - x1) / krok)); int b = v.Count;
            for (int i = 0; i <= nx; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)nx), yy = y(x), ya = y(x + .5f), yb = y(x - .5f);
                var nn = new Vector3(-(ya - yb), 1f, 0f).normalized;
                foreach (float z in new[] { z0, z1 }) { v.Add(new Vector3(x, yy, z) * K); n.Add(nn); uv.Add(new Vector2(x / 3f, z / 3f)); }
                foreach (float z in new[] { z0, z1 }) { v.Add(new Vector3(x, yy - .45f, z) * K); n.Add(new Vector3(0f, 0f, z == z0 ? -1f : 1f)); uv.Add(new Vector2(x / 3f, z / 3f - .15f)); }
            }
            for (int i = 0; i < nx; i++)
            {
                int a = b + i * 4, c = a + 4;                                 // top: a, a+1 (z0, z1); skirt: a+2, a+3
                // x falls along the strip: (a, c, a+1) faces up (as the villas' grid (i,j)->(i,j+1)->(i+1,j) with x rising)
                t.AddRange(new[] { a, c, a + 1, c, c + 1, a + 1 });
                t.AddRange(new[] { c, a, a + 2, c, a + 2, c + 2 });                     // the z0 skirt, facing -z
                t.AddRange(new[] { a + 1, c + 1, c + 3, a + 1, c + 3, a + 3 });         // the z1 skirt, facing +z
            }
        }
        // the road: its face either way so it reads from both sides; x falls along it
        Pas(DrogaX1, DrogaX0, DrogaZ - DrogaPol, DrogaZ + DrogaPol, x => Droga(x, DrogaZ, out _), 1f);
        // the paddock: rows across x, flat
        Pas(PX1, PX0, PZ0, PZ1, x => PaddockY, 4f);
        // its east and west ends' skirts
        foreach (float x in new[] { PX0, PX1 })
        {
            int a = v.Count;
            v.Add(new Vector3(x, PaddockY, PZ0) * K); v.Add(new Vector3(x, PaddockY, PZ1) * K); v.Add(new Vector3(x, PaddockY - .45f, PZ0) * K); v.Add(new Vector3(x, PaddockY - .45f, PZ1) * K);
            for (int k = 0; k < 4; k++) { n.Add(new Vector3(x == PX0 ? -1f : 1f, 0f, 0f)); uv.Add(new Vector2(PZ0 / 3f + (k % 2) * (PZ1 - PZ0) / 3f, k < 2 ? 0f : .15f)); }
            if (x == PX0) t.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 }); else t.AddRange(new[] { a, a + 1, a + 2, a + 2, a + 1, a + 3 });
        }
        Zapisz(g, "TOR DO KŁADÓW | droga i padok (szuter) | kolizja", "tor_kladow_szuter", v.ToArray(), n.ToArray(), uv.ToArray(), t, szuter, true);
    }

    // the track's markers: orange and white stakes either side every 8 m (no colliders - they bend under a quad)
    static void Tyczki(BraxChampionsMeshBatch b)
    {
        var o = Linia(); float nast = 0f; int n = 0;
        for (int i = 0; i < o.N; i++)
        {
            if (o.s[i] < nast) continue; nast = o.s[i] + 8f;
            if (Mathf.Abs(o.x[i] - MetaX) < 6f && o.z[i] > 905f) continue;
            foreach (float bok in new[] { 1f, -1f })
            {
                float u = bok * (Szer * .5f + Pobocze + .5f), x = o.x[i] - o.tz[i] * u, z = o.z[i] + o.tx[i] * u, y = Grunt(x, z);
                if (DoPaddocku(x, z) < 1f) continue;
                b.Box(n % 2 == 0 ? pomarancz : biel, new Vector3(x, y + .4f, z), new Vector3(.05f, .8f, .05f));
                b.Box(n % 2 == 0 ? biel : pomarancz, new Vector3(x, y + .84f, z), new Vector3(.09f, .14f, .09f));                 // the cap 2 cm proud of the stake (no shared faces)
            }
            n++;
        }
    }

    // the fence round the site (post and three rails, timber), 26 m in from its edges; the east side at x -8 with the road's
    // gate; colliders per span
    public static int Ogrodzenie;
    static void Plot(BraxChampionsMeshBatch b)
    {
        float i0 = X0 + FenceInset, j0 = Z0 + FenceInset, j1 = Z1 - FenceInset, i1 = PlotWschodX;
        var linie = new[] { (new Vector2(i0, j0), new Vector2(i0, j1)), (new Vector2(i0, j1), new Vector2(i1, j1)), (new Vector2(i1, j1), new Vector2(i1, j0)), (new Vector2(i1, j0), new Vector2(i0, j0)) };
        Ogrodzenie = 0;
        foreach (var (a, c) in linie)
        {
            float len = Vector2.Distance(a, c); int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.5f));
            for (int k = 0; k <= n; k++)
            {
                var p = Vector2.Lerp(a, c, k / (float)n);
                if (Mathf.Abs(p.x - PlotWschodX) < .1f && Mathf.Abs(p.y - DrogaZ) < DrogaPol + 1.5f) continue;   // the road's gate
                float y = Grunt(p.x, p.y);
                b.Box(drewno, new Vector3(p.x, y + .55f, p.y), new Vector3(.12f, 1.25f, .12f));
                if (k == n) break;
                var p1 = Vector2.Lerp(a, c, (k + 1) / (float)n);
                if (Mathf.Abs(p1.x - PlotWschodX) < .1f && Mathf.Abs(p1.y - DrogaZ) < DrogaPol + 1.5f) continue;
                float y1 = Grunt(p1.x, p1.y); var mid = (p + p1) * .5f; float l = Vector2.Distance(p, p1);
                float yaw = Mathf.Atan2(p1.x - p.x, p1.y - p.y) * Mathf.Rad2Deg, pitch = -Mathf.Atan2(y1 - y, l) * Mathf.Rad2Deg;
                var rq = Quaternion.Euler(pitch, yaw, 0f);
                foreach (float hy in new[] { .3f, .65f, 1.0f }) b.Box(drewno, new Vector3(mid.x, (y + y1) * .5f + hy, mid.y), new Vector3(.05f, .12f, Mathf.Sqrt(l * l + (y1 - y) * (y1 - y))), rq);
                W.Kolizja(new Vector3(mid.x, (y + y1) * .5f + 1.2f, mid.y) * K, new Vector3(.25f, 2.4f + Mathf.Abs(y1 - y), l) * K, Quaternion.Euler(0f, yaw, 0f));
                Ogrodzenie += Mathf.RoundToInt(l);
            }
        }
    }

    // ---------------------------------------------------------------- the paddock: the rental booth, the quads, the grandstand,
    // the start gate, the START / META arch, the light masts, the banners; the lap timer
    static BraxChampionsTorCzas Paddock(Transform g, BraxChampionsMeshBatch b)
    {
        var o = Linia(); float y = PaddockY;
        // the rental booth (owner: "budkę trzeba zrobić ładną"): 10 x 5 m, 3.4 m, dark timber on a concrete plinth, glazed
        // across its front (black mullions every 2 m) over a counter, a flat roof 1.5 m over the front with an LED line,
        // WYPOŻYCZALNIA KŁADÓW on its black fascia and BRAXHORN in gold standing on the roof; the door at its east side
        float bx = -62f, bz = 1000f, bw = 10f, bd = 5f, bh = 3.4f, fz = bz - bd * .5f;
        b.BoxMinMax(beton, bx - bw * .5f - .2f, bx + bw * .5f + .2f, y - .1f, y + .3f, bz - bd * .5f - 1.6f, bz + bd * .5f + .2f);   // plinth + its front step
        b.BoxMinMax(drewno, bx - bw * .5f, bx + bw * .5f, y + .3f, y + bh, fz + .12f, bz + bd * .5f);                         // the body
        b.BoxMinMax(drewno, bx - bw * .5f, bx + bw * .5f, y + .3f, y + .95f, fz, fz + .12f);                                 // under the counter
        b.BoxMinMax(drewno, bx - bw * .5f, bx + bw * .5f, y + 3.0f, y + bh, fz, fz + .12f);                                  // over the glass
        b.BoxMinMax(szyba, bx - bw * .5f + .1f, bx + bw * .5f - .1f, y + .95f, y + 3.0f, fz + .04f, fz + .08f);               // the glazing
        for (int k = 0; k <= 5; k++) b.BoxMinMax(czern, bx - bw * .5f + k * 2f - .05f, bx - bw * .5f + k * 2f + .05f, y + .95f, y + 3.0f, fz - .02f, fz + .1f);
        b.BoxMinMax(czern, bx - bw * .5f, bx + bw * .5f, y + .95f, y + 1.03f, fz - .45f, fz + .1f);                           // the counter
        b.BoxMinMax(czern, bx - bw * .5f - .4f, bx + bw * .5f + .4f, y + bh, y + bh + .2f, fz - 1.5f, bz + bd * .5f + .3f);   // the roof slab
        b.BoxMinMax(czern, bx - bw * .5f - .4f, bx + bw * .5f + .4f, y + bh - .25f, y + bh + .2f, fz - 1.55f, fz - 1.45f);    // its fascia
        b.BoxMinMax(led, bx - bw * .5f - .3f, bx + bw * .5f + .3f, y + bh - .27f, y + bh - .25f, fz - 1.43f, fz - 1.3f);       // the LED line under it
        b.BoxMinMax(czern, bx + bw * .5f - .02f, bx + bw * .5f + .03f, y + .3f, y + 2.4f, bz - .55f, bz + .55f);                // the door
        foreach (float sx in new[] { -1f, 1f }) b.BoxMinMax(czern, bx + sx * (bw * .5f + .25f) - .08f, bx + sx * (bw * .5f + .25f) + .08f, y + .3f, y + bh, fz - 1.42f, fz - 1.26f);   // roof posts
        BraxChampionsLetters3D.Text(b, biel, czern, "WYPOŻYCZALNIA KŁADÓW", new Vector3(bx, y + bh - .2f, fz - 1.56f), 180f, .2f, .04f, .12f, bw - 1f);
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN", new Vector3(bx, y + bh + .2f, fz - 1.1f), 180f, .7f, .1f, .14f, 0f);
        W.Kolizja(new Vector3(bx, y + bh * .5f, bz) * K, new Vector3(bw, bh, bd) * K, Quaternion.identity);
        W.Kolizja(new Vector3(bx, y + .5f, fz - .2f) * K, new Vector3(bw, 1.05f, .5f) * K, Quaternion.identity);
        // the helmet rack beside it: six helmets on two shelves, the quads' colours
        Color[] kolory = { new Color(.75f, .06f, .05f), new Color(.08f, .2f, .65f), new Color(.92f, .92f, .9f), new Color(.6f, .85f, .08f), new Color(1f, .42f, .02f), new Color(.38f, .16f, .66f) };
        float hx = bx - bw * .5f - 1.6f, hz = fz + .6f;
        b.BoxMinMax(stal, hx - .9f, hx + .9f, y, y + 1.6f, hz - .2f, hz + .2f);
        for (int i = 0; i < 6; i++)
        {
            var kask = BraxChampionsHarbour.Surface("tor_kladow_kask_" + i, kolory[i], .25f, .1f);
            float kx = hx - .6f + (i % 3) * .6f, ky = y + (i < 3 ? .75f : 1.35f);
            b.Lathe(kask, new Vector3(kx, ky, hz - .25f), new[] { new Vector2(0f, 0f), new Vector2(.15f, .02f), new Vector2(.16f, .14f), new Vector2(.11f, .26f), new Vector2(0f, .3f) }, 14);
            b.BoxMinMax(czern, kx - .11f, kx + .11f, ky + .1f, ky + .17f, hz - .42f, hz - .38f);                               // the visor
        }
        // the six bays in front of the booth (concrete, white lines) and the quads in them, facing the track
        float baysZ = fz - 9f, bayW = 2.8f, bay0 = bx - 2.5f * bayW;
        // the canopy over the bays: a black steel deck 3.3 m up on six posts, an LED line along its front (the quads in its shade)
        float c0 = bay0 - bayW * .5f - .6f, c1 = bay0 + 5.5f * bayW + .6f, cz0 = baysZ - 2.9f, cz1 = baysZ + 2.9f;
        b.BoxMinMax(czern, c0, c1, y + 3.3f, y + 3.5f, cz0, cz1);
        b.BoxMinMax(led, c0 + .2f, c1 - .2f, y + 3.28f, y + 3.3f, cz0 + .1f, cz0 + .25f);
        foreach (float cx in new[] { c0 + .2f, (c0 + c1) * .5f, c1 - .2f })
            foreach (float cz in new[] { cz0 + .2f, cz1 - .2f })
            { b.BoxMinMax(stal, cx - .09f, cx + .09f, y, y + 3.3f, cz - .09f, cz + .09f); W.Kolizja(new Vector3(cx, y + 1.65f, cz) * K, new Vector3(.2f, 3.3f, .2f) * K, Quaternion.identity); }
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN QUAD RENTAL", new Vector3((c0 + c1) * .5f, y + 3.33f, cz0 - .01f), 180f, .14f, .02f, .12f, c1 - c0 - 2f);
        // two feather flags at the booth's corners (black, gold edge, orange)
        foreach (float fx in new[] { bx - bw * .5f - 3.2f, bx + bw * .5f + 3.2f })
        {
            b.Cylinder(stal, new Vector3(fx, y, fz - 2f), .03f, 4.2f, 8);
            b.BoxMinMax(czern, fx, fx + .75f, y + 1.2f, y + 4.1f, fz - 2.02f, fz - 1.98f);
            b.BoxMinMax(zloto, fx + .75f, fx + .82f, y + 1.2f, y + 4.1f, fz - 2.03f, fz - 1.97f);
            b.BoxMinMax(pomarancz, fx, fx + .75f, y + 1.2f, y + 1.45f, fz - 2.03f, fz - 1.97f);
        }
        b.BoxMinMax(beton, bay0 - bayW * .5f - .3f, bay0 + 5.5f * bayW + .3f, y - .1f, y + .1f, baysZ - 2.2f, baysZ + 2.2f);
        W.Kolizja(new Vector3((bay0 + bay0 + 5f * bayW) * .5f, y, baysZ) * K, new Vector3(6f * bayW + .6f, .2f, 4.4f) * K, Quaternion.identity);
        for (int i = 0; i <= 6; i++) b.BoxMinMax(biel, bay0 - bayW * .5f + i * bayW - .05f, bay0 - bayW * .5f + i * bayW + .05f, y + .1f, y + .115f, baysZ - 2f, baysZ + 2f);
        var czas = new GameObject("TOR DO KŁADÓW | pomiar czasu okrążenia").AddComponent<BraxChampionsTorCzas>();
        czas.transform.SetParent(g, false);
        var wolne = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Source/Mistrzowie/Audio/Klad/klad_silnik_wolne.ogg");
        var jazda = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Source/Mistrzowie/Audio/Klad/klad_silnik_jazda.ogg");
        Klady = 0;
        for (int i = 0; i < BraxChampionsKlady.Flota.Length; i++)
        {
            var f = BraxChampionsKlady.Flota[i];
            try
            {
                if (BraxChampionsKlady.Postaw(g, f.model, f.kolor, f.napis, new Vector3(bay0 + i * bayW, y + .1f, baysZ) * K, 180f, czas, wolne, jazda)) Klady++;
            }
            catch (Exception e) { BraxBuildQuality.Warn("DO POPRAWKI TOR_KLADOW_KLAD " + f.napis + ": " + e.Message); }
        }
        // the grandstand over the start straight: 30 m, six rows (0.45 m up, 0.85 m deep), steel frame, black seats
        float gx0 = -152f, gx1 = -108f, gz = 940f;
        for (int r = 0; r < 6; r++)
        {
            float z0 = gz + r * .85f, ty = y + .45f * (r + 1);
            b.BoxMinMax(beton, gx0, gx1, y, ty, z0, z0 + .85f);
            for (float sx = gx0 + .3f; sx < gx1 - .3f; sx += .55f) b.BoxMinMax(siedzisko, sx, sx + .45f, ty, ty + .08f, z0 + .1f, z0 + .5f);
            W.Kolizja(new Vector3((gx0 + gx1) * .5f, (y + ty) * .5f, z0 + .425f) * K, new Vector3(gx1 - gx0, ty - y, .85f) * K, Quaternion.identity);
        }
        b.BoxMinMax(stal, gx0, gx1, y + 2.7f, y + 3.6f, gz + 5.1f, gz + 5.2f);   // the back rail panel
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN QUAD CROSS", new Vector3((gx0 + gx1) * .5f, y + 2.85f, gz + 5.08f), 180f, .55f, .06f, .12f, gx1 - gx0 - 4f);
        // the START / META arch over the finish line: two black towers, a beam with BRAXHORN and the chequered band
        int im = 0; float bd2 = float.MaxValue; for (int i = 0; i < o.N; i++) { float d = Mathf.Abs(o.x[i] - MetaX) + (o.z[i] > 905f ? 0f : 1000f); if (d < bd2) { bd2 = d; im = i; } }
        float my = Tor(im, 0f), half = Szer * .5f + Pobocze + .8f, mz = o.z[im];
        foreach (float sz in new[] { -1f, 1f }) { b.BoxMinMax(czern, MetaX - .45f, MetaX + .45f, my - .4f, my + 6.2f, mz + sz * half - .45f, mz + sz * half + .45f); W.Kolizja(new Vector3(MetaX, my + 2.9f, mz + sz * half) * K, new Vector3(.9f, 6.6f, .9f) * K, Quaternion.identity); }
        b.BoxMinMax(czern, MetaX - .35f, MetaX + .35f, my + 5f, my + 6.2f, mz - half - .45f, mz + half + .45f);
        for (int c = 0; c < 24; c++)
            for (int rr = 0; rr < 2; rr++)
            {
                float zc = mz - half + .1f + c * (2f * half - .2f) / 24f;
                b.BoxMinMax(((c + rr) % 2 == 0) ? biel : czern, MetaX - .37f, MetaX - .35f, my + 5.02f + rr * .25f, my + 5.27f + rr * .25f, zc, zc + (2f * half - .2f) / 24f);
            }
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN", new Vector3(MetaX - .36f, my + 5.62f, mz), -90f, .42f, .04f, .14f, 2f * half - 1f);
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN", new Vector3(MetaX + .36f, my + 5.62f, mz), 90f, .42f, .04f, .14f, 2f * half - 1f);
        // the finish line itself: a white band across the track
        b.BoxMinMax(biel, MetaX - .25f, MetaX + .25f, my + .04f, my + .07f, mz - Szer * .5f, mz + Szer * .5f);
        // the start gate: a concrete pad, a steel frame, five drop bars (FIM: 50-52 cm high, a 1 m+ position each)
        int ig = 0; bd2 = float.MaxValue; for (int i = 0; i < o.N; i++) { float d = Mathf.Abs(o.x[i] - BramkaX) + (o.z[i] > 905f ? 0f : 1000f); if (d < bd2) { bd2 = d; ig = i; } }
        float gy = Tor(ig, 0f), gzc = o.z[ig];
        b.BoxMinMax(beton, BramkaX - .3f, BramkaX + .9f, gy - .2f, gy + .08f, gzc - Szer * .5f, gzc + Szer * .5f);
        for (int k = 0; k <= 5; k++) b.BoxMinMax(stal, BramkaX - .05f, BramkaX + .05f, gy, gy + .55f, gzc - Szer * .5f + k * Szer / 5f - .05f, gzc - Szer * .5f + k * Szer / 5f + .05f);
        for (int k = 0; k < 5; k++) b.BoxMinMax(k % 2 == 0 ? czerwien : biel, BramkaX - .12f, BramkaX - .06f, gy + .05f, gy + .5f, gzc - Szer * .5f + k * Szer / 5f + .08f, gzc - Szer * .5f + (k + 1) * Szer / 5f - .08f);
        // light masts (4) round the paddock
        foreach (var (lx, lz) in new[] { (-157f, 952f), (-33f, 952f), (-157f, 1005f), (-33f, 1005f) })
        {
            b.Cylinder(stal, new Vector3(lx, y, lz), .14f, 9f, 10);
            b.BoxMinMax(czern, lx - .6f, lx + .6f, y + 9f, y + 9.4f, lz - .3f, lz + .3f);
            b.BoxMinMax(led, lx - .55f, lx + .55f, y + 8.98f, y + 9.0f, lz - .25f, lz + .25f);
            W.Kolizja(new Vector3(lx, y + 4.5f, lz) * K, new Vector3(.3f, 9f, .3f) * K, Quaternion.identity);
        }
        // the banners on the paddock's south side along the start straight: three black panels with BRAXHORN
        foreach (float px in new[] { -98f, -70f, -42f })
        {
            float pz = PZ0 - .5f;
            foreach (float sx in new[] { -3.2f, 3.2f }) b.Cylinder(stal, new Vector3(px + sx, y, pz), .05f, 1.9f, 8);
            b.BoxMinMax(czern, px - 3.2f, px + 3.2f, y + .7f, y + 1.8f, pz - .02f, pz + .02f);
            BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN", new Vector3(px, y + .95f, pz - .03f), 180f, .55f, .03f, .14f, 5.6f);
        }
        // the lap timer's line: the centre every 5 m from the finish line on (world)
        var pkt = new List<Vector3>(); float s0 = o.s[im], nast = 0f;
        for (int k = 0; k < o.N; k++)
        {
            int i = (im + k) % o.N; float s = ((o.s[i] - s0) % o.dlugosc + o.dlugosc) % o.dlugosc;
            if (s < nast) continue; nast = s + 5f;
            pkt.Add(new Vector3(o.x[i], Tor(i, 0f), o.z[i]) * K);
        }
        czas.punkty = pkt.ToArray(); czas.polSzerokosci = (Szer * .5f + Pobocze) * K;
        return czas;
    }
}
