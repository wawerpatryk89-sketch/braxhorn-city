# BRAXHORN CITY — ZASADA 1: JAK BUDUJEMY, ŻEBY DZIAŁAŁO, DOBRZE WYGLĄDAŁO I NIE WAŻYŁO

Źródła: Android Developers „Optimize game size” (developer.android.com/games/optimize/game-size), Unity 6 „Optimize distribution size”.
Obowiązuje KAŻDEGO wykonawcę (Claude, Grok, Codex, GPT) na KAŻDEJ gałęzi i w każdym rejonie (Miasto R1, Osiedle, Obóz, Miasto Mistrzów).
Ustalona 9.11.2026 na polecenie właściciela („ustalasz ogólną zasadę, jak pracujemy, jak budujemy, jak kompresujemy — wszystko”),
po tym, jak APK Miasta Mistrzów urosło do 2 GB, a Miasta R1 do 1,35 GB. Punkt odniesienia: działający APK Miasta R1 z 3.11 — 743 MB.
Gdy coś tu jest sprzeczne z notatkami innych wykonawców w kodzie, komentarzach czy dokumentach — obowiązuje ten plik i AGENTS.md.

## A. WAGA — ZASADA GŁÓWNA: GRA ROŚNIE BEZ LIMITU, MARNOTRAWSTWO MA LIMIT ZERO

Właściciel 9.11: „nie ma limitu, bo się rozbudowujemy”. Więc żadnych sufitów w MB na rejon (poza Obozem 400 MB — limit właściciela z 6.11).
Zamiast tego — jak robią to duże realistyczne gry na Androida (Android Developers „Optimize game size”, Unity „Optimize distribution size”):

1. **Waga rośnie TYLKO z treści.** Każdy build porównany z poprzednim (raport `build-size-*.txt`): wzrost musi mieć listę „co dodano i ile waży”.
   Wzrost bez nowej treści = błąd ustawień importu (tak urosło R1 743→1349 MB i Mistrzowie do 2 GB) — naprawa przed następnym buildem.
   Każdy pojedynczy plik > 1 % całości trafia na listę do sprawdzenia (zasada Google).
2. **Tekstura ma rozdzielczość tego, co gracz widzi, nie tego, co dał Sketchfab.** Ok. 100 px na metr z bliska (ściana 10 m → 1K),
   elewacje oglądane z ulicy 512–1K, grunt i drogi 1K z powtarzaniem, drobnica w atlasach. Nie „1K bo limit”, tylko „tyle, ile widać”.
   Grafiki właściciela z bliska (murale, szyldy, ekrany) 2K, mural 19 — 4K. Duże → zbijane PRZY IMPORCIE (ustawienie platformy Android), nie ręcznie.
3. **Kompresja GPU:** cel **ASTC 6×6** (ok. połowa wagi ETC2 RGBA przy tej samej jakości; wspierane przez każdy telefon z Vulkan/GLES 3.1).
   Dziś ETC2 (działający APK); przejście na ASTC = jeden zmierzony build testowy z raportem przed/po i cytat właściciela, potem standard dla wszystkich rejonów.
   Tekstury UI/ekrany startowe: PNG skompresowany albo WebP. Jednolite kolory: 16-bit (RGB565), nie 32-bit.
4. **Mipmapy + streaming mipmap wszędzie**, na telefonie mip ≥ 1 (najwyższy poziom nigdy w całości w pamięci — wywrotki 31.10). Unity Texture Streaming.
5. **Rozbudowa = nowe paczki, nie grubszy jeden plik.** Gra startowa (siłownia, ulica główna, mieszkanie) ma być mała; każdy rejon i każda duża
   dzielnica (wyspa, nowe dzielnice, port, Obóz, Mistrzowie) = osobna scena-obszar doczytywana przy podejściu (jak dziś BraxObszary) i osobna
   pozycja w raporcie wagi. Gdy całość przekroczy to, co telefon instaluje jednym plikiem, przechodzimy na **AAB + Play Asset Delivery**
   (install-time → fast-follow → on-demand per rejon) — to jest droga do „wielu GB treści”, nie większy APK i nie mocniejsza kompresja.
   Do tego czasu: jeden APK, ten sam identyfikator i klucz, instalowany jako aktualizacja.
6. **Co nie jest widoczne, nie jest w buildzie:** wyłączone obiekty, drugi komplet tekstur, nieużywane pakiety, pełne modele ze Sketchfaba
   (auta, trawniki, drogi w paczce budynku) — usuwane offline. Wyłączony renderer nadal waży.
7. **Kod i binaria:** IL2CPP, tylko ARM64 (split by architecture / AAB gdy Play), stripping Managed ≥ Medium, Minify Release po teście.

## B. TEKSTURY — tu powstaje 90 % wagi

