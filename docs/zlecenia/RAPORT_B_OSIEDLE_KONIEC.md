# Raport końcowy — wykonawca B, osiedle (gałąź `claude/osiedle-45b`, worktree `scratchpad/wtO-45b`)

Stan: HEAD `1487ca2`, drzewo czyste, NIC nie wypchnięte. Wszystko poniżej jest **niesprawdzone w Unity** (tylko lokalna
kompilacja Roslyn + bramka modułów; CI zweryfikuje). Kompilacja `scratchpad/kompiluj_45b.sh` i bramka
`scratchpad/brama_modulow.sh wtO-45b/unity-city/Assets` przed każdym commitem: **pusty wynik** (kod wyjścia 1 skryptu
kompilacji przy pustym wyniku = normalne, to końcowy `grep -v`).

## 1. Commity tej sesji (najnowsze na górze)

| commit | jedno zdanie |
|---|---|
| `1487ca2` | Dyskoteka/klub: konsola DJ bez klocka (cokół cofnięty, faza, front z 3 płycin z fugami, przelotki kabli). |
| `73d946a` | Kasyno — zaplecze: ławka holu, wieszak z haczykami i sejf z detalem zamiast prostopadłościanów. |
| `27426c4` | Sala bokserska: wspólne bryły z detalem (`BraxClaudeOsiedleMeble.cs`: ławka, szafka, sejf), ławki i wszystkie szafki szatni wymienione. |
| `ecbca34` | Audyt klocków `BRAXHORN_KLOCKI` (lista kandydatów, usuwane tylko luźne prymitywy „Cube”) + sprzedawca za ladą w Nutrition i przyjmujący w biurze warsztatu. |
| `e46ca94` | Jedno wejście — zabezpieczenia: główne drzwi wprost z kodu (`Klasa(id) == Wejscie`), drzwi wewnętrzne z kodu nigdy niezamykane, test łączności BFS i cofanie zamknięcia. |
| `e7f2515` | Jedno wejście dla gości: audyt drzwi, drugie wejście publicznego budynku zamknięte (ZAPLECZE / WYJŚCIE EWAKUACYJNE). |
| `588cd6f` | Audio stref: w środku pełna głośność, na zewnątrz tylko przez otwarte drzwi/otwory, za ścianą cisza; dźwięki salonu, warsztatu, wulkanizacji, sklepu stacji i myjni z syntezy. |
| `8b11980` | Warsztat + wulkanizacja: dwie strefy, koła 205/55 R16 × K w kodzie, podnośnik z autem, kanał wycięty w posadzce, maszyny wulkanizacji, złom z modeli Poly Haven. |
| `ac252b5` | Salon: główne stanowiska to Patryk (kopia bohatera z CI) i Monika (miejsce z postacią zastępczą). |
| `c3dd1ba` | Salon samochodowy piętrowy (narożnik B1 × Wiśniowa): szkło, talerze obrotowe, schody kamienne i ruchome w kodzie, antresola, plac z autami i tablicami 0 zł. |
| `b56de7f` | Golasy: stroje kąpielowe nigdzie, stroje imprezowe tylko w klubie, paleta ubrań bez barw skóry. |
| `e97a2c3` | E31: trasy pieszych nie przechodzą przez budynki (sieć dzielona na przeszkodach, kontrola kapsułą). |
| `51fda47` | Dźwięki pojazdów z syntezy (rozrusznik, zapłon, jałowy, zgaszenie, drzwi; auta AI ciche z zanikiem). |
| `3977589` | E3d/e: auta AI zwalniają przed zakrętem do ~15 km/h × K (test w symulatorze: za_szybko 0). |
| `6190315` | Całe oznakowanie poziome w wycięciach asfaltu (`BRAXHORN_DROGI_OZNAKOWANIE nakladki=0`). |
| `431f5ac` | E3b: łuki krawężników na skrzyżowaniach A. |
| `3daee0a` | E3a: ulice A poszerzone do 2 × 3.5 m × K tam, gdzie jest miejsce. |
| `847bbf5`, `287bfcf`, `6702a79`, `2bd9ee3` | E21 stacja paliw + myjnia, E32 warsztat (pierwsza wersja) i poprawki migotania. |

## 2. Logi do sprawdzenia w CI

