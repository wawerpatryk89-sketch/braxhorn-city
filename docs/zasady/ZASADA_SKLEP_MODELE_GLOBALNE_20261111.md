# ZASADA GLOBALNA: modele towaru do sklepów (Tripo właściciela) — 11.11.2026 (właściciel, 22:37–22:50)

> „Robię ci piękne rzeczy 3D w Tripo. Zaraz dostaniesz wszystko do sklepu i zrobisz sklep kodem.” (22:37)
> „Tworzę ciuchy, torebki, buty i robimy to globalnie na całą grę do sklepów, tak? Żeby każdy z tego korzystał. Bo to
> trochę waży jednak.” (22:44)
> „Jak czegoś nie wiesz, chcesz podgląd, jak wygląda, to mi mówisz zawsze, a nie na oko.” (21:57)

Obowiązuje w całej grze, u wszystkich wykonawców.

- **Podział ról.** Właściciel robi w Tripo plansze towaru: ubrania, torebki, buty, sprzęt. Wykonawca tnie je, skaluje, pakuje i rozstawia w sklepach, szafach i pokojach całej gry.
- **Jedna paczka dla wszystkich.** Żadnych kopii w rejonach. Te same modele biorą: galeria Mistrzów, sklepy miasta, szafy w mieszkaniach i hotelach.
- **Jak czegoś nie wiemy (wygląd, fason, kolor), pytamy właściciela o zdjęcie.** Nie robimy na oko.

## Format planszy (najlepszy)

- Jeden model z Tripo, w nim około 10 sztuk ustawionych w rzędach, z odstępami.
- Jedna tekstura (atlas) na całą planszę.
- Osobne plansze dla kategorii: koszulki, koszule, kurtki, spodnie, torebki, buty sportowe, buty eleganckie, sprzęt sportowy.

## Cięcie (`unity-tools/sklep/split_sheet.py`)

Wejście: GLB (z FBX przez FBX2glTF z npm: `fbx2gltf`) i `spec.json` z nazwami sztuk, rzędami, kolumnami i wysokościami w metrach.

Narzędzie robi kolejno:
1. Rzutuje widok z przodu, rozdziela sylwetki i przypisuje każdy kawałek (rękaw, guzik) do jego sztuki.
2. Dla każdej sztuki zapisuje osobny `.glb` w prawdziwych metrach: przód +Z, punkt zawieszenia na górze (ubrania) albo podstawa na dole (półki).
3. Robi wersję `_wieszak` (spłaszczoną przód–tył) do wieszaków.
4. Zapisuje wspólne tekstury w `_tekstury/`: kolor JPEG 2K, normal 2K, metal i szorstkość 1K. Cała plansza dzieli jeden zestaw tekstur.
5. Zapisuje `katalog.json` z nazwą, rodzajem, liczbą trójkątów i wymiarami każdej sztuki.

## Paczka i CI

- Paczka leży w `unity-city/Prywatne/braxhorn-sklep-<nazwa>/`. Jest zaszyfrowana CMS do certyfikatu transferowego CI, jak filmy. Zawiera pocięte modele i oryginał od właściciela.
- W każdym workflow APK, w kroku z `KLUCZ` przed Unity, uruchamia się `python3 unity-tools/sklep/install_sklep_modele.py`. Skrypt rozpakowuje wszystkie paczki do `Assets/Source/Prywatne/Miasto/<target>/`.
- W kodzie: `BraxMiastoAssety.Jest("sklep_ubrania_01")` i `BraxMiastoAssety.Model("sklep_ubrania_01", "ubranie_trencz_bezowy_wieszak")`. Stawiamy przez `Postaw(..., statyczny: false, cienie: false)`.
- Jedna siatka powtarza się na wielu wieszakach bez kopiowania pamięci. Dlatego nie stawiamy ich jako statyczne: statyczne łączenie kopiowałoby siatki.
- Bez klucza albo paczki sklep stawia zastępczy towar z kodu (`BraxKatalog.Garment` itd.).

## Paczki (stan 11.11.2026)

| paczka | target | sztuki |
|---|---|---|
| `braxhorn-sklep-ubrania-01` | `sklep_ubrania_01` | polo granat, henley beż, koszula w kratę, koszula biała, bluza grafit, kardigan beż, kurtka puchowa czarna, trencz beż, chinosy beż, dresy szare (+ wersje `_wieszak`) |
