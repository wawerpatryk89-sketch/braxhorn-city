# OBOWIĄZKOWE PIERWSZE SŁOWA ODPOWIEDZI — IDENTYFIKACJA OBSZARU

Globalne polecenie właściciela z 6.11.2026: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13

**Na tej gałęzi pierwsze słowa KAŻDEJ odpowiedzi wykonawcy (w tym nowy czat, raport, status, zlecenie, komentarz) muszą brzmieć: `MIASTO R1 —`.** Dopiero po nich informacja o wykonywanych pracach, gałęzi, stanie, testach, błędach, planie i dowodach. Nie zaczynać od „Witam”, „Sprawdzam”, „Raport”, „OBSZAR PRAC:” ani od jakiegokolwiek innego nagłówka.

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
