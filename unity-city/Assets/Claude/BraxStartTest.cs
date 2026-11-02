// TEST MIASTA — USUNIĘTY w buildzie 14 (właściciel 1.11, 21:45: „co ty zrobiłeś” — 8 przełączników na ekranie startowym nic
// nie dawało, bo wyłączanie działało dopiero po wczytaniu, a awaria była w trakcie). Ekran startowy (BraxStart) już go nie
// pokazuje i nic nie jest wyłączane. Zostaje tylko ta zaślepka, bo z Wylaczony() korzysta BraxObszary (także w gałęzi
// „horyzont” buildu 14) — bez niej scalenie gałęzi przestałoby się kompilować. Do usunięcia razem z ostatnim odwołaniem.
// Klasa statyczna (nie komponent) — nie występuje w żadnej scenie.
public static class BraxStartTest
{
    public static bool Wylaczony(string klucz) => false;
}
