using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ZNAKI DROGOWE OSIEDLA (build 14, wykonawca „drogi”). Polskie wzory budowane geometrią (ostre z każdej odległości, bez tekstur):
//   A-7 „ustąp pierwszeństwa” (trójkąt odwrócony, czerwona obwódka, żółte pole), C-12 „ruch okrężny” (niebieskie koło,
//   trzy białe strzałki przeciwnie do wskazówek zegara), C-9 „nakaz jazdy z prawej strony znaku”, D-6 „przejście dla pieszych”,
//   D-15 „przystanek autobusowy”, D-18 „parking” (litera P z prostokątów i łuku).
// Montaż jak w rzeczywistości: słupek ocynkowany Ø60 mm, tarcza PRZED słupkiem na dwóch obejmach (nic nie przechodzi przez
// lico — zdjęcie właściciela 22:56: słupek przez środek tablicy BYDGOSKA), dolna krawędź najniższej tarczy 2.1 m × K
// (na chodniku min. 2.0 m), tył tarczy szary (ocynk), krawędź 12 mm. Tablice z nazwą ulicy: dwie tarcze po obu stronach słupka.
public static partial class BraxClaudeOsiedle
{
    static Material ZnNieb => Plain("znak niebieski (folia odblaskowa)", new Color(.02f, .23f, .62f), .45f);
    static Material ZnCzerw => Plain("znak czerwony (folia odblaskowa)", new Color(.80f, .06f, .06f), .45f);
    static Material ZnZolty => Plain("znak żółty (folia odblaskowa)", new Color(.98f, .78f, .06f), .45f);
    static Material ZnBialy => Plain("znak biały (folia odblaskowa)", new Color(.94f, .94f, .92f), .45f);
    static Material ZnCzarny => Plain("znak czarny", new Color(.04f, .04f, .045f), .35f);
    static Material Ocynk => Plain("słupek ocynk", new Color(.62f, .64f, .66f), .45f, .7f);
    static float Ks => Mathf.Clamp(K, .85f, 1.25f);
    static int znakiPostawione;

    static string GrupaZnakow(Vector3 p) => p.z < FenceA + .5f ? "Osiedle B / znaki drogowe" : "Osiedle A / znaki drogowe";

    // wymiary tarcz (m, przed skalą): wysokość w pionie
    static float WysTarczy(string typ) => typ == "A-7" ? .75f * .866f : .6f;

    // Znak(i) na słupku: p = podstawa słupka (y = 0), n = lico tarczy (w stronę kierowcy). typy od góry do dołu.
    static void ZnakPionowy(Vector3 p, Vector3 n, params string[] typy)
    {
        n.y = 0f; if (n.sqrMagnitude < 1e-4f) return; n.Normalize();
        var g = group; group = GrupaZnakow(p);
        float k = Ks, rr = .03f, gap = .06f * k;
        float y = 2.1f * k; var stos = new List<(string typ, float yc)>();
        for (int i = typy.Length - 1; i >= 0; i--) { float h = WysTarczy(typy[i]) * k; stos.Add((typy[i], y + h * .5f)); y += h + gap; }
        float top = y - gap + .05f * k;
        p.y = 0f;
        Cylinder(Ocynk, p, rr, top, 10, false, false);
        Cylinder(ZnCzarny, p + Vector3.up * top, rr + .004f, .02f, 10, true, false);       // zaślepka
        Collider(p + Vector3.up * (top * .5f), new Vector3(.1f, top, .1f));
        var r = Vector3.Cross(Vector3.up, -n).normalized;
        float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        foreach (var (typ, yc) in stos)
        {
            var c = p + Vector3.up * yc + n * (rr + .03f);
            float h = WysTarczy(typ) * k;
            foreach (float dy in new[] { -h * .3f, h * .3f })
                OBox(Ocynk, p + Vector3.up * (yc + dy) + n * (rr * .5f + .012f), new Vector3(.09f * k, .035f * k, rr + .03f), yaw);   // obejma
            Tarcza(typ, c, r, Vector3.up, n, k);
        }
        group = g; znakiPostawione++;
    }

