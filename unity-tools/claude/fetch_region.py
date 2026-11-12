#!/usr/bin/env python3
"""Region R1 „na gotowo”: dodatkowe tekstury i modele CC0 z Poly Haven (uzupełnienie fetch_polyhaven.py).

Każdy wpis jest OPCJONALNY: brak w katalogu lub błąd pobierania nie przerywa buildu, tylko trafia
do raportu (kod C# ma zamienniki). Rośliny i drzewa w 2K (decyzja właściciela: bez odchudzania),
reszta w 1K (telefon). Wynik: unity-city/Assets/Source/Claude/PolyHaven/{textures,models}/<id>/
"""
import json
import pathlib
import sys
import time
import urllib.request

UA = {"User-Agent": "BraxhornCity-UnityBuild/1.0 (github.com/wawerpatryk89-sketch/braxhorn-city)"}
ROOT = pathlib.Path("unity-city/Assets/Source/Claude/PolyHaven")

TEXTURES = [
    # pawilon sportowy
    "exterior_wall_cladding_02", "wood_plank_wall", "concrete_floor_02", "white_plaster_02",
    "floor_tiles_06", "square_tiles", "rubber_tiles", "leather_white", "dark_wood",
    "painted_metal_shutter", "metal_plate", "wooden_planks",
    # taras, plaża, wnętrza
    "brown_mud_leaves_01", "wood_planks_grey", "plank_flooring_02", "wood_floor", "granite_tile_02",
    # front klubu NOVA, siłownia
    "leather_red_02", "leather_red_03", "tatami_mat", "concrete_wall_008",
    # plaża: parasole (płótno), leżaki (drewno), ręczniki
    "rough_linen", "terry_cloth", "gingham_check", "teak_veneer", "fabric_pattern_05",
    # mural w sklepie: cegła pod farbą
    "brick_wall_001", "red_brick_03",
    # tyły siłowni: wzgórze za wiaduktem
    "rock_wall_08", "rock_face_03",
    # biedne mieszkanie Braxa nad NOVA: łuszcząca się farba, stara tapeta, linoleum, brudne kafle, spękany tynk, chodnik,
    # rdzawe schody zewnętrzne, zniszczone szafki kuchenne
    "peeling_painted_wall", "decrepit_wallpaper", "old_linoleum_flooring_01", "dirty_tiles", "worn_cracked_plaster", "dirty_carpet",
    "rusty_painted_metal", "wood_cabinet_worn_long",
    # wieżowce nowych dzielnic z kodu (10.11): biały tynk, wapień, piaskowiec, okładzina panelowa, granit, czysta cegła
    "white_stucco", "marble_01", "sandstone_blocks_08", "exterior_wall_cladding_03", "granite_tile", "granite_tile_03",
]
# DWORZEC R1 (11.11, właściciel 15:53: „Poly Haven, tylko i wyłącznie Poly Haven. Do wszystkiego, materiałów, obić”): plac
# i perony z granitu, chodniki, asfalt, tłuczeń, podkłady, pas z wypustkami, okleina dębowa, trawa pasa lasu (lista =
# BraxMiastoDworzec.Materialy; blue_metal_plate, metal_plate_02, corrugated_iron, brown_leather są też w liście faweli).
TEXTURES_DWORZEC = [
    "granite_tile_04", "concrete_pavement_02", "clean_asphalt", "gravel_stones", "concrete_floor_01", "anti_skid_tiles",
    "oak_veneer_01", "leafy_grass", "blue_metal_plate", "metal_plate_02", "corrugated_iron", "brown_leather",
    "floor_tiles_02",   # płytki ścienne WC dworca (marmur jasny)
]
# FAWELA (9.11, właściciel: „z Poly Haven wykończenia, tekstury wszędzie”): stare tynki, cegła, pustak, beton, blacha falista,
# rdzawe drzwi i rolety, okiennice, asfalt, ziemia, płótno, skóra — lista = Materialy w BraxRegionFawela.cs (TylkoFawela).
# W 1K (telefon): 38 zestawów w 2K to ok. 0,5 GB tekstur w pamięci naraz — ściany dostają kolor + normalną 1024, drobne
# wykończenia sam kolor 512 (BraxRegionFawela.Odchudz).
TEXTURES_FAWELA_1K = [
    "brown_mud_dry", "stony_dirt_path", "painted_plaster_wall", "plastered_wall", "damaged_plaster", "red_brick_plaster_patch_02",
    "white_rough_plaster", "rough_plaster_broken", "red_plaster_weathered", "blue_plaster_weathered", "rebar_reinforced_concrete", "red_bricks_02",
    "slumped_mortar_brick", "concrete_block_wall", "concrete_layers", "rusty_corrugated_iron", "rusty_metal_03", "corrugated_iron_02",
    "corrugated_iron", "wood_peeling_paint_weathered", "old_planks_02", "rusty_metal_04", "green_metal_rust", "blue_metal_plate",
    "rusty_metal_shutter", "rusted_shutter", "wooden_garage_door", "wood_shutter", "metal_plate_02", "asphalt_03",
    "asphalt_04", "concrete_floor_worn_001", "damaged_concrete_floor", "wood_table_worn", "cotton_jersey", "brown_leather",
    "fabric_leather_02", "fabric_leather_01", "weathered_brown_planks",
]
MODELS_2K = [  # drzewa i krzewy: pełna jakość (2K); reszta roślin w 1K, bo edytor Unity na GitHubie ma 16 GB
    "shrub_01", "shrub_02", "shrub_03", "shrub_04",   # shrub_04: donice placu dworca i podszyt lasu (11.11)
    # island_tree_03 / pine_tree_01 / fir_tree_01: skany 2–17 mln trójkątów — w grze drzewo_verdant (Library)
]
MODELS_1K_PLANTS = [
    "grass_medium_01", "grass_medium_02",
    "flower_gazania", "flower_ursinia", "periwinkle_plant", "celandine_01",
    "anthurium_botany_01", "calathea_orbifolia_01", "potted_plant_01", "fern_02", "pachira_aquatica_01",
    "tree_stump_01", "tree_stump_02",   # las między miastem a fawelą (11.11): pniaki w podszycie
]
MODELS_1K = [  # mała architektura, wyposażenie, kamienie
    "rock_moss_set_01", "boulder_01", "stone_01",
    "coast_rocks_01", "coast_rocks_03", "sand_rocks_small_01",
    "planter_box_02", "planter_box_03", "planter_pot_clay",
    "painted_wooden_bench", "wooden_picnic_table", "street_lamp_01", "street_lamp_02",
    "metal_trash_can", "fire_hydrant", "lifebuoy", "ocean_buoy", "lateral_sea_marker",
    "wooden_crate_01", "wooden_crate_02", "wooden_barrels_01",
    "steel_frame_shelves_01", "steel_frame_shelves_02", "worn_metal_rack",
    "metal_stool_01", "metal_stool_02", "wooden_stool_01",
    "mounted_fluorescent_lights", "modern_ceiling_lamp_01", "korean_fire_extinguisher_01", "WetFloorSign_01",
    "wall_clock", "dartboard", "football", "dirty_football", "standing_chalkboard_01",
    "coffee_table_round_01", "modern_coffee_table_01", "industrial_coffee_table",
    "modern_arm_chair_01", "ArmChair_01", "mid_century_lounge_chair",
    "hanging_picture_frame_01", "hanging_picture_frame_02", "hanging_picture_frame_03",
    # MUZEUM w zabytku KINNAIRD (11.11, właściciel 20:06: „muzeum… ta Poly Haven pod to”) — eksponaty w gablotach (wpisy opcjonalne)
    # (wazy, serwis, talerz, telewizor, radio, okulary, lupa, zegar, kasa — już w liście niżej / wyżej)
    "marble_bust_01", "horse_statue_01", "antique_ceramic_vase_01",   # bez mosiężnych waz (zasada 10.11 — bez złota)
    # 12.11 00:53 (właściciel: „jak coś możesz ściągnąć z Polyheaven zabytkowego, to ściągnij do tej muzeum”) — zabytki CC0 bez złota
    # i mosiądzu (bez zegarków kieszonkowych, mikroskopu, kompasu, skrzyni ze złotem, złotych ram, mosiężnego żyrandola i lampy) i bez najcięższych siatek (książki, szachy, galeony)
    "dutch_ship_medium", "Lantern_01", "wooden_lantern_01",
    "gothic_statue", "bronze_whale_statue", "bronze_shark_statue", "horse_head", "bull_head",
    "antique_estoc", "antique_katana_01", "ornate_medieval_dagger", "ornate_medieval_mace", "ornate_war_hammer", "kite_shield",
    "Camera_01", "filmstrip_projector_8mm", "vintage_video_camera", "old_gas_mask", "binoculars", "vintage_radio_transceiver", "vintage_telephone_wall_clock",
    "GothicCabinet_01", "ClassicConsole_01", "mantel_clock_01", "ornate_mirror_01", "spinning_wheel_01",
    "industrial_wall_lamp", "industrial_wall_sconce", "security_camera_01",
    # tyły siłowni: biura w wieżowcu A, warsztaty pod wiaduktem, wzgórze
    "rollershutter_door", "metal_office_desk", "classic_laptop", "desk_lamp_arm_01", "office_notepads", "stationery_supplies",
    "SchoolChair_01", "modern_wooden_cabinet", "drawer_cabinet", "Sofa_01", "sofa_02", "dining_table", "WoodenTable_01", "WoodenTable_02",
    "tool_cart", "metal_tool_chest", "hand_truck", "old_tyre", "rusted_wheel_rim_01", "bench_vice_01", "barrel_03", "Barrel_02", "coast_land_rocks_02",
    # biedne mieszkanie Braxa nad NOVA (właściciel 31.10, 21:09–21:10): stare łóżko, telewizor kineskopowy, kuchenka, mikrofala,
    # czajnik, szafa, regał, krzesło, stół, budzik, kartony, skrzynka, konserwy, goła żarówka, radio
    "old_bed_frame", "Television_01", "electric_stove", "vintage_microwave", "vintage_electric_kettle", "painted_wooden_cabinet_02",
    "Shelf_01", "WoodenTable_03", "alarm_clock_01", "cardboard_box_01", "plastic_crate_01", "russian_food_cans_01", "lightbulb_01", "boombox",
    # 3.11 bosmanat — okienko z przekąskami (sprawdzone w API Poly Haven 3.11: hot-doga nie ma; są rogaliki, bułki, owoce)
    "croissant", "hamburger_buns", "food_apple_01", "lemon", "food_pears_asian_01", "carved_wooden_plate", "wooden_cutting_board",
    # 3.11 13:27 towar w sklepach ulicy (BraxSklepyTowar; lista z katalogu Poly Haven 3.11): piekarnia, cukiernia, warzywniak, delikatesy, kawiarnia, bary
    "bananas", "carrot_cake", "strawberry_chocolate_cake", "long_life_food", "wine_bottles_01", "CheeseBox_01", "multi_cleaner_bottle",
    "bleach_bottle", "wicker_basket_01", "wicker_basket_02", "tea_set_01", "jug_01", "wooden_bowl_01", "ceramic_vase_01",
    # FAWELA (9.11, właściciel: „wszystko, co się przydaje, z biblioteki — żeby wyglądało jak najbardziej realnie”): klimatyzatory,
    # krzesła z tworzywa, worki ze śmieciami, butle gazowe, baniaki, skrzynki, worki cementu, drabiny, kasa sklepowa, fotel
    # fryzjerski, pompka, tarcza do rzutek; stara sala bokserska: wentylatory, lampy przemysłowe, ławki, zegar, gaśnica, młot, lampa
    "exterior_aircon_unit", "plastic_monobloc_chair_01", "trashbag", "propane_tank", "small_lpg_tank", "plastic_crate_02",
    "plastic_jerrycan", "plastic_bottle_gallon", "metal_jerrycan", "cement_bag", "wooden_ladder", "folding_wooden_stool",
    "CashRegister_01", "BarberShopChair_01", "tire_pump", "medical_box", "ceiling_fan", "hanging_industrial_lamp", "security_light",
    "sledgehammer_01",
    # 10.11 sklepy pełne (MIASTO R1): fawela — naprawa telefonów (stare RTV), wszystko dla domu (chemia, garnki, konewki, szufelki);
    # ulica miasta — kwiaciarnia (wazony, mała roślina w doniczce), optyk (okulary, lupa), warsztat rowerowy (narzędzia, smary),
    # hotel (zegar stojący, walizki); rośliny w doniczkach, skrzynki z owocami, pieczywo — w bibliotece (skrzynki_owocow.py)
    "television_02", "gamepad", "all_purpose_cleaner", "drain_cleaner", "multi_cleaner_5_litre", "pot_enamel_01", "watering_can_metal_01",
    "dustpan", "metal_jug", "spray_paint_bottles", "potted_plant_04", "ceramic_vase_02", "ceramic_vase_03", "ceramic_vase_04",
    "round_spectacles", "magnifying_glass_01", "metal_toolbox", "lubricant_spray", "small_oil_can_01", "combination_wrench", "pipe_wrench",
    "ratchet_wrench", "vintage_suitcase", "vintage_grandfather_clock_01",
]


