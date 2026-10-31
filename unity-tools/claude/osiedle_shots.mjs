// Zdjęcia z prawdziwego playera Unity (osiedle) w CI: park, osiedle B, płot, punkty podróży.
// node unity-tools/claude/osiedle_shots.mjs   (serwuje ./unity-preview na 127.0.0.1:8765)
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root = path.resolve('unity-preview'), out = path.resolve('osiedle-shots');
fs.mkdirSync(out, {recursive: true});
const log = [];
const server = http.createServer((req, res) => {
  let f = path.resolve(root, '.' + decodeURIComponent(new URL(req.url, 'http://localhost').pathname));
  if (f !== root && !f.startsWith(root + path.sep)) { res.writeHead(403); res.end(); return; }
  if (fs.existsSync(f) && fs.statSync(f).isDirectory()) f = path.join(f, 'index.html');
  if (!fs.existsSync(f)) { res.writeHead(404); res.end(); return; }
  const types = {'.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg', '.mp3': 'audio/mpeg'};
  res.setHeader('Content-Type', types[path.extname(f)] || 'application/octet-stream');
  fs.createReadStream(f).pipe(res);
});
await new Promise(r => server.listen(8765, '127.0.0.1', r));

const VIEWS = [
  ['01_brama_do_parku', '-20,.08,-96,0,12,9'],
  ['02_aleja_parku', '-20,.08,-110,0,10,8'],
  ['03_fontanna', '-12,.08,-131,90,14,9'],
  ['04_park_panorama', '-62,.08,-126,270,10,10'],
  ['05_osiedle_b_z_parku', '-20,.1,-152,0,14,10'],
  ['06_ulica_b', '30,.1,-193,270,10,9'],
  ['07_wille_polnoc', '-60,.15,-188,180,14,10'],
  ['08_wille_poludnie', '50,.15,-198,0,14,10'],
  ['09_ogrody_basen', '-50,.1,-229,180,14,10'],
  ['10_nowy_plot_A', '30,.08,-98,0,10,9'],
  ['11_skrzyzowanie', '-12,.1,-186,315,16,11'],
  ['18_sklepy_b', '18,.1,-190,180,14,11'],
  ['19_kort_tenisowy', '19,.15,-198,0,26,12'],
  ['20_plac_zabaw', '-66,.08,-147,135,18,11'],
  ['22_kasyno', '86,.1,-18,0,12,12'],
  ['23_komisariat', '-86,.1,-18,0,12,12'],
  ['24_szpital', '-86,.1,38,0,12,12'],
  ['25_hala_walk', '-86,.1,88,0,12,12'],
  ['21_A_dom_07_nowa_willa', '10,.1,59,270,10,11'],
  ['12_A_ulica_glowna', '0,.08,20,180,10,10'],
  ['13_A_dom_01', '12,.1,27,270,10,10'],
  ['14_A_dom_07', '12,.1,59,270,10,10'],
  ['15_A_plot_dzialki', '16,.1,40,180,14,10'],
  ['16_A_bulwar', '-30,.1,39,270,10,9'],
  ['17_A_willa', '0,.08,-22,0,12,10'],
];
const TRAVEL = ['park_centralny', 'fontanna', 'osiedle_b', 'osiedle_b_ulica', 'willa_b', 'sklepy_b', 'kort_tenisowy'];

async function session(fn) {
  const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--disable-dev-shm-usage', '--disable-gpu-watchdog']});
  try {
    const page = await (await browser.newContext({viewport: {width: 1280, height: 576}, deviceScaleFactor: 1})).newPage();
    page.on('console', m => { const t = m.text(); if (/error|exception|BRAXHORN/i.test(t)) log.push(m.type() + ': ' + t.slice(0, 300)); });
    page.on('pageerror', e => log.push('PAGEERROR ' + e.message));
    await page.goto('http://127.0.0.1:8765/?qa=1', {waitUntil: 'domcontentloaded'});
    await page.click('#start');
    await page.waitForFunction(() => window.BraxhornState?.ready || window.BraxhornError, null, {timeout: 600000});
    if (await page.evaluate(() => !!window.BraxhornError)) throw Error('Unity startup failed: ' + await page.evaluate(() => String(window.BraxhornError)));
    await page.waitForTimeout(4000);
    await page.addStyleTag({content: '#hud,#action,#hint,#hits,.menu,#pause,#buttons,#joystick,#joylabel,#lookzone,#citymap,#places{display:none!important}'});
    await fn(page);
  } finally { await browser.close().catch(() => {}); }
}
const send = (page, m, a) => page.evaluate(([m, a]) => window.BraxSend(m, a), [m, a]);
async function settle(page) {
  const f0 = await page.evaluate(() => window.BraxhornState?.frames || 0);
  await page.waitForFunction(f => (window.BraxhornState?.frames || 0) >= f + 8, f0, {timeout: 240000}).catch(() => log.push('frames timeout'));
  await page.waitForTimeout(1500);
}
const results = [];
for (let i = 0; i < VIEWS.length; i += 4) {
  const chunk = VIEWS.slice(i, i + 4);
  for (let attempt = 0; attempt < 2; attempt++) {
    try {
      await session(async page => {
        if (i === 0) fs.writeFileSync(path.join(out, 'state-start.json'), JSON.stringify(await page.evaluate(() => window.BraxhornState), null, 1));
        for (const [name, view] of chunk) {
          if (fs.existsSync(path.join(out, name + '.png'))) continue;
          await send(page, 'ReviewView', view); await settle(page);
          await page.screenshot({path: path.join(out, name + '.png'), timeout: 300000});
          const s = await page.evaluate(() => window.BraxhornState);
          results.push({name, view, x: s.x, y: s.y, z: s.z, grounded: s.grounded});
        }
        if (i === 0) for (const id of TRAVEL) {
          await send(page, 'TravelPoint', id); await settle(page);
          const s = await page.evaluate(() => window.BraxhornState);
          results.push({travel: id, x: s.x, y: s.y, z: s.z, grounded: s.grounded, activePoint: s.activePoint});
        }
      });
      break;
    } catch (e) { log.push('chunk ' + i + ' attempt ' + attempt + ': ' + e.message); }
  }
}
fs.writeFileSync(path.join(out, 'results.json'), JSON.stringify({results, log}, null, 1));
server.close();
console.log('OSIEDLE_SHOTS', results.length, 'log', log.length);
