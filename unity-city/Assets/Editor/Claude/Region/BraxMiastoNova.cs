using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// CLUB NOVA OD NOWA (wykonawca A, 2.11). Zgłoszenia właściciela: z42 „czarna ściana, krzywy szyld, puste okno”; z43–z45
// „ma wyglądać PREMIUM, trochę POWIĘKSZONY, prawdziwe stanowisko DJ, prawdziwy bar, parkiet trochę większy, LASERY mają
// chodzić, KULA ma błyskać” (dziś: klocek-konsola, stożki, kula pikselowa, ciemno); 15:08 „wejście do klubu — jakiś PODEST,
// trzeba skakać przez niego… wszystko ma być PREMIUM”.
// ZDJĘTE (BraxMiastoA.Wyczysc, raport BRAXHORN_MIASTO_A_USUNIETE): całe stare wnętrze w sali (x 32.6…51.4, z 41.8…61.4,
// y 0.05…4.70), przedsionek (x 38.2…45, z 36.1…41.6), dawny blok za nim, pas frontu parteru x 32…52 (fasada, szyld
// „CLUB NOVA”, billboard „NOVA DYSKOTEKA”, próg/podest, drzwi) — źródło miasta, Codex, Fable i Claude 31.10 (kroki
// BraxClaudeNovaClub i BraxRegionInteriors.NovaClub/NovaFront WYŁĄCZONE). Nietknięte: klatka i mieszkanie Braxa nad klubem.
// NOWE (× k, podłoga = poziom chodnika 0.15 — wejście bez progu, idąc):
//  • fasada premium: czarny kamień, mosiężne lizeny, cokół z granitu, portal wejścia, szklany daszek z podświetleniem,
//    szyld „CLUB NOVA” z liter przestrzennych na podświetlanym panelu (równo, w osi wejścia), kasetony z plakatami,
//    kinkiety, czerwony chodnik, KOLEJKA ze słupkami i aksamitną liną, ochrona przy drzwiach (punkt akcji), gzyms;
//  • drzwi automatyczne szklane (BraxDrzwiPrzesuwne), przedsionek z szatnią (lada, wieszaki z okryciami) i kasą;
//  • sala 18.8 × 19.6 m (była ok. 13 × 14 użytkowej): parkiet LED 9.5 × 9 m (shader NovaParkiet, program co 8 bitów),
//    podest DJ z konsoletą (mikser dj_mixer.glb CC-BY + dwa odtwarzacze z talerzami), ekran LED za DJ-em, głośniki liniowe
//    i suby, kratownica nad parkietem z 4 laserami (laser_light.glb CC-BY; wachlarze promieni OMIATAJĄ salę) i 6 głowicami
//    ruchomymi (snopy), kula lustrzana (disco_ball.glb CC-BY) OBRACA SIĘ i BŁYSKA (iskry NovaIskry + plamki NovaKropki biegnące
//    po ścianach razem z kulą), światła sali zmieniające barwę w rytm (BraxNovaEfekty — tylko gdy gracz blisko);
//  • bar 11 m z podświetlanym frontem z onyksu, blatem, nalewakami, przejściem za ladę, barem tylnym z podświetlanymi
//    półkami butelek, lodówkami i lustrem, hokery; zaplecze (skrzynki, kegi, chłodnia, regały);
//  • 5 lóż (sofy w kształcie U × k, stoliki z kubełkami), oświetlenie liniowe (listwy LED), toalety damska i męska
//    (kabiny, umywalki z lustrem, pisuary), stoliki koktajlowe; punkty akcji pod misje (ochrona, bar, DJ, szatnia, zaplecze).
// Licencje modeli: Assets/Source/Claude/Nova/CREDITS.md (CC BY 4.0 — autorzy na ekranie Autorzy). Reszta kodem.
internal static class BraxMiastoNova
{
    const float F = BraxMiastoA.Chodnik, C = 4.55f;            // podłoga (= chodnik), sufit
    const float FrontZ = 36.1f, WewFront = 36.5f;               // lico fasady, lico wewnętrzne ściany frontowej
    const float HX0 = 32.6f, HX1 = 51.4f, HZ0 = 41.8f, HZ1 = 61.4f;   // sala
    const float SX1 = 37.98f;                                  // wschodnia ściana klatki mieszkania
    const float PX0 = 38.2f, PX1 = 45.0f;                      // przedsionek
    const float WX0 = 45.2f, WXM = 48.3f;                      // toalety: damska WX0..WXM-0.1, męska WXM+0.1..HX1
    const float DX0 = 36.5f, DX1 = 46.0f, DZ0 = 45.0f, DZ1 = 54.0f;  // parkiet
    const float RX0 = 37.5f, RX1 = 44.5f, RZ0 = 56.6f, RZ1 = 60.2f, RH = .45f;  // podest DJ
    const float BX0 = 47.4f, BX1 = 48.3f, BZ0 = 44.0f, BZ1 = 55.0f;  // lada baru
    const float ZX0 = 47.4f, ZZ0 = 57.2f;                      // zaplecze baru
    static float K => BraxMiastoA.K;
    static Vector3 Kula => new Vector3((DX0 + DX1) * .5f, 3.45f, (DZ0 + DZ1) * .5f);

    static Transform root, kol;

    // 10.11 (właściciel 10:06–10:08: „ta dyskoteka mi się nie podoba… kolega obija pikowaną skórą, drewienko — ty tak nie możesz?”,
    // „wygląda jak jeden wielki klocek, piksel”): zamiast gołych kolorów — materiały z fakturą. Skóra pikowana (chesterfield,
    // guziki w węzłach rombów) to własna tekstura z ziarnem skóry Poly Haven brown_leather (CC0) i mapą normalnych poduszek
    // (Assets/Source/Claude/Nova/tekstury/nova_skora_pikowana), barwiona w materiale; orzech = fornir teak przyciemniony,
    // marmur czarny = marble_01, posadzka = kamień polerowany; okucia z mosiądzu.
    const string TeksturyNova = "Assets/Source/Claude/Nova/tekstury/";
    static Material SkoraPik(string nazwa, Color c) => BraxUliceKit.TexPlik("NOVA: " + nazwa, TeksturyNova, "nova_skora_pikowana", .64f, c, c * .5f, .42f);
    static Material SkoraGladka(string nazwa, Color c) => BraxMiastoA.Tex("NOVA: " + nazwa, .7f, c, c * .5f, .45f, "brown_leather", "leather_red_02");
    static Material Orzech => BraxMiastoA.Tex("NOVA: orzech ciemny (fornir)", 1.4f, new Color(.52f, .36f, .27f), new Color(.20f, .12f, .07f), .42f, "teak_veneer", "dark_wood");
    static Material MarmurCzarny => BraxMiastoA.Tex("NOVA: marmur czarny polerowany", 1.8f, new Color(.30f, .30f, .32f), new Color(.05f, .05f, .055f), .12f, "marble_01");
    static Material Posadzka => BraxMiastoA.Tex("NOVA: posadzka kamień polerowany grafit", 1.6f, new Color(.42f, .42f, .44f), new Color(.10f, .10f, .11f), .22f, "granite_tile_02", "marble_01");
    static Material Mosiadz => BraxMiastoA.Mat("NOVA: mosiądz szczotkowany", new Color(.72f, .56f, .30f), .3f, .9f);
    static Color Bordo => new Color(.50f, .11f, .12f);
    static Color Czarna => new Color(.20f, .18f, .18f);
    static Color Sliwka => new Color(.38f, .11f, .30f);

    // Okładzina ściany sali od środka: cokół z orzecha, pas skóry pikowanej do wysokości oparcia (1.26 m), mosiężna listwa,
    // wyżej pionowe lamele z orzecha co 14 cm do sufitu. A = punkt na licu ściany (y dowolne), R = wzdłuż, N = do sali; otwory (s0, s1).
    static void Okladzina(Vector3 A, Vector3 R, Vector3 N, float L, params (float a, float b)[] otwory)
    {
        var sk = BraxUliceKit.S(SkoraPik("skóra pikowana ścian (bordo)", Bordo)); var wo = BraxUliceKit.S(Orzech); var ms = BraxUliceKit.S(Mosiadz);
        Vector3 P(float a, float y, float d) => new Vector3(A.x, 0, A.z) + R * a + Vector3.up * y + N * d;
        void B(UlSiatka u, Vector3 p, Vector3 q) => u.Box(Vector3.Min(p, q), Vector3.Max(p, q));
        var odc = new List<(float, float)>(); float s0 = 0f;
        foreach (var o in otwory.OrderBy(o => o.a)) { if (o.a - s0 > .08f) odc.Add((s0, o.a)); s0 = Mathf.Max(s0, o.b); }
        if (L - s0 > .08f) odc.Add((s0, L));
        foreach (var (a, b) in odc)
        {
            B(wo, P(a, F, .0f), P(b, F + .14f, .07f));                        // cokół
            B(sk, P(a, F + .14f, .01f), P(b, F + 1.22f, .065f));              // skóra pikowana
            B(ms, P(a, F + 1.22f, .0f), P(b, F + 1.26f, .075f));              // listwa mosiężna
            for (float t = a + .07f; t < b - .04f; t += .14f) B(wo, P(t - .035f, F + 1.30f, .0f), P(t + .035f, C - .16f, .045f));   // lamele
            B(wo, P(a, C - .16f, .0f), P(b, C - .1f, .05f));                    // listwa pod sufitem
        }
    }
    static readonly List<Light> swiatla = new List<Light>();
    static readonly List<Transform> lasery = new List<Transform>(), glowice = new List<Transform>(), migacze = new List<Transform>();

