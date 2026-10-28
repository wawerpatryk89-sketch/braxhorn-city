const {chromium}=require('playwright'),fs=require('fs'),cp=require('child_process'),assert=require('assert/strict');
const out='dev/review/estate-doors';fs.mkdirSync(out,{recursive:true});
const report={checks:[],errors:[],missing:[]};let browser;
const server=cp.spawn('python3',['-m','http.server','18773'],{stdio:'ignore'});
function check(name,pass,details){report.checks.push({name,pass,details});console.log(name,pass?'PASS':'FAIL',JSON.stringify(details??''));}
(async()=>{
 browser=await chromium.launch({headless:true,executablePath:'/tmp/browser/chrome-headless-shell-linux64/chrome-headless-shell',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const context=await browser.newContext({viewport:{width:915,height:412},hasTouch:true,isMobile:true});
 await context.addInitScript(()=>{localStorage.setItem('bx_sound','false');localStorage.setItem('bx_hero',JSON.stringify('brax'));});
 const page=await context.newPage();page.on('pageerror',e=>report.errors.push(e.message));page.on('response',r=>{if(r.status()>=400&&r.url().includes('127.0.0.1'))report.missing.push(r.url());});
 await page.goto('http://127.0.0.1:18773/public/osiedle/');
 await page.waitForFunction(()=>window.__os?.ready||window.__os?.assetErrors.length,null,{timeout:180000});
 assert.ok(await page.evaluate(()=>__os.ready));await page.evaluate(()=>__os.pause(true));
 const tests=await page.evaluate(()=>{
  const m=__os,result=[];
  const at=(d,z)=>{const a=d.yaw;return [d.x+Math.sin(a)*z,d.z+Math.cos(a)*z];};
  for(const d of m.doors.items){
   const pos=at(d,1.8);m.teleport(...pos,d.yaw,.22,d.base+1.7);const yaw=m.cam.yaw;
   const prompt=m.doors.label(m.player),nearest=m.doors.nearest(m.player)?.id;
   for(let i=0;i<24;i++)m.step(-Math.sin(d.yaw)*.1,-Math.cos(d.yaw)*.1);
   const closedZ=m.doors.local(d,m.player.pos).z;
   m.teleport(...pos,d.yaw,.22,d.base+1.7);m.pause(false);m.act('act');m.pause(true);
   const animation=m.player.state;m.sim(6);
   const angle=d.angle,opened=d.state;
   for(let i=0;i<28;i++)m.step(-Math.sin(d.yaw)*.1,-Math.cos(d.yaw)*.1);
   const insideZ=m.doors.local(d,m.player.pos).z;
   result.push({id:d.id,prompt,nearest,closedZ,animation,angle,opened,insideZ,yawStable:yaw===m.cam.yaw});
  }return result;
 });
 for(const r of tests)check(r.id,r.prompt==='OTWÓRZ'&&r.nearest===r.id&&r.closedZ>.59&&r.animation==='door'&&r.angle===1&&r.opened==='open'&&r.insideZ<-.55&&r.yawStable,r);
 // Real button input, closing obstruction, then opening from the inside.
 const safety=await page.evaluate(()=>{
  const m=__os,d=m.doors.items.find(d=>d.id==='villa-front');m.teleport(0,-29.9,0);m.pause(false);m.act('act');m.pause(true);m.sim(2);const staysOpen=d.state==='open';
  m.teleport(0,-28.2,0);m.pause(false);m.act('act');m.pause(true);m.sim(.16);m.teleport(0,-30.5,0);m.sim(2);const collisionSafe=!m.doors.overlaps(d,m.player,d.angle)&&d.state!=='closed';
  m.teleport(0,-28.2,0);m.sim(2);m.pause(false);m.act('act');m.pause(true);m.sim(2);const closes=d.state==='closed';
  m.teleport(0,-31.8,0);m.pause(false);m.act('act');m.pause(true);const clip=m.player.overlays.at(-1)?.name;m.sim(6);return {staysOpen,collisionSafe,closes,clip,opensInside:d.state==='open',angle:d.angle};
 });check('closing-safety-and-pull-animation',safety.staysOpen&&safety.collisionSafe&&safety.closes&&safety.clip==='Opening'&&safety.opensInside,safety);
 // Save an open entrance, reload, and verify its visible and physical state.
 await page.evaluate(()=>{__os.teleport(0,-28.3,.3);__os.checkpoint();});
 await page.reload();await page.waitForFunction(()=>__os?.ready,null,{timeout:180000});await page.evaluate(()=>__os.pause(true));
 await page.waitForFunction(()=>getComputedStyle(document.getElementById('loader')).opacity==='0');
 check('open-doors-survive-reload',await page.evaluate(()=>__os.doors.items.find(d=>d.id==='villa-front').angle===1));
 for(const [name,x,z,yaw]of [['villa-open',0,-26,.15],['casino-open',-86,-22,.15],['hospital-open',86,34,.15]]){
  await page.evaluate(([x,z,yaw])=>{__os.teleport(x,z,yaw,.18);__os.render();},[x,z,yaw]);await page.screenshot({path:out+'/'+name+'.png',timeout:120000});
 }
 await page.evaluate(()=>{const d=__os.doors.items.find(d=>d.id==='villa-front');__os.doors.setAngle(d,0);d.state='closed';d.target=0;__os.teleport(0,-28.3,.15,.18);__os.render();});
 await page.screenshot({path:out+'/villa-closed.png',timeout:120000});
 await page.evaluate(()=>__os.pause(false));await page.locator('#btnAct').tap();await page.waitForFunction(()=>__os.player.state==='door');await page.evaluate(()=>{__os.pause(true);__os.sim(1.8);__os.render();});await page.screenshot({path:out+'/villa-opening.png',timeout:120000});await page.evaluate(()=>__os.sim(4));
 check('touch-action-opens-door',await page.evaluate(()=>__os.doors.items.find(d=>d.id==='villa-front').state==='open'));
 check('no-js-errors',report.errors.length===0,report.errors);check('no-missing-game-resources',report.missing.length===0,report.missing);
 assert.ok(report.checks.every(c=>c.pass),'Door checks failed');
})().catch(e=>{report.failure=e.message;console.error(e);process.exitCode=1;}).finally(async()=>{fs.writeFileSync(out+'/report.json',JSON.stringify(report,null,2));if(browser)await browser.close();server.kill();});