- `BRAXHORN_LUDZIE_STROJE poza_plaza=0 odcien_skory=0 paleta_skora=0`
- `BRAXHORN_SALON …` (działka, zwalone, kondygnacje, `sprzedawcy=Patryk(model|zastępczy),Monika(zastępczy)`, `układ=ok`), `BRAXHORN_SALON auta=…`, `BRAXHORN_SALON_SCHODY_RUCHOME`, `BRAXHORN_DETALE obiekt=salon`
- `BRAXHORN_WARSZTAT_STREFY`, `BRAXHORN_WARSZTAT_KANAL`, `BRAXHORN_KOLA`, `BRAXHORN_WARSZTAT auto_na_podnośniku=…`, `BRAXHORN_DETALE obiekt=warsztat+wulkanizacja`
- `BRAXHORN_AUDIO_STREFY` (każda strefa: drzwi, otwory; `zza_sciany_bez_otworow` = sale bez znanych otworów — DO POPRAWKI)
- `BRAXHORN_WEJSCIA podwojne_przed=… podwojne=… zamkniete=… cofniete=…` + lista (`główne=kod|zgadnięte`, zamknięte, cofnięte (łączność)) — **przejrzeć listę** (heurystyka budynków)
- `BRAXHORN_KLOCKI` (kandydaci = partie bez tekstury nazwane jak mebel/pojazd/maszyna), `BRAXHORN_SKLEPY_OBSADA`
- `BRAXHORN_PIESI_TRASY przez_budynek=0`, `BRAXHORN_DZWIEKI_POJAZDOW`, `BRAXHORN_RUCH_TEST` (za_szybko), `BRAXHORN_DROGI_OZNAKOWANIE nakladki=0`
- Ujęcia (blok B w `unity-tools/claude/region_shots.mjs`): `B45_salon_*`, `DETAL_salon_*`, `DETAL_warsztat_*`, `DETAL_wulkanizacja_*`, `DETAL_ludzie_stroje_*`, `DETAL_sklep_nutrition_sprzedawca`, `DETAL_boks_*`, `DETAL_kasyno_*`, `DETAL_dyskoteka_konsola_dj`.

## 3. Czego NIE zrobiłem (z plikami i miejscem w kodzie)

Ścieżki względem `wtO-45b/unity-city/Assets/`.

- **E16 — rozsuwanie się pieszych w ruchu + `BRAXHORN_PIESI_TEST`.** `Claude/Runtime/BraxPieszy.cs`: `Idz()` / `Punkt()` — omijanie (`Przeszkoda`/`Zajete`) działa tylko przy `blisko` (przy kamerze); dodać odpychanie od sąsiadów z `wszyscy` (promień ~2r + 0.25, zwolnienie, gdy ktoś przed nami < 1.2 m, cofnięcie ruchu kończącego się w cudzym promieniu). Symulacja 120 s (nakładania = 0, wejścia_w_budynek = 0) w `Editor/Claude/BraxClaudeOsiedleLudzieMiejsca.cs` po `PrzechodnieRocketbox`. Sloty przy zebrach już są (commit `49370fe`) — rozbudować, nie dublować.
- **E20b — ganki i schody z kolizją, kamera.** Niezaczęte. Wille i domy w `Editor/Claude/BraxClaudeOsiedle.cs` (sekcje willi), kontrola kolizji w `Editor/Claude/BraxKontrolaKolizji.cs`. UWAGA: nazwa logu `BRAXHORN_WEJSCIA` jest teraz zajęta przez audyt jednego wejścia — E20b potrzebuje innej (np. `BRAXHORN_GANKI`).
- **E23 — przystanki (A ≥ 3, B ≥ 2).** Niezaczęte. Plan i punkty P-A1…3 i P-B1…4: `scratchpad/plan/ROZBUDOWA_B_PLAN.md` / `ROZBUDOWA_B_PLAN.png`. Nowy plik `Editor/Claude/BraxClaudeOsiedlePrzystanki.cs` + `Guard` po `DrogiA` w `BraxClaudeOsiedle.cs`; zatoki i oznakowanie w wycięciach (`OznakowanieWWycieciach`, `WytnijZGrupy`); jezdnia zatoki dopisać do `NaJezdniAut` (`BraxClaudeOsiedleAuta.cs`).
- **E24 — Żuk** (szyby, czyste lakiery, wnętrze, polska tablica, dalej auto NPC). Niezaczęte. Szablon `car_zuk`: `Editor/Claude/BraxClaudeOsiedleAutaModel.cs` (`PrzygotujAuto`), waga w `BraxClaudeOsiedleAuta.cs` (`AutaWaga`).
- **Klocki pozostałe.** Zrobione: sala bokserska, kasyno zaplecze, konsola DJ, warsztat. Reszta to lista z logu `BRAXHORN_KLOCKI`. Przykłady: regały marketu z gładkiego prostopadłościanu w `BraxClaudeOsiedle.cs` ok. linii 2501 (`kind == "market"`); Codex `EstateMesh_300` (84.8, 0, −36.5) w kasynie. Bryły do użycia: `LawkaX`, `Szafka`, `Sejf` w `Editor/Claude/BraxClaudeOsiedleMeble.cs`; modele: `BraxOsiedleAssety.ModelWnetrz(...)` (paczka `osiedle_wnetrza`), `Model(id)` (`fetch_osiedle.py`), `ModelPH(id)` (`fetch_polyhaven.py`, blok „osiedle — warsztat”).
- **Sale bez dźwięku stref.** Mają: salon, warsztat, wulkanizacja, sklep stacji, myjnia + muzyka kasyna, siłowni A, dyskoteki. Brak: szpital, komisariat, sala bokserska, arena, lokale pierzei B, wille. Dopisać do listy `strefy` w `Editor/Claude/BraxClaudeOsiedleAudioStref.cs` → `AudioStref()` (prostopadłościan sali, `synteza` z `Claude/Runtime/BraxDzwiekiStref.cs` — dodać rodzaje, otwory stałe). Sala bez drzwi/otworów zostaje w starym trybie zza ściany (log DO POPRAWKI).
- **Szpital E33.** Nieruszony (kod szpitala w `BraxClaudeOsiedle.cs` — szukać „szpital” / „hospital”).
- **S1/S2.** Czeka na decyzję właściciela. Jeśli pełne 9.06 m: scalić chodniki S1–Wiśniowa i S2–Bydgoska w jeden szeroki chodnik bez przerwy 0.2 m — `Editor/Claude/BraxClaudeOsiedleDrogiPoszerzenie.cs` (`PoszerzUliceA`, `szerOpis`) + chodniki w `BraxClaudeOsiedleDrogiA.cs`.
- **Salon.** Brak windy szklanej (opcja). Monika = postać zastępcza (`BraxClaudeOsiedleSalon.cs` → `SalonObsada`, `RolaMonikaZastepcza`, obiekt „Salon: stanowisko Monika”). Patryk = kopia bohatera (`SalonPatrykKopia`).
- **Koła.** Model ze Sketchfaba (właściciel ściągnie) podmienić w `Editor/Claude/BraxClaudeOsiedleWulkanizacja.cs` → `KolaPrzygotuj()` / `Kolo()` (jedna funkcja stawia wszystkie koła).

