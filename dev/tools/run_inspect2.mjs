import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const files = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--js-flags=--max-old-space-size=7000'] });
const page = await browser.newPage();
page.on('pageerror', e => console.error('[pageerror]', e.message));
await page.goto('http://127.0.0.1:8765/tools/inspect_assets.html');
await page.waitForFunction(() => window.ready);
fs.mkdirSync('/home/claude/work/prev/assets', { recursive: true });
for (const f of files) {
  try {
    const res = await page.evaluate(u => window.inspectAsset(u), '/street_in/' + encodeURIComponent(f));
    const name = f.replace(/\W+/g, '_') + '.jpg';
    fs.writeFileSync('/home/claude/work/prev/assets/' + name, Buffer.from(res.img.split(',')[1], 'base64'));
    delete res.img; console.log(JSON.stringify(res));
  } catch (e) { console.log(JSON.stringify({ f, error: String(e).slice(0, 300) })); }
}
await browser.close();
