#!/usr/bin/env python3
"""Download the CC0 Poly Haven furniture and decor of HOTEL SOBCZAK T03.

Owner 9.11 21:03: "dajesz wszedzie wykonczenie Poly Haven ... krzesla maja byc obite
skora lub materialem, wszystko ma byc wykonczone, nic nie jest sztuczne".
Real models replace the box furniture: leather dining chairs, a leather lounge
chair, ottomans, side and coffee tables, pendant lamps, framed oil paintings and photos, vases,
plants, bar stools, a desk lamp, terrace sets, a wall clock and suitcases.

Exact files, byte sizes and MD5 are pinned from api.polyhaven.com/files on 9.11;
a changed provider file fails the build instead of silently changing the look.
Seating and tables 2K, small decor 1K (texel density by real size).
Writes only unity-city/Assets/Source/Mistrzowie/HotelSobczak/<slug>/ (or --dest). No Unity, no APK.
"""
import argparse
from hashlib import md5
import json
from pathlib import Path, PurePosixPath
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/HotelSobczak"
API = "https://api.polyhaven.com/files/"
CDN = "https://dl.polyhaven.org/file/ph-assets/Models/"
USER_AGENT = "BRAXHORN-CITY-Unity6-PolyHaven-CC0-Staging/1.0"

