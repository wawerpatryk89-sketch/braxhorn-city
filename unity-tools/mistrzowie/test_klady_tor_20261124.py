"""The quad track and the owner's quads (owner 24.11.2026 11:31-11:45: "tu są kłady ... tor do kładów", "to są całe koła ...
żeby się kręciły, kierownica, żeby się kręciła tak samo", "Zrób tam, gdzie poleciłeś, zachód", "górki, pagórki, wyskoki",
"budkę trzeba zrobić ładną. Wypożyczanie ... minimum 4 kłady ... każdy inny kolor").

  - the owner's two Tripo quads cut into a body, four wheels and the handlebar, at the real machines' size, six paints, two
    far LODs, in an encrypted owner pack (the repository is public);
  - BraxChampionsKlad: AKCJA = WSIĄDŹ, astride (BraxPose.Ride), four ray-cast wheels on springs, the wheels roll, the front
    ones and the bars steer, jumps fly on gravity x K, the quad's own touch controls, a chase camera, real engine recordings;
  - BraxChampionsTorKladow: a 2.5 km loop west of the city, north of the D2 hill (14:00-14:20, his photos of real tracks:
    "jakieś zakręty", "w jakichś błotach albo po jakichś lasach", "lasy, górki", "więcej lasu"): a narrow woods' trail
    over an 18 m wooded hill (logs, mud, a creek), then a motocross field of hairpins (FIM's rules where they apply); the
    rental booth with the six quads, the grandstand, the start gate, the START / META arch; BraxChampionsTorCzas;
  - BraxChampionsLasKladow: the forest - Poly Haven's scanned pines and firs as crossed cards;
  - BraxChampionsOponyKladow (17:11 "Opony na zakrętach" + "Ściany z opon"): tyre bundles, tyre walls, the stakes' tape;
  - BraxChampionsKlubKladow (19:31 "bardziej luksusowo", "Kłady na podestach" + "Taras nad torem"): the quad club, the VIP terrace;
  - BraxChampionsWynikiKladow, BraxChampionsTablicaTop, BraxChampionsPodiumKladow (20:31 "Tablica TOP 10", "Turniej z
    rywalami", "Podium i puchar"; "nie do jazdy w nocy"): the TOP 10 board, the BRAXHORN cup, the podium and its cups."""
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
WYSCIG = (RT / "BraxChampionsWyscig.cs").read_text(encoding="utf-8")
RYWAL = (RT / "BraxChampionsKladRywal.cs").read_text(encoding="utf-8")
ZAWODNIK = (ED / "BraxChampionsKladZawodnik.cs").read_text(encoding="utf-8")
BLOTO = (RT / "BraxChampionsBloto.cs").read_text(encoding="utf-8")
LAS = (ED / "BraxChampionsLasKladow.cs").read_text(encoding="utf-8")
LAS_IMPORT = (ED / "BraxChampionsLasTextureImport.cs").read_text(encoding="utf-8")
OPONY = (ED / "BraxChampionsOponyKladow.cs").read_text(encoding="utf-8")
KLUB = (ED / "BraxChampionsKlubKladow.cs").read_text(encoding="utf-8")
OBROTNICA = (RT / "BraxChampionsObrotnica.cs").read_text(encoding="utf-8")
WYNIKI = (RT / "BraxChampionsWynikiKladow.cs").read_text(encoding="utf-8")
TOP = (RT / "BraxChampionsTablicaTop.cs").read_text(encoding="utf-8")
PODIUM = (ED / "BraxChampionsPodiumKladow.cs").read_text(encoding="utf-8")
OPONA = ROOT / "unity-city/Assets/Source/Mistrzowie/Opony/opona.bytes"
ATLAS = ROOT / "unity-city/Assets/Source/Mistrzowie/Las/las_atlas.png"
PACK = ROOT / "unity-city/Prywatne/braxhorn-sklep-klady-01"
K = 2.33 / 1.80


def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def floats(name, src=TOR):
    return [float(v) for v in re.search(name + r"\s*=\s*([-\d.f]+)", src).group(1).rstrip("f").split()]


def const(name, src=TOR):
    return float(re.search(r"\b" + name + r"\s*=\s*(-?[\d.]+)f", src).group(1))


def rogi():
    """the corners: (x, z, fillet radius, the riding width from this corner on - Start / MX / Las)"""
    szer = {n: const(n) for n in ("Start", "MX", "Las")}
    blok = TOR.split("public static readonly Vector4[] Rogi =")[1].split("};")[0]
    return [(float(a), float(b), float(c), szer[d]) for a, b, c, d in re.findall(r"new Vector4\((-?[\d.]+)f, (-?[\d.]+)f, ([\d.]+)f, (\w+)\)", blok)]


def petla():
    """the loop as the C# builds it: fillets of the corners; (arc lengths, straight lengths, the line every 1 m)"""
    C = rogi(); n = len(C); P = [(x, z) for x, z, _, _ in C]
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
        self.assertIn("float lim = przyczepnosc * sila * (bl ? .55f : 1f);", KLAD)                                     # grip from the load
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
        self.assertIn("public float Skret(float v01) => maxSkret * Mathf.Lerp(1f, .3f, Mathf.Clamp01(v01));", KLAD)
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
        self.assertGreater(dl, 2300); self.assertLess(dl, 2700)                                    # 2.5 km: two laps a race
        self.assertGreaterEqual(len(rogi()), 60)                                                   # owner 14:01 "jakieś zakręty"
        # the widths: the start 8 m, the motocross field 7 m (FIM >= 6 m), the woods' trail 5 m (two quads side by side)
        self.assertEqual((const("Start"), const("MX"), const("Las")), (8.0, 7.0, 5.0))
        self.assertGreaterEqual(const("MX"), 6.0); self.assertGreaterEqual(const("Las"), 2 * 1.28 + 1.5)
        C = rogi()
        self.assertEqual([w for _, _, _, w in C[1:32]], [const("Las")] * 31)                      # corners 1-31: the woods
        self.assertTrue(all(w >= const("MX") for _, _, _, w in C[32:]))
        self.assertIn("o.w[i] = sum / (2 * ww + 1);", TOR)                                         # the width eased over 16 m
        # the start straight (the last straight, into corner 0): 120 m+ before the first turn, no obstacle on it
        self.assertGreater(proste[-1], 120)
        self.assertNotRegex(TOR, r"new Przeszkoda\(%d," % (len(C) - 1))
        # rolling waves 8-10 m apart, at most 0.8 m
        self.assertIn("case Typ.Fale: return p.n * 9f;", TOR)
        for h in re.findall(r"Typ\.Fale, \.(\d+)f", TOR):
            self.assertLessEqual(float("." + h), .8)
        # every obstacle on its straight, 6 m clear of the next turn (the builder throws otherwise)
        dlug = {"Stol": None, "Podwojna": 6 + .4 + 5 + 4 + 5 + .4 + 8, "Schodek": 4.5 + 22 + 9, "Gorka": 40 + 8 + 18, "Brod": 18}
        for pr, od, typ, h, n in re.findall(r"new Przeszkoda\((\d+), ([\d.]+)f, Typ\.(\w+), \.?([\d.]+)f(?:, (\d+))?\)", TOR):
            if typ == "Klody": L = (int(n) - 1) * const("KlodaOdstep") + 2
            elif typ == "Bloto": L = int(n)
            else: L = dlug.get(typ) or (int(n) * (9 if typ == "Fale" else 4.5) if typ in ("Fale", "Muldy") else (35 if float(h) >= 1.8 else 23))
            self.assertLess(float(od) + L, proste[int(pr)] - 6, (typ, pr))
        self.assertIn('throw new InvalidOperationException("quad track: obstacle " + i + " runs into a turn");', TOR)

    def test_the_site(self):
        x0, x1, z0, z1 = const("X0"), const("X1"), const("Z0"), const("Z1")
        # west of the region (x < 0), north of the D2 railway hill (ellipse centre -330, 186.5; radii 250, 400)
        self.assertLessEqual(x1, 0.0)
        for x in (x0, (x0 + x1) / 2, x1):
            self.assertGreater(((x + 330) / 250) ** 2 + ((z0 - 186.5) / 400) ** 2, 1.2)
        # the loop inside the fence with room, the fence inside the rim band's end
        inset = const("FenceInset"); pob = const("Pobocze")
        pts = linia(1.0, szer=True)
        self.assertGreater(min(x - w / 2 - pob for x, z, w in pts) - (x0 + inset), 6)
        self.assertGreater((z1 - inset) - max(z + w / 2 + pob for x, z, w in pts), 6)
        self.assertGreater(min(z - w / 2 - pob for x, z, w in pts) - (z0 + inset), 6)
        self.assertGreater(inset, const("RimBand"))
        # the sections side by side: their shoulders 4 m+ apart in the woods (the trees between them, as on his photo), 8 m+
        # on the field (two berms' fills); points more than 40 m apart along the loop
        s = [0.0]
        for (xa, za, _), (xb, zb, _) in zip(pts, pts[1:]): s.append(s[-1] + math.hypot(xb - xa, zb - za))
        L = s[-1] + math.hypot(pts[0][0] - pts[-1][0], pts[0][1] - pts[-1][1])
        for i in range(0, len(pts), 2):
            xa, za, wa = pts[i]
            for j in range(i + 1, len(pts), 2):
                ds = abs(s[i] - s[j]); ds = min(ds, L - ds)
                if ds < 40: continue
                xb, zb, wb = pts[j]
                gap = math.hypot(xa - xb, za - zb) - wa / 2 - wb / 2 - 2 * pob
                self.assertGreater(gap, 8 if wa >= const("MX") and wb >= const("MX") else 4, (xa, za, xb, zb))
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
        # no flicker: thin layers at least 4 cm proud (the finish band, the club's plaza, the gate's pad), the skirts under every edge
        self.assertIn("my + .04f, my + .07f", TOR); self.assertIn("b.BoxMinMax(granit, PlacX0, PlacX1, y - .08f, yp, PlacZ0, PlacZ1);", KLUB); self.assertIn("gy - .2f, gy + .08f", TOR)
        self.assertIn("vT.Add(top - Vector3.up * .6f * K)", TOR)

    def test_paddock_and_timer(self):
        for s in ('"WYPOŻYCZALNIA KŁADÓW"', '"BRAXHORN"', '"BRAXHORN QUAD CROSS"'):
            self.assertIn(s, TOR)
        self.assertIn('"BRAXHORN QUAD CLUB"', KLUB)
        self.assertIn("BraxChampionsKlady.Postaw(g, f.model, f.kolor, f.napis, miejsca[i] * K, 180f, czas, wolne, jazda);", KLUB)
        self.assertIn("BraxChampionsKlubKladow.Zbuduj(g, b, y, czas, wolne, jazda);", TOR)
        self.assertIn("czas.punkty = pkt.ToArray();", TOR)
        self.assertIn("bool przezMete = indeks > n - 8 && i < 8;", CZAS)
        self.assertIn("if (liczy && zaliczone >= n * .9f)", CZAS)                                  # no lap by a short cut
        self.assertIn('const string Klucz = "BRAX_TOR_KLADOW_NAJLEPSZE";', CZAS)

    def test_textures_poly_haven_4k(self):
        prep = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        fx = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
        for slug in ("muddy_tracks", "brown_mud_02", "gravel_ground_01", "aerial_mud_1"):
            self.assertIn("'" + slug + "'", prep)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("/4k/", json.dumps(fx[slug][key]["4k"]))
            self.assertIn('"' + slug + '"', TOR)
        # from the owner's list where something fits: the booth's dark timber
        self.assertIn('"dark_paneled_wood"', TOR)


