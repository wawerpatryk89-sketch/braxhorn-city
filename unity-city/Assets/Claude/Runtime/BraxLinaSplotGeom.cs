using System.Collections.Generic;
using UnityEngine;

// BRAXHORN — geometria liny SKRĘCANEJ ZE SPLOTEK (MIASTO R1, ring w sali kickboxingu na faweli; właściciel 10.11 07:13:
// „na około są prawdziwe linki”, 07:20: „jak najbardziej realistyczne… stare, ale realistyczne”).
// Prawdziwa lina konopna / bawełniana: 3 splotki nawinięte śrubowo wokół osi (skok ok. 0.2 m), każda splotka okrągła
// i lekko na zakładkę z sąsiednią — z bliska widać skręt, z daleka gruba lina. Czysta matematyka (bez komponentów), żeby
// tej samej geometrii używały: lina pracująca w grze (BraxLinaSplot) i edytor (luźne końce lin przy furtce ringu).
public static class BraxLinaSplotGeom
{
    public sealed class Wynik
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<Color> c = new List<Color>();
        public readonly List<int> tr = new List<int>();
        public readonly List<float> t = new List<float>(), l = new List<float>();   // parametr wzdłuż boku (0..1) i poziom liny (dla ugięcia)
    }

    const int Boki = 5, NaSkok = 5;   // przekrój splotki i odcinki na jeden skok (telefon: cały ring ok. 60 tys. trójkątów)

    static float Szum(float x, float y)
    {
        float s = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f; s -= Mathf.Floor(s);
        float s2 = Mathf.Sin(x * 3.17f + y * 1.31f) * .5f + .5f;
        return s * .35f + s2 * .65f;
    }

    /// <summary>Lina wzdłuż łamanej `pkt` (prosta: 2 punkty). R — promień całej liny, skok — długość jednego skrętu,
    /// faza — obrót splotek na starcie (każda lina inaczej), poziom — indeks liny (ugięcie), tA..tB — parametr wzdłuż boku.</summary>
    public static void Lina(Wynik g, IList<Vector3> pkt, float R, float skok, Color barwa, float faza, float poziom, float tA = 0f, float tB = 1f)
    {
        int m = pkt.Count; if (m < 2) return;
        var dlug = new float[m]; for (int i = 1; i < m; i++) dlug[i] = dlug[i - 1] + Vector3.Distance(pkt[i - 1], pkt[i]);
        float L = dlug[m - 1]; if (L < 1e-3f) return;
        int odc = Mathf.Max(2, Mathf.CeilToInt(L / skok * NaSkok));
        float e = .5f * R, r = .56f * R, w = 2f * Mathf.PI / skok;
        // ramka osi przenoszona równolegle (lina może zwisać pionowo — bez skoku obrotu ramki)
        var os = new Vector3[odc + 1]; var poz = new Vector3[odc + 1]; var bok = new Vector3[odc + 1]; var gor = new Vector3[odc + 1];
        int seg = 0;
        for (int s = 0; s <= odc; s++)
        {
            float x = L * s / odc;
            while (seg < m - 2 && dlug[seg + 1] < x) seg++;
            float d = Mathf.Max(1e-5f, dlug[seg + 1] - dlug[seg]);
            poz[s] = Vector3.Lerp(pkt[seg], pkt[seg + 1], Mathf.Clamp01((x - dlug[seg]) / d));
            // styczna wygładzona (średnia sąsiednich odcinków łamanej)
            var tg = (pkt[seg + 1] - pkt[seg]).normalized;
            float u = (x - dlug[seg]) / d;
            if (u < .5f && seg > 0) tg = Vector3.Lerp((pkt[seg] - pkt[seg - 1]).normalized, tg, .5f + u).normalized;
            else if (u >= .5f && seg < m - 2) tg = Vector3.Lerp(tg, (pkt[seg + 2] - pkt[seg + 1]).normalized, u - .5f).normalized;
            os[s] = tg;
            if (s == 0)
            {
                var b0 = Vector3.Cross(Vector3.up, tg); if (b0.sqrMagnitude < 1e-4f) b0 = Vector3.Cross(Vector3.forward, tg);
                bok[0] = b0.normalized;
            }
            else
            {
                var b0 = bok[s - 1] - tg * Vector3.Dot(bok[s - 1], tg);
                bok[s] = b0.sqrMagnitude > 1e-8f ? b0.normalized : bok[s - 1];
            }
            gor[s] = Vector3.Cross(tg, bok[s]).normalized;
        }
        for (int j = 0; j < 3; j++)
        {
            int k0 = g.v.Count; float fj = faza + j * 2.0943951f, jas = 1f - .045f * j;
            for (int s = 0; s <= odc; s++)
            {
                float x = L * s / odc, th = w * x + fj;
                Vector3 rad = bok[s] * Mathf.Cos(th) + gor[s] * Mathf.Sin(th), dr = -bok[s] * Mathf.Sin(th) + gor[s] * Mathf.Cos(th);
                Vector3 c = poz[s] + rad * e, T = (os[s] + dr * (e * w)).normalized;
                Vector3 N1 = (rad - T * Vector3.Dot(rad, T)).normalized, N2 = Vector3.Cross(T, N1);
                float ts = Mathf.Lerp(tA, tB, (float)s / odc);
                // stara lina: przybrudzona tam, gdzie się o nią opiera (środek boku), plamy, ciemniejsze bruzdy między splotkami
                float brud = 1f - .16f * Mathf.Clamp01(1f - Mathf.Abs(ts - .5f) * 2.4f) - .1f * Szum(x * 1.7f + poziom * 3.1f, j + poziom);
                for (int q = 0; q <= Boki; q++)
                {
                    float f = q * 2f * Mathf.PI / Boki; var dd = N1 * Mathf.Cos(f) + N2 * Mathf.Sin(f);
                    float bruzda = Mathf.Lerp(.72f, 1f, Mathf.Clamp01(Vector3.Dot(dd, rad) * .5f + .62f));   // od strony osi ciemniej (cień w skręcie)
                    var k = barwa * (brud * bruzda * jas); k.a = 1f;
                    g.v.Add(c + dd * r); g.n.Add(dd); g.uv.Add(new Vector2(x / .25f, (float)q / Boki * .4f + j * .33f)); g.c.Add(k); g.t.Add(ts); g.l.Add(poziom);
                }
            }
            for (int s = 0; s < odc; s++)
                for (int q = 0; q < Boki; q++)
                {
                    int p0 = k0 + s * (Boki + 1) + q, p1 = p0 + Boki + 1;
                    g.tr.Add(p0); g.tr.Add(p0 + 1); g.tr.Add(p1); g.tr.Add(p0 + 1); g.tr.Add(p1 + 1); g.tr.Add(p1);
                }
        }
    }

    /// <summary>Spinki boku: pionowe paski z taśmy płóciennej owinięte wokół wszystkich lin na 1/3 i 2/3 długości (jak w prawdziwym ringu —
    /// trzymają liny w równych odstępach). Ugięcie pasków idzie za linami (poziom od dolnej do górnej liny).</summary>
    public static void Spinki(Wynik g, Vector3 a, Vector3 b, IList<float> wys, float R, Color kolor)
    {
        int n = wys.Count; if (n < 2) return;
        Vector3 os = b - a; float dl = os.magnitude; if (dl < 1e-3f) return; os /= dl;
        Vector3 bok = Vector3.Cross(Vector3.up, os).normalized; kolor.a = 1f;
        foreach (float ts in new[] { 1f / 3f, 2f / 3f })
        {
            var c = a + os * (dl * ts);
            for (int side = -1; side <= 1; side += 2)
            {
                int k0 = g.v.Count; var off = bok * (R + .005f) * side;
                foreach (var (y, lv) in new[] { (wys[0] - R - .012f, 0f), (wys[n - 1] + R + .012f, (float)(n - 1)) })
                    foreach (float w in new[] { -.03f, .03f })
                    { g.v.Add(c + off + os * w + Vector3.up * y); g.n.Add(bok * side); g.uv.Add(new Vector2(w * 4f, y * 2f)); g.c.Add(kolor); g.t.Add(ts); g.l.Add(lv); }
                if (side > 0) { g.tr.Add(k0); g.tr.Add(k0 + 2); g.tr.Add(k0 + 1); g.tr.Add(k0 + 1); g.tr.Add(k0 + 2); g.tr.Add(k0 + 3); }
                else { g.tr.Add(k0); g.tr.Add(k0 + 1); g.tr.Add(k0 + 2); g.tr.Add(k0 + 1); g.tr.Add(k0 + 3); g.tr.Add(k0 + 2); }
            }
            // owinięcie paska wokół każdej liny (krótki walec z taśmy na splotkach)
            for (int i = 0; i < n; i++)
            {
                int k0 = g.v.Count; var cc = c + Vector3.up * wys[i]; float rr = R * 1.12f;
                for (int s = 0; s <= 1; s++)
                    for (int q = 0; q <= 8; q++)
                    {
                        float f = q * 2f * Mathf.PI / 8; var dd = bok * Mathf.Cos(f) + Vector3.up * Mathf.Sin(f);
                        g.v.Add(cc + os * (s == 0 ? -.032f : .032f) + dd * rr); g.n.Add(dd); g.uv.Add(new Vector2(s, q / 8f)); g.c.Add(kolor); g.t.Add(ts); g.l.Add(i);
                    }
                for (int q = 0; q < 8; q++) { int p0 = k0 + q, p1 = p0 + 9; g.tr.Add(p0); g.tr.Add(p0 + 1); g.tr.Add(p1); g.tr.Add(p0 + 1); g.tr.Add(p1 + 1); g.tr.Add(p1); }
            }
        }
    }
}
