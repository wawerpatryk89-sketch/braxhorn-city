"""Metro M1 three stations: static/source and geometric regressions, NOT Unity playtest.
From unity-tools/claude: python -m unittest test_metro_3_stations -v
"""
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[2]
ASSET=ROOT/"unity-city/Assets"
BUILDER=(ASSET/"Editor/Claude/BraxMetroLine3Build.cs").read_text(encoding="utf8")
RIDE=(ASSET/"Claude/Runtime/BraxMetro3StopRide.cs").read_text(encoding="utf8")
CITY=(ASSET/"Editor/CityRebuild/BraxCityRebuild.cs").read_text(encoding="utf8")
SYSTEM=(ASSET/"Editor/CityRebuild/Systems/BraxCitySystemsBuild.cs").read_text(encoding="utf8")
VALID=(ASSET/"Editor/CityRebuild/Systems/BraxCitySystemsValidation.cs").read_text(encoding="utf8")
BOOT=(ASSET/"Editor/BraxGymBuild.cs").read_text(encoding="utf8")
WORLD=(ASSET/"City/BraxCityWorld.cs").read_text(encoding="utf8")
PLAYER=(ASSET/"BraxGymPlayer.cs").read_text(encoding="utf8")
SECTORS=(ASSET/"Editor/Claude/BraxClaudeObszary.cs").read_text(encoding="utf8")

