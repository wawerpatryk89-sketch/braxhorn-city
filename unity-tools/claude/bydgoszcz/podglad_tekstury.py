"""OSIEDLE BYDGOSKIE — zdjęcia z TEKSTURAMI przed buildem (23.11.2026).

Właściciel 23.11, 08:02–08:07: „zanim puścić build, zdjęcia chcę widzieć, jak to będzie wyglądać”, „asfalt jest asfaltem, budynki budynkami,
wszystko wykończone, Poly Haven”. Rysuje te same trójkąty co gra (osiedle_gen.py) z buforem głębi i teksturami Poly Haven 1K,
kafel i barwa jak w BraxBydgoszcz.Mat. To PRZYBLIŻENIE: bez światła, cieni i trawy z silnika, bez drzew. Prawdziwe zdjęcie daje tylko build.

    python3 podglad_tekstury.py <katalog_na_tekstury> <katalog_wyjscia>
"""
import io, json, math, struct, sys, urllib.request
from pathlib import Path
import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.argv_kopia = sys.argv[:]; sys.argv = [sys.argv[0], "--bez-podgladu"]
import osiedle_gen as G
sys.argv = sys.argv_kopia
UP = G.UP

# rodzaj → (zestawy Poly Haven w kolejności BraxBydgoszcz.Mat, kafel w metrach, czy mnożyć przez barwę materiału)
TEX = {"tynk": (["white_stucco_02"], 2.0, True), "beton": (["brushed_concrete_04"], 2.0, True), "cegla": (["brick_wall_02"], 1.0, False),
       "blacha": (["box_profile_metal_sheet"], 2.0, True), "drewno": (["weathered_brown_planks"], 1.5, True), "asfalt": (["clean_asphalt", "asphalt_02", "asphalt_04"], 4.0, False),
       "asfalt_stary": (["asphalt_04", "asphalt_02", "clean_asphalt"], 4.0, False), "chodnik": (["concrete_pavers", "pavement_02"], 2.0, False),
       "ziemia": (["brown_mud_dry"], 2.0, False), "trawa": (["leafy_grass"], 2.0, True)}
TRAWA_TINT = np.array([.10, .16, .04]) / np.array([.326, .24, .105])   # trawa R1: Grass004 zestrojona do luminancji 0,133 (BraxRegionTrawa) — tu przybliżona skanem leafy_grass