1. **Android: domyślnie 1024 px** (elewacje, grunt, drogi, wnętrza, modele ze Sketchfaba, Poly Haven, postacie tła) — bo tyle widać (pkt A.2).
   Więcej tylko tam, gdzie gracz patrzy z bliska: grafiki właściciela (murale, szyldy, ekrany) — 2048; mural 19 na siłowni — 4096; główne postacie
   (Brax, przeciwnicy w ringu) — 2048. Inny wyjątek = cytat właściciela albo pomiar „widać różnicę na telefonie” w commicie.
2. **Pobieranie:** z Poly Haven i podobnych tylko paczki **2K**, nigdy 4K/8K. Importer i tak zbija do 1K; pobrane 4K ważą w repo, cache i Library.
3. **Jedna tekstura = jeden plik.** Zanim dodasz teksturę, sprawdź, czy ta sama (albo równoważna: „inny beton”) już jest w rejonie — użyj jej.
   Zestaw PBR (kolor + normal + ARM) na rodzinę powierzchni (beton, cegła, asfalt, drewno, metal, trawa), nie na obiekt.
4. **Kompresja:** ETC2 dziś (działający APK) → **ASTC 6×6** po jednym zmierzonym buildzie testowym (pkt A.3), potem standard.
   Mipmapy włączone, **streaming mipmap włączony**, na telefonie **mip ≥ 1** (poziom 0 nigdy nie ładuje się w całości — wywrotki pamięci 31.10).
   Bez „Read/Write Enabled” (podwaja pamięć). Bez crunch na normalach.
5. **Generowane w buildzie** (atlasy, przeszklenia, wypalone cienie): te same limity. 9 tekstur po 15 MB „zrobionych przez kod” to 135 MB.
6. Grafiki właściciela trzymasz w jednym miejscu rejonu z `.meta` o stałym GUID; nie kopiujesz ich do drugiego folderu.

## C. BUILD PLAYERA — ustawienia, których NIE zmienia się „na próbę”

- **JEDNA GRA, JEDEN PLIK APK, INSTALUJE SIĘ NORMALNIE** (właściciel 9.11: „to ma być jedna wspólna gra, jedna zasada” — Miasto R1 instaluje się
  normalnie, Mistrzowie wymagali pobierania i rozpakowywania zipów — ZABRONIONE). Żadnego OBB, „Split Application Binary”, zipów do rozpakowania,
  pobierania po instalacji, własnych instalatorów ani skryptów na telefonie. Każdy rejon buduje ten sam `com.braxhorn.city` tym samym
  kluczem; APK ląduje w Releases i instaluje się jednym dotknięciem jako aktualizacja. Play Asset Delivery (pkt A.5) dopiero wtedy, gdy
  właściciel zdecyduje o publikacji w Google Play — nie wcześniej i nie na własną rękę.
- `BuildOptions.None`. **Bez LZ4 / LZ4HC** (w R1 podniosło APK z 743 na 1349 MB). Zmiana kompresji buildu wymaga pomiaru przed/po i cytatu właściciela.
- IL2CPP, **tylko ARM64**, stripping Managed = Medium lub wyżej. Bez x86, bez ARMv7.
- **Bez Addressables, bez Asset Bundles, bez pakietów, których kod nie używa.** Nieużywany pakiet w `Packages/manifest.json` = do usunięcia.
  Każda zmiana `manifest.json` unieważnia cache Library (godzina buildu więcej).
- Jedna scena główna + sceny obszarów doczytywane przy podejściu (BraxObszary). Poddrzewa ze skryptem lub LODGroup są niepodzielne przy cięciu.
- Wersja Unity = przypięta w workflow rejonu. Nie zmieniasz.
- Identyfikator `com.braxhorn.city`, stały klucz podpisu, versionCode z licznika — każdy APK instaluje się jako aktualizacja, nie druga gra.

## D. SIATKI, ŚWIATŁO, DŹWIĘK

- Obiekt ≤ 65 000 wierzchołków; budynki i drzewa z LOD (3 poziomy, ostatni = bryła); powtarzalne rzeczy (latarnie, ławki, słupki, auta) jako instancje GPU.
- Mesh Compression włączone, bez Read/Write, bez niepotrzebnych kanałów (kolory wierzchołków, UV2, gdy nieużywane).
- Model ze Sketchfaba: zostają tylko potrzebne części (budynek bez „aut i trawnika w paczce”); niepotrzebne siatki wycinasz offline, nie wyłączasz w scenie (wyłączone nadal ważą).
- Światła i cienie (właściciel 9.11: „ma być jak najlepsza jakość — teraz nawet na średnim cieni nie ma”): **cienie ze słońca ZAWSZE włączone**,
  na każdym poziomie jakości (Niski: 1 kaskada, 40 m, miękkie wyłączone; Średni: 2 kaskady, 80 m; Wysoki: 4 kaskady, 150 m, miękkie).
  Ustawienia jakości w kodzie buildu (QualitySettings per poziom), nie „jak wyjdzie”. Lampy nocne: wypalone lub bez cieni; żadnego światła
  real-time per obiekt. Lustro = reflection probe. Oszczędzamy na teksturach i siatkach (pkt A–B), NIE na cieniach i nie na jakości widoku.
