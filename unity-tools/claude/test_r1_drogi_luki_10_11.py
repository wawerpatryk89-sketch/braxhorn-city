"""MIASTO R1 10.11 — drogi: narożniki łukiem, szersza Wiaduktowa i Warsztatowa, auta bez korków przy autobusie.

Właściciel 10.11: „każda droga lekko zaokrąglona… narożniki zaokrąglone, łatwiej będą samochody wjeżdżać” (globalnie),
„te auta przez to się korkują”. Szybkie testy źródeł (zasada 7.11) + geometria łuków liczona z tych samych stałych.
"""
import math
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
ED = ROOT / "unity-city/Assets/Editor"
RT = ROOT / "unity-city/Assets/Claude/Runtime"


def czytaj(p):
    return pathlib.Path(p).read_text(encoding="utf-8")


def stala(src, nazwa):
    m = re.search(r"\bconst float [^;]*?\b" + nazwa + r"\s*=\s*(-?[0-9.]+)f?", src)
    return float(m.group(1))


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


B = czytaj(REG / "BraxRegionMiastoB.cs")
D = czytaj(REG / "BraxRegionMiastoBDzielnice.cs")
L = czytaj(REG / "BraxRegionMiastoBLuki.cs")
WX, WH, SZ, SH, AX, AH, KR = (stala(B, n) for n in ("WX", "WH", "SZ", "SH", "AX", "AH", "Kr"))
RLUKU = stala(L, "RLuku")
ZPL = stala(L, "ZebraPrzedLukiem")


class LukiNarozniki(unittest.TestCase):
    def test_promien_3_5_m_razy_K(self):
        self.assertAlmostEqual(RLUKU, 3.5 * 1.294, delta=.05)

    def test_plan_wszystkich_skrzyzowan(self):
        plan = metoda(L, "static void LukiPlan()")
        for nazwa in ("Główna × Wiaduktowa", "Wiaduktowa × Warsztatowa", "Aleja Płd. × Warsztatowa", "NdSkrzyzowania()"):
            self.assertIn(nazwa, plan)
        self.assertIn("if (ax && az)", plan)   # narożnik tylko przy dwóch ramionach (bez ramienia krawężnik prosto)
        app = metoda(B, "public static void Apply(")
        self.assertLess(app.index("LukiPlan"), app.index("Krok(\"1. nawierzchnie: rondo"))

    def test_plyty_i_trawniki_obchodza_luk(self):
        pl = metoda(B, "static void Plyta(")
        self.assertIn("BezNaroznikow", pl)
        self.assertIn("PlytaWLukach", pl)
        tr = metoda(B, "static void Trawa(")
        self.assertIn("BezNaroznikow", tr)
        self.assertIn("WKole(w, L, L.R - Kr)", tr)
        self.assertIn("(pasek przy łuku)", tr)   # wąskie paski po przycięciu = płyty, bez dziur w terenie

    def test_kraweznik_i_asfalt_luku_przed_asfaltem(self):
        po = metoda(B, "static void PlanOznakowania()")
        self.assertLess(po.index("LukiBuduj();"), po.index("BudujAsfalt(farbaB);"))
        bud = metoda(L, "static void LukiBuduj()")
        self.assertIn("Krawężniki łukiem", bud)
        self.assertIn("Jezdnie: asfalt narożników", bud)
        self.assertIn("BraxCityMeshCut.Subtract(ulicaA", bud)          # chodnik A wycięty w kwadracie narożnika
        self.assertIn("Near pavement hand-laid", bud)                    # łata w kostce A, nie w płytach B
        self.assertIn("BRAXHORN_MIASTO_B_MIGOTANIE asfalt łuku", bud)    # kontrola asfalt × asfalt

    def test_ruch_i_wyposazenie_znaja_luki(self):
        self.assertIn("if (WLukuAsfalt(q, .3f)) return true;", metoda(B, "public static bool NaJezdni("))
        self.assertIn("PozaLukiem(p)", B[B.index("static Vector3 NaPodlozu("):][:120])
        sk = metoda(B, "static void SkrzyzowanieT(")
        self.assertEqual(sk.count("OdsuniecieZebry(c)"), 2)   # oznakowanie i sprzęt: te same zebry
        self.assertIn("PozaLukiem(c + dir * (zebraOd - .35f)", sk)

    def test_zebry_dzielnic_za_lukiem(self):
        self.assertIn("OdsuniecieZebry(new Vector2(c, c2))", metoda(D, "static void NoweDzielniceOznakowanie("))
        self.assertIn("Mathf.Max(0f, RLuku - ZebraPrzedLukiem)", metoda(D, "static void NdUlice()"))

    def test_geometria_zebra_zaczyna_sie_tam_gdzie_luk_prawie_prosty(self):
        # początek zebry od rogu jezdni: 0.85 + odsunięcie; krawężnik łuku cofa się tam ≤ 0.3 m
        a0 = .85 + max(0.0, RLUKU - ZPL)
        cofniecie = RLUKU - math.sqrt(max(0.0, RLUKU ** 2 - max(0.0, RLUKU - a0) ** 2))
        self.assertLessEqual(cofniecie, .3)

    def test_geometria_chodnik_przy_scianie_wylotu_wiaduktowej(self):
        # narożnik płd.-zach. J1: róg WARSZTATU (−72.5, 20.6) musi zostać na chodniku ≥ 1 m od krawężnika łuku
        P = (WX - WH, 24.0); C = (P[0] - RLUKU, P[1] - RLUKU)
        rog = (-72.5, 20.6)
        d = math.dist(C, rog)
        self.assertLessEqual(d, RLUKU - KR - 1.0)
        # narożnik płd.-wsch.: róg KANTORU (−61.4, 20.6)
        P = (WX + WH, 24.0); C = (P[0] + RLUKU, P[1] - RLUKU)
        self.assertLessEqual(math.dist(C, (-61.4, 20.6)), RLUKU - KR - 1.0)


