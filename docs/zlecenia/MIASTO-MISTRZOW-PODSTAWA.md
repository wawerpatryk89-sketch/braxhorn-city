# Miasto Mistrzów — metro i pierwsze budynki

Ta sama gra BRAXHORN CITY, projekt `unity-city`, Unity 6000.0.58f2, Built-in Render Pipeline. Baza: `claude/miasto-opus-20261031`, commit `f1981049a9f7e9976b7d9e84216f1747d49a8774`. Osobna scena regionu do późniejszego scalenia; nie zastępuje miasta, osiedla, obozu ani aplikacji na telefonie.

## Zakres

- Teren rdzenia 1200 × 1000 m nominalnych, południowe wybrzeże do z = −500 m; wspólna skala `BraxClaudeOsiedle.K = 2.33f / 1.80f`, zastosowana raz.
- Ulice z geometrią pasów, chodnikami, obniżeniami i segmentami krawężników; 69 przejść, wycięcia pod szyny tramwajowe, żebrowane pola uwagi.
- Pętla tramwajowa, dwa tory, 10 peronów z wiatami; 16 par przystanków autobusowych = 32 stanowiska. Ruch pojazdów później.
- 3 stacje metra: DWORZEC, CENTRUM, ARENY / HOTELE; dwa połączone tory. Stacje, schody, bramki, perony, wyposażenie i ich tunele pochodzą z aktualnego `BraxClaudeMetro`, a nie z nowego uproszczonego generatora. Zmiana wspólnego pliku jest ograniczona do `partial` i opcjonalnego otwarcia końców tuneli; dotychczasowe `Apply()` nadal zamyka tunele starego miasta tak samo jak wcześniej.
- Trzy warianty okładzin i oznakowania. Istniejący `metro_train.glb`: dwa oryginalne wagony przy peronie M1, na postoju. Każda stacja ma dwa włączone mechanizmy schodów ruchomych. Trasy pociągów pozostają bez uruchomionego ruchu.
- Promenada 1100 × 30 m, szeroka plaża, marina z 80 miejscami (40 jachtów, 40 motorówek); jednostki pływające później. Palmy i drzewa z istniejących zasobów, wyłącznie na ziemi/trawie.
- Pozostała zabudowa w rezerwacjach działek: dwie główne areny (główna BRAXHORN), sale treningowe w dwóch lokalizacjach, pięciogwiazdkowe hotele i apartamenty, dwa duże kasyna, banki/finanse, galerie, sklepy, gastronomia, kino, kluby oraz osobny publiczny basen ze spa. Firmy BRAXHORN, WAWER i SOBCZAK. Pierwsze elewacje A1 i T01 są generowane dopiero po przejściu kontroli metra i podstawy w Unity.

## Ponowne użycie metra i skala

Wymiary istniejącej stacji są już w jednostkach świata Unity. Jej korzeń ma skalę (1,1,1), zmieniane są tylko położenie i obrót. Korytarze łączące dopasowują główki szyn, rozstaw 1.435 × K, osie torów ±7.6 jednostki świata i przekrój oryginalnych tuneli. Położenia wejść wynikają z oryginalnej geometrii stacji; grunt jest rzeczywiście wycięty w tych miejscach. Nie wolno zastosować K drugi raz.

| Stacja | Środek peronu w nominalnych m (x, z) | Oś |
|---|---:|---|
| M1 DWORZEC | 400, 359 | wschód–zachód |
| M2 CENTRUM | 765, 545 | południe–północ |
| M3 ARENY / HOTELE | 765, 800 | południe–północ |

Przesunięcia względem wstępnych znaczników chronią wejścia przed jezdnią i utrzymują cały istniejący tunel M2 na prostym odcinku za łukiem. Pasażerowie i odjazdy są odłożone; schody ruchome używają istniejącej animacji i mechanizmu jazdy z miasta. Automatyczne bramki zachowują mechanizm z miasta, żeby wejście nie było zamknięte niewidzialną kolizją.

