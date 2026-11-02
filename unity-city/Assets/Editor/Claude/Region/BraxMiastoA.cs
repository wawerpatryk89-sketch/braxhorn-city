using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO — wykonawca A (przebudowa 2.11 po 15:03: „od wyjścia z siłowni po morze i wyspę”). Wspólne narzędzia etapów A:
//  • Wyczysc — ZASADA WŁAŚCICIELA 11:00 („zawsze ściągasz, kładziesz nowe, a nie na to”): zanim cokolwiek położone,
//    z danej objętości znika WSZYSTKO stare: trójkąty źródła miasta (BraxCityMeshCut), pudła kolizji źródła (przycięte),
//    a w renowacji (Codex, Fable, Claude) — siatki (przycięte albo wyłączone), kolizje, światła, dźwięki, drzwi, siedziska,
//    punkty akcji. Nic nowego nie leży na starym (źródło migotania i „podestów” w wejściach).
//  • Sciana — płaszczyzna z prawdziwymi otworami (bez nakładek), Kol — kolizja pudełkowa, materiały świecące i z shaderów,
//    Siedzisko — BraxSeat z jawną wysokością i kierunkiem.
// Skala Braxa: k = BraxClaudeOsiedle.K (1.294); wymiary przedmiotów = rzeczywiste × k.
internal static class BraxMiastoA
{
    public static float K => BraxClaudeOsiedle.K;
    public const string Out = BraxRegion.Out + "/MiastoA";
    public const float Chodnik = .15f;   // poziom chodnika ulicy głównej (wejścia bez progu: podłoga lokalu = chodnik)

    public static void Log(string tekst) { Debug.Log("BRAXHORN_MIASTO_A_" + tekst); BraxRegion.Note("A: " + tekst); }

    static Bounds MM(float x0, float x1, float y0, float y1, float z0, float z1) { var b = new Bounds(); b.SetMinMax(new Vector3(x0, y0, z0), new Vector3(x1, y1, z1)); return b; }
    public static Bounds Obj(float x0, float x1, float y0, float y1, float z0, float z1) => MM(x0, x1, y0, y1, z0, z1);

    static bool PodKtoryms(Transform t, Transform[] pomin)
    {
        if (pomin == null) return false;
        for (var p = t; p; p = p.parent) foreach (var q in pomin) if (q && p == q) return true;
        return false;
    }

