// Separate, actual-browser smoke for already compiled mobile city, no source rebuild.
// Only verifies ordinary gameplay movement, not QA teleport/ReviewView.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const ROOT=path.resolve('player');
const OUT=path.resolve('walking-smoke');
fs.mkdirSync(OUT,{recursive:true});
const SOURCE='45a9555f3a449d2d78165882b465c8bb82267d7f';
const release=JSON.parse(fs.readFileSync(path.join(ROOT,'release.json'),'utf8'));
const result={sourceCommit:SOURCE,sourceRun:40930737141,passed:false,
  originalBraxReady:false,movementPassed:false,loaderPassed:false,
  physicalIphoneTested:false,fullWalkPassed:false,errors:[],checks:[]};
let browser,ctx,page,server;
function assert(name,condition,evidence=null){
  result.checks.push({name,passed:Boolean(condition),evidence});
  if(!condition)throw new Error(name+': '+JSON.stringify(evidence));
}
try{
 assert('Exact already-compiled Unity SHA',release.sourceCommit===SOURCE,release.sourceCommit);
 assert('No production publication',release.productionPublication===false);
 server=http.createServer((req,res)=>{
   try {
     let p=path.resolve(ROOT,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
     if(p!==ROOT&&!p.startsWith(ROOT+path.sep)){res.writeHead(403);res.end();return;}
     if(fs.existsSync(p)&&fs.statSync(p).isDirectory())p=path.join(p,'index.html');
     if(!fs.existsSync(p)){res.writeHead(404);res.end();return;}
     let ext=path.extname(p);
     res.setHeader('Content-Type',({'.html':'text/html','.js':'text/javascript','.json':'application/json','.wasm':'application/wasm','.unityweb':'application/octet-stream','.png':'image/png'})[ext]||'application/octet-stream');
     res.setHeader('Content-Length',String(fs.statSync(p).size));res.setHeader('Cache-Control','no-store');
     fs.createReadStream(p).pipe(res);
   } catch(e){res.writeHead(500);res.end(String(e));}
 });
 await new Promise(r=>server.listen(19455,'127.0.0.1',r));
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 ctx=await browser.newContext({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});
 page=await ctx.newPage();
 page.on('pageerror',e=>result.errors.push('page: '+e.message));
 page.on('console',m=>{if(m.type()==='error')result.errors.push('console: '+m.text());});
 await page.goto('http://127.0.0.1:19455/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready===true||window.BraxhornError,{timeout:360000});
 const state=await page.evaluate(()=>({p:window.BraxhornState,error:window.BraxhornError,transport:window.BraxCityLoadingStats}));
 assert('Original Brax actually started',state.p?.ready===true && state.p?.area==='city' && !state.error,{area:state.p?.area,error:state.error});
 result.originalBraxReady=true;
 assert('Correct player binary',state.p?.build===SOURCE,{actual:state.p?.build});
 assert('Complete native city gzip stream',state.transport?.complete===true && state.transport.maxInputChunk<=65536,
    {receivedBytes:state.transport?.receivedBytes,decodedBytes:state.transport?.decodedBytes});
 result.loaderPassed=true;
 const before=state.p;
 await page.evaluate(()=>window.BraxSend('MoveInput','0,1'));
 await page.waitForFunction(t=>window.BraxhornState?.simulationTime>t+1.1,before.simulationTime,{timeout:60000});
 await page.evaluate(()=>window.BraxSend('MoveInput','0,0'));
 const after=await page.evaluate(()=>window.BraxhornState);
 const distance=Math.hypot(after.x-before.x,after.z-before.z);
 assert('Brax moves with original joystick input',distance>.8,{meters:distance,start:[before.x,before.z],end:[after.x,after.z]});
 result.movementPassed=true;
 assert('No immediate WebGL navigation reset',await page.evaluate(()=>document.visibilityState==='visible'&&!!window.unityInstance));
 assert('No headless browser exceptions',result.errors.length===0,result.errors);
 await page.screenshot({path:path.join(OUT,'actual-brax-walking.png'),timeout:30000});
 result.passed=true;
}catch(error){
 result.errors.push(String(error));
 if(page)await page.screenshot({path:path.join(OUT,'failed-browser-state.png')}).catch(()=>{});
}finally{
 fs.writeFileSync(path.join(OUT,'smoke.json'),JSON.stringify(result,null,2));
 if(ctx)await ctx.close();if(browser)await browser.close();if(server)server.close();
 if(!result.passed)process.exitCode=1;
}
