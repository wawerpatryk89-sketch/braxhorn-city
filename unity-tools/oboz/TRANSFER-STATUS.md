# Obóz — dostawa zakończona, pierwszy APK zlecony (4 listopada 2026)

## Potwierdzona dostawa

- Gałąź: `gpt/oboz`. Zakres: tylko obóz; wygląd i kod generatora bez zmian.
- Odbiór Actions: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41755149298 — sukces.
- Zlecenie dostawy: commit `7cf6b9ebad014c6aa731ba2e6fbb8326ec13efcb`.
- Wysłano 7 oryginalnych paczek CMS do szkicu `oboz-transfer-key-41749904696`. Nazwy, rozmiary i SHA-256 zgodne z `transfer-ready.json`.
- Actions odszyfrowało i sprawdziło 204 pliki: 1 148 328 099 bajtów źródeł. To nie jest rozmiar APK ani RAM.
- CI zaszyfrowało źródła istniejącym narzędziem i sekretem oraz zapisało szkic `oboz-private-34114c7950bc`.
- 7 docelowych paczek: 1 076 328 272 bajtów. Kontrolne pobranie w CI: `readbackVerified=true`.
- Artefakt `oboz-delivery-proof`, ID `13287662593`, SHA-256 ZIP: `169e4085046a354592592cfdff83d4079923cb31b3346268a15efb2c031c7832`.
- Manifest z artefaktu ma dokładnie te same wpisy i sumy wszystkich źródeł co poprzedni manifest; zmieniły się tylko informacje o gotowych paczkach.
- SHA-256 nowego `private-inputs.json`: `57ccbb5fb17b248ff39a91abd549e5474796da3f102a86c08ae4f30f0f3a62f8`.

## Build

W tym samym commicie zapisano manifest i `build-request.json` z `firstInstall=true`.
Zmiana tego pliku wyzwala `.github/workflows/oboz-unity-build.yml` wyłącznie na `gpt/oboz`.
Unity `6000.0.58f2`, Android ARM64 / IL2CPP, metoda `BraxObozBuild.BuildAndroidReview`.
Wynik kompilacji i APK nie jest jeszcze potwierdzony. Sprawdzić najnowszy run na tej gałęzi.
Nie uruchomiono ani nie restartowano buildów miasta/osiedla. Nie zmieniono `main`.

## Pozostaje do potwierdzenia

- Preflight sekretów CI i odtworzenie zasobów.
- Rzeczywisty build Unity, stały podpis i wersja APK, zapis osobnego wydania.
- Uruchomienie na telefonie, wygląd i sterowanie Moniką.
- Działające aktualizacje zawartości bez nowego APK pozostają niewdrożone; cache nie zastępuje Addressables.

Nie powtarzać importu dostawy bez potrzeby. Oryginalne transportowe paczki, klucz odbiorcy,
docelowe paczki i artefakt dowodu pozostają zachowane. Nie publikować szkiców ani surowych źródeł.
