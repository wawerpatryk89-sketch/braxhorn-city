import {chromium} from 'playwright';import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
const sourceBuild=process.env.SOURCE_BUILD_SHA||process.env.GITHUB_SHA;
const root=path.resolve('unity-preview'),out=path.resolve('city-review');fs.mkdirSync(out,{recursive:true});const checks=[],logs=[];
const server=http.createServer((req,res)=>{let f=path.resolve(root,'.'+new URL(req.url,'http://local').pathname);if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403);res.end();return}if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');if(!fs.existsSync(f)){res.writeHead(404);res.end();return}res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm'})[path.extname(f)]||'application/octet-stream');fs.createReadStream(f).pipe(res)});await new Promise(r=>server.listen(8765,'127.0.0.1',r));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true});
page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
const state=()=>page.evaluate(()=>window.BraxhornState);const send=(m,a='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[m,a]);
const until=async(f,arg,timeout=180000)=>{await page.waitForFunction(f,arg,{timeout,polling:100});return state()};
const shot=async(name)=>{await page.screenshot({path:path.join(out,name+'.png'),timeout:90000})};
function check(name,passed,data){checks.push({name,passed,data});console.log(passed?'PASS':'FAIL',name);if(!passed)throw Error(name+' '+JSON.stringify(data))}
async function move(seconds,x,y){const a=await state();await send('MoveInput',x+','+y);await until(p=>window.BraxhornState?.simulationTime>=p.t+p.s,{t:a.simulationTime,s:seconds});await send('MoveInput','0,0');await until(()=>window.BraxhornState?.speed<.04);return state()}
async function point(id){await send('TravelPoint',id);await until(q=>{const s=window.BraxhornState;return s?.activePoint===q&&s.grounded},id);await page.waitForTimeout(300);return state()}
try{
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});
 await page.click('#start');
 await until(()=>window.BraxhornState?.ready||window.BraxhornError,null,480000);
 check('Original Unity estate starts without separate fighter controller',!await page.evaluate(()=>window.BraxhornError),await state());
 const expected=JSON.parse(fs.readFileSync(path.join(root,'city-migration.json')));
 const s=await until(()=>window.BraxhornState?.grounded);
 check('ORIGINAL CITY PLAYER in estate scene, exact Unity build',s.build===sourceBuild&&s.area==='estate'&&s.cityMeshes===553&&expected.areas.length===2,{build:s.build,area:s.area,meshes:s.cityMeshes});
 check('Original Brax clips and character physics ready',s.ready&&s.grounded&&typeof s.playerYaw==='number'&&typeof s.cameraYaw==='number'&&typeof s.animation==='string',{animation:s.animation,grounded:s.grounded});
 await shot('01-original-brax-estate');
 for(const yaw of [0,90,180,270]){
  await send('ReviewView','0,.08,92,'+yaw+',19,5.8');
  await until(v=>Math.abs(window.BraxhornState.yaw-v)<.1,yaw);
  const a=await state(),b=await move(.28,1,0);
  const dot=(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ;
  check('Exact city camera-relative movement '+yaw,dot>.10,{dot,animation:b.animation});
 }
 const beforeLook=await state();await send('LookInput','35,0');
 await until(v=>Math.abs(window.BraxhornState.yaw-v)>.3,beforeLook.yaw);
 check('Original city camera look works',true,{before:beforeLook.yaw,after:(await state()).yaw});
 for(const [id,name] of [['park','02-park'],['court','03-court'],['villa_front','04-villa'],['casino','05-casino'],['hospital','06-hospital'],['police','07-police'],['club','08-club'],['boxing','09-boxing'],['arena','10-arena']]){
  const v=await point(id);
  check('Original estate waypoint '+id,v.grounded&&v.cityMeshes===553&&v.area==='estate',{x:v.x,y:v.y,z:v.z});
  await shot(name);
 }
 await send('ReviewView','0,.08,-27.5,0,12,5.8');
 // Wait for the NEW position and the next 120ms nearby-door scan.
 // Grounded alone may still describe the previous waypoint.
 await until(()=>Math.abs(window.BraxhornState?.z+27.5)<.35 && Math.abs(window.BraxhornState?.x)<.35);
 await until(()=>window.BraxhornState?.context?.includes('DRZWI'));
 const before=await state();
 check('Original villa has working door interaction',before.context.includes('DRZWI'),before.context);
 await send('Command','action');await until(n=>window.BraxhornState.openDoors>n,before.openDoors);
 await shot('11-original-villa-open-door');
 check('Same original player and controls, no new fighter',true,{area:(await state()).area,player:'BraxGymPlayer',doors:(await state()).openDoors});
 const errors=logs.filter(l=>/PAGEERROR|NullReferenceException|MissingReferenceException|Shader.*(not supported|error)|abort\(|memory access out of bounds/.test(l));
 check('No runtime or shader exceptions',errors.length===0,errors);
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:true,commit:sourceBuild,checks,playerSource:'BraxGymPlayer.cs / BraxCityBuild.CreateEstate',platform:'Chromium WebGL2 / SwiftShader; 915x412 touch viewport, NOT physical iPhone',visualAcceptance:'user required'},null,2));
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:false,commit:sourceBuild,error:String(e),lastState:await state().catch(()=>null),checks},null,2));console.error(e);process.exitCode=1}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close()}
