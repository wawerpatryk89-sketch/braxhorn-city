# Wyspa premium i BRAXHORN ARENA — zmiany źródłowe, 4.11.2026

**Status: kod zapisany do przeglądu; odbiór P0 otwarty. Nie potwierdzono gotowości do testowego buildu według warunków właściciela.** Nie uruchomiono Unity, kompilacji APK/WebGL, workflow ani gry na telefonie. Nie powstały nowe zdjęcia ani nagranie przejścia przez zmienioną arenę. Kontrola składni nie jest kompilacją C#, importem modeli ani testem fizyki Unity.

## Pochodzenie i bezpieczne włączenie

- Repozytorium: `wawerpatryk89-sketch/braxhorn-city`.
- Gałąź zmian: `codex/wyspa-arena-premium-20261104`.
- Baza wskazana przez właściciela: `codex/miasto-naprawy-filmow-20261104`.
- HEAD odczytany przed pracą: `24d210d128dc1622772f32130ea1604af3dd4de0`.
- Nowsza baza przyjęta przed zapisem: `65ab55a9e831d289017f47c9875b193b41a26d1c`. W międzyczasie zmieniły się workflow oraz instrukcje APK FIRST. Zachowano je; do workflow dodano tylko dwa kroki przygotowania źródeł zawodników. Nie zmieniono wyzwalaczy ani warunków uruchomienia jobów.
- Zawodnicy **z obozu**, zgodnie z korektą właściciela: KADE i ZYRO. Kod importu, manifest, shader i przygotowanie zasobów skopiowane z `gpt/oboz`, commit `b98c5630b8161f62508c2e459356b9926d1836ab`. Nie zmieniano gałęzi ani scen obozu, osiedla i miasta.
- Wersja docelowa: Unity `6000.0.58f2`. Zachowano scenę `BraxhornCity_Wyspa` i obecny mechanizm doczytywania.
- Ta gałąź służy do przeglądu i późniejszego włączenia zmian. Obecna lista dozwolonych gałęzi ręcznego workflow pozostaje własnością wykonawcy miasta; nie rozszerzano jej i nie wykonywano dispatch. Przed ewentualnym zatwierdzonym testem należy ponownie sprawdzić HEAD i warunki workflow.

## Dwa filmy a kod

Przeanalizowano oryginalne nagrania właściciela, a nie wygenerowane wizualizacje:

| Materiał | Co rzeczywiście widać | Ograniczenie porównania |
|---|---|---|
| `a87108c5-ac17-4aef-a032-25a7e2836c18.mp4`, 72,565 s | Ośrodek, istniejące domki, alejki i plac zabaw; jednolite powierzchnie gruntu i trawników | Brak identyfikatora buildu. Nie przypisano filmu do konkretnego SHA |
| `ecc94851-d630-4460-9f22-e5a341f97291.mp4`, 114,493 s | 0–32 s: podróż łodzią; około 32–56 s: przystań i dojście; 64–72 s: fasada i szyld „ARENA BRAXHORN”; około 80 s: hol i bramki; 88–112 s: wnętrze, klatka bez dwóch walczących zawodników | Film pokazuje wejście do hali. Nie jest dowodem całkowicie zablokowanych drzwi. Nie potwierdza stanu najnowszych źródeł |

Kontaktowe arkusze klatek służyły analizie lokalnej. Nie są dowodem wyglądu poprawek. Filmów i prywatnych modeli nie dodano do publicznego repozytorium.

## Fakty, diagnoza i poprawki

### Wejście i arena

Aktywna ścieżka to `ArenaPremium()`. Historyczna `Arena()` pozostaje wyłączona. Przejrzano `ApBryla`, `ApWejscie`, `ApPlacPrzed`, `ApHol`, trybuny, klatkę, WC, tunel i szatnie.

