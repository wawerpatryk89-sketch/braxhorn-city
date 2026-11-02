using System;
using System.Collections.Generic;
using UnityEngine;

// DETALE ELEWACJI (build 14, właściciel 2.11 wg zdjęcia-wzorca „Urban City Buildings Pack”): gzymsy (pod dachem i między
// kondygnacjami), ramy okien z głębią i kamienne parapety, pilastry między lokalami, cokół, rury spustowe, markizy w paski nad
// witrynami (kolory lokali), kasetony na wysięgnikach, kinkiety przy wejściach, numery budynków, donice, kamery / skrzynki
// elektryczne / kratki wentylacyjne na ścianach od podwórza i parkingu, daszki nad wejściami służbowymi, zasłony w oknach
// mieszkań nad sklepami (przed „pokojem za szybą” OsiedleOkno), schody pożarowe tylko przy kamienicy (pierzeja B).
// Bryły i działki bez zmian. Wymiary ludzkie × K (wzrost bohatera): prześwit pod markizami i daszkami ≥ 2.3 m × K.
// Geometria w grupach „Elewacje / …” (scalanie per budynek i materiał, LOD: duże elementy do ~140 m, drobne do ~55 m).
public static partial class BraxClaudeOsiedle
{
    static readonly HashSet<string> elBudynki = new HashSet<string>();
    static int elGzymsy, elOkna, elMarkizy, elDetale;

    static Material MatSzkloMleczne => Plain("szkło mleczne lamp (nie świeci; nocą podświetlenie)", new Color(.96f, .93f, .84f), .7f);
    static Material MatRynna => MatGrafit;   // rury spustowe w tym samym materiale co wsporniki i lampy (mniej wywołań rysowania)
    static Material MatPlastikBialy => MatSzaraSkrzynka;   // obudowy kamer = ten sam jasny RAL co skrzynki
    static Material MatSzaraSkrzynka => Plain("skrzynka elektryczna szara (RAL 7035)", new Color(.72f, .73f, .72f), .35f, .2f);
    static readonly Color[] KoloryZaslon = { new Color(.90f, .87f, .80f), new Color(.80f, .74f, .64f), new Color(.62f, .66f, .68f), new Color(.55f, .20f, .22f), new Color(.46f, .52f, .42f), new Color(.93f, .92f, .89f), new Color(.30f, .34f, .45f) };

