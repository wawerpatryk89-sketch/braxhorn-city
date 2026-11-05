"""Verify promenade bike-lane cutout, never a decal over existing paving.

All checks are static geometry only; Unity scene/Android have not run.
"""
import json
from pathlib import Path
import unittest
import numpy as np
from shapely.geometry import box,Polygon
from shapely.ops import unary_union
from promenade_layout import (
    PAVILIONS,PAVILION_PADS,CYCLE_Z0,CYCLE_Z1,
    X0,X1,CYCLE_WIDTH,PEDESTRIAN_CONNECTIONS,PAVILION_FOOTPRINTS,validate
)

ROOT=Path(__file__).resolve().parents[2]
FOUND=ROOT/"unity-city/Assets/Source/Mistrzowie/Foundation.json"


class PromenadeLaneNoZFight(unittest.TestCase):
    def test_reference_12_pavilions_and_clear_walkway(self):
        self.assertTrue(validate())
        self.assertEqual(len(PAVILIONS),12)
        self.assertEqual(CYCLE_WIDTH,3.)
        self.assertEqual(X1-X0,1100.)
        self.assertEqual(len(PEDESTRIAN_CONNECTIONS),4)
        self.assertEqual(len(PAVILION_FOOTPRINTS),12)

    def test_cycle_lane_code_actually_removes_old_paving_and_no_offset(self):
        source=(ROOT/"unity-tools/mistrzowie/legacy_foundation.py").read_text()
        self.assertIn("if PROM_X0<=x<PROM_X1 and CYCLE_Z0<=z<CYCLE_Z1:return None",source)
        self.assertIn("asphalt=clip.difference(marked)",source)
        self.assertIn("inlay_polygon('cycle_lane',asphalt)",source)
        self.assertIn("inlay_polygon('cycle_mark',marked)",source)
        self.assertIn("marked.intersection(asphalt).area>1e-8",source)
        self.assertNotIn("PAVING_Y+.011",source)
        self.assertNotIn("quad('cycle_mark'",source)

    def test_source_4k_real_PBR_and_minimum_2k_android_contract(self):
        foundation=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsFoundation.cs").read_text()
        texture=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsTextureImport.cs").read_text()
        for token in ("clean_asphalt_diff_4k.jpg",
                      "clean_asphalt_nor_gl_4k.exr",
                      "clean_asphalt_arm_4k.png",
                      "concrete_pavement_02_diff_4k.jpg",
                      "concrete_pavement_02_nor_gl_4k.exr",
                      "concrete_pavement_02_arm_4k.png"):
            self.assertIn(token,foundation)
        self.assertIn('materials["cycle_lane"].SetColor(',foundation)
        self.assertIn('materials["cycle_mark"].SetColor(',foundation)
        self.assertIn("2048",texture)

    def test_12_shop_shells_and_original_2k_photo_signs(self):
        builder=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPromenade.cs").read_text()
        foundation=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsFoundation.cs").read_text()
        signs=(ROOT/"unity-tools/mistrzowie/verify_promenade_original_signs.py").read_text()
        self.assertIn('BraxChampionsPromenade.Build(root,d.promenade)',foundation)
        self.assertIn('d.promenade.Length!=12',foundation)
        self.assertIn('SignFace(root,p,Sign(p))',builder)
        self.assertIn('imp.maxTextureSize=2048',builder)
        self.assertIn('android.maxTextureSize!=2048',builder)
        self.assertIn('ClearGlass()',builder)
        self.assertIn('full 2K PBR shop shell',builder)
        self.assertIn("BLOCKED_UNMATCHED_OWNER_SIGNS",signs)
        self.assertIn("No invented/fallback sign",builder)
        self.assertFalse((ROOT/"unity-tools/mistrzowie/prepare_promenade_signs.py").exists())

    def test_shop_slab_has_no_original_ground_below_it(self):
        if not FOUND.is_file():self.skipTest("Generate physical foundation first")
        data=json.loads(FOUND.read_text())
        k=data["scaleK"]
        pads=[box(a,b,a+w,b+d) for a,b,w,d in PAVILION_FOOTPRINTS]
        covered=unary_union(pads)
        ground=[]
        for mesh in data["meshes"]:
            if mesh["role"] not in ("paving","grass"):continue
            v=np.asarray(mesh["positions"]).reshape(-1,3)/k
            for triangle in np.asarray(mesh["triangles"]).reshape(-1,3):
                coords=v[triangle][:,[0,2]]
                if coords[:,1].min()>-110 or coords[:,1].max()<-120:continue
                poly=Polygon(coords)
                if poly.area>1e-9:ground.append(poly)
        self.assertLess(unary_union(ground).intersection(covered).area,1e-4,
                        "Previous grass/paving surface still hidden below kiosk floor")

    def test_generated_mesh_red_and_white_replace_cobbles_exactly(self):
        if not FOUND.is_file():
            self.skipTest("Run legacy_foundation.py first; generated geometry is not committed")
        data=json.loads(FOUND.read_text())
        self.assertEqual(len(data["promenade"]),12)
        scale=data["scaleK"]
        role_polys={"paving":[],"cycle_lane":[],"cycle_mark":[]}
        region=box(X0,CYCLE_Z0,X1,CYCLE_Z1)
        lo=region.bounds
        for mesh in data["meshes"]:
            role=mesh["role"]
            if role not in role_polys:continue
            vertices=np.asarray(mesh["positions"],dtype=float).reshape(-1,3)/scale
            indices=np.asarray(mesh["triangles"],dtype=int).reshape(-1,3)
            for ix in indices:
                tri=vertices[ix][:,[0,2]]
                if tri[:,0].max()<lo[0]-.001 or tri[:,0].min()>lo[2]+.001:
                    continue
                if tri[:,1].max()<lo[1]-.001 or tri[:,1].min()>lo[3]+.001:
                    continue
                shape=Polygon(tri)
                if shape.is_valid and shape.area>1e-10:
                    clipped=shape.intersection(region)
                    if clipped.area>1e-9:role_polys[role].append(clipped)
        self.assertGreater(len(role_polys["cycle_lane"]),100)
        self.assertGreater(len(role_polys["cycle_mark"]),20)
        paving=unary_union(role_polys["paving"])
        red=unary_union(role_polys["cycle_lane"])
        white=unary_union(role_polys["cycle_mark"])
        self.assertLess(paving.area,1e-4,"Old paving remains under new bike lane")
        self.assertLess(red.intersection(white).area,1e-3,
                        "White paint physically overlaps red cycle asphalt")
        self.assertLess(red.union(white).symmetric_difference(region).area,.05,
                        "Cycle lane has a gap in the pavement or off-centre geometry")
        self.assertAlmostEqual(red.area+white.area,(X1-X0)*CYCLE_WIDTH,delta=.05)
        print("PROMENADE_Z_FIGHT_PROOF",json.dumps(dict(
            old_paving_area=paving.area,overlap_area=red.intersection(white).area,
            bike_lane_area=red.area,white_inlay_area=white.area,width=CYCLE_WIDTH,
            length=X1-X0)))


if __name__=="__main__":unittest.main()
