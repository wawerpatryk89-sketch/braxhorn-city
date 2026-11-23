"""The beach and the sea at it (owner 23.11 19:12, the GTA beach pictures: "chcę taki piasek i fale na morzu i woda jak
prawdziwa"; 19:27 "fale niech się wylewają na plażę jak normalnie", "te ścieżki usuń z plaży całkowicie"; 19:36 "ten piasek
tu lepiej wyglądał ... taki nierówny").

The shore field runs on the real beach in morze_sim (one waterline along the whole beach, the distance only grows out to
sea, the bed never steps up toward the sea, the run-up's reach); here the sand maker, the noise, the wiring and the numbers
that must agree between the editor, the shaders and the preview."""
import re
import sys
import unittest
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RT = ROOT / "unity-city/Assets/Mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
sys.path.insert(0, str(TOOLS))
import morze_plaza  # noqa: E402

MORZE = (RT / "ChampionsMorze.shader").read_text(encoding="utf-8")
PIASEK = (RT / "ChampionsPiasek.shader").read_text(encoding="utf-8")
PLAZA = (ED / "BraxChampionsPlaza.cs").read_text(encoding="utf-8")
BRZEG = (ED / "BraxChampionsMorzeBrzeg.cs").read_text(encoding="utf-8")
FUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
HOR = (ED / "BraxChampionsHorizon.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
SIM = (TOOLS / "morze_sim/morze_sim.csproj").read_text(encoding="utf-8")


def const(text, name):
    return float(re.search(r'\b' + name + r' = ([\d.]+)f', text).group(1))


class Piasek(unittest.TestCase):
    def test_poly_haven_aerial_sand_without_its_tyre_tracks(self):
        src = (TOOLS / "morze_plaza.py").read_text(encoding="utf-8")
        self.assertEqual(morze_plaza.SAND, "aerial_sand")
        self.assertIn("hashlib.md5(b).hexdigest() == item['md5']", src)            # the provider's size and MD5
        self.assertEqual(dict(morze_plaza.SAND_MAPS), {"Diffuse": "diff", "nor_gl": "nor_gl"})
        self.assertNotRegex(src, r"(?m)^\s*(import|from) scipy")
        # the track-free areas: inside the 2048 map and each big enough for a patch
        for x0, y0, x1, y1 in morze_plaza.CZYSTE:
            self.assertTrue(0 <= x0 < x1 <= 2048 and 0 <= y0 < y1 <= 2048)
            self.assertGreaterEqual(min(x1 - x0, y1 - y0), morze_plaza.PATCH)
        self.assertEqual(2048 % morze_plaza.STEP, 0)                                # a whole number of patches: it tiles

    def test_quilt_tiles_and_keeps_the_grain(self):
        rng = np.random.default_rng(1)
        src = rng.random((2048, 2048, 1)).astype(np.float32)
        out, = morze_plaza.zszyj([src], 512, 192, 128)
        self.assertEqual(out.shape, (512, 512, 1))
        self.assertAlmostEqual(float(out.mean()), float(src.mean()), delta=.02)
        self.assertGreater(float(out.std()), .8 * float(src.std()))                  # the blend keeps the contrast
        # it tiles: the jump across the wrap is no bigger than inside
        inside = np.abs(np.diff(out[:, :, 0], axis=1)).mean(); wrap = np.abs(out[:, 0, 0] - out[:, -1, 0]).mean()
        self.assertLess(wrap, inside * 1.5)

    def test_only_from_the_clean_areas(self):
        src = (TOOLS / "morze_plaza.py").read_text(encoding="utf-8")
        self.assertIn("x0, y0, x1, y1 = rects[rng.integers(len(rects))]", src)
        self.assertIn("sx, sy = rng.integers(x0, x1 - patch + 1), rng.integers(y0, y1 - patch + 1)", src)   # a patch fits inside
        self.assertIn("rects = [tuple(int(v * k) for v in r) for r in CZYSTE]", src)                       # 2048 -> the map's size

    def test_prepared_with_the_region_maps(self):
        self.assertIn("import morze_plaza", PREP)
        self.assertIn("szum,piasek=morze_plaza.main(DEST,DEST.parent)", PREP)
        self.assertIn("'damp_beach_sand',", PREP)
        self.assertNotIn("'aerial_beach_01'", PREP)
        self.assertIn('PiasekDiff = "Assets/Source/Mistrzowie/Textures/plaza_piasek_diff_4k.jpg"', PLAZA)
        self.assertIn('PiasekNor = "Assets/Source/Mistrzowie/Textures/plaza_piasek_nor_4k.jpg"', PLAZA)

    def test_one_sand_for_the_dry_and_the_wet_beach(self):
        self.assertIn('materials["sand_dry"]=BraxChampionsPlaza.Piasek(materials["sand_dry"],seaLevel);', FUND)
        self.assertIn('materials["sand_wet"]=materials["sand_dry"];', FUND)
        self.assertIn("m.SetVector(\"_Mokro\", new Vector4(Wbieganie + .02f, Wbieganie + .20f, 0, 0));", PLAZA)   # wet up to the run-up
        self.assertIn("m.SetVector(\"_Kafle\", new Vector4(15f * K, 11f * K, 0f, 2f * K));", PLAZA)               # Aerial Sand is 15 m
        self.assertIn("float3 wetAlb = dry * float3(.60, .61, .65)", PIASEK)                                     # footprints stay when wet
        self.assertIn('Shader "Braxhorn/ChampionsPiasek"', PIASEK)


class Szum(unittest.TestCase):
    def test_tileable_noise(self):
        f1, f2 = morze_plaza.worley(6, 3)
        self.assertLess(abs(float(f1[:, 0].mean() - f1[:, -1].mean())), .05)        # periodic
        g = morze_plaza.gladki(3, .02)
        self.assertAlmostEqual(float(g.min()), 0.0); self.assertAlmostEqual(float(g.max()), 1.0)
        self.assertLess(float(np.abs(g[:, 0] - g[:, -1]).mean()), float(np.abs(np.diff(g, axis=1)).mean()) * 3)

    def test_imported_linear_and_crisp(self):
        self.assertIn('SzumPath = "Assets/Source/Mistrzowie/Textures/morze_szum.png"', PLAZA)
        self.assertIn("t.sRGBTexture = false;", PLAZA)
        self.assertIn("a.format = TextureImporterFormat.RGB24;", PLAZA)


class Morze(unittest.TestCase):
    def test_the_field_and_the_shallows(self):
        self.assertIn("var tex = new Texture2D(pole.w, pole.h, TextureFormat.RGHalf, false, true)", PLAZA)
        self.assertIn("wrapMode = TextureWrapMode.Clamp", PLAZA)
        self.assertIn('materials["sea"]=BraxChampionsPlaza.Morze(d.meshes,seaLevel);', FUND)
        self.assertIn("var shallow=Save(BraxChampionsPlaza.Plycizna(mesh,seaLevel)", FUND)     # the water the run-up climbs with
        self.assertIn("sr.sharedMaterial=materials[\"sea\"];sr.shadowCastingMode=ShadowCastingMode.Off;", FUND)
        self.assertIn('if(m.role=="foam"){foamStrips++;continue;}', FUND)                       # the static strip is replaced
        self.assertIn("for (int i = 0; i < v.Length; i++) v[i].y = sea;", PLAZA)

    def test_wave_numbers_agree(self):
        f = re.search(r'_Fala \("[^"]*", Vector\) = \(([\d.]+),([\d.]+),([\d.]+),([\d.]+)\)', MORZE).groups()
        self.assertEqual([float(x) for x in f], [const(PLAZA, "Dlugosc"), const(PLAZA, "Okres"), const(PLAZA, "Wbieganie"), const(PLAZA, "ZasiegWbiegania")])
        self.assertEqual(const(PLAZA, "Wbieganie"), .27)                                        # ~10 units over the 2.25 % beach
        self.assertIn('m.SetVector("_Fala", new Vector4(Dlugosc, Okres, Wbieganie, ZasiegWbiegania));', PLAZA)
        self.assertIn("public const float Spadek = .0225f, Glebienie = .00035f;", BRZEG)

    def test_runs_up_and_drains(self):
        self.assertIn("float run = smoothstep(0, .10, p) * (1 - smoothstep(.10, .80, p));", MORZE)
        self.assertIn("w.y += _Fala.z * env * run + swell;", MORZE)
        self.assertIn("float front = (1 - smoothstep(0, lipW, depth)) * smoothstep(.15, .5, i.run)", MORZE)

    def test_clear_water_shows_the_sand(self):
        self.assertIn("float depth = max(i.world.y - f.y, 0);", MORZE)                          # the bed from the field
        self.assertIn("float3 water = lerp(bed * T, sc, murk);", MORZE)
        self.assertIn("float3 T = exp(-depth * _Pochlanianie.rgb);", MORZE)

    def test_no_flicker(self):
        self.assertIn("float3 filt = 1 - smoothstep(1, 2.8, fwidth(ph));", MORZE)               # ripples finer than a pixel gone
        self.assertIn("tex2Dlod(_Szum, float4(w.x / 70 + idx * .37", MORZE)                      # no mip seam at the crests
        self.assertIn("#pragma multi_compile_fog", MORZE)
        self.assertIn('"RenderType"="Opaque"', MORZE)                                          # opaque: no sorting, no z-fight look

    def test_the_far_sea_and_the_pool(self):
        self.assertIn('LoadAssetAtPath<Material>(Out + "/Materials/morze.mat")', HOR)
        self.assertIn('if (!sea) sea = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/sea.mat");', HOR)
        self.assertIn('materials["sea"]=SeaMaterial();', FUND)                                  # sea.mat stays for the rooftop pool
        self.assertIn("f.x += max(out2.x, out2.y);", MORZE)                                    # beyond the field: deep

    def test_simulator_runs_the_real_code(self):
        self.assertIn('Include="../../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsMorzeBrzeg.cs"', SIM)
        self.assertIn("new BraxChampionsMorzeBrzeg(sx0 - 8f, sz0 - 8f, sx1 + 8f, sz1 + 40f, 1024, 512, sea)", (TOOLS / "morze_sim/Program.cs").read_text(encoding="utf-8"))
        self.assertIn("new BraxChampionsMorzeBrzeg(sx0 - 8f, sz0 - 8f, sx1 + 8f, sz1 + 40f, PoleW, PoleH, sea)", PLAZA)
        self.assertIn("public const int PoleW = 1024, PoleH = 512;", PLAZA)

    def test_new_files_have_their_own_guids(self):
        seen = set()
        for f in ("Mistrzowie/ChampionsMorze.shader", "Mistrzowie/ChampionsPiasek.shader", "Editor/Mistrzowie/BraxChampionsMorzeBrzeg.cs",
                  "Editor/Mistrzowie/BraxChampionsPlaza.cs"):
            g = re.search(r"guid: ([0-9a-f]{32})", (ROOT / "unity-city/Assets" / (f + ".meta")).read_text(encoding="utf-8")).group(1)
            self.assertNotIn(g, seen); seen.add(g)


if __name__ == "__main__":
    unittest.main()
