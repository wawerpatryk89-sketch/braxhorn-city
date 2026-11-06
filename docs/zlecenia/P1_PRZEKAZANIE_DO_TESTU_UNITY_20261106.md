# P1 — BASEN MIEJSKI + SPA | przekazanie do testu Unity 6

Status: **ŹRÓDŁA I IMPORTY ZWERYFIKOWANE; KOMPILACJA UNITY NIEPRZEPROWADZONA**.

## Zakres P1 (bez innych dzielnic)
- Działka P1 **160 × 120 m** na X=520, Z=180. Hala, kryte skrzydło rodzinne i SPA, recepcja, pomieszczenia obsługi, wejścia, elewacje i szklenia.
- Basen 25 m, rodzinny i brodzik z niebieską animowaną wodą. Trójwymiarowe dno i ściany.
- Cztery **oryginalne** jacuzzi z wodą i bąbelkami. Dwie rzeczywiste leżanki zabiegowe, sauna, podwójna umywalka, szatnie, szafki, ławki, cztery prysznice premium w odrębnym pokoju, leżaki oraz stanowisko ratownika.
- Fizyczne stopnie, podest i podpory lekkiej zjeżdżalni `raft-slide-water-park.glb`. Wysokopoligonowa pięciokrotna spirala **nie jest** zatwierdzona ani wstawiona; niestabilne LOD 2K/8K są odrzucone.
- Oryginalne, niezmienione szyldy `P1_BASEN_BASEN_MIEJSKI.png` i `P1_SPA_SPA_WELLNESS.png` na osobnych kasetonach 4:1.
- 95 stanowisk parkingowych + 6 TAXI, dwa połączenia jezdni z parkingami, oddzielony chodnik/piazza.

## Dowody: rzeczywiste GitHub Actions, 6 listopada 2026
- Run [42152453084](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42152453084): **32/32 testy kontraktów P1 PASSED**; wejście do prywatnego szkicu P1 Release i import paczki `--dry-run` zakończyły się sukcesem.
- Run [42153286608](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42153286608): pozytywna weryfikacja hashy **dwóch oryginalnych ZIP wewnątrz** archiwum A1/T11. Ten etap naprawia błąd workflow, który wcześniej szukał nieistniejących osobnych załączników.
- Run [42153456257](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42153456257): **SUCCESS** — 32 testy P1, kryptograficzna weryfikacja A1/T11 i P1, oraz rzeczywisty import obu archiwów A1/T11 i oryginalnej paczki P1 do tymczasowego checkoutu runnera GitHub Actions. Wynik: `A1_T11_AND_P1_REAL_SOURCE_INSTALL_PASS`. Bez zmiany animacji, bez kompilacji Unity, bez APK.
- P1 Release: szkic `p1-basen-spa-20261106`, asset `P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip`, 121306476 bajtów, SHA256 `5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263`. Wydanie nadal **draft**, nie upubliczniać jako biblioteki modeli.
- A1/T11: szkic wydania o tagu `champions-a1-20261105` ma oryginalny zewnętrzny ZIP `BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip`. Używać `unpack_a1_t11_nested_release.py` i **dwóch SHA256-piętnowanych oryginałów**, nie polegać na `gh release download` po samym tagu: są dwa szkice o tym samym tagu.
- Źródło o sprawdzonym wyniku importu: gałąź `gpt/miasto-mistrzow-basen-p1-20261105`, commit `59fbfbd039cbb4b50cbb5c6deaf24e6ab20af4f5` (późniejsze zmiany tego dokumentu nie są zmianami modeli).

## Kolejny etap — tylko test scen, bez APK
1. Otworzyć [workflow Miasto Mistrzów](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/workflows/mistrzowie-apk.yml) **z gałęzi P1**: `gpt/miasto-mistrzow-basen-p1-20261105`.
2. Ustawić `etap=sceny`; `arena_assets_tag=champions-a1-20261105`; `p1_assets_tag=p1-basen-spa-20261106`; nie wybierać `apk` ani `aktualizacja` na etapie pierwszego QA geometrii. Nie zmieniać opcjonalnych mechanizmów weryfikacji innych dzielnic.
3. Sprawdzić kompilację Unity 6, generowanie prefabów, import glTFast/FBX, brak błędów shaderów PBR i nieblokowane wejście główne.
4. W wizualnej kontroli przejść: foyer → dwa gabinety masażu, foyer → SPA 4 jacuzzi, wejście do sauny, kabiny prysznicowe, oba baseny, schody i podest zjeżdżalni, wjazdy z drogi na dwa parkingi.
5. Zweryfikować poziom wody we wszystkich nieckach i czterech jacuzzi, zniknięcie oryginalnej nakładającej się płaskiej wody, przezroczystość fasad, brak przenikania postaci w wyposażenie, prawdziwe kolorystyki i proporcje oryginalnych szyldów.
6. Zmierzyć pamięć, czas doczytywania i FPS na Androidzie. **Dopiero po akceptacji** przygotować osobną, zgodną z istniejącą bazą **aktualizację delta**, zamiast kompilowania całego miasta bez potrzeby.

