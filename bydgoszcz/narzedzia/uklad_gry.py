"""Wycinek mapy -> dane dla gry (jednostki gry = metry x 1.294) + rysunek propozycji miejsca w Mieście R1."""
import json, math, sys
import matplotlib; matplotlib.use("Agg")
import matplotlib.pyplot as plt

def pas(p, pol):
    """Wielokąt pasa o połowie szerokości `pol` wokół łamanej p (bez bibliotek)."""
    import math
    L, R = [], []
    for i, (x, y) in enumerate(p):
        ax, ay = p[max(i - 1, 0)]; bx, by = p[min(i + 1, len(p) - 1)]
        d = math.hypot(bx - ax, by - ay) or 1.0
        nx, ny = -(by - ay) / d, (bx - ax) / d
        L.append((x + nx * pol, y + ny * pol)); R.append((x - nx * pol, y - ny * pol))
    return L + R[::-1]
from matplotlib.patches import Polygon, Rectangle
K = 1.294
os_ = json.load(open("/home/claude/narzedzia/bartodzieje_uklad.json")); ry = json.load(open("/home/claude/narzedzia/rynek_uklad.json"))
def wytnij(d, x0, x1, y0, y1):
    w = lambda p: any(x0 <= x <= x1 and y0 <= y <= y1 for x, y in p)
    return {"budynki": [b for b in d["budynki"] if x0 <= b["srodek"][0] <= x1 and y0 <= b["srodek"][1] <= y1],
            "ulice": [u for u in d["ulice"] if w(u["linia"])], "tereny": [t for t in d["tereny"] if w(t["linia"])], "drzewa": [q for q in d.get("drzewa", []) if x0 <= q[0] <= x1 and y0 <= q[1] <= y1]}
