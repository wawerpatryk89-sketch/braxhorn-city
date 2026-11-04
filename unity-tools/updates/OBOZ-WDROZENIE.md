# Obóz — zastosowanie standardu aktualizacji, 4 listopada 2026

Standard: `STANDARD-AKTUALIZACJI.md`, niezmieniona kopia z commita
`38422c5f031b5f9b85a3e1981c1a8199c9f75731`. Git blob:
`9fd6e212fb4baa72f453af01f316583ccfecdaa4`.

Baza odzyskanego projektu: `d533aa8128271c94087d34e06209db0cab3ff516`.
Lokalny commit przed tym etapem: `2b833ae` plus zachowane, wcześniejsze zmiany
zabudowy zaplecza. Ten etap nie zastępuje pozostałej pracy nad obozem.

## Wykonane w źródłach

- Zapisano obowiązujący standard i reguły kontynuacji w `AGENTS.md`.
- `BraxObozStableMesh` zastępuje numerację `Mesh_0`, `Mesh_1` identyfikatorem
  SHA-256 zależnym od geometrii. Ponowne użycie tej samej siatki nie zapisuje jej
  ponownie. Zmiana kolejności generowania nie zmienia jej ścieżki. Stare zasoby
  pozostają na dysku; nic nie usuwa ich ani ich `.meta`. To dotyczy statycznych
  siatek zabudowy, podłoża, oktagonu i lin, a nie przebudowy całej sceny.
- `BraxObozUpdateRegistry.Export` zapisuje rejestr sceny, siedmiu prefabów,
  zależności i ich `.meta`, GUID, SHA-256 i rozmiarów. Uwzględnia cały kod C#
  runtime i edytora, shadery, ustawienia i blokadę wersji pakietów. Nie wyłącza
  metod runtime z kontroli zgodności. Odrzuca eksport scen innych stref.
- `tools/snapshot_oboz_updates.py` sprawdza rejestr i tworzy niezmienną kopię
  plików. Kontroluje rzeczywiste bajty archiwum, wymagane metadane, dokładną obsadę,
  zależności oraz zachowanie adresów i GUID względem poprzedniego eksportu.
  Nie nadpisuje wcześniejszej kopii. Odrzuca także nowy kod dodany po sporządzeniu
  rejestru, uszkodzone wejścia i ścieżki wychodzące poza projekt.

To przygotowanie trwałych źródeł do integracji. **Nie jest to działający system
Addressables ani aktualizacja telefonu.** Eksport ma jawnie
`readyForContentUpdate=false` i `runtimeVerified=false`.

## Stałe adresy obozu

| Adres | Plik w projekcie Unity | Stan |
|---|---|---|
| `oboz/scenes/wejscie` | `Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity` | Zarezerwowany; scena nadal robocza |
| `oboz/characters/monika` | `Assets/PlayableGenerated/Oboz/monika/CharacterCached.prefab` | Monika, gracz |
| `oboz/characters/vorn` | `Assets/PlayableGenerated/Oboz/vorn/CharacterCached.prefab` | NPC |
| `oboz/characters/zyro` | `Assets/PlayableGenerated/Oboz/zyro/CharacterCached.prefab` | NPC |
| `oboz/characters/rakk` | `Assets/PlayableGenerated/Oboz/rakk/CharacterCached.prefab` | NPC |
| `oboz/characters/kade` | `Assets/PlayableGenerated/Oboz/kade/CharacterCached.prefab` | NPC |
| `oboz/characters/gorr` | `Assets/PlayableGenerated/Oboz/gorr/CharacterCached.prefab` | NPC |
| `oboz/characters/mis` | `Assets/PlayableGenerated/Oboz/mis/CharacterCached.prefab` | NPC; odchylenia jakości nadal niezatwierdzone |

Adres wejścia pozostanie stały po podziale zabudowy na sektory. Nie wolno
publikować obecnej całej sceny jako jednego wielkiego bundle.

## Pierwszy eksport w Unity

