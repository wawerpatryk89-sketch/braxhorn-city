#!/usr/bin/env python3
"""MIASTO R1 — Stary Rynek / Mostowa / Focha / Opera (Bydgoszcz): mapa OpenStreetMap (ODbL, © autorzy OpenStreetMap)
przeliczona na metry od kotwicy (środek kościoła Wniebowzięcia NMP, Gdańska 2). W grze: PunktZerowy (dane/rynek_rzut.json,
gra_a) + metry × K — układ kolegi przesunięty o (+42; +763) j. wg przyjętego planu połączeń (właściciel 23.11, 17:05 / 19:56).
    pobierz_osm.py            (przelicza zapisany dane/rynek_osm_map.xml)
"""
import json
import xml.etree.ElementTree as ET
from pathlib import Path

DANE = Path(__file__).resolve().parent / "dane"


def przelicz():
    R = json.loads((DANE / "rynek_rzut.json").read_text())
    la, lo, MLAT, MLON = R["lat_a"], R["lon_a"], R["MLAT"], R["MLON"]
    root = ET.fromstring((DANE / "rynek_osm_map.xml").read_bytes())
    nodes, ntag = {}, {}
    for e in root.iter("node"):
        nodes[e.get("id")] = (round((float(e.get("lon")) - lo) * MLON, 2), round((float(e.get("lat")) - la) * MLAT, 2))
        t = {g.get("k"): g.get("v") for g in e.iter("tag")}
        if t: ntag[e.get("id")] = t
    ways = {}
    for w in root.iter("way"):
        ways[w.get("id")] = ([n.get("ref") for n in w.iter("nd")], {g.get("k"): g.get("v") for g in w.iter("tag")})
    bud, ulice, tereny, punkty = [], [], [], []
    for i, (refs, t) in ways.items():
        p = [nodes[r] for r in refs if r in nodes]
        if len(p) < 2: continue
        if ("building" in t or "building:part" in t) and len(p) >= 4:
            bud.append({"osm": "w" + i, "czesc": "building:part" in t, "nr": t.get("addr:housenumber"), "ulica": t.get("addr:street"),
                        "rodzaj": t.get("building") or t.get("building:part"), "pietra": t.get("building:levels"),
                        "pietra_dach": t.get("roof:levels"), "wysokosc": t.get("height"), "min_wys": t.get("min_height"),
                        "dach": t.get("roof:shape"), "kolor": t.get("building:colour"), "kolor_dachu": t.get("roof:colour"),
                        "material": t.get("building:material"), "nazwa": t.get("name"), "zabytek": t.get("historic") or t.get("heritage"),
                        "uzytek": t.get("amenity") or t.get("shop") or t.get("tourism"), "obrys": p})
        elif "highway" in t:
            ulice.append({"osm": "w" + i, "klasa": t["highway"], "nazwa": t.get("name"), "most": t.get("bridge"), "warstwa": t.get("layer"),
                          "nawierzchnia": t.get("surface"), "pasy": t.get("lanes"), "jednokier": t.get("oneway"), "obszar": t.get("area") == "yes",
                          "tory": None, "linia": p})
        elif t.get("railway") in ("tram", "rail"):
            ulice.append({"osm": "w" + i, "klasa": "tory_" + t["railway"], "nazwa": t.get("name"), "most": t.get("bridge"), "linia": p})
        else:
            k = (t.get("waterway") and ("rzeka_" + t["waterway"])) or (t.get("natural") == "water" and "woda") or \
                (t.get("man_made") == "bridge" and "most") or t.get("leisure") or t.get("landuse") or t.get("natural") or \
                t.get("place") or (("ogrodzenie:" + t["barrier"]) if "barrier" in t else None) or t.get("amenity") or t.get("man_made")
            if k: tereny.append({"osm": "w" + i, "rodzaj": k, "nazwa": t.get("name"), "nawierzchnia": t.get("surface"),
                                 "zamkniety": p[0] == p[-1], "linia": p})
    for rel in root.iter("relation"):          # woda i place jako multipoligony (Brda jest relacją)
        t = {g.get("k"): g.get("v") for g in rel.iter("tag")}
        if t.get("type") != "multipolygon": continue
        k = (t.get("natural") == "water" and "woda") or t.get("place") or t.get("landuse") or t.get("leisure") or (("building" in t) and "budynek_rel")
        if not k: continue
        for m in rel.iter("member"):
            if m.get("type") == "way" and m.get("ref") in ways and m.get("role") in ("outer", "inner"):
                p = [nodes[r] for r in ways[m.get("ref")][0] if r in nodes]
                if len(p) >= 2: tereny.append({"osm": "r" + rel.get("id") + "/w" + m.get("ref"), "rodzaj": k, "rola": m.get("role"),
                                               "nazwa": t.get("name"), "zamkniety": p[0] == p[-1], "linia": p, "rel": rel.get("id")})
    for i, t in ntag.items():
        if t.get("natural") == "tree" or t.get("highway") in ("street_lamp",) or t.get("historic") or t.get("tourism") == "artwork" \
                or t.get("amenity") in ("bench", "fountain", "waste_basket") or t.get("barrier") == "bollard":
            punkty.append({"id": i, "tagi": t, "xz": nodes[i]})
    out = {"zrodlo": "OpenStreetMap (ODbL) — © autorzy OpenStreetMap; api.openstreetmap.org, pobrane 23.11.2026",
           "jednostka": "metr; x = wschód, z = północ; (0,0) = kotwica z rynek_rzut.json", "budynki": bud, "ulice": ulice, "tereny": tereny, "punkty": punkty}
    (DANE / "rynek_osm.json").write_text(json.dumps(out, ensure_ascii=False), encoding="utf-8")
    return out


if __name__ == "__main__":
    o = przelicz()
    print("budynki", len(o["budynki"]), "ulice", len(o["ulice"]), "tereny", len(o["tereny"]), "punkty", len(o["punkty"]))
