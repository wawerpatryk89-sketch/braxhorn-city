using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using M = BraxWnetrzaKit.Mat;

// BRAXHORN CITY — STACJA METRA M1 „BRAXHORN — NABRZEŻE” OD ZERA (wnętrza, 2.11).
// Właściciel 2.11: „metro ma być zrobione — wszystko”, „nie ma nawet wejścia, nie wiem, jak on tam wszedł — do poprawki
// CAŁKOWICIE”, na telefonie peron był pusty (w CI pociąg stał — dwa różne perony: stary G5 Codexa na −3.25 i stary Claude
// na −7.25, jeden nad drugim). Zasada 11:00: „zawsze ściągasz, kładziesz nową, a nie na to”.
// 1) USUNIĘTE: cała stara stacja G5 („Metro entrance and finished vestibule only”: wejście na bulwarze, hol, peron 16×18,
//    tablice PERONY/WYJŚCIE) i stara stacja Claude (budowana dawniej w tym pliku). Teren po starym wykopie zamknięty nową
//    nawierzchnią bulwaru i kolizją (bez dziury, bez niewidzialnych ścian).
// 2) NOWA STACJA pod końcem ulicy głównej (oś z = 28.5), jak prawdziwa:
//    • 2 wejścia z chodników ulicy głównej — przedłużenie chodnika północnego (z 32.6–36.0) i południowego (z 20.5–23.9)
//      na bulwar: zadaszone zejścia (szklana wiata na stalowej ramie), 2 biegi ze spocznikiem, poręcze po obu stronach,
//      balustrady, pylon z „M” i nazwą stacji, oświetlenie;
//    • hala biletowa (antresola −4.6) pod jezdnią: biletomaty, kasa z okienkiem, bramki z szybkami (otwierają się przed
//      graczem), mapy linii, tablice odjazdów, kierunkowskazy, kamery, gaśnica, wyjścia ewakuacyjne;
//    • schody stałe i ruchome (JADĄ w górę — BraxSchodyRuchome, A 2.11) z antresoli na peron wyspowy −11.2 (81 m, 2 tory);
//    • peron: krawędź z granitu, żółta linia, ławki, kosze, tablice, mapy, reklamy, kamery, gaśnice, zegary, SOS;
//    • POCIĄG ZAWSZE STOI przy peronie 1: pobrany model metro_train (2 wagony czołami na zewnątrz, drzwi zamknięte); bez modelu — wagony z kodu;
//    • tory w obie strony w tunele, które gasną w ciemności (bez świateł na końcu).
// Skala Braxa k = 1.294: stopnie 0.16·k × 0.30·k, poręcze 0.9·k, drzwi wagonu 1.3·k × 1.95·k, siedziska 0.45·k…
public static class BraxClaudeMetro
{
    // ---------------- wymiary (świat, m) ----------------
    static float K => W.K;
    const float yS = .15f;            // chodnik / bulwar
    const float yM = -4.6f;           // posadzka antresoli (hali biletowej)
    const float yMc = -.75f;          // sufit antresoli
    const float yP = -11.2f;          // posadzka peronu
    const float yHc = -5.0f;          // sufit hali peronowej
    static float yR => yP - 1.1f * K; // główka szyny (peron 1.1·k nad szyną)
    const float PX0 = -18f, PX1 = 63f;                 // hala peronowa
    const float zS = 18.1f, zN = 38.9f;                // wewnętrzne lica ścian zatorowych
    const float zPS = 22.8f, zPN = 34.2f;              // krawędzie peronu wyspowego (11.4 m)
    const float zTs = 20.9f, zTn = 36.1f;              // osie torów (peron 2 — płd., peron 1 — płn., pociąg)
    const float MX0 = 43.6f, MX1 = 81.1f, MZ0 = 20.5f, MZ1 = 36.0f; // antresola przedłużona pod odsuniętym zejściem   // antresola
    const float WX0 = 44.4f, WX1 = 58.4f, WZ0 = 25.4f, WZ1 = 31.5f;   // otwór schodów w stropie antresoli
    const float GX = 63f;                              // linia bramek (strefa płatna na zachód)
    // Wejście było przyklejone do narożnika kamienicy przy x≈66 (nagranie nr 2).
    // Przesuwamy CAŁE zejście 2.5 m w stronę otwartego bulwaru, ale nie do wody.
    // Przesuwane razem: otwór, schody stałe/ruchome, zadaszenie, murki, kolizje i punkt misji.
    const float EX0 = 69.0f;
    static float XKoniec => EX0 + (21f * .30f + 1.5f) * K; // końcowa krawędź dolnego biegu
    static readonly float[] EZ = { 20.5f, 32.6f };     // pasy zejść: płd. z 20.5–23.9, płn. z 32.6–36.0 (szer. 3.4)
    const float EW = 3.4f;
    // JEDNO wejście — płn. pas (z 32.6–36.0). Zespół wejścia odsunięty w osi X od kamienicy, z wolnym chodnikiem przed dojściem.
    // Pas płd. (z 20.5–23.9) zamknięty: chodnik nad nim nie jest cięty (wraca cały), pod nim pełny grunt (hala go omija ciągłą ścianą).
    static readonly float[] WEJSCIA = { 32.6f };
    static bool Wejscie(float z0) => Array.IndexOf(WEJSCIA, z0) >= 0;
    const float TW0 = -70f, TW1 = 112f;                // końce tuneli

    [Serializable] sealed class Raport
    {
        public string status = "NOT_RUN", blad = "";
        public List<string> usuniete = new List<string>(), uprzatniete = new List<string>(), uwagi = new List<string>();
        public int otwory, siatki, bryly, trojkaty, kolizje, lampy, wagony, siedziska, punktyAkcji, obsada;
        public string peron, wejscia;
    }

    static Raport rep;
    static Transform podziemie, zewnatrz;

    public static void Apply(Transform cityRoot)
    {
        if (!BraxClaudeTowerTerrace.Enabled) return;
        rep = new Raport();
        try { Build(cityRoot); rep.status = "BUILT"; }
        catch (Exception e) { rep.status = "FAILED"; rep.blad = e.Message; Debug.LogError("BRAXHORN_METRO " + e); }
        Directory.CreateDirectory("Assets/PlayableGenerated/ClaudeMetro");
        File.WriteAllText("Assets/PlayableGenerated/ClaudeMetro/metro.json", JsonUtility.ToJson(rep, true));
        Debug.Log("BRAXHORN_METRO " + JsonUtility.ToJson(rep));
    }

    // ---------------- materiały ----------------
    static Material mPosadzka, mPosadzkaJasna, mSciana, mLamperia, mSufit, mBelka, mStal, mStalCiemna, mSzklo, mZolty,
        mBeton, mBetonCiemny, mBetonNoc, mCzern, mPodklad, mSzyna, mTrzecia, mAtlas, mAtlasDruk, mKlosz, mStopien, mNosek,
        mGuma, mPudlo, mPas, mCzarny, mWnetrzeWagonu, mTapicerka, mPodwozie, mAluminium, mPoreczGuma, mCzerwony,
        mDrzwiTech, mZielen, mBialy, mDrewno;
    static Texture2D atlas;

    static void Materialy()
    {
        mPosadzka = M.Tex("metro posadzka granit", 1.2f, new Color(.80f, .80f, .80f), .62f, 0f, "granite_tile_02", "granite_tile");
        mPosadzkaJasna = M.Tex("metro krawędź peronu granit jasny", .9f, new Color(1.05f, 1.04f, 1.0f), .6f, 0f, "granite_tile", "granite_tile_02");
        // P0.1 (właściciel 23:05: „kafelki szachownica w szybie”): okładzina wielkoformatowa (płyty 2.4 m, jasny kamień/tynk
        // architektoniczny) zamiast drobnych kwadratów w dwóch odcieniach — szyb zejścia i hala tym samym wykończeniem
        mSciana = M.Tex("metro okładzina ścian (płyty wielkoformatowe)", 2.4f, new Color(.95f, .95f, .93f), .42f, 0f, "rectangular_facade_tiles", "white_plaster_02", "granite_tile", "square_tiles");
        mLamperia = M.Tex("metro cokół ścian granit grafit", 1.2f, new Color(.30f, .31f, .33f), .55f, 0f, "granite_tile_02", "granite_tile");
        mSufit = M.Plain("metro sufit panele aluminiowe", new Color(.80f, .81f, .82f), .5f, .35f);
        mBelka = M.Plain("metro belki i obramowania grafit", new Color(.20f, .21f, .23f), .55f, .4f);
        mStal = M.Plain("metro stal szczotkowana", new Color(.64f, .65f, .66f), .32f, .9f);
        mStalCiemna = M.Plain("metro stal grafitowa", new Color(.14f, .15f, .16f), .45f, .6f);
        mSzklo = M.Plain("metro szkło bezpieczne", new Color(.74f, .84f, .86f, .24f), .06f, 0f);
        mZolty = M.Plain("metro żółta linia (pole uwagi)", new Color(.88f, .70f, .06f), .75f);
        mBeton = M.Tex("metro beton torowiska", 2.4f, new Color(.58f, .58f, .56f), .9f, 0f, "concrete_floor_02", "concrete_wall_008");
        mBetonCiemny = M.Tex("metro beton tunelu (półmrok)", 2.4f, new Color(.22f, .22f, .21f), .92f, 0f, "concrete_wall_008", "concrete_floor_02");
        mBetonNoc = M.Tex("metro beton tunelu (ciemność)", 2.4f, new Color(.06f, .06f, .06f), .95f, 0f, "concrete_wall_008", "concrete_floor_02");
        mCzern = M.Plain("metro czerń głębi tunelu", new Color(.004f, .004f, .004f), 1f);
        mPodklad = M.Plain("metro podkłady betonowe", new Color(.40f, .39f, .38f), .9f);
        mSzyna = M.Plain("metro szyna", new Color(.50f, .48f, .46f), .35f, .85f);
        mTrzecia = M.Plain("metro osłona trzeciej szyny", new Color(.78f, .66f, .22f), .7f);
        atlas = M.Obraz(W.Src + "metro_atlas.png", 1024);
        mAtlas = M.Ekran("metro oznakowanie podświetlane (atlas)", atlas, new Color(.93f, .93f, .93f));
        mAtlasDruk = atlas ? BraxCityMaterials.Textured("Wnętrza: metro oznakowanie drukowane (atlas)", atlas, null, null, 1f) : mBialy;
        if (mAtlasDruk) { mAtlasDruk.SetFloat("_Roughness", .6f); EditorUtility.SetDirty(mAtlasDruk); }
        mKlosz = M.Ekran("metro klosz oprawy LED", null, new Color(.98f, .97f, .93f));
        mStopien = M.Tex("metro stopnie granit", .9f, new Color(.72f, .72f, .72f), .65f, 0f, "granite_tile_02", "granite_tile");
        mNosek = M.Plain("metro nosek stopnia antypoślizg", new Color(.86f, .66f, .06f), .8f);
        mGuma = M.Plain("wagon podłoga guma nakrapiana", new Color(.30f, .31f, .33f), .85f);
        mPudlo = M.Plain("wagon pudło stal nierdzewna", new Color(.72f, .74f, .76f), .36f, .85f);
        mPas = M.Plain("wagon pas linii M1 czerwień", new Color(.70f, .05f, .11f), .45f, .2f);
        mCzarny = M.Plain("wagon czarne ramy i uszczelki", new Color(.05f, .05f, .06f), .6f, .1f);
        mWnetrzeWagonu = M.Plain("wagon wnętrze panele", new Color(.88f, .89f, .88f), .55f);
        mTapicerka = M.Tex("wagon tapicerka siedzeń", .45f, new Color(.32f, .42f, .78f), .95f, 0f, "fabric_pattern_05", "rough_linen");
        mPodwozie = M.Plain("wagon podwozie i wózki", new Color(.10f, .10f, .11f), .7f, .3f);
        mAluminium = M.Plain("metro aluminium ryflowane (schody ruchome)", new Color(.56f, .57f, .58f), .45f, .8f);
        mPoreczGuma = M.Plain("metro poręcz ruchoma guma", new Color(.04f, .04f, .045f), .55f);
        mCzerwony = M.Plain("metro czerwień (gaśnica, SOS)", new Color(.72f, .05f, .05f), .45f, .1f);
        mDrzwiTech = M.Plain("metro drzwi techniczne stal malowana", new Color(.42f, .45f, .48f), .5f, .4f);
        mZielen = M.Plain("metro zieleń znaków ewakuacyjnych", new Color(.0f, .45f, .25f), .5f);
        mBialy = M.Plain("metro biel urządzeń", new Color(.90f, .90f, .88f), .5f);
        mDrewno = M.Tex("metro ławki drewno", .8f, new Color(.80f, .62f, .44f), .7f, 0f, "teak_veneer", "wood_floor", "dark_wood");
    }

    // ---------------- atlas (px od lewej-góry, 1024×1024) ----------------
    static RectInt A(int x, int y, int w, int h) => new RectInt(x, y, w, h);
    static readonly RectInt aLogo = A(0, 0, 256, 256), aNazwa = A(256, 0, 768, 128), aFryz = A(256, 128, 768, 64),
        aPerony = A(256, 192, 512, 64), aWyjN = A(0, 256, 512, 64), aWyjS = A(512, 256, 512, 64), aKasy = A(768, 192, 256, 64),
        aOdjazdy = A(0, 320, 512, 192), aMapa = A(512, 320, 512, 192), aEwak = A(0, 512, 256, 128), aBiletomat = A(256, 512, 256, 256),
        aZolta = A(512, 512, 512, 64), aBramkaOK = A(512, 576, 64, 64), aBramkaX = A(576, 576, 64, 64), aKasa = A(640, 576, 384, 64),
        aCzolo = A(512, 640, 512, 64), aNumer = A(0, 640, 256, 64), aPeron1 = A(256, 768, 128, 64), aPeron2 = A(384, 768, 128, 64),
        aMonitoring = A(0, 704, 256, 64), aGasnica = A(512, 704, 128, 128), aTrasa = A(0, 832, 1024, 64), aSos = A(640, 704, 128, 64),
        aRuchOK = A(768, 704, 128, 64), aKafel = A(768, 768, 256, 64), aZegar = A(640, 768, 128, 64), aArena = A(0, 896, 512, 128), aNova = A(512, 896, 512, 128);

    static void Tab(Vector3 c, Vector3 n, float w, float h, RectInt r, bool podswietl = true, bool dwustronny = false)
        => W.Tablica(podswietl ? mAtlas : mAtlasDruk, c, n, w, h, r, 1024, 1024, dwustronny);

    /// <summary>Tablica wisząca (dwie strony, kaseton grafitowy, cięgna do sufitu).</summary>
    static void Wiszaca(Vector3 c, Vector3 n, float w, float h, RectInt przod, RectInt tyl, float sufit)
    {
        n = new Vector3(n.x, 0, n.z).normalized; var bok = Vector3.Cross(n, Vector3.up);
        W.OBox(mBelka, c, new Vector3(w + .08f, h + .08f, .12f), Quaternion.LookRotation(n));
        Tab(c + n * .065f, n, w, h, przod); Tab(c - n * .065f, -n, w, h, tyl);
        foreach (float s in new[] { -.35f, .35f })
            W.Rura(mStal, c + bok * (w * s) + Vector3.up * (h * .5f + .04f), new Vector3(c.x, sufit, c.z) + bok * (w * s), .012f, 6);
    }

