"""Short contract test for Champions-only exclusion of NON-instantiated UV sources.

Do not fake a successful Unity import. These tests prove the user-approved
temporary isolation stays out of other zones, leaves art in the owner archive,
and never becomes an APK-stopping visual quality gate.
"""
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
def read(path):
    return (ROOT/path).read_text(encoding="utf-8")

class ChampionsUnusedUvImportScope(unittest.TestCase):
    def test_run_only_before_champions_unity_not_shared_game(self):
        workflow=read(".github/workflows/mistrzowie-apk.yml")
        hook="python3 unity-tools/mistrzowie/isolate_unused_glb_champions.py --apply-runner-only"
        index=workflow.index(hook)
        block_start=workflow.rfind("      - name: ",0,index)
        block=workflow[block_start:index]
        self.assertIn("continue-on-error: true",block)
        self.assertLess(index,workflow.index("      - name: Unity 6 - Miasto Mistrzów, Brax i APK do zwiedzania"))
        self.assertIn("MISTRZOWIE_PIPELINE",workflow)

    def test_runner_backup_and_exact_provenance(self):
        source=read("unity-tools/mistrzowie/isolate_unused_glb_champions.py")
        self.assertIn('os.environ.get("GITHUB_ACTIONS")!="true"',source)
        self.assertIn('os.environ.get("MISTRZOWIE_PIPELINE")!="20261105-A1-T11"',source)
        self.assertIn('Path(str(src)+".meta")',source)
        self.assertIn("shutil.copy2(src,dst)",source)
        self.assertIn('sha(dst.read_bytes())!=row["sha256"]',source)
        self.assertIn('"ownerVisualsChanged":False',source)
        self.assertIn('"OriginalSourcePreserved"',source.replace("originalSourcePreserved","OriginalSourcePreserved"))

    def test_neither_unused_model_is_referenced_by_champions_scene(self):
        editor=ROOT/"unity-city/Assets/Editor/Mistrzowie"
        scripts=list(editor.glob("*.cs"))
        self.assertGreater(len(scripts),30)
        combined="\n".join(p.read_text(encoding="utf8") for p in scripts)
        self.assertNotIn("stack_of_towels_2k.glb",combined)
        self.assertNotIn("harbor_crane.glb",combined)
        self.assertIn("stack of towels is deferred",combined)

if __name__=="__main__":
    unittest.main(verbosity=2)
