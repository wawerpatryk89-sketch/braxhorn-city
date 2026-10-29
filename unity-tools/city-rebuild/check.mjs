// Actual Unity WebGL review. Desktop software rendering is never a phone pass.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const out=path.resolve(process.env.QA_OUT||'city-rebuild-review');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const expected=process.env.EXPECTED_BUILD_SHA||release.sourceCommit;
const checks=[],logs=[],sections=[],timings={};
const viewport={width:915,height:412};
const server=http.createServer((req,res)=>{
 const url=new URL(req.url,'http://local');let local=path.resolve(root,'.'+decodeURIComponent(url.pathname));
 if(!local.startsWith(root+path.sep)&&local!==root){res.writeHead(403);res.end();return;}
 if(fs.existsSync(local)&&fs.statSync(local).isDirectory())local=path.join(local,'index.html');
 if(!fs.existsSync(local)){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.png':'image/png','.webp':'image/webp'})[path.extname(local)]||'application/octet-stream');
 res.setHeader('Content-Length',fs.statSync(local).size);res.setHeader('Cache-Control','no-store');fs.createReadStream(local).pipe(res);
});
await new Promise(resolve=>server.listen(8765,'127.0.0.1',resolve));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const context=await browser.newContext({viewport,hasTouch:true,isMobile:true,deviceScaleFactor:1,recordVideo:{dir:path.join(out,'video'),size:viewport}});
const page=await context.newPage();const recordingStart=Date.now();
page.setDefaultTimeout(90000);page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
await page.addInitScript(()=>{
 window.__cityReview={samples:[],signals:[],lifecycle:[],frames:[]};let previousFrame=0;
 for(const type of ['focus','blur','pageshow','pagehide','visibilitychange'])addEventListener(type,()=>window.__cityReview.lifecycle.push({type,at:performance.now(),visibility:document.visibilityState}));
 function frame(t){if(previousFrame&&window.BraxhornState?.ready&&window.__cityReview.frames.length<60000)window.__cityReview.frames.push(t-previousFrame);previousFrame=t;requestAnimationFrame(frame);}requestAnimationFrame(frame);
 setInterval(()=>{
  const s=window.BraxhornState,c=window.BraxhornCitySystems;if(!s?.ready)return;
  const matching=c?.area===s.area;const at=performance.now();if(window.__cityReview.samples.length<12000)window.__cityReview.samples.push({at,area:s.area,frames:s.frames,t:s.simulationTime,x:s.x,y:s.y,z:s.z,yaw:s.yaw,pitch:s.pitch,grounded:s.grounded,managedMemoryBytes:matching?(c?.managedMemoryBytes??null):null,observedFramesPerSecond:matching?(c?.observedFramesPerSecond??null):null,jsHeapBytes:performance.memory?.usedJSHeapSize??null});
  if(c?.area==='city'&&s.area==='city'&&window.__cityReview.signals.length<12000)window.__cityReview.signals.push({at,...c});
 },200);
});
const state=()=>page.evaluate(()=>window.BraxhornState);
const systems=()=>page.evaluate(()=>window.BraxhornCitySystems);
const send=(method,arg='')=>page.evaluate(([method,arg])=>window.BraxSend(method,arg),[method,arg]);
const until=async(fn,arg,timeout=180000)=>{await page.waitForFunction(fn,arg,{timeout,polling:50});return state();};
const brief=s=>s&&({area:s.area,x:s.x,y:s.y,z:s.z,yaw:s.yaw,pitch:s.pitch,frames:s.frames,simulationTime:s.simulationTime,grounded:s.grounded,doorOpen:s.doorOpen,context:s.context});
function check(name,passed,data){checks.push({name,passed,data});console.log(passed?'PASS':'FAIL',name);if(!passed)throw Error(name+' '+JSON.stringify(data));}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png'),timeout:90000});}
async function section(name,body){const record={name,startSeconds:(Date.now()-recordingStart)/1000};try{await body();record.passed=true;}catch(error){record.passed=false;record.error=String(error);checks.push({name:name+' completes',passed:false,data:String(error)});await send('MoveInput','0,0').catch(()=>{});await shot('failure-'+name).catch(()=>{});console.error(name,error);}finally{record.endSeconds=(Date.now()-recordingStart)/1000;sections.push(record);}}
async function fresh(x,y,z,yaw=0,pitch=19,distance=5.8){yaw=((yaw%360)+360)%360;const a=await state();await send('ReviewView',[x,y,z,yaw,pitch,distance].join(','));return until(p=>{const s=window.BraxhornState;return s?.area===p.area&&s.frames>=p.frame+4&&Math.abs(s.x-p.x)<.1&&Math.abs(s.z-p.z)<.1&&Math.abs(((s.yaw-p.yaw+540)%360)-180)<.2&&s.grounded;},{area:a.area,frame:a.frames,x,z,yaw});}
async function point(id){const a=await state();await send('TravelPoint',id);return until(p=>{const s=window.BraxhornState;return s?.area===p.area&&s.frames>=p.frame+4&&s.activePoint===p.id&&s.grounded;},{id,frame:a.frames,area:a.area});}
async function move(seconds,x,y){const a=await state();try{await send('MoveInput',x+','+y);await until(p=>window.BraxhornState?.area===p.area&&window.BraxhornState.simulationTime>=p.t+p.seconds,{area:a.area,t:a.simulationTime,seconds});}finally{await send('MoveInput','0,0');}await until(()=>window.BraxhornState?.speed<.04);return state();}
async function turn(yaw,pitch){yaw=((yaw%360)+360)%360;const a=await state();const delta=((a.yaw-yaw+540)%360)-180;await send('LookInput',[delta/.22,(pitch-a.pitch)/.16].join(','));return until(p=>{const s=window.BraxhornState;return s&&s.frames>p.frame&&Math.abs(((s.yaw-p.yaw+540)%360)-180)<.25&&Math.abs(s.pitch-p.pitch)<.25;},{frame:a.frames,yaw,pitch});}
async function walkToZ(target,direction,maxSimulation=15){const a=await state();try{await send('MoveInput','0,1');await until(p=>{const s=window.BraxhornState;return s?.area===p.area&&((p.direction>0?s.z>=p.target:s.z<=p.target)||s.simulationTime>=p.t+p.maxSimulation);},{area:a.area,target,direction,t:a.simulationTime,maxSimulation});}finally{await send('MoveInput','0,0');}await until(()=>window.BraxhornState?.speed<.04);const b=await state();check('Walk reaches z='+target,direction>0?b.z>=target-.1:b.z<=target+.1,{before:brief(a),after:brief(b)});return b;}
const cdp=await context.newCDPSession(page);
async function touchDrag(x,y,dx,dy){const before=await state();await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:1}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x+dx,y:y+dy,id:1}]});await until(f=>window.BraxhornState.frames>f,before.frames);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});return state();}