    // ---------------- budowa ----------------
    static void Build(Transform cityRoot)
    {
        W.Wyczysc();
        // 1) STARE METRO — ściągamy w całości
        rep.usuniete.AddRange(W.Usun("Metro entrance and finished vestibule only", "CLAUDE stacja metra BRAXHORN NABRZEŻE", "CLAUDE metro M1*"));
        Materialy();

        zewnatrz = new GameObject("CLAUDE metro M1 — wejścia z ulicy (na zewnątrz)").transform; zewnatrz.SetParent(cityRoot, false);
        podziemie = new GameObject("CLAUDE metro M1 — stacja BRAXHORN NABRZEŻE (pod ziemią)").transform; podziemie.SetParent(cityRoot, false);

        // 2) teren po starym wykopie (bulwar x 75–80.7, z 34.8–49.8) i po starych pustkach G5 — zamknięty
        ZamknijStaryWykop(cityRoot);

        // 3) otwory w chodniku (zejścia) + uprzątnięcie drobnych rzeczy z miejsca budowy + kolizje pod ziemią
        foreach (float z0 in WEJSCIA)
        {
            var otw = new Bounds(new Vector3((EX0 + XKoniec + .1f) * .5f, -.15f, z0 + EW * .5f), new Vector3(XKoniec + .1f - EX0, 1.5f, EW));
            rep.otwory += W.Otwor(cityRoot, otw, "metro_zejscie_" + z0.ToString("F1"), zewnatrz);
            var wiata = new Bounds(new Vector3((EX0 - 1.6f + XKoniec + .7f) * .5f, 3f, z0 + EW * .5f), new Vector3(XKoniec - EX0 + 2.3f, 6f, EW + .6f));   // + głowica schodów ruchomych na chodniku
            rep.uprzatniete.AddRange(W.Uprzatnij(cityRoot, wiata, zewnatrz));
        }
        foreach (var b in new[] {
            Bryla(MX0 - .5f, MX1 + .5f, yM - .45f, -.8f, MZ0 - .3f, MZ1 + .05f),          // antresola
            Bryla(PX0 - .5f, PX1 + .5f, yR - 1f, yHc, zS - .5f, zN + .5f),                // hala peronowa
            Bryla(TW0, PX0, yR - 1f, -6.4f, zS - .5f, zN + .5f), Bryla(PX1, TW1, yR - 1f, -6.4f, zS - .5f, zN + .5f) })   // tunele
            rep.otwory += W.Otwor(cityRoot, b, "metro_podziemie", zewnatrz);

        // 3b) W istniejącym APK #64 dojście z ulicy blokowała donica wieżowca w x≈63, z≈35.
        // Zdjąć drobne dekoracje na dojściu (model i kolizję), NIE wyłączać fasady
        // ani całej kolizji budynku. Strefa dotyczy tylko 4.6×3.0 m przed wejściem.
        var dojscie = Bryla(EX0 - 4.5f, EX0 + .1f, -.35f, 3.4f, EZ[1] + .3f, EZ[1] + EW - .2f);
        var dekoracjeDojscia = W.Uprzatnij(cityRoot, dojscie, zewnatrz, 2.5f);
        rep.uprzatniete.AddRange(dekoracjeDojscia.Select(x => "dojście metro: " + x));
        Debug.Log("BRAXHORN_METRO_DOJSCIE usuniętych drobnych przeszkód=" + dekoracjeDojscia.Count);

        // 4) budowa
        W.Start(zewnatrz); W.Wnetrze = false; W.Komorka = 12f;
        foreach (float z0 in WEJSCIA) Wejscie(z0, z0 > 28f);
        W.Wnetrze = true;
        foreach (float z0 in WEJSCIA) ZejscieWnetrze(z0, z0 > 28f);       // ściany szybu schodów poniżej terenu (wnętrze)
        rep.siatki += W.Zapisz("metro_wejscia");

        W.Start(podziemie); W.Komorka = 14f;
        Antresola();
        SchodyNaPeron();
        Hala();
        Tory();
        Tunele();
        Wyposazenie();
        W.Komorka = 26f;
        Pociag();
        rep.siatki += W.Zapisz("metro_stacja");

        // A 2.11 (właściciel: „NIC NIE NAKŁADAMY na nic”): oznakowanie, żółte linie, noski stopni, pasy i ramki leżały 2–12 mm
        // na ścianach / posadzce / pudłach wagonów. Pod każdym takim elementem powierzchnia pod spodem jest WYCINANA (element
        // siedzi w otworze, nie na powierzchni) — ta sama kontrola co w pierzejach, log BRAXHORN_MIASTO_A_MIGOTANIE metro.
        BraxMiastoA.KontrolaMigotania(new[] { zewnatrz, podziemie }, Bryla(TW0 - 10f, TW1 + 10f, -20f, 8f, 5f, 55f), t =>
        {
            var r = t.GetComponent<Renderer>(); var n = r && r.sharedMaterial ? r.sharedMaterial.name.ToLowerInvariant() : "";
            return n.Contains("oznakowanie") || n.Contains("żółta linia") || n.Contains("nosek") || n.Contains("pas linii") || n.Contains("klosz") || n.Contains("czarne ramy");
        }, "metro", true);

        // 5) widoczność (stacja rysowana tylko, gdy kamera/gracz pod ziemią albo przy wejściach) i zasięg kamery pod ziemią
        var wid = podziemie.gameObject.AddComponent<BraxWidocznoscWnetrza>(); wid.nazwa = "metro M1 NABRZEŻE";
        wid.strefy = new[] { Bryla(TW0 - 5f, TW1 + 5f, -16f, -.55f, 14f, 43f), Bryla(56f, 90f, -.55f, 14f, 8f, 48f) };
        var zw = new GameObject("zasięg kamery pod ziemią (BraxClaudeZasiegWnetrza)"); zw.transform.SetParent(podziemie, false);
        var zc = zw.AddComponent<BraxClaudeZasiegWnetrza>(); zc.zasieg = 140f; zc.sale = new[] { Bryla(TW0 - 5f, TW1 + 5f, -16f, -1.2f, 14f, 43f) };

        // 6) punkty misji (stare kotwice systemów przeniesione na nową stację) i raport
        W.Grupa = podziemie;
        W.Punkt("metro_entry", "metro_entry", new Vector3(EX0 - .6f, yS, EZ[1] + EW * .5f));
        W.Punkt("metro_stairs_bottom", "metro_stairs_bottom", new Vector3(XKoniec + 1.0f, yM, EZ[1] + EW * .5f));
        W.Punkt("metro_vestibule", "metro_vestibule", new Vector3(70.5f, yM, 28.25f));
        W.Punkt("metro_station_boundary", "metro_station_boundary", new Vector3(GX, yM, 28.5f));
        W.Punkt("metro_peron", "metro_peron", new Vector3(20f, yP, 28.5f));
        W.Punkt("metro_pociag", "metro_pociag", new Vector3(40f, yP, zPN - .9f));     // przy drzwiach pociągu (peron 1)
        rep.bryly = W.Bryly; rep.trojkaty = W.Trojkaty; rep.kolizje = W.Kolizje; rep.obsada = W.Obsady.Count;
        rep.uwagi.AddRange(W.Braki);
        rep.lampy = podziemie.GetComponentsInChildren<Light>(true).Length + zewnatrz.GetComponentsInChildren<Light>(true).Length;
        rep.siedziska = podziemie.GetComponentsInChildren<BraxSeat>(true).Length;
        rep.punktyAkcji = podziemie.GetComponentsInChildren<BraxPunktAkcji>(true).Length;
        rep.peron = "x " + PX0 + "…" + PX1 + ", z " + zPS + "…" + zPN + ", y " + yP;
        rep.wejscia = "płn. x " + EX0 + "…" + XKoniec.ToString("F1") + " z " + EZ[1] + "…" + (EZ[1] + EW) + " (jedyne); płd. z " + EZ[0] + "…" + (EZ[0] + EW) + " zamknięte — chodnik cały, pod nim grunt, ściana hali ciągła";
    }

    // ---------------- P0.1: TEST PRZEJŚCIA (właściciel 23:05: „schody kończą się ślepym podestem”, a w logu BUILT) ----------------
    // Na końcu buildu miasta (scena kompletna): 1) obce kolizje (nie należące do stacji) w objętościach przejść — zejścia pod
    // terenem, dół schodów, antresola, peron — WYŁĄCZONE (log z nazwą i miejscem); 2) przejście kapsułą gracza (r 0.33, 2.25 m,
    // stopień 0.4) co 0.5 m po trasie chodnik → schody → dół szybu → antresola → bramki → schody na peron → peron, dla OBU
    // wejść; podłoga promieniem w dół. Log BRAXHORN_METRO_PRZEJSCIE wejscie=płn ok|zablokowane:<kolizja @ miejsce> (cel: oba ok).
    public static void KontrolaPrzejscia()
    {
        if (!zewnatrz || !podziemie) { Debug.LogWarning("BRAXHORN_METRO_PRZEJSCIE brak stacji (krok metra nie zbudował korzeni)"); return; }
        Physics.SyncTransforms();
        float xEnd = EX0 + (12 - 1 + 11 - 1) * .30f * K + 1.5f * K;
        bool Wlasna(Collider c) => c.transform.IsChildOf(zewnatrz) || c.transform.IsChildOf(podziemie);
        const int maska = ~((1 << 8) | (1 << 2) | (1 << 29) | (1 << 31));
        // 1) obce kolizje w przejściach
        var obj = new List<Bounds> { Bryla(xEnd + .1f, MX1 - .1f, yM + .05f, yMc - .05f, MZ0 + .1f, MZ1 - .1f), Bryla(MX0 + .1f, MX1 - .1f, yM + .05f, yMc - .05f, MZ0 + .1f, MZ1 - .1f), Bryla(PX0, PX1, yP + .05f, yHc - .1f, zPS, zPN) };
        foreach (float z0 in WEJSCIA) obj.Add(Bryla(EX0 + .1f, xEnd + .1f, yM + .05f, -.25f, z0 + .2f, z0 + EW - .2f));
        var obce = new List<string>();
        foreach (var b in obj)
            foreach (var c in Physics.OverlapBox(b.center, b.extents, Quaternion.identity, maska, QueryTriggerInteraction.Ignore))
            {
                // pudełko dodane przez kontrolę kolizji (BraxKontrolaKolizji) jest dzieckiem elementu stacji, ale stacja ma własne
                // kolizje — w przejściu to obca przeszkoda (build 57c1767: „próg/ściana BRAXHORN kolizja (kontrola)” po 8 m schodów)
                bool zKontroli = c && c.name == "BRAXHORN kolizja (kontrola)";
                if (!c || !c.enabled || (Wlasna(c) && !zKontroli) || c.GetComponentInParent<BraxGymPlayer>()) continue;
                obce.Add(c.name + " (" + (c.transform.parent ? c.transform.parent.name : "-") + ") " + c.bounds.center.ToString("F1") + " " + c.bounds.size.ToString("F1"));
                c.enabled = false;
            }
        Physics.SyncTransforms();
        // 2) przejście kapsułą
        string Kapsula(Vector3 stopa)
        {
            var hits = Physics.OverlapCapsule(stopa + Vector3.up * (.33f + .4f), stopa + Vector3.up * (2.25f - .33f), .33f, maska, QueryTriggerInteraction.Ignore);
            foreach (var c in hits)
            {
                if (!c || c.name.Contains("blokada przejścia") || c.GetComponentInParent<BraxDrzwiPrzesuwne>() || c.GetComponentInParent<BraxCityDoor>() || c.GetComponentInParent<BraxGymPlayer>()) continue;
                return c.name + " (" + (c.transform.parent ? c.transform.parent.name : "-") + ")";
            }
            return null;
        }
        float Wolny(float x, float y, float za, float zb, float pref)
        {
            float best = float.NaN, bd = 1e9f;
            for (float z = za; z <= zb; z += .1f) if (Kapsula(new Vector3(x, y, z)) == null && Mathf.Abs(z - pref) < bd) { bd = Mathf.Abs(z - pref); best = z; }
            return best;
        }
        string Idz(List<Vector3> pkt, out float metry)
        {
            metry = 0; float y = pkt[0].y;
            for (int i = 0; i + 1 < pkt.Count; i++)
            {
                var a = pkt[i]; var b = pkt[i + 1]; var d = b - a; d.y = 0; float L = d.magnitude; if (L < .01f) continue;
                for (float s = 0; s <= L; s += .5f)
                {
                    var q = a + d / L * s;
                    if (!Physics.Raycast(new Vector3(q.x, y + 1.0f, q.z), Vector3.down, out var hit, 4.5f, maska, QueryTriggerInteraction.Ignore))
                        return "brak podłogi @" + new Vector3(q.x, y, q.z).ToString("F1");
                    if (hit.point.y > y + .45f) return "próg/ściana " + hit.collider.name + " @" + hit.point.ToString("F1");
                    y = hit.point.y;
                    var k = Kapsula(new Vector3(q.x, y, q.z));
                    if (k != null) return k + " @" + new Vector3(q.x, y, q.z).ToString("F1");
                    metry += .5f;
                }
            }
            return null;
        }
        var wyniki = new List<string>(); int ok = 0;
        foreach (float z0 in WEJSCIA)
        {
            bool pln = z0 > 28f; float zc = ZcSchodow(z0), zIn = pln ? EZ[1] - 1.0f : EZ[0] + EW + 1.0f;
            float zBr = Wolny(GX, yM, MZ0 + .5f, MZ1 - .5f, 28.45f);
            float zSt = Wolny(WX1 + .6f, yM, WZ0 + .4f, WZ1 - .4f, 27.15f);
            string wynik;
            if (float.IsNaN(zBr)) wynik = "zablokowane: bramki — brak wolnego przejścia na linii x " + GX;
            else if (float.IsNaN(zSt)) wynik = "zablokowane: wejście na schody na peron (x " + (WX1 + .6f).ToString("F1") + ")";
            else
            {
                var trasa = new List<Vector3> { new Vector3(EX0 - 4f, yS, zc), new Vector3(EX0 - .3f, yS, zc), new Vector3(xEnd + 1.0f, yM, zc), new Vector3(xEnd + 1.0f, yM, zIn),
                    new Vector3(GX + 3f, yM, zIn), new Vector3(GX + 1.5f, yM, zBr), new Vector3(GX - 1.5f, yM, zBr), new Vector3(WX1 + 1.0f, yM, zSt), new Vector3(WX0 - 1.5f, yP, zSt), new Vector3(20f, yP, 28.5f) };
                var r = Idz(trasa, out float m);
                wynik = r == null ? "ok (" + m.ToString("F0") + " m)" : "zablokowane:" + r + " po " + m.ToString("F0") + " m";
                // Odbiór metra wymaga DRÓŻNOŚCI obu przejść, nie tylko schodów stałych.
                // schody ruchome obok (jadą w dół; test idzie po ich rampie): chodnik → głowica → bieg → antresola → bramki
                float ze = ER_Os(z0);
                var trasaR = new List<Vector3> { new Vector3(EX0 - 3.5f, yS, ze), new Vector3(EX0 + .4f, yS, ze), new Vector3(xEnd - .3f, yM, ze), new Vector3(xEnd + 1.0f, yM, ze),
                    new Vector3(xEnd + 1.0f, yM, zIn), new Vector3(GX + 3f, yM, zIn), new Vector3(GX + 1.5f, yM, zBr), new Vector3(GX - 1.5f, yM, zBr) };
                var rr = Idz(trasaR, out float mR);
                if (r == null && rr == null) ok++;
                wyniki.Add("schody_ruchome=" + (rr == null ? "ok (" + mR.ToString("F0") + " m)" : "zablokowane:" + rr + " po " + mR.ToString("F0") + " m"));
            }
            wyniki.Add("wejscie=" + (pln ? "płn" : "płd") + " " + wynik);
            // wolny chodnik obok wejścia (cel ≥ 2.5 m·k z jednej strony): promień poziomy 1 m nad chodnikiem w obie strony od pasa
            try
            {
                float xm = (EX0 + 76.6f) * .5f, cel = 2.5f * K;
                float Wolne(float z, float kier) => Physics.Raycast(new Vector3(xm, yS + 1f, z), new Vector3(0f, 0f, kier), out var h, 15f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 15f;
                float wPln = Wolne(z0 + EW + .05f, 1f), wPld = Wolne(z0 - .05f, -1f);
                wyniki.Add("chodnik_obok płn=" + wPln.ToString("F1") + " m płd=" + wPld.ToString("F1") + " m (cel ≥ " + cel.ToString("F2") + ") " + (Mathf.Max(wPln, wPld) >= cel ? "ok" : "ZA WĄSKI"));
            }
            catch (Exception e) { wyniki.Add("chodnik_obok DO POPRAWKI " + e.Message); }
        }
        string linia = "BRAXHORN_METRO_PRZEJSCIE " + string.Join(" | ", wyniki) + " (cel: jedno wejście ok) obce_kolizje_wylaczone=" + obce.Count + (obce.Count > 0 ? " [" + string.Join("; ", obce.Take(20)) + "]" : "");
        if (ok == WEJSCIA.Length) Debug.Log(linia); else Debug.LogWarning(linia);
    }

    static Bounds Bryla(float x0, float x1, float y0, float y1, float z0, float z1)
        => new Bounds(new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, y1 - y0, z1 - z0));

    // ---------------- teren po starym metrze ----------------
    /// <summary>Pas dawnego zejścia (pkt 1, właściciel 01:39: „nie ma zostać nic”): pod chodnikiem pełny grunt, bez pomieszczeń,
    /// drzwi i wnęk. Hala antresoli omija ten pas: ściana obudowy (z okładziną i cokołem) jest jej ścianą, a na obu końcach pasa
    /// ściany szczytowe z tą samą okładziną i cokołem domykają lico — ciągła ściana bez otworów.</summary>
    static void PasZamkniety(float z0, float xEnd, bool pln)
    {
        float zA = pln ? z0 - .25f : z0, zB = pln ? z0 + EW : z0 + EW + .25f;           // od ściany obudowy do lica zewnętrznego
        W.MM(mSciana, EX0 - .6f, EX0 - .3f, yM, yMc, zA, zB, true);                   // szczyt zachodni (lico od hali)
        W.MM(mSciana, xEnd, xEnd + .25f, yM, yMc, zA, zB, true);                      // szczyt wschodni
        float lA = zA + (pln ? 0f : .02f), lB = zB - (pln ? .02f : 0f);               // bez zakładki z cokołem ściany zewnętrznej
        W.MM(mLamperia, EX0 - .62f, EX0 - .58f, yM, yM + .3f, lA, lB);
        W.MM(mLamperia, xEnd + .23f, xEnd + .27f, yM, yM + .3f, lA, lB);
    }

    static void ZamknijStaryWykop(Transform cityRoot)
    {
        // nawierzchnia jak bulwar (ten sam materiał co „Promenade paving”), kolizja z wierzchem na wysokości chodnika
        Material pav = null;
        foreach (var r in cityRoot.GetComponentsInChildren<MeshRenderer>(true))
            if (r.name.StartsWith("Promenade paving", StringComparison.Ordinal) && r.sharedMaterial) { pav = r.sharedMaterial; break; }
        if (!pav) { pav = M.Plain("bulwar płyty (zastępczy)", new Color(.46f, .47f, .48f), .85f); rep.uwagi.Add("brak materiału Promenade paving — zastępczy"); }
        W.Start(zewnatrz); W.Wnetrze = false;
        // stary wykop x 75–80.7, z 34.8–49.8, bez części w nowym zejściu północnym (x < 76.7, z < 36.0)
        foreach (var r in new[] {
            // Zamykanie starego wykopu NIE może zakleić nowego otworu schodów!
            new Rect(79.5f, 34.8f, 1.2f, 1.2f),
            new Rect(75f, 36.0f, 5.7f, 13.8f)
        })
        {
            W.Quad(pav, new Vector3(r.xMin, yS, r.yMin), new Vector3(r.xMin, yS, r.yMax), new Vector3(r.xMax, yS, r.yMax), new Vector3(r.xMax, yS, r.yMin));
            W.KolizjaMM(r.xMin, r.xMax, yS - .3f, yS, r.yMin, r.yMax);
        }
        // Stare pustki G5 były w CI zamykane samą NIEWIDOCZNĄ kolizją. Na Androidzie
        // po wycięciu źródłowej nawierzchni widoczna była niebieska/wodna dziura.
        // Odtwarzamy TYLKO powierzchnię faktycznie wyciętych prostokątów G5 z
        // BraxCityRebuild ("G5_access_corridor_void", "G5_station_platform_and_tracks_void").
        // Nie nakładamy płyty na zachowany chodnik. Pod x >= 84.0 jest już morze,
        // dlatego tam nawierzchnia nie może wejść.
        foreach (var r in new[] {
            new Rect(74.9f, 49.8f, 5.9f, 4.25f),
            new Rect(69.7f, 54.05f, 14.29f, 18.15f)
        })
        {
            W.Quad(pav,
                new Vector3(r.xMin, yS, r.yMin), new Vector3(r.xMin, yS, r.yMax),
                new Vector3(r.xMax, yS, r.yMax), new Vector3(r.xMax, yS, r.yMin));
            W.KolizjaMM(r.xMin, r.xMax, yS - .3f, yS, r.yMin, r.yMax);
        }
        rep.uwagi.Add("G5: odtworzono wizualnie nawierzchnię + kolizję starych wykopów; kontrola zrzutu przy nadbrzeżu konieczna");
        rep.siatki += W.Zapisz("metro_stary_wykop_zamkniety");
    }

