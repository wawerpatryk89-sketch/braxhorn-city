# ZASADA GLOBALNA: modele towaru do sklepów (Tripo właściciela) — 11.11.2026 (właściciel, 22:37–22:50)

> „Robię ci piękne rzeczy 3D w Tripo. Zaraz dostaniesz wszystko do sklepu i zrobisz sklep kodem.” (22:37)
> „Tworzę ciuchy, torebki, buty i robimy to globalnie na całą grę do sklepów, tak? Żeby każdy z tego korzystał. Bo to
> trochę waży jednak.” (22:44)
> „Jak czegoś nie wiesz, chcesz podgląd, jak wygląda, to mi mówisz zawsze, a nie na oko.” (21:57)

Obowiązuje w całej grze, u wszystkich wykonawców.

> „Tylko i wyłącznie jakość premium. Resztę dyskwalifikujesz.” (właściciel 12.11, 01:20): słabe sztuki z planszy (zniszczona siatka, zlane kolory, połamane) nie wchodzą do paczki (`"skip": true`), a właściciel dostaje informację, co odrzucono i dlaczego.

- **Podział ról.** Właściciel robi w Tripo plansze towaru: ubrania, torebki, buty, sprzęt. Wykonawca tnie je, skaluje, pakuje i rozstawia w sklepach, szafach i pokojach całej gry.
- **Jedna paczka dla wszystkich.** Żadnych kopii w rejonach. Te same modele biorą: galeria Mistrzów, sklepy miasta, szafy w mieszkaniach i hotelach.
- **Jak czegoś nie wiemy (wygląd, fason, kolor), pytamy właściciela o zdjęcie.** Nie robimy na oko.
- **Tylko marka BRAXHORN.** Na planszach nie ma logo prawdziwych marek.
- **Wgrywanie dużych plików:** szkic wydania na GitHubie (`releases/new?tag=sklep-tripo-…`, „Save draft”, nie publikować). Pobiera go wykonawca. Eksport z Tripo: GLB z teksturą 2K, około 5 MB na planszę.

## Format planszy (najlepszy)

- Jeden model z Tripo, w nim około 10 sztuk ustawionych w rzędach, z odstępami.
- Jedna tekstura (atlas) na całą planszę.
- Osobne plansze dla kategorii: koszulki, koszule, kurtki, spodnie, torebki, buty sportowe, buty eleganckie, sprzęt sportowy.

## Cięcie (`unity-tools/sklep/split_sheet.py`)

Wejście: GLB (z FBX przez FBX2glTF z npm: `fbx2gltf`) i `spec.json` z nazwami sztuk, rzędami, kolumnami i wysokościami w metrach.

Narzędzie robi kolejno:
1. Rzutuje widok z przodu, rozdziela sylwetki i przypisuje każdy kawałek (rękaw, guzik) do jego sztuki.
2. Dla każdej sztuki zapisuje osobny `.glb` w prawdziwych metrach: przód +Z, punkt zawieszenia na górze (ubrania) albo podstawa na dole (półki).
3. Robi wersję `_wieszak` (spłaszczoną przód–tył) do wieszaków.
4. Zapisuje wspólne tekstury w `_tekstury/`: kolor JPEG 2K, normal 2K, metal i szorstkość 1K. Cała plansza dzieli jeden zestaw tekstur.
5. Zapisuje `katalog.json` z nazwą, rodzajem, liczbą trójkątów i wymiarami każdej sztuki.

## Paczka i CI

- Paczka leży w `unity-city/Prywatne/braxhorn-sklep-<nazwa>/`. Jest zaszyfrowana CMS do certyfikatu transferowego CI, jak filmy. Zawiera pocięte modele i oryginał od właściciela.
- W każdym workflow APK, w kroku z `KLUCZ` przed Unity, uruchamia się `python3 unity-tools/sklep/install_sklep_modele.py`. Skrypt rozpakowuje wszystkie paczki do `Assets/Source/Prywatne/Miasto/<target>/`.
- W kodzie: `BraxMiastoAssety.Jest("sklep_ubrania_01")` i `BraxMiastoAssety.Model("sklep_ubrania_01", "ubranie_trencz_bezowy_wieszak")`. Stawiamy przez `Postaw(..., statyczny: false, cienie: false)`.
- Jedna siatka powtarza się na wielu wieszakach bez kopiowania pamięci. Dlatego nie stawiamy ich jako statyczne: statyczne łączenie kopiowałoby siatki.
- Bez klucza albo paczki sklep stawia zastępczy towar z kodu (`BraxKatalog.Garment` itd.).

