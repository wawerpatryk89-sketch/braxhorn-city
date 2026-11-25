#!/usr/bin/env python3
"""MIASTO MISTRZÓW - the owner's Polish classics from Tripo (24.11.2026 22:21-23:11, three zips: "Samochody i motory.
Klasyki ... koła zrobić, żeby się kręciły. Szyby trzeba powymieniać ... napisy ... poprawić"; "każde napisy, jak są źle,
to zmazać i nowe"; "wszystko odrestaurować co trzeba"; their place: a lit bunker under the hill by the quad track).

Each Tripo export is ONE fused mesh in its own units, with one 2048 atlas. Per vehicle this tool
  - turns it front +z (Tripo gives some with the front at -x) and scales it to the real vehicle's length (MODELE: the
    published dimensions), its wheels standing on y = 0; loose pieces Tripo left beside it are dropped (MODELE usun);
  - restores the paint and the lettering in its colour map (tekstura.py: everything placed on the body in metres, never
    straight into the atlas): rust, dirt and Tripo's smears taken back to the paint; garbled letters, fake badges and
    plates erased; new lettering (Montserrat SemiBold, the region's typeface; brand names only as plain words, no logos)
    and registration plates of the time (1976-2000: black, white characters; private 3 letters + 4 digits, state
    3 letters + 3 digits + letter, the militia MO);
  - replaces the glass:
      * painted windows (Tripo painted the glass and a blurred inside onto a closed shell): those faces become the glass
        part, drawn with the region's tinted car glass, glossy, opaque (there is no inside behind them) - or, where the
        car's glass was clear (the stock 126p), Tripo's painting of it is kept, made smooth; either way the outline
        straight (measured off the painting where the recipe gives it), 1.5 cm of black rubber round it, the dark Tripo
        smeared beside it taken back to the paint;
      * open windows (Tripo left the openings, the inside modelled): new panes are built in each opening - the frame's
        surface fitted round it, the pane 1.5 cm inside it, tucked 2 cm under the frame - drawn transparent;
  - gives the cars new wheels (kola.py: a real rubber tyre and the car's kind of rim, at its real size) where Tripo's
    stood, cut out the same way first: the faces inside the tyre's cylinder and outside its inner sidewall, connected to the
    tread, each wheel with its pivot at its hub (the centre of its own faces' box, so it spins without wobble); a car's
    front wheels also steer; a truck's rear twin tyres are one wheel; a motorcycle has two in its middle plane;
  - fills the atlas' gutters from the nearest painted texel (no seam of the old paint shows);
  - writes the parts (real metres, glTF axes, each relative to its pivot) with two far LODs (MeshLab's quadric collapse
    with the texture), the maps, and katalog.json (pivots in UNITY axes: x negated, as glTFast imports);
  - packs it (--pack) as an encrypted owner pack like every other: the repository is public.

    python3 przygotuj_klasyki.py SRC_DIR OUT_DIR [--tylko a,b] [--pack PACK_DIR] [--podglad DIR]

SRC_DIR holds the owner's tripo_convert_<id>.glb files (any sub-folder). Never starts Unity, a build or a workflow."""
import hashlib
import io
import json
import shutil
import struct
import subprocess
import sys
import tarfile
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True
import cv2                                                       # noqa: E402
import numpy as np                                               # noqa: E402
from PIL import Image                                            # noqa: E402
from scipy import ndimage                                        # noqa: E402
from scipy.sparse import coo_matrix                              # noqa: E402
from scipy.sparse.csgraph import connected_components            # noqa: E402
from scipy.spatial import cKDTree                                # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(1, str(Path(__file__).resolve().parents[1] / "klady"))
import przygotuj_klady as KL                                     # noqa: E402  (read_glb, zapisz_glb, uprosc)
import tekstura as T                                             # noqa: E402
import przepisy                                                  # noqa: E402  (the per-vehicle texture recipes)
import kola as KO                                                # noqa: E402  (the new wheels)

TARGET = "pojazdy_klasyki_01"
ROOT = Path(__file__).resolve().parents[3]

# the vehicles: Tripo id, the front axis in the source (x: front at -x), the real length (m), the kind, the real wheelbase,
# the tyre radius prior (m), the glass (painted: kept faces become glass; open: panes are built), what to drop
MODELE = {
    "milicja": dict(id="17e66447", nazwa="WARSZAWA MILICJA", os="z", dl=4.586, typ="auto", wb=2.816, R=.32,
                    wzor="FSO Warszawa 223 (4586 x 1695 mm, rozstaw osi 2700 mm; 6.40-15)", szyby="malowane"),
    "maluch": dict(id="3420733b", nazwa="FIAT 126p", os="z", dl=3.054, typ="auto", wb=1.84, R=.263,
                   wzor="Polski Fiat 126p (3054 x 1378 mm, rozstaw osi 1840 mm; 135/80 R12)", szyby="malowane"),
    "syrena": dict(id="45ca6f11", nazwa="SYRENA 105", os="z", dl=4.03, typ="auto", wb=2.30, R=.33,
                   wzor="FSO Syrena 105 (4030 x 1500 mm, rozstaw osi 2300 mm; 5.60-15)", szyby="otwarte"),
    "bosna": dict(id="419d345c", nazwa="BOSNA BAR", os="x", dl=5.413, typ="auto", wb=3.45, R=.341,
                  wzor="dostawczy furgon L2H2 (5413 x 2050 mm, rozstaw osi 3450 mm; 215/70 R15)", szyby="malowane"),
    "fiat125p": dict(id="dc963363", nazwa="FIAT 125p", os="z", dl=4.23, typ="auto", wb=2.505, R=.30,
                     wzor="Polski Fiat 125p (4230 x 1630 mm, rozstaw osi 2505 mm; 175 R13)", szyby="otwarte"),
    "polonez": dict(id="4e4a64e9", nazwa="POLONEZ CARO", os="z", dl=4.369, typ="auto", wb=2.509, R=.29, uprosc=90000,
                    wzor="FSO Polonez Caro (4369 x 1720 mm, rozstaw osi 2509 mm; 175/70 R13)", szyby="otwarte"),
    "fiat125p_tuning": dict(id="12fdc32f", nazwa="FIAT 125p TUNING", os="x", dl=4.23, typ="auto", wb=2.505, R=.29,
                            wzor="Polski Fiat 125p (4230 mm, rozstaw osi 2505 mm), tuning", szyby="otwarte"),
    "polonez_tuning": dict(id="877f0cef", nazwa="POLONEZ TUNING", os="x", dl=4.272, typ="auto", wb=2.509, R=.29,
                           wzor="FSO Polonez 1500 (4272 mm, rozstaw osi 2509 mm), tuning", szyby="otwarte"),
    "polonez_milicja": dict(id="dac08c4a", nazwa="POLONEZ MILICJA", os="z", dl=4.272, typ="auto", wb=2.509, R=.29,
                            wzor="FSO Polonez 1500 MO (4272 x 1650 mm, rozstaw osi 2509 mm; 175 SR13)", szyby="malowane",
                            usun=[("x<", -1.05)]),
    "nysa_milicja": dict(id="9478a4b6", nazwa="NYSA MILICJA", os="z", dl=4.4, typ="auto", wb=2.7, R=.35,
                         wzor="FSD Nysa 522 MO (rozstaw osi 2700 mm; 6.40-15)", szyby="malowane"),
    "star_zielony": dict(id="62df2e43", nazwa="STAR 200", os="z", dl=6.455, typ="ciezarowka", wb=3.6, R=.50,
                         wzor="Star 200 skrzyniowy (6455 x 2420 mm; 9.00-20)", szyby="otwarte"),
    "star_pomaranczowy": dict(id="75282eb7", nazwa="STAR 28", os="z", dl=6.43, typ="ciezarowka", wb=3.5, R=.50,
                              wzor="Star 28 skrzyniowy (6430 x 2400 mm; 9.00-20)", szyby="otwarte"),
    "komar": dict(id="53166df6", nazwa="KOMAR", os="x", dl=1.8, typ="motor", wb=1.175, R=.30,
                  wzor="Romet Komar (1800 mm, rozstaw osi 1175 mm; 2.25-19)", szyby=None),
    "jawa": dict(id="9eab6f88", nazwa="JAWA", os="x", dl=2.101, typ="motor", wb=1.369, R=.31,
                 wzor="Jawa 350 (2101 mm, rozstaw osi 1369 mm; 3.25-18)", szyby=None),
    "romet": dict(id="f48f7800", nazwa="ROMET", os="x", dl=1.78, typ="motor", wb=None, R=.267,
                  wzor="Romet motorower (1780 mm)", szyby=None),
    "junak": dict(id="f120e989", nazwa="JUNAK M10", os="x", dl=2.172, typ="motor", wb=1.417, R=.33,
                  wzor="Junak M10 (2172 mm, rozstaw osi 1417 mm; 3.25-19)", szyby=None),
    # the owner's second set (25.11 02:15, his pictures of each as it should look: the paint, the wheels, the stance)
    # the third set (25.11 02:46: "Warszawa powinna być, Nysa powinna być, taki polski skuter i taki motor, i żuk")
    "warszawa_m20": dict(id="ced1c391", nazwa="WARSZAWA M20", os="z", dl=4.665, typ="auto", wb=2.70, R=.35, zagesc=1,
                         wzor="FSO Warszawa M20 (4665 x 1695 mm, rozstaw osi 2700 mm; 6.00-16)", szyby="otwarte"),
    "nysa_bus": dict(id="21c28b0b", nazwa="NYSA 522", os="z", dl=4.40, typ="auto", wb=2.70, R=.35, zagesc=1,
                     wzor="FSD Nysa 522 mikrobus (rozstaw osi 2700 mm jak Warszawa; 6.50-16)", szyby="malowane"),
    "zuk_bus": dict(id="a30f579c", nazwa="ŻUK A07", os="z", dl=4.45, typ="auto", wb=2.70, R=.35, zagesc=1,
                    wzor="FSC Żuk A07 mikrobus (dł. ok. 4450 mm jak A11, rozstaw osi 2700 mm; 6.50-16)", szyby="otwarte"),
    "wsk": dict(id="488c456e", nazwa="WSK M06", os="x", dl=2.06, typ="motor", wb=1.29, R=.31, zagesc=1,
                wzor="WSK M06 (2060 mm, rozstaw osi 1290 mm; 2.75-19)", szyby=None),
    "osa": dict(id="a2862697", nazwa="OSA", os="z", dl=1.88, typ="motor", wb=1.28, R=.25, zagesc=1,
                wzor="WFM Osa M50 (1880 mm, rozstaw osi 1280 mm; 3.25x14)", szyby=None),
    "trabant": dict(id="5d43f9a5", nazwa="TRABANT 601", os="z", dl=3.555, typ="auto", wb=2.02, R=.29, zagesc=1,
                    wzor="Trabant 601 (3555 x 1505 mm, rozstaw osi 2020 mm; 5.20-13)", szyby="malowane"),
    "trabant_tuning_braz": dict(id="c53e1881", nazwa="TRABANT TUNING", os="z", dl=3.555, typ="auto", wb=2.02, R=.29, zagesc=1,
                                wzor="Trabant 601 (rozstaw osi 2020 mm), tuning", szyby="otwarte"),
    "trabant_tuning_czarny": dict(id="0d66af0c", nazwa="TRABANT TUNING", os="z", dl=3.555, typ="auto", wb=2.02, R=.29, uprosc=90000,
                                  wzor="Trabant 601 (rozstaw osi 2020 mm), tuning", szyby="otwarte"),
    "syrena_niebieska": dict(id="10b153a8", nazwa="SYRENA 105", os="x", dl=4.03, typ="auto", wb=2.30, R=.33, zagesc=1,
                             wzor="FSO Syrena 105 (4030 x 1500 mm, rozstaw osi 2300 mm; 5.60-15)", szyby="otwarte"),
    "fiat125p_granat": dict(id="4fe033dc", nazwa="FIAT 125p", os="x", dl=4.23, typ="auto", wb=2.505, R=.30, zagesc=1,
                            wzor="Polski Fiat 125p (4230 x 1630 mm, rozstaw osi 2505 mm; 175 R13)", szyby="otwarte"),
    "fiat125p_tuning_granat": dict(id="4fdf91bf", nazwa="FIAT 125p TUNING", os="z", dl=4.23, typ="auto", wb=2.505, R=.30,
                                   wzor="Polski Fiat 125p (rozstaw osi 2505 mm), tuning", szyby="malowane"),
    "polonez_caro_zielony": dict(id="3434bdde", nazwa="POLONEZ CARO PLUS", os="z", dl=4.318, typ="auto", wb=2.509, R=.295,
                                 wzor="FSO Polonez Caro Plus (4318 x 1650 mm, rozstaw osi 2509 mm; 185/70 R13)", szyby="otwarte"),
    "polonez_tuning_bordo": dict(id="381e5709", nazwa="POLONEZ TUNING", os="z", dl=4.272, typ="auto", wb=2.509, R=.30, zagesc=1,
                                 wzor="FSO Polonez (rozstaw osi 2509 mm), tuning", szyby="malowane"),
    "maluch_tuning_czerwony": dict(id="2b84a185", nazwa="FIAT 126p TUNING", os="z", dl=3.054, typ="auto", wb=1.84, R=.27,
                                   wzor="Polski Fiat 126p (rozstaw osi 1840 mm), tuning", szyby="otwarte"),
    "maluch_tuning_kremowy": dict(id="5e05033f", nazwa="FIAT 126p TUNING", os="x", dl=3.054, typ="auto", wb=1.84, R=.27, zagesc=1,
                                  wzor="Polski Fiat 126p (rozstaw osi 1840 mm), tuning", szyby="otwarte"),
    "maluch_tuning_szary": dict(id="6416e8df", nazwa="FIAT 126p TUNING", os="z", dl=3.054, typ="auto", wb=1.84, R=.27,
                                wzor="Polski Fiat 126p (rozstaw osi 1840 mm), tuning", szyby="otwarte"),
}
SZKLO = (26, 32, 38)         # the tinted car glass seen from outside (sRGB): the glass part's own colour in the game too
USZCZELKA = (17, 17, 19)     # the black rubber seal round a window

