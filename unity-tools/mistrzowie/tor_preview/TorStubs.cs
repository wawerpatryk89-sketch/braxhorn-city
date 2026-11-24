using UnityEngine;

// the quad track preview: the quads (an encrypted owner pack) and the runtime lap timer stand in
public class AudioClip : Object { }
public static class BraxCityMaterials { public const string ShaderName = "Braxhorn/CityStaticPBR"; }
public sealed class BraxChampionsTorCzas : MonoBehaviour { public Vector3[] punkty = new Vector3[0]; public float polSzerokosci; }
public sealed class BraxChampionsKladRywal : MonoBehaviour { public string imie, kolor; public int numer; public float umiejetnosc, pas, rozstawOsi; public Vector3 parking; public Quaternion parkingObrot; }
public sealed class BraxChampionsKlad : MonoBehaviour { }
public sealed class BraxChampionsObrotnica : MonoBehaviour { public BraxChampionsKlad klad; public Vector3 srodek; public float promien, stopniNaSekunde; }
public sealed class BraxChampionsBloto : MonoBehaviour { public Vector3[] srodek, wzdluz; public float[] polDl, polSzer; public Material pyl, grudki; public float K; }
public sealed class BraxChampionsWyscig : MonoBehaviour
{
    public Vector3[] punkty, lewo, sloty; public float[] vCel, szer; public bool[] skok; public float krok, dlugosc, sMety, polSzerokosci, K, strefaPromien; public int okrazen;
    public Quaternion kierunekStartu, belkaGora, belkaDol; public Transform[] belki; public BraxChampionsKladRywal[] rywale; public Renderer tablica; public Vector3 strefaStartu;
}
public sealed class BraxChampionsTablicaTop : MonoBehaviour { public Renderer ekran; public float naStrone; }
public static class BraxChampionsKlady
{
    public static readonly (string model, string kolor, string napis)[] Flota =
    {
        ("rancher", "czerwony", "KŁAD · CZERWONY"), ("rancher", "niebieski", "KŁAD · NIEBIESKI"), ("rancher", "bialy", "KŁAD · BIAŁY"),
        ("raptor", "limonkowy", "KŁAD · LIMONKOWY"), ("raptor", "pomaranczowy", "KŁAD · POMARAŃCZOWY"), ("raptor", "fioletowy", "KŁAD · FIOLETOWY"),
    };
    public static System.Collections.Generic.List<(string napis, Vector3 p, float yaw)> Miejsca = new System.Collections.Generic.List<(string, Vector3, float)>();
    // with a decrypted copy of the pack beside the run (Assets/Source/Prywatne/Miasto/pojazdy_klady_01): each part's .glb at its
    // pivot, the paint's colour map swapped in (a .png copy of it, PREVIEW_PICTURES = the run's folder)
    // a rival: the quad as above, its rider (the real builder, BraxChampionsKladZawodnik) on it
    public static BraxChampionsKladRywal PostawRywala(Transform parent, string model, string kolor, string imie, int numer, Vector3 p, float yaw,
                                                     Color stroj, Color akcent, float umiejetnosc, float pas, AudioClip wolne = null, AudioClip jazda = null)
    {
        var root = Postaw(parent, model, kolor, "RYWAL · " + imie, p, yaw);
        if (!root) { root = new GameObject("RYWAL · " + imie); root.transform.SetParent(parent, false); root.transform.position = p; root.transform.rotation = Quaternion.Euler(0f, yaw, 0f); }
        BraxChampionsKladZawodnik.Zbuduj(root.transform, model, stroj, akcent, "preview");
        var r = root.AddComponent<BraxChampionsKladRywal>(); r.imie = imie; r.numer = numer; r.parking = p;
        return r;
    }
    public static GameObject Postaw(Transform parent, string model, string kolor, string napis, Vector3 p, float yaw, BraxChampionsTorCzas tor = null, AudioClip wolne = null, AudioClip jazda = null)
    {
        Miejsca.Add((napis, p, yaw));
        string baza = BraxMiastoAssety.Baza + "pojazdy_klady_01/", kat = baza + "katalog.json";
        if (!System.IO.File.Exists(kat)) return null;
        var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(kat));
        foreach (var m in doc.RootElement.GetProperty("modele").EnumerateArray())
        {
            if (m.GetProperty("model").GetString() != model) continue;
            string tex = null;
            foreach (var w in m.GetProperty("warianty").EnumerateArray()) if (w.GetProperty("kolor").GetString() == kolor) tex = baza + "_tekstury/" + w.GetProperty("tekstura").GetString() + ".png";
            var root = new GameObject("KŁAD | " + napis); root.transform.SetParent(parent, false); root.transform.position = p; root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            float K = BraxClaudeOsiedle.K;
            foreach (var c in m.GetProperty("czesci").EnumerateArray())
            {
                var pv = c.GetProperty("pivot"); var piv = new Vector3(pv[0].GetSingle(), pv[1].GetSingle(), pv[2].GetSingle());
                string glb = baza + c.GetProperty("plik").GetString() + ".glb";
                var g = new GameObject(c.GetProperty("plik").GetString()); g.transform.SetParent(root.transform, false);
                g.transform.localPosition = piv * K; g.transform.localScale = Vector3.one * K;
                var pm = g.AddComponent<PreviewModel>(); pm.path = glb; (pm.min, pm.max) = GltfBounds.Read(glb);
                g.AddComponent<MeshRenderer>().sharedMaterial = new Material { name = "klad " + kolor, textured = true, picture = tex };
            }
            return root;
        }
        return null;
    }
}

