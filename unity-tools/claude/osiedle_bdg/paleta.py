"""MIASTO R1 — osiedle bydgoskie: jedna paleta materiałów dla podglądu i dla gry (C# czyta paleta.json).

Każdy materiał: tex = tekstura z listy właściciela (Poly Haven, ZASADY 5.8) albo None (gładki), m = rozmiar kafla w metrach,
kol = barwa sRGB docelowa (średnia tekstury przemnożona do tej barwy), szorst, metal, brud = mapa zacieków (UV1),
nakl = naklejka RGBA na UV1 (graffiti, napisy), alfa = wycinanie (balustrada z prętów).
Kolory elewacji — pomiar ze zdjęć właściciela (mediany pól ściany), 32a przyciemnione na jego prośbę (23.11, 15:32).
"""
import json
import sys
from pathlib import Path

ST = "white_stucco_02"; BET = "brushed_concrete_04"


def tynk(kol, brud=None, nakl=None):
    return {"tex": ST, "m": 2.0, "kol": kol, "szorst": 1.0, "brud": brud, "nakl": nakl}


P = {
    # wieżowce (kolory ze zdjęć: 32a IMG_1427/1624, 28a IMG szczyt od Leclerca, 30a w tle IMG_1427, 34a nad barem)
    "tynk_32a": tynk([208, 168, 104], "brud_32a_wejscie.png"), "szczyt_32a": tynk([200, 182, 136], "brud_32a_slepa.png"),
    "napis_32a": tynk([200, 182, 136], None, "napis_32a.png"), "graffiti_32a": tynk([214, 184, 104], None, "graffiti_32a.png"),
    "tynk_28a": tynk([172, 186, 160], "brud_32a_wejscie.png"), "szczyt_28a": tynk([178, 172, 152], "brud_32a_slepa.png"),
    "napis_28a": tynk([178, 172, 152], None, "napis_28a.png"),
    "tynk_30a": tynk([214, 182, 98], "brud_32a_wejscie.png"), "szczyt_30a": tynk([206, 186, 122], "brud_32a_slepa.png"),
    "napis_30a": tynk([206, 186, 122], None, "napis_30a.png"),
    "tynk_34a": tynk([202, 186, 150], "brud_32a_wejscie.png"), "szczyt_34a": tynk([214, 184, 110], "brud_32a_slepa.png"),
    "napis_34a": tynk([214, 184, 110], None, "napis_34a.png"),
    "opaska": tynk([198, 194, 184]), "tynk_zolty": tynk([214, 184, 104]),
    "cokol": {"tex": BET, "m": 1.5, "kol": [118, 123, 130], "szorst": 1.0},
    "cokol4": {"tex": BET, "m": 1.5, "kol": [128, 130, 132], "szorst": 1.0},
    "klatka4": tynk([150, 132, 120]),
    "komin": tynk([150, 140, 130]),
    "beton_daszek": {"tex": BET, "m": 1.5, "kol": [170, 168, 160], "szorst": 1.0},
    "beton_schody": {"tex": BET, "m": 1.5, "kol": [150, 148, 142], "szorst": 1.0},
    "beton_balkon": {"tex": BET, "m": 1.5, "kol": [168, 164, 154], "szorst": 1.0},
    "plycina": {"tex": None, "kol": [158, 160, 158], "szorst": .55}, "listwa_braz": {"tex": None, "kol": [92, 62, 46], "szorst": .6},
    "pcv": {"tex": None, "kol": [236, 236, 232], "szorst": .35}, "parapet": {"tex": None, "kol": [150, 152, 154], "szorst": .4, "metal": .5},
    "obrobka": {"tex": None, "kol": [140, 142, 144], "szorst": .45, "metal": .5},
    "drzwi_czerwone": {"tex": None, "kol": [168, 28, 30], "szorst": .45}, "drzwi_braz": {"tex": None, "kol": [104, 66, 46], "szorst": .5},
    "drzwi_stal": {"tex": None, "kol": [90, 92, 95], "szorst": .5, "metal": .4},
    "szyba_drzwi": {"tex": None, "kol": [40, 46, 50], "szorst": .08, "metal": .2}, "szyba_piwnica": {"tex": None, "kol": [30, 32, 34], "szorst": .2},
    "stal": {"tex": None, "kol": [60, 62, 64], "szorst": .45, "metal": .7}, "kratka": {"tex": None, "kol": [25, 25, 25], "szorst": .8},
    "skrzynka_gaz": {"tex": None, "kol": [212, 172, 40], "szorst": .5}, "rura_gaz": {"tex": None, "kol": [212, 172, 40], "szorst": .5},
    "papa": {"tex": None, "kol": [60, 60, 62], "szorst": .9}, "lampa": {"tex": None, "kol": [240, 240, 236], "szorst": .4},
    "tabliczka": {"tex": None, "kol": [40, 70, 150], "szorst": .4}, "domofon": {"tex": None, "kol": [120, 122, 124], "szorst": .4, "metal": .4},
    "zwir": {"tex": None, "kol": [128, 124, 118], "szorst": .95},
    "stal_balustrada": {"tex": None, "kol": [62, 64, 66], "szorst": .45, "metal": .6},
    "balustrada_prety": {"tex": None, "kol": [58, 60, 62], "szorst": .45, "metal": .6, "alfa": "balustrada_prety.png"},
    "balustrada_blacha_zielona": {"tex": None, "kol": [52, 128, 74], "szorst": .5, "metal": .2},
    "balustrada_blacha_ciemna": {"tex": None, "kol": [38, 38, 40], "szorst": .5, "metal": .2},
    "balustrada_blacha_biala": {"tex": None, "kol": [222, 220, 212], "szorst": .5, "metal": .1},
    "balustrada_blacha_braz": {"tex": None, "kol": [118, 62, 46], "szorst": .5, "metal": .2},
    "kostka": {"tex": "concrete_pavers", "m": 1.0, "kol": [150, 146, 140], "szorst": 1.0},
    "kraweznik": {"tex": BET, "m": 1.5, "kol": [160, 158, 152], "szorst": 1.0},
    "asfalt": {"tex": "clean_asphalt", "m": 4.0, "kol": [92, 92, 94], "szorst": 1.0},
    "trawa_podglad": {"tex": None, "kol": [88, 112, 62], "szorst": .95},
    # bar mleczny 34b (IMG_1631–1634), garaże (IMG_1418–1429, IMG_1632), pawilony przy głównej ulicy
    "tynk_bar": tynk([222, 196, 120], "brud_32a_wejscie.png"), "mural_zawisza": tynk([222, 196, 120], None, "mural_zawisza.png"),
    "tablica_bar": {"tex": None, "kol": [22, 22, 24], "szorst": .4, "nakl": "tablica_bar.png"},
    "okap_blacha": {"tex": None, "kol": [118, 122, 126], "szorst": .45, "metal": .5},
    "pilaster": tynk([226, 224, 216]), "garaz_a": tynk([214, 172, 142], "brud_32a_slepa.png"), "garaz_b": tynk([222, 204, 170], "brud_32a_slepa.png"),
    "brama_drewno_czerw": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [128, 58, 44], "szorst": .9},
    "brama_drewno_braz": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [104, 68, 46], "szorst": .9},
    "brama_uchylna": {"tex": None, "kol": [112, 74, 50], "szorst": .5, "metal": .3},
    "tynk_paw_a": tynk([232, 232, 228]), "tynk_paw_b": tynk([226, 214, 170]),
    "pas_pomarancz": {"tex": None, "kol": [222, 110, 40], "szorst": .5}, "pas_granat": {"tex": None, "kol": [36, 52, 110], "szorst": .5},
    # teren (OSM + zdjęcia)
    "asfalt_parking": {"tex": "clean_asphalt", "m": 4.0, "kol": [84, 84, 86], "szorst": 1.0},
    "linia_biala": {"tex": None, "kol": [232, 232, 228], "szorst": .6},
    "boisko": {"tex": BET, "m": 2.0, "kol": [150, 150, 148], "szorst": 1.0},
    "plac_zabaw": {"tex": None, "kol": [196, 170, 128], "szorst": 1.0},
    "stal_ocynk": {"tex": None, "kol": [178, 182, 186], "szorst": .4, "metal": .8},
    "trawa": {"tex": None, "kol": [86, 112, 58], "szorst": 1.0, "gra": "BraxRegionTrawa"},
}
# bloki czterokondygnacyjne: palety ze zdjęć (1 = blok 30 IMG_1611, 2 = blok 32 IMG_1605, 3 = blok przy garażach IMG_1420, 4 = blok 28 kremowy)
for k, (dol, a, b, sz) in {1: ([168, 140, 120], [214, 196, 150], [206, 176, 110], [208, 190, 140]),
                           2: ([150, 118, 108], [218, 204, 168], [212, 180, 112], [212, 196, 150]),
                           3: ([140, 118, 110], [222, 210, 180], [204, 170, 110], [214, 200, 160]),
                           4: ([170, 160, 140], [226, 214, 186], [214, 200, 160], [222, 210, 180])}.items():
    P["pas_dol_%d" % k] = tynk(dol); P["pole_a_%d" % k] = tynk(a, "brud_32a_wejscie.png"); P["pole_b_%d" % k] = tynk(b, "brud_32a_wejscie.png")
    P["szczyt4_%d" % k] = tynk(sz, "brud_32a_slepa.png")
