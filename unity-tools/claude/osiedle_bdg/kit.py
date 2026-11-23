"""MIASTO R1 — osiedle bydgoskie (Skłodowskiej-Curie): zestaw elementów bloku z wielkiej płyty po dociepleniu.

Właściciel 23.11.2026: „rób to tak, jak my robimy, wszystko tak, jak powinno być realistycznie”, „nic nie rób na oko”.
Wymiary elementów są z jego zdjęć (wejście 32a, IMG_1427) i z normalnych wymiarów bloków z lat 70.

Układ współrzędnych = Unity: x wschód, y góra, z północ, metry. Trójkąty: formalny iloczyn (b-a)x(c-a) wskazuje na
zewnątrz (w Unity to strona przednia). UV0 w metrach (tekstury Poly Haven mają rzeczywisty rozmiar kafla), UV1:
okna — współrzędne pokoju za szybą (część całkowita = numer pokoju, ułamek = położenie w pokoju), tynk — współrzędne
zacieków (u wzdłuż ściany, v wysokość). Styczna wzdłuż elewacji w prawo patrząc na ścianę, w = -1 (binormalna = góra).

Zasada anty-migotania (2.11): nic nie leży w tej samej płaszczyźnie na innej powierzchni — opaski okien są wycięte
ze ściany (ten sam poziom, bez nakładania), elementy na ścianie mają tył schowany albo ≥ 2 cm odstępu.
"""
import gzip
import json
import math
import struct

import numpy as np

UP = np.array([0., 1., 0.])


def nrm(v):
    v = np.asarray(v, float)
    return v / max(1e-9, float(np.linalg.norm(v)))


