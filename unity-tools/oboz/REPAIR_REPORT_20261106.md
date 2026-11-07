# OBÓZ TRENINGOWY — naprawy po audycie 6.11

Gałąź: `codex/oboz-przebudowa-pbr-20261105`. Punkt wyjścia: `72ae341cd927406015036603cea06051caa8cbe7`.
Zlecenie właściciela w tej rozmowie: „Dokończ te zlecenie”. Tryb: **sceny, bez APK**.
Odbiór wizualny i nagranie właściciela pozostają wymagane. Żaden brakujący model nie jest zastępowany kostką.
Właściciel doprecyzował: „Zgłoś się do mnie po animację do postaci, nie? Przed puszczeniem bulidu”. Po raporcie napraw obozu następuje kontakt po jego animacje; APK wymaga późniejszego, osobnego polecenia. Obecne kontrole sceny nie budują APK.
Następne polecenie właściciela z 7.11: „W repo są już załączone paczki animacji […] Zrób to jak skończysz to co masz w zleceniu”. Po obecnych naprawach środowiska należy wykorzystać paczki z repo do wspólnych animacji postaci obozu dla całej gry: 3 ciosy ręką, 3 nogami, uniki, powalenia, wstawanie, chód, bieg, skok i potrzebne reakcje/przejścia. **Monika wyłączona z tej zmiany; Miś: pozycja A, pozostałe objęte zadaniem postacie: pozycja T.** To rozszerzenie nie stanowi polecenia uruchomienia APK. W etapach E/F/G nie rozpoczyna się podmiany animacji.
Kolejne doprecyzowanie: „mają sparować na ringach efektownie i na workach trenować a Monika ma im rozkazywać zmiany zadać”. Po animacjach należy zintegrować zadania sparingów i treningu worków oraz zmiany stanowisk na polecenie Moniki. Monika pozostaje kierowniczką; jej model i obecne animacje nie podlegają wymianie w tym zadaniu. Polecenie dotyczy zachowań postaci, nie uruchomienia APK.
Właściciel następnie upoważnił do dobrania dalszych zadań pasujących do obozu i stworzenia rzeczywiście brakujących animacji: „Monika ma nimi zarządzać […] całą listę rozkazów […] a oni mają to wykonywać”. Zakres po środowisku: zbiórka, rozgrzewka, bieżnia, ćwiczenia siłowe, posiłek i odpoczynek obok worków/sparingów. Polecenia muszą sterować rzeczywistym dojściem, zajęciem stanowiska i zakończeniem czynności; nazwa zadania w UI nie jest dowodem jego wykonania. Najpierw wykorzystać dostarczone klipy, tworzyć tylko brakujące ruchy na właściwym szkielecie.

## A-01 — rzeczywista skala, podparcie, postacie i wiarygodny pomiar

- Wymiary modeli są mierzone w odpowiednim układzie lokalnym. Obrót domku o ±90° nie zamienia długości łóżka z szerokością.
- Collidery wyposażenia korzystają z lokalnego obrysu kompletnego modelu po ustawieniu.
- Drzwi domków: skrzydło 0,90 × 2,10 m przed K, dopasowane futryny, bale przy otworze i collidery ościeża.
- Oryginalne części kuchni są kopiowane raz, z zachowaniem hierarchii i transformacji źródła. Stół z kuchenką ma wysokość gotowania 0,90 m × K; stoły jadalni 0,75 m × K. Kuchenka i talerz mają rzeczywistą podporę wyznaczoną raycastem.
- Misa zlewu ma wysokość 0,85 m, dolna krawędź pisuaru 0,55 m, środek lustra 1,60 m ponad zmierzoną podłogą. Tyły wyposażenia i półka są ustawiane na faktycznych ścianach. Oryginalne krany pozostają częścią GLB.
- Monika i sześciu NPC mają wspólny pomiar geometrii podeszew. Korekta zachowuje początkowy pivot i wygasa względem niego podczas skoku. Nie zmieniono modelu, szkieletu ani animacji źródłowych. Poślizg w ruchu nadal wymaga przebiegu sceny/Play Mode.
- Audyt zapisuje całe modele, podparcie ścienne, wysokości montażu, postacie, materiały i elementy niezmierzone. Nie uznaje samej nazwy `Monika`, `NPC` lub `półka` za dowód poprawnego kontaktu.
- Byk pozostaje w swojej powiększonej skali. Kod `RondoBull` i przekształcenie `NativeMetres` nie zmieniają skali. Audyt porównuje faktyczny obrys z niezależnie zmierzonym wzorcem oryginału 4× × K: **2,434245 × 3,904765 × 6,201168 jednostek świata**.
- Kontrola sceny pobiera `.gitignore` i zależności testów, sprawdza dokładny `expected_sha`, kończy edytor po synchronicznej metodzie i zapisuje pełne komunikaty generatora. Nie wywołuje kompilacji APK.

