# ZLECENIE dla wykonawcy GPT — Braxhorn City (3.11–9.11.2026, potem wraca Claude)

Właściciel: Patryk. Pisze po polsku, krótko i ostro — odpowiadaj po polsku, krótko, szczerze, bez lania wody.
Gra: „GTA połączone z walkami” na Androida (Unity 6000.0.58f2, Built-in RP, glTFast). Miasto i osiedle to dziś dwie
osobne aplikacje (com.braxhorn.miastogl, com.braxhorn.osiedle). Skrót wizji i zasad: `docs/zlecenia/ZASADY_I_WIZJA.md`.

## 0. JAK PRACUJESZ (zasady bezwzględne)
1. **Nic nie wysyłasz bez zgody właściciela.** Build = push na gałąź CI (tabela niżej) albo ręczny workflow — tylko po jego
   „puszczaj”. Pracujesz na własnych gałęziach `gpt/miasto-20261103` i `gpt/osiedle-20261103` (od HEAD z tabeli).
   Nigdy `main`. Przed prośbą o zgodę: lista CO WCHODZI / CZEGO NIE + kompilacja, bramka modułów, brak sekretów w diffie.
2. **Sprawdzasz wszystko.** Po każdym buildzie czytasz logi `BRAXHORN_*` i oglądasz zdjęcia CI, ZANIM właściciel dostanie
   link. Jego zdjęcia i filmy oglądasz w całości (każda widoczna wada na listę, nie tylko to, co napisał).
3. **Nigdy nic nie nakładamy** — stara warstwa jest WYCINANA (gotowe: `BraxWycinanieSiatki`, `WytnijZGrupy`, `CutVolumes`,
   `OznakowanieWWycieciach`), nigdy przesuwana o milimetry.
4. **Skala K = 1.294** (świat pod Braxa 2.33 m): każdy realny wymiar × K (drzwi 0.9 × 2.1 → 1.16 × 2.72 m). Ludzie bez zmian.
   Modele Poly Haven/Sketchfab są w metrach — × K przy wstawianiu.
5. **Premium i detale**: gzymsy, parapety, cokoły, rynny do ziemi, fugi, krawężniki z łączeniami, listwy, ościeżnice,
   klamki, lampy, tabliczki, faktury. **Zakaz „klocków”** (mebel/pojazd/maszyna z gołych prostopadłościanów). Co złe i nie
   do zrobienia porządnie — USUŃ i porządnie domknij. Każda zmiana: log `BRAXHORN_*` + 1–2 ujęcia `DETAL_*` w
   `unity-tools/claude/region_shots.mjs` (`'x,y,z,yaw,pitch,dystans'`, yaw 0 = −Z, 90 = −X, 180 = +Z, 270 = +X).
6. Bez marek handlowych (BRAXHORN = nasza marka, wolno), bez wymyślonych nazw (robocze „OSIEDLE A 1”).
7. Licencje: tylko CC0 / CC-BY / Free Standard; nie CC BY-SA; nic od mortalityrexotable/golukumar.
8. **Repo PUBLICZNE.** Prywatne (Patryk, Monika, Nyxa, Nina, grafiki, postacie, paczki Free Standard) tylko w
   `unity-city/Prywatne/*.tar.gz.enc` (klucz tylko w sekrecie CI). Nie masz klucza: nie dodajesz paczek, nie odszyfrowujesz,
   surowych plików nie commitujesz. Keystore i sekrety — nie dotykasz.
9. Moduły Unity WYŁĄCZONE (w CI błąd CS1069): ParticleSystem, Terrain, WheelCollider, NavMesh, Video, Cloth, Wind, 2D,
   Tilemap, UI/Canvas, UIElements, TMP, ScreenCapture, AndroidJava*. UI = IMGUI, efekty = shader/animacja.
10. **Pamięć i czas buildu**: Unity w CI ma 16 GB. Każda pętla po wszystkich siatkach/trójkątach z limitem (build osiedla
    51fda47 padł „Killed” po 19 min w cięciu oznakowania — naprawione podziałem i limitami). Każdy krok budowy w osobnym
    try/catch (błąd = log „DO POPRAWKI”, build idzie dalej). Automatyczne usuwanie obiektów zawsze z bezpiecznikiem (limit
    liczby + lista w logu).
