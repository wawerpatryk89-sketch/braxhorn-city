# BRAXHORN CITY — R2 OSIEDLE DOMKÓW: REMONT PREMIUM / 30.10.2026

## Zakres i granice
Źródło: BRAXHORN_CITY_MASTER_V3_KOMPLETNY, rozdział 6 (region drugi), rozdział 10 (materiały i realizm), rozdział 27 (równoległa praca), rozdział 28 (odbiór). Dodatkowe polecenie operatora 30.10: najpierw remont **istniejącego** R2; rozbudowa później; brak obowiązku screenów co 15%.

**Tylko R2.** Oryginalny Brax, oryginalne animacje, identyczny `BraxGymPlayer` i sterowanie z miasta. Nie zmieniać R1, siłowni, produkcji, mechanik walki ani mastera. Nie scalać bez akceptacji operatora.

### Stan zastany, który chronimy
- 8 domów (6 źródłowych, willa Wiśniowa 4 i dom kontaktowy), oryginalny układ Wiśniowa/Bydgoska/Poznańska.
- Willa z salonem, kuchnią, sypialnią, łazienką, tarasem; dom kontaktowy.
- Kasyno, klub, komisariat, szpital, park, boisko, garaże, sala bokserska, oktagon.
- Istniejące drzwi, wejścia, kolizje, punkty przejścia i strefy misji (bez aktywacji misji).
- Scena Unity: `BraxCityBuild.CreateEstate()`, 553 źródłowe siatki, kontroler `BraxGymPlayer`. Film kontrolny przed remontem: `preview/estate-real-unity-video-20261030`, 128.05 s, 14 miejsc.

## Wykonane w osobnej gałęzi — nie mylić z opublikowaną wersją
Gałąź robocza: `codex/estate-premium-renovation-20261030`.

1. **Usunięcie plastikowych drzew w źródle osiedla Three.js.** W `dev/osiedle/world.js` generator walców i ikosaedrów został zastąpiony żądaniami prawdziwych instancji `park_tree.glb` z Verdant Trail. Żądania drzew z ulic, ogrodów i terenów publicznych trafiają do `addVegetation`, zamiast tworzyć fikcyjne bryły.
2. **Donice bez sztucznych kul-liści.** W willi i na tarasie źródłowe krzewy `park_shrub.glb` zastępują geometryczne imitacje liści; pozycja Y jest respektowana.
3. **28 dodatkowych nasadzeń krzewów** w parku i przy obrzeżach osiedla. Są poza główną osią alejek, boiskiem i wejściami; źródłowe modele instancjonowane, a kolizje drzew dotyczą pni.
4. **Wykorzystanie prawdziwych materiałów z dostarczonych modeli domów.** Loader `dev/osiedle/level.js` wyszukuje istniejące mapy z `stucco_indoor`, `stucco_yellow_outdoor`, `Parquetlight_1K`, `Concrete_obruby` i innych udokumentowanych materiałów; stosuje tylko rzeczywiście znalezione mapy na plaster/wood/stone. Brak mapy oznaczany jako BLOCKED, bez wymyślania zastępczego PBR.
5. **Korekta oświetlenia w źródłowej scenie Unity R2** — mniej wypłukane światło dzienne, lepsza czytelność cieni. Brax, drzwi i geometria bez zmian.

**Ważne:** punkty 1–4 dotyczą źródłowego generatora R2 i nie zostały jeszcze przeniesione do bieżącego binarnego WebGL Unity. Punkt 5 jest zmianą w edytorowym generatorze Unity i wymaga ponownej kompilacji oraz wizualnego odbioru. Nie ogłaszać remontu ukończonym.

## Materiały — rzeczywisty stan i blokery

