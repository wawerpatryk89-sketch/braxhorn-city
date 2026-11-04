"""Restore the exact estate Patryk and animation sources for Champions exploration.
Source e93d6c01f1c64fc5527987c9a0a7a2c57daf86e8. Private files stay encrypted in git.
"""
import hashlib
import io
import json
import shutil
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "unity-tools/claude"))
import prywatne
from import_patryk_transfer import restore
CEL = ROOT / "unity-city/Assets/Source/Mistrzowie/Player"

def pack_orm(roughness, metallic):
    """Preserve each source data texel: R=1, G=roughness, B=metallic.

    These owner maps are grayscale RGB(A). Reject color/mismatched maps rather
    than resampling or silently converting their numeric PBR values.
    """
    from PIL import Image, ImageChops
    if roughness.size != metallic.size:
        raise ValueError("Roughness/metallic dimensions differ")
    channels = []
    for im in (roughness, metallic):
        r, g, b = im.convert("RGB").split()
        if ImageChops.difference(r, g).getbbox() or ImageChops.difference(r, b).getbbox():
            raise ValueError("Expected original grayscale PBR map")
        channels.append(r)
    return Image.merge("RGB", (Image.new("L", roughness.size, 255), *channels))

def przygotuj_patryka(staging):
    from PIL import Image  # Pillow instalowany w kroku „Prepare unchanged player…”
    fbx = sorted((staging / "patryk").glob("*.fbx"))
    if len(fbx) != 1 or hashlib.sha256(fbx[0].read_bytes()).hexdigest() != "fb8e2630bc7f835a743d917c35c378c7d074f0fde993a0bbf864b31ab67e1d97":
        raise ValueError("Wymagany nowy Patryk.zip z 3.11.2026; stary model wycofany")
    dst = CEL / "patryk"
    # Usuń wyłącznie wygenerowaną kopię poprzedniej postaci, nigdy źródłową paczkę.
    if dst.exists():
        shutil.rmtree(dst)
    dst.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(fbx[0], dst / "Patryk.fbx")
    # Nowy eksport ma oryginały także w katalogu .fbm. Weryfikacja bajtów
    # zapobiega cichej podmianie map lub użyciu starej, mniejszej tekstury.
    expected = {
        "Color.png": "8b0cf4736e771bddf1ae71e61b48da39ca241106624de263c6d0106008e57ced",
        "Normal.png": "dc5a8ea5b277f13404a03b8a62b7d943367ef6676270410f596f510265f783f4",
        "Patryk_roughness.PNG": "29bdaa8e3170015ccaf7670d33f30de6177f3fb82faa1e5e8ded80a766ffa883",
        "Patryk_metallic.PNG": "da4e601c36031e514abad412cb350d52275fda67430b68116d9c9b676707541a",
    }
    obrazy = {}
    for name, checksum in expected.items():
        candidates = [p for p in (staging / "patryk").rglob("*") if p.is_file() and p.name == name]
        if len(candidates) != 1:
            raise ValueError("Brak lub duplikat oryginalnej mapy: " + name)
        data = candidates[0].read_bytes()
        if hashlib.sha256(data).hexdigest() != checksum:
            raise ValueError("Zmieniona oryginalna mapa: " + name)
        with Image.open(io.BytesIO(data)) as im:
            if im.size != (4096, 4096):
                raise ValueError("Źródłowa mapa nie jest 4K: " + name)
        obrazy[name] = data

    (dst / "Patryk_BaseColor.png").write_bytes(obrazy["Color.png"])
    (dst / "Patryk_Normal.png").write_bytes(obrazy["Normal.png"])
    with Image.open(io.BytesIO(obrazy["Patryk_roughness.PNG"])) as rough, \
         Image.open(io.BytesIO(obrazy["Patryk_metallic.PNG"])) as metal:
        pack_orm(rough, metal).save(dst / "Patryk_ORM.png")
    print(f"BRAXHORN_PRYWATNE Patryk: FBX {fbx[0].stat().st_size / 1048576:.1f} MB, "
          "Color/Normal/ORM 4K; oryginalne wartości PBR, bez zmniejszania map")
    return True


ANIMATIONS = {
    "brax": ["Idle", "Jab Cross (1)", "Mma Kick (1)", "Jump"],
    "extra": ["Walking", "Running"],
    "extra2": ["Walking", "Unarmed Run Forward", "Unarmed Jump Running"],
    "nyxa": ["Walking", "Mma Kick (1)"],
    "xd": ["Jab Cross"],
}
REQUIRED = {
    "CombatIdle": ["brax/Idle.fbx"],
    "Walk": ["extra/Walking.fbx", "nyxa/Walking.fbx", "extra2/Walking.fbx"],
    "Run": ["extra/Running.fbx", "extra2/Unarmed Run Forward.fbx"],
    "Punch": ["brax/Jab Cross (1).fbx", "xd/Jab Cross.fbx"],
    "Kick": ["brax/Mma Kick (1).fbx", "nyxa/Mma Kick (1).fbx"],
    "Jump": ["brax/Jump.fbx", "extra2/Unarmed Jump Running.fbx"],
}


def copy_animations(staging, destination):
    for clip, candidates in REQUIRED.items():
        if not any((staging / path).is_file() for path in candidates):
            raise RuntimeError("Missing original animation: " + clip)
    files = []
    for group, names in ANIMATIONS.items():
        for name in names:
            source = staging / group / (name + ".fbx")
            if not source.is_file():
                continue
            target = destination / group / source.name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, target)
            files.append(target)
    return files


def main():
    if not prywatne.klucz():
        raise RuntimeError("Missing repository key for the required estate Patryk")
    restore()
    with tempfile.TemporaryDirectory(prefix="champions-patryk-") as tmp:
        staging = Path(tmp)
        for name in ("patryk", "ruchy_brax", "ruchy_wlasciciela"):
            archive = prywatne.odszyfruj(name)
            if archive is None:
                raise RuntimeError("Missing or invalid original package: " + name)
            with archive:
                if prywatne.rozpakuj(archive, staging) < 1:
                    raise RuntimeError("Empty original package: " + name)
        przygotuj_patryka(staging)
        files = [{"path": str(target.relative_to(ROOT)), "sha256": hashlib.sha256(target.read_bytes()).hexdigest()}
                 for target in copy_animations(staging, CEL / "ruchy")]
        for name in ("Patryk.fbx", "Patryk_BaseColor.png", "Patryk_Normal.png", "Patryk_ORM.png"):
            target = CEL / "patryk" / name
            files.append({"path": str(target.relative_to(ROOT)), "sha256": hashlib.sha256(target.read_bytes()).hexdigest()})
    evidence = ROOT / "mistrzowie-evidence"
    evidence.mkdir(exist_ok=True)
    (evidence / "patryk-sources.json").write_text(json.dumps({
        "estateSourceCommit": "e93d6c01f1c64fc5527987c9a0a7a2c57daf86e8",
        "modelHeightWorld": 2.33, "files": files, "unityValidated": False,
    }, indent=2) + "\n")


if __name__ == "__main__":
    main()