## Paczki (stan 11.11.2026)

| paczka | target | sztuki |
|---|---|---|
| `braxhorn-sklep-ubrania-01` | `sklep_ubrania_01` | polo granat, henley beż, koszula w kratę, koszula biała, bluza grafit, kardigan beż, kurtka puchowa czarna, trencz beż, chinosy beż, dresy szare (+ wersje `_wieszak`) |
| `braxhorn-sklep-buty-01` | `sklep_buty_01` | pary: biegowe szare, sneakersy beż, trampki wysokie czarne, koszykarskie czerwone, korki neonowe, trekkingowe, sztyblety brązowe, workery miodowe, szpilki czerwone, sandały na obcasie (widok `top`, skala 1,1) |
| `braxhorn-sklep-torebki-01` | `sklep_torebki_01` | kuferek bordo kroko, lakierowana czarna, jeansowa, koszyk słomkowy, srebrna z bigielkiem, hobo czarna z ćwiekami, hobo taupe, kopertówka kremowa, kuferek różowy, listonoszka koniak (skala 1,6) |
| `braxhorn-sklep-ubrania-02` | `sklep_ubrania_02` | ramoneska czarna, bomberka khaki, bluza z kapturem szara, koszulka biała (zmatowiona: `rough_min` 0,55), katana jeansowa, sweter warkocz kremowy, jeansy szerokie, bojówki khaki, spódnica mini jeansowa, sukienka bordo (+ wersje `_wieszak`) |
| `braxhorn-sklep-manekiny-01` | `sklep_manekiny_01` | czarne manekiny w pełnych strojach: bomberka + jeansy, beżowy żakiet + spódnica + torebka, granatowy garnitur, czerwona suknia, czarny dres |
| `braxhorn-sklep-manekiny-02` | `sklep_manekiny_02` | czarne manekiny: kurtka baseballowa + bojówki, czarny garnitur, dres z kapturem, katana + jeansy (białe manekiny z planszy odrzucone przez właściciela 23:22 i wycięte: `"skip": true`) |
| `braxhorn-sklep-sport-01` | `sklep_sport_01` | odzież sportowa BRAXHORN: koszulka meczowa czarno-złota, koszulka czerwona, bluza z kapturem, bluza dresowa z lampasami, spodnie dresowe, spodenki, legginsy, kurtka puchowa, wiatrówka, koszulka bez rękawów |
| `braxhorn-sklep-bizuteria-01` | `sklep_bizuteria_01` | zegarki złoty i stalowy (na aksamitnych poduszkach, bo bransolety są ucięte), łańcuszek, naszyjnik z wisiorkiem, bransolety, obrączka, pierścionek, kolczyki (cięcie ręcznymi polami `boxes`, złoto rozjaśnione `color_gain`) |
| `braxhorn-sklep-sprzet-01` | `sklep_sprzet_01` | piłki nożna, do kosza, siatkowa, hantle 10 kg, kettlebell, rękawice bokserskie, worek bokserski, skakanka, mata (rakieta odrzucona: zniszczony naciąg) |
| `braxhorn-sklep-buty-02` | `sklep_buty_02` | sneakersy białe, biegowe czarne i białe, mokasyny, półbuty, trampki czerwone, trapery, szpilki czarne, wsuwane, zimowe z kożuszkiem |
| `braxhorn-sklep-buty-03` | `sklep_buty_03` | **14 pojedynczych butów sportowych** z trzeciej planszy właściciela (12.11 13:58): każdy stoi na podeszwie, noskiem wzdłuż półki, bokiem do klienta, w prawdziwej długości 25–29 cm (`unity-tools/sklep/buty_pojedyncze.py`). Jedyna paczka butów wystawiana w sklepach |
| `braxhorn-sklep-walka-01` | `sklep_walka_01` | sporty walki BRAXHORN: rękawice czarno-złote, kaski bokserskie (czerwony, niebieski, z kratą), ochraniacz na zęby w pudełku, buty bokserskie, zapaśnicze i treningowe (buty w rotacji sklepów sportowych i ściany butów SPORT ARENA, rękawice na stojaku; kaski i ochraniacz do rozstawienia) |
| `braxhorn-sklep-silownia-01` | `silownia_sprzet_01` | **sprzęt na siłownie (globalnie, wszystkie siłownie w grze):** ławka płaska (wydłużona do 1,15 m: `stretch`), dwie ławki regulowane, hantle heksagonalne, stojak 3-poziomowy z kompletem hantli |
| `braxhorn-sklep-silownia-02` | `silownia_sprzet_02` | **maszyny na siłownie (globalnie):** ławka olimpijska ze stojakiem, maszyna do prostowania nóg ze stosem, modlitewnik, ławka rzymska 45°, suwnica / hack (cięcie `cluster3d`: maszyny nachodzą na siebie w każdym widoku, ale w 3D się nie dotykają) |
| `braxhorn-sklep-silownia-03` | `silownia_sprzet_03` | talerze obciążeniowe BRAXHORN z uchwytami: 5, 10, 20 kg (dwa rozmiary) |
| `braxhorn-sklep-perfumy-01` / `-02` | `sklep_perfumy_01` / `_02` | 20 flakonów perfum (część z bykiem BRAXHORN): półki i lada perfumerii (G1: sklepy „cosmetics”) |
| `braxhorn-sklep-elektronika-01` | `sklep_elektronika_01` | laptopy (gamingowy czerwony, złoty, srebrny), tablety (3), zegarek chronograf, smartwatch, powerbanki: stoły i ściany sklepów RTV |
| `braxhorn-sklep-ring-01` | `silownia_ring_01` | ring bokserski BRAXHORN GYM 7,85 × 7,8 m (mata 0,86 m, 4 liny): zepsute nadruki Tripo zamaskowane, napis 3D BRAXHORN (stal) + GYM (złoto) na obu fartuchach, nadruk byka właściciela na macie, prawdziwe liny ze skórzanymi przekładkami (`unity-tools/sklep/ring_braxhorn.py`). Do wszystkich siłowni i sal treningowych |
| `braxhorn-sklep-postacie-01` | `postacie_01` | **postacie (globalnie):** 5 dziewczyn klubowych (czarna, czerwona, biała i różowa sukienka, panterka; 1,71–1,75 m, statyczne) stoją w dwóch grupach po obu stronach drzwi każdego klubu (E1 CENTRUM ROZRYWKI, KASYNO WAWER T11), gracz przechodzi między nimi; 4 dziewczyny ze szkieletem (czerwona i niebieska bluzka, czarne i białe bikini) z własnymi klipami Walking, Running, Idle, Taunt, Warming Up, Head Hit, Hit To Body (`unity-tools/sklep/postac_animacja.py`: przeniesienie ruchu na ich kości Mixamo, prędkość kroku w `<model>.json`) chodzą po promenadzie z mózgiem przechodnia `BraxPieszy` (postoje, gesty, rozmowy, reakcja na cios i ucieczka biegiem) |
| `braxhorn-sklep-basen-01` | `basen_01` | **basen, spa i aquapark (globalnie):** 2 zjeżdżalnie dla dzieci z Tripo właściciela (z palmą 2,7 m, z wieżyczką 3,4 m), 5 stołów do masażu (drewniany, kosmetyczny, składany czarny, leżanka do masażu tajskiego z Tripo + składany miętowy z biblioteki), z biblioteki P1: jacuzzi, kabina sauny, kabina prysznicowa, krzesło ratownika, ławka szatniowa, podwójna umywalka (przeliczone na metry, podstawa y = 0). Używa ich AQUAPARK BRAXHORN |
| `braxhorn-sklep-plakaty-01` | `plakaty_01` | **plakaty BRAXHORN NUTRITION** z dwoma kolegami właściciela (whey, kreatyna; logo adidas zamaskowane) jako podświetlane kasetony: ściana SPORT ARENA w G1 i citylighty przy wejściu, promenada (8 w rzędzie palm i 1 przy siłowni plenerowej), plac E1. `BraxChampionsPlakaty.Plakat()` jest publiczne dla siłowni, sal treningowych i sklepów z odżywkami |

