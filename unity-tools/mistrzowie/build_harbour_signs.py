#!/usr/bin/env python3
"""Owner 10.11 09:01-09:15 (promenade, port): "napisy" hung in the air as flat white TextMesh labels ("S04 | SKUTER",
"BRAXHORN MARINA | J01-J15", "PORT WYCIECZKOWCÓW | W01 / W02") overlapping each other over the water; the K1-K12
kiosks had small stickers. ZASADA-SZYLDY-3D (owner 9.11): every sign is a solid 3D body.

Owner 10.11 10:17 (photo of the marina gate: thin black frame, cream DejaVu letters standing on its top bar):
"kompletnie zakazuje takich tanich napisów ... Globalnie zakaz tanich napisów" (ZASADA-NAPISY-PREMIUM-10-11).
So: no system font (DejaVu/Arial/Liberation) - Montserrat SemiBold (SIL OFL 1.1, fonts/OFL.txt, latin + latin-ext
subsets for Ł Ą Ę Ż Ś), tracked capitals (real letter-spacing, not stretched glyphs), a brand lockup on the gates
(BRAXHORN over the function in smaller wide-spaced capitals), letters mounted on architecture (BraxChampionsHarbour
Gates: marble portal with a graphite sign band; kiosk fascia; pedestals), never standing on a bar.

Real 3D lettering with the region's sign kit (build_signs.Geometry: bevelled solid glyphs, closed and manifold):
* GATE_*    - lockup on the marble portal at the top of each gangway (BRAXHORN .40 m, function .15 m)
* BERTH_*   - berth numbers J01-J15, L01-L10, S01-S10, F01, W01, W02 on the marina pedestals / posts (0.15 m)
* KIOSK_K*  - the twelve promenade kiosk names on their canopy fascia (0.30 m)
Roles: 'face' (letter fronts), 'gold' (the bevel ring of the kit = also the face material), 'dark' (returns / backs).
Mirrored for Unity's left-handed axes: faces toward +Z, reads correctly from the front.
Written to HarbourSigns.bytes, generated in CI (never committed).
"""
import gzip
import json
from pathlib import Path

import numpy as np
from fontTools.ttLib import TTFont
from matplotlib.font_manager import FontProperties
from matplotlib.textpath import TextPath
from shapely.affinity import scale, translate
from shapely.geometry import Polygon
from shapely.ops import unary_union

from build_signs import SRC, K, Geometry, polygons
from build_sobczak_sign import mirror_for_unity
from port_layout import BERTHS
from promenade_layout import PAVILIONS

FONT_DIR = Path(__file__).resolve().parent / 'fonts'
FONT_FILES = [FONT_DIR / 'Montserrat-SemiBold-latin.ttf', FONT_DIR / 'Montserrat-SemiBold-latin-ext.ttf']
for f in FONT_FILES + [FONT_DIR / 'OFL.txt']:
    if not f.exists():
        raise FileNotFoundError(f'Premium sign typeface missing: {f} (Montserrat, SIL OFL 1.1)')
FPS = [FontProperties(fname=str(f)) for f in FONT_FILES]
_TT = [TTFont(str(f)) for f in FONT_FILES]
CMAPS = [t.getBestCmap() for t in _TT]
BANNED_FONTS = ('dejavu', 'liberation', 'arial', 'freesans', 'helvetica')
assert not any(b in str(f).lower() for f in FONT_FILES for b in BANNED_FONTS)

# brand lockup on every gangway portal: BRAXHORN over the function
GATES = {
    'GATE_MARINA': ('BRAXHORN', 'MARINA'),
    'GATE_MOTOR': ('BRAXHORN', 'MOTOR'),
    'GATE_WATERSPORTS': ('BRAXHORN', 'WATERSPORTS'),
    'GATE_FERRY': ('BRAXHORN', 'PROM'),
    'GATE_CRUISE': ('BRAXHORN', 'PORT WYCIECZKOWY'),
}
TITLE_CAP, SUB_CAP, LINE_GAP = .40, .15, .11      # metres; the C# portal band is .87 m tall
TITLE_TRACK, SUB_TRACK, KIOSK_TRACK, BERTH_TRACK = .20, .42, .14, .04   # letter-spacing in em


