# Miasto Mistrzów — audyt przed kolejnym APK

Data: 5 listopada 2026, Europe/Zurich (odczyty 4 listopada, UTC).  
Repozytorium: `wawerpatryk89-sketch/braxhorn-city`.  
Gałąź: `codex/miasto-mistrzow-20261104`.  
Audytowany kod: **`38bece9f7b1feacd851b74441be55a5072d1db63`**.  
Unity w projekcie i workflow: **6000.0.58f2**.

**Decyzja: nie uruchamiać ponownie obecnego kodu. Jest potwierdzony, nieusunięty bloker przejścia przez metro. Testy Pythona go nie obejmują. Nie ma jeszcze zweryfikowanego commita gotowego do następnego pełnego buildu.**

W tym zadaniu nie uruchomiono Unity, buildu, ponowienia ani publikacji APK. Nie zmieniono kodu gry, scen, zasobów, ustawień ani workflow. Jedynymi zapisywanymi zmianami są instrukcja korzystania z Unity Essentials i ten raport.

## 1. Zakres i poziom dowodów

Użyto Unity Essentials: audytu zdrowia projektu, kontroli gotowości buildu, diagnostyki błędów i sprawdzenia MCP. Lokalny projekt, manifest i narzędzia nie wykazały dostępnego mostka Unity MCP; w środowisku audytu nie ma uruchomionego edytora ani lokalnego kompilatora Unity. Nie instalowano nowego mostka.

Przejrzano historię gałęzi, workflow, pełne drzewo repozytorium, wszystkie 274 pliki C# w `unity-city/Assets` parserem oraz szczegółowo ścieżkę generowania Miasta Mistrzów, importy, animacje Patryka, budowanie i dostawę APK. Jest to audyt statyczny z testami lokalnymi i analizą faktycznie wykonanych logów CI, nie test wszystkich zachowań gry ani odbiór wizualny.

Unity w najnowszym runie wykonało kod **c95f6fe055b43cc1e26f81fbe3051beec3ea9b67**. Różnica do audytowanego HEAD 38bece9 obejmuje tylko dokumentację i zdjęcia referencyjne. Kod gry i workflow są takie same. Dokumentacyjny commit z tym raportem również nie naprawia gry.

## 2. Co faktycznie stało się w Actions

| Próba | Dowód | Wynik |
|---|---|---|
| #2 | [41889411877](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41889411877) | Około 26 min; niezapisana scena blokowała NewScene(Additive). Kod źródłowy w logu: 6f799b67994e1afa430906a04817d7df447a9678. |
| #3 | [41898644227](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41898644227) | Formularz wymagał pełnego SHA, wpisano nazwę gałęzi. |
| #4 | [41907419292](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41907419292) | Kod c30e305; generowanie doszło do kontroli metra, trasa kolidowała m.in. z koszem. |
| #5 | [41924689527](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41924689527) | Uruchomiono stary kod 47c89f0; prepare_assets.py zakończył się KeyError: 'rough', przed Unity. Pułapka starego formularza wystąpiła rzeczywiście. |
| #6 | [41924931920](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41924931920), [job 126906719933](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41924931920/job/126906719933) | **Failure**, kod c95f6fe, od 22:24:00 do 23:08:53 UTC, około 44 min 53 s. Wszystkie trzy stacje zatrzymują się na kolizji kasy biletowej. APK nie powstał. |

W #6 kolejno zapisano:

- 22:26:47 UTC: `BRAX_MISTRZOWIE_PBR_OK maps=62 original=4K`.
- 22:26:56: zweryfikowano źródła Patryka i odtworzono sześć dostarczonych klipów.
- 22:43:47: `BRAX_MISTRZOWIE_INPUTS_OK geometry=100 K=1.294444`.
- 22:56:51: `BRAX_MISTRZOWIE_FOUNDATION` — scena fundamentu zapisana; 100 siatek, 107918 trójkątów, 164 palmy, 123 drzewa, 80 miejsc cumowania.
- 23:07:59: wyjątek kontroli przejścia przez metro.
- Jedyny artefakt tego runu: `mistrzowie-validation-41924931920-1`, 23912 bajtów. To raporty walidacji, **nie APK**.

