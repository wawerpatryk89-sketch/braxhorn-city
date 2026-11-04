# Braxhorn City — wspólny standard aktualizacji stref

Decyzja właściciela: wspólny sposób szybkiego budowania i dostarczania poprawek dla **miasta, osiedla i obozu**, Unity 6. Zachować wygląd, jakość modeli, sterowanie i podział pracy. Ten dokument opisuje docelowy kontrakt; nie jest potwierdzeniem wdrożenia ani pomiaru przyspieszenia.

## Implementacja do wykorzystania przez wykonawców — 2026-11-04

Wspólne narzędzia i integracja osiedla: [kod `a9ca7986`](https://github.com/wawerpatryk89-sketch/braxhorn-city/tree/a9ca7986cb97560d16e466f8b78ef2a168f1e82b), [szczegółowa instrukcja CONTENT-UPDATES.md](https://github.com/wawerpatryk89-sketch/braxhorn-city/blob/a9ca7986cb97560d16e466f8b78ef2a168f1e82b/unity-tools/updates/CONTENT-UPDATES.md).

**Pierwszy sposób dostarczania jest prywatnym importem pliku, nie automatycznym CDN:** wykonawca publikuje `.braxupdate.zip` w draft release, właściciel pobiera go po zalogowaniu do GitHuba, a gra importuje przez systemowy wybór pliku. Bazowe paczki są w APK i działają bez sieci. SSO Netlify pozostaje włączone; token GitHuba nie trafia do gry. Wymagania HTTPS poniżej dotyczą przyszłego automatycznego pobierania.

Miasto i obóz: przenieść wyłącznie współdzielone pliki wskazane w instrukcji, podłączyć własne sceny i loader oraz własny workflow. Zachować cudze zmiany i trwające buildy. Kod jest punktem integracji, a nie potwierdzeniem, że mechanizm działa już w tych dwóch strefach.

Każda strefa wymaga **jednego nowego bazowego APK**, potem zgodne poprawki danych są wydawane przez `[content-update]` bez `BuildPlayer` i ponownego generowania świata. Historia `content-patch.json` jest trwała i rozszerzana na końcu; nowa baza APK nie może zgubić wcześniejszych poprawek. Zmiana C#/pakietów wymaga nowego APK. Przy każdym wydaniu podać `baseId`, rozmiar paczki, listę zmian oraz dowód testu na urządzeniu.

## Podział pracy — do przekazania wykonawcom

- **Miasto:** wykonawca miasta wdraża standard na swojej aktualnej gałęzi; zachowuje rozpoczęte naprawy awarii telefonu. Nie przejmuje kodu ani zasobów osiedla. Rejestruje aktualne sceny miasta, portu, wyspy i sektorów pod stałymi adresami. Zmiana lokalnej numeracji sektorów nie może zmieniać adresów pozostałych sektorów.
- **Obóz:** wykonawca obozu stosuje ten sam kontrakt. Pierwszy eksport zapisuje trwałe sceny/prefaby, ich `.meta` i rejestr strefy. Nie rozpoczyna integracji przez zastąpienie miasta lub osiedla własnym generatorem.
- **Osiedle:** wykonawca tej gałęzi odpowiada za integrację w osiedlu oraz wspólne narzędzia budowania. Zachowuje aktualne sceny głównej części i południa. Budowanie #61 jest osobnym, już uruchomionym wydaniem i nie otrzyma zmian w trakcie pracy.
- Wspólne skrypty aktualizacji mają jedną wersję i jednego właściciela zmiany. Przenosić uzgodniony commit wspólnego narzędzia, nie scalać całych gałęzi stref. Każdy wykonawca zapisuje SHA swojej bazy, zakres i wyniki testów. Nie restartować cudzego buildu.

## 1. Dwa niezależne cele

**Szybszy build:** ponowne wykorzystanie importu, kompilacji i przygotowanych scen. **Mniejsza aktualizacja telefonu:** aplikacja importuje zmienione paczki zawartości (docelowo może je pobierać). Sam podpis APK, rosnący versionCode albo cache Library nie zapewnia drugiego celu. Addressables sam nie naprawi generatora tworzącego od nowa cały świat.

## 2. Pamięć budowania

- Przypiąć dokładną wersję Unity, pakiety, platformę, ABI i profil grafiki. Oddzielić cache niezgodnych konfiguracji i stref.
- Zachowywać `Library`, w tym `Bee`, `PlayerDataCache`, `BuildPlayerData` i cache importu/shaderów, oraz `.meta` plików tworzonych w CI. Nie nadpisywać `.meta` zapisanych w źródłach. Nie usuwać Bee w celu oszczędzenia miejsca bez pomiaru kosztu.
- Używać stałych ścieżek projektu w CI. Raportować faktyczne przywrócenie oraz rozmiar i czas cache. Sprawdzać, czy zapis nie wypiera poprzedniego wpisu z limitu repo.
- Nie publikować prywatnych modeli, animacji, haseł ani rozpakowanej Library w publicznym repo. Cache zawierający prywatne źródła szyfrować; hasła tylko w sekretach CI.
- Zachowanie Library nie uzasadnia ignorowania zmian źródeł. Przy niezgodności cache wykonać poprawny build, a nie użyć starej sceny. Brak cache ma być widoczny w raporcie; zgodnie z dotychczasową decyzją właściciela sam brak cache nie blokuje APK.

## 3. Trwała mapa i generowanie fragmentów

- Zapisać wynik generatora jako trwałe sceny/prefaby/dane; zachować `.meta`, GUID i referencje. Binaria prywatnej mapy mogą być w zabezpieczonym artefakcie zamiast publicznego Git.
- Wydzielić stabilne sektory z identyfikatorami opartymi na współrzędnych/nazwach, nie na kolejności znalezienia obiektów. Zachować grupy LOD, pojazdy i powiązania komponentów.
- Każdy sektor ma jawne wejścia i zależności. Zmiana drzewa/domu przebudowuje zależne sektory, nie wszystkie. Zmiana wspólnego generatora/układu lub schematu danych może legalnie wymagać szerszej przebudowy.
- Nie wykluczać wszystkich plików runtime C# z klucza istniejącego generatora: wartości domyślne i metody runtime mogą wpływać na zapisane sceny. Najpierw oddzielić dane od kodu.

## 4. Paczki zawartości

- Unity Addressables: odrębne grupy/katalogi dla `miasto`, `osiedle`, `oboz` oraz jawne zależności wspólne. Nie pakować całej strefy do jednego ogromnego bundle. Pomiar duplikacji zależności obowiązkowy.
- Stabilne adresy, np. `osiedle/scenes/poludnie`, `miasto/scenes/port`, `oboz/scenes/wejscie`. Wspólne zasoby wersjonować jako kompatybilną zależność; nie nadpisywać ich bez kontroli wszystkich odbiorców.
- Pierwszy bazowy APK ma obsługiwać katalog, pobieranie, postęp, błędy sieci i lokalny cache. SceneManager sam nie odnajdzie scen przeniesionych poza APK: dostosować start, przejścia, obszary doczytywane i sprawdzanie dostępności scen. Zwalniać uchwyty Addressables i sceny; nie trzymać starej i nowej całej strefy jednocześnie.
- Aktualizację katalogu wykonywać przed załadowaniem zawartości, nie w trakcie korzystania z niej. Działająca lokalna wersja pozostaje dostępna przy braku sieci; pierwszy start bez pobranych danych ma pokazać czytelny błąd i ponowienie.
- Uzgodnić rzeczywisty HTTPS hosting paczek. GitHub **draft release nie jest publicznym CDN**. Nie umieszczać GitHub tokenu lub klucza podpisu w aplikacji. Wymogi dostępu do prywatnych zasobów obowiązują również dla CDN.

## 5. Dwa tryby wydania i zgodność

1. **Bazowy APK:** nowy kod aplikacji, podpis dotychczasowym kluczem, rosnący versionCode. Zapis SHA kodu, wersji Unity/pakietów, platformy, katalogu, paczek i `addressables_content_state.bin` dla tej bazy. Nic nie nadpisuje tej bazy.
2. **Content update:** tylko zgodne dane/sceny/modele. Użyć `Update a Previous Build` i pliku content state właściwej bazy/platformy. Porównać runtime C#, pakiety i schematy; zmiana kodu wymaga nowej bazy APK. Raportować zmienione paczki, sumy SHA-256 i łączny transfer wraz z zależnościami.

Najpierw wysłać wszystkie paczki i sprawdzić ich obecność/sumy, dopiero potem udostępnić katalog/manifest aktualizacji. Zachować wcześniejszy katalog i paczki do wycofania wydania. Aktualizacja nie może po cichu przełączyć telefonu na częściowo wysłaną zawartość.

## 6. Warunek odbioru — oddzielnie dla każdej strefy

- Zbudować i zainstalować bazę. Zmierzyć pełny i kolejny build: import, generowanie, kompilacja, pakowanie, wysyłka; rozmiar APK i paczek.
- Zmienić jeden element zapisany w danych. Zbudować content update bez nowego APK. Potwierdzić na tej samej instalacji zmianę oraz liczbę pobranych bajtów; niezmienione paczki mają identyczne hashe.
- Testy: brak sieci, przerwane pobieranie, uszkodzona/brakująca paczka, niezgodna baza, wycofanie aktualizacji, przejście między sektorami, ponowne uruchomienie i zachowanie ustawień/zapisu gry.
- Sprawdzić pamięć telefonu i brakujące skrypty/referencje. Udany build nie jest dowodem, że aplikacja uruchamia się na urządzeniu.
- Nie deklarować czasu „kilka minut” bez pomiaru. Nie zmniejszać jakości grafiki pod pozorem optymalizacji procesu wydawania.

## Oficjalne źródła

- https://docs.unity3d.com/6000.0/Manual/build-cache-location-reference.html
- https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/content-update-builds-overview.html

