using System.Collections.Generic;
using UnityEngine;

// TEKSTY INTERFEJSU PL / EN (build 14, zlecenie „sterowanie”). Prosta tabela tłumaczeń: klucz → { polski, angielski }.
// Język: domyślnie wg języka telefonu (Application.systemLanguage: polski → PL, każdy inny → EN), zmiana w MENU → USTAWIENIA,
// wybór zapamiętany (PlayerPrefs). Wszystkie NOWE napisy interfejsu idą przez BraxTeksty.T("klucz").
// Odczyt bez alokacji (słownik + tablica), formatowane komunikaty (F) alokują tylko w chwili pokazania komunikatu.
public static class BraxTeksty
{
    public enum Jezyk { PL = 0, EN = 1 }
    const string Klucz = "braxhorn_jezyk";
    static bool wczytany;
    static Jezyk jezyk = Jezyk.PL;

    public static Jezyk Aktualny { get { Wczytaj(); return jezyk; } }
    public static int Wersja { get; private set; }   // rośnie przy każdej zmianie języka (pamięci podręczne napisów odświeżają się same)

    static void Wczytaj()
    {
        if (wczytany) return;
        wczytany = true;
        int domyslny = Application.systemLanguage == SystemLanguage.Polish ? 0 : 1;
        int v = -1;
        try { v = PlayerPrefs.GetInt(Klucz, -1); } catch { }
        jezyk = (Jezyk)(v < 0 ? domyslny : Mathf.Clamp(v, 0, 1));
    }

    public static void Ustaw(Jezyk j)
    {
        Wczytaj();
        if (jezyk == j) return;
        jezyk = j; Wersja++;
        try { PlayerPrefs.SetInt(Klucz, (int)j); PlayerPrefs.Save(); } catch { }
    }

    public static string T(string klucz)
    {
        Wczytaj();
        return Tabela.TryGetValue(klucz, out var t) ? t[(int)jezyk] : klucz;
    }

    public static string F(string klucz, object a) => string.Format(T(klucz), a);
    public static string F(string klucz, object a, object b) => string.Format(T(klucz), a, b);

    public static int LiczbaWskazowek => 7;
    public static string Wskazowka(int i) => T("wsk." + ((i % LiczbaWskazowek + LiczbaWskazowek) % LiczbaWskazowek));

    // Napis przycisku AKCJA pochodzi z kodu gry (po polsku). Po angielsku: znane początki czynności tłumaczone,
    // reszta (nazwa miejsca) bez zmian. Pamięć ostatniego napisu — bez alokacji co klatkę.
    static string kontekstWe, kontekstWy; static int kontekstWersja = -1;
    static readonly string[,] Czynnosci =
    {
        { "OTWÓRZ DRZWI", "OPEN DOOR" }, { "ZAMKNIJ DRZWI", "CLOSE DOOR" }, { "USIĄDŹ", "SIT" }, { "POŁÓŻ SIĘ", "LIE DOWN" },
        { "WSTAŃ", "STAND UP" }, { "WSIĄDŹ", "BOARD" }, { "ZEJDŹ", "GET OFF" }, { "POGADAJ", "TALK" }, { "WRÓĆ DO MIASTA", "BACK TO THE CITY" },
        { "WEJDŹ", "ENTER" }, { "ZAMÓW", "ORDER" }, { "ZAGRAJ", "PLAY" }, { "TUNEL", "TUNNEL" }, { "WCZYTYWANIE DZIELNICY…", "LOADING DISTRICT…" }, { "ULICA — KOLEJNY ETAP", "STREET — NEXT STAGE" },
    };
    public static string Kontekst(string pl)
    {
        if (string.IsNullOrEmpty(pl) || Aktualny == Jezyk.PL) return pl;
        if (pl == kontekstWe && kontekstWersja == Wersja) return kontekstWy;
        string wy = pl;
        for (int i = 0; i < Czynnosci.GetLength(0); i++)
            if (pl.StartsWith(Czynnosci[i, 0], System.StringComparison.Ordinal)) { wy = Czynnosci[i, 1] + pl.Substring(Czynnosci[i, 0].Length); break; }
        kontekstWe = pl; kontekstWy = wy; kontekstWersja = Wersja;
        return wy;
    }

