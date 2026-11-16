"""MIASTO R1 16.11.2026 — nowa część miasta (wieżowce przy Alei Centralnej) zagospodarowana pod grę. Źródło, bez Unity i APK.

Właściciel 17:38: „tą nową część miasta… zagospodarować… coś pod grę… sklepy trzeba otworzyć… możesz kopiować, apteki”;
17:57: „ustawki… szare miejsce, ciemne… biuro deweloperskie… buchmacherkę… mały komis samochodów używanych… koło tramwaju”;
17:55: „wszystko realistyczne… najlepsze materiały”.
"""
import json
import math
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"


def src(p):
    return re.sub(r"//[^\n]*", "", Path(p).read_text(encoding="utf-8"))


LOK = src(REG / "BraxRegionFawelaWiezeLokale.cs")
WIEZE = src(REG / "BraxRegionFawelaWieze.cs")
PIERZ = src(REG / "BraxUlicePierzeje.cs")
ND = src(REG / "BraxRegionMiastoBDzielnice.cs")
NM = src(REG / "BraxRegionMiastoBNoweMiejsca.cs")

WG, FIL = 3.6, .4
K = 2.33 / 1.80


def lokale():
    out = []
    for m in re.finditer(r'new LokalWiezy\("(\w+)", (\d), (-?\d+), (\d+), (\d+), "([^"]+)", "(\w+)", "(\w+)", "([^"]+)"', LOK):
        out.append(dict(bud=m[1], strona=int(m[2]), bay0=int(m[3]), n=int(m[4]), drzwi=int(m[5]), szyld=m[6], typ=m[7], id=m[8], akcja=m[9]))
    return out


def budynki():
    b = {}
    for m in re.finditer(r'new Bud\("(\w+)", "(\w+)", [^,]+, (-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', ND):
        b[m[1]] = dict(model=m[2], x=float(m[3]), z=float(m[4]), w=float(m[6]), d=float(m[7]))
    return b


def sciana(x, z, w, d, si):
    """Rama ściany jak Fr.Nowa (R = up × N) i siatka filarów jak LinieSiatki."""
    x0, x1, z0, z1 = x - w / 2, x + w / 2, z - d / 2, z + d / 2
    A, N, L = [((x0, z1), (0, 1), w), ((x1, z0), (0, -1), w), ((x0, z0), (-1, 0), d), ((x1, z1), (1, 0), d)][si]
    R = (N[1], -N[0])
    wzdx = abs(R[0]) > .5
    a0, kier = (A[0], R[0]) if wzdx else (A[1], R[1])
    lo, hi = min(a0, a0 + kier * L), max(a0, a0 + kier * L)
    lin = sorted(s for j in range(math.floor(lo / WG) - 1, math.ceil(hi / WG) + 2)
                 for s in [(j * WG - a0) / kier] if FIL + .15 < s < L - FIL - .15)
    osie = [(lin[i] + lin[i + 1]) / 2 for i in range(len(lin) - 1)]
    wej = len(osie) // 2 if (si < 2 and len(osie) >= 3) or (si >= 2 and len(osie) >= 7) else -1
    P = lambda s, dd: (A[0] + R[0] * s - N[0] * dd, A[1] + R[1] * s - N[1] * dd)
    return lin, osie, wej, P


