# Publikacja testowa Unity 6 WebGL

Źródłem jest **artefakt GitHub Actions**, nie `public/` ani zapisany w Git
`unity-preview/` (ten zawiera starą siłownię z commita `ec3fadaf`).
Repozytorium źródłowe nie jest gotowym wynikiem kompilacji: jego `vercel.json`
wyłącza automatyczny deploy i usuwa nieistniejące `assemble-assets.cjs`.
Nie zmienia to konfiguracji na `main` ani istniejącej produkcji.

Workflow `unity6-vercel-preview.yml`:

1. Pobiera wyłącznie udany `claude-region-build.yml` z gałęzi
   `claude/miasto-opus-20261031`, z podanym pełnym SHA i ID wykonania.
2. Sprawdza SHA256 archiwum GitHub, pochodzenie `release.json`, `playable.json`,
   transportu i wszystkich plików, kompletność części danych oraz gzip/WASM.
3. Umieszcza niezmieniony player w `unity-webgl/unity-gym/`. Zachowuje
   `<base href="/unity-gym/">`; Vercel przekierowuje `/` do `/unity-gym/`.
   Nie stosuje przekierowania brakujących assetów do HTML.
4. Testuje rzeczywiste uruchomienie Unity w Google Chrome z obsługą AAC.
   Zwykły Chromium Playwright nie zawiera wszystkich kodeków audio Unity.
   Błąd blokuje publikację.
5. Tworzy nową, niezależną gałąź `preview/unity6-webgl-<sha>-<run>-<attempt>`
   z samymi sprawdzonymi plikami i konfiguracją. Istniejąca integracja Vercel Git
   publikuje tę gałąź jako Preview. Brak statusu Vercel nie oznacza sukcesu.

Wygenerowany `vercel.json` ustawia framework `null`, pustą instalację,
`outputDirectory: unity-webgl`, a komendą build jest lokalny
`node verify-preview.cjs`. To kontrola gotowego artefaktu, nie kompilacja gry.
Komenda wymaga `VERCEL_ENV=preview` i odrzuca produkcję.

Pliki `.unityweb` i `.partNNN` pozostają binarne bez wymuszonego
`Content-Encoding`: istniejący loader Unity i `DecompressionStream` obsługują
rozpakowanie, a części nie są osobnymi strumieniami gzip. Zwykły `.wasm` ma
`application/wasm`. Nazwy plików `Build` zawierają SHA i mogą mieć immutable
cache; HTML i manifesty są rewalidowane.

Domyślne odtworzenie awarii: run `41351314859`, commit
`5623047e2713a25ffc885a2d3037b5f8727fd606`, artefakt `13105227332`.
To historyczny build z 1 listopada, nie najnowszy stan prac nad miastem.
Dla nowszego buildu trzeba wskazać odpowiadający mu udany run i SHA.
Nie mieszać plików z różnych kompilacji. Wygasły artefakt blokuje publikację.

Test lokalny konfiguracji (syntetyczne pliki, nie test gry):

```sh
python3 -m unittest discover -s unity-tools/vercel -p 'test_*.py' -v
```

Dowód uruchomienia gry pochodzi dopiero z `unity-webgl-smoke.json` i zrzutu
ekranu w artefakcie `unity6-vercel-evidence-<run>`. Udany test lokalnego serwera
nie zastępuje kontroli odpowiedzi i uruchomienia pod adresem Vercel.
Produkcja wymaga osobnej zgody i osobnej procedury.
