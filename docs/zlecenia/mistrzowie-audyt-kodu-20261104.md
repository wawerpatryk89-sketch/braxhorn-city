# Miasto Mistrzów — rozszerzony audyt kodu przed wysyłką

Data: 2026-11-04. Baza przeglądu: `268f76da71be334058ac646b8672174325e9c66e`, gałąź `codex/miasto-mistrzow-20261104`.

## Werdykt

**Nie zatwierdzam jeszcze projektu jako bezbłędnego ani gotowego do wydania.** Wykonano przegląd statyczny dedykowanego kodu regionu i wybranych ścieżek wspólnych, testy narzędzi oraz kontrole wygenerowanej geometrii. Brakuje wykonania poprawionego generatora w Unity, kompilacji Android i testu urządzenia. Tych braków nie zastępuje czytanie C#.

Audyt wykonano bez uruchamiania workflow, buildu i publikacji APK. Po audycie właściciel polecił wysłać poprawki: kod i raport zapisano na gałęzi codex/miasto-mistrzow-20261104. Dwie wcześniejsze poprawki C# były już zapisane w bazowym commicie 268f76d.

## Potwierdzone problemy i stan napraw

| Priorytet | Problem i dowód | Stan |
|---|---|---|
| Wysoki, blokada buildu | Run #2 zatrzymał się na NewScene Additive przy niezapisanej scenie: InvalidOperationException w Foundation.Generate. | Wcześniejsza poprawka 268f76d wykorzystuje pustą scenę i chroni niezapisane sceny użytkownika. Wymaga wykonania w Unity. |
| Wysoki, kolejna blokada walidatora | Prosta trasa wejście–oktagon przecinała wysoki tył schodów trybun. Analitycznie 171 trafień próbka–bryła; trasa boczną alejką: 0 dla tych brył. | Wcześniejsza poprawka 268f76d zmienia wyłącznie trasę testu. Pełne Physics sceny nadal niepotwierdzone. |
| Średni, instalacja aktualizacji | Oba workflow używały run_number*100+attempt, ale mają osobne liczniki i ten sam applicationId. Przełączenie workflow może obniżyć versionCode. | Poprawka apk_version.py: wspólny zegar sekundowy od 2020 + 100000000, zakres kontrolowany; oba workflow używają jednej grupy concurrency. Test granic i wzrostu PASS. |
| Średni, fałszywe potwierdzenie dostawy | APK Actions potwierdzano przez rozmiar ZIP i SHA commita workflow; nie sprawdzano bajtów APK ani numeru przebiegu. | Poprawka: pobranie ZIP do pliku, sprawdzenie runId, dokładnie jednego oczekiwanego APK, długości i SHA-256. Release pozostaje niezależną drogą. Testy atrap: zmieniony APK tej samej długości, brak APK i obcy run odrzucane. |
| Wysoki przy ponownym przygotowaniu zasobów | restore_player usuwał cały katalog patryk z .meta i kopiował model przed walidacją tekstur. Brak map zostawiał zmienione dane i usuwał GUID-y. | Poprawka: najpierw weryfikacja wszystkich źródeł i kodowanie ORM; potem aktualizacja stałych nazw bez usuwania katalogu/.meta. Nowy test reprodukował błąd przed zmianą i przechodzi po zmianie. To nie jest transakcja odporna na awarię dysku podczas zapisu. |

Dodatkowo drugi workflow dostał jawne sparse-checkout-cone-mode: false, zgodnie z pierwszym: lista checkout zawiera konkretne pliki, nie tylko katalogi. Nie oznaczam tego jako przyczyny poprzedniej awarii.

Poprawki nie zmieniają modelu Patryka, tekstur, rozdzielczości, wyglądu, brył ani układu miasta.

## Dlaczego dotychczasowe buildy padały

