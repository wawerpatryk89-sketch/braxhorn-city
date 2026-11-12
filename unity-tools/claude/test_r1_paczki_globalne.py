"""MIASTO R1 — paczki globalne towaru i sprzętu właściciela z Tripo (braxhorn-sklep-*), te same co w Mieście Mistrzów
(właściciel 11.11 22:44: „robimy to globalnie na całą grę do sklepów… żeby każdy z tego korzystał”; 21:20: „żeby nie robić
podwójnej roboty”). Repo publiczne: w gicie tylko paczki zaszyfrowane (CMS do certyfikatu CI), klucz tylko w sekretach."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PRYW = ROOT / "unity-city" / "Prywatne"
WF = (ROOT / ".github" / "workflows" / "claude-region-build.yml").read_text()


class PaczkiGlobalne(unittest.TestCase):
    def test_tools_copied(self):
        for f in ["unity-tools/sklep/install_sklep_modele.py", "unity-tools/sklep/split_sheet.py", "unity-tools/wideo/install_braxhorn_wideo.py"]:
            self.assertTrue((ROOT / f).is_file(), f)
        self.assertIn("BRAX_SKLEP_SKIPPED: no KLUCZ", (ROOT / "unity-tools/sklep/install_sklep_modele.py").read_text())

    def test_packs_encrypted_only(self):
        packs = sorted(PRYW.glob("braxhorn-sklep-*"))
        self.assertGreaterEqual(len(packs), 13)
        for p in packs:
            m = json.loads((p / "manifest.json").read_text())
            self.assertTrue(m["target"])
            for part in m["parts"]:
                self.assertTrue((p / part["name"]).is_file(), p.name + "/" + part["name"])
            self.assertTrue((p / "recipient.crt").is_file() and (p / "recipient.pem.enc").is_file())
            self.assertEqual([q.name for q in p.iterdir() if q.suffix in (".glb", ".jpg", ".png", ".tar", ".pem")], [], p.name)
        names = {p.name for p in packs}
        for need in ["braxhorn-sklep-walka-01", "braxhorn-sklep-sprzet-01", "braxhorn-sklep-silownia-01", "braxhorn-sklep-silownia-02"]:
            self.assertIn(need, names)

    def test_ci_step_with_secret_only_in_dispatch_workflow(self):
        self.assertEqual(WF.count("run: python3 unity-tools/sklep/install_sklep_modele.py"), WF.count("run: python3 unity-tools/claude/r1_reklamy_tv.py"))
        i = WF.index("run: python3 unity-tools/sklep/install_sklep_modele.py")
        self.assertIn("KLUCZ: ${{ secrets.BRAXHORN_KEYSTORE_PASS }}", WF[i - 300:i])
        head = WF[:WF.index("jobs:")]
        self.assertIn("workflow_dispatch", head)
        for auto in ["push:", "schedule:", "pull_request"]:
            self.assertNotIn(auto, head)


if __name__ == "__main__":
    unittest.main()
