# Braxhorn City — drugie nagrania 4.11.2026 — audyt wyłącznie MIASTA (Unity 6)

Źródło: siedem załączonych filmów MP4 przekazanych przez właściciela. Dwa pliki `ef7e5189-df75-400e-9a72-c88dc7834b48*.mp4` zawierają ten sam film (6 unikalnych sekwencji). Poniższe obserwacje dotyczą starego APK z filmów, NIE nowej kompilacji z gałęzi napraw. Czasy przybliżone (od 0:00).

## Nowa runda oględzin
| Film | Oględziny | Co trzeba naprawić/odebrać |
|---|---|---|
| v1, 0:00–0:08 | Dach Tower, barierki, panorama miasta i góry na horyzoncie. | Winda, obrys i zasięg panoramy; panorama 3D istnieje w kodzie, jakość po zmianie farClip=580 m do testu |
| v1, 0:12–0:44 | Przy próbie windy kamera przez długi czas wypełniona ciemną ścianą i panelem drzwi. | Kamera w ciasnym szybie, kolizja wyjścia, aktywacja przycisku i przejazd — **niepotwierdzone po naprawie** |
| v2, 0:20–0:44 | Supermarket BRAXHORN ma widoczne regały za szybami, ale czarne ogrodzenie przed witryną i wejściem. | Wyciąć tylko przeszkodę w osi drzwi, nie usuwać reszty ogrodzenia; sprawdzić dostęp i sprzedaż |
| v3, 0:00–0:16 | Rondo z zieloną wyspą, ulice i sklepy, widać zakrzywione obrzeże ale również nieestetyczne styki. | Kwadratowe płyty pod okrągłymi wyspami usunięte z generatora Rondo/Pętla Płn./Zach.; reszta nawierzchni do testu |
| v3, 0:16–0:44 | Pasaż ze szklanymi fasadami i wejściami, przechodnie, ławki; widoczne wystające podpory. | Sprawdzić rzeczywiste przejścia między fasadami, kolizje witryn, materiały i wnętrza |
| v3, 0:52–1:08 | Nowe kwartały, duży ciemny parter, odsłonięte rusztowania i rozległy trawnik. | Dokończyć materiały i szkło, nie maskować ścian atrapą; identyfikacja nadmiarowych elementów modelu |
| v4, 0:00–1:08 | Nowe ulice pomiędzy budynkami, czerwone elewacje w rusztowaniach, bardzo ciemne przeszklone partery i szczeliny. | Weryfikacja importu PBR, szyb, geometrii fasad, kolizji budynków i przebiegu chodników |
| v5, 0:00–1:28 | Dalsze kwartały z rusztowaniami, czarne partery, nierówne wykończenie i małe wyspy zieleni przy drodze. | Nie dublować materiałów; kontrola małych trawników na asfalcie, brakujących tekstur i niedopracowanych przejść |
| v6, 0:32–1:00 | Prostokątne jaskrawo zielone plamy w jezdni przed przejściami, miejscami nienaturalne krawężniki. | Usuwanie wyłącznie niewielkich trawników faktycznie leżących w obrębie asfaltu, zachowanie parków i pasów zieleni |
| v6, 1:08–1:32 | Tramwaj CAF zamknięty na krótkim torze z kozłami oporowymi, nie funkcjonuje jako transport miejski. | **T1:** linia bez cofania ulicami Tramwajowa → C0 → Nowojorska → C3 → Tramwajowa, 6 przystanków, ruch + działająca interakcja Braxa, minimum 1 wagon |

## Stan wykonanych poprawek kodu po tej rundzie