[Run #1](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41885950373): błąd wyboru kanału PBR (KeyError: rough). Obsługa nazw dostawcy Rough/rough i wariantu fabric col_01 była już poprawiona przed obecną bazą.

[Run #2](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41889411877): przygotowanie źródeł i kompilacja skryptów przeszły, następnie generator próbował otworzyć scenę addytywnie przy niezapisanej scenie. Nie powstał APK; artefakt diagnostyczny nie był grą.

To dwa konkretne błędy integracji. Nowe testy geometrii i narzędzi nie dowodziły poprawności cyklu życia scen Unity, a awaria zatrzymała wykonanie przed metrem, budynkami i Patrykiem. Nie ma dowodu, że każdy build autorstwa GPT pada albo każdy autorstwa Claude działa. W tym regionie trzeba oceniać konkretne zmiany i wyniki uruchomienia.

## Zakres przeglądu

Dedykowany kod regionu: 11 plików C#, 1 shader, 20 pierwotnych plików Python, 2 nowe pliki Python, 2 workflow oraz dane wejściowe JSON. Przegląd statyczny nie jest kompilacją.

| Obszar | Pliki / sprawdzone zależności |
|---|---|
| Tworzenie i zapis scen | BraxChampionsFoundation.cs, BraxChampionsBuild.cs: start, ochrona istniejących scen, kolejność generatorów, ponowne otwarcie, walidacja i eksport |
| Infrastruktura | BraxChampionsInfrastructure.cs, BraxClaudeMetro.Reuse.cs: współrzędne stacji, przeniesienie napędów, liczba bramek/schodów, światła i culling |
| Budynki i arena | BraxChampionsBuildings.cs, BraxChampionsArena.cs: LOD, stałe kolizje, zapis prefabów, wejście, trybuny, podest, rola materiałów |
| Postać | BraxChampionsPatryk.cs, BraxChampionsPatrykHumanoid.cs, BraxChampionsPatrykImport.cs: źródło modelu, wysokość, Humanoid/Legacy, retarget, mapy i klipy |
| Eksploracja | BraxChampionsExplorer.cs, BraxChampionsDesktopInput.cs, ChampionsPatryk.shader: referencje gracza, kamera, wejście, 5 punktów, shader normal/ORM |
| Geometria Python | legacy_foundation.py, layout.py, meshkit.py, build_infrastructure.py, build_exteriors.py, build_signs.py, validate_contract.py |
| Źródła i podgląd | prepare_assets.py, restore_private.py, restore_player.py, prepare_signage.py, prepare_arena.py, draw_map.py |
| Dostawa | apk_delivery.py, package_output.py, verify_delivery.py, apk_version.py i oba workflow |
| Testy | test_prepare_assets.py, test_surface_geometry.py, test_restore_player.py, test_apk_delivery.py, test_apk_version.py |
| Dane | plan_inventory.json, verified_textures.json, arena-source.json, player-source.json, Packages/manifest.json |
| Wspólny kod — ścieżki używane przez region | BraxGymPlayer, BraxCityWorld, BraxTouchControls, BraxStart, BraxMiejscaTestowe, BraxMenuGry, BraxGrafika, BraxClaudeKamera, culling wnętrz, drzwi i schody; w generatorach BraxWnetrzaKit, BraxClaudeMetro, BraxZapisAssetu, źródła materiałów/prywatnych paczek |

To nie jest pełny audyt wszystkich przechodnich zależności całego repozytorium BRAXHORN: nie wszystkie skrypty innych regionów, globalne bootstrappy i zasoby binarne zostały wykonane lub sprawdzone w grze.

## Wykonane kontrole

- **30/30 testów Python PASS** po poprawkach. Dostawa i certyfikaty są testowane z atrapami narzędzi/usług; nie jest to rzeczywisty podpis ani zdalny upload.
- 22 pliki Python: kontrola składni PASS.
- Oba YAML: parsowanie PASS; wszystkie 15 sekcji run: bash -n PASS. To nie symulacja GitHub Actions.
- Wcześniejsze wykonane generatory: Foundation 100 siatek / 95668 trójkątów; infrastruktura 278 siatek / 715616 trójkątów; kontrakt metra PASS.
- Budynki A1 i T01: granice działek, wysokości, poprawność siatek i redukcja LOD PASS. Szyldy: 2 zamknięte bryły znaków, 5 gwiazd hotelu, testy geometrii PASS.
- 677 próbek podłoża dojść/startu na rzeczywistych wygenerowanych trójkątach: PASS; nie zastępują kapsuły Unity.
- Przygotowanie grafik areny wykonane. Przygotowanie znaków stacji lokalnie ograniczone brakiem binarnego atlasu w diagnostycznym checkout; poprzedni CI przeszedł ten etap.
- W środowisku nie ma Unity, dotnet, csc ani mcs. Zmieniony C# nie był tutaj kompilowany. Nie wykonano PlayMode, IL2CPP/Gradle, instalacji ani testu telefonu.

Szczegóły wcześniejszych kontroli i awarii: mistrzowie-audyt-build-20261104.md.

## Nadal otwarte, bez udawania pełnego sukcesu

1. **Unity/Android — wysoki priorytet, wynik nieznany.** Trzeba sprawdzić cały generator na prywatnych źródłach: metro, Physics, zapis i odczyt referencji, retarget Patryka, kompilację APK. Nieudany run #2 nie osiągnął tych etapów.
2. **Animacje — średni priorytet, ograniczenie walidacji.** Kontrola kości poza twardą bramką lokomocji może zapisywać ostrzeżenia zamiast blokować build. Test ruchu względem pozy spoczynkowej nie dowodzi, że każda animacja rzeczywiście zmienia się w czasie. Konieczny PlayMode: chód, bieg, skok, cios, kopnięcie, garda i przejścia.
3. **Pauza na schodach ruchomych — średni priorytet, kod współdzielony.** Pauza gracza zatrzymuje jego Update, ale BraxSchodyRuchomeJazda nadal wywołuje CharacterController.Move bez sprawdzania pauzy. To statycznie widoczny problem wymagający testu i celowanej poprawki wspólnego zachowania; nie zmieniano teraz innych regionów.
4. **Zasoby i wydajność — niezmierzone.** Brak pomiaru pamięci GPU/RAM, FPS, czasu startu i temperatury na urządzeniu. Sam brak błędów siatki nie dowodzi, że duża scena jest wydajna.
5. **Powtarzalność środowiska.** W drzewie unity-city/ProjectSettings zapisany jest tylko ProjectVersion.txt, brak packages-lock.json. Część ustawień powstaje z domyślnych Unity i kodu generatora; dokładna reprodukcja wymaga utrwalenia rzeczywistych ustawień z działającego projektu.
6. **Pozostała dostawa regionu.** verify_delivery.py dla zaszyfrowanych bundles sprawdza lokalne sumy i zdalne metadane, nie pobiera części dla niezależnej kontroli bajtów. Zawiera też nieaktualny tekst o contents: read. Nowa kontrola APK nie obejmuje tej oddzielnej ścieżki.
7. **Przygotowanie animacji.** copy_animations nie usuwa starych opcjonalnych klipów z ponownie użytego katalogu. Świeże CI nie ma tego stanu; ręczne przygotowanie powinno używać czystego katalogu albo jawnego manifestu aktualizacji.
8. **Pochodzenie danych.** Foundation.json zapisuje GITHUB_SHA; przy launcherze z main jest to SHA workflow, a nie gry. Główny raport Unity i manifest APK korzystają już z osobnego potwierdzenia źródła. Metadane Foundation pozostają do ujednolicenia.
9. **Zakres ukończenia miasta.** W kodzie ukończone są wybrane elementy podstawy, infrastruktury oraz A1/T01. Pozostałe działki to rezerwy; hotel nie ma gotowego wnętrza, ruch pojazdów jest wyłączony, pełna rzeźba byka wymaga źródła. 80 miejsc mariny nie oznacza 80 wstawionych jednostek. Generator eksploracji nie dodaje komponentu mapy regionu. Nie wolno przedstawiać tego jako ukończonego całego miasta.

## Warunki dalszego odbioru

Poprawiony kod musi jeszcze przejść pełny Unity/Android oraz sprawdzenie na urządzeniu. Wysłanie kodu nie jest wynikiem tego sprawdzenia. Przejście buildu potwierdzi budowalność, nie automatycznie wygląd, płynność ani pełną realizację planu.

Nowe poprawki są w mistrzowie-poprawki-audytu.patch względem 268f76d. Kod i raport przekazano do repozytorium na polecenie właściciela. Workflow z main nie zostanie automatycznie naprawiony przez zmianę plików na gałęzi gry; przyszłe uruchomienie musi używać właściwego workflow i SHA. Nie uruchamiano go w tym audycie.
