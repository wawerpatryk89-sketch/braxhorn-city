"""Owner 10.11 12:41 (APK 37): "nie ma żadnego hamulca ręcznego, żaden się przycisk nie pokazuje" and
"gwałtownie skręca nagle". Read-only gates for the A1 car HUD button and the smoothed, grip-limited steering."""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CAR = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCar.cs"
HUD = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCarHud.cs"
CMD = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCarCommands.cs"
TOUCH = ROOT / "unity-city/Assets/Claude/BraxTouchControls.cs"
K = 2.33 / 1.80


def field(src, name):
    return float(re.search(name + r" = ([\d.]+)f", src).group(1))


class CarHudSteeringTests(unittest.TestCase):
    def setUp(self):
        self.car = CAR.read_text(encoding="utf-8")
        self.hud = HUD.read_text(encoding="utf-8")

    def test_city_controls_hide_fight_buttons_in_the_car(self):
        # the reason for the region's own button: R1 routes and draws PUNCH/KICK/JUMP only on foot
        touch = TOUCH.read_text(encoding="utf-8")
        self.assertIn("bool OnFoot => player && !player.vessel && !player.seat && !player.championsCar;", touch)

    def test_handbrake_button_is_added_when_boarding(self):
        self.assertIn("gameObject.AddComponent<BraxChampionsCarHud>().car = this;", self.car)
        self.assertIn("car.HoldHandbrake(.15f)", self.hud)
        self.assertIn("public void HoldHandbrake(float seconds)", self.car)
        self.assertIn("BraxGui.PrzyciskKolowy(", self.hud)
        self.assertTrue((HUD.parent / "BraxChampionsCarHud.cs.meta").exists())

    def test_button_is_a_pictogram_not_text(self):
        for cheap in ("GUI.Label", "BraxGui.Napis", "TextMesh", "GUIStyle"):
            self.assertNotIn(cheap, self.hud)
        self.assertIn("static float Sdf(Vector2 p)", self.hud)

    def test_hud_hides_with_the_menu(self):
        self.assertIn('if (key == "pause") { Paused = !Paused; return; }', CMD.read_text(encoding="utf-8"))
        self.assertIn("!BraxChampionsCarCommands.Paused", self.hud)

    def test_wheel_is_rate_limited_and_curved(self):
        self.assertIn("steerInput = Mathf.MoveTowards(steerInput, shaped, (easing ? steerReturn : steerRate) * dt);", self.car)
        self.assertIn("float yaw = steerInput * maxSteer", self.car)
        rate = field(self.car, "steerRate")
        self.assertTrue(1.8 <= rate <= 3.0, rate)                       # full lock in .33-.55 s, not one frame
        curve, dead = field(self.car, "steerCurve"), field(self.car, "steerDeadZone")
        light = ((.3 - dead) / (1 - dead)) ** curve
        self.assertLess(light, .2)                                      # a light push = a light correction

    def test_yaw_rate_never_exceeds_tyre_grip(self):
        self.assertIn("turn = Mathf.Clamp(turn, -maxTurn, maxTurn);", self.car)
        grip = field(self.car, "lateralGrip")
        self.assertTrue(7.0 <= grip <= 10.5, grip)                      # 0.7-1.07 g, a sports car on road tyres
        for kph, max_deg in ((50, 45), (100, 21), (200, 11)):
            v = kph / 3.6 * K
            yaw_rate = math.degrees(grip * K / v)
            self.assertLessEqual(yaw_rate, max_deg, (kph, yaw_rate))
        # APK 37 (no limit): half push at 100 km/h was ~140 deg/s
        wheelbase, max_steer = 2.7 * K, 34.0
        v = 100 / 3.6 * K
        old = math.degrees(v / wheelbase * math.tan(math.radians(.5 * max_steer * (1 - (.84 * .25)))))
        self.assertGreater(old, 100)


if __name__ == "__main__":
    unittest.main()
