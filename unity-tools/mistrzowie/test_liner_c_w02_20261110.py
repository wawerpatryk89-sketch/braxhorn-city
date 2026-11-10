"""Owner 10.11 ("Po kasynie dokończ plaża port"): the owner's early-1900 liner (model C) at berth W02, real size."""
import json, struct, unittest
from pathlib import Path

from port_layout import BERTHS, CRUISE_C_LEN, CRUISE_QUAY_X0

ROOT = Path(__file__).resolve().parents[2]
GLB = ROOT / "unity-city/Assets/Source/HarborBeach/Models/cruise_liner_1900.glb"
H = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsHarbour.cs"


class LinerC(unittest.TestCase):
    def test_model_exterior_only_and_credited(self):
        b = GLB.read_bytes()
        n = struct.unpack("<I", b[12:16])[0]
        js = json.loads(b[20:20 + n])
        self.assertIn("CC-BY-4.0", js["asset"]["extras"]["license"])
        self.assertLess(len(b), 16 * 1024 * 1024)                       # 14 MB exterior, not the 215 MB original
        attrs = {k for m in js["meshes"] for p in m["primitives"] for k in p["attributes"]}
        self.assertEqual(attrs, {"POSITION", "NORMAL", "TEXCOORD_0"})  # glTFast imports it (no UV2/3)
        names = {nd.get("name") for nd in js["nodes"]}
        self.assertIn("Object_19", names)                                # the hull
        self.assertIn("Object_1220", names)                              # the stern ensign
        credits = (ROOT / "unity-city/Assets/Source/HarborBeach/CREDITS.md").read_text(encoding="utf-8")
        self.assertIn("cruise_liner_1900.glb", credits)
        self.assertIn("Mixmamo.studio", credits)

    def test_placed_at_w02(self):
        h = H.read_text(encoding="utf-8")
        self.assertIn('public const string LinerFile="cruise_liner_1900.glb";', h)
        self.assertIn('if(bay.kind=="cruise_ship"&&bay.id=="W02"){placed+=PlaceLinerC(boats,bay);continue;}', h)
        self.assertIn("public const float LinerHull=118.0f,LinerLengthOverall=123.7f;", h)
        w02 = next(b for b in BERTHS if b["id"] == "W02")
        self.assertAlmostEqual(w02["length"] if "length" in w02 else CRUISE_C_LEN, CRUISE_C_LEN, places=1)
        self.assertLess(w02["x"], CRUISE_QUAY_X0)                        # west face of the cruise pier


if __name__ == "__main__":
    unittest.main()
