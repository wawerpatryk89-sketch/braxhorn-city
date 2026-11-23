using UnityEngine;

// BRAXHORN — JEDNA KAMERA W KAŻDYM REJONIE (ZASADY 10.8).
//
// Właściciel 23.11.2026, 01:27: „kamera w każdym kolejnym buildzie już ma być bliżej postaci.
// Tak jak w GTA najnowszym. Blisko postaci kamera, najlepiej to będzie widać wtedy.”
//
// Do 23.11 kamera była wszędzie taka sama (5,8 za postacią, 7–9,5 za autem), ale liczby były
// wpisane wprost w BraxGymPlayer.cs — a ten plik ma cztery różne wersje na czterech gałęziach.
// Zmiana „kamera bliżej” oznaczałaby cztery ręczne poprawki w czterech miejscach, czyli
// dokładnie ten rozjazd, o który właściciel pyta od 16.11.
//
// Dlatego: LICZBY SĄ TUTAJ I TYLKO TUTAJ. BraxGymPlayer w każdym rejonie czyta je stąd.
// „Jeszcze bliżej” albo „trochę dalej” to zmiana jednej liczby na main — i idzie do wszystkich.
// Sprawdzacz (reguła K1) nie przepuszcza gałęzi, w której odległość kamery jest wpisana liczbą.
//
// Jednostki: jednostki gry. 1 m świata = 1,294 jednostki (ZASADY 6.5), bohater ma 2,33.
public static class BraxKameraStandard
{
    /// <summary>Pieszo: odległość kamery od celu (cel = 1,65 nad stopami). 4,2 j. = ok. 3,2 m.
    /// Przy polu widzenia 55° postać zajmuje około połowy wysokości ekranu (przy 5,8 — nieco ponad jedną trzecią).</summary>
    public const float Pieszo = 4.2f;

    /// <summary>Za autem na postoju.</summary>
    public const float AutoMin = 5.5f;

    /// <summary>Za autem przy pełnej prędkości (kamera odjeżdża, żeby było widać drogę).</summary>
    public const float AutoMaks = 7.5f;

    /// <summary>Prędkość auta, przy której kamera jest już na AutoMaks.</summary>
    public const float AutoPelnaPredkosc = 12f;

    /// <summary>Odległość kamery za autem dla danej prędkości — jedno liczenie dla wszystkich rejonów.</summary>
    public static float Auto(float predkosc)
    {
        return Mathf.Lerp(AutoMin, AutoMaks, Mathf.Clamp01(Mathf.Abs(predkosc) / AutoPelnaPredkosc));
    }
}
