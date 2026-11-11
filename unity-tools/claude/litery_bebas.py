#!/usr/bin/env python3
"""LITERY 3D Z FONTU (MIASTO R1, 11.11.2026): kontury glifów Bebas Neue -> dane C# dla liter przestrzennych.

Właściciel 11.11 14:43: „ten napis BRAXHORN GYM ma zrobić ładny w 3D”. Zasada napisów premium (10.11): litery 3D z pełnych
brył (front, boki, tył), font premium z licencją w repozytorium — tu Bebas Neue (Dharma Type, SIL OFL 1.1,
unity-tools/claude/fonts/BebasNeue-OFL.txt), krój najbliższy płaskiemu szyldowi siłowni (wąska, gruba antykwa bezszeryfowa).

Skrypt czyta font (fontTools), spłaszcza krzywe kwadratowe, porządkuje kontury (zewnętrzne przeciwnie do wskazówek zegara,
otwory zgodnie), łączy otwory z konturem, który je zawiera, i trójkątuje lico (earcut). Jednostki: wysokość wersalika = 1.
Wynik: Assets/Editor/Claude/Region/BraxLiteryBebasDane.cs (wygenerowany, SHA-256 fontu w nagłówku) + opcjonalnie JSON
(podgląd three.js). Bryłę z tych danych składa BraxLiteryBebas.cs (lico, tył, boki).

Użycie:  python3 unity-tools/claude/litery_bebas.py [--json podglad.json]
Wymaga:  pip install fonttools mapbox_earcut
"""
import argparse, hashlib, json, math, os, sys

import numpy as np
import mapbox_earcut as earcut
from fontTools.pens.basePen import BasePen
from fontTools.ttLib import TTFont

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
FONT = os.path.join(REPO, 'unity-tools', 'claude', 'fonts', 'BebasNeue-Regular.ttf')
OUT_CS = os.path.join(REPO, 'unity-city', 'Assets', 'Editor', 'Claude', 'Region', 'BraxLiteryBebasDane.cs')
ZNAKI = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ĄĆĘŁŃÓŚŹŻ-.·&+:/"   # 11.11: + : / (K+R, godziny, kierunki dworca)
KROK = .035          # maks. długość odcinka krzywej (w wysokościach wersalika)


class Kontury(BasePen):
    def __init__(self, glyphset):
        super().__init__(glyphset)
        self.kontury, self.akt = [], None

    def _moveTo(self, p):
        self.akt = [p]

    def _lineTo(self, p):
        self.akt.append(p)

    def _qCurveToOne(self, c, p):
        a = self.akt[-1]
        n = max(2, min(12, math.ceil((math.dist(a, c) + math.dist(c, p)) / KROK_F)))
        for i in range(1, n + 1):
            t = i / n
            self.akt.append(((1 - t) ** 2 * a[0] + 2 * (1 - t) * t * c[0] + t * t * p[0],
                             (1 - t) ** 2 * a[1] + 2 * (1 - t) * t * c[1] + t * t * p[1]))

    def _curveToOne(self, c1, c2, p):
        a = self.akt[-1]
        n = max(2, min(16, math.ceil((math.dist(a, c1) + math.dist(c1, c2) + math.dist(c2, p)) / KROK_F)))
        for i in range(1, n + 1):
            t = i / n; u = 1 - t
            self.akt.append((u ** 3 * a[0] + 3 * u * u * t * c1[0] + 3 * u * t * t * c2[0] + t ** 3 * p[0],
                             u ** 3 * a[1] + 3 * u * u * t * c1[1] + 3 * u * t * t * c2[1] + t ** 3 * p[1]))

    def _closePath(self):
        k = self.akt
        if len(k) > 1 and math.dist(k[0], k[-1]) < 1e-6:
            k = k[:-1]
        # odcinki zerowej długości precz
        czyste = [k[0]]
        for p in k[1:]:
            if math.dist(p, czyste[-1]) > 1e-6:
                czyste.append(p)
        if len(czyste) >= 3:
            self.kontury.append(czyste)
        self.akt = None

    _endPath = _closePath