Log nie wykazuje błędu CS, zabicia procesu z powodu pamięci ani braku miejsca jako przyczyny zakończenia #6. Kontrola metra rzuciła wyjątek przed `BuildPlayer`.

## 3. Potwierdzone błędy i potrzebne poprawki

### P0 — trasa kontroli metra przecina kolizję kasy

Pewność: potwierdzone w Unity i odtworzone analitycznie z geometrii źródłowej.

Pliki:

- [BraxClaudeMetro.Reuse.cs, 71–82](../../unity-city/Assets/Editor/Mistrzowie/BraxClaudeMetro.Reuse.cs): `ChampionsWalkingRoute()`.
- [BraxClaudeMetro.cs, 669–675](../../unity-city/Assets/Editor/Claude/BraxClaudeMetro.cs): rzeczywiste ściany i front kasy.
- [BraxChampionsBuild.cs, 263–300](../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs): kontrola kapsułą.
- [audit_metro.py, 63–98](../../unity-tools/mistrzowie/audit_metro.py): niepełny zestaw przeszkód w teście lokalnym.

Trasa używa dwukrotnie `EZ[1]-1.7f`, czyli lokalnego **Z=30.9**. Północna ściana kasy zajmuje Z=30.60–30.75, X=76.8–78.6. Odległość środka trasy od ściany wynosi tylko **0.15 m**, a promień kontrolowanej kapsuły wynosi **0.35 m**. W tym miejscu musi wystąpić kolizja.

Fragment logu M1:

```text
segment 2 blocked walkway ... local=(77.60,-4.60,30.90)
... Existing station and open tunnels/kolizje/kolizja
bounds=Center: (572.98, -2.62, 466.88), Extents: (0.90, 1.93, 0.08)
```

W segmencie 3 zgłoszono tę samą ścianę i front kasy. Wymiary colliderów odpowiadają elementom tworzonym przez `W.MM(..., true)`. M2 i M3 powielają błąd po transformacji całej stacji.

**Potrzebna poprawka:** poprowadzić oba punkty dalej od kasy i objąć kasę testami. Sprawdzony analitycznie kandydat to **`EZ[1]-1.35f`, czyli Z=31.25**. Przy tym wariancie nie wykryto przecięć z 28 dotychczasowymi bryłami testu ani czterema colliderami kasy odczytanymi ze źródła. Dotychczasowa trasa po dodaniu tych colliderów nie przechodzi.

To kandydat statyczny, **nie poprawka zatwierdzona przez Unity**. Nie został zastosowany. Test musi objąć także stałe obudowy bramek, ściany, schody i pozostałe collidery na całej trasie; pozostaje sprawdzenie Physics w trzech stacjach i po ponownym otwarciu sceny. Nie usuwać kasy, nie zmieniać jej wyglądu i nie wyłączać kontroli kolizji.

### P0 — stare workflow z main nie pasuje do aktualnych źródeł

Pewność: potwierdzona konfiguracja; stary SHA spowodował rzeczywistą awarię #5. Dalsze konflikty wynikają z porównania plików.

[Workflow Miasta Mistrzów](../../.github/workflows/mistrzowie-apk.yml) w tej gałęzi ma puste domyślne pole commit i aktualne przygotowanie źródeł. **Wersja na main nadal ma domyślne 47c89f05796c675d02a15982382a37ef4c5d20be.**

Sama zmiana pola commit na nowe SHA przy wybraniu workflow z main nie wystarczy:

