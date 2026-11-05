"""Fast, synthetic-only checks for PBR channel packing. No private model data."""
import tempfile
import unittest
from pathlib import Path

import pbr_pack


class PbrPackTests(unittest.TestCase):
    def test_pack_preserves_red_channels_in_linear_data(self):
        from PIL import Image
        metallic = Image.new("RGB", (2, 2), (17, 211, 119))
        rough = Image.new("RGB", (2, 2), (237, 27, 22))
        packed = pbr_pack.combine_channels(metallic.getchannel("R"),
                                           rough.getchannel("R"))
        self.assertEqual(packed.mode, "RGB")
        self.assertEqual(packed.getpixel((0, 0)), (17, 237, 255))

    def test_packed_files_have_deterministic_paths_and_guid(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            path = root / pbr_pack.OUTPUT / "monika-MR.png"
            path.parent.mkdir(parents=True)
            self.assertTrue(pbr_pack.atomic_write(path, b"test"))
            self.assertFalse(pbr_pack.atomic_write(path, b"test"))
            pbr_pack.make_meta(path, root)
            initial = path.with_name(path.name + ".meta").read_bytes()
            pbr_pack.make_meta(path, root)
            self.assertEqual(initial, path.with_name(path.name + ".meta").read_bytes())
            self.assertEqual(len(initial.split(b"guid: ")[1].strip()), 32)

    def test_source_path_is_restricted_to_unity_sources(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with self.assertRaises(ValueError):
                pbr_pack.source(root, "Assets/../secrets.txt")
            with self.assertRaises(ValueError):
                pbr_pack.source(root, "Assets/PlayableGenerated/payload.png")


if __name__ == "__main__":
    unittest.main()
