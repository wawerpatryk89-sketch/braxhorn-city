#!/usr/bin/env python3
"""THE QUAD TRACK'S FOREST: fetch one Poly Haven tree (CC0) for the card baker (wypal_drzewa.js).

    python3 pobierz_drzewo.py <poly haven id> <out dir>

Downloads the model's 1k glTF (its geometry is the same at every resolution) and, for its needles / leaves, the 4K colour
and the 4K ALPHA: Poly Haven's glTF export leaves the twigs' alpha out (it ships as its own *_twig_alpha / *_twigs_alpha
map) - without it a pine's needles render as black strips and a fir's sprigs as solid green sheets. The colour and the
alpha are joined into one RGBA PNG and the glTF patched to it (alphaMode MASK, 0.5) as <out>/<id>_las.gltf.
Owner's list (docs/zlecenia/MIASTO_LISTA_20261103.md 157): pine_tree_01, fir_tree_01, fir/pine saplings."""
import json
import os
import sys
import urllib.request

from PIL import Image

API = "https://api.polyhaven.com/files/"
UA = {"User-Agent": "BRAXHORN-UnityAssetBuild/1.0"}   # as prepare_assets.py (the API refuses the default one)


def otworz(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=300)


def get(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return
    os.makedirs(os.path.dirname(path) or ".", exist_ok=True)
    tmp = path + ".part"
    with otworz(url) as r, open(tmp, "wb") as f:
        while True:
            b = r.read(1 << 20)
            if not b:
                break
            f.write(b)
    os.replace(tmp, path)


def main(asset, out):
    files = json.load(otworz(API + asset))
    g = files["gltf"]["1k"]["gltf"]
    gpath = os.path.join(out, os.path.basename(g["url"]))
    get(g["url"], gpath)
    for rel, inc in g["include"].items():
        get(inc["url"], os.path.join(out, rel))
    gl = json.load(open(gpath))
    # the twigs' maps: the key the API names them by ("twig" / "twigs")
    twig = next(k[:-len("_alpha")] for k in files if k.endswith("_alpha"))
    mid = asset + "_" + twig
    diff = os.path.join(out, "textures", mid + "_diff_4k.jpg")
    alpha = os.path.join(out, "textures", mid + "_alpha_4k.png")
    get(files[twig + "_diff"]["4k"]["jpg"]["url"], diff)
    get(files[twig + "_alpha"]["4k"]["png"]["url"], alpha)
    for k in ("nor_gl", "arm"):
        get(files[twig + "_" + k]["4k"]["jpg"]["url"], os.path.join(out, "textures", mid + "_" + k + "_4k.jpg"))
    rgba = os.path.join(out, "textures", mid + "_diffa_4k.png")
    if not os.path.exists(rgba):
        im = Image.open(diff).convert("RGB")
        a = Image.open(alpha).convert("L")
        if a.size != im.size:
            a = a.resize(im.size, Image.LANCZOS)
        im.putalpha(a)
        im.save(rgba)
    twig_images = set()
    for i, im in enumerate(gl["images"]):
        u = im["uri"]
        if mid + "_diff" in u:
            im["uri"] = "textures/" + mid + "_diffa_4k.png"
            im["mimeType"] = "image/png"
            twig_images.add(i)
        elif mid + "_" in u:
            im["uri"] = u.replace("_1k.", "_4k.")
    tex = {i for i, t in enumerate(gl["textures"]) if t["source"] in twig_images}
    masked = []
    for m in gl["materials"]:
        bc = m.get("pbrMetallicRoughness", {}).get("baseColorTexture")
        if bc and bc["index"] in tex:
            m["alphaMode"] = "MASK"
            m["alphaCutoff"] = .5
            masked.append(m["name"])
    dst = os.path.join(out, asset + "_las.gltf")
    json.dump(gl, open(dst, "w"))
    print(asset, "->", dst, "masked:", masked, "nodes:", [n.get("name") for n in gl["nodes"] if "mesh" in n])


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
