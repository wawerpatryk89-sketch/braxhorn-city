# BRAXHORN R2 — odbiór rzeczywistych paczek premium (30.10.2026)

## Status: SOURCE VERIFIED / UNITY IMPORT PENDING / NO RELEASE

Sześć rzeczywistych paczek pojawiło się w **Bibliotece ChatGPT** w folderze `/Braxhorn city/Assety premium/` między 11:08:12 a 11:08:26 UTC 30.10.2026. Zostały otwarte i sprawdzone lokalnie (kontrola nagłówków GLB/ZIP, geometrii, tekstur i SHA-256). **Istnienie w Bibliotece nie oznacza dostępności w repozytorium GitHub ani zaimportowania do sceny Unity 6.** Nie deklarować, że okna/drzwi/dachówki widać w buildzie, dopóki kontrola rzeczywistego renderu tego nie potwierdzi.

| Paczka | SHA-256 | Sprawdzona zawartość | Źródło/licencja |
| --- | --- | --- | --- |
| `residential_window.glb` | `001c49c716bccd2d98b13f9139e77bda63de758754c08cb0a8bab2ac4a146578` | 3 meshe (rama, skrzydło, klamka), 1,04 × 1,42 × 0,13 m; 2756 trójkątów | [AleixoAlonso, Residential Window](https://sketchfab.com/3d-models/residential-window-ae11104237314463a61251fd46ded4b4), CC BY 4.0, embedded glTF metadata |
| `realistic_old_wooden_window.glb` | `e9d5a0e61ae06d57f7ee5b13e5aa53082ef38815823b6a62480e214822ec7f71` | 2 meshe, okno drewniane 0,61 × 1,09 × 0,15 m | [Nait, Realistic Old Wooden Window](https://sketchfab.com/3d-models/realistic-old-wooden-window-0d76c234747347b79975f3eb71003807), CC BY 4.0, embedded glTF metadata |
| `door-pack-free.glb` | `b92e4dc30dea8f1acd46d0774973e3b7a6144efc04878538042e9a0512f73a7d` | 4 meshe, 2 pary drzwi + futryn; granice paczki obejmują obie pary | [saeedsd, Door-Pack-Free](https://sketchfab.com/3d-models/door-pack-free-cdc29229c5014f1685df5052cfaee850), CC BY 4.0, embedded glTF metadata |
| `interior_wood_doors_pack_1.glb` | `41f6b8fdfb73c2849d45c41b76aac710dbd5bebdcf64bf50c90045110420aa2c` | 6 meshy, dwie sztuki i wizualizacje; **nie importować całego zestawu jako jednych drzwi** | [Toni García Vilche, Interior Wood Doors Pack 1](https://sketchfab.com/3d-models/interior-wood-doors-pack-1-32707dcfa6304f49bf99148d0211f5f7), CC BY 4.0, embedded glTF metadata |
| `grey_roof_01_4k.blend.zip` | `1927220f53fbb2ec5bc470c2632187504f87239bc2fed803dc1b29e8391db7d7` | Blender + cztery mapy (diffuse, displacement, roughness, GL normal), obrazy 4096² | [Poly Haven, Grey Roof 01](https://polyhaven.com/a/grey_roof_01), CC0 |
| `roof_tiles_4k.blend.zip` | `004630925258bbb471b3878a6e8e99e838a4aeade5ea3d1cce5d74a0c6536cd9` | Blender + cztery mapy (diffuse, displacement, roughness, GL normal), obrazy 4096² | [Poly Haven, Roof Tiles](https://polyhaven.com/a/roof_tiles), CC0 |

Poly Haven potwierdza licencję zbiorczą [CC0](https://polyhaven.com/license). Cztery pliki Sketchfab mają niezależną licencję **CC BY 4.0** i wymagają atrybucji autorów w grze (np. ekran Credits) oraz odnotowania zmian.

## Obowiązkowe zasady importu do osiedla Unity 6

1. Zasoby dostępne w Bibliotece mają status **INVENTORY PASS**, nie **SCENE PASS**. Przed kompilacją muszą trafić rzeczywiste binaria do stagingu wyłącznie R2, z weryfikacją SHA-256 i źródła, bez podmian losowymi modelami.
2. Dla okien zachować rzeczywiste otwory, rozmiary i orientację; szkła nie zamieniać na opaque box. Dla drzwi zachować istniejące `BraxCityDoor`, zawiasy, kolizje i sterowanie; osobno zweryfikować otwieranie.
3. Dla dachów konieczny import PBR, UV i wizualny przegląd; 4K stanowi **archiwum źródłowe**, do WebGL na iPhone stosować po audycie 1–2K z mipmapami/kompresją i weryfikacją w scenie. Nie nakładać materiału na losowe siatki.
4. Światło i Three.js nie są dowodem remontu Unity. Wyłącznie rzeczywisty nowy Unity6 WebGL + screenshoty/MP4 z oznaczeniem sha builda i plik raportu są podstawą odbioru.
5. Nie wolno publikować filmu z metody `TravelPoint`, `ReviewView` lub `TravelTo` między ujęciami jako „ciągłego spaceru”. Poprzedni film `braxhorn-osiedle-przed-remontem.mp4` używa tych przeskoków, ma wyłącznie znaczenie referencyjne sprzed remontu.
6. Osobny blok dla asfaltu: w Bibliotece istnieją `FourLaneRoadClean01_MR_4K.zip` i `TwoLaneRoadPatches02_MR_4K(1).zip`; brak dowodu przypięcia do aktualnych dróg R2 w Unity. Nie oznaczać asfaltu jako wdrożonego.
7. Brak możliwości fizycznego testu iPhone w GitHub Actions; Chromium WebGL2/SwiftShader to tylko QA automatyczne, **nie** odbiór urządzenia.
8. Każda gałąź inna niż `codex/estate-premium-renovation-20261030` oraz produkcja/main pozostają nietknięte.

**Odbiór 30.10.2026:** SOURCE MATERIAL AVAILABLE; R2 UNITY VISUAL REMODEL NOT YET VERIFIED; MOVIE NOT READY; HOSTING NOT READY. Nie wysyłać gotowego linku użytkownikowi przed PASS każdej bramki.
