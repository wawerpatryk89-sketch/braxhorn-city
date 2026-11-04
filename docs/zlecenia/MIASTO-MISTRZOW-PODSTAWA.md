# Miasto Mistrzów — podstawa, etap 1

Ta sama gra BRAXHORN CITY, projekt `unity-city`, Unity 6000.0.58f2, Built-in Render Pipeline. Baza: `claude/miasto-opus-20261031`, commit `f1981049a9f7e9976b7d9e84216f1747d49a8774`. Osobna scena regionu do późniejszego scalenia; nie zastępuje miasta, osiedla, obozu ani aplikacji na telefonie.

## Zakres

- Teren rdzenia 1200 × 1000 m nominalnych, południowe wybrzeże do z = −500 m; wspólna skala `BraxClaudeOsiedle.K = 2.33f / 1.80f`, zastosowana raz.
- Ulice z geometrią pasów, chodnikami, obniżeniami i segmentami krawężników; 68 przejść, wycięcia pod szyny tramwajowe, żebrowane pola uwagi.
- Pętla tramwajowa, dwa tory, 10 peronów z wiatami; 16 par przystanków autobusowych = 32 stanowiska. Ruch pojazdów później.
- 3 stacje metra: DWORZEC, CENTRUM, ARENY / HOTELE; dwa połączone tory. Stacje, schody, bramki, perony, wyposażenie i ich tunele pochodzą z aktualnego `BraxClaudeMetro`, a nie z nowego uproszczonego generatora. Zmiana wspólnego pliku jest ograniczona do `partial` i opcjonalnego otwarcia końców tuneli; dotychczasowe `Apply()` nadal zamyka tunele starego miasta tak samo jak wcześniej.
- Trzy warianty okładzin i oznakowania. Istniejący `metro_train.glb` zachowany jako zasób do etapu ruchu; nie ma modelu zastępczego ani automatycznej zamiany na wagon z kodu.
- Promenada 1100 × 30 m, szeroka plaża, marina z 80 miejscami (40 jachtów, 40 motorówek); jednostki pływające później. Palmy i drzewa z istniejących zasobów, wyłącznie na ziemi/trawie.
- Budynki wyłącznie jako rezerwacje działek: dwie główne areny (główna BRAXHORN), sale treningowe w dwóch lokalizacjach, pięciogwiazdkowe hotele i apartamenty, dwa duże kasyna, banki/finanse, galerie, sklepy, gastronomia, kino, kluby oraz osobny publiczny basen ze spa. Firmy BRAXHORN, WAWER i SOBCZAK. Budowa elewacji jest następnym etapem.

## Ponowne użycie metra i skala

Wymiary istniejącej stacji są już w jednostkach świata Unity. Jej korzeń ma skalę (1,1,1), zmieniane są tylko położenie i obrót. Korytarze łączące dopasowują główki szyn, rozstaw 1.435 × K, osie torów ±7.6 jednostki świata i przekrój oryginalnych tuneli. Położenia wejść wynikają z oryginalnej geometrii stacji; grunt jest rzeczywiście wycięty w tych miejscach. Nie wolno zastosować K drugi raz.

| Stacja | Środek peronu w nominalnych m (x, z) | Oś |
|---|---:|---|
| M1 DWORZEC | 400, 359 | wschód–zachód |
| M2 CENTRUM | 765, 545 | południe–północ |
| M3 ARENY / HOTELE | 765, 800 | południe–północ |

Przesunięcia względem wstępnych znaczników chronią wejścia przed jezdnią i utrzymują cały istniejący tunel M2 na prostym odcinku za łukiem. Pasażerowie, odjazdy i animacja schodów są odłożone. Automatyczne bramki zachowują mechanizm z miasta, żeby wejście nie było zamknięte niewidzialną kolizją.

## Źródła i jakość

Wyłącznie Poly Haven, istniejące zasoby repozytorium i potwierdzone zasoby właściciela. PBR 4K: kolor, normalna GL, roughness (pakowana do ARM), mipmapy i filtrowanie anizotropowe. Źródła obejmują `coast_sand_02`, `damp_sand`, `clean_asphalt`, `concrete_pavement_02` oraz istniejące materiały metra: `granite_tile_02`, `granite_tile`, `rectangular_facade_tiles`, `concrete_floor_02`, `concrete_wall_008`, `fabric_pattern_05`, `teak_veneer` z https://polyhaven.com/ . Trawa jest tą samą paczką Grass004 4K z biblioteki właściciela; SHA-256 map jest przypięte w `verified_textures.json`. Nie wprowadzamy nowego dostawcy zasobów.

Model pociągu jest istniejącą zoptymalizowaną wersją z repozytorium; jego tekstury źródłowe mają 1K, nie są sztucznie powiększane do 4K. Autorzy i licencja: `Assets/Source/Claude/Metro/CREDITS.md`. Oryginalne prywatne palmy są odszyfrowywane tylko w CI. Brak wymaganego pliku kończy etap błędem; nie dodajemy zastępczych palm ani płaskiego materiału zamiast wymaganego PBR.

## Budowanie i dowód

Zgodnie z `AGENTS.md` z 4.11.2026 commit/push nie uruchamia Unity. Jedynym wyzwalaczem jest `workflow_dispatch`, po odrębnej decyzji właściciela wskazującej gałąź, SHA, region i wariant. Używamy istniejącej, zarejestrowanej nazwy pliku workflow, ale jej wariant na tej gałęzi buduje tylko Miasto Mistrzów. Nie przenosić tego pliku na gałęzie miasta/osiedla ani na main. Przy przyszłym scalaniu przenosić kod regionu osobno od adaptera CI. Nie ma automatycznych ponowień.


Ręczny workflow `claude-region-build.yml` na gałęzi `codex/miasto-mistrzow-20261104` przygotowuje źródła, generuje geometrię, wywołuje `BraxChampionsBuild.Build`, sprawdza przejścia po schodach promieniem/kapsułą gracza, zapisuje i ponownie otwiera scenę oraz buduje paczki sceny i zasobów transportu dla Androida. Nie zmienia Build Settings ani rozruchu istniejącej gry. Nie tworzy odrębnego APK.

Wyniki:

- `mistrzowie-validation-<SHA>`: osobne raporty geometrii, kontraktu ponownego użycia, źródeł PBR i walidacji Unity.
- `mistrzowie-foundation-<SHA>`: zaszyfrowana edytowalna `.unitypackage`, paczki Android i licencje; części maksymalnie 190 MiB. Łączyć części według manifestu przed odszyfrowaniem standardowym kluczem repozytorium. Nie publikować rozpakowanych prywatnych modeli.

Pierwszy eksport zachowuje scenę, jej `.meta`, GUID i zależności. Kolejne ręczne poprawki należy wykonywać na tym zapisanym świecie, nie nadpisywać całej sceny generatorem. Generator odmawia nadpisania istniejącej sceny. Ten etap nie wdraża jeszcze sektorowych content updates/hostingu ani automatycznego scalania z głównym miastem.

Udana geometria lub kompilacja nie oznacza odbioru wyglądu. Raport zawsze oddziela wynik Unity od oceny wizualnej i testu na telefonie; te dwa pola pozostają niezatwierdzone do rzeczywistej kontroli.
