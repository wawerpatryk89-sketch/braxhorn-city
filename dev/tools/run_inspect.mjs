import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
const urls = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=swiftshader', '--enable-unsafe-swiftshader', '--js-flags=--max-old-space-size=6000'] });
const page = await browser.newPage();
page.on('console', m => { if (m.type() === 'error' || m.type() === 'warning') console.error('[page]', m.text().slice(0, 300)); });
// block textures during inspection to save time
await page.route(/\.(png|PNG|jpg)$/, r => r.abort());
await page.goto('http://127.0.0.1:8765/tools/inspect.html');
await page.waitForFunction(() => window.ready);
for (const u of urls) {
  const t = Date.now();
  try {
    const res = await page.evaluate(u => window.inspect(u), u);
    console.log(JSON.stringify(res));
  } catch (e) { console.log(JSON.stringify({ url: u, error: String(e).slice(0, 500) })); }
  console.error('took', (Date.now() - t) / 1000, 's');
}
await browser.close();
