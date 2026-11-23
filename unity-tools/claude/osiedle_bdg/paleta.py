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
    "papa": {"tex": "bitumen", "m": 10.0, "kol": [62, 62, 64], "szorst": 1.0}, "lampa": {"tex": None, "kol": [240, 240, 236], "szorst": .4},
    "tabliczka": {"tex": None, "kol": [40, 70, 150], "szorst": .4}, "domofon": {"tex": None, "kol": [120, 122, 124], "szorst": .4, "metal": .4},
    "zwir": {"tex": "gravel_floor_02", "m": 2.0, "kol": [128, 124, 118], "szorst": 1.0},
    "stal_balustrada": {"tex": None, "kol": [62, 64, 66], "szorst": .45, "metal": .6},
    "balustrada_prety": {"tex": None, "kol": [58, 60, 62], "szorst": .45, "metal": .6, "alfa": "balustrada_prety.png"},
    "balustrada_blacha_zielona": {"tex": None, "kol": [52, 128, 74], "szorst": .5, "metal": .2},
    "balustrada_blacha_ciemna": {"tex": None, "kol": [38, 38, 40], "szorst": .5, "metal": .2},
    "balustrada_blacha_biala": {"tex": None, "kol": [222, 220, 212], "szorst": .5, "metal": .1},
    "balustrada_blacha_braz": {"tex": None, "kol": [118, 62, 46], "szorst": .5, "metal": .2},
    "kostka": {"tex": "concrete_pavers", "m": 1.0, "kol": [150, 146, 140], "szorst": 1.0},
    "kraweznik": {"tex": BET, "m": 1.5, "kol": [160, 158, 152], "szorst": 1.0},
    "asfalt": {"tex": "clean_asphalt", "m": 4.0, "kol": [92, 92, 94], "szorst": 1.0},
    "trawa_podglad": {"tex": None, "kol": [86, 112, 58], "szorst": 1.0},   # tylko tło podglądu (1,2 km), w grze horyzont
    # bar mleczny 34b (IMG_1631–1634), garaże (IMG_1418–1429, IMG_1632), pawilony przy głównej ulicy
    "tynk_bar": tynk([222, 196, 120], "brud_32a_wejscie.png"), "mural_zawisza": tynk([222, 196, 120], None, "mural_zawisza.png"),
    "tablica_bar": {"tex": None, "kol": [22, 22, 24], "szorst": .4, "nakl": "tablica_bar.png"},
    "okap_blacha": {"tex": None, "kol": [118, 122, 126], "szorst": .45, "metal": .5},
    "pilaster": tynk([226, 224, 216]), "garaz_a": tynk([214, 172, 142], "brud_32a_slepa.png"), "garaz_b": tynk([222, 204, 170], "brud_32a_slepa.png"),
    "brama_drewno_czerw": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [128, 58, 44], "szorst": .9},
    "brama_drewno_braz": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [104, 68, 46], "szorst": .9},
    "brama_uchylna": {"tex": "painted_metal_shutter", "m": 2.0, "kol": [112, 74, 50], "szorst": .8},
    "tynk_paw_a": tynk([232, 232, 228]), "tynk_paw_b": tynk([226, 214, 170]),
    "pas_pomarancz": {"tex": None, "kol": [222, 110, 40], "szorst": .5}, "pas_granat": {"tex": None, "kol": [36, 52, 110], "szorst": .5},
    # teren (OSM + zdjęcia)
    "asfalt_parking": {"tex": "clean_asphalt", "m": 4.0, "kol": [84, 84, 86], "szorst": 1.0},
    "linia_biala": {"tex": None, "kol": [232, 232, 228], "szorst": .6},
    "boisko": {"tex": BET, "m": 2.0, "kol": [150, 150, 148], "szorst": 1.0},
    "plac_zabaw": {"tex": None, "kol": [196, 170, 128], "szorst": 1.0},
    "stal_ocynk": {"tex": None, "kol": [178, 182, 186], "szorst": .4, "metal": .8},
    # wyposażenie ulic (wyposazenie.py: położenia z OSM, wygląd ze zdjęć właściciela)
    "sygnal_szary": {"tex": None, "kol": [120, 124, 128], "szorst": .5, "metal": .5}, "sygnal_czarny": {"tex": None, "kol": [24, 24, 26], "szorst": .5},
    "swiatlo_czerwone": {"tex": None, "kol": [150, 20, 16], "szorst": .2}, "swiatlo_zolte": {"tex": None, "kol": [120, 90, 20], "szorst": .2},
    "swiatlo_zielone": {"tex": None, "kol": [40, 200, 110], "szorst": .2},
    "wiata_rama": {"tex": None, "kol": [30, 40, 70], "szorst": .45, "metal": .5}, "szyba_wiata": {"tex": None, "kol": [70, 86, 92], "szorst": .06, "metal": .2},
    "szlaban_pomarancz": {"tex": None, "kol": [226, 112, 28], "szorst": .45}, "szlaban_czerwony": {"tex": None, "kol": [196, 30, 30], "szorst": .45},
    "szlaban_bialy": {"tex": None, "kol": [236, 236, 232], "szorst": .45},
    "beton_lawka": {"tex": BET, "m": 1.5, "kol": [160, 158, 152], "szorst": 1.0},
    "lawka_drewno": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [128, 88, 58], "szorst": .8},
    "paczkomat": {"tex": None, "kol": [84, 88, 92], "szorst": .45, "metal": .4}, "paczkomat_ekran": {"tex": None, "kol": [20, 24, 28], "szorst": .1},
    "kontener_zielony": {"tex": None, "kol": [40, 110, 56], "szorst": .6}, "kontener_zolty": {"tex": None, "kol": [222, 186, 40], "szorst": .6},
    "kontener_niebieski": {"tex": None, "kol": [36, 76, 150], "szorst": .6},
    "hydrant": {"tex": None, "kol": [190, 30, 26], "szorst": .5, "metal": .3},
    "plot_rama": {"tex": None, "kol": [112, 50, 42], "szorst": .5, "metal": .4},
    "plot_prety": {"tex": None, "kol": [112, 50, 42], "szorst": .5, "metal": .4, "alfa": "plot_prety.png"},
    "zywoplot": {"tex": None, "wlasna": "zywoplot_lisc.png", "m": 1.0, "kol": [255, 255, 255], "szorst": .9},
    "zywoplot_brzeg": {"tex": None, "kol": [255, 255, 255], "szorst": .9, "alfa": "zywoplot_brzeg.png"},
    "zywoplot_galazki": {"tex": None, "kol": [255, 255, 255], "szorst": .85, "alfa": "zywoplot_galazki.png"},   # karty gałązek
    "latarnia_beton": {"tex": BET, "m": 1.5, "kol": [150, 150, 146], "szorst": 1.0},
    "latarnia_stal": {"tex": None, "kol": [70, 74, 78], "szorst": .45, "metal": .6},
    "oprawa": {"tex": None, "kol": [90, 94, 98], "szorst": .4, "metal": .5},
    # budynki z obrysu (ogolny.py) — kolory: zdjęcia właściciela IMG_1669–1677, lotnicze IMG_1664–1668; reszta ZAŁOŻENIE
    "szpital_bialy": tynk([226, 228, 226], "brud_32a_wejscie.png"), "szpital_szary": tynk([196, 198, 200], "brud_32a_wejscie.png"),
    "market_elewacja": {"tex": "exterior_wall_cladding_03", "m": 1.96, "kol": [210, 206, 198], "szorst": .8},
    "tynk_szary": tynk([196, 194, 186], "brud_32a_wejscie.png"), "tynk_kremowy": tynk([222, 210, 180], "brud_32a_wejscie.png"),
    "tynk_bialy": tynk([232, 230, 224], "brud_32a_wejscie.png"), "tynk_attyka": tynk([180, 178, 172]),
    # bloki przy Wyszyńskiego (IMG_1722–1726, 23.11): pola kolorów — żółty, łososiowy, szaroliliowy, jasnozielony; nadbudówki beż
    "wysz_zolty": tynk([212, 204, 140], "brud_32a_wejscie.png"), "wysz_losos": tynk([218, 166, 128], "brud_32a_wejscie.png"),
    "wysz_lawenda": tynk([180, 180, 198], "brud_32a_wejscie.png"), "wysz_ziel": tynk([190, 200, 140], "brud_32a_wejscie.png"),
    "wysz_bez": tynk([220, 200, 182], "brud_32a_slepa.png"), "wysz_pas": tynk([90, 120, 170]),
    "asfalt_czerwony": {"tex": "clean_asphalt", "m": 4.0, "kol": [150, 76, 70], "szorst": 1.0},
    # zdjęcia 23.11, 17:07–17:09: szkoła budowlana jasnozielona (IMG_1729–1735), market biały z niebieskim pasem (IMG_1730),
    # SOR biały z lądowiskiem (IMG_1727), przedszkole kremowe (IMG_1736–1737)
    "szkola_ziel": tynk([166, 192, 146], "brud_32a_wejscie.png"), "market_niebieski": {"tex": None, "kol": [30, 70, 150], "szorst": .45, "metal": .3},
    "market_rama": {"tex": None, "kol": [190, 192, 194], "szorst": .5, "metal": .2}, "market_szklo": {"tex": None, "kol": [70, 92, 110], "szorst": .06, "metal": .5},
    "dach_ciemny": {"tex": "corrugated_iron_02", "m": 2.7, "kol": [70, 74, 80], "szorst": .8},
    "lamele": {"tex": None, "kol": [150, 155, 160], "szorst": .45, "metal": .6}, "szpital_panel": {"tex": "exterior_wall_cladding_03", "m": 1.96, "kol": [112, 116, 122], "szorst": .8},
    "szpital_krem": tynk([226, 216, 196], "brud_32a_wejscie.png"), "szpital_cegla": {"tex": "red_bricks_04", "m": 2.5, "kol": [160, 86, 62], "szorst": 1.0},
    "ladowisko_krzyz_tlo": {"tex": None, "kol": [190, 40, 36], "szorst": .8}, "ladowisko_krzyz": {"tex": None, "kol": [236, 236, 232], "szorst": .8},
    "pole_logo": {"tex": None, "kol": [236, 238, 240], "szorst": .4},
    "ladowisko_okrag": {"tex": None, "kol": [232, 196, 40], "szorst": .8},
    "ladowisko": {"tex": BET, "m": 2.0, "kol": [168, 170, 172], "szorst": 1.0}, "przedszkole": tynk([218, 204, 176], "brud_32a_wejscie.png"),
    "dachowka": {"tex": "clay_roof_tiles_02", "m": 2.0, "kol": [168, 70, 44], "szorst": .8},
    "palik": {"tex": "weathered_brown_planks", "m": 1.2, "kol": [150, 118, 84], "szorst": .9},
    "trawa": {"tex": "grass004", "m": 2.5, "kol": [86, 112, 58], "szorst": 1.0, "gra": "BraxRegionTrawa"},
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
           "clean_asphalt": [.270, .274, .276], "weathered_brown_planks": [.380, .300, .232],
           "clay_roof_tiles_02": [.590, .323, .177],
           # 23.11 wieczorem (właściciel: „tekstury wszędzie, jak najbardziej realistycznie”) — pomiar plików 2K jak wyżej
           "bitumen": [.230, .214, .206], "gravel_floor_02": [.672, .656, .613], "painted_metal_shutter": [.502, .516, .527],
           "exterior_wall_cladding_03": [.678, .654, .607], "corrugated_iron_02": [.348, .345, .319], "red_bricks_04": [.386, .243, .145],
           "grass004": [.384, .431, .197], "herringbone_pavement_03": [.264, .232, .195], "red_brick_pavers": [.455, .250, .198],
           "playground_sand": [.485, .411, .276], "lakier_blacha": [.745, .745, .745], "grey_tiles": [.432, .378, .287], "plyty_szare": [.80, .80, .80], "granite_tile": [.306, .307, .308], "granite_tile_04": [.443, .396, .339],
           "rectangular_paving": [.499, .424, .336], "sandstone_blocks_04": [.774, .647, .501], "plotno_parasol": [.847, .847, .847],
           # Stary Rynek 21:57 (lista właściciela, MATERIALY-WLASCICIELA): stare cegły, kamień, tynk z fakturą, dachówki, bruk
           "red_brick": [.557, .403, .315], "medieval_red_brick": [.509, .388, .312], "brick_wall_006": [.676, .456, .326],
           "brick_wall_02": [.636, .559, .463], "stacked_brick_wall": [.442, .273, .177], "whitewashed_brick": [.650, .601, .551],
           "castle_wall_varriation": [.535, .499, .417], "rustic_stone_wall": [.466, .401, .300], "stone_wall_04": [.436, .403, .371],
           "rock_wall_13": [.298, .259, .223], "tynk_kamienica": [.744, .744, .744], "clay_roof_tiles_03": [.555, .334, .215],
           "ceramic_roof_01": [.557, .462, .322], "roof_tiles": [.675, .556, .430], "grey_roof_01": [.341, .336, .322],
           "stone_tiles_03": [.478, .427, .317], "brick_pavement_02": [.312, .286, .255], "pavement_02": [.588, .502, .414],
           "dark_paneled_wood": [.163, .084, .069]}


