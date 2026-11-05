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
