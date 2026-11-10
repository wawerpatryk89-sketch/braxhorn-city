"""Owner 10.11 08:40: auto 200 km/h, hamulec ręczny, gaz regulowany wychyleniem joysticka. Read-only gates."""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
CAR = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCar.cs"
CMD = ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCarCommands.cs"
BUILDER = ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs"
TOUCH = ROOT / "unity-city/Assets/Claude/BraxTouchControls.cs"


class CarHandbrakeThrottleTests(unittest.TestCase):
    def setUp(self):
        self.car = CAR.read_text(encoding="utf-8")
        self.builder = BUILDER.read_text(encoding="utf-8")

    def test_top_speed_stays_200(self):
        self.assertIn("public float maxSpeed = 200f / 3.6f", self.car)
        self.assertIn("car.maxSpeed = 200f / 3.6f * K;", self.builder)

    def test_throttle_follows_stick_deflection_with_curve(self):
        self.assertIn("float deflection = control.magnitude;", self.car)
        self.assertIn("Mathf.Pow(Mathf.Clamp01((deflection - .08f) / .92f), throttleCurve)", self.car)
        curve = float(re.search(r"throttleCurve = ([\d.]+)f", self.car).group(1))
        kph = lambda d: ((d - .08) / .92) ** curve * 200
        self.assertLess(kph(.2), 15)          # lekko = wolno
        self.assertTrue(40 < kph(.5) < 80)    # pół = miasto
        self.assertAlmostEqual(kph(1.0), 200)  # na maksa = 200

    def test_easing_the_stick_coasts_and_pulling_back_brakes(self):
        self.assertIn(": Mathf.Abs(target) < Mathf.Abs(speed) ? coastDecel : acceleration * launch;", self.car)
        self.assertIn(": changingDirection ? braking", self.car)
        self.assertIn("car.coastDecel = 4.5f * K;", self.builder)

    def test_handbrake_brakes_and_drifts(self):
        self.assertIn("public void PullHandbrake()", self.car)
        self.assertIn("float rate = handbrake ? braking * handbrakeBrakeFactor", self.car)
        self.assertIn("(sliding ? driftSteerBoost : 1f)", self.car)
        self.assertIn("Vector3 delta = travelDir * speed * dt;", self.car)
        self.assertIn("(sliding ? driftGrip : normalGrip)", self.car)
        self.assertIn("Input.GetKey(KeyCode.Space)", self.car)

    def test_jump_button_is_the_handbrake_without_touching_r1_controls(self):
        cmd = CMD.read_text(encoding="utf-8")
        self.assertIn('if (key == "jump") player.championsCar.PullHandbrake();', cmd)
        self.assertIn("p.gameObject.AddComponent<BraxChampionsCarCommands>()", self.car)
        self.assertTrue((ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsCarCommands.cs.meta").exists())
        self.assertIn('SendMessage("Command", key', TOUCH.read_text(encoding="utf-8"))

    def test_zero_to_200_is_quick(self):
        acc = float(re.search(r"car\.acceleration = ([\d.]+)f \* K", self.builder).group(1))
        top = float(re.search(r"topEndAccelerationFactor = ([\d.]+)f", self.car).group(1))
        v = 200 / 3.6
        t = v / ((1 - top) * acc) * -math.log(top)
        self.assertLess(t, 11.0)


if __name__ == "__main__":
    unittest.main()
