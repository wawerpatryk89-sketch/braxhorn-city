"""EC135 (GRIP420, CC-BY 4.0) -> baked static parts in metres, nose to +z, origin on the ground under the main rotor hub"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from glb import Glb
ROTOR_D = 10.20                       # EC135 main rotor diameter, m (Airbus data)
KEEP = {'EC135_RotorRear': 'wirnik_ogonowy', 'EC135_RotorMain': 'wirnik_glowny', 'EC135_Glass': 'szyby', 'EC135_PilotSeat_': 'fotel_pilota',
        'EC135_PilotSeat2': 'fotel_pilota_2', 'EC135_Body_Back': 'kadlub_tyl', 'EC135_Body_Inner': 'kabina', 'EC135_BackSeats': 'fotele_tyl',
        'EC135_Body_Outer': 'kadlub', 'EC135_CockpitButtons': 'kokpit'}

def load(path):
    g = Glb(path); js = g.js
    skin = js['skins'][0]; joints = skin['joints']
    ibm = g.acc(skin['inverseBindMatrices']).reshape(-1, 4, 4).transpose(0, 2, 1)
    JM = [g.world(j) @ ibm[k] for k, j in enumerate(joints)]
    names = {js['nodes'][j]['name']: j for j in joints}
    parts = {}
    for ni, n in enumerate(js['nodes']):
        if 'mesh' not in n: continue
        m = js['meshes'][n['mesh']]; key = None
        for k, v in KEEP.items():
            if m['name'].startswith(k): key = v
        if key is None or key in parts: continue
        pr = m['primitives'][0]
        P = g.acc(pr['attributes']['POSITION']).astype(np.float64); N = g.acc(pr['attributes']['NORMAL']).astype(np.float64)
        UV = g.acc(pr['attributes']['TEXCOORD_0']).astype(np.float64); I = g.acc(pr['indices']).reshape(-1).astype(np.int64)
        if 'JOINTS_0' in pr['attributes']:
            J = g.acc(pr['attributes']['JOINTS_0']); W = g.acc(pr['attributes']['WEIGHTS_0']).astype(np.float64)
            Ph = np.c_[P, np.ones(len(P))]; out = np.zeros((len(P), 4)); on = np.zeros((len(P), 3))
            for c in range(4):
                for k in range(len(joints)):
                    sel = (J[:, c] == k) & (W[:, c] > 0)
                    if sel.any(): out[sel] += W[sel, c:c + 1] * (Ph[sel] @ JM[k].T); on[sel] += W[sel, c:c + 1] * (N[sel] @ JM[k][:3, :3].T)
            P, N = out[:, :3], on
        else:
            M = g.world(ni); P = (np.c_[P, np.ones(len(P))] @ M.T)[:, :3]; N = N @ np.linalg.inv(M[:3, :3]).T
        N /= np.maximum(1e-9, np.linalg.norm(N, axis=1, keepdims=True))
        parts[key] = dict(P=P, N=N, UV=UV, I=I, mat=pr.get('material'))
    hub = g.world(names['Rotor_Main_01'])[:3, 3]; tail = g.world(names['Rotor_Rear_02'])[:3, 3]
    ymin = min(p['P'][:, 1].min() for p in parts.values())
    rot = parts['wirnik_glowny']['P']; s = ROTOR_D / (rot[:, 0].max() - rot[:, 0].min())
    O = np.array([0.0, ymin, hub[2]]); F = np.array([-1.0, 1.0, -1.0])
    for p in parts.values():
        p['P'] = (p['P'] - O) * s * F; p['N'] = p['N'] * F
    piv = {'wirnik_glowny': (hub - O) * s * F, 'wirnik_ogonowy': (tail - O) * s * F}
    return g, parts, piv, s

if __name__ == '__main__':
    import sys
    g, parts, piv, s = load(sys.argv[1])
    print('scale m/unit', s)
    for k, p in parts.items(): print(k, len(p['I']) // 3, 'tris  min', np.round(p['P'].min(0), 2), 'max', np.round(p['P'].max(0), 2), 'mat', p['mat'])
    print('pivots', {k: np.round(v, 3) for k, v in piv.items()})
    # side profile of the fuselage (kadlub + kadlub_tyl): y range of side-facing vertices per z slice
    P = np.r_[parts['kadlub']['P'], parts['kadlub_tyl']['P']]
    for z in np.arange(-6.8, 4.0, .4):
        sel = (np.abs(P[:, 2] - z) < .2)
        if sel.any(): print('z %5.1f  y %4.2f..%4.2f  |x|max %4.2f' % (z, P[sel, 1].min(), P[sel, 1].max(), np.abs(P[sel, 0]).max()))
