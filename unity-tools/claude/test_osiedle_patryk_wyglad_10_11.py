"""OSIEDLE 10.11 — Patryk jak dostarczony: ruchy właściciela prosto na kości, bez plastiku, dłonie do ciała.

Właściciel 18:38 „Nie chcę widzieć Patryka powyginanego znowu”, 18:40 „ma wyglądać Patryk tak jak go dostarczyłem, żadnego
plastiku”, 19:19 „ja pod niego animacje zrobiłem”, 19:23 „Patryk to poza T”. Podgląd z APK dd490e9: Humanoid psuł lewą nogę
(stopa bokiem 89°, kolano −22°) i skręcał ramiona; mapa chropowatości Tripo 0,11–0,57 + metal z szumu = połysk plastiku.
Uruchom z unity-tools/claude:  python3 -m unittest test_osiedle_patryk_wyglad_10_11
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Claude"


def czytaj(p):
    return pathlib.Path(p).read_text(encoding="utf-8")


def metoda(src, sygn):
    i = src.index(sygn); j = src.index("{", i); d = 0
    for k in range(j, len(src)):
        if src[k] == "{": d += 1
        elif src[k] == "}":
            d -= 1
            if d == 0: return src[i:k + 1]
    raise AssertionError(sygn)


H = czytaj(ED / "BraxClaudePatrykHumanoid.cs")
P = czytaj(ED / "BraxClaudeOsiedlePatryk.cs")
T = czytaj(ED / "BraxMixamoT.cs")


class Humanoid(unittest.TestCase):
    def test_sample_na_modelu_gracza(self):
        m = metoda(H, "static AnimationClip PatrykHumanoid(")
        self.assertIn("var animator = hero.AddComponent<Animator>()", m)
        self.assertLess(m.index("graph.Evaluate(0)"), m.index("var q = bones[b].localRotation"))
        self.assertIn("positions[i] = hips.localPosition", m)
        self.assertNotIn("SetHumanPose", m)

    def test_biodra_i_kierunek_ruchu(self):
        self.assertIn("desired.lockRootHeightY = desired.keepOriginalPositionY = true", T)
        self.assertIn("animator.applyRootMotion = false", H)

    def test_kosci_dloni_wymagane_na_modelu(self):
        for bone in ("LeftIndexProximal", "RightIndexProximal", "LeftLittleProximal", "RightLittleProximal"):
            self.assertIn("HumanBodyBones." + bone, T)
        self.assertIn("if (!animator.GetBoneTransform(bone))", T)


class Wyglad(unittest.TestCase):
    def test_bez_plastiku(self):
        m = metoda(P, "static Material PatrykMaterial(")
        self.assertIn('mat.SetFloat("_RoughnessFloor", .78f)', m)
        self.assertIn('mat.SetFloat("_MetallicScale", 0f)', m)
        self.assertIn('mat.SetTexture("_MainTex", kolor)', m)      # tekstury właściciela bez zmian

    def test_spokojna_poza_dlonie_do_ciala(self):
        self.assertIn('PK("Idle", true, true,', P)
        self.assertNotIn("PatrykRece(", H + P)

    def test_stopa_mierzona_na_nodze_podporowej(self):
        k = metoda(H, "static void PatrykNogiKlatka(")
        self.assertIn("bool podpora = bend < 35f && Vector3.Angle(shin, Vector3.down) < 45f;", k)
        self.assertIn("if (toe && podpora)", k)


if __name__ == "__main__":
    unittest.main()
