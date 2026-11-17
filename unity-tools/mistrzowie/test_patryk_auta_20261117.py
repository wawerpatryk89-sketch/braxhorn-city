"""Owner 17.11.2026 00:20, the answer to the night build: "Wstaw mi Patryka zamiast brax i podstaw z 3 auta z naszej kolekcji i
puszczaj". The player of the region is Patryk (the estate's private model and moves, restored only in CI; Brax only if his
import fails, said in the build), and three more cars of the estate collection stand driveable beside the Revuelto."""
import pathlib
import re
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
EXPLORER = (ED / "BraxChampionsExplorer.cs").read_text(encoding="utf-8")
PATRYK = (ED / "BraxChampionsPatryk.cs").read_text(encoding="utf-8")
CARS = (ED / "BraxChampionsKolekcjaAut.cs").read_text(encoding="utf-8")
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
sys.path.insert(0, str(TOOLS))


class Patryk(unittest.TestCase):
    def test_player_is_patryk_with_a_said_fallback(self):
        self.assertIn("var hero = Gracz(player.transform, body);", EXPLORER)
        self.assertIn("var h = BraxChampionsPatryk.PatrykBohater(player, body); Bohater = \"Patryk\";", EXPLORER)
        self.assertIn("BraxBuildQuality.Warn(\"DO POPRAWKI MISTRZOWIE_GRACZ Patryk nie wszedł, gramy Braxem: \"", EXPLORER)
        self.assertIn("report.playerCharacter=BraxChampionsExplorer.Bohater;", (ED / "BraxChampionsBuild.cs").read_text(encoding="utf-8"))

    def test_his_car_moves(self):
        for clip, src in (("Driving", "car/Driving.fbx"), ("ExitingCar", "car/Exiting Car.fbx")):
            self.assertRegex(PATRYK, r'PK\("' + clip + r'", (true|false), true, "' + re.escape(src) + r'"\)')
        self.assertIn('PK("EnteringCar", false, true, "patryk_owner/Entering Car.fbx", "car/Entering Car.fbx")', PATRYK)

    def test_restored_in_ci_only(self):
        i = WF.index("python3 unity-tools/mistrzowie/restore_private.py")
        j = WF.index("python3 unity-tools/mistrzowie/restore_player.py || echo")
        self.assertLess(i, j)
        for pack in ("patryk.tar.gz.enc", "ruchy_brax.tar.gz.enc", "ruchy_wlasciciela.tar.gz.enc", "import-patryk-20261103/",
                     "import-patryk-animations-20261104/"):
            self.assertIn("unity-city/Prywatne/" + pack, WF)
        self.assertIn("unity-city/Assets/Source/Mistrzowie/Player/", (ROOT / ".gitignore").read_text(encoding="utf-8"))

    def test_car_moves_copied_for_humanoid_import(self):
        import restore_player
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = pathlib.Path(tmp) / "src", pathlib.Path(tmp) / "ruchy"
            src.mkdir()
            for name in ("Entering Car.fbx", "Driving.fbx"):
                (src / name).write_bytes(b"Kaydara FBX Binary " + name.encode())
            files = restore_player.copy_car_moves(src, dst)
            self.assertEqual(sorted(p.name for p in files), ["Driving.fbx", "Entering Car.fbx"])
            self.assertEqual((dst / "car/Driving.fbx").read_bytes(), b"Kaydara FBX Binary Driving.fbx")


class Kolekcja(unittest.TestCase):
    def test_three_cars_beside_the_revuelto_one_bay_apart(self):
        self.assertIn("BraxChampionsKolekcjaAut.Generate(rig.transform);", EXPLORER)
        xs = [float(x) for x in re.findall(r'x = (\d+\.\d+)f', CARS)]
        self.assertEqual(len(xs), 3)
        row = sorted(xs + [645.175])
        for a, b in zip(row, row[1:]):
            self.assertAlmostEqual(b - a, 6.1, places=3)                     # one free 3.05 m bay between every two cars
        for x in row:
            self.assertAlmostEqual(((x - 31.45 - 600) / 3.05) % 1, .5, places=3)   # the centre of a painted bay

    def test_driveable_like_the_revuelto(self):
        for s in ("var car = root.AddComponent<BraxChampionsCar>();", "car.maxSpeed = 200f / 3.6f * K;", "car.rollingWheels = kola;",
                  "car.doorHinge = null;", "BraxChampionsCarBuilder.PlaceWheelsDown(copy, root.transform);"):
            self.assertIn(s, CARS)
        self.assertNotIn("CreatePrimitive", CARS)

    def test_sources_pinned_and_copied(self):
        import prepare_kolekcja_aut as p
        self.assertEqual(sorted(p.CARS), ["car_918.glb", "car_dc.glb", "car_r8.glb"])
        p.prepare(dry_run=True)
        for name in p.CARS:
            self.assertIn("public/assets/cars-src/" + name, WF)
        self.assertIn("python3 unity-tools/mistrzowie/prepare_kolekcja_aut.py || echo", WF)
        self.assertTrue((ED / "BraxChampionsKolekcjaAut.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
