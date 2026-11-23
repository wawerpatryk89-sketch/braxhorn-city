"""Owner 23.11.2026 15:51, his photo: two cars of the collection on the A1 east car park stood on their tails, ~25 m tall.
The estate's car_918 and car_dc GLBs are 4.5-5 cm long (a Sketchfab root matrix lays them down), PlaceWheelsDown skipped
every candidate pose of anything under 2 cm - so they kept the reset (standing) pose and were scaled by their height to
the real length. The threshold is relative now, and a car goes into the game only lying on its wheels at its real size."""
import itertools
import json
import pathlib
import struct
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
BUILDER = (ED / "BraxChampionsCarBuilder.cs").read_text(encoding="utf-8")
KOLEKCJA = (ED / "BraxChampionsKolekcjaAut.cs").read_text(encoding="utf-8")
CARS = ROOT / "public/assets/cars-src"


def mesh_extent(path):
    """the car's extent (x, y, z) in the GLB's own frame with every node transform applied (what a viewer shows)"""
    import numpy as np
    d = path.read_bytes(); n = struct.unpack("<I", d[12:16])[0]; j = json.loads(d[20:20 + n]); nodes = j["nodes"]

    def local(nd):
        if "matrix" in nd:
            return np.array(nd["matrix"], float).reshape(4, 4).T
        m = np.eye(4)
        if "scale" in nd:
            m = np.diag(list(nd["scale"]) + [1]) @ m
        if "rotation" in nd:
            x, y, z, w = nd["rotation"]
            r = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                          [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                          [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
            t = np.eye(4); t[:3, :3] = r; m = t @ m
        if "translation" in nd:
            t = np.eye(4); t[:3, 3] = nd["translation"]; m = t @ m
        return m
    lo = np.full(3, 1e18); hi = np.full(3, -1e18)

    def walk(i, p):
        nonlocal lo, hi
        nd = nodes[i]; w = p @ local(nd)
        if "mesh" in nd:
            for prim in j["meshes"][nd["mesh"]]["primitives"]:
                a = j["accessors"][prim["attributes"]["POSITION"]]
                for c in itertools.product(*zip(a["min"], a["max"])):
                    v = w @ np.array(list(c) + [1.0]); lo = np.minimum(lo, v[:3]); hi = np.maximum(hi, v[:3])
        for c in nd.get("children", []):
            walk(c, w)
    for r in j["scenes"][j.get("scene", 0)]["nodes"]:
        walk(r, np.eye(4))
    return hi - lo


class AutaKolekcji(unittest.TestCase):
    def test_the_threshold_is_relative(self):
        self.assertNotIn("if(b.size.x<.02f||b.size.y<.02f||b.size.z<.02f)continue;", BUILDER)
        self.assertIn("if(big<=1e-6f||b.size.x<big*.05f||b.size.y<big*.05f||b.size.z<big*.05f)continue;", BUILDER)
        self.assertIn('throw new InvalidOperationException("A1_OWNER_CAR_ORIENTATION: no candidate pose has measurable bounds");', BUILDER)

    def test_a_car_must_lie_on_its_wheels_at_its_real_size(self):
        self.assertIn("bounds.size.z >= 3.8f * K && bounds.size.z <= 5.6f * K && bounds.size.y < bounds.size.z * .45f && bounds.size.x < bounds.size.z * .6f", KOLEKCJA)
        self.assertIn('throw new InvalidOperationException("KOLEKCJA_AUT_NIE_STOI_NA_KOLACH "', KOLEKCJA)
        # the failure is reported (DO POPRAWKI) and the car stays out of the game
        self.assertIn('BraxBuildQuality.Warn("DO POPRAWKI KOLEKCJA_AUT "', KOLEKCJA)
        self.assertIn("if (root) UnityEngine.Object.DestroyImmediate(root);", KOLEKCJA)

    @unittest.skipUnless((CARS / "car_918.glb").exists(), "estate car sources not in this checkout")
    def test_the_tiny_sources_now_get_a_pose(self):
        try:
            import numpy  # noqa: F401
        except ImportError:
            self.skipTest("numpy")
        for name, real in (("car_918", 4.64), ("car_dc", 5.0), ("car_r8", 4.43)):
            w, h, l = (float(v) for v in mesh_extent(CARS / (name + ".glb")))
            self.assertGreater(l, w); self.assertGreater(l, h)           # lying down, front-back along z, in the source
            for dims in itertools.permutations((w, h, l)):               # every right-angle pose the builder tries
                big = max(dims)
                self.assertTrue(dims[0] < .02 or dims[1] < .02 or dims[2] < .02 or name == "car_r8",
                                name + ": the old absolute 2 cm skipped every pose of this source")
                self.assertFalse(min(dims) < big * .05, name + ": the relative test keeps every pose")
            # scaled by its real length the car is about 1.2-1.5 m high, not the ~25 m of the standing pose
            self.assertLess(h / l * real, 1.6)
            self.assertGreater(l / h * real, 15)


if __name__ == "__main__":
    unittest.main()
