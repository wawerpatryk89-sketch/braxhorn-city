"""The quad track and the owner's quads (owner 24.11.2026 11:31-11:45: "tu są kłady ... tor do kładów", "to są całe koła ...
żeby się kręciły, kierownica, żeby się kręciła tak samo", "Zrób tam, gdzie poleciłeś, zachód", "górki, pagórki, wyskoki",
"budkę trzeba zrobić ładną. Wypożyczanie ... minimum 4 kłady ... każdy inny kolor").

  - the owner's two Tripo quads cut into a body, four wheels and the handlebar, at the real machines' size, six paints, two
    far LODs, in an encrypted owner pack (the repository is public);
  - BraxChampionsKlad: AKCJA = WSIĄDŹ, astride (BraxPose.Ride), four ray-cast wheels on springs, the wheels roll, the front
    ones and the bars steer, jumps fly on gravity x K, the quad's own touch controls, a chase camera, real engine recordings;
  - BraxChampionsTorKladow: a 1.7 km loop west of the city, north of the D2 hill, FIM's motocross rules where they apply;
    the rental booth with the six quads, the grandstand, the start gate, the START / META arch; BraxChampionsTorCzas."""
import hashlib
import json
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
KLAD = (RT / "BraxChampionsKlad.cs").read_text(encoding="utf-8")
CZAS = (RT / "BraxChampionsTorCzas.cs").read_text(encoding="utf-8")
KLADY = (ED / "BraxChampionsKlady.cs").read_text(encoding="utf-8")
TOR = (ED / "BraxChampionsTorKladow.cs").read_text(encoding="utf-8")
BUILD = (ED / "BraxChampionsBuildings.cs").read_text(encoding="utf-8")
PREP = (TOOLS / "klady/przygotuj_klady.py").read_text(encoding="utf-8")
PACK = ROOT / "unity-city/Prywatne/braxhorn-sklep-klady-01"
K = 2.33 / 1.80


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def floats(name, src=TOR):
    return [float(v) for v in re.search(name + r"\s*=\s*([-\d.f]+)", src).group(1).rstrip("f").split()]


def const(name, src=TOR):
    return float(re.search(r"\b" + name + r"\s*=\s*(-?[\d.]+)f", src).group(1))


def rogi():
    return [(float(a), float(b), float(c)) for a, b, c in re.findall(r"new Vector3\((-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f\)", TOR.split("public static readonly Vector3[] Rogi =")[1].split("};")[0])]


def petla():
    """the loop as the C# builds it: fillets of the corners; (arc lengths, straight lengths, the line every 1 m)"""
    C = rogi(); n = len(C); P = [(x, z) for x, z, _ in C]
    t = []; luki = []; srodki = []
    for i in range(n):
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]; R = C[i][2]
        d1 = ((b[0] - a[0]), (b[1] - a[1])); l1 = math.hypot(*d1); d1 = (d1[0] / l1, d1[1] / l1)
        d2 = ((c[0] - b[0]), (c[1] - b[1])); l2 = math.hypot(*d2); d2 = (d2[0] / l2, d2[1] / l2)
        ang = math.acos(max(-1, min(1, d1[0] * d2[0] + d1[1] * d2[1])))
        t.append(R * math.tan(ang / 2)); luki.append(R * ang)
        cross = d1[0] * d2[1] - d1[1] * d2[0]
        p1 = (b[0] - d1[0] * t[-1], b[1] - d1[1] * t[-1]); nrm = (-d1[1] * math.copysign(1, cross), d1[0] * math.copysign(1, cross))
        srodki.append((p1[0] + nrm[0] * R, p1[1] + nrm[1] * R, R))
    proste = [math.dist(P[i], P[(i + 1) % n]) - t[i] - t[(i + 1) % n] for i in range(n)]
    return luki, proste, srodki


