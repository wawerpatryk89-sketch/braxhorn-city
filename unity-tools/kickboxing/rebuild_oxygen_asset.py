#!/usr/bin/env python3
"""Rebuild only owner-selected Oxygen3D CC-BY gym meshes for Unity 6.

The tracked source is an immutable compressed, millimetre-quantised
representation of the selected triangles from the owner-provided
`gym_environment.zip`, not any generated replacement shapes.
No boxing ring or unrelated geometry is imported.
"""
from __future__ import annotations

import base64
import gzip
import hashlib
import json
import pathlib
import struct

BASE = pathlib.Path(__file__).resolve().parent
PACK = BASE / "oxygen_exact_selected.b64.txt"
DEST = BASE.parent.parent / "unity-city/Assets/Source/Oxygen/Oxygen3D_Selected.glb"
RECEIPT = BASE / "oxygen_selected_receipt.json"
SOURCE = "https://sketchfab.com/3d-models/game-ready-gym-environment-asset-pack-8aef4a478bbe49d483280968aff59064"
PACKED_SHA256 = "28fbefef481c2d525e60525e11116709b66d062c714fe7b42d53a4e5eefff588"
REQUIRED = {
    "pear_one_fixed", "pear_one_head", "pear_two_fixed", "pear_two_head",
    "bench_press", "treadmill", "exercise_bike", "ladder", "dumbbell_rack",
}
GROUPS = {
    "pear_one": "Oxygen3D_GROUP_pear_one",
    "pear_two": "Oxygen3D_GROUP_pear_two",
    "bench_press": "Oxygen3D_GROUP_bench_press",
    "treadmill": "Oxygen3D_GROUP_treadmill",
    "exercise_bike": "Oxygen3D_GROUP_exercise_bike",
    "ladder": "Oxygen3D_GROUP_ladder",
    "dumbbell_rack": "Oxygen3D_GROUP_dumbbell_rack",
}


def align4(data: bytearray):
    data.extend(b"\0" * (-len(data) % 4))


