"""MIASTO R1: crossroad fence, street lamps and traffic-light pole regressions.

Static checks only. Real results require BraxGymBuild.SceneOnlyCityR1 in Unity 6.
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
LAMPS=(EDITOR/"Claude/BraxR1StreetLightingAudit.cs").read_text(encoding="utf8")
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
        for name,content in (("street",B),("traffic",CITY),("lighting",LAMPS),("build",BUILD)):
            with self.subTest(name=name):
                self.assertTrue(paired(content))

    def test_first_roundabout_location_is_grounded(self):
        self.assertIn("public const float RX = -126f, RZ = 28.5f",B)
        self.assertIn("var exact = Bx(-106.25f, -102.95f",B)
        self.assertIn("GZ - GH, GZ + GH",B)
        self.assertGreater(-104.5-(-126),10)   # fence is east of the FIRST main roundabout

    def test_old_whole_renderer_condition_is_replaced(self):
        self.assertIn("UsunPlotPrzedPierwszymRondem(korzenie)",B)
        self.assertNotIn("plot.Contains(b.min) && plot.Contains(b.max)",B)
        self.assertIn("bool Poprzeczna(Bounds b)",B)
        self.assertIn('n.Contains("chain link")',B)
        self.assertIn('n.Contains("szlaban")',B)
        self.assertIn('n.Contains("barrier")',B)

    def test_fence_never_clears_whole_street_or_building(self):
        self.assertIn("q.size.y > 6.0f || q.size.x > 21.0f",B)
        self.assertIn("q.size.z > 24.0f",B)
        self.assertIn("if (!named && !shape) continue",B)
        self.assertIn("if (!q.Intersects(road)) continue;",B)
        self.assertIn("Chroniony(r.transform)",B)
        self.assertNotIn("DestroyImmediate(g);",B[B.index("static void UsunPlotPrzedPierwszymRondem("):
                                             B.index("static string Sciezka(")])

    def test_fence_mesh_and_physics_colliders_both_cleaned(self):
        self.assertIn("UnityEngine.Object.DestroyImmediate(r.gameObject)",B)
        self.assertIn("foreach(var c in g.GetComponentsInChildren<Collider>(true).ToArray())",B)
        self.assertIn("c.enabled=false;",B)
        self.assertIn("BRAXHORN_GLOWNA_PLOT DO POPRAWKI",B)
        self.assertIn("kolizji=",B)

    def test_original_city_mesh_names_are_verified_from_source_manifest(self):
        self.assertIn('Assets/Source/City/city-manifest.json', B)
        self.assertIn('JsonUtility.FromJson<BraxCityBuild.Manifest>', B)
        self.assertIn('ObcaPrzeszkoda(originalName)', B)
        self.assertIn('sourceLabels[mesh.name]=mesh.originalName', LAMPS)
        self.assertIn('HasWord(originalName,"latarnia","lampa uliczna","light pole","lightpole")', LAMPS)
        self.assertIn('IsCandidate(tr,originalName,out bool street', LAMPS)

    def test_lamp_arm_is_aimed_toward_actual_road(self):
        self.assertIn("NaJezdni(probeA)",B)
        self.assertIn("NaJezdni(probeB)",B)
        self.assertIn("if (!frontRoad && backRoad)",B)
        self.assertIn("dir = -dir;",B)
        self.assertIn("DO WERYFIKACJI: nie znaleziono jezdni",B)

    def test_all_city_lamps_are_discovered_and_scanned(self):
        self.assertIn("foreach(var root in roots)",LAMPS)
        self.assertIn("root.GetComponentsInChildren<Transform>(true)",LAMPS)
        for s in ('n=="Latarnia uliczna"','n=="Latarnia parkowa"',
                  'names.IndexOf("streetlight"','names.IndexOf("lamp post"'):
            self.assertIn(s,LAMPS)
        self.assertIn("lampsFound=lamps.Count",LAMPS)

    def test_duplicates_and_bad_side_lamps_removed_only_when_grounded(self):
        self.assertIn("Near(q.foot,lamp.foot,3.6f)",LAMPS)
        self.assertIn("nearSignal=trafficPoles.Any",LAMPS)
        self.assertIn("bool inRoad=BraxRegionMiastoB.NaJezdni(lamp.foot)",LAMPS)
        self.assertIn("UnityEngine.Object.DestroyImmediate(lamp.t.gameObject)",LAMPS)
        self.assertIn("if(lamp.t.GetComponentsInChildren<BraxSwiatloDrogowe>(true).Length>0)",LAMPS)
        self.assertIn("BRAXHORN_R1_PLOT_LATARNIE",LAMPS)

    def test_live_traffic_lights_and_other_regions_are_preserved(self):
        self.assertIn("GetComponentsInChildren<BraxSwiatloDrogowe>(true)",LAMPS)
        self.assertIn('n.StartsWith("claude wyspa")',LAMPS)
        self.assertIn('n.Contains("osiedle")',LAMPS)
        self.assertIn("old.stopSrodek.Length==light.stopSrodek.Length",LAMPS)
        self.assertIn("Vector3.Distance(a,b)<.1f",LAMPS)
        self.assertNotIn("DestroyImmediate(old.gameObject)",LAMPS)

    def test_scene_reports_real_remaining_fence_collisions(self):
        self.assertIn("Physics.OverlapCapsule(",LAMPS)
        self.assertIn("FenceBlockers(scene,notes)",LAMPS)
        self.assertIn("r1-plot-latarnie.json",LAMPS)
        self.assertIn("blockersAcrossMainRoad=barriers",LAMPS)
        self.assertIn("NIEGOTOWE",LAMPS)

    def test_audit_runs_after_generation_before_quality_and_scene_save(self):
        self.assertIn("BraxR1StreetLightingAudit.Run(scene)",BUILD)
        self.assertLess(BUILD.index("BraxSupermarket.Wykonaj(scene,2)"),
                        BUILD.index("BraxR1StreetLightingAudit.Run(scene)"))
        self.assertLess(BUILD.index("BraxR1StreetLightingAudit.Run(scene)"),
                        BUILD.index('BraxKontrola.Wszystko(scene,"miasto")'))
        self.assertLess(BUILD.index("BraxR1StreetLightingAudit.Run(scene)"),
                        BUILD.index('EditorSceneManager.SaveScene(scene,scenePath)'))

    def test_scene_only_workflow_still_does_not_trigger_apk(self):
        self.assertIn("public static void SceneOnlyCityR1()",BUILD)
        self.assertIn("if(sceneOnly)\n        {",BUILD)
        self.assertIn("bez_APK=true bez_WebGL=true",BUILD)


if __name__=="__main__":
    unittest.main()
