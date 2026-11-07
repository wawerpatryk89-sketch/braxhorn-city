# BRAXHORN CITY — JEDYNA GLOBALNA INSTRUKCJA (7.11.2026)
**NAJPIERW PRZECZYTAJ TEN PLIK.** Obowiązuje we wszystkich czterech gałęziach rejonów. Nowa część 0 doprecyzowuje reguły z 6.11.2026; wcześniejsze, sprzeczne kopie zasad są nieaktualne. Wpisuj aktualizację instrukcji jako osobny commit zawierający wyłącznie `AGENTS.md` na każdej gałęzi; nigdy na `main`.

0. GLOBALNY STANDARD DUŻEJ GRY — UNITY 6 / ANDROID (najwyższy priorytet techniczny)
- Wzorzec: mały uruchamialny Player + osobne, wersjonowane pakiety treści, nie pełna przebudowa 1 GB+ APK przy każdej poprawce. To zasada dla wszystkich rejonów, a NIE twierdzenie, że obecny projekt już tak działa.
- Samo rozdzielenie scen *wewnątrz tego samego APK* może poprawić obciążenie pamięci podczas ładowania, ale NIE zmniejsza automatycznie pobieranej paczki. Mniejszy instalator wymaga faktycznego wyprowadzenia zasobów do osobnych AssetBundles/Addressables lub pakietów dystrybucyjnych.
- Projektuj bootstrap: lekkie menu, sterowanie, profil grafiki, sprawdzanie plików, komunikaty o postępie i błędach. Świat ładowany później na żądanie, z możliwością wejścia do gry, wyjścia i ponownego uruchomienia. Nie wolno pokazać `SUCCESS` zanim świat działa na urządzeniu.
- Dostarczanie dla APK spoza Google Play: osobne wersjonowane pakiety Addressables/AssetBundles, źródło pobierania zatwierdzone przez właściciela, offline fallback/instalacja lokalna, bez wbudowanych sekretów GitHuba. GitHub Actions artifact i prywatny DRAFT Release nie są automatycznie publicznym CDN-em.
- Dla przyszłej publikacji w Google Play osobno rozważ Android App Bundle + Play Asset Delivery (`install-time`, `fast-follow`, `on-demand`). PAD nie działa samoczynnie w samodzielnym APK z GitHuba.
- Rozdziel pakiety: wspólna biblioteka postaci/animacji/materialów, Miasto R1, Osiedle, Obóz, Miasto Mistrzów, opcjonalne wnętrza, roślinność oraz HD. Podział logiczny nie upoważnia do przenoszenia assetów między gałęziami lub do zmiany układu świata bez zgody.
- Wykorzystuj Addressables Analyze i Build Layout / BuildReport do wykrywania zduplikowanych siatek/tekstur/zależności: wspólne zasoby umieszczaj w wydzielonej grupie, nie powielaj w Player i w pakietach. Każdy pakiet ma stabilne ID, katalog, SHA-256 i raport rzeczywiście pobranych bajtów.
- Aktualizacja różnicowa WYŁĄCZNIE po zatwierdzeniu działającej bazy: przechowuj katalog bazowy, stabilne `.meta`/GUID i odpowiedni `addressables_content_state.bin`; nową zawartość publikuj bez ponownego instalowania kodu, jeśli to kompatybilne. Zmiana kodu C#/IL2CPP, konfiguracji Playera lub binarium wymaga nowego APK.
- Bezpieczny klient aktualizacji: pobierz do staging, sprawdź rozmiar/hash/zgodność, dopiero potem atomowo aktywuj. Po błędzie zachowaj ostatni działający pakiet; żadnych automatycznych kasowań cudzych assetów ani cache.
- Duży świat: addytywne/asynchroniczne ładowanie stref i wnętrz; bezpieczne zwalnianie referencji Addressables przy wyjściu; PBR, oryginalne geometrie/tekstury, zasięg cieni i LOD zależne od odległości. Nie usuwaj/nie odchudzaj assetów lub jakości wizualnej bez porównania ze wzorcem i zgody właściciela.
- Mipmap streaming ogranicza RAM/VRAM, ale sam NIE pobiera zasobów z serwera. Ustawienia pamięci to budżet, nie gwarancja; tekstury `non-streaming` mogą go przekroczyć. Mierz faktyczne maksymalne zużycie pamięci i ryzyko natywnego crasha Androida.
- Kompresję AssetBundles (LZ4/LZMA) wybieraj na podstawie pomiaru rozmiaru, czasu wczytania i pamięci telefonu, a nie na oko. LZMA często daje mniejszy transfer, ale rozpakowanie może kosztować więcej RAM; LZ4 pozwala odczytywać fragmenty.
- Cele: Obóz APK <= 400 MB wg wcześniejszej decyzji, pozostałe paczki bazowe dążyć do <= 500 MB. Nie nazywaj przekroczenia sukcesem, nie obiecuj wielkości bez rzeczywistego BuildReport. Nie blokuj kompilacji ostrzeżeniem o rozmiarze; zamiast tego raport do właściciela i brak deklaracji gotowości wydania.
- Każdy build: tabela rozmiarów APK, scen (skompresowany/nieskompresowany), tekstur, siatek, animacji, największych źródeł, raport zależności i duplikatów, różnica względem ostatniej wersji, wskaźniki RAM/VRAM/FPS z urządzenia, log awarii. `BuildPipeline.BuildPlayer = Succeeded` oznacza kompilację, NIE sprawny telefon.
- Jeśli w Androidzie `Loading.Preload`, `SIGTRAP`, `Position out of bounds`, `level1 corrupted`: raportuj dowód, analizuj MonoScript/GUID/serializację i dane sceny; nie zgaduj „brak RAM” ani „winny model X” bez symboli/logcat. Brak obowiązkowego MonoScript to błąd integralności, ostrzeżenie wyglądu pozostaje tylko raportem.
- TESTY: szybkie statyczne testy kodu mogą działać bez kosztownego Unity; pełne Unity/IL2CPP/generowanie scen/APK tylko po odrębnej zgodzie właściciela. Nie wywołuj `workflow_dispatch` samoczynnie z `push`; brak restartów i równoległych długich testów.
- Dokumentacja urzędowa i techniczna: Unity `https://docs.unity3d.com/6000.0/Documentation/Manual/assets-avoid-duplication.html` (duplikaty), `https://docs.unity3d.com/6000.0/Documentation/Manual/assetbundles-compression-format.html` (LZ4/LZMA), `https://developer.android.com/guide/playcore/asset-delivery` (PAD). Zasady powyżej są wymaganiami projektu, a nie twierdzeniem, że wykonano wdrożenie.

