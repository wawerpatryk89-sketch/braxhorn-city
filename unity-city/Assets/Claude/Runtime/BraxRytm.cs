using UnityEngine;

// MIASTO R1 — rytm utworu dla świateł i laserów balkonu DJ na Starym Rynku (24.11, 22:04 właściciel: „tego seta puść na
// balkonie na starym rynku” — set DJ Kris & Pablo de Silva, 71 min, tempo 136–141 BPM). Długi set ma zmienne tempo, więc
// zamiast stałego BPM mapa uderzeń (czasy kolejnych uderzeń stopy w sekundach od początku pliku, zmierzone z nagrania);
// pozycja w uderzeniach = numer ostatniego uderzenia + ułamek drogi do następnego. Bez mapy — stałe tempo jak dotąd.
public static class BraxRytm
{
    public static float Uderzenie(float czas, float bpm, float[] mapa)
    {
        if (mapa == null || mapa.Length < 2) return czas * bpm / 60f;
        int n = mapa.Length;
        if (czas <= mapa[0]) return (czas - mapa[0]) * bpm / 60f;
        if (czas >= mapa[n - 1]) return n - 1 + (czas - mapa[n - 1]) * bpm / 60f;
        int lo = 0, hi = n - 1;
        while (hi - lo > 1) { int m = (lo + hi) >> 1; if (mapa[m] <= czas) lo = m; else hi = m; }
        return lo + (czas - mapa[lo]) / Mathf.Max(1e-3f, mapa[hi] - mapa[lo]);
    }
}
