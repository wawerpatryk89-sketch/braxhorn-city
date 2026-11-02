# Naprawy osiedla — Patryk — 2 listopada 2026

Baza: `5637db7edec73eda8c74ac8f83f6c1a918b2a808` (po buildzie #41 `4a023291e38cbe1e087050da260e01870ebf3721`; późniejsze zmiany bazy dotyczą Vercel). Osobna gałąź: `codex/estate-flicker-patryk-voices-20261102`. Nie nadpisuje gałęzi Claude'a ani APK #41.

## Zmiany przygotowane do testu Unity

| Zgłoszenie | Zmiana i sposób sprawdzenia |
|---|---|
| Migotanie starej i nowej nawierzchni, płot na drodze | Stary algorytm usuwał wyłącznie trójkąty całkowicie zawarte w wycięciu. Nowy rzeczywiście odejmuje bryły od przecinających trójkątów i osiowych kolizji płotów; zachowuje UV, normalne i materiały. Test pola, przecięcia bez wierzchołków wewnątrz, transformacji i kolizji uruchamia Unity przed generowaniem. |
| Roślinność przeskakuje / przycina po obrocie kamery | Dither dla shaderów obsługujących zmianę LOD; bez nakładającego się nieprzezroczystego crossfade dla pozostałych. Zasięg trawy w shaderze zgodny z CPU; przygotowanie pobliskich kęp rozłożone na klatki. |
| Powyginany Patryk | Przenoszenie animacji przez Unity Humanoid, bez przepinania Hips i bez starego ręcznego przeliczania kierunków. Oryginalna hierarchia, bindposes i 4 wagi skóry. Kontrola długości kości, NaN i T-pozy w 8 próbkach każdego klipu. |
| Ciągła garda | Spokojny Idle z opuszczonymi rękami; CombatIdle przez 8 sekund po ciosie/kopnięciu poza sportem. |
| Skala | Zachowane 2.27 jednostki widocznej wysokości Patryka oznaczają 1.80 m. Skala odniesienia K=2.27/1.80. Piłki, obręcze/tablice, bramki i wysokość siatki odniesione do K. Nie przeskalowano bez kontroli całego planu ulic i budynków. |
| Dymki zamiast rozmów | 100 polskich kwestii w dwóch głosach, 200 plików Ogg, przestrzenne AudioSource, czas odpowiedzi z długości klipu. Usunięty interfejs dymków i syntetyczny gwar zastępujący mowę. |
| Różowe auta | Materiały glTF MR konwertowane istniejącym konwerterem projektu z zachowaniem map i UV. |
| Nakładanie aut / skręt | Zablokowane pogłębianie przecięć obrysów. Pełny obrys prywatnego auta uczestniczy w hamowaniu i zabezpieczeniu ruchu NPC. Przednie koła mają różne kąty wewnętrzne/zewnętrzne (Ackermann); istniejące trasy już zawierały łuki. |
| Głowy przez dach | Pomiar dachu nad fotelem i wypieczonej siatki pasażera. Najpierw obniżenie pozycji siedzenia, następnie ograniczona jednolita korekta wzrostu NPC; zbyt niska kabina zgłasza błąd. Patryk nie jest zmniejszany do auta. |
| Prywatny samochód | Auto wybrane spośród istniejących modeli z 4 kołami i kabiną mieszczącą Patryka; zarezerwowane miejsce przy ARENA BRAXHORN. Interakcja tylko gracza, joystick gaz/cofanie/skręt, kolizje bryły i bezpieczne miejsce wysiadania. Kamera za autem, odległość zależna od prędkości. |
| Wsiadanie NPC | Do 6 mieszkańców z bezpiecznym dojściem do samochodów na parkingach: podejście, siedzenie, wyjście i powrót. Ruch drogowy nie otrzymał nowego systemu zjazdu z trasy i parkowania. |
| Ukryte błędy | Błędy wymaganych etapów generowania są propagowane. Kontrola referencji między scenami przed zapisem. Na tej gałęzi przekroczenie czasu/przerwanie kontroli zdjęć nie jest maskowane jako sukces. |

## Dowody i granice walidacji

Analizowane nagrania użytkownika z Patrykiem: `57614dc6-de31-4029-a82e-93266162e78c.mp4`, `b1379c84-5801-468f-b4a8-9675d5d8962c.mp4`, `c89b725e-913a-43c5-afd3-5c38b9d679be.mp4`. Dwa filmy z Braxem nie były podstawą napraw.

Lokalnie sprawdzono składnię C#, składnię skryptów Python/JS, format workflow i dekodowanie wszystkich 200 Ogg. Nie ma lokalnego Unity ani prywatnych odszyfrowanych modeli: kompilacja, retargeting i testy geometrii wymagają rzeczywistego joba Unity w GitHub Actions. Przejście analizy składni nie jest kompilacją.

`estate_repair_review.mjs` uruchamia rzeczywisty player Unity WebGL: zdjęcia postaci, płotu, boiska, ronda, ludzi i trawy; kontrolę wsiadania, przejazdu i wysiadania; zapis faktycznych klatek canvas do WebM. Renderowanie SwiftShader w CI nie mierzy wydajności na telefonie. Nagrania canvas są bez ścieżki dźwiękowej — polską mowę należy sprawdzić w APK.

Przejścia do foteli są proceduralnym połączeniem chodu i pozy siedzącej. Nie dodano dedykowanej animacji otwierania drzwi ani chwytu za klamkę; jakość przejść i przenikanie przy samych drzwiach wymagają oceny nagrania. Nie uznawać samego ukończenia kompilacji za naprawę wszystkich błędów ani za test na urządzeniu.

## Pochodzenie nagrań

Wygenerowane lokalnie przez `piper-tts==1.3.0`, modele `pl_PL-darkman-medium` i `pl_PL-gosia-medium` z `rhasspy/piper-voices`, przypięta rewizja `b4e358ffcb232591bf73a540e70f7de528e4174a`. Karty modeli odsyłają do danych CC0 OHF-Voice/voice-datasets. W repo są wynikowe Ogg i indeks; modele ONNX nie są dystrybuowane. Odtworzenie: `python unity-tools/claude/osiedle_glosy.py --models /ścieżka/do/modeli`; sprawdzenie: `--check` (wymaga ffmpeg).
