"""Offline verification of approved P1 SPA owner-model integration.

Checks the CURRENT 16-file private owner ZIP, not the superseded 7-model
draft package. No Unity/PlayMode/Android execution is implied.
"""
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
BUILD=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs"
SPA=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1SpaFurnishing.cs"
SIG=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1OriginalSign.cs"
IMPORT=ROOT/"unity-tools/mistrzowie/import_pool_p1_owner_assets.py"
WATER=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Water.cs"


class PoolP1RealSpaEquipment(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.build=BUILD.read_text(encoding="utf-8")
        cls.spa=SPA.read_text(encoding="utf-8")
        cls.sign=SIG.read_text(encoding="utf-8")
        cls.imp=IMPORT.read_text(encoding="utf-8")

    def test_two_separate_treatment_rooms_with_unblocked_entries(self):
        s=self.build
        self.assertIn('SpaMassageRooms();',s)
        self.assertIn('Room("GABINET MASAZU 1"',s)
        self.assertIn('Room("GABINET MASAZU 2"',s)
        self.assertIn('RectGlazing(0,-4,24f,6.4f,true,0f,10f);',s)
        self.assertIn('Box(structure,"P1 massage suite south insulated wall",white,-14.0f,2.0f,-4f,8.15f,4.0f,.18f)',s)
        self.assertIn('P1 massage suite clean central corridor PBR',s)
        self.assertEqual(s.count('BraxChampionsPoolP1SpaFurnishing.Generate('),1)
        self.assertEqual(s.count('BraxChampionsPoolP1Equipment.InstallRemaining('),1)
        self.assertNotIn('OwnerEquipmentAnchors();',s)

    def test_original_sauna_massage_tables_vanity_cabins_and_lockers(self):
        s=self.spa
        for name in ('sauna_product_model.glb','portable-massage-table.glb',
                     'double-bathroom-vanity.glb','shower_cabin.glb',
                     'lockers.glb','locker_room_bench_free_model.glb'):
            self.assertIn(name,s)
        self.assertIn('PrefabUtility.InstantiatePrefab(source)',s)
        self.assertIn('Uniform scale only',s)
        self.assertIn('FileNotFoundException',s)
        self.assertNotIn('GameObject.CreatePrimitive(',s)
        self.assertIn('Room("PRYSZNICE PREMIUM",0f,12f,10.8f,8f,3.5f,white);',self.build)
        self.assertIn('foreach(float x in new[]{-3.6f,-1.2f,1.2f,3.6f})',s)

    def test_four_original_jacuzzis_and_source_measured_water(self):
        s=self.spa
        self.assertIn('hot-tub-original-exterior-model.glb',s)
        self.assertIn('foreach(float x in new[]{9.2f,14.7f})',s)
        self.assertIn('foreach(float z in new[]{-11.85f,-15.65f})',s)
        width,depth=2.23,2.85
        xs=(9.2,14.7); zs=(-11.85,-15.65)
        self.assertGreater(xs[1]-xs[0]-width,3.0)
        self.assertGreater(abs(zs[1]-zs[0])-depth,.9)
        for x in xs:
            self.assertGreater(x-width/2,6.6)
            self.assertLess(x+width/2,17.4)
        for z in zs:
            self.assertGreater(z-depth/2,-17.7)
            self.assertLess(z+depth/2,-9.9)
        self.assertIn('BraxChampionsPoolP1Water.Jacuzzi(',s)
        water=WATER.read_text(encoding="utf-8")
        self.assertIn('Spa water surface',water)
        self.assertIn('original.gameObject.SetActive(false);',water)

    def test_exact_two_owner_signs_no_keyword_guessing_or_retyping(self):
        s=self.sign
        self.assertIn('P1_BASEN_BASEN_MIEJSKI.png',s)
        self.assertIn('P1_SPA_SPA_WELLNESS.png',s)
        self.assertIn('Build(root,"BASEN_MIEJSKI",PoolSign,0f,6.80f,-28.40f,10.0f)',s)
        self.assertIn('Build(root,"SPA_WELLNESS",SpaSign,15.2f,5.37f,-28.40f,4.40f)',s)
        self.assertIn('var h=width/4f;',s)
        self.assertNotIn('IndexOf("BASEN"',s)

    def test_current_private_asset_bundle_requires_sha_and_pins_guids(self):
        s=self.imp
        self.assertIn('P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip',s)
        self.assertIn('5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263',s)
        self.assertIn('len(records)!=16',s)
        self.assertIn('meta=path+".meta"',s)
        self.assertIn('Refuse to replace an existing Unity source/GUID',s)
        self.assertNotIn('requests.post',s)


if __name__=="__main__":
    unittest.main()
