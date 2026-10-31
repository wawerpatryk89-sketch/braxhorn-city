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
]
MODELS_2K = [  # drzewa i krzewy: pełna jakość (2K); reszta roślin w 1K, bo edytor Unity na GitHubie ma 16 GB
    "shrub_01", "shrub_02", "shrub_03",
    # island_tree_03 / pine_tree_01 / fir_tree_01: skany 2–17 mln trójkątów — w grze drzewo_verdant (Library)
]
MODELS_1K_PLANTS = [
    "grass_medium_01", "grass_medium_02",
    "flower_gazania", "flower_ursinia", "periwinkle_plant", "celandine_01",
    "anthurium_botany_01", "calathea_orbifolia_01", "potted_plant_01", "fern_02", "pachira_aquatica_01",
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
    "industrial_wall_lamp", "industrial_wall_sconce", "security_camera_01",
    # tyły siłowni: biura w wieżowcu A, warsztaty pod wiaduktem, wzgórze
    "rollershutter_door", "metal_office_desk", "classic_laptop", "desk_lamp_arm_01", "office_notepads", "stationery_supplies",
    "SchoolChair_01", "modern_wooden_cabinet", "drawer_cabinet", "Sofa_01", "sofa_02", "dining_table", "WoodenTable_01", "WoodenTable_02",
    "tool_cart", "metal_tool_chest", "hand_truck", "old_tyre", "rusted_wheel_rim_01", "barrel_03", "Barrel_02", "coast_land_rocks_02",
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


def texture(slug: str, report: dict) -> None:
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
        url = pick(src, "2k", "1k")["jpg"]["url"]
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
    for t in TEXTURES:
        try:
            texture(t, report)
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