def piramida(obraz, alfa=False):
    tryb = "RGBA" if alfa else "RGB"; obraz = obraz.convert(tryb)
    if alfa and max(obraz.size) > 1024: obraz = obraz.resize((obraz.width * 1024 // max(obraz.size), obraz.height * 1024 // max(obraz.size)), Image.LANCZOS)
    p = [np.asarray(obraz, np.float32) / 255.0]
    while min(p[-1].shape[:2]) > 8:
        obraz = obraz.resize((max(1, obraz.width // 2), max(1, obraz.height // 2)), Image.LANCZOS); p.append(np.asarray(obraz, np.float32) / 255.0)
    for x in p: x[..., :3] **= 2.2   # do przestrzeni liniowej (alfa bez zmian)
    return p


LIB = G.TU.parents[2] / "unity-city/Assets/Source/Claude/Library"
_GLB = {}
def model(id, korona="liscie_2"):
    """Model z biblioteki właściciela (.glb) — te same pliki, które stawia gra. Węzły: pień + najlżejsza korona (liscie_2) albo cały krzew."""
    if (id, korona) in _GLB: return _GLB[(id, korona)]
    d = (LIB / (id + ".glb")).read_bytes(); n = struct.unpack_from("<I", d, 12)[0]; j = json.loads(d[20:20 + n]); b0 = 20 + n + 8
    def acc(i):
        a = j["accessors"][i]; bv = j["bufferViews"][a["bufferView"]]; k = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4}[a["type"]]
        dt = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}[a["componentType"]]
        x = np.frombuffer(d, dt, a["count"] * k, b0 + bv.get("byteOffset", 0) + a.get("byteOffset", 0)); return x.reshape(a["count"], k) if k > 1 else x
    obrazy = {}
    def obraz(i):
        if i not in obrazy:
            bv = j["bufferViews"][j["images"][i]["bufferView"]]; o = b0 + bv.get("byteOffset", 0)
            obrazy[i] = piramida(Image.open(io.BytesIO(d[o:o + bv["byteLength"]])), True)
        return obrazy[i]
    wezly = {"pien", korona} if any(nd.get("name") == korona for nd in j["nodes"]) else None
    czesci = []
    for nd in j["nodes"]:
        if "mesh" not in nd or (wezly and nd.get("name") not in wezly): continue
        for pr in j["meshes"][nd["mesh"]]["primitives"]:
            pos = acc(pr["attributes"]["POSITION"]).astype(float) * np.array([-1, 1, 1]) * np.array(nd.get("scale", [1, 1, 1])) + np.array(nd.get("translation", [0, 0, 0]))
            uv = acc(pr["attributes"]["TEXCOORD_0"]).astype(float)
            idx = (acc(pr["indices"]).astype(np.int64) if "indices" in pr else np.arange(len(pos))).reshape(-1, 3)
            mat = j["materials"][pr["material"]]; pbr = mat.get("pbrMetallicRoughness", {})
            tex = obraz(j["textures"][pbr["baseColorTexture"]["index"]]["source"]) if "baseColorTexture" in pbr else None
            ciach = mat.get("alphaCutoff", .45) if mat.get("alphaMode") == "MASK" else (.25 if mat.get("alphaMode") == "BLEND" else None)
            czesci.append((pos, uv, idx, tex, np.array(pbr.get("baseColorFactor", [1, 1, 1, 1])[:3]) ** 2.2, ciach))
    lo = min(c[0][:, 1].min() for c in czesci); hi = max(c[0][:, 1].max() for c in czesci)
    # jak BraxRegion.Put: środek obrysu nad punktem postawienia (drzewo_verdant ma w pliku przesunięcie ~(10; −73))
    mn = np.min([c[0].min(0) for c in czesci], 0); mx = np.max([c[0].max(0) for c in czesci], 0); sr = (mn + mx) / 2
    czesci = [(c[0] - np.array([sr[0], 0, sr[2]]),) + tuple(c[1:]) for c in czesci]
    _GLB[(id, korona)] = (czesci, hi - lo, lo); return _GLB[(id, korona)]


def drzewa_w_scenie(S, oko, zasieg):
    """Trójkąty modeli z S.rekw w promieniu `zasieg` od kamery: (pozycje świata [n,3,3], uv [n,3,2], tex, barwa, cutoff)."""
    wynik = []
    for (mid, x, y, z, yaw, h) in S.rekw:
        if (x - oko[0]) ** 2 + (z - oko[2]) ** 2 > zasieg ** 2: continue
        blisko = (x - oko[0]) ** 2 + (z - oko[2]) ** 2 < 32 ** 2     # z bliska gra pokazuje pełną koronę (LOD0 = liscie_0)
        czesci, wys, lo = model(mid, "liscie_0" if blisko else "liscie_2"); skala = h / wys if h > 0 else 1.0
        c, s_ = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        for pos, uv, idx, tex, barwa, ciach in czesci:
            P = (pos - np.array([0, lo, 0])) * skala; P = np.column_stack([P[:, 0] * c + P[:, 2] * s_, P[:, 1], -P[:, 0] * s_ + P[:, 2] * c]) + np.array([x, y, z])
            wynik.append((P[idx], uv[idx], tex, barwa, ciach))
    return wynik


def tekstury(kat):
    kat.mkdir(parents=True, exist_ok=True); wynik = {}; opis = {}
    for rodzaj, (slugi, metry, barwic) in TEX.items():
        for s in slugi:
            plik = kat / (s + "_diff_1k.jpg")
            if not plik.exists():
                try:
                    req = urllib.request.Request("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/%s/%s_diff_1k.jpg" % (s, s), headers={"User-Agent": "braxhorn-city-podglad/1.0"})
                    plik.write_bytes(urllib.request.urlopen(req, timeout=40).read())
                except Exception:
                    continue
            try: wynik[rodzaj] = (piramida(Image.open(plik)), metry, barwic); opis[rodzaj] = s; break
            except Exception: continue
    sciana = G.MURAL.with_name("mural_bar_mleczny_sciana.jpg")
    if sciana.exists(): wynik["obraz"] = (piramida(Image.open(sciana)), 1.0, False); opis["obraz"] = "mural właściciela (cała ściana)"
    return wynik, opis


def zdjecie(S, tex, plik, oko, cel, kat=68, W=1600, H=900):
    oko, cel = np.array(oko, float), np.array(cel, float); f = cel - oko; f /= np.linalg.norm(f)
    r = np.cross(UP, f); r /= np.linalg.norm(r); u = np.cross(f, r)
    slonce = np.array([-.5, .75, -.35]); slonce /= np.linalg.norm(slonce)
    t = math.tan(math.radians(kat / 2)); sy = (H / 2) / t; sx = sy; BL = .35
    gora, dol = np.array([.30, .52, .86]) ** 2.2, np.array([.74, .84, .95]) ** 2.2
    img = (dol[None, None, :] + (gora - dol)[None, None, :] * np.clip(1.25 - np.arange(H) / (H * .55), 0, 1)[:, None, None]) * np.ones((H, W, 3)); zb = np.full((H, W), np.inf)
    niebo_odb = dol * .8
    # --- mapa cieni (rzut prostokątny od słońca), 160 m wokół celu kamery
    ls = -slonce; lr = np.cross(UP, ls); lr /= np.linalg.norm(lr); lu = np.cross(ls, lr); R_SM, SM = 2048, 160.0; srod = cel.copy()
    smap = np.full((R_SM, R_SM), np.inf)
    def do_sm(Pw):
        d = Pw - srod; return np.column_stack([(d @ lr / SM + .5) * R_SM, (.5 - d @ lu / SM) * R_SM, d @ ls])
    for (g, m), (v, i) in S.m.items():
        L = do_sm(np.asarray(v)[:, :3]); T = np.asarray(i).reshape(-1, 3)
        for tri in T:
            X, Y, Z = L[tri, 0], L[tri, 1], L[tri, 2]
            x0, x1 = max(int(X.min()), 0), min(int(X.max()) + 1, R_SM - 1); y0, y1 = max(int(Y.min()), 0), min(int(Y.max()) + 1, R_SM - 1)
            if x0 > x1 or y0 > y1: continue
            den = (Y[1] - Y[2]) * (X[0] - X[2]) + (X[2] - X[1]) * (Y[0] - Y[2])
            if abs(den) < 1e-9: continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            l0 = ((Y[1] - Y[2]) * (gx - X[2]) + (X[2] - X[1]) * (gy - Y[2])) / den; l1 = ((Y[2] - Y[0]) * (gx - X[2]) + (X[0] - X[2]) * (gy - Y[2])) / den; l2 = 1 - l0 - l1
            ok = (l0 >= -.02) & (l1 >= -.02) & (l2 >= -.02)
            zz = l0 * Z[0] + l1 * Z[1] + l2 * Z[2]; sub = smap[y0:y1 + 1, x0:x1 + 1]; w = ok & (zz < sub); sub[w] = zz[w]
    modele = drzewa_w_scenie(S, oko, 115.0)
    for TR, UV, tx_, barwa_, ciach in modele:
        for tri, uvt in zip(TR, UV):
            L = do_sm(tri); X, Y, Z = L[:, 0], L[:, 1], L[:, 2]
            x0, x1 = max(int(X.min()), 0), min(int(X.max()) + 1, R_SM - 1); y0, y1 = max(int(Y.min()), 0), min(int(Y.max()) + 1, R_SM - 1)
            if x0 > x1 or y0 > y1: continue
            den = (Y[1] - Y[2]) * (X[0] - X[2]) + (X[2] - X[1]) * (Y[0] - Y[2])
            if abs(den) < 1e-9: continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
            l0 = ((Y[1] - Y[2]) * (gx - X[2]) + (X[2] - X[1]) * (gy - Y[2])) / den; l1 = ((Y[2] - Y[0]) * (gx - X[2]) + (X[0] - X[2]) * (gy - Y[2])) / den; l2 = 1 - l0 - l1
            ok = (l0 >= 0) & (l1 >= 0) & (l2 >= 0)
            if ciach is not None and tx_ is not None and ok.any():
                tm = tx_[min(3, len(tx_) - 1)]; th, tw = tm.shape[:2]
                uu = l0 * uvt[0, 0] + l1 * uvt[1, 0] + l2 * uvt[2, 0]; vv = l0 * uvt[0, 1] + l1 * uvt[1, 1] + l2 * uvt[2, 1]
                ok &= tm[(vv * th).astype(np.int64) % th, (uu * tw).astype(np.int64) % tw, 3] >= ciach
            zz = l0 * Z[0] + l1 * Z[1] + l2 * Z[2]; sub = smap[y0:y1 + 1, x0:x1 + 1]; w = ok & (zz < sub); sub[w] = zz[w]

    def raster(q, baza, pir, metry, amb, dif, szklo, gltf=False, ciach=None):
        z = q[:, 2]; X = q[:, 0] / z * sx + W / 2; Y = H / 2 - q[:, 1] / z * sy
        x0, x1 = max(int(math.floor(X.min())), 0), min(int(math.ceil(X.max())), W - 1); y0, y1 = max(int(math.floor(Y.min())), 0), min(int(math.ceil(Y.max())), H - 1)
        if x0 > x1 or y0 > y1: return
        den = (Y[1] - Y[2]) * (X[0] - X[2]) + (X[2] - X[1]) * (Y[0] - Y[2])
        if abs(den) < 1e-7: return
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + .5, np.arange(y0, y1 + 1) + .5)
        l0 = ((Y[1] - Y[2]) * (gx - X[2]) + (X[2] - X[1]) * (gy - Y[2])) / den; l1 = ((Y[2] - Y[0]) * (gx - X[2]) + (X[0] - X[2]) * (gy - Y[2])) / den; l2 = 1 - l0 - l1
        iz = l0 / z[0] + l1 / z[1] + l2 / z[2]
        sub = zb[y0:y1 + 1, x0:x1 + 1]; ok = (l0 >= -1e-4) & (l1 >= -1e-4) & (l2 >= -1e-4) & (iz > 0)
        ok &= (1.0 / np.where(iz > 0, iz, 1)) < sub
        if not ok.any(): return
        gl = 1.0 / iz[ok]
        if pir is not None:
            uu = (l0[ok] * q[0, 3] / z[0] + l1[ok] * q[1, 3] / z[1] + l2[ok] * q[2, 3] / z[2]) * gl / metry
            vv = (l0[ok] * q[0, 4] / z[0] + l1[ok] * q[1, 4] / z[1] + l2[ok] * q[2, 4] / z[2]) * gl / metry
            apx = abs(den) / 2; auv = abs((q[1, 3] - q[0, 3]) * (q[2, 4] - q[0, 4]) - (q[2, 3] - q[0, 3]) * (q[1, 4] - q[0, 4])) / 2 / metry ** 2
            tpp = math.sqrt(max(auv, 1e-12) / max(apx, 1e-9)) * pir[0].shape[1]
            tx = pir[int(np.clip(round(math.log2(max(tpp, 1e-6))), 0, len(pir) - 1))]; th, tw = tx.shape[:2]
            probka = tx[((vv if gltf else 1 - vv) * th).astype(np.int64) % th, (uu * tw).astype(np.int64) % tw]
            if ciach is not None:
                zost = probka[:, 3] >= ciach
                if not zost.any(): return
                iy_, ix_ = np.nonzero(ok); ok = np.zeros_like(ok); ok[iy_[zost], ix_[zost]] = True; gl = gl[zost]; probka = probka[zost]
            kol = probka[:, :3] * baza
        else:
            kol = np.broadcast_to(baza, (int(ok.sum()), 3)).copy()
            if szklo: kol = kol * .55 + niebo_odb * .45
        if dif > 0:
            xs = (gx[ok] - W / 2) / sx * gl; ys = (H / 2 - gy[ok]) / sy * gl
            Pw = oko + np.outer(xs, r) + np.outer(ys, u) + np.outer(gl, f); L = do_sm(Pw)
            w_sm = (L[:, 0] >= 1) & (L[:, 0] < R_SM - 2) & (L[:, 1] >= 1) & (L[:, 1] < R_SM - 2); swiatlo = np.zeros(len(L))
            for ddx in (-1, 0, 1):          # miękka krawędź cienia (3 × 3 próbki), bez schodków na ścianach
                for ddy in (-1, 0, 1):
                    ix = np.clip(L[:, 0].astype(int) + ddx, 0, R_SM - 1); iy = np.clip(L[:, 1].astype(int) + ddy, 0, R_SM - 1)
                    swiatlo += np.where(w_sm & (L[:, 2] > smap[iy, ix] + .25), 0.0, 1.0)
            swiatlo /= 9.0
            kol = kol * (amb + dif * swiatlo)[:, None]
        else:
            kol = kol * amb
        mg = np.clip((gl - 120) / 900, 0, .4)[:, None]; kol = kol * (1 - mg) + dol * mg
        sub[ok] = gl; img[y0:y1 + 1, x0:x1 + 1][ok] = kol

    for (g, m), (v, i) in S.m.items():
        a = np.asarray(v); T = np.asarray(i).reshape(-1, 3); P = a[:, :3] - oko
        C = np.column_stack([P @ r, P @ u, P @ f, a[:, 6], a[:, 7]])
        rodzaj = G.MATS[m][1]; barwa = TRAWA_TINT if rodzaj == "trawa" else np.array(G.tint(*G.MATS[m])) ** 2.2
        pir, metry, barwic = tex.get(rodzaj, (None, 1.0, True))
        baza = barwa if (pir is None or barwic) else np.ones(3)
        nn = a[T[:, 0], 3:6]; przodem = np.einsum("ij,ij->i", nn, a[T[:, 0], :3] - oko) < 0
        amb_t = .30 + .14 * np.clip(nn[:, 1], 0, 1); dif_t = .78 * np.clip(nn @ slonce, 0, 1)
        for tri, am, di in zip(T[przodem], amb_t[przodem], dif_t[przodem]):
            w = C[tri]
            if (w[:, 2] <= BL).all(): continue
            if abs(w[:, 0] / np.maximum(w[:, 2], BL)).min() > t * W / H * 1.5 and (np.sign(w[:, 0]) == np.sign(w[0, 0])).all() and (w[:, 2] > BL).all(): continue
            if (w[:, 2] <= BL).any():
                nowe = []
                for k in range(3):
                    p1, p2 = w[k], w[(k + 1) % 3]
                    if p1[2] > BL: nowe.append(p1)
                    if (p1[2] > BL) != (p2[2] > BL): nowe.append(p1 + (p2 - p1) * (BL - p1[2]) / (p2[2] - p1[2]))
                w = np.array(nowe)
            for k in range(1, len(w) - 1): raster(w[[0, k, k + 1]], baza, pir, metry, am, di, rodzaj == "szklo")
    for TR, UV, tx_, barwa_, ciach in modele:
        if tx_ is None: continue
        for tri, uvt in zip(TR, UV):
            P = tri - oko; w = np.column_stack([P @ r, P @ u, P @ f, uvt[:, 0], uvt[:, 1]])
            if (w[:, 2] <= BL).any(): continue
            nn = np.cross(tri[1] - tri[0], tri[2] - tri[0]); dl_ = np.linalg.norm(nn)
            if dl_ < 1e-9: continue
            nn /= dl_
            if ciach is None and np.dot(nn, tri[0] - oko) > 0: continue
            am = .34 + .10 * abs(nn[1]); di = .70 * (abs(nn @ slonce) if ciach is not None else max(nn @ slonce, 0))
            raster(w, barwa_, tx_, 1.0, am, di, False, True, ciach)
    out = np.clip(img, 0, 1) ** (1 / 2.2)
    Image.fromarray((out * 255).astype(np.uint8)).resize((1280, 720), Image.LANCZOS).save(plik, quality=92)


def main():
    kat_tex, wyj = Path(sys.argv[1]), Path(sys.argv[2]); wyj.mkdir(parents=True, exist_ok=True)
    S, opis = G.main(); tex, uzyte = tekstury(kat_tex); print("tekstury:", uzyte, "| bez tekstury:", sorted(set(TEX) - set(uzyte)))
    k2, k3 = opis["kamery_podgladu"]["2"], opis["kamery_podgladu"]["3"] if "kamery_podgladu" in opis else ((-40, 20), (-40, 0))
    kadry = {"1_blok_32a": ((k2[0], 1.8, k2[1]), (0, 12, 0)), "2_garaze_i_blok": ((k3[0], 1.8, k3[1]), (-54, 4, 30))}
    mur = next((np.asarray(v) for (g, m), (v, i) in S.m.items() if m.startswith("mural_bar_mleczny")), None)
    if mur is not None:
        c = mur[:, :3].mean(0); n = mur[0, 3:6]; kadry["3_bar_mleczny_mural"] = (tuple(c + n * 8.5 + np.cross(n, UP) * 3 + UP * (1.8 - c[1])), tuple(c + UP * .3))
    for nazwa in (sys.argv[3:] or kadry):
        oko, cel = kadry[nazwa]; zdjecie(S, tex, wyj / ("zdjecie_%s.jpg" % nazwa), oko, cel); print("zapisano", nazwa, flush=True)


if __name__ == "__main__":
    main()
