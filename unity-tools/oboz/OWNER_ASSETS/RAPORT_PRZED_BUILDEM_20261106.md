# OBÓZ — raport przygotowania przed testowym APK

Stan na 2026-11-06, obszar: wyłącznie **BRAXHORN OBÓZ TRENINGOWY**, Unity 6000.0.58f2, Android. Gałąź: `codex/oboz-przebudowa-pbr-20261105`.

## Fakty i dowody

- **Oryginalne archiwum właściciela:** `BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip`, 345 273 037 B, SHA-256 `b7d1bd8dd2ed64a4e23b6fad8af2497d0355073b687ea16e48b03f045741d006`. Weryfikacja rzeczywistych bajtów archiwum lokalnie: 30/30 oryginalnych zasobów zgodnych co do długości i SHA-256, ZIP CRC PASS. 17 GLB ma siatki i poprawny nagłówek GLB2.
- **Siłownia i opony:** `oboz_silownia_opony.zip` 7 298 373 B, SHA-256 `8fee0543521a8ae6043a0c4179e26f2bcd69f1cd778904e2d7b177f7cb22661f`; jest w prywatnym draft Release 435486352. FBX zawiera wymagane 7 rodzajów urządzeń, atlas PNG 2048×2048. Rzeczywiste łóżko `ikea_idanas_single_bed.glb` 5 374 568 B, SHA-256 `26efbc9f32157c909d1cd9bde9b1692e4734728a0dcde7466739fe2ce5ff449a`, także jest w tym wydaniu.
- **Wcześniejszy podpisany APK #22:** draft `oboz-review-latest` ID 433777528 zawiera `braxhorn-oboz-216084570-b9e1ba0.apk` 868 481 053 B, SHA-256 `2e07e2f31dbfb85b003472ed53c18357c554c192a5be887638d40d2c021c0b30`. Dodatkowo istnieje `release-identity.json` jako wskaźnik. Ten starszy APK nie zawiera ostatnich zmian i nie jest dowodem aktualnego działania. Waga znacznie przekracza preferowany budżet mobilny, wymaga testu instalacji i pamięci.
- **Szybki CI bez Unity/APK:** run [42228637941](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42228637941), SHA `367d9fbdf096607b3089d6e5194e4ac0fe7dccf2`: 159 testów `unity-tools/oboz/tests`, 2 testy treningu, 27 plików C# w parserze zakresów z 0 błędów — wszystkie zakończone sukcesem. Nie była to kompilacja C# w Unity ani test telefonu.

## Naprawy przed kolejnym Unity

1. Cztery domki i stołówka używają **pełnego oryginalnego zestawu 3D kuchni polowej**, a nie tylko części: stół z nogami, śrubami i półką, kuchenka z palnikami i rusztami, butla z zaworem, wieszak z hakami, zlew z rzeczywistą baterią, korkiem i nogami. Wszystkie 22 wymagane węzły występują w dostarczonym oryginalnym GLB i w preflighcie.
2. Skorygowano **raport sanitariatów**, żeby nie traktował czterech dodatkowych kranów w domkach jako błędu liczby kranów w murowanej łaźni.
3. Workflow nie żąda już identycznego APK **#18**, gdy istnieje podpisany APK **#22**. Przed Unity nadal wymaga poprawnego `applicationId`, niezmiennego certyfikatu, poprzedniego `sourceCommit`/`sourceRun` i rosnącego `versionCode`. Po kompilacji rzeczywiste APK nadal musi przejść `aapt`/`apksigner`.
4. Kolizje, NavMesh, ścieżki i wygląd pozostają **włączone w grze** i oceniane za pomocą raportów `collider-review.json`, `navigation-review.json`, `scene-validation.json`, `quality-review.json`. Nie traktować ostrzeżeń jakościowych jako twardego FAIL. Prawdziwe błędy Unity/importu/podpisu nadal blokują.
5. **APK FIRST:** po udanym podpisie pakiet jest zabezpieczany w GitHub Actions; dodatkowe przekazanie do draft Release/backup i zapis cache nie mogą unieważnić prawidłowego pliku.

## Stan odbioru i ryzyko

- **ZROBIONE/ZWERYFIKOWANE:** zgodność źródłowych 30 plików i manifestu, obecność dodatkowego ZIP, oryginalne podzespoły GLB, statyczne testy C#/Python, aktualizacja bazowego warunku APK, odblokowany tryb jakości raportowej.
- **NIEZWERYFIKOWANE:** faktyczne otwarcie projektu Unity po zmianach, poprawne importowanie 3 oryginalnych plików Blender do GLB, tekstury oraz UV na scenie, wydajność i orientacja wszystkich 3D, kolizje i działanie NPC w Play Mode, podpis NOWEGO APK, kompatybilność instalacji na telefonie.
- **OTWARTE WYMAGANIA UŻYTKOWNIKA:** drugi odmienny model skórzanego worka nie został jednoznacznie dostarczony; oryginalne zwoje lin nie są jeszcze dowodem gotowego ćwiczenia na długich linach; aktualizacja `.braxupdate.zip` nie jest potwierdzona jako instalowalna na urządzeniu. Nie deklarować „wszystko ukończone”.
- **Potencjalny problem Android:** poprzedni APK ma 868,5 MB, co przekracza wcześniejszy cel około 500 MB. Nie zmniejszać źródłowej jakości modeli ani tekstur bez osobnej decyzji właściciela; mierzyć nowy APK i pamięć na telefonie.

## Dozwolony następny krok

Jedno osobno autoryzowane, **ręczne** `workflow_dispatch` na gałęzi `codex/oboz-przebudowa-pbr-20261105`, parametry `first_install=false`, `content_action=none`, `quality_review=false` (pełny dodatkowy Play Mode nie jest wymagany do zwykłego APK). **Nie uruchomiono nowego buildu** podczas tych kontroli.

Jeżeli nowy testowy APK powstanie, zweryfikować SHA256, podpis i versionCode, udostępnić plik do pobrania oraz osobne raporty; nie oznaczać jakości sceny jako zaakceptowanej bez testów w Unity i na telefonie.
