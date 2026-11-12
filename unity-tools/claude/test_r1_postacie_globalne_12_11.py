"""MIASTO R1 (12.11.2026, właściciel 16:34–19:30): postacie globalne i walka w klatce areny na wyspie.

16:34 „zobacz tych dwóch, co tam walczą: zdeformowani, nie biją się … trzeba ich zrobić, żeby się napierdalali, a nie stoją
w miejscu”; 16:46 „Pitbull, Doberman — w obozie zdeformowani, do naprawy od zera”; 16:48 „są animacje T dla nich, wystarczy
tylko dodać, bez kombinowania”; 16:54 „Brax nie ruszaj”; 16:57 „postacie globalnie, te same pod całą grę”; 19:30 „Brax, Nyxa,
Krov, Vexa, Miś, Danny, Nina nie rób — mam ich w drugiej grze”.
"""
import importlib.util
import json
import pathlib
import re
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACK = ROOT / "unity-city/Prywatne/braxhorn-postacie-01"
WALKA = (ROOT / "unity-city/Assets/Claude/Runtime/BraxArenaWalka.cs").read_text(encoding="utf-8")
POSTACIE = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxPostacieGlobalne.cs").read_text(encoding="utf-8")
WYKON = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaWykonczenie.cs").read_text(encoding="utf-8")
PATRYK = (ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeOsiedlePatryk.cs").read_text(encoding="utf-8")
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
MANIFEST = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))


def klipy_edytora():
    blok = POSTACIE[POSTACIE.index("KlipyWalki ="):POSTACIE.index("KlipyWalkiWymagane")]
    return re.findall(r'\("(\w+)", (?:true|false)\)', blok)


def ataki():
    blok = WALKA[WALKA.index("static readonly Atak[] Ataki ="):WALKA.index("static readonly string[] Wymagane")]
    out = []
    for m in re.finditer(r'A\("(\w+)", ([\d.]+)f, ([\d.]+)f, (true|false), (.*?)\),\n', blok):
        ciosy = [(float(t), float(f)) for t, f in re.findall(r'[GK]\((\d*\.?\d+)f, (\d*\.?\d+)f', m.group(5))]
        out.append((m.group(1), float(m.group(2)), float(m.group(3)), m.group(4) == "true", ciosy))
    return out


class Paczka(unittest.TestCase):
    def test_zaszyfrowana_do_certyfikatu_ci(self):
        self.assertEqual(MANIFEST["target"], "postacie_01")
        self.assertIn("openssl cms -encrypt", MANIFEST["format"])
        for part in MANIFEST["parts"]:
            p = PACK / part["name"]
            self.assertTrue(p.exists(), part["name"])
            self.assertEqual(p.stat().st_size, part["bytes"])
            self.assertLessEqual(part["bytes"], 95 * 1024 * 1024)      # limit GitHuba 100 MB na plik
        self.assertTrue((PACK / "recipient.crt").exists())

    def test_nic_jawnego_w_repo(self):
        jawne = [p.name for p in PACK.iterdir() if p.suffix.lower() in (".fbx", ".png", ".jpg", ".glb", ".zip")]
        self.assertEqual(jawne, [])

    def test_zawodnicy_i_klipy_w_paczce(self):
        pliki = set(MANIFEST["files"])
        for pid in ("pitbul", "doberman"):
            for suf in (".fbx", "_BaseColor.jpg", "_Normal.png", "_ORM.png"):
                self.assertIn("postacie_01/%s/%s%s" % (pid, pid, suf), pliki)
        for k in klipy_edytora():
            self.assertIn("postacie_01/_ruchy_T/%s.fbx" % k, pliki)

    def test_braxa_i_postaci_z_drugiej_gry_nie_ma_w_paczce(self):
        for rel in MANIFEST["files"]:
            self.assertNotRegex(rel.lower(), r"brax|nyxa|krov|krow|vexa|mis/|danny|nina")


class Instalator(unittest.TestCase):
    def _modul(self):
        sys.path.insert(0, str(ROOT / "unity-tools/wideo"))
        spec = importlib.util.spec_from_file_location("install_postacie", ROOT / "unity-tools/postacie/install_postacie.py")
        m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m

    def test_bez_klucza_build_idzie_dalej(self):
        import os
        m = self._modul(); stary = os.environ.pop("KLUCZ", None)
        try:
            with tempfile.TemporaryDirectory() as t:
                self.assertEqual(m.install(packs=PACK.parent, out=pathlib.Path(t) / "out"), 0)
                self.assertFalse((pathlib.Path(t) / "out").exists())
        finally:
            if stary is not None: os.environ["KLUCZ"] = stary

    def test_rozklada_foldery_postaci_i_ruchow(self):
        import os
        m = self._modul()
        with tempfile.TemporaryDirectory() as t:
            t = pathlib.Path(t); packs = t / "packs"; p = packs / "braxhorn-postacie-99"; p.mkdir(parents=True)
            (p / "manifest.json").write_text(json.dumps({"target": "postacie_99"}))
            lib = t / "lib" / "postacie_99"; (lib / "ktos").mkdir(parents=True); (lib / "_ruchy_T").mkdir()
            (lib / "ktos" / "ktos.fbx").write_bytes(b"x"); (lib / "_ruchy_T" / "Garda.fbx").write_bytes(b"y"); (lib / "katalog.json").write_text("{}")
            stare = m.wideo.open_pack; m.wideo.open_pack = lambda pack, tmp: t / "lib"; os.environ["KLUCZ"] = "test"
            try: self.assertEqual(m.install(packs=packs, out=t / "out"), 0)
            finally: m.wideo.open_pack = stare; os.environ.pop("KLUCZ", None)
            self.assertTrue((t / "out" / "ktos" / "ktos.fbx").exists())
            self.assertTrue((t / "out" / "_ruchy_T" / "Garda.fbx").exists())

    def test_krok_w_kazdym_zadaniu_z_paczkami_sklepu(self):
        self.assertEqual(WF.count("run: python3 unity-tools/postacie/install_postacie.py"), WF.count("run: python3 unity-tools/sklep/install_sklep_modele.py"))
        self.assertGreaterEqual(WF.count("run: python3 unity-tools/postacie/install_postacie.py"), 3)