    // ---------------- schody (bieg) ----------------
    /// <summary>Bieg schodów w dół: górna krawędź w punkcie `gora` (środek szerokości), kierunek `dir` (poziomy), n wzniosów.
    /// Zwraca punkt dolny (środek, poziom dolnej posadzki, przy ostatnim wzniosie).</summary>
    static Vector3 Bieg(Vector3 gora, Vector3 dir, float szer, int n, float wz, float st, bool spod = true)
    {
        dir = dir.normalized; var bok = Vector3.Cross(Vector3.up, dir).normalized;
        var q = Quaternion.LookRotation(dir);
        for (int i = 0; i < n - 1; i++)
        {
            float y = gora.y - (i + 1) * wz;
            var c = gora + dir * ((i + .5f) * st); c.y = y - wz * .5f - .06f;
            W.OBox(mStopien, c, new Vector3(szer, wz + .12f, st), q, false, true);
            // nosek: pas antypoślizgowy przy krawędzi (od strony wchodzącego)
            var nc = gora + dir * ((i + 1) * st - .035f); nc.y = y + .004f;
            W.OBox(mNosek, nc, new Vector3(szer - .08f, .008f, .05f), q);
        }
        var nosekGora = gora + dir * -.035f; nosekGora.y = gora.y + .004f;
        W.OBox(mNosek, nosekGora, new Vector3(szer - .08f, .008f, .05f), q);
        var dol = gora + dir * ((n - 1) * st); dol.y = gora.y - n * wz;
        // płyta biegu (spód schodów) — skośna, betonowa okładzina
        if (spod)
        {
            var a = gora + dir * 0f + Vector3.down * .32f; var b = dol + Vector3.down * .02f;
            var sr = (a + b) * .5f; var dl = (b - a);
            var qs = Quaternion.LookRotation(dl.normalized, Vector3.Cross(dl, bok).normalized.y > 0 ? Vector3.Cross(dl, bok).normalized : -Vector3.Cross(dl, bok).normalized);
            W.OBox(mSciana, sr - (qs * Vector3.up) * .1f, new Vector3(szer, .2f, dl.magnitude), qs);
        }
        W.Rampa(gora, dol + dir * st, szer);
        return dol;
    }

    static void Porecz(Vector3 a, Vector3 b, float wys, bool zakoncz = true)
    {
        var A0 = a + Vector3.up * wys; var B0 = b + Vector3.up * wys;
        W.Rura(mStal, A0, B0, .022f * K, 10);
        float L = Vector3.Distance(A0, B0); int nWs = Mathf.Max(2, Mathf.CeilToInt(L / 1.4f) + 1);
        var bokW = Vector3.Cross(Vector3.up, (b - a)).normalized;
        for (int i = 0; i < nWs; i++)
        {
            var p = Vector3.Lerp(A0, B0, i / (float)(nWs - 1));
            // wspornik do ściany (ściana po stronie `bokW` lub przeciwnej — krótki pręt)
            W.Rura(mStal, p, p - Vector3.up * .08f, .009f * K, 6);
        }
        if (zakoncz)
        {
            var poz = new Vector3(A0.x - B0.x, 0, A0.z - B0.z).normalized;   // zakończenia poziome (jak w prawdziwych poręczach)
            W.Rura(mStal, A0, A0 + poz * .3f, .022f * K, 10);
            W.Rura(mStal, B0, B0 - poz * .3f, .022f * K, 10);
        }
    }

