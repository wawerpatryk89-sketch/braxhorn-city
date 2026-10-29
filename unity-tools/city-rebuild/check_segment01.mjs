// Actual Unity 6 real-render captures: street eye height + birds-eye aerial.
// Source code is never represented as screenshots; no mockups or image AI.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const out=path.resolve(process.env.QA_OUT||'city-segment-actual-unity');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const segment=JSON.parse(fs.readFileSync(path.join(root,'city-segment-review.json'),'utf8'));
const city=JSON.parse(fs.readFileSync(path.join(root,'city-migration.json'),'utf8'));
const sha=process.env.EXPECTED_BUILD_SHA||release.sourceCommit;
let server,ctx,browser,page;
let captures=[],checks=[],errors=[],loadingMs=0;
function check(name,ok,data=null){checks.push({name,passed:!!ok,data});if(!ok)throw Error(name+" "+JSON.stringify(data));}
const state=()=>page.evaluate(()=>window.BraxhornState);
async function send(method,arg){await page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);}
async function shot(name,details){
 const p=path.join(out,name+'.png');
 await page.screenshot({path:p,timeout:90000});
 captures.push({name:name+'.png',details,source:"actual compiled Unity 6 canvas (headless desktop)",camera:"QA only; no character teleport"});
}
async function view(name,xyz,yaw,pitch=14,distance=5.8){
 await send('ReviewView',[...xyz,yaw,pitch,distance].join(','));
 await page.waitForFunction(v=>{
    const s=window.BraxhornState;return s&&Math.abs(s.x-v[0])<.15&&Math.abs(s.z-v[2])<.15&&Math.abs(((s.yaw-v[3]+540)%360)-180)<.35;
 },[...xyz,yaw],{timeout:120000});
 await shot(name,"Grounded camera XYZ "+xyz.join(',')+" yaw="+yaw+" pitch="+pitch);
}
async function aerial(name,xyz,yaw,pitch){
 const playerBefore=await state();
 await send('ReviewAerialCamera',[...xyz,yaw,pitch].join(','));
 await page.waitForTimeout(350);
 const playerAfter=await state();
 check('No Brax relocation by aerial camera '+name,
   Math.hypot(playerBefore.x-playerAfter.x,playerBefore.z-playerAfter.z)<.05,
   {before:[playerBefore.x,playerBefore.z],after:[playerAfter.x,playerAfter.z]});
 await shot(name,"QA-only aerial camera XYZ "+xyz.join(',')+" yaw="+yaw+" pitch="+pitch+"; original Brax remains on street");
}
try {
 check('Correct Unity source',sha===release.sourceCommit&&sha===city.commit&&sha===segment.sourceCommit);
 check('Single real Unity city scene',city.areas.length===1&&city.areas[0].area==='city');
 check('Segment profile only, full source untouched',city.scope==='city-segment-mobile-architecture-review'&&segment.fullCitySourceRetained);
 check('Original Brax, no NPC gameplay',segment.originalBrax&&city.originalBraxRig);
 check('Selected frontages and visible skyline',segment.selectedBuildingFrontages>=5&&segment.selectedHighRises>=2,
   {frontages:segment.selectedBuildingFrontages,towers:segment.selectedHighRises});
 check('Source graphics trimmed',segment.originalCityMeshesPruned>0&&segment.distantCityRenderersPruned>0,
   {original:segment.originalCityMeshesPruned,city:segment.distantCityRenderersPruned});
 check('Stage-1 packed city under old monolithic 558 MB',release.transport.uncompressedBytes<558052588,
   {new:release.transport.uncompressedBytes,old:558052588});
 server=http.createServer((req,res)=>{
   let file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));
   if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
   if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
   if(!fs.existsSync(file)){res.writeHead(404);res.end();return;}
   res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.wasm':'application/wasm','.json':'application/json','.png':'image/png','.jpg':'image/jpeg','.unityweb':'application/octet-stream'})[path.extname(file)]||'application/octet-stream');
   res.setHeader('Content-Length',fs.statSync(file).size);res.setHeader('Cache-Control','no-store');
   fs.createReadStream(file).pipe(res);
 });
 await new Promise(resolve=>server.listen(18773,'127.0.0.1',resolve));
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 ctx=await browser.newContext({viewport:{width:915,height:412},deviceScaleFactor:1,hasTouch:true,isMobile:true});
 page=await ctx.newPage();
 page.on('pageerror',e=>errors.push(e.message));
 page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
 await page.goto('http://127.0.0.1:18773/?qa=1',{waitUntil:'domcontentloaded'});
 const time=Date.now();await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,{timeout:480000});
 loadingMs=Date.now()-time;
 const first=await state();
 check('Actual Unity player starts, only original Brax',first.ready===true&&first.area==='city'&&first.handFound&&first.leftFootFound&&first.rightFootFound);
 check('Player SHA matches compiled build',first.build===sha,{expected:sha,actual:first.build});
 check('Brax spawns in the intended local street',Math.abs(first.x-6.75)<1&&Math.abs(first.z-27.85)<1,{x:first.x,z:first.z});
 check('Bounded iOS gzip transport completed',await page.evaluate(()=>window.BraxCityLoadingStats?.complete===true&&window.BraxCityLoadingStats.maxInputChunk<=65536));
 await shot('01-street-original-brax-spawn','Normal street spawn: rendered Brax, pavement and environment');
 // Manual orbit must NOT be replaced by automatic tracking immediately.
 await send('LookInput','105,-840');
 await page.waitForFunction(()=>window.BraxhornState?.pitch<=-89.9,{timeout:30000});
 check('City camera looks vertically into the sky',(await state()).pitch<=-89.9);
 await shot('02-full-sky-clouds-and-sun','Camera pitched fully skyward -90°; actual 1K cloud panorama');
 await send('LookInput','-105,715'); // return to ~24 degrees and baseline yaw
 await page.waitForTimeout(420);
 await view('03-nutrition-street-level',[20.7,.23,22.1],180,15);
 await view('04-nova-facades',[42,.23,34.73],0,16);
 await view('05-crossing-from-brax',[52,.23,29],90,15);
 await view('06-billboard-brax-avenue',[-6.2,.23,22.4],0,-24);
 await view('07-billboard-brax-poster',[27.2,.23,34.8],180,-23);
 await aerial('08-overhead-east',[34,115,28.5],180,79);
 await aerial('09-overhead-nova',[43,105,44],180,79);
 await aerial('10-overhead-west',[4,88,28.5],180,78);
 await aerial('11-aerial-roofs-and-skyline',[27,120,28],130,58);
 // Aerial photos use QA-only camera. Resume Brax and check normal input.
 await view('12-restored-person-view',[6.75,.23,27.85],0,19);
 const before=await state();
 await send('MoveInput','0,1');
 await page.waitForFunction(t=>window.BraxhornState?.simulationTime>=t+.6,before.simulationTime,{timeout:100000});
 await send('MoveInput','0,0');
 const after=await state();
 check('Brax actually walks under normal controls',Math.hypot(before.x-after.x,before.z-after.z)>.08,
   {before:[before.x,before.z],after:[after.x,after.z]});
 check('No fatal JavaScript or Unity errors',errors.length===0,errors);
}catch(e){
 checks.push({name:'Fatal QA',passed:false,error:String(e)});
 if(page)await page.screenshot({path:path.join(out,'failure.png')}).catch(()=>{});
}finally{
 const result={passed:checks.every(c=>c.passed),sourceCommit:sha,buildId:release.buildId,
   checkedAt:new Date().toISOString(),checks,captures,
   originalBrax:true,developerMockups:false,
   screenshotsAre:'Actual compiled Unity 6 WebGL, direct browser screenshots',
   cameraAltitudeDisclaimer:'Aerial camera is QA-only and does not teleport the playable Brax',
   device:'Chromium SwiftShader simulated mobile viewport 915x412, NOT a physical iPhone',
   physicalIphonePass:false,fullWalkPass:false,userVisualAcceptance:'PENDING',
   initialUncompressedUnityDataBytes:release.transport.uncompressedBytes,
   initialCompressedUnityBytes:release.transport.bytes,
   desktopLoadMs:loadingMs,errors};
 fs.writeFileSync(path.join(out,'segment-qa.json'),JSON.stringify(result,null,2));
 if(ctx)await ctx.close();if(browser)await browser.close();if(server)server.close();
 if(!result.passed)process.exitCode=1;
}
