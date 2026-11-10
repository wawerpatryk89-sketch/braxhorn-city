#!/usr/bin/env python3
"""Owner 10.11 09:01-09:15 (promenade, port): "napisy" hung in the air as flat white TextMesh labels ("S04 | SKUTER",
"BRAXHORN MARINA | J01-J15", "PORT WYCIECZKOWCÓW | W01 / W02") overlapping each other over the water; the K1-K12
kiosks had small stickers. ZASADA-SZYLDY-3D (owner 9.11): every sign is a solid 3D body.

Real 3D lettering with the region's sign kit (build_signs.Geometry: bevelled solid glyphs, closed and manifold):
* GATE_*    - names on the steel gates at the top of each gangway from the promenade (letters 0.58 m)
* BERTH_*   - berth numbers J01-J15, L01-L10, S01-S10, F01, W01, W02 on the marina pedestals / posts (0.15 m)
* KIOSK_K*  - the twelve promenade kiosk names on their canopy fascia (0.30 m)
Roles: 'face' (letter fronts), 'gold' (the bevel ring of the kit = also the face material), 'dark' (returns / backs).
Sans lettering (DejaVu Sans Bold, fonts-dejavu-core, the same package as the kit's serif).
Mirrored for Unity's left-handed axes: faces toward +Z, reads correctly from the front.
Written to HarbourSigns.bytes, generated in CI (never committed).
"""
import gzip
import json
from pathlib import Path

import numpy as np
from matplotlib.font_manager import FontProperties
from matplotlib.textpath import TextPath
from shapely.affinity import scale, translate
from shapely.geometry import Polygon

from build_signs import SRC, K, Geometry, polygons
from build_sobczak_sign import mirror_for_unity
from port_layout import BERTHS
from promenade_layout import PAVILIONS

SANS = Path('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf')
if not SANS.exists():
    raise FileNotFoundError('Install fonts-dejavu-core: licensed source font required')
FP = FontProperties(fname=str(SANS))

GATES = {
    'GATE_MARINA': 'BRAXHORN MARINA',
    'GATE_MOTOR': 'BRAXHORN MOTOR',
    'GATE_WATERSPORTS': 'WATERSPORTS',
    'GATE_FERRY': 'PROM',
    'GATE_CRUISE': 'PORT WYCIECZKOWY',
}


def symmetric(rings):
    g = rings[0]
    for r in rings[1:]:
        g = g.symmetric_difference(r)
    return g


def sans_shape(s, height, maxwidth, tracking=0.0):
    """Glyph outlines of s, cap height = height (metres), centred on x = 0, baseline y = 0."""
    path = TextPath((0, 0), s, size=100, prop=FP)
    rings = [Polygon(q) for q in path.to_polygons() if len(q) > 3]
    g = symmetric(rings).buffer(0)
    assert g.is_valid and not g.is_empty, s
    cap = TextPath((0, 0), 'H', size=100, prop=FP).get_extents().height
    x0, y0, x1, y1 = g.bounds
    f = min(height / cap, maxwidth / (x1 - x0))
    return translate(scale(g, xfact=f, yfact=f, origin=(0, 0)), xoff=-(x0 + x1) * f / 2)


def finish(g, sign_id, label, width):
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 7) for v in m['positions']]
        m['normals'] = [round(v, 7) for v in m['normals']]
        m['uv'] = [round(v, 7) for v in m['uv']]
        m['id'] = sign_id + '_' + role
    return mirror_for_unity(dict(id=sign_id, label=label, nominalWidth=width, meshes=list(g.roles.values()),
                                 acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING'))


def lettering(sign_id, text, height, maxwidth, depth):
    g = Geometry()
    word = sans_shape(text, height, maxwidth)
    g.solid(word, 0, depth, front='face', side='dark', bevel=min(.006, depth * .15))
    return finish(g, sign_id, text, word.bounds[2] - word.bounds[0])


def main():
    signs = []
    for sid, text in GATES.items():
        signs.append(lettering(sid, text, .58, 7.4, .09))
    for b in BERTHS:
        signs.append(lettering('BERTH_' + b['id'], b['id'], .15, .5, .02))
    for p in PAVILIONS:
        signs.append(lettering('KIOSK_' + p['id'], p['sign'], .30, p['w'] - .9, .05))
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=signs)
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, ensure_ascii=False, separators=(',', ':')).encode()
    (SRC / 'HarbourSigns.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    widths = {s['id']: round(s['nominalWidth'], 2) for s in signs if not s['id'].startswith('BERTH_')}
    tris = sum(len(m['triangles']) // 3 for s in signs for m in s['meshes'])
    print(json.dumps(dict(status='HARBOUR_SIGNS_OK', signs=len(signs), triangles=tris, widths=widths)))


if __name__ == '__main__':
    main()
