"""OSIEDLE 10.11 — Patryk na ruchach właściciela, wsiadanie do auta, auta bez różu i z kołami.

Szybkie testy źródeł (zasada 7.11: bez Unity). Uruchom z unity-tools/claude:
    python3 -m unittest test_osiedle_patryk_auta_10_11
"""
import hashlib
import json
import os
import pathlib
import re
import tempfile
import unittest
from unittest import mock

TU = pathlib.Path(__file__).resolve().parent
ROOT = TU.parents[1]
ED = ROOT / "unity-city/Assets/Editor/Claude"
RT = ROOT / "unity-city/Assets/Claude/Runtime"


def czytaj(p):
    return pathlib.Path(p).read_text(encoding="utf-8")


def klipy_patryka():
    src = czytaj(ED / "BraxClaudeOsiedlePatryk.cs")
    blok = src[src.index("static readonly PatrykKlip[] KlipyPatryka"):]
    blok = blok[:blok.index("};")]
    stala = re.search(r'const string RuchyWlasciciela = "([^"]+)"', src).group(1)
    wynik = {}
    for m in re.finditer(r'PK\("(\w+)",\s*(true|false),\s*(true|false),(.*?)\),\s*$', blok, re.M):
        zrodla = []
        for z in m.group(4).split(","):
            z = z.strip()
            z = z.replace("RuchyWlasciciela + ", '"' + stala + '" + ').replace('" + "', "")
            zrodla.append(z.strip('"'))
        wynik[m.group(1)] = {"petla": m.group(2) == "true", "wMiejscu": m.group(3) == "true", "zrodla": zrodla}
    return wynik


class RuchyWlasciciela(unittest.TestCase):
    def test_chod_bieg_skok_z_paczki_wlasciciela_najpierw(self):
        k = klipy_patryka()
        katalog = json.loads(czytaj(TU / "patryk-owner-22-manifest.json"))
        pliki = {c["action"]: c["file"] for c in katalog["clips"]}
        for akcja in ("Walk", "Run", "Jump", "EnteringCar"):
            self.assertIn(akcja, k, akcja)
            self.assertEqual(k[akcja]["zrodla"][0], "patryk_owner/" + pliki[akcja], akcja)
            self.assertTrue(k[akcja]["wMiejscu"], akcja + ": bez dryfu bioder (ruch prowadzi fizyka/auto)")
        # stare ruchy zostają tylko jako zapas
        self.assertGreater(len(k["Walk"]["zrodla"]), 1)
        self.assertGreater(len(k["Run"]["zrodla"]), 1)
        self.assertFalse(k["EnteringCar"]["petla"])

    def test_ruchy_wlasciciela_prosto_na_kosci_reszta_humanoid(self):
        # build Codexa 5.11: surowe krzywe po ścieżkach → „brak krzywych Hips” → T-poza. 10.11 (APK dd490e9): Humanoid wykrzywia
        # ruchy zrobione pod Patryka. Ruchy właściciela: obrót kości w świecie po NAZWIE kości (nie ścieżki krzywych), bez Humanoid;
        # ruchy Braxa (inny szkielet) nadal przez HumanPoseHandler.
        h = czytaj(ED / "BraxClaudePatrykHumanoid.cs")
        self.assertIn("new HumanPoseHandler(animator.avatar, source.transform)", h)
        self.assertIn("writer.SetHumanPose(ref pose)", h)
        b = czytaj(ED / "BraxClaudePatrykBezposrednio.cs")
        self.assertIn("b.rotation = s.rotation * Quaternion.Inverse(s0) * T0[b];", b)
        self.assertIn("PatrykKosciPoNazwie(source.GetComponentsInChildren<Transform>(true))", b)
        p = czytaj(ED / "BraxClaudeOsiedlePatryk.cs")
        self.assertIn("PatrykRuchWlasciciela(sciezka)", p)
        self.assertIn("PatrykBezposrednio(hero, hips, kosci, spoczynek, sciezka, k, out uwagi)", p)
        self.assertIn("PatrykHumanoid(hero, awatarRuchu", p)
        self.assertIn("PatrykWybierzAwatar", p)
        imp = czytaj(ED / "BraxPrywatneImport.cs")
        self.assertIn("ModelImporterAnimationType.Human", imp)
        self.assertIn('assetPath.StartsWith(Katalog + "ruchy/patryk_owner/")', imp)
        self.assertIn("ModelImporterAnimationType.Generic", imp)

    def test_wsiadanie_nie_jest_wymagane_do_startu(self):
        p = czytaj(ED / "BraxClaudeOsiedlePatryk.cs")
        wym = re.search(r"PatrykWymagane = \{([^}]*)\}", p).group(1)
        self.assertNotIn("EnteringCar", wym)
        self.assertIn('"CarIdle"', wym)

    def test_paczka_22_spojna_z_katalogiem(self):
        katalog = json.loads(czytaj(TU / "patryk-owner-22-manifest.json"))
        transfer = ROOT / "unity-city/Prywatne/import-patryk-animations-20261106"
        m = json.loads(czytaj(transfer / "manifest.json"))
        self.assertEqual(m["zip_sha256"], katalog["animation_archive"]["sha256"])
        self.assertEqual(len(katalog["clips"]), 22)
        for i, part in enumerate(m["chunks"]):
            raw = (transfer / ("part-%03d.enc" % i)).read_bytes()
            self.assertEqual(len(raw), part["bytes"])
            self.assertEqual(hashlib.sha256(raw).hexdigest(), part["sha256"])
        # w publicznym repo tylko szyfr — żadnych FBX ani zipów
        for f in transfer.iterdir():
            self.assertNotIn(f.suffix.lower(), (".fbx", ".zip"), f.name)

    def test_prywatne_instaluje_paczke_dla_osiedla_bez_zatrzymania_buildu(self):
        src = czytaj(TU / "prywatne.py")
        fn = src[src.index("def patryk_i_ruchy"):src.index("def sprawdz")]
        self.assertIn("install_patryk_owner_22(CEL / \"ruchy\")", fn)
        blok = fn[fn.index("if osiedle_build:\n        from import_patryk_transfer"):fn.index("staging =")]
        self.assertIn("try:", blok)
        self.assertIn("except Exception", blok)
        self.assertIn("BRAXHORN_PATRYK_OWNER_22 DO POPRAWKI", blok)

    def test_bez_klucza_instalator_nie_wypakowuje(self):
        import import_patryk_owner_22 as imp
        with tempfile.TemporaryDirectory() as d, mock.patch.dict(os.environ, {"KLUCZ": ""}):
            with self.assertRaises(RuntimeError):
                imp.install(destination=pathlib.Path(d))
            self.assertEqual(list(pathlib.Path(d).rglob("*.fbx")), [])


