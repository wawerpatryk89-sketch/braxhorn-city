"""MIASTO R1 18.11 — prosty odczyt / zapis GLB (jedna siatka Tripo: POSITION, NORMAL, TEXCOORD_0, indeksy, 3 obrazy:
normalna, kolor, metal-szorstkość) dla pojazdów właściciela (pojazd_wlasciciela.py)."""
import io
import json
import struct

import numpy as np


def czytaj(path):
    b = open(path, "rb").read(); jl = struct.unpack("<I", b[12:16])[0]; J = json.loads(b[20:20 + jl]); bin0 = 20 + jl + 8
    def acc(i, typ, n):
        a = J["accessors"][i]; bv = J["bufferViews"][a["bufferView"]]; off = bin0 + bv.get("byteOffset", 0) + a.get("byteOffset", 0)
        dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16}[a["componentType"]]
        return np.frombuffer(b, dt, a["count"] * n, off).reshape(-1, n) if n > 1 else np.frombuffer(b, dt, a["count"], off)
    p = J["meshes"][0]["primitives"][0]
    V = acc(p["attributes"]["POSITION"], 0, 3).astype(np.float64)
    N = acc(p["attributes"]["NORMAL"], 0, 3).astype(np.float64) if "NORMAL" in p["attributes"] else None
    U = acc(p["attributes"]["TEXCOORD_0"], 0, 2).astype(np.float64)
    I = acc(p["indices"], 0, 1).astype(np.int64).reshape(-1, 3)
    node = J["nodes"][0]
    if "translation" in node: V = V + np.array(node["translation"])
    m = J["materials"][0]; pbr = m.get("pbrMetallicRoughness", {})
    obrazy = []
    for im in J["images"]:
        bv = J["bufferViews"][im["bufferView"]]; obrazy.append((im.get("mimeType", "image/jpeg"), b[bin0 + bv.get("byteOffset", 0):bin0 + bv.get("byteOffset", 0) + bv["byteLength"]]))
    tex = {"normal": J["textures"][m["normalTexture"]["index"]]["source"] if "normalTexture" in m else None,
           "base": J["textures"][pbr["baseColorTexture"]["index"]]["source"],
           "mr": J["textures"][pbr["metallicRoughnessTexture"]["index"]]["source"] if "metallicRoughnessTexture" in pbr else None}
    return {"V": V, "N": N, "U": U, "I": I, "obrazy": obrazy, "tex": tex, "nazwa": m.get("name", "")}


def pisz(path, nazwa, czesci, tex_uris, metal=1.0, rough=1.0):
    """czesci: lista (nazwa węzła, V, N, U, I) — każda część osobnym węzłem (koła osobno); wspólny materiał z teksturami
    zewnętrznymi (tex_uris: base / normal / mr, względem pliku)."""
    buf = bytearray(); bvs = []; accs = []; nodes = []; meshes = []
    def dodaj(arr, typ, comp, target=None, mm=False):
        nonlocal buf
        while len(buf) % 4: buf += b"\0"
        off = len(buf); data = arr.tobytes(); buf += data
        bv = {"buffer": 0, "byteOffset": off, "byteLength": len(data)}
        if target: bv["target"] = target
        bvs.append(bv)
        a = {"bufferView": len(bvs) - 1, "componentType": comp, "count": int(arr.shape[0]), "type": typ}
        if mm: a["min"] = [float(x) for x in arr.min(0)]; a["max"] = [float(x) for x in arr.max(0)]
        accs.append(a); return len(accs) - 1
    for (n, V, N, U, I, t) in czesci:
        pa = dodaj(V.astype(np.float32), "VEC3", 5126, 34962, True)
        na = dodaj(N.astype(np.float32), "VEC3", 5126, 34962)
        ua = dodaj(U.astype(np.float32), "VEC2", 5126, 34962)
        ia = dodaj(I.astype(np.uint32).reshape(-1), "SCALAR", 5125, 34963)
        meshes.append({"name": n, "primitives": [{"attributes": {"POSITION": pa, "NORMAL": na, "TEXCOORD_0": ua}, "indices": ia, "material": 0}]})
        node = {"name": n, "mesh": len(meshes) - 1}
        if t is not None: node["translation"] = [float(x) for x in t]
        nodes.append(node)
    root = {"name": nazwa, "children": list(range(len(nodes)))}
    nodes.append(root)
    J = {"asset": {"version": "2.0", "generator": "braxhorn pojazd_wlasciciela.py"}, "scene": 0, "scenes": [{"nodes": [len(nodes) - 1]}],
         "nodes": nodes, "meshes": meshes, "accessors": accs, "bufferViews": bvs, "buffers": [{"byteLength": len(buf)}],
         "materials": [{"name": "braxhorn " + nazwa, "doubleSided": True,
                        "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 2},
                                                 "metallicFactor": metal, "roughnessFactor": rough},
                        "normalTexture": {"index": 1}}],
         "textures": [{"source": 0, "sampler": 0}, {"source": 1, "sampler": 0}, {"source": 2, "sampler": 0}],
         "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}],
         "images": [{"uri": tex_uris["base"]}, {"uri": tex_uris["normal"]}, {"uri": tex_uris["mr"]}]}
    js = json.dumps(J, separators=(",", ":")).encode()
    while len(js) % 4: js += b" "
    while len(buf) % 4: buf += b"\0"
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(buf)) + struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(buf), 0x004E4942) + bytes(buf)
    open(path, "wb").write(out)


