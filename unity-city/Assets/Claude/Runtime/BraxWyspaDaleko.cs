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

    void OnEnable()
    {
        proxyWidoczne = true;
        UstawWidocznosc(true);
    }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .20f;
        bool gotowa = false;
        if (!string.IsNullOrEmpty(scena))
        {
            var target = SceneManager.GetSceneByName(scena);
            if (target.IsValid() && target.isLoaded)
            {
                var roots = target.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] && roots[i].activeInHierarchy &&
                        roots[i].name.StartsWith("CLAUDE wyspa", System.StringComparison.Ordinal))
                    { gotowa = true; break; }
                }
            }
        }
        // W pełni aktywna scena zastępuje proxy w TEJ SAMEJ próbce.
        // Żadnych dwóch nakładających się warstw terenu podczas przełączania.
        bool maBycWidoczne = !gotowa;
        if (maBycWidoczne == proxyWidoczne) return;
        proxyWidoczne = maBycWidoczne;
        UstawWidocznosc(proxyWidoczne);
        Debug.Log("BRAXHORN_WYSPA_WIDOCZNOSC proxy=" + proxyWidoczne +
            " real_scene=" + gotowa + " (bez znikania przy samym progu odleglosci)");
    }

    void UstawWidocznosc(bool widac)
    {
        foreach (var r in renderery)
            if (r && r.enabled != widac) r.enabled = widac;
    }
}
