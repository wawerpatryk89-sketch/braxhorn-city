using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — TRAWNIK MIASTA (wspólny krok, wykonawca „tyły”, 2.11; zgłoszenia 13.1: „trawa jest w ogóle zjebana, to nie jest
// nawet trawa”, „trawnik rysuje się dopiero, jak przechodzę”, trawa-szum, trawa na pionowych bokach płyt).
//
// DIAGNOZA 13.1 (kod 4a02329):
//  • „szum”: stary materiał Braxhorn/ClaudeWorldGround — gładkość = 1 − szorstkość skanu (Grass004 ma ~0.74 gładkości), więc
//    każde źdźbło łapało odblask nieba (srebrne iskry), odcień (.92,.95,.88) bladoniebieski, jedna skala bez plam — z daleka TV-szum;
//  • „rysuje się przy graczu”: kwatery trawnika leżały 1 cm NAD granitem placu, a granit (CityStaticPBR pod korzeniem renowacji)
//    dostawał w kroku anty-migotania przesunięcie głębi −1/−1 → z dalszej odległości granit wygrywał test głębi i trawnik
//    „znikał” (zostawała goła kostka), z bliska wracał. To NIE był LOD — to dwie płaszczyzny jedna na drugiej;
//  • trawa na ścianach: trawniki to były skrzynki (Box) z trawą także na pionowych bokach, tekstura rzutowana z góry → pasy.
// ROZWIĄZANIE (jak osiedle po buildzie 14):
//  • materiał „trawnik miasta” = shader Braxhorn/OsiedleTrawa + fotoskan Grass004 z osiedla (2K, trilinear, aniso 4): gładkość
//    liczona do 0.10 (matowa), odcień liczony do luminancji albedo 0.133 (ColorChecker „Foliage”), duże plamy, suchsze miejsca,
//    pasy koszenia 1.8 m / 9 %; pod drzewami wąski pierścień ziemi z liśćmi (maska 0.5 m/px);
//  • trawnik = SAMA płaska powierzchnia (siatka z góry, bez boków) w otworze nawierzchni — nigdy na niej ani pod nią w tym samym
//    miejscu; brzeg = betonowe obrzeże (stoi 4–5 cm nad trawą, zakrywa styk), żadnego pionowego „boku z trawy”;
//  • warstwa 3D: kępy instancjami (BraxClaudeTrawa + BraxClaudeTrawaDane, atlas grass_medium_01) — murawa 6.5 cm, ~11/m²,
//    kolor nasady = kolor trawnika pod nią (zanik kęp z odległością niewidoczny: trawnik z daleka ma ten sam kolor);
//  • stare trawniki miasta (glb + wszystko z materiałem „Ziemia: prawdziwa trawa Grass004”): materiał podmieniony W MIEJSCU
//    (nic nie kładziemy na stare), pionowe/strome trójkąty przeniesione do osobnego materiału ziemi (bez trawy na ścianach).
//
// API dla innych wykonawców (wywołuj w swoich krokach PRZED krokiem „trawnik miasta” w BraxRegion.Apply):
//  • BraxRegionTrawa.Trawnik(parent, nazwa, obszar, y, dziury, obrzeze) — trawnik prostokątny (obszar = otwór w nawierzchni),
//  • BraxRegionTrawa.Teren(renderer, kepy) — własna siatka terenu (wzgórze, skarpa): materiał trawy + rejestracja,
//  • BraxRegionTrawa.Drzewo(p) — pień drzewa na trawniku (pierścień ziemi i mniej kęp),
//  • BraxRegionTrawa.Material / Laka — same materiały (gdy trzeba tylko materiału).
// Krok końcowy: BraxRegionTrawa.Zakoncz(root) (BraxRegion.Apply, przed anty-migotaniem) — maska ziemi, kępy, podmiana starych.
public static class BraxRegionTrawa
{
    const string Tex = "Assets/Source/Claude/Osiedle/textures/";
    const string Detail = "Assets/Source/Claude/Osiedle/detail/";
    static string Out => BraxRegion.Out + "/Trawa";
    public const float GrubyObrzeza = .08f;          // szerokość betonowego obrzeża (m)
    public const float ObrzezeNadTrawa = .045f;      // obrzeże wystaje nad trawę
    const float MaskX0 = -140f, MaskZ0 = -150f, MaskRes = .5f; const int MaskW = 640, MaskH = 820;   // x −140..180, z −150..260

    sealed class Wpis { public GameObject go; public Rect obszar; public float y; public List<Rect> dziury; public bool kepy; public bool laka; }
    static readonly List<Wpis> trawniki = new List<Wpis>();
    static readonly List<Vector3> drzewa = new List<Vector3>();
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static BraxRegion.Report dlaBuildu;
    static Vector4 grassLin = Vector4.one; static Vector3 grassMeanLin = Vector3.zero;
    static int nTrawnikow, nStarych, nScian, nKep;
    static readonly List<string> notatki = new List<string>();

    // Stan jest na jeden build: BraxRegion.Apply tworzy nowy raport — wtedy czyścimy listy (bez haka w BraxRegion).
    static void Stan()
    {
        if (dlaBuildu == BraxRegion.Rep) return;
        dlaBuildu = BraxRegion.Rep; trawniki.Clear(); drzewa.Clear(); mats.Clear(); notatki.Clear();
        grassLin = Vector4.one; grassMeanLin = Vector3.zero; nTrawnikow = nStarych = nScian = nKep = 0;
        Directory.CreateDirectory(Out);
    }

    // ------------------------------------------------------------------ materiały
    static Texture2D Map(string path, bool normal, bool srgb)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!imp) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); imp = AssetImporter.GetAtPath(path) as TextureImporter; }
        if (!imp) throw new Exception("brak mapy " + path);
        // te same ustawienia co osiedle (BraxClaudeOsiedle.Map): kolor i normalne trawy 2K, reszta 512, trilinear, aniso 4
        bool near = path.Contains("grass004") || path.Contains("brown_mud");
        int size = path.EndsWith("albedo.jpg") ? (near ? 2048 : 1024) : path.EndsWith("normal.jpg") ? (path.Contains("grass004") ? 2048 : 1024) : 512;
        bool dirty = imp.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) || imp.sRGBTexture != srgb || imp.maxTextureSize != size
                     || !imp.mipmapEnabled || imp.wrapMode != TextureWrapMode.Repeat || imp.anisoLevel != 4 || imp.filterMode != FilterMode.Trilinear
                     || imp.textureCompression != TextureImporterCompression.Compressed || (path.EndsWith("mg.png") && imp.alphaSource != TextureImporterAlphaSource.FromInput);
        if (dirty)
        {
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = srgb; imp.maxTextureSize = size; imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Repeat; imp.anisoLevel = 4; imp.filterMode = FilterMode.Trilinear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            if (path.EndsWith("mg.png")) imp.alphaSource = TextureImporterAlphaSource.FromInput;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static float SrednieA(string png)
    {
        try
        {
            if (!File.Exists(png)) return -1f;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            float v = -1f;
            if (t.LoadImage(File.ReadAllBytes(png)))
            {
                var px = t.GetPixels32(); int step = Mathf.Max(1, px.Length / 262144), n = 0; double sum = 0;
                for (int i = 0; i < px.Length; i += step) { sum += px[i].a; n++; }
                v = (float)(sum / (255.0 * Mathf.Max(1, n)));
            }
            UnityEngine.Object.DestroyImmediate(t); return v;
        }
        catch (Exception) { return -1f; }
    }

    static Vector3 SrednieLiniowo(string jpg)
    {
        var r = Vector3.zero;
        try
        {
            if (!File.Exists(jpg)) return r;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (t.LoadImage(File.ReadAllBytes(jpg)))
            {
                var px = t.GetPixels32(); int step = Mathf.Max(1, px.Length / 262144), n = 0; double sr = 0, sg = 0, sb = 0;
                for (int i = 0; i < px.Length; i += step)
                { sr += Mathf.GammaToLinearSpace(px[i].r / 255f); sg += Mathf.GammaToLinearSpace(px[i].g / 255f); sb += Mathf.GammaToLinearSpace(px[i].b / 255f); n++; }
                r = new Vector3((float)(sr / n), (float)(sg / n), (float)(sb / n));
            }
            UnityEngine.Object.DestroyImmediate(t);
        }
        catch (Exception) { }
        return r;
    }

    /// <summary>Materiał trawnika miasta (park, plac, skwer): koszony, z pasami koszenia.</summary>
    public static Material Material { get { Stan(); return Mat(false); } }
    /// <summary>Materiał łąki / skarpy / wzgórza: bez pasów koszenia, więcej suchszych plam.</summary>
    public static Material Laka { get { Stan(); return Mat(true); } }

    static Material Mat(bool laka)
    {
        string key = laka ? "łąka" : "trawnik";
        if (mats.TryGetValue(key, out var have) && have) return have;
        var sh = Shader.Find("Braxhorn/OsiedleTrawa");
        string d = Tex + "grass004/";
        Material m;
        if (!sh || !File.Exists(d + "albedo.jpg") || !File.Exists(d + "normal.jpg") || !File.Exists(d + "mg.png") || !File.Exists(d + "ao.jpg"))
        {
            // zapas (brak paczki osiedla w buildzie): matowa zieleń bez połysku, nigdy stary „szum”
            Notatka("trawnik: brak shadera Braxhorn/OsiedleTrawa albo map grass004 osiedla — materiał zapasowy (matowy)");
            m = BraxCityMaterials.Surface("Miasto: trawnik (zapas, mat)", new Color(.22f, .33f, .13f), .95f);
            mats[key] = m; return m;
        }
        m = new Material(sh) { name = laka ? "Miasto: łąka i skarpy (fotoskan, plamy)" : "Miasto: trawnik (fotoskan, plamy, pasy koszenia)", enableInstancing = true };
        m.mainTexture = Map(d + "albedo.jpg", false, true);
        m.SetTexture("_BumpMap", Map(d + "normal.jpg", true, false));
        m.SetTexture("_MetallicGlossMap", Map(d + "mg.png", false, false));
        m.SetTexture("_OcclusionMap", Map(d + "ao.jpg", false, false));
        // gładkość liczona: średnia kanału A (gładkość skanu) × mnożnik = 0.10 (trawa matowa — bez srebrnych iskier „szumu”)
        float sm = SrednieA(d + "mg.png");
        m.SetFloat("_Gloss", sm > .01f ? Mathf.Clamp01(.10f / sm) : .12f);
        m.SetFloat("_Tile", 2f);
        m.SetFloat("_MacroTile", laka ? 23f : 29f);
        m.SetFloat("_MacroMix", laka ? .5f : .45f);
        m.SetFloat("_DryMix", laka ? .5f : .35f);
        m.SetFloat("_Stripe", 1.8f); m.SetFloat("_StripeMix", laka ? 0f : .045f);
        m.SetFloat("_BumpScale", 1.1f);
        var am = SrednieLiniowo(d + "albedo.jpg");
        if (am.y > .01f)
        {
            m.SetFloat("_MacroMean", .3f * am.x + .59f * am.y + .11f * am.z); m.SetFloat("_DryMean", am.y);
            // odcień liniowo jak osiedle 14: żywa zieleń (odcień ~89°), luminancja albedo 0.133; łąka odrobinę cieplejsza
            var baseLin = laka ? new Color(.60f, 1f, .50f) : new Color(.536f, 1f, .536f);
            float Y = .2126f * am.x * baseLin.r + .7152f * am.y * baseLin.g + .0722f * am.z * baseLin.b, k = (laka ? .128f : .133f) / Mathf.Max(1e-4f, Y);
            var lin = new Color(baseLin.r * k, baseLin.g * k, baseLin.b * k, 1f);
            m.color = lin.gamma;
            if (!laka) { grassLin = new Vector4(lin.r, lin.g, lin.b, 1f); grassMeanLin = new Vector3(am.x * lin.r, am.y * lin.g, am.z * lin.b); }
            Notatka("trawnik " + key + ": średnia tekstury liniowo " + am.ToString("F4") + ", gładkość skanu " + sm.ToString("F3") + " → 0.10, odcień " + ((Vector4)lin).ToString("F3"));
        }
        // ziemia z liśćmi pod drzewami (maska powstaje w Zakoncz)
        string sd = Tex + "brown_mud_leaves_01/";
        if (File.Exists(sd + "albedo.jpg") && File.Exists(sd + "normal.jpg"))
        {
            m.SetTexture("_SoilTex", Map(sd + "albedo.jpg", false, true)); m.SetTexture("_SoilBump", Map(sd + "normal.jpg", true, false));
            var s = SrednieLiniowo(sd + "albedo.jpg"); float sy = .2126f * s.x + .7152f * s.y + .0722f * s.z;
            float sk = sy > .005f ? .10f / sy : 1f; m.SetVector("_SoilTint", new Vector4(sk, sk, sk, 1f));
        }
        m.SetFloat("_SoilMix", 0f);
        AssetDatabase.CreateAsset(m, Out + "/" + (laka ? "laka" : "trawnik") + ".mat");
        mats[key] = m; return m;
    }

    static Material Obrzeze
    {
        get
        {
            if (mats.TryGetValue("obrzeże", out var have) && have) return have;
            Material m = null;
            try { m = BraxRegion.Pbr("Trawnik: obrzeże betonowe", 1.0f * BraxClaudeOsiedle.K, new Color(.86f, .85f, .82f), "concrete_floor_02", "concrete_wall_008"); }
            catch (Exception) { }
            if (!m) m = BraxCityMaterials.Surface("Trawnik: obrzeże betonowe", new Color(.62f, .61f, .58f), .9f);
            mats["obrzeże"] = m; return m;
        }
    }

    static Material Ziemia
    {
        get
        {
            if (mats.TryGetValue("ziemia", out var have) && have) return have;
            // boki starych trawników i strome skarpy: ziemia (bez trawy na ścianach); tekstura ziemi z liśćmi, jeśli jest
            Material m = null;
            string sd = Tex + "brown_mud_leaves_01/";
            if (File.Exists(sd + "albedo.jpg"))
                try { m = BraxCityMaterials.Textured("Trawnik: ziemia na bokach i skarpach", Map(sd + "albedo.jpg", false, true), File.Exists(sd + "normal.jpg") ? Map(sd + "normal.jpg", true, false) : null, null, 1f / 1.3f); m.SetColor("_Color", new Color(.62f, .58f, .54f)); m.SetFloat("_Roughness", .95f); m.SetFloat("_Metallic", 0f); }
                catch (Exception) { m = null; }
            if (!m) m = BraxCityMaterials.Surface("Trawnik: ziemia na bokach", new Color(.24f, .19f, .14f), .97f);
            mats["ziemia"] = m; return m;
        }
    }

    static void Notatka(string s) { if (notatki.Count < 60) notatki.Add(s); }

    // ------------------------------------------------------------------ API
    /// <summary>
    /// Trawnik prostokątny: obszar = otwór w nawierzchni (XZ), y = poziom trawy (np. nawierzchnia − 0.03).
    /// Powstaje SAMA płaska powierzchnia (bez boków) zwężona o obrzeże, kolizja płaska, betonowe obrzeże dookoła (opcjonalnie).
    /// dziury = prostokąty bez trawy (np. misa pod fontanną). Kępy i maska ziemi dochodzą w kroku końcowym.
    /// </summary>
    public static GameObject Trawnik(Transform parent, string nazwa, Rect obszar, float y, IList<Rect> dziury = null, bool obrzeze = true, bool kepy = true, bool laka = false)
    {
        Stan();
        if (obszar.width < .5f || obszar.height < .5f) return null;
        float e = obrzeze ? GrubyObrzeza : 0f;
        var r = Rect.MinMaxRect(obszar.xMin + e, obszar.yMin + e, obszar.xMax - e, obszar.yMax - e);
        var holes = (dziury ?? new Rect[0]).ToList();
        var mesh = Plaszczyzna(nazwa, r, y, holes, 2f);
        if (!mesh) return null;
        var go = new GameObject("Trawnik: " + nazwa);
        go.transform.SetParent(parent, false); go.transform.position = Vector3.zero;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = laka ? Mat(true) : Mat(false);
        mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        if (obrzeze)
        {
            var b = new BraxCitySystemMesh();
            float top = y + ObrzezeNadTrawa, h = .22f, cy = top - h * .5f;
            b.Box(new Vector3(obszar.center.x, cy, obszar.yMin + e * .5f), new Vector3(obszar.width, h, e));
            b.Box(new Vector3(obszar.center.x, cy, obszar.yMax - e * .5f), new Vector3(obszar.width, h, e));
            b.Box(new Vector3(obszar.xMin + e * .5f, cy, obszar.center.y), new Vector3(e, h, obszar.height - 2 * e));
            b.Box(new Vector3(obszar.xMax - e * .5f, cy, obszar.center.y), new Vector3(e, h, obszar.height - 2 * e));
            foreach (var hole in holes)
            {
                b.Box(new Vector3(hole.center.x, cy, hole.yMin - e * .5f), new Vector3(hole.width + 2 * e, h, e));
                b.Box(new Vector3(hole.center.x, cy, hole.yMax + e * .5f), new Vector3(hole.width + 2 * e, h, e));
                b.Box(new Vector3(hole.xMin - e * .5f, cy, hole.center.y), new Vector3(e, h, hole.height));
                b.Box(new Vector3(hole.xMax + e * .5f, cy, hole.center.y), new Vector3(e, h, hole.height));
            }
            BraxCityArchitecture.MeshObject("Trawnik: obrzeże betonowe " + nazwa, go.transform, b, Obrzeze);
        }
        trawniki.Add(new Wpis { go = go, obszar = r, y = y, dziury = holes, kepy = kepy, laka = laka });
        nTrawnikow++;
        return go;
    }

    /// <summary>Własna siatka terenu (wzgórze, skarpa, nasyp): dostaje materiał łąki (albo trawnika) i — gdy płaska i nisko — kępy.</summary>
    public static void Teren(MeshRenderer r, bool kepy = false, bool laka = true)
    {
        Stan();
        if (!r) return;
        var ms = r.sharedMaterials; if (ms.Length == 0) ms = new Material[1];
        ms[0] = laka ? Mat(true) : Mat(false); r.sharedMaterials = ms;
        if (kepy) trawniki.Add(new Wpis { go = r.gameObject, obszar = Rect.zero, y = float.NaN, kepy = true, laka = laka });
    }

    /// <summary>Pień drzewa stojący na trawniku: pierścień ziemi z liśćmi (0.8 m pełny, zanik do 1.6 m) i brak kęp przy pniu.</summary>
    public static void Drzewo(Vector3 p) { Stan(); drzewa.Add(p); }

    // Płaska siatka (tylko górna powierzchnia), komórki ≤ krok, z dziurami — wzorem BraxCityGround.Plane (indeksy całkowite).
    static Mesh Plaszczyzna(string nazwa, Rect area, float y, List<Rect> holes, float step)
    {
        var xs = new SortedSet<float> { area.xMin, area.xMax }; var zs = new SortedSet<float> { area.yMin, area.yMax };
        for (int i = 1; ; i++) { float x = area.xMin + i * step; if (x >= area.xMax - .01f) break; xs.Add(x); }
        for (int i = 1; ; i++) { float z = area.yMin + i * step; if (z >= area.yMax - .01f) break; zs.Add(z); }
        foreach (var h in holes)
        {
            if (h.xMin > area.xMin && h.xMin < area.xMax) xs.Add(h.xMin); if (h.xMax > area.xMin && h.xMax < area.xMax) xs.Add(h.xMax);
            if (h.yMin > area.yMin && h.yMin < area.yMax) zs.Add(h.yMin); if (h.yMax > area.yMin && h.yMax < area.yMax) zs.Add(h.yMax);
        }
        var xx = xs.ToArray(); var zz = zs.ToArray();
        var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>(); var n = new List<Vector3>();
        for (int i = 0; i < xx.Length - 1; i++)
            for (int j = 0; j < zz.Length - 1; j++)
            {
                if (xx[i + 1] - xx[i] < .005f || zz[j + 1] - zz[j] < .005f) continue;
                var c = new Vector2((xx[i] + xx[i + 1]) * .5f, (zz[j] + zz[j + 1]) * .5f);
                if (holes.Any(h => h.Contains(c))) continue;
                int b = v.Count;
                v.Add(new Vector3(xx[i], y, zz[j])); v.Add(new Vector3(xx[i], y, zz[j + 1])); v.Add(new Vector3(xx[i + 1], y, zz[j + 1])); v.Add(new Vector3(xx[i + 1], y, zz[j]));
                foreach (var p in v.Skip(b)) { uv.Add(new Vector2(p.x, p.z)); n.Add(Vector3.up); }
                t.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
        if (v.Count == 0) return null;
        var mesh = new Mesh { name = "Trawnik " + nazwa, indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
        mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        mesh.RecalculateBounds(); mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh, Out + "/trawnik_" + nTrawnikow.ToString("D3") + "_" + Bezpieczna(nazwa) + ".asset");
        return mesh;
    }

    static string Bezpieczna(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Substring(0, Mathf.Min(40, s.Length));

    // ------------------------------------------------------------------ krok końcowy
    public static void Zakoncz(Transform cityRoot)
    {
        Stan();
        Physics.SyncTransforms();
        var trawnik = Mat(false); var laka = Mat(true);
        // 1) Stare trawniki: materiał podmieniony w miejscu, pionowe trójkąty → ziemia. Nic nie kładziemy na stare.
        var stare = new List<(MeshRenderer r, bool laka)>();
        foreach (var r in BraxClaudeGround.All<MeshRenderer>(cityRoot))
        {
            if (!r || !r.enabled) continue;
            var ms = r.sharedMaterials; bool zmiana = false;
            for (int i = 0; i < ms.Length; i++)
            {
                if (!StaraTrawa(ms[i])) continue;
                var b = r.bounds; bool duza = b.size.x * b.size.z > 3000f || b.size.y > 4f;
                ms[i] = duza ? laka : trawnik; zmiana = true;
            }
            if (!zmiana) continue;
            r.sharedMaterials = ms; nStarych++;
            BezScian(r);
            var bb = r.bounds; stare.Add((r, bb.size.x * bb.size.z > 3000f || bb.size.y > 4f));
        }
        // 2) Pierścienie ziemi pod drzewami: zgłoszone + wszystkie drzewa z LOD (drzewo_verdant) stojące nisko, na trawie.
        foreach (var lod in BraxClaudeGround.All<LODGroup>(cityRoot))
        {
            if (!lod || !lod.gameObject.activeInHierarchy) continue;
            var rs = lod.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var b = rs[0].bounds; foreach (var x in rs) b.Encapsulate(x.bounds);
            if (b.size.y < 3f || b.min.y > 1.2f || b.min.y < -1f) continue;
            var p = new Vector3(lod.transform.position.x, b.min.y, lod.transform.position.z);
            if (!drzewa.Any(d => (d - p).sqrMagnitude < .25f)) drzewa.Add(p);
        }
        var soil = MaskaZiemi(trawnik, laka);
        // 3) Kępy: trawniki z API + płaskie stare trawniki (nisko, y −0.6..0.8)
        var sh = Shader.Find("Braxhorn/OsiedleKepyInst");
        var atlasImp = AssetImporter.GetAtPath(Detail + "kepy_atlas.png") as TextureImporter;
        if (!sh || !atlasImp || !File.Exists(Detail + "kepy_atlas.json")) Notatka("kępy: brak shadera OsiedleKepyInst albo atlasu kęp — trawniki bez kęp");
        else
        {
            var mat = KepyMaterial(sh, atlasImp);
            var siatka = KepaMesh();
            var rnd = new System.Random(20261102);
            int limit = 420000;
            foreach (var w in trawniki)
            {
                if (!w.kepy || !w.go || nKep >= limit) continue;
                var list = new List<Vector4>(); var ys = new List<float>();
                if (!float.IsNaN(w.y) && w.obszar.width > 0f) Siatka(w, rnd, soil, list, ys);
                else { var mr = w.go.GetComponent<MeshRenderer>(); if (mr) ZTrojkatow(mr, w.laka, rnd, soil, list, ys, limit - nKep); }
                Pole(w.go.transform, list, ys, mat, siatka);
            }
            foreach (var (r, l) in stare)
            {
                if (nKep >= limit) break;
                var list = new List<Vector4>(); var ys = new List<float>();
                ZTrojkatow(r, l, rnd, soil, list, ys, limit - nKep);
                Pole(r.transform, list, ys, mat, siatka);
            }
        }
        AssetDatabase.SaveAssets();
        string log = "BRAXHORN_TRAWA trawnik miasta: nowe trawniki " + nTrawnikow + ", stare podmienione w miejscu " + nStarych + " (pionowe trójkąty → ziemia: " + nScian
                     + "), drzewa z pierścieniem " + drzewa.Count + ", kępy " + nKep + "; materiał " + (trawnik ? trawnik.shader.name : "?");
        BraxRegion.Note(log);
        foreach (var s in notatki) Debug.Log("BRAXHORN_TRAWA " + s);
    }

    static bool StaraTrawa(Material m)
    {
        if (!m) return false;
        if (mats.Values.Contains(m)) return false;
        string n = m.name.ToLowerInvariant();
        if (n.Contains("boisko") || n.Contains("sztuczn") || n.Contains("turf") || n.Contains("kort")) return false;
        if (n.StartsWith("ziemia: prawdziwa trawa") || n.StartsWith("ziemia__prawdziwa_trawa")) return true;
        return m.shader && m.shader.name == "Braxhorn/ClaudeWorldGround" && (n.Contains("traw") || n.Contains("grass"));
    }

    // Pionowe i strome (> ~63°) trójkąty z materiałem trawy → osobny materiał ziemi (bez trawy na ścianach płyt i skarp).
    static void BezScian(MeshRenderer r)
    {
        var mf = r.GetComponent<MeshFilter>(); var src = mf ? mf.sharedMesh : null;
        if (!src || !src.isReadable) return;
        var ms = r.sharedMaterials; var M = r.transform.localToWorldMatrix; var v = src.vertices;
        var subs = new List<int[]>(); var mats2 = new List<Material>(); var sciany = new List<int>(); int moved = 0;
        for (int s = 0; s < src.subMeshCount; s++)
        {
            var tris = src.GetTriangles(s); var mat = ms[Mathf.Min(s, ms.Length - 1)];
            bool trawa = mats.Values.Contains(mat);
            if (!trawa) { subs.Add(tris); mats2.Add(mat); continue; }
            var keep = new List<int>(tris.Length);
            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = M.MultiplyPoint3x4(v[tris[i]]), b = M.MultiplyPoint3x4(v[tris[i + 1]]), c = M.MultiplyPoint3x4(v[tris[i + 2]]);
                var nn = Vector3.Cross(b - a, c - a); float len = nn.magnitude;
                if (len > 1e-8f && Mathf.Abs(nn.y / len) < .45f) { sciany.Add(tris[i]); sciany.Add(tris[i + 1]); sciany.Add(tris[i + 2]); moved++; continue; }
                keep.Add(tris[i]); keep.Add(tris[i + 1]); keep.Add(tris[i + 2]);
            }
            subs.Add(keep.ToArray()); mats2.Add(mat);
        }
        if (moved == 0) return;
        var copy = UnityEngine.Object.Instantiate(src); copy.name = src.name + "_trawa_bez_scian";
        copy.subMeshCount = subs.Count + 1;
        for (int s = 0; s < subs.Count; s++) copy.SetTriangles(subs[s], s);
        copy.SetTriangles(sciany, subs.Count);
        mats2.Add(Ziemia);
        AssetDatabase.CreateAsset(copy, Out + "/bez_scian_" + nStarych.ToString("D3") + ".asset");
        foreach (var col in r.GetComponents<MeshCollider>()) if (col.sharedMesh == src) col.sharedMesh = copy;
        mf.sharedMesh = copy; r.sharedMaterials = mats2.ToArray();
        nScian += moved / 3;
    }

    static float[,] MaskaZiemi(Material trawnik, Material laka)
    {
        var soil = new float[MaskW, MaskH];
        if (drzewa.Count == 0) return soil;
        foreach (var t in drzewa)
        {
            int i0 = Mathf.FloorToInt((t.x - 1.8f - MaskX0) / MaskRes), i1 = Mathf.CeilToInt((t.x + 1.8f - MaskX0) / MaskRes);
            int j0 = Mathf.FloorToInt((t.z - 1.8f - MaskZ0) / MaskRes), j1 = Mathf.CeilToInt((t.z + 1.8f - MaskZ0) / MaskRes);
            for (int i = Mathf.Max(0, i0); i <= Mathf.Min(MaskW - 1, i1); i++)
                for (int j = Mathf.Max(0, j0); j <= Mathf.Min(MaskH - 1, j1); j++)
                {
                    float x = MaskX0 + (i + .5f) * MaskRes, z = MaskZ0 + (j + .5f) * MaskRes;
                    float d = Mathf.Sqrt((t.x - x) * (t.x - x) + (t.z - z) * (t.z - z));
                    float sv = d < .8f ? 1f : d < 1.6f ? 1f - (d - .8f) / .8f : 0f;
                    if (sv <= 0f) continue;
                    sv = Mathf.Clamp01(sv * (.7f + Mathf.PerlinNoise(x * .9f + 11.3f, z * .9f + 7.7f) * .5f)) * .9f;
                    soil[i, j] = Mathf.Max(soil[i, j], sv);
                }
        }
        var px = new Color32[MaskW * MaskH];
        for (int j = 0; j < MaskH; j++) for (int i = 0; i < MaskW; i++) { byte b = (byte)Mathf.RoundToInt(soil[i, j] * 255f); px[j * MaskW + i] = new Color32(b, b, b, b); }
        var tex = new Texture2D(MaskW, MaskH, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
        string path = Out + "/maska_ziemi_miasto.png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp)
        {
            imp.textureType = TextureImporterType.SingleChannel;
            var ts = new TextureImporterSettings(); imp.ReadTextureSettings(ts); ts.singleChannelComponent = TextureImporterSingleChannelComponent.Red; imp.SetTextureSettings(ts);
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.npotScale = TextureImporterNPOTScale.None; imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear; imp.maxTextureSize = 1024; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
        }
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        foreach (var m in new[] { trawnik, laka })
        {
            if (!m || !mask || !m.HasProperty("_Mask")) continue;
            m.SetTexture("_Mask", mask);
            m.SetVector("_MaskRect", new Vector4(MaskX0, MaskZ0, 1f / (MaskW * MaskRes), 1f / (MaskH * MaskRes)));
            m.SetFloat("_SoilMix", m.GetTexture("_SoilTex") ? 1f : 0f);
            EditorUtility.SetDirty(m);
        }
        return soil;
    }

    static float SoilAt(float[,] soil, float x, float z)
    {
        int i = (int)((x - MaskX0) / MaskRes), j = (int)((z - MaskZ0) / MaskRes);
        return soil == null || i < 0 || j < 0 || i >= MaskW || j >= MaskH ? 0f : soil[i, j];
    }

    // Czy w punkcie trawnika coś stoi (ławka, pień, nawierzchnia nad trawą)? Niewidzialne cienkie podłogi nie zasłaniają.
    static readonly RaycastHit[] trafienia = new RaycastHit[16];
    static bool Zasloniete(Vector3 p)
    {
        int n = Physics.RaycastNonAlloc(p + Vector3.up * 6f, Vector3.down, trafienia, 6.2f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = trafienia[i];
            if (h.point.y <= p.y + .04f) continue;
            var c = h.collider; var b = c.bounds;
            var rr = c.GetComponent<Renderer>();
            if ((rr && rr.enabled) || b.size.y > .25f || b.size.x * b.size.z < 60f) return true;
        }
        return false;
    }

    // Murawa w prostokącie: siatka 0.3 m z przesunięciem (~11 kęp / m²), 6 cm od obrzeża, bez dziur, bez zasłoniętych miejsc.
    static void Siatka(Wpis w, System.Random rnd, float[,] soil, List<Vector4> list, List<float> ys)
    {
        const float hMur = .065f / .32f;
        var r = w.obszar;
        for (float z = r.yMin + .06f; z < r.yMax - .36f; z += .3f)
            for (float x = r.xMin + .06f; x < r.xMax - .36f; x += .3f)
            {
                float px = x + (float)rnd.NextDouble() * .3f, pz = z + (float)rnd.NextDouble() * .3f;
                if (w.dziury.Any(h => px > h.xMin - .06f && px < h.xMax + .06f && pz > h.yMin - .06f && pz < h.yMax + .06f)) continue;
                if (rnd.NextDouble() < SoilAt(soil, px, pz) / .9) continue;
                var p = new Vector3(px, w.y, pz);
                if (Zasloniete(p)) continue;
                bool lak = w.laka && rnd.NextDouble() < .35;
                float sc = lak ? .8f + (float)rnd.NextDouble() * .45f : hMur * (.9f + (float)rnd.NextDouble() * .2f);
                list.Add(new Vector4(px, pz, (float)rnd.NextDouble() * 360f, sc)); ys.Add(w.y - (lak ? .03f : .012f));
            }
    }

    // Kępy na siatce (stare trawniki, teren): trójkąty prawie poziome, nisko (y −0.6..0.8), gęstość wg pola.
    static void ZTrojkatow(MeshRenderer r, bool laka, System.Random rnd, float[,] soil, List<Vector4> list, List<float> ys, int budzet)
    {
        var mf = r.GetComponent<MeshFilter>(); var m = mf ? mf.sharedMesh : null;
        if (!m || !m.isReadable || budzet <= 0) return;
        var M = r.transform.localToWorldMatrix; var v = m.vertices; var ms = r.sharedMaterials;
        var tri = new List<(Vector3 a, Vector3 b, Vector3 c, float area)>(); float total = 0f;
        for (int s = 0; s < m.subMeshCount; s++)
        {
            if (!mats.Values.Contains(ms[Mathf.Min(s, ms.Length - 1)])) continue;
            var t = m.GetTriangles(s);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = M.MultiplyPoint3x4(v[t[i]]), b = M.MultiplyPoint3x4(v[t[i + 1]]), c = M.MultiplyPoint3x4(v[t[i + 2]]);
                var n = Vector3.Cross(b - a, c - a); float area = n.magnitude * .5f;
                if (area < 1e-4f || Mathf.Abs(n.y) / (2f * area) < .92f) continue;
                float cy = (a.y + b.y + c.y) / 3f; if (cy < -.6f || cy > .8f) continue;
                tri.Add((a, b, c, area)); total += area;
            }
        }
        if (total < 1f) return;
        // murawa 11/m² (koszona), łąka (duże stare połacie) 5/m² wyższych kęp; budżet: najwyżej 160 tys. na jedną siatkę
        float dens = laka ? 5f : 11f;
        dens = Mathf.Min(dens, Mathf.Min(160000f, budzet) / total);
        const float hMur = .065f / .32f;
        foreach (var (a, b, c, area) in tri)
        {
            float want = area * dens; int n = (int)want; if (rnd.NextDouble() < want - n) n++;
            for (int k = 0; k < n; k++)
            {
                float u = (float)rnd.NextDouble(), w = (float)rnd.NextDouble(); if (u + w > 1f) { u = 1f - u; w = 1f - w; }
                var p = a + (b - a) * u + (c - a) * w;
                if (rnd.NextDouble() < SoilAt(soil, p.x, p.z) / .9) continue;
                if (Zasloniete(p)) continue;
                bool lak = laka && rnd.NextDouble() < .6;
                float sc = lak ? .8f + (float)rnd.NextDouble() * .45f : hMur * (.9f + (float)rnd.NextDouble() * .2f);
                list.Add(new Vector4(p.x, p.z, (float)rnd.NextDouble() * 360f, sc)); ys.Add(p.y - (lak ? .03f : .012f));
            }
        }
    }

    // Jedno pole kęp = dziecko trawnika (przenosi się razem z nim do obszaru doczytywanego) + zasób danych.
    static void Pole(Transform owner, List<Vector4> list, List<float> ys, Material mat, Mesh siatka)
    {
        if (list.Count == 0 || !owner) return;
        var dane = ScriptableObject.CreateInstance<BraxClaudeTrawaDane>();
        dane.kepy = list.ToArray(); dane.y = ys.ToArray(); dane.wysokosc = .32f; dane.granicaLaki = .15f;
        AssetDatabase.CreateAsset(dane, Out + "/kepy_" + nKep.ToString("D7") + ".asset");
        var go = new GameObject("Trawa 3D (kępy, instancje)"); go.transform.SetParent(owner, false);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        var tr = go.AddComponent<BraxClaudeTrawa>(); tr.dane = dane; tr.siatka = siatka; tr.material = mat;
        nKep += list.Count;
    }

    [Serializable] sealed class KepyKomorka { public float u, v, w, h, aspekt; }
    [Serializable] sealed class KepyAtlas { public string zrodlo; public KepyKomorka[] komorki; }

    static Material KepyMaterial(Shader sh, TextureImporter imp)
    {
        if (mats.TryGetValue("kępy", out var have) && have) return have;
        bool dirty = imp.textureType != TextureImporterType.Default || !imp.sRGBTexture || imp.alphaSource != TextureImporterAlphaSource.FromInput || !imp.alphaIsTransparency
                     || !imp.mipmapEnabled || !imp.mipMapsPreserveCoverage || Mathf.Abs(imp.alphaTestReferenceValue - .45f) > .001f || imp.wrapMode != TextureWrapMode.Clamp
                     || imp.maxTextureSize != 2048 || imp.textureCompression != TextureImporterCompression.Compressed;
        if (dirty)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true; imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .45f;
            imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 2048; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport();
        }
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Detail + "kepy_atlas.png");
        var info = JsonUtility.FromJson<KepyAtlas>(File.ReadAllText(Detail + "kepy_atlas.json"));
        var m = new Material(sh) { name = "Miasto: kępy trawy (instancje)", mainTexture = atlas, enableInstancing = true };
        int nCells = Mathf.Min(5, info != null && info.komorki != null ? info.komorki.Length : 0);
        for (int i = 0; i < nCells; i++) { var c = info.komorki[i]; m.SetVector("_C" + i, new Vector4(c.u, c.v, c.w, c.h)); }
        m.SetFloat("_Cells", Mathf.Max(1, nCells)); m.SetFloat("_AtlasAspect", atlas ? atlas.width / (float)atlas.height : 2f);
        var g = Mat(false);
        if (g && g.shader && g.shader.name == "Braxhorn/OsiedleTrawa")
        {
            m.SetTexture("_GroundTex", g.mainTexture); m.SetVector("_GroundTint", grassLin);
            foreach (var pn in new[] { "_Tile", "_MacroTile", "_MacroMix", "_MacroMean", "_DryMix", "_DryMean", "_DryThr", "_DryGain", "_Stripe", "_StripeMix" }) if (g.HasProperty(pn)) m.SetFloat(pn, g.GetFloat(pn));
            if (g.HasProperty("_Dry")) m.SetVector("_Dry", g.GetVector("_Dry"));
        }
        // kolor kart = 1.15 × średnie albedo trawnika (kolor pęczków = kolor podłoża, czubki odrobinę jaśniejsze) — jak osiedle 14
        var cm = KartySrednio(Detail + "kepy_atlas.png"); float cy = .2126f * cm.x + .7152f * cm.y + .0722f * cm.z;
        float k = cy > .005f ? .133f * 1.15f / cy : 1f; var tip = new Vector4(k, k, k, 1);
        if (grassMeanLin.y > .005f && cm.x > .002f && cm.y > .002f && cm.z > .002f)
            tip = new Vector4(Mathf.Clamp(grassMeanLin.x * 1.15f / cm.x, 0f, 4f), Mathf.Clamp(grassMeanLin.y * 1.15f / cm.y, 0f, 4f), Mathf.Clamp(grassMeanLin.z * 1.15f / cm.z, 0f, 4f), 1f);
        m.SetVector("_TipTint", tip);
        AssetDatabase.CreateAsset(m, Out + "/kepy_trawy.mat"); mats["kępy"] = m;
        Notatka("kępy: karty " + nCells + ", odcień kart " + ((Vector3)(Vector4)tip).ToString("F2"));
        return m;
    }

    static Vector3 KartySrednio(string png)
    {
        var r = Vector3.zero;
        try
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (File.Exists(png) && t.LoadImage(File.ReadAllBytes(png)))
            {
                var px = t.GetPixels32(); double sr = 0, sg = 0, sb = 0, sa = 0;
                for (int i = 0; i < px.Length; i += 3)
                {
                    float a = px[i].a / 255f; if (a < .5f) continue;
                    sr += Mathf.GammaToLinearSpace(px[i].r / 255f) * a; sg += Mathf.GammaToLinearSpace(px[i].g / 255f) * a; sb += Mathf.GammaToLinearSpace(px[i].b / 255f) * a; sa += a;
                }
                if (sa > 0) r = new Vector3((float)(sr / sa), (float)(sg / sa), (float)(sb / sa));
            }
            UnityEngine.Object.DestroyImmediate(t);
        }
        catch (Exception) { }
        return r;
    }

    static Mesh KepaMesh()
    {
        string path = Out + "/kepa.asset";
        var have = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (have) return have;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var nr = new List<Vector3>(); var idx = new List<int>();
        for (int q = 0; q < 3; q++)
        {
            float a = q * 60f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .5f; int b = v.Count;
            v.Add(-d); v.Add(d); v.Add(d + Vector3.up); v.Add(-d + Vector3.up);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(1, 1)); uv.Add(new Vector2(0, 1));
            for (int k = 0; k < 4; k++) nr.Add(Vector3.up);
            idx.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
        }
        var m = new Mesh { name = "kępa miasta (3 płaszczyzny, 6 trójkątów)" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetNormals(nr); m.SetTriangles(idx, 0);
        m.bounds = new Bounds(new Vector3(0, .5f, 0), new Vector3(3f, 1.2f, 3f));
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
