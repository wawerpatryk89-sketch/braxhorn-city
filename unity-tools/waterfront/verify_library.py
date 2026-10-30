#!/usr/bin/env python3
"""Fail closed on staged BRAXHORN owner Library assets. Does not synthesize substitutes."""
from pathlib import Path
import argparse, hashlib, json, sys
EXPECTED={
  "Models/bateau_riva_boat.glb": "792eb1880a04c8d30f1ec87f5b6275fa13e3b3a4bd308a42e7cde12cf47b6da9",
  "Models/harbor_crane.glb": "680bbc4800df1ab3bf9046ef9f8a24ea8cc3ff39757f6954f8a133af72661349",
  "Models/jetski_collection.glb": "0019663a3539f47486abe8fc95ee369e96f0d9310696fa1b6a4326ad9d057f39",
  "Models/lifeguard_tower.glb": "255c609ca25fcaa8b5b55a0377121449ce358cbb85e8a1d3c55990e559bfca0a",
  "Models/rustic_wooden_dock.glb": "77f06a3b6abb7a876876284fb07124d3837a52e112fbe6f5cf6d007f9eabc5c4",
  "Models/yacht.glb": "4775965a62cd5359a05171b25fd793dc1ffc0f37749b55dd55637e175467f39e",
  "Sand/coast_sand_02_arm_2k.png": "d85a86006a915d6c7c6c313725f5049b37fcd3cddbb9fcdf7f4d15e6e0b5f986",
  "Sand/coast_sand_02_diff_2k.jpg": "e2b8dffe6fb249d18a5b0960837397f2ce38e8a7d77e2d3236ff978328b45a6a",
  "Sand/coast_sand_02_disp_2k.png": "bd2239723478ce5d14a52fd43dcdeb12087a936e9fa20f1a9c9f34a40fd78a8b",
  "Sand/coast_sand_02_normal_2k.png": "769ef01e7943d00a53442ac615b6df965384c2e5bda59b77fc254a36df7164c7",
  "Sand/damp_sand_arm_2k.png": "221e4fa8c189563d1f6f7d3bda20818526ab613d137ac11a1505cb89b82b923e",
  "Sand/damp_sand_diff_2k.jpg": "380404242fbb799984e970f765118b5985671716558a328c4c9ce3e23a2ae380",
  "Sand/damp_sand_disp_2k.png": "39b6a1a41c9611f87124472cb0a0745d1ce23998e5f3e0b9908d9c12920f9049",
  "Sand/damp_sand_normal_2k.png": "7f98343bbf28542240ded48c7c1734a56612a4b558da74e1531376744e8fb5ae"
}
def main():
 p=argparse.ArgumentParser();p.add_argument("--root",default="unity-city/Assets/Source/Waterfront")
 args=p.parse_args();root=Path(args.root);errors=[]
 for name,expected in EXPECTED.items():
  f=root/name
  if not f.is_file():
   errors.append({"path":name,"error":"missing"});continue
  h=hashlib.sha256()
  with f.open("rb") as stream:
   for b in iter(lambda:stream.read(1024*1024),b""):h.update(b)
  if h.hexdigest()!=expected:errors.append({"path":name,"error":"sha256 mismatch","actual":h.hexdigest()})
 report={"engine":"Unity6","source_count":len(EXPECTED),"sha256_ok":not errors,"errors":errors,
   "unity_import_pass":False,"visual_pass":False,"production_publish":False}
 print(json.dumps(report,indent=2))
 return 1 if errors else 0
if __name__=="__main__":sys.exit(main())
