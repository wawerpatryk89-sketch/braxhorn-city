"""New Patryk in Miasto Mistrzów (owner 23.11 15:47/15:54: the old one "do wyrzucenia", "dać nowego").

The global library's Patryk of 13.11 (the one Osiedle plays): fetched in CI from the owner's private draft with every
SHA-256 checked, imported like on Osiedle (model Humanoid, his 31 motions Generic straight onto his bones)."""
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import restore_patryk_globalny as R  # noqa: E402

ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
GLOB = (ED / "BraxChampionsPatrykGlobalny.cs").read_text(encoding="utf-8")
PAT = (ED / "BraxChampionsPatryk.cs").read_text(encoding="utf-8")
IMP = (ED / "BraxChampionsPatrykImport.cs").read_text(encoding="utf-8")
WF = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")


class Paczka(unittest.TestCase):
    def test_the_osiedle_package(self):
        self.assertEqual(R.PACZKA["release_id"], 443180649)
        self.assertEqual(R.PACZKA["tag"], "postacie-globalne-20261112")
        self.assertEqual(R.PACZKA["name"], "Patryk_globalny_13-11_Unity.zip")
        self.assertEqual(R.PACZKA["sha256"], "30ff44d34c5c45b1a8876e1266396b57faca6537e6d6f73600f152f6cea06887")
        self.assertEqual(R.MANIFEST_SHA, "22350070cc2d0537cbc94937d68c8b41eb1480e3a457aad639cc5ac407a275c7")
        self.assertEqual(R.CEL, ROOT / "unity-city/Assets/Source/Mistrzowie/Player/Postacie/patryk")
        self.assertEqual(len(R.RUCHY), 31)

    def test_the_folder_stays_out_of_git(self):
        gi = (ROOT / ".gitignore").read_text(encoding="utf-8")
        self.assertIn("unity-city/Assets/Source/Mistrzowie/Player/\n", gi)

    def test_ci_step_is_fatal_and_has_a_token(self):
        i = WF.index("- name: Geometry and source preparation")
        krok = WF[i:WF.index("- name:", i + 10)]
        self.assertIn("GH_TOKEN: ${{ github.token }}", krok)
        linia = [l for l in krok.splitlines() if "restore_patryk_globalny.py" in l and not l.strip().startswith("#")]
        self.assertEqual(len(linia), 1)
        self.assertNotIn("||", linia[0])


class Unity(unittest.TestCase):
    def test_same_motions_as_the_package(self):
        nazwy = re.findall(r'PK\("(\w+)", (?:true|false), true, Ruchy \+ "(\w+)\.fbx"\)', GLOB)
        self.assertEqual([n for n, _ in nazwy], R.RUCHY)
        self.assertTrue(all(n == f for n, f in nazwy))
        self.assertIn('const string GlobalnyModelSha = "' + R.MODEL_SHA + '";', GLOB)
        self.assertIn('public const string GlobalnyKatalog = PrywatneZrodla + "/Postacie/patryk";', GLOB)
        self.assertIn('public const string PrywatneZrodla = "Assets/Source/Mistrzowie/Player";', PAT)

    def test_game_clips(self):
        for n in ("Idle", "Walk", "Run", "Jump", "Punch", "Kick", "CombatIdle", "EnteringCar", "CarIdle", "Driving", "ExitingCar"):
            self.assertIn('"' + n + '"', GLOB.split("GlobalnyWymagane")[1].split(";")[0])
        self.assertIn('jazda.name = "Driving";', GLOB)
        self.assertIn("PatrykBezposrednio(hero, hips, kosci, spoczynek, sciezka, k, out string uwagi)", GLOB)
        self.assertIn("if (clip.humanMotion) throw", GLOB)

    def test_bohater_takes_the_new_one(self):
        self.assertIn("if (GlobalnyJest) hero = PatrykZbudujGlobalny(gracz, cialo);", PAT)
        self.assertIn("DO POPRAWKI MISTRZOWIE_PATRYK stary model", PAT)

    def test_import_rules(self):
        self.assertIn("public override uint GetVersion() { return 7; }", IMP)
        r = IMP.index('assetPath.StartsWith(Katalog + "Postacie/patryk/Ruchy/")')
        blok = IMP[r:IMP.index("}", r)]
        self.assertIn("ModelImporterAnimationType.Generic", blok)
        self.assertIn("ModelImporterAvatarSetup.NoAvatar", blok)
        self.assertIn('assetPath == Katalog + "Postacie/patryk/Patryk.fbx"', IMP)
        self.assertIn("TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4", IMP)


if __name__ == "__main__":
    unittest.main()
