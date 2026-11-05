# BRAXHORN CITY — OBÓZ: PRZEKAZANIE DO NIEZALEŻNEGO AUDYTU
Data: 2026-11-05. Zakres: tylko obóz, Unity 6.0 LTS 6000.0.58f2, Android.

## Decyzja właściciela (bez wyjątków)
Obóz jest częścią jednej gry BRAXHORN CITY. Prace kontynuować wyłącznie na
`codex/oboz-przebudowa-pbr-20261105`, **bez oddzielnego APK obozu jako docelowej
instalacji**. Kolejne zgodne zmiany zasobów/scen mają być dostarczane przez
`.braxupdate.zip`, nie przez pełną instalację aplikacji.

Nie modyfikować oryginalnych postaci, rigów, animacji ani ich tekstur. Nie scalać
na ślepo gałęzi miasta lub osiedla. Zachować projekt graficzny, oryginalne
PBR 2K blisko kamery, 4K panoramę i stałą skalę K=1.294.

## Faktyczne zmiany na tej samej gałęzi obozu
1. Włączono oficjalny wspólny system **BraxContent** z istniejącej gałęzi
   `claude/osiedle-42-poprawka` poprzez dokładnie te same Git bloby, wraz z
   `.meta`, a nie kopię nowo napisanego systemu. Dotyczy
   `Assets/Updates`, `Assets/Editor/Updates` (bez innych stref).
2. `Packages/manifest.json`: do zachowanych pakietów dodano
   `com.unity.addressables=2.7.6`, `com.unity.modules.assetbundle=1.0.0`,
   `com.unity.modules.unitywebrequestassetbundle=1.0.0`.
3. `Assets/Claude/BraxStart.cs` używa warunkowo
   `BraxContent.Initialize`, `BraxContent.Begin`, aktywacji i walidacji sceny,
   a przy aktywnym zgodnym baseline pokazuje w **istniejących USTAWIENIACH**
   przycisk `WCZYTAJ AKTUALIZACJĘ`. Bez `BraxContentBase.json`
   nadal używa starego `SceneManager` i **nie zmienia wyglądu** ekranu.
   Właścicielski `regionTitle` jest zachowany.
4. `Assets/Editor/Oboz/BraxObozContentBase.cs`:
   tworzy i waliduje scenę obozu, używa wyłącznie oryginalnych assetów,
   tworzy jedną **niezmienną bazę Addressables** i
   `addressables_content_state.bin` oraz rejestr obozu. Nie kompiluje APK
   i nie podmienia identyfikatora aplikacji. Ponowne stworzenie już istniejącej
   bazy jest zablokowane.
5. `unity-tools/oboz/build_content_picker.sh` kompiluje oryginalny Android
   document picker w dokładnie przypiętym obrazie Unity 6000.0.58f2. Wynik
   `Assets/Plugins/Android/BraxContentPicker.jar` jest budowany
   deterministycznie; klasę w JAR trzeba zweryfikować w finalnym bazowym APK.
6. `.github/workflows/oboz-content-base.yml`: właścicielski, ręczny
   **eksport i kontrola bazowego content**, bez `BuildPlayer`, bez APK.
   Przed Unity sprawdza źródła, sumy i testy. Zapisuje wyłącznie
   raport i manifest (bez publicznego udostępniania prywatnych GLB).
7. `.github/workflows/oboz-content-update.yml`: wyłącznie ręczne
   `workflow_dispatch` na tej samej gałęzi, wyłącznie baza
   `oboz-[0-9a-f]{32}`, sprawdza jawne zmiany jednej sceny obozu
   `Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity`.
   `content_snapshot.py restore-base` przywraca oryginalną bazę i GUID.
   `BraxContentBuild.UpdateFromBaseline` generuje tylko **zmienione paczki
   Addressables**, nie wywołuje `BuildPlayer`. `publish-update` publikuje
   plik `.braxupdate.zip` do uwierzytelnionego wydania, nie do publicznego CDN.
