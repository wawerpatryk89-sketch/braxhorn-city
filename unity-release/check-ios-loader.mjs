import {chromium,webkit} from 'playwright';
import fs from 'node:fs';import http from 'node:http';import path from 'node:path';
const root=path.resolve(process.env.PLAYER_ROOT||'deployed/public/unity-gym'),out=path.resolve(process.env.QA_OUT||'ios-review');fs.mkdirSync(out,{recursive:true});
const checks=[],logs=[];const source='19c71c167d4426523a251b94c3aeabbe9664aad5';
const report={passed:false,sourceCommit:source,qaCommit:process.env.GITHUB_SHA,checks,physicalIPhoneValidated:false,platforms:['Linux WebKit: native gzip transport only','Linux Chromium WebGL2: actual Unity startup and connected-scene gameplay']};
function check(name,ok,data){checks.push({name,passed:!!ok,data});console.log(ok?'PASS':'FAIL',name);if(!ok)throw Error(name+': '+JSON.stringify(data));}
const server=http.createServer((req,res)=>{
 const u=new URL(req.url,'http://local');
 if(u.pathname==='/transport-check'){res.setHeader('Content-Type','text/html');res.end('<!doctype html><base href="/unity-gym/"><script src="city-transport.js?v=ios-stream-01"></script><h1>Transport check</h1>');return;}
 const relative=u.pathname.replace(/^\/unity-gym\//,'');let f=path.resolve(root,relative||'index.html');
 if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403);res.end();return;}
 if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');
 if(!fs.existsSync(f)){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.png':'image/png','.webp':'image/webp'})[path.extname(f)]||'application/octet-stream');
 res.setHeader('Content-Length',String(fs.statSync(f).size));fs.createReadStream(f,{highWaterMark:65536}).pipe(res);
});
await new Promise(r=>server.listen(8765,'127.0.0.1',r));
let browser,page;const timeout=setTimeout(()=>{report.error='QA time limit reached';fs.writeFileSync(path.join(out,'ios-loader-report.json'),JSON.stringify(report,null,2));process.exit(1)},900000);
try{
 if(!process.env.PLAYER_URL){
  browser=await webkit.launch({headless:true});page=await browser.newPage();
  await page.goto('http://127.0.0.1:8765/transport-check');
  const stream=await page.evaluate(async()=>{
   const r=await window.BraxCityFetch('Build/19c71c167d44_WebGL.data.unityweb');const reader=r.body.getReader();let count=0;const headerBytes=[];
   // UnityWebData1.0 is 15 characters; its required terminating NUL is byte 16.
   while(true){const value=await reader.read();if(value.done)break;if(headerBytes.length<16)headerBytes.push(...value.value.subarray(0,16-headerBytes.length));count+=value.value.byteLength;}
   return {count,headerBytes,length:r.headers.get('content-length'),stats:window.BraxCityLoadingStats};
  });
  const expectedHeader=Array.from(Buffer.from('UnityWebData1.0\0'));
  check('WebKit native gzip stream completes at exact original length',stream.count===358936591&&stream.length==='358936591'&&JSON.stringify(stream.headerBytes)===JSON.stringify(expectedHeader)&&stream.stats.complete&&stream.stats.maxInputChunk<=65536,stream);
  // A missing segment must fail clearly, not silently return incomplete data.
  await page.route('**/*.part000',route=>route.fulfill({status:503,body:'unavailable'}));
  const failure=await page.evaluate(async()=>{try{const r=await window.BraxCityFetch('Build/19c71c167d44_WebGL.data.unityweb');const q=r.body.getReader();while(!(await q.read()).done){}return {failed:false}}catch(e){return {failed:true,message:String(e)}}});
  check('WebKit reports segment download failure',failure.failed&&failure.message.includes('503'),failure);
  await browser.close();browser=null;
 }
 browser=await chromium.launch({headless:true,args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage','--enable-precise-memory-info']});
 page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});
 page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR: '+e.message));
 const state=()=>page.evaluate(()=>window.BraxhornState);
 const send=(m,a='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[m,a]);
 const wait=async(f,a,t=120000)=>{await page.waitForFunction(f,a,{timeout:t,polling:100});return state();};
 const shot=n=>page.screenshot({path:path.join(out,n+'.png'),timeout:90000});
 const url=new URL(process.env.PLAYER_URL||'http://127.0.0.1:8765/unity-gym/');url.searchParams.set('qa','1');url.searchParams.set('v','ios-stream-01');
 const started=Date.now();await page.goto(url.href,{waitUntil:'domcontentloaded'});await page.click('#start');
 await wait(()=>window.unityInstance&&window.BraxhornState?.ready||window.BraxhornError,null,240000);
 check('Unity loads without error',!await page.evaluate(()=>window.BraxhornError),await state());
 const status=await wait(()=>window.BraxhornState?.grounded),stats=await page.evaluate(()=>window.BraxCityLoadingStats);
 check('Original city and Brax build are unchanged',status.build===source&&status.area==='city'&&status.cityMeshes===724&&status.handFound&&status.leftFootFound&&status.rightFootFound,{build:status.build,area:status.area,meshes:status.cityMeshes});
 check('Unity used bounded native streaming, not the large JS inflater',stats?.complete&&stats.mode==='native-gzip-stream'&&stats.decodedBytes===358936591&&stats.maxInputChunk<=65536&&stats.fullCompressedBufferAllocated===false,stats);
 report.startupSeconds=(Date.now()-started)/1000;report.metrics=await page.evaluate(()=>window.unityInstance.GetMetricsInfo());await shot('01-native-stream-gym');
 await send('ReviewView','6,.08,9.3,180,19,5.8');await wait(()=>window.BraxhornState?.grounded&&window.BraxhornState.context.includes('DRZWI'));
 await send('Command','action');await wait(()=>window.BraxhornState?.doorOpen);
 let before=await state();await send('MoveInput','0,1');
 await wait(()=>window.BraxhornState?.z>21.5);await send('MoveInput','0,0');await wait(()=>window.BraxhornState?.speed<.05);
 check('Brax walks through gym door to original street',(await state()).z>21.5,{before:before.z,after:(await state()).z});await shot('02-native-stream-street');
 await send('MoveInput','0,-1');await wait(()=>window.BraxhornState?.z<9.5);await send('MoveInput','0,0');await wait(()=>window.BraxhornState?.speed<.05);
 check('Brax returns from street into gym',(await state()).z<9.5,await state());await shot('03-native-stream-gym-return');
 await send('TravelPoint','closed_estate');await wait(()=>window.BraxhornState?.grounded&&window.BraxhornState.context.includes('OSIEDLE'));await send('Command','action');
 await wait(()=>window.BraxhornState?.area==='estate'&&window.BraxhornState.ready&&window.BraxhornState.grounded,null,240000);
 check('Original estate still loads', (await state()).cityMeshes===553,{build:(await state()).build,area:(await state()).area,meshes:(await state()).cityMeshes});await shot('04-native-stream-estate');
 await send('Command','action');await wait(()=>window.BraxhornState?.area==='city'&&window.BraxhornState.ready&&window.BraxhornState.grounded,null,240000);
 // Existing reset command returns to the original gym spawn without inventing a navigation point.
 await send('Command','reset');await wait(()=>window.BraxhornState?.grounded&&Math.abs(window.BraxhornState.x-1.5)<.1&&Math.abs(window.BraxhornState.z-2.8)<.1);
 check('Gym remains playable after city-estate round trip',(await state()).area==='city'&&await page.evaluate(()=>!!window.unityInstance),await state());await shot('05-final-connected-gym');
 const errors=logs.filter(x=>/PAGEERROR|NullReferenceException|MissingReferenceException|abort\(|memory access out of bounds|Shader.*(not supported|error)/i.test(x));
 check('No runtime exceptions',errors.length===0,errors);
 report.passed=true;
}catch(error){report.error=String(error);if(page)await page.screenshot({path:path.join(out,'ios-loader-failure.png'),timeout:10000}).catch(()=>{});console.error(error);process.exitCode=1;}
finally{clearTimeout(timeout);fs.writeFileSync(path.join(out,'ios-loader-report.json'),JSON.stringify(report,null,2));fs.writeFileSync(path.join(out,'ios-browser.log'),logs.join('\n'));if(browser)await browser.close();server.close();}
