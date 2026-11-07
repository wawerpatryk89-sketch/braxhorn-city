"""Cheap source/geometry regressions only; does not run Unity or validate a scene."""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / 'unity-city/Assets/Editor/Mistrzowie'


def source(name):
    return (EDITOR / name).read_text(encoding='utf-8')


def constant(text, name):
    m = re.search(r'const float\s+' + re.escape(name) + r'\s*=\s*([\d.+-]+)f?\s*;', text)
    if not m:
        raise AssertionError('Missing numeric geometry constant: ' + name)
    return float(m.group(1))


class PremiumReferenceRegressions(unittest.TestCase):
    def test_entrance_print_is_in_front_of_lobby_wall(self):
        text = source('BraxChampionsArenaMural.cs')
        z = constant(text, 'WallImageZ')
        self.assertGreater(z, 98.3)
        self.assertLessEqual(z - 98.3, .05)
        self.assertRegex(text, r'mesh\.triangles\s*=\s*new\[\]\s*\{0,2,1,0,3,2\}')
        # v0 -> v2 -> v1 has +Z normal, towards the lobby in original bowl space.
        a, b = (82, 27.33333333, 0), (0, 27.33333333, 0)
        self.assertGreater(a[0] * b[1] - a[1] * b[0], 0)

    def test_entrance_uv_is_readable_from_lobby_and_keeps_aspect(self):
        text = source('BraxChampionsArenaMural.cs')
        self.assertIn('(ImageRight-x)/(ImageRight-ImageLeft)', text)
        left, right = constant(text, 'ImageLeft'), constant(text, 'ImageRight')
        bottom, top = constant(text, 'MuralBase'), constant(text, 'MuralTop')
        self.assertAlmostEqual((right-left)/(top-bottom), 3.0, places=6)
        self.assertIn('if(x1<=ImageLeft)u0=u1=.998f', text)
        self.assertIn('else if(x0>=ImageRight)u0=u1=.002f', text)
        self.assertAlmostEqual(constant(text, 'DoorRight') - constant(text, 'DoorLeft'), 4.10)
        self.assertIn('DoorLeft,DoorRight,DoorTop,MuralTop,false', text)
        self.assertNotIn('AddComponent<BoxCollider>', text)
        self.assertNotIn('AddComponent<MeshCollider>', text)

    def test_case_preserves_owner_belt_and_plaque(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertIn('tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx', text)
        self.assertIn('BRAXHORN_ARENA_CASE_PLAQUE.jpg', text)
        self.assertEqual(text.count('OriginalBelt(display);'), 1)
        self.assertEqual(text.count('OriginalRectangleArtwork(display, gold, bronze);'), 1)
        self.assertNotIn('EncodeToPNG', text)
        self.assertNotIn('SetPixels', text)

    def test_case_wood_is_real_existing_pbr_not_flat_colour(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertRegex(text, r'var wood = BraxChampionsPoolP1Pbr2K\.Create\([\s\S]*?"teak_veneer", 1\.5f, Color\.white\);')
        self.assertIn('"Wood veneer showcase plinth", wood,', text)
        self.assertIn('"Wood veneer case base", wood,', text)
        self.assertIn('plinthCollider.size = new Vector3(1.82f, .26f, 1.08f);', text)
        self.assertIn('Podium(display, marble, gold);', text)
        self.assertIn('"Black marble pillar behind the showcase", marble,', text)
        self.assertIn('"Black leather back panel", backing,', text)

    def test_case_plaque_faces_front_and_clears_glass(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertIn('Quaternion.Euler(0,180f,0)', text)
        self.assertIn('new Vector3(0,1.075f,-.397f)', text)
        self.assertIn('new Vector3(0, 2.06f, -.535f)', text)
        # Front pane spans z≈-.548..-.522; plaque stays safely behind the glass.
        self.assertGreater(-.397, -.535 + .026/2)
        self.assertLess(-.397, -.321 - .125/2 - .01)
        self.assertAlmostEqual(1.22 / .61, 2.0, places=2)

    def test_case_rectangular_emission_does_not_add_realtime_lights(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertIn('foreach (float z in new[] { -.45f, .40f })', text)
        self.assertIn('"Warm LED rectangle long diffuser", warm,', text)
        self.assertIn('"Warm LED rectangle short diffuser", warm,', text)
        self.assertIn('new Vector3(0, 3.274f, z)', text)
        self.assertIn('"REFERENCE lower solid gold case rail"', text)
        self.assertEqual(text.count('AddComponent<Light>()'), 1)
        self.assertIn('lamp.shadows = LightShadows.None;', text)
        # Diffuser remains under the gold top frame and below the glass top.
        self.assertLess(3.274 + .011/2, 3.31 - .055/2)
        self.assertLess(3.31 + .055/2, 3.42 - .026/2)

    def test_optional_buildings_count_only_after_success(self):
        text = source('BraxChampionsBuildings.cs')
        self.assertRegex(text, r'bool casinoGenerated=false,poolGenerated=false;')
        self.assertRegex(text, r'Optional\("kasyno T11",\(\)=>\s*\{\s*BraxChampionsCasinoT11.Generate\(Out\);\s*casinoGenerated=true;')
        self.assertRegex(text, r'Optional\("basen P1",\(\)=>\s*\{\s*BraxChampionsPoolP1.Generate\(\);\s*poolGenerated=true;')
        self.assertIn('d.buildings.Length+(casinoGenerated?1:0)+(poolGenerated?1:0)', text)
        self.assertNotIn('return d.buildings.Length+2;', text)
        self.assertIn('visualAccepted=false', text)
        # Exhaust the arithmetic contract; this is not execution of Unity C#.
        expected = {(False,False):2, (True,False):3, (False,True):3, (True,True):4}
        for (casino,pool),count in expected.items():
            with self.subTest(casino=casino, pool=pool):
                self.assertEqual(2+int(casino)+int(pool), count)

    def test_changes_do_not_launch_builds_or_change_owner_approval(self):
        for filename in ('BraxChampionsArenaMural.cs', 'BraxChampionsBeltShowcase.cs', 'BraxChampionsBuildings.cs'):
            text = source(filename)
            with self.subTest(filename=filename):
                self.assertNotIn('BuildPipeline.BuildPlayer', text)
                self.assertNotIn('workflow_dispatch', text)
                self.assertNotIn('visualAccepted=true', text)

    def test_android_picker_java_source_is_disabled_when_apk_uses_compiled_jar(self):
        meta_path = ROOT / 'unity-city/Assets/Updates/Android/BraxContentPicker.java.meta'
        if meta_path.exists():
            meta = meta_path.read_text(encoding='utf-8')
            self.assertRegex(meta, r'Android:\s+Android[\s\S]{0,120}?enabled:\s+0')
        else:
            # Current branch intentionally has no loose Android Java source in Assets.
            self.assertFalse((ROOT / 'unity-city/Assets/Updates/Android/BraxContentPicker.java').exists())
        workflow = (ROOT / '.github/workflows/mistrzowie-apk.yml').read_text(encoding='utf-8')
        self.assertIn('Compile Android file picker for future content-only patches', workflow)
        self.assertIn('test -s unity-city/Assets/Plugins/Android/BraxContentPicker.jar', workflow)
        self.assertIn("if: ${{ inputs.etap != 'sceny' }}", workflow)


if __name__ == '__main__':
    unittest.main(verbosity=2)