// the classics' bunker's vehicles (an encrypted owner pack): with a decrypted copy beside the run
// (Assets/Source/Prywatne/Miasto/pojazdy_klasyki_01) each part's .glb at its pivot, its colour map (a .jpg)
public static class BraxChampionsKlasyki
{
    public const string Paczka = "pojazdy_klasyki_01";
    public static System.Collections.Generic.List<(string model, Vector3 p, float yaw)> Miejsca = new System.Collections.Generic.List<(string, Vector3, float)>();
    static string Baza => BraxMiastoAssety.Baza + Paczka + "/";
    public static bool Jest => System.IO.File.Exists(Baza + "katalog.json");
    static System.Text.Json.JsonElement? M(string model)
    {
        if (!Jest) return null;
        var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(Baza + "katalog.json"));
        foreach (var m in doc.RootElement.GetProperty("modele").EnumerateArray()) if (m.GetProperty("model").GetString() == model) return m.Clone();
        return null;
    }
    public static Vector2 Wymiar(string model) { var m = M(model); return m == null ? Vector2.zero : new Vector2(m.Value.GetProperty("dlugosc").GetSingle(), m.Value.GetProperty("szerokosc").GetSingle()); }
    public static string Typ(string model) => M(model)?.GetProperty("typ").GetString() ?? "";
    public static GameObject Postaw(Transform parent, string model, Vector3 p, float yaw, bool jezdny = true)
    {
        Miejsca.Add((model, p, yaw));
        var m = M(model); if (m == null) return null;
        float K = BraxClaudeOsiedle.K;
        var root = new GameObject("KLASYK | " + model); root.transform.SetParent(parent, false); root.transform.position = p * K; root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        string tex = Baza + "_tekstury/" + m.Value.GetProperty("kolor").GetString() + ".jpg";
        foreach (var c in m.Value.GetProperty("czesci").EnumerateArray())
        {
            var pv = c.GetProperty("pivot"); var piv = new Vector3(pv[0].GetSingle(), pv[1].GetSingle(), pv[2].GetSingle());
            string plik = c.GetProperty("plik").GetString(), glb = Baza + plik + ".glb";
            var g = new GameObject(plik); g.transform.SetParent(root.transform, false);
            g.transform.localPosition = piv * K; g.transform.localScale = Vector3.one * K;
            var pm = g.AddComponent<PreviewModel>(); pm.path = glb; (pm.min, pm.max) = GltfBounds.Read(glb);
            bool szklo = plik.Contains("szyby");
            g.AddComponent<MeshRenderer>().sharedMaterial = szklo ? new Material { name = "klasyk szyba", color = new Color(.05f, .06f, .07f) } : new Material { name = "klasyk " + model, textured = true, picture = tex };
        }
        return root;
    }
}
