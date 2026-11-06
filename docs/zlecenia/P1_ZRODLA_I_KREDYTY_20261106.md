# BRAXHORN CITY — P1 BASEN + SPA: źródła 3D i atrybucje (6.11.2026)

To jest rejestr pochodzenia modeli do projektu P1 Unity 6. **Nie jest potwierdzeniem wydania gry ani pełnej zgodności licencyjnej.**

## Zweryfikowane źródło prywatne
- GitHub draft Release: `p1-basen-spa-20261106`.
- Źródło: `P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip` (16 oryginałów: 14 3D i 2 grafiki BASEN/SPA) z sumą SHA-256 `5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263`.
- Import przez `unity-tools/mistrzowie/import_pool_p1_owner_assets.py` zachowuje dokładne bajty wszystkich plików źródłowych i pliki Unity `.meta`. Wyjątek: `stack_of_towels_2k.glb` to wersja z mapami przeskalowanymi 4K→2K, udokumentowana osobno.
- Pakiet przekazany do repozytorium jako **prywatny asset wydania**; nie publikować go jako otwartej biblioteki modeli. Samo posiadanie plików nie dowodzi licencji na ich dalszą redystrybucję.

## Modele wymagające wymienienia autorów (CC BY 4.0)
Informacje o autorach pochodzą z metadanych dostarczonych modeli i pliku `LICENCES_3D.txt` w paczce właściciela:

| Model | Autor |
|---|---|
| `locker_room_bench_free_model.glb` | AMMediaGames |
| `shower_cabin.glb` | Heliona |
| `lockers.glb` | TheThorns |
| `stack_of_towels_2k.glb` | fantasticmrcox |
| `wulop_massage_table.glb` | federico.scappiti |
| `modern_bathroom_assets.glb` | vixnik |

Licencja: https://creativecommons.org/licenses/by/4.0/ . Wymagana jest odpowiednia atrybucja w wydawanej grze lub jej dostępnej sekcji kredytów; autorzy i linki źródłowe muszą zostać ponownie sprawdzone przed publikacją. Nie podawać, że są to modele własnego autorstwa.

## Zjeżdżalnia i materiały PBR
- Użyta lekka `raft-slide-water-park.glb` pochodzi z https://shuriken.studio/models/props/raft-slide-water-park . Licencja https://shuriken.studio/legal/models pozwala modyfikować i osadzać w grze; zabrania osobnego rozpowszechniania modeli jako biblioteki. Źródło pobierane dopiero w CI, nie dodane surowo do publicznego repo.
- Ciężka `water_slide_spiral_5_turn.glb` (autor LegendaryGame_Officiel, CC BY 4.0) **nie jest wdrożona**: redukcja do 2 tys. trójkątów nie zachowała wymaganej ciągłości.
- Natywne mapy materiałowe 2K pochodzą z Poly Haven (CC0): https://polyhaven.com . Jeżeli produkt korzysta z live API jako źródła, wymagany jest kredyt za korzystanie z API („Powered by Poly Haven”); warunki: https://polyhaven.com/our-api .

## Przed publikacją aplikacji
Potwierdzić licencje wszystkich pozostałych modeli właściciela, wdrożyć wymagane kredyty, przeprowadzić kontrolę Unity 6 i dopiero po zatwierdzeniu przygotować aktualizację. Nie uruchamiano Unity, APK ani publikacji.
