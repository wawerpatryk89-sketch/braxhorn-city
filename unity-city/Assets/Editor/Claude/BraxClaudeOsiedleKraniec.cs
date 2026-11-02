using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// KRANIEC MAPY I PANORAMA — build 14 (właściciel 1.11, 20:27 i zdjęcia 22:17 z buildu 12: „za płotami ma być realna panorama
// zajebista — góry, lasy, pola, z każdej strony inny widok, obraz 3D; żadnych kwadratów, sztucznych rzeczy”; było: stożki-
// „choinki”, płaska jasnoniebieska płaszczyzna, gładkie zielone wzgórze jak ściana nad tunelem).
// Plany głębi od płotu w stronę horyzontu:
//  1) teren za płotem (siatka wysokości bez kolizji): łąki falujące coraz mocniej z odległością, pola (suchsze pasy na
//     wschodzie i południu), na północy WZGÓRZE nad tunelem — zbocze z trawą, skałą (na stromych miejscach i wychodniach),
//     ściółką pod drzewami; nad portalem stok od korony kamiennej ściany w górę, po bokach skalne skrzydła;
//  2) prawdziwe drzewa (te same co w parku, korony z gałązek) i krzewy: na zachodzie skraj lasu, na wschodzie sad w rzędach
//     i żywopłot, na południu pojedyncze drzewa na łące, na zboczach wzgórza las; na stoku nad tunelem krzewy i głazy;
//  3) dalsze plany lasu: karty drzew zawsze zwrócone do kamery (2 trójkąty na drzewo), las na zachodzie i południu,
//     szpalery na wschodzie, las na wzgórzu za grzbietem (z przerwą na widok miasta);
//  4) panorama 360° z prawdziwego zdjęcia (Poly Haven „Alps Field”, CC0) w niebie: góry z lasem, skały, dolina z wsią
//     i polami, ośnieżony szczyt, na północy za tunelem sylwetka naszego miasta (OsiedleNiebo + OsiedlePanorama.cginc).
// Teren i dalszy las przechodzą z odległością dokładnie w kolor panoramy za nimi (shadery OsiedleDal*) — bez granicy
// zasięgu kamery, bez „płaszczyzny” we mgle. Stożki i kopuły wzgórz z buildu 12 usunięte.
public static partial class BraxClaudeOsiedle
{
    const float KrNorth = 101f;                       // północna krawędź trawy osiedla A (portal tunelu z = 99.5)
    const float KrX0 = -114.5f, KrX1 = 114.5f, KrZ0 = -236.5f;   // prostokąt osiedla; dalej teren za płotem
    static Texture2D pasmo;
    static Material krTeren, krLas;
    static readonly List<Vector3> krDrzewa = new List<Vector3>();   // drzewa za płotem (ściółka pod koronami)

    static void KraniecMapy()
    {
        group = "Kraniec mapy (teren dookoła)";
        krDrzewa.Clear(); pasmo = null; krTeren = null; krLas = null;
        var estate = GameObject.Find("braxhorn-estate")?.transform;
        var physics = GameObject.Find("Original estate collisions")?.transform;
        Guard("panorama 360° (niebo + krajobraz ze zdjęcia, miasto za tunelem)", Panorama);
        Guard("tunel do miasta (wnętrze, lampy, szlaban, budka)", () => TunelDoMiasta(estate, physics));
        Guard("drzewa i krzewy za płotem, las na wzgórzu", RoslinyZaPlotem);
        Guard("teren za płotem: łąki, pola, wzgórze nad tunelem", Teren);
        Guard("dalsze plany lasu (karty drzew)", DalszyLas);
        Guard("drzewa: korony z gałązek, stare drzewa Codexa, korony z dala od dachów", () => DrzewaOsiedla(estate, physics));
        Guard("zasięg drobnych rzeczy (LOD z przenikaniem)", () =>
        {
            new GameObject("BRAXHORN zasięg drobnych rzeczy (LOD)").AddComponent<BraxZasiegDrobnych>();
            Debug.Log("BRAXHORN_ZASIEG komponent w scenie osiedla (progi LOD liczone w grze dla trybu grafiki)");
        });
        // ruch (ludzie, auta) i mapa — po drogach A (DrogiA), przed trawą 3D; błąd w jednym nie zatrzymuje reszty
        Guard("ruch: przechodnie i auta", RuchNaOsiedlu);
        Guard("mapa osiedla (menu)", MapaOsiedla);
        Guard("sport: piłki i gry", Sport);
        Guard("dźwięki: silniki aut i tło (ptaki, wiatr, miasto)", DzwiekiOsiedla);   // po RuchNaOsiedlu (auta już stoją)
    }