- Stary sparse checkout nie pobiera `unity-tools/claude/import_patryk_animations.py` i paczki `unity-city/Prywatne/import-patryk-animations-20261104`.
- Aktualny [restore_player.py:111](../../unity-tools/mistrzowie/restore_player.py) importuje ten moduł. Bez niego etap odtwarzania postaci zakończy się błędem.
- Stary workflow nie wykonuje `prepare_landscape.py`. Aktualny [BraxChampionsGrass.cs:36–49](../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsGrass.cs) wymaga maski gleby i `grass-clumps.json`.
- Stary workflow nie zawiera pozostałych aktualnych zabezpieczeń przygotowania i cache.

**Przy przyszłym ręcznym uruchomieniu trzeba wybrać „Use workflow from”: `codex/miasto-mistrzow-20261104` oraz pełne SHA nowego, naprawionego commita.** Nie ponawiać starego runu. Osobna synchronizacja launchera na main wymaga zgody właściciela; w tym audycie jej nie wykonano.

### P1 — rzeczywisty błąd importu harbor_crane.glb

Pewność: błąd importera potwierdzony w logach #2, #4 i #6; przyczyna wewnętrzna nieustalona.

Plik: [harbor_crane.glb](../../unity-city/Assets/Source/HarborBeach/Models/harbor_crane.glb). Log wskazuje `GLTFastGltfImporter.cs:294`.

Kontrola kontenera GLB: poprawna sygnatura i długość GLB 2, obrazy osadzone wewnątrz, brak zewnętrznych URI obrazów i buforów, zakresy bufferViews mieszczą się w buforze. To nie dowodzi, że importer poprawnie obsłuży całą zawartość modelu.

Nie znaleziono odwołania do tego dźwigu w generatorach Miasta Mistrzów. Import nie zatrzymał wejścia w BuildAndroid; **nie był fatalną przyczyną #6**. Potrzebny pełny komunikat glTFast i izolowany import modelu. Jeżeli model jest poza zależnościami tej sceny, można po sprawdzeniu zależności wydzielić go z obszaru importowanego przez ten wariant. Nie usuwać modelu z biblioteki projektu ani zastępować go bez zgody.

## 4. Poprawka sceny i areny

**Scena — poprawka potwierdzona na rzeczywistej ścieżce CI.**  
[BraxChampionsFoundation.cs:120–142](../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsFoundation.cs) wykorzystuje istniejącą scenę tylko wtedy, gdy jest jedyna, załadowana, bez ścieżki i bez obiektów. Inne niezapisane sceny powodują czytelny wyjątek przed modyfikacją zasobów. `SaveScene` jest sprawdzane, a użyta pusta scena nie jest zamykana w finally. Run #6 zapisał fundament i przeszedł do metra, więc pierwotny błąd NewScene(Additive) nie powrócił. Ręcznych wariantów pracy z cudzą otwartą sceną nie wykonano.

**Arena — poprawka jest w kodzie, pełne potwierdzenie nadal nieosiągnięte.**  
[BraxChampionsBuild.cs:214–235](../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs) omija tył schodów trybun trasą przez X=602 zamiast środka X=600. Zachowano raycast podłoża, limit wysokości kroku i kapsułę 0.35 m. Przegląd geometrii i transformacji nie wskazał, żeby poprawka wyłączała zabezpieczenia lub zmieniała wygląd areny.

Run #6 zatrzymał się w `CheckStations` na linii 59, przed generowaniem budynków i areny oraz `CheckExteriorAccess`. Historyczne „677 próbek podłoża” nie jest dowodem przejścia tej aktualnej sceny w Physics ani działania Patryka. W tym audycie nie powtórzono tego historycznego pomiaru. Nie zatwierdzono areny w Unity ani wizualnie.

## 5. Pozostałe obszary

