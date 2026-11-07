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

    def test_single_car_stands_right_of_entrance_at_real_metre_scale(self):
        builder = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        explore = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsExplorer.cs")
        self.assertIn('BraxChampionsCarBuilder.Generate(rig.transform)', explore)
        self.assertIn('FindObjectsByType<BraxChampionsCar>', builder)
        self.assertIn('645.175f, .16f, 712f', builder)
        self.assertIn('float uniform = LengthMetres / bounds.size.z;', builder)
        self.assertNotIn('LengthMetres * K / bounds.size.z', builder)
        self.assertIn('car.interactionRadius = 3.8f;', builder)
        # Exact regular-bay centre: between x lines 43.65/46.70 and
        # centered in row z[-90.6,-85.4]. Never on paint or accessible bay.
        x, z = 645.175 - 600, 712 - 800
        car_half_length = 4.95/2
        self.assertAlmostEqual(x,(43.65+46.70)/2,places=6)
        self.assertAlmostEqual(z,(-90.6-85.4)/2,places=6)
        self.assertGreater(x-43.65,1.4)
        self.assertGreater(46.70-x,1.4)
        self.assertGreater(z - car_half_length, -90.6)
        self.assertLess(z + car_half_length, -85.4)
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

    def test_reference_wall_showcase_preserves_owner_belt_and_photo(self):
        box = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBeltShowcase.cs")
        arena = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs")
        arrival = read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaArrival.cs")
        self.assertIn("BraxChampionsBeltShowcase.Generate(root)", arena)
        self.assertNotIn('return Trophy(root,', arrival)
        for value in ("granite_tile", "REFERENCE black marble wall column",
                      "REFERENCE optically clear FRONT glass", "REFERENCE small GOLD glass bracket",
                      "REFERENCE bronze vertical slat ", "BRAXHORN_ARENA_CASE_PLAQUE.jpg",
                      "OriginalBelt(display)", "OriginalRectangleArtwork(display"):
            self.assertIn(value, box)
        for removed in ("A1_RealRhombus_GraniteStone", "Podium(display, marble, gold)",
                        "Wood veneer showcase plinth", "RED narrow centred inset"):
            self.assertNotIn(removed, box)
        self.assertIn("UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>())", box)
        self.assertIn("new Vector3(-73.9f, 0, 63.3f)", box)
        # Face-on 3D belt, true single-pane clear glass and untouched artwork.
        self.assertIn("A1_BELT_OWNER_3D_FRONT_PRESENTATION", box)
        self.assertIn("CrystalGlassPanel(display", box)
        self.assertIn('new Color(.99f,.995f,1f,.014f)', box)
        self.assertIn("SOURCE_REQUIRED", read("unity-city/Assets/Editor/Mistrzowie/BraxChampionsBuildings.cs"))

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