class ResztaZakretow(unittest.TestCase):
    """Właściciel 16:32: „jeżeli zaokrąglona, to tak można skręcać… resztę zrób” — rondo, pętla, zakręt L, parking."""

    def test_wjazdy_rondo_i_petla_lukiem(self):
        pl = metoda(B, "static void PlacZPierscieniem(")
        self.assertIn("LukWlotu(c, dA, nA", pl)
        self.assertIn("LukWlotu(c, dB, nB", pl)
        self.assertIn("TriangulacjaUszu(pas)", pl)       # chodnik placu jako wielokąt (obrys z łukami wlotów)
        self.assertIn("lukiWlotow++", pl)
        self.assertGreaterEqual(stala(L, "RLukuWlotu"), 4.5)
        # geometria: łuk wjazdu mieści się w placu (R pierścienia 14.5, pół jezdni 4.5, plac 22)
        R, h, S, rf = 14.5, 4.5, 22.0, stala(L, "RLukuWlotu")
        a = math.sqrt((R + rf) ** 2 - (h + rf) ** 2)
        self.assertLess(a, S - .6)

    def test_zakret_L_bez_slepego_ogona(self):
        ub = metoda(B, "static void UliceBoczne()")
        self.assertIn('LukZew("Warsztatowa → Wiaduktowa (łuk zewnętrzny)"', ub)
        self.assertNotIn("Słupki blokujące wjazd na plac", ub)
        self.assertNotIn("Kraweznik(SX1, SX1 + Kr, SZ - SH, SZ + SH)", ub)
        self.assertIn("Odejmij(Rect.MinMaxRect(xa, SZ - SH, ox, SZ + SH)", ub)   # jezdnia kończy się na Wiaduktowej

    def test_luk_zewnetrzny_miesci_skret_w_lewo(self):
        # tor skrętu w lewo (BraxTrasyAut: Rl = clamp(lin + lout + 1.6, 4.5, 7)) + pół auta 1.25 wewnątrz koła łuku zewnętrznego
        ro = stala(B, "WarsztatowaLukZew")
        lin = WH * .5; Rl = min(7.0, max(4.5, lin + SH * .5 + 1.6))
        cur = (WX, SZ); C = (cur[0] - (Rl - SH * .5), cur[1] + (Rl - lin))
        O = (WX + WH, SZ - SH); Co = (O[0] - ro, O[1] + ro)
        d = (O[0] - C[0], O[1] - C[1]); m = math.hypot(*d)
        skraj = (C[0] + d[0] / m * (Rl + 1.25), C[1] + d[1] / m * (Rl + 1.25))
        self.assertLessEqual(math.dist(skraj, Co), ro - .3)

    def test_parking_supermarketu(self):
        plan = metoda(L, "static void LukiPlan()")
        self.assertIn("Aleja Płn. → parking supermarketu (wsch.)", plan)
        self.assertIn("Aleja Płn. → parking supermarketu (zach.)", plan)
        self.assertIn("PetlaZ - PetlaPlac - ZebraParkingu", B)


