using System.IO;
using UnityEditor;
using UnityEngine;

// GŁOŚNIKI W SALACH Z MUZYKĄ (właściciel 1.11, 18:06: „w siłowni by trzeba dodać głośniki, nie tylko żeby muzyka leciała —
// w narożnikach, wisiały u góry”). Czarna skrzynka 0.34 × 0.54 × 0.30 m z siatką, dwiema membranami i logo-paskiem,
// na pręcie do sufitu, w górnych narożnikach sali (3.6 m nad podłogą, pod sufitem), lekko pochylona w dół, przodem do środka.
// Pozycje trafiają do BraxClaudeMuzyka.glosniki (głośniej przy głośniku). Bez kolizji (pod sufitem), bez świecenia.
// Samodzielne (prymitywy Unity + własne materiały) — używane przez osiedle (BraxClaudeOsiedle.MusicZones) i miasto (BraxGymBuild).
public static class BraxClaudeGlosniki
{
    const string Dir = "Assets/PlayableGenerated/ClaudeGlosniki";
    static Material korpus, siatka, membrana, metal;

    static Material Mat(ref Material m, string nazwa, Color c, float gloss, float metalicznosc)
    {
        if (m) return m;
        string path = Dir + "/" + nazwa + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) return m;
        if (!AssetDatabase.IsValidFolder(Dir)) { Directory.CreateDirectory(Dir); AssetDatabase.ImportAsset(Dir); }
        m = new Material(Shader.Find("Standard")) { name = "Głośnik: " + nazwa, color = c };
        m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", metalicznosc);
        BraxZapisAssetu.Utworz(m, path);
        return m;
    }

    static void Czesc(PrimitiveType typ, Transform rodzic, Vector3 poz, Vector3 skala, Quaternion obrot, Material m, string nazwa)
    {
        var g = GameObject.CreatePrimitive(typ); g.name = nazwa;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(rodzic, false); g.transform.localPosition = poz; g.transform.localRotation = obrot; g.transform.localScale = skala;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // pod sufitem — cień niewidoczny, a kosztuje
    }

    // Głośnik zawieszony na pręcie do wysokości `sufit`, przodem (poziomo) w stronę `cel`, pochylony 14° w dół.
    public static GameObject Glosnik(Transform rodzic, Vector3 poz, Vector3 cel, float sufit)
    {
        Mat(ref korpus, "korpus", new Color(.045f, .047f, .05f), .35f, 0f);
        Mat(ref siatka, "siatka", new Color(.10f, .105f, .11f), .15f, .3f);
        Mat(ref membrana, "membrana", new Color(.02f, .02f, .022f), .55f, 0f);
        Mat(ref metal, "uchwyt", new Color(.30f, .31f, .33f), .6f, .8f);
        var root = new GameObject("głośnik (muzyka)").transform; root.SetParent(rodzic, true);
        Vector3 k = cel - poz; k.y = 0f; if (k.sqrMagnitude < 1e-4f) k = Vector3.forward;
        root.position = poz; root.rotation = Quaternion.LookRotation(k.normalized, Vector3.up);
        var obud = new GameObject("obudowa").transform; obud.SetParent(root, false); obud.localRotation = Quaternion.Euler(14f, 0f, 0f);
        Czesc(PrimitiveType.Cube, obud, Vector3.zero, new Vector3(.34f, .54f, .30f), Quaternion.identity, korpus, "skrzynka");
        Czesc(PrimitiveType.Cube, obud, new Vector3(0f, 0f, .152f), new Vector3(.31f, .51f, .008f), Quaternion.identity, siatka, "siatka");
        var naPrzod = Quaternion.Euler(90f, 0f, 0f);   // walec Unity stoi w osi Y — obrót, żeby patrzył do przodu (Z)
        Czesc(PrimitiveType.Cylinder, obud, new Vector3(0f, -.085f, .158f), new Vector3(.21f, .004f, .21f), naPrzod, membrana, "membrana nisko");
        Czesc(PrimitiveType.Cylinder, obud, new Vector3(0f, .15f, .158f), new Vector3(.08f, .004f, .08f), naPrzod, membrana, "membrana wysoko");
        Czesc(PrimitiveType.Cube, obud, new Vector3(0f, .245f, .158f), new Vector3(.12f, .012f, .004f), Quaternion.identity, metal, "pasek z logo");
        // uchwyt: jarzmo nad skrzynką i pręt do sufitu
        Czesc(PrimitiveType.Cube, root, new Vector3(0f, .30f, 0f), new Vector3(.22f, .03f, .06f), Quaternion.identity, metal, "jarzmo");
        float pret = Mathf.Clamp(sufit - (poz.y + .31f), .15f, 2.4f);
        Czesc(PrimitiveType.Cylinder, root, new Vector3(0f, .31f + pret * .5f, 0f), new Vector3(.035f, pret * .5f, .035f), Quaternion.identity, metal, "pręt do sufitu");
        return root.gameObject;
    }

    // Cztery głośniki w górnych narożnikach sali (wcięte od ścian), przodem do środka. Zwraca ich pozycje dla BraxClaudeMuzyka.
    // `sufit` — wysokość stropu (domyślnie górna krawędź prostopadłościanu sali); głośnik 3.6 m nad podłogą, ale nie wyżej niż 0.7 m pod stropem.
    public static Vector3[] WNarozach(Transform rodzic, Bounds sala, float wciecie = .6f, float sufit = float.NaN)
    {
        float top = float.IsNaN(sufit) ? sala.max.y : sufit;
        float y = Mathf.Min(top - .7f, sala.min.y + 3.6f);
        Vector3 a = sala.min, b = sala.max, c = new Vector3(sala.center.x, y, sala.center.z);
        var poz = new[] { new Vector3(a.x + wciecie, y, a.z + wciecie), new Vector3(b.x - wciecie, y, a.z + wciecie),
                          new Vector3(a.x + wciecie, y, b.z - wciecie), new Vector3(b.x - wciecie, y, b.z - wciecie) };
        foreach (var p in poz) Glosnik(rodzic, p, c, top);
        return poz;
    }
}
