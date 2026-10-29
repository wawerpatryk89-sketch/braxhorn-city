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
function check(name,passed,data){checks.push({name,passed,data});if(!passed)throw new Error(name+' FAILED '+JSON.stringify(data))}
async function settle(ms=450){await page.waitForTimeout(ms)}
async function reset(yaw=0){await send('Command','reset');await settle();if(yaw)await send('LookInput',(-yaw/.22)+',0');await settle()}
async function shot(name){await page.screenshot({path:path.join(out,name+'.png'),timeout:90000})}
try{
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});await page.click('#start');
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:240000});
 check('Unity initialized actual Brax',!(await page.evaluate(()=>window.BraxhornError)),await state());
 await page.waitForFunction(()=>window.unityInstance&&window.BraxhornState.frames>15,null,{timeout:90000});await settle(1000);await shot('01-premium-hall');
 for(const yaw of [0,90])for(const [key,axis,sign] of [['d','right',1],['a','right',-1],['w','forward',1],['s','forward',-1]]){
  await reset(yaw);const a=await state();await page.keyboard.down(key);await settle(600);await page.keyboard.up(key);await settle(150);const b=await state();
  const dot=(b.x-a.x)*a[axis+'X']+(b.z-a.z)*a[axis+'Z'];check('Camera-relative '+key+' at '+yaw,sign*dot>.10,{dot,before:a,after:b});
 }
 await reset();const j=await page.locator('#joystick').boundingBox(),a=await state();
 const cdp=await page.context().newCDPSession(page);const x=j.x+j.width/2,y=j.y+j.height/2;
 await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:1}]});
 await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x+j.width*.30,y,id:1}]});await settle(900);
 await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});await settle(200);const b=await state();
 check('Touch joystick right is camera right',(b.x-a.x)*a.rightX+(b.z-a.z)*a.rightZ>.15,{before:a,after:b});
 const old=await state();await page.mouse.move(550,135);await page.mouse.down();await page.mouse.move(700,150,{steps:8});await page.mouse.up();await settle();const rotated=await state();check('Manual camera only',Math.abs(rotated.yaw-old.yaw)>5,{before:old.yaw,after:rotated.yaw});
 await page.click('#punch');await page.waitForFunction(()=>window.BraxhornState.animation==='Punch',null,{timeout:10000});const h1=await state();await settle(280);const h2=await state();
 check('Original punch moves original bones',Math.hypot(h2.handX-h1.handX,h2.handY-h1.handY,h2.handZ-h1.handZ)>.015,{before:h1,after:h2});await shot('02-premium-punch');await settle(1400);
 await page.click('#kick');await page.waitForFunction(()=>window.BraxhornState.animation==='Kick',null,{timeout:10000});check('Original kick available',true,await state());await settle(300);await shot('03-premium-kick');await settle(1400);
 await page.click('#menu');await page.waitForFunction(()=>window.BraxhornState.paused);await page.click('#reset');await page.click('#resume');await settle(600);const resetState=await state();check('Pause and reset preserve controls',Math.hypot(resetState.x-1.5,resetState.z-2.8)<.3&&!resetState.paused,resetState);
 for(const [name,v] of [['04-ring-details','-9,1.05,-5.5,35,22,8'],['05-bags-windows','12.1,.08,-5.4,-90,25,6'],['06-brax-front','1.5,.08,2.8,180,10,3.7'],['07-library-lounge','13.3,.08,6.6,180,20,5']]){
  await send('ReviewView',v);await settle(1200);await shot(name);
 }
 check('No browser script errors',!logs.some(l=>l.startsWith('PAGEERROR:')),logs.filter(l=>l.startsWith('PAGEERROR:')));
 check('No shader or pink material errors',!logs.some(l=>/Shader.*(not supported|unsupported|error)|Hidden\/InternalErrorShader/i.test(l)),logs.filter(l=>/shader/i.test(l)));
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:true,platform:'Chromium WebGL2, 915x412, actual CDP touch input; not a physical iPhone test',checks},null,2));console.log('UNITY_PREMIUM_BROWSER_TEST_PASS',checks.length);
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({passed:false,error:String(e),checks},null,2));console.error(e);process.exitCode=1}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close()}
