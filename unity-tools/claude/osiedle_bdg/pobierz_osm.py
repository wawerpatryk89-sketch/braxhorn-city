#!/usr/bin/env python3
"""MIASTO R1 — osiedle bydgoskie: pobranie mapy OpenStreetMap (ODbL, © autorzy OpenStreetMap) dla całego wycinka osiedla
z główną ulicą do ronda Skrzetuskiego i szpitalem (właściciel 23.11, 16:22–16:23) i zapis w metrach układu osiedla.

Układ: x wschód, z północ, metry; punkt (0,0) = lat0/lon0 z dane/osiedle_rzut.json (ten sam co wycinek kolegi —
dopasowany do obrysu 32a, więc wszystkie dotychczasowe położenia zostają). Wynik: dane/osiedle_osm_map.xml (surowy)
i dane/osiedle_osm.json (budynki, ulice, tereny jak w osm_uklad.py kolegi: obrys / linia w metrach).

    pobierz_osm.py [x0 x1 z0 z1]      (domyślnie −330 250 −210 310)
"""
import json
import math
import sys
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

DANE = Path(__file__).resolve().parent / "dane"


def pobierz(x0, x1, z0, z1):
    R = json.loads((DANE / "osiedle_rzut.json").read_text())
    lat0, lon0, MLAT, MLON = R["lat0"], R["lon0"], R["MLAT"], R["MLON"]
    bbox = "%.7f,%.7f,%.7f,%.7f" % (lon0 + x0 / MLON, lat0 + z0 / MLAT, lon0 + x1 / MLON, lat0 + z1 / MLAT)
    x = urllib.request.urlopen("https://api.openstreetmap.org/api/0.6/map?bbox=" + bbox, timeout=120).read()
    (DANE / "osiedle_osm_map.xml").write_bytes(x)
    return x, (lat0, lon0, MLAT, MLON)


def przelicz(xml, rz):
    lat0, lon0, MLAT, MLON = rz
    root = ET.fromstring(xml)
    nodes, ways = {}, {}
    for e in root:
        if e.tag == "node":
            nodes[e.get("id")] = (round((float(e.get("lon")) - lon0) * MLON, 2), round((float(e.get("lat")) - lat0) * MLAT, 2))
        elif e.tag == "way":
            ways[e.get("id")] = ([n.get("ref") for n in e.findall("nd")], {t.get("k"): t.get("v") for t in e.findall("tag")})
    bud, ulice, tereny = [], [], []
    for i, (refs, t) in ways.items():
        p = [nodes[r] for r in refs if r in nodes]
        if len(p) < 2: continue
        if "building" in t and len(p) >= 4:
            bud.append({"osm": "w" + i, "nr": t.get("addr:housenumber"), "ulica": t.get("addr:street"), "rodzaj": t.get("building"),
                        "pietra": t.get("building:levels"), "wysokosc": t.get("height"), "dach": t.get("roof:shape"),
                        "nazwa": t.get("name"), "uzytek": t.get("amenity") or t.get("shop"), "obrys": p})
        elif "highway" in t:
            ulice.append({"osm": "w" + i, "klasa": t["highway"], "nazwa": t.get("name"), "pasy": t.get("lanes"),
                          "jednokier": t.get("oneway"), "rondo": t.get("junction") == "roundabout", "linia": p})
        elif t.get("railway") in ("tram", "rail"):
            ulice.append({"osm": "w" + i, "klasa": "tory_" + t["railway"], "nazwa": t.get("name"), "linia": p})
        else:
            k = ("parking" if t.get("amenity") == "parking" else t.get("leisure") or t.get("landuse") or t.get("natural")
                 or (("ogrodzenie:" + t["barrier"]) if "barrier" in t else None) or t.get("amenity"))
            if k: tereny.append({"osm": "w" + i, "rodzaj": k, "nazwa": t.get("name"), "zamkniety": p[0] == p[-1], "linia": p})
    out = {"zrodlo": "OpenStreetMap (ODbL) — © autorzy OpenStreetMap; api.openstreetmap.org, pobrane przez pobierz_osm.py",
           "jednostka": "metr; x = wschód, z = północ; (0,0) = lat0/lon0 z osiedle_rzut.json",
           "budynki": bud, "ulice": ulice, "tereny": tereny}
    (DANE / "osiedle_osm.json").write_text(json.dumps(out, ensure_ascii=False), encoding="utf-8")
    return out


if __name__ == "__main__":
    a = [float(v) for v in sys.argv[1:5]] if len(sys.argv) >= 5 else [-330, 250, -210, 310]
    xml, rz = pobierz(*a)
    o = przelicz(xml, rz)
    print("budynki", len(o["budynki"]), "ulice", len(o["ulice"]), "tereny", len(o["tereny"]))
