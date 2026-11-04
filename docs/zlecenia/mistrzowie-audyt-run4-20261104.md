# Miasto Mistrzów — naprawa po APK #4, 4 listopada 2026

## Stan i dowód awarii

Poprawiono kod po analizie pełnego logu i źródeł. **65 testów automatycznych oraz kontrole geometrii przeszły; nowego Unity, APK i telefonu nie uruchomiono.** Nie oznacza to potwierdzenia, że cała gra działa albo że kolejny build na pewno przejdzie.

- Gałąź: `codex/miasto-mistrzow-20261104`.
- Bazowy kod i workflow: `c30e3057ae03e3c6d34eeb1fb930a43632e68299`.
- [Run #4](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41907419292), [job 126845697427](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41907419292/job/126845697427), 56 min 25 s, FAILURE.
- Unity 6000.0.58f2, planowany Android ARM64/IL2CPP, GLES3, pakiet `com.braxhorn.mistrzowie`.
- Ostatni odczyt GitHub przed zapisem: #4 zakończony niepowodzeniem; brak nowszego przebiegu Miasta Mistrzów w zwróconej liście.

Log zatrzymał się w `BraxChampionsBuild.CheckWalk`, przed generowaniem budynków i Patryka:

```text
InvalidOperationException: M1 | DWORZEC | reused city metro blocked walkway at (567.45, -4.54, 467.81) / kolizja
```

To błąd sprawdzania fizycznego przejścia przez scenę, nie dowód awarii Androida ani błędu kompilatora C#. W #4 nie powstał APK; jedyny artefakt zawierał dowody walidacji. Przygotowanie źródeł i wcześniejsze testy przeszły. Kolejne etapy nie zostały osiągnięte.

## Przyczyny i poprawki

### 1. Trasa metra przebiegała przez wyposażenie

Po odjęciu przesunięcia M1 punkt z logu odpowiada około `(72.17, -4.60, 31.60)` w źródłowej stacji. Kapsuła testowa przecina kosz przy `(71.8, -4.6, 32)`. Ta sama stara trasa dalej przecina północne biletomaty. Na peronie stary odcinek od `(42.9, -11.2, 27.15)` do `(20, -11.2, 28.5)` przecina ławki i kolumnę.

W `ChampionsWalkingRoute` przejście przez antresolę prowadzi teraz publiczną alejką Z=30.9; koniec trasy na peronie ma Z=26.4. Nie usunięto koszy, biletomatów, ławek ani kolumn. Centrum przejścia bramki odczytywane jest z rzeczywistej blokady, zamiast z początku układu współrzędnych importowanego modelu.

Nowy `audit_metro.py` czyta współrzędne z aktualnych źródeł C#, liczy kapsułę względem 28 brył wyposażenia i odtwarza stare kolizje jako testy regresji. To analiza brył, nie pełna symulacja Unity ani pomiar prywatnej siatki bramki.

### 2. Obudowa techniczna przecinała zejście na peron

Stara pełna bryła X=54…58.4 miała stały wierzch Y=-5.0. Pierwszy bieg schodów schodzi przez nią: maksymalne obliczone przecięcie trasy wynosi 1.93691 jednostki świata. Nie była to jedynie źle wyznaczona ścieżka testowa.

Obudowa dla Miasta Mistrzów ma teraz wierzch dopasowany do faktycznej rampy pierwszego biegu, z minimum 0.4 jednostki poniżej niej. Pozostaje obudową z kolizją; nie wyłączono testu ani ścian całej stacji. Parametr jest opt-in dla tego regionu. Pozostali wywołujący `SchodyNaPeron()` zachowują dotychczasową geometrię.

### 3. Pociąg ustawiony pionowo

Log #4 zawierał:

```text
BRAXHORN_METRO_POCIAG ... dlugosc_m=5.8 ... gora_nad_szyna_m=27.73 ... os_dluga=z
```

Odczytano rzeczywisty `metro_train.glb`: jego korzeń i dziecko mają przeciwne obroty o 90 stopni wokół X. Kod zerował tylko obrót korzenia. Obliczone rozmiary modelu przed skalowaniem:

| Transformacja | X | Y | Z |
|---|---:|---:|---:|
| Zachowana hierarchia źródła | 70.21810 | 110.10966 | 529.07890 |
| Wyzerowany sam korzeń | 70.21810 | 529.07890 | 110.10966 |

W regionie Mistrzów pomiar i rozmieszczenie wagonów zachowują obrót źródła; obrót ustawienia na torze nakłada się na niego. Model, materiały i proporcje pozostają oryginalne. Dodano kontrolę wymiarów obu wagonów M1 w Unity, aby pionowy wagon nie przeszedł walidacji. Domyślne zachowanie innych regionów pozostaje bez zmian.

### 4. Powtórny koszt importu tych samych tekstur

Log pokazywał import kopii mapy z ustawieniami odziedziczonymi, a następnie ponowny import po ustawieniu docelowej kompresji: przykładowo mapa koloru piasku około 4.3 s + 28.2 s, normalna około 7.1 s + 53.1 s. Są to przykłady z tego przebiegu, nie prognoza czasu nowego buildu.

Nowy `BraxChampionsTextureImport` ustawia parametry przed pierwszym importem wygenerowanej kopii. `CopyMap` zachowuje istniejący GUID i sprawdza wynik importu. Zakres ograniczony do `PlayableGenerated/Mistrzowie/Textures` i ról color/normal/arm. Nadal 4096×4096, pełne mapy, RGB GL normal, ASTC 4×4 quality 100, mipmapy, streaming i anizotropia 8. Nie obniżono jakości ani rozdzielczości.

Mechanizm odpowiada dokumentacji Unity 6: [OnPostprocessTexture — format ustawia się w OnPreprocessTexture](https://docs.unity3d.com/cn/6000.0/ScriptReference/AssetPostprocessor.OnPostprocessTexture.html), [AssetPostprocessor](https://docs.unity3d.com/cn/6000.0/ScriptReference/AssetPostprocessor.html). Kompresja i rzeczywisty zysk czasu wymagają kolejnego uruchomienia Unity.

### 5. Diagnostyka i zachowanie importów po błędzie

- Wszystkie trzy stacje dostają niezależną kontrolę struktury i spaceru. Raport zachowuje wszystkie wykryte błędy tych kontroli przed przerwaniem etapu.
- Dla przeszkody log podaje odcinek, pozycję lokalną, pełną ścieżkę obiektu i granice kolizji. Zamiast anonimowego `kolizja` będzie można wskazać obiekt.
- Kontrola nadal przerywa build na błędzie. Pomijane są tylko blokady ruchomych skrzydeł bramek, a nie dowolna kolizja pod obiektem drzwi.
- Po niepowodzeniu Unity workflow może zachować zaszyfrowany cache importów. Dotychczas pierwszy nieudany przebieg tracił całą wykonaną pracę importowania. Nie przywraca się wygenerowanych scen ani kluczy podpisu.
- Dostawa gotowego APK jest nadal przed raportami i cache. Raport jest przed pakowaniem cache, aby błąd lub brak miejsca przy pakowaniu nie odbierał diagnostyki.
- Workflow nadal ma wyłącznie `workflow_dispatch`; brak samoczynnych buildów i ponowień.

## Wykonane sprawdzenia

| Kontrola | Wynik |
|---|---|
| Testy projektu Python | 51 PASS, w tym 8 nowych regresji metra; pozostałe obejmują materiały, źródła, Patryka, geometrię, cache i dostawę APK |
| Testy skanera C# | 14 PASS |
| Skan składni i zakresów C# | 274 pliki, 0 błędów; nie jest kompilacją Unity |
| Metro analitycznie | 28 brył, 10 punktów trasy, brak kolizji z modelowanym wyposażeniem; potwierdzone stare kolizje, poprawiony prześwit i osie realnego GLB |
| Foundation | PASS: 100 siatek, 95 668 trójkątów, 287 roślin, 80 rezerw stanowisk mariny |
| Infrastruktura | PASS: 278 siatek, 715 616 trójkątów, 3 stacje/otwory, 10 platform tramwaju, 32 autobusowe, 69 przejść |
| Kontrakt łączników metra | PASS: 6 końców źródłowych tuneli na prostych podejściach, zgodne rozstawy i wysokości, otwory przez teren |
| Rozmieszczenie trawy | PASS: 14 328 kęp, maska 1200×1500 |
| A1 i T01 | PASS: geometria LOD w działkach, wysokości nominalne 48 i 150 m |
| Audyt planu | 33 rezerwy, 2 wygenerowane budynki, 0 błędów kontroli rozmieszczenia |
| Workflow | YAML odczytany, unikalne identyfikatory kroków, tylko ręczny trigger, wszystkie bloki shell przeszły `bash -n` |
| Diff | `git diff --check` bez błędów |
| Unity / nowy APK / Android | NIEURUCHOMIONE |

Skaner uruchomiono na przypiętych zależnościach z `unity-tools/validation/requirements.txt` (tree-sitter 0.25.2, tree-sitter-c-sharp 0.23.5). Początkowy skan w lokalnym środowisku z tree-sitter 0.26.0 zakończył się awarią procesu; po użyciu wersji projektu skaner i jego testy przeszły. To różnica środowiska diagnostycznego, nie zmiana zależności projektu ani dowód błędu C#.

51 testów projektu i generatory geometrii sprawdzono także na dokładnych wersjach CI: numpy 2.2.6, shapely 2.1.2, Pillow 11.3.0, matplotlib 3.10.7. Składnia wszystkich 32 plików Python w obu katalogach walidacji jest poprawna. Metadane nowego skryptu C# odczytano jako YAML, z poprawnym GUID.

Polecenia odtworzenia:

```bash
python3 -m pip install -r unity-tools/validation/requirements.txt
python3 -m pip install numpy==2.2.6 shapely==2.1.2 pillow==11.3.0 matplotlib==3.10.7
python3 -m unittest discover -s unity-tools/validation -p 'test_*.py' -v
python3 unity-tools/validation/check_csharp_scopes.py unity-city/Assets
python3 -m unittest discover -s unity-tools/mistrzowie -p 'test_*.py' -v
python3 unity-tools/mistrzowie/audit_metro.py
python3 unity-tools/mistrzowie/build_infrastructure.py
python3 unity-tools/mistrzowie/prepare_landscape.py
python3 unity-tools/mistrzowie/validate_contract.py
python3 unity-tools/mistrzowie/build_exteriors.py
python3 unity-tools/mistrzowie/audit_layout.py
```

## Gra i zakres niepotwierdzony

Przejrzano kod Start→Explore, jednego gracza/kamery, bootstrapu HUD, sterowania, resetu postaci i wyjątków dla obszaru `mistrzowie`, przenoszenia Patryka, 7 wymaganych klipów, zapisu i ponownego otwierania scen. Taka inspekcja nie dowodzi poprawnego retargetingu, pracy bramek i schodów ruchomych ani obrazu, FPS i pamięci na telefonie.

Nie obniżono wymagań dotyczących Patryka, map 4K i zapisanych animacji. Nie pobierano lokalnie ponownie zaszyfrowanych zasobów ani nie zastępowano ich atrapami; przygotowanie tych źródeł przeszło w #4. Pełna walidacja fizyki, prywatnych bramek, materiałów i skompilowanego playera wymaga Unity.

Pozostaje oddzielny region eksploracji z A1/T01 i rezerwami pozostałych budynków. Audyt nie oznacza ukończenia całego planu, ruchu pojazdów, wnętrz hotelu ani rzeźby byka. Nie zmieniono `main`, nie scalono regionów i nie wykonano wizualnego odbioru.

## Następne zatwierdzone uruchomienie

W [formularzu workflow](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/workflows/mistrzowie-apk.yml) wybrać gałąź `codex/miasto-mistrzow-20261104`, pole commit pozostawić puste, `zdjecia=false`. Wybór tej gałęzi jest potrzebny również dla poprawionego workflow; sam input z SHA przy workflow z main nie zmienia kodu workflow. `Re-run` starego #4 testowałby stary commit.

Kolejność obowiązkowa: preflight → pełne trzy stacje → A1/T01 i dojście do oktagonu → zapis/ponowne otwarcie → Patryk i Explore → kompilacja APK → sprawdzenie pakietu/certyfikatu/wersji i dostawa → test telefonu. Cache nie jest dowodem powodzenia żadnego z tych etapów.

Zgodnie z [AGENTS.md](../../AGENTS.md), „Nowe buildy tylko po osobnej zgodzie właściciela”. W tej naprawie przygotowano kod, dowody i gałąź do pojedynczego zatwierdzonego uruchomienia; nie uruchomiono serii kosztownych prób.