MODELS = {
    'dining_chair_02': ('2k', {
        'dining_chair_02_2k.gltf': (2723, 'a8f25a5452e75fe1246624bb80f2d7f0'),
        'textures/dining_chair_02_arm_2k.jpg': (587889, '4df526356157c87076dd3d6e8e3d2fb5'),
        'dining_chair_02.bin': (544592, 'b7f8a91c0a5249d02de7885f3b242c4e'),
        'textures/dining_chair_02_diff_2k.jpg': (332586, 'be041803606b5bf7869f08ab58a1581f'),
        'textures/dining_chair_02_nor_gl_2k.jpg': (450908, '359ae52672c4c1277ff9f7cc780e1ca0'),
    }),
    'mid_century_lounge_chair': ('2k', {
        'mid_century_lounge_chair_2k.gltf': (2884, '0c51ce1efaf9d2a6b96076fa291990a9'),
        'textures/mid_century_lounge_chair_nor_gl_2k.jpg': (2646290, 'fccce25a26792f315b2aaa548e1e175f'),
        'textures/mid_century_lounge_chair_diff_2k.jpg': (2444450, 'c6b759bcd7a6bbe10b440184943355bd'),
        'textures/mid_century_lounge_chair_arm_2k.jpg': (2476056, 'ab5ac337a69816af00923d7e6615e527'),
        'mid_century_lounge_chair.bin': (171384, 'bded2f717792aefe222dca07633b94ce'),
    }),
    'Ottoman_01': ('2k', {
        'Ottoman_01_2k.gltf': (2656, '58d48c6863957282c8136a761bf2b087'),
        'Ottoman_01.bin': (115200, '7acac7b0744e80e7988c531677d6d57b'),
        'textures/Ottoman_01_diff_2k.jpg': (598798, 'e9a4a79856f4a8116b8e89dbbca96ddd'),
        'textures/Ottoman_01_nor_gl_2k.jpg': (1206308, 'bd85ffc4b909204fe912210c59ed8295'),
        'textures/Ottoman_01_arm_2k.jpg': (594754, 'aaedbe150a868bbab487e0985481d6f1'),
    }),
    'side_table_01': ('1k', {
        'side_table_01_1k.gltf': (2700, '4f72b88ca0cb4329d09d87788df495f3'),
        'side_table_01.bin': (117784, '344e55dbf8dc39b1bca0ca66b956a8dc'),
        'textures/side_table_01_diff_1k.jpg': (183315, '858d3b2c5f7f4c733a5c6a32183b11de'),
        'textures/side_table_01_nor_gl_1k.jpg': (71744, '9252911489f1024faad8bb1443d31d9e'),
        'textures/side_table_01_arm_1k.jpg': (129725, '79435258a51369b735349763689f1cba'),
    }),
    'coffee_table_round_01': ('2k', {
        'coffee_table_round_01_2k.gltf': (2776, 'c43a27b6ef35688f047de334ea8f4736'),
        'textures/coffee_table_round_01_arm_2k.jpg': (2631627, '4a5c3d6f8f923345d108b00a5fa7580d'),
        'textures/coffee_table_round_01_nor_gl_2k.jpg': (1177563, '84c6dcbe5e442b1f629541e94a15a241'),
        'coffee_table_round_01.bin': (131112, 'a664b067fa0ae9b6e0a8fa5878986816'),
        'textures/coffee_table_round_01_diff_2k.jpg': (1721075, 'd4333b0c534f7bce97dead1bd9667efd'),
    }),
    'modern_coffee_table_01': ('2k', {
        'modern_coffee_table_01_2k.gltf': (2771, '5e8490b726b706acffbed17d6174471b'),
        'textures/modern_coffee_table_01_rough_2k.jpg': (1476681, '48093edf08c1f4ddd85f995bcddc90f7'),
        'modern_coffee_table_01.bin': (154864, '6e8f65bff935af308eab9eff062f6daa'),
        'textures/modern_coffee_table_01_diff_2k.jpg': (1571631, '0e4e0c7fa75c2ba6e3f244fafd12190c'),
        'textures/modern_coffee_table_01_nor_gl_2k.jpg': (1495294, '475d7a37185a8f32318896a08188430e'),
    }),
    'modern_ceiling_lamp_01': ('1k', {
        'modern_ceiling_lamp_01_1k.gltf': (6177, 'b85c212ffcc2cb9c74b9e68f314fec3b'),
        'textures/modern_ceiling_lamp_01_arm_1k.jpg': (124022, '6667aa4091a273a041715dccd3b83bc8'),
        'textures/modern_ceiling_lamp_01_diff_1k.jpg': (79705, '7197acc141761c4c417023b9fb58db95'),
        'modern_ceiling_lamp_01.bin': (146828, '48b86e7e6db78e6d3dfd85b765c3ab68'),
        'textures/modern_ceiling_lamp_01_nor_gl_1k.jpg': (79333, 'f3ebd90aa565968ce1faf25a03b1f912'),
    }),
    'fancy_picture_frame_01': ('1k', {
        'fancy_picture_frame_01_1k.gltf': (5059, '40491160e255225326cb1c1146dd5d48'),
        'textures/fancy_picture_frame_01_nor_gl_1k.jpg': (216212, '23f759eb934c191140da30761f832442'),
        'textures/fancy_picture_frame_01_canvas_diff_1k.jpg': (100534, '11a8824c54066ce7f46be6cc2223ff75'),
        'fancy_picture_frame_01.bin': (25948, 'aeaeb6fcb58e4fa3cdd8a084d7757072'),
        'textures/fancy_picture_frame_01_canvas_rough_1k.jpg': (71299, '106564b4147a2aa199214641e21b2da6'),
        'textures/fancy_picture_frame_01_canvas_nor_gl_1k.jpg': (52182, 'ec41bf924450fca040afb0d299ea1294'),
        'textures/fancy_picture_frame_01_rough_1k.jpg': (119837, '66d1fe3bb6963788e049a814b3eb93c0'),
        'textures/fancy_picture_frame_01_diff_1k.jpg': (150435, '8506baa4f1cf9ea2a07e060fb3303ff5'),
    }),
    'hanging_picture_frame_02': ('1k', {
        'hanging_picture_frame_02_1k.gltf': (6433, '4db648e1f6ba5444a53adb31c81c8705'),
        'textures/hanging_picture_frame_02_diff_1k.jpg': (146402, '906c164baad3f02b67244998b7558a9c'),
        'textures/hanging_picture_frame_02_artwork_diff_1k.jpg': (138383, '4cc4caf766c1889ae5d7e8c71acbe19b'),
        'textures/hanging_picture_frame_02_arm_1k.jpg': (113780, '01e09bbe488c7a2953b5a207fdf192ca'),
        'textures/hanging_picture_frame_02_artwork_nor_gl_1k.jpg': (45035, '5682ebd5c7acef6af307013b707affb0'),
        'textures/hanging_picture_frame_02_artwork_roughness_1k.jpg': (74142, 'ffe5f1805a7219ce745c950b71115bec'),
        'hanging_picture_frame_02.bin': (115080, '3e88b7525d7bf9b8f33660100c953cff'),
        'textures/hanging_picture_frame_02_nor_gl_1k.jpg': (56776, 'bc686a415608ae66bd88f70d081fd572'),
    }),
    'ceramic_vase_01': ('1k', {
        'ceramic_vase_01_1k.gltf': (2706, '8f11b5472424578c2c505dee271a0868'),
        'textures/ceramic_vase_01_nor_gl_1k.jpg': (36504, '46081f52a4e9bb58d706477318627a22'),
        'textures/ceramic_vase_01_diff_1k.jpg': (32351, '52337c4fedc46490476d46a8950e1d6b'),
        'textures/ceramic_vase_01_arm_1k.jpg': (74240, '14e56f6d0f810525ddcf9305fa09f495'),
        'ceramic_vase_01.bin': (272656, '73f8a0f911f2d377e3b925b0ae9c7d4c'),
    }),
    'ceramic_vase_02': ('1k', {
        'ceramic_vase_02_1k.gltf': (2716, '2f49a3853a20857a2f0169a764689aa9'),
        'textures/ceramic_vase_02_nor_gl_1k.jpg': (164130, '05e671a686f54f3add772305cf531045'),
        'textures/ceramic_vase_02_diff_1k.jpg': (98894, '767b3412c76a91ddc807d69b5b34df96'),
        'ceramic_vase_02.bin': (293776, '39b375411e2dd303ac86c17cb1ded216'),
        'textures/ceramic_vase_02_rough_1k.jpg': (190645, 'cc2ad94640549ada75e9363d4ce8da5c'),
    }),
    'potted_plant_04': ('1k', {
        'potted_plant_04_1k.gltf': (7081, '077d481764145f6e5e256593aab427bb'),
        'potted_plant_04.bin': (241800, '03774ca8e28e2aa30fb26cb68d5e0093'),
        'textures/potted_plant_04_nor_gl_1k.jpg': (817786, '1c0b87906fd92c7b7ce5508110976474'),
        'textures/potted_plant_04_arm_1k.jpg': (478413, 'ed0030c6085bba45b6f63f3043b0b237'),
        'textures/potted_plant_04_diff_1k.jpg': (577545, '4a72fb3667fd3686ca4014f6676f59c4'),
    }),
    'bar_chair_round_01': ('1k', {
        'bar_chair_round_01_1k.gltf': (2833, '86fccd0ffcf70b942d1b2ff70445c2b9'),
        'textures/bar_chair_round_01_arm_1k.jpg': (751149, '48228bfd8b20bf805fd78b1198d5a67f'),
        'bar_chair_round_01.bin': (1017152, '3b9777a403670ddbde5cda533a5359ae'),
        'textures/bar_chair_round_01_diff_1k.jpg': (599006, 'f2f927c0536c4570fddb3825da824f36'),
        'textures/bar_chair_round_01_nor_gl_1k.jpg': (442193, '6c93ccdb5df8cf731f52be5a4e0317bb'),
    }),
    'outdoor_table_chair_set_01': ('1k', {
        'outdoor_table_chair_set_01_1k.gltf': (6826, 'e5f81fc86b52135f5a5897a51b859b07'),
        'textures/outdoor_table_chair_set_01_table_nor_gl_1k.jpg': (43095, 'd284647b39e4ecda93b3ea85f40dc2d7'),
        'textures/outdoor_table_chair_set_01_chair_arm_1k.jpg': (179920, '52352847c3ce824803a9d500eec5614d'),
        'textures/outdoor_table_chair_set_01_table_arm_1k.jpg': (201642, 'cf5aa95ce8b83991eaf5048eacea0611'),
        'textures/outdoor_table_chair_set_01_table_diff_1k.jpg': (151897, 'afd8871dffcad88170a27b5fce6e3c7d'),
        'outdoor_table_chair_set_01.bin': (456856, '8596ca40855bc417ac5248a54bd85990'),
        'textures/outdoor_table_chair_set_01_chair_nor_gl_1k.jpg': (46772, 'ac08f5c3335c94e1dca986383265c23a'),
        'textures/outdoor_table_chair_set_01_chair_diff_1k.jpg': (155372, 'cb6d4004ad957a6b89aabe4c8dddd6fd'),
    }),
    'wall_clock': ('1k', {
        'wall_clock_1k.gltf': (9008, '86c50202813c717074d1bc72ddad9109'),
        'textures/wall_clock_nor_gl_1k.jpg': (236489, 'd6404c2ffb48b17fbeed2cb9bf187a29'),
        'textures/wall_clock_diff_1k.jpg': (366683, '32e8346296eebf4776e7e917f0080c7f'),
        'textures/wall_clock_arm_1k.jpg': (444044, '6d962d1263bd5b34e16a69fa5473e8ac'),
        'textures/wall_clock_glass_arm_1k.jpg': (376244, '5e84b53fb7e087f4114de28d70787f5c'),
        'wall_clock.bin': (132412, 'f1399287d9ad2fa2cfb303e95bbdbc45'),
    }),
    'desk_lamp_arm_01': ('1k', {
        'desk_lamp_arm_01_1k.gltf': (4453, '29b204485d30e66ee3f183ef45bf4bb3'),
        'textures/desk_lamp_arm_01_nor_gl_1k.jpg': (651826, '5aabbfe920ea6726d5299be7d6224551'),
        'textures/desk_lamp_arm_01_arm_1k.jpg': (780379, '0b887af80f561216baa3da94d001d51a'),
        'textures/desk_lamp_arm_01_diff_1k.jpg': (702522, 'a5c1bf5465da39ca6ca9039dd4e3fd66'),
        'desk_lamp_arm_01.bin': (736804, '29ffa5bb6bee35b066e46073124fee1a'),
    }),
    'vintage_suitcase': ('1k', {
        'vintage_suitcase_1k.gltf': (15481, '08c32e1963eb74610fb291520c8345b4'),
        'textures/vintage_suitcase_01_arm_1k.jpg': (704919, '4db295f083c12d9d98d43c9bc8c76312'),
        'textures/vintage_suitcase_01_nor_gl_1k.jpg': (726302, '7927cc4e0cbaa387a8f1478e47bdcae3'),
        'textures/vintage_suitcase_01_diff_1k.jpg': (457761, '8b9025506413eb45ba15518ebaa06f35'),
        'textures/vintage_suitcase_02_arm_1k.jpg': (703902, 'bb480c03d08335160e00c1ec215134f2'),
        'textures/vintage_suitcase_02_nor_gl_1k.jpg': (727012, '309c88cf867f62b285353d4f087c1c97'),
        'vintage_suitcase.bin': (467856, '0a7a8d06b2c579beabbd36916866a7a9'),
        'textures/vintage_suitcase_02_diff_1k.jpg': (491501, '3793b2ab0206acf8272271468b23c7cd'),
    }),
}


