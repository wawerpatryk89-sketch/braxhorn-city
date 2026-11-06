"""Small offline tests of the private-source selection/restore boundary. No Unity."""
import hashlib
import importlib.util
import io
from pathlib import Path
import tarfile
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare_fighters.py'
spec = importlib.util.spec_from_file_location('wyspa_fighters', SCRIPT)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class FighterDelivery(unittest.TestCase):
    def test_selected_sources_are_two_camp_models_and_animations(self):
        _, _, selected, packs = module.selection()
        models = [n for n in selected if n.startswith('characters/') and n.endswith('.fbx')]
        self.assertEqual({n.split('/')[1] for n in models}, {'rakk', 'zyro'})
        self.assertEqual(len(models), 2)
        self.assertTrue(all(n.startswith(('characters/rakk/', 'characters/zyro/', 'animations/')) for n in selected))
        self.assertTrue(all(any(r['path'] in selected for r in group) for _, group in packs))
        self.assertEqual(len(packs), 3)

    def archive(self, folder, entries):
        path = folder / 'inputs.tar.gz'
        with tarfile.open(path, 'w:gz') as archive:
            for name, data in entries:
                info = tarfile.TarInfo(name); info.size = len(data)
                archive.addfile(info, io.BytesIO(data))
        return path

    def record(self, name, data):
        return dict(path=name, bytes=len(data), sha256=hashlib.sha256(data).hexdigest())

    def test_unselected_camp_environment_is_not_extracted(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); stage = root/'stage'; stage.mkdir()
            a = self.record('characters/kade/model.fbx', b'fighter')
            b = self.record('environment/forest.glb', b'forest')
            archive = self.archive(root, [(a['path'], b'fighter'), (b['path'], b'forest')])
            seen = set(); module.extract_selected(archive, stage, [a,b], {a['path']:a}, seen)
            self.assertEqual(seen, {a['path']})
            self.assertEqual((stage/a['path']).read_bytes(), b'fighter')
            self.assertFalse((stage/b['path']).exists())

    def test_tampered_source_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); stage = root/'stage'; stage.mkdir()
            item = self.record('animations/clip.fbx', b'correct')
            archive = self.archive(root, [(item['path'], b'changed')])
            with self.assertRaisesRegex(ValueError, 'bytes'):
                module.extract_selected(archive, stage, [item], {item['path']:item}, set())

    def test_unknown_path_and_duplicate_member_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); stage = root/'stage'; stage.mkdir()
            item = self.record('animations/clip.fbx', b'a')
            for entries in [[('../escape', b'a')], [(item['path'], b'a'), (item['path'], b'a')]]:
                archive = self.archive(root, entries)
                with self.assertRaisesRegex(ValueError, 'Unexpected/duplicate'):
                    module.extract_selected(archive, stage, [item], {}, set())

    def test_missing_original_package_member_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); stage = root/'stage'; stage.mkdir()
            item = self.record('animations/clip.fbx', b'a')
            archive = self.archive(root, [])
            with self.assertRaisesRegex(ValueError, 'Incomplete'):
                module.extract_selected(archive, stage, [item], {item['path']:item}, set())


if __name__ == '__main__':
    unittest.main()
