// Headless play-test: load the game, run a scripted sequence of inputs, save screenshots.
import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const [script = 'basic', quality = 'high', w = '1280', h = '720', touch = '0'] = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--enable-webgl'] });
const ctx = await browser.newContext({ viewport: { width: +w, height: +h }, hasTouch: touch === '1', isMobile: touch === '1', deviceScaleFactor: 1 });
const page = await ctx.newPage();
const errors = [];
page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') { errors.push(m.text().slice(0, 300)); } });
page.on('pageerror', e => errors.push('PAGEERROR ' + e.message));
await page.addInitScript((q) => { try { localStorage.setItem('mg_quality', JSON.stringify(q)); } catch { } }, quality);
await page.route(/fonts\.(googleapis|gstatic)/, r => r.abort());
await page.route(/test\.html$/, async r => { const res = await r.fetch(); r.fulfill({ response: res, headers: { ...res.headers(), 'content-security-policy': "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; img-src 'self' data: blob:; connect-src 'self'; font-src https://fonts.gstatic.com" } }); });
const t0 = Date.now();
await page.goto(process.env.URL || 'http://127.0.0.1:8765/out/minotaur-gym.html');
await page.waitForSelector('#loader.done', { timeout: 240000 });
console.log('loaded in', (Date.now() - t0) / 1000, 's');
fs.mkdirSync('/home/claude/work/shots', { recursive: true });
const shot = async (name) => { await page.screenshot({ path: `/home/claude/work/shots/${name}.png`, timeout: 180000, animations: 'allow', caret: 'initial' }); console.log('shot', name); };
const hold = async (key, ms) => { await page.keyboard.down(key); await page.waitForTimeout(ms); await page.keyboard.up(key); };
const fps = async () => page.evaluate(() => new Promise(r => { let n = 0; const t = performance.now(); const f = () => { n++; if (performance.now() - t < 2000) requestAnimationFrame(f); else r(n / 2); }; requestAnimationFrame(f); }));
await page.waitForTimeout(1500);
console.log('fps', await fps());
await shot(script + '_0_start');
const steps = JSON.parse(fs.readFileSync('/home/claude/work/tools/scripts.json', 'utf8'))[script] || [];
let i = 1;
for (const s of steps) {
  if (s.hold) await hold(s.hold, s.ms);
  if (s.press) await page.keyboard.press(s.press);
  if (s.wait) await page.waitForTimeout(s.wait);
  if (s.eval) console.log('eval', JSON.stringify(await page.evaluate(s.eval)));
  if (s.drag) { await page.mouse.move(s.drag[0], s.drag[1]); await page.mouse.down(); await page.mouse.move(s.drag[2], s.drag[3], { steps: 8 }); await page.mouse.up(); }
  if (s.shot) await shot(`${script}_${i++}_${s.shot}`);
}
console.log('errors', JSON.stringify([...new Set(errors)].slice(0, 20), null, 1));
await browser.close();