def tint(v):
    """_Color (sRGB) taki, że tekstura x _Color daje barwę docelową w przestrzeni liniowej (projekt Unity: Linear)."""
    if not v.get("tex"): return [round(c / 255, 4) for c in v["kol"]]
    a = SREDNIE[v["tex"]]
    return [round(((c / 255) ** 2.2 / (s ** 2.2)) ** (1 / 2.2), 4) for c, s in zip(v["kol"], a)]


# 23.11, 17:58 właściciel: „dosłownie wszystko ma mieć tekstury premium”. Malowana blacha, stal, PCV, farba na jezdni,
# drzwi i drobne wyposażenie dostają skan lakierowanej blachy (blue_metal_plate z Poly Haven odbarwiony do szarości —
# lakier_blacha: ta sama faktura, nierówności i normalne, barwę daje kol). Bez tekstury zostaje tylko szkło (gładkie,
# odbija niebo — tak jak prawdziwe) i świecące soczewki lamp/sygnalizatorów.
LAKIER = ["plycina", "listwa_braz", "pcv", "parapet", "obrobka", "drzwi_czerwone", "drzwi_braz", "drzwi_stal", "stal", "kratka",
          "skrzynka_gaz", "rura_gaz", "tabliczka", "domofon", "stal_balustrada", "balustrada_blacha_zielona", "balustrada_blacha_ciemna",
          "balustrada_blacha_biala", "balustrada_blacha_braz", "okap_blacha", "pas_pomarancz", "pas_granat", "linia_biala",
          "stal_ocynk", "sygnal_szary", "sygnal_czarny", "wiata_rama", "szlaban_pomarancz", "szlaban_czerwony", "szlaban_bialy",
          "paczkomat", "kontener_zielony", "kontener_zolty", "kontener_niebieski", "hydrant", "plot_rama", "latarnia_stal", "oprawa",
          "market_niebieski", "market_rama", "lamele", "ladowisko_krzyz_tlo", "ladowisko_krzyz", "ladowisko_okrag", "pole_logo"]