    /// <summary>Zdejmuje wszystko stare z objętości v (źródło + renowacja). pomin = poddrzewa nietykalne (np. klatka mieszkania).</summary>
    public static string Wyczysc(Transform cityRoot, Bounds v, string etykieta, params Transform[] pomin)
    {
        Directory.CreateDirectory(Out + "/Ciecia");
        int zrodloSiatki = 0, zrodloKol = 0, renSiatki = 0, renKol = 0, renKomp = 0;
        var src = BraxClaudeGround.SourceCity; var phys = BraxClaudeGround.SourcePhysics;
        if (src) zrodloSiatki = BraxCityMeshCut.Subtract(src, v, Out + "/Ciecia", etykieta);
        if (phys)
        {
            try { zrodloKol = BraxCityMeshCut.SubtractBoxColliders(phys, v); }
            catch (Exception e) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_MIASTO_A kolizje źródła " + etykieta + ": " + e.Message); }
        }
        // renowacja: chronione poddrzewa na czas cięcia wyłączone (Subtract pomija wyłączone renderery)
        var wylaczone = new List<MeshRenderer>();
        if (pomin != null) foreach (var p in pomin) if (p) foreach (var r in p.GetComponentsInChildren<MeshRenderer>(true)) if (r.enabled) { r.enabled = false; wylaczone.Add(r); }
        if (cityRoot) renSiatki = BraxCityMeshCut.Subtract(cityRoot, v, Out + "/Ciecia", etykieta + "_renowacja");
        foreach (var r in wylaczone) if (r) r.enabled = true;
        if (cityRoot)
        {
            var tol = v; tol.Expand(.1f);
            foreach (var c in cityRoot.GetComponentsInChildren<Collider>(true).ToArray())
            {
                if (!c || !c.enabled || PodKtoryms(c.transform, pomin)) continue;
                var b = c.bounds; if (!b.Intersects(v)) continue;
                bool wCalosci = tol.Contains(b.min) && tol.Contains(b.max);
                var bc = c as BoxCollider;
                if (wCalosci || c.isTrigger) { UnityEngine.Object.DestroyImmediate(c); renKol++; continue; }
                if (bc && Quaternion.Angle(bc.transform.rotation, Quaternion.identity) < .01f && (bc.transform.lossyScale - Vector3.one).sqrMagnitude < 1e-6f)
                {
                    Vector3 lo = Vector3.Max(b.min, v.min), hi = Vector3.Min(b.max, v.max);
                    if (hi.x - lo.x < .002f || hi.y - lo.y < .002f || hi.z - lo.z < .002f) continue;
                    void Kawalek(Vector3 a, Vector3 z)
                    {
                        var s = z - a; if (s.x < .002f || s.y < .002f || s.z < .002f) return;
                        var g = new GameObject(c.name + " (reszta po zdjęciu " + etykieta + ")"); g.transform.SetParent(bc.transform.parent, true);
                        g.transform.position = (a + z) * .5f; g.AddComponent<BoxCollider>().size = s;
                    }
                    Kawalek(b.min, new Vector3(lo.x, b.max.y, b.max.z));
                    Kawalek(new Vector3(hi.x, b.min.y, b.min.z), b.max);
                    Kawalek(new Vector3(lo.x, b.min.y, b.min.z), new Vector3(hi.x, lo.y, b.max.z));
                    Kawalek(new Vector3(lo.x, hi.y, b.min.z), new Vector3(hi.x, b.max.y, b.max.z));
                    Kawalek(new Vector3(lo.x, lo.y, b.min.z), new Vector3(hi.x, hi.y, lo.z));
                    Kawalek(new Vector3(lo.x, lo.y, hi.z), new Vector3(hi.x, hi.y, b.max.z));
                    UnityEngine.Object.DestroyImmediate(c); renKol++;
                }
                else if (v.Contains(b.center)) { UnityEngine.Object.DestroyImmediate(c); renKol++; }
            }
            // światła, dźwięki, drzwi, siedziska, punkty akcji, LOD — wszystko, co stoi w objętości
            foreach (var comp in cityRoot.GetComponentsInChildren<Component>(true).ToArray())
            {
                if (!comp || comp is Transform || comp is MeshFilter || comp is MeshRenderer || comp is Collider) continue;
                if (PodKtoryms(comp.transform, pomin) || !v.Contains(comp.transform.position)) continue;
                if (comp is Light || comp is AudioSource || comp is LODGroup || comp is MonoBehaviour)
                {
                    if (comp is BraxCityWorld) continue;
                    UnityEngine.Object.DestroyImmediate(comp); renKomp++;
                }
            }
            var world = UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>();
            if (world && world.doors != null) { world.doors = world.doors.Where(d => d).ToArray(); EditorUtility.SetDirty(world); }
        }
        string r0 = etykieta + ": źródło siatek " + zrodloSiatki + ", kolizji źródła " + zrodloKol + ", renowacja siatek " + renSiatki + ", kolizji " + renKol + ", komponentów (światła/drzwi/siedziska/dźwięki) " + renKomp;
        Debug.Log("BRAXHORN_MIASTO_A_USUNIETE " + r0 + " — objętość " + v.min.ToString("F2") + "…" + v.max.ToString("F2"));
        return r0;
    }

