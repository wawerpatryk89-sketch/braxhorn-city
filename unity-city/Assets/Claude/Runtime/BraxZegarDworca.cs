using System;
using UnityEngine;

// ZEGAR DWORCA R1 (11.11.2026, zasada „wszystko funkcjonalne jak w życiu”: zegary chodzą). Wskazówki obracają się wokół lokalnej
// osi Z (oś tarczy, zwrot do patrzącego) według czasu urządzenia; sekundnik skacze co sekundę jak w zegarach dworcowych.
// Klasa = plik, pola publiczne bez #if.
public sealed class BraxZegarDworca : MonoBehaviour
{
    public Transform godzinowa, minutowa, sekundnik;
    int ostatnia = -1;

    void Update()
    {
        var t = DateTime.Now;
        int s = t.Hour * 3600 + t.Minute * 60 + t.Second;
        if (s == ostatnia) return;
        ostatnia = s;
        float h = (t.Hour % 12 + t.Minute / 60f) * 30f, m = (t.Minute + t.Second / 60f) * 6f, sek = t.Second * 6f;
        if (godzinowa) godzinowa.localRotation = Quaternion.Euler(0f, 0f, -h);
        if (minutowa) minutowa.localRotation = Quaternion.Euler(0f, 0f, -m);
        if (sekundnik) sekundnik.localRotation = Quaternion.Euler(0f, 0f, -sek);
    }
}
