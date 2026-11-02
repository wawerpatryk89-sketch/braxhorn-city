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
  // mieszkanie Braxa nad NOVA (Opus, 1.11): drzwi klatki od ulicy, klatka, pokój (okna od ulicy), aneks
  ['07c_klatka_drzwi', '33.6,0.2,32.6,180,8,4'],
  ['07d_klatka_schody', '36.6,0.2,37.0,90,18,1.6'],
  ['07e_mieszkanie_okna', '41,5.15,38.6,0,12,2'],
  ['07f_mieszkanie_lozko_szafa', '41,5.15,40.4,180,14,2'],
  ['07g_mieszkanie_aneks', '40.6,5.15,39.4,90,14,2'],
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
  // ---- build 14 (dyspozytor 2.11): ujęcia z raportów wykonawców; 4. pole = podgląd interfejsu (BraxSend('ReviewUi', tryb))
  ['N14_H01_park_od_startu', '0,.1,-15,0,5,8', 'estate'],
  ['N14_L01_chodnik_sklepy_B', '-3,.14,-183,180,10,7', 'estate'],
  ['N14_L01b_ludzie_z_bliska', '-3,.14,-183,180,10,3', 'estate'],
  ['N14_L02_lawki_fontanna', '-20,.05,-126.3,135,25,14', 'estate'],
  ['N14_L03_kawiarnia_lada', '30.3,.14,-168.3,180,8,5', 'estate'],
  ['N14_L04_dyskoteka_wejscie', '87,.1,29.5,0,10,7', 'estate'],
  ['N14_L05_rondo_auta', '6,.1,33,315,12,8', 'estate'],
  ['N14_L06_silownia_recepcja', '85,.1,-68.3,180,10,4.5', 'estate'],
  ['N14_L07_parkiet', '93,.11,12,90,18,9', 'estate'],
  ['N14_L08_komisariat', '-90,.07,-35,0,10,5', 'estate'],
  ['N14_L09_kasyno_kasa', '72,.06,-34,0,8,4.5', 'estate'],
  ['N14_L10_szpital_hol', '-86,.1,22,0,12,7', 'estate'],
  ['N14_L11_zebra', '-58,.1,-19,45,30,18', 'estate'],
  ['N14_L12_park', '-20,.05,-112,0,18,16', 'estate'],
  ['N14_H02_horyzont_N', '0,.1,30,180,3,6', 'estate'],
  ['N14_H03_horyzont_N_z_parku', '-20,.1,-140,180,4,6', 'estate'],
  ['N14_H04_horyzont_W', '-104,.1,-125,90,3,5', 'estate'],
  ['N14_H05_horyzont_E', '104,.1,-125,270,3,5', 'estate'],
  ['N14_H06_horyzont_S', '19,.1,-226,0,3,5', 'estate'],
  ['N14_H07_tunel_przod', '0,.1,80,180,8,10', 'estate'],
  ['N14_H08_tunel_z_bliska', '0,1.2,96.8,180,2,1.5', 'estate'],
  ['N14_H09_budka_szlaban', '9,.1,94,135,10,4', 'estate'],
  ['N14_H10_lisce_z_bliska', '8,3,6,270,0,2', 'estate'],
  ['N14_H11_bydgoska_drzewa', '58,.1,0,180,8,6', 'estate'],
  ['N14_D01_rondo_z_gory', '0,0.2,42,0,70,12', 'estate'],
  ['N14_D02_rondo_wlot_pld', '3,0.2,20.5,180,8,6', 'estate'],
  ['N14_D03_rondo_wlot_zach', '-21,0.2,43.75,270,8,6', 'estate'],
  ['N14_D05_b2_dom_wsch', '22,0.2,36.9,270,12,8', 'estate'],
  ['N14_D06_b2_dom_zach', '-27,0.2,36.9,90,12,8', 'estate'],
  ['N14_D07_swiatla_tunel', '0,0.2,74,180,10,9', 'estate'],
  ['N14_D08_swiatla_b3', '14,0.2,87.75,90,10,8', 'estate'],
  ['N14_D09_swiatla_b1', '0,0.2,-2,0,10,9', 'estate'],
  ['N14_D10_zebra_tunel', '0,0.2,97,0,12,8', 'estate'],
  ['N14_D11_parking_silownia', '56,0.2,-80,270,14,8', 'estate'],
  ['N14_D12_parking_sala_bokserska', '-80,0.2,86.9,180,16,9', 'estate'],
  ['N14_D13_parking_kasyno', '85,0.2,-13,0,14,10', 'estate'],
  ['N14_D14_park_zebra', '-58,0.2,-54,0,12,9', 'estate'],
  ['N14_D15_tablica_bydgoska', '52,0.2,-10.5,0,6,3.5', 'estate'],
  ['N14_D17_przystanek_poznanska', '5.5,0.2,3,180,10,6', 'estate'],
  ['N14_D19_b_parking', '5,0.2,-158.5,180,14,9', 'estate'],
  ['N14_D21_b_plac_pawilonu', '8,0.2,-191.25,180,10,7', 'estate'],
  ['N14_K01_kasyno_ruletka', '73,.06,-31.5,0,12,3', 'estate'],
  // build 45 runda 2 — ZBLIŻENIA DETALI (kamera ~2–3 m × K od mebla/ściany, oczy): listwy, fugi, gniazdka, oprawy
  ['DETAL_dyskoteka_bar', '75,.06,3.3,0,6,2.4', 'estate'],
  ['DETAL_dyskoteka_bar_butelki', '72.2,.06,2.9,0,14,1.6', 'estate'],
  ['DETAL_dyskoteka_loze', '71.4,.06,10.6,90,8,2.4', 'estate'],
  ['DETAL_dyskoteka_loze_wyspa', '80.8,.06,12.5,90,8,2.4', 'estate'],
  ['DETAL_dyskoteka_dj', '98.6,.11,12,270,4,2.6', 'estate'],
  ['DETAL_dyskoteka_szatnia', '94.0,.06,26.3,270,4,2.2', 'estate'],
  ['DETAL_dyskoteka_wyjscie', '86,.06,2.8,0,2,2.4', 'estate'],
  ['DETAL_wejscie_dyskoteka', '86,.1,31.6,0,4,2.6', 'estate'],
  ['DETAL_wejscie_kasyno', '86,.1,-24.4,0,4,2.6', 'estate'],
  ['DETAL_wejscie_arena', '-86,.1,83.5,0,4,2.6', 'estate'],
  ['DETAL_kasyno_przejscie_vip', '85.6,.06,-48.8,0,6,2.2', 'estate'],
  ['N14_T01_witryna_kawiarni', '30.25,.3,-183.8,180,12,8', 'estate'],
  ['N14_T02_witryna_marketu', '42.55,.3,-183.8,180,12,8', 'estate'],
  // elewacje (oko → szyld z raportu, przeliczone: gracz na linii patrzenia, kamera za nim)
  ['N14_E01_pierzeja_chodnik', '18.0,0.1,-182.3,180,-7,5.7', 'estate'],
  ['N14_E02_pierzeja_ulica', '5.1,0.1,-190.2,227,-2,7.0', 'estate'],
  ['N14_E03_ulica_B', '-3.1,0.1,-192.0,262,-6,7.0', 'estate'],
  ['N14_E04_tyl_pierzei_schody', '34.0,0.1,-157.7,325,-4,7.0', 'estate'],
  ['N14_E05_arena_braxhorn', '-82.9,0.1,85.6,25,-4,7.0', 'estate'],
  ['N14_E06_komisariat', '-83.1,0.1,-21.3,27,-4,7.0', 'estate'],
  ['N14_E07_szpital', '-88.6,0.1,33.9,331,-4,7.0', 'estate'],
  ['N14_E08_dyskoteka', '86.0,0.1,35.0,0,-4,7.0', 'estate'],
  ['N14_E09_sala_bokserska', '82.9,0.1,85.6,335,-4,7.0', 'estate'],
  ['N14_E10_wawerowie_front', '-22.3,0.1,58.0,115,-6,7.0', 'estate'],
  ['N14_E11_wawerowie_balkon', '-38.3,0.1,59.9,288,-4,7.0', 'estate'],
  ['N14_E12_tyl_dyskoteki', '83.4,0.1,-5.9,209,-4,7.0', 'estate'],
  ['N14_E13_bok_komisariatu', '-64.6,0.1,-40.6,45,-4,6.5', 'estate'],
  ['N14_E14_silownia_front', '92.0,0.1,-64.0,0,-3,5.5', 'estate'],
  ['N14_E15_silownia_od_parku', '72.0,0.1,-97.0,180,-2,4.0', 'estate'],
  ['N14_E16_kort', '33.7,0.1,-201.7,0,-5,4.2', 'estate'],
  ['N14_E17_numer_4_willa', '-3.2,0.1,-27.8,0,-7,2.8', 'estate'],
  // auta (raport: kamera → punkt; gracz w punkcie patrzenia)
  ['N14_A01_rondo_z_gory', '0.0,0.1,42.0,180,43,29.9', 'estate'],
  ['N14_A02_rondo_oczy', '0.0,0.1,42.0,163,-0,20.9', 'estate'],
  ['N14_A03_swiatla_b1', '0.0,0.1,-19.0,38,11,16.7', 'estate'],
  ['N14_A04_b1_z_bliska', '25.0,0.1,-17.0,180,-4,7.5', 'estate'],
  ['N14_A05_skret_s1_b1', '-43.0,0.1,-19.0,38,11,11.6', 'estate'],
  ['N14_A06_parking_sala_bokserska', '88.0,0.1,89.0,225,16,19.1', 'estate'],
  ['N14_A06b_parking_b1', '86.0,0.1,-13.0,287,14,17.3', 'estate'],
  ['N14_A07_ulica_B', '-20.0,0.1,-193.0,68,2,21.6', 'estate'],
  ['N14_A08_szlaban_bez_aut', '0.0,0.1,97.0,180,4,17.0', 'estate'],
  // --- wykonawca B (build 45b, E21): stacja paliw + myjnia — ujęcia ogólne i zbliżenia detali (kamera ~2–3 m × K, oczy) ---
  ['B45_stacja_od_ulicy', '-15,0.1,-38,0,18,12', 'estate'],
  ['B45_myjnia_od_placu', '-5,0.1,-55,20,16,11', 'estate'],
  ['DETAL_stacja_dystrybutor_pistolety', '-26.9,0.1,-35.8,0,10,3', 'estate'],
  ['DETAL_stacja_gzyms_wiaty', '-31.5,4.5,-29,315,10,3.5', 'estate'],
  ['DETAL_sklep_witryna_parapet', '-33,0.1,-49,0,15,3', 'estate'],
  ['DETAL_sklep_rura_spustowa_cokol', '-35.5,0.1,-60.6,200,12,2.6', 'estate'],
  ['DETAL_sklep_wnetrze_lada', '-33.5,0.27,-54.6,90,10,3', 'estate'],
  ['DETAL_plac_kratka_kraweznik', '-34.5,0,-45.5,30,45,2.5', 'estate'],
  ['DETAL_myjnia_odplyw_panele', '0.75,0.1,-52,0,25,3', 'estate'],
  ['DETAL_myjnia_lanca_panel', '-9.5,0.1,-54,0,15,3', 'estate'],
  ['B45_warsztat_od_ulicy', '35,0.1,-40,0,14,12', 'estate'],
  ['DETAL_warsztat_brama_segmenty', '38.1,0.1,-42.3,0,10,3', 'estate'],
  ['DETAL_warsztat_okno_parapet_opaska', '23,0.1,-48,270,10,3', 'estate'],
  ['DETAL_warsztat_podnosnik_hala', '32.4,0.25,-48.5,0,15,3', 'estate'],
  // --- wykonawca B, runda 3 (E3a): ulice A poszerzone do 2 × 3.5 m × K tam, gdzie jest miejsce ---
  ['B45_E3a_wisniowa_poszerzona', '-58,0.1,30,180,14,12', 'estate'],
  ['B45_E3a_B1_przy_stacji', '-10,0.1,-19,270,12,12', 'estate'],
  ['B45_E3a_uliczka_S1', '-44.3,0.1,20,180,12,12', 'estate'],
  ['DETAL_ulica_kraweznik_chodnik_wisniowa', '-63.5,0.1,0,90,20,3', 'estate'],
  ['DETAL_ulica_oznakowanie_w_wycieciu', '-2,0.1,-13,0,40,2.5', 'estate'],
  // --- runda 3 (E3b): łuki krawężników na skrzyżowaniach ---
  ['B45_E3b_skrzyzowanie_B1_Poznanska', '0,0.1,-19,200,30,12', 'estate'],
  ['B45_E3b_skrzyzowanie_B3_Bydgoska', '58,0.1,86,225,30,12', 'estate'],
  ['DETAL_luk_kraweznika_fugi', '6.8,0.1,-12.2,225,20,3.5', 'estate'],
  ['DETAL_luk_chodnik_zebra', '12,0.1,-14,200,25,4', 'estate'],
  ['N14_UI01_przyciski', '0,.08,20,180,10,10', 'estate', 'przyciski'],
  ['N14_UI02_menu', '0,.08,20,180,10,10', 'estate', 'menu'],
  ['N14_UI03_ustawienia', '0,.08,20,180,10,10', 'estate', 'ustawienia'],
  ['N14_UI05_teleport', '0,.08,20,180,10,10', 'estate', 'teleport'],
  ['N14_UI07_mapa', '0,.08,20,180,10,10', 'estate', 'mapa'],
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
  // obszary doczytywane w tle (BraxObszary): poczekaj, aż ruszy i skończy się wczytywanie drugiego obszaru
  await page.waitForTimeout(900);
  await page.waitForFunction(() => !window.BraxhornState?.loadingParts, null, {timeout: 240000}).catch(() => log.push('obszar: timeout wczytywania'));
  const f0 = await page.evaluate(() => window.BraxhornState?.frames || 0);
  await page.waitForFunction(f => (window.BraxhornState?.frames || 0) >= f + 8, f0, {timeout: 240000}).catch(() => log.push('frames timeout'));
  await page.waitForTimeout(1500);
}
const results = [];
const PART = +(process.env.SHOT_PART || 0), PARTS = +(process.env.SHOT_PARTS || 1);
const ONLY = (process.env.SHOT_ONLY || '').split(',').filter(Boolean);
// Rejon buildu (CI, build 14 — rejony osobno): osiedle → tylko ujęcia osiedla ('estate'), miasto → tylko miasta, oba → wszystkie.
const REJON = (process.env.SHOT_REJON || 'oba').toLowerCase();
const POOL = VIEWS.filter(v => REJON === 'osiedle' ? v[2] === 'estate' : REJON === 'miasto' ? (v[2] || 'city') === 'city' : true);
const MINE = ONLY.length ? POOL.filter(v => ONLY.includes(v[0])) : POOL.filter((v, k) => k % PARTS === PART);
const save = () => fs.writeFileSync(path.join(out, `results-${PART}.json`), JSON.stringify({results, log}, null, 1));
for (let i = 0; i < MINE.length; i += 6) {
  const chunk = MINE.slice(i, i + 6);
  for (let attempt = 0; attempt < 2; attempt++) {
    try {
      await session(async page => {
        if (i === 0 && PART === 0) fs.writeFileSync(path.join(out, 'state-start.json'), JSON.stringify(await page.evaluate(() => window.BraxhornState), null, 1));
        for (const [name, view, want = 'city', ui = ''] of chunk) {
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
            if (ui) { await send(page, 'ReviewUi', ui); await page.waitForTimeout(1500); }   // interfejs Unity (przyciski, menu) na zdjęciu
            const buf = await page.screenshot({path: path.join(out, name + '.tmp.png'), timeout: 300000});
            if (ui) { await send(page, 'ReviewUi', 'wyl'); await page.waitForTimeout(500); }
            used = v; size = buf.length;
            if (size >= 150000 || ui) break;
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