    // ---------------- kolizje ----------------
    public static GameObject Kol(Transform rodzic, string nazwa, Vector3 a, Vector3 b)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b);
        var g = new GameObject("kolizja: " + nazwa); g.transform.SetParent(rodzic, false); g.transform.position = (lo + hi) * .5f;
        g.AddComponent<BoxCollider>().size = hi - lo; return g;
    }

    // ---------------- ściana z otworami (jedna płaszczyzna, bez nakładek) ----------------
    /// <summary>Lico ściany: początek A, oś wzdłuż R (poziomo), normalna N (do oglądającego); szerokość W, od y0 do y1.
    /// Otwory: prostokąty w układzie (s wzdłuż R, y). Ościeża w głąb (−N) na głębokość ość (0 = bez ościeży).</summary>
    public static void Sciana(UlSiatka s, Vector3 A, Vector3 R, Vector3 N, float W, float y0, float y1, IList<Rect> otwory, float osc = 0f, UlSiatka sOsc = null)
    {
        Vector3 P(float a, float y, float d) => A + R * a + Vector3.up * y + N * d;
        var xs = new SortedSet<float> { 0f, W }; var ys = new SortedSet<float> { y0, y1 };
        if (otwory != null) foreach (var o in otwory) { xs.Add(Mathf.Clamp(o.xMin, 0, W)); xs.Add(Mathf.Clamp(o.xMax, 0, W)); ys.Add(Mathf.Clamp(o.yMin, y0, y1)); ys.Add(Mathf.Clamp(o.yMax, y0, y1)); }
        var X = xs.ToArray(); var Y = ys.ToArray();
        for (int i = 0; i < X.Length - 1; i++)
            for (int j = 0; j < Y.Length - 1; j++)
            {
                if (X[i + 1] - X[i] < .002f || Y[j + 1] - Y[j] < .002f) continue;
                var c = new Vector2((X[i] + X[i + 1]) * .5f, (Y[j] + Y[j + 1]) * .5f);
                if (otwory != null && otwory.Any(o => o.Contains(c))) continue;
                s.Quad(P(X[i], Y[j], 0), P(X[i + 1], Y[j], 0), P(X[i + 1], Y[j + 1], 0), P(X[i], Y[j + 1], 0), N);
            }
        if (osc > 0f && otwory != null)
        {
            var so = sOsc ?? s;
            foreach (var o in otwory)
            {
                so.Quad(P(o.xMin, o.yMin, 0), P(o.xMin, o.yMin, -osc), P(o.xMin, o.yMax, -osc), P(o.xMin, o.yMax, 0), R);
                so.Quad(P(o.xMax, o.yMin, 0), P(o.xMax, o.yMin, -osc), P(o.xMax, o.yMax, -osc), P(o.xMax, o.yMax, 0), -R);
                if (o.yMax < y1 - .001f) so.Quad(P(o.xMin, o.yMax, 0), P(o.xMax, o.yMax, 0), P(o.xMax, o.yMax, -osc), P(o.xMin, o.yMax, -osc), Vector3.down);
                if (o.yMin > y0 + .001f) so.Quad(P(o.xMin, o.yMin, 0), P(o.xMax, o.yMin, 0), P(o.xMax, o.yMin, -osc), P(o.xMin, o.yMin, -osc), Vector3.up);
            }
        }
    }

    /// <summary>Pomieszczenie widziane od środka: podłoga (lico y0), sufit (lico y1), 4 ściany licem do środka.
    /// otwory[0..3] = ściany −z (południe), +x, +z, −x — prostokąty w układzie (s, y) każdej ściany (s rośnie w prawo patrząc na ścianę od środka).</summary>
    public static void Pokoj(Material podloga, Material sciany, Material sufit, float x0, float x1, float z0, float z1, float y0, float y1, Rect[][] otwory = null, bool bezPodlogi = false, bool bezSufitu = false)
    {
        if (!bezPodlogi) BraxUliceKit.S(podloga).Quad(new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), new Vector3(x0, y0, z1), Vector3.up);
        if (!bezSufitu) BraxUliceKit.S(sufit).Quad(new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), Vector3.down);
        var s = BraxUliceKit.S(sciany);
        // ściana południowa (z0), patrząc od środka (na −z): prawo = −x → start w x1
        Sciana(s, new Vector3(x1, 0, z0), Vector3.left, Vector3.forward, x1 - x0, y0, y1, otwory != null && otwory.Length > 0 ? otwory[0] : null);
        Sciana(s, new Vector3(x1, 0, z1), Vector3.back, Vector3.left, z1 - z0, y0, y1, otwory != null && otwory.Length > 1 ? otwory[1] : null);
        Sciana(s, new Vector3(x0, 0, z1), Vector3.right, Vector3.back, x1 - x0, y0, y1, otwory != null && otwory.Length > 2 ? otwory[2] : null);
        Sciana(s, new Vector3(x0, 0, z0), Vector3.forward, Vector3.right, z1 - z0, y0, y1, otwory != null && otwory.Length > 3 ? otwory[3] : null);
    }

    // ---------------- materiały ----------------
    public static Material Mat(string nazwa, Color kolor, float szorstkosc = .85f, float metal = 0f) => BraxCityMaterials.Surface("A: " + nazwa, kolor, szorstkosc, metal);
    public static Material Swieci(string nazwa, Color baza, Color emisja)
    {
        var m = BraxCityMaterials.Surface("A świeci: " + nazwa, baza, .35f, 0f);
        m.SetColor("_EmissionColor", emisja); EditorUtility.SetDirty(m); return m;
    }
    public static Material Tex(string nazwa, float metry, Color? odcien, Color zamiennik, float szorstkosc, params string[] slugi)
        => BraxUliceKit.Tex("A " + nazwa, metry, odcien, zamiennik, szorstkosc, slugi);
    static readonly Dictionary<string, Material> wlasne = new Dictionary<string, Material>();
    public static Material ZShadera(string shader, string nazwa, Action<Material> ustaw)
    {
        if (wlasne.TryGetValue(nazwa, out var have) && have) return have;
        var sh = Shader.Find(shader);
        if (!sh) { Log("BRAK SHADERA " + shader + " — " + nazwa + " zastępczo matowy"); return Mat(nazwa + " (bez shadera)", new Color(.1f, .1f, .12f), .8f); }
        var m = new Material(sh) { name = "A: " + nazwa };
        ustaw?.Invoke(m);
        Directory.CreateDirectory(Out + "/Materialy");
        AssetDatabase.CreateAsset(m, Out + "/Materialy/" + BraxUliceKit.Bezpieczna(nazwa) + ".mat");
        wlasne[nazwa] = m; return m;
    }
    public static void WyczyscPamiec() => wlasne.Clear();

    // ---------------- osobny obiekt z własną siatką (efekty, części ruchome) ----------------
    public static GameObject Obiekt(Transform rodzic, string nazwa, UlSiatka s, Material m, bool cien = false)
    {
        Directory.CreateDirectory(Out + "/Siatki");
        var mesh = s.Zbuduj("A: " + nazwa);
        AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(Out + "/Siatki/" + BraxUliceKit.Bezpieczna(nazwa) + ".asset"));
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = cien ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = cien;
        return g;
    }
    public static GameObject Obiekt(Transform rodzic, string nazwa, Mesh mesh, Material m)
    {
        Directory.CreateDirectory(Out + "/Siatki");
        AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(Out + "/Siatki/" + BraxUliceKit.Bezpieczna(nazwa) + ".asset"));
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return g;
    }

    // ---------------- obraz (mural, plakat, kaseton): czworokąt z teksturą, kadrowany do proporcji ----------------
    /// <summary>Obraz z pliku w prostokącie: lewy dolny róg ld, oś pozioma R, szerokość w, wysokość h, lico N.
    /// swiecenie &gt; 0 = podświetlany (kaseton). Leży DOKŁADNIE tam, gdzie wskazano — wołający dba o otwór w ścianie / odstęp.</summary>
    public static GameObject Obraz(Transform rodzic, string nazwa, string plik, Vector3 ld, Vector3 R, float w, float h, Vector3 N, float swiecenie, float szorstkosc = .6f)
    {
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(plik);
        if (!tex) { Log("brak pliku obrazu " + plik + " (" + nazwa + ")"); return null; }
        var m = ZShadera(BraxCityMaterials.ShaderName, "obraz " + nazwa, mm =>
        {
            mm.SetTexture("_MainTex", tex); mm.SetColor("_Color", Color.white); mm.SetFloat("_Roughness", szorstkosc); mm.SetFloat("_Metallic", 0f);
            if (swiecenie > 0f) { mm.SetTexture("_EmissionMap", tex); mm.SetColor("_EmissionColor", Color.white * swiecenie); }
        });
        float asp = tex.width / (float)Mathf.Max(1, tex.height), want = w / h, u0 = 0, u1 = 1, v0 = 0, v1 = 1;
        if (asp > want) { float q = want / asp; u0 = .5f - q * .5f; u1 = .5f + q * .5f; } else { float q = asp / want; v0 = .5f - q * .5f; v1 = .5f + q * .5f; }
        var mesh = new Mesh { name = "A: obraz " + nazwa };
        mesh.vertices = new[] { ld, ld + R * w, ld + R * w + Vector3.up * h, ld + Vector3.up * h };
        mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
        mesh.normals = Enumerable.Repeat(N.normalized, 4).ToArray(); mesh.colors = Enumerable.Repeat(Color.white, 4).ToArray();
        mesh.tangents = Enumerable.Repeat(new Vector4(R.x, R.y, R.z, 1), 4).ToArray();
        bool zgodnie = Vector3.Dot(Vector3.Cross(R, Vector3.up), N) >= 0;
        mesh.triangles = zgodnie ? new[] { 0, 1, 2, 0, 2, 3 } : new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        var g = Obiekt(rodzic, "Obraz: " + nazwa, mesh, m);
        g.GetComponent<MeshRenderer>().receiveShadows = swiecenie <= 0f;
        return g;
    }

    // ---------------- siedziska ----------------
    public static BraxSeat Siedzisko(Transform rodzic, string etykieta, Vector3 punkt, Vector3 przodem, bool lezy = false)
    {
        var g = new GameObject("Siedzisko: " + etykieta); g.transform.SetParent(rodzic, false);
        g.transform.position = punkt; przodem.y = 0f; g.transform.rotation = Quaternion.LookRotation(przodem.normalized, Vector3.up);
        var s = g.AddComponent<BraxSeat>(); s.lie = lezy; s.surfaceY = punkt.y; s.label = etykieta;
        return s;
    }

    // ---------------- punkt akcji pod misje ----------------
    public static BraxPunktAkcji Akcja(Transform rodzic, string id, string etykieta, string odpowiedz, Vector3 gdzie, float promien = 1.7f)
    {
        var g = new GameObject("Punkt akcji: " + id); g.transform.SetParent(rodzic, false); g.transform.position = gdzie;
        var p = g.AddComponent<BraxPunktAkcji>(); p.id = id; p.etykieta = etykieta; p.odpowiedz = odpowiedz; p.promien = promien;
        BraxUliceKit.Punkt(id, etykieta + " — " + odpowiedz, gdzie);
        return p;
    }

    // ---------------- drzwi automatyczne (przesuwne, bez progu) ----------------
    /// <summary>Dwuskrzydłowe drzwi przesuwne w otworze: środek dołu c (poziom podłogi), oś wzdłuż R, szerokość w, wysokość h.</summary>
    public static BraxDrzwiPrzesuwne DrzwiPrzesuwne(Transform rodzic, string etykieta, Vector3 c, Vector3 R, float w, float h, Material szklo, Material rama)
    {
        var g = new GameObject("Drzwi automatyczne: " + etykieta); g.transform.SetParent(rodzic, false); g.transform.position = c;
        g.transform.rotation = Quaternion.LookRotation(Vector3.Cross(R, Vector3.up), Vector3.up);   // lokalne X = R
        var skrzydla = new List<Transform>(); var przes = new List<Vector3>(); var blok = new List<Collider>();
        foreach (float sg in new[] { -1f, 1f })
        {
            var s = new UlSiatka(); var rm = new UlSiatka();
            float sw = w * .5f;
            // skrzydło: rama aluminiowa 5 cm, szkło hartowane 1 cm
            rm.Box(new Vector3(0, 0, -.03f), new Vector3(.05f, h, .03f)); rm.Box(new Vector3(sw - .05f, 0, -.03f), new Vector3(sw, h, .03f));
            rm.Box(new Vector3(.05f, 0, -.03f), new Vector3(sw - .05f, .1f, .03f)); rm.Box(new Vector3(.05f, h - .08f, -.03f), new Vector3(sw - .05f, h, .03f));
            s.Box(new Vector3(.05f, .1f, -.006f), new Vector3(sw - .05f, h - .08f, .006f));
            var sk = new GameObject("Skrzydło " + (sg < 0 ? "lewe" : "prawe")).transform; sk.SetParent(g.transform, false);
            sk.localPosition = new Vector3(sg < 0 ? -sw : 0f, 0f, 0f);
            var gs = Obiekt(sk, "Szyba skrzydła " + etykieta, s, szklo); var gr = Obiekt(sk, "Rama skrzydła " + etykieta, rm, rama);
            var bc = sk.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(sw * .5f, h * .5f, 0f); bc.size = new Vector3(sw, h, .1f);
            skrzydla.Add(sk); przes.Add(new Vector3(sg * (sw - .06f), 0f, 0f)); blok.Add(bc);
        }
        var d = g.AddComponent<BraxDrzwiPrzesuwne>();
        d.etykieta = etykieta; d.skrzydla = skrzydla.ToArray(); d.przesuniecie = przes.ToArray(); d.blokady = blok.ToArray(); d.promien = 3.2f;
        return d;
    }
}
