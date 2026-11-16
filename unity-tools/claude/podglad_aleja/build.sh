#!/bin/bash
# PODGLĄD NOWEJ CZĘŚCI MIASTA R1 (16.11.2026, właściciel 19:02: „bez zdjęcia nic nie akceptuje”): prawdziwy kod gry poza Unity —
# wieżowce z lokalami w parterach (BraxRegionFawelaWieze*.cs), wnętrza z generatora ulicy (BraxUlicePierzeje.cs: bukmacher, deweloper,
# kawiarnia, apteka…), plac ustawek i komis (BraxRegionMiastoBNoweMiejsca.cs). Atrapa UnityEngine z ../kontrola_geometrii/fake.
#   ./build.sh  → out/: geom.bin + geom.json (trójkąty świata z materiałami, modele, tekstury z kodu), migotanie.txt, opis.txt
# Części plików wycinane na żywo (zawsze aktualne): UlSiatka, litery, Bud faweli, rama ściany Fr, Bud mebli, napisy, tablica budynków.
set -e
cd "$(dirname "$0")"
R=../../../unity-city/Assets/Editor/Claude/Region
mkdir -p gen
{ sed -n 1,7p $R/BraxUliceKit.cs; sed -n '/^internal sealed class UlSiatka/,/^}/p' $R/BraxUliceKit.cs; } > gen/UlSiatka.cs
python3 - "$R" <<'PY'
import sys, re
d = sys.argv[1]
def rd(n): return open(d + '/' + n, encoding='utf-8').read()
def blok(s, start):                       # od linii zaczynającej się `start` do domknięcia klamry na tym samym wcięciu
    a = s.index(start); i = s.index('{', a); g = 0
    for j in range(i, len(s)):
        if s[j] == '{': g += 1
        elif s[j] == '}':
            g -= 1
            if g == 0: return s[a:j + 1]
    raise SystemExit('brak domknięcia: ' + start)
def linia(s, start): a = s.index(start); return s[a:s.index('\n', a)]
U = 'using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing UnityEngine;\nusing UnityEngine.Rendering;\nusing UnityEditor;\n'
# litery przestrzenne i napis w teksturze (BraxUliceKit) + pomoc
k = rd('BraxUliceKit.cs')
a = k.index('    // ---------------- litery przestrzenne'); b = k.index('    // ---------------- punkty pod misje')
c = k.index('    // ---------------- pomoc'); e = k.index('\n', k.index('public static readonly Color32 Bialy'))
open('gen/Litery.cs', 'w', encoding='utf-8').write(U + 'internal static partial class BraxUliceKit\n{\n' + k[a:b] + k[c:e + 1] + '}\n')
# Bud faweli, szum, rama ściany Fr, ściana z otworami, bryła w ramie
f = rd('BraxRegionFawela.cs'); dm = rd('BraxRegionFawelaDomy.cs')
fx = [blok(f, '    internal sealed class Bud'), linia(f, '    internal static float S01('), blok(f, '    internal static float Szum('), blok(f, '    static float H2(')]
dx = [blok(dm, '    internal struct Fr'), blok(dm, '    static void Sciana('), blok(dm, '    static void BoxF('), linia(dm, '    static void Pasek(')]
open('gen/Fawela.cs', 'w', encoding='utf-8').write(U + 'public static partial class BraxRegionFawela\n{\n    internal static float K => BraxClaudeOsiedle.K;\n'
    '    static Material T(string nazwa, float metry, Color tint, float szorstkosc, params string[] ids) => BraxRegionMeble.Tex("Fawela: " + nazwa, metry, tint, szorstkosc, ids);\n'
    '    static Material Tm(string nazwa, float metry, Color tint, float szorstkosc, params string[] ids) => BraxRegionMeble.Tex("Fawela: " + nazwa, metry, tint, szorstkosc, ids);\n'
    + '\n'.join(fx + dx) + '\n}\n')
# Bud mebli (BraxRegionMeble) i napis 3D z liter Bebas
m = rd('BraxRegionMeble.cs')
a = m.index('    static readonly Dictionary<string, (Material lico, Material boki)> napisMat'); b = m.index('\n    }\n', m.index('public static void Napis(Transform parent')) + 7
open('gen/Meble.cs', 'w', encoding='utf-8').write(U + 'public static partial class BraxRegionMeble\n{\n' + blok(m, '    public sealed class Bud') + '\n' + m[a:b] + '}\n')
# tablica budynków nowych dzielnic i układ (BraxRegionMiastoBDzielnice)
z = rd('BraxRegionMiastoBDzielnice.cs')
a = z.index('    // ---------------- układ'); b = z.index('    static int ndRootStart;')
open('gen/Dzielnice.cs', 'w', encoding='utf-8').write(U + 'public static partial class BraxRegionMiastoB\n{\n' + z[a:b] + '}\n')
PY
dotnet build -c Release -o bin -v q -nologo | grep -E " error |Build succeeded" | sed -E 's/^.*(podglad_aleja|Region)\///' | sort -u | head -60
dotnet bin/podglad_aleja.dll out "$@"
