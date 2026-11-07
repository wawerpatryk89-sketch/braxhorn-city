"""Verify read-only R1 texture-source audit and byte-accurate size receipt."""
import ast
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
AUDIT = ROOT / "unity-tools/claude/r1_texture_source_audit.py"
WORKFLOW = ROOT / ".github/workflows/claude-region-build.yml"
REPORT = ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeSizeReport.cs"

class R1TextureSourceAudit(unittest.TestCase):
    def test_script_is_readonly_and_compiles_without_third_party_packages(self):
        source = AUDIT.read_text(encoding="utf8")
        ast.parse(source, str(AUDIT))
        self.assertIn("Image.open(path)", source)
        self.assertIn("max(w, h) < MIN_2K", source)
        self.assertIn("Nothing resized", source)
        self.assertNotIn("Image.Resampling", source)
        self.assertNotIn(".resize(", source)
        self.assertNotIn(".thumbnail(", source)

    def test_manual_build_only_audit_receipts(self):
        workflow = WORKFLOW.read_text(encoding="utf8")
        report = REPORT.read_text(encoding="utf8")
        self.assertIn("r1_texture_source_audit.py", workflow)
        self.assertIn("city-review/r1-source-textures.json", workflow)
        self.assertIn("city-review/miasto-r1-rozmiary.json", workflow)
        self.assertIn('Path.GetFullPath(Path.Combine(Application.dataPath, "../../city-review"))', report)
        self.assertIn("File.Exists(outDir)", report)
        self.assertIn("new FileInfo(outDir).Length", report)
        self.assertIn("miasto-r1-rozmiary.json", report)

if __name__ == "__main__":
    unittest.main()