    // ---------------- drobne elementy elewacji ----------------
    // Kinkiet nowoczesny przy wejściu: płyta, korpus grafitowy 12 × 20 cm, mleczne szkło (jasne, nie świeci).
    static void Kinkiet(Vector3 p, Vector3 n)
    {
        var t = Prawo(n); var up = Vector3.up;
        OBoxN(MatGrafit, p + n * .01f, t, up, n, .1f, .26f, .02f);
        OBoxN(MatGrafit, p + n * .08f, t, up, n, .12f, .2f, .12f);
        OBoxN(MatSzkloMleczne, p + n * .142f, t, up, n, .08f, .15f, .004f);
        OBoxN(MatSzkloMleczne, p + n * .08f + up * .102f, t, up, n, .08f, .004f, .08f);
        OBoxN(MatSzkloMleczne, p + n * .08f - up * .102f, t, up, n, .08f, .004f, .08f);
        elDetale++;
    }
    // Latarenka klasyczna (dom jednorodzinny): ramię, czterospadowy daszek, mleczne szyby w czarnej ramce.
    static void Latarenka(Vector3 p, Vector3 n)
    {
        var t = Prawo(n); var up = Vector3.up; var blk = MatBokiCzarne;
        OBoxN(blk, p + n * .01f, t, up, n, .09f, .18f, .02f);
        Tube(blk, p + n * .02f + up * .04f, p + n * .22f + up * .04f, .012f, 6);
        var c = p + n * .24f - up * .08f;
        OBoxN(MatSzkloMleczne, c, t, up, n, .13f, .2f, .13f);
        foreach (int i in new[] { -1, 1 }) foreach (int j in new[] { -1, 1 }) OBoxN(blk, c + t * i * .065f + n * j * .065f, t, up, n, .014f, .2f, .014f);
        OBoxN(blk, c - up * .11f, t, up, n, .15f, .03f, .15f);
        var top = c + up * .1f; var apex = top + up * .08f;
        Vector3[] k = { top - t * .09f - n * .09f, top + t * .09f - n * .09f, top + t * .09f + n * .09f, top - t * .09f + n * .09f };
        for (int i = 0; i < 4; i++) { var a = k[i]; var b = k[(i + 1) % 4]; TN(blk, a, b, apex, Normal(a, b, apex, ((a + b) * .5f - top).normalized + up)); }
        elDetale++;
    }
    // Kamera monitoringu na wysięgniku (biała obudowa, czarny obiektyw), obrócona o kąt wzdłuż ściany i pochylona w dół.
    static void Kamera(Vector3 p, Vector3 n, float obrot = 30f)
    {
        var t = Prawo(n); var up = Vector3.up;
        OBoxN(MatPlastikBialy, p + n * .01f, t, up, n, .1f, .14f, .02f);
        var dir = (Quaternion.AngleAxis(obrot, up) * n); dir = (dir - up * .35f).normalized;
        Tube(MatPlastikBialy, p + n * .02f, p + n * .16f, .02f, 6);
        var c = p + n * .16f + dir * .1f;
        var bok = Vector3.Cross(up, dir).normalized; var gora = Vector3.Cross(dir, bok).normalized;
        OBoxN(MatPlastikBialy, c, bok, gora, dir, .09f, .085f, .24f);
        OBoxN(MatPlastikBialy, c + gora * .055f + dir * .03f, bok, gora, dir, .12f, .015f, .3f);       // daszek osłony
        OBoxN(MatGrafit, c + dir * .121f, bok, gora, dir, .06f, .06f, .004f);
        elDetale++;
    }
    // Skrzynka elektryczna (złącze kablowe) na ścianie: szara obudowa 0.6 × 1.0 × 0.22 m, drzwiczki, kratka, cokolik.
    static void Skrzynka(Vector3 dol, Vector3 n)
    {
        var t = Prawo(n); var up = Vector3.up;
        OBoxN(MatSzaraSkrzynka, dol + up * .55f + n * .11f, t, up, n, .6f, 1.0f, .22f);
        OBoxN(MatGrafit, dol + up * .55f + n * .222f, t, up, n, .006f, .9f, .004f);                   // szczelina drzwiczek
        OBoxN(MatGrafit, dol + up * .64f + n * .224f + t * .2f, t, up, n, .03f, .07f, .01f);          // zamek
        for (int i = 0; i < 4; i++) OBoxN(MatGrafit, dol + up * (.15f + i * .035f) + n * .222f - t * .15f, t, up, n, .18f, .012f, .004f);
        OBoxN(Concrete, dol + up * .025f + n * .13f, t, up, n, .66f, .05f, .28f);
        Collider(dol + up * .55f + n * .11f, new Vector3(Mathf.Abs(t.x) * .6f + Mathf.Abs(n.x) * .22f, 1.0f, Mathf.Abs(t.z) * .6f + Mathf.Abs(n.z) * .22f));
        elDetale++;
    }
    // Kratka wentylacyjna: ramka i skośne lamele.
    static void Kratka(Vector3 p, Vector3 n, float w = .5f, float h = .32f)
    {
        var t = Prawo(n); var up = Vector3.up; var m = MatAlu;
        OBoxN(m, p + up * (h * .5f - .015f) + n * .015f, t, up, n, w, .03f, .03f); OBoxN(m, p - up * (h * .5f - .015f) + n * .015f, t, up, n, w, .03f, .03f);
        OBoxN(m, p + t * (w * .5f - .015f) + n * .015f, t, up, n, .03f, h, .03f); OBoxN(m, p - t * (w * .5f - .015f) + n * .015f, t, up, n, .03f, h, .03f);
        OBoxN(MatGrafit, p + n * .002f, t, up, n, w - .04f, h - .04f, .004f);
        var lam = Vector3.Cross(t, Quaternion.AngleAxis(-35f, t) * n).normalized;
        for (float y = -h * .5f + .05f; y < h * .5f - .03f; y += .045f) OBoxN(m, p + up * y + n * .018f, t, lam, Vector3.Cross(t, lam).normalized, w - .06f, .035f, .006f);
        elDetale++;
    }
    // Rura spustowa: lej pod gzymsem, rura Ø 10 cm 6 cm od lica, uchwyty co 2 m, kolano wylotowe przy ziemi.
    static void RuraSpustowa(Vector3 dolNaLicu, Vector3 n, float yGora)
    {
        var up = Vector3.up; var t = Prawo(n); var c = dolNaLicu + n * .08f; c.y = 0f;
        Cylinder(MatRynna, c + up * .12f, .05f, yGora - .32f, 10, false, false);
        OBoxN(MatRynna, c + up * (yGora - .12f), t, up, n, .2f, .24f, .16f);                            // lej
        for (float y = 1.2f; y < yGora - .4f; y += 2f) OBoxN(MatRynna, c + up * y - n * .04f, t, up, n, .13f, .03f, .1f);
        OBoxN(MatRynna, c + up * .07f + n * .07f, t, up, n, .1f, .1f, .24f);                             // kolano
        elDetale++;
    }
    // Zasłony po bokach okna (przed szybą „pokoju za szybą” — tam, gdzie shader rysuje wnętrze): dwa panele w fałdach,
    // kolor i szerokość losowane z ziarna (każde mieszkanie inne), lambrekin u góry.
    static void Zaslony(Vector3 lewyDol, Vector3 t, Vector3 n, float w, float h, int seed)
    {
        var rnd = new System.Random(seed);
        int ci = rnd.Next(KoloryZaslon.Length); var m = Plain("zasłony mieszkań " + ci, KoloryZaslon[ci], .12f);
        var up = Vector3.up;
        foreach (int side in new[] { 0, 1 })
        {
            float f = .17f + (float)rnd.NextDouble() * .13f, u0 = side == 0 ? 0f : w - w * f, u1 = side == 0 ? w * f : w;
            const int pasy = 5;
            for (int i = 0; i < pasy; i++)
            {
                float a = Mathf.Lerp(u0, u1, i / (float)pasy), b = Mathf.Lerp(u0, u1, (i + 1) / (float)pasy);
                float da = i % 2 == 0 ? .006f : .024f, db = i % 2 == 0 ? .024f : .006f;
                Vector3 q0 = lewyDol + t * a + n * da, q1 = lewyDol + t * b + n * db;
                var fn = Vector3.Cross(up, q1 - q0).normalized; if (Vector3.Dot(fn, n) < 0) fn = -fn;
                QN(m, q0, q1, q1 + up * h, q0 + up * h, fn);
            }
        }
        if (rnd.NextDouble() < .55) OBoxN(m, lewyDol + t * (w * .5f) + up * (h - .09f) + n * .02f, t, up, n, w, .18f, .02f);   // lambrekin
    }
    // Opaska wokół okna (tynk jaśniejszy, 3.5 cm przed licem): boki i nadproże; parapet zostaje z generatora domu.
    static void Opaska(Vector3 c, Vector3 n, float w, float h, Material m, float s = .11f)
    {
        var t = Prawo(n); var up = Vector3.up;
        foreach (int i in new[] { -1, 1 }) OBoxN(m, c + t * i * (w * .5f + s * .5f) + n * .0175f, t, up, n, s, h + .02f, .035f);
        OBoxN(m, c + up * (h * .5f + s * .65f) + n * .02f, t, up, n, w + 2f * s + .06f, s * 1.3f, .04f);
        elOkna++;
    }

