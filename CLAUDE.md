# Instrukcja dla Claude / Braxhorn City

Aktualizacja obozu: właściciel nakazał wykonawcy samodzielny start po każdym
jednoznacznym poleceniu buildu. Wyjątek dla `gpt/oboz` i osobnego pliku
zlecenia, kontrola bazy SHA oraz zakaz duplikatów są na początku AGENTS.md.
Zwykłe commity nadal nie stanowią zlecenia. Reguły miasta/osiedla bez zmian.

Przeczytaj i wykonuj [AGENTS.md](AGENTS.md), zaczynając od najnowszej decyzji.
Miasto i osiedle: wyłącznie osobna decyzja właściciela i `workflow_dispatch`.
Obóz: ta sama decyzja właściciela, a wykonawca może wysłać opisane wyżej
zlecenie startu. Bez samoczynnych restartów i pushowania na main.

Dostawa gotowego APK: najpierw artifact/release po podpisaniu, potem opcjonalny cache. Link dla właściciela z aktualnego runu. Obowiązuje https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13 i AGENTS.md.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
