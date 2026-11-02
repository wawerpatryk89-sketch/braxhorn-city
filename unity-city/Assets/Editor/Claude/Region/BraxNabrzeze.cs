using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — NABRZEŻE OD NOWA (przebudowa miasta 2.11, wykonawca „nabrzeże”).
// Właściciel (gra 13.1 na telefonie): „dosłownie wszystko do poprawy — od wyjścia z siłowni po morze, port, plaże”.
// Zasady: jak najbardziej realistycznie; co złe — USUWAMY i budujemy od nowa (najpierw zdjąć stare, potem położyć nowe,
// nic na starą geometrię — źródło migotania); SKALA BRAXA: wymiar rzeczywisty × k (k = 2.33 / 1.80 = 1.294).
//
// Ten plik: wspólne narzędzia nabrzeża (plaża, bulwar, marina, port, wyspa):
//  • Zbior — siatki scalane per materiał (jeden obiekt = jedno wywołanie rysowania na materiał), UV w metrach świata
//    (rzut zależny od normalnej), zapis siatek do plików (Assets/PlayableGenerated/ClaudeRegion/Nabrzeze/scal_*.asset —
//    kontrola jakości traktuje „scal_” jako siatkę scaloną przez generator, nie jako jeden mebel);
//  • kolizje pudełkowe (osobne obiekty, bez renderera), napisy 3D z testem głębi, materiały Poly Haven z zamiennikiem.
public static class BraxNabrzeze
{
    public const float Woda = -1.7f;               // lustro wody (BraxCityWaterBuild.WaterY — tego nie zmieniamy)
    public const float Bulwar = .15f;              // nawierzchnia bulwaru (jak chodniki miasta)
    public const float Krawedz = 84f;              // krawędź nabrzeża (x) — dalej morze
    public const string Out = "Assets/PlayableGenerated/ClaudeRegion/Nabrzeze";
    public static float K => BraxClaudeOsiedle.K;  // skala Braxa (1.294)

    static int serial;
    public static readonly List<string> Usuniete = new List<string>();   // raport: co zdjęte przed położeniem nowego