    // ---------------- markiza w paski ----------------
    // pa → pb: odcinek na licu ściany (wzdłuż elewacji), n — normalna lica; płótno od yGora przy ścianie ze spadkiem
    // 30° na wysięg; lambrekin 0.22 m; paski ~0.25 m na zmianę m1 / m2; kaseta, listwa przednia, dwa ramiona łamane.
    static void Markiza(Vector3 pa, Vector3 pb, Vector3 n, float yGora, float wysieg, Material m1, Material m2, Material kaseta)
    {
        var up = Vector3.up; var t = pb - pa; t.y = 0; float len = t.magnitude; if (len < .4f) return; t /= len;
        var baza = new Vector3(pa.x, 0, pa.z);
        Vector3 W(float u, float y, float off) => baza + t * u + up * y + n * off;
        const float off0 = .16f; float yF = yGora - wysieg * Mathf.Tan(30f * Mathf.Deg2Rad);
        OBoxN(kaseta, W(len * .5f, yGora + .06f, .1f), t, up, n, len + .06f, .16f, .18f);
        var sl = (n * wysieg - up * (yGora - yF)).normalized; var sn = Vector3.Cross(t, sl).normalized; if (sn.y < 0) sn = -sn;
        int ns = Mathf.Max(2, Mathf.RoundToInt(len / .25f)); float sw = len / ns;
        for (int i = 0; i < ns; i++)
        {
            var m = i % 2 == 0 ? m1 : m2; float u0 = i * sw, u1 = u0 + sw;
            Vector3 a = W(u0, yGora, off0), b = W(u1, yGora, off0), c = W(u1, yF, off0 + wysieg), d = W(u0, yF, off0 + wysieg);
            QN(m, a, b, c, d, sn); QN(m, a, b, c, d, -sn);
            Vector3 e = W(u1, yF - .22f, off0 + wysieg), f = W(u0, yF - .22f, off0 + wysieg);
            QN(m, d, c, e, f, n); QN(m, d, c, e, f, -n);
        }
        OBoxN(kaseta, W(len * .5f, yF - .01f, off0 + wysieg), t, up, n, len + .04f, .05f, .05f);
        foreach (float u in new[] { .3f, len - .3f }) Tube(MatAlu, W(u, yGora - .55f, .03f), W(u, yF + .03f, off0 + wysieg - .06f), .02f, 8);
        foreach (float u in new[] { .3f, len - .3f }) OBoxN(MatAlu, W(u, yGora - .55f, .015f), t, up, n, .1f, .14f, .03f);
        nadGlowa.Add(("markiza", new Bounds(W(len * .5f, (yGora + yF) * .5f - .1f, off0 + wysieg * .5f), new Vector3(Mathf.Abs(t.x) * len + Mathf.Abs(n.x) * wysieg + .2f, yGora - yF + .5f, Mathf.Abs(t.z) * len + Mathf.Abs(n.z) * wysieg + .2f))));
        elMarkizy++;
    }

    // ================= PIERZEJA SKLEPÓW B =================
    // Wywoływane z ShopRow dla każdego lokalu (zamiast stalowej „markizy” 3.62 m i płaskiej tablicy na niej — zdjęcie 22:35:
    // „szyldy na tablicach przykręconych do czarnej ramy”). Lico witryn z = zf, normalna −z (ulica). Na lokal: dwie markizy
    // w paski nad szybami (kolory lokalu), szklany daszek nad drzwiami, litery przestrzenne 0.42 m na nadprożu (3.62–4.04 m,
    // nad kasetą markiz 3.56 m — nic ich nie zasłania), kaseton na wysięgniku przy pilastrze (dwustronny), kinkiety na
    // pilastrach, donice przy drzwiach.
    static void LokalPierzei(int k, string kind, float a, float b, float d0, float zf, float x0, float PY)
    {
        var g0 = group; var n = Vector3.back; string bud = "B pierzeja";
        Grupa("Elewacje / " + bud, 140f);
        Material Pas(string nazwa, Color c) => Plain("markiza płótno " + nazwa, c, .1f);
        var bialePas = Pas("biel", new Color(.94f, .93f, .90f));
        string tekst, blade; Material lico, boki, p1, p2, kas, bladeLico, bladeL, bladeB; float cap = .42f; Color pas;
        switch (kind)
        {
            case "butik": tekst = "NYXA"; cap = .46f; pas = new Color(.05f, .05f, .055f); lico = MatZloto; boki = MatBokiCzarne; p1 = Pas("czerń", new Color(.07f, .07f, .08f)); p2 = Pas("krem", new Color(.90f, .86f, .76f));
                kas = MatBokiCzarne; blade = "NYXA"; bladeLico = MatKaseton("czarny butik", new Color(.06f, .06f, .065f)); bladeL = MatZloto; bladeB = MatZloto; break;
            case "shake": tekst = "SHAKE BAR"; pas = new Color(.05f, .22f, .20f); lico = MatLicoBiale; boki = MatBoki("miętowe", new Color(.24f, .62f, .52f)); p1 = Pas("mięta", new Color(.36f, .74f, .62f)); p2 = bialePas;
                kas = MatAlu; blade = "SHAKE"; bladeLico = MatKaseton("miętowy", new Color(.30f, .70f, .58f)); bladeL = MatLicoBiale; bladeB = MatBokiStal; break;
            case "nutrition": tekst = "BRAXHORN NUTRITION"; cap = .4f; pas = new Color(.05f, .05f, .055f); lico = MatLicoBiale; boki = MatBokiCzarne; p1 = Pas("czerń", new Color(.07f, .07f, .08f)); p2 = Pas("zieleń BRAXHORN", new Color(.34f, .72f, .17f));
                kas = MatBokiCzarne; blade = null; bladeLico = MatKaseton("czarny BRAXHORN", new Color(.05f, .05f, .055f)); bladeL = MatLicoBiale; bladeB = MatBokiCzarne; break;
            case "kawiarnia": tekst = "KAWIARNIA"; pas = new Color(.17f, .10f, .06f); lico = MatLico("kremowe", new Color(.95f, .90f, .78f)); boki = MatBoki("brązowe", new Color(.32f, .19f, .11f)); p1 = Pas("brąz kawowy", new Color(.36f, .22f, .13f)); p2 = Pas("krem", new Color(.90f, .86f, .76f));
                kas = MatBoki("brązowe", new Color(.32f, .19f, .11f)); blade = "KAWA"; bladeLico = MatKaseton("kremowy kawiarni", new Color(.93f, .88f, .76f)); bladeL = MatLico("brązowe", new Color(.32f, .19f, .11f)); bladeB = MatBoki("brązowe", new Color(.32f, .19f, .11f)); break;
            default: tekst = "MARKET 24"; pas = new Color(.40f, .04f, .05f); lico = MatLicoBiale; boki = MatBoki("czerwone", new Color(.72f, .06f, .08f)); p1 = Pas("czerwień", new Color(.74f, .08f, .09f)); p2 = bialePas;
                kas = MatAlu; blade = "24H"; bladeLico = MatKaseton("czerwony market", new Color(.74f, .08f, .09f)); bladeL = MatLicoBiale; bladeB = MatBokiStal; break;
        }
        // markizy nad szybami (prześwit pod lambrekinem ≥ 2.3 m × K) i szklany daszek nad drzwiami
        float yG = 3.42f, wys = Mathf.Clamp((yG - .22f - Mathf.Max(2.3f * K, 2.3f) - .12f) / Mathf.Tan(30f * Mathf.Deg2Rad), .6f, 1.3f);
        Markiza(new Vector3(a + .64f, 0, zf), new Vector3(d0 - .12f, 0, zf), n, yG, wys, p1, p2, kas);
        Markiza(new Vector3(d0 + 1.92f, 0, zf), new Vector3(b - .64f, 0, zf), n, yG, wys, p1, p2, kas);
        group = g0;
        DaszekSzklany(bud, new Vector3(d0 + .9f, 3.3f, zf), n, 2.5f, 1.0f, 1.8f, .45f);
        // pas szyldowy (fascia: lakierowana płyta w kolorze lokalu nad witryną, 3.58–4.07 m, przed wieńcem) i litery na nim
        float cx = (a + b) * .5f;
        var gs = group; Grupa("Szyldy / " + bud + " / " + tekst, 170f, "Szyldy / " + bud);
        BoxN(Plain("pas szyldowy lokalu " + kind, pas, .55f, .2f), a + .6f, b - .6f, 3.58f, 4.07f, zf - .075f, zf - .005f);
        group = gs;
        SzyldLitery(bud, tekst, tekst, new Vector3(cx, 3.63f, zf - .075f), n, cap, lico, boki, .07f, .015f, kind == "nutrition" ? MatLico("zielone BRAXHORN", new Color(.36f, .78f, .18f)) : null, .17f);
        // kaseton na wysięgniku przy pilastrze lewej krawędzi lokalu (lico pilastra z = zf − 0.1)
        float xb = k == 0 ? x0 + .3f : a;
        if (blade != null)
        {
            float bc = .22f, H = WysokoscPionowego(blade, bc, bc * .3f) + .3f;
            Wysiegnik(bud, tekst + " (wysięgnik)", new Vector3(xb, 3.68f + H * .5f, zf - .1f), n, .08f, .5f, H, .14f, bladeLico, MatAlu, blade, bc, true, bladeL, bladeB);
        }
        else Wysiegnik(bud, tekst + " (wysięgnik z logo)", new Vector3(xb, 3.68f + .33f, zf - .1f), n, .08f, .66f, .66f, .14f, bladeLico, MatAlu, null, 0f, false, bladeL, bladeB, SignMat("byk_braxhorn"));
        // donice przy drzwiach (na placyku), kinkiet na pilastrze lewej krawędzi
        GrupaDrobna(bud, new Vector3(cx, 0, zf));
        Planter(new Vector3(d0 - .5f, PY, zf - .42f), .55f, .5f, 3100 + k, 0, 3);
        Planter(new Vector3(d0 + 2.3f, PY, zf - .42f), .55f, .5f, 3200 + k, 2, 1);
        Kinkiet(new Vector3(xb, 2.55f, zf - .1f), n);
        if (k == 0) NumerDomu(bud, "1", new Vector3(x0 + .3f, 2.95f, zf - .1f), n, .24f);
        group = g0;
    }

