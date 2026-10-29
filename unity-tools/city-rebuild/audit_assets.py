#!/usr/bin/env python3
"""Download selected original Poly Haven files and audit them before Unity use.

No scene, renderer, player, production deployment or existing source is changed.
All API responses and downloaded bytes are retained; glTF files are never rewritten.
The inventory describes glTF coordinates, not Unity's mirrored X convention.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import io
import json
import math
import os
from pathlib import Path, PurePosixPath
import struct
import sys
import time
from urllib.parse import unquote, urlparse
from urllib.request import Request, urlopen


API = "https://api.polyhaven.com"
MODELS = ("modular_urban_apartments_facade", "modular_factory_facade")
TEXTURES = ("brick_wall_001", "clean_asphalt")
ALLOWED_HOSTS = {"api.polyhaven.com", "dl.polyhaven.org", "cdn.polyhaven.com"}
MAX_FILE_BYTES = 512 * 1024 * 1024
MAX_TOTAL_BYTES = 1536 * 1024 * 1024
HEADERS = {"User-Agent": "BraxhornCitySourceAudit/1.0 (GitHub Actions)"}
IDENTITY = [1., 0., 0., 0., 0., 1., 0., 0., 0., 0., 1., 0., 0., 0., 0., 1.]


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def official_url(url):
    parsed = urlparse(url)
    if parsed.scheme != "https" or parsed.hostname not in ALLOWED_HOSTS:
        raise ValueError(f"Non-official download URL rejected: {url}")
    return url


def safe_relative(value):
    value = unquote(value)
    path = PurePosixPath(value)
    if not value or path.is_absolute() or ".." in path.parts or "\\" in value or urlparse(value).scheme:
        raise ValueError(f"Unsafe glTF dependency path: {value}")
    return Path(*path.parts)


class Downloader:
    def __init__(self, root):
        self.root = root
        self.files = []
        self.total = 0

    def fetch(self, url, target, expected=None):
        official_url(url)
        target.parent.mkdir(parents=True, exist_ok=True)
        temporary = target.with_name(target.name + ".download")
        for attempt in range(3):
            try:
                size = 0
                sha = hashlib.sha256()
                md5 = hashlib.md5(usedforsecurity=False)
                with urlopen(Request(url, headers=HEADERS), timeout=75) as response, temporary.open("wb") as out:
                    official_url(response.url)
                    while block := response.read(1024 * 1024):
                        size += len(block)
                        if size > MAX_FILE_BYTES or self.total + size > MAX_TOTAL_BYTES:
                            raise ValueError("Curated source download exceeded its explicit size budget")
                        sha.update(block)
                        md5.update(block)
                        out.write(block)
                if expected and expected.get("md5") and md5.hexdigest() != expected["md5"].lower():
                    raise ValueError(f"API MD5 mismatch for {url}")
                if expected and isinstance(expected.get("size"), int) and size != expected["size"]:
                    raise ValueError(f"API byte count mismatch for {url}: {size} != {expected['size']}")
                temporary.replace(target)
                self.total += size
                record = {"path": str(target.relative_to(self.root)), "url": url,
                          "bytes": size, "sha256": sha.hexdigest(), "md5": md5.hexdigest(),
                          "api_md5_verified": bool(expected and expected.get("md5"))}
                self.files.append(record)
                return target
            except Exception:
                temporary.unlink(missing_ok=True)
                if attempt == 2:
                    raise
                time.sleep(attempt + 1)

    def api(self, endpoint, target):
        self.fetch(f"{API}/{endpoint}", target)
        return json.loads(target.read_text())


def file_entries(value, path=()):
    """Yield official API file objects, retaining their location and includes."""
    if not isinstance(value, dict):
        return
    if isinstance(value.get("url"), str):
        yield path, value
    for key, child in value.items():
        if isinstance(child, dict):
            yield from file_entries(child, path + (key,))


def select_model(manifest, resolution):
    candidates = [(path, item) for path, item in file_entries(manifest)
                  if urlparse(item["url"]).path.lower().endswith(".gltf")
                  and resolution in path and "gltf" in [part.lower() for part in path]]
    if len(candidates) != 1:
        raise ValueError(f"Expected one {resolution} glTF; found {len(candidates)}. See saved API manifest.")
    return candidates[0]


def dependency_entry(uri, selected, manifest, resolution):
    # Relative URIs must be explained by the official manifest. No guessed URL.
    relative = str(safe_relative(uri)).replace(os.sep, "/")
    candidates = []
    for path, item in file_entries(selected):
        key = "/".join(path)
        if key.endswith(relative) or unquote(urlparse(item["url"]).path).endswith("/" + relative):
            candidates.append(item)
    if not candidates:
        for path, item in file_entries(manifest):
            if resolution in path and unquote(urlparse(item["url"]).path).endswith("/" + relative):
                candidates.append(item)
    unique = {item["url"]: item for item in candidates}
    if len(unique) != 1:
        raise ValueError(f"Dependency {uri!r} has {len(unique)} verified API matches")
    return next(iter(unique.values()))


def image_info(path):
    try:
        from PIL import Image
        with Image.open(path) as image:
            return {"width": image.width, "height": image.height, "mode": image.mode,
                    "format": image.format}
    except Exception as error:
        return {"inspection_error": str(error)}


def matmul(a, b):
    return [sum(a[k * 4 + row] * b[col * 4 + k] for k in range(4))
            for col in range(4) for row in range(4)]


def local_matrix(node):
    if "matrix" in node:
        return node["matrix"]
    x, y, z, w = node.get("rotation", [0, 0, 0, 1])
    sx, sy, sz = node.get("scale", [1, 1, 1])
    tx, ty, tz = node.get("translation", [0, 0, 0])
    return [(1-2*y*y-2*z*z)*sx, (2*x*y+2*z*w)*sx, (2*x*z-2*y*w)*sx, 0,
            (2*x*y-2*z*w)*sy, (1-2*x*x-2*z*z)*sy, (2*y*z+2*x*w)*sy, 0,
            (2*x*z+2*y*w)*sz, (2*y*z-2*x*w)*sz, (1-2*x*x-2*y*y)*sz, 0,
            tx, ty, tz, 1]


def combine_bounds(bounds):
    bounds = [entry for entry in bounds if entry]
    if not bounds:
        return None
    low = [min(entry["min"][i] for entry in bounds) for i in range(3)]
    high = [max(entry["max"][i] for entry in bounds) for i in range(3)]
    return {"min": low, "max": high, "size": [high[i] - low[i] for i in range(3)]}


def transform_bounds(bounds, matrix):
    if not bounds:
        return None
    points = []
    for x in (bounds["min"][0], bounds["max"][0]):
        for y in (bounds["min"][1], bounds["max"][1]):
            for z in (bounds["min"][2], bounds["max"][2]):
                points.append([matrix[i] * x + matrix[4+i] * y + matrix[8+i] * z + matrix[12+i]
                               for i in range(3)])
    return combine_bounds([{"min": point, "max": point} for point in points])


def load_gltf(path):
    """Read original .gltf or .glb without altering, externalizing or recompressing it."""
    binary = None
    if path.suffix.lower() == ".glb":
        raw = path.read_bytes()
        if raw[:4] != b"glTF" or struct.unpack_from("<I", raw, 4)[0] != 2:
            raise ValueError("Expected glTF 2 binary")
        if struct.unpack_from("<I", raw, 8)[0] != len(raw):
            raise ValueError("Incomplete glTF binary")
        offset = 12
        gltf = None
        while offset < len(raw):
            length, kind = struct.unpack_from("<II", raw, offset)
            chunk = memoryview(raw)[offset + 8:offset + 8 + length]
            if kind == 0x4E4F534A:
                gltf = json.loads(chunk.tobytes())
            elif kind == 0x004E4942:
                binary = chunk
            offset += 8 + length
        if gltf is None:
            raise ValueError("Missing glTF JSON chunk")
    else:
        gltf = json.loads(path.read_text())
    buffers = []
    for buffer in gltf.get("buffers", []):
        uri = buffer.get("uri", "")
        if not uri and binary is not None:
            buffers.append(binary)
        elif uri.startswith("data:"):
            import base64
            buffers.append(base64.b64decode(uri.partition(",")[2]))
        else:
            buffers.append((path.parent / safe_relative(uri)).read_bytes())
    return gltf, buffers


def image_source(gltf, buffers, entry, base):
    if "bufferView" in entry:
        view = gltf["bufferViews"][entry["bufferView"]]
        offset = view.get("byteOffset", 0)
        return io.BytesIO(buffers[view["buffer"]][offset:offset + view["byteLength"]])
    uri = entry.get("uri", "")
    if uri.startswith("data:"):
        import base64
        return io.BytesIO(base64.b64decode(uri.partition(",")[2]))
    return base / safe_relative(uri)


def inspect_gltf(path):
    gltf, buffers = load_gltf(path)
    def positions(accessor_index):
        accessor = gltf["accessors"][accessor_index]
        if accessor.get("sparse") or accessor.get("componentType") != 5126 or accessor.get("type") != "VEC3":
            raise ValueError(f"Position accessor {accessor_index} requires an unsupported decoding path")
        view = gltf["bufferViews"][accessor["bufferView"]]
        start = view.get("byteOffset", 0) + accessor.get("byteOffset", 0)
        stride = view.get("byteStride", 12)
        data = buffers[view["buffer"]]
        low, high = [math.inf] * 3, [-math.inf] * 3
        for index in range(accessor["count"]):
            point = struct.unpack_from("<3f", data, start + index * stride)
            if not all(math.isfinite(value) for value in point):
                raise ValueError("Non-finite position in source mesh")
            low = [min(low[i], point[i]) for i in range(3)]
            high = [max(high[i], point[i]) for i in range(3)]
        return combine_bounds([{"min": low, "max": high}]) if accessor["count"] else None
    meshes = []
    for index, mesh in enumerate(gltf.get("meshes", [])):
        primitives = []
        for primitive in mesh.get("primitives", []):
            attrs = primitive.get("attributes", {})
            position = attrs.get("POSITION")
            vertex_count = gltf["accessors"][position]["count"] if position is not None else 0
            indices = gltf["accessors"][primitive["indices"]]["count"] if "indices" in primitive else vertex_count
            mode = primitive.get("mode", 4)
            triangles = indices // 3 if mode == 4 else max(0, indices - 2) if mode in (5, 6) else 0
            primitives.append({"vertices": vertex_count, "indices": indices, "triangles": triangles,
                               "mode": mode, "material": primitive.get("material"),
                               "attributes": sorted(attrs),
                               "bounds": positions(position) if position is not None else None})
        meshes.append({"index": index, "name": mesh.get("name", ""), "primitives": primitives,
                       "bounds": combine_bounds([p["bounds"] for p in primitives])})
    instances = []
    def visit(index, parent, lineage, active):
        if index in active:
            raise ValueError("Cycle in glTF scene graph")
        node = gltf["nodes"][index]
        local = local_matrix(node)
        world = matmul(parent, local)
        mesh_index = node.get("mesh")
        instances.append({"index": index, "path": lineage + [node.get("name", str(index))],
                          "name": node.get("name", ""), "mesh": mesh_index,
                          "translation": node.get("translation", [0, 0, 0]),
                          "rotation": node.get("rotation", [0, 0, 0, 1]),
                          "scale": node.get("scale", [1, 1, 1]),
                          "local_matrix_column_major": local, "world_matrix_column_major": world,
                          "world_bounds": transform_bounds(meshes[mesh_index]["bounds"], world)
                          if mesh_index is not None else None})
        for child in node.get("children", []):
            visit(child, world, lineage + [node.get("name", str(index))], active | {index})
    scenes = gltf.get("scenes", [])
    if not scenes:
        raise ValueError("No glTF scene supplied")
    scene_index = gltf.get("scene", 0)
    for index in scenes[scene_index].get("nodes", []):
        visit(index, IDENTITY, [], set())
    images = []
    for entry in gltf.get("images", []):
        images.append({**entry, **image_info(image_source(gltf, buffers, entry, path.parent))})
    return {"coordinate_system": "Original glTF, metres as declared by glTF; no Unity X mirror applied",
            "source_file": path.name, "asset": gltf.get("asset"), "scene_index": scene_index,
            "extensions_used": gltf.get("extensionsUsed", []),
            "extensions_required": gltf.get("extensionsRequired", []),
            "mesh_count": len(meshes), "node_count": len(gltf.get("nodes", [])),
            "unique_mesh_triangles": sum(p["triangles"] for mesh in meshes for p in mesh["primitives"]),
            "instanced_scene_triangles": sum(sum(p["triangles"] for p in meshes[n["mesh"]]["primitives"])
                                             for n in instances if n["mesh"] is not None),
            "scene_bounds": combine_bounds([n["world_bounds"] for n in instances]),
            "meshes": meshes, "nodes": instances, "materials": gltf.get("materials", []),
            "textures": gltf.get("textures", []), "images": images}


def download_model(downloader, asset_id, root, resolutions):
    base = root / "models" / asset_id
    manifest = downloader.api(f"files/{asset_id}", base / "api-files.json")
    downloader.api(f"info/{asset_id}", base / "api-info.json")
    result = {"id": asset_id, "kind": "model", "variants": []}
    for resolution in resolutions:
        api_path, selected = select_model(manifest, resolution)
        directory = base / resolution
        main = directory / safe_relative(Path(urlparse(selected["url"]).path).name)
        downloader.fetch(selected["url"], main, selected)
        gltf = json.loads(main.read_text())
        dependencies = {entry["uri"] for collection in ("buffers", "images")
                        for entry in gltf.get(collection, []) if entry.get("uri")
                        and not entry["uri"].startswith("data:")}
        for uri in sorted(dependencies):
            entry = dependency_entry(uri, selected, manifest, resolution)
            downloader.fetch(entry["url"], directory / safe_relative(uri), entry)
        inspection = inspect_gltf(main)
        write_json(directory / "inventory.json", inspection)
        variant = {"resolution": resolution, "api_path": list(api_path),
                   "gltf": str(main.relative_to(root)), "dependencies": sorted(dependencies),
                   "inventory": str((directory / "inventory.json").relative_to(root)),
                   "mesh_count": inspection["mesh_count"], "node_count": inspection["node_count"],
                   "triangles": inspection["instanced_scene_triangles"],
                   "scene_bounds": inspection["scene_bounds"]}
        result["variants"].append(variant)
        print(json.dumps({"downloaded_model": asset_id, **variant}), flush=True)
    return result


def normalized(value):
    return "".join(char.lower() for char in value if char.isalnum())


def download_texture(downloader, asset_id, root):
    base = root / "textures" / asset_id
    manifest = downloader.api(f"files/{asset_id}", base / "api-files.json")
    downloader.api(f"info/{asset_id}", base / "api-info.json")
    roles = {"diffuse": ("diffuse", "diff", "albedo", "color"),
             "normal_gl": ("norgl", "normalgl"),
             "roughness": ("rough", "roughness"), "ao": ("ao",), "arm": ("arm",)}
    selected = {}
    for role, names in roles.items():
        entries = [(path, value) for path, value in file_entries(manifest)
                   if path and normalized(path[0]) in names and "2k" in path]
        preferences = ("png", "jpg", "exr") if role == "normal_gl" else ("jpg", "png", "exr")
        candidates = [(preferences.index(path[-1]), path, value) for path, value in entries
                      if path[-1] in preferences]
        if not candidates:
            continue
        _, api_path, entry = min(candidates, key=lambda item: item[0])
        target = base / "2k" / safe_relative(Path(urlparse(entry["url"]).path).name)
        downloader.fetch(entry["url"], target, entry)
        selected[role] = {"path": str(target.relative_to(root)), "api_path": list(api_path),
                          **image_info(target)}
    missing = [role for role in ("diffuse", "normal_gl") if role not in selected]
    if "roughness" not in selected and "arm" not in selected:
        missing.append("roughness or arm")
    if missing:
        raise ValueError(f"{asset_id} lacks required 2k texture roles: {missing}; see API manifest")
    result = {"id": asset_id, "kind": "texture", "resolution": "2k", "maps": selected}
    write_json(base / "inventory.json", result)
    print(json.dumps({"downloaded_texture": asset_id, "maps": selected}), flush=True)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, default=Path("curated/Source/CityRebuild"))
    parser.add_argument("--model-resolutions", nargs="+", choices=("1k", "2k"), default=["1k", "2k"])
    args = parser.parse_args()
    root = args.output.resolve()
    root.mkdir(parents=True, exist_ok=True)
    downloader = Downloader(root)
    report = {"schema": 1, "status": "running", "commit": os.getenv("GITHUB_SHA", "local"),
              "generated_utc": dt.datetime.now(dt.timezone.utc).isoformat(),
              "source": "https://polyhaven.com", "license_reference": "https://polyhaven.com/license",
              "purpose": "Source audit only; no visual acceptance, mobile performance pass or Unity compatibility claim",
              "original_gltf_preserved": True, "assets": [], "errors": []}
    try:
        for asset in MODELS:
            try:
                report["assets"].append(download_model(downloader, asset, root, args.model_resolutions))
            except Exception as error:
                report["errors"].append({"asset": asset, "error": str(error)})
                print(f"ERROR {asset}: {error}", file=sys.stderr, flush=True)
        for asset in TEXTURES:
            try:
                report["assets"].append(download_texture(downloader, asset, root))
            except Exception as error:
                report["errors"].append({"asset": asset, "error": str(error)})
                print(f"ERROR {asset}: {error}", file=sys.stderr, flush=True)
    finally:
        report["status"] = "failed" if report["errors"] else "source_audit_complete_visual_review_pending"
        report["files"] = downloader.files
        report["download_bytes"] = downloader.total
        write_json(root / "source-audit.json", report)
        summary = "# City rebuild source asset audit\n\n" + report["status"] + "\n\n"
        summary += "Original glTF and image bytes are preserved. Inspect inventory.json before choosing modules.\n"
        summary += "No scene has been changed. Appearance, Unity material compatibility and physical-phone performance remain unverified.\n\n"
        for asset in report["assets"]:
            summary += f"- {asset['id']}: {asset['kind']}\n"
            for variant in asset.get("variants", []):
                summary += f"  - {variant['resolution']}: {variant['mesh_count']} meshes; {variant['node_count']} nodes; {variant['triangles']} scene triangles; bounds {variant['scene_bounds']}\n"
        for error in report["errors"]:
            summary += f"- ERROR {error['asset']}: {error['error']}\n"
        (root / "AUDIT.md").write_text(summary)
        if os.getenv("GITHUB_STEP_SUMMARY"):
            with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as output:
                output.write(summary)
    return 1 if report["errors"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
