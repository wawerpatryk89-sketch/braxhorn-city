using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// WULKANIZACJA Z KODU (11.11.2026, właściciel 19:36: „Felgi są brzydkie w wulkanizacji”; zdjęcia z APK: czerwone maszyny-klocki;
// 21:52: „Wszystko ma być dopieszczone”). Wymiary rzeczywiste × K, siatki raz na wariant (BraxChlodnie.Wez) i stawiane wiele razy.
//  • KOŁO 205/55 R16: opona toczona (barki zaokrąglone, bieżnik z 4 rowkami obwodowymi, boki), felga aluminiowa — kołnierz,
//    bęben, 3 wzory szprych (5 podwójnych, 10 cienkich, 6 Y), piasta z dekielkiem, 5 śrub, wentyl; ciemna tarcza w środku bębna
//    (przez szprychy nie widać „na wylot”). Kolory felg: srebrny, grafit, czarny połysk.
//  • MONTAŻOWNICA: korpus z zaokrąglonymi krawędziami (grafit), talerz z 4 szczękami, słup odchylny i ramię z trzpieniem i głowicą
//    montażową (czerwony lakier), zbijak stopki z łopatką i odbojem gumowym z boku, 3 pedały, manometr z wężem, napis BRAXHORN 3D.
//  • WYWAŻARKA: szafka z tacą na ciężarki (przegródki, ciężarki), wałek chromowany ze stożkiem i nakrętką, osłona koła podniesiona
//    (półwalec), konsola z ekranem (odczyt L / R), ramię pomiarowe; koło na wałku.
internal static class BraxWarsztatKod
{
    static Material guma, alu, aluGrafit, aluCzarny, chrom, grafit, czerwien, czern, ekran, bialy;
    static void Materialy()
    {
        if (guma) return;
        guma = BraxUliceKit.Mat("koło: guma opony (mat)", new Color(.045f, .045f, .05f), .92f, 0f);
        alu = BraxCityMaterials.Surface("koło: felga aluminiowa (srebrna)", new Color(.80f, .81f, .83f), .22f, 1f);
        aluGrafit = BraxCityMaterials.Surface("koło: felga aluminiowa (grafit)", new Color(.30f, .31f, .33f), .3f, 1f);
        aluCzarny = BraxCityMaterials.Surface("koło: felga (czarny połysk)", new Color(.05f, .05f, .055f), .15f, .6f);
        chrom = BraxCityMaterials.Surface("armatura — chrom polerowany", new Color(.88f, .89f, .9f), .08f, 1f);
        grafit = BraxCityMaterials.Surface("wulkanizacja: lakier proszkowy grafit", new Color(.17f, .18f, .2f), .45f, .3f);
        czerwien = BraxCityMaterials.Surface("wulkanizacja: lakier proszkowy czerwony", new Color(.70f, .07f, .07f), .35f, .25f);
        czern = BraxCityMaterials.Surface("wulkanizacja: stal czarna", new Color(.05f, .05f, .055f), .5f, .5f);
        bialy = BraxMiastoA.Swieci("wulkanizacja: litery BRAXHORN", Color.white, new Color(.6f, .6f, .6f));
        ekran = BraxMiastoA.Swieci("wulkanizacja: ekran wyważarki", Color.white, new Color(.9f, .9f, .9f));
        // ekran wyważarki (z kodu): granat, dwa okna odczytu L / R z gramami, pasek „OK” na zielono, napis WYWAŻANIE
        const int W = 256, H = 144; var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(.03f, .07f, .16f);
        foreach (int ox in new[] { 14, 134 }) for (int y = 38; y < 100; y++) for (int x = ox; x < ox + 108; x++) px[y * W + x] = (y < 40 || y > 97 || x < ox + 2 || x > ox + 105) ? new Color(.35f, .6f, 1f) : new Color(.02f, .04f, .1f);
        for (int y = 8; y < 28; y++) for (int x = 88; x < 168; x++) px[y * W + x] = new Color(.1f, .65f, .25f);
        BraxChlodnie.TekstAtlasu(px, W, 14, 0, 108, H, "0", 50f, 40f, Color.white);                       // odczyt lewej płaszczyzny (g)
        BraxChlodnie.TekstAtlasu(px, W, 134, 0, 108, H, "5", 50f, 40f, new Color(1f, .8f, .2f));          // odczyt prawej (g)
        BraxChlodnie.TekstAtlasu(px, W, 14, 0, 108, H, "LEWA · G", 104f, 11f, new Color(.7f, .8f, 1f));
        BraxChlodnie.TekstAtlasu(px, W, 134, 0, 108, H, "PRAWA · G", 104f, 11f, new Color(.7f, .8f, 1f));
        BraxChlodnie.TekstAtlasu(px, W, 88, 0, 80, H, "OK", 12f, 12f, Color.white);
        BraxChlodnie.TekstAtlasu(px, W, 0, 0, W, H, "WYWAŻANIE", 122f, 16f, Color.white);
        var tx = new Texture2D(W, H, TextureFormat.RGB24, true); tx.SetPixels(px); tx.Apply();
        string kat = BraxRegion.Out + "/Wulkanizacja"; System.IO.Directory.CreateDirectory(kat); string path = kat + "/ekran_wywazarki.png";
        System.IO.File.WriteAllBytes(path, tx.EncodeToPNG()); Object.DestroyImmediate(tx);
        UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
        if (UnityEditor.AssetImporter.GetAtPath(path) is UnityEditor.TextureImporter imp) { imp.sRGBTexture = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 256; imp.SaveAndReimport(); }
        var t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (t) { ekran.SetTexture("_MainTex", t); if (ekran.HasProperty("_EmissionMap")) ekran.SetTexture("_EmissionMap", t); UnityEditor.EditorUtility.SetDirty(ekran); }
    }
    static Color32 C(float g) => new Color32((byte)(g * 255), (byte)(g * 255), (byte)(g * 255), 255);

