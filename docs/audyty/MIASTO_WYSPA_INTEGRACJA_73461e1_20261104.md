# Miasto + wyspa — weryfikacja integracji 4.11.2026

Sprawdzone źródła: `73461e1e0e2b803b44fc5df1d61bff335601662c`, gałąź `codex/miasto-naprawy-filmow-20261104`.

## Zapisane zmiany

- Commit wyspy `bdebd84b184e82892bed3a58a121be6d67de574a` włączony w całości przez fast-forward z bazy `65ab55a9e831d289017f47c9875b193b41a26d1c`. Bez konfliktów i bez zmian gałęzi obozu.
- Commit `73461e1`: wyjątki wyposażenia i oznakowania miasta B trafiają do `ndBlokery`. `Krok` nie raportuje OK, jeśli jego podetapy dopisały blokery. Końcowa istniejąca kontrola przerywa odbiór niekompletnego miasta.
- CS0819 z #84 był już poprawiony w bazie przed integracją. Nie przypisujemy tej wcześniejszej poprawki nowemu commitowi.

## Wykonane kontrole

| Kontrola | Wynik |
|---|---|
| `python unity-tools/claude/check_city_cs.py` na scalonych źródłach | PASS, 275 plików C# |
| Parser tree-sitter C# wszystkich 275 plików | 0 błędów składni |
| Kontrola AST każdej deklaracji zmiennej, również wielowierszowej | 0 deklaracji wielu zmiennych z `var` |
| `python -m unittest discover -s unity-tools/wyspa/tests -v` | 5/5 PASS |
| `python unity-tools/wyspa/prepare_fighters.py --check` | KADE, ZYRO; 25 plików, 131155333 bajty źródeł |
| YAML workflow `claude-region-build.yml` | Poprawny YAML, jedyny trigger `workflow_dispatch` |
| Aktualna gałąź vs sprawdzony commit przed zapisaniem raportu | identical |

## Dostępność źródeł zawodników

Sprawdzono metadane istniejących roboczych wydań przez listę releases. Endpoint po tagu zwracał 404 dla draftów, ale lista ujawniła istniejące pliki; nie traktujemy tego 404 jako braku zasobów.

- Release ID 433413969, `oboz-private-34114c7950bc`:
  - `oboz_34114c7950bc_000.tar.gz.enc`: uploaded, 166941216 bajtów.
  - `oboz_34114c7950bc_002.tar.gz.enc`: uploaded, 167620112 bajtów.
- Release ID 433386677, `oboz-transfer-key-41749904696`:
  - `monika-casual-motion-20261104.cms.enc`: uploaded, 233465 bajtów.
  - `recipient.pem.enc`: uploaded, 2512 bajtów.

Rozmiary wymaganych paczek odpowiadają manifestom. Nie pobierano ani nie odszyfrowywano prywatnych modeli w tej sesji. Weryfikacja SHA-256 bajtów oraz odszyfrowanie pozostają zadaniem przygotowania zasobów w CI; metadane nie są ich dowodem.

## Otwarte ograniczenia

Nie wykonano kompilacji C#, importu Unity, kompilacji shaderów, generowania scen, testów kolizji, APK ani testu Android. Parser i preflight nie zastępują tych kontroli. Nie potwierdzono usunięcia wszystkich usterek runtime ani wizualnej jakości wyspy.

Właściciel polecił naprawę i uruchomienie oraz dołączenie wyspy. W dostępnej integracji GitHub brak akcji `workflow_dispatch`; lokalnie brak Unity, gh i skonfigurowanej tożsamości/tokena GitHub do dispatch. Dostępne rerun starego #84 zbudowałoby jego stary SHA, bez połączonych poprawek. Nie uruchomiono starego kodu i nie zmieniono triggerów na push.

Wymagany następny krok: ręczny `workflow_dispatch` workflow `claude-region-build.yml`, gałąź `codex/miasto-naprawy-filmow-20261104`, `rejon=miasto`, `pamiec=true`. Zachować ograniczenie wariantu miastogl i APK FIRST. Nowe błędy z Unity zebrać przed kolejną poprawką; nie ogłaszać gotowości gry na podstawie tego raportu.
