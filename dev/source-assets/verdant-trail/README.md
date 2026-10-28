# Verdant Trail — materiały użytkownika

Zapis źródeł 28.10.2026. 8 unikalnych plików, około 1250 MiB, przechowywanych w Git LFS.
Nie są częścią `public/` i nie są ładowane przez grę.

## Dostarczono

- `verdant_trail.blend`: Blender 4.00, skompresowany Zstandard.
- `textures-01.zip`, `textures-02.zip`, `textures-04.zip`, `textures-06.zip`: oryginalne archiwa bez modyfikacji.
- `alps_field_2k.hdr`: środowisko HDR, nie model lasu.

Mapowanie oryginalnych nazw, SHA-256 i zawartość archiwów: `manifest.json`.
`verdant_trail(1).blend` oraz drugi HDR są identycznymi duplikatami według SHA-256.
Archiwum 5 dotarło w dwóch częściach: `textures-05a.zip` i `textures-05b.zip`; oba zapisane z kontrolą SHA-256.

## Potwierdzona struktura .blend

Odczytano binarne bloki pliku, bez wykonywania osadzonych skryptów:
173 obiekty, 161 bloków siatek, 49 materiałów, 144 bloki obrazów.
To liczby bloków danych, nie liczby unikalnych gotowych do gry modeli.
Pełne nazwy: `blend-inventory.json`.

Wśród obiektów są:
- `island_tree_01_a/b`, `tree_small_02_a/b/c`, części drzewa `jacaranda_tree`;
- `boulder_01`, `rock_07`, `rock_08`, `stone_01_LOD0`, `sand_rocks_small_01_LOD0`;
- klify `coastal_cliff`, skały `coast_rocks_05` i `rock_face`;
- pnie `dead_tree_trunk`, suche gałęzie i korzenie `root_cluster`;
- trawy `grass_medium_02`, bluszcz `Ivy`, krzewy `shrub_01`–`shrub_04`.

Archiwa zawierają tekstury PNG/JPG/EXR oraz środowiska HDR/EXR.
Wybrane obiekty wyeksportowano do `dev/osiedle/assets/park_*.glb`; przetestowano w przeglądarce 915×412 z emulacją dotyku (nie na fizycznym iPhonie).
Nie wszystkie 144 obrazy sceny są w archiwach. W szczególności brak koloru/alfy liści jacaranda. Dlatego drzewo parkowe składa się z jej oryginalnego pnia i gałęzi oraz UV-dopasowanych liści island_tree z tej samej paczki. To autorski montaż elementów paczki, nie eksport całej oryginalnej sceny.
Nie zastępować poprawnych tekstur zmyślonymi ani nie deklarować sceny jako gotowej do gry.

## Dalsze użycie

Najpierw wybrać pojedyncze drzewa, krzewy i kamienie do ogrodów/obrzeży osiedla.
Eksporty zoptymalizowane trzymać osobno od źródeł. Nie ładować całej sceny do gry.
Później wykorzystać wybrane elementy do osobnej planszy lasu.

## Pobranie źródeł

Po sklonowaniu repo zainstalować Git LFS i uruchomić:

```sh
git lfs pull --include='dev/source-assets/verdant-trail/*'
```

Prawa/licencja pakietu nie zostały opisane w dostarczonych plikach; nie dopisywano przypuszczeń.

## Użyte eksporty

- `park_tree.glb`: pień i gałęzie jacaranda + 1300 uproszczonych skupisk liści island_tree; 7 instancji.
- `park_shrub.glb`: shrub_02_a; 17 instancji.
- `park_rock.glb`: rock_07.001; 6 instancji.
- Skrypt odtwarzalnego eksportu: `dev/osiedle/tools/export_forest.py`. Tekstury kopii gry do 1024 px, uproszczona geometria gałęzi i liści. Oryginały bez zmian.
