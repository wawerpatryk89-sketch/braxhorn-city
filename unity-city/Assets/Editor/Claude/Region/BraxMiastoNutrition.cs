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
// z bykiem NA ŚCIANIE tylnej (osobny otwór w wykończeniu), ciemny mały klinkier na czterech ścianach (9.11);
// bez długich białych półek na ścianie zachodniej (polecenie właściciela 6.11). Lodówki
// z napojami, JEDNA lada SHAKE BAR z kasą (blendery, kubki, menu; 9.11), podesty ekspozycyjne, sufit z liniami LED, rośliny,
// punkty akcji (kup, zamów shake).
internal static class BraxMiastoNutrition
{
    const float F = BraxMiastoA.Chodnik, C = 3.9f, Front = 20.6f;
    const float X0 = 14.7f, X1 = 25.8f, Z0 = 11.8f, Z1 = 20.2f;     // lica wnętrza
    const float DX0 = 19.8f, DX1 = 22.2f;                           // drzwi automatyczne
    static float K => BraxMiastoA.K;
    static Transform root, kol;
    static Vector3 ekranKasy, punktLady;

    public static void Apply(Transform cityRoot)
    {
        // 9.11 17:55 (właściciel, zdjęcia z APK): „sklep z odżywkami ma być cały w tej małej cegiełce ciemnej, ta czerwona do
        // wywalenia”. Kopia materiału oryginału (city_baked_112, 3 m na kafel) dawała wielkie czerwone cegły — zdjęta. Wszystkie
        // cztery ściany: ciemny klinkier ze skanu Poly Haven brick_wall_001 w skali rzeczywistej (kafel 1 m × k), bez czerwonej
        // cegły jako zamiennika (brak skanu = ciemny kolor klinkieru, nie czerwień).
        var sciany = BraxRegionMeble.Tex("Nutrition: cegła ciemna (mały klinkier)", 1.0f, new Color(.60f, .51f, .47f), .9f, "brick_wall_001");
        if (!sciany) sciany = BraxMiastoA.Mat("Nutrition: cegła ciemna (brak skanu)", new Color(.27f, .21f, .19f), .9f);
        BraxUliceKit.Start();
        var r1 = BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(X0 - .05f, X1 + .1f, .05f, 4.9f, Z0 - .05f, Z1 + .15f), "nutrition_wnetrze");
        var r2 = BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(14.5f, 25.97f, .16f, 5.05f, Z1 + .15f, 21.9f), "nutrition_front");
        root = BraxRegion.Root(cityRoot, "CLAUDE A: BRAXHORN NUTRITION (od nowa)");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);
        BraxUliceKit.UstawGrupe("Nutrition fasada");
        Fasada();
        BraxUliceKit.UstawGrupe("Nutrition wnętrze", 50f);
        Wnetrze(sciany);
        int siatek = BraxUliceKit.Zapisz(root, "nutrition");
        Pozostalosci();
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

    static void Wnetrze(Material sciany)
    {
        var posadzka = BraxMiastoA.Tex("Nutrition: posadzka beton jasny", 2.0f, new Color(.80f, .80f, .79f), new Color(.62f, .62f, .60f), .6f, "concrete_floor_02");
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
        // MURAL na ścianie tylnej: otwór w płytce i obraz DOKŁADNIE w płaszczyźnie ściany (żadnej tablicy, żadnej nakładki)
        float mw = 5.6f, mh = mw * 941f / 1672f, mx0 = (X0 + X1) * .5f - mw * .5f, my0 = .45f;
        // ściana tylna (z0) widziana od środka: s rośnie od x1 w stronę x0
        var otwMural = Rect.MinMaxRect(X1 - (mx0 + mw), my0, X1 - mx0, my0 + mh);
        // ściana frontowa (z1) widziana od środka: s rośnie od x0 w stronę x1 (Pokoj: start x0, oś +x)
        var otwFrontOdSrodka = new[] { Rect.MinMaxRect(DX0 - X0, F, DX1 - X0, F + 2.85f), Rect.MinMaxRect(15.25f - X0, .62f, 19.35f - X0, 3.35f), Rect.MinMaxRect(22.65f - X0, .62f, 25.4f - X0, 3.35f) };
        BraxMiastoA.Pokoj(posadzka, sciany, sufit, X0, X1, Z0, Z1, F, C, new[] { new[] { otwMural }, null, otwFrontOdSrodka, null });
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
        // 6.11: usunięty wyłącznie długi regał na ścianie z płytką (półki, LED, plecy,
        // towar i jego kolizja). Lodówki po bokach muralu, wyspy i shake bar pozostają.
        var rng = new System.Random(2011);
        Color32[] marka = { BraxUliceKit.C(.05f, .05f, .055f), BraxUliceKit.C(.95f, .95f, .95f), BraxUliceKit.C(.25f, .75f, .15f), BraxUliceKit.C(.12f, .12f, .13f), BraxUliceKit.C(.85f, .2f, .15f), BraxUliceKit.C(.95f, .75f, .15f) };
        var tw = BraxUliceKit.S(towar); var cz = BraxUliceKit.S(czern);
        bool odzywki = BraxMiastoAssety.Jest("miasto_sklepy");
        // 11.11 (właściciel 19:44: „sklep z odżywkami… są lodówki. Po co lodówki w sklepie z odżywkami? Tam mają być półki, na półkach
        // odżywki”): lodówki zdjęte — po bokach muralu REGAŁY Z ODŻYWKAMI (czarna rama z zaokrąglonymi krawędziami, 5 białych półek,
        // plecy jasne, listwa LED pod górną półką), towar prawdziwy z paczki sklepów: dół — gainery i kubły 2–4 kg, środek — whey,
        // małe opakowania, góra — kreatyna, przedtreningówki, dodatki (BraxSklepyTowar „odz_dol / odz_polka / odz_gora”)
        var plecy = BraxMiastoA.Mat("Nutrition: plecy regału (biel mat)", new Color(.9f, .9f, .89f), .7f);
        foreach (float xa in new[] { X0 + .65f, X1 - 2.35f })
        {
            float xb = xa + 1.7f, zf = Z0 + .5f * K, hR = 2.15f * K;
            cz.Poducha(new Vector3(xa, F, Z0 + .005f), new Vector3(xa + .05f, F + hR, zf), .008f, 0f, 3);
            cz.Poducha(new Vector3(xb - .05f, F, Z0 + .005f), new Vector3(xb, F + hR, zf), .008f, 0f, 3);
            cz.Poducha(new Vector3(xa + .05f, F, Z0 + .005f), new Vector3(xb - .05f, F + .12f, zf - .02f), .006f, 0f, 3);           // cokół
            cz.Poducha(new Vector3(xa + .05f, F + hR - .08f, Z0 + .005f), new Vector3(xb - .05f, F + hR, zf), .008f, 0f, 3);        // wieniec
            BraxUliceKit.S(plecy).Quad(new Vector3(xa + .05f, F + .12f, Z0 + .03f), new Vector3(xb - .05f, F + .12f, Z0 + .03f), new Vector3(xb - .05f, F + hR - .08f, Z0 + .03f), new Vector3(xa + .05f, F + .12f + hR - .2f, Z0 + .03f), Vector3.forward);
            string[] typ = { "odz_dol", "odz_polka", "odz_polka", "odz_gora", "odz_gora" };
            float krok = (hR - .25f) / 5f;
            for (int p = 0; p < 5; p++)
            {
                float y = F + .12f + p * krok;
                if (p > 0) BraxUliceKit.S(polka).Poducha(new Vector3(xa + .05f, y - .025f, Z0 + .04f), new Vector3(xb - .05f, y, zf - .01f), .006f, 0f, 3);
                if (odzywki) BraxSklepyTowar.Polka(typ[p], root, new Vector3(xa + .1f, y + .004f, (Z0 + .04f + zf) * .5f), Vector3.right, Vector3.back, 1.5f, rng, krok - .06f);
                else
                    for (float x = xa + .16f; x < xb - .14f; x += .2f)
                    { tw.kolor = marka[rng.Next(marka.Length)]; tw.Rura(new Vector3(x, y + .002f, (Z0 + zf) * .5f), new Vector3(x, y + Mathf.Min(krok - .08f, .26f * K), (Z0 + zf) * .5f), .085f * K, 12); }
            }
            tw.kolor = BraxUliceKit.Bialy;
            BraxUliceKit.S(led).Box(new Vector3(xa + .08f, F + hR - .08f - .03f, zf - .06f), new Vector3(xb - .08f, F + hR - .08f - .005f, zf - .03f));
            BraxMiastoA.Kol(kol, "regał z odżywkami", new Vector3(xa, F, Z0), new Vector3(xb, F + hR, zf));
        }
        // JEDNA LADA (właściciel 9.11 17:57–17:58: „i tą ladę usunąć, bo są dwie”): wolnostojąca lada z kasą USUNIĘTA; zostaje
        // lada shake baru przy ścianie wschodniej — na niej kasa z ekranem (stan regeneracji), blendery, kubki; sprzedaż i shake z jednej lady.
        float lz0 = Z0 + 2.3f, lz1 = Z0 + 6.3f;
        // blat roboczy shake baru przy ścianie + blendery + kubki + menu z liter
        BraxUliceKit.S(dab).Box(new Vector3(X1 - .65f, F, lz0 - .5f), new Vector3(X1, F + .9f * K, lz1 + .5f), 2 | 4);
        BraxUliceKit.S(blat).Box(new Vector3(X1 - .68f, F + .9f * K, lz0 - .5f), new Vector3(X1, F + .9f * K + .04f, lz1 + .5f), 2);
        BraxMiastoA.Kol(kol, "blat shake baru", new Vector3(X1 - .68f, F, lz0 - .5f), new Vector3(X1, F + .9f * K + .04f, lz1 + .5f));
        float yb = F + .9f * K + .04f;
        // 11.11 (właściciel 19:44: „nie ma normalnej lady, żeby tam ktoś sprzedawał. Ona jest przy samej ścianie”): LADA SPRZEDAŻY
        // wolnostojąca 0.9 m przed blatem shake baru — sprzedawca stoi między ladą a blatem, klient od sali. Front z dębu na cofniętym
        // czarnym cokole, blat z białego kwarcu z nawisem od klienta, wejście za ladę od południa (przy regale).
        float lx0 = X1 - .68f - .9f - .62f, lx1 = lx0 + .62f, lzA = lz0 - .1f, lzB = lz1 + .5f;
        BraxUliceKit.S(czern).Poducha(new Vector3(lx0 + .06f, F, lzA), new Vector3(lx1, F + .1f, lzB), .006f, 0f, 3);
        BraxUliceKit.S(dab).Poducha(new Vector3(lx0, F + .1f, lzA), new Vector3(lx1, yb - .04f, lzB), .01f, 0f, 4);
        BraxUliceKit.S(blat).Poducha(new Vector3(lx0 - .06f, yb - .04f, lzA - .02f), new Vector3(lx1 + .01f, yb, lzB + .02f), .01f, 0f, 4);
        BraxMiastoA.Kol(kol, "lada sprzedaży", new Vector3(lx0 - .06f, F, lzA - .02f), new Vector3(lx1 + .01f, yb, lzB + .02f));
        BraxUliceKit.Napis3D("BRAXHORN NUTRITION", new Vector3(lx0 - .002f, F + .62f * K, (lzA + lzB) * .5f), Vector3.left, .11f, .02f, zielone, boki);
        // kasa z ekranem przodem do klienta (−x) na ladzie sprzedaży; ekran pokazuje regenerację (BraxNutritionCounter)
        float kz = lzA + .45f, kx = lx1 - .12f;
        BraxUliceKit.S(czern).Poducha(new Vector3(kx - .3f, yb, kz - .2f), new Vector3(kx, yb + .08f, kz + .2f), .01f, 0f, 3);
        BraxUliceKit.S(czern).Box(new Vector3(kx - .2f, yb + .08f, kz - .03f), new Vector3(kx - .16f, yb + .2f, kz + .03f));
        BraxUliceKit.S(czern).Poducha(new Vector3(kx - .22f, yb + .2f, kz - .19f), new Vector3(kx - .19f, yb + .44f, kz + .19f), .006f, 0f, 3);
        ekranKasy = new Vector3(kx - .225f, yb + .32f, kz);
        // na ladzie: kubły whey i shakery przy kasie (towar do ręki)
        if (odzywki) BraxSklepyTowar.Polka("odz_gora", root, new Vector3(lx0 + .3f, yb + .004f, kz + .5f), Vector3.forward, Vector3.right, lzB - kz - .7f, rng, .3f);
        for (int i = 0; i < 3; i++)
        {
            var b = new Vector3(X1 - .35f, yb, lz0 + .4f + i * .7f);
            BraxUliceKit.S(czern).Box(b + new Vector3(-.1f, 0, -.1f), b + new Vector3(.1f, .16f, .1f));
            BraxUliceKit.S(BraxUliceKit.SzkloPrzezroczyste).Rura(b + Vector3.up * .16f, b + Vector3.up * .42f, .07f, 10);
        }
        for (int i = 0; i < 8; i++) { var b = new Vector3(X1 - .3f, yb, lz1 - .2f - i * .09f); tw.kolor = marka[1]; tw.Rura(b, b + Vector3.up * .14f, .035f, 8); }
        tw.kolor = BraxUliceKit.Bialy;
        BraxUliceKit.Napis3D("SHAKE BAR", new Vector3(X1, 2.65f, (lz0 + lz1) * .5f), Vector3.left, .26f, .05f, zielone, boki);
        // menu shake baru z grafiki właściciela (3.11 14:37) na ścianie nad blatem roboczym; bez grafiki — dawny napis smaków
        var menuSB = odzywki ? BraxMiastoAssety.Model("miasto_sklepy", "menu_2") : null;
        if (menuSB) BraxMiastoAssety.Postaw(root, menuSB, "Nutrition: menu SHAKE BAR", new Vector3(X1 - .02f - .015f * K, 1.35f, (lz0 + lz1) * .5f), 270f, 1f, true, false);
        else BraxUliceKit.Napis3D("WANILIA · CZEKOLADA · TRUSKAWKA", new Vector3(X1, 2.25f, (lz0 + lz1) * .5f), Vector3.left, .1f, .02f, BraxMiastoA.Swieci("Nutrition: litery białe podświetlane", Color.white, new Color(1.6f, 1.6f, 1.6f)), boki);
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
                        var mt = odzywki ? BraxMiastoAssety.Model("miasto_sklepy", "odz_whey_" + (1 + (i + 2 * j + w) % 4)) : null;   // piramida prawdziwych kubłów whey 2 kg
                        if (mt) { BraxMiastoAssety.Postaw(root, mt, "Nutrition: kubeł whey na podeście", p, 0f, 1f, true, false); continue; }
                        tw.kolor = marka[(i + j + w) % 3]; tw.Rura(p, p + Vector3.up * .28f * K, .1f * K, 10);
                    }
            tw.kolor = BraxUliceKit.Bialy;
            BraxMiastoA.Kol(kol, "podest ekspozycyjny", c0 + new Vector3(-.93f, 0, -.63f), c0 + new Vector3(.93f, .55f * K + .03f, .63f));
        }
        var plakatN = odzywki ? BraxMiastoAssety.Model("miasto_sklepy", "plakat_05") : null;
        if (plakatN) BraxMiastoAssety.Postaw(root, plakatN, "Nutrition: plakat KREATYNA", new Vector3(X1 - .02f - .01f * K, F + 1.15f, Z0 + 7.2f), 270f, 1f, true, false);
        // rośliny w donicach (Poly Haven CC0, jeśli pobrane) przy witrynach
        var roslina = BraxRegion.Model("potted_plant_02", "potted_plant_04");
        if (roslina) foreach (var p in new[] { new Vector3(X0 + .6f, F, Z1 - .45f), new Vector3(X1 - .55f, F, Z1 - .45f) }) BraxRegion.Put(root, roslina, "Nutrition: roślina w donicy", p, 30f, 1f, true, 1.5f * K);
        BraxRegion.Warm(root, "Nutrition światło", new Vector3(18f, 3.5f, 16f), 1.3f, 9f);
        BraxRegion.Warm(root, "Nutrition światło", new Vector3(23f, 3.5f, 15f), 1.3f, 9f);
        // klient podchodzi do lady sprzedaży od sali (−x)
        float xk = X1 - .68f - .9f - .62f - .7f;
        BraxMiastoA.Akcja(root, "nutrition_lada", "KUP", "Sprzedawca: Białko, kreatyna, przedtreningówka — co dziś bierzesz?", new Vector3(xk, F, lz0 + .4f), 1.5f);
        BraxMiastoA.Akcja(root, "nutrition_shake", "ZAMÓW SHAKE", "Shake bar: Wanilia, czekolada albo truskawka — 30 g białka.", new Vector3(xk, F, lz1 - .2f), 1.4f);
        punktLady = new Vector3(xk, F, (lz0 + lz1) * .5f);
    }

    // 9.11 17:55 (zdjęcia z APK: „KREATYNA”, „REGENERACJA” i „REGENERACJA 92/100” wiszą przy muralu i na nim): napisy działów
    // i ekran regeneracji sklepu Codex (TextMesh — cięcie siatek ich nie zdejmuje) zostawały w środku nowego sklepu. Napisy
    // działów w obrysie wnętrza — usunięte; ekran regeneracji przeniesiony na ekran kasy (mały, przodem do klienta), punkt
    // uzupełniania energii — przy jedynej ladzie. Mural wolny od nakładek.
    static void Pozostalosci()
    {
        var wnetrze = new Bounds(new Vector3((X0 + X1) * .5f, 2.5f, (Z0 + Z1) * .5f), new Vector3(X1 - X0 + .4f, 5.2f, Z1 - Z0 + .6f));
        var lada = Object.FindObjectsByType<BraxNutritionCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var ekrany = new HashSet<TextMesh>(lada.Where(c => c && c.readout).Select(c => c.readout));
        int napisy = 0;
        foreach (var t in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t || t.transform.IsChildOf(root) || ekrany.Contains(t) || !wnetrze.Contains(t.transform.position)) continue;
            Object.DestroyImmediate(t.gameObject); napisy++;
        }
        foreach (var c in lada)
        {
            if (!c) continue;
            c.servicePoint = punktLady; c.interactionRadius = 1.5f;
            if (c.readout)
            {
                var tr = c.readout.transform; tr.SetParent(root, true);
                tr.position = ekranKasy; tr.rotation = Quaternion.Euler(0f, 90f, 0f);   // czytelny od strony klienta (−x)
                c.readout.characterSize = .028f * 10f / 64f; c.readout.color = new Color(.55f, 1f, .35f);
            }
            UnityEditor.EditorUtility.SetDirty(c);
        }
        BraxMiastoA.Log("NUTRITION: pozostałości Codex — napisy działów usunięte " + napisy + ", ekran regeneracji na kasie, punkt energii przy ladzie (" + lada.Length + ")");
    }
}