    // Całość pierzei (po lokalach): pilastry z bazą i głowicą, gzyms nad parterem, gzyms z konsolami pod attyką, okna
    // mieszkań (ościeżnice, słupek, kamienne parapety i nadproża, zasłony), okna szczytów, tył (daszki nad drzwiami
    // służbowymi, kinkiety, kamery, skrzynki, kratki, rury spustowe), szczyty (pilastry, kinkiety), schody pożarowe przy
    // oknie mieszkania nad marketem.
    static void ElewacjaPierzei(float x0, float x1, float zf, float zb, float H0, float H1, int lokale)
    {
        var g0 = group; string bud = "B pierzeja"; Grupa("Elewacje / " + bud, 140f);
        float unit = (x1 - x0) / lokale;
        var front = Vector3.back; var tyl = Vector3.forward;
        // pilastry: przy narożnikach i na granicach lokali
        var px = new List<(float x, float half)> { (x0 + .3f, .3f), (x1 - .3f, .3f) };
        for (int k = 1; k < lokale; k++) px.Add((x0 + k * unit, .35f));
        foreach (var (x, hw) in px)
        {
            BoxN(Plinth, x - hw - .05f, x + hw + .05f, 0f, .34f, zf - .15f, zf);
            BoxN(Stone, x - hw, x + hw, .34f, 3.48f, zf - .1f, zf);
            BoxN(Stone, x - hw - .06f, x + hw + .06f, 3.48f, 3.6f, zf - .15f, zf);
            elDetale++;
        }
        // gzyms nad parterem (przed wieńcem, nad literami) i jego powrót na szczytach
        BoxN(Stone, x0 - .1f, x1 + .1f, 4.1f, 4.22f, zf - .16f, zf - .05f);
        BoxN(Stone, x0 - .16f, x0 - .05f, 4.1f, 4.22f, zf - .16f, zb + .05f); BoxN(Stone, x1 + .05f, x1 + .16f, 4.1f, 4.22f, zf - .16f, zb + .05f);
        BoxN(Stone, x0 - .1f, x1 + .1f, 4.1f, 4.22f, zb + .05f, zb + .16f);
        elGzymsy++;
        // gzyms pod attyką z konsolami (lico piętra z = zf + 0.8, attyka od H1)
        BoxN(Stone, x0 + .12f, x1 - .12f, H1 - .22f, H1 - .02f, zf + .62f, zf + .8f);
        for (float x = x0 + .5f; x < x1 - .4f; x += .6f) BoxN(Stone, x - .06f, x + .06f, H1 - .32f, H1 - .22f, zf + .68f, zf + .8f, false);
        BoxN(Stone, x0 + .12f, x0 + .3f, H1 - .22f, H1 - .02f, zf + .62f, zb - .3f); BoxN(Stone, x1 - .3f, x1 - .12f, H1 - .22f, H1 - .02f, zf + .62f, zb - .3f);
        BoxN(Stone, x0 + .12f, x1 - .12f, H1 - .22f, H1 - .02f, zb - .3f, zb - .12f);
        elGzymsy++;
        // okna mieszkań od frontu (te same pozycje co w ShopRow: x = a + 1.4 + 3.2 j; szkło z = zf + 0.77…0.83, y H0 + 0.6…2.9)
        int seed = 7100;
        for (int k = 0; k < lokale; k++)
        {
            float a = x0 + k * unit, b = a + unit;
            for (float x = a + 1.4f; x < b - 1.2f && x + 2.06f < x1 - .3f; x += 3.2f)
            {
                float y0 = H0 + .6f, y1 = H0 + 2.9f;
                BoxN(Frame, x - .07f, x, y0 - .05f, y1 + .07f, zf + .72f, zf + .8f); BoxN(Frame, x + 2f, x + 2.07f, y0 - .05f, y1 + .07f, zf + .72f, zf + .8f);
                BoxN(Frame, x - .07f, x + 2.07f, y0 - .07f, y0, zf + .72f, zf + .8f);
                BoxN(Frame, x + .97f, x + 1.03f, y0, y1, zf + .74f, zf + .77f, false);                       // słupek
                BoxN(Frame, x, x + 2f, y1 - .62f, y1 - .57f, zf + .74f, zf + .77f, false);                   // ślemię (naświetle)
                BoxN(Stone, x - .14f, x + 2.14f, y0 - .13f, y0 - .07f, zf + .64f, zf + .8f);                 // parapet kamienny
                BoxN(Stone, x - .16f, x + 2.16f, y1 + .07f, y1 + .17f, zf + .72f, zf + .8f, false);          // nadproże
                var gd = group; GrupaDrobna(bud, new Vector3(x, 0, zf));
                Zaslony(new Vector3(x, y0, zf + .77f), Vector3.right, front, 2f, y1 - y0 - .62f, seed++);
                group = gd; elOkna++;
            }
        }
        // okna szczytów (ShopRow: x0 + 0.25…0.28 i x1 − 0.28…0.25, z = zf + 2.6 + 4 j, y H0 + 0.6…2.8)
        foreach (float sx in new[] { x0, x1 })
        {
            float o = sx == x0 ? -1f : 1f; var nn = new Vector3(o, 0, 0); float face = sx + o * (-.3f + .02f) + o * .03f;
            for (float z = zf + 2.5f; z < zb - 2f; z += 4f)
            {
                BoxN(Stone, Mathf.Min(face, face + o * .14f), Mathf.Max(face, face + o * .14f), H0 + .44f, H0 + .5f, z, z + 1.6f);
                var gd = group; GrupaDrobna(bud, new Vector3(sx, 0, z));
                Zaslony(new Vector3(face + o * .002f, H0 + .6f, o < 0 ? z + 1.5f : z + .1f), o < 0 ? Vector3.back : Vector3.forward, nn, 1.4f, 2.2f, seed++);
                group = gd; elOkna++;
            }
        }
        group = g0;
        // tył (od parku): daszki nad drzwiami służbowymi, kinkiety, kamery, skrzynki, kratki, rury spustowe
        for (int k = 0; k < lokale; k++)
        {
            float a = x0 + k * unit, b = a + unit;
            DaszekSzklany(bud, new Vector3(b - 2.15f, 3.05f, zb), tyl, 1.9f, .95f, 1.5f, .45f);
            GrupaDrobna(bud, new Vector3(a + 6f, 0, zb));
            Kinkiet(new Vector3(b - 1.2f, 2.35f, zb), tyl);
            Kamera(new Vector3(a + .5f, 3.7f, zb), tyl, 35f);
            bool ostatni = k == lokale - 1;   // przy ostatnim lokalu biegną schody pożarowe (x a+3.1…a+7.8)
            if (k % 2 == 0) Skrzynka(new Vector3(ostatni ? b - .65f : a + 4.7f, .32f, zb), tyl);
            Kratka(new Vector3(ostatni ? a + 2.4f : a + 6.6f, 3.25f, zb), tyl);
            if (k > 0)
            {
                // rura z attyki: po licu piętra (z = zb − 0.3) do gzymsu parteru, odsadzka, po licu parteru do ziemi
                Cylinder(MatRynna, new Vector3(a, H0 + .3f, zb - .22f), .05f, H1 - H0 - .78f, 10, false, false);
                OBoxN(MatRynna, new Vector3(a, H1 - .36f, zb - .22f), Vector3.right, Vector3.up, Vector3.forward, .2f, .24f, .16f);   // lej pod gzymsem (wylot z attyki)
                Tube(MatRynna, new Vector3(a, H0 + .32f, zb - .22f), new Vector3(a, H0 + .02f, zb + .08f), .05f, 10);
                RuraSpustowa(new Vector3(a, 0, zb), tyl, H0 + .06f);
            }
        }
        // szczyty: pilastry, kinkiety, kamera, numer od ulicy N–S
        foreach (float sx in new[] { x0, x1 })
        {
            float o = sx == x0 ? -1f : 1f; var nn = new Vector3(o, 0, 0);
            Grupa("Elewacje / " + bud, 140f);
            foreach (float z in new[] { zf + 3.6f, zf + 7.2f, zf + 10.8f })
            {
                BoxN(Plinth, Mathf.Min(sx, sx + o * .15f), Mathf.Max(sx, sx + o * .15f), 0f, .34f, z - .4f, z + .4f);
                BoxN(Stone, Mathf.Min(sx, sx + o * .1f), Mathf.Max(sx, sx + o * .1f), .34f, 3.48f, z - .33f, z + .33f);
                BoxN(Stone, Mathf.Min(sx, sx + o * .15f), Mathf.Max(sx, sx + o * .15f), 3.48f, 3.6f, z - .4f, z + .4f);
            }
            GrupaDrobna(bud, new Vector3(sx, 0, zf + 7f));
            foreach (float z in new[] { zf + 3.6f, zf + 10.8f }) Kinkiet(new Vector3(sx + o * .1f, 2.55f, z), nn);
            Kamera(new Vector3(sx, 3.75f, zb - .6f), nn, o < 0 ? -40f : 40f);
            RuraSpustowa(new Vector3(sx, 0, zf + 5.4f), nn, H0 + .06f);
        }
        group = g0;
        // schody pożarowe (stalowe, jak w kamienicach ze zdjęcia-wzorca): spocznik przed oknem mieszkania nad marketem,
        // bieg w dół wzdłuż ściany (42°), poręcze; kolizje: rampa biegu, spocznik i balustrady (można wejść)
        SchodyPozarowe(x0 + (lokale - 1) * unit + 7.8f, zb, H0);
        elBudynki.Add(bud);
        group = g0;
    }

