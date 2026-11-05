"""Read-only pre-Unity gate for THREE owner's exact panorama JPEGs and GUIDs.

No dependencies, network, art conversion or Unity invocation. In particular,
the first interior-wall mural is retained; both side images are required.
"""
import hashlib,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/"unity-city/Assets/Source/Mistrzowie/ArenaMurals"
ORIGINALS={
 "BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg":(
  "94ed97faea3125ebde2e8cb9137465202aac2a03d0eb97b9abb213128c68156f",
  "964968cb8a1b51f98810b93ebf638e62"),
 "BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg":(
  "97f52b27d4811afe833f45a37a3705084c824204053a25b7f7a4695209c14c5e",
  "7fd43010b2d45f21abd1825996fd21e7"),
 "BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg":(
  "0e43f5e68d12e3705a8eed5ceb81da1c489647ff0742dd7edc4389588a2c64a2",
  "899ea5f5ffe25ecaaac8d271b59aa8d0")
}
FOLDER_GUID="4a9279b478bb5b949e2ac8306523f245"

def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda:f.read(1024*1024),b""):h.update(block)
    return h.hexdigest()

def validate_guid(path,required):
    if not path.is_file():return "MISSING_META"
    match=re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$",path.read_text(),re.M)
    if not match:return "INVALID_META"
    return "PASS" if match.group(1).lower()==required else "WRONG_GUID"

def audit(root=ASSETS):
    root=Path(root)
    entries=[]
    for name,(expected,guid) in ORIGINALS.items():
        path=root/name
        state="MISSING" if not path.is_file() else "PASS" if sha(path)==expected else "WRONG_SOURCE"
        entries.append({"name":name,"status":state,"type":"originalJPEG"})
        entries.append({"name":name+".meta","status":validate_guid(Path(str(path)+".meta"),guid),"type":"UnityGUID"})
    entries.append({"name":"ArenaMurals.meta",
                    "status":validate_guid(Path(str(root)+".meta"),FOLDER_GUID),
                    "type":"UnityFolderGUID"})
    passed=sum(entry["status"]=="PASS" for entry in entries)
    return {"status":"PASS" if passed==len(entries) else "BLOCKED",
            "total":len(entries),"passed":passed,
            "originals":3,"wallPositions":["centre","left fighters","right belt"],
            "unityCompiled":False,"androidTested":False,"entries":entries}

if __name__=="__main__":
    result=audit()
    report=ROOT/"mistrzowie-evidence/arena-three-murals.json"
    report.parent.mkdir(parents=True,exist_ok=True)
    report.write_text(json.dumps(result,indent=2),encoding="utf-8")
    for entry in result["entries"]:
        if entry["status"]!="PASS":print("::error::"+entry["status"]+": "+entry["name"])
    print("CHAMPION_A1_THREE_MURALS",result["status"],result["passed"],"/",result["total"])
    if result["status"]!="PASS":raise SystemExit(1)
