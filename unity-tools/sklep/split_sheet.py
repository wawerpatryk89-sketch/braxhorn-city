#!/usr/bin/env python3
"""Cut an owner's Tripo "sheet" (one model with N products laid out in rows, one texture atlas) into separate shop products
for the whole game (owner 11.11.2026 22:44: "tworzę ciuchy, torebki, buty i robimy to globalnie na całą grę do sklepów").

usage: split_sheet.py <sheet.glb> <out dir> <spec.json>

spec.json: {"pack": "sklep_ubrania_01", "rows": 2, "cols": 5, "atlas": 2048, "mr": 1024,
            "metallic": "<metallic.png>", "roughness": "<roughness.png>",
            "items": [{"name": "ubranie_polo_granat", "height": 0.70, "pivot": "top", "kind": "koszulka"}, ...]}  # row by row, left to right

Every product: its own .glb (real metres, the pivot at its top centre for hangers or its base centre for shelves, its
front +Z), all of them pointing at the SAME textures in _tekstury/ (base colour JPEG, normal PNG, metallic-roughness PNG at
the given sizes), so a shop of hundreds of pieces costs one texture set. The input sheet is the owner's file (CC by the
owner, never published plain: the pack is encrypted). Pure Python + numpy + Pillow; no Blender needed.
"""
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def read_glb(path):
    b = Path(path).read_bytes()
    jl = struct.unpack("<I", b[12:16])[0]
    g = json.loads(b[20:20 + jl])
    off = 20 + jl
    bl = struct.unpack("<I", b[off:off + 4])[0]
    bin0 = b[off + 8:off + 8 + bl]

    def acc(i):
        a = g["accessors"][i]; bv = g["bufferViews"][a["bufferView"]]
        n = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a["componentType"]]
        start = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
        arr = np.frombuffer(bin0, dt, a["count"] * n, start)
        return arr.reshape(a["count"], n) if n > 1 else arr

    def image(i):
        im = g["images"][i]; bv = g["bufferViews"][im["bufferView"]]
        s = bv.get("byteOffset", 0)
        return bin0[s:s + bv["byteLength"]]

    return g, acc, image


def components(tri, nv):
    parent = np.arange(nv)

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]; x = parent[x]
        return x
    for a, b_, c in tri:
        ra, rb, rc = find(a), find(b_), find(c)
        parent[rb] = ra; parent[find(rc)] = ra
    return np.array([find(i) for i in range(nv)])


def write_glb(path, name, pos, nrm, uv, idx, tex_uris, metallic_factor=1.0):
    pos = pos.astype(np.float32); nrm = nrm.astype(np.float32); uv = uv.astype(np.float32)
    idx = idx.astype(np.uint32 if len(pos) > 65535 else np.uint16)
    blobs, views, accs = [], [], []

    def add(arr, target, typ, ctype, minmax=False):
        data = arr.tobytes(); pad = (-len(data)) % 4
        off = sum(len(x) for x in blobs)
        blobs.append(data + b"\0" * pad)
        views.append({"buffer": 0, "byteOffset": off, "byteLength": len(data), **({"target": target} if target else {})})
        a = {"bufferView": len(views) - 1, "componentType": ctype, "count": len(arr), "type": typ}
        if minmax: a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1
    ip = add(pos, 34962, "VEC3", 5126, True); inn = add(nrm, 34962, "VEC3", 5126); iu = add(uv, 34962, "VEC2", 5126)
    ii = add(idx.reshape(-1), 34963, "SCALAR", 5125 if idx.dtype == np.uint32 else 5123)
    g = {"asset": {"version": "2.0", "generator": "BRAXHORN split_sheet.py"},
         "scene": 0, "scenes": [{"nodes": [0]}], "nodes": [{"name": name, "mesh": 0}],
         "meshes": [{"name": name, "primitives": [{"attributes": {"POSITION": ip, "NORMAL": inn, "TEXCOORD_0": iu}, "indices": ii, "material": 0}]}],
         "materials": [{"name": "braxhorn " + Path(tex_uris["base"]).stem, "doubleSided": True,
                        "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 2},
                                                 "metallicFactor": metallic_factor, "roughnessFactor": 1.0},
                        "normalTexture": {"index": 1}}],
         "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}],
         "images": [{"uri": tex_uris["base"]}, {"uri": tex_uris["normal"]}, {"uri": tex_uris["mr"]}],
         "textures": [{"sampler": 0, "source": 0}, {"sampler": 0, "source": 1}, {"sampler": 0, "source": 2}],
         "bufferViews": views, "accessors": accs, "buffers": [{"byteLength": sum(len(x) for x in blobs)}]}
    js = json.dumps(g, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4)
    bin_ = b"".join(blobs)
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(bin_))
    out += struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(bin_), 0x004E4942) + bin_
    Path(path).write_bytes(out)


