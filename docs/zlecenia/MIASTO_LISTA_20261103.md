# MIASTO — dopisek 2.11, 23:05–23:12 (zdjęcia + 10 filmów właściciela z testu APK 5e8a108)

Ton właściciela: „Masz wszystko wyłapać, żebym się znowu nie wkurwił.” „Po chuj robić takie gówno — lepiej nie zrobić
w ogóle, zostawić puste.” „Wszystko dobrze zrobione, nie na pół gwizdka.” Zasada: coś złe i nie do naprawy dziś →
USUŃ (puste miejsce, porządnie wykończone), nie zostawiaj bubla.
Klatki: scratchpad/wideo_miasto/k3/arkusz3_0.jpg, arkusz3_1.jpg, arkusz4.jpg; zdjęcia w wątku prowadzącego.

## P0 — do nocnego buildu (kolejność)
1. METRO NIE DO WEJŚCIA (krytyczne). Dwa wejścia „BRAXHORN — NABRZEŻE” na końcu ul. Portowej (x 66.5…76.6,
   z 20.5…23.9 i 32.6…36). Schody w dół → na dole podest z kafelkiem „szachownica” i czarny prostokąt, dalej nie da się
   przejść (filmy 9689, 9690). Log 5e8a108: `BRAXHORN_METRO status BUILT` (antresola, peron, 3 wagony) — czyli stacja jest,
   ale przejście z szybu schodów na antresolę jest zamknięte albo niewidoczne. Sprawdź: ściany ZejscieWnetrze zamykające
   dolny koniec szybu, czarny materiał w otworze, BraxWidocznoscWnetrza (strefy), kolizje (273), otwory W.Otwor, kolejność
   względem BraxRegion (czy region nie dokłada później terenu/ściany). Dodaj test przejścia: CapsuleCast/kroki co 0.5 m
   po ścieżce chodnik → schody → antresola → bramki → schody na peron → peron, dla OBU wejść; log
   `BRAXHORN_METRO_PRZEJSCIE wejscie=płn ok|zablokowane:<collider> …` (cel: oba ok). Kafelki „szachownica” w szybie
   → okładzina premium (jak w hali). Ujęcia CI: METRO_szyb_dol, METRO_antresola, METRO_peron.
   Dwa wejścia po obu stronach ulicy są normalne (jak w Warszawie) — ZOSTAJĄ, ale oba muszą prowadzić do jednej stacji.
2. DRZEWA NA DRZWIACH. Drzewo przed drzwiami PIEKARNI, przy DELIKATESACH itd. Globalnie w mieście: żadne drzewo / krata
   / donica / ławka / hydrant / słup w pasie przed drzwiami (oś drzwi ± (szer./2 + 1.0 m × K), głębokość 2.5 m × K) — przenieś
   wzdłuż krawężnika albo usuń; log `BRAXHORN_DRZEWA_DRZWI przeniesione=… usuniete=… (cel 0 kolizji)`.
   „Jak jest drzewo na mieście, to ma być zielono koło drzewa”: kraty (ciemne kwadraty) → misa drzewa z trawą/okrywą
   zieloną i niskimi krzewami/bylinami, obrzeże kamienne z fugą (wycięcie w chodniku, nie nakładka). Wszystkie drzewa w
   mieście. Dwa hydranty obok siebie (ul. z DELIKATESAMI) → jeden. Stojak rowerowy wchodzi w ławkę przy przystanku →
   rozsunąć. Brązowy owalny „blat” wiszący przy drzewie przed PIEKARNIĄ → znaleźć i naprawić/usunąć.
3. NAPISY NACHODZĄ. „WINDA DACH” wchodzi na szyld „APTEKA” i „DELIKATESY” (napis windy na dach nie może siedzieć na
   szyldzie sklepu — osobna tabliczka przy drzwiach klatki albo usuń); „CLUB NOVA” podwójny (drugi napis pod spodem / za
   szkłem — film 9675); „SHAKE BAR” nachodzi na „SIŁA • NAWODNIENIE”. Kontrola globalna: przecięcia obrysów wszystkich
   napisów/szyldów/tabliczek → log `BRAXHORN_NAPISY_KOLIZJE liczba=… [nazwy]` (cel 0).
4. MEBLE W POWIETRZU (biura wieżowca): blaty bez nóg, siedziska/ławki wiszą, krzesła złożone z desek (zdjęcie biura
   z oknami). Kontrola globalna wnętrz: obiekt, którego dół jest > 2 cm nad posadzką i nic go nie podpiera → log
   `BRAXHORN_MEBLE_WISZACE` + naprawa (nogi/podstawy jak w realu) albo USUŃ. Recepcja BRAXHORN TOWER: gracz przechodzi
   przez ladę (film 9679) → kolizja lady; hol goły → premium (posadzka kamienna z fugą, ściana z logo w okładzinie, sofy,
   zieleń, oświetlenie liniowe w suficie bez „wiszących kresek”); dymek „Dzień dobry” nie może wisieć za ladą w ścianie.
5. ELEWACJA „MOZAIKA” (film 9665): kamienica przy BRAXHORN NUTRITION — cegła w kwadratach/pikselach na całej ścianie =
   dwie elewacje w jednej płaszczyźnie (migotanie) → dolna wycięta (BraxWycinanieSiatki, jak C3). To samo przy
   BRAXHORN GYM (zdjęcie z dołu — lewa kamienica). Szklane bryły-zaślepki (zdjęcie z placu, schody i drzwi widać przez
   „szkło”) — sprawdź po C4, czy zniknęły.

## P1 — po 01:00 (zapisz w raporcie, czego nie zdążyłeś)
6. SKLEP Z ODŻYWKAMI (BRAXHORN NUTRITION + SHAKE BAR): produkty to gołe walce (zielone/białe/czarne) i kolorowe klocki.
   Mają być realne opakowania: tuby z nakrętką i etykietą, puszki, butelki, batony w kartonikach — etykiety z napisami
   „KREATYNA”, „BIAŁKO WPC”, „BCAA”, „PRE-WORKOUT”, „GAINER”, „OMEGA-3”, „WITAMINY”, „ELEKTROLITY”, z ładną grafiką.
   Atlas etykiet przygotowuje prowadzący (scratchpad/assety_out/odzywki/) — użyj go. Półki pełne i równe, lada shake
   baru premium, posadzka nie „brudny dywan”.
7. PANORAMA Z WIEŻOWCA — „ma być petarda”: 3D miasto dookoła (nie płaskie plansze), od strony morza ARCHIPELAG: nasza
   wyspa (sylwetka zgodna z nową pozycją) + 6–12 innych wysp w tle w różnych odległościach (las, skały, latarnia, małe
   zabudowania), statki na redzie, mgła/haze na horyzoncie; niebo i tarcza słońca poprawione (M12). Taras na dachu: niebo
   wygląda jak płaska ściana z pionowymi smugami → do poprawy.
8. PLAC KONTENEROWY / PORT: „do zmiany w ogóle, masz nowe rzeczy”. Modele od właściciela w /mnt/user-data/uploads:
   container_ship.glb, cargo_ship.glb, city_props_collection_volume_1.glb (+ harbor_navigation_buoy.glb). Kontenery
   w realnych wymiarach (20/40 ft × K) w równych stosach z alejkami, suwnica/reach stacker, ogrodzenie, oświetlenie,
   kontenerowiec przy nabrzeżu z cumami. Żurawie różowe — sprawdzić po C1.
9. PLAŻA MIASTA — „samochodziki, fast food — co to jest, tragedia”: food trucki/budki → premium (realne wymiary, lada,
   menu, oświetlenie, ludzie w kolejce) albo USUNĄĆ.
10. RONDO KWADRATOWE w mieście → okrągłe (wyspa centralna z zielenią, krawężniki łukiem, oznakowanie w wycięciach,
    trasy AI po łuku).
11. Billboardy/plakaty/szyldy „nic premium”: plakaty Braxa na fasadach — rama, podświetlenie, różna treść; szyldy
    z grubością, podświetleniem, mocowaniem. Bez marek.
12. Taras/dach wieżowca: leżaki-klocki → premium; NOVA: drzwi z „laserowym obrazkiem” — wejście ma prowadzić do środka
    albo drzwi zamknięte bez obrazka (zgodnie z godzinami otwarcia).

