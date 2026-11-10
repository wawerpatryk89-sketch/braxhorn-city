"""MIASTO R1 10.11 — łodzie (właściciel 17:37): „widok za sterem w jachcie, nie w motorówce; na motorówce siedzieć z przodu
i mieć ręce na kierownicy — jest animacja driver; na skuterze też odpowiednio; skuter, motorówka — z tyłu prawidłowe siedzenie
i ręce; potem jacht ze środka kamera, widok zza steru”."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
A = ROOT / "unity-city/Assets"


def czytaj(p):
    return pathlib.Path(p).read_text(encoding="utf-8")


def metoda(src, sygn):
    i = src.index(sygn)
    j = src.index("{", i)
    d = 0
    for k in range(j, len(src)):
        if src[k] == "{":
            d += 1
        elif src[k] == "}":
            d -= 1
            if d == 0:
                return src[i:k + 1]
    raise AssertionError(sygn)


V = czytaj(A / "Claude/BraxVessel.cs")
P = czytaj(A / "Claude/BraxPose.cs")
G = czytaj(A / "BraxGymPlayer.cs")
M = czytaj(A / "Editor/Claude/Region/BraxNabrzezeMarina.cs")
S = czytaj(A / "Editor/Claude/Region/BraxNabrzezeSter.cs")
J = czytaj(A / "Editor/Claude/Region/BraxMiastoJazdaBraxa.cs")


class Kamery(unittest.TestCase):
    def test_widok_zza_steru_tylko_jacht(self):
        self.assertIn('public bool HasPilotCamera => rider && AtHelm && label == "JACHT";', V)
        self.assertNotIn('t.label == "JACHT" || t.label == "MOTORÓWKA"', M)   # motorówka bez kamery pilota

    def test_jacht_kamera_ze_stanowiska(self):
        j = metoda(S, "public static bool Jacht(")
        self.assertIn("vessel.helmCameraLocal = new Vector3(0f, yd + 1.86f * K, zBiodra - .30f * K);", j)   # nad szybą flybridge'u (1,45 m)
        self.assertIn("BraxNabrzezeSter.Jacht(v.transform, go, vessel)", M)


class Siedzenie(unittest.TestCase):
    def test_klip_jazda_przy_sterze(self):
        u = metoda(V, "void UstawZaSterem(")
        self.assertIn('player.clips.GetClip("Jazda")', u)
        self.assertIn('player.clips.CrossFade("Jazda", .2f, PlayMode.StopAll);', u)
        self.assertIn("BraxPose.Mode.Kierowca", u)
        # gracz przy sterze nie nadpisuje klipu zamrożonym Idle
        self.assertIn('!clips.IsPlaying("Jazda")', G)

    def test_po_zejsciu_bez_jazdy(self):
        for sygn in ("public bool Dismount()", "void Zwolnij()", "public bool Interact()"):
            self.assertIn("ZdejmijJazde(", metoda(V, sygn))

    def test_skuter_nogi_okrakiem_reka_z_klipu(self):
        ride = P[P.index("case Mode.Ride:"):P.index("case Mode.Kierowca:")]
        self.assertIn("if (!klip)", ride)                                   # ręce z klipu + IK, nie z celowania
        self.assertIn("mode == Mode.Kierowca)", P)                          # dłonie na kole także w trybie kierowcy
        self.assertIn("iter < 8", metoda(P, "static void DlonNaUchwyt("))

    def test_motorowka_prawdziwe_kolo(self):
        self.assertIn('x.name.Equals("Cylinder_3"', M)
        self.assertIn("vessel.seat.z = Mathf.Clamp(hub.z - .50f * K", M)
        self.assertIn("if (trafF.Count > 0) vessel.seat.y = trafF[0];", M)   # siedzisko = poduszka, nie rama chrom
        self.assertIn("chwytL = new Vector3(-R * .866f, R * .5f, 0f)", M)


class StanowiskoJachtu(unittest.TestCase):
    def test_kolo_kreci_sie_i_trzyma_dlonie(self):
        self.assertIn("kolo.localRotation = koloBaza * Quaternion.AngleAxis(-koloSter * 110f, Vector3.forward);", metoda(V, "public void Drive("))
        j = metoda(S, "public static bool Jacht(")
        self.assertIn("g.SetParent(kolo, false)", j)           # uchwyty dłoni są dziećmi koła
        self.assertIn("vessel.kolo = kolo;", j)

    def test_wymiary_realne_razy_K(self):
        j = metoda(S, "public static bool Jacht(")
        for w in ("1.5f * K", ".55f * K", ".98f * K", "1.18f * K", ".78f * K", ".26f * K", ".48f * K"):
            self.assertIn(w, j)

    def test_rury_widoczne_z_zewnatrz(self):
        r = metoda(S, "public void Rura(")
        self.assertIn("v.Add(a + n0 * r); v.Add(a + n1 * r); v.Add(b + n1 * r); v.Add(b + n0 * r);", r)


class JachtPrawdziwaWielkosc(unittest.TestCase):
    """Właściciel 18:19–18:20: jachty jak w Mistrzach (40,33 × 8,2 m × K), marina nie większa, „tobie starczy z 5”."""

    def test_jacht_40_m(self):
        self.assertIn('plik = "yacht.glb", label = "JACHT", dlug = 40.33f', M)

    def test_piec_jachtow_przy_A_i_w_boksach_F(self):
        self.assertIn('var pref = new[] { "F1", "F2", "F3", "F4" };', M)
        self.assertIn("if (n >= 5) break;", M)
        self.assertNotIn('"D" + (i + 1)', M)            # boksy D bez jachtów (dziób 15 m od pomostu F — nie wypłynie)

    def test_boks_F_miesci_kadlub(self):
        palce = [22, 36.5, 51, 65.5, 80]
        self.assertIn("static readonly float[] PalceF = { 22f, 36.5f, 51f, 65.5f, 80f };", M)
        K = 1.294
        woda = min(b - a for a, b in zip(palce, palce[1:])) - 2 * 1.0 * K
        self.assertGreaterEqual(woda - 8.2 * K, 2 * .5)   # ≥ 0,5 z każdej burty na odbijacze

    def test_kamera_steru_skala_K(self):
        j = metoda(S, "public static bool Jacht(")
        self.assertIn("deckY + 1.7f * K", j)                # szyba flybridge'u w prawdziwej wielkości (1,45 m)
        self.assertIn("deckY + 2.2f * K", j)                # hardtop (2,95 m)


class KlipWlasciciela(unittest.TestCase):
    def test_tylko_obroty_i_import_legacy(self):
        p = metoda(J, "static AnimationClip Przepnij(")
        self.assertIn('IndexOf("Rotation"', p)
        self.assertIn("ModelImporterAnimationType.Legacy", J)
        self.assertIn('Zrodlo = "Assets/Source/Claude/Brax/Ruchy/Driving.fbx"', J)

    def test_fbx_nie_w_gicie_tylko_z_wydania(self):
        gi = czytaj(ROOT / ".gitignore")
        self.assertIn("unity-city/Assets/Source/Claude/Brax/", gi)
        wf = czytaj(ROOT / ".github/workflows/claude-region-build.yml")
        self.assertEqual(wf.count("python3 unity-tools/claude/brax_jazda.py"), 3)
        bj = czytaj(ROOT / "unity-tools/claude/brax_jazda.py")
        self.assertIn('FBX_SHA = "74ccdfa8557ecf510be804b973b09606a2cc5a5b0048a4c0e5832a30451ed7e9"', bj)
        self.assertIn("sys.exit(0)", bj)                         # bez pliku build idzie dalej (poza z kodu)


if __name__ == "__main__":
    unittest.main()