def main(sheet, out_dir, spec_path):
    spec = json.loads(Path(spec_path).read_text())
    out = Path(out_dir); tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True)
    g, acc, image = read_glb(sheet)
    prim = g["meshes"][0]["primitives"][0]
    P = acc(prim["attributes"]["POSITION"]).astype(np.float64); N = acc(prim["attributes"]["NORMAL"]).astype(np.float64)
    U = acc(prim["attributes"]["TEXCOORD_0"]).astype(np.float64); I = acc(prim["indices"]).reshape(-1, 3).astype(np.int64)
    # the textures: base colour (JPEG), the normal map, metallic (B) + roughness (G) in one map
    pack = spec["pack"]; A, MRs = spec.get("atlas", 2048), spec.get("mr", 1024)
    mat = g["materials"][0]
    def img_of(texinfo):
        from io import BytesIO
        return Image.open(BytesIO(image(g["textures"][texinfo["index"]]["source"])))
    base = img_of(mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB").resize((A, A), Image.LANCZOS)
    base.save(tex / (pack + "_kolor.jpg"), quality=90)
    img_of(mat["normalTexture"]).convert("RGB").resize((A, A), Image.LANCZOS).save(tex / (pack + "_normal.png"), optimize=True)
    rough = Image.open(spec["roughness"]).convert("L").resize((MRs, MRs), Image.LANCZOS) if spec.get("roughness") else Image.new("L", (MRs, MRs), 230)
    metal = Image.open(spec["metallic"]).convert("L").resize((MRs, MRs), Image.LANCZOS) if spec.get("metallic") else Image.new("L", (MRs, MRs), 0)
    Image.merge("RGB", (Image.new("L", (MRs, MRs), 255), rough, metal)).save(tex / (pack + "_mr.png"), optimize=True)
    uris = {"base": "_tekstury/" + pack + "_kolor.jpg", "normal": "_tekstury/" + pack + "_normal.png", "mr": "_tekstury/" + pack + "_mr.png"}
    # the pieces: connected components, each assigned to the grid cell of its centre (rows top to bottom, columns left to right)
    comp = components(I, len(P))
    tri_comp = comp[I[:, 0]]
    lo, hi = P.min(0), P.max(0); rows, cols = spec["rows"], spec["cols"]
    # the products stand apart on the sheet: the front view (x, y) rasterised at 2 mm, the separate silhouettes labelled;
    # the silhouettes sorted into rows (top to bottom) and columns (left to right); a component (a sleeve, a collar, a
    # button) goes to the silhouette most of its vertices fall in
    from PIL import ImageDraw
    from scipy import ndimage
    res = spec.get("raster", .001)
    W_ = int((hi[0] - lo[0]) / res) + 3; H_ = int((hi[1] - lo[1]) / res) + 3
    def px(v): return ((v[0] - lo[0]) / res + 1, (hi[1] - v[1]) / res + 1)
    mask = Image.new("L", (W_, H_), 0); dr = ImageDraw.Draw(mask)
    for a, b_, c in I: dr.polygon([px(P[a]), px(P[b_]), px(P[c])], fill=255)
    sil = np.array(mask) > 0
    # products touching in the front view (a sleeve against the next one) are cut apart by eroding the silhouettes until
    # the expected number of separate ones appears; every pixel then takes the nearest label
    lab, nlab = ndimage.label(sil)
    for it in range(1, 40):
        if nlab >= rows * cols and sorted(ndimage.sum(np.ones_like(lab), lab, range(1, nlab + 1)))[-rows * cols] > 2000: break
        lab, nlab = ndimage.label(ndimage.binary_erosion(sil, iterations=it))
    print("erosion", it, "silhouettes", nlab)
    idx = ndimage.distance_transform_edt(lab == 0, return_distances=False, return_indices=True)
    lab = lab[idx[0], idx[1]]
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, nlab + 1))
    big = [i + 1 for i in np.argsort(-sizes)[:rows * cols]]
    print("silhouettes", nlab, "kept", len(big), "smallest kept px", int(min(sizes[b - 1] for b in big)))
    cy_ = {b: ndimage.center_of_mass(lab == b) for b in big}          # (row px, col px)
    order = sorted(big, key=lambda b: cy_[b][0])
    cell_of_label = {}
    for r in range(rows):
        for k, b in enumerate(sorted(order[r * cols:(r + 1) * cols], key=lambda b: cy_[b][1])): cell_of_label[b] = r * cols + k
    vx = np.clip(((P[:, 0] - lo[0]) / res + 1).astype(int), 0, W_ - 1); vy = np.clip(((hi[1] - P[:, 1]) / res + 1).astype(int), 0, H_ - 1)
    vlab = lab[vy, vx]
    cell_of_comp = {}
    for cid in np.unique(tri_comp):
        vs = np.unique(I[tri_comp == cid].reshape(-1)); ls = [cell_of_label[l] for l in vlab[vs] if l in cell_of_label]
        cell_of_comp[cid] = int(np.bincount(ls).argmax()) if ls else -1
    tri_cell = np.array([cell_of_comp[c] for c in tri_comp])
    report = []
    for cell, item in enumerate(spec["items"]):
        tris = I[tri_cell == cell]
        if len(tris) == 0:
            print("EMPTY", item["name"]); continue
        vs, inv = np.unique(tris.reshape(-1), return_inverse=True)
        p, n, u = P[vs].copy(), N[vs], U[vs]
        mn, mx = p.min(0), p.max(0); h = mx[1] - mn[1]
        s = item["height"] / h
        pivot = np.array([(mn[0] + mx[0]) * .5, mx[1] if item.get("pivot", "top") == "top" else mn[1], (mn[2] + mx[2]) * .5])
        p = (p - pivot) * s
        write_glb(out / (item["name"] + ".glb"), item["name"], p, n, u, inv.reshape(-1, 3), uris, spec.get("metallicFactor", 1.0))
        # the rail version: a garment on a hanger hangs flat (front to back x flatten), the normals by the inverse transpose
        if spec.get("flatten"):
            fz = spec["flatten"]; pf = p * np.array([1.0, 1.0, fz]); nf = n / np.array([1.0, 1.0, fz]); nf /= np.linalg.norm(nf, axis=1, keepdims=True)
            write_glb(out / (item["name"] + "_wieszak.glb"), item["name"] + "_wieszak", pf, nf, u, inv.reshape(-1, 3), uris, spec.get("metallicFactor", 1.0))
        report.append({"name": item["name"], "kind": item.get("kind", ""), "tris": int(len(tris)), "size_m": [round(float(x), 3) for x in (p.max(0) - p.min(0))], "pivot": item.get("pivot", "top")})
        print(item["name"], len(tris), "tris", [round(float(x), 2) for x in p.max(0) - p.min(0)])
    (out / "katalog.json").write_text(json.dumps({"pack": pack, "items": report}, indent=1, ensure_ascii=False) + "\n")


if __name__ == "__main__":
    main(*sys.argv[1:4])
