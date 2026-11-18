"""Czytnik binarnego FBX z TABLICAMI — wierzchołki, wagi skinningu, UV, połączenia.

Rozszerzenie fbx_czytnik.py: tam były tylko nazwy węzłów, tu wchodzą dane liczbowe.
Potrzebne, żeby ZMIERZYĆ (a nie zgadnąć), która kość trzyma dany fragment siatki —
na przykład czy krawat kapitana jest przypisany do klatki piersiowej, czy do szyi.

Bez Blendera, bez Unity, bez GPU.
"""
import struct
import zlib

import numpy as np

ROZMIAR = {"Y": 2, "C": 1, "I": 4, "F": 4, "D": 8, "L": 8}
SKALARNY = {"Y": "<h", "C": "<b", "I": "<i", "F": "<f", "D": "<d", "L": "<q"}
TABLICA = {"f": ("f4", 4), "d": ("f8", 8), "l": ("i8", 8), "i": ("i4", 4), "b": ("i1", 1)}


class Wezel:
    __slots__ = ("nazwa", "wartosci", "dzieci")

    def __init__(self, nazwa):
        self.nazwa = nazwa
        self.wartosci = []
        self.dzieci = []

    def znajdz(self, nazwa):
        return [d for d in self.dzieci if d.nazwa == nazwa]

    def pierwszy(self, nazwa):
        for d in self.dzieci:
            if d.nazwa == nazwa:
                return d
        return None

    def __repr__(self):
        return "<" + self.nazwa + " " + str(len(self.dzieci)) + " dzieci>"


class Fbx:
    def __init__(self, sciezka):
        self.dane = open(sciezka, "rb").read()
        if not self.dane.startswith(b"Kaydara FBX Binary"):
            raise ValueError("To nie jest binarny FBX: " + str(sciezka))
        self.wersja = struct.unpack_from("<I", self.dane, 23)[0]
        self.szeroki = self.wersja >= 7500
        self.korzen = Wezel("")
        o = 27
        while True:
            r = self._wezel(o, self.korzen)
            if r is None:
                break
            o = r

    def _prop(self, o):
        d = self.dane
        t = chr(d[o])
        o += 1
        if t in ROZMIAR:
            return struct.unpack_from(SKALARNY[t], d, o)[0], o + ROZMIAR[t]
        if t in "SR":
            n = struct.unpack_from("<I", d, o)[0]
            v = d[o + 4:o + 4 + n]
            return (v.decode("utf-8", "replace") if t == "S" else v), o + 4 + n
        if t in TABLICA:
            ile, kod, dlug = struct.unpack_from("<III", d, o)
            o += 12
            sur = d[o:o + dlug]
            if kod == 1:
                sur = zlib.decompress(sur)
            typ, _ = TABLICA[t]
            return np.frombuffer(sur, dtype=typ, count=ile), o + dlug
        raise ValueError("Nieznany typ właściwości: " + t)

    def _wezel(self, o, rodzic):
        d = self.dane
        if self.szeroki:
            koniec, ile, dlug = struct.unpack_from("<QQQ", d, o)
            o += 24
        else:
            koniec, ile, dlug = struct.unpack_from("<III", d, o)
            o += 12
        dl = d[o]
        o += 1
        nazwa = d[o:o + dl].decode("latin1")
        o += dl
        if koniec == 0:
            return None
        w = Wezel(nazwa)
        po = o
        for _ in range(ile):
            v, po = self._prop(po)
            w.wartosci.append(v)
        rodzic.dzieci.append(w)
        o += dlug
        while o < koniec:
            r = self._wezel(o, w)
            if r is None:
                o += 25 if self.szeroki else 13
                break
            o = r
        return koniec

    # ---- wyższy poziom -------------------------------------------------

    def obiekty(self):
        """{uid: wezel} dla wszystkiego pod Objects."""
        ob = self.korzen.pierwszy("Objects")
        out = {}
        if ob is None:
            return out
        for d in ob.dzieci:
            if d.wartosci and isinstance(d.wartosci[0], int):
                out[d.wartosci[0]] = d
        return out

    def nazwa_obiektu(self, w):
        for v in w.wartosci[1:]:
            if isinstance(v, str):
                return v.split("\x00")[0]
        return ""

    def polaczenia(self):
        """[(dziecko_uid, rodzic_uid, etykieta)] z sekcji Connections."""
        c = self.korzen.pierwszy("Connections")
        out = []
        if c is None:
            return out
        for d in c.dzieci:
            v = d.wartosci
            if len(v) >= 3 and isinstance(v[1], int) and isinstance(v[2], int):
                out.append((v[1], v[2], v[3] if len(v) > 3 else ""))
        return out


def siatka(w):
    """Wierzchołki (N,3) i indeksy wielokątów z węzła Geometry."""
    v = w.pierwszy("Vertices")
    i = w.pierwszy("PolygonVertexIndex")
    wierz = np.asarray(v.wartosci[0], dtype="f8").reshape(-1, 3) if v else None
    ind = np.asarray(i.wartosci[0], dtype="i8") if i else None
    return wierz, ind


def trojkaty(ind):
    """Wielokąty FBX (ostatni indeks znegowany) -> trójkąty."""
    out = []
    biez = []
    for x in ind:
        if x < 0:
            biez.append(int(~x))
            for k in range(1, len(biez) - 1):
                out.append((biez[0], biez[k], biez[k + 1]))
            biez = []
        else:
            biez.append(int(x))
    return np.array(out, dtype="i8")
