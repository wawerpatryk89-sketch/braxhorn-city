# Miasto Mistrzów — wykonana naprawa po audycie

5 listopada 2026, Europe/Zurich; pomiary i odczyty 4 listopada UTC. Gałąź: `codex/miasto-mistrzow-20261104`. Baza zmian: `a054a45b881c05f3f17f39d6e42f29a549480074`, zawierająca [audyt](MIASTO_MISTRZOW_AUDYT_20261105.md) kodu `38bece9` i rzeczywistego [błędu runu #6](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41924931920).

**Naprawiono potwierdzoną kolizję trasy kontrolnej metra i lukę testów. Przygotowano zabezpieczenia workflow i weryfikację podpisu przed Unity. Nie uruchomiono Unity, APK, ponowienia ani publikacji.**

Wykorzystano procedury Unity Essentials: diagnostyka błędu, bezpieczne zmiany narzędzi edytora, ochrona serializacji i walidacja buildu. Nie ma aktywnego połączenia Unity MCP ani lokalnego edytora. Poniższe wyniki nie są kompilacją C# w Unity ani testem telefonu.

## Próba diagnostyczna na późniejsze polecenie właściciela

Właściciel polecił próbę APK bez kontroli kolizji przejścia. W formularzu gałęzi ustaw `etap=apk`, `kontrola_tras=false`, `zdjecia=false`. To wywołuje osobną metodę BuildAndroidWithoutWalkChecks: pomija CheckWalk metra i CheckExteriorAccess areny/hotelu, zachowując fizyczne collidery, kontrolę konstrukcji stacji, poprawność zasobów, bezpieczny spawn gracza, serializację i podpis APK. Domyślnie kontrola tras pozostaje włączona. Raport zapisuje walkChecksSkipped=true i status z sufiksem WALK_CHECKS_SKIPPED. Sukces takiej próby nie jest odbiorem przechodniości.

## Wykonane zmiany

| Plik | Zmiana i jej powód |
|---|---|
| [BraxClaudeMetro.Reuse.cs](../../unity-city/Assets/Editor/Mistrzowie/BraxClaudeMetro.Reuse.cs) | Dwa punkty trasy przejścia przesunięto z lokalnego Z=30.90 na Z=31.25. Ściana kasy kończy się na Z=30.75, więc odległość wzrosła z 0.15 do 0.50 m przy promieniu kapsuły 0.35 m. Zachowano kasę, geometrię, promień i kontrolę kolizji. |
| [audit_metro.py](../../unity-tools/mistrzowie/audit_metro.py), [test_metro_regressions.py](../../unity-tools/mistrzowie/test_metro_regressions.py) | Audyt odczytuje cztery rzeczywiste collidery kasy z generatora. Łącznie obejmuje 32 bryły. Test odtwarza punkt i wymiary przeszkody z logu #6, wykazuje kolizję starej trasy i sprawdza poprawioną. |
| [pipeline.py](../../unity-tools/mistrzowie/pipeline.py), [prepare_assets.py](../../unity-tools/mistrzowie/prepare_assets.py), [test_pipeline.py](../../unity-tools/mistrzowie/test_pipeline.py) | Przed pobieraniem materiałów sprawdzane są zgodność workflow, wersja/rewizja Unity i obecność wymaganych źródeł. Aktualny kod uruchomiony ze starym workflow z main kończy się czytelną informacją o wyborze właściwej gałęzi. |
| [unity-build.json](../../unity-tools/mistrzowie/unity-build.json), [ProjectVersion.txt](../../unity-city/ProjectSettings/ProjectVersion.txt) | Jedna przypięta konfiguracja Unity 6000.0.85f2 / b028303d8e7d. Potwierdzono dostępność obrazów GameCI Android i base 3.2.2 oraz zapisano odczytane digesty. |
| [mistrzowie-apk.yml](../../.github/workflows/mistrzowie-apk.yml), [claude-region-build.yml](../../.github/workflows/claude-region-build.yml) | Oba ręczne workflow pobierają wersję i obraz z konfiguracji, używają nowego klucza cache, wykonują wczesną kontrolę podpisu dla APK i udostępniają tryb `sceny`. Do dowodów dołączono packages-lock.json, jeżeli wygeneruje go Unity. |
| [apk_delivery.py](../../unity-tools/mistrzowie/apk_delivery.py), [test_signing_preflight.py](../../unity-tools/mistrzowie/test_signing_preflight.py), [test_apk_delivery.py](../../unity-tools/mistrzowie/test_apk_delivery.py) | Nowy preflight sprawdza hostowe aapt/apksigner, alias braxhorn i możliwość użycia istniejącego klucza prywatnego przez podpisanie tymczasowego CSR. Nie zmienia keystore. Raportuje SHA-256 certyfikatu; manifest APK korzysta z przypiętej wersji Unity. |
| [BraxChampionsBuild.cs](../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs) | `ValidateScenes()` generuje, sprawdza i ponownie otwiera sceny, następnie kończy pracę przed BuildPlayer/bundles. Zapisuje oddzielny status bez deklaracji APK. Dodano odczyt szczegółowych raportów glTFast do source-imports.json; zwykły log wskazywał jedynie inspektor importera. |

Zmiana wersji silnika realizuje ustalenie audytu dotyczące oficjalnych poprawek Unity. Spójnie zmieniono projekt, oba workflow, obrazy, klucze cache i manifest dostawy. Nie wykonano importu/migracji w nowym edytorze. Poprzedniego cache Library z 6000.0.58f2 nie należy używać jako dowodu zgodności nowej wersji.

W tej naprawie nie zmieniono wyglądu scen, modeli, tekstur, układu miasta, areny, sterowania ani aplikacyjnego identyfikatora. Poprawka niezapisanej sceny z 268f76d pozostaje w kodzie i była wykonana skutecznie w #6. Kontrola areny nadal wymaga dojścia do tego etapu w Unity; run #6 zatrzymał się wcześniej.

## Dowody lokalne

| Kontrola | Wynik i ograniczenie |
|---|---|
| Regresja metra przed zmianą trasy | Po dodaniu kasy test starej bieżącej trasy nie przeszedł: ściana północna i front kasy. To odtworzenie przyczyny #6. |
| Regresja metra po zmianie | 12/12 testów przechodzi, również test zachowujący starą trasę jako przypadek błędny. Audyt 32 brył przechodzi. |
| Cały zestaw Miasta Mistrzów | **67/67 testów Python PASS**. Wcześniej 51. Nowe 16 testów: 4 metra, 7 zgodności pipeline, 5 podpisu. |
| Parser C# | **14/14 testów PASS; 274 pliki, 0 błędów składni/zakresów** na przypiętych tree-sitter 0.25.2 i tree-sitter-c-sharp 0.23.5. To nie sprawdza rozwiązywania typów, API Unity, IL2CPP ani shaderów. |
| Podpis | Testy wykorzystują prawdziwy keytool i jednorazowy testowy klucz JKS. Sprawdzono poprawny klucz bez zmiany jego bajtów, błędne hasło, brak aliasu, wpis zawierający sam certyfikat i brak narzędzia Android. Androidowe binaria w testach zastąpiono atrapami. Sekrety użytkownika nie były odczytywane lokalnie. |
| Spójność konfiguracji | Lokalne pipeline.py przechodzi na rzeczywistych plikach; wersja, rewizja i wymagane pliki checkoutu są zgodne. |
| Workflow | actionlint 1.7.12: oba zmienione workflow bez błędów, opcjonalne shellcheck/pyflakes wyłączone. Osobno wszystkie bloki run przechodzą bash -n. Sprawdzono kolejność preflight → podpis → przygotowanie → Unity. |
| Brak samoczynnej kompilacji | Oba workflow pozostają wyłącznie workflow_dispatch. Przejrzano wszystkie 54 workflow; automatyczne wyzwalacze push nie obejmują tej gałęzi. |
| Żuraw | [Pełny wynik Khronos glTF Validator](harbor-crane-validator-20261105.json), 2.0.0-dev.3.10: **0 błędów, 0 ostrzeżeń, 4 informacje o nieużywanych teksturach**. Nie jest to potwierdzenie importu w glTFast. Model pozostał nietknięty. |
| Diff | git diff --check bez błędów. |

Łącznie **81 testów Python** (67 projektu + 14 parsera). Kontrole geometrii, materiałów i krajobrazu wykonane w poprzednim audycie zachowują opisane tam ograniczenia. Nie przedstawiamy historycznych 677 próbek jako nowego pomiaru Unity.

## Nierozstrzygnięte rzeczy

- **Import harbor_crane.glb:** potwierdzony błąd glTFast z wcześniejszych logów nadal nie ma ustalonej przyczyny. Poprawny GLB nie wyklucza ograniczenia importera. Nowy raport odczytuje te same serializowane pola, co inspektor oficjalnego glTFast 6.20.0. W poprzednim runie ten błąd nie uniemożliwił wejścia w BuildAndroid, a generator Miasta Mistrzów nie odwołuje się do żurawia.
- **Pełna fizyka metra i areny:** analityczne 32 bryły nie obejmują wszystkich prywatnych siatek bramek ani wszystkich colliderów gotowej sceny. Wszystkie kontrole Physics i ponownego otwarcia sceny pozostają aktywne w Unity.
- **Nowy silnik, pakiety i GUID:** brak wykonanego importu w 6000.0.85f2. packages-lock.json i brakujące stabilne .meta trzeba pozyskać z właściwego projektu Unity; nie utworzono fikcyjnego locka ani nowych GUID dla istniejących odniesień. Sam artefakt locka nie oznacza jeszcze zapisania go w repo.
- **Android i dostawa:** IL2CPP, Gradle, końcowy podpis APK, publikacja/pobranie i instalacja na telefonie nie zostały wykonane. Preflight sprawdza narzędzia hosta; nie jest testem całego toolchaina kontenera Unity.
- **Odbiór gry:** wygląd, animacje, sterowanie, pamięć i FPS wymagają uruchomienia sceny/telefonu. Zakres wizualny pozostaje nieukończony zgodnie z przekazaniem projektu.

## Następny krok po decyzji właściciela

Kod daje podstawę do **kontrolnego uruchomienia Unity**, a nie do deklaracji gotowego APK. Najpierw wybrać workflow `Miasto Mistrzów — APK`, **Use workflow from: `codex/miasto-mistrzow-20261104`**, etap **`sceny`**, zdjęcia wyłączone. Pole commit może zostać puste: w tym wariancie kod jest przypięty do SHA zdarzenia gałęzi. Dla jednoznacznego powtórzenia użyć pełnego SHA commita zawierającego ten raport i poprawki, odczytanego z historii gałęzi — nie 47c89f0 ani 268f76d.

Etap `sceny` nadal pobiera/importuje/generuje zasoby i może trwać wiele minut. Nie został uruchomiony. Musi potwierdzić kompilację skryptów, importy, trzy stacje, arenę, Patryka i ponowne otwarcie scen. Sprawdzić unity-validation.json i source-imports.json. Dopiero po wyniku i kolejnej decyzji właściciela przystąpić do etapu `apk`.

`main` nie został zmieniony. Dlatego jego stary formularz nadal może pokazywać 47c89f0. Aktualny kod odrzuci stary pipeline przed pobieraniem zasobów, ale nowe zabezpieczenie nie może zadziałać w starym kodzie 47c89f0. Trzeba wybrać właściwą gałąź workflow.

Źródła sprawdzenia narzędzi: [Unity 6000.0.85f2](https://unity.com/releases/editor/whats-new/6000.0.85f2), [obrazy GameCI](https://hub.docker.com/r/unityci/editor/tags?name=ubuntu-6000.0.85f2), [Khronos glTF Validator](https://github.com/KhronosGroup/glTF-Validator), [oficjalny rejestr glTFast](https://packages.unity.com/com.unity.cloud.gltfast). Źródła komunikatów bezpieczeństwa i wcześniejsze dowody CI są w raporcie audytu.
