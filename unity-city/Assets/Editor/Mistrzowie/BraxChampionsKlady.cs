using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>THE OWNER'S QUADS (24.11.2026 11:31-11:45: "tu są kłady", "żeby się kręciły, kierownica, żeby się kręciła tak samo",
/// "minimum 4 kłady ... każdy inny kolor"). The encrypted owner pack braxhorn-sklep-klady-01 (unity-tools/mistrzowie/klady/
/// przygotuj_klady.py; staged by install_sklep_modele.py to Assets/Source/Prywatne/Miasto/pojazdy_klady_01) holds each
/// quad cut into a body, four wheels and the handlebar - each part about its own pivot, in real metres, with two far LODs -
/// and the colour maps of six paints. A quad here: the parts under their pivots (a hub per wheel, the front ones the
/// steering knuckles; the bars on their steering axis), the city's PBR material with the paint's colour map, a LODGroup per
/// part, a rigid body, the body's box collider and BraxChampionsKlad (the ride). Without the pack (no KLUCZ) nothing is placed
/// and the build goes on.</summary>
public static class BraxChampionsKlady
{
    public const string Paczka = "pojazdy_klady_01";
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/Klady";
    static float K => BraxClaudeOsiedle.K;

    [Serializable] sealed class Lod { public string plik; public int trojkaty; }
    [Serializable] sealed class Czesc { public string nazwa, plik; public float[] pivot; public int trojkaty; public float promien, szerokosc; public float[] os; public Lod[] lody; }
    [Serializable] sealed class Wariant { public string kolor, tekstura; public float farba_udzial; }
    [Serializable] sealed class Model
    {
        public string model, wzor, normal, mr; public float skala, dlugosc, szerokosc, wysokosc, rozstaw_osi, rozstaw_kol;
        public int trojkaty; public float[] siodlo; public Czesc[] czesci; public Wariant[] warianty;
    }
    [Serializable] sealed class Katalog { public string zrodlo, osie; public Model[] modele; }

    /// <summary>The six quads of the rental booth: the utility Rancher in three paints, the sport Raptor in three.</summary>
    public static readonly (string model, string kolor, string napis)[] Flota =
    {
        ("rancher", "czerwony", "KŁAD · CZERWONY"), ("rancher", "niebieski", "KŁAD · NIEBIESKI"), ("rancher", "bialy", "KŁAD · BIAŁY"),
        ("raptor", "limonkowy", "KŁAD · LIMONKOWY"), ("raptor", "pomaranczowy", "KŁAD · POMARAŃCZOWY"), ("raptor", "fioletowy", "KŁAD · FIOLETOWY"),
    };
    // the machines (real): the Rancher 263 kg, ~70 km/h, a calm launch, 4x4; the Raptor 700R 190 kg, ~110 km/h, a hard one.
    // + 90 kg of the rider (hondanews.com 2024 FourTrax Rancher; Yamaha Raptor 700R)
    static (float masa, float vMax, float przysp, bool napedAll, float skok) Osiagi(string model) =>
        model == "raptor" ? (280f, 105f, 5.2f, false, .26f) : (355f, 75f, 3.4f, true, .21f);

    public static bool Jest => BraxMiastoAssety.Jest(Paczka);
    public static int Postawione;
    static Katalog kat;
    static Katalog Kat()
    {
        if (kat != null) return kat;
        string p = BraxMiastoAssety.Baza + Paczka + "/katalog.json";
        if (!File.Exists(p)) return null;
        kat = JsonUtility.FromJson<Katalog>(File.ReadAllText(p));
        return kat;
    }