def linia(krok=1.0, szer=False):
    """the centre line as the C# lays it out: the straights between the fillets' tangent points and the fillet arcs (with
    szer: each point's riding width, the width of the corner it comes from)"""
    pts = linia_w(krok)
    return pts if szer else [(x, z) for x, z, _ in pts]


def linia_w(krok=1.0):
    C = rogi(); n = len(C); P = [(x, z) for x, z, _, _ in C]; pts = []
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
        pts += [(o[0] + R * math.cos(a1 + da * k / m), o[1] + R * math.sin(a1 + da * k / m), C[i][3]) for k in range(m + 1)]
        # the straight on to the next corner's fillet
        e, f2 = c, P[(i + 2) % n]; R2 = C[(i + 1) % n][2]
        g1 = (e[0] - b[0], e[1] - b[1]); lg = math.hypot(*g1); g1 = (g1[0] / lg, g1[1] / lg)
        g2 = (f2[0] - e[0], f2[1] - e[1]); lh = math.hypot(*g2); g2 = (g2[0] / lh, g2[1] / lh)
        t2 = R2 * math.tan(math.acos(max(-1, min(1, g1[0] * g2[0] + g1[1] * g2[1]))) / 2)
        q = (e[0] - g1[0] * t2, e[1] - g1[1] * t2); L = math.dist(p2, q); m = max(1, int(L / krok))
        pts += [(p2[0] + (q[0] - p2[0]) * k / m, p2[1] + (q[1] - p2[1]) * k / m, C[i][3]) for k in range(m + 1)]
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