Buty leżą na planszy w rzędach na podłodze, więc tnie się je w widoku z góry (`"view": "top"`). Cała plansza ma jedną skalę (`"scale"`), dzięki czemu rozmiary sztuk zgadzają się między sobą. Na półkach stawiamy przez `ShopModel` (podstawa na półce, przód do klienta).

## Wieszaki i manekiny (stan 11.11.2026, 23:30)

- Na wieszaku wiszą po 3 sztuki tego samego modelu obok siebie (rozmiary), potem następny model. Nie układamy na przemian „sukienka, spódnica, sukienka”.
- Manekiny stoją w witrynach i przy wejściu. Biorą pełne stroje z paczek `sklep_manekiny_*`, dobrane do stylu sklepu: sport (dres), elegancja (garnitur, suknia), denim (katana, jeansy), urban (kurtka baseballowa, dres z kapturem). Bez paczki zostaje manekin z kodu.
- Sklep z torebkami nie ma wieszaków na ubrania. Ma dwie półki z orzecha (1,1 m i 2,15 m) z torebkami właściciela, a pod nimi walizki.
- Białe manekiny nie wchodzą do gry (właściciel 23:22).

## Siłownie (właściciel 12.11.2026, 01:02)

> „Tak ci wysyłam sprzęt na siłownię, zapisać to globalnie na wszystkie siłownie, ten sprzęt.”

