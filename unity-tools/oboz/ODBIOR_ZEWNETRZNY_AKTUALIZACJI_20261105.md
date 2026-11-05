# BRAXHORN OBÓZ — odbiór zewnętrzny systemu aktualizacji (5.11.2026)

## Cel właściciela i stan wydania

**Jedna gra BRAXHORN CITY, Unity 6, Android.** Kolejne poprawki obozu mają aktualizować istniejącą grę bez każdorazowego przenoszenia pełnego APK i bez rozpoczynania całego świata od nowa. Nie zmieniać wyglądu obozu wbrew planowi, żadnych zmian modeli/rigów/animacji postaci.

**Gałąź do audytu:** `codex/oboz-przebudowa-pbr-20261105`.
**Stan:** źródła i transport 5 ZIP + oryginalny byk na GitHubie, kod i workflow aktualizacji przygotowane. **Nie zbudowano/nie przetestowano tutaj Unity, zgodnej bazy głównego APK, ani pliku .braxupdate.zip na telefonie.** Nie twierdzić inaczej.

## Co rzeczywiście zintegrowano

1. Źródłowy, współdzielony `BraxContent` i `BraxContentManifest` dla importu .braxupdate.zip, kontroli manifestu, kopii lokalnej, restartu i rollbacku; oryginalne pliki .meta/GUID zachowane. Backend z gałęzi `claude/osiedle-42-poprawka`, bez zmiany modułów miasta/osiedla.
2. `BraxContentBuild`, `BraxContentEdits` i `ContentUpdateScript.BuildContentUpdate` — nowa zawartość na podstawie niezmiennego pliku `addressables_content_state.bin`, a nie BuildPlayer.
3. Unity Packages: `com.unity.addressables: 2.7.6`, `com.unity.modules.assetbundle`, `com.unity.modules.unitywebrequestassetbundle`. Inne wcześniej przypięte zależności nie były zastąpione.
4. Istniejący `BraxStart.cs`: `BraxContent.Initialize/Begin` oraz przycisk **WCZYTAJ AKTUALIZACJĘ** tylko gdy aktywna baza zawartości. Gdy jej brak, działa stare wczytywanie sceny, zachowano `regionTitle`, logo, muzykę, inne ustawienia i układ ekranu.
5. `BraxObozContentBase.Export`: oryginalny generator obozu `BraxObozEnvironment.Prepare`, oryginalna obsada `BraxObozActors.Assemble`, kontrola fizycznej sceny, potem eksport oddzielnej zawartości Addressables `BraxContentBuild.Baseline("oboz", [jedna scena obozu])`. Brak `BuildPipeline.BuildPlayer` w nowym module.
6. Eksport ma osobną kopię źródeł i identyfikator bazy. Skrypt `unity-tools/oboz/content_candidate.py` weryfikuje jeden GUID sceny, katalog, wszystkie zależności, stan bazowy, identyfikator runtime, zgodność z wpisem Resources i brak APK. Zasoby prywatne archiwizowane wyłącznie w **zaszyfrowanej kopii**; nie dodawać do publicznego repo.
7. `BraxObozContentBase.UpdateInCI` uruchamia `BraxContentBuild.UpdateFromBaseline` na przywróconej, zaakceptowanej bazie. Mechanizm ma odrzucać inny kod, pakiety, GUID, stan projektu i brak jawnego `content-patch.json`.
8. Istniejący workflow `.github/workflows/oboz-unity-build.yml`: na tej gałęzi **stare zadanie APK nie jest dopuszczone**. Ręczne wejścia `content_action`: `preflight`, `export-candidate`, `update-from-base`. Domyślne `none` niczego nie uruchamia.
9. Oryginalne archiwum źródeł obozu: 31 wpisów, pięć ZIP-ów i oryginalny `A_charging_bull_statu_0725030443_refine.glb`. Importer weryfikuje zawartość SHA-256 i utrzymuje oryginalne modele 3D oraz mapy PBR (w tym 2K toru). Bunkier wyeliminowany z generatora.

## Wymagane dowody i sposób niezależnego testu