def braces(src):
    src=re.sub(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"', '', src)
    stack=[]
    for c in src:
        if c in '([{':stack.append(c)
        elif c in ')]}':
            if not stack or stack.pop()!=dict(zip(')]}','([{'))[c]:return False
    return not stack

class MetroM1ThreeStations(unittest.TestCase):
    def test_exactly_three_real_city_station_anchors(self):
        self.assertIn("Stops = { 72f, -76f, -218f }", BUILDER)
        self.assertIn('Names = { "NABRZEZE", "SRODMIESCIE", "ZACHOD" }', BUILDER)
        self.assertIn("for (int i = 0; i < Stops.Length; i++) Station(i);", BUILDER)
        x=[72,-76,-218]
        self.assertEqual(len(x),len(set(x)))
        self.assertTrue(all(abs(a-b)>100 for a,b in zip(x,x[1:])))
    def test_only_new_builder_is_called_from_city(self):
        self.assertIn("BraxMetroLine3Build.Apply(root);", CITY)
        self.assertNotIn("BraxClaudeMetro.Apply(root);", CITY)
        self.assertNotIn("BuildMetro(root,stone,concrete,metal);", SYSTEM)
        for label in ("G5_access_corridor_void", "G5_station_platform_and_tracks_void"):
            self.assertNotIn(label, CITY)
        self.assertNotIn("Metro stair "+'"', VALID)
    def test_build_order_after_all_streets(self):
        self.assertLess(CITY.index("BraxRegion.Apply(world,root);"), CITY.index("BraxMetroLine3Build.Apply(root);"))
    def test_real_train_asset_is_required_not_faked(self):
        self.assertIn('metro_train.glb', BUILDER)
        self.assertIn("BraxMetro3StopRide", BUILDER)
        self.assertIn("bez sztucznej atrapy", BUILDER)
        self.assertNotIn("PociagZKodu(", BUILDER)
    def test_two_continuous_tracks_and_tunnel(self):
        self.assertIn("RouteMinX = -251f, RouteMaxX = 112f", BUILDER)
        self.assertIn("TrackNorth = 32.3f, TrackSouth = 23.5f", BUILDER)
        self.assertIn("Mathf.Min(x + 12f, RouteMaxX)", BUILDER)
        self.assertIn("foreach (float tz in new[] { TrackSouth, TrackNorth })", BUILDER)
        self.assertIn("W.MM(rails, x, to", BUILDER)
        self.assertAlmostEqual(112-(-251),363)
    def test_terminal_has_track_ahead_of_last_platform(self):
        self.assertGreaterEqual(112-(72+24), 12)
        self.assertGreaterEqual((-218-24)-(-251), 8)

    def test_three_shaft_cuts_and_connected_foot_paths(self):
        self.assertIn('"M1_3_wejscie_" + i', BUILDER)
        self.assertIn('"M1_3_hala_" + i', BUILDER)
        self.assertIn('"M1_3_tunel_caly"', BUILDER)
        self.assertIn("26.88f, 44.35f, true", BUILDER)
        self.assertIn("41.20f, 44.80f, true", BUILDER)
        self.assertIn("Stair(x + 6f, x - 6f, StreetY, ConcourseY", BUILDER)
        self.assertIn("Stair(x - 6f, x + 11f, ConcourseY, PlatformY", BUILDER)
        self.assertIn("W.Rampa(", BUILDER)
        self.assertIn("podstopnicy", BUILDER)
    def test_mezzanine_has_no_duplicate_landing_or_ceiling_across_stairwell(self):
        # The street stair runs x+6 -> x-6 at z=43. Corridor starts exactly
        # at x-6 and turns at z=28 to the platform stair; no duplicate slab.
        self.assertIn("x - 8.5f, x - 6.0f, ConcourseY - .25f", BUILDER)
        self.assertIn("26.88f, 44.35f, true", BUILDER)
        self.assertNotIn("W.MM(tiles, x - 8.4f, x - 6f", BUILDER)
        self.assertIn("41.20f, 44.80f, true", BUILDER)
        self.assertIn("x + 7.2f, x + 11f, -1.95f", BUILDER)

    def test_source_pits_cut_after_station_meshes_are_authored(self):
        self.assertLess(
            BUILDER.index('meshes = W.Zapisz("M1_3_stacje_tory_i_tunele")'),
            BUILDER.index('opened += W.Otwor(cityRoot'))
        )
        self.assertLess(
            BUILDER.index("AddTrains();"),
            BUILDER.index('opened += W.Otwor(cityRoot')
        )

    def test_moving_train_cannot_be_split_from_its_mesh(self):
        self.assertIn("typeof(BraxMetro3StopRide)", SECTORS)
        self.assertIn("if (t.GetComponent<BraxMetro3StopRide>()) return;", SECTORS)

    def test_player_rides_train_without_character_controller(self):
        self.assertIn("public BraxMetro3StopRide metroRide;", PLAYER)
        self.assertIn("if(metroRide){speed=0f;vertical=0f;return;}", PLAYER)
        self.assertIn("public sealed class BraxMetro3StopRide", RIDE)
        self.assertIn("IBraxInterakcja", RIDE)
        self.assertIn("if(player.metroRide)return player.metroRide.PassengerContext;", WORLD)
        self.assertIn("if(player.metroRide){player.metroRide.TryAlight();return true;}", WORLD)
        self.assertIn("typeof(BraxMetro3StopRide)", SECTORS)
        self.assertIn("WYSIADZ", RIDE)
        self.assertIn("WSIADZ", RIDE)
        self.assertIn("Mathf.Sqrt(", RIDE) # braking
    def test_underground_culling_keeps_street_entrances_visible(self):
        self.assertIn("B(63f, 82f, -1.0f, 6f, 39f, 48f)", BUILDER)
        self.assertIn("B(-85f, -66f, -1.0f, 6f, 39f, 48f)", BUILDER)
        self.assertIn("B(-227f, -208f, -1.0f, 6f, 39f, 48f)", BUILDER)
    def test_quality_and_mobile_constraints(self):
        self.assertIn("BraxRegion.Pbr(", BUILDER)
        self.assertIn("KontrolaPrzejscia()", BUILDER)
        self.assertIn("BraxMetroLine3Build.KontrolaPrzejscia();", BOOT)
        self.assertIn("Physics.OverlapCapsule(", BUILDER)
        self.assertIn("NIEGOTOWE", BUILDER)
    def test_syntax_delimiters_balanced(self):
        for name,src in (("builder", BUILDER),("ride",RIDE),("city",CITY),
                         ("systems",SYSTEM),("validator",VALID),("player",PLAYER),
                         ("world",WORLD),("areas",SECTORS),("boot",BOOT)):
            self.assertTrue(braces(src),name)
if __name__=="__main__":
    unittest.main()
