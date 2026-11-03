using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// PRAWDZIWE POLSKIE ZNAKI DROGOWE (właściciel 3.11 10:32: „Znaki normalne prawdziwe! Na drogach”, 10:58: „nie masz prawdziwych
// znaków drogowych? Nie można tego pobrać?”). Tarcze z urzędowych wzorów (Wikimedia Commons „PL road sign *.svg”, domena
// publiczna — wzory z rozporządzenia; PNG 512 px pobierane w CI przez fetch_polyhaven.py do PolyHaven/znaki_pl/). Tarcza =
// płyta z wycięciem po kształcie znaku (alfa wzoru, nie prostokąt): lico odblaskowe ze wzorem, tył — szara blacha z tą samą
// sylwetką; obejmy na słupku ocynkowanym Ø 60 mm. Wymiary jak „średnie” znaki (A: bok 900 mm, B/C: Ø 600 mm, D: 600 mm)
// × K, dolna krawędź najniższej tarczy 2.2 m × K nad chodnikiem. Brak pliku wzoru = znaku NIE ma (żadnych zastępczych brył),
// wołający może zostawić stary. Log: BraxZnakiPL.Raport().
public static class BraxZnakiPL
{
    const float K = 1.294f;
    const string Dir = "Assets/Source/Claude/PolyHaven/znaki_pl/";
    const string Out = "Assets/PlayableGenerated/ZnakiPL";
    sealed class Wzor { public Material przod, tyl; public Mesh plyta; public float w, h; }
    static readonly Dictionary<string, Wzor> cache = new Dictionary<string, Wzor>();
    static Material stalOcynk, obejma;
    public static int Slupy, Tarcze, Brak;
    static readonly List<string> brakTypow = new List<string>();

    public static void Zeruj() { Slupy = Tarcze = Brak = 0; brakTypow.Clear(); cache.Clear(); stalOcynk = obejma = null; }
    public static bool Jest(string typ) => File.Exists(Dir + typ + ".png");
    public static string Raport() => "BRAXHORN_ZNAKI_PL słupów=" + Slupy + " tarcz=" + Tarcze + " brak_wzoru=" + Brak + (brakTypow.Count > 0 ? " [" + string.Join(", ", brakTypow) + "]" : "");

    // szerokość tarczy (m, przed × K): A — bok trójkąta 0.9; D-1 (romb) — przekątna 0.85; pozostałe 0.6
    static float Szer(string typ) => typ.StartsWith("A") ? .9f : typ == "D-1" ? .85f : .6f;

    static void Importuj(string p)
    {
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        if (!(AssetImporter.GetAtPath(p) is TextureImporter imp)) return;
        imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true; imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Trilinear;
        imp.anisoLevel = 4; imp.maxTextureSize = 512; imp.npotScale = TextureImporterNPOTScale.None; imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.SaveAndReimport();
    }

    static Material Nowy(string nazwa, Texture2D t, float szorstkosc, float metal, Color kolor)
    {
        var sh = Shader.Find("Braxhorn/CityStaticPBR");
        var m = new Material(sh) { name = nazwa, enableInstancing = true };
        if (t) m.SetTexture("_MainTex", t);
        m.SetColor("_Color", kolor); m.SetFloat("_Roughness", szorstkosc); m.SetFloat("_Metallic", metal);
        if (t) { m.SetFloat("_Cutoff", .5f); m.SetFloat("_AlphaClip", 1f); m.renderQueue = (int)RenderQueue.AlphaTest; }
        m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        Directory.CreateDirectory(Out);
        BraxZapisAssetu.Utworz(m, Out + "/" + nazwa.Replace(' ', '_').Replace(':', '_').Replace('/', '_').Replace('(', '_').Replace(')', '_') + ".mat");
        return m;
    }

