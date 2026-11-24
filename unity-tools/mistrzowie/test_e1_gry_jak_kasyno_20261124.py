"""E1 Centrum Rozrywki: billiards and bowling played the casino's way (owner 24.11 01:32: "Centrum rozrywki, żeby było można
grać tak samo. Gdzie bilard i kręgle").

AKCJA at a table / a lane opens the game (BraxChampionsStanowisko, as the roulette): the camera takes over, the player
leaves the picture, the walking controls rest, the game's buttons at the bottom, WSTAŃ walks on.
  - billiards: over the table; a finger on the cloth aims, ◀ ▶ fine; a guide line to the first ball / cushion and the
    struck ball's line; hold UDERZ (the power rises and falls, the cue draws back), let go = the shot; NOWA PARTIA;
  - bowling: low behind the ball; a finger near the ball moves it on the foul line, down the lane aims (at most maxKat);
    PODKRĘCENIE (a hook in the back third); hold RZUĆ, let go = the roll; the camera rides behind the ball, watches the
    pinsetter, comes back; the scorecard; NOWA GRA.
The physics, the pinsetter and the scoring are the 10.11 ones (b7dccdec)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
RT = ROOT / "unity-city/Assets/Mistrzowie"
BIL = (RT / "BraxChampionsBilard.cs").read_text(encoding="utf-8")
KRE = (RT / "BraxChampionsKregle.cs").read_text(encoding="utf-8")
ST = (RT / "BraxChampionsStanowisko.cs").read_text(encoding="utf-8")


class JakKasyno(unittest.TestCase):
    def test_both_are_seat_games(self):
        self.assertIn("public sealed class BraxChampionsBilard : MonoBehaviour, IBraxInterakcja, IBraxStanowisko", BIL)
        self.assertIn("public sealed class BraxChampionsKregle : MonoBehaviour, IBraxInterakcja, IBraxStanowisko", KRE)
        for src in (BIL, KRE):
            for m in ("public string Tytul(int ", "public void Kadr(int ", "public void Panel(int ", "public void Wstal(int "):
                self.assertIn(m, src)
            self.assertIn("BraxChampionsStanowisko.Wejdz(this, ", src)
            self.assertIn("if (e.type == EventType.MouseDown && silaR.Contains(e.mousePosition) && moze && BraxChampionsStanowisko.OdWejscia > .45f)", src)
            self.assertIn("if (e.type == EventType.MouseUp && trzymaSila)", src)
        # the seat framework the casino uses: the walking controls rest, the player out of the picture
        self.assertIn("if (tc && tc.enabled) tc.enabled = false; else tc = null;", ST)
        self.assertIn("ukryte[k].shadowCastingMode = ShadowCastingMode.ShadowsOnly;", ST)

    def test_offered_by_akcja(self):
        self.assertIn('return "GRAJ · BILARD · STÓŁ " + (t + 1);', BIL)
        self.assertIn('return "GRAJ · KRĘGLE · TOR " + (lane + 1) + (lane != tor || Koniec(lane) ? " · NOWA GRA" : "");', KRE)
        # the old walking play is gone: no shot with a punch, no ball at the hip
        self.assertNotIn("BraxGymPlayer.OnCios += Cios", BIL)
        self.assertNotIn("BILARD · UDERZ (CIOS = MOCNO)", BIL)
        self.assertNotIn("var at = gracz.transform.position + r * .40f + f * .12f + Vector3.up * .98f;", KRE)
        self.assertNotIn("KRĘGLE · WEŹ KULĘ", KRE)

    def test_billiards_aim_guide_and_power(self):
        self.assertIn("BraxChampionsStanowisko.Kadruj(cam, c, n, Vector3.right, 2f * polZ + .55f, 2f * polX + .45f, out oko, out obrot);", BIL)
        self.assertIn("var pl = new Plane(Vector3.up, Biala(t).position);", BIL)
        self.assertIn("if (Physics.SphereCast(c0, r * .98f, kierunek, out var hit, daleko, ~0, QueryTriggerInteraction.Ignore))", BIL)
        self.assertIn("Uderz(t, kierunek, Mathf.Lerp(.5f, silaMocna * 1.15f, Mathf.Pow(Mathf.Max(moc, .04f), 1.4f)));", BIL)
        self.assertIn("if (trzymaSila) moc = Mathf.PingPong((Time.unscaledTime - silaOd) / 1.3f, 1f);", BIL)
        self.assertIn('"NOWA PARTIA"', BIL)
        # the panel's status never under the casino's top bar
        self.assertNotIn("BraxChampionsGry.Ustaw(", BIL)

    def test_bowling_place_aim_hook_and_camera(self):
        self.assertIn("if (u < 2.2f) { startX = w.x - f.x;", KRE)
        self.assertIn("float kat = Mathf.Clamp(Vector3.SignedAngle(Vector3.forward, d, Vector3.up), -maxKat, maxKat);", KRE)
        self.assertIn("if (hakRzutu != 0f && u > glowa * .6f && u < glowa) kula.AddForce(Vector3.right * hakRzutu, ForceMode.Acceleration);", KRE)
        self.assertIn("if (moze) Rzut(kier, Mathf.Lerp(predkoscMin, predkoscMax, moc));", KRE)
        self.assertIn("var b = kula.position; float z = Mathf.Min(b.z - 2.6f, talia.z - 4.2f);", KRE)   # rides behind, stops short of the pins
        self.assertIn("if (BraxChampionsStanowisko.Gra(this, tor) && !Koniec(tor)) NaRozbieg();", KRE)  # back to aim for the next ball
        self.assertIn("if (tor >= 0 && !BraxChampionsStanowisko.Aktywne) Status();", KRE)
        # a real hook: at most ~3 steps x 0.35 m/s2 over the back third (about 0.6 m of curl at 9 m/s)
        hak = float(re.search(r"public float hakSila = ([\d.]+)f;", KRE).group(1))
        lane_back = 23.7 * .4
        t = lane_back / 9.0
        self.assertLess(.5 * 3 * hak * t * t, 1.0)
        # the speed: 18-32 km/h real (a recreational roll to a strong one)
        lo, hi = (float(v) for v in re.search(r"public float predkoscMin = ([\d.]+)f, predkoscMax = ([\d.]+)f;", KRE).groups())
        self.assertGreater(lo / 1.2944 * 3.6, 15)
        self.assertLess(hi / 1.2944 * 3.6, 35)


if __name__ == "__main__":
    unittest.main()
