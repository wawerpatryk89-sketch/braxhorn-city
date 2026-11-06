#!/usr/bin/env python3
"""Targeted code/data regression audit for estate (NO Unity editor, NO APK).

A PASS proves source contracts only. Physics, animation skinning and rendering on
the owner's phone must still be observed in an actual Unity device build.
"""
from pathlib import Path
import hashlib
import json
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
RUNTIME = ROOT / "unity-city/Assets/Claude/Runtime"
IMPORT = ROOT / "unity-tools/claude"


def src(folder, name):
    return (folder / name).read_text(encoding="utf-8")


class EstateCriticalContracts(unittest.TestCase):
    def test_22_originals_are_encrypted_in_current_git_tree(self):
        # The public raw owner ZIP must not be required after the one-time
        # safe CI transfer. This does not erase earlier public commit history.
        secret = ROOT / "unity-city/Prywatne"
        self.assertFalse((secret / "Patryk_22_oryginalne_ruchy.zip").exists())
        encrypted = secret / "import-patryk-animations-20261106"
        catalog = json.loads((IMPORT / "patryk-owner-22-manifest.json").read_text())
        manifest = json.loads((encrypted / "manifest.json").read_text())
        self.assertEqual(len(catalog["clips"]), 22)
        self.assertEqual(manifest["zip_sha256"], catalog["animation_archive"]["sha256"])
        self.assertEqual(manifest["zip_bytes"], 6172641)
        self.assertEqual(manifest["archive_compression"], "lzma")
        self.assertEqual(manifest["files"], {e["file"]: e["sha256"] for e in catalog["clips"]})
        self.assertEqual(len(manifest["chunks"]), 6)
        for entry in manifest["chunks"]:
            b = (encrypted / entry["file"]).read_bytes()
            self.assertEqual(len(b), entry["bytes"])
            self.assertEqual(hashlib.sha256(b).hexdigest(), entry["sha256"])
        self.assertTrue((encrypted / "import-private.pem.enc").exists())
        self.assertTrue((encrypted / "package-key.rsa").exists())

    def test_no_silent_extra16_dependency(self):
        importer = src(IMPORT, "prywatne.py")
        own = src(IMPORT, "import_patryk_owner_22.py")
        self.assertIn("install_patryk_owner_22(CEL / \"ruchy\")", importer)
        self.assertNotIn("install_extra16(CEL / \"ruchy\")", importer)
        self.assertIn("decrypt_transfer(transfer)", own)
        self.assertIn("verified_files(raw)", own)
        self.assertIn("if len(actual) != 22", own)

    def test_patryk_original_model_and_only_owner_motions(self):
        p = src(EDITOR, "BraxClaudeOsiedlePatryk.cs")
        m = src(EDITOR, "BraxClaudePatrykHumanoid.cs")
        self.assertIn("fb8e2630bc7f835a743d917c35c378c7d074f0fde993a0bbf864b31ab67e1d97", p)
        self.assertIn("PatrykPrzygotujWlasneRuchy()", p)
        self.assertIn("KlipyPatryka.Length + additionalReady != 22", p)
        self.assertNotIn("PatrykSpacerAwaryjny(", p + m)
        self.assertNotIn("PatrykHumanoid(hero", p)
        self.assertIn("Quaternion.Inverse(sourceRest[name])", m)
        self.assertIn("PatrykCarIdleZWlasnegoWejscia", p)

    def test_pink_cars_missing_wheels_and_skin_import(self):
        cars = src(EDITOR, "BraxClaudeOsiedleAutaModel.cs")
        qa = src(EDITOR, "BraxClaudeOsiedleAktualizacja.cs")
        self.assertIn('A.ileKol != 4', cars)
        self.assertIn('SiatkaKola(A, tris', cars)
        self.assertIn('MaterialAutaAndroid(materials[i])', cars)
        self.assertIn('sk.BakeMesh(baked)', cars)
        self.assertIn('UnityEngine.Object.DestroyImmediate(sk);', cars)
        self.assertIn("m.shader.isSupported", qa)
        self.assertIn("różowy materiał", qa)
        self.assertIn("samochodyGracza.Length != 1", qa)

    def test_no_roof_clipping_from_missing_samples(self):
        c = src(EDITOR, "BraxClaudeKabiny.cs")
        people = src(EDITOR, "BraxClaudeOsiedleLudzieMiejsca.cs")
        passengers = src(EDITOR, "BraxClaudePodrozni.cs")
        self.assertIn("if (klatki == 0) w.zapas = float.NegativeInfinity;", c)
        self.assertIn("osoba.klipy.Length == 0", c)
        self.assertIn("return miesci;", c)
        self.assertIn("if (!DopasujKabine(m, f))", people)
        self.assertIn("if (!DopasujKabine(m, seat))", passengers)

    def test_owner_exits_car_with_owner_sit_to_stand(self):
        motion = src(RUNTIME, "BraxAutoGracza.cs")
        importer = src(EDITOR, "BraxClaudeOsiedlePatryk.cs")
        self.assertIn('PK("SitToStand", false, true, "patryk_owner/Sit To Stand.fbx")', importer)
        self.assertIn('p.clips.GetClip("SitToStand")', motion)
        self.assertIn('p.clips.CrossFade("SitToStand", .15f)', motion)
        self.assertIn('Mathf.Clamp(wstawanie.length', motion)
        self.assertIn('Odlacz(point)', motion)

    def test_horn_is_not_looped_forever(self):
        s = src(RUNTIME, "BraxRuchAutSym.cs")
        self.assertIn("klaksonUzytyPrzyBlokadzie", s)
        self.assertIn("czas + 30f", s)
        self.assertIn("a.tGracz > 3f", s)

    def test_scene_build_has_active_routing_and_reports_errors(self):
        osiedle = src(EDITOR, "BraxClaudeOsiedle.cs")
        cars = src(EDITOR, "BraxClaudeOsiedleAuta.cs")
        qa = src(EDITOR, "BraxClaudeOsiedleAktualizacja.cs")
        build = (ROOT / "unity-city/Assets/Editor/BraxCityBuild.cs").read_text()
        self.assertIn('Guard("RUCH AKTYWNY', osiedle)
        self.assertIn("RuchNaOsiedlu", osiedle)
        self.assertIn("AutaWRuchu(szablony)", cars)
        self.assertIn("AutoPatryka(szablony)", cars)
        self.assertIn("BŁĄD w kroku auta", cars)
        self.assertIn("SprawdzAktualizacje(scene)", build)
        self.assertIn("city-review/aktualizacja-osiedla.txt", qa)

    def test_no_quality_tests_stall_mobile_build(self):
        tests = src(EDITOR, "BraxClaudeOsiedleKontrolaZlecenia.cs")
        self.assertIn("BraxBuildQuality.Advisory", tests)
        self.assertIn("BraxBuildQuality.Warn", tests)
        self.assertIn("WymaganeOsiedleException", tests)
        self.assertIn("m_EditorVersion: 6000.0.85f2",
                      (ROOT / "unity-city/ProjectSettings/ProjectVersion.txt").read_text())


if __name__ == "__main__":
    unittest.main()
