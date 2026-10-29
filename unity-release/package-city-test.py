"""Split transport only. Every original Unity data byte and the scene stay intact."""
import hashlib,json,pathlib
root=pathlib.Path('unity-preview');build=root/'Build'
source='19c71c167d4426523a251b94c3aeabbe9664aad5'
assert json.loads((root/'city-migration.json').read_text())['commit']==source
large=list(build.glob('*.data.unityweb'));assert len(large)==1
p=large[0];sha=hashlib.sha256();parts=[];total=0
with p.open('rb') as data:
 while payload:=data.read(32*1024*1024):
  name=p.name+f'.part{len(parts):03d}';(build/name).write_bytes(payload);sha.update(payload);total+=len(payload)
  parts.append({'path':'Build/'+name,'bytes':len(payload),'sha256':hashlib.sha256(payload).hexdigest()})
transport={'dataPath':'Build/'+p.name,'bytes':total,'sha256':sha.hexdigest(),'parts':parts,'sourceCommit':source,'assetQualityChanged':False}
combined=hashlib.sha256()
for part in parts:combined.update((root/part['path']).read_bytes())
assert combined.hexdigest()==transport['sha256']
(root/'transport.json').write_text(json.dumps(transport,indent=2)+'\n')
js='''/* Lossless transport for the existing Unity build. No scene/model changes. */
'use strict';
(()=>{
const manifest=__MANIFEST__;
const nativeFetch=window.fetch.bind(window);
const dataUrl=new URL(manifest.dataPath,document.baseURI).href;
window.BraxCityFetch=async function(input,options={}){
 const url=new URL(typeof input==='string'?input:input.url,document.baseURI).href;
 if(url!==dataUrl)return nativeFetch(input,options);
 let part=0,reader=null,received=0;
 const stream=new ReadableStream({
  async pull(controller){
   try{
    while(true){
     if(!reader){
      if(part>=manifest.parts.length){if(received!==manifest.bytes)throw Error('Incomplete Unity data');controller.close();return;}
      const segment=manifest.parts[part++];
      const response=await nativeFetch(new URL(segment.path,document.baseURI).href,{method:'GET',signal:options.signal});
      if(!response.ok||!response.body)throw Error('Unity data segment unavailable: '+response.status);
      reader=response.body.getReader();
     }
     const item=await reader.read();
     if(item.done){reader=null;continue;}
     received+=item.value.byteLength;controller.enqueue(item.value);return;
    }
   }catch(error){controller.error(error);}
  },
  async cancel(reason){if(reader)await reader.cancel(reason);}
 });
 return new Response(stream,{status:200,headers:{'Content-Type':'application/octet-stream','Content-Length':String(manifest.bytes)}});
};
})();
'''.replace('__MANIFEST__',json.dumps(transport,separators=(',',':')))
(root/'city-transport.js').write_text(js)
loader=next(build.glob('*.loader.js'));text=loader.read_text()
old='fetch(e,t).then(function(e){return b.readBodyWithProgress';assert text.count(old)==1
text=text.replace(old,'(window.BraxCityFetch||fetch)(e,t).then(function(e){return b.readBodyWithProgress',1)
old='n=b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress';assert text.count(old)==1
text=text.replace(old,'n=window.BraxCityFetch?b.fetchWithProgress:(b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress)',1)
loader.write_text(text);p.unlink()
index=root/'index.html';html=index.read_text();assert '<base ' not in html
html=html.replace('<head>','<head><base href="/unity-gym/">',1)
html=html.replace('<script>','<script src="city-transport.js"></script><script>',1)
html=html.replace('Pierwsze wejście pobiera dane Unity.','Pierwsze wejście pobiera około 291 MB danych Unity. Użyj Wi-Fi.')
index.write_text(html)
files={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in root.rglob('*') if p.is_file() and p.name!='vercel.json'}
(root/'release.json').write_text(json.dumps({'scope':'isolated-unity-gym','iteration':'connected-city-and-estate','sourceCommit':source,'sourceRun':40842927461,'originalCityReplaced':False,'fullGameplayMigration':False,'assetQualityChanged':False,'transport':transport,'files':files},indent=2)+'\n')
print('LOSSLESS_CITY_PACKAGE',total,len(parts),sha.hexdigest())