# the new wheels (kola.py; owner 25.11 03:25: Tripo's wheels out, "ma być guma normalnie, felga, jak powinno być"): the rim,
# its diameter (in), the tyre's width (m), the rim's colour - by the car's real tyre and the owner's pictures; the
# motorcycles keep theirs (spoked, on their stands)
KOLA = {
    "milicja": ("stalowa", 14, .175, "srebrny"), "maluch": ("stalowa", 12, .135, "srebrny"), "syrena": ("stalowa", 15, .145, "bialy"),
    "bosna": ("stalowa", 15, .215, "srebrny"), "fiat125p": ("stalowa", 13, .175, "srebrny"), "polonez": ("stalowa", 13, .175, "srebrny"),
    "fiat125p_tuning": ("siatka", 15, .195, "zloty"), "polonez_tuning": ("ramiona", 15, .205, "zloty"),
    "polonez_milicja": ("stalowa", 13, .175, "srebrny"), "nysa_milicja": ("stalowa", 16, .165, "srebrny"),
    "star_zielony": ("ciezarowa", 20, .24, "grafit"), "star_pomaranczowy": ("ciezarowa", 20, .24, "grafit"),
    "trabant": ("stalowa", 13, .132, "bialy"), "trabant_tuning_braz": ("siatka", 15, .195, "chrom"),
    "trabant_tuning_czarny": ("ramiona", 15, .195, "srebrny"), "syrena_niebieska": ("stalowa", 15, .145, "bialy"),
    "fiat125p_granat": ("ramiona", 14, .195, "srebrny"), "fiat125p_tuning_granat": ("ramiona", 16, .215, "grafit"),
    "polonez_caro_zielony": ("stalowa", 13, .185, "srebrny"), "polonez_tuning_bordo": ("ramiona", 16, .215, "grafit"),
    "maluch_tuning_czerwony": ("ramiona5", 13, .175, "srebrny"), "maluch_tuning_kremowy": ("ramiona5", 13, .185, "bialy"),
    "maluch_tuning_szary": ("ramiona5", 13, .195, "bialy"), "warszawa_m20": ("stalowa", 16, .155, "kremowy"),
    "nysa_bus": ("stalowa", 16, .165, "bialy"), "zuk_bus": ("stalowa", 16, .165, "srebrny"),
}

# the owner's badges from Tripo (25.11 02:16: "Ja ci wysłałem znaczki w 3D ... możesz tylko tam poprawiać delikatnie,
# odmalować"): their real width (m), the depth they stand off the body at most, how they are repainted - chrome (the
# light parts silver, glossy), or kept (black with chrome or white letters, only made glossy)
ZNACZKI = {
    "fso": dict(id="5c93fe61", szer=.080, wys=.080, gleb=.007, farba="chrom"),
    "trabant_s": dict(id="77939a01", szer=.072, wys=.072, gleb=.008, farba="chrom"),
    "polski_fiat": dict(id="91d4daa7", szer=.110, wys=.050, gleb=.005, farba="jak_jest"),
    "fso_polonez": dict(id="b3f304c1", szer=.260, wys=.047, gleb=.006, farba="jak_jest"),
}
# the far versions: the body at 30 % / 12 % of its triangles, a wheel at 35 % / 15 % (the glass is a few panes: none)
LODY = {"korpus": (.30, .12), "kolo": (.35, .15)}


def zapisz_glb(path, name, pos, nrm, uv, idx):
    """a part as glTF 2.0 binary: positions, normals, UVs, triangles and a plain material - no pictures inside or linked
    (the game builds the materials from _tekstury/ itself: Unity's import reads no texture per part)"""
    pos = pos.astype(np.float32); nrm = nrm.astype(np.float32); uv = uv.astype(np.float32)
    idx = idx.astype(np.uint32 if len(pos) > 65535 else np.uint16)
    blobs, views, accs = [], [], []

    def add(arr, target, typ, ctype, minmax=False):
        data = arr.tobytes(); off = sum(len(x) for x in blobs); blobs.append(data + b"\0" * ((-len(data)) % 4))
        views.append({"buffer": 0, "byteOffset": off, "byteLength": len(data), "target": target})
        a = {"bufferView": len(views) - 1, "componentType": ctype, "count": len(arr), "type": typ}
        if minmax: a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1
    ip = add(pos, 34962, "VEC3", 5126, True); inn = add(nrm, 34962, "VEC3", 5126); iu = add(uv, 34962, "VEC2", 5126)
    ii = add(idx.reshape(-1), 34963, "SCALAR", 5125 if idx.dtype == np.uint32 else 5123)
    g = {"asset": {"version": "2.0", "generator": "BRAXHORN przygotuj_klasyki.py"},
         "scene": 0, "scenes": [{"nodes": [0]}], "nodes": [{"name": name, "mesh": 0}],
         "meshes": [{"name": name, "primitives": [{"attributes": {"POSITION": ip, "NORMAL": inn, "TEXCOORD_0": iu}, "indices": ii, "material": 0}]}],
         "materials": [{"name": "braxhorn " + name, "doubleSided": True, "pbrMetallicRoughness": {"metallicFactor": 0.0, "roughnessFactor": 0.6}}],
         "bufferViews": views, "accessors": accs, "buffers": [{"byteLength": sum(len(x) for x in blobs)}]}
    js = json.dumps(g, separators=(",", ":")).encode(); js += b" " * ((-len(js)) % 4); bin_ = b"".join(blobs)
    Path(path).write_bytes(struct.pack("<III", 0x46546C67, 2, 28 + len(js) + len(bin_)) + struct.pack("<II", len(js), 0x4E4F534A) + js
                           + struct.pack("<II", len(bin_), 0x004E4942) + bin_)


# ---------------------------------------------------------------------------------------------------------------- load
def znajdz(src, mid):
    f = sorted(Path(src).rglob("tripo_convert_%s*.glb" % mid))
    if not f: raise SystemExit("BRAK %s w %s" % (mid, src))
    return f[0]


