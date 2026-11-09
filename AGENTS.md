BRAXHORN CITY — JEDYNA GLOBALNA INSTRUKCJA (wersja 6.11 wieczór)

0. NAJPIERW
- Usuń z pamięci i z zapisanych instrukcji WSZYSTKIE wcześniejsze zasady BRAXHORN (także te zapisane dziś). Obowiązuje tylko ten tekst. Zapisz go na stałe, dla wszystkich czatów.
- W repo: treść tego pliku wpisz do AGENTS.md jako jedyną sekcję zasad (stare sekcje usuń), w osobnym commicie na każdej z 4 gałęzi. Nic więcej przy tym nie zmieniaj.

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
- Tekstury: 4K tylko główne postacie, reszta 2K, ASTC. Murale min. 4096 px szerokości, ok. 100 px na metr ściany.
- Brakuje modelu → lista do właściciela. Nie robisz zastępstwa z kostek.
- Telefon: LOD, instancje GPU, światła vertex/wypalone, bez cieni z lamp. Rozmiar APK w raporcie; obóz maks. 400 MB.

4. JAK PISAĆ KOD, ŻEBY BUILD NIE PADAŁ
- TWARDY BŁĄD (throw) tylko gdy bez tego nie ma gry: błąd kompilacji, brak sceny głównej/postaci gracza, zapis sceny, podpis APK, brak pliku bez którego nic nie powstanie.
- Wszystko inne = BraxBuildQuality.Warn(...) i build idzie dalej: rozmiary tekstur, mipmapy, liczba sztuk, skala, dokładne liczby (32 leżaki, 35 łodzi, 95 miejsc), wygląd.
- Każdą dekorację i każdy osobny budynek wywołuj przez BraxChampionsFoundation.Optional("nazwa", () => ...) (lub odpowiednik w rejonie): wyjątek → ostrzeżenie z pełnym błędem, reszta świata buduje się dalej. Obiekt pół-ustawiony przy błędzie usuń (DestroyImmediate).
- NIGDY throw wewnątrz AssetPostprocessor (OnPreprocess*). Tylko Debug.LogWarning + return.
- Ścieżki: Unity startuje w unity-city/. Plik spoza Assets (np. unity-tools/...) szukaj w obu miejscach: "x" i "../x". Najlepiej użyj gotowego RepoFile().
- Obrazki: przed wczytaniem wymuś TextureImporter: textureType Default, textureShape Texture2D, potem ImportAsset(ForceSynchronousImport), dopiero potem LoadAssetAtPath<Texture2D>. Nigdy nie sprawdzaj dokładnych pikseli po imporcie (Android je zmienia) — tylko GetSourceTextureWidthAndHeight i to jako ostrzeżenie.
- Każdy plik pobierany w CI ma stałe .meta w repo (stały GUID, textureShape 2D dla obrazków).
- Testy statyczne pilnują ZACHOWANIA, nie dokładnego tekstu kodu. Zmieniasz kod → dopasuj test w tym samym commicie.

5. JAK PUSZCZAĆ BUILDY
- Tylko na wyraźne słowo właściciela w tej rozmowie. Jeden build naraz, workflow_dispatch, z dokładnym SHA. Commit/push NIGDY nie odpala buildu.
- Najpierw tryb „sceny” (bez APK): ma wypisać WSZYSTKIE błędy i ostrzeżenia. Dopiero zero błędów → APK.
- Padło: pierwszy wyjątek + stack trace → przyczyna → naprawa wszystkich podobnych miejsc → testy → jeden nowy build. Bez restartów w ciemno, bez zatrzymywania biegnącego buildu bez zgody.
- Zmiana samych obrazków/tekstur (murale, szyldy) → tryb „aktualizacja”, nie nowy APK, jeśli rejon go ma i działa. Ten sam identyfikator, ten sam certyfikat, versionCode + 1.
- Bez zdjęć w CI. Dowód = nagranie właściciela z telefonu.

