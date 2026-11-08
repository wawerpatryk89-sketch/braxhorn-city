"""Offline contract for exact owner GitHub Release mural names.

This does not fetch, alter or regenerate owner images, run Unity, or build APK.
"""
import unittest

import install_arena_murals_hd as mural


class ExactOwnerReleaseNames(unittest.TestCase):
    def setUp(self):
        self.assets = [
            {"id": index, "name": mural.RELEASE_FILES[target], "state": "uploaded",
             "digest": "sha256:" + mural.FILES[target]}
            for index, target in enumerate(mural.FILES, 100)
        ]

    def test_owner_filename_to_wall_mapping(self):
        self.assertEqual(mural.RELEASE_FILES, {
            "MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png": "IMG_0266.png",
            "MURAL_LEWA_ZAWODNICY_6144x2048.png": "IMG_0264.png",
            "MURAL_PRAWA_PAS_6144x2048.png": "IMG_0265.png",
        })
        self.assertEqual(set(mural.FILES), set(mural.RELEASE_FILES))
        self.assertEqual(mural.FILES["MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png"],
                         "5d30aa995bb0da043a34c282fb452743f1b04b9f4584abf2be925ea29a461a73")
        self.assertEqual(mural.FILES["MURAL_LEWA_ZAWODNICY_6144x2048.png"],
                         "0805d4170b696236dba7a37c89fc4415ee21a87b316c1d58734bac212cb11a2c")
        self.assertEqual(mural.FILES["MURAL_PRAWA_PAS_6144x2048.png"],
                         "b134858b5ba16dc85089642eb1f7432ef5c76e036a8d313805947923b7661304")

    def test_accepts_only_three_exact_names_and_matching_sha(self):
        selected = mural.select_assets(self.assets)
        self.assertEqual(len(selected), 3)
        for target, upload in mural.RELEASE_FILES.items():
            self.assertEqual(selected[target]["name"], upload)
            self.assertEqual(selected[target]["sha256"], mural.FILES[target])

    def test_rejects_old_renamed_assets_even_if_sha_matches(self):
        assets = [dict(asset) for asset in self.assets]
        assets[0]["name"] = "Mural1.png"
        self.assertEqual(len(mural.select_assets(assets)), 2)

    def test_rejects_correct_name_with_wrong_image(self):
        assets = [dict(asset) for asset in self.assets]
        assets[0]["digest"] = assets[1]["digest"]
        with self.assertRaisesRegex(ValueError, "wrong SHA-256"):
            mural.select_assets(assets)

    def test_rejects_duplicate_owner_filename(self):
        assets = self.assets + [dict(self.assets[0])]
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            mural.select_assets(assets)


if __name__ == "__main__":
    unittest.main()
