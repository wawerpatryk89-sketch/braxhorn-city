# Patryk — wyłącznie 22 oryginalne FBX, 6 listopada 2026

## Nie podmieniać źródeł
- Model: **Patryk(4).zip**; SHA256 `52d7f58de8ea38f269a3d5c2b24b9667c6c8a7eb36d3784ad0366f0f3ac44b47`.
- Animacje: **Patryk anima(6).zip**; SHA256 `033e5cacea9003ba05d8a05be744d55c3376c32c44e9bab894b1e43cc198685a`.
- Wszystkie 22 FBX są z tego najnowszego pliku, sprawdzone bajt po bajcie względem `patryk-owner-22-manifest.json`.
- Sześć FBX w starym prywatnym imporcie ma dokładnie te same hashe co najnowsza paczka; nie jest to podmiana animacji.
- Zabronione: Brax/Nyxa/Monika jako dawcy, proceduralny chód, deformacja modelu, silent fallbacks.

## Co jeszcze wymaga pojedynczego uploadu
Pozostałe 16 FBX są zabezpieczone w **jednym** 2 616 003-bajtowym pliku:

`BRAXHORN_PATRYK_EXTRA16_ENCRYPTED_20261106.zip`

SHA256: `8c6b367f6e780780bd20b008b92b24a6c43ee11b282b68314198d06dd6f542e3`.

Dostępny wyłącznie w załączniku rozmowy i lokalnym środowisku użytkownika. **Nie jest jeszcze w repo GitHub.** Repozytorium publiczne: nigdy nie przesyłać tam jawnych FBX.

**Miejsce docelowe pojedynczego uploadu GitHub** (bez rozpakowywania):

`unity-city/Prywatne/BRAXHORN_PATRYK_EXTRA16_ENCRYPTED_20261106.zip`

**Wyłącznie gałąź:** `codex/osiedle-audyt-naprawa-20261106`.

Importer `unity-tools/claude/import_patryk_extra16.py` korzysta z dotychczasowego szyfrowanego prywatnego klucza CI i sekretu `KLUCZ`. Używa RSA-OAEP SHA256 i AES-256-CBC PBKDF2 200000. Sprawdza hash zaszyfrowanej paczki, szyfrogramu, 16 FBX oraz ich zgodność z manifestem całej najnowszej paczki 22. W `prywatne.py` cały import Patryka przerywa się, jeśli któregokolwiek z 22 plików brakuje.

## Zasady renderowania i szkieletu
Model i oryginalne skale kości bez zmian. Color oraz Normal ze źródła 4K, ORM generowany w 2K, bez upraszczania twarzy. Idle z najspokojniejszej klatki oryginalnego chodu Patryka (brak dedykowanego Idle FBX), CarIdle z końcowej pozy oryginalnego `Entering Car`. Dla różnic pozy bind FBX ruch jest przenoszony jako **lokalna rotacja względna** na szkielet Patryka, bez syntetycznych animacji.

## Status odbioru
- SHA256 wszystkich 22 plików — **potwierdzony**.
- Przetestowano lokalnie round-trip szyfrowania AES i zgodność paczki 16 FBX — **potwierdzone**.
- Transfer zaszyfrowanych danych na GitHub — **niezakończony**.
- Import 22 FBX w edytorze Unity, montaż kości i odtwarzanie animacji — **niepotwierdzone**.
- Nowy APK / telefon — **nieuruchomione**.
- Ruch samochodów i głowy pasażerów — poprawki w źródle, **weryfikacja w urządzeniu otwarta**.

Nie udawać wyniku testu z telefonu i nie uruchamiać pełnego Android APK bez zatwierdzonej kompletnej integracji źródeł.
