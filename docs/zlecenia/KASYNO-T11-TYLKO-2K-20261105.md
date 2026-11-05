# KASYNO T11 — źródła Poly Haven wyłącznie 2K

Decyzja właściciela, 5 listopada 2026 r.: **nowe materiały pobierane do kasyna T11 mają być tylko natywne 2K (2048×2048), bez pobierania 4K i bez sztucznego skalowania 4K→2K.** Pozostałe istniejące materiały areny A1, hotelu i innych obszarów nie są przez to polecenie wymieniane ani retuszowane.

## Wdrożenie
- `unity-tools/mistrzowie/prepare_casino_textures_2k.py` pobiera z oficjalnego API Poly Haven wariant `2k` siedmiu powierzchni i trzy autentyczne mapy na materiał: `diff.jpg`, `nor_gl.png`, `arm.png` (AO / roughness / metalness).
- Działające nazwy materiałów: `rectangular_facade_tiles_02`, `rectangular_facade_tiles`, `granite_tile`, `clean_asphalt`, `concrete_pavement_02`, `concrete_floor_02`, `fabric_pattern_05`; razem 21 plików 2048×2048. Domyślny wariant koloru `col_01` dla tkaniny.
- Dla wszystkich plików weryfikowane są dostarczone przez Poly Haven rozmiar i MD5 oraz rzeczywista rozdzielczość z dekodera obrazu; manifest zapisuje także SHA-256 i URL pochodzenia.
- `BraxChampionsCasinoPbr2K.cs` tworzy **wszystkie siedem** materiałów kasyna wyłącznie z katalogu `CasinoT11/Textures2K`, bez wywołania dawnego materiału 4K.
- Szampańskie złoto i stal opraw oparte na PBR shaderze CityStaticPBR generowane z kodu bez pobierania map 4K.
- `BraxChampionsCasinoT11TextureImport.cs`: sRGB tylko w kolorze, normalna i ARM liniowe, ASTC 4×4 na Androidzie, filtrowanie trójliniowe i streaming mipmap.
- Generator nadaje teksturom i folderom stałe metadane `.meta` / GUID, aby aktualizacje nie gubiły zasobów.
- Workflow kasyna w ręcznie uruchamianym `mistrzowie-apk.yml` ma osobny cache `mistrzowie-casino-t11-2k` i krok pobierania przed Unity. Poprzednie zasoby 4K innych obszarów pozostają niezależne, a ich kompilacji nie uruchamiano.

## Status
Kod i kontrakty w repo: zapisane. **Pobierania źródeł do runnera i kompilacji Unity/Android nie wykonano w ramach tej aktualizacji**. Testy statyczne dodane w `test_casino_2k.py`; test w działającej scenie i odbiór wizualny są późniejszymi etapami.
