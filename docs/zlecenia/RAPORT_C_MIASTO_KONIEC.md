# RAPORT KOŃCOWY — wykonawca C (MIASTO), dla następnego wykonawcy

Worktree: `scratchpad/wtM-assety`, gałąź `claude/miasto-assety`. Mój ostatni commit to `9427567`. Na nim prowadzący dodał `9323603` (bezpieczniki usuwania: zapory, klocki, stare reklamy), `8ece0e0` (ekran startowy) i `2d25589` (paczka grafik). HEAD `2d25589`, drzewo czyste, NIC nie wypchnięte.
Ostatnie sprawdzenie przed commitem: `bash scratchpad/kompiluj_assety.sh` nic nie wypisał (OK), `bash scratchpad/brama_modulow.sh wtM-assety/unity-city/Assets` nic nie wypisał (OK).
Nic nie było uruchomione w Unity. Wszystko poniżej jest do sprawdzenia w CI: logi i ujęcia z `unity-tools/claude/region_shots.mjs`.
Shaderów nie da się skompilować lokalnie, sprawdza się tylko C#.

## 1. Commity tej sesji (od najstarszego)

| commit | jedno zdanie |
|---|---|
| 8e73708 | M1: materiały modeli mają unikalne nazwy pliku z globalnym licznikiem, więc nie nadpisują się nawzajem (różowe jachty). |
| 8d0c363 | C1: `BraxZapisAssetu` nie nadpisuje pliku zapisanego w tym samym buildzie (kolizja trafia do `_kolizja_N`); kontrola `BRAXHORN_MATERIALY_BRAK`. |
| 57fc735 | Kamera pieszo zawsze za plecami (wzór z osiedla) + ustawienie w MENU „KAMERA ZA PLECAMI WŁ./WYŁ.”. |
| 6bb6a09 | C3: w strefie siłowni migotanie usuwane wycinaniem dolnej warstwy pod górną, bez przesuwania; pary LOD pominięte. |
| 7a4ddb6 | C2: wyspa ok. 1 km od brzegu (ląd ok. 300 × 300 m), plaża premium na południu, ośrodek domków na płn./wsch., tor wodny z mariny, mapa. |
| e804706 | M10: skały wyspy osadzone na najniższym punkcie pod sobą, w głębi lądu tylko głazy. |
| be7be52 | C4/M7: szkło globalnie — przezroczyste z odbiciem nieba, fasady bez wnętrza mają szkło refleksyjne (`BraxSzyby`). |
| dc9a311 | M8: z nieba zniknął „księżyc” (wymazane słońce ze zdjęcia nieba), czysta tarcza słońca zgodna ze światłem sceny. |
| a49e6bd | Pkt 15 a/b/c: woda za kamerą bez krawędzi, miękka granica akwenu z prądem, siatka bezpieczeństwa gracza. |
| 57c1767 | P0.1 metro: test przejścia obu wejść, obce kolizje w przejściach wyłączone, okładzina szybu wielkoformatowa. |
| a218e59 | Pkt 7/7b/15d: pierścień horyzontu z brył 3D (archipelag, nasza wyspa, wzgórza, góry), bez naklejki i smug na niebie. |
| 366a593 | Pkt 17/15b′/20: sztorm na granicy otwartego morza, pętla wybrzeża w mgle, krawędzie świata z logiem i ujęciami. |
| 2b21ecb | Metro 1/1b/1c: jedno wejście (płn.), model metro_train na peronie (2 wagony), szafka hydrantowa zamiast czerwonej kostki. |
| 21429de | Metro 58: schody ruchome chodnik→antresola (w dół) i antresola→peron (w górę); postać jedzie ze stopniami, poręcz przewijana UV, dźwięk napędu. |
| db0def6 | Metro 1 (01:39): po zamkniętym płd. zejściu pod chodnikiem pełny grunt, ściana hali ciągła, bez pomieszczenia i bez drzwi. |
| e034a8c | Pkt 19/52, 41, 37/33, 42, 26/38: w mieście bez golasów, kierowcy w jadących autach, audyty skali i klocków, bez szlabanu, tunel 62 m bez czarnej ściany. |
| 72a3857 | Pkt 4: mozaika wycinana w 3 strefach, napisy z testem głębi i bez nakładania, przeszkody przed drzwiami usuwane, meble w powietrzu usuwane, lady z kolizją. |
| 95ace4b | Pkt 2: zielone misy drzew (granitowe obrzeże, okrywa, byliny) zamiast krat, jeden hydrant zamiast dwóch. |
| 6b253fe | Pkt 59: BRAXHORN FITNESS Nyxy w lokalu „gallery” (recepcja, cardio, sala zajęć, szatnia, quad „mural_fitness” 5.76 × 3.24 m). |
| 9427567 | Pkt 48/48b: stare reklamy usunięte (także NOVA i ekrany LED na wieżach), nowe billboardy i citylighty na nośnikach, mural 19 przy siłowni. |

