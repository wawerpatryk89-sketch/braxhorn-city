#!/bin/bash
# KONTROLA GEOMETRII R1 (MIASTO R1, 11.11.2026): buduje narzędzie i uruchamia generator dworca poza Unity.
#   ./build.sh            → wynik w out/: migotanie.txt (pary powierzchni < 2 cm z różnych materiałów — ma być 0), opis.txt (log generatora),
#                           geom.bin + geom.json (trójkąty świata z materiałami — do podglądu)
#   ./build.sh otwarte    → to samo z otwartymi drzwiami windy A i WC (kontrola części ruchomych)
# Klasa siatek UlSiatka jest wycinana na żywo z BraxUliceKit.cs (zawsze aktualna). Wymaga .NET 8 SDK.
set -e
cd "$(dirname "$0")"
R=../../../unity-city/Assets
mkdir -p gen
{ sed -n 1,7p $R/Editor/Claude/Region/BraxUliceKit.cs; sed -n '/^internal sealed class UlSiatka/,/^}/p' $R/Editor/Claude/Region/BraxUliceKit.cs; } > gen/UlSiatka.cs
dotnet build -c Release -o bin -v q -nologo | grep -E " error |Build succeeded" | head -40
if [ -n "$1" ]; then dotnet bin/harness.dll out_$1 $1; else dotnet bin/harness.dll out; fi