## Źródła i jakość

Wyłącznie Poly Haven, istniejące zasoby repozytorium i potwierdzone zasoby właściciela. PBR 4K: kolor, normalna GL, roughness (pakowana do ARM), mipmapy i filtrowanie anizotropowe. Źródła obejmują `coast_sand_02`, `damp_sand`, `clean_asphalt`, `concrete_pavement_02` oraz istniejące materiały metra: `granite_tile_02`, `granite_tile`, `rectangular_facade_tiles`, `concrete_floor_02`, `concrete_wall_008`, `fabric_pattern_05`, `teak_veneer` z https://polyhaven.com/ . Trawa jest tą samą paczką Grass004 4K z biblioteki właściciela; SHA-256 map jest przypięte w `verified_textures.json`. Metal048A i skóra Leather037 również pochodzą z potwierdzonych paczek właściciela, z SHA-256 map w tym samym pliku. Metalness jest pakowane do ARM. Nie wprowadzamy nowego dostawcy zasobów.

Model pociągu jest istniejącą zoptymalizowaną wersją z repozytorium; jego tekstury źródłowe mają 1K, nie są sztucznie powiększane do 4K. Autorzy i licencja: `Assets/Source/Claude/Metro/CREDITS.md`. Oryginalne prywatne palmy są odszyfrowywane tylko w CI. Brak wymaganego pliku kończy etap błędem; nie dodajemy zastępczych palm ani płaskiego materiału zamiast wymaganego PBR.

## Budowanie i dowód

Zgodnie z `AGENTS.md` z 4.11.2026 commit/push nie uruchamia Unity. Jedynym wyzwalaczem jest `workflow_dispatch`, po odrębnej decyzji właściciela wskazującej gałąź, SHA, region i wariant. Używamy istniejącej, zarejestrowanej nazwy pliku workflow, ale jej wariant na tej gałęzi buduje tylko Miasto Mistrzów. Nie przenosić tego pliku na gałęzie miasta/osiedla ani na main. Przy przyszłym scalaniu przenosić kod regionu osobno od adaptera CI. Nie ma automatycznych ponowień.


Ręczny workflow `claude-region-build.yml` na gałęzi `codex/miasto-mistrzow-20261104` przygotowuje źródła, generuje geometrię, wywołuje `BraxChampionsBuild.Build`, sprawdza przejścia po schodach promieniem/kapsułą gracza, a dopiero po zaliczeniu podstawy generuje A1 i T01. Sprawdza dojścia do budynków i drogę do oktagonu, zapisuje i ponownie otwiera scenę oraz buduje paczki sceny i zasobów transportu dla Androida. Nie zmienia Build Settings ani rozruchu istniejącej gry. Nie tworzy odrębnego APK.

Wyniki:

- `mistrzowie-validation-<RUN>-<ATTEMPT>-<SHA>`: osobne raporty geometrii, kontraktu ponownego użycia, źródeł PBR i walidacji Unity.
- `mistrzowie-region-<RUN>-<ATTEMPT>-<SHA>`: zaszyfrowana edytowalna `.unitypackage`, paczki Android i licencje; części maksymalnie 190 MiB. Łączyć części według manifestu przed odszyfrowaniem standardowym kluczem repozytorium. Nie publikować rozpakowanych prywatnych modeli.

Pierwszy eksport zachowuje scenę, jej `.meta`, GUID i zależności. Kolejne ręczne poprawki należy wykonywać na tym zapisanym świecie, nie nadpisywać całej sceny generatorem. Generator odmawia nadpisania istniejącej sceny. Ten etap nie wdraża jeszcze sektorowych content updates/hostingu ani automatycznego scalania z głównym miastem.

Udana geometria lub kompilacja nie oznacza odbioru wyglądu. Raport zawsze oddziela wynik Unity od oceny wizualnej i testu na telefonie; te dwa pola pozostają niezatwierdzone do rzeczywistej kontroli.

