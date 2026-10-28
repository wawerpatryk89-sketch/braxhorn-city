# Materiały (modele) — spis

Wszystko spakowane `gltfpack` (meshopt `-cc`, tekstury KTX2 `-tc`). Podgląd: `dev/docs/przeglad_modeli.jpg` (plansza 1) i `dev/docs/materialy_seria1.jpg` (seria 1 z 28.10).
Oryginały są tylko w załącznikach czatu. Jeśli trzeba spakować inaczej (np. większe tekstury), poproś użytkownika o ponowne wysłanie.

## Plansza 1 (wybrane wcześniej)

| Plik | Co to | Uwagi |
|---|---|---|
| `blokowisko_city7.glb` | blokowisko z kamienicami, model do gier | 54×53 m |
| `kamienice_city_street.glb` | fronty kamienic | z tyłu otwarte |
| `wiadukt_bridge.glb` | skan wiaduktu z łukami | czarna obudowa skanu do wycięcia |
| `podziemie_undercroft.glb` | podziemie ze sklepieniami (klub walk) | obudowa do wycięcia |
| `octagon.glb` | klatka | mata z cudzym logo → przemalować na BRAXHORN |

## Seria 1 (28.10, 15:40–15:50) — „na później”

| Plik | Źródło (Sketchfab) | Wymiary / tris | Do czego | Uwagi |
|---|---|---|---|---|
| `dom_suburban.glb` | suburban house | 16×15 m, 81 tys. (`-si 0.5`) | plansza 2, domki | spec-gloss przerobione na metal-rough (`sg2mr.py`) |
| `dom_z_wnetrzem.glb` | realistic house with interior | 8×9 m, 78 tys. | plansza 2, dom do wejścia | ma wnętrze, szkło z transmission (wyłączyć) |
| `dom_nowoczesny.glb` | modern house with interior | 10×10 m, 4 tys. | plansza 2 | low-poly |
| `dom_wnetrze_zewn.glb` | modern house interior & exterior free | jednostki w cm (×0.01) | plansza 2, dom do wejścia | clearcoat |
| `willa_nowoczesna.glb` | modern house villa (game ready 4k) | 20×15 m, 2 tys. | plansza 2 | tekstury zmniejszone do 2048 |
| `chatka_cozy.glb` | cozy cottage | 9×22 m działka, 414 tys. (`-si 0.5`) | plansza 2 | działka z płotem i garażem, ciężki — przed użyciem uprościć |
| `mieszkanie_wnetrze.glb` | modern apartment interior | jednostki w cm (×0.01) | wnętrze (np. mieszkanie w bloku) | |
| `osiedle_modular.glb` | neighbourhood city modular lowpoly | 42×39 m, 415 tys. | plansza 1/2, bloki, ulice | 1300 siatek — przy użyciu scalić; latarnie mają materiał „lightColors” (bez świecenia!) |
| `garaze_sowieckie.glb` | abandoned soviet garages pack | rząd garaży ~17 m | plansza 1, podwórko blokowiska | game-ready, 10 tys. |
| `budynki_billboardy.glb` | buildings | 4×4 m | tło | billboardy z cudzymi grafikami → podmienić na BRAXHORN |
| `zaulek_dzien.glb` | alley scene day | 17×3 m, 114 tys. (`-si 0.3 -slb`) | ściana zaułka / tło | skan z upieczonym światłem (unlit) |
| `palmy.glb` | palm tree pack lowpoly | 3 palmy, ~11 m | plansza 1, nabrzeże | 9 tys. |
| `ulica_rekwizyty.glb` | street asset pack | barierki, pachołki, znaki, kartony | plansza 1, ulica | |
| `rekwizyty_pbr.glb` | PBR street props pack | kosze, kontener, pachołki, worki, latarnia, kartony | plansza 1, ulica i podwórko | materiał latarni ma emissive → wyłączyć |
| `bunkier_underground.glb` | underground bunker | 9×15 m, 477 tys. | plansza 3, bunkier | wnętrze pod ziemią, ma szkielet/animację (drzwi) |
| `las_droga.glb` | dirt road through forest | teren 550×550 m, 341 tys. | plansza 3, las | przesunięty w górę (y 96–207) — wyciąć kawałek |
| `las_sosny.glb` | pine forest tree pack | 22 drzewa, 209 tys. (`-si 0.3`) | plansza 3 | to suche drzewa bez igieł (tak jest w oryginale) |
| `jaskinia.glb` | small cave scene with opening | skala ×100 (model ma 0,47 m), 421 tys. (`-si 0.2 -slb`) | plansza 3, kryjówka | |

Nie dotarło: **Southwark Borough Control Bunker** (zamiast modelu przyszedł plik z błędem „Too many requests” ze Sketchfaba).
Duplikaty pominięte: drugi `suburban_house`, drugi `modern_house_with_interior`, `modern_house_villa_1`, `neighbourhood_city_modular_lowpoly_1`, `palm_tree_pack_lowpoly_1`.
