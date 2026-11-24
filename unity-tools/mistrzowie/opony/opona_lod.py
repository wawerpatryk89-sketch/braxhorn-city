#!/usr/bin/env python3
"""THE QUAD TRACK'S TYRES (owner 24.11.2026 17:11, his pick "Opony na zakrętach" + "Ściany z opon"): Poly Haven's Old
Tyre (CC0, https://polyhaven.com/a/old_tyre - a scanned 0.6 m car tyre, 2880 triangles) as two levels of detail for
BraxChampionsOponyKladow, in one small binary the editor reads without an importer:

    python3 opona_lod.py <old_tyre_1k.gltf> <lod1.gltf> <out .bytes>

  LOD 0: the scan itself (2880 triangles); LOD 1: the same simplified by meshoptimizer with its UV seams kept (896):
      node node_modules/gltfpack/cli.js -i old_tyre_1k.gltf -o lod1.gltf -si 0.3 -noq      (npm gltfpack@1.3.0)
  The textures are the scan's own 4K maps (prepare_assets.py: 'old_tyre'); the far level is a plain ring (the builder).
Format (little-endian): b"OPN1", int32 levels; per level: int32 vertices, int32 indices, then per vertex 8 float32
(position x y z, normal x y z, uv u v - v flipped for Unity), then uint16 indices. The tyre stands on its tread, its axis
along z, its centre at the origin (metres)."""
import json
import os
import struct
import sys


def czytaj(path):
    g = json.load(open(path))
    base = os.path.dirname(path)
    bufs = [open(os.path.join(base, b["uri"]), "rb").read() for b in g["buffers"]]

    def acc(i):
        a = g["accessors"][i]; v = g["bufferViews"][a["bufferView"]]; b = bufs[v.get("buffer", 0)]
        off = v.get("byteOffset", 0) + a.get("byteOffset", 0)
        n = {"SCALAR": 1, "VEC2": 2, "VEC3": 3}[a["type"]]; dt = {5126: "f", 5123: "H", 5125: "I"}[a["componentType"]]
        st = v.get("byteStride", 0) or n * struct.calcsize(dt)
        return [struct.unpack_from("<" + dt * n, b, off + k * st) for k in range(a["count"])]

    assert len(g["meshes"]) == 1 and len(g["meshes"][0]["primitives"]) == 1, path
    node = next(n for n in g["nodes"] if "mesh" in n)
    assert not any(k in node for k in ("rotation", "scale", "matrix")), "the tyre's node is not at rest"
    p = g["meshes"][0]["primitives"][0]
    pos = acc(p["attributes"]["POSITION"]); nrm = acc(p["attributes"]["NORMAL"]); uv = acc(p["attributes"]["TEXCOORD_0"])
    idx = [i[0] for i in acc(p["indices"])]
    t = node.get("translation", (0, 0, 0))
    pos = [(x + t[0], y + t[1], z + t[2]) for x, y, z in pos]
    assert len(pos) < 65536
    return pos, nrm, uv, idx


def main(lod0, lod1, out):
    poziomy = [czytaj(lod0), czytaj(lod1)]
    with open(out, "wb") as f:
        f.write(b"OPN1"); f.write(struct.pack("<i", len(poziomy)))
        for pos, nrm, uv, idx in poziomy:
            f.write(struct.pack("<ii", len(pos), len(idx)))
            for p, n, t in zip(pos, nrm, uv):
                f.write(struct.pack("<8f", p[0], p[1], p[2], n[0], n[1], n[2], t[0], 1.0 - t[1]))
            f.write(struct.pack("<%dH" % len(idx), *idx))
            print(os.path.basename(out), "level: %d vertices, %d triangles" % (len(pos), len(idx) // 3))


if __name__ == "__main__":
    main(*sys.argv[1:4])
