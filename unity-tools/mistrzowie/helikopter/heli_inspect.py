"""inspect the source EC135: its skin joints and the meshes they carry (usage: python3 -I heli_inspect.py <this folder> <ec_135.glb>)"""
import sys, numpy as np
sys.path.insert(0, sys.argv[1])
from glb import Glb
g = Glb(sys.argv[2]); js = g.js
skin = js['skins'][0]; joints = skin['joints']; ibm = g.acc(skin['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
print('joints', [js['nodes'][j]['name'] for j in joints])
JM = [g.world(j) @ ibm[k] for k, j in enumerate(joints)]
for k, j in enumerate(joints): print(js['nodes'][j]['name'], 'world pos', np.round(g.world(j)[:3, 3], 2), 'JM-I max', np.round(np.abs(JM[k] - np.eye(4)).max(), 4))
for ni, n in enumerate(js['nodes']):
    if 'mesh' not in n: continue
    m = js['meshes'][n['mesh']]
    for pr in m['primitives']:
        P = g.acc(pr['attributes']['POSITION']).astype(np.float64)
        if 'JOINTS_0' in pr['attributes']:
            J = g.acc(pr['attributes']['JOINTS_0']); Wt = g.acc(pr['attributes']['WEIGHTS_0']).astype(np.float64)
            Ph = np.c_[P, np.ones(len(P))]; out = np.zeros((len(P), 4))
            for c in range(4):
                for k in range(len(joints)):
                    sel = J[:, c] == k
                    if sel.any(): out[sel] += Wt[sel, c:c + 1] * (Ph[sel] @ JM[k].T)
            Q = out[:, :3]; used = sorted(set(J[Wt > 0].tolist()))
        else:
            Q = (np.c_[P, np.ones(len(P))] @ g.world(ni).T)[:, :3]; used = None
        print(ni, m['name'][:40], 'min', np.round(Q.min(0), 1), 'max', np.round(Q.max(0), 1), 'joints', used)