11. Zapis assetów tylko przez `BraxZapisAssetu.Utworz` (nadpisanie ścieżki = różowe obiekty).
12. Jeden punkt = jeden commit (autor `wawerpatryk89-sketch`), opis po polsku: co, gdzie, jaki log.
13. Zasady świata: jedno wejście dla gości; dźwięk wnętrz na zewnątrz tylko przez otwarte drzwi; golasy tylko na plaży;
    każde jadące auto z kierowcą; zawodnik w stroju walki nie chodzi po mieście; każda postać-zawodnik jest JEDNA w świecie.

## 0b. ŻEBY NICZEGO NIE ZEPSUĆ I ŻEBY BUILDY PRZECHODZIŁY (właściciel: „musi się dobrze zaplanować, buildy mają przechodzić”)
1. **Plan przed pracą:** na początku każdego dnia wypisz właścicielowi plan (punkty, pliki, co ryzykowne) i poczekaj na OK.
   Nie przebudowuj niczego, co działa, „przy okazji”.
2. **Małe kroki:** jeden build = ograniczony zakres (kilka punktów), nie wszystko naraz. Każdy nowy krok budowy za
   try/catch i, jeśli ryzykowny, za flagą (`…Wlaczone = true/false`), żeby dało się go wyłączyć bez cofania kodu.
3. **Kompilacja przed każdym commitem:** projekt musi się kompilować bez błędów (lokalny kompilator C# z referencjami Unity
   albo inny sposób — nigdy nie wysyłaj kodu, którego nie skompilowałeś); bramka modułów (pkt 9) grepem po `unity-city/Assets`.
4. **Ostatni dobry stan:** przed wysyłką zapisz hash ostatniego udanego buildu. Gdy build padnie — przeczytaj log
   (`Killed`/exit 137 = brak pamięci, `error CS` = kompilacja, wyjątek w kroku = log „DO POPRAWKI”), wycofaj winny commit
   (`git revert`), wyślij ponownie dopiero po poprawce i zgodzie. Nigdy nie zostawiaj gałęzi CI w stanie, który się nie buduje.
5. **Po każdym buildzie:** status obu zadań (combined-unity6, android-apk), czas buildu (normalnie ~4–4.5 h), rozmiar APK
   (miasto ~0.8 GB — nie rośnij bez potrzeby), logi z pkt 3, zdjęcia CI. Dopiero potem link dla właściciela.

## 1. Gałęzie
| | gałąź CI (push = build ~4.5 h) | HEAD po wysyłce 3.11 | `unity-city/rejon-buildu.txt` |
|---|---|---|---|
| MIASTO | `claude/miasto-opus-20261031` | ostatni kod 8372b2b (+ commit z dokumentami), wysłane 3.11 rano | `miasto` |
| OSIEDLE | `claude/osiedle-42-poprawka` | ostatni kod 97a8270 (+ commit z dokumentami), wysłane 3.11 rano | `osiedle` |
APK i zdjęcia: draft release `claude-region-preview`. Raporty wykonawców Claude: `docs/zlecenia/RAPORT_C_MIASTO_KONIEC.md`,
`RAPORT_B_OSIEDLE_KONIEC.md` (co zrobione, gdzie w kodzie, pułapki). Lista zgłoszeń właściciela: `MIASTO_LISTA_20261103.md`.

## 2. CO JEST ZROBIONE (w buildach z 3.11)
**Miasto:** różowe obiekty (materiały + kontrola), wyspa 1 km od brzegu (plaża, 22 domki, recepcja, sklep, plac zabaw,
przystań, tor wodny, arena, ludzie: widownia do 150, ratownik, barman), drzewa liściaste na wyspie, panorama 3D (archipelag,
wzgórza, góry), koniec morza (woda bez krawędzi, sztorm, pętla w mgle, siatka bezpieczeństwa), metro (jedno wejście,
schody ruchome, pociąg z modelu, szafka hydrantowa), golasy, kierowcy, drogi bez zapór, elewacje-mozaiki wycięte, napisy,
przeszkody przed drzwiami, wiszące meble, misy drzew, hydranty, szyby, niebo/słońce, kamera za plecami, migotanie siłowni,
fitness Nyxy (mały lokal) z muralem, billboardy i murale z grafik właściciela, mieszkanie Braxa nad NOVA, sprzęt siłowni
Braxa, ekran startowy, nowe dzielnice z modeli właściciela, toalety w holu areny (damska i męska), stoisko sklepu
ośrodka z obsługą, pakiet płynności E19 w mieście (cienie i skóra wg odległości, 4 silniki, adaptacyjny limit
animowanych, horyzont bez cieni), poprawka wejścia do metra (pudełko kontroli kolizji w przejściu wyłączane).
**Osiedle:** drogi 9.06 m i łuki, oznakowanie w asfalcie, auta zwalniają, piesi nie przez budynki, golasy, salon aut
piętrowy (schody ruchome, Patryk sprzedawcą, Monika zastępcza), stacja + myjnia, warsztat + wulkanizacja (koła z kodu),
dźwięk wnętrz (część sal), jedno wejście (z zabezpieczeniem kasyna), pracownicy w sklepach, klocki w boksie/kasynie/
dyskotece, przystanki A 3 + B 2 i autobus z modelu (12 m, zawsze pełny), billboardy, auto sportowe Patryka, ekran startowy.

