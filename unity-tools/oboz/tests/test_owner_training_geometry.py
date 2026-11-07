"""Measured owner geometry and E wiring; Unity/phone verification is separate."""
import json
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
EDITOR=ROOT/'unity-city/Assets/Editor/Oboz'
RUNTIME=ROOT/'unity-city/Assets/Oboz'
REPORT=json.loads((ROOT/'unity-tools/oboz/OWNER_ASSETS/TRAINING_GEOMETRY_MEASUREMENTS_20261106.json').read_text('utf8'))

class OriginalTrainingGeometry(unittest.TestCase):
    def test_bag_uniform_calibration_preserves_original_proportions(self):
        b=REPORT['bag'];factor=1.2/b['sourceBodyHeightMetres']
        self.assertAlmostEqual(b['sourceAssemblyHeightMetres']*factor+.6,2.2744949286,places=6)
        self.assertAlmostEqual(b['oldBodyMetresWhenWholeAssemblyWas2_65'],1.8990801021,places=6)
        self.assertGreater(b['sourceBodyWidthMetres']*factor,.44)
        self.assertLess(b['sourceBodyWidthMetres']*factor,.46)
        self.assertEqual((b['sourceMeshCount'],b['sourceMaterialCount'],b['sourceTriangles']),(57,1,26950))

    def test_original_gym_contains_real_equipment_and_low_authored_mats(self):
        parts={x['name']:x for x in REPORT['gym']['parts']}
        for name in ('barbell_bar','weight_bench','weight_rack','pull_up_rack','parallel_bars_1',
                     'horizontal_bar_4','kettlebell_6','dumbbell_8','weight_plate_15'):
            self.assertIn(name,parts)
            self.assertGreater(parts[name]['triangles'],0)
        self.assertAlmostEqual(parts['yoga_mat_1']['sourceBottomMetres'],-1,places=5)
        self.assertAlmostEqual(parts['step_platform']['heightMetres'],.20,places=5)

    def test_bag_measurement_is_independent_of_physics_parameters(self):
        text=(RUNTIME/'BraxObozBagGeometry.cs').read_text('utf8')
        self.assertIn('mesh.vertices',text)
        self.assertIn('TryMeasureBodyBounds',text)
        self.assertIn('TryMeasureAttachment',text)
        self.assertNotIn('BraxWorek bag',text)
        self.assertNotIn('collider.bounds',text)

    def test_one_derived_mesh_keeps_source_channels_and_runtime_shared_class(self):
        text=(EDITOR/'BraxObozBagAssembly.cs').read_text('utf8')
        self.assertIn('CombineMeshes',text)
        self.assertIn('GetAssetDependencyHash',text)
        self.assertIn('OriginalSha256',text)
        for token in ('inputUv','inputNormals','inputTangents','enableInstancing=true'):
            self.assertIn(token,text)
        self.assertNotIn('RecalculateNormals(',text)
        self.assertNotIn('RecalculateTangents(',text)
        self.assertNotIn('targetHeight=2.65',text)

    def test_equipment_uses_original_static_triangle_colliders_and_atomic_stations(self):
        gym=(EDITOR/'BraxObozGymEquipment.cs').read_text('utf8')
        campus=(EDITOR/'BraxObozCampus.cs').read_text('utf8')
        stations=(EDITOR/'BraxObozTrainingStations.cs').read_text('utf8')
        self.assertIn('AddComponent<MeshCollider>',gym)
        self.assertNotIn('AddComponent<BoxCollider>',gym)
        self.assertIn('OnGround(',gym)
        self.assertIn('Optional("stanowisko "+label',campus)
        for fake in ('"Słupek drążka"','"Skrzynia plyometryczna"','"Sanie — płozy"','"Obciążenie sań"'):
            self.assertNotIn(fake,campus+stations)
        self.assertIn('SourceSha256',gym)

    def test_gym_atlas_is_color_and_source_dimensions_are_queried(self):
        importer=(EDITOR/'BraxObozImport.cs').read_text('utf8')
        gym=(EDITOR/'BraxObozGymEquipment.cs').read_text('utf8')
        self.assertIn('OwnerExtras/Gym/gym_environment.png',importer)
        self.assertIn('GetSourceTextureWidthAndHeight',gym)
        self.assertNotIn('atlas.width',gym)
        self.assertIn('TextureImporterShape.Texture2D',gym)

    def test_new_training_script_meta_guids_are_stable_and_unique(self):
        files=[EDITOR/(x+'.cs.meta') for x in ('BraxObozBagAssembly','BraxObozGymEquipment',
                 'BraxObozTrainingStations','BraxObozTrainingRegression')]
        files+=[RUNTIME/(x+'.cs.meta') for x in ('BraxObozBagGeometry','BraxObozEquipmentGeometry')]
        ids=[]
        for file in files:
            match=re.search(r'^guid: ([0-9a-f]{32})$',file.read_text('utf8'),re.M)
            self.assertIsNotNone(match);ids.append(match[1])
        self.assertEqual(len(ids),len(set(ids)))
