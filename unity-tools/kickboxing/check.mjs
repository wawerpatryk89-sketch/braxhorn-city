import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

// Real compiled WebGL only. ReviewView establishes the start of a test segment;
// all traversals, stairs, collisions, strikes and doors use the existing controls.
const root=path.resolve(process.env.UNITY_PREVIEW_DIR||'unity-preview');
const out=path.resolve(process.env.UNITY_REVIEW_DIR||'kickboxing-review');
const started=Date.now(), WAIT=45000, MAX_QA=Number(process.env.UNITY_QA_TIMEOUT_MS||870000);
const checks=[], logs=[], captures=[], segments=[], requests=[], navigations=[];
const limitations=[
  'Chromium WebGL2 with SwiftShader, a 915x412 touch viewport and genuine CDP multi-touch events. This is not a physical iPhone, Safari, or mobile GPU test.',
  'Frame times and memory below are measured in this instrumented desktop session with video recording. The physical-phone minimum 30 FPS acceptance gate remains unmeasured.',
  'Mirror render counts prove live reflection rendering with the player layer included. Screenshots/video require human inspection to judge the actual reflected character and visual quality.',
  'Runtime privacy evidence samples four training locations and three WC points with all three backroom doors open. It does not replace inspection of every possible camera position.',
  'ReviewView cuts are explicitly listed. Successful walkthrough legs use physical movement; a failed leg is not represented as completed.',
];
fs.mkdirSync(out,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.webp':'image/webp','.png':'image/png','.css':'text/css','.svg':'image/svg+xml'};
const server=http.createServer((req,res)=>{
  let pathname;
  try { pathname=decodeURIComponent(new URL(req.url,'http://local').pathname); }
  catch { res.writeHead(400);res.end();return; }
  // The preserved production loader uses <base href="/unity-gym/">.
  if(pathname==='/unity-gym')pathname='/';
  else if(pathname.startsWith('/unity-gym/'))pathname=pathname.slice('/unity-gym'.length);
  let file=path.resolve(root,'.'+pathname);
  if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
  try {
    if(fs.statSync(file).isDirectory())file=path.join(file,'index.html');
    const stat=fs.statSync(file);
    res.setHeader('Content-Type',mime[path.extname(file)]||'application/octet-stream');
    res.setHeader('Content-Length',stat.size);
    const stream=fs.createReadStream(file);
    stream.on('error',()=>res.destroy());stream.pipe(res);
  } catch { res.writeHead(404);res.end(); }
});
let browser,context,page,cdp,video,release,initial,timeoutError=null,closing=false;
let walkthroughContinuous=true,lastLegPassed=true,expectedNavigations=0,walkthroughTraceSaved=false;
const state=()=>page.evaluate(()=>window.BraxhornState);
const history=seq=>page.evaluate(q=>window.BraxhornHistory.filter(s=>s.qaSequence>q),seq);
const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
const angle=(a,b)=>Math.abs((((a-b)%360)+540)%360-180);
const distance=(a,b)=>Math.hypot(b.x-a.x,b.z-a.z);
const dot=(a,b,axis)=>(b.x-a.x)*a[axis+'X']+(b.z-a.z)*a[axis+'Z'];
const brief=s=>s&&({x:s.x,y:s.y,z:s.z,yaw:s.yaw,playerYaw:s.playerYaw,speed:s.speed,
  animation:s.animation,grounded:s.grounded,runEnabled:s.runEnabled,frames:s.frames,
  simulationTime:s.simulationTime,hits:s.hits,area:s.area,context:s.context});
const elapsed=()=>Number(((Date.now()-started)/1000).toFixed(3));
const fileKey=name=>name.toLowerCase().replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
function report(finished=false){
  const failed=checks.filter(c=>c.passed===false);
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({
    finished,passed:finished&&!timeoutError&&failed.length===0,error:timeoutError,
    commit:process.env.GITHUB_SHA||release?.sourceCommit||null,durationSeconds:elapsed(),
    platform:'Chromium WebGL2 / SwiftShader; 915x412; CDP touch; actual Unity video',
    physicalIPhoneValidated:false,physicalPhone30Fps:'NOT MEASURED',visualAcceptance:'REQUIRES HUMAN REVIEW',
    limitations,walkthroughContinuous,segments,checks,captures,navigations,
    network:{responses:requests.length,failed:requests.filter(r=>r.status>=400)},
    artifacts:{video:'walkthrough.webm',console:'browser.log',captures:'room-diagnostics.json',trace:'walkthrough-trace.json'},
  },null,2));
  fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
  fs.writeFileSync(path.join(out,'room-diagnostics.json'),JSON.stringify(captures,null,2));
}
function verify(name,passed,data){
  checks.push({name,passed:!!passed,atSeconds:elapsed(),data});
  console.log((passed?'PASS ':'FAIL ')+name);report();
  if(!passed){const e=new Error(name);e.recorded=true;throw e;}
}
async function until(name,predicate,arg,timeout=WAIT){
  try {await page.waitForFunction(predicate,arg,{timeout,polling:50});return await state();}
  catch(e){throw new Error(name+': '+e.message+'; latest='+JSON.stringify(brief(await state().catch(()=>null))));}
}
async function frames(a,count=3){return until('Unity frame progress',p=>window.BraxhornState?.frames>=p.frame+p.count,{frame:a.frames,count});}
async function simulated(a,seconds){return until('Unity simulation progress',p=>window.BraxhornState?.simulationTime>=p.time+p.seconds,{time:a.simulationTime,seconds});}
async function stop(){await send('MoveInput','0,0');return until('Input release',()=>{const s=window.BraxhornState;return s&&Math.abs(s.moveX)+Math.abs(s.moveY)<.001&&s.speed<.05;});}
async function releaseInput(){
  if(!page||page.isClosed())return;
  if(cdp)await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]}).catch(()=>{});
  for(const key of ['w','a','s','d'])await page.keyboard.up(key).catch(()=>{});
  await send('MoveInput','0,0').catch(()=>{});
}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png'),timeout:45000});}
async function step(name,body){
  const count=checks.length;
  try {await body();report();return true;}
  catch(e){
    if(!e.recorded)checks.push({name,passed:false,error:String(e),atSeconds:elapsed(),lastState:brief(await state().catch(()=>null))});
    if(checks.length===count)checks.push({name,passed:false,error:String(e),atSeconds:elapsed()});
    console.error('FAIL '+name+': '+String(e));
    await releaseInput();await shot('failure-'+fileKey(name)).catch(()=>{});report();return false;
  }
}
async function axes(yaw,frame){
  const a=yaw*Math.PI/180;
  return until('Camera basis',p=>{const s=window.BraxhornState;return s&&s.frames>=p.frame+2&&Math.abs(s.rightX-p.rx)<.02&&Math.abs(s.rightZ-p.rz)<.02&&Math.abs(s.forwardX-p.fx)<.02&&Math.abs(s.forwardZ-p.fz)<.02;},
    {frame,rx:-Math.cos(a),rz:Math.sin(a),fx:-Math.sin(a),fz:-Math.cos(a)});
}
async function review(x=1.5,y=.08,z=2.8,yaw=0,pitch=19,d=5.8,label='test segment'){
  const a=await state();
  segments.push({kind:'ReviewView start',label,atSeconds:elapsed(),position:{x,y,z},yaw,pitch,distance:d});
  await send('ReviewView',[x,y,z,yaw,pitch,d].join(','));await axes(yaw,a.frames);
  return until('Ground contact after segment start',()=>window.BraxhornState?.grounded&&window.BraxhornState.speed<.05);
}
async function look(yaw,pitch=19){
  const a=await state();
  const delta=((((yaw-a.yaw)%360)+540)%360)-180;
  await send('LookInput',[-delta/.22,(pitch-a.pitch)/.16].join(','));
  return axes(yaw,a.frames);
}
async function runMode(enabled){
  if((await state()).runEnabled!==enabled)await page.click('#run');
  return until('Run mode applied',v=>window.BraxhornState?.runEnabled===v,enabled);
}
async function waitIdle(){return until('Action ends',()=>window.BraxhornState?.animation==='Idle'&&window.BraxhornState.speed<.05);}
async function moveTo(x,z,label,tolerance=.16){
  await runMode(false);
  const first=await state(),deadline=Date.now()+WAIT;
  let a=first,anchor=first;
  while(Math.hypot(x-a.x,z-a.z)>tolerance){
    if(Date.now()>deadline)throw new Error('Physical route timeout '+label+' '+JSON.stringify(brief(a)));
    const dx=x-a.x,dz=z-a.z,d=Math.hypot(dx,dz),magnitude=Math.min(1,Math.max(.14,d*.95));
    await send('MoveInput',[magnitude*(dx*a.rightX+dz*a.rightZ)/d,magnitude*(dx*a.forwardX+dz*a.forwardZ)/d].join(','));
    a=await frames(a,2);
    if(a.simulationTime-anchor.simulationTime>1.3){
      if(distance(anchor,a)<.045)throw new Error('Collider blocks physical route '+label+' '+JSON.stringify(brief(a)));
      anchor=a;
    }
    if(a.y<-1||a.y>3)throw new Error('Unexpected vertical position on '+label+' '+a.y);
  }
  const end=await stop();
  segments.push({kind:'physical movement',label,atSeconds:elapsed(),from:brief(first),to:brief(end),target:{x,z}});
  return end;
}
async function capture(label){
  const unique=label+'-'+captures.length;
  await send('CaptureRoomDiagnostics',unique);
  await until('Read-only room diagnostics',v=>window.BraxhornRoomState?.label===v,unique,15000);
  const result=await page.evaluate(()=>({unity:window.BraxhornRoomState,
    browser:{userAgent:navigator.userAgent,jsHeap:performance.memory?{used:performance.memory.usedJSHeapSize,total:performance.memory.totalJSHeapSize,limit:performance.memory.jsHeapSizeLimit}:null,
      loading:window.BraxCityLoadingStats||null,resourceBytes:performance.getEntriesByType('resource').reduce((n,r)=>n+(r.encodedBodySize||0),0)}}));
  const metrics=await cdp.send('Performance.getMetrics').catch(()=>null);
  if(metrics)result.browser.cdp=Object.fromEntries(metrics.metrics.filter(m=>['JSHeapUsedSize','JSHeapTotalSize','Nodes','Documents'].includes(m.name)).map(m=>[m.name,m.value]));
  captures.push({atSeconds:elapsed(),label,...result});report();return result.unity;
}
async function openRoomDoor(id){
  let diag=await capture('door-before-'+id),door=diag.doors.find(d=>d.id===id);
  if(!door)throw new Error('Missing configured room door '+id);
  if(!door.open){
    await until('Approach door interaction '+id,label=>window.BraxhornState?.context.includes(label),door.label);
    await page.click('#action');await simulated(await state(),.7);
    diag=await capture('door-open-'+id);door=diag.doors.find(d=>d.id===id);
  }
  verify('Door opens through existing interaction: '+id,door.open&&door.leafLocalYaw.some(y=>Math.abs(y)>80),door);
  return door;
}
async function startLeg(label,fallback){
  if(!lastLegPassed){walkthroughContinuous=false;await review(...fallback,label+' recovery segment');}
  segments.push({kind:'walkthrough leg',label,atSeconds:elapsed()});
}
async function shutdown(){
  if(closing)return;closing=true;
  await releaseInput();
  if(page&&!page.isClosed()){
    const trace=await page.evaluate(()=>window.BraxhornHistory||[]).catch(()=>[]);
    fs.writeFileSync(path.join(out,walkthroughTraceSaved?'restart-trace.json':'walkthrough-trace.json'),JSON.stringify(trace,null,2));
  }
  if(context)await context.close().catch(()=>{});
  if(video)await video.saveAs(path.join(out,'walkthrough.webm')).catch(e=>logs.push('VIDEO_ERROR '+String(e)));
  const videoFile=path.join(out,'walkthrough.webm');
  const videoBytes=fs.existsSync(videoFile)?fs.statSync(videoFile).size:0;
  checks.push({name:'Actual recorded Unity walkthrough video',passed:!!video&&videoBytes>4096,atSeconds:elapsed(),data:{file:'walkthrough.webm',bytes:videoBytes}});
  if(browser)await browser.close().catch(()=>{});
  server.close();report(true);
}
const watchdog=setTimeout(async()=>{
  timeoutError='QA exceeded '+MAX_QA+' ms';
  checks.push({name:'Total QA budget',passed:false,error:timeoutError});report();
  // Closing the context flushes Playwright's actual WebGL recording before exit.
  const hardExit=setTimeout(()=>process.exit(1),12000);hardExit.unref();
  await shutdown();process.exit(1);
},MAX_QA);

