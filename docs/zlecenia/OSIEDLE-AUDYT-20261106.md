# BRAXHORN CITY — audyt i naprawy osiedla (Unity 6, Android)

**Data:** 2026-11-06. **Gałąź:** `codex/osiedle-audyt-naprawa-20261106`.
**Zakres:** wyłącznie osiedle. Inne rejony i prywatne oryginały bez zmian.
**Odniesienie:** ostatni znany udany build osiedla `c012f167e10f0f722cf081dd990b133981deb519`, a nie nowy test APK.

## Fakty i dowody

- Źródła: własny model `Patryk(4).zip`, **ostatnia** paczka `Patryk anima(6).zip` (22 pliki FBX, SHA256 `033e5cacea9003ba05d8a05be744d55c3376c32c44e9bab894b1e43cc198685a`).
- Udany szybki proces **szyfrowania + odszyfrowania + weryfikacji 22/22**: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42149847404
- Utworzono prywatny transfer `unity-city/Prywatne/import-patryk-animations-20261106/`. Z bieżącej gałęzi usunięto jawny ZIP; **nie wymazano go z historycznego publicznego commita**.
- Poprzednie materiały wideo użytkownika (stary APK) pokazują: T-pose/sztywne ręce, różowy samochód, brak widocznych kół, pasażera wystającego ponad dach, drugą podobną do Patryka postać w sklepie.
- Kontrola kodu i szybkie testy Python + parser struktury C#: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42151258450 — PASS.
- **Żaden z powyższych wyników nie dowodzi poprawnego wyglądu animacji, shaderów ani fizyki w uruchomionej grze Unity.**

## Matryca usterek i status

| Podsystem | Poprawka lub kontrola w źródłach | Stan faktyczny |
| --- | --- | --- |
| Patryk, oryginalny model | Wyłącznie hash oryginalnego FBX; model, siatka i bindposes nietknięte | Kontrola źródła PASS; nie testowano Unity |
| 22 animacje Mixamo | SHA256 per FBX, 22/22 wymagane. Nie stosować Braxa/Moniki/Nyxy jako dawców | Import archiwum i odszyfrowanie PASS; szkielet/wizualizacja do testu |
| Chód/bieg / T-pose | Bez proceduralnego fallback; ruchy wyłącznie właściciela, różnice pozy bazowej przeniesione względnymi rotacjami | Kod zmieniony; wymaga Play Mode i filmu |
| Tekstury Patryka | 4K Color/Normal z oryginału, 2K ORM dla Androida | Konfiguracja źródeł potwierdzona; wygląd do testu |
| Wsiadanie i kierownica | Własny `Entering Car`, CarIdle z oryginału, odchylenie głowy i dopasowanie do fotela, sterowanie samochodem | Źródła i test kontraktu PASS; działanie w grze niepotwierdzone |
| Samochód przy ARENA | Przewidziany dokładnie jeden `BraxAutoGracza`, test kierownicy, drzwi i zatoki do 40 m | Wymaga wygenerowania sceny; istnieje ryzyko odrzucenia wszystkich modeli |
| Różowe samochody | Konwersja glTF/URP shaderów na projektowy Built-In; audyt niezgodnych materiałów | Kod zmieniony; nie przetestowano renderowania Android |
| Samochody bez kół | Każda bryła musi mieć 4 rzeczywiste siatki; skinned geometry jest wypiekana, nie usuwana | Kod zmieniony; nie przetestowano modeli w Unity |
| Pasażerowie przez dach | Odrzucenie bez siatki głowy, bez klipów i bez zapasu nad głową; brak próbek nie jest zapasem 0 cm | Kod zmieniony; wymaga weryfikacji sceny |
| Klakson i korki | Blokada ponawiania klaksonu w tym samym zdarzeniu, odstęp 30 s | Kontrakt kodu PASS; ruch 10 min i urządzenie niezweryfikowane |
| Drugie wcielenie Patryka | Generowanie ze źródła i odrzucanie starych scenowych instancji `Patryk`; raport wymaga dokładnie jednego gracza | Kod zmieniony; postać ze sklepu musi zostać wizualnie zidentyfikowana |
| Drogi / parkingi / rondo | Istnieją testy szerokości dróg, zatok, toru ruchu i blokowania przejść | Testu faktycznej sceny jeszcze brak |
| Sklepy / salon / schody / drzwi | Kontrole wejścia kapsułą, drzwi i ścian; diagnoza z błędami w `city-review` | Bez dowodu wejścia na telefonie |
| PULS CLUB / loże / murale | Raport sceny wymaga 14 lóż, oryginalnych szyldów, pełnego muralu | Kontrola kodu, nie widoku na urządzeniu |
| Tekstury i pamięć Android | 4K dla unikatowego modelu, 2K ORM, pozostałe mapy i draw calls wymagają pomiaru | Profilowanie RAM/GPU niewykonane |
| Aktualizacja delta | Gałąź zawiera kod źródłowy; nie opublikowano paczki Addressables lub APK z tymi naprawami | Niewykonane |
| Publiczna historia assetów | Jawny ZIP usunięty z HEAD, lecz nadal znajduje się w historii commita `b4628b3952cf` | Wymaga osobnej operacji wymazania historii po decyzji właściciela |

## Kontrole techniczne i kryteria odbioru

1. `python3 -m unittest discover -s unity-tools/claude -p 'test_patryk_owner_contract.py' -v`.
2. `python3 -m unittest discover -s unity-tools/claude -p 'test_estate_critical.py' -v`.
3. `python3 -m pip install -r unity-tools/validation/requirements.txt`, potem `python3 unity-tools/validation/check_csharp_scopes.py unity-city/Assets`.
4. Dopiero po przeglądzie źródeł: Unity 6.0.85f2 — **realna kompilacja skryptów oraz generowanie i zapis sceny osiedla**, bez blokujących sztucznie kontroli jakości.
5. W Play Mode i na Androidzie: spacer, bieg, podskok, wejście/wyjście z auta, sterowanie; brak T-pose i złamanych kończyn; 4 koła na pojeździe, nie różowy shader, nie wystające głowy.
6. Film i licznik błędów w `city-review/aktualizacja-osiedla.txt`, kontrola wejść, parkingów, tunelu i klubu.
7. Oddzielna decyzja właściciela co do wydania paczki delta / APK; **sama kontrola źródła nie jest publikacją aktualizacji**.

**Zasada raportowania:** Bez wpisu i nagrania z Unity nie pisać `NAPRAWIONE W GRZE` ani `TESTY TELEFONU PASS`. Otwarta pozycja pozostaje otwarta.
