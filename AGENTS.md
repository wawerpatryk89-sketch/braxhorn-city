<!-- ZASADY BRAXHORN: 06.11.2026; REJON OBÓZ; AKTUALNA DECYZJA WŁAŚCICIELA -->
**IDENTYFIKACJA PRACY: OBÓZ —** każdą odpowiedź wykonawcy na tej gałęzi rozpocznij od tego identyfikatora. Nie zgłaszaj cudzych prac jako własnych.

# BRAXHORN CITY — GLOBALNA INSTRUKCJA PRACY (06.11.2026)

**WŁAŚCICIEL: instrukcja nadrzędna dla wszystkich wykonawców, obowiązuje w każdej rozmowie i na każdej poniższej gałęzi.** Jeśli starsze fragmenty tego pliku, `CLAUDE.md`, inne dokumenty albo automatyzacje są sprzeczne z tą instrukcją, stosuj niniejszą nowszą decyzję. Starsze informacje poniżej zachowano jako kontekst historyczny; nie stanowią one zgody na build ani odstępstwo. Najpierw odczytaj gałąź, przypisany rejon i aktualny stan repozytorium. Nie wykonuj prac w innym rejonie.  

Repo: `wawerpatryk89-sketch/braxhorn-city`. Silnik: Unity 6, Android APK.
Jeden rejon = jedna gałąź. Nie mieszaj rejonów, nie pushuj na main.
- Miasto R1: `claude/miasto-opus-20261031`
- Osiedle: `claude/osiedle-42-poprawka`
- Obóz: `codex/oboz-przebudowa-pbr-20261105`
- Miasto Mistrzów: `gpt/miasto-mistrzow-kasyno-t11-20261105`

## 1. BUILDY
- APK uruchamiasz tylko na wyraźne słowo właściciela, jeden naraz, przez `workflow_dispatch`. Żadnych restartów w ciemno i żadnych automatycznych wyzwalaczy.
- Przed każdym APK: przebieg „scena bez APK”, który zbiera WSZYSTKIE błędy naraz (lista na końcu logu), a nie zatrzymuje się na pierwszym.
- Twardy błąd tylko przy: brakującym pliku, złym typie zasobu, braku modelu lub klipu, błędzie kompilacji. Kontrole kosmetyczne (rozmiar importu, mipmapy, liczba sztuk) = tylko ostrzeżenie.
- Po padzie: pierwszy wyjątek + stack trace, przyczyna, poprawka WSZYSTKICH podobnych miejsc (np. wszystkich tekstur, nie jednej).
- Każdy zasób musi być na miejscu ze stałym `.meta` (stały GUID, `textureShape` 2D dla obrazków), zanim Unity wystartuje.
- Gdy ostatni APK padł, nie dodajesz nowych funkcji, tylko doprowadzasz build do końca.
- Bez zdjęć w CI. Dowodem jest nagranie właściciela z telefonu, a nie log.

## 2. WYGLĄD I JAKOŚĆ
- Skala K = 1,294. Każdy model = realny wymiar × K. Nigdy nie wciskaj modelu w wymyślone pudełko.
- Każdy przedmiot stoi na podłodze lub gruncie (raycast w dół), a rzeczy ścienne (pisuar, lustro, półka, szyld) przy ścianie. Nic w powietrzu, nic w ścianie, nic na sobie nawzajem.
- Postacie: stopy na ziemi, bez lewitowania i ślizgania.
- Szyldy na belce lub ścianie, nie na patykach w powietrzu. Murale min. 4096 px szerokości, na dużych ścianach 8192 px. Nie rozciągaj małych obrazków na całe ściany.
- Lustro = prawdziwe odbicie (reflection probe), nie kolorowy prostokąt.
- Tekstury: 4K tylko główne postacie, reszta 2K, ASTC. LOD, instancje GPU, światła wypalone lub vertex. Gra ma chodzić płynnie na średnich ustawieniach.
- Nie wstawiaj niczego, o czym właściciel nie mówił (np. żadnego dźwigu w Mieście Mistrzów). Nie wymyślaj „zatwierdzeń właściciela”. Gdy brakuje modelu, dajesz właścicielowi listę, a nie zastępstwo z klocków.