    // ------------------------------------------------------------------ koło (oś = lokalne +y, środek w 0, lico felgi w +y)
    public const float R = .316f, Sz = .205f;            // promień zewnętrzny i szerokość opony (m rzeczywiste)
    const float RF = .203f;                               // promień osadzenia (16")
    static void BudujKolo(BraxChlodnie.Partie p, int wzor, Material felga)
    {
        var bl = new[] { C(1f) };
        // opona: od stopki dolnej po zewnętrzu do stopki górnej; bieżnik z 4 rowkami
        var pr = new List<Vector3>();
        void A(float r, float y) => pr.Add(new Vector3(r, y, 0));
        float h = Sz * .5f;
        A(RF + .004f, -h + .012f); A(.245f, -h); A(.285f, -h + .004f); A(.305f, -h + .016f); A(R, -h + .04f);
        float[] rowki = { -.06f, -.02f, .02f, .06f };
        foreach (float yr in rowki) { A(R, yr - .006f); A(R - .009f, yr - .005f); A(R - .009f, yr + .005f); A(R, yr + .006f); }
        A(R, h - .04f); A(.305f, h - .016f); A(.285f, h - .004f); A(.245f, h); A(RF + .004f, h - .012f);
        BraxChlodnie.Toczony(p[guma], Vector3.zero, pr, bl, 36);
        // felga: kołnierz zewnętrzny (lico), bęben do kołnierza tylnego
        var pf = new List<Vector3>();
        void F(float r, float y) => pf.Add(new Vector3(r, y, 0));
        F(RF + .002f, h - .022f); F(.214f, h - .016f); F(.212f, h - .006f); F(.19f, h - .008f); F(.176f, h - .022f);   // od zewnątrz do środka — lico do +y
        BraxChlodnie.Toczony(p[felga], Vector3.zero, pf, bl, 36);
        var pb = new List<Vector3>();   // bęben (wewnątrz, normalne do osi) i kołnierz tylny
        pb.Add(new Vector3(.176f, h - .022f, 0)); pb.Add(new Vector3(.172f, h - .04f, 0)); pb.Add(new Vector3(.172f, -h + .03f, 0)); pb.Add(new Vector3(.205f, -h + .012f, 0));
        BraxChlodnie.Toczony(p[felga], Vector3.zero, pb, bl, 36);
        p[czern].kolor = C(.12f); p[czern].Kolo(new Vector3(0f, -.03f, 0f), .172f, 36); p[czern].kolor = C(1f);   // tarcza w głębi
        // szprychy: od piasty (r .055) do bębna (r .174), lico lekko wklęsłe (od y .078 przy piaście do .084 przy kołnierzu)
        int n = wzor == 1 ? 10 : wzor == 2 ? 6 : 5;
        var sp = p[felga];
        void Szprycha(float kat, float wPiasta, float wObr)
        {
            var u = new Vector3(Mathf.Cos(kat), 0f, Mathf.Sin(kat)); var t = new Vector3(-u.z, 0f, u.x);
            float r0 = .052f, r1 = .175f, yA0 = .074f, yA1 = .082f, yB = .045f;
            Vector3 a0 = u * r0 - t * wPiasta, a1 = u * r0 + t * wPiasta, b0 = u * r1 - t * wObr, b1 = u * r1 + t * wObr;
            Vector3 Up(float y) => Vector3.up * y;
            sp.Quad(a0 + Up(yA0), b0 + Up(yA1), b1 + Up(yA1), a1 + Up(yA0), Vector3.up);                         // lico
            sp.Quad(a0 + Up(yB), b0 + Up(yB), b0 + Up(yA1), a0 + Up(yA0), -t);                                   // boki
            sp.Quad(a1 + Up(yB), b1 + Up(yB), b1 + Up(yA1), a1 + Up(yA0), t);
        }
        for (int i = 0; i < n; i++)
        {
            float k = i * Mathf.PI * 2f / n;
            if (wzor == 0) { Szprycha(k - .1f, .011f, .009f); Szprycha(k + .1f, .011f, .009f); }       // 5 podwójnych
            else if (wzor == 1) Szprycha(k, .009f, .007f);                                                 // 10 cienkich
            else { Szprycha(k, .016f, .008f); Szprycha(k - .16f, .004f, .009f); Szprycha(k + .16f, .004f, .009f); }   // 6 Y
        }
        // piasta z dekielkiem, 5 śrub, wentyl
        var ph = new List<Vector3> { new Vector3(.058f, .045f, 0), new Vector3(.056f, .076f, 0), new Vector3(.034f, .082f, 0), new Vector3(.028f, .086f, 0), new Vector3(0f, .086f, 0) };
        BraxChlodnie.Toczony(p[felga], Vector3.zero, ph, bl, 24);
        for (int i = 0; i < 5; i++) { float k = i * Mathf.PI * 2f / 5f + .3f; var c0 = new Vector3(Mathf.Cos(k) * .043f, .078f, Mathf.Sin(k) * .043f); p[chrom].Rura(c0, c0 + Vector3.up * .012f, .0065f, 6); }
        p[czern].Rura(new Vector3(.19f, h - .02f, 0f), new Vector3(.19f, h + .006f, .0f), .0035f, 6);
    }

