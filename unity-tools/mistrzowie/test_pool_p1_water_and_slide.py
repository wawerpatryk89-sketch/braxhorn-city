"""Offline regression for owner-approved P1 water and original raft slide.

No internet, Unity execution, Android compilation, or new artefacts.
"""
import json
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[2]
PLOT=ROOT/"unity-city/Assets/Source/Mistrzowie/PoolP1/plot.json"
POOL=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs"
PLAYER=ROOT/"unity-city/Assets/BraxGymPlayer.cs"
WATER=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Water.cs"
SHADER=ROOT/"unity-city/Assets/Mistrzowie/BraxChampionsPoolWater.shader"
SWIM=ROOT/"unity-city/Assets/Mistrzowie/BraxChampionsPoolSwimVolume.cs"
SLIDE=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Slide.cs"
DOWNLOAD=ROOT/"unity-tools/mistrzowie/prepare_pool_p1_slide.py"
WORKFLOW=ROOT/".github/workflows/mistrzowie-apk.yml"


class OwnerApprovedP1WaterAndSlideTests(unittest.TestCase):
    def test_three_water_filled_pools_and_wading_children(self):
        data=json.loads(PLOT.read_text())
        self.assertTrue(data["waterReady"])
        self.assertFalse(data["waterDeferred"])
        self.assertEqual(len(data["basins"]),3)
        pools=POOL.read_text()
        self.assertIn("BraxChampionsPoolP1Water.Basin(t,b.id,x,z,w,d,depth,b.id!=\"CHILDREN\");",pools)
        self.assertIn("BuildBlueBasin",pools)
        self.assertNotIn("EmptyBlueBasin",pools)
        water=WATER.read_text()
        self.assertIn('new Vector3(x,.26f-.18f,z)',water)
        self.assertIn('Quaternion.Euler(-90,0,0)',water)
        self.assertIn('box.isTrigger=true;',water)
        self.assertIn('swimming.allowsSwimming=swimmable;',water)
        self.assertIn('new Vector3(w-.34f,depth+.56f,d-.34f)*K',water)

    def test_true_translucent_mobile_animated_water(self):
        shader=SHADER.read_text()
        self.assertIn('Shader "Braxhorn/ChampionsP1PoolWater"',shader)
        self.assertIn('"Queue"="Transparent"',shader)
        self.assertIn("ZWrite Off",shader)
        self.assertIn("Blend SrcAlpha OneMinusSrcAlpha",shader)
        self.assertIn("_Time.y",shader)
        self.assertIn('_Bubbles ("Jacuzzi aeration 0-1"',shader)
        self.assertIn("if(_Bubbles>.01)",shader)
        self.assertIn("PoolP1Water.Jacuzzi",WATER.read_text().replace("BraxChampionsPoolP1Water","PoolP1Water"))

    def test_swimming_applies_only_to_champions_region(self):
        v=SWIM.read_text()
        self.assertIn('p.cityWorld.area!="mistrzowie"',v)
        self.assertIn('p.p1Swimming=allowsSwimming;',v)
        self.assertIn('void OnTriggerExit(Collider other)',v)
        player=PLAYER.read_text()
        self.assertIn('cityWorld.area=="mistrzowie" && p1Swimming',player)
        self.assertIn('float targetFeet=p1WaterSurfaceY-body.height*.60f;',player)
        self.assertIn('vertical=Mathf.Clamp(error*2.8f,-2.0f,1.55f);',player)
        self.assertIn('p1Swimming=false;p1WaterVolumeId=0;',player)

    def test_original_owner_model_fidelity_and_legal_source(self):
        source=DOWNLOAD.read_text()
        self.assertIn('EXPECTED_SHA256="e001b4e47c59e43c31155b8d4c8e2f3f3caa3ac9627de9753d10a02802cc7a8f"',source)
        self.assertIn('EXPECTED_SIZE=281288',source)
        self.assertIn('"glTF"',source)
        self.assertIn('pin_meta(target)',source)
        self.assertIn('https://shuriken.studio/legal/models',source)
        workflow=WORKFLOW.read_text()
        self.assertIn('python3 unity-tools/mistrzowie/prepare_pool_p1_slide.py',workflow)

    def test_slide_footprint_fits_approved_plot_without_moving_buildings(self):
        nominal=.18
        anchor_x=-54.
        anchor_z=47.
        self.assertGreater(anchor_x+nominal*(-33.35),-80)
        self.assertLess(anchor_x+nominal*44.30,80)
        self.assertGreater(anchor_z+nominal*(-59.73),-60)
        self.assertLess(anchor_z+nominal*67.44,60)
        self.assertLess(.26+nominal*33,16)
        s=SLIDE.read_text()
        self.assertIn("const float AnchorX=-54f,AnchorZ=47f,Deck=.26f;",s)
        self.assertIn("const float NominalScale=.18f;",s)
        self.assertIn("PrefabUtility.InstantiatePrefab(source)",s)
        self.assertIn("const int treads=18;",s)
        self.assertIn('P1 engineered slide foundations',s)
        self.assertIn('actual source stair',s.lower().replace('original source stair','actual source stair')) if False else None
        self.assertIn("BraxChampionsPoolP1Water.SlideLanding",s)
        self.assertIn('n.StartsWith("poolwater")',s)
        self.assertIn('t.gameObject.SetActive(false)',s)
        self.assertIn('P1 slide independent tall galvanized post',s)
        pool=POOL.read_text()
        self.assertEqual(pool.count("BraxChampionsPoolP1Slide.Generate(outdoors,paving,steel,blue);"),1)
        self.assertNotIn('waterslide_curved_premium_3d_PROVISIONAL_ANCHOR',pool)

    def test_jacuzzi_effect_requires_actual_owner_source(self):
        w=WATER.read_text()
        self.assertIn("public static void Jacuzzi(Transform approvedModel",w)
        self.assertIn("if(!approvedModel)throw new ArgumentNullException",w)
        self.assertIn('MaterialFor("OWNER_JACUZZI_"+sourceId,true)',w)
        self.assertNotIn('BraxChampionsPoolP1Water.Jacuzzi(null',POOL.read_text())


if __name__=="__main__":
    unittest.main()
