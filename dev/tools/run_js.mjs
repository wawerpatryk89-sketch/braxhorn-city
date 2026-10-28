// Usage: node run_js.mjs <page.html> <script.js> [loadAll]
// Loads the page, models and all clips, then evaluates script.js (async body) and prints its JSON result.
// Images returned as {__img: dataURL, name} are written to /home/claude/work/prev/<name>.
import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const [pageFile, scriptFile] = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--js-flags=--max-old-space-size=6000'] });
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
page.on('console', m => { if (['error', 'log', 'warning'].includes(m.type())) console.error('[page]', m.text().slice(0, 400)); });
page.on('pageerror', e => console.error('[pageerror]', e.message));
await page.route(/\.fbm\/.*\.(png|PNG)$/, r => r.abort());
await page.goto('http://127.0.0.1:8765/tools/' + pageFile);
await page.waitForFunction(() => window.ready, null, { timeout: 120000 });
const anims = fs.readdirSync('/home/claude/work/brax').filter(f => f.endsWith('.fbx'));
await page.evaluate(async (anims) => {
  await loadModel('mino', '/minotaur/mino.fbx');
  await loadModel('brax', '/brax_inner/brax.fbx');
  for (const f of anims) await loadClip(f.replace('.fbx', ''), '/brax/' + encodeURIComponent(f));
}, anims);
const body = fs.readFileSync(scriptFile, 'utf8');
const res = await page.evaluate(`(async () => { ${body} })()`);
fs.mkdirSync('/home/claude/work/prev', { recursive: true });
const walk = (v) => {
  if (v && typeof v === 'object') {
    if (v.__img) { fs.writeFileSync('/home/claude/work/prev/' + v.name, Buffer.from(v.__img.split(',')[1], 'base64')); return 'saved:' + v.name; }
    if (v.__bin) { fs.writeFileSync(v.path, Buffer.from(v.__bin, 'base64')); return 'saved:' + v.path; }
    for (const k in v) v[k] = walk(v[k]);
  }
  return v;
};
console.log(JSON.stringify(walk(res), null, 1));
await browser.close();
