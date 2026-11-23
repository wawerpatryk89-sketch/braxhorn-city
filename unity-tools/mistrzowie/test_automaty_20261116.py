"""Casino slot machines (owner 16.11.2026 18:12-18:22: "porządne maszyny do grania ... mechaniczne i komputerowe ... w nich
komputer i mogą normalnie działać", "te maszyny mają funkcjonować", the same equipment in the SKY KASYNO and KASYNO WAWER).

The pay tables are recomputed here from the C# source over the full cycle (every drum / reel stop), so a changed strip
or table that makes the machines give money away (or keep it all) fails in CI; the game component itself is played by
automaty_sim (dotnet, 20 000 spins a machine: every win = the table, the reels on a stop, the lever back, the jackpot)."""
import hashlib
import itertools
import json
import re
import subprocess
import unittest
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city/Assets"
RULES = (ASSETS / "Mistrzowie/BraxChampionsAutomatyZasady.cs").read_text(encoding="utf-8")
GAME = (ASSETS / "Mistrzowie/BraxChampionsAutomaty.cs").read_text(encoding="utf-8")
KIT = (ASSETS / "Editor/Mistrzowie/BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")
TOWER = (ASSETS / "Editor/Mistrzowie/BraxChampionsWiezowce.cs").read_text(encoding="utf-8")
HALL = (ASSETS / "Editor/Mistrzowie/BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
PACK = ROOT / "unity-city/Prywatne/braxhorn-mistrzowie-kasyno-grafiki-20261116"


def ints(text):
    return [int(v) for v in re.findall(r"-?\d+", text)]


def array(name):
    m = re.search(r"%s\s*=\s*\{(.*?)\};" % re.escape(name), RULES, re.S)
    return m.group(1)


MECH = [ints(r) for r in re.findall(r"new\[\]\{([^}]*)\}", array("public static readonly int[][] PaskiMech"))]
SCREEN = [ints(r) for r in re.findall(r"new\[\]\{([^}]*)\}", array("public static readonly int[][] PaskiEkran"))]
TRZY = ints(array("public static readonly int[] Trzy"))
TABLE = [ints(r) for r in re.findall(r"\{([\d,]+)\}", array("public static readonly int[,] Ekranowy"))]
CONST = {k: int(v) for k, v in re.findall(r"(Wisnie2|Wisnia1) = (\d+)", RULES)}
SEVEN, STAR, CHERRY, KINDS = 0, 2, 6, 7


def mechanical(a, b, c):
    if a == b == c == SEVEN:
        return -1
    for k in range(1, KINDS):
        if k == STAR:
            continue
        if all(v in (k, STAR) for v in (a, b, c)) and k in (a, b, c):
            return TRZY[k]
    if a == CHERRY and b in (CHERRY, STAR):
        return CONST["Wisnie2"]
    return CONST["Wisnia1"] if a == CHERRY else 0


def line(s):
    if s[0] == SEVEN:
        n = 1
        while n < 5 and s[n] == SEVEN:
            n += 1
        first = SEVEN
    else:
        first, n = -1, 0
        for x in s:
            if x == SEVEN:
                break
            if x == STAR:
                n += 1
                continue
            if first < 0:
                first, n = x, n + 1
                continue
            if x == first:
                n += 1
            else:
                break
        if first < 0:
            first = STAR
    return TABLE[first][n - 3] if n >= 3 else 0


class PayTables(unittest.TestCase):
    def test_strips_are_real_reels(self):
        # the owner's seven symbols, 20 stops a reel, no two equal symbols side by side (round the drum too)
        for r in MECH + SCREEN:
            self.assertEqual(len(r), 20)
            self.assertTrue(set(r) <= set(range(KINDS)))
            self.assertTrue(all(r[i] != r[(i + 1) % 20] for i in range(20)), r)
        self.assertEqual([STAR in r for r in MECH], [False, True, False])          # the STAR only on the middle drum
        for k in range(KINDS):                                                     # every line of the pay glass can come up
            if k != STAR:
                self.assertTrue(all(k in r for r in MECH), k)
        self.assertEqual(len(TABLE), KINDS)
        self.assertEqual(re.search(r"public const int Symbole = (\d+)", RULES).group(1), "7")

    def test_mechanical_return_like_a_real_machine(self):
        win = jack = hits = 0
        for a, b, c in itertools.product(*MECH):
            m = mechanical(a, b, c)
            if m < 0:
                jack += 1
            elif m:
                win += m
                hits += 1
        n = 20 ** 3
        start = int(re.search(r"JackpotStart = (\d+)", GAME).group(1))
        rtp = win / n + jack * (start / 10) / n
        self.assertEqual(jack, 1)                                          # three SEVENS: 1 in 8 000
        self.assertTrue(.85 < rtp < .98, rtp)
        self.assertTrue(2.5 < n / hits < 5, n / hits)                     # fruit reels: a win every 3-4 spins

    def test_screen_return_like_a_real_machine(self):
        reels = [SCREEN[i] for i in (0, 1, 2, 1, 0)]
        freq = [Counter(r) for r in reels]
        e = 0
        for combo in itertools.product(range(KINDS), repeat=5):
            w = 1
            for r, s in enumerate(combo):
                w *= freq[r][s]
                if not w:
                    break
            if w:
                e += w * line(combo)
        rtp = e / 20 ** 5
        self.assertTrue(.88 < rtp < .97, rtp)
        self.assertIn("if (okno[0, l[0]] == 0)", RULES)                    # the SEVEN pays only as itself


class Game(unittest.TestCase):
    def test_plays_and_pays(self):
        self.assertIn("public sealed class BraxChampionsAutomaty : MonoBehaviour, IBraxInterakcja", GAME)
        self.assertIn("void OnEnable() { BraxInterakcje.Dodaj(this); }", GAME)
        self.assertIn("if (!BraxChampionsKasyno.Wydaj(st))", GAME)                    # the stake of the game (23.11: chosen on the stool)
        self.assertIn("return Graj(i, stawka);", GAME)                                 # standing: the machine's own
        self.assertIn("BraxChampionsKasyno.Wplac(wygrana);", GAME)
        self.assertIn("if (jackpot) wygrana = BraxChampionsKasyno.WezJackpot();", GAME)
        self.assertIn("m.dzwignia.localRotation = Quaternion.AngleAxis(k * 62f, Vector3.right);", GAME)
        # the positions come back into one turn after every spin (no float drift however long it is played)
        self.assertIn("s.poz[r] = ((Mathf.RoundToInt(s.poz[r]) % Z.Stopy) + Z.Stopy) % Z.Stopy;", GAME)
        self.assertIn("s.poz[r] % Z.Stopy * 18f", GAME)
        # the jackpot grows by 2 % of a stake (kept in hundredths)
        self.assertIn("(Setne + 2L * Math.Max(0, stawka))", GAME)

    def test_cheap_displays(self):
        # one picture for all jackpot toppers and one for the boards; a machine's own meter only once it is played
        self.assertIn("jTex = Wspolna(jackpoty, JackW, JackH, out jPx);", GAME)
        self.assertIn("tTex = Wspolna(tablice, TabW, TabH, out tPx);", GAME)
        self.assertIn("// the machine's own meter from now on", GAME)

    def test_sim_tool(self):
        sim = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")
        proj = (ROOT / "unity-tools/mistrzowie/automaty_sim/automaty_sim.csproj").read_text(encoding="utf-8")
        for f in ("BraxChampionsAutomatyZasady.cs", "BraxChampionsAutomaty.cs", "AutomatyStubs.cs"):
            self.assertIn(f, proj)
        self.assertIn('Check(w == expect, "win " + w + " = pay table " + expect);', sim)


class Cabinets(unittest.TestCase):
    def test_two_real_cabinets(self):
        for part in ("THREE REAL DRUMS", "the chrome lever with the red", "tilted back 15 degrees", "the curved topper"):
            self.assertIn(part, KIT)
        self.assertIn("public const float BebenR = .175f, BebenSzer = .11f", KIT)
        self.assertIn("RozstawEkran = .76f, RozstawMech = .82f, RozstawMieszany = .80f", KIT)   # room for the lever
        # every other machine the other type, every machine another theme than its neighbours (owner 16.11 19:13)
        self.assertIn("int gra = gry == null ? ((i + row + start) % 2 == 1 ? G.Video : G.Mech) : gry[(i + 2 * row + start) % gry.Length];", KIT)
        self.assertIn("int motyw = (start + i + 3 * row) % Motywy.Length;", KIT)
        self.assertIn('BraxChampionsFoundation.LibraryMetal("kasyno_" + id, c)', KIT)  # lacquered steel, chrome: the metal scan
        self.assertIn('BraxChampionsFoundation.LibraryLeather("kasyno_skora_czarna"', KIT)
        self.assertNotIn("BraxChampionsLetters3D.Text(b, chrom", KIT)          # the top glass is printed (1 600 triangles less)

    def test_owner_strips_1_to_1(self):
        # owner 16.11 19:13: six reel strips -> six themes; each cut into the game's seven symbols (his bands, his frame)
        self.assertIn('Wlasciciel + "/paski/" + Paski[t] + ".png"', KIT)
        self.assertIn("DO POPRAWKI KASYNO_PASKI_WLASCICIELA", KIT)
        names = re.search(r"Paski = \{([^}]*)\}", KIT).group(1)
        strips = re.findall(r'"([a-z_]+)"', names)
        self.assertEqual(len(strips), 6)
        ks = [ints(r) for r in re.findall(r"new\[\]\{([^}]*)\}", KIT[KIT.index("PasekSymbole ="):KIT.index("PasekY =")])]
        ys = [ints(r) for r in re.findall(r"new\[\]\{([^}]*)\}", KIT[KIT.index("PasekY ="):KIT.index("PasekY =") + 600])]
        self.assertEqual(len(ks), 6); self.assertEqual([len(k) for k in ks], [len(y) for y in ys])
        for k, y in zip(ks, ys):
            self.assertEqual(sorted(v for v in k if v >= 0), list(range(KINDS)))   # each of his strips carries the seven
            self.assertEqual(y, sorted(y)); self.assertTrue(all(0 < v < 2172 for v in y))
        man = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))
        for n in strips:
            self.assertIn("paski/%s.png" % n, man["files"])

    def test_encrypted_pack(self):
        man = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(len(man["files"]), 14)
        for n in ("logo_braxhorn_bet", "ramka_ekranu", "plakat_postaw_na_walke", "plakat_noc_wielkich_walk", "plakat_pas_mistrzowski",
                  "plakat_queens_of_the_cage", "plakat_walka_wieczoru", "plakat_fight_night"):
            self.assertIn("bet/%s.png" % n, man["files"])
        self.assertEqual(man["recipientCertificateSha256"], "4a1bdb0377eaa7dd294f62e13bd6e12efd0bfa1a0249ad604ca9f2632b560fd5")
        part = PACK / man["parts"][0]["name"]
        self.assertEqual(part.stat().st_size, man["parts"][0]["bytes"])
        self.assertEqual(hashlib.sha256(part.read_bytes()).hexdigest(), man["parts"][0]["sha256"])
        # the repository is public: nothing of his in the clear
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/KasynoWlasciciel"], cwd=ROOT,
                                 capture_output=True, text=True).stdout
        self.assertEqual(tracked.strip(), "")
        inst = (ROOT / "unity-tools/mistrzowie/install_owner_media.py").read_text(encoding="utf-8")
        self.assertIn('KASYNO = "braxhorn-mistrzowie-kasyno-grafiki-20261116"', inst)
        self.assertIn('KASYNO_DIR = ROOT / "unity-city/Assets/Source/Mistrzowie/KasynoWlasciciel"', inst)

    def test_no_picture_on_a_face(self):
        # the region's z-fight check: a picture stands >= 6 mm in front of the face it is on
        przed = float(re.search(r"const float Przed = ([\d.]+)f;", KIT).group(1))
        self.assertGreaterEqual(przed, .006)
        self.assertIn("m = BraxChampionsHarbour.Nakladka(m);", KIT)

    def test_both_casinos(self):
        self.assertIn('BraxChampionsKasynoSprzet.Zacznij(inside,kat,"SKY KASYNO")', TOWER)
        self.assertEqual(TOWER.count("BraxChampionsKasynoSprzet.Bank("), 2)
        self.assertEqual(TOWER.count("BraxChampionsKasynoSprzet.Karuzela("), 2)
        self.assertIn('BraxChampionsKasynoSprzet.Zacznij(g,body,"KASYNO WAWER")', HALL)
        self.assertIn("foreach(float x in new[]{-24.5f,-19.5f,19.5f,24.5f})BraxChampionsKasynoSprzet.Bank(", HALL)
        self.assertEqual(HALL.count("BraxChampionsKasynoSprzet.Karuzela("), 2)          # two a wing (front and back)
        self.assertIn('if(slots!=128||carousels!=4)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL slot machines "', HALL)
        self.assertIn('if(slots!=40)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_SKY_KASYNO slot machines "', TOWER)

    def test_meta_files(self):
        for f in ("Mistrzowie/BraxChampionsAutomaty.cs", "Mistrzowie/BraxChampionsAutomatyZasady.cs", "Mistrzowie/BraxChampionsAutomatyGry.cs",
                  "Editor/Mistrzowie/BraxChampionsKasynoSprzet.cs", "Editor/Mistrzowie/BraxChampionsKasynoGry.cs"):
            self.assertTrue((ASSETS / (f + ".meta")).exists(), f)

    def test_previews_compile_the_real_files(self):
        for p in ("wiezowce_preview/wiezowce_preview.csproj", "casino_preview/casino_preview.csproj"):
            proj = (ROOT / "unity-tools/mistrzowie" / p).read_text(encoding="utf-8")
            for f in ("BraxChampionsKasynoSprzet.cs", "BraxChampionsKasynoGry.cs", "BraxChampionsAutomaty.cs", "BraxChampionsAutomatyZasady.cs",
                      "BraxChampionsAutomatyGry.cs", "AutomatyStubs.cs"):
                self.assertIn(f, proj)




