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
  ['31_wyspa_pomost', '1020,-0.45,42,270,10,10'],          // C2: wyspa przeniesiona (środek 1200, 40)
  ['32_wyspa_plaza', '1062,2,42,270,12,10'],
  ['33_wyspa_polana', '1100,4,61,270,12,10'],
  ['34_wyspa_wzgorze', '1215,12,40,90,14,10'],
  ['35_wyspa_z_wody', '1000,0.1,80,300,10,12'],
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
  // MIASTO 2.11 (przebudowa A i B): od siłowni po wyspę (A), wiadukt→rondo, aleje, wieżowce, centrum, ruch, ludzie (B)
  ['M_A_arena_plac', '1136,3.5,42,270,12,24'],
  ['M_A_arena_szatnia_braxa', '1183,3,45.3,270,10,2'],
  ['M_A_arena_trybuny', '1164,4,35,0,10,12'],
  ['M_A_arena_tunel', '1178,3,42,90,8,6'],
  ['M_A_boisko_kosz_linie', '39,0.5,80,180,35,14'],
  ['M_A_boisko_pilka_linie', '10,0.5,80,180,35,16'],
  ['M_A_marina_jachty', '130,1,50,270,20,30'],
  ['M_A_marina_motorowka', '125,0.5,86.5,180,15,10'],
  ['M_A_metro_hala', '66,-3.6,28.5,90,5,8'],
  ['M_A_metro_peron_pociag', '25,-10.2,35,180,5,6'],
  ['M_A_metro_schody', '72,-2,34.3,270,25,5'],
  ['M_A_metro_schody_ruchome', '51.5,-7.9,30.1,270,15,9'],
  ['M_A_metro_wagon', '40,-9.6,35.6,180,6,6'],
  // METRO pkt 1/1b/1c (jedno wejście, pas płd. zamknięty, pobrany model pociągu, szafka hydrantowa)
  ['METRO_pociag_model', '40,-9.4,36.1,180,8,9'],
  ['METRO_chodnik_pld_caly', '71,.5,22.2,0,28,8'],
  ['METRO_antresola_sciana_ciagla', '62.5,-3.0,22.2,250,8,5'],
  ['DETAL_metro_czolo_pociagu', '59,-10,35.6,150,6,4'],
  ['DETAL_metro_szafka_hydrantowa', '43.9,-3.3,33.6,90,6,2'],
  ['DETAL_metro_sciana_okladzina_cokol', '65.9,-4.2,22.2,270,20,1.8'],
  // METRO pkt 58: schody ruchome (chodnik → antresola jadą w dół, antresola → peron w górę)
  ['METRO_schody_ruchome_wejscie', '66.2,.9,35.07,270,25,4'],
  ['METRO_schody_ruchome_antresola_dol', '75.5,-3.6,35.07,90,10,2.5'],
  ['DETAL_metro_grzebien_gora', '66.9,.2,35.07,270,45,1.6'],
  ['DETAL_metro_porecz_glowica', '65.4,1.3,35.71,225,10,1.8'],
  ['DETAL_metro_schody_ruchome_peron', '45.9,-10.9,30.1,270,35,2'],
  // pkt 42: wyjazd na osiedle bez szlabanu, tunel oświetlony bez czarnej ściany
  ['DROGI_wyjazd_osiedle_bez_szlabanu', '-126,1.2,-72,0,10,10'],
  ['DROGI_tunel_osiedle_wnetrze', '-126,2.5,-90,0,5,12'],
  ['DETAL_tunel_chodnik_odboj', '-130.5,.5,-106,0,20,4'],
  // pkt 4: elewacje bez mozaiki (wycinanie dolnej warstwy w strefach), lada recepcji z kolizją
  ['DETAL_elewacja_nutrition_sasiad', '32,6,21,0,15,10'],
  ['DETAL_elewacja_gym_lewa', '-30,6,22,0,20,10'],
  ['DETAL_elewacja_bank_alejka', '-110,5,68,180,10,8'],
  ['DETAL_tower_recepcja_lada', '53.6,1.0,44.3,270,15,3'],
  ['DETAL_misa_drzewa_zielona', '-10,.3,22.8,0,50,2.2'],
  // pkt 59: BRAXHORN FITNESS Nyxy (lokal „gallery”, ul. Portowa płn.)
  ['DETAL_fitness_szyld', '-4,3.8,36,180,10,7'],
  ['DETAL_fitness_recepcja', '-2.8,1.1,38.3,180,10,1.5'],
  ['DETAL_fitness_mural', '-5.73,2.0,43.0,180,0,3.2'],
  ['DETAL_fitness_szatnia', '0.5,1.2,41.0,270,8,1.6'],
  ['DETAL_fitness_cardio', '-8.2,1.0,38.0,0,15,1.3'],
  // pkt 48 / 48b: nośniki (miejsca 09/12/06/10 szukane fizyką przy buildzie — pozycje w logu BRAXHORN_BILLBOARDY)
  ['DETAL_billboard_citylight_nova', '27.6,1.4,34.0,180,8,2.5'],
  ['DETAL_billboard_mural_silownia', '12.1,2.3,20.7,0,8,6'],
  ['DETAL_billboard_rondo', '-126,7,28.5,0,25,12'],
  // wyspa: kibice na trybunach, ludzie na plaży, obsada (miejsca z kroków wyspy → postacie)
  ['WYSPA_arena_kibice', '1164,5,35,0,18,12'],
  ['DETAL_wyspa_kibice_rzedy', '1164,4.5,30,0,10,4'],
  ['WYSPA_plaza_ludzie', '1170,1,-60,225,12,18'],
  // pkt 46: mieszkanie Braxa nad NOVA (brama, klatka, przedpokój, salon z aneksem, łazienka, sypialnia)
  ['BRAX_mieszkanie_brama', '33.68,1.6,35.9,180,8,5'],
  ['DETAL_mieszkanie_domofon', '34.68,1.4,35.92,180,5,1.2'],
  ['DETAL_mieszkanie_klatka', '33.0,1.5,38.5,180,15,1.2'],
  ['DETAL_mieszkanie_przedpokoj', '39.3,6.3,38.6,90,5,1.2'],
  ['BRAX_mieszkanie_salon', '43.5,6.2,39.0,270,8,2.5'],
  ['DETAL_mieszkanie_kuchnia', '42.5,6.3,43.0,180,10,2.4'],
  ['DETAL_mieszkanie_lazienka', '39.3,6.2,42.4,180,10,2.4'],
  ['DETAL_mieszkanie_sypialnia', '49.2,5.9,42.0,180,15,3.5'],
  ['M_A_metro_wejscie', '68,0.35,34.3,270,12,9'],
  ['M_A_nova_sala', '41,1.8,50,180,5,8'],
  ['M_A_nova_wejscie', '42,1.8,36,180,5,9'],
  ['M_A_nutrition_witryna', '20.2,1.8,20.6,0,5,8'],
  ['M_A_nutrition_wnetrze', '20,1.6,14,0,5,5'],
  ['M_A_plaza_slady', '105,0.8,136,270,15,14'],
  ['M_A_port_brama_biuro', '75,1.5,15,0,8,12'],
  ['M_A_port_kontenery', '72,3,-10,0,30,10'],
  ['M_A_port_plac', '78,2,-8,270,15,22'],
  ['M_A_port_trap', '82,1.5,-8,270,10,8'],
  ['M_A_ulica_lawki_drzewa', '-20,1,22,90,10,12'],
  ['M_A_ulica_pierzeja_pld', '0,4,20.6,0,10,14'],
  ['M_A_ulica_pierzeja_pln', '0,4,36,180,10,14'],
  ['M_A_wyspa_plaza', '1170,1,-60,225,12,30'],
  ['M_A_wyspa_przystan', '1110,1,42,270,12,25'],
  ['M_A_wyspa_z_miasta', '1200,5,40,270,8,260'],
  ['M_B_01_skrzyzowanie_po_wiadukcie', '-50,0.2,28.5,90,12,14'],
  ['M_B_02_rondo_z_ulicy', '-96,0.2,28.5,90,14,16'],
  ['M_B_03_aleja_polnocna', '-116,0.35,56,180,12,12'],
  ['M_B_04_aleja_pd_przystanek', '-117,0.35,-30,0,12,12'],
  ['M_B_05_kamienica_detal', '-124,0.2,74,90,20,8'],
  ['M_B_06_warsztaty', '-92,0.35,-17,0,12,10'],
  ['M_B_07_plac_ustawek', '-91,0.35,-5,0,30,18'],
  ['M_B_08_szlaban_tunel', '-124,0.2,-58,0,10,12'],
  ['M_B_09_plac_wiezowcow', '0,0.2,-20,0,14,12'],
  ['M_B_10_wejscie_A', '40,0.2,-28,0,10,7'],
  ['M_B_11_lobby_A', '40,0.2,-37,0,8,4'],
  ['M_B_12_kawiarnia_B', '-44,0.2,-40,90,10,5'],
  ['M_B_13_biura_A_p3', '30,16.1,-39,90,10,5'],
  ['M_B_14_sejf_A_p6', '52,30.1,-39,270,10,4'],
  ['M_B_15_konferencja_A_p11', '30,53.4,-37,180,8,4'],
  ['M_B_16_nabrzeze', '76,0.35,-50,270,10,10'],
  ['M_B_17_kraniec_zachod', '-150,0.35,28.5,90,6,10'],
  ['M_B_17_kraniec_zachod', '-255,0.35,28.5,90,6,10'],
  ['M_B_18_kraniec_za_wiezowcami', '20,0.35,-62,0,6,10'],
  ['M_B_19_wiezowce', '-10,0.35,-8,0,28,22'],
  ['M_B_20_skwer_dawny_wiadukt', '-66,0.35,60,180,12,12'],
  ['M_B_21_fitness_B_p2', '-44,11.4,-40,90,10,5'],
  ['M_B_23_dawny_wiadukt', '-50,0.2,-30,90,14,12'],
  ['M_B_24_wiezowiec_B', '-36,0.2,-22,0,16,12'],
  ['M_B_30_tower_wejscie', '59,0.2,31,180,10,7'],
  ['M_B_31_tower_lobby', '58,0.2,40,180,8,5'],
  ['M_B_32_tower_apartament', '55,66.95,44,90,10,5'],
  ['M_B_33_tower_taras_morze', '60,120.55,44,270,10,6'],
  ['M_B_34_tower_taras_miasto', '58,120.55,52,90,12,6'],
  ['M_B_40_rondo_ruch', '-100,0.2,40,66,16,20'],
  ['M_B_41_j1_czerwone', '-56,0.2,36,54,10,14'],
  ['M_B_42_parking', '-92,0.2,-42,0,24,20'],
  ['M_B_43_zjazd_parking', '-118,0.2,-45,0,10,9'],
  ['M_B_44_przechodnie_aleja', '-118,0.2,60,180,8,10'],
  ['M_B_45_recepcja_tower', '57,0.2,45,90,8,5'],
  ['M_B_46_lawki_plac', '-40,0.2,-14,0,12,10'],
  ['M_B_50_ul_zachodnia', '-150,0.2,28.5,90,10,16'],
  ['M_B_51_komisariat', '-200,0.2,24,0,12,14'],
  ['M_B_52_komisariat_hol', '-200,0.2,13,0,8,5'],
  ['M_B_53_szpital', '-170,0.2,24,0,12,14'],
  ['M_B_54_szpital_sala', '-170,7.0,9,180,10,5'],
  ['M_B_55_petla_zachod', '-212,0.2,28.5,90,16,18'],
  ['M_B_56_centrum_aleja', '-160,0.2,86,90,14,20'],
  ['M_B_57_centrum_z_ronda', '-120,0.2,40,119,24,30'],
  ['M_B_58_galeria', '-223,0.2,-3,0,12,12'],
  ['M_B_59_centrum_z_tarasu_toweru', '60,120.55,52,90,6,6'],
  // etap „assety” (2.11): nowe dzielnice ze Sketchfaba na NASZYCH drogach, statki, sprzęt siłowni, bramki metra, autobus, tramwaj
  ['M_C_01_aleja_z_petli_zachod', '-268,0.2,26,90,8,14'],          // Aleja Centralna z wlotu pętli ZACHÓD na zachód (ATLANTA na zamknięciu osi)
  ['M_C_02_dzielnica_ny_z_ulicy', '-352,0.2,103,180,12,14'],       // kamienice nowojorskie (city3) z ul. Nowojorskiej
  ['M_C_03_centrum_z_ulicy_c1', '-450,0.2,45,180,14,20'],          // wieże centrum (city5) z ul. C1 na północ
  ['M_C_04_london_z_alei', '-342,0.2,34,180,22,24'],               // LONDON (216 m) z alei
  ['M_C_05_atlanta_z_c3', '-746,0.2,28.5,90,30,32'],               // ATLANTA (274 m) z ul. C3, reklama BRAXHORN SPORT na koronie
  ['M_C_06_kinnaird_bank_z_promenady', '-337,0.2,8,0,10,14'],      // bank KINNAIRD, bankomaty, promenada
  ['M_C_07_przystanek_autobus', '-326,0.2,11,180,8,11'],           // wiata + autobus na przystanku ALEJA CENTRALNA
  ['M_C_08_tramwaj_przystanek', '-485,0.2,-76,180,8,12'],          // tramwaj T1 na torowisku w nawierzchni, peron, wiata
  ['M_C_09_torowisko_z_bliska', '-520,0.2,-60,0,45,6'],            // szyny rowkowe w wycięciach asfaltu (bez nakładek)
  ['M_C_10_skrzyzowanie_aleja_c1', '-436,0.2,22,90,10,12'],        // sygnalizacja na wysięgnikach, zebry w nawierzchni
  ['M_C_11_sport_sprzet', '19,0.2,101.5,0,15,5'],                  // BRAXHORN SPORT: sprzęt w witrynie (pozycja sklepu z logu BRAXHORN SPORT)
  ['M_C_12_fitness_park_tower', '-44,11.4,-40,90,10,5'],           // PARK TOWER p.2: bieżnie i ławka z modeli (to samo ujęcie co M_B_21)
  ['M_C_13_bramki_metra', '66,-3.6,28.5,90,6,5'],                  // bramki biletowe (model) w hali metra
  ['M_C_14_port_drobnicowiec', '78,0.35,-36,0,14,22'],             // drobnicowiec przy nabrzeżu B, dźwig i kontenerowiec A w tle
  ['M_C_15_kontenerowiec_z_tarasu', '60,120.55,52,270,4,8'],       // kontenerowiec na redzie (x 720) z tarasu BRAXHORN TOWER
  ['M_C_16_dzielnice_z_gory', '-520,0.2,28.5,90,55,140'],          // nowe dzielnice z góry (siatka ulic, kwartały, wieże)
  // ZBLIŻENIA DETALI (zasada właściciela 2.11 21:53: przed wysłaniem sprawdzamy wszystkie detale) — 2–6 m od detalu, nisko
  ['DETAL_M_kraweznik_chodnik_aleja', '-350,0.2,33.5,0,35,4'],     // krawężnik z łączeniami, płyty chodnika z fugami, pas zieleni
  ['DETAL_M_szyny_w_asfalcie', '-520,0.2,-62,90,60,2.5'],          // szyna rowkowa w wycięciu asfaltu (bez nakładki), styk z jezdnią
  ['DETAL_M_wiata_przystanku', '-326,0.2,11,180,12,4'],            // wiata, ławka, rozkład, krawężnik peronu
  ['DETAL_M_hydrant_chodnik_c1', '-444,0.2,44,270,25,3'],          // hydrant na chodniku, płyty, krawężnik
  ['DETAL_M_kamienica_parter', '-352,0.2,103,180,10,5'],           // parter kamienicy: cokół, witryny, styk z chodnikiem
  ['DETAL_M_wieza_wejscie', '-450,0.2,40,180,8,6'],                // wejście wieży centrum: drzwi, zadaszenie, styk z płytami
  ['DETAL_M_krata_drzewa_lawka', '-372,0.2,10.5,0,50,3'],          // krata drzewa, ławka, kosz na promenadzie
  // C3 (2.11): migotanie siłowni — futryna wyjścia (z hali i z zewnątrz) i elewacja od ulicy (BRAXHORN_SILOWNIA_MIGOTANIE)
  ['DETAL_silownia_futryna_wyjscie', '6,0.2,11.4,180,6,2.5'],      // futryna drzwi wyjścia od strony hali, styk ze ścianą
  ['DETAL_silownia_futryna_zewnatrz', '6,0.2,11.4,0,6,3'],         // ta sama futryna od zewnątrz (drzwi otwarte przy podejściu)
  ['DETAL_silownia_elewacja', '0,0.2,24,0,12,10'],                 // elewacja siłowni od ulicy: płaszczyzny ścian, lizeny, gzyms
  // C2 (2.11): wyspa ~1 km od miasta (środek 1200, 40): dopływanie, plaża premium (południe), ośrodek domków (północ), chodniki
  ['M_W_01_doplywanie_z_przystani', '1014,-0.45,42,270,8,12'],    // z głowicy pomostu: tablica WYSPA, przystań, wyspa przed dziobem
  ['M_W_02_plaza_premium_rzedy', '1200,1,-70,0,14,12'],           // rzędy leżaków z parasolami, ratownik, boje kąpieliska
  ['M_W_03_plaza_deptak_wzdluz', '1235,1.5,-78,90,10,12'],        // deptak z desek wzdłuż plaży, latarnie, ławki, zaplecze
  ['M_W_04_domki_aleja_A1', '1215,4,85,270,10,12'],               // aleja A1: domki po obu stronach, krawężniki, latarnie
  ['M_W_05_domki_aleja_A3', '1225,4,125,270,12,12'],              // aleja A3 (północ)
  ['M_W_06_recepcja_i_plac_zabaw', '1176,3.5,88,180,10,10'],      // recepcja ośrodka od alei A1
  ['M_W_07_tor_wodny_z_mariny', '143,-0.5,24,270,6,10'],          // z pomostu D: tor wodny (boje) na wschód, jacht dziobem do wyjścia
  ['DETAL_wyspa_domek_okno', '1214,4.5,79,0,5,3'],                 // okno domku: rama, szpros, parapet, okiennice, rynna
  ['DETAL_wyspa_kraweznik_aleja', '1230,4,82,90,40,2.5'],          // krawężnik z fugami, kostka chodnika, styk z dojściem
  ['DETAL_wyspa_deptak_plazy', '1200,1.5,-70,90,25,3'],            // deski deptaku na legarach, styk z piaskiem
  ['DETAL_wyspa_punkt_widokowy', '1200,10,30,180,15,6'],           // punkt widokowy na kamiennym cokole (bez „latającej płyty”)
  // C4 (2.11): szyby bez „mlecznego plastiku” — tło lekko przyciemnione + odbicie nieba (BRAXHORN_SZYBY)
  ['DETAL_szyby_witryna_sport', '19,0.2,104,0,8,3.5'],             // witryna BRAXHORN SPORT: przez szybę wnętrze, odbicie nieba
  ['DETAL_szyby_wejscie_tower', '59,0.2,33,180,6,3.5'],            // przeszklone wejście i lobby BRAXHORN TOWER
  // M8 (2.11): niebo bez „księżyca z kraterami” — słońce zdjęcia wymazane, czysta tarcza słońca w kierunku światła (BRAXHORN_NIEBO)
  ['DETAL_niebo_horyzont_slonca', '60,120.55,52,45,-25,6'],         // z tarasu Tower w stronę słońca: horyzont bez żółtej tarczy
  ['DETAL_niebo_z_ulicy', '30,0.2,28,45,-30,4'],                    // z ulicy pod słońce: niebo, chmury, bez drugiej tarczy
  // PANORAMA 3D (pkt 7, 7b, 15d): pierścień horyzontu z brył — archipelag, nasza wyspa, wzgórza i góry; bez naklejki i smug
  ['DETAL_panorama_archipelag', '60,120.55,52,270,-6,3'],          // z tarasu Tower na morze: wyspy 3D, latarnia, nasza wyspa
  ['DETAL_panorama_lad', '58,120.55,52,90,-6,3'],                  // z tarasu Tower na ląd: wzgórza z polami i wsiami, lasy, góry
  ['DETAL_panorama_z_pomostu', '138,-0.6,22,270,2,5'],             // z pomostu D przy wodzie: archipelag na horyzoncie
  // KRAWĘDZIE ŚWIATA (pkt 20): każda z sensem — morze (archipelag, sztorm dalej), wybrzeże (pętla w mgle), ląd (granica, wzgórza 3D)
  ['KONIEC_wschod', '1330,1,40,270,4,8'],                          // ze wschodniego brzegu wyspy: otwarte morze, archipelag 3D
  ['KONIEC_polnoc', '-100,0.2,170,180,8,6'],                       // północna granica miasta: płot lasu, za nim wzgórza 3D
  ['KONIEC_poludnie', '-200,0.2,-75,0,8,6'],                       // południowa granica: murek, wzgórze tunelu, dalej plany 3D
  ['KONIEC_lad', '-860,0.2,50,90,8,6'],                            // zachodnia granica: balustrada, pola i wsie 3D, góry
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
