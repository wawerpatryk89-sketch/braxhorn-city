# Hotel Sobczak T03: kontrola kompilacji i podgląd bez Unity

Narzędzie dla wykonawcy. Nie uruchamia się w CI i nie buduje APK.

1. **Kompilacja.** `BraxChampionsHotelSobczakT03.cs` i `BraxChampionsHotelSobczakMaterials.cs` kompilują się z C# 9 (jak Unity 6) na zaślepkach API Unity (`../apartments_preview/Stubs.cs`, `RegionStubs.cs`, `HotelStubs.cs`). Zaślepki komponentów mają te same pola co prawdziwe skrypty (`BraxWinda`, `BraxDrzwiAutomatyczne`, `BraxHotelSobczakFloorStreamer`).
2. **Generator.** `HotelMain.cs` uruchamia prawdziwy `Generate()`. Zestaw W jest podmieniony na rejestrator ścian, a siatki (szyld 3D, drzwi wind) są przeliczane do metrów nominalnych. Wynik: `hotel_geometry.json` oraz wypis wind, drzwi automatycznych i streamingu pięter.
3. **Obrazy.**
   - `render3d.js`: widoki z buforem głębi (three.js w Chromium przez Playwright);
   - `render.py`: rzuty parteru, I piętra i piętra gościnnego.
   Szablon piętra jest rysowany na wszystkich 26 poziomach, jak 26 instancji prefabu w Unity.

```bash
# dotnet 8 SDK w PATH, python3 + numpy + matplotlib, node + playwright, npm install three@0.160.0
./run_preview.sh /tmp/t03 /ścieżka/do/node_modules/three
```

Tekstury i modele są prawdziwe: mapy Poly Haven 1k (`fetch_preview_textures.py`, API Poly Haven) i modele glTF przypięte MD5 (`prepare_hotel_sobczak_models.py`).
Światło liczone jak shader `CityStaticPBR` w grze: stałe słońce bez cieni, otoczenie `lerp(ziemia, niebo, n.y)` × `_AmbientIntensity`
osobno dla każdego materiału (także ciepłe otoczenie wnętrz), szeroki odblask nieba × 0,4, do 4 lamp `ForceVertex` z tłumieniem Unity,
bez mapowania tonów. Tego samego obrazu należy się spodziewać na telefonie. Kompilacja i odbiór wizualny w Unity są nadal wymagane.
