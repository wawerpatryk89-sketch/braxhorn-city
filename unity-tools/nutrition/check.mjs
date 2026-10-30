import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';

// Actual compiled Unity: position/energy fixtures are declared in the report.
// The entrance-to-counter legs themselves use normal movement and collisions.
const root=path.resolve('unity-preview'),out=path.resolve('nutrition-review');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json')));
const checks=[],errors=[],segments=[],cuts=[];
const server=http.createServer((req,res)=>{
 let p=decodeURIComponent(new URL(req.url,'http://local').pathname);
 if(p.startsWith('/unity-gym/'))p=p.slice('/unity-gym'.length);
 let file=path.resolve(root,'.'+p);
 if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 try{
  if(fs.statSync(file).isDirectory())file=path.join(file,'index.html');
  res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.png':'image/png','.webp':'image/webp','.mp3':'audio/mpeg'})[path.extname(file)]||'application/octet-stream');
  res.setHeader('Content-Length',fs.statSync(file).size);fs.createReadStream(file).pipe(res);
 }catch{res.writeHead(404);res.end();}
});
let browser,context,page,video,videoStarted,routeStart,routeEnd,phoneCounterActionTapped=false;
const check=(name,ok,data)=>{checks.push({name,passed:!!ok,data});console.log((ok?'PASS ':'FAIL ')+name);if(!ok)throw Error(name);};
const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
const player=()=>page.evaluate(()=>window.BraxhornState);
const energy=()=>page.evaluate(()=>window.BraxhornNutritionState);
const until=(fn,arg=null,timeout=90000)=>page.waitForFunction(fn,arg,{timeout,polling:150});
const shot=name=>page.screenshot({path:path.join(out,name+'.png'),timeout:60000});
async function at(id){cuts.push({type:'TravelPoint',id});await send('TravelPoint',id);await until(id=>window.BraxhornState?.activePoint===id&&window.BraxhornState.grounded,id);}
async function view(position){cuts.push({type:'ReviewView',position});await send('ReviewView',position);await until(()=>window.BraxhornState?.grounded);}
async function fixture(value){cuts.push({type:'ReviewEnergy',value});await send('ReviewEnergy',String(value));await until(v=>Math.abs(window.BraxhornNutritionState?.energy-v)<.01,value);}
async function walkTo(x,z,label,timeout=180000){
 const began=Date.now(),start=await player();let end;
 try{
  for(;;){
   end=await player();const dx=x-end.x,dz=z-end.z,dist=Math.hypot(dx,dz);
   if(dist<.28)break;
   if(Date.now()-began>timeout)throw Error('Physical walk blocked: '+label+' '+JSON.stringify(end));
   const mx=(dx*end.rightX+dz*end.rightZ)/dist;
   const my=(dx*end.forwardX+dz*end.forwardZ)/dist;
   await send('MoveInput',mx.toFixed(5)+','+my.toFixed(5));
   await page.waitForTimeout(180);
  }
 }finally{
  await send('MoveInput','0,0');
  segments.push({label,seconds:(Date.now()-began)/1000,start,end,physicalMovement:true,positionCutsWithinLeg:false});
 }
 await until(()=>window.BraxhornState.speed<.08);
}
try{
 await new Promise(r=>server.listen(8769,'127.0.0.1',r));
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:1280,height:720},hasTouch:true,isMobile:true,
  recordVideo:{dir:path.join(out,'raw-video'),size:{width:1280,height:720}}});
 videoStarted=Date.now();page=await context.newPage();video=page.video();page.on('pageerror',e=>errors.push(String(e)));
 await page.goto('http://127.0.0.1:8769/unity-gym/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
 await until(()=>window.BraxhornError||window.BraxhornState?.ready,null,600000);
 check('Unity starts without a startup error',!await page.evaluate(()=>window.BraxhornError));
 await until(()=>window.BraxhornNutritionState?.active&&window.BraxhornState.grounded);
 check('Exact source built in Unity 6',await page.evaluate(s=>window.BraxhornState.build===s,release.sourceCommit),release.sourceCommit);
 const receipt=JSON.parse(fs.readFileSync(path.join(root,'nutrition-shop.json')));
 check('Whole shop contains actual authored products and shelving',receipt.installedProducts>=130&&receipt.installedShelvingBays===8,receipt);
 check('Initial energy is bounded',(await energy()).energy>=0&&(await energy()).energy<=(await energy()).maximum,await energy());
 if(!(await player()).runEnabled)await send('Command','run');
 routeStart=Date.now();
 await view('18,0.15,23.35,90,14,5.8');await shot('00-kerb-and-frontage');
 await walkTo(26,23.35,'Actual movement along the street kerb');await shot('01-frontage-after-movement');
 await at('shop_door');await fixture(35);
 await until(()=>window.BraxhornState.context.includes('BRAXHORN NUTRITION'));
 const beforeDoor=await energy();await page.locator('#action').tap();
 await until(()=>window.BraxhornState.openDoors>0);
 check('Opening the shop door does not replenish energy',(await energy()).refills===beforeDoor.refills&&Math.abs((await energy()).energy-35)<.01,await energy());
 await walkTo(20.7,18.8,'Enter Nutrition through the actual open door');
 await shot('02-shop-entrance-and-interior');
 await walkTo(20.6,14.6,'Walk through the furnished shop to the service counter');
 await until(()=>window.BraxhornNutritionState.inRange&&window.BraxhornState.context.includes('UZUPEŁNIJ ENERGIĘ'));
 check('Actual running expenditure updates fighter energy',(await energy()).energy<35,await energy());
 await shot('03-counter-before-regeneration');
 check('Phone action is visible and enabled at the real counter',await page.locator('#action').isVisible()&&!await page.locator('#action').isDisabled()&&await page.locator('#action').innerText()==='UZUPEŁNIJ ENERGIĘ');
 const beforeRefill=await energy();await page.locator('#action').tap();phoneCounterActionTapped=true;
 await until(()=>window.BraxhornNutritionState.energy===window.BraxhornNutritionState.maximum);
 check('Counter restores the actual fighter resource',(await energy()).refills===beforeRefill.refills+1,await energy());
 await shot('04-counter-after-regeneration');
 check('Full energy is clearly shown on the phone button',await page.locator('#action').isVisible()&&await page.locator('#action').isDisabled()&&await page.locator('#action').innerText()==='ENERGIA PEŁNA');
 const full=await energy();await send('Command','action');await page.waitForTimeout(600);
 check('Repeat activation at full energy has no extra gain',(await energy()).energy===full.maximum&&(await energy()).refills===full.refills,await energy());
 routeEnd=Date.now();
 await fixture(40);await send('Command','pause');await until(()=>window.BraxhornState.paused&&window.BraxhornNutritionState.paused);
 const paused=await energy();await send('Command','action');await page.waitForTimeout(1000);
 check('Paused action cannot regenerate',(await energy()).energy===40&&(await energy()).refills===paused.refills,await energy());
 await send('Command','pause');await until(()=>!window.BraxhornState.paused&&!window.BraxhornNutritionState.paused);
 await at('shop_inside');await fixture(35);const away=await energy();
 await send('Command','action');await page.waitForTimeout(600);
 check('Action outside counter range cannot regenerate',!(await energy()).inRange&&(await energy()).energy===35&&(await energy()).refills===away.refills,await energy());
 await view('21.8,0.15,15.8,20,17,4.4');await shot('05-products-and-retail-finish');
 await at('closed_estate');await send('Command','action');await until(()=>window.BraxhornState.area==='estate',null,180000);
 check('Energy persists when entering the estate',(await energy()).energy===35,await energy());
 await view('0,0.08,92,0,19,5.8');await send('Command','action');await until(()=>window.BraxhornState.area==='city',null,180000);
 await until(()=>window.BraxhornNutritionState.area==='city');
 check('Energy persists when reloading the city',(await energy()).energy===35,await energy());
 await at('NUTRITION_COUNTER');await until(()=>window.BraxhornNutritionState.inRange);
 await send('Command','action');await until(()=>window.BraxhornNutritionState.energy===100);
 check('Recreated shop reconnects to the same energy owner',(await energy()).refills===full.refills+1,await energy());
 check('No browser exceptions',errors.length===0,errors);
}catch(e){errors.push(String(e));console.error('NUTRITION_QA_FAILED',e);process.exitCode=1;await shot('failure').catch(()=>{});}
finally{
 if(page&&!page.isClosed())await send('MoveInput','0,0').catch(()=>{});
 if(context)await context.close();if(browser)await browser.close();server.close();
 if(video){
  const raw=path.join(out,'actual-unity-diagnostics.webm');await video.saveAs(raw);
  if(routeStart){
   const args=['-hide_banner','-loglevel','error','-ss',String(Math.max(0,(routeStart-videoStarted)/1000-.5)),'-i',raw];
   if(routeEnd)args.push('-t',String((routeEnd-routeStart)/1000+1));
   args.push('-an','-c:v','libx264','-preset','fast','-crf','22','-pix_fmt','yuv420p','-movflags','+faststart',path.join(out,'actual-unity-shop-and-regeneration.mp4'));
   const result=spawnSync('ffmpeg',args,{encoding:'utf8',timeout:120000});
   if(result.status!==0){errors.push('Actual capture encoding failed: '+result.stderr);process.exitCode=1;}
  }
 }
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({sourceCommit:release.sourceCommit,checks,errors,segments,cuts,
  passed:checks.length>=14&&checks.every(c=>c.passed)&&errors.length===0,
  diagnosticPositionAndEnergyFixtures:true,continuousEntireVideo:false,actualMovementWithinNamedLegs:true,
  videoHasAudio:false,phoneCounterActionTapped,physicalPhoneValidated:false,visualAcceptance:'REQUIRES_OWNER_REVIEW',
  flickerReview:'Inspect actual moving and stationary frontage/kerb footage; no automated absence-of-flicker claim.',
  prototypeEnergyCosts:{runningPerMetre:.12,trainingHit:1.5,movementAndCombatPenaltiesAdded:false}},null,2));
}