    /// <summary>Koło (opona + felga) — obiekt: środek, oś (pionowa = leży), wariant 0–8 (wzór × kolor).</summary>
    public static Transform Kolo(Transform rodzic, string nazwa, Vector3 srodek, Vector3 os, int wariant, float obrot = 0f)
    {
        Materialy();
        int wz = wariant % 3, kol = (wariant / 3) % 3; var fm = kol == 0 ? alu : kol == 1 ? aluGrafit : aluCzarny;
        var w = BraxChlodnie.Wez("kolo_" + wz + "_" + kol, q => BudujKolo(q, wz, fm));
        var g = BraxChlodnie.Wstaw(rodzic, "Koło (opona 205/55 R16 + felga) · " + nazwa, w, Vector3.zero, true);
        g.position = srodek; g.rotation = Quaternion.FromToRotation(Vector3.up, os.normalized) * Quaternion.Euler(0f, obrot, 0f);
        return g;
    }

    // ------------------------------------------------------------------ montażownica (przód = lokalne +z)
    static void BudujMontazownice(BraxChlodnie.Partie p)
    {
        p[grafit].Poducha(new Vector3(-.40f, 0f, -.45f), new Vector3(.40f, .78f, .45f), .03f, 0f, 4);                   // korpus
        p[czern].Box(new Vector3(-.36f, .76f, -.41f), new Vector3(.36f, .80f, .41f), 4);                                 // płyta górna (ciemna)
        var bl = new[] { C(1f) };
        BraxChlodnie.Toczony(p[czern], Vector3.zero, new List<Vector3> { new Vector3(.30f, .80f, 0), new Vector3(.30f, .82f, 0), new Vector3(.29f, .83f, 0), new Vector3(0f, .83f, 0) }, bl, 32);   // talerz (wierzch do góry)
        for (int i = 0; i < 4; i++)
        {   // szczęki radialne (czerwone) — osiowe co 90°
            float k = i * 90f * Mathf.Deg2Rad; var u = new Vector3(Mathf.Cos(k), 0f, Mathf.Sin(k)); var t = new Vector3(-u.z, 0f, u.x);
            var lo = u * .17f - t * .03f + Vector3.up * .83f; var hi = u * .27f + t * .03f + Vector3.up * .88f;
            p[czerwien].Poducha(Vector3.Min(lo, hi), Vector3.Max(lo, hi), .006f, 0f, 3);
        }
        p[chrom].Rura(new Vector3(0f, .83f, 0f), new Vector3(0f, .90f, 0f), .028f, 12);
        // słup odchylny, ramię, trzpień z głowicą
        p[czerwien].Poducha(new Vector3(-.09f, .80f, -.56f), new Vector3(.09f, 1.78f, -.38f), .02f, 0f, 3);
        p[czerwien].Poducha(new Vector3(-.065f, 1.64f, -.40f), new Vector3(.065f, 1.76f, -.12f), .015f, 0f, 3);
        p[chrom].Rura(new Vector3(0f, 1.64f, -.20f), new Vector3(0f, 1.17f, -.20f), .024f, 12);                          // trzpień nad kołnierzem felgi
        p[czern].Poducha(new Vector3(-.035f, 1.115f, -.25f), new Vector3(.035f, 1.17f, -.15f), .01f, 0f, 3);              // głowica montażowa
        p[czern].Rura(new Vector3(.065f, 1.70f, -.20f), new Vector3(.16f, 1.70f, -.20f), .012f, 8);                       // dźwignia blokady
        // zbijak stopki (prawy bok): odbój gumowy, ramię z łopatką
        p[guma].Poducha(new Vector3(.40f, .12f, -.12f), new Vector3(.47f, .48f, .22f), .02f, 0f, 3);
        p[czerwien].Rura(new Vector3(.42f, .55f, -.40f), new Vector3(.62f, .62f, .05f), .03f, 10);
        p[czern].Poducha(new Vector3(.60f, .30f, .02f), new Vector3(.66f, .66f, .14f), .01f, 0f, 3);
        // 3 pedały z przodu, manometr z wężem (lewy bok), napis BRAXHORN na froncie
        for (int i = 0; i < 3; i++) p[czern].Poducha(new Vector3(-.2f + i * .14f, .015f, .44f), new Vector3(-.1f + i * .14f, .055f, .6f), .008f, 0f, 3);
        p[chrom].Rura(new Vector3(-.40f, .95f, .25f), new Vector3(-.43f, .95f, .25f), .04f, 16);                          // manometr pompowania
        p[czern].Rura(new Vector3(-.43f, .93f, .25f), new Vector3(-.45f, .4f, .3f), .007f, 6); p[czern].Rura(new Vector3(-.45f, .4f, .3f), new Vector3(-.44f, .05f, .5f), .007f, 6);
        BraxLiteryBebas.Napis(p[bialy], p[czern], "BRAXHORN", new Vector3(0f, .58f, .45f), Vector3.left, Vector3.up, Vector3.forward, .06f, .012f, .05f, .004f);
    }