class Wyscig(unittest.TestCase):
    """owner 24.11 12:57: "wyścig z rywalami" - 4 AI quads, the gate, 3 laps, positions, the LED board"""

    def test_scripts_and_metas(self):
        for f in (RT / "BraxChampionsWyscig.cs", RT / "BraxChampionsKladRywal.cs", ED / "BraxChampionsKladZawodnik.cs"):
            meta = Path(str(f) + ".meta").read_text(encoding="utf-8")
            self.assertIn("MonoImporter:", meta)
            self.assertRegex(meta, r"guid: [0-9a-f]{32}\n")
        self.assertIn("public sealed class BraxChampionsWyscig : MonoBehaviour", WYSCIG)
        self.assertIn("public sealed class BraxChampionsKladRywal : MonoBehaviour", RYWAL)
        # the AI's hands before the quad's physics step, the race before both
        self.assertIn("[DefaultExecutionOrder(5090)]", RYWAL); self.assertIn("[DefaultExecutionOrder(5100)]", KLAD)
        self.assertIn("[DefaultExecutionOrder(5080)]", WYSCIG)

    def test_rivals_ride_the_same_physics(self):
        # the rival's quad: the same BraxChampionsKlad, its inputs from the AI, never offered to the player
        self.assertIn("if (gracz || Jedzie || tylkoRywal) return \"\";", KLAD)
        self.assertIn("public void Wejscie(float g, float b, float s)", KLAD)
        self.assertIn("if (!gracz) { if (Rywal) { Kierownica(); Silnik(); } return; }", KLAD)
        self.assertIn("if (!gracz) { lot = ileNaZiemi == 0 ? lot + dt : 0f; trik = Trik.Brak; return; }", KLAD)   # no tricks, no record
        self.assertIn("root.GetComponent<BraxChampionsKlad>().tylkoRywal = true;", KLADY)
        # pure pursuit of its lane's point ahead, the speed of the line ahead times its skill (a double's own speed kept)
        self.assertIn("float delta = Mathf.Atan(2f * rozstawOsi * Mathf.Sin(alfa) / dl) * Mathf.Rad2Deg;", RYWAL)
        self.assertIn("float vT = w.vCel[jv] * (w.skok[jv] ? 1f : umiejetnosc);", RYWAL)
        self.assertIn("if (!k.NaZiemi) g = b = 0f;", RYWAL)
        self.assertIn("if (przewrot > 1.5f || stoi > 3f || zgubiony > 3f) { Wroc(); return; }", RYWAL)

    def test_the_four_rivals(self):
        blok = TOR.split("public static readonly (string model, string kolor, string imie, int numer, Color stroj, Color akcent, float umiejetnosc, float pas)[] Rywale =")[1].split("};")[0]
        r = re.findall(r'\("(\w+)", "(\w+)", "(\w+)", (\d+), new Color\([^)]*\), new Color\([^)]*\), ([\d.]+)f, (-?[\d.]+)f\)', blok)
        self.assertEqual(len(r), 4)
        self.assertEqual(sorted(x[0] for x in r), ["rancher", "rancher", "raptor", "raptor"])
        for model, kolor, imie, numer, umiej, pas in r:
            self.assertIn('("%s", "%s",' % (model, kolor), KLADY)                                 # a paint of the owner's pack
            self.assertTrue(.85 <= float(umiej) <= .97)
            self.assertLessEqual(abs(float(pas)) + 1.28 / 2, const("Szer") / 2 - .5)             # its lane inside the track
            # its lane drawn in where the track narrows (the field 7 m, the woods' trail 5 m): still inside, quads apart
            for w in (const("Start"), const("MX"), const("Las")):
                hw = w / 2; skala = min(1.0, max(0.0, (hw - .8) / 3.2)); granica = max(.3, hw - .9)
                self.assertLessEqual(min(abs(float(pas)) * skala, granica) + 1.28 / 2, hw - .2, (imie, w))
        self.assertIn("float hw = w.szer.Length == n ? w.szer[(Idx + 4) % n] * .5f : 4f, skala = Mathf.Clamp01((hw - .8f) / 3.2f), granica = Mathf.Max(.3f, hw - .9f);", RYWAL)
        self.assertIn("pasTeraz = Mathf.MoveTowards(pasTeraz, Mathf.Clamp(cel * skala, -granica, granica), 1.4f * dt);", RYWAL)
        self.assertIn("public float[] szer = new float[0];", WYSCIG)
        # each on his own side, as his place in the gate: places 1, 2, 4, 5 (u = 3.2, 1.6, -1.6, -3.2 m left)
        sloty = [3.2, 1.6, -1.6, -3.2]
        for (model, kolor, imie, numer, umiej, pas), u in zip(r, sloty):
            self.assertEqual(math.copysign(1, float(pas)), math.copysign(1, u))
        self.assertIn("public const int SlotGracza = 2;", WYSCIG)

    def test_the_gate_and_the_race(self):
        # five bars on hinges: lying forward on the pad without a race (the track is ridden through), up for the start
        self.assertIn("w.belkaGora = Quaternion.identity; w.belkaDol = Quaternion.Euler(0f, 0f, 90f);", TOR)
        self.assertIn("t.localRotation = w.belkaDol; w.belki[k] = t; Belek++;", TOR)
        self.assertIn("bb => bb.BoxMinMax(mat, -.03f, .03f, .005f, .425f, -hz, hz)", TOR)          # 0.12 + 0.425: FIM's ~50 cm
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);", TOR)
        # 3 - 2 - 1 and the drop; two laps of the 2.5 km loop from the gate; the order by the distance ridden along the line,
        # followed from where each rider was (a cut gains nothing)
        self.assertIn("if (t >= 3.6f) Puszczaj();", WYSCIG)
        self.assertIn("public const int Okrazen = 2;", TOR)
        self.assertIn("public float Dystans => sMety + Okr * dlugosc;", WYSCIG)
        self.assertIn("int Okr => puchar ? okrazenPucharu : okrazen;", WYSCIG)
        self.assertIn("for (int k = -4; k <= 12; k++)", WYSCIG)
        self.assertIn("static bool Przed(Uczestnik a, Uczestnik b) => a.meta ? (!b.meta || a.czas < b.czas) : (!b.meta && a.dystans > b.dystans);", WYSCIG)
        self.assertIn("if (!ja.klad || BraxChampionsKlad.Jedzie != ja.klad) { Przerwij(); break; }", WYSCIG)
        self.assertIn('const string KluczRekordu = "BRAX_TOR_WYSCIG_REKORD";', WYSCIG)
        # the gate's places behind the bars: the quad's front tyre (axle 0.67 m + tyre 0.30 m ahead of its root) clear of them
        self.assertIn("w.sloty[k] = new Vector3(BramkaX + 1.25f, gy + .08f, zc) * K;", TOR)
        self.assertGreater(1.25 - .09, .67 + .31)

    def test_speed_profile_and_doubles(self):
        # a corner: v^2 = g R (mu + tan b) / (1 - mu tan b) with the dirt's 0.7 and the berm's 13 deg; the straights 83 km/h
        self.assertEqual(const("MuWyscigu"), .7); self.assertEqual(const("VProsta"), 23.0)
        self.assertLess(const("VProsta") * 3.6, 105 * .97)
        self.assertIn("v = Mathf.Min(v, Mathf.Sqrt(9.81f * (MuWyscigu + tb) / Mathf.Max(.2f, 1f - MuWyscigu * tb) / kr));", TOR)
        # a double's speed from its take-off's lip angle, landing 4 m down the far side (the gap and the top cleared)
        for h in (2.0, 1.5):
            kat = math.atan(h / 6 / (1 - .15))
            v = math.sqrt(9.81 * 18.8 / math.sin(2 * kat))
            self.assertGreater(v * v * math.sin(2 * kat) / 9.81, 14.4); self.assertLess(v * v * math.sin(2 * kat) / 9.81, 22.8)
            self.assertLess(v, 75 / 3.6 * .97)                                                   # the Rancher gets there too
        self.assertIn("bool skacze = r[kf] >= VSkoku(q) - .5f;", TOR)                              # else rolled over
        self.assertEqual(const("VToczenie"), 7.0)

    def test_rider_reaches_the_bars_and_the_footrests(self):
        L = {n: const(n, ZAWODNIK) for n in ("Udo", "Golen", "Ramie", "Przedramie", "Pochylenie")}
        self.assertIn("? (new Vector3(0f, .808f, -.019f), new Vector3(.44f, .99f, .366f), new Vector3(.34f, .30f, .03f))", ZAWODNIK)
        self.assertIn(": (new Vector3(0f, .901f, -.135f), new Vector3(.44f, 1.135f, .216f), new Vector3(.36f, .40f, -.02f));", ZAWODNIK)
        wym = {"raptor": ((0, .808, -.019), (.44, .99, .366), (.34, .30, .03)), "rancher": ((0, .901, -.135), (.44, 1.135, .216), (.36, .40, -.02))}
        for model, (siodlo, chwyt, podnozek) in wym.items():
            pelvis = (siodlo[0], siodlo[1] + .09, siodlo[2] - .03)
            a = math.radians(L["Pochylenie"]); neck = (0, pelvis[1] + .52 * math.cos(a), pelvis[2] + .52 * math.sin(a))
            bark = (.2, neck[1] - .075 * math.cos(a) + .02 * math.sin(a), neck[2] - .075 * math.sin(a) - .02 * math.cos(a))
            dlon = (chwyt[0], chwyt[1] + .02, chwyt[2])
            reka = math.dist(bark, dlon)
            self.assertLess(reka, (L["Ramie"] + L["Przedramie"]) * .95, model)                    # bent at the elbow, not stretched
            self.assertGreater(reka, abs(L["Ramie"] - L["Przedramie"]) + .05, model)
            biodro = (.1, pelvis[1], pelvis[2]); kostka = (podnozek[0], podnozek[1] + .07, podnozek[2])
            noga = math.dist(biodro, kostka)
            self.assertLess(noga, (L["Udo"] + L["Golen"]) * .95, model); self.assertGreater(noga, .3, model)
        self.assertIn("foreach (var tr in t.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);", ZAWODNIK)

    def test_led_board(self):
        self.assertIn("public const int TW = 192, TH = 96;", WYSCIG)
        self.assertAlmostEqual(const("TablicaW") / const("TablicaH"), 192 / 96)                  # square LEDs
        # the board's font has the marks it prints (BraxChampionsPiksele): no '~'
        znaki = re.search(r'const string Znaki = "([^"]*)";', (RT / "BraxChampionsPiksele.cs").read_text(encoding="utf-8")).group(1)
        for t in ("WYŚCIG BRAXHORN", "3 OKRĄŻENIA · 4 RYWALI", "PODJEDŹ KŁADEM", "POD MASZYNĘ STARTOWĄ", "REKORD 6:15.0", "1. SZYMON   6:12.4*", "OKR 2/3", "META"):
            for c in t:
                self.assertIn(c, znaki, t)
        self.assertIn('BraxChampionsTorCzas.Czas(kolejnosc[i].czas) + (kolejnosc[i].szac ? "*" : "")', WYSCIG)
        # the screen faces the grandstand (north), its picture the right way round from there
        self.assertIn("be.QuadUV(ekran, new Vector3(x1, yb, zs), new Vector3(x1, yt, zs), new Vector3(x0, yt, zs), new Vector3(x0, yb, zs),", TOR)