try{
 const start=Date.now();await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});timings.navigationToStartButtonMs=Date.now()-start;
 await page.click('#start');const clicked=Date.now();await until(()=>window.BraxhornState?.ready||window.BraxhornError,null,600000);
 check('Actual Unity player starts',!await page.evaluate(()=>window.BraxhornError),await state());await until(()=>window.unityInstance&&window.BraxhornState?.grounded);
 timings.coldStartButtonToReadyMs=Date.now()-clicked;
 const first=await state();check('Compiled player matches the exact reviewed commit',first.build===expected&&release.sourceCommit===expected,{expected,actual:first.build,release:release.sourceCommit});
 check('Original player rig control points are present',first.handFound&&first.leftFootFound&&first.rightFootFound,{hand:first.handFound,leftFoot:first.leftFootFound,rightFoot:first.rightFootFound});await shot('01-gym-baseline-preserved');
 await until(()=>window.BraxhornCitySystems?.build===window.BraxhornState.build&&window.BraxhornCitySystems.reservationCount===11);

 await section('touch-and-camera',async()=>{
  await fresh(6.75,.23,22,0,19,5.8);const a=await state();const j=await page.locator('#joystick').boundingBox(),x=j.x+j.width/2,y=j.y+j.height/2;
  await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:1}]});await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x+j.width*.37,y,id:1}]});
  await until(t=>window.BraxhornState.simulationTime>=t+.3,a.simulationTime);await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await until(()=>window.BraxhornState.speed<.04);
  const b=await state();check('Touch joystick moves screen-right',(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ>.2,{before:brief(a),after:brief(b)});timings.coldStartButtonToConfirmedControlMs=Date.now()-clicked;timings.confirmedControlIncludesQaSetup=true;
  for(const yaw of [0,90,180,270]){const a=await fresh(6.75,.23,22,yaw),b=await move(.25,1,0);check('Movement follows yaw '+yaw,(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ>.15,{before:brief(a),after:brief(b)});}
  const look=await page.locator('#lookzone').boundingBox(),lx=look.x+look.width*.62,ly=look.y+look.height*.5,dy=Math.min(160,look.height*.38);
  for(const target of [-90,90]){
   await fresh(6.75,.23,22,90,0,5.8);
   for(let n=0;n<16&&Math.abs((await state()).pitch-target)>.2;n++)await touchDrag(lx,ly,0,Math.sign(target)*dy);
   const a=await state();check('Touch camera reaches vertical pitch '+target,Math.abs(a.pitch-target)<.2,brief(a));await shot(target<0?'02-camera-vertical-up':'03-camera-vertical-down');
   const b=await move(.25,1,0);check('Right movement remains defined at vertical pitch '+target,(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ>.15&&[b.x,b.y,b.z,b.yaw,b.pitch].every(Number.isFinite),{before:brief(a),after:brief(b)});
  }
  await fresh(6.75,.23,22,90,-45,5.8);await shot('04-skyline-from-player');const skyline=await state();await move(.5,0,1);check('Walking does not take control of the camera',Math.abs((await state()).yaw-skyline.yaw)<.2,brief(await state()));
 });

 await section('public-map',async()=>{
  await page.click('#menu');await until(()=>window.BraxhornState.paused);await page.click('#mapbutton');const labels=await page.locator('#places button').allTextContents();
  check('Normal map exposes friendly destinations and hides technical anchors',labels.length>0&&!labels.some(label=>/MAIN_|SIDE_|metro_|reserve|crossing|fight\d|anchor|QA/i.test(label)),labels);
  await page.click('#mapclose');await page.click('#resume');await until(()=>!window.BraxhornState.paused);
 });

 await section('gym-door-route',async()=>{
  await fresh(6,.08,9.4,180);if(!(await state()).doorOpen)await send('Command','action');await until(()=>window.BraxhornState.doorOpen);
  const before=await state();await until(t=>window.BraxhornState.simulationTime>=t+1,before.simulationTime);const exit=await walkToZ(22.1,1,6);await shot('05-walked-out-of-gym');
  check('Gym threshold is physically traversable',exit.z>=22.0,{before:brief(before),after:brief(exit)});await turn(0,19);const returned=await walkToZ(9.5,-1,6);check('Gym can be re-entered through the same door',returned.z<10,brief(returned));await shot('06-walked-back-into-gym');
 });

 await section('eleven-reservations',async()=>{
  const telemetry=await systems(),ids=[...telemetry.reservationIds].sort();const required=[...Array.from({length:5},(_,i)=>'MAIN_0'+(i+1)),...Array.from({length:6},(_,i)=>'SIDE_0'+(i+1))].sort();
  check('Exactly five main and six side permanent IDs exist',telemetry.reservationCount===11&&new Set(ids).size===11&&JSON.stringify(ids)===JSON.stringify(required),{ids,positions:telemetry.reservationPositions});
  check('Reservations have distinct physical anchors',new Set(telemetry.reservationPositions.map(p=>[p.x,p.y,p.z].map(v=>v.toFixed(2)).join(','))).size===11,telemetry.reservationPositions);
  fs.writeFileSync(path.join(out,'reservations.json'),JSON.stringify({ids:telemetry.reservationIds,positions:telemetry.reservationPositions,source:'Actual Unity runtime telemetry',clearanceAndVisualAcceptance:'requires inspected screenshots and traversal evidence'},null,2));
 });

 await section('traffic-full-cycle',async()=>{
  await fresh(46,.23,28.5,-90,8,4.2);const start=await systems(),end=start.signalElapsed+42;const photos=new Set();
  while((await systems()).signalElapsed<end){const s=await systems();if(!photos.has(s.trafficPhase)){photos.add(s.trafficPhase);await shot('traffic-'+s.trafficPhase);}await until(p=>{const c=window.BraxhornCitySystems;return c&& (c.trafficPhase!==p.phase||c.signalElapsed>=p.end);},{phase:s.trafficPhase,end},240000);}
  const all=await page.evaluate(start=>window.__cityReview.signals.filter(s=>s.signalElapsed>=start),start.signalElapsed);const phases=new Set(all.map(s=>s.trafficPhase));
  const required=['VehiclesRedAmber','VehiclesGreen','VehiclesAmber','AllRedBeforePedestrians','PedestriansGreen','PedestrianClearance','AllRedBeforeVehicles'];
  check('An entire actual signal cycle was observed',required.every(p=>phases.has(p))&&(await systems()).completedSignalCycles>start.completedSignalCycles,{phases:[...phases],start,end:await systems()});
  check('No conflicting pedestrian and vehicle permission',all.length>30&&all.every(s=>!(s.vehicleGreen&&s.pedestrianGreen)&&(!s.pedestrianGreen||s.vehicleRed)&&(!s.vehicleGreen||s.pedestrianRed)),{samples:all.length});
  fs.writeFileSync(path.join(out,'traffic-cycle.json'),JSON.stringify(all,null,2));
 });

 await section('metro-walk-down-and-back',async()=>{
  await fresh(78.9,.23,33.5,180,19,4.2);if((await state()).runEnabled)await send('Command','run');await until(()=>!window.BraxhornState.runEnabled);await shot('metro-01-entrance');
  const start=await state(),down=await walkToZ(46.25,1,16);check('Player walks down real stairs into the vestibule',down.y< -3.0&&down.y> -3.6&&down.x>77.3&&down.x<80.5,{before:brief(start),after:brief(down)});await shot('metro-02-vestibule');
  await turn(0,19);const returned=await walkToZ(33.7,-1,16);check('Player walks back up to the original sidewalk',returned.y>.03&&returned.y<.65&&returned.grounded,brief(returned));await shot('metro-03-returned-to-street');
  if(!(await state()).runEnabled)await send('Command','run');
 });

 await section('district-visual-review',async()=>{
  const views=[['street_gym','07-street',90],['shop_inside','08-nutrition',0],['club_dance','09-nova',0],['cellar_entry','10-cellar',90],['fight1_center','11-court',180],['garage_contact','12-garages',270],['quay_entry','13-waterfront',270],['harbour_reserve','14-port',0],['pier_end','15-pier',90],['underpass_reserve','16-viaduct',90],['closed_forest','17-forest-boundary',180]];
  for(const [id,name,yaw] of views){const p=await point(id);await fresh(p.x,p.y+.04,p.z,yaw,15,4.2);check('Grounded review location '+id,(await state()).grounded&&Math.abs((await state()).y-p.y)<.3,brief(await state()));await shot(name);}
  // These are the retained original 11 m palm groups, not a replacement model.
  for(const [label,z] of [['south',43],['north',91]]){
   await fresh(74,.23,z,90,-15,7);await shot('palms-'+label+'-trunk');
   await fresh(74,.23,z,90,-40,7);await shot('palms-'+label+'-crown');
  }
 });

 await section('separate-estate-and-return',async()=>{
  await point('closed_estate');await until(()=>window.BraxhornState.context.includes('OSIEDLE'));const departure=Date.now();await send('Command','action');await until(()=>window.BraxhornState?.area==='estate'&&window.BraxhornState.ready&&window.BraxhornState.grounded,null,420000);timings.cityToEstateReadyMs=Date.now()-departure;await shot('18-estate-entry');
  check('Separate estate is still the same compiled build',(await state()).build===expected,brief(await state()));await point('park');await shot('19-estate-park');
  await point('estate_entry');await until(()=>window.BraxhornState.context.includes('WRÓĆ DO MIASTA'));const back=Date.now();await send('Command','action');await until(()=>window.BraxhornState?.area==='city'&&window.BraxhornState.ready&&window.BraxhornState.grounded&&window.BraxhornState.x< -90,null,420000);timings.estateToVisitedCityReadyMs=Date.now()-back;await shot('20-returned-city');
  check('Return unloads estate and restores the city',(await state()).area==='city'&&(await state()).build===expected,brief(await state()));await point('gym_start');await shot('21-gym-after-roundtrip');
 });

 await section('browser-background-return',async()=>{
  const before=await state();const other=await context.newPage();await other.goto('about:blank');await other.bringToFront();await new Promise(r=>setTimeout(r,1200));await page.bringToFront();await other.close();
  await until(f=>window.BraxhornState?.frames>f+4,before.frames);check('Player updates after browser tab focus returns',(await state()).ready,brief(await state()));
  const events=await page.evaluate(()=>window.__cityReview.lifecycle);checks.push({name:'Physical phone application background recovery',passed:null,data:'Not performed: headless desktop tab focus only; lifecycle events are preserved',events});
 });
 const exceptions=logs.filter(line=>/PAGEERROR|NullReferenceException|MissingReferenceException|Shader.*(not supported|error)|abort\(|memory access out of bounds/.test(line));check('No runtime or shader exceptions',exceptions.length===0,exceptions);
}catch(error){checks.push({name:'Review setup or runtime',passed:false,data:String(error)});console.error(error);await shot('fatal-review-error').catch(()=>{});}
finally{
 const evidence=await page.evaluate(()=>window.__cityReview).catch(()=>null);const finalState=await state().catch(()=>null);
 const platform=await page.evaluate(()=>{const canvas=document.querySelector('#game'),gl=canvas?.getContext('webgl2'),extension=gl?.getExtension('WEBGL_debug_renderer_info');return {userAgent:navigator.userAgent,viewport:{width:innerWidth,height:innerHeight},devicePixelRatio,canvas:canvas?{width:canvas.width,height:canvas.height}:null,webglRenderer:extension?gl.getParameter(extension.UNMASKED_RENDERER_WEBGL):null,webglVendor:extension?gl.getParameter(extension.UNMASKED_VENDOR_WEBGL):null,webglAttributes:gl?.getContextAttributes()||null};}).catch(()=>null);
 const frames=(evidence?.frames||[]).filter(Number.isFinite).sort((a,b)=>a-b);const q=p=>frames.length?frames[Math.min(frames.length-1,Math.floor(frames.length*p))]:null;
 const samples=evidence?.samples||[],memory=samples.map(s=>s.managedMemoryBytes).filter(Number.isFinite);
 const passed=checks.filter(c=>c.passed===false).length===0;
 const report={passed,sourceCommit:expected,qaCommit:process.env.GITHUB_SHA||null,buildId:release.buildId,checks,sections,timings,platform,sessionWallSeconds:(Date.now()-recordingStart)/1000,frameTimingMs:{count:frames.length,median:q(.5),p95:q(.95),p99:q(.99),max:frames.at(-1)||null,meaning:'Browser animation-frame intervals on desktop software rendering; not physical-phone FPS'},memory:{managedSamples:memory.length,managedPeakBytes:memory.length?Math.max(...memory):null,meaning:'Managed heap telemetry only, not total process/GPU/iOS memory'},finalState:brief(finalState),visualAcceptance:'pending human/image review',physicalPhoneAcceptance:'not tested',videoScope:'Actual Unity automated review. Includes clearly logged QA position changes; metro and gym route sections are continuous physical movement.'};
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2));fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));if(evidence)fs.writeFileSync(path.join(out,'runtime-trace.json'),JSON.stringify(evidence));
 await context.close();const video=page.video();if(video)await video.saveAs(path.join(out,'browser-review.webm'));await browser.close();server.close();if(!passed)process.exitCode=1;
}
