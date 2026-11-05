#!/usr/bin/env python3
"""Read-only, no-Unity audit for A1 Arena + Casino T11 update and drivable car.
No APK, scene generation, push, deployment, signing, or material modification.
"""
import json
from pathlib import Path
import re
import sys
import unittest
ROOT=Path(__file__).resolve().parents[2]
def source(p):
    path=ROOT/p
    if not path.exists(): raise AssertionError("Required source missing: "+p)
    return path.read_text(encoding="utf-8")
class A1T11Preflight(unittest.TestCase):
    def test_original_single_city_branch(self):
        work=source(".github/workflows/mistrzowie-apk.yml")
        self.assertIn("gpt/miasto-mistrzow-kasyno-t11-20261105",work)
        self.assertIn("inputs.etap != 'aktualizacja'",work)
        self.assertIn("verify_previous_apk.py",work)
    def test_skeleton_and_vehicle_clips_are_wired(self):
        brax=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBrax.cs")
        clips=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarClips.cs")
        builder=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsExplorer.cs")
        self.assertIn("BraxChampionsCarClips.Attach(hero, animation)",brax)
        self.assertIn("BraxChampionsCarBuilder.Generate(rig.transform)",builder)
        for name in ("EnteringCar","Driving","ExitingCar"):
            self.assertIn(name,clips)
        self.assertIn("ModelImporterAnimationType.Legacy",clips)
    def test_player_car_control_hook(self):
        player=source("unity-city/Assets/BraxGymPlayer.cs")
        hud=source("unity-city/Assets/Claude/BraxTouchControls.cs")
        car=source("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        self.assertIn("public BraxChampionsCar championsCar;",player)
        self.assertIn("championsCar.Steer(move)",player)
        self.assertIn("championsCar.EmergencyExit()",player)
        self.assertIn("!player.championsCar",hud)
        self.assertIn("IBraxInterakcja",car)
        self.assertIn("BraxInterakcje.Dodaj(this)",car)
        self.assertIn("Rigidbody",car)
        self.assertIn("Physics.",car)
    def test_owner_car_source_matches_pipeline(self):
        prepare=source("unity-tools/mistrzowie/prepare_car_and_mixamo.py")
        clip=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarClips.cs")
        workflow=source(".github/workflows/mistrzowie-apk.yml")
        self.assertIn('public/assets/cars-src/car_rev.glb',prepare)
        self.assertIn('CAR_GIT_BLOB = "78245d0968fbb91ee205af8cc5f88a981015933f"',prepare)
        self.assertIn('public/assets/cars-src/car_rev.glb',workflow,
                      "sparse checkout must include the real estate car")
        self.assertIn('prepare_car_and_mixamo.py',workflow,
                      "workflow must verify+import owner model and Mixamo before Unity")
        self.assertIn('Assets/Source/Mistrzowie/Vehicles/Animations/',clip,
                      "Mixamo clip paths must match prepare_car_and_mixamo.py")
    def test_showcase_and_no_invisible_walls(self):
        exhibit=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs")
        arena=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs")
        self.assertIn("BraxChampionsBeltShowcase.Generate(root)",arena)
        for text in ("BRAXHORN_ARENA_CASE_PLAQUE.jpg","granite_tile","teak_veneer",
                     "OriginalBelt(display)", "A1_Rhombus_GraniteInsetTop"):
            self.assertIn(text,exhibit)
        self.assertIn("DestroyImmediate(cube.GetComponent<Collider>())",exhibit)
    def test_approval_only_quality_gates(self):
        build=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        gate=source("unity-city/Assets/Editor/BraxBuildQuality.cs")
        self.assertIn("BraxBuildQuality.Issue",build)
        self.assertIn("if (!Advisory) throw problem",gate)
        self.assertIn("quality gates are advisory",gate)
    def test_same_apk_identity_gate(self):
        check=source("unity-tools/mistrzowie/verify_previous_apk.py")
        self.assertIn("com.braxhorn.mistrzowie",check)
        self.assertIn("316045507",check)
        self.assertIn("f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe",check)
    def test_no_unsafe_masking_of_compilation_failures(self):
        w=source(".github/workflows/mistrzowie-apk.yml")
        self.assertIn("apk_delivery.py preflight",w)
        self.assertIn("BraxChampionsBuild.BuildAndroidWithoutWalkChecks",w)
        self.assertNotIn("continue-on-error: true\n        run: python3 unity-tools/mistrzowie/apk_delivery.py preflight",w)
    def test_python_importer_compiles(self):
        paths=[
            "unity-tools/mistrzowie/prepare_car_and_mixamo.py",
            "unity-tools/mistrzowie/import_selected_a1_t11.py",
            "unity-tools/mistrzowie/verify_previous_apk.py"
        ]
        for path in paths:
            with self.subTest(script=path):
                compile(source(path),path,"exec")
if __name__=="__main__":
    unittest.main(verbosity=2)
