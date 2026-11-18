"""MIASTO R1 18.11 — szyldy z grafik właściciela (12:13: „Tych szyldów nie akceptuję… to ci wydrukuję”; 12:57 plansza A 17 + B 10;
13:02: „Te wystarczą… jak brakuje, to zrób porządek sam” — C z ikon grafik A). Paczka zaszyfrowana (repo publiczne), kaseton
świetlny bez par migotania, nad DRZWIAMI lokalu (14:21: „Nad oknami? Ani nad drzwiami?”) — nie na szybie, nie na gzymsie."""
import json
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
SZ = (REG / "BraxSzyldyWlasciciela.cs").read_text(encoding="utf-8")
PIERZ = (REG / "BraxUlicePierzeje.cs").read_text(encoding="utf-8")
WIEZE = (REG / "BraxRegionFawelaWiezeLokale.cs").read_text(encoding="utf-8")
PACK = ROOT / "unity-city/Prywatne/braxhorn-sklep-szyldy-r1-01"


def slownik(nazwa):
    a = SZ.index("static readonly Dictionary<string, string> " + nazwa); b = SZ.index("};", a)
    return dict(re.findall(r'\{ "([^"]+)", "([^"]+)" \}', SZ[a:b]))


class Paczka(unittest.TestCase):
    def test_encrypted_pack_with_all_signs(self):
        m = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "szyldy_r1_01")
        pliki = set(m["files"])
        for sek, nazwa in (("A", "UlicaGlowna"), ("B", "AlejaCentralna"), ("C", "Wysiegniki")):
            for slug in slownik(nazwa).values():
                self.assertIn("szyldy_r1_01/szyld_%s_%s.png" % (sek, slug), pliki)
        self.assertIn("szyldy_r1_01/katalog.json", pliki)
        self.assertIn("oryginal/plansza_18_11.jpg", pliki)
        self.assertEqual(sorted(q.name for q in PACK.iterdir()), ["manifest.json", "part-000.cms", "recipient.crt", "recipient.pem.enc"])
        dane = (PACK / "part-000.cms").read_bytes()
        self.assertNotIn(b"\x89PNG", dane); self.assertNotIn(b"JFIF", dane)
        self.assertIn('Katalog = "Assets/Source/Prywatne/Miasto/szyldy_r1_01/"', SZ)

    def test_every_shop_has_its_graphic(self):
        a = PIERZ.index("static readonly Dictionary<string, Lokal> Lokale"); b = PIERZ.index("};", a)
        lokale = re.findall(r'new Lokal\("([^"]+)"', PIERZ[a:b])
        wysiegniki = re.findall(r'new Lokal\("([^"]+)"[^\n]*wysiegnik = true', PIERZ[a:b])
        self.assertEqual(len(lokale), 17)
        self.assertEqual(sorted(slownik("UlicaGlowna")), sorted(lokale))
        self.assertEqual(sorted(slownik("Wysiegniki")), sorted(wysiegniki))
        aleja = re.findall(r'new LokalWiezy\("[^"]+", \d+, \d+, \d+, \d+, "([^"]+)"', WIEZE)
        wlasne = {"BRAXHORN BET", "WAWER NIERUCHOMOŚCI"}                      # osobne grafiki właściciela (bet_01, nieruchomości)
        self.assertEqual(sorted(slownik("AlejaCentralna")), sorted(set(aleja) - wlasne))


