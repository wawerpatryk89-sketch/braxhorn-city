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
a=s.index('    struct ModelOprawki'); b=s.rindex('\n}')
open('gen/Okulary.cs','w',encoding='utf-8').write('using UnityEngine;\ninternal static partial class BraxUlicePierzeje\n{\n'+s[a:b+1]+'}\n')
PY
# napisy 3D (BraxRegionMeble.Napis) i sala muzeum (BraxMuzeum.cs: Sala, Gablota) — wycinane na żywo do podglądu „chlodnie”
python3 - "$R/Editor/Claude/Region" <<'PY'
import sys
d=sys.argv[1]
m=open(d+'/BraxRegionMeble.cs',encoding='utf-8').read()
a=m.index('    static readonly Dictionary<string, (Material lico, Material boki)> napisMat'); b=m.index('\n    }\n', m.index('public static void Napis(Transform parent'))+7
open('gen/Napis.cs','w',encoding='utf-8').write('using System.Collections.Generic;\nusing UnityEngine;\npublic static partial class BraxRegionMeble\n{\n'+m[a:b]+'}\n')
u=open(d+'/BraxMuzeum.cs',encoding='utf-8').read()
a=u.index('    static void Sala('); b=u.rindex('\n}')
open('gen/Muzeum.cs','w',encoding='utf-8').write('using System.Collections.Generic;\nusing UnityEngine;\npublic static partial class BraxRegionMiastoB\n{\n    static float K => BraxClaudeOsiedle.K; static int muzEksponatow;\n    public class Bud { public float x; }\n    public static void PodgladSali(Transform g, float bx, float y0, float xi0, float xi1, float zi0, float zi1, float hS, float zf, float dw, float dh, float gMur) => Sala(g, new Bud { x = bx }, y0, xi0, xi1, zi0, zi1, hS, zf, dw, dh, gMur);\n'+u[a:b+1]+'}\n')
PY
dotnet build -c Release -o bin -v q -nologo | grep -E " error |Build succeeded" | head -40
if [ -n "$1" ]; then dotnet bin/harness.dll out_$1 $1; else dotnet bin/harness.dll out; fi
