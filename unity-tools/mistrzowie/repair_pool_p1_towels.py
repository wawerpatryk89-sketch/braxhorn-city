"""Non-destructive UV compatibility for the owner's P1 towel GLB.

glTFast/Unity refused TEXCOORD_2 and TEXCOORD_3 on the original 2K towel
asset. A compatible *derived* import may omit ONLY unreferenced extra UV
attribute declarations. Every source byte remains in the approved owner ZIP
and an out-of-Assets original archive; BIN, meshes, topology, textures,
materials, nodes and supported TEXCOORD_0/1 data are unchanged.

No Unity or Android compilation is started by this module.
"""
from __future__ import annotations

import copy
import hashlib
import json
import re
import struct


class UnsafeTowelUvConversion(ValueError):
    """A change could affect the displayed appearance; preserve the original."""


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _parse_glb(raw: bytes):
    if len(raw) < 20:
        raise UnsafeTowelUvConversion("P1 towel GLB is too short")
    magic, version, size = struct.unpack_from("<4sII", raw, 0)
    if magic != b"glTF" or version != 2 or size != len(raw):
        raise UnsafeTowelUvConversion("P1 towel GLB v2 header/length is invalid")
    chunks = []
    pos = 12
    while pos < len(raw):
        if pos + 8 > len(raw):
            raise UnsafeTowelUvConversion("P1 towel GLB truncated chunk header")
        length, kind = struct.unpack_from("<I4s", raw, pos)
        pos += 8
        if length % 4 or pos + length > len(raw):
            raise UnsafeTowelUvConversion("P1 towel GLB chunk length is invalid")
        chunks.append((kind, raw[pos:pos + length]))
        pos += length
    if not chunks or chunks[0][0] != b"JSON" or sum(k == b"JSON" for k, _ in chunks) != 1:
        raise UnsafeTowelUvConversion("P1 towel GLB needs exactly one leading JSON chunk")
    try:
        doc = json.loads(chunks[0][1])
    except (TypeError, ValueError, UnicodeDecodeError) as exc:
        raise UnsafeTowelUvConversion("P1 towel GLB JSON is invalid") from exc
    if not isinstance(doc, dict) or doc.get("asset", {}).get("version") != "2.0":
        raise UnsafeTowelUvConversion("P1 towel requires glTF 2.0 document")
    return doc, chunks


def _texture_coords(value):
    """Read every explicit texture-info UV selector, including extensions."""
    if isinstance(value, dict):
        for key, item in value.items():
            if key == "texCoord":
                if not isinstance(item, int) or isinstance(item, bool):
                    raise UnsafeTowelUvConversion("Unexpected material texCoord selector")
                yield item
            else:
                yield from _texture_coords(item)
    elif isinstance(value, list):
        for item in value:
            yield from _texture_coords(item)


def derive_safe_uv_import(raw: bytes):
    """Return (importable_bytes, report) without modifying original GLB bytes.

    If a material actually uses TEXCOORD_2+, refuse any UV removal.
    """
    doc, chunks = _parse_glb(raw)
    meshes = doc.get("meshes", [])
    if not isinstance(meshes, list):
        raise UnsafeTowelUvConversion("P1 towel meshes must be an array")
    removed = []
    for mi, mesh in enumerate(meshes):
        if not isinstance(mesh, dict):
            raise UnsafeTowelUvConversion("Invalid P1 towel mesh")
        for pi, primitive in enumerate(mesh.get("primitives", [])):
            if not isinstance(primitive, dict):
                raise UnsafeTowelUvConversion("Invalid P1 towel primitive")
            attrs = primitive.get("attributes", {})
            if not isinstance(attrs, dict):
                raise UnsafeTowelUvConversion("Invalid P1 towel attributes")
            extra = [name for name in attrs
                     if re.fullmatch(r"TEXCOORD_([2-9]|[1-9][0-9]+)", name)]
            if extra and "KHR_draco_mesh_compression" in primitive.get("extensions", {}):
                raise UnsafeTowelUvConversion(
                    "Draco-compressed P1 towel extra UVs need manual mesh review")
            for name in extra:
                removed.append({"mesh": mi, "primitive": pi, "attribute": name})
    coords = sorted(set(_texture_coords(doc.get("materials", []))))
    if removed and any(coord >= 2 for coord in coords):
        raise UnsafeTowelUvConversion(
            "P1 towel material references TEXCOORD_2+; removing it would change appearance")
    if not removed:
        return raw, {
            "status": "ALREADY_COMPATIBLE", "removedUnusedUvAttributes": [],
            "originalSha256": sha256(raw), "importSha256": sha256(raw),
            "originalBinaryChunksPreserved": True, "originalTexturesPreserved": True,
        }
    derivative = copy.deepcopy(doc)
    for item in removed:
        attrs = derivative["meshes"][item["mesh"]]["primitives"][item["primitive"]]["attributes"]
        del attrs[item["attribute"]]
    # Keep the entire binary chunk and the glTF bufferView/accessor references.
    # Removing only unused primitive-attribute selectors is sufficient and
    # preserves all geometry, colours, normals, material and texture bytes.
    new_json = json.dumps(derivative, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    new_json += b" " * ((-len(new_json)) % 4)
    new_chunks = [(b"JSON", new_json)] + chunks[1:]
    body = b"".join(struct.pack("<I4s", len(data), kind) + data for kind, data in new_chunks)
    result = struct.pack("<4sII", b"glTF", 2, 12 + len(body)) + body
    check, check_chunks = _parse_glb(result)
    if check != derivative or check_chunks[1:] != chunks[1:]:
        raise UnsafeTowelUvConversion("P1 towel derived GLB verification failed")
    return result, {
        "status": "UNUSED_UV_CHANNELS_REMOVED_FROM_IMPORT_COPY",
        "removedUnusedUvAttributes": removed,
        "materialTexCoordSelectors": coords,
        "originalSha256": sha256(raw), "importSha256": sha256(result),
        "originalBinaryChunksPreserved": True, "originalTexturesPreserved": True,
        "originalVisualsNotReplaced": True,
    }
