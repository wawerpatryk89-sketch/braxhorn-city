import { chromium } from 'playwright';
import http from 'node:http';import fs from 'node:fs';import path from 'node:path';
const root=path.resolve('unity-preview'),out=path.resolve('unity-review');fs.mkdirSync(out,{recursive:true});
const mime={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.webp':'image/webp','.png':'image/png','.css':'text/css'};
const server=http.createServer((req,res)=>{let f=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://local').pathname));if(!f.startsWith(root)){res.writeHead(403);res.end();return}if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');if(!fs.existsSync(f)){res.writeHead(404);res.end();return}res.setHeader('Content-Type',mime[path.extname(f)]||'application/octet-stream');fs.createReadStream(f).pipe(res)});
await new Promise(r=>server.listen(8765,'127.0.0.1',r));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true,deviceScaleFactor:1});const logs=[],checks=[];
page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR: '+e.message));
const state=()=>page.evaluate(()=>window.BraxhornState);
function check(name,passed,data){checks.push({name,passed,data});if(!passed)throw new Error(name+' FAILED '+JSON.stringify(data))}
try{
 await page.goto('http://127.0.0.1:8765/',{waitUntil:'domcontentloaded'});await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:180000});
 check('Unity initialized original player',!(await page.evaluate(()=>window.BraxhornError)),await state());
 await page.waitForFunction(()=>window.unityInstance&&window.BraxhornState.frames>15,null,{timeout:90000});await page.waitForTimeout(1200);
 await page.screenshot({path:path.join(out,'01-gym-brax.png'),timeout:90000});
 const a=await state();await page.keyboard.down('w');await page.waitForTimeout(1600);const b=await state();await page.screenshot({path:path.join(out,'02-walking.png'),timeout:90000});await page.keyboard.up('w');
 check('Keyboard movement',Math.hypot(b.x-a.x,b.z-a.z)>.4,{before:a,after:b});await page.waitForTimeout(400);
 const j=await page.locator('#joystick').boundingBox();const c=await state();
 await page.mouse.move(j.x+j.width/2,j.y+j.height/2);await page.mouse.down();await page.mouse.move(j.x+j.width*.85,j.y+j.height/2,{steps:5});await page.waitForTimeout(1000);await page.mouse.up();const d=await state();
 check('Pointer joystick movement',Math.hypot(d.x-c.x,d.z-c.z)>.2,{before:c,after:d});
 const beforeLook=await state();await page.mouse.move(550,135);await page.mouse.down();await page.mouse.move(700,150,{steps:8});await page.mouse.up();await page.waitForTimeout(400);const afterLook=await state();check('Manual camera drag',Math.abs(afterLook.yaw-beforeLook.yaw)>5,{before:beforeLook.yaw,after:afterLook.yaw});
 await page.click('#punch');await page.waitForFunction(()=>window.BraxhornState.animation==='Punch',null,{timeout:10000});const h1=await state();await page.waitForTimeout(280);const h2=await state();
 check('Real punch animation moves bones',Math.hypot(h2.handX-h1.handX,h2.handY-h1.handY,h2.handZ-h1.handZ)>.015,{before:h1,after:h2});
 await page.screenshot({path:path.join(out,'03-punch.png'),timeout:90000});await page.waitForTimeout(1500);
 await page.click('#kick');await page.waitForFunction(()=>window.BraxhornState.animation==='Kick',null,{timeout:10000});await page.waitForTimeout(350);await page.screenshot({path:path.join(out,'04-kick.png'),timeout:90000});check('Real kick available',true,await state());
 await page.waitForTimeout(1500);await page.click('#menu');await page.waitForFunction(()=>window.BraxhornState.paused);await page.click('#reset');await page.click('#resume');await page.waitForTimeout(600);const reset=await state();check('Pause and safe reset',Math.hypot(reset.x-1.5,reset.z-2.8)<.3&&!reset.paused,reset);
 await page.screenshot({path:path.join(out,'05-gym-controls.png'),timeout:90000});
 check('No browser script errors',!logs.some(l=>l.startsWith('PAGEERROR:')),logs.filter(l=>l.startsWith('PAGEERROR:')));
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:true,platform:'Chromium software WebGL2, 915x412 touch emulation; not a physical iPhone test',checks},null,2));
 console.log('UNITY_PLAYABLE_BROWSER_TEST_PASS',checks.length);
}catch(e){await page.screenshot({path:path.join(out,'failure.png'),timeout:90000}).catch(()=>{});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:false,error:String(e),checks},null,2));console.error(e);process.exitCode=1}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close()}