class SzerszeUlice(unittest.TestCase):
    def test_wiaduktowa_i_warsztatowa_dwa_pasy(self):
        self.assertGreaterEqual(2 * WH / 1.294, 5.75)   # ≥ 2 × 2,9 m realnie
        self.assertGreaterEqual(2 * SH / 1.294, 5.75)

    def test_miejsce_z_boku_warsztatu_spojne(self):
        arch = czytaj(ED / "CityRebuild/BraxCityArchitecture.cs")
        m = re.search(r'new Building\("workshop","WARSZTAT",-87,(-?[0-9.]+)f?,', arch)
        x1 = float(m.group(1))
        lico = stala(B, "WiaduktowaLicoZach")
        self.assertAlmostEqual(lico, x1 + .05, delta=.01)
        chodnik_zach = (WX - WH - KR) - lico
        chodnik_wsch = -61.45 - (WX + WH + KR)
        self.assertGreaterEqual(chodnik_zach, 1.5)
        self.assertGreaterEqual(chodnik_wsch, 1.4)
        self.assertNotIn('Chodnik("ul. Wiaduktowa zach.", -70.95f', B)


class RuchAut(unittest.TestCase):
    def test_linia_zatrzymania_za_zebra_tylko_w_miescie(self):
        t = czytaj(ED / "Claude/BraxTrasyAut.cs")
        self.assertIn("public static Func<Vector2, float> OdsuniecieZebr;", t)
        self.assertIn("+ .6f + Luk(cur);", t)
        r = czytaj(REG / "BraxRegionRuchMiasta.cs")
        self.assertIn("BraxTrasyAut.OdsuniecieZebr = c => BraxRegionMiastoB.OdsuniecieZebry(c);", r)
        self.assertIn("BraxTrasyAut.OdsuniecieZebr = lukPrev;", r)   # osiedle w tym samym procesie bez zmian

    def test_kazde_zakleszczenie_z_miejscem_w_logu(self):
        r = czytaj(REG / "BraxRegionRuchMiasta.cs")
        self.assertIn("BRAXHORN_RUCH_MIASTO_B_ZAKLESZCZENIE", r)
        self.assertIn("st.naZakleszczenie =", r)

    def test_autobus_konflikty_i_linie_jego_obrysem(self):
        s = czytaj(RT / "BraxRuchAutSym.cs")
        us = metoda(s, "public void UstawStart()")
        self.assertIn("KonfliktyDuzych();", us)
        kd = metoda(s, "void KonfliktyDuzych()")
        self.assertIn("Konflikty(dlT, szT, osT);", kd)
        self.assertIn("CofnijLinie(dlT, szT, 40, osT);", kd)
        self.assertIn("osTyl", kd)   # oś tylna autobusu (−0,4 dł.), nie ogólna osobówki


if __name__ == "__main__":
    unittest.main()
