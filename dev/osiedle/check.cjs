const {chromium}=require('playwright');
const fs=require('fs');
const server=require('child_process').spawn('python3',['-m','http.server','18766'],{stdio:'ignore'});process.on('exit',()=>server.kill());
(async()=>{
await new Promise(r=>setTimeout(r,500));
const browser=await chromium.launch({headless:true,executablePath:process.env.CHROME_PATH||'/tmp/browser/chrome-headless-shell-linux64/chrome-headless-shell',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist']});
const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true,isMobile:true});
const errors=[];page.on('pageerror',e=>(errors.push(e.message),console.log('PAGE ERROR',e.message)));
await page.goto('http://127.0.0.1:18766/public/osiedle/'); console.log('PAGE LOADED');
await page.waitForFunction(()=>window.__os?.ready||window.__os?.assetErrors.length,{},{timeout:120000});
console.log('LOAD',await page.evaluate(()=>({status:document.getElementById('loadMsg').textContent,errors:__os.assetErrors})));
if(await page.evaluate(()=>!__os.ready))throw new Error('Asset load failed');
await page.evaluate(()=>{__os.start();__os.pause(true)});
const out='dev/osiedle/review';
if(!process.env.OS_FAST)for(const [name,x,z,yaw,pitch,y]of [['street',0,35,0,0,1.78],['villa',0,-23,0,0,1.78],['living',1,-33,.9,0,1.78],['kitchen',1,-39,1,0,1.78],['bedroom',5,-33,-.8,0,1.78],['garden',0,-54,Math.PI,0,1.78],['contact',30,-28,0,0,1.78],['casino-front',-86,-20,0,0,1.78],['casino-lobby',-89.4,-30,0,0,1.78],['casino-hall',-89.4,-40,-.45,0,1.82],['police-front',86,-20,0,0,1.78],['police-hall',86,-36,0,0,1.78],['club-front',-86,36,0,0,1.78],['club-hall',-86,12,0,0,1.94],['hospital-front',86,36,0,0,1.78],['hospital-hall',86,24,0,0,1.78],['hospital-room',75,20,0,-.1,1.78],['boxing',-86,74,0,-.07,2],['octagon',86,74,0,-.07,2],['park',4,-79,-1.2,0,1.78],['court',-8,-71,1,-.1,2.5],['overview',112,125,.67,-.72,142]]){
if(process.env.OS_SHOTS&&!process.env.OS_SHOTS.split(',').includes(name))continue;
await page.evaluate(v=>{__os.teleport(...v);const fog=__os.scene.fog;if(v[4]>20)__os.scene.fog=null;__os.renderer.render(__os.scene,__os.camera);__os.scene.fog=fog},[x,z,yaw,pitch,y]);
await page.screenshot({timeout:120000,path:`${out}/${name}.png`});
}
const routes=await page.evaluate(()=>{
 const results=[];
 function walk(name,points){__os.teleport(points[0][0],points[0][1]);const startYaw=__os.stats().yaw;let failure=null;
 for(const [x,z]of points.slice(1)){let n=0;while(Math.hypot(x-__os.player.pos.x,z-__os.player.pos.z)>.07&&n++<2200){const door=__os.doors.nearest(__os.player);if(door?.state==='closed'){__os.pause(false);__os.act('act');__os.pause(true);__os.sim(6);}const p=__os.player.pos,dx=x-p.x,dz=z-p.z,len=Math.hypot(dx,dz);const before=p.clone();__os.step(dx/len*Math.min(.09,len),dz/len*Math.min(.09,len));if(p.distanceTo(before)<.00001){failure={target:[x,z],at:p.toArray()};break;}}if(failure||n>=2200){failure??={timeout:true,target:[x,z]};break;}}
 results.push({name,passed:!failure,failure,yawUnchanged:startYaw===__os.stats().yaw});
 }
 walk('street-villa-terrace-return',[[0,52],[0,-32],[2,-34],[2,-44],[0,-44],[0,-51],[0,-44],[2,-44],[2,-34],[0,-32],[0,52]]);
 walk('bedroom-bathroom-return',[[0,-32],[2,-35],[2,-38],[7.2,-38],[7.2,-41],[7.2,-38],[2,-38],[2,-35],[0,-32]]);
 walk('both-streets-loop',[[0,39],[-43,39],[-43,-19],[43,-19],[43,39],[0,39]]);
 walk('contact-house-garden-return',[[30,-28],[30,-37],[30,-46],[30,-53],[30,-46],[30,-37],[30,-28]]);
 walk('park-entry',[[58,-19],[58,-67],[0,-67],[0,-83],[28,-83]]);
 walk('expanded-street-loop',[[0,86],[-58,86],[-58,-19],[58,-19],[58,86],[0,86]]);
 walk('casino-interior-entry',[[-86,-20],[-86,-29],[-89.4,-29],[-89.4,-40],[-89.4,-29],[-86,-29],[-86,-20]]);
 walk('club-interior-entry',[[-86,36],[-86,27],[-86,20],[-86,12],[-86,20],[-86,27],[-86,36]]);
 walk('police-interior-entry',[[86,-20],[86,-29],[86,-36],[86,-29],[86,-20]]);
 walk('hospital-through-corridor',[[86,39],[86,30],[86,20],[86,0],[86,-4]]);
 walk('boxing-arena-entry', [[-86,86],[-86,75],[-86,70],[-86,63],[-86,70],[-86,75],[-86,86]]);
 walk('octagon-arena-entry', [[86,86],[86,75],[86,70],[86,63],[86,70],[86,75],[86,86]]);
 for(const f of __os.LEVEL.facilities){walk(f.id+'-front-door',[[f.x,f.z+22],[f.x,f.z+13]]);}
 walk('garage-court',[[0,-19],[-24,-19],[-24,-30],[-24,-19],[0,-19]]);
 __os.teleport(0,95);__os.step(0,6);results.push({name:'closed-city-gate',passed:__os.player.pos.z<98});
 __os.teleport(43,-55);__os.step(0,-6);results.push({name:'closed-forest-gate',passed:__os.player.pos.z>-58});
 return results;
});
const report=await page.evaluate(()=>({stats:__os.stats(),vegetation:__os.vegetation,homeCount:__os.LEVEL.houses.length+2,models:__os.imported.map(o=>({name:o.name,position:o.position.toArray(),bounds:o.userData.collisionBounds}))}));report.errors=errors;report.routes=routes;
fs.writeFileSync(`${out}/report.json`,JSON.stringify(report,null,2));console.log(JSON.stringify(report));await browser.close();server.kill();if(errors.length||routes.some(r=>!r.passed))process.exitCode=1;
})().catch(e=>{console.error(e);server.kill();process.exit(1)});