def pole(k):
    return .5 * sum(k[i][0] * k[(i + 1) % len(k)][1] - k[(i + 1) % len(k)][0] * k[i][1] for i in range(len(k)))


def wewnatrz(p, k):
    x, y = p; w = False
    for i in range(len(k)):
        x1, y1 = k[i]; x2, y2 = k[(i + 1) % len(k)]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / (y2 - y1) + x1:
            w = not w
    return w


def glif(font, gs, cmap, ch, cap):
    g = cmap[ord(ch)]
    pen = Kontury(gs)
    gs[g].draw(pen)
    adv = font['hmtx'][g][0] / cap
    kont = [[(x / cap, y / cap) for x, y in k] for k in pen.kontury]
    # zagnieżdżenie: otwór = kontur wewnątrz nieparzystej liczby innych
    gleb = []
    for i, k in enumerate(kont):
        gleb.append(sum(1 for j, m in enumerate(kont) if j != i and wewnatrz(k[0], m)))
    zew = [i for i in range(len(kont)) if gleb[i] % 2 == 0]
    grupy = {i: [] for i in zew}
    for i in range(len(kont)):
        if gleb[i] % 2 == 1:
            # najmniejszy zawierający kontur zewnętrzny
            kand = [j for j in zew if wewnatrz(kont[i][0], kont[j])]
            j = min(kand, key=lambda j: abs(pole(kont[j])))
            grupy[j].append(i)
    pts, starty, tri = [], [], []
    for z in zew:
        ring = []
        for idx, czy_otwor in [(z, False)] + [(h, True) for h in grupy[z]]:
            k = kont[idx]
            a = pole(k)
            if (a > 0) == czy_otwor:      # zewnętrzny CCW (pole > 0), otwór CW
                k = k[::-1]
            ring.append(k)
        baza = len(pts)
        konce, n = [], 0
        for k in ring:
            starty.append(len(pts)); pts.extend(k); n += len(k); konce.append(n)
        v = np.array([p for k in ring for p in k], dtype=np.float64).reshape(-1, 2)
        t = earcut.triangulate_float64(v, np.array(konce, dtype=np.uint32))
        tri.extend(int(i) + baza for i in t)
    # kontrola: suma pól trójkątów = pole litery
    P = pts
    s_tri = sum(abs((P[tri[i + 1]][0] - P[tri[i]][0]) * (P[tri[i + 2]][1] - P[tri[i]][1]) - (P[tri[i + 2]][0] - P[tri[i]][0]) * (P[tri[i + 1]][1] - P[tri[i]][1])) * .5 for i in range(0, len(tri), 3))
    s_kont = sum(pole(kont[z]) for z in zew) and (sum(abs(pole(kont[z])) for z in zew) - sum(abs(pole(kont[h])) for z in zew for h in grupy[z]))
    if abs(s_tri - s_kont) > 1e-3 * max(1.0, s_kont):
        raise SystemExit(f'{ch}: trójkątowanie niezgodne z polem ({s_tri:.4f} vs {s_kont:.4f})')
    return dict(adv=adv, pts=pts, starty=starty, tri=tri)


