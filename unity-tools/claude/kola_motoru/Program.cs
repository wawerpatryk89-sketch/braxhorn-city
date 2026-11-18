using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

// TEST WYCINANIA KÓŁ (MIASTO R1, 18.11.2026; właściciel 14:49: „Zrób mi motor, w którym się kręcą koła”). Ścigacz testowy jako
// JEDNA siatka (jak model Tripo): opony 120/70-17 i 190/55-17 (profil eliptyczny), felgi z 5 ramionami i piastą, lagi widelca
// (kąt główki 24°), błotnik przedni i tylny tuż nad oponą, tarcze i zaciski, wahacz, zębatka, owiewka, bak, ogon, wydech —
// wymiary prawdziwe × K (1.294), szum ±3 mm × K jak w skanie. Wynik: czy BraxKolaMotoru.Szukaj trafia osie i promienie, a Podzial
// bierze do kół tylko opony / felgi / zębatkę (lagi, błotniki, zaciski, wahacz, wydech zostają w ramie). out/ — dane do obrazka.
static class Program
{
    const float K = 1.294f;
    static readonly List<Vector3> V = new List<Vector3>(); static readonly List<int> T = new List<int>(); static readonly List<int> Czesc = new List<int>();
    static readonly string[] Nazwy = { "owiewka", "bak", "ogon", "wydech", "lagi widelca", "błotnik przedni", "tarcze", "zaciski", "wahacz", "błotnik tylny",
                                       "opona przednia", "felga przednia", "opona tylna", "felga tylna", "zębatka", "kierownica" };
    // oczekiwane: 1 = koło przednie, 2 = koło tylne, 0 = rama, -1 = obojętne (tarcze kręcą się w prawdziwym motocyklu, mogą zostać)
    static readonly int[] Oczekiwane = { 0, 0, 0, 0, 0, 0, -1, 0, 0, 0, 1, 1, 2, 2, 2, 0 };
    static System.Random rnd = new System.Random(1811);

    static void Siatka(int czesc, int nu, int nv, Func<float, float, Vector3> f, bool zamkU = true, bool zamkV = false)
    {
        int b = V.Count;
        for (int j = 0; j <= nv; j++) for (int i = 0; i <= nu; i++) V.Add(f(i / (float)nu, j / (float)nv) * K + new Vector3(Sz(), Sz(), Sz()));
        for (int j = 0; j < nv; j++) for (int i = 0; i < nu; i++)
        {
            int a = b + j * (nu + 1) + i, c = a + nu + 1;
            T.AddRange(new[] { a, c, a + 1, a + 1, c, c + 1 }); Czesc.Add(czesc); Czesc.Add(czesc);
        }
    }
    static float Sz() => (float)(rnd.NextDouble() - .5) * .006f * K;
    const float TwoPi = 6.2831853f;

