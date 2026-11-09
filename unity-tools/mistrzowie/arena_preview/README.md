# Podgląd areny A1 bez Unity

Zasada właściciela z 9.11 (23:49): **renderowane zdjęcia przed każdym buildem**.

`run_preview.sh <katalog roboczy> <node_modules/three>` uruchamia prawdziwy `BraxChampionsBuildings.Generate()`
(bryła z `Exteriors.bytes`, hala, lobby, szyldy, trzy murale, gablota pasa) na stubach C# 9 i rysuje widoki
z `views.json` w metrach budynku (środek hali 0,0; wejście publiczne od strony -Z), z prawdziwymi mapami
i oświetleniem jak `CityStaticPBR` w grze.

Grafik właściciela nie ma w repozytorium. Przed uruchomieniem trzeba włożyć do
`<katalog roboczy>/Assets/Source/Mistrzowie/`:

- `ArenaMuralsHD/MURAL_*_6144x2048.png` i uruchomić na tym folderze `split_arena_murals.py` (kafle 2048),
- `ArenaSigns/champion_arena_*.png`.

Czego podgląd NIE rysuje: palm miasta (zaszyfrowana paczka właściciela, dostępna dopiero przy buildzie),
pasa 3D (FBX), mebli lobby i foteli VIP (modele dociągane przy buildzie). Zastępczych kształtów nie wstawiamy.
