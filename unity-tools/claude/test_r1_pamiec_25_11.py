"""MIASTO R1 — 25.11 (gra wylatuje po starcie, scena główna 2128 MB w build-size 55c140a).

Modele właściciela z paczek szyfrowanych (Prywatne/Miasto) mają obrazy wbudowane w .glb — glTFast robił z nich
nieskompresowane RGBA32 (autobus 119 MB, auta komisu 62–94 MB). Odchudzanie w CI wyjmuje je do plików
(_tekstury_glb, kompresja platformy, 1K na telefonie). Raport rozmiaru wypisuje, co waży każdy obszar.
"""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def test_odchudzanie_obejmuje_modele_wlasciciela_miasta():
    src = (ROOT / "unity-tools/claude/odchudzanie_modeli.py").read_text(encoding="utf-8")
    lista = src.split("GLB_DIRS = [")[1].split("]")[0]
    assert '"Source/Prywatne/Miasto"' in lista


def test_tekstury_glb_maja_limit_na_telefon():
    cs = (ROOT / "unity-city/Assets/Editor/Claude/BraxClaudeGlbTekstury.cs").read_text(encoding="utf-8")
    assert '"/_tekstury_glb/"' in cs and "ps.maxTextureSize = 1024" in cs


def test_raport_rozmiaru_wypisuje_zawartosc_obszarow():
    cs = (ROOT / "unity-city/Assets/Editor/BraxGymBuild.cs").read_text(encoding="utf-8")
    assert '"  W OBSZARZE "' in cs and "Take(40)" in cs
