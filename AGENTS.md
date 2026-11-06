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

# PILNE — APK FIRST (4.11.2026)

**Polecenie właściciela dla wszystkich wykonawców włącznie z Miastem Mistrzów:** po pomyślnym buildzie natychmiast udostępnij i zabezpiecz APK przed backupem, cache i raportami. Późniejszy błąd nie może odebrać gotowego pliku. Pełna zasada: [APK FIRST](docs/zasady/APK_FIRST_DOSTAWA_20261104.md), [Issue #13](https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13). Nie uruchamiać buildów bez zgody właściciela.

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

## Aktualny etap miasta R1 — obowiązkowy przed wysyłką

Dla gałęzi `codex/miasto-naprawy-filmow-20261104` obowiązuje [zakres i warunki zamknięcia R1](docs/audyty/MIASTO_R1_ETAP_DO_WYSLANIA_20261104.md). Miasto nie jest ukończone w całości: obecny etap obejmuje tylko istniejące sektory i 21 zadeklarowanych segmentów. Nie dodawać nowych kwartałów do zakresu tego testu. Przed jednym ręcznie zatwierdzonym buildem zweryfikować agregację błędów miasta B, kompletność budynków, kolizje T1, dojście do M1, referencje między scenami i prawidłowy package ID. **Zapis w repo nie oznacza pozytywnego odbioru Unity/Android.**

## DOSTAWA APK — ZASADA OBOWIĄZKOWA W MIEŚCIE I MIEŚCIE MISTRZÓW

Dotyczy również osiedla, obozu, wyspy i BRAXHORN ARENA. Zasada dla wszystkich wykonawców: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.

**Jeśli kompilacja Unity i walidacja APK się powiodły, właściciel musi otrzymać dostęp do tego konkretnego pliku, także gdy później zawiedzie cache, snapshot, backup lub zapis Library.** Przed backupem zapisz kopię do Actions artifact, wyślij APK do testowego wydania, sprawdź jego obecność, przekaż link do aktualnego SHA i numeru runu. Raportuj oddzielnie: Unity, podpis/wersję APK, upload oraz test telefonu. Nie deklaruj, że plik jest publicznie dostępny, jeśli wydanie jest roboczym draftem wymagającym zalogowania. Błąd opcjonalnego backupu po poprawnym APK = warning, nie blokada instalatora. Nie uruchamiaj builda ani retry bez zgody właściciela. Pokazuj wyłącznie najnowszy build z aktualnymi poprawkami.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
