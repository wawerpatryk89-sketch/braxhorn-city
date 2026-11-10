#!/usr/bin/env python3
"""Region letter kit: one solid 3D glyph per character, for every short text the generators compose in Unity
(door and room numbers, transit stop names, arena wayfinding, the vitrine plaque).

Owner 10.11 10:17-10:23: "Globalnie zakaz tanich napisów" - no TextMesh / bitmap font anywhere in the region
(docs/zasady/NAPISY_PREMIUM_ZAKAZ_TANICH_20261110.md). Same typeface as the harbour signs (Montserrat SemiBold,
SIL OFL 1.1, fonts/), same solid bevelled glyphs (build_signs.Geometry), mirrored for Unity (faces toward +Z).

Units: cap height 1, depth 1 (BraxChampionsLetters3D scales x/y by the cap height and z by the letter depth);
'adv' = advance width in cap units; glyph x runs from -adv to 0 after the mirror (the C# layout accounts for it).
Written to LetterKit.bytes (gzip JSON), generated in CI, never committed.
"""
import gzip
import json

from shapely.affinity import scale

from build_signs import SRC, Geometry
from build_sobczak_sign import mirror_for_unity
from build_harbour_signs import CAP, FPS, _TT, CMAPS, FONT_FILES, _advance, _font, symmetric
from matplotlib.textpath import TextPath
from shapely.geometry import Polygon

CHARSET = ('ABCDEFGHIJKLMNOPQRSTUVWXYZ' 'ĄĆĘŁŃÓŚŹŻÉÜÖ' '0123456789' "-/•.,:&'()!+#")
BEVEL = .02          # in cap units: 2 mm on a 10 cm number


def glyph(c):
    i = _font(c)
    path = TextPath((0, 0), c, size=100, prop=FPS[i])
    rings = [Polygon(q) for q in path.to_polygons() if len(q) > 3]
    adv = _advance(i, c) / CAP
    if not rings:
        return None, adv
    g = symmetric(rings).buffer(0)
    g = scale(g, xfact=1 / CAP, yfact=1 / CAP, origin=(0, 0))
    geo = Geometry()
    geo.solid(g, 0, 1.0, front='face', side='dark', bevel=BEVEL)
    meshes = []
    for role, m in geo.roles.items():
        meshes.append(dict(role='face' if role == 'gold' else role,
                           positions=[round(v, 5) for v in m['positions']],
                           triangles=m['triangles']))
    sign = mirror_for_unity(dict(meshes=meshes))
    return sign['meshes'], adv


def main():
    glyphs = []
    for c in CHARSET:
        meshes, adv = glyph(c)
        glyphs.append(dict(c=c, adv=round(adv, 5), meshes=meshes or []))
    space = round(_advance(0, ' ') / CAP, 5)
    doc = dict(version='1.0', font=FONT_FILES[0].name, license='SIL OFL 1.1', capUnits=1, depthUnits=1,
               frontAxis='+Z', space=space, glyphs=glyphs)
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, ensure_ascii=False, separators=(',', ':')).encode()
    (SRC / 'LetterKit.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    tris = sum(len(m['triangles']) // 3 for g in glyphs for m in g['meshes'])
    print(json.dumps(dict(status='LETTER_KIT_OK', font=FONT_FILES[0].name, glyphs=len(glyphs), triangles=tris,
                          bytes=len(blob))))


if __name__ == '__main__':
    main()
