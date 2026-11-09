"""<x>_geometry.json (meshes + W quads) -> quads only, so apartments_preview/zfight.py can check a plain-mesh building
(casino, arena) for coplanar overlapping faces.   usage: python3 mesh2quads.py casino_geometry.json casino_quads.json"""
import json
import sys

d = json.load(open(sys.argv[1])); q = list(d["quads"])
for e in d["meshes"]:
    name, m, v, t = e[:4]
    for i in range(0, len(t), 3):
        a, b, c = [v[3 * k:3 * k + 3] for k in t[i:i + 3]]
        q.append([name, m, 0, *a, *b, *c, *c])
json.dump(dict(quads=q), open(sys.argv[2], "w"))