    public static void Przygotuj()
    {
        Directory.CreateDirectory(Out);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    public static void Log(string s) { Debug.Log("BRAXHORN_NABRZEZE " + s); }

    // ---------------- materiały ----------------
    static readonly Dictionary<string, Material> kolory = new Dictionary<string, Material>();

    /// <summary>Materiał z fotoskanu Poly Haven (pierwszy dostępny slug); brak tekstur = kolor zastępczy (raport).</summary>
    public static Material Mat(string name, float metres, Color tint, params string[] slugs)
    {
        try { return BraxRegion.Pbr(name, metres, tint, slugs); }
        catch (Exception) { Log("brak tekstur " + string.Join("/", slugs) + " → kolor (" + name + ")"); return Kolor(name, tint * .85f, .85f); }
    }

    public static Material Kolor(string name, Color c, float roughness = .8f, float metallic = 0f)
    {
        string key = name + ColorUtility.ToHtmlStringRGBA(c) + roughness + "|" + metallic;
        if (kolory.TryGetValue(key, out var m) && m) return m;
        m = BraxCityMaterials.Surface(name, c, roughness, metallic);
        kolory[key] = m; return m;
    }

    // ---------------- siatka z UV w metrach świata ----------------
    public sealed class Siatka
    {
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<Color> c = new List<Color>();
        public readonly List<int> t = new List<int>();
        public int Count => v.Count;

        static Vector2 Rzut(Vector3 p, Vector3 nn)
        {
            float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x, p.z);
            if (ax > az) return new Vector2(p.z, p.y);
            return new Vector2(p.x, p.y);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d)
        {
            var nn = Vector3.Cross(b - a, cc - a).normalized;
            int s = v.Count;
            foreach (var p in new[] { a, b, cc, d }) { v.Add(p); n.Add(nn); uv.Add(Rzut(p, nn)); c.Add(Color.white); }
            t.Add(s); t.Add(s + 1); t.Add(s + 2); t.Add(s); t.Add(s + 2); t.Add(s + 3);
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 cc)
        {
            var nn = Vector3.Cross(b - a, cc - a).normalized;
            int s = v.Count;
            foreach (var p in new[] { a, b, cc }) { v.Add(p); n.Add(nn); uv.Add(Rzut(p, nn)); c.Add(Color.white); }
            t.Add(s); t.Add(s + 1); t.Add(s + 2);
        }

        /// <summary>Prostopadłościan (środek, wymiary, obrót wokół środka).</summary>
        public void Box(Vector3 center, Vector3 size, Quaternion rot)
        {
            Vector3 h = size * .5f;
            Vector3 P(float x, float y, float z) => center + rot * new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1));
            Quad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
            Quad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1));
            Quad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
            Quad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1));
            Quad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        }
        public void Box(Vector3 center, Vector3 size) { Box(center, size, Quaternion.identity); }
        /// <summary>Prostopadłościan z dwóch rogów (min, max).</summary>
        public void BoxMM(Vector3 a, Vector3 b) { Box((a + b) * .5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z))); }

        /// <summary>Rura / pręt od a do b (promień r, zwężenie r2), z dennicami.</summary>
        public void Rura(Vector3 a, Vector3 b, float r, int sides = 10, float r2 = -1f, bool caps = true)
        {
            if (r2 < 0) r2 = r;
            var ax = (b - a).normalized; if (ax.sqrMagnitude < 1e-8f) return;
            var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > .95f ? Vector3.forward : Vector3.up).normalized;
            var w = Vector3.Cross(ax, u);
            int s0 = v.Count;
            for (int i = 0; i <= sides; i++)
            {
                float an = i * Mathf.PI * 2f / sides;
                var dir = u * Mathf.Cos(an) + w * Mathf.Sin(an);
                v.Add(a + dir * r); n.Add(dir); uv.Add(new Vector2(i * 2f * Mathf.PI * r / sides, 0)); c.Add(Color.white);
                v.Add(b + dir * r2); n.Add(dir); uv.Add(new Vector2(i * 2f * Mathf.PI * r / sides, Vector3.Distance(a, b))); c.Add(Color.white);
            }
            for (int i = 0; i < sides; i++)
            {
                int p0 = s0 + i * 2, p1 = p0 + 2;
                t.Add(p0); t.Add(p1); t.Add(p0 + 1); t.Add(p1); t.Add(p1 + 1); t.Add(p0 + 1);   // na zewnątrz (Unity: zgodnie z ruchem wskazówek)
            }
            if (!caps) return;
            for (int e = 0; e < 2; e++)
            {
                var cen = e == 0 ? a : b; float rr = e == 0 ? r : r2; var nn = e == 0 ? -ax : ax;
                int cs = v.Count; v.Add(cen); n.Add(nn); uv.Add(Vector2.zero); c.Add(Color.white);
                for (int i = 0; i <= sides; i++)
                {
                    float an = i * Mathf.PI * 2f / sides; var dir = u * Mathf.Cos(an) + w * Mathf.Sin(an);
                    v.Add(cen + dir * rr); n.Add(nn); uv.Add(new Vector2(dir.x, dir.z) * rr); c.Add(Color.white);
                }
                for (int i = 0; i < sides; i++)
                {
                    if (e == 0) { t.Add(cs); t.Add(cs + 2 + i); t.Add(cs + 1 + i); }
                    else { t.Add(cs); t.Add(cs + 1 + i); t.Add(cs + 2 + i); }
                }
            }
        }

        /// <summary>Bryła z wielokąta (rzut XZ, przeciwnie do ruchu wskazówek patrząc z góry) od y0 do y1.</summary>
        public void Pryzma(IList<Vector2> poly, float y0, float y1)
        {
            int k = poly.Count;
            for (int i = 0; i < k; i++)
            {
                var p = poly[i]; var q = poly[(i + 1) % k];
                Quad(new Vector3(q.x, y0, q.y), new Vector3(p.x, y0, p.y), new Vector3(p.x, y1, p.y), new Vector3(q.x, y1, q.y));
            }
            for (int i = 1; i < k - 1; i++)
            {
                Tri(new Vector3(poly[0].x, y1, poly[0].y), new Vector3(poly[i + 1].x, y1, poly[i + 1].y), new Vector3(poly[i].x, y1, poly[i].y));
                Tri(new Vector3(poly[0].x, y0, poly[0].y), new Vector3(poly[i].x, y0, poly[i].y), new Vector3(poly[i + 1].x, y0, poly[i + 1].y));
            }
        }

        public void Dolacz(Siatka o, Matrix4x4 m)
        {
            int s = v.Count; var nm = m.inverse.transpose;
            for (int i = 0; i < o.v.Count; i++)
            {
                var p = m.MultiplyPoint3x4(o.v[i]); var nn = nm.MultiplyVector(o.n[i]).normalized;
                v.Add(p); n.Add(nn); uv.Add(Rzut(p, nn)); c.Add(o.c[i]);
            }
            foreach (var i in o.t) t.Add(s + i);
        }

        public Mesh Zbuduj(string name, bool tangents = true)
        {
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetColors(c); m.SetTriangles(t, 0);
            m.RecalculateBounds(); if (tangents) m.RecalculateTangents();
            return m;
        }
    }

    /// <summary>Zbiór siatek per materiał; Emit stawia po jednym obiekcie na materiał (siatki zapisane jako pliki).</summary>
    public sealed class Zbior
    {
        readonly Dictionary<Material, Siatka> czesci = new Dictionary<Material, Siatka>();
        public Siatka this[Material m]
        {
            get { if (!czesci.TryGetValue(m, out var s)) { s = new Siatka(); czesci[m] = s; } return s; }
        }
        public int Emit(Transform parent, string name, bool cienie = false, bool kolizja = false)
        {
            int n = 0;
            foreach (var kv in czesci)
            {
                if (kv.Value.Count == 0 || !kv.Key) continue;
                // duże zbiory dzielimy na kawałki ≤ 60 tys. wierzchołków (UInt16 na telefonie, lepsze odcinanie)
                foreach (var part in Podziel(kv.Value, 60000))
                {
                    var mesh = part.Zbuduj("scal " + name + " " + kv.Key.name);
                    string path = Out + "/scal_" + Bezpieczna(name) + "_" + (serial++).ToString("D4") + ".asset";
                    AssetDatabase.CreateAsset(mesh, path);
                    var g = new GameObject(name + " · " + kv.Key.name);
                    g.transform.SetParent(parent, false);
                    g.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key;
                    r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
                    if (kolizja) g.AddComponent<MeshCollider>().sharedMesh = mesh;
                    GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                    n++;
                }
            }
            czesci.Clear();
            return n;
        }

        static IEnumerable<Siatka> Podziel(Siatka s, int max)
        {
            if (s.Count <= max) { yield return s; yield break; }
            var cur = new Siatka(); var map = new Dictionary<int, int>();
            for (int i = 0; i < s.t.Count; i += 3)
            {
                if (cur.Count > max - 3) { yield return cur; cur = new Siatka(); map.Clear(); }
                for (int j = 0; j < 3; j++)
                {
                    int src = s.t[i + j];
                    if (!map.TryGetValue(src, out int dst)) { dst = cur.v.Count; map[src] = dst; cur.v.Add(s.v[src]); cur.n.Add(s.n[src]); cur.uv.Add(s.uv[src]); cur.c.Add(s.c[src]); }
                    cur.t.Add(dst);
                }
            }
            if (cur.Count > 0) yield return cur;
        }
    }

    public static string Bezpieczna(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char ch in s) sb.Append(char.IsLetterOrDigit(ch) && ch < 128 ? char.ToLowerInvariant(ch) : '_');
        return sb.ToString().Trim('_');
    }

    /// <summary>Pojedyncza siatka (np. teren) zapisana jako plik i postawiona jako obiekt.</summary>
    public static GameObject Obiekt(Transform parent, string name, Mesh mesh, Material m, bool kolizja, bool cienie = false)
    {
        string path = Out + "/" + Bezpieczna(name) + "_" + (serial++).ToString("D4") + ".asset";
        AssetDatabase.CreateAsset(mesh, path);
        var g = new GameObject(name); g.transform.SetParent(parent, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
        if (kolizja) g.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        return g;
    }

    // ---------------- kolizje ----------------
    public static BoxCollider Kol(Transform parent, string name, Vector3 center, Vector3 size, float yaw = 0f)
    {
        var g = new GameObject(name + " kolizja"); g.transform.SetParent(parent, false);
        g.transform.position = center; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        var c = g.AddComponent<BoxCollider>(); c.size = size;
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.BatchingStatic);
        return c;
    }
    public static BoxCollider KolMM(Transform parent, string name, Vector3 a, Vector3 b)
    {
        return Kol(parent, name, (a + b) * .5f, new Vector3(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z)));
    }
    /// <summary>Kolizja obrócona dowolnie (rampa, trap).</summary>
    public static BoxCollider KolRot(Transform parent, string name, Vector3 center, Vector3 size, Quaternion rot)
    {
        var g = new GameObject(name + " kolizja"); g.transform.SetParent(parent, false);
        g.transform.position = center; g.transform.rotation = rot;
        var c = g.AddComponent<BoxCollider>(); c.size = size;
        return c;
    }

    // ---------------- napisy ----------------
    static Material litery;
    /// <summary>Napis 3D (TextMesh z testem głębi). yaw = kierunek, w którym napis jest czytelny (patrzy na widza).</summary>
    public static void Napis(Transform parent, string text, Vector3 at, float yaw, float height, Color color)
    {
        var g = new GameObject("Napis " + text.Replace("\n", " ")); g.transform.SetParent(parent, false);
        g.transform.position = at; g.transform.rotation = Quaternion.Euler(0, yaw + 180f, 0);
        var tm = g.AddComponent<TextMesh>(); tm.text = text; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = height * 10f / 64f; tm.color = color;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!litery)
        {
            var sh = Shader.Find("Braxhorn/CitySignText");
            if (sh) { litery = new Material(sh) { name = "Nabrzeże: litery z testem głębi" }; litery.mainTexture = tm.font.material.mainTexture; AssetDatabase.CreateAsset(litery, Out + "/litery.mat"); }
        }
        if (litery) g.GetComponent<MeshRenderer>().sharedMaterial = litery;
        g.AddComponent<BraxCitySignText>();
    }

    // ---------------- usuwanie starego (zasada właściciela 11:00: najpierw zdjąć, potem położyć) ----------------
    /// <summary>Wycina geometrię źródłowego miasta (renderery i kolizje źródła) w bryle — przed położeniem nowej.</summary>
    public static void Wytnij(Bounds b, string etykieta, bool kolizje = true)
    {
        var src = BraxClaudeGround.SourceCity; var phys = BraxClaudeGround.SourcePhysics;
        int r = 0, k = 0;
        if (src) { try { r = BraxCityMeshCut.Subtract(src, b, Out + "/Wyciecia", etykieta); } catch (Exception e) { Log("wycięcie " + etykieta + ": " + e.Message); } }
        if (kolizje && phys) { try { k = BraxCityMeshCut.SubtractBoxColliders(phys, b); } catch (Exception e) { Log("kolizje " + etykieta + ": " + e.Message); } }
        if (kolizje && phys) k += WytnijKolizjeSiatek(phys, b);
        Usuniete.Add("źródło " + etykieta + " (siatek " + r + ", kolizji " + k + ") " + b.min.ToString("F1") + "…" + b.max.ToString("F1"));
    }

    /// <summary>Kolizje źródła (walce, siatki bez renderera), których środek leży w bryle — wyłączane.</summary>
    static int WytnijKolizjeSiatek(Transform phys, Bounds b)
    {
        int n = 0;
        foreach (var c in phys.GetComponentsInChildren<Collider>(true))
        {
            if (!c.enabled || c is BoxCollider) continue;
            if (!b.Contains(c.bounds.center)) continue;
            c.enabled = false; n++;
        }
        return n;
    }

    /// <summary>Usuwa obiekty (z poddrzewa root) o nazwie zaczynającej się od prefiksu, których bryła leży w obszarze (XZ).</summary>
    public static int UsunObiekty(Transform root, Rect xz, params string[] prefiksy)
    {
        if (!root) return 0;
        var doUsuniecia = new List<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t || t == root) continue;
            if (!prefiksy.Any(p => t.name.StartsWith(p, StringComparison.Ordinal))) continue;
            if (doUsuniecia.Any(g => t.IsChildOf(g.transform))) continue;
            var b = BraxRegion.Bounds(t.gameObject);
            var p0 = b.size == Vector3.zero ? t.position : b.center;
            if (!xz.Contains(new Vector2(p0.x, p0.z))) continue;
            doUsuniecia.Add(t.gameObject);
        }
        foreach (var g in doUsuniecia) { Usuniete.Add("obiekt „" + g.name + "” " + g.transform.position.ToString("F1")); UnityEngine.Object.DestroyImmediate(g); }
        return doUsuniecia.Count;
    }

    // ---------------- tekstury i materiały terenu / piany ----------------
    public const string Piaski = "Assets/Source/HarborBeach/Textures/";   // coast_sand_02 i damp_sand (Poly Haven CC0, w repo)

    /// <summary>Tekstura z pliku. Ustawienia importu zmieniamy TYLKO dla plików nabrzeża (HarborBeach) — tekstury Poly Haven
    /// są wspólne z innymi wykonawcami (trawa, skały), więc ładujemy je bez zmian. Normalne: zwykłe RGB (shader dekoduje sam).</summary>
    public static Texture2D TexPlik(string path, bool normal)
    {
        if (!File.Exists(path)) return null;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp && path.StartsWith(Piaski))
        {
            bool zmiana = imp.textureType != TextureImporterType.Default || imp.maxTextureSize != 1024 || imp.isReadable || (normal && imp.sRGBTexture);
            if (zmiana)
            {
                imp.textureType = TextureImporterType.Default; imp.maxTextureSize = 1024; imp.isReadable = false; imp.mipmapEnabled = true;
                imp.wrapMode = TextureWrapMode.Repeat; imp.textureCompression = TextureImporterCompression.Compressed;
                imp.sRGBTexture = !normal;
                imp.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Texture2D PH(string slug, string plik, bool normal)
    {
        return TexPlik(BraxRegion.PH + "textures/" + slug + "/" + plik, normal);
    }

    /// <summary>Materiał terenu (piasek suchy / mokry / trawa / skała mieszane kolorem wierzchołka).</summary>
    public static Material Teren(string name, Color trawa, float tPiasek = 2.4f)
    {
        var sh = Shader.Find("Braxhorn/NabrzezeTeren");
        if (!sh) throw new InvalidOperationException("brak shadera Braxhorn/NabrzezeTeren");
        var m = new Material(sh) { name = name };
        var p = TexPlik(Piaski + "coast_sand_02_diff_2k.jpg", false); var pn = TexPlik(Piaski + "coast_sand_02_nor_2k.png", true);
        var mk = TexPlik(Piaski + "damp_sand_diff_2k.jpg", false);
        if (p) m.SetTexture("_Piasek", p); if (pn) m.SetTexture("_PiasekN", pn); if (mk) m.SetTexture("_Mokry", mk ? mk : p);
        Texture2D t = null, tn = null;
        foreach (var slug in new[] { "grass004", "brown_mud_leaves_01" }) { t = PH(slug, "diffuse.jpg", false); if (t) { tn = PH(slug, "normal.jpg", true); break; } }
        if (t) m.SetTexture("_Trawa", t); if (tn) m.SetTexture("_TrawaN", tn);
        Texture2D s = null, sn = null;
        foreach (var slug in new[] { "rock_face_03", "rock_wall_08" }) { s = PH(slug, "diffuse.jpg", false); if (s) { sn = PH(slug, "normal.jpg", true); break; } }
        if (s) m.SetTexture("_Skala", s); if (sn) m.SetTexture("_SkalaN", sn);
        m.SetColor("_KPiasek", new Color(1f, .97f, .9f)); m.SetColor("_KMokry", new Color(.80f, .75f, .67f));
        m.SetColor("_KTrawa", t ? trawa : new Color(.33f, .42f, .22f)); m.SetColor("_KSkala", s ? Color.white : new Color(.48f, .46f, .43f));
        m.SetFloat("_TPiasek", tPiasek);
        if (!p) { m.SetColor("_KPiasek", new Color(.86f, .78f, .62f)); m.SetColor("_KMokry", new Color(.62f, .55f, .44f)); }
        m.enableInstancing = false;
        AssetDatabase.CreateAsset(m, Out + "/" + Bezpieczna(name) + ".mat");
        Log("materiał terenu " + name + ": piasek " + (p ? "fotoskan" : "BRAK") + ", mokry " + (mk ? "fotoskan" : "BRAK") + ", trawa " + (t ? t.name : "BRAK") + ", skała " + (s ? s.name : "BRAK"));
        return m;
    }

    static Texture2D szum;
    /// <summary>Szum do piany: 256×256, kafelkowany (szum wartości z zawinięciem siatki), zapisany jako PNG.</summary>
    public static Texture2D SzumPiany()
    {
        if (szum) return szum;
        string path = Out + "/szum_piany.png";
        const int N = 256;
        var tx = new Texture2D(N, N, TextureFormat.RGB24, false);
        var rnd = new System.Random(7031);
        float[][] krat = new float[5][]; int[] okres = { 4, 8, 16, 32, 64 };
        for (int o = 0; o < 5; o++) { krat[o] = new float[okres[o] * okres[o]]; for (int i = 0; i < krat[o].Length; i++) krat[o][i] = (float)rnd.NextDouble(); }
        float Wart(int o, float x, float y)
        {
            int P = okres[o]; float fx = x * P, fy = y * P; int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx2 = fx - ix, ty2 = fy - iy; tx2 = tx2 * tx2 * (3 - 2 * tx2); ty2 = ty2 * ty2 * (3 - 2 * ty2);
            float K2(int a, int b) => krat[o][((b % P + P) % P) * P + ((a % P + P) % P)];
            return Mathf.Lerp(Mathf.Lerp(K2(ix, iy), K2(ix + 1, iy), tx2), Mathf.Lerp(K2(ix, iy + 1), K2(ix + 1, iy + 1), tx2), ty2);
        }
        var px = new Color[N * N];
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
        {
            float u = (float)x / N, v = (float)y / N, s = 0, a = .5f, sum = 0;
            for (int o = 0; o < 5; o++) { s += Wart(o, u, v) * a; sum += a; a *= .55f; }
            s /= sum; s = Mathf.Clamp01((s - .5f) * 1.8f + .5f);
            px[y * N + x] = new Color(s, s, s);
        }
        tx.SetPixels(px); tx.Apply();
        File.WriteAllBytes(path, tx.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tx);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp) { imp.sRGBTexture = false; imp.wrapMode = TextureWrapMode.Repeat; imp.maxTextureSize = 256; imp.mipmapEnabled = true; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport(); }
        szum = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return szum;
    }

    /// <summary>Materiał piany (brzeg albo łódź).</summary>
    public static Material Piana(string name, bool lodz, float okres = 7f)
    {
        var sh = Shader.Find("Braxhorn/NabrzezePiana");
        if (!sh) throw new InvalidOperationException("brak shadera Braxhorn/NabrzezePiana");
        var m = new Material(sh) { name = name };
        m.SetTexture("_Szum", SzumPiany()); m.SetFloat("_Tryb", lodz ? 1 : 0); m.SetFloat("_Okres", okres);
        m.renderQueue = 3010;
        AssetDatabase.CreateAsset(m, Out + "/" + Bezpieczna(name) + ".mat");
        return m;
    }

    // ---------------- szum (rzeźba piasku, kształt wyspy) ----------------
    public static float Szum(float x, float z, float skala, int oktawy = 3)
    {
        float s = 0, a = 1, norm = 0, f = skala;
        for (int i = 0; i < oktawy; i++) { s += (Mathf.PerlinNoise(x * f + 31.7f * i, z * f + 17.3f * i) - .5f) * a; norm += a; a *= .5f; f *= 2.03f; }
        return s / norm * 2f;   // ok. −1..1
    }
}