    static void Opona(int cz, float zc, float R, float a, float b)
    {   // eliptyczny przekrój: pół-szerokość a, pół-wysokość b, środek przekroju na promieniu R − b
        float yc = R;
        Siatka(cz, 96, 20, (u, v) => { float th = u * TwoPi, ph = v * TwoPi; float rr = R - b + b * Mathf.Cos(ph); return new Vector3(a * Mathf.Sin(ph), yc + rr * Mathf.Sin(th), zc + rr * Mathf.Cos(th)); });
    }
    static void Pierscien(int cz, float zc, float yc, float r0, float r1, float x0, float x1, int n = 64)
    {   // walec (płaszcz zewn. r1) + dwa lica pierścieniowe r0..r1
        Siatka(cz, n, 2, (u, v) => { float th = u * TwoPi; float x = x0 + (x1 - x0) * v; return new Vector3(x, yc + r1 * Mathf.Sin(th), zc + r1 * Mathf.Cos(th)); });
        foreach (float x in new[] { x0, x1 }) Siatka(cz, n, 2, (u, v) => { float th = u * TwoPi, rr = r0 + (r1 - r0) * v; return new Vector3(x, yc + rr * Mathf.Sin(th), zc + rr * Mathf.Cos(th)); });
    }
    static void Rura(int cz, Vector3 p0, Vector3 p1, float r, int n = 16)
    {
        var d = (p1 - p0); var e1 = Vector3.Cross(d.normalized, Math.Abs(d.normalized.y) < .9f ? Vector3.up : Vector3.right).normalized; var e2 = Vector3.Cross(d.normalized, e1);
        Siatka(cz, n, 6, (u, v) => p0 + d * v + (e1 * Mathf.Cos(u * TwoPi) + e2 * Mathf.Sin(u * TwoPi)) * r);
    }
    static void Elipsoida(int cz, Vector3 c, Vector3 r) => Siatka(cz, 40, 24, (u, v) => { float th = u * TwoPi, ph = v * Mathf.PI; return c + new Vector3(r.x * Mathf.Sin(ph) * Mathf.Cos(th), r.y * Mathf.Cos(ph), r.z * Mathf.Sin(ph) * Mathf.Sin(th)); });
    static void Luk(int cz, float zc, float yc, float r, float a0, float a1, float hw)
    {   // łukowa skorupa (błotnik): kąt od przodu (+z) w górę, szerokość ±hw, lekko zaokrąglona w poprzek
        Siatka(cz, 40, 8, (u, v) => { float th = (a0 + (a1 - a0) * u) * Mathf.Deg2Rad; float x = -hw + 2f * hw * v; float rr = r - .25f * hw * (x * x) / (hw * hw); return new Vector3(x, yc + rr * Mathf.Sin(th), zc + rr * Mathf.Cos(th)); });
    }
    static void Pudlo(int cz, Vector3 lo, Vector3 hi)
    {
        var c = new[] { new Vector3(lo.x, lo.y, lo.z), new Vector3(hi.x, lo.y, lo.z), new Vector3(hi.x, hi.y, lo.z), new Vector3(lo.x, hi.y, lo.z), new Vector3(lo.x, lo.y, hi.z), new Vector3(hi.x, lo.y, hi.z), new Vector3(hi.x, hi.y, hi.z), new Vector3(lo.x, hi.y, hi.z) };
        int b = V.Count; foreach (var p in c) V.Add(p * K + new Vector3(Sz(), Sz(), Sz()));
        int[] f = { 0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2, 0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5 };
        foreach (int i in f) T.Add(b + i); for (int i = 0; i < 12; i++) Czesc.Add(cz);
    }