Sprzęt z paczki `silownia_sprzet_01` stawiamy w każdej siłowni w grze: w Mistrzach, w mieście R1, na osiedlu, w obozie i w hotelach. Stawia się go przez `BraxMiastoAssety.Model("silownia_sprzet_01", "lawka_regulowana")` i `Postaw(..., statyczny: false)`. Kolejne plansze siłowni (maszyny, bramy, gryfy, talerze) dochodzą jako `silownia_sprzet_02…`. Żaden rejon nie robi własnej kopii.

## Przegląd modeli z każdej strony (właściciel 12.11.2026, 13:58–14:03)

> „Przejrzeć te modele, czy wszystko się zgadza, żeby nie było lipy.” „Co się nadaje, zostawiamy, co nie, to nie i tyle.” „Czego nie da rady naprawić” — wyrzucamy.

Każdy model z Tripo oglądamy z sześciu stron, zanim trafi do sklepu: `node unity-tools/sklep/przeglad_modeli.js <folder paczki> <three> <arkusz.png>` (przód, tył, boki, góra, spód; `PRZEGLAD_KLIENT` dodaje widok od klienta). Sam widok z przodu nie wystarcza, bo Tripo dorabia tył i boki po swojemu.

**Wynik przeglądu z 12.11:**

| co | werdykt | co zrobione |
|---|---|---|
| buty `sklep_buty_01`, `sklep_buty_02` (20 par) | **nie nadają się**: każda para to jedna bryła, pięty albo całe wnętrza zrośnięte; po rozcięciu zostaje wyrwany bok, od nosków też widać zrost | paczki zostają w repozytorium, ale sklepy ich nie stawiają (`Odrzucony`) |
| trzecia plansza butów (37 sztuk) | 14 czystych pojedynczych butów; reszta ma nogi, zrośnięte pary albo wzory prawdziwych marek | paczka `sklep_buty_03` |
| piłka nożna, piłka siatkowa | dobre tylko z jednej strony, reszta rozmazana | piłki z kodu, szyte z prawdziwych paneli (`BraxKatalog.Ball`): 12 pięciokątów i 20 sześciokątów, siatkowa 18 paneli |
| piłka do kosza | dobra z każdej strony | zostaje; zdjęty pokręcony napis |
| rękawice czerwone | dwie rękawice zrośnięte w jedną krzywą bryłę | wyrzucone; czarno-złote leżą na półce |
| buty zapaśnicze | zrośnięta para | wyrzucone |
| perfumy: bursztyn, czerwone ze złotą szyjką, żółta kula, lawendowe | rozdarte boki albo tył | wyrzucone; zostaje 16 flakonów |
| kolczyki (obie pary) | urwane koło, luźne złote okruchy | wyrzucone |
| koszula biała | plecy w kratę (z innej koszuli z planszy) | zdjęta z wieszaków |
| spodnie dresowe z lampasami | naszywka to poszarpana czarna plama na złotych lampasach | zdjęte z wieszaków |
| kettlebell | luźny okruch przy uchwycie, wgnieciona tabliczka | okruch wycięty (`okruchy.py`), tabliczka wyrównana do kuli |
| zegarki | brak tarczy od tyłu (gładka skorupa) | stoją owinięte na aksamitnej poduszce, tył jest w poduszce |
| ubrania, torebki, elektronika, manekiny, kaski, sprzęt basenowy, siłownia | całe z każdej strony | zostają |
| skakanka | cała; pokręcony napis na rączkach | napis zdjęty, leży zwinięta na stojaku w SPORT ARENA |
| smartwatch, chronograf, 2 powerbanki | całe, dotąd nigdzie nie stały | półka na wysokości oczu w sklepach z telefonami i elektroniką |
| para hantli 10 kg, zwinięta mata | hantle mają wyrwane kolce i dziury na głowicach | nie stawiamy; w sklepie stoją hantle i maty z kodu |

