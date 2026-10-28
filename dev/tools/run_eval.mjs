// node run_eval.mjs page.html script.js  -> evaluates script (async body) in the page, writes __img results
import { chromium } from '/opt/node22/lib/node_modules/playwright/index.mjs';
import fs from 'fs';
const [pageFile, scriptFile, outDir] = process.argv.slice(2);
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--js-flags=--max-old-space-size=6000'] });
const page = await browser.newPage();
page.on('pageerror', e => console.error('[pageerror]', e.message));
page.on('console', m => { if (m.type() === 'error' || m.type() === 'log') console.error('[page]', m.text().slice(0, 300)); });
await page.goto('http://127.0.0.1:8765/tools/' + pageFile); await page.waitForFunction(() => window.ready, null, { timeout: 60000 });
const res = await page.evaluate(`(async () => { ${fs.readFileSync(scriptFile, 'utf8')} })()`);
const dir = outDir || '/tmp/claude-0/-home-claude/95554dc2-b859-5b05-8dbb-eb3d5a35a497/scratchpad/ev';
fs.mkdirSync(dir, { recursive: true });
const walk = (v) => { if (v && typeof v === 'object') { if (v.__img) { fs.writeFileSync(dir + '/' + v.name, Buffer.from(v.__img.split(',')[1], 'base64')); return 'saved:' + v.name; } for (const k in v) v[k] = walk(v[k]); } return v; };
console.log(JSON.stringify(walk(res), null, 1));
await browser.close();
