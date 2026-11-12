#!/usr/bin/env python3
"""Owner's rigged Tripo characters (12.11 02:20: "dziwka ze szkieletem. Ma się ruszać po mieście"): Mixamo-named skeleton
in T-pose, no animation. This bakes real motion onto the character's OWN bones and writes it into the GLB, so Unity
(glTFast, Legacy clips) plays it without any retargeting at runtime:

* source motion: the game's own Mixamo clips on Nyxa (public/assets/chars/nyxa.glb.gz: Walking, Idle ...), the same
  clips the browser version plays;
* retarget per frame through WORLD-SPACE rotation deltas from the rest pose: D = Ws(t) * Ws0^-1, Wt(t) = A D A^-1 * Wt0,
  local = parent(t)^-1 * Wt(t) - immune to the different local bone axes of the two rigs (Tripo hips -90 deg about Y,
  Mixamo hips 90/-90); A turns the source's facing onto the character's facing (toes);
* the hips: vertical bob and sway scaled by the hips-height ratio, in place (the game moves the character);
* bones the source lacks (finger tips, toe ends) keep their rest pose.

usage: postac_animacja.py <character.glb> <out.glb> [clip ...]     (default clips: Walking Idle)
"""
import gzip
import json
import struct
import sys
from pathlib import Path

import numpy as np
from scipy.spatial.transform import Rotation as Rot, Slerp

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "public/assets/chars/nyxa.glb.gz"


def load(path):
    d = Path(path).read_bytes()
    if str(path).endswith(".gz"):
        d = gzip.decompress(d)
    L = struct.unpack("<I", d[12:16])[0]
    J = json.loads(d[20:20 + L]); off = 20 + L
    bl = struct.unpack("<I", d[off:off + 4])[0]
    return J, bytearray(d[off + 8:off + 8 + bl])


def accessor(J, B, i):
    ac = J["accessors"][i]; bv = J["bufferViews"][ac["bufferView"]]
    n = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}[ac["type"]]
    ct = {5126: np.float32, 5123: np.uint16, 5121: np.uint8, 5122: np.int16, 5120: np.int8}[ac["componentType"]]
    a = np.frombuffer(bytes(B), ct, ac["count"] * n, bv.get("byteOffset", 0) + ac.get("byteOffset", 0)).reshape(ac["count"], n)
    if ac.get("normalized"):
        a = a.astype(np.float32) / {5122: 32767, 5120: 127, 5123: 65535, 5121: 255}[ac["componentType"]]
    return a.astype(np.float64)


def key(name):
    return (name or "").replace("mixamorig", "").replace(":", "").replace("_", "").lower()


class Skel:
    def __init__(self, J):
        self.J = J; self.nodes = J["nodes"]; self.par = {}
        for i, n in enumerate(self.nodes):
            for c in n.get("children", []):
                self.par[c] = i
        self.names = [n.get("name", "") for n in self.nodes]
        self.rest_r = [Rot.from_quat(n.get("rotation", [0, 0, 0, 1])) for n in self.nodes]
        self.rest_t = [np.array(n.get("translation", [0, 0, 0]), float) for n in self.nodes]
        self.rest_s = [np.array(n.get("scale", [1, 1, 1]), float) for n in self.nodes]

    def chain(self, i):
        c = [i]
        while c[-1] in self.par:
            c.append(self.par[c[-1]])
        return c[::-1]

    def world(self, i, local_r=None, local_t=None):
        """world rotation and position of node i; local_r / local_t: dict node -> override"""
        R = Rot.identity(); P = np.zeros(3); S = np.ones(3)
        for j in self.chain(i):
            r = local_r.get(j, self.rest_r[j]) if local_r else self.rest_r[j]
            t = local_t.get(j, self.rest_t[j]) if local_t else self.rest_t[j]
            P = P + R.apply(S * t); R = R * r; S = S * self.rest_s[j]
        return R, P

    def find(self, k):
        for i, n in enumerate(self.names):
            if key(n) == k:
                return i
        return None


def basis(sk, lr=None, lt=None):
    """frame of a skeleton pose: columns right, up, forward (any up axis: the browser rigs are bound Z-up)"""
    P = lambda k: sk.world(sk.find(k), lr, lt)[1]
    up = P("head") - P("hips"); up /= np.linalg.norm(up)
    right = P("rightupleg") - P("leftupleg"); right -= up * np.dot(right, up); right /= np.linalg.norm(right)
    fwd = np.cross(right, up)
    toes = sum(P(s + "toebase") - P(s + "foot") for s in ("left", "right"))
    if np.dot(toes, fwd) < 0:                          # forward = where the toes point
        fwd = -fwd; right = -right
    return np.stack([right, up, fwd], 1)