    // ---------------- panorama ----------------
    [Serializable] sealed class PasmoInfo { public string zrodlo = ""; public int szerokosc, wysokosc, wiezowce_miasta; public float przesuniecie_u = .02f, elewacja_gora = 35f, elewacja_dol = -10f, linia_max_st, linia_min_st; }
    static PasmoInfo pasmoInfo = new PasmoInfo();
    static readonly Color KrMgla = new Color(.62f, .67f, .74f);

    static void PasmoNaMaterial(Material m)
    {
        if (!m || !pasmo) return;
        m.SetTexture("_Pasmo", pasmo);
        m.SetVector("_PasmoSrodek", new Vector4(0f, 0f, (KrNorth + KrZ0) * .5f, 0f));
        m.SetFloat("_PasmoPromien", 2500f); m.SetFloat("_PasmoU0", pasmoInfo.przesuniecie_u);
        m.SetFloat("_PasmoGora", pasmoInfo.elewacja_gora); m.SetFloat("_PasmoDol", pasmoInfo.elewacja_dol);
        m.SetFloat("_PasmoJasnosc", 1.0f); m.SetFloat("_PasmoNasycenie", 1.25f);
        m.SetFloat("_PasmoMgla", .12f); m.SetColor("_PasmoMglaKolor", KrMgla);
    }

    static void Panorama()
    {
        const string path = "Assets/Source/Claude/Osiedle/sky/panorama_pasmo.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) { rep.notes.Add("panorama: brak " + path + " (fetch_osiedle.py)"); Debug.Log("BRAXHORN_HORYZONT BRAK pasa panoramy " + path + " — niebo bez krajobrazu"); return; }
        try { var js = Path.ChangeExtension(path, ".json"); if (File.Exists(js)) JsonUtility.FromJsonOverwrite(File.ReadAllText(js), pasmoInfo); } catch { }
        // niebo: bez mipmap (zawsze ~2× powiększone; globalny limit mip na telefonie go nie zmniejsza), szerokość 4K dozwolona dla nieba
        imp.textureType = TextureImporterType.Default; imp.textureShape = TextureImporterShape.Texture2D; imp.sRGBTexture = true;
        imp.mipmapEnabled = false; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = false;
        imp.wrapModeU = TextureWrapMode.Repeat; imp.wrapModeV = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear;
        imp.maxTextureSize = 4096; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        pasmo = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var sky = RenderSettings.skybox;
        bool wNiebie = sky && sky.HasProperty("_Pasmo");
        if (wNiebie) { PasmoNaMaterial(sky); sky.SetFloat("_PasmoJest", 1f); EditorUtility.SetDirty(sky); }
        else rep.notes.Add("panorama: niebo bez shadera Braxhorn/OsiedleNiebo — krajobraz tylko na terenie");
        // mgła powietrza: kolor między mgiełką gór na zdjęciu a niebem przy horyzoncie, nieco rzadsza (góry widać ostro)
        RenderSettings.fogDensity = .0036f; RenderSettings.fogColor = KrMgla;
        rep.notes.Add("panorama: " + pasmoInfo.zrodlo + " " + pasmoInfo.szerokosc + "×" + pasmoInfo.wysokosc + ", miasto " + pasmoInfo.wiezowce_miasta + " budynków, mgła 0.0036");
        Debug.Log("BRAXHORN_HORYZONT panorama=" + (pasmo ? pasmo.width + "x" + pasmo.height : "brak") + " źródło=" + pasmoInfo.zrodlo + " w_niebie=" + (wNiebie ? "tak" : "NIE") +
                  " linia_gór=" + pasmoInfo.linia_min_st.ToString("0.0") + "…" + pasmoInfo.linia_max_st.ToString("0.0") + "° miasto_na_północy=" + pasmoInfo.wiezowce_miasta + " budynków" +
                  " (N: dolina + miasto za tunelem, E: zalesiona góra, S: dolina ze wsią i polami, W: góra z lasem i skałami) stożki=0");
    }