    static void SchodyPozarowe(float xL, float zb, float H0)
    {
        var g0 = group; Grupa("Elewacje / B pierzeja — schody pożarowe", 140f, "Elewacje / B pierzeja");
        var st = MatGrafit; float y = H0 + .05f, xA = xL, xB = xL + 2.1f, z0 = zb, z1 = zb + 1.25f;
        BoxN(st, xA, xB, y - .06f, y, z0, z1);                                                              // krata spocznika
        for (float x = xA + .05f; x < xB; x += .1f) BoxN(MatBokiCzarne, x - .005f, x + .005f, y - .065f, y - .06f, z0, z1, false);
        foreach (float x in new[] { xA + .05f, xB - .05f }) Tube(st, new Vector3(x, y - .06f, z1 - .05f), new Vector3(x, y - .9f, z0 + .02f), .025f, 6);   // zastrzały
        void Porecz(Vector3 a, Vector3 b)
        {
            Tube(st, a + Vector3.up * 1.05f, b + Vector3.up * 1.05f, .022f, 6); Tube(st, a + Vector3.up * .55f, b + Vector3.up * .55f, .014f, 6);
            float L = (b - a).magnitude; int n = Mathf.Max(1, Mathf.RoundToInt(L / .12f));
            for (int i = 0; i <= n; i++) { var p = Vector3.Lerp(a, b, i / (float)n); Tube(st, p, p + Vector3.up * 1.05f, i == 0 || i == n ? .022f : .008f, 6); }
        }
        Porecz(new Vector3(xA, y, z1), new Vector3(xB, y, z1)); Porecz(new Vector3(xB, y, z0 + .05f), new Vector3(xB, y, z1));
        // bieg: od spocznika (xA) w dół ku −x, szerokość 0.9 m przy ścianie
        float run = y / Mathf.Tan(42f * Mathf.Deg2Rad), sx0 = xA - run; int stopnie = Mathf.CeilToInt(y / .19f);
        for (int i = 1; i < stopnie; i++)
        {
            float yy = y * i / stopnie, xx = sx0 + run * i / stopnie;
            BoxN(st, xx - .14f, xx + .14f, yy - .03f, yy, z0 + .08f, z0 + .98f, false);
        }
        foreach (float z in new[] { z0 + .06f, z0 + 1.0f })
        {
            var a = new Vector3(sx0, 0f, z); var b = new Vector3(xA, y - .05f, z);
            var d = (b - a).normalized; var bok = Vector3.forward; var gora = Vector3.Cross(bok, d).normalized; if (gora.y < 0) gora = -gora;
            OBoxN(st, (a + b) * .5f, d, gora, bok, (b - a).magnitude, .18f, .02f);                         // policzki
        }
        Porecz(new Vector3(sx0, 0f, z0 + 1.02f), new Vector3(xA, y, z0 + 1.02f));
        // kolizje: rampa biegu, spocznik, balustrady
        var ramp = new GameObject("kolizja schodów pożarowych"); ramp.transform.SetParent(colliders, false);
        var rd = new Vector3(run, y, 0f); ramp.transform.position = new Vector3(sx0 + run * .5f, y * .5f - .06f, z0 + .53f);
        ramp.transform.rotation = Quaternion.FromToRotation(Vector3.right, rd.normalized);
        ramp.AddComponent<BoxCollider>().size = new Vector3(rd.magnitude, .12f, .92f);
        Collider(new Vector3((xA + xB) * .5f, y - .03f, (z0 + z1) * .5f), new Vector3(xB - xA, .06f, z1 - z0));
        Collider(new Vector3((xA + xB) * .5f, y + .55f, z1), new Vector3(xB - xA, 1.1f, .06f)); Collider(new Vector3(xB, y + .55f, (z0 + z1) * .5f), new Vector3(.06f, 1.1f, z1 - z0));
        Hard(sx0 - .2f, xA, z0, z0 + 1.1f);
        elDetale++; group = g0;
    }

