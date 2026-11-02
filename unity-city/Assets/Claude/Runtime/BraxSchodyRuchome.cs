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

    Vector3 start; float dlugosc;

    void Awake()
    {
        start = transform.localPosition;
        dlugosc = krok.magnitude;
    }

    void Update()
    {
        if (dlugosc < 1e-4f) return;
        float t = Mathf.Repeat(Time.time * predkosc, dlugosc) / dlugosc;
        transform.localPosition = start + krok * t;
    }
}
