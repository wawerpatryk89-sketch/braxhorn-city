"""Owner 16.11.2026 22:52-23:08: LOTOTO BRAX CASH - "Kolektura ... i w telewizorach losowanie", "taki pokoik i gościa, który
będzie przy tym bębnie ... automatycznie ... 2 razy w tygodniu", the name "Lototo Brax Cash", the studio "w tym BRAXHORN BET",
his poster ("DO WYGRANIA 10 000 000 ZŁ · DWA LOSOWANIA W TYGODNIU · WYNIKI NA ŻYWO W TELEWIZJI · CODZIENNIE O 20:00").

23:42 "60 kuleczek ... losujemy 20", 17.11 ten numbers on the coupon: the kolektura's counter (a coupon of ten of 60 for
3 zł, five hits the stake back ... ten the kumulacja), the draws on Wednesday and Saturday at 20:00 by the phone's clock live
in the studio (the host by the glass drum, 60 balls, twenty down the chute into two rows of cups), the
studio camera's picture on every screen of the BET (a live picture, never a film), the numbers from the draw's date (the same
on every phone), the replay at the glass. The rules, the show and the counter are checked by automaty_sim."""
import hashlib
import json
import pathlib
import re
import subprocess
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
GAME = (RT / "BraxChampionsLototo.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoLototo.cs").read_text(encoding="utf-8")
TOWER = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")
PACK = ROOT / "unity-city/Prywatne/braxhorn-mistrzowie-lototo-20261116"
PACK_N = ROOT / "unity-city/Prywatne/braxhorn-mistrzowie-nieruchomosci-20261116"


class Rules(unittest.TestCase):
    def test_ten_of_60_twenty_drawn(self):
        self.assertIn('nazwa = "LOTOTO BRAX CASH", klucz = "braxhorn_lototo_kupony", marka = Tablica.Marka, pula = 60, losowane = 20, typy = 10, cena = 3,', GAME)
        self.assertIn("wygrane = new[] { 0, 0, 0, 0, 0, 3, 6, 25, 200, 5000 },", GAME)
        self.assertIn("kumulacjaStart = 10000000", GAME)                         # his poster: 10 000 000 zł
        self.assertIn("dni = new[] { DayOfWeek.Wednesday, DayOfWeek.Saturday }", GAME)
        # the draw from its date: the same on every phone
        self.assertIn("public int[] Losuj(int id) => Wybierz(new System.Random(unchecked(id * 7919 + 1049 + Ziarno)), losowane);", GAME)

    def test_brax_szostka_for_r1(self):
        self.assertIn('nazwa = "BRAX SZÓSTKA", klucz = "braxhorn_szostka_kupony", marka = Tablica.MarkaSzostka, pula = 40, losowane = 6, typy = 6, cena = 3,', GAME)
        self.assertIn("public static Zasady Gra(int i) => i == 1 ? BraxSzostka : BraxCash;", GAME)
        self.assertIn("IList<Renderer> ekrany, int gra = 0)", KIT)
        self.assertIn('"BRAX SZÓSTKA" };', GAME)

    def test_checked_by_the_simulator(self):
        sim = (TOOLS / "automaty_sim/Program.cs").read_text(encoding="utf-8")
        for c in ("lototo: the next draw Wednesday 18.11 20:00", "lototo: every number as often", "lototo: 75 394 027 566 coupons of ten of 60",
                  "lototo: all ten 1 in 408 074", "lototo: 50-60 % back without the kumulacja", "lototo: one coupon in five wins",
                  "lototo: five hits on 13.5 % of coupons", "lototo: the balls stay inside the drum", "lototo: the twenty in their cups in the order drawn",
                  "lototo: a coupon of ten bought for the next draw", "lototo: the same draw on every phone",
                  "brax szóstka: six different numbers of 1-40 every draw, not the BET's", "brax szóstka: 3 838 380 coupons of six of 40",
                  "brax szóstka: its counter"):
            self.assertIn(c, sim)
        self.assertIn("BraxChampionsLototo.cs", (TOOLS / "automaty_sim/automaty_sim.csproj").read_text(encoding="utf-8"))


