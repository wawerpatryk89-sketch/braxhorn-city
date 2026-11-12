using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// DWORZEC AUTOBUSOWO-KOLEJOWY MIASTA R1 + TORY + LAS MIĘDZY MIASTEM A FAWELĄ (MIASTO R1, 11.11.2026).
// Właściciel 14:49: „plan, jak oddzielić fawelę od miasta… jakimś lasem… odsunąć trochę… Parka nie chcę, las jakiś. Ale drzewa też,
// żeby były inne”; 15:32–15:35: „przedzielimy to torami i dworcem i lasem”, „zrób swój dworzec… jakoś taki jak w Mieście Mistrzów”,
// „rozdziel wtedy te fawele od miasta dworcem, torami i lasem. Od strony fawel jeszcze lekko”, „Bez lokomotywy, bez wagonu”,
// „Możesz zrobić mniejszy”; 15:37: „zrób inny trochę”; 15:53–15:55: tylko Poly Haven, meble z drewna obite, zaokrąglone;
// 16:08: każdy szyld 3D i zamocowany; 16:13: tablice informacji i WC niebieskie z białym napisem; 16:19–16:21: „skala, skala
// i skala”, autobusy muszą wjechać, stanąć (min. jedno stanowisko) i wyjechać; 16:28: „przygotuj w kodzie pod wysyłkę”.
// Wymiary pociągu (MIASTO MISTRZÓW przez właściciela, 16:32, skala gry K = 1.2944): skład 126.98 m, rozstaw szyn 1.857 m (osie
// szyn ±0.975), przewód jezdny 6.860 m nad główką szyny, krawędź peronu 0.984 m nad główką i 2.233 m od osi toru, osie torów
// co 5.825 m (perony boczne), wagon 3.728 m, lokomotywa 3.883 m.
// UKŁAD (świat, x na wschód, z na północ): miasto do z −68 (wschód od x −61.4) / Z0 (zachód); fawela odsunięta o
// BraxRegionFawela.Odsuniecie (115 m) — między nimi: las gęsty od miasta, ul. Dworcowa (z Alei Południowej, z = −75.5, dwa
// łuki R 14 do z −103.53), zatoki BUS (2 stanowiska po 18.5 m) i TAXI/K+R, pętla autobusowa (R 16.8 / wyspa 6.0), plac,
// budynek (hala z wieżą zegarową, skrzydła KOLEJ i AUTOBUSY), peron 1 przy budynku, tory 1 i 2, peron 2, kładka do faweli,
// las lekki od faweli. Tory: na zachód do portalu tunelu (x −440, wzgórze w pasie), na wschód kozły (x 65.2). Przejazdy
// kolejowe: łącznik do faweli (x −96) i ul. C0 (x −300). Wzgórze nad tunelem na osiedle (garb z murami oporowymi) usunięte
// 12.11 razem z tunelem (właściciel: „ten tunel trzeba wyjebać, nawet drzewa w nim rosną”) — za końcem Alei Płd. las.
// POZIOMY: grunt YC 0.15, jezdnia 0.17 (2 cm nad gruntem), chodnik/plac/peron/hala 0.32 (krawężnik 15 cm); główka szyny
// 0.32 − 0.984 = −0.664 (tory w płytkim przekopie — z placu na peron 1 bez schodów).
// MATERIAŁY: wyłącznie fotoskany Poly Haven (fetch_region.py — TEXTURES_DWORZEC); bez koloru gładkiego poza źródłami światła
// i szkłem. Drzewa: BraxMiastoDworzecLas (gatunki Poly Haven z unity-tools/claude/drzewa_las.py).
internal static partial class BraxMiastoDworzec
{
    public static readonly bool Wlaczony = true;
    static float K => BraxClaudeOsiedle.K;

    // ---------------- poziomy
    public const float YC = .15f, YJ = .17f, YP = .32f;