def symmetric(rings):
    g = rings[0]
    for r in rings[1:]:
        g = g.symmetric_difference(r)
    return g


def _font(c):
    for i, cmap in enumerate(CMAPS):
        if ord(c) in cmap:
            return i
    raise ValueError(f'glyph {c!r} not in the sign typeface')


def _advance(i, c):
    t = _TT[i]
    return t['hmtx'][CMAPS[i][ord(c)]][0] * 100.0 / t['head'].unitsPerEm


CAP = TextPath((0, 0), 'H', size=100, prop=FPS[0]).get_extents().height


def tracked(s, tracking):
    """Glyph outlines of s at size 100 (font units), letters set apart by tracking (em), baseline y = 0."""
    parts, x = [], 0.0
    for c in s:
        i = _font(c if c != ' ' else 'H')
        if c != ' ':
            path = TextPath((x, 0), c, size=100, prop=FPS[i])
            rings = [Polygon(q) for q in path.to_polygons() if len(q) > 3]
            if rings:
                parts.append(symmetric(rings).buffer(0))      # per glyph: counters of A B O R stay holes
        x += _advance(i, c) + tracking * 100
    g = unary_union(parts).buffer(0)
    assert g.is_valid and not g.is_empty, s
    return g


def sans_shape(s, height, maxwidth, tracking=0.0):
    """Tracked capitals of s, cap height = height (metres, smaller if wider than maxwidth), centred on x = 0."""
    g = tracked(s, tracking)
    x0, y0, x1, y1 = g.bounds
    f = min(height / CAP, maxwidth / (x1 - x0))
    return translate(scale(g, xfact=f, yfact=f, origin=(0, 0)), xoff=-(x0 + x1) * f / 2)


def lockup(title, sub):
    """BRAXHORN (cap .40) over the function (cap .15, wide-spaced), both centred; the function's baseline is y = 0."""
    t = sans_shape(title, TITLE_CAP, 99.0, TITLE_TRACK)
    u = sans_shape(sub, SUB_CAP, 99.0, SUB_TRACK)
    t = translate(t, yoff=SUB_CAP + LINE_GAP)
    return unary_union([t, u])


def finish(g, sign_id, label, width):
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 7) for v in m['positions']]
        m['normals'] = [round(v, 7) for v in m['normals']]
        m['uv'] = [round(v, 7) for v in m['uv']]
        m['id'] = sign_id + '_' + role
    return mirror_for_unity(dict(id=sign_id, label=label, nominalWidth=width, meshes=list(g.roles.values()),
                                 acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING'))


def lettering(sign_id, text, height, maxwidth, depth, tracking=0.0, shape=None):
    g = Geometry()
    word = shape if shape is not None else sans_shape(text, height, maxwidth, tracking)
    g.solid(word, 0, depth, front='face', side='dark', bevel=min(.006, depth * .15))
    return finish(g, sign_id, text, word.bounds[2] - word.bounds[0])


def main():
    signs = []
    for sid, (title, sub) in GATES.items():
        signs.append(lettering(sid, title + ' / ' + sub, 0, 0, .07, shape=lockup(title, sub)))
    for b in BERTHS:
        signs.append(lettering('BERTH_' + b['id'], b['id'], .15, .5, .02, BERTH_TRACK))
    for p in PAVILIONS:
        signs.append(lettering('KIOSK_' + p['id'], p['sign'], .30, p['w'] - 1.2, .05, KIOSK_TRACK))
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=signs)
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, ensure_ascii=False, separators=(',', ':')).encode()
    (SRC / 'HarbourSigns.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    widths = {s['id']: round(s['nominalWidth'], 2) for s in signs if not s['id'].startswith('BERTH_')}
    tris = sum(len(m['triangles']) // 3 for s in signs for m in s['meshes'])
    print(json.dumps(dict(status='HARBOUR_SIGNS_OK', font=FONT_FILES[0].name, signs=len(signs), triangles=tris, widths=widths)))


if __name__ == '__main__':
    main()