class Paczka(unittest.TestCase):
    def test_encrypted_owner_pack(self):
        m = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "pojazdy_klady_01")
        self.assertEqual(m["recipientCertificateSha256"], sha(ROOT / "unity-city/Prywatne/braxhorn-sklep-motory-01/recipient.crt"))
        self.assertEqual(sha(PACK / "recipient.crt"), m["recipientCertificateSha256"])
        for p in m["parts"]:
            self.assertEqual(sha(PACK / p["name"]), p["sha256"])
        # nothing of the owner's in the open: only the CMS parts, the manifest and the transfer certificate / key
        self.assertEqual(sorted(f.name for f in PACK.iterdir()), sorted(["manifest.json", "recipient.crt", "recipient.pem.enc"] + [p["name"] for p in m["parts"]]))
        files = m["files"]
        self.assertIn("oryginal/rancher_tripo.glb", files); self.assertIn("oryginal/raptor_tripo.glb", files)
        for model in ("rancher", "raptor"):
            for c in ("korpus", "kolo_przod_lewe", "kolo_przod_prawe", "kolo_tyl_lewe", "kolo_tyl_prawe", "kierownica"):
                for suf in ("", "_lod1", "_lod2"):
                    self.assertIn("pojazdy_klady_01/klad_%s_%s%s.glb" % (model, c, suf), files)
            self.assertIn("pojazdy_klady_01/_tekstury/klad_%s_normal__lin.jpg" % model, files)     # data maps linear (BraxMiastoAssetyTekstury)
            self.assertIn("pojazdy_klady_01/_tekstury/klad_%s_mr__lin.png" % model, files)
        kolory = [f for f in files if "_kolor_" in f]
        self.assertEqual(len(kolory), 6)                                                           # minimum 4, each its own colour
        self.assertIn("pojazdy_klady_01/katalog.json", files)
        # install_sklep_modele.py stages every braxhorn-sklep-* pack; the build's sparse checkout takes them all
        self.assertIn("unity-city/Prywatne/braxhorn-*/", (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8"))

    def test_real_size_and_the_cut(self):
        # one uniform scale from the published dimensions (Honda FourTrax Rancher 2024: 82.8 in long; Raptor 700R: 72.8 in)
        self.assertIn('"skala": 2.08', PREP); self.assertIn('"skala": 1.90', PREP)
        self.assertLess(abs(.975 * 2.08 - 82.8 * .0254) / (82.8 * .0254), .05)
        self.assertLess(abs(.97 * 1.90 - 72.8 * .0254) / (72.8 * .0254), .02)
        # the wheel's pivot at its own measured hub (no wobble), the cylinder 1.12 R for the knobs, 1.035 R under a fender
        self.assertIn('kola[nazwa] = {"x": (lo[0] + hi[0]) / 2, "y": (lo[1] + hi[1]) / 2, "z": (lo[2] + hi[2]) / 2,', PREP)
        self.assertIn("rmax = R * (1.12 - (1.12 - rg) * sn ** 2)", PREP)
        # the bars: above a plane square to the steering axis, joined to the grips; glTFast's x mirror in the catalogue
        self.assertIn('u = [-piv[0] * s, piv[1] * s, piv[2] * s]                     # Unity axes', PREP)
        # the paint repainted in linear light, its shading kept
        self.assertIn("nowy = t[None, None, :] * (L / ref)[..., None]", PREP)
        # LODs with the texture kept (MeshLab's quadric collapse with texture)
        self.assertIn("meshing_decimation_quadric_edge_collapse_with_texture", PREP)


class Kierowanie(unittest.TestCase):
    def test_akcja_and_the_rider(self):
        self.assertIn("public sealed class BraxChampionsKlad : MonoBehaviour, IBraxInterakcja", KLAD)
        self.assertIn('return "WSIĄDŹ · " + nazwa;', KLAD)
        self.assertIn("BraxPose.Get(g).Set(BraxPose.Mode.Ride, transform);", KLAD)
        # the seat that stops the walking is never offered to anyone (inactive: not in BraxSeat.All)
        self.assertIn('var o = new GameObject("KŁAD | miejsce kierowcy"); o.SetActive(false);', KLAD)
        self.assertIn("if (tc && tc.enabled) tc.enabled = false; else tc = null;", KLAD)
        self.assertIn("if (!force && Mathf.Abs(PredkoscKmh) > 8f) return false;", KLAD)          # off only when slow
        # a reset by the game lets the quad go of the player
        self.assertIn("if (!gracz.transform.IsChildOf(transform) || gracz.seat != miejsce)", KLAD)
        self.assertNotIn("void Start(", CZAS)                                                       # no Unity message clash

    def test_physics(self):
        self.assertIn("rb.AddForce(Vector3.down * (g + Physics.gravity.y), ForceMode.Acceleration);", KLAD)   # g x K
        self.assertIn("float kSpr = masa * g * .25f / (.4f * skok * K);", KLAD)                    # 40 % sag
        self.assertIn("float lim = przyczepnosc * sila;", KLAD)                                     # grip from the load
        self.assertIn("if (razem > lim && razem > 0f) { boczna *= lim / razem; wzdluz *= lim / razem; }", KLAD)
        self.assertIn("rb.AddTorque(right * (hamulec - gaz) * 2.4f, ForceMode.Acceleration);", KLAD)   # pitch in the air
        self.assertIn("if (doGory > 2.5f) Postaw();", KLAD)
        self.assertIn("if (h.collider.attachedRigidbody == rb || h.collider.transform.IsChildOf(transform)) continue;", KLAD)
        # Unity 6 names behind the project's own compat (as the billiards)
        self.assertIn("#if UNITY_6000_0_OR_NEWER", KLAD); self.assertIn("rb.angularDampingCompat(.6f);", KLAD)

    def test_wheels_bars_camera_sound(self):
        self.assertIn("obrot += vNaprzod / R * Mathf.Rad2Deg * dt;", KLAD)
        self.assertIn("if (i < 2) piasta.localRotation = Quaternion.Euler(0f, skret, 0f);", KLAD)
        self.assertIn("kierownica.localRotation = Quaternion.AngleAxis(skret * .9f, osKierownicy) * kierownicaSpoczynek;", KLAD)
        self.assertIn("float lockDeg = maxSkret * Mathf.Lerp(1f, .3f, Mathf.Clamp01(v));", KLAD)
        self.assertIn("[DefaultExecutionOrder(5100)]", KLAD)
        for f in ("klad_silnik_wolne.ogg", "klad_silnik_jazda.ogg"):
            p = ROOT / "unity-city/Assets/Source/Mistrzowie/Audio/Klad" / f
            self.assertGreater(p.stat().st_size, 20000)
            self.assertRegex((p.parent / (f + ".meta")).read_text(), r"guid: [0-9a-f]{32}")
        src = (TOOLS / "klady/silnik_zrodla.md").read_text(encoding="utf-8")
        self.assertIn("CC0", src); self.assertIn("bigsoundbank.com/quad-stopped-s0711.html", src)

    def test_the_fleet(self):
        flota = re.findall(r'\("(rancher|raptor)", "(\w+)", "(KŁAD · [^"]+)"\)', KLADY)
        self.assertEqual(len(flota), 6)
        self.assertEqual(len({k for _, k, _ in flota}), 6)                                          # six colours
        self.assertIn("var lg = t.gameObject.AddComponent<LODGroup>(); lg.SetLODs(lody.ToArray());", KLADY)
        self.assertIn("mat.SetTexture(\"_ARM\", arm);", KLADY)
        self.assertIn("kopia.RecalculateTangents();", KLADY)
        self.assertIn('if (!Jest) { BraxMiastoAssety.Log("kład "', KLADY)                        # no pack, no quads, the build goes on
        for f in (ED / "BraxChampionsKlady.cs.meta", ED / "BraxChampionsTorKladow.cs.meta", RT / "BraxChampionsKlad.cs.meta", RT / "BraxChampionsTorCzas.cs.meta"):
            self.assertRegex(f.read_text(encoding="utf-8"), r"guid: [0-9a-f]{32}")
        # the velocity's Unity 6 rename inside the accessors' bodies (the scope check parses both branches)
        self.assertIn("        get\n        {\n#if UNITY_6000_0_OR_NEWER\n            return rb.linearVelocity;", KLAD)


class Tor(unittest.TestCase):
    def test_fim_motocross(self):
        luki, proste, _ = petla()
        dl = sum(luki) + sum(proste)
        self.assertGreater(dl, 1500); self.assertLess(dl, 2000)                                    # FIM 1.5-2 km
        self.assertGreaterEqual(const("Szer"), 6.0)                                                # FIM >= 6 m
        # the start straight (the last straight, into corner 0): 80 m+ before the first turn, no obstacle on it
        self.assertGreater(proste[-1], 120)
        self.assertNotRegex(TOR, r"new Przeszkoda\(10,")
        # rolling waves 8-10 m apart, at most 0.8 m
        self.assertIn("case Typ.Fale: return p.n * 9f;", TOR)
        for h in re.findall(r"Typ\.Fale, \.(\d+)f", TOR):
            self.assertLessEqual(float("." + h), .8)
        # every obstacle on its straight, 6 m clear of the next turn (the builder throws otherwise)
        dlug = {"Stol": None, "Podwojna": 6 + .4 + 5 + 4 + 5 + .4 + 8, "Schodek": 4.5 + 22 + 9, "Gorka": 40 + 8 + 18, "Brod": 18}
        for pr, od, typ, h, n in re.findall(r"new Przeszkoda\((\d+), ([\d.]+)f, Typ\.(\w+), \.?([\d.]+)f(?:, (\d+))?\)", TOR):
            L = dlug.get(typ) or (int(n) * (9 if typ == "Fale" else 4.5) if typ in ("Fale", "Muldy") else (35 if float(h) >= 1.8 else 23))
            self.assertLess(float(od) + L, proste[int(pr)] - 6, (typ, pr))
        self.assertIn('throw new InvalidOperationException("quad track: obstacle " + i + " runs into a turn");', TOR)

    def test_the_site(self):
        x0, x1, z0, z1 = const("X0"), const("X1"), const("Z0"), const("Z1")
        # west of the region (x < 0), north of the D2 railway hill (ellipse centre -330, 186.5; radii 250, 400)
        self.assertLessEqual(x1, 0.0)
        for x in (x0, (x0 + x1) / 2, x1):
            self.assertGreater(((x + 330) / 250) ** 2 + ((z0 - 186.5) / 400) ** 2, 1.2)
        # the loop inside the fence with room, the fence inside the rim band's end
        inset = const("FenceInset"); brzeg = const("Szer") / 2 + const("Pobocze")
        xs = [x for x, z, r in rogi()]; zs = [z for x, z, r in rogi()]
        self.assertGreater(min(xs) - brzeg - (x0 + inset), 6); self.assertGreater((z1 - inset) - (max(zs) + brzeg), 6)
        self.assertGreater(min(zs) - brzeg - (z0 + inset), 6)
        self.assertGreater(inset, const("RimBand"))
        # the parallel sections never closer than 30 m (their embankments never meet)
        luki, proste, srodki = petla()
        C = rogi(); n = len(C); pts = []
        for i in range(n):
            a, b = C[i], C[(i + 1) % n]
            for k in range(0, 100, 2): pts.append((i, a[0] + (b[0] - a[0]) * k / 100, a[1] + (b[1] - a[1]) * k / 100))
        for i, xa, za in pts:
            for j, xb, zb in pts:
                if abs(i - j) > 1 and not {i, j} == {0, n - 1}:
                    self.assertGreater(math.hypot(xa - xb, za - zb), 30, (i, j))
        # the paddock and the road inside the fence; the road ends on the city's grass (x 0-53), never on its asphalt (x 60+)
        self.assertGreater(const("PZ1"), const("PZ0")); self.assertLess(const("PZ1"), z1 - inset)
        self.assertLessEqual(const("DrogaX1"), 53.0)
        self.assertIn("Mathf.Lerp(DrogaMiasto, PaddockY, Smooth(0f, -DrogaX0, -x))", TOR)
        self.assertIn("DrogaMiasto = .33f", TOR)                                                    # 12 cm over the grass (0.21)

    def test_ground_and_far_land(self):
        self.assertIn("if (e <= brzeg) h = Tor(i, u) - .35f;", TOR)                                 # under the riding surface
        self.assertIn("h = he + Mathf.Clamp(h - he, -dd / 1.25f, dd / 1.0f);", TOR)
        self.assertIn("if (dr > 35f) h -= RimDip * (1f - Smooth(0f, RimBand, DoKrawedzi(x, z)));", TOR)
        self.assertIn("BraxChampionsHorizon.Docisk = (x, z) => Mathf.Min(poprzedni != null ? poprzedni(x, z) : float.PositiveInfinity, Docisk(x, z));", TOR)
        # after the villas (their press first), before the far land
        self.assertLess(BUILD.index("BraxChampionsWilleDzialki.Zbuduj(null)"), BUILD.index("BraxChampionsTorKladow.Zbuduj(null)"))
        # no flicker: thin layers at least 4 cm proud (the finish band, the bays, the gate's pad), the skirts under every edge
        self.assertIn("my + .04f, my + .07f", TOR); self.assertIn("y - .1f, y + .1f, baysZ - 2.2f", TOR); self.assertIn("gy - .2f, gy + .08f", TOR)
        self.assertIn("vT.Add(top - Vector3.up * .6f * K)", TOR)

    def test_paddock_and_timer(self):
        for s in ('"WYPOŻYCZALNIA KŁADÓW"', '"BRAXHORN"', '"BRAXHORN QUAD RENTAL"', '"BRAXHORN QUAD CROSS"'):
            self.assertIn(s, TOR)
        self.assertIn("BraxChampionsKlady.Postaw(g, f.model, f.kolor, f.napis,", TOR)
        self.assertIn("czas.punkty = pkt.ToArray();", TOR)
        self.assertIn("bool przezMete = indeks > n - 8 && i < 8;", CZAS)
        self.assertIn("if (liczy && zaliczone >= n * .9f)", CZAS)                                  # no lap by a short cut
        self.assertIn('const string Klucz = "BRAX_TOR_KLADOW_NAJLEPSZE";', CZAS)

    def test_textures_poly_haven_4k(self):
        prep = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        fx = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
        for slug in ("muddy_tracks", "brown_mud_02", "gravel_ground_01"):
            self.assertIn("'" + slug + "'", prep)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("/4k/", json.dumps(fx[slug][key]["4k"]))
            self.assertIn('"' + slug + '"', TOR)
        # from the owner's list where something fits: the booth's dark timber
        self.assertIn('"dark_paneled_wood"', TOR)


def linia(krok=1.0):
    """the centre line as the C# lays it out: the straights between the fillets' tangent points and the fillet arcs"""
    C = rogi(); n = len(C); P = [(x, z) for x, z, _ in C]; pts = []
    for i in range(n):
        a, b, c = P[i - 1], P[i], P[(i + 1) % n]; R = C[i][2]
        d1 = ((b[0] - a[0]), (b[1] - a[1])); l1 = math.hypot(*d1); d1 = (d1[0] / l1, d1[1] / l1)
        d2 = ((c[0] - b[0]), (c[1] - b[1])); l2 = math.hypot(*d2); d2 = (d2[0] / l2, d2[1] / l2)
        ang = math.acos(max(-1, min(1, d1[0] * d2[0] + d1[1] * d2[1]))); t = R * math.tan(ang / 2)
        sgn = math.copysign(1, d1[0] * d2[1] - d1[1] * d2[0])
        p1 = (b[0] - d1[0] * t, b[1] - d1[1] * t); p2 = (b[0] + d2[0] * t, b[1] + d2[1] * t)
        o = (p1[0] - d1[1] * sgn * R, p1[1] + d1[0] * sgn * R)
        a1 = math.atan2(p1[1] - o[1], p1[0] - o[0]); a2 = math.atan2(p2[1] - o[1], p2[0] - o[0])
        da = (a2 - a1 + math.pi) % (2 * math.pi) - math.pi
        m = max(2, int(abs(da) * R / krok))
        pts += [(o[0] + R * math.cos(a1 + da * k / m), o[1] + R * math.sin(a1 + da * k / m)) for k in range(m + 1)]
        # the straight on to the next corner's fillet
        e, f2 = c, P[(i + 2) % n]; R2 = C[(i + 1) % n][2]
        g1 = (e[0] - b[0], e[1] - b[1]); lg = math.hypot(*g1); g1 = (g1[0] / lg, g1[1] / lg)
        g2 = (f2[0] - e[0], f2[1] - e[1]); lh = math.hypot(*g2); g2 = (g2[0] / lh, g2[1] / lh)
        t2 = R2 * math.tan(math.acos(max(-1, min(1, g1[0] * g2[0] + g1[1] * g2[1]))) / 2)
        q = (e[0] - g1[0] * t2, e[1] - g1[1] * t2); L = math.dist(p2, q); m = max(1, int(L / krok))
        pts += [(p2[0] + (q[0] - p2[0]) * k / m, p2[1] + (q[1] - p2[1]) * k / m) for k in range(m + 1)]
    return pts


def rampy():
    blok = TOR.split("public static readonly Wyrzutnia[] Rampy =")[1].split("};")[0]
    return [tuple(float(v) for v in w) for w in re.findall(r"new Wyrzutnia\(([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)", blok)]


class Freestyle(unittest.TestCase):
    """owner 24.11 12:57: "rampy po bokach" - the freestyle zone, its kickers and the tricks in the air"""

    def test_zone_clear_of_the_track(self):
        X0, X1, Z0, Z1 = const("FX0"), const("FX1"), const("FZ0"), const("FZ1")
        lip, bieg = const("FLip"), const("FBieg")
        deck, kat_l = const("LadowanieDeck"), const("LadowanieKat")
        pts = linia()
        def odl(x, z):
            dx = max(X0 - x, x - X1, 0.0); dz = max(Z0 - z, z - Z1, 0.0); return math.hypot(dx, dz)
        self.assertGreaterEqual(min(odl(x, z) for x, z in pts), 25.0)                           # no section within 25 m
        for z, h, kat, luka, lad in rampy():
            a = math.radians(kat); L = h / (1 - math.cos(a)) * math.sin(a)
            start, koniec = lip - L - bieg, lip + luka + deck + lad / math.tan(math.radians(kat_l))
            self.assertGreater(koniec, lip + luka); self.assertLess(koniec + 18, X1)            # the run-out inside the zone
            # the run-up's lane is clear of the track too (the west straight)
            for x in (start, start + 10):
                self.assertGreater(min(math.hypot(x - px, z - pz) for px, pz in pts), 20.0)
            self.assertTrue(Z0 < z < Z1)

    def test_ballistics_reachable_and_long_enough(self):
        vmax_wolny, a0 = 75 / 3.6, 3.4                                                          # the rancher, the slower one
        bieg = const("FBieg")
        for z, h, kat, luka, lad in rampy():
            a = math.radians(kat)
            dh = h + luka * math.tan(a) - lad
            vmin = math.sqrt(9.81 * luka * luka / (2 * math.cos(a) ** 2 * dh))                   # as VMin() in the C#
            # the rancher gets there on the run-up (its acceleration falls with v^2 to its top speed), with 10 % to spare
            v, s = 0.0, 0.0
            while s < bieg:
                v += a0 * (1 - (v / vmax_wolny) ** 2) * .01; s += v * .01
            v_lip = math.sqrt(v * v - 2 * 9.81 * h)                                             # the climb up the kicker
            self.assertGreater(v_lip, vmin * 1.15, "rampa z=%s" % z)
            # in the air long enough for the trick it is meant for (the button shows after .15 s): a 360 on every
            # kicker, the backflip on the medium and the big one
            t = luka / (vmin * math.cos(a))
            self.assertGreater(t, const("CzasObrotu", KLAD) + .15, "rampa z=%s t=%.2f" % (z, t))
            if h >= 2.0:
                self.assertGreater(t, const("CzasSalta", KLAD) + .15, "rampa z=%s t=%.2f" % (z, t))
            # the landing: the flight path at the knuckle within the clean-landing tilt (cos > .72 -> under 44 deg)
            vy = vmin * math.sin(a) - 9.81 * t
            self.assertLess(abs(math.degrees(math.atan2(vy, vmin * math.cos(a)))), 44.0)

    def test_tricks(self):
        self.assertIn("public const int PunktyObrot = 150, PunktySalto = 400;", KLAD)
        self.assertIn("trikOs = t == Trik.Salto ? -transform.right : Vector3.up * (skretWej < 0f ? -1f : 1f);", KLAD)
        # turned by the engine's own physics (the angular velocity), never faster than the body may spin
        for nazwa in ("CzasObrotu", "CzasSalta"):
            w = math.radians(360 / const(nazwa, KLAD)) / (1 - .6 * .02)
            self.assertLess(w, 9.0)
        self.assertIn("rb.maxAngularVelocity = 9f;", KLAD)
        self.assertIn("rb.angularVelocity = trikOs * (w * Mathf.Deg2Rad / (1f - .6f * dt));", KLAD)
        # the landing: clean on its wheels, a crash mid-trick (a wheel or the body down) scores nothing
        self.assertIn("if (transform.up.y <= .72f)", KLAD)
        self.assertIn("if (trik != Trik.Brak) Wywrotka();                 // a wheel down mid-trick", KLAD)
        self.assertIn("void OnCollisionEnter(Collision c) { if (gracz && trik != Trik.Brak) Wywrotka(); }", KLAD)
        self.assertIn("else if (!rozbity && lot >= .45f) Ladowanie();", KLAD)
        # the buttons in the air only, the keys Q / Space on a desktop; the best jump kept on the phone
        self.assertIn("if (MozeTrik && rSalto.Contains(p)) { chceSalto = true; continue; }", KLAD)
        self.assertIn("if (MozeTrik && Input.GetKeyDown(KeyCode.Space)) chceSalto = true;", KLAD)
        self.assertIn('const string KluczRekordu = "BRAX_KLAD_REKORD_SKOKU";', KLAD)
        # the camera keeps the flight's direction in the air (no whip in a backflip)
        self.assertIn("if (lot > .1f || trik != Trik.Brak) kier =", KLAD)
        # the zone is built by the track's builder
        self.assertIn("Freestyle(g, b);", TOR)
        for nr in (1, 2, 3):
            self.assertIn("tor_kladow_rampa_", TOR); self.assertIn("tor_kladow_ladowisko_", TOR)
        self.assertIn('"BRAXHORN FREESTYLE"', TOR)


if __name__ == "__main__":
    unittest.main()