class LokaleWParterach(unittest.TestCase):
    def test_table_has_shops_bet_and_developer(self):
        L = lokale()
        self.assertEqual(len(L), 12)
        typy = {l["typ"] for l in L}
        for t in ("bukmacher", "deweloper", "apteka", "market", "kawiarnia", "pizzeria", "bistro", "cukiernia", "piekarnia", "fryzjer", "optyk", "kwiaty"):
            self.assertIn(t, typy)
        bet = next(l for l in L if l["typ"] == "bukmacher")
        self.assertEqual((bet["szyld"], bet["akcja"]), ("BRAXHORN BET", "POSTAW ZAKŁAD"))
        self.assertEqual(len({l["id"] for l in L}), 12)                                   # id AKCJI bez powtórzeń

    def test_every_shop_fits_the_ground_floor_grid_and_skips_the_lobby(self):
        B = budynki(); zajete = {}
        for l in lokale():
            b = B[l["bud"]]
            self.assertEqual(b["model"], "miasto_centrum_c5", l)                         # tylko wieżowce budowane z kodu
            lin, osie, wej, P = sciana(b["x"], b["z"], b["w"], b["d"], l["strona"])
            q0 = l["bay0"] if l["bay0"] >= 0 else len(osie) + l["bay0"]; q1 = q0 + l["n"] - 1; qd = q0 + l["drzwi"]
            self.assertTrue(0 <= q0 and q1 < len(osie), l)
            self.assertFalse(q0 <= wej <= q1, l)                                         # portal holu wieżowca zostaje
            klucz = (l["bud"], l["strona"])
            for (a, c) in zajete.get(klucz, []): self.assertTrue(q1 < a or q0 > c, l)    # lokale nie nachodzą na siebie
            zajete.setdefault(klucz, []).append((q0, q1))
            s0, s1 = lin[q0], lin[q1 + 1]
            self.assertGreaterEqual(s1 - s0, 10.7)                                       # co najmniej 3 przęsła × 3.6 m
            a, c = P(s0 + .37, .04), P(s1 - .37, 7.05 - .124)
            zmin, zmax = min(a[1], c[1]), max(a[1], c[1])
            if l["strona"] == 0: self.assertTrue(-52.5 <= zmin and zmax <= -3.49, l)       # rząd C: posadzka w kwartale z promenadą
            else: self.assertTrue(40.5 <= zmin and zmax <= 91.0, l)                       # rząd A: od lica przy Alei Centralnej

    def test_tower_builder_opens_real_shops(self):
        self.assertIn("var lokaleTu = LokaleWiez.Where(l => l.bud == id).ToList();", WIEZE)
        self.assertIn("var szyba = lp != null ? WB(LokSzklo).K(Color.white) : oknaW;", WIEZE)
        self.assertIn("if (lp != null) Pasek(szyba, F, o.xMax, o.xMin, o.yMin, o.yMax, Gl - .06f);", WIEZE)   # szkło dwustronne
        self.assertIn("if (lp != null && lp.qd == q) continue;", WIEZE)
        self.assertIn("LicoWewnetrzne(WB(p.hol ? HolLico : LokLico).K(Color.white), p, yBase, yBase + LokSufit, Gl);", WIEZE)
        self.assertIn("if (hol == null) KolizjeZLokalami(korzen, id, x, z, w, d, yBase, top, Gl, Wys, plany);", WIEZE)
        self.assertIn("ZbudujLokal(korzen, id, p, yBase, Gl);", WIEZE)
        self.assertIn("foreach (var p in lokTu.OrderBy(p => p.sc)) { float pd = LokDrzwiW * .5f + (p.hol ? .25f : 0f); BoxF(cokB, F, cs0, p.sc - pd,", WIEZE)
        self.assertIn("BraxDrzwiLokalu.Podwojne(lr, \"R1_WIEZA_\"", LOK)
        self.assertIn("BraxUlicePierzeje.WnetrzeKamienicy(lr, l.szyld, l.typ, l.numer, A, -F.R, F.N, p.s1 - p.s0, LokGl + .55f, p.s1 - p.sc,", LOK)
        self.assertIn("BraxMiastoA.Akcja(lr, l.idAkcji, l.akcja, l.odpowiedz, klient, 1.6f);", LOK)
        self.assertIn('KolBox(g, "posadzka lokalu " + p.Nazwa', LOK)

    def test_kwartal_slabs_are_cut_under_shop_floors(self):
        self.assertIn('ChodnikBez("kwartał A" + (i + 1) + " (płyty)", Rect.MinMaxRect(xa, RzA0, xb, RzA1), bezLokali);', ND)
        self.assertIn('ChodnikBez("kwartał C" + (i + 1) + " z promenadą (płyty)", Rect.MinMaxRect(xa, RzC0, xb, RzC1), bezLokali);', ND)
        self.assertNotIn('Chodnik("kwartał A" + (i + 1) + " (płyty)", xa, xb, RzA0, RzA1);', ND)
        self.assertIn("try { NdLokaleKontrola(); }", ND)                                 # lokal bez wieżowca → łata płyt
        # 16.11: wycięcie 4 cm szersze pod ścianami lokalu (krawędź płyty nie w licu ściany — kontrola migotania)
        self.assertIn('wynik.Add((id + "|" + l.szyld, RectF(F, linie[q0] + LokOds - .04f, linie[q1 + 1] - LokOds + .04f, .04f, LokTyl + .04f)));', LOK)