## 3. PORZĄDKI (najpierw lista do akceptacji właściciela, dopiero potem usuwanie)
- Releases: cache CI (`unity-ci-cache-*`), zostaje tylko ten używany przez ostatni udany build danego rejonu. Stare APK, zostaje ostatni działający w każdym rejonie.
- Workflow: usuń jednorazowe i dublujące się (`*-once-*`, `*-smoke-*`, `*-probe-*`, kolejne preflighty). Na rejon zostają: jeden APK, jeden „scena bez APK”, jeden test źródeł.
- Usuń testy i pliki rzeczy, których ma nie być (np. dźwig w Mistrzach). Gałęzie po scaleniu usuń.

## 4. REJONY — KOLEJNOŚĆ PRAC
- Miasto Mistrzów: najpierw działający APK. Potem arena premium: przeszklona fasada, mural HD na całą ścianę za szkłem po wejściu, trybuny, dwa murale boczne HD, pas 3D w szklano-drewnianej kopule na podeście. Następnie kasyno WAWER, basen P1, metro scalone do 5 stacji, mały port bez dużych statków i bez dźwigu, prom, plaże, promenada z szyldami właściciela (każdy szyld osobnym plikiem, nie atlas).
- Obóz: wg audytu z 6.11. Kolejno: skala i stawianie modeli (nic w powietrzu), grunt (błoto, ubita ziemia, chodniki z obrzeżami wszędzie, trawa premium, ślady stóp na piasku i błocie, uginająca się mata), murale na ścianach, szyld na belce bramy, łaźnia z poprawną cegłą i lustrem, klimat obozu (domki z bali, 40–60 worków na belkach, ognisko, namioty, bordowe flagi, beczki, żarówki), Monika na ziemi, APK maks. 400 MB.
- Osiedle: najpierw audyt z nagrań właściciela (lista błędów z miejscem i przyczyną w kodzie), dopiero potem naprawy wg tych zasad. Nie ruszaj tego, co działa.
- Miasto R1: tak samo jak osiedle. Ładowanie ma być płynne jak na osiedlu. Duże statki później.

## 5. KOMUNIKACJA
- Po każdym kroku commit i push z numerem commita.
- Raport krótki: co zrobione, co padło (pierwszy błąd), co dalej. Bez „gotowe”, dopóki właściciel nie zobaczy tego na telefonie.
- Pytasz tylko przy decyzji nieodwracalnej (usuwanie, zmiana układu terenu). Resztę robisz.

---

## WCZEŚNIEJSZE INSTRUKCJE TEJ GAŁĘZI — KONTEKST HISTORYCZNY

**Zachowane bez usuwania; w przypadku sprzeczności nadrzędna jest globalna instrukcja właściciela z 06.11.2026 zamieszczona wyżej.**

---

# OBOWIĄZKOWE PIERWSZE SŁOWA ODPOWIEDZI — IDENTYFIKACJA OBSZARU

Globalne polecenie właściciela z 6.11.2026: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13

**Na tej gałęzi pierwsze słowa KAŻDEJ odpowiedzi wykonawcy (w tym nowy czat, raport, status, zlecenie, komentarz) muszą brzmieć: `OBÓZ —`.** Dopiero po nich informacja o wykonywanych pracach, gałęzi, stanie, testach, błędach, planie i dowodach. Nie zaczynać od „Witam”, „Sprawdzam”, „Raport”, „OBSZAR PRAC:” ani od jakiegokolwiek innego nagłówka.

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

# Aktualizacja właściciela — start obozu na polecenie, 4.11.2026

Wymóg dostawy: jeżeli plik APK powstał, zachowaj go do pobrania również przy
błędzie późniejszego kroku, kompilacji lub weryfikacji. Podaj uczciwy status
kontroli; kopii po błędzie nie nazywaj sprawdzonym wydaniem ani testem telefonu.
Nie ukrywaj pliku wyłącznie dlatego, że cały job jest czerwony. Jeżeli APK nie
powstał lub nie udało się zachować jego bajtów, wyjaśnij dokładnie ten brak.

Nowsze polecenie właściciela dla obozu: „Masz puścić ten bulic samodzielnie,
za każdym razem, jak będę chciał”. Wykonawca ma sam wysłać jedno zlecenie
buildu po jednoznacznym poleceniu właściciela, bez wymagania od niego klikania
w GitHubie lub logowania do dodatkowej usługi. Ten wyjątek dotyczy wyłącznie
APK obozu na `gpt/oboz`; nie zmienia zasad miasta ani osiedla.

