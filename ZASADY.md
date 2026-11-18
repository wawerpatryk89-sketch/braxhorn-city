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

---

*Zmiany w tym pliku — tylko za zgodą właściciela. Ostatnia aktualizacja: 18.11.2026.*
