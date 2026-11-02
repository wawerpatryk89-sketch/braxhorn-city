using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — TEREN DWÓCH WIEŻOWCÓW ZA SIŁOWNIĄ OD NOWA (wykonawca B, 2.11; zastępuje krok BraxRegionBackyard).
// Zgłoszenia 13.1 (05e9cc02, 9d6c27ac, 5fa5818d, e12d9bc2, 2ee299c1, 3df692ae, 55820270, z47): trawa-szum i „rysowanie się”
// trawnika, ławki zdeformowane i krzesła-klocki, kosze porozrzucane, posadzka-lustro, wejście = rama drzwi przed pustym gankiem,
// wieżowce płaskie z plamami, koniec placu od morza bez nabrzeża (pas lustra). Właściciel 15:10: „do budynku w ogóle nie można
// wejść… zrób parę pięter użytkowych, winda z przyciskami — dostępne tylko piętra użytkowe — w każdym wieżowcu”.
// ZDJĘTE (zasada „starą zwalić, nie nakładać”): stare bryły „High-rise south skyline” (A) i „High-rise south west skyline” (B)
// razem z ich windami, cały dawny korzeń tyłów (plac, kwatery, ławki, wiadukt), oryginał w strefie (tylko grunt, y < 3).
// POŁOŻONE OD NOWA: plac z płyt granitowych (szorstkość ≥ 0.8 — bez lustra), kwatery z murkiem (trawnik miasta w kwaterze,
// drzewa, ławki × k z BraxSeat), latarnie, kosze, park za wieżowcami, nabrzeże od morza (mur, kamienna czapa, barierka,
// pachołki), dwa wieżowce z elewacją szklaną (lizeny, gzymsy stropowe, korona, antena), okna jasne („pokój za szybą”),
// na piętrach użytkowych szyby przezroczyste i prawdziwe wnętrza; wejście: drzwi automatyczne przesuwne bez progu,
// lobby (recepcja, posadzka kamienna, kanapy, rośliny, tablica pięter), winda BraxWinda (panel wszystkich pięter, aktywne
// tylko użytkowe), piętra użytkowe A: biura (3), zarząd + SEJF + ochrona (6), konferencja z widokiem (11);
// B: kawiarnia w holu (0), klub fitness (2), biura (5). Każde piętro: korytarz, WC, klatka schodowa (wyjście ewakuacyjne).
public static class BraxRegionTylyB
{
    static float K => BraxClaudeOsiedle.K;
    const float YC = .15f;
    static Transform root;
    static readonly List<string> usuniete = new List<string>();
    static Material granit, murek, beton, alu, szklo, szkloCiemne, panel, sufit, tynk, wykladzina, kamienPos, drewno, stalNierdz, czarny, ledM, przyciskOn, przyciskSel, przyciskOff, zielonyZnak;
    static BraxCityWorld swiat;
    static int ileLawek, ileDrzew, ilePieter, ileWnetrz;

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        swiat = world; usuniete.Clear(); punkty.Clear(); ileLawek = ileDrzew = ilePieter = ileWnetrz = 0;
        root = BraxRegion.Root(cityRoot, "CLAUDE tyły B: plac wieżowców, park, nabrzeże, wieżowce A i B");
        Krok("usunięcie starego (bryły wieżowców, dawne tyły, oryginał w strefie)", () => UsunStare(cityRoot));
        Krok("materiały", Materialy);
        // kolejność jak na budowie (właściciel 16:04): nawierzchnie i mury → budynki → wyposażenie placu na gotowym podłożu
        pozniej.Clear();
        Krok("1. nawierzchnie: plac, park, kwatery, mur nabrzeża", Plac);
        Krok("2. budynki: wieżowiec A (BRAXHORN OFFICE)", () => Wiezowiec(A()));
        Krok("2. budynki: wieżowiec B (PARK TOWER)", () => Wiezowiec(B()));
        Krok("3. wyposażenie placu: drzewa, ławki, kosze, latarnie, barierka, pachołki", () => { Physics.SyncTransforms(); foreach (var a in pozniej) a(); pozniej.Clear(); });
        Krok("punkty świata (wejścia, piętra, sejf, trasa ochrony)", ZapiszPunkty);
        Debug.Log("BRAXHORN_MIASTO_B_TYLY ławek=" + ileLawek + " drzew=" + ileDrzew + " pięter użytkowych=" + ilePieter + " wnętrz=" + ileWnetrz +
                  " posadzki: granit szorstkość ≥ 0.8 (lustra=0) usunięte=" + usuniete.Count);
        Debug.Log("BRAXHORN_MIASTO_B_USUNIETE tyły: " + string.Join(" | ", usuniete.Take(40)));
    }

    static void Krok(string n, Action a)
    {
        try { a(); BraxRegion.Note("tyły B: " + n + ": OK"); }
        catch (Exception e) { BraxRegion.Note("tyły B: " + n + ": BŁĄD " + e.Message); Debug.LogError("BRAXHORN_MIASTO_B tyły " + n + ": " + e); }
    }
    static Bounds Bx(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }

    // =====================================================================================================================
    static void UsunStare(Transform cityRoot)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
        {
            if (!t) continue;
            if (t.name == "High-rise south skyline" || t.name == "High-rise south west skyline" || t.name == "CLAUDE tyły siłowni: plac, wieżowce, wiadukt")
            {
                usuniete.Add(t.name + " (" + t.GetComponentsInChildren<Renderer>(true).Length + " rendererów, " + t.GetComponentsInChildren<BraxCityElevator>(true).Length + " wind)");
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }
        if (swiat) swiat.additionalTowerElevators = swiat.additionalTowerElevators.Where(e => e).ToArray();
        // oryginał w strefie: tylko grunt (y < 3), nie budynki pierzei
        var objetosci = new[] { Bx(-61.4f, 58f, -3f, 3f, -68f, -6.3f), Bx(58f, 83.9f, -3f, 3f, -68f, -35.2f) };
        var korzenie = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int n = 0;
        foreach (var v in objetosci)
            foreach (var g in korzenie)
            {
                if (!g || g.transform == root || g.name.StartsWith("braxhorn-original-gym") || g.GetComponent<BraxGymPlayer>() || g.GetComponent<Camera>()) continue;
                if (g.name.StartsWith("CLAUDE miasto B")) continue;
                try { n += BraxCityMeshCut.Subtract(g.transform, v, BraxRegion.Out + "/Cuts", "tylyB"); } catch (Exception e) { usuniete.Add("cięcie " + g.name + ": " + e.Message); }
            }
        usuniete.Add("grunt oryginału w strefie: wycięte siatki " + n);
    }

    static void Materialy()
    {
        granit = BraxRegionMeble.Tex("Tyły B: płyty granitowe placu", 1.2f, new Color(.70f, .69f, .67f), .85f, "granite_tile_02", "concrete_pavers_03", "concrete_pavement_02");
        murek = BraxRegionMeble.Tex("Tyły B: granit murków kwater", 1.2f, new Color(.55f, .55f, .54f), .8f, "granite_tile_02", "rough_block_wall");
        beton = BraxRegionMeble.Tex("Tyły B: beton", 2.0f, new Color(.78f, .77f, .74f), .9f, "concrete_floor_02", "concrete_pavement_02", "concrete_wall_008");
        alu = BraxCityMaterials.Surface("Wieżowiec: aluminium lizen (anodowane)", new Color(.56f, .58f, .60f), .4f, .75f);
        szklo = BraxCityMaterials.Surface("Wieżowiec: szyba przezroczysta", new Color(.70f, .80f, .84f, .22f), .08f, .1f);
        szkloCiemne = BraxCityMaterials.Surface("Wieżowiec: szyba drzwi i witryn", new Color(.60f, .70f, .74f, .30f), .08f, .1f);
        panel = BraxCityMaterials.Surface("Wieżowiec: panel stropowy (szkło emaliowane grafit)", new Color(.16f, .19f, .22f), .35f, .3f);
        sufit = BraxCityMaterials.Surface("Wnętrza B: sufit podwieszany biały", new Color(.92f, .92f, .90f), .9f);
        tynk = BraxRegionMeble.Tex("Wnętrza B: ściany (farba)", 1.5f, new Color(.93f, .92f, .89f), .9f, "painted_plaster_wall", "white_plaster_02", "white_stucco_02");
        wykladzina = BraxCityMaterials.Surface("Wnętrza B: wykładzina grafitowa", new Color(.22f, .23f, .25f), .97f);
        kamienPos = BraxRegionMeble.Tex("Wnętrza B: posadzka kamienna (lobby)", 1.0f, new Color(.86f, .85f, .83f), .55f, "marble_01", "large_grey_tiles", "granite_tile_02");
        drewno = BraxRegionMeble.Tex("Wnętrza B: drewno dąb", 1.2f, new Color(.80f, .66f, .50f), .6f, "wood_floor", "wood_planks");
        stalNierdz = BraxCityMaterials.Surface("Wnętrza B: stal nierdzewna szczotkowana", new Color(.70f, .71f, .72f), .35f, .85f);
        czarny = BraxCityMaterials.Surface("Wnętrza B: czarny mat", new Color(.05f, .05f, .055f), .7f, .2f);
        ledM = BraxRegionMeble.Swiecacy("Wnętrza B: panel LED sufitowy", new Color(.96f, .95f, .90f), .9f);
        przyciskOn = BraxRegionMeble.Swiecacy("Winda: przycisk aktywny (biały)", new Color(.85f, .95f, 1f), 1.2f);
        przyciskSel = BraxRegionMeble.Swiecacy("Winda: przycisk bieżące piętro (bursztyn)", new Color(1f, .62f, .12f), 1.6f);
        przyciskOff = BraxCityMaterials.Surface("Winda: przycisk nieaktywny (szary)", new Color(.30f, .31f, .32f), .5f, .4f);
        zielonyZnak = BraxRegionMeble.Swiecacy("Znak: wyjście ewakuacyjne (zielony)", new Color(.05f, .75f, .25f), .8f);
    }

    // =====================================================================================================================
    // PLAC, PARK, NABRZEŻE — prostokąty bez nakładania (styk krawędzią)
    static void Plyta(string n, Material m, float x0, float x1, float z0, float z1, float top, float grub = .3f, bool kolizja = true)
    {
        if (x1 - x0 < .01f || z1 - z0 < .01f) return;
        var b = new BraxRegionMeble.Bud(); b.MinMax(new Vector3(x0, top - grub, z0), new Vector3(x1, top, z1));
        b.Obiekt(n, root, m, false, false);
        if (kolizja) BraxRegionMeble.Kolizja(root, n + " kolizja", new Vector3((x0 + x1) * .5f, top - grub * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, grub, z1 - z0));
    }

    static readonly List<Action> pozniej = new List<Action>();
    static void Pozniej(Action a) => pozniej.Add(a);
    static readonly Rect[] Kwatery = { Rect.MinMaxRect(-50f, -31f, -30f, -21f), Rect.MinMaxRect(-10f, -32f, 10f, -22f), Rect.MinMaxRect(28f, -31f, 48f, -21f) };

    static void Plac()
    {
        float g = .3f - .003f;
        Plyta("Plac: płyty P1a", granit, -61.45f, 66f, -13.25f, -6f, YC, g);
        Plyta("Plac: płyty P1b", granit, -61.15f, 66f, -24.75f, -13.25f, YC, g);
        Plyta("Plac: płyty P1c", granit, -61.4f, 66f, -35f, -24.75f, YC, g);
        Plyta("Plac: płyty P2 (przed B)", granit, -61.4f, 22f, -36f, -35f, YC, g);
        Plyta("Plac: płyty P3 (zach. od B)", granit, -61.4f, -54f, -56f, -36f, YC, g);
        Plyta("Plac: płyty P4 (między wieżowcami)", granit, -18f, 22f, -56f, -36f, YC, g);
        Plyta("Plac: płyty P5 (do nabrzeża)", granit, 58f, 83.6f, -68f, -35f, YC, g);
        Plyta("Plac: płyty P5b (do nabrzeża)", granit, 66f, 83.6f, -35f, -31f, YC, g);
        Plyta("Plac: płyty P6 (za B)", granit, -54f, -18f, -56f, -52f, YC, g);
        Plyta("Plac: płyty P7 (za A)", granit, 22f, 58f, -56f, -49f, YC, g);
        // park za wieżowcami (do granicy z −68): trawnik miasta (otwór = cały prostokąt, obrzeże betonowe), drzewa, alejka
        BraxRegionTrawa.Trawnik(root, "park za wieżowcami", Rect.MinMaxRect(-61.4f, -68f, 58f, -56f), YC - .03f, null, true, true, false);
        Plyta("Park: podłoże", beton, -61.4f, 58f, -68f, -56f, YC - .06f, .3f, false);
        for (float x = -56f; x < 56f; x += 9f) { float xx = x; Pozniej(() => Drzewo(new Vector3(xx + ((int)xx % 3), YC - .03f, -62.5f + ((int)(xx * 7) % 3)))); }
        // kwatery z murkiem 0.45 m × k (trawnik w kwaterze 5 cm pod koroną murku), po 3 drzewa, ławki przy dłuższych bokach (oparciem do murku)
        float hm = .45f * K, tm = .35f;
        foreach (var q in Kwatery)
        {
            var m = new BraxRegionMeble.Bud();
            m.MinMax(new Vector3(q.xMin, YC, q.yMin), new Vector3(q.xMax, YC + hm, q.yMin + tm));
            m.MinMax(new Vector3(q.xMin, YC, q.yMax - tm), new Vector3(q.xMax, YC + hm, q.yMax));
            m.MinMax(new Vector3(q.xMin, YC, q.yMin + tm), new Vector3(q.xMin + tm, YC + hm, q.yMax - tm));
            m.MinMax(new Vector3(q.xMax - tm, YC, q.yMin + tm), new Vector3(q.xMax, YC + hm, q.yMax - tm));
            m.Obiekt("Kwatera: murek granitowy", root, murek, false, true);
            BraxRegionMeble.Kolizja(root, "Kwatera: kolizja", new Vector3(q.center.x, YC + hm * .5f, q.center.y), new Vector3(q.width, hm, q.height));
            var wn = Rect.MinMaxRect(q.xMin + tm, q.yMin + tm, q.xMax - tm, q.yMax - tm);
            BraxRegionTrawa.Trawnik(root, "kwatera " + q.center.x.ToString("0"), wn, YC + hm - .05f, null, false, true, false);
            Plyta("Kwatera: ziemia", beton, wn.xMin, wn.xMax, wn.yMin, wn.yMax, YC + hm - .09f, hm - .1f, false);
            var qq = q; var wnn = wn;
            Pozniej(() =>
            {
                foreach (float t in new[] { .2f, .5f, .8f }) Drzewo(new Vector3(Mathf.Lerp(wnn.xMin, wnn.xMax, t), YC + hm - .05f, wnn.center.y + (t - .5f) * 1.6f));
                for (float x = qq.xMin + 2.5f; x < qq.xMax - 2f; x += 5f)
                {
                    BraxRegionMeble.Lawka(root, new Vector3(x, YC, qq.yMax + .5f * K), 0f); ileLawek++;
                    BraxRegionMeble.Lawka(root, new Vector3(x, YC, qq.yMin - .5f * K), 180f); ileLawek++;
                }
                BraxRegionMeble.Kosz(root, new Vector3(qq.xMin - .7f, YC, qq.yMax + .6f), 0f);
                BraxRegionMeble.Kosz(root, new Vector3(qq.xMax + .7f, YC, qq.yMin - .6f), 180f);
                BraxRegionMeble.Latarnia(root, new Vector3(qq.xMin - .8f, YC, qq.yMin - .8f));
                BraxRegionMeble.Latarnia(root, new Vector3(qq.xMax + .8f, YC, qq.yMax + .8f));
            });
        }
        // nabrzeże od morza (x 84, z −68…−31): mur z kamienia do wody (y −3), kamienna czapa 0.45 m, barierka × k, pachołki
        var kam = BraxRegionMeble.Tex("Nabrzeże: kamień", 1.2f, new Color(.66f, .65f, .62f), .85f, "rough_block_wall", "granite_tile_02", "concrete_wall_008");
        var mur = new BraxRegionMeble.Bud();
        mur.MinMax(new Vector3(83.6f, -3.2f, -68f), new Vector3(84.0f, YC - .02f, -31f));
        mur.MinMax(new Vector3(83.6f, YC - .02f, -68f), new Vector3(84.08f, YC + .1f, -31f));      // czapa (10 cm nad płytami — nie w ich płaszczyźnie)
        mur.Obiekt("Nabrzeże: mur i czapa", root, kam, false, true);
        BraxRegionMeble.Kolizja(root, "Nabrzeże: kolizja muru", new Vector3(83.8f, -1.5f, -49.5f), new Vector3(.4f, 3.4f, 37f));
        Pozniej(() => WyposazenieNabrzeza());
    }

    static void WyposazenieNabrzeza()
    {
        var bar = new BraxRegionMeble.Bud();
        BraxRegionMeble.Barierka(root, bar, new Vector3(83.82f, YC + .1f, -67.6f), new Vector3(83.82f, YC + .1f, -31.4f));
        bar.Obiekt("Nabrzeże: barierka stalowa", root, BraxRegionMeble.StalSzara, false, true);
        var pach = new BraxRegionMeble.Bud();
        for (float z = -65f; z < -32f; z += 8.5f)
        {
            var p = new Vector3(83.3f, YC, z);
            pach.Cyl(p, p + Vector3.up * .45f * K, .17f * K, 14, .14f * K); pach.Cyl(p + Vector3.up * .45f * K, p + Vector3.up * .52f * K, .22f * K, 14);
            BraxRegionMeble.Kolizja(root, "Pachołek kolizja", p + Vector3.up * .26f * K, new Vector3(.4f * K, .52f * K, .4f * K));
        }
        pach.Obiekt("Nabrzeże: pachołki cumownicze (żeliwo)", root, BraxRegionMeble.Zeliwo, false, true);
        for (float z = -64f; z < -34f; z += 12f) { BraxRegionMeble.Lawka(root, new Vector3(81.8f, YC, z), 90f); ileLawek++; }
        for (float z = -58f; z < -34f; z += 12f) BraxRegionMeble.Latarnia(root, new Vector3(82.6f, YC, z));
    }

    static void Drzewo(Vector3 p)
    {
        var src = BraxRegion.Model("drzewo_verdant"); if (!src) return;
        var o = BraxRegion.Put(root, src, "Plac: drzewo", p, (p.x * 31f + p.z * 17f) % 360f, 1f, false, 10f);
        if (!o) return;
        var c = new GameObject("Plac: pień kolizja").AddComponent<CapsuleCollider>(); c.transform.SetParent(root, false);
        c.transform.position = p + Vector3.up * 1.5f; c.radius = .3f; c.height = 3f;
        BraxRegionTrawa.Drzewo(p); ileDrzew++;
    }

    // =====================================================================================================================
    // WIEŻOWCE
    enum Rodzaj { Lobby, LobbyKawiarnia, Biura, Zarzad, Konferencja, Fitness }
    sealed class Def
    {
        public string nazwa, korona, idDrzwi; public float x0, x1, z0, z1; public int pietra; public Vector2 szyb;
        public Dictionary<int, Rodzaj> uzytkowe = new Dictionary<int, Rodzaj>();
        public Color lizena;
    }
    static float Hg => 5.0f * K;     // parter (hol) 5.0 m
    static float Hf => 3.6f * K;     // kondygnacja biurowa 3.6 m (strop do stropu)
    static float Pas => .55f * K;    // pas stropowy (panel) wokół stropu
    static float Y(int i) => YC + (i == 0 ? 0f : Hg + (i - 1) * Hf);

    static Def A() => new Def
    {
        nazwa = "BRAXHORN OFFICE", korona = "BRAXHORN OFFICE", idDrzwi = "TOWER_A_ENTRY", x0 = 22f, x1 = 58f, z0 = -49f, z1 = -35f, pietra = 11, szyb = new Vector2(40f, -44f),
        uzytkowe = new Dictionary<int, Rodzaj> { { 0, Rodzaj.Lobby }, { 3, Rodzaj.Biura }, { 6, Rodzaj.Zarzad }, { 11, Rodzaj.Konferencja } }, lizena = new Color(.56f, .58f, .60f)
    };
    static Def B() => new Def
    {
        nazwa = "PARK TOWER", korona = "PARK TOWER", idDrzwi = "TOWER_B_ENTRY", x0 = -54f, x1 = -18f, z0 = -52f, z1 = -36f, pietra = 9, szyb = new Vector2(-36f, -46f),
        uzytkowe = new Dictionary<int, Rodzaj> { { 0, Rodzaj.LobbyKawiarnia }, { 2, Rodzaj.Fitness }, { 5, Rodzaj.Biura } }, lizena = new Color(.30f, .24f, .18f)
    };

    // wymiary windy × k: kabina 1.6 × 1.4 m (wnętrze), drzwi 1.1 × 2.1 m
    static float KabW => 1.6f * K; static float KabD => 1.4f * K; static float DrzW => 1.1f * K; static float DrzH => 2.1f * K;

    static void Wiezowiec(Def d)
    {
        var T = new GameObject("Wieżowiec " + d.nazwa).transform; T.SetParent(root, false);
        int n = d.pietra; float top = Y(n) + Hf;   // dach
        var szkloOkna = OknoWiezowca(d);
        var mFas = new Dictionary<Material, BraxRegionMeble.Bud>();
        BraxRegionMeble.Bud M(Material m) { if (!mFas.TryGetValue(m, out var b)) mFas[m] = b = new BraxRegionMeble.Bud(); return b; }
        var lizenaM = BraxCityMaterials.Surface("Wieżowiec: lizeny " + d.nazwa, d.lizena, .4f, .7f);
        float rozstaw = 1.5f * K;   // lizeny co 1.5 m × k
        // ---------------- elewacja: dla każdej strony i kondygnacji pas stropowy + szkło (przezroczyste na piętrach użytkowych)
        for (int s = 0; s < 4; s++)
        {
            bool wzdX = s % 2 == 0; float c = s == 0 ? d.z1 : s == 1 ? d.x1 : s == 2 ? d.z0 : d.x0; int zew = s < 2 ? 1 : -1;
            float a0 = wzdX ? d.x0 : d.z0, a1 = wzdX ? d.x1 : d.z1;
            void Box(Material m, float b0, float b1, float y0, float y1, float n0, float n1)
            {
                // narożniki bez nakładania: elementy w warstwie elewacji (n0 < 0) stron ±x kończą się przed warstwą stron ±z
                if (!wzdX && n0 < -.001f && m != lizenaM) { b0 = Mathf.Max(b0, a0 - n0); b1 = Mathf.Min(b1, a1 + n0); }
                if (b1 - b0 < .003f || y1 - y0 < .003f) return;
                float c0 = c + zew * n0, c1 = c + zew * n1;
                var p0 = wzdX ? new Vector3(b0, y0, Mathf.Min(c0, c1)) : new Vector3(Mathf.Min(c0, c1), y0, b0);
                var p1 = wzdX ? new Vector3(b1, y1, Mathf.Max(c0, c1)) : new Vector3(Mathf.Max(c0, c1), y1, b1);
                M(m).MinMax(p0, p1);
            }
            for (int i = 0; i <= n; i++)
            {
                float y0 = Y(i), y1 = i == 0 ? Y(1) : Y(i) + Hf;
                bool uz = d.uzytkowe.ContainsKey(i);
                // pas stropowy u góry kondygnacji (panel grafit, w licu lizen − 2 cm)
                Box(panel, a0, a1, y1 - Pas, y1, -.3f, -.02f);
                // szkło (w głębi 0.18 m × k od lica lizen): przezroczyste (piętra użytkowe, hol) albo „pokój za szybą”
                float gy0 = i == 0 ? y0 : y0, gy1 = y1 - Pas;
                bool wejscie = i == 0 && s == 0;
                float dw = 2.4f * K;   // otwór drzwi automatycznych (2 skrzydła)
                if (wejscie)
                {
                    float m0 = d.szyb.x - dw * .5f - .1f, m1 = d.szyb.x + dw * .5f + .1f;
                    Box(uz ? szklo : szkloOkna, a0, m0, gy0, gy1, -.18f * K - .02f, -.18f * K);
                    Box(uz ? szklo : szkloOkna, m1, a1, gy0, gy1, -.18f * K - .02f, -.18f * K);
                    Box(szklo, m0, m1, y0 + DrzH + .3f, gy1, -.18f * K - .02f, -.18f * K);   // naświetle nad drzwiami
                    Box(alu, m0 - .08f, m0, y0, gy1, -.18f * K - .1f, -.18f * K + .06f);
                    Box(alu, m1, m1 + .08f, y0, gy1, -.18f * K - .1f, -.18f * K + .06f);
                    Box(alu, m0, m1, y0 + DrzH + .22f, y0 + DrzH + .3f, -.18f * K - .1f, -.18f * K + .06f);
                }
                else Box(uz ? szklo : szkloOkna, a0, a1, gy0, gy1, -.18f * K - .02f, -.18f * K);
                if (uz)
                {
                    // kolizja szkła (gracz nie wychodzi przez szybę) — przy wejściu z przerwą na drzwi
                    if (wejscie)
                    {
                        float m0 = d.szyb.x - dw * .5f - .1f, m1 = d.szyb.x + dw * .5f + .1f;
                        KolizjaSciany(wzdX, c - zew * .2f, a0, m0, y0, gy1); KolizjaSciany(wzdX, c - zew * .2f, m1, a1, y0, gy1);
                    }
                    else KolizjaSciany(wzdX, c - zew * .2f, a0, a1, y0, gy1);
                }
            }
            // lizeny pionowe na całej wysokości (od parteru do korony), narożniki pełne
            for (float a = a0; a <= a1 + .01f; a += rozstaw)
            {
                float aa = Mathf.Min(a, a1);
                bool przyDrzwiach = s == 0 && Mathf.Abs(aa - d.szyb.x) < 2.4f * K * .5f + .3f;
                Box(lizenaM, aa - .06f * K, aa + .06f * K, przyDrzwiach ? Y(1) - Pas : YC, top, -.18f * K, .14f * K);
            }
            Box(lizenaM, a0, a0 + .45f * K, YC, top, -.3f, .2f * K); Box(lizenaM, a1 - .45f * K, a1, YC, top, -.3f, .2f * K);
            // korona: attyka 2.4 m × k z pasem lizen, gzyms wieńczący
            Box(panel, a0, a1, top, top + 2.4f * K, -.3f, 0f);
            Box(alu, a0 - (wzdX ? .3f * K : 0f), a1 + (wzdX ? .3f * K : 0f), top + 2.4f * K, top + 2.55f * K, -.3f, .3f * K);
            // cokół z granitu przy parterze (0.4 m × k) — z przerwą na drzwi wejściowe; w przerwie próg z płyt w poziomie placu
            if (s == 0)
            {
                float m0 = d.szyb.x - 2.4f * K * .5f - .1f, m1 = d.szyb.x + 2.4f * K * .5f + .1f;
                Box(murek, a0, m0, YC - .1f, YC + .4f * K, -.3f, .05f); Box(murek, m1, a1, YC - .1f, YC + .4f * K, -.3f, .05f);
                Box(granit, m0, m1, YC - .3f, YC, -.3f, 0f);
            }
            else Box(murek, a0, a1, YC - .1f, YC + .4f * K, -.3f, .05f);
            Box(alu, a0 - (wzdX ? .35f * K : 0f), a1 + (wzdX ? .35f * K : 0f), Y(1) - Pas, Y(1) - Pas + .18f * K, 0f, .35f * K);
        }
        // dach: płyta, urządzenia (agregaty), maszt z anteną, napis korony
        M(beton).MinMax(new Vector3(d.x0 + .3f, top - .3f, d.z0 + .3f), new Vector3(d.x1 - .3f, top + .05f, d.z1 - .3f));
        for (int i = 0; i < 3; i++) M(stalNierdz).MinMax(new Vector3(d.x0 + 4f + i * 9f, top + .05f, d.z0 + 3f), new Vector3(d.x0 + 4f + i * 9f + 3.2f * K, top + 1.9f * K, d.z0 + 3f + 1.8f * K));
        var mast = new Vector3((d.x0 + d.x1) * .5f + 6f, top + .05f, (d.z0 + d.z1) * .5f);
        M(alu).Cyl(mast, mast + Vector3.up * 12f * K, .12f * K, 10, .05f * K);
        for (int i = 0; i < 4; i++) M(alu).Cyl(mast + Vector3.up * (5f + i * 1.6f) * K - Vector3.right * .8f * K, mast + Vector3.up * (5f + i * 1.6f) * K + Vector3.right * .8f * K, .02f * K, 6);
        foreach (var kv in mFas) kv.Value.Obiekt(d.nazwa + " — elewacja " + kv.Key.name, T, kv.Key, false, true);
        BraxRegionMeble.Napis(T, d.korona, new Vector3(d.szyb.x, top + 1.2f * K, d.z1 + .05f), Vector3.forward, 1.1f * K, new Color(.95f, .95f, .93f));
        BraxRegionMeble.Napis(T, d.korona, new Vector3(d.szyb.x, top + 1.2f * K, d.z0 - .05f), Vector3.back, 1.1f * K, new Color(.95f, .95f, .93f));
        // ---------------- wejście: daszek, drzwi automatyczne, wycieraczka (w posadzce, bez progu)
        Wejscie(T, d);
        // ---------------- trzon: szyb windy (ściany od parteru do dachu), winda BraxWinda
        Winda(T, d, top);
        // ---------------- kondygnacje użytkowe: posadzka, sufit, wnętrza
        foreach (var kv in d.uzytkowe.OrderBy(q => q.Key)) { Kondygnacja(T, d, kv.Key, kv.Value); ilePieter++; }
        BraxRegion.Note("wieżowiec " + d.nazwa + ": kondygnacji " + (n + 1) + ", użytkowe [" + string.Join(", ", d.uzytkowe.Keys) + "], wysokość " + (top + 2.55f * K).ToString("0.0") + " m");
    }

    static void KolizjaSciany(bool wzdX, float c, float a0, float a1, float y0, float y1)
    {
        if (a1 - a0 < .05f) return;
        var mid = wzdX ? new Vector3((a0 + a1) * .5f, (y0 + y1) * .5f, c) : new Vector3(c, (y0 + y1) * .5f, (a0 + a1) * .5f);
        var size = wzdX ? new Vector3(a1 - a0, y1 - y0, .12f) : new Vector3(.12f, y1 - y0, a1 - a0);
        BraxRegionMeble.Kolizja(root, "Wieżowiec: kolizja szyby", mid, size);
    }

    static readonly Dictionary<string, Material> oknaCache = new Dictionary<string, Material>();
    static Material OknoWiezowca(Def d)
    {
        var sh = Shader.Find("Braxhorn/OsiedleOkno"); const string path = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
        if (!sh || !File.Exists(path)) return BraxCityMaterials.Surface("Wieżowiec: szyba jasna (bez atlasu pokoi)", new Color(.55f, .64f, .70f), .12f, .15f);
        var m = new Material(sh) { name = "Wieżowiec: okna z pokojami " + d.nazwa };
        m.SetTexture("_Rooms", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        m.SetFloat("_Variant", 0); m.SetFloat("_Variants", 5); m.SetFloat("_CellH", Hf); m.SetFloat("_Floor0", YC + Hg);
        m.SetFloat("_CellW", 1.5f * K * 2f); m.SetFloat("_Depth", 5f * K); m.SetFloat("_RoomLight", .66f);
        m.SetColor("_Tint", new Color(.80f, .88f, .93f)); m.SetFloat("_Curtains", .3f);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        AssetDatabase.CreateAsset(m, BraxRegion.Out + "/Materials/wiezowiec_okna_" + d.idDrzwi + ".mat");
        return m;
    }

    static void Wejscie(Transform T, Def d)
    {
        float cx = d.szyb.x, z = d.z1 - .18f * K - .01f, dw = 2.4f * K;
        // daszek nad wejściem (szkło na stalowych belkach, wysięg 3.2 m × k)
        var b = new BraxRegionMeble.Bud(); var s = new BraxRegionMeble.Bud();
        float yd = YC + DrzH + .9f * K;
        b.MinMax(new Vector3(cx - 3.2f * K, yd, d.z1), new Vector3(cx + 3.2f * K, yd + .16f, d.z1 + 3.2f * K));
        foreach (float sx in new[] { -2.8f, 2.8f }) b.Cyl(new Vector3(cx + sx * K, yd + .16f, d.z1 + 3.0f * K), new Vector3(cx + sx * K, yd + 2.2f * K, d.z1 + .05f), .03f * K, 8);
        s.MinMax(new Vector3(cx - 3.1f * K, yd + .17f, d.z1 + .05f), new Vector3(cx + 3.1f * K, yd + .19f, d.z1 + 3.1f * K));
        b.Obiekt("Wejście: daszek (stal)", T, alu, false, true);
        s.Obiekt("Wejście: daszek (szkło)", T, szklo, false, false);
        BraxRegionMeble.Napis(T, d.nazwa, new Vector3(cx, yd + .5f * K, d.z1 + 3.2f * K + .03f), Vector3.forward, .34f * K, new Color(.95f, .95f, .93f));
        // drzwi automatyczne przesuwne: dwa skrzydła szklane w ramie, kieszenie za szkłem stałym (bez progu: posadzka = plac)
        var h = new GameObject("Drzwi automatyczne " + d.nazwa).transform; h.SetParent(T, false); h.position = new Vector3(cx, YC, z - .06f);
        Transform Skrzydlo(string n, float off)
        {
            var t = new GameObject(n).transform; t.SetParent(h, false); t.localPosition = new Vector3(off, 0, 0);
            var g = new BraxRegionMeble.Bud(); var r = new BraxRegionMeble.Bud();
            g.MinMax(t.position + new Vector3(-dw * .25f + .04f, .05f, -.01f), t.position + new Vector3(dw * .25f - .04f, DrzH + .15f, .01f));
            r.MinMax(t.position + new Vector3(-dw * .25f, 0f, -.025f), t.position + new Vector3(dw * .25f, .06f, .025f));
            r.MinMax(t.position + new Vector3(-dw * .25f, DrzH + .15f, -.025f), t.position + new Vector3(dw * .25f, DrzH + .21f, .025f));
            r.MinMax(t.position + new Vector3(-dw * .25f, 0f, -.025f), t.position + new Vector3(-dw * .25f + .05f, DrzH + .21f, .025f));
            r.MinMax(t.position + new Vector3(dw * .25f - .05f, 0f, -.025f), t.position + new Vector3(dw * .25f, DrzH + .21f, .025f));
            var go1 = g.Obiekt(n + ": szkło", t, szkloCiemne, false, false); var go2 = r.Obiekt(n + ": rama", t, alu, false, true);
            foreach (var go in new[] { go1, go2 }) if (go) { go.transform.SetParent(t, true); GameObjectUtility.SetStaticEditorFlags(go, 0); }
            var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, DrzH * .5f + .1f, 0); col.size = new Vector3(dw * .5f, DrzH + .2f, .08f);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            return t;
        }
        var dr = h.gameObject.AddComponent<BraxDrzwiPrzesuwne>();
        dr.skrzydloL = Skrzydlo("Skrzydło lewe", -dw * .25f); dr.skrzydloP = Skrzydlo("Skrzydło prawe", dw * .25f);
        dr.przesuw = dw * .5f - .05f; dr.zasieg = 2.8f; dr.nazwa = d.nazwa;
        // wycieraczka wpuszczona w posadzkę holu (w wycięciu posadzki: posadzka holu ma otwór w tym miejscu — Kondygnacja 0)
        punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi, label = d.nazwa + " — wejście", position = new Vector3(cx, YC, d.z1 + 2f) });
    }

    static readonly List<BraxCityPoint> punkty = new List<BraxCityPoint>();

    // ---------------- winda
    static void Winda(Transform T, Def d, float top)
    {
        int n = d.pietra;
        var pY = new float[n + 1]; var nazwy = new string[n + 1]; var uz = new bool[n + 1];
        for (int i = 0; i <= n; i++) { pY[i] = Y(i); uz[i] = d.uzytkowe.ContainsKey(i); nazwy[i] = i == 0 ? "0 HOL" : uz[i] ? i + " " + Opis(d.uzytkowe[i]) : i.ToString(); }
        Winda(T, d.nazwa, d.szyb, pY, uz, nazwy, top, 1f);
    }

    // Winda wielopiętrowa (BraxWinda): szyb od parteru do `top`, drzwi kabiny i przystanków od strony f (+1 = +z, −1 = −z),
    // przystanki (drzwi, opaska, przycisk przywołania, numer) tylko na piętrach użytkowych; panel w kabinie: wszystkie piętra.
    static void Winda(Transform T, string nazwa, Vector2 szyb, float[] pY, bool[] uz, string[] nazwy, float top, float f)
    {
        var W = new GameObject("Winda " + nazwa).transform; W.SetParent(T, false);
        float sx = szyb.x, sz = szyb.y, hw = KabW * .5f + .1f, hd = KabD * .5f + .1f, t = .25f;
        float y0s = pY[0];
        Vector3 V(float x, float y, float dz) => new Vector3(x, y, sz + f * dz);   // dz: od osi szybu w stronę drzwi
        void BoxZ(BraxRegionMeble.Bud bb, float x0, float x1, float y0, float y1, float dz0, float dz1) => bb.MinMax(new Vector3(x0, y0, Mathf.Min(sz + f * dz0, sz + f * dz1)), new Vector3(x1, y1, Mathf.Max(sz + f * dz0, sz + f * dz1)));
        void KolZ(string nn, float x0, float x1, float y0, float y1, float dz0, float dz1) { float za = sz + f * dz0, zb = sz + f * dz1; BraxRegionMeble.Kolizja(W, nn, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (za + zb) * .5f), new Vector3(x1 - x0, y1 - y0, Mathf.Abs(zb - za))); }
        var sc = new BraxRegionMeble.Bud();
        // ściany szybu: tył, boki (pełna wysokość); front — z otworami drzwi na piętrach użytkowych
        BoxZ(sc, sx - hw - t, sx + hw + t, y0s, top, -hd - t, -hd); KolZ("Szyb: kolizja tył", sx - hw - t, sx + hw + t, y0s, top, -hd - t, -hd);
        BoxZ(sc, sx - hw - t, sx - hw, y0s, top, -hd, hd); KolZ("Szyb: kolizja L", sx - hw - t, sx - hw, y0s, top, -hd, hd);
        BoxZ(sc, sx + hw, sx + hw + t, y0s, top, -hd, hd); KolZ("Szyb: kolizja P", sx + hw, sx + hw + t, y0s, top, -hd, hd);
        int n = pY.Length - 1;
        var pL = new Transform[n + 1]; var pP = new Transform[n + 1]; var wez = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float y0 = pY[i], y1 = i == n ? top : pY[i + 1];
            if (y1 <= y0 + .1f) y1 = y0 + 3.5f * K;
            if (uz[i])
            {
                BoxZ(sc, sx - hw - t, sx - DrzW * .5f - .05f, y0, y1, hd, hd + t);
                BoxZ(sc, sx + DrzW * .5f + .05f, sx + hw + t, y0, y1, hd, hd + t);
                BoxZ(sc, sx - DrzW * .5f - .05f, sx + DrzW * .5f + .05f, y0 + DrzH + .1f, y1, hd, hd + t);
                KolZ("Szyb: front L", sx - hw - t, sx - DrzW * .5f - .05f, y0, y1, hd, hd + t);
                KolZ("Szyb: front P", sx + DrzW * .5f + .05f, sx + hw + t, y0, y1, hd, hd + t);
                var hold = new GameObject("Drzwi przystanku p." + i).transform; hold.SetParent(W, false); hold.position = V(sx, y0, hd + t + .03f);
                pL[i] = Lisc(hold, "skrzydło L", -DrzW * .25f); pP[i] = Lisc(hold, "skrzydło P", DrzW * .25f);
                var op = new BraxRegionMeble.Bud();
                BoxZ(op, sx - DrzW * .5f - .16f, sx - DrzW * .5f - .05f, y0, y0 + DrzH + .2f, hd + t, hd + t + .06f);
                BoxZ(op, sx + DrzW * .5f + .05f, sx + DrzW * .5f + .16f, y0, y0 + DrzH + .2f, hd + t, hd + t + .06f);
                BoxZ(op, sx - DrzW * .5f - .16f, sx + DrzW * .5f + .16f, y0 + DrzH + .1f, y0 + DrzH + .25f, hd + t, hd + t + .06f);
                BoxZ(op, sx + DrzW * .5f + .35f, sx + DrzW * .5f + .55f, y0 + 1.0f * K, y0 + 1.35f * K, hd + t, hd + t + .03f);
                op.Obiekt("Winda: opaska drzwi i kaseta przywołania p." + i, W, stalNierdz, false, false);
                var kb = new BraxRegionMeble.Bud(); kb.Cyl(V(sx + DrzW * .5f + .45f, y0 + 1.18f * K, hd + t + .03f), V(sx + DrzW * .5f + .45f, y0 + 1.18f * K, hd + t + .045f), .035f * K, 12);
                kb.Obiekt("Winda: przycisk przywołania p." + i, W, przyciskOn, false, false);
                BraxRegionMeble.Napis(W, i.ToString(), V(sx, y0 + DrzH + .5f * K, hd + t + .07f), new Vector3(0, 0, f), .22f * K, new Color(.92f, .92f, .9f));
                wez[i] = V(sx + DrzW * .5f + .45f, y0, hd + t + 1.2f);
            }
            else BoxZ(sc, sx - hw - t, sx + hw + t, y0, y1, hd, hd + t);
        }
        sc.Obiekt("Szyb windy " + nazwa, W, tynk, false, true);
        // kabina: podłoga z kolizją, ściany, sufit z panelem LED, poręcze, panel przycisków wszystkich pięter
        var kab = new GameObject("Kabina " + nazwa).transform; kab.SetParent(W, false); kab.position = new Vector3(sx, y0s, sz);
        var kb2 = new BraxRegionMeble.Bud(); var kw = new BraxRegionMeble.Bud(); var kl = new BraxRegionMeble.Bud();
        float iw = KabW * .5f, id = KabD * .5f, ih = 2.4f * K;
        Vector3 L(float x, float y, float dz) => kab.position + new Vector3(x, y, f * dz);
        void KB(BraxRegionMeble.Bud bb, float x0, float y0, float dz0, float x1, float y1, float dz1) => bb.MinMax(Vector3.Min(L(x0, y0, dz0), L(x1, y1, dz1)), Vector3.Max(L(x0, y0, dz0), L(x1, y1, dz1)));
        KB(kb2, -iw, -.15f, -id, iw, 0f, id);
        KB(kw, -iw, 0f, -id - .05f, iw, ih, -id); KB(kw, -iw - .05f, 0f, -id, -iw, ih, id); KB(kw, iw, 0f, -id, iw + .05f, ih, id);
        KB(kw, -iw - .05f, ih, -id - .05f, iw + .05f, ih + .1f, id);
        KB(kw, -iw, 0f, id - .02f, -DrzW * .5f, ih, id); KB(kw, DrzW * .5f, 0f, id - .02f, iw, ih, id); KB(kw, -DrzW * .5f, DrzH, id - .02f, DrzW * .5f, ih, id);
        KB(kl, -iw + .2f, ih - .03f, -id + .2f, iw - .2f, ih - .005f, id - .2f);
        foreach (float sxx in new[] { -1f, 1f }) kb2.Cyl(L(sxx * (iw - .06f), .95f * K, -id + .2f), L(sxx * (iw - .06f), .95f * K, id - .3f), .02f * K, 8);
        var g1 = kb2.Obiekt("Kabina: podłoga i poręcze", kab, kamienPos, true, true); var g2 = kw.Obiekt("Kabina: ściany (stal szczotkowana)", kab, stalNierdz, false, true); var g3 = kl.Obiekt("Kabina: panel LED", kab, ledM, false, false);
        foreach (var g in new[] { g1, g2, g3 }) if (g) g.transform.SetParent(kab, true);
        BraxRegionMeble.Kolizja(kab, "Kabina: kolizja tył", L(0, ih * .5f, -id - .03f), new Vector3(KabW, ih, .06f));
        BraxRegionMeble.Kolizja(kab, "Kabina: kolizja L", L(-iw - .03f, ih * .5f, 0), new Vector3(.06f, ih, KabD));
        BraxRegionMeble.Kolizja(kab, "Kabina: kolizja P", L(iw + .03f, ih * .5f, 0), new Vector3(.06f, ih, KabD));
        var dk = new GameObject("Drzwi kabiny").transform; dk.SetParent(kab, false); dk.position = L(0, 0, id + .03f);
        var kL = Lisc(dk, "skrzydło kabiny L", -DrzW * .25f); var kP = Lisc(dk, "skrzydło kabiny P", DrzW * .25f);
        var przyciski = new Renderer[n + 1];
        var pan = new BraxRegionMeble.Bud(); KB(pan, iw - .02f, .9f * K, id - .2f * K - (n / 4 + 1) * .0f, iw, Mathf.Min(ih - .2f, 1.0f * K + ((n + 3) / 4) * .1f * K), id - .62f * K);
        var pg = pan.Obiekt("Kabina: kaseta panelu", kab, czarny, false, false); if (pg) pg.transform.SetParent(kab, true);
        int kol = n + 1 > 16 ? 4 : 2;
        for (int i = 0; i <= n; i++)
        {
            int col = i % kol, row = i / kol;
            var c = L(iw - .03f, .95f * K + row * (kol > 2 ? .085f : .12f) * K, id - .26f * K - col * .1f * K);
            var bb = new BraxRegionMeble.Bud(); bb.Cyl(c, c - Vector3.right * .015f, (kol > 2 ? .028f : .035f) * K, 10);
            var go = bb.Obiekt("Przycisk " + i, kab, uz[i] ? przyciskOn : przyciskOff, false, false);
            if (go) { go.transform.SetParent(kab, true); przyciski[i] = go.GetComponent<Renderer>(); }
        }
        var wd = W.gameObject.AddComponent<BraxWinda>();
        wd.nazwa = nazwa; wd.kabina = kab; wd.kabinaL = kL; wd.kabinaP = kP;
        wd.pietraY = pY; wd.pietraNazwy = nazwy; wd.uzytkowe = uz; wd.przystanekL = pL; wd.przystanekP = pP; wd.wezwanie = wez;
        wd.szyb = new Vector2(sx, sz); wd.wnetrze = new Vector2(iw, id); wd.przesuw = DrzW * .5f - .04f; wd.start = 0;
        wd.przyciski = przyciski; wd.przyciskAktywny = przyciskOn; wd.przyciskWybrany = przyciskSel; wd.przyciskNieaktywny = przyciskOff;
        foreach (var tt in kab.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tt.gameObject, 0);
        foreach (var tt in W.GetComponentsInChildren<Transform>(true)) if (tt.name.StartsWith("skrzydło") || tt.name.StartsWith("Drzwi")) GameObjectUtility.SetStaticEditorFlags(tt.gameObject, 0);
    }

    static Transform Lisc(Transform parent, string n, float off)
    {
        var t = new GameObject(n).transform; t.SetParent(parent, false); t.localPosition = new Vector3(off, 0, 0);
        var b = new BraxRegionMeble.Bud(); b.MinMax(t.position + new Vector3(-DrzW * .25f, 0f, -.02f), t.position + new Vector3(DrzW * .25f, DrzH, .02f));
        var go = b.Obiekt(n, t, stalNierdz, false, true); if (go) { go.transform.SetParent(t, true); GameObjectUtility.SetStaticEditorFlags(go, 0); }
        var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, DrzH * .5f, 0); col.size = new Vector3(DrzW * .5f, DrzH, .06f);
        GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        return t;
    }

    static string Opis(Rodzaj r) => r == Rodzaj.Biura ? "BIURA" : r == Rodzaj.Zarzad ? "ZARZĄD" : r == Rodzaj.Konferencja ? "KONFERENCJA" : r == Rodzaj.Fitness ? "FITNESS" : "HOL";

    // ---------------- kondygnacja użytkowa
    static void Kondygnacja(Transform T, Def d, int i, Rodzaj r)
    {
        var F = new GameObject("Kondygnacja " + i + " " + Opis(r) + " " + d.nazwa).transform; F.SetParent(T, false);
        float y0 = Y(i), y1 = i == 0 ? Y(1) : Y(i) + Hf, yc = y1 - Pas - .05f;   // sufit podwieszany pod pasem stropowym
        // posadzka i sufit do pasa stropowego (0.3 m od lica) — styk krawędzią z panelem elewacji, bez nakładania
        float ix0 = d.x0 + .3f, ix1 = d.x1 - .3f, iz0 = d.z0 + .3f, iz1 = d.z1 - .3f;
        float sx = d.szyb.x, sz = d.szyb.y, hw = KabW * .5f + .35f, hd = KabD * .5f + .35f;
        bool lobby = r == Rodzaj.Lobby || r == Rodzaj.LobbyKawiarnia;
        var pos = lobby ? kamienPos : r == Rodzaj.Fitness ? BraxCityMaterials.Surface("Fitness: nawierzchnia gumowa", new Color(.16f, .17f, .18f), .95f) : r == Rodzaj.Konferencja ? drewno : wykladzina;
        // posadzka (górna powierzchnia = y0; w holu y0 = plac — bez progu) z otworem na szyb; strop (płyta) pod nią
        var p = new BraxRegionMeble.Bud();
        p.MinMax(new Vector3(ix0, y0 - .3f, iz0), new Vector3(ix1, y0, sz - hd));
        p.MinMax(new Vector3(ix0, y0 - .3f, sz + hd), new Vector3(ix1, y0, iz1));
        p.MinMax(new Vector3(ix0, y0 - .3f, sz - hd), new Vector3(sx - hw, y0, sz + hd));
        p.MinMax(new Vector3(sx + hw, y0 - .3f, sz - hd), new Vector3(ix1, y0, sz + hd));
        p.Obiekt("Posadzka p." + i, F, pos, false, false);
        BraxRegionMeble.Kolizja(F, "Posadzka kolizja", new Vector3((ix0 + ix1) * .5f, y0 - .15f, (iz0 + iz1) * .5f), new Vector3(ix1 - ix0, .3f, iz1 - iz0));
        // sufit podwieszany z panelami LED (raster 1.2 m × k), otwór nad szybem nie jest potrzebny (szyb ma ściany)
        var s = new BraxRegionMeble.Bud(); var led = new BraxRegionMeble.Bud();
        s.MinMax(new Vector3(ix0, yc, iz0), new Vector3(ix1, yc + .04f, iz1));
        for (float x = ix0 + 2f; x < ix1 - 1.5f; x += 3.6f * K) for (float z = iz0 + 1.5f; z < iz1 - 1f; z += 3.0f * K)
            led.MinMax(new Vector3(x, yc - .015f, z), new Vector3(x + .6f * K, yc - .002f, z + .6f * K));
        s.Obiekt("Sufit p." + i, F, sufit, false, false); led.Obiekt("Panele LED p." + i, F, ledM, false, false);
        BraxRegionMeble.Kolizja(F, "Sufit kolizja (kamera nie wychodzi nad strop)", new Vector3((ix0 + ix1) * .5f, yc + .1f, (iz0 + iz1) * .5f), new Vector3(ix1 - ix0, .2f, iz1 - iz0));
        BraxRegion.Warm(F, "Oświetlenie p." + i + " (1)", new Vector3(ix0 + (ix1 - ix0) * .3f, yc - .4f, (iz0 + iz1) * .5f), 1.4f, 14f);
        BraxRegion.Warm(F, "Oświetlenie p." + i + " (2)", new Vector3(ix0 + (ix1 - ix0) * .7f, yc - .4f, (iz0 + iz1) * .5f), 1.4f, 14f);
        // WC i klatka schodowa za szybem (pas tylny): ściany działowe z drzwiami; zielony znak „WYJŚCIE EWAKUACYJNE”
        PasTylny(F, d, i, y0, yc);
        // wyposażenie wg rodzaju
        float fz0 = sz + hd + 1.4f * K, fz1 = iz1 - .6f;   // strefa frontowa (od korytarza przy windzie do szyby)
        switch (r)
        {
            case Rodzaj.Lobby: Lobby(F, d, y0, false); break;
            case Rodzaj.LobbyKawiarnia: Lobby(F, d, y0, true); break;
            case Rodzaj.Biura: Biura(F, d, y0, ix0, ix1, fz0, fz1); break;
            case Rodzaj.Zarzad: Zarzad(F, d, i, y0, ix0, ix1, fz0, fz1, yc); break;
            case Rodzaj.Konferencja: Konferencja(F, d, y0, ix0, ix1, fz0, fz1); break;
            case Rodzaj.Fitness: Fitness(F, d, y0, ix0, ix1, fz0, fz1); break;
        }
        punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_P" + i, label = d.nazwa + " — piętro " + i + " " + Opis(r), position = new Vector3(sx, y0, sz + hd + 1.5f) });
        ileWnetrz++;
    }

    // pas tylny (za szybem windy, strona −z): WC (drzwi), klatka schodowa (drzwi + znak), ściana z tynku na całej długości
    static void PasTylny(Transform F, Def d, int i, float y0, float yc)
    {
        float sz = d.szyb.y, hd = KabD * .5f + .35f, zb = sz - hd;   // ściana pasa tylnego = tył szybu
        float ix0 = d.x0 + .3f, ix1 = d.x1 - .3f, iz0 = d.z0 + .3f;
        if (zb - iz0 < 2.2f) return;
        var w = new BraxRegionMeble.Bud();
        float wcX0 = ix0 + 1f, wcX1 = wcX0 + 3.2f * K, klX0 = ix1 - 1f - 3.6f * K, klX1 = ix1 - 1f;
        float dW = 1.0f * K, dH = 2.1f * K;
        // ściana wzdłuż x na z = zb (lico) z otworami drzwi WC i klatki
        void SegX(float a, float b) { if (b - a > .02f) { w.MinMax(new Vector3(a, y0, zb - .15f), new Vector3(b, yc, zb)); BraxRegionMeble.Kolizja(F, "Ściana kolizja", new Vector3((a + b) * .5f, (y0 + yc) * .5f, zb - .075f), new Vector3(b - a, yc - y0, .15f)); } }
        float wcD = (wcX0 + wcX1) * .5f, klD = (klX0 + klX1) * .5f;
        SegX(ix0, wcD - dW * .5f); SegX(wcD + dW * .5f, klD - dW * .5f); SegX(klD + dW * .5f, ix1);
        w.MinMax(new Vector3(wcD - dW * .5f, y0 + dH, zb - .15f), new Vector3(wcD + dW * .5f, yc, zb)); w.MinMax(new Vector3(klD - dW * .5f, y0 + dH, zb - .15f), new Vector3(klD + dW * .5f, yc, zb));
        // ściany boczne WC i klatki (do szyby tylnej)
        foreach (float x in new[] { wcX1, klX0 }) { w.MinMax(new Vector3(x - .07f, y0, iz0), new Vector3(x + .07f, yc, zb - .15f)); BraxRegionMeble.Kolizja(F, "Ściana kolizja", new Vector3(x, (y0 + yc) * .5f, (iz0 + zb) * .5f), new Vector3(.14f, yc - y0, zb - iz0)); }
        w.Obiekt("Ściany pasa tylnego p." + i, F, tynk, false, true);
        Drzwi(F, d.idDrzwi + "_WC_" + i, "WC", new Vector3(wcD, y0, zb - .075f), dW, dH);
        Drzwi(F, d.idDrzwi + "_KLATKA_" + i, "KLATKA SCHODOWA", new Vector3(klD, y0, zb - .075f), dW, dH);
        BraxRegionMeble.Napis(F, "WC", new Vector3(wcD, y0 + dH + .25f, zb + .02f), Vector3.forward, .16f * K, new Color(.2f, .2f, .22f));
        var zn = new BraxRegionMeble.Bud(); zn.MinMax(new Vector3(klD - .45f * K, y0 + dH + .12f, zb), new Vector3(klD + .45f * K, y0 + dH + .42f, zb + .03f));
        zn.Obiekt("Znak: wyjście ewakuacyjne", F, zielonyZnak, false, false);
        BraxRegionMeble.Napis(F, "WYJŚCIE EWAKUACYJNE", new Vector3(klD, y0 + dH + .27f, zb + .04f), Vector3.forward, .09f * K, Color.white);
        // WC: miska, umywalka z lustrem (pionowe — nie posadzka), suszarka; klatka: biegi schodów (bez wejścia na inne piętra)
        var wc = new BraxRegionMeble.Bud(); var bial = BraxCityMaterials.Surface("WC: ceramika biała", new Color(.95f, .95f, .94f), .2f);
        var cm = new Vector3(wcX0 + .55f * K, y0, iz0 + .45f * K);
        wc.Cyl(cm, cm + Vector3.up * .4f * K, .2f * K, 16, .17f * K); wc.MinMax(cm + new Vector3(-.2f * K, .4f * K, -.35f * K), cm + new Vector3(.2f * K, .85f * K, -.2f * K));
        var um = new Vector3(wcX1 - .55f * K, y0, iz0 + .3f * K);
        wc.MinMax(um + new Vector3(-.3f * K, .78f * K, -.05f), um + new Vector3(.3f * K, .88f * K, .4f * K));
        wc.Obiekt("WC p." + i + ": ceramika", F, bial, false, true);
        BraxRegionMeble.Kolizja(F, "WC kolizja", cm + Vector3.up * .4f * K, new Vector3(.45f * K, .85f * K, .75f * K));
        var schody = new BraxRegionMeble.Bud();
        float run = .3f * K, rise = .17f * K; int st = Mathf.Clamp(Mathf.FloorToInt((zb - .2f - iz0 - .3f) / run), 3, 12);
        for (int k = 0; k < st; k++) schody.MinMax(new Vector3(klX0 + .3f, y0 + k * rise, iz0 + .3f + k * run), new Vector3(klX0 + .3f + 1.2f * K, y0 + (k + 1) * rise, iz0 + .3f + (k + 1) * run));
        schody.Obiekt("Klatka p." + i + ": bieg schodów", F, beton, false, true);
    }

    static void Drzwi(Transform F, string id, string label, Vector3 c, float w, float h)
    {
        var holder = new GameObject("Drzwi " + label + " (" + id + ")").transform; holder.SetParent(F, false); holder.position = c; holder.rotation = Quaternion.identity;
        var zaw = new GameObject("zawias").transform; zaw.SetParent(holder, false); zaw.position = c - Vector3.right * w * .5f;
        var b = new BraxRegionMeble.Bud(); b.MinMax(zaw.position + new Vector3(.01f, .01f, -.025f), zaw.position + new Vector3(w - .01f, h - .01f, .025f));
        var kl = new BraxRegionMeble.Bud(); kl.Cyl(zaw.position + new Vector3(w - .12f, 1.05f * K, .025f), zaw.position + new Vector3(w - .12f, 1.05f * K, .08f), .015f * K, 8);
        var g1 = b.Obiekt("Skrzydło " + label, zaw, drewno, false, true); var g2 = kl.Obiekt("Klamka " + label, zaw, stalNierdz, false, false);
        foreach (var g in new[] { g1, g2 }) if (g) { g.transform.SetParent(zaw, true); GameObjectUtility.SetStaticEditorFlags(g, 0); }
        var col = zaw.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(w * .5f, h * .5f, 0); col.size = new Vector3(w, h, .06f);
        GameObjectUtility.SetStaticEditorFlags(zaw.gameObject, 0); GameObjectUtility.SetStaticEditorFlags(holder.gameObject, 0);
        var door = holder.gameObject.AddComponent<BraxCityDoor>();
        door.id = id; door.label = label; door.leaves = new[] { zaw }; door.sides = new[] { 1f };
    }

    // ---------------- wyposażenie
    static void Model(Transform F, string n, Vector3 p, float yaw, float h, bool kol, params string[] ids) => BraxRegionMeble.Model(F, n, p, yaw, h, kol, ids);

    static void Lobby(Transform F, Def d, float y0, bool kawiarnia)
    {
        float sx = d.szyb.x, sz = d.szyb.y, hd = KabD * .5f + .35f;
        float zf = d.z1 - .6f, zl = sz + hd + 2.4f;
        // recepcja: lada z blatem kamiennym i przejściem, ekran z nazwą budynku na ścianie szybu
        var lada = new BraxRegionMeble.Bud(); var blat = new BraxRegionMeble.Bud();
        float lx = sx + (kawiarnia ? 8f : 7f), lz = zl + 1.2f;
        lada.MinMax(new Vector3(lx - 1.8f * K, y0, lz - .35f * K), new Vector3(lx + 1.8f * K, y0 + 1.05f * K, lz + .35f * K));
        blat.MinMax(new Vector3(lx - 1.85f * K, y0 + 1.05f * K, lz - .4f * K), new Vector3(lx + 1.85f * K, y0 + 1.1f * K, lz + .42f * K));
        lada.Obiekt("Recepcja: lada (dąb)", F, drewno, false, true); blat.Obiekt("Recepcja: blat (kamień)", F, kamienPos, false, true);
        BraxRegionMeble.Kolizja(F, "Recepcja kolizja", new Vector3(lx, y0 + .55f * K, lz), new Vector3(3.6f * K, 1.1f * K, .8f * K));
        BraxRegionMeble.KrzesloBiurowe(F, new Vector3(lx, y0, lz - .95f * K), 0f);
        BraxRegionMeble.Napis(F, "RECEPCJA", new Vector3(lx, y0 + .6f * K, lz + .36f * K), Vector3.forward, .16f * K, new Color(.92f, .9f, .85f));
        // tablica pięter (przy windzie)
        var tab = new BraxRegionMeble.Bud(); float tx = sx - 2.6f * K; float tz = sz + hd + .02f;
        tab.MinMax(new Vector3(tx - .55f * K, y0 + 1.2f * K, tz), new Vector3(tx + .55f * K, y0 + 2.4f * K, tz + .05f));
        tab.Obiekt("Tablica pięter", F, czarny, false, false);
        BraxRegionMeble.Napis(F, d.nazwa, new Vector3(tx, y0 + 2.25f * K, tz + .06f), Vector3.forward, .09f * K, Color.white);
        int k = 0;
        foreach (var kv in d.uzytkowe.OrderByDescending(q => q.Key)) { BraxRegionMeble.Napis(F, kv.Key + "  " + Opis(kv.Value), new Vector3(tx, y0 + 2.05f * K - k * .17f * K, tz + .06f), Vector3.forward, .07f * K, new Color(.9f, .9f, .85f)); k++; }
        // poczekalnia: 2 sofy, stolik, rośliny (Poly Haven, wymiary rzeczywiste × k)
        float wx = sx - (kawiarnia ? 9f : 8f);
        Model(F, "Hol: sofa", new Vector3(wx, y0, zf - 1.6f * K), 180f, .82f, true, "sofa_02", "sofa_03", "Sofa_01");
        Model(F, "Hol: sofa", new Vector3(wx, y0, zf - 4.2f * K), 0f, .82f, true, "sofa_02", "sofa_03", "Sofa_01");
        Model(F, "Hol: stolik", new Vector3(wx, y0, zf - 2.9f * K), 0f, .42f, true, "modern_coffee_table_01", "WoodenTable_02");
        foreach (float dx in new[] { -3.2f, 3.2f }) Model(F, "Hol: roślina w donicy", new Vector3(wx + dx * K, y0, zf - .9f * K), dx * 20f, 1.6f, true, "potted_plant_04", "potted_plant_02", "pachira_aquatica_01");
        Model(F, "Hol: roślina", new Vector3(sx + 3.2f * K, y0, sz + hd + .7f * K), 30f, 1.7f, true, "potted_plant_04", "potted_plant_02", "pachira_aquatica_01");
        if (kawiarnia)
        {
            // kawiarnia w holu: bar z ekspresem, lodówka, stoliki z krzesłami (BraxSeat), tablica menu
            float bx = d.x0 + 4f, bz = zl + 2f;
            var bar = new BraxRegionMeble.Bud(); bar.MinMax(new Vector3(bx - .4f * K, y0, bz - 2.2f * K), new Vector3(bx + .4f * K, y0 + 1.05f * K, bz + 2.2f * K));
            bar.Obiekt("Kawiarnia: bar (dąb)", F, drewno, false, true);
            var bb = new BraxRegionMeble.Bud(); bb.MinMax(new Vector3(bx - .45f * K, y0 + 1.05f * K, bz - 2.25f * K), new Vector3(bx + .45f * K, y0 + 1.1f * K, bz + 2.25f * K)); bb.Obiekt("Kawiarnia: blat", F, kamienPos, false, true);
            BraxRegionMeble.Kolizja(F, "Bar kolizja", new Vector3(bx, y0 + .55f * K, bz), new Vector3(.9f * K, 1.1f * K, 4.5f * K));
            var ex = new BraxRegionMeble.Bud(); ex.MinMax(new Vector3(bx - .25f * K, y0 + 1.1f * K, bz - .4f * K), new Vector3(bx + .2f * K, y0 + 1.55f * K, bz + .3f * K)); ex.Obiekt("Kawiarnia: ekspres", F, stalNierdz, false, false);
            BraxRegionMeble.Napis(F, "KAWIARNIA", new Vector3(bx + .5f * K, y0 + 2.6f * K, bz), Vector3.right, .3f * K, new Color(.95f, .9f, .78f));
            var bl = new BraxRegionMeble.Bud(); var nl = new BraxRegionMeble.Bud(); var kd = new BraxRegionMeble.Bud(); var ks = new BraxRegionMeble.Bud();
            for (int a = 0; a < 2; a++) for (int b = 0; b < 2; b++)
            {
                var c = new Vector3(bx + 3.2f * K + a * 2.4f * K, y0, bz - 1.6f * K + b * 2.6f * K);
                BraxRegionMeble.Stolik(F, bl, nl, c);
                BraxRegionMeble.Krzeslo(F, kd, ks, c + new Vector3(0, 0, -.6f * K), 0f);
                BraxRegionMeble.Krzeslo(F, kd, ks, c + new Vector3(0, 0, .6f * K), 180f);
            }
            bl.Obiekt("Kawiarnia: blaty stolików", F, kamienPos, false, true); nl.Obiekt("Kawiarnia: nogi stolików", F, czarny, false, true);
            kd.Obiekt("Kawiarnia: krzesła (drewno)", F, drewno, false, true); ks.Obiekt("Kawiarnia: krzesła (stal)", F, czarny, false, true);
            punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_KAWIARNIA", label = d.nazwa + " — kawiarnia", position = new Vector3(bx + .8f * K, y0, bz) });
        }
    }

    static void Biura(Transform F, Def d, float y0, float ix0, float ix1, float fz0, float fz1)
    {
        // open space: rzędy biurek (1.6 × 0.8 m × k) z krzesłami, poza korytarzem przy windzie (±3 m od osi szybu)
        float sx = d.szyb.x;
        for (float x = ix0 + 2.2f; x < ix1 - 2.2f; x += 2.0f * K)
        {
            if (Mathf.Abs(x - sx) < 3.2f) continue;
            for (float z = fz0 + .8f; z < fz1 - .8f; z += 3.0f * K)
            {
                BraxRegionMeble.Biurko(F, new Vector3(x, y0, z), 180f);
                BraxRegionMeble.KrzesloBiurowe(F, new Vector3(x, y0, z - .75f * K), 0f);
            }
        }
        Model(F, "Biuro: roślina", new Vector3(ix0 + .8f, y0, fz1 - .6f), 0f, 1.6f, true, "potted_plant_04", "potted_plant_02");
        Model(F, "Biuro: roślina", new Vector3(ix1 - .8f, y0, fz1 - .6f), 90f, 1.6f, true, "potted_plant_04", "potted_plant_02");
    }

    static void Zarzad(Transform F, Def d, int i, float y0, float ix0, float ix1, float fz0, float fz1, float yc)
    {
        // gabinet szefa (wschód, za szklaną ścianką z drzwiami), w nim SEJF przy ścianie szybu; biurka asystentów; trasa ochroniarza
        float gx = ix1 - 9f;
        var sc = new BraxRegionMeble.Bud();
        sc.MinMax(new Vector3(gx - .03f, y0, fz0 - 1.4f * K), new Vector3(gx + .03f, yc, fz1 - 2.2f * K));
        sc.Obiekt("Gabinet: ścianka szklana", F, szklo, false, false);
        BraxRegionMeble.Kolizja(F, "Gabinet: ścianka kolizja", new Vector3(gx, (y0 + yc) * .5f, (fz0 - 1.4f * K + fz1 - 2.2f * K) * .5f), new Vector3(.08f, yc - y0, fz1 - 2.2f * K - fz0 + 1.4f * K));
        BraxRegionMeble.Biurko(F, new Vector3(gx + 4.5f, y0, (fz0 + fz1) * .5f), -90f, 2.0f, "Gabinet szefa: biurko");
        BraxRegionMeble.KrzesloBiurowe(F, new Vector3(gx + 5.5f, y0, (fz0 + fz1) * .5f), -90f);
        Model(F, "Gabinet: sofa", new Vector3(gx + 2f, y0, fz1 - .9f * K), 180f, .82f, true, "sofa_02", "sofa_03");
        var sejf = BraxRegionMeble.Sejf(F, new Vector3(ix1 - .6f * K, y0, fz0 + .2f), -90f);
        Model(F, "Gabinet: roślina", new Vector3(ix1 - .8f, y0, fz1 - .6f), 0f, 1.7f, true, "potted_plant_04", "potted_plant_02");
        // biurka asystentów (zachód) i stanowisko ochrony przy windzie
        for (float x = ix0 + 2.5f; x < gx - 3f; x += 2.4f * K) { if (Mathf.Abs(x - d.szyb.x) < 3.2f) continue; BraxRegionMeble.Biurko(F, new Vector3(x, y0, fz1 - 1.6f), 180f); BraxRegionMeble.KrzesloBiurowe(F, new Vector3(x, y0, fz1 - 2.4f * K), 0f); }
        BraxRegionMeble.Biurko(F, new Vector3(d.szyb.x + 3.6f, y0, fz0 + .4f), 180f, 1.4f, "Ochrona: biurko");
        if (swiat)
        {
            punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_SEJF", label = d.nazwa + " — sejf w gabinecie zarządu (misja)", position = new Vector3(ix1 - 1.4f * K, y0, fz0 + .2f) });
            int k = 0;
            foreach (var p in new[] { new Vector3(d.szyb.x + 3.6f, y0, fz0 + 1.6f), new Vector3(ix0 + 2.5f, y0, fz0 + .8f), new Vector3(ix0 + 2.5f, y0, fz1 - 3.2f), new Vector3(gx - 1.5f, y0, fz1 - 3.2f), new Vector3(gx - 1.5f, y0, fz0 + .8f) })
                punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_OCHRONA_" + (++k), label = d.nazwa + " — trasa ochroniarza " + k, position = p });
        }
    }

    static void Konferencja(Transform F, Def d, float y0, float ix0, float ix1, float fz0, float fz1)
    {
        // sala konferencyjna z widokiem: stół 6 × 1.4 m × k, 12 krzeseł, ekran; strefa wypoczynku z sofami przy oknach
        float cx = ix0 + (ix1 - ix0) * .3f, cz = (fz0 + fz1) * .5f;
        var st = new BraxRegionMeble.Bud(); st.MinMax(new Vector3(cx - 3f * K, y0 + .72f * K, cz - .7f * K), new Vector3(cx + 3f * K, y0 + .76f * K, cz + .7f * K));
        foreach (float dx in new[] { -2.2f, 0f, 2.2f }) st.MinMax(new Vector3(cx + dx * K - .2f, y0, cz - .2f), new Vector3(cx + dx * K + .2f, y0 + .72f * K, cz + .2f));
        st.Obiekt("Konferencja: stół", F, drewno, false, true);
        BraxRegionMeble.Kolizja(F, "Stół kolizja", new Vector3(cx, y0 + .38f * K, cz), new Vector3(6f * K, .76f * K, 1.4f * K));
        for (int k = 0; k < 6; k++) { float x = cx - 2.5f * K + k * 1.0f * K; BraxRegionMeble.KrzesloBiurowe(F, new Vector3(x, y0, cz - 1.1f * K), 0f); BraxRegionMeble.KrzesloBiurowe(F, new Vector3(x, y0, cz + 1.1f * K), 180f); }
        var ekr = new BraxRegionMeble.Bud(); ekr.MinMax(new Vector3(cx - 3.8f * K - .05f, y0 + 1.0f * K, cz - 1.1f * K), new Vector3(cx - 3.8f * K, y0 + 2.2f * K, cz + 1.1f * K)); ekr.Obiekt("Konferencja: ekran", F, czarny, false, false);
        float lx = ix0 + (ix1 - ix0) * .75f;
        Model(F, "Wypoczynek: sofa", new Vector3(lx, y0, fz1 - 1.2f * K), 180f, .82f, true, "sofa_02", "sofa_03");
        Model(F, "Wypoczynek: fotel", new Vector3(lx + 2.4f * K, y0, fz1 - 2.6f * K), -120f, .9f, true, "modern_arm_chair_01", "mid_century_lounge_chair");
        Model(F, "Wypoczynek: stolik", new Vector3(lx, y0, fz1 - 2.5f * K), 0f, .42f, true, "modern_coffee_table_01");
        punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_WIDOK", label = d.nazwa + " — sala konferencyjna z widokiem", position = new Vector3(lx, y0, fz1 - .5f) });
    }

    static void Fitness(Transform F, Def d, float y0, float ix0, float ix1, float fz0, float fz1)
    {
        // klub fitness: bieżnie (2.0 × 0.8 m × k), ławka z gryfem na stojaku, stojak hantli, maty; wszystko z kodu, × k
        var cz = new BraxRegionMeble.Bud(); var st = new BraxRegionMeble.Bud(); var ek = new BraxRegionMeble.Bud();
        for (int k = 0; k < 4; k++)
        {
            var p = new Vector3(ix0 + 2f + k * 1.6f * K, y0, fz1 - 1.6f * K);
            cz.MinMax(p + new Vector3(-.4f * K, 0, -1.0f * K), p + new Vector3(.4f * K, .22f * K, 1.0f * K));       // pas bieżni
            st.Cyl(p + new Vector3(-.38f * K, .2f * K, -.9f * K), p + new Vector3(-.38f * K, 1.25f * K, -.95f * K), .03f * K, 8);
            st.Cyl(p + new Vector3(.38f * K, .2f * K, -.9f * K), p + new Vector3(.38f * K, 1.25f * K, -.95f * K), .03f * K, 8);
            ek.MinMax(p + new Vector3(-.35f * K, 1.25f * K, -1.05f * K), p + new Vector3(.35f * K, 1.5f * K, -.9f * K));   // konsola
            BraxRegionMeble.Kolizja(F, "Bieżnia kolizja", p + new Vector3(0, .5f * K, 0), new Vector3(.85f * K, 1.0f * K, 2.0f * K));
        }
        // ławka do wyciskania ze stojakiem i gryfem z talerzami
        var lb = new Vector3(ix0 + 2.5f + 4 * 1.6f * K + 2f, y0, (fz0 + fz1) * .5f);
        cz.MinMax(lb + new Vector3(-.17f * K, .4f * K, -.6f * K), lb + new Vector3(.17f * K, .47f * K, .6f * K));
        st.MinMax(lb + new Vector3(-.05f * K, 0, -.5f * K), lb + new Vector3(.05f * K, .4f * K, .5f * K));
        foreach (float sx in new[] { -.55f, .55f }) st.Cyl(lb + new Vector3(sx * K, 0, -.55f * K), lb + new Vector3(sx * K, 1.2f * K, -.55f * K), .03f * K, 8);
        st.Cyl(lb + new Vector3(-1.1f * K, 1.1f * K, -.55f * K), lb + new Vector3(1.1f * K, 1.1f * K, -.55f * K), .015f * K, 8);
        foreach (float sx in new[] { -.85f, .85f }) cz.Cyl(lb + new Vector3(sx * K - .03f, 1.1f * K, -.55f * K), lb + new Vector3(sx * K + .03f, 1.1f * K, -.55f * K), .225f * K, 18);
        BraxRegionMeble.Kolizja(F, "Ławka kolizja", lb + new Vector3(0, .3f * K, 0), new Vector3(.4f * K, .5f * K, 1.25f * K));
        // stojak z hantlami
        var hp = new Vector3(ix1 - 3f, y0, fz1 - .8f * K);
        st.MinMax(hp + new Vector3(-1.2f * K, .6f * K, -.25f * K), hp + new Vector3(1.2f * K, .64f * K, .25f * K));
        for (int k = 0; k < 6; k++) { var c = hp + new Vector3(-1f * K + k * .4f * K, .72f * K, 0); cz.Cyl(c - Vector3.forward * .15f * K, c + Vector3.forward * .15f * K, (.05f + k * .008f) * K, 10); }
        foreach (float sx in new[] { -1.1f, 1.1f }) st.MinMax(hp + new Vector3(sx * K - .04f, 0, -.2f * K), hp + new Vector3(sx * K + .04f, .6f * K, .2f * K));
        BraxRegionMeble.Kolizja(F, "Stojak kolizja", hp + new Vector3(0, .4f * K, 0), new Vector3(2.4f * K, .8f * K, .5f * K));
        cz.Obiekt("Fitness: guma i talerze (czarne)", F, czarny, false, true); st.Obiekt("Fitness: stal chrom", F, stalNierdz, false, true); ek.Obiekt("Fitness: konsole", F, BraxRegionMeble.Swiecacy("Fitness: ekran konsoli", new Color(.2f, .55f, .9f), .7f), false, false);
        BraxRegionMeble.Napis(F, "PARK TOWER FITNESS", new Vector3(d.szyb.x, Y(2) + 2.4f * K, d.szyb.y + KabD * .5f + .62f), Vector3.forward, .22f * K, new Color(.95f, .95f, .93f));
        punkty.Add(new BraxCityPoint { id = "R1_" + d.idDrzwi + "_FITNESS", label = d.nazwa + " — klub fitness", position = lb });
    }

    // =====================================================================================================================
    // BRAXHORN TOWER przy ulicy głównej (dyspozytor 16:00: Tower przechodzi do B). Elewacja zostaje (przebudowa tylko wejścia,
    // parteru i jednego pasa szyb na piętrze użytkowym). ZDJĘTE: „R1_TOWER accessible premium lobby lift and roof” (lobby-atrapa,
    // pełne bryły „Occupied floors”, stara winda BraxCityElevator, płyty tarasu), „CLAUDE lobby i wejście wieżowca”, „CLAUDE portale
    // windy premium”, „CLAUDE taras widokowy wieżowca”, „CLAUDE taras: kwiaty, rabaty i pergole”.
    // POŁOŻONE: lobby (posadzka kamienna, ściany, sufit z LED, recepcja, kanapy, rośliny, tablica pięter), drzwi automatyczne w
    // otworze elewacji (bez progu), winda BraxWinda (37 pięter na panelu, aktywne: 0 HOL, 20 APARTAMENT, 36 TARAS), apartament
    // pokazowy na 20. piętrze (salon, jadalnia, kuchnia z wyspą, sypialnia, łazienka, szyby przezroczyste), TARAS NA DACHU
    // (deski, balustrada szklana, leżaki, stoliki, donice z zielenią, pergola, luneta, kiosk windy).
    public static void TowerQuay(BraxCityWorld world, Transform cityRoot)
    {
        swiat = world; punkty.Clear();
        if (!granit) Materialy();
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList())
            if (t && (t.name == "R1_TOWER accessible premium lobby lift and roof" || t.name == "CLAUDE lobby i wejście wieżowca" || t.name == "CLAUDE portale windy premium" ||
                      t.name == "CLAUDE taras widokowy wieżowca" || t.name == "CLAUDE taras: kwiaty, rabaty i pergole"))
            { usuniete.Add("Tower: " + t.name + " (" + t.GetComponentsInChildren<Renderer>(true).Length + " rendererów)"); UnityEngine.Object.DestroyImmediate(t.gameObject); }
        if (world) { world.elevator = null; world.doors = world.doors.Where(d => d).ToArray(); }
        var T = BraxRegion.Root(cityRoot, "CLAUDE Tower B: lobby, winda, apartament, taras");
        var prev = root; root = T;
        try
        {
            const float x0 = 52f, x1 = 66f, z0 = 36.1f, z1 = 60f, cx = 59f, sz = 53f;
            const float hx0 = 52.4f, hx1 = 65.6f, hz0 = 36.55f, hz1 = 59.55f, baseH = 33.3f, FH = 3.35f;
            float roofY = baseH + 26 * FH;
            // ---------- piętra (37 przycisków): 0 parter, 1–9 trzon kamienicy (3.2 m), 10–35 wieżowiec (3.35 m), 36 dach
            int n = 36; var pY = new float[n + 1]; var uz = new bool[n + 1]; var nz = new string[n + 1];
            for (int i = 0; i <= n; i++) { pY[i] = i == 0 ? YC : i <= 9 ? 3.75f + (i - 1) * 3.2f : i <= 35 ? baseH + (i - 10) * FH : roofY; nz[i] = i.ToString(); }
            uz[0] = uz[20] = uz[36] = true; nz[0] = "0 HOL"; nz[20] = "20 APARTAMENT"; nz[36] = "36 TARAS";
            // ---------- LOBBY
            float yl = YC, hl = 3.75f - .35f;
            Plyta("Tower: posadzka lobby (kamień)", kamienPos, x0 + .3f, x1 - .3f, z0 + .25f, z1 - .25f, yl, .3f);
            var w = new BraxRegionMeble.Bud();
            w.MinMax(new Vector3(x0 + .3f, yl, z0 + .25f), new Vector3(x0 + .55f, hl, z1 - .25f));
            w.MinMax(new Vector3(x1 - .55f, yl, z0 + .25f), new Vector3(x1 - .3f, hl, z1 - .25f));
            w.MinMax(new Vector3(x0 + .55f, yl, z1 - .5f), new Vector3(x1 - .55f, hl, z1 - .25f));
            float dw = 2.12f;   // otwór drzwi w elewacji (źródło) — 2.12 m (≥ 1.2 × k), wys. 2.86 m (≥ 2.6 × k)
            w.MinMax(new Vector3(x0 + .55f, yl, z0 + .25f), new Vector3(cx - dw * .5f, hl, z0 + .5f));
            w.MinMax(new Vector3(cx + dw * .5f, yl, z0 + .25f), new Vector3(x1 - .55f, hl, z0 + .5f));
            w.MinMax(new Vector3(cx - dw * .5f, yl + 2.9f, z0 + .25f), new Vector3(cx + dw * .5f, hl, z0 + .5f));
            w.Obiekt("Tower: ściany lobby (tynk)", T, tynk, false, true);
            foreach (var (a, b, c, d) in new[] { (x0 + .3f, x0 + .55f, z0 + .25f, z1 - .25f), (x1 - .55f, x1 - .3f, z0 + .25f, z1 - .25f), (x0 + .55f, x1 - .55f, z1 - .5f, z1 - .25f),
                                                 (x0 + .55f, cx - dw * .5f, z0 + .25f, z0 + .5f), (cx + dw * .5f, x1 - .55f, z0 + .25f, z0 + .5f) })
                BraxRegionMeble.Kolizja(T, "Tower: kolizja ściany lobby", new Vector3((a + b) * .5f, (yl + hl) * .5f, (c + d) * .5f), new Vector3(b - a, hl - yl, d - c));
            var su = new BraxRegionMeble.Bud(); su.MinMax(new Vector3(x0 + .55f, hl, z0 + .5f), new Vector3(x1 - .55f, hl + .05f, z1 - .5f)); su.Obiekt("Tower: sufit lobby", T, sufit, false, false);
            var le = new BraxRegionMeble.Bud(); for (float x = x0 + 2f; x < x1 - 1.5f; x += 3.2f) for (float z = z0 + 2f; z < z1 - 1.5f; z += 3.6f) le.MinMax(new Vector3(x, hl - .015f, z), new Vector3(x + .6f * K, hl - .002f, z + .6f * K));
            le.Obiekt("Tower: panele LED lobby", T, ledM, false, false);
            BraxRegionMeble.Kolizja(T, "Tower: sufit kolizja", new Vector3(cx, hl + .15f, (z0 + z1) * .5f), new Vector3(x1 - x0 - 1f, .3f, z1 - z0 - 1f));
            BraxRegion.Warm(T, "Tower: światło lobby (1)", new Vector3(cx, hl - .4f, z0 + 6f), 1.4f, 12f); BraxRegion.Warm(T, "Tower: światło lobby (2)", new Vector3(cx, hl - .4f, z0 + 14f), 1.4f, 12f);
            // drzwi automatyczne w otworze elewacji (skrzydła 1.06 m, wys. 2.86 m)
            var h = new GameObject("Tower: drzwi automatyczne").transform; h.SetParent(T, false); h.position = new Vector3(cx, yl, z0 + .375f);
            Transform Skr(string nn, float off)
            {
                var t = new GameObject(nn).transform; t.SetParent(h, false); t.localPosition = new Vector3(off, 0, 0);
                var g = new BraxRegionMeble.Bud(); var r = new BraxRegionMeble.Bud();
                g.MinMax(t.position + new Vector3(-dw * .25f + .04f, .05f, -.01f), t.position + new Vector3(dw * .25f - .04f, 2.8f, .01f));
                r.MinMax(t.position + new Vector3(-dw * .25f, 0f, -.025f), t.position + new Vector3(dw * .25f, .06f, .025f));
                r.MinMax(t.position + new Vector3(-dw * .25f, 2.8f, -.025f), t.position + new Vector3(dw * .25f, 2.86f, .025f));
                r.MinMax(t.position + new Vector3(-dw * .25f, 0f, -.025f), t.position + new Vector3(-dw * .25f + .05f, 2.86f, .025f));
                r.MinMax(t.position + new Vector3(dw * .25f - .05f, 0f, -.025f), t.position + new Vector3(dw * .25f, 2.86f, .025f));
                var a1 = g.Obiekt(nn + ": szkło", t, szkloCiemne, false, false); var a2 = r.Obiekt(nn + ": rama", t, alu, false, true);
                foreach (var go in new[] { a1, a2 }) if (go) GameObjectUtility.SetStaticEditorFlags(go, 0);
                var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0, 1.43f, 0); col.size = new Vector3(dw * .5f, 2.86f, .08f);
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); return t;
            }
            var dr = h.gameObject.AddComponent<BraxDrzwiPrzesuwne>();
            dr.skrzydloL = Skr("Skrzydło lewe", -dw * .25f); dr.skrzydloP = Skr("Skrzydło prawe", dw * .25f); dr.przesuw = dw * .5f - .04f; dr.zasieg = 2.8f; dr.nazwa = "BRAXHORN TOWER";
            // wyposażenie lobby: recepcja (zach.), poczekalnia (wsch.), tablica pięter przy windzie, rośliny
            var lada = new BraxRegionMeble.Bud(); var blat = new BraxRegionMeble.Bud();
            lada.MinMax(new Vector3(x0 + 1.2f, yl, z0 + 6f), new Vector3(x0 + 1.2f + .7f * K, yl + 1.05f * K, z0 + 6f + 3.4f * K));
            blat.MinMax(new Vector3(x0 + 1.15f, yl + 1.05f * K, z0 + 5.95f), new Vector3(x0 + 1.25f + .7f * K, yl + 1.1f * K, z0 + 6.05f + 3.4f * K));
            lada.Obiekt("Tower: lada recepcji (dąb)", T, drewno, false, true); blat.Obiekt("Tower: blat recepcji (kamień)", T, kamienPos, false, true);
            BraxRegionMeble.Kolizja(T, "Tower: recepcja kolizja", new Vector3(x0 + 1.2f + .35f * K, yl + .55f * K, z0 + 6f + 1.7f * K), new Vector3(.7f * K, 1.1f * K, 3.4f * K));
            BraxRegionMeble.KrzesloBiurowe(T, new Vector3(x0 + .95f, yl, z0 + 6f + 1.7f * K), 90f);
            BraxRegionMeble.Napis(T, "BRAXHORN TOWER", new Vector3(x0 + .58f, yl + 2.6f, z0 + 8.2f), Vector3.right, .22f * K, new Color(.2f, .2f, .22f));
            Model(T, "Tower: sofa", new Vector3(x1 - 1.6f, yl, z0 + 7f), -90f, .82f, true, "sofa_02", "Sofa_01");
            Model(T, "Tower: fotel", new Vector3(x1 - 3.6f, yl, z0 + 5.2f), -30f, .9f, true, "modern_arm_chair_01", "mid_century_lounge_chair");
            Model(T, "Tower: stolik", new Vector3(x1 - 3.2f, yl, z0 + 7f), 0f, .42f, true, "modern_coffee_table_01");
            foreach (var p in new[] { new Vector3(x0 + 1.2f, yl, z0 + 1.3f), new Vector3(x1 - 1.2f, yl, z0 + 1.3f), new Vector3(x1 - 1.2f, yl, z0 + 11f) })
                Model(T, "Tower: roślina w donicy", p, p.x * 13f, 1.7f, true, "potted_plant_04", "potted_plant_02", "pachira_aquatica_01");
            var tab = new BraxRegionMeble.Bud(); tab.MinMax(new Vector3(cx + 2.4f, yl + 1.2f * K, sz - 1.4f - .3f), new Vector3(cx + 3.6f, yl + 2.4f * K, sz - 1.4f - .25f));
            tab.Obiekt("Tower: tablica pięter", T, czarny, false, false);
            BraxRegionMeble.Napis(T, "BRAXHORN TOWER", new Vector3(cx + 3f, yl + 2.25f * K, sz - 1.72f), Vector3.back, .08f * K, Color.white);
            BraxRegionMeble.Napis(T, "36  TARAS WIDOKOWY", new Vector3(cx + 3f, yl + 2.0f * K, sz - 1.72f), Vector3.back, .065f * K, new Color(.9f, .9f, .85f));
            BraxRegionMeble.Napis(T, "20  APARTAMENT POKAZOWY", new Vector3(cx + 3f, yl + 1.83f * K, sz - 1.72f), Vector3.back, .065f * K, new Color(.9f, .9f, .85f));
            BraxRegionMeble.Napis(T, "0  HOL · RECEPCJA", new Vector3(cx + 3f, yl + 1.66f * K, sz - 1.72f), Vector3.back, .065f * K, new Color(.9f, .9f, .85f));
            // ---------- WINDA (drzwi od strony lobby: −z)
            Winda(T, "BRAXHORN TOWER", new Vector2(cx, sz), pY, uz, nz, roofY + 3.6f, -1f);
            // ---------- APARTAMENT (20. piętro, y = pY[20])
            Apartament(T, pY[20], FH, hx0, hx1, hz0, hz1, cx, sz);
            // ---------- TARAS NA DACHU
            Taras(T, roofY, hx0, hx1, hz0, hz1, cx, sz);
            punkty.Add(new BraxCityPoint { id = "R1_TOWER_APARTAMENT", label = "BRAXHORN TOWER — apartament pokazowy (20. piętro)", position = new Vector3(cx - 3f, pY[20], 46f) });
            punkty.Add(new BraxCityPoint { id = "R1_TOWER_TARAS", label = "BRAXHORN TOWER — taras widokowy", position = new Vector3(cx, roofY, 42f) });
            ZapiszPunkty();
            Debug.Log("BRAXHORN_MIASTO_B_TOWER lobby + drzwi automatyczne + winda (37 pięter, użytkowe: 0, 20, 36) + apartament y=" + pY[20].ToString("0.0") + " + taras y=" + roofY.ToString("0.0") + "; zdjęte: " + string.Join(" | ", usuniete.Where(u => u.StartsWith("Tower"))));
        }
        finally { root = prev; }
    }

    static void Apartament(Transform T, float y, float FH, float hx0, float hx1, float hz0, float hz1, float cx, float sz)
    {
        var A = new GameObject("Tower: apartament pokazowy (20. piętro)").transform; A.SetParent(T, false);
        // pas elewacji tego piętra wycięty z bryły wieżowca (szyby nieprzezroczyste) i zastąpiony szkłem przezroczystym z lizenami
        int cut = 0;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t && t.name == "High-rise quay observation tower")
                cut += BraxCityMeshCut.Subtract(t, Bx(hx0 - .4f, hx1 + .4f, y + .015f, y + FH - .015f, hz0 - .4f, hz1 + .4f), BraxRegion.Out + "/Cuts", "tower_apartament");
        var gl = new BraxRegionMeble.Bud(); var fr = new BraxRegionMeble.Bud(); var sp = new BraxRegionMeble.Bud();
        float g0 = y + .56f, g1 = y + FH - .1f;
        foreach (var (a, b, c, wzdX) in new[] { (hx0, hx1, hz1, true), (hx0, hx1, hz0, true), (hz0, hz1, hx0, false), (hz0, hz1, hx1, false) })
        {
            Vector3 P(float u, float yy, float d) => wzdX ? new Vector3(u, yy, c + d) : new Vector3(c + d, yy, u);
            void Bx2(BraxRegionMeble.Bud bb, float u0, float u1, float yy0, float yy1, float d0, float d1) { var p0 = P(u0, yy0, d0); var p1 = P(u1, yy1, d1); bb.MinMax(Vector3.Min(p0, p1), Vector3.Max(p0, p1)); }
            float sign = (wzdX ? c > (hz0 + hz1) * .5f : c > (hx0 + hx1) * .5f) ? 1f : -1f;
            float m = wzdX ? 0f : .25f;   // boki ±x krótsze o warstwę stron ±z (narożniki bez nakładania)
            Bx2(gl, a + m, b - m, g0, g1, -sign * .09f, -sign * .07f);
            Bx2(sp, a + m, b - m, y + .015f, g0, -sign * .2f, -sign * .02f);
            Bx2(sp, a + m, b - m, g1, y + FH - .015f, -sign * .2f, -sign * .02f);
            int bays = Mathf.Max(2, Mathf.RoundToInt((b - a) / 1.65f));
            for (int i = 0; i <= bays; i++) { float u = Mathf.Lerp(a, b, i / (float)bays); Bx2(fr, Mathf.Max(a, u - .045f), Mathf.Min(b, u + .045f), y + .015f, y + FH - .015f, -sign * .2f, sign * .02f); }
            var mid = P((a + b) * .5f, (g0 + g1) * .5f, -sign * .1f);
            BraxRegionMeble.Kolizja(A, "Apartament: kolizja szyby", mid, wzdX ? new Vector3(b - a, g1 - g0, .12f) : new Vector3(.12f, g1 - g0, b - a));
        }
        gl.Obiekt("Apartament: szyby przezroczyste", A, szklo, false, false); fr.Obiekt("Apartament: słupki elewacji", A, alu, false, true); sp.Obiekt("Apartament: pasy stropowe", A, panel, false, true);
        // posadzka (deska dębowa, szorstkość .6), sufit z LED, otwór na szyb
        float sx0 = cx - KabW * .5f - .35f, sx1 = cx + KabW * .5f + .35f, sz0 = sz - KabD * .5f - .35f, sz1 = sz + KabD * .5f + .35f;
        float ix0 = hx0 + .2f, ix1 = hx1 - .2f, iz0 = hz0 + .2f, iz1 = hz1 - .2f;
        var p = new BraxRegionMeble.Bud();
        p.MinMax(new Vector3(ix0, y - .3f, iz0), new Vector3(ix1, y, sz0)); p.MinMax(new Vector3(ix0, y - .3f, sz1), new Vector3(ix1, y, iz1));
        p.MinMax(new Vector3(ix0, y - .3f, sz0), new Vector3(sx0, y, sz1)); p.MinMax(new Vector3(sx1, y - .3f, sz0), new Vector3(ix1, y, sz1));
        p.Obiekt("Apartament: podłoga (dąb)", A, drewno, false, false);
        BraxRegionMeble.Kolizja(A, "Apartament: podłoga kolizja", new Vector3((ix0 + ix1) * .5f, y - .15f, (iz0 + iz1) * .5f), new Vector3(ix1 - ix0, .3f, iz1 - iz0));
        float yc = y + FH - .45f;
        var s = new BraxRegionMeble.Bud(); s.MinMax(new Vector3(ix0, yc, iz0), new Vector3(ix1, yc + .04f, iz1)); s.Obiekt("Apartament: sufit", A, sufit, false, false);
        BraxRegionMeble.Kolizja(A, "Apartament: sufit kolizja", new Vector3((ix0 + ix1) * .5f, yc + .15f, (iz0 + iz1) * .5f), new Vector3(ix1 - ix0, .3f, iz1 - iz0));
        BraxRegion.Warm(A, "Apartament: światło salonu", new Vector3(cx - 2f, yc - .4f, 42f), 1.3f, 12f); BraxRegion.Warm(A, "Apartament: światło sypialni", new Vector3(cx - 2f, yc - .4f, 57f), 1.1f, 9f);
        // ściana działowa z = 55.3 (sypialnia zach., łazienka wsch.) z drzwiami, ściana x = 60.6 między nimi
        float zw = 55.3f, xw = 60.6f, dW = 1.0f * K, dH = 2.1f * K;
        var sc = new BraxRegionMeble.Bud();
        void Seg(float a, float b) { if (b - a > .02f) { sc.MinMax(new Vector3(a, y, zw - .07f), new Vector3(b, yc, zw + .07f)); BraxRegionMeble.Kolizja(A, "Ściana kolizja", new Vector3((a + b) * .5f, (y + yc) * .5f, zw), new Vector3(b - a, yc - y, .14f)); } }
        float d1 = 55.0f, d2 = 63.6f;
        Seg(ix0, d1 - dW * .5f); Seg(d1 + dW * .5f, sx0 - .05f); Seg(sx1 + .05f, d2 - dW * .5f); Seg(d2 + dW * .5f, ix1);
        sc.MinMax(new Vector3(d1 - dW * .5f, y + dH, zw - .07f), new Vector3(d1 + dW * .5f, yc, zw + .07f)); sc.MinMax(new Vector3(d2 - dW * .5f, y + dH, zw - .07f), new Vector3(d2 + dW * .5f, yc, zw + .07f));
        sc.MinMax(new Vector3(xw - .07f, y, zw + .07f), new Vector3(xw + .07f, yc, iz1)); BraxRegionMeble.Kolizja(A, "Ściana kolizja", new Vector3(xw, (y + yc) * .5f, (zw + iz1) * .5f), new Vector3(.14f, yc - y, iz1 - zw));
        sc.Obiekt("Apartament: ściany działowe", A, tynk, false, true);
        Drzwi(A, "TOWER_APT_SYPIALNIA", "SYPIALNIA", new Vector3(d1, y, zw), dW, dH);
        Drzwi(A, "TOWER_APT_LAZIENKA", "ŁAZIENKA", new Vector3(d2, y, zw), dW, dH);
        // salon (zach.): sofa, fotel, stolik, szafka RTV z telewizorem, dywan (2.5 cm nad podłogą — nie w jej płaszczyźnie)
        Model(A, "Salon: sofa", new Vector3(ix0 + 1.3f, y, 42.5f), 90f, .82f, true, "sofa_02", "Sofa_01");
        Model(A, "Salon: fotel", new Vector3(ix0 + 3.6f, y, 39.2f), 20f, .9f, true, "mid_century_lounge_chair", "modern_arm_chair_01");
        Model(A, "Salon: stolik", new Vector3(ix0 + 3.3f, y, 42.5f), 0f, .42f, true, "modern_coffee_table_01");
        var dyw = new BraxRegionMeble.Bud(); dyw.MinMax(new Vector3(ix0 + 2.0f, y, 40.3f), new Vector3(ix0 + 4.8f, y + .025f, 44.7f));
        dyw.Obiekt("Salon: dywan wełniany", A, BraxCityMaterials.Surface("Apartament: dywan (wełna szaro-beżowa)", new Color(.62f, .58f, .52f), .98f), false, false);
        var rtv = new BraxRegionMeble.Bud(); rtv.MinMax(new Vector3(ix0 + 5.6f, y, 41.2f), new Vector3(ix0 + 6.1f, y + .5f * K, 43.8f));
        rtv.Obiekt("Salon: szafka RTV", A, drewno, false, true);
        var tv = new BraxRegionMeble.Bud(); tv.MinMax(new Vector3(ix0 + 5.95f, y + .75f * K, 41.4f), new Vector3(ix0 + 6.0f, y + 1.45f * K, 43.6f));
        tv.Obiekt("Salon: telewizor", A, czarny, false, false);
        BraxRegionMeble.Kolizja(A, "RTV kolizja", new Vector3(ix0 + 5.85f, y + .3f * K, 42.5f), new Vector3(.5f, .6f * K, 2.6f));
        // jadalnia (wsch.): stół 1.8 × 0.9 m × k, 6 krzeseł
        var st = new BraxRegionMeble.Bud(); float tx = 62.6f, tz = 40.5f;
        st.MinMax(new Vector3(tx - .45f * K, y + .73f * K, tz - .9f * K), new Vector3(tx + .45f * K, y + .76f * K, tz + .9f * K));
        foreach (float dz in new[] { -.75f, .75f }) st.MinMax(new Vector3(tx - .3f * K, y, tz + dz * K - .04f), new Vector3(tx + .3f * K, y + .73f * K, tz + dz * K + .04f));
        st.Obiekt("Jadalnia: stół (dąb)", A, drewno, false, true);
        BraxRegionMeble.Kolizja(A, "Stół kolizja", new Vector3(tx, y + .38f * K, tz), new Vector3(.9f * K, .76f * K, 1.8f * K));
        var kd = new BraxRegionMeble.Bud(); var ks = new BraxRegionMeble.Bud();
        foreach (float dz in new[] { -.55f, 0f, .55f }) { BraxRegionMeble.Krzeslo(A, kd, ks, new Vector3(tx - .75f * K, y, tz + dz * K), 90f); BraxRegionMeble.Krzeslo(A, kd, ks, new Vector3(tx + .75f * K, y, tz + dz * K), -90f); }
        kd.Obiekt("Jadalnia: krzesła (drewno)", A, drewno, false, true); ks.Obiekt("Jadalnia: krzesła (stal)", A, czarny, false, true);
        // kuchnia: zabudowa przy ścianie wsch. + wyspa (kamień), okap
        var zab = new BraxRegionMeble.Bud(); var top = new BraxRegionMeble.Bud();
        zab.MinMax(new Vector3(ix1 - .62f * K, y, 44.5f), new Vector3(ix1, y + .88f * K, 50.5f)); top.MinMax(new Vector3(ix1 - .65f * K, y + .88f * K, 44.45f), new Vector3(ix1, y + .92f * K, 50.55f));
        zab.MinMax(new Vector3(61.2f, y, 46f), new Vector3(63.6f, y + .88f * K, 47.2f)); top.MinMax(new Vector3(61.1f, y + .88f * K, 45.9f), new Vector3(63.7f, y + .92f * K, 47.3f));
        zab.Obiekt("Kuchnia: zabudowa (fronty grafit)", A, BraxCityMaterials.Surface("Kuchnia: fronty lakier grafit mat", new Color(.18f, .19f, .2f), .55f, .1f), false, true);
        top.Obiekt("Kuchnia: blaty (kamień)", A, kamienPos, false, true);
        BraxRegionMeble.Kolizja(A, "Kuchnia kolizja", new Vector3(ix1 - .31f * K, y + .45f * K, 47.5f), new Vector3(.62f * K, .92f * K, 6f));
        BraxRegionMeble.Kolizja(A, "Wyspa kolizja", new Vector3(62.4f, y + .45f * K, 46.6f), new Vector3(2.6f, .92f * K, 1.4f));
        // sypialnia: łóżko 1.6 × 2.0 m × k (rama, materac, kołdra, poduszki), szafki nocne, szafa
        float bx = xw - 1.0f * K - .15f, bz = 57.4f;
        var lo = new BraxRegionMeble.Bud(); var ma = new BraxRegionMeble.Bud(); var ko = new BraxRegionMeble.Bud();
        lo.MinMax(new Vector3(bx - 1.0f * K, y, bz - .85f * K), new Vector3(bx + 1.0f * K, y + .3f * K, bz + .85f * K));
        lo.MinMax(new Vector3(bx + .95f * K, y, bz - .9f * K), new Vector3(bx + 1.05f * K, y + 1.1f * K, bz + .9f * K));   // wezgłowie
        ma.MinMax(new Vector3(bx - .98f * K, y + .3f * K, bz - .8f * K), new Vector3(bx + .95f * K, y + .52f * K, bz + .8f * K));
        ko.MinMax(new Vector3(bx - 1.0f * K, y + .52f * K, bz - .82f * K), new Vector3(bx + .45f * K, y + .58f * K, bz + .82f * K));
        foreach (float dz in new[] { -.4f, .4f }) ko.MinMax(new Vector3(bx + .5f * K, y + .52f * K, bz + dz * K - .3f * K), new Vector3(bx + .9f * K, y + .66f * K, bz + dz * K + .3f * K));
        lo.Obiekt("Sypialnia: łóżko (rama dąb)", A, drewno, false, true); ma.Obiekt("Sypialnia: materac", A, sufit, false, true);
        ko.Obiekt("Sypialnia: pościel", A, BraxCityMaterials.Surface("Sypialnia: pościel lniana", new Color(.86f, .84f, .80f), .95f), false, true);
        BraxRegionMeble.Kolizja(A, "Łóżko kolizja", new Vector3(bx, y + .3f * K, bz), new Vector3(2.1f * K, .6f * K, 1.8f * K));
        var lozkoSiedz = new GameObject("Siedzisko: łóżko"); lozkoSiedz.transform.SetParent(A, false); lozkoSiedz.transform.position = new Vector3(bx, y + .58f * K, bz);
        lozkoSiedz.transform.rotation = Quaternion.LookRotation(Vector3.left); var ls = lozkoSiedz.AddComponent<BraxSeat>(); ls.lie = true; ls.label = "ŁÓŻKO"; ls.surfaceY = y + .58f * K;
        var sza = new BraxRegionMeble.Bud(); sza.MinMax(new Vector3(ix0, y, 56.0f), new Vector3(ix0 + .62f * K, y + 2.3f * K, 58.8f)); sza.Obiekt("Sypialnia: szafa", A, drewno, false, true);
        BraxRegionMeble.Kolizja(A, "Szafa kolizja", new Vector3(ix0 + .31f * K, y + 1.15f * K, 57.4f), new Vector3(.62f * K, 2.3f * K, 2.8f));
        // łazienka: WC, umywalka, prysznic (brodzik + szkło)
        var bia = BraxCityMaterials.Surface("Łazienka: ceramika biała", new Color(.95f, .95f, .94f), .2f);
        var lz = new BraxRegionMeble.Bud(); var wc = new Vector3(ix1 - .5f * K, y, 56.2f);
        lz.Cyl(wc, wc + Vector3.up * .4f * K, .2f * K, 16, .17f * K); lz.MinMax(wc + new Vector3(.15f * K, .4f * K, -.2f * K), wc + new Vector3(.32f * K, .85f * K, .2f * K));
        lz.MinMax(new Vector3(ix1 - .5f * K, y + .8f * K, 58.2f), new Vector3(ix1, y + .9f * K, 59.0f));
        lz.MinMax(new Vector3(xw + .2f, y, 57.6f), new Vector3(xw + .2f + 1.0f * K, y + .05f, iz1));   // brodzik (5 cm)
        lz.Obiekt("Łazienka: ceramika", A, bia, false, true);
        var ps = new BraxRegionMeble.Bud(); ps.MinMax(new Vector3(xw + .2f + 1.0f * K, y + .05f, 57.6f), new Vector3(xw + .22f + 1.0f * K, y + 2.0f * K, iz1)); ps.Obiekt("Łazienka: szkło prysznica", A, szklo, false, false);
        BraxRegionMeble.Kolizja(A, "WC kolizja", wc + Vector3.up * .4f * K, new Vector3(.75f * K, .85f * K, .45f * K));
        BraxRegionMeble.Kolizja(A, "Prysznic kolizja", new Vector3(xw + .21f + 1.0f * K, y + 1.0f * K, (57.6f + iz1) * .5f), new Vector3(.05f, 2.0f * K, iz1 - 57.6f));
        foreach (var pp in new[] { new Vector3(ix0 + .8f, y, 37.4f), new Vector3(ix1 - .8f, y, 37.4f), new Vector3(sx0 - .8f, y, sz0 - .9f) })
            Model(A, "Apartament: roślina", pp, pp.x * 7f, 1.6f, true, "potted_plant_04", "pachira_aquatica_01", "potted_plant_02");
        BraxRegion.Note("Tower: apartament — pas elewacji wycięty z " + cut + " siatek, szkło przezroczyste, wnętrze: salon, jadalnia, kuchnia, sypialnia, łazienka");
    }

    static void Taras(Transform T, float y, float hx0, float hx1, float hz0, float hz1, float cx, float sz)
    {
        var R = new GameObject("Tower: taras widokowy na dachu").transform; R.SetParent(T, false);
        var deska = BraxRegionMeble.Tex("Taras: deska tarasowa", 1.5f, new Color(.80f, .70f, .58f), .75f, "wood_floor_deck", "weathered_planks", "wood_planks");
        float sx0 = cx - KabW * .5f - .35f, sx1 = cx + KabW * .5f + .35f, sz0 = sz - KabD * .5f - .35f, sz1 = sz + KabD * .5f + .35f;
        var p = new BraxRegionMeble.Bud();
        p.MinMax(new Vector3(hx0, y - .3f, hz0), new Vector3(hx1, y, sz0)); p.MinMax(new Vector3(hx0, y - .3f, sz1), new Vector3(hx1, y, hz1));
        p.MinMax(new Vector3(hx0, y - .3f, sz0), new Vector3(sx0, y, sz1)); p.MinMax(new Vector3(sx1, y - .3f, sz0), new Vector3(hx1, y, sz1));
        p.Obiekt("Taras: deski", R, deska, false, false);
        BraxRegionMeble.Kolizja(R, "Taras: kolizja", new Vector3((hx0 + hx1) * .5f, y - .15f, (hz0 + hz1) * .5f), new Vector3(hx1 - hx0, .3f, hz1 - hz0));
        // kiosk windy: daszek nad szybem (ściany szybu do y + 3.6 m)
        var ki = new BraxRegionMeble.Bud(); ki.MinMax(new Vector3(sx0 - .4f, y + 3.6f, sz0 - 1.4f), new Vector3(sx1 + .4f, y + 3.85f, sz1 + .4f)); ki.Obiekt("Taras: dach kiosku windy", R, alu, false, true);
        // balustrada szklana 1.1 m × k (słupki co 1.5 m, szyby, pochwyt) z kolizją 1.5 m
        float e = .3f, hb = 1.1f * K; var sl = new BraxRegionMeble.Bud(); var sk = new BraxRegionMeble.Bud();
        foreach (var (a, b) in new[] { (new Vector2(hx0 + e, hz0 + e), new Vector2(hx1 - e, hz0 + e)), (new Vector2(hx1 - e, hz0 + e), new Vector2(hx1 - e, hz1 - e)), (new Vector2(hx1 - e, hz1 - e), new Vector2(hx0 + e, hz1 - e)), (new Vector2(hx0 + e, hz1 - e), new Vector2(hx0 + e, hz0 + e)) })
        {
            var A3 = new Vector3(a.x, y, a.y); var B3 = new Vector3(b.x, y, b.y); float L = Vector3.Distance(A3, B3); int nn = Mathf.Max(1, Mathf.RoundToInt(L / 1.5f));
            for (int i = 0; i <= nn; i++) { var q = Vector3.Lerp(A3, B3, i / (float)nn); sl.Cyl(q, q + Vector3.up * hb, .03f * K, 10); }
            sl.Cyl(A3 + Vector3.up * hb, B3 + Vector3.up * hb, .035f * K, 10);
            bool wzdX = Mathf.Abs(a.y - b.y) < .01f;
            if (wzdX) sk.MinMax(new Vector3(Mathf.Min(a.x, b.x), y + .08f, a.y - .008f), new Vector3(Mathf.Max(a.x, b.x), y + hb - .06f, a.y + .008f));
            else sk.MinMax(new Vector3(a.x - .008f, y + .08f, Mathf.Min(a.y, b.y)), new Vector3(a.x + .008f, y + hb - .06f, Mathf.Max(a.y, b.y)));
            BraxRegionMeble.Kolizja(R, "Balustrada kolizja", (A3 + B3) * .5f + Vector3.up * .75f, wzdX ? new Vector3(L, 1.5f, .15f) : new Vector3(.15f, 1.5f, L));
        }
        sl.Obiekt("Taras: słupki i pochwyt (stal nierdzewna)", R, stalNierdz, false, true); sk.Obiekt("Taras: balustrada szklana", R, szklo, false, false);
        // leżaki (rama aluminiowa, materac) przodem do morza (+x), stoliki z krzesłami, donice z zielenią, pergola, luneta
        var lez = new BraxRegionMeble.Bud(); var mat = new BraxRegionMeble.Bud();
        for (int i = 0; i < 4; i++)
        {
            var c = new Vector3(hx1 - 2.2f * K, y, hz0 + 4f + i * 2.0f * K);
            lez.MinMax(c + new Vector3(-1.0f * K, .28f * K, -.32f * K), c + new Vector3(.5f * K, .33f * K, .32f * K));
            foreach (float dx in new[] { -.9f, .4f }) foreach (float dz in new[] { -.28f, .28f }) lez.MinMax(c + new Vector3(dx * K, 0, dz * K - .02f), c + new Vector3(dx * K + .05f, .28f * K, dz * K + .02f));
            mat.MinMax(c + new Vector3(-.95f * K, .33f * K, -.3f * K), c + new Vector3(.45f * K, .41f * K, .3f * K));
            mat.Box(c + new Vector3(-1.15f * K, .62f * K, 0), new Vector3(.5f * K, .08f * K, .6f * K), Quaternion.Euler(0, 0, -55f));
            BraxRegionMeble.Kolizja(R, "Leżak kolizja", c + new Vector3(-.3f * K, .3f * K, 0), new Vector3(1.6f * K, .6f * K, .66f * K));
            var lz = new GameObject("Siedzisko: leżak"); lz.transform.SetParent(R, false); lz.transform.position = c + new Vector3(-.2f * K, .41f * K, 0); lz.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var sl2 = lz.AddComponent<BraxSeat>(); sl2.lie = true; sl2.label = "LEŻAK"; sl2.surfaceY = y + .41f * K;
        }
        lez.Obiekt("Taras: leżaki (aluminium)", R, alu, false, true);
        mat.Obiekt("Taras: materace leżaków", R, BraxCityMaterials.Surface("Taras: tkanina outdoor (piaskowa)", new Color(.82f, .76f, .64f), .95f), false, true);
        var bl = new BraxRegionMeble.Bud(); var nl = new BraxRegionMeble.Bud(); var kd = new BraxRegionMeble.Bud(); var ks = new BraxRegionMeble.Bud();
        foreach (var c in new[] { new Vector3(hx0 + 2.4f, y, hz0 + 3.5f), new Vector3(hx0 + 2.4f, y, hz0 + 7.5f), new Vector3(hx0 + 2.4f, y, hz1 - 3.5f) })
        {
            BraxRegionMeble.Stolik(R, bl, nl, c);
            BraxRegionMeble.Krzeslo(R, kd, ks, c + new Vector3(0, 0, -.62f * K), 0f); BraxRegionMeble.Krzeslo(R, kd, ks, c + new Vector3(0, 0, .62f * K), 180f);
        }
        bl.Obiekt("Taras: blaty stolików", R, kamienPos, false, true); nl.Obiekt("Taras: nogi stolików", R, czarny, false, true);
        kd.Obiekt("Taras: krzesła (drewno)", R, drewno, false, true); ks.Obiekt("Taras: krzesła (stal)", R, czarny, false, true);
        var don = new BraxRegionMeble.Bud();
        var miejsca = new[] { new Vector3(hx0 + .9f, y, hz0 + 1.2f), new Vector3(hx1 - .9f, y, hz0 + 1.2f), new Vector3(hx0 + .9f, y, hz1 - 1.2f), new Vector3(hx1 - .9f, y, hz1 - 1.2f), new Vector3(hx0 + .9f, y, (hz0 + hz1) * .5f), new Vector3(hx1 - .9f, y, hz1 - 6f) };
        foreach (var c in miejsca)
        {
            don.MinMax(c + new Vector3(-.45f * K, 0, -.45f * K), c + new Vector3(.45f * K, .55f * K, .45f * K));
            BraxRegionMeble.Kolizja(R, "Donica kolizja", c + Vector3.up * .28f * K, new Vector3(.9f * K, .55f * K, .9f * K));
            Model(R, "Taras: roślina w donicy", c + Vector3.up * .55f * K, c.z * 11f, 1.5f, false, "potted_plant_04", "pachira_aquatica_01", "potted_plant_02");
        }
        don.Obiekt("Taras: donice (kompozyt grafit)", R, BraxCityMaterials.Surface("Taras: donica kompozyt grafit", new Color(.22f, .23f, .24f), .7f, .1f), false, true);
        // pergola nad leżakami (drewno, 4 słupy, belki co 0.6 m)
        var pe = new BraxRegionMeble.Bud(); float px0 = hx1 - 4.2f * K, px1 = hx1 - .8f, pz0 = hz0 + 3f, pz1 = hz0 + 3f + 8.5f * K, ph = 2.6f * K;
        foreach (float xx in new[] { px0, px1 }) foreach (float zz in new[] { pz0, pz1 }) pe.MinMax(new Vector3(xx - .08f * K, y, zz - .08f * K), new Vector3(xx + .08f * K, y + ph, zz + .08f * K));
        foreach (float xx in new[] { px0, px1 }) pe.MinMax(new Vector3(xx - .06f * K, y + ph, pz0 - .3f), new Vector3(xx + .06f * K, y + ph + .2f * K, pz1 + .3f));
        for (float zz = pz0; zz <= pz1 + .01f; zz += .6f * K) pe.MinMax(new Vector3(px0 - .3f, y + ph + .2f * K, zz - .04f * K), new Vector3(px1 + .3f, y + ph + .32f * K, zz + .04f * K));
        pe.Obiekt("Taras: pergola (drewno)", R, drewno, false, true);
        foreach (float xx in new[] { px0, px1 }) foreach (float zz in new[] { pz0, pz1 }) BraxRegionMeble.Kolizja(R, "Pergola kolizja", new Vector3(xx, y + ph * .5f, zz), new Vector3(.16f * K, ph, .16f * K));
        // luneta widokowa przy balustradzie (na morze)
        var lu = new BraxRegionMeble.Bud(); var lp = new Vector3(hx1 - 1.0f, y, hz1 - 3.5f);
        lu.Cyl(lp, lp + Vector3.up * 1.1f * K, .07f * K, 12); lu.Cyl(lp + Vector3.up * 1.2f * K - Vector3.right * .2f * K, lp + Vector3.up * 1.2f * K + Vector3.right * .25f * K, .1f * K, 12);
        lu.Obiekt("Taras: luneta widokowa", R, BraxCityMaterials.Surface("Taras: luneta (zieleń lakier)", new Color(.12f, .32f, .22f), .45f, .4f), false, true);
        BraxRegionMeble.Kolizja(R, "Luneta kolizja", lp + Vector3.up * .6f * K, new Vector3(.3f, 1.3f * K, .3f));
        BraxRegion.Warm(R, "Taras: oprawa przy kiosku", new Vector3(cx, y + 3.4f, sz0 - 1f), .8f, 8f);
    }

    // punkty świata (misje / CI) — dopisywane na końcu kroku
    public static void ZapiszPunkty()
    {
        if (!swiat) return;
        var ids = new HashSet<string>(punkty.Select(p => p.id));
        swiat.points = swiat.points.Where(p => !ids.Contains(p.id)).Concat(punkty).ToArray();
        Debug.Log("BRAXHORN_MIASTO_B_PUNKTY " + string.Join(", ", punkty.Select(p => p.id)));
        punkty.Clear();
    }
}