### Weryfikacja A-01

- Testy Python źródeł są wykonywane z `unity-tools/oboz`; wynik przed publikacją zostaje zweryfikowany na dokładnym indeksie tego etapu.
- Sprawdzenie składni i zakresów C# nie zastępuje kompilacji Unity.
- Do przebiegu Unity dodano 17 testów rzeczywistego zachowania: osie modelu przy czterech obrotach i dwóch skalach rodzica, kontakt z podłożem z wykluczeniem własnych colliderów oraz osiem pomiarów geometrii stóp i zachowania pivotu.
- Opublikowany commit: `005e76cde0c979cb6ffad44545e7dcf98ed09519`. Dokładny indeks przed publikacją: **214 testów źródeł + 2 testy kompilacyjnego kontraktu treningu PASS**, kontrola składni/zakresów 292 plików C# bez błędów. To nadal nie jest kompilacja Unity.
- GitHub test źródeł nr 58: **success**, run `42304677403`, ten sam SHA.
- Scena nr 25: run `42304823334`, job `128191396007`, ten sam SHA. Uruchomiony wyłącznie `oboz-scene-only`; `android-oboz` pominięty. W chwili przygotowania C trwa krok Unity; wynik i liczba ostrzeżeń zostaną dopisane z rzeczywistego raportu.
- APK: **nie budowano**. Telefon/nagranie: **nie zweryfikowano**.

## C-01 i domknięcie A-02 — powierzchnie, dojścia, stopy w ruchu

