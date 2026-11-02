// Real Unity WebGL captures and control checks. Software rendering is not a phone performance test.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root=path.resolve('unity-preview'), out=path.resolve('region-shots');
const part=Number(process.env.SHOT_PART||0), results=[], log=[];
fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{
  const pathname=decodeURIComponent(new URL(req.url,'http://localhost').pathname).replace(/^\/unity-gym(?=\/|$)/,'');
  let file=path.resolve(root,'.'+(pathname||'/'));
  if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}
  if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
  if(!fs.existsSync(file)){res.writeHead(404).end();return;}
  res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
  fs.createReadStream(file).pipe(res);
});
await new Promise(resolve=>server.listen(8765,'127.0.0.1',resolve));
let browser,page;
const save=()=>fs.writeFileSync(path.join(out,`repair-results-${part}.json`),JSON.stringify({part,results,log,deviceValidation:false},null,2));
const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
const state=()=>page.evaluate(()=>window.BraxhornState);
async function settle(){
  await page.waitForFunction(()=>window.BraxhornState?.ready&&!window.BraxhornState?.loadingParts&&!window.BraxhornState?.loadingArea,null,{timeout:240000});
  const f=(await state()).frames;
  await page.waitForFunction(f=>window.BraxhornState?.frames>=f+5,f,{timeout:240000});
}
async function gameSeconds(n){
  const t=(await state()).gameTime;
  await page.waitForFunction(t=>window.BraxhornState?.gameTime>=t,t+n,{timeout:240000});
}
async function shot(name,view){
  if(view)await send('ReviewView',view);
  await settle();
  const s=await state();
  if(s.area!=='estate')throw Error('Wrong scene for '+name+': '+s.area);
  await page.screenshot({path:path.join(out,name+'.png'),timeout:120000});
  results.push({name,state:s});save();
}
async function startVideo(){
  await page.evaluate(()=>{
    const canvas=document.querySelector('canvas');
    if(!canvas?.captureStream||!window.MediaRecorder)throw Error('Canvas video capture unavailable');
    const stream=canvas.captureStream(15),chunks=[];
    const recorder=new MediaRecorder(stream,{mimeType:'video/webm;codecs=vp8',videoBitsPerSecond:2000000});
    window.repairRecording={recorder,stream,chunks};
    recorder.ondataavailable=e=>{if(e.data.size)chunks.push(e.data);};
    window.repairRecording.finished=new Promise(resolve=>recorder.onstop=resolve);
    recorder.start(1000);
    window.repairRecording.timer=setTimeout(()=>{if(recorder.state==='recording')recorder.stop();},90000);
  });
}
async function stopVideo(name){
  const data=await page.evaluate(async()=>{
    const r=window.repairRecording;if(!r)return null;
    clearTimeout(r.timer);if(r.recorder.state==='recording')r.recorder.stop();await r.finished;
    r.stream.getTracks().forEach(t=>t.stop());
    const bytes=new Uint8Array(await new Blob(r.chunks,{type:'video/webm'}).arrayBuffer());
    window.repairRecording=null;return Array.from(bytes);
  });
  if(!data?.length)throw Error('Empty actual recording');
  fs.writeFileSync(path.join(out,name+'.webm'),Buffer.from(data));
}
try{
  browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage','--disable-gpu-watchdog','--js-flags=--max-old-space-size=6144']});
  page=await (await browser.newContext({viewport:{width:1280,height:576},deviceScaleFactor:1})).newPage();
  page.on('console',m=>{if(/error|exception|BRAXHORN/i.test(m.text()))log.push(m.text().slice(0,1000));});
  page.on('pageerror',e=>log.push('PAGEERROR '+e.message));
  await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
  await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:900000});
  if(await page.evaluate(()=>!!window.BraxhornError))throw Error(await page.evaluate(()=>String(window.BraxhornError)));
  await settle();
  if(part===0){
    await shot('01-patryk-spokojny','0,.1,20,0,12,4');
    await startVideo();await send('MoveInput','0,.4');await gameSeconds(2);await send('MoveInput','0,0');
    await send('Command','punch');await gameSeconds(2);await shot('02-patryk-po-ciosie');
    await gameSeconds(8);await shot('03-patryk-powrot-do-spoczynku');
    await stopVideo('patryk-chod-cios-spoczynek');
    await shot('04-plot-przy-drodze','-27,.2,36.9,90,12,8');
    await shot('05-kosz-skala','28,.1,-83,90,18,8');
  }else if(part===1){
    await send('ReviewCar','approach');await shot('06-auto-patryka-przed-wejsciem');
    await startVideo();await send('ReviewCar','interact');
    await page.waitForFunction(()=>window.BraxhornState?.inCar&&!window.BraxhornState?.carTransition,null,{timeout:240000});
    await shot('07-patryk-w-kabinie');
    const before=await state();await send('MoveInput','0,.6');await gameSeconds(3);
    await send('MoveInput','.45,.5');await gameSeconds(2);await send('MoveInput','0,0');
    await page.waitForFunction(()=>Math.abs(window.BraxhornState?.carSpeed||0)<.1,null,{timeout:240000});
    const after=await state();
    if(Math.hypot(after.x-before.x,after.z-before.z)<1)throw Error('Player car did not move at least one unit');
    await shot('08-auto-po-skrecie');await send('ReviewCar','interact');
    await page.waitForFunction(()=>!window.BraxhornState?.inCar,null,{timeout:240000});
    await shot('09-patryk-po-wyjsciu');await stopVideo('auto-wejscie-jazda-skret-wyjscie');
  }else{
    await shot('10-rondo-auta','6,.1,33,315,12,8');
    await startVideo();await gameSeconds(6);await send('LookInput','160,0');await gameSeconds(3);
    await stopVideo('ruch-aut-i-obrot-kamery');
    await shot('11-ludzie-pierzeja','-3,.14,-183,180,10,4');
    await shot('12-trawa','-20,.1,-140,180,12,6');
  }
  if(log.some(t=>/PAGEERROR|NullReferenceException|MissingReferenceException|BRAXHORN_GLOS brak/.test(t)))throw Error('Runtime errors: see log');
  console.log('BRAXHORN_REPAIR_REVIEW PASS part='+part+' screenshots='+results.length);
}catch(e){log.push('REVIEW_FAILED '+e.stack);process.exitCode=1;}
finally{
  if(page)await stopVideo('interrupted-recording-'+part).catch(()=>{});
  save();await browser?.close();server.close();
}
