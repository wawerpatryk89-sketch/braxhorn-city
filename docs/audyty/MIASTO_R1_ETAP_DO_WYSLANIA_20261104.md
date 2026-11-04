# BRAXHORN CITY — MIASTO R1 / ETAP DO ZAMKNIĘCIA I WYSŁANIA (4.11.2026)

**Status:** przygotowanie kodowe i bramek jakości do jednego testu Android, **nie ukończenie całego miasta**, nie publikacja produkcyjna. Kolejne kwartały, budynki i funkcje będą dopracowywane w następnych etapach, bez naruszania tej bazy.

**Gałąź:** `codex/miasto-naprawy-filmow-20261104` (kontynuacja istniejącej pracy, bez przenoszenia do nowej gałęzi). **Silnik:** Unity 6 `6000.0.58f2`. **Wariant testowy:** tylko `miasto` / `braxhorn-miastogl.apk`, OpenGL ES 3, `com.braxhorn.miastogl`. **Zakaz push-build:** tylko `workflow_dispatch` po wyraźnej zgodzie właściciela.

## 1. Co zamykamy w tym etapie

- Istniejąca siłownia startowa i wejście do miasta, sterowanie i kamera.
- Istniejące ulice oraz nowe dzielnice **21/21 segmentów** z prawdziwymi siatkami i teksturami; zachowane szyldy i styl premium, bez fasad-atráp, pustych pól i rusztowań.
- Wcześniej przygotowana marina: minimum 3 jachty, 6 motorówek, 4 skutery; wejście, pokład, ster, fotel, zejście i wypłynięcie na otwartą wodę.
- Metro M1 w zakresie tego etapu: dostępne wejście, schody/peron i inscenizowany kurs. **Nie jest to jeszcze kompletna sieć metra ani rzeczywiste drzwi oryginalnego GLB**.
- Tramwaj T1: jeden rzeczywisty CAF, zamknięta pętla, 6 przystanków, bez kolizji z budynkami; drzwi i wsiadanie wymagają testu importu i telefonu.
- Istniejące, zadeklarowane lokale: NOVA, mieszkanie Braxa, Tower z windą, apteka, Nutrition, supermarket i oznakowanie, bez dodatkowych obiektów w przyszłej części miasta.
- Rozdzielenie nabrzeża, wyspy i sektorów 150×150 m z loadingiem i bez znikających kolizji.

**Poza zakresem zamknięcia:** przyszłe kwartały, nowe ulice, nowe dzielnice i wszystkie niezamówione rozszerzenia. Nie usuwać ani nie zmieniać obecnej geometrii, oznakowania czy jakości, aby przyspieszyć build.

## 2. Co powinno być potwierdzone PRZED jednym ręcznym buildem

- [ ] W aktualnym HEAD są poprawki 3 P0 z audytu #84: T1 z kolizją zatrzymuje odbiór; brakujące budynki, niepełny PBR i rusztowania scalone zatrzymują odbiór; wejście M1 musi leżeć na lądzie i mieć drożne dojście.
- [ ] Błędy etapów miasta B i jego podetapów nie mogą zostać ukryte pod `OK` — agregacja i jawny wyjątek na końcu generowania.
- [ ] Sceny nabrzeża, wyspy i kwadratów nie mogą zapisywać zerwanych referencji między scenami; wykryte odwołanie cross-scene wstrzymuje build zamiast generować niekompletny obszar.
- [ ] `check_city_cs.py` działa statycznie; jest to tylko filtr, **nie zastępuje kompilacji C# w Unity**.
- [ ] Oryginalne zasoby pobrane i potwierdzone: 21 budynków, murale, atlas pokoi, model CAF, metro GLB, szyldy, 24 leki, 4 odżywki, ekspres i wiaty. Jeżeli brakuje źródła lub praw do użytku — raportować, nie tworzyć atrapy.
- [ ] Sprawdzić brak `unity-city/ProjectSettings/ProjectSettings.asset` w repo: Unity używało projektu-szablonu w #84. **Nie odtwarzać samodzielnie ustawień wizualnych**; przed przyjęciem APK wykazać rzeczywiste Graphics APIs, przestrzeń kolorów, rozdzielczości, render pipeline, podpis i `applicationId`.
- [ ] Próba odzysku zgodnego snapshotu `Library` ze starszej gałęzi musi mieć rzeczywistą weryfikację SHA-256; brak cache to jawny cache-miss, nie sukces.

## 3. Jednorazowy test techniczny — wyłącznie po zatwierdzeniu

- [ ] Build uruchamiany RĘCZNIE: `Claude - region R1 na gotowo (TEST, bez publikacji)`, `workflow_dispatch`, gałąź jak wyżej, rejon **miasto**, pamięć **tak**, tylko APK **miastogl**. Bez osiedla i obozu. Brak automatycznych retry.
- [ ] Unity 6000.0.58f2: kompilacja C# 0 błędów, wygenerowane wszystkie sceny, w raportach 0 blokujących `NIEGOTOWE`, 21/21 budynków, 0 kolizji T1, M1 na lądzie.
- [ ] Dowód danych: log pełny z wersją Unity, zasięgiem kwadratów, raporty mariny/T1/M1/fasad, ilość assetów, rozmiar APK, RAM podczas generowania i wynik cache.
- [ ] APK oznaczone `com.braxhorn.miastogl`, tym samym certyfikatem co istniejący odpowiedni pakiet na telefonie, z wyższym `versionCode`. **Nie instalować jako aktualizacji `com.braxhorn.city` lub `com.braxhorn.miasto` — to inne identyfikatory.**
- [ ] Na Androidzie: aktualizacja bez odinstalowania tylko przy zgodnym pakiecie; start siłowni → miasto → NOVA/Tower/apteka → metro → pełna trasa tramwaju → marina → wyspa → powrót. Bez crasha, ścian niewidzialnych i blokad; FPS/RAM/start, zdjęcia oraz krótki film.
- [ ] Jeśli CI albo Android nie przejdzie: raport **całej** listy usterek, jeden zbiorczy zestaw poprawek, ponowny audyt; nie uruchamiać następnego buildu bez zgody.

## 4. Podział wyniku

**Kod przygotowany** = zmiany skryptów, guardy, checklisty; jeszcze nie wiadomo, czy Unity i APK przejdą.

**Etap R1 testowy zaakceptowany** = rzeczywisty build Unity + dowody, brak P0, pomiary i gra uruchomiona na telefonie. Dopiero wtedy zamykamy obecny etap i kontynuujemy rozbudowę bez traktowania miasta jako skończonego.

**Mała aktualizacja telefonu:** obecny pełny APK nie jest patchowaniem tylko zmienionych plików. GitHub cache i podział na sceny przyspieszają CI/uruchamianie, ale nie zastępują zgodnego mechanizmu pobierania paczek zawartości.

**Materiały źródłowe:** `docs/audyty/MIASTO_BUILD84_20261104_AUDYT_ZBIORCZY.md`, [build #84](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41850269418), poprzedni audyt rozmowy z 4.11.2026.
