// Actual Unity WebGL review. Desktop software rendering is never a phone pass.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const out=path.resolve(process.env.QA_OUT||'city-lift-review');
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
const browser=await chromium.launch({executablePath:process.env.CHROMIUM_EXECUTABLE||undefined,args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const context=await browser.newContext({viewport,hasTouch:true,isMobile:true,deviceScaleFactor:1});
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

try {
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});
 await page.click('#start');
 await until(()=>window.BraxhornState?.ready||window.BraxhornError,null,600000);
 check('Unity starts at the pinned city commit',(await state())?.build===expected,brief(await state()));
 await shot('00-start');
 const initial=await state();
 fs.writeFileSync(path.join(out,'initial-state.json'),JSON.stringify(initial,null,2));
 // Only the initial placement on the tower sidewalk is a QA teleport.
 // Every subsequent metre through the doors and up/down the shaft is physical.
 await point('R1_TOWER_STREET_ENTRY');
 await turn(180,12); await shot('00-tower-entrance');
 if((await state()).runEnabled)await send('Command','run');
 check('Street entrance offers door interaction',(await state()).context.includes('OTWÓRZ DRZWI'));
 await page.locator('#action').click();
 let doorStart=await state();await until(t=>window.BraxhornState.simulationTime>=t+1,doorStart.simulationTime);
 await walkToZ(40.1,1,8); await shot('01-lobby');
 await walkToZ(49.4,1,15); await shot('02-lower-call');
 await page.locator('#action').click();
 let a=await state();await until(t=>window.BraxhornState.simulationTime>=t+2,a.simulationTime);
 await walkToZ(53,1,8); await shot('03-inside-car');
 const low=await state();
 check('Cabin offers a real upward ride',low.context.includes('WJEDŹ'),brief(low));
 await page.locator('#action').click();
 const top=initial.points.find(p=>p.id==='R1_ELEVATOR_TOP').position.y;
 if(process.env.TEST_LIFT_PAUSE==='1') {
  await until(y=>window.BraxhornState.y>y+2,low.y,120000);
  await send('Command','pause');await until(()=>window.BraxhornState.paused);
  const paused=await state();await page.waitForTimeout(1500);
  check('Lift holds passenger position while game is paused',Math.abs((await state()).y-paused.y)<.08,{before:brief(paused),after:brief(await state())});
  await send('Command','pause');await until(()=>!window.BraxhornState.paused);
 }
 await until(y=>window.BraxhornState.y>=y-.15,top,360000);
 await until(()=>window.BraxhornState.context.includes('ZJEDŹ NA ULICĘ'));
 await shot('04-top');
 check('Passenger reaches roof',Math.abs((await state()).y-top)<.2,brief(await state()));
 await turn(0,12);await walkToZ(44,-1,8);await shot('04-roof-duel-space');
 check('Player walks out onto roof deck',Math.abs((await state()).y-top)<.2,brief(await state()));
 await turn(180,12);await walkToZ(53,1,8);
 await until(()=>window.BraxhornState.context.includes('ZJEDŹ NA ULICĘ'));
 await page.locator('#action').click();
 await until(y=>window.BraxhornState.y<=y+.15,low.y,360000);
 a=await state();await until(t=>window.BraxhornState.simulationTime>=t+2,a.simulationTime);
 await turn(0,12);await walkToZ(49.4,-1,8);await shot('05-return');
 check('Passenger exits onto original lobby',Math.abs((await state()).y-low.y)<.2,brief(await state()));
 await walkToZ(34.4,-1,12);await shot('06-return-to-street');
 check('Player returns through the original street entrance',(await state()).z<34.5,brief(await state()));
 check('No emergency recovery or lost-floor event',!logs.some(l=>/CITY_LIFT_(SAFETY_STOP|RECOVERED_AT_DEPARTURE)/.test(l)));
} catch(error) {
 checks.push({name:'lift route',passed:false,error:String(error)});
 console.error(String(error));await shot('failure').catch(()=>{});
} finally {
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({sourceCommit:expected,passed:checks.length>0&&checks.every(c=>c.passed),fixture:'QA placement on tower sidewalk only; physical entrance, lift, roof walk and return after setup',checks,last:await state().catch(()=>null)},null,2));
 fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
 await context.close();await browser.close();server.close();
 process.exitCode=checks.every(c=>c.passed)?0:1;
}
