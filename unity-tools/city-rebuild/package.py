#!/usr/bin/env python3
"""Package a new isolated Unity city build using the existing iOS streaming fix.

This operates on generated unity-preview output only. It does not call the old
release scripts, which are deliberately pinned to an unrelated earlier build.
Transport parts are a byte-preserving delivery mechanism, not district streaming.
"""
import hashlib
import json
import os
from pathlib import Path
import runpy
import shutil
import zlib


ROOT = Path(__file__).resolve().parents[2]

PUBLIC_PLACES = {
    "gym_start": "Siłownia", "gym_ring_center": "Ring", "street_gym": "Ulica przy siłowni",
    "shop_inside": "BRAXHORN NUTRITION", "quay_entry": "Nabrzeże", "pier_end": "Molo",
    "yard_brama_yard": "Podwórko", "fight1_center": "Boisko", "club_dance": "Klub NOVA",
    "cellar_entry": "Piwnica klubu NOVA", "closed_estate": "Przejście na osiedle",
    "closed_forest": "Droga do lasu", "estate_entry": "Powrót do miasta", "park": "Park",
    "court": "Boisko osiedlowe", "villa_front": "Willa", "villa_living": "Salon willi",
    "villa_terrace": "Taras willi", "garages": "Garaże", "casino": "Kasyno", "club": "Dyskoteka",
    "police": "Komisariat", "hospital": "Szpital", "boxing": "Sala bokserska", "arena": "Hala walk",
}


def filter_public_navigation(path):
    html = path.read_text()
    old = "for(const p of points){const button=document.createElement('button');button.textContent=p.label||p.id;"
    if html.count(old) != 1:
        raise ValueError("The existing map UI changed; review its point filtering before packaging")
    new = "const publicPlaces=" + json.dumps(PUBLIC_PLACES, ensure_ascii=False, separators=(",", ":")) + ";"
    new += "for(const p of points){if(!Object.hasOwn(publicPlaces,p.id))continue;const button=document.createElement('button');button.textContent=publicPlaces[p.id];"
    path.write_text(html.replace(old, new, 1))


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def package_transport(root, commit, loader_source):
    build = root / "Build"
    candidates = list(build.glob("*.data.unityweb"))
    if len(candidates) != 1:
        raise ValueError(f"Expected exactly one gzip Unity data archive, found {len(candidates)}")
    source = candidates[0]
    with source.open("rb") as stream:
        if stream.read(2) != b"\x1f\x8b":
            raise ValueError("Existing iOS transport requires gzip Unity data")
    compressed = hashlib.sha256()
    expanded = hashlib.sha256()
    inflater = zlib.decompressobj(31)
    parts = []
    compressed_count = expanded_count = 0
    with source.open("rb") as stream:
        while payload := stream.read(32 * 1024 * 1024):
            name = source.name + f".part{len(parts):03d}"
            destination = build / name
            destination.write_bytes(payload)
            compressed.update(payload)
            compressed_count += len(payload)
            parts.append({"path": "Build/" + name, "bytes": len(payload),
                          "sha256": hashlib.sha256(payload).hexdigest()})
            for offset in range(0, len(payload), 65536):
                data = inflater.decompress(payload[offset:offset+65536])
                expanded.update(data)
                expanded_count += len(data)
    tail = inflater.flush()
    expanded.update(tail)
    expanded_count += len(tail)
    if not inflater.eof or inflater.unused_data:
        raise ValueError("Unity data archive is incomplete or contains unexpected trailing gzip data")
    if compressed.hexdigest() != digest(source):
        raise ValueError("Transport split changed compiled Unity data")
    combined = hashlib.sha256()
    for part in parts:
        with (root / part["path"]).open("rb") as stream:
            while block := stream.read(65536):
                combined.update(block)
    if combined.hexdigest() != compressed.hexdigest():
        raise ValueError("Reassembled transport parts differ from the compiled archive")
    manifest = {"schema": 1, "dataPath": "Build/" + source.name,
                "bytes": compressed_count, "sha256": compressed.hexdigest(),
                "uncompressedBytes": expanded_count, "uncompressedSha256": expanded.hexdigest(),
                "parts": parts, "sourceCommit": commit,
                "compiledDataBytesUnchanged": True,
                "districtStreaming": False}
    code = loader_source.read_text()
    if code.count("__MANIFEST__") != 1 or "new DecompressionStream('gzip')" not in code:
        raise ValueError("Unexpected source version of the existing iOS streaming fix")
    (root / "city-transport.js").write_text(code.replace("__MANIFEST__", json.dumps(manifest, separators=(",", ":"))))
    (root / "transport.json").write_text(json.dumps(manifest, indent=2) + "\n")
    loaders = list(build.glob("*.loader.js"))
    if len(loaders) != 1:
        raise ValueError("Expected one generated Unity loader")
    loader = loaders[0]
    text = loader.read_text()
    replacements = {
        "fetch(e,t).then(function(e){return b.readBodyWithProgress":
            "(window.BraxCityFetch||fetch)(e,t).then(function(e){return b.readBodyWithProgress",
        "n=b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress":
            "n=window.BraxCityFetch?b.fetchWithProgress:(b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress)",
    }
    for old, new in replacements.items():
        if text.count(old) != 1:
            raise ValueError("Generated loader differs from the verified Unity 6000.0.58f2 transport contract")
        text = text.replace(old, new, 1)
    loader.write_text(text)
    index = root / "index.html"
    html = index.read_text()
    if "<base " in html or "city-transport.js" in html:
        raise ValueError("Expected a fresh isolated preview entry, without an older deployment base")
    if html.count("function fail(e){") != 1 or "<script>" not in html:
        raise ValueError("Preview error handling or script entry differs from the existing player")
    html = html.replace("<script>", f'<script src="city-transport.js?v={commit[:12]}"></script><script>', 1)
    html = html.replace("function fail(e){", "window.addEventListener('brax-city-load-error',e=>fail(e.detail));\nfunction fail(e){", 1)
    index.write_text(html)
    source.unlink()  # Generated delivery copy only; original build/WebGL remains.
    return manifest


