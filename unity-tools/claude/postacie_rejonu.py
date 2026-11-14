"""Install only the global character packages selected for the requested region."""
import hashlib
import json
from pathlib import Path
import shutil
import tempfile

REGIONS = Path(__file__).resolve().parent / "obsada"


def digest(file):
    return hashlib.sha256(file.read_bytes()).hexdigest()


def valid(directory, selection):
    manifest = directory / "postac.json"
    if not manifest.is_file() or digest(manifest) != selection["manifest_sha256"]:
        return False
    model = json.loads(manifest.read_text(encoding="utf-8"))
    return (model["id"] == selection["id"] and model["version"] == selection["version"]
            and all((directory / path).is_file() and digest(directory / path) == expected
                    for path, expected in model["files"].items()))


def przygotuj_obsade(region, target, decrypt):
    cast = json.loads((REGIONS / (region + ".json")).read_text())
    if cast["player"] not in {c["id"] for c in cast["characters"]}:
        raise ValueError("Brak gracza w obsadzie rejonu: " + region)
    destination = target / "Postacie"
    destination.mkdir(parents=True, exist_ok=True)
    for selection in cast["characters"]:
        directory = destination / selection["id"]
        if valid(directory, selection):
            continue
        archive = decrypt(selection["package"])
        if archive is None:
            raise FileNotFoundError("Brak paczki postaci " + selection["id"] + " " + selection["version"])
        with archive, tempfile.TemporaryDirectory(dir=destination.parent) as tmp:
            staged = Path(tmp) / "source"
            staged.mkdir()
            for member in archive.getmembers():
                output = (staged / member.name).resolve()
                if not member.isfile() or staged not in output.parents:
                    raise ValueError("Niepoprawna ścieżka paczki postaci")
                output.parent.mkdir(parents=True, exist_ok=True)
                with archive.extractfile(member) as src, output.open("wb") as dst:
                    shutil.copyfileobj(src, dst)
            if not valid(staged, selection):
                raise ValueError("Paczka nie odpowiada obsadzie rejonu: " + selection["id"])
            previous = Path(tmp) / "previous"
            if directory.exists():
                directory.rename(previous)
            try:
                staged.rename(directory)
            except OSError:
                if previous.exists():
                    previous.rename(directory)
                raise