**Napisy.** Tripo dobrze rysuje tylko duży napis (BRAXHORN na czarno-złotej koszulce meczowej). Każdy mały nadruk wychodzi jako pokręcone litery („DRAXRUNV” na piłce, „BRAJHUH” na mankiecie rękawicy). Takie nadruki zdejmujemy z tekstury bez śladu, byk zostaje: `unity-tools/sklep/napisy_precz.py <rozszyfrowana paczka> unity-tools/sklep/napisy/<paczka>.json`. Lista nadruków jest zapisana tak, jak widać je na arkuszu przeglądu: model, widok i prostokąt w kafelku.

**Jak robić plansze w Tripo, żeby nie było poprawek:**

- Buty: **pojedyncze sztuki, nie pary**, z odstępem między nimi. Para na obrazku zawsze zrasta się w jedną bryłę.
- Najwyżej 8–10 sztuk na planszę. Przy 37 butach na jednej planszy każdy dostaje około 250 pikseli tekstury i wychodzi nieostry.
- Bez małych napisów na obrazku. Sam byk wychodzi dobrze, duży napis też; małe litery zawsze się kręcą.
- Bez nóg i stóp w butach.

## Co jest gdzie (stan 12.11.2026, 15:30)

Właściciel 15:18: „Mam nadzieję, że to nie przepadło.” Nic nie przepadło. Każda plansza z Tripo jest w zaszyfrowanej paczce:

- **W paczkach tej gałęzi (Mistrzowie):** ubrania 01–02, buty 01–03, torebki, biżuteria, manekiny 01–02, sport, sprzęt, walka, perfumy 01–02, elektronika, siłownia 01–03, ring 01, basen (zjeżdżalnie dla dzieci, stoły do masażu), postacie, plakaty.
- **W paczkach gałęzi R1:** motory, rowery, meble 01, muzeum, boks 01–02 (czarno-złoty i czerwony), końcówki wyciągu, ring 02.
- **Jeszcze niepocięte, zapisane jako oryginały** (`braxhorn-tripo-oryginaly-20261112`, nie wchodzi do buildu): 5 aut, duże maszyny na siłownię (00:56), trzy plansze mebli (01:43, 01:47, 01:51), duża zjeżdżalnia.

