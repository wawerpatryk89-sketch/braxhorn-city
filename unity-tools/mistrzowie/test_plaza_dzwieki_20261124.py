"""The beach's sounds (owner 24.11 01:31: "Wykonałeś mi te dźwięki z plaży?").

At every real sea of the region - the city beach, the villa island, the seafront villa's private beach - the surf's roar
from the nearest stretch of shore, each wave heard as it reaches the shore in time with the sea's picture, gulls now and
then. Made in the game (BraxChampionsMorzeDzwiek: no recordings, no licences), on a worker thread; played by
BraxChampionsSzumMorza from the shore field's waterline (BraxChampionsMorzeBrzeg.Brzeg), baked by the build."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"

SYN = (RT / "BraxChampionsMorzeDzwiek.cs").read_text(encoding="utf-8")
GRA = (RT / "BraxChampionsSzumMorza.cs").read_text(encoding="utf-8")
MORZE = (RT / "ChampionsMorze.shader").read_text(encoding="utf-8")
PLAZA = (ED / "BraxChampionsPlaza.cs").read_text(encoding="utf-8")
BRZEG = (ED / "BraxChampionsMorzeBrzeg.cs").read_text(encoding="utf-8")
FOUND = (ED / "BraxChampionsFoundation.cs").read_text(encoding="utf-8")
WYSPA = (ED / "BraxChampionsWyspaMorze.cs").read_text(encoding="utf-8")
NAB = (ED / "BraxChampionsNabrzezeMorze.cs").read_text(encoding="utf-8")


class Dzwieki(unittest.TestCase):
    def test_every_real_sea_has_them(self):
        self.assertIn('BraxChampionsPlaza.SzumMorza(root,BraxChampionsPlaza.PoleMiasta,0f,null,"PLAŻA | szum morza, fale i mewy");', FOUND)
        self.assertLess(FOUND.index('var root=new GameObject("BRAXHORN | MIASTO MISTRZOW | EXTERIOR FOUNDATION").transform;'),
                        FOUND.index("BraxChampionsPlaza.SzumMorza(root,"))
        self.assertIn("var pole = Pole(meshes, sea); PoleMiasta = pole;", PLAZA)
        self.assertIn('BraxChampionsPlaza.SzumMorza(g, pole, 1f, null, "WILLA 2 | szum morza, fale i mewy");', WYSPA)
        self.assertIn('"WILLA 3 | szum morza, fale i mewy");', NAB)
        # the seafront's surf softer in its window's side fade, exactly as the shader's _Boki
        self.assertIn("w => Mathf.SmoothStep(0f, 1f, Mathf.Min(w.x - d.x0 * K, d.x1 * K - w.x) / (Boki * K))", NAB)
        self.assertIn("return _Boki > 0 ? smoothstep(0, _Boki, min(u, 1 - u) / _BrzegRect.z) : 1;", MORZE)

    def test_from_the_waterline_chained_along_the_shore(self):
        self.assertIn("public List<Vector3> Brzeg(float krok)", BRZEG)
        self.assertIn("for (int i = 1; i < punkty.Count; i++) if (punkty[i].x < punkty[start].x) start = i;", BRZEG)
        self.assertIn("var punkty = pole.Brzeg(8f);", PLAZA)
        self.assertIn("c.wzdluzZ = wzdluzZ; c.dlugosc = Dlugosc; c.okres = Okres;", PLAZA)
        # checked on the real beach and the real seafront
        self.assertIn("MORZE_SIM sounds:", (TOOLS / "morze_sim/Program.cs").read_text(encoding="utf-8"))
        self.assertIn('Console.WriteLine("NABRZEZE_MORZE sounds: "', (TOOLS / "wille_preview/WilleMain.cs").read_text(encoding="utf-8"))

    def test_each_wave_in_time_with_the_picture(self):
        # the shader's phase: frac(d / length + _Time.y / period + Faza(w)); at the waterline d = 0
        self.assertIn("float p = frac(d / _Fala.x + _Time.y / _Fala.y + Faza(w.xz));", MORZE)
        self.assertIn("float Wzdluz(float x) { return sin(x * .0131) * .35 + sin(x * .0047 + 1.7) * .55; }", MORZE)
        self.assertIn("static float Wzdluz(float x) => Mathf.Sin(x * .0131f) * .35f + Mathf.Sin(x * .0047f + 1.7f) * .55f;", GRA)
        self.assertIn("float Faza(float2 w) { return Wzdluz(w.x) + _WzdluzZ * Wzdluz(w.y * 1.13 + 77); }", MORZE)
        self.assertIn("float Faza(Vector3 w) => Wzdluz(w.x) + wzdluzZ * Wzdluz(w.z * 1.13f + 77f);", GRA)
        self.assertIn("float t = Time.timeSinceLevelLoad, prog = 1f - BraxChampionsMorzeDzwiek.Uderzenie / okres;", GRA)   # _Time.y
        self.assertIn("float ph = t / okres + Faza(q), f = ph - Mathf.Floor(ph);", GRA)
        # the run-up rises from p = 0: the clip's water arrives Uderzenie s after it starts
        self.assertIn("float run = smoothstep(0, .10, p) * (1 - smoothstep(.10, .80, p));", MORZE)
        self.assertIn("public const float SzumSekund = 14f, FalaSekund = 6.5f, Uderzenie = .55f;", SYN)
        self.assertIn("public const float Dlugosc = 20f, Okres = 7f", PLAZA)
        self.assertEqual(float(re.search(r"SzumSekund = ([\d.]+)f", SYN).group(1)) % 7.0, 0.0)          # whole wave periods

    def test_made_in_the_game_off_the_main_thread(self):
        self.assertNotIn("UnityEngine", SYN)                                                  # pure C#: a worker thread may run it
        self.assertIn("praca = Task.Run(() => new[] { BraxChampionsMorzeDzwiek.Szum(), BraxChampionsMorzeDzwiek.Fala(),", GRA)
        self.assertIn("var c = AudioClip.Create(n, x.Length, 1, BraxChampionsMorzeDzwiek.SR, false);", GRA)
        for s in ("Resources.Load", "AssetDatabase", ".ogg", ".wav", ".mp3"):
            self.assertNotIn(s, GRA)
        # a phone's speaker plays nothing under ~200 Hz: the roar is pink noise to 1.2 kHz, not a brown rumble alone
        self.assertIn("float roar = hp.Hp(lo2.Lp(lo.Lp(pink, 1200f), 1200f), 120f)", SYN)
        # the gulls: a long call (a squeal, then 8-12 yelps at ~4.5 a second), "kyow" calls
        self.assertIn("int nut = 8 + r.Next(5); float t = .7f;", SYN)
        self.assertIn("t += .2f + .03f * (float)r.NextDouble();", SYN)

    def test_quiet_far_away_no_step(self):
        self.assertIn("if (od > zasieg + 20f || !Gotowe())", GRA)
        self.assertIn("float daleko = 1f - Mathf.SmoothStep(0f, 1f, (od - (zasieg - 30f)) / 50f);", GRA)
        self.assertIn("tloPoz = Vector3.MoveTowards(tloPoz, p, 40f * Time.deltaTime);", GRA)
        for f in ("BraxChampionsMorzeDzwiek.cs.meta", "BraxChampionsSzumMorza.cs.meta"):
            self.assertRegex((RT / f).read_text(encoding="utf-8"), r"guid: [0-9a-f]{32}")


if __name__ == "__main__":
    unittest.main()