def wczytaj(path, m):
    """the vehicle in metres: front +z, up +y, +x its LEFT (glTF), wheels on y = 0, the length centred on z = 0"""
    g, acc, image = KL.read_glb(path)
    pr = g["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(np.float64)
    N = acc(pr["attributes"]["NORMAL"]).astype(np.float64)
    U = acc(pr["attributes"]["TEXCOORD_0"]).astype(np.float64)
    I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    if m["os"] == "x":                                 # front at -x: (x, y, z) -> (z, y, -x), a rotation (no mirror)
        P = np.stack([P[:, 2], P[:, 1], -P[:, 0]], 1); N = np.stack([N[:, 2], N[:, 1], -N[:, 0]], 1)
    s = m["dl"] / (P[:, 2].max() - P[:, 2].min()); P = P * s
    P[:, 2] -= (P[:, 2].max() + P[:, 2].min()) / 2; P[:, 1] -= P[:, 1].min()
    mat = g["materials"][0]

    def obraz(ref):
        return Image.open(io.BytesIO(image(g["textures"][ref["index"]]["source"]))) if ref else None
    base = obraz(mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB")
    normal = obraz(mat.get("normalTexture")); mr = obraz(mat["pbrMetallicRoughness"].get("metallicRoughnessTexture"))
    if normal is None: normal = Image.new("RGB", base.size, (128, 128, 255))
    if mr is None: mr = Image.new("RGB", base.size, (255, 140, 0))             # no map: half rough, no metal
    for kind, val in m.get("usun", []):                                          # Tripo's loose pieces beside the vehicle
        cen = P[I].mean(1); ax = "xyz".index(kind[0])
        keep = ~((cen[:, ax] < val) if kind[1] == "<" else (cen[:, ax] > val)); I = I[keep]
    used = np.unique(I)                                                          # only the vertices still in use
    if len(used) < len(P):
        remap = -np.ones(len(P), np.int64); remap[used] = np.arange(len(used))
        P, N, U, I = P[used], N[used], U[used], remap[I]
    if m.get("uprosc") and len(I) > m["uprosc"] * 1.5:                           # a 2-million-triangle source
        P2, N2, U2, I2 = KL.uprosc(P, I, U, m["uprosc"])
        P, N, U, I = P2, N2, U2, I2.astype(np.int64)
    for _ in range(m.get("zagesc", 0)):                                          # a low-poly source: its faces quartered
        P, N, U, I = zagesc(P, N, U, I)
    return dict(P=P, N=N, U=U, I=I, skala=s, base=base, normal=normal.convert("RGB"), mr=mr.convert("RGB"))


def zagesc(P, N, U, I):
    """every face into four at its edges' middles (an edge's middle once for the faces sharing its two vertices; the UV
    and the normal halfway too): a Tripo export of 19 thousand faces has panels of a decimetre or two - seen from a side
    the far side's inside would pass as the near side, a window's edge would zigzag, a wheel's cut be coarse"""
    a, b, c = I[:, 0], I[:, 1], I[:, 2]
    kr = np.concatenate([np.stack([a, b], 1), np.stack([b, c], 1), np.stack([c, a], 1)])
    kr = np.sort(kr, 1); uniq, inv = np.unique(kr, axis=0, return_inverse=True); inv = inv.ravel()
    n0 = len(P); mid = n0 + inv; nf = len(I)
    mab, mbc, mca = mid[:nf], mid[nf:2 * nf], mid[2 * nf:]
    P2 = np.concatenate([P, (P[uniq[:, 0]] + P[uniq[:, 1]]) / 2])
    Nm = N[uniq[:, 0]] + N[uniq[:, 1]]; Nm /= np.maximum(np.linalg.norm(Nm, axis=1, keepdims=True), 1e-12)
    N2 = np.concatenate([N, Nm]); U2 = np.concatenate([U, (U[uniq[:, 0]] + U[uniq[:, 1]]) / 2])
    I2 = np.concatenate([np.stack([a, mab, mca], 1), np.stack([mab, b, mbc], 1), np.stack([mca, mbc, c], 1), np.stack([mab, mbc, mca], 1)])
    return P2, N2, U2, I2


def odchudz(P, N, U, I, part, budzet):
    """each part back to its budget of faces (MeshLab with the texture, its boundary kept: the cut between the body and a
    wheel stays where it was) - after the work done on the quartered faces"""
    P2, N2, U2, I2, part2 = [], [], [], [], []; n = 0
    for pv in np.unique(part):
        f = np.nonzero(part == pv)[0]; cel = budzet.get("kolo" if pv > 0 else "szyby" if pv < 0 else "korpus", 90000)
        if len(f) > cel * 1.3:
            p, nn, u, t = KL.uprosc(P, I[f], U, cel); t = t.astype(np.int64)
        else:
            used = np.unique(I[f]); rm = -np.ones(len(P), np.int64); rm[used] = np.arange(len(used))
            p, nn, u, t = P[used], N[used], U[used], rm[I[f]]
        P2.append(p); N2.append(nn); U2.append(u); I2.append(t + n); part2.append(np.full(len(t), pv)); n += len(p)
    return np.concatenate(P2), np.concatenate(N2), np.concatenate(U2), np.concatenate(I2), np.concatenate(part2)


def sasiedztwo(P, I):
    key = np.round(P / 1e-5).astype(np.int64); _, w = np.unique(key, axis=0, return_inverse=True); w = w.ravel()
    nf = len(I); Fw = w[I]
    VF = coo_matrix((np.ones(nf * 3), (Fw.ravel(), np.repeat(np.arange(nf), 3))), shape=(w.max() + 1, nf)).tocsr()
    return (VF.T @ VF).tocsr()


# ---------------------------------------------------------------------------------------------------------------- wheels
def kola_wstepnie(P, m):
    """each wheel from where it meets the ground: its contact z (the lowest points), its radius from the tyre's chord
    above the ground (c^2 = 2Ry - y^2 at y = 6, 9, 12 cm - only the tyre stands that low), its x band"""
    H = P[:, 1].max(); W = P[:, 0].max() - P[:, 0].min(); dl = m["dl"]; out = []
    boki = (1, -1) if m["typ"] != "motor" else (0,)
    for bok in boki:
        band = P[(P[:, 0] * bok > .45 * W / 2) & (P[:, 1] < .55 * H)] if bok else P[np.abs(P[:, 0]) < .07]
        for pol in (1, -1):
            b = band[band[:, 2] * pol > .12 * dl]
            low = b[b[:, 1] < (b[:, 1].min() if len(b) else 0) + .02]     # its own lowest (a Tripo tyre can float a little)
            if len(low) < 5 or low[:, 1].min() > .08: raise SystemExit("%s: no ground contact (side %d, %s)" % (m["nazwa"], bok, "front" if pol > 0 else "rear"))
            zc = float(np.median(low[:, 2])); Rs = []
            y00 = float(low[:, 1].min())
            for y in (.06, .09, .12):
                z = np.sort(b[np.abs(b[:, 1] - y00 - y) < .006][:, 2])
                if len(z) < 4: continue
                i = np.searchsorted(z, zc); lo = hi = min(max(i, 0), len(z) - 1)
                while lo > 0 and z[lo] - z[lo - 1] < .02: lo -= 1
                while hi < len(z) - 1 and z[hi + 1] - z[hi] < .02: hi += 1
                c = (z[hi] - z[lo]) / 2; Rs.append((c * c + y * y) / (2 * y))
            R = float(np.median(Rs)) if Rs else m["R"]
            if not (.7 * m["R"] < R < 1.35 * m["R"]): R = m["R"]
            ring = b[np.abs(np.hypot(b[:, 2] - zc, b[:, 1] - y00 - R) - R) < .02]
            if bok: xin, xout = float(np.percentile(np.abs(ring[:, 0]), 4)), float(np.percentile(np.abs(ring[:, 0]), 96))
            else: xin, xout = 0.0, float(np.percentile(np.abs(ring[:, 0]), 96))
            out.append(dict(bok=bok, pol=pol, z=zc, y=y00 + R, R=R, x_in=xin, x_out=xout))
    return out


def tnij_kola(P, I, kola, typ, FF):
    """faces -> wheel index (+1; 0 the body): inside the tyre's cylinder (1.04 R: a road tyre's smooth tread) and
    outside its inner sidewall, the connected pieces that hold tread faces; twice (the second round the measured hub)"""
    T3 = P[I]; nf = len(I)

    def skladowe(mask):
        idx = np.nonzero(mask)[0]
        k, lab = connected_components(FF[idx][:, idx], directed=False)
        full = -np.ones(nf, int); full[idx] = lab; return k, full
    for przebieg in (0, 1):
        part = np.zeros(nf, int)
        for i, k in enumerate(kola):
            r = np.hypot(T3[:, :, 2] - k["z"], T3[:, :, 1] - k["y"])
            if k["bok"]:
                x = T3[:, :, 0] * k["bok"]; inx = (x >= k["x_in"] - .015) & (x <= k["x_out"] + .04)
            else:
                inx = np.abs(T3[:, :, 0]) <= k["x_out"] + .012
            # over the hub the mudguard hugs the tyre (a motorcycle's by 2-3 cm): 1.01 R there, 1.04 R below
            rmax = np.where(T3[:, :, 1] > k["y"], 1.01, 1.04) * k["R"]
            cand = np.all(inx & (r <= rmax), axis=1) & (part == 0)
            n, lab = skladowe(cand)
            seed = cand & np.all(r > .86 * k["R"], axis=1)
            sizes = np.bincount(lab[cand], minlength=n)
            keep = [c for c in np.unique(lab[seed]) if sizes[c] >= 20]
            sel = cand & np.isin(lab, keep)
            if sel.sum() < 50: raise SystemExit("wheel %d: %d faces" % (i, sel.sum()))
            part[sel] = i + 1
            v = P[np.unique(I[sel])]; lo, hi = v.min(0), v.max(0)
            # the radius from the lower half (nothing but the tyre stands there): its length / 2; the hub that high
            dol = v[v[:, 1] < k["y"]]
            Rm = (dol[:, 2].max() - dol[:, 2].min()) / 2 if len(dol) > 10 else max(hi[1] - lo[1], hi[2] - lo[2]) / 2
            k.update(hz=(dol[:, 2].max() + dol[:, 2].min()) / 2 if len(dol) > 10 else (lo[2] + hi[2]) / 2,
                     hy=lo[1] + Rm, hx=(lo[0] + hi[0]) / 2, szer=hi[0] - lo[0], Rm=Rm)
            if przebieg == 0:                                    # round its own hub the second time (the tread's box)
                k["z"], k["y"] = k["hz"], k["hy"]; k["R"] = float(np.clip(k["Rm"], .85 * k["R"], 1.15 * k["R"]))
    return part


# ---------------------------------------------------------------------------------------------------------------- glass
def roznica(c, farby):
    """how far colours (n x 3, sRGB 0-1) are from the nearest paint: CIE Lab with the lightness at half weight (a panel's
    shading changes its lightness, not its hue) - a neutral grey glass is far from a beige or blue paint, a lighter roof
    of the same paint is near it"""
    if not farby or len(c) == 0: return np.full(len(c), 100.0)
    lab = cv2.cvtColor(np.clip(c, 0, 1).astype(np.float32).reshape(-1, 1, 3), cv2.COLOR_RGB2LAB).reshape(-1, 3)
    out = np.full(len(c), 1e9)
    for f in farby:
        q = cv2.cvtColor(np.array(f, np.float32).reshape(1, 1, 3), cv2.COLOR_RGB2LAB).reshape(3)
        dd = lab - q; dd[:, 0] *= .5; out = np.minimum(out, np.sqrt((dd ** 2).sum(1)))
    return out


def zaokraglij(obrys, r=.03, n=6):
    """a window's outline (u, v[, r]) with its corners rounded: r of each edge into a curve (0: a sharp corner, as at a
    vent's post)"""
    p = np.array([o[:2] for o in obrys], float); rr = [o[2] if len(o) > 2 else r for o in obrys]; out = []
    for i in range(len(p)):
        a, b, c = p[i - 1], p[i], p[(i + 1) % len(p)]
        t = min(rr[i], .45 * np.linalg.norm(a - b), .45 * np.linalg.norm(c - b))
        if t <= 1e-4: out.append(b); continue
        A = b + (a - b) / np.linalg.norm(a - b) * t; C = b + (c - b) / np.linalg.norm(c - b) * t
        for s in np.linspace(0, 1, n): out.append((1 - s) ** 2 * A + 2 * s * (1 - s) * b + s ** 2 * C)
    return np.array(out)


def szyby_malowane(mod, okna, farby, cien=.10, krok=.01, przyciemniane=True, prostokatne=False):
    """the painted glass: in each window box (view, u0, u1, v0, v1) the faces seen there (both of Tripo's layers) whose
    colour is not the body's paint and not the black rubber round the glass - then the window made whole in the view
    (its gaps closed over 4 cm, its holes filled: Tripo's light streaks and reflections in the glass are glass too) - or
    the window's outline measured off the painting, when the recipe gives it. Tinted: the glass' faces dark and glossy;
    clear (przyciemniane=False): Tripo's painting of the glass kept, only made smooth, the rubber round it the same"""
    sel = np.zeros(len(mod.I), bool)
    uc = mod.U[mod.I].mean(1); px = np.clip((uc[:, 0] * mod.w).astype(int), 0, mod.w - 1); py = np.clip((uc[:, 1] * mod.h).astype(int), 0, mod.h - 1)
    kol = cv2.GaussianBlur(mod.img, (5, 5), 0)[py, px]
    far = [np.array(f, np.float32) / 255 for f in farby]
    d = roznica(kol, far)
    lum = np.array([.299, .587, .114], np.float32); Lp = min(float(x @ lum) for x in far)
    namalowane = np.zeros((mod.h, mod.w), bool)                               # the texels made glass or seal so far
    for okno in okna:
        widok, box = okno[0], okno[1]; obrys = okno[2] if len(okno) > 2 else None
        op = okno[3] if len(okno) > 3 else {}
        do, gesto = op.get("do"), op.get("gesto", False)                # a wing before the window left out; a coarse raked glass
        cale = op.get("cale", False)                                    # all in the outline glass (Tripo's crumpled sheet there)
        uvf = T.WIDOKI[widok][1]
        vis = mod.widoczne(widok, box, cos=.15, glebia=.12, margines=0, do=do, gesto=gesto)
        u, v = uvf(mod.cen)
        vis &= (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3])
        f = vis & (d > 10) & (kol.max(1) > cien)
        # the view's grid: the box and 10 cm round it (the paint beside the window is cleaned too)
        u0, u1, v0, v1 = box; g0, g1, h0, h1 = u0 - .1, u1 + .1, v0 - .1, v1 + .1
        nu, nv = int(np.ceil((g1 - g0) / krok)) + 1, int(np.ceil((h1 - h0) / krok)) + 1
        # the glass faces drawn whole into the view's grid (a low-poly van has faces of a decimetre)
        M8 = np.zeros((nv, nu), np.uint8)
        tu, tv = uvf(mod.P[mod.I[f]])
        tri = np.stack([(tu - g0) / krok, (h1 - tv) / krok], -1)
        if len(tri): cv2.fillPoly(M8, list(np.round(tri * 4).astype(np.int32)), 1, shift=2)
        # (the glass inside its box: a big face's corner past it is not window)
        bu0, bv1 = int(round((u0 - g0) / krok)), int(round((h1 - v1) / krok)); bu1, bv0 = bu0 + int(round((u1 - u0) / krok)), bv1 + int(round((v1 - v0) / krok))
        M8[:bv1] = 0; M8[bv0 + 1:] = 0; M8[:, :bu0] = 0; M8[:, bu1 + 1:] = 0
        M = ndimage.binary_dilation(M8 > 0, iterations=1)
        M = ndimage.binary_closing(M, structure=np.ones((5, 5)), iterations=1)
        M = ndimage.binary_fill_holes(M)
        M = ndimage.binary_erosion(M, iterations=1)
        lab, n = ndimage.label(M)
        # each window as the glass is: its outline straight and clean (the convex hull of what Tripo painted there - a
        # car's window is convex), not the ragged edge of the painting; the window itself, not a speck beside it
        kadluby = []
        for i in range(1, n + 1):
            yx = np.argwhere(lab == i)
            if len(yx) * krok * krok < .02: continue
            if prostokatne:
                # a van's or a truck's window is a rectangle with rounded corners: its rows and columns where the
                # window covers most of them (Tripo's smears along an edge left out), the corners 3 cm round
                cm = lab == i; rows = cm.sum(1); cols = cm.sum(0)
                rr = np.nonzero(rows > .5 * rows.max())[0]; cc = np.nonzero(cols > .5 * cols.max())[0]
                a_u, b_u = g0 + (cc[0] + .5) * krok, g0 + (cc[-1] + .5) * krok; a_v, b_v = h1 - (rr[-1] + .5) * krok, h1 - (rr[0] + .5) * krok
                kadluby.append(zaokraglij([(a_u, b_v), (b_u, b_v), (b_u, a_v), (a_u, a_v)], r=.03)); continue
            h = cv2.convexHull(yx[:, ::-1].astype(np.float32)).reshape(-1, 2)
            kadluby.append(np.stack([g0 + (h[:, 0] + .5) * krok, h1 - (h[:, 1] + .5) * krok], -1))
        # or the window's outline measured off the model's own painting (its frame, the vent's post): exactly that
        if obrys: kadluby = [zaokraglij(o) for o in obrys]

        def odl(pu, pv):
            """how far inside the windows (m; less than 0 outside): the hulls' straight edges exactly, not their 1 cm grid"""
            out = np.full(len(pu), -1.0)
            for h in kadluby:
                a = h; b = np.roll(h, -1, 0); e = b - a; L = np.linalg.norm(e, axis=1); ok = L > 1e-6
                a, e, L = a[ok], e[ok], L[ok]
                znak = np.sign(np.sum(a[:, 0] * np.roll(a[:, 1], -1) - np.roll(a[:, 0], -1) * a[:, 1])) or 1.0
                nrm = np.stack([-e[:, 1], e[:, 0]], -1) / L[:, None] * znak          # inward
                dd = (pu[:, None] - a[None, :, 0]) * nrm[None, :, 0] + (pv[:, None] - a[None, :, 1]) * nrm[None, :, 1]
                out = np.maximum(out, dd.min(1))
            return out
        # the faces seen there - where the outline is measured, at the window's edge aslant too (the frame's slope left its
        # colour as notches in the outline) and 10 cm round it (the paint beside it cleaned)
        dv = T.WIDOKI[widok][0]; eu, ev = (np.array(x, float) for x in uvf(np.eye(3)))
        kand = vis | (mod.widoczne(widok, (g0, g1, h0, h1), cos=.02, glebia=.12, margines=0, do=do, gesto=gesto) & (u >= g0) & (u <= g1) & (v >= h0) & (v <= h1)) if obrys else vis
        wb_f = (u >= u0) & (u <= u1) & (v >= v0) & (v <= v1)
        # the window's surface: a smooth sheet fitted to the depth of the faces inside it (a raked windscreen curving into
        # the roof); what stands 4 cm proud of it is not glass (a mirror), nor what lies behind it (seen through a gap); the
        # glass' own faces those turned less than 45 degrees from it (not a pillar's side Tripo painted as glass)
        dep_c = mod.cen @ dv; cin = kand & wb_f & (odl(u, v) > 0)
        rz = lambda pu, pv: np.stack([np.ones(len(pu)), pu, pv, pu * pu, pu * pv, pv * pv], 1)
        kp = np.zeros(6); kp[0] = np.inf
        if cin.sum() > 30:
            # (from the faces near the median depth: the far side's inside seen through a gap left out)
            A, yy = rz(u[cin], v[cin]), dep_c[cin]; ok = np.abs(yy - np.median(yy)) < .3
            for _ in range(4):
                kp = np.linalg.lstsq(A[ok], yy[ok], rcond=None)[0]; r = np.abs(yy - A @ kp); ok = r < max(.015, 2.5 * np.median(r[ok]))
        plaszcz = lambda pu, pv: rz(pu, pv) @ kp
        # (the turn measured from the window's middle: Tripo's windscreen wraps round into the pillars, which a real one
        # does not)
        um, vm = (float(np.mean(u[cin])), float(np.mean(v[cin]))) if cin.any() else (0.0, 0.0)
        N0 = dv - (kp[1] + 2 * kp[3] * um + kp[4] * vm) * eu - (kp[2] + kp[4] * um + 2 * kp[5] * vm) * ev; N0 /= np.linalg.norm(N0)
        wc = dep_c - plaszcz(u, v); okienne = (np.abs(mod.n @ N0) > .7) & (wc < .04) & (wc > -.08)
        t1, t2 = mod.teksele(kand & okienne), mod.teksele(kand & ~okienne)
        ys, xs, pts = (np.concatenate([x1, x2]) for x1, x2 in zip(t1, t2))
        grp = np.arange(len(ys)) < len(t1[0])                                   # a texel of the window's own surface
        pu, pv = uvf(pts); od = odl(pu, pv); wyst = pts @ dv - plaszcz(pu, pv)
        # at the window's surface (not behind it, not a mirror): where its outline is measured; Tripo's own glass painting
        # elsewhere taken whole, as found
        blisko = ((wyst < .04) & (wyst > -.08)) if obrys and not cale else np.ones(len(ys), bool)
        wbox = (pu >= u0) & (pu <= u1) & (pv >= v0) & (pv <= v1) & blisko
        # the glass inside (all that lies at its surface: Tripo's faces turn every way), 1.5 cm of rubber round it
        szk = wbox & (od > .015)
        usz = wbox & (od > 0) & (od <= .015)
        # the glass' own faces (its glossy material) only those wholly inside the glass: a face of Tripo's 3-5 cm reaching
        # past the window's straight outline would draw its saw-teeth over the seal and the paint
        pu3, pv3 = uvf(mod.P[mod.I].reshape(-1, 3))
        w3 = ((odl(pu3, pv3) > .015) & (pu3 >= u0) & (pu3 <= u1) & (pv3 >= v0) & (pv3 <= v1)).reshape(-1, 3).all(1)
        if przyciemniane: sel |= vis & w3 & okienne
        # beside the window, 3.5 cm: the dark Tripo smeared along its edge (not the paint, darker than it, not standing proud
        # as a mirror does, not turned from the window's surface as a mirror's back is) takes the paint of the nearest body
        # texel beyond: the outline the seal's alone
        if usz.any():
            c = mod.img[ys, xs]; dc = roznica(c, far)
            brud = grp & (od <= 0) & (od > -.035) & (dc > 12) & (c @ lum < .8 * Lp) & blisko & ~namalowane[ys, xs]
            zr = (od <= -.035) & (dc <= 10) & blisko
            if brud.any() and zr.sum() > 20:
                _, j = cKDTree(pts[zr]).query(pts[brud])
                Y, X, SY, SX = ys[brud], xs[brud], ys[zr][j], xs[zr][j]
                mod.img[Y, X] = mod.img[SY, SX]
                for k, mp in mod.mapy.items():
                    hh, ww = mp.shape[:2]
                    mp[np.clip(Y * hh // mod.h, 0, hh - 1), np.clip(X * ww // mod.w, 0, ww - 1)] = mp[np.clip(SY * hh // mod.h, 0, hh - 1), np.clip(SX * ww // mod.w, 0, ww - 1)]
                mod.log.append("szyba %s: obok oczyszczone %d texels" % (widok, int(brud.sum())))
        # inside, the window's own painting (the blurred inside, the streaks) becomes the glass' dark tint, smooth and not
        # metal; round it a 1.5 cm black rubber seal, as the cars of the time had - the outline clean
        # (clear glass, not tinted: Tripo's own painting of it kept - the inside seen through it, the sky in it - smooth)
        for t, kolor, mrk in ((szk, SZKLO if przyciemniane else None, (255, 24, 0)), (usz, USZCZELKA, (255, 175, 0))):
            # and 2 texels past the islands' edges where that painting is (the atlas seams would show as light lines)
            mk = np.zeros((mod.h, mod.w), np.uint8); mk[ys[t], xs[t]] = 1
            rim = (cv2.dilate(mk, np.ones((5, 5), np.uint8)) > 0) & (mk == 0)
            ry, rx = np.nonzero(rim); rr = roznica(mod.img[ry, rx], far) > 8
            if kolor is None: rr[:] = False
            Y = np.concatenate([ys[t], ry[rr]]); X = np.concatenate([xs[t], rx[rr]])
            if kolor is not None: mod.img[Y, X] = np.array(kolor, np.float32) / 255
            namalowane[Y, X] = True
            for k, mp in mod.mapy.items():
                hh, ww = mp.shape[:2]; yy = np.clip(Y * hh // mod.h, 0, hh - 1); xx = np.clip(X * ww // mod.w, 0, ww - 1)
                mp[yy, xx] = (128, 128, 255) if k == "normal" else mrk
        mod.log.append("szyba %s %s: %d texels" % (widok, box, int((szk | usz).sum())))
    return sel


def zmaz_opone(mod, faces, k, rim=.66, prog=.12):
    """a tyre's outer sidewall cleaned of Tripo's lettering: the texels of the wheel's faces between the rim (rim x R) and
    the tread, on its outer side, that are not the rubber's own black - inpainted from the rubber round them"""
    ys, xs, pts = mod.teksele(faces)
    if len(ys) == 0: return
    r = np.hypot(pts[:, 2] - k["hz"], pts[:, 1] - k["hy"])
    out = (pts[:, 0] - k["hx"]) * (np.sign(k["hx"]) if k["bok"] else 1)
    ring = (r > rim * k["R"]) & (r < 1.0 * k["R"]) & ((out > 0) if k["bok"] else np.ones(len(r), bool))
    c = mod.img[ys, xs]; guma = np.median(c[ring], 0) if ring.any() else np.array([.1, .1, .1])
    dev = ring & (np.linalg.norm(c - guma, axis=1) > prog)
    mask = np.zeros((mod.h, mod.w), np.uint8); mask[ys[dev], xs[dev]] = 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8))
    reg = np.zeros_like(mask); reg[ys[ring], xs[ring]] = 255; mask &= cv2.dilate(reg, np.ones((3, 3), np.uint8))
    src = (np.clip(mod.img, 0, 1) * 255).astype(np.uint8); o = cv2.inpaint(src, mask, 7, cv2.INPAINT_TELEA)
    sel = mask > 0; mod.img[sel] = o[sel].astype(np.float32) / 255
    mod.log.append("opona z%.2f x%.2f: %d texels" % (k["hz"], k["hx"], int(sel.sum())))


def raster_glebi(mod, widok, box, krok=.01):
    """the front-most surface seen in a view's box on a krok grid: (u, v) -> depth toward the viewer (NaN: nothing)"""
    d, uvf = T.WIDOKI[widok]; u0, u1, v0, v1 = box
    nu, nv = int(np.ceil((u1 - u0) / krok)), int(np.ceil((v1 - v0) / krok))
    D = np.full((nv, nu), -np.inf)
    u, v = uvf(mod.cen); f = np.nonzero((u > u0 - .3) & (u < u1 + .3) & (v > v0 - .3) & (v < v1 + .3))[0]
    if len(f) == 0: return np.full((nv, nu), np.nan)
    T3 = mod.P[mod.I[f]]
    e = np.max(np.linalg.norm(T3 - np.roll(T3, 1, axis=1), axis=2), 1)
    n = np.clip(np.ceil(e / (krok * .5)).astype(int), 1, 60)
    for k in np.unique(n):
        tri = T3[n == k]
        a, b = np.meshgrid(np.arange(k + 1), np.arange(k + 1)); m = a + b <= k
        l1 = a[m] / k; l2 = b[m] / k; l3 = 1 - l1 - l2
        p = l1[None, :, None] * tri[:, None, 0] + l2[None, :, None] * tri[:, None, 1] + l3[None, :, None] * tri[:, None, 2]
        p = p.reshape(-1, 3); pu, pv = uvf(p); dep = p @ d
        iu = np.floor((pu - u0) / krok).astype(int); iv = np.floor((v1 - pv) / krok).astype(int)
        ok = (iu >= 0) & (iu < nu) & (iv >= 0) & (iv < nv)
        np.maximum.at(D, (iv[ok], iu[ok]), dep[ok])
    D[~np.isfinite(D)] = np.nan
    return D


def z_widoku(widok, u, v, dep):
    """a view's (u, v, depth) back to the body's x, y, z"""
    if widok == "L": return np.stack([dep, v, -u], -1)
    if widok == "P": return np.stack([-dep, v, u], -1)
    if widok == "F": return np.stack([u, v, dep], -1)
    if widok == "T": return np.stack([-u, v, -dep], -1)
    raise ValueError(widok)


def szyba_otwarta(mod, widok, box, krok=.01, wglab=.015, zakladka=2, prog=.05, min_pole=.012, rama=False):
    """a pane in an open window: the frame's surface fitted round the opening (a quadric in u, v through the box's rim,
    the deep points rejected), the opening = what lies over 5 cm deeper than it or nothing at all; the pane follows the
    fitted surface 1.5 cm inside and reaches 2 cm under the frame. Returns (positions, normals, triangles) or None."""
    D = raster_glebi(mod, widok, box, krok); nv, nu = D.shape; u0, u1, v0, v1 = box
    uu = u0 + (np.arange(nu) + .5) * krok; vv = v1 - (np.arange(nv) + .5) * krok; Ug, Vg = np.meshgrid(uu, vv)
    rim = np.zeros_like(D, bool); r = max(2, int(.03 / krok)); rim[:r] = rim[-r:] = True; rim[:, :r] = rim[:, -r:] = True
    ok = rim & np.isfinite(D)
    if ok.sum() < 20: return None
    # the frame's surface: a quadric in u, v (a windscreen curves across and bends at its foot, a side window curves down)
    baza = (lambda U_, V_: np.stack([np.ones_like(U_), U_, V_, U_ * U_, U_ * V_, V_ * V_], -1))
    A = baza(Ug, Vg)
    use = ok.copy()
    for _ in range(6):
        c, *_ = np.linalg.lstsq(A[use], D[use], rcond=None)
        res = D - A @ c; use = ok & (res > -.03)
    fit = A @ c; lo_d, hi_d = float(D[use].min()), float(D[use].max())
    # the sky seen past the body (above the roof, beside a pillar) is open to the box's rim: not an opening. What is seen
    # through the car (both windows open) is enclosed by the frame: it is.
    # (the box's top border is above the roof; from the front or the rear its sides are beside the pillars - from the
    # side its edges may cross the opening itself)
    nic = ~np.isfinite(D); lab0, n0 = ndimage.label(nic)
    brzegi = [lab0[0]] + ([lab0[:, 0], lab0[:, -1]] if widok in "FT" else [])
    zew = np.unique(np.concatenate(brzegi)); zew = zew[zew > 0]
    hole = (nic & ~np.isin(lab0, zew)) | (np.isfinite(D) & (D < fit - prog))
    hole[rim] = False
    # then the frame itself: the surface fitted again to the 1 - 4 cm round the opening found (the window's own frame,
    # not the box's edge - a box edge on the bonnet would tilt the pane over it), the opening found again against it
    for _ in range(2 if rama else 0):
        big = ndimage.binary_opening(hole, iterations=1)
        lab1, n1 = ndimage.label(big)
        if n1 == 0: break
        pole1 = ndimage.sum(np.ones_like(D), lab1, range(1, n1 + 1)) * krok * krok
        big = np.isin(lab1, [i + 1 for i in range(n1) if pole1[i] >= min_pole])
        ram = ndimage.binary_dilation(big, iterations=4) & ~ndimage.binary_dilation(big, iterations=1) & np.isfinite(D)
        if ram.sum() < 20: break
        use = ram.copy()
        for _ in range(5):
            c, *_ = np.linalg.lstsq(A[use], D[use], rcond=None)
            res = D - A @ c; use = ram & (res > -.03)
        fit = A @ c; lo_d, hi_d = float(D[use].min()), float(D[use].max())
        hole = ((nic & ~np.isin(lab0, zew)) | (np.isfinite(D) & (D < fit - prog))) & ndimage.binary_dilation(big, iterations=3)
        hole[rim] = False
    hole = ndimage.binary_opening(hole, iterations=1)
    lab, n = ndimage.label(hole)
    if n == 0: return None
    pole = ndimage.sum(np.ones_like(D), lab, range(1, n + 1)) * krok * krok
    keep = np.isin(lab, [i + 1 for i in range(n) if pole[i] >= min_pole])
    if not keep.any(): return None
    keep = ndimage.binary_fill_holes(keep)
    keep = ndimage.binary_dilation(keep, iterations=zakladka)
    # the pane's surface: a membrane stretched over the frame round the opening (each cell the mean of its neighbours,
    # the frame's own depth at the edge) - it meets the windscreen's foot at the scuttle, the side glass at the sill,
    # whatever the box's edge lies on; the fitted quadric only starts it
    ram = ndimage.binary_dilation(keep, iterations=1) & ~keep & np.isfinite(D) & (D >= fit - prog)
    Z = np.where(keep, fit, np.where(ram, D, np.nan))
    if widok in "FT":
        # a windscreen or a rear window lies between its two pillars: in each row, from the left pillar's depth to the
        # right one's (the rows smoothed over 9 cm); a row without a pillar takes its neighbours'
        Lr = np.full(nv, np.nan); Rr = np.full(nv, np.nan); J0 = np.zeros(nv, int); J1 = np.zeros(nv, int)
        for i in range(nv):
            jj = np.nonzero(keep[i])[0]
            if len(jj) == 0: continue
            J0[i], J1[i] = jj[0], jj[-1]
            a = D[i, max(0, jj[0] - 5):jj[0]]; b = D[i, jj[-1] + 1:jj[-1] + 6]
            a = a[np.isfinite(a)]; b = b[np.isfinite(b)]
            if len(a): Lr[i] = a.max()
            if len(b): Rr[i] = b.max()
        Lr = np.where(np.isnan(Lr), Rr, Lr); Rr = np.where(np.isnan(Rr), Lr, Rr)
        ok_r = np.isfinite(Lr)
        if ok_r.sum() >= 3:
            ii = np.arange(nv)
            Lr = np.interp(ii, ii[ok_r], ndimage.median_filter(Lr[ok_r], 9, mode="nearest"))
            Rr = np.interp(ii, ii[ok_r], ndimage.median_filter(Rr[ok_r], 9, mode="nearest"))
            Lr = ndimage.uniform_filter1d(Lr, 9, mode="nearest"); Rr = ndimage.uniform_filter1d(Rr, 9, mode="nearest")
            jg = np.arange(nu)[None, :]; t = np.clip((jg - J0[:, None]) / np.maximum(J1 - J0, 1)[:, None], 0, 1)
            Z = np.where(keep, Lr[:, None] + (Rr - Lr)[:, None] * t, Z)
            # a windscreen or a back window is near a plane: the pane kept within 5 cm of the plane fitted (robustly)
            # through it - a pillar's flank reaching back at a row's end would otherwise drag a wedge out behind the roof
            ii, jj = np.nonzero(keep & np.isfinite(Z))
            if len(ii) > 30:
                A = np.stack([np.ones(len(ii)), ii, jj], 1).astype(float); y = Z[ii, jj]; ok_p = np.ones(len(y), bool)
                for _ in range(4):
                    k_ = np.linalg.lstsq(A[ok_p], y[ok_p], rcond=None)[0]; r_ = np.abs(y - A @ k_)
                    ok_p = r_ < max(.02, 2.5 * np.median(r_[ok_p]))
                pl = k_[0] + k_[1] * np.arange(nv)[:, None] + k_[2] * np.arange(nu)[None, :]
                Z = np.where(keep, np.clip(Z, pl - .05, pl + .05), Z)
    elif ram.sum() >= 12:
        wsp = keep | ram
        for _ in range(600):
            Zp = np.pad(np.where(wsp, Z, 0), 1); Wp = np.pad(wsp.astype(float), 1)
            sm = Zp[:-2, 1:-1] + Zp[2:, 1:-1] + Zp[1:-1, :-2] + Zp[1:-1, 2:]
            wn = Wp[:-2, 1:-1] + Wp[2:, 1:-1] + Wp[1:-1, :-2] + Wp[1:-1, 2:]
            Z = np.where(keep & (wn > 0), sm / np.maximum(wn, 1), Z)
    # the mesh: 2 cm cells over the opening (any covered 1 cm cell), corners on the fitted surface
    s = 2; mk = np.zeros(((nv + s - 1) // s, (nu + s - 1) // s), bool)
    for a in range(s):
        for b in range(s):
            sub = keep[a::s, b::s]; mk[:sub.shape[0], :sub.shape[1]] |= sub
    cv_, cu_ = np.nonzero(mk)
    corners = {}; Pv, tri = [], []

    def idx(i, j):
        k = (i, j)
        if k not in corners:
            u = u0 + j * krok * s; v = v1 - i * krok * s
            corners[k] = len(Pv); Pv.append((u, v))
        return corners[k]
    for i, j in zip(cv_, cu_):
        a, b, c_, d_ = idx(i, j), idx(i, j + 1), idx(i + 1, j + 1), idx(i + 1, j)
        tri += [(a, d_, c_), (a, c_, b)]
    uv = np.array(Pv); U_, V_ = uv[:, 0], uv[:, 1]
    # a corner's depth: the mean of the membrane's cells round it
    ci = np.round((v1 - V_) / krok).astype(int); cj = np.round((U_ - u0) / krok).astype(int)
    acc = np.zeros(len(U_)); cnt = np.zeros(len(U_))
    for di in (-1, 0):
        for dj in (-1, 0):
            ii = np.clip(ci + di, 0, nv - 1); jj = np.clip(cj + dj, 0, nu - 1); ok2 = keep[ii, jj] & np.isfinite(Z[ii, jj])
            acc[ok2] += Z[ii, jj][ok2]; cnt[ok2] += 1
    dep = np.where(cnt > 0, acc / np.maximum(cnt, 1), baza(U_, V_) @ c)
    dep = np.clip(dep, lo_d - .02, hi_d) - wglab                     # never past the frame's own depths
    pos = z_widoku(widok, U_, V_, dep)
    tri = np.array(tri)
    t = pos[tri]; fn = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])       # smooth normals from the faces
    nrm = np.zeros_like(pos)
    for k in range(3): np.add.at(nrm, tri[:, k], fn)
    nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-12)
    if np.mean(nrm @ T.WIDOKI[widok][0]) < 0: nrm = -nrm
    # the winding: its face normal toward the viewer (glTF counter-clockwise front faces)
    t = pos[tri]; fn = np.cross(t[:, 1] - t[:, 0], t[:, 2] - t[:, 0])
    if np.mean(fn @ T.WIDOKI[widok][0]) < 0: tri = tri[:, ::-1]
    return pos, nrm, tri, float(keep.sum() * krok * krok)


# ---------------------------------------------------------------------------------------------------------------- one vehicle
def przygotuj_model(name, m, src, out, podglad=None):
    d = wczytaj(znajdz(src, m["id"]), m)
    P, N, U, I = d["P"], d["N"], d["U"], d["I"]
    mod = T.Model(P, I, U, d["base"], d["normal"], d["mr"])
    try: mod.kola = kola_wstepnie(P, m)                         # where the wheels stand (a recipe keeps its paint off them)
    except SystemExit: mod.kola = []
    przepis = przepisy.PRZEPISY.get(name); mod.nazwa = name
    img0 = mod.img.copy(); mr0 = mod.mapy["mr"].copy() if "mr" in mod.mapy else None
    info = przepis(mod) if przepis else {}
    info = info or {}
    wnetrze_jak_bylo(mod, img0, mr0, info.get("otwory", ()), info.get("okna", ()), log=mod.log)
    przepisy._bez_metalu(mod, przepisy._maska(mod, info.get("okna", ())))  # a body never metal-black in the city's shade
    log = list(mod.log)
    # the glass
    szklo = np.zeros(len(I), bool); panes = []; ciemne = []
    if info.get("okna"):
        szklo = szyby_malowane(mod, info["okna"], info.get("farby_okien", info.get("farby", [])), przyciemniane=info.get("przyciemniane", True),
                               prostokatne=info.get("okna_prostokatne", False))
        if info.get("szyby_w_teksturze"): szklo[:] = False       # a low-poly van: decimetre faces would zigzag the edge
        log.append("szyby malowane: %d faces" % szklo.sum())
    if info.get("otwory"):
        for o in info["otwory"]:
            widok, box = o[0], o[1]; op = dict(o[2]) if len(o) > 2 else {}
            # ciemna: the pane the tinted glass (dark, opaque) - a back window over Tripo's garbled light boot
            ciemna = op.pop("ciemna", False)
            r = szyba_otwarta(mod, widok, box, **op)
            if r is None: log.append("szyba %s %s: no opening found" % (widok, box)); continue
            (ciemne if ciemna else panes).append(r)
            log.append("szyba %s %s: %.2f m2, %d tris%s" % (widok, box, r[3], len(r[2]), ", przyciemniana" if ciemna else ""))
    znaczki = []
    for zn, widok, uc, vc in przepisy.ZNACZKI_NA_AUTACH.get(name, []):
        z = osadz_znaczek(mod, zn, widok, uc, vc)
        if z: znaczki.append(z); log.append("znaczek %s %s (%.2f, %.2f): wystaje %.1f mm" % (zn, widok, uc, vc, ZNACZKI[zn]["gleb"] * 1000))
    tablice = []
    for tekst, rodzaj, widok, uc, vc, w, h in mod.tablice3d:
        z = osadz_tablice(mod, widok, uc, vc, w, h)
        if z: z.update(tekst=tekst, rodzaj=rodzaj, w=w, h=h); tablice.append(z)
        else: log.append("UWAGA tablica %s %s: brak powierzchni" % (tekst, widok))
    for lam in przepisy.REFLEKTORY.get(name, []): mod.reflektor("F", *lam)
    mapa_swiatel(mod, m, log, info.get("farby", ()))
    # the body on its own centre line (a Tripo model can stand off it; the recipes read the views as they are): moved
    cx = (P[:, 0].max() + P[:, 0].min()) / 2
    if abs(cx) > .005:
        P[:, 0] -= cx
        panes = [(p[0] - np.array([cx, 0, 0]),) + tuple(p[1:]) for p in panes]
        ciemne = [(p[0] - np.array([cx, 0, 0]),) + tuple(p[1:]) for p in ciemne]
        for z in znaczki + tablice: z["pos"][0] -= cx
        log.append("środek: przesunięty o %.3f m w x" % -cx)
    # the wheels (the glass and the body first: a wheel never takes glass)
    FF = sasiedztwo(P, I)
    kola = kola_wstepnie(P, m)
    part = tnij_kola(P, I, kola, m["typ"], FF)
    part[szklo & (part == 0)] = -1
    if info.get("opony"):                                        # the tyres' sidewalls: Tripo's garbled letters off
        for i, k in enumerate(kola): zmaz_opone(mod, part == i + 1, k, info["opony"])
        log += [l for l in mod.log[-len(kola):]]
    dopelnij_szwy(mod, U, I, log)
    nowe_kola = np.zeros(0, bool)
    if m.get("zagesc"):
        n0 = len(I); P, N, U, I, part = odchudz(P, N, U, I, part, {"korpus": m.get("trojkaty", 90000), "kolo": 3500, "szyby": 12000})
        mod.P, mod.I, mod.U = P, I, U
        log.append("odchudzony: %d -> %d trójkątów" % (n0, len(I)))
    if name in KOLA and m["typ"] != "motor":
        # Tripo's wheels out, each a new one at its hub: its tyre's outer face where Tripo's was (flush with the arch)
        styl, cale, szer, kolor = KOLA[name]
        stare = part > 0
        # (and what the cut left of Tripo's old tyre on the body: its lettered foot hanging beside the new wheel)
        cc = P[I].mean(1); n0 = int(stare.sum())
        for k in kola:
            rk = np.hypot(cc[:, 2] - k["hz"], cc[:, 1] - k["hy"])
            stare |= (part == 0) & (rk < .97 * k["R"]) & (np.abs(cc[:, 0] - k["hx"]) < k["szer"] / 2 + .02)
        if stare.sum() > n0: log.append("resztki starych opon: %d trójkątów" % (int(stare.sum()) - n0))
        P2, N2, U2, I2, pa = [P], [N], [U], [I[~stare]], [part[~stare]]; n = len(P)
        for i, k in enumerate(kola):
            sg = 1.0 if k["hx"] > 0 else -1.0
            hx = sg * max(abs(k["hx"]) + k["szer"] / 2 - szer / 2, szer / 2)
            wp, wn, wu, wi = KO.kolo(k["R"], szer, cale, styl, kolor)
            wp, wn, wu, wi = KO.zamontuj(wp, wn, wu, wi, hx, k["hy"], k["hz"], sg < 0)
            P2.append(wp); N2.append(wn); U2.append(wu); I2.append(wi + n); pa.append(np.full(len(wi), i + 1)); n += len(wp)
            k["hx"], k["szer"] = hx, szer
        P, N, U, I, part = np.concatenate(P2), np.concatenate(N2), np.concatenate(U2), np.concatenate(I2), np.concatenate(pa)
        nowe_kola = part > 0
        log.append("koła nowe: %s %d\" %.0f mm, %s (%d trójkątów każde)" % (styl, cale, szer * 1000, kolor, int((part == 1).sum())))
    nazwy = []
    for k in kola:
        if k["bok"] == 0: nazwy.append("kolo_przod" if k["pol"] > 0 else "kolo_tyl")
        else: nazwy.append("kolo_%s_%s" % ("przod" if k["pol"] > 0 else "tyl", "lewe" if k["bok"] > 0 else "prawe"))
    return dict(name=name, m=m, P=P, N=N, U=U, I=I, mod=mod, part=part, kola=kola, nazwy=nazwy, panes=panes, ciemne=ciemne, info=info, log=log, skala=d["skala"],
                znaczki=znaczki, nowe_kola=nowe_kola, tablice=tablice)


def osadz_znaczek(mod, zn, widok, uc, vc):
    """where a badge sits (glTF metres, the body's own): its centre on the body's surface at (uc, vc) in the view (the
    front-most surface there), turned to the surface's normal there (its up kept up) - its back 1 mm off the paint"""
    Z = ZNACZKI[zn]; r, rv = Z["szer"] / 2, Z["wys"] / 2
    u, v = T.WIDOKI[widok][1](mod.cen); dv = T.WIDOKI[widok][0]
    f = mod.widoczne(widok, (uc - r, uc + r, vc - rv, vc + rv), cos=.3, glebia=.03, margines=0)
    f &= (np.abs(u - uc) < r) & (np.abs(v - vc) < rv)
    if not f.any(): return None
    w = mod.area[f]; n = (mod.n[f] * w[:, None]).sum(0); n /= np.linalg.norm(n)
    if n @ dv < 0: n = -n
    dep = float(np.max(mod.cen[f] @ dv))
    p = np.zeros(3); eu, ev = (np.array(x, float) for x in T.WIDOKI[widok][1](np.eye(3)))
    p = eu * uc + ev * vc; p = p - (p @ dv) * dv + dv * (dep + .001)
    # its frame: z out of the body (the normal), y up as near as the surface lets it, x = y x z
    yv = np.array([0, 1.0, 0]) - n * n[1]; yv /= np.linalg.norm(yv); xv = np.cross(yv, n)
    R = np.stack([xv, yv, n], 1)
    q = _kwaternion(R)
    return {"znaczek": zn, "pos": p.tolist(), "rot": q, "widok": widok}


def osadz_tablice(mod, widok, uc, vc, w, h):
    """where a plate is screwed on (glTF metres): its centre at (uc, vc) in the view, 4 mm off the front-most surface
    there, turned to that surface (tipped at most 6 degrees down or back, as a plate on its bracket), its up kept up"""
    u, v = T.WIDOKI[widok][1](mod.cen); dv = T.WIDOKI[widok][0]
    box = (uc - w / 2, uc + w / 2, vc - h / 2, vc + h / 2)
    f = mod.widoczne(widok, box, cos=.3, glebia=.04, margines=0) & (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3])
    if not f.any(): return None
    a = mod.area[f]; n = (mod.n[f] * a[:, None]).sum(0); n /= np.linalg.norm(n)
    if n @ dv < 0: n = -n
    n = n - dv * (n @ dv) * 0; n[1] = np.clip(n[1], -.10, .10); n /= np.linalg.norm(n)
    # its plane through the front-most point of the surface behind it, along its own normal
    d = float(np.max(mod.cen[f] @ n)) + .004
    eu, ev = (np.array(x, float) for x in T.WIDOKI[widok][1](np.eye(3)))
    p = eu * uc + ev * vc; t = (d - p @ n) / max(dv @ n, .2); p = p + dv * t
    yv = np.array([0, 1.0, 0]) - n * n[1]; yv /= np.linalg.norm(yv); xv = np.cross(yv, n)
    return {"pos": p.tolist(), "rot": _kwaternion(np.stack([xv, yv, n], 1))}