def facing(sk):
    """horizontal forward of a rest skeleton: from the feet to the toes"""
    f = np.zeros(3)
    for side in ("left", "right"):
        a, b = sk.find(side + "foot"), sk.find(side + "toebase")
        if a is not None and b is not None:
            d = sk.world(b)[1] - sk.world(a)[1]; d[1] = 0
            if np.linalg.norm(d) > 1e-9:
                f += d / np.linalg.norm(d)
    return f / np.linalg.norm(f)


def sample_clip(J, B, sk, name):
    a = next(x for x in J["animations"] if x.get("name") == name)
    rot, tra, times = {}, {}, None
    for c in a["channels"]:
        s = a["samplers"][c["sampler"]]; t = accessor(J, B, s["input"])[:, 0]; v = accessor(J, B, s["output"])
        node = c["target"]["node"]; path = c["target"]["path"]
        if path == "rotation":
            rot[node] = (t, v)
        elif path == "translation":
            tra[node] = (t, v)
        times = t if times is None or len(t) > len(times) else times
    return a, rot, tra, times


def interp_rot(t, v, x):
    if len(t) == 1:
        return Rot.from_quat(v[0])
    return Slerp(t, Rot.from_quat(v))(np.clip(x, t[0], t[-1]))


def interp_vec(t, v, x):
    return np.array([np.interp(x, t, v[:, k]) for k in range(3)])


