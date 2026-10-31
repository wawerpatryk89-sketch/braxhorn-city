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

// Ujęcie: 'x,y,z,yaw,pitch,dystans' (BraxGymPlayer.ReviewView): kamera = głowa gracza (y+1.65) + kierunek(yaw, pitch) × dystans,
// patrzy na gracza (czyli w stronę yaw+180°); pitch 7–57°, dystans 0.8–12 m; kolizja kamery zbliża ją do gracza.
// Kadry sprawdzone planerem (FOV pionowy 55°, 1280×576): viewplan/vp.py.
const VIEWS = [
  ['01_brama_do_parku', '-20,.08,-96,0,12,9'],
  ['02_aleja_parku', '-20,.08,-110,0,10,8'],
  ['03_fontanna', '-20,.08,-119.5,0,12,5.5'],
  ['04_park_panorama', '-62,.08,-126,270,10,10'],
  ['05_osiedle_b_z_parku', '-20,.1,-152,0,14,10'],
  ['06_ulica_b', '30,.1,-193,270,10,9'],
  ['07_wille_polnoc', '-66.8,.15,-188,180,10,12'],
  ['08_wille_poludnie', '50,.15,-198,0,14,10'],
  ['09_ogrody_basen', '-50,.1,-229,180,14,10'],
  ['11_skrzyzowanie', '-12,.1,-186,315,16,11'],
  ['18_sklepy_b', '18,.1,-190,180,14,11'],
  ['19_kort_tenisowy', '19,.15,-198,0,26,12'],
  ['20_plac_zabaw', '-72,.08,-135.5,0,9,7'],
  ['28_fontanna_z_bliska', '-12,.05,-126,90,7,3.5'],
  ['29_rabaty_aleja', '-47,.05,-119,0,8,4'],
  ['30_ogrod_willi_B', '-80,.1,-156,0,12,7'],
  ['31_dachy_B_z_gory', '-60,.1,-168,0,35,20'],
  ['32_A_ogrody', '8,.1,27,270,14,8'],
  ['33_park_drzewa', '-92,.08,-126,90,12,6'],
  ['26_trawa_z_bliska', '-44,.1,-114,20,9,5'],
  ['44_trawa_10m', '-99.5,.1,-158,180,18,10'],
  ['45_trawa_2m', '-99.5,.1,-160,200,35,2.2'],
  ['27_trawnik_z_gory', '-99.5,.1,-160,180,57,12'],
  ['37_silownia_plenerowa', '58,.05,-104.5,0,22,12'],
  ['38_silownia_A', '62,.1,-64,300,8,12'],
  ['39_silownia_A_srodek', '92,.1,-71.55,40,10,4'],
  ['40_nutrition_B', '16,.1,-177,210,9,3'],
  ['41_willa_sprzedaz_front', '-91.25,.1,-199,0,10,12'],
  ['42_willa_sprzedaz_salon', '-92,.1,-212,90,7,1.9'],
  ['43_willa_sprzedaz_kuchnia', '-90,.1,-210,270,7,1.9'],
  ['46_kawiarnia_wnetrze', '30.25,.1,-176,180,7,2.2'],
  ['47_market_wnetrze', '40.65,.1,-177.3,180,7,2'],
  ['48_butik_wnetrze', '-6.55,.1,-177,180,7,2'],
  ['49_shakebar_wnetrze', '5.65,.1,-176.3,180,7,2.2'],
  ['34_komisariat_sala', '-86,.1,-37,0,4,2.4'],
  ['35_komisariat_hol', '-86,.1,-36,180,5,2.2'],
  ['36_komisariat_cele', '-83,.1,-52,270,8,3'],
  ['22_kasyno', '86,.1,-18,0,12,12'],
  ['23_komisariat', '-86,.1,-18,0,12,12'],
  ['24_szpital', '-86,.1,38,0,12,12'],
  ['25_hala_walk', '-86,.1,88,0,12,12'],
  ['21_A_dom_07_nowa_willa', '10,.1,59,270,10,11'],
  ['12_A_ulica_glowna', '0,.08,20,180,10,10'],
  ['13_A_dom_01', '12,.1,27,270,10,10'],
  ['16_A_bulwar', '-30,.1,39,270,10,9'],
  ['17_A_willa', '0,.08,-22,0,12,10'],
];
const TRAVEL = ['park_centralny', 'fontanna', 'osiedle_b', 'osiedle_b_ulica', 'willa_b', 'sklepy_b', 'kort_tenisowy', 'silownia', 'nutrition_b', 'willa_na_sprzedaz_1', 'willa_na_sprzedaz_2', 'willa_na_sprzedaz_3'];

