import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';
const root=path.resolve('unity-preview'),out=path.resolve('tower-district-qa');
fs.mkdirSync(out,{recursive:true});
const release=JSON.parse(fs.readFileSync(path.join(root,'release.json'),'utf8'));
const checks=[],errors=[];
const server=http.createServer((req,res)=>{
 const u=new URL(req.url,'http://localhost');
 let file=path.resolve(root,'.'+decodeURIComponent(u.pathname));
 if(file!==root&&!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return}
 if(fs.existsSync(file)&&fs.statSync(file).isDirectory())file=path.join(file,'index.html');
 if(!fs.existsSync(file)){res.writeHead(404);res.end();return}
 const types={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm'};
 res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');
 fs.createReadStream(file,{highWaterMark:65536}).pipe(res);
});
await new Promise(resolve=>server.listen(8978,'127.0.0.1',resolve));
let browser,context,video;
const check=(name,passed,data)=>{checks.push({name,passed:!!passed,data});if(!passed)throw Error(name+' '+JSON.stringify(data));console.log('PASS',name)};
try{
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:932,height:430},isMobile:true,hasTouch:true,recordVideo:{dir:out,size:{width:932,height:430}}});
 const page=await context.newPage();video=page.video();
 page.on('pageerror',e=>errors.push('PAGE '+e.message));page.on('crash',()=>errors.push('PAGE_CRASH'));
 await page.goto('http://127.0.0.1:8978/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:360000});
 check('Actual Unity player ready',await page.evaluate(()=>window.BraxhornState?.ready),await page.evaluate(()=>window.BraxhornError));
 const send=(m,arg)=>page.evaluate(([a,b])=>window.BraxSend(a,b),[m,arg]);
 const ids=['apothecary','corner_store','restaurant','hotel','estate_corner','gallery','apartments_1','estate_apartments_2'];
 for(let i=0;i<ids.length;i++){
  const id='R2_OFFICE_'+ids[i];
  await send('TravelPoint',id);
  await page.waitForFunction(id=>window.BraxhornState?.activePoint===id&&window.BraxhornState?.grounded,id,{timeout:45000});
  await page.waitForTimeout(550);
  const state=await page.evaluate(()=>window.BraxhornState);
  check('Real office scene anchor '+id,state.area==='city'&&state.grounded&&state.ready,{x:state.x,y:state.y,z:state.z});
  await page.screenshot({path:path.join(out,String(i+1).padStart(2,'0')+'-office-'+ids[i]+'.png')});
 }
 check('All eight distinct office foyer points visible',checks.filter(x=>x.name.startsWith('Real office scene anchor')).length===8,checks.length);
}catch(e){errors.push(String(e));process.exitCode=1}
finally{
 if(context)await context.close();
 if(video)try{
  const raw=await video.path();
  const dest=path.join(out,'braxhorn-eight-real-offices.mp4');
  const x=spawnSync('ffmpeg',['-y','-loglevel','error','-i',raw,'-c:v','libx264','-preset','veryfast','-crf','25','-pix_fmt','yuv420p','-movflags','+faststart',dest],{encoding:'utf8',timeout:120000});
  if(x.status!==0||!fs.existsSync(dest)){errors.push('MP4 conversion failed '+x.stderr);process.exitCode=1}
  else fs.unlinkSync(raw);
 }catch(e){errors.push('Video failure '+String(e));process.exitCode=1}
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({sourceCommit:release.sourceCommit,checks,errors,physicalPhoneValidated:false,visualAccepted:false,walkableEntrancesVerified:false,videoAudioCaptured:false},null,2));
 if(browser)await browser.close();
 server.close();
}
