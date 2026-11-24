#!/usr/bin/env python3
"""MIASTO R1 — skrót plików gen Bydgoszczy (osiedle_bdg/gen, rynek_bdg/gen) w unity-city/Assets/Source/Claude/Bydgoszcz/
gen_skrot.json. Wczytywacz (BraxBydgoszcz) czyta gen spoza Assets, a klucz pamięci rejonu w CI (rejon_buildu.py) liczy
tylko pliki z unity-city/Assets — bez tego skrótu nowy osiedle.bin / rynek.bin przy niezmienionym kodzie dałby trafienie
w starą pamięć rejonu (stary świat w buildzie). Uruchomić po każdym skopiowaniu gen (test_r1_bydgoszcz_32a pilnuje).
    python3 unity-tools/claude/bydgoszcz_skrot.py
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GEN = [ROOT / "unity-tools/claude/osiedle_bdg/gen", ROOT / "unity-tools/claude/rynek_bdg/gen"]
CEL = ROOT / "unity-city/Assets/Source/Claude/Bydgoszcz/gen_skrot.json"


def skrot() -> dict:
    out = {}
    for g in GEN:
        for p in sorted(g.glob("*")):
            if p.is_file(): out[p.relative_to(ROOT).as_posix()] = hashlib.sha256(p.read_bytes()).hexdigest()
    return out


if __name__ == "__main__":
    CEL.write_text(json.dumps(skrot(), indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    print(CEL.relative_to(ROOT), len(skrot()), "plików")
