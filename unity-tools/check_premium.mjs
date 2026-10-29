import {chromium} from 'playwright';
import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const root=path.resolve('unity-preview'),out=path.resolve('unity-review');fs.mkdirSync(out,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.webp':'image/webp','.png':'image/png','.css':'text/css'};
const server=http.createServer((req,res)=>{let f=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://local').pathname));if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403);res.end();return}if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');if(!fs.existsSync(f)){res.writeHead(404);res.end();return}res.setHeader('Content-Type',mime[path.extname(f)]||'application/octet-stream');fs.createReadStream(f).pipe(res)});
await new Promise(r=>server.listen(8765,'127.0.0.1',r));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});const logs=[],checks=[];
page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR: '+e.message));
const state=()=>page.evaluate(()=>window.BraxhornState);
const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
function check(name,passed,data){checks.push({name,passed:!!passed,data});if(!passed)console.error('CHECK FAILED',name,JSON.stringify(data))}
async function frames(n=3){const s=await state();await page.waitForFunction(([f,n])=>window.BraxhornState?.frames>=f+n,[s.frames,n],{timeout:30000,polling:100})}
async function alignCamera(yaw){await page.waitForFunction(y=>{const s=window.BraxhornState,a=y*Math.PI/180;return s&&Math.abs(s.yaw-y)<.1&&Math.abs(s.rightX+Math.cos(a))<.02&&Math.abs(s.rightZ-Math.sin(a))<.02},yaw,{timeout:30000,polling:100});await frames(2)}
async function reset(yaw=0){await send('Command','reset');await alignCamera(0);if(yaw){await send('LookInput',(-yaw/.22)+',0');await alignCamera(yaw)}}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png'),timeout:90000})}
async function waitIdle(){await page.waitForFunction(()=>window.BraxhornState?.animation==='Idle',null,{timeout:30000,polling:100})}
async function moved(a,axis,sign){await page.waitForFunction(([p,axis,sgn])=>{const s=window.BraxhornState;return s&&sgn*((s.x-p.x)*p[axis+'X']+(s.z-p.z)*p[axis+'Z'])>.25},[a,axis,sign],{timeout:20000,polling:100})}
try{
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:240000});
 if(await page.evaluate(()=>window.BraxhornError))throw new Error(await page.evaluate(()=>window.BraxhornError));
 check('Unity initialized actual Brax',true,await state());
 await page.waitForFunction(()=>window.unityInstance&&window.BraxhornState.frames>15,null,{timeout:90000});await frames();await shot('01-premium-hall');
 for(const yaw of [0,90])for(const [key,axis,sign] of [['d','right',1],['a','right',-1],['w','forward',1],['s','forward',-1]]){
  await reset(yaw);const a=await state();let error=null;await page.keyboard.down(key);
  try{await moved(a,axis,sign)}catch(e){error=String(e)}finally{await page.keyboard.up(key)}
  await frames(2);const b=await state(),dot=(b.x-a.x)*a[axis+'X']+(b.z-a.z)*a[axis+'Z'];
  check('Camera-relative '+key+' at '+yaw,!error&&sign*dot>.1,{dot,before:a,after:b,error});
 }
 await reset();const j=await page.locator('#joystick').boundingBox(),a=await state();
 const cdp=await page.context().newCDPSession(page);const x=j.x+j.width/2,y=j.y+j.height/2;let touchError=null;
 await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:1}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x+j.width*.30,y,id:1}]});
 try{await moved(a,'right',1)}catch(e){touchError=String(e)}finally{await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]})}
 await frames(2);const b=await state();check('Touch joystick right is camera right',!touchError&&(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ>.15,{before:a,after:b,error:touchError});
 const old=await state();await page.mouse.move(550,135);await page.mouse.down();await page.mouse.move(700,150,{steps:8});await page.mouse.up();await frames(3);const rotated=await state();check('Manual camera only',Math.abs(rotated.yaw-old.yaw)>5,{before:old.yaw,after:rotated.yaw});
 await waitIdle();await page.click('#punch');await page.waitForFunction(()=>window.BraxhornState.animation==='Punch',null,{timeout:20000});const h1=await state();
 await page.waitForFunction(h=>{const s=window.BraxhornState;return s.frames>h.frames&&Math.hypot(s.handX-h.handX,s.handY-h.handY,s.handZ-h.handZ)>.015},h1,{timeout:20000,polling:50});const h2=await state();
 check('Original punch moves original bones',true,{before:h1,after:h2});await shot('02-premium-punch');await waitIdle();
 await page.click('#kick');await page.waitForFunction(()=>window.BraxhornState.animation==='Kick',null,{timeout:20000});check('Original kick available',true,await state());await shot('03-premium-kick');await waitIdle();
 await page.click('#menu');await page.waitForFunction(()=>window.BraxhornState.paused);await page.click('#reset');await page.click('#resume');await frames(3);const rs=await state();check('Pause and reset preserve controls',Math.hypot(rs.x-1.5,rs.z-2.8)<.3&&!rs.paused,rs);
 for(const [name,v] of [['04-ring-details','-9,1.05,-5.5,35,22,8'],['05-bags-windows','12.1,.08,-5.4,-90,25,6'],['06-brax-front','1.5,.08,2.8,180,10,3.3'],['07-library-lounge','13.3,.08,6.6,180,20,5']]){
  await send('ReviewView',v);await frames(5);
  if(name==='06-brax-front'){await send('MoveInput','0,-.24');await frames(5);await send('MoveInput','0,0');await waitIdle();await frames(3)}
  await shot(name);
 }
 check('No browser script errors',!logs.some(l=>l.startsWith('PAGEERROR:')),logs.filter(l=>l.startsWith('PAGEERROR:')));
 check('No shader or pink material errors',!logs.some(l=>/Shader.*(not supported|unsupported|error)|Hidden\/InternalErrorShader/i.test(l)),logs.filter(l=>/shader/i.test(l)));
 const passed=checks.every(c=>c.passed);fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed,platform:'Chromium WebGL2, 915x412, actual CDP touch input; camera-basis synchronized per rendered frame; not a physical iPhone test',checks},null,2));
 console.log(passed?'UNITY_PREMIUM_BROWSER_TEST_PASS':'UNITY_PREMIUM_BROWSER_TEST_FAIL',checks.length);if(!passed)process.exitCode=1;
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:false,error:String(e),checks},null,2));console.error(e);process.exitCode=1}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close()}