def _kwaternion(R):
    """a rotation matrix's quaternion (x, y, z, w)"""
    t = np.trace(R)
    if t > 0:
        s_ = np.sqrt(t + 1) * 2; w = .25 * s_; x = (R[2, 1] - R[1, 2]) / s_; y = (R[0, 2] - R[2, 0]) / s_; z = (R[1, 0] - R[0, 1]) / s_
    elif R[0, 0] > R[1, 1] and R[0, 0] > R[2, 2]:
        s_ = np.sqrt(1 + R[0, 0] - R[1, 1] - R[2, 2]) * 2; w = (R[2, 1] - R[1, 2]) / s_; x = .25 * s_; y = (R[0, 1] + R[1, 0]) / s_; z = (R[0, 2] + R[2, 0]) / s_
    elif R[1, 1] > R[2, 2]:
        s_ = np.sqrt(1 + R[1, 1] - R[0, 0] - R[2, 2]) * 2; w = (R[0, 2] - R[2, 0]) / s_; x = (R[0, 1] + R[1, 0]) / s_; y = .25 * s_; z = (R[1, 2] + R[2, 1]) / s_
    else:
        s_ = np.sqrt(1 + R[2, 2] - R[0, 0] - R[1, 1]) * 2; w = (R[1, 0] - R[0, 1]) / s_; x = (R[0, 2] + R[2, 0]) / s_; y = (R[1, 2] + R[2, 1]) / s_; z = .25 * s_
    q = np.array([x, y, z, w]); q /= np.linalg.norm(q); return [round(float(c), 6) for c in q]


