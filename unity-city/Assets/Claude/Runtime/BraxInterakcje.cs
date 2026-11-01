using System.Collections.Generic;
using UnityEngine;

// BRAXHORN — ogólny rejestr interakcji przycisku AKCJA (1.11, rozrywka na osiedlu ma być użytkowa).
// Dowolny obiekt (boisko, wyrzutnia piłek, później misje) dopisuje się do listy i odpowiada na dwa pytania:
//   Kontekst(gracz) — napis na przycisku AKCJA ("" = nic tu do zrobienia),
//   Wykonaj(gracz)  — wykonanie (true = obsłużone, dalsze interakcje nie są sprawdzane).
// BraxCityWorld pyta rejestr zaraz po pojazdach i siedziskach (Context / Interact).
public interface IBraxInterakcja
{
    string Kontekst(Vector3 gracz);
    bool Wykonaj(Vector3 gracz);
}

public static class BraxInterakcje
{
    static readonly List<IBraxInterakcja> lista = new List<IBraxInterakcja>();

    public static void Dodaj(IBraxInterakcja d) { if (d != null && !lista.Contains(d)) lista.Add(d); }
    public static void Usun(IBraxInterakcja d) { lista.Remove(d); }

    public static string Kontekst(Vector3 gracz)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            var d = lista[i]; if (d == null || (d is Object o && !o)) continue;
            var k = d.Kontekst(gracz);
            if (!string.IsNullOrEmpty(k)) return k;
        }
        return "";
    }

    public static bool Wykonaj(Vector3 gracz)
    {
        for (int i = 0; i < lista.Count; i++)
        {
            var d = lista[i]; if (d == null || (d is Object o && !o)) continue;
            if (!string.IsNullOrEmpty(d.Kontekst(gracz)) && d.Wykonaj(gracz)) return true;
        }
        return false;
    }
}
