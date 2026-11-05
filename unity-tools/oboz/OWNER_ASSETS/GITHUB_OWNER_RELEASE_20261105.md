# OBÓZ BRAXHORN — dostawa oryginalnych modeli do Unity

Aktualny status: kod nowego obozu w GitHub; nowy ZIP 345 MB i byk GLB są zweryfikowane, ale nie zostały jeszcze przekazane do GitHub Releases.

Tag draft release: oboz-owner-remodel-20261105 (DRAFT, nigdy publicznie published).
Nowe zasoby nie należą do siedmiu starszych szyfrowanych paczek oboz-private-34114c7950bc.

Wymagane pliki (alternatywa: zamiast jednego ZIP 345 MB można przesłać pięć ZIP-ów z iPhone'a, bez ich rozpakowywania):
- BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip | 345273037 B | SHA256 b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006
  LUB komplet pięciu części (łącznie 31 oryginalnych pozycji, wszystkie CRC i SHA-256 sprawdzone):
  1. BRAXHORN_MODELE_CZESC_01_Z_05.zip
  2. BRAXHORN_MODELE_CZESC_02_Z_05.zip
  3. BRAXHORN_MODELE_CZESC_03_Z_05.zip
  4. BRAXHORN_MODELE_CZESC_04_Z_05.zip
  5. BRAXHORN_MODELE_CZESC_05_Z_05.zip
- BRAXHORN_BULL_RONDO_4X_ORYGINALNE_TEKSTURY.glb | 2232460 B | SHA256 425788876b8fe2dfaba90157d74c6631d231184f96659759cdafade794a5352f

Workflow manual: Obóz — Unity 6 APK do testu, ref codex/oboz-przebudowa-pbr-20261105.
Najpierw owner_release_assets.py preflight (fail przed Unity, jeśli brak źródeł). Skrypt rozpoznaje automatycznie jeden duży ZIP albo pięć mniejszych ZIP-ów. Nie wymaga scalania ich przez właściciela. Również wymaga osobnego byka 4x GLB.
Następnie assets.py restore (stare paczki) oraz owner_release_assets.py restore (nowe modele), potem Unity.

Wszystko trafia do ignorowanego katalogu Assets/Source/Prywatne/Oboz/OwnerAssets.

Po zweryfikowanym rozpakowaniu 31 źródeł importer przygotowuje 52 pliki Unity; powiększony byk jest plikiem 53. Wszystkie otrzymują deterministyczne .meta/GUID, z zachowaniem istniejących poprawnych GUID, co zapobiega losowym referencjom po czystym imporcie. Wariant Grass Medium 01/02 zawiera .blend, a nie prefab GLB: pliki oryginalne pozostają zachowane, ale geometrii 3D nie deklarujemy jako wczytanej w Unity.

Rzeczywisty audyt źródeł poza Unity (2026-11-05): 31/31 CRC i SHA256, 53 pliki przygotowane, 70 unikatowych GUID, 24 kluczowe ścieżki obecne, sześć tekstur potwierdzone jako 2K (2048x2048), żadne źródła postaci nie były modyfikowane. To **nie** zastępuje kompilacji Unity ani testu Androida.
Nie publikować źródłowych GLB i ZIP w publicznym Gicie. Postacie i ich animacje są nienaruszalne.

Pozostałe ograniczenia: brak osobnego zweryfikowanego drugiego GLB Vintage w nowym manifeście, Blender .blend z trawy nie jest automatycznie gotowym modelem Unity. Pełny odbiór Unity/Android nadal potrzebny.

## Zastępstwo iPhone dla niedostępnej części 01 (05.11.2026)
Jeśli nie da się pobrać 85 MB BRAXHORN_MODELE_CZESC_01_Z_05.zip, właściciel może dosłać **sześć osobnych mniejszych ZIP-ów** z tej samej oryginalnej zawartości. Nie są to modele zastępcze i nie zmieniają źródeł:
- BRAXHORN_OBOZ_MALE_01_01_Z_06.zip — 16 396 829 B (zawiera także byka GLB 4×)
- BRAXHORN_OBOZ_MALE_01_02_Z_06.zip — 14 164 201 B
- BRAXHORN_OBOZ_MALE_01_03_Z_06.zip — 14 164 201 B
- BRAXHORN_OBOZ_MALE_01_04_Z_06.zip — 14 164 201 B
- BRAXHORN_OBOZ_MALE_01_05_Z_06.zip — 14 164 201 B
- BRAXHORN_OBOZ_MALE_01_06_Z_06.zip — 14 164 200 B

**W trybie sześciu małych ZIP-ów nie dosyłać osobno byka!** Jego oryginalny plik GLB znajduje się w małej paczce 01 i jest weryfikowany SHA-256 po rozpakowaniu.

Pozostawić poprzednie oryginalne BRAXHORN_MODELE_CZESC_02_Z_05.zip, CZESC_03, CZESC_04 i CZESC_05 (już przesłane w DRAFT). Dodatkowa źle nazwana kopia CZESC_03_Z_05.2.zip jest ignorowana i nie blokuje importu.
W GitHub Releases uploadować małe ZIP-y **bez rozpakowania**, nie publikować wersji roboczej (DRAFT).

owner_release_assets.py automatycznie wybiera tryb six_small, pobiera sześć małych ZIP-ów i istniejące części 02–05, odtwarza bajt-po-bajcie ZIP 01, weryfikuje SHA-256 i odczytuje oryginalnego byka GLB z małej paczki 01. Dalej uruchamia wspólny zweryfikowany importer pięciu części z deterministycznymi GUID-ami. Nie dotyka postaci i animacji.

## Aktualny odbiór rzeczywistego wydania GitHub — 05.11.2026

Wersja robocza DRAFT ID=435486352 znajduje się na poprawnej gałęzi
`codex/oboz-przebudowa-pbr-20261105`.
Potwierdzone z listy aktywnych załączników:
- BRAXHORN_MODELE_CZESC_01_Z_05.zip — 84984137 B
- BRAXHORN_MODELE_CZESC_02_Z_05.zip — 84997165 B
- BRAXHORN_MODELE_CZESC_03_Z_05.zip — 84995158 B
- BRAXHORN_MODELE_CZESC_04_Z_05.zip — 84985353 B
- BRAXHORN_MODELE_CZESC_05_Z_05.zip — 5311312 B
- A_charging_bull_statu_0725030443_refine.glb — 2232404 B

Istnieje dodatkowa omyłkowa `CZESC_03_Z_05.2.zip`; importer jej nie używa.
Oryginalny GLB byka jest poprawny: SHA256
`05667f880b5beaf720dec95ac480ce32c0e91e1e2ab318a28d34f36e475f4cf0`.
Jego sekcja BIN (geometria + oryginalna tekstura JPEG) jest identyczna z
wariantem 4x. Model pozostaje oryginalny; w Unity skalowanie odbywa się przez
Transform bez przepisywania pliku. Importer obsługuje obie nazwy i wymiary,
preferując właścicielski oryginał.

Pełna kontrola pięciu lokalnych ZIP: 31/31 plików, CRC poprawne, hash SHA-256
każdego pliku zgodny z manifestem, bez braków i duplikatów. Lista GitHub potwierdza
nazwy i rozmiary, ale **nie pozwala z niej sprawdzić zawartości załączników
bajt po bajcie**. Workflow sprawdzi dokładne hashe po pobraniu i zatrzyma się
przed Unity, gdy dane się nie zgodzą.

Status: dostawa obecna w DRAFT, kod transportu poprawiony i zapisany.
**Brak potwierdzenia kompilacji Unity/Android na tej gałęzi i brak nowego APK.**
Publikacja release nie jest potrzebna; pozostawić jako DRAFT.
