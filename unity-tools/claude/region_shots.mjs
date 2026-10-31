// Zdjęcia z prawdziwego playera R1 (miasto) w CI — ta sama metoda co osiedle (unity-tools/claude/osiedle_shots.mjs).
// node unity-tools/claude/region_shots.mjs   (serwuje ./unity-preview na 127.0.0.1:8765, zapisuje ./region-shots)
// Ujęcie: 'x,y,z,yaw,pitch,dystans' (BraxGymPlayer.ReviewView). Yaw: 0 = patrzy na -Z, 90 = -X, 180 = +Z, 270 = +X (morze).
import {chromium} from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import http from 'node:http';

const root = path.resolve('unity-preview'), out = path.resolve('region-shots');
fs.mkdirSync(out, {recursive: true});
const log = [];
const server = http.createServer((req, res) => {
  let p = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
  if (p === '/unity-gym') p = '/'; else if (p.startsWith('/unity-gym/')) p = p.slice('/unity-gym'.length);
  let f = path.resolve(root, '.' + p);
  if (f !== root && !f.startsWith(root + path.sep)) { res.writeHead(403); res.end(); return; }
  if (fs.existsSync(f) && fs.statSync(f).isDirectory()) f = path.join(f, 'index.html');
  if (!fs.existsSync(f)) { res.writeHead(404); res.end(); return; }
  const types = {'.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg', '.webp': 'image/webp', '.css': 'text/css', '.mp3': 'audio/mpeg'};
  res.setHeader('Content-Type', types[path.extname(f)] || 'application/octet-stream');
  fs.createReadStream(f).pipe(res);
});
await new Promise(r => server.listen(8765, '127.0.0.1', r));

