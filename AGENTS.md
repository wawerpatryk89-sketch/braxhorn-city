# OBOWIĄZKOWE PIERWSZE SŁOWA ODPOWIEDZI — IDENTYFIKACJA OBSZARU

Globalne polecenie właściciela z 6.11.2026: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13

**Na tej gałęzi pierwsze słowa KAŻDEJ odpowiedzi wykonawcy (w tym nowy czat, raport, status, zlecenie, komentarz) muszą brzmieć: `MIASTO MISTRZÓW —`.** Dopiero po nich informacja o wykonywanych pracach, gałęzi, stanie, testach, błędach, planie i dowodach. Nie zaczynać od „Witam”, „Sprawdzam”, „Raport”, „OBSZAR PRAC:” ani od jakiegokolwiek innego nagłówka.

**Globalne cztery prawidłowe początki — wybierz tylko rzeczywisty obszar własnego zadania:**
- `OBÓZ — ...`
- `MIASTO MISTRZÓW — ...`
- `MIASTO R1 — ...`
- `OSIEDLE — ...`

Gdy zadania nie przypisano jednoznacznie, pierwszy wyraz `NIEUSTALONY — ...`; nie udawać przypisania. Nie mylić miasta R1 z Miastem Mistrzów i nie podawać cudzych prac jako swoich. Nagłówek ma być widoczny w pierwszym wierszu każdej odpowiedzi, a nie tylko wewnątrz pliku, zadania lub tytułu pull requesta.

Zasada identyfikacji nie zmienia scen, kodu Unity, materiałów ani workflow. Nie uruchamia buildów i nie upoważnia do pracy nad inną gałęzią.

---
# PILNE P0 — 6.11.2026: TESTOWY APK BEZ BLOKAD PRZEZ KONTROLE KOLIZJI / JAKOŚCI

