"""Skąd biorą się kanciaki i deformacje — audyt całej obsady, liczbami.

Właściciel 17.11, 14:46: „ma być pod grę zrobione jak najlepiej, jak najmniej
deformacji, kanciaków".

Kanciaki i deformacje mają cztery różne przyczyny i każdą trzeba naprawiać gdzie
indziej — dlatego najpierw pomiar, a dopiero potem robota:

  1. ZA MAŁO TRÓJKĄTÓW — widać ścianki na zaokrągleniach. Tego nie da się naprawić
     w potoku, trzeba lepszego modelu ze źródła.
  2. ROZCIĘTE NORMALNE — ta sama pozycja ma kilka różnych normalnych, więc Unity
     cieniuje ją płaskimi plamami. To da się policzyć i wygładzić.
  3. ZA MAŁO KOŚCI NA WIERZCHOŁEK — przy jednej czy dwóch kościach łokieć i kolano
     załamują się jak rurka od wody. Standard to cztery.
  4. WAGI NIEZNORMALIZOWANE — suma wag inna niż 1 kurczy albo rozdyma siatkę
     w ruchu. To jest najczęstsza przyczyna „dziwnych" deformacji.

python3 -I audyt_postaci.py <katalog_z_postaciami>
"""
import sys
from collections import Counter
from pathlib import Path

import numpy as np

sys.path.insert(0, str(__import__("pathlib").Path(__file__).parent))
from fbx_tablice import Fbx, siatka, trojkaty  # noqa: E402


def normalne(geo):
    w = geo.pierwszy("LayerElementNormal")
    if w is None:
        return None, None, None
    n = w.pierwszy("Normals")
    if n is None:
        return None, None, None
    tab = np.asarray(n.wartosci[0], dtype="f8").reshape(-1, 3)
    tryb = w.pierwszy("MappingInformationType").wartosci[0]
    ref = w.pierwszy("ReferenceInformationType")
    ref = ref.wartosci[0] if ref else ""
    # Tripo zapisuje normalne jako IndexToDirect: tablica unikalnych + indeksy.
    # Bez rozwinięcia indeksów liczyłoby się nie to, co trzeba.
    idx = w.pierwszy("NormalsIndex")
    if idx is not None and "IndexToDirect" in ref:
        tab = tab[np.asarray(idx.wartosci[0], dtype="i8")]
    return tab, tryb, ref


def zbadaj(sciezka):
    f = Fbx(str(sciezka))
    ob = f.obiekty()
    geos = [w for w in ob.values() if w.nazwa == "Geometry"]
    if not geos:
        return None
    def ile_wierz(g):
        v = siatka(g)[0]
        return 0 if v is None else len(v)

    geo = max(geos, key=ile_wierz)
    wierz, ind = siatka(geo)
    tri = trojkaty(ind)
    n = len(wierz)

    # --- 2. rozcięte normalne -------------------------------------------
    plaskie = np.where(ind < 0, ~ind, ind).astype("i8")
    nrm, tryb, _ = normalne(geo)
    rozciete = kat_sredni = None
    if nrm is not None and tryb.startswith("ByPolygonVertex") and len(nrm) >= len(plaskie):
        nrm = nrm[: len(plaskie)]
        dl = np.linalg.norm(nrm, axis=1)
        nrm = nrm / np.where(dl == 0, 1, dl)[:, None]
        suma = np.zeros((n, 3))
        ile = np.zeros(n)
        np.add.at(suma, plaskie, nrm)
        np.add.at(ile, plaskie, 1)
        ile[ile == 0] = 1
        srednia = suma / ile[:, None]
        dl2 = np.linalg.norm(srednia, axis=1)
        # wierzchołek "rozcięty": uśrednione normalne znacznie krótsze niż 1,
        # bo składowe pokazują w różne strony
        rozciete = float(np.mean(dl2[ile > 1] < .985)) if (ile > 1).any() else 0.0
        kosinus = np.clip((nrm * srednia[plaskie]).sum(axis=1)
                          / np.where(dl2[plaskie] == 0, 1, dl2[plaskie]), -1, 1)
        kat_sredni = float(np.degrees(np.arccos(kosinus)).mean())

    # --- 3 i 4. skinning -------------------------------------------------
    pol = f.polaczenia()
    kosci = {uid for uid, w in ob.items()
             if w.nazwa == "Model" and len(w.wartosci) > 2 and w.wartosci[2] == "LimbNode"}
    dzieci = {}
    for d, r, _ in pol:
        dzieci.setdefault(r, []).append(d)

    ile_kosci = np.zeros(n, dtype=int)
    suma_wag = np.zeros(n)
    klastry = 0
    for uid, w in ob.items():
        if w.nazwa != "Deformer" or len(w.wartosci) < 3 or w.wartosci[2] != "Cluster":
            continue
        if not any(d in kosci for d in dzieci.get(uid, [])):
            continue
        klastry += 1
        idx = w.pierwszy("Indexes")
        wag = w.pierwszy("Weights")
        if idx is None or wag is None:
            continue
        i = np.asarray(idx.wartosci[0], dtype="i8")
        v = np.asarray(wag.wartosci[0], dtype="f8")
        m = i < n
        i, v = i[m], v[: int(m.sum())]
        istotne = v > 1e-4
        np.add.at(ile_kosci, i[istotne], 1)
        np.add.at(suma_wag, i, v)

    return {
        "trojkaty": len(tri),
        "wierzcholki": n,
        "kosci": klastry,
        "rozciete": rozciete,
        "kat": kat_sredni,
        "wplywy": Counter(ile_kosci.tolist()),
        "suma_min": float(suma_wag.min()),
        "suma_max": float(suma_wag.max()),
        "nieznorm": float(np.mean(np.abs(suma_wag - 1) > .02)),
        "bez_wag": int((suma_wag < 1e-6).sum()),
    }