## 4. Ryzyka

- **Schody ruchome** (`Claude/Runtime/BraxSchodyRuchome.cs`), nowy kod w grze, nietestowany:
  - gracz przesuwany przez `CharacterController.Move` w strefie stopni, pod stopniami pochylnia z kolizją;
  - pasażer NPC wraca na dół dopiero, gdy go nie widać;
  - poręcz zmienia offset tekstury materiału współdzielonego.
- **Talerze obrotowe** (`Claude/Runtime/BraxObrotowyPodest.cs`): auto jest dzieckiem talerza i obraca się z kolizją.
- **Kanał w warsztacie:** wycięty grunt Codexa (`CutVolumes`), batch „Podłoże” i kolizje (`PodzielKolizje` na `colliders` i `physics`) — sprawdzić, czy nie ma dziury obok kanału.
- **Audyt wejść:** wyłącza komponent drzwi; przynależność do budynku jest zgadywana (Linecast między punktami wewnętrznymi). Zabezpieczenia: drzwi z kodu nigdy, łączność BFS, budynki z listy bez wejścia z kodu — nic.
- **Audyt klocków:** usuwa prymitywy „Cube” o wymiarach mebla (filtry: znaczniki, kolizje, punkty, gracz).
- **`fetch_polyhaven.py`:** zmiana listy zmienia klucz pamięci podręcznej CI, więc pierwsze pobieranie potrwa dłużej. 8 nowych modeli sprawdzonych w API (1k gltf jest).
- **Modele NC:** auta salonu i warsztatu to w większości modele NC („DO WYMIANY PRZED SPRZEDAŻĄ”).
- **E31 (`e97a2c3`):** sieć pieszych dzielona na przeszkodach — możliwe mniej tras albo krótsze trasy (log `BRAXHORN_PIESI_TRASY`).

## 5. Sprawdzone pułapki (dla następnego wykonawcy)

