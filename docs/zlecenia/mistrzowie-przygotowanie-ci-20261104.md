# Miasto Mistrzów: przygotowanie CI według działających buildów Claude’a

## Dowody porównania

- Miasto #58: run 41686981679, job 126095635690, commit 874f194624e4dfead5637a4e3dfd1d3285a9cbe1.
- Osiedle #61: run 41740946642, job 126282298624, commit 5b9e7afb2d487ff0b5c92d79f90fffb97f70f99c.
- Statusy kroków potwierdzają powodzenie kompilacji i wysyłki. Odczytano workflow i kod buildera z #61. Pobranie pełnych dużych logów nie powiodło się; nie stanowią podstawy tego raportu.
- Miasto Mistrzów #3, run 41898644227: walidator odrzucił nazwę gałęzi wpisaną w pole SHA. Unity nie zostało uruchomione. Wcześniejszy run 41889411877 zatrzymał się na dodawaniu sceny przy niezapisanej pustej scenie; poprawka znajduje się już w historii tej gałęzi.

## Przygotowane zmiany

1. Przed pobieraniem i generowaniem zasobów: zwolnienie zbędnych narzędzi runnera, 12 GiB swap, limit joba 355 minut jak w porównanym workflow. Zachowany hostowy Android SDK wymagany do kontroli APK.
2. Oddzielny szyfrowany cache Library i metadanych źródeł dla Miasta Mistrzów, Unity 6000.0.58f2 i Androida. Bez Bee oraz Il2cppBuildCache. Istniejące GUID-y mają pierwszeństwo. Uszkodzony cache nie zmienia projektu, a brak cache pozwala na pełny import.
3. Sceny generowane nie są odtwarzane z cache: generatory Mistrzów wymagają własnego świeżego wyniku. Nie podmieniamy buildera Mistrzów builderem miasta/osiedla.
4. Znacznik `.gotowe` zapisywany dopiero po sukcesie BuildPlayer. Kontrola i wysyłka APK mogą działać po późniejszym błędzie Unity. Nie ukrywamy błędu joba ani nie pomijamy kontroli identyfikatora, wersji i certyfikatu.
5. Cache zapisuje się dopiero po potwierdzeniu odbioru APK. Błąd opcjonalnego cache lub raportu nie psuje udanej dostawy.
6. Formularz przyjmuje puste pole, pełne SHA lub dokładną nazwę gałęzi codex/miasto-mistrzow-20261104, z usuwaniem otaczających spacji. Wybrana gałąź Mistrzów jest przypięta do SHA zdarzenia.
7. Workflow uruchamia wszystkie testy Python przez unittest discover.

## Walidacja lokalna

- 34 testy Python: PASS, w tym cztery rzeczywiste testy szyfrowania i odtwarzania cache przez openssl/zstd/tar.
- Dwa pliki YAML poprawnie odczytane; 23 kroki shell sprawdzone przez bash -n.
- Sześć przypadków wyboru źródła sprawdzonych przez wykonanie rzeczywistego skryptu formularza: puste pole na gałęzi, nazwa gałęzi, otaczające spacje, puste pole na main, SHA wielkimi literami, odrzucenie błędnej wartości.
- Brak Unity w środowisku lokalnym: nie przeprowadzono kompilacji C#, importu Unity, budowy APK ani testu na urządzeniu. Nie deklarujemy, że cały projekt jest bez błędów lub że następny build na pewno przejdzie.
- Nie zmieniano wyglądu, geometrii, materiałów ani sterowania. To przygotowanie CI, nie zamknięcie wszystkich uwag poprzedniego audytu kodu.

## Uruchomienie

Po osobnej zgodzie na build: Actions → Miasto Mistrzów — APK → Run workflow → gałąź codex/miasto-mistrzow-20261104 → pole commit puste. Workflow tylko ręczny; ta zmiana nie uruchamia kompilacji.
