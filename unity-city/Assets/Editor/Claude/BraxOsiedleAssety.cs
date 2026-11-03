using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// MODELE ZE SKETCHFABA WE WNĘTRZACH OSIEDLA (build 45, runda 2). Paczka zaszyfrowana w repo (unity-city/Prywatne/
// osiedle_wnetrza.tar.gz.enc — CC-BY, autorzy w ekranie Autorzy), odszyfrowywana w CI przez unity-tools/claude/prywatne.py do
// Assets/Source/Prywatne/Osiedle/<paczka>/ (poza gitem). Brak klucza albo paczki = wnętrza z kodu jak dotąd
// (log BRAXHORN_OSIEDLE_ASSETY brak …), nic się nie wywraca. Wzór: BraxMiastoAssety (miasto).
//  • Model(paczka, plik): najpierw import _tekstury/<plik>/ (kompresja BraxOsiedleAssetyTekstury), potem GLB (glTFast),
//    sprawdzenie, czy materiały mają tekstury (inaczej ponowny import), cache;
//  • materiały glTFast → materiały miasta/osiedla (BraxCityMaterials.Imported: ten sam shader co auta i GLB Codexa — shadery
//    glTFast nie wypadają z buildu na telefon);
//  • modele w jednostkach źródła — skala liczona z obrysu do REALNYCH wymiarów × K (świat pod Braxa).
public static class BraxOsiedleAssety
{
    public const string Baza = "Assets/Source/Prywatne/Osiedle/";
    public const string Wnetrza = "osiedle_wnetrza";
    static float K => BraxClaudeOsiedle.K;
    static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
    static readonly HashSet<string> brakZgloszony = new HashSet<string>();
    public static int Postawione, Bledy;

    public static void Log(string s) => Debug.Log("BRAXHORN_OSIEDLE_ASSETY " + s);
    public static void Blad(string co, Exception e) { Bledy++; Debug.LogError("BRAXHORN_OSIEDLE_ASSETY " + co + " DO POPRAWKI: " + e.Message); }

    public static bool Jest(string paczka) => Directory.Exists(Baza + paczka) && Directory.GetFiles(Baza + paczka, "*.glb").Length > 0;