1. GAŁĘZIE I SILNIK
- Repo wawerpatryk89-sketch/braxhorn-city. Unity 6, Android, mobile-first.
- Miasto R1: claude/miasto-opus-20261031
- Osiedle: claude/osiedle-42-poprawka
- Obóz: codex/oboz-przebudowa-pbr-20261105
- Miasto Mistrzów: gpt/miasto-mistrzow-kasyno-t11-20261105
- Wersja Unity = ta przypięta w workflow danego rejonu (Mistrzowie 6000.0.85f2). Nie zmieniaj jej.
- Nigdy main. Nigdy dwa rejony w jednym commicie.
- JEDNA GAŁĄŹ = JEDEN WYKONAWCA NARAZ. Przed pracą: git fetch, sprawdź ostatnie commity. Przed pushem: fetch + rebase na najnowsze. Jeśli ktoś pushował w ostatnich 30 min, pytasz właściciela.

2. JAK PRACOWAĆ (każde zadanie)
1) Przeczytaj zadanie i kod, którego dotyczy. 2) Ustal przyczynę w kodzie (plik:linia). 3) Napraw WSZYSTKIE podobne miejsca, nie jedno. 4) Uruchom testy statyczne rejonu (python -m unittest w unity-tools/<rejon>, z jego folderu). 5) Commit + push. 6) Raport.
- Bez nowych funkcji, gdy ostatni build padł.
- Nie zmieniasz zatwierdzonego wyglądu. Nie dodajesz rzeczy, o których właściciel nie mówił.
- „Zatwierdzone przez właściciela” piszesz tylko z cytatem jego wiadomości. Wzór/zdjęcie od właściciela jest ważniejsze niż komentarz w kodzie.

3. JAK BUDOWAĆ SCENY (kod generatorów)
- Skala K = 1,294. Każdy model = realny wymiar × K (łóżko 2,0 m, krzesło 0,9 m, drzwi 2,1 m, człowiek 1,8 m). NIGDY nie wciskaj modelu w wymyślone pudełko (min(target/bounds)).
- Stawianie: raycast w dół → dolna krawędź bounds na podłodze/gruncie. Rzeczy ścienne (lustro, pisuar, półka, szyld, mural) raycast do ściany, tył na ścianie. Nic w powietrzu, w ścianie ani na sobie nawzajem; różne produkty = różne miejsca.
- Szyld zawsze na belce/ścianie/pasie nad witryną, nie na patykach i nie przed szybą.
- Materiały: tylko PBR z tekstur już pobieranych w danym rejonie (kolor + normal + ARM). Każda powierzchnia swój materiał (posadzka ≠ ściana ≠ sufit). Lustro = reflection probe.
- Tekstury: 4K tylko główne postacie, reszta 2K, ASTC. Murale min. 4096 px szerokości, ok. 100 px na metr ściany. Format importu sprawdzaj wg obsługi urządzenia; źródła HD pozostają niezmienione.
- Brakuje modelu → lista do właściciela. Nie robisz zastępstwa z kostek.
- Telefon: LOD, instancje GPU, światła vertex/wypalone, bez cieni z lamp. Rozmiar APK w raporcie; obóz maks. 400 MB. Limit przekroczony = nie deklaruj wydania gotowego, pokaż źródło wzrostu; nie zatrzymuj kompilacji kontrolą kosmetyczną.

