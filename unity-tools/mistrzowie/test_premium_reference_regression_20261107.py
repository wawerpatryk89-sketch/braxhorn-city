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


def field(text, name):
    # Also matches comma-separated declarations: const float A=1f, B=2f;
    m = re.search(r'[\s,]' + re.escape(name) + r'\s*=\s*([\d.]+)f?\s*[;,]', text)
    if not m:
        raise AssertionError('Missing numeric field: ' + name)
    return float(m.group(1))


class PremiumReferenceRegressions(unittest.TestCase):
    def test_entrance_print_is_in_front_of_lobby_wall(self):
        text = source('BraxChampionsArenaMural.cs')
        arena = source('BraxChampionsArena.cs')
        # 9.11 owner: the front wall is a real structural wall, clad in black marble on its lobby face
        # (23:43). The print panel stands 4.5 cm in front of the marble.
        self.assertIn('WallImageZ=BraxChampionsArena.FrontWallLobbyFaceZ+BraxChampionsArena.LobbyMarbleDepth+.045f', text)
        thickness = float(re.search(r'WallThickness=([\d.]+)f;', arena).group(1))
        self.assertIn('FrontWallLobbyFaceZ=98.3f-.045f+WallThickness', arena)
        self.assertGreaterEqual(thickness, .30)
        z = 98.3 - .045 + thickness + .03
        self.assertGreater(z, 98.3)
        # The print faces the lobby (+Z in bowl space); the winding is chosen from that normal.
        self.assertIn('Vector3.left,Vector3.forward,MuralWidth,MuralHeight,out _);', text)
        printer = source('BraxChampionsArenaMuralPrint.cs')
        self.assertIn('bool flip=Vector3.Dot(Vector3.Cross(tl-bl,tr-bl),facing)<0;', printer)

    def test_entrance_uv_is_readable_from_lobby_and_keeps_aspect(self):
        text = source('BraxChampionsArenaMural.cs')
        # Owner 9.11 23:43 "Mural jest ucięty": the WHOLE 3:1 picture above the doorway, no door cut-out,
        # viewer-right = -X (not mirrored), no edge-colour extensions.
        width, height = field(text, 'MuralWidth'), field(text, 'MuralHeight')
        self.assertAlmostEqual(width / height, 3.0, places=6)
        self.assertGreater(field(text, 'MuralBottom'), 4.20 + .18 + .16)      # above the door and its gold portal
        self.assertAlmostEqual(constant(text, 'DoorRight') - constant(text, 'DoorLeft'), 4.10)
        self.assertIn('new Vector3(cx+MuralWidth*.5f,MuralBottom,WallImageZ),Vector3.left,Vector3.forward', text)
        self.assertNotIn('AddSegment(', text)

    def test_entrance_mural_top_is_below_lobby_roof_underside(self):
        # Owner 9.11: entrance mural was cut at the top by the lobby roof.
        text = source('BraxChampionsArenaMural.cs')
        exteriors = (ROOT / 'unity-tools/mistrzowie/build_exteriors.py').read_text(encoding='utf-8')
        m = re.search(r"m\.slab\('roof',foot\.buffer\(\.55\),([\d.]+),([\d.]+)\)", exteriors)
        self.assertIsNotNone(m)
        underside = float(m.group(1)) - float(m.group(2))
        bowl_raise = .22 - .055
        top = field(text, 'MuralBottom') + field(text, 'MuralHeight') + .16 + bowl_raise
        self.assertLess(top, underside - .05)
        # ... and under the sight line from the entrance doors past the mezzanine edge (slab underside
        # 11.28 m, hall-side edge 12.9 m from the doors, wall 22.1 m away, eye 1.8 m): about 18 m.
        self.assertLess(top, 1.8 + (11.28 - 1.8) / 12.9 * 22.1 + .2)
        self.assertNotIn('AddComponent<BoxCollider>', text)
        self.assertNotIn('AddComponent<MeshCollider>', text)

    # 9.11 owner photo (wall vitrine on black marble) supersedes the 7.11/8.11
    # low-base case. Belt pose rules from the phone proofs stay in force.
    def test_case_preserves_owner_belt_and_plaque(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertIn('tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx', text)
        self.assertIn('BRAXHORN_ARENA_CASE_PLAQUE.jpg', text)
        # ONE belt in the whole arena: only the lobby vitrine hangs it.
        self.assertEqual(text.count('var belt=OriginalBelt(display,BeltLength,'), 1)
        self.assertIn('"VITRINE_B_CREST"', text)
        self.assertIn('.9889f,.8451f,.0684f,.8340f', text)  # measured crest region, U swapped for yaw 180
        self.assertNotIn('EncodeToPNG', text)
        self.assertNotIn('SetPixels', text)
        arena = source('BraxChampionsArena.cs')
        self.assertNotIn('BraxChampionsBeltShowcase.Generate(', arena)
        arrival = source('BraxChampionsArenaArrival.cs')
        self.assertEqual(arrival.count('BraxChampionsBeltShowcase.GenerateLobby(root)'), 1)

    def test_case_is_owner_photo_vitrine_and_has_no_old_backplate(self):
        text = source('BraxChampionsBeltShowcase.cs')
        for marker in (
            '"A1 VITRINE | wall back panel"',
            '"A1 VITRINE | FRONT glass"',
            '"A1 VITRINE | plaque shelf glass"',
            '"A1 VITRINE | brass side bracket"',
            '"A1 VITRINE | LED tray frame long"',
            '"A1 VITRINE | LED vertical back corner"',
            '"A1 VITRINE | LED bottom front line"',
            '"A1 VITRINE | gold belt pin"',
            '"A1 VITRINE | brushed brass plaque"',
            '"BRAXHORN ARENA"', '"CHAMPION\'S BELT"',
        ):
            self.assertIn(marker, text)
        self.assertNotIn('REFERENCE black leather back panel', text)
        self.assertNotIn('Podium(display', text)
        self.assertNotIn('Wood veneer showcase plinth', text)
        self.assertNotIn('inner back inset', text)
        # Owner 9.11: "po co ta deska obok" - no slat board, no twig planter.
        self.assertNotIn('bronze slat', text)
        self.assertNotIn('cylinder planter', text)
        # Hung on the marble hall wall (owner 9.11 23:43: no pillar), 5 mm in front of the marble.
        self.assertIn('LobbyZ=WallMuralFaceZ-PylonD-.005f', text)
        self.assertIn('o.transform.localRotation=Quaternion.Euler(0,180f,0);', text)
        # Black marble with white veins (photo); Poly Haven has none.
        self.assertIn('BraxChampionsFoundation.LibraryMarble("A1_vitrine_black_marble_veined"', text)
        self.assertIn('A1_VITRINE_MARBLE016_MISSING', text)
        prep = (ROOT / 'unity-tools/mistrzowie/prepare_assets.py').read_text(encoding='utf-8')
        self.assertIn("('Marble016',['Color','NormalGL','Roughness'])", prep)
        foundation = source('BraxChampionsFoundation.cs')
        self.assertIn('prefix="Marble016_4K-JPG_"', foundation)
        self.assertIn('"Marble016_ARM_4k.png"', foundation)
        # Glass contract unchanged: 12 mm, single alpha layer, no Z write.
        self.assertIn('glass.SetFloat("_ZWrite",0f);', text)
        self.assertIn('glass.SetFloat("_Transparent",1f);', text)
        self.assertIn('new Color(.94f,.96f,.98f,.025f)', text)
        self.assertIn('const float thickness=.012f', text)
        self.assertIn('A1_glass_', text)
        self.assertIn('24 rigid', text)
        self.assertIn('A1_BELT_OWNER_3D_FRONT_PRESENTATION', text)
        # Marble gets metric UV, never 0..1 per primitive face.
        self.assertIn('static GameObject MetricCube(', text)

    def test_case_dimensions_are_measured_on_owner_photo(self):
        text = source('BraxChampionsBeltShowcase.cs')
        belt = field(text, 'BeltLength')
        self.assertAlmostEqual(belt, 1.27, places=2)  # UFC Legacy belt, 50 in
        mm_per_px = belt / 962
        # CaseBottom / PylonW are no longer photo values: the case hangs on the wall (owner 9.11 23:43).
        for name, px in (('CaseW', 478), ('CaseH', 1358),
                         ('ShelfAbove', 213), ('PlaqueW', 347), ('PlaqueH', 135)):
            with self.subTest(name=name):
                self.assertAlmostEqual(field(text, name), px * mm_per_px, delta=.01)
        # The hung belt fits inside the case above the shelf.
        bottom, h = field(text, 'CaseBottom'), field(text, 'CaseH')
        shelf = field(text, 'ShelfAbove')
        self.assertIn('float beltCentreY=CaseBottom+.32f+BeltLength*.5f;', text)
        self.assertGreater(bottom + .32, bottom + shelf)
        self.assertLess(bottom + .32 + belt, bottom + h - .08)
        self.assertAlmostEqual(bottom, .95, places=2)              # belt centre at eye level
        self.assertLess(field(text, 'PylonD'), .08)                # a thin back panel, not a pillar
        # Everything is real metres scaled by K exactly once.
        self.assertIn('display.localScale=Vector3.one*k;', text)

    def test_case_plaque_faces_front_and_clears_glass(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertIn('Quaternion.Euler(0,180f,0),new Vector2(.031f,.055f)', text)
        # Plaque and lettering sit on the marble, behind the front glass.
        self.assertIn('new Vector3(0,plaqueY,-.006f)', text)
        self.assertIn('new Vector3(0,plaqueY-.005f,-.012f)', text)
        self.assertLess(.012, field(text, 'CaseD'))

    def test_case_reference_lighting_uses_one_realtime_light(self):
        text = source('BraxChampionsBeltShowcase.cs')
        self.assertEqual(text.count('AddComponent<Light>()'), 1)
        self.assertIn('lamp.shadows=LightShadows.None;', text)
        self.assertIn('lamp.renderMode=LightRenderMode.ForceVertex;', text)

    def test_optional_buildings_count_only_after_success(self):
        text = source('BraxChampionsBuildings.cs')
        self.assertIn('bool casinoGenerated=false;', text)
        self.assertRegex(text, r'Optional\("kasyno T11",\(\)=>\s*\{\s*BraxChampionsCasinoT11.Generate\(Out\);\s*casinoGenerated=true;')
        # 9.11 owner: pool P1 removed completely.
        self.assertNotIn('BraxChampionsPoolP1', text)
        self.assertIn('d.buildings.Length+(casinoGenerated?1:0)', text)
        self.assertIn('visualAccepted=false', text)
        self.assertFalse((ROOT / 'unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs').exists())

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