ROUL = (ASSETS / "Mistrzowie/BraxChampionsRuletka.cs").read_text(encoding="utf-8")


class Roulette(unittest.TestCase):
    """Owner 16.11 19:13: "ruletki porobić prawdziwe" - a European wheel as the real one, in both casinos."""

    def test_real_wheel(self):
        kolo = ints(re.search(r"public static readonly int\[\] Kolo = \{([^}]*)\}", ROUL).group(1))
        self.assertEqual(kolo, [0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 24, 16, 33, 1, 20, 14, 31,
                                9, 22, 18, 29, 7, 28, 12, 35, 3, 26])
        red = ints(re.search(r"static readonly int\[\] Czerwone = \{([^}]*)\}", ROUL).group(1))
        self.assertEqual(red, [1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36])
        # red and black alternate round the wheel (the zero apart)
        self.assertTrue(all((kolo[i] in red) != (kolo[i + 1] in red) for i in range(1, 36)))
        # the order runs clockwise seen from above: the pockets' angles and the painted ring agree
        self.assertIn("public static float KatKieszeni(int n) => -Array.IndexOf(Kolo, n) * 360f / 37f;", ROUL)
        self.assertIn("int n = BraxChampionsRuletka.Zasady.Kolo[(37 - j) % 37];", KIT)

    def test_bets_and_pay(self):
        self.assertIn('Pola = { "1-18", "PARZYSTE", "CZERWONE", "CZARNE", "NIEPARZYSTE", "19-36" }', ROUL)
        self.assertIn("if (n <= 0) return false;                                             // the zero takes the outside bets", ROUL)
        self.assertIn("if (Zasady.WygrywaZaklad(id, s.wynik)) wyg += b * Zasady.Mnoznik(id);", ROUL)     # 23.11: every chip by its odds
        self.assertIn("public static int Mnoznik(int id) => id <= 36 ? 36 : id < Tuziny ? 2 : 3;", ROUL)    # 35:1, 1:1, 2:1
        # the ball is aimed: its speed is scaled so it reaches the pockets over the drawn number, then settles in it
        self.assertIn("s.skala = 1f - d / Mathf.Max(1f, Mathf.Abs(bez));", ROUL)
        self.assertIn("float rel = Mathf.LerpAngle(a0, s.wynikKat, 1f - Mathf.Pow(1f - u, 3f));", ROUL)
        sim = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")
        self.assertIn('"ball in pocket " + wynik', sim)

    def test_real_table(self):
        for part in ("velour_velvet", "smoked_walnut_veneer", "the ball track", "eight chrome deflectors", "the chrome turret"):
            self.assertIn(part, KIT)
        self.assertIn("public const float TorR = .37f, TorY = .050f, KieszenR = .217f, KieszenY = .006f, KulkaR = .0095f;", KIT)
        self.assertIn('W.Obsada("krupier ruletki"', KIT)

    def test_both_casinos(self):
        self.assertIn("BraxChampionsKasynoSprzet.Ruletka(kas,new Vector3(0f,y,-9.4f),0f);", TOWER)
        self.assertIn("foreach(float x in new[]{-6.6f,6.6f})BraxChampionsKasynoSprzet.Blackjack(kas,new Vector3(x,y,-9.6f),180f);", TOWER)
        self.assertIn("BraxChampionsKasynoSprzet.Blackjack(kas,new Vector3(bx,HallFloor+.016f,-8.5f),180f);", HALL)
        self.assertIn("BraxChampionsKasynoSprzet.Ruletka(kas,new Vector3(rx,HallFloor+.016f,rz),rz>0f?0f:180f);", HALL)
        self.assertIn('if(roulettes!=4)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL roulettes "', HALL)
        self.assertTrue((ASSETS / "Mistrzowie/BraxChampionsRuletka.cs.meta").exists())

    def test_premium_display(self):
        """Owner 16.11 20:19: "ten taki 17 na zdjęciu, tego nie można zrobić bardziej premium?" - the LED board became a
        portrait screen on a column (after the TCS John Huxley displays): the last number big, 12 before it, red / zero /
        black over the last 100, 4 hot and 4 cold numbers - live, rebuilt by the game after every spin."""
        self.assertIn("public static void Zbuduj(Mesh m, int[] historia, int[] sto, int n)", ROUL)
        self.assertIn("if (s.siatka) Wyswietlacz.Zbuduj(s.siatka, s.historia, s.sto, s.stoN);", ROUL)
        # the start and the generator's sample are one played table: the history is the last 13 of the 100
        self.assertIn("for (int k = 0; k < historia.Length; k++) historia[k] = sto[sto.Length - 1 - k];", ROUL)
        self.assertIn("Wyswietlacz.Poczatek(los, s.historia, s.sto);", ROUL)
        self.assertIn("BraxChampionsRuletka.Wyswietlacz.Poczatek(new System.Random(77 + idx), hist, sto);", KIT)
        # the quads face the player (+z), as the slab's glass
        self.assertIn("tr.Add(i); tr.Add(i + 2); tr.Add(i + 1); tr.Add(i); tr.Add(i + 3); tr.Add(i + 2);", ROUL)
        for label in ('"BRAXHORN"', '"POPRZEDNIE"', '"OSTATNIE 100"', '"CZERWONE", "ZERO", "CZARNE"', '"GORĄCE"', '"ZIMNE"'):
            self.assertIn(label, KIT)
        self.assertNotIn("PaintTablica", ROUL)                                      # the old 64 x 128 LED board is gone
        sim = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")
        self.assertIn('"display shows " + wynik', sim)



