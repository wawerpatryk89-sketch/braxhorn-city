# BYDGOSZCZ W MIEŚCIE R1 — układ z mapy, plan miejsca, graffiti

**Stan 23.11.2026, noc. To jest przygotowanie: mapa, wymiary, plan i materiał. W grze nie ma jeszcze nic —
żaden plik rejonu nie został zmieniony i żaden build nie był uruchomiony.**

## Co zlecił właściciel (23.11, 02:07–02:21)

- *„w miasto R1 odwzorować ulice w Bydgoszczy (…) blokowisko”*; *„musisz pobrać mapę, by to dobrze zrobić układ”*.
- Miejsca: **M. Skłodowskiej-Curie 32a** („ten blok i obrzeża, parę bloków dalej, parę w lewo, parę w prawo,
  tył, przód, te garaże”), **Stary Rynek, Mostowa, kawałek Gdańskiej** — „połączyć z tamtym osiedlem i z naszym miastem R1”.
- Gdzie: *„cała prawa strona, jak wychodzisz z siłowni, idziesz w lewo (…) tam gdzie jest sklep, ten duży, osobny
  (…) po całości naszej mapy, nie rozciągać jej dalej (…) aż po te nowe wieżowce”*.
- *„Ten pawilon z muralem to akurat jest ten bar mleczny.”*
- Graffiti: *„wybieraj te najlepsze (…) Bydgoszcz, HWDP możesz dać”* — na bloki i na slumsy (fawelę).
- *„Proszę zdjęcia do wglądu, zanim cokolwiek zaakceptuję i będzie puszczone. Wykończyć budynki (…) naturalną trawę.”*

## Jak to odczytałam (do potwierdzenia przez właściciela)

1. „Prawa strona” = **północ** Miasta R1, za lasem i działką supermarketu (`BraxMiastoGranice.DzialkaSupermarketu`,
   x −152…−100, z 126…162). Dziś granica gry jest tam na z = 180.
2. Mapa **nie wydłuża się** na wschód ani zachód: nowy teren leży wzdłuż istniejącej mapy, od morza po nowe
   dzielnice (x −872…100), i idzie tylko w głąb na północ.
3. Osiedle staje przy sklepie, stare miasto przy nowych wieżowcach, między nimi łącznik
   (ul. Jagiellońska → ul. Skłodowskiej-Curie; w Bydgoszczy to 2,2 km, w grze ok. 100 jednostek).

Rysunek: `plany/propozycja-miejsca-miasto-r1.png`.

### Rzeka i łączniki (właściciel 02:26)

*„Łącznik Jagiellońska–Skłodowska to mogą być dwa, nie jeden, albo jeden przez wiadukt, który trzeba przejechać
(…) tam też rzeka może płynąć, tę rzekę może jakoś zaokrąglić, że tam przepływa”.*

Propozycja 2 na rysunku:

- **Rzeka** wypływa ze starego miasta spod mostu przy Mostowej, zakręca łukiem na południowy wschód i płynie
  między lasem Miasta R1 a osiedlem do morza (korytarz z 190…240). Osiedle odsunięte o 60 j. na północ:
  blok 32a = (−145,9; **508,8**), wycinek z 250…755.
- **Most 1** — wjazd z miasta obok sklepu, przez rzekę.
- **Łącznik A** (północny brzeg) — Jagiellońska → Skłodowskiej-Curie, na wiadukcie nad doliną.
- **Łącznik B** — bulwar od Grodzkiej i **most 2** przez rzekę na południe osiedla (ul. Pestalozziego).
- Wjazd z nowych dzielnic na Stary Rynek bez mostu: ten sam brzeg.

Przebieg rzeki poza starym miastem jest wymyślony (w Bydgoszczy Brda płynie inaczej) — to decyzja właściciela.

## Co jest w tym folderze

| Plik | Co to |
|---|---|
| `plany/plan-bartodzieje-sklodowskiej-32a.png` | osiedle, 380 × 380 m, numery i piętra z mapy |
| `plany/plan-stary-rynek-mostowa-gdanska.png` | stare miasto, 700 × 730 m |
| `plany/propozycja-miejsca-miasto-r1.png` | gdzie to stanie względem dzisiejszego Miasta R1 |
| `dane/osiedle-sklodowskiej-32a.json` | 53 budynki, 123 odcinki ulic i chodników, 118 terenów — **w jednostkach gry** |
| `dane/stare-miasto-rynek-mostowa-gdanska.json` | 201 budynków, 319 odcinków, 156 terenów — w jednostkach gry |
| `graffiti/*.png`, `graffiti/graffiti.json` | 66 naklejek wyciętych z plansz właściciela, z wyborem |
| `plany/graffiti-wybor-na-sciany.png` | 36 wybranych na ściany, do obejrzenia |
| `narzedzia/*.py` | skrypty, którymi to powstało (ścieżki z mojej sesji — do podmiany przy ponownym użyciu) |

**Dane.** Źródło: OpenStreetMap, pobrane 23.11.2026 (`api.openstreetmap.org`). Każdy budynek ma obrys
(`obrys_xz`), adres, rodzaj, liczbę pięter (jeśli jest w mapie), długość, szerokość i kąt. Jednostka gry =
metr × 1,294; x na wschód, z na północ. Punkt zerowy osiedla (blok 32a) = (−145,9; 508,8), Starego Rynku =
(−781,4; 287,1) — to jedna para liczb w `narzedzia/uklad_gry.py`, gdyby miejsce miało być inne.

**Licencja mapy:** ODbL. Gra, która korzysta z tego układu, potrzebuje w napisach wpisu
„© autorzy OpenStreetMap”. Nie jestem prawniczką — to trzeba potwierdzić przed wydaniem.

## Osiedle — co wynika z mapy i ze zdjęć właściciela

