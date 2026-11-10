#!/usr/bin/env python3
"""Owner 9.11 23:36 (casino T11): "Szyldu nie ma nadal. Jeżeli nie ma szyldu, zrób sam szyld porządny. Tak jak się
ma nazywać kasyno Wawer. Zrób zajebisty szyld, złoty." and "na wjeździe do garażu zobacz, co jest ... jakaś tabliczka".
Owner 10.11 00:22 on the first render: "Napis jest spoko, ale coś mi brakuje. Słaby to jest jeszcze. Nie jest premium."

Two REAL 3D signs built with the region's sign kit (ZASADA-SZYLDY-3D, like the Hotel Sobczak sign):
* WAWER_kasyno  - a full casino composition, not just two words: deep bevelled WAWER glyphs standing on a back-lit
                  halo, spaced K A S Y N O between gold rules with diamond ends, a jewelled crown on top, a double
                  prestige arch, the four card suits either side and a double underline with a centre diamond.
                  Every face is gold in Unity; halo, gems and rule lines glow.
* PARKING_garage - solid bevelled PARKING glyphs with a light strip, a ringed P and a base bar for the garage portal.
Mirrored for Unity's left-handed axes (faces toward +Z, reads correctly from the front).
Written to CasinoWawerSign.bytes, generated in CI (never committed).
"""
import gzip
import json
import math

import numpy as np
from matplotlib.font_manager import FontProperties
from matplotlib.textpath import TextPath
from shapely import symmetric_difference_all
from shapely.affinity import rotate, scale, translate
from shapely.geometry import Point, Polygon, box
from shapely.ops import unary_union

from build_signs import SRC, K, Geometry, polygons, stroke, text_shape
from build_sobczak_sign import mirror_for_unity
from build_harbour_signs import FONT_FILES


def clean(g):
    """Union of circles and triangles -> one outline without slivers or collinear points (the kit rejects zero-area triangles)."""
    return g.buffer(.012, quad_segs=4, join_style='round').buffer(-.012, join_style='round').simplify(.0015)


def diamond(x, y, h):
    w = h * .36
    return Polygon([(x, y + h / 2), (x + w, y), (x, y - h / 2), (x - w, y)])


def heart(x, y, h):
    g = unary_union([Point(-.24, .2).buffer(.27, quad_segs=10), Point(.24, .2).buffer(.27, quad_segs=10),
                     Polygon([(-.487, .085), (.487, .085), (0, -.5)])])
    return translate(scale(clean(g), h, h, origin=(0, 0)), x, y)


def spade(x, y, h):
    g = unary_union([Point(-.24, -.10).buffer(.27, quad_segs=10), Point(.24, -.10).buffer(.27, quad_segs=10),
                     Polygon([(-.487, .015), (.487, .015), (0, .52)]), Polygon([(-.06, -.2), (.06, -.2), (.19, -.5), (-.19, -.5)])])
    return translate(scale(clean(g), h, h, origin=(0, 0)), x, y)


def club(x, y, h):
    g = unary_union([Point(0, .24).buffer(.235, quad_segs=10), Point(-.26, -.09).buffer(.235, quad_segs=10),
                     Point(.26, -.09).buffer(.235, quad_segs=10), Polygon([(-.06, 0), (.06, 0), (.19, -.5), (-.19, -.5)])])
    return translate(scale(clean(g), h, h, origin=(0, 0)), x, y)