def przygotuj_znaczek(zn, src, out):
    """one of the owner's badges for the game: its face to +z, its real width, its depth pressed to what a badge stands
    off a car, its back at z = 0; lighter (MeshLab with the texture, 2500 faces), its maps 512 px - repainted gently: a
    chrome badge's light parts silver and glossy, its dark parts kept; every badge glossy"""
    Z = ZNACZKI[zn]; g, acc, image = KL.read_glb(znajdz(src, Z["id"])); pr = g["meshes"][0]["primitives"][0]
    P = acc(pr["attributes"]["POSITION"]).astype(np.float64); U = acc(pr["attributes"]["TEXCOORD_0"]).astype(np.float64)
    I = acc(pr["indices"]).reshape(-1, 3).astype(np.int64)
    P -= (P.max(0) + P.min(0)) / 2
    s_ = Z["szer"] / (P[:, 0].max() - P[:, 0].min()); P[:, :2] *= s_
    d = P[:, 2].max() - P[:, 2].min(); P[:, 2] = (P[:, 2] - P[:, 2].min()) / max(d, 1e-9) * Z["gleb"]
    if len(I) > 3000:
        P, N, U, I = KL.uprosc(P, I, U, 2500); I = I.astype(np.int64)
    else:
        N = np.zeros_like(P)
    T3 = P[I]; fn = np.cross(T3[:, 1] - T3[:, 0], T3[:, 2] - T3[:, 0]); N = np.zeros_like(P)
    for k in range(3): np.add.at(N, I[:, k], fn)
    N /= np.maximum(np.linalg.norm(N, axis=1, keepdims=True), 1e-12)
    mat = g["materials"][0]

    def obraz(ref):
        return Image.open(io.BytesIO(image(g["textures"][ref["index"]]["source"]))) if ref else None
    kol = np.asarray(obraz(mat["pbrMetallicRoughness"]["baseColorTexture"]).convert("RGB").resize((512, 512), Image.LANCZOS)).astype(np.float32) / 255
    nm = obraz(mat.get("normalTexture")); nm = nm.convert("RGB").resize((512, 512), Image.LANCZOS) if nm else Image.new("RGB", (512, 512), (128, 128, 255))
    L = kol @ np.array([.299, .587, .114], np.float32)
    mr = np.zeros((512, 512, 3), np.uint8); mr[..., 0] = 255
    if Z["farba"] == "chrom":
        # the whole badge chrome: its own light and shade as the polish, lifted (Tripo painted it dark)
        c = np.clip(.42 + .55 * (L - L.min()) / max(float(L.max() - L.min()), 1e-6), 0, 1)
        kol = np.repeat(c[..., None], 3, 2) * np.array([.96, .97, 1.0], np.float32)
        mr[..., 1] = 38; mr[..., 2] = 150
    else:
        jasne = L > .5
        mr[..., 1] = np.where(jasne, 38, 64); mr[..., 2] = np.where(jasne, 150, 0)
    base = "znaczek_%s" % zn; tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(kol, 0, 1) * 255 + .5).astype(np.uint8)).save(tex / (base + "_kolor.jpg"), quality=92)
    nm.save(tex / (base + "_normal__lin.jpg"), quality=95); Image.fromarray(mr).save(tex / (base + "_mr__lin.jpg"), quality=95)
    zapisz_glb(out / (base + ".glb"), base, P, N, U, I)
    return {"znaczek": zn, "plik": base, "kolor": base + "_kolor", "normal": base + "_normal__lin", "mr": base + "_mr__lin",
            "szerokosc": Z["szer"], "trojkaty": int(len(I))}


