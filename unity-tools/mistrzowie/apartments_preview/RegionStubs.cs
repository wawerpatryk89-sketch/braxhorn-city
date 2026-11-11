// Region stand-ins shared by the apartment and hotel previews (W kit recorder, foundation, quality log).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BraxClaudeOsiedle { public const float K = 2.33f / 1.80f; }
public static class BraxBuildQuality { public static void Warn(string m) => Console.WriteLine("WARN " + m); public static void Issue(Exception e) => Console.WriteLine("WARN ISSUE " + e.Message); }
public sealed class BraxSeat : MonoBehaviour { public float surfaceY; public string label; public bool lie; }
public sealed class BraxWidocznoscWnetrza : MonoBehaviour { public Bounds[] strefy = new Bounds[0]; public string nazwa = ""; }
public static partial class BraxChampionsFoundation
{
    public static Material SourceMaterial(string id, string slug, float metres, Color tint) => new Material { name = id + "|" + slug + "|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true };
    public static Material LibraryMarble(string id, Color tint, float metres) => new Material { name = id + "|Marble016|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true };
    public static Material LibraryAcg(string id, string asset, Color tint, float metres) => new Material { name = id + "|" + asset + "|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true };
    public static Material LibraryMarbleWhite(string id, Color tint, float metres) => new Material { name = id + "|Marble021|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true };
    public static Material LibraryTravertine(string id, Color tint, float metres) => new Material { name = id + "|Travertine009|" + metres.ToString(System.Globalization.CultureInfo.InvariantCulture), color = tint, textured = true };
    public static Material LibraryLeather(string id, Color tint) => new Material { name = id + "|Leather037|0.6", color = tint, textured = true };
    public static Material LibraryMetal(string id, Color tint) => new Material { name = id + "|Metal048A|0.65", color = tint, textured = true };
    public static void Optional(string what, Action build) { try { build(); } catch (Exception e) { Console.WriteLine("WARN OPTIONAL_FAILED " + what + ": " + e.Message); } }
    [Serializable] public sealed class MeshData { public string id, role; public float[] positions, uv; public int[] triangles; }
}
// shared by every preview (apartments, casino, arena, promenade): the leaves keep their hinge transforms
public sealed class BraxCityDoor : MonoBehaviour { public string id, label; public Transform[] leaves; public float[] sides; public float openAngle, range, speed; }
public sealed class BraxGymPlanarMirror : MonoBehaviour { public object sourceCamera; public Renderer mirrorRenderer; public int textureWidth, textureHeight, mirrorLayer; public float refreshRate, maximumViewDistance, reflectionDistance; }
public sealed class BraxChampionsMirrorCamera : MonoBehaviour { public const int MirrorLayer = 9; }
public static class BraxZapisAssetu { public static void Utworz(UnityEngine.Object o, string path) { } }
public static class BraxMiastoAssety
{
    public static bool Jest(string paczka) => false;   // the owner's packs are encrypted: never in the preview
    public static GameObject Model(string paczka, string plik) => new GameObject(plik);
    public static GameObject Postaw(Transform parent, GameObject src, string name, Vector3 p, float yaw, float s = 1f, bool st = true, bool sh = true)
    { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = p; Rec.Palms.Add(p); Rec.PalmObjs.Add(g.transform); return g; }
    public static void LodyZWezlow(GameObject o, float[] progi) { }
    public static Bounds Granice(GameObject o) => new Bounds(o.transform.position, Vector3.zero);
    public static void Przygotuj(GameObject o, bool statyczny = true, bool cienie = true) { }
    public static void Kolizja(Transform parent, GameObject o, string name, float margin) { }
}
public static class Rec
{
    public struct Q { public string g, m; public bool inside; public Vector3 a, b, c, d, la, lb, lc, ld; public Transform t; public bool hasUv; public Vector2 ua, ub, uc, ud; }
    public static readonly List<Q> Quads = new List<Q>();
    public static readonly Dictionary<string, Color> Colours = new Dictionary<string, Color>();
    public static readonly Dictionary<string, float[]> Ambient = new Dictionary<string, float[]>();
    public static readonly List<string> Cols = new List<string>();
    public static readonly List<string> Texts = new List<string>();
    public static readonly List<Vector3> Palms = new List<Vector3>();
    // the palm objects themselves: their world position follows the building root after the prefab is moved back to its plot
    public static readonly List<Transform> PalmObjs = new List<Transform>();
    public static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
    public static void Clear() { Quads.Clear(); Cols.Clear(); Texts.Clear(); Palms.Clear(); PalmObjs.Clear(); }
}
public static class BraxWnetrzaKit
{
    public static Transform Grupa; public static bool Wnetrze = true; public static float Otoczenie = 1f, Komorka = 14f;
    public static void Start(Transform g) { Grupa = g; Wnetrze = true; Komorka = 14f; }
    public static void Wyczysc() { }
    public static string Bezpieczna(string s) => new string(System.Array.ConvertAll(s.ToCharArray(), ch => char.IsLetterOrDigit(ch) ? ch : '_'));
    public static void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        if (!m) throw new Exception("null material at " + a.x + "," + a.y + "," + a.z);
        string path = Grupa.gameObject.name; for (var p = Grupa.parent; p != null; p = p.parent) path = p.gameObject.name + " >> " + path;
        Rec.Quads.Add(new Rec.Q { g = path, m = m.name, inside = Wnetrze, a = a, b = b, c = c, d = d, t = Grupa,
            la = Grupa.InverseTransformPoint(a), lb = Grupa.InverseTransformPoint(b), lc = Grupa.InverseTransformPoint(c), ld = Grupa.InverseTransformPoint(d) });
        Rec.Colours[m.name] = m.color; Rec.Mats[m.name] = m;
        Rec.Ambient[m.name] = new[] { m.sky.r, m.sky.g, m.sky.b, m.ground.r, m.ground.g, m.ground.b, m.ambient };
    }
    public static void QuadUV(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
    {
        Quad(m, a, b, c, d); var q = Rec.Quads[Rec.Quads.Count - 1]; q.hasUv = true; q.ua = ua; q.ub = ub; q.uc = uc; q.ud = ud; Rec.Quads[Rec.Quads.Count - 1] = q;
    }
    public static void MM(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool k = false)
        => OBox(m, new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(Math.Abs(x1 - x0), Math.Abs(y1 - y0), Math.Abs(z1 - z0)), Quaternion.identity, k);
    public static void OBox(Material m, Vector3 c, Vector3 s, Quaternion q, bool k = false, bool bezDna = false)
    {
        Vector3 h = s * .5f; Vector3 X = q * new Vector3(h.x, 0, 0), Y = q * new Vector3(0, h.y, 0), Z = q * new Vector3(0, 0, h.z);
        Vector3 P(int sx, int sy, int sz) => c + X * sx + Y * sy + Z * sz;
        Quad(m, P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)); Quad(m, P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1));
        Quad(m, P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1)); Quad(m, P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1));
        Quad(m, P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1)); if (!bezDna) Quad(m, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
        if (k) Rec.Cols.Add("box");
    }
    public static void Box(Material m, Vector3 c, Vector3 s, bool k = false) => OBox(m, c, s, Quaternion.identity, k);
    public static void Rura(Material m, Vector3 a, Vector3 b, float r, int boki = 10, bool z = true)
    {
        var os = b - a; if (os.magnitude < 1e-6f) return; os = os.normalized;
        var u = Vector3.Cross(os, Math.Abs(os.y) > .95f ? Vector3.forward : Vector3.up).normalized; var w = Vector3.Cross(os, u);
        for (int i = 0; i < boki; i++)
        {
            float t0 = 2 * Mathf.PI * i / boki, t1 = 2 * Mathf.PI * (i + 1) / boki;
            Vector3 n0 = u * Mathf.Cos(t0) + w * Mathf.Sin(t0), n1 = u * Mathf.Cos(t1) + w * Mathf.Sin(t1);
            Quad(m, a + n0 * r, a + n1 * r, b + n1 * r, b + n0 * r);
            if (z) { Quad(m, a, a + n1 * r, a + n0 * r, a + n0 * r); Quad(m, b, b + n0 * r, b + n1 * r, b + n1 * r); }
        }
    }
    public static void Walec(Material m, Vector3 p, float r, float h, int boki = 16, bool k = false)
    { Rura(m, p, p + Vector3.up * h, r, boki, true); if (k) Rec.Cols.Add("walec"); }
    public static void Kula(Material m, Vector3 c, float r, int pol = 16, int row = 10)
    {
        for (int i = 0; i < row; i++) for (int j = 0; j < pol; j++)
        {
            float a0 = Mathf.PI * i / row - Mathf.PI / 2, a1 = Mathf.PI * (i + 1) / row - Mathf.PI / 2, b0 = 2 * Mathf.PI * j / pol, b1 = 2 * Mathf.PI * (j + 1) / pol;
            Vector3 S(float la, float lo) => c + new Vector3(Mathf.Cos(la) * Mathf.Cos(lo), Mathf.Sin(la), Mathf.Cos(la) * Mathf.Sin(lo)) * r;
            Quad(m, S(a0, b0), S(a1, b0), S(a1, b1), S(a0, b1));
        }
    }
    public static void Siedzisko(Vector3 at, Vector3 forward, string label) => Rec.Cols.Add("seat");
    public static void Kolizja(Vector3 c, Vector3 size, Quaternion q) => Rec.Cols.Add("box");
    public static void KolizjaMM(float x0, float x1, float y0, float y1, float z0, float z1) => Rec.Cols.Add("mm");
    public static void Rampa(Vector3 gora, Vector3 dol, float szer) => Rec.Cols.Add("ramp " + gora.y.ToString("F2") + "->" + dol.y.ToString("F2"));
    public static Light Lampa(string n, Vector3 at, float moc, float zasieg, Color? k = null)
    { var g = new GameObject(n); g.transform.SetParent(Grupa, false); g.transform.position = at; var l = g.AddComponent<Light>(); l.range = zasieg; l.intensity = moc; return l; }
    public static void Napis(string t, Vector3 at, Vector3 n, float h, Color c) => Rec.Texts.Add(t + "@" + at.x.ToString("F2") + "," + at.y.ToString("F2") + "," + at.z.ToString("F2"));
    public static int Zapisz(string label) => Rec.Quads.Select(q => q.g + "|" + q.m + "|" + q.inside).Distinct().Count();
    public static class Mat { public static Material Plain(string n, Color c, float s = .7f, float m = 0f) => new Material { name = "plain:" + n }; }
}

