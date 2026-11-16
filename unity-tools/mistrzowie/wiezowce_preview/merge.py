"""merge.py <downtown_geometry.json> <out.json> : the five tower dumps (root-local nominal metres, MeshDump format) turned
and moved onto their plots, plus the region's foundation tiles round downtown, in one world file for
../hotel_sobczak_preview/geom2bin.py + render3d.js."""
import json, math, sys

src, out = sys.argv[1], sys.argv[2]
d = json.load(open(src))
res = dict(levels=[0], quads=[], meshes=[], models=[], palms=[], colours={}, ambient={}, pbr={}, lights=[])


def turn(yaw, x, z):
    a = math.radians(yaw); c, s = math.cos(a), math.sin(a)
    # Unity: Quaternion.Euler(0, yaw, 0) * v  ->  x' = x cos + z sin, z' = -x sin + z cos
    return lambda px, py, pz: (px * c + pz * s + x, py, -px * s + pz * c + z)


def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return [aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz]


for entry in d['meshdumps']:
    tid, yaw, x, z, path = entry.split('|')
    yaw, x, z = float(yaw), float(x), float(z)
    T = turn(yaw, x, z)
    m = json.load(open(path))
    for q in m['quads']:
        pts = q[3:15]; w = []
        for i in range(4): w += list(T(*pts[3 * i:3 * i + 3]))
        res['quads'].append([tid + ' ' + q[0], q[1], q[2]] + [round(v, 4) for v in w])
    for e in m['meshes']:
        p = e[2]; w = []
        for i in range(0, len(p), 3): w += list(T(p[i], p[i + 1], p[i + 2]))
        res['meshes'].append([tid + ' ' + e[0], e[1], [round(v, 4) for v in w]] + e[3:])
    qy = [0, math.sin(math.radians(yaw) / 2), 0, math.cos(math.radians(yaw) / 2)]
    K = 2.33 / 1.80
    for mo in m.get('models', []):
        # the dump keeps the model's world scale (the root carries K); this scene is in nominal metres
        mo = dict(mo); mo['pos'] = list(T(*mo['pos']))
        if 'yaw' in mo: mo['yaw'] = mo['yaw'] + yaw
        if 'rot' in mo: mo['rot'] = qmul(qy, mo['rot'])
        if abs(mo.get('scale', 1) - K) < 1e-3: mo['scale'] = 1
        res['models'].append(mo)
    for pl in m.get('palms', []):
        res['palms'].append(list(T(*pl[:3])) + pl[3:])
    for l in m.get('lights', []):
        res['lights'].append(list(T(*l[:3])) + [l[3] / K] + l[4:])
    for k in ('colours', 'ambient', 'pbr'): res[k].update(m.get(k, {}))
for f in d.get('foundation', []):
    res['meshes'].append(f)
# the region's lawn is Poly Haven sparse_grass with the city's green tint (as tower_lawn); paving / asphalt untinted
res['colours'].setdefault('fnd_grass|sparse_grass|2', [1.16, 1.64, 1.79, 1])
json.dump(res, open(out, 'w'))
print('MERGED quads=%d meshes=%d models=%d palms=%d lights=%d' % (len(res['quads']), len(res['meshes']), len(res['models']), len(res['palms']), len(res['lights'])))
