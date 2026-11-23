# ZASADY — BRAXHORN CITY

**To jest jedyny obowiązujący zbiór zasad.** Obowiązuje każdy rejon i każdego wykonawcę:
Osiedle, Miasto R1, Miasto Mistrzów, pokój prób, pojazdy i każdy następny rejon.

**Gdy cokolwiek innego mówi inaczej — wygrywa ten plik.** Dotyczy to komentarzy w kodzie,
notatek w projekcie i wszystkich wcześniejszych dokumentów z zasadami, w szczególności:
`ZASADA-0-JEDEN-STANDARD-OBRAZU-16-11`, `ZASADA-OBRAZ-MINIMUM-OSIEDLE-16-11`,
`ZASADA-KOLORY`, `STANDARD-TECHNICZNY`, `ZASADA-NADRZEDNA-WSZYSTKO-REALNE-16-11`,
`ZASADA-MIGOTANIE-PASOW-I-CIENKICH-LINII-16-11`, `ZASADA-ZERO-KLOCKOW`,
`ZASADA-GRAFIKA-NAJLEPSZA-NA-TELEFON-18-11`, `GRAFIKA-NA-TELEFON-JAK-ROBIA-NAJLEPSI-18-11`.
Tamte zostają jako **uzasadnienia i pomiary** — nie jako źródło decyzji.

Zasada jest jednostronna: **minimum wolno przekraczać w górę, nigdy w dół.**

---

## 0. PO CO TEN PLIK I JAK GO SPRAWDZIĆ

Zasady były rozsypane po dokumentach projektu. Każdy wykonawca czytał inny, bo każdy miał
własny `CLAUDE.md` na własnej gałęzi, a dokumentów narosło osiem i część mówiła co innego.
Jeden powoływał się na sprawdzacz `sprawdz_zasade_0.py` jako „jest, działa" — a tego pliku
nigdy nie było w repozytorium.

Dlatego: **jeden plik w korzeniu `main`, plus sprawdzacz, który naprawdę istnieje.**

    python3 -I unity-tools/claude/sprawdz_zasady.py .

Kod wyjścia 1 = są błędy. **Gałąź z błędami nie idzie do odbioru.**

Jeżeli zasada i kod się nie zgadzają, to jest błąd **zasady albo kodu** — nie powód, żeby
je zignorować. Zgłoś właścicielowi, popraw jedno albo drugie.

---

## 1. CEL: JAKOŚĆ KONSOLOWA NA TELEFONIE

Właściciel 18.11, 17:43: *„Jakość konsolowa nas interesuje, to jest raz. Jak najlepsza."*
Właściciel 18.11, 17:31: *„Poprzeczka to najlepsze gry na telefon. Braxhorn ma wyglądać
lepiej, nie wystarczająco."*

**Poprzeczką są najlepsze gry mobilne na rynku, nie nasz wczorajszy build.** Porównanie robi
się na zrzutach: ich kadr obok naszego kadru.

Co to NIE znaczy: że wolno zrobić coś, co nie chodzi płynnie. Gra ma chodzić na telefonie,
na baterii, bez przegrzewania.

**Telefon odniesienia: moto g86 5G, 7393 MB RAM, Mali-G615 MC2, Vulkan, Android 16.**
Zmierzona granica: scena z 1,5 GB danych działa, 1,7 GB zamyka grę. Diagnostyka z 18.11
pokazała śmierć przy 2103 MB przy włączaniu sceny.

**Trudność i nakład pracy nie są wymówką.** Jeżeli czegoś nie da się zrobić tak, jak w grze
z poprzeczki — szuka się taniej metody, która wygląda tak samo, a nie rezygnuje.

---

## 2. JAK SIĘ ROBI KONSOLOWY OBRAZ NA TELEFONIE

To jest sekcja, z której wynika reszta. Źródła: Unity, Arm (Mali), Qualcomm (Adreno),
Imagination (PowerVR). Pełny research z linkami: dokument projektu
`GRAFIKA-NA-TELEFON-JAK-ROBIA-NAJLEPSI-18-11`.

**2.0. Telefon rysuje obraz kafelkami.** Ekran jest dzielony na kafelki 32×32 lub 64×64
i każdy jest rysowany w maleńkiej pamięci na samym układzie, a dopiero gotowy trafia do
pamięci głównej. Tak robią wszystkie układy mobilne. Stąd jedna konsekwencja, z której
wynika cała reszta: **wąskim gardłem jest przepustowość pamięci, nie moc liczenia.**

**2.1. Wypalone bije liczone. Zawsze.**
Zanim ktokolwiek doda światło w czasie rzeczywistym, odbicie w czasie rzeczywistym albo
efekt na cały ekran — musi pokazać, że tego samego nie da się wypalić. Unity podaje jako
wzorzec Shadowguna: dynamicznego światła prawie wcale, statyczna geometria z wypalonym
światłem, sondy światła dla ruchomych, kontrast i połysk **namalowany w teksturze**,
promienie światła ręcznie zamodelowane, mgła udawana płaszczyznami.
Każde dynamiczne światło oznacza, że każdy obiekt w jego zasięgu jest rysowany jeszcze raz.

**2.2. `discard` i przezroczystość na wycinanie są ZAKAZANE poza listą wyjątków.**
To jest najdroższy pojedynczy błąd, jaki można zrobić na telefonie. Imagination pisze wprost:
nie używać `discard`. Arm: `discard` wymusza późne testowanie głębi i wyłącza mechanizmy
odrzucające niewidoczne piksele — **jeden `discard` w shaderze wyłącza wczesne odrzucanie dla
całego wywołania rysowania, nawet jeśli nigdy się nie wykona.** Przy roślinności jeden piksel
ekranu potrafi być liczony 10–15 razy.

Zamiast: przezroczystość mieszana albo nieprzezroczysta geometria. Kolejność rysowania:
**nieprzezroczyste → wycinane → mieszane.**

