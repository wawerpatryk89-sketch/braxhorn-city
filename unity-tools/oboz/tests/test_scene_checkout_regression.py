"""Exercise actual Git non-cone checkout rules before the expensive scene job."""
from pathlib import Path
import re
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = ROOT / ".github/workflows/oboz-unity-build.yml"


class SceneCheckoutRegression(unittest.TestCase):
    def test_every_source_test_job_receives_root_test_dependencies(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")
        jobs = re.split(r"\n  (?=[a-z][a-z0-9-]+:\n)", workflow)
        for job in jobs:
            if "unittest discover -s unity-tools/oboz/tests" not in job:
                continue
            block = re.search(r"sparse-checkout: \|\n((?:            [^\n]+\n)+)", job)
            self.assertIsNotNone(block, job.splitlines()[0])
            patterns = "\n".join(line.strip() for line in block.group(1).splitlines())
            with self.subTest(job=job.splitlines()[0]), tempfile.TemporaryDirectory() as tmp:
                folder = Path(tmp)
                def git(*args, input=None):
                    return subprocess.run(["git", *args], cwd=folder, input=input,
                        text=True, check=True, capture_output=True)
                git("init", "-q")
                for name in (".gitignore", "unity-tools/oboz/tests/test_contract.py",
                             "unity-city/Assets/Editor/Oboz/Fixture.cs"):
                    path = folder / name
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text("fixture\n", encoding="utf-8")
                git("add", ".")
                git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                    "commit", "-qm", "fixture")
                git("sparse-checkout", "init", "--no-cone")
                git("sparse-checkout", "set", "--no-cone", "--stdin", input=patterns+"\n")
                self.assertEqual((folder / ".gitignore").read_text(), "fixture\n")
                self.assertTrue((folder / "unity-tools/oboz/tests/test_contract.py").is_file())

    def test_revision_guard_rejects_missing_or_changed_commit(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")
        script = workflow.split("<<'PY_REVISION'\n", 1)[1].split("          PY_REVISION", 1)[0]
        script = "\n".join(line[10:] for line in script.splitlines())
        with tempfile.TemporaryDirectory() as tmp:
            folder = Path(tmp)
            def git(*args):
                return subprocess.check_output(["git", *args], cwd=folder, text=True).strip()
            git("init", "-q")
            git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                "commit", "--allow-empty", "-qm", "fixture")
            sha = git("rev-parse", "HEAD")
            import os, sys
            for expected, event, success in ((sha, sha, True), ("", sha, False),
                                             ("0"*40, sha, False), (sha, "1"*40, False)):
                env = dict(os.environ, EXPECTED_SHA=expected, GITHUB_SHA=event)
                result = subprocess.run([sys.executable, "-c", script], cwd=folder,
                    env=env, text=True, capture_output=True)
                self.assertEqual(result.returncode == 0, success, result.stderr)


if __name__ == "__main__":
    unittest.main()