class KlipyWprostNaSzkielet(unittest.TestCase):
    def test_bez_mostu_humanoid_z_obozu(self):
        self.assertNotIn("BraxObozCharacters", POSTACIE)
        self.assertNotIn("HumanPoseHandler", POSTACIE)
        self.assertIn("PatrykPrzenies(hero, hips, kosci, spoczynek, minY, sciezka, PK(klucz, petla, true), out _, katalog)", POSTACIE)

    def test_zdeformowana_postac_nie_trafia_do_gry(self):
        self.assertIn("int bledy = PatrykTestKosci(hero, kosci, spoczynek, klipy, out string raportTestu);", POSTACIE)
        self.assertIn('if (bledy > 0) throw new Exception("test kości: "', POSTACIE)
        self.assertIn("if (hero) Object.DestroyImmediate(hero);", POSTACIE)

    def test_klipy_postaci_we_wlasnym_katalogu(self):
        self.assertIn("string katalogWyniku = null)", PATRYK)
        self.assertIn('string cel = (katalogWyniku ?? PatrykWynik) + "/" + k.nazwa + ".anim";', PATRYK)
        self.assertIn('PostacieWynik = "Assets/PlayableGenerated/Postacie"', POSTACIE)

    def test_wzrost_z_kosci(self):
        self.assertIn('Kosc("HeadTop_End")', POSTACIE)
        self.assertIn('"LeftToeBase", "RightToeBase"', POSTACIE)


class Walka(unittest.TestCase):
    def test_kazdy_atak_ma_klip_w_paczce(self):
        klipy = set(klipy_edytora()); a = ataki()
        self.assertGreaterEqual(len(a), 14)
        for nazwa, dl, tempo, kop, ciosy in a:
            self.assertIn(nazwa, klipy, nazwa)
            self.assertTrue(ciosy, nazwa)
            self.assertEqual([t for t, _ in ciosy], sorted(t for t, _ in ciosy), nazwa)
            for t, f in ciosy:
                self.assertTrue(0.05 < t < 0.95, (nazwa, t))
                self.assertTrue(0.2 < f < 0.85, (nazwa, f))             # zasięg końcówki: ułamek wzrostu
            self.assertTrue(1.0 <= tempo <= 1.5, nazwa)

    def test_wymagane_klipy_runtime_sa_w_edytorze(self):
        wym = re.findall(r'"(\w+)"', WALKA[WALKA.index("string[] Wymagane"):WALKA.index("// długości klipów pomocniczych")])
        for k in wym: self.assertIn(k, klipy_edytora())
        for k in re.findall(r'(?:Graj|Krok|Ma)\([^"\n]*"(\w+)"', WALKA):
            self.assertIn(k, klipy_edytora(), k)

    def test_dystans_z_zasiegu_ciosu(self):
        self.assertIn("float cel_d = (fmax + (at.kop ? MarginesKop : MarginesPiesc)) * wzrost, d = Dystans();", WALKA)
        self.assertIn("const float MarginesPiesc = .16f, MarginesKop = .06f;", WALKA)

    def test_nie_stoja_w_miejscu(self):
        for s in ("void Krazenie()", "void Krok(int i, string klip", "void Odrzut(int cel", "void Atakuj(int kto, bool kontra)", "z[i].p += z[i].ruch * dt;"):
            self.assertIn(s, WALKA)

    def test_para_zostaje_w_klatce(self):
        self.assertIn("float maks = Mathf.Max(.2f, promien - Dystans() * .5f - .35f * wzrost);", WALKA)
        self.assertIn('BraxClaudeOsiedle.WalkaNaMacie(root, apC + Vector3.up * (apF + .9f * K), 4.55f * K - 1.6f, K, "pitbul", "PITBULL", "doberman", "DOBERMAN")', WYKON)

    def test_blad_nie_psuje_buildu(self):
        self.assertIn("if (holder) Object.DestroyImmediate(holder);", POSTACIE)
        self.assertIn('catch (Exception e) { Ostrzez("BRAXHORN_ARENA_WALKA DO POPRAWKI (klatka bez zawodników): " + e); }', WYKON)

    def test_skrypt_ma_staly_guid(self):
        meta = (ROOT / "unity-city/Assets/Claude/Runtime/BraxArenaWalka.cs.meta").read_text()
        self.assertIn("guid: f4e80b446d05259a509c85cb9d0227e1", meta)


if __name__ == "__main__":
    unittest.main()