    public static GameObject Model(string paczka, string plik)
    {
        string p = Baza + paczka + "/" + plik + ".glb";
        if (cache.TryGetValue(p, out var g) && g) return g;
        if (!File.Exists(p))
        {
            if (brakZgloszony.Add(p)) Log("brak " + p + " (paczka " + paczka + " nieodszyfrowana — sekret/paczka w CI) — zostaje wersja z kodu");
            return null;
        }
        string tex = Baza + paczka + "/_tekstury/" + plik;
        if (Directory.Exists(tex)) AssetDatabase.ImportAsset(tex, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        g = Wczytaj(p);
        if (g && Directory.Exists(tex) && !Teksturowany(p))
        {
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            g = Wczytaj(p);
            Log("ponowny import (tekstury po modelu): " + p + " → " + (Teksturowany(p) ? "z teksturami" : "NADAL bez tekstur — DO POPRAWKI"));
        }
        if (!g) { if (brakZgloszony.Add(p)) Log("import nieudany: " + p + " — DO POPRAWKI"); return null; }
        cache[p] = g;
        return g;
    }
    public static GameObject ModelWnetrz(string plik) => Model(Wnetrza, plik);

    static GameObject Wczytaj(string p) => AssetDatabase.LoadAssetAtPath<GameObject>(p) ?? AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    static bool Teksturowany(string p)
    {
        foreach (var m in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>())
            foreach (var n in m.GetTexturePropertyNames())
                if (m.GetTexture(n)) return true;
        return false;
    }

    public static Texture2D Tekstura(string paczka, string sciezkaWzgledna)
    {
        string p = Baza + paczka + "/" + sciezkaWzgledna;
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static readonly Dictionary<Material, Material> materialy = new Dictionary<Material, Material>();
    public static Material MaterialOsiedla(Material src)
    {
        if (!src) return src;
        if (materialy.TryGetValue(src, out var m) && m) return m;
        try { m = BraxCityMaterials.Imported(src); }
        catch (Exception e) { Log("materiał " + src.name + " bez konwersji (" + e.Message + ")"); m = src; }
        materialy[src] = m;
        return m;
    }

    public static void Przygotuj(GameObject o, bool cienie = true)
    {
        foreach (var r in o.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterials = r.sharedMaterials.Select(MaterialOsiedla).ToArray();
            r.shadowCastingMode = cienie ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
        foreach (var a in o.GetComponentsInChildren<Animation>(true)) UnityEngine.Object.DestroyImmediate(a);
        foreach (var a in o.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
    }

    public static Bounds Granice(GameObject o) => Granice(o.transform);
    public static Bounds Granice(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        var b = rs[0].bounds; foreach (var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }

    // Kopia modelu: skala tak, by WYMIAR odniesienia (szerokość — dłuższy bok w poziomie, albo wysokość) miał realny rozmiar × K;
    // podstawa (min y obrysu) w p.y, środek obrysu w (p.x, p.z), obrót yaw.
    public static GameObject Postaw(Transform parent, GameObject zrodlo, string nazwa, Vector3 p, float yaw, float? szerokoscRealna = null, float? wysokoscRealna = null, bool cienie = true)
    {
        if (!zrodlo) return null;
        var o = (GameObject)UnityEngine.Object.Instantiate(zrodlo, parent, false);
        o.name = nazwa;
        // 3.11 15:28 (hokery bokiem, kieliszki ogromne): korzeń modeli Sketchfab ma obrót −90° X (Z→Y) i skalę — zachować je, yaw dokładany na wierzch
        var r0 = zrodlo.transform.localRotation; var s0 = zrodlo.transform.localScale;
        o.transform.position = Vector3.zero; o.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * r0; o.transform.localScale = s0;
        var b = Granice(o);
        float s = 1f;
        if (szerokoscRealna.HasValue) s = szerokoscRealna.Value * K / Mathf.Max(1e-4f, Mathf.Max(b.size.x, b.size.z));
        else if (wysokoscRealna.HasValue) s = wysokoscRealna.Value * K / Mathf.Max(1e-4f, b.size.y);
        o.transform.localScale = s0 * s;
        b = Granice(o);
        o.transform.position += new Vector3(p.x - b.center.x, p.y - b.min.y, p.z - b.center.z);
        Przygotuj(o, cienie);
        Postawione++;
        return o;
    }

    // Węzły modelu: pierwsze dziecko o nazwie dokładnej / z przedrostkiem (bez wielkości liter).
    public static Transform Znajdz(Transform t, string nazwa)
    {
        foreach (var c in t.GetComponentsInChildren<Transform>(true)) if (string.Equals(c.name, nazwa, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
    public static List<Transform> Zaczyna(Transform t, string przedrostek)
        => t.GetComponentsInChildren<Transform>(true).Where(c => c.name.StartsWith(przedrostek, StringComparison.OrdinalIgnoreCase)).ToList();

    // Części modelu wg obrysu: najwyższe węzły z siatką pod korzeniem, posortowane malejąco wg szerokości (do podziału zestawu
    // na sofę, fotele, puf, gdy nazwy węzłów nieznane).
    public static List<Transform> Czesci(GameObject o)
    {
        var res = new List<Transform>();
        void Zbierz(Transform t, int glebokosc)
        {
            bool maRenderer = t.GetComponent<Renderer>();
            if (t != o.transform && (maRenderer || glebokosc >= 1 && t.childCount > 0 && t.GetComponentsInChildren<Renderer>(true).Length > 0 && t.parent != o.transform && t.parent.childCount > 1))
            { res.Add(t); return; }
            for (int i = 0; i < t.childCount; i++) Zbierz(t.GetChild(i), glebokosc + 1);
        }
        Zbierz(o.transform, 0);
        return res.OrderByDescending(t => { var b = Granice(t); return Mathf.Max(b.size.x, b.size.z); }).ToList();
    }

    public static BoxCollider Kolizja(Transform parent, GameObject o, string nazwa, float skurcz = .92f)
    {
        var b = Granice(o);
        var c = new GameObject(nazwa).AddComponent<BoxCollider>(); c.transform.SetParent(parent, false);
        c.transform.position = b.center; c.size = new Vector3(b.size.x * skurcz, b.size.y, b.size.z * skurcz);
        return c;
    }
}
