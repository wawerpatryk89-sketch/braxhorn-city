using UnityEngine;

// BRAXHORN — wolny obrót wokół osi obiektu (MIASTO R1, 10.11: rożen kebaba w barach — właściciel 09:10: „kebaba, który się
// kręci”). Mięso na pręcie (Library/kebab_mieso.glb, oś obrotu = oś Y modelu) obraca się jak w prawdziwym rożnie.
// 11.11 (właściciel 19:35: „kebab się nie kręci… Nie kręci się nic”): odległość liczona była od Camera.main — w grze kamera
// z tagiem MainCamera bywa inna niż kamera gracza (kadr, mapa), więc warunek „bliżej niż 25 m” mógł nigdy nie zajść. Teraz:
// odległość od gracza (BraxGymPlayer, szukany co 1 s); bez gracza — kręci się zawsze. Zasięg 40 m, tempo 1 obrót na 12 s
// (wyraźnie widać, że się kręci). Oś: pion świata (kebab) albo oś pręta w układzie obiektu (kurczaki na rożnie poziomym).
public sealed class BraxObrot : MonoBehaviour
{
    public float stopniNaSekunde = 30f;
    public float zasieg = 40f;
    public Vector3 os = Vector3.up;          // oś obrotu przez punkt obiektu: kebab — pion świata; rożen poziomy — oś pręta
    public bool osSwiata = true;             // true: oś w układzie świata (kebab), false: w układzie obiektu (pręt kurczaków)
    bool blisko = true; float sprawdz, szukaj;
    Transform gracz;

    void Update()
    {
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            if (!gracz && Time.unscaledTime >= szukaj)
            {
                szukaj = Time.unscaledTime + 1f;
                var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform;
            }
            blisko = !gracz || (gracz.position - transform.position).sqrMagnitude < zasieg * zasieg;
        }
        if (blisko) transform.Rotate(os, stopniNaSekunde * Time.deltaTime, osSwiata ? Space.World : Space.Self);
    }
}
