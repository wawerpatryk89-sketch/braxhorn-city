using System.Collections.Generic;
using UnityEngine;

// One frame of a fountain's moving water (owner 11.11 19:49: "fontanna ... woda ma lecieć, widzieć jak tryska woda, a nie ma
// stać w miejscu ... realistycznie"). Pure geometry, no Unity objects: BraxChampionsFontanna calls it every frame, the preview
// calls it for single frames. Every jet is a real ballistic arc (gravity g, the time of flight from the apex height); along it
// travel overlapping water streaks (two crossed quads each, soft edges from the atlas' left half), finer spray beads that
// drift sideways as the stream opens, and where a jet meets water: ripple rings spreading and fading, a few splash beads.
// The basin's surface gets a slowly drifting ripple layer (submesh 1). All positions are local, in game units.
public static class BraxChampionsFontannaKlatka
{
    public struct Strumien { public Vector3 a, b; public float h, w; public int n; public bool plusk; }

    static float R(int i, int k) { float x = Mathf.Sin(i * 12.9898f + k * 78.233f) * 43758.5453f; return x - Mathf.Floor(x); }

    // the arc from a to b with the apex h above the higher end: returns the flight time and fills v0 (initial velocity)
    public static float Lot(Vector3 a, Vector3 b, float h, float g, out Vector3 v0)
    {
        float top = Mathf.Max(a.y, b.y) + Mathf.Max(.01f, h);
        float vy = Mathf.Sqrt(2f * g * (top - a.y));
        float T = vy / g + Mathf.Sqrt(2f * (top - b.y) / g);
        v0 = new Vector3((b.x - a.x) / T, vy, (b.z - a.z) / T);
        return T;
    }
    static Vector3 P(Vector3 a, Vector3 v0, float g, float t) => a + v0 * t + Vector3.down * (.5f * g * t * t);

    public static void Klatka(Strumien[] s, Vector3[] lustroC, float[] lustroR, float g, float lift, float t,
                              List<Vector3> V, List<Vector3> N, List<Vector2> U, List<Color> C, List<int> T0, List<int> T1)
    {
        V.Clear(); N.Clear(); U.Clear(); C.Clear(); T0.Clear(); T1.Clear();
        for (int i = 0; i < s.Length; i++)
        {
            var j = s[i];
            float T = Lot(j.a, j.b, j.h, g, out var v0);
            int n = Mathf.Max(4, j.n);
            float L = 1.45f * T / n;                                       // overlapping: a continuous stream with moving pulses
            for (int k = 0; k < n; k++)
            {
                float ph = t / T + k / (float)n + R(i, 0) * .37f;
                float sk = ph - Mathf.Floor(ph), tt = sk * T;
                var head = P(j.a, v0, g, tt); var tail = P(j.a, v0, g, Mathf.Max(0f, tt - L));
                float a = .80f * Mathf.Clamp01(sk / .06f) * Mathf.Clamp01((1f - sk) / .06f + .35f);
                Smuga(V, N, U, C, T0, tail, head, j.w * (.85f + .3f * R(i, k + 3)), a);
                // spray beads: thinner, drifting out of the stream as it opens
                if ((k & 1) == 0)
                {
                    float ph2 = ph + .5f / n, s2 = ph2 - Mathf.Floor(ph2), t2 = s2 * T;
                    var side = new Vector3(R(i, k + 11) - .5f, R(i, k + 17) * .3f, R(i, k + 23) - .5f) * (j.w * 3.0f * s2);
                    var h2 = P(j.a, v0, g, t2) + side; var b2 = P(j.a, v0, g, Mathf.Max(0f, t2 - L * .35f)) + side;
                    Smuga(V, N, U, C, T0, b2, h2, j.w * .35f, .55f * Mathf.Clamp01((1f - s2) / .15f));
                }
            }
            if (!j.plusk) continue;
            // where the jet lands on water: three rings spreading and fading, four splash beads jumping up and out
            float rr = Mathf.Clamp(j.w * 9f, .25f, 1.1f);
            for (int q = 0; q < 3; q++)
            {
                float ph = t / 1.15f + q / 3f + R(i, 41), sq = ph - Mathf.Floor(ph);
                Kolo(V, N, U, C, T0, j.b + Vector3.up * lift, rr * (.12f + .88f * sq), .55f * (1f - sq));
            }
            for (int q = 0; q < 4; q++)
            {
                float ang = (q / 4f + R(i, 50 + q)) * Mathf.PI * 2f;
                var to = j.b + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * rr * .45f;
                float Ts = Lot(j.b, to, rr * .22f, g, out var vs);
                float ph = t / Ts + R(i, 60 + q), sq = ph - Mathf.Floor(ph), ts = sq * Ts;
                var h3 = P(j.b, vs, g, ts); var b3 = P(j.b, vs, g, Mathf.Max(0f, ts - Ts * .18f));
                Smuga(V, N, U, C, T0, b3, h3, j.w * .4f, .6f * (1f - sq));
            }
        }
        // the basins' surfaces: a drifting ripple layer `lift` over the water (UV in metres-ish, two directions blended by
        // the vertex alpha so the pattern never repeats visibly)
        for (int i = 0; i < lustroC.Length; i++)
        {
            var c = lustroC[i] + Vector3.up * lift; float r = lustroR[i]; int seg = Mathf.Clamp(Mathf.RoundToInt(r * 10f), 16, 64);
            int c0 = V.Count;
            var off = new Vector2(t * .045f, t * .031f);
            V.Add(c); N.Add(Vector3.up); U.Add(new Vector2(c.x, c.z) * .45f + off); C.Add(new Color(1, 1, 1, .9f));
            for (int k = 0; k <= seg; k++)
            {
                float a = 2f * Mathf.PI * k / seg; var p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                V.Add(p); N.Add(Vector3.up); U.Add(new Vector2(p.x, p.z) * .45f + off); C.Add(new Color(1, 1, 1, .9f));
            }
            for (int k = 0; k < seg; k++) { T1.Add(c0); T1.Add(c0 + 2 + k); T1.Add(c0 + 1 + k); }
        }
    }