- Ścieżki do stołówki, magazynu i punktu medycznego kierują się do rzeczywistych południowych wejść. Dodano dojście do sauny. Łaźnia otrzymała dojście wokół wschodniej ściany do północnego wejścia; poprzednia odnoga przecinała jej wnętrze. Dodano połączenie siłowni z siecią. Pozycje budynków pozostają zachowane.
- Dziesięć łagodnych podejść ma zgodną powierzchnię widoczną i kolizyjną: cztery ganki, cztery budynki usługowe, łaźnia i siłownia. Test planu sprawdza połączenie, fronty i brak przecięcia wnętrz; audyt zapisanej sceny sprawdza każde podejście 33 promieniami.
- Podkład stanowiska kończy się na 4 cm; mata ma 4 cm grubości i górną powierzchnię na 8 cm. Przy pełnym ugięciu 3 cm pozostaje 1 cm ponad podkładem. Poprzednia mata miała 1 cm grubości i chowała się w podkładzie.
- Deformacja korzysta z rzeczywistych podeszew gracza i NPC, promienia 0,4 m oraz limitu 3 cm przed K. Kontakt stóp odczytuje wysokość widocznych zdeformowanych trójkątów; fizyczna kapsuła nadal korzysta ze stabilnej kolizji.
- Ślady: zachowane referencje po wczytaniu sceny, wspólna pula 40 obiektów, wspólna siatka i materiał, zanikanie przez 20 sekund. Najbliższe podłoże i maska mieszanki rozstrzygają, czy trafiono w błoto/piasek; ślady nie są rysowane przez chodnik na grunt pod spodem.
- Oryginalna trawa Grass Medium 01/02 zachowuje siatki i tekstury. Obie klasy instancji mają zasięg 25 jednostek świata; shader wygasza widoczną geometrię między 23 a 25 m, również na obrzeżach całych grup renderowania. Dalsze podłoże korzysta z istniejącej tekstury trawy. Nie zmieniono wspólnego renderera innych rejonów.
- Maska oryginalnego `mud_forest_2k` obejmuje także rzeczywistą lokalizację Ringu 2 i worków pod pawilonem. Audyt odczytuje rzeczywiste mapy i kolory wierzchołków zapisanej sceny.
- Zgodnie z bieżącą tabelą A.1 „człowiek 1,8” manifest ustawia 1,8 m przed K dla siedmiu postaci. Stare 1,68 m Moniki i prowizoryczne deklaracje NPC nie stanowią wyjątku od tego zlecenia. Źródłowe modele, szkielety i klipy nie są modyfikowane. **Wyraźny wyjątek właściciela dotyczy byka; jego 4× pozostaje zachowane.**
- Walk/Run: edytor mierzy prędkość podporowej stopy w oryginalnych klipach na jednorazowej kopii postaci. Komponent obozu dopasowuje odtwarzanie do rzeczywistego przesunięcia kapsuły. Pauza, skoki, walka i przejścia animacji pozostają wyłączone z tego dopasowania. Dane trafiają do `audit/oboz-krok-postaci.json`. Nie deklarujemy jeszcze wyeliminowania poślizgu na nagraniu.

### Dowody C-01 / A-02

- Rozszerzono rzeczywistą regresję Unity o 18 kontroli serializacji śladów, puli, zanikania, geometrii i interpolacji maty oraz pomiaru klipu o znanej prędkości. Razem z A jest 35 kontroli; licznik raportu wynika z wykonanych kontroli, nie z wpisanej obietnicy.
- Weryfikacja przed publikacją C: **226 testów źródeł + 2 testy kontraktu treningu PASS**, C# **35 plików Editor i 17 runtime, 0 błędów składni/zakresów**; `git diff --check` bez uwag.
- Lokalnie dostępne są testy źródeł i analiza C#. **Unity i fizyka sceny wymagają przebiegu C na opublikowanym SHA.** Nagranie telefonu i FPS pozostają niezweryfikowane; aktualnego rozmiaru APK nie ma.

## C-02 / A-03 — błędy wykryte w scenie i zachowanie importów

