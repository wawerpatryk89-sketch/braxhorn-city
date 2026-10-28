// BRAXHORN CITY — a small 3D street brawler: gym → street (day/night, traffic, bosses, zombies) → crypt.
import * as THREE from 'three';
import { clone as cloneSkinned } from '../three/examples/jsm/utils/SkeletonUtils.js';
import { EffectComposer } from '../three/examples/jsm/postprocessing/EffectComposer.js';
import { RenderPass } from '../three/examples/jsm/postprocessing/RenderPass.js';
import { UnrealBloomPass } from '../three/examples/jsm/postprocessing/UnrealBloomPass.js';
import { GradePass } from './grade.js';
import { World } from './world.js';
import { buildGym, HALL, RING, DOOR, ALLEY } from './gym.js';
import { createWetFloor } from './floor.js';
import * as TX from './textures.js';
import { Fighter, MOVES, SCALE, angDiff } from './fighter.js';
import { Sound } from './sound.js';
import { UI } from './ui.js';
import { view, applyOrientation } from './view.js';
import * as A from './assets.js';
import { DayNight, lampU, LAMP_N, addLampField, smooth } from './env.js';
import { buildStreet, ST, prepareCar, CARS } from './street.js';
import { Traffic } from './traffic.js';
import { buildCrypt, CRYPT } from './crypt.js';
import { People } from './people.js';
import { buildVilla, VILLA } from './villa.js';
import { PLACES, buildPlace } from './places.js';
import { HEROES, PROLOGUE, CHAPTERS, BOSSES, CRYPT_BOSSES, ZOMBIE, ENDING, HINTS } from './story.js';

const $ = (id) => document.getElementById(id);
const store = { get(k, d) { try { const v = localStorage.getItem('bx_' + k); return v === null ? d : JSON.parse(v); } catch { return d; } }, set(k, v) { try { localStorage.setItem('bx_' + k, JSON.stringify(v)); } catch { } } };
const QUALITY = {
  high: { name: 'Wysoka', dpr: 2, reflections: true, reflectionScale: 0.5, streak: 44, streakTaps: 7, bloom: 1, shadow: 2048, cones: true, spots: 6, pointLights: 2, msaa: 4 },
  medium: { name: 'Średnia', dpr: 1.5, reflections: true, reflectionScale: 0.33, streak: 30, streakTaps: 5, bloom: 0.5, shadow: 1024, cones: true, spots: 4, pointLights: 1, msaa: 0 },
  low: { name: 'Niska', dpr: 1, reflections: false, reflectionScale: 0.25, streak: 12, streakTaps: 5, bloom: 0, shadow: 1024, cones: false, spots: 3, pointLights: 1, msaa: 0 },
};
const isTouch = matchMedia('(pointer: coarse)').matches || 'ontouchstart' in window;
const isIOS = /iPhone|iPad|iPod/.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
if (isTouch) for (const [k, q] of Object.entries(QUALITY)) { q.dpr = Math.min(q.dpr, k === 'high' ? 2 : 1.5); q.msaa = 0; q.shadow = Math.min(q.shadow, 1024); if (k === 'high') q.reflectionScale = 0.33; }
const flat = (a, b) => Math.hypot(a.pos.x - b.pos.x, a.pos.z - b.pos.z);
const rand = (a, b) => a + Math.random() * (b - a);
const fmtMoney = (v) => '$' + Math.round(v).toLocaleString('pl-PL');

// ---------------- logos (the player's brand) ----------------
async function makeLogos() {
  const [wide, badge, poster, mark] = await Promise.all(['logo/wide.webp', 'logo/badge.webp', 'logo/poster.webp', 'logo/mark.webp'].map(p => A.loadTex(p, true)));
  const cv = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; };
  const tex = (c) => { const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return t; };
  const P = poster.image, W = wide.image, B = badge.image, M = mark.image;
  // poster cropped to head + shirt logo
  const pc = cv(640, Math.round(640 * P.height * 0.66 / P.width)); pc.getContext('2d').drawImage(P, 0, 0, P.width, P.height * 0.66, 0, 0, pc.width, pc.height);
  // billboard: poster on the left, logo + tagline on the right
  const bb = cv(2048, 1024), bx = bb.getContext('2d');
  bx.fillStyle = '#050605'; bx.fillRect(0, 0, 2048, 1024);
  const g = bx.createRadialGradient(1350, 520, 50, 1350, 520, 900); g.addColorStop(0, 'rgba(139,255,58,0.18)'); g.addColorStop(1, 'rgba(0,0,0,0)'); bx.fillStyle = g; bx.fillRect(0, 0, 2048, 1024);
  const ph = 1024, pw = ph * P.width / (P.height * 0.62); bx.drawImage(P, 0, 0, P.width, P.height * 0.62, 0, 0, pw, ph);
  const fade = bx.createLinearGradient(pw - 160, 0, pw, 0); fade.addColorStop(0, 'rgba(5,6,5,0)'); fade.addColorStop(1, 'rgba(5,6,5,1)'); bx.fillStyle = fade; bx.fillRect(pw - 160, 0, 170, 1024);
  const lw = 2048 - pw - 60, lh = lw * W.height / W.width; bx.drawImage(W, pw + 20, 330 - lh / 2 + 80, lw, lh);
  bx.fillStyle = '#e8ece4'; bx.font = '800 96px "Big Shoulders Display", Impact, sans-serif'; bx.textAlign = 'center';
  bx.fillText('FUEL A STRONGER YOU', pw + 20 + lw / 2, 790);
  bx.fillStyle = '#8bff3a'; bx.fillRect(pw + 120, 840, lw - 200, 8);
  // tall ad for the bus stop
  const bt = cv(640, 960), tx = bt.getContext('2d'); tx.fillStyle = '#060706'; tx.fillRect(0, 0, 640, 960);
  tx.drawImage(B, 20, 60, 600, 600); tx.fillStyle = '#8bff3a'; tx.font = '800 64px "Big Shoulders Display", Impact, sans-serif'; tx.textAlign = 'center'; tx.fillText('NUTRITION', 320, 760); tx.fillStyle = '#e8ece4'; tx.font = '700 44px "Big Shoulders Display", Impact, sans-serif'; tx.fillText('FUEL A STRONGER YOU', 320, 850);
  // spray-painted mark: black -> transparent
  const ms = cv(512, 512), mx = ms.getContext('2d'); mx.drawImage(M, 0, 0, 512, 512);
  const id = mx.getImageData(0, 0, 512, 512), d = id.data;
  for (let i = 0; i < d.length; i += 4) { const l = Math.max(d[i], d[i + 1], d[i + 2]); d[i + 3] = Math.min(255, Math.max(0, (l - 30) * 2.2)); }
  mx.putImageData(id, 0, 0);
  return { wide, badge, poster, mark, posterCrop: tex(pc), billboard: tex(bb), badgeTall: tex(bt), markSpray: tex(ms), badgeImg: B, wideImg: W };
}