## Dopisek 23:11–23:12
13. PORT/MARINA „idealnie powiększony pod wszystkie statki, żeby ładnie były, można wypłynąć, wyliczone wszystko
    idealnie”. Policz z modeli: dla każdej jednostki (jachty, motorówka, skutery, łódź gracza, kontenerowiec przy
    nabrzeżu) długość i szerokość × K → stanowisko = długość + 10 %, szerokość + 2 × 0.6 m × K odbijacze; alejka między
    pomostami ≥ 1.75 × najdłuższa jednostka; główki portu i tor wodny ≥ 3 × szerokość największej + zapas; koło
    obrotowe w basenie ≥ 1.5 × najdłuższa; głębokość pod kilem. Test: z każdego stanowiska trasa wypłynięcia do otwartego
    morza (sweep obrysu łodzi) — log `BRAXHORN_PORT_WYPLYNIECIE stanowisko=… ok|kolizja:<obiekt>` (cel: wszystkie ok)
    + `BRAXHORN_PORT_WYMIARY`. Rzut z liczbami jako obraz do pokazania właścicielowi (scratchpad/plan/PORT_RZUT.png).
14. DŹWIĘKI: skutery wodne też (silnik zależny od prędkości, plusk), razem z motorówką i jachtami (M20).

## Dopisek 23:13 — KONIEC MORZA (właściciel „popłynąłem do końca morza i wypadłem z terenu”) — P0, przed metrem (szybkie)
15. a) Morze bez krawędzi: płaszczyzna wody (i dno/mgła) podąża za kamerą skokowo (snap do siatki, żeby fale nie
       pływały), więc nigdy nie widać końca wody i nie da się z niej wypaść.
    b) Granica akwenu „miękka”: od ~2.2 km od brzegu (dopasuj do nowej wyspy x 1200 + zapas) rośnie fala i mgła,
       komunikat „Silny prąd — dalej nie płyń, zawróć”, łódź/skuter zwalnia i jest łagodnie obracana z powrotem;
       twarda granica o 150 m dalej (łódź nie przejdzie). Gracz wpław — to samo.
    c) Siatka bezpieczeństwa globalna: gracz/pojazd/łódź poniżej poziomu terenu-wody − 5 m albo poza granicą świata →
       powrót na ostatnią bezpieczną pozycję (zapisywaną co 1 s na gruncie/wodzie), log `BRAXHORN_SIATKA_BEZP`.
    d) Wyspy na horyzoncie (archipelag, pkt 7) jako pierścień 3D daleko poza granicą — widać je zawsze na horyzoncie,
       nie da się dopłynąć (pierścień przesuwa się z kamerą, wyspy zawsze ~3–5 km dalej), z mgłą. Nasza wyspa i
       ewentualne małe wysepki do odwiedzenia — prawdziwe, w granicy akwenu.
    Log `BRAXHORN_AKWEN granica_miekka=… twarda=… woda_za_kamera=tak`.

## Dopisek 23:13 — WYSPA Z BLISKA „strasznie wygląda” (zdjęcie ze skutera: jasnozielone stożki-drzewa „kreskówkowe”,
gołe zbocze, pastelowe kolory; skuter cały magenta)
16. Wyspa: wszystkie drzewa-stożki z paczki Vacation Island → realistyczne drzewa (te same modele co drzewa miejskie /
    sosny i liściaste z osiedla, warianty skali i obrotu, LOD), krzewy i trawy przy ziemi; teren: trawa/piasek/skała
    z teksturami jak w mieście (nie pastel), skały naturalne; brzeg z przejściem piasek → woda (pianka). Z daleka
    (BraxWyspaDaleko) ta sama kolorystyka. Kolejność: P0 po pkt 5, jeśli zdążysz; inaczej pierwsze po 01:00.
    Skuter magenta → sprawdź po C1 (kontrola BRAXHORN_MATERIALY_BRAK musi obejmować łodzie/skutery).

## Dopisek 23:14 — pomysł właściciela: „wypływasz z drugiej strony” (zmienia pkt 15b)
15b′. Akwen zapętlony wzdłuż wybrzeża: na końcach północnym i południowym akwenu pas gęstej mgły ~150 m; w środku mgły
      (niewidoczne dla gracza, krótkie przyciemnienie 0.3–0.5 s) łódź/skuter/pływak przenosi się na drugi koniec akwenu
      (z ↔ −z względem środka akwenu, ta sama odległość od brzegu, ten sam kurs i prędkość) i wypływa z mgły — morze
      „bez końca”. Na otwarte morze (w głąb, od miasta) zostaje miękka granica: mgła + prąd łagodnie zawraca. Wyspy
      horyzontu (15d) zawsze widoczne. Siatka bezpieczeństwa (15c) zawsze. Sprawdź, żeby po przeniesieniu nie było
      kolizji (sweep obrysu łodzi), log `BRAXHORN_AKWEN_PETLA przeniesienia_test=ok`.

## Dopisek 23:14–23:15
17. Granica otwartego morza jako SZTORM: przy zbliżaniu do granicy pogoda się psuje (ciemne chmury, deszcz z siatki/
    shadera bez ParticleSystem, wysokie fale, wiatr, dźwięk), komunikat „Sztorm — zawróć, bo utoniesz”, łódź zawracana.
    Wyspy na horyzoncie (15d) — TAK, właściciel potwierdził. Małe wysepki do odwiedzenia — później.
16b. Zdjęcie 23:17 z bliska: palmy z płaskimi, bladymi liśćmi (karty „kreskówkowe”) + stożki — też do wymiany na
     realistyczne (palmy tylko na plaży, reszta wyspy drzewa liściaste/iglaste). Jeśli brak dobrego modelu palmy —
     zgłoś prowadzącemu, poszuka darmowego (CC0/CC-BY) do pobrania przez właściciela.

## Dopisek 23:19 — ZNAKI DROGOWE w mieście „do wymiany” (P1, po 01:00, globalnie, też na osiedlu do porównania)
18. Wszystkie znaki pionowe: realne wzory polskie (A-7 ustąp, B-20 STOP, D-6 przejście, B-1, B-36, C-2/C-4, D-1, D-3,
    T-tablice), wymiary tarcz wg „średnich” (np. trójkąt 900 mm, koło 800 mm) × K, lico odblaskowe z fakturą, rant
    i tył tarczy szary ocynk z obejmami, słupek Ø60 mm z kapslem, dolna krawędź 2.0–2.2 m × K nad chodnikiem, ustawienie
    0.5–1.0 m od krawężnika, twarzą do jadących. Tabliczki ulic (UL. PORTOWA) z ramką i mocowaniem. Sygnalizatory:
    realne (komory z daszkami, tarcza kontrastowa, przyciski dla pieszych, sygnalizator dla pieszych osobno, wysięgnik
    na skrzyżowaniach). Tekstury znaków generujemy sami (wektorowo w kodzie / atlas PNG — prowadzący może przygotować).
    Kontrola: znak nie stoi w drzwiach/przejściu (jak pkt 2), log `BRAXHORN_ZNAKI liczba=… typy=…`. Ujęcia DETAL_znak_*.

## KOLEJNOŚĆ PO 01:00 (właściciel 23:23): 1) PANORAMA z wieżowca (pkt 7 + 15d archipelag), 2) METRO (wynik
BRAXHORN_METRO_PRZEJSCIE z buildu 57c1767 + naprawa), potem reszta P0 (2–5), wyspa (16), P1.
Wysłane 23:22: osiedle 51fda47 (claude/osiedle-42-poprawka), miasto 57c1767 (claude/miasto-opus-20261031).
19. GOLASY (właściciel 23:23): ludzie w strojach kąpielowych / bez ubrań poza plażą — w mieście ZERO. Stroje plażowe tylko
    w strefie plaży (miasto: plaża przy porcie, wyspa). Poza nią: zamiana na ubrane warianty albo usunięcie. Kontrola
    `BRAXHORN_LUDZIE_STROJE poza_plaza=0`. Zaraz po metrze (szybkie).
7b. (23:24, zdjęcie ze skutera za miastem) Horyzont lądu za miastem to płaski pierścień-obraz (wzgórza, drzewa, klify
    jak naklejka, rozmyte), na niebie pionowe tęczowe smugi/pasy (artefakt pierścienia horyzontu albo tekstury nieba)
    → w ramach PANORAMY: ląd za miastem jako warstwy 3D (wzgórza z siatki, lasy z prawdziwych drzew/impostorów, pola,
    zabudowa podmiejska), mgiełka wg odległości, ZERO smug na niebie (sprawdzić ujęciem z wody w stronę lądu).