| Co | Wymiar z mapy | Ze zdjęć |
|---|---|---|
| Wieżowce 28a, 30a, **32a**, 34a | ok. 30,5 × 18,5 m; **32a ma 11 kondygnacji** | pozostałe trzy wyglądają na tę samą wysokość — w mapie brak liczby; ślepy szczyt z numerem i nazwą ulicy, piony balkonów, szary cokół, wejście z daszkiem i schodkami |
| Bloki 28, 30, 34, 38 | 45–47 × 11–12 m, **4 piętra** | ocieplone, tynk w polach beż / żółty / brąz, płaski dach z kominami |
| Bloki 32, 36 | 48–50 × 10–13 m, pięter brak w mapie | jak wyżej |
| Rzędy garaży (7 w wycinku) | 20–28 × 6–7 m | tynk w różnych kolorach, bramy drewniane dwuskrzydłowe i brązowe segmentowe, plac z ubitej ziemi |
| Pawilon 34b | 14,3 × 13,1 m, parter | żółty, witryny, dach z okapem — **bar mleczny**; na bocznej ścianie mural |
| Pawilony przy ulicy (32B, 34B i sąsiednie) | 13–20 × 9–13 m, parter | handel |
| Przedszkole 36a | 34,6 × 21,1 m | zielone ogrodzenie panelowe |
| Ulica Skłodowskiej-Curie | 2 pasy, asfalt | osiedlowe uliczki z parkowaniem wzdłuż, kostka betonowa, szlabany, wiata śmietnikowa z siatki |

Do wymyślenia nic: czego nie ma w mapie ani na zdjęciach, zostaje puste i idzie na listę braków (ZASADY 5.5).

## Stare miasto — co wynika z mapy

Stary Rynek to plac 79 × 61 m. Mostowa (deptak) ma 174 m i kończy się mostem długości 45 m nad Brdą.
Focha ma tory tramwajowe; przy Placu Teatralnym zaczyna się Gdańska. Przy Grodzkiej stoją spichrze.
Liczba pięter jest w mapie tylko dla 60 z 729 budynków — wysokości kamienic trzeba brać ze zdjęć właściciela.

## Graffiti

Z siedmiu plansz wyciętych jest 66 naklejek z przezroczystym tłem. W `graffiti.json` każda ma pole `wybor`:

- **`sciana` — 36 sztuk**, w tym oba „Bydgoszcz” i „HWDP” wskazane przez właściciela. Na bloki i na fawelę.
- **`odrzucone` — 30 sztuk**: słabsze, powtórzone oraz osiem napisów-szyldów (POLICE, TAXI, HOTEL, BAR, SHOP,
  FITNESS, GYM, BEACH). Właściciel 02:24: *„to nie mają być szyldy, tylko graffiti na ścianach”*.

**Plansza D (właściciel 02:29) — 17 sztuk, wszystkie na ściany:** BARTODZIEJE, BLOKI, 32A, BRDA, STARE MIASTO,
KRÓLOWIE BLOKÓW, SZACUNEK, NOKAUT, USTAWKA, SALKA, BXC, BYK W KORONIE, WZGÓRZE i NIE WCHODŹ (fawela), strzałka, dwa tagi.
Razem na ściany: **53**.

**Mural z prawdziwym klubem (właściciel 02:31) — nie wchodzi w tej postaci.** To cudza praca z herbem i nazwą
prawdziwego klubu; do gry potrzebny jest własny mural osiedla (własny znak, np. byk w koronie, własny napis).
Plansza od właściciela wystarczy.

Wybór z plansz A–C i planszy walk jest mój — właściciel ogląda `plany/graffiti-wybor-na-sciany.png` i poprawia.
**Cztery kafle planszy walk nie zostały wycięte:** mają prawdziwą markę albo realistyczną postać zawodnika (ZASADY 7.4).

## Woda i piasek (właściciel 02:29–02:31)

Miejsce potwierdzone: *„po lewej stronie mapy masz fawele (…) po drugiej stronie, tam gdzie jest wielki
supermarket, po prawej, tam masz zrobić te osiedle ze starym rynkiem”*.
Rzeka ma mieć prawdziwą wodę z prądem **do morza** (ZASADY 5.7a) — w propozycji 2 płynie ze starego miasta na wschód.
Piasek plaż: jeden, wybiera właściciel (ZASADY 5.11).

## Czego nie wolno przenieść ze zdjęć

Prawdziwe szyldy sklepów i lokali, nazwy firm, mural z nazwą prawdziwego klubu (ZASADY 7.4). Zdjęcia ulic są
wzorem wyglądu, nie teksturą. Nazwy ulic i układ zostają.

## Czego jeszcze nie ma — kolejność dalszej pracy

1. **Właściciel potwierdza miejsce** na rysunku propozycji (albo wskazuje inne).
2. Granica gry na północy (`BraxMiastoGranice.Z1`, las z ≥ 126) i doczytywane kwadraty 150 m dla nowego terenu.
3. Generator osiedla z `dane/osiedle…json`: drogi, chodniki, trawa, bloki z oknami, balkonami i wejściami,
   garaże, pawilon. Materiały z `MATERIALY-WLASCICIELA.md` (ZASADY 5.8), budynki wykończone (5.10), budżet kadru (10.2).
4. Generator starego miasta, most i rzeka.
5. Graffiti na ścianach osiedla i faweli.
6. **Zdjęcia do wglądu** (ZASADY 8.8) — z buildu, który uruchamia tylko właściciel (8.1). Bez zdjęć nic nie jest przyjęte.

Nie sprawdzono: gdzie dokładnie stoi siłownia (na rysunku przybliżenie), ile trójkątów da ten teren,
czy teren za z = 180 jest w scenie pusty.
