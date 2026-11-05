# Inwentarz załączników — 5 listopada 2026

W tej rozmowie użytkownik przekazał 10 modeli GLB, 4 murale, 10 zdjęć i 3 filmy (źródła są w paczce przekazywanej użytkownikowi, **nie zostały opublikowane publicznie na GitHubie**).

| Nazwa pliku | Weryfikacja zawartości | Trójkąty | Rozmiar surowego pliku |
|---|---|---:|---:|
| `24-piece-cutlery-set.glb` | 24 sztućce; stołówka | 13140 | 3.19 MiB |
| `bath-towel.glb` | ręcznik kąpielowy | 11592 | 1.40 MiB |
| `food_tray.glb` | taca | 316 | 0.11 MiB |
| `glass-food-display-showcase-with-trays.glb` | witryna gastronomiczna, 18 obrazów tekstur | 22544 | 34.92 MiB |
| `kitchen_-_assets.glb` | zestaw kuchenny, 19 obrazów tekstur | 102181 | 78.76 MiB |
| `model(1).glb` | stojak na piłki lekarskie; NIE worek | 3048 | 0.14 MiB |
| `picnic_set_1.glb` | zewnętrzny zestaw ławkowy/piknikowy | 1084 | 8.26 MiB |
| `rope-coil.glb` | zwinięta lina; dekoracja, nie kompletna lina do wspinaczki | 7928 | 0.98 MiB |
| `stockpot.glb` | garnek z pokrywą | 4752 | 0.20 MiB |
| `towel-rail-with-towel.glb` | wieszak z ręcznikiem | 10056 | 4.03 MiB |

Źródłem liczb jest odczyt struktury oryginalnych plików GLB przesłanych w rozmowie. To nie jest wynik importu Unity.

### Murale (oryginały 16:9 — zachować wygląd 1:1)
- `87405F34-A95B-4AD6-8637-A19599AADA61.jpeg` — **OBÓZ WOJOWNIKÓW**, czerwony, propozycja strefy przeszkód.
- `35502A48-F7AB-4650-9874-3266881B2DF6.jpeg` — **BRAXHORN FIGHT ZONE**, fiolet, propozycja strefy walki.
- `6C3045DC-051D-4B53-8F6E-FEA579A0DE21.jpeg` — **DROGA MISTRZA**, złoto, propozycja strefy mieszkalnej.
- `6B6E1CA3-605A-47FB-BF0E-F23B1D2C3301(1).jpeg` — **BRAXHORN TRAINING CAMP**, zieleń, propozycja hali.

### Referencje z aktualnego obozu
- 10 zdjęć `IMG_0086.jpeg` – `IMG_0095.jpeg`: wjazd, pasy, place, brama, wiata, autobus, ścieżki, las i rozstawienie sprzętu.
- 3 pliki wideo z gry: `3a21417f-6832-4571-b1f2-0594defbab0f.mp4`; `6ba2dbc9-9955-4419-ae43-6d497f33efef.mp4`; `2d4c79ed-139e-4042-a181-2c7e458d15b0.mp4`.
- Z nagrań nie wolno wnioskować o poprawności wszystkich mechanik tylko na podstawie wyglądu.

### Dostarczone oddzielnie / oczekiwane
- Właściciel ma pobrane łóżka pojedyncze, stołówkę i krzesła — aktualnie brak ich nowych plików w tej dostawie.
- Brązowy realistyczny worek bokserski: brak potwierdzonego modelu w dostawie.
- Dalsze materiały i instrukcje: zapowiedziane, inwentarz otwarty.

### Przygotowanie do Androida
Skontrolować każdy model: prawa użycia, pivot, skalę, PBR, tekstury, liczbę obiektów i kolizje. Szczególnie `kitchen_-_assets.glb` (78.76 MiB, 102181 trójkątów) i witryna z przezroczystymi powierzchniami są ryzykiem dla pamięci, czasu renderowania i rozmiaru APK; optymalizować bez zmiany wyglądu zaakceptowanych modeli.

Pełne skróty SHA-256 modeli są w `MANIFEST-ZASOBOW.json`. Nie mylić listy z gotowymi assetami w repozytorium ani z zaimportowanymi obiektami gry.
