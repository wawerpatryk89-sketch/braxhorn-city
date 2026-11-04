# Osiedle: schody, drogi, klub i kierowca — 4 listopada 2026

## Wersja i granica potwierdzenia

- Rejon: wyłącznie **osiedle**, Unity **6000.0.58f2**, glTFast **6.20.0**.
- Baza wskazana przez właściciela: `codex/osiedle-integracja-final-patryk-20261104`, pełny SHA `87fcd04830e3786cb219575cc892610678ca6c8b`.
- Gałąź zmian: `codex/osiedle-drogi-klub-kierowca-20261104`.
- Status: zmiany źródeł i lokalne kontrole statyczne. **Nie uruchomiono Unity, Play Mode, kompilacji APK ani testu telefonu.** W środowisku wykonawcy nie ma edytora Unity ani połączenia z nim.
- Zdjęcia/filmy właściciela służą do diagnozy starej wersji; nie są dowodem działania tej zmiany. Nie wygenerowano zastępczych „zdjęć z gry”. Nie ustalono SHA APK widocznego na tych nagraniach.

## Diagnoza i zmiany

| Obszar | Znaleziony problem | Zmiana w źródłach |
| --- | --- | --- |
| Wsiadanie | `EnteringCar` był w katalogu, lecz sterownik przechodził bezpośrednio do `CarIdle` | Otwieranie rzeczywistych drzwi, odtworzenie całego klipu właściciela według jego długości, dopasowanie końcowej pozycji bioder, przejście do fotela, zamknięcie drzwi |
| Dłonie | Stała poza ramion nie trafiała w kierownicę konkretnego modelu | Pomiar obręczy z geometrii, także wieloczęściowej; punkty obu chwytów, IK dwóch kości przez same obroty, ustawienie dłoni i palców, kontrola zasięgu przy doborze fotela |
| Patryk | Ruchy mogły wrócić do wcześniejszych zamienników; błędy długości kości kończyły się ostrzeżeniem | Chód `Strut Walking.fbx`, bieg `Fast Run.fbx`, skok `Jump.fbx` z paczki właściciela są wymagane; `EnteringCar` też wymagany. Błąd długości kości blokuje podmianę. Zachowane kontrole nóg na zapisanych klipach |
| Salon aut | Poprzednia poprawka zmieniała test kapsuły, nie schody | Oba biegi i krawędź antresoli cofnięte o **1,3 m**. Przesunięte auta na antresoli. Kontrola wolnego przedsionka przed oboma biegami i dłuższego korytarza w głąb salonu |
| Ulice | Stare latarnie/krawężniki blokowały pomiar miejsca jeszcze przed ich usunięciem | Pomiar względem trwałych działek/budynków i nowej sieci ulic. B2 może przesunąć oś; rondo i końce S1/S2 podążają za nią. Zaokrąglenia narożników nie są blokowane przez stare wyposażenie |
| Rondo | Za mało szerokości jezdni | Promień zewnętrzny jezdni **13,3 m**, opaski **5,2 m**, wyspy **3,6 m**; pierścień jezdni **8,1 m** zamiast **5,9 m**. Zaktualizowane trasy aut, próg włączania do ruchu i wycięcie starej nawierzchni po okręgu |
| Wyposażenie ulic | Hydranty miały współrzędne odpowiadające dawnym szerokościom | Pozycje wyznaczane z aktualnej osi i krawędzi jezdni |
| Kolizje auta gracza | Maska pomijała warstwę 8; kolejna próba poślizgu zaczynała się ponownie od starego położenia | Włączenie warstwy 8 z filtrowaniem własnych obiektów, sprawdzanie pośrednich obrotów nadwozia, przesunięcie do kontaktu i kolejny rzut z nowego miejsca |
| Ruch AI | Symulacja nie otrzymywała ogólnej geometrii przeszkód sceny | Adapter kolizji dla statycznych przeszkód, zaparkowanych aut i ludzi, oprócz istniejących obrysów aut/busów. Kontrola ruchu w odcinkach nie dłuższych niż 15 cm |
| Loże | Usunięto 42 sofy, zostawiono stoliki; niespójne stare liczniki, w tym niezdefiniowane `lozeN` | **14 kompletnych modeli `model.glb` właściciela**, ze stolikami zawartymi w modelu. Oryginalna geometria, jednolita skala, kolizje poszczególnych siatek i dwa miejsca na tylnej ławie. Zachowane siedzenia baru |
| Klub | Zbyt jasno, brak czytelnej świecącej kuli | Lokalna warstwa oświetlenia, wyłączenie słońca dla wnętrza, ciemniejszy ambient także w używanym shaderze CityStaticPBR; zachowane mapy materiałów. **4 lasery**, ograniczenie wiązek do pomieszczenia, emisja na oryginalnym modelu kuli i dodatkowe światło |
| Import zasobów | Nowe GLB wymagały dostawy do CI; import animacji osiedla uruchamiał się również dla miasta | Szyfrowana paczka 3 oryginalnych GLB i importer z kontrolą integralności. Import modelu, klubu i animacji osiedla tylko w gałęzi wykonania `osiedle` |
| Kontrola wyniku | `Guard` mógł jedynie zapisać błąd brakującej funkcji | Końcowa zbiorcza kontrola wymaganych zmian zapisuje `city-review/osiedle-zlecenie-20261104.txt` i przerywa generowanie, jeśli wymagana funkcja nie przejdzie kontroli |
| Ręczny workflow | Nowa nazwa gałęzi nie była dopuszczona w warunkach jobów | Dopuszczenie tej gałęzi z wejściem `rejon=osiedle`. Wyzwalacz pozostaje wyłącznie ręczny; kroki budowania, podpis, cache i dostawa APK bez zmian |

