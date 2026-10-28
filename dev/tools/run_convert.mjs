import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const mode = process.argv[2] || 'info';
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--js-flags=--max-old-space-size=6000'] });
const page = await browser.newPage();
page.on('console', m => { if (m.type() === 'error') console.error('[page]', m.text().slice(0, 300)); });
page.on('pageerror', e => console.error('[pageerror]', e.message));
await page.route(/\.fbm\/.*\.(png|PNG)$/, r => r.abort());
await page.goto('http://127.0.0.1:8765/tools/convert.html');
await page.waitForFunction(() => window.ready);
const anims = fs.readdirSync('/home/claude/work/brax').filter(f => f.endsWith('.fbx'));
const r1 = await page.evaluate(() => loadModel('mino', '/minotaur/mino.fbx'));
const r2 = await page.evaluate(() => loadModel('brax', '/brax_inner/brax.fbx'));
console.log('MINO', JSON.stringify(r1, null, 1));
console.log('BRAX', JSON.stringify(r2, null, 1));
for (const f of anims) {
  const info = await page.evaluate(([n, u]) => loadClip(n, u), [f.replace('.fbx', ''), '/brax/' + encodeURIComponent(f)]);
  console.log(JSON.stringify(info));
}
if (mode === 'preview') {
  fs.mkdirSync('/home/claude/work/prev', { recursive: true });
  const jobs = JSON.parse(process.argv[3]);
  let i = 0;
  for (const [key, clip, times, yaw] of jobs) {
    const imgs = await page.evaluate(([k, c, t, y]) => preview(k, c, t, y), [key, clip, times, yaw || 0]);
    imgs.forEach((d, j) => fs.writeFileSync(`/home/claude/work/prev/p_${String(i).padStart(2, '0')}_${j}.jpg`, Buffer.from(d.split(',')[1], 'base64')));
    i++;
  }
}
await browser.close();
