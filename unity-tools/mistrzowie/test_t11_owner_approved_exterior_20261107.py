"""Owner-specified T11 acceptance contracts (static tests only, NOT Unity/Android).
These tests are evidence of source wiring, not a claim the scene is visually approved.
"""
from pathlib import Path
import hashlib
import unittest

ROOT=Path(__file__).resolve().parents[2]
CASINO=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
SOURCE=(ROOT/"unity-tools/mistrzowie/prepare_casino_textures_2k.py").read_text(encoding="utf-8")
PBR=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoPbr2K.cs").read_text(encoding="utf-8")
ORIGINAL="Assets/Source/Mistrzowie/CasinoT11/OwnerDollar/dezyne_3d-dollar-sign-140.glb"
ORIGINAL_SHA="49e4fa9149aa69637db6231d2e98e611b3a41806b13a185daf7ad18e1a078640"

class CasinoOwnerApproval0711(unittest.TestCase):
    def test_no_skyscraper_generated(self):
        self.assertNotIn("Tower();",CASINO)
        self.assertNotIn("static void Tower()",CASINO)
        self.assertNotIn("Office tower north/south curtain glass",CASINO)
        self.assertIn("Site();ThreeMatchingFacades();RoofAndSkylight();CurvedFoyer();",CASINO)

    def test_front_kept_glazed_with_gold_pillars_and_black_corner_accents(self):
        self.assertIn('"T11 premium slightly tinted FRONT glass",frontGlass',CASINO)
        self.assertIn('new Color(.55f,.63f,.71f,.22f)',CASINO)
        self.assertIn('"T11 GOLD glossy front structural pilaster",pillarGold',CASINO)
        self.assertIn('"T11 GOLD glossy side structural pilaster",pillarGold',CASINO)
        self.assertIn("T11 delicate black corner inlay beside gold column",CASINO)

    def test_sides_rear_are_solid_owner_sandstone_no_side_glass(self):
        self.assertIn('facadeSandstone',CASINO)
        self.assertIn('T11 SOLID SANDSTONE 05 side wall between gold columns',CASINO)
        self.assertNotIn('T11 slim lightly tinted side window',CASINO)
        self.assertNotIn('T11 two-storey side glazing',CASINO)
        for label in ('Rear Sandstone 05 wall left','Rear Sandstone 05 wall right',
                      'Rear Sandstone 05 over-door lintel'):
            self.assertIn(label,CASINO)
        self.assertIn('"sandstone_blocks_05",',SOURCE)
        self.assertIn('"sandstone_blocks_05"',PBR)

    def test_approved_original_casino_sign_stays(self):
        self.assertIn('C1_KASYNO_KASYNO_WAWER.png',CASINO)
        self.assertIn('MainSign();RoadsideSigns();PremiumExteriorLighting();',CASINO)
        self.assertIn('"T11 original owner sign (always visible)"',CASINO)
        self.assertNotIn('BRAXHORN CITY CASINO',CASINO)

    def test_wide_continuous_left_entrance_and_three_front_bays(self):
        self.assertIn('T11 WIDE LEFT vehicle approach from X400 road',CASINO)
        self.assertIn('-65.25f,.224f,-42.5f,25.5f,.054f,11f',CASINO)
        # City road east sidewalk ends at world X=417.
        self.assertAlmostEqual(495-65.25-25.5/2,417)
        # Casino drive begins at local -52.5 => global X442.5.
        self.assertAlmostEqual(495-65.25+25.5/2,442.5)
        self.assertIn('if(side<0)',CASINO)
        self.assertIn('for(int i=0;i<=3;i++)',CASINO)
        self.assertIn('T11 3 VIP bays | common head line',CASINO)
        self.assertNotIn('for(int i=0;i<5;i++)\n                B(trim,"Actual white painted asphalt parking bay line"',CASINO)
        self.assertIn('UndergroundGarage();',CASINO)
        self.assertIn('const int baysPerRow=16;',CASINO)

    def test_owner_roof_dollar_original_not_proxy(self):
        self.assertIn(ORIGINAL,CASINO)
        self.assertIn(ORIGINAL_SHA,CASINO)
        self.assertIn('const float x=-24f,z=-15f,roofY=14.40f,nominalHeight=15.0f;',CASINO)
        self.assertIn('PrefabUtility.InstantiatePrefab(source)',CASINO)
        self.assertIn('RoofDollar();',CASINO)
        self.assertIn('T11_ROOF_DOLLAR_OWNER_GLB_NOT_STAGED',CASINO)
        self.assertNotIn('GameObject.CreatePrimitive(PrimitiveType.Cylinder) // dollar',CASINO)
        # When the original asset is checked in, its bytes must be exact.
        file=ROOT/"unity-city"/ORIGINAL
        if file.exists():
            self.assertEqual(hashlib.sha256(file.read_bytes()).hexdigest(),ORIGINAL_SHA)

    def test_no_casino_interior_created(self):
        self.assertIn("EMPTY interior",CASINO)
        self.assertNotIn("SlotMachine",CASINO)
        self.assertNotIn("AddCasinoTables",CASINO)

if __name__=="__main__":
    unittest.main()
