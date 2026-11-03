using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — KAMIENICA / BUDYNEK PIERZEI Z KOMPLETEM DETALI (wykonawca B, 2.11; właściciel 15:20: „od razu DETALE wszędzie:
// gzymsy i wszystko, jak na zdjęciach”; wzorzec: Urban City Buildings Pack — czerwona cegła, białe opaski, schody pożarowe, markizy).
// Każdy budynek: cokół z kamienia, boniowany parter (sklepy z witrynami, drzwiami, fryzem z szyldem i markizami w paski),
// lizeny narożne, okna wnęki ≥ 0.18 m × k z ramą, szprosem, opaską, nadprożem z kluczem i parapetem, gzymsy międzypiętrowe,
// gzyms nad parterem, gzyms wieńczący z kostkami (ząbkami), attyka z czapą, rury spustowe z obejmami, balkony z balustradą,
// schody pożarowe (strona boczna/tylna), klimatyzatory, tabliczka z numerem (polski wzór), dach: papa/żwir, kominy, wywiewki,
// antena, wyłaz. Szyby: „pokój za szybą” (shader Braxhorn/OsiedleOkno — jasne pokoje, nie czarne), witryny: wnętrza sklepów.
// Wszystkie wymiary = rzeczywiste × k (BraxClaudeOsiedle.K = 1.294). Siatki łączone per materiał, LOD0 (detal) / LOD1 (bryła
// z oknami, bez drobnicy), zapis plików robi BraxRegion.PersistMeshes (siatki pod korzeniem regionu).
// ZASADA BEZ MIGOTANIA: żadne dwie powierzchnie nie leżą w jednej płaszczyźnie na tym samym miejscu — elementy na licu ściany
// wystają ≥ 2 cm albo stykają się krawędzią; ściany sąsiadów w pierzei nie są dublowane (ściana wspólna tylko raz).
public static class BraxRegionKamienica
{
    static float K => BraxClaudeOsiedle.K;

    public sealed class Styl
    {
        public string nazwa;
        public Material sciana, cokol, boniowanie, detal, rama, szyba, witryna, dach, metal, markiza, szyld, tloSzyldu;
        public Color literySzyldu = Color.white;
    }

    public sealed class Segment
    {
        public string nazwa = "Kamienica";
        public float x0, x1, z0, z1;
        public int pietra = 5;                       // kondygnacje nad parterem
        public Styl styl;
        public bool[] sklepy = new bool[4];          // strona ma parter usługowy: 0 = +z, 1 = +x, 2 = −z, 3 = −x
        public bool[] ulica = new bool[4];           // strona od ulicy (markizy, szyld, numer)
        public bool[] sciepa = new bool[4];          // ściana wspólna z sąsiadem (bez okien; budowana tylko, gdy sąsiad niższy)
        public string[] szyldy = new string[4];
        public string numer = "";
        public bool schodyPozarowe, balkony = true, klimatyzatory = true;
        public int ziarno = 1;
        public float y0 = .15f;                      // poziom chodnika
    }

    // wymiary × k (wartości rzeczywiste w komentarzach)
    static float Hpar => 4.4f * K;      // parter 4.4 m
    static float Hp => 3.0f * K;        // kondygnacja 3.0 m
    static float Bay => 3.0f * K;       // rozstaw osi okien 3.0 m
    static float Ow => 1.25f * K;       // okno 1.25 × 1.65 m
    static float Oh => 1.65f * K;
    static float Os => .85f * K;        // parapet 0.85 m nad podłogą
    static float Glab => .18f * K;      // głębokość wnęki okna (≥ 15 cm × k)
    static float Lic => .32f;           // grubość warstwy lica (ściany)

    sealed class Mesze
    {
        public readonly Dictionary<Material, BraxRegionMeble.Bud> m = new Dictionary<Material, BraxRegionMeble.Bud>();
        public BraxRegionMeble.Bud this[Material mat] { get { if (!m.TryGetValue(mat, out var b)) m[mat] = b = new BraxRegionMeble.Bud(); return b; } }
    }

    struct Strona
    {
        public Vector3 O, A, N; public float L; public int i;
        public Vector3 P(float a, float y, float d) => O + A * a + Vector3.up * y + N * d;
    }

    static Strona StronaBud(Segment s, int i, float y0)
    {
        switch (i)
        {
            case 0: return new Strona { O = new Vector3(s.x1, y0, s.z1), A = Vector3.left, N = Vector3.forward, L = s.x1 - s.x0, i = 0 };
            case 1: return new Strona { O = new Vector3(s.x1, y0, s.z0), A = Vector3.forward, N = Vector3.right, L = s.z1 - s.z0, i = 1 };
            case 2: return new Strona { O = new Vector3(s.x0, y0, s.z0), A = Vector3.right, N = Vector3.back, L = s.x1 - s.x0, i = 2 };
            default: return new Strona { O = new Vector3(s.x0, y0, s.z1), A = Vector3.back, N = Vector3.left, L = s.z1 - s.z0, i = 3 };
        }
    }

    // prostopadłościan w układzie strony (a wzdłuż, y w górę, d na zewnątrz od lica)
    static void B(BraxRegionMeble.Bud b, Strona s, float a0, float a1, float y0, float y1, float d0, float d1)
    {
        // narożniki bez nakładania: elementy wchodzące w warstwę ściany (d0 < 0) na stronach bocznych (±x) kończą się
        // tam, gdzie zaczyna się ściana stron ±z (margines = głębokość elementu) — w narożniku jest tylko ściana ±z
        if (s.i % 2 == 1 && d0 < -.001f) { a0 = Mathf.Max(a0, -d0); a1 = Mathf.Min(a1, s.L + d0); }
        if (a1 - a0 < .003f || y1 - y0 < .003f || d1 - d0 < .003f) return;
        var p0 = s.P(a0, y0, d0); var p1 = s.P(a1, y1, d1);
        b.MinMax(Vector3.Min(p0, p1), Vector3.Max(p0, p1));
    }

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static BraxRegion.Report cacheDla;

    static Material OknoMat(string key, float wariant, float warianty, float cellH, float floor0, float cellW)
    {
        if (cacheDla != BraxRegion.Rep) { cacheDla = BraxRegion.Rep; cache.Clear(); }
        if (cache.TryGetValue(key, out var have) && have) return have;
        var sh = Shader.Find("Braxhorn/OsiedleOkno"); const string path = "Assets/Source/Claude/Osiedle/detail/pokoje.png";
        Material m;
        if (!sh || !File.Exists(path))
        {
            BraxRegion.Note("kamienice: brak shadera/atlasu pokoi — szyba jasna z odbiciem (nie czarna)");
            m = BraxCityMaterials.Surface("Kamienica: szyba jasna " + key, new Color(.52f, .60f, .64f), .12f, .1f);
        }
        else
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp && (imp.wrapMode != TextureWrapMode.Clamp || imp.textureType != TextureImporterType.Default))
            {
                imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
                imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
            }
            m = new Material(sh) { name = "Kamienica: " + key };
            m.SetTexture("_Rooms", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            m.SetFloat("_Variant", wariant); m.SetFloat("_Variants", warianty); m.SetFloat("_CellH", cellH); m.SetFloat("_Floor0", floor0);
            m.SetFloat("_CellW", cellW); m.SetFloat("_Depth", 4.2f * K); m.SetFloat("_RoomLight", .62f);
            Directory.CreateDirectory(BraxRegion.Out + "/Materials");
            BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Materials/kamienica_okno_" + cache.Count + ".mat");
        }
        cache[key] = m; return m;
    }

    // markiza w paski: tekstura z kodu (64×8, paski 2 kolory), kafel = 1 pas 0.25 m × k
    static Material Paski(string nazwa, Color a, Color b)
    {
        if (cacheDla != BraxRegion.Rep) { cacheDla = BraxRegion.Rep; cache.Clear(); }
        if (cache.TryGetValue("paski|" + nazwa, out var have) && have) return have;
        var t = new Texture2D(64, 8, TextureFormat.RGBA32, true) { name = "markiza " + nazwa, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        var px = new Color[64 * 8];
        for (int x = 0; x < 64; x++) for (int y = 0; y < 8; y++) px[y * 64 + x] = x < 32 ? a : b;
        t.SetPixels(px); t.Apply(true);
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        string p = BraxRegion.Out + "/Materials/markiza_" + cache.Count + ".asset";
        BraxZapisAssetu.Utworz(t, p);
        var m = BraxCityMaterials.Textured("Kamienica: markiza " + nazwa, t, null, null, 1f);
        m.SetFloat("_Roughness", .85f); m.SetFloat("_Cull", 0f);
        cache["paski|" + nazwa] = m; return m;
    }

    // ------------------------------------------------------------------ style (2–3 odcienie cegły, tynk, kamień)
    public static Styl CeglaCzerwona() => StylZ("cegła czerwona", BraxRegionMeble.Tex("Kamienica: cegła czerwona", 1.0f, new Color(.92f, .80f, .74f), .9f, "red_brick_03", "brick_wall_001", "brick_wall_10"),
        new Color(.93f, .92f, .88f), Paski("zieleń/krem", new Color(.10f, .30f, .20f), new Color(.92f, .89f, .80f)), new Color(.08f, .2f, .14f));
    public static Styl CeglaCiemna() => StylZ("cegła ciemna", BraxRegionMeble.Tex("Kamienica: cegła ciemna (klinkier)", 1.0f, new Color(.66f, .56f, .52f), .9f, "brick_wall_001", "red_brick_03", "brick_wall_10"),
        new Color(.86f, .84f, .80f), Paski("bordo/krem", new Color(.45f, .08f, .10f), new Color(.93f, .90f, .82f)), new Color(.32f, .05f, .06f));
    public static Styl CeglaJasna() => StylZ("cegła jasna", BraxRegionMeble.Tex("Kamienica: cegła jasna", 1.0f, new Color(1.0f, .92f, .82f), .9f, "brick_wall_10", "red_brick_03", "brick_wall_001"),
        new Color(.95f, .94f, .90f), Paski("granat/biel", new Color(.08f, .14f, .30f), new Color(.95f, .95f, .93f)), new Color(.06f, .1f, .22f));
    public static Styl Tynk() => StylZ("tynk", BraxRegionMeble.Tex("Kamienica: tynk kremowy", 1.5f, new Color(.95f, .90f, .80f), .92f, "white_stucco_02", "white_plaster_02"),
        new Color(.97f, .96f, .93f), Paski("terakota/krem", new Color(.62f, .25f, .12f), new Color(.95f, .91f, .82f)), new Color(.2f, .2f, .22f));
    public static Styl Kamien() => StylZ("kamień", BraxRegionMeble.Tex("Kamienica: piaskowiec", 1.2f, new Color(.96f, .93f, .86f), .9f, "white_sandstone_blocks_02", "rectangular_facade_tiles"),
        new Color(.90f, .88f, .82f), Paski("grafit/biel", new Color(.14f, .15f, .16f), new Color(.95f, .95f, .93f)), new Color(.12f, .12f, .13f));

    static Styl StylZ(string nazwa, Material sciana, Color detal, Material markiza, Color tloSzyldu)
    {
        return new Styl
        {
            nazwa = nazwa, sciana = sciana,
            cokol = BraxRegionMeble.Tex("Kamienica: cokół granit", 1.2f, new Color(.62f, .62f, .60f), .8f, "granite_tile_02", "rough_block_wall", "concrete_wall_008"),
            boniowanie = BraxRegionMeble.Tex("Kamienica: boniowanie (piaskowiec)", 1.2f, new Color(.86f, .84f, .79f), .88f, "white_sandstone_blocks_02", "rectangular_facade_tiles", "concrete_wall_008"),
            detal = BraxCityMaterials.Surface("Kamienica: sztukateria " + ColorUtility.ToHtmlStringRGB(detal), detal, .78f),
            rama = BraxCityMaterials.Surface("Kamienica: rama okna (drewno lakier grafit)", new Color(.11f, .12f, .12f), .55f, .1f),
            szyba = OknoMat("okna mieszkań", 0, 5, Hp, Hpar + .15f, Bay),
            witryna = OknoMat("witryny sklepów", 5, 3, Hpar, .15f, Bay),
            dach = BraxRegionMeble.Tex("Kamienica: dach (żwir na papie)", 1.5f, new Color(.62f, .60f, .58f), .95f, "bicolour_gravel", "concrete_floor_02"),
            metal = BraxCityMaterials.Surface("Kamienica: stal malowana czarna", new Color(.07f, .07f, .075f), .5f, .6f),
            markiza = markiza,
            tloSzyldu = BraxCityMaterials.Surface("Kamienica: kaseton szyldu " + ColorUtility.ToHtmlStringRGB(tloSzyldu), tloSzyldu, .45f, .2f),
        };
    }

    static int nBud, nDetali;
    public static int Budynki => nBud;

    // ------------------------------------------------------------------ budynek
    public static GameObject Budynek(Transform parent, Segment s)
    {
        var st = s.styl ?? CeglaCzerwona();
        var rnd = new System.Random(s.ziarno * 7919 + 13);
        var go = new GameObject("Budynek: " + s.nazwa); go.transform.SetParent(parent, false); go.transform.position = Vector3.zero;
        var lod0 = new GameObject("LOD0 (detal)").transform; lod0.SetParent(go.transform, false);
        var lod1 = new GameObject("LOD1 (bryła z oknami)").transform; lod1.SetParent(go.transform, false);
        var M0 = new Mesze(); var M1 = new Mesze();
        float y0 = s.y0, H = Hpar + s.pietra * Hp;
        float yTop = y0 + H, attyka = .9f * K;
        for (int i = 0; i < 4; i++)
        {
            var sd = StronaBud(s, i, y0);
            if (s.sciepa[i]) { Slepa(M0[st.sciana], sd, H + attyka); Slepa(M1[st.sciana], sd, H + attyka); continue; }
            Fasada(M0, st, s, sd, rnd, true);
            Fasada(M1, st, s, sd, rnd, false);
        }
        Dach(M0, st, s, rnd, yTop, attyka, true);
        Dach(M1, st, s, rnd, yTop, attyka, false);
        // szyldy (litery 3D) i numer — tylko LOD0
        for (int i = 0; i < 4; i++)
        {
            if (s.sciepa[i]) continue;
            var sd = StronaBud(s, i, y0);
            if (s.sklepy[i] && !string.IsNullOrEmpty(s.szyldy[i]))
            {
                var c = sd.P(sd.L * .5f, Hpar - .62f * K, .16f * K + .02f);
                BraxRegionMeble.Napis(lod0, s.szyldy[i], c, sd.N, .42f * K, st.literySzyldu);
                // szyld semaforowy (wysięgnik) przy narożniku — jak na wzorcu
                var tab = sd.P(1.2f * K, Hpar + 1.0f * K, .75f * K);
                B(M0[st.metal], sd, 1.2f * K - .02f, 1.2f * K + .02f, Hpar + 1.55f * K, Hpar + 1.6f * K, 0f, 1.25f * K);
                B(M0[st.tloSzyldu], sd, 1.2f * K - .06f, 1.2f * K + .06f, Hpar + .45f * K, Hpar + 1.5f * K, .3f * K, 1.2f * K);
                var g1 = new GameObject("szyld semaforowy"); g1.transform.SetParent(lod0, false);
                BraxRegionMeble.Napis(g1.transform, s.szyldy[i].Length > 10 ? s.szyldy[i].Substring(0, 10) : s.szyldy[i], tab + sd.A * .07f, sd.A, .16f * K, st.literySzyldu);
                BraxRegionMeble.Napis(g1.transform, s.szyldy[i].Length > 10 ? s.szyldy[i].Substring(0, 10) : s.szyldy[i], tab - sd.A * .07f, -sd.A, .16f * K, st.literySzyldu);
            }
            if (s.ulica[i] && !string.IsNullOrEmpty(s.numer))
            {
                // tabliczka adresowa (polski wzór: niebieska, biały napis) 0.35 × 0.25 m × k, przy drzwiach, 2.6 m × k
                float a = sd.L - .9f * K;
                B(M0[TabliczkaMat], sd, a - .18f * K, a + .18f * K, 2.5f * K, 2.76f * K, .02f, .045f);
                BraxRegionMeble.Napis(lod0, s.numer, sd.P(a, 2.63f * K, .05f), sd.N, .16f * K, Color.white);
            }
        }
        foreach (var kv in M0.m) kv.Value.Obiekt(s.nazwa + " — " + kv.Key.name, lod0, kv.Key, false, true);
        foreach (var kv in M1.m) kv.Value.Obiekt(s.nazwa + " — " + kv.Key.name + " (LOD1)", lod1, kv.Key, false, false);
        // kolizja bryły (ściany zewnętrzne) — jeden prostopadłościan
        BraxRegionMeble.Kolizja(go.transform, "Kolizja bryły " + s.nazwa, new Vector3((s.x0 + s.x1) * .5f, y0 + (H + attyka) * .5f, (s.z0 + s.z1) * .5f), new Vector3(s.x1 - s.x0, H + attyka, s.z1 - s.z0));
        var lg = go.AddComponent<LODGroup>();
        lg.SetLODs(new[] { new LOD(.10f, lod0.GetComponentsInChildren<Renderer>(true)), new LOD(.008f, lod1.GetComponentsInChildren<Renderer>(true)) });
        lg.RecalculateBounds();
        nBud++;
        return go;
    }

    static Material tabliczka;
    static Material TabliczkaMat { get { if (!tabliczka) tabliczka = BraxCityMaterials.Surface("Kamienica: tabliczka adresowa (niebieska emalia)", new Color(.05f, .18f, .52f), .3f, .1f); return tabliczka; } }

    static void Slepa(BraxRegionMeble.Bud b, Strona s, float h)
    {
        // ściana wspólna (ślepa): lico 0.32 m, bez okien — widoczna tylko nad niższym sąsiadem
        B(b, s, 0, s.L, 0, h, -Lic, 0f);
    }

    static void Fasada(Mesze M, Styl st, Segment seg, Strona s, System.Random rnd, bool detal)
    {
        float L = s.L, H = Hpar + seg.pietra * Hp, attyka = .9f * K;
        float pil = .55f * K;                        // lizena narożna
        int n = Mathf.Max(1, Mathf.FloorToInt((L - 2f * pil) / Bay));
        float m0 = (L - n * Bay) * .5f;
        var osie = new List<float>(); for (int i = 0; i < n; i++) osie.Add(m0 + Bay * (i + .5f));
        bool sklep = seg.sklepy[s.i];
        var sciana = st.sciana; var rama = st.rama; var det = st.detal;

        // ---------------- PARTER ----------------
        float cok = .55f * K;
        if (sklep)
        {
            float fryz0 = Hpar - 1.05f * K;                    // fryz z kasetonem szyldu
            float filar = .7f * K;
            // filary między witrynami (boniowane), cokół, fryz
            var gr = new List<float> { 0f };
            foreach (var o in osie) { gr.Add(o - (Bay - filar) * .5f); gr.Add(o + (Bay - filar) * .5f); }
            gr.Add(L);
            for (int i = 0; i + 1 < gr.Count; i += 2)
            {
                float a0 = gr[i], a1 = gr[i + 1];
                B(M[st.cokol], s, a0, a1, 0f, cok, -Lic, .04f);
                B(M[st.boniowanie], s, a0, a1, cok, fryz0, -Lic, 0f);
                if (detal)   // boniowanie: bonie 0.42 m z fugą 3 cm (wystają 3 cm)
                    for (float y = cok + .02f; y + .38f * K < fryz0; y += .45f * K)
                        B(M[st.boniowanie], s, a0 + .02f, a1 - .02f, y, y + .42f * K, 0f, .03f);
            }
            B(M[sciana], s, 0, L, fryz0, Hpar, -Lic, 0f);
            // kaseton szyldu na całej szerokości witryn
            if (!string.IsNullOrEmpty(seg.szyldy[s.i]))
                B(M[st.tloSzyldu], s, osie[0] - Bay * .45f, osie[osie.Count - 1] + Bay * .45f, fryz0 + .12f * K, Hpar - .2f * K, 0f, .14f * K);
            // witryny i drzwi
            int drzwi = osie.Count / 2;
            for (int i = 0; i < osie.Count; i++)
            {
                float a0 = osie[i] - (Bay - filar) * .5f, a1 = osie[i] + (Bay - filar) * .5f;
                bool d = i == drzwi;
                float yb = d ? 0f : cok;
                // szyba witryny / drzwi w głębi (wnęka 0.18 m × k)
                B(M[st.witryna], s, a0, a1, yb, fryz0, -Lic, -Glab);
                if (!d) B(M[st.cokol], s, a0, a1, 0f, yb, -Lic, .04f);   // cokół pod witryną (do 3.11 brakowało — dziura do wnętrza pod każdą witryną)
                if (d) B(M[st.cokol], s, a0, a1, -.02f, .02f, -Glab - .02f, .03f);   // próg granitowy (posadzka + 2 cm, bez progu do przeskakiwania)
                if (!detal) continue;
                // rama: ościeżnica, słupki, naświetle
                B(M[rama], s, a0, a0 + .07f * K, yb, fryz0, -Glab, -Glab + .06f * K);
                B(M[rama], s, a1 - .07f * K, a1, yb, fryz0, -Glab, -Glab + .06f * K);
                B(M[rama], s, a0, a1, fryz0 - .07f * K, fryz0, -Glab, -Glab + .06f * K);
                B(M[rama], s, a0, a1, fryz0 - .75f * K, fryz0 - .69f * K, -Glab, -Glab + .06f * K);   // naświetle
                B(M[rama], s, a0, a1, yb, yb + .1f * K, -Glab, -Glab + .06f * K);
                if (d)
                {
                    float m = (a0 + a1) * .5f;
                    B(M[rama], s, m - .035f * K, m + .035f * K, yb, fryz0 - .7f * K, -Glab, -Glab + .06f * K);
                    foreach (float sg in new[] { -1f, 1f })   // pochwyty drzwi (stal szczotkowana)
                        B(M[st.metal], s, m + sg * .18f * K - .015f, m + sg * .18f * K + .015f, .8f * K, 1.6f * K, -Glab + .06f * K, -Glab + .1f * K);
                }
                else B(M[rama], s, (a0 + a1) * .5f - .03f * K, (a0 + a1) * .5f + .03f * K, yb, fryz0 - .7f * K, -Glab, -Glab + .06f * K);
                // markiza w paski nad witryną (ramię stalowe, wysięg 1.2 m × k, spad 0.6 m × k)
                if (seg.ulica[s.i])
                {
                    float ym = fryz0 + .05f * K, wys = 1.25f * K, spad = .65f * K;
                    var q0 = s.P(a0 - .05f, ym, .05f); var q1 = s.P(a1 + .05f, ym, .05f);
                    var q2 = s.P(a1 + .05f, ym - spad, wys); var q3 = s.P(a0 - .05f, ym - spad, wys);
                    M[st.markiza].Quad(q0, q1, q2, q3);
                    M[st.markiza].Quad(q1, q0, q3, q2);
                    // falbana (lambrekin) 0.25 m
                    var f0 = q3; var f1 = q2; var f2 = q2 - Vector3.up * .25f * K; var f3 = q3 - Vector3.up * .25f * K;
                    M[st.markiza].Quad(f0, f1, f2, f3); M[st.markiza].Quad(f1, f0, f3, f2);
                    foreach (float aa in new[] { a0 + .1f, a1 - .1f })
                        M[st.metal].Cyl(s.P(aa, ym - .02f, .03f), s.P(aa, ym - spad + .02f, wys - .05f), .012f * K, 6);
                    nDetali++;
                }
            }
        }
        else
        {
            // parter mieszkalny/biurowy: cokół + boniowanie + okna jak piętra (wyżej podniesione)
            B(M[st.cokol], s, 0, L, 0f, cok, -Lic, .04f);
            Okna(M, st, seg, s, osie, cok, Hpar, Hpar - 2.1f * K, detal, rnd, -1, st.boniowanie);
            if (detal)
                for (float y = cok + .02f; y + .38f * K < Hpar - .4f * K; y += .45f * K)
                    foreach (var (a0, a1) in Miedzy(osie, L))
                        B(M[st.boniowanie], s, a0 + .02f, a1 - .02f, y, y + .42f * K, 0f, .03f);
        }
        // gzyms nad parterem (wysięg 0.3 m × k)
        B(M[det], s, -(s.i % 2 == 0 ? .3f * K : 0f), L + (s.i % 2 == 0 ? .3f * K : 0f), Hpar - .38f * K, Hpar, 0f, .3f * K);
        if (detal) B(M[det], s, -(s.i % 2 == 0 ? .34f * K : 0f), L + (s.i % 2 == 0 ? .34f * K : 0f), Hpar - .02f, Hpar + .07f * K, 0f, .34f * K);

        // ---------------- PIĘTRA ----------------
        bool balkonyTu = seg.balkony && seg.ulica[s.i] && seg.pietra >= 3;
        for (int f = 0; f < seg.pietra; f++)
        {
            float fy = Hpar + f * Hp;
            int balkonOkno = balkonyTu && f >= 1 && f < seg.pietra - 1 && (f % 2 == 1) ? osie.Count / 2 : -1;
            Okna(M, st, seg, s, osie, fy, fy + Hp, fy + Os, detal, rnd, balkonOkno, sciana);
            // gzyms międzypiętrowy (pas 0.12 m × k, wysięg 0.07 m × k)
            if (f > 0) B(M[det], s, -(s.i % 2 == 0 ? .07f * K : 0f), L + (s.i % 2 == 0 ? .07f * K : 0f), fy - .06f * K, fy + .06f * K, 0f, .07f * K);
            if (detal && balkonOkno >= 0) Balkon(M, st, s, osie[balkonOkno], fy);
            if (detal && seg.klimatyzatory && !seg.ulica[s.i] && rnd.NextDouble() < .35) Klima(M, st, s, osie[rnd.Next(osie.Count)] + Bay * .5f * (rnd.NextDouble() < .5 ? -1f : 1f) * 0f, fy + Os - .15f * K);
        }
        // lizeny narożne (od gzymsu parteru do gzymsu wieńczącego), wysięg 0.1 m × k
        B(M[det], s, 0f, pil, Hpar, H, 0f, .1f * K);
        B(M[det], s, L - pil, L, Hpar, H, 0f, .1f * K);
        // gzyms wieńczący z kostkami + attyka z czapą
        float wy = .5f * K;
        B(M[det], s, -(s.i % 2 == 0 ? wy : 0f), L + (s.i % 2 == 0 ? wy : 0f), H - .45f * K, H, 0f, wy);
        B(M[det], s, -(s.i % 2 == 0 ? wy * .6f : 0f), L + (s.i % 2 == 0 ? wy * .6f : 0f), H - .62f * K, H - .45f * K, 0f, wy * .6f);
        if (detal) for (float a = pil + .12f * K; a < L - pil - .1f * K; a += .32f * K) B(M[det], s, a, a + .14f * K, H - .72f * K, H - .62f * K, 0f, .18f * K);   // kostki (ząbki)
        B(M[sciana], s, 0, L, H, H + attyka, -Lic, 0f);
        B(M[det], s, -(s.i % 2 == 0 ? .06f * K : 0f), L + (s.i % 2 == 0 ? .06f * K : 0f), H + attyka, H + attyka + .08f * K, -Lic - .04f, .06f * K);
        // rury spustowe (Ø 10 cm × k) przy lizenach z obejmami co 2 m, wylewka nad chodnikiem
        if (detal)
        {
            foreach (float a in new[] { pil + .14f * K, L - pil - .14f * K })
            {
                var top = s.P(a, H - .5f * K, .12f * K); var dol = s.P(a, .25f * K, .12f * K);
                M[st.metal].Cyl(dol, top, .05f * K, 10);
                M[st.metal].Cyl(dol, dol + s.N * .18f * K - Vector3.up * .12f * K, .05f * K, 8);
                for (float y = .9f * K; y < H - .6f * K; y += 2f * K) B(M[st.metal], s, a - .07f * K, a + .07f * K, y, y + .04f * K, 0f, .15f * K);
            }
            // schody pożarowe na stronie bocznej/tylnej (nie od ulicy)
            if (seg.schodyPozarowe && !seg.ulica[s.i] && !seg.sklepy[s.i] && L > 3.2f * Bay && seg.pietra >= 3) SchodyPozarowe(M, st, s, osie, seg.pietra);
        }
    }

    static IEnumerable<(float, float)> Miedzy(List<float> osie, float L)
    {
        float prev = 0f;
        foreach (var o in osie) { yield return (prev, o - Ow * .5f - .16f * K); prev = o + Ow * .5f + .16f * K; }
        yield return (prev, L);
    }

    // jedno pasmo okien: ściana między fy0 a fy1, okna od yo (parapet) wysokości Oh; balkon = okno do podłogi (drzwi balkonowe)
    static void Okna(Mesze M, Styl st, Segment seg, Strona s, List<float> osie, float fy0, float fy1, float yo, bool detal, System.Random rnd, int balkon, Material sciana)
    {
        float L = s.L; float yg = yo + Oh;
        var gr = new List<float> { 0f };
        foreach (var o in osie) { gr.Add(o - Ow * .5f); gr.Add(o + Ow * .5f); }
        gr.Add(L);
        // filary między oknami — od podłogi pasma (fy0) do nadproża (yg). BŁĄD do 3.11: filary zaczynały się od parapetu (yo),
        // więc między oknami od podłogi do parapetu ściany nie było — dziury w elewacji na każdym piętrze (właściciel 3.11 11:11:
        // czarne i białe prostokąty między oknami = widok do pustego wnętrza / na wylot)
        for (int i = 0; i + 1 < gr.Count; i += 2) B(M[sciana], s, gr[i], gr[i + 1], fy0, yg, -Lic, 0f);
        // podokienniki i nadproża (pełna długość, z przerwą na drzwi balkonowe)
        for (int i = 0; i < osie.Count; i++)
        {
            float a0 = osie[i] - Ow * .5f, a1 = osie[i] + Ow * .5f;
            bool bal = i == balkon;
            float yb = bal ? fy0 + .02f : yo;
            if (!bal) B(M[sciana], s, a0, a1, fy0, yo, -Lic, 0f);
            else B(M[sciana], s, a0, a1, fy0, yb, -Lic, 0f);
            // szyba w głębi wnęki
            B(M[st.szyba], s, a0, a1, yb, yg, -Lic, -Glab);
            if (!detal) continue;
            float r = .065f * K;
            B(M[st.rama], s, a0, a0 + r, yb, yg, -Glab, -Glab + .06f * K);
            B(M[st.rama], s, a1 - r, a1, yb, yg, -Glab, -Glab + .06f * K);
            B(M[st.rama], s, a0, a1, yg - r, yg, -Glab, -Glab + .06f * K);
            B(M[st.rama], s, a0, a1, yb, yb + r, -Glab, -Glab + .06f * K);
            float sm = (a0 + a1) * .5f;
            B(M[st.rama], s, sm - .03f * K, sm + .03f * K, yb, yg, -Glab, -Glab + .05f * K);          // słupek (szpros pionowy)
            B(M[st.rama], s, a0, a1, yg - .5f * K, yg - .45f * K, -Glab, -Glab + .05f * K);           // ślemię (naświetle)
            // opaska (obramienie 0.12 m × k, wysięg 4 cm), nadproże z kluczem, parapet zewnętrzny
            float o = .12f * K;
            B(M[st.detal], s, a0 - o, a0, yb, yg, 0f, .04f * K);
            B(M[st.detal], s, a1, a1 + o, yb, yg, 0f, .04f * K);
            B(M[st.detal], s, a0 - o, a1 + o, yg, yg + .2f * K, 0f, .07f * K);                        // nadproże
            B(M[st.detal], s, sm - .1f * K, sm + .1f * K, yg - .05f * K, yg + .26f * K, .07f * K, .1f * K);   // klucz
            if (!bal) B(M[st.detal], s, a0 - o - .04f * K, a1 + o + .04f * K, yb - .07f * K, yb, 0f, .12f * K);   // parapet
            nDetali++;
        }
        // nadproża (pas nad oknami do sufitu pasma)
        B(M[sciana], s, 0, L, yg, fy1, -Lic, 0f);
    }

    static void Balkon(Mesze M, Styl st, Strona s, float a, float fy)
    {
        float w = Ow + .9f * K, gl = 1.0f * K;
        B(M[st.detal], s, a - w * .5f, a + w * .5f, fy - .16f * K, fy + .02f, 0f, gl);                        // płyta balkonu
        for (float d = .25f * K; d < gl; d += .45f * K) B(M[st.detal], s, a - w * .5f + .05f, a - w * .5f + .17f, fy - .4f * K, fy - .16f * K, d - .06f, d + .06f);   // wsporniki
        for (float d = .25f * K; d < gl; d += .45f * K) B(M[st.detal], s, a + w * .5f - .17f, a + w * .5f - .05f, fy - .4f * K, fy - .16f * K, d - .06f, d + .06f);
        float h = 1.1f * K;
        // balustrada stalowa: pochwyt, słupki co 12 cm
        var b = M[st.metal];
        var c0 = s.P(a - w * .5f + .04f, fy + .02f, gl - .04f); var c1 = s.P(a + w * .5f - .04f, fy + .02f, gl - .04f);
        var w0 = s.P(a - w * .5f + .04f, fy + .02f, .02f); var w1 = s.P(a + w * .5f - .04f, fy + .02f, .02f);
        b.Cyl(c0 + Vector3.up * h, c1 + Vector3.up * h, .025f * K, 8);
        b.Cyl(w0 + Vector3.up * h, c0 + Vector3.up * h, .025f * K, 8);
        b.Cyl(w1 + Vector3.up * h, c1 + Vector3.up * h, .025f * K, 8);
        b.Cyl(c0 + Vector3.up * .08f, c1 + Vector3.up * .08f, .015f * K, 6);
        for (float t = 0; t <= 1.001f; t += .12f / Vector3.Distance(c0, c1)) { var p = Vector3.Lerp(c0, c1, t); b.Cyl(p, p + Vector3.up * h, .009f * K, 5, -1f, false); }
        for (float t = 0; t <= 1.001f; t += .12f / Vector3.Distance(w0, c0)) { var p = Vector3.Lerp(w0, c0, t); b.Cyl(p, p + Vector3.up * h, .009f * K, 5, -1f, false); var q = Vector3.Lerp(w1, c1, t); b.Cyl(q, q + Vector3.up * h, .009f * K, 5, -1f, false); }
        nDetali++;
    }

    static void Klima(Mesze M, Styl st, Strona s, float a, float y)
    {
        // jednostka zewnętrzna klimatyzatora 0.8 × 0.55 × 0.3 m × k na wspornikach pod oknem
        var kl = BraxCityMaterials.Surface("Kamienica: klimatyzator (biały lakier)", new Color(.86f, .87f, .86f), .5f, .2f);
        B(M[kl], s, a - .4f * K, a + .4f * K, y - .6f * K, y - .05f * K, .04f, .34f * K);
        B(M[st.metal], s, a - .32f * K, a - .28f * K, y - .64f * K, y - .6f * K, 0f, .36f * K);
        B(M[st.metal], s, a + .28f * K, a + .32f * K, y - .64f * K, y - .6f * K, 0f, .36f * K);
        var kratka = BraxCityMaterials.Surface("Kamienica: kratka wentylatora", new Color(.18f, .19f, .2f), .6f, .4f);
        B(M[kratka], s, a - .08f * K, a + .3f * K, y - .5f * K, y - .14f * K, .34f * K, .34f * K + .02f);
        nDetali++;
    }

    static void SchodyPozarowe(Mesze M, Styl st, Strona s, List<float> osie, int pietra)
    {
        // podesty na każdym piętrze przed dwoma oknami, biegi ukośne między podestami, drabina na dole (stal czarna)
        float a0 = osie[0] - Ow * .6f, a1 = osie[1] + Ow * .6f, gl = 1.25f * K, h = 1.0f * K;
        var b = M[st.metal];
        for (int f = 0; f < pietra; f++)
        {
            float fy = Hpar + f * Hp + .02f;
            B(b, s, a0, a1, fy - .06f, fy, .05f, gl);                                    // krata podestu
            for (float a = a0; a <= a1 + .01f; a += .6f * K) b.Cyl(s.P(Mathf.Min(a, a1), fy, gl - .03f), s.P(Mathf.Min(a, a1), fy + h, gl - .03f), .015f * K, 6);
            b.Cyl(s.P(a0, fy + h, gl - .03f), s.P(a1, fy + h, gl - .03f), .02f * K, 6);
            b.Cyl(s.P(a0, fy + h * .5f, gl - .03f), s.P(a1, fy + h * .5f, gl - .03f), .012f * K, 6);
            foreach (float a in new[] { a0, a1 }) b.Cyl(s.P(a, fy + h, .05f), s.P(a, fy + h, gl - .03f), .02f * K, 6);
            // wsporniki ukośne do ściany
            foreach (float a in new[] { a0 + .2f, a1 - .2f }) b.Cyl(s.P(a, fy - .05f, gl - .1f), s.P(a, fy - .9f * K, .02f), .02f * K, 6);
            if (f + 1 < pietra)
            {
                // bieg: od podestu f (koniec a1) do podestu f+1 (początek a0+…) — policzki + stopnie
                float ya = fy, yb = fy + Hp;
                var p0 = s.P(a1 - .3f * K, ya, gl * .5f); var p1 = s.P(a0 + 1.2f * K, yb, gl * .5f);
                int n = 14;
                for (int k = 0; k < n; k++)
                {
                    var p = Vector3.Lerp(p0, p1, (k + .5f) / n);
                    b.Box(p, s.A.x != 0 ? new Vector3(.24f * K, .03f, gl * .7f) : new Vector3(gl * .7f, .03f, .24f * K));
                }
                foreach (float dd in new[] { .12f * K, gl * .88f }) { var q0 = s.P(a1 - .3f * K, ya, dd); var q1 = s.P(a0 + 1.2f * K, yb, dd); b.Cyl(q0, q1, .02f * K, 6); b.Cyl(q0 + Vector3.up * h, q1 + Vector3.up * h, .016f * K, 6); }
            }
        }
        // drabina pod pierwszym podestem (podniesiona, 2 m nad chodnikiem)
        float yl = Hpar - 2.3f * K;
        foreach (float a in new[] { a1 - .55f * K, a1 - .15f * K }) b.Cyl(s.P(a, yl, gl - .2f), s.P(a, Hpar, gl - .2f), .016f * K, 6);
        for (float y = yl + .1f; y < Hpar; y += .3f * K) b.Cyl(s.P(a1 - .55f * K, y, gl - .2f), s.P(a1 - .15f * K, y, gl - .2f), .012f * K, 6);
        nDetali++;
    }

    static void Dach(Mesze M, Styl st, Segment s, System.Random rnd, float yTop, float attyka, bool detal)
    {
        // pokrycie dachu (żwir na papie) 5 cm poniżej czapy attyki, obróbka; kominy z cegły, wywiewki, antena, wyłaz
        float e = Lic;
        var b = M[st.dach];
        b.MinMax(new Vector3(s.x0 + e, yTop - .05f, s.z0 + e), new Vector3(s.x1 - e, yTop + .1f, s.z1 - e));
        if (!detal) return;
        int kominy = 2 + rnd.Next(2);
        for (int i = 0; i < kominy; i++)
        {
            float x = Mathf.Lerp(s.x0 + 2f, s.x1 - 2f, (float)rnd.NextDouble()), z = Mathf.Lerp(s.z0 + 2f, s.z1 - 2f, (float)rnd.NextDouble());
            M[st.sciana].MinMax(new Vector3(x - .45f * K, yTop + .1f, z - .3f * K), new Vector3(x + .45f * K, yTop + 1.6f * K, z + .3f * K));
            M[st.detal].MinMax(new Vector3(x - .52f * K, yTop + 1.6f * K, z - .37f * K), new Vector3(x + .52f * K, yTop + 1.7f * K, z + .37f * K));
            foreach (float dx in new[] { -.2f, .2f }) M[st.metal].Cyl(new Vector3(x + dx * K, yTop + 1.7f * K, z), new Vector3(x + dx * K, yTop + 1.95f * K, z), .07f * K, 8);
        }
        for (int i = 0; i < 3; i++)
        {
            float x = Mathf.Lerp(s.x0 + 1.5f, s.x1 - 1.5f, (float)rnd.NextDouble()), z = Mathf.Lerp(s.z0 + 1.5f, s.z1 - 1.5f, (float)rnd.NextDouble());
            M[st.metal].Cyl(new Vector3(x, yTop + .1f, z), new Vector3(x, yTop + .7f * K, z), .1f * K, 10);
            M[st.metal].Cyl(new Vector3(x, yTop + .7f * K, z), new Vector3(x, yTop + .76f * K, z), .17f * K, 10);
        }
        // antena (maszt 3 m × k z poprzeczkami)
        { float x = s.x0 + (s.x1 - s.x0) * .7f, z = s.z0 + (s.z1 - s.z0) * .35f; var p = new Vector3(x, yTop + .1f, z);
          M[st.metal].Cyl(p, p + Vector3.up * 3f * K, .03f * K, 6);
          for (int i = 0; i < 4; i++) { var c = p + Vector3.up * (1.8f + i * .32f) * K; M[st.metal].Cyl(c - Vector3.right * (.6f - i * .1f) * K, c + Vector3.right * (.6f - i * .1f) * K, .012f * K, 5); } }
        // wyłaz dachowy
        { float x = s.x0 + (s.x1 - s.x0) * .3f, z = s.z0 + (s.z1 - s.z0) * .6f;
          M[st.metal].MinMax(new Vector3(x - .45f * K, yTop + .1f, z - .45f * K), new Vector3(x + .45f * K, yTop + .45f * K, z + .45f * K)); }
    }

    public static void Raport() => BraxRegion.Note("kamienice: budynków " + nBud + ", elementów detalu " + nDetali);
    public static void Zeruj() { nBud = 0; nDetali = 0; }
}