- C-01 opublikowano jako `03662ea8eee8ee212b40bf6f5451f75888360a5f`; test źródeł GitHub run `42307210061` zakończył się wynikiem **success**.
- Przebieg A-01 nr 25 zakończył generowanie i zapis sceny (**workflow success**, bez APK). Raport wykazał **54 ostrzeżenia, 10 komunikatów błędów, brak fatalnego wyjątku**. Kompilacja C# przeszła; zielony workflow nie jest potwierdzeniem jakości.
- Pierwszy błąd: `Assertion failed on expression: ShouldRunBehaviour()` w `BraxObozFootContactRegression.Run`, wywołanie `SendMessage("OnDisable")` w EditMode. Dwa takie wywołania dały dwie nieudane kontrole. Pozostałe osiem błędów to `Mesh.uv is out of bounds` w `CabinFinish`: zmiana liczby wierzchołków unieważniała UV, zanim skopiowano ich drugą stronę. Naprawa przechowuje oba pełne zestawy danych przed przypisaniem do Mesh.
- Pomiary A: 6094 renderery, 89 pełnych obiektów z deklaracją podparcia, 7 postaci, 48 worków. Postacie: **7/7 kontaktów stóp bez ostrzeżeń**. Byk: **4× zachowane**, zmierzony światowy obrys 2,434245 × 3,904765 × 6,201168.
- Pełne obiekty wymagające poprawy kontaktu: taca wisiała 35,18 cm nad podporą, sztućce przenikały jej zastępczy collider o 8,75 cm. W D są zastępowane kontaktem oryginalnych powierzchni.
- Surowe wskazania per-renderer: **3153 lewitujące, 480 zapadniętych, 65 bez gruntu**. Ten licznik zawiera m.in. osobne ogniwa zawieszonych worków, słupki/łaty połączonej konstrukcji i widoki LOD drzew. Nie jest liczbą źle postawionych mebli. Pełna lista pozostaje w artefakcie; wymaga odrębnego sprawdzenia geometrii i podparcia zespołów, bez automatycznego uznania ich za poprawne.
- Murale: 3/4 widoczne; dostarczone źródła 1536×864, co nie spełnia minimum 4096 px. Łóżka IDANAS: długość 2 m, obrys szerokości 1,220 m zamiast 0,9 m — trwa weryfikacja oryginalnej geometrii, bez ściskania modelu. Siedem worków miało niezgodny odstęp od lokalnej nawierzchni; cały zespół worek/łańcuch otrzymuje pomiar korpusu w E.
- Log potwierdził brak katalogu Library, czyli pełny import źródeł. Zmiana C-02 przywraca wykorzystanie zgodnego cache.
- Tryb sceny odczytuje istniejącą zgodną, zaszyfrowaną kopię trwałą, jeśli Actions nie ma wpisu. Po udanym przebiegu zapisuje osobny zaszyfrowany wpis importów dla dokładnego SHA. Nie dodaje kroku APK ani usuwania starych kopii.
- Profil cache i jego integralność nadal weryfikuje istniejący mechanizm projektu. Błąd cache pozostaje ostrzeżeniem; nie zastępuje kompilacji ani audytu sceny.

## D-01 — budynki, wyposażenie i fizyczny montaż oznaczeń

- Zachowano pozycje czterech domków, bramy, ronda, zatoki oraz bieżni. Według najnowszego zgłoszenia właściciela „koło bramy szły pasy jakieś niepotrzebne” usunięto dodatkowe pasy wzdłuż dojazdu, zebrę na asfalcie i namalowane przejście przez tartan. Oryginalne oznaczenia sportowe bieżni zostają.
- Szyld używa oryginalnego JPEG i jego przestrzennego konturu. Dolna krawędź widocznej bryły opiera się raycastem na belce z bala o długości 9,4 m i średnicy 58 cm. Dwa słupy mają średnicę 68 cm. Wysokość belki uwzględnia zmierzoną wysokość oryginalnego autobusu, nawierzchnię i 30 cm zapasu; nie zmniejsza autobusu ani byka.
- Cztery murale są montowane po powstaniu budynków i ich colliderów. Oryginalna płaszczyzna ma poprawiony kierunek przedniej strony, zachowane proporcje, odstęp 5 mm od zmierzonej ściany. Scalanie konstrukcji nie usuwa już renderera DROGA MISTRZA.
- Wszystkie istniejące wolnostojące tablice mają deski i pełne słupy z bali trafiające w rzeczywisty grunt. Tekst przylega do panelu. Nie przywrócono zbędnych znaków ani pasów przy bramie.
- Ręcznik na każdym ganku pochodzi z dostarczonego `towel-rail-with-towel.glb`, wyłącznie węzeł `Bath towel`, bez metalowego wieszaka. Linka przebiega po boku ganku między dwoma pełnymi słupami. Pomiar podwieszenia korzysta z rzeczywistego wierzchołka fałdu i collidera linki.
- Stołówka ma jeden komplet czterech stołów z desek (75 cm) oraz ośmiu ław (45 cm), 24 miejsca. Tace, sztućce i talerze pochodzą z oryginalnych modeli. Kontakt naczyń korzysta z faktycznych siatek i promieni pod obrysem, nie pełnego pudełka tacy. Usunięto podwójne komplety stołów.
- Wyposażenie kuchni korzysta z osobnych oryginalnych części; kuchenki i garnki mają własne powierzchnie podparcia. Przedmioty na stołach i półkach są rozmieszczone oddzielnie. Model witryny zachowuje oryginalne proporcje.
- Cztery modele budynków usługowych są generowane z istniejącego źródła konstrukcji, z jawnymi wymiarami i metadanymi. Drzwi mają 0,90 × 2,10 m, podłoga 0,16 m. W magazynie pozostają prawdziwe półki z desek; atrapy skrzyń usunięto. Punkt medyczny korzysta z oryginalnych IDANAS i metalowej szafy, bez pudełek udających łóżka i apteczkę. Brakujące lampy, skrzynie, piec i balia trafiają do H.
- Lustra łaźni mają sondy wypalane po całej geometrii i oświetleniu, przed zapisem sceny. Kod potwierdza utworzenie tekstury odbicia; brak urządzenia graficznego lub nieudany bake jest jawnie zgłaszany. Wynik tego pomiaru Unity jeszcze oczekuje.
- Oryginalny IDANAS zweryfikowano z pojedynczego dostarczonego GLB (SHA256 `26efbc9f32157c909d1cd9bde9b1692e4734728a0dcde7466739fe2ce5ff449a`). 1,220 m to obrys zwisającego koca; materac po jednolitej kalibracji długości ma szerokość 0,895771 m. Zachowano geometrię i proporcje. Jego powierzchnia ma wysokość 0,483092 m, czyli 3,31 cm ponad wymóg 0,45 m; rozbieżność pozostaje w raporcie, bez deformowania oryginału.

