import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

// Actual compiled Unity only. TravelPoint/ReviewView are explicitly used to
// isolate audio cases; this is a diagnostic sequence, not a continuous walk.
const root=path.resolve('unity-preview'),out=path.resolve('nova-audio-review');
fs.mkdirSync(out,{recursive:true});
const checks=[],errors=[],requests=[];
const expected=JSON.parse(fs.readFileSync(path.join(root,'release.json'))).sourceCommit;
const server=http.createServer((req,res)=>{
 let pathname=decodeURIComponent(new URL(req.url,'http://local').pathname);
 if(pathname.startsWith('/unity-gym/'))pathname=pathname.slice('/unity-gym'.length);
 let file=path.resolve(root,'.'+pathname);
 if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
 try{
  if(fs.statSync(file).isDirectory())file=path.join(file,'index.html');
  res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.mp3':'audio/mpeg','.wasm':'application/wasm','.png':'image/png','.webp':'image/webp'})[path.extname(file)]||'application/octet-stream');
  res.setHeader('Content-Length',fs.statSync(file).size);fs.createReadStream(file).pipe(res);
 }catch{res.writeHead(404);res.end();}
});
let browser,context,page;
const check=(name,passed,data)=>{
 checks.push({name,passed:!!passed,data});console.log((passed?'PASS ':'FAIL ')+name);
 if(!passed)throw Error(name);
};
const send=(method,arg)=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
const state=()=>page.evaluate(()=>window.BraxhornNovaMusic);
const until=(fn,arg=null,timeout=60000)=>page.waitForFunction(fn,arg,{timeout,polling:150});
const shot=name=>page.screenshot({path:path.join(out,name+'.png'),timeout:60000});
async function at(id){
 await send('TravelPoint',id);
 await until(id=>window.BraxhornState?.activePoint===id&&window.BraxhornState.grounded,id);
}
async function rms(){
 return page.evaluate(async()=>{
  const values=[];
  for(let sample=0;sample<8;sample++){
   let sum=0,count=0;
   for(const m of window.__novaMonitors){
    const a=new Float32Array(m.analyser.fftSize);m.analyser.getFloatTimeDomainData(a);
    for(const v of a){sum+=v*v;count++;}
   }
   values.push(count?Math.sqrt(sum/count):0);
   await new Promise(r=>setTimeout(r,100));
  }
  return {maximum:Math.max(...values),samples:values,contexts:window.__novaContexts.map(c=>c.state)};
 });
}
try{
 await new Promise(r=>server.listen(8768,'127.0.0.1',r));
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:915,height:412},hasTouch:true,isMobile:true});
 page=await context.newPage();page.on('pageerror',e=>errors.push(String(e)));
 page.on('response',r=>requests.push({url:r.url(),status:r.status()}));
 await page.addInitScript(()=>{
  window.__novaContexts=[];window.__novaMonitors=[];
  for(const name of ['AudioContext','webkitAudioContext']){
   const Native=window[name];if(!Native)continue;
   function Observed(...args){const c=new Native(...args);window.__novaContexts.push(c);return c;}
   Observed.prototype=Native.prototype;Object.setPrototypeOf(Observed,Native);window[name]=Observed;
  }
  const connect=AudioNode.prototype.connect;
  AudioNode.prototype.connect=function(destination,...args){
   if(destination===this.context.destination){
    let m=window.__novaMonitors.find(m=>m.context===this.context);
    if(!m){
     m={context:this.context,analyser:this.context.createAnalyser(),capture:this.context.createMediaStreamDestination()};
     m.analyser.fftSize=2048;
     connect.call(m.analyser,destination);connect.call(m.analyser,m.capture);
     window.__novaMonitors.push(m);
    }
    connect.call(this,m.analyser,...args);return destination;
   }
   return connect.call(this,destination,...args);
  };
  addEventListener('pointerdown',()=>{for(const c of window.__novaContexts)c.resume();});
 });
 await page.goto('http://127.0.0.1:8768/unity-gym/?qa=1',{waitUntil:'domcontentloaded'});
 await page.click('#start');
 await until(()=>window.BraxhornError||window.BraxhornState?.ready,null,600000);
 check('Unity startup has no error',!await page.evaluate(()=>window.BraxhornError));
 await until(()=>window.BraxhornNovaMusic?.active&&window.BraxhornState?.grounded);
 check('Exact compiled source',await page.evaluate(s=>window.BraxhornState.build===s,expected),expected);
 check('Music is not downloaded while starting in the gym',!requests.some(r=>r.url.includes('IndustrialPulseClub.mp3')),await state());
 await page.mouse.click(890,200); // Genuine input gesture for browser audio unlock.
 await at('club_door');
 await until(()=>window.BraxhornNovaMusic?.error||window.BraxhornNovaMusic?.loaded,null,120000);
 check('Approved edited clip loads',!(await state()).error&&(await state()).loaded,await state());
 await until(()=>window.BraxhornNovaMusic.playing&&window.BraxhornNovaMusic.time>1);
 const outside=await state();
 check('Party starts automatically before entering the club',outside.targetGain>0&&outside.targetGain<.3,outside);
 const doorstepAudio=await rms();
 check('Actual music is quietly audible outside NOVA',doorstepAudio.maximum>.00001,doorstepAudio);
 await shot('00-party-audible-from-doorstep');
 await at('club_dance');
 await until(()=>window.BraxhornNovaMusic.targetGain>.99&&window.BraxhornNovaMusic.volume>.5);
 check('Club interior is louder than the doorstep',(await state()).volume>outside.volume*2.5,{outside,inside:await state()});
 check('Intro removed and full edit loaded',(await state()).duration>179&&(await state()).duration<181,await state());
 const audible=await rms();check('Actual browser audio output is audible in NOVA',audible.maximum>.0001,audible);
 await shot('01-nova-approved-music');
 // Retain four seconds of actual Unity/WebAudio output, not a track dubbed
 // over a silent game recording.
 const recording=await page.evaluate(async()=>{
  const m=window.__novaMonitors[0];const chunks=[];
  const rec=new MediaRecorder(m.capture.stream,{mimeType:'audio/webm;codecs=opus'});
  const complete=new Promise(resolve=>{rec.onstop=async()=>{
   const bytes=new Uint8Array(await new Blob(chunks).arrayBuffer());
   let s='';for(let i=0;i<bytes.length;i+=16384)s+=String.fromCharCode(...bytes.subarray(i,i+16384));
   resolve(btoa(s));
  };});
  rec.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};rec.start();
  await new Promise(r=>setTimeout(r,4000));rec.stop();return complete;
 });
 fs.writeFileSync(path.join(out,'actual-unity-nova-audio.webm'),Buffer.from(recording,'base64'));
 await send('Command','sound');await until(()=>window.BraxhornNovaMusic.muted&&!window.BraxhornNovaMusic.playing);
 const muted=await rms();check('Existing sound button mutes NOVA',muted.maximum<.00001,muted);
 await send('Command','sound');await until(()=>window.BraxhornNovaMusic.playing);
 await send('Command','pause');await until(()=>window.BraxhornNovaMusic.paused&&!window.BraxhornNovaMusic.playing);
 const before=(await state()).time;await page.waitForTimeout(1500);
 check('Pause preserves music position',Math.abs((await state()).time-before)<.1,{before,after:(await state()).time});
 await send('Command','pause');await until(()=>!window.BraxhornNovaMusic.paused&&window.BraxhornNovaMusic.playing);
 check('Music resumes audibly',(await rms()).maximum>.0001,await state());
 await at('street_gym');await until(()=>window.BraxhornNovaMusic.volume<.001&&window.BraxhornNovaMusic.playing);
 const street=await rms();check('Party continues but is inaudible far from NOVA',street.maximum<.00001,street);
 await shot('02-street-without-club-music');
 await at('shop_inside');await page.waitForTimeout(1000);
 check('Nutrition does not receive NOVA music',(await state()).targetGain===0&&(await state()).volume<.001,await state());
 await at('club_dance');await until(()=>window.BraxhornNovaMusic.playing);
 check('Re-entry resumes the same single clip',(await state()).loaded&&(await state()).time>=before,await state());
 await at('closed_estate');await send('Command','action');
 await until(()=>window.BraxhornState?.area==='estate',null,120000);
 await until(()=>!window.BraxhornNovaMusic?.active&&!window.BraxhornNovaMusic?.playing);
 check('Scene unload stops club audio',(await rms()).maximum<.00001,await state());
 check('No browser exceptions',errors.length===0,errors);
}catch(e){errors.push(String(e));console.error('NOVA_AUDIO_QA_FAILED',e);process.exitCode=1;await shot('failure').catch(()=>{});}
finally{
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({sourceCommit:expected,checks,errors,
  passed:checks.length>=13&&checks.every(c=>c.passed)&&errors.length===0,
  runtimeAudioOutputMeasured:checks.some(c=>c.name==='Actual browser audio output is audible in NOVA'&&c.passed),
  diagnosticPositionCuts:true,continuousWalk:false,physicalPhoneValidated:false,videoWithAudio:false,
  recordedAudio:fs.existsSync(path.join(out,'actual-unity-nova-audio.webm'))?'actual-unity-nova-audio.webm':null},null,2));
 if(context)await context.close();if(browser)await browser.close();server.close();
}