def casino():
    g = Geometry()
    gold = dict(front='gold', side='accent')
    # WAWER: deep glyphs on a back-lit halo (a glowing plate 6 cm wider than every letter)
    word = text_shape('WAWER', 1.22, 7.4, .02)
    g.solid(word.buffer(.065, quad_segs=6, join_style='round'), 0, .03, front='light', side='light', bevel=.002)
    g.solid(word, .03, .36, bevel=.016, **gold)
    # K A S Y N O between two gold rules that end in diamonds
    label = text_shape('K A S Y N O', .40, 4.3, 1.58)
    g.solid(label.buffer(.035, quad_segs=5, join_style='round'), 0, .025, front='light', side='light', bevel=.0015)
    g.solid(label, .025, .22, bevel=.008, **gold)
    for s in (-1, 1):
        rule = box(min(s * 2.55, s * 4.55), 1.755, max(s * 2.55, s * 4.55), 1.80)
        g.solid(rule, .03, .14, bevel=.004, **gold)
        g.solid(diamond(s * 4.72, 1.778, .26), .03, .17, bevel=.006, **gold)
        g.solid(diamond(s * 2.40, 1.778, .16), .03, .15, bevel=.004, **gold)
    # Jewelled crown on top
    crown = Polygon([(-.86, 2.20), (.86, 2.20), (1.02, 2.80), (.52, 2.47), (0, 2.93), (-.52, 2.47), (-1.02, 2.80)])
    g.solid(crown, .03, .26, bevel=.012, **gold)
    g.solid(box(-.90, 2.04, .90, 2.17), .03, .30, bevel=.008, **gold)
    for x, y, r in ((-1.02, 2.86, .085), (1.02, 2.86, .085), (0, 3.00, .10), (-.52, 2.54, .06), (.52, 2.54, .06)):
        g.solid(Point(x, y).buffer(r, quad_segs=8), .03, .30, bevel=.012, **gold)
    for x in (-.60, -.30, 0, .30, .60):
        g.solid(Point(x, 2.105).buffer(.038, quad_segs=6), .30, .325, front='light', side='light', bevel=.002)
    # Double prestige arch from both ends up to the crown
    for s in (-1, 1):
        pts = [(s * 6.55 * math.cos(t), .12 + 2.42 * math.sin(t)) for t in np.linspace(.07, 1.375, 46)]
        g.solid(stroke(pts, .13), .03, .19, bevel=.010, **gold)
        g.solid(stroke(pts, .045), .19, .215, front='light', side='light', bevel=.002)
        inner = [(s * 6.15 * math.cos(t), .12 + 2.10 * math.sin(t)) for t in np.linspace(.09, 1.30, 40)]
        g.solid(stroke(inner, .05), .03, .13, bevel=.005, **gold)
    # The four card suits
    for shape in (spade(-4.62, .62, .86), heart(-5.62, .56, .74), diamond(4.62, .62, .98), club(5.62, .56, .78)):
        g.solid(shape, .03, .24, bevel=.012, **gold)
    # Double underline with a centre diamond
    for s in (-1, 1):
        g.solid(box(min(s * .34, s * 4.05), -.20, max(s * .34, s * 4.05), -.14), .03, .16, bevel=.005, **gold)
        g.solid(box(min(s * .34, s * 3.35), -.31, max(s * .34, s * 3.35), -.275), .03, .12, bevel=.003, **gold)
    g.solid(diamond(0, -.225, .30), .03, .19, bevel=.007, **gold)
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 7) for v in m['positions']]
        m['normals'] = [round(v, 7) for v in m['normals']]
        m['uv'] = [round(v, 7) for v in m['uv']]
        m['id'] = 'WAWER_kasyno_' + role
    return dict(id='WAWER_kasyno', brand='WAWER', service='kasyno', label='KASYNO WAWER', nominalWidth=13.3,
                needsSculptedEmblem=False, meshes=list(g.roles.values()), acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING')


def parking():
    g = Geometry()
    word = translate(text_shape('PARKING', .56, 3.9, 0), xoff=.48)          # word right, ringed P left, centred as a whole
    g.solid(word, .07, .22, front='gold', side='dark')
    g.solid(word, .035, .06, front='light', side='light', bevel=.0015)
    for poly in polygons(word):
        p = poly.representative_point()
        g.solid(p.buffer(.016, quad_segs=4), 0, .072, front='dark', side='dark', bevel=.001)
    c = (-2.07, .28)
    ring = Point(*c).buffer(.42, quad_segs=24).difference(Point(*c).buffer(.34, quad_segs=24))
    g.solid(ring, .06, .21, front='gold', side='accent', bevel=.006)
    g.solid(translate(text_shape('P', .42, .5, .07), xoff=c[0]), .07, .23, front='gold', side='dark')
    g.solid(stroke([(x, -.16) for x in np.linspace(-2.45, 2.45, 40)], .05), .05, .14, front='gold', side='accent', bevel=.006)
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 7) for v in m['positions']]
        m['normals'] = [round(v, 7) for v in m['normals']]
        m['uv'] = [round(v, 7) for v in m['uv']]
        m['id'] = 'PARKING_garage_' + role
    return dict(id='PARKING_garage', brand='PARKING', service='garage', label='PARKING', nominalWidth=5.0,
                needsSculptedEmblem=False, meshes=list(g.roles.values()), acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING')


# Owner 10.11 16:36-16:45 (casino): the rounded black roof the name stood on goes ("to czarne coś ... te jajo na czym
# jest szyld to wszystko psuje"), the name must be "lepszy i czytelniejszy", "premium premium", and from two reference
# pictures he chose the second ("ten drugi lepszy"): a heavy gold word with a red outline, the four card suits rising
# behind its top edge, gold ribbons with a red stripe flying from both ends and white sparkles.
# Built here as one layered relief in real depth (every layer its own closed solid, layers 4-14 cm apart in z, never
# a decal): gold letters (front .50) -> red outline (.36) -> gold rim (.26) -> suits behind the top -> ribbons behind
# the ends. It stands on the casino's front parapet between the pylons, against the sky.
CROWN_TEXT = 'KASYNO WAWER'
CROWN_CAP = 1.75
CROWN_RED, CROWN_RIM = .17, .27
MONTSERRAT = FontProperties(fname=str(FONT_FILES[0]))


def heavy_text(s, cap):
    """Montserrat SemiBold (the region's premium face, SIL OFL), set at cap height and made a display weight by
    growing every stroke 3.5 % of the cap with mitred corners (counters stay open)."""
    path = TextPath((0, 0), s, size=100, prop=MONTSERRAT)
    g = symmetric_difference_all([Polygon(q) for q in path.to_polygons() if len(q) > 3])
    capref = TextPath((0, 0), 'H', size=100, prop=MONTSERRAT).get_extents()
    f = cap / capref.height
    g = scale(g, f, f, origin=(0, 0))
    g = g.buffer(cap * .035, join_style='mitre', mitre_limit=2.0).simplify(.002)
    x0, _, x1, _ = g.bounds
    return translate(g, -(x0 + x1) / 2, 0)


def star4(x, y, r):
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        rr = r if i % 2 == 0 else r * .17
        pts.append((x + rr * math.cos(a), y + rr * math.sin(a)))
    return Polygon(pts)


def ribbon(side, xe, cap):
    """One flying ribbon: the band leaves the word behind its outline and sweeps up and out, folds back once (the red
    underside shows in the fold) and flies on as a swallowtail. Returns (band, fold, tail, width) for side +1 / -1."""
    h = 1.12
    xa, x1 = xe - 1.2, xe + 1.55
    lift = lambda x: 1.05 * max(0.0, (x - xa) / (x1 - xa)) ** 1.7
    yc = cap * .48
    n = 20
    xs = [xa + (x1 - xa) * i / n for i in range(n + 1)]
    band = Polygon([(x, yc + lift(x) - h / 2) for x in xs] + [(x, yc + lift(x) + h / 2) for x in reversed(xs)])
    y1 = yc + lift(x1)
    t0, x2 = x1 - .80, x1 + 1.85
    tl = lambda x: y1 - .78 - .30 * max(0.0, (x - t0) / (x2 - t0)) ** 1.2
    txs = [t0 - .06 + (x2 - t0 + .06) * i / n for i in range(n + 1)]    # 6 cm behind the fold's edge: no shared face
    tail = Polygon([(x, tl(x) - h / 2) for x in txs] + [(x2 - .62, tl(x2))] + [(x, tl(x) + h / 2) for x in reversed(txs)])
    fold = Polygon([(x1, y1 - h / 2), (t0, yc + lift(t0) - h / 2), (t0, tl(t0) - h / 2)])
    if side < 0:
        band, tail, fold = (scale(g, -1, 1, origin=(0, 0)) for g in (band, tail, fold))
    return band, fold, tail, h


def stripe(band, h):
    """A red band along the middle of a ribbon piece, 22 % of its width."""
    up = translate(band, 0, -h * .33).intersection(translate(band, 0, h * .33))
    return clean(up.buffer(-.04, join_style='mitre'))     # 4 cm inside every edge of its band: no shared side faces


def roof_crown():
    g = Geometry()
    cap = CROWN_CAP
    word = heavy_text(CROWN_TEXT, cap)
    red = word.buffer(CROWN_RED, quad_segs=8, join_style='round')
    rim = word.buffer(CROWN_RIM, quad_segs=8, join_style='round')
    # fill the little gaps between letters so the outline is one shape like the reference
    red = red.buffer(.10, quad_segs=6).buffer(-.10, quad_segs=6)
    rim = rim.buffer(.12, quad_segs=6).buffer(-.12, quad_segs=6)
    g.solid(word, .20, .50, front='gold', side='accent', bevel=.035)
    g.solid(red, .12, .36, front='red', side='red', bevel=.025, bevel_role='red')
    g.solid(rim, .06, .26, front='gold', side='accent', bevel=.02)
    # the four suits rising behind the top of the word, fanned like cards held in a hand
    top = cap
    # back to front: club, diamond, spade, heart - each its own z band (2-3 cm steps) so overlapping cards never share a
    # face plane with each other or with the word's layers (.06 .12 .20 .26 .36 .50)
    suits = ((club, -3.8, -17, 'black', 2.6, (-.21, .03), (-.15, .11)),
             (diamond, 3.75, 17, 'red', 2.55, (-.19, .05), (-.13, .13)),
             (spade, 1.28, 6, 'black', 2.75, (-.17, .07), (-.11, .15)),
             (heart, -1.28, -6, 'red', 2.75, (-.14, .09), (-.08, .18)))
    for fn, x, ang, role, hh, zr, zb in suits:
        y = top + hh * .30 - (.12 if abs(x) > 2 else 0)
        shape = rotate(fn(x, y, hh), -ang, origin=(x, y))
        g.solid(clean(shape.buffer(.10, quad_segs=6, join_style='round')), zr[0], zr[1], front='gold', side='accent', bevel=.018)
        g.solid(shape, zb[0], zb[1], front=role, side=role, bevel=.03, bevel_role=role)
    # ribbons from both ends of the word
    xe = rim.bounds[2]
    for side in (1, -1):
        band, fold, tail, h = ribbon(side, xe, cap)
        g.solid(tail, -.30, -.17, front='gold', side='accent', bevel=.012)
        g.solid(stripe(tail, h), -.23, -.13, front='red', side='red', bevel=.008, bevel_role='red')
        g.solid(fold, -.20, -.07, front='red', side='red', bevel=.010, bevel_role='red')
        g.solid(band, -.10, .03, front='gold', side='accent', bevel=.012)
        g.solid(stripe(band, h), -.035, .07, front='red', side='red', bevel=.008, bevel_role='red')
    # sparkles: each passes through the face it shines on (letter, heart, diamond, club), never lies on it
    for x, y, r, z0, z1 in ((-xe + 1.0, cap * .95, .44, .44, .58), (xe - 2.3, cap * .10, .36, .44, .58),
                            (-1.75, top + 1.95, .46, .12, .25), (4.05, top + 1.35, .36, .07, .21), (-4.35, top + 1.25, .30, .05, .17)):
        g.solid(star4(x, y, r), z0, z1, front='light', side='light', bevel=.004, bevel_role='light')
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 7) for v in m['positions']]
        m['normals'] = [round(v, 7) for v in m['normals']]
        m['uv'] = [round(v, 7) for v in m['uv']]
        m['id'] = 'KASYNO_WAWER_crown_' + role
    b = rim.bounds
    return dict(id='KASYNO_WAWER_crown', brand='WAWER', service='kasyno', label=CROWN_TEXT, nominalWidth=round(b[2] - b[0], 2),
                needsSculptedEmblem=False, meshes=list(g.roles.values()), acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING')


def main():
    wawer = mirror_for_unity(casino())
    garage = mirror_for_unity(parking())
    crown = mirror_for_unity(roof_crown())
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=[wawer, garage, crown])
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, ensure_ascii=False, separators=(',', ':')).encode()
    (SRC / 'CasinoWawerSign.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    out = []
    for s in doc['signs']:
        p = np.concatenate([np.array(m['positions']).reshape(-1, 3) for m in s['meshes']]) / K
        out.append(dict(id=s['id'], triangles=sum(len(m['triangles']) // 3 for m in s['meshes']),
                        roles=sorted(m['role'] for m in s['meshes']), min=p.min(0).round(2).tolist(), max=p.max(0).round(2).tolist()))
    print(json.dumps(dict(status='CASINO_WAWER_SIGNS_OK', signs=out)))


if __name__ == '__main__':
    main()
