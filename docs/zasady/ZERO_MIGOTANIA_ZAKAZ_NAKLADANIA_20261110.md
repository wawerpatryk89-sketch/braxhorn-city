# ZASADA GLOBALNA: zero migotania, zakaz nakładania (właściciel, 10.11.2026)

Właściciel 10.11, 13:04–13:06, wideo IMG_0866 (krawężniki i powierzchnie migoczą):
> „Globalnie wszystko sprawdzasz. Każde migotanie, krawężniki migotają, wszystko ma zniknąć, co niepotrzebne. A nie nakładanie jednej rzeczy na drugą, żeby iść na łatwość.”
> „Zakaz nakładania czegoś na cokolwiek. Globalnie. I wszystko poprawiasz, co migota.”
> „Zapisz to globalnie: zakaz nakładania tekstury na teksturę. A jeżeli gdzieś tekstura jest nałożona na teksturę, ma być zdjęte to, co niepotrzebne. Zero migotania czegokolwiek.”

Zasada dotyczy **wszystkich rejonów i wszystkich wykonawców**.

## Reguła
1. **Żadna powierzchnia nie leży na innej.** Dwie ściany, podłogi, płyty, krawężniki ani pasy nie mogą być w tej samej płaszczyźnie ani 1–5 mm nad sobą (np. nakładka, „druga warstwa”, przesunięcie głębi). Każdy kawałek terenu ma dokładnie jedną powierzchnię.
2. **Nowy materiał zastępuje stary.** Jeśli coś ma wyglądać inaczej, zmienia się materiał tej powierzchni albo wycina się ją i buduje nową w tym samym miejscu. Nie kładzie się nowej płyty na starej.
3. **Detale (pasy, linie LED, listwy, napisy) mają prawdziwą grubość** i stoją na krawędzi albo w wyciętym miejscu. Nie leżą płasko na innej powierzchni.
4. **To, co niepotrzebne, jest usuwane.** Jeżeli gdzieś już leżą dwie warstwy, zostaje ta właściwa, a druga znika z generatora.
5. **Kontrola przed buildem:** test geometrii (współpłaszczyznowe trójkąty z różnymi materiałami, odstęp < 2 cm) musi dać 0. Render z bliska krawężników, przejść, pasów i ścian bez migotania.

## Powiązane zasady z 10.11
- Napisy tylko w 3D, zakaz tanich napisów: `NAPISY_PREMIUM_ZAKAZ_TANICH_20261110.md`. Napisy TextMesh z czcionką Unity prześwitywały przez ściany jako „rysa” na wysokości oczu.
- Obiekty z kodu tylko z prawdziwych materiałów: `OBIEKTY_Z_KODU_PRAWDZIWE_MATERIALY_20261110.md`.
- **Drzwi w każdym budynku** (12:42–12:55): ma być widać, że to drzwi. Skrzydła mają ramę, uchwyty i próg, wokół jest czarny portal. Wszystkie drzwi są zamknięte i otwierają się przy podejściu (BraxCityDoor).
- **Szyby** (12:28, 12:50): zakaz szarego szkła. Jasne są tylko skrzydła drzwi, a szyby wokół drzwi i w elewacjach są czarne. Ramy czarne.

## Przebieg kontrolny 10.11 (po zapisaniu zasady)
- **Krawężniki miasta** (`build_infrastructure.py`): wierzch każdego krawężnika leżał w płaszczyźnie chodnika na tym samym pasie 18 cm, czyli **5091 m²** migotania. Krawężnik ma teraz własny pas wycięty ze wszystkich powierzchni na poziomie chodnika. Obniżone krawężniki mają dokładnie 5 m rampy, a rampa zaczyna się za kamieniem i ma boki. Po zmianie: 0 m².
- **Arena** (wnętrze + elewacja + LOD0): liczba par spadła ze 147 do 87. Zostały tylko spody leżące na posadzce (niewidoczne) i styki krawędzi poniżej 0,05 m². Poprawione: listwy schodów, złote krawędzie przejść, chodnik na schodach, dywany (płyty 12 mm), szatnie, drzwi wejściowe, cokoły flag, korona pod dachem, LED nad daszkiem, gablota, ławki i stopka dachu w LOD0.
- **Promenada i plaża**: kładki (deski wchodziły w piasek), cokoły budek, deski SUP, krzesła, ławki, latarnie, LED-y bram, sprzęt w budkach.
- **Narzędzie** `apartments_preview/zfight.py` mierzy odstęp płaszczyzn lokalnie. Wcześniej `n·v` przy współrzędnych rzędu 1 km dawało fałszywe pary na skośnych ścianach. Siatki modeli trzeba najpierw przepuścić przez `casino_preview/mesh2quads.py`, bo inaczej budynek złożony tylko z siatek daje fałszywe „0 par”.