def main(katalog):
    pliki = sorted(Path(katalog).glob("*/*.fbx"))
    if not pliki:
        pliki = sorted(Path(katalog).glob("*.fbx"))
    print("%-24s %8s %6s %7s %7s %6s %7s %8s" %
          ("postać", "trójkąty", "kości", "rozcięte", "kąt", "1-2 k.", "4+ k.", "nieznorm"))
    zbior = []
    for p in pliki:
        try:
            w = zbadaj(p)
        except Exception as e:
            print("%-24s  BŁĄD %s" % (p.parent.name, e))
            continue
        if not w:
            continue
        nazwa = p.parent.name if p.parent.name != Path(katalog).name else p.stem
        wpl = w["wplywy"]
        razem = max(1, sum(wpl.values()))
        malo = sum(v for k, v in wpl.items() if k in (1, 2)) / razem
        duzo = sum(v for k, v in wpl.items() if k >= 4) / razem
        print("%-24s %8d %6d %7s %7s %5.1f%% %6.1f%% %7.1f%%"
              % (nazwa, w["trojkaty"], w["kosci"],
                 "%.1f%%" % (100 * w["rozciete"]) if w["rozciete"] is not None else "   —",
                 "%.1f°" % w["kat"] if w["kat"] is not None else "   —",
                 100 * malo, 100 * duzo, 100 * w["nieznorm"]))
        zbior.append((nazwa, w, malo, duzo))

    print("\nCO TO ZNACZY")
    print("  rozcięte  — ile wierzchołków ma kilka różnych normalnych; to są widoczne")
    print("              ścianki i ostre krawędzie cieniowania (kanciaki).")
    print("  kąt       — średnie odchylenie normalnej od uśrednionej; im więcej, tym")
    print("              ostrzejsze przejścia między ściankami.")
    print("  1-2 k.    — wierzchołki trzymane przez jedną albo dwie kości; to tam łamią")
    print("              się łokcie i kolana.")
    print("  nieznorm  — wierzchołki, których wagi nie sumują się do 1; w ruchu kurczą")
    print("              albo rozdymają siatkę.")
    if zbior:
        print("\nNAJGORSZE")
        for klucz, opis in (("rozciete", "rozcięte normalne"), ("nieznorm", "wagi nieznormalizowane")):
            lista = [(x[1][klucz] or 0, x[0]) for x in zbior]
            lista.sort(reverse=True)
            print("  %-26s %s" % (opis + ":",
                  ", ".join("%s %.0f%%" % (n, 100 * v) for v, n in lista[:4])))
        lista = sorted(((x[2], x[0]) for x in zbior), reverse=True)
        print("  %-26s %s" % ("za mało kości na wierzchołek:",
              ", ".join("%s %.0f%%" % (n, 100 * v) for v, n in lista[:4])))


if __name__ == "__main__":
    main(sys.argv[1])
