using UnityEngine;

// SCHODY RUCHOME (wykonawca A, 2.11 — metro „perfekcyjne”): taśma stopni przesuwa się w kółko o jeden stopień wzdłuż biegu.
// Stopień wyjeżdża spod płyty dolnej i chowa się pod płytą górną, więc ruch wygląda na ciągły (bez przeskoku). Kolizja schodów
// to stała rampa obok taśmy — gracz po niej idzie; taśma jest tylko obrazem ruchu (bez fizyki, koszt: jedna transformacja).
public class BraxSchodyRuchome : MonoBehaviour
{
    [Tooltip("Wektor jednego stopnia w układzie rodzica (w stronę jazdy).")]
    public Vector3 krok = new Vector3(.3f, .2f, 0f);
    [Tooltip("Prędkość taśmy wzdłuż biegu (m/s); typowo 0.5.")]
    public float predkosc = .5f;

    Vector3 start; float dlugosc; float czasStart;
    bool gotowe;

    // AddComponent uruchamia Awake przed ustawieniem krok przez generator.
    // Mierzenie domyślnego (.3,.2) powodowało przeskok po każdym cyklu.
    void Start()
    {
        start = transform.localPosition;
        dlugosc = krok.magnitude;
        czasStart = Time.time;
        gotowe = dlugosc > .0001f;
    }

    void Update()
    {
        if (!gotowe) return;
        // Cały złożony pas stanowi powtarzalną sekcję. Reset następuje
        // dokładnie o JEDEN rzeczywisty krok, a nie o domyślną długość.
        float t = Mathf.Repeat((Time.time - czasStart) * predkosc, dlugosc) / dlugosc;
        transform.localPosition = start + krok * t;
    }
}
