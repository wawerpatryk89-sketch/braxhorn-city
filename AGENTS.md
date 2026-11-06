<!-- ZASADY BRAXHORN: 06.11.2026; REJON OSIEDLE; AKTUALNA DECYZJA WŁAŚCICIELA -->
**IDENTYFIKACJA PRACY: OSIEDLE —** każdą odpowiedź wykonawcy na tej gałęzi rozpocznij od tego identyfikatora. Nie zgłaszaj cudzych prac jako własnych.

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

**Na tej gałęzi pierwsze słowa KAŻDEJ odpowiedzi wykonawcy (w tym nowy czat, raport, status, zlecenie, komentarz) muszą brzmieć: `OSIEDLE —`.** Dopiero po nich informacja o wykonywanych pracach, gałęzi, stanie, testach, błędach, planie i dowodach. Nie zaczynać od „Witam”, „Sprawdzam”, „Raport”, „OBSZAR PRAC:” ani od jakiegokolwiek innego nagłówka.

**Globalne cztery prawidłowe początki — wybierz tylko rzeczywisty obszar własnego zadania:**
- `OBÓZ — ...`
- `MIASTO MISTRZÓW — ...`
- `MIASTO R1 — ...`
- `OSIEDLE — ...`

Gdy zadania nie przypisano jednoznacznie, pierwszy wyraz `NIEUSTALONY — ...`; nie udawać przypisania. Nie mylić miasta R1 z Miastem Mistrzów i nie podawać cudzych prac jako swoich. Nagłówek ma być widoczny w pierwszym wierszu każdej odpowiedzi, a nie tylko wewnątrz pliku, zadania lub tytułu pull requesta.

Zasada identyfikacji nie zmienia scen, kodu Unity, materiałów ani workflow. Nie uruchamia buildów i nie upoważnia do pracy nad inną gałęzią.

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

## WYMÓG WŁAŚCICIELA: APK PRZED BACKUPEM — 4.11.2026

Dotyczy wszystkich wykonawców i rejonów: **Miasto Mistrzów/miasto, osiedle, obóz, wyspa i arena**. Pełna reguła: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.

**Skompilowany i zweryfikowany APK musi zostać zabezpieczony w Actions artifact i udostępniony jako testowy APK przed rozpoczęciem opcjonalnego zapisu Library, checkpointów i baz aktualizacji.** Późniejsze błędy cache/backup raportuj jako warning, nie zabieraj właścicielowi już gotowego APK. Nie kasuj jedynej kopii. W raporcie zawsze konkretny bieżący SHA, numer runu, link, wariant, package ID i informacja, czy release jest draftem wymagającym dostępu. Nie nazywaj testem Androida samej kompilacji. Nie pokazuj starych buildów jako najnowszych. Zapis polityki nie jest zgodą na build, automatyczny start ani ponowienia.