    // a water streak from b (tail) to a (head): two crossed quads, UV u 0..0.5 (the atlas' streak), v along
    static void Smuga(List<Vector3> V, List<Vector3> N, List<Vector2> U, List<Color> C, List<int> T, Vector3 b, Vector3 a, float w, float alpha)
    {
        var d = a - b; if (d.sqrMagnitude < 1e-6f || alpha <= .01f) return;
        var s1 = Vector3.Cross(d, Vector3.up); if (s1.sqrMagnitude < 1e-8f) s1 = Vector3.right; s1 = s1.normalized * (w * .5f);
        var s2 = Vector3.Cross(d, s1).normalized * (w * .5f);
        var col = new Color(.93f, .97f, 1f, alpha);
        foreach (var (sd, nr) in new[] { (s1, s2.normalized), (s2, s1.normalized) })
        {
            int o = V.Count;
            V.Add(b - sd); V.Add(b + sd); V.Add(a + sd); V.Add(a - sd);
            for (int q = 0; q < 4; q++) { N.Add(nr); C.Add(col); }
            U.Add(new Vector2(0f, 0f)); U.Add(new Vector2(.5f, 0f)); U.Add(new Vector2(.5f, 1f)); U.Add(new Vector2(0f, 1f));
            T.Add(o); T.Add(o + 1); T.Add(o + 2); T.Add(o); T.Add(o + 2); T.Add(o + 3);
        }
    }
    // a ripple ring lying on the water: one quad with the atlas' ring (u .5..1)
    static void Kolo(List<Vector3> V, List<Vector3> N, List<Vector2> U, List<Color> C, List<int> T, Vector3 c, float r, float alpha)
    {
        if (alpha <= .01f) return;
        int o = V.Count; var col = new Color(.95f, .98f, 1f, alpha);
        V.Add(c + new Vector3(-r, 0, -r)); V.Add(c + new Vector3(r, 0, -r)); V.Add(c + new Vector3(r, 0, r)); V.Add(c + new Vector3(-r, 0, r));
        for (int q = 0; q < 4; q++) { N.Add(Vector3.up); C.Add(col); }
        U.Add(new Vector2(.5f, 0f)); U.Add(new Vector2(1f, 0f)); U.Add(new Vector2(1f, 1f)); U.Add(new Vector2(.5f, 1f));
        T.Add(o); T.Add(o + 2); T.Add(o + 1); T.Add(o); T.Add(o + 3); T.Add(o + 2);
    }

    // the atlas (64 x 64, RGBA): left half a soft streak (gaussian across, rounded ends), right half a thin ring
    public static Color32[] Atlas(int W = 64, int H = 64)
    {
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float a;
                if (x < W / 2)
                {
                    float u = (x + .5f) / (W / 2f) * 2f - 1f, v = (y + .5f) / H;
                    a = Mathf.Exp(-u * u * 3.2f) * Mathf.Pow(Mathf.Sin(Mathf.PI * v), .55f) * (.80f + .20f * Mathf.Sin(v * 37f));
                }
                else
                {
                    float u = (x - W / 2 + .5f) / (W / 2f) * 2f - 1f, v = (y + .5f) / H * 2f - 1f, r = Mathf.Sqrt(u * u + v * v);
                    a = Mathf.Exp(-Mathf.Pow((r - .78f) / .07f, 2f)) + .35f * Mathf.Exp(-Mathf.Pow((r - .55f) / .05f, 2f));
                }
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
            }
        return px;
    }
    // the surface ripple tile (64 x 64, repeats): bright wavy lines like light on moving water
    public static Color32[] Fale(int W = 64)
    {
        var px = new Color32[W * W];
        for (int y = 0; y < W; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x * 2f * Mathf.PI / W, v = y * 2f * Mathf.PI / W;
                float f = Mathf.Sin(u * 2f + Mathf.Sin(v * 3f) * 1.2f) + Mathf.Sin(v * 2f + Mathf.Sin(u * 1f) * 1.6f) + .6f * Mathf.Sin((u + v) * 3f);
                float a = Mathf.Pow(Mathf.Clamp01((f - .9f) / 1.6f), 1.6f) * .55f;
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        return px;
    }
}
