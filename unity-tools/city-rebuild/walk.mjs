// A continuous actual-Unity circuit from the normal spawn. No debug relocation.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const out=path.resolve(process.env.WALK_OUT||'city-walk-review');
const routePath=path.resolve(process.env.WALK_ROUTE||path.join(path.dirname(fileURLToPath(import.meta.url)),'walk-route.json'));
const routeBytes=fs.readFileSync(routePath),route=JSON.parse(routeBytes);
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const expected=process.env.EXPECTED_BUILD_SHA||release.sourceCommit;
const passes=Number(process.env.WALK_PASSES||1);
if(!Number.isInteger(passes)||passes<1||passes>2)throw Error('WALK_PASSES must be one or two complete circuits');
if(route.schema!==1||!Array.isArray(route.steps)||route.steps.length<2)throw Error('Invalid continuous route');
if(new Set(route.steps.map(s=>s.id)).size!==route.steps.length)throw Error('Route checkpoint IDs must be unique');
for(const s of route.steps){
 if(!/^[a-z0-9-]+$/.test(s.id)||Boolean(s.at)===Boolean(s.transition))throw Error('Invalid checkpoint '+s.id);
 if(s.at&&(!Array.isArray(s.at)||s.at.length!==3||!s.at.every(Number.isFinite)))throw Error('Invalid walk coordinates '+s.id);
}
fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{
 const u=new URL(req.url,'http://localhost');let f=path.resolve(root,'.'+decodeURIComponent(u.pathname));
 if(f!==root&&!f.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');
 if(!fs.existsSync(f)){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.png':'image/png','.webp':'image/webp','.wasm':'application/wasm'})[path.extname(f)]||'application/octet-stream');
 res.setHeader('Content-Length',fs.statSync(f).size);res.setHeader('Cache-Control','no-store');fs.createReadStream(f).pipe(res);
});
await new Promise(resolve=>server.listen(8766,'127.0.0.1',resolve));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const viewport={width:915,height:412};
const context=await browser.newContext({viewport,hasTouch:true,isMobile:true,deviceScaleFactor:1,recordVideo:{dir:path.join(out,'video'),size:viewport}});
const page=await context.newPage();const video=page.video();const started=Date.now();
const checkpoints=[],commands=[],logs=[],timings={};let activeCheckpoint=null,failure=null,completePasses=0;
page.setDefaultTimeout(90000);page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
await page.addInitScript(()=>{
 window.__walkEvidence={trace:[],frames:[],previous:0};
 const tick=t=>{const e=window.__walkEvidence;if(e.previous&&window.BraxhornState?.ready&&e.frames.length<200000)e.frames.push(t-e.previous);e.previous=t;requestAnimationFrame(tick);};requestAnimationFrame(tick);
 setInterval(()=>{const s=window.BraxhornState,c=window.BraxhornCitySystems,e=window.__walkEvidence;if(!s?.ready||e.trace.length>=60000)return;
  e.trace.push({at:performance.now(),area:s.area,x:s.x,y:s.y,z:s.z,yaw:s.yaw,pitch:s.pitch,frames:s.frames,t:s.simulationTime,speed:s.speed,grounded:s.grounded,managedMemoryBytes:c?.area===s.area?c.managedMemoryBytes:null,phase:c?.area===s.area?c.trafficPhase:null});
 },200);
});
const state=()=>page.evaluate(()=>window.BraxhornState);
const brief=s=>s&&({area:s.area,x:s.x,y:s.y,z:s.z,yaw:s.yaw,pitch:s.pitch,frames:s.frames,t:s.simulationTime,grounded:s.grounded,speed:s.speed,context:s.context});
const elapsed=()=>Math.round((Date.now()-started)/10)/100;
const ensure=(yes,message)=>{if(!yes)throw Error(message);};
const wait=async(fn,arg=null,timeout=180000)=>{await page.waitForFunction(fn,arg,{timeout,polling:100});return state();};
async function send(method,arg){
 ensure(['MoveInput','LookInput','Command'].includes(method),'Forbidden non-player command '+method);
 if(method==='Command')ensure(['action','run'].includes(arg),'Forbidden game command '+arg);
 commands.push({at:elapsed(),method,arg,checkpoint:activeCheckpoint});
 await page.evaluate(([method,arg])=>window.BraxSend(method,arg),[method,arg]);
}
const stop=()=>send('MoveInput','0,0');
const screenshot=async name=>page.screenshot({path:path.join(out,name+'.png'),timeout:90000});
async function advance(seconds){const a=await state();return wait(p=>{const s=window.BraxhornState;return s?.area===p.area&&s.simulationTime>=p.t+p.seconds;},{area:a.area,t:a.simulationTime,seconds});}
async function look(yaw,pitch=16){
 yaw=((yaw%360)+360)%360;
 for(let i=0;i<12;i++){
  const s=await state(),delta=((s.yaw-yaw+540)%360)-180;
  if(Math.abs(delta)<.25&&Math.abs(s.pitch-pitch)<.25)return;
  const limited=Math.max(-25,Math.min(25,delta));
  await send('LookInput',[limited/.22,(pitch-s.pitch)/.16].join(','));
  await wait(f=>window.BraxhornState?.frames>f,s.frames);
 }
 throw Error('Player camera did not accept a normal look input');
}
async function walkTo(step){
 const [x,y,z]=step.at,tolerance=step.tolerance??.35;const initial=await state();
 ensure(initial.area==='city','Walking checkpoint expected the city');
 const distance=s=>Math.hypot(x-s.x,z-s.z);
 const maximum=step.maxSimulationSeconds??Math.max(15,distance(initial)*1.2+12);
 let progressAt=initial.simulationTime,best=distance(initial),last=initial;
 try{
  while(distance(last)>tolerance){
   const current=await state();
   ensure(current.area===initial.area&&current.build===expected,'Unexpected scene/build while walking');
   ensure([current.x,current.y,current.z].every(Number.isFinite)&&current.y>-12,'Invalid position or fall below the scene');
   ensure(current.simulationTime-initial.simulationTime<=maximum,'Walk timeout at '+step.id);
   const d=distance(current);
   if(d<best-.12){best=d;progressAt=current.simulationTime;}
   ensure(current.simulationTime-progressAt<7,'Walking is obstructed before '+step.id);
   const desired=((Math.atan2(-(x-current.x),-(z-current.z))*180/Math.PI)%360+360)%360;
   await look(desired,step.walkPitch??16);
   await send('MoveInput','0,'+Math.max(.18,Math.min(1,d/1.2)).toFixed(3));
   last=await advance(.12);
   const moved=Math.hypot(last.x-current.x,last.z-current.z),dt=last.simulationTime-current.simulationTime;
   ensure(dt>=0&&moved<=6*dt+1,'Unexpected position discontinuity while walking; no relocation is accepted');
  }
 }finally{await stop();}
 await wait(()=>window.BraxhornState?.speed<.04&&window.BraxhornState.grounded);
 const landed=await state();ensure(Math.abs(landed.y-y)<(step.heightTolerance??.45),'Wrong floor at '+step.id+': '+landed.y+' expected '+y);
 ensure(distance(landed)<tolerance+.3,'Stopped outside checkpoint '+step.id);
 return landed;
}

try{
 await page.goto('http://127.0.0.1:8766/?walk=1',{waitUntil:'domcontentloaded'});await page.click('#start');const clicked=Date.now();
 await wait(()=>window.BraxhornError||window.BraxhornState?.ready,null,600000);
 ensure(!await page.evaluate(()=>window.BraxhornError),'Unity startup error');
 await wait(()=>window.unityInstance&&window.BraxhornState?.grounded);
 const first=await state();ensure(first.build===expected&&first.area==='city','Wrong player or initial scene');
 timings.coldStartButtonToReadyMs=Date.now()-clicked;await screenshot('00-natural-gym-spawn');
 if(first.runEnabled)await send('Command','run');await wait(()=>!window.BraxhornState.runEnabled);
 for(let pass=1;pass<=passes;pass++){
  for(const step of route.steps){
   activeCheckpoint=step.id;const record={id:step.id,pass,startSeconds:elapsed(),before:brief(await state())};checkpoints.push(record);
   console.log('WALK_BEGIN',pass,step.id,JSON.stringify(record.before));
   if(step.transition){
    await stop();await wait(t=>window.BraxhornState?.context.includes(t),step.context);
    await send('Command','action');await wait(area=>window.BraxhornState?.area===area&&window.BraxhornState.ready&&window.BraxhornState.grounded,step.transition,420000);
    ensure((await state()).build===expected,'Scene transition changed compiled build');
    if((await state()).runEnabled)await send('Command','run');await advance(2);
   }else await walkTo(step);
   if(step.openGymDoor){
    if(!(await state()).doorOpen)await send('Command','action');
    await wait(()=>window.BraxhornState.doorOpen);await advance(1);
   }
   if(step.openNearbyDoor&&(await state()).context?.includes('OTWÓRZ DRZWI')){
    await send('Command','action');await advance(1);
   }
   if(step.waitForPedestrians){
    await stop();await wait(()=>{const c=window.BraxhornCitySystems;return c?.trafficPhase==='PedestriansGreen'&&c.secondsRemaining>=7.3;},null,360000);
   }
   if(step.look){await look(...step.look);await advance(.5);}
   if(step.shot)await screenshot('p'+pass+'-'+step.id);
   if(step.holdSeconds)await advance(step.holdSeconds);
   record.after=brief(await state());record.endSeconds=elapsed();record.passed=true;
   console.log('WALK_PASS',pass,step.id,JSON.stringify(record.after));
  }
  completePasses++;
 }
 const errors=logs.filter(x=>/PAGEERROR|NullReferenceException|MissingReferenceException|abort\(|memory access out of bounds/.test(x));
 ensure(errors.length===0,'Runtime errors: '+JSON.stringify(errors));
}catch(error){failure=String(error);console.error('WALK_FAILED',activeCheckpoint,failure);await stop().catch(()=>{});await screenshot('walk-failure').catch(()=>{});}
finally{
 const evidence=await page.evaluate(()=>window.__walkEvidence).catch(()=>null),last=await state().catch(()=>null);
 const platform=await page.evaluate(()=>({userAgent:navigator.userAgent,viewport:{width:innerWidth,height:innerHeight},devicePixelRatio})).catch(()=>null);
 const frameIntervals=(evidence?.frames||[]).filter(Number.isFinite).sort((a,b)=>a-b),q=p=>frameIntervals.length?frameIntervals[Math.min(frameIntervals.length-1,Math.floor(frameIntervals.length*p))]:null;
 const passed=!failure&&completePasses===passes;
 const report={passed,failure,sourceCommit:expected,qaCommit:process.env.GITHUB_SHA||null,buildId:release.buildId,
  routeSha256:crypto.createHash('sha256').update(routeBytes).digest('hex'),requestedPasses:passes,completePasses,
  cameraAndMovement:'Only normal MoveInput, LookInput, run toggle and door/scene interaction. No ReviewView, TravelPoint, reset, or position writes.',
  initialPosition:'Normal compiled gym spawn',checkpoints,remainingCurrentPass:completePasses===passes?[]:route.steps.filter(s=>!checkpoints.some(c=>c.pass===completePasses+1&&c.id===s.id&&c.passed)).map(s=>s.id),
  areasWalked:[...new Set(route.steps.filter(s=>s.areaLabel&&checkpoints.some(c=>c.id===s.id&&c.passed)).map(s=>s.areaLabel))],
  timings,wallSeconds:elapsed(),platform,finalState:brief(last),physicalPhoneAcceptance:'not tested',visualAcceptance:'pending actual video and image inspection',
  frameTimingMs:{samples:frameIntervals.length,median:q(.5),p95:q(.95),p99:q(.99),meaning:'Headless desktop SwiftShader browser intervals, not phone FPS'},
  videoScope:'One actual Unity session from normal spawn, including normal scene loads. Stops at first blocked waypoint; never skips or teleports to manufacture completion.'};
 fs.writeFileSync(path.join(out,'walk-report.json'),JSON.stringify(report,null,2));fs.writeFileSync(path.join(out,'walk-commands.json'),JSON.stringify(commands));
 if(evidence)fs.writeFileSync(path.join(out,'walk-trace.json'),JSON.stringify(evidence));fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
 await context.close();if(video)await video.saveAs(path.join(out,'continuous-walk.webm'));await browser.close();server.close();if(!passed)process.exitCode=1;
}
