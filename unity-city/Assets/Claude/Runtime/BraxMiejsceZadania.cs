using UnityEngine;

// MIEJSCE ZADANIA (dzielnica zachodnia, 2.11): punkt AKCJI pod misje — dyżurka komisariatu (zgłoszenie), areszt (zatrzymanie),
// parking radiowozów (start pościgu), izba przyjęć szpitala, okienko kasowe banku… W promieniu `promien` przycisk AKCJA pokazuje
// `napis`; naciśnięcie zapisuje w dzienniku BRAXHORN_MISJA <id> (system misji podepnie się po id: Zgloszono / Ostatnie) i pokazuje
// krótki dymek nad graczem. Rejestr wspólny: BraxInterakcje (jak szlaban, boisko). Tylko pola proste (bez #if — zasada 2).
public sealed class BraxMiejsceZadania : MonoBehaviour, IBraxInterakcja
{
    public string id = "";
    public string napis = "";
    public float promien = 1.6f;
    public static string Ostatnie = "";
    public static float OstatnieCzas = -1f;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    public string Kontekst(Vector3 gracz)
    {
        var d = gracz - transform.position; if (Mathf.Abs(d.y) > 2f) return "";
        d.y = 0f; return d.magnitude <= promien ? napis : "";
    }

    public bool Wykonaj(Vector3 gracz)
    {
        if (string.IsNullOrEmpty(Kontekst(gracz))) return false;
        Ostatnie = id; OstatnieCzas = Time.time;
        Debug.Log("BRAXHORN_MISJA " + id + " (" + napis + ") przy " + transform.position.ToString("F1"));
        var p = FindAnyObjectByType<BraxGymPlayer>();
        if (p) BraxDymki.Pokaz(p.transform, napis, 2.5f, 2.6f);
        return true;
    }
}