async function session(fn) {
  const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--disable-dev-shm-usage', '--disable-gpu-watchdog']});
  try {
    const page = await (await browser.newContext({viewport: {width: 1280, height: 576}, deviceScaleFactor: 1})).newPage();
    page.on('console', m => { const t = m.text();
      if (t.includes('BRAXHORN_KADR')) { lastKadr = t.slice(t.indexOf('BRAXHORN_KADR') + 14).split('\n')[0].trim(); return; }   // pomiar kadru co 1 s (BraxClaudeKadr)
      if (/error|exception|BRAXHORN/i.test(t)) log.push(m.type() + ': ' + t.slice(0, 300)); });
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
let lastKadr = null;
// pomiar kadru: po ustawieniu ujęcia czekamy na świeżą linię BRAXHORN_KADR (trójkąty, obiekty, materiały w kadrze)
async function kadr(page) {
  lastKadr = null;
  for (let t = 0; t < 120 && lastKadr === null; t++) await page.waitForTimeout(500);
  return lastKadr;
}
// Równoległe maszyny w CI: SHOT_PART z SHOT_PARTS bierze co n-te ujęcie (punkty podróży tylko część 0).
const PART = +(process.env.SHOT_PART || 0), PARTS = +(process.env.SHOT_PARTS || 1);
// SHOT_ONLY=44_trawa_10m,45_trawa_2m — tylko wybrane ujęcia (np. porównanie ze starym playerem), bez punktów podróży
const ONLY = (process.env.SHOT_ONLY || '').split(',').filter(Boolean);
const MINE = ONLY.length ? VIEWS.filter(v => ONLY.includes(v[0])) : VIEWS.filter((v, k) => k % PARTS === PART);
if (ONLY.length) TRAVEL.length = 0;
const save = () => fs.writeFileSync(path.join(out, `results-${PART}.json`), JSON.stringify({results, log}, null, 1));
for (let i = 0; i < MINE.length; i += 6) {
  const chunk = MINE.slice(i, i + 6);
  for (let attempt = 0; attempt < 2; attempt++) {
    try {
      await session(async page => {
        if (i === 0 && PART === 0) fs.writeFileSync(path.join(out, 'state-start.json'), JSON.stringify(await page.evaluate(() => window.BraxhornState), null, 1));
        for (const [name, view] of chunk) {
          if (fs.existsSync(path.join(out, name + '.png'))) continue;
          // zasłonięty kadr (kamera w koronie drzewa itp.) daje prawie jednolity obraz: PNG < 150 kB (normalne > 500 kB).
          // Wtedy do 3 prób z kamerą obróconą o ±40° / bliżej; zapisany zostaje pierwszy niezasłonięty, a w wynikach — użyty wariant.
          const [vx, vy, vz, vyaw, vp, vd] = view.split(',').map(Number);
          const variants = [view, [vx, vy, vz, vyaw + 40, vp, vd].join(','), [vx, vy, vz, vyaw - 40, vp, vd].join(','), [vx, vy, vz, vyaw, Math.max(vp, 20), Math.max(.8, vd * .55)].join(',')];
          let used = view, size = 0;
          for (const v of variants) {
            await send(page, 'ReviewView', v); await settle(page);
            const buf = await page.screenshot({path: path.join(out, name + '.tmp.png'), timeout: 300000});   // plik docelowy dopiero po wyborze wariantu
            used = v; size = buf.length;
            if (size >= 150000) break;
            log.push('zasłonięty kadr ' + name + ' (' + v + ', ' + size + ' B) — próbuję inny');
          }
          fs.renameSync(path.join(out, name + '.tmp.png'), path.join(out, name + '.png'));
          const k = await kadr(page);
          const s = await page.evaluate(() => window.BraxhornState);
          results.push({name, view, used, pngBytes: size, kadr: k, x: s.x, y: s.y, z: s.z, grounded: s.grounded});
        }
        if (i === 0 && PART === 0) for (const id of TRAVEL) {
          await send(page, 'TravelPoint', id); await settle(page);
          const s = await page.evaluate(() => window.BraxhornState);
          results.push({travel: id, x: s.x, y: s.y, z: s.z, grounded: s.grounded, activePoint: s.activePoint});
        }
      });
      break;
    } catch (e) { log.push('chunk ' + i + ' attempt ' + attempt + ': ' + e.message); }
  }
  save();
}
save();
server.close();
console.log('OSIEDLE_SHOTS', results.length, 'log', log.length);