    // Tarcza: tył (ocynk) + krawędź 12 mm + lico; pikto. warstwami +3 mm / +6 mm.
    static void Tarcza(string typ, Vector3 c, Vector3 R, Vector3 U, Vector3 n, float k)
    {
        Vector3 P(float x, float y, float dz = 0f) => c + R * (x * k) + U * (y * k) + n * dz;
        Vector2[] obrys;
        if (typ == "A-7") { float a = .75f, h = a * .866f; obrys = new[] { new Vector2(-a * .5f, h / 3f), new Vector2(a * .5f, h / 3f), new Vector2(0, -2f * h / 3f) }; }
        else if (typ == "C-12" || typ == "C-9") obrys = Enumerable.Range(0, 32).Select(q => new Vector2(Mathf.Cos(q * Mathf.PI / 16f), Mathf.Sin(q * Mathf.PI / 16f)) * .3f).ToArray();
        else obrys = new[] { new Vector2(-.3f, -.3f), new Vector2(.3f, -.3f), new Vector2(.3f, .3f), new Vector2(-.3f, .3f) };
        const float gr = .012f;
        // tył i krawędź
        for (int q = 1; q + 1 < obrys.Length; q++) Tri3(Ocynk, P(obrys[0].x, obrys[0].y, -gr), P(obrys[q + 1].x, obrys[q + 1].y, -gr), P(obrys[q].x, obrys[q].y, -gr), -n);
        for (int q = 0; q < obrys.Length; q++)
        {
            var a = obrys[q]; var b = obrys[(q + 1) % obrys.Length]; var mid = (a + b) * .5f;
            var on = (R * mid.x + U * mid.y).normalized;
            Quad(Ocynk, P(a.x, a.y), P(b.x, b.y), P(b.x, b.y, -gr), P(a.x, a.y, -gr), on);
        }
        void Pole(Material m, float dz, params Vector2[] pts) { for (int q = 1; q + 1 < pts.Length; q++) Tri3(m, P(pts[0].x, pts[0].y, dz), P(pts[q].x, pts[q].y, dz), P(pts[q + 1].x, pts[q + 1].y, dz), n); }
        void Pierscien(Material m, float dz, float r0, float r1, float a0, float a1, Vector2 o, int seg)
        {
            for (int q = 0; q < seg; q++)
            {
                float t0 = Mathf.Lerp(a0, a1, q / (float)seg) * Mathf.Deg2Rad, t1 = Mathf.Lerp(a0, a1, (q + 1) / (float)seg) * Mathf.Deg2Rad;
                Vector2 d0 = new Vector2(Mathf.Cos(t0), Mathf.Sin(t0)), d1 = new Vector2(Mathf.Cos(t1), Mathf.Sin(t1));
                Pole(m, dz, o + d0 * r0, o + d0 * r1, o + d1 * r1, o + d1 * r0);
            }
        }
        void Kreska(Material m, float dz, Vector2 a, Vector2 b, float w)
        {
            var d = (b - a).normalized; var pp = new Vector2(-d.y, d.x) * (w * .5f);
            Pole(m, dz, a - pp, b - pp, b + pp, a + pp);
        }
        const float L1 = .003f, L2 = .006f, L3 = .009f;
        switch (typ)
        {
            case "A-7":
            {
                // obwódka czerwona (3 trapezy) + żółte pole; bez nakładania warstw
                var cen = Vector2.zero; float s = (.075f * .866f) / (.75f * .866f / 3f);   // grubość obwódki / promień wpisany
                var inn = obrys.Select(v => v * (1f - s * 1.0f)).ToArray();
                for (int q = 0; q < 3; q++) Pole(ZnCzerw, 0f, obrys[q], obrys[(q + 1) % 3], inn[(q + 1) % 3], inn[q]);
                Pole(ZnZolty, 0f, inn);
                break;
            }
            case "C-12":
            {
                Pierscien(ZnBialy, 0f, .285f, .3f, 0f, 360f, Vector2.zero, 32);
                Pole(ZnNieb, 0f, Enumerable.Range(0, 32).Select(q => new Vector2(Mathf.Cos(q * Mathf.PI / 16f), Mathf.Sin(q * Mathf.PI / 16f)) * .285f).ToArray());
                for (int j = 0; j < 3; j++)
                {
                    float a0 = 100f + j * 120f, a1 = a0 + 72f;   // łuk strzałki, kierunek przeciwny do wskazówek zegara (rośnie kąt)
                    Pierscien(ZnBialy, L1, .115f, .175f, a0, a1, Vector2.zero, 10);
                    float t = a1 * Mathf.Deg2Rad; var rad = new Vector2(Mathf.Cos(t), Mathf.Sin(t)); var tan = new Vector2(-rad.y, rad.x);
                    Pole(ZnBialy, L1, rad * .085f, rad * .205f, rad * .145f + tan * .075f);
                }
                break;
            }
            case "C-9":
            {
                Pierscien(ZnBialy, 0f, .285f, .3f, 0f, 360f, Vector2.zero, 32);
                Pole(ZnNieb, 0f, Enumerable.Range(0, 32).Select(q => new Vector2(Mathf.Cos(q * Mathf.PI / 16f), Mathf.Sin(q * Mathf.PI / 16f)) * .285f).ToArray());
                var d = new Vector2(1f, -1f).normalized; var pp = new Vector2(-d.y, d.x);
                Kreska(ZnBialy, L1, -d * .15f, d * .05f, .07f);
                Pole(ZnBialy, L1, d * .03f + pp * .1f, d * .17f, d * .03f - pp * .1f);
                break;
            }
            default:   // kwadrat D: biała obwódka + niebieskie pole
            {
                const float o = .3f, i = .285f;
                Pole(ZnBialy, 0f, new Vector2(-o, -o), new Vector2(o, -o), new Vector2(i, -i), new Vector2(-i, -i));
                Pole(ZnBialy, 0f, new Vector2(o, -o), new Vector2(o, o), new Vector2(i, i), new Vector2(i, -i));
                Pole(ZnBialy, 0f, new Vector2(o, o), new Vector2(-o, o), new Vector2(-i, i), new Vector2(i, i));
                Pole(ZnBialy, 0f, new Vector2(-o, o), new Vector2(-o, -o), new Vector2(-i, -i), new Vector2(-i, i));
                Pole(ZnNieb, 0f, new Vector2(-i, -i), new Vector2(i, -i), new Vector2(i, i), new Vector2(-i, i));
                if (typ == "D-6")
                {
                    Pole(ZnBialy, L1, new Vector2(-.24f, -.19f), new Vector2(.24f, -.19f), new Vector2(0f, .235f));
                    for (int s = 0; s < 4; s++) { float x = -.14f + s * .085f; Pole(ZnCzarny, L2, new Vector2(x, -.165f), new Vector2(x + .05f, -.165f), new Vector2(x + .065f, -.125f), new Vector2(x + .015f, -.125f)); }
                    Pierscien(ZnCzarny, L2, 0f, .027f, 0f, 360f, new Vector2(.012f, .085f), 12);
                    Kreska(ZnCzarny, L2, new Vector2(.004f, .05f), new Vector2(-.008f, -.03f), .042f);
                    Kreska(ZnCzarny, L2, new Vector2(-.008f, -.03f), new Vector2(-.06f, -.105f), .03f);
                    Kreska(ZnCzarny, L2, new Vector2(-.008f, -.03f), new Vector2(.05f, -.105f), .03f);
                    Kreska(ZnCzarny, L2, new Vector2(.0f, .04f), new Vector2(-.052f, -.005f), .026f);
                    Kreska(ZnCzarny, L2, new Vector2(.0f, .04f), new Vector2(.055f, .0f), .026f);
                }
                else if (typ == "D-15")
                {
                    Pole(ZnBialy, L1, new Vector2(-.21f, -.15f), new Vector2(.21f, -.15f), new Vector2(.21f, .17f), new Vector2(-.21f, .17f));
                    Pole(ZnNieb, L2, new Vector2(-.17f, -.07f), new Vector2(.17f, -.07f), new Vector2(.17f, .11f), new Vector2(-.17f, .11f));
                    for (int s = 0; s < 3; s++) { float x = -.15f + s * .105f; Pole(ZnBialy, L3, new Vector2(x, .03f), new Vector2(x + .085f, .03f), new Vector2(x + .085f, .09f), new Vector2(x, .09f)); }
                    Pierscien(ZnCzarny, L3, 0f, .03f, 0f, 360f, new Vector2(-.1f, -.075f), 12);
                    Pierscien(ZnCzarny, L3, 0f, .03f, 0f, 360f, new Vector2(.1f, -.075f), 12);
                }
                else if (typ == "D-18")
                {
                    Pole(ZnBialy, L1, new Vector2(-.12f, -.21f), new Vector2(-.05f, -.21f), new Vector2(-.05f, .21f), new Vector2(-.12f, .21f));
                    Pole(ZnBialy, L1, new Vector2(-.05f, .14f), new Vector2(.03f, .14f), new Vector2(.03f, .21f), new Vector2(-.05f, .21f));
                    Pole(ZnBialy, L1, new Vector2(-.05f, -.01f), new Vector2(.03f, -.01f), new Vector2(.03f, .06f), new Vector2(-.05f, .06f));
                    Pierscien(ZnBialy, L1, .04f, .11f, -90f, 90f, new Vector2(.03f, .1f), 12);
                }
                break;
            }
        }
    }

