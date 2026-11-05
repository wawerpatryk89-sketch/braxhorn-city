"""Offline tests for verified BRAXHORN owner-source transfer and stable Unity GUIDs.

Synthetic tiny input only; real 345 MB archive hashes are checked at restore.
No characters/animations or Unity editor are needed for these tests.
"""
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile

OBOS = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(OBOS))
import owner_assets_intake as intake


def zip_bytes(data):
    mem = io.BytesIO()
    with zipfile.ZipFile(mem, "w", compression=zipfile.ZIP_STORED) as out:
        for name, value in data.items():
            out.writestr(name, value)
    return mem.getvalue()


class SourceTransferTests(unittest.TestCase):
    def test_no_zip_traversal(self):
        for bad in ("../model.glb", "/etc/passwd", "other\\..\\model.glb"):
            with self.assertRaises(ValueError):
                intake.secure_name(bad)

    def test_source_check_and_repeatable_guid(self):
        model_name = "BRAXHORN_OBOZ/MODELE/leather_punching_bag.glb"
        nested_name = "BRAXHORN_OBOZ/TEKSTURY_I_ZESTAWY/grass_medium_01_2k.blend.zip"
        nested = zip_bytes({
            "grass_medium_01_2k.blend": b"BLENDER-SOURCE-KEEP-OUTSIDE-UNITY",
            "textures/grass_medium_01_diff_2k.jpg": b"ORIGINAL-JPEG-UNTOUCHED",
            "textures/grass_medium_01_nor_gl_2k.exr": b"ORIGINAL-EXR-UNTOUCHED",
        })
        originals = {
            model_name: b"ORIGINAL-GLB-UNTOUCHED",
            nested_name: nested,
            "BRAXHORN_OBOZ/MANIFEST_MATERIALOW.json": b"{}",
        }
        archive = zip_bytes(originals)
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            source_path = root / "owner.zip"
            source_path.write_bytes(archive)
            manifest_path = root / "handoff.json"
            manifest_path.write_text(json.dumps({
                "packageBytes": len(archive),
                "packageSha256": intake.sha256(archive),
                "contents": [{
                    "path": name, "bytes": len(data), "sha256": intake.sha256(data)
                } for name, data in originals.items()],
            }), encoding="utf-8")
            target = root / "Unity" / "OwnerAssets"
            old_manifest = intake.MANIFEST
            intake.MANIFEST = manifest_path
            try:
                review = intake.run(source_path, target, apply=False)
                self.assertEqual(review["status"], "verified_only")
                self.assertFalse(target.exists())
                first = intake.run(source_path, target, apply=True)
                self.assertEqual(first["status"], "applied")
                self.assertEqual(first["source_count"], 3)
                self.assertEqual(first["unity_files"], 4)
                self.assertEqual((target/"leather_punching_bag.glb").read_bytes(), originals[model_name])
                exr = target/"grass_medium_01_2k"/"textures"/"grass_medium_01_nor_gl_2k.exr"
                self.assertEqual(exr.read_bytes(), b"ORIGINAL-EXR-UNTOUCHED")
                self.assertFalse((target/"grass_medium_01_2k.blend").exists())
                metas = {p.relative_to(root).as_posix(): p.read_bytes()
                         for p in root.rglob("*.meta")}
                self.assertTrue(metas)
                self.assertTrue((target.parent/"OwnerAssets.meta").exists())
                self.assertTrue((target/"leather_punching_bag.glb.meta").exists())
                second = intake.run(source_path, target, apply=True)
                self.assertEqual(second["new_files"], 0)
                self.assertEqual(second["existing_unchanged"], 4)
                self.assertEqual(metas, {p.relative_to(root).as_posix(): p.read_bytes()
                                        for p in root.rglob("*.meta")})
            finally:
                intake.MANIFEST = old_manifest

    def test_five_mobile_zips_equal_original_source_files(self):
        """The iPhone ZIPs are independent and preserve original source bytes."""
        chunks = [
            {"BRAXHORN_OBOZ/MODELE/leather_punching_bag.glb": b"GLB-SOURCE"},
            {"BRAXHORN_OBOZ/PANORAMY/montorfano_4k.hdr": b"HDR-SOURCE"},
            {"BRAXHORN_OBOZ/MURALE_I_SZYLD/banner.jpeg": b"JPEG-SOURCE"},
            {"BRAXHORN_OBOZ/TEKSTURY_I_ZESTAWY/grass_medium_01_2k.blend.zip":
                zip_bytes({"textures/grass_medium_01_diff_2k.jpg": b"TEXTURE-SOURCE"})},
            {"BRAXHORN_OBOZ/MANIFEST_MATERIALOW.json": b"{}"},
        ]
        with tempfile.TemporaryDirectory() as tmp:
            target=Path(tmp)/"Unity"/"OwnerAssets"
            source_manifest=Path(tmp)/"manifest.json"
            all_files={k:v for part in chunks for k,v in part.items()}
            source_manifest.write_text(json.dumps({"contents":[
                {"path":key,"bytes":len(value),"sha256":intake.sha256(value)}
                for key,value in all_files.items()
            ]}),encoding="utf-8")
            old_manifest,old_parts=intake.MANIFEST,intake.SPLIT_PARTS
            zip_files=[]
            known={}
            try:
                for index,entries in enumerate(chunks,1):
                    name=f"BRAXHORN_MODELE_CZESC_{index:02d}_Z_05.zip"
                    dest=Path(tmp)/name
                    value=zip_bytes(entries)
                    dest.write_bytes(value)
                    zip_files.append(dest)
                    known[name]=(len(value),intake.sha256(value))
                intake.MANIFEST=source_manifest
                intake.SPLIT_PARTS=known
                checked=intake.run_parts(zip_files,target,apply=False)
                self.assertEqual(checked["parts_verified"],5)
                self.assertFalse(target.exists())
                imported=intake.run_parts(zip_files,target,apply=True)
                self.assertEqual(imported["source_count"],5)
                self.assertEqual(imported["unity_files"],5)
                self.assertTrue(imported["characters_untouched"])
                self.assertEqual((target/"leather_punching_bag.glb").read_bytes(),b"GLB-SOURCE")
                again=intake.run_parts(zip_files,target,apply=True)
                self.assertEqual(again["new_files"],0)
                self.assertEqual(again["existing_unchanged"],5)
            finally:
                intake.MANIFEST=old_manifest
                intake.SPLIT_PARTS=old_parts

    def test_meta_refuses_broken_existing_guid(self):
        with tempfile.TemporaryDirectory() as tmp:
            dest = Path(tmp)
            source = dest/"leather_punching_bag.glb"
            source.write_bytes(b"original")
            (dest/"leather_punching_bag.glb.meta").write_text("guid: INVALID\n")
            with self.assertRaisesRegex(ValueError, "GUID"):
                intake.ensure_stable_meta(dest, Path(source.name))


if __name__ == "__main__":
    unittest.main()
