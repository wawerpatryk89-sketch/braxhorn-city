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
from shapely.affinity import scale, translate
from shapely.geometry import Point, Polygon, box
from shapely.ops import unary_union

from build_signs import SRC, K, Geometry, polygons, stroke, text_shape
from build_sobczak_sign import mirror_for_unity


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


def main():
    wawer = mirror_for_unity(casino())
    garage = mirror_for_unity(parking())
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=[wawer, garage])
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
