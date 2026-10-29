"""Patch the loader only. Never rebuild or alter city, estate, gym or character data."""
import hashlib,json,os,pathlib,zlib
root=pathlib.Path(os.environ.get('PLAYER_ROOT','deployed/public/unity-gym'))
release=json.loads((root/'release.json').read_text())
source='19c71c167d4426523a251b94c3aeabbe9664aad5'
assert release['sourceCommit']==source
assert release['scope']=='isolated-unity-gym' and release['originalCityReplaced'] is False
manifest=json.loads((root/'transport.json').read_text())
assert manifest['sourceCommit']==source and manifest['bytes']==286821275
original={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in root.rglob('*') if p.is_file()}
for name,digest in release['files'].items():
 assert original[name]==digest,'Source differs from reviewed release: '+name
compressed=hashlib.sha256();decoded=hashlib.sha256();count=0;inflater=zlib.decompressobj(31)
for item in manifest['parts']:
 p=root/item['path'];assert p.stat().st_size==item['bytes']
 partHash=hashlib.sha256()
 with p.open('rb') as stream:
  while chunk:=stream.read(65536):
   compressed.update(chunk);partHash.update(chunk)
   data=inflater.decompress(chunk);count+=len(data);decoded.update(data)
 assert partHash.hexdigest()==item['sha256']
last=inflater.flush();count+=len(last);decoded.update(last)
assert inflater.eof and not inflater.unused_data and compressed.hexdigest()==manifest['sha256']
assert count==358936591 and decoded.hexdigest()=='fc8c053c6c53e7d8b39467d6b8faf26bf07388011a21c2c1dfa55d5cfa15acc5'
manifest['uncompressedBytes']=count
code=pathlib.Path('unity-release/ios-stream-loader.js').read_text()
assert code.count('__MANIFEST__')==1
(root/'city-transport.js').write_text(code.replace('__MANIFEST__',json.dumps(manifest,separators=(',',':'))))
index=root/'index.html';html=index.read_text()
assert html.count('src="city-transport.js"')==1
html=html.replace('src="city-transport.js"','src="city-transport.js?v=ios-stream-01"',1)
assert html.count('function fail(e){')==1
html=html.replace('function fail(e){',"window.addEventListener('brax-city-load-error',e=>fail(e.detail));\nfunction fail(e){",1)
index.write_text(html)
changed=[]
for name,digest in original.items():
 actual=hashlib.sha256((root/name).read_bytes()).hexdigest()
 if actual!=digest:changed.append(name)
assert sorted(changed)==['city-transport.js','index.html'],changed
for name in changed:release['files'][name]=hashlib.sha256((root/name).read_bytes()).hexdigest()
release['loadingFix']={'revision':'ios-stream-01','qaPassed':False,'sourceBefore':'dcc96fa09716a69d65c08252c663282494761642','sourceCommit':source,'nativeStreamingGzip':True,'maxInputChunkBytes':65536,'compressedBytes':manifest['bytes'],'uncompressedBytes':count,'uncompressedSha256':decoded.hexdigest(),'unityBinaryChanged':False,'visualAssetsChanged':False,'physicalIPhoneValidated':False}
(root/'release.json').write_text(json.dumps(release,indent=2)+'\n')
proof={'sourceCommit':source,'beforeCommit':'dcc96fa09716a69d65c08252c663282494761642','changedFiles':changed+['release.json'],'originalRuntimeFilesUnchanged':True,'compressedSha256':compressed.hexdigest(),'uncompressedSha256':decoded.hexdigest(),'uncompressedBytes':count,'originalFiles':original}
pathlib.Path('ios-review').mkdir(exist_ok=True)
pathlib.Path('ios-review/asset-identity.json').write_text(json.dumps(proof,indent=2)+'\n')
print('ORIGINAL_CITY_ASSETS_UNCHANGED',len(original)-2,'STREAMED_BYTES',count)