### Dowody D-01

- **235 testów źródeł + 2 testy kontraktu treningu PASS**; kontrola C# 36 Editor + 17 runtime, 0 błędów składni/zakresów. `git diff --check` bez uwag. To nie zastępuje kompilacji Unity.
- Zmierzone wierzchołki czterech wygenerowanych GLB: **24 kontrole wymiarów PASS** w lokalnym Blenderze 4.3.2. Wersja Blendera CI pozostaje 4.5.15; Unity pozostaje 6000.0.58f2.
- Do regresji Unity dołączono pomiar importowanej jadalni i oryginalnej tacy/sztućców. Zaczepy ręczników, podstawy tablic i montaż szyldu będą sprawdzane z zapisanej sceny.
- Trwa osobny przebieg C-02 / A-03 nr 26, run `42310565856`, dokładny SHA `0173cb8cdd6dce3916ce7c2b019799dd874ab63d`. Etap D nie zostanie uruchomiony równolegle z nim.
- D: Unity/nagranie **oczekują**; APK **nie budowano**. Żadnego wizualnego odbioru ani zera lewitacji nie zadeklarowano.

## C-03 / D-02 — odczyt wyniku C i poprawa testów referencji

- C nr 26 zakończył się **success**, bez APK. Generator: **54 ostrzeżenia, 0 błędów, brak fatalnego wyjątku**, scena zapisana. Poprawka UV ośmiu szczytów domków potwierdzona w rzeczywistym przebiegu.
- Kontrole zachowania: **32 PASS, 3 FAIL**. Dwa niepowodzenia przywracania pivotu i jedno referencji śladów wynikają z użycia `EditorJsonUtility` w teście obiektów bieżącej sceny. To API zapisuje referencje zasobów, pomija referencje do obiektów sceny. W fixtures zastosowano `JsonUtility`, które używa InstanceID w tej samej sesji, i dodano ścisłe kontrole `body`, `visual`, `clips` oraz początkowego pivotu. Nie zmieniono kodu runtime, tolerancji ani wysokości stóp. Nie jest to dowód przetrwania restartu Unity.
- Podstawa API: [Unity 6000.0 EditorJsonUtility.ToJson](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityeditor/editorjsonutility/tojson) i [JsonUtility.ToJson](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/jsonutility/tojson). Naprawione kontrole zostaną wykonane w następnym, pojedynczym przebiegu D.
- Pomiary C: 6104 renderery, surowe wskazania 3129 lewitujących / 488 zapadniętych / 65 bez gruntu, 3 z 4 murali, 8 ostrzeżeń skali. Są to pomiary SHA C sprzed opublikowanych zmian D. Pełnych obiektów 72 gruntowe + 17 ściennych; 7/7 postaci w tolerancji kontaktu. **Byk zachowuje dokładnie wcześniejszą skalę 4×**, bez zmiany któregokolwiek wymiaru wzorca.
- Grunt: mieszanka błota i wszystkie podejścia zmierzone bez ostrzeżeń. Szczegóły oraz SHA256 artefaktu zapisano w `EVIDENCE/C-02-unity-scene-42310565856.json`.
- Dokładny indeks C-03: **236 testów źródeł + 2 kontraktu treningu PASS**; kontrola zakresów C# 36 Editor + 17 runtime, 0 błędów. Unity poprawionych trzech kontroli oczekuje na przebieg D.
- D-01 opublikowano jako `87a31b04346f8844e453af79211f83ebafd2765a`. C-03 jest małą korektą po wyniku C, przed uruchomieniem D; E/F pozostają oddzielne.

