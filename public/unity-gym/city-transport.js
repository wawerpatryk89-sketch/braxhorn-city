/* Lossless transport for the existing Unity build. No scene/model changes. */
'use strict';
(()=>{
const manifest={"dataPath":"Build/19c71c167d44_WebGL.data.unityweb","bytes":286821275,"sha256":"f5cf692beb441d7a69f0d41361475533def4f2c02e6e55fddc3ad1b89fd0fe06","parts":[{"path":"Build/19c71c167d44_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"7ae445763fb45c70274d0661051c048cdba1e4b13303b795b4a1dcd1c248251d"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"f24cec6fc9c7fa0be31135fa3bfbc0d8a80dbeee152518456b93ee746e28a5b7"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"d522edf3775471c67727fde6ace5469a9ec31c9490f1788ba7f61c8d53cb368d"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"6961721bb53e72f063bcbf74d14a244601b6c29cde52e4cb174be0ab92a09d0f"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"e4ed965491a50e5db12714e938969f54e67eb01ef1bec6291259ea810bd9a10c"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"2b775d4d19b20ff903c05e50664e07b2af3890cc6564950369adaa6b786adac2"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"26c0b3045a315453609832a566b94d6739020dcbb8b8fd6fbd3be01b9ab7c7b4"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"fba9ab335cdc4d2d60637c2fed76000e532c01d56ac624d26af0b9da36e08223"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part008","bytes":18385819,"sha256":"a61ca423b0dbfeaae420dcba2e808e1afe7fa18ab2cd3444e921ba958e8c9c61"}],"sourceCommit":"19c71c167d4426523a251b94c3aeabbe9664aad5","assetQualityChanged":false};
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
