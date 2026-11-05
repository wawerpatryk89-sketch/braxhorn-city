#!/usr/bin/env python3
"""Safe pilot PBR channel packing for Unity 6 Android.

No original asset is edited, no scene/build is started. Monika uses one 4K
metallic/roughness pack, and the other six characters use one 2K pack each.
The existing clean_asphalt 4K roughness/AO pilot remains. Imported source
PBR maps are retained unmodified as a visual fallback.

Packed MR: R = source metallic.r, G = source roughness.r, B = 255.
Packed RA: R = source roughness.r, G = source occlusion.r, B = 255.
Both are LINEAR data maps. The red source channel is kept byte-for-byte
before platform ASTC compression. Shader keywords retain a legacy fallback.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
PROJECT = Path("unity-city")
OUTPUT = Path("unity-city/Assets/PlayableGenerated/Oboz/PackedPbr")
HERO_4K = "monika"
OTHER_2K = ("vorn", "zyro", "rakk", "kade", "gorr", "mis")
PILOT_SURFACE = "clean_asphalt"
SUPPORTED = {"RGB", "RGBA", "L", "LA", "P", "1"}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def source(root, unity_path, expected=None):
    # Editor manifest paths must be confined to the Unity Assets tree.
    rel = Path(unity_path)
    if (rel.is_absolute() or ".." in rel.parts or
            not unity_path.startswith("Assets/Source/")):
        raise ValueError("Unsafe source texture: " + unity_path)
    path = (root / PROJECT / rel).resolve()
    home = (root / PROJECT / "Assets/Source").resolve()
    if not path.is_relative_to(home) or not path.is_file():
        raise ValueError("Missing/outside source texture: " + unity_path)
    raw = path.read_bytes()
    sha = digest(raw)
    if expected is not None and sha != expected:
        raise ValueError("Source SHA mismatch: " + unity_path)
    return raw, sha


def image_channel(raw, image_id):
    from PIL import Image
    with Image.open(io.BytesIO(raw)) as picture:
        picture.load()
        if picture.mode not in SUPPORTED:
            raise ValueError("Non-8bit map requires manual review: " + image_id +
                             " (" + picture.mode + ")")
        data = picture.convert("RGB")
        return data.getchannel("R")


def combine_channels(first_red, second_red):
    from PIL import Image
    if first_red.size != second_red.size:
        raise ValueError("Cannot pack different map dimensions")
    # No scaling, interpolation, gamma curve or colour transform.
    return Image.merge("RGB", (first_red, second_red,
                               Image.new("L", first_red.size, 255)))


def make_meta(path, root):
    meta = path.with_name(path.name + ".meta")
    if not meta.exists():
        asset_path = path.relative_to(root / PROJECT).as_posix()
        guid = digest(("braxhorn-pbr-pilot:" + asset_path).encode())[:32]
        meta.write_text("fileFormatVersion: 2\nguid: " + guid + "\n")


def atomic_write(path, data):
    if path.is_file() and path.read_bytes() == data:
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + ".tmp")
    try:
        temp.write_bytes(data)
        temp.replace(path)
    finally:
        temp.unlink(missing_ok=True)
    return True


def pack_pair(root, *, asset_id, role, first, second, known_shas, target_size):
    """Pack pinned original red channels at target size, never upscale the sources."""
    from PIL import Image
    if not re.fullmatch(r"[a-z0-9_-]{2,40}", asset_id):
        raise ValueError("Untrusted pack identifier")
    if role not in ("MR", "RA"):
        raise ValueError("Unrecognized packed PBR role")
    raw_a, sha_a = source(root, first, known_shas.get(first))
    raw_b, sha_b = source(root, second, known_shas.get(second))
    # All current pilot inputs have pinned SHA-256. Do not silently accept an
    # unverified file substituted into the encrypted/public delivery.
    if first not in known_shas or second not in known_shas:
        raise ValueError("Unpinned PBR source for " + asset_id)
    a = image_channel(raw_a, first)
    b = image_channel(raw_b, second)
    if a.size != b.size or target_size not in (2048, 4096):
        raise ValueError("Unexpected source size/target for PBR: " + asset_id)
    if a.width < target_size or a.height < target_size:
        raise ValueError("Would artificially upscale low-detail PBR source: " + asset_id)
    if a.size != (target_size, target_size):
        a = a.resize((target_size, target_size), Image.Resampling.LANCZOS)
        b = b.resize((target_size, target_size), Image.Resampling.LANCZOS)
    # Input order is deliberate: MR=(metallic,roughness); RA=(roughness,AO).
    packed = combine_channels(a, b)
    buffer = io.BytesIO()
    packed.save(buffer, format="PNG", optimize=True)
    packed_bytes = buffer.getvalue()
    dest = root / OUTPUT / (asset_id + "-" + role + ".png")
    atomic_write(dest, packed_bytes)
    make_meta(dest, root)
    unit = "Assets/PlayableGenerated/Oboz/PackedPbr/" + dest.name
    return dict(id=asset_id, role=role, first=first, second=second,
                firstSha256=sha_a, secondSha256=sha_b,
                packedPath=unit, packedSha256=digest(packed_bytes),
                width=packed.width, height=packed.height,
                sourceBytes=len(raw_a) + len(raw_b), packedBytes=len(packed_bytes))


def run(root=ROOT):
    manifest = json.loads((root / PROJECT /
        "Assets/Editor/Oboz/CharacterManifest.json").read_text())
    people = {c["id"]: c for c in manifest["characters"]}
    char = people[HERO_4K]
    private = json.loads((root / "unity-tools/oboz/private-inputs.json").read_text())
    public = json.loads((root / "unity-tools/oboz/public-materials.json").read_text())
    sha = {"Assets/Source/Prywatne/Oboz/" + f["path"]: f["sha256"]
           for f in private["files"]}
    for f in public["files"]:
        # public-materials.json may include archive-sourced images elsewhere;
        # these two pilot source paths are listed as pinned standalone files.
        sha[f["path"].removeprefix("unity-city/")] = f["sha256"]
    result = []
    result.append(pack_pair(root, asset_id=HERO_4K, role="MR",
                            first=char["metallicPath"], second=char["roughnessPath"],
                            known_shas=sha, target_size=4096))
    for character_id in OTHER_2K:
        character = people[character_id]
        result.append(pack_pair(root, asset_id=character_id, role="MR",
                                first=character["metallicPath"],
                                second=character["roughnessPath"],
                                known_shas=sha, target_size=2048))
    prefix = "Assets/Source/Public/Oboz/" + PILOT_SURFACE + "/"
    result.append(pack_pair(root, asset_id=PILOT_SURFACE, role="RA",
                            first=prefix + "Roughness.jpg",
                            second=prefix + "AmbientOcclusion.jpg",
                            known_shas=sha, target_size=4096))
    index = dict(schema=1, pilotOnly=True, originalsPreserved=True,
                 unity="6000.0.58f2", character4K=[HERO_4K],
                 characters2K=list(OTHER_2K), entries=result)
    target = root / OUTPUT / "pack-index.json"
    atomic_write(target, (json.dumps(index, indent=2, ensure_ascii=False) + "\n").encode())
    print("BRAXHORN_OBOZ_PBR_PACK hero4k=monika others2k=6 packed_maps=" +
          str(len(result)) + " sources_preserved=true maps=" +
          ",".join(e["packedPath"] for e in result))
    return index


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    args = parser.parse_args()
    run(args.root.resolve())


if __name__ == "__main__":
    main()
