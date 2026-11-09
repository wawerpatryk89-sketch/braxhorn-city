#!/usr/bin/env python3
"""Kontrola składni shaderów CG poza Unity (10.11): błąd w shaderze = różowe budynki po 5 godzinach buildu.

Każdy blok CGPROGRAM z podanych plików jest kompilowany przez glslangValidator (tryb HLSL) z atrapą UnityCG.cginc
(unity-tools/claude/shadery/UnityCG_atrapa.cginc: makra Unity, macierze, próbkowanie z LOD zastąpione zwykłym).
Łapie literówki, nieznane zmienne, złe typy i brakujące średniki. Nie zastępuje kompilacji Unity (warianty, platformy).

    sudo apt-get install glslang-tools
    python3 unity-tools/claude/sprawdz_shadery.py Lustro.shader SzkloFasady.shader
"""
import pathlib, re, subprocess, sys, tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
SH = ROOT / "unity-city/Assets/Claude/Shaders"
ATRAPA = (pathlib.Path(__file__).resolve().parent / "shadery/UnityCG_atrapa.cginc").read_text(encoding="utf-8")


def wstaw(kod: str) -> str:
    def inc(m):
        if m.group(1) == "UnityCG.cginc":
            return ATRAPA
        return wstaw((SH / m.group(1)).read_text(encoding="utf-8"))
    return re.sub(r'#include "([^"]+)"', inc, re.sub(r"#pragma[^\n]*", "", kod))


def main(pliki) -> int:
    zle = 0
    with tempfile.TemporaryDirectory() as tmp:
        for nazwa in pliki:
            zrodlo = (SH / nazwa).read_text(encoding="utf-8")
            for nr, blok in enumerate(re.findall(r"CGPROGRAM(.*?)ENDCG", zrodlo, re.S)):
                kod = wstaw(blok)
                for etap in ("vert", "frag"):
                    f = pathlib.Path(tmp) / f"{nazwa}_{nr}.{etap}.hlsl"
                    f.write_text(kod, encoding="utf-8")
                    r = subprocess.run(["glslangValidator", "-D", "-S", etap, "-e", etap, "--hlsl-iomap", "--auto-map-locations",
                                        "--auto-map-bindings", "-V", "-o", "/dev/null", str(f)], capture_output=True, text=True)
                    bledy = [l for l in (r.stdout + r.stderr).splitlines() if "ERROR" in l]
                    print(f"{nazwa} blok {nr} {etap}: {'OK' if r.returncode == 0 else 'BŁĄD'}")
                    for b in bledy[:8]:
                        print("   ", b)
                    zle += r.returncode != 0
    return 1 if zle else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:] or ["Lustro.shader", "SzkloFasady.shader"]))
