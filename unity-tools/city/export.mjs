import fs from 'node:fs/promises';import path from 'node:path';import {chromium} from 'playwright';
const out=path.resolve('city-export');await fs.mkdir(out,{recursive:true});
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const logs=[],results=[];
try{
 for(const area of ['city','estate']){
  const context=await browser.newContext({viewport:{width:1280,height:720},acceptDownloads:true});
  await context.addInitScript(()=>{localStorage.setItem('bx_explore','true');localStorage.setItem('bx_explore2','true');localStorage.removeItem('bx_estate_doors');});
  const page=await context.newPage(),downloads=[];
  page.on('console',m=>{if(m.type()==='error'||m.type()==='warning')logs.push(area+' '+m.type()+': '+m.text())});page.on('pageerror',e=>logs.push(area+' PAGEERROR: '+e.message));
  page.on('download',d=>{const name=d.suggestedFilename();if(!/^[a-zA-Z0-9_.-]+$/.test(name))throw Error('Unexpected download path');downloads.push(d.saveAs(path.join(out,name)))});
  await page.goto('http://127.0.0.1:8765/unity-tools/city/export.html?area='+area,{waitUntil:'domcontentloaded',timeout:120000});
  await page.waitForFunction(()=>window.exportReady||window.exportError,null,{timeout:480000});
  const error=await page.evaluate(()=>window.exportError);if(error)throw Error(error);
  const result=await page.evaluate(()=>window.exportArea());await page.waitForTimeout(1000);await Promise.all(downloads);
  const stat=await fs.stat(path.join(out,'braxhorn-'+area+'.glb'));if(stat.size!==result.bytes)throw Error('Truncated original '+area+' export');
  result.sourceCommit=process.env.GITHUB_SHA;results.push(result);console.log('ORIGINAL_AREA_EXPORTED',area,result.meshCount,result.triangles,result.bytes);
  await page.screenshot({path:path.join(out,area+'-source.png'),timeout:90000});
  await context.close();
 }
 await fs.writeFile(path.join(out,'export-summary.json'),JSON.stringify({sourceCommit:process.env.GITHUB_SHA,complete:true,areas:results.map(r=>({area:r.area,meshes:r.meshCount,triangles:r.triangles,bytes:r.bytes,doors:r.doors.length,colliders:r.collision.boxes.length,points:r.points.length,omitted:r.omitted}))},null,2));
}finally{await fs.writeFile(path.join(out,'browser.log'),logs.join('\n'));await browser.close();}