    // ---------------- wejścia z ulicy ----------------
    static void Wejscie(float z0, bool polnoc)
    {
        // pkt 58: pas dzielony — schody stałe (płd. część) i obok schody ruchome (płn. część, jadą w dół)
        float zc = z0 + EW * .5f, zcS = ZcSchodow(z0), szer = SzerSchodow(z0);
        float wz = (yS - yM) / 23f, st = .30f * K;
        var dir = Vector3.right;
        var g1 = new Vector3(EX0, yS, zcS);
        var s1 = Bieg(g1, dir, szer, 12, wz, st);              // bieg 1: 12 wzniosów
        float spocz = 1.5f * K;
        var g2 = s1 + dir * spocz;                            // spocznik
        W.MM(mStopien, s1.x, g2.x, s1.y - .32f, s1.y, zcS - szer * .5f, zcS + szer * .5f);
        W.KolizjaMM(s1.x - .2f, g2.x, s1.y - .3f, s1.y, zcS - szer * .5f, zcS + szer * .5f);
        var d2 = Bieg(g2, dir, szer, 11, wz, st);              // bieg 2: 11 wzniosów → antresola
        float xEnd = d2.x;                                     // ~76.6
        try { SchodyRuchome("metro: schody ruchome chodnik → antresola", EX0 - .9f, 1f, yS, yM, ER_Os(z0), ER_STOPIEN, false, 30f, true); }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_SCHODY_RUCHOME chodnik → antresola DO POPRAWKI " + e.Message); }
        // ściany boczne szybu nad terenem: murek z granitu + balustrada szklana z pochwytem (1.1·k)
        float hBal = 1.1f * K, hMur = .55f;
        foreach (float s in new[] { -1f, 1f })
        {
            float zw = zc + s * (EW * .5f - .075f);
            W.MM(mLamperia, EX0 - .05f, xEnd + .2f, yS - .02f, yS + hMur, zw - .075f, zw + .075f, true);
            W.MM(mPosadzkaJasna, EX0 - .08f, xEnd + .23f, yS + hMur, yS + hMur + .05f, zw - .11f, zw + .11f);
            W.MM(mSzklo, EX0 + .2f, xEnd, yS + hMur + .05f, yS + hBal - .04f, zw - .012f, zw + .012f);
            W.Rura(mStal, new Vector3(EX0 - .05f, yS + hBal, zw), new Vector3(xEnd + .2f, yS + hBal, zw), .028f * K, 10);
            for (float x = EX0 + .2f; x <= xEnd + .05f; x += 1.6f)
                W.Rura(mStal, new Vector3(x, yS + hMur + .05f, zw), new Vector3(x, yS + hBal, zw), .018f * K, 8);
            W.KolizjaMM(EX0 - .05f, xEnd + .2f, yS, yS + hBal, zw - .08f, zw + .08f);
            // poręcze przy biegach (0.9·k nad noskami), po obu stronach
            float zp = zcS + s * (szer * .5f - .07f);
            Porecz(new Vector3(EX0 - .3f, yS, zp), new Vector3(s1.x, s1.y + wz, zp), .9f * K);
            Porecz(new Vector3(g2.x, g2.y, zp), new Vector3(xEnd, yM + wz, zp), .9f * K);
        }
        // murek z balustradą na dolnym końcu otworu (poprzecznie)
        W.MM(mLamperia, xEnd + .05f, xEnd + .2f, yS - .02f, yS + hMur, z0, z0 + EW, true);
        W.MM(mSzklo, xEnd + .11f, xEnd + .14f, yS + hMur, yS + hBal, z0 + .15f, z0 + EW - .15f);
        W.Rura(mStal, new Vector3(xEnd + .125f, yS + hBal, z0), new Vector3(xEnd + .125f, yS + hBal, z0 + EW), .028f * K, 10);
        W.KolizjaMM(xEnd, xEnd + .25f, yS, yS + hBal, z0, z0 + EW);
        // wiata: stalowa rama + szkło, nad całym zejściem (od chodnika do końca otworu)
        float wx0 = EX0 - .5f, wx1 = xEnd + .35f, hF = 4.2f, hT = 3.75f;
        foreach (float s in new[] { -1f, 1f })
        {
            float zs = zc + s * (EW * .5f + .02f);
            foreach (float x in new[] { EX0 - .1f, (wx0 + wx1) * .5f, wx1 - .15f })
            {
                float h = Mathf.Lerp(hF, hT, (x - wx0) / (wx1 - wx0));
                W.MM(mStalCiemna, x - .09f, x + .09f, yS, yS + h, zs - .09f, zs + .09f, true);
                W.MM(mStalCiemna, x - .16f, x + .16f, yS, yS + .02f, zs - .16f, zs + .16f);
            }
            // podłużnica
            var a = new Vector3(wx0, yS + hF, zs); var b = new Vector3(wx1, yS + hT, zs);
            W.OBox(mStalCiemna, (a + b) * .5f + Vector3.up * .1f, new Vector3(.16f, .22f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
        }
        var A0 = new Vector3(wx0, yS + hF + .22f, zc); var B0 = new Vector3(wx1, yS + hT + .22f, zc);
        var qd = Quaternion.LookRotation(B0 - A0);
        float Ld = Vector3.Distance(A0, B0);
        W.OBox(mSzklo, (A0 + B0) * .5f + Vector3.up * .02f, new Vector3(EW + .5f, .03f, Ld + .2f), qd);
        for (int i = 0; i <= 6; i++)
        {
            var p = Vector3.Lerp(A0, B0, i / 6f);
            W.OBox(mStalCiemna, p, new Vector3(EW + .5f, .1f, .08f), qd);
        }
        // czoło wiaty (od chodnika): pas z nazwą stacji (podświetlany) i logo M
        var czolo = new Vector3(wx0 - .06f, yS + hF + .02f, zc);
        W.MM(mStalCiemna, wx0 - .1f, wx0 + .05f, yS + hF - .48f, yS + hF + .38f, zc - EW * .5f - .25f, zc + EW * .5f + .25f);
        Tab(czolo + new Vector3(-.03f, -.05f, .35f), Vector3.left, EW - .4f, (EW - .4f) / 6f, aNazwa);
        Tab(czolo + new Vector3(-.03f, -.05f, -EW * .5f + .05f), Vector3.left, .7f, .7f, aLogo);
        // pod wiatą: oprawy LED i światło (zejście jasne także nocą)
        foreach (float x in new[] { wx0 + 1.4f, (wx0 + wx1) * .5f + 1.0f, wx1 - 1.2f })
        {
            float h = Mathf.Lerp(hF, hT, (x - wx0) / (wx1 - wx0));
            W.MM(mKlosz, x - .6f, x + .6f, yS + h + .1f, yS + h + .13f, zc - .12f, zc + .12f);
        }
        W.Lampa("metro: światło zejścia " + (polnoc ? "płn." : "płd."), new Vector3((wx0 + wx1) * .5f, 2.2f, zc), .9f, 10f);
        // pylon z „M” (czerwona kostka z logo na 4 stronach) i nazwą stacji
        var pyl = new Vector3(EX0 + .4f, yS, polnoc ? z0 + EW + .75f : z0 - .75f);
        float hP = 3.6f * K;
        W.MM(mStalCiemna, pyl.x - .2f, pyl.x + .2f, yS, yS + hP, pyl.z - .2f, pyl.z + .2f, true);
        W.MM(mStalCiemna, pyl.x - .3f, pyl.x + .3f, yS, yS + .05f, pyl.z - .3f, pyl.z + .3f);
        float kM = .95f; var kc = new Vector3(pyl.x, yS + hP + kM * .5f, pyl.z);
        W.Box(mBelka, kc, new Vector3(kM, kM, kM));
        foreach (var n in new[] { Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
            Tab(kc + n * (kM * .5f + .004f), n, kM - .04f, kM - .04f, aLogo);
        // tablica z nazwą stacji na pylonie (od strony ulicy i od bulwaru)
        foreach (var n in new[] { Vector3.left, Vector3.right })
            Tab(new Vector3(pyl.x, yS + 2.6f, pyl.z) + n * .205f, n, 1.4f, 1.4f / 6f, aNazwa, true);
        W.Napis("METRO", new Vector3(pyl.x - .21f, yS + 2.2f, pyl.z), Vector3.left, .22f, Color.white);
        W.Napis("METRO", new Vector3(pyl.x + .21f, yS + 2.2f, pyl.z), Vector3.right, .22f, Color.white);
        // posadzka szybu na dole otworu (pas spocznika antresoli pod gołym niebem nie istnieje — dół jest pod stropem)
    }

    /// <summary>Szyb schodów poniżej terenu: ściany okładzinowe, sufit nad niskim końcem, oprawy, oznakowanie.</summary>
    static void ZejscieWnetrze(float z0, bool polnoc)
    {
        float zc = z0 + EW * .5f;
        float xEnd = EX0 + (12 - 1 + 11 - 1) * .30f * K + 1.5f * K;
        foreach (float s in new[] { -1f, 1f })
        {
            float zw = zc + s * (EW * .5f - .075f);
            // ściana szybu od posadzki antresoli do terenu (okładzina), cokół granitowy
            W.MM(mSciana, EX0 - .05f, xEnd + .2f, yM, yS - .02f, zw - .075f, zw + .075f, true);
            W.MM(mLamperia, EX0 - .05f, xEnd + .2f, yM, yM + .25f, zw - .08f, zw + .08f);
        }
        // ściana czołowa pod chodnikiem (górny koniec, x = EX0)
        W.MM(mSciana, EX0 - .3f, EX0 - .05f, yM, yS - .02f, z0, z0 + EW, true);
        // tablica nad wejściem na dole schodów (widziana z antresoli): kierunek wyjścia
        // podciąg nad dolnym końcem biegu (krawędź otworu w stropie) z znakiem wyjścia widocznym ze spocznika
        W.MM(mBelka, xEnd, xEnd + .25f, yMc - .35f, yS - .05f, z0, z0 + EW);
        Tab(new Vector3(xEnd + .255f, yMc - .2f, zc), Vector3.right, .5f, .25f, aEwak);
        // tablica kierunku wyjścia wisząca nad przejściem z hali na spocznik (widziana od zachodu)
        float zH = polnoc ? z0 - .9f : z0 + EW + .9f;
        Wiszaca(new Vector3(XKoniec + 1.0f, yMc - .55f, zH), Vector3.left, 2.4f, .3f, polnoc ? aWyjN : aWyjS, polnoc ? aWyjN : aWyjS, yMc);
    }

    // ---------------- antresola (hala biletowa) ----------------
    static void Antresola()
    {
        // posadzka z otworem schodów (WX0..WX1 × WZ0..WZ1); strop pod spodem = sufit hali peronowej
        void Plyta(float x0, float x1, float z0, float z1)
        {
            if (x1 - x0 < .01f || z1 - z0 < .01f) return;
            W.MM(mPosadzka, x0, x1, yM - .4f, yM, z0, z1, true);
        }
        Plyta(MX0, MX1, MZ0, WZ0); Plyta(MX0, MX1, WZ1, MZ1); Plyta(MX0, WX0, WZ0, WZ1); Plyta(WX1, MX1, WZ0, WZ1);
        // sufit antresoli (panele) z pasami opraw LED
        float xEndS = EX0 + (12 - 1 + 11 - 1) * .30f * K + 1.5f * K;
        W.MM(mSufit, MX0, MX1, yMc, yMc + .05f, EZ[0] + EW, EZ[1]);
        foreach (float z0 in EZ)
        {
            W.MM(mSufit, MX0, EX0 - .3f, yMc, yMc + .05f, z0, z0 + EW);
            W.MM(mSufit, xEndS, MX1, yMc, yMc + .05f, z0, z0 + EW);
        }
        W.MM(mSciana, xEndS + .2f, MX1 + .3f, yM, yMc, MZ1, MZ1 + .3f, true);
        W.MM(mSciana, xEndS + .2f, MX1 + .3f, yM, yMc, MZ0 - .3f, MZ0, true);
        for (float x = MX0 + 1.5f; x < MX1 - 1f; x += 3.2f)
            foreach (float zL in new[] { 23.0f, 28.45f, 34.0f })
            {
                bool wPasie = (zL < EZ[0] + EW || zL > EZ[1]) && x + 1.1f > EX0 - .3f && x - 1.1f < xEndS;   // zejście albo zamknięte pom. techniczne
                if (!wPasie) W.MM(mKlosz, x - 1.1f, x + 1.1f, yMc - .02f, yMc, zL - .09f, zL + .09f);
            }
        for (float x = MX0 + 3f; x < MX1; x += 6f)
        {
            if (x > EX0 - .5f && x < xEndS + .3f) { W.MM(mBelka, x - .2f, x + .2f, yMc - .35f, yMc, EZ[0] + EW, EZ[1]); continue; }
            W.MM(mBelka, x - .2f, x + .2f, yMc - .35f, yMc, MZ0, MZ1);
        }
        // ściany: zachodnia, wschodnia, północna i południowa (z wyjątkiem pasów zejść, które mają własne ściany)
        W.MM(mSciana, MX0 - .3f, MX0, yM, yMc, MZ0 - .3f, MZ1 + .3f, true);
        W.MM(mSciana, MX1, MX1 + .3f, yM, yMc, MZ0 - .3f, MZ1 + .3f, true);
        W.MM(mSciana, MX0, EX0 - .3f, yM, yMc, MZ1, MZ1 + .3f, true);
        W.MM(mSciana, MX0, EX0 - .3f, yM, yMc, MZ0 - .3f, MZ0, true);
        // ściany obudowy zejść od strony hali (pod biegami schodów jest zamknięta przestrzeń techniczna)
        float xEnd = EX0 + (12 - 1 + 11 - 1) * .30f * K + 1.5f * K;
        W.MM(mSciana, EX0 - .3f, xEnd, yM, yMc, EZ[0] + EW, EZ[0] + EW + .25f, true);
        W.MM(mSciana, EX0 - .3f, xEnd, yM, yMc, EZ[1] - .25f, EZ[1], true);
        foreach (float zf in new[] { EZ[0] + EW + .25f, EZ[1] - .25f })
            W.MM(mLamperia, EX0 - .3f, xEnd, yM, yM + .3f, zf - .02f, zf + .02f);
        foreach (float z0 in EZ) if (!Wejscie(z0)) PasZamkniety(z0, xEnd, z0 > 28f);
        // cokół na ścianach zewnętrznych
        W.MM(mLamperia, MX0, EX0, yM, yM + .3f, MZ1 - .02f, MZ1);
        W.MM(mLamperia, MX0, EX0, yM, yM + .3f, MZ0, MZ0 + .02f);
        // balustrada wokół otworu schodów (szkło 1.1·k + pochwyt), bez boku wschodniego (wejście na schody i ruchome)
        float hB = 1.1f * K;
        void Bal(Vector3 a, Vector3 b)
        {
            var c = (a + b) * .5f; var d = b - a; var q = Quaternion.LookRotation(d.normalized);
            W.OBox(mSzklo, c + Vector3.up * (hB * .5f), new Vector3(.025f, hB - .05f, d.magnitude), q);
            W.Rura(mStal, a + Vector3.up * hB, b + Vector3.up * hB, .026f * K, 10);
            W.OBox(mStal, c + Vector3.up * .04f, new Vector3(.05f, .08f, d.magnitude), q);
            W.Kolizja(c + Vector3.up * hB * .5f, new Vector3(.08f, hB, d.magnitude), q);
        }
        Bal(new Vector3(WX0, yM, WZ0), new Vector3(WX1, yM, WZ0));
        Bal(new Vector3(WX0, yM, WZ1), new Vector3(WX1, yM, WZ1));
        Bal(new Vector3(WX0, yM, WZ0), new Vector3(WX0, yM, WZ1));
        Bal(new Vector3(WX1 - .05f, yM, 28.82f), new Vector3(WX1 - .05f, yM, 28.92f));
        // obramowanie krawędzi otworu (czoło stropu)
        W.MM(mBelka, WX0 - .02f, WX1, yM - .45f, yM - .4f, WZ0 - .02f, WZ0);
        W.MM(mBelka, WX0 - .02f, WX1, yM - .45f, yM - .4f, WZ1, WZ1 + .02f);

        // ---- bramki (linia GX) ----
        float hG = 1.0f * K, pas = .95f, korpus = .28f, dlug = 1.6f * K;
        float zA = 23.6f; int nPrz = 7;
        float z = zA;
        var listaBramek = new List<(float z0, float z1)>();
        // etap „assety” 2.11: bramki z modelu (ericdavis, CC-BY, paczka miasto_silownia_metro) ZAMIAST obudów i szybek z brył —
        // stare nie powstają; brak paczki = bramki z kodu jak dotąd
        bool bramkiModel = false; float zBm = 0f;
        try { bramkiModel = BraxMiastoAssety.BramkiMetra(podziemie, GX, yM, zA + ((nPrz + 1) * korpus + nPrz * pas) * .5f, nPrz, out float zAm, out zBm); if (bramkiModel) zA = zAm; }
        catch (Exception e) { Debug.LogError("BRAXHORN_MIASTO_ASSETY METRO bramki z modelu DO POPRAWKI: " + e); bramkiModel = false; }
        for (int i = 0; i <= nPrz && !bramkiModel; i++)
        {
            // obudowa bramki (stal + szkło górne, wyświetlacz)
            W.MM(mStal, GX - dlug * .5f, GX + dlug * .5f, yM, yM + hG, z, z + korpus, true);
            W.MM(mCzarny, GX - dlug * .5f + .1f, GX + dlug * .5f - .1f, yM + hG, yM + hG + .02f, z + .02f, z + korpus - .02f);
            Tab(new Vector3(GX + dlug * .5f + .002f, yM + hG - .15f, z + korpus * .5f), Vector3.right, .16f, .16f, i == 3 ? aBramkaX : aBramkaOK);
            Tab(new Vector3(GX - dlug * .5f - .002f, yM + hG - .15f, z + korpus * .5f), Vector3.left, .16f, .16f, aBramkaOK);
            W.MM(mCzarny, GX + .15f, GX + .45f, yM + hG + .02f, yM + hG + .04f, z + .04f, z + korpus - .04f);   // czytnik kart
            if (i < nPrz) { listaBramek.Add((z + korpus, z + korpus + pas)); z += korpus + pas; }
        }
        float zB = bramkiModel ? zBm : z + korpus;
        // szybki bramek: dwa skrzydła w każdym przejściu, chowają się w obudowy, gdy gracz podchodzi
        int nb = 0;
        foreach (var (p0, p1) in listaBramek)   // (pusta przy bramkach z modelu)
        {
            var g = new GameObject("bramka biletowa " + (++nb)); g.transform.SetParent(podziemie, false);
            g.transform.position = new Vector3(GX, yM, (p0 + p1) * .5f);
            var drz = g.AddComponent<BraxDrzwiPrzesuwne>(); drz.etykieta = "bramka metra " + nb; drz.promien = 1.9f; drz.czasRuchu = .35f;
            var sk = new List<Transform>(); var ps = new List<Vector3>();
            foreach (float s in new[] { -1f, 1f })
            {
                var leaf = new GameObject("skrzydło"); leaf.transform.SetParent(g.transform, false);
                leaf.transform.localPosition = new Vector3(0, .62f * K, s * pas * .25f);
                var mf = leaf.AddComponent<MeshFilter>(); var mr = leaf.AddComponent<MeshRenderer>();
                mf.sharedMesh = SkrzydloMesh(new Vector3(.03f, .75f * K, pas * .5f - .02f)); mr.sharedMaterial = mSzklo;
                sk.Add(leaf.transform); ps.Add(new Vector3(0, 0, s * (pas * .5f - .02f)));
            }
            drz.skrzydla = sk.ToArray(); drz.przesuniecie = ps.ToArray();
            var blok = new GameObject("blokada przejścia"); blok.transform.SetParent(g.transform, false);
            var bc = blok.AddComponent<BoxCollider>(); bc.size = new Vector3(.1f, 1.2f, pas); bc.center = new Vector3(0, .6f, 0);
            drz.blokady = new Collider[] { bc };
        }
        // przegrody szklane od bramek do ścian (z MZ0…zA i zB…MZ1)
        foreach (var (a0, a1) in new[] { (MZ0, zA), (zB, MZ1) })
        {
            W.MM(mSzklo, GX - .02f, GX + .02f, yM + .05f, yM + hB, a0 + .05f, a1 - .02f, true);
            W.Rura(mStal, new Vector3(GX, yM + hB, a0), new Vector3(GX, yM + hB, a1), .026f * K, 10);
            for (float zz = a0 + .05f; zz <= a1; zz += 1.3f) W.Rura(mStal, new Vector3(GX, yM, zz), new Vector3(GX, yM + hB, zz), .02f * K, 8);
        }
        // tablice nad bramkami: od strony wejścia — PERONY; od strony peronów — WYJŚCIA
        Wiszaca(new Vector3(GX + .6f, yMc - .75f, 26.0f), Vector3.right, 3.2f, .4f, aPerony, aWyjS, yMc);
        Wiszaca(new Vector3(GX + .6f, yMc - .75f, 31.4f), Vector3.right, 3.2f, .4f, aPerony, aWyjN, yMc);

        // ---- strefa biletowa (x GX…MX1, między obudowami zejść z 23.9…32.6) ----
        // biletomaty: 2 przy obudowie płd. (frontem na płn.), 2 przy obudowie płn. (frontem na płd.)
        int nbil = 0;
        foreach (var (zf, n) in new[] { (EZ[0] + EW + .25f, Vector3.forward), (EZ[1] - .25f, Vector3.back) })
            foreach (float x in new[] { 68.4f, 70.0f })
            {
                float w = .8f * K, d = .5f * K, h = 1.85f * K;
                var c = new Vector3(x, yM + h * .5f, zf + n.z * d * .5f);
                W.Box(mStalCiemna, c, new Vector3(w, h, d), true);
                W.Box(mStal, new Vector3(x, yM + h - .06f, zf + n.z * d * .5f), new Vector3(w + .02f, .12f, d + .02f));
                var fr = new Vector3(x, yM + 1.45f * K, zf + n.z * (d + .003f));
                Tab(fr, n, .55f * K, .55f * K, aBiletomat);
                W.Box(mCzarny, new Vector3(x - .2f, yM + 1.02f * K, zf + n.z * (d + .01f)), new Vector3(.18f, .06f, .02f));   // wydawanie biletów
                W.Box(mStal, new Vector3(x + .22f, yM + 1.12f * K, zf + n.z * (d + .015f)), new Vector3(.14f, .2f, .03f));   // terminal kart
                W.Box(mCzarny, new Vector3(x, yM + .55f * K, zf + n.z * (d + .01f)), new Vector3(.35f, .08f, .02f));          // odbiór reszty
                Tab(new Vector3(x, yM + h + .22f, zf + n.z * (d * .5f + .02f)), n, w, w / 4f, aKasy);
                nbil++;
                W.Akcja("metro_biletomat_" + nbil, "KUP BILET", "Bilet 20 min — 4,00 zł. Skasowany, miłej podróży.", new Vector3(x, yM, zf + n.z * (d + .7f)), null, 1.2f);
            }
        // kasa biletowa (okienko) przy ścianie wschodniej: przeszklony front na zachód
        float kx0 = MX1 - 1.8f, kx1 = MX1, kz0 = 26.4f, kz1 = 30.6f;
        W.MM(mStalCiemna, kx0, kx0 + .12f, yM, yM + 1.05f * K, kz0, kz1, true);                   // lada (front)
        W.MM(mPosadzkaJasna, kx0 - .25f, kx0 + .5f, yM + 1.05f * K, yM + 1.05f * K + .05f, kz0, kz1); // blat
        W.MM(mSzklo, kx0 + .04f, kx0 + .08f, yM + 1.05f * K + .05f, yMc - .5f, kz0 + .1f, kz1 - .1f, true);
        W.MM(mStalCiemna, kx0, kx0 + .12f, yMc - .5f, yMc, kz0, kz1);
        W.MM(mSciana, kx0, kx1, yM, yMc, kz0 - .15f, kz0, true); W.MM(mSciana, kx0, kx1, yM, yMc, kz1, kz1 + .15f, true);
        Tab(new Vector3(kx0 - .005f, yMc - .25f, (kz0 + kz1) * .5f), Vector3.left, 3.6f, .6f, aKasa);
        W.MM(mStalCiemna, kx1 - .45f, kx1, yM, yM + 2.0f * K, kz0 + .2f, kz1 - .2f);                       // szafa na bilety / sejf
        W.MM(mCzarny, kx0 + .25f, kx0 + .3f, yM + 1.05f * K + .05f, yM + 1.05f * K + .45f, 28.0f, 28.8f);   // monitor kasy
        W.MM(mCzarny, kx0 + .15f, kx0 + .4f, yM + 1.05f * K + .05f, yM + 1.05f * K + .08f, 27.2f, 27.7f);   // drukarka biletów
        var kasjer = W.Obsada("kasjer metra", new Vector3(kx0 + .95f, yM, 28.4f), 270f);
        W.Akcja("metro_kasa", "POROZMAWIAJ", "Kasa biletowa: bilety 20 min, 75 min, 24 h i karta miejska. Pociąg do OSIEDLA stoi na peronie 1.", new Vector3(kx0 - .8f, yM, 28.4f), kasjer, 1.6f);
        W.Lampa("metro: światło kasy", new Vector3(kx1 - .7f, yMc - .4f, 28.4f), .5f, 6f);
        // mapa linii i tablica odjazdów w strefie biletowej (na ścianach obudów)
        Tab(new Vector3(72.4f, yM + 1.75f * K, EZ[0] + EW + .26f), Vector3.forward, 2.4f, .9f, aMapa);
        Tab(new Vector3(72.4f, yM + 1.75f * K, EZ[1] - .26f), Vector3.back, 2.4f, .9f, aMapa);
        Tab(new Vector3(75.0f, yM + 1.9f * K, EZ[1] - .26f), Vector3.back, 1.8f, .675f, aOdjazdy);
        Tab(new Vector3(75.0f, yM + 1.9f * K, EZ[0] + EW + .26f), Vector3.forward, 1.8f, .45f, aArena);
        // nazwa stacji na ścianie wschodniej nad kasą
        Tab(new Vector3(MX1 - .01f, yMc - .45f, 34.2f), Vector3.left, 2.4f, .4f, aNazwa);
        // kosze, gaśnica, kamery, ewakuacja
        Kosz(new Vector3(71.8f, yM, 24.5f)); Kosz(new Vector3(71.8f, yM, 32.0f)); Kosz(new Vector3(60.4f, yM, 21.2f));
        Gasnica(new Vector3(MX1 - .02f, yM, 22.6f), Vector3.left);
        Gasnica(new Vector3(MX0 + .02f, yM, 33.6f), Vector3.right);
        Kamera(new Vector3(MX1 - .4f, yMc - .1f, 24.2f), new Vector3(-1, -.6f, .4f));
        Kamera(new Vector3(GX - .5f, yMc - .1f, 21.0f), new Vector3(.3f, -.6f, 1f));
        Kamera(new Vector3(MX0 + .4f, yMc - .1f, 35.5f), new Vector3(1f, -.6f, -.6f));
        Tab(new Vector3(MX0 + .01f, yM + 2.2f, 28.4f), Vector3.right, 2.0f, .5f, aMonitoring);
        // ławki w strefie płatnej (przy ścianie płn.)
        Lawka(new Vector3(50.5f, yM, MZ1 - .55f), Vector3.back); Lawka(new Vector3(54.5f, yM, MZ1 - .55f), Vector3.back);
        // światła antresoli (siatka 7 × 2)
        for (float x = MX0 + 3f; x < MX1; x += 6f)
            foreach (float zz in new[] { 23.2f, 33.4f })
                W.Lampa("metro: światło antresoli", new Vector3(x, yMc - .45f, zz), .55f, 9f);
        // miejsce obsady: pracownik ochrony metra przy bramkach
        W.Obsada("ochrona metra przy bramkach", new Vector3(GX + 1.4f, yM, 21.6f), 0f);
        W.Akcja("metro_ochrona", "POROZMAWIAJ", "Ochrona metra: proszę skasować bilet przy bramce. Peron 1 — pociąg do OSIEDLA.", new Vector3(GX + 1.4f, yM, 22.6f), null, 1.4f);
    }

    static Mesh SkrzydloMesh(Vector3 s)
    {
        var sm = new BraxCitySystemMesh(); sm.Box(Vector3.zero, s);
        var m = sm.Build("skrzydło bramki"); Directory.CreateDirectory(W.Out);
        BraxZapisAssetu.Utworz(m, AssetDatabase.GenerateUniqueAssetPath(W.Out + "/skrzydlo_bramki.asset"));
        return m;
    }

    // ---------------- schody stałe i ruchome antresola → peron ----------------
    static void SchodyNaPeron()
    {
        float szer = 3.1f, zc = 25.6f + szer * .5f;   // schody z 25.6–28.7
        float wz = (yM - yP) / 32f, st = .30f * K;
        var g1 = new Vector3(WX1, yM, zc);
        var s1 = Bieg(g1, Vector3.left, szer, 16, wz, st);
        var g2 = s1 + Vector3.left * (1.5f * K);
        W.MM(mStopien, g2.x, s1.x, s1.y - .32f, s1.y, zc - szer * .5f, zc + szer * .5f);
        W.KolizjaMM(g2.x, s1.x + .2f, s1.y - .3f, s1.y, zc - szer * .5f, zc + szer * .5f);
        var d2 = Bieg(g2, Vector3.left, szer, 16, wz, st);
        // podpora spocznika i obudowa przestrzeni pod schodami (pom. techniczne): płyty okładzinowe od strony peronu
        float xBot = d2.x;
        void Obudowa(float zf, float s)
        {
            // pionowy panel pod biegiem: wysokość od peronu do spodu schodów (trapez z prostokątów co 1 m)
            for (float x = xBot + .5f; x < 58.2f; x += 1f)
            {
                float t = Mathf.InverseLerp(xBot, 58.2f, x + .5f);
                float yTop = Mathf.Lerp(yP, yM, t) - .35f;
                if (yTop - yP < .3f) continue;
                W.MM(mSciana, x, x + 1f, yP, yTop, zf - .06f, zf + .06f, true);
            }
            W.MM(mLamperia, xBot + .5f, 58.2f, yP, yP + .25f, zf + s * .07f - .01f, zf + s * .07f + .01f);
        }
        Obudowa(25.5f, -1f);
        // FILM 4.11: poprzednia PEŁNA ściana z=25.4..31.5 i x=54..58.4
        // przecinała oba biegi (schody stałe oraz ruchome) tuż przy antresoli.
        // Zamiast osłaniać przejście kolejną płytą, WYCOFUJEMY pełną ścianę.
        // Pozostają tylko boczne policzki poza faktycznym światłem przejścia:
        // schody stałe z=25.6..28.7, ruchome z=28.95..31.25.
        W.MM(mSciana, 54.0f, 58.4f, yP, yM - .4f, WZ0, 25.52f, true);
        W.MM(mSciana, 54.0f, 58.4f, yP, yM - .4f, 31.35f, WZ1, true);
        Debug.Log("BRAXHORN_METRO_PRZEJSCIE_SCHODY: usunięta pełna ściana techniczna; schody=25.6..28.7, ruchome=28.95..31.25 (prześwit bez paneli)");
        // drzwi techniczne pod schodami (od strony peronu płd.)
        W.MM(mDrzwiTech, 55.2f, 56.3f, yP, yP + 2.1f * K, 25.41f, 25.44f);
        W.MM(mStal, 56.15f, 56.2f, yP + 1.0f * K, yP + 1.05f * K, 25.38f, 25.41f);
        W.Napis("POMIESZCZENIE TECHNICZNE", new Vector3(55.75f, yP + 2.1f * K + .2f, 25.39f), Vector3.back, .1f, Color.white);
        // poręcze: przy ścianie (płd.) i przy przegrodzie (płn.)
        foreach (float zp in new[] { zc - szer * .5f + .07f, zc + szer * .5f - .07f })
        {
            Porecz(new Vector3(g1.x + .3f, yM, zp), new Vector3(s1.x, s1.y, zp), .9f * K);
            Porecz(new Vector3(g2.x, g2.y, zp), new Vector3(d2.x - .3f, yP, zp), .9f * K);
        }
        // balustrada boczna płd. (szkło na biegu)
        void BalSkos(Vector3 a, Vector3 b, float z)
        {
            a.z = z; b.z = z; float h = 1.0f * K; var c = (a + b) * .5f + Vector3.up * h * .5f; var d = b - a;
            W.OBox(mSzklo, c, new Vector3(.025f, h - .05f, d.magnitude), Quaternion.LookRotation(d.normalized));
            W.Kolizja(c, new Vector3(.08f, h, d.magnitude), Quaternion.LookRotation(d.normalized));
        }
        BalSkos(g1, s1, 25.52f); BalSkos(g2, d2, 25.52f);
        // Poprzeczny pas stali z podłogi między schodami a ruchomymi zdjęty.
        // Schody mają osobne balustrady; łącznik na dolnym spoczniku jest wolny.
        Debug.Log("BRAXHORN_METRO_PRZEJSCIE_SCHODY: przejście bez poprzecznej listwy/murku");

        // schody ruchome (jadą w górę): z 28.95–31.25, nachylenie 30° — pkt 58: wspólny wykonawca SchodyRuchome (jazda, poręcz UV, dźwięk)
        float ez0 = 28.95f, ez1 = 31.25f, ezc = (ez0 + ez1) * .5f, stepW = 1.0f * K;
        float xTop = 58.6f, xTopPlat = xTop - 1.3f, xBotPlat = xTopPlat - (yM - yP) / Mathf.Tan(30f * Mathf.Deg2Rad);
        try { SchodyRuchome("metro: schody ruchome antresola → peron", xTop, -1f, yM, yP, ezc, stepW, true, 30f, false); }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_SCHODY_RUCHOME antresola → peron DO POPRAWKI " + e.Message); }
        foreach (float x in new[] { xBotPlat + 1f, (xBotPlat + xTopPlat) * .5f })
        {
            float t = Mathf.InverseLerp(xBotPlat, xTopPlat, x); float yy = Mathf.Lerp(yP, yM, t) - 1.0f;
            if (yy - yP > .4f) W.MM(mStalCiemna, x - .15f, x + .15f, yP, yy, ezc - .15f, ezc + .15f, true);
        }
        // tablice nad schodami na peronie: PERON 1 / PERON 2 + strzałki do wyjścia
        Wiszaca(new Vector3(xBot - 1.5f, yHc - .6f, 26.4f), Vector3.left, 1.2f, .6f, aPeron2, aPeron2, yHc);
        Wiszaca(new Vector3(xBot - 1.5f, yHc - .6f, 30.6f), Vector3.left, 1.2f, .6f, aPeron1, aPeron1, yHc);
        Wiszaca(new Vector3(xBot - 3.6f, yHc - .65f, 28.5f), Vector3.left, 3.2f, .4f, aWyjN, aWyjS, yHc);
    }

    // ---------------- schody ruchome (pkt 58): wspólny wykonawca dla wejścia z chodnika i antresola → peron ----------------
    static readonly float ER_STOPIEN = .8f * K;                                                  // stopień 800 mm × K
    static float ER_Os(float z0) => z0 + EW - .17f - .24f - ER_STOPIEN * .5f;          // oś schodów ruchomych w pasie wejścia (płn. część)
    static float SchodyZ1(float z0) => ER_Os(z0) - ER_STOPIEN * .5f - .24f - .085f;    // płn. krawędź schodów stałych (osłona między)
    static float ZcSchodow(float z0) => (z0 + .15f + SchodyZ1(z0)) * .5f;
    static float SzerSchodow(float z0) => SchodyZ1(z0) - (z0 + .15f);
    static Material mPoreczRuchoma, mStopienZolty;
    public static int SchodyRuchomeLiczba;

    static void MMx(Material m, float xa, float xb, float y0, float y1, float z0, float z1, bool kol = false)
        => W.MM(m, Mathf.Min(xa, xb), Mathf.Max(xa, xb), y0, y1, z0, z1, kol);

    static Material PoreczRuchoma()
    {
        if (mPoreczRuchoma) return mPoreczRuchoma;
        try
        {
            string dir = BraxMiastoA.Out + "/Metro"; Directory.CreateDirectory(dir);
            var tex = new Texture2D(64, 8, TextureFormat.RGBA32, true) { name = "metro poręcz ruchoma — guma ze znakiem", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[64 * 8];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 64; x++)
                {
                    byte c = 15;
                    if (x < 2) c = 7;                                                    // szew taśmy
                    if (x >= 22 && x < 42 && (y == 3 || y == 4)) c = 120;               // wytłoczony znak producenta (jasny pasek)
                    if ((x == 46 || x == 50) && y >= 2 && y <= 5) c = 120;
                    px[y * 64 + x] = new Color32(c, c, (byte)(c + 1), 255);
                }
            tex.SetPixels32(px); tex.Apply(true);
            BraxZapisAssetu.Utworz(tex, dir + "/porecz_ruchoma_guma.asset");
            var m = new Material(mPoreczGuma) { name = "metro poręcz ruchoma guma (przewijana)" };
            m.SetTexture("_MainTex", tex); m.SetColor("_Color", Color.white);
            BraxZapisAssetu.Utworz(m, dir + "/porecz_ruchoma.mat");
            mPoreczRuchoma = m;
        }
        catch (Exception e) { Debug.LogWarning("BRAXHORN_SCHODY_RUCHOME poręcz DO POPRAWKI " + e.Message); mPoreczRuchoma = mPoreczGuma; }
        return mPoreczRuchoma;
    }

    /// <summary>Rura wzdłuż łamanej z UV wzdłuż długości (u = metry / okres) — poręcz przewijana.</summary>
    static Mesh RuraUV(List<Vector3> pts, float r, int boki, float okres, string nazwa)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float s = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            if (i > 0) s += Vector3.Distance(pts[i - 1], pts[i]);
            Vector3 d = i == 0 ? pts[1] - pts[0] : i == pts.Count - 1 ? pts[i] - pts[i - 1] : (pts[i + 1] - pts[i]).normalized + (pts[i] - pts[i - 1]).normalized;
            d.Normalize();
            var a = Vector3.Cross(d, Vector3.forward); if (a.sqrMagnitude < 1e-6f) a = Vector3.Cross(d, Vector3.up); a.Normalize();
            var b = Vector3.Cross(a, d).normalized;
            for (int k = 0; k <= boki; k++)
            {
                float f = k * Mathf.PI * 2f / boki; var nn = a * Mathf.Cos(f) + b * Mathf.Sin(f);
                v.Add(pts[i] + nn * r); n.Add(nn); uv.Add(new Vector2(s / okres, k / (float)boki));
            }
        }
        for (int i = 0; i + 1 < pts.Count; i++)
            for (int k = 0; k < boki; k++)
            {
                int a0 = i * (boki + 1) + k, a1 = a0 + 1, b0 = a0 + boki + 1, b1 = b0 + 1;
                t.Add(a0); t.Add(b0); t.Add(a1); t.Add(a1); t.Add(b0); t.Add(b1);
            }
        var m = new Mesh { name = nazwa };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    /// <summary>Schody ruchome wzdłuż osi x. xTop: zewnętrzny koniec płyty górnej, sx: kierunek „w dół biegu” wzdłuż x (±1).
    /// Taśma stopni z żółtymi krawędziami (przód + boki) przesuwana w pętli, podstopnice, płyty spocznikowe z grzebieniami
    /// (zęby nad stopniami), osłony boczne inox (kratownica), balustrada szklana, poręcz gumowa przewijana (UV) z zawrotami
    /// w głowicach, zaokrąglone noski osłon, spód kratownicy; kolizje: rampa biegu, płyty, balustrady. Jazda + dźwięk:
    /// BraxSchodyRuchomeJazda. oslonaPld: pionowa osłona pod kratownicą od strony −z (zamyka przestrzeń pod biegiem).</summary>
    static void SchodyRuchome(string nazwa, float xTop, float sx, float yTop, float yBot, float ezc, float stepW, bool wGore, float kat, bool oslonaPld)
    {
        float h = yTop - yBot, run = h / Mathf.Tan(kat * Mathf.Deg2Rad), plat = 1.3f;
        float xTopPlat = xTop + sx * plat, xBotPlat = xTopPlat + sx * run, xBotEnd = xBotPlat + sx * plat;
        var pT = new Vector3(xTopPlat, yTop, ezc); var pB = new Vector3(xBotPlat, yBot, ezc);
        float zA = ezc - stepW * .5f, zB = ezc + stepW * .5f;
        if (!mStopienZolty) mStopienZolty = BraxUliceKit.Mat("metro: pasy boczne stopni (żółte)", new Color(.88f, .70f, .06f), .75f);
        // 1) taśma stopni (jedzie): w górę stopnie 0…n (n schowany pod płytą dolną), w dół −1…n−1 (−1 schowany pod płytą górną)
        int nSt = Mathf.CeilToInt(run / (.4f * K * .866f));
        float dx = run / nSt, dy = h / nSt;
        var tasma = new GameObject(nazwa + " — taśma stopni (" + (wGore ? "jedzie w górę" : "jedzie w dół") + ")").transform; tasma.SetParent(W.Grupa, false);
        var sAl = new UlSiatka(); var sZo = new UlSiatka();
        void BoxX(UlSiatka u, float xa, float xb, float y0, float y1, float z0, float z1) => u.Box(new Vector3(Mathf.Min(xa, xb), y0, z0), new Vector3(Mathf.Max(xa, xb), y1, z1));
        for (int i = wGore ? 0 : -1; i <= (wGore ? nSt : nSt - 1); i++)
        {
            float xs = xTopPlat + sx * i * dx, xf = xs + sx * dx, y = yTop - (i + 1) * dy;
            float xp = xf - sx * .05f;                                                         // żółta krawędź przednia 5 cm
            BoxX(sAl, xp, xs, y - .02f, y, zA + .04f, zB - .04f);
            BoxX(sZo, xf, xp, y - .02f, y, zA + .04f, zB - .04f);
            BoxX(sZo, xf, xs, y - .02f, y, zA, zA + .04f);
            BoxX(sZo, xf, xs, y - .02f, y, zB - .04f, zB);
            BoxX(sAl, xs - .005f, xs + .005f, y, y + dy - .02f, zA, zB);                     // podstopnica do spodu stopnia wyżej
            for (float zr = zA + .1f; zr < zB - .1f; zr += .09f)                                // ryflowanie podstopnicy (żeberka)
                BoxX(sAl, xs, xs + sx * .012f, y + .02f, y + dy - .04f, zr, zr + .02f);
        }
        BraxMiastoA.Obiekt(tasma, nazwa + ": taśma (aluminium)", sAl, mAluminium);
        BraxMiastoA.Obiekt(tasma, nazwa + ": taśma (żółte krawędzie)", sZo, mStopienZolty);
        var ruch = tasma.gameObject.AddComponent<BraxSchodyRuchome>();
        ruch.krok = wGore ? new Vector3(-sx * dx, dy, 0f) : new Vector3(sx * dx, -dy, 0f); ruch.predkosc = .5f;
        // 2) płyty spocznikowe (aluminium) + płyty grzebieniowe (żółte) z zębami nad stopniami
        foreach (var (xe, xk, y, dk) in new[] { (xTop, xTopPlat, yTop, sx), (xBotEnd, xBotPlat, yBot, -sx) })
        {
            float xg = xk - dk * .18f;                                                          // płyta grzebieniowa 18 cm od linii stopni
            MMx(mAluminium, xe, xg, y - .02f, y + .01f, zA, zB);
            MMx(mZolty, xg, xk, y - .02f, y + .012f, zA, zB);
            for (float zt = zA + .015f; zt < zB - .01f; zt += .03f)
                MMx(mZolty, xk, xk + dk * .05f, y - .004f, y + .008f, zt - .004f, zt + .004f);
            for (float zr = zA + .06f; zr < zB - .05f; zr += .06f)                              // ryflowanie płyty
                MMx(mStal, xe + dk * .08f, xg - dk * .03f, y + .01f, y + .014f, zr, zr + .012f);
            MMx(mStal, xe - dk * .06f, xe, y - .02f, y + .012f, zA, zB);                       // listwa progowa inox
            W.KolizjaMM(Mathf.Min(xe, xk), Mathf.Max(xe, xk), y - .3f, y + .01f, zA, zB);
        }
        W.Rampa(pT - Vector3.right * (sx * .1f), pB + Vector3.right * (sx * .1f), stepW);
        // 3) osłony boczne inox (kratownica), balustrady szklane, noski głowic, poręcz przewijana
        var sk = Quaternion.LookRotation((pB - pT).normalized);
        float L = Vector3.Distance(pT, pB), hb = .95f * K, ra = .14f;
        var porecze = new List<Renderer>();
        foreach (float s in new[] { -1f, 1f })
        {
            float zz = ezc + s * (stepW * .5f + .12f);
            W.OBox(mStal, (pT + pB) * .5f + new Vector3(0, -.35f, zz - ezc), new Vector3(.24f, 1.1f, L + .4f), sk);
            MMx(mStal, xTopPlat, xTop - sx * .3f, yTop - .8f, yTop + .12f, zz - .12f, zz + .12f);
            MMx(mStal, xBotPlat, xBotEnd + sx * .3f, yBot - .1f, yBot + .12f, zz - .12f, zz + .12f);
            W.Rura(mStal, new Vector3(xTop - sx * .3f, yTop, zz - .115f), new Vector3(xTop - sx * .3f, yTop, zz + .115f), .12f, 12);
            W.Rura(mStal, new Vector3(xBotEnd + sx * .3f, yBot, zz - .115f), new Vector3(xBotEnd + sx * .3f, yBot, zz + .115f), .12f, 12);
            W.OBox(mSzklo, (pT + pB) * .5f + new Vector3(0, hb * .5f + .1f, zz - ezc), new Vector3(.02f, hb - .1f, L), sk);
            MMx(mSzklo, xTopPlat, xTop, yTop + .12f, yTop + hb, zz - .01f, zz + .01f);
            MMx(mSzklo, xBotEnd, xBotPlat, yBot + .12f, yBot + hb, zz - .01f, zz + .01f);
            W.Kolizja((pT + pB) * .5f + new Vector3(0, hb * .5f, zz - ezc), new Vector3(.1f, hb, L), sk);
            W.KolizjaMM(Mathf.Min(xTopPlat, xTop - sx * .4f), Mathf.Max(xTopPlat, xTop - sx * .4f), yTop, yTop + hb, zz - .12f, zz + .12f);
            W.KolizjaMM(Mathf.Min(xBotPlat, xBotEnd + sx * .4f), Mathf.Max(xBotPlat, xBotEnd + sx * .4f), yBot, yBot + hb, zz - .12f, zz + .12f);
            // poręcz: zawrót w głowicy górnej → bieg → zawrót w głowicy dolnej (u wzdłuż długości)
            float yH = yTop + hb + .05f, yHb = yBot + hb + .05f, xe = xTop - sx * .2f, xeB = xBotEnd + sx * .2f;
            var pts = new List<Vector3> { new Vector3(xe + sx * .25f, yH - 2f * ra, zz) };
            for (int k = 0; k <= 6; k++) { float th = Mathf.Lerp(-90f, 90f, k / 6f) * Mathf.Deg2Rad; pts.Add(new Vector3(xe - sx * ra * Mathf.Cos(th), yH - ra + ra * Mathf.Sin(th), zz)); }
            pts.Add(new Vector3(xTopPlat, yH, zz)); pts.Add(new Vector3(xBotPlat, yHb, zz));
            for (int k = 0; k <= 6; k++) { float th = Mathf.Lerp(90f, -90f, k / 6f) * Mathf.Deg2Rad; pts.Add(new Vector3(xeB + sx * ra * Mathf.Cos(th), yHb - ra + ra * Mathf.Sin(th), zz)); }
            pts.Add(new Vector3(xeB - sx * .25f, yHb - 2f * ra, zz));
            var por = BraxMiastoA.Obiekt(tasma, nazwa + ": poręcz ruchoma " + (s < 0 ? "lewa" : "prawa"), RuraUV(pts, .04f, 10, .5f, nazwa + " poręcz"), PoreczRuchoma());
            porecze.Add(por.GetComponent<Renderer>());
        }
        W.OBox(mStal, (pT + pB) * .5f + new Vector3(0, -.95f, 0), new Vector3(stepW + .5f, .1f, L), sk);   // spód kratownicy
        // 4) osłona pod kratownicą (strona −z): skośny pas pod kratownicą + pionowe płyty co 0.5 m od posadzki (zachodzą na pas
        //    1 cm przed nim — bez wspólnej płaszczyzny), pod spocznikiem górnym płyta do spodu osłony spocznika
        if (oslonaPld)
        {
            float zo1 = zA - .24f - .02f, zo0 = zo1 - .06f, tg = Mathf.Tan(kat * Mathf.Deg2Rad);
            W.OBox(mSciana, (pT + pB) * .5f + new Vector3(0, -1.3f, (zo0 + zo1) * .5f - ezc), new Vector3(.06f, 1.1f, L), sk, true);
            float Linia(float x) => Mathf.Clamp(yTop - sx * (x - xTopPlat) * tg, yBot, yTop);
            float xa = Mathf.Min(xTop, xBotPlat), xb = Mathf.Max(xTop, xBotPlat);
            for (float x = xa; x < xb - .01f; x += .5f)
            {
                float xm = Mathf.Min(x + .5f, xb), xd = sx > 0 ? xm : x, xu = sx > 0 ? x : xm;
                bool spocz = sx * (xu - xTopPlat) < 0f;
                float yp = sx * (xd - xTopPlat) <= 0f ? yTop - .8f : Linia(xd) - 1.676f;
                if (spocz) yp = Mathf.Min(yp, yTop - .8f);
                if (yp - yBot < .2f) continue;
                W.MM(mSciana, x, xm, yBot, yp, zo0 - .01f, zo1 - .01f, true);
                W.MM(mLamperia, x, xm, yBot, yBot + .25f, zo0 - .03f, zo0 - .01f);               // cokół na licu (od strony schodów)
            }
        }
        var jazda = new GameObject(nazwa + " — napęd i jazda (BraxSchodyRuchomeJazda)").AddComponent<BraxSchodyRuchomeJazda>();
        jazda.transform.SetParent(W.Grupa, false);
        jazda.gora = pT; jazda.dol = pB; jazda.szer = stepW; jazda.wGore = wGore; jazda.predkosc = .5f; jazda.okresPoreczy = .5f; jazda.porecze = porecze.ToArray();
        SchodyRuchomeLiczba++;
        Debug.Log("BRAXHORN_SCHODY_RUCHOME " + nazwa + " kierunek=" + (wGore ? "gora" : "dol") + " stopnie=" + (nSt + 1) + " stopien_m=" + stepW.ToString("F2")
            + " wznios_m=" + h.ToString("F2") + " bieg_m=" + run.ToString("F2") + " kat=" + kat + " x " + Mathf.Min(xTop, xBotEnd).ToString("F1") + "…" + Mathf.Max(xTop, xBotEnd).ToString("F1")
            + " z " + (zA - .24f).ToString("F2") + "…" + (zB + .24f).ToString("F2") + " poręcz_uv=" + (mPoreczRuchoma && mPoreczRuchoma != mPoreczGuma ? "tak" : "NIE") + " jazda=tak dźwięk=tak");
    }

    // ---------------- hala peronowa ----------------
    static void Hala()
    {
        float hx0 = PX0, hx1 = PX1;
        // peron wyspowy: płyta, krawędzie z jasnego granitu (z podcięciem — wnęka pod krawędzią), żółte linie
        W.MM(mPosadzka, hx0, hx1, yP - .35f, yP, zPS + .45f, zPN - .45f, true);
        foreach (float s in new[] { -1f, 1f })
        {
            float ze = s < 0 ? zPS : zPN;
            float zin = ze - s * .45f;
            W.MM(mPosadzkaJasna, hx0, hx1, yP - .3f, yP, Mathf.Min(ze, zin), Mathf.Max(ze, zin), true);
            // ściana peronu od strony toru, cofnięta (wnęka ratunkowa pod krawędzią)
            float zw = ze - s * .55f;
            W.MM(mBetonCiemny, hx0, hx1, yR - .35f, yP - .3f, zw - .1f, zw + .1f, true);
            // pole uwagi: żółta linia 0.6·k od krawędzi, pas ryflowany
            float zy = ze - s * (.6f * K);
            W.MM(mZolty, hx0 + .2f, hx1 - .2f, yP, yP + .006f, zy - .15f, zy + .15f);
            for (float x = hx0 + .4f; x < hx1 - .3f; x += 1.2f)
                W.MM(mZolty, x, x + .9f, yP + .006f, yP + .012f, zy - .1f, zy + .1f);
        }
        // ściany zatorowe (okładzina, cokół, fryz z nazwą), sufit, ściany szczytowe z portalami tuneli
        foreach (float s in new[] { -1f, 1f })
        {
            float zw = s < 0 ? zS : zN;
            float zo = zw + s * .2f;
            W.MM(mSciana, hx0, hx1, yR - .35f, yHc, Mathf.Min(zw, zo), Mathf.Max(zw, zo), true);
            W.MM(mLamperia, hx0, hx1, yR - .35f, yP - .2f, zw - s * .01f - .01f, zw - s * .01f + .01f);
            // fryz z nazwą stacji na wysokości okien wagonu (czytelny z pociągu) co 6 m
            var n = s < 0 ? Vector3.forward : Vector3.back;
            for (float x = hx0 + 3f; x < hx1 - 2f; x += 6f)
                Tab(new Vector3(x, yP + 2.2f, zw - s * .012f), n, 2.6f, 2.6f / 12f, aFryz, false);
            // pas w kolorze linii pod sufitem
            W.MM(mPas, hx0, hx1, yHc - .5f, yHc - .38f, zw - s * .015f - .006f, zw - s * .015f + .006f);
        }
        // sufit podwieszany tuż pod stropem (bez wspólnej płaszczyzny ze spodem płyty antresoli), bez otworu schodów
        float ys0 = yHc - .03f, ys1 = yHc - .005f;
        W.MM(mSufit, hx0, WX0, ys0, ys1, zS, zN); W.MM(mSufit, WX1, hx1, ys0, ys1, zS, zN);
        W.MM(mSufit, WX0, WX1, ys0, ys1, zS, WZ0); W.MM(mSufit, WX0, WX1, ys0, ys1, WZ1, zN);
        // lamele i ciągłe linie LED nad krawędziami i osią peronu
        bool Studnia(float xa, float xb, float za, float zb) => xb > WX0 && xa < WX1 && zb > WZ0 && za < WZ1;
        for (float x = hx0 + 1.5f; x < hx1; x += 3f)
        {
            if (x > WX0 - .1f && x < WX1 + .1f) { W.MM(mBelka, x - .06f, x + .06f, yHc - .25f, ys0, zS, WZ0); W.MM(mBelka, x - .06f, x + .06f, yHc - .25f, ys0, WZ1, zN); continue; }
            W.MM(mBelka, x - .06f, x + .06f, yHc - .25f, ys0, zS, zN);
        }
        foreach (float z in new[] { zPS + .9f, 28.5f, zPN - .9f })
            for (float x = hx0 + .5f; x < hx1 - .5f; x += 2.4f)
                if (!Studnia(x, x + 2.1f, z - .1f, z + .1f)) W.MM(mKlosz, x, x + 2.1f, yHc - .28f, yHc - .25f, z - .1f, z + .1f);
        // ściany szczytowe z portalami torów (otwory na pełną wysokość skrajni)
        float hPort = yR + 5.1f * 1.0f;
        foreach (float xe in new[] { hx0, hx1 })
        {
            float s = xe < 0 ? -1f : 1f; float x0 = xe, x1 = xe + s * .3f;
            float xa = Mathf.Min(x0, x1), xb = Mathf.Max(x0, x1);
            W.MM(mSciana, xa, xb, yR - .35f, yHc, zPS, zPN, true);                       // czoło peronu (do dna)
            foreach (float zt in new[] { zTs, zTn })
            {
                float tz0 = zt == zTs ? zS : zPN, tz1 = zt == zTs ? zPS : zN;
                W.MM(mSciana, xa, xb, hPort, yHc, tz0, tz1, true);                         // nadproże portalu
                W.MM(mBelka, xa - .02f, xb + .02f, hPort - .25f, hPort, tz0, tz1);
            }
            // drzwi ewakuacyjne / techniczne w ścianie szczytowej nad peronem
            float xf = s < 0 ? xb + .005f : xa - .005f; var nf = s < 0 ? Vector3.right : Vector3.left;
            W.MM(mDrzwiTech, Mathf.Min(xf, xf + s * -.03f), Mathf.Max(xf, xf + s * -.03f), yP, yP + 2.1f * K, 27.6f, 29.4f);
            W.MM(mStal, Mathf.Min(xf, xf - s * .05f), Mathf.Max(xf, xf - s * .05f), yP + 1.0f * K, yP + 1.0f * K + .04f, 28.4f, 28.6f);
            Tab(new Vector3(xf - s * .01f, yP + 2.1f * K + .35f, 28.5f), nf, 1.1f, .55f, aEwak);
            Tab(new Vector3(xf - s * .01f, yP + 2.8f, 32.6f), nf, 2.4f, .4f, aNazwa);
            Tab(new Vector3(xf - s * .01f, yP + 2.8f, 24.4f), nf, 2.4f, .4f, aNazwa);
        }
        // kolumny w osi peronu (okładzina stalowa, cokół granitowy)
        foreach (float x in new[] { -10f, -1f, 8f, 17f, 26f, 35f })
        {
            W.MM(mStal, x - .35f, x + .35f, yP, yHc, 28.15f, 28.85f, true);
            W.MM(mLamperia, x - .37f, x + .37f, yP, yP + .3f, 28.13f, 28.87f);
        }
        // światła peronu (ForceVertex) — dwa rzędy nad krawędziami, co 9 m
        for (float x = hx0 + 4f; x < hx1; x += 9f)
            foreach (float z in new[] { zPS + 1.4f, zPN - 1.4f })
                W.Lampa("metro: światło peronu", new Vector3(x, yHc - .6f, z), .6f, 11f);
        // pod antresolą (wschodni koniec peronu) — sufit niżej nie jest potrzebny: strop antresoli = sufit hali (yHc)
    }

    // ---------------- tory w hali ----------------
    static void Tory()
    {
        foreach (float zt in new[] { zTs, zTn })
        {
            float tz0 = zt == zTs ? zS : zPN - .55f, tz1 = zt == zTs ? zPS + .55f : zN;
            Torowisko(PX0, PX1, zt, tz0, tz1, mBeton);
        }
    }

    static void Torowisko(float x0, float x1, float zt, float tz0, float tz1, Material dno)
    {
        float yB = yR - .35f;
        W.MM(dno, x0, x1, yB - .2f, yB, tz0, tz1, true);
        float rozst = 1.435f * K * .5f;   // rozstaw szyn × k (skala pociągu)
        foreach (float s in new[] { -1f, 1f })
        {
            float zr = zt + s * rozst;
            W.MM(mSzyna, x0, x1, yR - .17f, yR - .03f, zr - .035f, zr + .035f);
            W.MM(mSzyna, x0, x1, yR - .03f, yR, zr - .04f, zr + .04f);
            W.MM(mSzyna, x0, x1, yR - .19f, yR - .16f, zr - .08f, zr + .08f);
        }
        // trzecia szyna z osłoną (po stronie ściany)
        float zt3 = zt + (zt < 28.5f ? -1f : 1f) * (rozst + .75f);
        W.MM(mStalCiemna, x0, x1, yR - .1f, yR + .02f, zt3 - .05f, zt3 + .05f);
        W.MM(mTrzecia, x0, x1, yR + .06f, yR + .1f, zt3 - .12f, zt3 + .12f);
        for (float x = x0 + .4f; x < x1; x += 2.5f) W.MM(mStalCiemna, x, x + .08f, yB, yR + .06f, zt3 - .06f, zt3 + .06f);
        // podkłady betonowe co 0.6·k
        for (float x = x0 + .3f; x < x1 - .2f; x += .6f * K)
            W.MM(mPodklad, x - .13f, x + .13f, yR - .35f, yR - .19f, zt - 1.25f * K * .5f * 1.6f, zt + 1.25f * K * .5f * 1.6f);
        // odwodnienie między szynami
        W.MM(mStalCiemna, x0, x1, yB + .01f, yB + .015f, zt - .12f, zt + .12f);
    }

    // ---------------- tunele w obie strony (gasną w ciemności) ----------------
    static void Tunele()
    {
        float yB = yR - .35f, yT = yR + 5.1f;
        foreach (var (xa, xb, kier) in new[] { (TW0, PX0, -1f), (PX1, TW1, 1f) })
            foreach (float zt in new[] { zTs, zTn })
            {
                float tz0 = zt == zTs ? zS : zPN, tz1 = zt == zTs ? zPS : zN;
                float dl = xb - xa;
                // trzy odcinki: półmrok przy stacji, ciemność, czerń
                var odc = kier < 0
                    ? new[] { (xb - 14f, xb, mBeton), (xb - 32f, xb - 14f, mBetonCiemny), (xa, xb - 32f, mBetonNoc) }
                    : new[] { (xa, xa + 14f, mBeton), (xa + 14f, xa + 32f, mBetonCiemny), (xa + 32f, xb, mBetonNoc) };
                foreach (var (o0, o1, mat) in odc)
                {
                    Torowisko(o0, o1, zt, tz0, tz1, mat);
                    W.MM(mat, o0, o1, yB - .2f, yT, tz0 - .3f, tz0, true);     // ściana
                    W.MM(mat, o0, o1, yB - .2f, yT, tz1, tz1 + .3f, true);
                    W.MM(mat, o0, o1, yT, yT + .3f, tz0 - .3f, tz1 + .3f);      // strop
                    // korytka kablowe na ścianie (po stronie zewnętrznej)
                    float zk = zt < 28.5f ? tz0 + .05f : tz1 - .05f;
                    for (float yy = yR + 1.0f; yy < yR + 2.2f; yy += .4f)
                        W.MM(mStalCiemna, o0, o1, yy, yy + .05f, Mathf.Min(zk, zk + (zt < 28.5f ? .25f : -.25f)), Mathf.Max(zk, zk + (zt < 28.5f ? .25f : -.25f)));
                }
                // ściana między tunelami (przy portalach — gruba, dalej ciemna)
                // lampki tunelowe (małe klosze) co 8 m, tylko w pierwszych 30 m
                float zl = zt < 28.5f ? tz1 - .05f : tz0 + .05f;
                for (int i = 1; i <= 4; i++)
                {
                    float x = kier < 0 ? xb - i * 8f : xa + i * 8f;
                    W.MM(mKlosz, x - .15f, x + .15f, yR + 3.2f, yR + 3.35f, zl - .06f, zl + .06f);
                }
                // semafor przy wjeździe w tunel (czerwone i zielone światło) — tylko przy torze z pociągiem zielone
                float xs = kier < 0 ? xb - 2f : xa + 2f;
                W.MM(mStalCiemna, xs - .15f, xs + .15f, yR + 1.8f, yR + 2.6f, zl - .12f, zl + .12f);
                W.Kula(zt == zTn ? M.Ekran("metro semafor zielony", null, new Color(.15f, 1f, .45f)) : M.Ekran("metro semafor czerwony", null, new Color(1f, .12f, .08f)),
                       new Vector3(xs, yR + 2.4f, zl + (zt < 28.5f ? -.13f : .13f)), .07f, 10, 6);
                // koniec: czerń (tunel znika w ciemności)
                float xe = kier < 0 ? xa : xb;
                W.MM(mCzern, xe - .2f, xe + .2f, yB - .2f, yT + .3f, tz0 - .3f, tz1 + .3f, true);
            }
    }

    // ---------------- wyposażenie peronu ----------------
    static void Wyposazenie()
    {
        // ławki (dwustronne) między kolumnami, kosze, tablice odjazdów, mapy, zegary, gaśnice, SOS, kamery
        foreach (float x in new[] { -5.5f, 3.5f, 12.5f, 21.5f, 30.5f })
        {
            Lawka(new Vector3(x, yP, 28.5f - .45f), Vector3.back);
            Lawka(new Vector3(x, yP, 28.5f + .45f), Vector3.forward);
            Kosz(new Vector3(x + 2.2f, yP, 28.5f));
        }
        foreach (float x in new[] { -5.5f, 21.5f, 39.5f })
            Wiszaca(new Vector3(x, yHc - 1.0f, 28.5f), Vector3.forward, 2.0f, .75f, aOdjazdy, aOdjazdy, yHc);
        foreach (float x in new[] { 8f, 26f })
        {
            Tab(new Vector3(x, yP + 1.65f, 28.15f - .005f), Vector3.back, .68f, .255f, aMapa);
            Tab(new Vector3(x, yP + 1.65f, 28.85f + .005f), Vector3.forward, .68f, .255f, aMapa);
        }
        foreach (float x in new[] { -10f, 17f })
        {
            Tab(new Vector3(x, yP + 2.6f, 28.15f - .005f), Vector3.back, .66f, .33f, aZegar);
            Tab(new Vector3(x, yP + 2.6f, 28.85f + .005f), Vector3.forward, .66f, .33f, aZegar);
            Tab(new Vector3(x, yP + .95f, 28.15f - .005f), Vector3.back, .66f, .082f, aZolta);
            Tab(new Vector3(x, yP + .95f, 28.85f + .005f), Vector3.forward, .66f, .082f, aZolta);
        }
        Gasnica(new Vector3(-1f, yP, 28.15f), Vector3.back); Gasnica(new Vector3(35f, yP, 28.85f), Vector3.forward);
        foreach (var (x, zf, n) in new[] { (-1f, 28.85f, Vector3.forward), (35f, 28.15f, Vector3.back) })
        {
            W.MM(mCzerwony, x - .2f, x + .2f, yP + 1.1f, yP + 1.6f, Mathf.Min(zf, zf + n.z * .05f), Mathf.Max(zf, zf + n.z * .05f));
            Tab(new Vector3(x, yP + 1.8f, zf + n.z * .055f), n, .5f, .25f, aSos);
        }
        foreach (float x in new[] { -14f, 4f, 22f, 40f })
            Kamera(new Vector3(x, yHc - .3f, 28.5f), new Vector3(x < 20 ? -1f : 1f, -.5f, 0f));
        // reklamy (citylight) na ścianie płd. za torem 2: plakaty marki z repo (podświetlane)
        string brand = "Assets/CityRebuild/Brand/";
        var plakaty = new[] { ("brax-fighter.jpg", 1.5f, 2.25f), ("training.png", 3.4f, 1.91f), ("nyxa-pink.jpg", 1.5f, 2.25f), ("brand-wide.png", 3.4f, 1.91f) };
        float xp = 6f;
        foreach (var (plik, w, h) in plakaty)
        {
            var tex = M.Obraz(brand + plik, 1024);
            if (tex)
            {
                var m = M.Ekran("metro reklama " + plik, tex, new Color(.9f, .9f, .9f));
                W.MM(mStalCiemna, xp - w * .5f - .08f, xp + w * .5f + .08f, yP + 1.3f - .08f, yP + 1.3f + h + .08f, zS + .0f, zS + .07f);
                W.QuadUV(m, new Vector3(xp - w * .5f, yP + 1.3f, zS + .075f), new Vector3(xp - w * .5f, yP + 1.3f + h, zS + .075f),
                         new Vector3(xp + w * .5f, yP + 1.3f + h, zS + .075f), new Vector3(xp + w * .5f, yP + 1.3f, zS + .075f),
                         new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(0, 0));
            }
            xp += 12f;
        }
        Tab(new Vector3(54f, yP + 2.2f, zS + .075f), Vector3.forward, 3.4f, .85f, aNova, true);
        // monitor dla maszynisty na czole peronu (zachodni koniec, tor 1)
        W.Rura(mStalCiemna, new Vector3(-14.5f, yP, zPN - 1.2f), new Vector3(-14.5f, yP + 2.6f, zPN - 1.2f), .05f, 8);
        W.MM(mCzarny, -14.9f, -14.1f, yP + 2.3f, yP + 2.8f, zPN - 1.25f, zPN - 1.15f);
        // miejsce obsady: dyżurny ruchu na peronie
        W.Obsada("dyżurny ruchu na peronie", new Vector3(40f, yP, 31.8f), 270f);
        W.Akcja("metro_dyzurny", "POROZMAWIAJ", "Dyżurny: pociąg do OSIEDLA odjeżdża z peronu 1 za 2 minuty. Proszę wsiadać.", new Vector3(40f, yP, 31.0f), null, 1.4f);
    }

    static void Lawka(Vector3 at, Vector3 twarz)
    {
        // ławka metalowa z siedziskiem z drewna: siedzisko 0.45·k, długość 1.8·k
        float L = 1.8f * K, hs = .45f * K, gl = .45f * K;
        var q = Quaternion.LookRotation(twarz);
        var bok = q * Vector3.right; var tyl = -twarz.normalized;
        var c = at + Vector3.up * (hs - .03f);
        W.OBox(mDrewno, c, new Vector3(L, .05f, gl), q);
        W.OBox(mDrewno, at + Vector3.up * (hs + .28f) + tyl * (gl * .5f - .02f), new Vector3(L, .4f, .04f), q * Quaternion.Euler(-12f, 0, 0));
        foreach (float s in new[] { -.42f, .42f })
        {
            var p = at + bok * (L * s);
            W.OBox(mStalCiemna, p + Vector3.up * (hs * .5f - .03f), new Vector3(.06f, hs - .06f, gl - .05f), q);
            W.OBox(mStalCiemna, p + Vector3.up * (hs + .25f) + tyl * (gl * .5f - .01f), new Vector3(.05f, .5f, .04f), q);
        }
        W.Kolizja(at + Vector3.up * (hs * .5f), new Vector3(L, hs, gl), q);
        W.Siedzisko(at + Vector3.up * hs, twarz, "ŁAWKA");
    }

    static void Kosz(Vector3 at)
    {
        float r = .21f * K, h = .8f * K;
        W.Walec(mStal, at, r, h * .92f, 16, true);
        W.Walec(mStalCiemna, at + Vector3.up * h * .92f, r * 1.05f, h * .08f, 16);
        W.MM(mCzarny, at.x - r * .5f, at.x + r * .5f, at.y + h - .002f, at.y + h + .002f, at.z - r * .3f, at.z + r * .3f);
    }

    static void Gasnica(Vector3 at, Vector3 n)
    {
        // pkt 1c: SZAFKA HYDRANTOWA naścienna (hydrant wewnętrzny 25 z wężem półsztywnym) + gaśnica na wieszaku obok.
        // Wymiary prawdziwe × K: szafka 0.63 × 0.73 × 0.25 m; skrzynka stalowa (dno, boki, góra), drzwi z ramą i szybą,
        // w środku zwijadło z wężem, zawór, prądownica; zawiasy, zamek, piktogram nad szafką, gaśnica 6 kg z wężykiem.
        var q = Quaternion.LookRotation(n); var bok = Vector3.Cross(Vector3.up, n).normalized;
        float w = .63f * K, h = .73f * K, d = .25f * K, t = .02f;
        var dol = at + Vector3.up * (.75f * K);                       // spód szafki 0.75 m nad posadzką
        Vector3 C(float x, float y, float z) => dol + bok * x + Vector3.up * y + n * z;
        W.OBox(mCzerwony, C(0, h * .5f, .01f), new Vector3(w, h, .02f), q);                         // plecy
        W.OBox(mCzerwony, C(-w * .5f + t * .5f, h * .5f, d * .5f), new Vector3(t, h, d), q);        // boki
        W.OBox(mCzerwony, C(w * .5f - t * .5f, h * .5f, d * .5f), new Vector3(t, h, d), q);
        W.OBox(mCzerwony, C(0, t * .5f, d * .5f), new Vector3(w - 2 * t, t, d), q);                 // dno
        W.OBox(mCzerwony, C(0, h - t * .5f, d * .5f), new Vector3(w - 2 * t, t, d), q);             // góra
        // drzwi: rama 4 cm (czerwona) wokół szyby, szyba lekko cofnięta (nic nie leży na niczym)
        float fz = d + .012f, r = .045f;
        W.OBox(mCzerwony, C(0, h - r * .5f, fz), new Vector3(w, r, .024f), q);
        W.OBox(mCzerwony, C(0, r * .5f, fz), new Vector3(w, r, .024f), q);
        W.OBox(mCzerwony, C(-w * .5f + r * .5f, h * .5f, fz), new Vector3(r, h - 2 * r, .024f), q);
        W.OBox(mCzerwony, C(w * .5f - r * .5f, h * .5f, fz), new Vector3(r, h - 2 * r, .024f), q);
        W.OBox(mSzklo, C(0, h * .5f, fz - .004f), new Vector3(w - 2 * r, h - 2 * r, .006f), q);
        foreach (float zy in new[] { .15f, h - .15f })                                               // zawiasy (lewa) i zamek (prawa)
            W.Rura(mStal, C(-w * .5f - .006f, zy - .04f, fz), C(-w * .5f - .006f, zy + .04f, fz), .009f, 8);
        W.OBox(mStalCiemna, C(w * .5f - r * .5f, h * .5f, fz + .016f), new Vector3(.022f, .06f, .01f), q);
        // wnętrze: zwijadło (bęben + tarcze), wąż w zwojach, zawór na rurze zasilającej, prądownica w uchwycie
        var os = C(-.04f, h * .52f, .02f);
        W.Rura(mStalCiemna, os, os + n * (d * .55f), .04f, 12);
        foreach (float zz in new[] { .03f, d * .62f }) W.Rura(mCzerwony, os + n * zz, os + n * (zz + .008f), h * .36f, 20, true);
        for (int i = 0; i < 4; i++) W.Rura(mGuma, os + n * (.05f + i * .035f), os + n * (.05f + i * .035f + .03f), h * (.17f + i * .025f), 18, true);
        W.Rura(mStal, C(w * .5f - .07f, -.05f, .05f), C(w * .5f - .07f, h * .28f, .05f), .018f, 10);            // rura zasilająca
        W.Walec(mStal, C(w * .5f - .07f, h * .28f, .05f) - Vector3.up * .02f, .03f, .07f, 10);                    // zawór
        W.Rura(mCzerwony, C(w * .5f - .07f, h * .28f + .05f, .05f), C(w * .5f - .07f, h * .28f + .05f, .1f), .006f, 6);   // pokrętło
        W.Rura(mStalCiemna, C(w * .5f - .09f, h * .62f, .1f), C(w * .5f - .09f, h * .9f, .1f), .02f, 8);          // prądownica
        W.Napis("HYDRANT", C(0, h + .08f, .012f), n, .07f, new Color(.85f, .1f, .08f));
        Tab(C(0, h + .26f, .002f), n, .22f, .22f, aGasnica);
        // gaśnica na wieszaku obok (spód 0.15 m nad posadzką)
        var g = at + bok * (w * .5f + .28f) + n * .1f;
        W.OBox(mStalCiemna, g + Vector3.up * (.95f * K) - n * .085f, new Vector3(.08f, .1f, .01f), q);            // wieszak
        W.Walec(mCzerwony, g + Vector3.up * (.15f * K), .08f, .48f * K, 14);
        W.Walec(mStalCiemna, g + Vector3.up * (.15f * K + .48f * K), .03f, .06f, 8);                              // zawór
        W.Rura(mCzarny, g + Vector3.up * (.15f * K + .5f * K) + bok * .03f, g + Vector3.up * (.15f * K + .2f * K) + bok * .09f, .012f, 6);
    }

    static void Kamera(Vector3 at, Vector3 kier)
    {
        var q = Quaternion.LookRotation(kier.normalized);
        W.Rura(mBialy, at, at + Vector3.up * .25f, .025f, 6);
        W.OBox(mBialy, at - (q * Vector3.forward) * .02f, new Vector3(.12f, .1f, .3f), q);
        W.OBox(mCzarny, at + (q * Vector3.forward) * .15f, new Vector3(.08f, .07f, .02f), q);
    }

    // ---------------- pociąg M1 (2 zoptymalizowane wagony kane_sk06, kurs ~60 s przy peronie 1) ----------------
    const string ModelPociagu = "Assets/Source/Claude/Metro/metro_train.glb";   // CC BY 4.0 kane_sk06 (CREDITS.md)

    static void Pociag()
    {
        // Tylko jeden skład dwuwagonowy z oryginalnego GLB. Nie podmieniaj
        // nieudanego modelu na trzy kolejne wagony z kodu: dawało to duplikaty.
        try
        {
            AssetDatabase.ImportAsset(ModelPociagu, ImportAssetOptions.ForceSynchronousImport);
            var zrodlo = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPociagu);
            if (!zrodlo)
                throw new InvalidOperationException("BRAXHORN_METRO_M1_SOURCE_BRAK: " + ModelPociagu);
            if (!PociagModel(zrodlo))
                throw new InvalidOperationException("BRAXHORN_METRO_M1_MODEL_NIEGOTOWY: nie utworzono kompletnego składu");
        }
        catch (Exception e)
        {
            Debug.LogError("BRAXHORN_METRO_M1_NIEGOTOWY: " + e);
            throw;
        }
    }

    static Bounds Granice(GameObject o, Func<Renderer, bool> filtr)
    {
        bool jest = false; var b = new Bounds(o.transform.position, Vector3.zero);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            if (filtr != null && !filtr(r)) continue;
            if (!jest) { b = r.bounds; jest = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    static string NazwyMat(Renderer r) => string.Join(" ", r.sharedMaterials.Where(m => m).Select(m => m.name.ToLowerInvariant()));

    /// <summary>Dwa wagony modelu czołami na zewnątrz (jednostka dwuczłonowa) na torze płn. przy peronie 1. Skala z prawdziwej
    /// szerokości pudła 2.8 m × K, ograniczona prześwitem do krawędzi peronu (wózki nie wchodzą w lico peronu). Koła na główce
    /// szyny. Kolizja pudła (bryła), drzwi zamknięte — pociąg stoi. Kierunek czoła z elementów czoła (reflektory / szyba czołowa).</summary>
    static bool PociagModel(GameObject zrodlo)
    {
        var probe = (GameObject)UnityEngine.Object.Instantiate(zrodlo);
        probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        probe.transform.localScale = Vector3.one;
        // Importowany GLB ma dowolną oś długości (X, Y lub Z).
        // W #91 porównywano tylko X/Z; wagon ustawiony długością w osi Y
        // wyrastał nad ulicę i jego BoxCollider blokował wejście TOWER.
        var surowe = Granice(probe, null);
        if (surowe.size.sqrMagnitude < 1e-6f)
            throw new InvalidOperationException("BRAXHORN_METRO_M1_MODEL_BEZ_GEOMETRII");
        Quaternion ustawDoTorow = surowe.size.y > surowe.size.x && surowe.size.y > surowe.size.z
            ? Quaternion.Euler(0f, 0f, -90f)
            : surowe.size.z > surowe.size.x ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        probe.transform.rotation = ustawDoTorow;
        var cale = Granice(probe, null);
        Func<Renderer, bool> pudlo = r => NazwyMat(r).Contains("side");
        var bP = Granice(probe, pudlo);
        if (bP.size.sqrMagnitude < 1e-6f) bP = cale;
        float szerP = bP.size.z > cale.size.z * .5f ? bP.size.z : cale.size.z;
        float szerC = cale.size.z;
        var bC = Granice(probe, r => { var n = NazwyMat(r); return n.Contains("mirror.001") || n.Contains("aistandardsurface"); });
        float zn = bC.center.x - cale.center.x;
        bool czoloZnane = bC.size.sqrMagnitude > 1e-6f && Mathf.Abs(zn) > cale.size.x * .25f;
        float kierunekCzola = czoloZnane ? Mathf.Sign(zn) : 1f;
        UnityEngine.Object.DestroyImmediate(probe);
        if (szerP < 1e-4f || cale.size.y < 1e-4f || cale.size.x < 1e-4f) return false;

        // Używamy TEGO SAMEGO optymalizowanego modelu kane_sk06 z galerii właściciela.
        // 208 MB oryginału -> 25 MB w repo (CREDITS.md: 383k -> 96k tris, tekstury 4K -> 1K).
        // Rozmiar docelowy ograniczają: rozstaw torowiska, bok peronu i
        // przede wszystkim sufit tunelu (nie może wystawać na ulicę).
        float skala = Mathf.Min(2.8f * K / szerP,
            (2f * (zTn - zPN) - .20f) / szerC,
            (yHc - .30f - yR) / cale.size.y);
        // Dwa człony tego samego składu: 20 cm na widoczny przegub.
        float dl = cale.size.x * skala, gap = .20f;
        if (!float.IsFinite(skala) || skala <= 0f ||
            dl < 8f || 2f * dl + gap > PX1 - PX0 - 5f)
            throw new InvalidOperationException("BRAXHORN_METRO_M1_SKALA_NIEGOTOWA: długość wagonu=" +
                dl.ToString("F2") + ", skala=" + skala.ToString("F4") +
                ", GLB=" + cale.size.ToString("F2"));
        float xWsch = PX1 - 3f;
        var sklad = new GameObject("METRO M1 — pociąg kursujący kane_sk06 (2 wagony, 60 s)").transform;
        sklad.SetParent(W.Grupa, false);
        sklad.position = Vector3.zero;
        var otwL = new List<Transform>();
        var otwP = new List<Transform>();
        var dojscia = new List<Vector3>();
        bool boundsGotowe = false;
        Bounds wszystkie = default;
        Bounds poprzedniWagon = default;
        Material rd = mPudlo;
        Material ciemny = mCzarny;
        for (int i = 0; i < 2; i++)
        {
            float cx = xWsch - dl * .5f - i * (dl + gap);
            // Przy obu członach kabiny na zewnątrz, wagony wzdłuż osi X toru.
            float yaw = (i == 0 ? kierunekCzola : -kierunekCzola) > 0f ? 0f : 180f;
            var w = (GameObject)UnityEngine.Object.Instantiate(zrodlo, sklad, false);
            w.name = "metro M1 — wagon " + (101 + i) + " (autentyczny model kane_sk06 CC BY)";
            w.transform.SetPositionAndRotation(Vector3.zero,
                Quaternion.Euler(0f, yaw, 0f) * ustawDoTorow);
            w.transform.localScale = Vector3.one * skala;
            var b = Granice(w, null);
            var bp = Granice(w, pudlo); if (bp.size.sqrMagnitude < 1e-6f) bp = b;
            w.transform.position = new Vector3(cx - b.center.x, yR - b.min.y, zTn - bp.center.z);
            BraxMiastoAssety.Przygotuj(w, false, true);  // dynamiczny model — bez StaticBatching
            foreach (var t in w.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            foreach (var c in w.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);  // collider całego wagonu należy do ruchomego składu
            b = Granice(w, null);
            bp = Granice(w, pudlo);
            if (bp.size.sqrMagnitude < 1e-6f) bp = b;
            var kol = new GameObject("M1: kolizja ruchomego wagonu " + (101 + i)).AddComponent<BoxCollider>();
            kol.transform.SetParent(sklad, false);
            kol.transform.position = b.center;
            kol.size = new Vector3(Mathf.Max(.1f, b.size.x - .2f),
                Mathf.Max(.1f, b.size.y - .15f),
                Mathf.Max(.1f, bp.size.z - .15f));
            // STARA W.KolizjaMM była statyczną ścianą na peronie, która
            // zostawałaby niewidzialna po odjeździe wagonu. Usunięta.
            if (i == 1)
            {
                // Jeden skład z DWÓCH wagonów: wstaw elastyczny, ciemny łącznik
                // pomiędzy rzeczywistymi czołami modeli, nie trzeci pojazd.
                float szczelina = poprzedniWagon.min.x - b.max.x;
                if (szczelina < -.20f || szczelina > .65f)
                    throw new InvalidOperationException("BRAXHORN_METRO_M1_SPRZEG: nieprawidłowa szczelina " + szczelina.ToString("F2"));
                var lacznik = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lacznik.name = "M1: ciemny przegub i połączenie wagonów 101-102";
                lacznik.transform.SetParent(sklad, false);
                lacznik.transform.position = new Vector3((poprzedniWagon.min.x + b.max.x) * .5f, yP + 1.05f * K, zTn);
                lacznik.transform.localScale = new Vector3(Mathf.Max(.2f, szczelina + .2f), 1.75f * K, 1.25f * K);
                lacznik.GetComponent<MeshRenderer>().sharedMaterial = mGuma;
                var przegubKolizja = lacznik.GetComponent<Collider>();
                if (przegubKolizja) UnityEngine.Object.DestroyImmediate(przegubKolizja);
            }
            poprzedniWagon = b;
            if (!boundsGotowe) { wszystkie = b; boundsGotowe = true; }
            else wszystkie.Encapsulate(b);
            // FAIL CLOSED: nie zapisuj nigdy wagonu przecinającego ulicę,
            // strop, peron albo krawędź tunelu. Sam Debug.LogError nie
            // zatrzymywał generowania wadliwego APK.
            if (b.min.x < PX0 + .5f || b.max.x > PX1 - .5f ||
                b.min.y < yR - .2f || b.max.y > yHc - .25f ||
                b.max.z > zN - .1f || b.min.z < zPN - .02f)
                throw new InvalidOperationException("BRAXHORN_METRO_M1_SKRAJNIA_NIEGOTOWA: wagon " + i +
                    " granice=" + b.min.ToString("F2") + "…" + b.max.ToString("F2") +
                    ", strop_y=" + yHc.ToString("F2") + ", tor_y=" + yR.ToString("F2"));
            // Stara siatka ma płaskie zamknięte drzwi (GLB bez węzłów
            // door_left/door_right, bez animacji). Tylko optyczne panele
            // przesuwne tuż przy boku od peronu. Nie deformujemy modelu.
            // Po otwarciu odsłania się zaciemniony portal wejścia, a NPC
            // odgrywają dojście/odejście; to nie jest pełna kabina.
            foreach (float offX in new[] { -.22f, .22f })
            {
                float xx = bp.center.x + offX * bp.size.x;
                float zz = bp.min.z - .05f;
                float yy = yP + 1.03f * K;
                var otw = new GameObject("M1: wejście sceniczne do wagonu " + (101 + i)).transform;
                otw.SetParent(sklad, false);
                otw.position = new Vector3(xx, yy, zz);
                void Klocek(string nazwa, Material mat, Vector3 pos, Vector3 size, List<Transform> kolekcja = null)
                {
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = nazwa; g.transform.SetParent(otw, false);
                    g.transform.localPosition = pos; g.transform.localScale = size;
                    g.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    var c = g.GetComponent<Collider>(); if (c) UnityEngine.Object.DestroyImmediate(c);
                    if (kolekcja != null) kolekcja.Add(g.transform);
                }
                float h = 1.95f * K, ww = 1.34f * K;
                Klocek("M1: głębia w otworze", ciemny, new Vector3(0f, 0f, -.026f),
                    new Vector3(ww, h, .025f));
                Klocek("M1: skrzydło lewe", rd, new Vector3(-ww * .25f, 0f, -.065f),
                    new Vector3(ww * .5f, h, .03f), otwL);
                Klocek("M1: skrzydło prawe", rd, new Vector3(ww * .25f, 0f, -.065f),
                    new Vector3(ww * .5f, h, .03f), otwP);
                dojscia.Add(sklad.InverseTransformPoint(new Vector3(xx, yP, zPN - .1f)));
            }
            rep.wagony++;
        }

        if (rep.wagony < 2) { UnityEngine.Object.DestroyImmediate(sklad.gameObject); return false; }
        // M1 przejeżdża tylko po istniejącym torze i znika w ciemnym tunelu.
        // Dla długich wagonów wyjazd jest liczony od całej bryły, aby nie
        // teleportować ich, gdy ogon składu stoi jeszcze przy peronie.
        var rb = sklad.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        var ruch = sklad.gameObject.AddComponent<BraxMetroM1Ruch>();
        ruch.drzwiLewe = otwL.ToArray();
        ruch.drzwiPrawe = otwP.ToArray();
        ruch.punktyWejscia = dojscia.ToArray();
        ruch.rozsuw = .58f * K;
        // Po poprzedniej wersji minimum +60 m umieszczało czoło składu za końcem
        // tunelu TW1=112 — poza wymodelowanym metrem, pod nieciętą ulicą.
        // Obydwa położenia spoczynkowe MUSZĄ mieścić cały skład w tunelach.
        float wjazdMin = PX1 + 4f - wszystkie.min.x;
        float wjazdMax = TW1 - 4f - wszystkie.max.x;
        float wyjazdMax = PX0 - 4f - wszystkie.max.x;
        float wyjazdMin = TW0 + 4f - wszystkie.min.x;
        if (wjazdMax < wjazdMin || wyjazdMin > wyjazdMax)
            throw new InvalidOperationException("BRAXHORN_METRO_M1_TUNEL_ZA_KROTKI: skład nie mieści się w tunelu; nie wolno stawiać go na ulicy");
        ruch.wejscieOffsetX = wjazdMin;
        ruch.wyjazdOffsetX = wyjazdMax;
        Debug.Log("BRAXHORN_METRO_M1_SKLAD_W_TUNELU wjazd=" + ruch.wejscieOffsetX.ToString("F2") +
            " zakres=[" + wjazdMin.ToString("F2") + "," + wjazdMax.ToString("F2") + "]" +
            " wyjazd=" + ruch.wyjazdOffsetX.ToString("F2") +
            " zakres=[" + wyjazdMin.ToString("F2") + "," + wyjazdMax.ToString("F2") + "]");
        var npc = sklad.gameObject.AddComponent<BraxMetroM1Pasazerowie>();
        npc.pociag = ruch;
        npc.poziomPeronu = yP;
        npc.zPeron = zPN - .3f;
        Debug.Log("BRAXHORN_METRO_M1_POCIAG_KURS: dwa autentyczne wagony, dlugosc=" +
            wszystkie.size.x.ToString("F1") + "m, skala=" + skala.ToString("F4") +
            ", par_drzwi_scenicznych=" + otwL.Count +
            ", wjazd_offset=" + ruch.wejscieOffsetX.ToString("F1") +
            ", wyjazd_offset=" + ruch.wyjazdOffsetX.ToString("F1") +
            ", cykl≈60s, bez nowej stacji i bez statycznej kolizji pociągu");
        return true;
    }

    static void PociagZKodu()
    {
        float L = 24.6f, gap = .8f;
        float xStart = PX1 - .6f - L * .5f;   // wagon 1 (czoło wschodnie) przy końcu peronu pod antresolą
        for (int i = 0; i < 3; i++)
        {
            float cx = xStart - i * (L + gap);
            Wagon(cx, L, i == 0, i == 2, 101 + i);
            rep.wagony++;
            if (i < 2) Przejscie(cx - L * .5f - gap, cx - L * .5f);
        }
    }

    static void Przejscie(float x0, float x1)
    {
        // przejście międzywagonowe (harmonijka) — gracz przechodzi z wagonu do wagonu
        float zc = zTn, f = yP, w = 1.25f * K, h = 2.0f * K;
        W.MM(mGuma, x0, x1, f - .1f, f, zc - w * .5f, zc + w * .5f, true);
        for (float x = x0; x < x1; x += .1f)
        {
            W.MM(mCzarny, x, x + .06f, f, f + h, zc - w * .5f - .06f, zc - w * .5f);
            W.MM(mCzarny, x, x + .06f, f, f + h, zc + w * .5f, zc + w * .5f + .06f);
            W.MM(mCzarny, x, x + .06f, f + h, f + h + .06f, zc - w * .5f - .06f, zc + w * .5f + .06f);
        }
        W.KolizjaMM(x0, x1, f, f + h, zc - w * .5f - .1f, zc - w * .5f);
        W.KolizjaMM(x0, x1, f, f + h, zc + w * .5f, zc + w * .5f + .1f);
    }

    static void Wagon(float cx, float L, bool czoloWsch, bool czoloZach, int numer)
    {
        float zc = zTn, Wd = 2.8f * K, hw = Wd * .5f, f = yP;           // podłoga = poziom peronu
        float yDol = yR + .55f, yOkDol = f + 1.15f, yOkGora = f + 2.38f, yDrzwi = f + 1.95f * K, yOkap = yR + 4.15f, ySufit = f + 2.62f;
        float x0 = cx - L * .5f, x1 = cx + L * .5f;
        float dW = 1.3f * K;
        float[] drzwi = { cx - 8.0f, cx, cx + 8.0f };
        float zPer = zc - hw, zDal = zc + hw;     // strona peronu (płd.) i dalsza (płn.)
        float kab = 2.0f;                          // kabina maszynisty na czołach
        float wx0 = czoloZach ? x0 + kab : x0 + .15f, wx1 = czoloWsch ? x1 - kab : x1 - .15f;

        // podwozie, wózki, koła
        W.MM(mPodwozie, x0 + .6f, x1 - .6f, yR + .75f, f - .25f, zc - hw + .2f, zc + hw - .2f);
        foreach (float bx in new[] { x0 + 3.4f, x1 - 3.4f })
        {
            W.MM(mPodwozie, bx - 1.5f, bx + 1.5f, yR + .35f, yR + .85f, zc - 1.25f, zc + 1.25f);
            foreach (float ox in new[] { -1.15f, 1.15f })
                foreach (float s in new[] { -1f, 1f })
                {
                    var kc = new Vector3(bx + ox, yR + .43f * K, zc + s * (1.435f * K * .5f));
                    W.Rura(mStalCiemna, kc + Vector3.forward * -.07f, kc + Vector3.forward * .07f, .42f * K, 14);
                }
        }
        for (float x = x0 + 7f; x < x1 - 7f; x += 2.6f)
            W.MM(mStalCiemna, x, x + 1.8f, yR + .7f, f - .3f, zc - hw + .35f, zc + hw - .35f);
        // podłoga (guma) — kolizja
        W.MM(mGuma, x0 + .05f, x1 - .05f, f - .25f, f, zc - hw + .05f, zc + hw - .05f, true);
        // dach (środek + skosy)
        W.MM(mPudlo, x0, x1, yOkap, yOkap + .5f, zc - hw * .62f, zc + hw * .62f);
        foreach (float s in new[] { -1f, 1f })
        {
            var c = new Vector3(cx, yOkap + .25f, zc + s * hw * .81f);
            W.OBox(mPudlo, c, new Vector3(L, .12f, hw * .42f), Quaternion.Euler(s * -22f, 0, 0) * Quaternion.identity);
        }
        W.MM(mPodwozie, x0 + 2f, x0 + 5f, yOkap + .5f, yOkap + .75f, zc - .6f, zc + .6f);   // klimatyzator
        W.MM(mPodwozie, x1 - 5f, x1 - 2f, yOkap + .5f, yOkap + .75f, zc - .6f, zc + .6f);

        // ściany boczne z oknami i drzwiami
        foreach (float zw in new[] { zPer, zDal })
        {
            bool peron = zw == zPer; float s = peron ? -1f : 1f;
            float zo0 = Mathf.Min(zw, zw - s * .07f), zo1 = Mathf.Max(zw, zw - s * .07f);     // pudło (grubość .07)
            float zi0 = Mathf.Min(zw - s * .07f, zw - s * .1f), zi1 = Mathf.Max(zw - s * .07f, zw - s * .1f);   // okładzina wewnętrzna
            // odcinki między drzwiami
            var kraw = new List<float> { wx0 }; foreach (float d in drzwi) { kraw.Add(d - dW * .5f); kraw.Add(d + dW * .5f); } kraw.Add(wx1);
            for (int i = 0; i < kraw.Count; i += 2)
            {
                float a = kraw[i], b = kraw[i + 1]; if (b - a < .1f) continue;
                W.MM(mPudlo, a, b, yDol, yOkDol, zo0, zo1); W.MM(mPudlo, a, b, yOkGora, yOkap, zo0, zo1);
                W.MM(mWnetrzeWagonu, a, b, f, yOkDol, zi0, zi1); W.MM(mWnetrzeWagonu, a, b, yOkGora, ySufit, zi0, zi1);
                // pas linii M1 pod oknami (na zewnątrz)
                W.MM(mPas, a, b, yOkDol - .25f, yOkDol - .1f, s > 0 ? zo1 : zo0 - .004f, s > 0 ? zo1 + .004f : zo0);
                // okna: słupki co ~2.3 m, szyby
                int nOk = Mathf.Max(1, Mathf.RoundToInt((b - a) / 2.3f)); float ow = (b - a) / nOk;
                for (int k = 0; k <= nOk; k++)
                {
                    float x = a + k * ow;
                    W.MM(mCzarny, x - .07f, x + .07f, yOkDol, yOkGora, zo0 - .005f, zo1 + .005f);
                    W.MM(mWnetrzeWagonu, x - .09f, x + .09f, yOkDol, yOkGora, zi0, zi1);
                }
                W.MM(mSzklo, a, b, yOkDol, yOkGora, zw - s * .035f - .01f, zw - s * .035f + .01f);
                W.KolizjaMM(a, b, f, yOkap, zo0 - .02f, zi1 + .02f);
                if (peron && i == 0) Tab(new Vector3((a + b) * .5f, yOkDol - .55f, zo0 - .006f), Vector3.back, 1.4f, .35f, aNumer, false);
                // mapa trasy M1 nad oknami (w środku wagonu)
                float wMap = Mathf.Min(3.2f, b - a - .4f);
                if (wMap > 1.2f) Tab(new Vector3((a + b) * .5f, (yOkGora + ySufit) * .5f, peron ? zi1 + .006f : zi0 - .006f), peron ? Vector3.forward : Vector3.back, wMap, wMap / 16f, aTrasa, true);
            }
            // nad drzwiami i pod nimi
            foreach (float d in drzwi)
            {
                W.MM(mPudlo, d - dW * .5f, d + dW * .5f, yDrzwi, yOkap, zo0, zo1);
                W.MM(mWnetrzeWagonu, d - dW * .5f, d + dW * .5f, yDrzwi, ySufit, zi0, zi1);
                W.MM(mPudlo, d - dW * .5f, d + dW * .5f, yDol, f, zo0, zo1);
                W.MM(mCzarny, d - dW * .5f - .05f, d - dW * .5f, f, yDrzwi, zo0 - .01f, zo1 + .01f);
                W.MM(mCzarny, d + dW * .5f, d + dW * .5f + .05f, f, yDrzwi, zo0 - .01f, zo1 + .01f);
                // skrzydła drzwi: od peronu odsunięte na zewnątrz (otwarte), z drugiej strony zamknięte
                foreach (float sd in new[] { -1f, 1f })
                {
                    float lx0 = peron ? d + sd * dW * .5f : d, lx1 = peron ? d + sd * dW : d + sd * dW * .5f;
                    float xa = Mathf.Min(lx0, lx1), xb = Mathf.Max(lx0, lx1);
                    float zz = peron ? zo0 - .06f : zw;   // otwarte: na zewnątrz pudła
                    // skrzydło: rama (dół, góra, boki) + szyba w środku
                    W.MM(mPudlo, xa, xb, f + .02f, f + 1.1f, zz - .03f, zz + .03f);
                    W.MM(mPudlo, xa, xb, yDrzwi - .25f, yDrzwi - .02f, zz - .03f, zz + .03f);
                    W.MM(mPudlo, xa, xa + .1f, f + 1.1f, yDrzwi - .25f, zz - .03f, zz + .03f);
                    W.MM(mPudlo, xb - .1f, xb, f + 1.1f, yDrzwi - .25f, zz - .03f, zz + .03f);
                    W.MM(mSzklo, xa + .1f, xb - .1f, f + 1.1f, yDrzwi - .25f, zz - .01f, zz + .01f);
                    W.MM(mCzarny, (sd < 0 ? xb - .03f : xa), (sd < 0 ? xb : xa + .03f), f + .02f, yDrzwi - .02f, zz - .04f, zz + .04f);
                    if (!peron) W.KolizjaMM(xa, xb, f, yDrzwi, zz - .05f, zz + .05f);
                }
                // drążek w osi drzwi
                W.Rura(mStal, new Vector3(d, f, zc), new Vector3(d, ySufit, zc), .02f * K, 10);
            }
        }
        // ściany czołowe: przejście międzywagonowe albo kabina
        foreach (var (xe, czolo, s) in new[] { (x1, czoloWsch, 1f), (x0, czoloZach, -1f) })
        {
            float xw0 = Mathf.Min(xe, xe - s * .1f), xw1 = Mathf.Max(xe, xe - s * .1f);
            if (!czolo)
            {
                float pw = 1.25f * K * .5f, ph = f + 2.0f * K;
                W.MM(mPudlo, xw0, xw1, yDol, yOkap + .5f, zc - hw, zc - pw, true); W.MM(mPudlo, xw0, xw1, yDol, yOkap + .5f, zc + pw, zc + hw, true);
                W.MM(mPudlo, xw0, xw1, ph, yOkap + .5f, zc - pw, zc + pw); W.MM(mPudlo, xw0, xw1, yDol, f, zc - pw, zc + pw);
                W.MM(mWnetrzeWagonu, xw0 - s * .02f, xw1 - s * .02f, f, ySufit, zc - hw + .1f, zc - pw);
                W.MM(mWnetrzeWagonu, xw0 - s * .02f, xw1 - s * .02f, f, ySufit, zc + pw, zc + hw - .1f);
            }
            else
            {
                // czoło: przednia ściana z szybą, maska, reflektory, wyświetlacz kierunku, sprzęg
                float xf0 = Mathf.Min(xe, xe - s * .15f), xf1 = Mathf.Max(xe, xe - s * .15f);
                // ściana czołowa: dół (srebrny), czarna maska wokół szyby (pasy), szyba czołowa, góra z wyświetlaczem
                W.MM(mPudlo, xf0, xf1, yDol, f + 1.0f, zc - hw, zc + hw);
                W.MM(mCzarny, xf0 - .01f, xf1 + .01f, f + 1.0f, f + 1.15f, zc - hw + .15f, zc + hw - .15f);
                W.MM(mCzarny, xf0 - .01f, xf1 + .01f, yOkap - .45f, yOkap - .1f, zc - hw + .15f, zc + hw - .15f);
                foreach (float sz in new[] { -1f, 1f })
                {
                    W.MM(mCzarny, xf0 - .01f, xf1 + .01f, f + 1.15f, yOkap - .45f, Mathf.Min(zc + sz * (hw - .15f), zc + sz * (hw - .3f)), Mathf.Max(zc + sz * (hw - .15f), zc + sz * (hw - .3f)));
                    W.MM(mPudlo, xf0, xf1, f + 1.0f, yOkap - .1f, Mathf.Min(zc + sz * hw, zc + sz * (hw - .15f)), Mathf.Max(zc + sz * hw, zc + sz * (hw - .15f)));
                }
                W.MM(mSzklo, (xf0 + xf1) * .5f - .012f, (xf0 + xf1) * .5f + .012f, f + 1.15f, yOkap - .45f, zc - hw + .3f, zc + hw - .3f);
                W.MM(mPudlo, xf0, xf1, yOkap - .1f, yOkap + .5f, zc - hw, zc + hw);
                W.KolizjaMM(xf0, xf1, yDol, yOkap, zc - hw, zc + hw);
                var nf = s > 0 ? Vector3.right : Vector3.left;
                Tab(new Vector3(xe + s * .03f, yOkap - .3f, zc), nf, 1.8f, .225f, aCzolo);
                foreach (float sz in new[] { -1f, 1f })
                {
                    W.Kula(M.Ekran("wagon reflektor", null, new Color(1f, .97f, .9f)), new Vector3(xe + s * .02f, f + .75f, zc + sz * (hw - .5f)), .11f, 10, 6);
                    W.Kula(M.Ekran("wagon światło końca czerwone", null, new Color(.55f, .03f, .03f)), new Vector3(xe + s * .02f, f + .75f, zc + sz * (hw - .85f)), .07f, 8, 5);
                }
                W.MM(mPodwozie, Mathf.Min(xe, xe + s * .5f), Mathf.Max(xe, xe + s * .5f), yR + .7f, yR + 1.0f, zc - .2f, zc + .2f);   // sprzęg
                Tab(new Vector3(xe + s * .03f, f + .45f, zc), nf, .9f, .225f, aNumer, false);
                // kabina: pulpit, fotel, ściana działowa z drzwiami i szybką
                float xk = xe - s * kab;
                float xp0 = Mathf.Min(xe - s * .2f, xe - s * .9f), xp1 = Mathf.Max(xe - s * .2f, xe - s * .9f);
                W.MM(mStalCiemna, xp0, xp1, f, f + 1.05f, zc - hw + .2f, zc + hw - .2f);
                W.MM(mCzarny, xp0, xp1, f + 1.05f, f + 1.1f, zc - .7f, zc + .4f);
                W.MM(mTapicerka, Mathf.Min(xk + s * .4f, xk + s * .9f), Mathf.Max(xk + s * .4f, xk + s * .9f), f + .45f * K, f + .5f * K, zc - .3f, zc + .3f);
                float xd0 = Mathf.Min(xk, xk + s * .08f), xd1 = Mathf.Max(xk, xk + s * .08f);
                W.MM(mWnetrzeWagonu, xd0, xd1, f, ySufit, zc - hw + .1f, zc - .5f, true);
                W.MM(mWnetrzeWagonu, xd0, xd1, f, ySufit, zc + .5f, zc + hw - .1f, true);
                W.MM(mDrzwiTech, xd0, xd1, f, f + 2.0f * K, zc - .5f, zc + .5f, true);
                W.MM(mSzklo, xd0 - .01f, xd1 + .01f, f + 1.4f, f + 2.1f, zc - .3f, zc + .3f);
                W.MM(mWnetrzeWagonu, xd0, xd1, f + 2.0f * K, ySufit, zc - .5f, zc + .5f);
                // ściany boczne kabiny (bez drzwi pasażerskich)
                foreach (float zw in new[] { zPer, zDal })
                {
                    float sz = zw == zPer ? -1f : 1f;
                    float ka = Mathf.Min(xe, xk), kb = Mathf.Max(xe, xk);
                    float za = Mathf.Min(zw, zw - sz * .07f), zb = Mathf.Max(zw, zw - sz * .07f);
                    W.MM(mPudlo, ka, kb, yDol, yOkDol, za, zb); W.MM(mPudlo, ka, kb, yOkGora, yOkap, za, zb);
                    W.MM(mPudlo, ka, ka + .3f, yOkDol, yOkGora, za, zb); W.MM(mPudlo, kb - .3f, kb, yOkDol, yOkGora, za, zb);
                    W.MM(mSzklo, ka + .3f, kb - .3f, yOkDol, yOkGora, (za + zb) * .5f - .01f, (za + zb) * .5f + .01f);
                    W.KolizjaMM(ka, kb, f, yOkap, za, zb);
                }
                W.MM(mGuma, Mathf.Min(xe, xk), Mathf.Max(xe, xk), f - .25f, f, zc - hw + .05f, zc + hw - .05f, true);
            }
        }
        // sufit wnętrza z liniami LED, kanał wentylacji, poręcze poziome
        W.MM(mWnetrzeWagonu, wx0, wx1, ySufit, ySufit + .05f, zc - hw + .1f, zc + hw - .1f);
        foreach (float sz in new[] { -.75f, .75f }) W.MM(mKlosz, wx0 + .3f, wx1 - .3f, ySufit - .03f, ySufit, zc + sz - .1f, zc + sz + .1f);
        W.MM(mWnetrzeWagonu, wx0 + .3f, wx1 - .3f, ySufit - .12f, ySufit, zc - .3f, zc + .3f);
        foreach (float sz in new[] { -1f, 1f })
        {
            float zr = zc + sz * (hw - .62f), yr = f + 2.05f * K * .95f;
            W.Rura(mStal, new Vector3(wx0 + .3f, yr, zr), new Vector3(wx1 - .3f, yr, zr), .018f * K, 8);
            for (float x = wx0 + 1f; x < wx1; x += 2.4f) W.Rura(mStal, new Vector3(x, yr, zr), new Vector3(x, ySufit, zr), .012f, 6);
        }
        // siedzenia podłużne między drzwiami (oba boki) + drążki przy końcach ławek
        var odc = new List<(float, float)>();
        var kr = new List<float> { wx0 + .3f }; foreach (float d in drzwi) { kr.Add(d - dW * .5f - .35f); kr.Add(d + dW * .5f + .35f); } kr.Add(wx1 - .3f);
        for (int i = 0; i < kr.Count; i += 2) if (kr[i + 1] - kr[i] > 1.2f) odc.Add((kr[i], kr[i + 1]));
        foreach (float sz in new[] { -1f, 1f })
        {
            float zwIn = zc + sz * (hw - .1f), gl = .48f * K, hs = .45f * K;
            foreach (var (a, b) in odc)
            {
                float zsF = zwIn - sz * gl;
                W.MM(mStal, a, b, f, f + hs - .1f, Mathf.Min(zwIn, zsF + sz * .1f), Mathf.Max(zwIn, zsF + sz * .1f));
                W.MM(mTapicerka, a + .03f, b - .03f, f + hs - .1f, f + hs, Mathf.Min(zwIn, zsF), Mathf.Max(zwIn, zsF));
                W.MM(mTapicerka, a + .03f, b - .03f, f + hs, f + hs + .55f * K, Mathf.Min(zwIn, zwIn - sz * .1f), Mathf.Max(zwIn, zwIn - sz * .1f));
                // podział na miejsca (szwy)
                for (float x = a + .62f * K; x < b - .3f; x += .62f * K)
                    W.MM(mCzarny, x - .01f, x + .01f, f + hs, f + hs + .004f, Mathf.Min(zwIn, zsF), Mathf.Max(zwIn, zsF));
                foreach (float x in new[] { a + .05f, b - .05f })
                    W.Rura(mStal, new Vector3(x, f + hs, zsF + sz * .05f), new Vector3(x, ySufit, zsF + sz * .05f), .018f * K, 8);
                W.KolizjaMM(a, b, f, f + hs, Mathf.Min(zwIn, zsF), Mathf.Max(zwIn, zsF));
                W.Siedzisko(new Vector3((a + b) * .5f, f + hs, (zwIn + zsF) * .5f), new Vector3(0, 0, -sz), "SIEDZENIE");
            }
        }
        W.Lampa("wagon " + numer + ": światło wnętrza", new Vector3(cx, ySufit - .3f, zc), .55f, 14f);
    }
}
