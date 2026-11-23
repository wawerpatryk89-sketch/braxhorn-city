import json, math, sys
import matplotlib
matplotlib.use("Agg")
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
from matplotlib.patches import Polygon

wej, wyj, tytul, R = sys.argv[1], sys.argv[2], sys.argv[3], float(sys.argv[4])
cx, cy = (float(sys.argv[5]), float(sys.argv[6])) if len(sys.argv) > 6 else (0.0, 0.0)
d = json.load(open(wej, encoding="utf-8"))
fig, ax = plt.subplots(figsize=(10, 10), dpi=160)
ax.set_facecolor("#f4f1ea")
def blisko(p, m=1.25): return any(abs(x - cx) < R * m and abs(y - cy) < R * m for x, y in p)

ZIEL = {"grass", "park", "garden", "playground", "pitch", "wood", "scrub", "village_green", "recreation_ground", "meadow", "flowerbed"}
for t in d["tereny"]:
    p = t["linia"]
    if not blisko(p): continue
    r = t["rodzaj"]
    if r in ("water", "riverbank", "river", "canal") and t["zamkniety"]:
        ax.add_patch(Polygon(p, closed=True, fc="#b9d3e6", ec="none", zorder=1))
    elif r in ("river", "canal") and len(p) >= 2:
        ax.add_patch(Polygon(pas(p, 19 if r == "river" else 8), closed=True, fc="#b9d3e6", ec="none", zorder=1))
    elif r == "brzeg_wody":
        ax.plot(*zip(*p), color="#8fb3cc", lw=.8, zorder=2)
    elif r in ZIEL and t["zamkniety"]:
        ax.add_patch(Polygon(p, closed=True, fc="#d5e3c4", ec="none", zorder=1))
    elif r == "parking" and t["zamkniety"]:
        ax.add_patch(Polygon(p, closed=True, fc="#e3e0d8", ec="#b9b5aa", lw=.6, zorder=2))
    elif r == "pedestrian" and t["zamkniety"]:
        ax.add_patch(Polygon(p, closed=True, fc="#ece4d2", ec="none", zorder=1))

SZER = {"primary": 7, "secondary": 6, "tertiary": 5, "residential": 4, "living_street": 3.5, "unclassified": 4, "service": 2.6, "pedestrian": 4}
nazwy = {}
for u in d["ulice"]:
    p = u["linia"]
    if len(p) < 2 or not blisko(p): continue
    k = u["klasa"]
    if k.startswith("tory"):
        ax.plot(*zip(*p), color="#7a6f5d", lw=.8, ls=(0, (4, 2)), zorder=5)
    elif k in SZER:
        if k == "pedestrian" and p[0] == p[-1]:
            ax.add_patch(Polygon(p, closed=True, fc="#ece4d2", ec="#cfc6b0", lw=.6, zorder=1)); 
        else:
            ax.plot(*zip(*p), color="#ffffff" if k != "pedestrian" else "#ece4d2", lw=SZER[k] * 1.6, zorder=3, solid_capstyle="round")
            ax.plot(*zip(*p), color="#c9c4b8", lw=.4, zorder=3)
        if u.get("nazwa"):
            dl = sum(math.hypot(p[i+1][0]-p[i][0], p[i+1][1]-p[i][1]) for i in range(len(p)-1))
            nazwy.setdefault(u["nazwa"], []).append((dl, p))
    elif k in ("footway", "path", "steps", "cycleway"):
        ax.plot(*zip(*p), color="#cdbfa3", lw=.7, ls=(0, (2, 1.5)), zorder=2)

KOL = {"garages": "#c9b79c", "garage": "#c9b79c", "retail": "#e0a458", "commercial": "#e0a458", "kindergarten": "#9fb8c9", "school": "#9fb8c9", "church": "#b08ea2"}
for b in d["budynki"]:
    p = b["obrys"]
    if not blisko(p, 1.05): continue
    pi = b["pietra"]
    try: n = int(float(pi)) if pi else None
    except ValueError: n = None
    fc = KOL.get(b["rodzaj"]) or ("#e0a458" if b.get("uzytek") else None) or ("#5b6b7a" if (n or 0) >= 8 else "#8fa0ad" if n else "#b3bcc4")
    ax.add_patch(Polygon(p, closed=True, fc=fc, ec="#3b4650", lw=.6, zorder=6))
    x, y = b["srodek"]
    if abs(x - cx) < R and abs(y - cy) < R and (b["pole_m2"] > (140 if R < 250 else 900)):
        et = (b["nr"] or "") + ("\n%d p." % n if n else "")
        if et: ax.text(x, y, et, ha="center", va="center", fontsize=6.5 if R < 250 else 5, color="#111", zorder=9, linespacing=.9,
                       bbox=dict(boxstyle="round,pad=.12", fc="#ffffffcc", ec="none"))
for x, y in d.get("drzewa", []):
    if abs(x - cx) < R and abs(y - cy) < R: ax.plot(x, y, "o", ms=2.2, color="#7fa36b", zorder=4)

for n, l in nazwy.items():
    dl, p = max(l)
    q = [pt for pt in p if abs(pt[0]-cx) < R*.92 and abs(pt[1]-cy) < R*.92]
    if len(q) < 2: continue
    a, b2 = q[len(q)//2 - 1], q[len(q)//2]
    kat = math.degrees(math.atan2(b2[1]-a[1], b2[0]-a[0]))
    if kat > 90: kat -= 180
    if kat < -90: kat += 180
    ax.text((a[0]+b2[0])/2, (a[1]+b2[1])/2, n, rotation=kat, rotation_mode="anchor", ha="center", va="center", fontsize=6.5, color="#5a4a2f", zorder=8,
            bbox=dict(boxstyle="round,pad=.1", fc="#ffffffb0", ec="none"))

ax.set_xlim(cx - R, cx + R); ax.set_ylim(cy - R, cy + R); ax.set_aspect("equal")
krok = 50 if R <= 250 else 100
ax.set_xticks([t for t in range(int(cx - R)//krok*krok, int(cx + R) + 1, krok)]); ax.set_yticks([t for t in range(int(cy - R)//krok*krok, int(cy + R) + 1, krok)])
ax.grid(color="#00000014", lw=.5, zorder=0); ax.tick_params(labelsize=7, colors="#555")
ax.set_xlabel("metry na wschód", fontsize=8, color="#555"); ax.set_ylabel("metry na północ", fontsize=8, color="#555")
ax.set_title(tytul, fontsize=12, loc="left", color="#222", pad=10)
ax.annotate("N", xy=(cx + R*.92, cy + R*.93), xytext=(cx + R*.92, cy + R*.80), ha="center", fontsize=10, color="#222",
            arrowprops=dict(arrowstyle="-|>", color="#222"), zorder=10)
fig.text(.01, .008, "Układ: © autorzy OpenStreetMap (ODbL), pobrane 23.11.2026. Kratka co %d m. Liczba pięter tylko tam, gdzie jest w mapie." % krok, fontsize=7, color="#666")
fig.savefig(wyj, bbox_inches="tight", facecolor="white")
print("zapisano", wyj)