4. JAK PISAĆ KOD, ŻEBY BUILD NIE PADAŁ
- TWARDY BŁĄD (throw) tylko gdy bez tego nie ma gry: błąd kompilacji, brak sceny głównej/postaci gracza, zapis sceny, podpis APK, brak pliku bez którego nic nie powstanie; nieprawidłowy lub brakujący MonoScript/GUID w wymaganej scenie to błąd integralności, nie kosmetyka.
- Wszystko inne = BraxBuildQuality.Warn(...) i build idzie dalej: rozmiary tekstur, mipmapy, liczba sztuk, skala, dokładne liczby (32 leżaki, 35 łodzi, 95 miejsc), wygląd.
- Każdą dekorację i każdy osobny budynek wywołuj przez BraxChampionsFoundation.Optional("nazwa", () => ...) (lub odpowiednik w rejonie): wyjątek → ostrzeżenie z pełnym błędem, reszta świata buduje się dalej. Obiekt pół-ustawiony przy błędzie usuń (DestroyImmediate).
- NIGDY throw wewnątrz AssetPostprocessor (OnPreprocess*). Tylko Debug.LogWarning + return.
- Ścieżki: Unity startuje w unity-city/. Plik spoza Assets (np. unity-tools/...) szukaj w obu miejscach: "x" i "../x". Najlepiej użyj gotowego RepoFile().
- Obrazki: przed wczytaniem wymuś TextureImporter: textureType Default, textureShape Texture2D, potem ImportAsset(ForceSynchronousImport), dopiero potem LoadAssetAtPath<Texture2D>. Nigdy nie sprawdzaj dokładnych pikseli po imporcie (Android je zmienia) — tylko GetSourceTextureWidthAndHeight i to jako ostrzeżenie.
- Każdy plik pobierany w CI ma stałe .meta w repo (stały GUID, textureShape 2D dla obrazków).
- Testy statyczne pilnują ZACHOWANIA, nie dokładnego tekstu kodu. Zmieniasz kod → dopasuj test w tym samym commicie.

5. JAK PUSZCZAĆ BUILDY
- Tylko na wyraźne słowo właściciela w tej rozmowie. Jeden build naraz, workflow_dispatch, z dokładnym SHA. Commit/push NIGDY nie odpala buildu.
- Najpierw tryb „sceny” (bez APK): ma wypisać WSZYSTKIE błędy i ostrzeżenia. Unity / import dużych zasobów / scena też są kosztowne — wymagają odrębnego polecenia właściciela. Dopiero zero błędów integralności → APK; ostrzeżenia jakości nie blokują kompilacji.
- Padło: pierwszy wyjątek + stack trace → przyczyna → naprawa wszystkich podobnych miejsc → testy → jeden nowy build. Bez restartów w ciemno, bez zatrzymywania biegnącego buildu bez zgody.
- Zmiana samych obrazków/tekstur (murale, szyldy) → tryb „aktualizacja”, nie nowy APK, jeśli rejon go ma i działa. Ten sam identyfikator, ten sam certyfikat, versionCode + 1.
- Bez zdjęć w CI. Dowód działania na Androidzie = nagranie właściciela z telefonu i/lub logcat z konkretnego SHA, nie zielony CI.

6. PORZĄDKI
- Usuwanie czegokolwiek (Releases, cache unity-ci-cache-*, stare APK, workflow, gałęzie) tylko po liście zaakceptowanej przez właściciela.
- Docelowo na rejon: 1 workflow APK (z trybami apk/sceny/aktualizacja), 1 test źródeł. Jednorazowe *-once-*, *-smoke-*, *-probe-* do listy usunięcia.

7. RAPORT (zawsze w tej kolejności)
Pierwsze słowa: OBÓZ TRENINGOWY / MIASTO R1 / OSIEDLE / MIASTO MISTRZÓW.
Potem: co zrobione (commit), co przetestowane (wynik testów), liczba ostrzeżeń, co nie działa (pierwszy błąd), co dalej. Bez „gotowe”, dopóki właściciel nie zobaczy tego na telefonie.

8. AKTUALNE PRIORYTETY I ZASADA DOWODÓW
- Najpierw przyczyny nieudanych buildów i awarii telefonu, później nowe funkcje. W R1 obowiązuje zlecony audyt mostu/wiaduktu i infrastruktury przejazdu. Zmiany układu i wizualizacji wymagają weryfikacji ze wzorcem od właściciela, nie zgaduj parametrów.
- Nie zakładaj, że jakakolwiek gałąź ma już mały bootstrap, działające zdalne Addressables lub delta-update. Odczytaj jej prawdziwy `BuildPlayerOptions.scenes`, katalog i `BuildReport`.
- Przed pracą sprawdź aktualny HEAD gałęzi i aktywność innego wykonawcy. Nie nakładaj commitów na czyjeś aktywne zmiany. Zasady globalne zapisuj osobnym commitem tylko `AGENTS.md`, roboty nad R1 osobnym commitem.
- Raportuj w kolejności: FAKTY (źródło, run, commit) -> DIAGNOZA (plik:linia) -> DECYZJA -> DZIAŁANIE (dokładny zakres) -> DOWÓD (test, log, telefon) -> BRAKI. Nie deklaruj wykonania ani testów, których faktycznie nie było.