| Obszar | Ustalenie i dowód | Status / zalecenie |
|---|---|---|
| C# | Parser: 274 pliki, 0 błędów. W #6 Unity wykonało BuildAndroid na kodzie równoważnym HEAD. Nie znaleziono nieosłoniętych zależności UnityEditor w przejrzanym kodzie runtime; wywołania WebGL są warunkowe. | Kompilacja skryptów edytora przeszła w CI. Parser nie zastępuje kompilatora; Android IL2CPP/Gradle nie zostały jeszcze wykonane. |
| Pakiety | `Packages/manifest.json`: glTFast 6.20.0, wymagane moduły Unity. Brak śledzonego `packages-lock.json`. | Ryzyko odtwarzalności zależności pośrednich. Zapisać lock wygenerowany przez właściwy Unity Package Manager, nie konstruować go ręcznie. |
| GUID i ustawienia | Pełne drzewo Git: 222 z 274 plików C# nie ma śledzonego pliku .meta. ProjectSettings zawiera tylko ProjectVersion.txt. | Nie jest to dowód uszkodzonych referencji w świeżo generowanym CI. Jest to ryzyko przenoszenia scen/cache. Zapisywać stabilne .meta i potrzebne ustawienia wygenerowane w Unity; nie nadawać losowych GUID istniejącym scenom. |
| Tekstury | W #6 zweryfikowano 62 źródłowe mapy 4K. Aktualne przygotowanie obsługuje warianty nazw roughness. Importer wygenerowanych map ustawia 4096, ASTC 4×4, mipmapy, streaming, poprawne sRGB dla kolorów. | Nie potwierdzono wszystkich shaderów na urządzeniu. Zachować ustaloną jakość; nie obniżać rozdzielczości dla ukrycia problemu. |
| Patryk | Odtwarzanie źródeł i sześciu klipów właściciela przeszło #6. Generator retargetuje na szkielet modelu i sprawdza nogi dla chodu/biegu. | Generowanie postaci i walidacja gotowej sceny nie zostały osiągnięte. Mechanizm próbuje zapasowych źródeł animacji po błędzie; raportować rzeczywiście wybrany klip. Sprawdzić deformacje, ręce i stopy w ruchu, nie tylko obecność klipów. |
| Zapis scen | Kod sprawdza siatki/materialy/skrypty, zapisuje scenę i ponownie ją otwiera; osobno buduje Explore. | W #6 nie osiągnięto kontroli ponownego otwarcia końcowej sceny. W raporcie sceneReloadPassed=false nie oznacza, że samo otwarcie już się nie udało; etap nie został wykonany. |
| Android | `BraxChampionsBuild.cs:105–163`: ARM64, IL2CPP Release/OptimizeSize, minSdk 24, GLES3, APK bez OBB, com.braxhorn.mistrzowie, sceny Start i Explore. | Konfiguracja spójna z osobnym APK do zwiedzania. targetSdk i część ustawień pozostają domyślne. Zapisać wersje SDK/NDK/JDK i wybrane targetSdk przed Unity. Brak dowodu udanej kompilacji Androida. |
| Podpis | Ten sam keystore, alias braxhorn, wymagane hasło i versionCode. `apk_delivery.py:54–85` sprawdza aapt, apksigner i SHA-256 certyfikatu z keytool. | Przed Unity sprawdzić, czy klucz da się odczytać, alias istnieje i narzędzia są dostępne. Aktualny etap przed Unity sprawdza obecność sekretów/base64, nie pełny certyfikat. Nie potwierdzono złego klucza; brak APK uniemożliwia sprawdzenie podpisu wynikowego. |
| VersionCode | Osobny licznik czasu z kontrolą zakresu; workflow jest serializowany. | Testy lokalne przeszły. Zdolność aktualizacji zainstalowanej aplikacji wymaga zgodnego applicationId, certyfikatu i większego versionCode oraz próby na telefonie. |
| Dostawa | Gotowy marker po udanym BuildPlayer; niezależne Actions artifact i test release, kontrola tożsamości i pobranego pliku przed opcjonalnymi zdjęciami/cache. | Logika i testy lokalne są poprawne w sprawdzonym zakresie. W #6 etap nie miał APK do dostarczenia. Nie ma dowodu rzeczywistej dostawy tego wariantu. |
| Cache i czas | W #6 brak trafienia PBR/Library; po błędzie zapisano zaszyfrowane Library. Cache nie przechowuje wygenerowanych Assets ani Bee/Il2CppBuildCache. | Potwierdzony zapis nie dowodzi skutecznego odtworzenia w następnym runie. Generowanie i część importów nadal wrócą. Brak podstaw do obietnicy buildu w kilka minut. |
| Telefon | Streaming ma budżet 512 MB i redukcję maksymalnie o jeden mip. ASTC 4×4 nie jest gwarantowane na każdym GPU dopuszczonym przez GLES3. | Potencjalne ryzyko RAM/GPU; urządzenia bez ASTC mogą rozpakować tekstury do RGBA32. Potrzebny profil rzeczywistego telefonu, bez samowolnej zmiany jakości. Nie istnieje potwierdzony tutaj uniwersalny limit APK 500 MB. |
| Kompletność wizualna | Lokalny generator tworzy obecnie 2 elewacje przy 33 rezerwacjach; raport wskazuje brak źródłowego rzeźbionego byka. | Braki zakresu wizualnego są jawne, nie są wykazaną przyczyną awarii. Sukces przyszłego APK nie będzie oznaczał ukończenia całego planu ani jakości zaakceptowanej przez właściciela. |

