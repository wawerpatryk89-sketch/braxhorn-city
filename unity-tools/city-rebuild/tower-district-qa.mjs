import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';
const root=path.resolve('unity-preview'),out=path.resolve('tower-district-qa');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const checks=[],errors=[];
const server=http.createServer((req,res)=>{
 const u=new URL(req.url,'http://localhost');
 let file=path.resolve(root,'.'+decodeURIComponent(u.pathname));
 if(file===root)file=path.join(root,'index.html');
 if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
 if(!fs.existsSync(file)){res.writeHead(404);res.end();return}
 const types={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm'};
 res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');
 fs.createReadStream(file,{highWaterMark:65536}).pipe(res);
});
await new Promise(resolve=>server.listen(8978,'127.0.0.1',resolve));
let browser,context,video,page,stage='startup';
const check=(name,passed,data)=>{checks.push({name,passed:!!passed,data});if(!passed)throw Error(name+' '+JSON.stringify(data));console.log('PASS',name)};
try{
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:932,height:430},isMobile:true,hasTouch:true,recordVideo:{dir:out,size:{width:932,height:430}}});
 page=await context.newPage();video=page.video();
 page.on('pageerror',e=>errors.push('PAGE '+e.message));page.on('crash',()=>errors.push('PAGE_CRASH'));
 await page.goto('http://127.0.0.1:8978/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:360000});
 check('Actual Unity player ready',await page.evaluate(()=>window.BraxhornState?.ready),await page.evaluate(()=>window.BraxhornError));
 const send=(m,arg)=>page.evaluate(([a,b])=>window.BraxSend(a,b),[m,arg]);
 const ids=['apothecary','corner_store','restaurant','hotel','estate_corner','gallery','apartments_1','estate_apartments_2'];
 for(let i=0;i<ids.length;i++){
  const id='R2_OFFICE_'+ids[i];
  await send('TravelPoint',id);
  await page.waitForFunction(id=>window.BraxhornState?.activePoint===id&&window.BraxhornState?.grounded,id,{timeout:45000});
  await page.waitForTimeout(550);
  const state=await page.evaluate(()=>window.BraxhornState);
  check('Real office scene anchor '+id,state.area==='city'&&state.grounded&&state.ready,{x:state.x,y:state.y,z:state.z});
  await page.screenshot({path:path.join(out,String(i+1).padStart(2,'0')+'-office-'+ids[i]+'.png')});
 }
 check('All eight distinct office foyer points visible',checks.filter(x=>x.name.startsWith('Real office scene anchor')).length===8,checks.length);

 // QA camera enters the existing cabin; this does NOT prove walkable entrance.
 // Actual ascent/descent must occur in the Unity moving passenger cabin.
 const sendReview=v=>send('ReviewView',v.map(n=>Number(n).toFixed(3)).join(','));
 const get=()=>page.evaluate(()=>window.BraxhornState);
 // Observe state transitions instead of hiding the actual Unity failure behind
 // a generic Playwright timeout. These are runtime measurements, not mock data.
 const compact=s=>({x:s?.x,y:s?.y,z:s?.z,context:s?.context,grounded:s?.grounded,
   verticalSpeed:s?.verticalSpeed,gameTime:s?.gameTime,frames:s?.frames,ready:s?.ready});
 const observe=async(label,predicate,timeout)=>{
   stage=label;
   const start=Date.now(),samples=[];
   while(Date.now()-start<timeout){
     const state=await get(),snapshot={elapsedMs:Date.now()-start,...compact(state)};
     if(samples.length===0 || snapshot.context!==samples[samples.length-1].context ||
       Math.abs((snapshot.y??0)-(samples[samples.length-1].y??0))>1.0 ||
       (Date.now()-start)-(samples[samples.length-1].elapsedMs??0)>1800)
       samples.push(snapshot);
     if(predicate(state)){console.log('LIFT_TRANSITION_PASS',label,JSON.stringify(snapshot));return state;}
     await page.waitForTimeout(350);
   }
   const safe=label.replace(/[^a-z0-9_-]/gi,'-');
   const details={sourceCommit:release.sourceCommit,stage:label,samples,lastState:compact(await get())};
   fs.writeFileSync(path.join(out,'lift-diagnostic-'+safe+'.json'),JSON.stringify(details,null,2));
   console.error('LIFT_TRANSITION_FAIL',JSON.stringify(details));
   await page.screenshot({path:path.join(out,'lift-failure-'+safe+'.png')}).catch(()=>{});
   throw Error('Actual Unity lift transition failed: '+label);
 };
 for(const id of ids){
  const lowerId='R1_TOWER_'+id+'_LOWER_LIFT',roofId='R1_TOWER_'+id+'_ROOF';
  const points=(await get()).points||[];
  const lower=points.find(p=>p.id===lowerId),roof=points.find(p=>p.id===roofId);
  check('Source Unity lift points '+id,!!lower&&!!roof,{lowerId,roofId});
  const dir=Math.sign(roof.position.z-lower.position.z);
  check('Valid separate physical shaft '+id,Math.abs(roof.position.z-lower.position.z)>.5&&Math.abs(dir)===1,{lower:lower.position,roof:roof.position});
  const shaftZ=lower.position.z+dir*3.15;
  await sendReview([lower.position.x,lower.position.y+.12,shaftZ,180,15,4.2]);
  await observe('closed cabin '+id,s=>s?.context?.includes('OTWÓRZ DRZWI WINDY'),30000);
  const start=await get();
  console.log('LIFT_ACTION_START',id,JSON.stringify(compact(start)));
  await send('Command','action');
  await observe('opened cabin '+id,s=>s?.context?.includes('WJEDŹ NA DACH'),15000);
  await send('Command','action');
  await page.waitForFunction(y=>window.BraxhornState?.y>y+15,start.y,{timeout:60000});
  await page.waitForFunction(()=>window.BraxhornState?.context?.includes('ZJEDŹ NA ULICĘ'),null,{timeout:120000});
  const top=await get();
  check('Actual moving cabin carried passenger UP '+id,top.y>start.y+15&&Math.abs(top.y-roof.position.y)<2.5,{startY:start.y,topY:top.y,roofY:roof.position.y});
  await page.screenshot({path:path.join(out,'lift-up-'+id+'.png')});
  await send('Command','action');
  await page.waitForFunction(y=>window.BraxhornState?.y<y-15,top.y,{timeout:60000});
  await page.waitForFunction(()=>window.BraxhornState?.context?.includes('WJEDŹ NA DACH'),null,{timeout:120000});
  const end=await get();
  check('Actual moving cabin carried passenger DOWN '+id,Math.abs(end.y-start.y)<2.5,{startY:start.y,endY:end.y});
 }
 check('All eight separate physical lift round trips verified',
  checks.filter(c=>c.name.startsWith('Actual moving cabin carried passenger DOWN ')&&c.passed).length===8,checks.length);
 check('No Unity browser crash',!errors.some(x=>x.includes('PAGE_CRASH')),errors);
}catch(e){errors.push('QA stage '+stage+': '+String(e));console.error('REAL_UNITY_QA_FAILED',stage,String(e));process.exitCode=1}
finally{
 if(context)await context.close();
 if(video)try{
  const raw=await video.path();
  const dest=path.join(out,'braxhorn-eight-real-offices.mp4');
  const x=spawnSync('ffmpeg',['-y','-loglevel','error','-i',raw,'-c:v','libx264','-preset','veryfast','-crf','25','-pix_fmt','yuv420p','-movflags','+faststart',dest],{encoding:'utf8',timeout:120000});
  if(x.status!==0||!fs.existsSync(dest)){errors.push('MP4 conversion failed '+x.stderr);process.exitCode=1}
  else fs.unlinkSync(raw);
 }catch(e){errors.push('Video failure '+String(e));process.exitCode=1}
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({sourceCommit:release.sourceCommit,checks,errors,physicalPhoneValidated:false,visualAccepted:false,walkableEntrancesVerified:false,videoAudioCaptured:false},null,2));
 if(browser)await browser.close();
 server.close();
}
