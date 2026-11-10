#!/usr/bin/env python3
"""Owner 10.11 19:14 (photo of the A1 arena from the Hotel Sobczak roof): "Arena na dachu można mojego byka dać żeby z góry
był widoczny". The owner's BRAXHORN bull mark (unity-city/Assets/Source/Claude/Osiedle/signs/byk_braxhorn.png, the brand's
mark on the estate signs) becomes a giant 3D relief on the black A1 roof, readable from the hotel and from the air:

* brushed steel PLATES with real thickness and bevelled edges exactly where the mark's silver plates are; the dark seams of
  the mark stay open (the black roof shows between the plates, as in the drawing),
* a green LED BAND round the whole silhouette, like the mark's green glow,
* 80 m across the horn tips (roof 192 x 118 m), every plate a closed manifold (build_signs.Geometry), nothing lies on the
  roof 1-5 mm apart: the bases sit ON the roof slab (BraxChampionsArenaRoofBull lays the +Z front up).

Writes ArenaRoofBull.bytes (same SignFile JSON as the other signs, K applied once). CI-generated, never committed.
"""
import gzip
import json

import contourpy
import numpy as np
from PIL import Image
from shapely import symmetric_difference_all
from shapely.geometry import Polygon
from shapely.geometry.polygon import orient
from shapely.ops import unary_union

from build_signs import ROOT, SRC, K, Geometry, polygons
from build_sobczak_sign import mirror_for_unity

MARK = ROOT / 'unity-city/Assets/Source/Claude/Osiedle/signs/byk_braxhorn.png'
WIDTH = 80.0                     # metres across the horn tips
PLATE_H, PLATE_BEVEL = .45, .07  # steel plates
LED_H, LED_BEVEL = .24, .03      # green LED band
LED_GAP, LED_W = .55, .70        # band .55 m outside the silhouette, .70 m wide


def _shift_or(m, r, op):
    """Binary dilation (op=np.logical_or) / erosion (op=np.logical_and) with a disk of radius r (numpy only: CI has no
    OpenCV/SciPy)."""
    out = m.copy()
    h, w = m.shape
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy > r * r or (dx == 0 and dy == 0):
                continue
            sh = np.zeros_like(m) if op is np.logical_or else np.ones_like(m)
            sh[max(dy, 0):h + min(dy, 0), max(dx, 0):w + min(dx, 0)] = m[max(-dy, 0):h + min(-dy, 0), max(-dx, 0):w + min(-dx, 0)]
            out = op(out, sh)
    return out


def mark_plates():
    im = np.asarray(Image.open(MARK).convert('RGB')).astype(np.int16)
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    green = (g > r + 35) & (g > b + 35) & (g > 90)
    silver = (im.min(2) > 95) & ~green
    silver = _shift_or(_shift_or(silver, 2, np.logical_or), 2, np.logical_and)     # close 5 px
    silver = _shift_or(_shift_or(silver, 1, np.logical_and), 1, np.logical_or)     # open 3 px
    ys, xs = np.nonzero(silver)
    s = WIDTH / (xs.max() - xs.min() + 1)
    cx, cy = (xs.max() + xs.min() + 1) / 2, (ys.max() + ys.min() + 1) / 2
    z = np.pad(silver.astype(float), 1)
    rings = []
    for line in contourpy.contour_generator(z=z).lines(.5):
        if len(line) >= 8:
            pts = [((x - 1 + .5 - cx) * s, -(y - 1 + .5 - cy) * s) for x, y in line]
            ring = Polygon(pts).buffer(0)
            if not ring.is_empty:
                rings.append(ring)
    region = symmetric_difference_all(rings)                         # even-odd: holes are the mark's dark seams
    px = s * s
    plates = []
    for part in polygons(region):
        if part.area < 400 * px:
            continue                                                  # shading specks
        holes = [h for h in part.interiors if Polygon(h).area > 600 * px]   # the nostrils stay, specks close
        poly = Polygon(part.exterior, holes).buffer(0).simplify(.10, preserve_topology=True)
        for q in polygons(poly.buffer(0)):
            if q.area > 1.5:
                plates.append(orient(q, sign=1))
    return plates


def build():
    plates = mark_plates()
    silhouette = unary_union(plates).buffer(1.6, join_style='round').buffer(-1.6, join_style='round')
    silhouette = unary_union([Polygon(p.exterior) for p in polygons(silhouette)])   # outline only, no rings inside the face
    band = silhouette.buffer(LED_GAP + LED_W, quad_segs=6).difference(silhouette.buffer(LED_GAP, quad_segs=6))
    band = band.simplify(.05, preserve_topology=True).buffer(0)
    g = Geometry()
    for p in plates:
        g.solid(p, 0.0, PLATE_H, front='steel', side='steel_edge', bevel=PLATE_BEVEL, bevel_role='steel')
    for p in polygons(band):
        if p.area > .5:
            g.solid(orient(p, sign=1), 0.0, LED_H, front='led', side='steel_edge', bevel=LED_BEVEL, bevel_role='led')
    for role, m in g.roles.items():
        m['positions'] = [round(v * K, 6) for v in m['positions']]
        m['normals'] = [round(v, 6) for v in m['normals']]
        m['uv'] = [round(v / 4 * .25, 6) for v in m['uv']]   # planar metres / 4: 1 tile per 4 m on the big plates
        m['id'] = 'BRAXHORN_roof_bull_' + role
    sign = dict(id='BRAXHORN_roof_bull', meshes=list(g.roles.values()), plates=len(plates),
                widthMetres=WIDTH, source=str(MARK.relative_to(ROOT)))
    return mirror_for_unity(sign), plates, band


def main():
    sign, plates, band = build()
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=[sign])
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, separators=(',', ':')).encode()
    (SRC / 'ArenaRoofBull.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    tris = sum(len(m['triangles']) // 3 for m in sign['meshes'])
    print(json.dumps(dict(status='ARENA_ROOF_BULL_OK', plates=len(plates), ledArea=round(band.area, 1),
                          plateArea=round(sum(p.area for p in plates), 1), triangles=tris,
                          roles=[m['role'] for m in sign['meshes']])))


if __name__ == '__main__':
    main()
