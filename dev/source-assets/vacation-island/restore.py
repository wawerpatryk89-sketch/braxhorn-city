from pathlib import Path
import hashlib, zipfile
root=Path(__file__).resolve().parent
out=root/'vacation-island-unity-main.zip'
with out.open('wb') as dst:
    for name in ['vacation-island-unity-main.zip.part001','vacation-island-unity-main.zip.part002']:
        with (root/name).open('rb') as src:
            while data:=src.read(1024*1024): dst.write(data)
assert out.stat().st_size==14856362
assert hashlib.sha256(out.read_bytes()).hexdigest()=='739a819323f134d5cd6b555560b5c8476f6f5ead2165ead72f8efcc91edf3d0d'
with zipfile.ZipFile(out) as z:
    assert z.testzip() is None
print('OK:',out)
