# Obóz — pierwszy APK do testu

Stan: dostawa źródeł zweryfikowana; zlecono pierwszy build, wynik jeszcze nieznany. To wydanie
do oceny sceny i ruchu na telefonie, nie baza do aktualizacji Addressables.

## Zakres

- Unity `6000.0.58f2`, Android ARM64, IL2CPP, Built-in, OpenGLES3, ASTC, Linear.
- `BraxObozBuild.BuildAndroidReview`: scena startowa i obóz z Moniką oraz
  sześcioma NPC. Własna lista scen, bez wywoływania generatorów innych stref.
- Stałe `com.braxhorn.oboz`, alias podpisu `braxhorn`, rosnący `versionCode`.
  Brak podpisu zatrzymuje build; nie ma zastępczego podpisu debug.
- Kontrola struktury sceny, brakujących skryptów i źródeł przed BuildPlayer.
  Niepotwierdzone wzrosty NPC i odchylenia Misia pozostają widoczne w raporcie;
  nie oznacza to zgody na jakość ani gotowości całej gry.
- Wspólna zaszyfrowana Library z Bee, oddzielny profil obozu. Brak cache
  dopuszcza pełny import, zgodnie ze standardem. Czasów nie zmierzono.
- Szyfrowany zapis wygenerowanych scen i prefabów z `.meta`. Przy niezgodnych
  wejściach generator przebudowuje scenę, zachowując istniejące GUID. Podział
  na niezależnie generowane sektory pozostaje niewdrożony.

## Zweryfikowana dostawa przed pierwszym uruchomieniem

`private-inputs.json` opisuje **204 pliki, 1 148 328 099 bajtów źródeł**.
To nie jest rozmiar APK, zaszyfrowanego archiwum ani RAM telefonu.
Spis pomija nieużywane kopie GLB i pliki tymczasowe po kopiowaniu. Zachowuje
oryginalne bajty wszystkich wybranych modeli, map i animacji.

Dostawa została zakończona i zweryfikowana w Actions (run `41755149298`).
Wszystkie 204 źródła zachowały oryginalne bajty. Siedem paczek jest dostępnych
w niepublicznym szkicu `oboz-private-34114c7950bc`; kontrolne pobranie oraz
SHA-256 przeszły. `private-inputs.json` pochodzi z artefaktu `oboz-delivery-proof`.
Hasło podpisu nie opuszcza CI. `build-request.json` zleca pierwszy testowy APK;
wynik kompilacji Unity i działanie na telefonie nadal wymagają sprawdzenia.
Istniejące sekrety CI mają nazwy `BRAXHORN_KEYSTORE_PASS`,
`BRAXHORN_KEYSTORE_B64`, `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`.
Ich dostępność dla tego joba zostanie sprawdzona przed uruchomieniem Unity.

## Dostawa prywatnych zasobów

W środowisku z przygotowanymi źródłami i bezpiecznym powiązaniem `KLUCZ`
z dotychczasowym hasłem podpisu, z katalogu głównego repo:

```sh
python3 unity-tools/oboz/assets.py verify
python3 unity-tools/oboz/assets.py encrypt
```

Szyfrowanie wywołuje niezmienione `unity-tools/claude/prywatne.py szyfruj`.
Każda paczka ma nagłówek `Salted__`, SHA-256 i limit 200 MiB. Nie nadpisuje
wcześniejszych paczek. Manifest dostaje dokładne nazwy, sumy i `releaseTag`.
Wysyłający musi utworzyć **draft release** o tym tagu, wysłać wszystkie
wymienione pliki `.enc`, pobrać je kontrolnie i sprawdzić SHA-256. Dopiero
potem zapisać zaktualizowany manifest na `gpt/oboz`. Surowe źródła nie trafiają
do publicznego Git ani do publicznego wydania.

CI pobiera paczki z wydania, sprawdza sumy szyfrogramów, odszyfrowuje do katalogu
tymczasowego, sprawdza każdy plik i dopiero potem instaluje kompletny zestaw.
Brak, zła suma, dowiązanie lub obcy plik zatrzymują import. Metadane Unity
zachowuje cache i checkpoint; dostawa źródeł nie nadpisuje `.meta`.

## Uruchomienie na własnej gałęzi

Workflow: `.github/workflows/oboz-unity-build.yml`. Zwykły push kodu nie
uruchamia tego buildu. Po sprawdzeniu całej dostawy utwórz/zmień
`unity-tools/oboz/build-request.json` i wyślij go na **`gpt/oboz`**:

```json
{
  "firstInstall": true,
  "privateInputsSha256": "SHA-256 aktualnego pliku private-inputs.json",
  "request": "unikalny opis tego zlecenia"
}
```

Plik wyzwalający istnieje po zweryfikowanej dostawie `41755149298`. `firstInstall`
wolno ustawić na `true` tylko dla pierwszej instalacji obozu. Gdy istnieje
poprzednia tożsamość APK, zawsze sprawdzany jest ten sam certyfikat i wyższy
numer wersji, niezależnie od tego pola. Build może też przyjąć
`workflow_dispatch`, jeżeli workflow zostanie zarejestrowany na domyślnej gałęzi.
Nie wymaga to zmiany `main`: wyzwalanie plikiem działa na gałęzi obozu.

## Wynik i dowody

APK trafia do osobnego, niezmiennego draft release `oboz-review-<versionCode>`.
`release-identity.json` zawiera sprawdzony przez `aapt`/`apksigner` identyfikator,
wersję, certyfikat i SHA-256 APK. Wydanie przechowuje też raport sceny, raport
buildu i zaszyfrowany checkpoint, jeśli jego zapis się udał. Dopiero po wysłaniu
wydania aktualizowany jest wskaźnik metadanych `oboz-review-latest`.

Brak opcjonalnego cache lub checkpointu nie usuwa APK. Zachowywane są
dotychczasowe wydania. Nie wykonujemy zrzutów ekranu w CI.

Weryfikacja lokalna: 73 testy Pythona (26 narzędzi źródeł, 22 dostawy/podpisu,
25 niezmienionych wspólnych narzędzi); składnia workflow/Bash/Python i parser
20 plików C#. Testy szyfrowania używają sztucznego hasła wyłącznie do fikstur.
To nie jest kompilacja w Unity. Nie ma jeszcze APK, testu telefonu ani
potwierdzonej aktualizacji zawartości bez nowego APK.