    // ---------------- teren: wysokość ----------------
    static float S01(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3f - 2f * t); }

    // odległość od prostokąta osiedla (0 na płocie)
    static float KrOdl(float x, float z)
    {
        float dx = Mathf.Max(0f, Mathf.Max(KrX0 - x, x - KrX1)), dz = Mathf.Max(0f, Mathf.Max(KrZ0 - z, z - KrNorth));
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // wzgórze nad tunelem (północ): od korony ściany portalu (9.3 m) w górę do grzbietu ~17 m, boki opadają, za grzbietem
    // płaskowyż i spadek; nad rurą tunelu zawsze ≥ 8.4 m; skalne skrzydła obok ściany portalu
    static float KrWzgorze(float x, float z)
    {
        if (z < KrNorth) return 0f;
        float ax = Mathf.Abs(x);
        float front = ax <= 20.3f ? 9.3f : 9.3f * (1f - S01(20.3f, 31f, ax)) * (1f - S01(KrNorth, KrNorth + 9f, z));
        float crest = (6f + 11f * (1f - S01(30f, 150f, ax))) * (1f - S01(150f, 235f, ax));
        float t = S01(KrNorth, KrNorth + 40f, z);
        float h = Mathf.Lerp(front, Mathf.Max(front, crest), t);
        float tyl = 1f - S01(215f, 305f, z);
        h *= tyl;
        h += ((Mathf.PerlinNoise(x / 37f + 4.2f, z / 37f + 1.3f) - .5f) * 4.5f + (Mathf.PerlinNoise(x / 9f + 2.1f, z / 9f + 8.4f) - .5f) * 1.1f) * t * tyl;
        return Mathf.Max(0f, h);
    }

    static float KrWys(float x, float z)
    {
        float d = KrOdl(x, z);
        float amp = S01(12f, 150f, d) * 3.4f;
        float h = -.03f + amp * ((Mathf.PerlinNoise(x / 95f + 3.1f, z / 95f + 7.7f) - .45f) * 2f + (Mathf.PerlinNoise(x / 33f + 11f, z / 33f + 5f) - .5f) * .7f);
        if (z > KrNorth)
        {
            h += KrWzgorze(x, z);
            float ax = Mathf.Abs(x);
            if (ax <= 20.3f) h = Mathf.Max(h, 9.27f);                                                   // korona ściany portalu
            if (z <= KrNorth + TunDl + 8f) h = Mathf.Max(h, 8.37f * (1f - S01(6.5f, 12f, ax)));      // nad rurą tunelu ≥ 8.4 m
        }
        return h;
    }

    static Vector3 KrNormalna(float x, float z)
    {
        const float e = 1f;
        return new Vector3(KrWys(x - e, z) - KrWys(x + e, z), 2f * e, KrWys(x, z - e) - KrWys(x, z + e)).normalized;
    }

    // kolor wierzchołka: R skała, G ściółka, B pole (suchsza trawa)
    static Color KrKolor(float x, float z, Vector3 n)
    {
        float slope = 1f - n.y, hw = z > KrNorth ? KrWzgorze(x, z) : 0f;
        float r = S01(.18f, .42f, slope);
        if (hw > 3f) r = Mathf.Max(r, S01(.66f, .76f, Mathf.PerlinNoise(x / 13f + 9.3f, z / 13f + 2.2f)) * .9f);
        float g = 0f;
        foreach (var t in krDrzewa) { float dx = t.x - x, dz = t.z - z, d2 = dx * dx + dz * dz; if (d2 < 30f) g = Mathf.Max(g, (1f - Mathf.Sqrt(d2) / 5.5f) * .65f); }
        float d = KrOdl(x, z), b = 0f;
        if (d > 25f && hw < 1f)
        {
            // pola w pasach (wschód i południe), łąka przy płocie i na zachodzie
            int pas = Mathf.FloorToInt((x * .8f + z * .35f) / 48f);
            float los = Mathf.Abs(Mathf.Sin(pas * 12.9898f) * 43758.5453f) % 1f;
            float strona = x > KrX1 ? 1f : z < KrZ0 ? .9f : .35f;
            b = (los > .45f ? .55f + los * .45f : 0f) * strona * S01(25f, 45f, d);
        }
        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), 1f);
    }

    // Linie siatki w jednej osi: na zewnątrz prostokąta coraz rzadziej (wyjscie: do odległości d krok k), w środku krokW,
    // a przy osi (|v| < srodek) krokSrodek (kształt zbocza nad portalem).
    static List<float> KrOs(float in0, float in1, float krokW, float krokSrodek, float srodek, (float d, float krok)[] wyjscie)
    {
        var l = new List<float>();
        var neg = new List<float>(); float dd = 0f;
        foreach (var (d, krok) in wyjscie) while (dd < d) { dd += krok; neg.Add(in0 - dd); }
        neg.Reverse(); l.AddRange(neg);
        for (float v = in0; v < in1 - .01f;)
        {
            l.Add(v);
            v = Mathf.Min(in1, v + (Mathf.Abs(v) < srodek ? krokSrodek : krokW));
        }
        l.Add(in1);
        dd = 0f;
        foreach (var (d, krok) in wyjscie) while (dd < d) { dd += krok; l.Add(in1 + dd); }
        return l;
    }

    sealed class KrSiatka
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Color> c = new List<Color>();
        public readonly List<int> i = new List<int>(); public readonly Dictionary<long, int> idx = new Dictionary<long, int>();
    }
    static int krSiatki;

    static GameObject KrObiekt(string nazwa, List<Vector3> v, List<Vector3> n, List<Color> c, List<int> i, Material m, bool cien, List<Vector2> uv = null, List<Vector2> uv2 = null, Bounds? granice = null)
    {
        if (v.Count == 0 || !m) return null;
        var mesh = new Mesh { name = "Kraniec " + nazwa, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); if (n != null) mesh.SetNormals(n); if (c != null) mesh.SetColors(c);
        if (uv != null) mesh.SetUVs(0, uv); if (uv2 != null) mesh.SetUVs(1, uv2);
        mesh.SetTriangles(i, 0);
        if (n == null) mesh.RecalculateNormals();
        mesh.RecalculateBounds(); if (granice.HasValue) mesh.bounds = granice.Value;
        AssetDatabase.CreateAsset(mesh, Out + "/kraniec_" + (krSiatki++) + ".asset");
        var g = new GameObject("Kraniec: " + nazwa); g.transform.SetParent(Group(group), false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = cien ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
        r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return g;
    }

    static Material KrTerenMat()
    {
        if (krTeren) return krTeren;
        var sh = Shader.Find("Braxhorn/OsiedleDal");
        if (!sh) { rep.notes.Add("teren: brak shadera Braxhorn/OsiedleDal — trawa osiedla"); krTeren = Grass; return krTeren; }
        var m = new Material(sh) { name = "Osiedle: teren za płotem (łąki, pola, wzgórze)" };
        string T(string id) => Tex + id + "/albedo.jpg";
        if (File.Exists(T("grass004"))) m.SetTexture("_MainTex", Map(T("grass004"), false, true));
        var g = Grass; if (g) m.SetColor("_Color", g.color * new Color(1.02f, 1.02f, .96f, 1f));
        m.SetFloat("_Tile", 3f);
        string skala = File.Exists(T("rock_face")) ? "rock_face" : "rough_block_wall";
        if (File.Exists(T(skala))) m.SetTexture("_RockTex", Map(T(skala), false, true));
        m.SetFloat("_RockTile", skala == "rock_face" ? 6f : 3f);
        if (File.Exists(T("brown_mud_leaves_01"))) m.SetTexture("_SoilTex", Map(T("brown_mud_leaves_01"), false, true));
        m.SetFloat("_MglaOd", .58f); m.SetFloat("_MglaDo", 420f);
        PasmoNaMaterial(m);
        m.SetFloat("_PasmoJest", pasmo ? 1f : 0f);   // bez pasa panoramy: tylko mgła sceny (nie czarny kolor)
        AssetDatabase.CreateAsset(m, Out + "/teren_za_plotem.mat"); mats["teren za płotem"] = m; tile[m] = 1f;
        rep.notes.Add("teren: skała " + skala);
        return krTeren = m;
    }

    static void Teren()
    {
        group = "Kraniec mapy (teren dookoła)";
        var m = KrTerenMat();
        var outw = new[] { (30f, 3f), (100f, 6f), (250f, 14f), (450f, 30f) };
        var xs = KrOs(KrX0, KrX1, 6f, 3f, 46f, outw);
        // z: południe ← prostokąt (6 m) → północ: przy portalu gęsto (2.5 m), potem rzadziej
        var zs = new List<float>();
        { float dd = 0f; var neg = new List<float>(); foreach (var (d, k) in outw) while (dd < d) { dd += k; neg.Add(KrZ0 - dd); } neg.Reverse(); zs.AddRange(neg); }
        for (float z = KrZ0; z < KrNorth - .01f; z = Mathf.Min(KrNorth, z + 6f)) zs.Add(z);
        zs.Add(KrNorth);
        { float z = KrNorth; foreach (var (d, k) in new[] { (40f, 2.5f), (130f, 5f), (230f, 10f), (420f, 25f) }) while (z < KrNorth + d) { z += k; zs.Add(z); } }
        var reg = new Dictionary<string, KrSiatka> { { "północ (wzgórze)", new KrSiatka() }, { "południe", new KrSiatka() }, { "zachód", new KrSiatka() }, { "wschód", new KrSiatka() } };
        int V(KrSiatka s, int ix, int iz)
        {
            long key = ((long)ix << 32) | (uint)iz;
            if (s.idx.TryGetValue(key, out var k)) return k;
            float x = xs[ix], z = zs[iz];
            var nn = KrNormalna(x, z);
            k = s.v.Count; s.v.Add(new Vector3(x, KrWys(x, z), z)); s.n.Add(nn); s.c.Add(KrKolor(x, z, nn)); s.idx[key] = k; return k;
        }
        int quads = 0;
        for (int ix = 0; ix < xs.Count - 1; ix++)
            for (int iz = 0; iz < zs.Count - 1; iz++)
            {
                float cx = (xs[ix] + xs[ix + 1]) * .5f, cz = (zs[iz] + zs[iz + 1]) * .5f;
                if (cx > KrX0 && cx < KrX1 && cz > KrZ0 && cz < KrNorth) continue;      // wnętrze osiedla
                var s = cz > KrNorth ? reg["północ (wzgórze)"] : cz < KrZ0 ? reg["południe"] : cx < KrX0 ? reg["zachód"] : reg["wschód"];
                int a = V(s, ix, iz), b = V(s, ix + 1, iz), c = V(s, ix + 1, iz + 1), d = V(s, ix, iz + 1);
                s.i.AddRange(new[] { a, d, c, a, c, b }); quads++;
            }
        // skalne skrzydła obok ściany portalu: pionowa ściana skały od ziemi do zbocza na linii z = 101 (|x| 20.3…31)
        var sk = reg["północ (wzgórze)"];
        foreach (float sx in new[] { -1f, 1f })
        {
            var xx = xs.Where(x => sx * x >= 20.29f && sx * x <= 31.01f).OrderBy(x => sx * x).ToList();
            for (int q = 0; q < xx.Count - 1; q++)
            {
                float xa = xx[q], xb = xx[q + 1], ha = KrWys(xa, KrNorth), hb = KrWys(xb, KrNorth);
                if (ha < .05f && hb < .05f) continue;
                int k = sk.v.Count; var rock = new Color(1f, 0f, 0f, 1f); var nb = new Vector3(0, .15f, -1f).normalized;
                sk.v.AddRange(new[] { new Vector3(xa, -.05f, KrNorth - .02f), new Vector3(xb, -.05f, KrNorth - .02f), new Vector3(xb, hb, KrNorth - .02f), new Vector3(xa, ha, KrNorth - .02f) });
                sk.n.AddRange(Enumerable.Repeat(nb, 4)); sk.c.AddRange(Enumerable.Repeat(rock, 4));
                sk.i.AddRange(new[] { k, k + 3, k + 2, k, k + 2, k + 1, k, k + 2, k + 3, k, k + 1, k + 2 });   // obie strony (widoczna od osiedla)
            }
        }
        foreach (var kv in reg) KrObiekt("teren " + kv.Key, kv.Value.v, kv.Value.n, kv.Value.c, kv.Value.i, m, kv.Key.StartsWith("północ"));
        // głazy na stoku nad tunelem i na zboczach (bryły z szumem, skała jak w wychodniach)
        var gl = new KrSiatka(); var rnd = new System.Random(5150); int glazy = 0;
        foreach (var (gx, gz, r) in new[] { (-14f, 104.5f, 1.6f), (12.5f, 105.5f, 2.1f), (17.5f, 103.2f, 1.2f), (-18.5f, 108f, 1.9f), (-7f, 112f, 1.3f), (6f, 116f, 1.7f),
                                             (24f, 103.5f, 1.4f), (-25f, 104f, 1.8f), (-38f, 118f, 2.4f), (41f, 121f, 2.0f), (-62f, 128f, 2.6f), (70f, 133f, 2.2f), (28f, 131f, 1.5f), (-30f, 137f, 1.7f) })
        { KrGlaz(gl, new Vector3(gx, KrWys(gx, gz) - r * .35f, gz), r, rnd); glazy++; }
        KrObiekt("głazy", gl.v, null, gl.c, gl.i, m, true);
        rep.notes.Add("teren za płotem: " + quads + " pól siatki (łąki, pola, wzgórze nad tunelem), " + glazy + " głazów, bez kolizji");
        Debug.Log("BRAXHORN_HORYZONT teren pól=" + quads + " głazy=" + glazy + " wzgórze_nad_tunelem=tak (zbocze ze skałą i drzewami, nie ściana) drzewa_za_płotem=" + krDrzewa.Count);
    }

    // Głaz: dwudziestościan podzielony raz (80 trójkątów), wierzchołki przesunięte szumem, spłaszczony.
    static void KrGlaz(KrSiatka s, Vector3 c, float r, System.Random rnd)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var p = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                                    new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
        int[] f = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
        var tri = new List<int>(f); var mid = new Dictionary<long, int>();
        int Mid(int a, int b) { long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (mid.TryGetValue(k, out var m)) return m; p.Add((p[a] + p[b]) * .5f); mid[k] = p.Count - 1; return p.Count - 1; }
        var t2 = new List<int>();
        for (int q = 0; q < tri.Count; q += 3) { int a = tri[q], b = tri[q + 1], cc = tri[q + 2], ab = Mid(a, b), bc = Mid(b, cc), ca = Mid(cc, a); t2.AddRange(new[] { a, ab, ca, b, bc, ab, cc, ca, bc, ab, bc, ca }); }
        float sx = .8f + (float)rnd.NextDouble() * .5f, sz = .8f + (float)rnd.NextDouble() * .5f, sy = .55f + (float)rnd.NextDouble() * .25f;
        float o1 = (float)rnd.NextDouble() * 10f, o2 = (float)rnd.NextDouble() * 10f;
        int k0 = s.v.Count;
        foreach (var q in p)
        {
            var d = q.normalized;
            float n = 1f + (Mathf.PerlinNoise(d.x * 1.7f + o1, d.z * 1.7f + d.y + o2) - .5f) * .55f;
            s.v.Add(c + new Vector3(d.x * sx, d.y * sy, d.z * sz) * r * n);
            s.c.Add(new Color(1f, 0f, 0f, 1f));
        }
        for (int q = 0; q + 2 < t2.Count; q += 3)
        {
            int a = k0 + t2[q], b = k0 + t2[q + 1], cc = k0 + t2[q + 2];
            var nrm = Vector3.Cross(s.v[b] - s.v[a], s.v[cc] - s.v[a]);
            if (Vector3.Dot(nrm, (s.v[a] + s.v[b] + s.v[cc]) / 3f - c) >= 0f) s.i.AddRange(new[] { a, b, cc }); else s.i.AddRange(new[] { a, cc, b });
        }
    }

    // ---------------- rośliny za płotem ----------------
    static void RoslinyZaPlotem()
    {
        group = "Kraniec mapy / drzewa za płotem";
        var rnd = new System.Random(8181);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        int drzewa = 0, krzewy = 0;
        bool Sadz(float x, float z, float h, float minOdst = 6f)
        {
            foreach (var t in krDrzewa) if ((t.x - x) * (t.x - x) + (t.z - z) * (t.z - z) < minOdst * minOdst) return false;
            var go = Tree(new Vector3(x, KrWys(x, z) - .12f, z), h, R(0, 360), false);
            if (!go) return false;
            krDrzewa.Add(new Vector3(x, h, z)); drzewa++; return true;
        }
        // zachód: skraj lasu (gęsto przy płocie z przerwami, głębiej coraz gęściej)
        for (int k = 0; k < 900 && drzewa < 36; k++)
        {
            float z = R(KrZ0 + 4f, KrNorth - 8f), d = 9f + Mathf.Pow((float)rnd.NextDouble(), .8f) * 46f;
            if (Mathf.PerlinNoise(z / 40f + 2f, d / 25f) < .38f) continue;               // polany i przerwy w skraju
            Sadz(KrX0 - d, z, R(8f, 12.5f), 6.5f);
        }
        int zach = drzewa;
        // wschód: sad (drzewa owocowe w rzędach co 7 m) i żywopłot wzdłuż płotu
        for (float x = KrX1 + 20f; x < KrX1 + 52f; x += 7f)
            for (float z = -40f; z < 31f; z += 7f)
                if (rnd.NextDouble() > .12) Sadz(x + R(-.4f, .4f), z + R(-.4f, .4f), R(4.8f, 6.2f), 5f);
        group = "Kraniec mapy / krzewy za płotem";
        for (float z = KrZ0 + 6f; z < KrNorth - 4f; z += R(2.4f, 3.6f))
        {
            if (Mathf.PerlinNoise(z / 30f, 7.3f) < .3f) continue;
            if (Bush(new Vector3(KrX1 + 4.5f + R(-.4f, .4f), KrWys(KrX1 + 4.5f, z) - .05f, z), R(1.5f, 2.3f), R(0, 360))) krzewy++;
        }
        group = "Kraniec mapy / drzewa za płotem";
        int wsch = drzewa - zach;
        // południe: pojedyncze drzewa na łące i grupy krzewów
        foreach (var (x, z) in new[] { (-70f, -262f), (-12f, -281f), (38f, -258f), (82f, -296f), (-96f, -318f), (6f, -338f), (58f, -345f), (-48f, -300f) })
            if (Sadz(x + R(-3, 3), z + R(-3, 3), R(9f, 12f), 8f))
            {
                group = "Kraniec mapy / krzewy za płotem";
                for (int q = 0; q < 3; q++) if (Bush(new Vector3(x + R(-5, 5), KrWys(x, z) - .05f, z + R(-5, 5)), R(1.4f, 2.2f), R(0, 360))) krzewy++;
                group = "Kraniec mapy / drzewa za płotem";
            }
        int pol = drzewa - zach - wsch;
        // północ: las na zboczach wzgórza (bez drzew na środku grzbietu — widok miasta za tunelem) i ramę portalu
        for (int k = 0; k < 1400 && drzewa - zach - wsch - pol < 40; k++)
        {
            float x = R(-175f, 175f), z = R(KrNorth + 6f, 235f), ax = Mathf.Abs(x);
            if (ax < 58f && z > KrNorth + 14f) continue;              // grzbiet nad tunelem otwarty (miasto za wzgórzem)
            if (ax < 20f) continue;
            if (KrWzgorze(x, z) < 1.2f || Mathf.PerlinNoise(x / 45f + 9f, z / 45f) < .33f) continue;
            Sadz(x, z, R(9f, 13f), 7f);
        }
        foreach (var (x, z) in new[] { (-16.5f, 109f), (17.5f, 110.5f) }) Sadz(x, z, R(6.5f, 7.5f), 4f);   // dwa mniejsze drzewa po bokach nad portalem
        group = "Kraniec mapy / krzewy za płotem";
        foreach (var (x, z) in new[] { (-9f, 103.4f), (-4.5f, 106f), (3f, 104.2f), (8.5f, 103.6f), (11f, 108f), (-12f, 107.5f), (0f, 109.5f), (-20f, 103f), (21f, 104f), (14.5f, 113f), (-15f, 115f), (6f, 120f), (-5f, 123f) })
            if (Bush(new Vector3(x, KrWys(x, z) - .08f, z), R(1.5f, 2.4f), R(0, 360))) krzewy++;
        rep.notes.Add("za płotem: " + drzewa + " drzew (zachód " + zach + ", sad na wschodzie " + wsch + ", południe " + pol + ", wzgórze " + (drzewa - zach - wsch - pol) + "), " + krzewy + " krzewów");
        Debug.Log("BRAXHORN_HORYZONT drzewa_za_płotem=" + drzewa + " (W las " + zach + ", E sad " + wsch + ", S łąka " + pol + ", N wzgórze " + (drzewa - zach - wsch - pol) + ") krzewy=" + krzewy);
    }

    // ---------------- dalsze plany lasu (karty) ----------------
    static void DalszyLas()
    {
        group = "Kraniec mapy (teren dookoła)";
        const string path = Mdl + "drzewo_klaster/las.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        var sh = Shader.Find("Braxhorn/OsiedleDalLas");
        if (!imp || !sh) { rep.notes.Add("dalszy las: brak las.png albo shadera"); Debug.Log("BRAXHORN_HORYZONT dalszy_las=0 (brak las.png / shadera)"); return; }
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
        imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f; imp.wrapMode = TextureWrapMode.Clamp;
        imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        var m = new Material(sh) { name = "Osiedle: dalszy las (karty drzew)" };
        m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); m.SetFloat("_Cutoff", .45f);
        m.SetFloat("_MglaOd", .58f); m.SetFloat("_MglaDo", 420f);
        PasmoNaMaterial(m);
        m.SetFloat("_PasmoJest", pasmo ? 1f : 0f);
        AssetDatabase.CreateAsset(m, Out + "/dalszy_las.mat"); mats["dalszy las"] = m; tile[m] = 1f; krLas = m;
        var rnd = new System.Random(6464);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        var reg = new Dictionary<string, (List<Vector3> v, List<Vector2> uv, List<Vector2> uv2, List<int> i)>();
        int n = 0;
        void Karta(string region, float x, float z, float h)
        {
            if (!reg.TryGetValue(region, out var r)) reg[region] = r = (new List<Vector3>(), new List<Vector2>(), new List<Vector2>(), new List<int>());
            var baza = new Vector3(x, KrWys(x, z) - .3f, z);
            int wariant = rnd.Next(4); float u0 = wariant * .25f, u1 = u0 + .25f, w = h * R(.8f, 1.0f);
            int k = r.v.Count;
            r.v.AddRange(new[] { baza, baza, baza, baza });
            r.uv.AddRange(new[] { new Vector2(u0, 0), new Vector2(u1, 0), new Vector2(u1, 1), new Vector2(u0, 1) });
            r.uv2.AddRange(new[] { new Vector2(-w * .5f, 0), new Vector2(w * .5f, 0), new Vector2(w * .5f, h), new Vector2(-w * .5f, h) });
            r.i.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            n++;
        }
        bool Wolne(float x, float z, float od)
        {
            foreach (var t in krDrzewa) if ((t.x - x) * (t.x - x) + (t.z - z) * (t.z - z) < od * od) return false;
            return true;
        }
        // zachód: las od ~60 m za płotem, gęsty (z polanami)
        for (int k = 0; k < 9000 && n < 700; k++)
        {
            float x = KrX0 - R(58f, 215f), z = R(KrZ0 - 120f, KrNorth + 180f);
            if (Mathf.PerlinNoise(x / 55f + 1f, z / 55f + 3f) < .36f || !Wolne(x, z, 7f)) continue;
            Karta("zachód", x + R(-1, 1), z, R(10f, 15f));
        }
        int w0 = n;
        // południe: las za łąką (od ~85 m)
        for (int k = 0; k < 9000 && n - w0 < 520; k++)
        {
            float x = R(KrX0 - 150f, KrX1 + 150f), z = KrZ0 - R(85f, 210f);
            if (Mathf.PerlinNoise(x / 60f + 5f, z / 60f + 8f) < .4f || !Wolne(x, z, 7f)) continue;
            Karta("południe", x, z, R(10f, 15f));
        }
        int s0 = n;
        // wschód: szpalery drzew na miedzach pól
        foreach (float lx in new[] { KrX1 + 92f, KrX1 + 150f, KrX1 + 205f })
            for (float z = KrZ0 - 60f; z < KrNorth + 120f; z += R(6f, 9f))
                if (Mathf.PerlinNoise(lx / 20f, z / 35f) > .3f) Karta("wschód", lx + R(-2, 2), z, R(9f, 13f));
        foreach (float lz in new[] { -150f, -40f, 50f })
            for (float x = KrX1 + 70f; x < KrX1 + 215f; x += R(6f, 9f))
                if (Mathf.PerlinNoise(x / 35f, lz / 20f) > .35f) Karta("wschód", x, lz + R(-2, 2), R(9f, 12f));
        int e0 = n;
        // północ: las na wzgórzu za grzbietem (bez środka — miasto za tunelem widoczne nad grzbietem)
        for (int k = 0; k < 9000 && n - e0 < 420; k++)
        {
            float x = R(-300f, 300f), z = R(150f, 300f), ax = Mathf.Abs(x);
            if (ax < 78f || KrWzgorze(x, z) < 1.5f && z < 200f) continue;
            if (Mathf.PerlinNoise(x / 50f + 7f, z / 50f + 2f) < .32f || !Wolne(x, z, 7f)) continue;
            Karta("północ", x, z, R(11f, 16f));
        }
        foreach (var kv in reg)
        {
            var b = new Bounds(); bool first = true;
            foreach (var p in kv.Value.v) { if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p); }
            b.Expand(new Vector3(18f, 36f, 18f)); b.center += Vector3.up * 8f;
            KrObiekt("dalszy las " + kv.Key, kv.Value.v, Enumerable.Repeat(Vector3.up, kv.Value.v.Count).ToList(), null, kv.Value.i, m, false, kv.Value.uv, kv.Value.uv2, b);
        }
        rep.notes.Add("dalszy las: " + n + " kart drzew (zachód " + w0 + ", południe " + (s0 - w0) + ", wschód " + (e0 - s0) + ", północ " + (n - e0) + ")");
        Debug.Log("BRAXHORN_HORYZONT dalszy_las=" + n + " kart (W " + w0 + ", S " + (s0 - w0) + ", E szpalery " + (e0 - s0) + ", N za grzbietem " + (n - e0) + ") trójkątów=" + n * 2);
    }
}
