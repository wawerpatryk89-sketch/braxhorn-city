// Odpala prawdziwy build WebGL w Chromium (SwiftShader) i robi zdjęcia z kamery QA.
// użycie: node shoot.cjs <folder playera> <out dir> "<nazwa>|x,y,z,yaw,pitch,dist" ...
const { chromium } = require('/opt/npm-tools/node_modules/playwright');
const http = require('http'), fs = require('fs'), path = require('path');
const root = path.resolve(process.argv[2]), out = path.resolve(process.argv[3]); fs.mkdirSync(out, { recursive: true });
const shots = process.argv.slice(4).map(s => { const [name, view] = s.split('|'); return { name, view }; });
const mime = { '.html': 'text/html', '.js': 'application/javascript', '.json': 'application/json', '.wasm': 'application/wasm', '.webp': 'image/webp', '.png': 'image/png', '.css': 'text/css' };
const server = http.createServer((req, res) => {
  let p = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  if (p === '/unity-gym') p = '/'; else if (p.startsWith('/unity-gym/')) p = p.slice('/unity-gym'.length);
  let f = path.resolve(root, '.' + p);
  try { if (fs.statSync(f).isDirectory()) f = path.join(f, 'index.html'); const st = fs.statSync(f);
    res.setHeader('Content-Type', mime[path.extname(f)] || 'application/octet-stream'); res.setHeader('Content-Length', st.size);
    fs.createReadStream(f).pipe(res); } catch { res.writeHead(404); res.end(); }
});
(async () => {
  await new Promise(r => server.listen(8765, r));
  const b = await chromium.launch({ args: ['--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl', '--no-sandbox', '--disable-dev-shm-usage', '--js-flags=--max-old-space-size=6144', '--renderer-process-limit=1'] });
  const page = await b.newPage({ viewport: { width: 1280, height: 720 } });
  const errs = []; page.on('console', m => { if (m.type() === 'error') errs.push(m.text().slice(0, 200)); });
  page.on('pageerror', e => errs.push('PAGEERROR ' + String(e).slice(0, 200)));
  await page.goto('http://127.0.0.1:8765/unity-gym/?qa=1', { waitUntil: 'load', timeout: 120000 });
  await page.click('#start');
  const t0 = Date.now(); let ready = false;
  while (Date.now() - t0 < 20 * 60 * 1000) {
    await page.waitForTimeout(5000);
    const st = await page.evaluate(() => ({ u: !!window.unityInstance, s: window.BraxhornState ? { ready: window.BraxhornState.ready, area: window.BraxhornState.area, x: window.BraxhornState.x, frames: window.BraxhornState.frames } : null, t: (document.querySelector('#loadtext') || {}).textContent, stats: window.BraxCityLoadingStats && { r: window.BraxCityLoadingStats.receivedBytes, d: window.BraxCityLoadingStats.decodedBytes } }));
    process.stdout.write(JSON.stringify(st) + '\n');
    if (st.s && st.s.frames > 30) { ready = true; break; }
    if (errs.length > 5) break;
  }
  console.log('READY', ready, 'errors:', errs.slice(0, 5).join(' | '));
  if (ready) {
    await page.waitForTimeout(8000);
    await page.screenshot({ path: path.join(out, '00-start.jpg'), quality: 80, timeout: 300000 });
    for (const s of shots) {
      await page.evaluate(v => window.BraxSend('ReviewView', v), s.view);
      await page.waitForTimeout(6000);
      await page.screenshot({ path: path.join(out, s.name + '.jpg'), quality: 82, timeout: 300000 });
      const st = await page.evaluate(() => { const s = window.BraxhornState || {}; return { x: s.x, y: s.y, z: s.z, yaw: s.yaw, area: s.area, context: s.context }; });
      console.log('SHOT', s.name, JSON.stringify(st));
    }
  }
  await b.close(); server.close();
})().catch(e => { console.error('FATAL', e); process.exit(1); });