try {
  release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
  await new Promise((resolve,reject)=>{server.once('error',reject);server.listen(8765,'127.0.0.1',resolve);});
  browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
  context=await browser.newContext({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1,
    recordVideo:{dir:path.join(out,'recordings'),size:{width:915,height:412}}});
  page=await context.newPage();video=page.video();page.setDefaultTimeout(WAIT);
  page.on('console',m=>logs.push(m.type()+': '+m.text()));
  page.on('pageerror',e=>logs.push('PAGEERROR: '+e.message));
  page.on('crash',()=>logs.push('PAGE_CRASH'));
  page.on('response',r=>requests.push({url:r.url(),status:r.status()}));
  page.on('framenavigated',f=>{if(f===page.mainFrame())navigations.push({url:f.url(),atSeconds:elapsed(),expected:expectedNavigations-->0});});
  await page.addInitScript(()=>{
    let current=null,sequence=0;window.BraxhornHistory=[];
    Object.defineProperty(window,'BraxhornState',{configurable:true,get:()=>current,set:s=>{
      current=s;if(s){s.qaSequence=++sequence;
        // Travel-point lists are immutable map metadata and do not belong in
        // thousands of per-frame evidence samples or their memory footprint.
        const sample={...s};delete sample.points;window.BraxhornHistory.push(sample);
        if(window.BraxhornHistory.length>12000)window.BraxhornHistory.splice(0,2000);}
    }});
  });
  cdp=await context.newCDPSession(page);await cdp.send('Performance.enable');
  const url=new URL(process.env.UNITY_GYM_URL||'http://127.0.0.1:8765/unity-gym/');url.searchParams.set('qa','1');
  expectedNavigations=1;await page.goto(url.href,{waitUntil:'domcontentloaded'});await page.click('#start');
  await until('Compiled Unity startup',()=>window.BraxhornState?.ready||window.BraxhornError,null,360000);
  verify('Actual compiled Unity player initializes',!await page.evaluate(()=>window.BraxhornError),await state());
  initial=await until('Original player and ground ready',()=>window.unityInstance&&window.BraxhornState?.frames>8&&window.BraxhornState.grounded);
  const expected=process.env.GITHUB_SHA||release.sourceCommit;
  verify('Exact compiled source commit',/^[a-f0-9]{40}$/.test(expected)&&initial.build===expected&&release.sourceCommit===expected,{expected,runtime:initial.build,manifest:release.sourceCommit});
  verify('Original animated Brax bones remain present',initial.handFound&&initial.leftFootFound&&initial.rightFootFound,{hand:initial.handFound,leftFoot:initial.leftFootFound,rightFoot:initial.rightFootFound});
  verify('Existing iOS streaming algorithm retained',release.loadingFix?.algorithmPreserved===true&&release.loadingFix?.nativeStreamingGzip===true,release.loadingFix);
  await step('Room diagnostic wiring',async()=>{
    const d=await capture('startup');
    verify('Room references are assigned',d.wcConfigured&&d.mirror.configured&&['gym-backrooms','gym-locker','gym-wc'].every(id=>d.doors.some(x=>x.id===id)),d);
  });
  await review(6,.08,8,0,7,1.8,'entry composition');await frames(await state(),4);await shot('01-entry-composition');

  await step('Four camera directions',async()=>{
    for(const yaw of [0,90,180,270]){
      await review();await look(yaw);await runMode(false);const a=await state();
      await send('MoveInput','1,0');await simulated(a,.35);const b=await stop();
      verify('Screen-relative right at '+yaw+' degrees',dot(a,b,'right')>.28&&Math.abs(dot(a,b,'forward'))<.10&&angle(a.yaw,b.yaw)<.1,{before:brief(a),after:brief(b),along:dot(a,b,'right')});
    }
  });
  await step('Walk and run',async()=>{
    const speeds={};
    for(const enabled of [false,true]){
      await review();await runMode(enabled);await send('MoveInput','0,1');
      const a=await until('Steady motion',v=>{const s=window.BraxhornState;return s&&s.animation===(v?'Run':'Walk')&&Math.abs(s.speed-(v?5:2))<.05;},enabled);
      const b=await simulated(a,.35);await stop();
      speeds[enabled?'run':'walk']=distance(a,b)/(b.simulationTime-a.simulationTime);
    }
    verify('Measured running and walking displacement',speeds.walk>1.7&&speeds.walk<2.3&&speeds.run>4.5&&speeds.run<5.5,speeds);
  });
  await step('Two-finger mobile control',async()=>{
    await review();await look(45);await runMode(false);const a=await state();
    const box=await page.locator('#joystick').boundingBox(),lookBox=await page.locator('#lookzone').boundingBox();
    const jx=box.x+box.width/2,jy=box.y+box.height/2,jr=box.width*.37;
    const lx=lookBox.x+lookBox.width*.23,ly=lookBox.y+90;
    const touch=(type,touchPoints)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints});
    await touch('touchStart',[{x:jx,y:jy,id:1}]);await touch('touchMove',[{x:jx+jr,y:jy,id:1}]);
    await touch('touchStart',[{x:jx+jr,y:jy,id:1},{x:lx,y:ly,id:2}]);
    for(const delta of [25,50,75,100]){
      await touch('touchMove',[{x:jx+jr,y:jy,id:1},{x:lx+delta,y:ly,id:2}]);await frames(await state(),1);
    }
    await frames(await state(),5);await touch('touchEnd',[]);const b=await stop();
    const trace=await history(a.qaSequence),unique=[...new Map(trace.map(s=>[s.frames,s])).values()],motion=[];
    for(let i=1;i<unique.length;i++){
      const p=unique[i-1],q=unique[i],d=distance(p,q);
      if(d>.003&&p.moveX>.98&&Math.abs(p.moveY)<.02)motion.push({length:d,along:dot(p,q,'right')});
    }
    verify('Two real active touches move and orbit independently',angle(a.yaw,b.yaw)>10&&motion.length>=3&&motion.every(s=>s.along>s.length*.80),{before:brief(a),after:brief(b),motion});
    fs.writeFileSync(path.join(out,'two-finger-trace.json'),JSON.stringify(trace,null,2));
    const resting=await simulated(b,.3);verify('Touch release has no residual movement',distance(b,resting)<.03,{before:brief(b),after:brief(resting)});
  });
  await step('Jump',async()=>{
    const a=await review();await page.click('#jump');
    await until('Jump lifts player',p=>window.BraxhornHistory.some(s=>s.qaSequence>p.seq&&s.y>p.y+.25&&!s.grounded),{seq:a.qaSequence,y:a.y});
    await until('Jump returns to floor',p=>{const s=window.BraxhornState;return s.grounded&&s.frames>p.frame+3&&Math.abs(s.y-p.y)<.12&&window.BraxhornHistory.some(q=>q.qaSequence>p.seq&&!q.grounded&&q.y>p.y+.25);},{seq:a.qaSequence,frame:a.frames,y:a.y});
    const trace=await history(a.qaSequence),b=await state();
    verify('Original jump lifts and lands without penetration',Math.max(...trace.map(s=>s.y))>a.y+.30&&Math.min(...trace.map(s=>s.y))>-.08&&distance(a,b)<.05,{before:brief(a),after:brief(b),peak:Math.max(...trace.map(s=>s.y))});await waitIdle();
  });
  for(const [command,animation] of [['punch','Punch'],['kick','Kick']])await step('Original '+animation,async()=>{
    const a=await review();await page.click('#'+command);
    await until(animation+' animation sample',p=>window.BraxhornHistory.some(s=>s.qaSequence>p.seq&&s.frames>p.frame&&s.animation===p.animation),{seq:a.qaSequence,frame:a.frames,animation});
    await waitIdle();const trace=(await history(a.qaSequence)).filter(s=>s.frames>a.frames&&s.animation===animation);
    const boneDelta=s=>command==='punch'?Math.hypot((s.handX-s.x)-(a.handX-a.x),(s.handY-s.y)-(a.handY-a.y),(s.handZ-s.z)-(a.handZ-a.z)):
      Math.max(...['leftFootPosition','rightFootPosition'].map(p=>Math.hypot((s[p].x-s.x)-(a[p].x-a.x),(s[p].y-s.y)-(a[p].y-a.y),(s[p].z-s.z)-(a[p].z-a.z))));
    verify('Original '+animation+' moves character bones',trace.length>0&&Math.max(...trace.map(boneDelta))>.035,{samples:trace.length,maximumBoneDelta:Math.max(0,...trace.map(boneDelta))});
    verify(animation+' out of range does not hit a bag',(await state()).hits===a.hits,{before:a.hits,after:(await state()).hits});
  });

  // The following successful legs form one continuous physical walkthrough.
  await review(6,.08,8,0,7,1.8,'continuous walkthrough start');await runMode(false);
  lastLegPassed=await step('Ring stairs traversal',async()=>{
    await look(0,19);await moveTo(-9,1.2,'hall to ring stairs');const before=await state();
    await moveTo(-9,-4.2,'climb original ring stairs');const ring=await state();
    verify('Ring reached by physical stairs without jumping',ring.y>.75&&ring.y<1.2&&ring.grounded&&ring.z<-2.6&&before.y<.2,{before:brief(before),onRing:brief(ring)});
    await shot('02-ring-from-platform');await capture('ring');
    await moveTo(-9,1.2,'descend original ring stairs');
    verify('Ring stairs return to hall floor',(await state()).grounded&&Math.abs((await state()).y)<.12,brief(await state()));
  });
  lastLegPassed=await step('Training bags traversal and contact',async()=>{
    await startLeg('ring to bags',[1.5,.08,2.8,270,19,5.8]);
    await moveTo(11.9,1.5,'foreground route to bags');await moveTo(12.1,-5.4,'bag approach aisle');await look(270,19);
    await moveTo(13,-5.4,'face and approach suspended bag');
    const a=await state();await page.click('#punch');
    await until('Punch hits bag',hits=>window.BraxhornState?.hits>hits,a.hits);
    await until('Bag swings in strike direction',seq=>window.BraxhornHistory.some(s=>s.qaSequence>seq&&s.lastHitBag===1&&s.bagAngles[1].y>.3),a.qaSequence);
    await shot('03-bag-contact');await waitIdle();const b=await state();
    verify('Directed punch swings the hanging bag',b.hits===a.hits+1&&b.lastHitBag===1&&b.lastHitImpulse.y>40&&Math.abs(b.lastHitImpulse.x)<15,{before:brief(a),after:brief(b),impulse:b.lastHitImpulse,distance:b.lastHitDistance,alignment:b.lastHitAlignment});
    await page.click('#kick');await until('Kick hits bag',hits=>window.BraxhornState?.hits>hits,b.hits);await waitIdle();
    verify('Kick also contacts the original training system',(await state()).hits===b.hits+1,{before:b.hits,after:(await state()).hits});
    const actionTrace=await history(a.qaSequence),peak=Math.max(...actionTrace.map(s=>Math.hypot(s.bagAngles[1].x,s.bagAngles[1].y)));
    const settled=await simulated(await state(),3);
    verify('Bag oscillation loses energy after contact',Math.hypot(settled.bagAngles[1].x,settled.bagAngles[1].y)<peak*.4,{peakDegrees:peak,settledDegrees:Math.hypot(settled.bagAngles[1].x,settled.bagAngles[1].y)});
    fs.writeFileSync(path.join(out,'bag-contact-trace.json'),JSON.stringify(await history(a.qaSequence),null,2));await capture('bags');
  });
  lastLegPassed=await step('Live training mirror',async()=>{
    await startLeg('bags to mirror',[11.8,.08,-3,270,19,5.8]);
    await moveTo(11.8,-3,'mirror viewing position');await look(270,12);await frames(await state(),4);
    const a=await capture('mirror-visible-before');await moveTo(12.3,-3,'movement reflected by main mirror');await frames(await state(),8);
    const b=await capture('mirror-visible-after');
    verify('Real reflection renders animated player layer',b.mirror.ready&&b.mirror.textureCreated&&b.mirror.renders>a.mirror.renders&&b.mirror.sourceIncludesPlayer&&b.mirror.sourceIncludesMirror&&b.mirror.width>=512&&b.mirror.width<=1024,b.mirror);
    await shot('04-live-mirror');await look(90,12);await frames(await state(),3);
    const hidden=await capture('mirror-out-of-view-before');await frames(await state(),10);const hiddenEnd=await capture('mirror-out-of-view-after');
    verify('Main mirror does not render while behind camera',hiddenEnd.mirror.renders-hidden.mirror.renders<=1,{before:hidden.mirror.renders,after:hiddenEnd.mirror.renders});
    await look(270,12);await frames(await state(),4);await shot('05-mirror-return');
  });
  lastLegPassed=await step('Backroom doors and physical traversal',async()=>{
    await startLeg('mirror to backrooms',[1.5,.08,2.8,90,19,5.8]);
    const d=await capture('annex-route-layout'),entry=d.doors.find(x=>x.id==='gym-backrooms'),locker=d.doors.find(x=>x.id==='gym-locker'),wc=d.doors.find(x=>x.id==='gym-wc');
    if(!entry||!locker||!wc||!d.wcConfigured)throw new Error('Backroom route configuration is incomplete');
    const corridorX=locker.position.x+.925,outerX=entry.position.x+1.6,roomX=locker.position.x-1.25;
    await moveTo(1.5,2.8,'clear training-floor crossing');await moveTo(outerX,entry.position.z,'discreet backroom entrance');await look(90,12);
    await openRoomDoor(entry.id);await shot('06-backrooms-open-from-hall');
    await moveTo(entry.position.x-1.1,entry.position.z,'through backroom door');
    await moveTo(corridorX,entry.position.z,'through corridor screen opening');
    await moveTo(corridorX,locker.position.z,'locker corridor');await openRoomDoor(locker.id);
    await moveTo(roomX,locker.position.z,'through separate locker door');
    verify('Locker room entered through its separate door',(await state()).x<locker.position.x-.65&&(await state()).grounded,brief(await state()));
    await shot('07-locker-room');await capture('locker-room');
    await moveTo(corridorX,locker.position.z,'locker exit into corridor');await moveTo(corridorX,wc.position.z,'concealed toilet approach');
    await openRoomDoor(wc.id);await moveTo(roomX,wc.position.z,'through opaque WC door');
    verify('WC entered through corridor and separate door',(await state()).x<wc.position.x-.65&&(await state()).grounded,brief(await state()));
    await look(90,15);await shot('08-toilet-interior');
    const privacy=await capture('privacy-all-room-doors-open');
    verify('No sampled training viewpoint sees WC with all doors open',privacy.privacySamplePassedWithDoorsOpen&&privacy.privacy.length>=12,{wc:privacy.wcPosition,doors:privacy.doors,rays:privacy.privacy});
    await moveTo(roomX,privacy.wcPosition.z,'approach WC collision from clear aisle');await look(90,15);
    const fixtureApproach=await state();await send('MoveInput','0,1');
    const blocked=await simulated(fixtureApproach,.8),blockedEnd=await simulated(blocked,.35);await stop();
    verify('WC fixture blocks sustained character movement',distance(blocked,blockedEnd)<.035&&blockedEnd.x>privacy.wcPosition.x+.4&&blockedEnd.x<roomX+.05&&blockedEnd.grounded,
      {wc:privacy.wcPosition,before:brief(fixtureApproach),blocked:brief(blocked),after:brief(blockedEnd)});
    await moveTo(roomX,wc.position.z,'return from fixture to WC door');
    // Verify the opaque toilet leaf closes and reopens using normal interaction.
    await until('WC closing interaction',()=>window.BraxhornState?.context.includes('TOALETA'));
    await page.click('#action');await simulated(await state(),.7);const closed=await capture('wc-door-closed');
    verify('WC door closes fully',closed.doors.some(x=>x.id==='gym-wc'&&!x.open&&x.leafLocalYaw.every(y=>Math.abs(y)<2)),closed.doors);
    await openRoomDoor(wc.id);
    await moveTo(corridorX,wc.position.z,'WC exit');
    // Opening from inside swings this existing bidirectional door into the
    // corridor. Reopen from the corridor before walking north along it.
    await until('WC interaction from corridor',()=>window.BraxhornState?.context.includes('TOALETA'));
    await page.click('#action');await simulated(await state(),.7);await openRoomDoor(wc.id);
    await moveTo(corridorX,entry.position.z,'return along concealed corridor');
    await moveTo(entry.position.x-1.1,entry.position.z,'return through screen opening');await moveTo(outerX,entry.position.z,'backroom exit into hall');
    verify('Backrooms return to main hall',(await state()).x>entry.position.x+.6&&(await state()).grounded,brief(await state()));
    await shot('09-return-to-hall');await capture('backrooms-return');
  });
  lastLegPassed=await step('Original entrance city exit and return',async()=>{
    await startLeg('hall to city',[6,.08,8,180,19,5.8]);
    await moveTo(6,9.6,'original gym exit approach');await look(180,19);
    if(!(await state()).doorOpen){await page.click('#action');await until('Original main door opens',()=>window.BraxhornState?.doorOpen);}
    await simulated(await state(),.8);await moveTo(6,13.4,'through unchanged city entrance');await moveTo(6,21.6,'out to existing street');
    const street=await state();verify('Physical exit reaches existing city',street.z>21&&street.area===initial.area&&street.grounded,{street:brief(street),cityMeshes:street.cityMeshes});
    await shot('10-existing-city-outside');await look(0,19);
    await moveTo(6,13.4,'return from street');await moveTo(6,9.6,'re-enter original gym doorway');
    verify('Physical city to gym return remains playable',(await state()).z<10&&(await state()).grounded&&Math.abs((await state()).y)<.15,brief(await state()));
    await shot('11-city-return');await capture('after-walkthrough');
  });
  if(!lastLegPassed)walkthroughContinuous=false;

  await step('Reset and restart',async()=>{
    const before=await state();await send('Command','reset');
    await until('Reset settles',()=>window.BraxhornState?.grounded&&window.BraxhornState.animation==='Idle'&&window.BraxhornState.speed<.05);
    const reset=await simulated(await state(),.35);
    verify('Reset returns to original spawn with no stale movement',distance(reset,initial)<.10&&angle(reset.yaw,0)<.1&&Math.abs(reset.moveX)+Math.abs(reset.moveY)<.001,{before:brief(before),after:brief(reset),initial:brief(initial)});
    await capture('before-explicit-reload');
    fs.writeFileSync(path.join(out,'walkthrough-trace.json'),JSON.stringify(await history(0),null,2));
    walkthroughTraceSaved=true;
    verify('No unsolicited page reload during walkthrough',navigations.filter(n=>!n.expected).length===0,navigations);
    expectedNavigations=1;await page.reload({waitUntil:'domcontentloaded'});await page.click('#start');
    await until('Explicit restart initializes',()=>window.BraxhornState?.ready||window.BraxhornError,null,180000);
    const restarted=await until('Restart player grounded',()=>window.BraxhornState?.ready&&window.BraxhornState.grounded);
    verify('Explicit reload restarts same Unity build',!await page.evaluate(()=>window.BraxhornError)&&restarted.build===release.sourceCommit&&distance(restarted,initial)<.10,{restart:brief(restarted),build:restarted.build});
    await capture('after-explicit-reload');await shot('12-restarted-player');
  });
  await step('Runtime errors and measured performance evidence',async()=>{
    const errors=logs.filter(l=>/PAGEERROR:|PAGE_CRASH|NullReferenceException|MissingReferenceException|IndexOutOfRangeException|InvalidOperationException|Shader.*(not supported|unsupported|error)|Hidden\/InternalErrorShader|abort\(|memory access out of bounds|Braxhorn room diagnostics bridge/.test(l));
    verify('No Unity runtime, shader or page exceptions',errors.length===0,errors);
    const measured=captures.filter(c=>c.unity.frameTiming.samples>5);
    verify('Local frame timing and memory measurements recorded',measured.length>=3&&measured.every(c=>c.unity.frameTiming.meanMs>0),measured.map(c=>({label:c.label,timing:c.unity.frameTiming,memory:c.unity.memory,browserHeap:c.browser.jsHeap})));
    verify('No unsolicited navigation after restart',navigations.filter(n=>!n.expected).length===0,navigations);
  });
} catch(e) {
  if(!e.recorded)checks.push({name:'Fatal initialization or runner error',passed:false,error:String(e),atSeconds:elapsed()});
  console.error(e);if(page&&!page.isClosed())await shot('failure-fatal').catch(()=>{});
} finally {
  clearTimeout(watchdog);await shutdown();
  process.exitCode=checks.some(c=>c.passed===false)||timeoutError?1:0;
  console.log(process.exitCode?'KICKBOXING_WEBGL_QA_FAILED':'KICKBOXING_WEBGL_QA_PASS',checks.length,'checks');
}
