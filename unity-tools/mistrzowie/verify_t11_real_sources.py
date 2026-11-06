"""Verify downloaded, unchanged Poly Haven original 2K files and embedded GLBs.

This runs without Unity/Android and leaves the source bytes untouched.
"""
from pathlib import Path
import hashlib
import json
import struct
from PIL import Image
from prepare_casino_textures_2k import DEST as MAP_DIR
from prepare_casino_vegetation_2k import DEST as ORIGINAL, UNITY_DEST as MODELS, SLUGS

PROJECT=Path(__file__).resolve().parents[2]/"unity-city"

def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as stream:
        for piece in iter(lambda:stream.read(1<<20),b""):
            h.update(piece)
    return h.hexdigest()

def verify_maps():
    data=json.loads((MAP_DIR.parent/"t11-polyhaven-2k-manifest.json").read_text())
    assert data["status"]=="2K_SOURCE_VERIFIED" and len(data["rows"])==21
    assert len({r["file"] for r in data["rows"]})==21
    for record in data["rows"]:
        path=MAP_DIR/record["file"]
        assert path.is_file() and sha(path)==record["sha256"],path
        with Image.open(path) as image:
            assert image.size==(2048,2048),(path,image.size)

def verify_models():
    data=json.loads((ORIGINAL/"source-vegetation-2k.json").read_text())
    assert len(data["losslessEmbeddedModels"])==len(SLUGS)==2
    for source in data["originalSourceFiles"]:
        path=PROJECT/source["file"]
        assert path.is_file() and sha(path)==source["sha256"],path
        if path.suffix.lower() in (".jpg",".png"):
            with Image.open(path) as image:
                assert image.size==(2048,2048),path
    for slug in SLUGS:
        path=MODELS/slug/(slug+"_2k_embedded.glb")
        original=ORIGINAL/slug/(slug+"_2k.gltf")
        assert path.is_file() and original.is_file(),slug
        original_json=json.loads(original.read_bytes())
        receipt=next(x for x in data["losslessEmbeddedModels"] if x["embeddedGlb"]==str(path))
        assert sha(path)==receipt["glbSha256"]
        assert sha(original)==receipt["originalSha256"]
        raw=path.read_bytes()
        magic,version,total=struct.unpack_from("<4sII",raw)
        assert (magic,version,total)==(b"glTF",2,len(raw))
        json_size,typ=struct.unpack_from("<I4s",raw,12)
        assert typ==b"JSON"
        doc=json.loads(raw[20:20+json_size])
        bin_size,bin_typ=struct.unpack_from("<I4s",raw,20+json_size)
        assert bin_typ==b"BIN\x00"
        assert bin_size==doc["buffers"][0]["byteLength"]
        assert 28+json_size+bin_size==len(raw)
        for field in ("asset","meshes","nodes","accessors","materials","textures","samplers","scenes","scene"):
            assert doc.get(field)==original_json.get(field),(slug,field)
        for image in doc.get("images",[]):
            assert "uri" not in image and "bufferView" in image
            view=doc["bufferViews"][image["bufferView"]]
            assert view["byteOffset"]+view["byteLength"]<=bin_size
        assert not any("uri" in b for b in doc["buffers"])

if __name__=="__main__":
    verify_maps()
    verify_models()
    evidence={"status":"LIVE_T11_SOURCE_AUDIT_PASSED","native2048Maps":21,
              "originalVegetationGLBs":2,"originalBytesPreserved":True,
              "unityRun":False,"apkBuilt":False}
    Path("t11-source-audit.json").write_text(json.dumps(evidence,indent=2)+"\n")
    print(json.dumps(evidence))
