// Play-test harness for the hosted build. Usage: node play2.mjs <steps.json> [w h touch]
import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const [stepsFile, W = '1280', H = '720', touch = '0'] = process.argv.slice(2);
const steps = JSON.parse(fs.readFileSync(stepsFile, 'utf8'));
const OUT = '/tmp/claude-0/-home-claude/95554dc2-b859-5b05-8dbb-eb3d5a35a497/scratchpad/play';
fs.mkdirSync(OUT, { recursive: true });
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--autoplay-policy=no-user-gesture-required'] });
const ctx = await browser.newContext({ viewport: { width: +W, height: +H }, hasTouch: touch === '1', isMobile: touch === '1', deviceScaleFactor: 1 });
const page = await ctx.newPage();
const logs = [];
page.on('console', m => { const t = m.text(); if (m.type() === 'error' || m.type() === 'warning' || t.startsWith('[t]')) { logs.push(m.type() + ': ' + t.slice(0, 300)); } });
page.on('pageerror', e => logs.push('pageerror: ' + e.message));
const t0 = Date.now();
await page.goto('http://127.0.0.1:8765/dist/index.html' + (steps.query || ''));
for (const s of steps.steps) {
  try {
    if (s.wait) await page.waitForFunction(s.wait, null, { timeout: (s.timeout || 120) * 1000 });
    if (s.click) await page.click(s.click, { timeout: 20000 });
    if (s.pause !== undefined) await page.evaluate((v) => window.__mg.pause(v), s.pause);
    if (s.eval) { const r = await page.evaluate(s.eval); if (r !== undefined) logs.push('eval ' + JSON.stringify(r).slice(0, 600)); }
    if (s.sleep) await page.waitForTimeout(s.sleep);
    if (s.sim) await page.evaluate(([sec, ks]) => window.__mg.sim(sec, ks), [s.sim, s.keys || []]);
    if (s.shot) { await page.evaluate(() => { window.__mg.pause(true); window.__mg.render(); }); await page.screenshot({ timeout: 120000, path: `${OUT}/${s.shot}.jpg`, quality: 70, type: 'jpeg' }); if (s.unpause) await page.evaluate(() => window.__mg.pause(false)); logs.push('shot ' + s.shot + ' @' + ((Date.now() - t0) / 1000).toFixed(1) + 's'); }
  } catch (e) { logs.push('STEP FAIL ' + JSON.stringify(s).slice(0, 120) + ' -> ' + e.message.split('\n')[0]); if (s.must) break; }
}
console.log(logs.join('\n'));
await browser.close();
