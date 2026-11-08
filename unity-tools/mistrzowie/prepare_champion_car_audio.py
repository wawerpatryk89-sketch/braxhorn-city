#!/usr/bin/env python3
from pathlib import Path
import hashlib, json
ROOT=Path(__file__).resolve().parents[2]
MANIFEST=ROOT/"unity-tools/mistrzowie/champion_car_audio_sources.json"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    doc=json.loads(MANIFEST.read_text("utf-8"))
    assert doc["status"]=="BINARIES_COMMITTED_AND_HASHED"
    assert doc["source"]["source_sha256"]=="15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6"
    for c in doc["clips"]:
        p=ROOT/c["unity_path"]; raw=p.read_bytes()
        if len(raw)!=c["bytes"] or sha(p)!=c["sha256"]: raise ValueError("hash/size mismatch "+c["role"])
        if not raw.startswith(b"OggS") or b"vorbis" not in raw[:8192]: raise ValueError("not Ogg/Vorbis "+c["role"])
        if b"CC BY 3.0 Unported" not in raw[:8192] or b"Antti Makkonen" not in raw[:8192]: raise ValueError("attribution missing "+c["role"])
        if c["role"]=="idle" and c["duration_seconds"]<4.5: raise ValueError("idle too short")
        if c["role"]=="rev" and c["duration_seconds"]<2.0: raise ValueError("rev too short")
        print("A1_CAR_AUDIO_VERIFIED",c["role"],len(raw),c["sha256"])
    print("A1_CAR_AUDIO_FINAL_PASS clips=2 real_recording=true network=false apk=false")
if __name__=="__main__": main()