Patryk **nie został zmniejszony**: docelowy wzrost pozostaje 2,33 jednostki świata (1,80 × skala osiedla). IK nie zmienia długości kości. To są właściwości kodu, nie wizualne potwierdzenie braku deformacji po retargetingu.

Docelowa szerokość zwykłej jezdni to 2 × 3,5 m × skala osiedla (około 9,06 jednostki), ograniczona przez trwałe obrysy. Nowa kontrola wymaga co najmniej 8 m na każdej z ośmiu ulic A i 8 m na pierścieniu ronda. **Ostateczne szerokości każdej ulicy musi potwierdzić raport wygenerowanej sceny**; nie przedstawiamy celu jako wyniku pomiaru Unity.

## Oryginalne modele klubu

Manifest `unity-city/Prywatne/import-club-20261104/manifest.json` zapisuje nazwy źródłowe i SHA-256. Paczka zawiera dokładne bajty wszystkich trzech dostarczonych GLB; do układu klubu wybrany jest `model.glb` jako `raised-seating-booth.glb`. Pozostałe dwa modele są dostarczone do importu, ale nie są ustawione w scenie.

Wybrany model ma 7 siatek i 3024 trójkąty, oryginalny obrys około 2,410 × 1,270 × 1,526 m. Nie zawiera tekstur: nie dopisano mu fikcyjnego statusu materiałów 4K ani nie zmieniono jego wyglądu przez tworzenie zamiennika. Nowa paczka przechowywana jest zaszyfrowana. Import zachowuje istniejące pliki `.meta`; nie wymaga nowego sekretu CI. Nie odczytano produkcyjnego klucza prywatnego podczas przygotowania paczki.

Kontrola źródłowej geometrii aut potwierdziła wieloczęściowe kierownice: Revuelto 12 części, 918 2 części, R8 6 części; po przeliczeniu do długości samochodu szerokości obręczy wynoszą odpowiednio około 0,363 / 0,372 / 0,356 m. Nazwane komplety zawierają również poduszkę i szprychy, dlatego próg udziału obręczy jest niższy niż dla anonimowych części; nadal wymagane są pokrycie co najmniej 7/8 obwodu i ograniczenia kształtu. To kontrola źródeł GLB, nie test importera ani chwytu w Unity. Finalny model auta musi jednocześnie mieć drzwi kierowcy, rozpoznaną obręcz, zasięg rąk i miejsce na głowę. Nie ma cichego zastąpienia brakującej kierownicy punktem na desce rozdzielczej.

