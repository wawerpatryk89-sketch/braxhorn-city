"""MIASTO R1 — 25.11 (uwagi właściciela 15:25–15:36 po APK 55c140a): wejścia bloków i wjazd z miasta na osiedle.

- Wieżowce (blok_32a): podest schodów przy drzwiach (był metr od ściany), daszek przy ścianie, doświetle piwnicy zamknięte
  z obu końców, żwir nad kostką, teren pod doświetlem nie jest wycinany (prześwit pod spód).
- Bloki 4-kondygnacyjne (blok4): wysunięcie klatki PRZED ścianą (było w środku bloku — drzwi niewidoczne), podest od drzwi.
- Aleja Północna miasta przechodzi w wjazd 1 osiedla (bez parkingu starego sklepu); jezdnia wjazdu = 2 × AH.
"""
import re
import sys
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "unity-tools/claude/osiedle_bdg"))

import blok4  # noqa: E402
import blok_32a as B  # noqa: E402


def _pudla(S, g, m):
    v = np.array(S.cz[(g, m)]["v"])
    return [v[i:i + 24] for i in range(0, len(v), 24)]


def test_32a_podest_przy_drzwiach_i_zejscie_do_piwnicy():
    S, _ = B.zbuduj("32a", 0)
    pudla = _pudla(S, "klatka", "beton_schody")
    podest = [p for p in pudla if abs(p[:, 1].max() - B.COKOL_Z) < 1e-3 and p[:, 1].min() < 0]
    assert podest and abs(podest[0][:, 2].min() - B.DL) < 1e-3, "podest schodów ma stać przy drzwiach"
    daszek = np.array(S.cz[("klatka", "beton_daszek")]["v"])
    assert abs(daszek[:, 2].min() - B.DL) < 1e-3, "daszek przy ścianie"
    # 25.11 (właściciel 15:35, 16:50): zejście schodami w dół do drzwi piwnicy, nie zamknięte doświetle
    v = np.array(S.cz[("klatka", "beton_schody")]["v"])
    assert v[:, 1].min() < -1.8, "dno zejścia ok. −2"
    assert ("klatka", "drzwi_stal") in S.cz, "drzwi piwnicy na dole zejścia"
    assert ("klatka", "zwir") not in S.cz
    # 25.11 17:21–17:37 (IMG_2117 przy 32a; „murek wystaje na ulicę”): drzwi w ścianie czołowej pod podestem wejścia,
    # patrzą wzdłuż wykopu; nic z klatki nie wychodzi poza narożnik budynku (x = 0)
    dv = np.array(S.cz[("klatka", "drzwi_stal")]["v"]); dn = np.array(S.cz[("klatka", "drzwi_stal")]["n"])
    assert abs(dv[:, 0].mean() - (B.SZ_POL - B.ZEJ_U0 + .08)) < 1e-3 and (dn[:, 0] < -.99).all()
    for (g, m), c in S.cz.items():
        if g == "klatka": assert np.array(c["v"])[:, 0].min() > -1e-6, (m, "wystaje za narożnik")


def test_teren_omija_zejscie_do_piwnicy():
    src = (ROOT / "unity-tools/claude/osiedle_bdg/osiedle.py").read_text(encoding="utf-8")
    kod = [ln for ln in src.split("def wiezowce")[1].split("\ndef ")[0].splitlines() if not ln.strip().startswith("#")]
    assert any("MODELE.append" in ln and "B.DL + B.ZEJ_D" in ln for ln in kod), "wykop zejścia wycięty z terenu"
    assert any("B.SZ_POL - B.ZEJ_U1" in ln for ln in kod), "wycięcie tylko na długości wykopu"


def test_blok4_klatka_wysunieta_przed_sciane():
    S, _ = blok4.zbuduj(47.2, 11.41, "30", 1, klatki=3, seed=30)
    kl = np.array(S.cz[("front", "klatka4")]["v"])
    drzwi = np.array(S.cz[("front", "drzwi_braz")]["v"])
    assert kl[:, 2].min() < -.3 and kl[:, 2].max() <= 1e-6, "wysunięcie klatki przed licem ściany (front patrzy na −z)"
    assert drzwi[:, 2].max() < 0, "drzwi przed ścianą bloku, nie w środku"
    # 25.11 17:01 (IMG_2124, blok 30): drzwi na poziomie chodnika, bez schodów na zewnątrz, daszek nad drzwiami,
    # jeden pionowy rząd okienek klatki co pół kondygnacji — żadne okienko nie wchodzi na drzwi ani daszek
    assert abs(drzwi[:, 1].min() - blok4.PROG) < 1e-3 and drzwi[:, 1].max() < 2.3
    schody = np.array(S.cz[("front", "beton_schody")]["v"])
    assert schody[:, 1].max() <= blok4.PROG + 1e-6, "tylko próg, bez schodów w górę"
    dasz = np.array(S.cz[("front", "beton_daszek")]["v"])
    assert dasz[:, 1].min() > drzwi[:, 1].max()
    ramy = np.array(S.cz[("front", "okno_klatka")]["v"]) if ("front", "okno_klatka") in S.cz else np.zeros((0, 3))
    c0 = 47.2 / 6                                                       # środek pierwszej klatki
    przed = ramy[np.abs(ramy[:, 0] - c0) < .4]
    assert len(przed) and przed[:, 1].min() > dasz[:, 1].max(), "okienka klatki nad daszkiem"


