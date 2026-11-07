# BRAXHORN CITY — Content Delivery Architecture V2

Status: **obowiązująca specyfikacja implementacyjna**.  
Silnik: Unity 6, Android mobile-first.  
Cel: jeden mały Player/Core + wersjonowane pakiety regionów + automatyczne pobieranie i aktualizacje bez ręcznych ZIP-ów.

## 1. Inwarianty

- Zatwierdzony wygląd, geometria, materiały, skala, UV i jakość źródeł nie mogą się zmienić z powodu migracji architektury.
- Gałąź Git nie jest paczką runtime.
- Zielony build nie oznacza działającego content delivery.
- Region nie jest „V2-ready”, dopóki nie przejdzie testu Android: pierwszy download, restart, cache offline, przerwany transfer i rollback.
- Kod/IL2CPP/PlayerSettings/pakiety Unity => nowy Player baseline.
- Dane kompatybilne z istniejącym runtime => content update.

## 2. Warstwy

### Core Player
Tylko:
- bootstrap scene,
- UI/menu/ustawienia/save,
- Content Manager,
- Download Manager,
- obsługa błędów i rollback,
- minimalne lokalne shadery i zasoby wymagane przed pobraniem.

Nie zawiera pełnych światów, wnętrz, ciężkich 2K/HD tekstur regionu, dużego audio ani przypadkowych zależności scen regionu.

### Shared Runtime
Tylko assety faktycznie współdzielone przez >=2 rejony. Przykłady:
- wspólne postacie,
- wspólne animacje,
- wspólne pojazdy,
- wspólne materiały/shadery,
- wspólne UI/audio, jeśli rzeczywiście używane w wielu regionach.

Shared nie jest „folderem na wszystko”. Każdy wpis musi mieć dowód zależności.

### Region
Nazwy logiczne:
- region-miasto-r1
- region-osiedle
- region-oboz
- region-miasto-mistrzow
- przyszłe: region-port, region-wyspa, itd.

W dużym regionie dopuszczalne podgrupy:
- scene
- environment-buildings
- interiors
- vegetation
- audio
- optional-hd

Podział ma minimalizować koszt aktualizacji bez tworzenia duplikatów.

## 3. Budżet bundle

To budżet projektu, nie ograniczenie Unity:
- cel typowego bundle: 32–96 MiB,
- >128 MiB: obowiązkowy raport przyczyny i kosztu aktualizacji,
- optional-HD może być większy, ale osobno i świadomie.

Nie tnij bundle mechanicznie. Najpierw Analyze/Build Layout i graf zależności.

## 4. Addressables

Standard: Addressables dla regionów i zdalnego contentu.

Każdy release contentu przechowuje:
- Unity version,
- Addressables version,
- source commit,
- zone,
- baseId,
- runtimeId,
- version,
- catalog path/hash,
- addressables_content_state.bin dla bazy,
- GUID-y scen,
- listę bundle,
- bytes + SHA-256 dla każdego artefaktu.

Katalogi i bundle są immutable/versioned. Nie nadpisujemy aktywnych plików w miejscu.

## 5. Release Index

Player pobiera mały indeks środowiska, np.:

```json
{
  "format": 2,
  "channel": "dev",
  "regions": [
    {
      "zone": "oboz",
      "version": "2026.11.07.1",
      "baseId": "oboz-...",
      "runtimeId": "<64 hex>",
      "catalog": "oboz/2026.11.07.1/catalog.json",
      "catalogSha256": "<64 hex>",
      "downloadBytes": 123456789
    }
  ]
}
```

URL bazowy jest konfiguracją środowiska, bez tokenów/secrets w APK. W indeksie preferowane są ścieżki względne do zatwierdzonego hosta.

## 6. Runtime lifecycle

