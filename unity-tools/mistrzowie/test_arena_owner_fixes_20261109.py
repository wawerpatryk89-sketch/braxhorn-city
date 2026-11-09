#!/usr/bin/env python3
"""Owner phone proofs 9.11 (arena): lying VIP chairs, 3D signs, hall walls."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def source(name):
    return (EDITOR / name).read_text(encoding="utf-8")


class ArenaOwnerFixes20261109(unittest.TestCase):
    def test_vip_chairs_keep_imported_root_rotation(self):
        # glTFast keeps the Sketchfab -90 X matrix on the instance root; the
        # facing yaw must be composed on top, never written over it.
        vip = source("BraxChampionsVIPSeats.cs")
        self.assertIn("var imported=chair.transform.localRotation;", vip)
        self.assertIn("chair.transform.localRotation=yaw*imported;", vip)
        self.assertNotIn("chair.transform.localRotation=facing;", vip)
        self.assertIn("AuthoredHeightUnits=73.6f", vip)
        self.assertIn("CHAMPIONS_VIP_UPRIGHT_FAILED height=", vip)
        # Straight rows square to the cage.
        self.assertIn("var towardCage = new Vector3(0, 0, cz - z);", vip)

    def test_sign_silhouettes_match_pinned_owner_pngs(self):
        sil = source("BraxChampionsArenaSignSilhouettes.cs")
        verify = (ROOT / "unity-tools/mistrzowie/verify_champion_assets.py").read_text(encoding="utf-8")
        for name, digest in re.findall(r"// (champion_arena_\w+\.png) 1536x256 sha256=([0-9a-f]{64})", sil):
            self.assertIn(f'"ArenaSigns/{name}":"{digest}"', verify)
        self.assertEqual(len(re.findall(r"static readonly float\[\] S\d=", sil)), 4)

    def test_every_sign_and_hall_banner_is_a_3d_body(self):
        signs = source("BraxChampionsArenaSigns.cs")
        # Owner 9.11 23:45: "co ten szyld robi w środku, w arenie, ten mój z zewnątrz" - no exterior sign inside.
        self.assertNotIn("\n        HallBanners(t,body);", signs)
        self.assertIn("hallBanners=REMOVED", signs)
        self.assertIn('"A1 SIGN BODY | "+label', signs)
        self.assertEqual(signs.count("CurvedSignBody(t,"), 3)
        audit = source("BraxSceneAudit.cs")
        self.assertIn('AddExact("Arena: szyldy jako bryły 3D",signBodies,4,', audit)

    def test_trophy_cases_use_real_lathe_cups(self):
        trophies = source("BraxChampionsArenaTrophies.cs")
        arrival = source("BraxChampionsArenaArrival.cs")
        self.assertIn("EntranceTrophyCases(arenaRoot, exterior)", arrival)
        # Closed profile: starts and ends on the axis, cup 0.598 m on 0.12 m plinth.
        pts = re.findall(r"new Vector2\(([\d.]+)f,([\d.]+)f\)", trophies.split("CupProfile={")[1].split("};")[0])
        self.assertEqual(float(pts[0][0]), 0.0)
        self.assertEqual(float(pts[-1][0]), 0.0)
        self.assertAlmostEqual(max(float(y) for _, y in pts), 0.598, places=3)
        self.assertIn("const float CupMetalHeight=.598f, CupPlinth=.12f;", trophies)
        # Owner 9.11: different designs, not one.
        for profile in ("UrnProfile", "GlobeProfile", "BowlProfile", "GobletProfile"):
            self.assertIn("static readonly Vector2[] " + profile + "={", trophies)
        self.assertIn("switch(design%5)", trophies)
        self.assertIn("A1_trophy_polished_gold.mat", trophies)
        # Engraved plates on both plinth faces, atlas committed with its generator.
        atlas = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaTrophies/trophy_plaques_atlas.png"
        self.assertTrue(atlas.is_file())
        self.assertEqual(atlas.read_bytes()[16:24], (2048).to_bytes(4, "big") + (512).to_bytes(4, "big"))
        self.assertIn("PlaqueMesh(plate%8)", trophies)
        self.assertIn('TrophyFolder="/Source/Mistrzowie/ArenaTrophies/"', source("BraxChampionsArenaLobbyTextureImport.cs"))


if __name__ == "__main__":
    unittest.main()
