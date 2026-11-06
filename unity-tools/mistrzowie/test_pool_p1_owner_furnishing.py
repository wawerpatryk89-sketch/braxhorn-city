"""P1 actual owner furniture and finished guest circulation.

Static regression only; real Unity mesh collisions and Android visuals need
separate PlayMode/device validation before this can be called completed.
"""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[2]
DIR=ROOT/"unity-city/Assets/Editor/Mistrzowie"
POOL=DIR/"BraxChampionsPoolP1.cs"
SPA=DIR/"BraxChampionsPoolP1SpaFurnishing.cs"
REMAINING=DIR/"BraxChampionsPoolP1Equipment.cs"
WATER=DIR/"BraxChampionsPoolP1Water.cs"
PACK=ROOT/"unity-tools/mistrzowie/import_pool_p1_owner_assets.py"
WORKFLOW=ROOT/".github/workflows/mistrzowie-apk.yml"


class OwnerFurnishingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pool=POOL.read_text(encoding="utf-8")
        cls.spa=SPA.read_text(encoding="utf-8")
        cls.remaining=REMAINING.read_text(encoding="utf-8")

    def test_actual_four_tubs_and_measured_nonoverlapping_water(self):
        self.assertIn('"hot-tub-original-exterior-model.glb"',self.spa)
        self.assertIn('foreach(float x in new[]{9.2f,14.7f})',self.spa)
        self.assertIn('foreach(float z in new[]{-11.85f,-15.65f})',self.spa)
        self.assertIn('BraxChampionsPoolP1Water.Jacuzzi(tub.transform,id,',self.spa)
        water=WATER.read_text(encoding="utf-8")
        self.assertIn('Spa water surface',water)
        self.assertIn('sourceRenderer.bounds',water)
        self.assertIn('original.gameObject.SetActive(false);',water)
        self.assertIn('Jacuzzi(parent,sourceId,local,new Vector2(width,depth));',water)
        # Fit envelopes for each tub: 2.30m wide by 2.89m deep.
        self.assertGreater(5.5-2.30,2.0)   # clear central west/east aisle
        self.assertGreater(3.8-2.89,.85)    # row separation
        self.assertGreater((9.2-2.30/2)-6.6,1.30)  # west door approach
        self.assertGreater(17.4-(14.7+2.30/2),1.4) # east circulation

    def test_sauna_and_real_massage_treatment_suites(self):
        self.assertIn('Room("SAUNA",12.4f,-22.5f,8.9f,8f,3.4f,white,-1);',self.pool)
        self.assertIn('"sauna_product_model.glb"',self.spa)
        self.assertIn('Room("GABINET MASAZU 1"',self.pool)
        self.assertIn('Room("GABINET MASAZU 2"',self.pool)
        self.assertEqual(self.spa.count('Install(rooms,"portable-massage-table.glb"'),2)
        self.assertIn('Install(rooms,"double-bathroom-vanity.glb"',self.spa)
        # Real glass-room circulation, no filled wall across the 2.2m entry:
        self.assertIn('Box(structure,"P1 massage suite south insulated wall",white,-14.0f,2.0f,-4f,8.15f,4.0f,.18f)',self.pool)
        self.assertIn('RectGlazing(0,-4,24f,6.4f,true,0f,10f);',self.pool)

    def test_real_shower_cabins_not_in_family_lockers(self):
        self.assertIn('Room("PRYSZNICE PREMIUM",0f,12f,10.8f,8f,3.5f,white);',self.pool)
        self.assertIn('foreach(float x in new[]{-3.6f,-1.2f,1.2f,3.6f})',self.spa)
        self.assertIn('x,12.4f,1.38f,1.16f,2.35f);',self.spa)
        self.assertIn('shower_cabin.glb',self.spa)
        self.assertIn('old)UnityEngine.Object.DestroyImmediate(obj.gameObject)',self.spa)
        self.assertIn('FileNotFoundException',self.spa)
        self.assertNotIn('OWNER_ASSET_PENDING',self.pool)

    def test_no_duplicate_sauna_showers_tubs_in_second_module(self):
        self.assertEqual(self.pool.count('BraxChampionsPoolP1SpaFurnishing.Generate('),1)
        self.assertEqual(self.pool.count('BraxChampionsPoolP1Equipment.InstallRemaining('),1)
        self.assertIn('"pool-ladder.glb"',self.remaining)
        self.assertIn('"Sun-lounger_FBX.fbx"',self.remaining)
        self.assertIn('"lifeguard-high-chair-original.glb"',self.remaining)
        self.assertNotIn('"sauna_product_model.glb"',self.remaining)
        self.assertNotIn('"hot-tub-original-exterior-model.glb"',self.remaining)
        self.assertNotIn('"shower_cabin.glb"',self.remaining)
        self.assertNotIn('Lifeguard chair support',self.pool)

    def test_structural_source_mesh_colliders_without_fake_room_blockers(self):
        # Real jacuzzi shells, padded rims, SPA benches and massage tables
        # must stop the reviewing player. The original shower glass door
        # stays accessible: use tray+frame meshes, not a blocking AABB.
        self.assertIn('static int PhysicalSourceColliders(GameObject instance,string file)',self.spa)
        self.assertIn('int structuralColliders=PhysicalSourceColliders(instance,file);',self.spa)
        self.assertIn('own.StartsWith("spa outer")',self.spa)
        self.assertIn('own.StartsWith("spa access step")',self.spa)
        self.assertIn('own.Contains("padded deck")',self.spa)
        self.assertIn('own.StartsWith("basin")',self.spa)
        self.assertIn('own=="object_2"||own=="object_6"',self.spa)
        self.assertNotIn('own=="object_3"',self.spa)
        self.assertIn('filter.gameObject.AddComponent<MeshCollider>()',self.spa)
        self.assertIn('StaticMeshCollision(lookout)',self.remaining)
        self.assertIn('StaticMeshCollision(chaise)',self.remaining)

    def test_real_arrival_driving_access_and_pedestrian_separation(self):
        self.assertIn('"P1 WEST parking access connected to roundabout"',self.pool)
        self.assertIn('"P1 EAST parking access connected to roundabout"',self.pool)
        self.assertIn('if(Mathf.Abs(Mathf.Cos(a))>.92f)continue;',self.pool)
        self.assertIn('float z=row==0?-52f:-38f;',self.pool)
        self.assertIn('if(bank==1&&row==1&&i==23)continue;',self.pool)
        self.assertEqual(2*2*24-1,95)
        # Nearest row north boundary at -35.2; piazza south edge -33.5.
        self.assertGreater((-33.5)-(-38+5.6/2),1.5)

    def test_private_release_verified_and_not_published_as_public_source(self):
        importer=PACK.read_text(encoding="utf-8")
        self.assertIn('P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip',importer)
        self.assertIn('ARCHIVE_SHA256="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"',importer)
        self.assertIn('len(records)!=16',importer)
        self.assertIn('Refuse to replace an existing Unity source/GUID',importer)
        workflow=WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("default: 'p1-basen-spa-20261106'",workflow)
        self.assertIn('asset_id',workflow)
        self.assertIn('import_pool_p1_owner_assets.py',workflow)


if __name__=="__main__":
    unittest.main()
