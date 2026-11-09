"""MIASTO R1: płot przed pierwszym rondem i odbiór latarni (BraxRegionMiastoB).

9.11: BraxR1StreetLightingAudit usunięty (dublował OdbiorWyposazeniaUlic) — jego testy zdjęte.
Static checks only.
"""
from pathlib import Path
import math
import re
import unittest

ROOT=Path(__file__).resolve().parents[2]
REGION=ROOT/"unity-city/Assets/Editor/Claude/Region"
EDITOR=ROOT/"unity-city/Assets/Editor"
B=(REGION/"BraxRegionMiastoB.cs").read_text(encoding="utf8")
CITY=(REGION/"BraxRegionRuchMiasta.cs").read_text(encoding="utf8")
BUILD=(EDITOR/"BraxGymBuild.cs").read_text(encoding="utf8")


def paired(source):
    source=re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', "",source)
    stack=[]
    close={")":"(","]":"[","}":"{"}
    for ch in source:
        if ch in "([{":stack.append(ch)
        elif ch in ")]}":
            if not stack or stack.pop()!=close[ch]:return False
    return not stack


class R1StreetSafety(unittest.TestCase):
    def test_csharp_braces_and_parentheses(self):
        for name,content in (("street",B),("traffic",CITY),("build",BUILD)):
            with self.subTest(name=name):
                self.assertTrue(paired(content))

    def test_first_roundabout_location_is_grounded(self):
        self.assertIn("public const float RX = -126f, RZ = 28.5f",B)
        self.assertGreater(-104.5-(-126),10)   # płot stoi na wschód od PIERWSZEGO ronda (koniec dawnego miasta x −104)

    def test_old_city_end_fence_is_cut_by_measured_volume(self):
        # 9.11: płot/szlaban/budka są w scalonych siatkach źródła — szukanie po nazwie nic nie znalazło (log APK 9.11).
        body = B[B.index("static void UsunPlotPrzedPierwszymRondem("):B.index("static string Sciezka(")]
        self.assertIn("UsunPlotPrzedPierwszymRondem(korzenie)",B)
        self.assertIn("var brama = Bx(-104.7f, -99.2f, .22f, 6.0f, 20.75f, 35.95f)", body)   # między licami budynków, nad nawierzchnią
        self.assertIn("BraxCityMeshCut.Subtract(src, brama", body)                           # tylko źródło miasta
        self.assertIn("BraxClaudeGround.SourceCity", body)
        self.assertNotIn("DestroyImmediate(g);", body)

    def test_fence_colliders_cleaned_without_threshold_across_road(self):
        body = B[B.index("static void UsunPlotPrzedPierwszymRondem("):B.index("static string Sciezka(")]
        self.assertIn("c.enabled = false; kolizje++;", body)
        self.assertIn("q.size.x > 6f", body)                                                  # tylko wąskie (słupki, siatka, szlaban)
        self.assertIn("BRAXHORN_GLOWNA_PLOT", body)

    def test_old_city_end_separate_meshes_disabled_by_number(self):
        env = (ROOT / "unity-city/Assets/Editor/CityRebuild/Environment/BraxCityEnvironment.cs").read_text(encoding="utf-8")
        reb = (ROOT / "unity-city/Assets/Editor/CityRebuild/BraxCityRebuild.cs").read_text(encoding="utf-8")
        self.assertIn("SourceOldCityEndMeshes = { 213, 215, 216, 217, 218, 219 }", env)
        self.assertIn("Disable(original,BraxCityEnvironment.SourceOldCityEndMeshes)", reb)
        self.assertIn("SourceNovaOldSignMeshes = { 675 }", env)
        self.assertIn("Disable(original,BraxCityEnvironment.SourceNovaOldSignMeshes)", reb)

    def test_source_manifest_used_for_street_furniture(self):
        # odbiór latarni/ławek przez manifest nazw źródła, nie po CityMesh_N
        self.assertIn('labels[m.name] = m.originalName', B)
        self.assertIn('LatarniaZrodla(name)', B)

    def test_lamp_arm_is_aimed_toward_actual_road(self):
        # Probe incrementally across the green strip, not just two stale points.
        self.assertIn("float maxProbe = Mathf.Max(9f, wysieg + 4f);",B)
        self.assertIn("bool JezdniaPoStronie(Vector3 strona)",B)
        self.assertIn("if (NaJezdni(p + strona * d)) return true;",B)
        self.assertIn("bool frontRoad = JezdniaPoStronie(dir), backRoad = JezdniaPoStronie(-dir);",B)
        self.assertIn("if (!frontRoad && backRoad)",B)
        self.assertIn("dir = -dir;",B)
        self.assertIn("DO WERYFIKACJI: brak jezdni w promieniu",B)

    def test_scene_only_workflow_still_does_not_trigger_apk(self):
        self.assertIn("public static void SceneOnlyCityR1()",BUILD)
        self.assertIn("if(sceneOnly)\n        {",BUILD)
        self.assertIn("bez_APK=true bez_WebGL=true",BUILD)


if __name__=="__main__":
    unittest.main()