// 10 miejsc z zlecenia STOP (siłownia, Nutrition, NOVA, ulica, wieżowiec, plaża, port, metro, boisko, tyły siłowni) + reszta QA.
const VIEWS = [
  ['01_silownia_wyjscie', '6,0.2,13.5,180,14,9'],
  ['02_silownia_ring', '12,0.2,6,0,12,8'],
  ['03_ulica_wschod', '8,0.2,28,270,12,10'],
  ['04_ulica_zachod', '8,0.2,28,90,12,10'],
  ['05_nutrition_front', '20.7,0.2,25.5,0,12,9'],
  ['06_nutrition_wnetrze', '20.3,0.2,18,0,12,9'],
  ['06b_nutrition_mural', '20.25,0.2,18,0,6,6'],
  ['02b_worki_z_bliska', '14,0.2,8,90,10,2.5'],
  ['02c_tatami_judo', '-2,0.2,4,180,28,9'],
  ['02d_gruszki', '13,0.2,-3,90,10,4'],
  ['03b_elewacja_nutrition', '22,0.2,30,0,22,12'],
  ['07b_elewacja_nad_nova', '44,0.2,28,180,24,12'],
  ['11b_lobby_obraz', '59,0.3,41,180,8,5'],
  ['20b_pociag_z_bliska', '66,-7.1,59.5,270,8,3'],
  ['07_nova_front', '42,0.2,31.5,180,12,9'],
  ['08_nova_wnetrze', '43.5,0.2,44,180,12,10'],
  ['09_nova_bar', '50,0.2,52,90,12,10'],
  ['10_wiezowiec_wejscie', '59,0.2,31,180,14,10'],
  ['11_wiezowiec_lobby', '59,0.3,39,180,12,9'],
  ['12_boiska', '10,0.2,72,180,14,12'],
  ['13_boisko_kosz', '12,0.2,74,90,12,14'],
  ['14_podworko_zielen', '10,0.2,92,0,14,12'],
  ['15_plaza_na_morze', '88,0.4,133,270,14,12'],
  ['16_plaza_piasek', '78,0.4,125,0,16,12'],
  ['17_marina_zejscie', '120,0.1,119,0,14,8'],
  ['17b_marina_pomost', '120,-0.5,104,0,12,9'],
  ['17c_marina_jachty', '122,-0.5,84,270,12,9'],
  ['17d_marina_skutery', '118.5,-0.5,72,90,12,6'],
  ['18_marina_z_plazy', '135,0.3,113,20,14,12'],
  ['30_wyspa_z_plazy', '140,0.4,120,270,10,12'],
  ['31_wyspa_pomost', '262,-0.5,62,270,10,10'],
  ['32_wyspa_plaza', '288,2,62,270,12,10'],
  ['33_wyspa_polana', '300,4,81,270,12,10'],
  ['34_wyspa_wzgorze', '330,14,60,90,14,10'],
  ['35_wyspa_z_wody', '240,0.1,100,300,10,12'],
  ['19_port_kontenery', '76,0.2,2,0,14,12'],
  ['19b_port_dzwig', '60,0.2,-10,270,14,12'],
  ['20_metro_peron', '70,-7.1,59.5,270,12,9'],
  ['21_metro_schody', '78,-3.1,47,180,14,9'],
  ['22_tyly_plac', '0,0.2,-20,0,14,12'],
  ['23_tyly_wiadukt', '-50,0.2,-30,90,14,12'],
  ['23b_tyly_tunel_portal', '-66,0.2,-52,0,12,10'],
  ['24_tyly_wiezowiec_A', '40,0.2,-22,0,16,12'],
  ['24b_tyly_wiezowiec_B', '-36,0.2,-22,0,16,12'],
  ['25_drzewa_z_bliska', '20,0.2,100,0,10,5'],
  ['26_trawa_2m', '20,0.2,100,200,35,2.2'],
  // osiedle w tym samym playerze (build 5, Opus): przejście sceną (ReviewScene), potem ReviewView we współrzędnych osiedla
  ['E01_tunel_z_miasta', '-66.2,8.3,-66,0,12,8'],
  ['E02_osiedle_wylot_tunelu', '0,.1,88,180,12,9', 'estate'],
  ['E03_oktagon_od_wejscia', '-86,.1,76,0,16,7', 'estate'],
  ['E04_oktagon_trybuny', '-80,.1,61,90,14,6', 'estate'],
  ['E05_oktagon_mata_z_gory', '-86,.7,66.5,0,45,5', 'estate'],
  ['E06_silownia_A_recepcja', '88.5,.1,-72,139,12,5', 'estate'],
  ['E07_silownia_A_sciany_mural', '84,.1,-72,0,7,4', 'estate'],
  ['E08_kasyno_bok_pietro', '50,.1,-18,250,7,12', 'estate'],
  ['E09_lawka_park_A', '-28,.1,-84,180,20,5', 'estate'],
  ['E10_A_aleja_ulica_glowna', '0,.08,20,180,10,10', 'estate'],
  ['E11_kasyno_rog_chodnika', '50,.1,-18,325,7,12', 'estate'],
  ['E12_willa1_roze', '-89.21,.1,-215.90,220,26,1.2', 'estate'],
  ['E13_komisariat_szyld', '-86,.1,-18,0,12,12', 'estate'],
  ['E14_park_lawki_B', '-20,.08,-110,0,10,8', 'estate'],
  ['E15_hala_walk_front', '-86,.1,88,0,12,12', 'estate'],
  ['E16_dom_wawerow_front', '-8,.1,59,90,10,9', 'estate'],
  ['E17_dom_wawerow_jak_na_zdjeciu', '-12,.1,46,125,8,10', 'estate'],
  ['E18_tablice_tur_pod_zamosciem', '0,.1,97,0,10,7', 'estate'],
];
const TRAVEL = (process.env.TRAVEL_POINTS || '').split(',').filter(Boolean);