1. Dwie pary drzwi i collider słupka zajmowały środek dojścia. Portal ma teraz jedną szeroką parę drzwi automatycznych. Boczne ościeża pozostają poza światłem przejścia; zachowano istniejący szklany daszek i architekturę fasady. Nie wyłączono zbiorczo kolizji ścian.
2. Wizualna rampa i jej obrócony collider pudełkowy miały osobno wyliczaną geometrię. Teraz MeshCollider używa tej samej siatki co nawierzchnia; rampa ma wznios 0,15 m na 1,8 m i dochodzi do poziomu holu.
3. Szyldy wejściowe i oznaczenia używają kolejności **BRAXHORN ARENA**. Dodano dyskretne pasy światła portalu, oznakowanie dojścia i dwa kosze poza osią przejścia. Granit, beton, kamień, płytki i drewno wymagają tekstur źródłowych; brak tekstur jest błędem.
4. Trzy istniejące modele bramek zamiast czterech ustawiają środkowy kanał na osi wejścia. Generator wymaga prawdziwych modeli i kompletnych ruchomych skrzydeł. Usunięto cichy zastępczy wariant bramek. Ochronę przesunięto na boki.
5. Zachowano trybuny, krzesełka, loże VIP, klatkę, logo maty, telebim, dach, WC i szatnie. Pierwszy rząd z interakcją siedzenia nie jest obsadzany przez kibiców. Stołki, trenerzy i barierki tunelu mają korektę położenia dla przejścia wokół klatki. Sędzia stoi poza osią drzwiczek.
6. Globalny krok wygaszania materiałów obejmował także oprawy areny. Dodano końcowe przywrócenie umiarkowanej emisji wyłącznie oznaczonym materiałom areny na wyspie. Światła trybun ograniczono z sześciu do dwóch; nie dodano migania ani nowych efektów postprocessingu.
7. Istniejąca kamera ma SphereCast i zabezpieczenia odległości. Nie deklarujemy, że nowe ustawienie rozwiązało każdy przypadek kamery: wymaga przejścia i obrotów przy fasadzie w Unity.

### Pokaz KADE / ZYRO z obozu

- Skopiowano istniejący most Humanoid → Legacy, oryginalny importer i materiały PBR obozu. Nie podmieniano siatek ani strojów. Obie postacie mają rękawice potwierdzone w manifeście obozu.
- Lokalnie zweryfikowano SHA-256 wszystkich **25 plików, 131 155 333 bajty**. Kopia obejmuje dwa modele, ich tekstury oraz wymagane źródła animacji; nie kopiuje zabudowy obozu. Surowe pliki są ignorowane przez Git.
- Przy późniejszym przygotowaniu sceny używany jest ten sam import co w obozie. W scenie pokazu pozostaje po pięć klipów: garda, krok, kombinacja, unik, trafienie. Każdy kanał Transform jest sprawdzany względem szkieletu. Klipy wynikowe są oddzielnymi kopiami; XZ korzenia i bioder nie przesuwają pary poza choreografię.
- Zapętlona choreografia zamienia role, utrzymuje wzajemny dystans i ogranicza pozycje w klatce. Reakcja na trafienie zależy od bliskości dłoni i głowy po próbkowaniu animacji. Nie dodano AI walki z graczem. Aktualizacja animacji zatrzymuje się poza zasięgiem obserwacji.
- **Ograniczenie źródła:** manifest obozu nadal zawiera `previewHeight=1.8`, nieuzgodniony docelowy wzrost i `qualityExceptionApproved=false`. Zachowano obecny obozowy tryb `sourceReviewOnly` i skalę świata 1,294. To nie jest nowa akceptacja jakości. Import, stopy na macie, rzeczywisty zasięg ciosów, przenikanie kończyn i płynność przejść wymagają oględzin w Unity. Zgodność hashy tego nie potwierdza.

### Plaża, morze i ośrodek

