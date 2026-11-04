# PRIORYTET: DOSTAWA APK FIRST

Przeczytaj [AGENTS.md](AGENTS.md) i [zasadę APK FIRST](docs/zasady/APK_FIRST_DOSTAWA_20261104.md). Najpierw zabezpieczyć i udostępnić APK, potem cache; żaden późniejszy błąd cache nie odbiera właścicielowi gotowej aplikacji.

# Instrukcja dla Claude / Braxhorn City

Przeczytaj i wykonuj [AGENTS.md](AGENTS.md). Najważniejsza decyzja właściciela z 4.11.2026: push i commit NIE uruchamiają buildów. Miasto, osiedle i obóz: tylko osobna ręczna decyzja właściciela i `workflow_dispatch`. Zakaz samodzielnych restartów, automatycznych wyzwalaczy i pushowania na main.

Na bieżącej gałęzi miasta przed buildem przeczytaj także `docs/audyty/MIASTO_R1_ETAP_DO_WYSLANIA_20261104.md`. To odbiór obecnego etapu, nie całej przyszłej zabudowy. Nie uruchamiaj builda bez aktualnej zgody.

Wymóg wydania aktualnego APK przed backupem: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13. Także Miasto Mistrzów. Nie uruchamiaj samodzielnie buildu.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
