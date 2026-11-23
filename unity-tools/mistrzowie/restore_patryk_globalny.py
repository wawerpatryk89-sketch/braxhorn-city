"""The newest Patryk for Miasto Mistrzów: the global character library's Patryk of 13.11 (the one Osiedle plays).

Owner 23.11 15:47 / 15:54: "Patryka dałeś złą wersję, to nie jest ten najnowszy ... stara do wyrzucenia" - "dać nowego".
The package is the owner's private draft release (postacie-globalne-20261112, never published); nothing of it goes into
git. It is installed to Assets/Source/Mistrzowie/Player/Postacie/patryk (git-ignored), where BraxChampionsPatrykImport
imports it and BraxChampionsPatrykGlobalny builds the player. Every check is fatal: a build without the new Patryk would
spend 5.5 hours to deliver the old one again.
"""
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[2]
CEL = ROOT / "unity-city/Assets/Source/Mistrzowie/Player/Postacie/patryk"
# the same draft asset and checksums as the Osiedle cast (unity-tools/claude/obsada/osiedle.json on claude/osiedle-42-poprawka)
PACZKA = {
    "release_id": 443180649,
    "tag": "postacie-globalne-20261112",
    "name": "Patryk_globalny_13-11_Unity.zip",
    "sha256": "30ff44d34c5c45b1a8876e1266396b57faca6537e6d6f73600f152f6cea06887",
}
PREFIKS = "unity-city/Assets/Source/Prywatne/Postacie/patryk/"
MANIFEST_SHA = "22350070cc2d0537cbc94937d68c8b41eb1480e3a457aad639cc5ac407a275c7"
MODEL_SHA = "eacee0beb02fac53587337edee09f790577a700464448bf797d2ed65a2c66d1f"   # BraxClaudeOsiedlePatryk on Osiedle
RUCHY = ["Walk", "Run", "Jump", "EnteringCar", "CarIdle", "ExitingCar", "CombatIdle", "CombatForward", "CombatBackward",
         "CombatLeft", "Punch", "Punch2", "Punch3", "Kick", "Kick2", "Kick3", "Dodge", "Block", "HitHead", "HitBody",
         "Knockdown", "GetUp", "Bow", "Taunt", "Victory", "Defeat", "WarmUp", "Roll", "GolfDrive", "GolfPutt", "Speedbag"]


def sha(path):
    with open(path, "rb") as f:
        return hashlib.file_digest(f, "sha256").hexdigest()


def pobierz(cel):
    repo = os.environ["GITHUB_REPOSITORY"]
    api = f"repos/{repo}/releases"
    release = json.loads(subprocess.check_output(["gh", "api", f"{api}/{PACZKA['release_id']}"]))
    if release["draft"] is not True or release["tag_name"] != PACZKA["tag"]:
        raise ValueError("expected the private draft " + PACZKA["tag"])
    assets = [a for a in release["assets"] if a["name"] == PACZKA["name"] and a["state"] == "uploaded"]
    if len(assets) != 1:
        raise FileNotFoundError(PACZKA["name"] + " not in the draft " + PACZKA["tag"])
    if assets[0].get("digest") and assets[0]["digest"] != "sha256:" + PACZKA["sha256"]:
        raise ValueError("the draft's archive differs from the approved one")
    with open(cel, "wb") as out:
        subprocess.run(["gh", "api", f"{api}/assets/{assets[0]['id']}", "-H", "Accept: application/octet-stream"],
                       stdout=out, check=True)


def rozpakuj(archiwum, cel):
    if sha(archiwum) != PACZKA["sha256"]:
        raise ValueError("wrong checksum of " + PACZKA["name"])
    with ZipFile(archiwum) as z:
        nazwy = [n for n in z.namelist() if not n.endswith("/")]
        if not nazwy or any(not n.startswith(PREFIKS) for n in nazwy):
            raise ValueError("a file outside " + PREFIKS)
        for n in nazwy:
            wzgl = n[len(PREFIKS):]
            out = (cel / wzgl).resolve()
            if cel.resolve() not in out.parents:
                raise ValueError("a path outside the character folder: " + n)
            out.parent.mkdir(parents=True, exist_ok=True)
            with z.open(n) as src, out.open("xb") as dst:
                shutil.copyfileobj(src, dst)


def sprawdz(cel):
    manifest = cel / "postac.json"
    if sha(manifest) != MANIFEST_SHA:
        raise ValueError("postac.json differs from the approved Patryk 2026-11-13")
    m = json.loads(manifest.read_text(encoding="utf-8"))
    if m.get("id") != "patryk" or m.get("version") != "2026-11-13" or m.get("rig") != "Mixamo" or m.get("bones") != 65:
        raise ValueError("not the global Patryk 2026-11-13 (Mixamo, 65 bones)")
    if list(m.get("motions", {})) != RUCHY:
        raise ValueError("the motion list differs: " + ", ".join(m.get("motions", {})))
    if sha(cel / "Patryk.fbx") != MODEL_SHA:
        raise ValueError("Patryk.fbx is not the 13.11 model")
    pliki = m.get("files", {})
    wymagane = {"Patryk.fbx", "Patryk_BaseColor.png", "Patryk_Normal.png", "Patryk_ORM.png"} | {"Ruchy/" + r + ".fbx" for r in RUCHY}
    if set(pliki) != wymagane:
        raise ValueError("the file list differs from the model, 3 maps and 31 motions")
    for wzgl, suma in pliki.items():          # every file byte-checked against the manifest
        if not (cel / wzgl).is_file() or sha(cel / wzgl) != suma:
            raise ValueError("a changed or missing file: " + wzgl)
    if sorted(str(p.relative_to(cel)) for p in cel.rglob("*") if p.is_file()) != sorted(wymagane | {"postac.json"}):
        raise ValueError("an extra file in the package")


def main():
    CEL.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=CEL.parent) as tmp:
        tmp = Path(tmp)
        archiwum = tmp / PACZKA["name"]
        pobierz(archiwum)
        staged = tmp / "patryk"
        staged.mkdir()
        rozpakuj(archiwum, staged)
        sprawdz(staged)
        if CEL.exists():
            shutil.rmtree(CEL)
        staged.rename(CEL)
    print("BRAXHORN_PATRYK_GLOBALNY 2026-11-13: Patryk.fbx + " + str(len(RUCHY)) + " ruchów + 3 mapy 2K, SHA-256 zgodne -> "
          + str(CEL.relative_to(ROOT)))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:   # the message only; nothing private is printed
        print("::error::BRAXHORN_PATRYK_GLOBALNY " + type(e).__name__ + ": " + str(e))
        sys.exit(1)