    // ---------------- tory i perony (pociąg MIASTA MISTRZÓW — dane przez właściciela 11.11 16:32)
    public const float SkladDl = 126.98f, RozstawSzyn = 1.857f, SzynaOs = .975f, OsieTorow = 5.825f;
    public const float PeronNadGlowka = .984f, KrawedzOdOsi = 2.233f, SiecNadGlowka = 6.86f, SzerWagonu = 3.728f, SzerLoko = 3.883f;
    public const float T1 = -150.23f, T2 = T1 - OsieTorow;
    public const float GlowkaY = YP - PeronNadGlowka;                 // −0.664
    public const float SzynaH = .223f, PodkladH = .28f, PodkladDl = 3.36f, PodkladSz = .34f, PodkladRozstaw = .78f;   // ×K (0.172 / 0.22 / 2.6 / 0.26 / 0.6 m)
    public const float Formacja = GlowkaY - SzynaH - PodkladH - .35f;  // dno przekopu (podsypka pod podkładem 0.35 m)
    public const float P1Tyl = -140f, P1Kraw = T1 + KrawedzOdOsi, P2Kraw = T2 - KrawedzOdOsi, P2Tyl = -166.3f;
    public const float PX0 = -78f, PX1 = 63f;                         // perony 141 m ≥ skład 126.98 m (+ 14 m zapasu)
    public const float KozlyX = 65.2f, KoniecTorow = 66.5f;
    public const float PortalX = -440f, TunelDl = 40f, TunelH = 7.4f;
    public const float LozeN = -146.5f, LozeS = T2 - 3.73f;          // dno przekopu poza peronami (symetrycznie: 3.73 m od osi)
    public const float Skarpa = 2.5f;                                  // skarpa przekopu 1:1.5 (głębokość ok. 1.7 m)

    // ---------------- drogi (pas ruchu 3.5 m × K = 4.53 m)
    public const float Pas = 4.53f, Kr = .25f;
    public const float DworcowaZ = -75.5f, RL = 14f, DrogaZ = DworcowaZ - 2f * RL - .03f;   // −103.53
    public const float Luk1X = -88f, Luk1Z = DworcowaZ - RL, Luk2X = Luk1X + 2f * RL, Luk2Z = DrogaZ + RL;
    public const float AlejaX1 = -121.5f, LicoAlei = -114f;            // krawędź jezdni Alei Południowej, lico chodnika
    public const float PasAleiZ1 = -67.5f;                             // pas trawnika L8/L9 przejęty pod ul. Dworcową (do z −67.5)
    public const float LacznikX = -96f, LacznikH = 3.5f;               // łącznik do faweli przez przejazd (jezdnia 7.0 m)
    public const float C0X = -300f, C0Z0 = -66.75f;                    // ul. C0 → przejazd → fawela (r2)
    public const float PetlaX = 52f, PetlaZ = DrogaZ, PetlaR = 16.8f, PetlaWyspa = 6f;
    public const float BusX0 = -12f, BusX1 = 33f, TaxiX0 = -46f, TaxiX1 = -16f, ZatokaSz = 4.53f;   // BUS: wjazd 4 + 2 × 18.5 + wyjazd 4 m; TAXI/K+R 30 m; między nimi przejście (x −14)
    public const float StanowiskoDl = 18.5f;                           // autobus 15.5 m + 3 m
    public const float BudX0 = -52.6f, BudX1 = 14.6f, BudZ0 = -124f, BudZ1 = -140f;
    public const float RampaN = -137f, RampaS = LozeS - 9.5f;          // początek / koniec zjazdu do przejazdu (spadek ok. 9 %)