    // ================= OSIEDLE A: boki i tyły budynków publicznych, siłownia =================
    // Budynki Codexa 40 × 30 m (lica: tył z = cz − 15.215, boki x = cx ± 20.215, okładzina CivicFacades); okna boków
    // co 5 m (z = cz − 12.5…12.5, ±1.3), tyłu x = cx ± 5.5 / 11 / 16.5 (±1.8), wyjście tylne x cx ± 2 do 3.1 m.
    static void ElewacjeBudynkowA()
    {
        var g0 = group;
        var list = new (string id, float cx, float cz, float h, bool tyl)[]
        {
            ("komisariat", -86f, -42f, 4.5f, true), ("szpital", -86f, 14f, 4.5f, true), ("arena", -86f, 64f, 7f, true),
            ("dyskoteka", 86f, 14f, 7f, true), ("sala bokserska", 86f, 64f, 7f, true), ("kasyno", 86f, -42f, 7f, false),
        };
        foreach (var (id, cx, cz, h, tyl) in list)
        {
            string bud = "A " + id; Grupa("Elewacje / " + bud, 140f);
            float zt = cz - 15.215f, zfr = cz + 15.215f, xl = cx - 20.215f, xr = cx + 20.215f;
            // cokół 0.45 m (kamień łamany) — boki, tył (z przerwą na wyjście), front (bez kasyna; przerwa na wejście)
            BoxN(Plinth, xl - .05f, xl, 0f, .45f, zt - .05f, zfr + .05f); BoxN(Plinth, xr, xr + .05f, 0f, .45f, zt - .05f, zfr + .05f);
            if (tyl) { BoxN(Plinth, xl - .05f, cx - 2.05f, 0f, .45f, zt - .05f, zt); BoxN(Plinth, cx + 2.05f, xr + .05f, 0f, .45f, zt - .05f, zt); }
            if (id != "kasyno") { BoxN(Plinth, xl - .05f, cx - 2.05f, 0f, .35f, zfr, zfr + .05f); BoxN(Plinth, cx + 2.05f, xr + .05f, 0f, .35f, zfr, zfr + .05f); }
            // gzyms międzykondygnacyjny (budynki 7 m: między rzędami okien 2.6 / 4.1 m) na bokach i z tyłu
            if (h > 5f)
            {
                BoxN(Stone, xl - .1f, xl, 3.62f, 3.76f, zt - .1f, zfr); BoxN(Stone, xr, xr + .1f, 3.62f, 3.76f, zt - .1f, zfr);
                if (tyl) BoxN(Stone, xl - .1f, xr + .1f, 3.62f, 3.76f, zt - .1f, zt);
                elGzymsy++;
            }
            elGzymsy++;   // gzym koronujący (CivicFacades) liczony z budynkiem
            if (tyl) DaszekSzklany(bud, new Vector3(cx, 3.32f, zt), Vector3.back, 5.4f, 1.3f, 4.9f, .6f);
            // drobne: rury, kinkiety, kamery, skrzynki, kratki
            float yKam = h > 5f ? 6.4f : 3.55f, yKr = h > 5f ? 3.2f : 3.3f;
            GrupaDrobna(bud, new Vector3(cx, 0, zt));
            if (tyl)
            {
                foreach (float s in new[] { -1f, 1f }) Kinkiet(new Vector3(cx + s * 2.45f, 2.15f, zt), Vector3.back);
                Skrzynka(new Vector3(cx + 8.25f, .35f, zt), Vector3.back);
                Kamera(new Vector3(cx - 8.25f, yKam, zt), Vector3.back, -30f);
                foreach (float s in new[] { -1f, 1f }) Kratka(new Vector3(cx + s * 13.75f, yKr, zt), Vector3.back);
                foreach (float s in new[] { -1f, 1f }) RuraSpustowa(new Vector3(cx + s * 19.75f, 0, zt), Vector3.back, h);
            }
            foreach (float s in new[] { -1f, 1f })
            {
                float xs = s < 0 ? xl : xr; var nn = new Vector3(s, 0, 0);
                GrupaDrobna(bud, new Vector3(xs, 0, cz));
                RuraSpustowa(new Vector3(xs, 0, cz), nn, h);
                RuraSpustowa(new Vector3(xs, 0, cz + 14.55f), nn, h);
                foreach (float dz in new[] { -5f, 5f }) Kratka(new Vector3(xs, yKr, cz + dz), nn);
                foreach (float dz in new[] { -10f, 10f }) Kinkiet(new Vector3(xs, 3.08f, cz + dz), nn);
                Kamera(new Vector3(xs, yKam, cz + 14.3f), nn, s * 40f);
                if (s > 0) Skrzynka(new Vector3(xs, .35f, cz - 10f), nn);
            }
            elBudynki.Add(bud);
        }
        // siłownia A (GymA: gymHall = wnętrze hali x hx0…hx1, z hzP…hzS, ściany 0.3 m; front z drzwiami od północy, tył od parku)
        if (gymHall.size.x > 1f)
        {
            string bud = "A siłownia"; Grupa("Elewacje / " + bud, 140f);
            float xw = gymHall.min.x - .3f, zF = gymHall.max.z + .3f, zP = gymHall.min.z - .3f, H = gymHall.max.y, xe = gymHall.max.x + .3f;
            BoxN(Plinth, xw - .05f, xw, 0f, .5f, zP - .05f, zF + .05f); BoxN(Plinth, xw - .05f, xe, 0f, .5f, zP - .05f, zP);
            BoxN(Stone, xw - .1f, xw, 4.9f, 5.04f, zP - .1f, zF); BoxN(Stone, xw - .1f, xe, 4.9f, 5.04f, zP - .1f, zP);
            elGzymsy += 2;
            float zm = (zF + zP) * .5f;
            GrupaDrobna(bud, new Vector3(xw, 0, zm));
            foreach (float z in new[] { zF - .4f, zm, zP + .4f }) RuraSpustowa(new Vector3(xw, 0, z), Vector3.left, H);
            foreach (float dz in new[] { -10.5f, -3.5f, 3.5f, 10.5f }) Kratka(new Vector3(xw, 3.4f, zm + dz), Vector3.left, .6f, .4f);
            Kamera(new Vector3(xw, 4.2f, zF - .6f), Vector3.left, -40f);
            Kinkiet(new Vector3(xw, 3.0f, zm + 7f), Vector3.left); Kinkiet(new Vector3(xw, 3.0f, zm - 7f), Vector3.left);
            GrupaDrobna(bud, new Vector3((xw + xe) * .5f, 0, zP));
            foreach (float x in new[] { xw + .6f, (xw + xe) * .5f, xe - .6f }) RuraSpustowa(new Vector3(x, 0, zP), Vector3.back, H);
            elBudynki.Add(bud);
        }
        group = g0;
    }
    // przy wejściu do siłowni A (GymA): kinkiety po bokach drzwi (otwór dX ± 0.9 m do 3.1 m)
    static void WejscieSilowni(float dX, float fz1)
    {
        var g0 = group; GrupaDrobna("A siłownia", new Vector3(dX, 0, fz1));
        foreach (float s in new[] { -1f, 1f }) Kinkiet(new Vector3(dX + s * 1.35f, 2.35f, fz1), Vector3.forward);
        group = g0;
    }