for _k in LAKIER:
    if _k in P and not P[_k].get("tex"):
        P[_k] = dict(P[_k], tex="lakier_blacha", m=2.5, szorst=max(.6, P[_k].get("szorst", .6)))
# budki z jedzeniem przed marketem (budki.py) — lakierowana blacha, szyldy z naklejką
for _k, _c in (("kiosk_bialy", [232, 230, 224]), ("kiosk_dach", [70, 72, 76]), ("kiosk_wnetrze", [52, 50, 48]), ("kiosk_blat", [180, 182, 184]),
               ("markiza", [196, 40, 36]), ("przyczepa_biala", [236, 236, 232]), ("przyczepa_czerwona", [190, 34, 30]), ("opona", [26, 26, 28]),
               ("slupek_zolty", [226, 188, 30]), ("kontener_zolty", [232, 206, 84]), ("kontener_zolty_rel", [214, 196, 96]),
               ("markiza_pasy", [226, 222, 214]), ("slupek_bialy", [236, 236, 232]), ("slupek_czerwony", [196, 30, 30])):
    P[_k] = {"tex": "lakier_blacha", "m": 2.5, "kol": _c, "szorst": .6}
P["szyld_bosna"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [236, 196, 40], "szorst": .6, "nakl": "szyld_bosna.png"}
P["szyld_burger"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [28, 28, 30], "szorst": .6, "nakl": "szyld_burger.png"}
P["szyld_zapiekanki"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [196, 40, 34], "szorst": .6, "nakl": "szyld_zapiekanki.png"}
P["menu_bosna"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [26, 28, 28], "szorst": .7, "nakl": "menu_bosna.png"}   # tablica busa z bosną
P["szyld_lody"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [44, 44, 46], "szorst": .6, "nakl": "szyld_lody.png"}
P["deska"] = {"tex": "weathered_brown_planks", "m": 2.0, "kol": [120, 96, 72], "szorst": 1.0}
P["trawa_ozdobna"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [200, 184, 140], "szorst": 1.0}
P["market_elewacja"] = {"tex": "plyty_szare", "m": 4.4, "kol": [204, 198, 186], "szorst": 1.0}   # kwadratowe płyty ~1,1 m (zdjęcia 18:47)
P["slup_grafit"] = {"tex": "granite_tile", "m": 2.3, "kol": [72, 74, 76], "szorst": .35}   # 20:34: słupy podcienia — polerowany ciemny granit (płyty z fugami)
P["posadzka"] = {"tex": BET, "m": 2.0, "kol": [168, 166, 160], "szorst": .8}
P["pas_zielony"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [40, 120, 70], "szorst": .6}
for _k in range(1, 5):
    P["baner_%d" % _k] = {"tex": "lakier_blacha", "m": 2.5, "kol": [30, 70, 150], "szorst": .7, "nakl": "baner_%d.png" % _k}
