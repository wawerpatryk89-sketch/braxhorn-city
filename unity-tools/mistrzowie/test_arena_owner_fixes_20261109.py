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
        self.assertIn("HallBanners(t,body);", signs)
        self.assertIn('"A1 SIGN BODY | "+label', signs)
        self.assertEqual(signs.count("CurvedSignBody(t,"), 3)
        audit = source("BraxSceneAudit.cs")
        self.assertIn('AddExact("Arena: szyldy i banery jako bryły 3D",signBodies,6,', audit)
        # Banners sit above the 16-row stands (16 x .40 = 6.4 m).
        bottom = float(re.search(r"HallBannerBottom=([\d.]+)f", signs).group(1))
        self.assertGreater(bottom, 6.4 + .165 + 5)


if __name__ == "__main__":
    unittest.main()
