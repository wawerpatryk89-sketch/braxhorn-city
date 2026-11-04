import json
from pathlib import Path
import tempfile
import unittest

import pipeline


class PipelineTests(unittest.TestCase):
    def setUp(self):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        self.root = Path(tmp.name)
        self.config = json.loads(Path(__file__).with_name('unity-build.json').read_text())
        path = self.root/'unity-tools/mistrzowie/unity-build.json'
        path.parent.mkdir(parents=True)
        path.write_text(json.dumps(self.config))
        self.version = self.root/'unity-city/ProjectSettings/ProjectVersion.txt'
        self.version.parent.mkdir(parents=True)
        self.version.write_text(f"m_EditorVersion: {self.config['unity']}\n"
                               f"m_EditorVersionWithRevision: {self.config['unity']} ({self.config['revision']})\n")

    def test_current_launcher_and_project_agree(self):
        result = pipeline.check_launcher(self.root, {'GITHUB_ACTIONS':'true',
                                                    'MISTRZOWIE_PIPELINE':self.config['pipeline']})
        self.assertEqual(result['unity'], self.config['unity'])

    def test_old_main_launcher_fails_before_source_preparation(self):
        with self.assertRaisesRegex(RuntimeError, 'Use workflow from'):
            pipeline.check_launcher(self.root, {'GITHUB_ACTIONS':'true'})

    def test_outdated_launcher_protocol_is_rejected(self):
        with self.assertRaisesRegex(RuntimeError, 'Use workflow from'):
            pipeline.check_launcher(self.root, {'GITHUB_ACTIONS':'true','MISTRZOWIE_PIPELINE':'old'})

    def test_repository_only_local_preparation_does_not_require_github(self):
        self.assertEqual(pipeline.check_launcher(self.root, {})['unity'], self.config['unity'])

    def test_editor_version_drift_fails(self):
        self.version.write_text('m_EditorVersion: 6000.0.58f2\n')
        with self.assertRaisesRegex(RuntimeError, 'disagree'):
            pipeline.configuration(self.root)

    def test_revision_drift_fails(self):
        self.version.write_text(f"m_EditorVersion: {self.config['unity']}\n")
        with self.assertRaisesRegex(RuntimeError, 'revision'):
            pipeline.configuration(self.root)

    def test_missing_owner_animation_module_is_reported_before_downloads(self):
        for relative in pipeline.REQUIRED:
            path = self.root/relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('fixture')
        pipeline.check_checkout(self.root)
        (self.root/'unity-tools/claude/import_patryk_animations.py').unlink()
        with self.assertRaisesRegex(RuntimeError, 'import_patryk_animations.py'):
            pipeline.check_checkout(self.root)


if __name__ == '__main__':
    unittest.main()