- Rondo i dwie podobne pętle: usunięte kwadratowe betonowe podkłady pod okrągłymi trawnikami.
- Asfalt: dodatkowy podział nakładających się prostokątów na rozłączne płaty (bez nakładania na dawną powierzchnię).
- Drobne trawniki: selektywne zdjęcie jedynie obiektów w pełni na jezdni; bez ingerencji w duże zieleńce.
- Supermarket: przygotowane selektywne wycięcie ogrodzenia w świetle dojścia, kontrola cięć w logu.
- Budynki nowej dzielnicy: kolizje powiązane z rzeczywistym obiektem budynku, żeby doczytywanie/zwalnianie nie zostawiało niewidocznych ścian.
- Tramwaj: przedłużenie ze starego krótkiego odcinka do pełnej zamkniętej pętli około 1,19 km (według wzoru geometrii), **bez fizycznego zawracania**, sześć przystanków, rzeczywisty model CAF, styki torów, rozdział jazdy/hamowania/postojów.
- Tramwaj: `Awake` nie sprawdza pustej trasy przed inicjalizacją przez generator, właściwe uruchomienie odbywa się w `Start`.
- Tramwaj: `BraxSeat` współpracuje z modelem wagonu, wsiadanie i wysiadanie w interakcji AKCJA; wysiadanie wyłącznie na postoju, gracz synchronizowany przed pracą kamery.
- Tramwaj: dodatkowy komponent pasażerów Rocketbox, *warunkowo* realizuje animowane wsiadanie/wysiadanie **tylko przy wykrytych osobnych, ruchomych skrzydłach CAF**. Brak węzłów drzwi w modelu to brak funkcji, NIE wolno twierdzić, że działa. Modele/animacje muszą pochodzić z istniejących źródeł, nie z atrap.
- Tramwaj: nowy audyt SAT porównuje obrys CAŁEGO wagonu na łukach z geometrią dostępnych budynków, wykryte kolizje są raportowane przed zatwierdzeniem APK.
- Przed kosztownym buildem CI sprawdza zaszyfrowane źródła CAF, wiat, leków, odżywek, murali i atlasu okien. Nowa gałąź uruchamia się WYŁĄCZNIE ręcznie.

## Weryfikacja obowiązkowa (bez twierdzenia, że naprawiono)

1. Rzeczywista kompilacja Unity 6, kontrola błędów C# i logów `BRAXHORN_TRAM_T1_*`, `BRAXHORN_MIASTO_B_*` oraz `BRAXHORN_SUPERMARKET_*`.
2. Oględziny nowego APK: przejazd pełnej pętli T1, brak wjazdów tramwaju w elewacje, przejezdne zakręty i skrzyżowania, bez odtwarzania asfaltu na asfalcie.
3. Oględziny i interakcje: otwieranie **rzeczywistych** drzwi CAF, siedzenie w wagonie, możliwość wyjścia tylko na peronie; NPC wsiadają/wychodzą jeśli importer wykrył skrzydła. W razie braku — wymagana osobna praca z modelem.
4. Nowa dzielnica: ciemne partery, rusztowania, luki, niepełne przeszklenia, błędne tekstury PBR, trawniki i kwadratowe chodniki; porównanie z filmami po pełnym imporcie.
5. Tower: kamera po przejeździe na dach, pełne 360°, wszystkie drzwi, brak czarnych ekranów; supermarket, NOVA i apartament — swobodne wejścia.
6. Ostatecznie Android: czas uruchomienia, zużycie RAM, FPS, brak crashów i dostępność aktualizacji strefowych.

**Brak jeszcze rzeczywistego testu APK dla tej gałęzi.** Nie scalać z gałęzią miasta i nie publikować jako ukończone bez odbioru. Osiedla i obozu nie dotykać.

## Dodatkowe naprawy po audycie (zapisane w kodzie, NIE potwierdzone w APK)

- **Tower / kamera windy:** przy rzeczywistym wejściu do kabiny automatyczny widok zza pleców zamienia się w widok w stronę drzwi na wysokości oczu. Po wyjściu przywracany jest normalny widok TPP. Minimalizuje to ryzyko czarnego ekranu z filmów v1 (0:12–0:44).
- **Nowa dzielnica / szyby:** przy materiałach rozpoznanych jako szkło lub ciemny metal fasady zmieniany jest istniejący materiał na jaśniejsze szkło refleksyjne PBR, bez dokładania drugiej powierzchni. Nie zastępuje to ręcznej kontroli wszystkich witryn.
- **Supermarket:** możliwe do usunięcia tylko fragmenty płotu bezpośrednio przed wejściem, reszta zachowana. Jeśli generator nie znajdzie płotu w tej samej scenie, log zgłosi brak naprawy.
- **T1 / kamera i interakcja:** poprawiony kontekst przycisku gracza, zachowanie siedzenia podczas jazdy, inicjalizacja po zbudowaniu trasy i aktualizacja postaci przed kamerą; sześć przystanków na pełnej pętli.
- **T1 / model drzwi:** `unity-tools/claude/check_tram_glb.py` sprawdza w binarnym `tramwaj_caf.glb`, czy rzeczywiste skrzydła istnieją jako minimum dwie odrębne siatki. Jeśli nie, build z tej testowej gałęzi zatrzyma się wcześnie (niedokończona mechanika, brak oszustwa).