class Montaz(unittest.TestCase):
    def test_kaseton_no_flicker_pairs(self):
        self.assertIn("public const float Rama = .025f, Wystep = .022f, Dystans = .025f;", SZ)   # każda para równoległych ≥ 2 cm
        self.assertIn("s.uv[i0] = new Vector2(0f, 0f); s.uv[i0 + 1] = new Vector2(1f, 0f);", SZ)  # obraz nieodbity
        self.assertIn("var R = Vector3.Cross(n, Vector3.up).normalized;", SZ)                     # prawo patrzącego

    def test_street_sign_above_door_not_on_glass(self):
        # nadproże drzwi = nadproże witryn (3.05): nad drzwiami pełny pas 45 cm do spodu gzymsu 3.50 (dotąd drzwi do 3.20)
        self.assertIn("const float WitrynaGora = 3.05f, DrzwiGora = WitrynaGora;", PIERZ)
        self.assertIn("otwory.Add(new Otwor { r = Rect.MinMaxRect(f.drzwiS - 1.17f, Chodnik, f.drzwiS + 1.17f, DrzwiGora), gleb = .3f, drzwi = true });", PIERZ)
        self.assertIn("f.N, 2.34f, DrzwiGora - Chodnik, .3f, 0f, BraxDrzwiLokalu.ModelDla(lok.typ));", PIERZ)   # drzwi w tym otworze
        self.assertNotIn("3.2f), gleb = .3f, drzwi = true", PIERZ)
        self.assertIn("otwory.Add(new Otwor { r = Rect.MinMaxRect(s0, .62f, s1, WitrynaGora), gleb = GlebWitryny, witryna = true });", PIERZ)
        self.assertIn("const float g0 = WitrynaGora + .005f + BraxSzyldyWlasciciela.Rama, g1 = 3.50f - .005f - BraxSzyldyWlasciciela.Rama, odSciany = .09f;", PIERZ)
        self.assertIn("OBox(f, st.detal, 0f, f.W, 3.50f, 3.62f, 0f, .13f);", PIERZ)             # spód gzymsu nad parterem
        k = PIERZ[PIERZ.index("static bool KasetonWlasciciela"):PIERZ.index("static void Elewacja")]
        self.assertIn("float sc = Mathf.Clamp(f.drzwiS, lewo + (w + zapas) * .5f, prawo - (w + zapas) * .5f);", k)   # oś = oś drzwi
        self.assertNotIn("Witryny(f)", k)
        a = PIERZ.index("static void Szyldy(Fasada f)")
        self.assertLess(PIERZ.index("if (KasetonWlasciciela(f)) goto poSzyldzie;", a), PIERZ.index("string firma = lok.typ", a))
        self.assertIn("const float yNad = DrzwiGora,", PIERZ)                                    # szyld zapasowy też nad nadprożem

    def test_awning_over_door_too(self):
        # 14:33 (właściciel: „A ten taki daszek, to po co one są nad oknem zamiast nad drzwiami?”): markiza nad drzwiami, jedna linia
        m = PIERZ[PIERZ.index("if (lok.markiza)"):PIERZ.index("if (lok.wysiegnik)")]
        self.assertNotIn("if (i == drzwiBay) continue;", m)
        self.assertIn("if (i == drzwiBay) { s0 = mdA; s1 = mdB; }", m)
        self.assertIn("Mathf.Min(drzwiBay * f.bay + .2f, f.drzwiS - 1.37f)", m)            # wsporniki ± 1.27 m — na murze, nie w otworze
        self.assertIn("Mathf.Max((drzwiBay + 1) * f.bay - .2f, f.drzwiS + 1.37f)", m)
        self.assertIn("float yA = 3.04f, yB = 2.74f, dB = 1.05f, yC = 2.52f, pas = .26f;", m)   # dół falbany 2.52 m > Brax 2.33 m

    def test_door_leaves_fit_brax(self):
        drzwi = (REG / "BraxDrzwiLokalu.cs").read_text(encoding="utf-8")
        self.assertIn("float hS = Mathf.Min(2.85f, H - .2f);", drzwi)       # 2.90 m → skrzydła 2.70 m (Brax 2.33 m)
        self.assertIn("float hS = Mathf.Min(2.6f, H - .33f), hN = hS + .26f;", drzwi)   # przesuwne: 2.57 m

    def test_blade_sign_face(self):
        self.assertIn("BraxSzyldyWlasciciela.LicoWysiegnika(BraxSzyldyWlasciciela.PlikC(lok.szyld), fc, fn, 2f * bk)", PIERZ)

    def test_aleja_centralna_on_fascia(self):
        self.assertIn('BraxSzyldyWlasciciela.Kaseton(BraxSzyldyWlasciciela.PlikB(l.szyld), F.P(sB, yc, -.26f - .08f), F.N, .62f, .08f, sb - sa - .4f,', WIEZE)
        self.assertIn("sB = Mathf.Clamp(p.sc, sa + .2f + wB * .5f, sb - .2f - wB * .5f);", WIEZE)   # nad drzwiami lokalu
        self.assertIn("BraxUliceKit.S(listwa).Box(F.P(sa, yc - .36f, -.215f), F.P(sb, yc + .36f, -.26f));", WIEZE)   # listwa 0.72 m ≥ lico 0.62 + rama
        self.assertIn("if (!betLogo && !grafikaB && (firma == null", WIEZE)

    def test_preview_builds_all_signs(self):
        proj = (ROOT / "unity-tools/claude/podglad_aleja/podglad_aleja.csproj").read_text(encoding="utf-8")
        prog = (ROOT / "unity-tools/claude/podglad_aleja/Program.cs").read_text(encoding="utf-8")
        self.assertIn("BraxSzyldyWlasciciela.cs", proj)
        self.assertEqual(prog.count('new BraxCityArchitecture.Building("'), 17)
        self.assertIn('"C5_U12", "C5_U07", "C5_U04", "C5_U09", "C5_U02", "C5_U08", "C5_U00", "C5_U03"', prog)


if __name__ == "__main__":
    unittest.main()
