"""Offline structure check: existing 3D fir sources at the compact camp fence.

No Unity, visual, performance or phone claim; no character assets touched.
"""
import re
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
SOURCE=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozVegetation.cs").read_text("utf8")
LAYOUT=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampusLayout.cs").read_text("utf8")


class BoundaryForestTest(unittest.TestCase):
    def test_reuses_authored_lod_prototypes(self):
        self.assertIn("AddFenceWoodland(forest,prototypes)",SOURCE)
        self.assertIn("Object.Instantiate(prototypes[prototypeIndex],forest)",SOURCE)
        self.assertIn("const int ringSamples=160;",SOURCE)
        self.assertIn("LODs(new[]{new LOD(.40f",SOURCE)
        self.assertIn("PlotRadiusX",SOURCE)
        self.assertIn("PlotRadiusZ",SOURCE)
        self.assertIn("PlotCenterZ",SOURCE)

    def test_no_road_or_off_terrain_plantings(self):
        self.assertIn("Mathf.Abs(x)>135f||Mathf.Abs(z)>135f",SOURCE)
        self.assertIn("z < -105f && Mathf.Abs(x)<23f",SOURCE)
        self.assertIn("BraxObozCampusLayout.Reserved(x,z,2f)",SOURCE)
        self.assertIn("GateZ=-125f",LAYOUT)

    def test_original_tree_source_and_mobile_lod_kept(self):
        self.assertIn('name+"_LOD1__unity.glb"',SOURCE)
        self.assertIn('name+"_LOD2__unity.glb"',SOURCE)
        self.assertNotIn("Random.Range",SOURCE)
        self.assertIn("new LOD(.075f",SOURCE)


if __name__=="__main__":
    unittest.main()
