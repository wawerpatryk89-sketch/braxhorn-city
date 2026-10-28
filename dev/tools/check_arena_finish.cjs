const {chromium}=require('playwright'),fs=require('fs'),cp=require('child_process'),assert=require('assert/strict');
const out='dev/review/estate-doors',report={checks:[],errors:[]};let browser;
const server=cp.spawn('python3',['-m','http.server','18774'],{stdio:'ignore'});
(async()=>{
 browser=await chromium.launch({headless:true,executablePath:'/tmp/browser/chrome-headless-shell-linux64/chrome-headless-shell',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
 const page=await browser.newPage({viewport:{width:915,height:412},isMobile:true,hasTouch:true});page.on('pageerror',e=>report.errors.push(e.message));
 await page.goto('http://127.0.0.1:18774/public/osiedle/');await page.waitForFunction(()=>__os?.ready,null,{timeout:180000});await page.evaluate(()=>__os.pause(true));
 await page.waitForFunction(()=>getComputedStyle(document.getElementById('loader')).opacity==='0');
 for(const [name,x,expected]of [['boxing',-86,.84],['octagon',86,.6]]){
  const result=await page.evaluate(([x,expected])=>{const m=__os;m.teleport(x,74,0);for(let i=0;i<110;i++)m.step(0,-.1);return{position:m.player.pos.toArray(),expected,yaw:m.cam.yaw};},[x,expected]);
  const pass=Math.abs(result.position[2]-63)<.1&&Math.abs(result.position[1]-expected)<.04&&result.yaw===0;
  report.checks.push({name:name+'-steps-and-platform',pass,...result});assert.ok(pass,JSON.stringify(result));
  await page.evaluate(x=>{__os.cam.dist=7.5;__os.teleport(x-2.5,70,.4,.6);__os.render();},x);
  await page.screenshot({path:out+'/'+name+'.png',timeout:120000});
 }
 const render=await page.evaluate(()=>{const lights=[];__os.scene.traverse(o=>{if(o.isLight)lights.push(o.type);});return{lights,antialias:__os.renderer.getContext().getContextAttributes().antialias,cachedShadows:!__os.renderer.shadowMap.autoUpdate,movingDoorShadows:__os.doors.items.some(d=>d.leaves.some(l=>l.mesh.castShadow)),render:__os.stats()};});
 report.render=render;const pass=render.lights.length===3&&render.antialias&&render.cachedShadows&&!render.movingDoorShadows;
 report.checks.push({name:'existing-lighting-and-quality-retained',pass});assert.ok(pass);assert.equal(report.errors.length,0);console.log(JSON.stringify(report));
})().catch(e=>{report.failure=e.message;console.error(e);process.exitCode=1;}).finally(async()=>{fs.writeFileSync(out+'/arenas.json',JSON.stringify(report,null,2));if(browser)await browser.close();server.kill();});
