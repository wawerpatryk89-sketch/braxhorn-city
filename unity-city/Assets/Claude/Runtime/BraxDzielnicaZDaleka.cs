using UnityEngine;
using UnityEngine.SceneManagement;

// BRYŁY Z DALEKA (dzielnica zachodnia, 2.11): jedna uproszczona siatka wieżowców i budynków dzielnicy w scenie głównej miasta —
// sylwetka widoczna z tarasu Tower, z plaży i z wiaduktu, gdy obszar BraxhornCity_Dzielnica nie jest wczytany (BraxObszary).
// Gdy obszar się wczyta (prawdziwe budynki stoją), bryły są chowane. Sprawdzanie co 0,5 s, bez pracy w klatce.
public sealed class BraxDzielnicaZDaleka : MonoBehaviour
{
    public string scena = "BraxhornCity_Dzielnica";
    public Renderer bryly;
    float nast;
    bool? stan;

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .5f;
        bool wczytany = SceneManager.GetSceneByName(scena).isLoaded;
        if (stan.HasValue && stan.Value == wczytany) return;
        stan = wczytany;
        if (bryly) bryly.enabled = !wczytany;
        Debug.Log("BRAXHORN_DZIELNICA bryły z daleka: " + (wczytany ? "schowane (obszar wczytany)" : "widoczne (obszar niewczytany)"));
    }
}
