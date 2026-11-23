"""Tnie plansze graffiti właściciela na pojedyncze naklejki z przezroczystym tłem.
Tło = biel połączona z brzegiem obrazu (biel WEWNĄTRZ liter zostaje)."""
import json, sys
import numpy as np
from PIL import Image
from scipy import ndimage as nd

wyj = sys.argv[1]
PLIKI = [("plansza_d", "ee9bdb22-image.png", True, [])]
man = []
for nazwa, plik, tnij, pomin in PLIKI:
    rgba = np.asarray(Image.open("/mnt/user-data/uploads/" + plik).convert("RGBA")).copy()
    H, W, _ = rgba.shape
    if rgba[..., 3].min() > 250:       # obraz bez przezroczystości: tło = biel połączona z brzegiem (biel w literach zostaje)
        jasne = rgba[..., :3].min(axis=2) > 238
        et, n = nd.label(jasne)
        brzeg = set(np.unique(np.concatenate([et[0], et[-1], et[:, 0], et[:, -1]]))) - {0}
        tlo = np.isin(et, list(brzeg))
        rgba[..., 3] = np.where(tlo, 0, 255)
        rgba[..., 3][nd.binary_dilation(tlo, iterations=1) & ~tlo] = 140
    tresc = rgba[..., 3] > 130         # mgiełka sprayu i zacieki poniżej progu nie sklejają sąsiednich kafli
    if tnij:
        grupy, m = nd.label(nd.binary_dilation(tresc, iterations=2))
        ramki = nd.find_objects(grupy)
        pola = nd.sum(np.ones_like(grupy), grupy, range(1, m + 1))
        duze = [i + 1 for i in range(m) if pola[i] >= 9000]
        # drobne plamy i krople dołączają do dużego kafla, w którego ramce (z zapasem) leży ich środek
        for i in range(m):
            if pola[i] >= 9000 or ramki[i] is None: continue
            cy, cx = (ramki[i][0].start + ramki[i][0].stop) / 2, (ramki[i][1].start + ramki[i][1].stop) / 2
            naj, najd = None, 1e9
            for dz in duze:
                r = ramki[dz - 1]
                if r[0].start - 26 <= cy <= r[0].stop + 26 and r[1].start - 26 <= cx <= r[1].stop + 26:
                    dd = abs(cy - (r[0].start + r[0].stop) / 2) + abs(cx - (r[1].start + r[1].stop) / 2)
                    if dd < najd: naj, najd = dz, dd
            grupy[grupy == i + 1] = naj if naj else 0
        ramki = nd.find_objects(grupy)
        grupy = np.where(rgba[..., 3] > 0, nd.grey_dilation(grupy, size=(15, 15)), 0)   # miękkie brzegi wracają do swojego kafla
        ramki = nd.find_objects(grupy)
    else:
        ys, xs = np.where(rgba[..., 3] > 20); ramki = [(slice(ys.min(), ys.max() + 1), slice(xs.min(), xs.max() + 1))]; grupy = None
    kafle = []
    for idx, r in enumerate(ramki, 1):
        if r is None: continue
        y0, y1, x0, x1 = r[0].start, r[0].stop, r[1].start, r[1].stop
        if (y1 - y0) * (x1 - x0) < 6000: continue
        kafle.append((y0, y1, x0, x1, idx))
    kafle.sort(key=lambda k: (round((k[0] + k[1]) / 2 / (H / 4.2)), k[2]))
    nr = 0
    for y0, y1, x0, x1, idx in kafle:
        cx, cy = (x0 + x1) / 2 / W, (y0 + y1) / 2 / H
        if any(abs(cx - px) < .11 and abs(cy - py) < .13 for px, py in pomin):
            man.append({"plansza": nazwa, "pominiety": True, "powod": "prawdziwa marka albo realistyczna postać (ZASADY 7.4)", "ramka": [int(x0), int(y0), int(x1), int(y1)]}); continue
        nr += 1
        wyc = rgba[y0:y1, x0:x1].copy()
        if grupy is not None:
            wyc[..., 3] = np.where(grupy[y0:y1, x0:x1] == idx, wyc[..., 3], 0)   # sąsiedni kafel w ramce nie wchodzi
        n2 = "%s_%02d.png" % (nazwa, nr) if tnij else nazwa + ".png"
        Image.fromarray(wyc, "RGBA").save(wyj + "/" + n2, optimize=True)
        man.append({"plik": n2, "plansza": nazwa, "szer_px": int(x1 - x0), "wys_px": int(y1 - y0), "krycie": round(float((wyc[..., 3] > 0).mean()), 2)})
    print(nazwa, "kafli", nr, "pominiętych", sum(1 for q in man if q.get("pominiety") and q["plansza"] == nazwa), "obraz", W, "x", H)
json.dump({"zrodlo": "plansze właściciela, 23.11.2026 02:16", "naklejki": man}, open(wyj + "/graffiti_d.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("razem naklejek", sum(1 for q in man if "plik" in q))
