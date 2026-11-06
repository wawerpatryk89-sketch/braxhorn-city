"""Offline consistency checks for P1; no Unity, downloads, builds or repo writes."""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "unity-city/Assets/Source/Mistrzowie/PoolP1/plot.json"
LEGACY = ROOT / "unity-tools/mistrzowie/legacy_foundation.py"
UNITY = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs"
INTEGRATION = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuildings.cs"
MATERIALS = ROOT / "unity-tools/mistrzowie/prepare_pool_p1_textures_2k.py"


def bounds(b):
    return (b["x"]-b["w"]/2, b["z"]-b["d"]/2,
            b["x"]+b["w"]/2, b["z"]+b["d"]/2)


def overlap(a, b):
    return a[0]<b[2] and b[0]<a[2] and a[1]<b[3] and b[1]<a[3]


class PoolP1LayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=json.loads(SOURCE.read_text(encoding="utf-8"))

    def test_original_masterplan_plot(self):
        plot=self.data["lot"]
        self.assertEqual((plot["id"],plot["x"],plot["z"],plot["w"],plot["d"],plot["maximumHeight"]),
                         ("P1",520,180,160,120,16))
        self.assertEqual(self.data["parkingSpaces"],95)
        self.assertEqual(self.data["texResolution"],2048)
        self.assertFalse(self.data["waterDeferred"])
        self.assertTrue(self.data["waterReady"])

    def test_real_excavations_are_disjoint_and_on_lot(self):
        outer=bounds(self.data["lot"])
        pools=self.data["basins"]
        self.assertEqual([p["id"] for p in pools],
                         ["SPORT_25M","FAMILY","CHILDREN"])
        for a in pools:
            amin,zmin,amax,zmax=bounds(a)
            self.assertTrue(outer[0]<=amin<amax<=outer[2])
            self.assertTrue(outer[1]<=zmin<zmax<=outer[3])
            self.assertGreater(a["depth"],0)
            for b in pools:
                if b is not a:self.assertFalse(overlap(bounds(a),bounds(b)),
                    f"These excavations overlap: {a['id']} / {b['id']}")

    def test_owner_jacuzzi_mount_is_not_an_excavation(self):
        self.assertEqual(len(self.data["basins"]),3)
        unity=UNITY.read_text(encoding="utf-8")
        self.assertIn("BraxChampionsPoolP1SpaFurnishing.Generate(",unity)
        equip=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1SpaFurnishing.cs").read_text()
        self.assertIn('hot-tub-original-exterior-model.glb',equip)
        self.assertIn("foreach(float x in new[]{9.2f,14.7f})",equip)
        self.assertIn("foreach(float z in new[]{-11.85f,-15.65f})",equip)
        self.assertIn("BraxChampionsPoolP1Water.Jacuzzi(",equip)
        self.assertNotIn('if(b.id=="JACUZZI")',unity)
        self.assertNotIn('SPA jacuzzi underwater tiled sitting ledge',unity)

    def test_pool_with_water_is_built_into_ground_not_over_it(self):
        source=LEGACY.read_text(encoding="utf-8")
        self.assertIn("POOL_HOLES=[",source)
        self.assertIn("if any(inside((x,z),r) for r in POOL_HOLES):return None",source)
        self.assertIn("[T11_GARAGE_RAMP]+POOL_HOLES+",source)
        unity=UNITY.read_text(encoding="utf-8")
        self.assertIn("DeckWithBasins",unity)
        self.assertIn("BuildBlueBasin",unity)
        self.assertIn("BraxChampionsPoolP1Water.Basin",unity)
        self.assertIn("waterReady",unity)
        self.assertNotIn("water=SeaMaterial",unity)
        self.assertIn("d.waterDeferred||!d.waterReady",unity)
        self.assertIn("RectGlazing(0,-28,25f,5.7f,true,0f,3.5f)",unity)
        self.assertIn("RectGlazing(-41,23,22,7.9f,false,0f,3.3f)",unity)

    def test_isolated_buildings_integration(self):
        build=INTEGRATION.read_text(encoding="utf-8")
        self.assertEqual(build.count("BraxChampionsPoolP1.Generate();"),1)
        self.assertIn("return d.buildings.Length+2;",build)
        self.assertIn("BraxChampionsCasinoT11.Generate(Out);",build)

    def test_polyhaven_real_2k_and_import(self):
        source=MATERIALS.read_text(encoding="utf-8")
        self.assertIn('"white_stucco_02"',source)
        self.assertIn('"marble_mosaic_tiles"',source)
        self.assertIn('"long_white_tiles"',source)
        self.assertIn('"herringbone_brick_02"',source)
        self.assertIn('"https://api.polyhaven.com/files/"+slug',source)
        self.assertIn("image.size!=(2048,2048)",source)
        self.assertIn("hashlib.sha256",source)
        self.assertIn("ensure_meta(destination)",source)
        src=UNITY.read_text(encoding="utf-8")
        self.assertIn('new Color(.90f,.965f,1f,.12f)',src)
        self.assertIn("BlendMode.SrcAlpha",src)
        self.assertIn("BlendMode.OneMinusSrcAlpha",src)
        self.assertIn('P1_warm_herringbone_paving',src)
        self.assertIn('"P1 front entrance pedestrian piazza PBR HERRINGBONE",patternPaving',src)
        self.assertIn('new Color(.40f,.40f,.41f)',src)


if __name__ == "__main__":
    unittest.main()