    static readonly Dictionary<string, string[]> Tabela = new Dictionary<string, string[]>
    {
        // ekran gry
        { "hud.podpowiedz", new[] { "lewy kciuk: ruch (mocniej = bieg)  ·  prawy: kamera", "left thumb: move (push further = run)  ·  right: camera" } },
        { "hud.lodz", new[] { "GAZ: w górę  ·  STER: w bok", "THROTTLE: up  ·  STEER: sideways" } },
        { "hud.zdjecia", new[] { "TRYB ZDJĘĆ — dotknij ekranu, żeby wrócić", "PHOTO MODE — tap the screen to return" } },
        { "hud.muzyka", new[] { "MUZYKA WŁ. / WYŁ.", "MUSIC ON / OFF" } },
        // menu główne
        { "menu.pauza", new[] { "PAUZA", "PAUSED" } },
        { "menu.wroc", new[] { "WRÓĆ", "RESUME" } },
        { "menu.ustawienia", new[] { "USTAWIENIA", "SETTINGS" } },
        { "menu.mapa", new[] { "MAPA", "MAP" } },
        { "menu.mapa_wkrotce", new[] { "MAPA · wkrótce", "MAP · soon" } },
        { "menu.zglos", new[] { "ZGŁOŚ", "REPORT" } },
        { "menu.utknalem", new[] { "UTKNĄŁEM", "I'M STUCK" } },
        { "menu.zdjecia", new[] { "TRYB ZDJĘĆ", "PHOTO MODE" } },
        { "menu.test", new[] { "TEST", "TEST" } },
        { "menu.autorzy", new[] { "AUTORZY", "CREDITS" } },
        { "menu.zamknij", new[] { "ZAMKNIJ", "CLOSE" } },
        { "menu.wstecz", new[] { "WSTECZ", "BACK" } },
        { "menu.dalej", new[] { "DALEJ", "NEXT" } },
        { "menu.poprzednia", new[] { "POPRZEDNIA", "PREVIOUS" } },
        { "menu.mapa_ty", new[] { "zielona kropka = Ty", "green dot = you" } },
        { "menu.autorzy_brak", new[] { "BRAXHORN CITY — AUTORZY\n(brak listy w tym buildzie)", "BRAXHORN CITY — CREDITS\n(no list in this build)" } },
        { "menu.strona", new[] { "strona {0} / {1}", "page {0} / {1}" } },
        // ustawienia
        { "ust.muzyka", new[] { "GŁOŚNOŚĆ MUZYKI", "MUSIC VOLUME" } },
        { "ust.efekty", new[] { "GŁOŚNOŚĆ EFEKTÓW", "EFFECTS VOLUME" } },
        { "ust.czulosc", new[] { "CZUŁOŚĆ KAMERY", "CAMERA SENSITIVITY" } },
        { "ust.przyciski", new[] { "ROZMIAR PRZYCISKÓW", "BUTTON SIZE" } },
        { "ust.male", new[] { "MAŁE", "SMALL" } },
        { "ust.srednie", new[] { "ŚREDNIE", "MEDIUM" } },
        { "ust.duze", new[] { "DUŻE", "LARGE" } },
        { "ust.grafika", new[] { "JAKOŚĆ GRAFIKI", "GRAPHICS QUALITY" } },
        { "ust.niska", new[] { "NISKA", "LOW" } },
        { "ust.srednia", new[] { "ŚREDNIA", "MEDIUM" } },
        { "ust.wysoka", new[] { "WYSOKA", "HIGH" } },
        { "ust.auto", new[] { "automatycznie dla tego telefonu: {0} ({1})", "automatic for this phone: {0} ({1})" } },
        { "ust.reczny", new[] { "wybrane ręcznie (zapamiętane) · automatycznie byłoby: {0}", "chosen manually (saved) · automatic would be: {0}" } },
        { "ust.jezyk", new[] { "JĘZYK", "LANGUAGE" } },
        { "ust.kamera", new[] { "KAMERA ZA PLECAMI", "CAMERA BEHIND" } },
        { "ust.polski", new[] { "POLSKI", "POLSKI" } },
        { "ust.english", new[] { "ENGLISH", "ENGLISH" } },
        // test
        { "test.licznik", new[] { "LICZNIK KL./S I PAMIĘCI", "FPS & MEMORY COUNTER" } },
        { "test.wl", new[] { "WŁ.", "ON" } },
        { "test.wyl", new[] { "WYŁ.", "OFF" } },
        { "test.teleport", new[] { "TELEPORT DO MIEJSC", "TELEPORT TO PLACES" } },
        { "test.pora", new[] { "PORA DNIA · wkrótce", "TIME OF DAY · soon" } },
        { "test.pogoda", new[] { "POGODA · wkrótce", "WEATHER · soon" } },
        { "test.odnowa", new[] { "START REJONU (OD NOWA)", "REGION START (RESET)" } },
        { "test.brak_miejsc", new[] { "Brak miejsc do teleportu w tym rejonie.", "No teleport places in this area." } },
        { "test.pamiec", new[] { "{0} KL./S · pamięć {1}", "{0} FPS · memory {1}" } },
        // komunikaty
        { "msg.zapisano", new[] { "ZAPISANO zgłoszenie: {0}", "Report SAVED: {0}" } },
        { "msg.folder", new[] { "folder: {0}", "folder: {0}" } },
        { "msg.blad_zapisu", new[] { "Nie udało się zapisać zgłoszenia: {0}", "Could not save the report: {0}" } },
        { "msg.chodnik", new[] { "Przeniesiono na najbliższy chodnik ({0} m)", "Moved to the nearest pavement ({0} m)" } },
        { "msg.droga", new[] { "Przeniesiono na najbliższą drogę ({0} m)", "Moved to the nearest road ({0} m)" } },
        { "msg.punkt", new[] { "Przeniesiono: {0}", "Moved to: {0}" } },
        { "msg.reset", new[] { "Powrót na start rejonu", "Back to the region start" } },
        { "msg.teleport", new[] { "Teleport: {0}", "Teleport: {0}" } },
        // miejsca (TEST → TELEPORT)
        { "miejsce.kawiarnia", new[] { "KAWIARNIA", "CAFÉ" } },
        { "miejsce.market", new[] { "MARKET", "SUPERMARKET" } },
        { "miejsce.silownia", new[] { "SIŁOWNIA", "GYM" } },
        { "miejsce.arena", new[] { "ARENA BRAXHORN", "ARENA BRAXHORN" } },
        { "miejsce.kasyno", new[] { "KASYNO", "CASINO" } },
        { "miejsce.szpital", new[] { "SZPITAL", "HOSPITAL" } },
        { "miejsce.komisariat", new[] { "KOMISARIAT", "POLICE STATION" } },
        { "miejsce.park", new[] { "PARK", "PARK" } },
        { "miejsce.boiska", new[] { "BOISKA", "SPORTS COURTS" } },
        { "miejsce.tunel", new[] { "TUNEL", "TUNNEL" } },
        { "miejsce.wille", new[] { "WILLE", "VILLAS" } },
        { "miejsce.inne", new[] { "INNE", "OTHER" } },
        // ekran wczytywania
        { "start.przygotowanie", new[] { "PRZYGOTOWANIE…", "PREPARING…" } },
        { "start.wczytywanie", new[] { "WCZYTYWANIE", "LOADING" } },
        { "start.uruchamianie", new[] { "URUCHAMIANIE…", "STARTING…" } },
        { "start.nieudany", new[] { "POPRZEDNI START SIĘ NIE UDAŁ — ZRÓB ZRZUT EKRANU. DOTKNIJ, ŻEBY WCZYTAĆ GRĘ.", "THE PREVIOUS START FAILED — TAKE A SCREENSHOT. TAP TO LOAD THE GAME." } },
        { "start.brak_sceny", new[] { "BRAK SCENY GRY W PACZCE", "NO GAME SCENE IN THIS PACKAGE" } },
        { "start.blad_sceny", new[] { "NIE MOŻNA WCZYTAĆ SCENY {0}", "CANNOT LOAD SCENE {0}" } },
        { "start.diag_podpowiedz", new[] { "przytrzymaj ekran 3 s — diagnostyka", "hold the screen for 3 s — diagnostics" } },
        { "start.diag_tytul", new[] { "DIAGNOSTYKA", "DIAGNOSTICS" } },
        { "start.poprzedni_ok", new[] { "Poprzedni start: bez problemu (albo pierwsze uruchomienie).", "Previous start: OK (or first launch)." } },
        { "start.wskazowka", new[] { "WSKAZÓWKA", "TIP" } },
        { "start.osiedle", new[] { "OSIEDLE TUR POD ZAMOŚCIEM", "TUR POD ZAMOŚCIEM ESTATE" } },
        { "start.miasto", new[] { "MIASTO", "THE CITY" } },
        { "start.dziennik", new[] { "Dziennik tego startu:", "This start's log:" } },
        // wskazówki na ekranie wczytywania
        { "wsk.0", new[] { "Lekkie wychylenie joysticka = chód, mocne = bieg.", "Tilt the joystick slightly to walk, all the way to run." } },
        { "wsk.1", new[] { "Przeciągnij palcem po prawej stronie ekranu, żeby obrócić kamerę. Po chwili sama wróci za plecy.", "Drag on the right side of the screen to turn the camera. It returns behind you on its own." } },
        { "wsk.2", new[] { "Przycisk AKCJA mówi, co możesz zrobić: usiąść, pogadać, otworzyć drzwi.", "The ACTION button tells you what you can do: sit, talk, open a door." } },
        { "wsk.3", new[] { "MENU → ZGŁOŚ zapisuje zrzut ekranu z miejscem w grze — tak najłatwiej pokazać błąd.", "MENU → REPORT saves a screenshot with your position — the easiest way to show a bug." } },
        { "wsk.4", new[] { "Utknąłeś? MENU → UTKNĄŁEM przeniesie cię na najbliższy chodnik.", "Stuck? MENU → I'M STUCK moves you to the nearest pavement." } },
        { "wsk.5", new[] { "Rozmiar przycisków, głośność i jakość grafiki zmienisz w MENU → USTAWIENIA.", "Change button size, volume and graphics in MENU → SETTINGS." } },
        { "wsk.6", new[] { "Drzwi otwierają się, gdy do nich podchodzisz, i same się zamykają za tobą.", "Doors open as you walk up to them and close behind you." } },
    };
}