class LasyIBloto(unittest.TestCase):
    """owner 24.11 12:57 his pick "Boczne linie i błoto" (the mud, the dust); 14:01-14:20 "w jakichś błotach albo po jakichś
    lasach", "lasy, górki", "więcej lasu": the woods' trail over the hill, its logs, mud and creek"""

    def przeszkody(self):
        return [(int(a), float(b), c, float(d), int(n) if n else 0) for a, b, c, d, n in re.findall(r"new Przeszkoda\((\d+), ([\d.]+)f, Typ\.(\w+), ([\d.]+)f(?:, (\d+))?\)", TOR)]

    def test_the_wooded_hill(self):
        gx, gz, gh, gr = const("GoraX"), const("GoraZ"), const("GoraH"), const("GoraR")
        self.assertEqual((gh, gr), (18.0, 150.0))
        self.assertIn("return GoraH * .5f * (1f + Mathf.Cos(Mathf.PI * d / GoraR)) * Smooth(0f, RimBand + 12f, DoKrawedzi(x, z));", TOR)
        self.assertIn("public static float Naturalna(float x, float z) => BraxChampionsHorizon.FarHeight(x, z) + Gora(x, z);", TOR)
        # the woods' corners on the hill (its slopes and its top), the field's off it
        C = rogi()
        for x, z, r, w in C[1:32]:
            self.assertLess(math.hypot(x - gx, z - gz), gr + 15, (x, z))
        # the track climbs it with the land (the base smoothed over 50 m: a grade a quad rides, no wall)
        self.assertIn("baza = new float[N]; int w = 50;", TOR)
        # the slope: steepest 18 * pi / 2 / 150 = 19 %, the trail's switchbacks across it
        self.assertLess(gh * math.pi / 2 / gr, .2)

    def test_logs_mud_and_the_creek_in_the_woods(self):
        p = self.przeszkody()
        lesne = [q for q in p if q[0] <= 31]
        self.assertEqual(sorted({q[2] for q in lesne}), ["Bloto", "Brod", "Fale", "Klody"])
        # logs: 0.4 m pine trunks 2.4 m apart on their dirt ramps, at most 3 in a row, ridden at a walk (5 m/s)
        self.assertEqual((const("KlodaOdstep"), const("KlodaR")), (2.4, .2))
        for q in p:
            if q[2] == "Klody": self.assertLessEqual(q[4], 3); self.assertLessEqual(q[3], .4)
        self.assertIn("case Typ.Klody: if (t > -8f && t < q.dl) v = Mathf.Min(v, 5f); break;", TOR)
        self.assertIn('kora = BraxChampionsFoundation.SourceMaterial("tor_kladow_kora", "pine_bark", 2f, Color.white);', TOR)
        # the creek's ford: 0.8 m dip, the water 0.42 m deep
        self.assertIn("public const float WodaGlebokosc = .42f;", TOR)

    def test_ribbon_textures_at_real_size(self):
        # the riding surface's UVs in metres: the ruts (muddy_tracks, 2.25 m on Poly Haven) and the mud (aerial_mud_1, 8 m)
        self.assertIn("uvT.Add(new Vector2(u, s));", TOR)
        self.assertNotIn("u / 2.25f", TOR)
        self.assertIn('SourceMaterial("tor_kladow_koleiny", "muddy_tracks", 2.25f,', TOR)
        self.assertIn('SourceMaterial("tor_kladow_bloto", "aerial_mud_1", 8f,', TOR)

    def test_mud_pits(self):
        mud = [q for q in self.przeszkody() if q[2] == "Bloto"]
        self.assertEqual(len(mud), 2)                                                              # the hole on the climb, the big pit
        _, proste, _ = petla()
        for prosta, od, _, h, n in mud:
            self.assertLessEqual(prosta, 31)                                                       # both in the woods
            self.assertLess(od + n, proste[prosta] - 6); self.assertLessEqual(h, .35)
        self.assertIn("case Typ.Bloto: return p.n > 0 ? p.n : 26f;", TOR)
        # the quad in it: the tyre holds 55 %, the mud drags at 0.25 of the load; the rivals slow to 29 km/h for it
        self.assertIn("float lim = przyczepnosc * sila * (bl ? .55f : 1f);", KLAD)
        self.assertIn("if (bl) wzdluz -= Mathf.Clamp(vLon * masa * .25f / dt, -.25f * sila, .25f * sila);", KLAD)
        self.assertIn("case Typ.Bloto: if (t > -10f && t < q.dl) v = Mathf.Min(v, 8f); break;", TOR)
        self.assertIn("public static bool WBlocie(Vector3 p)", BLOTO)

    def test_dust_and_mud_particles(self):
        # no particle module in the project: soft billboards in one mesh per kind
        self.assertNotIn("ParticleSystem", BLOTO)
        self.assertIn("pPyl = new Pula(160); pBloto = new Pula(140);", BLOTO)
        self.assertIn("if (i >= 2) Wyrzut(hit.point, vLon, bl, wf);", KLAD)
        self.assertIn("foreach (var m in new[] { c.pyl, c.grudki }) { m.SetFloat(\"_VertexTint\", 1f); EditorUtility.SetDirty(m); }", TOR)
        self.assertIn("if (mokre) q.v[i] += Vector3.down * (9.81f * K * dt);", BLOTO)            # the clumps on the world's gravity
        meta = (RT / "BraxChampionsBloto.cs.meta").read_text(encoding="utf-8")
        self.assertRegex(meta, r"guid: [0-9a-f]{32}\n")


def png(path):
    """(width, height, rows of RGBA bytes) of an 8-bit RGBA PNG (no PIL in the checks)"""
    import struct
    import zlib
    b = path.read_bytes(); assert b[:8] == b"\x89PNG\r\n\x1a\n"
    i, idat, w = 8, b"", 0
    while i < len(b):
        n, typ = struct.unpack(">I4s", b[i:i + 8]); d = b[i + 8:i + 8 + n]; i += 12 + n
        if typ == b"IHDR": w, h, depth, ctype = struct.unpack(">IIBB", d[:10]); assert (depth, ctype) == (8, 6)
        elif typ == b"IDAT": idat += d
    raw = zlib.decompress(idat); st = w * 4; rows = []; prev = bytearray(st); p = 0
    for _ in range(h):
        f = raw[p]; line = bytearray(raw[p + 1:p + 1 + st]); p += 1 + st
        for x in range(st):
            a = line[x - 4] if x >= 4 else 0; up = prev[x]; c = prev[x - 4] if x >= 4 else 0
            if f == 1: line[x] = (line[x] + a) & 255
            elif f == 2: line[x] = (line[x] + up) & 255
            elif f == 3: line[x] = (line[x] + (a + up) // 2) & 255
            elif f == 4:
                pa, pb, pc = abs(up - c), abs(a - c), abs(a + up - 2 * c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else up if pb <= pc else c)) & 255
        rows.append(bytes(line)); prev = line
    return w, h, rows


