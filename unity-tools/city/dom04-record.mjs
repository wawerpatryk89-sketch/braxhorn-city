// Actual Unity6 WebGL screenshots and one continuous 24-second video. No spoofed frames.
import {chromium} from 'playwright';
import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
import {spawnSync} from 'node:child_process';
const root=path.resolve('unity-preview'),out=path.resolve('dom04-evidence');
fs.mkdirSync(out,{recursive:true});
const logs=[],samples=[],sha=process.env.GITHUB_SHA;
const report={passed:false,scene:'BraxhornEstate',scope:'DOM04_ONLY',unityCommit:sha,githubRun:process.env.GITHUB_RUN_ID,
 engine:'Unity 6 WebGL',device:'Chromium WebGL2 / SwiftShader; NOT physical iPhone',
 beforeAfterSameCamera:false,continuousVideo:false,productionPublished:false,visualAcceptance:false};
const server=http.createServer((req,res)=>{
 let f=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);
 if(f!==root&&!f.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');
 if(!fs.existsSync(f)){res.writeHead(404);res.end();return}
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json'})[path.extname(f)]||'application/octet-stream');
 fs.createReadStream(f).pipe(res);
});
await new Promise(r=>server.listen(8767,'127.0.0.1',r));
let browser,context,page,video;
try {
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:960,height:540},isMobile:true,hasTouch:true,recordVideo:{dir:path.join(out,'raw'),size:{width:960,height:540}}});
 page=await context.newPage();video=page.video();
 page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
 const send=(m,a='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[m,a]);
 const state=()=>page.evaluate(()=>window.BraxhornState);
 const switchView=m=>page.evaluate(m=>window.unityInstance.SendMessage('Dom04PremiumEvidence',m,''),m);
 const shot=async name=>page.screenshot({path:path.join(out,name+'.png'),timeout:90000});
 await page.goto('http://127.0.0.1:8767/?qa=1&dom04=1',{waitUntil:'domcontentloaded'});
 await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:480000});
 if(await page.evaluate(()=>!!window.BraxhornError))throw Error('Unity WebGL start failed');
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:180000});
 const s=await state();
 if(s.build!==sha||s.area!=='estate'||s.cityMeshes!==553||!s.handFound||!s.leftFootFound||!s.rightFootFound)throw Error('Wrong Unity6 estate/player evidence');
 await send('ReviewView','-14,.08,6,100,7,12');await page.waitForTimeout(1400);
 await switchView('Dom04Before');await page.waitForTimeout(1200);await shot('01-before');
 const before=await state();
 await switchView('Dom04After');await page.waitForTimeout(1400);await shot('02-after');
 const after=await state();
 if(Math.hypot(before.x-after.x,before.z-after.z)>.03 || Math.abs(before.yaw-after.yaw)>.01)throw Error('Before/after camera mismatch');
 report.beforeAfterSameCamera=true;
 if(Math.abs(before.x+14)>.04||Math.abs(before.z-6)>.04)throw Error('DOM04 QA placement failed; refusing street pictures');
 await send('ReviewView','-17,.08,0,90,12,8');await page.waitForTimeout(1600);await shot('03-facade');
 await send('ReviewView','-13,.08,1,90,32,7');await page.waitForTimeout(1600);await shot('04-grass-paving-fence');
 await send('ReviewView','-13,.08,0,90,9,8');await page.waitForTimeout(1600);
 const houseStart=await state();
 if(Math.abs(houseStart.x+13)>.04||Math.abs(houseStart.z)>.04)throw Error('DOM04 continuous film placement failed');
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:30000});
 // Film interval: original MoveInput + LookInput exclusively. No ReviewView, teleport, or scene jump.
 const clock=await page.evaluate(()=>performance.now());const started=Date.now();
 let previous=await state(),traveled=0,frames=0,grounded=0;
 const firstFrame=previous.frames;
 while(Date.now()-started<24000){
    const s=await state(),elapsed=(Date.now()-started)/1000;
    const target=elapsed<12?{x:-14,z:-6}:{x:-14,z:6};
    const remain=Math.hypot(target.x-s.x,target.z-s.z),dx=(target.x-s.x)/Math.max(.01,remain),dz=(target.z-s.z)/Math.max(.01,remain);
    const mag=remain>.75?.55:0;
    await send('MoveInput',[(dx*s.rightX+dz*s.rightZ)*mag,(dx*s.forwardX+dz*s.forwardZ)*mag].join(','));
    await send('LookInput','-0.35,0');
    await page.waitForTimeout(500);
    const t=await state(),dist=Math.hypot(t.x-previous.x,t.z-previous.z),dt=t.simulationTime-previous.simulationTime;
    if(t.area!=='estate'||t.build!==sha||dt<0||dist>Math.max(1.3,dt*6.2+.5)||Math.abs(t.y-previous.y)>1.5)throw Error('Noncontinuous Unity player motion');
    traveled+=dist;frames++;if(t.grounded)grounded++;
    samples.push({t:elapsed,x:t.x,y:t.y,z:t.z,frame:t.frames,cameraYaw:t.cameraYaw});previous=t;
 }
 await send('MoveInput','0,0');
 const end=await page.evaluate(()=>performance.now());
 if(traveled<.8 || grounded/frames<.8)throw Error('Physical DOM04 camera movement insufficient: '+traveled);
 const errors=logs.filter(l=>/PAGEERROR|NullReferenceException|MissingReferenceException|Shader.*(error|not supported)|abort\(|memory access out of bounds/.test(l));
 if(errors.length)throw Error(errors.join('\n'));
 await page.close();const raw=path.join(out,'recorded.webm');
 await video.saveAs(raw);await context.close();context=null;
 const probe=spawnSync('ffprobe',['-v','error','-show_entries','format=duration','-of','json',raw],{encoding:'utf8'});
 if(probe.status!==0)throw Error(probe.stderr);
 const dur=Number(JSON.parse(probe.stdout).format.duration),clip=(end-clock)/1000,trim=Math.max(0,dur-clip);
 const mp4=path.join(out,'dom04-real-unity6-24s.mp4');
 const encode=spawnSync('ffmpeg',['-v','error','-y','-i',raw,'-ss',String(trim),'-t','24','-an','-vf','fps=20,format=yuv420p','-c:v','libx264','-preset','veryfast','-crf','21','-movflags','+faststart',mp4],{encoding:'utf8'});
 if(encode.status!==0)throw Error(encode.stderr);
 const p=spawnSync('ffprobe',['-v','error','-show_entries','format=duration,size','-of','json',mp4],{encoding:'utf8'});
 if(p.status!==0)throw Error(p.stderr);
 report.durationSeconds=Number(JSON.parse(p.stdout).format.duration);
 if(report.durationSeconds<19||report.durationSeconds>29)throw Error('Incorrect final MP4 duration');
 report.traveledMetres=traveled;report.frameSamples=samples;report.continuousVideo=true;
 report.verifiedBraxPlayer='BraxGymPlayer';report.passed=true;report.visualAcceptance=false;report.mobileIphoneTested=false;
 fs.unlinkSync(raw);fs.rmSync(path.join(out,'raw'),{recursive:true,force:true});
}catch(e){report.error=String(e);process.exitCode=1;try{await page?.screenshot({path:path.join(out,'FAIL.png')})}catch{};console.error(e)}
finally {fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2));fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
 try{await context?.close()}catch{};try{await browser?.close()}catch{};server.close();}
