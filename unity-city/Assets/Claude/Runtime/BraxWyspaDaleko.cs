using UnityEngine;
using UnityEngine.SceneManagement;

// R1: daleka sylwetka wyspy zachowuje ten sam PBR co źródło.
// Nigdy nie znika na samej podstawie odległości: dopiero PO faktycznym
// wczytaniu i aktywacji właściwej sceny wyspy. Dzięki temu rejs nie pokazuje
// pustego morza, gdy Android wolniej doczytuje ciężkie assety.
// Nie nakładamy starej i nowej wyspy: proxy gaśnie w chwili gotowości sceny.
public sealed class BraxWyspaDaleko : MonoBehaviour
{
    public Vector3 srodek;
    public float blizej = 230f; // starsze sceny: parametr pozostaje kompatybilny
    public Renderer[] renderery = new Renderer[0];
    public string scena = "BraxhornCity_Wyspa";

    float nast;
    bool proxyWidoczne = true;
    // Wczytana scena przejmuje rysowanie natychmiast: zero klatek
    // z dwoma nakładającymi się terenami.

    bool CzyPrawdziwaWyspa()
    {
        if (string.IsNullOrEmpty(scena)) return false;
        var target = SceneManager.GetSceneByName(scena);
        if (!target.IsValid() || !target.isLoaded) return false;
        var roots = target.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i] && roots[i].activeInHierarchy &&
                roots[i].name.StartsWith("CLAUDE wyspa", System.StringComparison.Ordinal))
                return true;
        return false;
    }

    void Synchronizuj(string powod)
    {
        bool gotowa = CzyPrawdziwaWyspa();
        bool widac = !gotowa;
        if (proxyWidoczne != widac)
        {
            proxyWidoczne = widac;
            Debug.Log("BRAXHORN_WYSPA_WIDOCZNOSC " + powod +
                " proxy=" + widac + " real_scene=" + gotowa);
        }
        UstawWidocznosc(widac);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += Wczytano;
        SceneManager.sceneUnloaded += Zwolniono;
        Synchronizuj("aktywacja");
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= Wczytano;
        SceneManager.sceneUnloaded -= Zwolniono;
    }
    void Wczytano(Scene s, LoadSceneMode mode)
    {
        if (s.name == scena) Synchronizuj("wczytano");
    }
    void Zwolniono(Scene s)
    {
        if (s.name == scena) Synchronizuj("zwolniono");
    }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        // Wydarzenie SceneManager wyłącza proxy w tej samej ramce,
        // a ten lekki fallback chroni przed nietypowym porządkiem aktywacji.
        Synchronizuj("stan");
    }

    void UstawWidocznosc(bool widac)
    {
        foreach (var r in renderery)
            if (r && r.enabled != widac) r.enabled = widac;
    }
}
