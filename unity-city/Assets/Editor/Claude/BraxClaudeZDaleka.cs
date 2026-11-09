using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// OBSZARY Z DALEKA — uproszczona kopia zawartości obszaru w scenie głównej (HLOD), 9.11 (właściciel 17:42–17:45: „zobacz jak
// to się rysuje… tak samo wyspa jak do niej płynę — globalnie poprawnie”, „jak pokazują się obiekty”).
// Dotąd poza promieniem doczytania (kwadraty 120 m, wyspa 200 m) nie było NIC albo szare/ciemne pudła (BraxSylwetkiZDaleka,
// sam teren wyspy z ośmiokątem areny) — budynki, drzewa, domki wyspy „wyskakiwały” przy podejściu.
// Tu, tuż przed przeniesieniem węzłów obszaru do jego sceny (BraxClaudeObszary.Wydziel), z tych samych węzłów powstaje:
//  • z dużych brył (największy wymiar ≥ MinRozmiar): siatka zgrupowana w komórki (vertex clustering, Rossignac–Borrel) —
//    wierzchołki w tej samej komórce i o tym samym kierunku normalnej łączą się w jeden (UV, normalna, styczna, kolor
//    pierwszego), zdegenerowane trójkąty odpadają; ściany o przeciwnych normalnych nie sklejają się;
//  • z obiektów z LODGroup: ostatni poziom LOD bez zmian (drzewa z liśćmi alfa nie mogą być grupowane);
//  • materiały i tekstury te same co oryginał (ten sam wygląd, zero nowych tekstur), jedna siatka na obszar, podsiatka na materiał;
//  • komórka dobierana tak, żeby obszar miał najwyżej maxTris trójkątów (start 0.5 m — przy 120 m ~4 px na telefonie).
// Kopia ma BraxObszarZDaleka (chowa ją w klatce wczytania sceny obszaru). Bez kolizji. Siatka zapisana jako tylko-GPU
// (m_IsReadable = 0 — bez kopii w pamięci CPU telefonu).
public static class BraxClaudeZDaleka
{
    public const float MinRozmiar = 2.5f;
    public static long Suma, SumaWierzcholkow; public static int Obszary;
    public static string Podsumowanie() => "obszarów " + Obszary + ", trójkątów " + Suma + ", wierzchołków " + SumaWierzcholkow + " (~" + (SumaWierzcholkow * 60 / 1048576) + " MB GPU)";
    const int MaxTrisLodNaObiekt = 1500;
    const int MinTrisNaMaterial = 24;

    struct Zrodlo { public Renderer r; public Mesh m; public Matrix4x4 l2w; public bool dokladne; public float rozmiar; }

    sealed class Kubel
    {
        public Material mat;
        public readonly List<Vector3> v = new List<Vector3>();
        public readonly List<Vector3> n = new List<Vector3>();
        public readonly List<Vector4> t = new List<Vector4>();
        public readonly List<Vector2> uv = new List<Vector2>(), uv2 = new List<Vector2>();
        public readonly List<Color32> c = new List<Color32>();
        public readonly List<int> i = new List<int>();
        public readonly Dictionary<long, int> mapa = new Dictionary<long, int>();
    }

    sealed class Dane
    {
        public Vector3[] v, n; public Vector4[] t; public Vector2[] uv, uv2; public Color32[] c; public int[][] sub;
    }

    static bool Pomin(Renderer r)
    {
        // stare sylwetki z daleka (pudła, teren wyspy) — zastępowane przez tę kopię, nie kopiowane do niej
        return r.GetComponentInParent<BraxSylwetkiZDaleka>(true) || r.GetComponentInParent<BraxWyspaDaleko>(true)
            || r.GetComponentInParent<BraxDzielnicaZDaleka>(true) || r.GetComponentInParent<BraxObszarZDaleka>(true)
            // rzeczy w ruchu (auta, tramwaj, ludzie, animowane) — w kopii stałyby zamrożone w miejscu startu
            || r.GetComponentInParent<Rigidbody>(true) || r.GetComponentInParent<Animator>(true)
            || r.GetComponentInParent<BraxAuto>(true) || r.GetComponentInParent<BraxRuchAut>(true) || r.GetComponentInParent<BraxTramT1>(true) || r.GetComponentInParent<BraxPieszy>(true)
            || r.GetComponentInParent<BraxMieszkaniec>(true) || r.GetComponentInParent<BraxVessel>(true);
    }

