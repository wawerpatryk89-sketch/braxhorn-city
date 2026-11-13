using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// SZYLDY PREMIUM (build 14, decyzje właściciela 2.11): zamiast płaskich tablic na słupach i ramach — litery przestrzenne
// (każda litera bryłą z głębią, czasem na listwie montażowej) albo kasetony (skrzynka z licem z akrylu i literami wypukłymi),
// zawsze NA elewacji nad wejściem albo na wysięgniku prostopadle do ściany (jak „PAWN SHOP” na zdjęciu-wzorcu Urban City).
// Zasady: nic nie zasłania szyldu (daszek / markiza zawsze poniżej, kontrola: promienie 1.5 m od lica do przodu w nic nie
// trafiają — log BRAXHORN_SZYLDY zasloniete=0); szyldy i daszki ponad 2.3 m bez kolizji (blokowały kamerę) i bez płyt
// „spodu stropu” dla kamery; nic nie świeci (zasada sceny — lico kasetonu jasne kolorem, nie emisją; materiały mają
// w nazwie „podświetlenie nocą”, żeby przyszły tryb nocy znalazł je po nazwie). Nazwy: hala walk = „ARENA BRAXHORN”
// (decyzja 2.11, 00:12 — słowa Oktagon/Octagon nigdzie na szyldach). Geometria scalana per budynek i materiał,
// z dystansem widoczności (LODGroup) — FlushElewacje.
public static partial class BraxClaudeOsiedle
{
    // ---------------- rejestr: szyldy do kontroli zasłonięcia, bryły ponad głową do kontroli kolizji ----------------
    sealed class SzyldRek { public string nazwa, grupa; public Vector3 c, n, r, up; public float w, h; }
    static readonly List<SzyldRek> szyldyRej = new List<SzyldRek>();
    static readonly List<(string co, Bounds b)> nadGlowa = new List<(string, Bounds)>();
    static int nSzyldow, nKasetonow, nWysiegnikow, nDaszkow, nNumerow, nStarychTablic;

    static void WyczyscElewacje()
    {
        szyldyRej.Clear(); nadGlowa.Clear(); zasiegGrup.Clear(); celGrupy.Clear(); literyRazem = 0;
        nSzyldow = nKasetonow = nWysiegnikow = nDaszkow = nNumerow = nStarychTablic = 0;
        elBudynki.Clear(); elGzymsy = elOkna = elMarkizy = elDetale = 0;
    }

    static Vector3 Prawo(Vector3 n) => Vector3.Cross(Vector3.up, -n).normalized;

    // grupy generatora dla elewacji i szyldów: zasięg widoczności (m) i grupa docelowa przy scalaniu
    static readonly Dictionary<string, float> zasiegGrup = new Dictionary<string, float>();
    static readonly Dictionary<string, string> celGrupy = new Dictionary<string, string>();
    static void Grupa(string nazwa, float zasieg, string cel = null)
    {
        group = nazwa; cel = cel ?? nazwa;
        zasiegGrup[nazwa] = zasiegGrup.TryGetValue(nazwa, out var z) ? Mathf.Max(z, zasieg) : zasieg; celGrupy[nazwa] = cel;
    }
    // detale drobne (lampy, kamery, kratki, donice) — w kwadratach 24 m, znikają dalej niż ~55 m (budżet telefonu)
    static void GrupaDrobna(string budynek, Vector3 p)
    {
        int cx = Mathf.FloorToInt(p.x / 24f), cz = Mathf.FloorToInt(p.z / 24f);
        Grupa("Elewacje / " + budynek + " — detale " + cx + "," + cz, 55f);
    }

    static void Rejestruj(string nazwa, Vector3 c, Vector3 n, float w, float h)
    {
        szyldyRej.Add(new SzyldRek { nazwa = nazwa, grupa = group, c = c, n = n.normalized, r = Prawo(n), up = Vector3.up, w = w, h = h });
        var b = new Bounds(c, Vector3.zero); var r = Prawo(n);
        foreach (int i in new[] { -1, 1 }) foreach (int j in new[] { -1, 1 }) { b.Encapsulate(c + r * w * .5f * i + Vector3.up * h * .5f * j); b.Encapsulate(c - n * .3f + r * w * .5f * i + Vector3.up * h * .5f * j); }
        nadGlowa.Add((nazwa, b));
    }

    // ---------------- materiały ----------------
    static Material MatLicoBiale => Plain("litery przestrzenne: lico białe akryl (podświetlenie nocą)", new Color(.95f, .95f, .93f), .55f);
    static Material MatBokiCzarne => Plain("litery przestrzenne: boki czarne (aluminium lakierowane)", new Color(.04f, .04f, .045f), .5f, .4f);
    static Material MatBokiStal => Plain("litery przestrzenne: boki stal szczotkowana", new Color(.60f, .61f, .63f), .62f, .9f);
    static Material MatAlu => Plain("aluminium szczotkowane (kasetony, wsporniki)", new Color(.64f, .66f, .68f), .6f, .85f);
    static Material MatGrafit => Plain("stal grafit (wsporniki, lampy, rury)", new Color(.12f, .125f, .13f), .45f, .6f);
    static Material MatZloto => Plain("złoto kasyna (mosiądz szczotkowany)", new Color(.80f, .62f, .25f), .72f, .9f);
    static Material MatLico(string nazwa, Color c) => Plain("litery przestrzenne: lico " + nazwa + " (podświetlenie nocą)", c, .5f, .05f);
    static Material MatBoki(string nazwa, Color c) => Plain("litery przestrzenne: boki " + nazwa, c, .45f, .3f);
    static Material MatKaseton(string nazwa, Color c) => Plain("kaseton: lico " + nazwa + " (podświetlenie nocą)", c, .35f);

