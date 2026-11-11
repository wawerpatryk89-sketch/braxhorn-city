// Zamienniki klas projektu (tylko to, czego używa generator dworca R1). Geometria — prawdziwe UlSiatka (wycinane z BraxUliceKit.cs),
// BraxLiteryBebas (prawdziwe pliki), BraxMiastoGranice (prawdziwy plik). Reszta zapisuje to, co trzeba do podglądu i kontroli.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public interface IBraxInterakcja { }
public sealed class BraxSeat : MonoBehaviour { public bool lie; public float surfaceY; public string label; }
public sealed class BraxWinda : MonoBehaviour
{
    public string nazwa; public Transform kabina, kabinaL, kabinaP; public float[] pietraY; public string[] pietraNazwy; public bool[] uzytkowe;
    public Transform[] przystanekL, przystanekP; public Vector3[] wezwanie; public Vector2 szyb, wnetrze; public float przesuw; public int start;
    public Renderer[] przyciski; public Material przyciskAktywny, przyciskWybrany, przyciskNieaktywny;
}
public sealed class BraxCityDoor : MonoBehaviour { public string id, label; public Transform[] leaves; public float[] sides; }
public sealed class BraxCityWorld : MonoBehaviour { public BraxCityDoor[] doors = new BraxCityDoor[0]; }
public sealed class BraxZegarDworca : MonoBehaviour { public Transform godzinowa, minutowa, sekundnik; }
public sealed class BraxDrzwiPrzesuwne : MonoBehaviour { public string etykieta; public Transform[] skrzydla; public Vector3[] przesuniecie; public Collider[] blokady; public float promien; }

public static class BraxClaudeOsiedle { public const float WzrostBraxa = 2.33f; public static float K => WzrostBraxa / 1.80f; }

public static class BraxRegionFawela
{
    public static readonly bool Wlaczona = true;
    public const float Odsuniecie = 115f, PlanSzczyt = -357f;
    public static float PlanKrawedz(float x) => x > -61.4f ? -68f : BraxMiastoGranice.Z0;
    public static float KrawedzSwiat(float x) => PlanKrawedz(x) - Odsuniecie;
    public static float WysTerenu(float x, float z) => .15f;      // teren faweli poza zakresem harnessu
    public static float WysNaKrawedzi(float x) => .17f;
}

public static class Rejestr
{
    public sealed class Model { public string id, nazwa; public Vector3 floor; public float yaw, scale, sink, maxH; }
    public sealed class Swiatlo { public string nazwa; public Vector3 p; public float i, r; }
    public static readonly List<Model> Modele = new List<Model>();
    public static readonly List<Swiatlo> Swiatla = new List<Swiatlo>();
    public static readonly List<string> Notatki = new List<string>();
}

public static class BraxRegion
{
    public const string Out = "Assets/Claude/Region";
    public static void Note(string t) => Rejestr.Notatki.Add(t);
    public static Transform Root(Transform parent, string name) { var g = new GameObject(name); g.transform.SetParent(parent, false); return g.transform; }
    static readonly Dictionary<string, GameObject> modele = new Dictionary<string, GameObject>();
    public static GameObject Model(params string[] ids)
    {
        foreach (var id in ids)
        {
            if (modele.TryGetValue(id, out var g)) return g;
            if (Harness.MaModel(id)) { g = new GameObject(id); g.SetActive(false); modele[id] = g; return g; }
        }
        return null;
    }
    public static GameObject Put(Transform parent, GameObject source, string label, Vector3 floor, float yaw, float scale = 1f, bool solid = true, float maxHeight = 0f, float sink = 0f, bool skalaK = true)
    {
        if (!source) return null;
        float kk = BraxClaudeOsiedle.K;
        if (skalaK && Mathf.Abs(scale - kk) > .002f) { scale *= kk; if (maxHeight > 0f) maxHeight *= kk; }
        Rejestr.Modele.Add(new Rejestr.Model { id = source.name, nazwa = label, floor = floor, yaw = yaw, scale = scale, sink = sink, maxH = maxHeight });
        var o = new GameObject(label); o.transform.SetParent(parent, false); o.transform.position = floor; return o;
    }
    public static Light Warm(Transform parent, string name, Vector3 at, float intensity, float range)
    {
        Rejestr.Swiatla.Add(new Rejestr.Swiatlo { nazwa = name, p = at, i = intensity, r = range });
        var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(parent, false); l.transform.position = at; l.intensity = intensity; l.range = range; return l;
    }
}