## Wykonane kontrole

```sh
python3 -m unittest discover -s unity-tools/claude -p 'test_*.py' -v
python3 -m unittest discover -s unity-tools/validation -p 'test_*.py' -v
python3 unity-tools/validation/check_csharp_scopes.py unity-city/Assets
git diff --check
```

- **17/17 testów importu:** 6 nowej paczki klubu, 6 animacji właściciela, 5 transferu Patryka. Sprawdzają między innymi pełne szyfrowanie/odszyfrowanie na kluczach testowych, integralność, odrzucenie uszkodzeń, bezpieczeństwo ścieżek, zachowanie GUID i izolację rejonów. Nie dowodzą dostępności produkcyjnego sekretu GitHub Actions.
- **14/14 testów walidatora C#** na wersjach przypiętych w `unity-tools/validation/requirements.txt`.
- **250 plików C#: 0 błędów parsera i kontrolowanych konfliktów zakresów.** To nie kompilacja: narzędzie nie rozwiązuje typów ani API Unity.
- YAML workflow poprawny. Zachowany wyłącznie `workflow_dispatch`; ta gałąź dopuszczona tylko dla osiedla. Żaden sprawdzony wyzwalacz `push` w repo nie pasuje do nowej gałęzi. Nie zmieniono kroków builda.
- `git diff --check`: bez błędów.

Dodano regresje Unity dla dokładności IK bez rozciągania kości oraz zatrzymania przed cienką przeszkodą i wznowienia ruchu. **Nie zostały tutaj wykonane**. Generator wykona je wraz z kontrolą sceny po osobno zleconym uruchomieniu Unity.

## Co nadal wymaga wykonania w Unity / na telefonie

1. Import prywatnych zasobów z produkcyjnym sekretem, kompilacja całego projektu Unity i przejście końcowych kontroli generatora, bez pomijania błędów.
2. Chód, bieg i skok Patryka w zbliżeniu z przodu/boku/tyłu: kolana, stopy i skóra. Sam test długości kości tego nie zastępuje.
3. Pełne wsiadanie i wysiadanie, zasięg i orientacja dłoni, łokcie, palce, biodra/głowa w kabinie. Pomiar chwytu przy generowaniu dotyczy pozy statycznej; nie dowodzi całej sekwencji w ruchu.
4. Wejście do salonu i sali bokserskiej, przejście przed schodami i wejście na antresolę rzeczywistym kontrolerem Patryka.
5. Przejazd małymi ulicami i rondem, mijanie autobusów, hamowanie AI przed przeszkodą i graczem, uderzenia/skosy/cofanie przy słupkach i ścianach. Zmierzyć koszt nowych zapytań fizyki na telefonie.
6. Oświetlenie klubu w dzień/noc i z różnych kamer, świecenie kuli, wszystkie wachlarze laserów, wejście/wyjście gracza, kolizje i siadanie w lożach.
7. Przegląd pozostałego rozmieszczenia i migotania w wygenerowanej scenie; istniejące audyty projektu nadal są uruchamiane. Nie ma dowodu usunięcia wszystkich błędów całego osiedla.

## Ręczny start po decyzji właściciela

[Workflow regionu](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/workflows/claude-region-build.yml): `Use workflow from` = gałąź poniżej, `rejon` = `osiedle`, `pamiec` = `true`. Ten formularz nie ma pola `SOURCE_COMMIT`; przed uruchomieniem sprawdzić HEAD gałęzi względem SHA przekazanego przy odbiorze kodu. Wyłączony job WebGL pozostaje wyłączony, wariant aktywny to Android APK. Zapis tej instrukcji nie uruchamia workflow.

```text
codex/osiedle-drogi-klub-kierowca-20261104
```

Zmiany źródeł i nowe odszyfrowane pliki wchodzą do istniejącego klucza generowania świata; `pamiec=true` nie oznacza użycia starej sceny mimo nowych źródeł. Nie jest też obietnicą krótkiego buildu ani małej aktualizacji telefonu.