class Siatka:
    """Siatki pogrupowane: (grupa, materiał) -> wierzchołki."""

    def __init__(self):
        self.cz = {}

    def _c(self, g, m):
        k = (g, m)
        if k not in self.cz:
            self.cz[k] = {"v": [], "n": [], "t": [], "uv": [], "uv1": [], "i": []}
        return self.cz[k]

    # ---------------- podstawy ----------------
    def tri_list(self, g, m, pts, n, t, uv, uv1, idx, w=-1.0):
        c = self._c(g, m); b = len(c["v"])
        c["v"] += [tuple(map(float, p)) for p in pts]
        c["n"] += [tuple(map(float, n))] * len(pts) if np.ndim(n) == 1 else [tuple(map(float, x)) for x in n]
        c["t"] += [tuple(map(float, t)) + (w,)] * len(pts) if np.ndim(t) == 1 else [tuple(map(float, x)) + (w,) for x in t]
        c["uv"] += [tuple(map(float, x)) for x in uv]
        c["uv1"] += [tuple(map(float, x)) for x in uv1] if uv1 is not None else [(0.0, 0.0)] * len(pts)
        c["i"] += [b + k for k in idx]

    def quad(self, g, m, a, b, c, d, uv_o=None, uv1=None, t=None, kier=None, obrot=False):
        """Czworokąt a-b-c-d (dookoła). kier = kierunek, w który ma patrzeć strona przednia (gdy podany, kolejność jest
        poprawiana sama). Bez kier: iloczyn (b-a)x(d-a) na zewnątrz. UV w metrach rzutem na styczną i binormalną."""
        a, b, c, d = (np.asarray(p, float) for p in (a, b, c, d))
        if kier is not None and np.dot(np.cross(b - a, d - a), kier) < 0:
            b, d = d, b
            if uv1 is not None: uv1 = [uv1[0], uv1[3], uv1[2], uv1[1]]
        n = nrm(np.cross(b - a, d - a))
        if t is None:
            t = np.cross(n, UP) if abs(n[1]) < .7 else np.array([1., 0., 0.]) * (1 if n[1] > 0 else -1)
            t = nrm(t - n * np.dot(t, n))
        bt = nrm(np.cross(t, n)) if abs(n[1]) < .7 else nrm(np.cross(n, t))
        if abs(n[1]) < .7: bt = UP.copy() if np.dot(np.cross(t, n), UP) >= 0 else -UP
        o = a if uv_o is None else np.asarray(uv_o, float)
        uv = [(np.dot(p - o, t), np.dot(p - o, bt)) for p in (a, b, c, d)]
        if obrot:       # deski pionowo: u wzdłuż binormalnej; styczna = kierunek u, znak w z konwencji Unity (B = N x T * w)
            uv = [(y, x) for (x, y) in uv]; t, bt = bt, t
            w = 1.0 if np.dot(np.cross(n, t), bt) > 0 else -1.0
            self.tri_list(g, m, [a, b, c, d], n, t, uv, uv1, [0, 1, 2, 0, 2, 3], w=w)
            return n
        self.tri_list(g, m, [a, b, c, d], n, t, uv, uv1, [0, 1, 2, 0, 2, 3])
        return n

    def box(self, g, m, o, ex, ey, ez, sx, sy, sz, bez=()):
        """Prostopadłościan: narożnik o, osie ex/ey/ez (prawoskrętne w sensie formalnym), wymiary. bez = ściany pominięte
        ('-x','+x','-y','+y','-z','+z')."""
        o = np.asarray(o, float); ex, ey, ez = nrm(ex) * sx, nrm(ey) * sy, nrm(ez) * sz
        P = lambda i, j, k: o + ex * i + ey * j + ez * k
        sc = {
            "-x": (P(0, 0, 0), P(0, 0, 1), P(0, 1, 1), P(0, 1, 0)),
            "+x": (P(1, 0, 0), P(1, 1, 0), P(1, 1, 1), P(1, 0, 1)),
            "-y": (P(0, 0, 0), P(1, 0, 0), P(1, 0, 1), P(0, 0, 1)),
            "+y": (P(0, 1, 0), P(0, 1, 1), P(1, 1, 1), P(1, 1, 0)),
            "-z": (P(0, 0, 0), P(0, 1, 0), P(1, 1, 0), P(1, 0, 0)),
            "+z": (P(0, 0, 1), P(1, 0, 1), P(1, 1, 1), P(0, 1, 1)),
        }
        flip = np.dot(np.cross(ex, ey), ez) < 0
        for k, (a, b, c, d) in sc.items():
            if k in bez: continue
            if flip: self.quad(g, m, a, d, c, b)
            else: self.quad(g, m, a, b, c, d)

    def rura(self, g, m, a, b, r, seg=8, kapsle=False):
        a, b = np.asarray(a, float), np.asarray(b, float); ax = nrm(b - a)
        p = nrm(np.cross(ax, UP if abs(ax[1]) < .9 else np.array([1., 0, 0]))); q = np.cross(ax, p)
        L = float(np.linalg.norm(b - a))
        for k in range(seg):
            f0, f1 = 2 * math.pi * k / seg, 2 * math.pi * (k + 1) / seg
            d0 = p * math.cos(f0) + q * math.sin(f0); d1 = p * math.cos(f1) + q * math.sin(f1)
            pts = [a + d0 * r, a + d1 * r, b + d1 * r, b + d0 * r]
            ns = [d0, d1, d1, d0]
            uv = [(k / seg * 2 * math.pi * r, 0), ((k + 1) / seg * 2 * math.pi * r, 0), ((k + 1) / seg * 2 * math.pi * r, L), (k / seg * 2 * math.pi * r, L)]
            # (b-a)x(d-a) dla a=a+d0r, b=a+d1r, d=b+d0r: (d1-d0)r x ax  — na zewnątrz przy tej kolejności osi
            idx = [0, 3, 2, 0, 2, 1] if np.dot(np.cross((d1 - d0), ax), d0 + d1) < 0 else [0, 1, 2, 0, 2, 3]
            self.tri_list(g, m, pts, ns, ax, uv, None, idx)

    # ---------------- ściana z otworami ----------------
    def sciana(self, g, m, o, t, L, H, otwory=(), uv0=(0, 0), uv1_skala=None, uv1_o=(0, 0)):
        """Płaska ściana: narożnik dolny lewy o (patrząc na ścianę), styczna t (w prawo), wysokość H w górę, normalna
        n = t x góra... liczona tak, by ściana patrzyła na widza. Otwory (u0,u1,v0,v1) w metrach od o. Komórki siatki
        podziału bez otworów są łączone w rzędy. uv1_skala: (Lc, Hc) dla współrzędnych zacieków."""
        o = np.asarray(o, float); t = nrm(t); up = UP
        us = sorted({0.0, L} | {float(x) for h in otwory for x in h[:2] if 0 < x < L})
        vs = sorted({0.0, H} | {float(x) for h in otwory for x in h[2:] if 0 < x < H})

        def w_otworze(u, v):
            return any(h[0] <= u <= h[1] and h[2] <= v <= h[3] for h in otwory)
        for j in range(len(vs) - 1):
            v0, v1 = vs[j], vs[j + 1]; vm = (v0 + v1) / 2
            i = 0
            while i < len(us) - 1:
                if w_otworze((us[i] + us[i + 1]) / 2, vm): i += 1; continue
                k = i
                while k + 1 < len(us) - 1 and not w_otworze((us[k + 1] + us[k + 2]) / 2, vm): k += 1
                u0, u1 = us[i], us[k + 1]
                P = lambda u, v: o + t * u + up * v
                uv1 = None
                if uv1_skala:
                    uv1 = [((uv1_o[0] + u) / uv1_skala[0], (uv1_o[1] + v) / uv1_skala[1]) for u, v in ((u0, v0), (u1, v0), (u1, v1), (u0, v1))]
                self.quad(g, m, P(u0, v0), P(u1, v0), P(u1, v1), P(u0, v1), uv_o=o - t * uv0[0] - up * uv0[1], uv1=uv1, t=t, kier=np.cross(up, t))
                i = k + 1

    def dane(self):
        return self.cz