def main():
    raw = gzip.decompress(base64.b64decode(PACK.read_text(encoding="ascii")))
    assert hashlib.sha256(raw).hexdigest() == PACKED_SHA256, "Oxygen3D source changed"
    assert raw.startswith(b"OXPK1")
    offset = 5
    tex_bytes = struct.unpack_from("<I", raw, offset)[0]
    offset += 4
    tex = raw[offset:offset + tex_bytes]
    offset += tex_bytes
    assert tex[:3] == b"\xff\xd8\xff", "Expected original-atlas JPEG"

    meshes, bin_data, buffer_views, accessors, nodes, groups, data_records = [], bytearray(), [], [], [], {}, []

    def buffer_view(payload: bytes, target=None):
        align4(bin_data)
        at = len(bin_data)
        bin_data.extend(payload)
        out = {"buffer": 0, "byteOffset": at, "byteLength": len(payload)}
        if target is not None:
            out["target"] = target
        buffer_views.append(out)
        return len(buffer_views) - 1

    def accessor(buf_view, component_type, count, shape, mins=None, maxs=None):
        a = {"bufferView": buf_view, "byteOffset": 0,
             "componentType": component_type, "count": count, "type": shape}
        if mins is not None:
            a["min"] = mins
            a["max"] = maxs
        accessors.append(a)
        return len(accessors) - 1

    while offset < len(raw):
        name_size = raw[offset]
        offset += 1
        if name_size == 0:
            break
        name = raw[offset:offset + name_size].decode("ascii")
        offset += name_size
        nv, nt = struct.unpack_from("<HH", raw, offset)
        offset += 4
        assert name and nv and nt and nv < 65535 and nt < 65535
        assert any(name == req or name.startswith(req) for req in REQUIRED), f"Unexpected model: {name}"
        positions = bytearray()
        normals = bytearray()
        uv = bytearray()
        bounds_min = [float("inf")] * 3
        bounds_max = [float("-inf")] * 3
        for _ in range(nv):
            x, y, z, nx, ny, nz, u, v = struct.unpack_from("<hhhbbbHH", raw, offset)
            offset += 13
            coords = [x * .001, y * .001, z * .001]
            for axis, c in enumerate(coords):
                bounds_min[axis] = min(bounds_min[axis], c)
                bounds_max[axis] = max(bounds_max[axis], c)
            positions.extend(struct.pack("<fff", *coords))
            normals.extend(struct.pack("<fff", nx / 127., ny / 127., nz / 127.))
            uv.extend(struct.pack("<ff", u / 65535., v / 65535.))
        faces = raw[offset:offset + nt * 6]
        assert len(faces) == nt * 6
        assert max(struct.unpack("<" + "H" * nt * 3, faces)) < nv
        offset += len(faces)
        pview = buffer_view(positions, 34962)
        nview = buffer_view(normals, 34962)
        uview = buffer_view(uv, 34962)
        fview = buffer_view(faces, 34963)
        attrib = {
            "POSITION": accessor(pview, 5126, nv, "VEC3", bounds_min, bounds_max),
            "NORMAL": accessor(nview, 5126, nv, "VEC3"),
            "TEXCOORD_0": accessor(uview, 5126, nv, "VEC2"),
        }
        mesh_id = len(meshes)
        meshes.append({"name": "Oxygen3D_mesh_" + name, "primitives": [
            {"attributes": attrib, "indices": accessor(fview, 5123, nt * 3, "SCALAR"), "material": 0, "mode": 4}
        ]})
        node_id = len(nodes)
        nodes.append({"name": "Oxygen3D_PART_" + name, "mesh": mesh_id})
        group = name.split("_")[0] if name.startswith("pear_") else None
        if name.startswith("pear_one"):
            group = "pear_one"
        elif name.startswith("pear_two"):
            group = "pear_two"
        elif name.startswith("bench_press"):
            group = "bench_press"
        elif name.startswith("exercise_bike"):
            group = "exercise_bike"
        elif name.startswith("dumbbell_rack"):
            group = "dumbbell_rack"
        elif name.startswith("treadmill"):
            group = "treadmill"
        elif name.startswith("ladder"):
            group = "ladder"
        assert group in GROUPS, name
        groups.setdefault(group, []).append(node_id)
        data_records.append({"name": name, "vertices": nv, "triangles": nt,
                             "boundsMin": bounds_min, "boundsMax": bounds_max})

    assert offset == len(raw), "Extra data in original mesh pack"
    assert len(data_records) == 22, f"Expected 22 source geometry parts, got {len(data_records)}"
    assert REQUIRED <= {x["name"] for x in data_records}, "Essential models missing"
    assert set(groups) == set(GROUPS), "Wrong assemblies"
    assert sum(x["triangles"] for x in data_records) > 7500

    scene_roots = []
    for key, group_name in GROUPS.items():
        scene_roots.append(len(nodes))
        nodes.append({"name": group_name, "children": groups[key]})
    image_view = buffer_view(tex)
    gltf = {
        "asset": {"version": "2.0", "generator": "BRAXHORN Oxygen3D exact selected mesh importer",
                  "copyright": "Oxygen3D — CC BY Attribution; source: " + SOURCE},
        "scene": 0,
        "scenes": [{"nodes": scene_roots, "name": "Oxygen3D selected gym equipment only — no original rings"}],
        "nodes": nodes, "meshes": meshes, "accessors": accessors,
        "bufferViews": buffer_views,
        "buffers": [{"byteLength": len(bin_data)}],
        "materials": [{"name": "Original Oxygen3D gym atlas", "doubleSided": False,
                       "pbrMetallicRoughness": {"baseColorTexture": {"index": 0},
                                                "roughnessFactor": .88, "metallicFactor": 0.0}}],
        "textures": [{"source": 0, "sampler": 0}],
        "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 10497, "wrapT": 10497}],
        "images": [{"name": "Original Oxygen3D atlas resampled to 512px",
                    "mimeType": "image/jpeg", "bufferView": image_view}],
        "extras": {"originalArchiveSHA256": "c847f82ec2c0f07a7466812c303414135576a50f5432708518518fa6a6e302ad",
                   "source": SOURCE, "license": "CC BY Attribution",
                   "modifications": "Selected equipment only; texture atlas resized; millimetre vertex quantisation"}
    }
    gltf["buffers"][0]["byteLength"] = len(bin_data)
    align4(bin_data)
    json_chunk = json.dumps(gltf, separators=(",", ":"), ensure_ascii=True).encode("utf-8")
    json_chunk += b" " * (-len(json_chunk) % 4)
    total = 12 + 8 + len(json_chunk) + 8 + len(bin_data)
    glb = (struct.pack("<III", 0x46546C67, 2, total) +
           struct.pack("<II", len(json_chunk), 0x4E4F534A) + json_chunk +
           struct.pack("<II", len(bin_data), 0x004E4942) + bin_data)
    DEST.parent.mkdir(parents=True, exist_ok=True)
    DEST.write_bytes(glb)
    receipt = {"source": SOURCE, "author": "Oxygen3D", "license": "CC BY Attribution",
               "destination": str(DEST), "outputSha256": hashlib.sha256(glb).hexdigest(),
               "models": list(GROUPS), "parts": data_records, "ringModelsImported": 0,
               "packedSha256": PACKED_SHA256, "bytes": len(glb)}
    RECEIPT.write_text(json.dumps(receipt, indent=2))
    print("OXYGEN3D_ORIGINAL_ASSET_IMPORT_OK", json.dumps({"models": list(GROUPS),
          "parts": len(data_records), "rings": 0, "bytes": len(glb),
          "sha256": receipt["outputSha256"]}))


if __name__ == "__main__":
    main()