# R1 — cała wyspa: audyt ciągów pieszych (06.11.2026)

**Rejon:** Miasto R1, wyspa premium / BRAXHORN ARENA, Unity 6.  
**Stan:** poprawki kodu zapisane; brak potwierdzonego przejścia w Unity Play Mode i brak nowego APK z tymi zmianami.  
**Odbiór P0 pozostaje otwarty.** Statyczny audyt nie jest dowodem drożności w grze.

## Fakty → diagnoza → poprawki

| Obszar | Wcześniejszy błąd w generatorze | Zmiana |
|---|---|---|
| Chodniki A1/A2/A3 i dojścia do domków | Na skrzyżowaniu znikała cała płyta długości 1 m na podstawie jednej próbki środka. | Cięcie odcinków do dokładnej granicy przecięcia, bez znikających całych metrów. |
| Fizyka chodnika | Collider do 4 m mógł obejmować również miejsca, w których nie było widocznej płyty. | Kolizja tylko pod rzeczywiście utworzonym fragmentem, zgodnie z widocznym nachyleniem. |
| Wysokości | Każda aleja liczyła profil w swojej osi, przez co na przecięciach możliwe były różne poziomy. | Wspólny pomiar terenu w osiach świata i wyrównanie spadku do 0,12 m na metr. |
| Kładki do ratowników | Deski miały wygląd, ale nie miały odpowiadającej im kolizji. | Segmentowe collidery pod każdym odcinkiem desek; kontrola styku z promenadą. |
| Ławki, kosze, latarnie | Nie wszystkie elementy sprawdzały planowaną trasę przed ustawieniem. | Ochrona korytarzy kładek i skrzyżowań, sprawdzenie zajętości przed postawieniem wyposażenia. |
| A2 → plaża | Promenada plażowa i aleje domków nie miały jawnego łącznika. | Wyznaczanie wolnego połączenia z zachowaną promenadą; gdy brak bezkolizyjnej trasy — raport ostrzegawczy. |
| Pomost → deptak | Pierwszy fragment deptaka mógł zaczynać się 15 cm pod pokładem. | Wyrównanie punktu startu względem pokładu i ograniczenie skoków wysokości. |
| Domki | Bryła mogła powstać zanim postawiono zaplanowany chodnik. | Kontrola kolizji planu alejek przed postawieniem domku. |
| Rośliny areny | Brak roślin ozdobnych potrafił przerwać generowanie sceny. | Ostrzeżenie jakościowe zamiast zatrzymania budowania; bez sztucznych zamienników. |

## Zasięg raportu fizyki

Po wygenerowaniu nawierzchni, budynków, wyposażenia, palm i drzew wykonywany jest audyt metodą AudytCalejWyspy. Używa RaycastAll i OverlapCapsule w Unity. Loguje niedrożności, brak chodnika pod stopami i różnice wysokości na trasach:
- główne aleje A1, A2, A3 i łącznik z placu areny;
- dojścia do recepcji, sklepu oraz każdego utworzonego domku;
- warunkowy łącznik plaża–A2;
- pomost i deptak od przystani do areny;
- promenada plażowa oraz wszystkie kładki do ratowników.

Identyfikator logu: WYSPA_CALA_TRASA_AUDYT; uzupełniające: WYSPA_CHODNIK_SPOJNOSC, WYSPA_KLADKI_SPOJNOSC, WYSPA_POMOST_STYK, WYSPA_POLACZENIE_PLAZA_A2. Kontrole jakości są ostrzeżeniami i nie blokują APK.

## Krótkie testy regresji

Plik: unity-tools/wyspa/tests/test_walkways_static.py. Zawiera osiem szybkich testów statycznych, bez Unity, bez pobierania prywatnych paczek i bez budowania APK. W bieżącym przeglądzie odczytanych źródeł zweryfikowano 13/13 warunków dotyczących istnienia ścieżek kodu, kolejności audytu i ochrony colliderów. **Nie wykonano kompilacji C# ani fizycznego testu w Unity.**

## Usterki i ograniczenia — nie oznaczać jako zakończone

1. Część domków i budek ma dekoracyjne skrzydła drzwi i collider obejmujący całą bryłę. Nie ma potwierdzenia możliwości wejścia do ich wnętrz; nie wolno deklarować pełnej funkcjonalności lokali na wyspie.
2. Pitbull/RAKK i Doberman/ZYRO: manifest używa klipów przenoszonych w większości od innych postaci, a kontrola skinningu i runtime jest niezakończona. Właściciel zgłosił deformacje kończyn. Podłączenie 10 animacji nie jest dowodem naprawy. Dedykowana paczka specjalnych ruchów Pitbulla nie została potwierdzona jako zaimportowana.
3. Dla warunkowego połączenia plaża–A2 może nie istnieć bezkolizyjny korytarz. Wtedy generator raportuje brak, nie stawia chodnika przez przeszkody.
4. Nadal wymagany test wizualny i praktyczny: pełny spacer Braxem od przystani, przez arenę i plażę do osiedla; wejścia, przejścia, wysokości, kolizje, FPS i Android. Nie uruchomiono Unity po zmianach.

## Reguła dalszego wykonywania

Prace wyłącznie w gałęzi codex/miasto-metro-apk-kontrola-20261105. Bez modyfikowania main i bez samodzielnego uruchomienia kosztownego buildu. Zwykłe usterki geometrii i dekoracji raportować jako ostrzeżenia bez blokowania APK; błędy techniczne uniemożliwiające kompilację pozostają fatalne.

Komity potwierdzające zapis napraw: 56324b4, bbc0429, 05923c7, f0a5e98, 962686d, 2b9ac81, 79d48ed, 63f84a5, 5fb6d03.
