# BRAXHORN CITY — OBOWIĄZKOWA ZASADA WŁAŚCICIELA

Dotyczy wszystkich wykonawców, także Miasta Mistrzów, miasta, osiedla, obozu i wyspy. Kanoniczna reguła: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.

**Nie uruchamiać kompilacji Unity 6 bez wyraźnej zgody właściciela; zwykły commit/push nie jest zgodą.** Żadnego samoczynnego retry, łączenia gałęzi ani produkcyjnej publikacji.

**Gotowy APK = priorytet dostawy.** Po pozytywnej kompilacji i weryfikacji podpisu/identyfikatora/wersji najpierw zabezpieczyć plik jako Actions artifact i wydać go jako testowy APK, a dopiero potem zapisywać zaszyfrowane cache, bazę aktualizacji i checkpoint. Niezależne błędy cache/snapshot po zbudowaniu poprawnego APK oznaczać jako ostrzeżenia, nie blokować dostępu do gry. Nie kasować jedynej kopii APK. Link dla właściciela musi dotyczyć aktualnego SHA i być faktycznie dostępny — zaznaczać, kiedy draft wymaga zalogowania. Publikacja APK nie może być zatajona pod czerwonym statusem nieistotnego backupu.

**Raporty wyłącznie najnowszych buildów zawierających aktualne poprawki.** Oddzielać: wynik Unity, weryfikację APK, udostępnienie pliku, test telefonu. Brak APK oznacza brak możliwej oceny wizualnej, nie „gotowe”.