## 20. KONIEC ŚWIATA — każda krawędź z sensem (właściciel 23:24 „dobrze zakończyć świat, bez bez sensu czegoś”)
- MORZE (wschód): sztorm/prąd + archipelag-horyzont (15, 17, 7).
- WYBRZEŻE N/S: pętla w mgle (15b′) albo klify/falochron z latarnią, za nimi horyzont 3D.
- LĄD (za miastem): ostatnie ulice kończą się realnie — obwodnica/autostrada na nasypie z ekranami akustycznymi (wjazd
  zamknięty: „ROBOTY DROGOWE”/barierki), tory kolejowe za ogrodzeniem, rzeka/kanał, pas lasu, wzgórza 3D za nimi (7b).
  Gracz pieszo i autem: ogrodzenie/bariery z sensem (nie niewidzialna ściana), komunikat tylko przy próbie obejścia.
- OSIEDLE: to samo dla granic osiedla (las, pola, droga zamknięta), spójne z miastem.
- Siatka bezpieczeństwa wszędzie (15c). Ujęcia CI z każdej krawędzi: KONIEC_wschod, KONIEC_polnoc, KONIEC_poludnie,
  KONIEC_lad. Log `BRAXHORN_KRAWEDZIE` (każda krawędź: typ zamknięcia, dziury=0).
8b. (23:25) KONTENERY premium robione w kodzie (bez pobierania): ISO 20 ft 6.06 × 2.44 × 2.59 m i 40 ft 12.19 m (× K),
    ściany z blachy falistej (geometria trapezowa, nie tekstura z kreskami), drzwi dwuskrzydłowe z 4 ryglami i dźwigniami,
    naroża odlewane (corner castings) z otworami, rama dolna i górna, numer kontenera i tabliczka CSC (bez marek
    przewoźników), wyblakła farba + rdza na krawędziach (8–10 kolorów), instancjonowane (GPU instancing) — jedna siatka na
    typ. Stosy równe z alejkami; na statku (model statek_kontenerowiec z paczki miasto_port, już w buildzie 57c1767)
    sprawdzić, czy własne kontenery modelu wyglądają dobrze — jak nie, podmienić na nasze.
21. (23:26) SKUTER LATA: skuter wjechał/wyskoczył nad stos kontenerów przy statku (zdjęcie) — łodzie i skutery zawsze na
    powierzchni wody (y = fala ± zanurzenie), kolizja z kadłubem/nabrzeżem tylko w poziomie (odbicie), nigdy wspinanie;
    log `BRAXHORN_LODZIE_WYSOKOSC max_nad_woda=…`.
22. Modele do pobrania przez właściciela (CC-BY, znalezione 23:26): palma „Realistic Palm Tree 4 Free” (NextSpring, 8k),
    „Realistic Palm Tree Free” (27k), „Realistic Tree 2 Free” (4.6k), „Coffee Cart” (outlier_spa), „UNDERPOLY: Free
    Jet-Ski + Trailer”. Lista: scratchpad/szukaj_2326/wyn.json.

## 23. POLY HAVEN (CC0) — właściciel 23:30: „skały też są w Polyheaven, nie korzystali”. CI już pobiera przez
unity-tools/claude/fetch_polyhaven.py, ale tylko ~10 modeli (island_tree_01/02, jacaranda_tree, tree_small_02, doniczki,
krzesła). ROZSZERZYĆ listę (1k tekstury dla telefonu) i UŻYĆ:
- skały/wybrzeże: coast_rocks_01/02/03/05, coast_land_rocks_02/03/04, coastal_cliff_04, coast_line_01/02, boulder_01,
  rock_moss_set_01/02, sand_rocks_small_01, rock_07/09 → brzeg wyspy, wybrzeże miasta, falochrony, horyzont.
- roślinność: pine_tree_01, fir_tree_01, fir/pine_sapling, shrub_01…04, fern_02, grass_medium_01/02, grass_bermuda_01,
  island_tree_03, tree_stump_01/02 → wyspa (zamiast stożków), misy drzew w mieście (zieleń), obóz, horyzont.
- UWAGA palmy na wyspie (blade, płaskie liście): sprawdzić, czy to island_tree_01/02 z zepsutym materiałem liści
  (alpha cutout / sRGB / brak albedo) — wtedy naprawa materiału, nie model.
- Sprawdź też tekstury PH (asfalt, bruk, beton, cegła, tynk) dla elewacji i nawierzchni.
API api.polyhaven.com osiągalne z sesji; pliki pobiera CI.
- PH dodatkowo: ocean_buoy, lifebuoy, modular_wooden_pier → boje toru wodnego, koła ratunkowe na nabrzeżu/plaży,
  pomost na wyspie.
24. LATARNIA MORSKA — robimy SAMI w kodzie (modele z Sketchfaba zabawkowe, właściciel 23:32): wieża ~30–40 m × K, styl
    bałtycki (cegła ceglasta z fugą albo biały tynk z pasami), cokół kamienny, drzwi, okienka na klatce schodowej,
    galeria z balustradą, laternia przeszklona (szprosy), kopuła miedziana z patyną, wiatrowskaz; w nocy obracające się
    światło (stożek/billboard + błysk na horyzoncie). Na cyplu/wyspie + wersja horyzontu. Ujęcia DETAL_latarnia_*.
25. TAKSÓWKI (właściciel 23:33: „taksówek nie mamy”) — P1: auta-taksówki w ruchu AI (nasze modele aut, malowanie
    taxi + podświetlany kogut „TAXI” na dachu, numer boczny, bez marek korporacji), postoje taxi (metro, port, dworzec,
    NOVA, wieżowiec), zatrzymanie gestem/przyciskiem albo wezwanie z telefonu; przejazd = szybka podróż z kamerą z
    tylnego siedzenia/skrótem; taksometr — cena (EKONOMIA, teraz 0 zł). Osiedle: postój przy wjeździe.

## DO WPISANIA W claude/braxhorn-city-WIZJA.md po 01:00 (właściciel 23:37)
- Priorytet: miasto i osiedle ROZBUDOWAĆ jeszcze bardziej (główna część gry), obóz zrobić dobrze, na końcu miasto mistrzów.
- Do miasta mistrzów jedzie się TYLKO po awansie do Ligi Mistrzów (walki o tytuł) — nie wcześniej, nie inną drogą.
- (23:38) OBÓZ odwiedzany wielokrotnie: między osiedlem a miastem, po utracie pasa, i OBOWIĄZKOWO przed wyjazdem do miasta
  mistrzów (zaliczenie obozu = warunek). Obóz „konkretny”: nie tylko walki — treningi wytrzymałościowe (biegi górskie,
  pływanie w jeziorze, kłody, liny, opony, sanki, interwały), siła, technika, sparingi; zadania obozowe do wymyślenia później.
- (23:40) PLAN BOSSÓW (propozycja prowadzącego, do akceptacji): każdy rejon ma bossa z WŁASNYM miejscem walk i stylem,
  przed bossem jego ekipa (2–4 walki); po pokonaniu miejsce zostaje (rewanże, obrony). OSIEDLE: garaże (ustawki, uliczny),
  sala bokserska (ring, bokser), arena osiedla (klatka, MMA) → PAS OSIEDLA. MIASTO: port/magazyn kontenerów (nielegalne,
  zapaśnik), podziemie metro/bunkier (klub walk, brawler), zaplecze NOVA (ring VIP, Muay Thai), arena miasta (oficjalna
  liga, kickbokser), arena na wyspie (mistrz miasta, MMA) → PAS MIASTA. OBÓZ: egzamin = sparing z mistrzem obozu.
  MIASTO MISTRZÓW: ranking top 10 = gwiazdy z własnym wejściem/show, mistrz świata = boss finałowy. Technicznie: JEDEN
  zestaw premium klatka/ring + różne otoczenia, światło, publiczność → tanio dla telefonu. ~8 bossów × 2–4 ≈ 30+ walk w etapie 1.
8c. (23:42) PORT KONTENEROWY — CAŁKOWICIE OD NOWA (same „kwadraty”): plac, stosy, nabrzeże, budynki obsługi. OBA DŹWIGI
    (suwnice nabrzeżowe) ustawione realnie: na szynach wzdłuż krawędzi nabrzeża, wysięgnik nad stanowiskiem statku,
    rozstaw nóg nad torami/pasem ruchu, zderzaki końcowe szyn, kabel zasilający w korycie; drugi dźwig też na szynach,
    w odstępie roboczym. Pawilon przy nabrzeżu z różowymi oknami → sprawdzić po C1. Rzut portu do akceptacji (PORT_RZUT.png).
