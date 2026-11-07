"""Non-destructive UV compatibility for the owner's P1 towel GLB.

Unused extra UV declarations may be removed. A material using UV2+ may be
redirected to UV0/1 ONLY after exact per-vertex byte equality is demonstrated
on every primitive using that material. Original BIN, geometry, texture bytes,
accessors and the separate owner source archive stay unchanged. No Unity run.
"""
from __future__ import annotations
import copy
import hashlib
import json
import re
import struct


class UnsafeTowelUvConversion(ValueError):
    """A change could affect appearance; retain the owner's source."""


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _parse_glb(raw: bytes):
    if len(raw) < 20:
        raise UnsafeTowelUvConversion("P1 towel GLB is too short")
    magic, version, size = struct.unpack_from("<4sII", raw, 0)
    if magic != b"glTF" or version != 2 or size != len(raw):
        raise UnsafeTowelUvConversion("P1 towel GLB v2 header/length is invalid")
    chunks, pos = [], 12
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
    if isinstance(value, dict):
        for key, item in value.items():
            if key == "texCoord":
                if not isinstance(item, int) or isinstance(item, bool) or item < 0:
                    raise UnsafeTowelUvConversion("Unexpected material texCoord selector")
                yield item
            else:
                yield from _texture_coords(item)
    elif isinstance(value, list):
        for item in value:
            yield from _texture_coords(item)


def _uv_bytes(doc, chunks, index):
    """Conservative proof reader: dense, uncompressed FLOAT VEC2 in GLB BIN."""
    try:
        if type(index) is not int or index < 0:
            raise ValueError("invalid accessor index")
        acc = doc["accessors"][index]
        if acc.get("type") != "VEC2" or acc.get("componentType") != 5126:
            raise ValueError("UV proof requires FLOAT VEC2")
        if acc.get("sparse") or acc.get("normalized") or acc.get("extensions"):
            raise ValueError("unsupported UV accessor encoding")
        view = doc["bufferViews"][acc["bufferView"]]
        buffers = doc["buffers"]
        bins = [data for kind, data in chunks if kind == b"BIN\x00"]
        if view.get("buffer", 0) != 0 or len(bins) != 1 or buffers[0].get("uri") or view.get("extensions"):
            raise ValueError("UV proof requires uncompressed embedded buffer zero")
        count, offset = acc["count"], acc.get("byteOffset", 0)
        start, length = view.get("byteOffset", 0), view["byteLength"]
        stride = view.get("byteStride", 8)
        if any(type(x) is not int for x in (count, offset, start, length, stride)):
            raise ValueError("invalid UV dimensions")
        if not 0 < count <= 5000000 or min(offset, start, length) < 0 or not 8 <= stride <= 252 or stride % 4:
            raise ValueError("invalid UV bounds/stride")
        if offset + (count - 1) * stride + 8 > length or start + length > min(len(bins[0]), buffers[0]["byteLength"]):
            raise ValueError("UV data outside original buffer")
        begin = start + offset
        if stride == 8:
            return bins[0][begin:begin + count * 8]
        return b"".join(bins[0][begin + i * stride:begin + i * stride + 8] for i in range(count))
    except (KeyError, IndexError, TypeError, ValueError) as error:
        raise UnsafeTowelUvConversion("UV alias not proven; conversion could change appearance: " + str(error)) from error


def _remap(value, mapping):
    if isinstance(value, dict):
        for key, item in value.items():
            if key == "texCoord" and item in mapping:
                value[key] = mapping[item]
            else:
                _remap(item, mapping)
    elif isinstance(value, list):
        for item in value:
            _remap(item, mapping)


