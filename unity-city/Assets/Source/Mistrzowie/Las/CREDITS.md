# Las toru kładów — karty drzew

`las_atlas.png` (4096 × 2048, 8 × 2 komórek 512 × 1024): każde drzewo z boku pod 0° i 90°, wyrenderowane z prawdziwych
skanów Poly Haven (wyłącznie ich własne tekstury i siatki, nic nie domalowane). Robią to skrypty w `unity-tools/mistrzowie/las`:
`pobierz_drzewo.py` (model 1k + igły w 4K z ich własną maską alfa), `wypal_drzewa.js` (render 2×), `atlas_lasu.py` (zmniejszenie,
pokrycie maski, kolor pod przezroczystością). Używa `BraxChampionsLasKladow` (Unity: dwie skrzyżowane karty na drzewo).

| Komórki | Model | Skąd | Autor | Licencja |
|---|---|---|---|---|
| 0–5 (sosna_a, sosna_b, sosna_c) | Pine Tree 01, warianty a / b / c | https://polyhaven.com/a/pine_tree_01 | Poly Haven | CC0 |
| 6–11 (jodla_a, jodla_b, jodla_c) | Fir Tree 01, warianty a / b / c (lista właściciela) | https://polyhaven.com/a/fir_tree_01 | Poly Haven | CC0 |
| 12–13 (mloda_jodla) | Fir Sapling, wariant a | https://polyhaven.com/a/fir_sapling | Poly Haven | CC0 |
| 14–15 (mloda_sosna) | Pine Sapling Small, wariant a | https://polyhaven.com/a/pine_sapling_small | Poly Haven | CC0 |

Tekstury gruntu i kłód lasu (`forest_floor`, `pine_bark`, Poly Haven CC0) pobiera `unity-tools/mistrzowie/prepare_assets.py`.
