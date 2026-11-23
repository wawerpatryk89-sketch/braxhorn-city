"""The casino games played from their seats (owner 23.11 17:35: "siadasz na krzesło i od razu pokazuje Ci się sama maszyna
i kręcisz maszyną ... wygrana, przegrana, wszystko ... siadasz przy pokerze, widać tylko stół, karty"), the money wheel and
the dice that never moved (17:48 "to koło fortuny się nie kręciło", 17:54 "kości nawet tej gry nie widziałem").

The rules themselves are played in automaty_sim (the machines, the roulette layout, blackjack split / double, Hold'em, baccarat,
sic bo, the money wheel with the money checked to the credit); here the wiring, the framing and the fixes."""
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RT = ROOT / "unity-city/Assets/Mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
ST = (RT / "BraxChampionsStanowisko.cs").read_text(encoding="utf-8")
STUB = (TOOLS / "wiezowce_preview/StanowiskoStub.cs").read_text(encoding="utf-8")
GRY = {n: (RT / f"BraxChampions{n}.cs").read_text(encoding="utf-8") for n in ("Automaty", "Blackjack", "Holdem", "Bakarat", "Ruletka", "SicBo", "KoloFortuny")}
SPRZET = (ED / "BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")
KGRY = (ED / "BraxChampionsKasynoGry.cs").read_text(encoding="utf-8")
KBJ = (ED / "BraxChampionsKasynoBlackjack.cs").read_text(encoding="utf-8")
KSB = (ED / "BraxChampionsKasynoSicBo.cs").read_text(encoding="utf-8")
WEJ = (ED / "BraxChampionsKasynoWejscie.cs").read_text(encoding="utf-8")
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
RUCH = (ED / "BraxChampionsRuchome.cs").read_text(encoding="utf-8")
EXPL = (ED / "BraxChampionsExplorer.cs").read_text(encoding="utf-8")
BATCH = (ED / "BraxChampionsMeshBatch.cs").read_text(encoding="utf-8")
SIM = (TOOLS / "automaty_sim/Program.cs").read_text(encoding="utf-8")