1. Start Core Player.
2. Odczyt ostatniej potwierdzonej lokalnej wersji.
3. Pobranie indeksu release, jeśli sieć dostępna.
4. Sprawdzenie zone/baseId/runtimeId.
5. Obliczenie wymaganych bajtów.
6. Pokazanie użytkownikowi statusu/progressu.
7. Pobranie do staging/cache.
8. Walidacja rozmiaru/hash/zgodności.
9. Załadowanie katalogu wersjonowanego.
10. Pobranie zależności Addressables.
11. Asynchroniczne załadowanie sceny regionu.
12. Po zdrowym starcie zapis wersji jako confirmed.
13. Dopiero później usunięcie niepotrzebnej starej wersji/cache.

Brak sieci:
- istniejąca confirmed version działa z cache,
- brak pierwszej wersji => czytelny komunikat „region wymaga pobrania”.

## 7. Rollback

Przechowujemy:
- confirmed,
- previous,
- trial/pending.

Nowa wersja jest trial dopóki nie uruchomi regionu poprawnie. Nieudana inicjalizacja/scene load przy trial powoduje powrót do previous przy następnym starcie.

Nie kasuj previous przed potwierdzeniem trial.

## 8. Download Manager

Produkcja:
- zero file pickera,
- zero ręcznego ZIP/.braxbase/.braxupdate,
- progress w bajtach,
- timeout,
- retry z limitem,
- przerwany transfer nie narusza aktywnej wersji,
- kontrola wolnego miejsca,
- HTTPS,
- brak path traversal,
- SHA-256 przed aktywacją.

File picker może istnieć tylko jako jawny DEV/RECOVERY feature i nie może być domyślnym flow użytkownika.

## 9. DEV vs Google Play

DEV / sideload:
- Addressables + zatwierdzony CDN/hosting testowy,
- ten sam model regionów i manifestów.

Google Play:
- AAB + Play Asset Delivery dla install-time/fast-follow/on-demand tam, gdzie ma sens,
- PAD jest transportem, nie osobną architekturą świata.

Nie forkować struktury contentu na dwa niezależne systemy.

## 10. CI

Docelowe role:
- source-preflight
- content-base
- content-update
- player-release

source-preflight:
- szybki i statyczny.

content-base:
- buduje Addressables baseline/state,
- zapisuje manifest i raport,
- nie tworzy nowego Playera bez potrzeby.

content-update:
- korzysta z zatwierdzonego state,
- raportuje dokładnie zmienione bundle i bajty,
- nie wywołuje BuildPlayer.

player-release:
- tylko po zmianie runtime lub wyraźnym zleceniu właściciela.

Długie buildy nie startują automatycznie z push.

## 11. Raport obowiązkowy

Każdy region raportuje:
- Core Player bytes,
- region total bytes,
- bytes per bundle,
- top największych bundle,
- duplicate dependencies,
- Player/content duplication,
- changed bundles względem bazy,
- transfer bytes aktualizacji,
- load time,
- RAM/VRAM peak,
- wynik Android,
- commit + manifest SHA.

## 12. Kryteria odbioru V2

PASS tylko jeśli:
- Player startuje bez pełnego regionu przypadkowo wbudowanego,
- region ma własny katalog i bundle,
- shared dependencies nie są kopiowane bez potrzeby,
- gra pobiera content sama,
- przerwany download nie niszczy gry,
- hash/compatibility są sprawdzane,
- update danych nie wymaga nowego APK,
- kod/runtime change wymusza nowy baseline,
- cache offline działa,
- rollback działa,
- asynchroniczne load/unload sceny działa na Androidzie,
- właściciel ma dowód z urządzenia.

## 13. Migracja istniejących rejonów

Kolejność:
1. inventory/audit bez zmiany wyglądu,
2. Addressables dependency + wspólne kontrakty,
3. oddzielenie bootstrap od world content,
4. wyprowadzenie regionu z Playera,
5. shared dependency pass,
6. remote catalog + automatic downloader,
7. content update,
8. Android proof,
9. dopiero potem PAD/AAB production integration.

Nie przenosić czterech regionów naraz jednym niezweryfikowanym refactorem. Każdy region przechodzi ten sam kontrakt i dowód.
