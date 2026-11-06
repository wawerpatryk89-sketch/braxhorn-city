"""Offline checks for P1 SPA finished layout and ORIGINAL owner source fidelity."""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
BUILD=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs"
SPA=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1SpaFurnishing.cs"
SIG=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1OriginalSign.cs"
IMPORT=ROOT/"unity-tools/mistrzowie/import_pool_p1_spa_bundle.py"


class PoolP1RealSpaEquipment(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build=BUILD.read_text()
        cls.spa=SPA.read_text()
        cls.sign=SIG.read_text()
        cls.imp=IMPORT.read_text()

    def test_two_separate_treatment_rooms_with_walkable_entries(self):
        s=self.build
        self.assertIn('SpaMassageRooms();',s)
        self.assertIn('Room("GABINET MASAZU 1"',s)
        self.assertIn('Room("GABINET MASAZU 2"',s)
        self.assertIn('RectGlazing(-7.75f,1f,10f,6.2f,false,0f,1.35f)',s)
        self.assertIn('P1 massage suite clean central corridor PBR',s)
        self.assertEqual(s.count('BraxChampionsPoolP1SpaFurnishing.Generate('),1)
        self.assertNotIn('OwnerEquipmentAnchors();',s)

    def test_original_sauna_beds_vanity_shower_and_lockers(self):
        s=self.spa
        for name in ('sauna_product_model.glb','portable-massage-table.glb',
                     'double-bathroom-vanity.glb','shower_cabin.glb',
                     'lockers.glb','locker_room_bench_free_model.glb'):
            self.assertIn(name,s)
        self.assertIn('PrefabUtility.InstantiatePrefab(source)',s)
        self.assertIn('Uniform scale only',s)
        self.assertIn('P1 ORIGINAL OWNER MODEL REQUIRED',s)
        self.assertNotIn('GameObject.CreatePrimitive(',s)  # no fake replacements

    def test_four_original_jacuzzis_fit_without_blocking_middle_corridor(self):
        s=self.spa
        self.assertIn('hot-tub-original-exterior-model.glb',s)
        self.assertIn('foreach(float x in new[]{9.2f,14.7f})',s)
        self.assertIn('foreach(float z in new[]{-11.85f,-15.65f})',s)
        xs=(9.2,14.7)
        zs=(-11.85,-15.65)
        width,depth=2.23,2.85
        self.assertGreater((xs[1]-xs[0])-width,3.0)   # open central entrance
        self.assertGreater(abs(zs[1]-zs[0])-depth,.9) # access between rows
        for x in xs:
            self.assertGreater(x-width/2,6.6)
            self.assertLess(x+width/2,17.4)
        for z in zs:
            self.assertGreater(z-depth/2,-17.7)
            self.assertLess(z+depth/2,-9.9)
        self.assertIn('BraxChampionsPoolP1Water.Jacuzzi(',s)

    def test_exact_two_signs_and_no_keyword_guessing(self):
        s=self.sign
        self.assertIn('P1_BASEN_BASEN_MIEJSKI.png',s)
        self.assertIn('P1_SPA_SPA_WELLNESS.png',s)
        self.assertIn('BuildSpaSign(p1Root);',s)
        self.assertIn('sw=4.2f,sh=1.05f',s)
        self.assertNotIn('IndexOf("BASEN"',s)

    def test_asset_bundle_requires_sha256_and_keeps_original_guid(self):
        s=self.imp
        self.assertIn('3b86a094f3b4b31ff11597d78272e853181857f057d429f8b4d13493ab0059e9',s)
        self.assertIn('len(records)!=len(EXPECTED)',s)
        self.assertIn('original_meta_sha256',s)
        self.assertIn('Existing owner source/GUID differs, no silent replacement',s)
        self.assertNotIn('requests.post',s)


if __name__=="__main__":
    unittest.main()
