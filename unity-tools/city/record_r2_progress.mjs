// EARLY CHECKPOINT ONLY. Does not bypass or satisfy record_r2_continuous_walk.mjs.
// Before/after uses the imported baseline and scoped after meshes in one real Unity build.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';

const root=path.resolve('unity-preview'), out=path.resolve('r2-progress-review');
fs.mkdirSync(out,{recursive:true});
const sha=process.env.GITHUB_SHA, logs=[], samples=[];
const audit={passed:false,stage:'WORK IN PROGRESS — EARLY CHECKPOINT (15% acceptance pending)',sourceSha:sha,runId:process.env.GITHUB_RUN_ID,scene:'BraxhornEstate',platform:'Chromium WebGL2/SwiftShader mobile viewport; NOT physical iPhone',fullRenovationApproved:false,teleportsDuringFilm:0,preRollPlacement:'Existing ReviewView used BEFORE video interval; no placement or scene switch during film'};
const bindingPath=['city-review/r2-segment-bindings.json','unity-city/city-review/r2-segment-bindings.json'].find(p=>fs.existsSync(p));
if(!bindingPath)throw Error('No actual Unity scoped binding report');
audit.unityBindings=JSON.parse(fs.readFileSync(bindingPath));
if(audit.unityBindings.bindings.length!==8||audit.unityBindings.realTreeInstances!==2||audit.unityBindings.realShrubInstances!==12||audit.unityBindings.fullRenovationApproved!==false)throw Error('Incorrect partial two-house checkpoint attestation');
const server=http.createServer((req,res)=>{
 let f=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);
 if(f!==root&&!f.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');
 if(!fs.existsSync(f)){res.writeHead(404);res.end();return}
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json'})[path.extname(f)]||'application/octet-stream');fs.createReadStream(f).pipe(res);
});
await new Promise(r=>server.listen(8765,'127.0.0.1',r));
let browser,context,page,video;
try{
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:960,height:540},isMobile:true,hasTouch:true,recordVideo:{dir:path.join(out,'raw'),size:{width:960,height:540}}});
 page=await context.newPage();video=page.video();
 page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
 const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
 const state=()=>page.evaluate(()=>window.BraxhornState);
 const compare=method=>page.evaluate(m=>window.unityInstance.SendMessage('R2ProgressEvidence',m,''),method);
 const shot=async name=>page.screenshot({path:path.join(out,name+'.png'),timeout:90000});
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:480000});
 if(await page.evaluate(()=>!!window.BraxhornError))throw Error('Unity startup failed');
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:180000});
 const initial=await state();
 if(initial.build!==sha||initial.area!=='estate'||initial.cityMeshes!==553)throw Error('Wrong exact Unity build');
 if(!initial.handFound||!initial.leftFootFound||!initial.rightFootFound)throw Error('Original Brax rig incomplete');
 // Real Unity baseline first, then the same camera after. These are separate screenshots.
 await send('ReviewView','-14,.08,6,100,7,12');await page.waitForTimeout(1500);
 await compare('ProgressBefore');await page.waitForTimeout(1200);await shot('01-before');
 audit.before=await state();
 await compare('ProgressAfter');await page.waitForTimeout(1200);await shot('02-after');
 audit.after=await state();
 if(Math.hypot(audit.before.x-audit.after.x,audit.before.z-audit.after.z)>.03||Math.abs(audit.before.yaw-audit.after.yaw)>.01)throw Error('Before/after camera mismatch');
 await send('ReviewView','-17,.08,0,90,12,8');await page.waitForTimeout(1500);await shot('03-facade-detail');
 await send('ReviewView','-13,.08,1,90,32,7');await page.waitForTimeout(1500);await shot('04-approach-detail');
 await send('ReviewView','-14,.08,8,100,7,12');await page.waitForTimeout(1500);await shot('05-roof-overview');
 await send('ReviewView','14,.08,6,260,7,12');await page.waitForTimeout(1500);
 await compare('ProgressBefore');await page.waitForTimeout(1200);await shot('06-dom03-before');
 await compare('ProgressAfter');await page.waitForTimeout(1200);await shot('07-dom03-after');
 await send('ReviewView','18,.08,3,270,12,8');await page.waitForTimeout(1500);await shot('08-dom03-detail');
 await send('ReviewView','0,.08,6,90,7,12');await page.waitForTimeout(1500);await shot('09-garden-overview');
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:30000});
 // From this point only original MoveInput and LookInput. No reset, Travel*, ReviewView.
 const interval=await page.evaluate(()=>({clock:performance.now(),origin:performance.timeOrigin}));
 const began=Date.now();let previous=await state(),traveled=0,grounded=0,frames=0;
 const initialFrames=previous.frames;
 while(Date.now()-began<30000){
  const s=await state();const target={x:0,z:-8};const remain=Math.hypot(target.x-s.x,target.z-s.z);
  const dx=(target.x-s.x)/Math.max(.01,remain), dz=(target.z-s.z)/Math.max(.01,remain);
  const magnitude=remain>1?.43:0;
  await send('MoveInput',[(dx*s.rightX+dz*s.rightZ)*magnitude,(dx*s.forwardX+dz*s.forwardZ)*magnitude].join(','));
  // Continuous physical camera orbit from dom04 to dom03, independent of movement.
  await send('LookInput','-13.6,0');
  await page.waitForTimeout(500);
  const next=await state(),delta=Math.hypot(next.x-previous.x,next.z-previous.z),dt=next.simulationTime-previous.simulationTime;
  if(next.area!=='estate'||next.build!==sha||next.cityMeshes!==553||dt<0||delta>Math.max(1.3,dt*6.2+.5)||Math.abs(next.y-previous.y)>1.5)throw Error('Unity traversal discontinuity');
  traveled+=delta;frames++;if(next.grounded)grounded++;
  samples.push({wallMs:Date.now()-began,x:next.x,y:next.y,z:next.z,t:next.simulationTime,renderFrame:next.frames,cameraYaw:next.cameraYaw,animation:next.animation});previous=next;
 }
 await send('MoveInput','0,0');
 const end=await page.evaluate(()=>performance.now());
 if(traveled<3||grounded/frames<.8)throw Error('Insufficient real continuous ground movement: '+traveled);
 const errors=logs.filter(l=>/PAGEERROR|NullReferenceException|MissingReferenceException|Shader.*(error|not supported)|abort\(|memory access out of bounds/.test(l));
 if(errors.length)throw Error(errors.join('\n'));
 await page.close();const raw=path.join(out,'raw.webm');await video.saveAs(raw);await context.close();context=null;
 // Playwright video begins when its page is created; use its documented screencast
 // frame timing via wall creation time, with a generous measured pre-roll interval.
 // Video duration includes startup, so trim the LAST recorded physical-walk interval.
 const probe=spawnSync('ffprobe',['-v','error','-show_entries','format=duration','-of','json',raw],{encoding:'utf8'});
 if(probe.status!==0)throw Error(probe.stderr);
 const duration=Number(JSON.parse(probe.stdout).format.duration);
 const walkDuration=(end-interval.clock)/1000;
 const trim=Math.max(0,duration-walkDuration);
 const output=path.join(out,'r2-progress-continuous-walk.mp4');
 const enc=spawnSync('ffmpeg',['-v','error','-y','-i',raw,'-ss',String(trim),'-t',String(Math.min(walkDuration,35)),'-an','-vf','fps=20,format=yuv420p','-c:v','libx264','-preset','veryfast','-crf','21','-movflags','+faststart',output],{encoding:'utf8'});
 if(enc.status!==0)throw Error(enc.stderr);
 const info=spawnSync('ffprobe',['-v','error','-show_entries','format=duration,size','-of','json',output],{encoding:'utf8'});
 if(info.status!==0)throw Error(info.stderr);
 const f=JSON.parse(info.stdout).format;if(Number(f.duration)<20||Number(f.duration)>40)throw Error('Invalid checkpoint MP4 length');
 audit.passed=true;audit.durationSeconds=Number(f.duration);audit.groundTraveledMetres=traveled;audit.samples=samples;
 audit.originalPlayer='BraxGymPlayer';audit.meshCount=553;audit.preRollSeconds=trim;
 audit.stage='WORK IN PROGRESS — TWO HOUSES / FULL RENOVATION NOT ACCEPTED';
 audit.renderFramesDuringFilm=previous.frames-initialFrames;
 audit.measuredSoftwareRendererFps=audit.renderFramesDuringFilm/walkDuration;
 fs.rmSync(path.join(out,'raw'),{recursive:true,force:true});fs.unlinkSync(raw);
}catch(e){audit.error=String(e);process.exitCode=1;try{await page.screenshot({path:path.join(out,'FAIL.png')})}catch{};console.error(e)}
finally{
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(audit,null,2));fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
 try{await context?.close()}catch{};try{await browser?.close()}catch{};server.close();
}
