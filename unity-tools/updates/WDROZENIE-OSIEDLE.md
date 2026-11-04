# Wdrożenie w osiedlu — stan 2026-11-04

Baza: `5b9e7afb2d487ff0b5c92d79f90fffb97f70f99c`, gałąź `claude/osiedle-42-poprawka`. Zmiany dotyczą wyłącznie procesu budowania i dokumentacji. Nie modyfikują wyglądu ani działania stref. Build #61 pracuje ze swoją pierwotną wersją i nie otrzymuje tych zmian w locie.

## Wykonane

1. Pełna szyfrowana Library: zachowane Bee, IL2CPP, PlayerDataCache oraz metadane importowanych źródeł. Pliki modeli i klucz podpisu nie są oddzielnie dołączane do archiwum. Library może zawierać przetworzone prywatne zasoby, dlatego archiwum pozostaje szyfrowane.
2. Profil cache obejmuje strefę, wersję Unity, manifest pakietów i wariant Androida. Zmiana zwykłego skryptu nie wyrzuca całej Library; Unity sam sprawdza zależności. Cache nie oznacza, że scena odpowiada nowym źródłom — generator nadal stosuje osobny dokładny klucz.
3. Odczyt: cache Actions v2 → zaszyfrowana kopia w dedykowanym draft release → wcześniejszy cache v1 do migracji → pełny import z ostrzeżeniem. Rozpakowanie odbywa się do osobnego katalogu. Zła konfiguracja, dowiązania i ścieżki wychodzące poza projekt są odrzucane. Metadane ze źródeł mają pierwszeństwo.
4. Zapis trwały: szyfrowane części do 1,5 GiB, weryfikacja SHA-256, indeks zmieniany po przesłaniu wszystkich części. Dwie ostatnie kopie, oddzielne dla gałęzi i profilu. Narzędzie odmawia zapisu do publicznego wydania i usuwa wyłącznie starsze części tego dedykowanego cache. To nie jest CDN gry.
5. Znacznik zakończonego generowania świata powstaje przed BuildPlayer. CI kontroluje klucz, platformę, sceny, zależności i `.meta`, po czym może zachować ten świat także po późniejszym błędzie IL2CPP/Gradle. Nie zapisuje niekompletnego checkpointu i nie oznacza go jako działający APK.
6. Kolejne buildy jednej gałęzi nie anulują trwającego buildu. GitHub może zastąpić starsze oczekujące uruchomienie nowszym; bieżące pozostaje w toku. Podsumowanie pokazuje realne przywrócenie i zapis cache.

## Sprawdzenie

`python3 -W error::ResourceWarning -m unittest discover -s unity-tools/updates -p 'test_*.py' -v`

25 testów: pełny cykl szyfrowania/odczytu, Bee i PlayerDataCache, zachowanie GUID źródeł, zgodność profili, integralność, niebezpieczne ścieżki i dowiązania, przerwany upload, poprzednia poprawna kopia, dwie wersje cache, odrzucenie publicznego release i niekompletnego świata. Operacje GitHub w testach trwałego zapisu są symulowane; testy nie przesyłają prywatnych danych.

Workflow `zone-update-tools.yml` sprawdza te narzędzia bez budowania kolejnego APK. Osobno należy zmierzyć odczyt/zapis rzeczywistej Library i dwa kolejne buildy Unity. Pierwszy zapis Bee nie przyspieszy kompilacji, która już się zakończyła. Należy porównać następne uruchomienie o zgodnym profilu, rozmiar archiwum i koszt transferu. Żadnego konkretnego czasu nie potwierdzono.

## Dalszy etap — jeszcze nie wdrożony

- Podział generatora osiedla na trwałe sektory o stabilnych identyfikatorach i jawnych zależnościach. Obecnie zmiana źródeł nadal może wymagać wygenerowania całego osiedla.
- Addressables, katalogi i content state, bazowy APK oraz małe aktualizacje zawartości. Nie dodano jeszcze pakietu Addressables ani nie zmieniono loadera gry.
- Wpięcie loadera w `BraxStart`, `BraxObszary`, `BraxSceneTransit`, sprawdzanie dostępności scen w `BraxCityWorld` i `BraxSzlaban`; zachowanie startowego UI i zwalniania pamięci.
- Uzgodnienie dostępu do HTTPS hostingu paczek. Wykryty projekt Netlify `braxhorn-25d-3d-test-20261027` wymaga SSO dla wszystkich wdrożeń. Nie wyłączono ochrony, nie utworzono publicznego CDN. Draft release GitHuba nie jest alternatywą dla anonimowego pobierania w aplikacji; nie wolno wbudowywać tokenu GitHuba w APK.
- Test na telefonie: baza → jedna zmiana → pobranie zmienionych paczek bez nowego APK; awaria sieci, wycofanie i zużycie RAM. Obecne APK nadal zawiera całe osiedle.

## Dla miasta i obozu

Przenieść katalog `unity-tools/updates` oraz analogiczne kroki własnego workflow. Nie kopiować całego workflow osiedla bez porównania z bieżącą gałęzią. W ich generatorze znacznik gotowego świata musi powstać dopiero po ukończeniu generowania i kontroli wszystkich zależności. Nazwy stref: `miasto`, `osiedle`, `oboz`. Przypiąć własny zgodny profil i nie mieszać paczek z różnymi bazami APK. Pełny kontrakt: `STANDARD-AKTUALIZACJI.md`.
