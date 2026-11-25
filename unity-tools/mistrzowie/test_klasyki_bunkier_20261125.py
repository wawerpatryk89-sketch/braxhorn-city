"""The owner's Polish classics and their bunker under the quad track's hill (owner 24.11.2026 22:21-23:13).

    python3 -B -m pytest -p no:cacheprovider unity-tools/mistrzowie/test_klasyki_bunkier_20261125.py
"""
import json
import re

import numpy as np
import sys
import unittest
from pathlib import Path

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
KL = ROOT / "unity-tools/mistrzowie/klasyki"
PACK = ROOT / "unity-city/Prywatne/braxhorn-sklep-klasyki-01"
sys.path.insert(0, str(KL))


def bez_komentarzy(src):
    return "\n".join(l.split("//")[0] for l in src.splitlines())


class Narzedzie(unittest.TestCase):
    def setUp(self):
        import przygotuj_klasyki as PK
        import przepisy
        self.PK, self.P = PK, przepisy

    def test_kazdy_pojazd_ma_przepis_i_wymiary(self):
        # 16 of the first three zips, 11 cars of the second set (02:15), 5 of the third (02:46: Warszawa, Nysa, Żuk, Osa,
        # WSK); the duplicate 125p left out
        self.assertEqual(len(self.PK.MODELE), 32)
        ids = [m["id"] for m in self.PK.MODELE.values()]
        self.assertEqual(len(ids), len(set(ids)))
        for name, m in self.PK.MODELE.items():
            self.assertIn(name, self.P.PRZEPISY, name)
            self.assertIn(m["typ"], ("auto", "ciezarowka", "motor"))
            self.assertIn(m["os"], ("x", "z"))
            # real lengths: a moped 1.7 m to a Star truck 6.5 m
            self.assertTrue(1.7 <= m["dl"] <= 6.6, name)
            if m["typ"] != "motor": self.assertIn(name, self.P.REFLEKTORY, name)   # its headlamps for the night
            if m["typ"] != "motor": self.assertIn(m["szyby"], ("malowane", "otwarte"), name)

    def test_lakiery_szyby_swiatla_znaczki(self):
        """owner 25.11 02:14-03:02: the paint renewed (a new coat keeping the light and shade, glossy, never black metal),
        the windows straight with their rubber, every old car its black plate, the lamps lit at night, his 3D badges on"""
        import tekstura as T
        self.assertTrue(hasattr(T.Model, "lakiery")); self.assertTrue(hasattr(T.Model, "lampa")); self.assertTrue(hasattr(T.Model, "reflektor"))
        src = (KL / "przepisy.py").read_text(); pk = (KL / "przygotuj_klasyki.py").read_text()
        self.assertIn("def _bez_metalu", src); self.assertIn("przepisy._bez_metalu(", pk)
        self.assertIn("def mapa_swiatel", pk); self.assertIn("_swiatla.jpg", pk)
        # the old cars' plates black: today's white only on the food van of today
        self.assertEqual(re.findall(r'nowa=(True|"zabytkowa")', src), ["True"])
        for zn, Z in self.PK.ZNACZKI.items():
            self.assertTrue(.05 <= Z["szer"] <= .30 and Z["gleb"] <= .01, zn)
        for auto, lst in self.P.ZNACZKI_NA_AUTACH.items():
            self.assertIn(auto, self.PK.MODELE)
            for zn, widok, u, v in lst: self.assertIn(zn, self.PK.ZNACZKI); self.assertIn(widok, ("F", "T"))

    def test_nowe_kola(self):
        """owner 03:25: every car's wheels new - a rubber tyre and a proper rim, at the car's real tyre size"""
        import kola as KO
        for name, m in self.PK.MODELE.items():
            if m["typ"] == "motor": continue
            styl, cale, szer, kolor = self.PK.KOLA[name]
            self.assertIn(kolor, KO.PLATY); self.assertTrue(12 <= cale <= 20 and .12 <= szer <= .25, name)
            P, N, U, I = KO.kolo(m["R"], szer, cale, styl, kolor)
            self.assertLess(len(I), 6000, name)
            self.assertAlmostEqual(float(np.hypot(P[:, 1], P[:, 2]).max()), m["R"], delta=.003)   # stands on its radius
            self.assertLessEqual(float(P[:, 0].max() - P[:, 0].min()), szer * 1.02)
            self.assertTrue(((U >= 0) & (U <= 1)).all())

    def test_tablice_z_epoki(self):
        """1976-2000: private 3 letters + 4 digits, state 3 letters + 3 digits + a letter, the militia MO + 4 digits; the
        food van of today its current plate"""
        src = (KL / "przepisy.py").read_text()
        tabl = re.findall(r'(?:tablice\(\w+, |\.tablica\("[LPFT]", )"([A-Z0-9 ]+)"', src)
        self.assertGreaterEqual(len(tabl), 14)
        for t in tabl:
            self.assertRegex(t, r"^([A-Z]{3} \d{4}|[A-Z]{3} \d{3}[A-Z]|MO \d{4}|[A-Z]{3} \d[A-Z]\d{2})$", t)
        self.assertIn('"MO 4052"', src); self.assertIn('"MO 7846"', src); self.assertIn('"MO 1261"', src)

    def test_napisy_montserrat_bez_logo(self):
        import tekstura as T
        self.assertTrue(all(f.exists() for f in T.FONT_FILES))
        self.assertIn("Montserrat-SemiBold", T.FONT_FILES[0].name)
        # the ogonek of Ą stays inside the letters' picture (it was cut at 1.0 em)
        _, _, h_a = T.tekst_rgba("GORACE", .1, (255, 255, 255)); _, _, h_o = T.tekst_rgba("GORĄCE", .1, (255, 255, 255))
        self.assertGreater(h_o, h_a + .1 * .12)
        # brand names only as plain words: no logo pictures are laid, only text and plates
        src = (KL / "przepisy.py").read_text()
        self.assertNotRegex(src, r"\.png|\.jpg|logo\(")

    def test_tablice_osobne_chrom_i_szyby(self):
        """owner 25.11 08:17 ("ty się lepiej przyjrzyj jak to wygląda"): a plate its own flat part screwed on (a plate
        painted on a curved boot bent with it), every classic's plates in one atlas; a white coat never on the chrome and
        the lamps (the Syrena's bumpers, grille, rings); a two-tone body's line smoothed; a wing kept out of the back
        window's view; a raked coarse glass seen whole; a back window given dark glass"""
        import tekstura as T
        pk = (KL / "przygotuj_klasyki.py").read_text(); src = (KL / "przepisy.py").read_text(); tx = (KL / "tekstura.py").read_text()
        self.assertIn("def osadz_tablice", pk); self.assertIn("def atlas_tablic", pk); self.assertIn('"tablice": [{"tekst"', pk)
        self.assertIn("self.tablice3d.append", tx)
        self.assertLessEqual(20 * self.PK.TAB_H, 2048); self.assertLessEqual(self.PK.TAB_KOL * self.PK.TAB_W, 2048)   # 120 slots in 2048 px
        self.assertIn("def _zostaw", src); self.assertIn("def _obszary", src); self.assertIn("obszary=True", src)
        self.assertTrue(any(k.startswith("chrom") for _, _, k in self.P.SYRENA_ZOSTAW))
        self.assertTrue(any(k.startswith("lampa") for _, _, k in self.P.SYRENA_ZOSTAW))
        self.assertEqual(sum(1 for w, _, _ in self.P.SYRENA_ZOSTAW if w == "P"), sum(1 for w, _, _ in self.P.SYRENA_ZOSTAW if w == "L"))
        self.assertIn("do=None, gesto=False", tx)
        t125 = [o for o in self.P.FIAT125P_TUNING_OKNA if o[0] == "T"][0]; self.assertIn("do", t125[3])
        self.assertIn('"szyby_ciemne"', pk); self.assertIn('op.pop("ciemna", False)', pk)
        # the militia's white stripes laid straight; Tripo's crumpled sheet in a back window all dark glass; the inside
        # behind an opening restored only where the opening is (never the roof, never the far side's skin)
        self.assertEqual(len(re.findall(r"\n    _pas\(mod, ", src)), 3)
        self.assertIn('op.get("cale", False)', pk); self.assertIn("gl &= ~wierzch", pk); self.assertIn("przeciw = ", pk)
        self.assertIn("resztki starych opon", pk)

    def test_szyby_i_kola(self):
        src = (KL / "przygotuj_klasyki.py").read_text()
        self.assertIn("def szyby_malowane", src); self.assertIn("def szyba_otwarta", src)
        self.assertIn("convexHull", src)                                     # a window's outline straight, not ragged
        self.assertIn("1.01, 1.04", src)                                     # the mudguard over the hub stays on the body
        self.assertIn('"kolo_%s_%s"', src)
        # the 126p's windows (owner 25.11 00:55 "Szyby w maluchu?"): measured outlines, the glass clear as it was, the
        # rubber round it; the atlas' gutters filled (no seam of the old paint)
        P = (KL / "przepisy.py").read_text()
        self.assertIn("okna=MALUCH_OKNA, przyciemniane=False", P)
        self.assertEqual(sorted({o[0] for o in self.P.MALUCH_OKNA}), ["F", "L", "P", "T"])
        self.assertIn("def zaokraglij", src); self.assertIn("def dopelnij_szwy", src)
        self.assertIn("USZCZELKA", src)
        # parts without pictures inside or linked (Unity reads no texture per part)
        z = src[src.index("def zapisz_glb"):src.index("# ------", src.index("def zapisz_glb"))]
        self.assertNotIn('"images"', z); self.assertNotIn("baseColorTexture", z)


