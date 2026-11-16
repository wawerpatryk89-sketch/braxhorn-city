using UnityEngine;

// WYJĄTEK OD JEDNEGO OBRAZU. Rejon, który świadomie musi mieć inny obraz (noc, ciemne wnętrze,
// obóz, wnętrze kina), wstawia ten komponent do scenu i wpisuje powód — wtedy BraxObrazGlobalny
// pomija standard, ale WYPISUJE to w logu buildu. Bez powodu wyjątek też działa, tylko w logu
// stoi „bez podanego powodu” i widać, że ktoś go wstawił po cichu.
//
// Osobny plik, bo komponent musi dać się wstawić z inspektora, a Unity wiąże to z nazwą pliku.
public sealed class BraxObrazWyjatek : MonoBehaviour
{
    [TextArea] public string powod = "";
}
