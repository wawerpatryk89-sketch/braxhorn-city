import hashlib
import importlib.util
import pathlib
import shutil
import tempfile
import unittest
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("kopia_bazy", ROOT / "unity-tools/updates/kopia_bazy.py")
kb = importlib.util.module_from_spec(spec); spec.loader.exec_module(kb)
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")


class KopiaBazy(unittest.TestCase):
    def _zip(self, tmp, wpisy):
        p = pathlib.Path(tmp) / "src.zip"
        with zipfile.ZipFile(p, "w") as z:
            for n, d in wpisy.items(): z.writestr(n, d)
        return p

    def test_rozpakowuje_po_zgodnej_sumie(self):
        with tempfile.TemporaryDirectory() as t:
            z = self._zip(t, {"Source/a.txt": "a", "Source/b/c.txt": "c"})
            stare = dict(kb.SHA); kb.SHA["curated-extended.zip"] = hashlib.sha256(z.read_bytes()).hexdigest()
            try:
                self.assertEqual(kb.main("curated-extended.zip", pathlib.Path(t) / "Assets", pobierz=lambda plik, tmp: shutil.copy(z, pathlib.Path(tmp) / plik)), 0)
            finally: kb.SHA.clear(); kb.SHA.update(stare)
            self.assertEqual((pathlib.Path(t) / "Assets/Source/b/c.txt").read_text(), "c")

    def test_zla_suma_zatrzymuje(self):
        with tempfile.TemporaryDirectory() as t:
            z = self._zip(t, {"Source/a.txt": "a"})
            with self.assertRaises(SystemExit):
                kb.main("curated-extended.zip", pathlib.Path(t) / "Assets", pobierz=lambda plik, tmp: shutil.copy(z, pathlib.Path(tmp) / plik))
            self.assertFalse((pathlib.Path(t) / "Assets/Source/a.txt").exists())

    def test_sciezka_poza_katalog_odrzucona(self):
        with tempfile.TemporaryDirectory() as t:
            z = self._zip(t, {"../poza.txt": "x"})
            with self.assertRaises(SystemExit): kb.rozpakuj(z, pathlib.Path(t) / "cel")

    def test_nieznany_plik(self):
        with self.assertRaises(SystemExit): kb.main("cos.zip", "x")

    def test_workflow_ma_zapas_po_wygaslym_artefakcie(self):
        # zadanie APK: oba kroki pobrania artefaktu z 29.10 nie przerywają buildu, zaraz po nich kopia z wydania
        i = WF.index("  android-apk:"); j = WF.index("\n  scene-only-unity6:", i); apk = WF[i:j]
        for ident, plik, cel in (("zrodla_fasad", "curated-extended.zip", "unity-city/Assets"), ("zrodla_workow", "kickboxing-review.zip", "reviewed-gym-source")):
            self.assertIn("id: " + ident + "\n      continue-on-error: true", apk)
            self.assertIn("if: steps." + ident + ".outcome != 'success'", apk)
            self.assertIn("run: python3 unity-tools/updates/kopia_bazy.py " + plik + " " + cel, apk)
        self.assertIn("contents: write", apk[:600])     # szkic wydania widać tylko z prawem zapisu


if __name__ == "__main__":
    unittest.main()
