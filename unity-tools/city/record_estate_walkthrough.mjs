import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';
import {spawnSync} from 'node:child_process';
const root=path.resolve('unity-preview'),out=path.resolve('estate-video-review');
fs.mkdirSync(out,{recursive:true});
const logs=[],stops=[];
const types={'.html':'text/html','.js':'application/javascript','.json':'application/json','.wasm':'application/wasm','.webp':'image/webp','.png':'image/png','.gz':'application/octet-stream'};
const server=http.createServer((req,res)=>{
 let f=path.resolve(root,'.'+new URL(req.url,'http://localhost').pathname);
 if(!f.startsWith(root+path.sep)&&f!==root){res.writeHead(403);res.end();return}
 if(fs.existsSync(f)&&fs.statSync(f).isDirectory())f=path.join(f,'index.html');
 if(!fs.existsSync(f)){res.writeHead(404);res.end();return}
 res.setHeader('Content-Type',types[path.extname(f)]||'application/octet-stream');
 fs.createReadStream(f).pipe(res);
});
await new Promise(ok=>server.listen(8765,'127.0.0.1',ok));
let browser,context,page,raw,readyOffset=0;
try{
 browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
 context=await browser.newContext({viewport:{width:960,height:540},deviceScaleFactor:1,hasTouch:true,isMobile:true,recordVideo:{dir:out,size:{width:960,height:540}}});
 page=await context.newPage();const video=page.video();const start=Date.now();
 page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
 await page.goto('http://127.0.0.1:8765/?qa=1',{waitUntil:'domcontentloaded'});
 await page.locator('#start').click();
 await page.waitForFunction(()=>window.BraxhornState?.ready||window.BraxhornError,null,{timeout:480000,polling:100});
 const initial=await page.evaluate(()=>({state:window.BraxhornState,error:window.BraxhornError}));
 if(initial.error||!initial.state?.ready||initial.state?.area!=='estate'||initial.state?.cityMeshes!==553)throw Error('Original Unity estate not ready: '+JSON.stringify(initial).slice(0,400));
 readyOffset=Math.max(0,(Date.now()-start)/1000-1.5);
 await page.waitForFunction(()=>window.BraxhornState?.grounded,null,{timeout:90000});
 const send=(method,arg='')=>page.evaluate(([m,a])=>window.BraxSend(m,a),[method,arg]);
 const wait=ms=>page.waitForTimeout(ms);
 const sites=[
  ['estate_entry','01-wejscie',0,0.9],
  ['villa_front','02-dom-elewacja',15,1.5],
  ['villa_living','03-wnetrze-domu',35,0.9],
  ['villa_terrace','04-taras',190,1.1],
  ['garages','05-garaze',65,0.9],
  ['park','06-park-zielen',35,1.4],
  ['court','07-plac',135,1.0],
  ['casino','08-kasyno',15,1.0],
  ['club','09-klub',120,1.0],
  ['police','10-komisariat',35,1.0],
  ['hospital','11-szpital',35,1.0],
  ['boxing','12-sala-bokserska',60,1.0],
  ['arena','13-arena',110,1.0],
  ['estate_entry','14-podsumowanie-osiedla',180,1.0]
 ];
 for(const [id,name,yaw,walk] of sites){
  await send('MoveInput','0,0');
  await send('TravelPoint',id);
  await page.waitForFunction(q=>window.BraxhornState?.activePoint===q&&window.BraxhornState?.grounded,id,{timeout:45000,polling:100});
  const p=await page.evaluate(()=>window.BraxhornState);
  if(p.area!=='estate'||p.cityMeshes!==553)throw Error('Wrong area at '+id);
  await send('ReviewView',p.x+','+p.y+','+p.z+','+yaw+',19,6.5');
  await wait(700);
  await page.screenshot({path:path.join(out,name+'.png'),timeout:60000});
  await wait(1300);
  await send('MoveInput','0,0.65');
  await wait(Math.round(walk*1000));
  await send('MoveInput','0,0');
  await send('LookInput','20,0');
  await wait(650);
  const st=await page.evaluate(()=>window.BraxhornState);
  stops.push({id,label:name,x:st.x,y:st.y,z:st.z,area:st.area,animation:st.animation});
  console.log('RECORDED_ORIGINAL_UNITY',id,'BRAx',st.animation,'x',st.x,'z',st.z);
 }
 await send('MoveInput','0,0');
 await wait(800);
 await page.close();
 raw=path.join(out,'real-unity-capture.webm');await video.saveAs(raw);
 await context.close();await browser.close();browser=null;
 const output=path.join(out,'braxhorn-osiedle-przed-remontem.mp4');
 const ff=spawnSync('ffmpeg',['-hide_banner','-loglevel','error','-y','-ss',String(readyOffset),'-i',raw,'-an','-vf','fps=20,scale=960:540:flags=lanczos,format=yuv420p','-c:v','libx264','-preset','veryfast','-crf','23','-movflags','+faststart',output],{encoding:'utf8',timeout:180000});
 if(ff.status!==0)throw Error('ffmpeg '+ff.stderr);
 const probe=spawnSync('ffprobe',['-v','error','-show_entries','format=duration,size','-of','json',output],{encoding:'utf8'});
 if(probe.status!==0)throw Error('ffprobe '+probe.stderr);
 const stats=JSON.parse(probe.stdout).format;
 if(Number(stats.duration)<25||Number(stats.size)<150000||Number(stats.size)>90000000)throw Error('Invalid recording '+JSON.stringify(stats));
 const errors=logs.filter(x=>/PAGEERROR|NullReferenceException|MissingReferenceException|abort\(|memory access out of bounds/.test(x));
 if(errors.length)throw Error('Unity runtime error '+errors.join(' | ').slice(0,400));
 const report={passed:true,kind:'REAL UNITY WEBGL SCREEN CAPTURE, not generated images',originalCityPlayer:'BraxGymPlayer',scene:'BraxhornEstate',meshCount:553,stops,durationSeconds:Number(stats.duration),bytes:Number(stats.size),capture:'Chromium WebGL2 SwiftShader, not iPhone',visualAcceptance:'pending user review',sourceBuild:process.env.SOURCE_BUILD_SHA||'140407203586a6c150faf6355ebd4c1a2faa55c7'};
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify(report,null,2));
 console.log('REAL_UNITY_VIDEO_PASS',JSON.stringify({duration:stats.duration,bytes:stats.size,stops:stops.length}));
}catch(e){console.error(e);fs.writeFileSync(path.join(out,'error.txt'),String(e)+'\n'+logs.slice(-35).join('\n'));process.exitCode=1;
 try{if(page&&!page.isClosed())await page.close()}catch{}
 try{await context?.close()}catch{}
 try{await browser?.close()}catch{}
}finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));server.close()}