def pisz_wezly(path, nazwa, wezly, materialy, katalog_tekstur=""):
    """Wiele węzłów z hierarchią i wiele materiałów (pojazdy właściciela: nadwozie, naklejki, koła ze skrętem).
    wezly: lista (nazwa, nazwa rodzica | None, translacja | None, [(indeks materiału, V, N, UV, I), …]) — prymityw na materiał.
    materialy: dict(name, tex=(base, normal | None, mr | None) względem katalog_tekstur | color=(r, g, b) liniowo,
    metal, rough, double, mask=alphaCutoff)."""
    buf = bytearray(); bvs = []; accs = []; nodes = []; meshes = []; images = []; textures = []
    def dodaj(arr, typ, comp, target=None, mm=False):
        nonlocal buf
        while len(buf) % 4: buf += b"\0"
        off = len(buf); data = arr.tobytes(); buf += data
        bv = {"buffer": 0, "byteOffset": off, "byteLength": len(data)}
        if target: bv["target"] = target
        bvs.append(bv)
        a = {"bufferView": len(bvs) - 1, "componentType": comp, "count": int(arr.shape[0]), "type": typ}
        if mm: a["min"] = [float(x) for x in arr.min(0)]; a["max"] = [float(x) for x in arr.max(0)]
        accs.append(a); return len(accs) - 1
    def tekstura(plik):
        uri = katalog_tekstur + plik
        for i, im in enumerate(images):
            if im["uri"] == uri: return next(j for j, t in enumerate(textures) if t["source"] == i)
        images.append({"uri": uri}); textures.append({"source": len(images) - 1, "sampler": 0}); return len(textures) - 1
    mats = []
    for m in materialy:
        pbr = {"metallicFactor": float(m.get("metal", 0.0)), "roughnessFactor": float(m.get("rough", 1.0))}
        mat = {"name": m["name"], "pbrMetallicRoughness": pbr}
        if "tex" in m:
            base, normal, mr = m["tex"]
            pbr["baseColorTexture"] = {"index": tekstura(base)}
            if mr: pbr["metallicRoughnessTexture"] = {"index": tekstura(mr)}
            if normal: mat["normalTexture"] = {"index": tekstura(normal)}
        else:
            c = m.get("color", (1, 1, 1)); pbr["baseColorFactor"] = [float(c[0]), float(c[1]), float(c[2]), 1.0]
        if m.get("double"): mat["doubleSided"] = True
        if "mask" in m: mat["alphaMode"] = "MASK"; mat["alphaCutoff"] = float(m["mask"])
        mats.append(mat)
    idx = {}
    for (n, rodzic, t, prym) in wezly:
        node = {"name": n}
        if t is not None: node["translation"] = [float(x) for x in t]
        if prym:
            prims = []
            for (mi, V, N, UV, I) in prym:
                pa = dodaj(np.asarray(V, np.float32), "VEC3", 5126, 34962, True)
                na = dodaj(np.asarray(N, np.float32), "VEC3", 5126, 34962)
                ua = dodaj(np.asarray(UV, np.float32), "VEC2", 5126, 34962)
                ia = dodaj(np.asarray(I, np.uint32).reshape(-1), "SCALAR", 5125, 34963)
                prims.append({"attributes": {"POSITION": pa, "NORMAL": na, "TEXCOORD_0": ua}, "indices": ia, "material": int(mi)})
            meshes.append({"name": n, "primitives": prims}); node["mesh"] = len(meshes) - 1
        idx[n] = len(nodes); nodes.append(node)
    for (n, rodzic, t, prym) in wezly:
        if rodzic: nodes[idx[rodzic]].setdefault("children", []).append(idx[n])
    korzenie = [idx[n] for (n, rodzic, t, prym) in wezly if not rodzic]
    nodes.append({"name": nazwa, "children": korzenie})
    J = {"asset": {"version": "2.0", "generator": "braxhorn glb_proste.pisz_wezly"}, "scene": 0, "scenes": [{"nodes": [len(nodes) - 1]}],
         "nodes": nodes, "meshes": meshes, "accessors": accs, "bufferViews": bvs, "buffers": [{"byteLength": len(buf)}],
         "materials": mats, "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}]}
    if images: J["images"] = images; J["textures"] = textures
    js = json.dumps(J, separators=(",", ":"), ensure_ascii=False).encode()
    while len(js) % 4: js += b" "
    while len(buf) % 4: buf += b"\0"
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(buf)) + struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(buf), 0x004E4942) + bytes(buf)
    open(path, "wb").write(out)
