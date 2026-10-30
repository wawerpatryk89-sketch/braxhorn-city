import {webkit,chromium} from 'playwright';
import fs from 'node:fs';
const url='https://braxhorn-city-i44hthxgl-wawerpatryk89-1409.vercel.app';
fs.mkdirSync('ios-waterfront-review',{recursive:true});
const report={url,sourceCommit:'62d5484d8050c59c4fcbcc4b4111c41d0d2d29ed',physicalIPhoneValidated:false,tests:[]};
for(const [name,engine] of [['webkit',webkit],['chromium',chromium]]){
 let browser;
 try{
  browser=await engine.launch({headless:true,args:name==='chromium'?['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']:[]});
  const page=await browser.newPage({viewport:{width:932,height:430},isMobile:true,hasTouch:true});
  const errors=[];page.on('pageerror',e=>errors.push(String(e)));page.on('crash',()=>errors.push('PAGE_CRASH'));
  await page.goto(url,{waitUntil:'domcontentloaded',timeout:90000});
  await page.screenshot({path:'ios-waterfront-review/'+name+'-entry.png'});
  const info=await page.evaluate(()=>({title:document.title,hasStart:!!document.querySelector('#start'),webgl2:!!document.createElement('canvas').getContext('webgl2')}));
  const response=await page.request.get(url+'/release.json');
  const raw=await response.text();const contentType=response.headers()['content-type']||'';const manifest=contentType.includes('application/json')?JSON.parse(raw):{};const blocked=!contentType.includes('application/json');
  report.tests.push({engine:name,...info,manifestSource:manifest.sourceCommit,manifestOk:manifest.sourceCommit===report.sourceCommit,blocked,contentType,httpStatus:response.status(),responseUrl:response.url(),responseSample:blocked?raw.slice(0,160):undefined,errors});
 }catch(e){report.tests.push({engine:name,error:String(e)});}finally{if(browser)await browser.close();}
}
fs.writeFileSync('ios-waterfront-review/report.json',JSON.stringify(report,null,2));
if(report.tests.some(t=>!t.manifestOk||!t.hasStart||t.error))process.exitCode=1;
