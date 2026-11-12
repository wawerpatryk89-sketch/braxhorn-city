"""BRAXHORN R1 island arena. Static contract checks, NOT Unity scene or phone proof.

Stan po 9.11: wyspa i arena przywrócone z buildu 6.11 (commit 9d9aff5) na R1,
z decyzjami właściciela: żadnych ringów plenerowych obok areny, kostka wokół
areny, pokaz walki pominięty (zawodnicy z obozu nie są w gałęzi miasta),
każdy brak = ostrzeżenie (build idzie dalej), BraxBuildQuality nie istnieje na R1.

Run: python -m unittest test_island_arena_finish -v
"""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
REGION = ROOT / "unity-city" / "Assets" / "Editor" / "Claude" / "Region"
RUNTIME = ROOT / "unity-city" / "Assets" / "Claude" / "Runtime"
ISLAND = (REGION / "BraxMiastoWyspa.cs").read_text(encoding="utf8")
ARENA = (REGION / "BraxMiastoWyspaArena.cs").read_text(encoding="utf8")
DOMKI = (REGION / "BraxMiastoWyspaDomki.cs").read_text(encoding="utf8")
FINISH = (REGION / "BraxMiastoWyspaWykonczenie.cs").read_text(encoding="utf8")
ODBIOR = (REGION / "BraxWyspaOdbior.cs").read_text(encoding="utf8")
RUNTIME_FILES = ("BraxArenaPokaz.cs", "BraxWyspaAmbient.cs", "BraxWyspaKontrola.cs",
                 "BraxPiasekPowierzchnia.cs", "BraxWyspaDaleko.cs")

LITERALS = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"')


def strip_comments(source: str) -> str:
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", source)


def without_disabled_blocks(source: str, symbol: str) -> str:
    """Kod poza blokami #if <symbol> … (#else …) #endif — to, co kompiluje się bez symbolu."""
    out, skip = [], False
    for line in source.splitlines(keepends=True):
        stripped = line.strip()
        if stripped.startswith("#if " + symbol):
            skip = True
            continue
        if stripped.startswith("#else") and skip:
            skip = False
            continue
        if stripped.startswith("#endif"):
            skip = False
            continue
        if not skip:
            out.append(line)
    return "".join(out)


def delimiters_balanced(source: str) -> bool:
    masked = LITERALS.sub("", source)
    stack = []
    opening = {")": "(", "]": "[", "}": "{"}
    for char in masked:
        if char in "([{":
            stack.append(char)
        elif char in ")]}":
            if not stack or stack.pop() != opening[char]:
                return False
    return not stack


