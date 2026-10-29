import {chromium} from 'playwright';
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';

// Exercise the compiled Unity player. Wall-clock timeouts bound failures; motion
// assertions wait for Unity frames or the same capped simulation time as movement.
const root=path.resolve('unity-preview'),out=path.resolve('unity-review');
fs.mkdirSync(out,{recursive:true});
const started=Date.now(),logs=[],checks=[];
const WAIT=90000,MAX_QA=Number(process.env.UNITY_QA_TIMEOUT_MS||1200000);
const limitations=[
  'Chromium WebGL2 with SwiftShader and CDP touch emulation; not a physical iPhone, Safari, or mobile GPU test.',
  'Collision checks cover one source wall, the ring apron, the squat rack, and the ground; they do not prove every mesh or animation is free of penetration.',
  'Screenshots are evidence for human visual review. Automated tests do not grade premium appearance or approve visual acceptance.',
];
const mime={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.webp':'image/webp','.png':'image/png','.css':'text/css'};
const server=http.createServer((req,res)=>{
  const pathname=decodeURIComponent(new URL(req.url,'http://local').pathname);
  let file=path.resolve(root,'.'+pathname);
  if(!file.startsWith(root+path.sep)&&file!==root){res.writeHead(403);res.end();return;}
  if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
  if(!fs.existsSync(file)){res.writeHead(404);res.end();return;}
  res.setHeader('Content-Type',mime[path.extname(file)]||'application/octet-stream');
  fs.createReadStream(file).pipe(res);
});
await new Promise(resolve=>server.listen(8765,'127.0.0.1',resolve));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});
page.on('console',m=>logs.push(m.type()+': '+m.text()));
page.on('pageerror',e=>logs.push('PAGEERROR: '+e.message));
await page.addInitScript(()=>{
  let current=null,sequence=0;
  window.BraxhornHistory=[];
  Object.defineProperty(window,'BraxhornState',{
    configurable:true,get:()=>current,set:s=>{
      current=s;
      if(s){s.qaSequence=++sequence;window.BraxhornHistory.push(s);if(window.BraxhornHistory.length>6000)window.BraxhornHistory.splice(0,1000);}
    },
  });
});
const state=()=>page.evaluate(()=>window.BraxhornState);
const history=seq=>page.evaluate(q=>window.BraxhornHistory.filter(s=>s.qaSequence>q),seq);
const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
const angle=(a,b)=>Math.abs((((a-b)%360)+540)%360-180);
const distance=(a,b)=>Math.hypot(b.x-a.x,b.z-a.z);
const dot=(a,b,axis)=>(b.x-a.x)*a[axis+'X']+(b.z-a.z)*a[axis+'Z'];
const brief=s=>({x:s.x,y:s.y,z:s.z,yaw:s.yaw,cameraYaw:s.cameraYaw,playerYaw:s.playerYaw,rightX:s.rightX,rightZ:s.rightZ,forwardX:s.forwardX,forwardZ:s.forwardZ,speed:s.speed,animation:s.animation,grounded:s.grounded,runEnabled:s.runEnabled,moveX:s.moveX,moveY:s.moveY,frames:s.frames,gameTime:s.gameTime,simulationTime:s.simulationTime});
function check(name,passed,data){checks.push({name,passed,data});console.log((passed?'PASS ':'FAIL ')+name);if(!passed)throw new Error(name+' FAILED '+JSON.stringify(data));}
async function until(name,predicate,arg,timeout=WAIT){
  try{await page.waitForFunction(predicate,arg,{timeout,polling:50});return await state();}
  catch(error){throw new Error(name+': '+error.message+'; latest='+JSON.stringify(await state().catch(()=>null)));}
}
async function frames(after,count=3){return until('Unity frames advance',(p)=>window.BraxhornState?.frames>=p.frame+p.count,{frame:after.frames,count});}
async function simulated(after,seconds){return until('Unity simulation advances',(p)=>window.BraxhornState?.simulationTime>=p.time+p.seconds,{time:after.simulationTime,seconds});}
async function axes(yaw,afterFrame){
  const a=yaw*Math.PI/180;
  return until('Rendered camera basis matches orbit yaw',p=>{
    const s=window.BraxhornState;if(!s||s.frames<p.frame+2)return false;
    return Math.abs(s.rightX-p.rx)<.015&&Math.abs(s.rightZ-p.rz)<.015&&Math.abs(s.forwardX-p.fx)<.015&&Math.abs(s.forwardZ-p.fz)<.015;
  },{frame:afterFrame,rx:-Math.cos(a),rz:Math.sin(a),fx:-Math.sin(a),fz:-Math.cos(a)});
}
async function review(x=1.5,y=.08,z=2.8,yaw=0,pitch=19,distance=5.8,ground=true){
  const a=await state();await send('ReviewView',[x,y,z,yaw,pitch,distance].join(','));
  await axes(yaw,a.frames);
  if(ground)await until('Player settles on ground',()=>window.BraxhornState?.grounded&&window.BraxhornState.speed<.05);
  return state();
}
async function startAtYaw(yaw){
  const a=await review();
  // LookInput is the real camera-control entry point, not a test-only rotation.
  await send('LookInput',(-yaw/.22)+',0');
  return axes(yaw,a.frames);
}
async function stop(key){
  if(key)await page.keyboard.up(key);
  return until('Released input stops player',()=>{const s=window.BraxhornState;return s&&Math.abs(s.moveX)+Math.abs(s.moveY)<.001&&s.speed<.05;});
}
async function hasMoved(a,min=.25){
  return until('Movement produces displacement',p=>{
    const s=window.BraxhornState;
    return s&&s.frames>p.frames&&s.simulationTime-p.time>=.16&&Math.hypot(s.x-p.x,s.z-p.z)>=p.min;
  },{x:a.x,z:a.z,time:a.simulationTime,frames:a.frames,min});
}
async function runMode(enabled){
  if((await state()).runEnabled!==enabled)await page.click('#run');
  return until('Run toggle applied',v=>window.BraxhornState?.runEnabled===v,enabled);
}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png'),timeout:90000});}
const cdp=await page.context().newCDPSession(page);
const touch=(type,touchPoints)=>cdp.send('Input.dispatchTouchEvent',{type,touchPoints});
async function waitIdle(){return until('Action returns to idle',()=>window.BraxhornState?.animation==='Idle'&&window.BraxhornState.speed<.05);}
function writeReport(passed,error){
  fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed,error,commit:process.env.GITHUB_SHA||null,durationSeconds:(Date.now()-started)/1000,platform:'Chromium WebGL2 / SwiftShader, 915x412, actual CDP touch events',visualAcceptance:'NOT AUTOMATICALLY APPROVED',limitations,checks},null,2));
}
const watchdog=setTimeout(()=>{writeReport(false,'QA exceeded '+MAX_QA+' ms');fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));process.exit(1);},MAX_QA);
try{
  const url=new URL(process.env.UNITY_GYM_URL||'http://127.0.0.1:8765/');url.searchParams.set('qa','1');
  await page.goto(url.href,{waitUntil:'domcontentloaded'});await page.click('#start');
  await until('Unity initializes',()=>window.BraxhornState?.ready||window.BraxhornError,null,240000);
  check('Unity initialized actual player',!await page.evaluate(()=>window.BraxhornError),await state());
  const manifest=JSON.parse(fs.readFileSync(path.join(root,'playable.json'),'utf8'));
  const expected=process.env.GITHUB_SHA||manifest.commit;
  const initial=await until('Ready player and original bones',()=>window.unityInstance&&window.BraxhornState?.frames>8&&window.BraxhornState.grounded);
  check('Compiled scene and runtime use the requested commit',/^[a-f0-9]{40}$/.test(expected)&&initial.build===expected&&manifest.commit===expected,{expected,runtime:initial.build,manifest:manifest.commit});
  check('Original animation clips and tracked bones exist',manifest.originalBrax===true&&manifest.originalGym===true&&initial.handFound&&initial.leftFootFound&&initial.rightFootFound,{animationNames:manifest.animationNames,hand:initial.handFound,leftFoot:initial.leftFootFound,rightFoot:initial.rightFootFound});
  await shot('01-premium-hall');

  let independentFacingCases=0;
  for(const yaw of [0,45,90,135,180,225,270,315]){
    for(const [key,axis,sign] of [['d','right',1],['a','right',-1],['w','forward',1],['s','forward',-1]]){
      const a=await startAtYaw(yaw);
      const intended=Math.atan2(sign*a[axis+'X'],sign*a[axis+'Z'])*180/Math.PI;
      if(angle(a.playerYaw,intended)>60)independentFacingCases++;
      await page.keyboard.down(key);await hasMoved(a);await stop(key);const b=await state();
      const along=sign*dot(a,b,axis),across=dot(a,b,axis==='right'?'forward':'right');
      check('Screen-relative '+key.toUpperCase()+' at orbit '+yaw+' degrees',along>.20&&Math.abs(across)<Math.max(.08,along*.12)&&angle(a.yaw,b.yaw)<.1&&angle(a.cameraYaw,b.cameraYaw)<.2,{along,across,before:brief(a),after:brief(b)});
    }
  }
  check('Movement works with player initially facing other directions',independentFacingCases>=8,{casesWithInitialFacingOver60Degrees:independentFacingCases,total:32});

  const joystick=await page.locator('#joystick').boundingBox();
  const jx=joystick.x+joystick.width/2,jy=joystick.y+joystick.height/2,jr=joystick.width*.37;
  for(const yaw of [0,135,270]){
    const a=await startAtYaw(yaw);
    await touch('touchStart',[{x:jx,y:jy,id:1}]);
    await touch('touchMove',[{x:jx+jr,y:jy,id:1}]);
    await hasMoved(a);const held=await state();await touch('touchEnd',[]);await stop();const b=await state();
    check('Full touch joystick follows screen right at '+yaw,held.moveX>.98&&Math.abs(held.moveY)<.02&&dot(a,b,'right')>.20&&Math.abs(dot(a,b,'forward'))<.10,{before:brief(a),held:brief(held),after:brief(b)});
    const stopped=await simulated(b,.30);
    check('Touch release stays stopped at '+yaw,distance(b,stopped)<.025&&angle(b.yaw,stopped.yaw)<.1,{before:brief(b),after:brief(stopped)});
  }

  // Both fingers stay active during this test. The second finger actually uses
  // #lookzone, while the first continues moving on the left joystick.
  await runMode(false);const dual=await startAtYaw(45);
  await touch('touchStart',[{x:jx,y:jy,id:1}]);
  await touch('touchMove',[{x:jx+jr,y:jy,id:1}]);
  await touch('touchStart',[{x:jx+jr,y:jy,id:1},{x:530,y:155,id:2}]);
  for(const x of [555,580,605,630])await touch('touchMove',[{x:jx+jr,y:jy,id:1},{x,y:155,id:2}]);
  await frames(dual,7);await touch('touchEnd',[]);await stop();
  const dualEnd=await state();const trace=await history(dual.qaSequence);
  const unique=[...new Map(trace.map(s=>[s.frames,s])).values()];
  const movingSegments=[];
  for(let i=1;i<unique.length;i++){
    const a=unique[i-1],b=unique[i],length=distance(a,b);
    if(length>.003&&a.moveX>.98&&Math.abs(a.moveY)<.02)movingSegments.push({along:dot(a,b,'right'),length,frame:b.frames});
  }
  check('Two simultaneous touches move and rotate independently',angle(dual.yaw,dualEnd.yaw)>10&&movingSegments.length>=3&&movingSegments.every(s=>s.along>s.length*.80),{before:brief(dual),after:brief(dualEnd),segments:movingSegments});
  fs.writeFileSync(path.join(out,'two-finger-trace.json'),JSON.stringify(trace,null,2));

  const speeds={};
  for(const enabled of [false,true]){
    await review();await runMode(enabled);await page.keyboard.down('w');
    const a=await until('Stable '+(enabled?'run':'walk')+' animation',e=>{const s=window.BraxhornState;return s&&s.animation===(e?'Run':'Walk')&&Math.abs(s.speed-(e?5:2))<.04;},enabled);
    const b=await simulated(a,.25);await stop('w');
    speeds[enabled?'run':'walk']={speed:distance(a,b)/(b.simulationTime-a.simulationTime),before:brief(a),after:brief(b)};
  }
  check('Run is faster than walk in actual movement',speeds.walk.speed>1.7&&speeds.walk.speed<2.3&&speeds.run.speed>4.5&&speeds.run.speed<5.5&&speeds.run.speed/speeds.walk.speed>2.1,speeds);

  const jumpStart=await review();await page.click('#jump');
  await until('Jump leaves the ground',q=>window.BraxhornHistory.some(s=>s.qaSequence>q.seq&&s.y>q.y+.25&&!s.grounded),{seq:jumpStart.qaSequence,y:jumpStart.y});
  await until('Jump lands on original ground',q=>{const s=window.BraxhornState;const air=window.BraxhornHistory.filter(v=>v.qaSequence>q.seq&&!v.grounded&&v.y>q.y+.25);return air.length&&s.frames>air[air.length-1].frames&&s.grounded&&Math.abs(s.y-q.y)<.12;},{seq:jumpStart.qaSequence,y:jumpStart.y});
  const jumpTrace=await history(jumpStart.qaSequence),jumpEnd=await state();
  check('Jump lifts and lands without floor penetration',Math.max(...jumpTrace.map(s=>s.y))-jumpStart.y>.30&&Math.min(...jumpTrace.map(s=>s.y))>-.08&&distance(jumpStart,jumpEnd)<.04,{before:brief(jumpStart),after:brief(jumpEnd),peak:Math.max(...jumpTrace.map(s=>s.y))});
  fs.writeFileSync(path.join(out,'jump-trace.json'),JSON.stringify(jumpTrace,null,2));await waitIdle();

  function boneDelta(a,b,kind){
    if(kind==='hand')return Math.hypot((a.handX-a.x)-(b.handX-b.x),(a.handY-a.y)-(b.handY-b.y),(a.handZ-a.z)-(b.handZ-b.z));
    const distanceTo=p=>Math.hypot((a[p].x-a.x)-(b[p].x-b.x),(a[p].y-a.y)-(b[p].y-b.y),(a[p].z-a.z)-(b[p].z-b.z));
    return Math.max(distanceTo('leftFootPosition'),distanceTo('rightFootPosition'));
  }
  for(const [button,animation,bone,screenshot] of [['#punch','Punch','hand','02-premium-punch'],['#kick','Kick','foot','03-premium-kick']]){
    const a=await review();await page.click(button);
    await until('Original '+animation+' is sampled',p=>window.BraxhornHistory.some(s=>s.qaSequence>p.seq&&s.animation===p.animation&&s.frames>p.frame),{seq:a.qaSequence,animation,frame:a.frames});
    await shot(screenshot);await waitIdle();
    const samples=(await history(a.qaSequence)).filter(s=>s.animation===animation&&s.frames>a.frames);
    const delta=samples.reduce((max,s)=>Math.max(max,boneDelta(a,s,bone)),0);
    check('Original '+animation+' moves real '+bone+' bones',samples.length>=1&&delta>.035,{boneDeltaMetres:delta,frames:samples.map(s=>s.frames),before:brief(a),after:brief(await state())});
    check(animation+' outside bag range does not score',(await state()).hits===a.hits,{before:a.hits,after:(await state()).hits});
    fs.writeFileSync(path.join(out,animation.toLowerCase()+'-trace.json'),JSON.stringify(samples,null,2));
  }

  // Original bags are at x=14 and z=-8.4,-5.4,-2.4,0.6 in Unity coordinates.
  // Approach bag 1 using real movement; no test command sets facing or a hit.
  await review(12.4,.08,-5.4,270);await runMode(false);await page.keyboard.down('w');
  await until('Approach the original training bag',()=>{const s=window.BraxhornState;return s.x>13.05&&Math.abs(s.playerYaw-90)<8;});await stop('w');
  const contactStart=await state();await page.click('#punch');
  await until('Punch contacts one bag',h=>window.BraxhornState?.hits>h,contactStart.hits);
  await until('Bag physically swings away from the punch',q=>window.BraxhornHistory.some(s=>s.qaSequence>q&&s.hits>0&&s.lastHitBag===1&&s.bagAngles[1].y>.3),contactStart.qaSequence);
  const contactSamples=await history(contactStart.qaSequence),contact=await state();
  check('Contact scores once and pushes bag away from strike',contact.hits===contactStart.hits+1&&contact.lastHitBag===1&&contact.lastHitImpulse.y>45&&Math.abs(contact.lastHitImpulse.x)<10,{before:brief(contactStart),hits:contact.hits,bag:contact.lastHitBag,impulse:contact.lastHitImpulse,maxRoll:Math.max(...contactSamples.map(s=>s.bagAngles[1].y)),distance:contact.lastHitDistance,alignment:contact.lastHitAlignment});
  await shot('08-bag-contact');await waitIdle();
  fs.writeFileSync(path.join(out,'bag-contact-trace.json'),JSON.stringify(contactSamples,null,2));
  const kickStart=await state();await page.click('#kick');await until('Kick contacts original bag',h=>window.BraxhornState?.hits>h,kickStart.hits);await waitIdle();
  check('Original kick also contacts the training bag',(await state()).hits===kickStart.hits+1,{before:kickStart.hits,after:(await state()).hits});

  // Reset in the same event-loop task as starting an attack: no phantom contact
  // may survive cancellation, even when the old hit deadline later expires.
  const cancelled=await state();
  await page.evaluate(()=>{window.BraxSend('Command','punch');window.BraxSend('Command','reset');});
  await simulated(await state(),1.3);const cancelEnd=await state();
  check('Reset cancels pending contact',cancelEnd.hits===cancelled.hits&&distance({x:1.5,z:2.8},cancelEnd)<.05,{hitsBefore:cancelled.hits,hitsAfter:cancelEnd.hits,after:brief(cancelEnd)});

  // Confirm the source bounds that BraxGymBuild mirrors in X. The assertions
  // use known walls/equipment, never guessed mesh names or undocumented positions.
  const collisionFile=path.resolve('unity-city/Assets/Source/gym-collisions.json');
  const world=JSON.parse(fs.readFileSync(collisionFile,'utf8'));
  const near=(a,b)=>Math.abs(a-b)<.001;
  const sourceCases=[
    {name:'Original side wall',find:b=>near(b.minX,18)&&near(b.maxX,21)&&near(b.top,8.5),z:-6},
    {name:'Original ring apron',find:b=>b.tag==='ring',z:-5.5},
    {name:'Original squat rack',find:b=>near(b.minX,11.3)&&near(b.maxX,12.7)&&near(b.minZ,5.8)&&near(b.maxZ,7.2),z:6.5},
  ];
  for(const c of sourceCases){
    const source=world.boxes.find(c.find);check(c.name+' collider exists in original source',!!source,source||null);
    const face=-source.minX;
    await review(face+1.5,.08,c.z,90);await runMode(true);const a=await state();await page.keyboard.down('w');
    await simulated(a,1.1);const b=await state();await simulated(b,.45);const end=await state();await stop('w');
    check(c.name+' blocks sustained movement',a.x-end.x>.65&&end.x>=face+.23&&end.x<=face+.50&&distance(b,end)<.045&&end.grounded&&Math.abs(end.y)<.12,{source,unityFaceX:face,before:brief(a),blocked:brief(b),after:brief(end)});
  }

  await review();await page.keyboard.down('d');await hasMoved(await state(),.2);await page.click('#menu');
  const paused=await until('Pause active',()=>window.BraxhornState?.paused);
  await page.keyboard.up('d');await send('MoveInput','1,0');await send('Command','punch');
  const still=await frames(paused,7);
  check('Pause blocks movement and action input',distance(paused,still)<.01&&Math.abs(still.y-paused.y)<.01&&Math.abs(still.moveX)+Math.abs(still.moveY)<.001&&still.simulationTime===paused.simulationTime&&still.animation!=='Punch',{before:brief(paused),after:brief(still)});
  await page.click('#reset');await page.click('#resume');
  const resumed=await until('Reset and resume ready',()=>{const s=window.BraxhornState;return s&&!s.paused&&s.grounded&&s.animation==='Idle';});
  const quiet=await simulated(resumed,.35);
  check('Pause reset resumes without stale input',distance({x:1.5,z:2.8},quiet)<.05&&distance(resumed,quiet)<.025&&angle(quiet.yaw,0)<.1,{before:brief(resumed),after:brief(quiet)});

  for(const [name,view] of [
    ['04-ring-details',[-9,1.05,-5.5,35,22,8]],
    ['05-bags-windows',[12.1,.08,-5.4,-90,25,6]],
    ['06-brax-front',[1.5,.08,2.8,180,10,3.7]],
    ['07-library-lounge',[13.3,.08,6.6,180,20,5]],
  ]){await review(...view);await frames(await state(),3);await shot(name);}
  const scriptErrors=logs.filter(l=>l.startsWith('PAGEERROR:'));
  const shaderErrors=logs.filter(l=>/Shader.*(not supported|unsupported|error)|Hidden\/InternalErrorShader/i.test(l));
  const runtimeErrors=logs.filter(l=>/NullReferenceException|MissingReferenceException|IndexOutOfRangeException|InvalidOperationException|abort\(|memory access out of bounds|Braxhorn state bridge/.test(l));
  check('No browser script errors',scriptErrors.length===0,scriptErrors);
  check('No shader error or fallback material messages',shaderErrors.length===0,shaderErrors);
  check('No Unity runtime exceptions',runtimeErrors.length===0,runtimeErrors);
  writeReport(true);console.log('UNITY_PREMIUM_BROWSER_TEST_PASS',checks.length);
}catch(error){
  await shot('failure').catch(()=>{});
  const last=await state().catch(()=>null);fs.writeFileSync(path.join(out,'last-state.json'),JSON.stringify(last,null,2));
  writeReport(false,String(error));console.error(error);process.exitCode=1;
}finally{
  clearTimeout(watchdog);fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));
  await browser.close();server.close();
}
