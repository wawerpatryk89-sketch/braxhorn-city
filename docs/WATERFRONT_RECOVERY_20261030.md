# BRAXHORN CITY — Recovery / niezakończony remont trasy gym → morze
Data: 2026-10-30. **To jest rejestr odtworzenia, nie raport ukończenia sceny.**

## Dlaczego powstał ten zapis
Użytkownik polecił odzyskać prace ChatGPT Work po zawieszeniu i kontynuować od wyjścia z siłowni w prawo do morza. Zrzut ekranu poprzedniego Worka wymienia lokalny commit `4630b32` (deklarowane 4 jachty / 2 skutery), którego **GitHub nie rozpoznaje**. Nie wolno uznać tych obiektów za dostępne w repozytorium ani twierdzić, że zostały uratowane.

## Punkty odniesienia — fakty z GitHub
- Repo: `wawerpatryk89-sketch/braxhorn-city`.
- Zastana zdalna gałąź: `codex/city-waterfront-premium-20261030`.
- Zweryfikowany zdalny HEAD zastany w chwili przejęcia: `43460ea9955cff693cf44186273abe6fd4a59b25` (naprawa importu panoramy nieba jako Texture2D).
- Tamtejszy rejestr `unity-city/Docs/AI/WaterfrontTakeover.json` pokazuje **source-only**: podmianę nieba na materiał biblioteki i rzeczywistą teksturę nawierzchni tarasu. Reszta obszaru pozostała do wykonania.
- Zamknięty jako błędny pierwszy build `6f478...`: Unity Editor `City cloud sky did not import` — brak grywalnego wyniku.
- Następny build dla `43460ea...` przeszedł Unity compile + weryfikację binariów. Jednocześnie kontrola zdjęć i przejścia nie była jeszcze zakończona w chwili sporządzenia dokumentu.
- `main` pozostaje poza zakresem zmian. Zabronione: merge, publikacja LIVE, edycja scen wnętrza siłowni prowadzonej przez osobnego wykonawcę, naruszanie oryginalnych plików źródłowych.

## Odzysk i rzeczywiste zmiany
- Z tej zdalnej bazy utworzono oddzielną gałąź **`codex/waterfront-recovery-20261030`**.
- Commit **`83d7fd8b19eecf590e72344a7db4f051083729e8`**: w `BraxCityGround.cs` usunięto nakładanie się powierzchni chodników z krawężnikami w z=23.755–23.995 oraz z=33.005–33.245 (wcześniej powierzchnie na identycznym y=.15). Zachowano drogę, drzwi, istniejącą geometrię ramp i sterowanie. To **naprawa konkretnego potencjalnego źródła z-fightingu w źródłach**, bez wizualnego PASS do czasu testu.
- Commit **`62d5484d8050c59c4fcbcc4b4111c41d0d2d29ed`**: workflow `city-full-functional-access.yml` obejmuje odrębną gałąź recovery; uruchamia prawdziwy Unity WebGL build, weryfikację playera i zdjęcia/test drogi po kompilacji. Nie publikuje produkcji.
- Wynik kontrolnego workflow `https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41013639868` jest odrębny od źródłowych zmian i nie może być przypisany jako PASS przed zakończeniem i obejrzeniem artefaktów.
- Zapis sprzed odzyskania: `https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41008598585`.

## Zlecony zakres — nadal bez akceptacji
1. BRAXHORN NUTRITION — realistyczne fizyczne 3D regały, suplementy i shakery, lada, witryna, wejście.
2. Fasady wszystkich budynków na przejściu gym → morze: materiały PBR, detale i zlikwidowanie wszelkich migotań.
3. NOVA / dyskoteka — wyposażony lokal, światło, parkiet i muzyka działająca zgodnie z zasadami audio gry.
4. Wieżowiec — sprawdzone wejście, lobby, kabina, jazda na dach i z powrotem, taras Premium.
5. Nabrzeże — mały port, jachty, skutery wodne, prawdziwy pomost i ulepszone molo.
6. Plaża — rzeczywiste tekstury suchego/mokrego piasku, ukształtowanie, budka ratownicza, przejścia terenów.
7. Woda — sensowne pozycje i ruch obiektów morskich, ewentualny odległy statek.
8. Daleka wyspa — skala, pozycja i zgodność generatora z Unity 6 Built-in.
9. Szkoła / boiska — funkcjonalne boiska, ping-pong i otoczenie jeśli zgodne modele istnieją.
10. Trawa, krawędzie, przejścia i usunięcie pustych/surowych powierzchni.

## Zweryfikowane lokalizacje plików w bibliotece użytkownika
**Biblioteka ChatGPT; poniższe pliki NIE są automatycznie dostępne w repozytorium / runnerze Unity:**
- `/Braxhorn city/Asset Library/3D Models/Watercraft/`: `super-yacht02.glb`, `frickies_yacht.glb`, `yacht.glb`, `jetski_collection.glb`.
- `/Braxhorn city/Asset Library/3D Models/Port/Docks/rustic_wooden_dock.glb`.
- `/Braxhorn city/Asset Library/Textures/CoastSand/coast_sand_02_4k.blend.zip`.
- `/Braxhorn city/Asset Library/Textures/DampSand/damp_sand_4k.blend.zip`.
- `/Braxhorn city/Asset Library/3D Models/Coast/Vacation Island - GitHub/vacation-island-unity-main.zip`.
- `/Braxhorn city/Asset Library/3D Models/Entertainment/Nightclub/`: `disco_ball_with_colored_lights.glb`, `dj_mixer.glb`.
- `/Braxhorn city/Asset Library/3D Models/Stores/BRAXHORN_NUTRITION/Shelving/Steel_Frame_Shelves_01/steel_frame_shelves_01_4k.blend.zip`.
- `/Braxhorn city/Asset Library/3D Models/Sports/football_field.glb`.
Prawa/licencje i możliwość importu oraz osadzenia mają być sprawdzone PRZED dystrybucją. Nie podstawiać plików zdjęć na półkach jako brył opakowań. Jeżeli wymagany model jest niedostępny w źródle Unity, **zgłosić brak; nie wymyślać modelu i nie zaznaczać DONE**.

## Reguła odbioru
Rzeczywiste screeny gotowej uruchomionej gry przy około 15/30/45/60/75/90/100% wizualnego zakresu; spacer sterowaną postacią od siłowni do morza i z powrotem, wejścia do każdego z wymaganych wnętrz oraz przejazd windą. Konieczne osobne testy migotania ścian, krawężników, morza i nabrzeża, telefon iPhone/Android, brak teleportów, brak utraty istniejącego sterowania. Nie ogłaszać PASS tylko na podstawie commita, `BUILD SUCCEEDED`, obrazu z edytora ani planu w PDF. Finalny PASS wystawia użytkownik.
