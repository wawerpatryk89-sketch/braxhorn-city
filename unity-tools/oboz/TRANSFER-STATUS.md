# Obóz — odblokowanie dostawy, 4 listopada 2026

Kod i źródła obozu istniały. Brakowało możliwości zaszyfrowania zasobów bez
lokalnego dostępu do hasła podpisu. Nowa ścieżka używa publicznego klucza
odbiorcy; klucz prywatny jest chroniony istniejącym sekretem wyłącznie w CI.

## Potwierdzone

- [Przygotowanie klucza w Actions](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41749904696): sukces.
- 204 oryginalne źródła, 1 148 328 099 bajtów, zgodne z `private-inputs.json`.
- 7 paczek CMS AES-256-GCM / RSA-OAEP-SHA256, 1 077 537 308 bajtów.
- Paczki zapisane w Bibliotece właściciela pod nazwami z tabeli. Wszystkie
  odczytano ponownie i sprawdzono SHA-256. Nie publikowano surowych modeli.
- 31 testów Python, w tym pełne przejście testowych bajtów przez szyfrowanie
  publicznym kluczem, odczyt, dotychczasowe szyfrowanie CI i odczyt po wysyłce.
  Test przesyłania korzysta z lokalnej atrapy GitHuba; rzeczywisty odbiór CI
  nie był jeszcze uruchomiony.

## Dokładny pozostały krok

Wysłać wszystkie siedem plików `.cms.enc` do istniejącego **draft release**
`oboz-transfer-key-41749904696`, bez nadpisywania trzech plików klucza.
Są to szyfrogramy; samo ich pobranie nie ujawnia modeli. Hasło APK ani prywatny
klucz transportowy nie opuszczają CI w postaci jawnej.

Po porównaniu nazw i rozmiarów w wydaniu skopiować `transfer-ready.json` na
`transfer-request.json` i zapisać commit na `gpt/oboz`. To uruchamia sam odbiór:
sprawdzenie sum, odszyfrowanie, kontrolę wszystkich źródeł, ponowne szyfrowanie
istniejącym narzędziem i zapis do osobnego draft release `oboz-private-*`.
Dopiero pomyślny artefakt `oboz-delivery-proof` pozwala zastąpić
`private-inputs.json` jego sprawdzoną wersją i utworzyć `build-request.json`
z `firstInstall=true` oraz SHA-256 tego manifestu. To uruchomi właściwy build.
Nie zmieniać wspólnych narzędzi, wyglądu, scen miasta ani osiedla.

## Obecna blokada

Dostawa binarna do GitHuba nie została zakończona. Zapis kodu działa przez
połączone konto, lokalny Git nie ma uwierzytelnienia do push. Mechanizm Biblioteki
zwrócił lokalne kopie, bez adresów pobrania dla Actions. Próba otwarcia GitHuba
w przeglądarce została odrzucona przez kontrolę uprawnień z komunikatem o braku
zgody. Nie obchodzono tej blokady. `build-request.json` nadal nie istnieje;
Unity i APK obozu nie zostały uruchomione. Nie ma testu telefonu ani działających
aktualizacji Addressables.

## Paczki do wysłania

| Plik | Bajty | SHA-256 |
| --- | ---: | --- |
| camp-transfer-000.cms.enc | 167140803 | `3dcd271896353a710e2bf4d8763c549592a6f8009c75a2864b9340b9bf3d6f15` |
| camp-transfer-001.cms.enc | 180950976 | `46173832b0f7c23e15341f5264679f65c0d4958e3c069f62dfbdf5ccec2a10e8` |
| camp-transfer-002.cms.enc | 167817805 | `ed31e541a4ae5e2ac893d5ad1acca60f1d4340dc25eb368c53d86616c0ef2e29` |
| camp-transfer-003.cms.enc | 162371365 | `24cf6e2be6ae94666c61440f7b87ebbbc633974631f8684b2819d369341098bc` |
| camp-transfer-004.cms.enc | 171303347 | `83fbad0993af2b31354a639dd79be02e24c1d6b3458ad06ae35323d34bc7cf93` |
| camp-transfer-005.cms.enc | 157130693 | `e22f20edc16a57ff7b0a2e36a941d4940e88e743b24ea0791152e74f8ae6739c` |
| camp-transfer-006.cms.enc | 70822319 | `19c73a6609ab403d6598d5427a2a8c8f0f2ef23628f24c88ef2e749773ee80ab` |