def _alias_referenced_uvs(doc, derivative, chunks):
    proofs, cached = [], {}
    def packed(index):
        if index not in cached:
            cached[index] = _uv_bytes(doc, chunks, index)
        return cached[index]
    for material_id, material in enumerate(doc.get("materials", [])):
        extra = sorted({coord for coord in _texture_coords(material) if coord >= 2})
        if not extra:
            continue
        primitives = [(mi, pi, p) for mi, mesh in enumerate(doc.get("meshes", []))
                      for pi, p in enumerate(mesh.get("primitives", [])) if p.get("material") == material_id]
        # Variant-only bindings need a separate proof; do not guess ownership.
        if not primitives or any("KHR_materials_variants" in p.get("extensions", {})
                                 for mesh in doc.get("meshes", []) for p in mesh.get("primitives", [])):
            raise UnsafeTowelUvConversion("Material UV binding unproven; could change appearance")
        mapping = {}
        for coord in extra:
            proved = None
            for target in (0, 1):
                rows = []
                for mi, pi, primitive in primitives:
                    attrs = primitive["attributes"]
                    old, new = "TEXCOORD_" + str(coord), "TEXCOORD_" + str(target)
                    if old not in attrs or new not in attrs or packed(attrs[old]) != packed(attrs[new]):
                        break
                    data = packed(attrs[old])
                    rows.append({"mesh": mi, "primitive": pi, "material": material_id,
                                 "from": coord, "to": target, "vertices": len(data) // 8,
                                 "identicalUvSha256": sha256(data)})
                else:
                    proved = rows
                    mapping[coord] = target
                    break
            if proved is None:
                raise UnsafeTowelUvConversion("P1 towel material references different TEXCOORD_2+; conversion would change appearance")
            proofs.extend(proved)
        _remap(derivative["materials"][material_id], mapping)
    return proofs


def derive_safe_uv_import(raw: bytes):
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
            extra = [name for name in attrs if re.fullmatch(r"TEXCOORD_([2-9]|[1-9][0-9]+)", name)]
            if extra and "KHR_draco_mesh_compression" in primitive.get("extensions", {}):
                raise UnsafeTowelUvConversion("Draco-compressed P1 towel extra UVs need manual mesh review")
            removed.extend({"mesh": mi, "primitive": pi, "attribute": name} for name in extra)
    coords = sorted(set(_texture_coords(doc.get("materials", []))))
    if not removed:
        return raw, {"status": "ALREADY_COMPATIBLE", "removedUnusedUvAttributes": [],
                     "originalSha256": sha256(raw), "importSha256": sha256(raw),
                     "originalBinaryChunksPreserved": True, "originalTexturesPreserved": True}
    derivative = copy.deepcopy(doc)
    proofs = _alias_referenced_uvs(doc, derivative, chunks) if any(coord >= 2 for coord in coords) else []
    for item in removed:
        del derivative["meshes"][item["mesh"]]["primitives"][item["primitive"]]["attributes"][item["attribute"]]
    new_json = json.dumps(derivative, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    new_json += b" " * ((-len(new_json)) % 4)
    body = b"".join(struct.pack("<I4s", len(data), kind) + data
                    for kind, data in [(b"JSON", new_json)] + chunks[1:])
    result = struct.pack("<4sII", b"glTF", 2, 12 + len(body)) + body
    check, after = _parse_glb(result)
    if check != derivative or after[1:] != chunks[1:]:
        raise UnsafeTowelUvConversion("P1 towel derived GLB verification failed")
    return result, {
        "status": "DUPLICATE_UV_CHANNELS_ALIASED_WITH_BYTE_PROOF" if proofs else "UNUSED_UV_CHANNELS_REMOVED_FROM_IMPORT_COPY",
        "removedUnusedUvAttributes": removed, "identicalUvProofs": proofs,
        "materialTexCoordSelectors": coords,
        "importMaterialTexCoordSelectors": sorted(set(_texture_coords(derivative.get("materials", [])))),
        "originalSha256": sha256(raw), "importSha256": sha256(result),
        "originalBinaryChunksPreserved": True, "originalTexturesPreserved": True,
        "originalVisualsNotReplaced": True, "unityTested": False,
    }
