#!/usr/bin/env python3
"""Fetch only two main Poly Haven blend files and inspect their authored LODs."""
import json
from pathlib import Path
import subprocess

from audit_assets import Downloader, write_json


ROOT = Path(__file__).resolve().parents[2]
SLUGS = ("tree_small_02", "jacaranda_tree")
SOURCE = ROOT / "city-tree-sources"
REPORT = ROOT / "city-tree-report"
INSPECTOR = ROOT / "unity-city/Assets/Editor/CityRebuild/Materials/InspectPolyHavenFoliage.py"
MAX_MAIN_BLEND_TOTAL = 512 * 1024 * 1024


def main():
    SOURCE.mkdir(exist_ok=True)
    REPORT.mkdir(exist_ok=True)
    downloader = Downloader(SOURCE)
    summary = {"scope": "Authored foliage LOD metadata only; no scene, foliage replacement or visual acceptance",
               "separate_textures_downloaded": False, "geometry_nodes_evaluated": False,
               "planned": [], "errors": []}
    try:
        for slug in SLUGS:
            base = SOURCE / slug
            manifest = downloader.api(f"files/{slug}", base / "api-files.json")
            metadata = downloader.api(f"info/{slug}", base / "api-info.json")
            entry = manifest["blend"]["1k"]["blend"]
            if not isinstance(entry.get("size"), int) or entry["size"] <= 0:
                raise ValueError("Official API lacks a valid main blend byte count for " + slug)
            from urllib.parse import urlparse
            target = base / Path(urlparse(entry["url"]).path).name
            summary["planned"].append({"slug": slug, "declared_bytes": entry["size"],
                                       "url": entry["url"], "path": str(target.relative_to(SOURCE)),
                                       "api_md5": entry.get("md5"), "metadata": metadata})
        total = sum(item["declared_bytes"] for item in summary["planned"])
        summary["declared_main_blend_total_bytes"] = total
        write_json(REPORT / "download-plan.json", summary)
        print("DECLARED_MAIN_BLEND_BYTES_BEFORE_DOWNLOAD", total, json.dumps([{k:v for k,v in p.items() if k!='metadata'} for p in summary["planned"]]), flush=True)
        if total > MAX_MAIN_BLEND_TOTAL:
            raise ValueError(f"Main blend sources require {total} bytes, above explicit 512 MiB metadata-inspection budget; review plan before downloading")
        if not INSPECTOR.is_file():
            raise ValueError("Foliage LOD inspection source has not been integrated")
        for plan in summary["planned"]:
            slug = plan["slug"]
            target = SOURCE / plan["path"]
            downloader.fetch(plan["url"], target, {"size":plan["declared_bytes"], "md5":plan["api_md5"]})
            with (REPORT / (slug + "-blender.log")).open("w") as log:
                result = subprocess.run(["blender", "--background", "--disable-autoexec", str(target),
                                         "--python", str(INSPECTOR), "--", "--slug", slug,
                                         "--output", str(REPORT / (slug + "-lods.json"))],
                                        stdout=log, stderr=subprocess.STDOUT, timeout=480)
            if result.returncode or not (REPORT / (slug + "-lods.json")).is_file():
                raise RuntimeError(f"Blender could not inspect {slug}; inspect preserved log")
            print("AUTHORED_FOLIAGE_LOD_INVENTORY_READY", slug, flush=True)
    except Exception as error:
        summary["errors"].append(str(error))
        raise
    finally:
        summary["files"] = downloader.files
        summary["status"] = "failed" if summary["errors"] else "metadata_ready_visual_selection_pending"
        write_json(REPORT / "source-provenance.json", summary)


if __name__ == "__main__":
    main()
