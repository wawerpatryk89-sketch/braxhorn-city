using System;
using UnityEngine;

// PUNKT AKCJI POD MISJE (wnętrza miasta 2.11): kasa, lada, recepcja, szatnia, bar… Gracz w promieniu → na przycisku AKCJA
// napis `etykieta` (KUP BILET, ZAMÓW, POROZMAWIAJ, ODBIERZ…); naciśnięcie → dymek z odpowiedzią nad punktem obsady
// (albo nad samym punktem) i zdarzenie `Wykonano(id)` dla przyszłych misji. Rejestr: BraxInterakcje (ten sam co boiska).
// `id` stałe (np. "metro_kasa", "tower_recepcja") — misje odwołują się do niego, nie do pozycji.
public sealed class BraxPunktAkcji : MonoBehaviour, IBraxInterakcja
{
    public static event Action<string> Wykonano;

    public string id = "";
    public string etykieta = "AKCJA";
    [TextArea] public string odpowiedz = "";
    public float promien = 1.7f;
    public Transform mowca;             // punkt obsady (np. kasjer) — dymek nad nim; puste = nad punktem
    public float wysDymka = 2.4f;

    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    public string Kontekst(Vector3 gracz)
    {
        var v = gracz - transform.position;
        if (Mathf.Abs(v.y) > 2.2f) return "";
        v.y = 0f;
        return v.sqrMagnitude <= promien * promien ? etykieta : "";
    }

    public bool Wykonaj(Vector3 gracz)
    {
        if (!string.IsNullOrEmpty(odpowiedz)) BraxDymki.Pokaz(mowca ? mowca : transform, odpowiedz, 3.5f, wysDymka);
        Debug.Log("BRAXHORN_AKCJA " + id + " (" + etykieta + ")");
        var w = Wykonano; if (w != null) w(id);
        return true;
    }
}