    static bool Alfa(Material m)
    {
        if (!m) return false;
        int q = m.renderQueue;
        if (q >= 2450 && q < 3000) return true;
        if (m.IsKeywordEnabled("_ALPHATEST_ON")) return true;
        string s = m.shader ? m.shader.name : "";
        return m.HasProperty("_Cutoff") && (s.IndexOf("leaf", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("foliage", StringComparison.OrdinalIgnoreCase) >= 0
            || s.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("cutout", StringComparison.OrdinalIgnoreCase) >= 0
            || s.IndexOf("nature", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    static int Tris(Mesh m) { long s = 0; for (int i = 0; i < m.subMeshCount; i++) s += m.GetIndexCount(i) / 3; return (int)Math.Min(s, int.MaxValue); }

    static List<Zrodlo> Zbierz(List<Transform> wezly, out int pominieteMale, out int pominieteLod)
    {
        pominieteMale = 0; pominieteLod = 0;
        var res = new List<Zrodlo>();
        var wLod = new HashSet<Renderer>(); var ostatni = new Dictionary<Renderer, float>();
        foreach (var w in wezly)
            foreach (var g in w.GetComponentsInChildren<LODGroup>(true))
            {
                if (!g.gameObject.activeInHierarchy || !g.enabled) continue;
                var lods = g.GetLODs();
                foreach (var l in lods) foreach (var r in l.renderers) if (r) wLod.Add(r);
                var s = g.transform.lossyScale; float rozm = g.size * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                for (int k = lods.Length - 1; k >= 0; k--)
                {
                    var rs = lods[k].renderers.Where(r => r).ToArray();
                    if (rs.Length == 0) continue;
                    foreach (var r in rs) ostatni[r] = rozm;
                    break;
                }
            }
        foreach (var w in wezly)
            foreach (var r in w.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || Pomin(r)) continue;
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue;
                var mats = r.sharedMaterials;
                if (mats.Length == 0 || mats.All(m => !m)) continue;
                if (mats.Any(m => m && m.HasProperty("_FadeEnd") && m.HasProperty("_FadeStart"))) continue;   // kępy trawy — gasną w shaderze
                bool lod = wLod.Contains(r);
                float rozmiar;
                if (lod)
                {
                    if (!ostatni.TryGetValue(r, out rozmiar)) continue;   // wyższe poziomy LOD — z daleka i tak niewidoczne
                    if (rozmiar < MinRozmiar) { pominieteMale++; continue; }
                    if (Tris(mf.sharedMesh) > MaxTrisLodNaObiekt) { pominieteLod++; continue; }
                }
                else
                {
                    var b = r.bounds; rozmiar = Mathf.Max(b.size.x, b.size.y, b.size.z);
                    if (rozmiar < MinRozmiar) { pominieteMale++; continue; }
                }
                bool dokladne = lod || mats.Any(Alfa);
                if (dokladne && !lod && Tris(mf.sharedMesh) > MaxTrisLodNaObiekt) { pominieteLod++; continue; }
                res.Add(new Zrodlo { r = r, m = mf.sharedMesh, l2w = r.transform.localToWorldMatrix, dokladne = dokladne, rozmiar = rozmiar });
            }
        return res;
    }

    // pamięć edytora w CI: w schowku tylko małe siatki (powtarzane egzemplarze — drzewa, domki); duże scalone siatki miasta
    // czytane przy użyciu (zwykle 1 egzemplarz) i od razu oddawane
    const int MaxWierzcholkowWSchowku = 30000;
    static Dane Czytaj(Mesh m, Dictionary<Mesh, Dane> cache)
    {
        if (cache.TryGetValue(m, out var d)) return d;
        d = new Dane { v = m.vertices, n = m.normals, t = m.tangents, uv = m.uv, uv2 = m.uv2, c = m.colors32, sub = new int[m.subMeshCount][] };
        for (int s = 0; s < m.subMeshCount; s++)
            d.sub[s] = m.GetTopology(s) == MeshTopology.Triangles ? m.GetTriangles(s) : new int[0];
        if (d.v.Length <= MaxWierzcholkowWSchowku) cache[m] = d;
        return d;
    }

    static int KlasaNormalnej(Vector3 n)
    {
        float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
        if (ax >= ay && ax >= az) return n.x >= 0 ? 0 : 1;
        if (ay >= az) return n.y >= 0 ? 2 : 3;
        return n.z >= 0 ? 4 : 5;
    }

    static long Klucz(Vector3 p, float kom, int klasa)
    {
        const long M = (1L << 20) - 1, O = 1L << 19;
        long x = (Mathf.FloorToInt(p.x / kom) + O) & M, y = (Mathf.FloorToInt(p.y / kom) + O) & M, z = (Mathf.FloorToInt(p.z / kom) + O) & M;
        return x | (y << 20) | (z << 40) | ((long)klasa << 60);
    }

    // jedno przejście grupowania; zwraca kubły materiałów (trójkąty po odrzuceniu zdegenerowanych)
    static Dictionary<Material, Kubel> Grupuj(List<Zrodlo> zr, float kom, Dictionary<Mesh, Dane> cache, out long tris)
    {
        var kub = new Dictionary<Material, Kubel>();
        tris = 0;
        foreach (var z in zr)
        {
            var d = Czytaj(z.m, cache);
            var mats = z.r.sharedMaterials;
            var l2w = z.l2w; bool odwroc = l2w.determinant < 0f;
            int nv = d.v.Length;
            bool maN = d.n.Length == nv, maT = d.t.Length == nv, maUv = d.uv.Length == nv, maUv2 = d.uv2.Length == nv, maC = d.c.Length == nv;
            for (int s = 0; s < d.sub.Length; s++)
            {
                var mat = s < mats.Length ? mats[s] : null;
                if (!mat || d.sub[s].Length == 0) continue;
                if (!kub.TryGetValue(mat, out var k)) { k = new Kubel { mat = mat }; kub[mat] = k; }
                // wierzchołki tej podsiatki → indeksy w kuble (grupowane albo dokładne)
                var idx = new Dictionary<int, int>();
                int Wez(int i)
                {
                    if (idx.TryGetValue(i, out int w)) return w;
                    var p = l2w.MultiplyPoint3x4(d.v[i]);
                    var n = maN ? l2w.MultiplyVector(d.n[i]).normalized : Vector3.up;
                    if (!z.dokladne)
                    {
                        long key = Klucz(p, kom, KlasaNormalnej(n));
                        if (k.mapa.TryGetValue(key, out w)) { idx[i] = w; return w; }
                        w = k.v.Count; k.mapa[key] = w;
                    }
                    else w = k.v.Count;
                    k.v.Add(p); k.n.Add(n);
                    if (maT) { var t = d.t[i]; var tw = l2w.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized; k.t.Add(new Vector4(tw.x, tw.y, tw.z, odwroc ? -t.w : t.w)); }
                    else k.t.Add(new Vector4(1, 0, 0, 1));
                    k.uv.Add(maUv ? d.uv[i] : Vector2.zero); k.uv2.Add(maUv2 ? d.uv2[i] : Vector2.zero);
                    k.c.Add(maC ? d.c[i] : new Color32(255, 255, 255, 255));
                    idx[i] = w; return w;
                }
                var tri = d.sub[s];
                for (int j = 0; j + 2 < tri.Length; j += 3)
                {
                    int a = Wez(tri[j]), b = Wez(tri[j + 1]), c = Wez(tri[j + 2]);
                    if (a == b || b == c || a == c) continue;
                    if (odwroc) { k.i.Add(a); k.i.Add(c); k.i.Add(b); } else { k.i.Add(a); k.i.Add(b); k.i.Add(c); }
                    tris++;
                }
            }
        }
        return kub;
    }

    /// <summary>Tworzy kopię z daleka dla węzłów obszaru (wywołanie PRZED przeniesieniem ich do sceny obszaru). Zwraca obiekt albo null.</summary>
    public static GameObject Utworz(Scene main, string scena, List<Transform> wezly, float komorka, int maxTris, string folder)
    {
        var zr = Zbierz(wezly, out int male, out int lodDuze);
        if (zr.Count == 0) { Debug.Log("BRAXHORN_Z_DALEKA " + scena + ": brak dużych brył (pominięte drobne " + male + ") — bez kopii"); return null; }
        var cache = new Dictionary<Mesh, Dane>();
        long zrodloTris = zr.Sum(z => (long)Tris(z.m));
        float kom = Mathf.Max(.05f, komorka);
        Dictionary<Material, Kubel> kub = null; long tris = 0; int przejscia = 0; float komUzyta = kom;
        for (; przejscia < 6; przejscia++)
        {
            komUzyta = kom;
            kub = Grupuj(zr, kom, cache, out tris);
            if (tris <= maxTris) break;
            // powierzchnie: trójkąty ~ 1/komórka² — skok o pierwiastek nadmiaru (+10 %), najmniej ×1.3
            kom *= Mathf.Max(1.3f, Mathf.Sqrt(tris / (float)maxTris) * 1.1f);
        }
        cache.Clear();
        var kubly = kub.Values.Where(k => k.i.Count / 3 >= MinTrisNaMaterial).ToList();
        if (kubly.Count == 0) { Debug.Log("BRAXHORN_Z_DALEKA " + scena + ": po uproszczeniu nic nie zostało — bez kopii"); return null; }
        var mesh = new Mesh { name = "Z daleka: " + scena };
        int nv = kubly.Sum(k => k.v.Count);
        mesh.indexFormat = nv > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        var V = new List<Vector3>(nv); var N = new List<Vector3>(nv); var T = new List<Vector4>(nv); var U = new List<Vector2>(nv); var U2 = new List<Vector2>(nv); var C = new List<Color32>(nv);
        var subs = new List<int[]>();
        foreach (var k in kubly)
        {
            int o = V.Count;
            V.AddRange(k.v); N.AddRange(k.n); T.AddRange(k.t); U.AddRange(k.uv); U2.AddRange(k.uv2); C.AddRange(k.c);
            var ii = new int[k.i.Count]; for (int j = 0; j < ii.Length; j++) ii[j] = k.i[j] + o;
            subs.Add(ii);
        }
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetTangents(T); mesh.SetUVs(0, U); mesh.SetUVs(1, U2); mesh.SetColors(C);
        mesh.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, false);
        mesh.RecalculateBounds();
        string dir = folder + "/ZDaleka";
        Directory.CreateDirectory(dir); AssetDatabase.Refresh();
        string path = BraxZapisAssetu.Utworz(mesh, AssetDatabase.GenerateUniqueAssetPath(dir + "/" + scena + ".asset"));
        // tylko GPU: bez kopii siatki w pamięci CPU telefonu (nikt jej nie czyta w grze)
        var zap = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (!zap) zap = mesh;
        var so = new SerializedObject(zap); var pr = so.FindProperty("m_IsReadable");
        if (pr != null) { pr.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(zap); AssetDatabase.SaveAssets(); }

        var root = main.GetRootGameObjects().FirstOrDefault(g => g.name == "CLAUDE z daleka (obszary)");
        if (!root) { root = new GameObject("CLAUDE z daleka (obszary)"); SceneManager.MoveGameObjectToScene(root, main); root.transform.position = Vector3.zero; }
        var go = new GameObject("Z daleka: " + scena);
        go.transform.SetParent(root.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = zap;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = kubly.Select(k => k.mat).ToArray();
        mr.shadowCastingMode = ShadowCastingMode.On;   // zasada 1 D: cienie zawsze (rzucane tylko w zasięgu cieni trybu)
        mr.receiveShadows = true;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        var zd = go.AddComponent<BraxObszarZDaleka>(); zd.scena = scena; zd.renderery = new Renderer[] { mr };
        long koniec = subs.Sum(x => (long)x.Length / 3);
        Suma += koniec; SumaWierzcholkow += nv; Obszary++;
        Debug.Log("BRAXHORN_Z_DALEKA " + scena + ": brył " + zr.Count + " (trójkątów źródła " + zrodloTris + ") → " + koniec + " trójkątów, wierzchołków " + nv +
                  ", materiałów " + kubly.Count + ", komórka " + komUzyta.ToString("0.00") + " m (przejść " + Mathf.Min(przejscia + 1, 6) + "), pominięte drobne " + male +
                  ", ciężkie LOD/alfa " + lodDuze + ", plik " + path);
        return go;
    }
}
