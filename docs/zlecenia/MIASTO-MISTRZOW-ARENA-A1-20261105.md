# BRAXHORN CHAMPION ARENA A1 — etap wejścia, VIP i otoczenia (5.11.2026)

**Gałąź:** `gpt/miasto-mistrzow-arena-premium-20261105` (pochodna `codex/miasto-mistrzow-20261104`). **Silnik:** Unity 6, projekt pod `unity-city`. Bez zmian w innych regionach i bez automatycznego buildu.

## Zakres, stan kodu
- **A1:** istniejąca hala z oktagonem i sędziami została zachowana; układ widowni rozdzielony w miejscach przejść; schody na sektory po bokach, wolny korytarz wejściowy.
- **VIP przy sędziach:** 16 miejsc w dwóch rzędach i dwóch grupach. Moduł `BraxChampionsVIPSeats` używa docelowego fotela właściciela `free_luxurious_barrel_seat_2k.glb`, jeśli jest obecny; bez tego pliku nie wolno budować areny jako kompletnej; usunięto wszystkie fotele-zamienniki. Stanowiska sędziów i komentatorów oraz ich wyposażenie pozostawione.
- **Oświetlenie:** 4 istniejące punkty nad oktagonem przeniesione do odrębnej nazwy i zwiększone do zasięgu 13 m, intensywności 2.15, z oprawami LED i materiałem emisyjnym. Brak nowych shadow maps; istniejące lampy siedzeń bez zmian. Ocena jasności na telefonie oczekuje na APK.
- **Elewacja:** `facade_glass` jest teraz rzeczywiście przezroczystym, niemetalicznym materiałem CityStaticPBR; `clear_glass` służy drzwiom i gablocie. Materiały i ich odbicia do weryfikacji na Androidzie.
- **Wejścia:** na froncie drzwi dwuskrzydłowe publiczne, na północy osobne wejście zawodników. Środkowe otwory mają dokładnie 9,4 m niezależnie od LOD. Osobny korytarz prowadzi do zaplecza, dwa pomieszczenia z ławkami i szafkami. Drzwi korzystają z istniejącego `BraxCityDoor`.
- **Otoczenie:** dwa utwardzone parkingi z rozdzielonymi rzędami stanowisk, odbojami, liniami, krawężnikami, dojściami pieszymi i drogami wjazdowymi. Koniec dojazdów przylega do północnej krawędzi drogi z=650 (granica nominalna z=667). Oś wejścia i istniejący dojazd z miasta pozostają drożne. Duże powierzchnie mają UV w metrach, aby asfalt i kostka Poly Haven nie rozciągały się na kilkadziesiąt metrów.
- **Wejście premium:** czerwony dywan, chromowane słupki z miękką czerwoną liną i ciepłe lampy w holu.
- **Ekspozycja pasa:** przeszklona gablota w holu przy drodze na widownię. Ładowanie oryginalnego FBX i map 4K, z Androidowym limitem 2K/ASTC.
- **Szyldy:** cztery wersje `CHAMPION ARENA`, rozmieszczane według ich oryginalnego projektu na zakrzywionej elewacji, bez podmieniania typografii. Przy braku źródeł w repo zostają istniejące litery z generatora (nie fałszywe nowe logo).

## Ostateczny pakiet źródeł do Unity — lokalnie gotowy, binaria nadal nie w repo
Wygenerowany na podstawie oryginalnych plików właściciela `champions_arena_A1_unity_ready.zip`:
- dokładny rozmiar `107 780 275` bajtów,
- SHA-256 `c96e39cd218b2f884560be0e89d0f24b378e7a49512486830643f6934cdc4f71`,
- **11 oryginalnych zasobów**: pas FBX + PBR, 4 szyldy, fotel VIP GLB z 2K PBR,
- **15 plików .meta** z deterministycznymi GUID (w tym metadane czterech folderów),
- manifest i instrukcja importu; razem **28 pozycji**.

