# BRAXHORN CITY — ZASADA 1: JAK BUDUJEMY, ŻEBY DZIAŁAŁO, DOBRZE WYGLĄDAŁO I NIE WAŻYŁO

Obowiązuje KAŻDEGO wykonawcę (Claude, Grok, Codex, GPT) na KAŻDEJ gałęzi i w każdym rejonie (Miasto R1, Osiedle, Obóz, Miasto Mistrzów).
Ustalona 9.11.2026 na polecenie właściciela („ustalasz ogólną zasadę, jak pracujemy, jak budujemy, jak kompresujemy — wszystko”),
po tym, jak APK Miasta Mistrzów urosło do 2 GB, a Miasta R1 do 1,35 GB. Punkt odniesienia: działający APK Miasta R1 z 3.11 — 743 MB.
Gdy coś tu jest sprzeczne z notatkami innych wykonawców w kodzie, komentarzach czy dokumentach — obowiązuje ten plik i AGENTS.md.

## A. LIMITY WAGI (twarde — build cięższy to regresja do naprawy, nie „stan faktyczny”)

| rejon | APK maks. | uwaga |
|---|---|---|
| Miasto R1 | 800 MB | 3.11 było 743 MB przy pełnym mieście, porcie i wyspie |
| Miasto Mistrzów | 600 MB | 2 GB = błąd ustawień importu, nie „dużo treści” |
| Osiedle | 600 MB | |
| Obóz | 400 MB | limit właściciela z 6.11 |

Każdy build zapisuje raport rozmiaru (`build-size-*.txt`: łącznie, tekstury, siatki, audio, per scena). W raporcie z buildu podajesz
**wagę APK, wagę tekstur i różnicę do poprzedniego buildu**. Wzrost > 5 % bez treści, której właściciel wprost zażądał = naprawiasz przed
następnym buildem. Nie wolno „zapisać stanu” w teście, żeby test przechodził przy większej wadze.

## B. TEKSTURY — tu powstaje 90 % wagi

1. **Android: maks. 1024 px na WSZYSTKIM** (elewacje, grunt, drogi, wnętrza, modele ze Sketchfaba, Poly Haven, postacie tła).
   Wyjątki, tylko te: grafiki właściciela oglądane z bliska (murale, szyldy, ekrany) — 2048; mural 19 na siłowni — 4096; główne postacie
   (Brax, przeciwnicy w ringu) — 2048. Każdy inny wyjątek = cytat właściciela w commicie.
2. **Pobieranie:** z Poly Haven i podobnych tylko paczki **2K**, nigdy 4K/8K. Importer i tak zbija do 1K; pobrane 4K ważą w repo, cache i Library.
3. **Jedna tekstura = jeden plik.** Zanim dodasz teksturę, sprawdź, czy ta sama (albo równoważna: „inny beton”) już jest w rejonie — użyj jej.
   Zestaw PBR (kolor + normal + ARM) na rodzinę powierzchni (beton, cegła, asfalt, drewno, metal, trawa), nie na obiekt.
4. **Kompresja:** ETC2 (jak w działającym APK) — ASTC dopiero po zmierzonym teście na telefonie właściciela, osobnym commitem.
   Mipmapy włączone, **streaming mipmap włączony**, na telefonie **mip ≥ 1** (poziom 0 nigdy nie ładuje się w całości — wywrotki pamięci 31.10).
   Bez „Read/Write Enabled” (podwaja pamięć). Bez crunch na normalach.
5. **Generowane w buildzie** (atlasy, przeszklenia, wypalone cienie): te same limity. 9 tekstur po 15 MB „zrobionych przez kod” to 135 MB.
6. Grafiki właściciela trzymasz w jednym miejscu rejonu z `.meta` o stałym GUID; nie kopiujesz ich do drugiego folderu.

## C. BUILD PLAYERA — ustawienia, których NIE zmienia się „na próbę”

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
- Światła: słońce + wypalone/vertex; **bez cieni z lamp**, bez świateł real-time per obiekt. Lustro = reflection probe.
- Audio: Vorbis, SFX mono, muzyka i ambient streaming; bez WAV w buildzie.

## E. JAKOŚĆ — żeby wyglądało (skrót pkt 3 AGENTS.md, obowiązuje w całości)

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

- [ ] żadna nowa tekstura > 1024 px na Androidzie bez wyjątku z pkt B.1
- [ ] żadnego nowego pliku 4K/8K w repo ani w pobieraniu CI
- [ ] `BuildOptions.None`, ARM64, brak nowych pakietów w `manifest.json`
- [ ] każdy nowy obiekt: realny wymiar × K, raycast, materiał PBR z rejonu
- [ ] testy statyczne + sprawdzacz C# zielone
- [ ] commit z cytatem właściciela albo z „decyzja techniczna wg ZASADA-1 pkt …”
- [ ] bez pomocników
