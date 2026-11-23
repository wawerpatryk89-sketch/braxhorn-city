"""OSM (.osm XML) -> układ w metrach lokalnych: budynki, ulice, zieleń, woda. Zapis JSON."""
import json, math, sys
import xml.etree.ElementTree as ET

plik, lat0, lon0, wyj = sys.argv[1], float(sys.argv[2]), float(sys.argv[3]), sys.argv[4]
MLAT = 111132.92 - 559.82 * math.cos(2 * math.radians(lat0)) + 1.175 * math.cos(4 * math.radians(lat0))
MLON = 111412.84 * math.cos(math.radians(lat0)) - 93.5 * math.cos(3 * math.radians(lat0))

def xy(lat, lon):
    return (round((lon - lon0) * MLON, 2), round((lat - lat0) * MLAT, 2))

root = ET.parse(plik).getroot()
nodes, ways, rels, punkty = {}, {}, [], []
for e in root:
    if e.tag == "node":
        nodes[e.get("id")] = xy(float(e.get("lat")), float(e.get("lon")))
        t = {x.get("k"): x.get("v") for x in e.findall("tag")}
        if t:
            punkty.append((e.get("id"), t))
    elif e.tag == "way":
        ways[e.get("id")] = ([n.get("ref") for n in e.findall("nd")], {x.get("k"): x.get("v") for x in e.findall("tag")})
    elif e.tag == "relation":
        rels.append((e.get("id"), [(m.get("type"), m.get("ref"), m.get("role")) for m in e.findall("member")],
                     {x.get("k"): x.get("v") for x in e.findall("tag")}))

def geom(refs):
    return [nodes[r] for r in refs if r in nodes]

def pole(p):
    return abs(sum(p[i][0] * p[i + 1][1] - p[i + 1][0] * p[i][1] for i in range(len(p) - 1))) / 2 if len(p) > 3 else 0.0

def srodek(p):
    q = p[:-1] if len(p) > 1 and p[0] == p[-1] else p
    return (round(sum(a for a, _ in q) / len(q), 1), round(sum(b for _, b in q) / len(q), 1))

def prostokat(p):
    """Najmniejszy prostokąt opisany: (dłuższy bok, krótszy bok, kąt dłuższego boku w stopniach od osi X)."""
    q = p[:-1] if p[0] == p[-1] else p
    naj = None
    for i in range(len(q)):
        ax, ay = q[i]; bx, by = q[(i + 1) % len(q)]
        d = math.hypot(bx - ax, by - ay)
        if d < 0.5: continue
        ux, uy = (bx - ax) / d, (by - ay) / d
        us = [x * ux + y * uy for x, y in q]; vs = [-x * uy + y * ux for x, y in q]
        w, h = max(us) - min(us), max(vs) - min(vs)
        if naj is None or w * h < naj[0]:
            kat = math.degrees(math.atan2(uy, ux)) if w >= h else math.degrees(math.atan2(ux, -uy))
            naj = (w * h, max(w, h), min(w, h), kat % 180)
    return (round(naj[1], 1), round(naj[2], 1), round(naj[3], 1)) if naj else (0, 0, 0)

bud, ulice, tereny, drzewa = [], [], [], []
def budynek(i, p, t):
    if len(p) < 4: return
    dl, sz, kat = prostokat(p)
    bud.append({"osm": i, "nr": t.get("addr:housenumber"), "ulica": t.get("addr:street"), "rodzaj": t.get("building"),
                "pietra": t.get("building:levels"), "wysokosc": t.get("height"), "dach": t.get("roof:shape"),
                "nazwa": t.get("name"), "uzytek": t.get("amenity") or t.get("shop") or t.get("tourism"),
                "pole_m2": round(pole(p)), "dlugosc_m": dl, "szerokosc_m": sz, "kat": kat, "srodek": srodek(p), "obrys": p})

for i, (refs, t) in ways.items():
    p = geom(refs)
    if "building" in t or "building:part" in t and False:
        budynek("w" + i, p, t)
    elif "highway" in t:
        ulice.append({"osm": "w" + i, "klasa": t["highway"], "nazwa": t.get("name"), "nawierzchnia": t.get("surface"),
                      "pasy": t.get("lanes"), "szerokosc": t.get("width"), "jednokier": t.get("oneway"),
                      "most": t.get("bridge"), "usluga": t.get("service"), "linia": p})
    elif t.get("railway") in ("tram", "rail"):
        ulice.append({"osm": "w" + i, "klasa": "tory_" + t["railway"], "nazwa": t.get("name"), "linia": p})
    else:
        k = (t.get("amenity") == "parking" and "parking") or t.get("leisure") or t.get("landuse") or t.get("natural") or t.get("waterway") or t.get("barrier") and "ogrodzenie:" + t["barrier"]
        if k and len(p) >= 2:
            tereny.append({"osm": "w" + i, "rodzaj": k, "nazwa": t.get("name"), "zamkniety": p[0] == p[-1], "linia": p})
def pierscienie(refy_drog):
    """Skleja drogi-członków relacji w pierścienie. Zwraca (zamknięte, otwarte) jako listy list identyfikatorów węzłów."""
    kawalki = [list(r) for r in refy_drog if len(r) >= 2]; zamk, otw = [], []
    while kawalki:
        r = kawalki.pop()
        zmiana = True
        while r[0] != r[-1] and zmiana:
            zmiana = False
            for j, q in enumerate(kawalki):
                if q[0] == r[-1]: r += q[1:]
                elif q[-1] == r[-1]: r += q[-2::-1]
                elif q[-1] == r[0]: r = q[:-1] + r
                elif q[0] == r[0]: r = q[:0:-1] + r
                else: continue
                kawalki.pop(j); zmiana = True; break
        (zamk if r[0] == r[-1] and len(r) >= 4 else otw).append(r)
    return zamk, otw

for i, mem, t in rels:
    if t.get("type") == "multipolygon":
        zamk, otw = pierscienie([ways[ref][0] for typ, ref, rola in mem if typ == "way" and rola in ("outer", "") and ref in ways])
        woda = t.get("natural") == "water" or t.get("waterway")
        for r in zamk:
            p = geom(r)
            if "building" in t: budynek("r" + i, p, t)
            elif woda: tereny.append({"osm": "r" + i, "rodzaj": "water", "nazwa": t.get("name"), "zamkniety": True, "linia": p})
        if woda:
            for r in otw: tereny.append({"osm": "r" + i, "rodzaj": "brzeg_wody", "nazwa": t.get("name"), "zamkniety": False, "linia": geom(r)})
for i, t in punkty:
    if t.get("natural") == "tree": drzewa.append(nodes[i])

json.dump({"zrodlo": "OpenStreetMap (ODbL) — © autorzy OpenStreetMap", "srodek_lat_lon": [lat0, lon0],
           "jednostka": "metr; x=wschód, y=północ", "budynki": bud, "ulice": ulice, "tereny": tereny, "drzewa": drzewa},
          open(wyj, "w", encoding="utf-8"), ensure_ascii=False)
print(plik, "budynki", len(bud), "ulice", len(ulice), "tereny", len(tereny), "drzewa", len(drzewa))
