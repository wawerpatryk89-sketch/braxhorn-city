import {chromium} from 'playwright';import fs from 'node:fs';import path from 'node:path';import http from 'node:http';
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
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
 await until(()=>window.BraxhornState?.ready||window.BraxhornError,null,480000);check('Unity city initializes',!await page.evaluate(()=>window.BraxhornError),await state());
 const expected=JSON.parse(fs.readFileSync(path.join(root,'city-migration.json'))),s=await until(()=>window.BraxhornState?.grounded);
 check('Exact compiled city commit and geometry',s.build===process.env.GITHUB_SHA&&s.area==='city'&&s.cityMeshes===724&&expected.areas.length===2,{state:s,manifest:expected});await shot('01-original-gym-in-city');
 for(const yaw of [0,90,180,270]){
  await send('ReviewView','1.5,.08,2.8,'+yaw+',19,5.8');await until(v=>Math.abs(window.BraxhornState.yaw-v)<.1,yaw);await page.waitForTimeout(200);const a=await state();const b=await move(.25,1,0);const dot=(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ;check('Screen-relative right in city at '+yaw,dot>.15,{dot});
 }
 await point('gym_door_in');await send('Command','action');await until(()=>window.BraxhornState?.doorOpen);await point('gym_door_out');
 await send('ReviewView','6,.08,13,180,19,5.8');await until(()=>window.BraxhornState?.grounded);const exit=await move(2.3,0,1);check('Original gym exit reaches city without the former z clamp',exit.z>21.0,{x:exit.x,y:exit.y,z:exit.z});await shot('02-original-street');
 for(const [id,name]of [['quay_entry','03-waterfront'],['pier_end','04-original-pier'],['shop_inside','05-shop-interior'],['club_dance','06-nova-club'],['cellar_entry','07-nova-cellar'],['fight1_center','08-original-courtyard']]){
  const s=await point(id);check('Original source point '+id+' has ground',s.grounded&&s.y>-4.0&&s.y<2.0,{x:s.x,y:s.y,z:s.z});await shot(name);
  if(id==='cellar_entry'){await page.waitForTimeout(700);check('NOVA cellar does not trigger old below-floor reset',(await state()).y<-3.3,await state());}
 }
 await point('closed_estate');await send('Command','action');const estate=await until(()=>window.BraxhornState?.area==='estate'&&window.BraxhornState.ready&&window.BraxhornState.grounded,null,300000);
 check('Original estate scene loads through the city gate',estate.cityMeshes===553&&estate.build===process.env.GITHUB_SHA,estate);await shot('09-estate-entry');
 for(const [id,name]of [['park','10-estate-park'],['court','11-estate-court'],['villa_front','12-original-villa'],['casino','13-casino'],['hospital','14-hospital'],['police','15-police'],['club','16-estate-club'],['boxing','17-boxing-hall'],['arena','18-arena']]){
  const s=await point(id);check('Original estate point '+id+' is reachable',s.grounded&&s.y>-.15,{x:s.x,y:s.y,z:s.z});await shot(name);
 }
 await send('ReviewView','0,.08,-27.5,0,12,5.8');await until(()=>window.BraxhornState?.grounded);await page.waitForTimeout(400);
 const before=await state();check('Original villa door has interaction',before.context.includes('DRZWI'),before.context);await send('Command','action');await until(n=>window.BraxhornState.openDoors>n,before.openDoors);await shot('19-villa-open-door');
 await point('estate_entry');await send('Command','action');await until(()=>window.BraxhornState?.area==='city'&&window.BraxhornState.ready&&window.BraxhornState.grounded,null,300000);await shot('20-city-return');
 check('Estate return keeps the original city map',(await state()).x<-90,await state());
 const errors=logs.filter(l=>/PAGEERROR|NullReferenceException|MissingReferenceException|Shader.*(not supported|error)|abort\(|memory access out of bounds/.test(l));check('No runtime or shader exceptions',errors.length===0,errors);
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:true,commit:process.env.GITHUB_SHA,checks,platform:'Chromium WebGL2 / SwiftShader; 915x412 touch viewport, not a physical iPhone test',limitations:expected.notYetPorted,visualAcceptance:'requires screenshot review'},null,2));
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:false,commit:process.env.GITHUB_SHA,error:String(e),lastState:await state().catch(()=>null),checks},null,2));console.error(e);process.exitCode=1}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close()}
