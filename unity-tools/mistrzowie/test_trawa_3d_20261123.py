"""The region's 3D grass (owner 23.11 17:59 "tak samo trawa ... zmienić globalnie", 18:00 "żeby ta trawa nie była płaska",
18:37 "długa, nie jest krótka, nie jest płaska", 18:38 "to jest dobra trawa"): Poly Haven Grass Bermuda 01 cards in crossed
clumps over every lawn, near the camera.

The mask and the clumps run on the real lawn in trawa_sim (the Foundation lawn: no coplanar neighbour blocks it, no clump off
its cell or in a trunk, a piece made twice is the same); here the card maker, the wiring and the numbers that must agree."""
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
import trawa_bermuda  # noqa: E402

SHADER = (RT / "ChampionsTrawaKarta.shader").read_text(encoding="utf-8")
DANE = (RT / "BraxChampionsTrawa3DDane.cs").read_text(encoding="utf-8")
DRAW = (RT / "BraxChampionsTrawa3D.cs").read_text(encoding="utf-8")
MASKA = (ED / "BraxChampionsTrawa3DMaska.cs").read_text(encoding="utf-8")
BUILD = (ED / "BraxChampionsTrawa3DBuild.cs").read_text(encoding="utf-8")
EXPL = (ED / "BraxChampionsExplorer.cs").read_text(encoding="utf-8")
GRASS = (ED / "BraxChampionsGrass.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
SIM = (TOOLS / "trawa_sim/trawa_sim.csproj").read_text(encoding="utf-8")


def shader_default(name):
    return float(re.search(name + r' \("[^"]*", (?:Float|Range\([^)]*\))\) = ([\d.]+)', SHADER).group(1))


def const(text, name):
    return float(re.search(r'\b' + name + r' = ([\d.]+)f', text).group(1))


class Karta(unittest.TestCase):
    def test_from_poly_haven_bermuda_4k_checked(self):
        src = (TOOLS / "trawa_bermuda.py").read_text(encoding="utf-8")
        self.assertEqual(trawa_bermuda.SLUG, "grass_bermuda_01")
        self.assertIn("'https://api.polyhaven.com/files/' + SLUG", src)
        self.assertIn("get('4k', {}).get('jpg')", src)
        self.assertIn("hashlib.md5(b).hexdigest() == item['md5']", src)          # the provider's size and MD5
        self.assertEqual(dict(trawa_bermuda.MAPS), {"Diffuse": "diff", "Alpha": "alpha"})
        self.assertNotRegex(src, r"(?m)^\s*(import|from) scipy")                 # the build runner has none
        self.assertEqual((trawa_bermuda.W, trawa_bermuda.H), (2048, 1024))       # a long card: 2 : 1

    def test_prepared_with_the_region_maps(self):
        self.assertIn("import trawa_bermuda", PREP)
        self.assertIn("card=trawa_bermuda.main(DEST,DEST.parent)", PREP)
        self.assertIn("BraxChampionsTrawaKartaImport.Sciezka", BUILD)
        self.assertIn('Sciezka = "Assets/Source/Mistrzowie/Textures/grass_bermuda_01_karta.png"', BUILD)
        self.assertIn('kartaJson = Source + "trawa-karta.json"', BUILD)

    def test_label_finds_separate_plants(self):
        m = np.zeros((12, 12), bool)
        m[1:4, 1:4] = True; m[6:11, 7] = True; m[8, 1] = True
        lab, n = trawa_bermuda.label(m)
        self.assertEqual(n, 3)
        self.assertEqual(len(np.unique(lab[1:4, 1:4])), 1)
        self.assertNotEqual(lab[2, 2], lab[8, 7])

    def test_fill_leaves_no_dark_fringe(self):
        rgb = np.zeros((16, 16, 3), np.float32); al = np.zeros((16, 16), np.float32)
        rgb[4:8, 4:8] = (60, 120, 30); al[4:8, 4:8] = 1
        out = trawa_bermuda.fill(rgb, al)
        self.assertTrue(np.allclose(out[5, 5], (60, 120, 30), atol=1))          # the plant keeps its colour
        self.assertGreater(out[0, 15].min(), 25)                                  # far from it: its colour, not black
        self.assertAlmostEqual(float(out[0, 15][1] / out[0, 15][0]), 2.0, places=1)

    def test_root_shade_measured_like_the_shader(self):
        self.assertEqual(trawa_bermuda.NASADA, shader_default("_Nasada"))
        self.assertEqual(trawa_bermuda.NASADA_PAS, shader_default("_NasadaPas"))
        self.assertIn("public float nasada = .55f, nasada_pas = 1.6f;", BUILD)
        self.assertIn("float nasada = lerp(_Nasada, 1, saturate(IN.uv_MainTex.y * _NasadaPas));", SHADER)
        self.assertIn("info.linear_nasada", BUILD)                                # the tint uses the shaded mean

    def test_imported_without_thinning_far_away(self):
        self.assertIn("t.mipMapsPreserveCoverage = true; t.alphaTestReferenceValue = .5f;", BUILD)
        self.assertIn("t.alphaIsTransparency = true", BUILD)
        self.assertIn("a.format = TextureImporterFormat.ASTC_6x6", BUILD)
        self.assertIn("t.wrapMode = TextureWrapMode.Clamp", BUILD)


class Shader(unittest.TestCase):
    def test_cut_out_lit_as_the_lawn_and_no_shadow_caster(self):
        self.assertIn('Shader "Braxhorn/ChampionsTrawaKarta"', SHADER)
        self.assertIn("alphatest:_Cutoff", SHADER)
        self.assertIn("noforwardadd", SHADER)                                   # no extra pass for each street lamp
        self.assertIn("Cull Off", SHADER)
        self.assertIn("#pragma multi_compile_instancing", SHADER)
        self.assertIn('"Queue"="AlphaTest"', SHADER)
        self.assertIn("FallBack Off", SHADER)                                     # no opaque quads in the depth / shadows
        self.assertNotIn("addshadow", SHADER)
        self.assertNotIn("o.Normal", SHADER)                                      # the clump's up normal: lit as the lawn

    def test_fades_into_the_ground_without_popping(self):
        self.assertIn("float koniec = lerp(_Zasieg, _Blisko + _Pas, los);", SHADER)
        self.assertIn("float f = saturate((koniec - d) / _Pas);", SHADER)
        self.assertIn("v.vertex.xyz *= f;", SHADER)
        self.assertNotIn("sin(dot(", SHADER)                                      # a stable hash, the same on every phone

    def test_the_lawns_own_tone(self):
        for p in ("_MacroTile", "_MacroMix", "_MacroMean", "_DryMix", "_DryMean", "_DryThr", "_DryGain", "_Stripe", "_StripeMix"):
            self.assertIn(p, SHADER)
        self.assertIn('foreach (var p in new[] { "_MacroTile", "_MacroMix", "_MacroMean", "_DryMix", "_DryMean", "_DryThr", "_DryGain", "_Stripe", "_StripeMix" })', BUILD)
        self.assertIn('m.SetTexture("_LawnTex", grass.GetTexture("_MainTex"));', BUILD)
        self.assertIn("var cel = BraxChampionsGrass.LawnLinear();", BUILD)
        self.assertIn("var cel=LawnLinear();", GRASS)                             # one lawn green for both


class Rozmiary(unittest.TestCase):
    def test_long_grass_the_owner_chose(self):
        self.assertEqual(const(BUILD, "KartaSzer"), .48)
        self.assertEqual(const(BUILD, "KartaWys"), .24)                          # 24 cm (x .85-1.2 x .85-1.2 in height)
        self.assertEqual(const(BUILD, "Krok"), .22)
        self.assertIn("draw.krok = Krok * K;", BUILD)

    def test_reach_agrees_between_the_shader_the_build_and_the_drawer(self):
        self.assertEqual(const(BUILD, "Zasieg"), shader_default("_Zasieg"))
        self.assertEqual(const(BUILD, "Blisko"), shader_default("_Blisko"))
        self.assertEqual(const(BUILD, "Pas"), shader_default("_Pas"))
        self.assertIn('m.SetFloat("_Zasieg", Zasieg); m.SetFloat("_Blisko", Blisko); m.SetFloat("_Pas", Pas);', BUILD)
        self.assertIn("draw.zasieg = Zasieg;", BUILD)

    def test_data_layout(self):
        self.assertIn("public const int Komorek = 32;", DANE)
        self.assertIn("public const int BajtowLatki = Komorek * Komorek / 8;", DANE)
        self.assertIn("for (int cz = 0; cz < N; cz++) for (int j = 0; j < 4; j++) bity.Add((byte)(l.wynik[cz] >> (8 * j)));", MASKA)
        self.assertIn("return (bity[o + (b >> 3)] & (1 << (b & 7))) != 0;", DANE)


class Rysowanie(unittest.TestCase):
    def test_main_camera_only_no_shadows_batches_of_1023(self):
        self.assertIn("[DefaultExecutionOrder(10000)]", DRAW)
        self.assertIn("Graphics.DrawMeshInstanced(siatka, 0, material, arr, arr.Length, mpb, ShadowCastingMode.Off, true, warstwa, cam, LightProbeUsage.Off);", DRAW)
        self.assertIn("int n = Mathf.Min(1023, bufor.Count - p * 1023);", DRAW)

    def test_never_stalls_a_frame(self):
        self.assertIn("if (nowe >= naKlatke) continue;", DRAW)
        self.assertIn("draw.naKlatke = 8;", BUILD)
        self.assertIn("if (dx * dx + dy * dy + dz * dz > zw * zw) { porcje[l] = null; zrobione.RemoveAt(i); }", DRAW)

    def test_holds_30_fps(self):
        self.assertIn("case BraxGrafika.Tryb.Niska: return Mathf.Min(zasieg, 9f);", DRAW)
        self.assertIn("case BraxGrafika.Tryb.Srednia: return Mathf.Min(zasieg, 12f);", DRAW)
        self.assertIn("if (wolno > 2f) { cel = Mathf.Max(zasiegMin, cel - 3f); wolno = 0f; }", DRAW)
        self.assertIn("biezacyZasieg = Mathf.MoveTowards(biezacyZasieg, cel, 1.5f * Mathf.Min(dt, .1f));", DRAW)   # glides, no pop
        self.assertIn("mpb.SetFloat(IdZasieg, biezacyZasieg);", DRAW)             # the shared material is never changed

    def test_clumps_are_always_the_same(self):
        self.assertNotIn("Random", DANE)
        self.assertNotIn("Time.", DANE)
        self.assertIn("wynik.Add(Matrix4x4.TRS(new Vector3(bx + x, y - .01f, bz + z), Quaternion.Euler(tx, yaw, tz), new Vector3(sc, sc * wys, sc)));", DANE)


class Maska(unittest.TestCase):
    def test_lawns_and_what_stands_on_them(self):
        self.assertIn('if (m.shader && m.shader.name == "Braxhorn/ChampionsGrass") return !m.HasProperty("_Blade") || m.GetFloat("_Blade") < .5f;', BUILD)
        self.assertIn('if (zdjecie && m.HasProperty("_MainTex") && m.GetTexture("_MainTex") == zdjecie) return true;', BUILD)
        self.assertIn("if (len < 1e-8f || Mathf.Abs(n.y) / len < .95f) continue;", BUILD)   # the lawn's top only
        self.assertIn("if (s < mats.Length && Trawnik(mats[s], zdjecie)) continue;", BUILD)  # a lawn is no obstacle to itself
        self.assertIn("players.Any(p => r.transform.IsChildOf(p))", BUILD)          # the player walks through it
        self.assertIn("if (ymax <= minY + .005f || ymin >= maxY + wysokosc) return;", MASKA)
        self.assertIn("if (ymin < l.y + wysokosc && ymax > l.y + .005f) { l.blok[cz] |= maska; any = true; }", MASKA)
        self.assertEqual(const(BUILD, "Przeszkody"), .8)

    def test_soil_rings_read_as_the_lawn_shader_reads_them(self):
        self.assertIn('var rect = grass.GetVector("_MaskRect");', BUILD)
        self.assertIn("gleba.GetPixelBilinear((x - rect.x) * rect.z, (z - rect.y) * rect.w).r > ZiemiaOd", BUILD)
        self.assertIn("{ wrapMode = TextureWrapMode.Clamp }", BUILD)

    def test_edge_margin_across_pieces_and_gentle_slopes(self):
        self.assertIn("foreach (var l in lista) if (Math.Abs(l.yq - yq) <= 2) r |= l.trawnik[cz] & ~l.blok[cz];", MASKA)
        self.assertIn("l.wynik[cz] = r & lewy & prawy & dol & gora;", MASKA)

    def test_no_closure_over_out_parameters(self):
        # CS1628 (found by trawa_sim before Unity): the strip's extent is gathered in locals, then returned
        self.assertIn("xmin = lo; xmax = hi;", MASKA)


class Wpiecie(unittest.TestCase):
    def test_built_with_the_exploration_scene_before_it_is_split(self):
        i = EXPL.index("SmallThingsLayers(scene);\n        // 23.11")
        j = EXPL.index("BraxChampionsTrawa3DBuild.Generate(scene, rig.transform);")
        k = EXPL.index("rig.AddComponent<BraxChampionsWydajnosc>().kamera = camera;")
        self.assertLess(i, j); self.assertLess(j, k)

    def test_replaces_the_old_blades(self):
        self.assertIn("foreach (var c in All<BraxClaudeTrawa>(scene).ToList())", BUILD)
        self.assertIn("UnityEngine.Object.DestroyImmediate(c);", BUILD)

    def test_warns_instead_of_failing(self):
        # missing card or lawn: the build goes on with the flat lawn, named in the log
        self.assertIn('BraxBuildQuality.Warn("A1_TRAWA_3D_MISSING', BUILD)
        self.assertIn('BraxBuildQuality.Warn("A1_TRAWA_3D_EMPTY', BUILD)

    def test_simulator_runs_the_real_code(self):
        self.assertIn('Include="../../../unity-city/Assets/Mistrzowie/BraxChampionsTrawa3DDane.cs"', SIM)
        self.assertIn('Include="../../../unity-city/Assets/Editor/Mistrzowie/BraxChampionsTrawa3DMaska.cs"', SIM)

    def test_new_files_have_their_own_guids(self):
        guids = {}
        for f in ("Mistrzowie/ChampionsTrawaKarta.shader", "Mistrzowie/BraxChampionsTrawa3D.cs", "Mistrzowie/BraxChampionsTrawa3DDane.cs",
                  "Editor/Mistrzowie/BraxChampionsTrawa3DBuild.cs", "Editor/Mistrzowie/BraxChampionsTrawa3DMaska.cs"):
            meta = (ROOT / "unity-city/Assets" / (f + ".meta")).read_text(encoding="utf-8")
            g = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
            self.assertNotIn(g, guids); guids[g] = f


if __name__ == "__main__":
    unittest.main()
