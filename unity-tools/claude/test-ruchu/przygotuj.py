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
    obsada.pobierz(obsada.PACZKA, os.environ["GITHUB_REPOSITORY"], archive)
    staged = work / "source"
    obsada.odczytaj(archive, staged)
    model = staged / "patryk/patryk.fbx"
    blender = Path(os.environ["RUNNER_TEMP"]) / "blender-4.2.3-linux-x64/blender"
    obsada.dodatki_osiedla(target.parent.parent / "Prywatne", blender, z_flota=False)
    obsada.popraw_model(model, work / model.name, blender)
    (staged / "patryk").rename(target)

from osiedle_mowa import prepare
resources = obsada.ROOT / "unity-city/Assets/Resources"
prepare(resources/"OsiedleGlosy", resources/"OsiedleMimika", "9a79ffe04cb33bdb_m.ogg")
