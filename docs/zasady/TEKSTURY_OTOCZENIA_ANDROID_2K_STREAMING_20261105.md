# Android — tekstury otoczenia 2K z bliska i mipmap streaming w oddali
Data polecenia właściciela: 5 listopada 2026 | Unity 6 | Miasto Mistrzów i Obóz

## Decyzja i zakres
- Najbliższe powierzchnie: **asfalt, kostka brukowa, chodniki, ściany, elewacje, drewno, metal, piasek, ziemia, trawa** — maksimum **2048 × 2048 (2K)** na Androidzie.
- Mipmapy włączone; **mipmap streaming** wybiera faktycznie potrzebne poziomy rozdzielczości według widoczności, rozmiaru obiektu na ekranie i dostępnego budżetu. Mniejsza rozdzielczość z daleka nie jest ręcznie przypisana do współrzędnych — robi to Unity.
- **Tiling UV i skala materiałów pozostają takie, jak w zatwierdzonych materiałach**. Tiling powiela szczegół na powierzchni, ale sam nie redukuje pamięci tekstury.
- Oryginalne źródła 4K, kolory, normalne, kanały PBR, UV, geometria i branding są zachowane. Nie wykonywać nieautoryzowanej zmiany wizualnej ani podmiany tekstur.
- Główna grywalna postać Monika w Obozie: zachowane istniejące mapy do 4K. Pozostali obozowi NPC: import do 2K. Miasto Mistrzów używa Braxa przygotowanego z istniejących źródeł do 2K; nie modyfikować jego rigowania ani map.
- Wyjątek techniczny Obozu: czterowidokowe atlasowe billboardy drzew pozostają z limitem 8K, formatem ASTC 6×6 i streamingiem, aby nie pogorszyć sylwetek roślin bez pomiaru. Nie utożsamiać tego limitu ze stałą obecnością 8K w VRAM.

## Implementacja
1. Miasto Mistrzów: `Assets/Editor/Mistrzowie/BraxChampionsTextureImport.cs` — limit Android i importu 2K dla generowanych kopii map powierzchni, streaming i dotychczasowy ASTC 4×4. `BraxChampionsFoundation.cs` weryfikuje wynik importu do 2K; oryginały i obecny tiling 4K są zachowane.
2. Miasto Mistrzów: `BraxChampionsBuild.cs` — streaming w każdym profilu jakości, wstępny budżet 256 MB, maks. redukcja 3 poziomów mipmap, budowa LZ4.
3. Obóz: `Assets/Editor/Oboz/BraxObozImport.cs` — limit 2K otoczenia i NPC, streaming mipmap; Monika bez redukcji.
4. Obóz: `BraxObozMaterials.cs` — publiczne mapy asfaltu, kostki i trawy do 2K, mip streaming, istniejący tiling PBR.
5. Obóz: `BraxObozBuild.cs` — włączenie streamingu dla profili jakości, wstępny budżet 256 MB, maks. redukcja 3 poziomów, LZ4.

## Granice pewności i odbiór
- Limit `maxTextureSize` oznacza maksymalną rozdzielczość mapy w imporcie, **nie gwarantuje** pełnych 2K stale z bliska. Unity może wybrać niższą mipmapę również blisko przy przeciążeniu budżetu; potrzebne są pomiary i korekta budżetu dla konkretnego telefonu.
- 256 MB jest **konfiguracją wstępną**, nie wynikiem profilu ani gwarancją płynności. Budżet obejmuje także tekstury, które nie streamują; jeśli przekracza go samo minimum danych, Unity nie zapewni zgodności z limitem.
- Wymagany dowód: poprawny APK, kontrola jakości asfaltu/kostki/ścian z odległości bliskiej i dalekiej, pomiar `Texture.currentTextureMemory`, `Texture.desiredTextureMemory`, Profiler i Android GPU oraz płynność na telefonie.
- Zmiana kodu nie oznacza poprawnej kompilacji, wyniku optymalizacji ani zgody na uruchomienie nowego buildu. Istniejące buildy uruchomione na starszym SHA nie zawierają tej poprawki.
- Nie zmieniać w innych gałęziach: Miasta, Osiedla ani BRAXHORN ARENA bez osobnej decyzji właściciela.