8. `.github/workflows/oboz-unity-build.yml` nie dopuszcza uruchamiania
   zadania oddzielnego APK na tej gałęzi: job jest **skipped**, bez czasu runnera.
   Legacy builder `com.braxhorn.oboz` pozostawiono tylko dla innych dawnych
   gałęzi, aby nie niszczyć historii i nie zmieniać pozostałych wydań.
9. Dołączono oryginalny wspólny `content_snapshot.py` z testami z gałęzi
   osiedla (szyfrowanie, GUID, stan bazy, odrzucenie złego klucza i ścieżek).
   Testy kontraktu na obozie: `test_content_update_contract.py`.

## Oryginalne źródła obozu
Wersja robocza GitHub ID 435486352, tag `oboz-owner-remodel-20261105`:
5 poprawnych ZIP (CZESC_01..05) oraz oryginalny byk
`A_charging_bull_statu_0725030443_refine.glb` (2232404 B).
Model byka ma te same dane siatki i JPEG co 4x, końcowa skala jest w
Transform Unity. Bunkier jest całkowicie usunięty z generatora i planu,
archiwalne źródło nie zostało niszczone.
Dodatkowa błędnie nazwana kopia CZESC_03_Z_05.2.zip jest ignorowana.
Właściciel nie musi ponownie przesyłać plików.

## Zasady odbioru przez drugiego wykonawcę
- Uruchomić `python3 -m unittest discover -s unity-tools/oboz/tests -p 'test_*.py' -v`
  oraz `python3 -m unittest discover -s unity-tools/updates -p 'test_content_snapshot.py' -v`.
- Zweryfikować składnię, ustawienia Android, zależności Addressables 2.7.6,
  brak duplikatów GUID, fizyczne modele i prawidłowy PBR.
- Ręcznie uruchomić workflow `oboz-content-base.yml` na gałęzi obozu;
  sprawdzić Unity Editor log, `BraxContentBuild/base/manifest.json`,
  `addressables_content_state.bin` i raport sceny. **To jeszcze nie jest
  paczka aktualizacji działająca na telefonie.**
- Przed prawdziwą aktualizacją telefonu udowodnić, że zainstalowany
  **podpisany APK BRAXHORN CITY** ma dokładnie zgodny runtime, identyfikator
  aplikacji, bazę `oboz-...`, pliki Addressables i obsługę wyboru
  `.braxupdate.zip`. Jeżeli nie, potrzebne jest JEDNORAZOWE wdrożenie
  zgodnej bazy w głównej aplikacji (nie osobna aplikacja obozu).
- Dla kolejnego poprawionego pliku sceny przygotować
  `unity-city/content-patch.json` z jawną tablicą `edits`; uruchomić
  wyłącznie `oboz-content-update.yml`, podając **prawdziwe** `base_id`
  z wydanej bazy. Zmiana C#/pakietu/shadera może wymagać nowej bazy APK.
- Odbiór na Androidzie: aktualizacja **tej samej instalacji** bez reinstalacji,
  liczba pobranych bajtów, GUID, wejście do obozu, brak bunkra, wszystkie
  drzwi, bieżnie, ringi, worki, WC i zachowanie postaci; restart, offline,
  błędny/niepełny ZIP, cofnięcie przerwanej aktualizacji, pamięć RAM/VRAM i FPS.

## Co NIE jest jeszcze potwierdzone
**Brak uruchomienia nowego workflow Unity i brak potwierdzonej aktualizacji
obozu na telefonie.** Nie ma wydanej kompatybilnej bazy
`oboz-<32hex>` pod BRAXHORN CITY i zainstalowanego
`addressables_content_state.bin`. Nie ma nowego wygenerowanego pliku
`.braxupdate.zip`, zweryfikowanych FPS/RAM ani dowodu uruchomienia gry po
patchu. Nie wolno twierdzić, że aktualizacja telefonu działa, dopóki
te warunki nie przejdą odbioru.

Nie przestawiać gałęzi `main`, `gpt/oboz`, nie publikować surowych GLB
ani nie uruchamiać automatycznie długiego buildu.