    static readonly Dictionary<string, Material> materialy = new Dictionary<string, Material>();
    static Texture2D Tekstura(string nazwa, string ext)
    {
        string p = BraxMiastoAssety.Baza + Paczka + "/_tekstury/" + nazwa + ext;
        if (!File.Exists(p)) return null;
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }
    // the city's PBR (CityStaticPBR, the sun of every material): the paint's colour map, Tripo's normal map (RGB, glTF's +Y) and
    // its metallic-roughness map as ARM (R = 1: no AO map; G roughness; B metal); both sides (Tripo's thin parts)
    static Material Material(Model m, Wariant w)
    {
        string id = m.model + "_" + w.kolor;
        if (materialy.TryGetValue(id, out var mat) && mat) return mat;
        var kolor = Tekstura(w.tekstura, ".jpg"); var nrm = Tekstura(m.normal, ".jpg"); var arm = Tekstura(m.mr, ".png");
        if (!kolor || !nrm || !arm) throw new InvalidOperationException("quad maps missing: " + id);
        mat = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "Mistrzowie kład " + id, enableInstancing = true };
        mat.SetTexture("_MainTex", kolor); mat.SetTexture("_BumpMap", nrm); mat.SetTexture("_ARM", arm);
        mat.SetFloat("_NormalEncoding", 0f); mat.SetFloat("_Roughness", 1f); mat.SetFloat("_Metallic", 1f); mat.SetFloat("_UseARMForAO", 0f);
        mat.SetFloat("_VertexTint", 0f); mat.SetFloat("_Cull", 0f); mat.SetColor("_Color", Color.white);
        mat.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        Directory.CreateDirectory(Out + "/Materials");
        string path = Out + "/Materials/klad_" + id + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        materialy[id] = mat; return mat;
    }

    // a part's mesh from the pack's .glb (glTFast), with tangents (the normal map needs them; a copy if the import has none)
    static readonly Dictionary<string, Mesh> siatki = new Dictionary<string, Mesh>();
    static Mesh Siatka(string plik)
    {
        if (siatki.TryGetValue(plik, out var s) && s) return s;
        var g = BraxMiastoAssety.Model(Paczka, plik);
        var mf = g ? g.GetComponentInChildren<MeshFilter>(true) : null;
        if (!mf || !mf.sharedMesh) throw new InvalidOperationException("quad part missing: " + plik);
        s = mf.sharedMesh;
        if (s.tangents == null || s.tangents.Length != s.vertexCount)
        {
            Directory.CreateDirectory(Folder);
            var kopia = UnityEngine.Object.Instantiate(s); kopia.name = plik; kopia.RecalculateTangents();
            string path = Folder + "/" + plik + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path)) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(kopia, path); s = kopia;
        }
        siatki[plik] = s; return s;
    }

    static Vector3 V(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;

    // a part (its LOD0 and the two far ones) under the pivot t; the meshes are in real metres, x K here
    static int Czesci(Transform t, Czesc c, Material mat, string nazwa)
    {
        var lody = new List<LOD>(); int tri = 0;
        var pliki = new List<string> { c.plik }; if (c.lody != null) pliki.AddRange(c.lody.Select(l => l.plik));
        float[] progi = { .20f, .07f, .012f };                         // the share of the screen's height each LOD holds to
        for (int i = 0; i < pliki.Count; i++)
        {
            var o = new GameObject(nazwa + (i == 0 ? "" : " LOD" + i)); o.transform.SetParent(t, false); o.transform.localScale = Vector3.one * K;
            var mesh = Siatka(pliki[i]); o.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = o.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = i == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off; r.receiveShadows = true;
            lody.Add(new LOD(progi[Mathf.Min(i, progi.Length - 1)], new Renderer[] { r }));
            if (i == 0) tri += mesh.triangles.Length / 3;
        }
        if (lody.Count > 1) { var lg = t.gameObject.AddComponent<LODGroup>(); lg.SetLODs(lody.ToArray()); lg.RecalculateBounds(); }
        return tri;
    }

    /// <summary>One quad at p (its wheels on the ground), facing yaw; null without the pack.</summary>
    public static GameObject Postaw(Transform parent, string model, string kolor, string napis, Vector3 p, float yaw,
                                    BraxChampionsTorCzas tor = null, AudioClip wolne = null, AudioClip jazda = null)
    {
        if (!Jest) { BraxMiastoAssety.Log("kład " + model + " " + kolor + ": brak paczki " + Paczka + " (KLUCZ w CI) - miejsce zostaje puste"); return null; }
        var k = Kat(); var m = k?.modele?.FirstOrDefault(x => x.model == model); var w = m?.warianty?.FirstOrDefault(x => x.kolor == kolor);
        if (m == null || w == null) throw new InvalidOperationException("quad not in the pack's catalogue: " + model + " " + kolor);
        var mat = Material(m, w);
        var root = new GameObject("KŁAD | " + napis + " | " + m.wzor);
        root.transform.SetParent(parent, false); root.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, yaw, 0f));
        var klad = root.AddComponent<BraxChampionsKlad>();
        klad.nazwa = napis; klad.K = K; klad.tor = tor; klad.silnikWolne = wolne; klad.silnikJazda = jazda;
        int tri = 0;
        string[] kola = { "kolo_przod_lewe", "kolo_przod_prawe", "kolo_tyl_lewe", "kolo_tyl_prawe" };
        foreach (var c in m.czesci)
        {
            int ki = Array.IndexOf(kola, c.nazwa);
            if (ki >= 0)
            {
                // the hub (rides the suspension; the front ones steer) and the wheel spinning in it
                var piasta = new GameObject("piasta " + c.nazwa).transform; piasta.SetParent(root.transform, false);
                piasta.localPosition = V(c.pivot) * K;
                var kolo = new GameObject("koło " + c.nazwa).transform; kolo.SetParent(piasta, false);
                tri += Czesci(kolo, c, mat, c.plik);
                klad.piasty[ki] = piasta; klad.kola[ki] = kolo; klad.spoczynek[ki] = piasta.localPosition;
                if (ki == 0) klad.promien = c.promien;
            }
            else if (c.nazwa == "kierownica")
            {
                var os = new GameObject("kierownica (oś skrętu)").transform; os.SetParent(root.transform, false);
                os.localPosition = V(c.pivot) * K;
                tri += Czesci(os, c, mat, c.plik);
                klad.kierownica = os; klad.kierownicaSpoczynek = Quaternion.identity;
                klad.osKierownicy = V(c.os).sqrMagnitude > .5f ? V(c.os).normalized : Vector3.up;
            }
            else
            {
                var korpus = new GameObject("korpus").transform; korpus.SetParent(root.transform, false);
                tri += Czesci(korpus, c, mat, c.plik);
            }
        }
        var siodlo = new GameObject("siodło").transform; siodlo.SetParent(root.transform, false); siodlo.localPosition = V(m.siodlo) * K;
        klad.siodlo = siodlo;
        // the body's box: between the wheels, from the footrests to the seat (the wheels are the rays)
        var kol = new GameObject("kolizja korpusu"); kol.transform.SetParent(root.transform, false);
        var box = kol.AddComponent<BoxCollider>();
        float yb = klad.promien * .75f, yt = m.siodlo[1] + .05f;
        box.center = new Vector3(0f, (yb + yt) * .5f, 0f) * K;
        box.size = new Vector3(m.szerokosc * .62f, yt - yb, m.dlugosc * .92f) * K;
        klad.korpus = box;
        var o = Osiagi(model);
        klad.masa = o.masa; klad.vMaxKmh = o.vMax; klad.przyspieszenie = o.przysp; klad.naprzod4x4 = o.napedAll; klad.skok = o.skok;
        var rb = root.AddComponent<Rigidbody>(); rb.mass = o.masa; rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate;
        Postawione++;
        Debug.Log("MISTRZOWIE_KLAD " + napis + " " + m.wzor + " tri=" + tri + " wheelbase=" + m.rozstaw_osi.ToString("F2") + "m R=" + klad.promien.ToString("F3") + "m at " + p);
        return root;
    }

    /// <summary>A rival of the race (owner 24.11 12:57: "wyścig z rywalami"): one of the owner's quads in its paint with its
    /// rider on it (BraxChampionsKladZawodnik) and the AI's hands (BraxChampionsKladRywal), parked at p; never the player's.</summary>
    public static BraxChampionsKladRywal PostawRywala(Transform parent, string model, string kolor, string imie, int numer, Vector3 p, float yaw,
                                                     Color stroj, Color akcent, float umiejetnosc, float pas, AudioClip wolne = null, AudioClip jazda = null)
    {
        var root = Postaw(parent, model, kolor, "RYWAL · " + imie + " #" + numer, p, yaw, null, wolne, jazda);
        if (!root) return null;
        root.GetComponent<BraxChampionsKlad>().tylkoRywal = true;
        BraxChampionsKladZawodnik.Zbuduj(root.transform, model, stroj, akcent, Folder);
        var r = root.AddComponent<BraxChampionsKladRywal>();
        r.imie = imie; r.numer = numer; r.umiejetnosc = umiejetnosc; r.pas = pas;
        var f = Flota.FirstOrDefault(x => x.kolor == kolor);                // the paint's name as the fleet's labels say it
        r.kolor = f.napis != null ? f.napis.Substring(f.napis.LastIndexOf('·') + 1).Trim() : kolor.ToUpperInvariant();
        r.parking = p; r.parkingObrot = Quaternion.Euler(0f, yaw, 0f);
        var m = Kat()?.modele?.FirstOrDefault(x => x.model == model);
        if (m != null && m.rozstaw_osi > .5f) r.rozstawOsi = m.rozstaw_osi;
        return r;
    }
}