class Paczka(unittest.TestCase):
    def test_paczka_zaszyfrowana(self):
        m = json.loads((PACK / "manifest.json").read_text())
        self.assertEqual(m["target"], "pojazdy_klasyki_01")
        pliki = sorted(p.name for p in PACK.iterdir())
        self.assertTrue(all(n.startswith("part-") and n.endswith(".cms") or n in ("manifest.json", "recipient.crt", "recipient.pem.enc") for n in pliki), pliki)
        for part in m["parts"]: self.assertLessEqual(part["bytes"], 90 * 1024 * 1024)
        lib = [f for f in m["files"] if f.startswith("pojazdy_klasyki_01/")]
        self.assertIn("pojazdy_klasyki_01/katalog.json", lib)
        self.assertEqual(len([f for f in m["files"] if f.startswith("oryginal/")]), 32 + 4)
        for zn in ("fso", "trabant_s", "polski_fiat", "fso_polonez"):
            self.assertIn("pojazdy_klasyki_01/znaczek_%s.glb" % zn, lib)
        for t in ("tablice_kolor.jpg", "tablice_mr__lin.jpg", "tablice_normal__lin.jpg"):     # the plates' one atlas
            self.assertIn("pojazdy_klasyki_01/_tekstury/" + t, lib)
        for model in ("trabant", "warszawa_m20", "maluch_tuning_czerwony"):
            self.assertIn("pojazdy_klasyki_01/_tekstury/klasyk_%s_swiatla.jpg" % model, lib)
        for model in ("maluch", "syrena", "fiat125p", "polonez", "star_zielony", "junak"):
            self.assertIn("pojazdy_klasyki_01/klasyk_%s_korpus.glb" % model, lib)
            self.assertIn("pojazdy_klasyki_01/_tekstury/klasyk_%s_kolor.jpg" % model, lib)
        # nothing of the owner's beside the pack in the clear
        self.assertFalse(list((ROOT / "unity-city/Assets/Source/Prywatne").glob("**/klasyk_*")) if (ROOT / "unity-city/Assets/Source/Prywatne").exists() else [])