TAB_KOL, TAB_W, TAB_H = 6, 340, 100                  # the plates' atlas: 6 x 20 slots of 340 x 100 px in 2048 x 2048


def atlas_tablic(modele, out):
    """every classic's plates in one atlas (the game's one material for them): each plate's picture in a slot, its uv
    written into its catalogue entry; the black of a plate's edges in the last slot"""
    A = 2048; im = Image.new("RGB", (A, A), (14, 14, 16)); k = 0
    for e in modele:
        for t in e.get("tablice", []):
            if k >= TAB_KOL * 20 - 1: raise SystemExit("KLASYKI: the plates' atlas is full")
            x0, y0 = (k % TAB_KOL) * TAB_W + 2, (k // TAB_KOL) * TAB_H + 2
            ph = min(TAB_H - 4, int(round((TAB_W - 4) * t["h"] / t["w"]))); pw = int(round(ph * t["w"] / t["h"])) if ph < TAB_H - 4 else TAB_W - 4
            pw = min(pw, TAB_W - 4); ph = int(round(pw * t["h"] / t["w"]))
            p = T.tablica_obraz(t["tekst"], t["rodzaj"], t["w"], t["h"], ppm=int(pw / t["w"] * 3)).convert("RGB").resize((pw, ph), Image.LANCZOS)
            im.paste(p, (x0, y0))
            t["uv"] = [round(x0 / A, 6), round(1 - (y0 + ph) / A, 6), round((x0 + pw) / A, 6), round(1 - y0 / A, 6)]
            k += 1
    tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True)
    im.save(tex / "tablice_kolor.jpg", quality=94)
    mr = np.zeros((A, A, 3), np.uint8); mr[..., 0] = 255; mr[..., 1] = 90               # a plate's paint, a little gloss
    Image.fromarray(mr).save(tex / "tablice_mr__lin.jpg", quality=95)
    Image.new("RGB", (64, 64), (128, 128, 255)).save(tex / "tablice_normal__lin.jpg", quality=95)
    return {"kolor": "tablice_kolor", "normal": "tablice_normal__lin", "mr": "tablice_mr__lin", "ile": k,
            "czern": [round((A - 8) / A, 6), round(4 / A, 6)]}


