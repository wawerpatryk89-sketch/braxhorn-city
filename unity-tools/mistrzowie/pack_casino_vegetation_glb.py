"""Lossless glTF 2.0 -> embedded GLB repack of original Poly Haven geometry.

Copies exact .bin mesh bytes and original 2K image bytes; no geometry,
material, UV, transforms, colors, or textures are regenerated or resized.
The original .gltf/.bin/images remain in unity-city/OriginalSources,
outside Unity's automatic broken external-URI glTF importer.
"""
from pathlib import Path, PurePosixPath
import base64
import hashlib
import json
import struct


def _read_uri(root: Path, uri: str) -> bytes:
    if uri.startswith("data:"):
        header, separator, raw = uri.partition(",")
        if not separator or not header.endswith(";base64"):
            raise ValueError("Only base64 data URIs supported in source glTF")
        return base64.b64decode(raw, validate=True)
    pure = PurePosixPath(uri)
    if pure.is_absolute() or ".." in pure.parts or ":" in uri or "\\" in uri:
        raise ValueError("Unsafe external glTF source path: " + uri)
    resolved = (root / uri).resolve()
    if not resolved.is_relative_to(root.resolve()):
        raise ValueError("External glTF reference escapes source folder: " + uri)
    if not resolved.is_file():
        raise FileNotFoundError("Original Poly Haven external source absent: " + str(resolved))
    return resolved.read_bytes()


def _pad(data: bytearray, byte: int = 0) -> None:
    while len(data) % 4:
        data.append(byte)


def pack_embedded_original(source: Path, output: Path) -> dict:
    raw_source = source.read_bytes()
    gltf = json.loads(raw_source)
    if gltf.get("asset", {}).get("version") != "2.0":
        raise ValueError("Not glTF 2.0: " + str(source))
    buffers = gltf.get("buffers", [])
    images = gltf.get("images", [])
    if not buffers or not images or not gltf.get("meshes"):
        raise ValueError("Original glTF lacks meshes, buffer or 2K images")
    binary = bytearray()
    old_buffer_offsets = []
    for buf in buffers:
        uri = buf.get("uri")
        if not isinstance(uri, str):
            raise ValueError("Expected external original glTF buffer URI")
        data = _read_uri(source.parent, uri)
        expected = buf.get("byteLength")
        if not isinstance(expected, int) or expected <= 0 or expected > len(data):
            raise ValueError("Original glTF buffer length differs")
        _pad(binary)
        old_buffer_offsets.append(len(binary))
        binary.extend(data[:expected])
    for view in gltf.get("bufferViews", []):
        index = view["buffer"]
        if index >= len(old_buffer_offsets) or index < 0:
            raise ValueError("Invalid original bufferView index")
        view["byteOffset"] = old_buffer_offsets[index] + view.get("byteOffset", 0)
        view["buffer"] = 0
    source_images = []
    for item in images:
        if "uri" not in item:
            # Embedded original image; its bufferView was already relocated.
            if "bufferView" not in item:
                raise ValueError("Original image has no URI or bufferView")
            continue
        uri = item["uri"]
        data = _read_uri(source.parent, uri)
        path = uri.split(",", 1)[0] if uri.startswith("data:") else uri.lower()
        if path.endswith((".jpg", ".jpeg")):
            mime = "image/jpeg"
        elif path.endswith(".png"):
            mime = "image/png"
        else:
            raise ValueError("Unexpected original Poly Haven texture type: " + uri)
        _pad(binary)
        offset = len(binary)
        binary.extend(data)
        item["bufferView"] = len(gltf.setdefault("bufferViews", []))
        item["mimeType"] = mime
        del item["uri"]
        gltf["bufferViews"].append({"buffer": 0, "byteOffset": offset, "byteLength": len(data)})
        source_images.append({"uri": uri, "sha256": hashlib.sha256(data).hexdigest()})
    _pad(binary)
    gltf["buffers"] = [{"byteLength": len(binary)}]
    json_raw = bytearray(json.dumps(gltf, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    _pad(json_raw, 32)
    size = 12 + 8 + len(json_raw) + 8 + len(binary)
    if size > 4294967295:
        raise ValueError("Embedded Poly Haven GLB exceeds glTF 2.0 byte limit")
    content = (struct.pack("<4sII", b"glTF", 2, size) +
               struct.pack("<I4s", len(json_raw), b"JSON") + json_raw +
               struct.pack("<I4s", len(binary), b"BIN\x00") + binary)
    if output.is_file():
        if output.read_bytes() != content:
            raise ValueError("Generated embedded GLB differs from original source; do not overwrite: " + str(output))
    else:
        output.parent.mkdir(parents=True, exist_ok=True)
        tmp = output.with_name(output.name + ".partial")
        try:
            tmp.write_bytes(content)
            tmp.replace(output)
        finally:
            tmp.unlink(missing_ok=True)
    return {
        "originalGltf": str(source),
        "originalSha256": hashlib.sha256(raw_source).hexdigest(),
        "embeddedGlb": str(output),
        "glbSha256": hashlib.sha256(content).hexdigest(),
        "images": source_images,
        "meshes": len(gltf["meshes"]),
        "binaryUnchanged": True,
        "sourceImagesUnchanged": True,
        "sourceGltfUnchanged": True,
    }