6. PORZĄDKI
- Usuwanie czegokolwiek (Releases, cache unity-ci-cache-*, stare APK, workflow, gałęzie) tylko po liście zaakceptowanej przez właściciela.
- Docelowo na rejon: 1 workflow APK (z trybami apk/sceny/aktualizacja), 1 test źródeł. Jednorazowe *-once-*, *-smoke-*, *-probe-* do listy usunięcia.

7. RAPORT (zawsze w tej kolejności)
Pierwsze słowa: OBÓZ TRENINGOWY / MIASTO R1 / OSIEDLE / MIASTO MISTRZÓW.
Potem: co zrobione (commit), co przetestowane (wynik testów), liczba ostrzeżeń, co nie działa (pierwszy błąd), co dalej. Bez „gotowe”, dopóki właściciel nie zobaczy tego na telefonie.

8. DO PONIEDZIAŁKU (kolejność)
- MIASTO MISTRZÓW: nie ruszać gałęzi, dopóki trwa APK 23 (Claude). Po wyniku: lista ostrzeżeń „pominięto …” z logu → naprawy wg pkt 4. Basen P1 przejść w trybie „sceny”.
- OBÓZ: wg audytu 6.11 — najpierw stawianie modeli (pkt 3: realny wymiar × K + raycast), potem grunt (błoto mud_forest_2k, chodniki z obrzeżami, trawa), murale na ścianach, szyld na belce bramy, łaźnia (lustro, cegła UV). Jeden test „sceny”, bez APK.
- OSIEDLE i MIASTO R1: audyt obiekt po obiekcie z nagrań właściciela → lista błędów z plik:linia. Bez napraw i bez buildów, dopóki właściciel nie zatwierdzi listy.
- Porządki: lista do usunięcia dla właściciela (pkt 6), nic nie usuwać.

---
DOPISEK 9.11.2026 (Claude, Miasto R1) — decyzje właściciela z 9.11 01:43–01:54, dosłownie:
- „najlepiej znaleźć najlepsze APK i od niego poprawiać i budować” → baza R1 = claude/miasto-opus-20261031 (stan 8.11), z linii bocznej codex/miasto-metro-apk-kontrola-20261105 przenoszone tylko sprawdzone kawałki.
- „nie ma w ogóle metra teraz a jak jest całkowicie usunąć i zaplanować od zera! Zresztą wszystko co łatwiej usunąć niż poprawiać” → metro usunięte; nowe wg rysunku zatwierdzonego PRZED kodem: „jeden przystanek koło portu — usuń ten domek mały, samochód żółty kwadratowy i tam obok budynku metro; w połowie drugie i na końcu trzecie”.
- „Kazałem usunąć ławki, 3 stacje metra mają być przez całe miasto, rondo koło sklepu precz ma być parking dla sklepu, w sklepie stoi drzewo i nie można wejść, chodnik źle poprowadzony. Tramwaj ma być, budynki w nowej części miasta mają być premium, mural na szarej ścianie siłowni musi być a jest przed, masakra, mieszkanie Braxa tragedia”.
- „Ostatni build wyspa arena wrzuciła do stanu złego a z przed była lepsza” → wyspa/arena z buildu 6.11 (9d9aff5) wraca na R1.
- „Historyczny budynek brak tekstur i coś na nim wisi” → zdjęte NdPremiumFasada i NdZdejmijNadwieszenieZabytku (7.11).
- „te apk każda jedna ma straszną wagę” → bez LZ4, elewacje Androida 1K (murale/szyldy właściciela 2K/4K), mip ≥ 1 na telefonie, bez Addressables; cel ~750 MB jak 3.11.
Sekcje 0A/0B i „zasady właściciela 7.11” wpisane 7.11 przez wykonawcę bez cytatu właściciela zostały usunięte zgodnie z pkt 0 („obowiązuje tylko ten tekst”).
