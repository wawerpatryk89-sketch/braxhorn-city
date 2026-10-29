// Actual compiled Unity 6 WebGL mobile-street candidate QA.
// Desktop Chromium/SwiftShader is an integration diagnostic, not an iPhone pass.
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
const root=path.resolve(process.env.PLAYER_ROOT||'unity-preview');
const output=path.resolve(process.env.QA_OUT||'city-ios-review');
fs.mkdirSync(output,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const expected=process.env.EXPECTED_BUILD_SHA||release.sourceCommit;
const city=JSON.parse(fs.readFileSync(path.join(root,'city-migration.json'),'utf8'));
const rebuild=JSON.parse(fs.readFileSync(path.join(root,'city-rebuild.json'),'utf8'));
const records=[],errors=[];let browser=null,context=null,server=null,page=null;
let start=Date.now(),navigationCount=0;
function check(name,condition,evidence){
  records.push({name,passed:Boolean(condition),evidence});
  if(!condition)throw new Error(name+' '+JSON.stringify(evidence));
}
try {
  check('Exact signed source SHA',expected===release.sourceCommit&&expected===city.commit,{expected,release:release.sourceCommit,city:city.commit});
  check('Only city scene in the stage-1 mobile player',city.areas.length===1&&city.areas[0].area==='city',city.areas.map(x=>x.area));
  check('Original Brax and gym source preserved',city.originalBraxRig===true&&city.originalModels===true,{rig:city.originalBraxRig,models:city.originalModels});
  check('Eighteen real street buildings',rebuild.actuallyBuiltStreetBuildings===18,rebuild.actuallyBuiltStreetBuildings);
  check('At least nine new tower modules',rebuild.actuallyBuiltHighRises>=9,rebuild.actuallyBuiltHighRises);
  check('397 original vehicle meshes removed from review scene',rebuild.reviewOnlyVehicleObjectsRemoved===397,rebuild.reviewOnlyVehicleObjectsRemoved);
  check('No active missions and other playables',rebuild.missionReservations===11,{missionReservations:rebuild.missionReservations,mode:city.scope});
  const bytes=release.transport.uncompressedBytes,compressed=release.transport.bytes;
  // Budget is a safeguard against another 558 MB city+estate bootstrap,
  // not a measured iPhone memory threshold.
  check('iOS decoded bootstrap is materially smaller than previous 558 MB',bytes<400000000,{bytes,originalCityAndEstate:558052588,compressed});
  server=http.createServer((req,res)=>{
    try {
      const u=new URL(req.url,'http://localhost');
      let file=path.resolve(root,'.'+decodeURIComponent(u.pathname));
      if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
      if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
      if(!fs.existsSync(file)){res.writeHead(404);res.end();return;}
      res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.unityweb':'application/octet-stream','.png':'image/png'})[path.extname(file)]||'application/octet-stream');
      res.setHeader('Cache-Control','no-store');
      res.setHeader('Content-Length',String(fs.statSync(file).size));
      fs.createReadStream(file).pipe(res);
    } catch(e){res.writeHead(500);res.end(String(e));}
  });
  await new Promise(resolve=>server.listen(18772,'127.0.0.1',resolve));
  browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
  context=await browser.newContext({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});
  page=await context.newPage();
  page.on('framenavigated',frame=>{if(frame===page.mainFrame())navigationCount++;});
  page.on('pageerror',e=>errors.push('PAGEERROR '+e.message));
  page.on('console',m=>{if(m.type()==='error')errors.push('CONSOLE '+m.text());});
  await page.goto('http://127.0.0.1:18772/?ios-qa=1',{waitUntil:'domcontentloaded'});
  check('Real Unity loader page and Start button',await page.locator('#start').count()===1,{});
  await page.click('#start');
  await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,{timeout:420000});
  const initial=await page.evaluate(()=>({state:window.BraxhornState,error:window.BraxhornError,load:window.BraxCityLoadingStats}));
  check('Actual Unity player starts with Brax',initial.state?.ready===true&&!initial.error,{error:initial.error,area:initial.state?.area});
  check('Compiled player matches reviewed SHA',initial.state?.build===expected,{expected,actual:initial.state?.build});
  check('Only original city area is running',initial.state?.area==='city',initial.state?.area);
  check('Native streamed loader completed with bounded chunks',initial.load?.complete===true&&initial.load?.maxInputChunk<=65536,initial.load);
  await page.screenshot({path:path.join(output,'01-actual-city-gym-start.png'),timeout:90000});
  const before=await page.evaluate(()=>window.BraxhornState);
  await page.evaluate(()=>window.BraxSend('MoveInput','0,1'));
  await page.waitForFunction(t=>window.BraxhornState?.simulationTime>=t+.65,before.simulationTime,{timeout:120000});
  await page.evaluate(()=>window.BraxSend('MoveInput','0,0'));
  const after=await page.evaluate(()=>window.BraxhornState);
  check('Brax moves with real controller',Math.hypot(after.x-before.x,after.z-before.z)>.07,{before:{x:before.x,z:before.z},after:{x:after.x,z:after.z}});
  // Targeted lamppost regression from previous no-teleport walk failure.
  // The teleport here is used ONLY for inspection, never reported as a
  // passed continuous 87-checkpoint walking route.
  const view=await page.evaluate(()=>{window.BraxSend('ReviewView','46.19,0.23,34.77,90,16,5.8');return window.BraxhornState;});
  await page.waitForFunction(()=>{const s=window.BraxhornState;return s&&Math.abs(s.x-46.19)<.15&&Math.abs(s.z-34.77)<.15&&s.grounded;},{timeout:120000});
  await page.screenshot({path:path.join(output,'02-actual-city-far-sidewalk.png')});
  const pre=await page.evaluate(()=>window.BraxhornState);
  await page.evaluate(()=>window.BraxSend('MoveInput','0,1'));
  await page.waitForFunction(t=>window.BraxhornState?.simulationTime>=t+1.3,pre.simulationTime,{timeout:120000});
  await page.evaluate(()=>window.BraxSend('MoveInput','0,0'));
  const post=await page.evaluate(()=>window.BraxhornState);
  check('Corrected NOVA far sidewalk is physically passable',post.x<pre.x-.38,{before:{x:pre.x,z:pre.z},after:{x:post.x,z:post.z},expectedDirection:'x decreasing'});
  await page.screenshot({path:path.join(output,'03-actual-city-nova-approach.png')});
  check('No spontaneous browser main-frame reload',navigationCount===1,{navigationCount});
  check('No browser runtime errors',errors.length===0,errors);
}catch(e){
  records.push({name:'QA exception',passed:false,error:String(e)});
  if(page)await page.screenshot({path:path.join(output,'failure.png')}).catch(()=>{});
}finally{
  const report={sourceCommit:expected,buildId:release.buildId,passed:records.every(c=>c.passed),checks:records,
    sourceDownloadBytes:release.transport.bytes,sourceExpandedBytes:release.transport.uncompressedBytes,
    stage:'city-only-with-original-Brax-and-gym',
    entranceAndEstate:'Source preserved; estate not bundled into mobile city-review runtime',
    physicalIphoneTested:false,actualUnityScreenshotCapture:true,
    testedBrowser:'Headless Chromium with SwiftShader; NOT physical iPhone',
    elapsedSeconds:(Date.now()-start)/1000,errors,navigationCount};
  fs.writeFileSync(path.join(output,'ios-review.json'),JSON.stringify(report,null,2));
  if(context)await context.close();if(browser)await browser.close();
  if(server)server.close();
  if(!report.passed)process.exitCode=1;
}