    /// <summary>Montażownica do opon z kołem na talerzu: p = środek podstawy na posadzce, przod = strona operatora.</summary>
    public static Transform Montazownica(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int wariantKola)
    {
        Materialy();
        var t = BraxChlodnie.Korzen(rodzic, "Montażownica do opon · " + nazwa, p, przod);
        BraxChlodnie.Wstaw(t, "montażownica", BraxChlodnie.Wez("montazownica", BudujMontazownice), Vector3.zero, true);
        Kolo(t, "na montażownicy", p + Vector3.up * (.90f + Sz * .5f) * BraxClaudeOsiedle.K, Vector3.up, wariantKola, 15f);
        BraxChlodnie.Kolizja(t, "montażownica", new Vector3(-.45f, 0f, -.58f), new Vector3(.68f, 1.8f, .62f));
        return t;
    }

    // ------------------------------------------------------------------ wyważarka (przód = lokalne +z, wałek w +x)
    static void BudujWywazarke(BraxChlodnie.Partie p)
    {
        p[grafit].Poducha(new Vector3(-.32f, 0f, -.40f), new Vector3(.32f, .92f, .40f), .03f, 0f, 4);                     // szafka
        // taca na ciężarki: obrzeże i przegródki (czarne), ciężarki (szare bryłki)
        foreach (var (a, b) in new[] { (new Vector3(-.30f, .92f, -.10f), new Vector3(.30f, .945f, -.09f)), (new Vector3(-.30f, .92f, .37f), new Vector3(.30f, .945f, .38f)),
                                       (new Vector3(-.30f, .92f, -.09f), new Vector3(-.29f, .945f, .37f)), (new Vector3(.29f, .92f, -.09f), new Vector3(.30f, .945f, .37f)),
                                       (new Vector3(-.10f, .92f, -.09f), new Vector3(-.09f, .945f, .37f)), (new Vector3(.09f, .92f, -.09f), new Vector3(.10f, .945f, .37f)) })
            p[czern].Box(a, b);
        var cz = p[chrom];
        for (int i = 0; i < 9; i++) { float x = -.26f + (i % 3) * .19f + (i / 3) * .03f, z = -.04f + (i / 3) * .13f; cz.Box(new Vector3(x, .92f, z), new Vector3(x + .045f, .937f, z + .02f)); }
        // wałek ze stożkiem i nakrętką (koło między .40 a .60)
        var bl = new[] { C(1f) };
        cz.Rura(new Vector3(.32f, .70f, 0f), new Vector3(.68f, .70f, 0f), .02f, 12);
        cz.Rura(new Vector3(.66f, .70f, 0f), new Vector3(.72f, .70f, 0f), .045f, 12);
        p[czern].Rura(new Vector3(.72f, .70f, 0f), new Vector3(.74f, .70f, 0f), .06f, 6);
        // osłona koła podniesiona: półwalec za i nad kołem (oś wzdłuż x, promień .42, od 70° do 215°)
        int N = 16; float rO = .42f, ya = .70f;
        for (int i = 0; i < N; i++)
        {
            float a0 = (70f + i * 145f / N) * Mathf.Deg2Rad, a1 = (70f + (i + 1) * 145f / N) * Mathf.Deg2Rad;
            Vector3 P(float a, float x) => new Vector3(x, ya + Mathf.Sin(a) * rO, Mathf.Cos(a) * rO);   // 90° = nad kołem, 180° = za nim
            var nn = new Vector3(0f, Mathf.Sin((a0 + a1) * .5f), Mathf.Cos((a0 + a1) * .5f));
            p[grafit].Quad(P(a0, .34f), P(a1, .34f), P(a1, .66f), P(a0, .66f), nn); p[grafit].Quad(P(a0, .66f), P(a1, .66f), P(a1, .34f), P(a0, .34f), -nn);
        }
        // konsola z ekranem (z tyłu u góry): słupek, obudowa bez przedniej ściany, ramka, ekran
        p[grafit].Poducha(new Vector3(-.08f, .92f, -.38f), new Vector3(.08f, 1.14f, -.30f), .01f, 0f, 3);
        p[czern].Box(new Vector3(-.22f, 1.14f, -.40f), new Vector3(.22f, 1.42f, -.32f), 32);
        foreach (var (a, b) in new[] { (new Vector3(-.22f, 1.39f, -.32f), new Vector3(.22f, 1.42f, -.30f)), (new Vector3(-.22f, 1.14f, -.32f), new Vector3(.22f, 1.17f, -.30f)),
                                       (new Vector3(-.22f, 1.17f, -.32f), new Vector3(-.19f, 1.39f, -.30f)), (new Vector3(.19f, 1.17f, -.32f), new Vector3(.22f, 1.39f, -.30f)) })
            p[czern].Box(a, b);
        int ke = p[ekran].v.Count;
        p[ekran].Quad(new Vector3(.19f, 1.17f, -.30f), new Vector3(-.19f, 1.17f, -.30f), new Vector3(-.19f, 1.39f, -.30f), new Vector3(.19f, 1.39f, -.30f), Vector3.forward);
        p[ekran].uv[ke] = new Vector2(0f, 0f); p[ekran].uv[ke + 1] = new Vector2(1f, 0f); p[ekran].uv[ke + 2] = new Vector2(1f, 1f); p[ekran].uv[ke + 3] = new Vector2(0f, 1f);
        // ramię pomiarowe (bok przy wałku)
        p[czern].Rura(new Vector3(.32f, .48f, .12f), new Vector3(.42f, .5f, .12f), .01f, 6);
    }

