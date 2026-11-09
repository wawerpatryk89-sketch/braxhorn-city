# Apartamentowce T04–T07: kontrola kompilacji i podgląd bez Unity

Narzędzie dla wykonawcy. Nie uruchamia się w CI i nie buduje APK.

1. **Kompilacja.** `BraxChampionsApartments.cs` kompiluje się z C# 9 (jak Unity 6) na zaślepkach API Unity (`Stubs.cs`). Błąd składni albo typu wychodzi tu, a nie dopiero po godzinie budowania w Unity.
2. **Podgląd.** `Main.cs` uruchamia generator każdego budynku. Zestaw W (BraxWnetrzaKit) jest podmieniony na rejestrator ścian. Wynik trafia do `T0x_geometry.json`.
3. **Obrazy.** `run_preview.sh` renderuje każdy budynek tym samym rendererem co hotel (`../hotel_sobczak_preview/render3d.js`): prawdziwe mapy Poly Haven (i marmur ambientCG Marble016), modele glTF, światło liczone jak shader `CityStaticPBR` w grze. Ujęcia: `views.py` (elewacje, narożnik, wejście, lobby, skrzynki, korytarz, schody, salon, sypialnia, łazienka).
4. **Miganie (z-fighting).** `zfight.py` szuka ścianek zwróconych w tę samą stronę, w tej samej płaszczyźnie (na zewnątrz < 6 mm, wewnątrz < 1,5 mm), nakładających się, z różnymi materiałami. Ścianki dociśnięte do innej bryły (niewidoczne) są pomijane. Tak znaleziono migający czarny pas na narożniku (film właściciela IMG_0786).

```bash
# .NET 8 SDK, python3 + numpy + shapely + Pillow, node + playwright, npm install three@0.160.0
./run_preview.sh /tmp/apt /ścieżka/do/node_modules/three T04 T05 T06 T07
```

`render.py` (stary, płaskie kolory) zostaje do rzutów pięter. Zaślepki odwzorowują tylko to, czego używa generator. Kompilacja i odbiór w Unity są nadal wymagane.