    static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        float zf = .70f, zr = -.70f, Rf = .30f, Rr = .32f;   // rozstaw 1.40 m, promienie opon 120/70-17 i 190/55-17
        Opona(10, zf, Rf, .060f, .042f); Opona(12, zr, Rr, .095f, .052f);
        foreach (var (cz, z, R, w) in new[] { (11, zf, Rf, .035f), (13, zr, Rr, .060f) })
        {
            Pierscien(cz, z, R, .200f, .216f, -w, w);                                         // obręcz felgi
            Pierscien(cz, z, R, .0f, .045f, -w - .015f, w + .015f, 24);                        // piasta
            for (int k = 0; k < 5; k++) { float th = k * TwoPi / 5f; Rura(cz, new Vector3(0, R + .045f * Mathf.Sin(th), z + .045f * Mathf.Cos(th)), new Vector3(0, R + .2f * Mathf.Sin(th + .2f), z + .2f * Mathf.Cos(th + .2f)), .013f, 8); }
        }
        // widelec: lagi poza oponą (x ± 0.10), kąt główki 24°
        var dirW = new Vector3(0, Mathf.Cos(24f * Mathf.Deg2Rad), -Mathf.Sin(24f * Mathf.Deg2Rad));
        foreach (float x in new[] { -.10f, .10f }) Rura(4, new Vector3(x, Rf - .02f, zf + .01f), new Vector3(x, Rf, zf) + dirW * .78f, .027f);
        Luk(5, zf, Rf, Rf + .028f, 25f, 125f, .065f);                                          // błotnik przedni 2.8 cm nad oponą
        foreach (float x in new[] { -.082f, .082f }) Pierscien(6, zf, Rf, .085f, .160f, x - .0025f, x + .0025f);   // tarcze 320 mm
        foreach (float x in new[] { -.105f, .105f }) Pudlo(7, new Vector3(x - .02f, Rf - .17f, zf - .2f), new Vector3(x + .02f, Rf - .03f, zf - .1f));   // zaciski
        foreach (float x in new[] { -.15f, .15f }) Pudlo(8, new Vector3(x - .025f, Rr - .035f, zr - .02f), new Vector3(x + .025f, Rr + .035f, zr + .56f));    // wahacz
        Luk(9, zr, Rr, Rr + .03f, 40f, 150f, .10f);                                             // błotnik tylny (hugger)
        Pierscien(14, zr, Rr, .06f, .105f, .083f, .089f);                                       // zębatka (w obrysie opony 190)
        Elipsoida(0, new Vector3(0, .78f, .25f), new Vector3(.17f, .22f, .62f));                // owiewka
        Elipsoida(1, new Vector3(0, .98f, .05f), new Vector3(.16f, .12f, .26f));                // bak
        Elipsoida(2, new Vector3(0, .88f, -.55f), new Vector3(.12f, .08f, .36f));               // ogon
        Rura(3, new Vector3(.12f, .32f, .05f), new Vector3(.17f, .55f, -.62f), .055f);          // wydech boczny
        Rura(15, new Vector3(-.33f, 1.02f, .43f), new Vector3(.33f, 1.02f, .43f), .012f);       // kierownica
        float zMin = V.Min(p => p.z), zMax = V.Max(p => p.z), L = zMax - zMin;
        var kp = BraxKolaMotoru.Szukaj(V, T, true, (zMin + zMax) * .5f, L); var kt = BraxKolaMotoru.Szukaj(V, T, false, (zMin + zMax) * .5f, L);
        var podzial = BraxKolaMotoru.Podzial(V, T.ToArray(), kp, kt, L);
        Console.WriteLine("model testowy: " + V.Count + " wierzchołków, " + T.Count / 3 + " trójkątów, długość " + L.ToString("0.000") + " m");
        Console.WriteLine("przód: " + kp + "   (prawda: oś z " + (zf * K).ToString("0.000") + ", y " + (Rf * K).ToString("0.000") + ", R " + (Rf * K).ToString("0.000") + ", szer. 0.155)");
        Console.WriteLine("tył:   " + kt + "   (prawda: oś z " + (zr * K).ToString("0.000") + ", y " + (Rr * K).ToString("0.000") + ", R " + (Rr * K).ToString("0.000") + ", szer. 0.246)");
        int bledy = 0;
        for (int c = 0; c < Nazwy.Length; c++)
        {
            int n = 0; var ile = new int[3];
            for (int t = 0; t < Czesc.Count; t++) if (Czesc[t] == c) { n++; ile[podzial[t]]++; }
            int dobre = Oczekiwane[c] < 0 ? n : ile[Oczekiwane[c]];
            if (Oczekiwane[c] >= 0 && dobre < n) bledy += n - dobre;
            Console.WriteLine($"  {Nazwy[c],-16} {n,6} trójk.: rama {ile[0],6}, koło P {ile[1],6}, koło T {ile[2],6}  " + (Oczekiwane[c] < 0 ? "(obojętne)" : (dobre == n ? "OK" : "BŁĘDNIE " + (n - dobre))));
        }
        Console.WriteLine("BRAXHORN_TEST_KOLA " + (kp.ok && kt.ok ? "osie znalezione" : "BRAK OSI") + ", trójkątów w złej grupie: " + bledy);
        Directory.CreateDirectory("out");
        File.WriteAllText("out/motor.json", "{\"v\":[" + string.Join(",", V.Select(p => $"[{p.x:0.####},{p.y:0.####},{p.z:0.####}]")) + "],\"t\":[" + string.Join(",", T) +
            "],\"g\":[" + string.Join(",", podzial) + "],\"cz\":[" + string.Join(",", Czesc) + $"],\"osP\":[{kp.x:0.####},{kp.y:0.####},{kp.z:0.####}],\"osT\":[{kt.x:0.####},{kt.y:0.####},{kt.z:0.####}],\"bledy\":{bledy}" + "}");
        Environment.Exit(kp.ok && kt.ok && bledy == 0 ? 0 : 1);
    }
}