- Zachowano geometrię terenu i istniejące mieszanie piasku suchego, wilgotnego, trawy i skały. Generator wymaga rzeczywistych tekstur z dotychczasowego zestawu CC0. Nie dodano drugiej płaskiej powierzchni piasku.
- Maska piasku jest zapisana jako wagi trójkątów przy generowaniu. Ślady powstają przy opadnięciu kości stopy, podczas ruchu po gruncie, zgodnie z kierunkiem postaci. Shader pokazuje przód podeszwy, obcas i bieżnik. Limit: 24 ślady, zanikanie przez 45 s, bez deformacji siatki i dodatkowego draw calla. Reset po zmianie sceny/dużym przemieszczeniu. Synchronizacja z krokiem nadal wymaga testu animowanego Braxa.
- Morze korzysta z osobnej kopii obecnego materiału wody: delikatne animowane normalne, bez unoszenia ogromnych pól siatki oceanu. Dodano pas piany na istniejącym shaderze brzegu i drobne muszle scalone w jeden materiał z LOD. Zachowano kamienie i roślinność.
- Ambient ma trzy źródła: morze, wiatr i tło gali. Najbliższy fragment brzegu jest wyliczany co 0,15 s, głośność zmienia się płynnie, a we wnętrzu areny morze jest silnie wyciszane. Referencje i źródła należą do sceny wyspy.
- Audio jest **autorską syntezą**, nie pobranym nagraniem kibiców lub morza. Pętle 16 s/mono/22,05 kHz z przenikaniem końca w początek powstają przy przygotowaniu sceny; importer używa Vorbis. Synteza głosów i oklasków jest przygotowana w kodzie. **Nie wykonano odsłuchu docelowej pętli ani jej dekodowania na Androidzie; jakość premium i brak słyszalnego łączenia pozostają do oceny.** Nie publikowano cudzych nagrań bez licencji.
- Zachowano pomost, domki, recepcję, sklep, alejki, plac zabaw, leżaki, parasole, bary, ratowników i atrakcje. Ich generowanie ma ostrzejszą obsługę błędów. Poprawki ośrodka są ograniczone do wykończenia i kontroli istniejącego układu; nie wykonano nowego projektu zabudowy ani nie potwierdzono wizualnego odbioru całości.

### Błędy generatora i oddzielna scena

`Krok()` areny już nie zamienia wyjątków w samo ostrzeżenie. Błędy terenu, wejścia, klatki, tekstur, bramek, plaży, źródeł pokazu i ambientu przerywają ten etap oraz zapisują nazwę elementu i wyjątek w `unity-city/Logs/wyspa-p0-odbior.json`. Błąd nie może prowadzić do pozytywnego odbioru wyspy.

Nowy odbiór danych obejmuje brakujące skrypty, siatki, materiały, shadery, zerwane referencje tekstur, collidery siatek, kości, klipy, siedziska, drzwi i obsadę NPC. Punkty tras są zapisane w scenie, a nie tylko w statycznym słowniku generatora. Przed wydzieleniem sceny wykonywana będzie kontrola końcowa; kontrola przed pakowaniem obejmuje także sceny odtworzone z cache, żeby stary checkpoint nie omijał wymagań.

Próba geometrii otwiera tymczasowo skrzydła i odtwarza stan ich blokad, sprawdza podłoże i kapsułę na trasach wejścia, holu, bramek, obejścia klatki, tunelu, WC i szatni, po czym przywraca zmiany w `finally`. **To przygotowany kod kontroli, nie wykonany test poruszania CharacterControllerem.** Może ujawnić dalsze przeszkody podczas pierwszego uruchomienia Unity.

Przejrzano `BraxClaudeObszary.cs`: istniejąca twarda kontrola referencji między scenami pozostaje. Nie zmieniano progów doczytywania wyspy (200 m) i zwalniania (280 m). Zawodnicy i ich animacje należą do sceny wyspy. Lekki akwen podróży pozostaje we własnym istniejącym korzeniu; nie przeniesiono całej wyspy do sceny startowej.

## Wykonane kontrole i rzeczy do odbioru

Wyniki maszynowe: [wyspa-premium-static-20261104.json](wyspa-premium-static-20261104.json).

| Kontrola | Wynik |
|---|---|
| `git diff --check` | PASS |
| 5 testów Python selekcji/ekstrakcji zasobów | PASS: dobór dwóch zawodników, brak importu otoczenia, błędny hash, brak pliku, obca/duplikowana ścieżka |
| SHA-256 prywatnych źródeł KADE/ZYRO | PASS, 25/25 plików |
| Parser tree-sitter C# | Bez błędów składni w 132 lokalnych plikach; repozytorium bazowe ma 270 plików C#. Nie jest to pełna kompilacja |
| 53 workflow | Brak push/create pasującego do gałęzi zmian; brak wywołania dispatch |
| Import Unity, walidator geometrii, shader compiler | NIE URUCHOMIONO |
| APK/WebGL, FPS, RAM urządzenia, wielkość APK | NIE URUCHOMIONO / BRAK POMIARÓW |
| Nowe zdjęcia i film przejścia | NIE WYGENEROWANO |

Przed zamknięciem P0 pozostają:

1. Import kompletu miasta i dwóch zawodników w Unity 6000.0.58f2; sprawdzenie shaderów, referencji i raportu generatora. Usunięcie ewentualnych błędów wykrytych przez nową kontrolę. Nie zastępować brakujących modeli uproszczonymi bryłami.
2. Ręczne przejście gracza: cumowanie → pomost → plac → otwierające się drzwi → hol → bramki → trybuna/siedzenie → WC → tunel/szatnia → klatka → wyjście. Bez teleportowania; sprawdzić obroty kamery przy fasadzie. Zarejestrować trzy wymagane widoki oraz ciągły film.
3. Obejrzenie KADE/ZYRO przez kilka pełnych pętli: skala, gardy, ciosy, uniki, kontakt i reakcje, stopy, brak przenikania i przekroczenia klatki. To warunek, nie zadeklarowany sukces.
4. Przejście i bieg po suchej/mokrej plaży, zakręty i zatrzymanie: moment i kierunek odcisku, wygasanie, limit, brak śladów na deskach/trawie i brak migotania. Ocena materiałów, wykończenia domków i całej plaży.
5. Odsłuch morza/wiatru/tła gali, łączenia pętli i tłumienia po wejściu do areny. Ocena, czy synteza zapewnia oczekiwaną jakość dźwięku.
6. Po oddzielnej zgodzie na konkretny test: Android, minimum dwie podróże miasto–wyspa–miasto, pomiar FPS/frametime, RAM procesu, czasu wejścia i zwalniania. Dodane logi `BRAXHORN_WYSPA_STREAM` i `BRAXHORN_WYSPA_POMIAR` są instrumentacją; przydzielona pamięć Unity nie jest pełnym RAM procesu Androida. Sprawdzić czarne materiały, stripping shaderów, draw calle i znikające LOD.

## Pliki

Zmodyfikowane:

- `.gitignore`
- `.github/workflows/claude-region-build.yml` — wyłącznie przygotowanie źródeł w istniejących jobach, z zachowaniem najnowszych zmian APK FIRST.
- `unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspa.cs`
- `unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaArena.cs`
- `unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaDomki.cs`
- `unity-city/Assets/Editor/BraxGymBuild.cs`
- `unity-city/Assets/Claude/BraxObszary.cs`
- `unity-city/Assets/Claude/Runtime/BraxSladyPiasek.cs`
- `unity-city/Assets/Claude/Shaders/NabrzezeTeren.shader`

Nowe pliki C# (każdy z `.meta`):

- `unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaWykonczenie.cs`
- `unity-city/Assets/Editor/Claude/Region/BraxWyspaOdbior.cs`
- `unity-city/Assets/Claude/Runtime/BraxArenaPokaz.cs`
- `unity-city/Assets/Claude/Runtime/BraxPiasekPowierzchnia.cs`
- `unity-city/Assets/Claude/Runtime/BraxWyspaAmbient.cs`
- `unity-city/Assets/Claude/Runtime/BraxWyspaKontrola.cs`

Kopie istniejącego mostu obozu i jego metadanych (bez edycji sceny obozu):

- `unity-city/Assets/Editor/Oboz.meta`
- `unity-city/Assets/Editor/Oboz/BraxObozCharacters.cs` + `.meta`
- `unity-city/Assets/Editor/Oboz/BraxObozMonikaMotion.cs` + `.meta`
- `unity-city/Assets/Editor/Oboz/BraxObozImport.cs` + `.meta`
- `unity-city/Assets/Editor/Oboz/CharacterManifest.json` + `.meta`
- `unity-city/Assets/Oboz.meta`
- `unity-city/Assets/Oboz/ObozCharacter.shader` + `.meta`
- `unity-tools/oboz/assets.py`
- `unity-tools/oboz/motion_candidates.py`
- `unity-tools/oboz/motion-candidates.json`
- `unity-tools/oboz/private-inputs.json`

Nowe przygotowanie i dokumentacja:

- `unity-tools/wyspa/prepare_fighters.py`
- `unity-tools/wyspa/tests/test_prepare_fighters.py`
- `docs/audyty/WYSPA_PREMIUM_20261104.md`
- `docs/audyty/wyspa-premium-static-20261104.json`

`BraxClaudeObszary.cs`, sterownik kamery i pozostałe źródła wymienione w analizie były czytane bez zmian. Pełny commit stanowi dowód zapisania kodu; nie stanowi dowodu ukończonej wyspy w działającej grze.