A = wytnij(os_, -200, 190, -200, 190)          # osiedle: 390 x 390 m
B = wytnij(ry, -70, 215, -75, 470)             # stare miasto: 285 x 545 m
# kotwice w grze (propozycja po słowach właściciela 02:18: cała północna strona WZDŁUŻ mapy, aż po nowe wieżowce,
# bez wydłużania mapy): osiedle za lasem przy sklepie, stare miasto na zachód od niego, przy nowych dzielnicach.
KA = (round(100.0 - 190 * K, 1), round(190.0 + 200 * K, 1))      # (0,0) = blok 32a; wycinek x -405..100, z 190..695
KB = (round(-872.0 + 70 * K, 1), round(190.0 + 75 * K, 1))       # (0,0) = środek Starego Rynku; wycinek x -872..-503, z 190..895
def gra(p, k): return [(round(k[0] + x * K, 2), round(k[1] + y * K, 2)) for x, y in p]
def zapis(w, k, nazwa, opis):
    out = {"zrodlo": "OpenStreetMap (ODbL), © autorzy OpenStreetMap; pobrane 2026-11-23", "opis": opis, "skala_K": K,
           "jednostka": "jednostka gry (metr x 1.294); x = wschód, z = północ; punkt (0,0) wycinka w grze = " + str(k),
           "budynki": [{kk: b[kk] for kk in ("osm", "nr", "ulica", "rodzaj", "pietra", "nazwa", "uzytek", "pole_m2", "dlugosc_m", "szerokosc_m", "kat")} | {"obrys_xz": gra(b["obrys"], k)} for b in w["budynki"]],
           "ulice": [{kk: u.get(kk) for kk in ("osm", "klasa", "nazwa", "nawierzchnia", "pasy", "most")} | {"linia_xz": gra(u["linia"], k)} for u in w["ulice"]],
           "tereny": [{"rodzaj": t["rodzaj"], "nazwa": t.get("nazwa"), "zamkniety": t["zamkniety"], "linia_xz": gra(t["linia"], k)} for t in w["tereny"]],
           "drzewa_xz": gra(w["drzewa"], k)}
    json.dump(out, open(nazwa, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    print(nazwa, "budynki", len(out["budynki"]), "ulice", len(out["ulice"]), "tereny", len(out["tereny"]))
zapis(A, KA, sys.argv[1] + "/osiedle-sklodowskiej-32a.json", "Bartodzieje: Skłodowskiej-Curie 32a i okolica, 390 x 390 m")
zapis(B, KB, sys.argv[1] + "/stare-miasto-rynek-mostowa-gdanska.json", "Stary Rynek, Mostowa, most, Focha, początek Gdańskiej, 285 x 545 m")

fig, ax = plt.subplots(figsize=(10, 11.5), dpi=150); ax.set_facecolor("#eef2ee")
ax.add_patch(Rectangle((170, -500), 400, 2100, fc="#c5dbe9", ec="none", zorder=0)); ax.text(215, 560, "MORZE", fontsize=11, color="#4a7391", rotation=90)
ax.add_patch(Rectangle((-872, -85.45), 1042, 265.45, fc="#d9d6cf", ec="#444", lw=1.2, zorder=1)); ax.text(-860, 40, "MIASTO R1 — dziś (granica gry)", fontsize=10, color="#222", zorder=5)
ax.add_patch(Rectangle((-470, -472), 540, 272, fc="#e3cdb4", ec="#8a6a45", lw=.8, zorder=1)); ax.text(-455, -350, "FAWELA", fontsize=10, color="#6b4a25")
ax.add_patch(Rectangle((-470, -200), 540, 115, fc="#cfe0c2", ec="none", zorder=1)); ax.text(-455, -150, "las, dworzec, tory", fontsize=8, color="#4d6a3a")
ax.add_patch(Rectangle((-262, 126), 222, 54, fc="#b9d3a6", ec="none", zorder=2)); ax.text(-258, 165, "las", fontsize=7, color="#3f5d2c", zorder=6)
ax.add_patch(Rectangle((-152, 126), 52, 36, fc="#e0a458", ec="#333", lw=.8, zorder=3)); ax.annotate("sklep (supermarket)", xy=(-126, 144), xytext=(-40, 150), fontsize=8, arrowprops=dict(arrowstyle="-", color="#333"), zorder=7)
ax.plot([-66.2, -66.2], [-85, 123], color="#777", lw=4, zorder=3); ax.text(-62, -20, "wiadukt", fontsize=7, rotation=90, color="#444", zorder=6)
ax.plot(8, 34, "o", ms=9, color="#b8232f", zorder=6); ax.annotate("siłownia (ok.)", xy=(8, 34), xytext=(30, 70), fontsize=9, color="#b8232f", arrowprops=dict(arrowstyle="-", color="#b8232f"), zorder=7)
ax.annotate("", xy=(-120, 40), xytext=(-5, 40), arrowprops=dict(arrowstyle="-|>", color="#b8232f", lw=1.6), zorder=7); ax.text(-110, 47, "z siłowni w lewo", fontsize=8, color="#b8232f", zorder=7)
def rysuj(w, k, kol, ramka):
    def tnij(o):
        o.set_clip_path(ramka); return o
    for t in w["tereny"]:
        if t["zamkniety"] and t["rodzaj"] == "water": tnij(ax.add_patch(Polygon(gra(t["linia"], k), closed=True, fc="#c5dbe9", ec="none", zorder=2)))
        elif t["rodzaj"] in ("river", "canal") and len(t["linia"]) >= 2: tnij(ax.add_patch(Polygon(gra(pas(t["linia"], 19 if t["rodzaj"] == "river" else 8), k), closed=True, fc="#c5dbe9", ec="none", zorder=2)))
        elif t["zamkniety"] and t["rodzaj"] in ("grass", "park", "garden", "playground", "pitch", "wood"): tnij(ax.add_patch(Polygon(gra(t["linia"], k), closed=True, fc="#d5e3c4", ec="none", zorder=2)))
    for u in w["ulice"]:
        if u["klasa"] in ("primary", "secondary", "tertiary", "residential", "living_street", "unclassified", "pedestrian", "service") and len(u["linia"]) >= 2:
            for l in ax.plot(*zip(*gra(u["linia"], k)), color="#ffffff", lw=1.6 if u["klasa"] != "service" else .8, zorder=3): tnij(l)
    for b in w["budynki"]: tnij(ax.add_patch(Polygon(gra(b["obrys"], k), closed=True, fc=kol, ec="#2c3640", lw=.3, zorder=4)))
rA = ax.add_patch(Rectangle((KA[0] - 200 * K, KA[1] - 200 * K), 390 * K, 390 * K, fc="#f4f1ea", ec="#1f5fa8", lw=1.4, zorder=1))
rysuj(A, KA, "#8fa0ad", rA)
ax.text(KA[0] - 200 * K + 6, KA[1] + 190 * K - 22, "OSIEDLE — Skłodowskiej-Curie 32a\n390 × 390 m = 505 × 505 j.", fontsize=9, color="#1f5fa8", zorder=8, bbox=dict(fc="#ffffffd0", ec="none"))
ax.plot(*KA, "*", ms=11, color="#1f5fa8", zorder=9); ax.annotate("blok 32a", xy=KA, xytext=(KA[0] + 35, KA[1] - 45), fontsize=8, color="#1f5fa8", zorder=9)
rB = ax.add_patch(Rectangle((KB[0] - 70 * K, KB[1] - 75 * K), 285 * K, 545 * K, fc="#f4f1ea", ec="#8a3f8f", lw=1.4, zorder=1))
rysuj(B, KB, "#c9a27a", rB)
ax.text(KB[0] - 70 * K + 6, KB[1] + 470 * K - 26, "STARE MIASTO — Rynek, Mostowa,\nmost, Focha, Gdańska\n285 × 545 m = 369 × 705 j.", fontsize=9, color="#8a3f8f", zorder=8, bbox=dict(fc="#ffffffd0", ec="none"), va="top")
ax.plot(*KB, "*", ms=11, color="#8a3f8f", zorder=9); ax.annotate("Stary Rynek", xy=KB, xytext=(KB[0] + 60, KB[1] - 70), fontsize=8, color="#8a3f8f", zorder=9)
# laczniki (propozycja)
ax.plot([-126, -126], [162, 190], color="#d1495b", lw=2.4, zorder=6); ax.annotate("wjazd obok sklepu", xy=(-126, 178), xytext=(-60, 215), fontsize=7, color="#d1495b", arrowprops=dict(arrowstyle="-", color="#d1495b"), zorder=9)
zj = KB[1] + 212 * K; zs = KA[1] + 57 * K
ax.plot([-503, -405], [zj, zs], color="#d1495b", lw=2.4, zorder=6)
ax.annotate("łącznik: Jagiellońska → Skłodowskiej-Curie\n(w grze ok. 100 j.; naprawdę 2,2 km)", xy=(-455, (zj + zs) / 2), xytext=(-480, 760), fontsize=7, color="#d1495b", arrowprops=dict(arrowstyle="-", color="#d1495b"), zorder=9, bbox=dict(fc="#ffffffd0", ec="none"))
ax.plot([-700, -700], [180, 190], color="#d1495b", lw=2.4, zorder=6); ax.annotate("wjazd z nowych dzielnic", xy=(-700, 185), xytext=(-860, 150), fontsize=7, color="#d1495b", arrowprops=dict(arrowstyle="-", color="#d1495b"), zorder=9)
ax.text(-860, -60, "nowe dzielnice (wieżowce)", fontsize=8, color="#444", zorder=6)
ax.set_xlim(-900, 330); ax.set_ylim(-500, KB[1] + 470 * K + 40); ax.set_aspect("equal")
ax.set_xlabel("x gry (jednostki; 1 j. = 0,773 m)", fontsize=8); ax.set_ylabel("z gry — północ w górę", fontsize=8); ax.tick_params(labelsize=7); ax.grid(color="#0000000f", lw=.5)
ax.set_title("Propozycja: cała północna strona mapy — osiedle przy sklepie, stare miasto przy nowych wieżowcach", fontsize=12, loc="left")
fig.text(.01, .006, "Miasto R1 z granic w kodzie (BraxMiastoGranice). Położenie siłowni przybliżone. Układ Bydgoszczy: © autorzy OpenStreetMap (ODbL).", fontsize=6.5, color="#555")
fig.savefig(sys.argv[2], bbox_inches="tight", facecolor="white"); print("zapisano", sys.argv[2], "KA", KA, "KB", KB)