### Rzeczywisty wynik D-02 — scena nr 27

- Opublikowany SHA: `6d1e98be492b925205f1201f8b999bfafa90dd41`. [Przebieg nr 27](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42314590358), job `128224000092`, zakończony 7.11 o 00:19:51 UTC: **success**, tylko scena; `android-oboz` pominięty.
- **67/67 regresji Unity PASS**. Generator zapisał scenę i zgłosił **59 ostrzeżeń, 0 błędów, brak fatalnego wyjątku**. Pełny log importu przed rozpoczęciem diagnostyki generatora zawiera jednak **dwa błędy `Rig Error: Required human bone 'Hips' not found`** dla statycznego FBX przystanku i siłowni. Nie jest to wynik „zero błędów całego przebiegu”. Przyczyna: wspólny importer traktował te FBX jak postacie. E ustawia dla statycznych `OwnerAssets` `None`/`NoAvatar`, bez zmian importu postaci, i dodaje kontrolę rzeczywistych importerów oraz braku podzasobów Avatar.
- Szyld ma zmierzone oparcie na belce, szczelina **0**. **4/4 murale** mają poprawne odsadzenie od ścian około 5 mm i zachowane proporcje. Cztery ostrzeżenia dotyczą wyłącznie niewystarczającej rozdzielczości źródeł 1536×864. Nie powiększano ich sztucznie.
- **36/36 elementów zastawy** (6 tac, 6 talerzy, 24 komplety sztućców) ma prawidłowy kontakt z powierzchnią. Dwie sondy luster wypalone i przypisane: 256 px, pliki EXR 569 922 B i 548 110 B. **7/7 postaci** ma kontakt stóp w tolerancji; wszystkie 10 podejść jest ciągłych w 33/33 próbkach. To nie jest ocena animacji na telefonie.
- **Byk 4× zachowany**: obrys świata 2,434245 × 3,904765 × 6,201168. Ten przebieg korzysta z oryginału 1× przeskalowanego dokładnie czterokrotnie; nie wczytał osobnego pliku o nazwie 4X. Pomiar potwierdza docelową wielkość, niezależnie od nazwy źródła.
- Do dalszej naprawy: **7/48 worków** miało niezgodną odległość od nawierzchni; E przebudowuje ich kalibrację względem skórzanego korpusu. **32/90 tras NavMesh** są częściowe przy starych schodach Ringu 1; E wprowadza sześć stopni o mniejszym wzniosie. Cztery drzwi usługowe mają niezmierzone zawieszenie. Dziesięć surowych podejrzeń zablokowania wejść wymaga pomiaru w środku otworu zamiast przy osi zawiasu; surowe wyniki pozostają w raporcie.
- Surowe renderery: **6201**, wskazania **3093 lewitujące / 490 zapadniętych / 65 bez gruntu**. Pełne obiekty: 150 gruntowych, 35 ściennych, 4 niezmierzone. Nie są to jeszcze zerowe wyniki odbioru: konstrukcje i zawieszenia otrzymują w E osobny pomiar rzeczywistych powierzchni. Osiem ostrzeżeń IDANAS dotyczy niezmienionej wysokości materaca 0,483092 m wobec 0,45 m.
- Artefakt `13551287781`: 564 052 B, SHA256 `ffddce2ee4c170cbc68a7e776a495135482dcfeb616b93e277cbd36d6f363f5e`. Dowody i wartości z JSON zapisano w `EVIDENCE/D-02-unity-scene-42314590358.json`. Rozmiar źródeł 1 808 110 320 B **nie jest rozmiarem APK**.