    static Wzor Pobierz(string typ)
    {
        if (cache.TryGetValue(typ, out var have) && have.przod && have.plyta) return have;
        string p = Dir + typ + ".png";
        if (!File.Exists(p)) return null;
        Importuj(p);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        if (!tex) return null;
        // tył: ta sama sylwetka (alfa wzoru), szara blacha — osobny PNG
        var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!raw.LoadImage(File.ReadAllBytes(p))) { Object.DestroyImmediate(raw); return null; }
        var px = raw.GetPixels32();
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(146, 149, 152, px[i].a);
        raw.SetPixels32(px); raw.Apply();
        Directory.CreateDirectory(Out);
        string pt = Out + "/" + typ + "_tyl.png"; File.WriteAllBytes(pt, raw.EncodeToPNG());
        float asp = raw.height / (float)raw.width; Object.DestroyImmediate(raw);
        Importuj(pt);
        var texT = AssetDatabase.LoadAssetAtPath<Texture2D>(pt);
        var wz = new Wzor { w = Szer(typ) * K };
        wz.h = wz.w * asp;
        wz.przod = Nowy("Znak " + typ + " — lico odblaskowe", tex, .45f, 0f, Color.white);
        wz.tyl = Nowy("Znak " + typ + " — tył (blacha ocynkowana)", texT, .55f, .55f, Color.white);
        // płyta: lico (+z lokalnie) i tył (−z), 12 mm grubości; u rośnie w prawo patrzącego na lico (lokalne −x)
        float hw = wz.w * .5f, hh = wz.h * .5f, e = .006f;
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
        void V(float x, float y, float z, Vector3 nn) { v.Add(new Vector3(x, y, z)); n.Add(nn); uv.Add(new Vector2(.5f - x / wz.w, .5f + y / wz.h)); }
        V(hw, hh, e, Vector3.forward); V(-hw, hh, e, Vector3.forward); V(-hw, -hh, e, Vector3.forward); V(hw, -hh, e, Vector3.forward);
        V(-hw, hh, -e, Vector3.back); V(hw, hh, -e, Vector3.back); V(hw, -hh, -e, Vector3.back); V(-hw, -hh, -e, Vector3.back);
        var m = new Mesh { name = "Znak " + typ + " — płyta" };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.subMeshCount = 2;
        m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0); m.SetTriangles(new[] { 4, 5, 6, 4, 6, 7 }, 1);
        m.RecalculateBounds(); m.RecalculateTangents();
        BraxZapisAssetu.Utworz(m, Out + "/" + typ + "_plyta.asset");
        wz.plyta = m;
        cache[typ] = wz;
        return wz;
    }

    /// <summary>Słupek ocynkowany z tarczami (typy od góry). stopa — punkt na chodniku; przod — dokąd patrzy lico
    /// (w stronę nadjeżdżających). Zwraca null, gdy nie ma żadnego wzoru (nic nie stawia).</summary>
    public static GameObject Slup(Transform parent, string nazwa, Vector3 stopa, Vector3 przod, params string[] typy)
    {
        przod.y = 0f; if (przod.sqrMagnitude < 1e-6f) return null; przod.Normalize();
        var lista = new List<(string typ, Wzor wz)>();
        foreach (var typ in typy)
        {
            var wz = Pobierz(typ);
            if (wz != null) lista.Add((typ, wz)); else { Brak++; if (!brakTypow.Contains(typ)) brakTypow.Add(typ); }
        }
        if (lista.Count == 0) return null;
        if (!stalOcynk) stalOcynk = BraxCityMaterials.Surface("Znak: słupek ocynkowany", new Color(.63f, .65f, .67f), .45f, .85f);
        if (!obejma) obejma = BraxCityMaterials.Surface("Znak: obejmy (stal ocynkowana ciemna)", new Color(.38f, .39f, .41f), .5f, .8f);
        var g = new GameObject(nazwa); g.transform.SetParent(parent, false);
        g.transform.position = stopa; g.transform.rotation = Quaternion.LookRotation(przod, Vector3.up);
        float rS = .03f * K, d = rS + .04f, y = 2.2f * K, odstep = .06f * K;
        var ob = new BraxRegionMeble.Bud();
        // od dołu: ostatni typ najniżej
        for (int i = lista.Count - 1; i >= 0; i--)
        {
            var (typ, wz) = lista[i];
            var t = new GameObject("Znak " + typ); t.transform.SetParent(g.transform, false);
            t.transform.localPosition = new Vector3(0f, y + wz.h * .5f, d); t.transform.localRotation = Quaternion.identity;
            t.AddComponent<MeshFilter>().sharedMesh = wz.plyta;
            var mr = t.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { wz.przod, wz.tyl };
            mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true;
            GameObjectUtility.SetStaticEditorFlags(t, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            // obejmy: 2 na tarczę (¼ i ¾ wysokości), od słupka do tyłu tarczy
            foreach (float f in new[] { .25f, .75f })
            {
                var c = g.transform.TransformPoint(new Vector3(0f, y + wz.h * f, d * .5f));
                ob.Box(c, new Vector3(.05f * K, .035f * K, d + .01f), g.transform.rotation);
            }
            y += wz.h + odstep; Tarcze++;
        }
        var sl = new BraxRegionMeble.Bud();
        sl.Cyl(stopa, stopa + Vector3.up * (y - odstep + .05f), rS, 12);
        sl.Obiekt("Znak: słupek ocynkowany Ø 60 mm", g.transform, stalOcynk, false, true);
        ob.Obiekt("Znak: obejmy", g.transform, obejma, false, false);
        BraxRegionMeble.Kolizja(g.transform, "Znak: kolizja słupka", stopa + Vector3.up * (y * .5f), new Vector3(.12f, y, .12f));
        Slupy++;
        return g;
    }
}
