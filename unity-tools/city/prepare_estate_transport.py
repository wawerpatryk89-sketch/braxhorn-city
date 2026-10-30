#!/usr/bin/env python3
"""Prepare original shared-city Brax estate WebGL for iPhone, without altering Unity scene/assets."""
import hashlib,json,os,pathlib,re,zlib
root=pathlib.Path(os.environ["PLAYER_DIR"])
data=list((root/"Build").glob("*.data.unityweb"))
assert len(data)==1,[(x.name) for x in data]
p=data[0]
with p.open("rb") as f:assert f.read(2)==b"\x1f\x8b","Expected original Unity gzip, refuse synthetic transport"
digest=hashlib.sha256();decoded=hashlib.sha256();inflater=zlib.decompressobj(31)
parts=[];uncompressed=0;total=0
with p.open("rb") as src:
 for i in range(100):
  b=src.read(32*1024*1024)
  if not b:break
  q=p.with_name(p.name+f".part{i:03d}");q.write_bytes(b)
  digest.update(b);total+=len(b)
  raw=inflater.decompress(b);uncompressed+=len(raw);decoded.update(raw)
  parts.append(dict(path="Build/"+q.name,bytes=len(b),sha256=hashlib.sha256(b).hexdigest()))
assert parts and inflater.eof and not inflater.unused_data
tail=inflater.flush();uncompressed+=len(tail);decoded.update(tail)
assert total==p.stat().st_size and uncompressed>total
manifest=dict(dataPath="Build/"+p.name,bytes=total,sha256=digest.hexdigest(),parts=parts,
 sourceCommit="140407203586a6c150faf6355ebd4c1a2faa55c7",assetQualityChanged=False,
 uncompressedBytes=uncompressed)
loaderSource=pathlib.Path("unity-release/ios-stream-loader.js").read_text()
assert loaderSource.count("__MANIFEST__")==1
(root/"city-transport.js").write_text(loaderSource.replace("__MANIFEST__",json.dumps(manifest,separators=(",",":"))))
loaders=list((root/"Build").glob("*.loader.js"));assert len(loaders)==1
loader=loaders[0];s=loader.read_text()
if "window.BraxCityFetch" not in s:
 m=re.search(r"fetchWithProgress=function\(e,t\)\{.{0,190}?fetch\(e,t\)\.then",s)
 assert m,"Unity loader download hook missing; refuse publishing non-streaming iPhone build"
 span=m.group(0);new=span.replace("fetch(e,t).then","(window.BraxCityFetch||fetch)(e,t).then")
 assert span!=new
 s=s[:m.start()]+new+s[m.end():]
 loader.write_text(s)
assert "window.BraxCityFetch" in loader.read_text()
index=root/"index.html";html=index.read_text()
assert html.count("<script>")>=1 and "city-transport.js" not in html
html=html.replace("<script>","<script src=\"city-transport.js\"></script><script>",1)
html=html.replace("function fail(e){","window.addEventListener('brax-city-load-error',e=>fail(e.detail));\nfunction fail(e){",1)
index.write_text(html)
p.unlink()
assert not p.exists() and len(parts)>=6
(root/"estate-stream-manifest.json").write_text(json.dumps(manifest,indent=2)+"\n")
print("ESTATE_NATIVE_STREAM_READY",json.dumps(dict(compressed=total,decoded=uncompressed,parts=len(parts),sha256=digest.hexdigest(),decodedSha256=decoded.hexdigest())))