Po rozpakowaniu **zachować ścieżki i .meta** pod `unity-city/Assets/Source/Mistrzowie/`. Zawartość modeli i tekstur nie została zmieniona przy dodaniu .meta; kontrola każdego oryginalnego SHA-256 i każdego GUID przeszła lokalnie. W lokalnym katalogu roboczym etap przygotowania zasobów jest wykonany; nie oznacza to jeszcze importu na runnerze GitHub Actions.

Importer w repo: `python3 unity-tools/mistrzowie/import_champion_assets.py /sciezka/champions_arena_A1_unity_ready.zip --dry-run`, a po zatwierdzeniu bez `--dry-run`. Następnie `python3 unity-tools/mistrzowie/verify_champion_assets.py` musi zwrócić **26/26 PASS** (11 plików + 15 GUID), zanim Unity rozpocznie import.

Workflow `.github/workflows/mistrzowie-apk.yml` na tej gałęzi obsługuje również ręczny `arena_assets_tag`: tag wydania GitHub zaczynający się od `champions-a1-` z ZIP jako assetem. Po wskazaniu tagu w formularzu **ręcznie** uruchamianego workflow archiwum zostaje pobrane, zweryfikowane i zaimportowane przed kompilacją. Gdy tagu nie ma, a w repo brakuje wymaganych 26 plików, workflow ma szybko przerwać próbę i nie marnować czasu na Unity.

**Stan blokady:** ZIP istnieje w artefaktach tej rozmowy, ale nie został umieszczony w GitHub Release ani zapisany jako duże pliki Git w repo. Samo przygotowanie kodu i paczki nie zwalnia z konieczności dostarczenia plików na runner. Żaden build nie został uruchomiony przez to zlecenie.

## Statyczne kontrole wykonane
1. Punkty frontowych i tylnych otworów: od x=−4,70 do +4,70 m dla obu LOD n=64 i n=128, sprawdzone algorytmicznie.
2. VIP: przy zaimportowanym oryginalnym GLB 16 foteli, odstęp poziomy środek–środek 1,45 m, minimalny odstęp między obrysami około 0,49 m, dwa rzędy rozdzielone o 1,75 m. Nie ma już wariantu zastępczego bez modelu właściciela.
3. Parking i dojścia: zachowanie granicy drogi z=667, brak wejścia nawierzchni pieszej w pas drogi przy opisanych współrzędnych.
4. Zbilansowane nawiasy i klamry w odczytanych plikach C#; **to nie jest kompilacja**.
5. Testy regresyjne do CI: `test_arena_finish.py`, `test_arena_vip.py`, `test_arena_clearance.py` oraz kontrola binariów; testy statyczne nie zastępują Unity.

## Nadal do potwierdzenia
- Import i referencje binarnych zasobów w faktycznym repo/runnerze.
- Kompilacja C# oraz Python z pełną strukturą repo (nie ma lokalnego sklonowanego checkoutu).
- Poprawność materiałów, geometrii, drzwi, sterowania i kolizji w Unity Play Mode.
- Wizualne i wydajnościowe działanie APK na telefonie, odczyt błędów, FPS i pamięci.
- Ostateczna kontrola dojścia do obu pięter trybun oraz drzwi wewnętrznych.
- Własny model fotela VIP jest w końcowym ZIP razem z trwałym GUID, lecz **nie został jeszcze wprowadzony do repozytorium lub GitHub Release**. Sam kod `BraxChampionsVIPSeats` nie potwierdza jego importu.
- Pozostałe 31 rezerwacji budynków w planie nie jest ukończoną zabudową; A2 jako druga arena nie była przedmiotem tej naprawy.

**Tryb budowania:** zgodnie z `AGENTS.md` nie uruchamiać APK, Addressables, testów Unity ani ponowień bez osobnego polecenia właściciela. Zmiana gałęzi GitHub nie jest dowodem, że kod przeszedł na Androidzie. Nie scalać `main`.
