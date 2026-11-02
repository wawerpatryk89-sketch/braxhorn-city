using UnityEngine;

// BRAXHORN CITY — Brax nie chodzi po wodzie i nie spaceruje po dnie (zdjęcie CI 13.1 nr 35: „Brax stoi na wodzie”).
// Niewidzialna podłoga źródła y=0 nad morzem jest usunięta (BraxRegionSea), więc w wodzie Brax schodzi po piasku plaży
// najwyżej po pas (wejście do kąpieliska). Głębiej niż 1,3 m pod lustrem wody (spadnięcie z pomostu, rewa za bojami) —
// wraca na ostatnie suche miejsce (pomost, plaża, bulwar). Na łodzi i na siedzisku nie działa. Koszt: jedno porównanie na klatkę.
public sealed class BraxWodaGlebia : MonoBehaviour
{
    public float woda = -1.7f;
    public float glebokosc = 1.3f;
    public Rect morze = new Rect(84f, -520f, 816f, 990f);   // x 84…900, z −520…470
    BraxGymPlayer gracz; Vector3 suche; bool jest; float szukaj;

    void Update()
    {
        if (!gracz)
        {
            if (Time.unscaledTime < szukaj) return;
            szukaj = Time.unscaledTime + 1f;
            gracz = FindFirstObjectByType<BraxGymPlayer>();
            if (!gracz) return;
        }
        if (gracz.vessel || gracz.seat || !gracz.body || !gracz.body.enabled) return;
        var p = gracz.transform.position;
        bool nadMorzem = morze.Contains(new Vector2(p.x, p.z));
        if (!nadMorzem || p.y > woda + .05f)
        {
            if (gracz.body.isGrounded) { suche = p; jest = true; }
            return;
        }
        if (p.y < woda - glebokosc)
        {
            var cel = jest ? suche : gracz.worldSpawn;
            Debug.Log("BRAXHORN_WODA za głęboko (y " + p.y.ToString("F1") + ") — powrót na brzeg " + cel.ToString("F1"));
            gracz.TravelTo(cel + Vector3.up * .1f);
        }
    }
}