    /// <summary>Wyważarka z kołem na wałku: p = środek podstawy, przod = strona operatora (wałek po prawej operatora? — lokalne +x).</summary>
    public static Transform Wywazarka(Transform rodzic, string nazwa, Vector3 p, Vector3 przod, int wariantKola)
    {
        Materialy();
        var t = BraxChlodnie.Korzen(rodzic, "Wyważarka · " + nazwa, p, przod);
        BraxChlodnie.Wstaw(t, "wyważarka", BraxChlodnie.Wez("wywazarka", BudujWywazarke), Vector3.zero, true);
        float k = BraxClaudeOsiedle.K;
        Kolo(t, "na wyważarce", t.position + t.rotation * (new Vector3(.50f, .70f, 0f) * k), t.rotation * Vector3.right, wariantKola, 0f);
        BraxChlodnie.Kolizja(t, "wyważarka", new Vector3(-.35f, 0f, -.42f), new Vector3(.75f, 1.45f, .42f));
        return t;
    }

    // ------------------------------------------------------------------ podnośnik „żaba” (lewarek warsztatowy na kółkach), przód = +z
    static void BudujZabe(BraxChlodnie.Partie p)
    {
        p[czerwien].Poducha(new Vector3(-.13f, .07f, -.55f), new Vector3(.13f, .19f, .30f), .025f, 0f, 3);              // korpus
        foreach (float x in new[] { -.15f, .15f })
        {
            p[czern].Rura(new Vector3(x - .025f * Mathf.Sign(x), .045f, .22f), new Vector3(x + .01f * Mathf.Sign(x), .045f, .22f), .045f, 12);   // koła przednie
            p[czern].Rura(new Vector3(x * .8f, .07f, -.46f), new Vector3(x * .8f, .02f, -.46f), .008f, 6);                                    // widelec
            p[czern].Rura(new Vector3(x * .8f - .015f, .03f, -.46f), new Vector3(x * .8f + .015f, .03f, -.46f), .03f, 10);                   // kółko skrętne
        }
        p[czern].Poducha(new Vector3(-.05f, .19f, .0f), new Vector3(.05f, .235f, .32f), .01f, 0f, 3);                    // ramię
        var bl = new[] { C(1f) };
        BraxChlodnie.Toczony(p[czern], new Vector3(0f, .235f, .30f), new List<Vector3> { new Vector3(.06f, 0f, 0), new Vector3(.06f, .02f, 0), new Vector3(.045f, .025f, 0), new Vector3(0f, .025f, 0) }, bl, 16);   // siodło
        p[chrom].Rura(new Vector3(0f, .17f, -.55f), new Vector3(0f, .82f, -1.05f), .016f, 10);                          // dyszel
        p[guma].Rura(new Vector3(0f, .82f, -1.05f), new Vector3(0f, .92f, -1.13f), .02f, 10);                            // rękojeść
    }

    /// <summary>Podnośnik „żaba” przy aucie: p = środek korpusu na posadzce, przod = kierunek siodła (dyszel do tyłu).</summary>
    public static Transform Zaba(Transform rodzic, string nazwa, Vector3 p, Vector3 przod)
    {
        Materialy();
        var t = BraxChlodnie.Korzen(rodzic, "Podnośnik żaba · " + nazwa, p, przod);
        BraxChlodnie.Wstaw(t, "żaba", BraxChlodnie.Wez("zaba", BudujZabe), Vector3.zero, true);
        BraxChlodnie.Kolizja(t, "żaba", new Vector3(-.17f, 0f, -.6f), new Vector3(.17f, .26f, .36f));
        return t;
    }
}
