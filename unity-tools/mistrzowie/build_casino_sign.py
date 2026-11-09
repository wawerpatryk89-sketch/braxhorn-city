#!/usr/bin/env python3
"""Owner 9.11 23:36 (casino T11): "Szyldu nie ma nadal. Jeżeli nie ma szyldu, zrób sam szyld porządny. Tak jak się
ma nazywać kasyno Wawer. Zrób zajebisty szyld, złoty." and "na wjeździe do garażu zobacz, co jest ... jakaś tabliczka".

Two REAL 3D signs from the region's own generator (ZASADA-SZYLDY-3D, like the Hotel Sobczak sign):
* WAWER_kasyno  - build_signs.build('WAWER','kasyno'): solid bevelled WAWER glyphs, KASYNO line, recessed light
                  strips, double prestige arch and base bar; every face is gold in Unity (no ivory).
* PARKING_garage - solid bevelled PARKING glyphs with a light strip, a ringed P and a base bar for the garage portal.
Mirrored for Unity's left-handed axes (faces toward +Z, reads correctly from the front).
Written to CasinoWawerSign.bytes, generated in CI (never committed).
"""
import gzip
import json

import numpy as np
from shapely.affinity import translate
from shapely.geometry import Point

from build_signs import SRC, K, Geometry, build, polygons, stroke, text_shape
from build_sobczak_sign import mirror_for_unity


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
    casino = mirror_for_unity(build('WAWER', 'kasyno'))
    garage = mirror_for_unity(parking())
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=[casino, garage])
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