def get(url: str) -> bytes:
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=180) as r:
                return r.read()
        except Exception as exc:  # noqa: BLE001
            if attempt == 3:
                raise
            print(f"retry {attempt + 1} {url}: {exc}")
            time.sleep(3 * (attempt + 1))
    raise RuntimeError(url)


def files(asset: str) -> dict:
    return json.loads(get(f"https://api.polyhaven.com/files/{asset}"))


def pick(res: dict, *keys):
    for k in keys:
        if k in res:
            return res[k]
    raise KeyError(f"brak rozdzielczości {keys} w {list(res)}")


def texture(slug: str, report: dict, res=("2k", "1k")) -> None:
    d = ROOT / "textures" / slug
    if (d / "diffuse.jpg").exists() and (d / "normal.jpg").exists() and (d / "arm.jpg").exists():
        report["textures"][slug] = "already"
        return
    f = files(slug)
    d.mkdir(parents=True, exist_ok=True)
    for key, out in (("Diffuse", "diffuse.jpg"), ("nor_gl", "normal.jpg"), ("arm", "arm.jpg")):
        src = f.get(key)
        if src is None and key == "Diffuse":
            src = f.get("diff") or f.get("Color")
        if src is None:
            raise KeyError(f"{slug}: brak mapy {key} ({list(f)})")
        url = pick(src, *res)["jpg"]["url"]
        (d / out).write_bytes(get(url))
    report["textures"][slug] = "ok"
    print("TEX", slug, "OK")


