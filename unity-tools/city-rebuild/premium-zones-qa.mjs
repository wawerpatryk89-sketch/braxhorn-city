import {chromium} from 'playwright';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const out=path.resolve('premium-zones-qa');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const checks=[],errors=[];
const server=http.createServer((req,res)=>{
 let u=new URL(req.url,'http://localhost');
 let p=path.resolve(root,'.'+decodeURIComponent(u.pathname));
 if(!p.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 if(fs.existsSync(p)&&fs.statSync(p).isDirectory())p=path.join(p,'index.html');
 if(!fs.existsSync(p)){res.writeHead(404);res.end();return;}
 const types={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.png':'image/png','.webp':'image/webp'};
 res.setHeader('Content-Type',types[path.extname(p)]||'application/octet-stream');
 fs.createReadStream(p,{highWaterMark:65536}).pipe(res);
});
await new Promise(resolve=>server.listen(8976,'127.0.0.1',resolve));
let browser,context,video;
const check=(name,ok,detail)=>{checks.push({name,passed:!!ok,detail});console.log(ok?'PASS':'FAIL',name,JSON.stringify(detail));if(!ok)throw Error(name)};
try{
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:932,height:430},isMobile:true,hasTouch:true,recordVideo:{dir:out,size:{width:932,height:430}}});
 const page=await context.newPage();
 video=page.video();
 page.on('pageerror',e=>errors.push('PAGEERROR '+e.message));
 page.on('crash',()=>errors.push('PAGE_CRASH'));
 page.on('console',m=>{if(m.type()==='error')errors.push('CONSOLE '+m.text())});
 await page.goto('http://127.0.0.1:8976/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:360000});
 check('Unity runtime ready',!!(await page.evaluate(()=>window.BraxhornState?.ready)),await page.evaluate(()=>window.BraxhornError));
 const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
 const get=()=>page.evaluate(()=>window.BraxhornState);
 const point=async(id)=>{
  await send('TravelPoint',id);
  await page.waitForFunction(id=>window.BraxhornState?.activePoint===id&&window.BraxhornState?.grounded,id,{timeout:90000});
  return get();
 };
 // Doors are required to work, not merely appear on a screenshot.
 for(const [id,label] of [['shop_door','BRAXHORN NUTRITION'],['club_door','NOVA']]){
  const before=await point(id);
  await page.waitForTimeout(600);
  const prompt=(await get()).context||'';
  check('Door prompt near '+label,prompt.includes('OTWÓRZ')&&prompt.includes(label),prompt);
  await send('Command','action');
  await page.waitForFunction(n=>window.BraxhornState?.openDoors>n,before.openDoors,{timeout:10000});
  const after=await get();
  check('Real door physically opens '+label,after.openDoors>before.openDoors,{before:before.openDoors,after:after.openDoors});
  await page.screenshot({path:path.join(out,'door-'+id+'.png')});
 }
 for(const [id,file] of [['shop_inside','01-nutrition'],['club_dance','02-nova'],['quay_entry','03-waterfront'],['pier_end','04-pier'],['R1_TOWER_LOBBY','05-tower-lobby'],['R1_TOWER_ROOF_ENTRY','06-tower-roof']]){
  const s=await point(id);await page.screenshot({path:path.join(out,file+'.png')});
  check('Accessible real world anchor '+id,s.ready&&s.grounded&&s.area==='city',{x:s.x,y:s.y,z:s.z,context:s.context});
 }
 // Tower must be a real moving cabin, not a teleport to a screenshot.
 const lower=await point('R1_ELEVATOR_0');
 await send('Command','action');
 await page.waitForFunction(()=>window.BraxhornState?.context?.includes('WJEDŹ NA DACH'),null,{timeout:15000});
 await send('Command','action');
 await page.waitForFunction(y=>window.BraxhornState?.y>y+15,lower.y,{timeout:45000});
 const upper=await get();
 check('Tower passenger rides physical elevator upward',upper.y>lower.y+15,{from:lower.y,to:upper.y});
 await page.screenshot({path:path.join(out,'tower-real-elevator-arrival.png')});
 await page.waitForFunction(()=>window.BraxhornState?.context?.includes('ZJEDŹ NA ULICĘ'),null,{timeout:15000});
 await send('Command','action');
 await page.waitForFunction(y=>window.BraxhornState?.y<y-15,upper.y,{timeout:45000});
 const lowerAgain=await get();
 check('Tower passenger returns down in same physical cabin',lowerAgain.y<upper.y-15,{from:upper.y,to:lowerAgain.y});
 // Physical metro floor is below grade, with a real platform collider.
 await send('ReviewView','77.85,-3.17,63,180,15,4.2');
 await page.waitForFunction(()=>window.BraxhornState?.grounded&&Math.abs(window.BraxhornState?.z-63)<.5,null,{timeout:90000});
 const metro=await get();await page.screenshot({path:path.join(out,'07-metro-platform.png')});
 check('Metro platform can carry Brax',metro.grounded&&metro.y< -2.9&&metro.y> -3.5,metro);
 // The G5 acceptance route must be physically traversable in both directions.
 await send('ReviewView','78.0,-3.17,46.5,180,15,4.2');
 await page.waitForFunction(()=>window.BraxhornState?.grounded&&Math.abs(window.BraxhornState?.z-46.5)<.5,null,{timeout:90000});
 try{
  await send('MoveInput','0,1');
  await page.waitForFunction(()=>window.BraxhornState?.z>60.0,null,{timeout:25000});
 }finally{await send('MoveInput','0,0')}
 const arrived=await get();
 check('Metro G5 walked from vestibule into station',arrived.z>60&&arrived.grounded,arrived);
 try{
  await send('MoveInput','0,-1');
  await page.waitForFunction(()=>window.BraxhornState?.z<48.0,null,{timeout:25000});
 }finally{await send('MoveInput','0,0')}
 const returned=await get();
 check('Metro G5 walked back from station to vestibule',returned.z<48&&returned.grounded,returned);
 await page.screenshot({path:path.join(out,'08-metro-return-route.png')});

 check('No runtime page crash',!errors.some(x=>x.includes('PAGE_CRASH')),errors.slice(-8));
}catch(e){errors.push('QA_FATAL '+String(e));process.exitCode=1}
finally{
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({build:release.sourceCommit,checks,errors,physicalIPhoneValidated:false,visualAccepted:false},null,2));
 if(context)await context.close();
 if(video){
  try{
   const raw=await video.path();
   const mp4=path.join(out,'braxhorn-real-unity-review.mp4');
   const conversion=spawnSync('ffmpeg',['-y','-loglevel','error','-i',raw,'-c:v','libx264','-preset','veryfast','-crf','25','-pix_fmt','yuv420p','-movflags','+faststart',mp4],{encoding:'utf8',timeout:120000});
   if(conversion.status!==0||!fs.existsSync(mp4)||fs.statSync(mp4).size<1024){errors.push('VIDEO_CONVERSION_FAILED '+conversion.stderr);process.exitCode=1}
   else{console.log('UNITY_REVIEW_VIDEO',mp4,fs.statSync(mp4).size);fs.unlinkSync(raw)}
  }catch(e){errors.push('VIDEO_CAPTURE_FAILED '+String(e));process.exitCode=1}
 }
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({build:release.sourceCommit,checks,errors,physicalIPhoneValidated:false,visualAccepted:false,videoAudioCaptured:false},null,2));
 if(browser)await browser.close();
 server.close();
}