## 6. Wersja Unity i oficjalne poprawki bezpieczeństwa

Projekt i workflow nadal wskazują 6000.0.58f2. Oficjalne komunikaty Unity zaktualizowane 3 listopada 2026 wskazują **6000.0.85f2** jako poprawioną wersję linii 6000.0 dla spraw eMudhra i Brotli.

To potwierdzony brak aktualizacji silnika, a nie przyczyna kolizji w #6. Podatność aplikacji na Brotli zależy od przetwarzania niezaufanych danych przez wskazane API; w przeszukanym kodzie Assets i narzędzi nie znaleziono wywołań BrotliStream/BrotliDecoder/BrotliEncoder. Nie jest to pełny audyt zależności binarnych.

Przed wydaniem zaplanować spójną aktualizację `ProjectSettings/ProjectVersion.txt`, obu `unityVersion` w workflow, kluczy cache i wersji zapisanej w `apk_delivery.py`. Najpierw potwierdzić dostępność właściwego obrazu Android w GameCI i wykonać kontrolę importu/kompilacji. Nie wystarczy podmiana jednego numeru. W tym audycie silnika nie zaktualizowano.

Źródła oficjalne:

- [Unity — eMudhra](https://gw-prd.hexagon.unity.com/security/unity-emudhra-compromise)
- [Unity — Brotli](https://gw-prd.hexagon.unity.com/security/unity-brotli-decoder-vulnerability)
- [Unity 6000.0.85f2](https://unity.com/vn/releases/editor/whats-new/6000.0.85f2)
- [Unity 6 — lock files](https://docs.unity.com/en-us/engine/6000.0/manual/packages-list/dependencies-lp/upm-conflicts-auto)
- [Unity 6 — formaty tekstur według platformy](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-choose-format-by-platform)

## 7. Wykonane testy i ich granice

| Sprawdzenie | Wynik |
|---|---|
| `unittest discover -s unity-tools/mistrzowie -p 'test_*.py'` | **51/51 PASS**, po pobraniu rzeczywistego śledzonego modelu pociągu do lokalnej kopii. |
| `unittest discover -s unity-tools/validation -p 'test_*.py'` | **14/14 PASS** na wersjach parsera przypiętych w requirements. |
| `check_csharp_scopes.py unity-city/Assets` | **274 pliki, 0 błędów**; to analiza składni i zakresów, nie kompilator Unity. |
| Python / workflow | Składnia 30 skryptów, YAML oraz bash -n dla 13 bloków run w aktualnym workflow — PASS. |
| Fundament i infrastruktura | build_infrastructure.py — PASS; 100 siatek fundamentu, 287 roślin, 80 miejsc cumowania; 3 stacje, 2 tory metra, 2 tramwajowe, 10 peronów tramwajowych, 32 autobusowe, 69 przejść. |
| Krajobraz / kontrakt | prepare_landscape.py: 14328 kęp trawy; validate_contract.py — PASS. |
| Elewacje i układ | build_exteriors.py, audit_layout.py, build_signs.py — PASS z jawnymi brakami zakresu wizualnego. |
| Istniejący audit_metro.py | PASS na 28 bryłach wyposażenia, ale bez colliderów kasy. **To potwierdzona luka pokrycia testów.** |
| Dodatkowa analiza kasy | Oryginalna trasa koliduje; kandydat Z=31.25 nie koliduje ze sprawdzonym zestawem 32 brył. Nie wykonano Unity Physics dla kandydata. |
| Lokalne Unity / PlayMode / Android / telefon | **NIEWYKONANE**. |

Lokalne generatory utworzyły dane testowe bez uruchamiania Unity. Nie odtwarzano lokalnie prywatnych paczek ani sekretów. O ich poprawnym przygotowaniu w #6 świadczy istniejący log CI.

W pierwszej próbie lokalnej dwa testy nie mogły znaleźć pociągu przez ograniczony checkout. Po pobraniu oryginalnego pliku oba przeszły; nie był to błąd repozytorium. Systemowy parser w innej wersji zakończył proces błędem po testach; w odizolowanym środowisku z przypiętymi tree-sitter 0.25.2 i tree-sitter-c-sharp 0.23.5 testy i pełny skan zakończyły się kodem 0.

## 8. Warunki kolejnego uruchomienia

1. Naprawić trasę metra i dodać test, który wykazuje błąd starej trasy oraz brak kolizji poprawionej — co najmniej z kasą, a docelowo ze wszystkimi stałymi przeszkodami na przejściu. Zachować wygląd.
2. Rozstrzygnąć błąd importu dźwigu i zapewnić powtarzalne zależności/importy. Przygotować wczesne sprawdzenie keystore oraz narzędzi Android.
3. Uzgodnić i przygotować spójną aktualizację Unity do poprawionej wersji, z osobnym cache i potwierdzonym obrazem buildowym.
4. Powtórzyć szybkie testy na **dokładnym nowym SHA**, przejrzeć diff i udokumentować nadal niezweryfikowane etapy.
5. Preferować najpierw kontrolę w edytorze bez tworzenia APK: generowanie, wszystkie trzy przejścia metra, arena, postać, zapis i ponowne otwarcie scen. Obecny BuildInternal nie udostępnia czystego trybu takiej walidacji: Build(false) tworzy również bundles/eksport. Wydzielić osobny tryb kontroli scen, jeśli ma być używany; nie uruchamiać go tu automatycznie.
6. Dopiero po osobnej zgodzie właściciela uruchomić właściwy workflow z gałęzi Miasta Mistrzów i pełnym nowym SHA. Po sukcesie: certyfikat i tożsamość APK, pobranie artefaktu, instalacja/aktualizacja na Androidzie, przejście metra i areny, sterowanie, animacje oraz pomiar RAM/FPS.

**Właściwa baza naprawy:** 38bece9f7b1feacd851b74441be55a5072d1db63, zawierająca 268f76d i kolejne poprawki.  
**268f76d ani 47c89f0 nie są aktualnym kandydatem.**  
**SHA następnego buildu musi być nowym potomkiem z rzeczywistą naprawą. W chwili audytu takiego zweryfikowanego SHA nie ustalono.**

Aktualne 65 testów lokalnych daje podstawę do dalszej naprawy i kontroli, **nie do ponawiania znanej wadliwej wersji przez kolejne kilkadziesiąt minut**.