- (23:43) MODEL WŁAŚCICIELA (zastępuje „plan bossów” jako zasada): każdy rejon (osiedle, osiedle z willami, miasto, liga
  mistrzów) ma JEDNEGO MISTRZA z pasem. Żeby z nim walczyć — kwalifikacja w lidze rejonu: punkty za wygrane z ~5+
  „kozakami” rejonu (każdy w swoim miejscu walk — miejsca z planu bossów zostają jako areny kozaków). Przegrana = minus
  punkty, cofasz się w rankingu, walczysz znowu. Próg punktów → walka o pas z mistrzem → pas = przejście dalej.

## 26. ZAKAZ „KLOCKÓW” — ZASADA GLOBALNA (właściciel 23:43, zdjęcie: ciężarówki portowe z kostek i walców)
„Nigdy więcej nie chcę takich rzeczy widzieć.” Żaden pojazd, maszyna, mebel, urządzenie złożony z gołych prostopadłościanów
i walców. Albo prawdziwy model (paczki miasto_pojazdy itd., Poly Haven, pobrane przez właściciela), albo porządnie
modelowany w kodzie z detalem (fazy, zaokrąglenia, szyby, lampy, opony z felgą), albo USUNĄĆ. Audyt globalny miasto +
osiedle: log `BRAXHORN_KLOCKI lista=[ścieżka, liczba prymitywów, wymiary]` — wszystkie obiekty-grupy złożone w >80 %
z prymitywów/boksów bez tekstury szczegółu; każdy pozycja: wymiana albo usunięcie. Pierwsze po 01:00 razem z panoramą/metrem.
Do wpisania w claude/braxhorn-city-ZASADA-DETALE.md po 01:00.
1b. (23:44) METRO — DECYZJA WŁAŚCICIELA: JEDNO wejście (nie dwa), przesunięte tak, żeby nie blokowało przejścia — przy
    budynku, w jednym miejscu (wpięte w chodnik obok elewacji, swobodne przejście chodnikiem ≥ 2.5 m × K obok wiaty).
    Drugie wejście zwalone, chodnik/teren w tym miejscu odtworzony (wycięcie zamknięte, bez dziury). Stacja i przejście
    do peronu z jednego wejścia (test BRAXHORN_METRO_PRZEJSCIE dla jednego wejścia).
1c. (23:45, zdjęcie z dołu schodów) Korytarz stacji WIDAĆ (mapa linii, kosz), ale wejść się nie da → niewidzialna kolizja
    na progu (sprawdzić log BRAXHORN_METRO_PRZEJSCIE z 57c1767 — wyłączone kolizje). Czerwony KLOCEK (szafka hydrantowa?)
    → prawdziwa szafka hydrantowa z drzwiczkami, szybką, wężem (albo usuń). Kafel „szachownica” na ścianach i posadzce →
    prawdziwa okładzina (płyty wielkoformatowe, fuga). POCIĄG: ma stać na peronie — użyć POBRANEGO modelu (metro_train
    z paczki miasto_silownia_metro, Assets/Source/Claude/Metro), nie wagonów z klocków; drzwi otwarte, wnętrze oświetlone.
27. (23:46) ELEWACJE JEDNOLITE — GLOBALNIE: wieżowce z doklejonym dołem w innym stylu (różowo-ceglana „plamista” elewacja
    z innymi oknami pod ciemnym szklanym trzonem — zdjęcie). Dolną elewację ZWALIĆ (wyciąć, nie nakładać) i zrobić
    taką samą jak budynek u góry — ciągła od parteru do dachu (te same okna, rytm, materiał), parter tylko z witrynami/
    wejściem w tym samym stylu. Audyt globalny: budynek, którego elewacja zmienia materiał/rytm okien w pionie bez sensu
    architektonicznego → log `BRAXHORN_ELEWACJE_NIESPOJNE lista=…` + naprawa. Plamista tekstura „brudu” → czysta.
28. (23:47) DŹWIĘK — za siłownią (z tyłu, na zewnątrz) słychać muzykę z wnętrza. Muzyka wnętrz (siłownia, NOVA, kasyno,
    sklepy) tylko w środku: strefy (BoxCollider/obrys pomieszczenia) — w środku pełna głośność, w korytarzu/przy otwartych
    drzwiach ciszej (tłumienie przez ściany), na zewnątrz za ścianą ~0 (najwyżej stłumiony bas przy samych drzwiach).
    Źródła 3D z min/max distance dopasowanym do sali; bez źródeł 2D na całą mapę. Globalnie miasto + osiedle (jak zasada
    z kasyna: główna sala głośno, korytarz ciszej). Log `BRAXHORN_AUDIO_STREFY`.
    Doprecyzowanie właściciela 23:47: dotyczy KAŻDEGO pomieszczenia. Słychać z zewnątrz tylko tam, gdzie są otwarte drzwi /
    otwór (przy drzwiach); tył budynku / ściana bez drzwi = NIC nie słychać.
29. (23:47) JEDNO WEJŚCIE — ZASADA GLOBALNA: przeważnie wszędzie jedno wejście dla gości (nie dwa). Drugie dozwolone tylko
    jako realne wyjście ewakuacyjne/zaplecze (zamknięte dla gracza, oznaczone). Audyt miasto + osiedle: budynki z ≥2
    wejściami dla gości → zostaje jedno (najlepiej od ulicy), reszta zwalona i zamurowana elewacją (wycięcie, nie
    nakładka). Log `BRAXHORN_WEJSCIA podwojne=0`.
30. (23:47) SCHODY EWAKUACYJNE na elewacjach (kamienice przy BRAXHORN NUTRITION i BRAXHORN GYM — film 9665, zdjęcie z dołu):
    zrobione źle — płaskie, z wiszącymi niebieskimi/żółtymi prostokątami, na migoczącej elewacji. Od nowa w kodzie, premium:
    stalowe podesty na każdej kondygnacji przy oknach-wyjściach, biegi schodów z prawdziwymi stopniami (krata), balustrady
    z pochwytem, drabina opuszczana na ostatnim poziomie, wsporniki kotwione w ścianę, czarna farba z rdzą; wymiary × K;
    okna/drzwi wyjścia na podest dopasowane. Kolorowe prostokąty usunąć. Audyt wszystkich schodów ewakuacyjnych w mieście.
31. (23:48, 2 zdjęcia z placu z trawnikiem i ławkami): (a) OGROMNA szara płaszczyzna przecina niebo nad placem (od lewej,
    jak dach/płyta bez końca) — znaleźć i usunąć/naprawić (krytyczne, widać z daleka); (b) beżowe okrągłe „blaty” wiszą
    w powietrzu (parasole bez masztu / stoły bez nóg) — prawdziwe stoliki z parasolami albo usunąć; (c) półnagi mężczyzna
    na ławce (golasy, pkt 19); (d) ławki stoją na krawędzi murka/obrzeża trawnika — przestawić na równy grunt, 0.5 m od
    krawędzi; (e) pierzeja w tle z różnych „doklejonych” fragmentów elewacji (pkt 27).
32. (23:48) KAWIARNIA (w pawilonie przy placie): napis „KAWIARNIA” wisi w powietrzu w środku sali → szyld na elewacji/nad
    ladą na ścianie; lada = goła skrzynia + szary klocek → lada premium (front z frezowaniem, blat kamienny, ekspres
    kolbowy, młynki, witryna chłodnicza z ciastami, kasa, kubki, tablica menu nad ladą — atlas jak SHAKE BAR); stół przy
    chesterfieldach z rozjechanymi deskami na blacie → prawdziwy stół; krzesła ucięte / same oparcia → prawdziwe;
    wisząca beżowa półka po lewej → półka na wspornikach albo usunąć; dodać: lampy wiszące, rośliny, obrazy, goście przy
    stolikach, zapach „premium”. Barista NPC przy ekspresie. (Kawiarnia = usługa z ceną, EKONOMIA.)