1. Użyj Unity `6000.0.58f2` i istniejącego importu prywatnych zasobów. Przygotuj
   scenę obozu i prefabów przez istniejący proces; nie zastępuj innych stref.
2. Zapisz sceny. Uruchom `BraxObozUpdateRegistry.Export` z procesu Unity albo
   menu `BRAXHORN → Obóz → Aktualizacje → Zapisz rejestr scen i zależności`.
   Sam eksport nie uruchamia generatora ani buildu i nie zmienia Build Settings.
3. Z katalogu repo uruchom:

   ```sh
   python3 tools/snapshot_oboz_updates.py --output unity-city/ObozUpdateExport/oboz-source-001.zip
   ```

   Przy kolejnej kopii podaj `--previous` ze ścieżką do poprzedniego
   `registry.json` i nową nazwę archiwum. GUID/powiązania nie mogą się zmienić
   niejawnie. Zachowaj rejestr razem z archiwum.
4. Archiwum zawiera prywatne modele i mapę. Przekaż je dotychczasową prywatną
   ścieżką, z szyfrowaniem przed publicznym repo. Nie jest to paczka CDN ani
   `addressables_content_state.bin`. Plików `.meta` nie generuj od nowa przy
   odtwarzaniu; importuj je razem z zasobami.

## Pozostałe wymagania standardu

| Wymaganie | Rzeczywisty stan |
|---|---|
| Stałe sceny/prefaby i komplet metadanych | Kod zapisu/eksportu przygotowany; pierwszy eksport Unity niewykonany |
| Sektory z osobnymi wejściami i zależnościami | Niewdrożone; generator nadal odtwarza scenę obozu |
| Cache importu/Bee, raport trafienia i czasu | Podłączone w kodzie CI obozu; nie uruchomiono i nie zmierzono przyspieszenia |
| Jedna wspólna wersja narzędzi wydania | `unity_cache.py`, `durable_cache.py`, `world_checkpoint.py` i testy przeniesione bez zmian z `1d83c1a6db4f6c71eb670e5a57bab963ce50e5c1` |
| Addressables, grupy/paczki i analiza duplikacji | Niewdrożone; pakiet nie jest dodany do manifestu obozu |
| Pobieranie, postęp, błędy, offline i zwalnianie scen | Niewdrożone |
| HTTPS/CDN i zasady dostępu prywatnych zasobów | Nieustalone; nie wymyślać adresu ani nie używać tokenu GitHub w grze |
| Baza APK i właściwy content state | Brak; archiwum źródeł nie zastępuje bazy |
| Test zmiany jednego elementu bez nowego APK | Niewykonany; wymaga zintegrowanej bazy i telefonu |
| Brak sieci, przerwanie, uszkodzenie paczki, rollback, zapisy, RAM | Niewykonane na telefonie |

## Weryfikacja pierwotnego etapu standardu

- 13 nowych testów kopii: pełne bajty i metadane, stary rejestr, GUID, brak
  zależności, duplikaty, dokładna obsada, niezmienność kopii i ograniczenie ścieżek.
- Łącznie 26 testów narzędzi przeszło. Początkowo brakowało modułu `png` w
  domyślnym interpreterze; użyto istniejących zależności z poprzedniego etapu.
- Kontrola integralności: 278 źródeł; parser gramatyki: 19 plików C#, bez błędów.
- Kontrola białych znaków zmian własnych bez błędów. Oryginalny standard ma
  pustą linię na końcu; zachowano ją, aby kopia miała identyczny Git blob SHA.
- **Nie uruchomiono Unity, nie skompilowano C# w Unity i nie sprawdzono nowych
  identyfikatorów/rejestru na rzeczywistej wygenerowanej scenie.** Nie ma tu
  edytora ani kompilatora C#. Żaden wynik testów Pythona nie zmienia tego statusu.

Dokumentacja API: [GetDependencies](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.GetDependencies.html),
[AssetPathToGUID](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.AssetPathToGUID.html),
[Content update builds](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/content-update-builds-overview.html).