class Unity(unittest.TestCase):
    def setUp(self):
        self.kl = (ED / "BraxChampionsKlasyki.cs").read_text()
        self.bu = (ED / "BraxChampionsBunkier.cs").read_text()
        self.tor = (ED / "BraxChampionsTorKladow.cs").read_text()
        self.las = (ED / "BraxChampionsLasKladow.cs").read_text()

    def test_klasyki_jezdza(self):
        k = bez_komentarzy(self.kl)
        self.assertIn('Paczka = "pojazdy_klasyki_01"', k)
        self.assertIn("AddComponent<BraxChampionsCar>()", k)
        self.assertIn("car.rollingWheels = toczace.ToArray(); car.steeringWheels = skretne.ToArray();", k)
        self.assertIn('_Cull", 0f', k)                                       # Tripo's thin shells: both sides
        self.assertIn("SzybaCiemna", k); self.assertIn("SzybaJasna", k)
        # the lamps at night (the light map as the emission, black by day) and the owner's badges on the body
        self.assertIn('mat.SetTexture("_EmissionMap", swiatla)', k); self.assertIn("AddComponent<BraxChampionsSwiatlaKlasykow>()", k)
        self.assertIn("Znaczki(nadwozie, m);", k)
        self.assertIn('c.material == "kola"', k); self.assertIn("public Mapy kola;", k)
        # the plates their own flat parts (one atlas, one material), a back window's new dark pane
        self.assertIn("Tablice(nadwozie, m);", k); self.assertIn("public TabliceMapy tablice;", k); self.assertIn("public Tablica[] tablice;", k)
        self.assertIn("m.szyby.ciemne", k); self.assertIn("siatki[nazwa] = mesh", k)
        sw = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsSwiatlaKlasykow.cs").read_text()
        self.assertIn("public static bool Noc", sw); self.assertIn("MaterialPropertyBlock", sw)
        self.assertTrue((ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsSwiatlaKlasykow.cs.meta").exists())
        for f in ("BraxChampionsKlasyki.cs.meta", "BraxChampionsBunkier.cs.meta"): self.assertTrue((ED / f).exists())

    def test_bunkier_w_torze(self):
        t = bez_komentarzy(self.tor)
        self.assertIn("BraxChampionsBunkier.Zbuduj(g);", t)
        self.assertLess(t.index("BraxChampionsBunkier.Zbuduj(g);"), t.index('W.Zapisz("mistrzowie_tor_kladow")'))
        self.assertIn("BraxChampionsBunkier.Grunt(x, z, h)", t)
        self.assertIn("BraxChampionsBunkier.Dziura(", t)
        self.assertIn("BraxChampionsBunkier.Strefa(x, z)", t)
        self.assertIn("BraxChampionsBunkier.Wolne(x, z)", bez_komentarzy(self.las))

    def test_bunkier_wymiary_i_bez_migotania(self):
        b = bez_komentarzy(self.bu)
        self.assertIn("Tunel = 6f, TunelH = 4.5f", b)
        self.assertRegex(b, r"HallX0 = -445f, HallX1 = -393f, HallZ0 = 797f, HallZ1 = 827f, HallH = 6f")
        # the cut's floor ends at the headwall's face (the tunnel's floor from there): never two floors on one plane
        self.assertIn("q >= sF - .4f", b)
        self.assertIn("tunelX = p[i1].x;", b); self.assertIn("px = tunelX", b); self.assertIn("xk = HallX0 - sc", b)
        # the painted lines stand 2 cm proud without a bottom face
        for m in re.finditer(r"W\.OBox\(linie,[^;]*;", b): self.assertTrue(m.group(0).rstrip(";").endswith("false, true)"), m.group(0))
        # the caps 10 cm under the ground where the terrain's cells are kept over them
        self.assertIn("Podloga + .3f) - .1f", b)
        # the hall's roof under the ground: the floor lowered if it would not be
        self.assertIn("nad - (HallH + Strop + 1.5f)", b)

    def test_wszystkie_klasyki_w_bunkrze(self):
        import przygotuj_klasyki as PK
        rzedy = re.findall(r'"([a-z0-9_]+)"', self.bu[self.bu.index("Poludnie ="):self.bu.index("public static readonly List<Vector3> Miejsca")])
        self.assertEqual(sorted(rzedy), sorted(PK.MODELE))

    def test_droga_nie_przecina_toru(self):
        """the forest road's knots keep 10 m from the woods' trail (its corners) and inside the west fence (x -504)"""
        rogi = [tuple(map(float, r)) for r in re.findall(r"new Vector4\((-?[\d.]+)f, (-?[\d.]+)f", self.tor)]
        wezly = [tuple(map(float, w)) for w in re.findall(r"new Vector2\((-?[\d.]+)f, (-?[\d.]+)f\)", self.bu[self.bu.index("Wezly ="):self.bu.index("};", self.bu.index("Wezly ="))])]
        self.assertGreaterEqual(len(wezly), 6)
        for (x, z) in wezly:
            self.assertGreater(x, -504 + 2.5 + 2)
            for (rx, rz) in rogi:
                self.assertGreater(((x - rx) ** 2 + (z - rz) ** 2) ** .5, 10.0, (x, z, rx, rz))


if __name__ == "__main__":
    unittest.main()
