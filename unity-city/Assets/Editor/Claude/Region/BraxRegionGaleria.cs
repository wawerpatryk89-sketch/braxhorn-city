using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — etap 1.6 „GALERIA” (plan właściciela 31.10, zgłoszenia 2.11 p. 27–31: „gdzieś miała być ULICA W LEWO i miała być
// GALERIA wybudowana — nic nie widzę”, „całe miasto za małe”). Od zera, na terenie, gdzie dotąd było „lustro” (dolna połowa nieba —
// brak gruntu) i resztki oryginału:
//   • ul. KOLEJOWA pod wiaduktem (słupy wiaduktu na liniach krawężników — jak ulica pod koleją nadziemną): jezdnia, chodniki z płyt,
//     krawężniki, kratki, latarnie, drzewa w kratach, ławki, kosze, słupki, zebry pod łukami, skrzyżowanie T, zawrotka z barierką;
//   • GALERIA BRAXHORN (2 kondygnacje, pasaż, atrium ze szklanym dachem, kładki, schody, ruchome schody, fontanna, toalety, zaplecze
//     z rampą, wejście główne od wiaduktu i drugie od północy) — BraxRegionGaleriaBryla.cs, lokale — BraxRegionGaleriaLokale.cs;
//   • pierzeja po drugiej stronie ulicy (barber, kebab, serwis telefonów, LOMBARD z wnętrzem, pralnia) — BraxRegionGaleriaPierzeja.cs;
//   • parking, plac ustawek 3 na 3 (plac manewrowy przy rampie), ścieżka pd., trawniki do skraju lasu;
//   • BAR 24 przy ulicy głównej: stary budynek rozebrany, nowy z wnętrzem — BraxRegionGaleriaBar24.cs.
// Współrzędne PRAWDZIWE (sprawdzone w kodzie sceny) — scratchpad/plan/GALERIA_PLAN.md. Skala: k = BraxClaudeOsiedle.K (Brax 2.33 m).
// Zasada 2.11 11:00: „zawsze ściągasz, kładziesz nową, a nie na to” — najpierw sprzątanie terenu (stare siatki oryginału wycięte,
// stare drzewa/krzewy/latarnie z wcześniejszych kroków usunięte, stary Bar 24 rozebrany), potem budowa.
// Budżet: galeria + pierzeja pod osobnym korzeniem → osobna scena doczytywana (BraxhornCity_KGaleria, hak w BraxGymBuild);
// w scenie głównej grunt, ulica i lekkie bryły „z daleka” (BraxZastepstwoObszaru). Siatki scalane per grupa i materiał, zapisane
// jako pliki (Assets/PlayableGenerated/ClaudeRegion/Galeria/Meshes). Nic nie świeci (lampy = światła punktowe ForceVertex).
// Wnętrza: kanał „słońce/AO” shadera CityStaticPBR (UV3, znacznik 2718) — pod dachem słońce = 0, więc wnętrza nie są oświetlone
// słońcem jak plac; pod wiaduktem cień.
public static partial class BraxRegionGaleria
{
    public static float K => BraxClaudeOsiedle.K;
    public const string Dir = BraxRegion.Out + "/Galeria";
    public const string NazwaObszaru = "CLAUDE galeria BRAXHORN (obszar)";
    public const string NazwaUlicy = "CLAUDE ulica Kolejowa i otoczenie galerii";
    public const string ScenaObszaru = "BraxhornCity_KGaleria";

    // ---------------- poziomy (y) ----------------
    const float YJ = .02f;     // jezdnia
    const float YC = .17f;     // chodnik (krawężnik 15 cm)
    const float YG = .20f;     // posadzka parteru galerii
    const float YP = 6.65f;    // posadzka piętra
    const float YD = 13.10f;   // wierzch stropodachu

    // ---------------- ul. Kolejowa (na wschód od wiaduktu; wiadukt = wykonawca „tyły”, lica x −70.95 i −61.45) ----------------
    const float WiaduktX0 = -70.95f, WiaduktX1 = -61.45f;
    const float KJX0 = -59.0f, KJX1 = -52.6f;          // jezdnia Kolejowej (6.4 m), krawężniki 0.2 m
    const float KCX1 = -47.5f;                         // chodnik wsch. do lica pierzei
    const float KZ0 = 50.5f, KZ1 = 109.5f;              // jezdnia od pd. (wjazd w łuk wiaduktu z 51.7…62.3) do zawrotki
    const float ZawX = -55.4f, ZawZ = 115.0f, ZawR = 6.0f;   // zawrotka (koło) na końcu, za nią barierka i skraj lasu
    // ---------------- galeria (zach. od wiaduktu; na zach. ulica S1 dzielnicy, chodnik do x −111.5) ----------------
    const float GX0 = -111.5f, GX1 = -74.5f, GZ0 = 51f, GZ1 = 100f;
    // ---------------- dojazd, parking, plac ustawek (pn. od galerii) ----------------
    const float DZ0 = 103f, DZ1 = 110f;                 // droga dojazdowa od ulicy S1 (x −111.5) do parkingu
    const float PZ1 = 136.5f;                           // pn. krawędź parkingu / placu

    static Transform cityRoot, ulica, obszar, bryly;
    static BraxCityWorld swiat;
    static int nObiektow, nLamp, nDrzew, nSiedzisk, nUsunietych, nWycietych;
    static readonly List<string> usuniete = new List<string>();

    public static void Apply(BraxCityWorld world, Transform root)
    {
        cityRoot = root; swiat = world;
        nObiektow = nLamp = nDrzew = nSiedzisk = nUsunietych = nWycietych = 0; usuniete.Clear(); Miejsca.Clear();
        grupy.Clear(); kolejnosc.Clear(); metry.Clear(); tekstury.Clear(); matCache.Clear(); siatekZapisanych = 0;
        Directory.CreateDirectory(Dir + "/Meshes"); Directory.CreateDirectory(Dir + "/Tex"); Directory.CreateDirectory(Dir + "/Materials");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Materialy();
        // 1) sprzątanie terenu (zasada 2.11 11:00)
        Krok("sprzątanie terenu (stare siatki, zieleń i latarnie z wcześniejszych kroków)", Sprzatanie);
        Physics.SyncTransforms();
        ulica = BraxRegion.Root(root, NazwaUlicy);
        obszar = BraxRegion.Root(root, NazwaObszaru);
        bryly = new GameObject("Bryły z daleka (gdy obszar galerii nie jest wczytany)").transform; bryly.SetParent(ulica, false);
        // 2) budowa
        Krok("ul. Kolejowa: grunt, jezdnia, chodniki, krawężniki, zebry, zawrotka", UlicaKolejowa);
        Krok("parking, plac ustawek, ścieżka, trawniki", ParkingIPlac);
        Krok("galeria: bryła, elewacje, stropy, atrium, kładki, schody", BudujBryle);
        Krok("galeria: lokale czynne i zamknięte", BudujLokale);
        Krok("pierzeja wsch. z lombardem", BudujPierzeje);
        Krok("ulica: latarnie, drzewa w kratach, ławki, kosze, słupki", MalaArchitektura);
        Krok("bryły z daleka (zastępstwo obszaru)", BrylyZDaleka);
        Krok("BAR 24: nowy budynek z wnętrzem", BudujBar24);
        Krok("miejsca obsady, klientów i kibiców", ZapiszMiejsca);
        Krok("punkty świata i rejestr ulicy", PunktySwiata);
        Flush();
        Krok("utrwalenie siatek galerii", Utrwal);
        Debug.Log("BRAXHORN_GALERIA zbudowana: obiektów " + nObiektow + ", lamp " + nLamp + ", drzew " + nDrzew + ", siedzisk " + nSiedzisk +
                  ", miejsc ludzi " + Miejsca.Count + ", siatek zapisanych " + siatekZapisanych + ", usuniętych starych obiektów " + nUsunietych +
                  ", wyciętych siatek oryginału " + nWycietych + ", k = " + K.ToString("F3"));
        if (usuniete.Count > 0) Debug.Log("BRAXHORN_GALERIA_USUNIETE " + string.Join(" | ", usuniete.Take(80)));
    }

    static void Krok(string co, Action a)
    {
        try { a(); BraxRegion.Note("galeria: " + co + ": OK"); }
        catch (Exception e) { Debug.LogError("BRAXHORN_GALERIA " + co + ": " + e); BraxRegion.Note("galeria: " + co + ": BŁĄD " + e.Message); }
    }

    // ======================================================================== SPRZĄTANIE
    // Prostokąty terenu (x0, x1, z0, z1): wszystko, co tu stało przed tym krokiem, idzie do usunięcia / wycięcia.
    static readonly Rect[] Teren =
    {
        Rect.MinMaxRect(-111.5f, 48f, WiaduktX0, 140f),   // galeria, ścieżka pd., dojazd, parking, plac (zach. od wiaduktu, wsch. od dzielnicy)
        Rect.MinMaxRect(WiaduktX1, 48f, -33f, 126f),      // ul. Kolejowa, pierzeja, zaułek, zawrotka
    };
    static bool WTerenie(Vector3 p) { foreach (var r in Teren) if (r.Contains(new Vector2(p.x, p.z))) return true; return false; }

    static void Sprzatanie()
    {
        // a) siatki ORYGINAŁU (lustro, łąka, kostka, stare bryły) — wycięcie trójkątów w prostopadłościanach terenu
        var src = BraxClaudeGround.SourceCity;
        if (src)
            foreach (var r in Teren)
            {
                var v = new Bounds(); v.SetMinMax(new Vector3(r.xMin, -3f, r.yMin), new Vector3(r.xMax, 80f, r.yMax));
                nWycietych += BraxCityMeshCut.Subtract(src, v, Dir + "/Cuts", "galeria_teren");
                if (BraxClaudeGround.SourcePhysics)
                {
                    // kolizje oryginału nad podłogą (podłoga y ≤ 0 zostaje jako zabezpieczenie)
                    var vk = new Bounds(); vk.SetMinMax(new Vector3(r.xMin, .05f, r.yMin), new Vector3(r.xMax, 80f, r.yMax));
                    BraxCityMeshCut.SubtractBoxColliders(BraxClaudeGround.SourcePhysics, vk);
                }
            }
        // b) obiekty dołożone wcześniej (drzewa, krzewy, kępy, kamienie, ławki, latarnie, kosze) w terenie
        foreach (var nazwa in new[] { "CLAUDE zieleń naturalna i mała architektura", "CLAUDE drzewa zamiast kul na patykach", "CLAUDE prawdziwe drzewa" })
        {
            var t = Szukaj(cityRoot, nazwa); if (!t) continue;
            var doUsuniecia = new List<GameObject>();
            foreach (Transform c in t)
            {
                Vector3 p = c.position;
                var rs = c.GetComponentsInChildren<Renderer>(true);
                if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); p = b.center; }
                if (WTerenie(p)) doUsuniecia.Add(c.gameObject);
            }
            foreach (var g in doUsuniecia) { usuniete.Add(g.name + " " + BraxKontrola.F(g.transform.position)); UnityEngine.Object.DestroyImmediate(g); nUsunietych++; }
        }
        // c) trawnik miasta („CLAUDE trawa*”) i stare płyty na terenie — po rendererach pod korzeniem renowacji, poza wiaduktem,
        //    ulicą główną i pierzejami (te mają swoich wykonawców)
        var chron = new[] { "Riveted steel viaduct", "Street pavement and dry asphalt", "Building ", "High-rise", "BRAXHORN CITY", NazwaUlicy, NazwaObszaru, "CLAUDE tyły" };
        var stare = new List<GameObject>();
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!r) continue;
            bool ok = false; for (var p = r.transform; p && p != cityRoot; p = p.parent) if (chron.Any(c => p.name.StartsWith(c))) { ok = true; break; }
            if (ok) continue;
            var b = r.bounds;
            if (b.size.y > 1.5f || b.size.x > 60f || b.size.z > 60f) continue;                 // tylko płyty i drobnica (nie bryły)
            if (!WTerenie(b.center) || b.center.y > 3f) continue;
            if (!BraxRegion.IsGrassRenderer(r) && b.size.y > .4f) continue;
            stare.Add(r.gameObject);
        }
        foreach (var g in stare.Distinct()) { if (!g) continue; usuniete.Add(g.name + " " + BraxKontrola.F(g.transform.position)); UnityEngine.Object.DestroyImmediate(g); nUsunietych++; }
        BraxRegion.Note("galeria: sprzątanie — wycięte siatki oryginału " + nWycietych + ", usunięte obiekty " + nUsunietych);
    }

    static Transform Szukaj(Transform t, string nazwa)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (c.name == nazwa) return c;
        return null;
    }

    // ======================================================================== MATERIAŁY
    static readonly Dictionary<Material, float> metry = new Dictionary<Material, float>();
    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    static readonly string[] Korzenie = { BraxRegion.PH + "textures/", "Assets/Source/Claude/Osiedle/textures/" };

    static bool Plik(string slug, out string d, out string n, out string a)
    {
        foreach (var k in Korzenie)
        {
            d = k + slug + "/diffuse.jpg"; n = k + slug + "/normal.jpg"; a = k + slug + "/arm.jpg";
            if (File.Exists(d)) return true;
        }
        string cr = "Assets/Source/CityRebuild/textures/" + slug + "/2k/";
        d = cr + slug + "_diff_2k.jpg"; n = cr + slug + "_nor_gl_2k.png"; a = cr + slug + "_arm_2k.jpg";
        return File.Exists(d);
    }
    static Texture2D T(string path) { if (!File.Exists(path)) return null; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); return AssetDatabase.LoadAssetAtPath<Texture2D>(path); }

    /// <summary>Materiał z tekstur (pierwszy dostępny slug). metres = metrów na powtórzenie. chropowatosc &gt; 0 = bez mapy ARM,
    /// stała chropowatość (posadzki: ≥ 0.5 — nie lustro). Brak tekstur = materiał gładki w kolorze zapasowym.</summary>
    static Material Tex(string nazwa, float metres, Color tint, float chropowatosc, Color zapas, params string[] slugi)
    {
        string key = "t|" + nazwa;
        if (matCache.TryGetValue(key, out var c) && c) return c;
        Material m = null;
        foreach (var s in slugi)
        {
            if (!Plik(s, out var d, out var n, out var a)) continue;
            var td = T(d); if (!td) continue;
            m = BraxCityMaterials.Textured("Galeria: " + nazwa + " [" + s + "]", td, T(n), chropowatosc > 0 ? null : T(a), 1f / Mathf.Max(.05f, metres));
            m.SetColor("_Color", tint);
            if (chropowatosc > 0) { m.SetFloat("_Roughness", chropowatosc); m.SetFloat("_Metallic", 0f); m.SetFloat("_UseARMForAO", 0f); }
            EditorUtility.SetDirty(m);
            break;
        }
        if (!m) { m = Gladki(nazwa + " (bez tekstury)", zapas, chropowatosc > 0 ? chropowatosc : .8f); BraxRegion.Note("galeria: brak tekstur " + string.Join("/", slugi) + " → kolor"); }
        else metry[m] = 1f;   // UV siatek w metrach, powtórzenie w materiale
        matCache[key] = m; return m;
    }
    static Material Gladki(string nazwa, Color c, float chrop = .8f, float metal = 0f)
    {
        string key = "g|" + nazwa;
        if (matCache.TryGetValue(key, out var m) && m) return m;
        m = BraxCityMaterials.Surface("Galeria: " + nazwa, c, chrop, metal);
        matCache[key] = m; return m;
    }

    // paleta
    static Material mAsfalt, mPlyty, mPlytyCiemne, mKraweznik, mGranit, mTrawa, mBeton, mFarbaBiala, mFarbaZolta, mZeliwo, mStalGrafit, mStalSzczotk,
                    mSzklo, mSzkloDach, mSzkloCiemne, mPosadzka, mPosadzkaJasna, mTynk, mTynkCieply, mSufit, mOkladzina, mOkladzinaCiemna, mKamienCokol,
                    mDrewno, mDrewnoJasne, mMembrana, mGuma, mCegla, mCeglaCiemna, mWoda, mLustro, mChrom, mCzern, mBiel, mTkanina, mSkora,
                    mKafelBialy, mKafelSzary, mLaminat, mMarmur, mBetonPolerowany, mKostka;

    static void Materialy()
    {
        mAsfalt = Tex("asfalt jezdni", 3.5f, new Color(.62f, .61f, .60f), .9f, new Color(.18f, .18f, .19f), "asphalt_04", "clean_asphalt", "asphalt_02");
        mPlyty = Tex("chodnik z dużych płyt betonowych", 1.6f, new Color(.86f, .85f, .82f), .88f, new Color(.6f, .6f, .58f), "concrete_pavers_02", "concrete_pavement_02", "concrete_pavers_03");
        mPlytyCiemne = Tex("pas płyt grafitowych", 1.2f, new Color(.55f, .55f, .56f), .86f, new Color(.4f, .4f, .41f), "concrete_pavers_03", "concrete_pavers_02");
        mKostka = Tex("kostka granitowa (plac pod łukami)", 1.4f, new Color(.8f, .78f, .75f), .9f, new Color(.5f, .5f, .48f), "granite_tile_02", "herringbone_brick_02", "square_tiles");
        mKraweznik = Gladki("krawężnik granitowy", new Color(.47f, .46f, .44f), .92f);
        mGranit = Tex("granit płomieniowany", 1.2f, new Color(.78f, .77f, .75f), .78f, new Color(.5f, .5f, .5f), "granite_tile_02", "granite_tile");
        mTrawa = AssetDatabase.LoadAssetAtPath<Material>("Assets/PlayableGenerated/ClaudeGround/Ziemia__prawdziwa_trawa_Grass004.mat");
        if (!mTrawa) { try { mTrawa = BraxClaudeGround.Grass(); } catch (Exception) { mTrawa = Gladki("trawa", new Color(.33f, .45f, .22f), .95f); } }
        mBeton = Tex("beton architektoniczny", 2.5f, new Color(.82f, .81f, .78f), .9f, new Color(.55f, .55f, .52f), "concrete_wall_008", "concrete_floor_02");
        mFarbaBiala = Gladki("farba drogowa biała", new Color(.86f, .86f, .82f), .7f);
        mFarbaZolta = Gladki("farba drogowa żółta", new Color(.85f, .68f, .12f), .7f);
        mZeliwo = Gladki("żeliwo kratek", new Color(.10f, .10f, .10f), .75f, .45f);
        mStalGrafit = Gladki("stal grafit (słupy, ramy)", new Color(.13f, .14f, .15f), .5f, .6f);
        mStalSzczotk = Gladki("stal szczotkowana", new Color(.62f, .63f, .64f), .45f, .85f);
        mSzklo = Gladki("szkło witryn (szyba)", new Color(.58f, .68f, .70f, .20f), .06f, .1f);
        mSzkloDach = Gladki("szkło dachu atrium (szyba)", new Color(.70f, .80f, .84f, .28f), .08f, .1f);
        mSzkloCiemne = Gladki("szkło fasady przyciemniane (szyba)", new Color(.20f, .27f, .30f, .78f), .08f, .2f);
        mPosadzka = Tex("posadzka galerii (płyty 60×120, mat)", 1.2f, new Color(.80f, .78f, .75f), .55f, new Color(.72f, .70f, .67f), "large_grey_tiles", "floor_tiles_06", "square_tiles");
        mPosadzkaJasna = Tex("posadzka lokali (gres jasny, mat)", .9f, new Color(.92f, .91f, .88f), .6f, new Color(.85f, .84f, .80f), "floor_tiles_06", "large_grey_tiles", "square_tiles");
        mTynk = Tex("tynk wnętrz biały", 2f, new Color(.95f, .95f, .93f), .92f, new Color(.92f, .92f, .9f), "white_plaster_02", "painted_plaster_wall");
        mTynkCieply = Tex("tynk ciepły", 2f, new Color(.93f, .88f, .80f), .92f, new Color(.9f, .85f, .78f), "painted_plaster_wall", "white_plaster_02");
        mSufit = Gladki("sufit podwieszany biały", new Color(.90f, .90f, .89f), .95f);
        mOkladzina = Tex("okładzina fasady (płyty włóknocementowe)", 3f, new Color(.80f, .78f, .74f), .85f, new Color(.62f, .6f, .57f), "exterior_wall_cladding_02", "exterior_wall_cladding", "rectangular_facade_tiles");
        mOkladzinaCiemna = Gladki("okładzina grafitowa (aluminium)", new Color(.16f, .17f, .18f), .55f, .5f);
        mKamienCokol = Tex("cokół z kamienia", 1.5f, new Color(.62f, .60f, .57f), .85f, new Color(.45f, .44f, .42f), "white_sandstone_blocks_02", "granite_tile_02");
        mDrewno = Tex("drewno ciemne", 1.2f, new Color(.80f, .72f, .62f), .7f, new Color(.40f, .28f, .18f), "dark_wood", "wood_floor", "wooden_planks");
        mDrewnoJasne = Tex("drewno dębowe jasne", 1.2f, new Color(1f, .96f, .9f), .68f, new Color(.70f, .55f, .38f), "wood_floor", "plank_flooring_02", "wood_planks");
        mMembrana = Gladki("membrana dachowa", new Color(.30f, .31f, .32f), .95f);
        mGuma = Gladki("guma czarna", new Color(.06f, .06f, .065f), .9f);
        mCegla = Tex("cegła czerwona", 2.4f, new Color(.95f, .88f, .84f), .9f, new Color(.55f, .28f, .22f), "brick_wall_10", "red_brick_03", "brick_wall_001");
        mCeglaCiemna = Tex("cegła ciemna", 2.4f, new Color(.62f, .55f, .52f), .9f, new Color(.32f, .22f, .2f), "brick_wall_001", "red_brick_03", "brick_wall_10");
        mWoda = Gladki("woda fontanny", new Color(.42f, .56f, .60f, .72f), .05f, .05f);
        mLustro = Gladki("lustro (tafla)", new Color(.80f, .83f, .85f), .04f, 1f);
        mChrom = Gladki("chrom", new Color(.78f, .78f, .80f), .2f, 1f);
        mCzern = Gladki("czerń matowa", new Color(.035f, .035f, .04f), .8f);
        mBiel = Gladki("biel lakier", new Color(.92f, .92f, .91f), .5f);
        mTkanina = Tex("tkanina obić", .8f, new Color(.55f, .55f, .58f), .95f, new Color(.4f, .4f, .42f), "rough_linen", "fabric_pattern_05", "terry_cloth");
        mSkora = Tex("skóra obić", .8f, new Color(.9f, .85f, .8f), .55f, new Color(.30f, .18f, .12f), "leather_red_02", "leather_red_03", "leather_white");
        mKafelBialy = Tex("kafle łazienkowe białe", .6f, new Color(.96f, .96f, .96f), .55f, new Color(.92f, .92f, .92f), "square_tiles", "floor_tiles_06");
        mKafelSzary = Tex("gres łazienkowy szary", .6f, new Color(.62f, .62f, .63f), .62f, new Color(.5f, .5f, .52f), "large_grey_tiles", "blue_floor_tiles_01");
        mLaminat = Gladki("laminat jasny (meble)", new Color(.88f, .87f, .84f), .55f);
        mMarmur = Tex("konglomerat blatów", 1.0f, new Color(.95f, .95f, .94f), .5f, new Color(.9f, .9f, .9f), "marble_01");
        mBetonPolerowany = Tex("posadzka zaplecza (beton zatarty)", 2.5f, new Color(.70f, .70f, .68f), .7f, new Color(.55f, .55f, .53f), "concrete_floor_02", "concrete_wall_008");
    }

    // ======================================================================== SIATKI (scalanie per grupa i materiał)
    sealed class Siatka
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Vector4> bk = new List<Vector4>();
        public readonly List<int> t = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 bake, Vector2? ua = null, Vector2? ub = null, Vector2? uc = null, Vector2? ud = null)
        {
            var nn = Vector3.Cross(b - a, c - a); if (nn.sqrMagnitude < 1e-12f) nn = Vector3.Cross(c - a, d - a);
            if (nn.sqrMagnitude < 1e-12f) return;
            nn.Normalize();
            int s = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int i = 0; i < 4; i++) { n.Add(nn); bk.Add(new Vector4(bake.x, bake.y, 1f, 2718f)); }
            if (ua.HasValue) { uv.Add(ua.Value); uv.Add(ub.Value); uv.Add(uc.Value); uv.Add(ud.Value); }
            else { uv.Add(P(a, nn)); uv.Add(P(b, nn)); uv.Add(P(c, nn)); uv.Add(P(d, nn)); }
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 bake)
        {
            var nn = Vector3.Cross(b - a, c - a); if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count; v.Add(a); v.Add(b); v.Add(c);
            for (int i = 0; i < 3; i++) { n.Add(nn); bk.Add(new Vector4(bake.x, bake.y, 1f, 2718f)); }
            uv.Add(P(a, nn)); uv.Add(P(b, nn)); uv.Add(P(c, nn));
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }
        // UV w metrach świata, rzut wg normalnej (ciągłe płytki między bryłami)
        static Vector2 P(Vector3 p, Vector3 nn)
        {
            float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
            if (ax >= az) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }
    }
    sealed class Grupa { public string nazwa; public Transform rodzic; public Vector2 bake; public readonly Dictionary<Material, Siatka> s = new Dictionary<Material, Siatka>(); public GameObject kolizje; }
    static readonly Dictionary<string, Grupa> grupy = new Dictionary<string, Grupa>();
    static readonly List<Grupa> kolejnosc = new List<Grupa>();
    static Grupa g;

    /// <summary>Bieżąca grupa scalania (osobny obiekt na materiał). sun = widoczność słońca (0 = pod dachem), ao = zacienienie.</summary>
    static void G(string nazwa, Transform rodzic, float sun = 1f, float ao = 1f)
    {
        if (!grupy.TryGetValue(nazwa, out g)) { g = new Grupa { nazwa = nazwa, rodzic = rodzic, bake = new Vector2(sun, ao) }; grupy[nazwa] = g; kolejnosc.Add(g); }
        else g.bake = new Vector2(sun, ao);
    }
    static Siatka S(Material m)
    {
        if (!m) m = mBeton;
        if (!g.s.TryGetValue(m, out var s)) { s = new Siatka(); g.s[m] = s; }
        return s;
    }

    /// <summary>Prostopadłościan osiowy (środek, rozmiar). bez = pomiń ściany: 1 dół, 2 góra (oszczędność trójkątów).</summary>
    static void B(Vector3 c, Vector3 r, Material m, int bez = 0)
    {
        if (r.x <= 0 || r.y <= 0 || r.z <= 0) return;
        var s = S(m); var bk = g.bake;
        Vector3 a = c - r * .5f, b = c + r * .5f;
        s.Quad(new Vector3(a.x, a.y, b.z), new Vector3(b.x, a.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(a.x, b.y, b.z), bk);
        s.Quad(new Vector3(b.x, a.y, a.z), new Vector3(a.x, a.y, a.z), new Vector3(a.x, b.y, a.z), new Vector3(b.x, b.y, a.z), bk);
        s.Quad(new Vector3(a.x, a.y, a.z), new Vector3(a.x, a.y, b.z), new Vector3(a.x, b.y, b.z), new Vector3(a.x, b.y, a.z), bk);
        s.Quad(new Vector3(b.x, a.y, b.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, b.y, a.z), new Vector3(b.x, b.y, b.z), bk);
        if ((bez & 2) == 0) s.Quad(new Vector3(a.x, b.y, b.z), new Vector3(b.x, b.y, b.z), new Vector3(b.x, b.y, a.z), new Vector3(a.x, b.y, a.z), bk);
        if ((bez & 1) == 0) s.Quad(new Vector3(a.x, a.y, a.z), new Vector3(b.x, a.y, a.z), new Vector3(b.x, a.y, b.z), new Vector3(a.x, a.y, b.z), bk);
    }
    /// <summary>Prostopadłościan z min/max.</summary>
    static void BX(float x0, float x1, float y0, float y1, float z0, float z1, Material m, int bez = 0)
        => B(new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), m, bez);

    /// <summary>Prostopadłościan obrócony (yaw w stopniach wokół pionu, opcjonalnie pochylenie wokół osi X lokalnej).</summary>
    static void BR(Vector3 c, Vector3 r, Quaternion q, Material m)
    {
        var s = S(m); var bk = g.bake; Vector3 h = r * .5f;
        Vector3 P(float x, float y, float z) => c + q * new Vector3(x * h.x, y * h.y, z * h.z);
        s.Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), bk);
        s.Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), bk);
        s.Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), bk);
        s.Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), bk);
        s.Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1), bk);
        s.Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1), bk);
    }
    static Quaternion Yaw(float deg) => Quaternion.Euler(0, deg, 0);

    /// <summary>Walec (oś pionowa albo dowolna a→b), z denkami.</summary>
    static void Walec(Vector3 a, Vector3 b, float r, Material m, int seg = 12, float r2 = -1f, bool denka = true)
    {
        if (r2 < 0) r2 = r;
        var s = S(m); var bk = g.bake;
        var ax = (b - a).normalized;
        var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(ax, u);
        for (int i = 0; i < seg; i++)
        {
            float f0 = 2 * Mathf.PI * i / seg, f1 = 2 * Mathf.PI * (i + 1) / seg;
            Vector3 n0 = u * Mathf.Cos(f0) + w * Mathf.Sin(f0), n1 = u * Mathf.Cos(f1) + w * Mathf.Sin(f1);
            s.Quad(a + n1 * r, a + n0 * r, b + n0 * r2, b + n1 * r2, bk);
            if (denka) { s.Tri(b, b + n1 * r2, b + n0 * r2, bk); s.Tri(a, a + n0 * r, a + n1 * r, bk); }
        }
    }
    /// <summary>Płaski czworokąt z jawnymi UV 0..1 (szyldy, tablice, ekrany). a=lewy dół, b=prawy dół, c=prawy góra, d=lewy góra (patrząc z przodu).</summary>
    static void Plansza(Vector3 srodek, Vector3 normal, float w, float h, Material m)
    {
        var r = Vector3.Cross(Vector3.up, -normal).normalized; if (r.sqrMagnitude < .1f) r = Vector3.right;
        var up = Vector3.Cross(r, normal).normalized;   // r × n = góra
        Vector3 a = srodek - r * w * .5f - up * h * .5f, b = srodek + r * w * .5f - up * h * .5f, c = srodek + r * w * .5f + up * h * .5f, d = srodek - r * w * .5f + up * h * .5f;
        // przód = strona normalnej: kolejność wierzchołków jak w Quad (normal = (b−a)×(c−a))
        var nn = Vector3.Cross(b - a, c - a);
        if (Vector3.Dot(nn, normal) < 0) S(m).Quad(b, a, d, c, g.bake, new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
        else S(m).Quad(a, b, c, d, g.bake, new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1));
    }

    // kolizje: BoxCollider na wspólnym obiekcie grupy (osiowe), obrócone — osobne obiekty
    static void Kol(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        if (!g.kolizje) { g.kolizje = new GameObject(g.nazwa + " · kolizje"); g.kolizje.transform.SetParent(g.rodzic, false); }
        var c = g.kolizje.AddComponent<BoxCollider>();
        c.center = new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f);
        c.size = new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0));
    }
    static void KolR(Vector3 c, Vector3 r, Quaternion q, string nazwa)
    {
        var o = new GameObject(nazwa); o.transform.SetParent(g.rodzic, false); o.transform.position = c; o.transform.rotation = q;
        o.AddComponent<BoxCollider>().size = r;
    }
    /// <summary>Prostopadłościan + kolizja.</summary>
    static void BK(float x0, float x1, float y0, float y1, float z0, float z1, Material m, int bez = 0) { BX(x0, x1, y0, y1, z0, z1, m, bez); Kol(x0, x1, y0, y1, z0, z1); }

    static int siatekZapisanych;
    static void Flush()
    {
        foreach (var gr in kolejnosc)
        {
            foreach (var kv in gr.s)
            {
                var s = kv.Value; if (s.v.Count == 0) continue;
                var mesh = new Mesh { name = "Galeria " + gr.nazwa + " · " + kv.Key.name, indexFormat = s.v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(s.v); mesh.SetNormals(s.n); mesh.SetUVs(0, s.uv); mesh.SetUVs(3, s.bk); mesh.SetTriangles(s.t, 0);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                var o = new GameObject(gr.nazwa + " · " + kv.Key.name);
                o.transform.SetParent(gr.rodzic, false);
                o.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key;
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
                nObiektow++;
            }
            gr.s.Clear();
        }
        kolejnosc.Clear(); grupy.Clear(); g = null;
    }

    static void Utrwal()
    {
        AssetDatabase.StartAssetEditing();
        try
        {
            var seen = new HashSet<Mesh>();
            foreach (var r in new[] { ulica, obszar, barKorzen })
            {
                if (!r) continue;
                foreach (var f in r.GetComponentsInChildren<MeshFilter>(true))
                {
                    var m = f.sharedMesh; if (!m || seen.Contains(m) || AssetDatabase.Contains(m)) continue; seen.Add(m);
                    AssetDatabase.CreateAsset(m, Dir + "/Meshes/gal_" + (siatekZapisanych++).ToString("D5") + ".asset");
                }
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
    }

    // ======================================================================== MODELE (Poly Haven / biblioteka), × k
    static GameObject Model(Transform rodzic, string etykieta, Vector3 podloga, float yaw, float maxWys, bool kolizja, params string[] id)
    {
        var src = BraxRegion.Model(id); if (!src) return null;
        var o = BraxRegion.Put(rodzic, src, etykieta, podloga, yaw, K, kolizja, maxWys > 0 ? maxWys * K : 0f);
        if (o) { nObiektow++; foreach (var t in o.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic); }
        return o;
    }

    static Light Lampa(Transform rodzic, string nazwa, Vector3 p, float moc, float zasieg, bool ciepla = false)
    {
        var l = BraxRegion.Warm(rodzic, nazwa, p, moc, zasieg);
        if (!ciepla) l.color = new Color(1f, .97f, .92f);
        nLamp++; return l;
    }

    static void Siedzisko(Transform rodzic, string etykieta, Vector3 punkt, Vector3 przod)
    {
        var o = new GameObject("Siedzisko: " + etykieta); o.transform.SetParent(rodzic, false);
        o.transform.position = punkt; o.transform.rotation = Quaternion.LookRotation(new Vector3(przod.x, 0, przod.z).normalized, Vector3.up);
        var s = o.AddComponent<BraxSeat>(); s.surfaceY = punkt.y; s.label = etykieta; nSiedzisk++;
    }
}