## 2. Logi do sprawdzenia w CI (oczekiwane wartości)

- `BRAXHORN_MATERIALY_BRAK … liczba=0` (etapy `przed_zapisem_sceny`, `pliki_scen_miasta`); `BRAXHORN_ZAPIS_KOLIZJA` tylko informacyjnie.
- `BRAXHORN_SILOWNIA_MIGOTANIE pary_po_powyżej_0.1m²=0 stref_ciecia=4`; `BRAXHORN_MIGOTANIE` (nierozwiązane).
- `BRAXHORN_SZYBY`, `BRAXHORN_NIEBO`, `BRAXHORN_PANORAMA`, `BRAXHORN_AKWEN`, `BRAXHORN_AKWEN_PETLA`, `BRAXHORN_KRAWEDZIE`, `BRAXHORN_TOR_WODNY`, `BRAXHORN_WYSPA`, `BRAXHORN_SIATKA_BEZP` (runtime).
- `BRAXHORN_METRO_PRZEJSCIE wejscie=płn ok … schody_ruchome=ok … chodnik_obok płn=…/płd=… ok` (cel: ≥ 3.24 m z jednej strony).
- `BRAXHORN_METRO_POCIAG model=metro_train wagony=2 …` (sprawdzić `czolo=z modelu`, `spod_pudla_nad_szyna_m` względem `peron_nad_szyna_m`, brak „poza skrajnią”).
- `BRAXHORN_SCHODY_RUCHOME` ×2 (`poręcz_uv=tak`).
- `BRAXHORN_LUDZIE_STROJE poza_plaza=0` (lista postaci z największym udziałem skóry, próg 0.45), `BRAXHORN_LUDZIE_SKALA`, `BRAXHORN_AUTA_KIEROWCY jadace_bez=0`.
- `BRAXHORN_SKALA niezgodne=… [lista]` — tylko audyt, bez napraw.
- `BRAXHORN_KLOCKI lista=… usuniete_pojazdy=… do_wymiany=[…]` — reszta poza pojazdami tylko na liście.
- `BRAXHORN_DROGI_ZAPORY usuniete=…`, `BRAXHORN_TUNEL_OSIEDLE` (runtime: zawrót).
- `BRAXHORN_NAPISY_KOLIZJE`, `BRAXHORN_DRZEWA_DRZWI`, `BRAXHORN_MEBLE_WISZACE`, `BRAXHORN_LADY_KOLIZJE`, `BRAXHORN_HYDRANTY`.
- `BRAXHORN_FITNESS … bieżnie_model=…/3 mural=…`.
- `BRAXHORN_BILLBOARDY postawione=6 brak_grafiki=… usuniete_stare=… [miejsca z pozycjami]`. Pozycje 09/12/06/10 są wyszukiwane fizyką; jeśli któreś się nie mieści, log mówi „brak wolnego miejsca”.

Nowe ujęcia: `METRO_*`, `DETAL_metro_*`, `DROGI_*`, `DETAL_tunel_*`, `DETAL_elewacja_*`, `DETAL_tower_recepcja_lada`, `DETAL_misa_drzewa_zielona`, `DETAL_fitness_*`, `DETAL_billboard_*`, a z wcześniejszych punktów `DETAL_panorama_*`, `KONIEC_*`, `DETAL_szyby_*`, `DETAL_niebo_*`, `DETAL_wyspa_*`, `DETAL_silownia_*`.