    // ================= WILLE (hak na końcu Villa) =================
    // Kinkiet przy drzwiach, numer domu z cyfr przestrzennych (osiedle B: 1…10 po kolei działek), donica przy schodku,
    // dwie lampy przy bramie garażu. Pw — przeliczenie (wzdłuż ulicy, wysokość, głębokość) → świat; −Wax = normalna frontu.
    static void ElewacjaWilli(int index, Func<float, float, float, Vector3> Pw, Vector3 Wax, float door0, float door1, float f0,
                              bool garageOn, float g0, float g1, string label)
    {
        var gr0 = group; string bud = label ?? ("B willa " + (index + 1)); var n = -Wax;
        GrupaDrobna(bud, Pw(door0, 0, f0));
        Kinkiet(Pw(door0 - .36f, 2.05f, f0), n);
        if (label == null) NumerDomu(bud, (index + 1).ToString(), Pw(door1 + .52f, 2.15f, f0), n, .2f, MatBokiCzarne);
        GrupaDrobna(bud, Pw(door0, 0, f0));
        var pd = Pw(door0 - .95f, 0f, f0 - .42f);
        Planter(new Vector3(pd.x, 0f, pd.z), .5f, .5f, 5200 + index, 1, 3);
        if (garageOn) foreach (float a in new[] { g0 + .36f, g1 - .36f }) Kinkiet(Pw(a, 2.3f, f0), n);
        elBudynki.Add(bud);
        group = gr0;
    }