33. (23:49) KAWIARNIA wygląda jak recepcja, nie kawiarnia (pkt 32 — przebudowa całości, charakter kawiarni). Drzwi WC: za
    małe (niższe niż Brax), wstawione w ścianę jak naklejka, obok wisząca deska → drzwi 0.9 × 2.1 m × K (1.16 × 2.72 m) z
    ościeżnicą, klamką, tabliczką; za nimi prawdziwa łazienka (umywalka, WC, lustro, płytki). SKALA MIASTA = PODSTAWA
    (M17, właściciel): audyt globalny skali starej części miasta × K — drzwi, lady, krzesła, stoły, schody, poręcze,
    witryny vs Brax 2.33 m; log `BRAXHORN_SKALA niezgodne=… [obiekt, wymiar, oczekiwany]` + naprawa. Priorytet zaraz po
    panoramie i metrze.
34. (23:49) WC w kawiarni (pawilon szklany): w środku pusto, ściany to szyby na ulicę, biały klocek zamiast muszli, brak
    sufitu — „lepiej, żeby tego nie było”. USUNĄĆ WC i drzwi (ściana kawiarni domknięta porządnie, bez śladu). Globalna
    reguła: żadnych pomieszczeń sanitarnych w szklanych pawilonach; WC tylko jako pełne pomieszczenie (ściany pełne,
    sufit, płytki, armatura) albo wcale.
35. (23:50, kawiarnia z góry) Meble porozrzucane: krzesła luzem bez stolików, obrócone w różne strony, krzesło wchodzi w sofę,
    słupki kolejkowe (czarne) między krzesłami, stół z ukośnymi listwami „X” na blacie między sofami. ZASADA GLOBALNA
    UKŁADU MEBLI: krzesła tylko przy stołach (2–4, przodem do stołu, odstęp ~0.1 m), sofy z własnym stolikiem, przejścia
    ≥ 0.9 m × K, zero przenikania mebli (kontrola obrysów), słupki kolejki tylko przy kasie/wejściu. Log
    `BRAXHORN_MEBLE_UKLAD kolizje=0 krzesla_bez_stolu=0`. Miasto + osiedle.
36. (23:50, park z aleją): (a) cienki czarny słup na środku alei/trawnika bez oprawy (latarnia bez głowicy?) — prawdziwa
    latarnia parkowa (jak ta w tle) albo usunąć; (b) czarny prostokątny klocek wisi przed ciemnym wieżowcem — znaleźć,
    usunąć; (c) za płotem parku płaski horyzont-naklejka (wzgórza, drzewa) → panorama 3D (7b); (d) trawa jaskrawa,
    plamista → naturalna tekstura (Poly Haven grass), bez przesycenia.
37. (23:51) SKALA LUDZI W MIEŚCIE — właściciel: „ludzie skali nie mają” (zdjęcie: facet na ławce i półnagi w slipach
    wyglądają za mało przy Braxie). Ludzie w mieście = te same wysokości co na osiedlu (#44: zasada „ludzie bez zmian”,
    baza jak na osiedlu) — porównać rzeczywiste wysokości NPC miasta vs osiedla w logu `BRAXHORN_LUDZIE_SKALA
    miasto_sr=… osiedle_sr=… brax=2.33` i wyrównać do osiedla; ławki/siedzenia × K pasujące do ludzi. Półnagi w slipach
    na placu → pkt 19. Wieżowiec na szarym „cokole-murze” doklejonym — pkt 27; czarny klocek i słup — pkt 36.
38. (23:52) SKLEPY / KIOSK bez wyposażenia (półki z kolorowymi klockami, goła lada-klocek, ciemna podłoga) — ZAKAZ KLOCKÓW
    (26). Poly Haven (CC0, CI pobiera): owoce/warzywa food_apple_01, bananas, lemon, food_lime_01, food_kiwi_01,
    food_avocado_01, food_pears_asian_01, food_pomegranate_01, sweet_potato, yellow_onion, food_ginger_01; pieczywo/ciasta
    croissant, hamburger_buns, carrot_cake, strawberry_chocolate_cake; konserwy long_life_food, russian_food_cans_01;
    napoje wine_bottles_01, plastic_bottle_gallon; chemia bleach_bottle, multi_cleaner_bottle; wyposażenie
    wooden_display_shelves_01, steel_frame_shelves_01/02/03, painted_wooden_shelves, wicker_basket_01/02, plastic_crate_01/02/03,
    wooden_crate_01/02, cardboard_box_01, wooden_cutting_board. + nasze etykiety (atlas odżywek) dla opakowań.
    Każdy sklep: lada z kasą, regały pełne realnymi produktami pasującymi do typu sklepu (piekarnia, delikatesy, apteka,
    kiosk, kantor, odżywki), chłodziarki z napojami, posadzka sklepowa, oświetlenie.
    PRACOWNIK W KAŻDYM SKLEPIE / USŁUDZE — WSZĘDZIE (właściciel): NPC za ladą (strój pasujący: fartuch piekarza,
    kitel w aptece…), animacja obsługi, kwestia przy podejściu. Log `BRAXHORN_SKLEPY bez_pracownika=0 klocki=0`.
39. (23:53, ulica z kamienicami ze Sketchfaba — dół wygląda dobrze) Wysokie szklane wieżowce „dostawione do góry” — wyrastają
    zza/na kamienicach, wyglądają jak postawione na ich dachach, różnią się stylem. Zasada: wieżowiec tylko na WŁASNEJ
    działce z własnym parterem/placem przy ulicy (widać, gdzie stoi na ziemi), nie bezpośrednio za pierzeją kamienic tak,
    żeby wyglądał na nadbudowę; gdzie nie ma działki → usunąć/przenieść. Strefowanie: kamienice w kwartale razem, wieżowce
    w dzielnicy biurowej. Audyt `BRAXHORN_WIEZOWCE_NADBUDOWA lista=…` (wieżowiec, którego podstawa nie styka się z ulicą/placem
    a stoi < 15 m za niższym budynkiem). Powiązane z pkt 27.
40. (23:53) INTERFEJS PREMIUM (cała gra: menu, USTAWIENIA, HUD, podpowiedzi akcji, dymki, mapa, sklep): teraz goły tekst IMGUI
    na tle gry, „WSTECZ” nachodzi na pasek energii, wybrane opcje nieczytelne, suwaki gołe. Ograniczenie: moduły UI/Canvas/
    TMP wyłączone → premium w IMGUI: własne GUIStyle z teksturami 9-slice (panele ciemne półprzezroczyste z zaokrągleniem
    i cienką ramką w akcencie, karty), przyciemnienie + pauza gry pod menu, jedna czcionka z polskimi znakami (wagi),
    przełączniki segmentowe (MAŁE/ŚREDNIE/DUŻE) z wyraźnym zaznaczeniem, suwaki z wartością, ikony, bezpieczne marginesy
    (wycięcie aparatu), skala pod DPI, animacje wejścia. Prowadzący przygotuje projekt UI do akceptacji właściciela
    (makieta) PRZED wdrożeniem. Osiedle i miasto wspólnie.
39b. (23:54) Właściciel: wieżowce „robią robotę” (widok z ulicy kamienic z wieżowcami w tle — dobry klimat metropolii).
     ZMIANA pkt 39: wieżowców NIE usuwamy. Poprawiamy TYLKO miejsca, gdzie podstawa wieżowca widocznie siedzi na dachu
     niższego budynku (efekt nadbudowy) — tam odsunąć o kilka metrów / dać własny parter w tym samym stylu. Reszta zostaje.

## Dopisek 23:55–01:01 (przegląd prowadzącego 01:05)
41. KIEROWCY: auta w ruchu AI w mieście bez ludzi — każdy jadący samochód ma kierowcę (jak na osiedlu, gdzie już siedzą
    dobrze); zaparkowane puste. Log `BRAXHORN_AUTA_KIEROWCY jadace_bez=0`.
42. DROGI ZABLOKOWANE siatką/ogrodzeniem i szlabanami (zdjęcie: siatka przez całą jezdnię za placem z BANKIEM; szlaban
    „OSIEDLE – WJAZD ZAMKNIĘTY” przed tunelem z czarną ścianą) = BŁĄD. Każda ulica miasta przejezdna albo kończy się
    sensownie (pkt 20). Tunel do osiedla: do decyzji właściciela (A: punkt przejazdu do osiedla — ekran ładowania /
    komunikat „Osiedle — osobna aplikacja” do czasu połączenia; B: zwalić tunel, ulica zawraca). Czarna ściana w tunelu ZAKAZ.
43. NOVA (miasto) — dyskoteka: lasery RUCHOME (obrót/omiatanie, zmiana koloru w rytmie), kule dyskotekowe ŚWIECĄ
    (refleksy/plamki światła na ścianach i suficie, obrót). Bez ParticleSystem — shader/animacja.