    // ---------------- szyld: litery przestrzenne na elewacji ----------------
    // baza = środek linii bazowej napisu na licu ściany (albo pilastrów), n = normalna lica. Litery odsunięte o dystans
    // (kołki dystansowe), opcjonalnie na listwie montażowej (pas za literami). Zwraca szerokość napisu.
    static float SzyldLitery(string budynek, string nazwa, string tekst, Vector3 baza, Vector3 n, float cap, Material lico, Material boki,
                             float glebokosc = .1f, float dystans = .05f, Material listwa = null, float s = .15f, float track = .12f)
    {
        var g0 = group; Grupa("Szyldy / " + budynek + " / " + nazwa, 170f, "Szyldy / " + budynek);
        var r = Prawo(n); float w = SzerokoscNapisu(tekst, cap, s, track);
        if (listwa)
        {
            float lw = w + cap * .6f, ly0 = cap * .28f, ly1 = cap * .66f;
            OBoxN(listwa, baza + n * (dystans * .5f) + Vector3.up * ((ly0 + ly1) * .5f), r, Vector3.up, n, lw, ly1 - ly0, dystans);
        }
        Napis3D(tekst, baza + n * dystans, n, cap, glebokosc, lico, boki, s, track);
        Rejestruj(nazwa, baza + n * (dystans + glebokosc) + Vector3.up * cap * .5f, n, w, cap);
        nSzyldow++; group = g0; return w;
    }

    // ---------------- kaseton (skrzynka świetlna) na ścianie ----------------
    // c = środek tylnej ściany kasetonu na licu, n = normalna; rama aluminiowa, lico z akrylu (kolor) wpuszczone 4 cm od krawędzi,
    // litery wypukłe na licu (linie: tekst, wysokość, przesunięcie w pionie od środka, przesunięcie w bok).
    static void Kaseton(string budynek, string nazwa, Vector3 c, Vector3 n, float w, float h, float d, Material lico, Material rama,
                        (string t, float cap, float dy, float dx)[] linie, Material licoL, Material bokiL, float s = .15f)
    {
        var g0 = group; Grupa("Szyldy / " + budynek + " / " + nazwa, 170f, "Szyldy / " + budynek);
        var r = Prawo(n); var up = Vector3.up;
        OBoxN(rama, c + n * d * .5f, r, up, n, w, h, d);
        var f = c + n * (d + .008f);   // 8 mm przed czołem skrzynki (bez migotania z daleka)
        QN(lico, f - r * (w * .5f - .045f) - up * (h * .5f - .045f), f + r * (w * .5f - .045f) - up * (h * .5f - .045f), f + r * (w * .5f - .045f) + up * (h * .5f - .045f), f - r * (w * .5f - .045f) + up * (h * .5f - .045f), n);
        foreach (var (t, cap, dy, dx) in linie) Napis3D(t, f + up * (dy - cap * .5f) + r * dx, n, cap, .025f, licoL, bokiL, s);
        Rejestruj(nazwa, c + n * (d + .03f), n, w, h);
        nKasetonow++; nSzyldow++; group = g0;
    }