| Materiał / detal | Stan | Decyzja |
|---|---|---|
| Drzewa, krzewy, skały Verdant Trail | Udokumentowane, modele źródłowe częściowo zachowane; brak `park_tree.glb` w bieżącej gałęzi GitHub | Odtworzyć wyłącznie z zatwierdzonego źródła i SHA, bez sztucznych zamienników |
| Tynk wewnętrzny i elewacyjny | Materiały `stucco_indoor`, `stucco_yellow_outdoor` w istniejących domach; operator dostarczył `white_plaster_02_4k.blend.zip` do biblioteki | Najpierw odzyskać i porównać rzeczywiste mapy; osobny import 4K source -> 1–2K runtime |
| Cegła | `PlainRedBricks_1K`, `M_Brick_Clay_Beveled` w źródłowych domach; dodatkowe Bricks106B/097/104 w bibliotece | Wykorzystać istniejące, nie pobierać ponownie |
| Kostka i nawierzchnie | PavingStones151 2K i materiały drogowe w bibliotece ChatGPT; niezweryfikowany import do Unity | Nie udawać PBR szarym kolorem; wymaga kontrolowanego importu |
| Nawierzchnia drogowa / asfalt | W bibliotece `/Braxhorn city/Asset Library/Textures/Roads/` są `FourLaneRoadClean01_MR_4K.zip` i `TwoLaneRoadPatches02_MR_4K(1).zip`; oba archiwa zweryfikowano technicznie (5 map PBR każde). **Nie są zaimportowane do Unity R2; licencja i poprawność UV pozostają do sprawdzenia.** | **BLOKADA wdrożenia**: nie nakładać fikcyjnego asfaltu ani zastępczego koloru. Zweryfikować prawa i dopasować rzeczywiste mapy do istniejącej geometrii drogi |
| Okna i stolarka | W `/Braxhorn city/Assety premium/` potwierdzone: `residential_window.glb`, `realistic_old_wooden_window.glb`, `door-pack-free.glb`, `interior_wood_doors_pack_1.glb`. `residential_window.glb` jest poprawnym GLB v2, 3 siatki, 9 węzłów, 4 obrazy; metadane modelu wskazują Sketchfab `AleixoAlonso`, licencja **CC BY 4.0** (wymaga przypisania autorstwa). **Modele nie są zaimportowane do Unity R2.** | **BLOKADA wdrożenia**: sprawdzić licencje każdego modelu, skalę, klamki, kolizje, osadzenie w oryginalnych otworach i działające drzwi; zakaz sztucznych paneli |
| Dachówki / połacie | W `/Braxhorn city/Assety premium/` dostępne są `roof_tiles_4k.blend.zip` i `grey_roof_01_4k.blend.zip`. Gałąź R2 zawiera próbę mapowania dwóch zweryfikowanych tekstur Poly Haven do oryginalnych meshów dachów przez `BraxEstateR2ApprovedRoofs.Apply()`; **weryfikacja kompilacji i wizualnego efektu najnowszego SHA nadal obowiązkowa**. | Bez deklarowania gotowego dachu przed PASS aktualnego Unity build, potwierdzeniem liczby slotów i zrzutami renderu |
| Glazura, płytki | `MarbleFloorTiles_1K`, `MI_Gypsum_Tiles_uldnbcdn_2K`, `MI_Marble_Floor_th5jfjufw_4K` w istniejących domach | Zweryfikować wygląd, UV i prawa do użycia przed zmianą |
| Donice, ławki | Zasoby w bibliotece; część geometrii w scenie nadal uproszczona | Bez dodatkowych sztucznych zamienników |
| Komisariat — kartony/graty | Wymaga identyfikacji rzeczywistych meshów i kontroli wnętrza | **BLOKADA bezpieczeństwa:** nie usuwać przypadkowych meshów ani biurek. Najpierw selekcja, screenshot przed/po |
| Park Pine Forest 2,7 GB | Operator rozważa pobranie, nie ma potwierdzonego importu | Nie używać w tym etapie; ewentualnie do przyszłej rozbudowy |

### Audyt dostępnych plików 30.10.2026 — biblioteka ≠ import do Unity
- `FourLaneRoadClean01_MR_4K.zip`: SHA-256 `42246da7da60d8837682ce5bbff4489f4aaa32e0ef434e41d1c2cc8cd264293c`, ZIP bez błędów, 5 map (BaseColor, Normal, Roughness, AO, Height).
- `TwoLaneRoadPatches02_MR_4K(1).zip`: SHA-256 `ab64260ecdb137caa33367dddae078bbd37e28a261b787b223aa84a713fa5d5f`, ZIP bez błędów, 5 analogicznych map.
- `residential_window.glb`: SHA-256 `001c49c716bccd2d98b13f9139e77bda63de758754c08cb0a8bab2ac4a146578`; GLB v2/3 siatki; deklarowana w metadanych licencja CC BY 4.0. Nie potwierdzono jeszcze osadzenia/wyglądu w Unity.
- Istnienie plików nie oznacza zgody na ich automatyczne użycie; dopiero po weryfikacji pochodzenia, importu do `BraxhornEstate`, kolizji, drzwi i zrzutów renderu.