## 3. Czego NIE zrobiłem (pliki i funkcje)

### Pkt 5 dopisku — w całości nie zaczęty
- **Wyspa 16/16b/23**: `Editor/Claude/Region/BraxMiastoWyspa.cs` (`Apply`, `Plaze`, `Teren`, `Arena`) i `BraxMiastoWyspaDomki.cs` (`PlazaPremium`, `Osrodek`, `MorzeOtwarte`).
- **Port i kontenery 8/8b/8c/13/21**: `Editor/Claude/Region/BraxMiastoPort.cs` (brama, ogrodzenie, ciężarówki, kontenery). Pojazdy z klocków powinien usunąć nowy audyt `BraxKontrolaKlockow` — sprawdzić to w logu KLOCKI.
- **Dźwięki 14/28/M20** (pojazdy start/stop, otoczenie): `Claude/Runtime/BraxDzwiekiSwiata.cs`, `Claude/BraxVessel.cs`, `BraxAuto` (ruch miasta B).
- **Jedno wejście 29**: wszystkie budynki z dwoma wejściami. Metro ma już jedno.
- **Sklepy 38** (wyposażenie z Poly Haven, pracownik w każdym sklepie, `BRAXHORN_SKLEPY bez_pracownika=0 klocki=0`): `Editor/Claude/Region/BraxUlicePierzeje.cs` → `Wnetrze(...)` (regały z kolorowych pudełek), lista modeli w punkcie 38 dopisku.
- **Kawiarnia 32–34** (charakter kawiarni, drzwi WC 1.16 × 2.72 m z łazienką): kawiarnia PARK TOWER (`BraxRegionTylyB.cs`) i lokal „KAWIARNIA” (`BraxUlicePierzeje.Lokale`, id `estate_corner`).
- **Szpital i komisariat 50/51**: `BraxRegionTylyB.cs` / `BraxRegionMiastoB.Komisariat`, `.Szpital`.
- **NOVA i mieszkanie Braxa 43/46/47**: `Editor/Claude/Region/BraxMiastoNova.cs` — brama klatki „KLATKA”, bryła klubu, podwójny napis NOVA DYSKOTEKA + CLUB NOVA. Plakaty z NOVA są już usunięte, a `Plakat()` został w pliku jako nieużywany.
- **Wejście na siłownię 45** (szyld 3D, portal, schody/rampa, porządek z koszami): strefa siłowni chroniona (`BraxKontrola.StrefaChroniona`). Mural 19 stawia `BraxMiastoBillboardy.MuralSilowni` (szuka ślepego lica przy x 9.8…14.4 lub −0.8…2.3, z ≈ 20.6).
- **Płynność 44**: budżet rysowania i LOD — nic nie robione.