for nr, k in (("28", 4), ("30", 1), ("32", 2), ("34", 3), ("36", 3), ("38", 1)):
    P["napis_blok_" + nr] = tynk(P["szczyt4_%d" % k]["kol"], None, "napis_blok_%s.png" % nr)

# średnie barwy tekstur (sRGB średniej liniowej, pomiar plików 2K Poly Haven 23.11) — do przeliczenia barwy docelowej na _Color
SREDNIE = {"white_stucco_02": [.683, .679, .666], "brushed_concrete_04": [.409, .368, .327], "concrete_pavers": [.409, .372, .328],
           "clean_asphalt": [.270, .274, .276], "weathered_brown_planks": [.380, .300, .232]}


def tint(v):
    """_Color (sRGB) taki, że tekstura x _Color daje barwę docelową w przestrzeni liniowej (projekt Unity: Linear)."""
    if not v.get("tex"): return [round(c / 255, 4) for c in v["kol"]]
    a = SREDNIE[v["tex"]]
    return [round(((c / 255) ** 2.2 / (s ** 2.2)) ** (1 / 2.2), 4) for c, s in zip(v["kol"], a)]


for _k, _v in P.items():
    _v["tint"] = tint(_v)

if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    (out / "paleta.json").write_text(json.dumps(P, ensure_ascii=False, indent=0), encoding="utf-8")
    print(len(P), "materiałów")
