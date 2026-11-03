using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// BRAXHORN NUTRITION OD NOWA (wykonawca A, 2.11). Zgłoszenia: z35 „miał być mural na ścianie — zobacz, co się stało” (mural
// stał na wolnej tablicy na środku), z36 „sklep CAŁY do przebudowy” (ceglana ściana z pustymi półkami, wyspy), audyt §5
// (brak lady, lodówki, sprzedawcy; napisy działów wiszą w powietrzu), osiedle 22:36 (sklep firmowy premium, shake bar).
// ZDJĘTE (BraxMiastoA.Wyczysc): całe wnętrze x 14.7…25.8, z 11.75…20.35, y 0.05…4.9 (Codex BraxNutritionShopBuild, Fable,
// Claude 31.10: regały, wyspy, tablica z muralem, lampy, drzwi) i pas frontu parteru x 14.5…25.97 (stara witryna, szyld,
// drzwi z modułu); krok BraxRegionInteriors.NutritionShop wyłączony.
// NOWE (× k, podłoga = chodnik, wejście bez progu): fasada premium (antracyt, stal szczotkowana, witryny z prawdziwym szkłem,
// drzwi automatyczne, daszek, szyld z liter przestrzennych z zielonym podświetleniem marki, logo-kaseton), wnętrze: MURAL
// z bykiem NA ŚCIANIE tylnej (w płaszczyźnie ściany — otwór w tynku), ściana produktów z podświetlanymi półkami (puszki
// białka, gainery, kreatyna, przedtreningówki, batony, shakery), napisy działów NA ŚCIANIE nad regałami, lodówki
// z napojami, lada z kasą i SHAKE BAR (blendery, kubki, menu), podesty ekspozycyjne, sufit z liniami LED, rośliny,
// punkty akcji (kup, zamów shake).
internal static class BraxMiastoNutrition
{
    const float F = BraxMiastoA.Chodnik, C = 3.9f, Front = 20.6f;
    const float X0 = 14.7f, X1 = 25.8f, Z0 = 11.8f, Z1 = 20.2f;     // lica wnętrza
    const float DX0 = 19.8f, DX1 = 22.2f;                           // drzwi automatyczne
    static float K => BraxMiastoA.K;
    static Transform root, kol;

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start();
        var r1 = BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(X0 - .05f, X1 + .1f, .05f, 4.9f, Z0 - .05f, Z1 + .15f), "nutrition_wnetrze");
        var r2 = BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(14.5f, 25.97f, .16f, 5.05f, Z1 + .15f, 21.9f), "nutrition_front");
        root = BraxRegion.Root(cityRoot, "CLAUDE A: BRAXHORN NUTRITION (od nowa)");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        BraxUliceKit.UstawGrupe("Nutrition fasada");
        Fasada();
        BraxUliceKit.UstawGrupe("Nutrition wnętrze", 50f);
        Wnetrze();
        int siatek = BraxUliceKit.Zapisz(root, "nutrition");
        BraxRegion.Rep.nutrition = true;
        BraxMiastoA.Log("NUTRITION: fasada + wnętrze " + (X1 - X0).ToString("F1") + "×" + (Z1 - Z0).ToString("F1") + " m, mural na ścianie, siatek " + siatek + "; zdjęte: " + r1 + " | " + r2);
    }

    static void Fasada()
    {
        var antracyt = BraxMiastoA.Tex("Nutrition: okładzina antracyt", 2.0f, new Color(.30f, .31f, .33f), new Color(.13f, .14f, .15f), .55f, "exterior_wall_cladding_02", "exterior_wall_cladding", "concrete_wall_008");
        var granit = BraxMiastoA.Tex("Nutrition: cokół granit", 1.4f, new Color(.45f, .45f, .46f), new Color(.18f, .18f, .19f), .6f, "granite_tile_02", "granite_tile");
        var stal = BraxMiastoA.Mat("Nutrition: stal szczotkowana", new Color(.60f, .62f, .64f), .3f, .9f);
        var alu = BraxMiastoA.Mat("Nutrition: aluminium witryn grafit", new Color(.09f, .10f, .105f), .4f, .55f);
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        var zielone = BraxMiastoA.Swieci("Nutrition: litery zielone (neon marki)", new Color(.45f, 1f, .2f), new Color(.9f, 2.4f, .35f));
        var biale = BraxMiastoA.Swieci("Nutrition: litery białe podświetlane", Color.white, new Color(1.6f, 1.6f, 1.6f));
        var boki = BraxMiastoA.Mat("Nutrition: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        float z = Front + .15f, yG = 5.05f, W = 26f - 14.5f;
        // okładzina z otworami: witryny i drzwi; ościeża 0.55 m w głąb (do lica wnętrza)
        Rect R(float x0, float x1, float y0, float y1) => Rect.MinMaxRect(x0 - 14.5f, y0, x1 - 14.5f, y1);
        var wit1 = R(15.25f, 19.35f, .62f, 3.35f); var drz = R(DX0, DX1, F, F + 2.85f); var wit2 = R(22.65f, 25.4f, .62f, 3.35f);
        var s = BraxUliceKit.S(antracyt);
        // lico patrzy na +z (ulica): oś R = −x, początek przy x 26 (prawa krawędź patrząc od ulicy)
        BraxMiastoA.Sciana(s, new Vector3(26f, 0, z), Vector3.left, Vector3.forward, W, .75f, yG, new[] { Mirror(wit1, W), Mirror(drz, W), Mirror(wit2, W) }, z - Z1, s);
        s.Quad(new Vector3(14.5f, .75f, Front), new Vector3(14.5f, .75f, z), new Vector3(14.5f, yG, z), new Vector3(14.5f, yG, Front), Vector3.left);
        s.Quad(new Vector3(25.97f, .75f, Front), new Vector3(25.97f, .75f, z), new Vector3(25.97f, yG, z), new Vector3(25.97f, yG, Front), Vector3.right);
        var g = BraxUliceKit.S(granit);
        foreach (var (a, b) in new[] { (14.5f, 15.25f), (19.35f, DX0), (DX1, 22.65f), (25.4f, 25.97f) }) g.Box(new Vector3(a, F, Front), new Vector3(b, .75f, z + .02f), 4 | 16);
        foreach (var (a, b) in new[] { (15.25f, 19.35f), (22.65f, 25.4f) }) g.Box(new Vector3(a, F, Front), new Vector3(b, .62f, z + .02f), 4 | 16);
        // witryny: rama aluminiowa, słupki, szkło (za nim wnętrze sklepu)
        foreach (var (a, b) in new[] { (15.25f, 19.35f), (22.65f, 25.4f) })
        {
            var al = BraxUliceKit.S(alu); float zz = Z1 + .25f;
            al.Box(new Vector3(a, .62f, zz - .05f), new Vector3(a + .07f, 3.35f, zz + .05f)); al.Box(new Vector3(b - .07f, .62f, zz - .05f), new Vector3(b, 3.35f, zz + .05f));
            al.Box(new Vector3(a, .62f, zz - .05f), new Vector3(b, .72f, zz + .05f)); al.Box(new Vector3(a, 3.27f, zz - .05f), new Vector3(b, 3.35f, zz + .05f));
            int n = Mathf.Max(1, Mathf.RoundToInt((b - a) / 1.5f));
            for (int i = 1; i < n; i++) { float x = a + (b - a) * i / n; al.Box(new Vector3(x - .03f, .72f, zz - .04f), new Vector3(x + .03f, 3.27f, zz + .04f)); }
            BraxUliceKit.S(szklo).Quad(new Vector3(a, .62f, zz), new Vector3(b, .62f, zz), new Vector3(b, 3.35f, zz), new Vector3(a, 3.35f, zz), Vector3.forward);
            BraxMiastoA.Kol(kol, "witryna Nutrition", new Vector3(a, F, zz - .06f), new Vector3(b, 3.35f, zz + .06f));
        }
        // drzwi automatyczne w osi ościeża, posadzka sklepu wchodzi pod drzwi (bez progu)
        BraxMiastoA.DrzwiPrzesuwne(root, "BRAXHORN NUTRITION — wejście", new Vector3((DX0 + DX1) * .5f, F, Z1 + .3f), Vector3.right, DX1 - DX0, 2.85f, szklo, alu);
        // lizeny ze stali szczotkowanej
        var st = BraxUliceKit.S(stal);
        foreach (float x in new[] { 14.62f, 19.55f, 22.45f, 25.85f }) st.Box(new Vector3(x - .07f, .75f, z), new Vector3(x + .07f, 3.5f, z + .1f), 16);
        // pas szyldu: litery przestrzenne „BRAXHORN NUTRITION” (zielone podświetlenie marki) + logo-kaseton z bykiem
        st.Box(new Vector3(14.6f, 3.5f, z), new Vector3(25.9f, 3.58f, z + .14f), 16);
        float cap = .52f, w = BraxUliceKit.SzerokoscNapisu("BRAXHORN NUTRITION", cap); if (w > 8.6f) cap *= 8.6f / w;
        BraxUliceKit.Napis3D("BRAXHORN NUTRITION", new Vector3(21.7f, 3.92f, z), Vector3.forward, cap, .1f, zielone, boki);
        // logo-kaseton: rama-pierścień (bez płyty za obrazem), obraz 8 cm przed okładziną
        BraxMiastoA.Obraz(root, "Nutrition logo (kaseton)", "Assets/CityRebuild/Brand/brand-badge.png", new Vector3(16.75f, 3.72f, z + .08f), Vector3.left, 1.1f, 1.1f, Vector3.forward, .55f);
        var ra = BraxUliceKit.S(alu);
        ra.Box(new Vector3(15.59f, 3.66f, z), new Vector3(15.65f, 4.88f, z + .12f), 16); ra.Box(new Vector3(16.75f, 3.66f, z), new Vector3(16.81f, 4.88f, z + .12f), 16);
        ra.Box(new Vector3(15.65f, 3.66f, z), new Vector3(16.75f, 3.72f, z + .12f), 16); ra.Box(new Vector3(15.65f, 4.82f, z), new Vector3(16.75f, 4.88f, z + .12f), 16);
        // daszek nad wejściem (szkło na stali, dół 3.05 m)
        st.Box(new Vector3(DX0 - .4f, 3.05f, z), new Vector3(DX1 + .4f, 3.15f, z + 1.2f), 16);
        BraxUliceKit.S(biale).Box(new Vector3(DX0 - .38f, 3.02f, z + 1.12f), new Vector3(DX1 + .38f, 3.05f, z + 1.18f));
        // gzyms
        g.Box(new Vector3(14.45f, yG, Front), new Vector3(26.0f, yG + .14f, z + .3f), 16);
        g.Box(new Vector3(14.48f, yG + .14f, Front), new Vector3(25.98f, yG + .3f, z + .18f), 16);
        BraxMiastoA.Kol(kol, "fasada Nutrition", new Vector3(14.5f, F, Z1), new Vector3(DX0, yG, z));
        BraxMiastoA.Kol(kol, "fasada Nutrition", new Vector3(DX1, F, Z1), new Vector3(25.97f, yG, z));
        BraxMiastoA.Kol(kol, "nadproże Nutrition", new Vector3(DX0, F + 2.85f, Z1), new Vector3(DX1, yG, z));
    }
    // prostokąt otworu w układzie „od lewej” → w układzie ściany zaczynającej się od x 26 (oś −x)
    static Rect Mirror(Rect r, float W) => Rect.MinMaxRect(W - r.xMax, r.yMin, W - r.xMin, r.yMax);

    static void Wnetrze()
    {
        var posadzka = BraxMiastoA.Tex("Nutrition: posadzka beton jasny", 2.0f, new Color(.80f, .80f, .79f), new Color(.62f, .62f, .60f), .6f, "concrete_floor_02");
        var tynk = BraxMiastoA.Tex("Nutrition: tynk biały", 2.4f, new Color(.95f, .95f, .94f), new Color(.9f, .9f, .89f), .9f, "white_plaster_02");
        var sufit = BraxMiastoA.Mat("Nutrition: sufit biały", new Color(.93f, .93f, .92f), .9f);
        var czern = BraxMiastoA.Mat("Nutrition: regały czarny lakier", new Color(.05f, .05f, .055f), .35f, .2f);
        var polka = BraxMiastoA.Mat("Nutrition: półki białe połysk", new Color(.95f, .95f, .95f), .25f);
        var led = BraxMiastoA.Swieci("Nutrition: listwa LED biała", Color.white, new Color(1.5f, 1.5f, 1.45f));
        var ledZ = BraxMiastoA.Swieci("Nutrition: listwa LED zielona", new Color(.45f, 1f, .2f), new Color(.6f, 2.0f, .25f));
        var towar = BraxMiastoA.Mat("Nutrition: puszki i opakowania (kolory z wierzchołków)", Color.white, .4f, .05f);
        var dab = BraxMiastoA.Mat("Nutrition: dąb jasny", new Color(.62f, .48f, .32f), .55f);
        var blat = BraxMiastoA.Mat("Nutrition: blat kwarc biały", new Color(.92f, .92f, .91f), .25f);
        var zielone = BraxMiastoA.Swieci("Nutrition: litery zielone (neon marki)", new Color(.45f, 1f, .2f), new Color(.9f, 2.4f, .35f));
        var boki = BraxMiastoA.Mat("Nutrition: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        var stal = BraxMiastoA.Mat("Nutrition: stal szczotkowana", new Color(.60f, .62f, .64f), .3f, .9f);
        var lodowka = BraxMiastoA.Swieci("Nutrition: lodówka (wnętrze podświetlone)", new Color(.85f, .92f, 1f), new Color(.45f, .55f, .65f));
        // MURAL na ścianie tylnej: otwór w tynku i obraz DOKŁADNIE w płaszczyźnie ściany (żadnej tablicy, żadnej nakładki)
        float mw = 5.6f, mh = mw * 941f / 1672f, mx0 = (X0 + X1) * .5f - mw * .5f, my0 = .45f;
        // ściana tylna (z0) widziana od środka: s rośnie od x1 w stronę x0
        var otwMural = Rect.MinMaxRect(X1 - (mx0 + mw), my0, X1 - mx0, my0 + mh);
        // ściana frontowa (z1) widziana od środka: s rośnie od x0 w stronę x1 (Pokoj: start x0, oś +x)
        var otwFrontOdSrodka = new[] { Rect.MinMaxRect(DX0 - X0, F, DX1 - X0, F + 2.85f), Rect.MinMaxRect(15.25f - X0, .62f, 19.35f - X0, 3.35f), Rect.MinMaxRect(22.65f - X0, .62f, 25.4f - X0, 3.35f) };
        BraxMiastoA.Pokoj(posadzka, tynk, sufit, X0, X1, Z0, Z1, F, C, new[] { new[] { otwMural }, null, otwFrontOdSrodka, null });
        BraxMiastoA.Obraz(root, "MURAL BRAXHORN (byk) na ścianie", "Assets/CityRebuild/Brand/nutrition-mural.jpg", new Vector3(mx0, my0, Z0), Vector3.right, mw, mh, Vector3.forward, 0f, .85f);
        // posadzka pod drzwiami do lica fasady (wejście równo z chodnikiem)
        BraxUliceKit.S(posadzka).Quad(new Vector3(DX0, F, Z1), new Vector3(DX1, F, Z1), new Vector3(DX1, F, Front), new Vector3(DX0, F, Front), Vector3.up);   // do linii zabudowy — dalej chodnik
        BraxMiastoA.Kol(kol, "posadzka sklepu", new Vector3(X0, F - .3f, Z0), new Vector3(X1, F, Front + .15f));
        BraxMiastoA.Kol(kol, "sufit sklepu", new Vector3(X0, C, Z0), new Vector3(X1, C + .2f, Z1));
        BraxMiastoA.Kol(kol, "ściana tylna", new Vector3(X0, F, Z0 - .2f), new Vector3(X1, C, Z0));
        BraxMiastoA.Kol(kol, "ściana zachód", new Vector3(X0 - .2f, F, Z0), new Vector3(X0, C, Z1));
        BraxMiastoA.Kol(kol, "ściana wschód", new Vector3(X1, F, Z0), new Vector3(X1 + .2f, C, Z1));
        // sufit: linie LED (3 cm pod sufitem) i zielony akcent nad muralem
        for (float x = X0 + 1.4f; x < X1 - 1f; x += 2.3f) BraxUliceKit.S(led).Box(new Vector3(x - .03f, C - .05f, Z0 + 1.0f), new Vector3(x + .03f, C - .02f, Z1 - .8f));
        BraxUliceKit.S(ledZ).Box(new Vector3(mx0, my0 + mh + .15f, Z0 + .02f), new Vector3(mx0 + mw, my0 + mh + .19f, Z0 + .06f));
        BraxUliceKit.S(ledZ).Box(new Vector3(mx0, my0 - .2f, Z0 + .02f), new Vector3(mx0 + mw, my0 - .16f, Z0 + .06f));
        // ŚCIANA PRODUKTÓW (zachód): regał na całą długość, 5 półek z listwami LED, towar marki
        var rng = new System.Random(2011);
        Color32[] marka = { BraxUliceKit.C(.05f, .05f, .055f), BraxUliceKit.C(.95f, .95f, .95f), BraxUliceKit.C(.25f, .75f, .15f), BraxUliceKit.C(.12f, .12f, .13f), BraxUliceKit.C(.85f, .2f, .15f), BraxUliceKit.C(.95f, .75f, .15f) };
        var tw = BraxUliceKit.S(towar); var cz = BraxUliceKit.S(czern); var pl = BraxUliceKit.S(polka);
        float rd = .5f, rz0 = Z0 + .25f, rz1 = Z1 - .55f, rh = 2.35f * K;
        cz.Box(new Vector3(X0, F, rz0), new Vector3(X0 + .04f, F + rh, rz1), 1);                 // plecy (przy ścianie)
        cz.Box(new Vector3(X0, F, rz0 - .04f), new Vector3(X0 + rd, F + rh, rz0), 1); cz.Box(new Vector3(X0, F, rz1), new Vector3(X0 + rd, F + rh, rz1 + .04f), 1);
        cz.Box(new Vector3(X0, F, rz0), new Vector3(X0 + rd, F + .12f, rz1), 1 | 4);             // cokół regału
        string[] dzialy = { "BIAŁKO", "KREATYNA", "PRZEDTRENINGÓWKI", "BATONY", "AKCESORIA" };
        // 3.11 14:17: prawdziwe odżywki BRAXHORN SPORT NUTRITION z etykiet właściciela (kubły, worki gainera) zamiast walców z kolorów
        bool odzywki = BraxMiastoAssety.Jest("miasto_sklepy"); var twN = odzywki ? BraxSklepyTowar.Korzen(root, "BRAXHORN NUTRITION — ściana produktów") : null;
        for (int p = 0; p < 5; p++)
        {
            float y = F + .12f + p * (rh - .3f) / 4f;
            pl.Box(new Vector3(X0 + .04f, y - .03f, rz0), new Vector3(X0 + rd, y, rz1), 1);
            BraxUliceKit.S(led).Box(new Vector3(X0 + rd - .04f, y - .05f, rz0), new Vector3(X0 + rd - .01f, y - .03f, rz1));
            if (p == 4) break;
            if (odzywki) { BraxSklepyTowar.Polka(p == 0 ? "odz_dol" : p == 3 ? "odz_gora" : "odz_polka", twN, new Vector3(X0 + .27f, y + .004f, rz0 + .1f), Vector3.forward, Vector3.left, rz1 - rz0 - .25f, rng, (rh - .3f) / 4f - .02f); continue; }
            for (float zq = rz0 + .1f; zq < rz1 - .15f;)
            {
                int typ = (p + (int)((zq - rz0) / 2.4f)) % 4;
                float r = typ == 0 ? .1f * K : typ == 1 ? .085f * K : typ == 2 ? .055f * K : .045f * K;
                float h = typ == 0 ? .28f * K : typ == 1 ? .2f * K : typ == 2 ? .18f * K : .22f * K;
                if (p == 3 && typ < 2) { r = .045f * K; h = .22f * K; }
                var c = new Vector3(X0 + .27f, y, zq + r);
                if (typ == 3 && p != 3) { tw.kolor = marka[rng.Next(marka.Length)]; tw.Box(new Vector3(X0 + .1f, y, zq), new Vector3(X0 + .4f, y + .06f * K, zq + .2f * K)); zq += .22f * K; continue; }   // batony w kartonikach
                var kol0 = marka[rng.Next(3)]; var pas = marka[2 + rng.Next(4)];
                tw.kolor = kol0; tw.Rura(c, c + Vector3.up * h * .35f, r, 10, true);
                tw.kolor = pas; tw.Rura(c + Vector3.up * h * .35f, c + Vector3.up * h * .7f, r, 10, false);
                tw.kolor = kol0; tw.Rura(c + Vector3.up * h * .7f, c + Vector3.up * h, r, 10, true);
                zq += r * 2f + .025f;
            }
        }
        tw.kolor = BraxUliceKit.Bialy;
        if (odzywki) BraxSklepyTowar.Zakoncz(twN, "BRAXHORN NUTRITION");
        BraxMiastoA.Kol(kol, "ściana produktów", new Vector3(X0, F, rz0 - .04f), new Vector3(X0 + rd, F + rh, rz1 + .04f));
        // napisy działów NA ŚCIANIE nad regałem (litery przestrzenne na tynku, nie w powietrzu)
        float seg = (rz1 - rz0) / 3f;
        for (int i = 0; i < 3; i++)
            BraxUliceKit.Napis3D(new[] { "BIAŁKO", "KREATYNA", "BATONY" }[i], new Vector3(X0, F + rh + .25f, rz0 + seg * (i + .5f)), Vector3.right, .2f, .04f, zielone, boki);
        // LODÓWKI z napojami (ściana tylna, po bokach muralu)
        foreach (float xa in new[] { X0 + .65f, X1 - 2.35f })
        {
            // lodówka przeszklona: korpus (boki, góra, dół, plecy podświetlone), półki, napoje w środku, drzwi ze szkła
            float xb = xa + 1.7f, zf = Z0 + .75f, hL = 2.1f * K;
            cz.Box(new Vector3(xa, F, Z0), new Vector3(xa + .06f, F + hL, zf)); cz.Box(new Vector3(xb - .06f, F, Z0), new Vector3(xb, F + hL, zf));
            cz.Box(new Vector3(xa + .06f, F, Z0), new Vector3(xb - .06f, F + .15f, zf), 4); cz.Box(new Vector3(xa + .06f, F + hL - .22f, Z0), new Vector3(xb - .06f, F + hL, zf), 8);
            BraxUliceKit.S(lodowka).Quad(new Vector3(xa + .06f, F + .15f, Z0 + .03f), new Vector3(xb - .06f, F + .15f, Z0 + .03f), new Vector3(xb - .06f, F + hL - .22f, Z0 + .03f), new Vector3(xa + .06f, F + hL - .22f, Z0 + .03f), Vector3.forward);
            for (int p = 0; p < 4; p++)
            {
                float y = F + .15f + p * (hL - .4f) / 4f;
                if (p > 0) BraxUliceKit.S(polka).Box(new Vector3(xa + .06f, y - .015f, Z0 + .05f), new Vector3(xb - .06f, y, zf - .06f));
                for (float x = xa + .14f; x < xb - .12f; x += .105f)
                    for (float zb = Z0 + .15f; zb < zf - .12f; zb += .16f)
                    { tw.kolor = marka[rng.Next(marka.Length)]; tw.Rura(new Vector3(x, y + .002f, zb), new Vector3(x, y + .2f * K, zb), .032f * K, 6); }
            }
            tw.kolor = BraxUliceKit.Bialy;
            BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Quad(new Vector3(xa + .06f, F + .15f, zf - .02f), new Vector3(xb - .06f, F + .15f, zf - .02f), new Vector3(xb - .06f, F + hL - .22f, zf - .02f), new Vector3(xa + .06f, F + hL - .22f, zf - .02f), Vector3.forward);
            BraxUliceKit.S(stal).Box(new Vector3((xa + xb) * .5f - .02f, F + .15f, zf - .02f), new Vector3((xa + xb) * .5f + .02f, F + hL - .22f, zf + .02f));
            BraxUliceKit.S(stal).Rura(new Vector3((xa + xb) * .5f - .08f, F + .9f, zf + .05f), new Vector3((xa + xb) * .5f - .08f, F + 1.6f, zf + .05f), .012f, 6);
            BraxMiastoA.Kol(kol, "lodówka", new Vector3(xa, F, Z0), new Vector3(xb, F + hL, zf + .05f));
        }
        // LADA z kasą i SHAKE BAR (wschód): lada dębowa z kwarcowym blatem, za nią blat roboczy z blenderami, menu na ścianie
        float lx0 = X1 - 2.4f, lx1 = lx0 + .65f, lz0 = Z0 + 2.3f, lz1 = Z0 + 6.3f, lh = 1.05f * K;
        BraxUliceKit.S(dab).Box(new Vector3(lx0, F, lz0), new Vector3(lx1, F + lh - .05f, lz1), 4);
        BraxUliceKit.S(ledZ).Box(new Vector3(lx0 - .03f, F + .08f, lz0 + .05f), new Vector3(lx0, F + .12f, lz1 - .05f));
        BraxUliceKit.S(blat).Box(new Vector3(lx0 - .06f, F + lh - .05f, lz0 - .03f), new Vector3(lx1 + .03f, F + lh, lz1 + .03f));
        BraxMiastoA.Kol(kol, "lada", new Vector3(lx0 - .06f, F, lz0), new Vector3(lx1 + .03f, F + lh, lz1));
        // kasa z ekranem
        BraxUliceKit.S(czern).Box(new Vector3(lx0 + .15f, F + lh, lz0 + .4f), new Vector3(lx0 + .45f, F + lh + .08f, lz0 + .8f));
        BraxUliceKit.S(czern).Box(new Vector3(lx0 + .3f, F + lh + .08f, lz0 + .5f), new Vector3(lx0 + .33f, F + lh + .38f, lz0 + .7f));
        // blat roboczy shake baru przy ścianie + blendery + kubki + menu z liter
        BraxUliceKit.S(dab).Box(new Vector3(X1 - .65f, F, lz0 - .5f), new Vector3(X1, F + .9f * K, lz1 + .5f), 2 | 4);
        BraxUliceKit.S(blat).Box(new Vector3(X1 - .68f, F + .9f * K, lz0 - .5f), new Vector3(X1, F + .9f * K + .04f, lz1 + .5f), 2);
        BraxMiastoA.Kol(kol, "blat shake baru", new Vector3(X1 - .68f, F, lz0 - .5f), new Vector3(X1, F + .9f * K + .04f, lz1 + .5f));
        float yb = F + .9f * K + .04f;
        for (int i = 0; i < 3; i++)
        {
            var b = new Vector3(X1 - .35f, yb, lz0 + .4f + i * .7f);
            BraxUliceKit.S(czern).Box(b + new Vector3(-.1f, 0, -.1f), b + new Vector3(.1f, .16f, .1f));
            BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Rura(b + Vector3.up * .16f, b + Vector3.up * .42f, .07f, 10);
        }
        for (int i = 0; i < 8; i++) { var b = new Vector3(X1 - .3f, yb, lz1 - .2f - i * .09f); tw.kolor = marka[1]; tw.Rura(b, b + Vector3.up * .14f, .035f, 8); }
        tw.kolor = BraxUliceKit.Bialy;
        BraxUliceKit.Napis3D("SHAKE BAR", new Vector3(X1, 2.65f, (lz0 + lz1) * .5f), Vector3.left, .26f, .05f, zielone, boki);
        BraxUliceKit.Napis3D("WANILIA · CZEKOLADA · TRUSKAWKA", new Vector3(X1, 2.25f, (lz0 + lz1) * .5f), Vector3.left, .1f, .02f, BraxMiastoA.Swieci("Nutrition: litery białe podświetlane", Color.white, new Color(1.6f, 1.6f, 1.6f)), boki);
        // PODESTY EKSPOZYCYJNE (dąb + biały blat) z piramidami puszek — przodem do wejścia
        foreach (var c0 in new[] { new Vector3(18.2f, F, 16.3f), new Vector3(21.4f, F, 16.3f) })
        {
            BraxUliceKit.S(dab).Box(c0 + new Vector3(-.9f, 0, -.6f), c0 + new Vector3(.9f, .55f * K, .6f), 4);
            BraxUliceKit.S(blat).Box(c0 + new Vector3(-.93f, .55f * K, -.63f), c0 + new Vector3(.93f, .55f * K + .03f, .63f));
            float yy = .55f * K + .03f;
            for (int w = 0; w < 3; w++)
                for (int i = 0; i < 4 - w; i++)
                    for (int j = 0; j < 3 - w; j++)
                    {
                        var p = c0 + new Vector3(-.6f + (i + w * .5f) * .4f, yy + w * .28f * K, -.3f + (j + w * .5f) * .3f);
                        tw.kolor = marka[(i + j + w) % 3]; tw.Rura(p, p + Vector3.up * .28f * K, .1f * K, 10);
                    }
            tw.kolor = BraxUliceKit.Bialy;
            BraxMiastoA.Kol(kol, "podest ekspozycyjny", c0 + new Vector3(-.93f, 0, -.63f), c0 + new Vector3(.93f, .55f * K + .03f, .63f));
        }
        // rośliny w donicach (Poly Haven CC0, jeśli pobrane) przy witrynach
        var roslina = BraxRegion.Model("potted_plant_02", "potted_plant_04");
        if (roslina) foreach (var p in new[] { new Vector3(X0 + .6f, F, Z1 - .45f), new Vector3(X1 - .55f, F, Z1 - .45f) }) BraxRegion.Put(root, roslina, "Nutrition: roślina w donicy", p, 30f, 1f, true, 1.5f * K);
        BraxRegion.Warm(root, "Nutrition światło", new Vector3(18f, 3.5f, 16f), 1.3f, 9f);
        BraxRegion.Warm(root, "Nutrition światło", new Vector3(23f, 3.5f, 15f), 1.3f, 9f);
        BraxMiastoA.Akcja(root, "nutrition_lada", "KUP", "Sprzedawca: Białko, kreatyna, przedtreningówka — co dziś bierzesz?", new Vector3(lx0 - .6f, F, (lz0 + lz1) * .5f), 1.7f);
        BraxMiastoA.Akcja(root, "nutrition_shake", "ZAMÓW SHAKE", "Shake bar: Wanilia, czekolada albo truskawka — 30 g białka.", new Vector3(lx0 - .6f, F, lz1 + .4f), 1.4f);
    }
}
