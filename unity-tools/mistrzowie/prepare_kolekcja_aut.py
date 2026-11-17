"""Copy three cars of the estate collection unchanged into the region before Unity (owner 17.11 00:20: "podstaw z 3 auta z
naszej kolekcji"): Porsche 918 Spyder, Audi R8, Dodge Challenger - the same GLBs as the estate's car salon, tracked at
public/assets/cars-src. Each is pinned by its git blob hash and size: a changed source is refused, never substituted.
Stable .meta GUIDs (uuid5 of the path, as prepare_champion_car.py). BraxChampionsKolekcjaAut builds them driveable on the
A1 car park; a car missing here is reported there (DO POPRAWKI KOLEKCJA_AUT) and the build goes on."""
import hashlib
import json
import struct
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "public/assets/cars-src"
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/Vehicles"
NS = uuid.UUID("93ae6172-879d-4ea9-9dda-11f20578c2d5")
CARS = {
    "car_918.glb": ("fcc12f87c41cd665fd1e2c9e8d3292df3add280d", 1494016, "2026 Porsche 918 Spyder"),
    "car_r8.glb": ("011bac86bd8dc471908e014c55ce3f0822b89659", 1210028, "2010 Audi R8"),
    "car_dc.glb": ("f24c51f3f759bae3880240661be5ede3c6301d89", 1879084, "2015 Dodge Challenger"),
}


def blob(data):
    return hashlib.sha1(b"blob " + str(len(data)).encode("ascii") + b"\0" + data).hexdigest()


def meta(target):
    rel = target.relative_to(ROOT / "unity-city").as_posix()
    return ("fileFormatVersion: 2\nguid: " + uuid.uuid5(NS, rel).hex + "\n").encode("utf-8")


def check(name, data):
    sha, size, title = CARS[name]
    if len(data) != size or blob(data) != sha:
        raise ValueError("changed collection car, refused: " + name)
    if data[:4] != b"glTF" or struct.unpack_from("<I", data, 4)[0] != 2 or struct.unpack_from("<I", data, 8)[0] != len(data):
        raise ValueError("not a glTF 2 GLB: " + name)
    n = struct.unpack_from("<I", data, 12)[0]
    doc = json.loads(data[20:20 + n])
    if title not in json.dumps(doc.get("asset", {}), ensure_ascii=False) and title.split(" ", 1)[1] not in json.dumps(doc, ensure_ascii=False)[:4000]:
        raise ValueError("unexpected car in " + name)
    return doc


def prepare(dry_run=False):
    planned = {}
    for name in CARS:
        data = (SRC / name).read_bytes()
        check(name, data)
        target = DEST / name
        planned[target] = data
        planned[Path(str(target) + ".meta")] = meta(target)
    if not (DEST.parent / (DEST.name + ".meta")).exists():
        planned[DEST.parent / (DEST.name + ".meta")] = meta(DEST)
    for target, raw in planned.items():
        if target.is_file() and target.read_bytes() != raw:
            raise ValueError("existing file differs, approved bytes preserved: " + str(target))
    if not dry_run:
        for target, raw in planned.items():
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(raw)
    print("KOLEKCJA_AUT_SOURCES " + ("DRY " if dry_run else "") + " ".join(sorted(CARS)) + " unchanged")


if __name__ == "__main__":
    prepare("--dry-run" in sys.argv)