44. PŁYNNOŚĆ: na ŚREDNIEJ jakości gra się tnie miejscami — profil: draw calls, batching/instancing (kontenery, drzewa,
    latarnie), LOD i culling (wnętrza tylko przy wejściu), cienie (odległość), rozdzielczość renderu; cel stałe 30 fps
    (lepiej 45–60) na średniej. Log FPS z ujęć CI (min/śr) w trasach testowych.
45. WEJŚCIE NA SIŁOWNIĘ (BRAXHORN GYM, szara ściana na końcu ul. Portowej) PREMIUM: prawdziwy szyld 3D (litery
    przestrzenne podświetlane, logo), zadaszenie/portal wejścia, oświetlenie, schody/rampa, drzwi szklane z ramą, MURAL na
    szarej ścianie z obrazka właściciela (attachments 77d3187b-image.png — „BRAXHORN” + 2 postacie; napis „STREET FIGHTER”
    do decyzji — to nazwa cudzej gry, zasada „bez marek”), z oświetleniem muralu. Kosze/kartony luzem przy wejściu → porządek.
46. MIESZKANIE BRAXA w mieście — sprawdzić, czy jest (nad siłownią / przy NOVA, drzwi „KLATKA” obok NOVA). Wejście
    „KLATKA” (drewniane drzwi wklejone w czarną elewację NOVA) się nie nadaje → porządna brama klatki schodowej
    (domofon, numer, daszek, oświetlenie), klatka schodowa, mieszkanie premium Braxa u góry.
47. NOVA Z ZEWNĄTRZ — „dziura”: niska czarna bryła wciśnięta między kamienice z dziurą/wnęką nad nią (ciemne szkło),
    podwójny napis (NOVA DYSKOTEKA + CLUB NOVA), naklejone banery BRAXHORN NUTRITION i plakaty Braxa na elewacji klubu
    → ZWALIĆ i zbudować od nowa: porządny budynek dyskoteki na całą wysokość pierzei (fasada klubu premium, jeden szyld
    neonowy, wejście z kolejką i bramkarzem), mieszkanie Braxa w kondygnacjach nad klubem — „ma być petarda”.
    Plakaty/banery tylko w ramach i na właściwych miejscach (pkt 48).
48. BILLBOARDY / PLAKATY od właściciela (dośle grafiki): rozmieścić w DOBRYCH miejscach — nośniki z ramą (billboard
    wolnostojący 12 m² na słupie przy dużej ulicy, citylight na przystankach, ramy na ślepych ścianach kamienic,
    podświetlenie), NIE na szklanych wieżowcach. Grafika ekranu startowego gry (dośle). Plan rozmieszczenia do akceptacji.
49. ELEWACJE PLAMISTE: alejka przy BANKU — elewacja niebieska z brązowymi plamami (dwie tekstury/warstwy, migotanie
    albo brak tekstury) + niebieski pas u podstawy → wycięcie dolnej warstwy (BraxWycinanieSiatki) / jedna tekstura.
    Rynny kończą się w powietrzu krzywymi kolankami → rynna do chodnika z kolanem i wylewką/do studzienki.
50. SZPITAL MIEJSKI (miasto): z windy wysiadasz w otwartą halę z łóżkami — brak korytarza; niebieskie słupy, przeszklenie
    od podłogi. → przebudowa: korytarz z dyżurką pielęgniarek, sale 2–4 łóżkowe za drzwiami, łóżka szpitalne realne
    (barierki, stelaż, pościel), szafki, stojaki kroplówek, monitory, oświetlenie, oznakowanie oddziałów. Panel windy
    = UI premium (pkt 40) — teraz ucięty tekst „REHABILITA CJA”, napis „ODDZIAŁ – SALA 1” pływa nad drzwiami.
51. SZPITAL — recepcja i cały parter, POLICJA (komisariat miasta) — wyglądają tak samo źle → przebudowa premium
    (izba przyjęć z rejestracją i poczekalnią; komisariat z dyżurką za szybą, poczekalnią, korytarzem do pokoi).
52. GOLASY: kobieta naga przy parkingu przy wyjeździe na osiedle (zdjęcie 00:19) — pkt 19 (zero poza plażą).
53. Film 9715 (23:16): skuter magenta (C1), wieża kratowa w tle na morzu — sprawdzić, co to (latarnia/maszt?) i
    czy pasuje; nic poza tym.
48b. (01:01) Właściciel: billboardów NIE za dużo. WSZYSTKIE obecne billboardy/plakaty/banery (w tym na klubie NOVA) USUNĄĆ.
     Nowe tylko z grafik właściciela, w niewielkiej liczbie, w dobrych miejscach (pkt 48), plan do akceptacji.
54. (01:02) SALON SAMOCHODOWY — MIASTO: salon premium z drogimi autami (na start gracza nie stać — ceny z EKONOMIA),
    P1 po 01:00. OSIEDLE: buduje TERAZ wykonawca B (lokalizacja przy stacji/warsztacie).
55. GRAFIKI właściciela: scratchpad/plan/GRAFIKI_ROZMIESZCZENIE.md (ekran startowy, billboardy, mural) — po akceptacji.
56. (01:03) ROZBUDOWA MIASTA (osobny etap po poprawkach, plan do akceptacji): miasto „ma się rozrosnąć” — jedno miasto
    rozbudowane konkretnie zamiast wielu. Nowe dzielnice: zaułki/„szare uliczki” pod ustawki i walki 3×3 (podwórka,
    przejścia bramowe, zaplecza, pod wiaduktem, parking wielopoziomowy, dach), dzielnica biedna/robotnicza (stare
    kamienice, warsztaty, garaże, bazar) — miejscówki walk kozaków ligi (model z pkt 23:43). SKALA konkretnie (33, 37) —
    najpierw. Osiedle też rozbudowane (B2, gospodarstwo). Do WIZJA w projekcie.
- (01:18) DO WIZJA (później): gra Braxem i Nyxą NA PRZEMIAN wg HARMONOGRAMU WALK — kalendarz: Brax ma walkę danego dnia →
  przygotowanie; po niej Nyxa wie, że jej walka następna → przygotowuje się wcześniej; między walkami życie i treningi.
  Szczegóły później. TERAZ: budujemy miasto i osiedle potężne.