class WsiadanieDoAuta(unittest.TestCase):
    def setUp(self):
        self.rt = czytaj(RT / "BraxAutoGracza.cs")

    def wejdz(self):
        return self.rt[self.rt.index("IEnumerator Wejdz()"):self.rt.index("const float Skora")]

    def wyjdz(self):
        return self.rt[self.rt.index("IEnumerator Wyjdz("):self.rt.index("public void PrzedTeleportem")]

    def test_wsiadanie_ruchem_wlasciciela_potem_poza_kierowcy(self):
        w = self.wejdz()
        self.assertIn('p.clips["EnteringCar"]', w)
        i_ruch = w.index('CrossFade("EnteringCar"')
        i_car = w.index('CrossFade("CarIdle"', i_ruch)
        self.assertLess(i_ruch, i_car)
        self.assertIn("WrapMode.ClampForever", w)       # Once po końcu = poza wiązania (T-poza) — zabronione
        self.assertIn("StartCoroutine(Drzwi(0f,1f", w)    # drzwi otwierają się, gdy sięga do klamki
        self.assertIn("korektaWsiadania", w)
        self.assertIn("ObrotRuchu", w)

    def test_bez_klipu_stara_droga(self):
        w = self.wejdz()
        self.assertIn("else", w)
        self.assertIn('yield return Drzwi(0f,1f,.55f);', w)

    def test_wysiadanie_tym_samym_ruchem_od_konca(self):
        w = self.wyjdz()
        self.assertIn('CrossFade("EnteringCar"', w)
        self.assertIn("ruch.time=ruch.length", w)
        self.assertIn("ruch.speed=-ruch.length/czas", w)
        self.assertIn("WrapMode.ClampForever", w)
        self.assertLess(w.index('CrossFade("EnteringCar"'), w.index('CrossFade("Idle",.3f'))

    def test_build_mierzy_biodra_i_obrot_konca_wsiadania(self):
        e = czytaj(ED / "BraxClaudeAutoPatryka.cs")
        self.assertIn('anim.GetClip("EnteringCar")', e)
        self.assertIn("wsiadanie.SampleAnimation(anim.gameObject,wsiadanie.length)", e)
        self.assertIn("car.korektaWsiadania=korektaWsiadania", e)
        self.assertIn("car.obrotWsiadania=obrotWsiadania", e)
        self.assertIn("BRAXHORN_AUTO_PATRYKA_WSIADANIE", e)
        self.assertIn("public Vector3 korektaWsiadania; public float obrotWsiadania;", self.rt)


class AutaBezRozuIZKolami(unittest.TestCase):
    def setUp(self):
        self.src = czytaj(ED / "BraxClaudeOsiedleAutaModel.cs")
        self.fn = self.src[self.src.index("static AutoSzablon PrzygotujAuto"):]

    def test_kazdy_material_przez_konwerter_androida(self):
        self.assertIn("static Material MaterialAutaAndroid(Material source)", self.src)
        conv = self.src[self.src.index("static Material MaterialAutaAndroid"):self.src.index("static int autoSiatki")]
        for znak in ("isSupported", "gltf", "Universal Render Pipeline", "Shader Graphs/"):
            self.assertIn(znak, conv)
        self.assertIn("BraxCityMaterials.Textured(", conv)   # tekstury zachowane
        self.assertIn("materials[i] = MaterialAutaAndroid(materials[i]);", self.fn)

    def test_czesci_skinned_wypiekane_nie_wylaczane(self):
        self.assertNotIn("smr.enabled = false", self.fn)
        self.assertIn("sk.BakeMesh(baked)", self.fn)
        self.assertIn("AddComponent<MeshRenderer>()", self.fn)

    def test_auto_bez_czterech_kol_odrzucone_bez_zatrzymania_buildu(self):
        self.assertRegex(self.fn, r"A\.ileKol != 4")
        auta = czytaj(ED / "BraxClaudeOsiedleAuta.cs")
        self.assertRegex(auta, r"try \{ var s = PrzygotujAuto\(src, tmp\);.*\}\s*\n\s*catch \(Exception e\)")


if __name__ == "__main__":
    unittest.main()
