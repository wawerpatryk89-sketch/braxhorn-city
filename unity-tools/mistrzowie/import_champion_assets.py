"""Opt-in import of the owner-supplied original championship belt and four signs.

The complete 108MB archive contains original belt, four signs and 2K original VIP GLB.
The binary remains a separate conversation artifact until explicitly imported.
This script DOES NOT fetch, build, regenerate or alter original visual assets.
"""
import argparse
import hashlib
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DESTINATION = ROOT / "unity-city"
ARCHIVE_SHA256 = "453a5fa9cd637d6a2a733893c453faa12914dc2610f69bf73dd3d5f65fbfcef1"
BASE = "Assets/Source/Mistrzowie/"
EXPECTED = {
    BASE + "ArenaSigns/champion_arena_red_front.png",
    BASE + "ArenaSigns/champion_arena_gold_front.png",
    BASE + "ArenaSigns/champion_arena_red_fight_night.png",
    BASE + "ArenaSigns/champion_arena_gold_arena_walk.png",
    BASE + "Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx",
    BASE + "Trophies/ChampionshipBelt_ARM.png",
    BASE + "VIP/free_luxurious_barrel_seat_2k.glb",
    BASE + "Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/Color.png",
    BASE + "Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/Normal.png",
    BASE + "Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/championship_belt_3d_model_metallic.PNG",
    BASE + "Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/championship_belt_3d_model_roughness.PNG",
}


def hash_file(path):
    digest = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(4 * 1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def install(archive: Path, dry_run: bool = False):
    if not archive.is_file():
        raise FileNotFoundError(f"Original asset archive missing: {archive}")
    actual_hash = hash_file(archive)
    if actual_hash != ARCHIVE_SHA256:
        raise RuntimeError(f"Archive SHA256 mismatch. Expected {ARCHIVE_SHA256}; got {actual_hash}")
    with zipfile.ZipFile(archive) as z:
        if z.testzip():
            raise RuntimeError("Source archive CRC failed")
        names = set(z.namelist())
        if not EXPECTED <= names:
            raise RuntimeError(f"Missing original files: {sorted(EXPECTED-names)}")
        if names - (EXPECTED | {"ASSET-MANIFEST.json", "IMPORT-README.md"}):
            raise RuntimeError("Archive contains unexpected files: " + repr(sorted(names-EXPECTED)))
        verified = []
        for name in sorted(EXPECTED):
            # Explicit fixed name allowlist prevents ZIP traversal.
            target = DESTINATION / name
            if target.exists():
                data = z.read(name)
                if hashlib.sha256(target.read_bytes()).digest() != hashlib.sha256(data).digest():
                    raise RuntimeError("Existing original asset differs; refusing overwrite: " + str(target))
                verified.append("UNCHANGED " + name)
            else:
                verified.append(("DRY-RUN " if dry_run else "INSTALL ") + name)
        if dry_run:
            return verified
        for name in sorted(EXPECTED):
            target = DESTINATION / name
            if target.exists():
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            tmp = target.with_suffix(target.suffix + ".temporary")
            with z.open(name) as f, open(tmp, "wb") as out:
                while True:
                    chunk = f.read(4 * 1024 * 1024)
                    if not chunk:
                        break
                    out.write(chunk)
            tmp.replace(target)
        return verified


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("archive", type=Path, help="verified champions_arena_A1_full_import.zip with VIP chair")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    for line in install(args.archive, args.dry_run):
        print(line)
    print("CHAMPION_OWNER_ASSETS_VALIDATED: no Unity build launched")