class Stanowisko(unittest.TestCase):
    def test_runs_after_the_players_camera_and_owns_the_screen(self):
        self.assertIn("[DefaultExecutionOrder(5000)]", ST)
        self.assertIn("kam.transform.SetPositionAndRotation(kPos, kRot);", ST)
        self.assertIn("if (tc && tc.enabled) tc.enabled = false; else tc = null;", ST)           # the walking HUD rests
        self.assertIn("if (tc) { tc.enabled = true; Input.simulateMouseWithTouches = false; }", ST)
        self.assertIn("ukryte[k].shadowCastingMode = ShadowCastingMode.ShadowsOnly;", ST)        # the body out of the picture
        self.assertIn("ukryte[k].shadowCastingMode = tryby[k];", ST)                             # and back as it was
        self.assertIn("if (Input.GetKeyDown(KeyCode.Escape)) Koniec(true);", ST)                 # the phone's back button: WSTAŃ
        self.assertIn('if (ui.Przycisk(ui.wstan, "WSTAŃ"', ST)
        self.assertIn("if (BraxChampionsStanowisko.Aktywne) return;", (RT / "BraxChampionsGryHud.cs").read_text(encoding="utf-8"))

    def test_the_tap_that_sat_down_never_plays(self):
        self.assertIn("ui.Nowa();", ST)
        self.assertIn("bool Gotowy => trzyma && Time.unscaledTime - od > .35f;", ST)
        self.assertIn("if (e.type != EventType.MouseUp || !Gotowy || !r.Contains(e.mousePosition)) return false;", ST)

    def test_seats_only_on_the_players_floor_at_their_real_height(self):
        self.assertIn("public const float Zasieg = 28f, PietroNad = 2.6f, PietroPod = 1.5f;", ST)
        self.assertIn("bool on = d.y < PietroNad && d.y > -PietroPod && d.x * d.x + d.z * d.z < Zasieg * Zasieg;", ST)
        self.assertIn("var k = Inst(); s.surfaceY = s.transform.position.y;", ST)
        self.assertIn("BraxChampionsStanowisko.Siedziska(transform);", GRY["Automaty"])     # the bar's stools, the sofas

    def test_the_framing_fills_the_band_over_the_panel(self):
        """Kadruj in Python: the object's top and bottom land at the band's edges (16:9 and 20:9, FOV 55)."""
        self.assertIn("float d = Mathf.Max(h / (2f * f * tv), w / (2f * .94f * asp * tv));", ST)
        self.assertIn("var L = c - u * ((.5f - m) * hv);", ST)
        for asp in (16 / 9, 20 / 9):
            H = 1080.0; s = H; m = s * .024
            g = (m * .6 + s * .078 + m) / H; d0 = (H - s * .2 - m * .5 - m * .6) / H
            tv = math.tan(math.radians(27.5)); w, h = .72, .47
            d = max(h / (2 * (d0 - g) * tv), w / (2 * .94 * asp * tv)); hv = 2 * d * tv; mm = (g + d0) / 2
            shift = (.5 - mm) * hv                                    # the centre seen above the screen's middle by this
            top = .5 - (h / 2 + shift) / hv; bottom = .5 + (h / 2 - shift) / hv
            self.assertGreaterEqual(top, g - 1e-6); self.assertLessEqual(bottom, d0 + 1e-6)
            self.assertAlmostEqual(min(top - g, d0 - bottom), 0.0, places=4)

    def test_every_game_is_a_seat_game(self):
        for n, src in GRY.items():
            self.assertRegex(src, r"public sealed class BraxChampions\w+ : MonoBehaviour, IBraxInterakcja, IBraxStanowisko", n)
            for m in ("public string Tytul(int i)", "public void Kadr(int i, Camera cam, out Vector3 oko, out Quaternion obrot)",
                      "public void Panel(int i, U ui)", "public void Wstal(int i)"):
                self.assertIn(m, src, n + ": " + m)
        for n in ("Blackjack", "Holdem", "Bakarat"):
            self.assertIn("BraxChampionsStanowisko.Rejestruj(this, stoly[i].siedziska[k], i * 8 + k);", GRY[n], n)
        for n in ("Ruletka", "SicBo", "KoloFortuny"):
            self.assertIn("BraxChampionsStanowisko.Wejdz(this, ", GRY[n], n)

    def test_the_stand_in_has_the_real_api(self):
        """The previews and the simulator compile the games with StanowiskoStub: every public name the games use is in both."""
        real = ST[ST.index("public sealed class Ui"):]; stub = STUB[STUB.index("public sealed class Ui"):]
        for name in ("Przycisk", "Pole", "Stawka", "Tekst", "Rzut", "Dotyk", "Obrys", "Etykieta", "Klik", "Zloto", "Krem", "Czerwien", "Zielen", "Szary", "Granat",
                     "dol", "m", "s"):
            for src in (real, stub):
                self.assertRegex(src, r"public[^;{]*\b" + name + r"\b", name)
        for name in ("Rejestruj", "Siedziska", "Wejdz", "Wstan", "Kadruj", "KadrStolu", "KadrBlatu", "Stawki", "Gra"):
            self.assertIn(" " + name + "(", ST, name); self.assertIn(" " + name + "(", STUB, name)


class Siedziska(unittest.TestCase):
    def test_every_stool_is_a_seat_at_its_leather_top(self):
        self.assertIn("m.siedzisko = W.Siedzisko(at + q * new Vector3(0f, TaboretSiedzisko, Taboret), q * Vector3.back, G.Nazwy[m.gra]);", SPRZET)
        self.assertEqual(SPRZET.count("        Siedzisko(m, at, q);"), 1)          # the classic, the video, the wheel machine
        self.assertEqual(KGRY.count("        Siedzisko(m, at, q);"), 1)            # video poker and keno
        # the stool's leather: the lathe from .605 up .10 (its top .705)
        self.assertIn("b.Lathe(skora, c + new Vector3(0f, .605f, 0f), new[] { new Vector2(0f, 0f), new Vector2(.16f, 0f), new Vector2(.195f, .02f), new Vector2(.205f, .05f),", SPRZET)
        self.assertIn("public const float TaboretSiedzisko = .705f;", SPRZET)

    def test_every_table_chair_is_a_seat(self):
        self.assertIn("b.Cylinder(skora, cp + Vector3.up * .64f, .22f, .09f, 24, Quaternion.identity, true, true);", KBJ)   # its top .73
        self.assertIn("public const float KrzesloSiedzisko = .73f;", KBJ)
        for gra in ('"BLACKJACK"', '"CASINO HOLD\'EM"', '"BAKARAT"'):
            self.assertIn(gra + ");", (ED / ("BraxChampionsKasyno" + {'"BLACKJACK"': "Blackjack", '"CASINO HOLD\'EM"': "Holdem", '"BAKARAT"': "Bakarat"}[gra] + ".cs")).read_text(encoding="utf-8"))


