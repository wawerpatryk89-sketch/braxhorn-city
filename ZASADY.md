# ZASADY — BRAXHORN CITY

**To jest jedyny obowiązujący zbiór zasad.** Obowiązuje każdy rejon i każdego wykonawcę:
Osiedle, Miasto R1, Miasto Mistrzów, pokój prób, pojazdy i każdy następny rejon.

**Gdy cokolwiek innego mówi inaczej — wygrywa ten plik.** Dotyczy to komentarzy w kodzie,
notatek w projekcie i wszystkich wcześniejszych dokumentów z zasadami, w szczególności:
`ZASADA-0-JEDEN-STANDARD-OBRAZU-16-11`, `ZASADA-OBRAZ-MINIMUM-OSIEDLE-16-11`,
`ZASADA-KOLORY`, `STANDARD-TECHNICZNY`, `ZASADA-NADRZEDNA-WSZYSTKO-REALNE-16-11`,
`ZASADA-MIGOTANIE-PASOW-I-CIENKICH-LINII-16-11`, `ZASADA-0-JEDEN-STANDARD-OBRAZU`,
`ZASADA-ZERO-KLOCKOW`. Tamte zostają jako uzasadnienia i pomiary — **nie jako źródło decyzji**.

Zasada jest jednostronna: **minimum wolno przekraczać w górę, nigdy w dół.**

---

## 0. DLACZEGO TEN PLIK ISTNIEJE

Zasady były dokumentami w projekcie. Każdy wykonawca czytał inny, bo każdy miał własny
`CLAUDE.md` na własnej gałęzi, a dokumentów z zasadami narosło osiem i część z nich mówiła
co innego. Jeden z nich powoływał się nawet na sprawdzacz `sprawdz_zasade_0.py` jako
„jest, działa" — a tego pliku **nigdy nie było w repozytorium**.

Dlatego teraz: **jeden plik, w korzeniu `main`, plus sprawdzacz, który naprawdę istnieje.**

    python3 -I unity-tools/claude/sprawdz_zasady.py .

Kod wyjścia 1 = są błędy. Gałąź z błędami nie idzie do odbioru.

---

## 1. SPRZĘT I CEL

Gra ma chodzić na telefonie, na baterii, bez przegrzewania. Telefon odniesienia:
**moto g86 5G, 8 GB RAM**. Zmierzona granica: scena z 1,5 GB danych działa, 1,7 GB zamyka grę.

To nie jest konsola. Nie porównujemy się do gier, które chodzą z wtyczką w gniazdku —
nadrabiamy światłem, materiałami i ruchem, bo to widać najbardziej.

---

## 2. TEKSTURY — sufit 2048, format ASTC 6×6

**To była największa rozjazdowa.** Jeden rejon ciągnął 4K, drugi 2K, trzeci bez kompresji.
Pięć limitów rozdzielczości i pięć formatów w jednym APK.

| Klasa | Co to jest | Limit | Format Android |
|---|---|---|---|
| `TwarzBohatera` | Patryk, Monika, przeciwnicy w ringu — twarz z 0,5 m | 2048 | ASTC 4×4 |
| `Postac` | reszta obsady, ciała bohaterów | 2048 | ASTC 6×6 |
| `Mimika` | atlas mimiki (8 poziomów ułożonych 4×2) | 2048 | ASTC 4×4 |
| `ZBliska` | murale, szyldy, ekrany, wnętrza w zasięgu ręki | 2048 | ASTC 6×6 |
| `Swiat` | elewacje, grunt, drogi, modele z Poly Haven, wnętrza ogólne | 1024 | ASTC 6×6 |
| `Daleko` | kopie z daleka, niebo, tło | 512 | ASTC 6×6 |

**2.1. Sufit 2048. 4096 tylko z pisemnym wyjątkiem właściciela** w treści commita.
Dlaczego: 4K to czterokrotność pamięci wobec 2K, a telefon tego nie pokaże. Ściana 4 m
pokryta teksturą 2K to 512 pikseli na metr; z dwóch metrów oko i tak nie rozróżnia więcej.
Dziś zatwierdzony jest jeden wyjątek: mural 19 na siłowni.

