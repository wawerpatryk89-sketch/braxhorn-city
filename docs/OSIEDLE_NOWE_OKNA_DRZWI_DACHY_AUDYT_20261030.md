# BRAXHORN CITY — R2: kontrola sześciu nowych źródłowych assetów / 30.10.2026

Zakres: **wyłącznie istniejące osiedle domków**. Położenie i funkcja domów, komisariatu, kasyna, szpitala, 24 drzwi i wspólny `BraxGymPlayer` pozostają niezmienione. Nie używać atrap ani przypadkowych assetów.

## Potwierdzone źródła w bibliotece użytkownika
Lokalizacja: `/Braxhorn City/Assety premium`. Archiwa nie zostały w żaden sposób nadpisane. Źródłowy spis w `asset-library/intake-20261030-estate-doors-windows-roofs.json`.

| Asset | Wymiary sceny GLB (m) | Kontrola | Plan bez samowolnej podmiany |
|---|---:|---|---|
| `residential_window.glb` | 1,04 × 1,42 × 0,13 | 3 mesh, 1 materiał, 4 obrazy, model Sketchfab CC-BY 4.0, autor AleixoAlonso | Kandydat do nowoczesnych okien. Najpierw sprawdzić dopasowanie otworu, szkło, UV i brak zablokowanego przejścia. |
| `realistic_old_wooden_window.glb` | 0,61 × 1,09 × 0,15 | 2 mesh, 2 materiały, 5 obrazów, Sketchfab CC-BY 4.0, Nait | Wygląd starego drewnianego okna. NIE stosować automatycznie do wszystkich nowoczesnych elewacji. |
| `door-pack-free.glb` | 3,31 × 2,39 × 0,16 (łącznie dwa warianty obok siebie) | 4 mesh, 4 materiały, 13 obrazów, Sketchfab CC-BY 4.0, saeedsd | Rozdzielić dwa skrzydła i dwie ościeżnice na osobne prefaby; oryginalny animator drzwi i kolizje zachować. |
| `interior_wood_doors_pack_1.glb` | 2,13 × 5,30 × 12,90 (scena prezentacyjna, NIE jeden komplet drzwi) | 6 mesh, 2 materiały, 6 obrazów, Sketchfab CC-BY 4.0, Toni García Vilche | **Nie importować całej sceny jako pojedynczych drzwi.** Wyodrębnić prefaby D1/D2, bez obiektów preview, przeliczyć położenia osi i zawiasów. |
| `roof_tiles_4k.blend.zip` | materiał PBR | Diffuse, Normal GL, Roughness, Displacement i BLEND, Poly Haven CC0 | Dachówki ceramiczne. Import przez oficjalne, weryfikowane kopie map 1K do Unity R2; archiwum 4K zostaje w bibliotece. |
| `grey_roof_01_4k.blend.zip` | materiał PBR | Diffuse, Normal GL, Roughness, Displacement i BLEND, Poly Haven CC0 | Dachówka antracyt. Wybór konkretnego dachu tylko po sprawdzeniu UV i nagraniu przed/po. |

## Zasady użycia

1. **Jeden oryginalny Brax z miasta**, bez dodatkowego kontrolera i bez nowych animacji. Okna, drzwi i dachy nigdy nie modyfikują `BraxGymPlayer`.
2. Nie wolno zakryć żadnego z 24 działających wejść, zmienić zawiasów bez testu ani przykleić okna na pełną ścianę bez rzeczywistego otworu.
3. Materiały PBR muszą pochodzić z certyfikowanego źródła: CC0 Poly Haven, 1K runtime, zachowane UV. Oryginały 4K z biblioteki są nienaruszone.
4. Cztery modele okien/drzwi mają licencję CC-BY, więc wymagają atrybucji i nie powinny być publicznie publikowane jako osobne paczki biblioteczne. Dopiero po uzyskaniu bezpiecznej ścieżki importu do Unity i sprawdzeniu licencji są kandydatami do wykorzystania w komercyjnej grze.
5. Dla obecnego remontu używamy przede wszystkim tynków, cegieł, dachów, trawy, roślinności i istniejących wykończeń. Asfalt czeka na osobną dostawę. Nie generować fałszywych materiałów tylko po to, żeby zaznaczyć etap jako ukończony.
6. Wszystkie filmy prezentujące efekt to prawdziwy render Unity6 WebGL z załączonym QA i dowodem pochodzenia assetów, a nie grafiki poglądowe.

## Stan faktyczny
- Sześć paczek sprawdzonych (archiwum/GLB), zachowanych w bibliotece: PASS.
- Źródłowy materiał dachówki/tynk/cegła: kopie 1K z API Poly Haven zweryfikowane w QA: PASS.
- Komplet nowych okien/drzwi 3D zaimportowany do Unity: **NIE**. Konieczna ścieżka dostarczenia binariów do odizolowanego CI i dopasowanie do rzeczywistych ścian.
- Wszystkie wejścia i fasady po remoncie odebrane wizualnie na fizycznym iPhonie: **NIE**.
