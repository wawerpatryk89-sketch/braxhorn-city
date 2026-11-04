# Miasto — naprawa startu i przygotowanie aktualizacji

Baza miasta: `874f194624e4dfead5637a4e3dfd1d3285a9cbe1`, build Android #58.
Gałąź pracy: `codex/city58-startup-update-20261104`.
Wspólny kontrakt: `38422c5f031b5f9b85a3e1981c1a8199c9f75731`.
Wspólne narzędzia Library i checkpointów: `1d83c1a6db4f6c71eb670e5a57bab963ce50e5c1`.

## Dowody awarii #58

- Pięć raportów natywnych: `SIGTRAP`, wątek `Loading.Preload`, identyczny stos.
- Komunikat Unity: `level1 is corrupted`, `Position out of bounds`. Nie potwierdzono braku RAM.
- SHA-256 oryginalnego APK: `69592f179a12d2628dd77cbba7737d1cb35d8eea1842c200a65b7a3463b72e44`.
- ZIP/CRC oraz nagłówki i zakresy obiektów 98 plików serializowanych są poprawne. UnityPy odczytał 135501 natywnych obiektów głównej sceny; nie weryfikuje pól skryptów bez type tree.
- Rejestr x25 w powtarzających się awariach ma wartość 180994368. To początek obiektu 136026 w `level1`: `BraxMorzeZaKamera`, Assembly-CSharp, 32 bajty. Komponent ma cztery serializowane pola w kodzie, a w APK tylko bazowy nagłówek.
- Komponent był w `BraxAkwen.cs` wraz z klasą statyczną. Przeniesiono go do własnego pliku, z trwałym `.meta`; treść klasy zachowana bez zmian.
- Sam test `MonoScript.GetClass()` w Unity 6000.0.58f2 akceptuje stary komponent. Rzeczywista budowa paczek scen Androida odtworzyła błąd: stary komponent 32 bajty, poprawiony 68 bajtów (cztery pola). Unity podczas pakowania starej sceny zgłasza niewłaściwy/brakujący skrypt. Poprawiona scena zachowuje material, komorka, pol i bezWody po zapisie i ponownym wczytaniu. Dowód: workflow `41745527276`, krok Unity udany; krok zapisu raportu Python wymagał poprawienia praw plików po kontenerze.
- Workflow `41746342179` zakończył się sukcesem: potwierdzone 32 → 68 bajtów oraz odczyt rzeczywistego materiału i wszystkich trzech pozostałych pól z paczki Androida.
- Kontrola przed generowaniem wymaga teraz trwałego pliku MonoScript i zgodnej nazwy konkretnego MonoBehaviour. Tymczasowy MonoScript może zwracać poprawne `GetClass()` przy pustej ścieżce zasobu, dlatego samo `GetClass()` nie wystarczało.
- Próba emulatora nie doszła do uruchomienia aplikacji (połączenie adb). Nie jest wynikiem testu gry.

Pełny prywatny bugreport nie jest dołączany do repozytorium.

## Zakres integracji narzędzi

- `unity-tools/updates/{unity_cache,durable_cache,world_checkpoint}.py` i ich testy pochodzą bez zmian ze wspólnego commitu. Workflow miasta korzysta z nich do szyfrowanej Library, trwałej kopii w prywatnym draft release i kontroli gotowego generowania.
- Miasto ma osobny szyfrowany checkpoint scen, ustawień i GUID, odczytywany tylko przy identycznym kluczu wejść. Adapter `android_build_cache.py --world-only` nie dubluje Library. Żadne dane scen nie są zapisywane do jawnego cache Androida.
- Migracja starego cache ograniczona do dokładnego builda miasta #58. Brak/uszkodzenie cache skutkuje poprawnym importem/generowaniem, a nie użyciem częściowych danych.
- Bee/IL2CPP pozostają zachowane. Zmiany gameplay/generatora nadal unieważniają świat. Trzy pliki ekranu startowego mogą być zmieniane bez generowania świata, ponieważ start jest tworzony ponownie przed pakowaniem.
- Znacznik gotowego świata powstaje po kontroli scen i tekstur, przed BuildPlayer. Awaria późniejszego pakowania nie kasuje poprawnego generowania.
- Stabilne adresy scen miasta, portu, wyspy i sektorów oparte na współrzędnych trafiają do `miasto-strefy.json`, wraz z GUID i zależnościami. Rejestr uczciwie oznacza dostawę jako `embedded-apk`.
- Certyfikat #58 sprawdzony przez apksigner: `f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe`. Wydanie wymaga tego samego podpisu i versionCode większego od 58; identyfikator `com.braxhorn.miastogl` pozostaje.

## Stan odbioru

25 testów wspólnych narzędzi i 15 testów narzędzi miasta przeszło lokalnie. Dotyczą m.in. szyfrowania, integralności, zachowania GUID i natywnych wyników kompilacji, odrzucenia innego profilu, wycofania częściowego odczytu, prywatnego magazynu i checkpointu świata. Operacje GitHub magazynu są symulowane w testach. C# sprawdzony składniowo; rzeczywistą serializację sprawdza osobny mały workflow Unity.

Nie potwierdzono jeszcze uruchomienia poprawionego miasta, czasu kolejnego pełnego buildu ani małej aktualizacji na telefonie. Nie zmniejszono jakości ani zakresu miasta.

## Pozostałe zależności wspólne

Addressables, wspólny loader startu/przejść i HTTPS hosting z odpowiednim dostępem nadal wymagają wdrożenia. Nie kopiować całej gałęzi osiedla. Wpiąć wspólne narzędzia z uzgodnionego commitu, a sceny miasta rejestrować pod adresami powyżej. Wymagany test baza → jedna zmiana zawartości bez nowego APK, zachowanie niezmienionych hashy paczek, brak sieci/przerwanie/rollback. Obecny APK nadal zawiera miasto; nie jest to jeszcze aktualizacja samych paczek zawartości.