### Pozostałe
- **Schody ewakuacyjne 30**: płaskie schody z niebieskimi i żółtymi prostokątami przy NUTRITION i GYM to NIE jest generator z kodu. `BraxUlicePierzeje.Detale()` robi schody tylko na bokach zachodnich (`f.N.x < -.5f`). Te złe są prawdopodobnie w oryginalnej scenie (CityMesh_*) albo w teksturze elewacji. Trzeba je znaleźć w scenie (okolica x 12…50 i −50…−16, z 4…46), usunąć i postawić nowe generatorem `Detale()`, poszerzając warunek o boki wschodnie i lica przy siłowni.
- **Układ mebli 35** (`BRAXHORN_MEBLE_UKLAD kolizje=0 krzesla_bez_stolu=0`): brak. Jest tylko `BraxKontrolaMiastaDetale.Wiszace` (usuwa meble w powietrzu).
- **Wieżowce na dachach 39b**: `Editor/CityRebuild/BraxCityStreetSkyline.cs` (`HighRises`, `Footprint`) — odsunąć albo dać własny parter tam, gdzie wieża siedzi na dachu niższego budynku.
- **Rynny 49** (kolano w powietrzu → rynna do chodnika z wylewką): `BraxUlicePierzeje.Detale()` i elewacje boczne. Plamistą elewację w alejce przy BANKU obejmuje wycinanie (strefa w `BraxKontrolaMigotania.Strefy()`); trzeba to sprawdzić na `DETAL_elewacja_bank_alejka`.
- **Naprawy według list SKALA i KLOCKI**: `Editor/Claude/Region/BraxRegionRuchMiasta.cs` → `AudytSkali()` (drzwi i siedziska tylko w logu) oraz `Editor/Claude/BraxKontrolaKlockow.cs` (usuwa tylko pojazdy i maszyny; reszta `do_wymiany=[…]` czeka na wymianę na modele albo usunięcie). Audyt skali nie obejmuje lad, stołów, schodów, poręczy ani witryn — trzeba go rozszerzyć.
- **Elewacje 27** (globalny audyt zmiany materiału lub rytmu w pionie): nie zrobione. Zrobione jest tylko wycinanie mozaiki w 3 strefach.
- **Drzewa 2, reszta**: stojak rowerowy wchodzący w ławkę przy przystanku i brązowy „blat” przy piekarni nie są poprawione celowo. Stojaki to wspólna siatka w `BraxMiastoUlica.Stojak`; „blat” może zniknąć przez `MebleWiszace`, jeśli w nazwie jest stół/blat. Przeszkody przed drzwiami są USUWANE, nie przenoszone wzdłuż krawężnika.
- **Obwodnica, tory i rzeka na granicy lądu**: nie zaczęte (krawędzie lądu: `BraxRegionHoryzont.cs`, `BraxPanorama.cs`, `BraxMiastoGranice`).
- **Pociąg z otwartymi drzwiami i oświetlonym wnętrzem (1c)**: model metro_train nie ma wnętrza, a drzwi ma zamknięte. Teraz to 2 wagony z kolizją pudła (`BraxClaudeMetro.PociagModel`). Stary wagon z wnętrzem jest dalej w kodzie (`PociagZKodu` / `Wagon`) i działa jako zapas. Do zrobienia: model z wnętrzem albo wnętrze z kodu w obrysie modelu, z wycięciem drzwi.
- **Kolejne stacje metra 60** i jazda pociągiem: nie zaczęte. Kod stacji M1 nie jest sparametryzowany (stałe na początku `BraxClaudeMetro.cs`).
- **Tunel na osiedle 42**: czeka na decyzję A/B. Teraz jest tunel 62 m z betonu i `Claude/Runtime/BraxTunelZawrot.cs` (komunikat i powrót, gdy brak sceny `BraxhornEstate`).
- **Golasy 19 — możliwa przyczyna niepotwierdzona**: filtr działa po odcieniach ubrań i po masce skóry z CI (`BraxClaudeOsiedleLudzie.NowaOsoba`, `UdzialSkory`, `PodobnyDoSkory`). Jeśli maska z `fetch_rocketbox.py` jest zła, próg 0.45 trzeba dobrać według listy w logu.
- **Fitness 59, braki**: hol ma 7.05 m głębokości, więc w szatni nie ma osobnego WC, tylko prysznic. Nyxa i klientki są tylko miejscami obsady (`W.Obsada`) — nie wiem, czy etap ludzi w mieście stawia postacie z `BraxWnetrzaKit.Obsady`. Grafika 02.jpg podepnie się, jeśli plik będzie w `Assets/Source/Prywatne/Grafiki/billboardy/`.

## 4. Ryzyka