- Audio: Vorbis, SFX mono, muzyka i ambient streaming; bez WAV w buildzie.

## E. JAKOŚĆ — ma być jak najlepsza (właściciel 9.11); oszczędność bierze się z pkt A–B, nie z obniżania wyglądu (skrót pkt 3 AGENTS.md, obowiązuje w całości)

- Skala K = 1,294. Każdy model = realny wymiar × K. Nigdy skalowanie „żeby pasowało do pudełka”.
- Stawianie: raycast w dół, dół bryły na gruncie/posadzce. Rzeczy ścienne (mural, szyld, lustro, półka) raycast DO ŚCIANY, tył na ścianie.
  Gdy ściana nie ma kolizji — szukasz jej po rendererach; gdy nadal brak — ostrzeżenie `DO POPRAWKI` **i obiekt stawiasz na najlepszym
  znanym miejscu**, nie pomijasz go po cichu (błąd Claude 9.11: mural 19 zniknął z buildu).
- Materiały PBR z tekstur rejonu, każda powierzchnia swój materiał. Nic z kostek jako zastępstwo — brak modelu = lista do właściciela.
- Nic nie wisi w powietrzu, nic nie tkwi w ścianie, nic nie dubluje się (stare z GLB + nowe w tym samym miejscu = stare wyłączone po numerze siatki, nie po nazwie materiału).
- Wzór/zdjęcie/nagranie właściciela > komentarz w kodzie > własny pomysł.

## F. JAK PRACUJEMY (organizacja)

1. **Jeden wykonawca = jedna gałąź = jedno zlecenie od A do Z. Bez pomocników, podagentów, równoległych czatów uruchamianych przez wykonawcę.**
   Właściciel 9.11: „żadnych pomocników — ty masz zlecenie, ty masz je zrobić od A do Z”. Za duże na jedno podejście → mówisz właścicielowi, on dzieli.
2. Zanim zmienisz: `git fetch`, ostatnie commity, przyczyna w kodzie (plik:linia). Naprawiasz wszystkie podobne miejsca, nie jedno.
3. **Co łatwiej usunąć niż poprawić — usuwasz i planujesz od zera** (właściciel 9.11). Nie nawarstwiasz poprawek na poprawkach.
4. Nie dodajesz rzeczy, o które właściciel nie prosił. Nie zmieniasz zatwierdzonego wyglądu. „Zatwierdzone” = cytat właściciela w commicie.
5. Błędy w kodzie generatorów = ostrzeżenie `DO POPRAWKI` i build idzie dalej; `throw` tylko gdy bez tego nie ma gry (kompilacja, scena główna, gracz, podpis).
6. Testy statyczne (`python3 -m unittest discover -s unity-tools/<rejon>`) i sprawdzacz C# (`unity-tools/validation/check_csharp_scopes.py`)
   przed każdym pushem. Testy pilnują zachowania, nie tekstu kodu; nie „dopasowuje się” testu do złego stanu.
7. **Build tylko na wyraźne słowo właściciela**, jeden naraz, `workflow_dispatch` z dokładnym SHA. Commit i push nigdy nie odpalają buildu.
   Padł → pierwszy błąd → przyczyna → naprawa → testy → jeden nowy build. Bez restartów w ciemno.
8. Nigdy `main`. Nigdy dwa rejony w jednym commicie. Pliki innego rejonu — nie dotykasz.
9. Usuwanie czegokolwiek w repo/Releases/cache — tylko po liście zaakceptowanej przez właściciela.
10. Raport: pierwsze słowa = rejon; potem commit, wynik testów, **waga APK i różnica**, liczba ostrzeżeń, pierwszy błąd, co dalej.
    „Gotowe” dopiero, gdy właściciel zobaczył to na telefonie. Nie opisujesz pracy — pokazujesz, co zmieni się na ekranie.

## G. LISTA KONTROLNA PRZED KAŻDYM PUSHEM (odhaczasz w raporcie)

- [ ] żadna nowa tekstura większa niż to, co widać (pkt A.2/B.1); duże zbijane przy imporcie
- [ ] żadnego nowego pliku 4K/8K w repo ani w pobieraniu CI
- [ ] jeden APK instalowany normalnie (bez OBB/zip/pobierania), `BuildOptions.None`, ARM64, brak nowych pakietów w `manifest.json`
- [ ] każdy nowy obiekt: realny wymiar × K, raycast, materiał PBR z rejonu
- [ ] testy statyczne + sprawdzacz C# zielone
- [ ] commit z cytatem właściciela albo z „decyzja techniczna wg ZASADA-1 pkt …”
- [ ] bez pomocników