## 3. NAJPIERW: sprawdź buildy z 3.11
Pobierz logi i zdjęcia obu. Sprawdź: `BRAXHORN_MATERIALY_BRAK` (0), `BRAXHORN_METRO_PRZEJSCIE`, `BRAXHORN_WYSPA_KOMPLET`,
`BRAXHORN_LUDZIE_STROJE`, `BRAXHORN_LUDZIE_SKALA`, `BRAXHORN_SKALA`, `BRAXHORN_KLOCKI`, `BRAXHORN_WEJSCIA`,
`BRAXHORN_DROGI_ZAPORY`, `BRAXHORN_DROGI_OZNAKOWANIE_BUDZET`, `BRAXHORN_AUTOBUS`, `BRAXHORN_KASYNO`, `BRAXHORN_AKWEN*`,
`BRAXHORN_PANORAMA`, `BRAXHORN_BILLBOARDY`, `BRAXHORN_FITNESS`, `BRAXHORN_MIESZKANIE_BRAXA`, `BRAXHORN_SILOWNIA_WYPOSAZENIE`,
`BRAXHORN_START`, `BRAXHORN_WYDAJNOSC`, `BRAXHORN_MIGOTANIE`, `BRAXHORN_DETALE_BEZPIECZNIK`, każde „DO POPRAWKI”, wyjątki, czas i pamięć buildu.
Wypisz właścicielowi krótko. NAJPIERW naprawiasz to, co build pokazał jako zepsute.

## 4. DO ZROBIENIA — MIASTO (kolejność)
1. **WYSPA** (priorytet właściciela — „sprawdź ją 10 razy”): toalety areny (zrobione: pawilony w holu — sprawdź na
   zdjęciach CI), braki z `BRAXHORN_WYSPA_KOMPLET`, logo na macie klatki (wycięcie w siatce maty, nie naklejka), arena premium pkt 57 (trybuny z siedziskami, ring/oktagon premium,
   telebim, światła, wejście zawodników z tunelem, szatnie z sufitem), palmy (lekki model — poproś właściciela), skały
   brzegu, piana, 150 widzów → sprawdź płynność (animacje wyłączane poza kamerą, LOD).
2. Mieszkanie Braxa — poprawki po zdjęciach CI (kierunek mebli, dach nad toaletami klubu).
3. Port i kontenery od nowa (8, 8b, 8c, 13, 21): rzut do akceptacji PRZED budową, kontenery ISO z blachy falistej,
   suwnice na szynach, test wypłynięcia łodzi, łodzie zawsze na wodzie.
4. Sklepy z wyposażeniem i pracownikiem (38), kawiarnia (32–35), szpital i komisariat (50, 51), NOVA z zewnątrz (47),
   wejście na siłownię premium z szyldem 3D (45), schody ewakuacyjne (30), wieżowce na dachach (39b), rynny (49).
5. Naprawy z list `BRAXHORN_SKALA` i `BRAXHORN_KLOCKI`; fitness Nyxy więcej cardio (większy lokal?).
6. Granica lądu (obwodnica/tory/rzeka, 20), dźwięki łodzi (14), dźwięk wnętrz (28), jedno wejście (29), płynność (44).
7. Metro: pociąg z wnętrzem i otwartymi drzwiami, kolejne stacje = kopia M1 + jazda „prowizorka” (60).
8. Autobus w mieście: gracz wsiada i wysiada (jak na osiedlu, pkt niżej).