def retarget(src_J, src_B, tgt_J, clip, fps=30):
    S = Skel(src_J); T = Skel(tgt_J)
    a, rot, tra, times = sample_clip(src_J, src_B, S, clip)
    dur = float(times[-1] - times[0]); n = max(2, int(round(dur * fps)) + 1)
    xs = np.linspace(times[0], times[-1], n)
    # bone mapping (target node -> source node)
    src_by_key = {key(nm): i for i, nm in enumerate(S.names) if "mixamorig" in (nm or "")}
    mapping = {i: src_by_key[key(nm)] for i, nm in enumerate(T.names) if "mixamorig" in (nm or "") and key(nm) in src_by_key}
    order = sorted(mapping, key=lambda i: len(T.chain(i)))
    # facing alignment source -> target (yaw only)
    # The browser rigs are BOUND lying (Z-up) while their clips stand Y-up: the source rest is turned into the clips'
    # frame by G, measured on the standing Idle (frame 0), so a bone's delta is motion, not the bind/clip axis swap.
    # (the Walking cycle averaged: symmetric, upright - Nyxa's Idle is a turned fighting stance)
    _, wrot, wtra, wtimes = sample_clip(src_J, src_B, S, "Walking")
    acc = np.zeros((3, 3))
    for x in np.linspace(wtimes[0], wtimes[-1], 24, endpoint=False):
        acc += basis(S, {j: interp_rot(*wrot[j], x) for j in wrot}, {j: interp_vec(*wtra[j], x) for j in wtra})
    U, _, Vt = np.linalg.svd(acc); Bclip = U @ Vt
    if np.linalg.det(Bclip) < 0:
        Bclip[:, 0] *= -1
    lr0 = {j: interp_rot(*wrot[j], wtimes[0]) for j in wrot}; lt0 = {j: interp_vec(*wtra[j], wtimes[0]) for j in wtra}
    Bbind = basis(S)
    G = Rot.from_matrix(Bclip @ Bbind.T)
    Bs, Bt = Bclip, basis(T)
    A = Rot.from_matrix(Bt @ Bs.T)                      # source (clip frame) -> character rest frame
    ft = Bt[:, 2]
    src_rest_w = {j: G * S.world(j)[0] for j in mapping.values()}
    tgt_rest_w = {i: T.world(i)[0] for i in mapping}
    # rest poses differ (Nyxa's bind arms are not the character's T-pose): every bone of the character is first turned
    # so it points where the source bone points at rest (direction to its first mapped child), then gets the motion
    child = {}
    for i in mapping:
        for c in T.nodes[i].get("children", []):
            if c in mapping:
                child[i] = c; break
    for i in sorted(mapping, key=lambda i: len(T.chain(i))):
        if i not in child or key(T.names[i]) == "hips":
            continue
        j, jc = mapping[i], mapping[child[i]]
        ds = A.apply(G.apply(S.world(jc)[1] - S.world(j)[1]))
        dt = T.world(child[i])[1] - T.world(i)[1]
        if np.linalg.norm(ds) < 1e-9 or np.linalg.norm(dt) < 1e-9:
            continue
        ds /= np.linalg.norm(ds); dt /= np.linalg.norm(dt)
        ax = np.cross(dt, ds); sn = np.linalg.norm(ax); cs = np.dot(dt, ds)
        if sn < 1e-9:
            continue
        Q = Rot.from_rotvec(ax / sn * np.arctan2(sn, cs))
        tgt_rest_w[i] = Q * tgt_rest_w[i]
    hs, ht = S.find("hips"), T.find("hips")
    us, ut = Bbind[:, 1], Bt[:, 1]
    h_src = np.dot(S.world(hs)[1], us) - min(np.dot(S.world(S.find(f + "toebase"))[1], us) for f in ("left", "right"))
    h_tgt = np.dot(T.world(ht)[1], ut) - min(np.dot(T.world(T.find(f + "toebase"))[1], ut) for f in ("left", "right"))
    scale = h_tgt / h_src
    src_hips_rest = S.world(hs, lr0, lt0)[1]                # standing hips (Idle frame 0), clip frame
    out_r = {i: [] for i in mapping}; out_hips_t = []
    for x in xs:
        lr = {j: interp_rot(*rot[j], x) for j in rot}
        lt = {j: interp_vec(*tra[j], x) for j in tra}
        Ws = {}
        for j in sorted(set(mapping.values()), key=lambda j: len(S.chain(j))):
            Ws[j] = S.world(j, lr, lt)
        Wt = {}
        for i in order:
            j = mapping[i]
            D = Ws[j][0] * src_rest_w[j].inv()
            Wt[i] = (A * D * A.inv()) * tgt_rest_w[i]
            p = T.par.get(i)
            if p in Wt:
                parent_w = Wt[p]
            else:
                parent_w = T.world(p)[0] if p is not None else Rot.identity()
            out_r[i].append((parent_w.inv() * Wt[i]).as_quat())
        # hips: in place (no travel along the facing), bob and sway scaled
        d = A.apply(Ws[hs][1] - src_hips_rest) * scale
        fwd = ft / np.linalg.norm(ft); d = d - fwd * np.dot(d, fwd)
        p = T.par.get(ht); pr, _ = T.world(p) if p is not None else (Rot.identity(), None)
        ps = np.ones(3)
        for j in (T.chain(p) if p is not None else []):
            ps = ps * T.rest_s[j]
        out_hips_t.append(T.rest_t[ht] + pr.inv().apply(d) / ps)
    for i in out_r:                                     # continuous quaternions (no flips between keys)
        q = np.array(out_r[i])
        for k in range(1, len(q)):
            if np.dot(q[k], q[k - 1]) < 0:
                q[k] = -q[k]
        out_r[i] = q
    retarget.scale = scale                              # character units per source unit (hips height ratio)
    return xs - xs[0], out_r, np.array(out_hips_t), ht, len(mapping)


def write(tgt_J, tgt_B, clips, out):
    J = json.loads(json.dumps(tgt_J)); B = bytearray(tgt_B)
    J.setdefault("animations", [])

    def add(arr, typ):
        arr = np.ascontiguousarray(arr, np.float32); pad = (-len(B)) % 4
        B.extend(b"\0" * pad); off = len(B); B.extend(arr.tobytes())
        J["bufferViews"].append({"buffer": 0, "byteOffset": off, "byteLength": arr.nbytes})
        acc = {"bufferView": len(J["bufferViews"]) - 1, "componentType": 5126, "count": len(arr), "type": typ}
        if typ == "SCALAR":
            acc["min"] = [float(arr.min())]; acc["max"] = [float(arr.max())]
        J["accessors"].append(acc)
        return len(J["accessors"]) - 1
    for name, (t, rots, hips_t, hips_node, _) in clips.items():
        ti = add(t.reshape(-1, 1), "SCALAR"); samplers = []; channels = []
        for node, q in rots.items():
            samplers.append({"input": ti, "output": add(q, "VEC4"), "interpolation": "LINEAR"})
            channels.append({"sampler": len(samplers) - 1, "target": {"node": node, "path": "rotation"}})
        samplers.append({"input": ti, "output": add(hips_t, "VEC3"), "interpolation": "LINEAR"})
        channels.append({"sampler": len(samplers) - 1, "target": {"node": hips_node, "path": "translation"}})
        J["animations"].append({"name": name, "samplers": samplers, "channels": channels})
    J["buffers"][0]["byteLength"] = len(B)
    js = json.dumps(J, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4)
    B.extend(b"\0" * ((-len(B)) % 4))
    Path(out).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(B)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                          + struct.pack("<II", len(B), 0x004E4942) + bytes(B))