class Ruletka(unittest.TestCase):
    def test_the_layout_is_the_generators(self):
        src = GRY["Ruletka"]
        self.assertIn("public const float UkladXW = .39f, UkladL = 1.65f, UkladZN = .435f, UkladD = .85f, UkladY = .775f;", src)
        self.assertIn("const float UkladXW = .39f, UkladL = 1.65f, UkladZN = .435f, UkladD = .85f, UkladY = .775f;", SPRZET)
        self.assertIn("const int UkW = 2048, UkH = 1024, UkM = 40, UkKol = 140, UkZero = 180, UkRzad = 200, UkSiatka = 320;", SPRZET)
        # the six boxes where the generator lays their chips: PoleX / PoleZ
        for e in range(6):
            gen_x = .39 - (180 + 2 * 140 * e + 140) / 2048 * 1.65
            rt_x = .39 - (180 + e * 280 + 140) / 2048 * 1.65
            self.assertAlmostEqual(gen_x, rt_x, places=6)
        self.assertIn("if (id < Tuziny) { int e = id - Zewn; rozmiar = new Vector2(280f, 140f); return new Vector2(180 + e * 280 + 140, 110f); }", src)
        self.assertIn("static float PoleZ => UkladZN - 110f / UkH * UkladD;", SPRZET)

    def test_every_bet_returns_36_of_37(self):
        def wins(bet, n):
            red = {1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36}
            if bet <= 36: return n == bet
            if n <= 0: return False
            if bet < 43: return [n <= 18, n % 2 == 0, n in red, n not in red, n % 2 == 1, n >= 19][bet - 37]
            if bet < 46: return (n - 1) // 12 == bet - 43
            return (n - 1) % 3 == bet - 46
        mult = lambda b: 36 if b <= 36 else 2 if b < 43 else 3
        for b in range(49):
            self.assertAlmostEqual(sum(wins(b, n) for n in range(37)) * mult(b) / 37, 36 / 37, places=9)


class Ruchome(unittest.TestCase):
    def test_the_batch_still_marks_static_so_the_sweep_is_needed(self):
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);", BATCH)

    def test_moving_parts_lose_their_static_flags_before_the_scene_is_saved(self):
        self.assertIn("BraxChampionsRuchome.Odblokuj(scene);", EXPL)
        self.assertLess(EXPL.index("BraxChampionsRuchome.Odblokuj(scene);"), EXPL.index("if (!EditorSceneManager.SaveScene(scene, ScenePath))"))
        for part in ("D(c.kolo); D(c.klapka);", "A(c.kostki);", "D(c.zawias);", "A(c.skrzydla); D(c.stopien);",
                     "All<BraxChampionsObrot>(scene)) D(c.transform);", "D(t.rotor); D(t.kulka);", "A(m.bebny); D(m.dzwignia);"):
            self.assertIn(part, RUCH)
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(x.gameObject, 0); n++;", RUCH)
        self.assertTrue((ED / "BraxChampionsRuchome.cs.meta").exists())


class SicBo(unittest.TestCase):
    def test_its_chips_are_made_again(self):
        self.assertIn('            Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[i] = z;', KSB)
        for line in KSB.splitlines():
            if "//" in line:
                self.assertNotIn("Wstaw(czZeton", line.split("//", 1)[1])

    def test_its_sign_in_gold_3d_letters(self):
        self.assertIn('BraxChampionsKasynoSprzet.SzyldLitery(kas,new Vector3(0f,4.6f,12.95f),"SIC BO",1.5f,.50f,HallCeiling);', HALL)
        self.assertIn("BraxChampionsLetters3D.Text(b, zlotoKola, czern, nazwa, c + n * (hw + .002f) + Vector3.down * (h * .28f), 90f * f, h * .56f, .02f, .14f, w * .86f);", WEJ)


class Symulator(unittest.TestCase):
    def test_the_tables_are_played_with_the_money_checked(self):
        for s in ('"BLACKJACK at the chair: split + double, 3:2, bust, split aces: OK"', '"blackjack split+double pays 40 + 20 ("',
                  '"split aces: 21 pays even money, not 3:2 ("', '"hold\'em call = twice the ante"', '"baccarat pays "',
                  '"sic bo pays "', '"wheel pays "', '"roulette pays "', '"POWTÓRZ puts the last spin\'s chips"'):
            self.assertIn(s, SIM)


if __name__ == "__main__":
    unittest.main()