class Las(unittest.TestCase):
    """owner 24.11 14:20 "Tak, ale więcej lasu": the forest of Poly Haven's scanned pines and firs, as crossed cards"""

    def test_the_cards_from_the_real_scans(self):
        self.assertIn('public const string Atlas = "Assets/Source/Mistrzowie/Las/las_atlas.png";', LAS)
        karty = re.findall(r'new Karta\("(\w+)", ([\d.]+)f, ([\d.]+)f, (-?[\d.]+)f, (\d+)\)', LAS)
        self.assertEqual([k[0] for k in karty], ["sosna_a", "sosna_b", "sosna_c", "jodla_a", "jodla_b", "jodla_c", "mloda_jodla", "mloda_sosna"])
        self.assertEqual([int(k[4]) for k in karty], list(range(0, 16, 2)))                        # each tree two cells: 0 and 90 deg
        for nazwa, wys, ramka, stopa, _ in karty:
            wys, ramka = float(wys), float(ramka)
            self.assertGreaterEqual(ramka, wys); self.assertLess(ramka, wys * 1.4)                 # the tree fills its picture
            if nazwa.startswith("mloda"): self.assertLess(wys, 3)
            else: self.assertTrue(14 <= wys <= 21, nazwa)                                          # real grown pines and firs
        # the picture half as wide as it is tall (512 x 1024), centred on the trunk
        self.assertIn("float h = ka.ramka * d.skala, hw = h * .25f, y0 = d.y + ka.stopa * d.skala;", LAS)
        credits = (ATLAS.parent / "CREDITS.md").read_text(encoding="utf-8")
        for m in ("pine_tree_01", "fir_tree_01", "fir_sapling", "pine_sapling_small"):
            self.assertIn("https://polyhaven.com/a/" + m, credits)
        self.assertIn("CC0", credits)
        # the tools that made it: the needles' own alpha (Poly Haven's glTF leaves it out), no mipmaps in the render, the
        # cut-out's coverage kept in the atlas
        pob = (TOOLS / "las/pobierz_drzewo.py").read_text(encoding="utf-8")
        self.assertIn('twig = next(k[:-len("_alpha")] for k in files if k.endswith("_alpha"))', pob)
        self.assertIn('m["alphaMode"] = "MASK"', pob)
        self.assertIn("t.generateMipmaps = false; t.minFilter = THREE.LinearFilter;", (TOOLS / "las/wypal_drzewa.js").read_text(encoding="utf-8"))
        atl = (TOOLS / "las/atlas_lasu.py").read_text(encoding="utf-8")
        self.assertIn("def pokrycie(alpha_duze, alpha):", atl); self.assertIn("def wypelnij(rgba):", atl)

    def test_the_atlas(self):
        w, h, rows = png(ATLAS)
        self.assertEqual((w, h), (4096, 2048))
        for c in range(16):
            x0, y0 = (c % 8) * 512, (c // 8) * 1024
            krycie = sum(1 for y in range(y0, y0 + 1024, 8) for x in range(x0, x0 + 512, 8) if rows[y][x * 4 + 3] > 127) / (128 * 64)
            self.assertTrue(.05 < krycie < .35, (c, krycie))                                      # a tree, not a sheet
            # its trunk's foot at the bottom middle of its cell
            stopa = [rows[y0 + 1023 - dy][(x0 + 256 + dx) * 4 + 3] for dy in range(2, 24) for dx in range(-24, 25)]
            self.assertGreater(max(stopa), 200, c)
            # the colour under the transparent texels pushed out (no black fringe at any mip level)
            przez = [rows[y][x * 4:x * 4 + 3] for y in range(y0, y0 + 1024, 32) for x in range(x0, x0 + 512, 32) if rows[y][x * 4 + 3] == 0]
            self.assertGreater(sum(sum(p) for p in przez) / max(1, 3 * len(przez)), 25, c)

    def test_material_and_import(self):
        for s in ('m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0f);', "m.renderQueue = 2450;",
                  'm.SetFloat("_VertexTint", 1f);', "r.shadowCastingMode = ShadowCastingMode.On;"):
            self.assertIn(s, LAS)
        for s in ("t.mipMapsPreserveCoverage = true;", "t.alphaTestReferenceValue = .5f;", "android.format = TextureImporterFormat.ASTC_6x6;",
                  '"Assets/Source/Mistrzowie/Las/"', "t.wrapMode = UnityEngine.TextureWrapMode.Clamp;"):
            self.assertIn(s, LAS_IMPORT)
        for f in ("BraxChampionsLasKladow.cs", "BraxChampionsLasTextureImport.cs"):
            meta = (ED / (f + ".meta")).read_text(encoding="utf-8")
            self.assertIn("MonoImporter:", meta); self.assertRegex(meta, r"guid: [0-9a-f]{32}\n")
        # one mesh per 60 m square (one draw call each), its trunks' capsules with it; the young trees none
        self.assertIn("public const float Oczko = 4.6f, OczkoMlodych = 3.2f, Kwadrat = 60f", LAS)
        self.assertIn("var cc = pk.AddComponent<CapsuleCollider>(); cc.direction = 1;", LAS)
        self.assertIn("if (ka.Mloda) Mlodych++; else { Drzew++; p.pnie.Add(d); }", LAS)
        self.assertIn("var rnd = new System.Random(20261124);", LAS)                               # the same forest every build

    def test_where_it_stands(self):
        # the hill, the wall round the site (not on the city's side), the woods north-west of the hill, the field's clumps and rows
        for s in ("float gora = 1f - Smooth(T.GoraR * .9f, T.GoraR * 1.1f, dh);", "float sciana = (1f - Smooth(T.FenceInset - 2f, T.FenceInset + 12f, kr + brzeg * 14f)) * wschod;",
                  "float kepa = Smooth(.60f, .70f, n) * wschod;", "if (NaPolu(x, z)) d = Mathf.Max(d, PoleGestosc);"):
            self.assertIn(s, LAS)
        # clear of the trail 2.5 m past its shoulders, of the field and the start 6 m; the paddock, the freestyle zone, the road,
        # the LED board, the fence line
        self.assertGreaterEqual(const("OdLesnejSciezki", LAS), 2.0); self.assertGreaterEqual(const("OdToru", LAS), 5.0)
        self.assertGreaterEqual(const("OdFreestyle", LAS), 15.0); self.assertGreaterEqual(const("OdPaddocku", LAS), 10.0)
        for s in ("T.DoPaddocku(x, z) < OdPaddocku", "T.DoFreestyle(x, z) < OdFreestyle", "Mathf.Abs(z - T.DrogaZ) < T.DrogaPol + 8f",
                  "dx * dx + dz * dz < OdTablicy * OdTablicy", "Mathf.Abs(x - fx0) < OdPlotu", "if (e < 1.6f) continue;",
                  "(e < od || los >= d * Mathf.Lerp(.55f, 1f, Smooth(od, od + 3f, e)))"):
            self.assertIn(s, LAS)
        # the trunk's foot sunk by the ground's slope, never floating
        self.assertIn("t.y = T.Grunt(t.x, t.z) - .1f - Mathf.Sqrt(gx * gx + gz * gz) * .35f * Mathf.Max(1f, t.skala);", LAS)

    def test_forest_floor_and_its_textures(self):
        # the grass split again where the forest stands: its needle litter under it (one closed surface with the grass)
        self.assertIn("foreach (var q in vl) pl.Add(BraxChampionsLasKladow.Las(q.x / K, q.z / K) - .45f);", TOR)
        self.assertIn('Zapisz(g, "TOR DO KŁADÓW | grunt: ściółka lasu | kolizja", "tor_kladow_grunt_sciolka", va, nrm, ua, tl, sciolka, true);', TOR)
        self.assertIn('SourceMaterial("tor_kladow_sciolka", "forest_floor", 2.14f,', TOR)
        self.assertIn("BraxChampionsLasKladow.Zbuduj(g);", TOR)
        prep = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        fx = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
        for slug in ("pine_bark", "forest_floor"):
            self.assertIn("'" + slug + "'", prep)
            for key in ("Diffuse", "nor_gl", "Rough", "arm"):
                self.assertIn("/4k/", json.dumps(fx[slug][key]["4k"]))


class Opony(unittest.TestCase):
    """owner 24.11 17:11 "Jakie rampy barierki na zakrętach może?"; his pick: "Opony na zakrętach" and "Ściany z opon"."""

    def poziomy(self):
        import struct
        b = OPONA.read_bytes(); self.assertEqual(b[:4], b"OPN1")
        n, = struct.unpack_from("<i", b, 4); i = 8; wynik = []
        for _ in range(n):
            nv, ni = struct.unpack_from("<ii", b, i); i += 8
            v = [struct.unpack_from("<8f", b, i + k * 32) for k in range(nv)]; i += nv * 32
            t = struct.unpack_from("<%dH" % ni, b, i); i += ni * 2
            wynik.append((v, t))
        self.assertEqual(i, len(b))
        return wynik

    def test_the_scanned_tyre(self):
        lv = self.poziomy()
        self.assertEqual([len(t) // 3 for v, t in lv], [2880, 896])              # the scan, the same simplified (UV seams kept)
        for v, t in lv:
            self.assertLess(max(t), len(v))
            r = [math.hypot(p[0], p[1]) for p in v]; zs = [p[2] for p in v]
            self.assertAlmostEqual(max(r), .3, 2); self.assertLess(max(zs) - min(zs), .17)   # 0.6 m across, 0.165 wide, axis z
            self.assertTrue(all(0 <= p[6] <= 1 and 0 <= p[7] <= 1 for p in v))
        self.assertIn("https://polyhaven.com/a/old_tyre", (OPONA.parent / "CREDITS.md").read_text(encoding="utf-8"))
        self.assertIn('SourceMaterial("tor_kladow_opona", "old_tyre", 1f, Color.white)', OPONY)   # its own 4K maps on its own UVs
        prep = (TOOLS / "prepare_assets.py").read_text(encoding="utf-8")
        fx = json.loads((TOOLS / "pbr-provider-preflight.json").read_text(encoding="utf-8"))
        self.assertIn("'old_tyre'", prep)
        for key in ("Diffuse", "nor_gl", "Rough", "arm"):
            self.assertIn("/4k/old_tyre/", json.dumps(fx["old_tyre"][key]["4k"]))
        tool = (TOOLS / "opony/opona_lod.py").read_text(encoding="utf-8")
        self.assertIn("1.0 - t[1]", tool)                                          # glTF's v flipped for Unity

    def test_levels_for_a_phone(self):
        # a bundle: the scan close up, the simplified one, a 10-sided ring; a wall starts at the simplified one; 28 a group
        self.assertIn("static readonly int[] PoziomyPiramidy = { 0, 1, 2 }, PoziomySciany = { 1, 2 };", OPONY)
        self.assertIn("lista.Add(Pierscien(10));", OPONY)
        self.assertIn("NaGrupe = 28f;", OPONY)
        self.assertLess(28 * 2090, 65536)                                          # 16-bit indices
        self.assertIn("var lg = go.AddComponent<LODGroup>();", OPONY)
        self.assertIn('go => go.name.EndsWith(" LOD1") || go.name.EndsWith(" LOD2")', (TOOLS / "tor_preview/TorMain.cs").read_text(encoding="utf-8"))

    def test_bundles_and_walls_where_real_tracks_put_them(self):
        # the apex of each turn of 35 deg+: 3-2-1 on the field and the start (60 deg+), 2-1 in the woods; past the stakes
        self.assertIn("bool duza = o.w[sz] >= 6f && kat >= 60f;", OPONY)
        self.assertIn("public const float MinKatPiramidy = 35f, MinKatSciany = 90f, MinVWjazdu = 12.5f, MinHamowanie = 4f;", OPONY)
        self.assertGreater(const("OdPobocza", OPONY), .5 + .1)                     # the stakes stand 0.5 m past the shoulder
        # the walls: the fast turns' outsides (and the start's first turn), the sections closer than 10 m side by side
        self.assertIn("float sg = -Mathf.Sign(o.kr[sz]);", OPONY)
        self.assertIn("bool start = k == pierwszy;", OPONY)
        self.assertIn("if (jb < i || gap > MaxSzczelina || gap < 1.4f) { Zamknij(); continue; }", OPONY)
        self.assertEqual((const("MaxSzczelina", OPONY), const("MinWzdluz", OPONY)), (10.0, 80.0))
        # two rows, the top one nested (0.6 sin 60), a box collider each 5 tyres and each bundle
        self.assertAlmostEqual(const("Wiersz", OPONY), .6 * math.sin(math.radians(60)), 2)
        self.assertEqual(OPONY.count("W.Kolizja("), 2)
        self.assertIn("BraxChampionsOponyKladow.Zbuduj(g);", TOR)
        self.assertLess(TOR.index("BraxChampionsOponyKladow.Zbuduj(g);"), TOR.index('W.Zapisz("mistrzowie_tor_kladow");'))
        meta = (ED / "BraxChampionsOponyKladow.cs.meta").read_text(encoding="utf-8")
        self.assertRegex(meta, r"guid: [0-9a-f]{32}\n")

    def test_the_tape_in_a_picture(self):
        # the stakes recorded (a break where the row stops); the tape between neighbours 0.6 m up, sagging 4 cm
        self.assertIn("public static readonly List<Vector3>[] Paliki", TOR)
        self.assertIn("if (float.IsNaN(a.x) || float.IsNaN(c.x)) continue;", OPONY)
        self.assertEqual((const("TasmaH", OPONY), const("TasmaZwis", OPONY)), (.6, .04))
        # a thin bright line as geometry flickers (ZASADA-MIGOTANIE 3.1): a 6 cm band in a transparent, mipmapped picture
        self.assertIn("c.a = (byte)(y >= 12 && y < 20 ? 255 : 0);", OPONY)
        self.assertIn("imp.mipmapEnabled = true; imp.wrapMode = TextureWrapMode.Repeat;", OPONY)
        self.assertIn('BraxChampionsHarbour.Surface("tor_kladow_tasma", Color.white, .55f, 0f, null, true);', OPONY)
        self.assertGreater(const("TasmaPas", OPONY), .2)                           # the quad: 4 times the band


class KlubKladow(unittest.TestCase):
    """owner 24.11 19:31 "A tak trochę tak bardziej luksusowo zrobić tam ... koło tego wypożyczalni"; his pick: "Kłady na
    podestach" and "Taras nad torem"."""

    def test_quads_on_podiums_one_turning(self):
        # six podiums: five in a row, the lime Raptor in front on a bigger one; a quad rides off 28 cm as off a kerb
        self.assertEqual((const("PodestR", KLUB), const("PodestH", KLUB), const("ObrotR", KLUB), const("ObrotH", KLUB)), (1.9, .28, 2.3, .32))
        self.assertIn("public const int Bohater = 3;", KLUB)
        rozstaw, r = const("Rozstaw", KLUB), const("PodestR", KLUB)
        self.assertGreater(rozstaw - 2 * r, 1.0)                                                   # room between podiums
        self.assertGreater(const("RzadZ", KLUB) - r - (const("ObrotZ", KLUB) + const("ObrotR", KLUB)), 2.5)   # the hero's quad can turn out
        # the quad (2.03 x 1.24 m) stays on its podium's collider (the square inside the disc) as it turns: half its diagonal
        self.assertLess(math.hypot(2.03, 1.24) / 2, r * 1.41 / 2)
        self.assertIn("W.Kolizja(new Vector3(c.x, yp + h * .5f, c.z) * K, new Vector3(r * 1.41f, h, r * 1.41f) * K, Quaternion.identity);", KLUB)
        # the turntable: only while parked on it, nobody riding, a showroom's 40 s a turn
        self.assertIn("if (!klad || !rb || !rb.isKinematic || klad.Rywal || BraxChampionsKlad.Jedzie == klad) return;", OBROTNICA)
        self.assertIn("public float stopniNaSekunde = 9f;", OBROTNICA)
        self.assertRegex((RT / "BraxChampionsObrotnica.cs.meta").read_text(encoding="utf-8"), r"guid: [0-9a-f]{32}\n")
        # the canopy: its timber underside below the black slab (1 cm apart: nothing coplanar), BRAXHORN QUAD CLUB in gold
        self.assertIn("b.BoxMinMax(deski, WiataX0 + .1f, WiataX1 - .1f, yw - .06f, yw - .01f, WiataZ0 + .1f, WiataZ1 - .1f);", KLUB)
        self.assertIn('SourceMaterial("tor_kladow_klub_granit", "granite_tile_04", 2f,', KLUB)
        self.assertLess(const("PlacZ1", KLUB), 995.9)                                              # clear of the booth's plinth
        self.assertNotRegex(KLUB, r"\bT\.led\b|Emission")                                         # nothing glows

    def test_vip_terrace(self):
        # 2.6 m up over the start gate (x -72), in the paddock, clear of the grandstand and the riders' park
        tx0, tx1, tz0, tz1 = (const(n, KLUB) for n in ("TX0", "TX1", "TZ0", "TZ1"))
        self.assertTrue(tx0 < const("BramkaX") < tx1)
        self.assertGreaterEqual(tz0, const("PZ0")); self.assertGreater(tx0 - 4.5, -108)            # the grandstand ends at x -108
        self.assertLess(tz1, const("ParkZ") - 4)
        self.assertIn("foreach (float px in new[] { -98f, -42f })", TOR)                           # the middle banner gave way
        # the balustrade 1.1 m (the building code's for a terrace), glass with a teak handrail; the stairs 16 x 16.25 = 2.6 m
        self.assertEqual(const("Balustrada", KLUB), 1.1)
        self.assertAlmostEqual(int(re.search(r"public const int Stopni = (\d+);", KLUB).group(1)) * const("Stopien", KLUB), const("TH", KLUB), 3)
        st, bg = const("Stopien", KLUB), const("Bieg", KLUB)
        self.assertTrue(.60 <= 2 * st + bg <= .65)                                                 # a comfortable step (2 rises + 1 tread)
        self.assertIn("W.Kolizja(srodekRampy * K, new Vector3(SchodyZ1 - SchodyZ0, .1f, dlug) * K, obr);", KLUB)
        # outdoor lounge furniture at real sizes; the owner's bouclé; four planters with young pines (the forest's cards)
        self.assertIn('SourceMaterial("tor_kladow_klub_boucle", "curly_teddy_natural", .336f, Color.white);', KLUB)
        self.assertIn("b.Box(teak, P(0f, .2f, 0f), new Vector3(len, .16f, .85f), q);", KLUB)
        self.assertIn("b.BoxMinMax(marmur, x - .35f, x + .35f, yd + 1.03f, yd + 1.06f, z - .35f, z + .35f);", KLUB)
        self.assertIn("BraxChampionsLasKladow.Dodatkowe.Add(", KLUB)
        self.assertIn("var lista = new List<Drzewo>(Drzewa()); lista.AddRange(Dodatkowe);", LAS)
        self.assertIn("BraxChampionsLasKladow.Dodatkowe.Clear();", KLUB)


def ints(name, src):
    return [int(v) for v in re.search(r"\b" + name + r"\s*=\s*\{([^}]*)\}", src).group(1).replace(" ", "").split(",") if v]


def tablica(name, src):
    return [float(v.strip().rstrip("f")) for v in re.search(r"\b" + name + r"\s*=\s*\{([^}]*)\}", src).group(1).split(",") if v.strip()]


def pole(name, src):
    return int(re.search(r"\b" + name + r"\s*=\s*(-?\d+)\b", src).group(1))


class TablicaPucharPodium(unittest.TestCase):
    """owner 24.11 20:31 "Tablica z najlepszymi wynikami ... na turniej albo coś takiego?" - his picks "Tablica TOP 10",
    "Turniej z rywalami", "Podium i puchar"; "Nie do jazdy w nocy" (daytime only)"""

    def test_scripts_and_metas(self):
        for f in (RT / "BraxChampionsWynikiKladow.cs", RT / "BraxChampionsTablicaTop.cs", ED / "BraxChampionsPodiumKladow.cs"):
            meta = Path(str(f) + ".meta").read_text(encoding="utf-8")
            self.assertIn("MonoImporter:", meta); self.assertRegex(meta, r"guid: [0-9a-f]{32}\n")
        self.assertIn("public static class BraxChampionsWynikiKladow", WYNIKI)
        self.assertIn("public sealed class BraxChampionsTablicaTop : MonoBehaviour", TOP)
        self.assertIn("public static class BraxChampionsPodiumKladow", PODIUM)
        self.assertIn("BraxChampionsPodiumKladow.Zbuduj(g, b, y, w, Folder);", TOR)

    def test_the_ten_best_on_the_phone(self):
        # four lists of ten, PlayerPrefs JSON, every read and write guarded; the single records before them seed them once
        self.assertEqual(pole("Ile", WYNIKI), 10)
        for k in ("BRAX_TOR_TOP_WYSCIG", "BRAX_TOR_TOP_OKRAZENIE", "BRAX_TOR_TOP_SKOK", "BRAX_TOR_PUCHAR_ZWYCIEZCY", "BRAX_TOR_PUCHARY"):
            self.assertIn('"%s"' % k, WYNIKI)
        for k in ("BRAX_TOR_WYSCIG_REKORD", "BRAX_TOR_KLADOW_NAJLEPSZE", "BRAX_KLAD_REKORD_SKOKU"):
            self.assertIn('"%s"' % k, WYNIKI)
        self.assertGreaterEqual(WYNIKI.count("try {"), 4); self.assertNotIn("PlayerPrefs.DeleteAll", WYNIKI)
        # the times shortest first, the jumps most points first, a tie: the one there first keeps his place
        self.assertIn("public static bool Lepszy(Rodzaj r, float a, float b) => r == Rodzaj.Skok ? a > b : a < b;", WYNIKI)
        self.assertIn("for (int i = 0; i < l.Count; i++) if (Lepszy(r, w, l[i].w)) { m = i; break; }", WYNIKI)
        self.assertIn("if (r == Rodzaj.Puchar) m = 0;", WYNIKI)                                     # the newest cup winner on top
        # who and his quad's paint: TY for the player, the rival's paint from the fleet's labels
        self.assertIn('public const string Gracz = "TY";', WYNIKI)
        self.assertIn("r.kolor = f.napis != null ? f.napis.Substring(f.napis.LastIndexOf('·') + 1).Trim() : kolor.ToUpperInvariant();", KLADY)
        self.assertIn('public string kolor = "";', RYWAL)

    def test_results_come_in(self):
        # the race: every real time over the line (the rivals' too), never an estimated one, never a cup race (one lap)
        self.assertIn("if (z.meta && !z.szac) { int m = R.Dodaj(R.Rodzaj.Wyscig, z.czas, z.imie, R.Kolor(z.klad)); if (z == ja) wynikTop = m; }", WYSCIG)
        blok = WYSCIG.split("void Koniec()")[1].split("ostatnie.Clear();")[0]
        self.assertIn("if (!puchar)", blok)
        # the lap (the lap timer), the jump (the player's only: a rival scores nothing)
        self.assertIn("BraxChampionsWynikiKladow.Dodaj(BraxChampionsWynikiKladow.Rodzaj.Okrazenie, t, BraxChampionsWynikiKladow.Gracz, BraxChampionsWynikiKladow.Kolor(jedzie));", CZAS)
        self.assertIn("BraxChampionsWynikiKladow.Dodaj(BraxChampionsWynikiKladow.Rodzaj.Skok, pkt, BraxChampionsWynikiKladow.Gracz, BraxChampionsWynikiKladow.Kolor(this));", KLAD)
        self.assertIn("if (!gracz) { lot = ileNaZiemi == 0 ? lot + dt : 0f; trik = Trik.Brak; return; }", KLAD)

    def test_the_cup(self):
        # three races of one lap with the same four rivals, the MXGP points (FIM: 25, 22, 20, 18, 16, 15 ... 1)
        self.assertEqual(ints("PunktyMXGP", WYSCIG), [25, 22, 20, 18, 16, 15, 14, 13, 12, 11])
        self.assertIn("w.okrazenPucharu = 1; w.wyscigowPucharu = 3;", TOR)
        self.assertIn("zz.punkty += i < PunktyMXGP.Length ? PunktyMXGP[i] : 0; zz.miejsca.Add(i + 1);", WYSCIG)
        # a tie: the better place in the last race (as motocross breaks it)
        self.assertIn("k.Sort((a, b) => a.punkty != b.punkty ? b.punkty.CompareTo(a.punkty) : Ostatnie(a).CompareTo(Ostatnie(b)));", WYSCIG)
        # the next race from the gate 12 s after the last; getting off stops the cup; the winner on the board, the player's cups
        self.assertEqual(const("CzasPrzerwy", WYSCIG), 12.0)
        self.assertIn("if (t > CzasPrzerwy) Ustawienie();", WYSCIG)
        blok = WYSCIG.split("case Faza.Wyniki:")[1].split("case Faza.Podium:")[0]
        self.assertIn("if (!ja.klad || BraxChampionsKlad.Jedzie != ja.klad) { Przerwij(); break; }", blok)
        self.assertIn("R.Dodaj(R.Rodzaj.Puchar, k[0].punkty, k[0].imie, k[0].klad);", WYSCIG)
        self.assertIn("if (wygrales) R.DodajPuchar();", WYSCIG)
        # its button under the race's (Y on a desktop); daytime only: no lights anywhere in it
        self.assertIn("else if (Input.GetKeyDown(KeyCode.Y) || Dotkniety(PrzyciskPucharu())) Zacznij(true);", WYSCIG)
        self.assertIn("Rect PrzyciskPucharu() { float u = Jedn; return new Rect(Screen.width * .5f - 230f * u, 110f * u, 460f * u, 84f * u); }", WYSCIG)
        for src in (WYSCIG, TOP, PODIUM, WYNIKI):
            self.assertNotRegex(src, r"\bLight\b|AddComponent<Light>|noc\b")

    def test_podium(self):
        h = tablica("Wysokosc", PODIUM)
        st0 = const("Stopien", PODIUM)
        dx = [float(v.strip().replace("Stopien", str(st0)).rstrip("f")) for v in re.search(r"Przesuniecie\s*=\s*\{([^}]*)\}", PODIUM).group(1).split(",")]
        self.assertEqual(h, sorted(h, reverse=True)); self.assertLessEqual(h[0], .8)               # 1st highest, a quad rides off it
        self.assertEqual(dx[0], 0.0); self.assertLess(dx[1], 0); self.assertGreater(dx[2], 0)      # the 2nd at the west: the audience's left
        st, gl = const("Stopien", PODIUM), const("Glebokosc", PODIUM)
        self.assertGreaterEqual(st, 1.28 + 1.5); self.assertGreaterEqual(gl, 2.05 + .8)              # a Rancher (2.05 x 1.28 m) with room
        # facing the paddock (south); the player not on it watches from 8.5 m before it, facing it
        self.assertIn("w.podiumObrot = Quaternion.Euler(0f, 180f, 0f);", PODIUM)
        self.assertIn("w.podiumWidz = new Vector3(PodiumX, y, WidzZ) * K; w.podiumWidzObrot = Quaternion.identity;", PODIUM)
        self.assertGreater(const("WidzZ", PODIUM) - 945.2, 6)                                     # clear of the grandstand's back for the camera
        # in the paddock's free west: inside it, clear of the grandstand (z 940 - 945.2), the riders' park (x -102), the masts
        x0, x1, z0, z1 = (const(n, PODIUM) for n in ("PlytaX0", "PlytaX1", "PlytaZ0", "PlytaZ1"))
        self.assertGreaterEqual(x0, const("PX0")); self.assertLessEqual(z1, const("PZ1")); self.assertGreater(z0, 945.2 + 8)
        self.assertLess(x1, const("ParkX") - 2 - 5)
        tx0, tx1 = const("TopX", PODIUM) - const("TopW", PODIUM) / 2, const("TopX", PODIUM) + const("TopW", PODIUM) / 2
        self.assertGreater(tx0, const("PX0") + 3); self.assertLess(tx1, x0 - 3)
        for mx, mz in ((-157, 952), (-157, 1005)):
            self.assertGreater(min(abs(mx - tx0), abs(mx - tx1)), 3)
        # the boxes 1 cm into the middle one and 1 cm shallower: no face shared (zfight 0); each 1 cm into the platform
        self.assertIn("float x0 = cx - Stopien * .5f - (i == 2 ? .01f : 0f), x1 = cx + Stopien * .5f + (i == 1 ? .01f : 0f), z0 = PodiumZ + (i == 0 ? 0f : .01f), z1 = PodiumZ + Glebokosc - (i == 0 ? 0f : .01f);", PODIUM)
        self.assertIn("b.BoxMinMax(podest, x0, x1, yp - .01f, yp + h, z0, z1);", PODIUM)
        self.assertIn('"BRAXHORN QUAD CUP"', PODIUM)

    def test_ceremony_and_cups(self):
        # the first three on their quads on the steps (the rivals' AI off, parked bodies), the player held there or before it
        self.assertIn("if (na) { z.rywal.Aktywny = false; z.klad.Sam(false); z.klad.Ustaw(podium[i], podiumObrot, Vector3.zero); }", WYSCIG)
        self.assertIn("z.klad.Ustaw(na ? podium[i] : podiumWidz, na ? podiumObrot : podiumWidzObrot, Vector3.zero);", WYSCIG)
        self.assertEqual(const("CzasPodium", WYSCIG), 14.0)
        self.assertIn("if (t > CzasPodium) Sprzatnij();", WYSCIG)
        self.assertIn("for (int i = 0; i < puchary.Length && i < stolPoz.Length; i++) if (puchary[i]) puchary[i].SetPositionAndRotation(stolPoz[i], stolObr[i]);", WYSCIG)
        # three cups, the metals' measured colours (gold sRGB 255/226/155, silver 252/250/245), real heights, not static
        self.assertEqual(tablica("Puchar", PODIUM), [.62, .56, .5])
        self.assertIn('Surface("tor_kladow_puchar_zloto", new Color(1f, .886f, .608f), .16f, 1f);', PODIUM)
        self.assertIn('Surface("tor_kladow_puchar_srebro", new Color(.988f, .98f, .96f), .14f, 1f);', PODIUM)
        self.assertIn("GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);", PODIUM)
        # each on its step beside its quad (inside the step, clear of a 1.28 m wide quad), 2 mm over it
        self.assertIn("w.pucharyNaPodium[i] = new Vector3(cx - 1.15f, yp + h + .002f, PodiumZ + .45f) * K;", PODIUM)
        st = const("Stopien", PODIUM)
        self.assertLess(1.15 + .17 / 2, st / 2); self.assertGreater(1.15 - .215, 1.28 / 2)

    def test_top10_board(self):
        tw, th = pole("TW", TOP), pole("TH", TOP)
        self.assertEqual((tw, th), (256, 128))
        self.assertAlmostEqual(const("TopW", PODIUM) / const("TopH", PODIUM), tw / th)              # square LEDs
        # its mipmaps (no shimmer from afar: ZASADA-MIGOTANIE), the LEDs' squares near; the race board's too
        for src in (TOP, WYSCIG):
            self.assertIn("TextureFormat.RGBA32, true) { filterMode = FilterMode.Point", src)
            self.assertIn("tex.SetPixels32(px); tex.Apply(true);", src)
        # ten rows 9 px apart under the title, the page's dots under them
        self.assertIn("int y = TH - 34 - i * 9;", TOP); self.assertGreater(th - 34 - 9 * 9, 6 + 2)
        # the columns hold their longest marks: 8 (1240 PKT, 12:34.5), 8 (the name), 12 (POMARAŃCZOWY), 5 (24.11)
        kol = {n: pole(n, TOP) for n in ("KolMiejsce", "KolWartosc", "KolKto", "KolKlad", "KolData")}
        self.assertLess(kol["KolWartosc"] + 8 * 6, kol["KolKto"]); self.assertLess(kol["KolKto"] + 8 * 6, kol["KolKlad"])
        self.assertLess(kol["KolKlad"] + 12 * 6, kol["KolData"]); self.assertLessEqual(kol["KolData"] + 5 * 6, tw)
        # every mark it and the race board print is in the LED font
        znaki = re.search(r'const string Znaki = "([^"]*)";', (RT / "BraxChampionsPiksele.cs").read_text(encoding="utf-8")).group(1)
        for t in ("TOP 10 · WYŚCIG", "TOP 10 · OKRĄŻENIE", "TOP 10 · SKOK", "PUCHAR BRAXHORN", "PUCHAR · START", "PUCHAR · PO 2/3",
                  "BRAK WYNIKÓW · JEDŹ!", "SKOCZ NA FREESTYLE", "TWOJE PUCHARY: 2", "3 WYŚCIGI · 25-22-20-18-16", "1240 PKT",
                  "POMARAŃCZOWY", "BIAŁY", "24.11", "TWÓJ REKORD 7:41.6", "PUNKTY 25-22-20-18-16", "ZWYCIĘZCA: SZYMON", "PODIUM",
                  "WYŚCIG 2/3", "1. SZYMON   47 PKT 1-2-1"):
            for c in t:
                self.assertIn(c, znaki, t)
        # the titles fit at twice the size: 15 marks on the race board, 21 on the TOP 10
        for t in ("PUCHAR BRAXHORN", "PUCHAR · PO 2/3", "WYŚCIG BRAXHORN"):
            self.assertLessEqual(len(t) * 12 - 2, 192 - 8)
        self.assertLessEqual(len("TOP 10 · OKRĄŻENIE") * 12 - 2, tw - 8)
        # the screen faces the paddock (south), its picture the right way round from there
        self.assertIn("be.QuadUV(ekran, new Vector3(x0, yb, zs), new Vector3(x0, yt, zs), new Vector3(x1, yt, zs), new Vector3(x1, yb, zs),", PODIUM)
        self.assertIn("t.ekran = r.Length > 0 ? r[0] : null; t.wyscig = w;", PODIUM)


if __name__ == "__main__":
    unittest.main()
