# AQUAPARK BRAXHORN — Miasto Mistrzów (12.11.2026, noc)

Polecenie właściciela 02:47–02:56: „masz strefę spa, masz zjeżdżalnię, masz wszystko … budujesz basen przez całą noc”, „zrób taki basen,
jak wstanę rano powiem: o kurwa, ty to zrobiłeś”, „wykończenia w detale, wszystko najlepsze”. Wzór: grafika aquaparku z 12.11 00:04
(wieża z bykiem na kamiennej kolumnie, czerwona i żółta rura, trzy niebieskie tory, wodospad ze skał, palmy, basen).

## Działka
Stara rezerwa P1 między drogą X400 a parkiem: x 426–632, z 87–313 (206 × 226 m). Grunt regionu wycięty
(`legacy_foundation.AQUAPARK_SITE`), wszystkie nawierzchnie kładzie generator, równo z chodnikami (0,16).

## Co stoi (`BraxChampionsAquapark*.cs`)
- **Pawilon wejściowy** od Z70 (116 × 32 m): szklany hol pod daszkiem 28 × 8 m, napis 3D AQUAPARK BRAXHORN z bykiem nad wejściem;
  recepcja z kwarcu z LED, ściana z czarnego marmuru z bykiem; bramki; szatnie KOBIETY / MĘŻCZYŹNI (szafki, ławki, 4 prysznice,
  2 umywalki, WC, wyjście na basen); SPA & WELLNESS: hala jacuzzi z 4 wannami właściciela (bąbelki, siadanie), sauna (2 kabiny),
  gabinety masażu A/B (5 stołów, leżenie). Drzwi otwierają się przy podejściu.
- **Laguna** o swobodnym kształcie (ok. 84 × 55 m, 1,30 m) z wyspą z palmami; schody i drabinki ze stali nierdzewnej, światła pod wodą,
  pływanie (pasy `BraxSwimmingZone`).
- **Wieża zjeżdżalni** 10 × 10 m, podesty 4/8/12/16 m, schody dwubiegowe, szklane barierki, dach z oświetleniem, kamienna kolumna
  z bykiem BRAXHORN (stal + zielony LED; złoto z grafiki = światło).
- **Zjeżdżalnie (wszystkie jeżdżą — AKCJA przy bramce):** CZERWONY BYK (rura z 16 m, 2,5 obrotu, 142 m), ŻÓŁTY WIR (rura z 12 m,
  1,5 obrotu, 92 m), NIEBIESKI WYŚCIG (3 tory z 8 m z garbem). Jazda na plecach nogami w dół, prędkość z wysokości, wpadnięcie do laguny.
- **Wodospad** ze skał między rurą a torami.
- **Strefa dzieci** (turkusowa nawierzchnia EPDM): brodzik 35 cm z 2 zjeżdżalniami właściciela i grzybkiem-fontanną, płotek, ławki.
- **2 jacuzzi** zewnętrzne na tekowym pokładzie, **bar przy basenie** (BRAXHORN POOL BAR, hokery), **139 leżaków** (leżenie),
  61 parasoli, prysznice, ratownicy, latarnie, palmy, donice z kwiatami, szklane ogrodzenie z bramą od tramwaju.
- **Leniwa rzeka** (dopisana w nocy 12.11, `BraxChampionsAquaparkRzeka.cs` + `BraxChampionsLeniwaRzeka.cs`): kanał 6,5 m szerokości
  i 1,3 m głębokości dookoła wyspy z palmami i głazami, w miejscu dużego zachodniego trawnika. Ma muszlę basenu, światła pod wodą i pływanie.
  12 kół BRAXHORN (czerwone, żółte, turkusowe, białe) płynie z nurtem przez cały czas. AKCJA przy kole = „WSIĄDŹ DO KOŁA”: Brax siada
  i płynie dookoła wyspy. Joystick albo AKCJA = zejście na brzeg. Trzy łuki wodne z wyspy nad kanałem, schody z poręczami od wschodu
  i od północy, 3 drabinki, tablica LENIWA RZEKA.
- **4 cabany VIP** na wschodnim trawniku (`BraxChampionsAquaparkVip.cs`): podest z tekowych desek, płócienny dach z numerem, zasłony
  związane czerwoną taśmą, łoże dla dwojga (leżenie, 2 miejsca), poduszki, stolik ze stali z kubełkiem i ręcznikami.
- **Byk BRAXHORN jako mozaika na dnie laguny** (granatowe płytki, białe obrysy). Prosto czyta się z tarasu, z wieży widać go w całości.
- **Szum wody** (`BraxZrodloPetli`): wodospad, grzybek, plac wodny, jacuzzi, SPA i łuki rzeki.
- **Spacerowiczki** właściciela (bikini): dwie trasy wokół laguny i brzeg rzeki, tam i z powrotem.

## Kontrola
- Podgląd bez Unity: `unity-tools/mistrzowie/aquapark_preview` (prawdziwy kod generatora + `hotel_sobczak_preview/render3d.js`).
- Każda nawierzchnia sprawdzana polem (`AQUAPARK_TRIANGULATE_AREA`): bez dziur i bez zachodzenia na otwory (zero migotania).
- Test: `unity-tools/mistrzowie/test_aquapark_20261112.py`.