**Globalna decyzja właściciela dla Claude, Codex, GPT, Work i pozostałych wykonawców, wszystkie gry/rejony BRAXHORN**. Pełny zapis: [Issue #13 — aktualizacja 6.11.2026](https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13).

- Przy najbliższym **osobno autoryzowanym** buildzie APK: testy jakości/kolizji/przejść, schodów, drzwi, położenia mebli, modeli, materiałów, animacji i wyglądu = **OSTRZEŻENIE, nie przerwanie procesu**. Zachować raport z lokalizacją błędu; NIE raportować fałszywego „QA PASS”. Niezgodna ławka czy jedno przejście nie mogą skasować 5 godzin kompilacji.
- **Nie usuwać faktycznych colliderów, nie wyłączać fizyki w aplikacji**. Wyłączyć wyłącznie blokujące bramki *oceny jakości* (`throw`, niepotrzebne `exit 1`, strict QA, `quality-strict.txt`, quality-only workflow). Tam, gdzie istnieje `BraxBuildQuality`, zastosować tryb `Advisory`; raport `build/QualityReview/`.
- Błędy krytyczne technicznie (C#, generowanie/zapis wymaganych scen i assetów, build Android, podpis globalny, identyfikator/versionCode, rzeczywisty plik APK) **nadal muszą blokować** i być raportowane zgodnie z prawdą.
- **APK FIRST**: jeśli powstało poprawne podpisane APK, zachowaj i udostępnij plik przed cache/backupem/raportem. Późniejszy błąd pomocniczy nie unieważnia istniejącego instalatora.
- Bez aktualnej zgody właściciela **nie uruchamiać buildów ani automatycznych restartów**, nie scalać rejonów, nie modyfikować zatwierdzonej grafiki. Zasada dotyczy wszystkich gałęzi roboczych, także tych, na których nie ma jeszcze niniejszej notatki; wykonawcy muszą ją przejąć z centralnego Issue #13.

# Standard tekstur TYLKO głównych postaci grywalnych (5.11.2026)

Decyzja właściciela: **wyłącznie postacie, którymi użytkownik steruje osobiście** (np. Brax, Nyxa, Patryk, Monika w roli grywalnej). Głowa/twarz do 4K, ciało/ubranie do 2K, buty/akcesoria do 1K; **tylko gdy postać faktycznie ma oddzielne materiały i UV**. Zwykli NPC, przeciwnicy i przechodnie są wyłączeni z tej reguły. Nie zmieniać wyglądu, obecnych tekstur, podziału UV ani materiałów bez przeglądu modelu i osobnej decyzji właściciela. Dotychczasowe postacie nie są przez tę notatkę przerobione. Szczegóły: [Tekstury postaci grywalnych](docs/zasady/TEKSTURY_POSTACI_GRYWALNYCH_20261105.md). Zmiana dokumentacji nie uruchamia buildu.

# Aktualna decyzja właściciela — testowy APK bez blokad jakości

Stosuj [APK do ręcznej oceny](docs/zasady/APK_RECZNA_OCENA_20261105.md).
Kontrole jakości postaci, kolizji i przejść mają ostrzegać, nie blokować APK.
Fizyczne kolizje pozostają. Kompilacja, generowanie, podpis i plik APK nadal muszą się udać.
Ta nowsza decyzja zastępuje sprzeczne starsze bramki jakości. Bez uruchamiania buildów.

## Unity Essentials — obowiązek wszystkich wykonawców (5 listopada 2026, Europe/Zurich)

**Decyzja właściciela: od teraz korzystamy z Unity Essentials przy każdej pracy nad grami Unity, w której jego procedury mają zastosowanie.** Dotyczy Codex, GPT, Work, Claude i innych wykonawców; BRAXHORN CITY, Miasta Mistrzów, osiedla, obozu oraz prac nad BRAXHORN ARENA. Każdy wykonawca ma zapoznać się z tą zasadą przed rozpoczęciem pracy. Zasadę należy przenosić do instrukcji kolejnych gałęzi i projektów w ramach zleconej pracy, bez nadpisywania cudzych zmian i bez samodzielnego zapisu na main.

- Korzystaj z właściwych procedur wtyczki: rozpoznanie projektu, audyt techniczny, diagnoza błędów, implementacja funkcji, kontrola gotowości kompilacji oraz sprawdzanie połączenia Unity MCP. W tej instalacji Unity Essentials ma identyfikator pakietu `unity-workbench`; to ten sam produkt, nie drugie narzędzie.
- Stosuj je do kodu C#, scen i prefabów, modeli i tekstur, materiałów i shaderów, animacji i retargetingu, fizyki i sterowania, zależności, wydajności oraz konfiguracji Androida, podpisu i dostawy APK — w zakresie faktycznie dostępnych procedur i narzędzi.
- Przed zmianą zbierz dowody, odczytaj aktualne pliki i SHA, ustal przyczynę, przygotuj najmniejszą uzasadnioną poprawkę i sprawdź regresje. Zachowaj zatwierdzony wygląd, układ, skalę i oryginalne zasoby. Nie wyłączaj kontroli, żeby ukryć błąd.
- Unity Essentials jest wtyczką procedur pracy dla asystenta. Samo jej użycie nie oznacza dostępu do edytora. Osobno sprawdź dostępność Unity, mostka MCP i aktywnego połączenia. Jeżeli dany wykonawca nie ma wtyczki lub połączenia, ma to uczciwie zgłosić, zastosować zapisane tu zasady i kontynuować możliwą pracę; nie deklarować fikcyjnego użycia wtyczki ani testu.
- Raportuj oddzielnie: analizę statyczną, testy lokalne, kompilację skryptów w Unity, generowanie i ponowne otwarcie scen, kompilację Androida, weryfikację podpisu, rzeczywiście dostarczony APK oraz test na telefonie. Podaj dowody i ograniczenia.
- Nie instaluj samodzielnie pakietów MCP ani nie zmieniaj wersji silnika tylko dlatego, że używasz Unity Essentials. Obowiązują wcześniejsze zasady zgody na zmiany zależności, ochrony projektu i ręcznych buildów. Polecenie korzystania z wtyczki **nie jest zgodą na uruchomienie buildu**.

# PILNE — APK FIRST (4.11.2026)

**Polecenie właściciela obowiązuje wszystkich, w tym Miasto Mistrzów:** gotowy APK zabezpiecz i udostępnij PRZED cache, snapshotem i raportami. Późniejsza awaria nie może odebrać pliku. Miasto Mistrzów ma osobny ręczny workflow APK; stan kompilacji i dostawy sprawdzać w aktualnym przekazaniu poniżej. Zobacz [pełną zasadę](docs/zasady/APK_FIRST_DOSTAWA_20261104.md) i [Issue #13](https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13). Nowe buildy tylko po osobnej zgodzie właściciela.

---

# BRAXHORN CITY — OBOWIĄZKOWA ZASADA DLA WSZYSTKICH WYKONAWCÓW (4.11.2026)

**Decyzja właściciela: żadnych automatycznych buildów.** Dotyczy Claude, Codex, GPT, Work oraz wszystkich innych wykonawców; miasta, osiedla i obozu.

1. **Commit i push służą do zapisu kodu, nie są zgodą na build.** Zmiana pliku `build-request.json`, `content-update-request.json`, komunikat `CITY BUILD:`, PR, merge, tag, zakończenie innego workflow ani niepowodzenie poprzedniego nie mogą automatycznie uruchamiać kompilacji.
2. **Unity 6 Android APK/AAB, WebGL, Addressables oraz kosztowne testy Unity uruchamia się WYŁĄCZNIE ręcznie** przez `workflow_dispatch`, po osobnym, jednoznacznym poleceniu właściciela dla konkretnej gałęzi, commita, rejonu i wariantu. Nie używać `push`, `pull_request`, `workflow_run`, `schedule` ani skryptowego dispatch do samoczynnego uruchamiania.
3. Wykonawcy mogą poprawiać kod, audytować, testować statycznie i zapisywać zmiany w swoich gałęziach bez startowania kompilacji Unity. Nie uruchamiać serii buildów po drobnych zmianach. Przed ręcznym buildem przekazać listę poprawek, ryzyka oraz odczytane statusy.
4. **Nie uruchamiać samodzielnie ponownie** błędnego, anulowanego ani ukończonego buildu, nie anulować cudzego bieżącego buildu i nie włączać automatycznych wyzwalaczy. Jeżeli ręczne uruchomienie na GitHub nie jest możliwe, zgłosić problem i nie obchodzić zakazu.
5. Rejony osobno: **miasto / osiedle / obóz**. Bez zgody nie scalać rejonów i nie wysyłać połączonego APK. Nie zmieniać wyglądu, jakości ani zasobów innych wykonawców bez polecenia. Nigdy nie pushować na `main` bez osobnej zgody.
6. Dla zatwierdzonego APK zachować stały certyfikat, poprawne `applicationId` i `versionCode`, szyfrowanie prywatnych zasobów oraz zgodną pamięć `Library` i checkpointów. Przywrócenie cache **nie jest** dowodem małej aktualizacji na telefonie.
7. Raportować osobno: kod, preflight, kompilację, opublikowany APK oraz test na Androidzie. Dowód: link runu, SHA i logi. Bez testu nie deklarować, że działa.

**Nadrzędna zasada:** „My decydujemy, kiedy build uruchamiamy i kiedy go naprawiamy”. Bez aktualnej zgody — **ZERO buildów i automatycznych ponowień**.

## Miasto Mistrzów — obowiązkowy punkt startowy

Wymaganie właściciela z 4.11.2026: przed oceną i dalszym wykonaniem przeczytaj [sześć zdjęć odniesienia i wymagania jakości Miasta Mistrzów](docs/zlecenia/mistrzowie-referencje-jakosci/README.md). Jakość ma przewyższać pierwsze miasto właściciela: detal, tekstury, cienie i oświetlenie; luksusowy program obiektów zachować zgodnie z istniejącym projektem. Odbioru wizualnego nie zastępuje udana kompilacja.

Przed pracą w tym regionie przeczytaj [mapę i przekazanie](docs/zlecenia/MIASTO-MISTRZOW-START.md). Tam są pełny plan PDF, dwa podglądy map, aktualny rzut generatora, źródła współrzędnych, standard jakości i status pierwszego buildu. Zachowaj układ, oryginalne zasoby i skalę; dokumentacja odróżnia plan, istniejący kod i wynik sprawdzony w Unity. Powyższe zasady ręcznego uruchamiania buildów pozostają w mocy.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
