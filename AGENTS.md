# Standard tekstur TYLKO głównych postaci grywalnych (5.11.2026)

Decyzja właściciela: **wyłącznie postacie, którymi użytkownik steruje osobiście** (np. Brax, Nyxa, Patryk, Monika w roli grywalnej). Głowa/twarz do 4K, ciało/ubranie do 2K, buty/akcesoria do 1K; **tylko gdy postać faktycznie ma oddzielne materiały i UV**. Zwykli NPC, przeciwnicy i przechodnie są wyłączeni z tej reguły. Nie zmieniać wyglądu, obecnych tekstur, podziału UV ani materiałów bez przeglądu modelu i osobnej decyzji właściciela. Dotychczasowe postacie nie są przez tę notatkę przerobione. Szczegóły: [Tekstury postaci grywalnych](docs/zasady/TEKSTURY_POSTACI_GRYWALNYCH_20261105.md). Zmiana dokumentacji nie uruchamia buildu.

# Aktualna decyzja właściciela — testowy APK bez blokad jakości

Stosuj [APK do ręcznej oceny](docs/zasady/APK_RECZNA_OCENA_20261105.md).
Kontrole jakości postaci, kolizji i przejść mają ostrzegać, nie blokować APK.
Fizyczne kolizje pozostają. Kompilacja, generowanie, podpis i plik APK nadal muszą się udać.
Ta nowsza decyzja zastępuje sprzeczne starsze bramki jakości. Bez uruchamiania buildów.

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

## WYMÓG WŁAŚCICIELA: APK PRZED BACKUPEM — 4.11.2026

Dotyczy wszystkich wykonawców i rejonów: **Miasto Mistrzów/miasto, osiedle, obóz, wyspa i arena**. Pełna reguła: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.

**Skompilowany i zweryfikowany APK musi zostać zabezpieczony w Actions artifact i udostępniony jako testowy APK przed rozpoczęciem opcjonalnego zapisu Library, checkpointów i baz aktualizacji.** Późniejsze błędy cache/backup raportuj jako warning, nie zabieraj właścicielowi już gotowego APK. Nie kasuj jedynej kopii. W raporcie zawsze konkretny bieżący SHA, numer runu, link, wariant, package ID i informacja, czy release jest draftem wymagającym dostępu. Nie nazywaj testem Androida samej kompilacji. Nie pokazuj starych buildów jako najnowszych. Zapis polityki nie jest zgodą na build, automatyczny start ani ponowienia.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
