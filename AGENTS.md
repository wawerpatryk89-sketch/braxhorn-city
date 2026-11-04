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

Przed pracą w tym regionie przeczytaj [mapę i przekazanie](docs/zlecenia/MIASTO-MISTRZOW-START.md). Tam są pełny plan PDF, dwa podglądy map, aktualny rzut generatora, źródła współrzędnych, standard jakości i status pierwszego buildu. Zachowaj układ, oryginalne zasoby i skalę; dokumentacja odróżnia plan, istniejący kod i wynik sprawdzony w Unity. Powyższe zasady ręcznego uruchamiania buildów pozostają w mocy.
