# BRAXHORN CHAMPION ARENA A1 — etap wejścia, VIP i otoczenia (5.11.2026)

**Gałąź:** `gpt/miasto-mistrzow-arena-premium-20261105` (pochodna `codex/miasto-mistrzow-20261104`). **Silnik:** Unity 6, projekt pod `unity-city`. Bez zmian w innych regionach i bez automatycznego buildu.

## Zakres, stan kodu
- **A1:** istniejąca hala z oktagonem i sędziami została zachowana; układ widowni rozdzielony w miejscach przejść; schody na sektory po bokach, wolny korytarz wejściowy.
- **VIP przy sędziach:** 16 miejsc w dwóch rzędach i dwóch grupach. Moduł `BraxChampionsVIPSeats` używa docelowego fotela właściciela `free_luxurious_barrel_seat_2k.glb`, jeśli jest obecny; bez tego pliku w scenie pozostają tymczasowe, wyraźnie ulepszone fotele wygenerowane z PBR Leather037 i Metal048A. Stanowiska sędziów i komentatorów oraz ich wyposażenie pozostawione.
- **Oświetlenie:** 4 istniejące punkty nad oktagonem przeniesione do odrębnej nazwy i zwiększone do zasięgu 13 m, intensywności 2.15, z oprawami LED i materiałem emisyjnym. Brak nowych shadow maps; istniejące lampy siedzeń bez zmian. Ocena jasności na telefonie oczekuje na APK.
- **Elewacja:** `facade_glass` jest teraz rzeczywiście przezroczystym, niemetalicznym materiałem CityStaticPBR; `clear_glass` służy drzwiom i gablocie. Materiały i ich odbicia do weryfikacji na Androidzie.
- **Wejścia:** na froncie drzwi dwuskrzydłowe publiczne, na północy osobne wejście zawodników. Środkowe otwory mają dokładnie 9,4 m niezależnie od LOD. Osobny korytarz prowadzi do zaplecza, dwa pomieszczenia z ławkami i szafkami. Drzwi korzystają z istniejącego `BraxCityDoor`.
- **Otoczenie:** dwa utwardzone parkingi z rozdzielonymi rzędami stanowisk, odbojami, liniami, krawężnikami, dojściami pieszymi i drogami wjazdowymi. Koniec dojazdów przylega do północnej krawędzi drogi z=650 (granica nominalna z=667). Oś wejścia i istniejący dojazd z miasta pozostają drożne. Duże powierzchnie mają UV w metrach, aby asfalt i kostka Poly Haven nie rozciągały się na kilkadziesiąt metrów.
- **Wejście premium:** czerwony dywan, chromowane słupki z miękką czerwoną liną i ciepłe lampy w holu.
- **Ekspozycja pasa:** przeszklona gablota w holu przy drodze na widownię. Ładowanie oryginalnego FBX i map 4K, z Androidowym limitem 2K/ASTC.
- **Szyldy:** cztery wersje `CHAMPION ARENA`, rozmieszczane według ich oryginalnego projektu na zakrzywionej elewacji, bez podmieniania typografii. Przy braku źródeł w repo zostają istniejące litery z generatora (nie fałszywe nowe logo).

## Zasoby: pełny pakiet 108 MB — archiwum sprawdzone, binaria poza repo
Jednolity pakiet `champions_arena_A1_full_import.zip` (107 782 665 B, SHA256 `453a5fa9cd637d6a2a733893c453faa12914dc2610f69bf73dd3d5f65fbfcef1`) zawiera:
- FBX oryginalnego pasa + mapy Color, Normal, Roughness, Metallic + spakowane ARM;
- cztery przycięte oryginalne grafiki CHAMPION ARENA w RGBA;
- **oryginalny fotel VIP w zoptymalizowanej do Androida postaci GLB 2K**, bez zmiany siatek.

Przeszedł CRC wszystkich plików i weryfikację SHA256 jedenastu zasobów z manifestu. ZIP jest pobieralnym artefaktem tej rozmowy, **ale GitHub Connector nie zapisał jego dużych binarnych zasobów w repo**. Import wymaga dostarczenia pliku do środowiska budowania i użycia `python3 unity-tools/mistrzowie/import_champion_assets.py /ścieżka/champions_arena_A1_full_import.zip --dry-run`, następnie bez `--dry-run`. Niczego nie oznaczać jako Unity-ready przed potwierdzeniem importu.

## Statyczne kontrole wykonane
1. Punkty frontowych i tylnych otworów: od x=−4,70 do +4,70 m dla obu LOD n=64 i n=128, sprawdzone algorytmicznie.
2. VIP: przy zaimportowanym oryginalnym GLB 16 foteli, odstęp poziomy środek–środek 1,45 m, minimalny odstęp między obrysami około 0,49 m, dwa rzędy rozdzielone o 1,75 m. Dla tymczasowego wariantu bez GLB obowiązuje jego osobna geometria.
3. Parking i dojścia: zachowanie granicy drogi z=667, brak wejścia nawierzchni pieszej w pas drogi przy opisanych współrzędnych.
4. Zbilansowane nawiasy i klamry w odczytanych plikach C#; **to nie jest kompilacja**.
5. Testy do CI: `unity-tools/mistrzowie/test_arena_finish.py` — wymagają uruchomienia i nie są deklarowane jako wykonane w Unity.

## Nadal do potwierdzenia
- Import i referencje binarnych zasobów w faktycznym repo/runnerze.
- Kompilacja C# oraz Python z pełną strukturą repo (nie ma lokalnego sklonowanego checkoutu).
- Poprawność materiałów, geometrii, drzwi, sterowania i kolizji w Unity Play Mode.
- Wizualne i wydajnościowe działanie APK na telefonie, odczyt błędów, FPS i pamięci.
- Ostateczna kontrola dojścia do obu pięter trybun oraz drzwi wewnętrznych.
- Własny model fotela VIP jest już w kompletnym 108 MB ZIP, lecz **nie został wprowadzony do repozytorium**. Sam kod `BraxChampionsVIPSeats` nie potwierdza jego importu.
- Pozostałe 31 rezerwacji budynków w planie nie jest ukończoną zabudową; A2 jako druga arena nie była przedmiotem tej naprawy.

**Tryb budowania:** zgodnie z `AGENTS.md` nie uruchamiać APK, Addressables, testów Unity ani ponowień bez osobnego polecenia właściciela. Zmiana gałęzi GitHub nie jest dowodem, że kod przeszedł na Androidzie. Nie scalać `main`.