57. (01:19) ARENA GŁÓWNA (wyspa; każda arena główna) — „KOZAK”: wielka, PEŁNA publiczności, premium. Trybuny z prawdziwymi
    siedziskami (model „Low poly stadium/sports arena seats” anDDDres CC-BY — właściciel ściągnie; instancjonowane),
    publiczność: bliskie rzędy 3D z animacjami (Cheering/Clapping z Mixamo), dalsze rzędy impostory; ring i oktagon
    PREMIUM w kodzie (liny z owijką, narożniki z poduszkami, fartuch z logo, mata z logo, schodki, stołki narożników,
    stolik sędziów, kamery, telebim nad ringiem, światła kratownicy, dym/reflektory shaderem), oktagon z modelu
    wesamtufail (pobierany) albo w kodzie. Boczne wejście zawodników + tunel wejścia z efektami. Szatnie z sufitami.
    WALKI w 2.5D (właściciel + prowadzący): modele 3D, ruch w płaszczyźnie, kamera z boku dynamiczna; ustawki 3×3 =
    2.5D z głębią (beat 'em up). Arena zbudowana pod kamerę 2.5D (tło publiczności w kadrze).
58. (01:21) SCHODY RUCHOME: METRO (chodnik → antresola i antresola → peron: schody ruchome obok stałych) + SALON
    SAMOCHODOWY PIĘTROWY (osiedle). Model do pobrania: „Metaretail: Escalator” (vmmaniac, CC-BY, 8k, z animacją) —
    właściciel ściągnie; do tego czasu w kodzie: stopnie z grzebieniami i żółtymi krawędziami przesuwane (animacja
    stopni / przewijanie UV), poręcz gumowa przewijana UV, balustrada szklana, osłony boczne inox; jazda gracza i ludzi
    (platforma przenosi postać), kierunek góra/dół, dźwięk napędu.
- (01:29) DO POSTACIE (później): ekran startowy z CAŁĄ EKIPĄ (Brax i Nyxa z pasami z przodu, mistrzowie, kozacy) po
  wygenerowaniu postaci — ten sam układ (logo lewy górny, START/USTAWIENIA lewy dół). W menu GALERIA ZAWODNIKÓW: karty
  rywali (sylwetka „?” do spotkania, odblokowanie po walce, statystyki), półka pasów (kto trzyma), ukryci = „?” + podpowiedź.
- (01:33) DO POSTACIE: zawodnicy-zwierzęta ŻYJĄ w świecie — plan dnia (rano trening, w dzień praca w SWOIM miejscu, wieczorem
  bar/klub), własne biznesy i miejsca: szkółka walki, warsztat, siłownia, bar, ochrona, sklep; spotykani na mieście i osiedlu
  (rozmowa, zaczepka, wyzwanie, podpowiedź o innym zawodniku), relacje między nimi (ekipy, rywalizacje). Mistrz rejonu ma
  swoją „siedzibę”. Ukryci — tylko w swoich warunkach.
  Każdy zawodnik = JEDEN egzemplarz w całym świecie (bez duplikatów, nie jako zwykły przechodzień); mijasz go na mieście
  wg jego planu dnia. Kontrola runtime: rejestr postaci unikalnych (spawn tylko jeśli nie istnieje), log BRAXHORN_POSTACIE_DUPLIKATY=0.
- (01:35) DO POSTACIE/ROZBUDOWA: KLUBY zamiast siłowni dla każdego (~9): osiedle — siłownia A (ekipa uliczna), sala bokserska
  (buldog, szkółka); wille — klub fitness z dojo karate, stadnina z ringiem; miasto — BRAXHORN GYM (Brax), BRAXHORN FITNESS
  (NYXA = WŁAŚCICIELKA, baza przy grze Nyxą, liga damska trenuje), piwnica bokserska, obóz Muay Thai przy porcie, klub MMA
  w centrum. BRAXHORN NUTRITION + shake bar = wspólny biznes Braxa i Nyxy. Mistrz rejonu — siedziba w swoim klubie.
- (01:35) KOREKTA: Brax i Nyxa STARTUJĄ BIEDNI — małe mieszkanko nad siłownią + stara BRAXHORN GYM (jedyne źródło dochodu).
  Nyxa na starcie trenuje i pomaga w siłowni; BRAXHORN FITNESS = jej cel (otwiera za zarobione), NUTRITION + shake bar =
  wspólny cel do kupienia; rozwój siłowni (sprzęt, remont, klienci, karnety); dalej mieszkanie, auto, łódź, willa mistrza.
  (do WIZJA/EKONOMIA)
59. (01:36) BRAXHORN FITNESS NYXY — OD RAZU (od startu gry, skromny, typowy fitness dla kobiet; jej źródło dochodu jak GYM
    Braxa): w mieście blisko BRAXHORN GYM (ul. Portowa / sąsiedni lokal parteru — wybrać wolny lokal, rzut do akceptacji):
    recepcja z ladą i NPC, strefa cardio (bieżnie — modele treadmill z paczki miasto_silownia_metro), sala zajęć z lustrami,
    matami, piłkami, hantelkami, step, drążki/TRX, szatnia damska, kolorystyka biel/róż/czerń, MURAL z grafiki właściciela
    (billboardy/02.jpg „BRAXHORN FITNESS” z Nyxą — paczka grafiki), szyld 3D na elewacji, Nyxa i 3–4 klientki ćwiczą.
    Jedno wejście, detale, × K. Ujęcia DETAL_fitness_*.
- (01:37) DO EKONOMIA: NIERUCHOMOŚCI — wiele mieszkań naraz w różnych miejscach (miasto, osiedle, wille, później miasto
  mistrzów); kupno lepszego bez sprzedaży starego; sprzedaż opcjonalna; przeprowadzki w obie strony; każde mieszkanie =
  punkt zapisu/odpoczynku i szybkiej podróży; wyposażenie i ulepszanie mieszkań.
- (01:38) PATRYK i MONIKA (właściciel + Monika, modele prywatne — tylko zaszyfrowane) = AGENCI NIERUCHOMOŚCI i WŁAŚCICIELE
  SALONÓW SAMOCHODOWYCH w całej grze (salon osiedle, salon miasto, biuro nieruchomości). Do EKONOMIA/POSTACIE.
60. (01:40) LINIA METRA z sensem (rozbudowa, po tym buildzie): M1 — NABRZEŻE (istniejąca) ↔ 2–3 nowe stacje w innych
    częściach miasta (drugi koniec miasta, centrum biurowe/wieżowce, dzielnica robotnicza; później przystanek przy osiedlu).
    Każda stacja: jedno wejście, schody ruchome, antresola z bramkami (bilet — EKONOMIA), peron (kod stacji M1
    sparametryzowany, każda z innym kolorem okładziny i nazwą). Jazda: wsiadasz do pociągu, drzwi się zamykają, przejazd
    tunelem z widokiem z wagonu (20–30 s, z możliwością pominięcia), wysiadasz na wybranej stacji = szybka podróż.
    Pociągi kursują między stacjami (rozkład), na MAPIE linia i stacje.
    Jazda (właściciel 01:40): „prowizorka” — wnętrze wagonu stoi, za oknami zapętlony krótki segment tunelu (przewijanie,
    lampy co kilka m, bujanie wagonu, dźwięk), 10–15 s, hamowanie → peron docelowy, wysiadasz. Bez tunelu przez miasto.
    Kolejne stacje = kopia stacji M1 (ten sam kod, inne miejsce/nazwa/kolor).
- (01:42) DO POSTACIE: w walkach także LUDZIE — ale jako SPECJALNE postacie (osobne dopracowane modele z charakterem,
  generowane jak zwierzęta), NIE przechodnie. Propozycja: po 2 ludzi na rejon (M + K). Przechodnie zostają przechodniami.
- (01:48) DO POSTACIE: NINA (model właściciela, paczki nina_1..3 + ruchy capoeira w nina.zip) = zawodniczka-CZŁOWIEK, liga
  damska, styl CAPOEIRA (propozycja: kozaczka w mieście, szkółka capoeiry w dzielnicy robotniczej/na plaży). Właściciel ma
  też drugiego człowieka (do przysłania) — odpowiednik w lidze męskiej. Ruchy z innej gry → retarget Humanoid (Unity).
  Monika ≠ Nina (Monika — model dośle właściciel).
- (01:51) ZASADA STYLU POSTACI (właściciel): KAŻDA postać-zwierzę = LUDZKA budowa ciała (anatomia sportowca, mięśnie) +
  głowa i skóra/futro zwierzęcia. Miś też (grubsze futro na karku/plecach/przedramionach, największy; kimono z czarnym pasem
  jako sensei obozu). Obecny „puchaty” Miś do ponownej generacji. (do POSTACIE)
- (01:52) CROW = LEW (istniejąca postać właściciela). Nie dublować lwa. Mistrz willi — propozycja: BIAŁY TYGRYS (albo czarna
  pantera); mistrz świata — srebrnogrzbiety goryl; Miś = sensei obozu (wyjątek „puchaty” do decyzji). Ekipy 3×3: kozak +
  2 ludzi z ekipy (8 modeli ludzi, kolory ekip w grze); ekipa Braxa: Brax + 2 z siłowni (Dany, Nina / Crow, Vexa).
- (01:55) DO POSTACIE: każdy zawodnik-szef RZĄDZI SWOJĄ DZIELNICĄ, ma ekipę (koledzy/przydupasy — ludzie, 12 modeli ludzi
  + kolory ekip) wysyłaną do walk/ustawek; pokonanie ekipy → szef wychodzi; wygrana = szacunek w dzielnicy (ekipa nie
  zaczepia, ktoś może dołączyć do ekipy gracza w 3×3).
- (01:56) DO WIZJA: MIASTO = pierwsze i GŁÓWNE miejsce gry (start, baza Braxa i Nyxy), WIELKIE, miejsce, do którego się
  WRACA (obrony pasów, biznesy, mieszkania). OSIEDLE = etap PRZEJŚCIOWY w drodze do Ligi Mistrzów Świata. Priorytet
  rozbudowy: miasto.
- (01:59) DO POSTACIE — STROJE: model Tripo = ciało+ubranie w jednym (nie przebieramy; tylko dodatki do kości i
  przemalowanie tekstury). Zawodnik w stroju walki NIE chodzi po mieście — tylko klub/sala/szatnia/arena. Po mieście
  tylko postacie z WERSJĄ ULICZNĄ (mistrzowie + wybrani kozacy). Ekipy uliczne generowane od razu w ciuchach ulicznych.
  Nowe postacie: OWIJKI zamiast rękawic (rękawice = dodatek w grze do walki). Młody bokser w rękawicach — tylko sala
  bokserska i liga. Ludzie (6 + Nina + Dany) walczą w ligach obok zwierząt; docelowo 16–18 ludzi.
61. (02:29) POŁĄCZENIE REGIONÓW (etap po poprawkach): jedna aplikacja, regiony jako osobne sceny/paczki (Play Asset Delivery /
    Addressables), w pamięci jeden region. Przejazd: miasto → autostrada (scena 1–2 km) → TUNEL (async: zwolnij miasto, wczytaj
    osiedle; za wolno → korek/dłuższy tunel) → osiedle. Tak samo osiedle → obóz → miasto mistrzów (lub samolot/statek).
    Tunel w mieście = opcja A (wyjazd na autostradę); do czasu połączenia komunikat. Port: bosman i NPC pływający łodziami — etap ekonomii.
    (02:30) Autostrada: krajobraz wzdłuż w 3D (panoramy 3D jak horyzont miasta, nie obrazki) — „fajna zajawka”.
62. (02:30) KOMIS UŻYWANYCH AUT w mieście (obok salonu premium, na który Braxa na start nie stać): plac z autami używanymi
    (różne stany, ceny tańsze — EKONOMIA), budka/biuro handlarza, tablice cen, flagi/girlandy, myjka; pierwsze auto Braxa
    kupuje się tu. Prowadzi: Patryk i Monika (biznes) albo handlarz NPC — do decyzji właściciela.
    (02:31) Autostrada: pasy ruchu + pas awaryjny, bariery energochłonne, ekrany akustyczne, znaki i tablice kierunkowe, zjazdy
    (na stację/MOP — dekoracyjne albo zamknięte), POLICJA na autostradzie pilnuje porządku (radiowóz, kontrola prędkości,
    zatrzymanie za wykroczenie — mandat, EKONOMIA), ruch aut AI w obu kierunkach.
    (02:31) Komis prowadzą PATRYK i MONIKA — rozdzieleni po biznesach (np. w mieście: jedno w komisie, drugie w biurze
    dewelopera/salonie; na osiedlu podobnie: salon / biuro nieruchomości), ale MIESZKAJĄ RAZEM (wspólne mieszkanie/dom —
    wieczorem wracają). Każde jako jeden egzemplarz w świecie (plan dnia: praca w jednym miejscu → dom).
    (02:32) DECYZJA: MIASTO — PATRYK prowadzi KOMIS używanych aut, MONIKA — biuro DEWELOPERSKIE (mieszkania/nieruchomości);
    SALON premium w mieście prowadzi ktoś inny (NPC). OSIEDLE — inaczej: Patryk salon samochodowy (jak teraz), Monika biuro
    nieruchomości (do ustawienia). Mieszkają razem.
- (02:40) Przy finalizacji: w docs/zlecenia/ZLECENIE_GPT_20261103.md §3 MIASTO pkt 1 = WYSPA (drzewa zamiast stożków z modeli
  miasta, toalety areny, pełny audyt BRAXHORN_WYSPA_KOMPLET i uzupełnienie braków, arena pkt 57) — przed skalą i klockami.
- (02:42) OSIEDLE: autobus zawsze pełny 12 m × K; jeśli log BRAXHORN_AUTOBUS DO POPRAWKI — POSZERZYĆ ŁUKI krawężników na trasie
  autobusu A (BraxClaudeOsiedleDrogiPoszerzenie.cs / łuki E3b) — dopisać do zlecenia GPT (osiedle pkt 1).
- (02:42) AUTOBUS — GRACZ WSIADA: na przystanku, gdy autobus stoi z otwartymi drzwiami → kontekst „WSIĄDŹ” (bilet — EKONOMIA,
  teraz 0 zł), Brax siada w środku (jak w aucie: seat), kamera za plecami/wewnątrz, jazda trasą, na każdym przystanku
  „WYSIĄDŹ”. NPC wsiadają i wysiadają jak teraz. Osiedle i miasto. (zlecenie GPT, osiedle pkt 1)

## 3.11 rano (10:00–10:50) — nagrania i zdjęcia właściciela z APK 57c1767 (build 23:22) — STATUS
Zrobione w kodzie (gałąź miasta, niewysłane do 3.11 ~10:50, niesprawdzone na obrazie):
- ✅ metro: wejście blokowało pudełko kontroli kolizji (8372b2b)
- ✅ nowe dzielnice: czarne ściany/okna (szkło jako czarny metal, szyby przed pustą bryłą) → szkło refleksyjne (7487db9);
  gołe bryły z daleka → budynki stale w scenie, sylwetki wyłączone; sylwetki CENTRUM szkło (021a063)
- ✅ wyspa: arena z brył wyłączona (+ plac, food trucki, toalety, kibice); palmy z kodu → drzewo z modelu (d4fdf43)
- ✅ arena premium — w toku u pomocnika (osobna gałąź claude/miasto-pomoc-ruch, do scalenia)
- ✅ kontenerowiec na redzie wyłączony (słaby model 1 MB + metal = ciemny); kontenerowiec z kodu w porcie A wyłączony (86e6dbb)
- ✅ siatki/szlabany/pachołki/bloki w poprzek każdej jezdni (231df07); znaki stojące na jezdni (be1e25b)
- ✅ marina: 5 boksów przy D + pomost F z boksami + odnoga G, jachty rufą do pomostu, trap (f4aa9b7, e3d9d53)
- ✅ skuter okrakiem, motorówka w fotelu (a657119); jacht: kamera blisko za sterem (b92872d)
- ✅ autobus: ciemne szyby modeli → szkło z odbiciem (ac1aca4); wiata przystanku przodem do jezdni (033c28c)
- ✅ migotanie elewacji: wycinanie w całym mieście z budżetem (15bd43c)
- ✅ ludzie: cofnięty adaptacyjny limit (1e550ff), 16 par rozmawiających z dymkami (33194e7), limit animowanych 52,
  18 aut w ruchu zamiast 8 (da74293)
- ✅ podwórko: wiata śmietnikowa ze środka przejścia wyłączona, piłki × K (9edc5f9); latarnie ścienne bez ściany (5845c9c)
- ✅ „Utknąłem”: cel tylko pod gołym niebem (2f8ba28)
- ✅ CI: zdjęcia w 12 częściach (było 3 → zrobiło się 57 z 218 ujęć) (d5bd95f)
Do zrobienia:
- ❌ nowe dzielnice: ruch aut (trasy przez Aleję Centralną i C0–C3) i przechodnie; rondo „kwadratowe”; prawdziwe znaki drogowe
- ❌ wyspa: drzewa różnych gatunków + prawdziwe palmy (skany Poly Haven odchudzone pod telefon), większy pomost pod jacht
  (zlecony pomocnikowi po arenie)
- ❌ czerwona cegła widoczna w oknach (szyby przezroczyste przed murem — BraxSzyby: przezroczyste tylko z wnętrzem), czerwone
  murki z cegły na dachu (klocki), lustrzana tafla na placu, migająca elewacja wieży przy placu z pawilonem (tyły B)
- ❌ skala całego świata — w nocy był tylko audyt (BRAXHORN_SKALA lista), bez poprawek
- ❌ wejście na jacht pieszo (teraz WSIĄDŹ przy trapie), miasto wszerz (właściciel: później)

## DO ZROBIENIA — zapamiętane (właściciel 3.11 12:17)
- **Aleje palm wzdłuż chodników**: „grube” palmy z paczki właściciela (palm_hp_4 → `miasto_palmy/palma.glb`, 3 LOD, 13 m × K)
  ustawić szpalerem wzdłuż chodników (w misach z trawnikiem — BraxMisyDrzew), potem ogólne przerobienie zieleni miasta.
- Kolejka na poniedziałek (3.11): rowery i skutery do jazdy + wypożyczalnia skuterów, wózek widłowy do jazdy na placu portu;
  plac portu (magazyn, budka ochrony, podmisja firmy kontenerowej); jacht z wnętrzem do zwiedzania (paczka gotowa offline:
  scratchpad/port_hd/jacht_wnetrze.glb, logo Sea Ray zamalowane — nie wdrożony); cegły zamiast szyb w blokach podwórka
  (x 26.9–63.1 / z 99.5–113.5 i x −35.7…−13.1 / z 50.9–113.5); stare billboardy i powtarzający się napis NOVA MUSIC;
  skala rekwizytów źródła miasta (1:1 → × K, z kolizjami); szersze drogi i zaokrąglone narożniki; znikający skuter wodny.