    public static void Apply(Transform cityRoot)
    {
        BraxUliceKit.Start(); BraxMiastoA.WyczyscPamiec(); swiatla.Clear(); lasery.Clear(); glowice.Clear(); migacze.Clear();
        var flat = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith("CLAUDE mieszkanie Braxa"));
        var stareKlub = cityRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("CLAUDE NOVA") || t.name.StartsWith("NOVA club")).ToArray();
        // 1) ZDEJMIJ STARE
        var raport = new List<string>();
        raport.Add(BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(HX0, HX1, .05f, 4.70f, 41.62f, HZ1 + .05f), "nova_sala", flat));
        raport.Add(BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(SX1 + .05f, HX1, .05f, 4.70f, 36.18f, 41.62f), "nova_przedsionek_wc", flat));
        raport.Add(BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(SX1 + .02f, 52.0f, .16f, 5.05f, 34.6f, 36.55f), "nova_front_klub", flat));
        raport.Add(BraxMiastoA.Wyczysc(cityRoot, BraxMiastoA.Obj(32.0f, SX1 + .02f, .16f, 5.05f, 34.6f, 36.2f), "nova_front_klatka", flat));
        // 9.11: bryła „nova_stary_szyld_nad_wejsciem” (fcf0df14, 7.11: x 36.3–47.7, y 5.04–7.85, z 34.0–36.03) ZDJĘTA —
        // y 5.04–7.85 to piętro mieszkania Braxa (Fl 5.10 … Ceil 7.95), więc cięła elewację źródłową i okna od ulicy
        // (właściciel 9.11: „mieszkanie Braxa tragedia”). Stary biały napis NOVA zdejmuje precyzyjnie BraxNovaEntranceFinish
        // (pas 38.74–45.26 × 3.66–4.72 × 35.48–36.40) na końcu buildu — jeden mechanizm, nie dwa.
        int stare = 0; foreach (var t in stareKlub) if (t) { BraxUliceKit.Usun(t.gameObject, "stare wyposażenie NOVA"); stare++; }
        // 3.11 (właściciel: „dyskoteka… nie można wejść”): STARE wejście źródła (BraxCityVenueDoors „NOVA_SOURCE_ENTRY” + czarne
        // stalowe skrzydło z BraxClaudeVenues) stało na zawiasie z kolizją 2.1 × 2.7 m w osi nowych drzwi automatycznych (z 36.08
        // vs 36.28) — zamknięte drzwi w przejściu. Zdjęte razem z wpisem w świecie (wejście = drzwi automatyczne).
        int stareDrzwi = 0;
        foreach (var d in Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d && d.id == "NOVA_SOURCE_ENTRY") { Object.DestroyImmediate(d.gameObject); stareDrzwi++; }
        var swiatNova = Object.FindFirstObjectByType<BraxCityWorld>(FindObjectsInactive.Include);
        if (swiatNova && swiatNova.doors != null) swiatNova.doors = swiatNova.doors.Where(x => x).ToArray();
        raport.Add("stare drzwi wejścia źródła (NOVA_SOURCE_ENTRY): " + stareDrzwi);
        root = BraxRegion.Root(cityRoot, "CLAUDE A: CLUB NOVA (od nowa)");
        kol = new GameObject("kolizje").transform; kol.SetParent(root, false);

        BraxUliceKit.UstawGrupe("NOVA fasada");
        Fasada();
        BraxUliceKit.UstawGrupe("NOVA przedsionek i toalety", 60f);
        Przedsionek(); Toalety();
        BraxUliceKit.UstawGrupe("NOVA sala", 70f);
        Sala();
        BraxUliceKit.UstawGrupe("NOVA bar", 60f);
        Bar();
        BraxUliceKit.UstawGrupe("NOVA loże", 60f);
        Loze();
        BraxUliceKit.UstawGrupe("NOVA DJ i nagłośnienie", 70f);
        StanowiskoDJ();
        BraxUliceKit.UstawGrupe("NOVA kratownica", 70f);
        Kratownica();
        int siatek = BraxUliceKit.Zapisz(root, "nova");
        Efekty();
        Swiatla();
        var ef = root.gameObject.AddComponent<BraxNovaEfekty>();
        ef.kula = root.Find("Kula lustrzana (obrót)"); ef.obrotyNaMinute = 12f; ef.lasery = lasery.ToArray(); ef.glowice = glowice.ToArray(); ef.migacze = migacze.ToArray(); ef.swiatla = swiatla.ToArray();
        BraxRegion.Rep.nova = true;
        BraxMiastoA.Log("NOVA: fasada + przedsionek + sala " + (HX1 - HX0).ToString("F1") + "×" + (HZ1 - HZ0).ToString("F1") + " m, parkiet " + (DX1 - DX0) + "×" + (DZ1 - DZ0) + " m, laserów " + lasery.Count + ", głowic " + glowice.Count + ", świateł " + swiatla.Count + ", siatek " + siatek + ", starych grup " + stare + "; zdjęte: " + string.Join(" | ", raport));
    }

    // ================= FASADA =================
    static void Fasada()
    {
        var kamien = BraxMiastoA.Tex("NOVA: czarny kamień polerowany", 2.2f, new Color(.16f, .16f, .17f), new Color(.06f, .06f, .065f), .35f, "granite_tile_02", "granite_tile", "marble_01");
        var granit = BraxMiastoA.Tex("NOVA: cokół granit", 1.4f, new Color(.45f, .45f, .46f), new Color(.18f, .18f, .19f), .6f, "granite_tile_02", "granite_tile");
        var mosiadz = BraxMiastoA.Mat("NOVA: mosiądz szczotkowany", new Color(.72f, .56f, .30f), .32f, .9f);
        var stal = BraxMiastoA.Mat("NOVA: stal czarna", new Color(.05f, .05f, .055f), .4f, .7f);
        var szklo = BraxUliceKit.SzkloPrzezroczyste;
        var panel = BraxMiastoA.Mat("NOVA: panel szyldu (czarne szkło)", new Color(.02f, .02f, .025f), .1f, .3f);
        var listwaMag = BraxMiastoA.Swieci("NOVA: listwa LED magenta", new Color(.9f, .3f, .8f), new Color(2.2f, .35f, 1.8f));
        var listwaCiepla = BraxMiastoA.Swieci("NOVA: listwa LED ciepła", new Color(1f, .9f, .75f), new Color(1.6f, 1.25f, .8f));
        var litery = BraxMiastoA.Swieci("NOVA: litery szyldu (podświetlane)", Color.white, new Color(2.0f, 1.8f, 2.1f));
        var boki = BraxMiastoA.Mat("NOVA: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        var welur = BraxMiastoA.Mat("NOVA: lina aksamitna bordo", new Color(.42f, .03f, .08f), .9f);
        var chrom = BraxMiastoA.Mat("NOVA: chrom słupków", new Color(.85f, .86f, .88f), .12f, 1f);
        float y0 = F, yG = 5.05f, z = 35.95f;   // lico okładziny 15 cm przed dawną linią fasady
        // okładzina: otwory — wejście do klubu, drzwi klatki (zostają), kasetony plakatów
        var drzwiKlubu = Rect.MinMaxRect(40.8f - 32f, F, 43.2f - 32f, F + 2.6f * 1.0f + .25f);
        var drzwiKlatki = Rect.MinMaxRect(33.0f - 32f, F, 34.36f - 32f, 3.01f);   // pkt 46: brama klatki 1.16 × 2.72 m (× K) z ościeżnicą
        var s = BraxUliceKit.S(kamien);
        // lico (okładzina 15 cm przed starą linią — bok przy x 32 domyka)
        BraxMiastoA.Sciana(s, new Vector3(32f, 0, z), Vector3.right, Vector3.back, 20f, .75f, yG, new[] { drzwiKlubu, drzwiKlatki }, .15f + .40f, s);
        s.Quad(new Vector3(32f, .75f, z), new Vector3(32f, .75f, 36.1f), new Vector3(32f, yG, 36.1f), new Vector3(32f, yG, z), Vector3.left);
        s.Quad(new Vector3(52f, .75f, z), new Vector3(52f, .75f, 36.1f), new Vector3(52f, yG, 36.1f), new Vector3(52f, yG, z), Vector3.right);
        // cokół granitowy (2 cm przed okładziną), z przerwami na drzwi
        var g = BraxUliceKit.S(granit);
        foreach (var (a, b) in new[] { (32f, 33.0f), (34.36f, 40.8f), (43.2f, 52f) })
            g.Box(new Vector3(a, F, z - .02f), new Vector3(b, .75f, 36.1f), 4 | 32);
        // mosiężne lizeny co ok. 2.4 m (poza drzwiami i panelem szyldu), cienkie listwy LED w fugach
        var m = BraxUliceKit.S(mosiadz);
        foreach (float x in new[] { 32.12f, 35.0f, 37.6f, 39.95f, 44.05f, 46.3f, 49.15f, 51.88f })
            m.Box(new Vector3(x - .06f, .75f, z - .12f), new Vector3(x + .06f, yG - .02f, z), 32);
        // gzyms nad parterem (dwie warstwy, bez wspólnych płaszczyzn)
        var gz = BraxUliceKit.S(granit);
        gz.Box(new Vector3(31.9f, yG, z - .32f), new Vector3(52.1f, yG + .14f, 36.1f), 32);
        gz.Box(new Vector3(31.95f, yG + .14f, z - .2f), new Vector3(52.05f, yG + .30f, 36.1f), 32);
        // portal wejścia: mosiężna rama 15 cm wokół otworu drzwi, ościeże 55 cm w głąb (drzwi w osi muru)
        m.Box(new Vector3(40.65f, F, z - .1f), new Vector3(40.8f, F + 2.98f, z + .02f), 32);
        m.Box(new Vector3(43.2f, F, z - .1f), new Vector3(43.35f, F + 2.98f, z + .02f), 32);
        m.Box(new Vector3(40.65f, F + 2.85f, z - .1f), new Vector3(43.35f, F + 3.0f, z + .02f), 32);
        // drzwi automatyczne (szkło) w środku grubości ściany: przejście bez progu — podłoga przedsionka = chodnik
        BraxMiastoA.DrzwiPrzesuwne(root, "CLUB NOVA — wejście", new Vector3(42f, F, 36.28f), Vector3.right, 2.4f, 2.85f, szklo, stal);
        BraxUliceKit.S(listwaCiepla).Box(new Vector3(40.82f, F + 2.80f, 36.02f), new Vector3(43.18f, F + 2.84f, 36.06f));   // listwa nad drzwiami w ościeżu
        // daszek szklany nad wejściem: rama stalowa, szkło, podświetlenie krawędzi, cięgna do muru (dół 3.30 m — Brax 2.33 przechodzi)
        float dy = 3.30f, dz0 = 34.35f;
        var st = BraxUliceKit.S(stal);
        st.Box(new Vector3(39.0f, dy, dz0), new Vector3(45.0f, dy + .16f, dz0 + .1f));
        st.Box(new Vector3(39.0f, dy, dz0 + .1f), new Vector3(39.1f, dy + .16f, z - .01f)); st.Box(new Vector3(44.9f, dy, dz0 + .1f), new Vector3(45.0f, dy + .16f, z - .01f));
        for (float x = 40.2f; x < 44.9f; x += 1.2f) st.Box(new Vector3(x - .03f, dy + .04f, dz0 + .1f), new Vector3(x + .03f, dy + .12f, z - .01f));
        BraxUliceKit.S(szklo).Box(new Vector3(39.1f, dy + .13f, dz0 + .1f), new Vector3(44.9f, dy + .145f, z - .01f));
        BraxUliceKit.S(listwaMag).Box(new Vector3(39.02f, dy - .03f, dz0 + .01f), new Vector3(44.98f, dy, dz0 + .06f));
        foreach (float x in new[] { 39.6f, 44.4f }) st.Rura(new Vector3(x, dy + .14f, dz0 + .12f), new Vector3(x, 4.9f, z - .02f), .018f, 8);
        // 10.11 (właściciel 09:26, zdjęcie fasady: „klub NOVA — to jest stary szyld, już od 5 buildów, nie chcę go”; 6.11:
        // „tylko stary napis nad NOVA”): panel z czarnego szkła z literami „CLUB NOVA” i różową obwódką LED nad daszkiem
        // (x 38.4…45.6, y 3.62…4.86) NIE jest już stawiany — nad wejściem zostaje wyłącznie szyld właściciela „NOVA DYSKOTEKA”.
        // pkt 48b (właściciel 01:01): plakaty i banery na klubie NOVA USUNIĘTE — reklama tylko na nośnikach (BraxMiastoBillboardy)
        // kinkiety przy wejściu
        foreach (float x in new[] { 40.25f, 43.75f })
        {
            st.Box(new Vector3(x - .08f, 2.0f, z - .06f), new Vector3(x + .08f, 2.5f, z));
            BraxUliceKit.S(listwaCiepla).Box(new Vector3(x - .05f, 2.04f, z - .1f), new Vector3(x + .05f, 2.46f, z - .06f));
        }
        // portal drzwi klatki (kamienna opaska — drzwi mieszkania zostają)
        g.Box(new Vector3(32.88f, F, z - .06f), new Vector3(33.0f, 3.13f, z), 32); g.Box(new Vector3(34.36f, F, z - .06f), new Vector3(34.48f, 3.13f, z), 32);
        g.Box(new Vector3(32.88f, 3.01f, z - .06f), new Vector3(34.48f, 3.13f, z), 32);
        // (czerwony chodnik NIE kładziony — zasada właściciela 15:40: nic nie leży na istniejącej powierzchni)
        // KOLEJKA: słupki chromowane co 1.25 m z aksamitną liną (zwis), 1.35 m od fasady, wolny chodnik od strony ulicy
        var ch = BraxUliceKit.S(chrom); var wl = BraxUliceKit.S(welur);
        float qz = 34.6f, ph = 1.0f * K; var slupki = new List<float>();
        for (float x = 34.4f; x <= 40.45f; x += 1.21f) slupki.Add(x);
        foreach (float x in slupki)
        {
            ch.Walec(new Vector3(x, F, qz), .16f * K, .035f, 18); ch.Rura(new Vector3(x, F + .03f, qz), new Vector3(x, F + ph - .05f, qz), .03f * K, 10);
            ch.Walec(new Vector3(x, F + ph - .06f, qz), .045f * K, .08f, 12);
            var c = kol.gameObject; var g2 = new GameObject("kolizja: słupek kolejki"); g2.transform.SetParent(kol, false); g2.transform.position = new Vector3(x, F + ph * .5f, qz);
            var cc = g2.AddComponent<CapsuleCollider>(); cc.radius = .1f; cc.height = ph;
        }
        for (int i = 0; i < slupki.Count - 1; i++)
        {
            Vector3 a = new Vector3(slupki[i] + .05f, F + ph - .1f, qz), b = new Vector3(slupki[i + 1] - .05f, F + ph - .1f, qz);
            Vector3 prev = a;
            for (int q = 1; q <= 6; q++) { float t = q / 6f; var p = Vector3.Lerp(a, b, t) + Vector3.down * (.16f * 4f * t * (1 - t)); wl.Rura(prev, p, .022f, 8, false); prev = p; }
        }
        // lina zamykająca kolejkę przy drzwiach (wejście przez bramkę ochrony) i kolizja liny (nie przechodzi się „pod liną”)
        BraxMiastoA.Kol(kol, "lina kolejki", new Vector3(slupki[0], F, qz - .05f), new Vector3(slupki[slupki.Count - 1], F + ph - .1f, qz + .05f));
        BraxMiastoA.Kol(kol, "fasada NOVA zachód", new Vector3(32f, F, z), new Vector3(33.0f, yG, 36.5f));
        BraxMiastoA.Kol(kol, "fasada NOVA klatka–wejście", new Vector3(34.36f, F, z), new Vector3(40.8f, yG, 36.5f));
        BraxMiastoA.Kol(kol, "fasada NOVA nad bramą klatki", new Vector3(33.0f, 3.01f, z), new Vector3(34.36f, yG, 36.5f));
        BraxMiastoA.Kol(kol, "fasada NOVA wschód", new Vector3(43.2f, F, z), new Vector3(52f, yG, 36.5f));
        BraxMiastoA.Kol(kol, "nadproże wejścia", new Vector3(40.8f, F + 2.98f, z), new Vector3(43.2f, yG, 36.5f));
        BraxMiastoA.Akcja(root, "nova_ochrona", "POROZMAWIAJ", "Ochrona: Wejście z kolejki, bez odzieży sportowej. Dziś w środku gra rezydent.", new Vector3(44.1f, F, 35.0f), 1.8f);
    }

    static void Plakat(Vector3 lewyDol, float w, float h, string plik, string nazwa)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(plik);
        var rama = BraxMiastoA.Mat("NOVA: rama kasetonu (aluminium czarne)", new Color(.04f, .04f, .045f), .4f, .6f);
        // kaseton: sama rama-pierścień 12 cm (bez płyty tylnej za plakatem — nic w płaszczyźnie plakatu ani tuż za nim)
        var r = BraxUliceKit.S(rama); float z = lewyDol.z; float x0 = lewyDol.x, y0 = lewyDol.y;
        r.Box(new Vector3(x0 - .06f, y0 - .06f, z - .12f), new Vector3(x0, y0 + h + .06f, z), 32); r.Box(new Vector3(x0 + w, y0 - .06f, z - .12f), new Vector3(x0 + w + .06f, y0 + h + .06f, z), 32);
        r.Box(new Vector3(x0, y0 - .06f, z - .12f), new Vector3(x0 + w, y0, z), 32); r.Box(new Vector3(x0, y0 + h, z - .12f), new Vector3(x0 + w, y0 + h + .06f, z), 32);
        if (!tex) { BraxMiastoA.Log("NOVA: brak pliku plakatu " + plik); return; }
        var m = BraxMiastoA.ZShadera(BraxCityMaterials.ShaderName, "NOVA kaseton " + nazwa, mm =>
        {
            mm.SetTexture("_MainTex", tex); mm.SetColor("_Color", Color.white); mm.SetFloat("_Roughness", .3f); mm.SetFloat("_Metallic", 0f);
            mm.SetTexture("_EmissionMap", tex); mm.SetColor("_EmissionColor", new Color(.75f, .75f, .75f));
        });
        // plakat: kadrowanie do proporcji kasetonu (środek obrazu), lico 2 cm przed ramą
        float asp = tex.width / (float)Mathf.Max(1, tex.height), want = w / h, u0 = 0, u1 = 1, v0 = 0, v1 = 1;
        if (asp > want) { float k = want / asp; u0 = .5f - k * .5f; u1 = .5f + k * .5f; } else { float k = asp / want; v0 = .5f - k * .5f; v1 = .5f + k * .5f; }
        var mesh = new Mesh { name = "NOVA kaseton " + nazwa };
        float zf = z - .08f;
        mesh.vertices = new[] { new Vector3(lewyDol.x, lewyDol.y, zf), new Vector3(lewyDol.x + w, lewyDol.y, zf), new Vector3(lewyDol.x + w, lewyDol.y + h, zf), new Vector3(lewyDol.x, lewyDol.y + h, zf) };
        mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
        mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray(); mesh.colors = Enumerable.Repeat(Color.white, 4).ToArray();
        mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 4).ToArray();
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateBounds();
        BraxMiastoA.Obiekt(root, "Kaseton: " + nazwa, mesh, m);
    }

    // ================= PRZEDSIONEK, SZATNIA, KASA =================
    static void Przedsionek()
    {
        var posadzka = BraxMiastoA.Tex("NOVA: posadzka kamień grafit", 1.2f, new Color(.30f, .30f, .32f), new Color(.12f, .12f, .13f), .55f, "granite_tile_02", "granite_tile");
        var sciana = BraxMiastoA.Mat("NOVA: panele akustyczne grafit", new Color(.07f, .07f, .08f), .95f);
        var sufit = BraxMiastoA.Mat("NOVA: sufit czarny mat", new Color(.025f, .025f, .03f), .95f);
        var lada = Orzech;
        var blat = MarmurCzarny;
        var mos = Mosiadz;
        var cieple = BraxMiastoA.Swieci("NOVA: listwa LED ciepła", new Color(1f, .9f, .75f), new Color(1.6f, 1.25f, .8f));
        // pokój przedsionka: front z otworem drzwi, ściana do sali z przejściem 4 m
        var otwFront = new[] { Rect.MinMaxRect(PX1 - 43.2f, F, PX1 - 40.8f, F + 2.85f) };
        var otwSala = new[] { Rect.MinMaxRect(40.0f - PX0, F, 44.0f - PX0, F + 3.2f) };
        BraxMiastoA.Pokoj(posadzka, sciana, sufit, PX0, PX1, WewFront, 41.6f, F, C, new[] { otwFront, null, otwSala, null });
        // podłoga przedsionka sięga do drzwi (pod skrzydłami, bez progu) — płyta pod ościeżem
        // (od linii zabudowy 36.1 — dalej, do lica okładziny 35.95, leży chodnik; NIC na nim nie kładziemy)
        BraxUliceKit.S(posadzka).Quad(new Vector3(40.8f, F, FrontZ), new Vector3(43.2f, F, FrontZ), new Vector3(43.2f, F, WewFront), new Vector3(40.8f, F, WewFront), Vector3.up);
        BraxMiastoA.Kol(kol, "posadzka przedsionka", new Vector3(PX0, F - .3f, 35.9f), new Vector3(PX1, F, 41.6f));
        BraxMiastoA.Kol(kol, "sufit przedsionka", new Vector3(PX0, C, WewFront), new Vector3(PX1, C + .2f, 41.6f));
        BraxMiastoA.Kol(kol, "ściana przedsionka zachód", new Vector3(SX1, F, WewFront), new Vector3(PX0, C, 41.6f));
        BraxMiastoA.Kol(kol, "ściana przedsionek–toalety", new Vector3(PX1, F, WewFront), new Vector3(WX0, C, 41.6f));
        // okładzina przedsionka jak w sali: skóra pikowana, mosiądz, lamele z orzecha (otwory: drzwi, przejście do sali, kasa)
        Okladzina(new Vector3(PX0, 0, WewFront), Vector3.forward, Vector3.right, 41.6f - WewFront);
        Okladzina(new Vector3(PX1, 0, WewFront), Vector3.forward, Vector3.left, 41.6f - WewFront, (37.0f - .05f - WewFront, 38.8f + .05f - WewFront));
        Okladzina(new Vector3(PX0, 0, WewFront), Vector3.right, Vector3.forward, PX1 - PX0, (40.65f - PX0, 43.35f - PX0));
        Okladzina(new Vector3(PX0, 0, 41.6f), Vector3.right, Vector3.back, PX1 - PX0, (40.0f - PX0, 44.0f - PX0));
        // szatnia: lada (wys. 1.05 × k) przy ścianie zachodniej, za nią wieszaki z okryciami
        float lh = 1.05f * K, lx0 = 39.5f, lx1 = 40.05f, lz0 = 37.2f, lz1 = 40.8f;
        var sl = BraxUliceKit.S(lada); sl.Box(new Vector3(lx0, F, lz0), new Vector3(lx1, F + lh - .04f, lz1), 4);
        BraxUliceKit.S(blat).Box(new Vector3(lx0 - .04f, F + lh - .04f, lz0 - .04f), new Vector3(lx1 + .06f, F + lh, lz1 + .04f), 4);
        BraxUliceKit.S(cieple).Box(new Vector3(lx1 + .01f, F + .05f, lz0 + .05f), new Vector3(lx1 + .04f, F + .09f, lz1 - .05f));
        BraxMiastoA.Kol(kol, "lada szatni", new Vector3(lx0, F, lz0), new Vector3(lx1 + .06f, F + lh, lz1));
        var okr = BraxUliceKit.S(BraxMiastoA.Mat("NOVA: okrycia (kolory z wierzchołków)", Color.white, .9f));
        var rng = new System.Random(36);
        // 10.11 (właściciel 09:26: „szatnie mają być normalne, nie kwadraty”): płaszcze i wieszaki z modeli jak w dyskotece osiedla
        // (paczka osiedle_wnetrza: Wool Coat 3DECraft, Wood Clothes Hanger tedpermana — CC BY). Płaszcz wisi ramionami w poprzek
        // drążka (drążek wzdłuż z), co ok. 0.34 m, część miejsc z pustym wieszakiem; 5 odcieni wełny. Bez paczki — bryły jak dotąd.
        var plaszcz = BraxMiastoAssety.Model("osiedle_wnetrza", "szatnia_plaszcz_3decraft");
        var wieszak = BraxMiastoAssety.Model("osiedle_wnetrza", "szatnia_wieszak_roh3d");
        Color[] welna = { new Color(.16f, .16f, .17f), new Color(.62f, .45f, .28f), new Color(.17f, .21f, .34f), new Color(.45f, .45f, .46f), new Color(.40f, .12f, .14f) };
        int plaszczy = 0, wieszakow = 0;
        foreach (float xr in new[] { 38.6f, 39.1f })
        {
            float yDrazek = F + 1.75f * K;
            BraxUliceKit.S(mos).Rura(new Vector3(xr, yDrazek, 36.9f), new Vector3(xr, yDrazek, 41.2f), .018f, 8);
            if (plaszcz)
            {
                for (float zz = 37.15f; zz < 41.0f; zz += .34f)
                {
                    int los = rng.Next(10);
                    if (los < 7)
                    {
                        var o = NaDrazku(plaszcz, "Szatnia: płaszcz", new Vector3(xr, yDrazek - .04f, zz), 1.02f, true, (float)rng.NextDouble());
                        if (o) { Odcien(o, welna[rng.Next(welna.Length)]); plaszczy++; }
                    }
                    else if (wieszak && NaDrazku(wieszak, "Szatnia: pusty wieszak", new Vector3(xr, yDrazek + .1f, zz), .24f, true, (float)rng.NextDouble())) wieszakow++;
                }
                continue;
            }
            for (float zz = 37.0f; zz < 41.1f; zz += .16f)
            {
                okr.kolor = BraxUliceKit.C(.1f + (float)rng.NextDouble() * .5f, .08f + (float)rng.NextDouble() * .3f, .08f + (float)rng.NextDouble() * .35f);
                float dl = (.8f + (float)rng.NextDouble() * .35f) * K;
                okr.Box(new Vector3(xr - .2f, F + 1.73f * K - dl, zz - .05f), new Vector3(xr + .2f, F + 1.73f * K, zz + .05f));
            }
        }
        okr.kolor = BraxUliceKit.Bialy;
        BraxMiastoA.Log("NOVA szatnia: " + (plaszcz ? "płaszcze z modelu " + plaszczy + ", puste wieszaki " + wieszakow : "BRAK paczki osiedle_wnetrza — okrycia z brył"));
        // kasa (stanowisko biletów) przy ścianie wschodniej
        sl.Box(new Vector3(44.3f, F, 37.0f), new Vector3(PX1, F + lh - .04f, 38.8f), 4);
        BraxUliceKit.S(blat).Box(new Vector3(44.25f, F + lh - .04f, 36.96f), new Vector3(PX1, F + lh, 38.84f), 4);
        BraxMiastoA.Kol(kol, "kasa", new Vector3(44.25f, F, 37.0f), new Vector3(PX1, F + lh, 38.8f));
        // listwy LED pod sufitem (oświetlenie liniowe), 4 cm pod sufitem, 5 cm od ścian
        var lmag = BraxUliceKit.S(BraxMiastoA.Swieci("NOVA: listwa LED magenta", new Color(.9f, .3f, .8f), new Color(2.2f, .35f, 1.8f)));
        lmag.Box(new Vector3(PX0 + .05f, C - .08f, WewFront + .05f), new Vector3(PX1 - .05f, C - .04f, WewFront + .09f));
        lmag.Box(new Vector3(PX0 + .05f, C - .08f, 41.6f - .09f), new Vector3(PX1 - .05f, C - .04f, 41.6f - .05f));
        BraxMiastoA.Akcja(root, "nova_szatnia", "ODDAJ KURTKĘ", "Szatnia: numerek 27, odbiór przy wyjściu.", new Vector3(40.7f, F, 39.0f), 1.5f);
        BraxMiastoA.Akcja(root, "nova_kasa", "KUP WEJŚCIE", "Kasa: wejście 40 zł, w cenie jeden napój przy barze.", new Vector3(43.6f, F, 37.9f), 1.5f);
    }

    // Model na drążku szatni: wysokość realna wys × K, ramiona (dłuższy wymiar poziomy) w poprzek drążka (drążek wzdłuż z),
    // GÓRA obrysu w punkcie p (haczyk na drążku); los 0…1: przód/tył i lekki skręt. Obrót i skala korzenia z importu zostają.
    static GameObject NaDrazku(GameObject src, string nazwa, Vector3 p, float wys, bool odGory, float los)
    {
        if (!src) return null;
        var o = (GameObject)Object.Instantiate(src, root, false); o.name = nazwa;
        var r0 = src.transform.localRotation; var s0 = src.transform.localScale;
        o.transform.position = Vector3.zero; o.transform.rotation = r0; o.transform.localScale = s0;
        var b = BraxMiastoAssety.Granice(o);
        float yaw = (b.size.x >= b.size.z ? 0f : 90f) + (los < .5f ? 0f : 180f) + (los * 12f - 6f);
        o.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * r0;
        b = BraxMiastoAssety.Granice(o);
        float s = wys * K / Mathf.Max(1e-4f, b.size.y);
        o.transform.localScale = s0 * s;
        b = BraxMiastoAssety.Granice(o);
        o.transform.position += new Vector3(p.x - b.center.x, odGory ? p.y - b.max.y : p.y - b.min.y, p.z - b.center.z);
        BraxMiastoAssety.Przygotuj(o, true, false);
        return o;
    }

    static readonly Dictionary<(Material, int), Material> odcienie = new Dictionary<(Material, int), Material>();
    static void Odcien(GameObject o, Color c)
    {
        int klucz = (Mathf.RoundToInt(c.r * 255) << 16) | (Mathf.RoundToInt(c.g * 255) << 8) | Mathf.RoundToInt(c.b * 255);
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials = r.sharedMaterials.Select(src =>
            {
                if (!src || !src.HasProperty("_Color")) return src;
                if (odcienie.TryGetValue((src, klucz), out var have) && have) return have;
                var m = new Material(src) { name = src.name + " (odcień " + klucz.ToString("X6") + ")" };
                m.SetColor("_Color", c * 1.6f);
                Directory.CreateDirectory(BraxMiastoA.Out + "/Materialy");
                BraxZapisAssetu.Utworz(m, BraxMiastoA.Out + "/Materialy/nova_szatnia_" + odcienie.Count + ".mat");
                odcienie[(src, klucz)] = m; return m;
            }).ToArray();
    }

    // Regały z butelkami (model osiedla) wzdłuż ściany wschodniej baru tylnego (x = HX1), od BZ0 do BZ1, na blacie y0.
    static bool RegalyButelek(float bx, float y0)
    {
        var polki = BraxMiastoAssety.Model("osiedle_wnetrza", "bar_polki_butelki_windupbird");
        if (!polki) { BraxMiastoA.Log("NOVA bar: BRAK paczki osiedle_wnetrza — półki i butelki z kodu"); return false; }
        try
        {
            var tmp = (GameObject)Object.Instantiate(polki); tmp.transform.position = Vector3.zero; tmp.transform.rotation = polki.transform.localRotation;
            var hs = tmp.GetComponentsInChildren<Transform>(true).Where(t => t != tmp.transform && (t.name.StartsWith("Object") || t.name.StartsWith("Box")))
                .Select(t => BraxRegion.Bounds(t.gameObject).size.y).Where(h => h > 1e-4f).ToList();
            var b0 = BraxMiastoAssety.Granice(tmp); Object.DestroyImmediate(tmp);
            float maxB = hs.Count >= 3 ? hs.Max() : b0.size.y * .2f;
            float s = Mathf.Min(.33f * K / maxB, (C - .45f - y0) / Mathf.Max(1e-4f, b0.size.y));
            bool dlugiX = b0.size.x >= b0.size.z;
            float yaw = dlugiX ? 270f : 0f;                     // dłuższy bok wzdłuż ściany (z), przód do sali (−x) — jak na osiedlu
            float wr = Mathf.Max(b0.size.x, b0.size.z) * s, L = BZ1 - BZ0 - .2f;
            int n = Mathf.Clamp(Mathf.FloorToInt((L + .1f) / (wr + .1f)), 1, 8);
            float krok = L / n;
            int postawione = 0;
            for (int i = 0; i < n; i++)
            {
                float cz = BZ0 + .1f + krok * (i + .5f);
                var o = (GameObject)Object.Instantiate(polki, root, false); o.name = "Bar tylny: regał z butelkami " + (i + 1) + " (windupbird, CC BY)";
                var r0 = polki.transform.localRotation; var s0 = polki.transform.localScale;
                o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * r0; o.transform.localScale = s0 * s;
                var b = BraxMiastoAssety.Granice(o);
                o.transform.position += new Vector3(HX1 - .06f - b.max.x, y0 - b.min.y, cz - b.center.z);
                BraxMiastoAssety.Przygotuj(o, true, false);
                postawione++;
            }
            // butelki whisky i wina (Helyeouka) na ladzie baru od strony sali — przy nalewakach, na blacie z nawisem (x BX0 − 0.28 … BX1)
            var butelki = BraxMiastoAssety.Model("osiedle_wnetrza", "bar_butelki_helyeouka");
            int naLadzie = 0;
            if (butelki)
                foreach (float z in new[] { 45.2f, 48.2f, 51.7f, 54.3f })
                    if (NaDrazku(butelki, "Lada: butelki whisky i wina (Helyeouka, CC BY)", new Vector3((BX0 + BX1) * .5f + .05f, F + 1.1f * K + .001f, z), .3f, false, .3f)) naLadzie++;
            BraxMiastoA.Log("NOVA bar: regały z butelkami z modelu osiedla ×" + postawione + " (szer. " + wr.ToString("F2") + " m, skala " + s.ToString("F3") + "), zestawy butelek na ladzie " + naLadzie);
            return postawione > 0;
        }
        catch (System.Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_MIASTO_A NOVA bar regały z modelu: " + e.Message); return false; }
    }

    // ================= TOALETY =================
    static void Toalety()
    {
        var plytki = BraxMiastoA.Tex("NOVA WC: płytki czarne", 1.0f, new Color(.25f, .25f, .27f), new Color(.08f, .08f, .09f), .45f, "granite_tile_02", "granite_tile");
        var sciana = BraxMiastoA.Mat("NOVA WC: ściana tynk grafit", new Color(.12f, .12f, .13f), .8f);
        var sufit = BraxMiastoA.Mat("NOVA: sufit czarny mat", new Color(.025f, .025f, .03f), .95f);
        var ceramika = BraxMiastoA.Mat("NOVA WC: ceramika biała", new Color(.94f, .94f, .93f), .2f);
        var chrom = BraxMiastoA.Mat("NOVA: chrom słupków", new Color(.85f, .86f, .88f), .12f, 1f);
        var hpl = BraxMiastoA.Mat("NOVA WC: płyty kabin HPL czarne", new Color(.06f, .06f, .07f), .5f);
        var lustro = BraxMiastoA.Mat("NOVA WC: lustro", new Color(.75f, .78f, .80f), .04f, 1f);
        var blat = BraxMiastoA.Mat("NOVA: blat czarny kamień", new Color(.05f, .05f, .055f), .3f, .1f);
        var led = BraxMiastoA.Swieci("NOVA: listwa LED biała", new Color(1f, 1f, 1f), new Color(1.5f, 1.45f, 1.4f));
        var litery = BraxMiastoA.Swieci("NOVA: litery szyldu (podświetlane)", Color.white, new Color(2.0f, 1.8f, 2.1f));
        var boki = BraxMiastoA.Mat("NOVA: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        foreach (bool meska in new[] { false, true })
        {
            float x0 = meska ? WXM + .1f : WX0, x1 = meska ? HX1 : WXM - .1f, z0 = WewFront, z1 = 41.6f, xd = (x0 + x1) * .5f;
            var otwSala = new[] { Rect.MinMaxRect(xd - .6f - x0, F, xd + .6f - x0, F + 2.6f) };
            BraxMiastoA.Pokoj(plytki, sciana, sufit, x0, x1, z0, z1, F, 3.2f, new[] { null, null, otwSala, null });
            BraxMiastoA.Kol(kol, "WC posadzka", new Vector3(x0, F - .3f, z0), new Vector3(x1, F, z1));
            BraxMiastoA.Kol(kol, "WC strop", new Vector3(x0, 3.2f, z0), new Vector3(x1, 3.4f, z1));
            BraxMiastoA.Kol(kol, "WC ściana", new Vector3(x1, F, z0), new Vector3(x1 + .1f, 3.2f, z1));
            // przegroda z HPL 1.5 m za drzwiami (zasłania wnętrze od sali, jak w prawdziwych toaletach klubów)
            var hp = BraxUliceKit.S(hpl);
            hp.Box(new Vector3(x0 + .02f, F + .12f, 39.95f), new Vector3(xd + .7f, F + 2.1f * K, 40.0f));
            BraxMiastoA.Kol(kol, "WC przegroda", new Vector3(x0, F, 39.9f), new Vector3(xd + .7f, F + 2.1f * K, 40.05f));
            // dwie kabiny przy ścianie frontowej: przegrody, drzwi uchylone, miska ustępowa ze spłuczką
            float cw = (x1 - x0) * .5f;
            for (int i = 0; i < 2; i++)
            {
                float a = x0 + i * cw, b = a + cw, gl = 1.55f;
                if (i == 1) hp.Box(new Vector3(a - .015f, F + .12f, z0 + .02f), new Vector3(a + .015f, F + 2.0f * K, z0 + gl));
                hp.Box(new Vector3(a + .04f, F + .12f, z0 + gl - .015f), new Vector3(a + .2f, F + 2.0f * K, z0 + gl + .015f));
                // skrzydło drzwi uchylone o ok. 30° do środka kabiny
                var piv = new Vector3(b - .04f, F + .12f, z0 + gl); float dl = cw - .3f;
                var kier = Quaternion.Euler(0, -30f, 0) * Vector3.left;
                hp.OBox(piv + kier * dl * .5f + Vector3.up * (1.0f * K - .06f), kier, Vector3.up, Vector3.Cross(kier, Vector3.up).normalized, dl, 2.0f * K - .14f, .03f);
                var c = new Vector3((a + b) * .5f, F, z0 + .42f * K);
                var ce = BraxUliceKit.S(ceramika);
                ce.Walec(c + Vector3.forward * .1f, .15f * K, .28f * K, 16);
                ce.Walec(c + new Vector3(0, .28f * K, .14f), .21f * K, .12f * K, 18);
                ce.Box(new Vector3(c.x - .22f * K, F + .5f * K, z0 + .02f), new Vector3(c.x + .22f * K, F + .9f * K, z0 + .2f * K));
                BraxUliceKit.S(chrom).Walec(new Vector3(c.x, F + .9f * K, z0 + .1f * K), .035f, .02f, 10);
                BraxMiastoA.Kol(kol, "miska ustępowa", new Vector3(c.x - .25f * K, F, z0), new Vector3(c.x + .25f * K, F + .45f * K, z0 + .7f * K));
            }
            // umywalki w blacie z lustrem i listwą LED (ściana wschodnia), pisuary w męskiej (ściana zachodnia)
            float ux = x1 - .55f * K;
            BraxUliceKit.S(blat).Box(new Vector3(ux, F + .82f * K, 40.2f), new Vector3(x1 - .01f, F + .88f * K, 41.4f), 0);
            BraxMiastoA.Kol(kol, "blat umywalek", new Vector3(ux, F, 40.2f), new Vector3(x1, F + .88f * K, 41.4f));
            foreach (float uz in new[] { 40.55f, 41.05f })
            {
                BraxUliceKit.S(ceramika).Walec(new Vector3(x1 - .3f * K, F + .88f * K, uz), .17f * K, .06f, 16);
                BraxUliceKit.S(chrom).Rura(new Vector3(x1 - .06f, F + .88f * K, uz), new Vector3(x1 - .06f, F + 1.1f * K, uz), .015f, 8);
                BraxUliceKit.S(chrom).Rura(new Vector3(x1 - .06f, F + 1.1f * K, uz), new Vector3(x1 - .2f * K, F + 1.1f * K, uz), .013f, 8);
            }
            BraxUliceKit.S(lustro).Box(new Vector3(x1 - .03f, F + 1.15f * K, 40.25f), new Vector3(x1 - .01f, F + 1.95f * K, 41.35f));
            BraxUliceKit.S(led).Box(new Vector3(x1 - .05f, F + 1.97f * K, 40.25f), new Vector3(x1 - .02f, F + 2.0f * K, 41.35f));
            if (meska)
                foreach (float uz in new[] { 38.5f, 39.2f })
                {
                    var ce = BraxUliceKit.S(ceramika);
                    ce.Box(new Vector3(x0 + .02f, F + .55f * K, uz - .18f * K), new Vector3(x0 + .3f * K, F + 1.2f * K, uz + .18f * K));
                    BraxMiastoA.Kol(kol, "pisuar", new Vector3(x0, F + .5f * K, uz - .2f * K), new Vector3(x0 + .32f * K, F + 1.2f * K, uz + .2f * K));
                }
            // 10.11 (właściciel 09:26: „łazienki mają być pozamykane, nieotwarte”): drzwi ZAMKNIĘTE w przejściu (otwór toalety xd ± 0.6,
            // otwór w ścianie sali przesunięty o 5 cm — skrzydło kryje oba), ościeżnica przez całą grubość ściany 41.58…41.82
            // (lica ścian w środku ościeżnicy), klamka od sali, kolizja skrzydła.
            float hc = meska ? 49.85f : 46.75f, da = Mathf.Min(xd, hc) - .6f, db = Mathf.Max(xd, hc) + .6f, dh = F + 2.6f;
            BraxUliceKit.S(Orzech).Box(new Vector3(da, F + .01f, 41.68f), new Vector3(db, dh - .005f, 41.72f));
            hp.Box(new Vector3(da - .07f, F, 41.58f), new Vector3(da, dh + .07f, 41.82f)); hp.Box(new Vector3(db, F, 41.58f), new Vector3(db + .07f, dh + .07f, 41.82f));
            hp.Box(new Vector3(da, dh, 41.58f), new Vector3(db, dh + .07f, 41.82f));
            var kl = BraxUliceKit.S(Mosiadz); float xk = meska ? da + .12f : db - .12f, xk2 = meska ? xk + .14f : xk - .14f;
            kl.Rura(new Vector3(xk, F + 1.05f, 41.72f), new Vector3(xk, F + 1.05f, 41.79f), .012f, 8);
            kl.Rura(new Vector3(xk, F + 1.05f, 41.79f), new Vector3(xk2, F + 1.05f, 41.79f), .012f, 8);
            kl.Walec(new Vector3(xk, F + 1.0f, 41.72f), .035f, .012f, 12);
            BraxMiastoA.Kol(kol, "WC drzwi zamknięte", new Vector3(da, F, 41.62f), new Vector3(db, dh, 41.78f));
            // oznaczenie nad drzwiami od strony sali
            BraxUliceKit.Napis3D(meska ? "WC MĘSKA" : "WC DAMSKA", new Vector3(xd, F + 2.75f, 41.8f), Vector3.forward, .16f, .03f, litery, boki);
            BraxRegion.Warm(root, "WC światło", new Vector3(xd, 2.9f, (z0 + z1) * .5f), 1.2f, 6f);
        }
    }

    // ================= SALA =================
    static void Sala()
    {
        var posadzka = Posadzka;
        var sciana = BraxMiastoA.Mat("NOVA: panele akustyczne grafit", new Color(.07f, .07f, .08f), .95f);
        var sufit = BraxMiastoA.Mat("NOVA: sufit czarny mat", new Color(.025f, .025f, .03f), .95f);
        var lmag = BraxMiastoA.Swieci("NOVA: listwa LED magenta", new Color(.9f, .3f, .8f), new Color(2.2f, .35f, 1.8f));
        var lnieb = BraxMiastoA.Swieci("NOVA: listwa LED niebieska", new Color(.3f, .5f, 1f), new Color(.4f, .7f, 2.4f));
        // otwory w ścianie południowej sali (lico od środka: s rośnie od x HX1 w stronę x HX0)
        Rect OtwS(float a, float b, float h) => Rect.MinMaxRect(HX1 - b, F, HX1 - a, F + h);
        var otwPld = new[] { OtwS(40.0f, 44.0f, 3.2f), OtwS(46.15f, 47.35f, 2.6f), OtwS(49.25f, 50.45f, 2.6f) };
        // podłoga sali z dziurą na parkiet LED (parkiet to osobna powierzchnia w tym samym poziomie — nie nakłada się)
        var pod = BraxUliceKit.S(posadzka);
        void P(float x0, float x1, float z0, float z1) { if (x1 - x0 > .01f && z1 - z0 > .01f) pod.Quad(new Vector3(x0, F, z0), new Vector3(x1, F, z0), new Vector3(x1, F, z1), new Vector3(x0, F, z1), Vector3.up); }
        P(HX0, HX1, HZ0, DZ0); P(HX0, HX1, DZ1, HZ1); P(HX0, DX0, DZ0, DZ1); P(DX1, HX1, DZ0, DZ1);
        BraxMiastoA.Pokoj(posadzka, sciana, sufit, HX0, HX1, HZ0, HZ1, F, C, new[] { otwPld, null, null, null }, bezPodlogi: true);
        // ściana południowa sali od strony przedsionka i toalet (druga strona przegrody) — lico w przedsionku robi Pokoj przedsionka
        BraxMiastoA.Kol(kol, "posadzka sali", new Vector3(HX0, F - .3f, HZ0), new Vector3(HX1, F, HZ1));
        BraxMiastoA.Kol(kol, "sufit sali", new Vector3(HX0, C, HZ0), new Vector3(HX1, C + .2f, HZ1));
        BraxMiastoA.Kol(kol, "ściana sali zachód", new Vector3(HX0 - .2f, F, HZ0), new Vector3(HX0, C, HZ1));
        BraxMiastoA.Kol(kol, "ściana sali wschód", new Vector3(HX1, F, HZ0), new Vector3(HX1 + .2f, C, HZ1));
        BraxMiastoA.Kol(kol, "ściana sali północ", new Vector3(HX0, F, HZ1), new Vector3(HX1, C, HZ1 + .2f));
        foreach (var (a, b) in new[] { (HX0, 40.0f), (44.0f, 46.15f), (47.35f, 49.25f), (50.45f, HX1) })
            BraxMiastoA.Kol(kol, "ściana sali południe", new Vector3(a, F, 41.6f), new Vector3(b, C, HZ0));
        foreach (var (a, b) in new[] { (40.0f, 44.0f), (46.15f, 47.35f), (49.25f, 50.45f) })
            BraxMiastoA.Kol(kol, "nadproże", new Vector3(a, b - a > 3f ? F + 3.2f : F + 2.6f, 41.6f), new Vector3(b, C, HZ0));
        // parkiet LED (płytki 1 m, program świateł) — powierzchnia w poziomie podłogi + rama ze stali nierdzewnej (2 cm wyżej)
        var parkiet = BraxMiastoA.ZShadera("Braxhorn/NovaParkiet", "NOVA parkiet LED", m => { m.SetFloat("_Plytka", 1.0f); m.SetVector("_Srodek", new Vector4((DX0 + DX1) * .5f, (DZ0 + DZ1) * .5f, 0, 0)); m.SetFloat("_Pion", 0f); m.SetFloat("_Moc", 1.25f); });
        BraxUliceKit.S(parkiet).Quad(new Vector3(DX0, F, DZ0), new Vector3(DX1, F, DZ0), new Vector3(DX1, F, DZ1), new Vector3(DX0, F, DZ1), Vector3.up);
        var inox = BraxUliceKit.S(BraxMiastoA.Mat("NOVA: stal nierdzewna szczotkowana", new Color(.62f, .63f, .65f), .35f, .9f));
        inox.Box(new Vector3(DX0 - .06f, F, DZ0 - .06f), new Vector3(DX1 + .06f, F + .02f, DZ0), 4); inox.Box(new Vector3(DX0 - .06f, F, DZ1), new Vector3(DX1 + .06f, F + .02f, DZ1 + .06f), 4);
        inox.Box(new Vector3(DX0 - .06f, F, DZ0), new Vector3(DX0, F + .02f, DZ1), 4); inox.Box(new Vector3(DX1, F, DZ0), new Vector3(DX1 + .06f, F + .02f, DZ1), 4);
        // oświetlenie liniowe: listwy LED pod sufitem wzdłuż ścian (4 cm pod sufitem, 5 cm od ścian) i pionowe co 3 m na ścianach
        var lm = BraxUliceKit.S(lmag); var ln = BraxUliceKit.S(lnieb);
        lm.Box(new Vector3(HX0 + .05f, C - .08f, HZ0 + .05f), new Vector3(HX1 - .05f, C - .04f, HZ0 + .09f));
        lm.Box(new Vector3(HX0 + .05f, C - .08f, HZ1 - .09f), new Vector3(HX1 - .05f, C - .04f, HZ1 - .05f));
        lm.Box(new Vector3(HX0 + .05f, C - .08f, HZ0 + .09f), new Vector3(HX0 + .09f, C - .04f, HZ1 - .09f));
        // okładzina: ściana zachodnia (za lożami) na całej długości, południowa od sali między otworami (przejście, WC), wschodnia
        // przy barze tam, gdzie nie ma baru tylnego ani zaplecza; północna = mural na całą ścianę
        Okladzina(new Vector3(HX0, 0, HZ0), Vector3.forward, Vector3.right, HZ1 - HZ0);
        Okladzina(new Vector3(HX0, 0, HZ0), Vector3.right, Vector3.forward, HX1 - HX0, (40.0f - HX0, 44.0f - HX0), (46.05f - HX0, 47.45f - HX0), (49.15f - HX0, 50.55f - HX0));
        Okladzina(new Vector3(HX1, 0, HZ0), Vector3.forward, Vector3.left, ZZ0 - .2f - HZ0, (BZ0 - HZ0, BZ1 - HZ0));
        // stoliki koktajlowe (wys. 1.1 m × k) przy wejściu do sali
        var blat = MarmurCzarny; var mos = BraxUliceKit.S(Mosiadz);
        foreach (var p in new[] { new Vector3(37.0f, F, 43.4f), new Vector3(45.6f, F, 43.4f) })
        {
            BraxUliceKit.S(blat).Walec(p + Vector3.up * (1.1f * K - .04f), .32f * K, .04f, 22);
            mos.Rura(p + Vector3.up * .02f, p + Vector3.up * (1.1f * K - .04f), .04f, 10); mos.Walec(p, .25f * K, .02f, 18);
            var g = new GameObject("kolizja: stolik koktajlowy"); g.transform.SetParent(kol, false); g.transform.position = p + Vector3.up * .55f * K;
            var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .3f * K; cc.height = 1.1f * K;
        }
        BraxMiastoA.Akcja(root, "nova_parkiet", "TAŃCZ", "Parkiet: rezydent gra do rana.", new Vector3((DX0 + DX1) * .5f, F, (DZ0 + DZ1) * .5f), 3.0f);
    }

    // ================= BAR =================
    static void Bar()
    {
        var onyks = SkoraPik("front baru — skóra pikowana czarna", Czarna);
        var blat = MarmurCzarny;
        var orzech = Orzech;
        var szklo = BraxMiastoA.Mat("NOVA: półki szklane", new Color(.75f, .85f, .9f, .35f), .05f, .1f);
        var butelki = BraxMiastoA.Mat("NOVA: butelki (kolory z wierzchołków)", Color.white, .12f, .15f);
        var led = BraxMiastoA.Swieci("NOVA: listwa LED ciepła", new Color(1f, .9f, .75f), new Color(1.6f, 1.25f, .8f));
        var lustro = BraxMiastoA.Mat("NOVA WC: lustro", new Color(.75f, .78f, .80f), .04f, 1f);
        var lodowka = BraxMiastoA.Swieci("NOVA: lodówki barowe (wnętrze podświetlone)", new Color(.7f, .85f, 1f), new Color(.35f, .55f, .8f));
        var chrom = Mosiadz;
        // 10.11 (właściciel 12:25: „czarną skórę, a nie ten kolor… a najlepiej pikowaną”): siedziska hokerów — czarna skóra pikowana
        var skora = SkoraPik("siedziska hokerów — skóra pikowana czarna", Czarna);
        float h = 1.1f * K;
        // lada: front z podświetlanego onyksu (od strony sali), korpus, blat z nawisem, podnóżek
        BraxUliceKit.S(onyks).Box(new Vector3(BX0, F + .12f, BZ0), new Vector3(BX0 + .04f, F + h - .06f, BZ1), 2 | 4 | 8);
        BraxUliceKit.S(orzech).Box(new Vector3(BX0 + .04f, F, BZ0), new Vector3(BX1, F + h - .06f, BZ1), 4 | 8 | 1);
        BraxUliceKit.S(orzech).Box(new Vector3(BX0, F, BZ0), new Vector3(BX0 + .04f, F + .12f, BZ1), 4 | 2);
        BraxUliceKit.S(led).Box(new Vector3(BX0 - .02f, F + .12f, BZ0 + .05f), new Vector3(BX0 + .005f, F + .15f, BZ1 - .05f));   // ciepła listwa pod frontem
        BraxUliceKit.S(Mosiadz).Box(new Vector3(BX0 - .025f, F + h - .1f, BZ0), new Vector3(BX0 + .005f, F + h - .07f, BZ1));      // mosiężna listwa pod blatem
        BraxUliceKit.S(blat).Box(new Vector3(BX0 - .28f, F + h - .06f, BZ0 - .05f), new Vector3(BX1 + .05f, F + h, BZ1 + .05f));
        BraxUliceKit.S(chrom).Rura(new Vector3(BX0 - .18f, F + .25f, BZ0 + .1f), new Vector3(BX0 - .18f, F + .25f, BZ1 - .1f), .025f, 10);
        BraxMiastoA.Kol(kol, "lada baru", new Vector3(BX0 - .28f, F, BZ0), new Vector3(BX1 + .05f, F + h, BZ1));
        // nalewaki na blacie (3 wieże z kranami)
        foreach (float z in new[] { 46.5f, 50.0f, 53.4f })
        {
            var p = new Vector3((BX0 + BX1) * .5f + .1f, F + h, z);
            BraxUliceKit.S(chrom).Rura(p, p + Vector3.up * .45f, .045f, 10);
            for (int i = -1; i <= 1; i++) { var t = p + new Vector3(-.06f, .36f, i * .1f); BraxUliceKit.S(chrom).Rura(t, t + new Vector3(-.12f, -.06f, 0), .012f, 6); BraxUliceKit.S(skora).Rura(t + Vector3.up * .02f, t + Vector3.up * .2f, .016f, 6); }
        }
        // bar tylny: lodówki pod blatem roboczym, blat, lustro, 3 półki szklane z podświetleniem i butelkami
        float bx = HX1 - .45f;
        BraxUliceKit.S(lodowka).Box(new Vector3(bx, F, BZ0), new Vector3(HX1 - .02f, F + .95f, BZ1), 2 | 4 | 8);
        BraxUliceKit.S(blat).Box(new Vector3(bx - .05f, F + .95f, BZ0), new Vector3(HX1 - .01f, F + 1.0f, BZ1), 2);
        BraxUliceKit.S(lustro).Box(new Vector3(HX1 - .04f, F + 1.0f, BZ0 + .1f), new Vector3(HX1 - .02f, C - .4f, BZ1 - .1f), 2);
        var bu = BraxUliceKit.S(butelki); var rng = new System.Random(124);
        Color32[] kolory = { BraxUliceKit.C(.55f, .30f, .08f), BraxUliceKit.C(.12f, .35f, .12f), BraxUliceKit.C(.85f, .88f, .9f), BraxUliceKit.C(.25f, .35f, .75f), BraxUliceKit.C(.6f, .1f, .12f), BraxUliceKit.C(.9f, .75f, .3f) };
        // 10.11 (właściciel 09:26: „alkohol ma być normalnie ten sam, co jest na osiedlu — nasz, prawdziwy alkohol”): regały z butelkami
        // z modelu dyskoteki osiedla (Bar with bottles — windupbird, CC BY) na całej długości baru tylnego, plecami do lustra; butelki
        // whisky i wina (Helyeouka, CC BY) na blacie roboczym. Skala jak na osiedlu: najwyższa butelka 0.33 m × k. Bez paczki — półki z kodu.
        bool polkiZModelu = RegalyButelek(bx, F + 1.0f + .02f);
        for (int sh = 0; sh < 3 && !polkiZModelu; sh++)
        {
            float y = F + 1.55f + sh * .62f;
            BraxUliceKit.S(szklo).Box(new Vector3(bx + .05f, y - .02f, BZ0 + .1f), new Vector3(HX1 - .05f, y, BZ1 - .1f), 2);
            BraxUliceKit.S(led).Box(new Vector3(HX1 - .1f, y - .05f, BZ0 + .1f), new Vector3(HX1 - .06f, y - .02f, BZ1 - .1f));
            for (float z = BZ0 + .25f; z < BZ1 - .2f; z += .13f + (float)rng.NextDouble() * .03f)
            {
                bu.kolor = kolory[rng.Next(kolory.Length)];
                float r = (.035f + (float)rng.NextDouble() * .012f) * K, hb = (.2f + (float)rng.NextDouble() * .06f) * K, x = bx + .2f + (float)rng.NextDouble() * .08f;
                bu.Rura(new Vector3(x, y + .002f, z), new Vector3(x, y + hb, z), r, 7);
                bu.Rura(new Vector3(x, y + hb, z), new Vector3(x, y + hb + .03f, z), r * .55f, 6, false);
                bu.Rura(new Vector3(x, y + hb + .03f, z), new Vector3(x, y + hb + .1f * K, z), r * .32f, 6);
            }
        }
        bu.kolor = BraxUliceKit.Bialy;
        BraxMiastoA.Kol(kol, "bar tylny", new Vector3(bx - .05f, F, BZ0), new Vector3(HX1, C, BZ1));
        // hokery przed barem (siedzisko 0.78 m × k), każdy z BraxSeat
        for (float z = BZ0 + .6f; z < BZ1 - .3f; z += 1.2f)
        {
            var p = new Vector3(BX0 - .62f, F, z);
            // 10.11 (właściciel 12:19: „krzesła — siedziska za płaskie, zrób to normalnie”): hoker jak w barze premium — ciężka
            // mosiężna podstawa z fazą, kolumna, podnóżek z rury (pierścień na 4 wspornikach), talerz pod siedzisko i GRUBA miękka
            // poducha 11 cm × K z zaokrągloną krawędzią i kopułką (UlSiatka.PoduchaOkragla), skóra koniak
            var ch = BraxUliceKit.S(chrom);
            ch.Walec(p, .21f * K, .025f, 24); ch.Walec(p + Vector3.up * .025f, .17f * K, .012f, 24);
            ch.Rura(p + Vector3.up * .03f, p + Vector3.up * .68f * K, .032f, 12);
            float yN = .3f * K, rN = .17f * K;
            for (int q = 0; q < 16; q++)
            {
                float a0 = q * Mathf.PI / 8f, a1 = (q + 1) * Mathf.PI / 8f;
                ch.Rura(p + new Vector3(Mathf.Cos(a0) * rN, yN, Mathf.Sin(a0) * rN), p + new Vector3(Mathf.Cos(a1) * rN, yN, Mathf.Sin(a1) * rN), .012f, 8);
            }
            for (int q = 0; q < 4; q++) { float a = q * Mathf.PI * .5f + Mathf.PI * .25f; ch.Rura(p + Vector3.up * yN, p + new Vector3(Mathf.Cos(a) * rN, yN, Mathf.Sin(a) * rN), .009f, 6); }
            ch.Walec(p + Vector3.up * .68f * K, .15f * K, .03f, 24);
            BraxUliceKit.S(skora).PoduchaOkragla(p + Vector3.up * (.71f * K), .21f * K, .11f * K, .045f * K, .018f);
            BraxMiastoA.Siedzisko(root, "HOKER", p + Vector3.up * (.82f * K + .03f), Vector3.left);
            var g = new GameObject("kolizja: hoker"); g.transform.SetParent(kol, false); g.transform.position = p + Vector3.up * .4f * K;
            var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .2f * K; cc.height = .8f * K;
        }
        // zaplecze: ściana z drzwiami, skrzynki, kegi, chłodnia, regał
        var sciana = BraxMiastoA.Mat("NOVA WC: ściana tynk grafit", new Color(.12f, .12f, .13f), .8f);
        var sw = BraxUliceKit.S(sciana);
        sw.Box(new Vector3(ZX0, F, ZZ0 - .2f), new Vector3(49.4f, C, ZZ0), 4 | 8); sw.Box(new Vector3(50.6f, F, ZZ0 - .2f), new Vector3(HX1, C, ZZ0), 4 | 8);
        sw.Box(new Vector3(49.4f, F + 2.6f, ZZ0 - .2f), new Vector3(50.6f, C, ZZ0), 8);
        sw.Box(new Vector3(ZX0 - .2f, F, ZZ0 - .2f), new Vector3(ZX0, C, HZ1), 4 | 8);
        BraxMiastoA.Kol(kol, "ściana zaplecza", new Vector3(ZX0, F, ZZ0 - .2f), new Vector3(49.4f, C, ZZ0));
        BraxMiastoA.Kol(kol, "ściana zaplecza", new Vector3(50.6f, F, ZZ0 - .2f), new Vector3(HX1, C, ZZ0));
        BraxMiastoA.Kol(kol, "ściana zaplecza zachód", new Vector3(ZX0 - .2f, F, ZZ0 - .2f), new Vector3(ZX0, C, HZ1));
        var skrz = BraxUliceKit.S(BraxMiastoA.Mat("NOVA: skrzynki plastikowe (kolory)", Color.white, .7f));
        for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++)
            {
                skrz.kolor = (i + j) % 2 == 0 ? BraxUliceKit.C(.75f, .1f, .1f) : BraxUliceKit.C(.15f, .35f, .2f);
                var p = new Vector3(ZX0 + .3f + i * .45f, F + j * .3f, HZ1 - .6f);
                skrz.Box(p, p + new Vector3(.4f, .28f, .3f));
            }
        skrz.kolor = BraxUliceKit.Bialy;
        BraxMiastoA.Kol(kol, "skrzynki", new Vector3(ZX0 + .3f, F, HZ1 - .6f), new Vector3(ZX0 + 2.1f, F + 1.2f, HZ1 - .3f));
        for (int i = 0; i < 3; i++) { var p = new Vector3(HX1 - .4f, F, ZZ0 + .6f + i * .7f); BraxUliceKit.S(chrom).Walec(p, .26f, .6f * K, 16); }
        BraxMiastoA.Kol(kol, "kegi", new Vector3(HX1 - .7f, F, ZZ0 + .3f), new Vector3(HX1, F + .6f * K, ZZ0 + 2.2f));
        BraxRegion.Warm(root, "Zaplecze baru: światło", new Vector3(49.4f, 3.6f, 59.3f), 1.0f, 6f);
        BraxMiastoA.Akcja(root, "nova_bar", "ZAMÓW", "Barman: Co podać? Woda, sok albo coś mocniejszego.", new Vector3(BX0 - .7f, F, 49.5f), 1.8f);
        BraxMiastoA.Akcja(root, "nova_zaplecze", "ZAPLECZE", "Zaplecze baru: dostawy przyjmujemy od podwórka.", new Vector3(50.0f, F, 56.5f), 1.4f);
    }

    // ================= LOŻE =================
    static void Loze()
    {
        // 10.11 (właściciel 12:25: „czarną skórę… a najlepiej pikowaną”): siedziska i oparcia lóż — czarna skóra pikowana
        var welur = SkoraPik("siedziska loży — skóra pikowana czarna", Czarna);
        var welurC = SkoraPik("oparcia loży — skóra pikowana czarna", Czarna);
        var blat = MarmurCzarny;
        var inox = Mosiadz;
        var mlecz = BraxMiastoA.Mat("NOVA: szkło mleczne przegród", new Color(.75f, .72f, .78f, .55f), .2f);
        var lmag = BraxMiastoA.Swieci("NOVA: listwa LED magenta", new Color(.9f, .3f, .8f), new Color(2.2f, .35f, 1.8f));
        float sh = .45f * K, gl = .62f * K, op = .95f * K;   // wysokość siedziska, głębokość siedziska, wysokość oparcia
        float[] z0s = { 42.9f, 46.4f, 49.9f, 53.4f, 56.9f };
        float x0 = HX0, xb = HX0 + .25f, xF = HX0 + 2.45f;
        foreach (float z0 in z0s)
        {
            float z1 = z0 + 3.1f, zc = (z0 + z1) * .5f;
            var w = BraxUliceKit.S(welur); var wc = BraxUliceKit.S(welurC); var cok = BraxUliceKit.S(Orzech);
            // 10.11 (właściciel 10:29: „miękkie, okrągłe poduszki”): cokół z orzecha, na nim MIĘKKIE poduchy o zaokrąglonych krawędziach
            // i wypchniętych licach (UlSiatka.Poducha) — siedzisko wzdłuż ściany z trzech poduch, boki U z dwóch, oparcia pikowane
            // nad każdą poduchą; 1.2 cm przerwy między poduchami (czytelna tapicerka)
            float hc = .14f * K, pr = .065f * K, po = .055f * K;
            cok.Box(new Vector3(x0 + .02f, F, z0 - .1f), new Vector3(xb + gl - .04f, F + hc, z1 + .1f));
            cok.Box(new Vector3(xb + gl - .04f, F, z0 - .1f), new Vector3(xF - .04f, F + hc, z0 + .05f + gl - .04f));
            cok.Box(new Vector3(xb + gl - .04f, F, z1 - .05f - gl + .04f), new Vector3(xF - .04f, F + hc, z1 + .1f));
            float dz = (z1 - z0 - .1f) / 3f;
            for (int q = 0; q < 3; q++)
            {
                float a0 = z0 + .05f + q * dz + .006f, a1 = a0 + dz - .012f;
                w.Poducha(new Vector3(xb, F + hc, a0), new Vector3(xb + gl, F + sh, a1), pr, .02f);
                wc.Poducha(new Vector3(x0 + .02f, F + hc, a0), new Vector3(xb, F + op, a1), po, .022f);
            }
            float dx = (xF - (xb + gl + .01f)) / 2f;
            for (int q = 0; q < 2; q++)
            {
                float a0 = xb + gl + .01f + q * dx + .006f, a1 = a0 + dx - .012f;
                w.Poducha(new Vector3(a0, F + hc, z0 + .05f), new Vector3(a1, F + sh, z0 + .05f + gl), pr, .02f);
                w.Poducha(new Vector3(a0, F + hc, z1 - .05f - gl), new Vector3(a1, F + sh, z1 - .05f), pr, .02f);
            }
            wc.Poducha(new Vector3(xb + .006f, F + hc, z0 - .1f), new Vector3(xF, F + op, z0 + .05f), po, .022f);
            wc.Poducha(new Vector3(xb + .006f, F + hc, z1 - .05f), new Vector3(xF, F + op, z1 + .1f), po, .022f);
            // pikowanie oparcia: listwy co 0.3 m (1 cm przed licem)
            // (pikowanie oparcia — w teksturze skóry pikowanej, guziki w węzłach rombów)
            // stolik niski z kubełkiem i kieliszkami
            var c = new Vector3(xb + gl + .55f, F, zc);
            BraxUliceKit.S(blat).Walec(c + Vector3.up * (.5f * K - .03f), .42f * K, .03f, 24);
            BraxUliceKit.S(inox).Rura(c + Vector3.up * .02f, c + Vector3.up * (.5f * K - .03f), .05f, 10); BraxUliceKit.S(inox).Walec(c, .3f * K, .02f, 20);
            var kub = c + Vector3.up * (.5f * K) + new Vector3(.1f, 0, .05f);
            BraxUliceKit.S(inox).Walec(kub, .1f * K, .2f * K, 14);
            var but = BraxUliceKit.S(BraxMiastoA.Mat("NOVA: butelki (kolory z wierzchołków)", Color.white, .12f, .15f));
            but.kolor = new Color32(28, 72, 40, 255); but.Rura(kub + Vector3.up * .1f, kub + new Vector3(.02f, .34f * K, 0), .035f * K, 10);   // szampan w kubełku: zielone szkło
            but.kolor = new Color32(196, 160, 70, 255); but.Rura(kub + new Vector3(.02f, .34f * K, 0), kub + new Vector3(.025f, .42f * K, 0), .014f * K, 8); but.kolor = BraxUliceKit.Bialy;   // złota folia szyjki
            var g = new GameObject("kolizja: stolik loży"); g.transform.SetParent(kol, false); g.transform.position = c + Vector3.up * .25f * K;
            var cc = g.AddComponent<CapsuleCollider>(); cc.radius = .42f * K; cc.height = .5f * K;
            BraxMiastoA.Kol(kol, "loża siedzisko", new Vector3(x0, F, z0 - .1f), new Vector3(xb + gl, F + sh, z1 + .1f));
            BraxMiastoA.Kol(kol, "loża bok", new Vector3(xb, F, z0 - .1f), new Vector3(xF, F + sh, z0 + .05f + gl));
            BraxMiastoA.Kol(kol, "loża bok", new Vector3(xb, F, z1 - .05f - gl), new Vector3(xF, F + sh, z1 + .1f));
            BraxMiastoA.Siedzisko(root, "LOŻA", new Vector3(xb + gl * .5f, F + sh, zc), Vector3.right);
            BraxMiastoA.Siedzisko(root, "LOŻA", new Vector3(xb + gl + .6f, F + sh, z0 + .05f + gl * .5f), Vector3.forward);
            BraxMiastoA.Siedzisko(root, "LOŻA", new Vector3(xb + gl + .6f, F + sh, z1 - .05f - gl * .5f), Vector3.back);
            // przegroda ze szkła mlecznego z ramą (między lożami) i listwa LED na ścianie za oparciem
            if (z0 > z0s[0]) { BraxUliceKit.S(mlecz).Box(new Vector3(xb, F + op, z0 - .14f), new Vector3(xF, F + 1.75f * K, z0 - .1f)); BraxUliceKit.S(inox).Box(new Vector3(xb, F + 1.75f * K, z0 - .15f), new Vector3(xF, F + 1.75f * K + .03f, z0 - .09f)); }
            BraxUliceKit.S(lmag).Box(new Vector3(HX0 + .08f, F + op + .25f, z0 + .2f), new Vector3(HX0 + .11f, F + op + .29f, z1 - .2f));   // przed lamelami
        }
        BraxMiastoA.Akcja(root, "nova_loza_vip", "ZAREZERWUJ LOŻĘ", "Kelnerka: Loża na dziś wolna — rezerwacja przy barze.", new Vector3(xF + .6f, F, 51.5f), 1.6f);
    }

    // ================= DJ =================
    static void StanowiskoDJ()
    {
        var podest = BraxMiastoA.Mat("NOVA: podest DJ (czarny mat)", new Color(.03f, .03f, .035f), .8f);
        var konsola = BraxMiastoA.Mat("NOVA: konsoleta (czarny lakier)", new Color(.02f, .02f, .025f), .3f, .2f);
        var alu = BraxMiastoA.Mat("NOVA: aluminium sprzętu", new Color(.35f, .36f, .38f), .4f, .8f);
        var glosnik = BraxMiastoA.Mat("NOVA: głośniki (czarna farba strukturalna)", new Color(.03f, .03f, .035f), .85f);
        var siatka = BraxMiastoA.Mat("NOVA: siatka głośnika", new Color(.06f, .06f, .065f), .7f, .4f);
        var ekranM = BraxMiastoA.ZShadera("Braxhorn/NovaParkiet", "NOVA ekran LED za DJ", m => { m.SetFloat("_Plytka", .5f); m.SetVector("_Srodek", new Vector4((RX0 + RX1) * .5f, 2.4f, 0, 0)); m.SetFloat("_Pion", 1f); m.SetFloat("_Moc", 1.0f); });
        var lmag = BraxMiastoA.Swieci("NOVA: listwa LED magenta", new Color(.9f, .3f, .8f), new Color(2.2f, .35f, 1.8f));
        float top = F + RH;
        // podest (2 stopnie po 0.225 m od wschodu — pod krok gracza 0.36)
        BraxUliceKit.S(podest).Box(new Vector3(RX0, F, RZ0), new Vector3(RX1, top, RZ1), 4);
        BraxUliceKit.S(lmag).Box(new Vector3(RX0, F + .05f, RZ0 - .03f), new Vector3(RX1, F + .09f, RZ0));
        BraxMiastoA.Kol(kol, "podest DJ", new Vector3(RX0, F, RZ0), new Vector3(RX1, top, RZ1));
        for (int i = 0; i < 2; i++)
        {
            float x0 = RX1 + (1 - i) * .4f, h = F + RH * (i + 1) / 3f;
            BraxUliceKit.S(podest).Box(new Vector3(RX1, F, 58.4f), new Vector3(x0 + .4f, h, 59.8f), 4);
            BraxMiastoA.Kol(kol, "stopień podestu", new Vector3(RX1, F, 58.4f), new Vector3(x0 + .4f, h, 59.8f));
        }
        // konsoleta DJ (front ze świecącym logo pasem), blat na 1.0 m × k nad podestem
        float dz0 = 57.1f, dz1 = 57.95f, dx0 = 39.3f, dx1 = 42.7f, dh = 1.0f * K;
        BraxUliceKit.S(SkoraPik("front konsolety DJ — skóra pikowana czarna", Czarna)).Box(new Vector3(dx0, top, dz0), new Vector3(dx1, top + dh - .05f, dz1), 4);
        BraxUliceKit.S(alu).Box(new Vector3(dx0 - .03f, top + dh - .05f, dz0 - .03f), new Vector3(dx1 + .03f, top + dh, dz1 + .03f), 4);
        BraxUliceKit.S(lmag).Box(new Vector3(dx0 + .1f, top + .25f, dz0 - .03f), new Vector3(dx1 - .1f, top + .29f, dz0));
        BraxMiastoA.Kol(kol, "konsoleta DJ", new Vector3(dx0, top, dz0), new Vector3(dx1, top + dh, dz1));
        // dwa odtwarzacze z talerzami (jog) po bokach miksera
        foreach (float x in new[] { 39.95f, 42.05f })
        {
            var p = new Vector3(x, top + dh, (dz0 + dz1) * .5f);
            BraxUliceKit.S(konsola).Box(p + new Vector3(-.33f, 0, -.4f), p + new Vector3(.33f, .1f, .4f));
            BraxUliceKit.S(alu).Walec(p + new Vector3(0, .1f, .05f), .17f, .025f, 24);
            BraxUliceKit.S(siatka).Walec(p + new Vector3(0, .125f, .05f), .12f, .006f, 20);
            for (int i = 0; i < 6; i++) BraxUliceKit.S(lmag).Box(p + new Vector3(-.28f + i * .05f, .1f, -.33f), p + new Vector3(-.25f + i * .05f, .115f, -.3f));
        }
        var mikser = BraxRegion.Model("dj_mixer") ?? (System.IO.File.Exists("Assets/Source/Claude/Nova/dj_mixer.glb") ? LoadGlb("Assets/Source/Claude/Nova/dj_mixer.glb") : null);
        if (mikser) BraxRegion.Put(root, mikser, "Mikser DJ (dj_mixer — CC BY Astate)", new Vector3(41f, top + dh, (dz0 + dz1) * .5f), 180f, 1f, false, .14f * K, 0f, false);
        else BraxUliceKit.S(konsola).Box(new Vector3(40.75f, top + dh, 57.2f), new Vector3(41.25f, top + dh + .1f, 57.85f));
        BraxMiastoA.Akcja(root, "nova_dj", "POROZMAWIAJ Z DJ-em", "DJ: Zamówienia muzyczne po północy. Teraz leci set rezydenta.", new Vector3(43.9f, top, 58.7f), 1.5f);
        // ekran LED za DJ-em (10 cm przed ścianą) z ramą
        float ez = HZ1 - .12f;
        // 3.11 15:59 (właściciel): MURAL NOVA CLUB za DJ-em (grafika z czatu, 3:1) zamiast wzoru LED; bez paczki — ekran LED
        // 10.11 (właściciel 09:26: „ten mural miał być na całą ścianę”): mural NOVA CLUB na CAŁEJ ścianie północnej sali — od ściany
        // zachodniej (x 32.6) do ściany zaplecza baru (x 47.2), od posadzki po sufit; kadr wypełnia ścianę (proporcja zachowana,
        // nadmiar obcięty po równo z góry i z dołu). Bez czarnej obudowy ekranu. Bez paczki grafik — ekran LED w obudowie jak dotąd.
        // 10.11 11:xx (właściciel 10:41: „ten mural na całą ścianę koło DJ-a”): mural owinięty dalej na ścianę zaplecza od strony
        // parkietu (x 47.19, z 61.39 → 57.01) — cały koniec sali za DJ-em to mural, bez ciemnego bloku po prawej
        if (!MuralNaCalaSciane(HX0 + .01f, ZX0 - .21f, F + .01f, C - .02f, HZ1 - .012f, ZX0 - .212f, ZZ0 - .19f))
        {
            BraxUliceKit.S(ekranM).Quad(new Vector3(RX1 + 1f, F + .9f, ez), new Vector3(RX0 - 1f, F + .9f, ez), new Vector3(RX0 - 1f, C - .35f, ez), new Vector3(RX1 + 1f, C - .35f, ez), Vector3.back);
            BraxUliceKit.S(glosnik).Box(new Vector3(RX0 - 1.08f, F + .82f, ez), new Vector3(RX1 + 1.08f, C - .27f, HZ1 - .01f), 32);   // obudowa (lico ekranu 2 cm przed nią… ekran leży w otworze obudowy)
        }
        // 10.11 (właściciel 10:39: „koło DJ-a dwie kolumny, jak schody, wiszą, czarne — ma zniknąć”): podwieszone głośniki liniowe
        // (2 × 8 czarnych modułów w łuku) usunięte — zasłaniały mural. Zostają suby na posadzce przed podestem.
        foreach (float x in new[] { RX0 + .9f, RX1 - .9f })
        {
            BraxUliceKit.S(glosnik).Box(new Vector3(x - .65f, F, RZ0 - 1.05f), new Vector3(x + .65f, F + .75f, RZ0 - .05f));
            BraxUliceKit.S(siatka).Box(new Vector3(x - .58f, F + .07f, RZ0 - 1.07f), new Vector3(x + .58f, F + .68f, RZ0 - 1.05f));
            BraxMiastoA.Kol(kol, "subwoofer", new Vector3(x - .65f, F, RZ0 - 1.07f), new Vector3(x + .65f, F + .75f, RZ0 - .05f));
        }
        // głośniki narożne na wspornikach
        // (10.11: bez głośnika w narożniku przy ścianie z muralem — wisiał na rogu muralu)
        foreach (var p in new[] { new Vector3(HX0 + .4f, 3.5f, HZ0 + .4f), new Vector3(HX1 - .4f, 3.5f, HZ0 + .4f) })
        {
            var d = (new Vector3((DX0 + DX1) * .5f, 1.5f, (DZ0 + DZ1) * .5f) - p).normalized; var r = Vector3.Cross(Vector3.up, d).normalized; var u = Vector3.Cross(d, r);
            BraxUliceKit.S(glosnik).OBox(p, r, u, d, .5f, .75f, .45f); BraxUliceKit.S(siatka).OBox(p + d * .23f, r, u, d, .44f, .69f, .01f);
        }
    }

    // Mural właściciela (szyldy/mural_nova.jpg) na prostokącie ściany x0…x1, y0…y1 w płaszczyźnie z (lico do sali: −z).
    /// <summary>Mural NOVA CLUB na ścianie z = const (widz patrzy w +z), od x0 do x1. Gdy podano sx: ciąg dalszy za narożnikiem
    /// wewnętrznym na ścianie x = sx (lico ku −x) od z (narożnik) do sz0 — obraz rozwinięty na obie ściany jak ekran LED owinięty
    /// wokół rogu. Kadr wypełnia całość; nadmiar wysokości obcinany głównie z dołu (u góry logo NOVA CLUB; dół zasłania podest DJ).</summary>
    static bool MuralNaCalaSciane(float x0, float x1, float y0, float y1, float z, float sx = float.NaN, float sz0 = 0f)
    {
        const string p = "Assets/Source/Prywatne/Grafiki/szyldy/mural_nova.jpg";
        if (!File.Exists(p)) { Debug.Log("BRAXHORN_MURAL brak " + p + " — NOVA: ekran LED zamiast muralu"); return false; }
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p); if (!tex) return false;
        bool bok = !float.IsNaN(sx);
        float asp = (float)tex.width / Mathf.Max(1, tex.height), Wn = x1 - x0, Wb = bok ? z - sz0 : 0f, W = Wn + Wb, H = y1 - y0;
        float u0 = 0f, u1 = 1f, v0 = 0f, v1 = 1f;
        // 10.11 (właściciel 17:46: „szczególnie ten mural ucięty”, 18:27 „zrób to jeszcze”): CAŁY obraz, bez obcinania.
        // Ściana szersza niż obraz → mural na pełną wysokość, szerokość = wysokość × proporcje, na środku ściany głównej
        // (gdy się nie mieści — reszta za narożnikiem). Dawniej kadr wypełniał ścianę 19 × 4,4 m i ucinał ok. 31 % wysokości.
        if (W / H > asp)
        {
            float Wobr = H * asp;
            if (Wobr <= Wn) { float c = (x0 + x1) * .5f; x0 = c - Wobr * .5f; x1 = c + Wobr * .5f; Wn = Wobr; bok = false; Wb = 0f; }
            else if (bok) { Wb = Wobr - Wn; sz0 = z - Wb; }
            W = Wn + Wb;
        }
        else { float vis = W / (H * asp); u0 = (1f - vis) * .5f; u1 = 1f - u0; }
        float uR = u0 + (u1 - u0) * (Wn / W);   // u na narożniku
        var m = new Material(Shader.Find("Standard")) { name = "mural_nova_cala_sciana", mainTexture = tex };
        m.SetFloat("_Glossiness", .25f); m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", new Color(.6f, .6f, .6f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        Directory.CreateDirectory(BraxMiastoA.Out + "/Materialy");
        BraxZapisAssetu.Utworz(m, BraxMiastoA.Out + "/Materialy/mural_nova_cala_sciana.mat");
        var mesh = new Mesh { name = "NOVA: mural na całą ścianę" };
        var v = new List<Vector3> { new Vector3(x0, y0, z), new Vector3(x1, y0, z), new Vector3(x1, y1, z), new Vector3(x0, y1, z) };
        var uv = new List<Vector2> { new Vector2(u0, v0), new Vector2(uR, v0), new Vector2(uR, v1), new Vector2(u0, v1) };   // widz patrzy w +z: +x po prawej
        var nr = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        var tr = new List<int> { 0, 3, 2, 0, 2, 1 };
        if (bok)
        {   // ściana za narożnikiem: widz patrzy w +x, po prawej −z (od narożnika z ku sz0)
            v.AddRange(new[] { new Vector3(sx, y0, z), new Vector3(sx, y0, sz0), new Vector3(sx, y1, sz0), new Vector3(sx, y1, z) });
            uv.AddRange(new[] { new Vector2(uR, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(uR, v1) });
            nr.AddRange(new[] { Vector3.left, Vector3.left, Vector3.left, Vector3.left });
            tr.AddRange(new[] { 4, 7, 6, 4, 6, 5 });
        }
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetNormals(nr); mesh.SetTriangles(tr, 0); mesh.RecalculateBounds();
        BraxMiastoA.Obiekt(root, "mural nova_club", mesh, m);   // nazwa chroniona w BraxNovaEntranceFinish (szyld właściciela)
        Debug.Log("BRAXHORN_MURAL mural_nova CAŁA ŚCIANA " + Wn.ToString("F1") + (bok ? " + za narożnikiem " + Wb.ToString("F1") : "") + "×" + H.ToString("F2") + " m, kadr u " + u0.ToString("F2") + "…" + u1.ToString("F2") + ", v " + v0.ToString("F2") + "…" + v1.ToString("F2"));
        return true;
    }

    static void KulaZLusterek(Transform piv, float r)
    {
        // paleta 16 × 1 px (bez filtrowania): kolor fasetki = albedo i emisja
        string dir = BraxMiastoA.Out + "/Tekstury"; Directory.CreateDirectory(dir);
        string png = dir + "/nova_kula_paleta.png";
        Color[] pal =
        {
            new Color(.97f, .97f, 1f), new Color(.88f, .90f, .95f), new Color(.78f, .80f, .86f), new Color(.66f, .68f, .75f),
            new Color(1f, .86f, .96f), new Color(.84f, .92f, 1f), new Color(.95f, .95f, .9f), new Color(.55f, .57f, .64f),
            new Color(.92f, .93f, .98f), new Color(.40f, .42f, .48f), new Color(.99f, .99f, .99f), new Color(.72f, .75f, .82f),
            new Color(.90f, .80f, 1f), new Color(.30f, .31f, .36f), new Color(.85f, .87f, .92f), new Color(.98f, .92f, .86f),
        };
        if (!File.Exists(png))
        {
            var t = new Texture2D(16, 1, TextureFormat.RGBA32, false); t.SetPixels(pal); t.Apply();
            File.WriteAllBytes(png, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(png, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(png);
            imp.mipmapEnabled = false; imp.filterMode = FilterMode.Point; imp.wrapMode = TextureWrapMode.Clamp;
            imp.textureCompression = TextureImporterCompression.Uncompressed; imp.npotScale = TextureImporterNPOTScale.None; imp.SaveAndReimport();
        }
        var paleta = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
        var lustra = BraxCityMaterials.Surface("A świeci: NOVA kula — lusterka (paleta = emisja)", Color.white, .08f, .7f);
        if (paleta) { lustra.SetTexture("_MainTex", paleta); lustra.SetTexture("_EmissionMap", paleta); }
        lustra.SetColor("_EmissionColor", paleta ? new Color(1.05f, 1.05f, 1.1f) : new Color(.7f, .7f, .75f)); EditorUtility.SetDirty(lustra);
        var fuga = BraxMiastoA.Mat("NOVA: kula — fuga (rdzeń)", new Color(.05f, .05f, .06f), .6f, .3f);
        var rng = new System.Random(4242);
        var s = new UlSiatka(); var core = new UlSiatka();
        const int pasy = 18, fasety = 36;
        Vector3 P(float th, float ph, float rr) => new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * rr;
        for (int j = 0; j < pasy; j++)
        {
            float t0 = Mathf.PI * j / pasy, t1 = Mathf.PI * (j + 1) / pasy;
            int nf = Mathf.Max(6, Mathf.RoundToInt(fasety * Mathf.Sin((t0 + t1) * .5f)));   // przy biegunach mniej, ale kwadratowe lusterka
            for (int i = 0; i < nf; i++)
            {
                float p0 = 2f * Mathf.PI * i / nf, p1 = 2f * Mathf.PI * (i + 1) / nf;
                // rdzeń (fuga) — pełna komórka na promieniu 0.985 r
                core.Quad(P(t0, p0, r * .985f), P(t0, p1, r * .985f), P(t1, p1, r * .985f), P(t1, p0, r * .985f), P((t0 + t1) * .5f, (p0 + p1) * .5f, 1f));
                // lusterko: 5 % mniejsze na każdym brzegu, płaskie, normalna = środek komórki
                float ta = Mathf.Lerp(t0, t1, .05f), tb = Mathf.Lerp(t0, t1, .95f), pa = Mathf.Lerp(p0, p1, .05f), pb = Mathf.Lerp(p0, p1, .95f);
                var nn = P((t0 + t1) * .5f, (p0 + p1) * .5f, 1f);
                int k0 = s.Wierzcholki;
                s.Quad(P(ta, pa, r), P(ta, pb, r), P(tb, pb, r), P(tb, pa, r), nn);
                var uvF = new Vector2((rng.Next(16) + .5f) / 16f, .5f);
                for (int q = 0; q < 4; q++) { s.uv[k0 + q] = uvF; s.n[k0 + q] = nn; }
            }
        }
        BraxMiastoA.Obiekt(piv, "Kula lustrzana — lusterka (świecą)", s, lustra).transform.localPosition = Vector3.zero;
        BraxMiastoA.Obiekt(piv, "Kula lustrzana — fuga", core, fuga).transform.localPosition = Vector3.zero;
        // uchwyt: kołnierz i krótki pręt nad kulą (pręt do stropu stawia kratownica)
        var ch = new UlSiatka(); ch.Walec(Vector3.up * (r - .01f), .05f, .06f, 14); ch.Walec(Vector3.up * (r + .05f), .015f, .25f, 8);
        BraxMiastoA.Obiekt(piv, "Kula lustrzana — uchwyt", ch, BraxMiastoA.Mat("NOVA: chrom słupków", new Color(.85f, .86f, .88f), .12f, 1f)).transform.localPosition = Vector3.zero;
    }

    static GameObject LoadGlb(string p)
    {
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }

    // ================= KRATOWNICA, LASERY, GŁOWICE =================
    const float TY = 4.05f;   // dolny pas kratownicy
    static void Kratownica()
    {
        var alu = BraxMiastoA.Mat("NOVA: kratownica aluminium", new Color(.62f, .63f, .66f), .35f, .85f);
        var s = BraxUliceKit.S(alu); float a = .29f, x0 = DX0, x1 = DX1, z0 = DZ0, z1 = DZ1;
        void Belka(Vector3 p, Vector3 q)
        {
            var d = (q - p); float L = d.magnitude; d /= L; var side = Vector3.Cross(Vector3.up, d).normalized;
            foreach (var o in new[] { Vector3.zero, side * a, Vector3.up * a, side * a + Vector3.up * a }) s.Rura(p + o, q + o, .024f, 6, false);
            for (float t = 0; t < L; t += .5f)
            {
                var b0 = p + d * t; var b1 = p + d * Mathf.Min(L, t + .5f);
                s.Rura(b0, b1 + Vector3.up * a, .009f, 4, false); s.Rura(b0 + side * a, b1 + side * a + Vector3.up * a, .009f, 4, false);
                s.Rura(b0, b0 + side * a, .009f, 4, false); s.Rura(b0 + Vector3.up * a, b0 + side * a + Vector3.up * a, .009f, 4, false);
            }
        }
        Belka(new Vector3(x0, TY, z0), new Vector3(x1, TY, z0)); Belka(new Vector3(x1, TY, z1), new Vector3(x0, TY, z1));
        Belka(new Vector3(x0, TY, z1), new Vector3(x0, TY, z0)); Belka(new Vector3(x1, TY, z0), new Vector3(x1, TY, z1));
        // zawiesia (wciągniki) do stropu
        foreach (var p in new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1) })
        {
            s.Rura(new Vector3(p.x, TY + a, p.z), new Vector3(p.x, C, p.z), .012f, 6);
            BraxUliceKit.S(BraxMiastoA.Mat("NOVA: konsoleta (czarny lakier)", new Color(.02f, .02f, .025f), .3f, .2f)).Box(new Vector3(p.x - .12f, C - .3f, p.z - .1f), new Vector3(p.x + .12f, C - .05f, p.z + .1f));
        }
        // pręt kuli i silnik
        s.Rura(new Vector3(Kula.x, Kula.y + .3f * K, Kula.z), new Vector3(Kula.x, C, Kula.z), .015f, 6);
    }

    // ================= EFEKTY (osobne obiekty — ruchome albo z własnym shaderem) =================
    static void Efekty()
    {
        // KULA: model CC-BY (WwWuengy) w osi obrotu; iskry na jej powierzchni (dziecko — obracają się z nią)
        var piv = new GameObject("Kula lustrzana (obrót)").transform; piv.SetParent(root, false); piv.position = Kula;
        float d = .65f * K;
        // 10.11 (właściciel 09:26: „kula ma świecić, kurwa, się kręcić”; w APK szaroniebieska gładka bryła — materiał modelu po
        // konwersji matowy, obrotu na gładkiej kuli nie widać): kula z kodu z LUSTERKAMI — 18 pasów × 36 fasetek, każda fasetka
        // płaska (własna normalna), 5 % szczeliny z ciemną fugą (rdzeń), jasność i odcień fasetki z palety (srebro, biel, róż,
        // błękit, kilka ciemnych) — paleta jest też mapą EMISJI, więc kula ŚWIECI, a obrót (12 obr./min) widać po wzorze lusterek.
        KulaZLusterek(piv, d * .5f);
        var isk = new UlSiatka(); var rng = new System.Random(77); var uv1 = new List<Vector2>();
        for (int i = 0; i < 56; i++)
        {
            float yv = 1f - 2f * (i + .5f) / 56f, rr = Mathf.Sqrt(1 - yv * yv), th = i * 2.39996f;
            var p = new Vector3(Mathf.Cos(th) * rr, yv, Mathf.Sin(th) * rr) * (d * .5f + .01f);
            int k0 = isk.Wierzcholki;
            isk.Quad(p, p, p, p, Vector3.up);
            isk.uv[k0] = new Vector2(-1, -1); isk.uv[k0 + 1] = new Vector2(1, -1); isk.uv[k0 + 2] = new Vector2(1, 1); isk.uv[k0 + 3] = new Vector2(-1, 1);
            float a = (float)rng.NextDouble(), b = (float)rng.NextDouble(); for (int q = 0; q < 4; q++) uv1.Add(new Vector2(a, b));
        }
        var iskM = isk.Zbuduj("NOVA iskry kuli"); iskM.SetUVs(1, uv1); iskM.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
        var iskG = BraxMiastoA.Obiekt(piv, "Kula: iskry (błyski)", iskM, BraxMiastoA.ZShadera("Braxhorn/NovaIskry", "NOVA iskry kuli", m => { m.SetFloat("_Rozmiar", .28f); m.SetFloat("_Moc", 2.6f); }));
        iskG.transform.localPosition = Vector3.zero;
        // PLAMKI od kuli na ścianach, suficie i podłodze (shader liczy trafienie; obrót = kąt kuli)
        var kr = new UlSiatka(); var uvK = new List<Vector2>();
        for (int i = 0; i < 260; i++)
        {
            float yv = Mathf.Lerp(.55f, -.95f, (i + .5f) / 260f), rr = Mathf.Sqrt(1 - yv * yv), th = i * 2.39996f;
            var dir = new Vector3(Mathf.Cos(th) * rr, yv, Mathf.Sin(th) * rr);
            int k0 = kr.Wierzcholki;
            kr.Quad(Kula, Kula, Kula, Kula, dir);
            kr.uv[k0] = new Vector2(-1, -1); kr.uv[k0 + 1] = new Vector2(1, -1); kr.uv[k0 + 2] = new Vector2(1, 1); kr.uv[k0 + 3] = new Vector2(-1, 1);
            for (int q = 0; q < 4; q++) kr.n[k0 + q] = dir;
            float a = (float)rng.NextDouble(), b = (float)rng.NextDouble(); for (int q = 0; q < 4; q++) uvK.Add(new Vector2(a, b));
        }
        var krM = kr.Zbuduj("NOVA plamki kuli"); krM.SetUVs(1, uvK);
        var sala = new Bounds(); sala.SetMinMax(new Vector3(HX0, F, HZ0), new Vector3(HX1, C, HZ1)); krM.bounds = sala;
        BraxMiastoA.Obiekt(root, "Kula: plamki na ścianach (NovaKropki)", krM, BraxMiastoA.ZShadera("Braxhorn/NovaKropki", "NOVA plamki kuli", m =>
        {
            m.SetVector("_Srodek", Kula); m.SetVector("_SalaMin", new Vector4(HX0, F, HZ0, 0)); m.SetVector("_SalaMax", new Vector4(HX1, C, HZ1, 0));
            m.SetFloat("_Rozmiar", .02f); m.SetFloat("_Moc", 1.5f);
        }));
        // reflektor punktowy (pin-spot) na kratownicy wycelowany w kulę + snop
        var promBialy = BraxMiastoA.ZShadera("Braxhorn/WnetrzaPromien", "NOVA snop biały", m => { m.SetColor("_Tint", new Color(.9f, .9f, 1f)); m.SetFloat("_Moc", .35f); });
        var pin = new Vector3(DX0 + .15f, TY - .1f, DZ0 + .15f);
        Promienie(root, "Pin-spot na kulę", pin, (Kula - pin).normalized, 0f, 1, (Kula - pin).magnitude, .03f, .12f, promBialy, false);
        // LASERY w narożnikach kratownicy: oprawa (laser_light.glb, CC-BY JanoschR) + wachlarz 7 promieni (omiatanie w BraxNovaEfekty)
        var laserSrc = LoadGlb("Assets/Source/Claude/Nova/laser_light.glb");
        var barwy = new[] { new Color(.15f, 1f, .25f), new Color(1f, .1f, .25f), new Color(.25f, .4f, 1f), new Color(1f, .15f, .9f) };
        var narozniki = new[] { new Vector3(DX0 + .15f, TY - .05f, DZ1 - .15f), new Vector3(DX1 - .15f, TY - .05f, DZ1 - .15f), new Vector3(DX1 - .15f, TY - .05f, DZ0 + .15f), new Vector3(DX0 + .15f, TY - .05f, DZ0 + .5f) };
        for (int i = 0; i < 4; i++)
        {
            var p = narozniki[i]; var cel = new Vector3((DX0 + DX1) * .5f, F + .8f, (DZ0 + DZ1) * .5f) - p; cel.y = 0;
            var pv = new GameObject("Laser " + (i + 1) + " (obrotnica)").transform; pv.SetParent(root, false); pv.position = p;
            pv.rotation = Quaternion.LookRotation(cel.normalized, Vector3.up) * Quaternion.Euler(18f, 0, 0);
            if (laserSrc)
            {
                var o = BraxRegion.Put(pv, laserSrc, "Oprawa lasera (laser_light — CC BY JanoschR)", p - Vector3.up * .3f, 0f, 1f, false, .3f * K, 0f, false);
                if (o) o.transform.rotation = pv.rotation * o.transform.rotation;
            }
            var col = barwy[i]; var mat = BraxMiastoA.ZShadera("Braxhorn/WnetrzaPromien", "NOVA laser " + i, m => { m.SetColor("_Tint", col); m.SetFloat("_Moc", 1.3f); });
            // 10.11 (właściciel 10:45: „lasery — nie że świeci jak było, tylko ewentualnie może mrugać”): cieńsze promienie, w grze
            // tylko KRÓTKIE błyski (BraxNovaEfekty.LaserBlysk) — przez większość czasu wiązek nie ma
            var wach = Promienie(pv, "Wachlarz promieni lasera " + (i + 1), p, pv.forward, 46f, 7, 12f, .006f, .028f, mat, true);
            lasery.Add(wach);
        }
        // GŁOWICE RUCHOME na kratownicy (po 3 na długich bokach): jarzmo + głowica + snop w dół
        var czarny = BraxMiastoA.Mat("NOVA: konsoleta (czarny lakier)", new Color(.02f, .02f, .025f), .3f, .2f);
        var snop = BraxMiastoA.ZShadera("Braxhorn/WnetrzaPromien", "NOVA snop głowicy", m => { m.SetColor("_Tint", new Color(.75f, .55f, 1f)); m.SetFloat("_Moc", .28f); });
        int n = 0;
        foreach (float z in new[] { DZ0 + .15f, DZ1 - .15f })
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(Mathf.Lerp(DX0, DX1, (i + 1) / 4f), TY - .05f, z);
                var g = new GameObject("Głowica ruchoma " + (++n)).transform; g.SetParent(root, false); g.position = p; g.rotation = Quaternion.LookRotation(Vector3.down, z < 50f ? Vector3.forward : Vector3.back);
                var s = new UlSiatka(); s.BoxC(new Vector3(0, 0, .05f), new Vector3(.3f, .3f, .1f)); s.BoxC(new Vector3(0, 0, .28f), new Vector3(.24f, .24f, .34f)); s.Walec(new Vector3(0, -.13f, .45f), .1f, .02f, 14);
                var gg = BraxMiastoA.Obiekt(g, "Głowica (korpus)", s, czarny); gg.transform.localPosition = Vector3.zero; gg.transform.localRotation = Quaternion.identity;
                Promienie(g, "Snop głowicy", p + g.forward * .45f, g.forward, 0f, 1, 6.5f, .08f, .75f, snop, false);
                glowice.Add(g);
            }
        // MIGACZE (właściciel 10.11 10:45: „światełka mrugające mają być jeszcze na dyskotece, oprócz kuli”): reflektory PAR LED
        // na kratownicy — czarna puszka z jarzmem, soczewka ciemna (zgaszona) i przed nią świecąca (włączana przez BraxNovaEfekty
        // w rytm: gonitwa kolorów, błyski na uderzenie, stroboskop). Do tego dwa białe stroboskopy przy DJ-u.
        var puszka = BraxMiastoA.Mat("NOVA: puszka PAR (czarny mat)", new Color(.03f, .03f, .035f), .5f, .4f);
        var ciemna = BraxMiastoA.Mat("NOVA: soczewka zgaszona", new Color(.08f, .08f, .09f), .15f, .2f);
        var barwyPar = new[] { new Color(1f, .1f, .7f), new Color(.2f, .4f, 1f), new Color(.15f, 1f, .5f), new Color(1f, .55f, .1f), new Color(.65f, .2f, 1f), new Color(.1f, .95f, 1f) };
        int nm = 0;
        void Migacz(Vector3 p, Vector3 cel, Color c, string rodzaj, float r)
        {
            var g = new GameObject("Migacz " + (++nm) + " (" + rodzaj + ")").transform; g.SetParent(root, false); g.position = p;
            g.rotation = Quaternion.LookRotation((cel - p).normalized, Vector3.up);
            var s = new UlSiatka(); s.Rura(new Vector3(0, 0, -.12f), new Vector3(0, 0, .1f), r + .03f, 16);
            s.Box(new Vector3(-r - .06f, -.02f, -.03f), new Vector3(-r - .04f, .2f, .03f)); s.Box(new Vector3(r + .04f, -.02f, -.03f), new Vector3(r + .06f, .2f, .03f));
            var o = BraxMiastoA.Obiekt(g, "Migacz: puszka", s, puszka); o.transform.localPosition = Vector3.zero; o.transform.localRotation = Quaternion.identity;
            var sc = new UlSiatka(); sc.Rura(new Vector3(0, 0, .1f), new Vector3(0, 0, .104f), r, 16);
            var oc = BraxMiastoA.Obiekt(g, "Migacz: soczewka zgaszona", sc, ciemna); oc.transform.localPosition = Vector3.zero; oc.transform.localRotation = Quaternion.identity;
            var sw = new UlSiatka(); sw.Rura(new Vector3(0, 0, .106f), new Vector3(0, 0, .11f), r - .005f, 16);
            var em = BraxMiastoA.Swieci("NOVA migacz " + ColorUtility.ToHtmlStringRGB(c), c, c * 3.2f);
            var os = BraxMiastoA.Obiekt(g, "Migacz: soczewka świecąca", sw, em); os.transform.localPosition = Vector3.zero; os.transform.localRotation = Quaternion.identity;
            migacze.Add(g);
        }
        var srodek = new Vector3((DX0 + DX1) * .5f, F, (DZ0 + DZ1) * .5f);
        for (int i = 0; i < 5; i++)
            foreach (float z in new[] { DZ0 + .15f, DZ1 - .15f })
            {
                float x = Mathf.Lerp(DX0 + .6f, DX1 - .6f, (i + .5f) / 5f);
                Migacz(new Vector3(x, TY - .22f, z), new Vector3(x, F, Mathf.Lerp(z, srodek.z, .45f)), barwyPar[(i + (z < 50f ? 0 : 3)) % barwyPar.Length], "PAR LED", .1f);
            }
        foreach (float x in new[] { DX0 + .15f, DX1 - .15f })
            for (int i = 0; i < 2; i++)
            {
                float z = Mathf.Lerp(DZ0 + 1.5f, DZ1 - 1.5f, (i + .5f) / 2f);
                Migacz(new Vector3(x, TY - .22f, z), new Vector3(Mathf.Lerp(x, srodek.x, .45f), F, z), barwyPar[(i * 2 + (x < 40f ? 1 : 4)) % barwyPar.Length], "PAR LED", .1f);
            }
        foreach (float x in new[] { RX0 + .25f, RX1 - .25f })
            Migacz(new Vector3(x, F + RH + 1.95f, RZ0 + .35f), new Vector3(x, F + 1.2f, DZ1 - 2f), Color.white, "stroboskop", .13f);
        BraxMiastoA.Log("NOVA efekty: kula " + "z lusterek (świeci, 12 obr/min)" + ", iskier 56, plamek 260, lasery " + (laserSrc ? "laser_light.glb" : "bez oprawy") + " ×4 (7 promieni, tylko krótkie błyski), głowic " + n + ", migaczy " + migacze.Count + " (PAR LED + 2 stroboskopy)");
    }

    /// <summary>Wachlarz promieni (każdy = dwa skrzyżowane pasy, uv.y wzdłuż) z punktu p w kierunku dir; rozpiętość w stopniach.
    /// Zwraca obiekt-rodzica (obracany przez BraxNovaEfekty dla laserów).</summary>
    static Transform Promienie(Transform rodzic, string nazwa, Vector3 p, Vector3 dir, float rozp, int ile, float dl, float w0, float w1, Material m, bool doLasera)
    {
        var g = new GameObject(nazwa).transform; g.SetParent(rodzic, false); g.position = p; g.rotation = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > .95f ? Vector3.forward : Vector3.up);
        var s = new UlSiatka();
        for (int i = 0; i < ile; i++)
        {
            float a = ile == 1 ? 0f : Mathf.Lerp(-rozp * .5f, rozp * .5f, i / (float)(ile - 1));
            var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
            foreach (var bok in new[] { Vector3.Cross(Vector3.up, d).normalized, Vector3.up })
            {
                int k0 = s.Wierzcholki;
                s.Quad(-bok * w0, bok * w0, d * dl + bok * w1, d * dl - bok * w1, Vector3.Cross(bok, d));
                s.uv[k0] = new Vector2(0, 0); s.uv[k0 + 1] = new Vector2(1, 0); s.uv[k0 + 2] = new Vector2(1, 1); s.uv[k0 + 3] = new Vector2(0, 1);
            }
        }
        var o = BraxMiastoA.Obiekt(g, nazwa + " (siatka)", s, m); o.transform.localPosition = Vector3.zero; o.transform.localRotation = Quaternion.identity;
        return g;
    }

    // ================= ŚWIATŁA =================
    static void Swiatla()
    {
        // 6 kolorowych świateł nad parkietem (barwa w rytm — BraxNovaEfekty), ciepłe nad barem i lożami, przedsionek
        for (int i = 0; i < 6; i++)
        {
            var p = new Vector3(Mathf.Lerp(DX0 + 1.5f, DX1 - 1.5f, (i % 3) / 2f), 3.6f, i < 3 ? DZ0 + 2f : DZ1 - 2f);
            var l = BraxRegion.Warm(root, "NOVA światło parkietu " + (i + 1), p, 1.6f, 9f); l.color = new Color(1f, .2f, .8f); swiatla.Add(l);
        }
        BraxRegion.Warm(root, "NOVA światło baru", new Vector3(49.6f, 3.4f, 47f), 1.3f, 7f);
        BraxRegion.Warm(root, "NOVA światło baru", new Vector3(49.6f, 3.4f, 53f), 1.3f, 7f);
        BraxRegion.Warm(root, "NOVA światło lóż", new Vector3(34.5f, 3.2f, 47.5f), .9f, 7f);
        BraxRegion.Warm(root, "NOVA światło lóż", new Vector3(34.5f, 3.2f, 56.5f), .9f, 7f);
        BraxRegion.Warm(root, "NOVA światło przedsionka", new Vector3(41.6f, 3.6f, 39f), 1.2f, 7f);
        BraxRegion.Warm(root, "NOVA światło DJ", new Vector3(41f, 3.6f, 58.5f), 1.0f, 6f);
    }
}