**2.2. ASTC 6×6 jest standardem, 4×4 tylko dla twarzy i mimiki.** 4×4 to 8 bitów na piksel,
6×6 to 3,56 — **2,25× różnicy przy jakości nieodróżnialnej na elewacji czy asfalcie**.
Sama obsada Osiedla: 304 MB w 4×4, 135 MB w 6×6.

**2.3. Bez kompresji = zakazane.** `Uncompressed` i `RGB24` nie mają wstępu do buildu Androida.

**2.4. Mipmapy i streaming zawsze.** Wyjątek tylko dla UI i ekranów startowych.

**2.5. Bez `Read/Write Enabled`** i bez drugiego kompletu tych samych map w innym folderze.

---

## 3. OBRAZ — minimum to Osiedle

Liczby wyjęte z `BraxClaudeOsiedle.Daylight()`. **Poniżej nich nie wolno zejść w żadnym rejonie.**

| Parametr | Minimum |
|---|---|
| Słońce — moc | 1.24 |
| Słońce — barwa | (1.000, 0.955, 0.875) |
| Słońce — cienie | Soft, siła ≥ 0.80 |
| Słońce — przesunięcie cienia | bias ≤ 0.04, normalBias ≤ 0.25 |
| Otoczenie | Trilight; niebo (.60,.68,.80), horyzont (.50,.53,.50), ziemia (.30,.28,.23) |
| Mgła | **włączona**, ExponentialSquared, barwa z horyzontu **własnego** nieba rejonu |
| Bliska płaszczyzna kamery | 0.25 m w otwartym terenie, 0.15 m przy ścianie |
| Przejście koloru | nasycenie 1.15, kontrast 1.06, jedno przejście, **bez bloomu** |
| Rzutowanie cieni | StableFit |
| Wygładzanie krawędzi | MSAA 2× telefon / 4× komputer, wypalone w buildzie |
| Przestrzeń barw | **Linear.** Gamma jest błędem |

**3.1. Gęstość mgły zależy od skali rejonu, nie jest jedną liczbą.** Osiedle 0.0042;
Miasto R1 0.00042, bo przy skali miasta gęstość Osiedla zjada panoramę już na 300 m.
Rejon podaje swoją gęstość w logu buildu. Zakazane jest **wyłączenie** mgły, nie jej dobranie.

**3.2. To jest wymuszone kodem**, nie dobrą wolą: `Assets/Claude/Runtime/BraxObrazGlobalny.cs`
(po każdym wczytaniu sceny) i `Assets/Editor/Claude/BraxObrazBuild.cs` (wypalenie w buildzie).
Wystarczy mieć `main`.

**3.3. Wyjątek jawny.** Rejon, który świadomie musi mieć inny obraz (noc, ciemne wnętrze),
wstawia do sceny `BraxObrazWyjatek` z powodem. Widać to w logu — po cichu się nie da.

**3.4a. PRZESUNIĘCIE CIENIA (bias) NALEŻY DO REJONU, nie do standardu.**
Poprawka mojego błędu z 16.11: zbijałem bias do 0,04 i normalBias do 0,25 w każdym rejonie,
bo w Mieście były niższe i wyszedłem z założenia, że niżej = lepiej. To prawda tylko dla
**drgania krawędzi** cienia. Dla **acne** — tego stopniowanego migotania na całych
powierzchniach — jest odwrotnie: przesunięcie musi być większe niż texel mapy cieni
rzutowany na świat, a ten rośnie wprost proporcjonalnie do zasięgu cieni. Osiedle ma
kilkadziesiąt metrów, miasto kilkaset. Ten sam bias, który w Osiedlu wystarcza, w mieście
jest kilkukrotnie za mały — i wtedy **migocze wszystko**. R1 miał normalBias 0,35 i działało;
standard zbijał mu go do 0,25.

Od teraz: rejon ustawia bias u siebie, standard go **nie dotyka**, a wartości trafiają do
logu `BRAX_OBRAZ` jako `bias=…/…(rejonu, nietkniete)`.

**3.4. `QualitySettings` ustawia WYŁĄCZNIE `BraxGrafika.cs`.** Żaden skrypt rejonu, żadna
próba, żaden pokaz. Dziś łamie to 47 miejsc — do posprzątania.