def model(mid: str, res: str, report: dict) -> None:
    d = ROOT / "models" / mid
    if (d / f"{mid}.gltf").exists() or (d / f"{mid}.glb").exists():
        report["models"][mid] = "already"
        return
    f = files(mid)
    g = pick(f["gltf"], res, "1k", "2k")["gltf"]
    d.mkdir(parents=True, exist_ok=True)
    (d / f"{mid}.gltf").write_bytes(get(g["url"]))
    n = 1
    for rel, info in (g.get("include") or {}).items():
        p = d / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(get(info["url"]))
        n += 1
    report["models"][mid] = f"ok ({res}, {n} plików)"
    print("MODEL", mid, res, "files", n)


def main() -> int:
    report = {"source": "Poly Haven (CC0), wpisy opcjonalne", "textures": {}, "models": {}, "missing": []}
    for t in TEXTURES + [x for x in TEXTURES_DWORZEC if x not in TEXTURES]:
        try:
            texture(t, report)
        except Exception as exc:  # noqa: BLE001
            report["missing"].append(f"tex:{t}: {exc}")
            print("BRAK TEKSTURY", t, exc)
    for t in TEXTURES_FAWELA_1K:
        try:
            texture(t, report, ("1k", "2k"))
        except Exception as exc:  # noqa: BLE001
            report["missing"].append(f"tex:{t}: {exc}")
            print("BRAK TEKSTURY", t, exc)
    for m in MODELS_2K:
        try:
            model(m, "2k", report)
        except Exception as exc:  # noqa: BLE001
            report["missing"].append(f"model:{m}: {exc}")
            print("BRAK MODELU", m, exc)
    for m in MODELS_1K_PLANTS + MODELS_1K:
        try:
            model(m, "1k", report)
        except Exception as exc:  # noqa: BLE001
            report["missing"].append(f"model:{m}: {exc}")
            print("BRAK MODELU", m, exc)
    out = ROOT / "region-fetch.json"
    out.write_text(json.dumps(report, indent=2, ensure_ascii=False))
    print("REGION FETCH:", len(report["textures"]), "tekstur,", len(report["models"]), "modeli, brakuje:", len(report["missing"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
