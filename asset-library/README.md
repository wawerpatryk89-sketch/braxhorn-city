# BRAXHORN — biblioteka źródeł modeli

## Rzeczywisty stan, 29.10.2026

Ta biblioteka znajduje się na gałęzi `codex/city-unity-gym-port-20261029`. Nie dodano nowych assetów do działającej gry i nie zmieniono `main` ani produkcji.

**W repozytorium są już prawdziwe pliki dwóch sof, nie tylko opisy:**

- [Sofa 03 — BLEND + 4 tekstury 4K](sources/sofa_03/)
- [Sofa 02 — BLEND + 4 tekstury 4K](sources/sofa_02/)

Łącznie 10 plików źródłowych, 33 717 193 bajty. Pliki pobrano z oficjalnego Poly Haven i porównano SHA-256 każdego pliku z zawartością archiwów przesłanych przez właściciela. Wszystkie 10 sum jest identycznych. Oryginalnych opakowań ZIP nie duplikowano.

Dowód: [raport sum kontrolnych](sofas-import-receipt.json) oraz [zakończony import źródeł](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/40790764684).

**Pozostałe paczki są skatalogowane, ale ich oryginały nie zostały zapisane w tym repozytorium.** Nie przedstawiać katalogu jako potwierdzonego uploadu wszystkich modeli.

## Katalogi dostaw

- [Pierwsze 10 plików — katalog](catalog-20261029.json): 481 721 572 bajty, metadane i sumy kontrolne; źródła tej pierwszej dostawy nie zostały wgrane.
- [Kolejne 9 plików — zawartość, statusy i duplikaty](intake-20261029-02.json): 328 406 418 bajtów otrzymanych załączników. Dwie sofy mają zapisane źródła; reszta oczekuje na odpowiednie przechowywanie lub potwierdzenie licencji.

Są to liczby dostarczonych plików, nie unikatowych zestawów modeli.

## Druga dostawa: kontrola zawartości

| Materiał | Potwierdzona zawartość | Stan |
|---|---|---|
| Sofa 02 / Sofa 03 | Po 1 BLEND i 4 tekstury; ZIP CRC poprawne | Źródła zapisane; nie importowano jeszcze do Unity |
| VNB Gaming, Unity | 124 prefabrykaty, 25 materiałów, 29 PNG i 1 FBX | Katalog; oczekuje na prywatny magazyn |
| VNB Gaming, ZIP | 132 FBX, 131 OBJ; wliczony jeden zbiorczy FBX | Katalog; alternatywny format tej samej kolekcji |
| VNB Home, ZIP | 347 FBX, 347 OBJ; wliczony jeden zbiorczy FBX | Katalog; wersja 1.1 według README |
| VNB - Home Set.unitypackage | 238 prefabrykatów, 31 materiałów, 33 PNG i 1 FBX | Katalog; autor oznacza tę nazwę jako starszą wersję |
| ModularBuilding 3.zip | 57 FBX, 2 BLEND, 1 unitypackage, 14 PNG, 2 EXR | Pełny zestaw źródeł; katalog, bez uploadu archiwum |
| ProceduralWallFile.blend | Plik Blender 2.93, 40 013 052 bajty | Identyczny plik znajduje się w ModularBuilding 3.zip |
| Modular Building 3(1).unitypackage | SHA-256 identyczne jak poprzednio dostarczony pakiet | Duplikat; ta sama kopia jest też w ModularBuilding 3.zip |

Plików BLEND ani osadzonego kodu nie uruchamiano. Sprawdzenie archiwum i sum kontrolnych nie jest testem wyglądu lub importu w Unity.

## Licencje i zakres publikacji

Repozytorium `braxhorn-city` jest publiczne. Publikacja samych modeli jest czymś innym niż wykorzystanie ich we własnej grze.

- **Sofa 03:** CC0, autor Fran Calvente, https://polyhaven.com/a/sofa_03 .
- **Sofa 02:** CC0, autor Kirill Sannikov, https://polyhaven.com/a/sofa_02 .
- **VNB Gaming i Home:** strony autora pozwalają na użycie w projektach, ale wyraźnie zakazują publikacji jego pracy osobno lub jako paczki assetów. Jednocześnie pole licencji na stronie podaje CC BY 4.0; nie rozstrzygamy tej sprzeczności na korzyść publicznej redystrybucji. Źródła wymagają prywatnego przechowywania albo wyjaśnienia licencji z autorem. Źródła: https://vnbp.itch.io/low-poly-3d-gaming-set-vnb oraz https://vnbp.itch.io/low-poly-home-set .
- **Modular Building 3:** README i strona autora podają dosłownie „Creative Commons Attribution 1.0 International License”. Zapis wymaga doprecyzowania; nie oznaczamy paczki jako CC0. https://loafbrr.itch.io/modular-building-3 .
- Informacje o pierwszej dostawie pozostają w jej katalogu: fotel Poly Haven, garaże, siłownie, kuchnie, pojazdy i pozostałe zestawy. Brak potwierdzenia licencji nie oznacza zgody na publiczne udostępnianie źródeł.

Modele VNB są celowo low-poly. Zachować jako bibliotekę pomocniczą; nie podmieniać nimi bez zgody docelowej oprawy premium.

## Unity — potwierdzony wynik testu siłowni

[Uruchomienie 40788363680](https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/40788363680) zakończyło się `success`: import rzeczywistego GLB, kompilacja WebGL i zapis paczki testowej. Pobrana paczka zawiera index.html, WebGL.data, WebGL.wasm, loader, framework i qa.json.

`qa.json` wskazuje Unity `6000.0.58f2` i cel `imported-original-gym-only-not-a-playable-game`.

To wynik techniczny, nie odebrana wizualnie ani grywalna migracja. Nie ma potwierdzenia testu mobilnego, nie opublikowano gry i nie przeniesiono całego miasta ani osiedla.

## Zasady dalszej pracy

Zachować oryginały oraz identyfikację wersji. Import i optymalizację wykonywać na kopiach. Nie wykonywać skryptów z pobranych paczek bez kontroli. Nie podmieniać modeli, wyglądu ani produkcji bez polecenia właściciela. Samo zapisanie źródła do biblioteki nie oznacza dodania go do sceny gry.