# Stary Rynek (rynek_bdg/rynek.py): tynki kamienic wg zdjęć właściciela (zdjecia/stare_miasto 01–13), płyty Rynku, nabrzeża Brdy
# 21:56–21:59 właściciel: „nie wyglądają, jakby miały teksturę” → tynk z wyraźną fakturą (plastered_wall_05 z listy,
# odbarwiony jak lakier_blacha — barwę daje paleta), parter z kamienia z listy (rustic_stone_wall) w odcieniu tynku.
for _k, _c in (("k_zolty", [222, 196, 120]), ("k_krem", [232, 220, 190]), ("k_szaroniebieski", [170, 182, 190]), ("k_rozowy", [222, 172, 160]),
               ("k_bialy", [236, 232, 222]), ("k_ochra", [206, 168, 104]), ("k_zielony", [188, 202, 168]), ("k_terakota", [196, 128, 96]),
               ("k_kremowy2", [226, 206, 160]), ("k_jasnozolty", [228, 212, 150]), ("k_losos", [226, 194, 180]), ("k_niebieskoszary", [176, 186, 194])):
    P[_k] = {"tex": "tynk_kamienica", "m": 2.0, "kol": _c, "szorst": 1.0}
    P["kb" + _k[1:]] = {"tex": "rustic_stone_wall", "m": 1.52, "kol": [int((c + s_ * 255) / 2 * .92) for c, s_ in zip(_c, [.466, .401, .300])], "szorst": .95}
