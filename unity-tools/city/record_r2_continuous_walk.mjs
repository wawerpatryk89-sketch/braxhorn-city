// Real-time Unity WebGL capture: one uninterrupted original BraxGymPlayer walk.
// NO ReviewView, TravelPoint, TravelTo, reset, synthetic frames or scene switches.
// This is a PREPARED recorder. A real importer-produced R2 remodel attestation is
// mandatory; without it, fail closed and publish nothing.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';

const root=path.resolve('unity-preview');
const out=path.resolve('estate-r2-film-review');
fs.mkdirSync(out,{recursive:true});
const verification=path.resolve('city-review/r2-remodel-manifest.json');
const required=['roofTextureBindings','windowModelsInstanced','doorModelsInstanced'];
const audit={passed:false,recordingKind:'ONE CONTINUOUS LIVE UNITY6 WALK',teleportsAllowed:false,teleportsUsed:0,frames:[],stops:[],sourceSha:process.env.SOURCE_BUILD_SHA||process.env.GITHUB_SHA||null};
let server,browser,context,page;
const logs=[];
const fail=message=>{throw new Error(message)};
const requireRealRemodel=()=>{
 if(!fs.existsSync(verification))fail('R2 Unity remodel attestation missing; light-only/Three.js builds are not eligible for film release');
 const report=JSON.parse(fs.readFileSync(verification,'utf8'));
 if(report.scene!=='BraxhornEstate'||report.importer!=='Unity6' || report.originalBraxPreserved!==true)fail('Remodel attestation identity mismatch');
 for(const key of required)if(!Number.isFinite(report[key])||report[key]<1)fail('No verified Unity scene implementation of '+key);
 if(report.visualQAConfirmed!==true)fail('Actual Unity visual QA not yet accepted');
 if(!report.sourceAssetHashes||Object.keys(report.sourceAssetHashes).length<4)fail('Missing verified material/model source hashes');
 return report;
};
const status=()=>page.evaluate(()=>window.BraxhornState);
const send=(method,value='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,value]);
const sleep=ms=>page.waitForTimeout(ms);
const distance=(a,b)=>Math.hypot(a.x-b.x,a.z-b.z);
const route=[
 {x:0,z:82,label:'Wejście do osiedla'},
 {x:0,z:59,label:'Domy przy ulicy'},
 {x:0,z:36,label:'Zabudowa Wiśniowej'},
 {x:0,z:13,label:'Ulica i ogródki'},
 {x:0,z:-9,label:'Dojście do willi'},
 {x:0,z:-24,label:'Willa — front'},
];
const visited=[];
let video;
try{
 const remodel=requireRealRemodel();
 server=http.createServer((req,res)=>{
  let file=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);
  if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
  if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
  if(!fs.existsSync(file)){res.writeHead(404);res.end();return}
  res.setHeader('Content-Type',({'.html':'text/html','.js':'text/javascript','.json':'application/json','.wasm':'application/wasm'})[path.extname(file)]||'application/octet-stream');
  fs.createReadStream(file).pipe(res);
 });
 await new Promise(resolve=>server.listen(8765,'127.0.0.1',resolve));
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:960,height:540},isMobile:true,hasTouch:true,deviceScaleFactor:1,recordVideo:{dir:out,size:{width:960,height:540}}});
 page=await context.newPage();video=page.video();
 page.on('console',msg=>logs.push(msg.type()+': '+msg.text()));
 page.on('pageerror',err=>logs.push('PAGEERROR '+err.message));
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:480000,polling:100});
 if(await page.evaluate(()=>!!window.BraxhornError))fail('Unity runtime failed to start');
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:120000,polling:100});
 let previous=await status();
 if(previous.area!=='estate'||previous.cityMeshes!==553||previous.build!==audit.sourceSha)fail('Wrong Unity scene, geometry, or Brax source SHA');
 if(Math.abs(previous.x)>1.5||Math.abs(previous.z-92)>3)fail('Player already relocated; reject noncontinuous recording');
 if(!previous.handFound||!previous.leftFootFound||!previous.rightFootFound)fail('Original Brax skeleton incomplete');
 let traveled=0,groundedSamples=0,totalSamples=0,stuck=0;
 for(const [index,target] of route.entries()){
  let elapsed=0,arrived=false;
  while(elapsed<90){
   const s=await status();
   if(!s||s.area!=='estate'||s.cityMeshes!==553||s.build!==audit.sourceSha)fail('Lost original Unity estate/player during walk');
   const remain=distance(s,target);
   if(remain<1.7){arrived=true;break}
   const dx=(target.x-s.x)/remain,dz=(target.z-s.z)/remain;
   const x=.65*(dx*s.rightX+dz*s.rightZ),y=.65*(dx*s.forwardX+dz*s.forwardZ);
   await send('MoveInput',x.toFixed(4)+','+y.toFixed(4));
   await sleep(600);
   const next=await status();
   if(!next||next.loadingArea||next.area!=='estate')fail('Area switched mid-walk');
   const delta=distance(next,previous);
   const simDelta=next.simulationTime-previous.simulationTime;
   if(simDelta<0||delta>Math.max(2,simDelta*6.2+1.2))fail('DISCONTINUITY / teleport-like displacement '+delta.toFixed(2));
   if(Math.abs(next.y-previous.y)>2.5)fail('Suspicious vertical discontinuity');
   if(delta<.035)stuck++;else stuck=0;
   if(stuck>12)fail('BLOCKED collision/navigation near '+JSON.stringify({x:next.x,z:next.z,target}));
   traveled+=delta;totalSamples++;if(next.grounded)groundedSamples++;
   audit.frames.push({x:+next.x.toFixed(3),y:+next.y.toFixed(3),z:+next.z.toFixed(3),t:+next.simulationTime.toFixed(2),animation:next.animation});
   previous=next;elapsed+=.6;
  }
  await send('MoveInput','0,0');
  if(!arrived)fail('Walking route unreachable without teleport at '+target.label);
  const reached=await status();
  if(distance(reached,target)>2.3)fail('Waypoint mismatch '+target.label);
  await sleep(900);
  const shot=String(index+1).padStart(2,'0')+'-'+target.label.replaceAll(/[^\w\p{L}]+/gu,'-').toLowerCase()+'.png';
  await page.screenshot({path:path.join(out,shot),timeout:60000});
  visited.push({label:target.label,x:reached.x,z:reached.z,shot});
 }
 await send('MoveInput','0,0');await sleep(1000);
 if(traveled<75||groundedSamples/Math.max(1,totalSamples)<.8)fail('Insufficient verified uninterrupted ground traversal '+traveled);
 const exceptions=logs.filter(line=>/PAGEERROR|NullReferenceException|MissingReferenceException|abort\(|memory access out of bounds/i.test(line));
 if(exceptions.length)fail('Unity runtime errors: '+exceptions.slice(0,5).join(' | '));
 await page.close();
 const raw=path.join(out,'real-unity-live.webm');
 await video.saveAs(raw);
 await context.close();await browser.close();browser=null;
 const output=path.join(out,'braxhorn-r2-ciagly-spacer.mp4');
 const encoding=spawnSync('ffmpeg',['-hide_banner','-loglevel','error','-y','-i',raw,'-an','-vf','fps=20,scale=960:540:flags=lanczos,format=yuv420p','-c:v','libx264','-preset','veryfast','-crf','21','-movflags','+faststart',output],{encoding:'utf8',timeout:240000});
 if(encoding.status!==0)fail('MP4 encoder failed '+encoding.stderr);
 const probe=spawnSync('ffprobe',['-v','error','-show_entries','format=duration,size','-of','json',output],{encoding:'utf8'});
 if(probe.status!==0)fail('MP4 ffprobe failed '+probe.stderr);
 const info=JSON.parse(probe.stdout).format;
 if(Number(info.duration)<45||Number(info.size)<400000)fail('Invalid physical walk video evidence');
 audit.passed=true;
 audit.scene='BraxhornEstate';audit.originalCityPlayer='BraxGymPlayer';audit.cityMeshes=553;
 audit.verifiedRemodel=remodel; audit.stops=visited;audit.groundTraveledMeters=+traveled.toFixed(2);
 audit.groundedSamples=groundedSamples;audit.totalSamples=totalSamples;audit.durationSeconds=Number(info.duration);
 audit.mp4Bytes=Number(info.size);audit.browser='Chromium WebGL2/SwiftShader, NOT physical iPhone';
 console.log('R2_CONTINUOUS_REAL_UNITY_VIDEO_PASS',JSON.stringify({traveled,stops:visited.length,duration:info.duration,bytes:info.size}));
}catch(error){
 audit.error=String(error);process.exitCode=1;console.error('R2_CONTINUOUS_WALK_FAIL',error);
 try{await send('MoveInput','0,0')}catch{}
 try{if(page&&!page.isClosed())await page.screenshot({path:path.join(out,'FAIL.png')})}catch{}
}finally{
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(audit,null,2));
 fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
 try{await page?.close()}catch{}
 try{await context?.close()}catch{}
 try{await browser?.close()}catch{}
 try{server?.close()}catch{}
}
