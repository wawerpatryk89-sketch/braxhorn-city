# Wdrożenie w osiedlu — stan 2026-11-04

Baza: `5b9e7afb2d487ff0b5c92d79f90fffb97f70f99c`, gałąź `claude/osiedle-42-poprawka`. Pierwszy etap zmienił proces budowania. Drugi etap dodaje opt-in Addressables i prywatny import aktualizacji; zachowuje geometrię oraz progi doczytywania stref. Build #61 pracuje ze swoją pierwotną wersją i nie otrzymuje tych zmian w locie.

## Wykonane

1. Pełna szyfrowana Library: zachowane Bee, IL2CPP, PlayerDataCache oraz metadane importowanych źródeł. Pliki modeli i klucz podpisu nie są oddzielnie dołączane do archiwum. Library może zawierać przetworzone prywatne zasoby, dlatego archiwum pozostaje szyfrowane.
2. Profil cache obejmuje strefę, wersję Unity, manifest pakietów i wariant Androida. Zmiana zwykłego skryptu nie wyrzuca całej Library; Unity sam sprawdza zależności. Cache nie oznacza, że scena odpowiada nowym źródłom — generator nadal stosuje osobny dokładny klucz.
3. Odczyt: cache Actions v2 → zaszyfrowana kopia w dedykowanym draft release → wcześniejszy cache v1 do migracji → pełny import z ostrzeżeniem. Rozpakowanie odbywa się do osobnego katalogu. Zła konfiguracja, dowiązania i ścieżki wychodzące poza projekt są odrzucane. Metadane ze źródeł mają pierwszeństwo.
4. Zapis trwały: szyfrowane części do 1,5 GiB, weryfikacja SHA-256, indeks zmieniany po przesłaniu wszystkich części. Dwie ostatnie kopie, oddzielne dla gałęzi i profilu. Narzędzie odmawia zapisu do publicznego wydania i usuwa wyłącznie starsze części tego dedykowanego cache. To nie jest CDN gry.
5. Znacznik zakończonego generowania świata powstaje przed BuildPlayer. CI kontroluje klucz, platformę, sceny, zależności i `.meta`, po czym może zachować ten świat także po późniejszym błędzie IL2CPP/Gradle. Nie zapisuje niekompletnego checkpointu i nie oznacza go jako działający APK.
6. Kolejne buildy jednej gałęzi nie anulują trwającego buildu. GitHub może zastąpić starsze oczekujące uruchomienie nowszym; bieżące pozostaje w toku. Podsumowanie pokazuje realne przywrócenie i zapis cache.

## Sprawdzenie

`python3 -W error::ResourceWarning -m unittest discover -s unity-tools/updates -p 'test_*.py' -v`

32 testy: pełny cykl szyfrowania/odczytu, Bee i PlayerDataCache, zachowanie GUID źródeł, zgodność profili, integralność, niebezpieczne ścieżki i dowiązania, przerwany upload, poprzednia poprawna kopia, dwie wersje cache, odrzucenie publicznego release i niekompletnego świata oraz izolacja snapshotu content. Operacje GitHub w testach trwałego zapisu są symulowane; testy nie przesyłają prywatnych danych.

Workflow `zone-update-tools.yml` sprawdza te narzędzia bez budowania kolejnego APK. Osobno należy zmierzyć odczyt/zapis rzeczywistej Library i dwa kolejne buildy Unity. Pierwszy zapis Bee nie przyspieszy kompilacji, która już się zakończyła. Należy porównać następne uruchomienie o zgodnym profilu, rozmiar archiwum i koszt transferu. Żadnego konkretnego czasu nie potwierdzono.

## Drugi etap — kod i walidacja

Kod został sprawdzony na `codex/zone-content-updates-20261104`, commit `a9ca7986cb97560d16e466f8b78ef2a168f1e82b`. Ten commit dokumentacyjny towarzyszy przeniesieniu sprawdzonej implementacji na `claude/osiedle-42-poprawka` i rozpoczęciu pierwszego bazowego APK z aktualizacjami. Nie zmienia testowanego kodu.

- Addressables 2.7.6: osobna grupa dla istniejących scen oraz rozdzielone zależności. Stabilne GUID-y pochodzą z trwałego, szyfrowanego snapshotu. Content build nie uruchamia generatora całego świata ani BuildPlayer.
- Loader podłączony do `BraxStart`, `BraxObszary`, `BraxSceneTransit`, `BraxCityWorld` i `BraxSzlaban`. Obsługuje bazę w APK, katalog aktualizacji, ładowanie i zwalnianie uchwytów scen.
- Prywatny plik `.braxupdate.zip` importowany przez systemowy wybór pliku w Androidzie. Zawiera katalog i paczki różniące się od bazy. Nie wymaga publicznego hostingu ani tokenu GitHuba w grze.
- Kontrola strefy, bazy i runtime, rozmiarów i SHA-256; instalacja do nowego katalogu; aktywacja po restarcie; dziennik próby i powrót po przerwanym starcie.
- Historia poprawek scen pozostaje w repozytorium. Nowy bazowy APK uwzględnia tę historię, a kolejne content buildy pomijają operacje już zastosowane w bazie.
- Workflow content obsługuje commity `[content-update]`; pełny workflow APK je pomija. Nowy bazowy APK jest potrzebny przy zmianach kodu/pakietów.

32 testy Python transportu/cache/snapshotów przechodzą lokalnie i w CI: [run 41750728409](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41750728409).

Walidacja Unity 6000.0.58f2 zakończyła się sukcesem na obu platformach: [run 41750728494](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41750728494).

- Android: kompilacja wszystkich skryptów C#, budowa bazowych i zmienionych paczek, 14 kontroli manifestu/paczki/historii zmian oraz kompilacja Java wyboru pliku względem rzeczywistych bibliotek Unity i Androida (`BRAX_CONTENT_ANDROID_JAVA_PASS`).
- Linux: zbudowany i uruchomiony player; ładowanie bazy, import, restart ze zmienioną sceną, doczytanie i zwolnienie obszaru, potwierdzenie wersji oraz powrót po przerwanym starcie. Wszystkie tryby zwróciły `BRAX_CONTENT_RUNTIME_PASS`.
- W syntetycznym teście Androida baza miała 1029164 bajty, aktualizacja 9009 bajtów. To dowód pominięcia niezmienionych paczek w tym teście, nie pomiar rozmiaru ani czasu aktualizacji rzeczywistego osiedla.

Do potwierdzenia pozostają zakończenie pełnego bazowego APK oraz test na docelowym telefonie: instalacja, natywny wybór pliku, prawdziwa poprawka mapy, wygląd i zużycie pamięci. Build #61 używa wcześniejszej wersji i nie otrzymuje tych zmian w locie. Nie wyłączono SSO Netlify.

Instrukcja wykonawców: [CONTENT-UPDATES.md](CONTENT-UPDATES.md).

## Dla miasta i obozu

Przenieść katalog `unity-tools/updates` oraz analogiczne kroki własnego workflow. Nie kopiować całego workflow osiedla bez porównania z bieżącą gałęzią. W ich generatorze znacznik gotowego świata musi powstać dopiero po ukończeniu generowania i kontroli wszystkich zależności. Nazwy stref: `miasto`, `osiedle`, `oboz`. Przypiąć własny zgodny profil i nie mieszać paczek z różnymi bazami APK. Pełny kontrakt: `STANDARD-AKTUALIZACJI.md`.
