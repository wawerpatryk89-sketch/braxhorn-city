# BRAXHORN CITY — AUDYT CAŁEGO OSIEDLA / 4.11.2026

**Zakres:** wyłącznie Unity 6, osiedle A+B, wnętrza, dojazdy, boiska, teren i granice. Bez ingerencji w miasto lub obóz.

**Weryfikowalne źródło:** GitHub Actions [build #41751910203](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41751910203), gałąź `claude/osiedle-42-poprawka`, commit `a4c9160e40c7f404779c13e7dfb0300f9247ce15` (4.11.2026, zakończony SUCCESS). Zrzuty wideo użytkownika pokazują dodatkowo dyskotekę i otoczenie. **UWAGA:** raport dotyczy starego, zakończonego buildu, NIE potwierdza napraw w nowszych commitach / na Androidzie. Wynik skanera = ryzyko geometryczne, nie koniecznie widoczne migotanie.

## Priorytet 0 — krytyczne przed uznaniem osiedla za gotowe

1. **Z-fighting / nakładanie powierzchni:** `BRAXHORN_MIGOTANIE osiedle` — 3978 potencjalnych par przed kontrolą; skaner zgłasza wycięcie 3069, ale po korekcie nadal 1399 par, z czego 181 o wspólnym polu powyżej 1 m². **Nie usuwać wszystkiego automatem:** części par są przezroczyste (szyby, lasery), ruchome lub specjalnie chronione. Dla nieprzezroczystych, stałych warstw usuwać dolną/starą geometrię, nie nakładać nowej.
2. **ARENA BRAXHORN:** `BRAXHORN_ARENA_MIGOTANIE` 57 par przed, 19 po; log wprost `DO POPRAWKI`. Obszar x -109.3…-62.7, z 45.7…83.3. Skontrolować elewacje, szyldy, trybuny i matę. Nie zmieniać muralu/szyldów bez potrzeby.
3. **Dyskoteka:** po kontroli 96 par w grupie `Osiedle A / dyskoteka — klub`. Osobno błąd użytkowy na nagraniu: zestaw Chesterfield był dzielony na pojedyncze fragmenty siatki jako fotele; loże z odwróconymi/niespójnymi elementami. Poprawka kodowa na gałęzi `codex/osiedle-loze-salon-przejscia-20261104`; wymaga widoków po wykonaniu (z góry, od frontu, test przejścia).
4. **Salon samochodowy:** 46 par w grupie ruchomych elementów + lista dużych nakładek m.in. `salon__policzek_stal_grafit ↔ szklo_balustrad` ok. (-41.3, 2.67, -42.2), `poręcz ↔ inox` ok. (-39, 3.72, -43.8). Na wideo wejście i schody utrudniają dostęp. Kod zmieniony na gałęzi lóż/salonu; potwierdzić kapsułę w Unity i przejście na antresolę.
5. **Przejścia i nawigacja:** `BRAXHORN_ROBOT` — 11512 punktów, 13 utknieć, 448 przypadków przenikania, 296 wypadnięć. To sygnały z symulacji, nie liczba awarii gracza. Niezbędne testy rzeczywistych tras: spawn → ulica → dyskoteka/arena/salon → park → osiedle B → tenis i wille → tunel. W szczególności schody i kolizje progów/drzwi.
6. **Ruch samochodów:** `BRAXHORN_RUCH_TEST` wykazuje 69 zakleszczeń w 600 s (20 aut); `BRAXHORN_TOR_SKRETU` 306 kolizji na 3056 punktach testowych. `BRAXHORN_DROGI_KONTROLA` wykrywa 31 przeszkód. Nie wracać do starych wąskich dróg: sprawdzać kolizje przy narożnikach, skrzyżowaniach, słupkach oraz szerokości realnych modeli.
7. **Patryk i sterowanie:** zgłoszona deformacja barków/rąk/stóp i brak biegu. W oryginalnej scenie `BraxCityBuild.CreateEstate` było `runtime.walkOnly=true`, mimo że generator zawiera `Run`. Nowa gałąź audytu ustawia `walkOnly=false` i weryfikuje wymagane klipy/kości; ocena wyglądu musi nastąpić w rzeczywistym APK.

## Priorytet 1 — naprawić na obszarach lokalnych

- **Oznakowanie jezdni:** `BRAXHORN_DROGI_OZNAKOWANIE etapa=koniec`: 19 par nakładek farby/jezdni (m.in. ok. -63,-15…-19), farba 4 mm–5 cm nad podłożem. Potrzebny pomiar, czy jest z-fighting, i wycięcie do zera — nie maskowanie przez przesuwanie materiałów.
- **Drogi:** docelowo 9.06 jednostek (2 pasy). 4/8 poszerzone do celu, 4/8 częściowo. `ul_wisniowa` i `ul_bydgoska` po 6.9, `B2` 7.9, `B1` 8.65 — ograniczenia obiektami/działkami. Nie ruszać geometrii bez trasy przejazdu i testu z samochodem 6.47 × 2.98.
- **Szpital:** `BRAXHORN_SKALA_KONTROLA drzwi` zgłasza 4 otwory mniejsze od normy wysokości w miejscach `hospital-room-` — potrzeba pomiaru prześwitu kapsuły (wysokość/wielkość bohatera).
- **Wnętrza:** `BRAXHORN_POMIESZCZENIA` 240 braków z 41 pomieszczeń wg heurystyki systemu; wymaga klasyfikacji per lokal — nie należy automatycznie dodawać 240 elementów.
- **Ustawienie elementów:** `BRAXHORN_UMIESZCZENIE` zgłasza 396 poprawek i 93 pozostałe zgłoszenia, w tym 29 zasłoniętych szyldów i 8 lad bez przejścia. Zweryfikować dostępność sklepów i lokali.
- **Skala:** `BRAXHORN_SKALA_RAPORT`: 319 obiektów sprawdzonych, 241 korygowanych, 28 nadal poza normą (m.in. fotele i lady). Nie zmieniać proporcji modeli automatycznie bez porównania.
- **Autobusy:** raport: linia B kierunek wschodni nie ma przystanków (DO POPRAWKI). Połączenie linii A↔B przez park nie jest zbudowane.
- **Sport:** kod `BraxSportBoisko` obsługuje kosz, nożną i tenis (AKCJA, CIOS, KOP). W zakończonym logu piłka nożna ma fallback na boisku koszykarskim (brak wolnego trawnika). Gałąź audytu sprawdza kolejno także mniejsze pola, po czym jawnie zgłasza konflikt, jeśli nadal brak wolnego miejsca. Trzeba potwierdzić pozycję boisk oraz animacje Patryka.

## Priorytet 2 / nie ruszać bez potrzeby

- **Obszary z wyciętym podłożem:** automat zgłosił wycięcie geometrii pod 48 grupami, m.in. kasyno, dyskoteka, sala bokserska, sklepy. To wykonana korekta, ale nie dowód braku migotania.
- **Trawa i drzewa:** w logu 307828 kęp trawy, 524 krzewy, 347 koron; dużą liczbę instancji należy zweryfikować na telefonie pod kątem FPS, overdraw i streaming/LOD. Nie usuwać roślin bez sprawdzenia wpływu na kompozycję.
- **Tunele i sektor południowy:** w starym logu z tunelu wycięto 612 trójkątów terenu kolidującego z wejściem; wymagana próba wejścia/wyjścia i doczytywania osiedla B bez zapadania się gracza.
- **Materiały:** zgłoszenie `leather_red_02` bez tekstury; sprawdzić materiał docelowy i licencję, bez zamiany na losowy low-poly element.
- **Brakujące skrypty:** stary build raportował 0 brakujących skryptów dla `BraxhornEstate` i `BraxhornEstate_Poludnie`. To pozytywny test, nie znosi powyższych problemów.

## Granica potwierdzenia / plan testu

1. Najpierw **jedna** aktualizacja testowa Unity 6 osiedla, z gałęzi naprawczej; nie scalać bez proof.
2. Przed akceptacją: klatki wideo z włączonym/zgaszonym światłem, ruch kamery przy arenie, salonie, dyskotece, fasadach/sklepach i na mostkach; zbliżenia do pary prześwietlonych powierzchni.
3. Sterowanie na telefonie: Patryk idle/walk/run bez deformacji, wsiadanie i jazda sportowym autem, AKCJA+CIOS/KOP na wszystkich trzech boiskach; udane trafienie do kosza, gol, tenis return.
4. Trasa robota + test ręczny kolizji: bez teleportów przez ściany, zapadania się, zablokowania drzwi i zatrzymania samochodów.
5. Raporty: `BRAXHORN_MIGOTANIE`, `BRAXHORN_ARENA_MIGOTANIE`, `BRAXHORN_ROBOT`, `BRAXHORN_DROGI_KONTROLA`, `BRAXHORN_SPORT_QA`; archiwum zdjęć/filmów z rzeczywistego Unity i Androida. **Nie deklarować „naprawiono wszystko” na podstawie PASS kompilacji**.

**Status:** audyt wykonany na podstawie prawdziwego logu wcześniejszego APK i wskazanych nagrań; zidentyfikowane ryzyka nadal do testów nowej wersji. Brak w tym raporcie niepotwierdzonych sukcesów.
