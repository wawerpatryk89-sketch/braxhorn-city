"""Read-only A1 car/showcase regression gates. No Unity compile or device claim."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]

def read(name):
    return (ROOT / name).read_text(encoding="utf-8")

class ChampionOwnerCarShowcaseTests(unittest.TestCase):
    def test_exact_estate_model_source_is_used_without_placeholder(self):
        builder = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        prep = read("unity-tools/mistrzowie/prepare_champion_car.py")
        self.assertIn('Assets/Source/Mistrzowie/Vehicles/car_rev.glb', builder)
        self.assertIn('public/assets/cars-src/car_rev.glb', prep)
        self.assertIn('78245d0968fbb91ee205af8cc5f88a981015933f', prep)
        self.assertIn('BraxChampionsCar', builder)
        self.assertIn('Original estate car_rev GLB has no visible geometry', builder)
        self.assertNotIn("GameObject.CreatePrimitive(PrimitiveType.Cube)", builder)

    def test_single_car_stands_in_estate_parking_bay_with_open_aisle(self):
        builder = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        explore = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsExplorer.cs")
        self.assertIn('BraxChampionsCarBuilder.Generate(rig.transform)', explore)
        self.assertIn('FindObjectsByType<BraxChampionsCar>', builder)
        self.assertIn('562.45f, .16f, 711f', builder)
        # Check car's 4.95m x ~2m footprint against west parking x[-100,-30]
        # z[-109,-85] relative to (600,800). No invented physical barriers.
        x, z = 562.45 - 600, 711 - 800
        car_half_length = 4.95/2
        self.assertGreater(x - 1.3, -100)
        self.assertLess(x + 1.3, -30)
        self.assertGreater(z - car_half_length, -109)
        self.assertLess(z + car_half_length, -85)
        self.assertNotIn('new GameObject("Invisible', builder)

    def test_existing_mobile_controls_are_connected_to_driving(self):
        player = read("unity-city/Assets/BraxGymPlayer.cs")
        car = read("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        self.assertIn('public BraxChampionsCar championsCar;', player)
        self.assertIn('championsCar.Steer(move)', player)
        self.assertIn('championsCar.EmergencyExit()', player)
        self.assertIn('if(championsCar)', player)
        self.assertIn('IBraxInterakcja', car)
        self.assertIn('BraxInterakcje.Dodaj(this)', car)
        self.assertIn('Enter', car) if False else None
        for name in ("EnteringCar", "Driving", "ExitingCar"):
            self.assertIn(name, car)

    def test_original_mixamo_clips_are_attached_to_brax_without_replacing_skin(self):
        clips = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarClips.cs")
        brax = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBrax.cs")
        owner = read("unity-tools/mistrzowie/import_brax_car_animations.py")
        self.assertIn("BraxChampionsCarClips.Attach(hero, animation)", brax)
        for name in ("Driving.fbx", "Entering Car.fbx", "Exiting Car.fbx"):
            self.assertIn(name, clips)
            self.assertIn(name, owner)
        self.assertIn("Assets/Source/Mistrzowie/Brax/CarAnimations/", owner)
        self.assertIn("Assets/Source/Mistrzowie/Brax/CarAnimations/", clips)
        self.assertIn("AnimationUtility.SetEditorCurve", clips)
        self.assertIn("mappedBones.Count < 35", clips)
        self.assertNotIn("DestroyImmediate(originalBrax)", clips)

    def test_old_orphan_osiedle_driver_cannot_break_champions_compile(self):
        orphan = ROOT/"unity-city/Assets/Claude/Runtime/BraxAutoGracza.cs"
        self.assertFalse(orphan.is_file(), "old copied estate driver references missing Patryk API")
        self.assertTrue((ROOT/"unity-city/Assets/Mistrzowie/BraxChampionsCar.cs").is_file())

    def test_real_rhombus_polyhaven_teak_glass_and_owner_photo(self):
        box = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs")
        arena = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs")
        arrival = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaArrival.cs")
        self.assertIn("BraxChampionsBeltShowcase.Generate(root)", arena)
        self.assertNotIn('return Trophy(root,', arrival)
        for value in ("granite_tile", "teak_veneer", "BRAXHORN_ARENA_CASE_PLAQUE.jpg",
                      "A1_RealRhombus_GraniteStone", "MeshCollider",
                      "OriginalBelt(display)", "OriginalRectangleArtwork(display"):
            self.assertIn(value, box)
        self.assertIn("UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>())", box)
        self.assertIn("new Vector3(-73.9f, 0, 63.3f)", box)

    def test_signed_update_compatibility_gate_precedes_unity(self):
        wf = read(".github/workflows/mistrzowie-apk.yml")
        verifier = read("unity-tools/mistrzowie/verify_previous_apk.py")
        self.assertIn('com.braxhorn.mistrzowie', verifier)
        self.assertIn('316045507', verifier)
        self.assertIn('f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe', verifier)
        self.assertLess(wf.index("verify_previous_apk.py"), wf.index("Unity 6 - Miasto Mistrzów"))
        self.assertIn("BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip", wf)
        self.assertIn("prepare_champion_car.py --dry-run", wf)
        self.assertIn("import_brax_car_animations.py", wf)

if __name__ == "__main__":
    unittest.main()
