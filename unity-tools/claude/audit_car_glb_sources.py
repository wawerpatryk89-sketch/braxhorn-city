#!/usr/bin/env python3
"""Read-only audit of original car GLB objects used by the estate generator.

No mesh/texture changes and no Blender or Unity required.
Only asset-name/GLB-metadata is printed; this is NOT a visual or physics test.
"""
from __future__ import annotations

import json
from pathlib import Path
import re
import struct
import sys

ROOT = Path(__file__).resolve().parents[2]
CAR_DIR = ROOT / "public/assets/cars-src"
PATTERN = re.compile(r"wheel|tire|tyre|rim|felg|kolo|koło|front.?door|rear.?door|door|drzwi|steer|kierown", re.I)


def inspect(path: Path) -> dict:
    with path.open("rb") as stream:
        header = stream.read(20)
        if len(header) != 20:
            raise ValueError("Truncated GLB header")
        magic, version, total, json_size, chunk_type = struct.unpack("<4s4I", header)
        if magic != b"glTF" or version != 2 or chunk_type != 0x4e4f534a:
            raise ValueError("Expected glTF v2 GLB JSON first chunk")
        if total != path.stat().st_size or json_size > total - 20:
            raise ValueError("Invalid chunk size")
        data = json.loads(stream.read(json_size))
    nodes = data.get("nodes", [])
    meshes = data.get("meshes", [])
    materials = data.get("materials", [])
    named = [str(n.get("name", "")) for n in nodes]
    named_mesh = [str(m.get("name", "")) for m in meshes]
    all_named = list(dict.fromkeys(named + named_mesh))
    labelled = [n for n in all_named if PATTERN.search(n)]
    return {
        "asset": path.name,
        "bytes": total,
        "nodes": len(nodes),
        "meshes": len(meshes),
        "primitives": sum(len(m.get("primitives", [])) for m in meshes),
        "materials": len(materials),
        "named_door_nodes": [name for name in labelled if re.search(r"door|drzwi", name, re.I)][:12],
        "named_wheel_nodes": [name for name in labelled if re.search(r"wheel|tire|tyre|rim|felg|koło|kolo", name, re.I)][:12],
        "named_steering_nodes": [name for name in labelled if re.search(r"steer|kierown", name, re.I)][:8],
        "sample_node_names": [n for n in all_named if n][:20],
        "mesh_nodes": sum(1 for n in nodes if "mesh" in n),
        "animations": len(data.get("animations", [])),
        "extensions": data.get("extensionsUsed", []),
        "source_mesh_not_guaranteed_drivable": not any(re.search(r"door|drzwi", n, re.I) for n in labelled),
    }


def main() -> int:
    files = sorted(CAR_DIR.glob("car_*.glb"))
    if not files:
        raise FileNotFoundError(f"No original GLB vehicles under {CAR_DIR}")
    result = [inspect(file) for file in files]
    print("BRAXHORN_CAR_GLB_SOURCE_AUDIT " + json.dumps(result, ensure_ascii=False))
    print("SUMMARY vehicle_sources=" + str(len(result)) +
          " no_door_nodes=" + str(sum(x["source_mesh_not_guaranteed_drivable"] for x in result)) +
          " no_named_wheel_nodes=" + str(sum(not x["named_wheel_nodes"] for x in result)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