def wnetrze_jak_bylo(mod, img0, mr0, otwory, okna, log):
    """the inside seen through a window as Tripo painted it: a new coat of the body's paint reached the seats, the parcel
    shelf, the pillars' inner sides (Tripo paints them the body's colour) - in each open window's box every face more
    than 10 cm behind the row's outermost surface there (the frame), and in a painted window measured off the painting
    every face inside its outline (the glass, painted later) get their texels back as they were"""
    n = 0
    # the body's top skin (the roof, the bonnet, the boot lid: facing up, the highest in its 2 cm column) is the outside
    c = mod.cen; key = np.floor(c[:, 0] / .02).astype(np.int64) * 100003 + np.floor(c[:, 2] / .02).astype(np.int64)
    o = np.argsort(key); kk = key[o]; first = np.r_[True, kk[1:] != kk[:-1]]; ks = kk[first]
    ymax = np.maximum.reduceat(c[o, 1], np.nonzero(first)[0]); jj = np.clip(np.searchsorted(ks, key), 0, len(ks) - 1)
    wierzch = (mod.n[:, 1] > .3) & (c[:, 1] > ymax[jj] - .06)
    w_obrysie = przepisy._w_oknie(mod, [o for o in okna if len(o) > 2])
    if w_obrysie.any():
        ys, xs, _ = mod.teksele(w_obrysie); mod.img[ys, xs] = img0[ys, xs]; n += len(ys)
    for o in otwory:
        widok, (u0, u1, v0, v1) = o[0], o[1]
        dv = T.WIDOKI[widok][0]; u, v = T.WIDOKI[widok][1](mod.cen); dep = mod.cen @ dv
        w = (u >= u0) & (u <= u1) & (v >= v0) & (v <= v1)
        if w.sum() < 10: continue
        # the outermost surface per 2 cm row (the frame or the glass there; across a hole, the pillars at its ends)
        rz = np.clip(((v - v0) / .02).astype(int), 0, int((v1 - v0) / .02))
        ref = np.full(rz.max() + 1, -np.inf); np.maximum.at(ref, rz[w], dep[w])
        # (only where the opening is: a 2 cm cell whose nearest surface lies 10 cm behind its row's frame - the roof or a
        # boot lid sloping away above or below the window is no opening, its far part never the inside)
        kc = np.clip(((u - u0) / .02).astype(int), 0, 100000) * 1000 + rz
        o = np.argsort(kc[w]); kk = kc[w][o]; dd = dep[w][o]; first = np.r_[True, kk[1:] != kk[:-1]]
        ks = kk[first]; dmax = np.maximum.reduceat(dd, np.nonzero(first)[0])
        j = np.clip(np.searchsorted(ks, kc), 0, len(ks) - 1); otwor = (ks[j] == kc) & (dmax[j] < ref[rz] - .10)
        # (and the inside seen there: the nearest 15 cm through the opening - a fastback's roof far behind it is no inside)
        gl = w & (dep < ref[rz] - .10) & otwor & (dep > dmax[j] - .15)
        # (a hollow car: through the windscreen its back window's surround, the far side's skin - what the opposite
        # view sees as the outside is never the inside)
        przeciw = {"L": "P", "P": "L", "F": "T", "T": "F"}.get(widok)
        if przeciw: gl &= ~mod.widoczne(przeciw, (-9.0, 9.0, -1.0, 9.0), cos=.15, glebia=.04, margines=0, zewn=True)
        gl &= ~wierzch
        if not gl.any(): continue
        ys, xs, _ = mod.teksele(gl)
        mod.img[ys, xs] = img0[ys, xs]
        if mr0 is not None:
            # (its roughness as it was; never metal: Tripo's metal there, lit by the sky alone, showed as chrome)
            mp = mod.mapy["mr"]; hh, ww = mp.shape[:2]; yy = np.clip(ys * hh // mod.h, 0, hh - 1); xx = np.clip(xs * ww // mod.w, 0, ww - 1)
            mp[yy, xx, 1] = np.maximum(mr0[yy, xx, 1], 120); mp[yy, xx, 2] = np.minimum(mr0[yy, xx, 2], 20)
        n += len(ys)
    if n: log.append("wnętrze za szybami jak u Tripo: %d texels" % n)


def mapa_swiatel(mod, m, log, farby=()):
    """the night's light map (black, the lamps in their light: the game lights them at night through the material's
    emission, off by day): the rear's red lenses (red, saturated, seen from behind, not the body's new paint) and the
    lamps a recipe laid, and the headlamps it marked (measured on the vehicle's front view: Tripo's lenses are too like
    the chrome and a cream paint to be told apart). Indicators and reversing lights stay dark: they light only when used"""
    out = np.zeros_like(mod.img)
    hsv = cv2.cvtColor(np.clip(mod.img, 0, 1).astype(np.float32), cv2.COLOR_RGB2HSV)
    H, Sat, V = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    czerw = ((H < 14) | (H > 344)) & (Sat > .55) & (V > .30)
    lak = getattr(mod, "lakier_maska", None); auto_r = czerw & (lak < .5) if lak is not None else czerw.copy()
    if farby:                                                    # a red car's own paint is no lamp
        auto_r &= roznica(mod.img.reshape(-1, 3), [np.array(f, np.float32) / 255 for f in farby]).reshape(mod.img.shape[:2]) > 14
    top = float(mod.P[:, 1].max()); W = float(mod.P[:, 0].max() - mod.P[:, 0].min()); n = 0
    for widok in ("T",):
        u, v = T.WIDOKI[widok][1](mod.cen)
        vis = mod.widoczne(widok, (-9, 9, .15, top * .80), cos=.5, glebia=.10, margines=0) & (v > .15) & (v < top * .80)
        ys, xs, pts = mod.teksele(vis)
        sel_r = auto_r[ys, xs] if widok == "T" and not any(r == "tyl" for _, _, r in mod.swiatla) else np.zeros(len(ys), bool)
        out[ys[sel_r], xs[sel_r]] = np.clip(mod.img[ys[sel_r], xs[sel_r]] * 1.6, 0, 1) * [1, .35, .3]; n += int(sel_r.sum())
    for widok, box, rodzaj in mod.swiatla:
        u, v = T.WIDOKI[widok][1](mod.cen)
        f = mod.widoczne(widok, box, cos=.3, glebia=.08, margines=0) & (u >= box[0]) & (u <= box[1]) & (v >= box[2]) & (v <= box[3])
        ys, xs, pts = mod.teksele(f); pu, pv = T.WIDOKI[widok][1](pts)
        jj = (pu >= box[0]) & (pu <= box[1]) & (pv >= box[2]) & (pv <= box[3])
        if rodzaj.startswith("przod"):
            if rodzaj == "przod_o":                                                          # a round lens
                r0 = (box[1] - box[0]) / 2; jj &= np.hypot(pu - (box[0] + box[1]) / 2, pv - (box[2] + box[3]) / 2) <= r0
            jj &= (V[ys, xs] > .30) & (Sat[ys, xs] < .45)                                     # the lens, not its rim
            out[ys[jj], xs[jj]] = (1.0, .96, .86)
        else: jj &= czerw[ys, xs]; out[ys[jj], xs[jj]] = np.clip(mod.img[ys[jj], xs[jj]] * 1.6, 0, 1) * [1, .35, .3]
        n += int(jj.sum())
    mod.swiatla_img = out.astype(np.float32)
    log.append("światła na noc: %d texels" % n)


def dopelnij_szwy(mod, U, I, log):
    """the atlas' gutters (the texels no face covers) each the colour of the nearest covered texel, in every map: what the
    restoration painted (a window, a plate, a stripe) then carries to its island's edge, and no seam of the old paint shows
    as a hairline where the GPU blends across it"""
    for nazwa, a in [("kolor", mod.img)] + list(mod.mapy.items()) + ([("swiatla", mod.swiatla_img)] if getattr(mod, "swiatla_img", None) is not None else []):
        h, w = a.shape[:2]; cov = np.zeros((h, w), np.uint8)
        tri = np.round(U[I] * [w, h] * 4).astype(np.int32)
        cv2.fillPoly(cov, list(tri), 1, shift=2)
        for t in tri: cv2.polylines(cov, [t], True, 1, shift=2)              # the thinnest faces too
        pusto = cov == 0
        if not pusto.any() or pusto.all(): continue
        iy, ix = ndimage.distance_transform_edt(pusto, return_distances=False, return_indices=True)
        a[pusto] = a[iy[pusto], ix[pusto]]
        if nazwa == "kolor": log.append("szwy: %d texels poza ścianami dopełnione" % int(pusto.sum()))


def zapisz_model(r, out):
    name, m, mod = r["name"], r["m"], r["mod"]
    tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True)
    base = "klasyk_%s" % name
    mod.obraz().save(tex / (base + "_kolor.jpg"), quality=92)
    mod.mapa("normal").save(tex / (base + "_normal__lin.jpg"), quality=95)
    mr = np.asarray(mod.mapa("mr")).copy(); mr[..., 0] = 255                   # no AO in Tripo's map: R = 1
    Image.fromarray(mr).save(tex / (base + "_mr__lin.jpg"), quality=95)
    Image.fromarray((np.clip(mod.swiatla_img, 0, 1) * 255 + .5).astype(np.uint8)).save(tex / (base + "_swiatla.jpg"), quality=90)
    P, N, U, I, part = r["P"], r["N"], r["U"], r["I"], r["part"]
    czesci = []

    def czesc(nazwa, faces, piv, lod_key, extra=None):
        used = np.unique(faces); remap = -np.ones(len(P), int); remap[used] = np.arange(len(used))
        plik = "%s_%s" % (base, nazwa); pos = P[used] - piv; tri = remap[faces]; uv = U[used]
        zapisz_glb(out / (plik + ".glb"), plik, pos, N[used], uv, tri)
        e = {"nazwa": nazwa, "plik": plik, "pivot": [round(-piv[0], 5), round(piv[1], 5), round(piv[2], 5)], "trojkaty": int(len(faces)),
             "promien": 0.0, "szerokosc": 0.0, "lody": []}
        if lod_key and KL.pymeshlab is not None:
            for k, udzial in enumerate(LODY[lod_key], 1):
                p3, n3, u3, f3 = KL.uprosc(pos, tri, uv, max(60, len(tri) * udzial))
                zapisz_glb(out / ("%s_lod%d.glb" % (plik, k)), "%s_lod%d" % (plik, k), p3, n3, u3, f3)
                e["lody"].append({"plik": "%s_lod%d" % (plik, k), "trojkaty": int(len(f3))})
        if extra: e.update(extra)
        czesci.append(e); return e
    czesc("korpus", I[part == 0], np.zeros(3), "korpus")
    for i, (k, nazwa) in enumerate(zip(r["kola"], r["nazwy"])):
        czesc(nazwa, I[part == i + 1], np.array([k["hx"], k["hy"], k["hz"]]), "kolo",
              dict({"promien": round(float(k["R"]), 4), "szerokosc": round(float(k["szer"]), 4)},
                   **({"material": "kola"} if r.get("nowe_kola") is not None and r["nowe_kola"].any() else {})))
    szyby = None
    if (part == -1).any():
        e = czesc("szyby", I[part == -1], np.zeros(3), None); szyby = {"plik": e["plik"], "rodzaj": "przyciemniane"}
    # the new panes: the clear ones (the open windows) and the tinted ones (a back window given dark glass)
    for klucz, nazwa in (("panes", "szyby_otwarte"), ("ciemne", "szyby_ciemne")):
        if not r.get(klucz): continue
        pos = np.concatenate([p[0] for p in r[klucz]]); nrm = np.concatenate([p[1] for p in r[klucz]])
        off = np.cumsum([0] + [len(p[0]) for p in r[klucz]])[:-1]
        tri = np.concatenate([p[2] + o for p, o in zip(r[klucz], off)])
        tri = np.concatenate([tri, tri[:, ::-1]])                                  # both sides (the inside looks out)
        plik = "%s_%s" % (base, nazwa)
        zapisz_glb(out / (plik + ".glb"), plik, pos, nrm, np.zeros((len(pos), 2)), tri)
        czesci.append({"nazwa": nazwa, "plik": plik, "pivot": [0.0, 0.0, 0.0], "trojkaty": int(len(tri)), "promien": 0.0, "szerokosc": 0.0, "lody": []})
        szyby = dict(szyby or {}, **{"otwarte" if klucz == "panes" else "ciemne": plik})
    lo, hi = P.min(0), P.max(0)
    kp = [k for k in r["kola"] if k["pol"] > 0]; kt = [k for k in r["kola"] if k["pol"] < 0]
    rozstaw = float(np.mean([k["hz"] for k in kp]) - np.mean([k["hz"] for k in kt]))
    return {"model": name, "nazwa": m["nazwa"], "typ": m["typ"], "wzor": m["wzor"], "skala_tripo": round(r["skala"], 5),
            "czesci": czesci, "szyby": szyby or {}, "kolor": base + "_kolor", "normal": base + "_normal__lin", "mr": base + "_mr__lin", "swiatla": base + "_swiatla",
            "dlugosc": round(float(hi[2] - lo[2]), 3), "szerokosc": round(float(hi[0] - lo[0]), 3), "wysokosc": round(float(hi[1] - lo[1]), 3),
            "rozstaw_osi": round(rozstaw, 3), "trojkaty": int(sum(c["trojkaty"] for c in czesci)), "napisy": r["info"].get("napisy", []),
            "znaczki": [{"znaczek": z["znaczek"], "plik": "znaczek_" + z["znaczek"], "pos": [round(-z["pos"][0], 5), round(z["pos"][1], 5), round(z["pos"][2], 5)],
                         "rot": [z["rot"][0], -z["rot"][1], -z["rot"][2], z["rot"][3]]} for z in r.get("znaczki", [])],
            "tablice": [{"tekst": z["tekst"], "rodzaj": z["rodzaj"], "w": z["w"], "h": z["h"],
                         "pos": [round(-z["pos"][0], 5), round(z["pos"][1], 5), round(z["pos"][2], 5)],
                         "rot": [z["rot"][0], -z["rot"][1], -z["rot"][2], z["rot"][3]]} for z in r.get("tablice", [])],
            "log": r["log"]}