GRY = (ASSETS / "Mistrzowie/BraxChampionsAutomatyGry.cs").read_text(encoding="utf-8")
KIT2 = (ASSETS / "Editor/Mistrzowie/BraxChampionsKasynoGry.cs").read_text(encoding="utf-8")
PACK2 = ROOT / "unity-city/Prywatne/braxhorn-mistrzowie-kasyno-grafiki-20261116-2"


def gry_array(name):
    return ints(re.search(r"%s\s*=\s*\{(.*?)\};" % re.escape(name), GRY, re.S).group(1))


class NewGames(unittest.TestCase):
    """Owner 16.11 20:28: "musiałbyś zrobić różne gry w automatach ... lepiej rozstawić ... pokaż, jak się robi luksus" -
    VIDEO POKER, KENO and KOŁO MISTRZÓW beside the two reel slots, working, in banks and on carousels in both casinos."""

    def test_video_poker_full_pay(self):
        # Jacks or Better 9/6, the real full-pay table (99.54 % with the best play), for one at the max bet
        self.assertEqual(gry_array("public static readonly int[] Wyplata"), [0, 1, 2, 3, 4, 6, 9, 25, 50, 800])
        self.assertIn('"POKER KRÓLEWSKI"', GRY)
        # the AUTO HOLD follows the simple strategy in its order (Wizard of Odds' 16 rules)
        order = ["DoKrolewskiego(k, 4)", "u == Full || u == KolorU || u == Strit", "DoPokera(k, 4)", "u == DwiePary", "u == ParaOdWaleta",
                 "DoKrolewskiego(k, 3)", "DoKoloru(k)", "Takie(k, 2)) != 0", "Otwarty(k)", "Wysokie(k, true)", "DoPokera(k, 3)",
                 "Wysokie(k, false)", "Dziesiatka10(k)", "Ranga(k[i]) >= Walet) return 1 << i"]
        at = [GRY.index(o) for o in order]
        self.assertEqual(at, sorted(at))
        sim = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")
        for c in ('"video poker RTP "', '"AUTO HOLD marked"', '"held card stays"', '"poker pays "'):
            self.assertIn(c, sim)

    def test_keno_return(self):
        from math import comb
        keno = GRY[GRY.index("public static class KenoGra"):]
        pay = ints(re.search(r"public static readonly int\[\] Wyplata = \{([^}]*)\}", keno).group(1))
        self.assertEqual(pay, [0, 0, 0, 1, 6, 100, 2400])
        rtp = sum(p * comb(6, c) * comb(74, 20 - c) / comb(80, 20) for c, p in enumerate(pay))
        self.assertAlmostEqual(rtp, .9202, places=3)                                # Las Vegas video keno: 90-91 %
        self.assertIn("public const int Pola = 80, Kule = 20, Typy = 6;", GRY)

    def test_wheel_machine_return(self):
        strip3 = gry_array("public static readonly int[] Pasek3")
        self.assertEqual(len(strip3), 20); self.assertEqual(strip3.count(7), 1)
        self.assertEqual([v if v != 7 else 5 for v in strip3], MECH[2])              # the stepper's third reel, one grapes -> KOŁO
        seg = gry_array("public static readonly int[] Segmenty")
        self.assertEqual(len(seg), 22)
        sevens = int(re.search(r"public const int Siodemki = (\d+);", GRY).group(1))
        line_pay = 0
        for a, b, c in itertools.product(MECH[0], MECH[1], strip3):
            m = mechanical(a, b, c)
            line_pay += sevens if m < 0 else m
        rtp = line_pay / 8000 + strip3.count(7) / 20 * (sum(seg) / len(seg) / 10)
        self.assertTrue(.93 < rtp < .97, rtp)                                       # 95.5 %, as real wheel slots
        self.assertIn("if (m.gra != G.Kolo) BraxChampionsKasyno.Dorzuc(st);", GAME)       # not on the linked jackpot
        self.assertIn("s.koloCel = s.koloKat + 3f * 360f + Mathf.Repeat(KG.KatSegmentu(s.koloSeg) - s.koloKat, 360f);", GAME)

    def test_owner_art_1_to_1(self):
        man = json.loads((PACK2 / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(sorted(man["files"]), ["karty/dama.png", "karty/krol.png", "karty/rewers.png", "karty/walet.png",
                                                "szyldy/arkusz_szyldy_monety_logo.png", "toppery/topper_keno.png",
                                                "toppery/topper_kolo_mistrzow.png", "toppery/topper_video_poker.png",
                                                "zetony/zetony_5_25_100_500_1000.png"])
        self.assertEqual(man["recipientCertificateSha256"], "4a1bdb0377eaa7dd294f62e13bd6e12efd0bfa1a0249ad604ca9f2632b560fd5")
        for part in man["parts"]:
            f = PACK2 / part["name"]
            self.assertEqual(hashlib.sha256(f.read_bytes()).hexdigest(), part["sha256"])
        inst = (ROOT / "unity-tools/mistrzowie/install_owner_media.py").read_text(encoding="utf-8")
        self.assertIn('KASYNO_2 = "braxhorn-mistrzowie-kasyno-grafiki-20261116-2"', inst)
        # his toppers whole, his court cards whole inside the card's margin, the corner indices on the margin
        self.assertIn("if (src != null) { Wklej(px, w, h, src, sw, sh, 0, 0, sw, sh, 0, 0, w, h); return px; }", KIT2)
        self.assertIn("if (fig[f] != null) Wklej(px, w, h, fig[f], fw[f], fh[f], 0, 0, fw[f], fh[f], ax, ay, aw, ah);", KIT2)
        self.assertIn('Wczytaj(dir + "rewers.png"', KIT2)
        self.assertIn('Wlasciciel + "/zetony/zetony_5_25_100_500_1000.png"', KIT2)
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/KasynoWlasciciel"], cwd=ROOT,
                                 capture_output=True, text=True).stdout
        self.assertEqual(tracked.strip(), "")

    def test_carousels_and_mixed_banks(self):
        self.assertIn("float rb = .36f / Mathf.Tan(Mathf.PI / n) + .03f;", KIT2)     # the cabinets' back corners clear
        self.assertIn("var dlugie=new[]{Mech,Video,Kolo,Poker,Mech,Video,Kolo,Keno};var krotkie=new[]{Video,Keno,Kolo,Poker};", HALL)
        for cyc in ([0, 1, 4, 2, 0, 1, 4, 3], [1, 3, 4, 2]):                         # neighbours differ, also round the cycle
            self.assertTrue(all(cyc[i] != cyc[(i + 1) % len(cyc)] for i in range(len(cyc))))
        self.assertIn("BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(-9.5f,y,2.0f),8,new[]{Kolo,Video},0,2.95f);", TOWER)
        self.assertIn("BraxChampionsKasynoSprzet.Karuzela(kas,new Vector3(9.8f,y,2.0f),8,new[]{Poker,Keno},0,2.95f);", TOWER)



OBS = (ASSETS / "Mistrzowie/BraxChampionsKasynoObsluga.cs").read_text(encoding="utf-8")
KIT3 = (ASSETS / "Editor/Mistrzowie/BraxChampionsKasynoWejscie.cs").read_text(encoding="utf-8")


class Entrance(unittest.TestCase):
    """Owner 16.11 20:29: "Porządna recepcja, konkretna, na maksa ... szatnia tam powinna być ... pokaż, jak się robi luksus" -
    the security arches with velvet ropes, the players club desk, the coat check, the cashier's cage; working."""

    def test_service_works(self):
        self.assertIn("public sealed class BraxChampionsKasynoObsluga : MonoBehaviour, IBraxInterakcja", OBS)
        self.assertIn("public const int Powitanie = 1000, Wymiana = 1000;", OBS)
        self.assertIn("if (!Klub)", OBS)                                                 # the welcome free play once
        self.assertIn('"SZATNIA · TWÓJ NUMEREK: "', OBS)
        self.assertIn("q.kurtka.gameObject.SetActive(Numerek > 0)", OBS)                  # his jacket hangs while it is there
        sim = (ROOT / "unity-tools/mistrzowie/automaty_sim/Program.cs").read_text(encoding="utf-8")
        for c in ('"the welcome only once"', '"the jacket back for the tag"', '"the cashier changes 1000"'):
            self.assertIn(c, sim)

    def test_owner_signs_1_to_1(self):
        # his sheet cut into its parts (found by their transparency), each on a black lacquered plaque, nothing of ours on it
        self.assertIn('Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png"', KIT3)
        parts = [ints(r) for r in re.findall(r"\{ (\d+, \d+, \d+, \d+) \}", KIT3[KIT3.index("static readonly int[,] Arkusz"):KIT3.index("ArkNazwy")])]
        self.assertEqual(len(parts), 11)
        self.assertTrue(all(x + w <= 2172 and y + h <= 724 for x, y, w, h in parts))
        self.assertIn("if (src != null) Wklej(px, w, h, src, sw, sh, Arkusz[i, 0], sh - Arkusz[i, 1] - Arkusz[i, 3], Arkusz[i, 2], Arkusz[i, 3], 0, 0, w, h);", KIT3)

    def test_cloakroom_hangs_the_owners_jackets(self):
        # 16.11 22:37 "na wieszakach to powiesić normalne ciuchy, te co mamy, nie? Kurtki": the shared shop packs' hanger cuts
        for n in ("sklep_ubrania_01/ubranie_trencz_bezowy", "sklep_ubrania_02/ubranie_ramoneska_czarna", "sklep_ubrania_01/ubranie_kurtka_puchowa_czarna",
                  "sklep_ubrania_02/ubranie_bomberka_khaki", "sklep_ubrania_02/ubranie_katana_jeansowa"):
            self.assertIn('"' + n + '"', KIT3)
        self.assertIn('BraxMiastoAssety.Model(pack, n.Substring(sl + 1) + "_wieszak")', KIT3)
        self.assertIn("BraxMiastoAssety.Przygotuj(o, false, false);", KIT3)        # shared meshes: not static, no shadows
        self.assertIn("x += wlasne ? .15f : .11f", KIT3)                            # 12-13 cm thick cuts 15 cm apart
        self.assertIn("if (!wlasne || !KurtkaNaWieszaku(", KIT3)                   # the code coats only without the packs
        self.assertIn("lod.SetLODs(new[] { new LOD(.04f, rs) });", KIT3)
        self.assertIn("var jego = Kurtka(KurtkaGracza);", KIT3)                    # the player's own jacket too
        self.assertIn('Debug.Log("KASYNO_SZATNIA "', KIT3)

    def test_both_casinos(self):
        for f in ("Bramka(kas,new Vector3(0f,fl,-22.4f),0f)", "Recepcja(kas,new Vector3(-8.0f,fl,-20.5f),90f,4.0f,true)",
                  "Szatnia(kas,new Vector3(8.0f,fl,-20.5f),-90f,3.6f,2,true)", "Kasa(kas,new Vector3(13.8f,fl,-21.4f),0f,3)",
                  "SzyldWiszacy(kas,new Vector3(0f,6.0f,0f),BraxChampionsKasynoSprzet.ArkRuletka,2.6f,HallCeiling)"):
            self.assertIn("BraxChampionsKasynoSprzet." + f, HALL)
        for f in ("Recepcja(kas,new Vector3(-3.0f,y,-3.6f),90f,1.4f,false)", "Szatnia(kas,new Vector3(-9.8f,y,-12.0f),0f,2.0f,1,true)",
                  "Kasa(kas,new Vector3(-12.15f,y,-9.3f),90f,2)"):
            self.assertIn("BraxChampionsKasynoSprzet." + f, TOWER)
        self.assertIn('if(obsluga!=5)BraxBuildQuality.Warn("DO POPRAWKI T11_CASINO_HALL service points "', HALL)
        self.assertIn('if(obsluga!=4)BraxBuildQuality.Warn("DO POPRAWKI TOWER_T09_SKY_KASYNO service points "', TOWER)
        for f in ("Mistrzowie/BraxChampionsKasynoObsluga.cs", "Editor/Mistrzowie/BraxChampionsKasynoWejscie.cs"):
            self.assertTrue((ASSETS / (f + ".meta")).exists(), f)


if __name__ == "__main__":
    unittest.main()