public static class BraxRegionMeble
{
    public static BoxCollider Kolizja(Transform parent, string name, Vector3 c, Vector3 size, Quaternion rot)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = c; g.transform.rotation = rot;
        var b = g.AddComponent<BoxCollider>(); b.size = size; return b;
    }
    public static BoxCollider Kolizja(Transform parent, string name, Vector3 c, Vector3 size) => Kolizja(parent, name, c, size, Quaternion.identity);
}

internal static partial class BraxUliceKit
{
    public const string Out = BraxRegion.Out + "/Ulice";
    public static float K => BraxClaudeOsiedle.K;
    static readonly Dictionary<(string, Material), UlSiatka> partie = new Dictionary<(string, Material), UlSiatka>();
    static readonly Dictionary<string, float> zasiegGrup = new Dictionary<string, float>();
    public static string Grupa = "ulice";
    public static void Start() { partie.Clear(); zasiegGrup.Clear(); Grupa = "ulice"; }
    public static void UstawGrupe(string nazwa, float zasieg = 0f) { Grupa = nazwa; if (zasieg > 0f) zasiegGrup[nazwa] = zasieg; }
    public static UlSiatka S(Material m)
    {
        if (!m) m = Mat("zamiennik: szary", new Color(.5f, .5f, .5f), .8f);
        if (!partie.TryGetValue((Grupa, m), out var s)) partie[(Grupa, m)] = s = new UlSiatka();
        return s;
    }
    public static int Zapisz(Transform rodzic, string prefiks)
    {
        int n = 0; var grupy = new Dictionary<string, Transform>();
        foreach (var kv in partie)
        {
            var s = kv.Value; if (s.Wierzcholki == 0) continue;
            if (!grupy.TryGetValue(kv.Key.Item1, out var g)) { g = new GameObject(kv.Key.Item1).transform; g.SetParent(rodzic, false); grupy[kv.Key.Item1] = g; }
            var mesh = s.Zbuduj(prefiks + " / " + kv.Key.Item1 + " / " + kv.Key.Item2.name);
            var go = new GameObject(kv.Key.Item2.name); go.transform.SetParent(g, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = kv.Key.Item2; n++;
        }
        partie.Clear(); zasiegGrup.Clear(); return n;
    }
    public static string Bezpieczna(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    public static Material Mat(string nazwa, Color kolor, float szorstkosc = .85f, float metal = 0f)
    {
        if (mats.TryGetValue(nazwa, out var m)) return m;
        m = new Material(Shader.Find("kolor")) { name = nazwa }; m.color = kolor; m.SetFloat("_Roughness", szorstkosc); m.SetFloat("_Metallic", metal); m.rodzaj = "kolor";
        if (kolor.a < .99f) m.renderQueue = 3000;
        return mats[nazwa] = m;
    }
    public static Material Tex(string nazwa, float metry, Color? odcien, Color zamiennik, float szorstkosc, params string[] slugi)
    {
        if (mats.TryGetValue(nazwa, out var m)) return m;
        string slug = slugi.FirstOrDefault(Harness.MaTeksture);
        if (slug == null) return Mat(nazwa + " (bez tekstury)", zamiennik, szorstkosc);
        m = new Material(Shader.Find("pbr")) { name = nazwa + " [" + slug + "]" }; m.metry = metry; m.slugi = new[] { slug }; m.rodzaj = "pbr";
        if (odcien.HasValue) m.color = odcien.Value; m.SetFloat("_Roughness", szorstkosc);
        return mats[nazwa] = m;
    }
    public static Material SzkloPrzezroczyste => Mat("szkło witryn (przezroczyste)", new Color(.62f, .70f, .72f, .22f), .06f, .1f);
    public static void NapisWTeksturze(Color[] px, int W, int H, string tekst, float cx, float by, float cap, Color kolor, float s = .15f, float track = .14f) { }
}

internal static class BraxMiastoA
{
    public const string Out = "Assets/Claude/Region/A";
    public static GameObject Kol(Transform rodzic, string nazwa, Vector3 a, Vector3 b)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b);
        var g = new GameObject("kolizja: " + nazwa); g.transform.SetParent(rodzic, false); g.transform.position = (lo + hi) * .5f;
        g.AddComponent<BoxCollider>().size = hi - lo; return g;
    }
    public static Material Swieci(string nazwa, Color baza, Color emisja)
    {
        var m = BraxUliceKit.Mat("A świeci: " + nazwa, baza, .35f, 0f); m.SetColor("_EmissionColor", emisja); m.rodzaj = "swieci"; return m;
    }
    public static GameObject Obiekt(Transform rodzic, string nazwa, UlSiatka s, Material m, bool cien = false)
    {
        var mesh = s.Zbuduj("A: " + nazwa);
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = cien ? ShadowCastingMode.On : ShadowCastingMode.Off; return g;
    }
    public static GameObject Obiekt(Transform rodzic, string nazwa, Mesh mesh, Material m)
    {
        var g = new GameObject(nazwa); g.transform.SetParent(rodzic, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = m; return g;
    }
    public static BraxSeat Siedzisko(Transform rodzic, string etykieta, Vector3 punkt, Vector3 przodem, bool lezy = false)
    {
        var g = new GameObject("Siedzisko: " + etykieta); g.transform.SetParent(rodzic, false);
        g.transform.position = punkt; przodem.y = 0f; g.transform.rotation = Quaternion.LookRotation(przodem.normalized, Vector3.up);
        var s = g.AddComponent<BraxSeat>(); s.lie = lezy; s.surfaceY = punkt.y; s.label = etykieta; return s;
    }
    // prawdziwa geometria drzwi automatycznych (kopia BraxMiastoA.DrzwiPrzesuwne)
    public static BraxDrzwiPrzesuwne DrzwiPrzesuwne(Transform rodzic, string etykieta, Vector3 c, Vector3 R, float w, float h, Material szklo, Material rama)
    {
        var g = new GameObject("Drzwi automatyczne: " + etykieta); g.transform.SetParent(rodzic, false); g.transform.position = c;
        g.transform.rotation = Quaternion.LookRotation(Vector3.Cross(R, Vector3.up), Vector3.up);
        var skrzydla = new List<Transform>(); var przes = new List<Vector3>(); var blok = new List<Collider>();
        foreach (float sg in new[] { -1f, 1f })
        {
            var s = new UlSiatka(); var rm = new UlSiatka(); float sw = w * .5f;
            rm.Box(new Vector3(0, 0, -.03f), new Vector3(.05f, h, .03f)); rm.Box(new Vector3(sw - .05f, 0, -.03f), new Vector3(sw, h, .03f));
            rm.Box(new Vector3(.05f, 0, -.03f), new Vector3(sw - .05f, .1f, .03f)); rm.Box(new Vector3(.05f, h - .08f, -.03f), new Vector3(sw - .05f, h, .03f));
            s.Box(new Vector3(.05f, .1f, -.006f), new Vector3(sw - .05f, h - .08f, .006f));
            var sk = new GameObject("Skrzydło " + (sg < 0 ? "lewe" : "prawe")).transform; sk.SetParent(g.transform, false);
            sk.localPosition = new Vector3(sg < 0 ? -sw : 0f, 0f, 0f);
            Obiekt(sk, "Szyba skrzydła " + etykieta, s, szklo); Obiekt(sk, "Rama skrzydła " + etykieta, rm, rama);
            var bc = sk.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(sw * .5f, h * .5f, 0f); bc.size = new Vector3(sw, h, .1f);
            skrzydla.Add(sk); przes.Add(new Vector3(sg * (sw - .06f), 0f, 0f)); blok.Add(bc);
        }
        var d = g.AddComponent<BraxDrzwiPrzesuwne>(); d.etykieta = etykieta; d.skrzydla = skrzydla.ToArray(); d.przesuniecie = przes.ToArray(); d.blokady = blok.ToArray(); d.promien = 3.2f;
        return d;
    }
}
public sealed class BraxLustro : MonoBehaviour { public float zasieg = 16f; public int rozmiar = 512; }
public static class BraxZapisAssetu { public static void Utworz(UnityEngine.Object o, string p) { } }
public static class BraxCityMaterials { public static Material Surface(string n, Color c, float r, float m = 0f) => BraxUliceKit.Mat(n, c, r, m); }
public sealed class BraxKran : MonoBehaviour { public Renderer strumien; public float promien; }
public static class BraxSklepyTowar
{
    public static bool Ma(string typ) => false;
    public static void Polka(string typ, Transform parent, Vector3 start, Vector3 wzdl, Vector3 wglab, float dl, System.Random rng, float maxWys = 99f) { }
}