## Nadal NIE POTWIERDZONE
- Realna kompilacja Unity 6 ani przebieg `sceny` nie zostały uruchomione w tym zleceniu.
- Sterowanie postacią i pływanie w wodzie na fizycznym telefonie nie zostały zweryfikowane; nie dodawać nowych animacji.
- Kontrola całego pakietu Android (FPS/RAM, shader urządzenia, interakcje, kolizje wejść, czas ładowania) nie została wykonana.
- Aktualizacja Addressables/delta, jej import na telefonie i wspólne scalenie z gałęziami innych wykonawców **nie zostały wykonane**.
- Nie dodawać ciężkiej spirali 461 tys. trójkątów ani innych niezoptymalizowanych modeli. Ręczniki 94 tys. trójkątów pozostają niewstawione.
- Licencje modeli CC BY wymagają odpowiednich kredytów przed publiczną publikacją (rejestr `P1_ZRODLA_I_KREDYTY_20261106.md`).

**Wynik:** źródła, paczki i integracja tekstowa są przygotowane **do uruchomienia scenowego testu Unity 6**. Nie oznaczać jako gotowego APK ani opublikowanej aktualizacji bez dalszego dowodu.


## Audyt po ostatnim zleceniu — dodatkowe dowody źródłowe z 6.11.2026
- [GitHub Actions **42174251170**](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42174251170): **SUCCESS**. Zreprodukowane **32/32** testy P1; `CSHARP_SCOPE_CHECK files=10 errors=0` z parserem C#; sprawdzenie archiwum P1 121,3 MB (16 oryginałów i pliki .meta); prawidłowe CRC oraz sumy; **8 modeli GLB** z rzeczywiście występującymi częściami kolizji; prawdziwy import prywatnych paczek A1/T11 i P1 do checkoutu GitHub runnera.
- Sprawdzone nazwy i liczby **źródłowych siatek konstrukcyjnych**: jacuzzi **14**, sauna **3**, kabina prysznicowa **2** (brodzik + konstrukcja, bez pełnego kolidera szklanych drzwi), stół do masażu **9**, podwójna umywalka **12**, drabinka basenu **1**, szafki **6**, ławka do szatni **1**. Liczby są kontraktem statycznym źródła, **nie** fizycznym testem kolizji na Androidzie.
- Nowy plik `unity-tools/mistrzowie/verify_pool_p1_model_geometry.py` odczytuje *rzeczywiste* źródłowe GLB z dokładnego, haszowanego ZIP-a. Sprawdza m.in. część `Spa water surface` oryginalnego jacuzzi i nazwane siatki sauny/pryszniców oraz to, że oryginały nie zostały zdeformowane.
- Poprawka `BraxChampionsPoolP1SpaFurnishing.cs`: brak wymaganej bryły kolizyjnej zatrzymuje generowanie P1 zamiast zostawiać mebel pozbawiony kolizji; dodano kolizję do **oryginalnego kamiennego blatu** umywalki. Poprawka zachowuje originalną geometrię i wszystkie materiały.
- Ostatnia wersja bazowa przetestowana powyżej: commit `b17296b6c78fec8a88cd9eb3409fdc29e78067b4`; dopisanie tego protokołu nie modyfikuje modeli ani kodu generatorów.
- **Nie wykonano nadal:** Unity Editor compilation, `etap=sceny`, PlayMode, APK, test telefonu, merge ani delta release. Źródłowe testy przygotowują do tych etapów, nie zastępują ich. Kolejna właściwa czynność to **`etap=sceny` na gałęzi P1**, a nie pełny build APK.