def main():
    commit = os.environ.get("GITHUB_SHA")
    if not commit or len(commit) != 40:
        raise ValueError("An exact source commit is required for a reviewable city build")
    runpy.run_path(str(ROOT / "unity-tools/city/package.py"), run_name="__main__")
    out = ROOT / "unity-preview"
    filter_public_navigation(out / "index.html")
    city_manifest = json.loads((out / "city-migration.json").read_text())
    if city_manifest.get("commit") != commit:
        raise ValueError("Compiled city provenance does not match the build source commit")
    source_loader = ROOT / "unity-release/ios-stream-loader.js"
    manifest = package_transport(out, commit, source_loader)
    evidence = ROOT / "city-rebuild-review"
    evidence.mkdir(exist_ok=True)
    curated = ROOT / "unity-city/Assets/Source/CityRebuild/source-audit.json"
    if curated.is_file():
        shutil.copyfile(curated, out / "curated-source-audit.json")
    gym_commit = os.environ.get("BRAX_GYM_INTEGRATION_COMMIT") or None
    if gym_commit and (len(gym_commit) != 40 or any(c not in "0123456789abcdef" for c in gym_commit)):
        raise ValueError("Gym integration commit must be an exact SHA or remain explicitly pending")
    release = {"schema": 1, "scope": "isolated-city-rebuild-preview",
               "buildId": f"city-{commit[:12]}-{os.environ.get('GITHUB_RUN_ID', 'local')}",
               "sourceCommit": commit, "cityCommit": commit, "gymIntegrationCommit": gym_commit,
               "gymIntegrationStatus": "integrated_commit_requires_runtime_review" if gym_commit else "new_gym_integration_pending",
               "sourceRun": os.environ.get("GITHUB_RUN_ID"),
               "unityVersion": "6000.0.58f2", "target": "WebGL",
               "originalCityExportRun": 40840263799, "originalGymExportRun": 40787817505,
               "curatedBaseAuditRun": 40866427252, "curatedSourceAuditRun": 40869502253,
               "transport": manifest,
               "loadingFix": {"revision": "ios-stream-01", "sourceSha256": digest(source_loader),
                              "nativeStreamingGzip": True, "maxInputChunkBytes": 65536,
                              "compiledDataBytesUnchanged": True, "physicalIPhoneValidated": False},
               "visualAcceptance": "pending", "physicalPhoneAcceptance": "pending",
               "productionPublication": False}
    release["files"] = {path.relative_to(out).as_posix(): digest(path)
                        for path in out.rglob("*") if path.is_file() and path.name != "release.json"}
    (out / "release.json").write_text(json.dumps(release, indent=2) + "\n")
    shutil.copyfile(out / "release.json", evidence / "release.json")
    shutil.copyfile(out / "transport.json", evidence / "transport.json")
    print("CITY_REBUILD_ISOLATED_PACKAGE", release["buildId"], manifest["bytes"], manifest["uncompressedBytes"], len(manifest["parts"]), flush=True)


if __name__ == "__main__":
    main()
