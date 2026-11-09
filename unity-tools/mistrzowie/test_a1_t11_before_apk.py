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
        prepare=source("unity-tools/mistrzowie/prepare_champion_car.py")
        importer=source("unity-tools/mistrzowie/import_brax_car_animations.py")
        clip=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarClips.cs")
        workflow=source(".github/workflows/mistrzowie-apk.yml")
        self.assertIn('public/assets/cars-src/car_rev.glb',prepare)
        self.assertIn('GIT_BLOB_SHA1 = "78245d0968fbb91ee205af8cc5f88a981015933f"',prepare)
        self.assertIn('public/assets/cars-src/car_rev.glb',workflow,
                      "sparse checkout must include the real estate car")
        self.assertIn('prepare_champion_car.py',workflow,
                      "workflow must copy and verify the ORIGINAL owner car before Unity")
        self.assertIn('import_brax_car_animations.py',workflow,
                      "workflow must import the three original Mixamo animations")
        self.assertIn('BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip',workflow,
                      "one version-locked owner asset wrapper should supply all A1/T11 sources")
        self.assertIn('Assets/Source/Mistrzowie/Brax/CarAnimations/',clip)
        self.assertIn('Assets/Source/Mistrzowie/Brax/CarAnimations/',importer,
                      "Animation importer and Brax runtime clip source must agree")
    def test_showcase_and_no_invisible_walls(self):
        exhibit=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs")
        arena=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs")
        arrival=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaArrival.cs")
        self.assertNotIn("BraxChampionsBeltShowcase.Generate(",arena)
        self.assertIn('Optional("gablota pasa w lobby",()=>BraxChampionsBeltShowcase.GenerateLobby(root));',arrival)
        for text in ("BRAXHORN_ARENA_CASE_PLAQUE.jpg","LibraryMarble",
                     "A1 VITRINE | black marble pillar","A1 VITRINE | FRONT glass",
                     "A1 VITRINE | brass side bracket","A1 VITRINE | gold belt pin",
                     "var belt=OriginalBelt(display,BeltLength,"):
            self.assertIn(text,exhibit)
        self.assertNotIn("REFERENCE black leather back panel",exhibit)
        self.assertNotIn("Podium(display",exhibit)
        # Decorative parts never collide; the only solid is the visible
        # marble pillar (slat wall removed by the owner 9.11).
        self.assertIn("DestroyImmediate(cube.GetComponent<Collider>())",exhibit)
        self.assertEqual(exhibit.count(",true);"),1)
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
    def test_arena_and_casino_share_one_serialized_unity_scene_without_apk(self):
        buildings=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuildings.cs")
        build=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        casino=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs")
        self.assertIn("BraxChampionsArena.Generate(root);",buildings)
        self.assertIn("BraxChampionsArenaArrival.Generate(root, materials);",buildings)
        self.assertIn("BraxChampionsArenaSigns.Generate(root);",buildings)
        self.assertIn("BraxChampionsCasinoT11.Generate(Out);",buildings)
        self.assertIn("outFolder+\"/Prefabs/T11_Casino_Exterior.prefab\"",casino)
        self.assertIn("report.buildingExteriors=BraxChampionsBuildings.Generate();",build)
        self.assertIn('if(validationOnly)VerifyArenaCasinoAssembly(scene,report,"foundation-serialized");',build)
        self.assertIn('if(validationOnly)VerifyArenaCasinoAssembly(scene,report,"exploration-serialized");',build)
        self.assertIn("BraxChampionsExplorer.Generate(report.sourceCommit);",build)
        self.assertIn("GetRootGameObjects()",build)
        self.assertIn("arenaSigns!=4||casinoSigns!=1||casinoDollars!=1",build)
        self.assertIn("A1_T11_SAME_SCENE_PASS",build)
        self.assertIn("car_checked_separately_in_exploration=true APK_NOT_BUILT=true",build)
        gate=build[build.index("static void VerifyArenaCasinoAssembly("):
                   build.index("static int RecordSourceImportDiagnostics(")]
        self.assertNotIn("BuildPlayer",gate)
        self.assertNotIn("workflow_dispatch",gate)

    def test_exact_car_audio_and_120_kph_are_in_the_same_serialized_explore_scene(self):
        build=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuild.cs")
        explore=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsExplorer.cs")
        car=source("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        builder=source("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        self.assertIn("BraxChampionsCarBuilder.Generate(rig.transform);",explore)
        self.assertIn('if(validationOnly)VerifyArenaCasinoAssembly(scene,report,"exploration-serialized");',build)
        self.assertIn("if(validationOnly)VerifyArenaCasinoCarAssembly(scene,report);",build)
        self.assertIn("static void VerifyArenaCasinoCarAssembly(Scene scene,Report report)",build)
        self.assertIn("GetComponentsInChildren<BraxChampionsCar>(true)",build)
        self.assertIn("cars.Length!=1",build)
        self.assertIn("car.bodyCollider.bounds.size.z-BraxChampionsCarBuilder.WorldLengthMetres",build)
        self.assertIn("car.maxSpeed*3.6f/Mathf.Max(.01f,car.worldScale)-120f",build)
        self.assertIn("BraxChampionsCarBuilder.EngineIdleAudio",build)
        self.assertIn("BraxChampionsCarBuilder.EngineRevAudio",build)
        self.assertIn("A1_T11_CAR_SAME_SCENE_PASS",build)
        self.assertIn("public float maxSpeed = 120f / 3.6f",car)
        self.assertIn("const float LengthMetres = 4.95f;",builder)
        # Owner 9.11: no 6 m cap, "wszystko ma być odpowiednie pod skalę, też
        # auto" -> real 4.95 m x K like every other object.
        self.assertIn("public static float WorldLengthMetres => LengthMetres * K;",builder)
        self.assertNotIn("OWNER_CAP_6M",builder)
        self.assertIn('car.engineIdleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EngineIdleAudio);',builder)
        self.assertIn('car.engineRevClip = AssetDatabase.LoadAssetAtPath<AudioClip>(EngineRevAudio);',builder)
        gate=build[build.index("static void VerifyArenaCasinoCarAssembly("):
                   build.index("static int RecordSourceImportDiagnostics(")]
        self.assertNotIn("BuildPipeline.BuildPlayer",gate)
        self.assertNotIn("workflow_dispatch",gate)

    def test_no_unsafe_masking_of_compilation_failures(self):
        w=source(".github/workflows/mistrzowie-apk.yml")
        self.assertIn("apk_delivery.py preflight",w)
        self.assertIn("BraxChampionsBuild.BuildAndroidWithoutWalkChecks",w)
        self.assertNotIn("continue-on-error: true\n        run: python3 unity-tools/mistrzowie/apk_delivery.py preflight",w)
    def test_python_importer_compiles(self):
        paths=[
            "unity-tools/mistrzowie/prepare_champion_car.py",
            "unity-tools/mistrzowie/import_brax_car_animations.py",
            "unity-tools/mistrzowie/import_selected_a1_t11.py",
            "unity-tools/mistrzowie/verify_previous_apk.py"
        ]
        for path in paths:
            with self.subTest(script=path):
                compile(source(path),path,"exec")
if __name__=="__main__":
    unittest.main(verbosity=2)
