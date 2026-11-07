#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

SCHEMA_VERSION = 1
HEAVY_RESOURCE_BYTES = 5 * 1024 * 1024

IGNORED_RESOURCE_SUFFIXES = {
    ".meta", ".cs", ".asmdef", ".asmref", ".json", ".txt", ".xml", ".md"
}


def read_text(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except Exception:
        return ""


def is_under_resources(path: Path) -> bool:
    return any(part == "Resources" for part in path.parts)


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Report-only audit of BRAXHORN content-delivery architecture."
    )
    parser.add_argument("--project", default="unity-city")
    parser.add_argument("--zone", required=True)
    parser.add_argument("--output", default="")
    args = parser.parse_args()

    project = Path(args.project)
    assets = project / "Assets"
    packages = project / "Packages" / "manifest.json"
    output = Path(args.output) if args.output else Path("audit") / f"content-architecture-{args.zone}.json"
    output.parent.mkdir(parents=True, exist_ok=True)

    checks: list[dict[str, Any]] = []

    def check(name: str, ok: bool, detail: str, severity: str = "warning") -> None:
        checks.append({
            "name": name,
            "ok": bool(ok),
            "severity": "info" if ok else severity,
            "detail": detail,
        })

    deps: dict[str, str] = {}
    if packages.is_file():
        try:
            deps = json.loads(read_text(packages)).get("dependencies", {})
        except Exception as exc:
            check("packages-manifest-valid", False, f"Invalid JSON: {exc}", "error")
    else:
        check("packages-manifest-present", False, str(packages), "error")

    addressables_version = deps.get("com.unity.addressables", "")
    check(
        "addressables-installed",
        bool(addressables_version),
        f"com.unity.addressables={addressables_version or 'MISSING'}",
        "error",
    )

    expected_runtime = [
        assets / "Updates" / "BraxContent.cs",
        assets / "Updates" / "BraxContentManifest.cs",
    ]
    expected_editor = [
        assets / "Editor" / "Updates" / "BraxContentBuild.cs",
        assets / "Editor" / "Updates" / "BraxContentEdits.cs",
    ]
    runtime_present = all(p.is_file() for p in expected_runtime)
    editor_present = all(p.is_file() for p in expected_editor)
    check(
        "content-runtime-layer",
        runtime_present,
        ", ".join(f"{p.relative_to(project)}={'yes' if p.is_file() else 'no'}" for p in expected_runtime),
        "error",
    )
    check(
        "content-build-layer",
        editor_present,
        ", ".join(f"{p.relative_to(project)}={'yes' if p.is_file() else 'no'}" for p in expected_editor),
        "error",
    )

    content_runtime = read_text(assets / "Updates" / "BraxContent.cs")
    manual_picker = "BraxContentPicker" in content_runtime or "ChooseUpdate()" in content_runtime
    automatic_remote = (
        "UnityWebRequest" in content_runtime
        and ("Remote" in content_runtime or "remote" in content_runtime)
        and ("manifest" in content_runtime.lower())
    )
    check(
        "automatic-download-manager",
        automatic_remote,
        "Automatic remote manifest/download flow detected" if automatic_remote
        else "No automatic remote manifest/download flow detected in BraxContent.cs",
        "error",
    )
    check(
        "manual-picker-not-production-path",
        not manual_picker or automatic_remote,
        "Legacy picker present but automatic flow also exists" if manual_picker and automatic_remote
        else ("No legacy picker detected" if not manual_picker else "Legacy/manual picker appears to be the only update UX"),
        "error",
    )

    build_layer = read_text(assets / "Editor" / "Updates" / "BraxContentBuild.cs")
    baseline_state = (
        "addressables_content_state.bin" in build_layer
        and "BuildPlayerContent" in build_layer
        and "BuildContentUpdate" in build_layer
    )
    check(
        "addressables-content-state-workflow",
        baseline_state,
        "baseline + content state + content update symbols present" if baseline_state
        else "Missing baseline/content-state/content-update implementation",
        "error",
    )

    resources_files: list[dict[str, Any]] = []
    heavy_resources: list[dict[str, Any]] = []
    if assets.is_dir():
        for path in assets.rglob("*"):
            if not path.is_file() or not is_under_resources(path):
                continue
            rel = path.relative_to(project).as_posix()
            size = path.stat().st_size
            rec = {"path": rel, "bytes": size}
            resources_files.append(rec)
            if path.suffix.lower() not in IGNORED_RESOURCE_SUFFIXES and size >= HEAVY_RESOURCE_BYTES:
                heavy_resources.append(rec)

    check(
        "no-heavy-region-assets-in-resources",
        not heavy_resources,
        f"heavy Resources files >=5MiB: {len(heavy_resources)}",
        "warning",
    )

    buildplayer_hits: list[dict[str, Any]] = []
    if (assets / "Editor").is_dir():
        for path in (assets / "Editor").rglob("*.cs"):
            text = read_text(path)
            count = text.count("BuildPipeline.BuildPlayer")
            if count:
                buildplayer_hits.append({
                    "path": path.relative_to(project).as_posix(),
                    "count": count,
                })

    addressable_data_dir = assets / "AddressableAssetsData"
    check(
        "addressables-settings-materialized",
        addressable_data_dir.exists(),
        str(addressable_data_dir.relative_to(project)) if addressable_data_dir.exists()
        else "Assets/AddressableAssetsData not committed/generated in this checkout",
        "warning",
    )

    all_error_checks_ok = all(c["ok"] for c in checks if c["severity"] == "error")
    production_ready = bool(
        all_error_checks_ok
        and addressables_version
        and runtime_present
        and editor_present
        and automatic_remote
        and baseline_state
    )

    report = {
        "schemaVersion": SCHEMA_VERSION,
        "zone": args.zone,
        "project": project.as_posix(),
        "productionReady": production_ready,
        "addressablesVersion": addressables_version or None,
        "checks": checks,
        "resources": {
            "fileCount": len(resources_files),
            "bytes": sum(x["bytes"] for x in resources_files),
            "heavyFiles": sorted(heavy_resources, key=lambda x: x["bytes"], reverse=True)[:100],
        },
        "buildPlayerCallSites": buildplayer_hits,
        "notes": [
            "Report-only: this tool never edits Unity assets and never blocks a build.",
            "productionReady=false is expected until automatic remote delivery, staging/SHA/rollback and Android proof exist.",
            "BuildPipeline.BuildPlayer call sites are inventory, not automatically defects.",
        ],
    }

    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    md = output.with_suffix(".md")
    lines = [
        f"# BRAXHORN content architecture audit — {args.zone}",
        "",
        f"- productionReady: **{str(production_ready).lower()}**",
        f"- Addressables: `{addressables_version or 'MISSING'}`",
        f"- Resources files: {len(resources_files)} ({sum(x['bytes'] for x in resources_files)} bytes)",
        f"- Heavy Resources >=5 MiB: {len(heavy_resources)}",
        f"- BuildPlayer call sites: {len(buildplayer_hits)}",
        "",
        "## Checks",
    ]
    for item in checks:
        icon = "PASS" if item["ok"] else "GAP"
        lines.append(f"- {icon} `{item['name']}` — {item['detail']}")
    md.write_text("\n".join(lines) + "\n", encoding="utf-8")

    print(f"BRAX_CONTENT_ARCH_AUDIT zone={args.zone} productionReady={str(production_ready).lower()} output={output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
