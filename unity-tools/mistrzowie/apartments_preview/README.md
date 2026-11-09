# Apartamentowce T04–T07: kontrola kompilacji i podgląd bez Unity

Narzędzie dla wykonawcy. Nie uruchamia się w CI i nie buduje APK.

1. **Kompilacja.** `BraxChampionsApartments.cs` kompiluje się z C# 9 (jak Unity 6) na zaślepkach API Unity (`Stubs.cs`). Błąd składni albo typu wychodzi tu, a nie dopiero po godzinie budowania w Unity.
2. **Podgląd.** `Main.cs` uruchamia generator każdego budynku. Zestaw W (BraxWnetrzaKit) jest podmieniony na rejestrator ścian. Wynik trafia do `T0x_geometry.json`.
3. **Obrazy.** `render.py` rysuje z tych danych elewację 3/4 i rzut piętra:
   - płaskie kolory, bez tekstur i światła;
   - układ współrzędnych lewoskrętny, jak w Unity.

```bash
# .NET 8 SDK (np. z builds.dotnet.microsoft.com) w PATH jako dotnet
cd unity-tools/mistrzowie/apartments_preview
dotnet build -v q && dotnet run --no-build      # APT_BUILDING ... + T0x_geometry.json
python3 render.py T04                           # T04_front34.png, T04_rear34.png, T04_plan_*.png
```

Zaślepki odwzorowują tylko to, czego używa generator. Kompilacja w Unity nadal jest wymagana, ale błąd składni wychodzi tu w kilka sekund.
