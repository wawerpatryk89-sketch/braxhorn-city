using UnityEngine;

// BRAXHORN — zamknięta sala bez rysowania świata za ścianami (audyt FPS siłowni A, 1.11: 9–14 kl./s na ŚREDNIEJ, gdy
// kamera patrzy na mural — za ścianą jest cały park i osiedle B do 420 m zasięgu kamery, rysowane mimo że ich nie widać;
// gra nie ma wypiekanego zasłaniania). Gdy kamera jest w jednym z pudeł sali (hala, przejście, szatnia), zasięg kamery
// spada do `zasieg` (dalej niż najdalsza ściana sali i przez otwarte drzwi do kasyna), a trawa 3D osiedla (kępy za
// ścianami) nie jest rysowana. Po wyjściu kamery z sali — wartości sprzed wejścia (chyba że coś je w międzyczasie
// zmieniło, np. tryb grafiki — wtedy zostaje nowa wartość). Wygląd w środku bez zmian.
public sealed class BraxClaudeZasiegWnetrza : MonoBehaviour
{
    public Bounds[] sale = new Bounds[0];
    public float zasieg = 60f;
    Camera kam; bool wSrodku; float stary, staraMurawa, staraLaka; BraxClaudeTrawa trawa; float szukaj;

    void LateUpdate()
    {
        if (!kam || !kam.isActiveAndEnabled) { kam = Camera.main; if (!kam) return; }
        var p = kam.transform.position; bool w = false;
        foreach (var b in sale) if (b.Contains(p)) { w = true; break; }
        if (!trawa && Time.unscaledTime > szukaj) { szukaj = Time.unscaledTime + 2f; trawa = FindAnyObjectByType<BraxClaudeTrawa>(); }
        if (w)
        {
            // także co klatkę: tryb grafiki po wczytaniu sceny ustawia zasięg na nowo
            if (kam.farClipPlane > zasieg + .01f) { stary = kam.farClipPlane; kam.farClipPlane = zasieg; }
            if (!wSrodku && trawa) { staraMurawa = trawa.zasiegMurawy; staraLaka = trawa.zasiegLaki; trawa.zasiegMurawy = 0f; trawa.zasiegLaki = 0f; }
            wSrodku = true;
        }
        else if (wSrodku)
        {
            wSrodku = false;
            if (Mathf.Abs(kam.farClipPlane - zasieg) < .01f && stary > zasieg) kam.farClipPlane = stary;
            if (trawa && trawa.zasiegMurawy == 0f && trawa.zasiegLaki == 0f) { trawa.zasiegMurawy = staraMurawa; trawa.zasiegLaki = staraLaka; }
        }
    }
}
