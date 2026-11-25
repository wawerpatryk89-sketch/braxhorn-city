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


def test_32a_podest_przy_drzwiach_i_doswietle_zamkniete():
    S, _ = B.zbuduj("32a", 0)
    pudla = _pudla(S, "klatka", "beton_schody")
    # podest: najwyższy stopień (do +1,00) zaczyna się na licu ściany (z = DL)
    podest = [p for p in pudla if abs(p[:, 1].max() - B.COKOL_Z) < 1e-3 and p[:, 1].min() < 0]
    assert podest and abs(podest[0][:, 2].min() - B.DL) < 1e-3, "podest schodów ma stać przy drzwiach"
    # oba końce doświetla: ścianki od lica ściany do murka
    konce = [p for p in pudla if abs(p[:, 2].min() - B.DL) < 1e-3 and p[:, 1].max() < .5 and (p[:, 0].max() - p[:, 0].min()) < .2]
    assert len(konce) == 2, "doświetle piwnicy zamknięte z obu końców"
    zw = np.array(S.cz[("klatka", "zwir")]["v"])
    assert zw[:, 1].min() > .12, "żwir doświetla nad kostką (+0,12), nic na wylot"
    daszek = np.array(S.cz[("klatka", "beton_daszek")]["v"])
    assert abs(daszek[:, 2].min() - B.DL) < 1e-3, "daszek przy ścianie"


def test_osiedle_nie_wycina_terenu_pod_doswietlem():
    src = (ROOT / "unity-tools/claude/osiedle_bdg/osiedle.py").read_text(encoding="utf-8")
    kod = [ln for ln in src.split("def wiezowce")[1].split("\ndef ")[0].splitlines() if not ln.strip().startswith("#")]
    assert not any("MODELE.append" in ln and "B.DL + 1.39" in ln for ln in kod), "teren pod doświetlem zostaje"


def test_blok4_klatka_wysunieta_przed_sciane():
    S, _ = blok4.zbuduj(47.2, 11.41, "30", 1, klatki=3, seed=30)
    kl = np.array(S.cz[("front", "klatka4")]["v"])
    drzwi = np.array(S.cz[("front", "drzwi_braz")]["v"])
    assert kl[:, 2].min() < -.3 and kl[:, 2].max() <= 1e-6, "wysunięcie klatki przed licem ściany (front patrzy na −z)"
    assert drzwi[:, 2].max() < 0, "drzwi przed ścianą bloku, nie w środku"
    schody = np.array(S.cz[("front", "beton_schody")]["v"])
    assert abs(schody[:, 2].max() - kl[:, 2].min()) < 1e-3, "podest od lica wysunięcia (drzwi)"


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
