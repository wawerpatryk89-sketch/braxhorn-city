"""Read-only, one-file-at-a-time audit of approved Champions GLB sources.

Inspect original P1 owner ZIP and tracked HarborBeach crane without rewriting
their image, geometry, UV, material or source bytes. NOT a Unity import test.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile

def parse_glb(raw: bytes, name: str) -> dict:
    record = dict(name=name, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(),
                  state="PASS_STRUCTURE", errors=[], warnings=[], meshes=0,
                  primitives=0, vertices=0, materials=0, textures=0,
                  imageCount=0, extensionsRequired=[], extraUVs=[],
                  materialUVs=[])
    def bad(message):
        record["errors"].append(message)
    if len(raw) < 20:
        bad("GLB header missing")
        record["state"]="ERROR_SOURCE"
        return record
    magic, version, size = struct.unpack_from("<4sII",raw,0)
    if magic != b"glTF" or version != 2 or size != len(raw):
        bad("Not valid glTF 2.0 GLB or wrong byte length")
    chunks=[]
    at=12
    while at < len(raw):
        if at+8>len(raw):
            bad("Truncated chunk header")
            break
        n,kind=struct.unpack_from("<I4s",raw,at)
        at+=8
        if n%4 or at+n>len(raw):
            bad("Truncated or unaligned GLB chunk")
            break
        chunks.append((kind,raw[at:at+n]))
        at+=n
    if not chunks or chunks[0][0]!=b"JSON":
        bad("First chunk must contain JSON")
        record["state"]="ERROR_SOURCE"
        return record
    try:
        data=json.loads(chunks[0][1])
    except (ValueError,UnicodeDecodeError) as ex:
        bad("Malformed glTF JSON: "+str(ex))
        record["state"]="ERROR_SOURCE"
        return record
    if not isinstance(data,dict) or str(data.get("asset",{}).get("version"))!="2.0":
        bad("GLTF version not 2.0")
    accesses=data.get("accessors",[])
    mesh=data.get("meshes",[])
    material=data.get("materials",[])
    record["meshes"]=len(mesh)
    record["materials"]=len(material)
    record["textures"]=len(data.get("textures",[]))
    record["imageCount"]=len(data.get("images",[]))
    record["extensionsRequired"]=data.get("extensionsRequired",[])
    record["nodes"]=len(data.get("nodes",[]))
    if not mesh:
        bad("No source mesh")
    for mi,m in enumerate(mesh):
        for pi,p in enumerate(m.get("primitives",[])):
            record["primitives"]+=1
            attrs=p.get("attributes",{})
            if not isinstance(attrs,dict) or "POSITION" not in attrs:
                bad(f"Mesh {mi} primitive {pi} missing POSITION")
                continue
            pos=attrs["POSITION"]
            if not isinstance(pos,int) or pos<0 or pos>=len(accesses):
                bad(f"Mesh {mi} primitive {pi} invalid POSITION accessor")
            else:
                record["vertices"]+=accesses[pos].get("count",0)
            for attr,aid in attrs.items():
                if not isinstance(aid,int) or aid<0 or aid>=len(accesses):
                    bad(f"Mesh {mi} primitive {pi} invalid accessor: {attr}")
                if attr.startswith("TEXCOORD_"):
                    try: uv=int(attr.split("_")[-1])
                    except ValueError: uv=-1
                    if uv>=2:
                        record["extraUVs"].append(dict(mesh=mi,primitive=pi,uv=uv))
    def walk(obj):
        if isinstance(obj,dict):
            for key,value in obj.items():
                if key=="texCoord":
                    if isinstance(value,int) and not isinstance(value,bool):
                        record["materialUVs"].append(value)
                    else:
                        bad("Invalid texture coordinate selector")
                else: walk(value)
        elif isinstance(obj,list):
            for value in obj: walk(value)
    walk(material)
    record["materialUVs"]=sorted(set(record["materialUVs"]))
    if record["extraUVs"]:
        values=sorted(set(x["uv"] for x in record["extraUVs"]))
        if any(x>=2 for x in record["materialUVs"]):
            record["warnings"].append(
                "Material actually REFERENCES unsupported UV2+; preserve appearance, do not strip automatically")
        else:
            record["warnings"].append(
                "Unused UV2+ attributes may be removable from a derived import only, not owner source")
        record["state"]="GLTFAST_UV_COMPATIBILITY_REVIEW_REQUIRED"
    for ext in record["extensionsRequired"]:
        if ext in ("KHR_draco_mesh_compression","EXT_meshopt_compression",
                   "KHR_texture_basisu"):
            record["warnings"].append(
                "Decoder support required for "+ext+"; check matching Unity packages")
    if not record["primitives"]:
        bad("No mesh primitives")
    if record["errors"]:
        record["state"]="ERROR_SOURCE"
    return record

def main() -> int:
    parser=argparse.ArgumentParser()
    parser.add_argument("--owner-zip",type=Path,required=True)
    parser.add_argument("--harbor-glb",type=Path,required=True)
    parser.add_argument("--output",type=Path,default=Path("mistrzowie-evidence/source-glb-audit.json"))
    args=parser.parse_args()
    original_archive=args.owner_zip
    archive_sha=hashlib.sha256(original_archive.read_bytes()).hexdigest()
    expected="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"
    if archive_sha!=expected:
        raise ValueError("Wrong approved P1 ZIP SHA256 - refuse to use different models")
    reports=[]
    with zipfile.ZipFile(original_archive) as archive:
        if archive.testzip() is not None:
            raise ValueError("Original P1 ZIP CRC invalid")
        assets=sorted(n for n in archive.namelist() if n.lower().endswith(".glb"))
        if not assets:
            raise ValueError("No GLB in approved P1 owner ZIP")
        for index,name in enumerate(assets,1):
            data=archive.read(name)
            report=parse_glb(data,name)
            reports.append(report)
            print(f"BRAX_REAL_GLB {index}/{len(assets)} {name} "
                  f"{report['state']} vertices={report['vertices']} "
                  f"UV={report['materialUVs']}",flush=True)
    harbor=args.harbor_glb
    if not harbor.is_file():
        raise FileNotFoundError("Harbor crane original missing: "+str(harbor))
    report=parse_glb(harbor.read_bytes(),str(harbor))
    reports.append(report)
    print("BRAX_REAL_GLB HARBOR "+report["state"]+" "
          +str(report["extensionsRequired"]),flush=True)
    summary={
        "status":"SOURCE_STRUCTURE_PASS" if all(not r["errors"] for r in reports)
                else "SOURCE_STRUCTURE_ERRORS",
        "checked":len(reports),
        "p1Models":len(reports)-1,
        "glTFastUvReview":[r["name"] for r in reports if r["extraUVs"]],
        "sourceErrors":[r["name"] for r in reports if r["errors"]],
        "notes":"GLB source/UV review only; not a Unity importer or in-game visual check.",
        "files":reports,
    }
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(summary,indent=2,ensure_ascii=False)+"\n",encoding="utf8")
    print("BRAX_REAL_GLB_REPORT "+json.dumps({k:summary[k] for k in
          ("status","checked","p1Models","glTFastUvReview","sourceErrors")}),flush=True)
    return 0 if not summary["sourceErrors"] else 1

if __name__=="__main__":
    sys.exit(main())