    // Tablica z nazwą ulicy (polski wzór: granat, biały napis): dwie tarcze po obu stronach słupka (słupek ZA tablicą,
    // nic nie przechodzi przez napis), obejmy u góry i u dołu, słupek wystaje nad tablicę z zaślepką.
    static void TablicaUlicy(string id, Vector3 p, bool streetAlongX)
    {
        float k = Ks;
        if (Physics.CheckSphere(p + Vector3.up * 1.2f, .3f, ~0, QueryTriggerInteraction.Ignore)) p += (streetAlongX ? Vector3.forward : Vector3.right) * .8f;
        var g = group; group = GrupaZnakow(p);
        p.y = 0f;
        const float rr = .03f;
        float yc = 2.75f * k, w = .9f * k, h = .225f * k, top = yc + h * .5f + .12f * k;
        Cylinder(Ocynk, p, rr, top, 10, false, false);
        Cylinder(ZnCzarny, p + Vector3.up * top, rr + .004f, .02f, 10, true, false);
        Collider(p + Vector3.up * (top * .5f), new Vector3(.1f, top, .1f));
        Vector3 n = streetAlongX ? Vector3.forward : Vector3.right;
        float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
        foreach (float s in new[] { 1f, -1f })
        {
            var c = p + Vector3.up * yc + n * s * (rr + .022f);
            OBox(Frame, c - n * s * .006f, new Vector3(w + .03f * k, h + .03f * k, .012f), yaw);     // rama (aluminium) za licem
            Sign(id, c + n * s * .0005f, w, h, n * s, false);
        }
        foreach (float dy in new[] { -h * .5f - .025f * k, h * .5f + .025f * k })                     // obejmy łączące obie tarcze
            OBox(Ocynk, p + Vector3.up * (yc + dy), new Vector3(.16f * k, .03f * k, 2f * (rr + .03f)), yaw);
        group = g;
    }
}
