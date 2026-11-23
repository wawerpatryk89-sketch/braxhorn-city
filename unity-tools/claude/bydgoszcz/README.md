# MIASTO R1 — osiedle bydgoskie przy M. Skłodowskiej-Curie 32a (etap 1, 23.11.2026)

**Stan: geometria i kod gotowe do wczytania, krok WYŁĄCZONY (`BraxBydgoszcz.Wlaczone = false`). W grze tego jeszcze nie ma i nikt tego w grze nie widział.**

Właściciel 23.11, 02:07–02:49: odwzorować ulice i bloki przy Skłodowskiej-Curie 32a po stronie dużego sklepu; „nie chcę lipy”;
„to ma wyglądać jak najbardziej realistycznie”; „zdjęcia do wglądu, zanim cokolwiek zaakceptuję”.

| Plik | Co to |
|---|---|
| `osiedle_metry.json` | wycinek mapy OpenStreetMap 390 × 390 m w metrach, (0,0) = blok 32a (© autorzy OpenStreetMap, ODbL) |
| `osiedle_gen.py` | liczy całą geometrię i zapisuje `osiedle.bin`, `osiedle_opis.json`, `podglad_*.png` |
| `osiedle.bin` | to, co wczytuje gra (`unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs`) |
| `osiedle_opis.json` | liczby, lista zbudowanych budynków, **lista braków** |
| `podglad_*.png` | kontrola GEOMETRII z tych samych trójkątów — płaskie kolory, bez tekstur, światła i trawy z gry. **To nie jest wygląd gry.** |
| `../test_r1_bydgoszcz.py` | 8 testów pliku i wpięcia |

## Co jest zbudowane (35 budynków, 111 660 trójkątów, najcięższy 9 070)

Grunt z mapy: jezdnie, uliczki i parkingi osiedlowe, chodniki z krawężnikiem, trawniki (trawa z `BraxRegionTrawa`).
Budynki na PRAWDZIWYCH obrysach z mapy, okna i drzwi jako prawdziwe otwory w ścianie (bez nakładek):
wieżowce szare 28a/30a/32a/34a (11 kond., balkony z kolorowymi płytami, ślepy szczyt z polem i adresem), wieżowce przy
Wyszyńskiego (11 kond., pola koloru), bloki 28–38 (4 kond., żółte pola, brązowe pasy klatek, czerwony cokół, kominy z cegły),
7 rzędów garaży z bramami, 4 pawilony, bar mleczny 34b, szkoła (zielona, białe okna, wejście ze słupami).

## Czego NIE ma — uczciwie

- **18 budynków bez zdjęcia nie stoi** (lista w `osiedle_opis.json`): m.in. szpital, duży sklep (właściciel 02:40: „na razie zostaw”),
  budynek z czerwonym dachem przy ulicy, przedszkole 36a, bloki przy Wyszyńskiego bez adresu w mapie.
- Drzewa, latarnie, znaki, sygnalizacja, słupki, wiaty śmietnikowe, ogrodzenia, auta, ludzie — brak. Osiedle bez drzew wygląda łyso.
- Oznakowanie jezdni (linie, zebry) — brak; ma wejść w teksturę (ZASADY 4.7).
- Graffiti (53 sztuki na gałęzi `claude/bydgoszcz-uklad-20261123`) — nie nałożone.
- **Mural na barze mlecznym — JEST** (`unity-city/Assets/Source/Claude/Bydgoszcz/mural_bar_mleczny.jpg`, 3,0 × 4,8 m w wycięciu ściany).
  Decyzja właściciela 23.11, 08:01: *„ten mural to mój kolega tam robił (…) możesz go normalnie wstawić, z klubem Zawisza naszym”* —
  wyjątek właściciela od ZASAD 7.4 dla tego jednego obrazu. Plik od właściciela jest ucięty z prawej (napis bez ostatniej litery);
  pełny obraz = podmiana pliku i ponowne `osiedle_gen.py`. Która ściana baru: przyjęto najdłuższą bez wejścia, najbliżej uliczki.
- Rzeka, mosty, łączniki, stare miasto — brak.
- Przyjęte bez pewności: która długa ściana wieżowca ma balkony (przyjęto zachodnią) i gdzie są wejścia; wysokość skrzydeł szkoły
  poza głównym; 11 kondygnacji wieżowców 28a/30a/34a i przy Wyszyńskiego (w mapie jest tylko dla 32a; reszta policzona ze zdjęć).
- Tekstury w 2K jak reszta R1 (źródło 4K z ZASAD 5.9 wymaga zmiany importu całego rejonu).

## Dlaczego wyłączone

Na północ od granicy gry (z > 180) `BraxRegionHoryzont` stawia wzgórza i las (wiersze 120 i 555). Osiedle w (−145,9; 508,8) stanęłoby
w zboczu. Włączenie = przesunięcie `BraxMiastoGranice.Z1`, horyzontu, lasu i panoramy. To zmiana w rdzeniu rejonu — nie robiona na ślepo.

## Sprawdzone

Kompilacja C# poza Unity na prawdziwych bibliotekach: 0 błędów (jak przed zmianą). Testy: 645 OK (637 + 8 nowych).
Generator powtarzalny (dwa przebiegi = identyczny plik). Push tej gałęzi nie uruchamia żadnego workflow. Build: tylko właściciel.
