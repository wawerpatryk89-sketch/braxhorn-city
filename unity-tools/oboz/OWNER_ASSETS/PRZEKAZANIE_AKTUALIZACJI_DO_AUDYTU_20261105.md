# BRAXHORN OBÓZ — przekazanie do niezależnego audytu aktualizacji
Data: 2026-11-05. Cel właściciela: ta sama gra BRAXHORN CITY, poprawki obozu jako aktualizacje, bez osobnej aplikacji i bez każdorazowego pełnego APK.

## Nie popełniać ponownie błędu
- Dotychczasowy BraxObozBuild.BuildAndroidReview buduje osobne APK `com.braxhorn.oboz` i **nie jest mechanizmem aktualizacji BRAXHORN CITY**. Nie nazywać go aktualizacją.
- Wariant "dodać Addressables do manifestu i od razu puścić kompilację" został wycofany: commit `43b58d697e673308676619e11bd0ca50ca2d9a47`. Nie dodawać niezweryfikowanych pakietów na ślepo.
- Nie scalać automatycznie całych gałęzi miasta, osiedla i obozu; nie ruszać modeli/animacji postaci.
- Nie uruchamiać ponownie Unity ani nie zwiększać wersji APK, zanim integracja zostanie porównana z faktycznie zainstalowaną bazą.

## Zamiast nowego systemu użyć już przygotowanego wspólnego standardu
Oryginalny kod aktualizacji: `a9ca7986cb97560d16e466f8b78ef2a168f1e82b`,
opis `unity-tools/updates/CONTENT-UPDATES.md`:
https://github.com/wawerpatryk89-sketch/braxhorn-city/blob/a9ca7986cb97560d16e466f8b78ef2a168f1e82b/unity-tools/updates/CONTENT-UPDATES.md

Przenosić selektywnie uzgodnione skrypty `Assets/Updates`, `Assets/Editor/Updates` i `unity-tools/updates`, nie nadpisując aktualnego kodu obozu. Istniejący system używa prywatnych `.braxupdate.zip` importowanych z pliku przez USTAWIENIA → WCZYTAJ AKTUALIZACJĘ. Nie używać GitHub draft jako CDN, nie dodawać tokenów GitHub do Android APK.

Do pierwszej bazy trzeba:
1. Potwierdzić dokładne `applicationId`, certyfikat i `versionCode` istniejącej aplikacji, a także bazę `baseId`; brak potwierdzenia zabrania deklarowania zgodności.
2. Przygotować w Unity sceny obozu i 7 prefabów, ich oryginalne `.meta` i GUID (stałe adresy `oboz/scenes/wejscie` i `oboz/characters/{id}`), zgodne zależności bez kopiowania całej gry.
3. Zweryfikować zgodność kodu C#, pakietów Unity i komponentów z bazową aplikacją. Jeżeli niezgodne — najpierw **jedna zintegrowana aktualizacja bazowego APK**, a nie osobne `com.braxhorn.oboz`.
4. Z użyciem istniejącego `BraxContentBuild.Baseline("oboz",orderedScenePaths)` przygotować bazę i zachować dokładny `addressables_content_state.bin`.
5. Dla późniejszych zmian danych używać `BuildContentUpdate` i porównania od poprzedniej bazy, publikować tylko zgodne paczki do prywatnego importu przez wybór pliku.
6. Zarejestrować dowód wykonania: build Unity i Android, baza, zmiana jednej rzeczy, import i restart na tym samym telefonie, rollback, FPS/RAM i podpis. Bez tego `readyForContentUpdate=false`.

## Obecny stan obozu
- Kod roboczej przebudowy: `codex/oboz-przebudowa-pbr-20261105`. Brama `(0,-125)`, skala `K=1.294`, brak bunkra; scena wymaga niezależnego przeglądu.
- Zasoby w GitHub DRAFT `oboz-owner-remodel-20261105`, release ID `435486352`: pięć ZIP-ów `BRAXHORN_MODELE_CZESC_01_Z_05.zip`…`_05_Z_05.zip` plus oryginalny `A_charging_bull_statu_0725030443_refine.glb`. Nadmiarowa kopia `_03_Z_05.2.zip` jest ignorowana.
- `owner_release_assets.py` weryfikuje SHA-256 i importuje prywatne źródła. Oryginalny byk jest skalowany w Unity przez Transform, bez zmiany modelu.
- `BraxObozUpdateRegistry` eksportuje rejestr, **nie** gotowe Addressables: `readyForContentUpdate=false`.
- **Nie zbudowano tu i nie przetestowano aktualizacji .braxupdate.zip dla obozu na Androidzie.** Bez tego nie wolno nazywać projektu gotowym do małych aktualizacji.

## Zakazy właściciela i dowód odbioru
Nie zmieniać wyglądu zadanych elementów, materiałów PBR 2K z bliska, modeli postaci, ich animacji, skali świata, bramy, archiwów prywatnych. Nie uruchamiać samodzielnie pełnego buildu. Niezależny wykonawca musi potwierdzić poprawki commitem, testami Unity, oryginalnym identyfikatorem gry oraz raportem Androida. Ten dokument jest przekazaniem stanu, a nie oświadczeniem o ukończeniu aktualizacji.
