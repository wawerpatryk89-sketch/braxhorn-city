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
    def test_vehicle_packs_owner_12_11(self):
        # 01:30 „motory globalnie do wszystkich miast, rejonów”; 01:32 „rowery globalnie… bierz tylko te, które się nadają”
        for nazwa, target, sztuki in [("braxhorn-sklep-motory-01", "pojazdy_motory_01", 5), ("braxhorn-sklep-rowery-01", "pojazdy_rowery_01", 2),
                                      ("braxhorn-sklep-meble-01", "meble_01", 5), ("braxhorn-sklep-muzeum-01", "muzeum_eksponaty_01", 9),
                                      # 01:47 „jeżeli dostałeś coś, czego nie potrzebujesz, zapisz to na gałęzi… albo na siłowni”
                                      ("braxhorn-sklep-boks-01", "silownia_boks_01", 8), ("braxhorn-sklep-boks-02", "silownia_boks_02", 7),
                                      ("braxhorn-sklep-wyciag-01", "silownia_wyciag_01", 6), ("braxhorn-sklep-ring-01", "silownia_ring_01", 1),
                                      ("braxhorn-sklep-ring-02", "silownia_ring_02", 1)]:
            m = json.loads((PRYW / nazwa / "manifest.json").read_text())
            self.assertEqual(m["target"], target)
            glb = [f for f in m["files"] if f.startswith(target + "/") and f.endswith(".glb")]
            self.assertEqual(len(glb), sztuki, nazwa)
            self.assertIn("oryginal/spec.json", m["files"])
        spec = (ROOT / "unity-tools/claude/pakuj_tripo.py").read_text()
        self.assertIn('"openssl", "cms", "-encrypt"', spec)
        self.assertNotIn("env:KLUCZ", spec); self.assertNotIn("-decrypt", spec)   # pakowanie tylko kluczem publicznym
        self.assertTrue((ROOT / "unity-tools/claude/obroc_arkusz.py").is_file())
        # 01:51 meble: fotel relaks w apartamencie Tower, konsola i witryny w gabinecie szefa (z zastępstwem bez paczki)
        t = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionTylyB.cs").read_text()
        self.assertIn('BraxMiastoAssety.Model("meble_01", plik)', t)
        for n in ["fotel_relaks_skora_czarna", "konsola_marmur_czarny", "witryna_orzech_szklo"]:
            self.assertIn('"' + n + '"', t)
        self.assertIn('if (!Tripo(A, "Salon: fotel relaks"', t)

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


class SilowniaIArena(unittest.TestCase):
    # 12.11 01:17–01:48: „zrobisz tak samo siłownię porządną u nas. I worki tak samo. Wszystko”, „tak samo masz wyposażyć szatnie
    # w arenie… worki do treningu, gruszki”, „jeżeli coś… jest lepsze od tego, co mamy, zastępuj”
    def test_gym_uses_owner_gear_with_code_fallback(self):
        g = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionGym.cs").read_text()
        for n in ['"gruszka_refleks_na_stojaku"', '"worek_stojacy_czarno_zloty"', '"stojak_hantli_3_poziomy"', '"lawka_plaska"', '"lawka_regulowana"',
                  '"lawka_olimpijska_ze_stojakiem"', '"maszyna_prostowanie_nog"', '"modlitewnik"', '"lawka_do_prostownikow"', '"suwnica_hack"']:
            self.assertIn(n, g)
        self.assertIn("if (!tripo1 && Miejsce(2.6f, .8f, 1.0f", g)          # stojak hantli z kodu tylko bez paczki
        self.assertIn("if (!tripo1 && Miejsce(1.3f, .5f, .6f", g)           # ławka płaska z kodu tylko bez paczki

    def test_arena_locker_rooms_warmup(self):
        a = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspaArena.cs").read_text()
        self.assertIn("ApRozgrzewka(brax, sg, x0 + t, x1, cz, F, h);", a)
        for n in ['"worek_wiszacy_czarno_zloty"', '"gruszka_refleks_na_stojaku"', '"gruszka_podwojnie_zaczepiona"', '"kask_bokserski_czerwony"',
                  '"kask_bokserski_niebieski"', '"ochraniacz_tulowia_czerwony"', '"tarcza_thai_"']:
            self.assertIn(n, a)
        self.assertIn("Vector3.Min(a, b), Vector3.Max(a, b)", a)             # szatnia gości (oś z odwrócona) bez odwróconych brył


class MotoryRowery(unittest.TestCase):
    # 01:30 „motory globalnie do wszystkich miast”, 01:32 „rowery… bierz tylko te, które się nadają”, 02:05 „dokończ to, co masz”
    def test_motorcycles_on_parking_bikes_in_workshop(self):
        m = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoMotory.cs").read_text()
        r = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionRuchMiasta.cs").read_text()
        u = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxUlicePierzeje.cs").read_text()
        self.assertIn('"pojazdy_motory_01"', m); self.assertIn('"pojazdy_rowery_01"', m)
        self.assertIn("ile = Mathf.Clamp(ile, 1, 2)", m)                     # najwyżej 2 na stanowisku — kierownice się nie stykają
        self.assertIn("BraxMiastoMotory.NaStanowisku(gm, m.c, m.przod, m.rozmiar", r)
        self.assertIn("if (dlaMotorow.Contains(i)) continue;", r)          # stanowisko z motocyklami nie dostaje auta
        self.assertIn("BraxMiastoMotory.Rower(bg, i,", u)
        self.assertNotIn("Rower(", u.split("ZASADA")[0][:0])


class SportTowar(unittest.TestCase):
    # 12.11 02:06: „jak kolega dostarczył zasoby w 3D, to wykorzystaj je, gdzie potrzeba. Premium, max”
    def test_braxhorn_sport_uses_owner_goods_with_fallback(self):
        s = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxMiastoSport.cs").read_text()
        for n in ["sklep_sport_01", "_wieszak", "sklep_walka_01/buty_bokserskie_czarno_zlote", "sklep_buty_02/", "sklep_walka_01/kask_bokserski_czerwony",
                  "sklep_walka_01/ochraniacz_szczeki_w_pudelku", '"pilka_nozna"', '"mata_zwinieta_fioletowa"', "sklep_manekiny_02/manekin_dres_czarny_kaptur"]:
            self.assertIn(n, s)
        self.assertIn("postawione == 0", s); self.assertIn("if (butyT > 0) continue;", s); self.assertIn("sprzetN == 0", s)   # z kodu tylko bez paczek
        self.assertNotIn("manekin_bialy", s)                                 # białe manekiny odrzucone (11.11 23:22)


if __name__ == "__main__":
    unittest.main()