    // ---------------- kaseton na wysięgniku (prostopadle do ściany, dwustronny) ----------------
    // p = punkt na licu ściany na wysokości środka kasetonu, nS = normalna ściany; kaseton od dystans do dystans + wysun,
    // wysokość h, grubość d; litery po obu stronach (poziomo albo pionowo — litera pod literą); obraz = lico z teksturą (logo).
    static void Wysiegnik(string budynek, string nazwa, Vector3 p, Vector3 nS, float dystans, float wysun, float h, float d,
                          Material lico, Material rama, string tekst, float cap, bool pionowo, Material licoL, Material bokiL, Material obraz = null)
    {
        var g0 = group; Grupa("Szyldy / " + budynek + " / " + nazwa, 170f, "Szyldy / " + budynek);
        var t = Prawo(nS); var up = Vector3.up; var c = p + nS * (dystans + wysun * .5f);
        OBoxN(rama, c, nS, up, t, wysun, h, d);
        foreach (float s in new[] { 1f, -1f })
        {
            var fn = t * s; var f = c + fn * (d * .5f + .008f); var rr = Prawo(fn);
            Vector3 q0 = f - rr * (wysun * .5f - .04f) - up * (h * .5f - .04f), q1 = f + rr * (wysun * .5f - .04f) - up * (h * .5f - .04f),
                    q2 = f + rr * (wysun * .5f - .04f) + up * (h * .5f - .04f), q3 = f - rr * (wysun * .5f - .04f) + up * (h * .5f - .04f);
            if (obraz) QuadUV(obraz, q0, q1, q2, q3, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), fn);
            else QN(lico, q0, q1, q2, q3, fn);
            if (!string.IsNullOrEmpty(tekst))
            {
                if (pionowo) { float H = WysokoscPionowego(tekst, cap, cap * .3f); NapisPionowy3D(tekst, f + up * H * .5f, fn, cap, cap * .3f, .02f, licoL, bokiL); }
                else Napis3D(tekst, f - up * cap * .5f, fn, cap, .02f, licoL, bokiL);
            }
            Rejestruj(nazwa + (s > 0 ? " (strona A)" : " (strona B)"), c + fn * (d * .5f + .03f), fn, wysun, h);
        }
        // mocowanie: płyta na ścianie i dwa ramiona (górne i dolne) przez całą długość kasetonu
        OBoxN(rama, p + nS * .015f, t, up, nS, .14f, h + .3f, .03f);
        foreach (float yy in new[] { h * .5f + .06f, -h * .5f - .06f })
            OBoxN(MatGrafit, p + nS * ((dystans + wysun) * .5f) + up * yy, nS, up, t, dystans + wysun, .05f, .05f);
        nWysiegnikow++; nSzyldow++; group = g0;
    }

    // Baner pionowy na wysięgniku (płótno 2 cm na drążku górnym i dolnym, prostopadle do ściany), litery po obu stronach.
    static void BanerPionowy(string budynek, string nazwa, Vector3 p, Vector3 nS, float wysun, float h, Material plotno, string tekst, float cap)
    {
        var g0 = group; Grupa("Szyldy / " + budynek + " / " + nazwa, 170f, "Szyldy / " + budynek);
        var t = Prawo(nS); var up = Vector3.up; const float dyst = .12f; var c = p + nS * (dyst + wysun * .5f);
        OBoxN(plotno, c, nS, up, t, wysun, h, .02f);
        foreach (float yy in new[] { h * .5f + .03f, -h * .5f - .03f }) Tube(MatGrafit, p + nS * .02f + up * yy, p + nS * (dyst + wysun + .05f) + up * yy, .022f, 8);
        OBoxN(MatGrafit, p + nS * .015f, t, up, nS, .1f, h + .2f, .03f);
        foreach (float s in new[] { 1f, -1f })
        {
            var fn = t * s; var f = c + fn * .011f; float H = WysokoscPionowego(tekst, cap, cap * .3f);
            NapisPionowy3D(tekst, f + up * H * .5f, fn, cap, cap * .3f, .006f, MatLicoBiale, MatLicoBiale, .17f);
            Rejestruj(nazwa + (s > 0 ? " (strona A)" : " (strona B)"), c + fn * .03f, fn, wysun, h);
        }
        nSzyldow++; group = g0;
    }

    // ---------------- szklany daszek na wspornikach (bez słupów) ----------------
    // p = środek górnej krawędzi przy ścianie (y = wierzch daszku), n = normalna ściany; w × g; zastrzały od dołu (bez cięgien
    // nad daszkiem — nie przecinają szyldu). Bez kolizji i bez płyty kamery (QN, LODGroup).
    static void DaszekSzklany(string budynek, Vector3 p, Vector3 n, float w, float g, float rozstaw, float zastrzalDol = .6f)
    {
        var g0 = group; Grupa("Elewacje / " + budynek, 140f);
        var t = Prawo(n); var up = Vector3.up; var st = MatGrafit;
        OBoxN(st, p + n * (g - .05f) - up * .07f, t, up, n, w, .14f, .1f);                                     // belka przednia
        foreach (float s in new[] { -1f, 1f }) OBoxN(st, p + t * s * (w * .5f - .04f) + n * (g * .5f) - up * .06f, t, up, n, .08f, .12f, g - .02f);
        OBoxN(st, p + n * .03f - up * .07f, t, up, n, w, .14f, .06f);                                          // płaskownik przy ścianie
        for (float a = -w * .5f + w / 3f; a < w * .5f - .1f; a += w / 3f) OBoxN(st, p + t * a + n * (g * .5f) - up * .045f, t, up, n, .05f, .07f, g - .1f);   // żebra
        OBoxN(GlassClear, p + n * (g * .5f) - up * .004f, t, up, n, w - .08f, .012f, g - .1f);                   // tafla
        foreach (float s in new[] { -1f, 1f })
        {
            var a0 = p + t * s * rozstaw * .5f + n * .02f - up * zastrzalDol; var a1 = p + t * s * rozstaw * .5f + n * (g - .1f) - up * .12f;
            Tube(st, a0, a1, .028f, 8);
            OBoxN(st, a0 + n * .01f, t, up, n, .12f, .18f, .02f);                                                // kotwa
        }
        var b = new Bounds(p + n * (g * .5f) - up * .2f, Vector3.zero);
        b.Encapsulate(p + t * w * .5f + n * g); b.Encapsulate(p - t * w * .5f - up * zastrzalDol); nadGlowa.Add(("daszek " + budynek, b));
        nDaszkow++; group = g0;
    }

    // Godło na kasetonie policji: złoty krąg i srebrna gwiazda (ogólny znak — nie godło państwowe, jak tekstura buildu 5).
    static void Gwiazda(Vector3 c, Vector3 n, float R, float depth, Material lico, Material boki)
    {
        var r = Prawo(n); var up = Vector3.Cross(r, n).normalized; var D = n * depth;
        var p = new Vector3[10];
        for (int i = 0; i < 10; i++) { float a = Mathf.PI * .5f + i * Mathf.PI / 5f, rr = i % 2 == 0 ? R : R * .42f; p[i] = c + (r * Mathf.Cos(a) + up * Mathf.Sin(a)) * rr; }
        for (int i = 0; i < 10; i++) { var q = p[(i + 1) % 10]; TN(lico, c + D, p[i] + D, q + D, n); QN(boki, p[i], q, q + D, p[i] + D, ((p[i] + q) * .5f - c).normalized); }
    }
    static void Krazek(Vector3 c, Vector3 n, float R, float depth, Material lico, Material boki, int seg = 24)
    {
        var r = Prawo(n); var up = Vector3.Cross(r, n).normalized; var D = n * depth;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
            Vector3 p0 = c + (r * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * R, p1 = c + (r * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * R;
            TN(lico, c + D, p0 + D, p1 + D, n); QN(boki, p0, p1, p1 + D, p0 + D, ((p0 + p1) * .5f - c).normalized);
        }
    }

    // ---------------- usuwanie starych płaskich tablic Codexa ----------------
    static int UsunTablice(Transform estate, System.Func<Bounds, bool> pasuje)
    {
        int n = 0;
        foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true).ToList())
        {
            if (!mr) continue; var b = mr.bounds;
            if (pasuje(b)) { UnityEngine.Object.DestroyImmediate(mr.gameObject); n++; }
        }
        nStarychTablic += n; return n;
    }

    // ================= SZYLDY BUDYNKÓW OSIEDLA A (zamiast CivicSigns) =================
    // Budynki Codexa 40 × 30 m (civic.js): lico frontu z = cz + 15 (okładzina CivicFacades 15.185–15.215, pilastry do 15.56,
    // co 5 m poza osią), wejście x cx ± 2 do 3.1 m. Stary daszek Codexa (7 × 3.5 m, 3.10–3.35 m, na dwóch słupkach z = cz + 17.8)
    // zasłaniał szyld (zdjęcia 2.11: „HALA WALK · OKTAGON”, komisariat) i blokował kamerę — wycięty z siatek, kolizje słupków
    // wyłączone; w jego miejscu szklany daszek na wspornikach (wierzch 3.32 m, bez słupów, bez kolizji). Szyld zawsze nad nim.
    // Tablica „WEJŚCIE” Codexa (1.6 × 0.3 m na 3.45–3.75 m) też precz — miejsce na szyld.
    static void SzyldyBudynkowA(Transform estate, Transform physics)
    {
        var g0 = group;
        var list = new (string id, float cx, float cz, float h)[]
        {
            ("komisariat", -86f, -42f, 4.5f), ("szpital", -86f, 14f, 4.5f), ("arena", -86f, 64f, 7f), ("dyskoteka", 86f, 14f, 7f), ("sala bokserska", 86f, 64f, 7f),
        };
        int slupki = 0, tablice = 0;
        foreach (var (id, cx, cz, h) in list)
        {
            float front = cz + 15f, lico = cz + 15.215f, pil = cz + 15.56f;
            // stare tablice: napis budynku (13 × 2.44 m, 2.83–5.27 m) i „WEJŚCIE”
            tablice += UsunTablice(estate, b => b.size.x > 6f && b.size.x < 14.5f && b.size.z < .8f && b.min.y > 2.6f && b.max.y < 5.8f && Mathf.Abs(b.center.x - cx) < 1.5f && b.center.z > front - .2f && b.center.z < front + 1f);
            tablice += UsunTablice(estate, b => b.size.x > 1.3f && b.size.x < 1.9f && b.size.y < .45f && b.min.y > 3.3f && b.max.y < 3.95f && Mathf.Abs(b.center.x - cx) < .6f && b.center.z > front + .15f && b.center.z < front + .6f);
            // stary daszek: wierzch (3.345), spód (3.095), boki i przód osobno — nadproże drzwi (spód na 3.10) zostaje
            if (estate) CutVolumes(estate, physics, new List<Bounds> {
                MinMax(cx - 3.6f, cx + 3.6f, 3.3f, 3.4f, front - .25f, front + 3.45f), MinMax(cx - 3.6f, cx + 3.6f, 3.07f, 3.098f, front - .25f, front + 3.45f),
                MinMax(cx - 3.6f, cx + 3.6f, 3.07f, 3.4f, front + 3.3f, front + 3.45f), MinMax(cx - 3.58f, cx - 3.42f, 3.07f, 3.4f, front - .25f, front + 3.45f), MinMax(cx + 3.42f, cx + 3.58f, 3.07f, 3.4f, front - .25f, front + 3.45f),
                MinMax(cx - 3.25f, cx - 2.95f, -.05f, 3.15f, front + 2.65f, front + 2.95f), MinMax(cx + 2.95f, cx + 3.25f, -.05f, 3.15f, front + 2.65f, front + 2.95f) });
            if (physics)
                foreach (var c in physics.GetComponentsInChildren<Collider>(true))
                {
                    var b = c.bounds;
                    if (c.enabled && b.size.x < 1f && b.size.z < 1f && b.size.y > 2.5f && Mathf.Abs(Mathf.Abs(b.center.x - cx) - 3.1f) < .4f && Mathf.Abs(b.center.z - (front + 2.8f)) < .45f) { c.enabled = false; slupki++; }
                }
            string bud = "A " + id;
            DaszekSzklany(bud, new Vector3(cx, 3.24f, lico), Vector3.forward, 6.2f, 1.25f, 5.5f, .62f);   // płytko i nisko: z ulicy nie zasłania dolnej linii szyldu
            var fwd = Vector3.forward;
            switch (id)
            {
                case "komisariat":
                {
                    // granatowy kaseton z godłem (krąg + gwiazda) i literami POLICJA / KOMISARIAT, nad daszkiem (3.46–4.32 m)
                    var navy = MatKaseton("granat policji", new Color(.05f, .12f, .32f));
                    var c = new Vector3(cx, 3.89f, lico);
                    Kaseton(bud, "POLICJA (kaseton)", c, fwd, 6.8f, .86f, .2f, navy, MatAlu, new[] { ("POLICJA", .40f, .13f, .45f), ("KOMISARIAT", .16f, -.25f, .45f) }, MatLicoBiale, MatBokiStal);
                    var g1 = group; Grupa("Szyldy / " + bud + " / POLICJA (kaseton)", 170f, "Szyldy / " + bud);
                    var e = c + fwd * .212f - Prawo(fwd) * 2.75f;                     // godło po lewej patrzącego
                    Krazek(e, fwd, .33f, .02f, MatZloto, MatZloto); Krazek(e + fwd * .02f, fwd, .27f, .01f, navy, navy);
                    Gwiazda(e + fwd * .03f, fwd, .24f, .025f, MatBokiStal, MatBokiStal);
                    group = g1;
                    FlagaPolska(bud, new Vector3(cx - 5f, 3.55f, pil), fwd);
                    break;
                }
                case "szpital":
                {
                    var blue = new Color(.06f, .27f, .62f);
                    var rr = Prawo(fwd);
                    float w = SzyldLitery(bud, "SZPITAL", "SZPITAL", new Vector3(cx, 3.58f, lico) + rr * .45f, fwd, .56f, MatLico("niebieskie", blue), MatBokiStal, .1f);
                    // obok (po lewej patrzącego): niebieski kaseton z białym H
                    Kaseton(bud, "H (kaseton)", new Vector3(cx, 3.86f, lico) + rr * (.45f - w * .5f - .65f), fwd, .74f, .74f, .18f, MatKaseton("niebieski szpital", blue), MatAlu, new[] { ("H", .46f, 0f, 0f) }, MatLicoBiale, MatBokiStal, .2f);
                    // wysięgnik z H przy pilastrze (widać go wzdłuż chodnika) i flaga
                    Wysiegnik(bud, "H (wysięgnik)", new Vector3(cx + 10f, 3.9f, pil), fwd, .08f, .78f, .78f, .16f, MatKaseton("niebieski szpital", blue), MatAlu, "H", .44f, false, MatLicoBiale, MatBokiStal);
                    FlagaPolska(bud, new Vector3(cx - 5f, 3.55f, pil), fwd);
                    break;
                }
                case "arena":
                {
                    // ARENA BRAXHORN: białe litery z czerwonymi bokami 1 m na czarnej listwie (przed pilastrami), pod spodem OSIEDLE;
                    // pionowy czerwony kaseton ARENA na wysięgniku przy pilastrze (jak na zdjęciu-wzorcu)
                    var red = new Color(.66f, .05f, .06f);
                    // 3.11 16:06 (właściciel: „Arena BRAXHORN?” — szyldy premium z grafik): szyld główny 4:1 i banery 1:4 z paczki grafik;
                    // bez grafik — litery 3D jak dotąd
                    if (!SzyldPanel("arena_szyld", new Vector3(cx, 4.6f, pil) + fwd * .03f, fwd, 7.5f, 2.5f))
                    {
                        SzyldLitery(bud, "ARENA BRAXHORN", "ARENA BRAXHORN", new Vector3(cx, 4.2f, pil), fwd, .9f, MatLicoBiale, MatBoki("czerwone", red), .14f, .07f, MatBokiCzarne, .15f, .11f);
                        SzyldLitery(bud, "OSIEDLE", "OSIEDLE", new Vector3(cx, 3.56f, lico), fwd, .3f, MatLicoBiale, MatBoki("czerwone", red), .06f, .04f);
                    }
                    Wysiegnik(bud, "ARENA (wysięgnik pionowy)", new Vector3(cx + 10f, 5.0f, pil), fwd, .1f, .74f, 3.1f, .2f, MatKaseton("czerwony arena", red), MatBokiCzarne, "ARENA", .42f, true, MatLicoBiale, MatBokiCzarne);
                    // banery LIGA na wysięgnikach przy skrajnych pilastrach (płótno na drążkach, litery pionowo)
                    foreach (float s in new[] { -15f, 15f })
                        if (!SzyldPanel("arena_baner", new Vector3(cx + s, 5.0f, pil) + fwd * .03f, fwd, 1.0f, 3.0f))
                            BanerPionowy(bud, "baner LIGA " + (s < 0 ? "L" : "P"), new Vector3(cx + s, 5.0f, pil), fwd, .9f, 3.0f, Plain("baner płótno czerwone", red, .15f), "LIGA", .4f);
                    break;
                }
                case "dyskoteka":
                {
                    var mag = new Color(.82f, .14f, .62f);
                    // szyld neonowy z grafiki właściciela (3.11 15:52, PULS CLUB · BRAXHORN, 3:1); bez paczki — litery 3D
                    if (SzyldPanel("puls_club", new Vector3(cx, 4.3f, lico + fwd.z * .02f + fwd.x * .02f), fwd, 3.9f, 1.3f)) break;
                    SzyldLitery(bud, "PULS CLUB (nazwa osiedla, 3.11 15:47 — inna niż NOVA w mieście)", "PULS CLUB", new Vector3(cx, 4.25f, lico), fwd, .95f, MatLico("magenta", mag), MatBokiCzarne, .13f, .06f);
                    SzyldLitery(bud, "CLUB · DJ · DANCE", "CLUB · DJ · DANCE", new Vector3(cx, 3.6f, lico), fwd, .24f, MatLicoBiale, MatBoki("fiolet", new Color(.40f, .10f, .52f)), .05f, .04f);
                    break;
                }
                case "sala bokserska":
                {
                    var red = new Color(.66f, .05f, .06f);
                    // 16:30: sala bokserska = klub PRZECIWNIKA — WAWER FIGHT CLUB (grafika z arkusza 5:1); bez niej — BRAXHORN BOXING / litery
                    if (SzyldPanel("wawer_fight", new Vector3(cx, 4.45f, pil) + fwd * .03f, fwd, 6.5f, 1.3f)) break;
                    if (SzyldPanel("boxing_szyld", new Vector3(cx, 4.45f, pil) + fwd * .03f, fwd, 6f, 2f)) break;
                    SzyldLitery(bud, "SALA BOKSERSKA", "SALA BOKSERSKA", new Vector3(cx, 4.25f, pil), fwd, .85f, MatLicoBiale, MatBokiCzarne, .12f, .07f, MatLico("czerwone", red));
                    SzyldLitery(bud, "BRAXHORN BOXING", "BRAXHORN BOXING", new Vector3(cx, 3.58f, lico), fwd, .26f, MatLico("czerwone", red), MatBokiCzarne, .06f, .04f);
                    break;
                }
            }
            // kinkiety po bokach wejścia, donice przy elewacji
            foreach (float s in new[] { -1f, 1f })
            {
                GrupaDrobna(bud, new Vector3(cx, 0, front));
                Kinkiet(new Vector3(cx + s * 2.45f, 2.15f, lico), fwd);
                Planter(new Vector3(cx + s * 3.0f, .05f, lico + .4f), .66f, .5f, 811 + (int)cx + (int)cz + (s > 0 ? 1 : 0), 0, 2);
            }
            elBudynki.Add(bud);
        }
        rep.notes.Add("szyldy premium A: 5 budynków (litery przestrzenne / kasetony / wysięgniki), stare tablice usunięte: " + tablice + ", słupki daszków Codexa: " + slupki + " kolizji wyłączonych");
        group = g0;
    }

    // Flaga Polski na ukośnym drzewcu przy ścianie (budynki publiczne): drzewce 40° w górę od lica, płat 1.2 × 0.75 m z fałdami.
    static void FlagaPolska(string budynek, Vector3 p, Vector3 n, float dl = 2.1f)
    {
        var g0 = group; Grupa("Elewacje / " + budynek, 140f);
        var dir = (n * Mathf.Cos(40f * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(40f * Mathf.Deg2Rad)).normalized;
        var bok = Prawo(n);
        OBoxN(MatGrafit, p + n * .02f, bok, Vector3.up, n, .12f, .2f, .04f);
        Tube(MatAlu, p + n * .04f, p + dir * dl, .022f, 8);
        OBoxN(MatZloto, p + dir * (dl + .04f), bok, Vector3.up, n, .07f, .07f, .07f);
        var bialy = Plain("flaga biel", new Color(.94f, .94f, .93f), .2f); var czerw = Plain("flaga czerwień", new Color(.84f, .07f, .15f), .2f);
        const int panele = 6; float s0 = .3f, s1 = s0 + 1.2f;
        for (int i = 0; i < panele; i++)
        {
            float a0 = Mathf.Lerp(s0, s1, i / (float)panele), a1 = Mathf.Lerp(s0, s1, (i + 1) / (float)panele);
            float o0 = (i % 2 == 0 ? .04f : -.04f) * (i == 0 ? 0f : 1f), o1 = ((i + 1) % 2 == 0 ? .04f : -.04f);
            Vector3 t0 = p + dir * a0 + bok * o0, t1 = p + dir * a1 + bok * o1;
            foreach (var (m, y0, y1) in new[] { (bialy, 0f, .375f), (czerw, .375f, .75f) })
            {
                Vector3 q0 = t0 - Vector3.up * y0, q1 = t1 - Vector3.up * y0, q2 = t1 - Vector3.up * y1, q3 = t0 - Vector3.up * y1;
                var fn = Vector3.Cross(q1 - q0, q3 - q0).normalized;
                QN(m, q0, q1, q2, q3, fn); QN(m, q0, q1, q2, q3, -fn);
            }
        }
        group = g0;
    }

    // ================= SIŁOWNIA A, KORT, STRÓŻÓWKA, WILLA CODEXA =================
    // Siłownia BRAXHORN (GymA): szyld nad wejściem i drugi od parku — litery przestrzenne zamiast tablic „braxhorn”.
    static void SzyldSilowniFront(float dX, float sY0, float fz1, float maxW)
    {
        float cap = .8f; while (SzerokoscNapisu("BRAXHORN", cap) > maxW && cap > .4f) cap -= .02f;
        // 16:30 (właściciel: „Braxa — BRAXHORN”): siłownia A = klub Braxa (w środku BRAXHORN GYM) — szyld BRAXHORN BOXING z grafiki
        if (SzyldPanel("boxing_szyld", new Vector3(dX, sY0 + .3f, fz1) + Vector3.forward * .03f, Vector3.forward, 2.4f, .8f)) wawerFight = true;
        else SzyldLitery("A siłownia", "BRAXHORN (front)", "BRAXHORN", new Vector3(dX, sY0 + .42f, fz1), Vector3.forward, cap, MatLicoBiale, MatBokiCzarne, .12f, .05f);
        if (!wawerFight) SzyldLitery("A siłownia", "KICKBOXING (front)", "KICKBOXING", new Vector3(dX, sY0, fz1), Vector3.forward, .26f, MatLico("zielone BRAXHORN", new Color(.36f, .78f, .18f)), MatBokiCzarne, .05f, .04f);
    }
    static bool wawerFight;
    static void SzyldSilowniPark(Vector3 srodek, float yBaza)
    {
        if (SzyldPanel("boxing_szyld", new Vector3(srodek.x, yBaza + .6f, srodek.z) + Vector3.back * .03f, Vector3.back, 5.4f, 1.8f)) return;   // klub Braxa
        SzyldLitery("A siłownia", "BRAXHORN (od parku)", "BRAXHORN", new Vector3(srodek.x, yBaza + .65f, srodek.z), Vector3.back, 1.5f, MatLicoBiale, MatBokiCzarne, .16f, .06f);
        SzyldLitery("A siłownia", "KICKBOXING (od parku)", "KICKBOXING", new Vector3(srodek.x, yBaza, srodek.z), Vector3.back, .45f, MatLico("zielone BRAXHORN", new Color(.36f, .78f, .18f)), MatBokiCzarne, .07f, .05f);
    }

    // Klub tenisowy: brama wejściowa (dwa słupy z klinkieru, belka) z napisem KLUB TENISOWY zamiast tablicy na siatce.
    static void SzyldKortu(float gx, float gate, float dir)
    {
        var g0 = group; Grupa("Elewacje / B klub tenisowy", 140f);
        var n = new Vector3(0, 0, -dir);
        foreach (float s in new[] { -1f, 1f }) BoxMinMax(Clinker, gx + s * 1.35f - .2f, gx + s * 1.35f + .2f, 0, 3.0f, gate - .2f, gate + .2f, true);
        foreach (float s in new[] { -1f, 1f }) BoxN(Concrete, gx + s * 1.35f - .24f, gx + s * 1.35f + .24f, 3.0f, 3.06f, gate - .24f, gate + .24f);
        BoxN(Anthracite, gx - 1.6f, gx + 1.6f, 3.06f, 3.42f, gate - .16f, gate + .16f);
        group = g0;
        SzyldLitery("B klub tenisowy", "KLUB TENISOWY", "KLUB TENISOWY", new Vector3(gx, 3.13f, gate + n.z * .16f), n, .2f, MatLicoBiale, MatBoki("zielone kort", new Color(.12f, .36f, .22f)), .04f, .02f, null, .16f);
        nadGlowa.Add(("belka bramy kortu", new Bounds(new Vector3(gx, 3.24f, gate), new Vector3(3.2f, .36f, .32f))));
    }

    static void SzyldOchrony(float x, float z)
    {
        SzyldLitery("park stróżówka", "OCHRONA", "OCHRONA", new Vector3(x, 2.4f, z + 1.6f), Vector3.forward, .24f, MatLicoBiale, MatBokiCzarne, .05f, .03f);
    }

    // Willa Codexa (Wiśniowa 4): tablica „WIŚNIOWA 4 · WAWER” (world.js: 3.6 × 0.68 m na x −5.9, y 1.85, z −29.8) → sam numer 4
    // (decyzja właściciela) — cyfra ze stali szczotkowanej na drewnianym panelu przy wejściu, obok lampa.
    static void NumerWillaCodexa(Transform estate)
    {
        if (!estate) return;
        int n = UsunTablice(estate, b => b.size.x > 3.3f && b.size.x < 3.9f && b.size.y > .55f && b.size.y < .8f && b.size.z < .2f && Mathf.Abs(b.center.x + 5.9f) < .6f && Mathf.Abs(b.center.y - 1.85f) < .3f && Mathf.Abs(b.center.z + 29.8f) < .4f);
        if (StacjaWlaczona) return;
        var g0 = group; Grupa("Elewacje / A willa Wiśniowa 4", 60f);
        Napis3D("4", new Vector3(-3.2f, 1.82f, -29.74f), Vector3.forward, .34f, .04f, MatBokiStal, MatBokiStal, .16f);
        Kinkiet(new Vector3(-3.2f, 2.42f, -29.76f), Vector3.forward);
        nNumerow++; group = g0;
        rep.notes.Add("willa Codexa (Wiśniowa 4): tablica „WIŚNIOWA 4 · WAWER” usunięta (" + n + " siatek), numer 4 ze stali na panelu wejścia");
    }

    // Numer domu z cyfr przestrzennych (stal szczotkowana albo czerń) na licu ściany: o = środek linii bazowej.
    static void NumerDomu(string budynek, string numer, Vector3 o, Vector3 n, float cap = .2f, Material m = null)
    {
        var g0 = group; GrupaDrobna(budynek, o);
        m = m ? m : MatBokiStal;
        Napis3D(numer, o + n * .02f, n, cap, .03f, m, m, .16f);
        nNumerow++; group = g0;
    }

    // ================= KONTROLA: zasłonięcie szyldów, kolizje ponad głową =================
    static readonly Dictionary<Batch, Bounds> granicePaczek = new Dictionary<Batch, Bounds>();
    static Bounds GranicePaczki(Batch b)
    {
        if (granicePaczek.TryGetValue(b, out var g)) return g;
        g = new Bounds(b.v[0], Vector3.zero); foreach (var v in b.v) g.Encapsulate(v);
        granicePaczek[b] = g; return g;
    }
    static bool Promien(Vector3 o, Vector3 d, float len, Vector3 a, Vector3 b, Vector3 c)
    {
        var e1 = b - a; var e2 = c - a; var p = Vector3.Cross(d, e2); float det = Vector3.Dot(e1, p);
        if (Mathf.Abs(det) < 1e-9f) return false;
        float inv = 1f / det; var tv = o - a; float u = Vector3.Dot(tv, p) * inv; if (u < 0f || u > 1f) return false;
        var q = Vector3.Cross(tv, e1); float v = Vector3.Dot(d, q) * inv; if (v < 0f || u + v > 1f) return false;
        float t = Vector3.Dot(e2, q) * inv; return t > 0f && t < len;
    }
    static readonly Dictionary<MeshFilter, (Vector3[] v, int[] t)> siatkiSwiat = new Dictionary<MeshFilter, (Vector3[], int[])>();
    static string CoZaslania(SzyldRek s, List<MeshFilter> sceny)
    {
        const float L = 1.5f;
        var starty = new List<Vector3> { s.c };
        foreach (float a in new[] { -.35f, .35f }) starty.Add(s.c + s.r * s.w * a);
        foreach (float a in new[] { -.3f, .3f }) starty.Add(s.c + s.up * s.h * a);
        foreach (var st in starty)
        {
            var o = st + s.n * .02f; var seg = new Bounds(o, Vector3.zero); seg.Encapsulate(o + s.n * L); seg.Expand(.02f);
            foreach (var kv in batches)
            {
                if (kv.Key.Item1 == s.grupa || kv.Value.v.Count == 0) continue;
                if (!GranicePaczki(kv.Value).Intersects(seg)) continue;
                var bv = kv.Value.v; var bi = kv.Value.i;
                for (int k = 0; k + 2 < bi.Count; k += 3)
                {
                    Vector3 a = bv[bi[k]], b = bv[bi[k + 1]], c = bv[bi[k + 2]];
                    var tb = new Bounds(a, Vector3.zero); tb.Encapsulate(b); tb.Encapsulate(c);
                    if (tb.Intersects(seg) && Promien(o, s.n, L, a, b, c)) return kv.Key.Item1 + " / " + kv.Key.Item2.name;
                }
            }
            foreach (var mf in sceny)
            {
                if (!mf) continue; var mr = mf.GetComponent<MeshRenderer>(); if (!mr || !mr.enabled || !mr.bounds.Intersects(seg)) continue;
                if (!siatkiSwiat.TryGetValue(mf, out var sw))
                {
                    try { var m = mf.sharedMesh; var M = mf.transform.localToWorldMatrix; sw = m.isReadable ? (m.vertices.Select(p => M.MultiplyPoint3x4(p)).ToArray(), m.triangles) : (new Vector3[0], new int[0]); }
                    catch { sw = (new Vector3[0], new int[0]); }
                    siatkiSwiat[mf] = sw;
                }
                for (int k = 0; k + 2 < sw.t.Length; k += 3)
                {
                    Vector3 a = sw.v[sw.t[k]], b = sw.v[sw.t[k + 1]], c = sw.v[sw.t[k + 2]];
                    var tb = new Bounds(a, Vector3.zero); tb.Encapsulate(b); tb.Encapsulate(c);
                    if (tb.Intersects(seg) && Promien(o, s.n, L, a, b, c)) return mf.name;
                }
            }
        }
        return null;
    }
    static int KontrolaSzyldow(Transform estate, Transform physics)
    {
        granicePaczek.Clear(); siatkiSwiat.Clear();
        var sceny = new List<MeshFilter>();
        foreach (var t in new[] { estate, root }) if (t) sceny.AddRange(t.GetComponentsInChildren<MeshFilter>(false).Where(m => m && m.sharedMesh));
        int zasl = 0;
        foreach (var s in szyldyRej)
        {
            var co = CoZaslania(s, sceny);
            if (co != null) { zasl++; rep.notes.Add("SZYLD ZASŁONIĘTY: " + s.nazwa + " ← " + co); Debug.LogWarning("BRAXHORN_SZYLDY zasłonięty: " + s.nazwa + " ← " + co); }
        }
        // kolizje ponad 2.3 m w bryłach szyldów i daszków (kamera): wyłączone i policzone
        int kol = 0;
        foreach (var t in new[] { colliders, physics })
        {
            if (!t) continue;
            foreach (var c in t.GetComponentsInChildren<Collider>(true))
            {
                if (!c.enabled) continue; var b = c.bounds; if (b.min.y < 2.25f) continue;
                // tylko bryły w całości w obrysie szyldu / daszku (stare daszki, tablice) — nie ściany i nadproża budynków
                foreach (var (co, nb) in nadGlowa) { var e = nb; e.Expand(.3f); if (e.Contains(b.min) && e.Contains(b.max)) { c.enabled = false; kol++; break; } }
            }
        }
        if (kol > 0) rep.notes.Add("szyldy i daszki: " + kol + " kolizji ponad 2.3 m wyłączonych (kamera)");
        granicePaczek.Clear(); siatkiSwiat.Clear();
        return zasl;
    }

    // ================= SCALANIE: siatki elewacji i szyldów z zasięgiem widoczności =================
    // Paczki z grup zarejestrowanych przez Grupa() → jedna siatka na (grupa docelowa, materiał), wszystkie siatki grupy pod
    // jednym LODGroup: znikają dalej niż zasięg (przy lodBias 1; telefon „średnia” 0.8 → ~0.8 zasięgu). Bez płyt kamery
    // (BraxClaudeSufity pomija LODGroup). Cienie: detale drobne bez cieni.
    static int elewN;
    static void FlushElewacje()
    {
        var klucze = batches.Keys.Where(k => zasiegGrup.ContainsKey(k.Item1) && batches[k].v.Count > 0).ToList();
        var poCelu = klucze.GroupBy(k => celGrupy[k.Item1]).ToList();
        float tanHalf = Mathf.Tan(27.5f * Mathf.Deg2Rad);   // kamera gracza: pole widzenia 55°
        foreach (var gr in poCelu)
        {
            float zasieg = gr.Max(k => zasiegGrup[k.Item1]);
            var parent = new GameObject("siatki (widoczne do ~" + zasieg.ToString("0") + " m)").transform; parent.SetParent(Group(gr.Key), false);
            var rs = new List<Renderer>();
            foreach (var pm in gr.GroupBy(k => k.Item2))
            {
                var mat = pm.Key; var v = new List<Vector3>(); var nn = new List<Vector3>(); var tt = new List<Vector4>(); var uv = new List<Vector2>(); var ii = new List<int>();
                foreach (var k in pm) { var b = batches[k]; int off = v.Count; v.AddRange(b.v); nn.AddRange(b.n); tt.AddRange(b.t); uv.AddRange(b.uv); foreach (var i in b.i) ii.Add(i + off); }
                var mesh = new Mesh { name = gr.Key + " / " + mat.name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v); mesh.SetNormals(nn); mesh.SetTangents(tt); mesh.SetUVs(0, uv); mesh.SetTriangles(ii, 0); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, Out + "/elew_" + (elewN++) + ".asset");
                var g = new GameObject(mat.name); g.transform.SetParent(parent, false);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.receiveShadows = true;
                r.shadowCastingMode = zasieg <= 60f || mat.renderQueue >= 2450 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                rs.Add(r); rep.meshes++; rep.triangles += ii.Count / 3;
            }
            foreach (var k in gr) batches.Remove(k);
            if (rs.Count == 0) { Object.DestroyImmediate(parent.gameObject); continue; }
            var lod = parent.gameObject.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.01f, rs.ToArray()) }); lod.RecalculateBounds();
            float cull = Mathf.Clamp(lod.size / (2f * zasieg * tanHalf), .002f, .9f);
            lod.SetLODs(new[] { new LOD(cull, rs.ToArray()) }); lod.RecalculateBounds();
        }
    }

    // ================= KONIEC: detale A, kontrola, scalanie, logi =================
    static void ElewacjeKoniec(Transform estate, Transform physics)
    {
        var g0 = group;
        Guard("elewacje A: boki i tyły budynków publicznych (rury, kratki, kamery, skrzynki, daszki serwisowe)", () => ElewacjeBudynkowA());
        Guard("elewacje A: willa Codexa — numer 4", () => NumerWillaCodexa(estate));
        int zasl = 0;
        Guard("kontrola zasłonięcia szyldów", () => zasl = KontrolaSzyldow(estate, physics));
        Guard("scalanie elewacji i szyldów (LOD)", FlushElewacje);
        Debug.Log("BRAXHORN_ELEWACJE budynki=" + elBudynki.Count + " gzymsy=" + elGzymsy + " okna=" + elOkna + " markizy=" + elMarkizy + " detale=" + elDetale + " daszki=" + nDaszkow + " numery=" + nNumerow);
        Debug.Log("BRAXHORN_SZYLDY premium=" + nSzyldow + " zasloniete=" + zasl + " kasetony=" + nKasetonow + " wysiegniki=" + nWysiegnikow + " litery=" + literyRazem + " stare_tablice=" + nStarychTablic);
        rep.notes.Add("elewacje: " + elBudynki.Count + " budynków, gzymsy " + elGzymsy + ", okna " + elOkna + ", markizy " + elMarkizy + "; szyldy premium " + nSzyldow + " (zasłonięte " + zasl + ")");
        group = g0;
    }
}
