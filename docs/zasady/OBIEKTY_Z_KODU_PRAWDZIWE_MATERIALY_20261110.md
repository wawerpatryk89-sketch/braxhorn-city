# ZASADA GLOBALNA: obiekty z kodu tylko z prawdziwych materiałów (właściciel, 10.11.2026)

Właściciel 10.11, 10:27: „Zapisz to globalnie. Dla wszystkich. Jeżeli tworzymy coś przez kod, meble czy coś, okej, ja pozwalam, ale wszystko ma wyglądać. Co ma być drewnem, ma być drewnem, ma być obite skórą pikowaną czy czymś, materiałem. Wtedy to wygląda.”
10:28: „Lustro ma być lustrem, bez żadnych zjebanych detali.”

Dotyczy **wszystkich rejonów i wszystkich wykonawców**. Uzupełnia zasadę tanich napisów (`NAPISY_PREMIUM_ZAKAZ_TANICH_20261110.md`).

## Reguła
Obiekty zbudowane w kodzie są dozwolone, np. meble, budki, ławki, leżaki, lady, regały, parasole, szyldy, balustrady. Każda ich część ma jednak wyglądać jak prawdziwy materiał, z którego byłaby zrobiona:

| Część | Musi być | Zakazane |
|---|---|---|
| drewno (blaty, listwy, deski, podłogi, siedziska) | tekstura PBR drewna: albedo + normal + ARM, słoje w kierunku deski, skala w metrach | gładki brązowy kolor |
| tapicerka (sofy, fotele, krzesła barowe, ławy w lożach) | skóra pikowana: geometria pikowania lub normal map, guziki, szwy; albo tkanina z teksturą splotu | płaski prostopadłościan w jednym kolorze |
| tkanina (parasole, markizy, materace, poduszki, zasłony) | tekstura tkaniny ze splotem, lamówka, szew; czasza z żebrami | płaska „biała płachta” bez struktury |
| metal (stal, mosiądz, chrom, grafit malowany proszkowo) | metaliczność i chropowatość jak w prawdziwym materiale, szczotkowanie albo połysk | kolor bez metaliczności |
| kamień, marmur, beton | tekstura PBR z żyłkowaniem lub porami | jednolity szary lub biały |
| szkło | przezroczystość, odbicie, krawędź profilu | nieprzezroczysty kolorowy prostokąt |
| lustro | prawdziwe odbicie: reflection probe przy lustrze (box projection) albo odbicie planarne w ważnych miejscach, metal 1 / gładkość ~0,98, rama lub szlif krawędzi | szara albo srebrna płyta, „lustro” z koloru, tekstura udająca odbicie |

Dodatkowo:
1. **Proporcje i detale jak w rzeczywistości.** Krzesło ma siedzisko na 0,45 m, oparcie, nogi i stopki. Ława ma ramę i listwy. Sofa ma poduchy, a nie jeden klocek.
2. **Kolory bez „sraczkowatych” brązów i beży** (właściciel). Drewno: dąb naturalny, srebrny teak, orzech ciemny. Złoto tylko tam, gdzie należy do marki.
3. **Tekstury tylko z legalnych źródeł:** Poly Haven / ambientCG (CC0) albo paczki właściciela. Na Androida 2K, z bliska 4K (standard Unity 6).
4. **Zdjęcie przed buildem.** Każdy nowy mebel lub obiekt z kodu trzeba pokazać na renderze z bliska. Płaska powierzchnia bez tekstury na zdjęciu to błąd do poprawki przed wysyłką.

## Wdrożenie w Mieście Mistrzów
Przegląd od 10.11. Promenada, budki, plaża, port: wszystkie materiały bez tekstury, tam gdzie powinna być, zamienione na PBR. Lista zmian jest w `claude/braxhorn-city-MISTRZOWIE-PROMENADA-PORT-STAN-10-11.md`.
