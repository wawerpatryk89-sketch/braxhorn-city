using UnityEngine;
using UnityEngine.SceneManagement;

// OBSZAR Z DALEKA (9.11, właściciel 17:42–17:45: „zobacz jak to się rysuje… tak samo wyspa, jak do niej płynę — globalnie
// poprawnie”, „jak pokazują się obiekty”). Miasto jest podzielone na kwadraty 150 m (oraz nabrzeże i wyspę) doczytywane
// dopiero 120 m (wyspa 450 m) przed granicą — dalej było puste morze/pusta ulica, a budynki „wyskakiwały” przy podejściu.
// Przy buildzie (BraxClaudeZDaleka) z zawartości KAŻDEGO obszaru powstaje uproszczona kopia dużych brył (te same materiały
// i tekstury, siatka zgrupowana w komórki), która zostaje w scenie głównej. Ten komponent pokazuje ją, dopóki prawdziwa scena
// obszaru nie jest wczytana, i chowa w tej samej klatce, w której scena obszaru jest gotowa (zdarzenie sceneLoaded) —
// bez nakładania dwóch wersji i bez pustki. Po zwolnieniu obszaru kopia wraca. Sprawdzanie zapasowe co 0.5 s.
public sealed class BraxObszarZDaleka : MonoBehaviour
{
    public string scena;
    public Renderer[] renderery = new Renderer[0];

    float nast;
    int stan = -1;

    void OnEnable()
    {
        SceneManager.sceneLoaded += Wczytano;
        SceneManager.sceneUnloaded += Zwolniono;
        Synchronizuj();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= Wczytano;
        SceneManager.sceneUnloaded -= Zwolniono;
    }

    void Wczytano(Scene s, LoadSceneMode m) { if (s.name == scena) Synchronizuj(); }
    void Zwolniono(Scene s) { if (s.name == scena) Synchronizuj(); }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        Synchronizuj();
    }

    void Synchronizuj()
    {
        if (string.IsNullOrEmpty(scena)) return;
        var s = SceneManager.GetSceneByName(scena);
        bool wczytany = s.IsValid() && s.isLoaded;
        int nowy = wczytany ? 1 : 0;
        if (nowy == stan) return;
        stan = nowy;
        foreach (var r in renderery) if (r) r.enabled = !wczytany;
    }
}