## Kolejność kolejnych prac w obecnym R2
1. Zgrać brakujące oryginały i licencje z biblioteki (bez zgadywania).
2. Zbudować podgląd tylko R2 w Unity 6 z tym samym Braxem, sprawdzić wejścia, kolizje, światło.
3. Zmienić materiał trawy i gruntu dopiero po prawidłowym imporcie PBR i porównaniu na iPhonie; trawa 3D z LOD.
4. Po weryfikacji licencji i imporcie dostępnych oryginalnych map drogowych wymienić nawierzchnie, zachowując szerokości dróg, chodników i bram.
5. Po sprawdzeniu licencji, geometrii i importu dostępnych modeli okien/drzwi dopracować fasady, parapety, wnęki, przeszklenia i dachy bez zasłaniania wejść.
6. Wnętrza: ściany/tynki, płytki, posadzki, łazienki, czytelne oświetlenie; nie udawać gotowych pięter.
7. Komisariat: usunąć wyłącznie zidentyfikowane zbędne kartony, zachować biurka, stanowiska obsługi i dostępność wejść.
8. Willa i dom kontaktowy: tarasy, roślinność, ogrodzenia, naturalne wykończenia i drogi dojścia.
9. Park i boisko: źródłowa zieleń, bez zasłaniania stref walki, dojść i wyjść.
10. QA: film **prawdziwym ruchem bez teleportów**, przegląd wszystkich wejść, test WebGL i urządzenia fizycznego. Żadnego automatycznego merge do produkcji.

## Plan przyszłej rozbudowy — bez wykonywania teraz
- **Mieszkalna część premium:** powiększenie istniejącego układu ulic, nowe różne typy willi (bryły, dachy, okna, wejścia, ogrody), nie kopie przemalowanych domów.
- **Duży park:** osobna zielona oś, spacerowe alejki, miejsca spotkań i istniejące boisko; przestrzeń dla misji 3 na 3.
- **Usługi:** wydzielić docelowy sklep odzieżowy i punkt z odżywkami, utrzymać kasyno, klub, komisariat, szpital, sale walk.
- **Ruch i dostępność:** logiczny wjazd z R1, garaże, podjazdy, miejsca parkingowe, wejścia i bezpieczne ścieżki dla NPC.
- **Technika:** additive scene/Addressables w docelowej aplikacji, wspólny `BraxGymPlayer`, LOD, GPU instancing, ograniczenia pamięci; pomiar na telefonach, nie tylko w Chromium.
- **Brama do lasu:** zostawić zablokowaną do osobnego etapu, nie tworzyć przypadkowego połączenia.

## Wariant koncepcyjny rozbudowy R2 (NIE WDROŻONY, do zatwierdzenia)
W masterze nie ma zatwierdzonej liczby nowych willi ani dokładnych współrzędnych nowych parcel. Poniższe ilości są **propozycją**, nie stanem gry:
- Dodatkowe **10–14 działek** o różnych rozmiarach, podzielone na 2–3 krótkie ulice mieszkalne z rondem lub placem, bez kopiowania tych samych fasad.
- Duży park o powierzchni co najmniej **2× obecnej części parkowej** (wymaga pomiaru mapy i kontroli stref walki); naturalne krawędzie, szerokie ścieżki, ławki i wydzielony plac spotkań.
- Wyodrębnione **2 punkty handlowe** wymagane w masterze: sklep odzieżowy oraz punkt odżywek, z czytelnymi wejściami.
- Strefa usług publicznych (komisariat/szpital) zachowuje połączenie drogowe i wejścia, bez sztucznych barier.
- Dodatkowy dojazd techniczny do garaży, logiczne chodniki, zatoki postojowe i bezpieczne dojścia do parku.
- Rezerwa na przyszłe misje, interakcje Nyxy i walki trzy na trzy, bez uruchamiania logiki w etapie remontu.
- Nowa granica terenu, brama i połączenie z R1 dopiero po zatwierdzeniu planu, kolizji i wydajności.

**Warunek rozpoczęcia rozbudowy:** użytkownik akceptuje szkic zagospodarowania i dostępne zasoby; remont istniejącego osiedla jest wcześniej odebrany.

## Odbiór
Brak zgody na publikację i brak gotowego remontu = **NIEODEBRANE**. Dowód: rzeczywisty build Unity + materiał wideo + PASS/FAIL kolizji, drzwi i oryginalnego Braxa + fizyczny iPhone. Przed wydaniem każdej wersji opisać dokładnie, które elementy są zrobione, a które nadal zablokowane przez brak assetów.
