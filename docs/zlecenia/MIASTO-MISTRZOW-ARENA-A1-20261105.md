# BRAXHORN CHAMPION ARENA A1 — etap wejścia, VIP i otoczenia (5.11.2026)

**Gałąź:** `gpt/miasto-mistrzow-arena-premium-20261105` (pochodna `codex/miasto-mistrzow-20261104`). **Silnik:** Unity 6, projekt pod `unity-city`. Bez zmian w innych regionach i bez automatycznego buildu.

## Zakres, stan kodu
- **A1:** istniejąca hala z oktagonem i sędziami została zachowana; układ widowni rozdzielony w miejscach przejść; schody na sektory po bokach, wolny korytarz wejściowy.
- **VIP przy sędziach:** 16 dużych foteli wykończonych PBR Leather037 i metalem Metal048A, 2 rzędy w dwóch grupach. Stanowiska sędziów i komentatorów oraz ich wyposażenie pozostawione.
- **Oświetlenie:** 4 istniejące punkty nad oktagonem przeniesione do odrębnej nazwy i zwiększone do zasięgu 13 m, intensywności 2.15, z oprawami LED i materiałem emisyjnym. Brak nowych shadow maps; istniejące lampy siedzeń bez zmian. Ocena jasności na telefonie oczekuje na APK.
- **Elewacja:** `facade_glass` jest teraz rzeczywiście przezroczystym, niemetalicznym materiałem CityStaticPBR; `clear_glass` służy drzwiom i gablocie. Materiały i ich odbicia do weryfikacji na Androidzie.
- **Wejścia:** na froncie drzwi dwuskrzydłowe publiczne, na północy osobne wejście zawodników. Środkowe otwory mają dokładnie 9,4 m niezależnie od LOD. Osobny korytarz prowadzi do zaplecza, dwa pomieszczenia z ławkami i szafkami. Drzwi korzystają z istniejącego `BraxCityDoor`.
- **Otoczenie:** dwa utwardzone parkingi z rozdzielonymi rzędami stanowisk, odbojami, liniami, krawężnikami, dojściami pieszymi i drogami wjazdowymi. Koniec dojazdów przylega do północnej krawędzi drogi z=650 (granica nominalna z=667). Oś wejścia i istniejący dojazd z miasta pozostają drożne. Duże powierzchnie mają UV w metrach, aby asfalt i kostka Poly Haven nie rozciągały się na kilkadziesiąt metrów.
- **Wejście premium:** czerwony dywan, chromowane słupki z miękką czerwoną liną i ciepłe lampy w holu.
- **Ekspozycja pasa:** przeszklona gablota w holu przy drodze na widownię. Ładowanie oryginalnego FBX i map 4K, z Androidowym limitem 2K/ASTC.
- **Szyldy:** cztery wersje `CHAMPION ARENA`, rozmieszczane według ich oryginalnego projektu na zakrzywionej elewacji, bez podmieniania typografii. Przy braku źródeł w repo zostają istniejące litery z generatora (nie fałszywe nowe logo).

## Zasoby: osobny pakiet binarny NIE JEST w tej gałęzi
Pakiet `champions_asset_import.zip` jest przygotowany na podstawie materiałów użytkownika, ale GitHub Connector zapisuje tu tekst, nie duże pliki FBX/PNG. **Nie zakładać**, że pakiet trafił na GitHub.

- Archiwum SHA256: `2ffe2c891d505985691292714c7890a36d190c6a51da08e6835e6aef04751f75` (91 MB).
- Zawartość w ZIP ma ścieżki od `Assets/Source/Mistrzowie/`; należy umieścić je pod `unity-city/` w projekcie Unity **przed** pakowaniem projektu. Prawdziwe źródło FBX: `Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx`; obok `.fbm/` z Color/Normal/Metallic/Roughness oraz `ChampionshipBelt_ARM.png`.
- `ArenaSigns/`: `champion_arena_red_front.png`, `champion_arena_gold_front.png`, `champion_arena_red_fight_night.png`, `champion_arena_gold_arena_walk.png`. Powstały przez wycięcie czterech oryginalnych wzorów z dwóch arkuszy użytkownika i usunięcie białego tła (bez modyfikowania napisów).
- Wysokiej jakości oryginalne mapy pasa (4096×4096) zachowane, import Android ASTC do 2048. FBX ma w surowych danych 16 578 pozycji wierzchołków i 16 538 wielokątów (około 33 023 trójkątów po triangulacji), przed oceną importu przez Unity.

## Statyczne kontrole wykonane
1. Punkty frontowych i tylnych otworów: od x=−4,70 do +4,70 m dla obu LOD n=64 i n=128, sprawdzone algorytmicznie.
2. VIP: 16 foteli, odstęp poziomy środek–środek 1,35 m; prześwit osi korytarza między strefami około 6,30 m po odjęciu gabarytów foteli; dwa rzędy rozdzielone o 1,75 m.
3. Parking i dojścia: zachowanie granicy drogi z=667, brak wejścia nawierzchni pieszej w pas drogi przy opisanych współrzędnych.
4. Zbilansowane nawiasy i klamry w odczytanych plikach C#; **to nie jest kompilacja**.
5. Testy do CI: `unity-tools/mistrzowie/test_arena_finish.py` — wymagają uruchomienia i nie są deklarowane jako wykonane w Unity.

## Nadal do potwierdzenia
- Import i referencje binarnych zasobów w faktycznym repo/runnerze.
- Kompilacja C# oraz Python z pełną strukturą repo (nie ma lokalnego sklonowanego checkoutu).
- Poprawność materiałów, geometrii, drzwi, sterowania i kolizji w Unity Play Mode.
- Wizualne i wydajnościowe działanie APK na telefonie, odczyt błędów, FPS i pamięci.
- Ostateczna kontrola dojścia do obu pięter trybun oraz drzwi wewnętrznych.
- Pozostałe 31 rezerwacji budynków w planie nie jest ukończoną zabudową; A2 jako druga arena nie była przedmiotem tej naprawy.

**Tryb budowania:** zgodnie z `AGENTS.md` nie uruchamiać APK, Addressables, testów Unity ani ponowień bez osobnego polecenia właściciela. Zmiana gałęzi GitHub nie jest dowodem, że kod przeszedł na Androidzie. Nie scalać `main`.