| Sprawdzany element | Co ma wykonać niezależny wykonawca | Stan w tej rozmowie |
|---|---|---|
| GitHub source | Porównać diff tego brancha względem bazowego, potwierdzić brak zmian postaci i innych stref | Zmiany w kodzie zapisane |
| Materiały | Sprawdzić wszystkie pięć ZIP i byka, pobrać i zweryfikować pełne SHA-256 | Nazwy i rozmiary na GitHub potwierdzone, lokalne SHA-256 wcześniej zweryfikowane |
| Preflight | Ręcznie uruchomić `content_action=preflight`; potwierdzić sukces testów Pythona | **Nie uruchamiano workflow** |
| Unity 6 Android | Ręcznie wybrać `content_action=export-candidate`; sprawdzić katalog, 1 scenę, GUID i `content_state`, brak APK | **Nie uruchamiano Unity** |
| Wspólna baza BRAXHORN CITY | Zintegrować potwierdzony baseline + importer w **podpisanym APK tej samej aplikacji**, bez nowego `com.braxhorn.oboz`; nie publikować tylko kandydatury bazowej | **Nie ma dowodu takiej integracji** |
| Aktualizacja danych | Zachować dokładny `base_id=oboz-<32hex>` z zatwierdzonej bazy i dodać jawny `unity-city/content-patch.json`; uruchomić `update-from-base` | **Nie ma jeszcze zatwierdzonej bazy** |
| Telefon Android | Sprawdzić import ZIP przez systemowy picker, zmianę tylko obozu, restart, offline, przerwanie, cofnięcie, kolizje, FPS/RAM/VRAM | **Nie wykonano** |

## Istotne ograniczenia

- Poprzednie buildy `com.braxhorn.oboz` są oddzielnymi aplikacjami testowymi i NIE są aktualizacją głównego BRAXHORN CITY. Klasa `BraxObozBuild` jest historycznie obecna, lecz na gałęzi przebudowy stary job APK jest wyłączony. **Nie zmieniać globalnego identyfikatora aplikacji przypadkowym nadpisaniem.**
- Kod nowego importera/scen C# wymaga raz bazy APK zawierającej ten kod, podpisanej dotychczasowym kluczem i o prawidłowym package ID. Addressables nie dostarcza nowych skryptów do już zainstalowanego APK.
- Kod `BraxContentPicker.java` znajduje się w projekcie, ale dla Androida trzeba jeszcze potwierdzić kompilację i dołączenie JAR do docelowej **wspólnej bazy APK**, nie tylko obecność źródła Java.
- `export-candidate` **nie publikuje bazy jako działającej**: tworzy lokalny content state i zaszyfrowany artefakt do audytu. Snapshot może zostać zatwierdzony dopiero w powiązaniu z faktycznym podpisanym APK, które umie go wczytać.
- `update-from-base` ma wymagać nienaruszonego runtime fingerprint i GUID. Gdy kod, API, schemat klasy, wersja Unity lub zależności się zmienią, prawidłową decyzją jest odbudowa nowej zgodnej bazy, a nie udawana aktualizacja danych.
- Dla pojedynczych przyszłych poprawek zaleca się dalszy podział obozu na sektory; obecna pierwsza baza ma jedną dużą scenę obozu. **Nie ma jeszcze gwarancji małej paczki** po zmianie elementu w takiej scenie.
- Prywatny draft GitHub nie jest publicznym CDN; w tej implementacji aktualizację użytkownik wskazuje lokalnie w USTAWIENIACH. Nie wprowadzać tokenu GitHub do aplikacji.
- Funkcjonalność postaci, trafienia, animacje, 3D Grass Medium z .blend, kompletność łóżek i sprzętu siłowni oraz wygląd premium wymagają niezależnego odbioru; integracja aktualizacji nie dowodzi ich jakości.

## Testy zapisane w repo, jeszcze nie uruchomione na GitHub Actions

- `unity-tools/oboz/tests/test_owner_intake.py` — integralność rozpakowania, oryginałów i GUID.
- `unity-tools/oboz/tests/test_owner_six_small.py` — odtworzenie z małych ZIP.
- `unity-tools/oboz/tests/test_bull_draft_delivery.py` — dokładny właścicielski byk/relacja do ronda.
- `unity-tools/oboz/tests/test_compact_owner_remodel.py` — geometria i bunkier.
- `unity-tools/oboz/tests/test_content_candidate.py` — katalog, stan, baza, scena, GUID.
- `unity-tools/oboz/tests/test_content_integration_contract.py` — jedna gra, importer warunkowy, brak APK dla tego brancha.

**Wniosek:** przekazać tę gałąź innemu wykonawcy do audytu i pierwszej próby preflight. Żadnego automatycznego APK ani zmiany wizualnego projektu. Nie deklarować gotowej, zainstalowanej aktualizacji, dopóki nie przejdzie realny test Android.
