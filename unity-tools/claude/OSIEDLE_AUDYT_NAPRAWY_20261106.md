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

## Audyt uzupełniający: kolizje ruchu i bezpieczeństwo pieszych (6.11.2026)

**Zakres:** przegląd rzeczywistych skryptów generatora sceny i runtime, historii zmian, logów ostatniego APK z 5.11.2026 oraz niezależnego workflow statycznego. Nie wykonano nowego Play Mode, kompilacji Unity ani testu telefonu. Żadnego automatycznego scalania, przebudowy wizualnej lub nowego APK.

### Dowód: poprzedni APK, nie bieżący kod

[GitHub Actions 42032292474, SHA `c012f167e10f0f722cf081dd990b133981deb519`](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42032292474):
- `BRAXHORN_RUCH_TEST`: 20 aut, 600 s symulacji, **30 zakleszczeń**, 0 przecięć, 0 wyjazdów poza drogę, 30 próbek powyżej limitu skrętu (z 38 830).
- `BRAXHORN_AUTA_PARKING`: pierwszy przebieg 49 miejsc / 16 aut, następny przebieg 49 miejsc / **0 aut** (124 odrzucenia z powodu przeszkody). Nie przypisywać przyczyny drugiego przebiegu bez odtworzenia sceny.
- `BRAXHORN_KABINA`: 65 prób, **45 niedopasowanych** do dachu; źródłowy mechanizm w bieżącej gałęzi odrzuca pasażerów bez potwierdzonego prześwitu.
- `BRAXHORN_AKTUALIZACJA_SCENA FAIL`: **362 wpisy**, w tym powtarzające się błędy siatek i materiałów. To sygnały, nie liczba 362 unikalnych przyczyn. Samo `BuildPipeline.BuildPlayer: Succeeded` nie świadczy o poprawności sceny.
- Starszy APK z 4.11 zgłaszał 69 zakleszczeń, ale porównanie z nim nie dowodzi rozwiązania obecnego problemu.

### Poprawki źródłowe wpisane na bieżącą gałąź

- `BraxRuchAutSym.UstawStart`: kontrola przeszkód statycznych, zaparkowanych samochodów, busów i auta gracza **przed** akceptacją startu pojazdu AI.
- `BraxRuchAut.Start/Update`: callback fizyki sceny i obrysy zewnętrznych pojazdów rejestrowane przed rozstawieniem AI; `Physics.SyncTransforms` weryfikuje pozycje koliderów. Wymagana ocena kosztu na Androidzie.
- `BraxPieszy.Przeszkoda/KrokUcieka`: kontrola wielu trafień `SphereCastNonAlloc` i końcowej kapsuły celu, żeby nie wykonywać ruchu ucieczki przez ścianę lub inne postaci.
- `BraxPieszy.AutoBlisko` + `BraxAuto.PredkoscAktualna`: nie ignorować samochodu jadącego tuż przed przejściem (wcześniej warunek `m < 3.5f` ignorował również poruszające się pojazdy).
- `BraxRuchAutSym.Ruch`: limit prędkości przed ciasnym zakrętem i w nim także między tyknięciami decyzji AI. Ten warunek nie dowodzi braku zatorów.

### Kontrola źródeł po poprawkach

[GitHub Actions 42212717956](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42212717956), commit `95d0e7528cbb286a5de29c39e77085b92539bce3`:
- **36/36 testów statycznych PASS**: 6 kontraktu Patryka, 5 dostawy oryginalnych klipów, 25 regresji osiedla.
- **116 plików C#** przeszło kontrolę strukturalną: 106 plików modułu Editor/Claude oraz 10 plików dodatkowych, 0 wykrytych błędów.
- Żaden z tych testów nie kompiluje IL2CPP ani nie uruchamia sceny Unity.

### Otwarty odbiór funkcjonalny — wymagane po bieżących zmianach

1. **Ruch samochodowy:** nowa symulacja sceny 600 s; zapisać `zakleszczenia`, `nachodzenia`, `poza_droga`, `za_szybkoWZakrecie`, minimalne odstępy, korki przy rondzie i skrzyżowaniach, pojazdy obce/parkowane i autobusy. Cel: zero kolizji, zero wyjazdów z dróg i zero zakleszczeń. Stare 30 pozostaje **niezweryfikowane po poprawkach**.
2. **Parkingi:** wyjaśnić 16 aut przy pierwszym przebiegu i 0 przy następnym, zweryfikować ponowne generowanie, docelowe zajęcie, pełny prześwit i nieblokowane drzwi.
3. **Pasażerowie:** na rzeczywistym szkielecie i klipach sprawdzić każdy model kabiny i brak głów ponad dachem, w tym samochód Patryka.
4. **Postać Patryk:** 22 oryginalne FBX — Idle/Walk/Run/CarIdle, kolana, stopy, barki, animacje wsiadania i wysiadania; bez retargetu cudzych animacji.
5. **Piesi:** wszystkie zebry oraz 10 minut ruchu NPC — brak przenikania w auta, ściany i ludzi; brak pustych/zakleszczonych odcinków.
6. **Scena:** usunąć rzeczywiste przyczyny błędów siatek/materiałów z 362 wpisów, przetestować wejścia i schody, wnętrza, boiska, dyskotekę, drzwi i oznaczenia; rozróżnić duplikaty diagnostyczne.
7. **Android:** nowa globalnie podpisana baza APK jest niezbędna dla zmian C#/IL2CPP; zwykła paczka content delta nie zaktualizuje logiki samochodów. Bez testu na telefonie nie zatwierdzać wydania.

**Wniosek audytu:** poprawki w kodzie i statyczne testy przeszły; ruch, kolizje, animacje i jakość sceny pozostają do odbioru na nowej wersji Unity 6000.0.85f2. Nie przedstawiać starego APK jako potwierdzenia nowego kodu.
