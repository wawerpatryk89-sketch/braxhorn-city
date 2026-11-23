using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// MIASTO R1 — trawa 3D osiedla bydgoskiego (23.11): kępy generowane w biegu wokół kamery z MASKI TRAWNIKA zamiast zapisanej
// listy kęp. Osiedle ma ~245 tys. m² trawników (j. gry) — przy 20 kępach/m² to ~5 mln kęp, czyli ~100 MB danych; z maską
// (1 bit na 0,32 m) jest ~0,6 MB, a kępy liczy się tylko w kawałkach 8 × 8 m w zasięgu kamery (deterministycznie: ten sam
// kawałek zawsze daje te same kępy). Ta sama siatka kępy i materiał co trawa miasta (BraxRegionTrawa: Grass Bermuda 01),
// ta sama gęstość i wysokość murawy. Rysowanie: Graphics.DrawMeshInstanced po ≤ 1023, bez cieni, zanik z odległością w shaderze.
public class BraxClaudeTrawaMaska : ScriptableObject
{
    public Vector2 poczatek;      // x, z (j. gry) lewego dolnego rogu maski
    public float krok = .32f;     // bok piksela maski (j. gry)
    public int szer, wys;         // piksele
    public byte[] bity = new byte[0];
    public float y = .1f;         // wysokość trawnika (j. gry)
    public float gestosc = 20f;   // kęp na m² (j. gry)
    public float wysokoscKepy = .13f, skalaSiatki = .32f;

    public bool Trawa(float x, float z)
    {
        int i = Mathf.FloorToInt((x - poczatek.x) / krok), j = Mathf.FloorToInt((z - poczatek.y) / krok);
        if (i < 0 || j < 0 || i >= szer || j >= wys) return false;
        int k = j * szer + i; return (bity[k >> 3] & (1 << (k & 7))) != 0;
    }
}