# stare cegły z listy właściciela — każda w swojej barwie (kol = średnia skanu), kilka odmian, żeby nie było jednej
for _k, (_t, _m) in {"c_czerwona": ("red_brick", 1.4), "c_sredniowieczna": ("medieval_red_brick", 2.0), "c_ceglasta": ("brick_wall_006", 3.0),
                     "c_mieszana": ("brick_wall_02", 2.0), "c_stara": ("stacked_brick_wall", 1.73), "c_bielona": ("whitewashed_brick", 2.0)}.items():
    P[_k] = {"tex": _t, "m": _m, "kol": [round(c * 255) for c in SREDNIE[_t]], "szorst": .95}
# elewacje kamienne z listy (21:58 „taki kamień na elewację w kolorze”, 21:59 „gdzieniegdzie”): naturalne i barwione
for _k, (_t, _m, _c) in {"s_piaskowiec": ("rustic_stone_wall", 1.52, None), "s_szary": ("stone_wall_04", 1.7, None),
                         "s_zamkowy": ("castle_wall_varriation", 2.0, None), "s_grafit": ("rock_wall_13", 2.0, None),
                         "s_ochra": ("stone_wall_04", 1.7, [176, 150, 108]), "s_rozowy": ("rustic_stone_wall", 1.52, [184, 140, 120])}.items():
    P[_k] = {"tex": _t, "m": _m, "kol": _c or [round(c * 255) for c in SREDNIE[_t]], "szorst": .95}
