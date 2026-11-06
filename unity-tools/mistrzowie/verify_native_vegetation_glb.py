"""Audit actual original Poly Haven 2K glTF and byte-preserving embedded GLB.

No Unity, scene changes, texture substitution or geometry changes.
"""
import hashlib
import io
import json
import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "unity-city"
ORIGINALS = PROJECT / "OriginalSources/Mistrzowie/CasinoT11/Vegetation2K"
IMPORTED = PROJECT / "Assets/Source/Mistrzowie/CasinoT11/Vegetation2K"
SLUGS = ("shrub_03", "quiver_tree_02")


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def verified_glb(path):
    raw = path.read_bytes()
    if len(raw) < 28 or struct.unpack_from("<4sII", raw, 0) != (b"glTF", 2, len(raw)):
        raise ValueError("Invalid GLB header: " + str(path))
    jlen, jtype = struct.unpack_from("<I4s", raw, 12)
    if jtype != b"JSON":
        raise ValueError("Missing GLB JSON chunk")
    doc = json.loads(raw[20:20+jlen])
    pos = 20+jlen
    blen, btype = struct.unpack_from("<I4s", raw, pos)
    if btype != b"BIN\x00" or pos+8+blen != len(raw):
        raise ValueError("Invalid embedded GLB binary chunk")
    return doc, raw[pos+8:], digest(raw)


def audit(slug):
    srcdir = ORIGINALS / slug
    source = srcdir / (slug + "_2k.gltf")
    output = IMPORTED / slug / (slug + "_2k_embedded.glb")
    if not source.is_file() or not output.is_file():
        raise FileNotFoundError("Missing actual original or lossless GLB: " + slug)
    if not Path(str(output) + ".meta").is_file():
        raise FileNotFoundError("Missing stable GUID for native GLB: " + slug)
    raw = source.read_bytes()
    original = json.loads(raw)
    derived, binary, glb_sha = verified_glb(output)
    for key in ("asset", "meshes", "nodes", "scenes", "scene", "materials",
                "textures", "accessors", "samplers", "skins", "animations",
                "extensionsUsed", "extensionsRequired"):
        if original.get(key) != derived.get(key):
            raise ValueError("Original source changed for " + slug + ": " + key)
    prior_views = original.get("bufferViews", [])
    produced_views = derived.get("bufferViews", [])
    if len(prior_views) > len(produced_views):
        raise ValueError("Original buffer views lost: " + slug)
    original_buffers = []
    for item in original["buffers"]:
        uri = item["uri"]
        if uri.startswith("/") or ":" in uri or ".." in uri or chr(92) in uri:
            raise ValueError("Unsafe original mesh buffer URI: " + uri)
        original_buffers.append((srcdir / uri).read_bytes()[:item["byteLength"]])
    for index, view in enumerate(prior_views):
        counterpart = produced_views[index]
        n = view["byteLength"]
        old = original_buffers[view["buffer"]]
        old_off = view.get("byteOffset", 0)
        new_off = counterpart.get("byteOffset", 0)
        if old[old_off:old_off+n] != binary[new_off:new_off+n]:
            raise ValueError("Original geometry bytes changed: " + slug + " bufferView " + str(index))
    images_checked = 0
    for image, embedded in zip(original.get("images", []), derived.get("images", [])):
        if "uri" not in image:
            continue
        src = srcdir / image["uri"]
        data = src.read_bytes()
        view = produced_views[embedded["bufferView"]]
        off = view["byteOffset"]
        if data != binary[off:off+view["byteLength"]]:
            raise ValueError("Original image bytes changed: " + str(src))
        with Image.open(io.BytesIO(data)) as decoded:
            if decoded.size != (2048, 2048):
                raise ValueError("Native Poly Haven texture is not 2K: " + str(src))
        images_checked += 1
    if images_checked < 3:
        raise ValueError("Expected original 2K diffuse, normal and ARM imagery for " + slug)
    return {
        "slug": slug, "originalSha256": digest(raw), "embeddedGlbSha256": glb_sha,
        "meshes": len(original["meshes"]), "checkedSourceViews": len(prior_views),
        "checkedOriginal2KImages": images_checked,
        "sourceGeometryPreserved": True, "sourceTexturesPreserved": True
    }


def main():
    results = [audit(slug) for slug in SLUGS]
    manifest = json.loads((ORIGINALS / "source-vegetation-2k.json").read_text())
    for entry in manifest["originalSourceFiles"]:
        file = PROJECT / entry["file"]
        if not file.is_file() or digest(file.read_bytes()) != entry["sha256"]:
            raise ValueError("Original Poly Haven manifest source changed: " + entry["file"])
    if len(manifest["losslessEmbeddedModels"]) != 2:
        raise ValueError("Both lossless native GLBs required")
    report = {"status": "PASS_REAL_NATIVE_POLYHAVEN_2K",
              "models": results, "sourceFileCount": len(manifest["originalSourceFiles"]),
              "originalBytesModified": False, "unityImported": False, "apkBuilt": False}
    evidence = ROOT / "mistrzowie-evidence"
    evidence.mkdir(exist_ok=True)
    (evidence / "t11-real-2k-source-audit.json").write_text(json.dumps(report, indent=2))
    print(json.dumps(report))


if __name__ == "__main__":
    main()
