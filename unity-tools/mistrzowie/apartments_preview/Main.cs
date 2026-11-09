using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class BraxClaudeOsiedle { public const float K = 1.2944f; }
public static class BraxBuildQuality { public static void Warn(string m) => Console.WriteLine("WARN " + m); }
public sealed class BraxWidocznoscWnetrza : MonoBehaviour { public Bounds[] strefy = new Bounds[0]; public string nazwa = ""; }
public static class BraxChampionsFoundation
{
    public static Material SourceMaterial(string id, string slug, float metres, Color tint) => new Material { name = id + "|" + slug };
    public static Material LibraryMarble(string id, Color tint, float metres) => new Material { name = id + "|Marble016" };
}
public static class BraxMiastoAssety
{
    public static GameObject Model(string paczka, string plik) => new GameObject(plik);
    public static GameObject Postaw(Transform parent, GameObject src, string name, Vector3 p, float yaw, float s = 1f, bool st = true, bool sh = true)
    { var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.position = p; Rec.Palms.Add(p); return g; }
    public static void LodyZWezlow(GameObject o, float[] progi) { }
}
public static class Rec
{
    public struct Q { public string g, m; public bool inside; public Vector3 a, b, c, d; }
    public static readonly List<Q> Quads = new List<Q>();
    public static readonly List<string> Cols = new List<string>();
    public static readonly List<string> Texts = new List<string>();
    public static readonly List<Vector3> Palms = new List<Vector3>();
    public static void Clear() { Quads.Clear(); Cols.Clear(); Texts.Clear(); Palms.Clear(); }
}
public static class BraxWnetrzaKit
{
    public static Transform Grupa; public static bool Wnetrze = true; public static float Otoczenie = 1f, Komorka = 14f;
    public static void Start(Transform g) { Grupa = g; Wnetrze = true; Komorka = 14f; }
    public static void Wyczysc() { }
    public static void Quad(Material m, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        if (!m) throw new Exception("null material at " + a.x + "," + a.y + "," + a.z);
        Rec.Quads.Add(new Rec.Q { g = Grupa.gameObject.name, m = m.name, inside = Wnetrze, a = a, b = b, c = c, d = d });
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
    public static void Rura(Material m, Vector3 a, Vector3 b, float r, int boki = 10, bool z = true)
    {
        var d = b - a; var side = Vector3.Cross(d, Vector3.up); side = side.magnitude < 1e-5f ? new Vector3(r, 0, 0) : side.normalized * r;
        var up = new Vector3(0, r, 0); Quad(m, a - side + up, b - side + up, b + side + up, a + side + up);
    }
    public static void KolizjaMM(float x0, float x1, float y0, float y1, float z0, float z1) => Rec.Cols.Add("mm");
    public static void Rampa(Vector3 gora, Vector3 dol, float szer) => Rec.Cols.Add("ramp " + gora.y.ToString("F2") + "->" + dol.y.ToString("F2"));
    public static Light Lampa(string n, Vector3 at, float moc, float zasieg, Color? k = null)
    { var g = new GameObject(n); g.transform.SetParent(Grupa, false); g.transform.position = at; var l = g.AddComponent<Light>(); l.range = zasieg; l.intensity = moc; return l; }
    public static void Napis(string t, Vector3 at, Vector3 n, float h, Color c) => Rec.Texts.Add(t + "@" + at.x.ToString("F2") + "," + at.y.ToString("F2") + "," + at.z.ToString("F2"));
    public static int Zapisz(string label) => Rec.Quads.Select(q => q.g + "|" + q.m + "|" + q.inside).Distinct().Count();
    public static class Mat { public static Material Plain(string n, Color c, float s = .7f, float m = 0f) => new Material { name = "plain:" + n }; }
}

public static class Program
{
    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    public static void Main(string[] args)
    {
        var ext = new Dictionary<string, Material>();
        foreach (var n in new[] { "arena_black", "light", "steel", "clear_glass", "facade_glass", "arena_gold" }) ext[n] = new Material { name = n };
        foreach (var id in args.Length > 0 ? args : BraxChampionsApartments.Ids)
        {
            Rec.Clear();
            BraxChampionsApartments.Generate(id, ext, "out");
            var sb = new StringBuilder();
            sb.Append("{\"id\":\"" + id + "\",\"colliders\":" + Rec.Cols.Count + ",\"texts\":[" + string.Join(",", Rec.Texts.Select(t => "\"" + t + "\"")) + "],\"quads\":[");
            sb.Append(string.Join(",", Rec.Quads.Select(q => "[\"" + q.g + "\",\"" + q.m + "\"," + (q.inside ? 1 : 0) + "," +
                string.Join(",", new[] { q.a, q.b, q.c, q.d }.Select(v => F(v.x) + "," + F(v.y) + "," + F(v.z))) + "]")));
            sb.Append("]}");
            File.WriteAllText(id + "_geometry.json", sb.ToString());
            Console.WriteLine("DUMP " + id + " quads=" + Rec.Quads.Count + " colliders=" + Rec.Cols.Count + " texts=" + Rec.Texts.Count + " palms=" + Rec.Palms.Count);
        }
    }
}
