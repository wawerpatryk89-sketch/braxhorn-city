#!/usr/bin/env python3
"""Restore original GLBs from verified Git transport parts; never use substitutes."""
import hashlib, json, shutil, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "dev/osiedle/assets"
UNITY = ROOT / "unity-city/Assets/StreamingAssets/Estate"
GENERATED = ROOT / "unity-city/Assets/EstateMigration/Generated"
NAMES = ("gameready_casino_scene", "policestation", "park_tree")

def digest(p):
    h = hashlib.sha256()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1024*1024), b""): h.update(chunk)
    return h.hexdigest()

def main():
    parts = json.loads((ROOT/"dev/deploy/asset-parts.json").read_text())["assets"]
    originals = {x["file"]:x for x in json.loads((SRC/"manifest.json").read_text())}
    recovered = {}
    for name in NAMES:
        filename = name + ".glb"
        source = originals[filename]
        rel = "dev/osiedle/assets/" + filename
        matches = [x for x in parts if x["path"] == rel]
        if len(matches) != 1: raise ValueError("Transport missing/ambiguous: " + rel)
        item = matches[0]
        if item["size"] != source["bytes"]: raise ValueError("Size mismatch: " + filename)
        dest = SRC / filename
        if not dest.exists() or digest(dest) != source["sha256"]:
            tmp = dest.with_suffix(".glb.recovering")
            try:
                offset = 0
                with tmp.open("wb") as out:
                    for part in sorted(item["parts"],key=lambda p:p["offset"]):
                        path = ROOT/part["path"]
                        if not path.is_file() or path.stat().st_size != part["size"]:
                            raise FileNotFoundError("Original part missing: " + str(path))
                        if offset != part["offset"]: raise ValueError("Noncontiguous parts")
                        with path.open("rb") as inp: shutil.copyfileobj(inp,out,1024*1024)
                        offset += part["size"]
                if offset != source["bytes"] or digest(tmp) != source["sha256"]:
                    raise ValueError("Original SHA256 mismatch: " + filename)
                tmp.replace(dest)
            finally: tmp.unlink(missing_ok=True)
        if dest.stat().st_size != source["bytes"] or digest(dest) != source["sha256"]:
            raise ValueError("Original not verified: " + filename)
        UNITY.mkdir(parents=True,exist_ok=True)
        target = UNITY/filename
        if not target.exists() or digest(target) != source["sha256"]: shutil.copyfile(dest,target)
        if digest(target) != source["sha256"]: raise ValueError("Unity asset changed: " + filename)
        recovered[name] = dict(name=name,source=rel,present=True,bytes=source["bytes"],sha256=source["sha256"])
        print("VERIFIED ORIGINAL",filename,source["bytes"],source["sha256"],flush=True)
    for path in (UNITY/"EstateSourceManifest.json",GENERATED/"EstateSourceManifest.json"):
        if not path.is_file(): raise FileNotFoundError("Source manifest missing: " + str(path))
        m = json.loads(path.read_text())
        if len(m.get("sourceModels",[])) != 21 or len(m.get("doors",[])) != 24 or len(m.get("collision",[])) != 1013 or len(m.get("vegetation",[])) != 30:
            raise ValueError("Source scene inventory drifted")
        for item in m["binaryAssets"]:
            if item["name"] in recovered: item.update(recovered[item["name"]])
        for item in m["sourceModels"]:
            if item["name"] in recovered: item["source"] = recovered[item["name"]]["source"]
        m["missingOriginalModels"] = [x["name"] for x in m["binaryAssets"] if not x["present"]]
        if m["missingOriginalModels"]: raise ValueError("Unresolved originals: "+str(m["missingOriginalModels"]))
        path.write_text(json.dumps(m,ensure_ascii=False,indent=2)+"\n")
    GENERATED.mkdir(parents=True,exist_ok=True)
    (GENERATED/"RecoveredOriginals.json").write_text(json.dumps(dict(originals_recovered=list(recovered),source_model_instances=21,doors=24,vegetation=30,collisions=1013,unity_compiled=False,visual_1to1_verified=False),indent=2)+"\n")
    print("ORIGINAL SOURCE RESTORED; Unity and visual tests still required",flush=True)

if __name__ == "__main__":
    try: main()
    except (OSError,ValueError,KeyError) as e:
        print("RECOVERY FAILED:",e,file=sys.stderr)
        raise SystemExit(1)
