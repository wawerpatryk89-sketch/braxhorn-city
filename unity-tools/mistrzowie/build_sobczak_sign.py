#!/usr/bin/env python3
"""Owner 9.11 (Hotel Sobczak rebuild): "Tak samo szyld ma trafić odpowiedni".

The hotel sign is a REAL 3D body like every sign in the region
(ZASADA-SZYLDY-3D): the region's own sign generator (build_signs.build)
for brand SOBCZAK + service HOTEL - solid bevelled glyphs with a gold face,
recessed light strip, dark pins, five hotel stars, prestige frame and the
S monogram in a gold ring (no external emblem model needed for SOBCZAK).
Written to a separate file so the arena/T01 Signs.bytes contract stays as is.
"""
import gzip
import json

from build_signs import ROOT, SRC, K, build


def mirror_for_unity(sign):
    """build() lays the text along +x with the faces toward +Z. Unity is left-handed: seen from +Z
    the viewer's right is -x, so that text reads backwards (checked in the depth-buffered preview,
    hotel_sobczak_preview/render3d.js). Mirror x and swap the winding: faces stay toward +Z and the
    lettering reads correctly from the front."""
    for m in sign['meshes']:
        p = m['positions']
        for i in range(0, len(p), 3):
            p[i] = -p[i]
        n = m.get('normals')
        if n:
            for i in range(0, len(n), 3):
                n[i] = -n[i]
        t = m['triangles']
        for i in range(0, len(t), 3):
            t[i + 1], t[i + 2] = t[i + 2], t[i + 1]
    sign['readsFrom'] = '+Z (mirrored for Unity left-handed axes)'
    return sign


def main():
    sign = mirror_for_unity(build('SOBCZAK', 'hotel'))
    doc = dict(version='1.0', scaleK=K, frontAxis='+Z', rootScale=[1, 1, 1], signs=[sign])
    SRC.mkdir(parents=True, exist_ok=True)
    blob = json.dumps(doc, ensure_ascii=False, separators=(',', ':')).encode()
    (SRC / 'HotelSobczakSign.bytes').write_bytes(gzip.compress(blob, compresslevel=9, mtime=0))
    tris = sum(len(m['triangles']) // 3 for m in sign['meshes'])
    roles = sorted(m['role'] for m in sign['meshes'])
    print(json.dumps(dict(status='SOBCZAK_HOTEL_SIGN_OK', id=sign['id'], roles=roles, triangles=tris,
                          nominalWidth=sign['nominalWidth'], emblem=not sign['needsSculptedEmblem'])))


if __name__ == '__main__':
    main()
