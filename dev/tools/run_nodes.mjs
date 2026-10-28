import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
const [url, depth] = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--js-flags=--max-old-space-size=7000'] });
const page = await browser.newPage();
await page.route(/\.(png|PNG|jpe?g)$/, r => r.abort());
await page.goto('http://127.0.0.1:8765/tools/nodes.html'); await page.waitForFunction(() => window.ready);
console.log(await page.evaluate(([u, d]) => window.nodes(u, +d), [url, depth || 2]));
await browser.close();
