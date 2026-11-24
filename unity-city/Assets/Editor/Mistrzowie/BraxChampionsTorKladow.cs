using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using W = BraxWnetrzaKit;

/// <summary>THE BRAXHORN QUAD TRACK (owner 24.11.2026 11:31-11:45: "tor do kładów gdzieś z boku ... jakiś zajebisty", "Zrób tam,
/// gdzie poleciłeś, zachód", "górki, pagórki, wyskoki", "budkę trzeba zrobić ładną ... minimum 4 kłady"; 14:00-14:04, with his
/// two photos of real tracks: "jakieś zakręty ... weź zobacz jak na internecie jak to wygląda", "w jakichś błotach albo po jakichś
/// lasach", "Coś takiego lasy, górki"; the plan's map accepted 14:20 with "więcej lasu").
/// West of the city, north of the D2 railway hill: a 2.5 km loop the way the owner's photos show them - a narrow track snaking
/// back and forth in hairpins with grass and trees between its loops, on a wooded hill. In the riding order: the start
/// straight (8 m, from the gate past the paddock and the grandstand, no jump - FIM), turn 1, then the WOODS (5 m wide: the
/// GNCC-style cross-country ATV trails): switchbacks up the north face of an 18 m hill (logs, a mud hole), over its top,
/// switchbacks down its south face (a creek), out along its foot through the big MUD PIT; then the MOTOCROSS field (7 m): five
/// bands with an S-bend each and banked hairpins between them - a rhythm double and the big table, whoops and a small table,
/// the step-up and rollers, rollers and a small table, the last big table and a double - and the last corner onto the start
/// straight. Every turn a BERM (the outer edge raised, up to 13 degrees). Trees everywhere off the track: the hill's forest,
/// rows and clumps between the field's loops, a forest wall round the site (BraxChampionsLasKladow). The surface: Poly Haven
/// Muddy Tracks (tyre ruts), its shoulders Brown Mud 02, the mud Aerial Mud 1, the paddock and the road Gravel Ground 01 (no
/// dirt on the owner's list - ZASADY 5.8).
/// The paddock: the rental booth (WYPOŻYCZALNIA KŁADÓW BRAXHORN) with the six quads in their bays (BraxChampionsKlady), the
/// grandstand over the start straight, the start gate, the START / META arch, light masts, the riders' park; the freestyle
/// zone north of the start straight; the road from the city's north-west corner (the street mouth at x 60, z 935). The lap
/// timer: BraxChampionsTorCzas; the race: BraxChampionsWyscig. All in nominal metres x K.</summary>
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
    // the riding widths: the start straight, the motocross field, the woods' trail
    public const float Start = 8f, MX = 7f, Las = 5f;
    // the loop's corners in the riding order (x, z, fillet radius, the width from this corner on): the start straight runs west
    // along z 915 into corner 0; the woods from corner 1 to corner 32, the field from 32 on (the plan of 24.11, v9)
    public static readonly Vector4[] Rogi =
    {
        new Vector4(-266f, 915f, 16f, MX), new Vector4(-266f, 889f, 9f, Las), new Vector4(-318f, 889f, 14f, Las), new Vector4(-334f, 899f, 14f, Las),
        new Vector4(-374f, 899f, 14f, Las), new Vector4(-390f, 889f, 14f, Las), new Vector4(-482f, 889f, 9f, Las), new Vector4(-482f, 871f, 9f, Las),
        new Vector4(-446f, 871f, 14f, Las), new Vector4(-432f, 865f, 14f, Las), new Vector4(-404f, 865f, 14f, Las), new Vector4(-390f, 871f, 14f, Las),
        new Vector4(-326f, 871f, 9f, Las), new Vector4(-326f, 853f, 9f, Las), new Vector4(-452f, 853f, 9f, Las), new Vector4(-452f, 831f, 12f, Las),
        new Vector4(-432f, 814f, 12f, Las), new Vector4(-432f, 790f, 12f, Las), new Vector4(-452f, 770f, 12f, Las), new Vector4(-452f, 750f, 9f, Las),
        new Vector4(-412f, 750f, 12f, Las), new Vector4(-397f, 759f, 12f, Las), new Vector4(-372f, 759f, 12f, Las), new Vector4(-357f, 750f, 12f, Las),
        new Vector4(-334f, 750f, 9f, Las), new Vector4(-334f, 732f, 9f, Las), new Vector4(-396f, 732f, 12f, Las), new Vector4(-406f, 727f, 12f, Las),
        new Vector4(-428f, 727f, 12f, Las), new Vector4(-438f, 732f, 12f, Las), new Vector4(-452f, 732f, 9f, Las), new Vector4(-452f, 714f, 9f, Las),
        new Vector4(-300f, 714f, 25f, MX), new Vector4(-280f, 700f, 25f, MX), new Vector4(-235f, 700f, 16f, MX), new Vector4(-222f, 712f, 16f, MX),
        new Vector4(-200f, 712f, 16f, MX), new Vector4(-187f, 700f, 16f, MX), new Vector4(-50f, 700f, 21f, MX), new Vector4(-50f, 742f, 21f, MX),
        new Vector4(-170f, 742f, 16f, MX), new Vector4(-183f, 754f, 16f, MX), new Vector4(-205f, 754f, 16f, MX), new Vector4(-218f, 742f, 16f, MX),
        new Vector4(-250f, 742f, 21f, MX), new Vector4(-250f, 784f, 21f, MX), new Vector4(-220f, 784f, 16f, MX), new Vector4(-207f, 772f, 16f, MX),
        new Vector4(-185f, 772f, 16f, MX), new Vector4(-172f, 784f, 16f, MX), new Vector4(-70f, 784f, 21f, MX), new Vector4(-70f, 826f, 21f, MX),
        new Vector4(-172f, 826f, 16f, MX), new Vector4(-185f, 838f, 16f, MX), new Vector4(-207f, 838f, 16f, MX), new Vector4(-220f, 826f, 16f, MX),
        new Vector4(-250f, 826f, 21f, MX), new Vector4(-250f, 868f, 21f, MX), new Vector4(-222f, 868f, 16f, MX), new Vector4(-209f, 858f, 16f, MX),
        new Vector4(-187f, 858f, 16f, MX), new Vector4(-174f, 868f, 16f, MX), new Vector4(-19f, 868f, 16f, MX), new Vector4(-19f, 915f, 16f, Start),
    };

    // ---------------------------------------------------------------- the hill (owner 14:04: "lasy, górki")
    // An 18 m wooded hill in the site's west (its slope a raised cosine, 150 m round), on the land's own lie; it fades out
    // before the site's rim band (the far land meets the site's ground there as before).
    public const float GoraX = -410f, GoraZ = 820f, GoraH = 18f, GoraR = 150f;
    public static float Gora(float x, float z)
    {
        float d = Mathf.Sqrt((x - GoraX) * (x - GoraX) + (z - GoraZ) * (z - GoraZ));
        if (d >= GoraR) return 0f;
        return GoraH * .5f * (1f + Mathf.Cos(Mathf.PI * d / GoraR)) * Smooth(0f, RimBand + 12f, DoKrawedzi(x, z));
    }
    /// <summary>The land's own lie inside the site: the far land and the hill.</summary>
    public static float Naturalna(float x, float z) => BraxChampionsHorizon.FarHeight(x, z) + Gora(x, z);

    // ---------------------------------------------------------------- the obstacles
    public enum Typ { Stol, Podwojna, Schodek, Fale, Muldy, Gorka, Brod, Bloto, Klody }
    public struct Przeszkoda
    {
        public int prosta; public float od; public Typ typ; public float h; public int n;
        public float s0, dl;                                       // set from the centre line: its start along the loop, its length
        public Przeszkoda(int prosta, float od, Typ typ, float h, int n = 0) { this.prosta = prosta; this.od = od; this.typ = typ; this.h = h; this.n = n; s0 = 0f; dl = 0f; }
    }
    public const float KlodaOdstep = 2.4f, KlodaR = .2f;           // logs 0.4 m thick, 2.4 m apart, each on its dirt ramps
    // the straight i runs from corner i to corner i+1; od = metres from the straight's start
    public static readonly Przeszkoda[] Przeszkody =
    {
        // the woods
        new Przeszkoda(1, 12f, Typ.Klody, .38f, 2),       // into the woods: two logs across the trail
        new Przeszkoda(5, 30f, Typ.Bloto, .35f, 18),      // a mud hole on the north face (18 m, 0.35 m deep)
        new Przeszkoda(11, 20f, Typ.Klody, .38f, 1),      // a log
        new Przeszkoda(13, 70f, Typ.Klody, .38f, 3),      // three logs on the climb to the top
        new Przeszkoda(25, 15f, Typ.Brod, .8f),           // the creek on the south face: an 18 m dip, 0.8 m, water 0.42 m deep
        new Przeszkoda(31, 40f, Typ.Bloto, .3f, 26),      // the big mud pit along the hill's foot (owner 24.11 12:57)
        new Przeszkoda(31, 85f, Typ.Fale, .6f, 3),        // rollers out of the woods
        // the motocross field
        new Przeszkoda(37, 10f, Typ.Podwojna, 1.5f),      // band 1: the rhythm double
        new Przeszkoda(37, 55f, Typ.Stol, 2.4f),          //         the big table-top: 8 m face (~19 deg to the lip), 16 m top, 11 m landing
        new Przeszkoda(39, 8f, Typ.Muldy, .45f, 10),      // band 2: the whoops, 10 x 4.5 m, 0.45 m
        new Przeszkoda(39, 62f, Typ.Stol, 1.3f),          //         a small table
        new Przeszkoda(49, 10f, Typ.Schodek, 1.5f),       // band 3: step up 1.5 m, 22 m plateau, step down onto a 9 m landing
        new Przeszkoda(49, 50f, Typ.Fale, .7f, 2),        //         two rollers (FIM: 8-10 m apart, <= 0.8 m)
        new Przeszkoda(51, 6f, Typ.Fale, .7f, 4),         // band 4: rollers
        new Przeszkoda(51, 45f, Typ.Stol, 1.3f),          //         a small table
        new Przeszkoda(61, 15f, Typ.Stol, 2.4f),          // band 5: the last big table
        new Przeszkoda(61, 70f, Typ.Podwojna, 2.0f),      //         and a double before the last corner
    };
    public const float WodaGlebokosc = .42f;

    // ---------------------------------------------------------------- the freestyle zone (owner 24.11 12:57: "rampy po bokach";
    // 14:00 "jak to takie dziwne przeskoczyć?" - redone as a real FMX set-up and moved north of the start straight)
    // On bare earth, 25 m+ from the track: three lanes running east, each an FMX kicker - a steel deck on a circular
    // transition to its lip, on an open steel frame (posts, diagonal braces, the lip's legs), its lip painted - and a big dirt
    // landing: a steep 1:1 face towards the kicker, a 2 m knuckle 0.5 m over the lip, then a long landing (a smooth curve,
    // steepest ~32 deg) into the run-out. The gap and the window of speeds from the ballistics (VMin to the knuckle, VMax to
    // the landing's foot); a board at each run-up's start gives it; both quads reach the slowest on the 70 m run-up after the
    // climb (the utility Rancher with 18 % to spare on the big one).
    public const float FX0 = -300f, FX1 = -170f, FZ0 = 945f, FZ1 = 1018f, FLip = -220f, FBieg = 70f;
    public struct Wyrzutnia { public float z, h, kat, luka, ladowanie; public Wyrzutnia(float z, float h, float kat, float luka, float ladowanie) { this.z = z; this.h = h; this.kat = kat; this.luka = luka; this.ladowanie = ladowanie; } }
    public static readonly Wyrzutnia[] Rampy = { new Wyrzutnia(958f, 2.2f, 35f, 12f, 2.7f), new Wyrzutnia(980f, 2.6f, 38f, 14f, 3.1f), new Wyrzutnia(1002f, 3.0f, 40f, 16f, 3.5f) };
    public const float RampaSzer = 4.5f, LadowanieSzer = 10f, LadowanieDeck = 2f, LadowanieKat = 28f, Wybieg = 15f;
    /// <summary>The landing's slope length (a smooth curve from the knuckle to the ground, steepest ~32 deg).</summary>
    public static float LadowanieDl(Wyrzutnia r) => r.ladowanie / Mathf.Tan(LadowanieKat * Mathf.Deg2Rad) * 1.6f;
    /// <summary>The fastest speed at the lip (real m/s) that still lands on the landing (at its foot).</summary>
    public static float VMax(Wyrzutnia r)
    {
        float a = r.kat * Mathf.Deg2Rad, l = r.luka + LadowanieDeck + LadowanieDl(r), dh = r.h + l * Mathf.Tan(a);
        return Mathf.Sqrt(9.81f * l * l / (2f * Mathf.Cos(a) * Mathf.Cos(a) * Mathf.Max(.01f, dh)));
    }
    /// <summary>The kicker's length (the circular transition: radius h / (1 - cos a), length R sin a).</summary>
    public static float RampaDl(Wyrzutnia r) { float a = r.kat * Mathf.Deg2Rad, R = r.h / (1f - Mathf.Cos(a)); return R * Mathf.Sin(a); }
    /// <summary>The slowest speed at the lip (real m/s) that reaches the landing's knuckle.</summary>
    public static float VMin(Wyrzutnia r)
    {
        float a = r.kat * Mathf.Deg2Rad, dh = r.h + r.luka * Mathf.Tan(a) - r.ladowanie;
        return Mathf.Sqrt(9.81f * r.luka * r.luka / (2f * Mathf.Cos(a) * Mathf.Cos(a) * Mathf.Max(.01f, dh)));
    }
    static float freestyleY = float.NaN;
    public static float FreestyleY { get { if (float.IsNaN(freestyleY)) freestyleY = Naturalna((FX0 + FX1) * .5f, (FZ0 + FZ1) * .5f); return freestyleY; } }
    public static float DoFreestyle(float x, float z) { float dx = Mathf.Max(Mathf.Max(FX0 - x, x - FX1), 0f), dz = Mathf.Max(Mathf.Max(FZ0 - z, z - FZ1), 0f); return Mathf.Sqrt(dx * dx + dz * dz); }

    // ---------------------------------------------------------------- the centre line: fillets, sampled every 0.5 m
    public sealed class Os
    {
        public float[] x, z, tx, tz, kr, s, w; public float dlugosc;     // w: the riding width there (smoothed over 16 m)
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
        var px = new List<float>(); var pz = new List<float>(); var pk = new List<float>(); var pw = new List<float>();
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
        float wr = 0f;
        void Dodaj(Vector2 p, float k) { px.Add(p.x); pz.Add(p.y); pk.Add(k); pw.Add(wr); }
        for (int i = 0; i < n; i++)
        {
            // the arc of corner i (curvature + for a left turn: x east, z north, left = counter-clockwise)
            float R = Rogi[i].z, len = Mathf.Abs(da[i]) * R; int m = Mathf.Max(2, Mathf.CeilToInt(len / krok)); wr = Rogi[i].w;
            for (int k = 0; k < m; k++) { float a = a0[i] + da[i] * k / m; Dodaj(ctr[i] + R * new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Mathf.Sign(da[i]) / R); }
            s += len;
            // the straight to corner i+1
            var b = t2[i]; var c = t1[(i + 1) % n]; float L = Vector2.Distance(b, c); int q = Mathf.Max(1, Mathf.CeilToInt(L / krok));
            prostaOd[i] = s; prostaDo[i] = s + L;
            for (int k = 0; k < q; k++) Dodaj(Vector2.Lerp(b, c, k / (float)q), 0f);
            s += L;
        }
        var o = new Os { x = px.ToArray(), z = pz.ToArray(), kr = pk.ToArray(), prostaOd = prostaOd, prostaDo = prostaDo };
        // the width from its corners, changing over 16 m (the trail narrows into the woods, widens out of them)
        { int N0 = pw.Count, ww = 16; o.w = new float[N0]; for (int i = 0; i < N0; i++) { float sum = 0f; for (int k = -ww; k <= ww; k++) sum += pw[(i + k + N0) % N0]; o.w[i] = sum / (2 * ww + 1); } }
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
    static Vector2 P(Vector4 r) => new Vector2(r.x, r.y);

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
            case Typ.Bloto: return p.n > 0 ? p.n : 26f;
            case Typ.Klody: return (Mathf.Max(1, p.n) - 1) * KlodaOdstep + 2f;
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
            case Typ.Bloto: return -p.h * (t < 4f ? Esa(t / 4f) : t > p.dl - 4f ? 1f - Esa((t - p.dl + 4f) / 4f) : 1f);   // in 4 m, flat, out 4 m
            case Typ.Klody:
            {
                // each log on its dirt ramps: a 2 m bump to the log's top (the ray-cast wheel rolls over it, the log lies on it)
                float h = 0f;
                for (int k = 0; k < Mathf.Max(1, p.n); k++) { float d = Mathf.Abs(t - 1f - k * KlodaOdstep); if (d < 1f) h = Mathf.Max(h, p.h * .5f * (1f + Mathf.Cos(Mathf.PI * d))); }
                return h;
            }
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
    public static float PaddockY { get { if (float.IsNaN(paddockY)) paddockY = Naturalna((PX0 + PX1) * .5f, (PZ0 + PZ1) * .5f) + .05f; return paddockY; } }
    static float Baza(int i)
    {
        if (baza != null) return baza[i];
        var o = Linia(); int N = o.N; var raw = new float[N];
        for (int k = 0; k < N; k++) raw[k] = Naturalna(o.x[k], o.z[k]);
        // the start straight in front of the paddock at the paddock's own level
        for (int k = 0; k < N; k++) if (o.z[k] > 905f && o.x[k] > -260f && o.x[k] < -40f) raw[k] = PaddockY - .1f;
        baza = new float[N]; int w = 50;                             // +-25 m (0.5 m samples), a cosine window: the track climbs the hill
        for (int k = 0; k < N; k++)
        {
            float sum = 0f, ws = 0f;
            for (int j = -w; j <= w; j++) { float q = .5f * (1f + Mathf.Cos(Mathf.PI * j / w)); sum += raw[(k + j + N) % N] * q; ws += q; }
            baza[k] = sum / ws;
        }
        return baza[i];
    }
    public static int Indeks(float s) { var o = Linia(); s = ((s % o.dlugosc) + o.dlugosc) % o.dlugosc; int lo = 0, hi = o.N - 1; while (lo < hi) { int m = (lo + hi + 1) / 2; if (o.s[m] <= s) lo = m; else hi = m - 1; } return lo; }
    /// <summary>The berm: tan of the bank at sample i (+ the outer edge is to the right for a left turn).</summary>
    static float Przechyl(int i) { var o = Linia(); float k = o.kr[i]; return Mathf.Sign(k) * Mathf.Tan(13f * Mathf.Deg2Rad) * Mathf.Clamp01(Mathf.Abs(k) * 18f); }
    /// <summary>The speed a turn at the line's sample i holds (m/s: its radius, its berm's bank, the tyres' grip).</summary>
    public static float VZakretu(int i)
    {
        var o = Linia(); float kr = Mathf.Abs(o.kr[i]), tb = Mathf.Abs(Przechyl(i));
        return kr > 1e-4f ? Mathf.Min(VProsta, Mathf.Sqrt(9.81f * (MuWyscigu + tb) / Mathf.Max(.2f, 1f - MuWyscigu * tb) / kr)) : VProsta;
    }
    /// <summary>The race's riding speed (m/s) at the line's sample i (ProfilWyscigu: the turns, the obstacles, the braking).</summary>
    public static float VNaLinii(int i)
    {
        var o = Linia(); var pr = ProfilWyscigu(); int n = pr.v.Length; float sG = o.s[IndeksBramki()];
        return pr.v[Mathf.RoundToInt((((o.s[i] - sG) % o.dlugosc) + o.dlugosc) % o.dlugosc / KrokWyscigu) % n];
    }
    /// <summary>The riding surface at sample i, u metres to the left of the centre line (the shoulders continue it).</summary>
    public static float Tor(int i, float u)
    {
        var o = Linia(); float b = Przechyl(i);
        return Baza(i) + Profil(o.s[i]) + Mathf.Abs(b) * (o.w[i] * .5f - Mathf.Sign(b) * u);
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
    public static float Smooth(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }
    public static float DoKrawedzi(float x, float z) => Mathf.Min(Mathf.Min(x - X0, X1 - x), Mathf.Min(z - Z0, Z1 - z));
    public static bool WTorze(float x, float z) => x > X0 && x < X1 && z > Z0 && z < Z1;
    public static float DoPaddocku(float x, float z) { float dx = Mathf.Max(Mathf.Max(PX0 - x, x - PX1), 0f), dz = Mathf.Max(Mathf.Max(PZ0 - z, z - PZ1), 0f); return Mathf.Sqrt(dx * dx + dz * dz); }
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
        float h = Naturalna(x, z);
        ziemia = -1f;
        float dp = DoPaddocku(x, z);
        if (dp < 14f) h = Mathf.Lerp(PaddockY - .3f, h, Smooth(0f, 14f, dp));
        // the freestyle zone flat (its ramps' gaps are worked out on level ground), its run-up lanes bare earth
        float df = DoFreestyle(x, z);
        if (df < 15f) h = Mathf.Lerp(FreestyleY, h, Smooth(0f, 15f, df));
        if (df < 6f) ziemia = Mathf.Max(ziemia, 1f - df / 6f);            // the whole zone bare earth (an FMX arena), a soft edge
        float y = Droga(x, z, out float dr);
        if (dr < DrogaPol + 1f + 8f) h = Mathf.Lerp(y - .3f, h, Smooth(0f, 8f, dr - DrogaPol - 1f));
        // at the city's edge (x 0) down to its grass, where the far land is pressed (along the road): no step, no gap
        if (dr <= 35f && x > -15f) h = Mathf.Lerp(.19f, h, Smooth(0f, 15f, -x));
        if (Najblizszy(x, z, 26f, out int i, out float u))
        {
            float e = Mathf.Abs(u), brzeg = Linia().w[i] * .5f + Pobocze, nat = h;
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
    static Material nawierzchnia, pobocze, trawa, szuter, woda, beton, drewno, stal, czern, zloto, biel, pomarancz, czerwien, szyba, led, siedzisko, bloto, kora, sciolka;
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
        bloto = BraxChampionsFoundation.SourceMaterial("tor_kladow_bloto", "aerial_mud_1", 8f, new Color(1.06f, .86f, .68f));   // dark wet mud, its ruts (8 m), warmed to the track's brown
        kora = BraxChampionsFoundation.SourceMaterial("tor_kladow_kora", "pine_bark", 2f, Color.white);   // the logs' pine bark (2 m)
        sciolka = BraxChampionsFoundation.SourceMaterial("tor_kladow_sciolka", "forest_floor", 2.14f, new Color(.66f, .63f, .58f));   // the pine forest's needles and cones (2.14 m), its sunlit scan taken down to the shade of a pine wood
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
        var pyl = BlotoIPyl(g);
        DrogaIPaddock(g);
        var b = new BraxChampionsMeshBatch();
        Tyczki(b);
        Plot(b);
        Freestyle(g, b);
        var czas = Paddock(g, b);
        Rywali = Belek = 0;
        var wyscig = Wyscig(g, b);
        b.Build(g, "TOR DO KŁADÓW | detale", Folder, true, K);
        Trojkaty += b.Triangles;
        BraxChampionsLasKladow.Zbuduj(g);                            // the forest (owner 14:20 "więcej lasu")
        Trojkaty += BraxChampionsLasKladow.Trojkaty;
        BraxChampionsOponyKladow.Zbuduj(g);                          // the tyres at the turns, the tyre walls, the tape (17:11)
        Trojkaty += BraxChampionsOponyKladow.Trojkaty;
        W.Zapisz("mistrzowie_tor_kladow");
        Kolizje = W.Kolizje - k0;
        // the far land goes under the site too (with the villas' press)
        var poprzedni = BraxChampionsHorizon.Docisk;
        BraxChampionsHorizon.Docisk = (x, z) => Mathf.Min(poprzedni != null ? poprzedni(x, z) : float.PositiveInfinity, Docisk(x, z));
        Przeszkod = Przeszkody.Length;
        Debug.Log("MISTRZOWIE_TOR_KLADOW length=" + o.dlugosc.ToString("F0") + "m width=" + Szer + "m obstacles=" + Przeszkod + " quads=" + Klady +
                  " triangles=" + Trojkaty + " colliders=" + Kolizje + " paddockY=" + PaddockY.ToString("F2") + " timer=" + (czas ? czas.punkty.Length : 0) +
                  " race=" + (wyscig ? wyscig.punkty.Length : 0) + " rivals=" + Rywali + " mud=" + (pyl ? pyl.srodek.Length : 0) +
                  " trees=" + BraxChampionsLasKladow.Drzew + " young=" + BraxChampionsLasKladow.Mlodych + " tyres=" + BraxChampionsOponyKladow.Opon);
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
                pod[q] = (Najblizszy(x, z, 8f, out int iu, out float u) && Mathf.Abs(u) < Linia().w[iu] * .5f - .5f) || (x > PX0 + .5f && x < PX1 - .5f && z > PZ0 + .5f && z < PZ1 - .5f);
            }
        // grass and bare earth (the track's fills, cuts and its edge) over the same vertices, the line between them cut exactly
        // where the field crosses zero (as the villas' grass / sand), the cut points shared: one closed surface; then the
        // grass split the same way by the forest (BraxChampionsLasKladow.Las over 0.45): its floor of needles under the trees
        var vl = new List<Vector3>(v); var uvl = new List<Vector2>(uv);
        var tg = new List<int>(); var tz = new List<int>(); var tl = new List<int>();
        List<float> pl = new List<float>(pole); var ciecia = new Dictionary<long, int>();
        int Ciecie(int p, int q)
        {
            long key = p < q ? (long)p * 4000000L + q : (long)q * 4000000L + p;
            if (ciecia.TryGetValue(key, out int r)) return r;
            float t = Mathf.Clamp01(pl[p] / (pl[p] - pl[q]));
            vl.Add(Vector3.Lerp(vl[p], vl[q], t)); uvl.Add(Vector2.Lerp(uvl[p], uvl[q], t)); pl.Add(0f);
            return ciecia[key] = vl.Count - 1;
        }
        void Trojkat(List<int> ujemne, List<int> dodatnie, int p0, int p1, int p2)
        {
            bool g0 = pl[p0] < 0f, g1 = pl[p1] < 0f, g2 = pl[p2] < 0f;
            if (g0 == g1 && g1 == g2) { var l = g0 ? ujemne : dodatnie; l.Add(p0); l.Add(p1); l.Add(p2); return; }
            int A, B, C; bool gA;
            if (g0 != g1 && g0 != g2) { A = p0; B = p1; C = p2; gA = g0; }
            else if (g1 != g0 && g1 != g2) { A = p1; B = p2; C = p0; gA = g1; }
            else { A = p2; B = p0; C = p1; gA = g2; }
            int ab = Ciecie(A, B), ac = Ciecie(A, C);
            var la = gA ? ujemne : dodatnie; var lo = gA ? dodatnie : ujemne;
            la.Add(A); la.Add(ab); la.Add(ac);
            lo.Add(ab); lo.Add(B); lo.Add(C); lo.Add(ab); lo.Add(C); lo.Add(ac);
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                int a = i * (nz + 1) + j, bq = (i + 1) * (nz + 1) + j, c = a + 1, e = bq + 1;
                if (pod[a] && pod[bq] && pod[c] && pod[e]) continue;
                Trojkat(tg, tz, a, c, bq); Trojkat(tg, tz, c, e, bq);
            }
        // the forest's floor: the grass's triangles cut again where the forest's field crosses its edge
        pl = new List<float>(vl.Count); ciecia.Clear();
        foreach (var q in vl) pl.Add(BraxChampionsLasKladow.Las(q.x / K, q.z / K) - .45f);   // under 0 the grass, over it the forest
        var trawy = tg; tg = new List<int>();
        for (int k = 0; k + 2 < trawy.Count; k += 3) Trojkat(tg, tl, trawy[k], trawy[k + 1], trawy[k + 2]);
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
        Zapisz(g, "TOR DO KŁADÓW | grunt: ściółka lasu | kolizja", "tor_kladow_grunt_sciolka", va, nrm, ua, tl, sciolka, true);
    }

    // the riding surface and its shoulders: a ribbon along the line (0.5 m steps in the obstacles, 1 m in the turns, 2 m on
    // the plain straights), u across in 1 m; UVs along the track (the ruts run with the riding), a skirt under each edge
    static void Wstega(Transform g)
    {
        var o = Linia();
        // the columns: the shoulders' outer edges, the track's edges and quarters of its half width (each row its own width)
        float[] fr = { 1f, 1f, .75f, .5f, .25f, 0f, -.25f, -.5f, -.75f, -1f, -1f };
        float U(int c, int i) { float hw = o.w[i] * .5f; return c == 0 ? hw + Pobocze : c == fr.Length - 1 ? -hw - Pobocze : fr[c] * hw; }
        var rzedy = new List<int>(); float ostatni = -99f;
        for (int i = 0; i < o.N; i++)
        {
            float s = o.s[i]; bool w = false;
            foreach (var p in ulozone) if (s > p.s0 - 2f && s < p.s0 + p.dl + 2f) { w = true; break; }
            float krok = w ? .5f : Mathf.Abs(o.kr[i]) > .004f ? 1f : 2f;
            if (s - ostatni >= krok - 1e-3f) { rzedy.Add(i); ostatni = s; }
        }
        rzedy.Add(0);                                                  // closed: the last row is the first
        int nu = fr.Length; var rowI = new List<int>();
        var vT = new List<Vector3>(); var nT = new List<Vector3>(); var uvT = new List<Vector2>(); var tT = new List<int>(); var tP = new List<int>(); var tS = new List<int>(); var tB = new List<int>();
        var rowS = new List<float>();
        // the mud's edges: ragged across the track (as a real pit's are), 0.1 - 3.9 m in from its ends
        bool WBlocie(int r, int c)
        {
            float sm = (rowS[r] + rowS[r + 1]) * .5f, um = (U(c, rowI[r]) + U(c + 1, rowI[r])) * .5f;
            foreach (var p in ulozone)
                if (p.typ == Typ.Bloto && sm > p.s0 + 2f + 1.3f * Mathf.Sin(um * 1.3f + .5f) + .6f * Mathf.Sin(um * 3.1f)
                                       && sm < p.s0 + p.dl - 2f - 1.2f * Mathf.Sin(um * 1.1f + 2f) - .5f * Mathf.Sin(um * 2.7f)) return true;
            return false;
        }
        for (int r = 0; r < rzedy.Count; r++)
        {
            int i = rzedy[r]; float s = r == rzedy.Count - 1 ? o.dlugosc : o.s[i];
            float lx = -o.tz[i], lz = o.tx[i];
            rowS.Add(s); rowI.Add(i);
            // each column's normal from the surface's own slope along and across there (the split's two lines differ)
            int ia = (i + o.N - 2) % o.N, ib = (i + 2) % o.N; float ds = 1f;
            for (int c = 0; c < nu; c++)
            {
                float u = U(c, i);
                float dAlong = (Tor(ib, u) - Tor(ia, u)) / (2f * ds), dAcross = Tor(i, u + .5f) - Tor(i, u - .5f);
                var tan = new Vector3(o.tx[i], dAlong, o.tz[i]).normalized; var lew = new Vector3(lx, dAcross, lz).normalized;
                var n = Vector3.Cross(tan, lew).normalized; if (n.y < 0f) n = -n;
                // UVs in metres (the materials tile by their real size: muddy_tracks 2.25 m, the mud 8 m)
                vT.Add(new Vector3(o.x[i] + lx * u, Tor(i, u), o.z[i] + lz * u) * K); nT.Add(n); uvT.Add(new Vector2(u, s));
            }
            // the skirts: from each edge 0.6 m down (no gap to the ground under any angle)
            foreach (int c in new[] { 0, nu - 1 })
            {
                // lit as the earth bank it stands for (out and up), not as a dark wall
                var top = vT[r * (nu + 2) + c]; vT.Add(top - Vector3.up * .6f * K); nT.Add(new Vector3((c == 0 ? lx : -lx) * .6f, .8f, (c == 0 ? lz : -lz) * .6f)); uvT.Add(new Vector2(U(c, i) + (c == 0 ? .6f : -.6f), s));
            }
        }
        int st = nu + 2;
        for (int r = 0; r + 1 < rzedy.Count; r++)
        {
            int a = r * st, b = (r + 1) * st;
            for (int c = 0; c + 1 < nu; c++)
            {
                                // facing up: start -> ahead -> right (the villas' grid winding; u falls with c: c+1 is to the right)
                var lista = (c == 0 || c == nu - 2) ? tP : WBlocie(r, c) ? tB : tT;
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
        Zapisz(g, "TOR DO KŁADÓW | błoto (mokre, koleiny) | kolizja", "tor_kladow_bloto", v, nn, uv, tB, bloto, true);
        tP.Clear(); tP.AddRange(tS);
        Zapisz(g, "TOR DO KŁADÓW | skarpy pod krawędzią", "tor_kladow_skarpy", v, nn, uv, tP, pobocze, false);
    }

    // the mud pits for the quads (BraxChampionsBloto: their grip and drag) and the wheels' dust and mud clumps (soft billboards:
    // the CityStaticPBR's blend with the particles' own colour and fade in their vertices)
    static BraxChampionsBloto BlotoIPyl(Transform g)
    {
        var o = Linia();
        var c = new GameObject("TOR DO KŁADÓW | błoto i pył spod kół").AddComponent<BraxChampionsBloto>();
        c.transform.SetParent(g, false); c.K = K;
        var sr = new List<Vector3>(); var wz = new List<Vector3>(); var dl = new List<float>(); var sz = new List<float>();
        foreach (var p in ulozone)
        {
            if (p.typ != Typ.Bloto) continue;
            int i = Indeks(p.s0 + p.dl * .5f);
            sr.Add(new Vector3(o.x[i], Tor(i, 0f), o.z[i]) * K); wz.Add(new Vector3(o.tx[i], 0f, o.tz[i]));
            dl.Add((p.dl * .5f - 2f) * K); sz.Add((o.w[i] * .5f + Pobocze) * K);      // within its ragged ends
        }
        c.srodek = sr.ToArray(); c.wzdluz = wz.ToArray(); c.polDl = dl.ToArray(); c.polSzer = sz.ToArray();
        c.pyl = BraxChampionsHarbour.Surface("tor_kladow_pyl", Color.white, 1f, 0f, null, true);
        c.grudki = BraxChampionsHarbour.Surface("tor_kladow_grudki", Color.white, .35f, 0f, null, true);   // wet: a little gloss
        foreach (var m in new[] { c.pyl, c.grudki }) { m.SetFloat("_VertexTint", 1f); EditorUtility.SetDirty(m); }
        return c;
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
            float half = o.w[Indeks(p.s0 + p.dl * .5f)] * .5f + Pobocze + 1.4f;
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
    /// <summary>The stakes along each side (left, right) in the order the track runs; a NaN where the row breaks (the
    /// woods, the finish line, the paddock): the tape (BraxChampionsOponyKladow) runs between neighbours.</summary>
    public static readonly List<Vector3>[] Paliki = { new List<Vector3>(), new List<Vector3>() };
    static void Tyczki(BraxChampionsMeshBatch b)
    {
        var o = Linia(); float nast = 0f; int n = 0; var przerwa = new Vector3(float.NaN, 0f, 0f);
        Paliki[0].Clear(); Paliki[1].Clear();
        for (int i = 0; i < o.N; i++)
        {
            if (o.s[i] < nast) continue; nast = o.s[i] + 8f;
            if (Mathf.Abs(o.x[i] - MetaX) < 6f && o.z[i] > 905f) { Paliki[0].Add(przerwa); Paliki[1].Add(przerwa); continue; }
            if (o.w[i] < 6f) { Paliki[0].Add(przerwa); Paliki[1].Add(przerwa); continue; }      // in the woods the trees mark the trail
            foreach (float bok in new[] { 1f, -1f })
            {
                float u = bok * (o.w[i] * .5f + Pobocze + .5f), x = o.x[i] - o.tz[i] * u, z = o.z[i] + o.tx[i] * u, y = Grunt(x, z);
                var lista = Paliki[bok > 0f ? 0 : 1];
                if (DoPaddocku(x, z) < 1f) { lista.Add(przerwa); continue; }
                lista.Add(new Vector3(x, y, z));
                b.Box(n % 2 == 0 ? pomarancz : biel, new Vector3(x, y + .4f, z), new Vector3(.05f, .8f, .05f));
                b.Box(n % 2 == 0 ? biel : pomarancz, new Vector3(x, y + .84f, z), new Vector3(.09f, .14f, .09f));                 // the cap 2 cm proud of the stake (no shared faces)
            }
            n++;
        }
        // the logs across the woods' trail (pine bark), each sunk in its dirt ramps, its top their crest
        foreach (var p in ulozone)
        {
            if (p.typ != Typ.Klody) continue;
            for (int k = 0; k < Mathf.Max(1, p.n); k++)
            {
                int i = Indeks(p.s0 + 1f + k * KlodaOdstep);
                float hw = o.w[i] * .5f + Pobocze + .6f, r = KlodaR * (1f + .08f * (k % 3));
                var lew = new Vector3(-o.tz[i], 0f, o.tx[i]);
                var c0 = new Vector3(o.x[i], Tor(i, 0f) - r, o.z[i]) - lew * hw;
                b.Cylinder(kora, c0, r, 2f * hw, 10, Quaternion.FromToRotation(Vector3.up, lew), true, true, r * .92f);
                Klod++;
            }
        }
    }
    public static int Klod;

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

    // the freestyle zone: per lane the FMX kicker (its deck a collider, its underside, the painted lip, an open steel frame
    // under it) and the dirt landing (one mesh with its 1:1 sides sunk into the ground, its collider); at each run-up's start
    // a board with the lane's window of speeds; the sign over the zone
    static void Freestyle(Transform g, BraxChampionsMeshBatch b)
    {
        float y0 = FreestyleY; int nr = 0;
        foreach (var r in Rampy)
        {
            nr++;
            float a = r.kat * Mathf.Deg2Rad, R = r.h / (1f - Mathf.Cos(a)), L = RampaDl(r), x0 = FLip - L, hw = RampaSzer * .5f;
            float Prof(float t) => R - Mathf.Sqrt(Mathf.Max(0f, R * R - t * t));     // t from the kicker's foot
            // the deck: 0.25 m steps along, a mesh with its collider, and its underside 6 cm below (an open frame shows it)
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            var vd = new List<Vector3>(); var nd = new List<Vector3>(); var td = new List<int>();
            int m = Mathf.CeilToInt(L / .25f);
            for (int i = 0; i <= m; i++)
            {
                float tt = L * i / m, yy = y0 + Prof(tt), sl = tt / Mathf.Sqrt(Mathf.Max(1e-4f, R * R - tt * tt));
                var nn = new Vector3(-sl, 1f, 0f).normalized;
                foreach (float zz in new[] { r.z - hw, r.z + hw })
                {
                    v.Add(new Vector3(x0 + tt, yy + .02f, zz) * K); n.Add(nn); uv.Add(new Vector2(tt / 1.5f, (zz - r.z) / 1.5f));
                    vd.Add(new Vector3(x0 + tt, yy - .04f, zz) * K); nd.Add(-nn);
                }
            }
            for (int i = 0; i < m; i++) { int q = i * 2; t.AddRange(new[] { q, q + 1, q + 2, q + 1, q + 3, q + 2 }); td.AddRange(new[] { q, q + 2, q + 1, q + 1, q + 2, q + 3 }); }
            Zapisz(g, "TOR DO KŁADÓW | freestyle rampa " + nr + " | kolizja", "tor_kladow_rampa_" + nr, v.ToArray(), n.ToArray(), uv.ToArray(), t, stal, true);
            Zapisz(g, "TOR DO KŁADÓW | freestyle rampa " + nr + " | spód", "tor_kladow_rampa_" + nr + "_spod", vd.ToArray(), nd.ToArray(), uv.ToArray(), td, czern, false);
            // the deck's edges (6 cm plates) and the lip painted orange across its last 0.5 m
            for (int i = 0; i < m; i++)
            {
                float t0 = L * i / m, t1 = L * (i + 1) / m;
                Vector3 A(float tt, float zz, bool gora) => new Vector3(x0 + tt, y0 + Prof(tt) + (gora ? .02f : -.04f), zz);
                float zl = r.z - hw, zr = r.z + hw;
                b.Quad(czern, A(t0, zl, false), A(t0, zl, true), A(t1, zl, true), A(t1, zl, false));   // facing -z (outward)
                b.Quad(czern, A(t1, zr, false), A(t1, zr, true), A(t0, zr, true), A(t0, zr, false));   // facing +z (outward)
                if (t1 > L - .5f)
                {
                    Vector3 P(float tt, float zz) => new Vector3(x0 + tt, y0 + Prof(tt) + .035f, zz);
                    b.Quad(pomarancz, P(t0, zl + .05f), P(t0, zr - .05f), P(t1, zr - .05f), P(t1, zl + .05f));
                }
            }
            // the frame: posts every 1.2 m both sides down to the ground, diagonal braces between them, the lip's two legs
            // and their cross brace
            var slupki = new List<float>(); for (float tt = .9f; tt < L - .3f; tt += 1.2f) slupki.Add(tt); slupki.Add(L - .06f);
            foreach (float zz in new[] { r.z - hw + .12f, r.z + hw - .12f })
            {
                for (int k = 0; k < slupki.Count; k++)
                {
                    float tt = slupki[k], top = y0 + Prof(tt) - .04f;
                    b.BoxMinMax(stal, x0 + tt - .05f, x0 + tt + .05f, y0 - .1f, top, zz - .05f, zz + .05f);
                    if (k == 0) continue;
                    var p0 = new Vector3(x0 + slupki[k - 1], y0 + .05f, zz); var p1 = new Vector3(x0 + tt, top - .05f, zz);
                    var d = p1 - p0; b.Box(stal, (p0 + p1) * .5f, new Vector3(.035f, d.magnitude, .035f), Quaternion.FromToRotation(Vector3.up, d));
                }
            }
            b.BoxMinMax(stal, FLip - .1f, FLip - .02f, y0 + .25f, y0 + .35f, r.z - hw + .12f, r.z + hw - .12f);       // the lip legs' cross brace
            W.Kolizja(new Vector3(x0 + L * .5f, y0 + r.h * .3f, r.z) * K, new Vector3(L * .7f, r.h * .6f, RampaSzer) * K, Quaternion.identity);
            BraxChampionsLetters3D.Text(b, zloto, czern, nr.ToString(), new Vector3(FLip - .3f, y0 + r.h * .45f, r.z - hw - .1f), 180f, .6f, .05f, .12f, 0f);
            // the board at the run-up's start: the lane's window of speeds (km/h at the lip)
            float bx = x0 - FBieg + 3f, bz = r.z + hw + 3.5f;
            foreach (float px in new[] { bx - .9f, bx + .9f }) b.Cylinder(stal, new Vector3(px, y0, bz), .05f, 2.1f, 8);
            b.BoxMinMax(czern, bx - 1.2f, bx + 1.2f, y0 + 1.2f, y0 + 2.0f, bz - .04f, bz + .04f);
            BraxChampionsLetters3D.Text(b, zloto, czern, Mathf.RoundToInt(VMin(r) * 3.6f / 5f) * 5 + "-" + Mathf.FloorToInt(VMax(r) * 3.6f / 5f) * 5 + " KM/H", new Vector3(bx, y0 + 1.42f, bz - .06f), 180f, .32f, .03f, .12f, 2.2f);
            // the landing: its 1:1 face, the 2 m knuckle, the long landing; 1:1 sides; under the ground past its foot
            float lx0 = FLip + r.luka - r.ladowanie, lx1 = FLip + r.luka, lx2 = lx1 + LadowanieDeck, lx3 = lx2 + LadowanieDl(r);
            float Ld(float x)
            {
                if (x < lx0 || x > lx3) return -.3f;
                if (x < lx1) return r.ladowanie * Esa((x - lx0) / (lx1 - lx0)) * .98f + .02f;
                if (x < lx2) return r.ladowanie;
                return r.ladowanie * (1f - Esa((x - lx2) / (lx3 - lx2)));
            }
            var lv = new List<Vector3>(); var ln = new List<Vector3>(); var lu = new List<Vector2>(); var lt = new List<int>();
            float hl = LadowanieSzer * .5f, side = r.ladowanie + 1f; int nxL = Mathf.CeilToInt((lx3 - lx0 + 2f) / .5f), nu = Mathf.CeilToInt((2f * (hl + side)) / .5f);
            float H(float x, float zz) => Mathf.Max(-.3f, Ld(x) - Mathf.Max(0f, Mathf.Abs(zz - r.z) - hl));
            for (int i = 0; i <= nxL; i++)
                for (int j = 0; j <= nu; j++)
                {
                    float x = lx0 - 1f + i * .5f, zz = r.z - hl - side + j * .5f, yy = H(x, zz);
                    float gx = H(x + .25f, zz) - H(x - .25f, zz), gz = H(x, zz + .25f) - H(x, zz - .25f);
                    lv.Add(new Vector3(x, y0 + yy, zz) * K); ln.Add(new Vector3(-gx * 2f, 1f, -gz * 2f).normalized); lu.Add(new Vector2(x, zz));
                }
            for (int i = 0; i < nxL; i++)
                for (int j = 0; j < nu; j++)
                {
                    int q = i * (nu + 1) + j, qa = (i + 1) * (nu + 1) + j;
                    if (lv[q].y < (y0 - .29f) * K && lv[q + 1].y < (y0 - .29f) * K && lv[qa].y < (y0 - .29f) * K && lv[qa + 1].y < (y0 - .29f) * K) continue;
                    lt.AddRange(new[] { q, q + 1, qa, q + 1, qa + 1, qa });           // (x rising, z rising): the villas' winding, facing up
                }
            Zapisz(g, "TOR DO KŁADÓW | freestyle lądowisko " + nr + " | kolizja", "tor_kladow_ladowisko_" + nr, lv.ToArray(), ln.ToArray(), lu.ToArray(), lt, pobocze, true);
        }
        // the sign: BRAXHORN FREESTYLE on a black board on two posts at the zone's north edge, facing the lanes
        float sx = (FX0 + FX1) * .5f, sz = FZ1 - 7f;
        foreach (float px in new[] { sx - 5.5f, sx + 5.5f }) { b.Cylinder(stal, new Vector3(px, y0, sz), .09f, 4.75f, 10); W.Kolizja(new Vector3(px, y0 + 2.375f, sz) * K, new Vector3(.25f, 4.75f, .25f) * K, Quaternion.identity); }   // the posts' tops over the board
        b.BoxMinMax(czern, sx - 6f, sx + 6f, y0 + 3f, y0 + 4.6f, sz - .06f, sz + .06f);
        BraxChampionsLetters3D.Text(b, zloto, czern, "BRAXHORN FREESTYLE", new Vector3(sx, y0 + 3.45f, sz - .08f), 180f, .7f, .06f, .12f, 11f);
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
            b.BoxMinMax(czern, fx, fx + .75f, y + 1.45f, y + 4.1f, fz - 2.02f, fz - 1.98f);                          // over the orange band (no shared faces)
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
        int im = IndeksMety();
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
        // the start gate: a concrete pad (the five places behind the bars, the bars lying on it in front when no race is on)
        // and the steel hinge beam across the track; the five drop bars move (the race: Wyscig)
        int ig = IndeksBramki();
        float gy = Tor(ig, 0f), gzc = o.z[ig];
        b.BoxMinMax(beton, BramkaX - .7f, BramkaX + 2.6f, gy - .2f, gy + .08f, gzc - Szer * .5f, gzc + Szer * .5f);
        b.BoxMinMax(stal, BramkaX - .14f, BramkaX - .04f, gy + .08f, gy + .12f, gzc - Szer * .5f + .02f, gzc + Szer * .5f - .02f);
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

    // ---------------------------------------------------------------- the race (owner 24.11 12:57: "wyścig z rywalami")
    static int NaProstejStartu(float x) { var o = Linia(); int best = 0; float bd = float.MaxValue; for (int i = 0; i < o.N; i++) { float d = Mathf.Abs(o.x[i] - x) + (o.z[i] > 905f ? 0f : 1000f); if (d < bd) { bd = d; best = i; } } return best; }
    public static int IndeksBramki() => NaProstejStartu(BramkaX);
    public static int IndeksMety() => NaProstejStartu(MetaX);

    // the riding speed along the line (real m/s): a corner from its radius and its berm (the friction of the dirt 0.7 with the
    // 13 deg bank: v^2 = g R (mu + tan b) / (1 - mu tan b)), the obstacles' own speeds, the braking (5 m/s2) for what comes
    public const float KrokWyscigu = 2f, MuWyscigu = .7f, VProsta = 23f, HamowanieAI = 5f, RozpedAI = 2f, VToczenie = 7f;
    public const int Okrazen = 2;                                   // the loop is 2.5 km: two laps, ~8 min
    /// <summary>A double's jump speed (real m/s): the take-off's lip angle (its face: Rampa, 6 m), the flight from the lip over
    /// the gap and the second one's top to 4 m down its landing (the middle of the safe range).</summary>
    public static float VSkoku(Przeszkoda p)
    {
        float kat = (float)Math.Atan(p.h / 6f / (1f - .15f));
        float dl = .4f + 5f + 4f + 5f + .4f + 4f;
        return Mathf.Sqrt(9.81f * dl / Mathf.Sin(2f * kat));
    }
    public sealed class ProfilW { public Vector3[] p, lewo; public float[] v, szer; public bool[] skok; public float sMety; public List<string> doubles = new List<string>(); }
    static ProfilW profilW;
    public static ProfilW ProfilWyscigu()
    {
        if (profilW != null) return profilW;
        var o = Linia(); float L = o.dlugosc; int ig = IndeksBramki(), im = IndeksMety();
        float sG = o.s[ig]; int n = Mathf.FloorToInt(L / KrokWyscigu);
        var w = new ProfilW { p = new Vector3[n], lewo = new Vector3[n], v = new float[n], szer = new float[n], skok = new bool[n] };
        w.sMety = ((o.s[im] - sG) % L + L) % L;
        var sl = new float[n];
        float T(float s, Przeszkoda q) { float t = s - q.s0; if (t > L * .5f) t -= L; if (t < -L * .5f) t += L; return t; }
        for (int k = 0; k < n; k++)
        {
            float s = (sG + k * KrokWyscigu) % L; sl[k] = s; int i = Indeks(s);
            w.p[k] = new Vector3(o.x[i], Tor(i, 0f), o.z[i]); w.lewo[k] = new Vector3(-o.tz[i], 0f, o.tx[i]); w.szer[k] = o.w[i];
            float kr = Mathf.Abs(o.kr[i]), tb = Mathf.Abs(Przechyl(i)), v = VProsta;
            if (kr > 1e-4f) v = Mathf.Min(v, Mathf.Sqrt(9.81f * (MuWyscigu + tb) / Mathf.Max(.2f, 1f - MuWyscigu * tb) / kr));
            foreach (var q in ulozone)
            {
                float t = T(s, q);
                switch (q.typ)
                {
                    case Typ.Stol: if (t > -20f && t < q.dl) v = Mathf.Min(v, q.h >= 1.8f ? 17f : 12.5f); break;     // the big one cleared, the small one cased
                    case Typ.Podwojna: if (t > -35f && t < q.dl) { w.skok[k] = true; v = Mathf.Min(v, VSkoku(q)); } break;
                    case Typ.Schodek: if (t > -15f && t < q.dl) v = Mathf.Min(v, 11f); break;
                    case Typ.Fale: if (t > -10f && t < q.dl) v = Mathf.Min(v, 12f); break;
                    case Typ.Muldy: if (t > -8f && t < q.dl) v = Mathf.Min(v, 9f); break;
                    case Typ.Gorka: if (t > 30f && t < q.dl) v = Mathf.Min(v, 16f); break;
                    case Typ.Brod: if (t > -10f && t < q.dl + 5f) v = Mathf.Min(v, 8f); break;
                    case Typ.Bloto: if (t > -10f && t < q.dl) v = Mathf.Min(v, 8f); break;
                    case Typ.Klody: if (t > -8f && t < q.dl) v = Mathf.Min(v, 5f); break;      // over the logs at a walk
                }
            }
            w.v[k] = v;
        }
        Hamuj(w.v);
        // a double the slower quad cannot get up to its speed for (out of a turn): rolled over (up, down, up, down) instead
        var r = new float[n]; float prev = w.v[n - 1];
        for (int pass = 0; pass < 2; pass++) for (int k = 0; k < n; k++) { r[k] = Mathf.Min(w.v[k], Mathf.Sqrt(prev * prev + 2f * RozpedAI * KrokWyscigu)); prev = r[k]; }
        foreach (var q in ulozone)
        {
            if (q.typ != Typ.Podwojna) continue;
            int kf = -1; for (int k = 0; k < n; k++) { float t = T(sl[k], q); if (t >= 0f && t < KrokWyscigu) { kf = k; break; } }
            if (kf < 0) continue;
            bool skacze = r[kf] >= VSkoku(q) - .5f;
            w.doubles.Add("PODWOJNA h=" + q.h + " s0=" + q.s0.ToString("F0") + " v_skoku=" + (VSkoku(q) * 3.6f).ToString("F0") + "km/h rozped=" + (r[kf] * 3.6f).ToString("F0") + "km/h " + (skacze ? "SKACZE" : "TOCZY"));
            if (skacze) continue;
            for (int k = 0; k < n; k++) { float t = T(sl[k], q); if (t > -35f && t < q.dl) w.v[k] = Mathf.Min(w.v[k], VToczenie); }
        }
        Hamuj(w.v);
        return profilW = w;
    }
    static void Hamuj(float[] v)
    {
        int n = v.Length;
        for (int pass = 0; pass < 2; pass++) for (int k = n - 1; k >= 0; k--) v[k] = Mathf.Min(v[k], Mathf.Sqrt(v[(k + 1) % n] * v[(k + 1) % n] + 2f * HamowanieAI * KrokWyscigu));
    }

    // the four rivals (BraxChampionsKladRywal): two sport Raptors, two utility Ranchers, each rider in his quad's colours; in
    // the gate's places 1, 2, 4, 5 (the player in the middle), each keeping to his side of the track (its lane, metres left)
    public static readonly (string model, string kolor, string imie, int numer, Color stroj, Color akcent, float umiejetnosc, float pas)[] Rywale =
    {
        ("raptor", "pomaranczowy", "SZYMON", 11, new Color(1f, .42f, .02f), new Color(.05f, .05f, .05f), .95f, 2.2f),
        ("raptor", "fioletowy", "KAMIL", 27, new Color(.38f, .16f, .66f), new Color(.92f, .92f, .9f), .91f, 1.0f),
        ("rancher", "niebieski", "WOJTEK", 5, new Color(.08f, .2f, .65f), new Color(.92f, .92f, .9f), .97f, -1.0f),
        ("rancher", "bialy", "DAMIAN", 63, new Color(.92f, .92f, .9f), new Color(.75f, .06f, .05f), .9f, -2.2f),
    };
    public const float ParkX = -100f, ParkZ = 962f, ParkKrok = 4.5f;            // the riders' park in the paddock, by the start
    public const float TablicaX = -118f, TablicaZ = 888f, TablicaW = 10f, TablicaH = 5f, TablicaNad = 3f;   // the LED board in the infield
    public static int Rywali, Belek;

    // a part that moves (a gate bar): its own mesh about its hinge, nothing of it static
    static Transform Ruchomy(Transform g, string nazwa, Vector3 piwot, Action<BraxChampionsMeshBatch> rysuj)
    {
        var p = new GameObject(nazwa).transform; p.SetParent(g, false); p.localPosition = piwot * K;
        var bb = new BraxChampionsMeshBatch(); rysuj(bb);
        bb.Build(p, nazwa + " | siatka", Folder, true, K);
        foreach (var tr in p.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);
        Trojkaty += bb.Triangles;
        return p;
    }

    static BraxChampionsWyscig Wyscig(Transform g, BraxChampionsMeshBatch b)
    {
        var o = Linia(); var pr = ProfilWyscigu(); float y = PaddockY;
        var w = new GameObject("TOR DO KŁADÓW | wyścig z rywalami (3 okrążenia)").AddComponent<BraxChampionsWyscig>();
        w.transform.SetParent(g, false);
        int n = pr.p.Length;
        w.punkty = new Vector3[n]; w.lewo = new Vector3[n]; w.vCel = new float[n]; w.skok = new bool[n]; w.szer = new float[n];
        for (int k = 0; k < n; k++) { w.punkty[k] = pr.p[k] * K; w.lewo[k] = pr.lewo[k]; w.vCel[k] = pr.v[k]; w.skok[k] = pr.skok[k]; w.szer[k] = pr.szer[k]; }
        w.krok = KrokWyscigu; w.dlugosc = o.dlugosc; w.sMety = pr.sMety; w.polSzerokosci = Szer * .5f + Pobocze; w.K = K; w.okrazen = Okrazen;
        // the gate: five places behind the bars, facing west down the start straight; the bars on their hinges (down: lying
        // forward on the pad, up: 0.42 m over the hinge, FIM's 50 cm over the ground)
        int ig = IndeksBramki(); float gy = Tor(ig, 0f), gzc = o.z[ig], bw = Szer / 5f;
        w.sloty = new Vector3[5]; w.belki = new Transform[5];
        w.kierunekStartu = Quaternion.LookRotation(Vector3.left, Vector3.up);
        w.belkaGora = Quaternion.identity; w.belkaDol = Quaternion.Euler(0f, 0f, 90f);
        for (int k = 0; k < 5; k++)
        {
            float zc = gzc - Szer * .5f + (k + .5f) * bw;
            w.sloty[k] = new Vector3(BramkaX + 1.25f, gy + .08f, zc) * K;
            var mat = k % 2 == 0 ? czerwien : biel; float hz = bw * .5f - .08f;
            var t = Ruchomy(g, "TOR DO KŁADÓW | maszyna startowa: belka " + (k + 1), new Vector3(BramkaX - .09f, gy + .12f, zc), bb => bb.BoxMinMax(mat, -.03f, .03f, .005f, .425f, -hz, hz));
            t.localRotation = w.belkaDol; w.belki[k] = t; Belek++;
        }
        w.strefaStartu = new Vector3(BramkaX + 12f, gy, gzc) * K; w.strefaPromien = 24f;
        // the riders' park: the four rivals on their quads, a black board with PARK ZAWODNIKÓW behind them
        var wolne = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Source/Mistrzowie/Audio/Klad/klad_silnik_wolne.ogg");
        var jazda = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Source/Mistrzowie/Audio/Klad/klad_silnik_jazda.ogg");
        var lista = new List<BraxChampionsKladRywal>();
        for (int i = 0; i < Rywale.Length; i++)
        {
            var r = Rywale[i];
            try
            {
                var rv = BraxChampionsKlady.PostawRywala(g, r.model, r.kolor, r.imie, r.numer, new Vector3(ParkX + i * ParkKrok, y, ParkZ) * K, 180f, r.stroj, r.akcent, r.umiejetnosc, r.pas, wolne, jazda);
                if (rv) { lista.Add(rv); Rywali++; }
            }
            catch (Exception e) { BraxBuildQuality.Warn("DO POPRAWKI TOR_KLADOW_RYWAL " + r.imie + ": " + e.Message); }
        }
        w.rywale = lista.ToArray();
        float px0 = ParkX - 2f, px1 = ParkX + 3f * ParkKrok + 2f, pz = ParkZ + 4.5f;
        foreach (float sx in new[] { px0 + .3f, px1 - .3f }) { b.Cylinder(stal, new Vector3(sx, y, pz + .1f), .06f, 2.45f, 10); W.Kolizja(new Vector3(sx, y + 1.2f, pz + .1f) * K, new Vector3(.15f, 2.4f, .15f) * K, Quaternion.identity); }
        b.BoxMinMax(czern, px0, px1, y + 1.55f, y + 2.35f, pz, pz + .08f);
        BraxChampionsLetters3D.Text(b, zloto, czern, "PARK ZAWODNIKÓW", new Vector3((px0 + px1) * .5f, y + 1.75f, pz - .02f), 180f, .42f, .03f, .12f, px1 - px0 - 1.2f);
        // the LED board in the infield across the start straight, facing the grandstand and the paddock: a black frame on two
        // steel legs, the screen (192 x 96 LEDs, ~5 cm each) 3 cm in front of it
        float gb = Grunt(TablicaX, TablicaZ), x0 = TablicaX - TablicaW * .5f, x1 = TablicaX + TablicaW * .5f, yb = gb + TablicaNad, yt = yb + TablicaH;
        b.BoxMinMax(czern, x0 - .3f, x1 + .3f, yb - .3f, yt + .3f, TablicaZ - .25f, TablicaZ + .15f);
        foreach (float lx in new[] { TablicaX - 3f, TablicaX + 3f })
        {
            b.Cylinder(stal, new Vector3(lx, gb - .4f, TablicaZ - .05f), .18f, TablicaNad + .2f, 12);
            W.Kolizja(new Vector3(lx, gb + TablicaNad * .5f, TablicaZ - .05f) * K, new Vector3(.4f, TablicaNad + .4f, .4f) * K, Quaternion.identity);
        }
        W.Kolizja(new Vector3(TablicaX, (yb + yt) * .5f, TablicaZ - .05f) * K, new Vector3(TablicaW + .6f, TablicaH + .6f, .4f) * K, Quaternion.identity);
        var ekran = BraxChampionsHarbour.Surface("tor_kladow_tablica_led", Color.white, .35f, 0f, new Color(1.6f, 1.6f, 1.6f));
        var be = new BraxChampionsMeshBatch(); float zs = TablicaZ + .18f;
        // seen from the north (looking south) its left is east: u 0 at x1
        be.QuadUV(ekran, new Vector3(x1, yb, zs), new Vector3(x1, yt, zs), new Vector3(x0, yt, zs), new Vector3(x0, yb, zs), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        var te = be.Build(g, "TOR DO KŁADÓW | tablica wyników LED", Folder, false, K);
        var ekrany = te.GetComponentsInChildren<MeshRenderer>(true); w.tablica = ekrany.Length > 0 ? ekrany[0] : null;
        Trojkaty += be.Triangles;
        Debug.Log("MISTRZOWIE_TOR_KLADOW_WYSCIG punkty=" + n + " meta=" + pr.sMety.ToString("F0") + "m rywale=" + Rywali + " belki=" + Belek + " " + string.Join(" | ", pr.doubles));
        return w;
    }
}