class BukmacherIDeweloper(unittest.TestCase):
    def test_generator_branches(self):
        self.assertIn('if (t == "bukmacher") Bukmacher(', PIERZ)
        self.assertIn('if (t == "deweloper") Deweloper(', PIERZ)
        self.assertIn('case "bukmacher": pod = new[] { "granite_tile", "granite_tile_03" };', PIERZ)
        self.assertIn('case "deweloper": pod = new[] { "wood_floor", "plank_flooring_02" };', PIERZ)
        self.assertIn('&& lok.typ != "bukmacher" && lok.typ != "deweloper"', PIERZ)    # bez regałów z towarem
        self.assertIn('lok.typ == "apteka" || lok.typ == "bukmacher")', PIERZ)           # okienko kasowe nad ladą

    def test_bet_screens_are_textures_and_odds_board(self):
        self.assertIn("BraxUliceKit.NapisWTeksturze(px, Wd, Hd, tytul,", PIERZ)          # napis na ekranie = obraz, nie nakładka
        self.assertIn('EkranBET("LIGA MISTRZÓW   DZIŚ"', PIERZ)
        self.assertIn('"BRAXHORN BET   KURSY"', PIERZ)
        self.assertIn('BraxMiastoA.Akcja(bg, "bet_transmisja", "OGLĄDAJ TRANSMISJĘ"', PIERZ)
        self.assertIn("StolikWysoki(W(sT, y0, d), bg, kolizje);", PIERZ)

    def test_developer_office(self):
        self.assertIn('"MIESZKANIA   ALEJA CENTRALNA"', PIERZ)
        self.assertIn('KrzesloTapicerowane("Biuro dewelopera: krzesło doradcy"', PIERZ)
        self.assertIn('"makieta: bryły (biały karton)"', PIERZ)