## Pierwsza zabudowa i korekta właściciela

| Obiekt | Działka nominalna | Bryła / wnętrze | Status |
|---|---:|---|---|
| A1 BRAXHORN CHAMPION ARENA — PÓŁNOC | 220 × 150 m | bryła 192 × 118 × 48 m; powiększona sala widowni 120 × 70 × 28 m | kod elewacji i adaptacji areny osiedla |
| T01 HOTEL BRAXHORN, pięć gwiazdek w świecie gry | 120 × 110 m | wysokość 150 m, podium, wieża i cofnięta korona | kod elewacji i basenu na dachu |
| A2 CHAMPION ARENA — POŁUDNIE | 220 × 150 m | rezerwa drugiej hali | budowa później |

48 m oznacza wysokość A1. Na polecenie właściciela porównano źródło osiedla: `BraxClaudeOsiedle.ArenaInterior` bazuje na hali około 40 × 30 × 7 m. A1 nie jest od niej mniejsza. Zachowano oktagon, jego wyposażenie i schemat widowni z czterech stron; skopiowany kod przeniesiono do izolowanego `BraxChampionsArena`, bez zmiany plików osiedla. Pochodzenie i skrót metody zapisano w `arena-source.json`. Widownię rozbudowano do 16 rzędów w sektorach. Fotele nie są powiększone razem z budynkiem: mają profilowane poduszki, oparcia, podłokietniki oraz rzeczywiste mapy skóry Leather037 4K. Liczbę rzeczywiście utworzonych miejsc zapisze raport Unity; nie deklarujemy zweryfikowanej pojemności publiczności przed jego wykonaniem.

Elewacje: odrębne bryły areny i hotelu, szklenie z podziałami, kamień, metal z mapą metalness, gzymsy, wejścia, zadaszenia, detale dachu, odwodnienia i dwa poziomy LOD. Hotel ma wyciętą nieckę basenu na dachu, schodki, wodę na istniejącym shaderze i taras. To nie zastępuje osobnego publicznego basenu ze spa P1. Wnętrza hotelu pozostają do wykonania.

Szyldy mają zamknięte, fazowane litery 3D oraz łuk i pięć gwiazd hotelu. Nazwa północnej areny: CHAMPION ARENA, z marką BRAXHORN i dopiskiem PÓŁNOC. Brakuje potwierdzonego modelu rzeźbionego byka z zaakceptowanego wzoru; pozostawiono jawny punkt montażu, bez imitacji. Font bazowy nie jest deklarowany jako odwzorowanie wzoru 1:1. Szyldy wymagają odbioru wizualnego właściciela.

## Kontrole tej aktualizacji

Poprawiono odwrócone powierzchnie pasów geometrii oraz ścianek walców, a także zdegenerowane UV pionowych płaszczyzn. Nowe testy sprawdzają rzeczywiste zwroty normalnych i powierzchnię UV. Tunele otrzymały pełne podkłady, podstawy szyn, chodniki techniczne, korytka i oprawy oświetleniowe. Dodatkowe ciągi piesze A1/T01 usuwają kolidujące miejsca nasadzeń; palmy nadal tylko w ziemi.

Raport statyczny nie jest kompilacją C#. Nie uruchomiono Unity, nie wykonano renderu akceptacyjnego ani pomiaru na telefonie. Wynik `visualAccepted` i `deviceTested` pozostaje `false`. Ręczny build musi ponownie otworzyć zapisaną scenę i powtórzyć kontrole przejść, komponentów metra i zapisanych zasobów; błędy przerywają eksport.

Zasada APK FIRST z 4.11 została zachowana: zaszyfrowany wynik jest przesyłany przed dodatkowymi raportami, pod niezmienną nazwą z run ID/attempt/SHA, a jego obecność potwierdza odczyt GitHub API. Raporty i cache nie unieważniają później dostawy. Bieżący wariant nadal nie buduje APK. Workflow ma `contents: read`; drugi kanał Release nie jest w nim skonfigurowany.