## 4b. KRÓTSZE BUILDY (właściciel: „żeby nie puszczać całego, już po 4 godziny to trwa”)
Robisz to dopiero po pkt 1–2 miasta i pkt 1 osiedla. Pracujesz na OSOBNEJ gałęzi próbnej, nigdy w buildzie, który ma dojść
do właściciela.
1. **Pomiar przed zmianą.** Z logu CI policz czas każdego etapu (`BRAXHORN_CZAS etap=…` plus czasy kroków joba): pobieranie,
   import Unity, każdy krok generowania świata (`Build()`) i samo APK. Wypisz właścicielowi tabelę. Przed pomiarem nic nie
   zmieniasz. Pierwszy pomiar (miasto, run 41536703095, job sceny): import startowy 270 s, `silownia_baza` 2415 s,
   `generowanie rejon=miasto` 7011 s (prawie 2 h), obróbka siatek 195 s, tekstur 323 s, player 1183 s, razem Unity 13383 s.
   Najwięcej da pamięć generowania świata i bazy siłowni. Dodaj czasy pod-kroków generowania miasta, żeby wiedzieć, która
   część jest najdroższa.
2. **Pamięć części świata.** Teraz pamięć jest tylko dla CAŁEGO osiedla (`PAMIEC_REJONOW`). Klucz zawiera kod, więc przy
   każdej zmianie kodu jest pudło. Cel: część świata, której pliki się nie zmieniły (hash plików tej części), bierze się
   z pamięci CI zamiast generować od nowa. Zacznij od jednej dużej, niezależnej części, np. pierścienia horyzontu albo wyspy.
   Przed dodaniem zmierz rozmiar (limit 10 GB na repo, Library zajmuje ~7–9 GB).
3. **Warunki.**
   - Build z pamięcią i bez pamięci ma dać ten sam świat. Porównaj liczby z kontroli: `BRAXHORN_SKALA`, `KLOCKI`,
     `WEJSCIA`, `WYSPA_KOMPLET`.
   - Błąd odczytu pamięci oznacza generowanie od nowa, a nie przerwany build.
4. **Wejście do głównej gałęzi.** Dopiero gdy próba przejdzie 2 razy (raz z trafieniem, raz bez), i tylko za zgodą
   właściciela.

## 4c. PŁYNNOŚĆ BEZ UTRATY JAKOŚCI (właściciel: „muliła nawet na średnim… nie tracąc na jakości grafiki”)
- Nie ruszasz `BraxGrafika` (ustawienia jakości), `shadowDistance`, `lodBias`, MSAA, tekstur ani modeli. Wycinasz tylko
  to, czego gracz nie widzi: cienie i animacje daleko i poza kamerą, dźwięki dalekich aut, cienie z odległości większej
  niż zasięg cieni.
- Po każdym buildzie sprawdź `BRAXHORN_WYDAJNOSC` w mieście i na osiedlu.
- Każda nowa rzecz z ludźmi lub wieloma obiektami (kibice, plaża, metro) dostaje od razu culling animacji i cienie tylko
  blisko.

## 5. DO ZROBIENIA — OSIEDLE (kolejność)
1. Autobus: GRACZ WSIADA („WSIĄDŹ” na przystanku, siedzi w środku, „WYSIĄDŹ” na przystankach); jeśli
   `BRAXHORN_AUTOBUS DO POPRAWKI` — poszerzyć łuki krawężników na trasie A (autobus zawsze 12 m × K, nie pomniejszać).
2. Monika w salonie i biurze nieruchomości (gdy właściciel dośle model), E24 Żuk (tekstura KTX2), E16 piesi się mijają,
   E20b ganki/schody z kolizją, dźwięk pozostałych sal, klocki pozostałe, szpital E33.
3. NIE ruszasz bez decyzji właściciela: uliczki S1/S2, rozbudowa B2/gospodarstwo.

## 6. Pułapki
RAPORT_B §5 i RAPORT_C: strefy scen (osiedle B i port doczytywane — drzwi przez `BraxCityDoor.All`), siatki obiektów
osobnych zapisywać jako asset, kolejność kroków w `Build()`, nie zamykać drzwi testów CI (`*-front-door`,
`casino-zaplecze-hall`, `gym-rear`), zmiana `fetch_polyhaven.py` = dłuższe pobieranie w CI, skany Poly Haven drzew mają
miliony trójkątów (nie używać), modele aut NC oznaczone „DO WYMIANY PRZED SPRZEDAŻĄ”.

## 7. Raport dla właściciela (codziennie)
Co zrobione (commit + zdanie), co niesprawdzone w Unity, ryzyka, czego potrzebujesz od niego (decyzje, pliki do ściągnięcia).
