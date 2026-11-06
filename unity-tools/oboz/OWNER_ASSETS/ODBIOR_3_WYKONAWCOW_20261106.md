# BRAXHORN OBÓZ — audyt niezależny trzech wykonawców (06.11.2026)

## Status i ograniczenia
Zakres: TYLKO obóz w Unity 6000.0.58f2, Android mobile-first. Jedna aplikacja BRAXHORN CITY; nie zmieniać miasta, osiedla ani źródeł postaci. Gałąź `codex/oboz-przebudowa-pbr-20261105`. Ostatnie poprawki kodowe nie są potwierdzoną sceną ani APK. NIE publikować, NIE uruchamiać automatycznie pełnego buildu. Wymagany oddzielny dowód Unity Edit/Play Mode i z telefonu.

### Dowód dostępności oryginałów
1. Prywatny DRAFT GitHub Release 435486352, `oboz-owner-remodel-20261105`: 5 oryginalnych części BRAXHORN_MODELE_CZESC_01..05_Z_05 + byk GLB. Łącznie 30 materiałów źródłowych w głównej paczce, 31 wpisów z manifestem. Nigdy nie kopiować prywatnych GLB do publicznego Git.
2. Dodatkowa, odnaleziona w ChatGPT Library paczka `gym_environment.zip` (4 214 018 B): `gym_environment.fbx` (57 nazwanych obiektów), `gym_environment.png` 2048×2048, oryginalny .blend; wyposażenie ławki, bieżnia mechaniczna, hantle, kettle, stojaki, drążki, poręcze, rower, maty.
3. `tractor_tire.glb` (3 668 680 B) z Library: rzeczywisty model 3D; NIE było go w poprzednich pięciu częściach.
4. `free_lowpoly_crossfit_-_rope_climbing.glb` (29 712 B) z Library: kandydat, wymaga akceptacji jakości. Nie używać jako automatycznej zamiany premium.
5. Przygotowana nowa prywatna paczka `BRAXHORN_OBOZ_DODATKI_SILOWNIA_20261106.zip` (7 298 373 B), SHA-256 `8fee0543521a8ae6043a0c4179e26f2bcd69f1cd778904e2d7b177f7cb22661f`. Ta paczka JEST PRZYGOTOWANA, lecz **nie została wgrana do GitHub Release**. Workflow wymaga jej obecności i ma zatrzymać się przed Unity, nie używając atrap. `owner_extra_assets.py` importuje trzy zatwierdzone pliki; nie importuje kandydata low-poly ani .blend.
6. Starszy manifest `private-inputs.json`: 204 pliki źródłowe, m.in. wcześniejsza siłownia. Nie mylić go z nową paczką 30 materiałów. Dawna siłownia z miasta nie jest automatycznie zaakceptowanym zastępstwem obozowej.

## Kontrola A — MODEL / MATERIAŁ, w tym zgodność z filmami właściciela
- [ ] Wszystkie oryginalne GLB, FBX, 2K/4K źródła zidentyfikowane SHA-256; żaden plik bez licencji nie trafia do publicznego repo.
- [ ] Nowa siłownia: 57 obiektów FBX, oryginalny atlas 2048, rozmiar i położenie na właściwej posadzce; bez atrap hantli, ławek, sztang i maszyn. Bez zdublowanych ringów/worków.
- [ ] Opony `tractor_tire.glb` na dwóch stanowiskach, 100% siatki i tekstur bez redukcji; fizyka, pozycja i animacja treningowa osobno zweryfikowane.
- [ ] Stojak na piłki lekarskie `model(1).glb` na stanowisku rozgrzewki.
- [ ] Długie liny treningowe: aktualne `rope-coil.glb` to zwoje, a nie pełne liny aktywne; brak akceptacji oznacza NIEGOTOWE.
- [ ] 24 oryginalne worki skórzane, nie proste cylindry; zawieszenie, kolizje i odgłos trafienia.
- [ ] Cztery oryginalne murale na właściwych budynkach, bez pustych tablic i przycinania obrazu; szyld bramy mocowany fizycznie.
- [ ] Trawa: Grass Medium 01 + 02 2K jako rzeczywista gęsta murawa. Obecny kod `BraxObozVegetation.Grass` wciąż tworzy instancje `grass_bermuda_01_seedling_d__unity.glb`; wymaga odrębnej przebudowy i pomiaru FPS.
- [ ] Błoto `mud_forest_2k`, zagłębienia, pobocza, poprawne UV/Normal/Roughness/Height; nie rozlewać błota po trawniku i trasach.
- [ ] Elewacja łaźni od zewnątrz ciemne PBR zgodnie z najnowszym poleceniem właściciela; w środku jasne białe cieniowane płytki PBR z Poly Haven. Poprzedni dokument podawał jasną elewację i ciemną podłogę: pierwszeństwo ma Nowsze polecenie użytkownika.
- [ ] Nie zmieniać postaci ani ich oryginalnych FBX.

