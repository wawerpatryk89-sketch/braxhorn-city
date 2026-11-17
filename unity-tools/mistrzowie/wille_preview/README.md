# Podgląd willi na sprzedaż bez Unity

Zasada właściciela z 9.11 (23:49): **renderowane zdjęcia przed każdym buildem**.

`run_preview.sh <katalog roboczy> <node_modules/three> [nr willi 1..3]` uruchamia prawdziwy generator
`BraxChampionsWille` na stubach C# 9 (`../apartments_preview/Stubs.cs`), zrzuca siatki (`MeshDump.cs`),
sprawdza nakładające się płaszczyzny (`zfight.py`) i renderuje widoki z `views.json` (lot, taras z basenem,
salon, kuchnia, tył z garażem, sypialnia, łazienka, schody) rendererem hotelu. Modele Poly Haven brane są z
`<katalog roboczy>/Assets/Source/Mistrzowie` (przygotowane przez `prepare_assets.py`); palmy miasta nie są
rysowane w podglądzie. Ziemia podglądu leży 4,5 m niżej, żeby było widać basen infinity.
