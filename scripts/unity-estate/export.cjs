#!/usr/bin/env node
// Exact source-procedural capture + binary-asset preflight; no fallback models.
'use strict';
const fs=require('fs'),path=require('path'),crypto=require('crypto'),{spawn}=require('child_process');
const {chromium}=require('playwright');
const root=path.resolve(__dirname,'../..');
const output=path.resolve(process.argv[2]||'unity-city/Assets/EstateMigration/Generated');
fs.mkdirSync(output,{recursive:true});
const PORT=Number(process.env.ESTATE_EXPORT_PORT||18777);
const sourceScripts=['dev/osiedle/layout.js','dev/osiedle/level.js','dev/osiedle/world.js',
 'dev/osiedle/civic.js','dev/osiedle/arenas.js','dev/osiedle/entrances.js',
 'dev/osiedle/doors.js','dev/osiedle/vegetation.js'];
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const instances=[
 ...requireHousePlacements(),
 ['garaze_sowieckie',-35,-39,23,Math.PI/2],
 ['gameready_casino_scene',-86,-43,34,Math.PI/2],
 ['policestation',86,-43,31,0],
 ['nightclub',-86,14,12.6,Math.PI],
 ['blackjack_table',-99,-31,3.1,0],
 ['roulette_table',-73,-31,3.8,0],
 ['stylized_poker_table_-_game_asset',-99,-54,4,0],
 ...[75,97].flatMap(x=>[6,16].map(z=>['hospital_bed',x,z,1.15,0])),
 ...[93,97,101].map(x=>['hospital_chair',x,25,.65,Math.PI]),
 ['hospital_wheelchair',102,23,.75,-Math.PI/2]
];
function requireHousePlacements(){
 // Kept in sync with level.js; the browser source layout is independently cross-checked below.
 return [
  ['dom_suburban',-28,27,17,Math.PI/2],
  ['dom_nowoczesny',27,28,14,-Math.PI/2],
  ['willa_nowoczesna',-29,0,17,Math.PI/2],
  ['dom_z_wnetrzem',28,0,14,-Math.PI/2],
  ['dom_wnetrze_zewn',-28,59,20,0],
  ['dom_nowoczesny',28,59,15,-Math.PI/2]
 ];
}
const modelNames=new Set(instances.map(x=>x[0]));
const vegetationNames=['park_tree','park_shrub','park_rock'];
function locate(name){
 const isHouse=['dom_suburban','dom_nowoczesny','willa_nowoczesna','dom_z_wnetrzem','dom_wnetrze_zewn','garaze_sowieckie'].includes(name);
 const options=isHouse?[path.join('dev/models',name+'.glb')]:
  [path.join('public/osiedle/assets',name+'.glb'),path.join('dev/osiedle/assets',name+'.glb')];
 return options.find(p=>fs.existsSync(path.join(root,p)))||null;
}
async function main(){
 const server=spawn('python3',['-m','http.server',String(PORT),'--bind','127.0.0.1'],{cwd:root,stdio:'ignore'});
 let browser;
 try{
  await new Promise(r=>setTimeout(r,1500));
  browser=await chromium.launch({headless:true,args:[
   '--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist'
  ]});
  const page=await browser.newPage({acceptDownloads:true,viewport:{width:1024,height:640}});
  const exceptions=[];
  page.on('pageerror',e=>exceptions.push(e.stack||e.message));
  await page.goto('http://127.0.0.1:'+PORT+'/dev/osiedle/tools/unity-export.html');
  await page.waitForFunction(()=>window.__estateExport&&(window.__estateExport.ready||window.__estateExport.error),null,{timeout:180000});
  const failed=await page.evaluate(()=>window.__estateExport.error);
  if(failed)throw new Error(failed);
  const manifest=await page.evaluate(()=>window.__estateExport.manifest);
  if(instances.length!==21||manifest.sourceModelInstancesExpected!==instances.length)throw new Error('Model inventory changed');
  for(let i=0;i<6;i++){
   const h=manifest.level.houses[i],p=instances[i];
   if([h.model,h.x,h.z,h.width,h.rotation].some((v,j)=>typeof v==='number'?Math.abs(v-p[j])>1e-8:v!==p[j]))
    throw new Error('House source/layout drift: '+h.id);
  }
  const downloading=page.waitForEvent('download',{timeout:240000});
  await page.evaluate(()=>window.__estateExport.download());
  const download=await downloading;
  const exportPath=path.join(output,'EstateProceduralAndDoors.glb');
  await download.saveAs(exportPath);
  const models=[...new Set([...modelNames,...vegetationNames])].map(name=>{
   const p=locate(name);
   return {name,source:p||null,present:!!p,bytes:p?fs.statSync(path.join(root,p)).size:null,
    sha256:p?hash(path.join(root,p)):null};
  });
  const missing=models.filter(x=>!x.present).map(x=>x.name);
  manifest.sourceModels=instances.map(([name,x,z,width,rotation])=>({name,x,z,width,rotation,
   source:models.find(m=>m.name===name).source}));
  manifest.binaryAssets=models;
  manifest.sourceHashes=Object.fromEntries(sourceScripts.map(p=>[p,hash(path.join(root,p))]));
  manifest.generatedGlbSha256=hash(exportPath);
  manifest.generatedGlbBytes=fs.statSync(exportPath).size;
  manifest.missingOriginalModels=missing;
  manifest.status=missing.length?'BLOCKED_MISSING_ORIGINAL_MODELS':'EXPORTED_NOT_UNITY_ACCEPTED';
  manifest.sourceEntrypoint=[0,.08,92]; // observed in dev/osiedle/main.js boot(), without prior save
  manifest.qa={threeSourceCaptured:true,unityCompiled:false,unitySceneWalked:false,
   screenshotsFromUnity:[],physicalPhoneTested:false,visualAccepted:false};
  fs.writeFileSync(path.join(output,'EstateSourceManifest.json'),JSON.stringify(manifest,null,2)+'\n');
  fs.writeFileSync(path.join(output,'SOURCE_AUDIT.txt'),
   'SOURCE IMPORT: '+manifest.status+'\nMissing source GLBs: '+(missing.join(', ')||'none')+
   '\nUnity compile: NOT TESTED\nUnity 1:1 visual acceptance: NOT TESTED\n');
  if(exceptions.length)throw new Error('Browser export errors: '+exceptions.join('; '));
  console.log('Procedural world and 24 original source door definitions exported: '+manifest.generatedGlbBytes+' bytes');
  console.log('Source model instances: '+instances.length+', vegetation placements: '+manifest.vegetation.length);
  console.log('Missing source GLBs: '+JSON.stringify(missing));
  console.log('Status: '+manifest.status);
 } finally {if(browser)await browser.close();server.kill();}
}
main().catch(e=>{console.error(e);process.exitCode=1;});
