# BRAXHORN CITY — APK FIRST / DOSTAWA GOTOWEGO WYNIKU

**Decyzja właściciela (4.11.2026), P0. Obowiązuje wszystkich wykonawców:** Claude, Codex, ChatGPT Work, GPT i przyszłych. Wszystkie rejony: **miasto, osiedle, obóz, wyspa, Miasto Mistrzów**. Centralna [zasada #13](https://github.com/wawerpatryk89-sketch/braxhorn-city/issues/13) jest obowiązkowa.

## Gwarancja dostawy
1. **Najpierw wynik, potem dodatki.** Po poprawnej kompilacji oraz walidacji podpisu, applicationId i versionCode natychmiast zabezpiecz wygenerowany APK. Zachowaj go niezależnie jako GitHub Actions artifact (30 dni; bez kompresji) i jako osobny asset testowego GitHub Release, jeśli uprawnienia to umożliwiają. Weryfikuj obecność w GitHub API, SHA-256 i rozmiar.
2. **Nie usuwaj jedynej kopii.** Pliki na runnerze wolno czyścić dopiero po potwierdzeniu zewnętrznego zapisu. Stosuj unikalną nazwę zawierającą rejon, run ID, attempt, krótki SHA. Nie nadpisuj wcześniej dostarczonej wersji.
3. **Cache / Library / content baseline / snapshot / backup / media są pomocnicze.** Mogą działać dopiero po zabezpieczeniu APK. Ich błędy mają być zgłoszone jako ostrzeżenia, nie wolno im unieważnić już zbudowanego i dostarczonego APK. Dla kroków pomocniczych stosuj continue-on-error lub osobne joby. Kompilacja, podpis, walidacja i potwierdzenie realnej dostawy pozostają krytyczne.
4. **Jeśli jeden upload zawiedzie, drugi pozostaje ratunkiem.** Gdy nie uda się zapisać ani Actions artifact, ani wydania testowego, jawnie zgłoś błąd dostawy. Samo utworzenie APK na znikającym runnerze nie oznacza dostarczenia.
5. **Raport:** tylko najnowszy build z aktualnym kompletem poprawek; nr run, SHA, rejon, czas, applicationId/versionCode, rozmiar, suma SHA-256, aktywny bezpośredni link do APK. Cache i baseline to osobne statusy. Nie podawaj starych buildów jako aktualnych.
6. **Miasto Mistrzów:** bieżący workflow przygotowuje zaszyfrowaną scenę i Android bundles, a nie gotowy APK. Te paczki także zapisz natychmiast po przygotowaniu; kolejne raporty nie mogą usuwać wyniku. Po dodaniu osobnego APK zasada APK FIRST obowiązuje bez wyjątku.
7. **Zero nowych buildów bez odrębnej zgody właściciela** — sam zapis zasad, commit i push nie autoryzują builda ani ponowień.

## Wzorzec w GitHub Actions
- Po buildzie i walidacji: kontrola plików i sha256sum.
- Niezwłocznie: actions/upload-artifact, unikalna nazwa, if-no-files-found: error, retention-days: 30, compression-level: 0.
- Następnie: niezmienne wydanie testowe, potwierdzenie assetów przez gh release view, bez nadpisywania starszych wersji.
- Dopiero potem: backup, Library, baseline i media (continue-on-error: true i jawne ostrzeżenia).
- Wyświetl działający link do pliku. Nie udawaj sukcesu bez weryfikacji.

**Przyczyna zasady:** w osiedlu #77 (run 41830501748) skompilowane i przesłane APK braxhorn-osiedle-4752f69.apk zostało zachowane na GitHub Release, ale późniejsze Preserve immutable encrypted content baseline after successful APK oznaczyło całość jako failure. To nie może się powtórzyć jako utrata dostępności wyniku.

**Uwaga:** sam dokument nie oznacza, że wszystkie historyczne workflow są już technicznie poprawione. Każdy wykonawca ma obowiązek zaimplementować kontrolę w obsługiwanym przez siebie workflow przed kolejnym zatwierdzonym buildem.