1. **Billboardy**: `UsunStare` usuwa w całym mieście obiekty z nazwą billboard/plakat/baner/banner/poster/reklam/artwork. Prowadzący ograniczył to w `9323603` do obiektów wielkości nośnika, ale listę w logu i tak trzeba sprawdzić. Miejsca 09/12/06/10 są szukane fizyką, więc pozycja może wypaść nieładnie. Ujęcia `DETAL_billboard_*` mają stałe pozycje tylko dla NOVA, muralu i ronda.
2. **Kontrole detali** (`BraxKontrolaMiastaDetale`) działają po słowach w nazwach i usuwają obiekty. Fałszywe trafienia są możliwe, na przykład ławka we wnętrzu blisko drzwi albo stół na tarasie bez kolizji pod spodem. Kolejność kroków jest celowa: kontrole idą przed autami i ludźmi.
3. **Wycinanie mozaiki** w 3 nowych strefach kopiuje siatki tylko dla tych rendererów (rozmiar paczki). Strefy są z prostokątów zgadniętych z kodu, bez podglądu.
4. **Metro**: skala modelu pociągu jest liczona z obrysu materiałów „side”. Kierunek czoła wynika z materiałów „mirror.001”/„aiStandardSurface” — jeśli ich nazwy w imporcie są inne, wagony mogą stać czołami do siebie. Schody ruchome wejścia zwężają schody stałe do 1.48 m (1.14 m realnie).
5. **BraxSchodyRuchomeJazda / BraxTunelZawrot / BraxRegionSignage** to nowy kod runtime bez testu w grze: `CharacterController.Move` na schodach, `OnGUI` i `sceneLoaded`.
6. **Ludzie**: zmiany w `BraxClaudeOsiedleLudzie.cs` (wspólny plik z osiedlem) są włączane tylko w scenach `BraxhornCity*`. Zmiana nazw scen wyłączy filtr.
7. **Tunel 62 m**: sufit tunelu jest pod zboczem (wysokość zbocza ok. 13.5 m nad portalem), ale nie zostało to sprawdzone wizualnie z góry.
8. **Fitness**: lustra i mural stoją 3 cm od lica ściany liczonego z `BraxUlicePierzeje.Wnetrze` (sIn0 = .34, głębokość 7.05). Jeśli prawdziwe lico jest gdzie indziej, wejdą w ścianę albo zawisną.
9. **Shadery** (`MiastoPierscien.shader`, zmiany w `MiastoTlo.cginc`/`MiastoNiebo.shader`) nie były kompilowane lokalnie.

## 5. RUNDA 2 (3.11, 02:18–03:30) — dopisek

HEAD `09877e2` (koniec rundy), drzewo czyste, NIC nie wypchnięte. Kompilacja (`kompiluj_assety.sh`) i bramka (`brama_modulow.sh`) przed każdym commitem nic nie wypisały. Na tej gałęzi są też commity prowadzącego (`86ada29`, `bb98c36` — paczki zaszyfrowane).

| commit | jedno zdanie |
|---|---|
| 4b2785f | Wyspa: miejsca ludzi (kibice, plaża, obsada) i znaczniki obsady metra i fitnessu są wreszcie zamieniane na postacie; jest też audyt `BRAXHORN_WYSPA_KOMPLET` z listą BRAKI. |
| 48e6613 | Pkt 46: mieszkanie Braxa nad NOVA — brama klatki 1.16 × 2.72 m z domofonem, numerem, daszkiem i kinkietem, klatka z poręczami i skrzynkami, urządzone mieszkanie ok. 90 m² świata (przedpokój, łazienka, salon z aneksem, sypialnia, „ODPOCZNIJ” na łóżku). |
| 264cd11 | Pkt 46: okna mieszkania przezroczyste na całym piętrze (prostokąt szkła nadbudowy NOVA poszerzony do x 51.4). |
| 0689659 | Siłownia Braxa: sprzęt dokładany w wolnych miejscach hali (z paczki klatki, atlas, bieżnie, ławka; z kodu hantle, talerze, gryfy, ławka płaska, podłoga gumowa). |
| b42b327 | Wyspa (a): w głębi lądu drzewa liściaste z miasta (`drzewo_verdant`) zamiast palm, palmy przy plaży z pełniejszą zielenią liści. |
| 0715bdb | Wyspa (b): toalety w arenie — damska i męska jako pełne pomieszczenia: dwa pawilony w holu po bokach przejścia od wejścia do klatki, z kabinami, WC, pisuarami, umywalkami, lustrem, suszarką, płytkami i napisami (`BraxMiastoWyspa.Arena`, blok „TOALETY AREN”). |
| 09877e2 | Wyspa (c): sklep ośrodka z obsługą i wyposażeniem — stoisko na tarasie (lada, kasa, lodówka z napojami, skrzynki z owocami, sprzedawca i klient; `BraxMiastoWyspaDomki.Domek`, gałąź `tabliczka == "SKLEP"`). |

