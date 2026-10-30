#!/usr/bin/env python3
"""Inspect original exported mesh bounds and materials before choosing replacements."""
import argparse
import csv
import hashlib
import json
from pathlib import Path

from audit_assets import image_source, inspect_gltf, load_gltf, write_json


def unity_bounds(bounds):
    if not bounds:
        return None
    return {"min": [-bounds["max"][0], bounds["min"][1], bounds["min"][2]],
            "max": [-bounds["min"][0], bounds["max"][1], bounds["max"][2]],
            "size": bounds["size"]}


def contacts(source, output):
    from PIL import Image, ImageDraw
    gltf, buffers = load_gltf(source)
    materials = gltf.get("materials", [])
    sheets = []
    # Label every material, including shared images and untextured colours.
    for start in range(0, len(materials), 80):
        sheet = Image.new("RGB", (10 * 128, 8 * 150), (28, 28, 28))
        draw = ImageDraw.Draw(sheet)
        for slot, material in enumerate(materials[start:start+80]):
            x, y = slot % 10 * 128, slot // 10 * 150
            pbr = material.get("pbrMetallicRoughness", {})
            texture = pbr.get("baseColorTexture", {}).get("index")
            if texture is not None:
                image_index = gltf["textures"][texture].get("source")
                entry = gltf["images"][image_index]
                with Image.open(image_source(gltf, buffers, entry, source.parent)) as image:
                    preview = image.convert("RGB")
                    preview.thumbnail((124, 124))
                    sheet.paste(preview, (x + (128-preview.width)//2, y + 22))
            else:
                colour = pbr.get("baseColorFactor", [1, 1, 1, 1])
                rgb = tuple(max(0, min(255, round(channel * 255))) for channel in colour[:3])
                draw.rectangle((x+2, y+24, x+125, y+145), fill=rgb)
            draw.text((x+3, y+3), f"mat {start+slot}: {material.get('name', '')[:12]}", fill="white")
        dest = output / f"materials-{start//80+1:02d}.jpg"
        sheet.save(dest, quality=80)
        sheets.append(dest.name)
    return sheets


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--manifest", type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    report = inspect_gltf(args.source)
    report["source_sha256"] = hashlib.file_digest(args.source.open("rb"), "sha256").hexdigest()
    report["source_bytes"] = args.source.stat().st_size
    report["scope"] = "Read-only source inventory, not visual or collision acceptance"
    for node in report["nodes"]:
        node["unity_world_bounds"] = unity_bounds(node["world_bounds"])
    manifest = json.loads(args.manifest.read_text()) if args.manifest else {}
    original_materials = manifest.get("materials", [])
    by_name = {item.get("name"): item for item in original_materials}
    for material in report["materials"]:
        material["source_metadata"] = by_name.get(material.get("name"))
    report["source_points"] = manifest.get("points", [])
    report["source_zones"] = manifest.get("zones", [])
    report["material_contact_sheets"] = contacts(args.source, args.output)
    write_json(args.output / "original-city-inventory.json", report)
    with (args.output / "mesh-bounds.csv").open("w", newline="") as output:
        writer = csv.writer(output)
        writer.writerow(["node", "mesh", "name", "minX_Unity", "minY", "minZ", "maxX_Unity", "maxY", "maxZ", "triangles", "materials"])
        for node in report["nodes"]:
            if node["mesh"] is None:
                continue
            mesh = report["meshes"][node["mesh"]]
            bounds = node["unity_world_bounds"]
            writer.writerow([node["index"], node["mesh"], node["name"], *bounds["min"], *bounds["max"],
                             sum(p["triangles"] for p in mesh["primitives"]),
                             ",".join(str(p["material"]) for p in mesh["primitives"])])
    summary = {key: report[key] for key in ("source_sha256", "source_bytes", "mesh_count", "node_count", "unique_mesh_triangles", "instanced_scene_triangles", "scene_bounds")}
    summary["materials"] = len(report["materials"])
    summary["source_modified"] = False
    write_json(args.output / "summary.json", summary)
    print(json.dumps(summary), flush=True)


if __name__ == "__main__":
    main()
