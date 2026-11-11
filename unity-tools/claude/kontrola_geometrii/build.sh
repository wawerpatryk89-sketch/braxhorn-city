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
# oprawki okularów z ulicy (BraxUlicePierzeje.Okulary) — wycinane na żywo do podglądu „chlodnie”
python3 - "$R/Editor/Claude/Region/BraxUlicePierzeje.cs" <<'PY'
import sys
s=open(sys.argv[1],encoding='utf-8').read()
a=s.index('    static Material okularyRama'); b=s.index('\n', s.index('KoloryOprawek ='))
open('gen/Okulary.cs','w',encoding='utf-8').write('using UnityEngine;\ninternal static partial class BraxUlicePierzeje\n{\n'+s[a:b+1]+'    internal static Color32 KolorOprawki(int i) => KoloryOprawek[i % KoloryOprawek.Length];\n}\n')
PY
dotnet build -c Release -o bin -v q -nologo | grep -E " error |Build succeeded" | head -40
if [ -n "$1" ]; then dotnet bin/harness.dll out_$1 $1; else dotnet bin/harness.dll out; fi