class ArenaIslandFinish(unittest.TestCase):
    def test_octagon_is_preserved_but_outdoor_rings_are_not_built(self):
        self.assertIn('Krok("klatka", ApKlatka)', ARENA)
        self.assertIn('Krok("trybuny"', ARENA)
        # Właściciel 9.11: „obok sceny ringi stały — do usunięcia”. Ringi() zostaje w kodzie, ale nie jest wołane.
        code = strip_comments(ISLAND)
        self.assertIsNone(re.search(r"\bRingi\(\)\s*;", code))
        self.assertIn("static void Ringi()", ISLAND)
        # odbiór zgłasza każdy pozostały ring/linę jako brak do poprawki
        self.assertIn('c.name == "kolizja: ring plenerowy" || c.name == "kolizja: liny ringu"', ODBIOR)
        self.assertIn("if (rings.Length > 0)", ODBIOR)
        self.assertIn('t.name.Contains("wyspa_ring_"', ODBIOR)

    def test_paving_is_part_of_original_terrain_not_overlay(self):
        self.assertIn("var tChodnik = new List<int>();", ISLAND)
        self.assertIn("mesh.subMeshCount = 3;", ISLAND)
        self.assertIn("mesh.SetTriangles(tChodnik, 2);", ISLAND)
        self.assertIn("r.sharedMaterials = new[] { mT, mP, mCh };", ISLAND)
        # 12.11: drobna siatka 6 × 6 i klasyfikacja po środku każdego trójkąta (gładka krawędź placu i pierścienia)
        self.assertIn("const int fine = 6;", ISLAND)
        self.assertIn("t1.Add(aa)", ISLAND)
        self.assertIn("concrete_pavers_03", ISLAND)
        # maska piasku: kostka (trzeci submesh) nie jest piaskiem
        self.assertIn("tT.Concat(tP).Concat(tChodnik)", ISLAND)

    def test_circular_path_is_centred_on_arena_and_fits_flat_area(self):
        # pierścień liczony od środka ARENY (AC = plac + 4 m), nie od środka placu
        self.assertIn("var arenaC = new Vector2(AC.x - CX, AC.z - CZ);", ISLAND)
        self.assertIn("Vector2.Distance(new Vector2(cx, cz), arenaC)", ISLAND)
        self.assertIn("Vector2.Distance(new Vector2(xx, zz), arenaC)", ISLAND)
        self.assertIn("d >= 26f && d <= 31.5f", ISLAND)
        self.assertIn("Mathf.Abs(zz - Plac.y) < 2.5f", ISLAND)
        self.assertIn("xx <= arenaC.x - 31f && xx >= Plac.x - 35.5f", ISLAND)
        self.assertIn("odAreny > 23f && odAreny < 35f", ISLAND)
        match = re.search(r"const float PlacR = ([0-9.]+)f", ISLAND)
        self.assertIsNotNone(match)
        self.assertGreater(float(match.group(1)), 31.5)
        arena_r = float(re.search(r"const float ArenaR = ([0-9.]+)f", ISLAND).group(1))
        # kostka zaczyna się za narożnikami ośmioboku (promień opisany) i kończy w strefie płaskiej placu (+4 m przesunięcia)
        self.assertGreater(26.0, arena_r)
        self.assertLessEqual(31.5 + 4.0, float(match.group(1)))

    def test_furniture_bins_and_signs_are_outside_annular_paver_path(self):
        self.assertIn("34.5f", ARENA)
        self.assertIn("var b = p + styczna * 2.6f", ARENA)
        self.assertIn("var l = p - styczna * 2.6f", ARENA)
        for k in range(3):
            for side in (-1, 1):
                theta = side * (0.25 + k * 0.35)
                radial = (-math.cos(theta), math.sin(theta))
                tangent = (-radial[1], radial[0])
                for offset in (0, -2.6, 2.6):
                    radius = math.hypot(34.5 * radial[0] + offset * tangent[0],
                                        34.5 * radial[1] + offset * tangent[1])
                    self.assertGreater(radius, 31.5 + 1.0)
        # kosze i tablice kierunkowe: xw = apC.x - apotema (24 cos 22.5°); promień od apC > 31.5
        apotema = 24.0 * math.cos(math.pi / 8)
        self.assertIn("new Vector3(xw - 10.4f, y, apC.z + side * 7f)", ARENA)
        self.assertIn("new Vector3(xw - 10.6f, y + 2.1f, apC.z + side * 5.2f)", ARENA)
        self.assertGreater(math.hypot(apotema + 10.4, 7.0), 31.5)
        self.assertGreater(math.hypot(apotema + 10.6, 5.2), 31.5)

    def test_boardwalk_profile_has_no_steps_and_continuous_colliders(self):
        # build 6.11: profil deptaka co 1 m, spadek ≤ 12 cm/m w obu kierunkach, pochylnia 1:10 na plac, kolizja każdego odcinka
        self.assertIn("Mathf.Max(mx + .25f, yPrev - .12f)", ISLAND)
        self.assertIn("Mathf.Max(prof[i].y, prof[i + 1].y - .12f)", ISLAND)
        self.assertIn('new GameObject("kolizja: deptak")', ISLAND)
        self.assertIn('new GameObject("kolizja: pochylnia deptaka")', ISLAND)
        self.assertIn("float lr = dh * 10f;", ISLAND)
        self.assertIn('Ostrzez("WYSPA_POMOST_STYK', ISLAND)

    def test_architecture_textures_missing_is_warning_not_throw(self):
        for material in ("apGranit", "apBeton", "apKamien", "apPlytki", "apDab"):
            self.assertIn(material, ARENA)
        self.assertIn("foreach (var mat in new[] { apGranit, apBeton, apKamien, apPlytki, apDab })", ARENA)
        self.assertIn('Ostrzez("BRAXHORN_WYSPA_DO_POPRAWKI brak tekstury architektury areny', ARENA)
        self.assertNotIn("BRAXHORN_WYSPA_P0: brak tekstury architektury", ARENA)

    def test_show_fight_is_skipped_without_camp_files(self):
        # zawodnicy z obozu (BraxObozCharacters) nie są w gałęzi miasta: pokaz = ostrzeżenie + return, kod pod symbolem
        self.assertIn("internal static bool ZawodnicyObozuDostepni", FINISH)
        self.assertIn("if (!ZawodnicyObozuDostepni)", FINISH)
        self.assertIn('Ostrzez("BRAXHORN_ARENA_POKAZ pominięty', FINISH)
        compiled = strip_comments(without_disabled_blocks(FINISH, "BRAX_OBOZ_W_MIESCIE"))
        self.assertNotIn("BraxObozCharacters.", compiled)
        self.assertNotIn("throw new", compiled)
        self.assertIn('Krok("walka pokazowa", ApWalkaPokazowa)', ARENA)
        # odbiór: brak pokazu bez plików obozu nie jest błędem wyspy
        self.assertIn("if (!BraxMiastoWyspa.ZawodnicyObozuDostepni)", ODBIOR)
        # runtime pokazu nie zależy od obozu
        self.assertNotIn("BraxObozCharacters", (RUNTIME / "BraxArenaPokaz.cs").read_text(encoding="utf8"))

    def test_no_hard_errors_and_no_build_quality_on_r1(self):
        for name, source in (("ISLAND", ISLAND), ("ARENA", ARENA), ("DOMKI", DOMKI), ("FINISH", FINISH), ("ODBIOR", ODBIOR)):
            with self.subTest(file=name):
                self.assertNotIn("BraxBuildQuality", strip_comments(source))
        self.assertEqual(strip_comments(ISLAND).count("throw new"), 0)
        self.assertEqual(strip_comments(DOMKI).count("throw new"), 0)
        self.assertEqual(strip_comments(ODBIOR).count("throw new"), 0)
        # jedyny throw areny: wewnątrz try w ApLogoMaty (łapany, zamieniany na ostrzeżenie)
        self.assertEqual(strip_comments(ARENA).count("throw new"), 1)
        self.assertIn('Ostrzez("BRAXHORN_WYSPA_DO_POPRAWKI logo maty', ARENA)
        # odbiór: krok nie rzuca dalej, kontrola zwraca listę braków
        self.assertNotIn("throw new InvalidOperationException(\"BRAXHORN_WYSPA_P0", ODBIOR)
        self.assertIn("public static List<string> Sprawdz(Transform root, bool pelnaScena = false)", ODBIOR)
        self.assertIn('Debug.LogWarning(Prefiks + nazwa + ": " + e);', ODBIOR)
        self.assertIn("internal static void Ostrzez(string tekst)", FINISH)

    def test_original_arena_cage_and_video_board_retained(self):
        for phrase in ('Krok("klatka", ApKlatka)',
                       'Krok("rampa i telebim", ApRampa)',
                       'Krok("hol (kasy, bramki)", ApHol)',
                       'Krok("dach z kratownicami", ApDach)',
                       'Krok("plac przed wejściem", ApPlacPrzed)'):
            self.assertIn(phrase, ARENA)
        self.assertIn('"BRAXHORN ARENA"', ARENA)

    def test_csharp_lexical_delimiters(self):
        for source in (ISLAND, ARENA, DOMKI, FINISH, ODBIOR):
            self.assertTrue(delimiters_balanced(source))
        for name in RUNTIME_FILES:
            with self.subTest(file=name):
                self.assertTrue(delimiters_balanced((RUNTIME / name).read_text(encoding="utf8")))


if __name__ == "__main__":
    unittest.main()