## E-01 — worki, areny i rzeczywiste połączenia konstrukcji

- Worek jest kalibrowany względem faktycznego skórzanego korpusu `Low_Poly_lambert1_0`: 1,20 m przed K. Całe oryginalne zawiesie pozostaje zachowane, a dół korpusu leży 0,60 m nad podłożem trafionym osobno dla każdego worka. Dawne skalowanie całego zespołu do 2,65 m powiększało sam korpus do około 1,90 m.
- Oryginał worka: 57 części, 26 950 trójkątów, SHA256 `09aa34aea1d750f5931a07def5bd6547fa29afac4cf4e921783c093fa9190f61`. Jeden połączony mesh zachowuje wszystkie wierzchołki, UV, normalne i tangenty; 48 worków współdzieli mesh oraz materiał z instancingiem GPU. Źródłowy GLB i kod trafień, kołysania oraz dźwięku `BraxWorek` pozostają niezmienione.
- Podest każdego ringu ma deski i belki na podporach, wysokość 1 m oraz 6 m prześwitu między wewnętrznymi powierzchniami lin. Sześć stopni prowadzi z boku do otworu w linach; każdy wznios wynosi 0,2243 jednostki świata, poniżej używanego limitu NavMesh 0,30. Wynik przejścia musi potwierdzić Unity.
- Ustawianie asymetrycznych konstrukcji zachowuje ich X/Z. Ring 2 jest osadzany na rzeczywistej podłodze pawilonu 0,11 m; schody nie przesuwają środka ringu przez zmianę środka bounds. Oktagon otrzymuje osobne deski, belki, podpory i ławy z bali. Ławy ogniska mają siedzisko 0,45 m; dolne bale opałowe stoją na gruncie, górne opierają się na dolnych.
- Wyposażenie siłowni pochodzi z rzeczywistych części dostarczonego FBX. Ławka ze sztangą i stojak z obciążeniami pozostają kompletnymi autorskimi zespołami; pozostałe przyrządy są ustawiane osobno na matę. Nie jest już ustawiana jedna rozległa kolekcja, która miała maty i sprzęt na różnych poziomach. Opony zachowują własną geometrię; zwój liny stoi przy drążkach. Brak sań, skrzyń plyometrycznych i rozwiniętej battle rope jest jawny.
- Gumowe maty mają podbudowę i drewniane krawędzie na równym poziomie. Pawilon ma deski o szerokości około 20 cm. Dach siłowni otrzymuje cztery rzeczywiste drewniane oparcia płatwi na istniejących belkach. Krokwie sięgają blachy, a płatwie dachu worków dochodzą do belek, zamiast pozostawać zawieszone nad nimi.
- Prosta ma 100 m przed K, czyli 129,4 jednostki świata, środek nadal `(68, -24)`. Owal, droga i chodniki są przycinane do wspólnej granicy nawierzchni. Odcinek wspólny owalu i prostej, 11,52269 m² przed K, nie ma dwóch nakładających się warstw. Mała tablica 100 M stoi poza obiema nawierzchniami. Pozycje bramy, ronda i zatoki pozostają zachowane.
- Audyt zachowuje wszystkie surowe wskazania rendererów. Konstrukcje mają jawne zestawy siatek i dokładne zakresy trójkątów po scaleniu. Osobne wyspy geometryczne są mierzone oddzielnie; graf rzeczywistych połączeń prowadzi do widocznej siatki gruntu z tym samym colliderem. Nazwa, rodzic ani samo deklarowane przeznaczenie elementu nie stanowią potwierdzenia podparcia. Przekroczenie budżetu pozostaje wynikiem niezmierzonym.
- Pomiar zaczepów korzysta z rzeczywistej powierzchni podpory, także niewypukłego MeshCollider. Nieobsługiwany `ClosestPoint` nie może zwrócić pozornego kontaktu. Wysokość konstrukcji jest oddzielona od potwierdzenia jej podparcia; prawidłowo wymiarowany, lecz lewitujący ring nie przechodzi audytu.
- Uzupełnienie szczegółów D: szkło kabin stoi na posadzce 0,24 m, przycisk WC korzysta z oryginalnego węzła GLB. Oryginalny brodzik ma odpływ, więc usunięto dodatkowy cylinder. Nie dostarczono kolumny prysznicowej ani syfonu umywalki: brak trafia do H, bez imitacji z brył i bez emisji wody z pustego miejsca.
- Biały obrys ronda leży 4–9 mm nad rzeczywistymi nawierzchniami. Cokół wraz z całym pomnikiem jest ustawiany pionowo na wyspie; **skala 4× byka, jego siatka, materiały, X/Z oraz funkcja `RondoBull` pozostają zachowane**. Audyt wymaga także potwierdzenia zgodności wymiarów byka.
- Pomiar zawieszenia drzwi usługowych wybiera rzeczywistą krawędź najbliższą osi zawiasu, niezależnie od znaku osi po imporcie. Osobny `geometryPassage` bada otwór pełną kapsułą gracza i rzeczywistym podłożem; wyklucza wyłącznie jawne ruchome skrzydła. Surowe wskazania przy pivocie pozostają zachowane. To kontrola geometrii, nie test otwierania drzwi w Play Mode.

