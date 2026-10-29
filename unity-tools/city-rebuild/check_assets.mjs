import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root=path.resolve('.'),out=path.resolve('city-asset-visual-review');fs.mkdirSync(out,{recursive:true});
const server=http.createServer((req,res)=>{const file=path.resolve(root,'.'+new URL(req.url,'http://local').pathname);if(!file.startsWith(root+path.sep)||!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404);res.end();return;}res.setHeader('Content-Type',({'.html':'text/html','.js':'application/javascript','.gltf':'model/gltf+json','.bin':'application/octet-stream','.jpg':'image/jpeg','.png':'image/png'})[path.extname(file)]||'application/octet-stream');res.setHeader('Content-Length',fs.statSync(file).size);fs.createReadStream(file).pipe(res);});
await new Promise(r=>server.listen(8770,'127.0.0.1',r));
const browser=await chromium.launch({args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader','--ignore-gpu-blocklist','--disable-dev-shm-usage']});
const page=await browser.newPage({viewport:{width:1280,height:800}});const logs=[],reports=[];
page.on('console',m=>logs.push(m.type()+': '+m.text()));page.on('pageerror',e=>logs.push('PAGEERROR '+e.message));
const urban='modular_urban_apartments_facade',factory='modular_factory_facade';
const part=(name,y=0)=>({name,y});
const cases=[
 {id:'urban-large-window',asset:urban,label:'Urban: duże okno + odpowiadająca ściana',parts:[part('wall_window_centered_large_01'),part('window_centered_large_01')]},
 {id:'urban-small-window',asset:urban,label:'Urban: małe okno 02 + odpowiadająca ściana',parts:[part('wall_window_centered_small_02'),part('window_centered_small_02')]},
 {id:'urban-large-door',asset:urban,label:'Urban: duże drzwi + odpowiadająca ściana',parts:[part('wall_door_centered_large_01'),part('door_centered_large_01')]},
 {id:'urban-small-door',asset:urban,label:'Urban: małe drzwi 02 + odpowiadająca ściana',parts:[part('wall_door_centered_small_02'),part('door_centered_small_02')]},
 {id:'urban-two-floors',asset:urban,label:'Urban: test dwóch kondygnacji 3 m + gzyms i korona',parts:[part('wall_door_centered_large_01'),part('door_centered_large_01'),part('wall_window_centered_large_01',3),part('window_centered_large_01',3),part('cornice_standard_standard_01',3),part('crown_standard_standard_01',6)]},
 {id:'factory-window',asset:factory,label:'Factory: duże okno + odpowiadająca ściana',parts:[part('wall_window_centered_large_01'),part('window_centered_large_01')]},
];
try{
 await page.goto('http://127.0.0.1:8770/unity-tools/city-rebuild/asset-view.html');await page.waitForFunction(()=>window.assetViewerReady,{},{timeout:120000});
 for(const item of cases){for(const view of ['front','oblique']){const config={...item,view};const report=await page.evaluate(config=>window.inspectAsset(config),config);await page.screenshot({path:path.join(out,item.id+'-'+view+'.jpg'),type:'jpeg',quality:90});reports.push(report);console.log('SOURCE_ASSET_RENDERED',item.id,view,JSON.stringify(report.bounds));}}
 fs.writeFileSync(path.join(out,'report.json'),JSON.stringify({scope:'Source asset inspection; not a game preview or Unity acceptance',reports,logs},null,2));
}catch(error){fs.writeFileSync(path.join(out,'failure.json'),JSON.stringify({error:String(error),reports,logs},null,2));throw error;}
finally{fs.writeFileSync(path.join(out,'browser.log'),logs.join('\n'));await browser.close();server.close();}