    /// <summary>Stanowiska obsługi (kasy, informacja, kawiarnia) — ludzi stawia krok ludzi miasta B.</summary>
    public static readonly List<(Vector3 p, float yaw, string gdzie)> Obsluga = new List<(Vector3, float, string)>();
    /// <summary>Obszary zajęte (jezdnie, place, perony, tory, budynek) — las ich omija.</summary>
    internal static readonly List<Rect> Zajete = new List<Rect>();
    internal static readonly List<(Vector2 c, float r)> ZajeteKola = new List<(Vector2, float)>();
    /// <summary>Obrysy nawierzchni (wielokąty wypukłe w XZ): płyty, jezdnie, krawężniki, chodniki — teren pod nimi nie powstaje
    /// (zasada 10.11 „zero migotania, zakaz nakładania”: jeden kawałek terenu = jedna powierzchnia).</summary>
    internal static readonly List<Vector2[]> Pokrycie = new List<Vector2[]>();
    internal static void Pokryj(params Vector2[] w) { if (w.Length >= 3) Pokrycie.Add(w); }
    internal static void Pokryj(float x0, float z0, float x1, float z1) => Pokryj(new Vector2(Mathf.Min(x0, x1), Mathf.Min(z0, z1)), new Vector2(Mathf.Max(x0, x1), Mathf.Min(z0, z1)), new Vector2(Mathf.Max(x0, x1), Mathf.Max(z0, z1)), new Vector2(Mathf.Min(x0, x1), Mathf.Max(z0, z1)));
    static bool WWielokacie(Vector2[] w, Vector2 p)
    {
        // wypukły, dowolna orientacja: punkt po tej samej stronie wszystkich krawędzi (z tolerancją 1 mm)
        int plus = 0, minus = 0;
        for (int i = 0; i < w.Length; i++)
        {
            Vector2 a = w[i], b = w[(i + 1) % w.Length];
            float c = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x), l = (b - a).magnitude;
            if (l < 1e-5f) continue;
            if (c / l > .001f) plus++; else if (c / l < -.001f) minus++;
        }
        return plus == 0 || minus == 0;
    }
    static Dictionary<(int, int), List<Vector2[]>> pokrycieKub;
    static bool Pokryte(Vector2 p)
    {
        if (pokrycieKub == null)
        {
            pokrycieKub = new Dictionary<(int, int), List<Vector2[]>>();
            foreach (var w in Pokrycie)
            {
                float x0 = w.Min(q => q.x), x1 = w.Max(q => q.x), z0 = w.Min(q => q.y), z1 = w.Max(q => q.y);
                for (int i = Mathf.FloorToInt(x0 / 8f); i <= Mathf.FloorToInt(x1 / 8f); i++)
                    for (int j = Mathf.FloorToInt(z0 / 8f); j <= Mathf.FloorToInt(z1 / 8f); j++)
                    { if (!pokrycieKub.TryGetValue((i, j), out var l)) pokrycieKub[(i, j)] = l = new List<Vector2[]>(); l.Add(w); }
            }
        }
        if (!pokrycieKub.TryGetValue((Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.y / 8f)), out var k)) return false;
        foreach (var w in k) if (WWielokacie(w, p)) return true;
        return false;
    }
    /// <summary>Komórka terenu w całości pod nawierzchnią (9 próbek: narożniki 1 cm do środka, środki krawędzi, środek).</summary>
    static bool KomorkaPokryta(float x0, float z0, float x1, float z1)
    {
        for (int a = 0; a <= 2; a++) for (int b = 0; b <= 2; b++)
        {
            float x = Mathf.Lerp(x0 + .01f, x1 - .01f, a * .5f), z = Mathf.Lerp(z0 + .01f, z1 - .01f, b * .5f);
            if (!Pokryte(new Vector2(x, z))) return false;
        }
        return true;
    }

    internal static Transform root;
    static readonly List<string> opis = new List<string>();

    // ============================================================================ TEREN PASA (świat)
    static float S01(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }

    // 12.11: wzgórze nad tunelem na osiedle (Garb, mury oporowe) usunięte razem z tunelem (właściciel 12:54) — w tym miejscu las.

    /// <summary>Wzgórze zachodniego portalu tunelu kolejowego (x ≤ PortalX − 2); dalej teren horyzontu przedłuża je w góry.</summary>
    public static float WzgorzePortalu(float x, float z)
    {
        if (x > PortalX - 2f) return 0f;
        float h = 10.85f * S01(-98f, -120f, z) * S01(-198f, -176f, z);
        return YC + h;
    }

    /// <summary>Profil jezdni przejazdu (łącznik do faweli, ul. C0): poziom jezdni → w dół do główki szyny → płasko → w górę.</summary>
    public static float ProfilPrzejazdu(float z)
    {
        if (z >= RampaN) return YJ;
        if (z > LozeN) return Mathf.Lerp(GlowkaY, YJ, (z - LozeN) / (RampaN - LozeN));
        if (z >= LozeS) return GlowkaY;
        if (z > RampaS) return Mathf.Lerp(YJ, GlowkaY, (z - RampaS) / (LozeS - RampaS));
        return YJ;
    }

    static float Przekop(float x, float z)
    {
        if (x < PortalX || x > KoniecTorow + 1f) return 1e9f;
        bool stacja = x >= PX0 && x <= PX1;
        float n = stacja ? P1Kraw : LozeN, s = stacja ? P2Kraw : LozeS;
        if (x > PX1 && x <= KoniecTorow + 1f) { n = P1Kraw; s = P2Kraw; }   // za peronami do kozłów — mur końcowy, bez skarp
        if (z <= n && z >= s) return Formacja;
        if (stacja || x > PX1) return 1e9f;
        float d = z > n ? z - n : s - z;
        // UWAGA (12.11): poza skarpą zwraca YC, a WysKorytarza bierze minimum — każde wzniesienie pasa poza przekopem (x PortalX…
        // KoniecTorow) byłoby spłaszczone do YC (tak zniknął garb nad dawnym tunelem osiedla). Nowe wzniesienie tu = najpierw
        // zwrócić 1e9 dla d ≥ Skarpa (i sprawdzić grunt pod jezdniami: dziś grunt pod łukami ul. Dworcowej leży dokładnie 2 cm niżej).
        return Formacja + (YC - Formacja) * Mathf.Clamp01(d / Skarpa);
    }

    /// <summary>Wysokość terenu w pasie dworca (od linii miasta do krawędzi faweli). Nawierzchnie (jezdnie 0.17, place 0.32)
    /// leżą nad nim; przekop torów, zjazdy przejazdów, wzgórze portalu zachodniego w nim (garb nad tunelem osiedla usunięty 12.11).</summary>
    public static float WysKorytarza(float x, float z)
    {
        float h = YC;
        float w = WzgorzePortalu(x, z); if (w > h) h = w;
        float p = Przekop(x, z); if (p < h) h = p;
        foreach (float xc in new[] { LacznikX, C0X })
            if (Mathf.Abs(x - xc) <= LacznikH + .6f && z < RampaN + .5f && z > RampaS - .5f) h = Mathf.Min(h, ProfilPrzejazdu(z) - .06f);
        // krawędź faweli: płynnie w jej teren (wysokość jej górnej krawędzi w danym x)
        float zk = BraxRegionFawela.KrawedzSwiat(x);
        if (z < zk + 12f)
        {
            float hk = BraxRegionFawela.WysTerenu(x, BraxRegionFawela.PlanKrawedz(x));
            h = Mathf.Lerp(h, hk, S01(zk + 12f, zk, z));
        }
        return h;
    }

    // ============================================================================ BUDOWA
    public static void Apply(Transform cityRoot)
    {
        if (!Wlaczony || !BraxRegionFawela.Wlaczona) return;
        opis.Clear(); Obsluga.Clear(); Zajete.Clear(); ZajeteKola.Clear(); Pokrycie.Clear(); pokrycieKub = null; Farby.Clear();
        root = BraxRegion.Root(cityRoot, "CLAUDE dworzec R1: las, dworzec autobusowo-kolejowy, tory");
        BraxUliceKit.Start();
        Materialy();
        Krok("drogi: ul. Dworcowa, zatoki, pętla, łączniki, przejazdy", Drogi);
        Krok("plac i chodniki", Plac);
        Krok("tory, podkłady, podsypka, kozły, portal zachodni", Tory);
        Krok("perony 1 i 2 (krawędzie wg pociągu)", Perony);
        Krok("teren pasa: las, przekop, wzgórza, mury oporowe", Teren);   // po drogach i peronach — polany wg obszarów zajętych
        Krok("sieć trakcyjna", Siec);
        Krok("budynek dworca: hala, wieża zegarowa, skrzydła", Budynek);
        Krok("wnętrze hali: kasy, informacja, kawiarnia, WC, poczekalnia", Wnetrze);
        Krok("wiaty peronów i wiata BUS", Wiaty);
        Krok("kładka nad torami do faweli", Kladka);
        Krok("wyposażenie: ławki, kosze, latarnie, tablice, biletomaty", Wyposazenie);
        Krok("zapis siatek dworca", Zapisz);
        Krok("las: gęsty od miasta, lekki od faweli", Las);
        Krok("kontrola skali (autobus, pociąg, przejścia)", KontrolaSkali);
        Debug.Log("BRAXHORN_DWORZEC_R1 " + string.Join(" | ", opis));
    }

    static void Krok(string n, Action a)
    {
        try { a(); BraxRegion.Note("dworzec: " + n + ": OK"); }
        catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_DWORZEC_R1 " + n + ": " + e); opis.Add(n + ": BŁĄD " + e.Message); }
    }

    internal static void Opis(string s) => opis.Add(s);

    // ============================================================================ MATERIAŁY (Poly Haven)
    internal static Material kamien, granat, niebieski, czarnyAlu, grafit, stal, drewno, dab, posadzka, plyty, granitC, chodnik, asfalt, farba, farbaZ,
        zwir, podklad, szyna, dotyk, skora, beton, betonMur, trawa, sciolka, dach, marmurBlat, szklo, led, litery, tarcza, ekran, czerwony, kamienMur, zielenWyspy;

    static Material T(string n, float metry, Color tint, float rough, float metal, params string[] slugi)
    {
        var m = BraxUliceKit.Tex("Dworzec: " + n, metry, tint, tint, rough, slugi);
        if (m && metal > 0f && m.HasProperty("_Metallic")) { m.SetFloat("_Metallic", metal); EditorUtility.SetDirty(m); }
        if (m && m.HasProperty("_Roughness")) { m.SetFloat("_Roughness", rough); EditorUtility.SetDirty(m); }
        return m;
    }

    static void Materialy()
    {
        var b = Color.white;
        kamien = T("kamień elewacji (marmur jasny)", 2.4f, new Color(1f, .97f, .93f), .85f, 0f, "marble_01", "sandstone_blocks_08");
        kamienMur = T("mur oporowy (bloki piaskowca)", 2.6f, new Color(.92f, .9f, .86f), .9f, 0f, "sandstone_blocks_08", "concrete_wall_008");
        granat = T("blacha granatowa (attyki, wiaty)", 2.4f, new Color(.56f, .63f, .77f), .75f, .5f, "blue_metal_plate");
        niebieski = T("tablice informacyjne — niebieskie", 1.6f, new Color(.62f, .92f, 1.6f), .55f, .25f, "blue_metal_plate");
        // czarne okucia: malowana blacha stalowa (Poly Haven blue_metal_plate w czerni) — gładka, bez ryflowania (metal_plate = blacha ryflowana)
        czarnyAlu = T("okucia szkła — czarne aluminium anodowane", 1.2f, new Color(.075f, .075f, .085f), .45f, .6f, "blue_metal_plate", "metal_plate");
        grafit = T("konstrukcja stalowa — grafit", 1.6f, new Color(.2f, .22f, .25f), .65f, .6f, "blue_metal_plate", "metal_plate");
        stal = T("stal szczotkowana", 1.2f, new Color(.81f, .83f, .84f), .35f, .95f, "corrugated_iron", "metal_plate_02");   // jasna stal ocynkowana (metal_plate_02 = rdzawe arkusze)
        drewno = T("lamele teak", 1.4f, b, .75f, 0f, "teak_veneer");
        dab = T("okleina dębowa", 1.2f, b, .6f, 0f, "oak_veneer_01", "teak_veneer");
        posadzka = T("posadzka hali (granit polerowany)", 2.4f, new Color(.95f, .94f, .92f), .45f, 0f, "granite_tile_04", "granite_tile");
        plyty = T("płyty placu (granit)", 2.4f, b, .85f, 0f, "granite_tile_04", "granite_tile");
        granitC = T("granit ciemny (krawężniki, pasy)", 1.2f, b, .6f, 0f, "granite_tile", "granite_tile_02");
        chodnik = T("chodnik (płyty betonowe)", 2.0f, b, 1f, 0f, "concrete_pavement_02", "concrete_floor_02");
        asfalt = T("asfalt", 3.0f, b, 1f, 0f, "clean_asphalt", "asphalt_02", "asphalt_03");
        farba = T("farba drogowa biała", 1.5f, new Color(.96f, .96f, .94f), .9f, 0f, "concrete_floor_01", "concrete_floor_02");
        farbaZ = T("farba drogowa żółta", 1.5f, new Color(.95f, .76f, .07f), .9f, 0f, "concrete_floor_01", "concrete_floor_02");
        zwir = T("podsypka (tłuczeń)", 1.2f, new Color(.86f, .84f, .8f), 1f, 0f, "gravel_stones", "rocky_gravel");
        podklad = T("podkłady betonowe", 1.0f, new Color(.74f, .72f, .69f), 1f, 0f, "concrete_floor_01", "concrete_floor_02");
        szyna = T("szyny (stal walcowana)", .8f, new Color(.6f, .56f, .53f), .5f, .8f, "metal_plate_02", "metal_plate");
        dotyk = T("pas ostrzegawczy (płytki z wypustkami)", .6f, new Color(.96f, .9f, .7f), .9f, 0f, "anti_skid_tiles", "concrete_floor_01");
        skora = T("skóra obić (brązowa)", .8f, b, .55f, 0f, "brown_leather", "fabric_leather_02");
        beton = T("beton architektoniczny", 2.0f, b, .95f, 0f, "concrete_floor_01", "concrete_wall_008");
        betonMur = T("beton murów i peronów", 2.2f, new Color(.9f, .89f, .87f), .95f, 0f, "concrete_wall_008", "concrete_floor_01");
        trawa = T("trawa (pas lasu, skarpy)", 2.5f, b, 1f, 0f, "leafy_grass", "aerial_grass_rock", "grass004");
        sciolka = T("ściółka lasu (liście, igły)", 2.6f, new Color(.92f, .9f, .86f), 1f, 0f, "brown_mud_leaves_01", "forest_leaves_02");
        dach = T("pokrycie dachu (blacha na rąbek)", 1.6f, new Color(.38f, .41f, .45f), .6f, .6f, "corrugated_iron", "metal_plate");
        marmurBlat = T("blat marmurowy", 1.4f, b, .25f, 0f, "marble_01");
        czerwony = T("blacha czerwona (kozły, znaki)", 1.2f, new Color(.78f, .14f, .1f), .6f, .3f, "metal_plate", "metal_plate_02");
        zielenWyspy = trawa;
        szklo = BraxUliceKit.SzkloPrzezroczyste;
        led = BraxMiastoA.Swieci("Dworzec: oprawa LED (ciepła)", new Color(1f, .97f, .9f), new Color(2.2f, 2.05f, 1.8f));
        litery = BraxMiastoA.Swieci("Dworzec: litery białe (podświetlane)", new Color(.96f, .95f, .92f), new Color(.55f, .54f, .5f));
        tarcza = BraxMiastoA.Swieci("Dworzec: tarcza zegara (mleczna)", new Color(.97f, .96f, .93f), new Color(.35f, .35f, .33f));
        ekran = BraxMiastoA.Swieci("Dworzec: ekran (ciemny granat)", new Color(.03f, .06f, .12f), new Color(.08f, .2f, .45f));
        MaterialyWindWC();
    }

    // ============================================================================ NARZĘDZIA SIATKI
    internal static void Grupa(string n, float zasieg = 0f) => BraxUliceKit.UstawGrupe("Dworzec: " + n, zasieg);
    internal static UlSiatka S(Material m) => BraxUliceKit.S(m);
    internal static void B(Material m, float x0, float y0, float z0, float x1, float y1, float z1, int pomin = 0) => S(m).Box(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), pomin);
    /// <summary>Płyta nawierzchni (wierzch yTop, grubość gr, bez spodu) — boki zakrywają styk z gruntem.</summary>
    internal static void Plyta(Material m, float x0, float z0, float x1, float z1, float yTop, float gr = .3f)
    {
        if (Mathf.Abs(x1 - x0) < .01f || Mathf.Abs(z1 - z0) < .01f) return;
        Pokryj(x0, z0, x1, z1);
        S(m).Box(new Vector3(Mathf.Min(x0, x1), yTop - gr, Mathf.Min(z0, z1)), new Vector3(Mathf.Max(x0, x1), yTop, Mathf.Max(z0, z1)), 4);
    }
    /// <summary>Prostokąt minus otwory (prostokąty) — kawałki bez nakładania (płyty peronu wokół szybów wind, posadzka skrzydła wokół WC).</summary>
    internal static List<Rect> BezOtworow(Rect r, IList<Rect> otwory)
    {
        var l = new List<Rect> { r };
        foreach (var o in otwory)
        {
            var n = new List<Rect>();
            foreach (var q in l)
            {
                if (!q.Overlaps(o)) { n.Add(q); continue; }
                float x0 = Mathf.Max(q.xMin, o.xMin), x1 = Mathf.Min(q.xMax, o.xMax), z0 = Mathf.Max(q.yMin, o.yMin), z1 = Mathf.Min(q.yMax, o.yMax);
                if (q.xMin < x0 - .001f) n.Add(Rect.MinMaxRect(q.xMin, q.yMin, x0, q.yMax));
                if (x1 < q.xMax - .001f) n.Add(Rect.MinMaxRect(x1, q.yMin, q.xMax, q.yMax));
                if (q.yMin < z0 - .001f) n.Add(Rect.MinMaxRect(x0, q.yMin, x1, z0));
                if (z1 < q.yMax - .001f) n.Add(Rect.MinMaxRect(x0, z1, x1, q.yMax));
            }
            l = n;
        }
        return l;
    }
    internal static void PlytaBez(Material m, float x0, float z0, float x1, float z1, float yTop, float gr, IList<Rect> otwory)
    {
        foreach (var r in BezOtworow(Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1)), otwory)) Plyta(m, r.xMin, r.yMin, r.xMax, r.yMax, yTop, gr);
    }
    internal static void Zajmij(float x0, float z0, float x1, float z1) => Zajete.Add(Rect.MinMaxRect(Mathf.Min(x0, x1), Mathf.Min(z0, z1), Mathf.Max(x0, x1), Mathf.Max(z0, z1)));
    internal static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    internal static GameObject Kol(string n, Vector3 a, Vector3 b) => BraxMiastoA.Kol(root, "Dworzec: " + n, a, b);

    // ============================================================================ TEREN
    static void Teren()
    {
        Grupa("teren");
        var xsSpec = new List<float> { PortalX - 2f, PortalX, PX0, PX1, KoniecTorow, KoniecTorow + 1f, -61.4f };
        foreach (float xc in new[] { LacznikX, C0X }) { xsSpec.Add(xc - LacznikH - .6f); xsSpec.Add(xc + LacznikH + .6f); xsSpec.Add(xc - LacznikH); xsSpec.Add(xc + LacznikH); }
        var zsSpec = new List<float> { LozeN + Skarpa, LozeS, LozeS - Skarpa, P1Kraw, P2Kraw, P1Tyl, P2Tyl, RampaN, RampaS, T1 + 3.73f, T2 - 3.73f };
        // krawędzie prostych nawierzchni (komórki pod nimi w całości pokryte — teren tam nie powstaje)
        foreach (float zc in new[] { DworcowaZ, DrogaZ }) foreach (float d in new[] { Pas, Pas + Kr, Pas + Kr + 3f }) { zsSpec.Add(zc + d); zsSpec.Add(zc - d); }
        zsSpec.AddRange(new[] { DrogaZ - Pas - ZatokaSz, DrogaZ - Pas - ZatokaSz - Kr, BudZ0, BudZ1, PetlaZ + PetlaR + Kr, PetlaZ + PetlaR + Kr + 3f, PetlaZ - PetlaR - Kr });
        xsSpec.AddRange(new[] { LicoAlei, Luk1X, Luk2X, BudX0, BudX1, PetlaX - PetlaR - Kr, PetlaX - PetlaR - Kr - 3f, PetlaX + PetlaR + Kr, LacznikX - LacznikH - Kr, LacznikX + LacznikH + Kr, LacznikX + LacznikH + Kr + 2.5f });
        Pokryj(BudX0, BudZ1, BudX1, BudZ0);                                   // budynek (posadzki budowane później)
        pokrycieKub = null;
        int trojkatow = 0; pominieteKomorki = 0;
        // zachód (x FawelaX0…−61.4, od Z0 do krawędzi faweli), wschód (x −61.4…FawelaX1, od −68), pas przy Alei (L8/L9 → ul. Dworcowa)
        foreach (var (x0, x1, zTop, zBot, nazwa) in new[]
        {
            (BraxMiastoGranice.FawelaX0, -61.4f, BraxMiastoGranice.Z0, BraxRegionFawela.KrawedzSwiat(-100f), "zachód"),
            (-61.4f, BraxMiastoGranice.FawelaX1, -68f, BraxRegionFawela.KrawedzSwiat(0f), "wschód"),
            (LicoAlei, -61.4f, PasAleiZ1, BraxMiastoGranice.Z0, "pas przy Alei Południowej"),
        })
        {
            var xs = Siatka(x0, x1, 2f, xsSpec); var zs = Siatka(zBot, zTop, 2f, zsSpec);
            trojkatow += Pole(nazwa, xs, zs);
        }
        Opis("teren pasa: trójkątów " + trojkatow + ", komórek pod nawierzchnią pominiętych " + pominieteKomorki + ", odsunięcie faweli " + BraxRegionFawela.Odsuniecie.ToString("F0") + " m");
    }

    static int pominieteKomorki;
    static List<float> Siatka(float a, float b, float krok, List<float> spec)
    {
        var l = new List<float>(); for (float v = a; v < b - .3f; v += krok) l.Add(v); l.Add(b);
        foreach (var s in spec) if (s > a + .05f && s < b - .05f) l.Add(s);
        l.Sort(); var w = new List<float>();
        foreach (var v in l) if (w.Count == 0 || v - w[w.Count - 1] > .05f) w.Add(v);
        return w;
    }

    // pole terenu: wspólne wierzchołki, dwie partie (trawa / ściółka lasu / tłuczeń w przekopie), kolizja siatki
    static int Pole(string nazwa, List<float> xs, List<float> zs)
    {
        int nx = xs.Count, nz = zs.Count, pominiete = 0;
        var v = new Vector3[nx * nz];
        for (int i = 0; i < nx; i++) for (int j = 0; j < nz; j++) v[i * nz + j] = new Vector3(xs[i], WysKorytarza(xs[i], zs[j]), zs[j]);
        var tri = new Dictionary<Material, List<int>> { [trawa] = new List<int>(), [sciolka] = new List<int>(), [zwir] = new List<int>() };
        for (int i = 0; i + 1 < nx; i++)
            for (int j = 0; j + 1 < nz; j++)
            {
                float cx = (xs[i] + xs[i + 1]) * .5f, cz = (zs[j] + zs[j + 1]) * .5f;
                // otwór portalu zachodniego (grubość muru czołowego) — widać wnętrze tunelu
                if (cx > PortalX - 2f && cx < PortalX && cz < T1 + 3.6f && cz > T2 - 3.6f) continue;
                if (KomorkaPokryta(xs[i], zs[j], xs[i + 1], zs[j + 1])) { pominiete++; continue; }
                int a = i * nz + j, b2 = (i + 1) * nz + j, c = (i + 1) * nz + j + 1, d = i * nz + j + 1;
                float hc = (v[a].y + v[b2].y + v[c].y + v[d].y) * .25f;
                var m = hc < Formacja + .2f ? zwir : (GestoscLasu(cx, cz) > .62f ? sciolka : trawa);
                // z = na północ: (x,z) → (x+1,z) → (x+1,z+1) — normalna w górę przy kolejności a, d, c / a, c, b
                tri[m].AddRange(new[] { a, d, c, a, c, b2 });
            }
        int n = 0;
        foreach (var kv in tri)
        {
            if (kv.Value.Count == 0) continue;
            var mesh = new Mesh { name = "Dworzec: teren " + nazwa + " — " + kv.Key.name, indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = v; mesh.uv = v.Select(p => new Vector2(p.x, p.z)).ToArray();
            mesh.SetTriangles(kv.Value, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var g = BraxMiastoA.Obiekt(root, "Dworzec: teren " + nazwa + " (" + kv.Key.name + ")", mesh, kv.Key);
            var r = g.GetComponent<MeshRenderer>(); r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = true;
            g.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            n += kv.Value.Count / 3;
        }
        pominieteKomorki += pominiete;
        return n;
    }

    // ============================================================================ ZAPIS
    static void Zapisz()
    {
        int n = BraxUliceKit.Zapisz(root, "dworzec_r1");
        // nawierzchnie i perony: kolizja siatki (gracz chodzi po płytach, nie po gruncie pod nimi)
        int kol = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.parent == null || !t.parent.name.StartsWith("Dworzec: nawierzchnie", StringComparison.Ordinal)) continue;
            var mf = t.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh || t.GetComponent<Collider>()) continue;
            t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh; kol++;
        }
        Opis("siatek " + n + ", kolizji nawierzchni " + kol);
    }
}