    // ================= DOM RODZINY WAWERÓW (hak na końcu WawerHouse) =================
    // Bryła i działka bez zmian. Opaski okien (jasny tynk), latarenki przy drzwiach i z tyłu, balkon od ogrodu (płyta na
    // konsolach, balustrada z prętów jak przęsła płotu, drzwi balkonowe w ramie orzechowej). Szyby: „pokój za szybą”
    // (WawerHouse — materiał Glass zamiast ciemnej tafli; zdjęcie 23:01: czarne prostokąty okien).
    static void ElewacjaWawer()
    {
        var g0 = group; string bud = "A dom Wawerów"; Grupa("Elewacje / " + bud, 140f);
        const float x0 = -35.5f, x1 = -25.5f, z1 = 66f;
        var opaska = Pbr("opaski okien tynk jasny (dom Wawerów)", "white_stucco_02", 2.0f, new Color(.98f, .95f, .90f));
        var R = Vector3.right; var L = Vector3.left; var F = Vector3.forward;
        var okna = new List<(Vector3 c, Vector3 n, float w, float h)>
        {
            (new Vector3(x1, 1.75f, 53.6f), R, 1.2f, 1.4f), (new Vector3(x1, 1.75f, 56.2f), R, 1.2f, 1.4f), (new Vector3(x1, 1.75f, 58.8f), R, 1.2f, 1.4f),
            (new Vector3(x1, 4.95f, 64.8f), R, .8f, .62f),   // okno 64.6 (markiza i okiennica) bez opaski
            (new Vector3(x0, 1.75f, 54.5f), L, 1.2f, 1.4f), (new Vector3(x0, 1.75f, 58.8f), L, 1.2f, 1.4f), (new Vector3(x0, 1.75f, 63.0f), L, 1.2f, 1.4f),
            (new Vector3(x0, 4.95f, 56.0f), L, .8f, .62f), (new Vector3(x0, 4.95f, 62.0f), L, .8f, .62f),
            (new Vector3(-32.8f, 1.75f, z1), F, 1.1f, 1.3f), (new Vector3(-28.2f, 1.75f, z1), F, 1.1f, 1.3f), (new Vector3(-30.5f, 4.95f, z1), F, .8f, .62f),
        };
        foreach (var (c, n, w, h) in okna) Opaska(c, n, w + .14f, h + .14f, opaska);
        // balkon od ogrodu (ściana x0): płyta 3.0 × 1.25 m na 3.25–3.4 m, konsole, balustrada 1.0 m, drzwi balkonowe
        var bars = Plain("przęsła płotu czarne", new Color(.04f, .04f, .045f), .5f, .6f);
        var brown = Plain("okleina orzech (okna, drzwi, rynny)", new Color(.26f, .15f, .08f), .45f);
        const float bz0 = 57.6f, bz1 = 60.6f, by = 3.4f, bx = x0 - 1.25f;
        BoxN(Concrete, bx, x0, by - .15f, by, bz0, bz1);
        BoxN(Stone, bx - .03f, x0, by - .02f, by + .02f, bz0 - .03f, bz1 + .03f, false);
        foreach (float z in new[] { bz0 + .25f, bz1 - .25f })
        {
            BoxN(bars, x0 - .06f, x0, by - .75f, by - .15f, z - .05f, z + .05f, false);
            Tube(bars, new Vector3(x0 - .03f, by - .7f, z), new Vector3(bx + .15f, by - .16f, z), .03f, 6);
        }
        BoxN(bars, bx, bx + .04f, by + .96f, by + 1.0f, bz0, bz1, false);
        foreach (float zz in new[] { bz0, bz1 }) BoxN(bars, bx, x0, by + .96f, by + 1.0f, zz - .02f, zz + .02f, false);
        for (float z = bz0 + .06f; z < bz1 - .03f; z += .12f) BoxN(bars, bx + .008f, bx + .032f, by, by + .96f, z - .012f, z + .012f, false);
        foreach (float zz in new[] { bz0, bz1 }) for (float x = bx + .1f; x < x0 - .05f; x += .12f) BoxN(bars, x - .012f, x + .012f, by, by + .96f, zz - .012f, zz + .012f, false);
        // drzwi balkonowe: rama orzech, szyba „pokój za szybą”, próg
        const float dz0 = 58.6f, dz1 = 59.6f, dy0 = by + .02f, dy1 = 5.3f;
        BoxN(brown, x0 - .05f, x0, dy0, dy1 + .07f, dz0 - .07f, dz0); BoxN(brown, x0 - .05f, x0, dy0, dy1 + .07f, dz1, dz1 + .07f);
        BoxN(brown, x0 - .05f, x0, dy1, dy1 + .07f, dz0, dz1); BoxN(brown, x0 - .05f, x0, dy0, dy0 + .08f, dz0, dz1);
        BoxN(Glass, x0 - .03f, x0 - .01f, dy0 + .08f, dy1, dz0, dz1, false);
        BoxN(brown, x0 - .045f, x0 - .03f, dy0 + .08f, dy1, (dz0 + dz1) * .5f - .03f, (dz0 + dz1) * .5f + .03f, false);
        Opaska(new Vector3(x0, (dy0 + dy1) * .5f + .03f, (dz0 + dz1) * .5f), L, dz1 - dz0 + .14f, dy1 - dy0 + .07f, opaska);
        nadGlowa.Add(("balkon domu Wawerów", new Bounds(new Vector3(x0 - .62f, by + .3f, (bz0 + bz1) * .5f), new Vector3(1.3f, 1.4f, bz1 - bz0 + .1f))));
        // latarenki: przy drzwiach (pod daszkiem ganku, nad tabliczką) i z tyłu pod balkonem
        GrupaDrobna(bud, new Vector3(x1, 0, 60.5f));
        Latarenka(new Vector3(x1 + .02f, 2.42f, 60.45f), R);
        Latarenka(new Vector3(x0 - .02f, 2.45f, 59.1f), L);
        elBudynki.Add(bud);
        group = g0;
    }
}
