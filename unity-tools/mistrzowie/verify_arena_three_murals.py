"""Hard pre-Unity gate for THREE exact owner HD PNG murals and deterministic Unity GUIDs.

The source release mapping is:
IMG_0264.png -> LEFT fighters
IMG_0265.png -> RIGHT belt
IMG_0266.png -> ENTRANCE

No fallback JPEG is accepted. No art conversion or network is performed here.
"""
import hashlib,json,re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/"unity-city/Assets/Source/Mistrzowie/ArenaMuralsHD"
ORIGINALS={
 "MURAL_LEWA_ZAWODNICY_6144x2048.png":(
  "0805d4170b696236dba7a37c89fc4415ee21a87b316c1d58734bac212cb11a2c",
  "687656e2c1dc56029ecaca5d00f1304e","IMG_0264.png"),
 "MURAL_PRAWA_PAS_6144x2048.png":(
  "b134858b5ba16dc85089642eb1f7432ef5c76e036a8d313805947923b7661304",
  "7ff3e828508b5fb2af24b914888caba1","IMG_0265.png"),
 "MURAL_WEJSCIE_CHAMPION_FIGHTER_6144x2048.png":(
  "5d30aa995bb0da043a34c282fb452743f1b04b9f4584abf2be925ea29a461a73",
  "38a2a5ce62325bb3add49f4ce726c76c","IMG_0266.png")
}
FOLDER_GUID="0797d54d6c4a51949ce34cdef81b0a8e"
PNG_SIGNATURE=b"\x89PNG\r\n\x1a\n"
SIZE=(6144,2048)

def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda:f.read(1024*1024),b""):h.update(block)
    return h.hexdigest()

def png_size(path):
    raw=path.read_bytes()[:24]
    if len(raw)<24 or raw[:8]!=PNG_SIGNATURE or raw[12:16]!=b"IHDR":
        return None
    return (int.from_bytes(raw[16:20],"big"),int.from_bytes(raw[20:24],"big"))

def validate_guid(path,required):
    if not path.is_file():return "MISSING_META"
    match=re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$",path.read_text(encoding="utf-8"),re.M)
    if not match:return "INVALID_META"
    return "PASS" if match.group(1).lower()==required else "WRONG_GUID"

def audit(root=ASSETS):
    root=Path(root)
    entries=[]
    for name,(expected,guid,release_name) in ORIGINALS.items():
        path=root/name
        if not path.is_file(): state="MISSING"
        elif sha(path)!=expected: state="WRONG_SOURCE"
        elif png_size(path)!=SIZE: state="WRONG_DIMENSIONS"
        else: state="PASS"
        entries.append({"name":name,"releaseName":release_name,"status":state,
                        "type":"ownerHDPNG","size":"6144x2048","sha256":expected})
        entries.append({"name":name+".meta","status":validate_guid(Path(str(path)+".meta"),guid),
                        "type":"UnityGUID","expectedGuid":guid})
    entries.append({"name":"ArenaMuralsHD.meta",
                    "status":validate_guid(Path(str(root)+".meta"),FOLDER_GUID),
                    "type":"UnityFolderGUID","expectedGuid":FOLDER_GUID})
    passed=sum(entry["status"]=="PASS" for entry in entries)
    return {"status":"PASS" if passed==len(entries) else "BLOCKED",
            "total":len(entries),"passed":passed,"ownerHD":3,
            "releaseFiles":["IMG_0264.png","IMG_0265.png","IMG_0266.png"],
            "wallPositions":["left fighters","right belt","entrance"],
            "unityCompiled":False,"androidTested":False,"entries":entries}

if __name__=="__main__":
    result=audit()
    report=ROOT/"mistrzowie-evidence/arena-three-murals.json"
    report.parent.mkdir(parents=True,exist_ok=True)
    report.write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding="utf-8")
    for entry in result["entries"]:
        if entry["status"]!="PASS":
            print("::error::"+entry["status"]+": "+entry["name"])
    print("CHAMPION_A1_THREE_HD_MURALS",result["status"],result["passed"],"/",result["total"])
    if result["status"]!="PASS":raise SystemExit(1)