Nowe logi: `BRAXHORN_LUDZIE_WYSPA`, `BRAXHORN_WYSPA_KOMPLET`, `BRAXHORN_MIESZKANIE_BRAXA`, `BRAXHORN_SILOWNIA_WYPOSAZENIE` oraz „WYSPA zieleń: palm …, drzew liściastych …”.
Nowe ujęcia: `WYSPA_*`, `DETAL_wyspa_kibice_rzedy`, `DETAL_wyspa_palma_lisc`, `BRAX_mieszkanie_*`, `DETAL_mieszkanie_*`, `DETAL_silownia_braxa_*`.

### Czego NIE zrobiłem w tej rundzie (pliki i funkcje)
- **Wyspa (b) — toalety areny**: zrobione w `0715bdb` jako dwa pawilony w holu. Wejście jest z holu (przejścia do klatki), nie z korytarza trybun, bo trybuny nie mają korytarza na parterze. Podłoga w toaletach to żywica hali, nie płytki: płytki na posadzce byłyby nakładką na żywicę.
- **Wyspa (c) — plaża, ośrodek, przystań, arena według listy z pkt 57**: z kodu plaża ma już deptak, latarnie, ławki, kosze, WC, wypożyczalnię, bar, przebieralnie, ratowników z flagami i boje. Ośrodek ma domki, chodniki z krawężnikami, latarnie, ławki, recepcję i plac zabaw, a w `09877e2` doszło stoisko sklepu. Reszta czeka na listę BRAKI z logu `BRAXHORN_WYSPA_KOMPLET` po buildzie CI (`BraxRegionRuchMiasta.KompletWyspy`). Brakuje mat z logo w oktagonie, które wymaga podziału siatki maty, nie nakładki. Ring z linami w arenie (jest tylko oktagon, ringi plenerowe są w `Ringi()`) i trybuny instancjonowane to prace do zrobienia według pkt 57.
- **Wyspa — drzewa Poly Haven** (pine/fir/island_tree): świadomie nie dodane, bo to skany po 2–17 mln trójkątów. Prowadzący poprosi właściciela o lekki model palmy. Liście palm z kodu są dalej kolorem wierzchołków, bez tekstury cutout.
- **Kawiarnia 32–35** (charakter kawiarni, WC usunąć, meble przy stołach): nie zaczęte.
- **Sklepy 38** (Poly Haven, pracownik w każdym sklepie): nie zaczęte (`BraxUlicePierzeje.Wnetrze`).
- **Schody ewakuacyjne 30**: nie zaczęte (opis w sekcji 3 wyżej).
- **Fitness Nyxy — więcej cardio** (rowerki, orbitreki): nie zrobione. Hol lokalu ma tylko 7.05 m głębokości, a wolnego lokalu obok nie sprawdzałem. Sąsiedzi „gallery” (x −10…2) to POCZTA (`office_entrance`, x −22…−10) i brama podwórka (x 1.5…18.5).
- **Siłownia Braxa**: sprzęt jest tylko DOKŁADANY. Nie wymieniałem klocków ze starego modelu siłowni (`braxhorn-original-gym.glb`, `BraxGymReferenceRebuild`). Hantle i talerze są z cylindrów Unity (`BraxRegionGym.Walec`), a ring jest bez zmian.

