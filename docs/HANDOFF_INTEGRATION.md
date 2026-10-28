# Aktualizacja: grywalna postać na osiedlu

Wersja 2260795 była niepełną integracją: zapisywała postać przy przejściu, ale na osiedlu zostawiała kamerę pierwszoosobową. Użytkownik odrzucił ten rezultat. Bieżąca poprawka zastępuje podgląd działającą postacią z gry.

- Osiedle używa tego samego `Fighter`, szkieletu i animacji `chars/brax.glb.gz` / `chars/nyxa.glb.gz`, tekstur, danych bohaterów, HUD-u i obsługi dotyku `UI` co miasto.
- Wspólne skróty i menu ciosów: `dev/shared/player-controls.js`. Dostępne są ruch, chód/bieg, ciosy, specjal, skok, blok, unik, gesty i shake. Przeciwnicy, nowe misje i pojazdy nadal pozostają osobnym późniejszym etapem.
- Wybrany bohater, HP i ulepszenia, pieniądze, broń/pozostałe użycia, postęp, kamera oraz bieżąca pozycja osiedla są zachowywane. Bezpośrednie wejście korzysta z ostatnio wybranego bohatera (domyślnie Brax). Historia przeglądarki po powrocie do miasta odczytuje aktualny checkpoint.
- Kamera śledzi postać. Obrót zależy od użytkownika. Kolizje dotyczą kapsuły postaci, umeblowania, ścian i podłóg. Podwyższona podłoga komisariatu otrzymała rzeczywiste stopnie.
- Świat nie jest przebudowywany ani pobierany razem z siłownią. Statyczne cienie osiedla są liczone raz; postać ma ruchomy cień kontaktowy, bez nowych lamp i efektów poświaty.
- Osiedle buduje swój ekran z `dev/src/template.html`. Stare osobne pliki podglądu HTML/CSS usunięte. Układ świata i ładowanie modeli wydzielono do `dev/osiedle/level.js`, fizykę do `player-world.js`.
- Test pełnego przejścia Nyxą: `dev/tools/check_integration.cjs`; test Braxa, sterowania i powrotu z ulepszeniami oraz bronią: `dev/tools/check_estate_player.cjs`; trasy obu ulic, wnętrz i aren: `dev/osiedle/check.cjs` — 21/21.

Poniżej historyczny opis wcześniejszej poprawki wydajności. Opis pierwszoosobowego osiedla dotyczy wyłącznie starego wydania 2260795 i nie opisuje bieżącej poprawki.

---

# BRAXHORN CITY — siłownia i połączenie osiedla, 28.10.2026

Gałąź robocza: `codex/city-osiedle-integration`.
Połączono miasto z `bc260900e71b3d87ec32dfd71cfa53e38cf8e460` i osiedle z `codex/osiedle-stage1` (wymagania do `fba9149`). Scalanie bez konfliktów.
Status: użytkownik wyraźnie zlecił publikację do istniejącego repozytorium i gry. Wydanie zawiera kod i gotowe zasoby z 8299fad. Publikacja przez GitHub API z powodu braku uwierzytelnienia lokalnego git push. Stan produkcji potwierdza status Vercel przypisany do commitu wydania.

## Potwierdzona przyczyna kosztownego oświetlenia

W wersji `bc26090` plik `gym3.bin.gz` zawierał 1 410 606 składowych koloru, a aktualne siatki siłowni oczekiwały 1 412 784. Konsola zgłaszała `bake: vertex count changed 1410606 1412784`, a `__mg.baked()` zwracało `false`.

Przez to statyczna sala pozostawała na materiałach obliczających światło na bieżąco, a 11 lamp wewnętrznych nie było wyłączanych. Odtworzono bake dla obecnej geometrii, 470 928 wierzchołków i 73 siatek, w `public/assets/bake/gym4.bin.gz` (414 KiB). SHA-256: `f4c49466b26e063c2c5a54683f38109a453a1534ad9c99c12a1064209145d1fb`.

Po poprawce:
- `__mg.baked() === true`; 11 lokalnych lamp wnętrza wyłączonych.
- W dziennym trybie wyłączone także 2 lokalne reflektory zaułka. Zostają światło ogólne i kierunkowe dla postaci; statyczną salę oświetla zapisany bake.
- Nieprawidłowy bake blokuje start z czytelnym komunikatem; nie uruchamia kosztownego trybu zastępczego. Sprawdzono przez podmianę odpowiedzi `gym4` na stary plik `gym3`.
- Usunięto emissive z tablicy marki. Bloom nadal wyłączony; nie obniżono nastaw rozdzielczości ani MSAA względem przejętej wersji.

To jest potwierdzona usterka i jej naprawa, nie pomiar FPS na fizycznym telefonie ani dowód, że usunięto każdy możliwy problem wydajności.

## Ruch i praca w tle

`frame-time.js` dzieli czas wolnej klatki na kroki maksymalnie 1/30 s. Wcześniej `tick` przyjmował najwyżej 1/20 s z całej klatki — poniżej 20 FPS ruch spowalniał. Limit nadrobienia po długim zatrzymaniu wynosi 0,2 s.

Test postaci przez jedną sekundę przy symulowanych 60/20/10 klatkach: 4,247 / 4,362 / 4,387 m. Różnica wynika m.in. z interpolacji ruchu; nie występuje dwukrotne zwolnienie przy 10 klatkach. Kamera zachowuje kierunek.