def test_aleja_polnocna_w_wjazd1_bez_parkingu():
    cs = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionMiastoB.cs").read_text(encoding="utf-8")
    assert "static bool Wjazd1 => BraxBydgoszcz.Wlaczone;" in cs
    blok = cs.split("if (Wjazd1)")[1].split("return;")[0]
    assert "DzialkaSupermarketu.yMax" in blok and "Asfalt(" not in blok and "MiejscaParkingu" not in blok
    luki = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxRegionMiastoBLuki.cs").read_text(encoding="utf-8")
    assert re.search(r"if \(!BraxBydgoszcz\.Wlaczone\)\s*\{\s*Luk\(\"Aleja Płn\. → parking", luki)
    os_ = (ROOT / "unity-tools/claude/osiedle_bdg/osiedle.py").read_text(encoding="utf-8")
    assert '"szer": 2 * 4.5 / 1.294' in os_, "jezdnia wjazdu 1 = jezdnia alei (2 × AH)"
    assert "public const float AX = -126f, AH = 4.5f;" in cs


def test_uskok_wiezowca_sciana_na_zewnatrz():
    """Uskok między połówkami wieżowca (15:39–15:40 „klatki, wszędzie”): ściana patrzy na zachód (na zewnątrz)."""
    S, _ = B.zbuduj("28a", 0)
    for (g, m), c in S.cz.items():
        if g != "uskok_N": continue
        v = np.array(c["v"]); i = np.array(c["i"]).reshape(-1, 3)
        n = np.cross(v[i[:, 1]] - v[i[:, 0]], v[i[:, 2]] - v[i[:, 0]])
        assert (n[:, 0] < 0).all(), "normalne uskoku na −X (zachód)"


def test_budynek_ogolny_ma_wejscie():
    import ogolny
    from shapely.geometry import box as _box, LineString as _LS
    S = ogolny.zbuduj(_box(0, 0, 30, 12), dict(kond=4, h_kond=3.0, tynk="tynk_szary", dojscia=_LS([(-5, -6), (40, -6)])), "t")
    assert ("t", "drzwi_stal") in S.cz and ("t", "beton_daszek") in S.cz and ("t", "beton_schody") in S.cz
    v = np.array(S.cz[("t", "beton_daszek")]["v"])
    assert v[:, 2].max() <= 1e-6, "wejście od strony drogi (−z)"


def test_wiata_szklo_przezroczyste_i_napis_szpitala():
    import paleta
    assert 0 < paleta.P["szyba_wiata"].get("przezr", 0) < 1
    assert (ROOT / "unity-city/Assets/Source/Claude/Bydgoszcz/napis_sor.png").exists()
    cs = (ROOT / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")
    assert "public float przezr;" in cs and "d.przezr > 0f" in cs


def test_okolice_32_i_parking_bez_paska():
    """25.11 16:57–18:18 (IMG_2118/2122/2123/2127/2128): zielony płot przy bloku 32, rząd przy 32a (wiata śmietnikowa,
    latarnia, 2 pojemniki na odzież, paczkomat); alejka przez parking jest parkingiem — bez pasa jezdni/kostki przez środek."""
    w = (ROOT / "unity-tools/claude/osiedle_bdg/wyposazenie.py").read_text(encoding="utf-8")
    assert "    okolice_32(S, d, G, ile)\n" in w
    for f in ("def plot_zielony", "def wiata_smietnikowa", "def kontener_odziez", "def blokada", "def kontener_1100"):
        assert f in w
    t = (ROOT / "unity-tools/claude/osiedle_bdg/teren.py").read_text(encoding="utf-8")
    assert "JEZ = unary_union(jezdnie).difference(alejki)" in t
    assert "KOS = unary_union(jezdnie_kostka).intersection(JEZ)" in t
    import json
    op = json.loads((ROOT / "unity-tools/claude/osiedle_bdg/gen/osiedle_opis.json").read_text(encoding="utf-8"))["wyposazenie"]
    assert op.get("plot_zielony_32") == 1 and op.get("wiaty_smietnikowe") == 1 and op.get("pojemniki_odziez") == 2


def test_parkingi_przy_blokach_kostka_bez_linii():
    """25.11 18:46–18:49 (satelita IMG_2164/2165, IMG_2128): parkingi przy blokach — szara kostka jak alejki, bez linii;
    asfalt z liniami tylko z OSM asphalt i przed marketem."""
    t = (ROOT / "unity-tools/claude/osiedle_bdg/teren.py").read_text(encoding="utf-8")
    assert 'if t.get("nawierzchnia") != "asphalt" and not przed_marketem: parkingi_kostka.append(p)' in t
    assert "for q in getattr(PARK_A, \"geoms\", [PARK_A]):" in t


def test_zaplecze_marketu_rampy():
    """25.11 (IMG_2134–2137): zaplecze marketu (o92, przyklejone od południa) — rampy z fartuchami, bramy, schody, czarny
    płot; plac rozładunku przed nim; bez sztucznego wejścia z daszkiem."""
    import json
    o = (ROOT / "unity-tools/claude/osiedle_bdg/ogolny.py").read_text(encoding="utf-8")
    assert "def market_tyl(" in o and 'if sp.get("zaplecze"):' in o and 'not sp.get("zaplecze")' in o
    assert "def market_bok_zach(" in o and "market_bok_zach(S, nazwa, P, H, att)" in o        # podcień boczny, paczkomat, schody
    op = json.loads((ROOT / "unity-tools/claude/osiedle_bdg/gen/osiedle_opis.json").read_text(encoding="utf-8"))["inne"]
    assert any("zaplecze marketu" in v.get("zrodlo", "") for v in op.values())
