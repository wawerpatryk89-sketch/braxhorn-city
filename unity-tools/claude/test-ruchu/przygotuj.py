"""Stage only Patryk for the short Unity input test, from the production source."""
from pathlib import Path
import os
import sys
import tempfile
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import pobierz_obsade as obsada

target = obsada.ROOT / "unity-city/Assets/Source/ObsadaOsiedla/patryk"
target.parent.mkdir(parents=True, exist_ok=True)
with tempfile.TemporaryDirectory(dir=target.parent) as temp:
    work = Path(temp)
    archive = work / "source.zip"
    wybrana = obsada.pobierz(obsada.PACZKA, os.environ["GITHUB_REPOSITORY"], archive)
    staged = work / "source"
    model = staged / "patryk/patryk.fbx"
    blender = Path(os.environ["RUNNER_TEMP"]) / "blender-4.2.3-linux-x64/blender"
    if wybrana == obsada.GOTOWA_PACZKA:
        bar = work / "bar" / "source"
        obsada.odczytaj_gotowa(archive, staged, bar)
        obsada.dodatki_osiedla(target.parent.parent / "Prywatne", blender, z_flota=False, gotowy_bar=bar)
    else:
        obsada.odczytaj(archive, staged)
        obsada.dodatki_osiedla(target.parent.parent / "Prywatne", blender, z_flota=False)
        obsada.popraw_model(model, work / model.name, blender)
    obsada.zainstaluj(staged / "patryk", target)

from osiedle_mowa import prepare
resources = obsada.ROOT / "unity-city/Assets/Resources"
prepare(resources/"OsiedleGlosy", resources/"OsiedleMimika", "9a79ffe04cb33bdb_m.ogg")
