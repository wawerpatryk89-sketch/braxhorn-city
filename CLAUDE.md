## AKTUALNA INSTRUKCJA WŁAŚCICIELA — 06.11.2026

**REJON: MIASTO MISTRZÓW.** Każdą odpowiedź zacznij od `MIASTO MISTRZÓW —`. **Zanim rozpoczniesz pracę, odczytaj cały [AGENTS.md](AGENTS.md), od pierwszej linijki.** Znajduje się tam pełna globalna instrukcja z 06.11.2026, aktualna lista czterech gałęzi, kolejność prac, zasady jakości i bezpieczeństwa buildów. Jest ona nadrzędna względem sprzecznych wcześniejszych zapisów poniżej (w szczególności starszych wyjątków uruchamiania buildu przez zmianę pliku zlecenia). APK wolno uruchomić **tylko po wyraźnej decyzji właściciela, pojedynczo przez `workflow_dispatch`**. Sam zapis instrukcji/commit/push **nie jest zgodą na build**. Nie modyfikuj `main`, nie mieszaj rejonów i nie usuwaj niczego bez wcześniejszej listy oraz akceptacji właściciela.

---

# PRIORYTET: DOSTAWA APK FIRST

Przed pracą przeczytaj [AGENTS.md](AGENTS.md) i [zasadę gwarancji APK](docs/zasady/APK_FIRST_DOSTAWA_20261104.md). Gotowy APK nie może przepaść przez późniejszy cache; do raportu podawaj wyłącznie najnowszy build z aktualnymi poprawkami.

# Instrukcja dla Claude / Braxhorn City

Przeczytaj i wykonuj [AGENTS.md](AGENTS.md). Najważniejsza decyzja właściciela z 4.11.2026: push i commit NIE uruchamiają buildów. Miasto, osiedle i obóz: tylko osobna ręczna decyzja właściciela i `workflow_dispatch`. Zakaz samodzielnych restartów, automatycznych wyzwalaczy i pushowania na main.


## Wspólny standard wykonania i napraw — 4 listopada 2026

Na polecenie właściciela przed kolejną pracą przeczytaj i stosuj [standard Unity 6 dla wszystkich wykonawców](docs/zasady/STANDARD_PRACY_UNITY6_20261104.md). Zawiera przypięte wersje Claude Anthropic, wymagania PBR i 4K blisko / minimum 2K dalej, przygotowanie buildów, udokumentowane awarie i stan napraw. Dodanie instrukcji nie oznacza gotowości istniejących scen. Zachowaj bieżące zasady zlecania buildów, cudze zmiany i osobne rejony; aktualizuj gałąź z kontrolą oczekiwanego HEAD.
