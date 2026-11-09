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
./run_preview.sh /tmp/t03 /ścieżka/do/node_modules/three/build/three.min.js
```

Kolory są płaskie, bez tekstur Poly Haven i bez świateł Unity. Kompilacja i odbiór wizualny w Unity są nadal wymagane.