### Dowody E-01

- Niezależne obliczenia przecięcia nawierzchni: **11/11 PASS**. Skrypt SciPy, wejścia i wyniki zapisano w `EVIDENCE/E-01-numerical/`. To porównanie matematyczne z obliczeniami float32, nie wykonanie C# ani Unity.
- Dokładny indeks kodu E: **268 testów źródeł + 2 testy kontraktu treningu PASS**, bez pominiętych testów; C# **52 Editor + 21 runtime, 0 błędów składni/zakresów**. Regresje silnikowe dotyczą m.in. importowanej geometrii worka, schodów, powierzchni 100 m, podparcia całych konstrukcji i niewypukłych colliderów. To jeszcze nie jest kompilacja Unity.
- Unity E, przejścia NavMesh, telefon i FPS: **jeszcze niezweryfikowane**. Nie deklaruje się zera nieprawidłowych kontaktów na podstawie samych testów źródłowych.

## Pozostałe etapy

Pozostają E, F i G z oddzielnymi publikacjami oraz pojedynczymi kontrolami sceny. Lista H jest weryfikowana równolegle; nowe propozycje modeli wymagają wskazanej przez właściciela akceptacji. Nie ma potwierdzenia limitu 400 MB dla nowej wersji, ponieważ nie uruchomiono APK. Następnie następuje realizacja nowego polecenia animacji z paczek w repo, z wyjątkiem Moniki i z rozdziałem Miś=A / pozostałe=T, przed osobną decyzją właściciela o APK.