async function boot() {
  const fontStack = '"Big Shoulders Display", "Arial Narrow", Impact, sans-serif';
  try { await Promise.race([document.fonts.load('800 100px "Big Shoulders Display"'), new Promise(r => setTimeout(r, 2500))]); } catch { }
  let landscape = store.get('land', true);
  applyOrientation(landscape, isTouch);
  let qKey = store.get('quality2', isTouch && !isIOS ? 'medium' : 'high'); if (!QUALITY[qKey]) qKey = 'medium';
  const Q = { ...QUALITY[qKey] };
  const loadMsg = (m) => { const el = $('loadMsg'); if (el) el.textContent = m; };

  // ---------------- renderer ----------------
  const canvas = $('c');
  const fail = (msg) => { const l = $('loader'); l.classList.remove('done'); loadMsg(msg); const b = l.querySelector('.bar'); if (b) b.hidden = true; };
  canvas.addEventListener('webglcontextlost', (e) => { e.preventDefault(); fail('Telefonowi zabrakło pamięci grafiki. Zamknij kartę i otwórz grę ponownie, najlepiej z jakością Niska.'); });
  const renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: 'high-performance' });
  if (renderer.getContext().isContextLost()) { fail('Przeglądarka nie dała grze dostępu do grafiki 3D.'); return; }
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, Q.dpr));
  renderer.setSize(view.w(), view.h(), false);
  renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.0;
  renderer.shadowMap.enabled = true; renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  A.initLoaders(renderer);
  let lastBytes = 0;
  A.net.onProgress = () => { if (A.net.bytes - lastBytes > 200000) { lastBytes = A.net.bytes; const el = $('netMB'); if (el) el.textContent = (A.net.bytes / 1048576).toFixed(1) + ' MB'; } };

  const sound = new Sound(store.get('sound', true));
  const soundReady = sound.prepare();

  loadMsg('Ładuję logo i postacie…');
  const logos = await makeLogos();
  const META = await A.loadJSON('chars/clips.json');

  // ---------------- scene ----------------
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x0b0f1a);
  scene.fog = new THREE.FogExp2(0x0a0807, 0.011);
  const camera = new THREE.PerspectiveCamera(55, view.w() / view.h(), 0.1, 420);
  camera.layers.enable(2); camera.layers.enable(3);
  const world = new World();
  loadMsg('Buduję siłownię…');
  const gym = buildGym(scene, world, Q, fontStack, logos);
  const cTex = TX.concrete(512);
  const floor = createWetFloor({ width: HALL.maxX - HALL.minX + 1.2, depth: ALLEY.z1 + 0.6 - (HALL.minZ - 3.6), center: new THREE.Vector3(0, 0, (ALLEY.z1 + 0.6 + HALL.minZ - 3.6) / 2), maps: cTex, wet: TX.wetMask(), quality: Q });
  gym.root.add(floor.mesh);
  const hemi = new THREE.HemisphereLight(0xb09070, 0x1c1612, 0.85); scene.add(hemi);
  const key = new THREE.DirectionalLight(0xfff0dc, 1.6);
  key.castShadow = true; key.shadow.mapSize.set(Q.shadow, Q.shadow);
  const sc = key.shadow.camera; sc.left = -12; sc.right = 12; sc.top = 12; sc.bottom = -12; sc.near = 1; sc.far = 60;
  key.shadow.bias = -0.0004; key.shadow.normalBias = 0.03;
  scene.add(key, key.target);
  const env = new DayNight(scene, { hour: store.get('hour', 17.6), dayLengthMin: 12 });
  // reflections on skin / metal: capture the hall
  const pmrem = new THREE.PMREMGenerator(renderer);
  env.sky.visible = false;
  scene.position.y = -1.7; scene.updateMatrixWorld(true);
  const envGym = pmrem.fromScene(scene, 0.04, 0.1, 60).texture;
  scene.position.y = 0; scene.updateMatrixWorld(true);
  env.sky.visible = true;
  scene.environment = envGym;
  const GYM_HEMI = { sky: new THREE.Color(0xc4a888), ground: new THREE.Color(0x2a221b), i: 1.3 }, GYM_KEY = { c: new THREE.Color(0xfff0dc), i: 2.0 };

  // ---------------- characters ----------------
  const chars = {}, charMats = [];
  function loadChar(k) {
    if (!chars[k]) chars[k] = (async () => {
      const [g, map, nrm, orm] = await Promise.all([A.loadRig(`chars/${k}.glb.gz`), A.loadTex(`tex/${k}_color.webp`, true), A.loadTex(`tex/${k}_normal.webp`, false), A.loadTex(`tex/${k}_orm.webp`, false)]);
      const mat = addLampField(new THREE.MeshStandardMaterial({ map, normalMap: nrm, roughnessMap: orm, metalnessMap: orm, metalness: 0.6, roughness: 1, envMapIntensity: 0.9 }), 'lit');
      g.scene.traverse(o => { if (o.isSkinnedMesh) o.material = mat; }); charMats.push(mat);
      return { scene: g.scene, clips: g.animations, mat, used: 0 };
    })();
    return chars[k];
  }
  const blobTex = TX.radial('rgba(0,0,0,0.75)', 'rgba(0,0,0,0)');
  const blobMat = new THREE.MeshBasicMaterial({ map: blobTex, transparent: true, depthWrite: false });
  const actors = [];
  async function spawn(k, { name, team = 'enemy', size = 1, moves = null, isPlayer = false, hp = 100, pos = [0, 0, 0], heading = 0 } = {}) {
    const c = await loadChar(k);
    const model = c.used++ === 0 ? c.scene : cloneSkinned(c.scene);
    const f = new Fighter({ model, clips: c.clips, meta: META, name: name || k, world, size, moves, isPlayer });
    f.key = k; f.team = team; f.hp = f.maxHp = hp;
    f.pos.set(pos[0], 0, pos[2]); f.y = pos[1] || 0; f.heading = heading;
    scene.add(f.root); f.root.position.y = f.y; f.root.rotation.y = heading;
    const b = new THREE.Mesh(new THREE.PlaneGeometry(1.6 * size, 1.6 * size).rotateX(-Math.PI / 2), blobMat); b.renderOrder = 2; scene.add(b); f.blob = b;
    actors.push(f);
    return f;
  }
  function despawn(f) {
    scene.remove(f.root); scene.remove(f.blob);
    const i = actors.indexOf(f); if (i >= 0) actors.splice(i, 1);
    const wi = world.dynamic.indexOf(f.col); if (wi >= 0) world.dynamic.splice(wi, 1);
    f.mixer.stopAllAction();
  }

  // ---------------- post ----------------
  const composer = new EffectComposer(renderer, new THREE.WebGLRenderTarget(view.w(), view.h(), { type: THREE.HalfFloatType, samples: Q.msaa }));
  composer.addPass(new RenderPass(scene, camera));
  const bloom = new UnrealBloomPass(new THREE.Vector2(256, 256), 0.42, 0.45, 0.95);
  bloom.enabled = Q.bloom > 0; composer.addPass(bloom);
  const grade = new GradePass(); composer.addPass(grade);
  function resize() {
    applyOrientation(landscape, isTouch);
    const w = view.w(), h = view.h();
    renderer.setSize(w, h, false); camera.aspect = w / h; camera.updateProjectionMatrix();
    composer.setPixelRatio(renderer.getPixelRatio()); composer.setSize(w, h);
    const pr = renderer.getPixelRatio();
    bloom.setSize(Math.round(w * pr * Math.max(0.25, Q.bloom) * 0.5), Math.round(h * pr * Math.max(0.25, Q.bloom) * 0.5));
    floor.setSize(w * pr, h * pr);
    env.stars.material.uniforms.px.value = pr;
  }
  window.addEventListener('resize', resize);
  window.addEventListener('orientationchange', () => setTimeout(resize, 250));
  resize();
  let floorReflect = Q.reflections;
  function applyQuality(k) {
    qKey = k; Object.assign(Q, QUALITY[k]); store.set('quality2', k);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, Q.dpr));
    floor.uniforms.reflStrength.value = Q.reflections ? 1 : 0; bloom.enabled = Q.bloom > 0;
    for (const c of gym.cones || []) c.visible = Q.cones;
    for (const l of gym.lights) { if (l.isSpotLight) l.visible = l.userData.rank < Q.spots; else if (l.userData.tier) l.visible = l.userData.tier <= Q.pointLights; }
    if (key.shadow.mapSize.x !== Q.shadow) { key.shadow.mapSize.set(Q.shadow, Q.shadow); key.shadow.map?.dispose(); key.shadow.map = null; }
    floorReflect = Q.reflections; resize();
  }

  // ---------------- UI ----------------
  const ui = new UI({ isTouch, quality: QUALITY, qKey, sound, moves: MOVE_LIST });
  const people = new People({ scene, world, A });
  const save = { money: store.get('money', 0), best: store.get('best', 0) };
  const upg = Object.assign({ hp: 0, dmg: 0, armor: 0, shakes: 1 }, store.get('upg', {}));
  const SHOP = [
    { id: 'shakes', name: 'Shake BRAXHORN', desc: 'Leczy 60% zdrowia w walce (przycisk SHAKE / klawisz B).', price: 150, max: 9 },
    { id: 'hp', name: 'Białko BRAXHORN', desc: '+15 do maksymalnego zdrowia.', price: 400, max: 5 },
    { id: 'dmg', name: 'Kreatyna BRAXHORN', desc: '+8% siły ciosów.', price: 500, max: 5 },
    { id: 'armor', name: 'Ochraniacze', desc: '−8% obrażeń, które dostajesz.', price: 500, max: 5 },
    { id: 'bat', name: 'Kolczasta pałka', desc: 'Nowa pałka do ręki (16 uderzeń).', price: 250, max: 1 },
  ];
  function applyUpgrades() {
    if (!player || !G.hero) return;
    const frac = player.hp / player.maxHp;
    player.maxHp = G.hero.hp + 15 * upg.hp; player.hp = Math.min(player.maxHp, Math.max(1, frac * player.maxHp));
    player.dmgMul = G.hero.dmg * (1 + 0.08 * upg.dmg); player.armor = 1 - 0.08 * upg.armor;
    ui.shakes(upg.shakes); updateBars();
  }
  function buy(id) {
    const it = SHOP.find(i => i.id === id); if (!it) return false;
    if (G.money < it.price) { ui.banner('', 'Za mało pieniędzy.', 1.4); return false; }
    if (id === 'bat') { if (G.bat) { ui.banner('', 'Masz już pałkę.', 1.4); return false; } if (!G.batModel) return false; equipBat(); }
    else { if ((upg[id] || 0) >= it.max) { ui.banner('', 'Masz już maksimum.', 1.4); return false; } upg[id] = (upg[id] || 0) + 1; store.set('upg', upg); }
    addMoney(-it.price); sound.play('cash', { gain: 0.6 }); applyUpgrades(); return true;
  }
  function useShake() {
    if (!player || upg.shakes <= 0 || player.hp >= player.maxHp - 1) return;
    upg.shakes--; store.set('upg', upg); player.hp = Math.min(player.maxHp, player.hp + player.maxHp * 0.6); updateBars(); ui.shakes(upg.shakes);
    ui.banner('', 'Shake BRAXHORN: +60% zdrowia', 1.4); sound.play('cash', { gain: 0.3, rate: 0.7 });
  }
  ui.onBuy = (id) => { const ok = buy(id); ui.shop(true, SHOP, upg, G.money); return ok; };
  const G = { mode: 'title', chapter: 'gym', hero: null, money: save.money, bossIdx: 0, boss: null, lock: null, street: null, crypt: null, traffic: null, zombies: [], pickups: [], caseJob: null, bat: null, cryptStage: 0, hintT: 0, nightWas: null, kills: 0, specialIdx: 0 };
  ui.money(G.money);

  // ---------------- title + hero select ----------------
  const heroModels = {};
  loadMsg('Ładuję Braxa i Nyxę…');
  const [braxC, nyxaC] = await Promise.all([loadChar('brax'), loadChar('nyxa')]);
  // preview: both heroes in the ring under the lights
  for (const [k, x] of [['brax', RING.x - 1.2], ['nyxa', RING.x + 1.2]]) {
    const f = await spawn(k, { name: HEROES[k].name, team: 'hero', pos: [x, RING.h, RING.z + 0.6], heading: Math.PI * 0 });
    f.setIdle('Boxing'); heroModels[k] = f;
  }
  const cam = { mode: store.get('cam', '3d'), yaw: 0, pitch: 0.3, dist: 5.8, isoYaw: Math.PI / 4, target: new THREE.Vector3(RING.x, 1.6, RING.z), shake: 0, lastManual: -10, auto: true };
  const camPos = new THREE.Vector3();
  let player = null, partner = null;
  let selectFocus = 'brax';

  // ---------------- input ----------------
  const keys = new Set();
  const joy = { x: 0, y: 0, active: false };
  const actions = [];
  const press = (a) => { actions.push(a); sound.unlock(); ui.hideKeysSoon(); };
  window.addEventListener('keydown', (e) => {
    if (e.target && ['INPUT', 'TEXTAREA', 'SELECT'].includes(e.target.tagName)) return;
    const k = e.key.toLowerCase();
    if ([' ', 'arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(k)) e.preventDefault();
    keys.add(k); if (e.repeat) return;
    const map = { escape: 'menu', p: 'menu', ' ': 'jump', j: 'punch', k: 'kick', l: 'combo', u: 'kicks', e: 'throw', t: 'taunt', g: 'dance', r: 'warmup', v: 'cam', m: 'moves', q: 'iso-left', h: 'help', f: 'hook', x: 'dodge', o: 'act', i: 'special', b: 'shake', n: 'timefast', '1': 'view-model', '2': 'view-wire', '3': 'view-bones', enter: 'enter' };
    if (map[k]) press(map[k]);
    sound.unlock();
  });
  window.addEventListener('keyup', (e) => keys.delete(e.key.toLowerCase()));
  // audio may only start/resume inside a real activation (tap release / click / key), not on pointerdown
  for (const ev of ['pointerup', 'touchend', 'click']) window.addEventListener(ev, () => sound.unlock(), { passive: true, capture: true });
  window.addEventListener('blur', () => keys.clear());
  ui.onAction = press;
  const held = { block: false };
  ui.onHold = (name, v) => { held[name] = v; sound.unlock(); };
  ui.onJoy = (x, y, active) => { joy.x = x; joy.y = y; joy.active = active; };
  ui.onLook = (dx, dy) => { if (cam.mode === '3d') { cam.yaw -= dx * 0.006; cam.pitch = Math.max(0.05, Math.min(1.2, cam.pitch + dy * 0.004)); cam.lastManual = performance.now(); } };
  ui.onZoom = (d) => { cam.dist = Math.max(3, Math.min(10, cam.dist * (1 + d))); };

  function goFullscreen() {
    const d = document, el = d.documentElement;
    try { const r = (el.requestFullscreen || el.webkitRequestFullscreen); if (r && !(d.fullscreenElement || d.webkitFullscreenElement)) { const p = r.call(el, { navigationUI: 'hide' }); if (p && p.then) p.then(() => { try { screen.orientation.lock('landscape').catch(() => { }); } catch { } setTimeout(resize, 300); }).catch(() => { }); } } catch { }
  }
  ui.showTitle(true);
  if (isIOS && !(navigator.standalone || matchMedia('(display-mode: standalone)').matches || matchMedia('(display-mode: fullscreen)').matches)) $('iosHint').hidden = false;
  $('loader').classList.add('done');
  ui.onTitle = async () => {
    goFullscreen(); sound.unlock();
    await soundReady; sound.playMusic('street');
    ui.showTitle(false); G.mode = 'select'; ui.showSelect(true, HEROES); selectHero('brax');
  };
  function selectHero(k) { selectFocus = k; ui.selectHero(k); }
  ui.onSelectHero = selectHero;
  ui.onConfirmHero = (k) => startGame(k);

  // ---------------- game start ----------------
  async function startGame(heroKey) {
    G.hero = HEROES[heroKey]; ui.showSelect(false);
    const otherKey = heroKey === 'brax' ? 'nyxa' : 'brax';
    player = heroModels[heroKey]; partner = heroModels[otherKey];
    player.isPlayer = true; player.team = 'hero';
    player.hp = player.maxHp = G.hero.hp; player.speedMul = G.hero.speed; player.dmgMul = G.hero.dmg;
    player.pos.set(-1.5, 0, 2.8); player.y = 0; player.heading = Math.PI;
    partner.team = 'spar'; partner.name = HEROES[otherKey].name; partner.hp = partner.maxHp = 120; partner.dmgMul = 0.7;
    partner.pos.set(RING.x, 0, RING.z - 1.0); partner.y = RING.h; partner.heading = 0; partner.setIdle('Boxing');
    cam.target.set(player.pos.x, 1.6, player.pos.z); cam.yaw = Math.PI; cam.snap = true;
    ui.setHero(G.hero, HEROES[otherKey]);
    applyUpgrades();
    window.__mg.player = player; window.__mg.partner = partner;
    G.mode = 'story';
    await ui.story(PROLOGUE);
    G.mode = 'play'; G.chapter = 'gym';
    await ui.chapter(CHAPTERS.gym);
    ui.objective(HINTS.gymStart);
    sound.setRoom('gym');
    // gym members, then stream the street in the background
    people.populateGym().catch(e => console.warn('gym people', e));
    loadStreet().catch(e => console.error(e));
  }

  // ---------------- street ----------------
  let streetPromise = null;
  function loadStreet() {
    if (streetPromise) return streetPromise;
    streetPromise = (async () => {
      const st = await buildStreet({ scene, world, font: fontStack, A, logos, Q });
      const carKeys = Object.keys(CARS);
      const models = {};
      await Promise.all(carKeys.map(async k => { try { models[k] = prepareCar(k, await A.loadGLTF(k + '.glb')); } catch (e) { console.warn('car', k, e); } }));
      // a few parked cars
      G.myCar = { x: -10.5, z: ST.road0 + 0.95 };
      const parked = [['car_rev', G.myCar.x, G.myCar.z, Math.PI / 2], ['car_zuk', -24, ST.road0 + 0.95, Math.PI / 2], ['car_gmc', 46, ST.road1 - 0.95, -Math.PI / 2], ['car_mus', -33, ST.road1 - 0.95, -Math.PI / 2], ['car_dia', -47.5, ST.near + 2.0, 0.6], ['car_mar', -47.5, ST.far - 1.2, 2.6]];
      for (const [k, x, z, ry] of parked) {
        if (!models[k]) continue;
        const o = models[k].clone(); o.position.set(x, k === 'car_dia' || k === 'car_mar' ? 0.15 : 0, z); o.rotation.y = ry; st.group.add(o);
        const s = models[k].userData.size; const c = Math.abs(Math.cos(ry)), sn = Math.abs(Math.sin(ry));
        const hw = (s.x * c + s.z * sn) / 2, hd = (s.x * sn + s.z * c) / 2;
        world.box(x - hw, x + hw, z - hd, z + hd, 1.4, -1, 'car');
        if (models[k].userData.def.police) st.lampSources.push({ pos: new THREE.Vector3(x, 1.9, z), col: new THREE.Color(1, 0, 0), r: 9, kind: 'flash', phase: Math.random() * 3 });
      }
      G.traffic = new Traffic({ scene: st.group, models, sound }); G.traffic.camPos = camera.position;
      // pickups: the barbed bat in the alley
      const batG = await A.loadGLTF('bat.glb'); G.batModel = batG.scene; G.batModel.traverse(o => { if (o.isMesh) { addLampField(o.material, 'lit'); o.castShadow = true; } });
      spawnBatPickup(new THREE.Vector3(ALLEY.x0 + 2.2, 0, ALLEY.z1 - 2.6));
      // sky reflection map for the street
      G.street = st;
      people.populateStreet(ST, isTouch ? 6 : 9).catch(e => console.warn('street people', e));
      // preload the first bosses + zombies
      loadChar('tiger'); loadChar('zombie'); loadChar('tiger2'); loadChar('fant'); loadChar('musc');
      return st;
    })();
    return streetPromise;
  }
  let cryptPromise = null;
  function loadCrypt() {
    if (cryptPromise) return cryptPromise;
    cryptPromise = (async () => { const c = await buildCrypt({ scene, world, A, font: fontStack, logos }); loadChar('mino'); loadChar('demon'); G.crypt = c; return c; })();
    return cryptPromise;
  }

  let villaPromise = null;
  function loadVilla() {
    if (villaPromise) return villaPromise;
    villaPromise = (async () => { const v = await buildVilla({ scene, world, A, onStatus: (m) => ui.loading(true, m) }); G.villa = v; return v; })();
    return villaPromise;
  }
  const placePromises = {};
  function loadPlace(k) {
    if (!placePromises[k]) placePromises[k] = buildPlace(PLACES[k], { scene, world, A, onStatus: (m) => ui.loading(true, m) }).then(p => { (G.places ||= {})[k] = p; return p; });
    return placePromises[k];
  }
  // ---------------- travel between the city, the villa and the gang turfs ("rewiry") ----------------
  function clearFoes() {
    for (const z of [...G.zombies]) despawn(z); G.zombies = [];
    for (const a of [...actors]) if (a.thug) despawn(a);
    if (G.boss) { if (actors.includes(G.boss)) despawn(G.boss); G.boss = null; G.lock = null; ui.boss(null); G.spawningBoss = false; }
    G.gang = null;
  }
  const DEST_TITLE = { street: 'Ulica BRAXHORN', villa: 'Willa' };
  async function travel(dest) {
    if (G.travelling || dest === G.chapter) { ui.showMap(false); return; }
    G.travelling = true; ui.showMap(false);
    await ui.fade(true);
    let target = null;
    try {
      if (dest === 'villa') { ui.loading(true, 'Jadę do willi…'); target = await loadVilla(); }
      else if (PLACES[dest]) {
        ui.loading(true, 'Jadę: ' + PLACES[dest].title + '…');
        const d = PLACES[dest];
        [target] = await Promise.all([loadPlace(dest), ...[...new Set(d.waves.flat().concat(d.boss.person))].map(n => people.template(n)), people.loadClips(), loadChar('brax')]);
        if (d.boss.bat && !G.batModel) await loadStreet();
      } else if (!G.street) { ui.loading(true, 'Ładuję ulicę…'); await loadStreet(); }
    } catch (e) { console.error(e); ui.loading(false); await ui.fade(false); G.travelling = false; ui.banner('', 'Nie udało się załadować miejsca.', 2); return; }
    ui.loading(false);
    clearFoes(); dropCaseIfAny();
    if (G.street) G.street.group.visible = false; gym.root.visible = false; if (G.crypt) G.crypt.group.visible = false;
    if (G.villa) { G.villa.group.visible = false; G.villa.region.off = true; }
    for (const p of Object.values(G.places || {})) { p.group.visible = false; p.region.off = true; }
    if (G.traffic) G.traffic.enabled = false;
    if (partner) { partner.root.visible = false; partner.col.top = -10; }
    env.sky.visible = true;
    G.chapter = dest; G.place = PLACES[dest] ? target : null;
    player.vy = 0; player.onGround = true; player.backToLoco(0); player.knock.set(0, 0); player.speed = 0;
    if (dest === 'street') {
      G.street.group.visible = true; gym.root.visible = true; if (G.traffic) G.traffic.enabled = true;
      player.pos.set(G.myCar.x + 1.5, 0, ST.near + 1.8); player.y = 0.15; player.heading = Math.PI / 2;
      cam.yaw = -Math.PI / 2 - 0.72; sound.setRoom('street'); sound.playMusic('street');
      ui.objective(G.cryptOpen ? HINTS.crypt : HINTS.street);
    } else if (dest === 'villa') {
      const v = target; v.group.visible = true; v.region.off = false;
      player.pos.set(v.spawn.x, 0, v.spawn.z); player.y = world.groundAt(v.spawn.x, v.spawn.z, v.spawn.y + 1.0); player.heading = Math.PI;
      cam.yaw = 0; sound.setRoom('street'); sound.loop('city', 0); sound.playMusic('street');
      ui.objective('Twoja willa. Kanapa: odpoczynek. Laptop w kuchni: sklep. Porsche: mapa miasta.');
    } else {
      const p = target; p.group.visible = true; p.region.off = false;
      player.pos.set(p.spawn.x, 0, p.spawn.z); player.y = p.spawn.y + 0.05; player.heading = p.face;
      cam.yaw = p.face + Math.PI; sound.setRoom(p.def.room); sound.loop('city', p.def.mood === 'outdoor' ? 0.3 : 0); sound.playMusic('street');
      startGang(p);
    }
    cam.target.set(player.pos.x, player.y + 1.6, player.pos.z); cam.pitch = 0.3; cam.snap = true; cam.lastManual = -1e9;
    store.set('lastPlace', dest);
    await ui.fade(false);
    if (dest === 'villa') ui.banner('WILLA', 'Dom ' + G.hero.name + (G.hero.key === 'nyxa' ? 'y' : 'a'), 2.4);
    else if (PLACES[dest]) ui.banner(PLACES[dest].title.toUpperCase(), PLACES[dest].sub, 3);
    else ui.banner('', DEST_TITLE.street, 1.6);
    G.travelling = false;
  }
  const goVilla = () => travel('villa'), leaveVilla = () => travel('street');
  function openMap() {
    const turf = (k) => store.get('turf_' + k, false);
    const items = [
      { key: 'street', title: 'Ulica BRAXHORN', sub: 'Siłownia, bossowie Czerwonego Rogu, w nocy zarażeni.', tag: G.cryptOpen ? 'Krypta otwarta' : 'Bossowie: ' + Math.min(G.bossIdx, BOSSES.length) + '/' + BOSSES.length },
      { key: 'villa', title: 'Willa', sub: 'Odpoczynek i sklep BRAXHORN.', tag: 'Dom' },
      ...Object.values(PLACES).map(d => ({ key: d.key, title: d.title, sub: d.sub, tag: turf(d.key) ? 'Zdobyty · gang wraca' : 'Nagroda $' + (d.reward + d.boss.reward).toLocaleString('pl-PL'), done: turf(d.key) })),
    ];
    keys.clear(); joy.active = false; joy.x = joy.y = 0; held.block = false; if (player) player.input.mag = 0;
    ui.showMap(true, items, G.chapter);
  }
  ui.onTravel = (k) => travel(k);
  function dropCaseIfAny() { if (G.carry) dropCase(); if (G.caseJob) { if (G.caseJob.obj.parent) G.caseJob.obj.parent.remove(G.caseJob.obj); G.caseJob = null; G.carry = false; } }

  // ---------------- gang fights on the turfs ----------------
  function startGang(p) {
    const d = p.def, done = store.get('turf_' + d.key, false);
    G.gang = { p, d, wave: 0, t: 2.5, alive: [], bossSpawned: false, bossDead: false, finished: false, again: done };
    ui.objective(done ? 'Gang wrócił na rewir. Pogoń ich jeszcze raz.' : 'Rozbij gang: trzy fale, potem ich szef.');
  }
  async function spawnWave(gg) {
    const d = gg.d, names = d.waves[gg.wave], spots = gg.p.gang;
    gg.spawning = true;
    try {
      const fs = await Promise.all(names.map((n, i) => {
        const s = spots[Math.min(spots.length - 1, i + gg.wave)];
        return spawnThug(n, { pos: [s.x + rand(-0.4, 0.4), s.y, s.z + rand(-0.6, 0.6)], heading: gg.p.face + Math.PI, hp: 52 + gg.wave * 8, size: rand(0.94, 1.04), dmg: 0.7 + gg.wave * 0.06, aggro: 0.7 + gg.wave * 0.1, moves: gg.wave === 2 ? ['jab', 'hook', 'kick', 'kicks'] : ['jab', 'hook', 'kick'], label: 'Czerwony Róg' });
      }));
      gg.alive.push(...fs);
    } finally { gg.spawning = false; }
    ui.banner('FALA ' + (gg.wave + 1), gg.wave === 0 ? 'Idą na ciebie.' : gg.wave === 1 ? 'Następni.' : 'Ostatni przed szefem.', 1.6);
    gg.wave++;
  }
  async function spawnTurfBoss(gg) {
    const b = gg.d.boss, s = gg.p.gang[1];
    gg.spawning = true;
    try {
      const f = await spawnThug(b.person, { pos: [s.x, s.y, s.z], heading: gg.p.face + Math.PI, hp: b.hp, size: b.size, dmg: b.dmg, aggro: 1.0, moves: b.moves, label: b.name });
      f.boss = true; f.def = { ...b, id: 'turf', aggro: 1.0 };
      if (b.bat && G.batModel) {
        const m = G.batModel.clone(); m.scale.setScalar(1 / (SCALE * f.k) * 0.95); m.rotation.set(Math.PI / 2, 0, 0); m.position.set(0, 9, 2.5);
        f.attach(m, 'mixamorigRightHand'); const tip = new THREE.Object3D(); tip.position.set(0, 0.85, 0); m.add(tip); f.addLimb('CLUB', tip); f.weapon = true;
      }
      gg.bossSpawned = true; gg.alive.push(f);
      bossIntro(f);
    } finally { gg.spawning = false; }
  }
  function updateGang(dt) {
    const gg = G.gang; if (!gg || gg.finished || gg.spawning || G.travelling) return;
    gg.alive = gg.alive.filter(f => !f.dead);
    if (gg.alive.length) return;
    gg.t -= dt; if (gg.t > 0) return;
    gg.t = 2.2;
    if (gg.wave < gg.d.waves.length) spawnWave(gg);
    else if (!gg.bossSpawned) spawnTurfBoss(gg);
    else {
      gg.finished = true;
      const bonus = Math.round(gg.d.reward * (gg.again ? 0.4 : 1));
      addMoney(bonus); sound.play('cash', { gain: 0.8 }); sound.playMusic('street');
      ui.banner(gg.again ? 'REWIR OCZYSZCZONY' : 'REWIR ZDOBYTY', '+$' + bonus.toLocaleString('pl-PL') + ' od miasta za spokój', 3.2);
      store.set('turf_' + gg.d.key, true);
      ui.objective('Wróć do auta przy wejściu (JEDŹ), żeby wybrać kolejne miejsce.');
    }
  }
  async function rest() {
    await ui.fade(true); env.setHour(env.hour + 6); player.hp = player.maxHp; updateBars(); await new Promise(r => setTimeout(r, 400)); await ui.fade(false);
    ui.banner('', 'Wypoczęty. Minęło 6 godzin.', 2);
  }

  // ---------------- pickups: cash, briefcase, bat ----------------
  const cashMat = addLampField(new THREE.MeshStandardMaterial({ color: 0x3c8a3a, roughness: 0.6, emissive: 0x153a12, emissiveIntensity: 0.6 }));
  const bandMat = new THREE.MeshStandardMaterial({ color: 0xf2e6b8, roughness: 0.5 });
  const haloTex = TX.radial('rgba(255,255,255,1)', 'rgba(255,255,255,0)');
  function makeCash() {
    const g = new THREE.Group();
    for (let i = 0; i < 3; i++) { const b = new THREE.Mesh(new THREE.BoxGeometry(0.32, 0.05, 0.16), cashMat); b.position.y = i * 0.055; b.rotation.y = i * 0.25; g.add(b); const band = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.055, 0.165), bandMat); band.position.copy(b.position); band.rotation.y = b.rotation.y; g.add(band); }
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: haloTex, color: new THREE.Color(0.35, 1.0, 0.25), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.6 })); s.scale.setScalar(0.9); s.position.y = 0.1; g.add(s);
    return g;
  }
  function dropCash(x, z, total, y = 0) {
    const n = Math.min(4, Math.max(1, Math.round(total / 60)));
    for (let i = 0; i < n; i++) {
      const o = makeCash(); const a = Math.random() * Math.PI * 2, r = 0.4 + Math.random() * 0.8;
      o.position.set(x + Math.cos(a) * r, y + 0.25, z + Math.sin(a) * r); scene.add(o);
      G.pickups.push({ kind: 'cash', obj: o, value: Math.round(total / n), t: 0, vy: 3 + Math.random() * 2, base: y });
    }
  }
  function makeCase() {
    const g = new THREE.Group();
    const body = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.36, 0.13), addLampField(new THREE.MeshStandardMaterial({ color: 0x1a1a1c, metalness: 0.6, roughness: 0.3 })));
    body.position.y = 0.18; g.add(body);
    for (const sx of [-1, 1]) { const c = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.37, 0.135), new THREE.MeshStandardMaterial({ color: 0xc0c4c8, metalness: 1, roughness: 0.25 })); c.position.set(sx * 0.24, 0.18, 0); g.add(c); }
    const h = new THREE.Mesh(new THREE.TorusGeometry(0.07, 0.015, 6, 12, Math.PI), new THREE.MeshStandardMaterial({ color: 0x111, metalness: 0.5 })); h.position.y = 0.36; g.add(h);
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: haloTex, color: new THREE.Color(1.0, 0.85, 0.3), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.7 })); s.scale.setScalar(1.6); s.position.y = 0.2; g.add(s); g.userData.halo = s;
    return g;
  }
  function spawnBatPickup(p) {
    const o = new THREE.Group(); const m = G.batModel.clone(); m.rotation.z = Math.PI / 2 - 0.2; m.position.y = 0.08; o.add(m);
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: haloTex, color: new THREE.Color(1, 0.35, 0.25), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0.6 })); s.scale.setScalar(1.4); s.position.set(0, 0.15, 0); o.add(s);
    o.position.copy(p); scene.add(o);
    G.pickups.push({ kind: 'bat', obj: o, t: 0 });
  }
  function equipBat() {
    if (G.bat) return;
    const m = G.batModel.clone();
    // bone space is in rig units: scale the bat up, grip in the fist, barbed end pointing away from the thumb
    const s = 1 / (SCALE * player.k);
    m.scale.setScalar(s * 0.95); m.rotation.set(Math.PI / 2, 0, 0); m.position.set(0, 9, 2.5);
    player.attach(m, 'mixamorigRightHand');
    const tip = new THREE.Object3D(); tip.position.set(0, 0.85, 0); m.add(tip);
    player.addLimb('CLUB', tip);
    G.bat = { obj: m, hits: 16 };
    ui.weapon('PAŁKA', G.bat.hits);
  }
  function dropBat() { if (!G.bat) return; G.bat.obj.parent.remove(G.bat.obj); G.bat = null; ui.weapon(null); }

  // ---------------- combat ----------------
  const stats = { combo: 0, comboT: 0, punchAlt: 0, danceAlt: 0 };
  let timeScale = 1, slowT = 0;
  const sparks = [];
  const sparkTex = TX.radial('rgba(255,236,200,1)', 'rgba(255,140,60,0)');
  for (let i = 0; i < 12; i++) { const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: sparkTex, blending: THREE.AdditiveBlending, transparent: true, depthWrite: false })); s.visible = false; s.renderOrder = 10; scene.add(s); sparks.push({ s, t: 1 }); }
  const spark = (p, big) => { const k = sparks.find(x => x.t >= 1) || sparks[0]; k.t = 0; k.big = big; k.s.position.copy(p); k.s.visible = true; };
  const inRing = (f) => f.y > RING.h - 0.15 && Math.abs(f.pos.x - RING.x) < RING.half && Math.abs(f.pos.z - RING.z) < RING.half;
  const foes = (a, b) => a !== b && a.team !== b.team && !(a.team === 'enemy' && b.team === 'enemy');
  function nearestTarget(f, maxD) {
    let best = null, bd = maxD;
    for (const o of actors) if (o.root.visible && foes(f, o) && !['ko', 'down'].includes(o.state) && Math.abs(o.y - f.y) < 0.8) { const d = flat(o, f); if (d < bd) { bd = d; best = { x: o.pos.x, z: o.pos.z, f: o }; } }
    if (G.chapter === 'gym') for (const b of gym.bags) { const d = Math.hypot(b.col.x - f.pos.x, b.col.z - f.pos.z); if (d < bd && f.y < 0.5) { bd = d; best = { x: b.col.x, z: b.col.z, bag: b }; } }
    return best;
  }
  function playerAttack(k) {
    const t = nearestTarget(player, 2.8);
    player.faceTarget = t ? { x: t.x, z: t.z } : null;
    if (player.doMove(k)) { sound.whoosh(k !== 'jab', player.pos); if (Math.random() < 0.3) sound.play('grunt', { gain: 0.35, pos: player.pos }); }
  }
  const segDist = (p, x, y0, y1, z) => { const cy = Math.max(y0, Math.min(p.y, y1)); return Math.hypot(p.x - x, p.y - cy, p.z - z); };
  function resolveStrike(ev) {
    const a = ev.by; if (!a.limbs[ev.limb]) return;
    const p = a.limbWorld(ev.limb);
    const reach = ev.weapon ? 0.55 : 0.34;
    for (const t of actors) {
      if (!t.root.visible || !foes(a, t) || ['ko', 'down', 'pair', 'getup'].includes(t.state)) continue;
      const d = segDist(p, t.pos.x, t.y + 0.35 * t.k, t.y + 2.05 * t.k, t.pos.z) - t.radius;
      if (d > reach) continue;
      const dir = new THREE.Vector3(t.pos.x - a.pos.x, 0, t.pos.z - a.pos.z).normalize();
      const zone = p.y - t.y > 1.45 * t.k ? 'head' : 'body';
      let dmg = ev.dmg * (a.dmgMul || 1) * (t.team === 'hero' && t.armor ? t.armor : 1);
      const res = t.hit(zone, dmg, dir, a);
      if (!res) continue;
      if (res === 'dodged') { sound.whoosh(false, t.pos); continue; }
      if (res === 'blocked') { sound.block(t.pos); spark(p, false); cam.shake = Math.max(cam.shake, 0.05); updateBars(); continue; }
      spark(p, dmg > 11); sound.punch(dmg / 16, t.pos); cam.shake = Math.max(cam.shake, dmg > 11 ? 0.22 : 0.1);
      if (ev.weapon && a === player && G.bat) { G.bat.hits--; ui.weapon('PAŁKA', G.bat.hits); if (G.bat.hits <= 0) { dropBat(); ui.banner('', 'Pałka pękła!', 1.4); sound.play('crash', { gain: 0.3 }); } }
      if (a === player) { stats.combo++; stats.comboT = 1.6; ui.combo(stats.combo); }
      if (t === player) { ui.flash(); if (Math.random() < 0.5) sound.play('grunt', { gain: 0.45, rate: 0.9, pos: t.pos }); }
      if (t.zombie && Math.random() < 0.5) sound.play('zombie', { gain: 0.5, pos: t.pos, rate: 1.1 });
      updateBars();
      if (res === 'ko') onKO(t, a);
    }
    if (G.chapter === 'gym') for (const b of gym.bags) {
      const d = segDist(p, b.col.x, b.bottomY, b.topY, b.col.z) - b.col.r;
      if (d > 0.3) continue;
      const dir = new THREE.Vector2(b.col.x - a.pos.x, b.col.z - a.pos.z).normalize();
      b.vx += dir.x * ev.force; b.vz += dir.y * ev.force; spark(p, ev.force > 2); sound.bag(ev.force / 2.6, { x: b.col.x, z: b.col.z });
      if (a === player) { stats.combo++; stats.comboT = 1.6; ui.combo(stats.combo); }
    }
    if (G.chapter === 'gym') for (const bl of balls) {
      if (p.distanceTo(bl.pos) - bl.r > 0.35) continue;
      const dir = new THREE.Vector3(bl.pos.x - a.pos.x, 0, bl.pos.z - a.pos.z).normalize(); const k = ev.limb[1] === 'F' ? 7.5 : 4.5;
      bl.vel.set(dir.x * k, ev.limb[1] === 'F' ? 3.2 : 1.5, dir.z * k); sound.thump(0.4, bl.pos);
    }
  }
  function onKO(t, by) {
    slowT = t.boss ? 1.2 : 0.4; sound.thump(1, t.pos);
    if (t === player) {
      ui.banner('LEŻYSZ', G.chapter === 'gym' ? 'Wstajesz…' : 'Wstajesz z resztką sił…', 2.2);
      if (G.carry) dropCase();
      player.downTime = 1.6;
      player.onState = (s) => { if (s === 'loco' && player.hp <= player.maxHp * 0.5) { player.hp = player.maxHp * 0.6; updateBars(); player.onState = null; } };
      return;
    }
    if (t.team === 'spar') { ui.banner('NOKAUT!', t.name + ' leży. Zaraz wstanie.'); sound.bell(2); return; }
    // enemies stay down for good
    t.downTime = Infinity; t.dead = true; G.kills++;
    if (t.zombie) { dropCash(t.pos.x, t.pos.z, ZOMBIE.reward * rand(0.7, 1.4)); t.fadeT = 3.5; if (Math.random() < 0.7) sound.play('zombie', { gain: 0.6, rate: 0.8, pos: t.pos }); }
    if (t.thug && !t.boss) { dropCash(t.pos.x, t.pos.z, 45 * rand(0.8, 1.5), t.y); t.fadeT = 5; }
    if (t.boss) bossDefeated(t);
  }
  function updateBars() {
    if (player) ui.hp('me', player.hp / player.maxHp);
    if (G.boss) ui.bossHp(G.boss.hp / G.boss.maxHp);
    else if (partner && G.chapter === 'gym') ui.bossHp(partner.hp / partner.maxHp);
  }

  // ---------------- throw (paired animation) ----------------
  const TS = 1.8;
  function throwTarget() {
    if (!player || player.state !== 'loco' || !player.onGround) return null;
    let best = null, bd = 2.2;
    for (const o of actors) if (o !== player && foes(player, o) && o.has('Shoulder Throw, Victim') && ['loco', 'attack', 'emote'].includes(o.state) && Math.abs(player.y - o.y) < 0.3 && !o.noThrow) { const d = flat(player, o); if (d < bd) { bd = d; best = o; } }
    return best;
  }
  function startThrow(victim) {
    const Am = META['Shoulder Throw, Aggressor'], V = META['Shoulder Throw, Victim'];
    const ta = player.trajAt(Am.traj, TS), tv = player.trajAt(V.traj, TS);
    const Ha = [Am.start[0] + ta[0], Am.start[1] + ta[1]], Hv = [V.start[0] + tv[0], V.start[1] + tv[1]];
    const theta = Math.atan2(victim.pos.x - player.pos.x, victim.pos.z - player.pos.z) - Math.atan2(Hv[0] - Ha[0], Hv[1] - Ha[1]);
    const c = Math.cos(theta), s = Math.sin(theta);
    const R = (x, z) => [x * c + z * s, -x * s + z * c];
    const P = player.pos.clone();
    if (inRing(player)) {
      const nA = Am.traj.length - 2, nV = V.traj.length - 2;
      const endA = R(Am.traj[nA] - ta[0], Am.traj[nA + 1] - ta[1]), endV = R(V.start[0] + V.traj[nV] - Ha[0], V.start[1] + V.traj[nV + 1] - Ha[1]);
      const lim = RING.half - 0.75; let sx = 0, sz = 0;
      for (const e of [endA, endV]) { const ex = P.x + e[0] * SCALE, ez = P.z + e[1] * SCALE; const cx = Math.max(RING.x - lim, Math.min(RING.x + lim, ex)), cz = Math.max(RING.z - lim, Math.min(RING.z + lim, ez)); if (Math.abs(cx - ex) > Math.abs(sx)) sx = cx - ex; if (Math.abs(cz - ez) > Math.abs(sz)) sz = cz - ez; }
      P.x += sx; P.z += sz;
    }
    const ra = R(Am.start[0], Am.start[1]), rv = R(Am.start[0] + ta[0] - tv[0], Am.start[1] + ta[1] - tv[1]);
    player.pos.set(P.x - ra[0] * SCALE, P.y, P.z - ra[1] * SCALE);
    victim.pos.set(P.x - rv[0] * SCALE, victim.pos.y, P.z - rv[1] * SCALE);
    for (const [f, other] of [[player, victim], [victim, player]]) { f.stopOverlays(0.12); f.setState('pair'); f.heading = theta; f.ghost = true; f.partner = other; f.faceTarget = null; f.speed = 0; f.vel.set(0, 0); f.knock.set(0, 0); }
    player.play('Shoulder Throw, Aggressor', { t0: TS, fade: 0.15, hold: true, onEnd: () => endThrow(victim) });
    victim.play('Shoulder Throw, Victim', { t0: TS, fade: 0.15, hold: true });
    sound.whoosh(true, player.pos); sound.play('grunt', { gain: 0.5, pos: player.pos });
    setTimeout(() => { sound.thump(1, victim.pos); cam.shake = 0.35; spark(victim.hipsWorld(), true); }, (3.25 - TS) * 1000);
  }
  function endThrow(victim) {
    const kipHead = Math.atan2(-1.0, -32.2), kipStart = [-0.1, 2.4];
    const hp = player.hipsWorld(); player.heading = player.facingFromHips(); player.pos.x = hp.x; player.pos.z = hp.z; player.ghost = false; player.partner = null;
    player.stopOverlays(0); player.setState('loco');
    const vh = victim.hipsWorld(), hd = victim.limbWorld('HD');
    const th = Math.atan2(hd.x - vh.x, hd.z - vh.z) - kipHead, c = Math.cos(th), s = Math.sin(th);
    victim.heading = th; victim.pos.x = vh.x - (kipStart[0] * c + kipStart[1] * s) * SCALE; victim.pos.z = vh.z - (-kipStart[0] * s + kipStart[1] * c) * SCALE;
    if (victim.y > RING.h - 0.2) { const lim = RING.half - 0.55; victim.pos.x = Math.max(RING.x - lim, Math.min(RING.x + lim, victim.pos.x)); victim.pos.z = Math.max(RING.z - lim, Math.min(RING.z + lim, victim.pos.z)); }
    victim.ghost = false; victim.partner = null; victim.stopOverlays(0);
    victim.hp = Math.max(0, victim.hp - 24 * (player.dmgMul || 1)); updateBars();
    if (victim.hp <= 0 && victim.team === 'enemy') { victim.setState('down'); victim.play(victim.moves.ko.clip, { t0: 2.2, fade: 0.1, hold: true }); onKO(victim, player); return; }
    if (victim.hp <= 0) victim.hp = 1;
    victim.setState('getup');
    victim.play(MOVES.getup.clip, { rate: 1.05, fade: 0, onEnd: () => { victim.invuln = 0.8; victim.backToLoco(0.3); } });
    stats.combo += 3; stats.comboT = 1.8; ui.combo(stats.combo);
  }

  // ---------------- door to the alley ----------------
  const DCX = (DOOR.x0 + DOOR.x1) / 2, DZ = HALL.maxZ;
  const door = gym.door; door.state = 'closed';
  function nearDoor() {
    if (!player || player.y > 0.3) return null;
    const dx = Math.abs(player.pos.x - DCX), dz = player.pos.z - DZ;
    if (dx > 1.4) return null;
    if (dz < 0 && dz > -2.2) return 'in';
    if (dz > 0.6 && dz < 2.8) return 'out';
    return null;
  }
  function useDoor() {
    const side = nearDoor(); if (!side || player.state !== 'loco') return false;
    if (door.state === 'open') { door.target = 0; door.state = 'closing'; return true; }
    if (door.state !== 'closed') return true;
    const push = side === 'in';
    player.pos.x = DCX + (push ? 0.15 : 0.55); player.pos.z = push ? DZ - 0.85 : DZ + 0.6 + 1.05;
    player.heading = push ? 0 : Math.PI; player.speed = 0; player.vel.set(0, 0); player.faceTarget = null;
    const m = push ? MOVES.doorPush : MOVES.doorPull;
    player.setState('door');
    door.use = { o: player.play(m.clip, { rate: m.rate, fade: 0.2, onEnd: () => player.backToLoco(0.3) }), win: push ? [1.25, 3.1] : [1.9, 3.7] };
    door.state = 'opening'; setTimeout(() => sound.play('door', { gain: 0.7, pos: { x: DCX, z: DZ } }), 700);
    return true;
  }
  function updateDoor(dt) {
    if (door.state === 'opening' && door.use) {
      const o = door.use.o, [a, b] = door.use.win; const k = Math.max(0, Math.min(1, (o.t - a) / (b - a)));
      door.target = k * k * (3 - 2 * k);
      if (player.state !== 'door') { door.target = 1; door.state = 'open'; door.use = null; }
    }
    if (door.state === 'closing') { const inGap = actors.some(f => Math.abs(f.pos.x - DCX) < 1.2 && Math.abs(f.pos.z - (DZ + 0.3)) < 0.9); if (inGap) door.target = Math.max(door.target, 0.5); else if (door.angle < 0.02) door.state = 'closed'; }
    door.angle += (door.target - door.angle) * Math.min(1, dt * (door.state === 'opening' ? 20 : 3.5));
    door.pivot.rotation.y = -1.75 * door.angle;
    gym.doorCol.top = door.angle < 0.15 ? DOOR.h : -10;
  }

  // ---------------- AI ----------------
  function aiSpar(o, dt) {
    const p = player; o.ai ||= { cd: 1.5, stepCd: 0, tauntCd: 4 };
    const ai = o.ai; o.input.mag = 0; ai.cd -= dt; ai.stepCd -= dt; ai.tauntCd -= dt;
    if (o.state !== 'loco') return;
    const pin = inRing(p) && ['loco', 'attack', 'emote', 'jump', 'hit', 'block', 'dodge'].includes(p.state);
    const d = flat(o, p);
    if (!pin) {
      o.setIdle(d < 9 ? 'Boxing' : 'Boxing (2)');
      const cx = RING.x - o.pos.x, cz = RING.z - 0.6 - o.pos.z;
      if (Math.hypot(cx, cz) > 0.8) { const m = Math.hypot(cx, cz); o.input.x = cx / m; o.input.z = cz / m; o.input.mag = 0.3; o.input.run = false; }
      else o.heading += angDiff(o.heading, Math.atan2(p.pos.x - o.pos.x, p.pos.z - o.pos.z)) * Math.min(1, dt * 3);
      if (d < 7 && ai.tauntCd < 0 && Math.random() < 0.01) { o.faceTarget = p.pos; o.doMove('taunt'); ai.tauntCd = 7; }
      return;
    }
    o.setIdle('Boxing'); o.faceTarget = p.pos;
    brawl(o, dt, { moves: ['jab', 'hook', 'kick', 'kicks'], block: 0.3, dodge: 0.12, cool: 1.6, reach: 2.15 });
  }
  // shared fighting behaviour for sparring partner, bosses and zombies
  function brawl(o, dt, cfg) {
    const p = player, ai = o.ai; const d = flat(o, p);
    if (p.state === 'attack' && p.stateT < 0.15 && d < 2.6) {
      if (!ai.reacted) { ai.reacted = true; const r = Math.random(); if (r < cfg.block && o.startBlock()) { o.blockTimer = 0.9; return; } if (r < cfg.block + cfg.dodge && o.doMove('dodge')) return; }
    } else if (p.state !== 'attack') ai.reacted = false;
    o.heading += angDiff(o.heading, Math.atan2(p.pos.x - o.pos.x, p.pos.z - o.pos.z)) * Math.min(1, dt * 6);
    const reach = cfg.reach * o.k;
    if (d > reach + 1.2) { const k = 1 / d; o.input.x = (p.pos.x - o.pos.x) * k; o.input.z = (p.pos.z - o.pos.z) * k; o.input.mag = cfg.walk ?? (d > 6 ? 0.9 : 0.4); o.input.run = cfg.run ?? d > 6; return; }
    if (d > reach && ai.stepCd < 0 && o.has('Medium Step Forward (1)')) { o.doMove(d > reach + 0.6 ? 'stepl' : 'stepf'); ai.stepCd = 0.5; return; }
    if (d > reach) { const k = 1 / d; o.input.x = (p.pos.x - o.pos.x) * k; o.input.z = (p.pos.z - o.pos.z) * k; o.input.mag = 0.3; o.input.run = false; return; }
    if (d < 1.2 * o.k && ai.stepCd < 0 && o.has('Step Backward')) { o.doMove('stepb'); ai.stepCd = 0.8; return; }
    if (ai.cd < 0 && !['ko', 'down', 'pair'].includes(p.state)) {
      const mv = cfg.moves[Math.floor(Math.random() * cfg.moves.length)];
      if (o.doMove(mv)) { ai.cd = cfg.cool * (0.6 + Math.random() * 0.9); if (o.boss && Math.random() < 0.35) sound.play(o.key === 'demon' ? 'roar' : 'grunt', { gain: o.key === 'demon' ? 0.4 : 0.35, rate: o.key === 'demon' ? 1.2 : 0.75, pos: o.pos }); }
      else ai.cd = 0.3;
    }
  }
  function aiEnemy(o, dt) {
    o.ai ||= { cd: 1.2, stepCd: 0 };
    const ai = o.ai; o.input.mag = 0; ai.cd -= dt; ai.stepCd -= dt;
    if (o.state !== 'loco' || o.dead) return;
    if (!player || ['ko', 'down'].includes(player.state)) { o.setIdle('Idle'); if (o.boss && ai.cd < 0 && Math.random() < 0.01) { o.faceTarget = player.pos; o.doMove('taunt'); ai.cd = 3; } return; }
    o.faceTarget = player.pos;
    if (o.zombie) {
      ai.groanT = (ai.groanT ?? rand(2, 7)) - dt;
      if (ai.groanT < 0) { ai.groanT = rand(4, 9); sound.play('zombie', { gain: 0.45, pos: o.pos }); }
      brawl(o, dt, { moves: ZOMBIE.moves, block: 0, dodge: 0, cool: 1.9, reach: 1.9, walk: 0.32, run: false });
      return;
    }
    const b = o.def;
    o.setIdle(o.has('Boxing') ? 'Boxing' : 'Idle');
    // crowds: only the two closest gang members fight, the rest circle at a distance and wait their turn
    if (o.thug && !o.boss) {
      const d = flat(o, player);
      const closer = actors.filter(a => a !== o && a.team === 'enemy' && !a.dead && a.root.visible && flat(a, player) < d).length;
      if (closer >= 2 && d < 5.5) {
        o.heading += angDiff(o.heading, Math.atan2(player.pos.x - o.pos.x, player.pos.z - o.pos.z)) * Math.min(1, dt * 5);
        if (d < 3.4) { const k = 1 / (d || 1); o.input.x = (o.pos.x - player.pos.x) * k; o.input.z = (o.pos.z - player.pos.z) * k; o.input.mag = 0.25; o.input.run = false; }
        else if (ai.cd < 0 && Math.random() < 0.004) { o.doMove('taunt'); ai.cd = 3; }
        return;
      }
    }
    brawl(o, dt, { moves: b.moves, block: 0.22 * b.aggro, dodge: 0.1, cool: 1.5 / b.aggro, reach: o.weapon ? 2.5 : 2.15 });
  }

  // ---------------- gang members: Mixamo people driven by the hero's fighting animations ----------------
  const thugClips = {};
  async function thugClipSet(T, name) {
    if (thugClips[name]) return thugClips[name];
    const bc = await loadChar('brax');
    const it = bc.clips.find(c => c.name === 'Idle').tracks.find(t => t.name === 'mixamorigHips.position');
    const rest = it ? it.values[1] : 53.6, k = T.hipsY / rest;
    const want = new Set(); T.root.traverse(o => { if (o.isBone) want.add(o.name); });
    thugClips[name] = bc.clips.map(c => new THREE.AnimationClip(c.name, c.duration, c.tracks.filter(t => want.has(t.name.split('.')[0])).map(t => {
      if (!t.name.endsWith('.position')) return t;
      const n = t.clone(); n.values = Float32Array.from(t.values, v => v * k); return n;
    })));
    return thugClips[name];
  }
  let braxHipsM = 0;
  async function spawnThug(name, { pos = [0, 0, 0], heading = 0, hp = 60, size = 1, moves = ['jab', 'hook', 'kick'], aggro = 0.8, dmg = 0.8, label = 'Gang' } = {}) {
    await people.loadClips();
    const T = await people.template(name);
    const clips = await thugClipSet(T, name);
    if (!braxHipsM) { const bc = await loadChar('brax'); const tmp = cloneSkinned(bc.scene); tmp.scale.setScalar(SCALE); tmp.updateMatrixWorld(true); braxHipsM = tmp.getObjectByName('mixamorigHips').getWorldPosition(new THREE.Vector3()).y; }
    const inner = cloneSkinned(T.root); inner.scale.setScalar((braxHipsM / SCALE) / T.hipsWorld);
    const wrap = new THREE.Group(); wrap.add(inner);
    const f = new Fighter({ model: wrap, clips, meta: META, name: label, world, size, moves: null, isPlayer: false });
    f.key = 'thug'; f.team = 'enemy'; f.hp = f.maxHp = hp; f.thug = true;
    f.pos.set(pos[0], 0, pos[2]); f.y = pos[1] || 0; f.heading = heading;
    scene.add(f.root); f.root.position.y = f.y; f.root.rotation.y = heading;
    const b = new THREE.Mesh(new THREE.PlaneGeometry(1.6 * size, 1.6 * size).rotateX(-Math.PI / 2), blobMat); b.renderOrder = 2; scene.add(b); f.blob = b;
    f.def = { moves, aggro, name: label }; f.dmgMul = dmg; f.noRefill = true; f.setIdle('Boxing');
    actors.push(f);
    return f;
  }

  // ---------------- bosses ----------------
  async function spawnBoss(def, x, z, y = 0) {
    const f = await spawn(def.model, { name: def.name, team: 'enemy', size: def.size || 1, hp: def.hp, pos: [x, y, z], heading: -Math.PI / 2 });
    f.boss = true; f.def = def; f.dmgMul = def.dmg; f.speedMul = def.speed; f.noRefill = true;
    if (def.weapon === 'club') { const club = META.__club; if (club) { const tip = new THREE.Object3D(); tip.position.fromArray(club.tipLocal); f.attach(tip, 'mixamorigRightHand'); f.addLimb('CLUB', tip); f.weapon = true; } }
    if (def.claws) { f.addLimb('CLUB', f.limbs.RH); }
    if (def.id === 'demon') { f.noThrow = true; f.reactKey = (zone, dmg) => dmg >= 18 ? 'hitHeavy' : 'hitHead'; }
    return f;
  }
  function bossIntro(f) {
    G.boss = f; ui.boss(f.def.name, f.def.title); ui.bossHp(1);
    ui.say(f.def.name, f.def.intro, 4.2);
    sound.playMusic('boss');
    if (f.key === 'demon') sound.play('roar', { gain: 0.9 }); else sound.play('grunt', { gain: 0.5, rate: 0.7, pos: f.pos });
  }
  function bossDefeated(f) {
    const def = f.def;
    ui.banner('POKONANY!', def.name + ' leży.', 2.4); sound.bell(1);
    dropCash(f.pos.x, f.pos.z, def.reward, f.y);
    if (def.outro) setTimeout(() => ui.say(def.name, def.outro, 3), 1400);
    G.boss = null; G.lock = null; ui.boss(null);
    f.fadeT = 6;
    if (G.chapter === 'street') {
      G.bossIdx++;
      sound.playMusic('street');
      if (G.bossIdx >= BOSSES.length) { G.cryptOpen = true; ui.objective(HINTS.crypt); loadCrypt(); }
      else ui.objective(HINTS.street);
    } else if (G.chapter === 'crypt') {
      if (def.id === 'asterion') setTimeout(() => summonDemon(f), 3500);
      else if (def.id === 'demon') setTimeout(() => gameEnd(), 4000);
    }
  }
  async function updateBossTriggers() {
    if (G.chapter !== 'street' || G.boss || G.spawningBoss || G.bossIdx >= BOSSES.length) return;
    const def = BOSSES[G.bossIdx];
    if (player.pos.x > def.x - 12 && player.pos.z > ST.near) {
      G.spawningBoss = true;
      try {
        const z = Math.max(ST.near + 1.2, Math.min(ST.road1 - 0.5, player.pos.z + rand(-1.5, 1.5)));
        const f = await spawnBoss(def, def.x + 4, z);
        G.lock = [def.x - 15, def.x + 10];
        bossIntro(f);
      } finally { G.spawningBoss = false; }
    }
  }
  async function summonDemon(ast) {
    ui.say('', CRYPT_BOSSES[1].intro, 4);
    sound.play('roar', { gain: 1 }); cam.shake = 0.8;
    await new Promise(r => setTimeout(r, 1800));
    const O = G.crypt.origin;
    const f = await spawnBoss(CRYPT_BOSSES[1], O.x + 2.5, O.z, G.crypt.floorY);
    if (ast) ast.fadeT = 0.01;
    bossIntro(f);
  }

  // ---------------- zombies (night) ----------------
  async function spawnZombie() {
    if (!player) return;
    const side = Math.random() < 0.5 ? -1 : 1;
    let x = player.pos.x + side * rand(12, 18); x = Math.max(ST.x0 + 1, Math.min(ST.x1 - 2, x));
    const z = rand(ST.near + 0.8, ST.far - 0.6);
    const f = await spawn('zombie', { name: ZOMBIE.name, team: 'enemy', hp: ZOMBIE.hp, pos: [x, 0, z], heading: side > 0 ? -Math.PI / 2 : Math.PI / 2 });
    f.zombie = true; f.dmgMul = ZOMBIE.dmg; f.speedMul = 0.9; f.noRefill = true; f.koMove = 'dying';
    f.reactKey = (zone, dmg, k) => Math.random() < 0.5 ? 'zhit' : k;
    f.setIdle('Idle');
    G.zombies.push(f);
  }

  // ---------------- briefcase job ----------------
  function startCaseJob() {
    const x = rand(-36, 52), z = rand(ST.near + 1, ST.far - 1);
    const o = makeCase(); o.position.set(x, ST.road0 < z && z < ST.road1 ? 0 : 0.15, z); scene.add(o);
    G.caseJob = { obj: o, state: 'lying', t: 0 };
    ui.banner('WALIZKA', 'Ktoś zgubił walizkę z kasą. Znajdź ją i zanieś do siłowni!', 3);
  }
  function pickCase() { const c = G.caseJob; c.state = 'carried'; c.obj.userData.halo.visible = false; player.attach(c.obj, 'mixamorigLeftHand'); c.obj.scale.setScalar(1 / (SCALE * player.k)); c.obj.position.set(0, 12, 0); c.obj.rotation.set(0, 0, Math.PI / 2); G.carry = true; ui.objective(HINTS.case); sound.play('cash', { gain: 0.4 }); }
  function dropCase() { const c = G.caseJob; if (!c || c.state !== 'carried') return; c.obj.parent.remove(c.obj); c.obj.scale.setScalar(1); c.obj.rotation.set(0, 0, 0); c.obj.position.set(player.pos.x + 0.6, 0.15, player.pos.z); c.obj.userData.halo.visible = true; scene.add(c.obj); c.state = 'lying'; G.carry = false; }
  function deliverCase() { const c = G.caseJob; c.obj.parent.remove(c.obj); G.caseJob = null; G.carry = false; addMoney(500); ui.banner('DOSTARCZONE!', '+$500 za walizkę', 2.4); sound.play('cash', { gain: 0.7 }); ui.objective(G.chapter === 'street' ? HINTS.street : ''); G.caseCd = 50; }
  function addMoney(v) { G.money += v; store.set('money', G.money); ui.money(G.money, v > 0 ? v : 0); }

  // ---------------- crypt ----------------
  async function enterCrypt() {
    if (G.chapter === 'crypt' || G.enteringCrypt) return;
    G.enteringCrypt = true;
    await ui.fade(true);
    ui.loading(true, 'Schodzę do Krypty…');
    const c = await loadCrypt();
    await Promise.all([loadChar('mino'), loadChar('demon')]);
    ui.loading(false);
    // clean up the street
    for (const z of [...G.zombies]) despawn(z); G.zombies = [];
    G.chapter = 'crypt';
    if (G.street) G.street.group.visible = false; gym.root.visible = false; c.group.visible = true;
    if (G.traffic) G.traffic.enabled = false;
    env.sky.visible = false;
    const O = c.origin;
    player.pos.set(O.x - 2.4, 0, O.z); player.y = c.floorY; player.heading = Math.PI / 2; player.backToLoco(0);
    cam.target.set(player.pos.x, player.y + 1.6, player.pos.z); cam.yaw = -Math.PI / 2 - 0.4; cam.snap = true;
    sound.setRoom('crypt'); sound.loop('city', 0); sound.loop('crowd', 0.6);
    const f = await spawnBoss(CRYPT_BOSSES[0], O.x + 2.4, O.z, c.floorY);
    f.heading = -Math.PI / 2;
    await ui.fade(false);
    await ui.chapter(CHAPTERS.crypt);
    ui.objective('Pokonaj Asteriona w oktagonie.');
    bossIntro(f);
    G.enteringCrypt = false;
  }
  async function gameEnd() {
    G.mode = 'ending'; sound.playMusic('street'); sound.loop('crowd', 0.9);
    const best = Math.max(save.best, G.money); store.set('best', best);
    await ui.ending(ENDING, { money: G.money, kills: G.kills, hero: G.hero.name });
    // free play afterwards: back on the street at dawn
    location.reload();
  }

  // ---------------- camera ----------------
  function camForward() { const yaw = cam.mode === '3d' ? cam.yaw : cam.isoYaw; return { f: new THREE.Vector2(-Math.sin(yaw), -Math.cos(yaw)), r: new THREE.Vector2(Math.cos(yaw), -Math.sin(yaw)) }; }
  function updateCamera(dt) {
    let tgt;
    if (G.mode === 'select' || G.mode === 'title') {
      const f = heroModels[selectFocus]; const h = f.hipsWorld();
      if (G.mode === 'title') { tgt = new THREE.Vector3(RING.x, RING.h + 0.6, RING.z + 0.6); cam.yaw += dt * 0.1; cam.pitch = 0.16; cam.dist = 7.2; }
      else { tgt = new THREE.Vector3(RING.x + (selectFocus === 'brax' ? -0.35 : 0.35), RING.h + 0.15, RING.z + 0.6); cam.yaw += angDiff(cam.yaw, 0.0) * Math.min(1, dt * 2); cam.pitch = 0.02; cam.dist = 7.4; }
    } else {
      const h = player.hipsWorld(); tgt = new THREE.Vector3(h.x, player.y + (cam.mode === '3d' ? 1.55 : 1.0) * player.k, h.z);
    }
    cam.target.lerp(tgt, 1 - Math.exp(-dt * 9));
    // street: drift towards a diagonal view along the street when the player is not steering the camera
    if (G.mode === 'play' && cam.mode === '3d' && performance.now() - cam.lastManual > 3500) {
      let want = null;
      if (G.chapter === 'street' && player.pos.z > ST.near - 0.5) want = -Math.PI / 2 - 0.72;
      if (G.place && G.chapter === G.place.def.key) want = G.place.face + Math.PI;
      // crypt: look across the fight (perpendicular to the player-boss line), from the side nearer the cage centre
      if (G.chapter === 'crypt' && G.boss && G.crypt) {
        const ax = G.boss.pos.x - player.pos.x, az = G.boss.pos.z - player.pos.z, al = Math.hypot(ax, az) || 1, O = G.crypt.origin;
        const mx = (G.boss.pos.x + player.pos.x) / 2, mz = (G.boss.pos.z + player.pos.z) / 2;
        const n1 = [-az / al, ax / al], n2 = [az / al, -ax / al];
        const d1 = Math.hypot(mx + n1[0] * 4 - O.x, mz + n1[1] * 4 - O.z), d2 = Math.hypot(mx + n2[0] * 4 - O.x, mz + n2[1] * 4 - O.z);
        const n = d1 < d2 ? n1 : n2; want = Math.atan2(n[0], n[1]);
      }
      if (want !== null) { cam.yaw += angDiff(cam.yaw, want) * Math.min(1, dt * (G.chapter === 'crypt' ? 1.6 : 0.8)); if (G.chapter !== 'crypt') cam.pitch += ((G.place && G.chapter === G.place.def.key ? 0.24 : 0.32) - cam.pitch) * Math.min(1, dt * 0.8); }
    }
    if (G.mode === 'play' && G.chapter === 'crypt' && cam.mode === '3d') { cam.pitch += (Math.max(cam.pitch, 0.86) - cam.pitch) * Math.min(1, dt * 3); cam.dist += (Math.max(cam.dist, 6.4) - cam.dist) * Math.min(1, dt * 3);
    }
    let yaw, pitch, dist, fov;
    if (cam.mode === '3d' || G.mode !== 'play') { yaw = cam.yaw; pitch = cam.pitch; dist = cam.dist; fov = G.mode === 'select' ? 42 : 55; }
    else { yaw = cam.isoYaw; pitch = 0.68; dist = 17; fov = 28; }
    camPos.set(Math.sin(yaw) * Math.cos(pitch), Math.sin(pitch), Math.cos(yaw) * Math.cos(pitch)).multiplyScalar(dist).add(cam.target);
    if (cam.mode === '3d' || G.mode !== 'play') {
      const dir = camPos.clone().sub(cam.target); let k = 1; const o = cam.target;
      for (const b of world.boxes) {
        if (b.top - b.bottom < 2.6 || b.bottom > Math.max(o.y, camPos.y) || b.top < Math.min(o.y, camPos.y)) continue;
        const pad = 0.3; let t0 = 0, t1 = 1, ok = true;
        for (const [oo, dd, lo, hi] of [[o.x, dir.x, b.minX - pad, b.maxX + pad], [o.z, dir.z, b.minZ - pad, b.maxZ + pad]]) {
          if (Math.abs(dd) < 1e-6) { if (oo < lo || oo > hi) { ok = false; break; } continue; }
          let a = (lo - oo) / dd, c = (hi - oo) / dd; if (a > c) [a, c] = [c, a];
          t0 = Math.max(t0, a); t1 = Math.min(t1, c); if (t0 > t1) { ok = false; break; }
        }
        if (ok && t0 > 0) k = Math.min(k, t0);
      }
      const indoor = o.z < HALL.maxZ && G.chapter === 'gym';
      if (indoor && dir.y > 0) k = Math.min(k, (HALL.height - 0.7 - o.y) / dir.y);
      if (G.chapter === 'crypt' && dir.y > 0) k = Math.min(k, (8.8 - o.y) / dir.y);
      const rcol = G.chapter === 'villa' && G.villa ? G.villa.col : G.place && G.chapter === G.place.def.key ? G.place.col : null;
      if (rcol) {
        const col = rcol, L = dir.length(), steps = Math.ceil(L / 0.2);
        for (let i = 2; i <= steps; i++) {
          const t = i / steps, px = o.x + dir.x * t, py = o.y + dir.y * t, pz = o.z + dir.z * t, ci = col.idx(px, pz);
          if (ci >= 0 && col.walls[ci] && col.walls[ci].some(w => w[0] < py + 0.3 && w[1] > py - 0.3)) { k = Math.min(k, Math.max(0.08, (i - 1.5) / steps)); break; }
        }
        const ci = col.idx(o.x, o.z), fl = ci >= 0 ? col.floors[ci] : null;
        if (fl && dir.y > 0) for (const h of fl) if (h > o.y + 0.15 && h < o.y + 4) k = Math.min(k, Math.max(0.08, (h - 0.35 - o.y) / dir.y));
      }
      k = Math.max(0.12, k); camPos.copy(o).addScaledVector(dir, k);
      const d = k * dir.length(); if (d < 2.4) camPos.y += (2.4 - d) * 0.85;
      camPos.y = Math.max((player && G.mode === 'play' ? player.y : 0) + 0.35, camPos.y);
      if (rcol) { const ci = rcol.idx(camPos.x, camPos.z), fl = ci >= 0 ? rcol.floors[ci] : null; if (fl) for (const h of fl) if (h > o.y + 0.3 && h < camPos.y + 0.4) camPos.y = Math.min(camPos.y, h - 0.4); }
    }
    if (cam.shake > 0) { cam.shake = Math.max(0, cam.shake - dt); const a = cam.shake * 0.25; camPos.x += (Math.random() - 0.5) * a; camPos.y += (Math.random() - 0.5) * a; }
    if (cam.snap || camera.position.distanceTo(camPos) > 8) { camera.position.copy(camPos); cam.snap = false; } else camera.position.lerp(camPos, 1 - Math.exp(-dt * 16));
    const cut = gym.cut, sy = Math.sin(yaw), cy = Math.cos(yaw), iso = cam.mode !== '3d' && G.mode === 'play', outside = player && player.pos.z > HALL.maxZ + 0.3;
    cut.ceil.visible = !iso; cut.px.visible = !(iso && sy > 0.3); cut.nx.visible = !(iso && sy < -0.3); cut.pz.visible = !(iso && (cy > 0.3 || outside)); cut.nz.visible = !(iso && cy < -0.3);
    if (Math.abs(camera.fov - fov) > 0.1) { camera.fov += (fov - camera.fov) * Math.min(1, dt * 6); camera.updateProjectionMatrix(); }
    camera.lookAt(cam.target);
    sound.setListener(camera.position.x, camera.position.z, yaw);
  }

  // ---------------- lighting: indoor/outdoor blend + lamp field ----------------
  const lampTmp = [];
  let outF = 0;
  const fogIn = new THREE.Color(0x0a0807);
  function updateLighting(dt, t) {
    const o = env.update(dt);
    let target = 0;
    const placeMood = G.place && G.chapter === G.place.def.key ? G.place.def.mood : null;
    if (player && G.mode === 'play') { if (G.chapter === 'crypt') target = 2; else if (G.chapter === 'villa' || placeMood || player.pos.z > ALLEY.z1 - 1) target = 1; else if (player.pos.z > HALL.maxZ + 0.4) target = 0.6; }
    else target = 0;
    const crypt = target === 2;
    if (!crypt) outF += ((target) - outF) * Math.min(1, dt * 2.5);
    if (crypt) {
      hemi.color.set(0x8a6a50); hemi.groundColor.set(0x120c08); hemi.intensity = 0.55; key.color.set(0xffe2c0); key.intensity = 1.1;
      scene.fog.color.set(0x060403); scene.fog.density = 0.03; scene.background.set(0x020202); lampU.uAmb.value.setRGB(0.32, 0.27, 0.24); renderer.toneMappingExposure = 1.05;
    } else {
      env.outdoorLights(hemi, key);
      const k = outF;
      hemi.color.lerpColors(GYM_HEMI.sky, hemi.color.clone(), k); hemi.groundColor.lerpColors(GYM_HEMI.ground, hemi.groundColor.clone(), k);
      hemi.intensity = GYM_HEMI.i * (1 - k) + hemi.intensity * k;
      key.color.lerpColors(GYM_KEY.c, key.color.clone(), k); key.intensity = GYM_KEY.i * (1 - k) + key.intensity * k;
      scene.fog.color.lerpColors(fogIn, env.fogColor, k); scene.fog.density = 0.011 * (1 - k) + (0.0055 + 0.012 * o.night) * k;
      scene.background.copy(env.fogColor);
      lampU.uAmb.value.copy(env.amb);
      renderer.toneMappingExposure = 1.12 * (1 - k) + (1.0 + 0.15 * o.night) * k;
      if (placeMood === 'indoor') {
        // tunnel: dim daylight from the portals, sodium lamps carry the scene
        hemi.intensity *= 0.35; key.intensity *= 0.15;
        lampU.uAmb.value.copy(env.amb).multiplyScalar(0.32).addScalar(0.07);
        scene.fog.color.copy(env.fogColor).multiplyScalar(0.25); scene.fog.density = 0.028; scene.background.copy(scene.fog.color);
        renderer.toneMappingExposure = 1.1;
      }
    }
    // reflections of the (bright) gym capture on skin/clothes: dim them outdoors at night and in the crypt
    const envK = crypt ? 0.3 : 0.9 * (1 - outF) + (0.32 + 0.58 * o.day) * outF;
    for (const m of charMats) m.envMapIntensity = envK;
    // key light: follows the player; outdoors it comes from the sun/moon
    const ph = player ? player.hipsWorld() : cam.target;
    if (outF > 0.5 && !crypt) { const d = o.elev > -2 ? env.sunDir : env.moonDir; key.position.set(ph.x + d.x * 30, Math.max(8, d.y * 30), ph.z + d.z * 30); }
    else key.position.set(ph.x + 3, 11, ph.z + 4);
    key.target.position.set(ph.x, 0, ph.z); key.target.updateMatrixWorld();
    // lamp field: pick the brightest LAMP_N sources near the camera target
    lampTmp.length = 0;
    const tx = cam.target.x, tz = cam.target.z;
    const add = (s, w) => { if (w <= 0.01) return; const dx = s.pos.x - tx, dz = s.pos.z - tz; const score = w / (1 + (dx * dx + dz * dz) / (s.r * s.r * 4)); lampTmp.push([score, s, w]); };
    if (G.street && G.street.group.visible) {
      for (const s of G.street.lampSources) {
        let w = s.kind === 'street' ? o.lamps : s.kind === 'flash' ? 1 : 0.3 + 0.7 * o.lamps;
        if (s.kind === 'flash') { const ph2 = Math.floor(t * 5 + (s.phase || 0)) % 2; s.col.setRGB(ph2 ? 1.5 : 0, 0, ph2 ? 0 : 1.8); }
        add(s, w);
      }
      if (G.traffic) for (const s of G.traffic.lampSources) add(s, s.w);
    }
    if (G.crypt && G.crypt.group.visible) for (const s of G.crypt.lampSources) add(s, 1);
    if (G.villa && G.villa.group.visible) for (const s of G.villa.lampSources) add(s, s.kind === 'pool' ? 0.4 + 0.6 * o.lamps : o.lamps);
    if (G.place && G.place.group.visible) for (const s of G.place.lampSources) add(s, G.place.def.mood === 'indoor' ? 1 : 0.15 + 0.85 * o.lamps);
    lampTmp.sort((a, b) => b[0] - a[0]);
    for (let i = 0; i < LAMP_N; i++) {
      const e = lampTmp[i];
      if (e) { const s = e[1]; lampU.lampPos.value[i].set(s.pos.x, s.pos.y, s.pos.z, s.r); lampU.lampCol.value[i].set(s.col.r * e[2], s.col.g * e[2], s.col.b * e[2]); if (s.dir) lampU.lampDir.value[i].set(s.dir.x, s.dir.y, s.dir.z, s.cos); else lampU.lampDir.value[i].w = -2; }
      else { lampU.lampPos.value[i].set(0, -500, 0, 1); lampU.lampCol.value[i].set(0, 0, 0); lampU.lampDir.value[i].w = -2; }
    }
    return o;
  }

  // ---------------- gym physics: bags + balls ----------------
  const ballMat = new THREE.MeshStandardMaterial({ map: TX.leather(0x2b211b), roughness: 0.6 });
  const balls = gym.spawnBallsAt.map(([x, z], i) => {
    const r = 0.2 + (i % 2) * 0.03;
    const m = new THREE.Mesh(new THREE.SphereGeometry(r, 20, 14), ballMat); m.castShadow = true; m.receiveShadow = true; gym.root.add(m);
    return { m, r, pos: new THREE.Vector3(x, r, z), vel: new THREE.Vector3(), q: new THREE.Quaternion() };
  });
  function updateBags(dt) {
    for (const b of gym.bags) {
      const w2 = 9.8 / b.L; b.ax = b.ax || 0; b.az = b.az || 0;
      b.vx += (-w2 * b.ax - 0.55 * b.vx) * dt; b.vz += (-w2 * b.az - 0.55 * b.vz) * dt; b.ax += b.vx * dt; b.az += b.vz * dt;
      const lim = b.L * 0.5; b.ax = Math.max(-lim, Math.min(lim, b.ax)); b.az = Math.max(-lim, Math.min(lim, b.az));
      b.group.rotation.z = Math.asin(Math.max(-1, Math.min(1, b.ax / b.L))); b.group.rotation.x = -Math.asin(Math.max(-1, Math.min(1, b.az / b.L)));
      b.col.x = b.group.position.x + b.ax; b.col.z = b.group.position.z + b.az;
    }
  }
  function updateBalls(dt) {
    for (const bl of balls) {
      bl.vel.y -= 18 * dt; bl.pos.addScaledVector(bl.vel, dt);
      if (bl.pos.y < bl.r) { bl.pos.y = bl.r; if (bl.vel.y < -1.5) bl.vel.y *= -0.35; else bl.vel.y = 0; }
      const fr = Math.exp(-(bl.pos.y <= bl.r + 0.01 ? 1.6 : 0.2) * dt); bl.vel.x *= fr; bl.vel.z *= fr;
      const n = world.bounce(bl.pos, bl.r, bl.pos.y - bl.r);
      if (n) { const vn = bl.vel.x * n.x + bl.vel.z * n.z; if (vn < 0) { bl.vel.x -= 1.6 * vn * n.x; bl.vel.z -= 1.6 * vn * n.z; } }
      for (const f of actors) { if (Math.abs(f.y - (bl.pos.y - bl.r)) > 1.0) continue; const dx = bl.pos.x - f.pos.x, dz = bl.pos.z - f.pos.z, rr = f.radius * 0.8 + bl.r, d = Math.hypot(dx, dz); if (d < rr && d > 1e-4) { bl.pos.x = f.pos.x + dx / d * rr; bl.pos.z = f.pos.z + dz / d * rr; const push = Math.max(1.2, Math.hypot(f.vel.x, f.vel.y) * 1.25); bl.vel.x = dx / d * push; bl.vel.z = dz / d * push; } }
      bl.pos.x = Math.max(HALL.minX + 0.3, Math.min(HALL.maxX - 0.3, bl.pos.x)); bl.pos.z = Math.max(HALL.minZ + 0.3, Math.min(HALL.maxZ - 0.3, bl.pos.z));
      const sp = Math.hypot(bl.vel.x, bl.vel.z); if (sp > 0.01) { const ax = new THREE.Vector3(bl.vel.z, 0, -bl.vel.x).normalize(); bl.q.setFromAxisAngle(ax, sp * dt / bl.r); bl.m.quaternion.premultiply(bl.q); }
      bl.m.position.copy(bl.pos);
    }
  }

  // ---------------- context action (door / pick up / crypt) ----------------
  function contextAction() {
    if (!player || G.mode !== 'play') return null;
    if (G.chapter === 'gym' || player.pos.z < ALLEY.z1 + 0.5) { const d = nearDoor(); if (d && player.state === 'loco') return { label: 'DRZWI', fn: useDoor }; }
    for (const p of G.pickups) if (p.kind === 'bat' && !G.bat && Math.hypot(p.obj.position.x - player.pos.x, p.obj.position.z - player.pos.z) < 1.4) return { label: 'WEŹ', fn: () => { scene.remove(p.obj); G.pickups.splice(G.pickups.indexOf(p), 1); equipBat(); sound.play('block', { gain: 0.5 }); ui.banner('', 'Masz kolczastą pałkę! CIOS = zamach.', 2); } };
    if (G.caseJob && G.caseJob.state === 'lying' && G.caseJob.obj.parent === scene && Math.hypot(G.caseJob.obj.position.x - player.pos.x, G.caseJob.obj.position.z - player.pos.z) < 1.3) return { label: 'WEŹ', fn: pickCase };
    if (G.chapter === 'villa' && G.villa) {
      const v = G.villa, d = (p) => Math.hypot(player.pos.x - p.x, player.pos.z - p.z);
      if (d(v.car) < 4) return { label: 'JEDŹ', fn: openMap };
      if (d(v.shop) < 2.5) return { label: 'SKLEP', fn: () => ui.shop(true, SHOP, upg, G.money) };
      if (d(v.sofa) < 2.8) return { label: 'ODPOCZNIJ', fn: rest };
      return null;
    }
    if (G.chapter === 'street' && G.myCar && Math.hypot(player.pos.x - G.myCar.x, player.pos.z - G.myCar.z) < 3.4 && !G.boss) return { label: 'JEDŹ', fn: openMap };
    if (G.place && G.chapter === G.place.def.key && Math.hypot(player.pos.x - G.place.exit.x, player.pos.z - G.place.exit.z) < 3.2) return { label: 'JEDŹ', fn: openMap };
    if (G.chapter === 'street' && Math.hypot(player.pos.x - ST.door.x, player.pos.z - ST.door.z) < 2.2) {
      if (G.cryptOpen) return { label: 'WEJDŹ', fn: enterCrypt };
      return { label: 'ZAMKNIĘTE', fn: () => ui.banner('', 'Drzwi pilnuje Kafar. Najpierw pokonaj bossów na ulicy.', 2) };
    }
    return null;
  }

  // ---------------- main loop ----------------
  const clock = new THREE.Clock();
  let tAll = 0;
  renderer.compile(scene, camera);
  ui.score && ui.score({ ko: 0, bag: 0 });
  let wasStreet = false;

  // ---------------- pause menu (all the settings live here, the HUD stays clean) ----------------
  let menuOpen = false;
  function openMenu() {
    if (menuOpen || G.mode !== 'play' || G.travelling) return;
    menuOpen = true; keys.clear(); joy.active = false; joy.x = joy.y = 0; held.block = false;
    if (player) { player.input.mag = 0; player.input.block = false; }
    ui.showPause(true, isTouch ? '' : 'Esc lub P: powrót do gry');
  }
  function closeMenu() { if (!menuOpen) return; menuOpen = false; ui.showPause(false); clock.getDelta(); }
  function menuAction(a) {
    if (a === 'menu') { if (menuOpen) closeMenu(); else openMenu(); }
    else if (a === 'resume') closeMenu();
    else if (a === 'cam') { cam.mode = cam.mode === '3d' ? '25d' : '3d'; store.set('cam', cam.mode); ui.setCam(cam.mode); }
    else if (a === 'iso-left') { if (cam.mode === '25d') cam.isoYaw += Math.PI / 2; }
    else if (a === 'moves') ui.toggleMoves();
    else if (a === 'help') ui.toggleKeys();
    else if (a === 'timefast') { env.fast = env.fast > 1 ? 1 : 30; ui.setTimeFast(env.fast > 1); }
    else if (a.startsWith('q-')) { applyQuality(a.slice(2)); ui.setQuality(a.slice(2)); if (menuOpen) render(); }
    else if (a === 'orient') { landscape = !(view.rotated || view.w() > view.h()); store.set('land', landscape); resize(); ui.setOrient(view.w() > view.h()); }
    else if (a === 'fullscreen') goFullscreen();
    else if (a === 'sound') { sound.toggle(); store.set('sound', sound.on); ui.setSound(sound.on); }
    else return false;
    return true;
  }
  document.addEventListener('visibilitychange', () => { if (document.hidden) openMenu(); });

  function tick(dtIn) {
    let dt = Math.min(dtIn, 1 / 20); const realDt = dt;
    if (slowT > 0) { slowT -= dt; timeScale = 0.35; } else timeScale += (1 - timeScale) * Math.min(1, dt * 4);
    dt *= timeScale; tAll += dt;
    const playing = G.mode === 'play' && player;

    // actions
    while (actions.length) {
      const a = actions.shift();
      if (a === 'enter' && G.mode === 'title') { ui.onTitle(); continue; }
      if (menuAction(a)) continue;
      if (!playing) continue;
      if (a === 'jump') { if (!G.carry) player.jump(); }
      else if (a === 'punch') { if (G.bat) playerAttack(['bat1', 'bat2', 'bat3'][stats.punchAlt++ % 3]); else playerAttack(stats.punchAlt++ % 3 === 2 ? 'hook' : 'jab'); }
      else if (a === 'hook') playerAttack('hook');
      else if (a === 'kick') playerAttack('kick');
      else if (a === 'kicks') playerAttack('kicks');
      else if (a === 'combo') playerAttack('combo');
      else if (a === 'special') { const sp = G.hero.specials; playerAttack(sp[G.specialIdx++ % sp.length]); }
      else if (a === 'dodge') { const t = nearestTarget(player, 3); player.faceTarget = t ? { x: t.x, z: t.z } : null; if (player.doMove('dodge', { force: player.state === 'emote' })) sound.whoosh(true, player.pos); }
      else if (a === 'act' || a === 'door') { const c = contextAction(); if (c) c.fn(); else if (G.carry) { dropCase(); } }
      else if (a === 'throw') { const v = throwTarget(); if (v) startThrow(v); else ui.banner('', 'Podejdź bliżej przeciwnika, żeby rzucić.', 1.4); }
      else if (['taunt', 'warmup', 'shadow', 'guard', 'idle', 'stepf', 'stepb', 'stepl', 'breakdance'].includes(a)) { player.faceTarget = null; if (player.state === 'emote' && player.lastMove === a) player.backToLoco(0.3); else player.doMove(a, { force: player.state === 'emote' }); }
      else if (a === 'dance') { player.doMove(stats.danceAlt++ % 2 ? 'dance2' : 'dance', { force: player.state === 'emote' }); }
      else if (a === 'shake') useShake();
    }

    const o = updateLighting(realDt, tAll);
    if (playing) {
      // input relative to camera
      let ix = 0, iy = 0;
      if (keys.has('w') || keys.has('arrowup')) iy += 1; if (keys.has('s') || keys.has('arrowdown')) iy -= 1;
      if (keys.has('a') || keys.has('arrowleft')) ix -= 1; if (keys.has('d') || keys.has('arrowright')) ix += 1;
      let mag = Math.min(1, Math.hypot(ix, iy)), run = !keys.has('shift');
      if (joy.active) { ix = joy.x; iy = joy.y; mag = Math.min(1, Math.hypot(ix, iy)); run = true; }
      const { f, r } = camForward();
      const wx = f.x * iy + r.x * ix, wz = f.y * iy + r.y * ix, wl = Math.hypot(wx, wz) || 1;
      player.input.x = wx / wl; player.input.z = wz / wl; player.input.mag = mag; player.input.run = run && !G.carry;
      if (player.state === 'emote' && mag > 0.25) player.backToLoco(0.25);
      player.input.block = held.block || keys.has('c');
      if (player.input.block && ['loco', 'emote'].includes(player.state)) { const t = nearestTarget(player, 3); player.faceTarget = t ? { x: t.x, z: t.z } : null; player.startBlock(); }
      updateDoor(dt);
      if (player.state === 'loco') player.faceTarget = null;
      const near = actors.some(a2 => foes(player, a2) && !a2.dead && flat(player, a2) < 5);
      player.setIdle(near ? 'Boxing' : 'Idle');
      // chapter transitions
      if (G.chapter === 'gym' && player.pos.z > ALLEY.z1 - 0.2) {
        if (!G.street) { if (!G.waitStreet) { G.waitStreet = true; ui.loading(true, 'Ładuję ulicę…'); loadStreet().then(() => { ui.loading(false); G.waitStreet = false; }); } player.pos.z = ALLEY.z1 - 0.3; }
        else { G.chapter = 'street'; if (!G.streetShown) { G.streetShown = true; ui.chapter(CHAPTERS.street); } ui.objective(G.cryptOpen ? HINTS.crypt : HINTS.street); sound.setRoom('street'); partner.root.visible = false; partner.col.top = -10; G.caseCd = 25; }
      } else if (G.chapter === 'street' && player.pos.z < ALLEY.z1 - 1.0 && player.pos.x > ALLEY.x0 && player.pos.x < ALLEY.x1) {
        G.chapter = 'gym'; sound.setRoom('gym'); partner.root.visible = true; partner.col.top = partner.y + partner.height;
        if (G.carry) deliverCase();
      }
    }
    // AI
    if (partner && G.chapter === 'gym' && G.mode === 'play' && partner.root.visible) aiSpar(partner, dt);
    for (const a of actors) if (a.team === 'enemy') aiEnemy(a, dt);
    for (const a of actors) a.update(dt);
    for (const a of actors) if (!Number.isFinite(a.pos.x + a.pos.z + a.y + a.heading)) { a.pos.set(player ? player.pos.x + 2 : 0, 0, player ? player.pos.z : 0); a.y = 0; a.heading = 0; a.vel.set(0, 0); a.knock.set(0, 0); a.vy = 0; a.onGround = true; a.stopOverlays(0); a.setState('loco'); }
    if (!Number.isFinite(cam.target.x + cam.target.y + cam.target.z + cam.yaw + cam.pitch)) { cam.target.set(0, 1.6, 0); cam.yaw = 0; cam.pitch = 0.3; }
    // keep the sparring partner in the ring
    if (partner && partner.state !== 'pair' && partner.y > RING.h - 0.2) { partner.pos.x = Math.max(RING.x - RING.half + 0.5, Math.min(RING.x + RING.half - 0.5, partner.pos.x)); partner.pos.z = Math.max(RING.z - RING.half + 0.5, Math.min(RING.z + RING.half - 0.5, partner.pos.z)); }
    if (partner && G.mode === 'play' && partner.root.visible && partner.y < RING.h - 0.3 && partner.onGround && partner.state === 'loco' && G.chapter === 'gym') { partner.pos.set(RING.x, 0, RING.z - 0.6); partner.y = RING.h; }
    // boss arena lock + crypt cage
    if (G.lock && player) player.pos.x = Math.max(G.lock[0], Math.min(G.lock[1], player.pos.x));
    if (G.chapter === 'crypt' && G.crypt) for (const a of actors) if (a.root.visible) G.crypt.clampInside(a);
    for (const a of actors) { for (const ev of a.events) resolveStrike(ev); a.events.length = 0; }
    if (player && player.landed && player.landed < -7) { cam.shake = 0.12; sound.thump(0.5, player.pos); }
    if (player) player.landed = null;
    // fading out defeated enemies
    for (const a of [...actors]) if (a.fadeT !== undefined && a.dead) { a.fadeT -= dt; if (a.fadeT < 1.2) a.root.position.y = a.y - (1.2 - a.fadeT) * 0.8; if (a.fadeT <= 0) { despawn(a); const zi = G.zombies.indexOf(a); if (zi >= 0) G.zombies.splice(zi, 1); } }

    if (G.chapter === 'gym' || !player) { updateBags(dt); updateBalls(dt); }
    for (const a of gym.animated) a(dt);
    if (gym.neon) gym.neon.material.opacity = 0.85 + 0.15 * Math.sin(tAll * 23) * (Math.sin(tAll * 1.3) > 0.97 ? 1 : 0.1);
    for (const k of sparks) if (k.t < 1) { k.t += dt * 4; k.s.scale.setScalar((k.big ? 0.9 : 0.55) * (0.4 + k.t)); k.s.material.opacity = 1 - k.t; if (k.t >= 1) k.s.visible = false; }
    for (const a of actors) { const h = a.hipsWorld(); a.blob.visible = a.root.visible; a.blob.position.set(h.x, world.groundAt(h.x, h.z, a.y + 0.1) + 0.012, h.z); a.blob.scale.setScalar(1 + Math.max(0, a.y - world.groundAt(a.pos.x, a.pos.z, a.y)) * 0.4); }

    // street systems
    if (G.street && (G.chapter === 'street' || G.chapter === 'gym')) {
      G.street.update(dt, o, tAll);
      if (G.traffic) G.traffic.update(dt, actors.filter(a => a.root.visible), o, (f, car, dir) => {
        if (f.state === 'ko' || f.state === 'down') return;
        const dmg = f.isPlayer ? 22 : 45;
        f.hp = Math.max(0, f.hp - dmg); f.stopOverlays(0.05); f.ko(dir); f.knock.set(dir.x, dir.z).multiplyScalar(9);
        sound.play('crash', { gain: 0.8, pos: f.pos }); cam.shake = 0.4; spark(f.hipsWorld(), true);
        if (f.isPlayer) { ui.flash(); ui.banner('', 'Uważaj na auta!', 1.4); if (G.carry) dropCase(); f.downTime = 1.6; if (f.hp <= 0) f.hp = f.maxHp * 0.4; }
        else if (f.hp <= 0) onKO(f, null);
        updateBars();
      }, tAll);
    }
    if (G.crypt && G.chapter === 'crypt') G.crypt.update(dt, tAll);
    if (G.villa && G.chapter === 'villa') G.villa.update(dt, o, tAll);
    if (G.place && G.chapter === G.place.def.key) { G.place.update(dt, o, tAll); updateGang(dt); if (player.y < G.place.spawn.y - 6) { player.pos.set(G.place.spawn.x, 0, G.place.spawn.z); player.y = G.place.spawn.y + 0.1; player.vy = 0; player.onGround = true; cam.snap = true; } }
    if (people.list.length && (G.chapter === 'street' || G.chapter === 'gym')) {
      const threats = [];
      for (const a of actors) if (a.team === 'enemy' && !a.dead && a.root.visible) threats.push({ x: a.pos.x, z: a.pos.z, r: 8 });
      if (player && (player.state === 'attack' || G.boss)) threats.push({ x: player.pos.x, z: player.pos.z, r: 6 });
      people.update(dt, { threats, camPos: camera.position, night: o.night, bounds: { x0: ST.x0, x1: ST.x1 - 2 } });
    }
    // night: zombies
    if (playing && G.chapter === 'street') {
      updateBossTriggers();
      const night = o.night > 0.55;
      if (G.nightWas !== null && night !== G.nightWas) ui.banner(night ? 'NOC' : 'ŚWIT', night ? HINTS.night : HINTS.day, 3);
      G.nightWas = night;
      G.zT = (G.zT ?? 8) - dt;
      const alive = G.zombies.filter(z => !z.dead).length;
      if (night && !G.boss && G.zT < 0 && alive < (isTouch ? 2 : 3)) { G.zT = rand(14, 24); spawnZombie(); }
      if (!night) for (const z of G.zombies) if (!z.dead) { z.dead = true; z.fadeT = 2.5; z.stopOverlays(0.2); }
      // briefcase jobs
      G.caseCd = (G.caseCd ?? 30) - dt;
      if (!G.caseJob && G.caseCd < 0) startCaseJob();
      if (G.caseJob && G.caseJob.state === 'lying') G.caseJob.obj.rotation.y += dt;
    }
    // pickups
    for (const p of [...G.pickups]) {
      p.t += dt;
      if (p.kind === 'cash') {
        if (p.vy !== undefined) { p.obj.position.y += p.vy * dt; p.vy -= 12 * dt; if (p.obj.position.y < p.base + 0.15) { p.obj.position.y = p.base + 0.15; p.vy = undefined; } }
        p.obj.rotation.y += dt * 2;
        if (player && p.t > 0.6) { const dx = player.pos.x - p.obj.position.x, dz = player.pos.z - p.obj.position.z, d = Math.hypot(dx, dz); if (d < 2.6) { p.obj.position.x += dx * Math.min(1, dt * 6); p.obj.position.z += dz * Math.min(1, dt * 6); } if (d < 0.6) { scene.remove(p.obj); G.pickups.splice(G.pickups.indexOf(p), 1); addMoney(p.value); sound.play('cash', { gain: 0.5 }); } }
        if (p.t > 60) { scene.remove(p.obj); G.pickups.splice(G.pickups.indexOf(p), 1); }
      } else if (p.kind === 'bat') p.obj.rotation.y += dt * 0.8;
    }
    // sound beds
    if (playing) {
      const outside = G.chapter === 'street' ? 1 : G.chapter === 'gym' ? Math.max(0, Math.min(1, (player.pos.z - HALL.maxZ) / 9)) * 0.6 : 0;
      sound.loop('city', 0.55 * outside);
      const nc = G.traffic && G.chapter === 'street' ? G.traffic.nearest(player.pos.x, player.pos.z) : null;
      if (nc) sound.loop('engine', Math.max(0, 1 - nc.d / 25) * 0.7 * outside, 0.6 + nc.car.v / 14); else sound.loop('engine', 0);
      const pol = G.traffic && G.chapter === 'street' ? G.traffic.cars.find(c => c.siren) : null;
      sound.loop('siren', pol ? Math.max(0, 1 - Math.hypot(pol.x - player.pos.x, pol.z - player.pos.z) / 60) * 0.35 : 0);
      const tr = G.street && G.chapter === 'street' && G.street.train;
      sound.loop('train', tr && tr.obj.visible ? Math.max(0, 1 - Math.hypot(G.street.viaduct.X0 + 4.8 - player.pos.x, tr.z + 30 - player.pos.z) / 110) * 0.8 : 0, 0.9);
    }

    if (stats.comboT > 0) { stats.comboT -= dt; if (stats.comboT <= 0) { stats.combo = 0; ui.combo(0); } }
    if (player && player.state === 'loco' && player.hp < player.maxHp && !G.boss) { player.hp = Math.min(player.maxHp, player.hp + dt * (G.chapter === 'gym' ? 6 : 1.2)); updateBars(); }
    if (partner && partner.state === 'loco' && partner.hp < partner.maxHp && player && !inRing(player)) { partner.hp = Math.min(partner.maxHp, partner.hp + dt * 6); updateBars(); }
    if (playing) {
      const c = contextAction(); ui.action(c ? c.label : null);
      ui.throwReady(!!throwTarget()); ui.shakeNeeded(player.hp < player.maxHp * 0.7);
      ui.clock(env.clock, o.night > 0.5);
      if (!G.boss) ui.showOpp(G.chapter === 'gym' && (flat(player, partner) < 10 || inRing(player)));
      if (G.chapter === 'gym' && !G.sparHint && inRing(player)) { G.sparHint = true; ui.banner('SPARING', isTouch ? 'CIOS, KOP, SPECJAL i RZUT po prawej' : 'J cios · K kop · I specjal · E rzut', 2.4); sound.bell(1); }
      if (G.chapter === 'street' && G.street && !G.batHint && G.pickups.some(p => p.kind === 'bat' && flat(player, { pos: p.obj.position }) < 6)) { G.batHint = true; ui.banner('', HINTS.bat, 2.6); }
      store.set('hour', env.hour);
    }
    updateCamera(realDt);
    ui.tick(realDt);
  }
  const jv = new THREE.Vector3();
  let hurtT = 0;
  ui.flash = () => { hurtT = 1; };
  function render() {
    env.sky.position.copy(camera.position);
    const gu = grade.uniforms; gu.uTime.value = tAll;
    const low = player && G.mode === 'play' && player.hp < player.maxHp * 0.3 ? 0.35 + 0.15 * Math.sin(tAll * 6) : 0;
    hurtT = Math.max(0, hurtT - 0.045); gu.uHurt.value = Math.max(low, hurtT * 0.9);
    gu.uVig.value = G.chapter === 'crypt' ? 0.55 : 0.42;
    if (floorReflect && G.chapter !== 'crypt' && (!player || player.pos.z < ALLEY.z1 + 6)) floor.update(renderer, scene, camera);
    composer.render();
  }
  let paused = false, frameErrors = 0;
  function frame() {
    requestAnimationFrame(frame);
    const d = clock.getDelta(); if (paused) return;
    if (menuOpen) { while (actions.length) menuAction(actions.shift()); return; }
    if (!$('map').hidden) { actions.length = 0; return; }
    try { tick(d); render(); }
    catch (e) { frameErrors++; if (frameErrors === 1 || frameErrors % 300 === 0) console.error(e); }
  }
  window.__mg = {
    THREE, A, G, env, world, actors, scene, renderer, progs: () => renderer.info.programs, get player() { return player; }, get cam() { return cam; }, camera, sound,
    sim(sec, ks = []) { ks.forEach(k => keys.add(k)); for (let i = 0; i < Math.round(sec * 60); i++) tick(1 / 60); ks.forEach(k => keys.delete(k)); },
    render, act: (a) => actions.push(a), pause(v) { paused = v; }, joy, openMenu, closeMenu,
    start: (k) => startGame(k), title: () => ui.onTitle(), spawnThug, people, loadPlace, travel, openMap, killFoes() { for (const a of [...actors]) if (a.team === 'enemy' && !a.dead) { a.hp = 0; a.ko(new THREE.Vector3(0, 0, -1)); onKO(a, player); } }, goVilla, leaveVilla, loadVilla, buy, upg, loadStreet, loadCrypt, enterCrypt, spawnZombie, setHour: (h) => env.setHour(h),
    tp(x, z) { player.pos.set(x, 0, z); cam.snap = true; },
  };
  ui.setSound(sound.on); ui.setQuality(qKey); ui.setCam(cam.mode); ui.setOrient(view.w() > view.h());
  frame();
}