def fetch(url):
    if not (url.startswith(CDN) or url.startswith(API)):
        raise ValueError("Disallowed source: " + url)
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=120) as r:
                return r.read()
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dest", type=Path, default=DEST, help="target folder (preview tools use a scratch folder)")
    dest = parser.parse_args().dest
    total = 0
    for slug, (res, files) in MODELS.items():
        out = dest / slug
        info = json.loads(fetch(API + slug))["gltf"][res]["gltf"]
        urls = {info["url"].rsplit("/", 1)[-1]: info["url"]}
        urls.update({rel: item["url"] for rel, item in info["include"].items()})
        if set(urls) != set(files):
            raise RuntimeError(f"Poly Haven {slug} {res}: file list differs from the 9.11 pin")
        for rel, (size, digest) in files.items():
            if PurePosixPath(rel).is_absolute() or ".." in PurePosixPath(rel).parts:
                raise ValueError("unsafe path " + rel)
            path = out / rel
            blob = path.read_bytes() if path.exists() else None
            if blob is None or len(blob) != size or md5(blob).hexdigest() != digest:
                blob = fetch(urls[rel])
            if len(blob) != size or md5(blob).hexdigest() != digest:
                raise RuntimeError(f"Poly Haven {slug} {rel}: size/MD5 differs from the 9.11 pin")
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(blob)
            total += size
        print(f"BRAX_T03_MODEL {slug} {res} files={len(files)}", flush=True)
    print(f"BRAX_T03_MODELS OK models={len(MODELS)} bytes={total} (Powered by Poly Haven, CC0)")


if __name__ == "__main__":
    main()