# dachy z listy: trzy dachówki i łupek
P["dachowka_03"] = {"tex": "clay_roof_tiles_03", "m": 2.6, "kol": [146, 86, 58], "szorst": .85}
P["dachowka_ceram"] = {"tex": "ceramic_roof_01", "m": 3.5, "kol": [150, 96, 66], "szorst": .85}
P["dachowka_karpiowka"] = {"tex": "roof_tiles", "m": 2.0, "kol": [158, 94, 66], "szorst": .85}
P["lupek"] = {"tex": "grey_roof_01", "m": 8.0, "kol": [82, 84, 86], "szorst": .8}
P["bruk"] = {"tex": "stone_tiles_03", "m": 1.92, "kol": [128, 120, 106], "szorst": 1.0}             # ulice Starówki i jezdnia mostu
# skrzyżowanie Focha × Gdańska × Jagiellońska (22:02, zdjęcia IMG_1983–1992): jasny kamień okładziny, piaskowy tynk Focha 2,
# brązowa miedź hełmu kościoła, szyld klubu w podcieniach
P["s_jasny"] = {"tex": "granite_tile_04", "m": 2.4, "kol": [178, 174, 164], "szorst": .85}          # okładzina z ciosów (Jagiellońska 2)
P["k_piaskowy"] = {"tex": "tynk_kamienica", "m": 2.0, "kol": [178, 166, 142], "szorst": 1.0}
P["kb_piaskowy"] = {"tex": "granite_tile_04", "m": 2.4, "kol": [158, 146, 122], "szorst": .9}       # ciosy podcieni Focha 2
P["miedz_braz"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [104, 80, 62], "szorst": .55}
P["szyld_klub"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [24, 22, 26], "szorst": .6, "nakl": "szyld_klub.png"}
P["balustrada_most"] = {"tex": None, "kol": [30, 32, 32], "szorst": .5, "metal": .5, "alfa": "balustrada_most.png"}
P["beton_most"] = {"tex": "brushed_concrete_04", "m": 2.0, "kol": [184, 182, 176], "szorst": .9}       # belka mostu (m28)
P["witraz"] = {"tex": None, "kol": [46, 52, 58], "szorst": .15, "metal": .3}                         # szkło okien kościoła (ciemne, odbija niebo)
P["kadlub"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [36, 34, 34], "szorst": .6}                 # barka: kadłub stalowy
P["kadlub_pas"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [150, 32, 28], "szorst": .6}
P["kabina_biala"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [226, 224, 216], "szorst": .6}
P["k_szarobez"] = {"tex": "tynk_kamienica", "m": 2.0, "kol": [188, 170, 142], "szorst": 1.0}       # spichrze: beżowy tynk (góra, odnowiona)
P["k_szarobez_dol"] = {"tex": "tynk_kamienica", "m": 2.0, "kol": [146, 128, 104], "szorst": 1.0}   # dół szczytu: ciemniejszy, zabrudzony (IMG_2010)
P["k_wypelnienie"] = {"tex": "tynk_kamienica", "m": 2.0, "kol": [230, 220, 198], "szorst": 1.0}    # wypełnienie muru pruskiego (kremowe)
P["deska_ciemna"] = {"tex": "weathered_brown_planks", "m": 1.6, "kol": [66, 54, 44], "szorst": .85}   # wrota, okiennice spichrzy
P["szyna"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [118, 116, 112], "szorst": .4}                # szyny i przewody trakcji
P["belka_ciemna"] = {"tex": "dark_paneled_wood", "m": 1.4, "kol": [46, 34, 28], "szorst": .8}       # mur pruski spichrzy
P["dachowka_braz"] = {"tex": "clay_roof_tiles_02", "m": 2.0, "kol": [150, 84, 62], "szorst": .8}
P["dachowka_stara"] = {"tex": "clay_roof_tiles_02", "m": 2.0, "kol": [128, 70, 54], "szorst": .85}
P["cegla_komin"] = {"tex": "red_bricks_04", "m": 1.5, "kol": [150, 84, 62], "szorst": .95}
P["blacha_miedz"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [96, 140, 120], "szorst": .6}          # patyna na daszkach wykuszy
P["krawoznik"] = {"tex": "granite_tile", "m": 1.0, "kol": [150, 148, 144], "szorst": .8}
P["latarnia_czarna"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [30, 30, 32], "szorst": .6}
P["klosz"] = {"tex": None, "kol": [240, 230, 200], "szorst": .2}                                    # szkło latarni
P["parasol"] = {"tex": "plotno_parasol", "m": 1.2, "kol": [236, 232, 222], "szorst": .9}
P["donica"] = {"tex": "weathered_brown_planks", "m": 1.0, "kol": [118, 72, 48], "szorst": .85}
P["balustrada_kuta"] = {"tex": None, "kol": [34, 36, 36], "szorst": .5, "metal": .5, "alfa": "balustrada_kuta.png"}
P["meble_ogrodka"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [44, 42, 40], "szorst": .6}
P["rynek_plyty"] = {"tex": "pavement_02", "m": 2.12, "kol": [200, 186, 166], "szorst": .9}          # zdj. 02: jasne płyty (lista)
P["rynek_pas"] = {"tex": "brick_pavement_02", "m": 2.0, "kol": [150, 106, 88], "szorst": .9}       # ukośne pasy ciemniejsze (lista)
P["nabrzeze"] = {"tex": "granite_tile_04", "m": 2.0, "kol": [150, 146, 140], "szorst": .9}
P["mur_nabrzeza"] = {"tex": "granite_tile", "m": 2.3, "kol": [128, 126, 122], "szorst": .8}
P["woda_brda"] = {"tex": None, "kol": [30, 42, 36], "szorst": .14, "metal": .0, "woda": "HarbourWater"}   # w grze: materiał wody portu (fale, odbicia)
P["rama_witryny"] = {"tex": "lakier_blacha", "m": 2.5, "kol": [58, 56, 54], "szorst": .6}   # witryny kamienic: rama grafitowa
P["deska_podbitka"] = {"tex": "weathered_brown_planks", "m": 1.6, "kol": [120, 92, 66], "szorst": .8}   # podbitka okapu kamienic
P["kostka_jezdnia"] = {"tex": "herringbone_pavement_03", "m": 2.4, "kol": [124, 122, 118], "szorst": 1.0}   # IMG_1758–1765
P["kostka_parking"] = {"tex": "red_brick_pavers", "m": 1.8, "kol": [128, 82, 70], "szorst": 1.0}            # zatoki IMG_1760–1761
P["plac_zabaw"] = {"tex": "playground_sand", "m": 2.07, "kol": [196, 170, 128], "szorst": 1.0}

for _k, _v in P.items():
    _v["tint"] = tint(_v)

if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else "."); out.mkdir(parents=True, exist_ok=True)
    (out / "paleta.json").write_text(json.dumps(P, ensure_ascii=False, indent=0), encoding="utf-8")
    (out / "srednie.json").write_text(json.dumps(SREDNIE), encoding="utf-8")          # podgląd: średnie tekstur do odcienia
    print(len(P), "materiałów")