## Patryk do chodzenia i zwiedzania — 4.11.2026

Źródło: najnowszy odczytany stan `codex/osiedle-integracja-final-patryk-20261104`, commit `e93d6c01f1c64fc5527987c9a0a7a2c57daf86e8`. Model właściciela z 3.11, kolor i normalna 4K, ORM 2K, oryginalne animacje z zaszyfrowanych paczek. Generator zachowuje szkielet, bindposes, anatomię i skalę: 1,80 m nominalnie = 2,33 jednostki świata. Kapsuła ma 2,37 jednostki wysokości; korzeń gracza nie otrzymuje drugiego mnożnika K.

Przeniesiono generator Patryka i najnowsze poprawki Humanoid/chodu/biegu do osobnych plików `BraxChampionsPatryk*`, z odrębnym katalogiem importu i shaderem o identycznym kodzie źródłowym jak wersja osiedla. Nie zmieniono generatora ani zasobów postaci osiedla. Wspólny `BraxGymPlayer` otrzymał potrzebne poprawki chodzenia, skoku i kamery; zachowuje API oraz dotychczasowe funkcje miasta, w tym `SnapKamera`. Nie przeniesiono samochodu, ruchu ulicznego ani logiki budowania osiedla. Granice akwenu pierwszego miasta nie ograniczają współrzędnych Miasta Mistrzów.

Po ręcznie zatwierdzonym generowaniu powstaną dwie kompletne sceny:

| Scena w `Assets/PlayableGenerated/Mistrzowie` | Zastosowanie |
|---|---|
| `Mistrzowie_Foundation.unity` | Region bez gracza, do późniejszego wczytywania addytywnego |
| `Mistrzowie_Explore.unity` | Ten sam teren z jednym Patrykiem, kamerą, światłem i sterowaniem do zwiedzania |

Scenę `Mistrzowie_Explore` otwierać samodzielnie (Single), bez równoczesnego ładowania Foundation lub gracza innego regionu. Start: dojście od południa do CHAMPION ARENA — PÓŁNOC, nominalnie x=600, z=700; kierunek patrzenia na arenę. Reset wraca w to miejsce. Pięć punktów w istniejącym menu testowym: arena, wejście hotelu i trzy wejścia metra. Punkty są sprawdzane promieniem podłoża i kapsułą o rzeczywistych wymiarach Patryka.

Android zachowuje istniejący HUD: lewy joystick chód/bieg, prawa strona obrót kamery, przyciski akcji/skoku i menu. Sterowanie dotykowe uruchamia istniejący `BraxTouchControls`, bez dodatkowego egzemplarza w scenie. W Unity Editor/PC: WASD lub strzałki — chód, Shift — bieg, prawy przycisk myszy — kamera, Spacja — skok, E — interakcja, R — reset, Escape — pauza. Adapter klawiatury wywołuje istniejące polecenia gracza.

Model i źródłowe mapy są weryfikowane SHA-256 przed użyciem. Oryginalny importer wymaga poprawnych animacji i blokuje poważne błędy ustawienia stóp. Walidacja Unity po ponownym otwarciu sceny sprawdza zapisane animacje, kości, tekstury, referencje, jedną kamerę i wolny start. Kontrole metra i dojść uwzględniają teraz kapsułę Patryka 2,37 m, zamiast wcześniejszej 2,25 m.

Stan tej zmiany: kod i testy statyczne; **Unity, Play Mode i Android nie zostały uruchomione**. Wynik aktualnego workflow pozostaje paczką edytowalnych scen oraz bundles, nie APK. Bundle nie dostarcza nowego kodu C# do starej aplikacji: host musi zawierać kod z tego commita. Test ruchu, animacji, kamery, schodów i zapisu scen jest wymagany przed deklaracją działania na telefonie. Szczegóły pochodzenia: `unity-tools/mistrzowie/player-source.json`.