def main(char, out, names):
    sJ, sB = load(SOURCE); tJ, tB = load(char)
    clips = {}
    for n in names:
        clips[n] = retarget(sJ, sB, tJ, n)
        print(json.dumps({"clip": n, "seconds": round(float(clips[n][0][-1]), 3), "frames": len(clips[n][0]), "bones": clips[n][4]}))
    if "Walking" in clips:
        # "Idle" of the city: standing still in the walk's own contact pose (Nyxa's Idle is a fighting stance)
        t, rots, hips, hn, nb = clips["Walking"]
        clips["Idle"] = (np.array([0.0, 2.0]), {i: np.stack([q[0], q[0]]) for i, q in rots.items()}, np.stack([hips[0], hips[0]]), hn, nb)
    write(tJ, tB, clips, out)
    # strides of the browser clips (public/assets/chars/clips.json, source units per cycle) in the character's units/s
    strides = json.loads((ROOT / "public/assets/chars/clips.json").read_text())
    speed = {n: round(strides[n]["stride"] * retarget.scale / strides[n]["duration"], 5) for n in clips if n in strides and "stride" in strides[n]}
    meta = {"clips": {n: round(float(c[0][-1]), 3) for n, c in clips.items()}, "speed_units_per_s": speed,
            "source": "public/assets/chars/nyxa.glb.gz (Walking)", "tool": "unity-tools/sklep/postac_animacja.py"}
    Path(str(out) + ".json").write_text(json.dumps(meta, indent=1) + "\n")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3:] or ["Walking", "Idle"])


def drop_backdrop(src, out):
    """Tripo sometimes leaves a flat board behind the figure (owner's 4th character, 12.11 02:33): every connected piece
    that is a thin slab or strip (thinner than 3 % of the figure, longer than 40 % of it) is cut out of the
    index buffer; the vertices, skin and textures stay as they are."""
    J, B = load(src)
    from split_sheet import components
    pr = J["meshes"][0]["primitives"][0]
    P = accessor(J, B, pr["attributes"]["POSITION"]); I = accessor(J, B, pr["indices"]).astype(np.int64).reshape(-1, 3)
    comp = components(I, len(P)); tc = comp[I[:, 0]]
    H = P[:, 1].max() - P[:, 1].min(); keep = np.ones(len(I), bool); dropped = []
    for c in np.unique(tc):
        v = P[np.unique(I[tc == c].reshape(-1))]; ext = np.sort(v.max(0) - v.min(0))
        if ext[0] < .03 * H and ext[2] > .4 * H:      # the board and the thin rim strips round it
            keep[tc == c] = False; dropped.append((int((tc == c).sum()), ext.round(3).tolist()))
    if dropped:                                        # rim fragments lying in the board's plane (small pieces only)
        bv = P[np.unique(I[~keep].reshape(-1))]; cen = bv.mean(0); nrm = np.linalg.svd(bv - cen)[2][2]
        for c in np.unique(tc[keep]):
            sel = tc == c
            if sel.sum() > 2000:
                continue
            v = P[np.unique(I[sel].reshape(-1))]
            if np.abs((v - cen) @ nrm).max() < .015 * H:
                keep[sel] = False; dropped.append((int(sel.sum()), "in board plane"))
    idx = np.ascontiguousarray(I[keep].reshape(-1), np.uint32)
    pad = (-len(B)) % 4; B.extend(b"\0" * pad); off = len(B); B.extend(idx.tobytes())
    J["bufferViews"].append({"buffer": 0, "byteOffset": off, "byteLength": idx.nbytes, "target": 34963})
    J["accessors"].append({"bufferView": len(J["bufferViews"]) - 1, "componentType": 5125, "count": int(len(idx)), "type": "SCALAR"})
    pr["indices"] = len(J["accessors"]) - 1
    J["buffers"][0]["byteLength"] = len(B)
    js = json.dumps(J, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4); B.extend(b"\0" * ((-len(B)) % 4))
    Path(out).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(B)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                          + struct.pack("<II", len(B), 0x004E4942) + bytes(B))
    print(json.dumps({"backdrop_dropped": dropped}))