- **Lokalna kompilacja** (`kompiluj_45b.sh`, Roslyn bez pełnego Unity 6) filtruje błędy `FindObjectsByType`/`FindAnyObjectByType`. Przy `var x = Object.FindObjectsByType<T>(…)` typ jest wtedy nieznany i lecą mylące błędy dalej. Deklarować typ jawnie: `T[] tab = Object.FindObjectsByType<T>(…)`.
- **CS0136 (konflikt nazw lokalnych)** w długich funkcjach (`gz`, `x1`, `drzwi`) — w klasie częściowej wiele nazw jest już zajętych.
- **Kolejność kroków w `Build()`:**
  - stacja (5604) → warsztat → salon → muzyka/audio stref (po `MusicZones`);
  - auta (`RuchNaOsiedlu` w `KraniecMapy` ~5671; tu `SalonAuta`, `WarsztatAuto`) → ludzie (`LudzieNaOsiedlu` 5693) → obsady (`StacjaKasjer`, `WarsztatMechanicy`, `SalonObsada`, `ObsadaSklepow`);
  - audyt wejść i klocków przed `Flush()`;
  - NPC tylko po `LudzieNaOsiedlu` (`rbPostacie`).
- **Wycinanie zamiast nakładania:**
  - farba = trójkąty odjęte od asfaltu (`OznakowanieWWycieciach`); otwory w posadzce = prostokąty wokół otworu (`WarsztatPosadzka`);
  - `WytnijZGrupy` tnie partie generatora dokładnie (Sutherland–Hodgman);
  - `CutVolumes` tnie siatki Codexa i dzieli kolizje ≤ 2.2 m;
  - „Original virtual floor” (y −0.18…0) nie jest cięta przy y ≥ 0.01 — tniemy ją świadomie tylko pod kanałem.
- **Bez wspólnych lic:** obudowy z `ObwodBez`, pasy/fugi jako osobne prostopadłościany w warstwie, szkło 1 cm cofnięte w ramie. Przy ruchomych stopniach bieżnia jest krótsza od odstępu o 6 mm, żeby sąsiednie stopnie nie nachodziły na siebie.
- **Assety:** siatki i materiały obiektów osobnych (ruchome stopnie, talerze, poręcze, koła) muszą być zapisane jako asset (`AssetDatabase.CreateAsset`; u mnie `SiatkaSalonu.Zapisz`, `SalonZapisz`) — inaczej znikają przy zapisie sceny. Wzór dla siatek wstawek: `BraxZapisAssetu` (runda 2, `adfd72e`).
- **Skala:** modele Poly Haven są w metrach — × K przy wstawianiu (`SalonModel`, `WkModel`); `Put()` sam nie skaluje.
- **Strefy scen:** osiedle B i port są doczytywane — drzwi tam nie mogą być w `BraxCityWorld.doors` (odwołanie między scenami zeruje się przy zapisie). Do drzwi w grze używać `BraxCityDoor.All`.
- **Korony drzew Wiśniowej** sięgają do x −49.3 (y 2.5–7.8): bryła salonu od x −49.1, cięcie pasa zachodniego tylko do 2.3 m.
- **Prywatne modele** (Patryk) nigdy w repo — tylko kopia obiektu zbudowanego w CI (`GameObject.Find("BraxhornPlayer")` → `Patryk`).
- **Testy CI używają drzwi:**
  - drzwi frontowe: `casino-front-door`, `club-front-door`, `hospital-front-door`, `police-front-door`, `boxing-front-door`, `arena-front-door`;
  - przejścia i trasy: `casino-zaplecze-hall` (kasyno → zaplecze, `d2b6264`), `gym-rear` (jedyne wejście), trasy `nova-back-room` / `club-backstage`;
  - takich drzwi nie zamykać.

## 6. Pliki do pokazania właścicielowi

- `scratchpad/plan/SALON_OSIEDLE_RZUT.png` — rzut salonu (parter + antresola)
- `scratchpad/salon/kolo_podglad.png` — podgląd siatki koła 205/55 R16 × K
- `scratchpad/plan/rb_stroje_arkusz.jpg` — tekstury strojów Rocketbox (golasy)
- `scratchpad/plan/STACJA_WARSZTAT_RZUT.png`, `scratchpad/plan/ROZBUDOWA_B_PLAN.png`

## 7. Runda 6 (3.11, po 02:18) — dopisek

Stan: HEAD `a5e0413` na `claude/osiedle-45b`, drzewo czyste, nic nie wypchnięte; kompilacja `kompiluj_45b.sh` i bramka
`brama_modulow.sh` przed każdym commitem: pusty wynik. Wszystko **niesprawdzone w Unity (CI)**.

### Commity rundy 6

