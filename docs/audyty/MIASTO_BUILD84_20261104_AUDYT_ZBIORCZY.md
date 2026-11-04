# BRAXHORN CITY — AUDYT ZBIORCZY APK #84 / 4.11.2026

**Zakres:** wyłącznie miasto. Repo `wawerpatryk89-sketch/braxhorn-city`, gałąź `codex/miasto-naprawy-filmow-20261104`.
**Źródło dowodu:** [GitHub Actions #84](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41850269418), commit `713059c19b4e153a0e39bc390d9a1c755a293820`, job `android-apk` ID `126645650150`.
**Tryb pracy:** audyt + poprawki kodu i CI, bez pusha do `main`, bez zmiany wizualnej projektu i bez automatycznego buildu.

## Potwierdzone błędy

1. **P0 — kompilacja C#**: `Assets/Editor/Claude/BraxClaudeMetro.cs:1355:9` — CS0819, deklaracja `var rd = mPudlo, ciemny = mCzarny;`. Poprawa: dwie deklaracje typu `Material`. Dołożona szybka statyczna bramka w CI.
2. **P0 — kontrola zasobów pominięta w rzeczywistym jobie APK**: weryfikacje szyldów SOBCZAK/WAWER, ekspresu kawiarni, modelu tramwaju CAF, oryginalnych wiat, leków, odżywek i składu metra były w jobie `combined-unity6`, który ma `if: false` na tej gałęzi. Przeniesiono ich wykonanie także do `android-apk` po odszyfrowaniu źródeł i przed Unity.
3. **P1 — brak Library**: cache `library-android-v2-miasto-2a1cb92dc336cbc2aebbffc0-` miał MISS; fallback `unity-ci-cache-miasto-...` zakończył się `gh release view` exit 1 (brak dostępnego wydania). W nowej wersji brak starego snapshotu jest kontrolowanym MISS, a błędy autoryzacji pozostają widoczne. **Nie stworzono brakującej Library**; zgodny nowy cache może powstać dopiero po udanej kompilacji.
4. **P0 — fałszywy status odbioru**: `BraxRegion.Apply` przechwytywał błędy etapów, a na końcu zawsze ustawiał `BUILT`; `BraxMiastoOdbior` jedynie logował istotne błędy (marina, wejścia, metra, tramwaju, Tower, apteki itd.). Dodano zliczanie błędów i blokadę publikacji przy `NIEGOTOWE`.
5. **P1 — błąd wysyłki wtórny**: `BRAK APK` w publikacji wynikał z niepowstania APK po CS0819, a nie z udowodnionego błędu uploadu. Nie uruchamiać automatycznych ponowień.

## Elementy technicznie sprawdzone w runie #84

- Pobrano oryginalne paczki i materiały CC0; preflight podpisu oraz versionCode przeszedł.
- Wybrano `BRAXHORN_REJON miasto` z `unity-city/rejon-buildu.txt`. Globalne `PAMIEC_REJONOW=osiedle` w logu było mylące: konkretny krok miał `osiedle miasto`; potwierdza to log klucza rejonu.
- Unity 6000.0.58f2 uruchomił kompilator, lecz **nie wygenerował sceny ani APK**. Nie zarejestrowano testu aplikacji na telefonie.
- `#79–#83` anulowane; `#84` failure. Automatyczny `push` dla tej gałęzi wyłączono osobnym commitem. Pozostaje `workflow_dispatch`.

## Otwarte ryzyka — NIE wolno uznawać ich za naprawione bez logu Unity/Android

1. **Metro M1:** weryfikacja ciągłości wejścia, schodów, kolizji, paneli drzwi i efektu NPC; aktualny kod realizuje *inscenizację* jednego kursu, nie pełną sieć metra ani prawdziwe drzwi GLB.
2. **Tramwaj T1:** rzeczywiste niezależne skrzydła CAF, przejazd pełnej pętli, tor i bryła na łukach, przystanki, wejście gracza i NPC. Preflight wymaga nazwanego modelu, test Android nadal konieczny.
3. **Port i marina:** min. 3 jachty, 6 motorówek, 4 skutery, fotel `assise_4` w Riva, przejście po pokładzie, kamera sterowa, zejście na pomost, manewr bez kolizji.
4. **Nowe fasady premium i miasto:** wykończenie 21 segmentów, PBR bez czarnych ścian, rusztowań, niebieskich dziur, z-fightingu, działania szyldów i sklepów.
5. **NOVA, apteka, BRAXHORN NUTRITION, Tower:** fizycznie drożne drzwi, towary i etykiety, panorama i winda.
6. **Jakość mobile:** pełny start, brak crashy, FPS, RAM, rozsądna wielkość APK, przełączanie doczytywanych kwadratów 150×150 m.
7. **Aktualizacja telefonu:** stały klucz i `versionCode` umożliwiają aktualizację *właściwego packageId*, ale nie oznaczają transferu wyłącznie zmienionych plików; content-only wymaga zgodnej bazy/Addressables i osobnego testu.

## Odbiór przed publikacją

1. Statyczny preflight C# oraz zasobów (bez błędu).
2. Rzeczywista kompilacja Unity 6 Android (brak `error CS`, `NIEGOTOWE` i brakujących skryptów).
3. Raporty `BRAXHORN_MIASTO_ODBIOR`, `BRAXHORN_MARINA`, `BRAXHORN_METRO_M1`, `BRAXHORN_TRAM_T1`, `BRAXHORN_NOWA_DZIELNICA_PREMIUM`.
4. APK z prawidłowym podpisem, właściwym pakietem i wyższym `versionCode`; dopiero później instalacja i test na Androidzie.
5. Jeśli którykolwiek warunek nie przechodzi: **jedna wspólna poprawka i ponowny AUDYT**, bez automatycznego kolejnego buildu.

**Status tego audytu:** poprawki źródłowe/CI przygotowane. Wynik kolejnego rzeczywistego Unity i test na telefonie są nieznane do czasu ich przeprowadzenia. Nie ogłaszać gotowości.