### Ryzyka tej rundy
- **Mieszkanie**: zajmuje teraz piętro nad toaletami klubu (x 45.2…51.4). Wycięcie „pustej skorupy” w `BraxRegionBraxFlat.Apply` sięga tam dalej, co trzeba sprawdzić na ujęciach (szczególnie `BRAX_mieszkanie_salon` i `DETAL_mieszkanie_sypialnia`). Sofa, fotel, stół i inne modele Poly Haven mają kąt obrotu zgadnięty (yaw) — mogą stać tyłem. Brama ma otwór okładziny NOVA powiększony w `BraxMiastoNova` (x 33.0…34.36, do y 3.01).
- **Ludzie wyspy i obsada**: do 150 kibiców plus obsada to dużo postaci skinned na telefonie. Animacja ma budżet, ale rysowanie nie.
- **Siłownia**: miejsca są szukane fizyką (`BraxRegion.Free`), więc sprzęt może stanąć w nietypowym miejscu hali; pozycje są w logu.
- **Wyspa**: drzewo `drzewo_verdant` w głębi lądu (gęstość .12–.3 na siatce 9 m) to dodatkowe renderery — sprawdzić FPS.

## 6. RUNDA 3 (3.11, ok. 02:50) — płynność miasta bez utraty jakości grafiki

Gałąź worktree `wtM-assety` to `claude/miasto-assety`, a nie `claude/miasto-opus-20261031` z polecenia. Nie przełączałem gałęzi: zlecenie od początku dotyczyło tego worktree i tej gałęzi. HEAD `7a66fb0`, drzewo czyste, NIC nie wypchnięte. Kompilacja i bramka przed każdym commitem nic nie wypisały.

| commit | jedno zdanie |
|---|---|
| c0c2d55 | Pakiet wydajności E19 z osiedla (`e60e8e6`) przeniesiony do wspólnych klas runtime, których używa miasto. Obejmuje kibiców i plażę wyspy (BraxMieszkaniec), a `BRAXHORN_WYDAJNOSC` jest teraz też w logu miasta. Szczegóły niżej. |
| 7a66fb0 | Pierścień horyzontu: wszystkie renderery bez rzucania i odbierania cieni oraz bez sond światła i odbić, kolizje usuwane; liczniki w `BRAXHORN_PANORAMA`. |

Szczegóły `c0c2d55`:
- Zmienione pliki: `BraxLudzie` (`Detale`, adaptacja limitu w `Animuj`), `BraxPieszy`, `BraxMieszkaniec`, `BraxAuto`.
- Cień postaci tylko do 20 m × K, a skóra na 2 kościach dalej niż 32 m × K.
- Cień aut tylko do 20 m × K, i to tylko dla części, które w buildzie rzucały cień.
- Silniki grają tylko u 4 najbliższych aut.
- Limit animowanych ludzi spada z 40 do 16 przy średnio < 35 kl./s, a wraca powyżej 42.
- Przed zmianą `BraxLudzie.cs` w mieście był identyczny z osiedlem sprzed E19; pozostałe trzy pliki różniły się, więc zmiany naniosłem ręcznie w tych samych miejscach.

Co ustaliłem:
- Animacje ludzi miasta, wyspy i obsady mają `cullingType = BasedOnRenderers` (ustawiane w `NowaOsoba`), więc nie liczą się poza ekranem. Dodatkowo działa budżet odległości `BraxLudzie.Animuj`. Liczba animacji bez cullingu jest w logu `BRAXHORN_WYDAJNOSC` (`animacje_bez_cullingu=`).
- Ustawień grafiki NIE ruszałem: BraxGrafika, shadowDistance, lodBias, MSAA, tekstury i modele są bez zmian.

Czego nie zrobiłem lub nie dało się sprawdzić bez Unity:
- „Nic nie rzuca cienia z odległości większej niż shadowDistance”: Unity sam pomija rzucających cień poza shadowDistance. Zostaje tylko przypadek dużych scalonych siatek (np. kwadraty K), których obrys sięga w zasięg cienia — wtedy w cieniu rysuje się cała siatka. Poprawka wymaga dzielenia siatek, czyli zmiany modeli, co jest zakazane — tylko odnotowuję.
- Na urządzeniu nie mierzyłem FPS ani nie sprawdzałem histerezy cienia (miganie przy 20 m), dźwięku 4 silników ani adaptacji limitu.
- Kibiców na wyspie może być do 150. Bez LOD postaci (brak w paczce) rysowanie skinned zostaje, a ograniczona jest tylko animacja i cień.