### Granice ukończenia

Nie mamy jeszcze kompilacji Unity ani testu rzeczywistego ruchu, drzwi CAF, pasażerów Rocketbox, FPS lub załadunku miasta. Nie ma podstaw, by obiecywać, że cały tramwaj i wszystkie fasady są gotowe. Brak modeli drzwi w GLB albo kolizje na łukach = wymagane dalsze naprawy przed akceptacją.

## 4.11 — naprawy PREMIUM nowych budynków (bez ingerencji w osiedle / obóz)

Użytkownik dodatkowo wymaga, aby wszystkie potrzebujące poprawek budynki nowej dzielnicy otrzymały wykończenie premium, **nie tylko usunięte rusztowania**. W źródłach Unity 6 nowa dzielnica obejmuje **21 modeli / segmentów** (LONDON, KINNAIRD, ATLANTA, 14 segmentów City5 i 4 segmenty City3). Potwierdzić ich rzeczywistą obecność po odszyfrowaniu paczek.

Wykonano w `BraxRegionMiastoBDzielnice.cs`:
- Usunięto **podwójny przebieg** zdejmowania rusztowań. Pozostawiono jedno ostrożne czyszczenie oddzielnych węzłów `scaffold/rusztowanie`, bez usuwania całej elewacji przez nazwę. Jeżeli rusztowanie jest scalone z budynkiem, kod zgłasza błąd i wymaga korekty źródła modelu.
- Lista rendererów LOD jest odbudowywana **po** czyszczeniu GLB. Nie zawiera odwołań do zniszczonych elementów. Budynek bez siatki fasady jest zgłaszany jako `BRAK_FASAD` i nie otrzymuje fikcyjnej elewacji.
- Zastosowano `NdPremiumFasada`: zmienia tylko nieteksturowane, prawie czarne **materiały prawdziwych ścian/parterów** na istniejącej siatce, z 3 różnymi rodzinami PBR: klinkier kamienic, piaskowiec kwartałów City5, granit wieżowców. Podstawą są zeskanowane tekstury CC0 już przewidziane dla miasta (`brick_wall_001`, `rectangular_facade_tiles`, `granite_tile_02`, alternatywy). Jeśli skan nie istnieje po imporcie, log `PREMIUM_MATERIAL_BEZ_PBR` oraz `NIEGOTOWA`; nie udajemy premium samym kolorem.
- Oryginalne tekstury PBR, ozdobne drzwi, framugi, szkło, poręcze, dachy i główna architektura modeli pozostają. Poprawiono rozpoznawanie tekstur z różnych slotów shaderów (w tym `_BaseMap`), żeby nie usuwać gotowych materiałów glTFast.
- **Zero dodatkowych płaszczyzn elewacji, zero zbędnych lamp, zero równoległych rusztowań, bez wzrostu liczby polygonów.** Nie powstają atrapy szyb ani zaklejane na ślepo szczeliny.
- Log `BRAXHORN_NOWA_DZIELNICA_PREMIUM` wyświetla dla każdego budynku liczbę wymienionych, zachowanych i brakujących materiałów, dzięki czemu nie można raportować pozornych sukcesów.

**Nierozstrzygnięte:** dokładny przebieg szczelin między ścianami, nieopisane rusztowania scalone ze źródłem oraz wszystkie ciemne partery nieoznaczone nazwami ścian wymagają oględzin rzeczywistego importu/Androida. Nie nakładać nowej bryły na starą. Pełny odbiór premium: bez czarnych okien, bez pustych prześwitów, bez z-fightingu, przechodnie bramy, jednolita jakość materiałów i prawidłowe LOD przy przejściu z daleka.

**Dowód:** zmiany kodu na gałęzi `codex/miasto-naprawy-filmow-20261104`; nie jest to jeszcze render Unity 6 ani potwierdzone APK.
