# BRAXHORN CITY — OSIEDLE | audyt i poprawki 4.11.2026

**Zakres:** wyłącznie osiedle Unity 6. Źródło liczb: realny log GitHub Actions [run #41751910203](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41751910203), commit a4c9160. Nowe zmiany są na odrębnej gałęzi i wymagają walidacji w następnym buildzie Android. Nie scalamy automatycznie zmian innych wykonawców.

## Potwierdzone błędy (stan starszego builda)

- Migotanie: 3978 par powierzchni przed naprawą, 1399 po pierwszym wycięciu, w tym 181 par większych niż 1 m². Arena: 57 przed / 19 po. **Uwaga:** część par jest celowo przezroczysta (laser, szkło, woda); liczba nie jest równoznaczna z liczbą widocznych artefaktów.
- Salon samochodowy: `BRAXHORN_SALON układ DO POPRAWKI: auto na placu poza działką (x -38.5)`.
- Piłka nożna: `piłka nożna: 2 bramki — boisko do kosza w Parku Wiśniowym (brak wolnego trawnika)`. Koszykówka, tenis i siatkówka były już utworzone. Moduł `KraniecMapy()` wywołuje `Sport` i `RuchNaOsiedlu`; nie dodajemy drugiego wywołania.
- Ruch: 31 przeszkód zgłoszonych w obszarze jezdni, symulacja 20 aut i 600 s: 69 zakleszczeń, bez nachodzeń i wyjazdów poza drogę.
- Malowanie poziome: kontrola zgłosiła 19 nakładek; część wycięć asfaltu przerwana limitem złożoności, bezpieczny odstęp awaryjny może zostać w grze.
- Patryk: w starszym logu Walk miał `stopa_max=140.5°` (19 naruszeń >35°), 6 klatek kolan w tył i 2 przeprosty >10°. Następne zmiany rigu są realizowane także przez osobnego wykonawcę.

## Zmiany przygotowane na gałęzi audytu (wymagają testu Unity)

1. Dyskoteka: 13 lóż U z 3 pełnych sof Chesterfield każda, zamiast wyodrębniania pojedynczych małych MeshFilter jako fotela. Jest to zmiana układu źródłowego, nie nakładka.
2. Salon: cofnięte podesty od głównego wejścia, odsunięte schody i test fizycznej kapsuły na przejściu; auto przed budynkiem przesunięte z x=-38.5 na x=-38.8; brama aut pozostaje w oryginalnej szerokości.
3. Patryk: `walkOnly=false`, wymagany klip `Run`, `BraxRuchySport` do zagrywek, wyłączanie pozy sportowych podczas jazdy/siedzenia.
4. Piłka nożna: poszukiwanie osobnego trawnika dla boiska 18×11, 14×8 lub 11×6 m; zabroniony awaryjny montaż na boisku do kosza. Brak wolnego miejsca ma przerwać build zamiast fałszywego sukcesu.
5. Sport i samochód: walidacja wygenerowanych rzeczywistych obiektów Unity — kosz, piłka nożna, tenis, klip biegu i prywatne auto (fotel, Rigidbody, BoxCollider, do 40 m od areny).
6. Migotanie: drugi przebieg wycięcia wyłącznie dla dużych, nieprzezroczystych par i pozostałości areny; dodatkowy log liczby nierozwiązanych par opaque. Przezroczyste efekty nie są usuwane.

## Warunki akceptacji (nieudowodnione bez nowego builda)

- `BRAXHORN_SALON_PRZEJSCIE OK`, realna trasa w drzwiach i na schodach.
- `BRAXHORN_OSIEDLE_INTERAKCJE_OK` i właściwy obszar boiska piłki nożnej w Parku Centralnym, bez kolizji z koszykówką.
- Patryk potrafi chodzić/biegać, kozłować/rzucać, kopać/podawać i odbijać piłkę rakietą bez deformacji kości.
- `BRAXHORN_MIGOTANIE_POWTORKA` + porównanie `BRAXHORN_ARENA_MIGOTANIE` z 57/19; nie obiecywać zera przed pomiarem.
- Ruch samochodów i zawracanie: w raporcie nadal osobny defekt **69 zakleszczeń**; ten zestaw zmian nie oznacza automatycznie ich naprawy.
- Test telefonu z nagraniem/zdjęciami. Build/push bez testów nie jest dowodem jakości.

**Zasady:** tylko Unity 6, bez zmian w wyglądzie dróg / mapy / brandingu, bez nadpisywania źródłowych assetów i prac równoległych. Nie publikować ani nie scalać do wersji głównej bez potwierdzenia QA.
