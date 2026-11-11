using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — mała architektura i meble tyłów W SKALI BRAXA (k = BraxClaudeOsiedle.K = 2.33 / 1.80 = 1.294), budowane kodem.
// Zgłoszenia 13.1: „ławki rozjebane” (modele składane z części: modular_street_seating rozsypany, painted_wooden_bench jak ława
// kościelna), kosz ucięty, latarnia z głowicą przy ziemi. Tu każdy element to JEDNA siatka (albo kilka na materiał) liczona
// z wymiarów rzeczywistych × k, stawiana równo na podłożu (y = poziom nawierzchni), z kolizją i — ławki — z punktem BraxSeat.
// Tekstury: mapowanie planarne w metrach świata (cegła, beton, kostka nie mają szwów między ścianami), kafel = wymiar
// rzeczywisty skanu × k (cegła red_brick_03: 1.0 m × k — 13.1 miało 3 m × 3 = 9 m na kafel → „ogromne cegły”).
public static class BraxRegionMeble
{
    public static float K => BraxClaudeOsiedle.K;

    // ------------------------------------------------------------------ budowa siatek (UV planarne w metrach świata)
    public sealed class Bud
    {
        readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> t = new List<int>();
        public int Count => v.Count;
        static Vector2 UV(Vector3 p, Vector3 nn)
        {
            float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
            if (ax >= az) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var nn = Vector3.Cross(b - a, c - a); if (nn.sqrMagnitude < 1e-12f) nn = Vector3.Cross(c - a, d - a);
            if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int i = 0; i < 4; i++) { n.Add(nn); uv.Add(UV(v[s + i], nn)); }
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            var nn = Vector3.Cross(b - a, c - a); if (nn.sqrMagnitude < 1e-12f) return; nn.Normalize();
            int s = v.Count; v.Add(a); v.Add(b); v.Add(c);
            for (int i = 0; i < 3; i++) { n.Add(nn); uv.Add(UV(v[s + i], nn)); }
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }
        /// <summary>Prostopadłościan; rot obraca wokół środka.</summary>
        public void Box(Vector3 c, Vector3 size, Quaternion rot)
        {
            var h = size * .5f;
            Vector3 P(float x, float y, float z) => c + rot * new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
            Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
            Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
            Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
            Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1));
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        }
        public void Box(Vector3 c, Vector3 size) => Box(c, size, Quaternion.identity);
        /// <summary>Box z min/max (wygodniej dla ścian).</summary>
        public void MinMax(Vector3 a, Vector3 b) => Box((a + b) * .5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z)));
        /// <summary>Walec (rura) z denkami.</summary>
        public void Cyl(Vector3 a, Vector3 b, float r, int sides = 12, float r2 = -1f, bool caps = true)
        {
            if (r2 < 0) r2 = r;
            var ax = (b - a).normalized; var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(ax, u);
            for (int i = 0; i < sides; i++)
            {
                float a0 = 2 * Mathf.PI * i / sides, a1 = 2 * Mathf.PI * (i + 1) / sides;
                Vector3 d0 = u * Mathf.Cos(a0) + w * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + w * Mathf.Sin(a1);
                int s = v.Count;
                v.Add(a + d0 * r); v.Add(a + d1 * r); v.Add(b + d1 * r2); v.Add(b + d0 * r2);
                n.Add(d0); n.Add(d1); n.Add(d1); n.Add(d0);
                float l = (b - a).magnitude, c0 = (float)i / sides * 2 * Mathf.PI * r, c1 = (float)(i + 1) / sides * 2 * Mathf.PI * r;
                uv.Add(new Vector2(c0, 0)); uv.Add(new Vector2(c1, 0)); uv.Add(new Vector2(c1, l)); uv.Add(new Vector2(c0, l));
                t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
                if (caps) { Tri(a, a + d1 * r, a + d0 * r); Tri(b, b + d0 * r2, b + d1 * r2); }
            }
        }
        public Mesh Build(string name)
        {
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }
        /// <summary>Obiekt sceny z tej siatki (pusta siatka = null). kolizja: siatkowa (dla terenu/schodów nie używać).</summary>
        public GameObject Obiekt(string name, Transform parent, Material mat, bool kolizja = false, bool cienie = true)
        {
            if (v.Count == 0 || !mat) return null;
            var mesh = Build(name);
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            if (kolizja) g.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);
            return g;
        }
    }

    /// <summary>Kolizja pudełkowa (osobny obiekt, obrót dowolny).</summary>
    public static BoxCollider Kolizja(Transform parent, string name, Vector3 c, Vector3 size, Quaternion rot)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = c; g.transform.rotation = rot;
        var b = g.AddComponent<BoxCollider>(); b.size = size; return b;
    }
    public static BoxCollider Kolizja(Transform parent, string name, Vector3 c, Vector3 size) => Kolizja(parent, name, c, size, Quaternion.identity);

    // ------------------------------------------------------------------ materiały (cache na build)
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static BraxRegion.Report dla;
    static void Stan() { if (dla != BraxRegion.Rep) { dla = BraxRegion.Rep; mats.Clear(); } }

    /// <summary>
    /// Materiał z teksturą: kafel = wymiar rzeczywisty skanu (m) × k. Kolejno: tekstury osiedla (albedo/normal/ao),
    /// Poly Haven regionu (diffuse/normal/arm), CityRebuild (2k). Brak wszystkich = kolor jednolity (matowy) i notatka.
    /// </summary>
    public static Material Tex(string nazwa, float realneMetry, Color tint, float szorstkosc, params string[] ids)
    {
        Stan();
        if (mats.TryGetValue(nazwa, out var have) && have) return have;
        float metres = realneMetry * K;
        Material m = null;
        foreach (var id in ids)
        {
            string os = "Assets/Source/Claude/Osiedle/textures/" + id + "/";
            if (File.Exists(os + "albedo.jpg"))
            {
                var d = Load(os + "albedo.jpg"); var nr = File.Exists(os + "normal.jpg") ? Load(os + "normal.jpg") : null;
                if (!d) continue;
                m = BraxCityMaterials.Textured(nazwa + " [" + id + "]", d, nr, null, 1f / metres);
                if (File.Exists(os + "ao.jpg")) { var ao = Load(os + "ao.jpg"); if (ao) { m.SetTexture("_OcclusionMap", ao); m.SetTextureScale("_OcclusionMap", Vector2.one / metres); m.SetFloat("_OcclusionStrength", .8f); } }
                m.SetFloat("_Roughness", szorstkosc); m.SetFloat("_Metallic", 0f);
                break;
            }
            if (BraxRegion.HasTexture(id))
            {
                try { m = BraxRegion.Pbr(nazwa, metres, null, id); } catch (Exception) { m = null; }
                if (m) break;
            }
        }
        if (!m)
        {
            BraxRegion.Note("tyły: brak tekstur " + string.Join("/", ids) + " dla „" + nazwa + "” — kolor jednolity");
            m = BraxCityMaterials.Surface(nazwa, tint, szorstkosc);
        }
        else { m.SetColor("_Color", tint); EditorUtility.SetDirty(m); }
        mats[nazwa] = m; return m;
    }

    static Texture2D Load(string p) => BraxRegion.TeksturaRaz(p);   // 10.11: bez ponownego importu przy każdym meblu

    /// <summary>Materiał gładki (okucia, lakier, szkło). Kolor z alfą &lt; 1 = przezroczysty.</summary>
    public static Material Gladki(string nazwa, Color c, float szorstkosc, float metal = 0f) => BraxCityMaterials.Surface(nazwa, c, szorstkosc, metal);

    /// <summary>Materiał świecący (oprawy, kasetony): bez światła czasu rzeczywistego, tylko emisja.</summary>
    public static Material Swiecacy(string nazwa, Color c, float moc)
    {
        Stan();
        if (mats.TryGetValue("emisja|" + nazwa, out var have) && have) return have;
        var src = BraxCityMaterials.Surface(nazwa, c, .6f);
        var m = new Material(src) { name = nazwa + " (emisja)" };
        m.SetColor("_EmissionColor", c * moc); m.EnableKeyword("_EMISSION");
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Materials/emisja_" + mats.Count + ".mat");
        mats["emisja|" + nazwa] = m; return m;
    }

    // wspólne materiały mebli
    public static Material Drewno => Tex("Meble: drewno listew (olejowane)", 1.5f, new Color(.78f, .62f, .46f), .7f, "wood_planks", "wooden_planks", "weathered_planks");
    public static Material Zeliwo => Gladki("Meble: żeliwo grafitowe malowane", new Color(.10f, .11f, .11f), .55f, .5f);
    public static Material StalSzara => Gladki("Meble: stal ocynkowana", new Color(.55f, .56f, .57f), .45f, .8f);
    public static Material StalCzarna => Gladki("Meble: stal czarna", new Color(.06f, .065f, .07f), .4f, .6f);
    public static Material Beton => Tex("Tyły: beton architektoniczny", 2.0f, new Color(.80f, .79f, .76f), .9f, "concrete_floor_02", "concrete_pavement_02", "concrete_wall_008");
    public static Material Szklo => Gladki("Tyły: szkło bezpieczne (lekko zielone)", new Color(.62f, .74f, .72f, .30f), .06f, .1f);

    // ------------------------------------------------------------------ meble zewnętrzne (wymiary rzeczywiste × k)

    /// <summary>
    /// Ławka parkowa (żeliwne boki, 5 listew siedziska, 3 listwy oparcia): długość 1.80, siedzisko 0.45 m, oparcie do 0.85 m — × k.
    /// Przodem (strona siedzenia) w kierunku yaw (0 = +z). Jedna siatka na materiał, kolizja, punkt BraxSeat.
    /// </summary>
    public static GameObject Lawka(Transform parent, Vector3 p, float yaw, string nazwa = "Ławka parkowa")
    {
        float k = K; var rot = Quaternion.Euler(0, yaw, 0);
        var g = new GameObject(nazwa); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var drew = new Bud(); var zel = new Bud();
        Vector3 L(float x, float y, float z) => p + rot * new Vector3(x * k, y * k, z * k);
        float len = 1.80f, sh = .45f;
        // boki żeliwne: noga przednia, tylna, wspornik siedziska, ramię oparcia, podłokietnik
        foreach (float sx in new[] { -len * .5f + .12f, len * .5f - .12f })
        {
            zel.Box(L(sx, sh * .5f, .17f), new Vector3(.05f * k, sh * k, .06f * k), rot);
            zel.Box(L(sx, sh * .5f, -.20f), new Vector3(.05f * k, sh * k, .06f * k), rot);
            zel.Box(L(sx, sh - .03f, -.01f), new Vector3(.05f * k, .05f * k, .48f * k), rot);
            var oparcie = rot * Quaternion.Euler(-15f, 0, 0);
            zel.Box(L(sx, .66f, -.25f), new Vector3(.05f * k, .46f * k, .05f * k), oparcie);
            zel.Box(L(sx, .64f, .02f), new Vector3(.05f * k, .04f * k, .40f * k), rot);   // podłokietnik
            zel.Box(L(sx, .555f, .20f), new Vector3(.05f * k, .18f * k, .04f * k), rot);
            zel.Box(L(sx, .04f, -.015f), new Vector3(.06f * k, .03f * k, .44f * k), rot);   // stopka
        }
        // listwy siedziska (5 × 9 cm, szczelina 1.2 cm)
        for (int i = 0; i < 5; i++) drew.Box(L(0, sh + .018f, .19f - i * .102f), new Vector3((len - .02f) * k, .035f * k, .09f * k), rot);
        // oparcie (3 listwy, pochylone 15°)
        var tilt = rot * Quaternion.Euler(-15f, 0, 0);
        for (int i = 0; i < 3; i++)
        {
            float yy = .56f + i * .105f, zz = -.235f - (yy - .56f) * .27f;
            drew.Box(L(0, yy, zz), new Vector3((len - .02f) * k, .09f * k, .03f * k), tilt);
        }
        drew.Obiekt("Ławka: listwy", g.transform, Drewno);
        zel.Obiekt("Ławka: żeliwo", g.transform, Zeliwo);
        Kolizja(g.transform, "Ławka: kolizja", L(0, sh * .5f + .05f, -.02f), new Vector3(len * k, (sh + .1f) * k, .5f * k), rot);
        Kolizja(g.transform, "Ławka: kolizja oparcia", L(0, .68f, -.26f), new Vector3(len * k, .38f * k, .08f * k), rot);
        var seat = new GameObject("Siedzisko: " + nazwa); seat.transform.SetParent(g.transform, false);
        seat.transform.position = L(0, sh + .036f, .02f); seat.transform.rotation = rot;
        var s = seat.AddComponent<BraxSeat>(); s.label = "ŁAWKA"; s.lie = false; s.surfaceY = p.y + (sh + .036f) * k;
        return g;
    }

    /// <summary>Kosz uliczny na słupku (stal malowana grafit, wkład ocynkowany): 0.95 m, korpus Ø 0.42 — × k.</summary>
    public static GameObject Kosz(Transform parent, Vector3 p, float yaw = 0f)
    {
        float k = K; var g = new GameObject("Kosz uliczny"); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var rot = Quaternion.Euler(0, yaw, 0);
        var a = new Bud(); var b = new Bud();
        a.Cyl(p, p + Vector3.up * .95f * k, .035f * k, 10);                                 // słupek
        a.Cyl(p + rot * new Vector3(0, .35f * k, .24f * k), p + rot * new Vector3(0, .92f * k, .24f * k), .21f * k, 16, .22f * k);   // korpus
        a.Cyl(p + rot * new Vector3(0, .92f * k, .24f * k), p + rot * new Vector3(0, .95f * k, .24f * k), .235f * k, 16);   // obręcz
        a.Box(p + Vector3.up * .01f * k, new Vector3(.18f * k, .02f * k, .18f * k));        // stopa
        b.Cyl(p + rot * new Vector3(0, .935f * k, .24f * k), p + rot * new Vector3(0, .955f * k, .24f * k), .18f * k, 16);   // wkład
        a.Obiekt("Kosz: korpus", g.transform, Gladki("Kosz: stal malowana grafit", new Color(.12f, .14f, .13f), .5f, .3f));
        b.Obiekt("Kosz: wkład", g.transform, StalSzara);
        Kolizja(g.transform, "Kosz: kolizja", p + rot * new Vector3(0, .48f * k, .2f * k), new Vector3(.5f * k, .96f * k, .55f * k), rot);
        return g;
    }

    /// <summary>Latarnia parkowa: słup stalowy 4.5 m × k, oprawa z kloszem (nie świeci w dzień).</summary>
    public static GameObject Latarnia(Transform parent, Vector3 p, float wys = 4.5f)
    {
        float k = K, h = wys * k; var g = new GameObject("Latarnia parkowa"); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var a = new Bud(); var c = new Bud();
        a.Cyl(p, p + Vector3.up * .5f * k, .11f * k, 12, .09f * k);
        a.Cyl(p + Vector3.up * .5f * k, p + Vector3.up * h, .065f * k, 12, .045f * k);
        a.Cyl(p + Vector3.up * h, p + Vector3.up * (h + .06f * k), .16f * k, 12);
        a.Cyl(p + Vector3.up * (h + .42f * k), p + Vector3.up * (h + .52f * k), .26f * k, 12, .05f * k);   // daszek
        c.Cyl(p + Vector3.up * (h + .06f * k), p + Vector3.up * (h + .42f * k), .15f * k, 12, .22f * k);  // klosz
        a.Obiekt("Latarnia: słup", g.transform, Gladki("Latarnia: stal grafit", new Color(.09f, .10f, .10f), .45f, .55f));
        c.Obiekt("Latarnia: klosz", g.transform, Swiecacy("Latarnia: klosz mleczny", new Color(.92f, .90f, .84f), .35f));
        Kolizja(g.transform, "Latarnia: kolizja", p + Vector3.up * h * .5f, new Vector3(.24f * k, h, .24f * k));
        return g;
    }

    /// <summary>Krata żeliwna wokół pnia (1.2 m × k): otwór w nawierzchni dookoła ma ziemię 3 cm niżej (podawany osobno).</summary>
    public static GameObject KrataDrzewa(Transform parent, Vector3 p)
    {
        float k = K, s = 1.2f * k * .5f; var g = new GameObject("Krata drzewa"); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var a = new Bud(); float y = p.y + .012f;
        a.Box(new Vector3(p.x, y, p.z + s - .03f), new Vector3(2 * s, .024f, .06f)); a.Box(new Vector3(p.x, y, p.z - s + .03f), new Vector3(2 * s, .024f, .06f));
        a.Box(new Vector3(p.x + s - .03f, y, p.z), new Vector3(.06f, .024f, 2 * s)); a.Box(new Vector3(p.x - s + .03f, y, p.z), new Vector3(.06f, .024f, 2 * s));
        for (int i = 1; i < 10; i++)
        {
            float o = -s + i * (2 * s / 10f); if (Mathf.Abs(o) < .22f * k) continue;
            a.Box(new Vector3(p.x + o, y, p.z), new Vector3(.025f, .02f, 2 * s - .06f));
        }
        for (int i = 1; i < 10; i++)
        {
            float o = -s + i * (2 * s / 10f); if (Mathf.Abs(o) < .22f * k) continue;
            a.Box(new Vector3(p.x, y, p.z + o), new Vector3(2 * s - .06f, .02f, .025f));
        }
        a.Obiekt("Krata drzewa: żeliwo", g.transform, Zeliwo, false, false);
        return g;
    }

    /// <summary>Słupek stalowy (blokada wjazdu) 0.9 m × k.</summary>
    public static void Slupek(Transform parent, Bud b, Vector3 p)
    {
        float k = K; b.Cyl(p, p + Vector3.up * .9f * k, .07f * k, 12); b.Cyl(p + Vector3.up * .9f * k, p + Vector3.up * .93f * k, .075f * k, 12);
        Kolizja(parent, "Słupek: kolizja", p + Vector3.up * .45f * k, new Vector3(.16f * k, .9f * k, .16f * k));
    }

    /// <summary>Barierka stalowa (poręcz Ø 48, dwie poprzeczki, słupki co 1.5 m): wys. 1.1 m × k, z a do b po podłożu y.</summary>
    public static void Barierka(Transform parent, Bud b, Vector3 a, Vector3 c, bool kolizja = true)
    {
        float k = K, h = 1.1f * k; var d = c - a; float len = d.magnitude; if (len < .2f) return;
        int n = Mathf.Max(1, Mathf.CeilToInt(len / (1.5f * k)));
        for (int i = 0; i <= n; i++) { var q = Vector3.Lerp(a, c, (float)i / n); b.Cyl(q, q + Vector3.up * h, .03f * k, 8); }
        b.Cyl(a + Vector3.up * h, c + Vector3.up * h, .028f * k, 10);
        b.Cyl(a + Vector3.up * h * .62f, c + Vector3.up * h * .62f, .016f * k, 8);
        b.Cyl(a + Vector3.up * h * .28f, c + Vector3.up * h * .28f, .016f * k, 8);
        if (kolizja)
        {
            var mid = (a + c) * .5f + Vector3.up * h * .5f;
            Kolizja(parent, "Barierka: kolizja", mid, new Vector3(.12f, h, len), Quaternion.LookRotation(new Vector3(d.x, 0, d.z).normalized));
        }
    }

    /// <summary>Napis 3D (test głębi) na tablicy: tekst czytelny dla patrzącego od strony „przod” (kierunek świata).</summary>
    // 11.11 (lista ze zdjęć właściciela: „FRYZJER — napis-duch na szybie”; zasada SZYLDY 3D 9.11): dotąd płaski TextMesh (czcionka
    // systemowa, półprzezroczysty z daleka — „duch”). Teraz LITERY PRZESTRZENNE krojem Bebas (jak szyldy miasta): lico w kolorze
    // napisu, boki ciemniejsze, głębokość 15 % wysokości (2–4 cm), tył napisu w punkcie p (lico w stronę przod) — wszystkie szyldy,
    // tabliczki, semafory, numery i napisy wnętrz R1 jednym wywołaniem. Wysokość wys = wysokość wiersza jak dotąd (wersalik 72 %).
    static readonly Dictionary<string, (Material lico, Material boki)> napisMat = new Dictionary<string, (Material, Material)>();
    public static void Napis(Transform parent, string tekst, Vector3 p, Vector3 przod, float wys, Color kolor)
    {
        if (string.IsNullOrWhiteSpace(tekst)) return;
        przod = przod.normalized; if (przod.sqrMagnitude < .5f) przod = Vector3.forward;
        string klucz = ColorUtility.ToHtmlStringRGB(kolor);
        if (!napisMat.TryGetValue(klucz, out var mm) || !mm.lico)
            napisMat[klucz] = mm = (BraxCityMaterials.Surface("Napis 3D: lico " + klucz, kolor, .4f, .1f),
                                     BraxCityMaterials.Surface("Napis 3D: boki " + klucz, Color.Lerp(kolor, Color.black, .55f), .5f, .2f));
        float cap = wys * .72f, gl = Mathf.Clamp(cap * .15f, .02f, .04f);
        // jak BraxLiteryBebas.Napis(nn): prawo czytającego = up × (−przód), góra liter = prawo × przód
        var up0 = Mathf.Abs(Vector3.Dot(przod, Vector3.up)) > .95f ? Vector3.forward : Vector3.up;
        var r = Vector3.Cross(up0, -przod).normalized; var up = Vector3.Cross(r, przod).normalized;
        var sl = new UlSiatka(); var sb = new UlSiatka();
        BraxLiteryBebas.Napis(sl, sb, tekst, -up * (cap * .5f), r, up, przod, cap, gl, .05f, .002f);
        var g = new GameObject("Napis " + tekst); g.transform.SetParent(parent, false); g.transform.position = p;
        if (sl.Wierzcholki > 0) BraxMiastoA.Obiekt(g.transform, "Napis 3D " + tekst + " (lico)", sl, mm.lico, true);
        if (sb.Wierzcholki > 0) BraxMiastoA.Obiekt(g.transform, "Napis 3D " + tekst + " (boki)", sb, mm.boki, true);
    }

    // ------------------------------------------------------------------ meble wnętrz (× k)

    /// <summary>Biurko biurowe 1.60 × 0.80 × 0.75 m × k (blat, nogi płozowe, blenda). yaw: strona siedzącego = −z lokalnie.</summary>
    public static GameObject Biurko(Transform parent, Vector3 p, float yaw, float dl = 1.6f, string nazwa = "Biurko")
    {
        float k = K; var rot = Quaternion.Euler(0, yaw, 0);
        var g = new GameObject(nazwa); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var top = new Bud(); var st = new Bud();
        Vector3 L(float x, float y, float z) => p + rot * new Vector3(x * k, y * k, z * k);
        top.Box(L(0, .735f, 0), new Vector3(dl * k, .03f * k, .8f * k), rot);
        foreach (float sx in new[] { -dl * .5f + .05f, dl * .5f - .05f })
        {
            st.Box(L(sx, .36f, 0), new Vector3(.05f * k, .72f * k, .06f * k), rot);
            st.Box(L(sx, .02f, 0), new Vector3(.06f * k, .04f * k, .72f * k), rot);
            st.Box(L(sx, .70f, 0), new Vector3(.05f * k, .04f * k, .72f * k), rot);
        }
        st.Box(L(0, .45f, .36f), new Vector3((dl - .12f) * k, .45f * k, .015f * k), rot);   // blenda od strony przejścia
        top.Obiekt(nazwa + ": blat", g.transform, Tex("Biuro: blat dąb jasny", 1.0f, new Color(.86f, .78f, .66f), .55f, "wood_floor", "plank_flooring_02", "wood_planks"));
        st.Obiekt(nazwa + ": stelaż", g.transform, Gladki("Biuro: stelaż biały proszkowy", new Color(.86f, .86f, .85f), .5f, .2f));
        Kolizja(g.transform, nazwa + ": kolizja", L(0, .38f, 0), new Vector3(dl * k, .76f * k, .8f * k), rot);
        return g;
    }

    /// <summary>Krzesło obrotowe biurowe (siedzisko 0.47 m × k), przodem w yaw.</summary>
    static Mesh tapBiuro;
    public static GameObject KrzesloBiurowe(Transform parent, Vector3 p, float yaw)
    {
        float k = K; var rot = Quaternion.Euler(0, yaw, 0);
        var g = new GameObject("Krzesło biurowe"); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var tap = new Bud(); var st = new Bud();
        Vector3 L(float x, float y, float z) => p + rot * new Vector3(x * k, y * k, z * k);
        // 10.11 (12:25 „każde jedno krzesło ma być obite”): siedzisko i oparcie jako miękkie poduchy (zaokrąglone, wypchnięte), nie płyty
        if (!tapBiuro)
        {
            var u = new UlSiatka();
            u.Poducha(new Vector3(-.24f * k, .435f * k, -.23f * k), new Vector3(.24f * k, .51f * k, .23f * k), .03f, .013f);
            u.Poducha(new Vector3(-.23f * k, .56f * k, -.29f * k), new Vector3(.23f * k, 1.06f * k, -.21f * k), .03f, .013f);
            tapBiuro = u.Zbuduj("Biuro: krzesło — tapicerka miękka");
            System.IO.Directory.CreateDirectory(BraxUliceKit.Out);
            BraxZapisAssetu.Utworz(tapBiuro, BraxUliceKit.Out + "/mebel_krzeslo_biurowe_tapicerka.asset");
        }
        var tm = new GameObject("Krzesło: tapicerka (miękka)"); tm.transform.SetParent(g.transform, false); tm.transform.position = p; tm.transform.rotation = rot;
        tm.AddComponent<MeshFilter>().sharedMesh = tapBiuro; tm.AddComponent<MeshRenderer>().sharedMaterial = Gladki("Biuro: tkanina grafitowa", new Color(.17f, .18f, .2f), .95f);
        st.Cyl(L(0, .08f, 0), L(0, .44f, 0), .028f * k, 8);
        for (int i = 0; i < 5; i++) { var d = Quaternion.Euler(0, i * 72f, 0) * new Vector3(0, 0, .3f); st.Box(L(d.x * .5f, .07f, d.z * .5f), new Vector3(.04f * k, .03f * k, .3f * k), rot * Quaternion.Euler(0, i * 72f, 0)); }
        st.Box(L(0, .62f, -.24f), new Vector3(.05f * k, .3f * k, .03f * k), rot);

        st.Obiekt("Krzesło: stelaż", g.transform, StalCzarna);
        Kolizja(g.transform, "Krzesło: kolizja", L(0, .5f, -.02f), new Vector3(.55f * k, 1.0f * k, .55f * k), rot);
        var seat = new GameObject("Siedzisko: krzesło biurowe"); seat.transform.SetParent(g.transform, false);
        seat.transform.position = L(0, .505f, .03f); seat.transform.rotation = rot;
        var s = seat.AddComponent<BraxSeat>(); s.label = "KRZESŁO"; s.surfaceY = p.y + .505f * k;
        return g;
    }

    /// <summary>Sejf stalowy (0.70 × 1.20 × 0.60 m × k): drzwi z pokrętłem szyfrowym, kołem i zawiasami. Przodem w yaw.</summary>
    public static GameObject Sejf(Transform parent, Vector3 p, float yaw)
    {
        float k = K; var rot = Quaternion.Euler(0, yaw, 0);
        var g = new GameObject("SEJF (misja)"); g.transform.SetParent(parent, false); g.transform.position = Vector3.zero;
        var body = new Bud(); var det = new Bud(); var chrom = new Bud();
        Vector3 L(float x, float y, float z) => p + rot * new Vector3(x * k, y * k, z * k);
        body.Box(L(0, .62f, 0), new Vector3(.70f * k, 1.16f * k, .60f * k), rot);
        body.Box(L(0, .02f, 0), new Vector3(.66f * k, .04f * k, .56f * k), rot);
        det.Box(L(-.01f, .62f, .305f), new Vector3(.60f * k, 1.04f * k, .02f * k), rot);                       // drzwi
        foreach (float y in new[] { .30f, .94f }) chrom.Cyl(L(-.30f, y - .06f, .31f), L(-.30f, y + .06f, .31f), .018f * k, 8);   // zawiasy
        chrom.Cyl(L(.10f, .80f, .315f), L(.10f, .80f, .345f), .055f * k, 20);                                 // pokrętło szyfrowe
        chrom.Cyl(L(.10f, .80f, .345f), L(.10f, .80f, .36f), .02f * k, 10);
        var kol = L(.10f, .55f, .33f);
        chrom.Cyl(L(.10f, .55f, .315f), kol, .016f * k, 8);
        for (int i = 0; i < 3; i++) { var d = rot * (Quaternion.Euler(0, 0, i * 120f) * new Vector3(.09f * k, 0, 0)); chrom.Cyl(kol, kol + d, .011f * k, 6); chrom.Box(kol + d * 1.05f, Vector3.one * .03f * k, rot); }
        body.Obiekt("Sejf: korpus", g.transform, Gladki("Sejf: stal lakierowana antracyt", new Color(.13f, .14f, .15f), .35f, .55f));
        det.Obiekt("Sejf: drzwi", g.transform, Gladki("Sejf: drzwi lakier ciemnoszary", new Color(.18f, .19f, .2f), .3f, .55f));
        chrom.Obiekt("Sejf: okucia", g.transform, Gladki("Sejf: chrom", new Color(.78f, .78f, .76f), .18f, .95f));
        Kolizja(g.transform, "Sejf: kolizja", L(0, .62f, 0), new Vector3(.72f * k, 1.2f * k, .62f * k), rot);
        return g;
    }

    /// <summary>Stolik kawiarniany Ø 0.70 × 0.75 m × k na jednej nodze.</summary>
    public static void Stolik(Transform parent, Bud blat, Bud noga, Vector3 p)
    {
        float k = K;
        blat.Cyl(p + Vector3.up * .72f * k, p + Vector3.up * .75f * k, .35f * k, 20);
        noga.Cyl(p + Vector3.up * .02f * k, p + Vector3.up * .72f * k, .03f * k, 8);
        noga.Cyl(p, p + Vector3.up * .02f * k, .22f * k, 16);
        Kolizja(parent, "Stolik: kolizja", p + Vector3.up * .38f * k, new Vector3(.7f * k, .76f * k, .7f * k));
    }

    /// <summary>Krzesło kawiarniane (siedzisko 0.46 m × k) przodem w yaw, z punktem BraxSeat.</summary>
    public static void Krzeslo(Transform parent, Bud drewno, Bud stal, Vector3 p, float yaw)
    {
        float k = K; var rot = Quaternion.Euler(0, yaw, 0);
        Vector3 L(float x, float y, float z) => p + rot * new Vector3(x * k, y * k, z * k);
        // 10.11 (właściciel 12:25: „każde jedno krzesło ma być obite… nic sztucznego”): zamiast deski siedziska i listwy oparcia —
        // krzesło tapicerowane (rama z orzecha, miękkie siedzisko i oparcie ze skóry pikowanej czarnej), wspólne z lokalami ulicy
        BraxUlicePierzeje.KrzesloTapicerowane("Krzesło (tapicerowane)", parent, p, yaw);
        var seat = new GameObject("Siedzisko: krzesło kawiarni"); seat.transform.SetParent(parent, false);
        seat.transform.position = L(0, .48f, .02f); seat.transform.rotation = rot;
        var s = seat.AddComponent<BraxSeat>(); s.label = "KRZESŁO"; s.surfaceY = p.y + .48f * k;
    }

    /// <summary>Model Poly Haven / biblioteki o zadanej wysokości RZECZYWISTEJ × k (null, gdy brak modelu).</summary>
    public static GameObject Model(Transform parent, string etykieta, Vector3 p, float yaw, float wysRealna, bool kolizja, params string[] ids)
    {
        var src = BraxRegion.Model(ids); if (!src) return null;
        var o = BraxRegion.Put(parent, src, etykieta, p, yaw, 1f, false, 0f);
        if (!o) return null;
        var b = BraxRegion.Bounds(o); if (b.size.y < 1e-3f) return o;
        float s = wysRealna * K / b.size.y;
        o.transform.localScale *= s;
        b = BraxRegion.Bounds(o);
        o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
        if (kolizja)
        {
            b = BraxRegion.Bounds(o);
            Kolizja(parent, etykieta + " kolizja", b.center, b.size);
        }
        return o;
    }
}