class Show(unittest.TestCase):
    def test_live_at_the_clock_and_the_replay(self):
        self.assertIn("if (od < Z.Program && !(pokaz && !powtorka && pokazId == id)) Pokaz(id, od, false);", GAME)
        self.assertIn('return Z.nazwa + " · POWTÓRKA LOSOWANIA ("', GAME)
        # the camera renders only during a show near the player, one frame when the board changes
        self.assertIn("kamera.enabled = false;", GAME)
        self.assertIn("if (pokaz && blisko && Time.unscaledTime >= nastepnaKlatka) { nastepnaKlatka = Time.unscaledTime + 1f / 15f;", GAME)
        self.assertIn("if (ekran) { ekran.mainTexture = rt;", GAME)

    def test_no_film_on_the_screens(self):
        bet = TOWER[TOWER.index("static void Bet("):TOWER.index("// ---------------- T10: elliptical")]
        self.assertIn("var r=tv.GetComponent<Renderer>();if(r)betEkrany.Add(r);", bet)
        self.assertIn("UnityEngine.Object.DestroyImmediate(tv);", bet)                     # never the film player
        self.assertIn("lototo=BraxChampionsKasynoSprzet.Lototo(inside,kat,", bet)
        self.assertIn("if (ekrany != null) foreach (var r in ekrany) if (r) { r.sharedMaterial = lotEkran; ne++; }", KIT)
        self.assertNotIn("VideoPlayer", GAME + KIT)


class Studio(unittest.TestCase):
    def test_machine_balls_and_host(self):
        self.assertIn("var m0 = new Vector3(-.9f, 0f, -.9f); float bR = .36f, cY = 1.30f;", KIT)   # a 72 cm glass drum
        self.assertIn("lot.kule = new Transform[Z.pula];", KIT)
        self.assertIn('ZapiszSiatke(KulaSiatka((n - 1) % 8, (n - 1) / 8, .03f), "lototo_kula_" + n.ToString("00"))', KIT)
        self.assertIn('W.Obsada("prowadzący losowania LOTOTO BRAX CASH"', KIT)
        self.assertIn("lot.miejsca = new Transform[Z.losowane];", KIT)
        self.assertIn("int wRz = Mathf.Min(10, Z.losowane)", KIT)                     # twenty cups: two rows of ten
        self.assertIn('var cam = kam.AddComponent<Camera>(); cam.enabled = false;', KIT)

    def test_owner_poster_1_to_1(self):
        self.assertIn('Wczytaj(LototoWlasciciel + "/plakaty/lototo_brax_cash.png"', KIT)
        self.assertIn("if (src != null) Wklej(lp, lw, lh, src, sw, sh, LototoLogo[0], sh - LototoLogo[1] - LototoLogo[3], LototoLogo[2], LototoLogo[3], 0, 0, lw, lh);", KIT)
        x, y, w, h = map(int, re.search(r"LototoLogo = \{ (\d+), (\d+), (\d+), (\d+) \}", KIT).groups())
        self.assertTrue(x + w <= 1024 and y + h <= 1536)
        for pack, rel in ((PACK, "plakaty/lototo_brax_cash.png"), (PACK_N, "plakaty/wawer_nieruchomosci_monika.png")):
            man = json.loads((pack / "manifest.json").read_text(encoding="utf-8"))
            self.assertEqual(list(man["files"]), [rel])
            self.assertEqual(man["recipientCertificateSha256"], "4a1bdb0377eaa7dd294f62e13bd6e12efd0bfa1a0249ad604ca9f2632b560fd5")
            for part in man["parts"]:
                self.assertEqual(hashlib.sha256((pack / part["name"]).read_bytes()).hexdigest(), part["sha256"])
        inst = (TOOLS / "install_owner_media.py").read_text(encoding="utf-8")
        self.assertIn('("braxhorn-mistrzowie-lototo-20261116", "LototoWlasciciel"', inst)
        self.assertIn('("braxhorn-mistrzowie-nieruchomosci-20261116", "NieruchomosciWlasciciel"', inst)
        for d in ("LototoWlasciciel", "NieruchomosciWlasciciel"):
            tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/" + d], cwd=ROOT, capture_output=True, text=True).stdout
            self.assertEqual(tracked.strip(), "")

    def test_board_words_sized_to_their_ink(self):
        self.assertIn("lot.slowaSzer = lotSzer;", KIT)
        self.assertIn("float Szer(int i)", GAME)
        for w in ("NA ŻYWO", "POWTÓRKA", "WYNIKI", "KUMULACJA", "NASTĘPNE LOSOWANIE", "ŚRODA", "SOBOTA", "LOTOTO BRAX CASH"):
            self.assertIn('"' + w + '"', GAME)

    def test_preview_compiles_it(self):
        for p in ("wiezowce_preview/wiezowce_preview.csproj", "automaty_sim/automaty_sim.csproj"):
            self.assertIn("BraxChampionsLototo.cs", (TOOLS / p).read_text(encoding="utf-8"))
        self.assertTrue((RT / "BraxChampionsLototo.cs.meta").exists())
        self.assertTrue((ED / "BraxChampionsKasynoLototo.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
