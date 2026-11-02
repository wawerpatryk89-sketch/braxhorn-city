# Wynik naprawy Vercel — 2 listopada 2026

Konfiguracja wdrażania została poprawiona na `claude/miasto-opus-20261031`.
Nie zmieniono plików gry, scen Unity ani produkcji/main.

## Opublikowany podgląd

- URL: https://braxhorn-city-git-preview-unity6-webg-e7466a-wawerpatryk89-1409.vercel.app/unity-gym/
- Vercel deployment: `dpl_3SRm7m9aj1hCh9djDn4kW6cA8jtF`.
- Status Vercel zapisany na GitHubie: `success` / `Deployment has completed`.
- Gałąź z gotowym wynikiem: `preview/unity6-webgl-5623047e2713-41430848000-1`.
- Commit publikacji: `e03b28b38ae1ae1d1b006cef7c809d6924f568ec`.
- Źródło gry: `5623047e2713a25ffc885a2d3037b5f8727fd606` (historyczny build wskazany w zgłoszeniu).
- Kompilator: Unity `6000.0.58f2`, WebGL; źródłowy run `41351314859`.
- Artefakt źródłowy: `13105227332`, ZIP SHA256 `6db71f5a055217d75a8df4d8accd51b6eefe8a0917920b1c65dc9ce3f181ac65`.

## Dowody

Udana walidacja i publikacja:
https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41430848000

Dowód (raport, screenshot, HTTP log i deployment-proof):
https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41430848000/artifacts/13134491512

- 8 testów pakowania i zabezpieczeń: PASS.
- SHA256 archiwum i pochodzenie artefaktu: PASS.
- 64 pliki w spisie oryginalnego buildu: PASS.
- 22 części danych: PASS; 734926024 B skompresowane, 880716660 B po rozpakowaniu.
- Kontrola WebAssembly, loadera, ścieżek i manifestów: PASS.
- Uruchomienie dokładnie tego playera na serwerze testowym w Google Chrome 156.0.8078.79: PASS.
- Raport: `runtimePassed: true`, `errors: []`, `failedRequests: []`, `consoleErrors: []`;
  instancja Unity i `SendMessage` działają, canvas 1280×720, 25 zasobów Build.
- Poprzednie próby w Chromium wykryły błąd audio AAC; test wykonano następnie
  w Chrome obsługującym ten kodek, bez ignorowania błędów i bez zmian w playerze.

## Ograniczenie weryfikacji docelowego serwera

Publikacja Vercel zakończyła się sukcesem, lecz test HTTP/przeglądarki bezpośrednio
na chronionym podglądzie nie został zaliczony. Żądanie proof JSON otrzymało HTML,
a autoryzowany connector Vercel potwierdził `403 forbidden` na odczycie dostępu
do tego deploymentu (`read_protection_bypass`). Projekt/team nie są dostępne
przez obecne połączenie Vercel. Nie wyłączono ochrony wdrożenia.

Kontrola docelowego serwera:
https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41431818305

Do ukończenia live QA potrzebny jest dostęp połączenia Vercel do projektu
`braxhorn-city` w zespole `wawerpatryk89-1409`. Nie potwierdzono nagłówków
docelowego serwera ani działania na fizycznym iPhonie. Udany test Chrome na
runnerze nie jest przedstawiany jako zaliczony test pod chronionym URL.
