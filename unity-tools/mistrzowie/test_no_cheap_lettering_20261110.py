"""Owner 10.11 10:17-10:23: "kompletnie zakazuję takich tanich napisów ... Globalnie, zakaz tanich napisów";
"białe dziwne napisy od chuja ... po czacie to ma wszystko zniknąć" (docs/zasady/NAPISY_PREMIUM_ZAKAZ_TANICH_20261110.md).
No TextMesh / bitmap-font text anywhere in Miasto Mistrzów; every text = solid 3D letters in the licensed premium typeface
on an architectural carrier."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
EDITOR = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RUNTIME = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


class NoCheapLettering(unittest.TestCase):
    def test_no_textmesh_or_bitmap_font_in_the_region(self):
        banned = ("AddComponent<TextMesh>", "GetComponent<TextMesh>", "TextMeshPro", "LegacyRuntime.ttf",
                  "W.Napis(", "BraxWnetrzaKit.Napis(", "BraxNabrzeze.Napis(", "BraxRegionMeble.Napis(")
        for folder in (EDITOR, RUNTIME):
            for f in folder.glob("*.cs"):
                src = f.read_text(encoding="utf-8")
                for b in banned:
                    self.assertNotIn(b, src, f"{f.name}: {b}")

    def test_premium_typeface_licensed_and_not_a_system_font(self):
        fonts = TOOLS / "fonts"
        for n in ("Montserrat-SemiBold-latin.ttf", "Montserrat-SemiBold-latin-ext.ttf", "OFL.txt"):
            self.assertTrue((fonts / n).is_file(), n)
        self.assertIn("SIL Open Font License", (fonts / "OFL.txt").read_text(encoding="utf-8"))
        for script in ("build_harbour_signs.py", "build_letter_kit.py"):
            src = (TOOLS / script).read_text(encoding="utf-8")
            self.assertNotIn("DejaVuSans-Bold.ttf'", src)
            self.assertNotIn("/usr/share/fonts", src)
        kit = (TOOLS / "build_letter_kit.py").read_text(encoding="utf-8")
        for c in "ĄĆĘŁŃÓŚŹŻ0123456789":
            self.assertIn(c, kit)

    def test_ci_generates_the_kit(self):
        wf = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
        self.assertIn("python3 unity-tools/mistrzowie/build_letter_kit.py", wf)
        self.assertIn("python3 unity-tools/mistrzowie/build_harbour_signs.py", wf)

    def test_marina_gates_are_architecture_not_a_frame_with_letters_on_top(self):
        h = (EDITOR / "BraxChampionsHarbour.cs").read_text(encoding="utf-8")
        gates = h[h.index("static void Gates(Transform harbour"):h.index("public static int PlaceOriginalFleet(")]
        self.assertIn("GateStone", gates)                       # marble pylons + lintel
        self.assertIn("sign band", gates)                       # graphite band the letters are mounted on
        self.assertIn("Letters(signs,id,new Vector3(cx,yb,zf+.017f),0f,scale);", gates)
        self.assertIn("Letters(signs,id,new Vector3(cx,yb,zb-.017f),180f,scale);", gates)
        self.assertNotIn("QuayY+top+.06f", gates)               # the old letters standing on the top bar
        s = (TOOLS / "build_harbour_signs.py").read_text(encoding="utf-8")
        self.assertIn("'GATE_MARINA': ('BRAXHORN', 'MARINA')", s)
        safety = (EDITOR / "BraxChampionsHarbourSafety.cs").read_text(encoding="utf-8")
        self.assertIn("o.x0-BraxChampionsHarbour.GatePylon", safety)

    def test_composed_texts_use_the_kit(self):
        for name, needle in (("BraxChampionsInfrastructure.cs", "BraxChampionsLetters3D.Text(letters"),
                             ("BraxChampionsArenaAthletes.cs", '"WEJŚCIE ZAWODNIKÓW"'),
                             ("BraxChampionsArenaAthletes.cs", '"SZATNIA " + room'),
                             ("BraxChampionsBeltShowcase.cs", '"CHAMPION\'S BELT"'),
                             ("BraxChampionsApartments.cs", "BraxChampionsLetters3D.Text(doorNumbers"),
                             ("BraxChampionsHotelSobczakT03.cs", '"ROOM NUMBER "+number')):
            src = (EDITOR / name).read_text(encoding="utf-8")
            self.assertIn(needle, src, name)
            self.assertIn("BraxChampionsLetters3D", src, name)
        infra = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")
        self.assertIn("localbox('sign',x,z,axis,0,base+2.40,v-.86,5.0,.26,.05)", infra)   # stop names on a fascia


if __name__ == "__main__":
    unittest.main()
