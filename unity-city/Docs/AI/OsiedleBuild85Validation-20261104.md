# Osiedle: kontrola po awarii APK #85

Gałąź: `codex/osiedle-integracja-final-patryk-20261104`.
Baza: `05ce2815af22f4ff08f606177fbd9d615fdf3a82`.
Run: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41882388584
Job Android: `126759246946`.

## Potwierdzona przyczyna zatrzymania

Unity 6000.0.58f2 zgłosił CS0246 w `BraxClaudeOsiedleSalon.cs`, linie 878 i 880: nierozpoznany `InvalidOperationException`. Plik nie importował `System`. Poprawka kwalifikuje oba odwołania jako `System.InvalidOperationException`, bez zmiany walidacji fizycznego przejścia, geometrii ani wyglądu. Nie powstał APK; błąd jego publikacji był wtórny.

## Dodatkowe poprawki workflow

- Oddzielna grupa concurrency dla faktycznej gałęzi i rejonu zamiast wspólnej, wpisanej na stałe gałęzi innej integracji.
- Kopia gotowego APK w Actions artifact przed publikacją i opcjonalnymi backupami, bez kompresji, retencja 7 dni.
- Checkpoint, baza aktualizacji i pakowanie Library wymagają udanego zabezpieczenia i publikacji APK.
- Opcjonalny backup bazy aktualizacji i sprawdzenie checkpointu nie zmieniają udanej kompilacji w błąd całego przebiegu.
- Wyzwalanie pozostaje wyłącznie `workflow_dispatch`. Region osiedle daje wariant `osiedle`, application ID `com.braxhorn.osiedle`. Sposób podpisywania i numerowania APK pozostają bez zmian.

## Wykonane sprawdzenia

- Parser C# tree-sitter: 245 plików w `unity-city`, brak błędów składni. To nie jest kompilator ani dowód zgodności typów z Unity.
- Python AST: 64 skrypty w `unity-tools`, bez błędów składni.
- Odczyt 13 plików JSON projektu i 51 workflow YAML: bez błędów parsowania.
- `bash -n` dla wszystkich bloków shell workflow regionu, po zastąpieniu wyrażeń GitHub: PASS.
- 27 bezpośrednich odwołań workflow regionu do skryptów: wszystkie obecne w repozytorium.
- Przegląd niezdefiniowanych popularnych typów System w C#: jedyne znalezione przypadki to dwa naprawione odwołania. Nie stanowi pełnej analizy semantycznej.
- `python3 -m unittest discover -s unity-tools/updates -p 'test_*.py'`: 32/32 PASS. Komunikaty bad decrypt występują w oczekiwanych testach odrzucania błędnych kluczy.
- `python3 -m unittest discover -s unity-tools/claude -p 'test_*.py'`: 5/5 PASS.
- `python3 unity-tools/check_webgl_bridges.py`: PASS, pięć dostawców mostków.
- `python3 unity-tools/kickboxing/package.py --check-template`: PASS po pobraniu `public/unity-gym` do lokalnego sparse checkout. Pierwsza lokalna próba bez tego katalogu zgłosiła brak index.html; katalog jest w repozytorium i w checkout CI.
- Finalna kontrola składni zmienionego C# i workflow oraz `git diff --check`: PASS.

## Pozostałe granice dowodów

Lokalne środowisko nie ma Unity Editor. Nie wykonano nowej kompilacji Unity, generowania scen, importu prywatnych zasobów, kontroli kolizji w edytorze ani testu na Androidzie. Nie można zagwarantować, że kolejne etapy buildu przejdą na podstawie kontroli statycznej. Nie usunięto żadnej walidacji scen ani wymagań dotyczących zasobów. Wymagany jest nowy ręczny run na poprawionym commicie, region `osiedle`, następnie weryfikacja APK i uruchomienie na telefonie.