def podglad_ciecia(r, path):
    """the cut seen from both sides: the body grey, each wheel its own colour, the glass blue, the new panes cyan"""
    import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
    P, I, part = r["P"], r["I"], r["part"]; cen = P[I].mean(1)
    kol = np.array([[.6, .6, .6], [1, 0, 0], [0, .7, 0], [1, .5, 0], [.7, 0, .9], [.2, .2, .2]])
    fig, axs = plt.subplots(1, 3, figsize=(24, 5), gridspec_kw={"width_ratios": [3, 3, 1.5]})
    for ax, sgn in zip(axs[:2], (1, -1)):
        s = cen[:, 0] * sgn > -.05 if r["m"]["typ"] != "motor" else np.ones(len(cen), bool)
        c = np.where(part[:, None] == -1, [[.1, .3, 1]], kol[np.clip(part, 0, 5)])
        o = np.argsort(cen[s, 0] * sgn)
        ax.scatter(cen[s][o, 2] * sgn, cen[s][o, 1], s=.3, c=c[s][o]); ax.set_aspect("equal")
        for p in r["panes"]: ax.scatter(p[0][:, 2] * sgn, p[0][:, 1], s=.2, c="c")
        for k in r["kola"]:
            if k["bok"] * sgn >= 0: ax.add_patch(plt.Circle((k["hz"] * sgn, k["hy"]), k["R"], fill=False, color="k", lw=.6))
        ax.set_title("%s %s" % (r["name"], "lewy bok" if sgn > 0 else "prawy bok (lustro)"))
    s = cen[:, 2] > 0; o = np.argsort(cen[s, 2])
    c = np.where(part[:, None] == -1, [[.1, .3, 1]], kol[np.clip(part, 0, 5)])
    axs[2].scatter(cen[s][o, 0], cen[s][o, 1], s=.3, c=c[s][o]); axs[2].set_aspect("equal"); axs[2].set_title("przód")
    fig.savefig(path, dpi=80, bbox_inches="tight"); plt.close(fig)


def sha256(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for b in iter(lambda: f.read(1 << 20), b""): h.update(b)
    return h.hexdigest()


def spakuj(lib, zrodla, pack_dir, opis):
    """tar(oryginal/ + pojazdy_klasyki_01/) -> openssl cms -encrypt -aes256 (DER) to the CI transfer certificate -> 90 MB
    parts + manifest.json: the format install_sklep_modele.py opens (as przygotuj_klady.spakuj)"""
    wzor = ROOT / "unity-city/Prywatne/braxhorn-sklep-motory-01"
    pack = Path(pack_dir); pack.mkdir(parents=True, exist_ok=True)
    for f in ("recipient.crt", "recipient.pem.enc"): shutil.copy2(wzor / f, pack / f)
    with tempfile.TemporaryDirectory() as tmp:
        # (tarred straight from where the files are: a staged copy of them would need the disk twice over)
        skad = {"oryginal/%s_tripo.glb" % model: Path(p) for model, p in zrodla.items()}
        skad.update({"%s/%s" % (TARGET, f.relative_to(lib).as_posix()): f for f in Path(lib).rglob("*") if f.is_file()})
        files = {rel: sha256(skad[rel]) for rel in sorted(skad)}
        tar = Path(tmp) / "pack.tar"
        with tarfile.open(tar, "w") as t:
            for rel in files: t.add(skad[rel], arcname=rel)
        cms = Path(tmp) / "pack.cms"
        subprocess.run(["openssl", "cms", "-encrypt", "-binary", "-aes256", "-outform", "DER", "-in", str(tar), "-out", str(cms),
                        str(pack / "recipient.crt")], check=True)
        # (a short write - the disk full - must never leave a broken pack behind)
        if cms.stat().st_size < tar.stat().st_size: raise SystemExit("KLASYKI_PACK: the encrypted pack is shorter than its tar - disk full?")
        tar_sha = sha256(tar); tar.unlink()                                    # (its room for the parts)
        for old in pack.glob("part-*.cms"): old.unlink()
        data = cms.read_bytes(); parts = []; S = 90 * 1024 * 1024
        for i in range(0, len(data), S):
            name = "part-%03d.cms" % (i // S); (pack / name).write_bytes(data[i:i + S])
            parts.append({"name": name, "bytes": len(data[i:i + S]), "sha256": sha256(pack / name)})
        manifest = {"source": opis, "target": TARGET,
                    "staging": "unity-city/Assets/Source/Prywatne/Miasto/" + TARGET + " (BraxMiastoAssety.Model)",
                    "format": "tar -> openssl cms -encrypt -aes256 (DER) to recipient.crt (Braxhorn camp input transfer; its private key recipient.pem.enc is protected by KLUCZ in Actions) -> split 90 MB",
                    "tarSha256": tar_sha, "cmsSha256": sha256(cms), "parts": parts, "files": files,
                    "recipientCertificateSha256": sha256(pack / "recipient.crt")}
        (pack / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + "\n")
    print("KLASYKI_PACK %s: %d files, %d parts" % (pack, len(files), len(parts)))


def main(a):
    if len(a) < 2: raise SystemExit(__doc__)
    src, out = Path(a[0]), Path(a[1]) / TARGET
    tylko = a[a.index("--tylko") + 1].split(",") if "--tylko" in a else list(MODELE)
    podglad = Path(a[a.index("--podglad") + 1]) if "--podglad" in a else None
    if out.exists() and "--tylko" not in a: shutil.rmtree(out)
    out.mkdir(parents=True, exist_ok=True)
    kat_path = out / "katalog.json"
    katalog = json.loads(kat_path.read_text()) if kat_path.exists() else {
        "zrodlo": "Tripo właściciela 24.11.2026 22:21-23:09 (trzy paczki klasyków), przygotowane przez unity-tools/mistrzowie/klasyki/przygotuj_klasyki.py",
        "osie": "Unity: +x w prawo, +y w górę, +z przód; metry rzeczywiste (przed K); pivot = punkt obrotu części", "modele": []}
    for name in tylko:
        m = MODELE[name]
        r = przygotuj_model(name, m, src, out)
        e = zapisz_model(r, out)
        if podglad: podglad.mkdir(parents=True, exist_ok=True); podglad_ciecia(r, podglad / ("ciecie_%s.png" % name))
        katalog["modele"] = [x for x in katalog["modele"] if x["model"] != name] + [e]
        kola = [c for c in e["czesci"] if c["nazwa"].startswith("kolo")]
        print("KLASYK %s: %s x %s x %s m, rozstaw osi %s m (wzór %s), koła %s, %d trójkątów, szyby %s" % (
            name, e["dlugosc"], e["szerokosc"], e["wysokosc"], e["rozstaw_osi"], m["wb"],
            " ".join("%s R%.3f szer%.3f %dtri" % (c["nazwa"][5:], c["promien"], c["szerokosc"], c["trojkaty"]) for c in kola),
            e["trojkaty"], e["szyby"] or "-"))
        for line in e["log"]: print("   ", line)
    katalog["znaczki"] = [przygotuj_znaczek(zn, src, out) for zn in ZNACZKI]
    tex = out / "_tekstury"; tex.mkdir(parents=True, exist_ok=True); ak, an, am = KO.atlas()
    ak.save(tex / "kola_kolor.jpg", quality=92); an.save(tex / "kola_normal__lin.jpg", quality=95); am.save(tex / "kola_mr__lin.jpg", quality=95)
    katalog["kola"] = {"kolor": "kola_kolor", "normal": "kola_normal__lin", "mr": "kola_mr__lin"}
    katalog["modele"].sort(key=lambda x: list(MODELE).index(x["model"]))
    katalog["tablice"] = atlas_tablic(katalog["modele"], out)
    kat_path.write_text(json.dumps(katalog, ensure_ascii=False, indent=1))
    if "--pack" in a:
        zrodla = {n: znajdz(src, MODELE[n]["id"]) for n in MODELE}
        zrodla.update({"znaczek_" + zn: znajdz(src, Z["id"]) for zn, Z in ZNACZKI.items()})
        spakuj(out, zrodla, a[a.index("--pack") + 1],
               "Właściciel 24.11.2026 22:21-23:11: polskie klasyki z Tripo (auta, ciężarówki Star, motory) - odnowione, nowe "
               "napisy i tablice, wymienione szyby, koła osobno (kręcą się, przednie skręcają), prawdziwe wymiary; miejsce: "
               "oświetlony bunkier pod górką przy torze kładów (wjazd od lasu)")


if __name__ == "__main__":
    main(sys.argv[1:])