**3.5. Cienie ze słońca nigdy nie są wyłączane.** Na żadnym poziomie jakości.
Oszczędzamy na teksturach i siatkach, nie na cieniach.

---

## 4. MATERIAŁY — wszystko realne

**Zasada nadrzędna właściciela: wszystko ma być z prawdziwych materiałów i tekstur.**
Źródło: Poly Haven (CC0, wolno komercyjnie, bez podawania autora) albo biblioteka właściciela.

**4.1. Każda widoczna powierzchnia ma pełny PBR** — kolor + normalne + chropowatość
(i AO, gdy jest). Ściany, sufity, posadzki, chodniki, elewacje, mury, schody, dachy, blaty.

**4.2. Materiał z samego koloru jest zakazany tam, gdzie istnieje fotoskan.** Drewno ma być
drewnem, kamień kamieniem, beton betonem.

**4.3. Czysty PBR bez map wolno tylko tam, gdzie fotoskanu nie ma:** szkło, metal gładki,
lakier samochodowy, plastik urządzeń, ekrany, lampy, lustra.

**4.4. Rzutowanie w metrach świata, nie „na obiekt".** Cegła ~0,25 m, deska ~0,15 m,
płyta chodnikowa ~0,5 m, tynk 1,5–2 m. Powtarzalność łamana szumem 5–10 %.

**4.5. Czego nie umiemy zrobić realistycznie — zostaje puste** i idzie na listę braków.
Nigdy klocek w kolorze jako zastępstwo.

---

## 5. POSTACIE

**5.0a. JEDEN POTOK OBSADY DLA CZTERECH REJONÓW. Rejon nie ma własnego.**
Właściciel 18.11, 10:54: „W czterech rejonach mają być takie same postacie jakościowo."
Obsada to dziś **46 postaci przerobionych i 102 w kolejce** — przy czterech rejonach i
czterech kopiach potoku to są cztery różne jakości tej samej osoby.

Potok obsady leży na `main` i **tylko tam**:

| Gdzie | Co robi |
|---|---|
| `unity-tools/claude/pobierz_obsade.py` | pobiera paczkę, sprawdza nazwy i sumy kontrolne |
| `unity-tools/claude/osiedle_ruch.py` | rig, ujęcia ruchu, stopy, ramiona |
| `unity-tools/claude/osiedle_wagi.py` | wygładzenie wag na stawach (4 kości na wierzchołek) |
| `unity-tools/claude/osiedle_twarz.py` | atlas mimiki 512 na poziom, usta, powieki, zęby |
| `unity-tools/claude/osiedle_mowa.py` | ścieżki ruchu ust z nagrań |
| `Assets/Editor/Claude/BraxObsadaImport.cs` | import: 2048, ASTC 6×6 / 4×4 dla twarzy i mimiki |
| `Assets/Editor/Claude/BraxClaudeOsiedleObsada.cs` | budowa postaci, materiał, mimika, skala |
| `Assets/Claude/BraxObsadaOsiedla.shader` | shader obsady |

**Rejonowi nie wolno trzymać własnej kopii żadnego z tych plików ani własnych ustawień
importu obsady.** Poprawka wchodzi raz, na `main`, i dostają ją wszystkie cztery rejony
i wszystkie 148 postaci. Nazwy plików zaczynają się od `osiedle_`, bo tam powstały —
to nazwa historyczna, nie przypisanie do rejonu.

**5.0. To jest wymuszone kodem**, nie dobrą wolą rejonu:
`Assets/Claude/Runtime/BraxPostacGlobalna.cs` nakłada standard po każdym wczytaniu sceny —
gładkość 0, metaliczność 0, normalne ×1,3, `SkinQuality.Bone4`, sondy światła i odbić,
cienie. Wystarczy mieć `main`. Wyjątek: obiekt z `BraxObrazWyjatek` jest pomijany,
a powód trafia do logu `BRAX_POSTAC`.

**5.1. Połysk — ustalone przez właściciela 18.11, wybór z sześciu podejść w pokoju prób.**