## Kontrola B — ARCHITEKTURA / UKŁAD / INTERAKCJE
- [ ] Wiata: oryginalny FBX + oryginalny kolorowy atlas 2K, otwarcie na zatokę autobusową, brak sztucznych ławek przed wiatą. Kod skorygował yaw o 180 stopni, ale wymaga oględzin w Unity.
- [ ] Byk: oryginalny model, front do wjazdu (odwrócony o 180° w kodzie), stabilny cokół bez kolizji z autobusem.
- [ ] Szyld: przylega do belki i wsporników nad bramą, brak wizualnego wiszenia, pełne światło bramy, nieodwrócone napisy, całe logo.
- [ ] Bieżnia pełna wokół nowego ogrodzenia + odrębna prosta dokładnie 100 metrów, białe linie. Chodniki i kostka NIE przecinają czerwonej nawierzchni. Samo usunięcie pasów geometrycznych nie rozwiązuje kolizji głównych ścieżek.
- [ ] Wszystkie 4 domki dwuosobowe, po 2 rzeczywiste zaakceptowane JEDNOOSOBOWE łóżka (nie zestaw zastępczy lowpoly `bunk_bed`), szafki metalowe, kuchnia polowa, umywalka, kosze, pełne dojścia i drzwi.
- [ ] Stołówka: rzeczywisty `kitchen_-_assets.glb`, witryna, sztućce, garnki, tace, stoły bez blokowania korytarzy. `picnic_set_1.glb` zweryfikować: to drewniana ławka, nie kompletny stół.
- [ ] Łaźnia betonowa: wymienić białą prowizoryczną elewację. Pisuary, WC, brodziki, umywalki i baterie muszą stykać się z posadzką/ścianami; spłukiwanie, krany, prysznice i drzwi działają. Bez wiszących ceramik.
- [ ] Układ sanitariatów bez nakładania koliderów na drzwi; test przejścia, kamery, NavMesh.
- [ ] Ogrodzenie zamknięte, główna brama pozostaje w ustalonym miejscu, przestrzenie niepuste, wszystkie chodniki mają obrzeża.

## Kontrola C — UNITY / ANDROID / WYDANIE
- [ ] Unity Edit Mode: import 3 dodatkowych oryginalnych modeli, kontrola GUID, materiałów, rozdzielczości PBR, prefabów i pozycji world bounds.
- [ ] Unity Play Mode: stanowiska, wejścia, kolizje, ścieżki NPC, przystanek, schodki ringów, działa fizyka worków.
- [ ] Monika: użytkownik zgłasza delikatne unoszenie i kołysanie podczas Idle. Sprawdzić controller capsule, punkt styku stopy z terenem, root motion i bezruch kamery. Nie zmieniać rigów/źródłowych animacji bez zgody.
- [ ] Rzeczywisty podpis, package ID i zgodność z główną aplikacją BRAXHORN CITY; brak udawania aktualizacji przez osobne `com.braxhorn.oboz`.
- [ ] Żadnych fałszywych PASS przy ostrzeżeniach sceny. Test RAM/VRAM, FPS i rozmiar APK na realnym Androidzie. Wideo i zdjęcia każdej strefy po wdrożeniu.
- [ ] Dopóki kompatybilna baza Addressables nie jest zatwierdzona na telefonie, nie nazywać osobnego APK aktualizacją `.braxupdate.zip`.

## Wymagany wynik trzech niezależnych kontroli
Każdy audytor oddaje: SHA commita, listę faktycznych źródeł i SHA plików, zdjęcia wszystkich stref z telefonu, raport porównania z czterema filmami użytkownika, jawne testy PASS/FAIL oraz problemy blokujące. Odbiór dopiero po zgodnych trzech raportach oraz akceptacji właściciela. Zielony GitHub Actions = skompilowane, nie = wizualnie ukończone.

**Nieukończone lub niedostępne na dziś:** prawdziwe zaakceptowane łóżka jednoosobowe; dodatkowa paczka gym/tire nieumieszczona jeszcze w prywatnym GitHub Release; Grass Medium 3D; nowe ciemne PBR łaźni i białe płytki ścian; pełny test rozmieszczenia; Monika Idle; aktualizacja .braxupdate na telefonie. Nie zatajać tych blokad.
