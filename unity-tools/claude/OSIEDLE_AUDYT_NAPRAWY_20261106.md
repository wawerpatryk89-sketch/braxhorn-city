# BRAXHORN CITY — osiedle Unity 6: audyt i naprawy (6.11.2026)

**Gałąź:** `codex/osiedle-audyt-naprawa-20261106`

**Ograniczenie:** WYŁĄCZNIE osiedle, Unity 6 i oryginalny model Patryka. Nie modyfikować oryginalnych FBX, geometrii, skali kości ani wyglądu postaci. Bez cudzych animacji.

## Potwierdzone w GitHub Actions

- [Bezpieczny import Patryka 22/22](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42149847404) — oryginalne SHA-256, kompletny zaszyfrowany transfer `unity-city/Prywatne/import-patryk-animations-20261106`; brak jawnego ZIP w bieżącym drzewie (UWAGA: historia Gita zawiera wcześniejszy jawny commit).
- [Najnowsze szybkie testy osiedla](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42177190005) — 32/32 testy statyczne Python i skaner 112 plików C# bez błędów **składniowych**. To NIE jest kompilacja Unity ani akceptacja grafiki na Androidzie.
- [Audyt oryginalnych aut GLB](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42173173037) — 10 modeli, 2 z nazwanymi oddzielnymi drzwiami, 2 bez jawnych nazw koła (nie mylić z fizycznym brakiem geometrii).

## Zapisane poprawki źródłowe

1. **Patryk** — tylko jeden oryginalny model gracza, kontrola sum FBX, 4K BaseColor/Normal i 2K ORM, brak sztucznego chodu, 22 własne ruchy; kontrola biomechaniki i dubli.
2. **Sport** — przyciski CIOS/KOP działają tylko wewnątrz aktywnych boisk, bez normalnych ataków w eksploracji. Do zdarzeń piłki używane są wyłącznie oryginalne `GoalkeeperCatch`, `GoalkeeperPlacingBall` oraz `GoalkeeperDropKick`; procedurę obracania kończyn Braxa wyłączono dla Patryka. Na korcie rakieta jest rekwizytem podążającym za oryginalną dłonią. Wszystkie 4 rodzaje boisk walidowane.
3. **Salon** — osobne stanowisko sprzedawcy „Patryk” pozostaje zgodnie z wcześniejszym zleceniem właściciela, ale nie jest nieruchomą pozą Idle: używa oryginalnego `Typing`. Pozostałe skopiowane postaci Patryka są wykrywane.
4. **Auta** — prawdziwe siatki kół obowiązkowe, przekształcenie importowanego skinned nadwozia przez BakeMesh, mobilne materiały zgodne z Built-In; model bez kół odrzucany. Wejście/wyjście gracza ignoruje wyłącznie kolizję jego własnego auta.
5. **Pasażerowie** — odrzucone kabiny z brakującą skórą głowy, brakiem klatek siadania lub niefinitymi wartościami dachu; bez głów wystających nad dach.
6. **Ruch i parking** — zerowanie wyniku testu z wcześniejszej generacji, korekta skali awaryjnych zatok, co druga zatoka wolna, ograniczone trąbienie.
7. **Drzwi i wejścia** — kontrola fizycznej drożności, bez fałszywego ignorowania ścian; zielone znaki wyjścia dopiero po pozytywnym teście łączności; błędy zgłaszane do raportu.
8. **Autobusy** — zerowanie starych parametrów, kontrola braku przystanków i zejścia z drogi, kompatybilny shader przy importowanych autobusach.
9. **Centralny raport** — jawne `DO POPRAWKI` z generowania aut, salonu, sportu, boksu, dyskoteki, obsady NPC, wejść, pasażerów i autobusów trafia do `city-review/aktualizacja-osiedla.txt`.
10. **Kontroler gry** — osiedle (explorationOnly) przyjmuje CIOS/KOP wyłącznie w granicach boisk, używa wyłącznie `GoalkeeperCatch` / `GoalkeeperPlacingBall` / `GoalkeeperDropKick` Patryka, uruchamia zdarzenia piłki; fora Braxa i jego IK nie obracają już kończyn Patryka. Interfejs Android ma rzeczywiste przyciski.
11. **Przesyłanie małych aktualizacji** — `.github/workflows/zone-content-update.yml` wybiera projektowe Unity `6000.0.85f2` tylko dla osiedla; profil innych stref pozostaje bez zmian. Nadal nie zastępuje nowego podpisanego APK dla zmian C#.
12. **Personel salonu** — właścicielskie stanowisko „Patryk (sprzedawca)” ma klip `Typing` z ostatniej paczki i nie tworzy już nieruchomej figury Idle. Osobne, niezlecone kopie nadal trafiają do raportu.

## Wdrożenie na telefon — warunek podpisu i nowej bazy

Dokument `unity-tools/updates/CONTENT-UPDATES.md` jednoznacznie mówi, że `[content-update]` przesyła zapisane sceny, prefaby i materiały, ale **nie zmienia skompilowanego kodu IL2CPP**. Naprawy `Assets/BraxGymPlayer.cs`, `Assets/Claude/Runtime/BraxAutoGracza.cs`, `Assets/Claude/Runtime/BraxRuchySport.cs` i pozostałe zmiany w kodzie wymagają jednego nowego, globalnie podpisanego bazowego APK. Nie oszukiwać właściciela obietnicą, że wystarczy sama paczka `.braxupdate.zip`. Pełna kompilacja Android NIE była jeszcze uruchomiona, a urządzenie NIE zostało przetestowane.

## Otwarte, wymagające Unity Play Mode / Android

- Odtwarzanie 22 FBX na realnym szkielecie Patryka: stopy, barki, ręce, kontakt, brak wykręconych kości; Idle i CarIdle.
- Odtwarzanie ruchów sportowych z piłką: nie ma dostarczonego osobnego tenisowego forehand/backhand, więc NIE twierdzić, że uderzenie rakietą jest anatomicznie zatwierdzone. Rozgrywka opiera się na oryginalnym FBX i zdarzeniu kontaktu.
- Kontrola kabiny sportowego auta i animacji drzwi (faktyczne wyposażenie modelu, miejsce pod areną), sprawdzenie na ekranie Android.
- Rzeczywista fizyczna dostępność każdego wejścia, pięter, schodów, sklepów i lóż.
- 10-minutowy test ruchu sceny, przejazd pełnowymiarowego autobusu 12 m × K i brak zatorów, weryfikacja wyglądu kół / materiałów.
- Działanie małych aktualizacji bez pełnego APK na telefonie (mechanizm osobny; nie oznaczać jako potwierdzony).

**Decyzja:** NIE ogłaszać „wszystkie błędy naprawione”. Stan: źródła poprawione, szybkie testy PASS, urządzenie i kompilacja Unity bez testu. Nie wysyłać pełnego buildu na podstawie samych kontroli składniowych. 
