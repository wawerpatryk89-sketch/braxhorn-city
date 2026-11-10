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


B = czytaj(ED / "BraxClaudePatrykBezposrednio.cs")
H = czytaj(ED / "BraxClaudePatrykHumanoid.cs")
P = czytaj(ED / "BraxClaudeOsiedlePatryk.cs")


class Bezposrednio(unittest.TestCase):
    def test_poza_spoczynkowa_zrodla_przed_probkowaniem(self):
        m = metoda(B, "static AnimationClip PatrykBezposrednio(")
        self.assertLess(m.index("var S0 = src.ToDictionary"), m.index("clip.SampleAnimation(source, t);"))
        self.assertLess(m.index("var T0 = bones.ToDictionary"), m.index("clip.SampleAnimation(source, t);"))

    def test_biodra_skala_wysokosci_i_bez_dryfu(self):
        m = metoda(B, "static AnimationClip PatrykBezposrednio(")
        self.assertIn("float skala = hSrc > 1e-5f ? hTgt / hSrc : 1f;", m)
        self.assertIn("if (spec.wMiejscu) { d.x = 0f; d.z = 0f; }", m)

    def test_zly_szkielet_nie_daje_klipu(self):
        m = metoda(B, "static AnimationClip PatrykBezposrednio(")
        self.assertIn("if (dopasowane < 40) throw", m)                 # wtedy zapasowy ruch, nie T-poza
        self.assertIn("if (clip.humanMotion) throw", m)

    def test_bez_latania_skutkow_humanoid(self):
        for s in ("PatrykSymetriaNog", "PatrykKolanoDoPrzodu", "PatrykPoprawkiRuchu"):
            self.assertNotIn(s, B + H + P)

    def test_wybor_awatara_pomija_ruchy_wlasciciela(self):
        self.assertIn("if (PatrykRuchWlasciciela(zr))", metoda(H, "static Avatar PatrykWybierzAwatar("))


class Wyglad(unittest.TestCase):
    def test_bez_plastiku(self):
        m = metoda(P, "static Material PatrykMaterial(")
        self.assertIn('mat.SetFloat("_RoughnessFloor", .78f)', m)
        self.assertIn('mat.SetFloat("_MetallicScale", 0f)', m)
        self.assertIn('mat.SetTexture("_MainTex", kolor)', m)      # tekstury właściciela bez zmian

    def test_spokojna_poza_dlonie_do_ciala(self):
        self.assertIn("PatrykRece(rest.Select(s=>s.t).ToList(),forward);", metoda(H, "static AnimationClip PatrykPozaStala("))

    def test_stopa_mierzona_na_nodze_podporowej(self):
        k = metoda(H, "static void PatrykNogiKlatka(")
        self.assertIn("bool podpora = bend < 35f && Vector3.Angle(shin, Vector3.down) < 45f;", k)
        self.assertIn("if (toe && podpora)", k)


if __name__ == "__main__":
    unittest.main()
