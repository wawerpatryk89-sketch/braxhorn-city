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
| `braxhorn-sklep-walka-01` | `sklep_walka_01` | sporty walki BRAXHORN: rękawice czarno-złote, kaski bokserskie (czerwony, niebieski, z kratą), ochraniacz na zęby w pudełku, buty bokserskie, zapaśnicze i treningowe (buty w rotacji sklepów sportowych i ściany butów SPORT ARENA, rękawice na stojaku; kaski i ochraniacz do rozstawienia) |
| `braxhorn-sklep-silownia-01` | `silownia_sprzet_01` | **sprzęt na siłownie (globalnie, wszystkie siłownie w grze):** ławka płaska (wydłużona do 1,15 m: `stretch`), dwie ławki regulowane, hantle heksagonalne, stojak 3-poziomowy z kompletem hantli |
| `braxhorn-sklep-silownia-02` | `silownia_sprzet_02` | **maszyny na siłownie (globalnie):** ławka olimpijska ze stojakiem, maszyna do prostowania nóg ze stosem, modlitewnik, ławka rzymska 45°, suwnica / hack (cięcie `cluster3d`: maszyny nachodzą na siebie w każdym widoku, ale w 3D się nie dotykają) |
| `braxhorn-sklep-silownia-03` | `silownia_sprzet_03` | talerze obciążeniowe BRAXHORN z uchwytami: 5, 10, 20 kg (dwa rozmiary) |
| `braxhorn-sklep-perfumy-01` / `-02` | `sklep_perfumy_01` / `_02` | 20 flakonów perfum (część z bykiem BRAXHORN): półki i lada perfumerii (G1: sklepy „cosmetics”) |
| `braxhorn-sklep-elektronika-01` | `sklep_elektronika_01` | laptopy (gamingowy czerwony, złoty, srebrny), tablety (3), zegarek chronograf, smartwatch, powerbanki: stoły i ściany sklepów RTV |
| `braxhorn-sklep-ring-01` | `silownia_ring_01` | ring bokserski BRAXHORN GYM 7,85 × 7,8 m (mata 0,86 m, 4 liny): zepsute nadruki Tripo zamaskowane, napis 3D BRAXHORN (stal) + GYM (złoto) na obu fartuchach, nadruk byka właściciela na macie, prawdziwe liny ze skórzanymi przekładkami (`unity-tools/sklep/ring_braxhorn.py`). Do wszystkich siłowni i sal treningowych |

Buty leżą na planszy w rzędach na podłodze, więc tnie się je w widoku z góry (`"view": "top"`). Cała plansza ma jedną skalę (`"scale"`), dzięki czemu rozmiary sztuk zgadzają się między sobą. Na półkach stawiamy przez `ShopModel` (podstawa na półce, przód do klienta).

## Wieszaki i manekiny (stan 11.11.2026, 23:30)

- Na wieszaku wiszą po 3 sztuki tego samego modelu obok siebie (rozmiary), potem następny model. Nie układamy na przemian „sukienka, spódnica, sukienka”.
- Manekiny stoją w witrynach i przy wejściu. Biorą pełne stroje z paczek `sklep_manekiny_*`, dobrane do stylu sklepu: sport (dres), elegancja (garnitur, suknia), denim (katana, jeansy), urban (kurtka baseballowa, dres z kapturem). Bez paczki zostaje manekin z kodu.
- Sklep z torebkami nie ma wieszaków na ubrania. Ma dwie półki z orzecha (1,1 m i 2,15 m) z torebkami właściciela, a pod nimi walizki.
- Białe manekiny nie wchodzą do gry (właściciel 23:22).

## Siłownie (właściciel 12.11.2026, 01:02)

> „Tak ci wysyłam sprzęt na siłownię, zapisać to globalnie na wszystkie siłownie, ten sprzęt.”

Sprzęt z paczki `silownia_sprzet_01` stawiamy w każdej siłowni w grze: w Mistrzach, w mieście R1, na osiedlu, w obozie i w hotelach. Stawia się go przez `BraxMiastoAssety.Model("silownia_sprzet_01", "lawka_regulowana")` i `Postaw(..., statyczny: false)`. Kolejne plansze siłowni (maszyny, bramy, gryfy, talerze) dochodzą jako `silownia_sprzet_02…`. Żaden rejon nie robi własnej kopii.