| | połysk | metaliczność | faktura (mapa normalnych) |
|---|---|---|---|
| **STANDARD GRY** | **0,00** | **0** | **×1,3** |

To było ustawienie „6 bez odblasku". Decyzja zapadła po obejrzeniu wszystkich sześciu
obok siebie na żywych postaciach. **Nie otwieramy tego ponownie bez właściciela.**

**5.1a. KAŻDA POSTAĆ W GRZE, OD 18.11. Bez wyjątków i bez „ten rejon ma inaczej".**
Właściciel 18.11, 16:51: „Każda postać w grze od dzisiaj, każda jedna ma wyglądać tak, jak
w zaakceptowanej wersji. Te ustawienia, żadnych błysków świecących, nic."

To nie jest powtórzenie punktu 5.1, tylko odpowiedź na to, jak ten punkt dał się ominąć.

**Co się stało 18.11.** Gałąź osiedla trzymała **starą kopię** `BraxObsadaOsiedla.shader` —
bez `_Gladkosc`, `_MetalMnoznik` i `_SilaNormalnych`, czytającą połysk i metal wprost z mapy:

```hlsl
o.Metallic   = packedMap.r;      // bez mnożnika
o.Smoothness = packedMap.a;      // bez mnożnika
```

`BraxPostacGlobalna` szuka w materiale właśnie tych nazw. Nie znajdowało ich, **nie robiło
nic** — i wypisywało w logu, że standard został nałożony na 19 postaci. Czyli build wyglądałby
dokładnie jak przed poprawką, a log i wykonawca zapewnialiby, że to wariant 6. Właściciel
wyłapał to na słowo, zanim zobaczył obraz.

Punkt 5.0a zakazywał tej kopii już wcześniej. Zabrakło **sprawdzania**, nie zakazu.

**Dwie rzeczy obowiązkowe od teraz:**

1. **Shader postaci musi mieć pokrętła standardu.** Sprawdzane automatycznie:
   `unity-tools/claude/sprawdz_zasady.py`, reguła **P1** — `BraxObsadaOsiedla.shader` musi
   zawierać `_Gladkosc`, `_MetalMnoznik` i `_SilaNormalnych`, i to **użyte w liczeniu
   światła**, nie tylko zadeklarowane. Brak któregokolwiek = błąd, gałąź nie do odbioru.
   Rejon, który wprowadza własny shader postaci, dokłada w nim te same trzy pokrętła.

2. **Build ma UDOWODNIĆ, że objął wszystkich.** `BraxPostacGlobalna` liczy materiały, które
   nie pasują do żadnej znanej nazwy, i kończy log albo:
   `materiałów bez standardu: 0 (wszyscy objęci)`
   albo listą shaderów, które uciekły. **Niezerowa lista to błąd, nie uwaga** — znaczy, że
   jakaś postać wygląda inaczej niż reszta.

**Objęte shadery** (stan 18.11): `Braxhorn/Obsada Osiedla`, `Braxhorn/HeroPremium` (Patryk,
Brax, Nyxa — tam matowość nazywa się szorstkością i działa odwrotnie, więc podłoga
szorstkości idzie w górę do pełnej), `Braxhorn/LudzieUbranie`, `Braxhorn/LudzieWlosy`,
`Standard`. Nowy shader postaci bez obsługi w `BraxPostacGlobalna` zgłosi się sam w logu.

**Sufit tekstur obsady zostaje taki, jak w pokoju prób** (ciała 4096, mimika 2048), mimo
punktu 2.1. Właściciel 18.11, 16:50: „mają wyglądać tak, jak w ostatnim teście pokoju" —
to jest obraz, który widział i przyjął, więc on wygrywa z sufitem. Zmierzony koszt: +67 MB
na trzech postaciach z mapami 4096. Oszczędność i tak siedzi w ASTC 6×6 zamiast 4×4
(obsada Osiedla: 483 MB → 222 MB).

Skóra i tkanina mają być matowe. Gładkość z map generatora 3D jest
zawyżona i nieprawdziwa — ten sam połysk dostaje wełna, skóra i dłoń. Metaliczność tkanin
i skóry = 0. Zmierzone skutki zawyżonego połysku: krawat kapitana (jasność 42 na koszuli 229)
rozmywał się w jasną smugę, a ciemny mundur ochroniarza dostawał „białe ślady" — przy czym
w jego teksturze **nie ma ani jednego białego piksela**, maksimum to 204.