| commit | jedno zdanie |
|---|---|
| `7096627` | E23: przystanki z nazwami (OSIEDLE A 1/2/3, OSIEDLE B 1/2), nowe A 3 i B 2, autobus z kodu (awaryjny) i `BraxAutobus`: jazda po trasie ruchu, postój drzwiami przy wiacie, wysiadanie/wsiadanie, auta AI widzą autobus. |
| `9134b4e` | Billboardy osiedla 02, 04, 05, 13, 14, 08 tylko na nośnikach (citylight, rama na płaskim murze, tablica na słupach), brak grafiki = pusta rama. |
| `a5e0413` | Autobus z modelu `miasto_pojazdy/autobus.glb` (12 m × K × skala dopasowana do łuków), kierowca w środku, skrzydła drzwi z węzłów modelu, syk drzwi; autobus z kodu zostaje awaryjny. |

### Logi do sprawdzenia w CI (runda 6)

- `BRAXHORN_PRZYSTANKI A=… B=…` (wymóg A ≥ 3, B ≥ 2)
- `BRAXHORN_AUTOBUS model=glb|kod`, `skala_dopasowana=…`, `kierowcy=…`, `pasażerowie=…`; `BRAXHORN_AUTOBUS_TRASA poza_jezdnią=…`
- `BRAXHORN_BILLBOARDY postawione=… brak_grafiki=…` (lista miejsc; „PUSTA RAMA” = brak pliku NN.jpg)
- Ujęcia: `DETAL_przystanek_*`, `DETAL_billboard_*`

### Czego NIE zrobiłem (runda 6)

- **E24 Żuk** — model `car_zuk.glb` to jedna siatka z JEDNYM materiałem i atlasem tekstur w KTX2 (Basis): szyby i lakier są tylko w teksturze, bez dekodera nie da się ich wydzielić. Do zrobienia: zrzut atlasu z Unity (RenderTexture + ReadPixels) albo dekoder Basis, klasyfikacja trójkątów po kolorze tekstury (szyby → osobna podsiatka ze szkłem, lakier → czyste warianty), wnętrze z kodu (fotele, deska, kierownica) i polska tablica. Miejsce: `Editor/Claude/BraxClaudeOsiedleAutaModel.cs` → `PrzygotujAuto` (szablon `car_zuk`).
- **Audio stref pozostałych sal** (szpital, komisariat, sala bokserska, arena, lokale pierzei B, wille) — dopisać prostopadłościany sal do listy `strefy` w `Editor/Claude/BraxClaudeOsiedleAudioStref.cs` → `AudioStref()`, rodzaje dźwięku w `Claude/Runtime/BraxDzwiekiStref.cs`.
- **E16** (piesi się mijają) i **E20b** (ganki i schody z kolizją; log NIE `BRAXHORN_WEJSCIA` — zajęty) — bez zmian względem sekcji 3.
- **Linia autobusowa A↔B** — drogi przez parki (E17) nie ma; są dwie linie: A (pętla Poznańska–Bydgoska: A 1, A 3) i B (ulica główna, po autobusie na kierunek: B 1, B 2). Po zbudowaniu E17 wystarczy nowa trasa w `BraxTrasyAut.TrasyOsiedla` i jej nazwa w `Autobusy()` (`Editor/Claude/BraxClaudeOsiedleAutobus.cs`).

### Ryzyka (runda 6)

- **Autobus w łukach:** 12 m × K = 15.5 m na łukach 90° (promień pasa ~9.5 m) i na rondzie. Skala jest dobierana przed budową sprawdzeniem nadwozia na jezdni (1.0 → 0.7), a wynik trafia do logu. Przy skali < 1 autobus jest proporcjonalnie mniejszy. Tył nadwozia może zahaczać o narożnik chodnika — do obejrzenia w filmie.
- **Przystanki bez zatoki:** chodniki A mają 2.1 m, więc przystanki są przykrawężnikowe (P-17 na pasie). Autobus stoi na pasie, a auta AI za nim hamują (obrys zewnętrzny); wyprzedzania nie ma.
- **Drzwi modelu:** węzły z „door/drzwi” są przesuwane w układzie autobusu. Jeśli model ma drzwi po lewej albo inaczej podzielone, ruch może wyglądać źle. Wtedy w `AutobusZModelu` trzeba wyłączyć `skrzydla` (zostaje sam syk drzwi).
- **Kierowca:** siedzisko liczone z wymiarów (1.0 m nad jezdnią, przód po lewej). Przy innym wnętrzu modelu może wisieć albo przenikać — do obejrzenia.
- **Billboardy:** miejsca z list kandydatów są sprawdzane fizyką. Jeśli żaden kandydat nie przejdzie, pozycja wypada (log). Ramy stawiam tylko na płaskim, nieszklanym licu.
- **Pasażerowie:** wsiadanie to znikanie przy drzwiach, wysiadanie to pojawienie się przy drzwiach i 1.5 m kroku. Grupy czekających i wysiadających zamieniają się po każdym postoju.
