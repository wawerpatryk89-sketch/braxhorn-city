# Podgląd kasyna T11 bez Unity

Zasada właściciela z 9.11 (23:49): **renderowane zdjęcia przed każdym buildem**.

`run_preview.sh <katalog roboczy> <node_modules/three>` uruchamia prawdziwy generator
`BraxChampionsCasinoT11` na stubach C# 9 (`../apartments_preview/Stubs.cs`), zrzuca wszystkie siatki
(`MeshDump.cs`: kolor, ambient, metaliczność, szorstkość, emisja każdego materiału), sprawdza nakładające się
płaszczyzny (`zfight.py`) i renderuje widoki z `views.json` z prawdziwymi mapami Poly Haven / ambientCG
i oświetleniem jak `CityStaticPBR` w grze.

Szyldy 3D (`KASYNO WAWER`, `PARKING`) powstają z `build_casino_sign.py` do `CasinoWawerSign.bytes`
(generowane w CI, nigdy nie commitowane). Roślinność GLB i palmy miasta nie są rysowane w podglądzie.