**5.2. Wagi skinningu: cztery kości na wierzchołek.** Zmierzone na paczce wejściowej:
43–68 % wierzchołków trzymała **jedna albo dwie kości** — stąd załamania na łokciach i kolanach
zamiast fałdy. Potok wygładza je automatycznie (`osiedle_wagi.py`); palce zostają nietknięte.
Log `BRAX_WAGI` podaje udział przed i po.

**5.3. Mimika.** Atlas 512 na poziom (plik 2048×1024). Niżej zęby mają dwa piksele i znikają.
Sufit importu dla mimiki to 2048 — niższy skasuje całą rozdzielczość po cichu.

**5.4. Czego potok nie naprawi: liczby trójkątów.** Ósemka (Patryk, Monika, Mateusz, Kamil,
Ewa, Martyna, Wiechu, Plażowicz) ma po 17–18 tys. trójkątów, reszta obsady 33–40 tys.
Kanciaki na zaokrągleniach biorą się stąd i **wymagają nowych modeli ze źródła**.

**5.5. Normalne są w porządku** — rozciętych 0–0,8 %. Nikt nie ma szukać kanciaków tutaj.

---

## 6. GŁOS I TEKSTY

**6.1. Głosy własne (Patryk, Monika) to nagrania.** Nie ma syntezy ich głosu i nie będzie.
Nowy tekst ich głosem = nowe nagranie.

**6.2. Głosy tła to generator** (Piper, offline, polskie głosy). Tekst zmienia się **bez
nagrywania** — plik `unity-tools/claude/zrob_kwestie.py`. Dziś bank: 292 kwestie w dwóch głosach.

**6.3. Nazwy plików niosą znaczenie:** `_m` / `_f` to płeć, `<postać>_` to głos własny tej
postaci, `<postać>_pijany_` to kwestie po pijaku. Kobieta nigdy nie dostaje męskiego głosu.

---

## 7. BUILD I ODBIÓR

**7.1. APK uruchamia WYŁĄCZNIE właściciel**, pojedynczo, przez `workflow_dispatch`.
Commit i push **nie są zgodą na build**.

**7.2. Nic nie jest zrobione bez pomiaru.** „Wykonawca obiecał" nie jest dowodem.
Automat mierzy, bramka przerywa, nagranie dowodzi.

**7.3. Klucz pamięci podręcznej liczy się z CAŁEGO potoku**, nie z ręcznie wypisanej listy
plików. Inaczej build bierze gotowce i udaje, że wykonał zmianę — tak przepadło przejście
wygładzania wag 17.11 (APK różnił się od poprzedniego o 100 bajtów).

**7.4. Raport wagi z porównaniem.** Każdy build porównany z poprzednim; wzrost musi mieć
listę „co doszło i ile waży". Wzrost bez nowej treści = błąd ustawień.

**7.5. Nic się nie usuwa** bez wcześniejszej listy i zgody właściciela. Odseparować — tak.
Wyrzucić — nie.

---

## 8. CO JEST JESZCZE DO DECYZJI WŁAŚCICIELA

Te trzy rzeczy są świadomie **niezdecydowane** — nie wolno ich rozstrzygać samemu:

1. **Pełne tekstury na telefonie.** `BraxGrafika.Mip()` wymusza połowę rozdzielczości
   w każdym trybie, także Wysokim. To obejście awarii pamięci z 31.10. Zdjęcie go to
   największy pojedynczy zapas jakości, jaki został — ale wymaga pomiaru na telefonie.
2. **Cienie w trybie ŚREDNIM.** Dziś twarde, jedna kaskada na 35 m — krawędź cienia skacze.
   Propozycja: miękkie, 2 kaskady, 45 m. Dotyka płynności na słabszych telefonach.
3. **MSAA 4× zamiast 2× na telefonie.** Dopiero gdy APK z 2× okaże się stabilny.

---

*Zmiany w tym pliku — tylko za zgodą właściciela. Ostatnia aktualizacja: 18.11.2026.*
