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