Workflow obozu może uruchomić się po zmianie WYŁĄCZNIE zlecenia
`unity-tools/oboz/build-request.json`. Każde nowe zlecenie wymaga aktualnego
polecenia właściciela, nowego identyfikatora, `ownerRequested: true`, SHA
sprawdzonej bazy i zgodnego manifestu dostawy. Przed wysłaniem sprawdź runy;
nie dubluj istniejącego zlecenia. Zwykłe commity kodu, PR, merge, harmonogram
ani błąd poprzedniego runu nie są zleceniem i nie mogą tworzyć nowego zlecenia.
Wysłanie następuje pojedynczym commitem na sprawdzonej bazie, z kontrolą
oczekiwanego SHA gałęzi. Ręczne `workflow_dispatch` pozostaje dostępne.

Aktualnie zlecono jeden build całego połączonego obozu po #6: trzy areny,
zachowanie NPC, dźwięki i zabezpieczenie dostawy APK. Bez samoczynnych ponowień.
Poniższe wcześniejsze reguły nadal obowiązują poza tym konkretnym wyjątkiem.

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

---

## Dotychczasowe zasady obozu — zachowane

# Obóz — zasady kontynuacji

Przed zmianami procesu aktualizacji przeczytaj `unity-tools/updates/STANDARD-AKTUALIZACJI.md` oraz `unity-tools/updates/OBOZ-WDROZENIE.md`.

Standard właściciela pochodzi z gałęzi `codex/zone-updates-20261104`, commit `38422c5f031b5f9b85a3e1981c1a8199c9f75731`; SHA pliku: `9fd6e212fb4baa72f453af01f316583ccfecdaa4`. Kopia jest niezmieniona. Nowszy uzgodniony standard przenoś z konkretnym SHA; nie twórz alternatywnej wersji wspólnych narzędzi.

Wspólne `unity_cache.py`, `durable_cache.py`, `world_checkpoint.py` i ich testy przeniesiono bez zmian z `1d83c1a6db4f6c71eb670e5a57bab963ce50e5c1`. Build obozu: `unity-tools/oboz/README.md`. `build-request.json` jest tylko dokumentem zlecenia, NIGDY automatycznym wyzwalaczem. Nie twórz zlecenia przed kompletną, zaszyfrowaną dostawą zasobów.

Zakres: tylko obóz. Monika jest graczem; NPC to VORN, ZYRO, RAKK, KADE, GORR i Miś. Modele innych zawodników służą wyłącznie jako źródła animacji. Zachowaj jakość i ustalony wygląd.

Nie zastępuj miasta/osiedla generatorem obozu, nie scalaj całych gałęzi stref i nie restartuj cudzego buildu. Prywatne źródła, eksporty scen, sekrety i cache nie mogą trafić do publicznego gita.

Zachowuj `.meta`, GUID, trwałe adresy i zapisy gry. Nie przedstawiaj podpisu APK, cache importu, rejestru zasobów ani samego kodu jako działającej aktualizacji zawartości. Osobno raportuj kontrolę kodu, uruchomienie Unity i test tej samej instalacji na telefonie.

## GOTOWY APK MUSI BYĆ DOSTĘPNY WŁAŚCICIELOWI — reguła 4.11.2026

**Obowiązuje także Miasto Mistrzów, miasto, osiedle, obóz, wyspę i wszystkie przyszłe rejony.** Decyzja właściciela (wspólna): https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.

Po zakończeniu kompilacji Unity i sprawdzeniu podpisu/identyfikatora/wersji **NATYCHMIAST ZABEZPIECZ APK w Actions artifact oraz testowym release, przed jakimkolwiek zapisem cache/Library/checkpoint**. Przekaż właścicielowi link do prawidłowego SHA/run i pliku; wyraźnie zaznacz dostęp tylko dla zalogowanych do draftu. Nie usuwaj jedynej kopii pliku. Awaria opcjonalnego backupu już po gotowym APK to ostrzeżenie, NIE powód do odebrania właścicielowi możliwości testowania. Jeśli publikacja release padła, wskaż bezpośrednio kopię w Actions artifact; nie zatajać jej pod czerwonym buildem. Rozróżniaj: kompilacja, podpis, upload, test na telefonie. Nie deklaruj sukcesu instalacji bez testu. Raportuj tylko najnowsze buildy zawierające bieżące poprawki. **Ta reguła nie upoważnia do samodzielnych buildów ani retry.** Istniejące wyraźne wyjątki właściciela do uruchomienia pojedynczego buildu pozostają bez zmian.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
