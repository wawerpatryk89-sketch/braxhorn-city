# BRAXHORN CITY — Osiedle R2, audyt kodu i odbioru
Data: 2026-11-06. Silnik: Unity 6, projekt osiedla 6000.0.85f2 (pozostałe strefy mają odrębny profil).
Zakres: wyłącznie osiedle. Bez przebudowy grafiki, geometrii, szkieletu Patryka i bez scalenia do main.

## Ocena
**Kod częściowo naprawiony. Odbiór gry: NIEZATWIERDZONY.**
Zielone testy Pythona i parsera C# nie potwierdzają kompilacji Unity, fizyki w Play Mode, obrazu ani wydajności na Androidzie. Nie twierdzić, że wszystkie błędy zniknęły.

## Źródła i porównanie

- Gałąź audytowana: `codex/osiedle-audyt-naprawa-20261106`, stan bazowy na początku przeglądu `d762cb0bcf15105c6fe6899971dc06b2bb326f93`.
- Poprzedni APK: run https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42032292474, SHA `c012f167e10f0f722cf081dd990b133981deb519`, wynik workflow **success**, artifact `android-apk-osiedle-c012f167e10f0f722cf081dd990b133981deb519-1`. JEST TO STARSZY KOD niż obecna gałąź. Sukces kompilacji nie oznacza sukcesu testów sceny.
- Ostatni potwierdzony szybki test po korektach ruchu: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/42212717956 (**success**, źródła z `95d0e752`). Jest to **NO Unity / NO APK**.
- Starszy pełny log Unity z przebiegu `42032292474` zgłosił `BRAXHORN_AKTUALIZACJA_SCENA FAIL — 362 błędów`. Część to powtarzające się braki shaderów/siatek pojedynczych modeli, a część realne problemy ruchu i lokali; nie liczyć wszystkich jako osobnych defektów.

## Wyniki starej symulacji ruchu — WYMAGAJĄ PONOWIENIA
Stary build: 20 aut, 600 s, krok 0.1 s. `zakleszczenia=30`; `nachodzenia=0`; `poza_droga=0`; `przejazdyWezlow=786`; średnia prędkość 15 km/h; stanie na auto 186 s; `zaSzybkoWZakrecie=30/38830`. Powtarzający się punkt blokady: rondo/wyspa przy (0,42); w logach występują wzajemnie czekające na siebie samochody. Trasy w starej symulacji nie obejmowały fizycznych przeszkód sceny (ruch w teście generatora używał `BraxRuchAutSym` bez delegata `przeszkodaSceny`). Zero `nachodzenia` nie potwierdza więc pełnego testu kolizji z pieszymi, słupkami i zaparkowanymi autami.

## Co jest faktycznie zapisane w źródłach 6.11

1. **AI aut — start, otoczenie.** `BraxRuchAutSym.UstawStart()` pomija pozycje zajęte przez otoczenie i pojazdy zewnętrzne; `BraxRuchAut.Start()` rejestruje przeszkody przed rozstawieniem, a `Update()` synchronizuje transformacje fizyki przed zapytaniami do koliderów.
2. **AI aut — zakręty.** `BraxRuchAutSym.Ruch()` ogranicza prędkość według aktualnej i następnej krzywizny łuku. Nie ma jeszcze nowego testu 600 s potwierdzającego, czy starsze 30 przekroczeń spadło do 0.
3. **Piesi.** `BraxPieszy` używa wielu trafień `SphereCastNonAlloc` zamiast polegać tylko na pierwszym wyniku, a ruch ucieczki jest odrzucany, gdy kolejny krok prowadzi przez przeszkodę lub zajęty punkt. Dodatkowo zbliżające się auta przy zebrze nie są ignorowane tylko dlatego, że są bliżej niż 3.5 m; uwzględniana jest ich aktualna prędkość `BraxAuto.PredkoscAktualna`.
4. **Patryk.** Import 22 oryginalnych FBX z weryfikacją SHA; wymagane Idle/Walk/Run/EnteringCar oraz ruchy sportowe. Bez obcych procedur deformujących kończyny. Stanowisko „Patryk (sprzedawca)” ma własny klip Typing. To walidacja kodu i archiwów, nie dowód wizualny prawidłowych stóp, ramion i pracy szkieletu.
5. **Kabiny i wystające postaci.** `BraxClaudeKabiny` odrzuca nieweryfikowalne pomiary i postaci z głową ponad dachem zamiast zmniejszać skale modeli. `KierowcyAut()` / `PodrozniParkingow()` usuwają odrzucone instancje. Stary pomiar: 45 nieudanych dopasowań z 65 zbadanych miejsc; nie ma nowego pomiaru po zmianach.
6. **Auto gracza.** `BraxAutoGracza` ma rzeczywiste sterowanie, Rigidbody/Collider, kontrole przeszkód, EnteringCar/SitToStand, drzwi; kolizje własnego auta nie blokują wejścia/wyjścia. **Brak potwierdzonego modelu sportowego** mieszczącego Patryka 2.33 m w starym logu. Najlepszy wymieniony kandydat `car_rev` w pomiarze starego buildu miał zapas -0.357 m. Nie wolno sztucznie zmniejszać Patryka ani ogłaszać, że auto jest gotowe.
7. **Parking.** Odstęp między autami: co druga zatoka ma pozostać wolna; pomiary kolizji i szerokości są zapisane w generatorze. Stary log: 49 miejsc, najpierw 16 aut, w późniejszym przebiegu 0; to wskazówka do sprawdzenia, czy sceny/kolizje są czyszczone między generacjami, ale bez nowego testu nie jest to udowodniona przyczyna.
8. **Autobusy.** Audyt braku przystanków i zejścia z trasy wpisywany do centralnego raportu; materiały GLB konwertowane do mobilnego shadera. Stary log: 5 próbek poza jezdnią linii A, jeden kierunek B bez przystanków. Nie ma dowodu fizycznej korekty geometrii drogi i pełnej dostępności przystanków.
9. **Scena — kontrola wejść, modeli i materiałów.** Raport `SprawdzAktualizacje` zbiera błędy od generatorów, analizuje podwójnego Patryka, siedzenia w salonie, rzeczywiste koła aut, brak modeli, obsadę i drożność przejść. Błędy jakości celowo nie blokują testowego APK; należy odczytać raport, a nie pomylić wygenerowanego artefaktu z odbiorem.
10. **Aktualizacje.** Obecny kod C# nie jest możliwy do dostarczenia zwykłą paczką zasobów `.braxupdate.zip`. Wymaga nowej, globalnie podpisanej bazy APK/IL2CPP; dopiero potem zasoby i sceny można aktualizować małą paczką przy zgodnym `base_id`.