def kerning(font, cmap, cap):
    """Pary kerningu GPOS (PairPos format 1 i 2) dla znaków ZNAKI."""
    znaki = {cmap[ord(c)]: c for c in ZNAKI if ord(c) in cmap}
    wynik = {}
    if 'GPOS' not in font:
        return wynik
    gpos = font['GPOS'].table
    for lk in gpos.LookupList.Lookup:
        for st in lk.SubTable:
            if lk.LookupType == 9:
                st = st.ExtSubTable
            if getattr(st, 'LookupType', lk.LookupType) != 2:
                continue
            pierwsze = st.Coverage.glyphs
            if st.Format == 1:
                for i, g1 in enumerate(pierwsze):
                    if g1 not in znaki:
                        continue
                    for pv in st.PairSet[i].PairValueRecord:
                        if pv.SecondGlyph in znaki and pv.Value1 and getattr(pv.Value1, 'XAdvance', 0):
                            wynik.setdefault(znaki[g1] + znaki[pv.SecondGlyph], pv.Value1.XAdvance / cap)
            elif st.Format == 2:
                k1 = st.ClassDef1.classDefs if st.ClassDef1 else {}
                k2 = st.ClassDef2.classDefs if st.ClassDef2 else {}
                for g1 in pierwsze:
                    if g1 not in znaki:
                        continue
                    c1 = k1.get(g1, 0)
                    for g2, ch2 in znaki.items():
                        c2 = k2.get(g2, 0)
                        v = st.Class1Record[c1].Class2Record[c2].Value1
                        if v and getattr(v, 'XAdvance', 0):
                            wynik.setdefault(znaki[g1] + ch2, v.XAdvance / cap)
    return {k: v for k, v in wynik.items() if abs(v) > 1e-4}


def cs_float(a):
    return ', '.join(f'{v:.4f}f'.replace('-0.0000f', '0f') for v in a)


def main():
    global KROK_F
    ap = argparse.ArgumentParser()
    ap.add_argument('--json', help='zapis danych do podglądu (three.js)')
    a = ap.parse_args()
    font = TTFont(FONT)
    cap = font['OS/2'].sCapHeight
    KROK_F = KROK * cap
    gs = font.getGlyphSet(); cmap = font.getBestCmap()
    sha = hashlib.sha256(open(FONT, 'rb').read()).hexdigest()
    spacja = font['hmtx'][cmap[32]][0] / cap
    dane = {}
    for ch in ZNAKI:
        if ord(ch) in cmap:
            dane[ch] = glif(font, gs, cmap, ch, cap)
    kern = kerning(font, cmap, cap)
    linie = [
        '// WYGENEROWANE — nie edytować ręcznie. unity-tools/claude/litery_bebas.py (MIASTO R1, 11.11.2026).',
        f'// Font: Bebas Neue Regular (Dharma Type, SIL OFL 1.1, unity-tools/claude/fonts/), SHA-256 {sha}.',
        '// Jednostki: wysokość wersalika = 1; kontury zewnętrzne przeciwnie do wskazówek zegara, otwory zgodnie; tri = lico.',
        'using System.Collections.Generic;',
        '',
        'internal static partial class BraxLiteryBebas',
        '{',
        f'    public const string FontSha256 = "{sha}";',
        f'    const float Spacja = {spacja:.4f}f;',
        '    static readonly Dictionary<char, (float adv, float[] pts, int[] starty, int[] tri)> Glify = new Dictionary<char, (float, float[], int[], int[])>',
        '    {',
    ]
    for ch, d in dane.items():
        flat = [c for p in d['pts'] for c in p]
        linie.append(f"        ['{ch}'] = ({d['adv']:.4f}f, new[] {{ {cs_float(flat)} }},")
        linie.append(f"               new[] {{ {', '.join(map(str, d['starty']))} }}, new[] {{ {', '.join(map(str, d['tri']))} }}),")
    linie += ['    };',
              '    static readonly Dictionary<string, float> Kerning = new Dictionary<string, float>',
              '    {']
    for k, v in sorted(kern.items()):
        linie.append(f'        ["{k}"] = {v:.4f}f,')
    linie += ['    };', '}', '']
    with open(OUT_CS, 'w', encoding='utf-8') as f:
        f.write('\n'.join(linie))
    print(f'{OUT_CS}: znaków {len(dane)}, par kerningu {len(kern)}, punktów {sum(len(d["pts"]) for d in dane.values())}, trójkątów {sum(len(d["tri"]) // 3 for d in dane.values())}')
    if a.json:
        json.dump(dict(sha=sha, spacja=spacja, glify=dane, kerning=kern), open(a.json, 'w'))


if __name__ == '__main__':
    main()
