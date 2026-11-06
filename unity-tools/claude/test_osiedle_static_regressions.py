#!/usr/bin/env python3
"""Fast source-level regressions for the Unity 6 estate; not a device test.

Never claim a passed source test proves rendered appearance, navigation or APK.
"""
from pathlib import Path
import json
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Claude"
PRYWATNE = ROOT / "unity-city/Prywatne"


def source(name):
    return (EDITOR / name).read_text(encoding="utf-8")


class EstateSourceRegressions(unittest.TestCase):
    def test_owner_animation_transfer_private_only(self):
        manifest = json.loads((ROOT / "unity-tools/claude/patryk-owner-22-manifest.json").read_text())
        transfer = PRYWATNE / "import-patryk-animations-20261106"
        self.assertTrue((transfer / "manifest.json").is_file(), "Encrypted owner FBX not committed")
        self.assertFalse((PRYWATNE / "Patryk_22_oryginalne_ruchy.zip").exists(),
                         "Plain owner FBX must not be left in the current public branch")
        self.assertEqual(manifest["animation_archive"]["file_count"], 22)
        script = (ROOT / "unity-tools/claude/prywatne.py").read_text()
        self.assertIn('install_patryk_owner_22(CEL / "ruchy")', script)
        self.assertNotIn('install_extra16(CEL / "ruchy")', script)
        self.assertIn('digest != clip["sha256"]', script)

    def test_skinned_car_geometry_not_destroyed_without_baking(self):
        car = source("BraxClaudeOsiedleAutaModel.cs")
        self.assertIn("sk.BakeMesh(baked)", car)
        self.assertIn("AddComponent<MeshFilter>()", car)
        self.assertIn("AddComponent<MeshRenderer>()", car)
        self.assertIn("if (A.ileKol != 4", car)
        self.assertIn("mesh.vertexCount < 12", car)
        self.assertIn("MaterialAutaAndroid", car)

    def test_cars_use_only_persistent_compatible_materials(self):
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        car = source("BraxClaudeOsiedleAutaModel.cs")
        self.assertIn("!m.shader.isSupported", scene)
        self.assertIn('shader.IndexOf("Universal Render Pipeline"', scene)
        self.assertIn('shader.IndexOf("Shader Graphs/"', scene)
        self.assertIn("BraxCityMaterials.Imported", car)

    def test_audit_does_not_count_arena_group_as_missing_wheels(self):
        scene = source("BraxClaudeOsiedleAktualizacja.cs")
        self.assertIn('t.name.StartsWith("Auto Patryka · ", StringComparison.Ordinal)', scene)
        wheel_audit = scene.split("var wszystkieAuta =", 1)[1].split("foreach (var auto in wszystkieAuta)", 1)[0]
        self.assertNotIn('t.name.StartsWith("Auto Patryka", StringComparison.Ordinal)', wheel_audit)
        self.assertIn('auto.Find("koło " + id + "/obrót")', scene)
        self.assertIn("patryki.Length != 1", scene)

    def test_npc_cabin_requires_real_head_and_anim_frames(self):
        cabins = source("BraxClaudeKabiny.cs")
        people = source("BraxClaudeOsiedleLudzieMiejsca.cs")
        passengers = source("BraxClaudePodrozni.cs")
        self.assertIn("if (klatki == 0) w.zapas = float.NegativeInfinity", cabins)
        self.assertIn("skora.Count == 0", cabins)
        self.assertIn("osoba.klipy.Length == 0", cabins)
        self.assertIn("if (!DopasujKabine(m, f))", people)
        self.assertIn("if (!DopasujKabine(m, seat))", passengers)

    def test_player_car_has_owner_clips_and_clearance_checks(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        private_car = source("BraxClaudeAutoPatryka.cs")
        self.assertIn('PK("EnteringCar"', player)
        self.assertIn('PK("Walk"', player)
        self.assertIn('PK("Run"', player)
        self.assertIn('if (wybor.t == null)', private_car)
        self.assertIn('car.drzwi', private_car)
        self.assertIn("ZapasPozy(skora", private_car)
        self.assertIn("brak wolnego stanowiska pod areną", private_car)

    def test_estate_scene_calls_all_fundamental_procedures(self):
        project = (ROOT / "unity-city/Assets/Editor/BraxCityBuild.cs").read_text()
        estate = source("BraxClaudeOsiedle.cs")
        for function in ("PatrykBohater", "AutoGraczaKontrola", "SalonPrzejscieKontrola",
                         "SportGryKontrola", "SprawdzAktualizacje"):
            self.assertIn(function + "(", project)
        for function in ("RuchNaOsiedlu", "LudzieNaOsiedlu", "DrzwiBezSciany", "SprawdzZlecenieOsiedla"):
            self.assertIn(function, estate)
        self.assertIn("BraxBuildQuality.Warn", source("BraxClaudeOsiedleKontrolaZlecenia.cs"))

    def test_no_unmeasured_patryk_headroom_or_invalid_roof(self):
        car = source("BraxClaudeAutoPatryka.cs")
        roof = source("BraxClaudeKabiny.cs")
        self.assertIn("skora.Count == 0", car)
        self.assertIn("skora.Sum(s => s.v.Length) < 8", car)
        self.assertIn("float.IsNaN(dach)", roof)
        self.assertIn("wl.zapas = float.NegativeInfinity", roof)

    def test_patryk_face_4k_import_not_silently_degraded(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        self.assertIn("if (!texture)", player)
        self.assertIn("texture.width != limit || texture.height != limit", player)
        self.assertIn("throw new InvalidOperationException(\"Patryk: nieprawidłowa tekstura", player)

    def test_no_fake_static_player_substitution(self):
        player = source("BraxClaudeOsiedlePatryk.cs")
        motion = source("BraxClaudePatrykHumanoid.cs")
        self.assertNotIn("PatrykSpacerAwaryjny(", player + motion)
        self.assertNotIn("PatrykPozaStala(", player + motion)
        self.assertIn("PatrykWlasnyFBX(hero, kosci", player)
        self.assertIn("Quaternion.Inverse(sourceRest[name])", motion)


if __name__ == "__main__":
    unittest.main()
