using UnityEngine;

// BRAXHORN — SYLWETKI Z DALEKA (wykonawca B, 2.11, ETAP 3: CENTRUM z wieżowcami 80…200 m × k).
// Miasto jest dzielone przy buildzie na kwadraty 150 m doczytywane dopiero 120 m przed ich zawartością (BraxObszary), więc
// wieżowce CENTRUM (x −262…−156) z siłowni, z tarasu BRAXHORN TOWER i z plaży byłyby niewidoczne. Ta sylwetka (bryły
// wieżowców 0.5 m w głąb od lica, ciemne szkło, jedna siatka) zostaje w scenie głównej (korzeń z pustą LODGroup — niepodzielny)
// i znika, gdy kamera jest bliżej niż `ukryjBlizej` od obszaru (wtedy kwadrat z prawdziwymi wieżowcami jest już wczytany;
// sylwetka jest w środku brył, więc przy krótkim nakładaniu nie migocze). Sprawdzanie co 0.5 s. Klasa = plik; bez pól w #if.
public sealed class BraxSylwetkiZDaleka : MonoBehaviour
{
    public Renderer sylwetka;
    public Bounds obszar;
    public float ukryjBlizej = 100f;

    float nast;
    BraxGymPlayer gracz;

    void Update()
    {
        if (Time.unscaledTime < nast || !sylwetka) return;
        nast = Time.unscaledTime + .5f;
        // Nie wszystkie androidowe kamery miasta mają tag MainCamera.
        // Dotąd Camera.main==null pozostawiał pełne czarne bryły "z daleka" nawet
        // obok gracza. Odległość wyznacz też ze znanej postaci Braxa.
        var kam = Camera.main;
        if (!kam && !gracz) gracz = FindFirstObjectByType<BraxGymPlayer>();
        if (!kam && !gracz)
        {
            sylwetka.enabled = false; // brak pewnej pozycji: nie zasłaniaj prawdziwego miasta
            return;
        }
        var p = kam ? kam.transform.position : gracz.transform.position;
        var c = obszar.center; c.y = p.y;
        var b = new Bounds(c, new Vector3(obszar.size.x, 1f, obszar.size.z));
        bool widac = b.SqrDistance(p) > ukryjBlizej * ukryjBlizej;
        if (sylwetka.enabled != widac) sylwetka.enabled = widac;
    }
}