Nie są aktualizowane ukryta postać partnera ani systemy ulicy wewnątrz hali. Nie ma zapisu stałej godziny do localStorage w każdej klatce. Podczas ładowania miasta i przejść pętla nie konkuruje o renderowanie. W trybie zwiedzania nie pobiera się z wyprzedzeniem pięciu modeli przeciwników. Licznik FPS jest ukryty podczas pauzy/ładowania i nie liczy ich jako klatek rozgrywki.

## Przejście między planszami

- Miasto: pozycja „Osiedle Wiśniowa” na mapie oraz akcja „OSIEDLE” przy bramie na wschodzie (`closed_estate`).
- Osiedle: start przy bramie południowej, `x=0,z=92`, zwiedzanie całej istniejącej planszy. Przycisk powrotu przy bramie oraz w mapie.
- Powrót: miasto przy bramie `x=100,z=34.5`, bez ponownego prologu i wyboru bohatera.
- Przechowywane w sesji karty: bohater, HP, pieniądze, stan bossów/krypty, liczba pokonanych, broń i jej użycia oraz kamera. Ulepszenia i preferencje pozostają we wspólnym localStorage.
- Aktywna walka lub niesiona dostawa blokuje przejście, aby nie zgubić jej bieżącego stanu. Niepodniesiona walizka nie blokuje zwiedzania.
- Po udanym powrocie checkpoint jest zużywany, aby odświeżenie nie odtwarzało starszych pieniędzy/HP. Obsłużono powrót z pamięci historii przeglądarki i odtworzenie wyrzuconej strony.

Poprawiono też wyzwalanie starej walki ulicznej po powrocie: boss uruchamia się tylko w pobliżu swojego miejsca, a spóźnione ładowanie przeciwnika nie otwiera walki po zmianie lokacji.

To dwie osobno ładowane plansze, nie jedna stale załadowana scena. Osiedle pozostaje pierwszoosobowym trybem zwiedzania architektury; model bohatera, walki, NPC i misje w tej planszy nie zostały dodane. Jest to istniejący sposób oglądania osiedla, a nie ukończona rozgrywka dzielnicy.

## Weryfikacja

- `node dev/tools/check_integration.cjs`: 19/19 kontroli, zero błędów JavaScript i brakujących zasobów gry. Rzeczywiste przejście UI przez bramę, pełne ładowanie osiedla, powrót i ponowne wejście do siłowni; Nyxa, pieniądze i postęp zachowane.
- `OS_FAST=1 node dev/osiedle/check.cjs`: 21/21 tras, w tym wejścia/powroty z kasyna, dyskoteki, komisariatu, szpitala i aren. Brama centrum nadal stanowi widoczną granicę sceny — przejście uruchamia przycisk.
- `node dev/tools/check_resume.cjs`: odtworzenie Braxa oraz kontrola, że odświeżenie nie odtwarza zużytego checkpointu. Wynik w `dev/review/integration/resume.json`.
- `node dev/tools/check_stale_bake.cjs` / `dev/review/integration/stale-bake.json`: sprawdzony zakaz kosztownego trybu zastępczego przy niezgodnym bake.
- Obejrzane rzeczywiste zrzuty 915×412 z emulacją dotyku: `gym.png`, `city-gate.png`, `estate.png`, `park.png`, `returned-city.png`, `returned-gym.png`.
- `before.json`/`after.json` pochodzą z porównania kodu siłowni w Chromium/SwiftShader. Ich czasy nie są wynikiem iPhone'a i nie należy obiecywać na ich podstawie konkretnego FPS.

Build: `ESBUILD=<ścieżka-do-esbuild> python3 dev/tools/build_quick.py`, następnie analogicznie `python3 dev/osiedle/build.py`. Nie zmieniono konfiguracji wdrożenia. Nie dodano aut/NPC/misji do osiedla ani nowych efektów świetlnych.

## Format wydania przez GitHub API

Pojedyncze żądanie połączenia GitHub ma limit 16 MiB. Duże modele są zapisane bez zmian jako części po 9 MiB w `dev/deploy/parts/`. `node dev/tools/assemble-assets.cjs` odtwarza oryginalne ścieżki i weryfikuje długość oraz SHA każdego modelu przed publikacją. Polecenie jest ustawione jako buildCommand w vercel.json. Zawartość modeli jest identyczna z przetestowaną lokalnie.

Oryginalne archiwa ZIP/BLEND/HDR z dev/source-assets nie zostały wysłane przez tę ścieżkę; pozostają w lokalnej historii z LFS. Wszystkie zasoby wymagane do działania gry są w wydaniu. Część dodatkowych obrazów przeglądu może być uzupełniona po uruchomieniu produkcji.

Publikacja potwierdzona: `main` = `22607951f6cd92f47fdbcb108a4a62fa1c305c89`, Vercel status success, produkcja `https://braxhorn-city.vercel.app`, osiedle `/osiedle/`. Wdrożenie Vercel: B3k5rGhuf7AcSNUrHLDhcDr1pauw. Lokalna historia zachowuje oryginalne duże modele i archiwa; commit wydania na GitHub ma osobną historię od bc26090 i modele transportowane w częściach. Nie nadpisywać main lokalną gałęzią przez force-push.