async function session(fn) {
  const browser = await chromium.launch({args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--ignore-gpu-blocklist', '--disable-dev-shm-usage', '--disable-gpu-watchdog', '--js-flags=--max-old-space-size=6144']});
  try {
    const page = await (await browser.newContext({viewport: {width: 1280, height: 576}, deviceScaleFactor: 1})).newPage();
    page.on('console', m => { const t = m.text(); if (/error|exception|BRAXHORN/i.test(t)) log.push(m.type() + ': ' + t.slice(0, 300)); });
    page.on('pageerror', e => log.push('PAGEERROR ' + e.message));
    await page.goto('http://127.0.0.1:8765/?qa=1', {waitUntil: 'domcontentloaded'});
    await page.click('#start');
    await page.waitForFunction(() => (window.BraxhornState?.ready) || (window.BraxhornState?.frames > 30) || window.BraxhornError, null, {timeout: 900000});
    if (await page.evaluate(() => !!window.BraxhornError)) throw Error('Unity startup failed: ' + await page.evaluate(() => String(window.BraxhornError)));
    await page.waitForTimeout(6000);
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
const PART = +(process.env.SHOT_PART || 0), PARTS = +(process.env.SHOT_PARTS || 1);
const ONLY = (process.env.SHOT_ONLY || '').split(',').filter(Boolean);
const MINE = ONLY.length ? VIEWS.filter(v => ONLY.includes(v[0])) : VIEWS.filter((v, k) => k % PARTS === PART);
const save = () => fs.writeFileSync(path.join(out, `results-${PART}.json`), JSON.stringify({results, log}, null, 1));
for (let i = 0; i < MINE.length; i += 6) {
  const chunk = MINE.slice(i, i + 6);
  for (let attempt = 0; attempt < 2; attempt++) {
    try {
      await session(async page => {
        if (i === 0 && PART === 0) fs.writeFileSync(path.join(out, 'state-start.json'), JSON.stringify(await page.evaluate(() => window.BraxhornState), null, 1));
        for (const [name, view, want = 'city'] of chunk) {
          if (fs.existsSync(path.join(out, name + '.png'))) continue;
          const area = await page.evaluate(() => window.BraxhornState?.area || '');
          if (area !== want) {
            await send(page, 'ReviewScene', want);
            await page.waitForFunction(w => window.BraxhornState?.area === w && !window.BraxhornState?.loadingArea, want, {timeout: 900000}).catch(() => log.push('zmiana sceny na ' + want + ': timeout'));
            await page.waitForTimeout(8000);
          }
          // zasłonięty kadr (kamera w drzewie / ścianie) = prawie jednolity obraz: PNG < 150 kB. Wtedy próba obrotu ±40° / bliżej.
          const [vx, vy, vz, vyaw, vp, vd] = view.split(',').map(Number);
          const variants = [view, [vx, vy, vz, vyaw + 40, vp, vd].join(','), [vx, vy, vz, vyaw - 40, vp, vd].join(','), [vx, vy, vz, vyaw, Math.max(vp, 20), Math.max(.8, vd * .55)].join(',')];
          let used = view, size = 0;
          for (const v of variants) {
            await send(page, 'ReviewView', v); await settle(page);
            const buf = await page.screenshot({path: path.join(out, name + '.tmp.png'), timeout: 300000});
            used = v; size = buf.length;
            if (size >= 150000) break;
            log.push('zasłonięty kadr ' + name + ' (' + v + ', ' + size + ' B) — próbuję inny');
          }
          fs.renameSync(path.join(out, name + '.tmp.png'), path.join(out, name + '.png'));
          const s = await page.evaluate(() => window.BraxhornState || {});
          results.push({name, view, used, pngBytes: size, x: s.x, y: s.y, z: s.z, area: s.area, grounded: s.grounded});
          save();
        }
        if (i === 0 && PART === 0) for (const id of TRAVEL) {
          await send(page, 'TravelPoint', id); await settle(page);
          const s = await page.evaluate(() => window.BraxhornState || {});
          await page.screenshot({path: path.join(out, 'travel_' + id + '.png'), timeout: 300000});
          results.push({name: 'travel_' + id, x: s.x, y: s.y, z: s.z, area: s.area});
          save();
        }
      });
      break;
    } catch (e) { log.push('sesja ' + i + ' próba ' + attempt + ': ' + e.message); save(); }
  }
}
save();
server.close();
console.log('REGION_SHOTS done part', PART, 'ujęć:', results.length, 'log:', log.length);