## Wskazane commity napraw / ślad kontroli
- Spawn AI i przeszkody: `e0d0efbd` oraz `070bbc21`.
- Przenikanie pieszych w trakcie ucieczki: `24279efd`.
- Testy ruchu i rozszerzenie CI: `9de8c364`, `0b17fea5`.
- Prędkość w zakrętach: `49547de3`.
- Synchronizacja PhysX: `0ab4e07c`.
- Próby statyczne: `bf20bb9d`.
- Piesi i pojazdy na zebrach: `365018ac`, `f3828376`, `21afc396`, `95d0e752`.
- Ręczne dopuszczenie bieżącej gałęzi do testowego podpisanego APK, **bez automatycznego uruchomienia**: `19a0fe9a` w `.github/workflows/claude-region-build.yml`; `workflow_dispatch`, wybrać gałąź audytu i `rejon=osiedle`.

## Zidentyfikowane ograniczenia niepozwalające na odbiór

- Brak nowej kompilacji Unity 6 i artefaktu APK z aktualnego SHA; brak 10-minutowego testu aut po naprawach (także z prawdziwymi przeszkodami sceny).
- Brak weryfikacji zatorów ronda (0,42), postoju na jezdni, busa 12 m × K, ciągłości przejść, braku zacinania i braku utraty FPS na Androidzie.
- Brak akceptacji na telefonie wyglądu Patryka, stóp, rąk, wejścia i wyjścia z samochodu; materiałów i geometrii wszystkich aut.
- Brak zweryfikowanego sportowego auta z wystarczającą kabiną dla Patryka, zgodnego z oryginalnymi wymiarami i wyglądem.
- Stare logi wskazywały 18 nakładających się par w arenie osiedla, sportowe boiska współdzielące teren, błędy obsady oraz wadliwe shadery/siatki pojazdów. Bez aktualnego raportu sceny nie oznaczać jako naprawione.
- Otwarte PR dotyczące osiedla (np. #5, #6, #8, #9, #11) są gałęziami alternatywnymi i nie stanowią automatycznie zatwierdzonej integracji. Nie scalać równolegle bez porównania zmian.

## Odbiór przed wypuszczeniem

A. Przygotować jeden test Unity 6 na **dokładnym** HEAD osiedla, najpierw kompilacja i zapis logu `BRAXHORN_RUCH_TEST`, `BRAXHORN_AKTUALIZACJA_SCENA`, `BRAXHORN_KABINA`, `BRAXHORN_AUTO_PATRYKA`, `BRAXHORN_AUTOBUS`; bez zatrzymywania produkcji przez same raporty.
B. Porównać z bazą: 30 zakleszczeń, 30 próbek zbyt szybkich, 5 punktów busa poza jezdnią, 45 nieprawidłowych miejsc kabin, 18 nakładek areny, 362 raporty sceny. Wymagać rozliczenia każdego rodzaju błędu.
C. Na Androidzie przejechać komplet tras i rondo przez 10 minut, sprawdzić ruch pieszych, przejścia, parkingi, sklepy, schody, salon i klub; zweryfikować bieganie Patryka, animacje, wsiadanie i wysiadanie, wygląd kierowców.
D. Po udanym APK oznaczyć konkretny podpis, SHA i bazę zawartości. Nie nazywać pliku aktualizacją delta, jeśli zawiera nowy skompilowany kod.
E. Bez zgody właściciela nie uruchamiać kosztownego pełnego buildu, nie scalać, nie publikować i nie zmieniać grafiki.

## Werdykt
**Stan źródeł: poprawki i szybkie testy potwierdzone.**
**Stan wykonania całego zlecenia w Unity na telefonie: NIEPOTWIERDZONY.**
**Gotowość do wydania: NIE — do czasu dynamicznego testu, ponownego raportu jakości i odbioru Android.**