class UstawkiIKomis(unittest.TestCase):
    def test_dark_concrete_yard_behind_atlanta(self):
        self.assertIn("NdPlacAtlanty();", ND)
        self.assertNotIn('Plyta("Plac ATLANTA: płyty granitowe", granitPlac, NXA, NC[3] - NPR, NS1 - NPR, RzB1, YC, .3f - YA);', ND)
        self.assertIn('foreach (var r in Odejmij(plac, PlacUstawekA)) if (Niepuste(r)) Plyta("Plac ATLANTA: płyty granitowe"', NM)
        self.assertIn('"concrete_floor_worn_001"', NM)
        self.assertIn('z.id = "ustawka_atlanta"; z.napis = "USTAWKA 3 NA 3";', NM)
        self.assertIn("try { NdPlacUstawek(); }", ND)
        m = re.search(r"PlacUstawekA = Rect\.MinMaxRect\((-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f\)", NM)
        x0, z0, x1, z1 = map(float, m.groups())
        atl = budynki()["ATLANTA"]
        self.assertGreater(z0, atl["z"] + atl["d"] / 2)                                   # na północ od ATLANTY (w jej cieniu)
        self.assertTrue(-872 < x0 and x1 < -759.75 and z1 <= 149.5)                       # w granicach placu i miasta

    def test_used_car_lot_by_the_tram(self):
        self.assertIn('TrawaBez("Pas zieleni na południu (do granicy)", Rect.MinMaxRect(NXA, BraxMiastoGranice.Z0, NX, NS1 - NPR), true, PeronTramwaju, KomisRect);', ND)
        self.assertIn("try { NdKomis(); }", ND)
        m = re.search(r"KomisRect = Rect\.MinMaxRect\((-?[\d.]+)f, (-?[\d.]+)f, (-?[\d.]+)f,", NM)
        x0, z0, x1 = map(float, m.groups())
        self.assertTrue(-750 + 9.75 <= x0 and x1 <= -672)                                 # między ul. C3 a peronem T1 TRAMWAJOWA
        self.assertGreater(z0, -85.45)
        # auta stoją (bez BraxAuto — zamiatanie aut ich nie dotyczy), przód z kół, obrót tylko wokół pionu
        self.assertNotIn("BraxAuto", NM)
        self.assertIn("Vector3.SignedAngle(przod, Vector3.forward, Vector3.up)", NM)
        man = json.loads((ROOT / "unity-city/Prywatne/braxhorn-sklep-pojazdy-02/manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(man["target"], "pojazdy_komis_01")
        pliki = [f for f in man["files"] if f.endswith(".glb")]
        self.assertEqual(len(pliki), 5)
        for plik in re.findall(r'\("(Braxhorn_\w+)", "', NM):
            self.assertIn("pojazdy_komis_01/" + plik + ".glb", man["files"])



class ZdjeciaZEdytora(unittest.TestCase):
    """Właściciel 19:02: „bez zdjęcia nic nie akceptuje” — zdjęcia nowych miejsc z gotowej sceny w tym samym przebiegu APK."""
    def test_photos_before_scene_save_and_split(self):
        gym = src(ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs")
        i_z = gym.index("BraxR1Zdjecia.Wykonaj(scene)")
        self.assertLess(gym.index("BraxWyspaOdbior.SprawdzScene(scene);"), i_z)
        self.assertLess(i_z, gym.index('string scenePath=osiedle?null:Generated+(fullCity?"/BraxhornCity.unity"'))
        z = src(REG / "BraxR1Zdjecia.cs")
        self.assertIn('public const string Katalog = "Logs/r1-zdjecia";', z)            # unity-city/Logs/ = artefakt diagnostyki
        self.assertIn("if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)", z)
        self.assertEqual(len(re.findall(r'\("\d\d_\w+", new Vector3', z)), 28)   # 12 nowej części + 8 lądowiska + 2 biura z grafikami + 3 banery właściciela + 3 biuro komisu (16.11)
        wf = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")
        self.assertIn("unity-city/Logs/", wf)


class GrafikiBetWlasciciela(unittest.TestCase):
    """16.11 21:51: „Masz, to ci się przyda” — logo BRAXHORN BET i 6 plakatów walk właściciela (paczka zaszyfrowana)."""

    def test_pack_encrypted_with_logo_and_six_posters(self):
        pk = ROOT / "unity-city/Prywatne/braxhorn-sklep-bet-01"
        m = json.loads((pk / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "bet_01")
        for f in ["bet_01/logo.png", "bet_01/logo_kaseton.jpg"] + ["bet_01/plakat_0%d.jpg" % i for i in range(1, 7)]:
            self.assertIn(f, m["files"])
        self.assertEqual(sorted(q.name for q in pk.iterdir()), ["manifest.json", "part-000.cms", "recipient.crt", "recipient.pem.enc"])   # nic jawnego
        dane = (pk / "part-000.cms").read_bytes()
        self.assertNotIn(b"\x89PNG", dane); self.assertNotIn(b"JFIF", dane)

    def test_bet_uses_owner_graphics_without_fallback_blocks(self):
        g = src(REG / "BraxBetGrafiki.cs")
        self.assertIn('Katalog = "Assets/Source/Prywatne/Miasto/bet_01/"', g)
        self.assertIn("Lico(obraz, srodek + n * .025f, n, w, h);", g)                 # plakat 2.5 cm przed ścianą
        self.assertIn("Lico(obraz, srodek + n * (gl - .03f), n, w, h);", g)          # lico kasetonu w ramie
        self.assertNotIn("Rura(c", g)                                                 # bez linek
        self.assertIn("BraxBetGrafiki.Kaseton(W(sLogo", PIERZ)
        self.assertIn("BraxBetGrafiki.Stojak(nrW[ix]", PIERZ)
        self.assertIn('l.typ == "bukmacher" && BraxBetGrafiki.Kaseton(F.P(sm, yc, -.26f), F.N, .88f)', LOK)


class GrafikiNieruchomosci(unittest.TestCase):
    """16.11 22:57: „Ty to zamówiłeś na recepcję”, 23:01: „To zrób w ramkach” — logo W, 3 zdjęcia i 3 ogłoszenia (paczka zaszyfrowana)."""

    def test_pack_encrypted_with_logo_photos_and_listings(self):
        pk = ROOT / "unity-city/Prywatne/braxhorn-sklep-nieruchomosci-01"
        m = json.loads((pk / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "nieruchomosci_01")
        for f in ["nieruchomosci_01/logo.png", "nieruchomosci_01/logo_kaseton.jpg"] + ["nieruchomosci_01/plakat_0%d.jpg" % i for i in range(1, 7)]:
            self.assertIn(f, m["files"])
        for i in range(1, 7):
            self.assertIn("oryginal/plakat_0%d_oryginal.png" % i, m["files"])            # oryginały właściciela w paczce
        self.assertEqual(sorted(q.name for q in pk.iterdir()), ["manifest.json", "part-000.cms", "recipient.crt", "recipient.pem.enc"])   # nic jawnego
        dane = (pk / "part-000.cms").read_bytes()
        self.assertNotIn(b"\x89PNG", dane); self.assertNotIn(b"JFIF", dane)

    def test_office_logo_wall_frames_and_window_listing(self):
        g = src(REG / "BraxNieruchomosciGrafiki.cs")
        self.assertIn('Katalog = "Assets/Source/Prywatne/Miasto/nieruchomosci_01/"', g)
        self.assertIn("Lico(obraz, srodek + n * .012f, n, w, h);", g)                 # obraz 2.3 cm za licem ramy (≥ 2 cm)
        self.assertIn("Lico(obraz, c0 + n * (gl - .03f), n, w, h);", g)              # lico kasetonu w ramie
        self.assertNotIn("Mosiadz", g)                                                # zasada 10.11: bez złota w kodzie
        self.assertIn("BraxUliceKit.Napis3D(tekst, c0 - Vector3.up * (cap * .5f) + n * .025f, n, cap, .05f, LicoStal, BokiGrafit);", g)
        dew = PIERZ[PIERZ.index("static void Deweloper("):PIERZ.index("const float WolneOdDrzwi")]
        self.assertIn("BraxNieruchomosciGrafiki.Kaseton(W(kcL, y0 + 2.6f * k, dTyl), f.N, hK)", dew)
        self.assertIn("foreach (var (nr, sg) in new[] { (2, -1f), (3, 1f) })", dew)  # za recepcją logo i zdjęcia (wille, marina)
        # 23:15 „domy na sprzedaż z boku, żeby klient miał do nich dostęp, a nie przy kasie”: ogłoszenia w strefie klienta z AKCJĄ
        self.assertIn("foreach (var (nr, adres, co) in oferty)", dew)
        self.assertIn('BraxMiastoA.Akcja(bg, "nieruchomosci_oferta_" + nr, "OBEJRZYJ OFERTĘ"', dew)
        self.assertNotIn("Plakat(4, W(so", dew); self.assertNotIn("(4, -1f)", dew)
        self.assertIn("BraxNieruchomosciGrafiki.Plakat(1, W(sD, y0 + 1.75f * k, dc), nSc, h)", dew)   # kawalerka przy biurkach
        self.assertIn("BraxNieruchomosciGrafiki.Stojak(6, W(sw, y0, -.45f), f.N, 1.1f, .62f)", dew)
        self.assertIn("if (!grafiki)", dew)                                           # bez paczki — tablica oferty z kodu


class BaneryWlasciciela(unittest.TestCase):
    """16.11 23:14 baner komisu, 23:16 baner motywacyjny nad ulicą główną „z dwóch stron” (paczka zaszyfrowana)."""
    BAN = src(REG / "BraxMiastoBanery.cs")

    def test_pack_encrypted_with_both_banners(self):
        pk = ROOT / "unity-city/Prywatne/braxhorn-sklep-banery-r1-01"
        m = json.loads((pk / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "banery_r1_01")
        for f in ["banery_r1_01/komis_baner.jpg", "banery_r1_01/trenuj_baner.jpg", "oryginal/komis_baner_oryginal.png", "oryginal/trenuj_baner_oryginal.png"]:
            self.assertIn(f, m["files"])
        self.assertEqual(sorted(q.name for q in pk.iterdir()), ["manifest.json", "part-000.cms", "recipient.crt", "recipient.pem.enc"])
        dane = (pk / "part-000.cms").read_bytes()
        self.assertNotIn(b"\x89PNG", dane); self.assertNotIn(b"JFIF", dane)

    def test_komis_banner_on_fence(self):
        # 23:37 „Na płocie go powieś” — baner na płocie od ulicy, 2.5 cm przed licem słupków, opaski poza polem nadruku
        nd = src(REG / "BraxRegionMiastoBNoweMiejsca.cs")
        kom = src(REG / "BraxRegionMiastoBKomis.cs")
        self.assertIn("banerKomisu = BraxMiastoBanery.NaPlocie(g, srodekBanera, Vector3.forward, hBanera, OdstepBanera);", nd)
        self.assertNotIn("NaDachuKontenera(g", nd)
        self.assertIn("internal const float OdstepBanera = .055f;", kom)
        self.assertIn("srodekBanera = new Vector3(R.xMin + 9.8f, y + .25f + .05f, zU + OdstepBanera);", kom)
        self.assertIn('Katalog = "Assets/Source/Prywatne/Miasto/banery_r1_01/"', self.BAN)
        self.assertIn("Vector3.up * (sg * (h * .5f + .011f))", self.BAN)                                  # opaski przez krawędź banera
        self.assertIn("public static string WRamce(Transform g, Vector3 srodek, Vector3 n, float h)", self.BAN)
        self.assertIn("BraxMiastoBanery.WRamce(root, new Vector3(bx, yP + 1.55f * k, z0 + w1), Vector3.forward, .75f)", kom)

    def test_komis_normal_fence_and_enterable_office(self):
        # 23:38 „zrób płot normalny … tę budkę zrób trochę lepszą, że tam można było wejść”
        nd = src(REG / "BraxRegionMiastoBNoweMiejsca.cs")
        kom = src(REG / "BraxRegionMiastoBKomis.cs")
        self.assertIn("string biuro = KomisBiuro(g, R, y);", nd)
        self.assertIn("string plot = KomisPlot(g, R, y, xWj0, xWj1, out var srodekBanera, out float hBanera);", nd)
        # płot panelowy 3D: siatka w teksturze z alfą (cutout), słupki 60 × 60, podmurówka wzdłuż płotu, narożniki bez zdublowanych słupków
        self.assertIn("imp.mipMapsPreserveCoverage = true", kom)
        self.assertIn('m.EnableKeyword("_ALPHATEST_ON")', kom)
        self.assertNotIn('m.SetFloat("_Cull", 0f)', kom)                                               # dwa quady, bez podwójnego renderu
        self.assertIn("gPod = .05f, sx = .06f, sz = .06f", kom)
        self.assertIn("new Vector3((p1 - p0).magnitude - .004f, hPod + .05f, gPod), rot)", kom)
        self.assertIn('new Vector3(xZ, y, zT), new Vector3(xW, y, zT), hP, sl, pod, pan, false, false);', kom)
        # biuro: drzwi na zawiasach (same się otwierają), ściany warstwami z pustką 2.5 cm (bez lic w jednej płaszczyźnie)
        self.assertIn('d.id = "R1_KOMIS_BIURO"', kom)
        self.assertIn("d.leaves = new[] { zaw }", kom)
        self.assertIn("e0 = .04f, e1 = .065f, e2 = .09f, w1 = .11f", kom)
        self.assertIn('BraxMiastoA.Akcja(root, "komis_biuro", "ZAPYTAJ O AUTO"', kom)
        self.assertIn('"R1_KOMIS_BIURO_WEJSCIE"', kom)
        self.assertIn("PlytaWnetrza()", kom)                                                           # listwy H w teksturze
        self.assertIn('180f, .24f, false, "classic_laptop")', kom)                                     # laptop w skali (nie 2.6 cm)

    def test_single_variant_of_polyhaven_models(self):
        meb = src(REG / "BraxRegionMeble.cs")
        self.assertIn("JedenWariant(o);", meb)
        self.assertIn('if (t && !t.name.EndsWith("_a")) UnityEngine.Object.DestroyImmediate(t.gameObject);', meb)

    def test_street_banner_double_sided_over_roadway(self):
        ul = src(REG / "BraxMiastoUlica.cs")
        self.assertIn("BraxMiastoBanery.NadUlica(root, -17f, 20.6f, 36.1f, -18f, -16f, Grunt(-17f, (KrawPld + KrawPln) * .5f), 8f)", ul)
        self.assertIn("yD = yJezdni + 5.8f", self.BAN)                                                  # skrajnia 4.6 m z zapasem
        self.assertIn("Lico(sA, c, Vector3.left, szer, h);", self.BAN)                                   # nadruk z obu stron, nie lustrzany
        self.assertIn("Lico(sB, c, Vector3.right, szer, h);", self.BAN)
        self.assertIn("kot.Rura(a + dir * .05f, a + dir * .35f, .022f, 8);", self.BAN)                 # napinacze śrubowe


class LadowiskoNaDachu(unittest.TestCase):
    """16.11 22:00: „Na dachu wieżowca dla helikoptera miejsce… Zrób też miejsce zajebiste i wejście na dach.”"""
    LAD = src(REG / "BraxRegionFawelaWiezeLadowisko.cs")

    def test_tower_gets_real_lobby_and_lift_to_roof(self):
        self.assertIn('LadowiskoWieza = "C5_U04"', self.LAD)
        self.assertIn("lokTu.Add(new LokalPlan { hol = true, tyl = HolGl", WIEZE)
        self.assertIn("PortalWiezy(F, o, WB, st, oknaW, rama, id, si, LokalQ(q)?.hol == true)", WIEZE)
        self.assertIn("BraxDrzwiLokalu.Podwojne(hr, \"R1_WIEZA_\" + id + \"_HOL\"", self.LAD)
        self.assertIn("BraxRegionTylyB.WindaZewn(hr,", self.LAD)
        self.assertIn("internal static void WindaZewn(", src(REG / "BraxRegionTylyB.cs"))

    def test_collisions_follow_setbacks_and_leave_lift_shaft_open(self):
        self.assertIn("KolizjeZLokalami(korzen, id, x, z, w, d, yBase, top, Gl, Wys, plany, bryly, SzybRect(SzybLadowiska(hol)))", WIEZE)
        self.assertIn("MinusRect(br, dziura.Value)", LOK)
        self.assertIn('wynik.Add((id + "|SZYB LĄDOWISKA"', LOK)

    def test_helideck_real_dimensions_markings_in_texture(self):
        self.assertIn("PlytaB = 17f, Polka = 1.5f, PlytaNad = 4.4f", self.LAD)
        self.assertIn("float rIn = 4f, rOut = 5f;", self.LAD)                       # okrąg przyziemienia 0.5 D, szer. 1 m
        self.assertIn("TeksturaPlyty()", self.LAD)                                  # oznakowanie w teksturze płyty
        self.assertIn("int nR = 23;", self.LAD)                                     # schody 23 × 17.7 cm
        self.assertIn('BraxMiastoA.Akcja(g, "ladowisko_lot", "LOT HELIKOPTEREM"', self.LAD)
        for pkt in ["R1_LADOWISKO_HOL", "R1_LADOWISKO_PLYTA", "R1_LADOWISKO_TARAS", "R1_LADOWISKO_WINDA_GORA"]:
            self.assertIn(pkt, self.LAD)

    def test_finishes_real_materials_and_sign(self):
        # konstrukcja: stal malowana proszkowo (nie blacha ryflowana na słupach); stopnie: blacha ryflowana
        self.assertIn('Surface("lądowisko: stal konstrukcji (farba proszkowa RAL 7016)"', self.LAD)
        self.assertIn('"lądowisko: stopnie i pomosty (blacha ryflowana ocynkowana)", .6f', self.LAD)
        # pawilon: posadzka z marmuru w poziomie tarasu (płyty tarasu wycięte pod pawilonem — bez progu do windy)
        self.assertIn("foreach (var r in MinusRect(rIn, rPaw))", self.LAD)
        self.assertIn('Wyrob(bpp, "Pawilon odlotów: posadzka marmurowa"', self.LAD)
        # trzon windy w granicie dookoła; front z otworami na opaskę i kasetę (hol i pawilon)
        self.assertIn("static void OkladzinaFrontuSzybu(", self.LAD)
        self.assertEqual(self.LAD.count("OkladzinaFrontuSzybu(b"), 2)
        self.assertIn("internal static Vector2 DrzwiWindy", src(REG / "BraxRegionTylyB.cs"))
        # rękaw wiatrowy: pasy jako osobne materiały; napis nad wejściem z liter podświetlanych na daszku portalu
        self.assertIn('"lądowisko: rękaw — tkanina pomarańczowa"', self.LAD)
        self.assertIn('const string napis = "LĄDOWISKO · BRAXHORN HELI";', self.LAD)

    def test_roof_clutter_removed_on_helipad_tower(self):
        self.assertIn("bool ladowisko = id == LadowiskoWieza && plany.Any(p => p.hol);", WIEZE)
        self.assertIn("if (!namiot && !ladowisko) WB(wStal)", WIEZE)


class KontrolaMigotania(unittest.TestCase):
    """16.11 wieczór: podgląd z prawdziwego kodu (unity-tools/claude/podglad_aleja) — pary powierzchni < 2 cm = 0 (różne materiały)."""

    def test_boards_have_text_in_texture_not_floating_letters(self):
        tab = PIERZ[PIERZ.index("static void TablicaZaLada("):PIERZ.index("static Material TablicaTekstura(")]
        self.assertNotIn("Napis3D", tab)
        self.assertIn("TablicaTekstura(nazwa, wiersze", tab)

    def test_skirting_and_panelling_are_solids_at_least_2cm(self):
        self.assertIn("const float gLis = .03f;", PIERZ)
        self.assertIn("lam.Box(P(s0, y0 + .085f, -.06f), P(s0 + o, yL, dT + o));", PIERZ)
        self.assertNotIn("lam.Quad(", PIERZ)

    def test_pizza_oven_has_real_mouth_and_avoids_counter(self):
        self.assertIn("bool kolA = ladaGl &&", PIERZ)
        self.assertNotIn("dO = d1 - .12f + .006f", PIERZ)

    def test_tower_window_frames_are_solids_and_sills_stop_at_wall(self):
        self.assertIn("static void RamaLica(", WIEZE)
        self.assertNotIn("Pasek(rama,", WIEZE)
        self.assertIn("o.yMin - .16f, o.yMin, -.1f, 0f, false, dokladne);", WIEZE)
        self.assertIn("o.yMin - .12f, o.yMin, -.08f, 0f, false, true);", WIEZE)
        self.assertIn("o.yMin - .3f, o.yMin + .03f, -1.1f, 0f, false, false);", WIEZE)

    def test_preview_report_has_zero_flicker_pairs(self):
        rap = ROOT / "unity-tools/claude/podglad_aleja/out/migotanie.txt"
        if not rap.exists():
            self.skipTest("podgląd nie zbudowany (bash unity-tools/claude/podglad_aleja/build.sh)")
        pierwsza = rap.read_text(encoding="utf-8").splitlines()[0]
        self.assertIn("migotanie (różne materiały) 0, szkło 0", pierwsza)


if __name__ == "__main__":
    unittest.main()