Wyjątek wolno mieć tam, gdzie mieszana nie zadziała (liście z gęstą sylwetką), ale wtedy:
wpis w logu buildu, ile obiektów go używa, i ograniczenie do najbliższego poziomu LOD.

**2.3. Efekty na cały ekran — nie.** Każdy taki efekt wypycha rysowanie poza kafelek
i dokłada przebieg przez pamięć główną. Nasz obraz ma **jedno** przejście koloru
(nasycenie, kontrast) i nic więcej. Bez bloomu, bez SSAO, bez głębi ostrości.

**2.4. Wygładzanie krawędzi robi MSAA, nie efekt po obrazie.** Na kafelkowym układzie MSAA
jest tanie, bo dzieje się w kafelku. Wypalone w buildzie (`BraxObrazBuild`), nigdy zmieniane
w trakcie gry — zmiana MSAA w locie odtwarza bufor ekranu i na Vulkanie zamykała aplikację
(właściciel 31.10: „wypierdala z aplikacji").

**2.5. Nigdy „Don't Clear" na kamerze.** Na telefonie to każe układowi wczytać poprzednią
zawartość z pamięci głównej. Drobiazg w ustawieniach, realny koszt.

**2.6. Mało materiałów, atlasy tekstur.** Mniej materiałów = więcej rzeczy rysowanych
jednym wywołaniem. Atlas zamiast dwudziestu osobnych tekstur ładuje się szybciej i nie zrywa
łączenia w paczki.

**2.7. Nie gonimy fizycznej poprawności.** Unity pisze wprost: próba odwzorowania świata
w całości na telefonie kończy się grą ograniczoną, szarą i ciężką. Wygląd buduje się
sztuczkami — to jest rzemiosło, nie chodzenie na skróty.

---

## 3. TEKSTURY

| Klasa | Co to jest | Limit | Format Android |
|---|---|---|---|
| `TwarzBohatera` | Patryk, Monika, przeciwnicy w ringu — twarz z 0,5 m | 2048 | ASTC 4×4 |
| `Postac` | reszta obsady, ciała bohaterów | **4096** (patrz 3.1a) | ASTC 6×6 |
| `Mimika` | atlas mimiki (8 poziomów ułożonych 4×2) | 2048 | ASTC 4×4 |
| `ZBliska` | murale, szyldy, ekrany, wnętrza w zasięgu ręki | 2048 | ASTC 6×6 |
| `Swiat` | elewacje, grunt, drogi, modele z Poly Haven, wnętrza | 1024 | ASTC 6×6 |
| `Daleko` | kopie z daleka, niebo, tło | 512 | ASTC 6×6 |

**3.1. Sufit świata to 2048.** 4096 poza obsadą — tylko z pisemnym wyjątkiem właściciela
w treści commita. Dziś zatwierdzony jest jeden: mural 19 na siłowni.
Dlaczego: 4K to czterokrotność pamięci wobec 2K. Ściana 4 m pokryta teksturą 2K to 512
pikseli na metr; z dwóch metrów oko nie rozróżnia więcej.

**3.1a. Obsada ma 4096 na ciałach i to NIE jest wyjątek do wyproszenia.**
Właściciel 18.11, 16:50: *„mają wyglądać tak, jak w ostatnim teście pokoju"* — a tam sufit
ciał to 4096. To jest obraz, który widział i przyjął, więc wygrywa z sufitem świata.
Zmierzony koszt: trzy postacie z mapami 4096 to +67 MB. Oszczędność siedzi gdzie indziej —
ASTC 6×6 zamiast 4×4 schodzi na obsadzie Osiedla z 483 MB na 222 MB.

**3.2. ASTC 6×6 jest standardem, 4×4 tylko dla twarzy i mimiki.** 4×4 to 8 bitów na piksel,
6×6 to 3,56 — **2,25× różnicy przy jakości nieodróżnialnej na elewacji czy asfalcie.**

**3.3. Bez kompresji = zakazane.** `Uncompressed` i `RGB24` nie mają wstępu do buildu Androida.

**3.4. Mipmapy zawsze.** Wyjątek tylko dla UI i ekranów startowych. Mipmapa to nie jest
oszczędność — to jest **jedyne** lekarstwo na migotanie cienkich linii z odległości.

**3.5. Bez `Read/Write Enabled`** (podwaja pamięć) i bez drugiego kompletu tych samych map
w innym folderze.

---

## 4. OBRAZ — minimum to Osiedle

Liczby wyjęte z `BraxClaudeOsiedle.Daylight()`. **Poniżej nich nie wolno zejść w żadnym rejonie.**

| Parametr | Minimum |
|---|---|
| Słońce — moc | 1.24 |
| Słońce — barwa | (1.000, 0.955, 0.875) |
| Słońce — cienie | Soft, siła ≥ 0.80 |
| Otoczenie | Trilight; niebo (.60,.68,.80), horyzont (.50,.53,.50), ziemia (.30,.28,.23) |
| Mgła | **włączona**, ExponentialSquared, barwa z horyzontu własnego nieba rejonu |
| Bliska płaszczyzna kamery | 0.25 m w otwartym terenie, 0.15 m przy ścianie |
| Przejście koloru | nasycenie 1.15, kontrast 1.06, **jedno** przejście, bez bloomu |
| Rzutowanie cieni | StableFit |
| Wygładzanie krawędzi | MSAA 2× telefon / 4× komputer, wypalone w buildzie |
| Przestrzeń barw | **Linear.** Gamma jest błędem |

**4.1. Gęstość mgły zależy od skali rejonu**, nie jest jedną liczbą. Osiedle 0.0042;
Miasto R1 0.00042, bo przy skali miasta gęstość Osiedla zjada panoramę już na 300 m.
Rejon podaje swoją gęstość w logu. Zakazane jest **wyłączenie** mgły, nie jej dobranie.

**4.2. To jest wymuszone kodem**, nie dobrą wolą: `Assets/Claude/Runtime/BraxObrazGlobalny.cs`
(po każdym wczytaniu sceny) i `Assets/Editor/Claude/BraxObrazBuild.cs` (wypalenie w buildzie).
**Obecność obu plików na gałęzi rejonu jest obowiązkowa.** 18.11 okazało się, że gałąź Osiedla
nie miała `BraxObrazBuild` w ogóle — szła bez wygładzania krawędzi i bez anizotropii,
i dlatego u właściciela migotały wszystkie pasy.

**4.3. Wyjątek jawny.** Rejon, który świadomie musi mieć inny obraz (noc, ciemne wnętrze),
wstawia do sceny `BraxObrazWyjatek` z powodem. Widać to w logu — po cichu się nie da.

**4.4. PRZESUNIĘCIE CIENIA (bias) NALEŻY DO REJONU**, standard go nie dotyka.
Poprawka błędu z 16.11: zbijanie biasu do 0,04 w każdym rejonie jest dobre tylko dla
**drgania krawędzi** cienia. Dla **acne** — migotania na całych powierzchniach — jest
odwrotnie: przesunięcie musi być większe niż texel mapy cieni rzutowany na świat, a ten
rośnie wprost proporcjonalnie do zasięgu cieni. Osiedle ma kilkadziesiąt metrów, miasto
kilkaset. Ten sam bias, który w Osiedlu wystarcza, w mieście jest kilkukrotnie za mały —
i wtedy migocze wszystko. Wartości trafiają do logu jako `bias=…/…(rejonu, nietkniete)`.

**4.5. `QualitySettings` ustawiają TRZY pliki standardu obrazu i żaden inny:**

| Plik | Kiedy | Co |
|---|---|---|
| `Assets/Claude/BraxGrafika.cs` | w czasie gry | tryby NISKA / ŚREDNIA / WYSOKA |
| `Assets/Editor/Claude/BraxObrazBuild.cs` | przy buildzie | wypalenie MSAA, anizotropii, StableFit |
| `Assets/Claude/Runtime/BraxObrazGlobalny.cs` | po wczytaniu sceny | pilnowanie, że scena nie zeszła poniżej minimum |

Żaden skrypt rejonu, żadna próba, żaden pokaz.
*(Wcześniejsza wersja tej zasady wymieniała tylko `BraxGrafika` — przez co sprawdzacz zgłaszał
cztery błędy na samym standardzie z `main`. To była pomyłka w zasadzie, nie w kodzie, i tak
właśnie ma się to rozstrzygać: punkt 0.)*

**4.6. Cienie ze słońca nigdy nie są wyłączane.** Na żadnym poziomie jakości.
Oszczędzamy na teksturach i siatkach, nie na cieniach.

**4.7. Cienkie jasne linie idą w TEKSTURĘ albo w WYCIĘCIE, nigdy jako naklejka nad powierzchnią.**
Element węższy niż 25 cm (linie podłużne, krawędziowe, przerywane, zygzaki) → w teksturę
asfaltu, bo tekstura ma mipmapy i z odległości sama się uśrednia. Szerszy (zebry, strzałki,
napisy) → wycięcie w nawierzchni, farba na lico. **Nigdy osobna płyta nad powierzchnią** —
dwie powierzchnie w jednym miejscu biją się o głębię i migoczą z odległości.
Kontrola: licznik `nakladki=N` w logu. **Wymóg: `nakladki=0`.** Inna wartość to błąd.

---

## 5. MATERIAŁY — wszystko realne

**Zasada nadrzędna właściciela: wszystko ma być z prawdziwych materiałów i tekstur.**
Źródło: Poly Haven (CC0, wolno komercyjnie, bez podawania autora) albo biblioteka właściciela.

**5.1. Każda widoczna powierzchnia ma pełny PBR** — kolor + normalne + chropowatość
(i AO, gdy jest). Ściany, sufity, posadzki, chodniki, elewacje, mury, schody, dachy, blaty.

**5.2. Materiał z samego koloru jest zakazany tam, gdzie istnieje fotoskan.** Drewno ma być
drewnem, kamień kamieniem, beton betonem.

**5.3. Czysty PBR bez map wolno tylko tam, gdzie fotoskanu nie ma:** szkło, metal gładki,
lakier samochodowy, plastik urządzeń, ekrany, lampy, lustra.

**5.4. Rzutowanie w metrach świata, nie „na obiekt".** Cegła ~0,25 m, deska ~0,15 m,
płyta chodnikowa ~0,5 m, tynk 1,5–2 m. Powtarzalność łamana szumem 5–10 %.

**5.5. Czego nie umiemy zrobić realistycznie — zostaje puste** i idzie na listę braków.
Nigdy klocek w kolorze jako zastępstwo.

**5.6. Materiał robiony W GRZE musi mieć z czego powstać.** Obiekt tworzony w czasie gry
(`CreatePrimitive`) kopiuje wbudowany materiał domyślny, którego wariant shadera nie musi
trafić do APK — i wtedy jest **różowy**. 18.11 tak wyglądały wszystkie piłki na boiskach.
Lekarstwo: materiał zapisany jako zasób przy buildzie i podpięty do sceny
(`PlayableGenerated/StandardRuntime.mat`), kopiowany w grze. **Nie** „zawsze dołączaj shader
Standard" — to pakuje wszystkie jego warianty, dziesiątki MB.

**5.7. Woda ma płynąć.** Nieruchoma tekstura wody jest błędem, nie uproszczeniem.
Minimum: kilka warstw przewijanych z różną prędkością, jaśniejsze krawędzie strug,
pierścień piany w miejscu uderzenia, garść kropel. Przewijanie tekstury to jedna linijka
w shaderze i na telefonie jest praktycznie darmowe.

**5.8. LISTA WŁAŚCICIELA — Z NIEJ BIERZE SIĘ NAJPIERW.**
Właściciel 23.11, 01:57: *„wybrane tekstury, które mi się podobają z Poly Haven, z których
przede wszystkim trzeba korzystać"*. Lista: `MATERIALY-WLASCICIELA.md` w korzeniu `main`
(79 pozycji: 72 tekstury, 7 modeli; elewacje i mury, podłogi, drewno, kostki i chodniki,
dachy, tkaniny, skały, zieleń), wersja dla narzędzi: `unity-tools/claude/materialy_wlasciciela.json`.

- Wykańczając powierzchnię, wykonawca **najpierw** szuka na liście.
- Spoza listy bierze dopiero wtedy, gdy lista nie ma nic na daną powierzchnię — i pisze
  w opisie bolidu, czego zabrakło i co wziął. Listę rozszerza właściciel, nie wykonawca.
- Lista nie zwalnia z niczego: klasa i rozmiar z rozdziału 3, rzutowanie z 5.4, a modele
  zieleni wchodzą do gry dopiero odchudzone do budżetu 10.2 i z LOD (10.5).

**5.9. ŹRÓDŁO ZAWSZE 4K. TELEFON DOSTAJE POMNIEJSZONE I SKOMPRESOWANE.**
Właściciel 23.11, 02:03: *„Zawsze pobieramy tekstury w 4K (…) żebyśmy potem mogli grę
podkręcić na komputer"*.

- **Pobiera się 4K i to jest jedyny komplet plików.** Nie ma osobnych plików „na telefon"
  (3.5: zakaz drugiego kompletu tych samych map).
- **Rozmiar na telefon ustawia import, nie pobieranie:** nadpisanie dla Androida według
  rozdziału 3 — 2048 w zasięgu ręki, **1024 elewacje, grunt, drogi**, 512 z daleka,
  ASTC 6×6. Telefon to więc nie „wszędzie 2K": 2K jest sufitem (3.1), a elewacja i chodnik
  idą w 1024. Przy kaflu 2 m to 512 pikseli na metr — tyle samo, co 2K na ścianie 4 m.
- **Komputer weźmie pełne źródło** z tych samych plików — bez pobierania i bez przerabiania
  materiałów drugi raz. Wersji na komputer jeszcze nie ma, więc dziś limit importu to 2048
  wszędzie i sprawdzacz (T1) tego pilnuje; przy wersji na komputer podnosi się jedną nastawę
  importu dla komputera, Android zostaje bez zmian.
- APK od tego nie rośnie: do paczki trafia rozmiar po imporcie, nie źródło.
- **Stan 23.11:** `fetch_polyhaven.py` na gałęziach pobiera tekstury w 2K, a modele z 1K.
  To jest do zmiany w rejonach. Koszt zmierzony na listach plików Poly Haven: 72 tekstury
  z listy to ok. 2,6 GB w 4K wobec ok. 0,7 GB w 2K. **Niezmierzone:** czas pierwszego buildu
  i miejsce w pamięci podręcznej CI po tej zmianie.

**5.10. BUDYNEK SIĘ WYKAŃCZA. ROBOTY NIE ZOSTAWIA SIĘ W POŁOWIE.**
Właściciel 23.11, 01:57: *„Wy macie wykańczać budynki, nie wykańczacie"*; 02:00: *„mamy dużo
rzeczy niepowykańczanych, jest zostawione w połowie roboty"*.

- Budynek albo wnętrze jest **wykończone**, gdy każda powierzchnia, którą gracz może
  zobaczyć, ma materiał według 5.1: elewacja z każdej widocznej strony, cokół, dach,
  ściany w środku, sufit, podłoga, schody, kostka albo chodnik przy wejściu.
- Powierzchnia w samym kolorze tam, gdzie istnieje fotoskan, to **niewykończone** (5.2),
  nie „uproszczone".
- Czego wykonawca nie skończył, wypisuje w opisie bolidu jako **listę niewykończonych**
  z miejscem — obok listy braków z 5.5. Niewykończone przemilczane = bolid nieodebrany.
- Sprawdzacz wypisuje materiały z samego koloru nazwane jak drewno, kamień, beton, cegła,
  tynk, tkanina (reguła M2), a skrypty pobierające 2K i 1K — reguła M3. To są uwagi, nie
  błędy: sprawdzacz czyta nazwy, więc pokazuje, co obejrzeć, i nie wyrokuje.
  Stan 23.11: Osiedle 45, Miasto R1 50, Miasto Mistrzów 56, pokój prób 44.

---

## 6. POSTACIE

**6.0. JEDEN POTOK OBSADY DLA CZTERECH REJONÓW. Rejon nie ma własnego.**
Właściciel 18.11, 10:54: „W czterech rejonach mają być takie same postacie jakościowo."

Potok leży na `main` i **tylko tam**:

| Gdzie | Co robi |
|---|---|
| `unity-tools/claude/pobierz_obsade.py` | pobiera paczkę, sprawdza nazwy i sumy kontrolne |
| `unity-tools/claude/osiedle_ruch.py` | rig, ujęcia ruchu, stopy, ramiona |
| `unity-tools/claude/osiedle_twarz.py` | atlas mimiki, usta, powieki, zęby |
| `unity-tools/claude/osiedle_mowa.py` | ścieżki ruchu ust z nagrań |
| `Assets/Editor/Claude/BraxObsadaImport.cs` | import: sufit, ASTC, redukcja klatek |
| `Assets/Editor/Claude/BraxClaudeOsiedleObsada.cs` | budowa postaci, materiał, mimika, skala |
| `Assets/Claude/BraxObsadaOsiedla.shader` | shader obsady |

**Rejonowi nie wolno trzymać własnej kopii żadnego z tych plików.** Nazwy zaczynają się od
`osiedle_`, bo tam powstały — to nazwa historyczna, nie przypisanie do rejonu.

**6.1. KAŻDA POSTAĆ W GRZE WYGLĄDA TAK SAMO.** Właściciel 18.11, 16:51: *„Każda postać
w grze od dzisiaj, każda jedna ma wyglądać tak, jak w zaakceptowanej wersji. Te ustawienia,
żadnych błysków świecących, nic."*

| | połysk | metaliczność | faktura (mapa normalnych) |
|---|---|---|---|
| **STANDARD GRY** | **0,00** | **0** | **×1,3** |

To jest ustawienie „6 bez odblasku" z pokoju prób, wybrane przez właściciela z sześciu
podejść obejrzanych obok siebie na żywych postaciach. **Nie otwieramy tego bez właściciela.**

**6.2. Standard musi mieć na czym stanąć, i build ma to UDOWODNIĆ.**
18.11 gałąź Osiedla miała starą kopię `BraxObsadaOsiedla.shader` — bez `_Gladkosc`,
`_MetalMnoznik` i `_SilaNormalnych`, czytającą połysk i metal wprost z mapy.
`BraxPostacGlobalna` szukało tych nazw, nie znajdowało ich, **nie robiło nic** — i meldowało
w logu, że standard nałożono na 19 postaci. Build wyglądałby jak przed poprawką, a log
i wykonawca zapewnialiby, że to wariant 6.

Dwie rzeczy obowiązkowe:
1. **Reguła P1 sprawdzacza** — `BraxObsadaOsiedla.shader` musi mieć te trzy pokrętła
   **użyte w liczeniu światła**, nie tylko zadeklarowane. Brak = błąd, gałąź nie do odbioru.
   Rejon z własnym shaderem postaci dokłada w nim te same trzy pokrętła.
2. **Log `BRAX_POSTAC` kończy się liczbą materiałów BEZ standardu** i nazwami ich shaderów.
   Niezerowa lista to **błąd, nie uwaga** — znaczy, że jakaś postać wygląda inaczej.

Objęte shadery (18.11): `Braxhorn/Obsada Osiedla`, `Braxhorn/HeroPremium` (tam matowość
nazywa się szorstkością i działa odwrotnie — podłoga szorstkości idzie w górę do pełnej),
`Braxhorn/LudzieUbranie`, `Braxhorn/LudzieWlosy`, `Standard`.

**6.3. Wagi skinningu: cztery kości na wierzchołek** (`SkinQuality.Bone4`).

**6.4. Mimika:** atlas 512 na poziom (plik 2048×1024). Niżej zęby mają dwa piksele i znikają.

**6.5. Wzrost postaci jest liczbą w kodzie, nie cechą modelu.** Zmierzone z FBX: wszystkie
modele obsady mają siatkę tej samej wysokości (patryk 98,26 jedn., monika 98,54). O wyglądzie
decyduje wyłącznie wpisana liczba. Mnożnik świata K = 1,294; bohater 1,80 m = 2,33 jednostki.
Właściciel 18.11: kobiety **trochę** niższe od mężczyzn — kobiety 1,70–1,74,
mężczyźni 1,77–1,81. Nie wpisujemy całym grupom jednej liczby: pięć kobiet z identycznym
1,68 wyglądało jak odbite z formy.

**6.6. Nikt nie dobiera postaciom ruchów bez właściciela.** Właściciel 18.11:
„weź ty nie rób żadnych animacji". Import wolno zmieniać (redukcja klatek, kompresja),
bo to zapis tych samych krzywych — dobierania, retargetowania i przerabiania ujęć nie.

---

## 7. GŁOS I TEKSTY

**7.1. Głosy własne (Patryk, Monika) to nagrania.** Nie ma syntezy ich głosu i nie będzie.

**7.2. Głosy tła to generator** (Piper, offline, polskie głosy). Tekst zmienia się **bez
nagrywania**.

**7.3. Nazwy plików niosą znaczenie:** `_m` / `_f` to płeć, `<postać>_` to głos własny tej
postaci.

**7.4. Nazwy produktów, marek i leków — nigdy prawdziwe.** To cudze znaki towarowe.
Opisowo, po funkcji: „na grypę", „na gardło", „syrop na kaszel".

---

## 8. BUILD I ODBIÓR

**8.1. APK uruchamia WYŁĄCZNIE właściciel**, pojedynczo, przez `workflow_dispatch`.
Commit, push ani zapis instrukcji **nie są zgodą na build**.

**8.2. Nic nie jest zrobione bez pomiaru.** „Wykonawca obiecał" nie jest dowodem.
Dowód to liczba z logu, zrzut z gry albo pomiar z pliku.

**8.3. Każda zmiana grafiki: zdjęcie przed i po, w tym samym ujęciu.**

**8.4. Klucz pamięci podręcznej liczy się z CAŁEGO potoku**, nie z ręcznie wypisanej listy
plików. Inaczej build bierze stare postacie z pamięci i wygląda na to, że poprawka nie działa.
Ten sam powód ma `GetVersion()` w importerze obsady: **każda zmiana ustawień importu =
podniesienie numeru.**

**8.5. Raport wagi z porównaniem.** Każdy build porównany z poprzednim; wzrost musi mieć
wyjaśnienie. Waga APK Osiedla: 735 MB (4–10.11, działało) → 1465 MB (15.11) → 1877 MB (16.11,
wyrzucało z gry). Taki skok musi być widoczny od razu, a nie po tygodniu.

**8.6. Nic się nie usuwa** bez wcześniejszej listy i zgody właściciela. Odseparować — tak.

**8.7. Rejony się nie mieszają.** Poprawka w jednym rejonie nie dotyka plików drugiego.

**8.8. Z KAŻDYM BOLIDEM IDĄ ZDJĘCIA Z GRY — dla wykonawców, na poprawki.**
Właściciel 23.11, 01:28: *„Z każdym bolidem mają być zdjęcia. Dla wykonawców. Na poprawki."*
To zastępuje decyzję z 3.11 („bez zdjęć CI — to tylko czas zajmuje"). Zdjęcia wracają, ale
**nie wstrzymują APK**: robi je osobny przebieg, równolegle albo po buildzie.

- **Stała lista ujęć rejonu.** Te same nazwy i te same miejsca w każdym bolidzie, żeby dwa
  bolidy dało się położyć obok siebie — to jest od razu „przed i po" z 8.3. Nowa dzielnica
  albo nowy obiekt = nowe ujęcia dopisane na końcu. Starych się nie usuwa.
- **Co musi być na liście:** każde miejsce zmienione w tym bolidzie, najcięższe miejsce
  rejonu, jedna panorama z daleka, jedno ujęcie z poziomu gracza z kamerą z 10.8 i każde
  wnętrze, do którego da się wejść.
- **Z prawdziwej gry, nie z edytora:** skompilowany player, 1280×576 albo więcej,
  w proporcjach telefonu, bez HUD i bez licznika (`BraxPlynnosc.Zdjecia = true`).
- **Przy każdym zdjęciu liczby kadru.** Obok zdjęć plik `statystyki.txt`: nazwa ujęcia,
  paczki, przejścia, trójkąty, obiekty rzucające cień. Rejon Mistrzów już to ma
  (`BraxChampionsVisualReview`) — ten format jest wzorem. Ujęcie ponad budżet z 10.2 jest
  „do poprawki" z urzędu, bez dyskusji o tym, jak wygląda.
- **Strona przeglądu.** Przy każdym zdjęciu werdykt **OK** albo **do poprawki** z powodem,
  na górze lista poprawek na następny bolid. Wzór: przegląd „Osiedle R2 build 4" z 31.10.
  Werdykt pisze wykonawca rejonu i pisze wprost, co nie wyszło. Zdjęcie z podpisem „OK",
  na którym widać błąd, jest gorsze niż brak zdjęcia.
- **Bolid bez zdjęć jest nieodebrany** — tak samo jak gałąź z błędem sprawdzacza.

---

## 9. CO JEST DO DECYZJI WŁAŚCICIELA

Świadomie **nierozstrzygnięte** — nie wolno ich rozstrzygać samemu:

1. **Pełne tekstury na telefonie.** `BraxGrafika.Mip()` wymusza połowę rozdzielczości
   w każdym trybie. To obejście awarii pamięci z 31.10, nie decyzja artystyczna.
   **Zdjęcie go to największy pojedynczy zapas jakości, jaki został** — ale wymaga, żeby
   pamięć zeszła poniżej granicy telefonu i pomiaru na jego sprzęcie.
2. **Cienie w trybie ŚREDNIM.** Dziś twarde, jedna kaskada na 35 m — krawędź skacze.
   Propozycja: miękkie, 2 kaskady, 45 m.
3. **MSAA 4× zamiast 2×** — dopiero gdy APK z 2× okaże się stabilny na jego telefonie.
4. **Liczby wspólnego pliku trybów jakości (10.3) i zasięgów według wielkości (10.4).**
   Dziś są trzy wersje `BraxGrafika.cs` (różnią się zasięgiem, cieniami i progiem
   szczegółów) i dwie wersje `BraxZasiegDrobnych.cs`. Jedna wersja wymaga wyboru tych
   liczb, a wybór wymaga pomiaru licznikiem na telefonie w każdym rejonie. Do tego czasu
   liczby są zamrożone — żaden rejon ich nie zmienia.

---

## 10. PŁYNNOŚĆ I KAMERA

Właściciel 23.11, 01:13: *„Mamy ustalić jeden konkretny zespół konkretnych zasad. Jak ma to
wyglądać, że ta gra […] nie zamulała. Minimum 30 FPS, minimum."*
Właściciel 23.11, 01:27: *„Pod każdy rejon jedne wspólne zasady, żebyśmy utrzymali tą jakość
minimum co jest, albo jeszcze lepsza. Wszystko płynnie chodziło i kamera w każdym kolejnym
buildzie już ma być bliżej postaci. Tak jak w GTA najnowszym."*

**Skąd ten rozdział.** Do 23.11 ten plik ustalał, jak gra ma WYGLĄDAĆ (rozdziały 3–6),
i nie miał ani jednej liczby o tym, jak ma CHODZIĆ. Liczby istniały — w `STANDARD-TECHNICZNY`
(1.11) i `ZASADA-0` (16.11) — ale nie trafiły tutaj i nic ich nie mierzyło. Stan zmierzony
23.11 na gałęziach rejonów:

- `BraxGrafika.cs` ma **trzy różne wersje**. W trybie ŚREDNIM Miasto R1 rysuje wszystko do
  1600 m, Osiedle i Mistrzowie do 420 m (tabela w 10.3).
- Zasięg zależny od wielkości obiektu jest w **dwóch połówkach i żaden rejon nie ma obu**:
  Osiedle ma go dla obiektów z poziomami szczegółów, Mistrzowie dla zwykłych obiektów,
  Miasto R1 nie ma żadnej (10.4).
- W kadrze mierzono 4,5 mln trójkątów (park Osiedla, 31.10) i 8,3 mln (galeria G1
  w Mistrzach, 16.11) przy limicie 0,8 mln, którego żaden build nie sprawdzał.

To nie są różne potrzeby rejonów. To ten sam rozjazd co przy teksturach, tylko w miejscu,
którego nikt nie pilnował.

**10.1. MINIMUM 30 KLATEK NA SEKUNDĘ.**
Telefon odniesienia z rozdziału 1 (moto g86), tryb ŚREDNI. Niżej nie wolno zejść w żadnym
rejonie i w żadnym miejscu rejonu. 60 klatek w trybie WYSOKIM na mocniejszym telefonie jest
celem; 30 jest podłogą.

- **Limit klatek nigdy nie jest ustawiany poniżej 30** (reguła F1 sprawdzacza).
- **Liczy się najgorsza sekunda, nie średnia.** Jedno szarpnięcie przy doczytaniu obszaru
  nie jest spadkiem klatek, ale ma być widać je w logu.
- **Dowód:** licznik w grze i wiersze `BRAX_PLYNNOSC` w logu (`BraxPlynnosc`). Pomiar to
  dwie minuty chodu i jazdy przez najcięższe miejsce rejonu. Licznik jest na telefonie
  domyślnie widoczny; cztery palce przytrzymane na ekranie przez sekundę chowają go
  i przywracają.

**10.2. BUDŻET KADRU.**

| Co | Limit |
|---|---|
| Jedna klatka, razem z przebiegami cieni | **≤ 800 tys. trójkątów, ≤ 400 wywołań rysowania** |
| Scena-obszar | ≤ 1,5 mln trójkątów |
| Rekwizyt / mebel / drzewo / budynek | ≤ 5 tys. / 15 tys. / 20 tys. / 60 tys. trójkątów |
| Jedna siatka | ≤ 65 000 wierzchołków |

Liczby są z `STANDARD-TECHNICZNY` z 1.11 — nie są nowe. Nowe jest to, że są mierzone:
w grze licznik robi się czerwony, a przy każdym bolidzie liczby kadru stoją obok zdjęć
(8.8). **Ujęcie ponad budżet = bolid nieodebrany.**

**10.3. JEDEN PLIK TRYBÓW JAKOŚCI.** `Assets/Claude/BraxGrafika.cs` ma być ten sam
w każdym rejonie — tak jak potok obsady (6.0), rejon nie trzyma własnej wersji.
Stan 23.11, tryb ŚREDNI na telefonie:

| | Osiedle, Mistrzowie, pokój prób | Miasto R1 |
|---|---|---|
| Zasięg kamery | 420 m | 1600 m |
| Cienie | twarde, 1 kaskada, 35 m | miękkie, 2 kaskady, 80 m |
| Próg szczegółów (lodBias) | 0,8 | 1,25 |
| Tryb NISKI — cienie | **wyłączone** (wbrew 4.6) | twarde, 40 m |

Wspólna wersja trafi na `main` po pomiarze licznikiem w każdym rejonie, bo wybór tych liczb
należy do właściciela (9.4). **Do tego czasu liczby są zamrożone:** żaden rejon nie zmienia
swojego `BraxGrafika.cs`. Sprawdzacz wypisuje wersję pliku i jego zasięgi przy każdym
przebiegu (G1), żeby rozjazd było widać, a nie trzeba go było szukać.

**10.4. ZASIĘG WEDŁUG WIELKOŚCI, NIE JEDNA LICZBA.**
Jedna liczba dla wszystkiego daje wybór między dwiema złymi rzeczami: albo ławka rysuje się
z kilometra, albo budynek znika razem z ławką. Dlatego zasięg zależy od wielkości obiektu.
Są dwa mechanizmy, bo są dwa rodzaje obiektów. **Oba mają być w każdym rejonie.**

**(a) SUFIT — żeby drobiazg nie rysował się z kilometra.** Zwykły obiekt, bez poziomów
szczegółów:

| Obiekt | Warstwa | Rysowany najwyżej do |
|---|---|---|
| DROBNY — największy wymiar poniżej 1,2 m (1,55 j.) | 21 | 90 j. (ok. 70 m) |
| MAŁY — poniżej 4 m (5,18 j.) | 20 | 260 j. (ok. 200 m) |
| DUŻY — budynki, grunt, drzewa, teren | bez zmian | dalekiego zasięgu rejonu |

Przy tych liczbach obiekt znika, gdy ma na ekranie mniej niż 2 % wysokości. To jest metoda
rejonu Mistrzów (`BraxChampionsWydajnosc`), wyjęta do jednego miejsca. Sortowanie przy
buildzie: `BraxZasiegSortowanie.Posortuj(scena)`. Zasięgi w grze ustawia `BraxPlynnosc`.
**Rejon nie wpisuje własnych warstw ani własnych zasięgów.**

**(b) PODŁOGA — żeby nic nie wyskakiwało tuż przy postaci.** Właściciel 1.11, 20:49:
*„kwiaty, drobne rzeczy pojawiają się dopiero tuż przy postaci"*. Obiekt z poziomami
szczegółów (LODGroup) nie znika bliżej niż:

| Największy wymiar | poniżej 0,8 j. | do 2,5 j. | do 6 j. | do 16 j. | większy |
|---|---|---|---|---|---|
| Widoczny co najmniej do (tryb ŚREDNI) | 55 j. | 80 j. | 120 j. | 220 j. | 300 j. |

Tryb NISKI × 0,75, WYSOKI × 1,5. Pilnuje tego `Assets/Claude/Runtime/BraxZasiegDrobnych.cs`
— plik z Osiedla, który przelicza ostatni próg każdej grupy tak, żeby zasięg nie zależał
od trybu jakości. Sufit nie leży poniżej podłogi: 90 ≥ 80 i 260 ≥ 120.

**Stan 23.11:** (a) działa tylko w Mistrzach. (b) dodaje tylko skrypt sceny Osiedla
(`BraxClaudeOsiedleKraniec`), a sam plik ma dwie wersje — nowsza, z Osiedla, nie nakłada
na siebie dwóch poziomów, gdy shader nie umie ich przenikać. Miasto R1 nie ma ani (a),
ani (b) w swojej scenie. Liczby obu mechanizmów są zamrożone do pomiaru (9.4); włączenie
(b) w nowym rejonie wydłuża rysowanie drobiazgów, więc idzie razem z licznikiem i ze
zdjęciami, nie na ślepo.

- **Siatki scalone dzieli się na wielkości PRZED scaleniem.** Tysiąc drobiazgów scalonych
  z budynkiem ma wymiar budynku i rysuje się w całości z każdej odległości. Tak było
  w galerii G1: 8,3 mln trójkątów, z czego 7,5 mln w kawałkach poniżej 1,2 m.
- **Daleki zasięg zależy od skali rejonu** — tak jak gęstość mgły (4.1). Rejon podaje go
  w logu. Mgła ma zakrywać granicę zasięgu; tam, gdzie obiekty wyskakują z niczego,
  mgła jest za rzadka albo zasięg za krótki.
- **Z daleka nic dużego nie znika.** Budynek poza wczytanym obszarem zostaje jako uproszczona
  kopia w tych samych materiałach. Nigdy szare pudło.

**10.5. POZIOMY SZCZEGÓŁÓW.** Każdy obiekt powyżej 3 tys. trójkątów ma trzy: pełny do 25 m,
połowa trójkątów do 80 m, jedna piąta do 200 m, dalej nie jest rysowany. Drzewo na ostatnim
poziomie jest płaską planszą z przezroczystością mieszaną, nie wycinaną (2.2).

**10.6. WNĘTRZA NIE RYSUJĄ SIĘ Z ULICY.** Wnętrze, którego nie widać przez szybę, nie jest
rysowane, dopóki gracz nie jest przy drzwiach. Ten sam mechanizm i te same odległości co
obszary: wczytanie 45 m przed wejściem, zwolnienie 110 m za nim. Witryna, przez którą widać
środek, pokazuje tylko to, co stoi przy szybie.

**10.7. BEZPIECZNIK.** Gdy średnia z trzech sekund spada poniżej 30 klatek, gra sama
zdejmuje ciężar — w tej kolejności i ani kroku dalej:

1. zasięg warstw DROBNE i MAŁE × 0,75;
2. próg szczegółów (lodBias) − 20 % — wykonuje `BraxGrafika`, bo tylko on rusza
   `QualitySettings` (4.5);
3. rozdzielczość — **wyłącznie przy wczytaniu sceny**, nigdy w trakcie gry.

Bezpiecznik nie wyłącza cieni (4.6) i nie rusza MSAA ani rozdzielczości w locie: to odtwarza
bufor ekranu i na Vulkanie zamykało aplikację (2.4). **Kod bezpiecznika powstaje po pierwszym
pomiarze na telefonie** — pisany bez liczb robiłby migające drobiazgi zamiast płynności.

**10.8. JEDNA KAMERA, BLIŻEJ POSTACI.**

| | Było do 23.11 | Jest |
|---|---|---|
| Pieszo, za postacią | 5,8 j. (ok. 4,5 m) | **4,2 j. (ok. 3,2 m)** |
| Za autem, postój → pełna prędkość | 7 → 9,5 j. | **5,5 → 7,5 j.** |
| Postać na ekranie (pole widzenia 55°) | nieco ponad ⅓ wysokości | około połowy |

- Liczby są w `Assets/Claude/BraxKameraStandard.cs` **i tylko tam**. `BraxGymPlayer.cs`
  każdego rejonu czyta je stamtąd: `BraxKameraStandard.Pieszo` na starcie i przy powrocie
  do ustawień, `BraxKameraStandard.Auto(prędkość)` za autem. Odległość wpisana liczbą
  w pliku rejonu jest błędem (reguła K1).
- „Jeszcze bliżej" albo „trochę dalej" to zmiana jednej liczby na `main`, na słowo
  właściciela, ze zdjęciem przed i po z poziomu gracza. Nie cztery poprawki w czterech
  gałęziach.
- Bez zmian zostają: kąt startowy, wysokość celu 1,65, kolizja kamery ze ścianami
  i chowanie postaci, gdy ściana dosuwa kamerę bliżej niż 1,1.
- **Bliższa kamera poprawia wygląd, nie klatki.** Telefon i tak rysuje wszystko przed
  kamerą aż do granicy zasięgu. Klatki dają 10.2 i 10.4.

**10.9. PLIKI STANDARDU PŁYNNOŚCI.** Obecność wszystkich trzech na gałęzi rejonu jest
obowiązkowa, tak jak plików obrazu (4.2).

| Plik | Co robi |
|---|---|
| `Assets/Claude/BraxKameraStandard.cs` | liczby kamery (10.8) |
| `Assets/Claude/Runtime/BraxPlynnosc.cs` | liczby budżetu i zasięgów, licznik w grze, log `BRAX_PLYNNOSC`, zasięgi warstw 20 i 21 |
| `Assets/Editor/Claude/BraxZasiegSortowanie.cs` | sortowanie obiektów na warstwy zasięgu przy buildzie, log `BRAX_ZASIEG_WARSTWY` |

Co czym jest pilnowane:

| Zasada | Sprawdzacz (od 23.11) | Licznik w grze | Zdjęcia z bolidu (8.8) |
|---|---|---|---|
| 10.1 klatki | F1: limit klatek poniżej 30 | tak | — |
| 10.2 budżet kadru | — | tak | tak |
| 10.3 jeden plik trybów | G1: wypisuje wersję i zasięgi | — | — |
| 10.4 (a) sufit drobiazgów | W1: rejon nie sortuje obiektów; W2: własne liczby | — | panorama z daleka |
| 10.4 (b) podłoga drobiazgów | G1: wypisuje wersję `BraxZasiegDrobnych` | — | ujęcie z poziomu gracza |
| 10.8 kamera | K1: odległość wpisana liczbą | — | ujęcie z poziomu gracza |
| 10.9 pliki standardu | W0: brak pliku | — | — |

10.5, 10.6 i 10.7 nie mają jeszcze automatu. Do czasu, aż go dostaną, dowodem jest
`statystyki.txt` z ujęć przed budynkiem i w parku.

**10.10. KOLEJNOŚĆ.** Najpierw pomiar, potem cięcie — nie odwrotnie.

1. Każdy rejon bierze `main`, przechodzi sprawdzacz (K1, W0, W1, F1) i wypuszcza bolid
   z licznikiem i zdjęciami.
2. Właściciel przechodzi każdy rejon z licznikiem na ekranie. Czerwone miejsca trafiają do
   logu z pozycją kamery.
3. Z tych liczb powstaje wspólny `BraxGrafika.cs` (10.3, 9.4) i bezpiecznik (10.7).
4. Rejon ponad budżetem tnie to, co pokazał pomiar — nie to, co się komuś wydaje.

---

*Zmiany w tym pliku — tylko za zgodą właściciela. Ostatnia aktualizacja: 23.11.2026.*