const MOVE_LIST = [
  ['Ataki', [['punch', 'Prosty + krzyżowy', 'J'], ['hook', 'Sierpowy', 'F'], ['kick', 'Kopnięcie MMA', 'K'], ['kicks', 'Seria kopnięć', 'U'], ['combo', 'Kombinacja kopnięć', 'L'], ['special', 'Cios specjalny postaci', 'I'], ['throw', 'Rzut przez bark (przy rywalu)', 'E']]],
  ['Obrona', [['block', 'Blok (przytrzymaj)', 'C'], ['dodge', 'Unik', 'X']]],
  ['Ruch', [['jump', 'Skok', 'Spacja'], ['act', 'Akcja: drzwi, podnieś, wejdź', 'O'], ['stepf', 'Krok bokserski w przód'], ['stepl', 'Długi wypad'], ['stepb', 'Krok w tył']]],
  ['Pokaz', [['taunt', 'Prowokacja', 'T'], ['warmup', 'Rozgrzewka', 'R'], ['guard', 'Garda'], ['dance', 'Taniec', 'G'], ['breakdance', 'Breakdance (Nyxa)']]],
  ['Świat', [['timefast', 'Przyspiesz dzień i noc', 'N'], ['cam', 'Kamera 3D / 2.5D', 'V']]],
];

const start = () => boot().catch(err => {
  console.error(err);
  const el = document.getElementById('loadMsg');
  if (el) el.textContent = 'Nie udało się uruchomić gry: ' + String(err && err.message ? err.message : err).slice(0, 160);
});
if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start); else start();
