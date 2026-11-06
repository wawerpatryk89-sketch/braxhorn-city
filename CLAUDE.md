## AKTUALNA INSTRUKCJA WŁAŚCICIELA — 06.11.2026

**REJON: OSIEDLE.** Każdą odpowiedź zacznij od `OSIEDLE —`. **Zanim rozpoczniesz pracę, odczytaj cały [AGENTS.md](AGENTS.md), od pierwszej linijki.** Znajduje się tam pełna globalna instrukcja z 06.11.2026, aktualna lista czterech gałęzi, kolejność prac, zasady jakości i bezpieczeństwa buildów. Jest ona nadrzędna względem sprzecznych wcześniejszych zapisów poniżej (w szczególności starszych wyjątków uruchamiania buildu przez zmianę pliku zlecenia). APK wolno uruchomić **tylko po wyraźnej decyzji właściciela, pojedynczo przez `workflow_dispatch`**. Sam zapis instrukcji/commit/push **nie jest zgodą na build**. Nie modyfikuj `main`, nie mieszaj rejonów i nie usuwaj niczego bez wcześniejszej listy oraz akceptacji właściciela.

---

# Instrukcja dla Claude / Braxhorn City

Przeczytaj i wykonuj [AGENTS.md](AGENTS.md). Najważniejsza decyzja właściciela z 4.11.2026: push i commit NIE uruchamiają buildów. Miasto, osiedle i obóz: tylko osobna ręczna decyzja właściciela i `workflow_dispatch`. Zakaz samodzielnych restartów, automatycznych wyzwalaczy i pushowania na main.

Dostawa APK przed cache i globalne zasady: https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13.
