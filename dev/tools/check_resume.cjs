const {chromium}=require('playwright');const fs=require('fs'),cp=require('child_process'),assert=require('assert/strict');
const server=cp.spawn('python3',['-m','http.server','18770'],{stdio:'ignore'});let browser;
(async()=>{
 browser=await chromium.launch({executablePath:process.env.CHROME_PATH||'/tmp/browser/chrome-headless-shell-linux64/chrome-headless-shell',args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const page=await browser.newPage({viewport:{width:915,height:412},hasTouch:true});
 await page.addInitScript(()=>{if(!sessionStorage.getItem('resume_test_seeded')){sessionStorage.setItem('resume_test_seeded','yes');sessionStorage.setItem('bx_city_estate_v1',JSON.stringify({version:1,hero:'brax',explore:true,hp:110,money:654,bossIdx:1,kills:9,cryptOpen:true,yaw:.7,pitch:.25,dist:6,bat:false}));}});
 await page.goto('http://127.0.0.1:18770/public/?from=osiedle');
 await page.waitForFunction(()=>window.__mg?.G.chapter==='street'&&!__mg.G.loadingWorld,null,{timeout:180000});
 const resumed=await page.evaluate(()=>{__mg.pause(true);return{hero:__mg.G.hero.key,money:__mg.G.money,kills:__mg.G.kills,yaw:__mg.cam.yaw,checkpoint:sessionStorage.getItem('bx_city_estate_v1'),query:location.search,baked:__mg.baked()};});
 assert.equal(resumed.hero,'brax');assert.equal(resumed.money,654);assert.equal(resumed.kills,9);assert.equal(resumed.yaw,.7);assert.equal(resumed.checkpoint,null);assert.equal(resumed.query,'');assert.equal(resumed.baked,true);
 // A later refresh must never replay the previous district's balance.
 await page.evaluate(()=>localStorage.setItem('bx_money','777'));await page.reload();await page.waitForFunction(()=>window.__mg,null,{timeout:120000});
 const reloaded=await page.evaluate(()=>{__mg.pause(true);return{money:__mg.G.money,mode:__mg.G.mode,player:!!__mg.player};});
 assert.equal(reloaded.money,777);assert.equal(reloaded.mode,'title');assert.equal(reloaded.player,false);
 fs.writeFileSync('dev/review/integration/resume.json',JSON.stringify({passed:true,resumed,reloaded},null,2));console.log('Resume and non-replay PASS',JSON.stringify({resumed,reloaded}));
})().catch(e=>{console.error(e);process.exitCode=1}).finally(async()=>{if(browser)await browser.close();server.kill();});