# ---------------- eksport ----------------
def zapisz_bin(siatka, materialy, sciezka, kotwica, opis):
    """Format BYD2 (gzip): nagłówek JSON (materiały, kotwica, opis), potem siatki: grupa, materiał,
    v(3f) n(3f) t(4f) uv(2f) uv1(2f), indeksy uint32."""
    out = bytearray()
    hdr = json.dumps({"materialy": materialy, "kotwica": kotwica, "opis": opis}, ensure_ascii=False).encode("utf-8")
    out += b"BYD2" + struct.pack("<i", len(hdr)) + hdr
    cz = siatka.dane()
    out += struct.pack("<i", len(cz))
    for (g, m), c in sorted(cz.items()):
        for s in (g, m):
            b = s.encode("utf-8"); out += struct.pack("<i", len(b)) + b
        nv, ni = len(c["v"]), len(c["i"])
        out += struct.pack("<ii", nv, ni)
        a = np.hstack([np.array(c["v"], np.float32), np.array(c["n"], np.float32), np.array(c["t"], np.float32),
                       np.array(c["uv"], np.float32), np.array(c["uv1"], np.float32)]).astype("<f4")
        out += a.tobytes() + np.array(c["i"], "<u4").tobytes()
    with gzip.open(sciezka, "wb", compresslevel=9) as f:
        f.write(bytes(out))


def scal_wg_materialu(siatka):
    """Jedna siatka na materiał (podgląd całego osiedla: mniej wywołań rysowania)."""
    out = Siatka(); licz = {}
    for (g, m), c in siatka.dane().items():
        k = licz.get(m, 0)
        if len(out._c("razem%d" % k, m)["v"]) + len(c["v"]) > 60000:
            k += 1; licz[m] = k
        d = out._c("razem%d" % k, m); b = len(d["v"])
        for f in ("v", "n", "t", "uv", "uv1"): d[f] += c[f]
        d["i"] += [b + i for i in c["i"]]
    return out


def zapisz_glb(siatka, sciezka):
    """Podgląd (three.js): z -> -z (glTF prawoskrętny, północ = -z), kolejność trójkątów odwrócona; każda (grupa,
    materiał) to osobny węzeł o nazwie 'grupa|materiał'; styczne w atrybucie TANGENT (w odwrócone przez odbicie)."""
    cz = siatka.dane(); bufs = bytearray(); views = []; acc = []; meshes = []; nodes = []

    def add(arr, target, typ, comp=5126, minmax=False):
        nonlocal bufs
        while len(bufs) % 4: bufs += b"\0"
        off = len(bufs); bufs += arr.tobytes(); views.append({"buffer": 0, "byteOffset": off, "byteLength": arr.nbytes, **({"target": target} if target else {})})
        a = {"bufferView": len(views) - 1, "componentType": comp, "count": int(arr.shape[0]), "type": typ}
        if minmax: a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        acc.append(a); return len(acc) - 1
    M = np.array([1, 1, -1], np.float32)
    for (g, m), c in sorted(cz.items()):
        v = np.array(c["v"], np.float32) * M; n = np.array(c["n"], np.float32) * M
        t = np.array(c["t"], np.float32); t[:, :3] *= M; t[:, 3] *= -1
        uv = np.array(c["uv"], np.float32); uv1 = np.array(c["uv1"], np.float32)
        i = np.array(c["i"], np.uint32).reshape(-1, 3)[:, [0, 2, 1]].reshape(-1)
        attrs = {"POSITION": add(v, 34962, "VEC3", minmax=True), "NORMAL": add(n, 34962, "VEC3"), "TANGENT": add(t, 34962, "VEC4"),
                 "TEXCOORD_0": add(uv, 34962, "VEC2"), "TEXCOORD_1": add(uv1, 34962, "VEC2")}
        ia = add(i, 34963, "SCALAR", comp=5125)
        meshes.append({"name": g + "|" + m, "primitives": [{"attributes": attrs, "indices": ia}]})
        nodes.append({"name": g + "|" + m, "mesh": len(meshes) - 1})
    while len(bufs) % 4: bufs += b"\0"
    gl = {"asset": {"version": "2.0"}, "scenes": [{"nodes": list(range(len(nodes)))}], "scene": 0, "nodes": nodes, "meshes": meshes,
          "accessors": acc, "bufferViews": views, "buffers": [{"byteLength": len(bufs)}]}
    js = json.dumps(gl).encode(); js += b" " * ((4 - len(js) % 4) % 4)
    with open(sciezka, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(bufs)))
        f.write(struct.pack("<II", len(js), 0x4E4F534A) + js)
        f.write(struct.pack("<II", len(bufs), 0x004E4942) + bytes(bufs))


def trojkaty(siatka):
    return sum(len(c["i"]) // 3 for c in siatka.dane().values())
