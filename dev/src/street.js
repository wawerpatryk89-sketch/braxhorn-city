// The street outside the gym: photogrammetry scans on the far side, procedural shop facades on the near side,
// road, sidewalks, street lamps, signs with the BRAXHORN brand, a police cordon (west) and the railway viaduct
// with the door to the crypt (east). Everything outdoors is lit by the shared lamp field (env.js).
import * as THREE from 'three';
import { mergeGeometries } from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import { addLampField } from './env.js';
import * as TX from './textures.js';
import { signBoard } from './citytex.js';
import { floatGeo } from './bake.js';

export const ST = {
  near: 20.6, road0: 24, road1: 33, far: 35.5, x0: -44.2, x1: 61.4,
  laneWB: 26.9, laneEB: 30.1, crossX: -52, viaductX: 68, door: { x: 61.2, z: 22.3 },
};

const rnd = (() => { let s = 1234567; return () => { s = (s * 16807) % 2147483647; return s / 2147483647; }; })();

// ---------- canvas helpers ----------
function canvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }
function ctex(c, srgb = true, repeat = false) { const t = new THREE.CanvasTexture(c); if (srgb) t.colorSpace = THREE.SRGBColorSpace; if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = 4; return t; }

// facade: brick or render wall with windows; emissive map with randomly lit windows
export function facadeTextures(w, h, style, shop) {
  const PX = 40, W = Math.round(w * PX), H = Math.round(h * PX);
  const c = canvas(W, H), x = c.getContext('2d'), e = canvas(W, H), ex = e.getContext('2d');
  const hc = canvas(W, H), hx = hc.getContext('2d'); hx.fillStyle = '#808080'; hx.fillRect(0, 0, W, H); // relief: 128 = wall
  ex.fillStyle = '#000'; ex.fillRect(0, 0, W, H);
  const base = { brick: '#6e3a2a', brick2: '#5b3426', render: '#8d8578', render2: '#6f7479', dark: '#3b3b40' }[style] || '#6e3a2a';
  x.fillStyle = base; x.fillRect(0, 0, W, H);
  if (style.startsWith('brick')) {
    const bh = 0.075 * PX, bw = 0.23 * PX;
    for (let r = 0; r * bh < H; r++) for (let k = -1; k * bw < W; k++) {
      const v = 0.75 + rnd() * 0.4; x.fillStyle = `rgba(${150 * v | 0},${78 * v | 0},${58 * v | 0},0.55)`;
      x.fillRect(k * bw + (r % 2) * bw / 2 + 1, r * bh + 1, bw - 2, bh - 2);
      hx.fillStyle = '#5a5a5a'; hx.fillRect(k * bw + (r % 2) * bw / 2, r * bh, bw, 1); hx.fillRect(k * bw + (r % 2) * bw / 2, r * bh, 1, bh);
    }
  } else {
    for (let i = 0; i < 2600; i++) { x.fillStyle = `rgba(0,0,0,${rnd() * 0.06})`; x.fillRect(rnd() * W, rnd() * H, 2 + rnd() * 30, 2 + rnd() * 30); }
  }
  // grime streaks
  for (let i = 0; i < 40; i++) { const gx = rnd() * W; const g = x.createLinearGradient(0, 0, 0, H); g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.18)'); x.fillStyle = g; x.fillRect(gx, rnd() * H * 0.5, 4 + rnd() * 18, H); }
  // cornice lines
  x.fillStyle = 'rgba(30,26,24,0.55)'; x.fillRect(0, H - 4.3 * PX, W, 0.25 * PX); x.fillRect(0, 0, W, 0.35 * PX);
  // upper windows
  const floors = Math.floor((h - 4.6) / 3.2), cols = Math.max(2, Math.floor(w / 2.6));
  const colW = W / cols;
  for (let f = 0; f < floors; f++) for (let k = 0; k < cols; k++) {
    const wx = k * colW + colW * 0.25, ww = colW * 0.5, wy = H - (4.6 + f * 3.2 + 2.2) * PX, wh = 1.6 * PX;
    x.fillStyle = '#1b1d22'; x.fillRect(wx - 3, wy - 3, ww + 6, wh + 6);
    const lit = rnd();
    const glass = x.createLinearGradient(wx, wy, wx + ww * 0.6, wy + wh); glass.addColorStop(0, '#8fa3b8'); glass.addColorStop(0.45, '#4d5d70'); glass.addColorStop(1, '#1d242e');
    x.fillStyle = glass; x.fillRect(wx, wy, ww, wh);
    x.fillStyle = 'rgba(255,255,255,0.10)'; x.beginPath(); x.moveTo(wx + ww * 0.15, wy); x.lineTo(wx + ww * 0.45, wy); x.lineTo(wx + ww * 0.1, wy + wh); x.lineTo(wx - ww * 0.2, wy + wh); x.fill(); // sky glint
    if (rnd() < 0.5) { x.fillStyle = `rgba(${200 + rnd() * 40 | 0},${190 + rnd() * 40 | 0},${170 + rnd() * 40 | 0},0.55)`; x.fillRect(wx, wy, ww * (0.2 + rnd() * 0.3), wh); } // curtain
    x.fillStyle = '#c9c2b4'; x.fillRect(wx - 4, wy + wh + 2, ww + 8, 5); // sill
    x.fillStyle = '#2a2a2e'; x.fillRect(wx + ww / 2 - 1.5, wy, 3, wh);
    // relief: recessed window, raised frame + sill
    hx.fillStyle = '#b0b0b0'; hx.fillRect(wx - 5, wy - 5, ww + 10, wh + 10);
    hx.fillStyle = '#303030'; hx.fillRect(wx, wy, ww, wh);
    hx.fillStyle = '#d8d8d8'; hx.fillRect(wx - 6, wy + wh + 2, ww + 12, 6);
    if (lit > 0.42) {
      const col = lit > 0.92 ? '#7fb6ff' : lit > 0.8 ? '#ffd9a0' : '#ffb35c';
      ex.fillStyle = col; ex.globalAlpha = 0.55 + rnd() * 0.45; ex.fillRect(wx, wy, ww, wh); ex.globalAlpha = 1;
      if (rnd() > 0.5) { ex.fillStyle = 'rgba(0,0,0,0.6)'; ex.fillRect(wx + ww * rnd() * 0.6, wy, ww * 0.25, wh); } // curtain / person
    }
  }
  // shop front
  const sy = H - 4.0 * PX;
  x.fillStyle = '#16181b'; x.fillRect(0.4 * PX, sy + 0.2 * PX, W - 0.8 * PX, 3.8 * PX);
  const sg = x.createLinearGradient(0, sy, 0, H); sg.addColorStop(0, '#7d90a0'); sg.addColorStop(0.35, '#3f4d58'); sg.addColorStop(1, '#151b21');
  x.fillStyle = sg; x.fillRect(0.6 * PX, sy + 1.1 * PX, W - 1.2 * PX, 2.9 * PX);
  for (let r = 0; r < 3; r++) { const y = sy + (1.9 + r * 0.62) * PX; x.fillStyle = 'rgba(20,24,28,0.5)'; x.fillRect(0.6 * PX, y + 16, W - 1.2 * PX, 3); for (let i = 0; i < (W - 1.2 * PX) / 9; i++) { const v = 60 + rnd() * 60 | 0; x.fillStyle = `rgba(${v + 40},${v + 25},${v},0.22)`; x.fillRect(0.6 * PX + i * 9, y + 4 + rnd() * 4, 6, 12); } } // shelves
  x.fillStyle = 'rgba(255,255,255,0.08)'; for (let k = 0; k < 3; k++) { const gx = 0.6 * PX + (W - 1.2 * PX) * (0.15 + k * 0.3); x.beginPath(); x.moveTo(gx, sy + 1.1 * PX); x.lineTo(gx + 30, sy + 1.1 * PX); x.lineTo(gx - 20, H); x.lineTo(gx - 50, H); x.fill(); }
  hx.fillStyle = '#3a3a3a'; hx.fillRect(0.6 * PX, sy + 1.1 * PX, W - 1.2 * PX, 2.9 * PX);
  x.fillStyle = '#0b0c0e'; for (let k = 1; k < 4; k++) x.fillRect(0.6 * PX + (W - 1.2 * PX) * k / 4 - 2, sy + 1.1 * PX, 4, 2.9 * PX);
  if (shop) {
    const gx0 = 0.6 * PX, gy0 = sy + 1.1 * PX, gw = W - 1.2 * PX, gh = 2.9 * PX;
    const g = ex.createLinearGradient(0, gy0, 0, gy0 + gh); g.addColorStop(0, shop.glow || '#ffcf8a'); g.addColorStop(1, 'rgba(40,30,20,1)');
    ex.globalAlpha = 0.38; ex.fillStyle = g; ex.fillRect(gx0, gy0, gw, gh); ex.globalAlpha = 1;
    ex.fillStyle = 'rgba(0,0,0,0.7)';
    for (let k = 0; k < 3; k++) ex.fillRect(gx0, gy0 + gh * (0.38 + 0.2 * k), gw, 5);
    for (let i = 0; i < gw / 9; i++) { ex.fillStyle = `rgba(0,0,0,${0.3 + rnd() * 0.5})`; ex.fillRect(gx0 + i * 9 + rnd() * 3, gy0 + gh * (0.25 + 0.2 * Math.floor(rnd() * 3)), 5, 10 + rnd() * 10); }
    ex.fillStyle = '#000'; for (let k = 1; k < 4; k++) ex.fillRect(gx0 + gw * k / 4 - 3, gy0, 6, gh);
    ex.fillRect(W * 0.5 - 0.55 * PX, H - 2.4 * PX, 1.1 * PX, 2.4 * PX);
  }
  // door
  x.fillStyle = '#0a0a0a'; x.fillRect(W * 0.5 - 0.55 * PX, H - 2.4 * PX, 1.1 * PX, 2.4 * PX);
  return { map: ctex(c), emissiveMap: null, normalMap: null }; // baked light: no relief map, no lit windows (day only)
}

// height canvas -> tangent-space normal map (Sobel)
function heightToNormal(hc, k = 2) {
  const W = hc.width, H = hc.height, src = hc.getContext('2d').getImageData(0, 0, W, H).data;
  const out = canvas(W, H), ox = out.getContext('2d'), img = ox.createImageData(W, H), d = img.data;
  const h = (x, y) => src[(Math.min(H - 1, Math.max(0, y)) * W + Math.min(W - 1, Math.max(0, x))) * 4] / 255;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const dx = (h(x + 1, y) - h(x - 1, y)) * k, dy = (h(x, y + 1) - h(x, y - 1)) * k;
    const l = Math.hypot(dx, dy, 1), o = (y * W + x) * 4;
    d[o] = (-dx / l * 0.5 + 0.5) * 255; d[o + 1] = (dy / l * 0.5 + 0.5) * 255; d[o + 2] = (1 / l * 0.5 + 0.5) * 255; d[o + 3] = 255;
  }
  ox.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(out); t.anisotropy = 4; return t;
}

export function paverTexture() {
  const S = 256, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = '#6b6862'; x.fillRect(0, 0, S, S);
  const n = 4;
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) {
    const v = 0.85 + rnd() * 0.25; x.fillStyle = `rgb(${120 * v | 0},${116 * v | 0},${108 * v | 0})`;
    x.fillRect(i * S / n + 2, j * S / n + 2, S / n - 4, S / n - 4);
  }
  for (let i = 0; i < 900; i++) { x.fillStyle = `rgba(0,0,0,${rnd() * 0.12})`; x.fillRect(rnd() * S, rnd() * S, 1 + rnd() * 6, 1 + rnd() * 6); }
  const t = ctex(c, true, true); return t;
}

function textSign(font, text, color, { w = 1024, h = 256, size = 150, bg = null } = {}) {
  const c = canvas(w, h), x = c.getContext('2d');
  if (bg) { x.fillStyle = bg; x.fillRect(0, 0, w, h); }
  x.font = `800 ${size}px ${font}`; x.textAlign = 'center'; x.textBaseline = 'middle';
  x.shadowColor = color; x.shadowBlur = 4; x.fillStyle = color; x.fillText(text, w / 2, h / 2 + 6);
  return ctex(c);
}

// ---------- car material diet: merge untextured parts per car into one vertex-coloured mesh ----------
export function slimCar(root) {
  root.updateMatrixWorld(true);
  const inv = new THREE.Matrix4().copy(root.matrixWorld).invert();
  const solid = [], glass = [], lights = [], keep = [];
  const tmpM = new THREE.Matrix4(), col = new THREE.Color();
  root.traverse(o => {
    if (!o.isMesh) return;
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    const m = mats[0];
    const isGlass = (m.transmission > 0.05) || (m.transparent && m.opacity < 0.95) || /glass|window|windshield/i.test(m.name || '');
    const isLight = /light|lamp|led|glow|emiss/i.test(m.name || '') || (m.emissive && m.emissive.getHSL({ h: 0, s: 0, l: 0 }).l > 0.2);
    tmpM.multiplyMatrices(inv, o.matrixWorld);
    if (m.map && !isGlass) { keep.push(o); return; }
    const g = new THREE.BufferGeometry();
    const src = o.geometry.index ? o.geometry.toNonIndexed() : o.geometry;
    const p = src.attributes.position, n = src.attributes.normal, cnt = p.count;
    const P = new Float32Array(cnt * 3), N = new Float32Array(cnt * 3), C = new Float32Array(cnt * 3);
    const v = new THREE.Vector3(), nm = new THREE.Matrix3().getNormalMatrix(tmpM);
    col.copy(m.color || new THREE.Color(0.5, 0.5, 0.5));
    if (isLight && m.emissive) col.add(m.emissive);
    for (let i = 0; i < cnt; i++) {
      v.fromBufferAttribute(p, i).applyMatrix4(tmpM); P[i * 3] = v.x; P[i * 3 + 1] = v.y; P[i * 3 + 2] = v.z;
      if (n) { v.fromBufferAttribute(n, i).applyMatrix3(nm).normalize(); N[i * 3] = v.x; N[i * 3 + 1] = v.y; N[i * 3 + 2] = v.z; }
      C[i * 3] = col.r; C[i * 3 + 1] = col.g; C[i * 3 + 2] = col.b;
    }
    g.setAttribute('position', new THREE.BufferAttribute(P, 3));
    g.setAttribute('normal', new THREE.BufferAttribute(N, 3));
    g.setAttribute('color', new THREE.BufferAttribute(C, 3));
    if (!n) g.computeVertexNormals();
    (isGlass ? glass : isLight ? lights : solid).push(g);
  });
  const out = new THREE.Group();
  const add = (geos, mat) => { if (!geos.length) return null; const mg = mergeGeometries(geos); const mesh = new THREE.Mesh(mg, mat); out.add(mesh); return mesh; };
  const body = add(solid, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.32, metalness: 0.55, envMapIntensity: 1.2 }));
  const gl = add(glass, new THREE.MeshStandardMaterial({ vertexColors: true, color: 0x9fb4c8, roughness: 0.05, metalness: 0.9, transparent: true, opacity: 0.55, envMapIntensity: 1.5, depthWrite: false }));
  const li = add(lights, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.2, emissive: 0xffffff, emissiveIntensity: 0 }));
  for (const o of keep) {
    const m = Array.isArray(o.material) ? o.material[0] : o.material;
    const nm = new THREE.MeshStandardMaterial({ map: m.map, normalMap: m.normalMap || null, color: m.color, roughness: m.roughness ?? 0.5, metalness: m.metalness ?? 0.2, transparent: m.transparent, opacity: m.opacity, alphaTest: m.alphaTest || 0, side: m.side });
    const mesh = new THREE.Mesh(o.geometry, nm); tmpM.multiplyMatrices(inv, o.matrixWorld); mesh.applyMatrix4(tmpM); out.add(mesh);
  }
  out.userData = { body, glass: gl, lights: li };
  out.traverse(o => { if (o.isMesh) { addLampField(o.material, 'lit'); o.castShadow = false; o.receiveShadow = false; } });
  return out;
}

// car definitions: target length (m), extra yaw to make the model's nose point to local +z
export const CARS = {
  car_s4: { len: 4.7, yaw: 0, name: 'Audi S4' }, car_r8: { len: 4.45, yaw: 0, name: 'Audi R8' }, car_rev: { len: 4.95, yaw: 0, name: 'Lamborghini' },
  car_918: { len: 4.65, yaw: 0, name: 'Porsche 918' }, car_mus: { len: 4.6, yaw: 0, name: 'Mustang' }, car_dc: { len: 5.0, yaw: 0, name: 'Challenger' },
  car_dia: { len: 4.5, yaw: 0, name: 'Policja', police: true }, car_mar: { len: 4.7, yaw: 0, name: 'Policja', police: true },
  car_zuk: { len: 4.4, yaw: Math.PI, name: 'Żuk' }, car_gmc: { len: 5.8, yaw: -Math.PI / 2, name: 'GMC' },
};
export function prepareCar(key, gltf) {
  const def = CARS[key];
  const raw = gltf.scene;
  raw.traverse(o => {
    if (!o.isMesh) return;
    for (const m of (Array.isArray(o.material) ? o.material : [o.material])) {
      m.userData.car = true;
      if (m.transmission) m.transmission = 0; // glass transmission = an extra render pass of the scene
      // window glass: dark and nearly opaque (see-through glass from above made the car look roofless)
      if (/glass|window|windshield/i.test(m.name || '') && !/light|lamp|red|orange|tail|indicator/i.test(m.name || '')) {
        m.transparent = true; m.opacity = Math.max(m.opacity, 0.9); m.color && m.color.setRGB(0.02, 0.025, 0.03); m.roughness = 0.08; m.metalness = 0.4; m.depthWrite = true;
      }
    }
  });
  const holder = new THREE.Group(); holder.add(raw);
  raw.rotation.y = def.yaw; holder.updateMatrixWorld(true);
  const b = new THREE.Box3().setFromObject(raw), s = b.getSize(new THREE.Vector3());
  const k = def.len / Math.max(s.x, s.z);
  raw.scale.multiplyScalar(k); holder.updateMatrixWorld(true);
  const b2 = new THREE.Box3().setFromObject(raw), c2 = b2.getCenter(new THREE.Vector3());
  raw.position.sub(new THREE.Vector3(c2.x, b2.min.y, c2.z));
  holder.updateMatrixWorld(true);
  // light anchors (nose = +z): front-most / rear-most vertices of light-ish meshes, split left/right
  const size0 = new THREE.Box3().setFromObject(raw).getSize(new THREE.Vector3());
  const anchors = { hl: [], tl: [] };
  {
    const fr = [], rr = [], v = new THREE.Vector3();
    raw.traverse(o => {
      if (!o.isMesh) return;
      const names = (Array.isArray(o.material) ? o.material : [o.material]).map(m => m.name || '').join(' ') + ' ' + o.name;
      if (!/light|lamp|lgith|glass_red|red_glass|glassred|reflector|emiss|lsight/i.test(names) || /interior|inerior|window|license/i.test(names)) return;
      const red = /red|tail|rear|brake/i.test(names);
      const p = o.geometry.attributes.position, step = Math.max(1, Math.floor(p.count / 3000));
      for (let i = 0; i < p.count; i += step) { v.fromBufferAttribute(p, i).applyMatrix4(o.matrixWorld); if (v.z > size0.z * 0.3 && !red) fr.push(v.clone()); else if (v.z < -size0.z * 0.3) rr.push(v.clone()); }
    });
    const pick = (pts, front) => {
      if (pts.length < 6) return null;
      const ext = front ? Math.max(...pts.map(p => p.z)) : Math.min(...pts.map(p => p.z));
      const near = pts.filter(p => Math.abs(p.z - ext) < 0.45 && p.y > 0.25 && p.y < size0.y * 0.75);
      const side = (sg) => { const q = near.filter(p => Math.sign(p.x) === sg && Math.abs(p.x) > size0.x * 0.12); if (q.length < 2) return null; const c = new THREE.Vector3(); q.forEach(p => c.add(p)); c.divideScalar(q.length); c.z = ext + (front ? 0.03 : -0.03); return c; };
      const L = side(-1), Rr = side(1); return L && Rr ? [L, Rr] : null;
    };
    const def = (front) => [-1, 1].map(sx => new THREE.Vector3(sx * size0.x * 0.36, size0.y * (front ? 0.5 : 0.55), (front ? 1 : -1) * (size0.z / 2 + 0.02)));
    anchors.hl = pick(fr, true) || def(true); anchors.tl = pick(rr, false) || def(false);
  }
  const slim = slimCar(holder);
  slim.userData.anchors = anchors;
  const size = new THREE.Box3().setFromObject(slim).getSize(new THREE.Vector3());
  slim.userData.size = size; slim.userData.key = key; slim.userData.def = def;
  return slim;
}

// ---------- the street ----------
// Plansza 1: the main street runs west-east from the quay (x = XW) under the railway viaduct to the closed gate
// towards the house estate (x = XE). The district around it (quay, far-side frontage, courtyard, shop, club,
// closures) is built in district.js; everything static here is baked (bake.js), nothing glows.
export const XW = -66, XE = 104;
export async function buildStreet({ scene, world, font, A, logos, Q }) {
  const G = new THREE.Group(); G.name = 'street'; scene.add(G);
  const out = { group: G, lampSources: [], glows: [], cones: [], people: [], parked: [], flashers: [], animated: [], neons: [] };
  const L = (pos, col, r, kind, extra = {}) => { const s = { pos: pos.clone ? pos.clone() : new THREE.Vector3(...pos), col: new THREE.Color(col), r, kind, ...extra }; out.lampSources.push(s); return s; };
  const halo = (() => { const c = canvas(128, 128), x = c.getContext('2d'), g = x.createRadialGradient(64, 64, 0, 64, 64, 64);
    for (const [o, a] of [[0, 1], [0.05, 0.92], [0.12, 0.5], [0.25, 0.16], [0.5, 0.045], [1, 0]]) g.addColorStop(o, `rgba(255,255,255,${a})`);
    x.fillStyle = g; x.fillRect(0, 0, 128, 128); return ctex(c, false); })();
  out.halo = halo;

  // ---------- ground: sidewalks + curbs ----------
  const paver = paverTexture();
  const walkMat = new THREE.MeshStandardMaterial({ map: paver, roughness: 0.85, color: 0xd8d4cc });
  const curbMat = new THREE.MeshStandardMaterial({ color: 0x8b8984, roughness: 0.8 });
  const box = (mat, x0, x1, y0, y1, z0, z1, uv = 0) => {
    const g = new THREE.BoxGeometry(x1 - x0, y1 - y0, z1 - z0); g.translate((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2);
    if (uv) { const p = g.attributes.position, u = g.attributes.uv; for (let i = 0; i < p.count; i++) u.setXY(i, p.getX(i) / uv, p.getZ(i) / uv); }
    const m = new THREE.Mesh(g, mat); m.receiveShadow = true; G.add(m); return m;
  };
  const SW = 0.15;
  for (const m of [box(walkMat, XW, XE, -0.2, SW, ST.near - 0.2, ST.road0, 2), box(walkMat, XW, XE, -0.2, SW, ST.road1, ST.far + 0.6, 2),
    box(curbMat, XW, XE, -0.2, SW + 0.01, ST.road0 - 0.25, ST.road0, 0), box(curbMat, XW, XE, -0.2, SW + 0.01, ST.road1, ST.road1 + 0.25, 0),
    box(curbMat, XW, XW + 0.25, -0.2, SW + 0.01, ST.road0, ST.road1, 0)]) m.userData.ground = true;
  world.box(XW, XE, ST.near, ST.road0, SW, -1, 'walk');
  world.box(XW, XE, ST.road1, ST.far + 0.6, SW, -1, 'walk');

  // ---------- road: the tile model merged into one static mesh per material (it takes the baked light) ----------
  const roadG = await A.loadGLTF('road.glb');
  const roadSrc = roadG.scene; roadSrc.updateMatrixWorld(true);
  const rb = new THREE.Box3().setFromObject(roadSrc), rs = rb.getSize(new THREE.Vector3());
  const nTiles = Math.round((XE - XW - 0.25) / 15), tileLen = (XE - XW - 0.25) / nTiles;
  const rsx = (ST.road1 - ST.road0) / rs.x, rsz = tileLen / rs.z;
  const roadParts = new Map();
  for (let i = 0; i < nTiles; i++) {
    const h = new THREE.Group(); const t = roadSrc.clone(); h.add(t);
    t.position.set(-(rb.min.x + rs.x / 2), -rb.max.y, -(rb.min.z + rs.z / 2));
    h.scale.set(rsx, rsx, rsz); h.rotation.y = Math.PI / 2; h.position.set(XW + 0.25 + tileLen * (i + 0.5), 0, (ST.road0 + ST.road1) / 2); h.updateMatrixWorld(true);
    t.traverse(o => { if (!o.isMesh) return; const g = floatGeo(o.geometry).applyMatrix4(o.matrixWorld); const key = o.material.uuid; if (!roadParts.has(key)) roadParts.set(key, { mat: o.material, geos: [] }); roadParts.get(key).geos.push(g.index ? g.toNonIndexed() : g); });
  }
  for (const { mat, geos } of roadParts.values()) { for (const k of ['aoMap', 'lightMap', 'emissiveMap']) mat[k] = null; for (const k of ['map', 'normalMap', 'roughnessMap', 'metalnessMap']) if (mat[k] && (mat[k].channel || 0) !== 0) mat[k] = null; mat.envMapIntensity = 0; mat.roughness = Math.max(mat.roughness ?? 0.8, 0.8); const m = new THREE.Mesh(mergeGeometries(geos), mat); m.receiveShadow = true; m.userData.ground = true; G.add(m); out.roadMat = mat; }

  // ---------- near side: procedural shop buildings (the nutrition shop and the harbour store are in district.js) ----------
  const brickT = TX.brick(512, 512); brickT.map.repeat.set(1, 1);
  const sideMat = new THREE.MeshStandardMaterial({ map: brickT.map, color: 0xb59a8a, roughness: 0.9 });
  const roofMat = new THREE.MeshStandardMaterial({ color: 0x3a3836, roughness: 0.95 });
  const NEAR = [
    { x0: -52, x1: -38, h: 16, s: 'brick', sign: ['APTEKA', '#5fb27a'] },
    { x0: -38, x1: -26, h: 12, s: 'render', sign: ['MONOPOLOWY', '#d39a4a'] },
    { x0: 1, x1: 12, h: 14, s: 'brick', sign: ['BAR 24/7', '#5aa9c9'] },
    { x0: 12, x1: 24, h: 20, s: 'render2', sign: ['KEBAB', '#d6b545'] },
    { x0: 24, x1: 36, h: 13, s: 'dark', sign: ['LOMBARD', '#c96a72'] },
    { x0: 36, x1: 48, h: 17, s: 'render', sign: ['HOTEL', '#d98a5a'] },
    { x0: 48, x1: 61.4, h: 15, s: 'brick2' },
    { x0: 71, x1: 87, h: 14, s: 'render2', sign: ['WARSZTAT', '#9aa3ab'] },
    { x0: 87, x1: XE, h: 16, s: 'brick' },
  ];
  out.buildings = [];
  for (const b of NEAR) {
    const w = b.x1 - b.x0, d = 9, z0 = ST.near - d;
    const ft = facadeTextures(w, b.h, b.s, null);
    const front = new THREE.MeshStandardMaterial({ map: ft.map, roughness: 0.82 });
    const g = new THREE.BoxGeometry(w, b.h, d);
    const uv = g.attributes.uv, p = g.attributes.position, nrm = g.attributes.normal;
    for (let i = 0; i < uv.count; i++) { if (Math.abs(nrm.getZ(i)) < 0.5) uv.setXY(i, (Math.abs(nrm.getX(i)) > 0.5 ? p.getZ(i) : p.getX(i)) / 3, p.getY(i) / 3); }
    const m = new THREE.Mesh(g, [sideMat, sideMat, roofMat, roofMat, front, sideMat]);
    m.position.set((b.x0 + b.x1) / 2, b.h / 2, z0 + d / 2); m.receiveShadow = true; G.add(m);
    world.box(b.x0, b.x1, z0, ST.near, b.h, -1, 'bldg');
    out.buildings.push({ mesh: m, mat: front, b });
    box(roofMat, b.x0, b.x1, b.h, b.h + 0.5, ST.near - 0.35, ST.near);
    if (b.sign) {
      const [text, color] = b.sign;
      const sw = Math.min(w - 1.2, text.length * 0.62 + 1.6);
      const sm = new THREE.Mesh(new THREE.BoxGeometry(sw, sw / 4, 0.12), [roofMat, roofMat, roofMat, roofMat, new THREE.MeshStandardMaterial({ map: signBoard(font, text, { fg: color, bg: '#1a1b1d', size: text.length > 9 ? 120 : 150 }), roughness: 0.7 }), roofMat]);
      sm.position.set((b.x0 + b.x1) / 2, 4.5, ST.near + 0.12); G.add(sm);
    }
  }
  // facade details: ledges + cornices, shop awnings, AC units, fire escapes (merged per material)
  {
    const ledges = [], acs = [], fe = [], aw = {};
    const stoneMat = new THREE.MeshStandardMaterial({ color: 0x8e877c, roughness: 0.85 });
    const acMat = new THREE.MeshStandardMaterial({ color: 0xb9bcbe, roughness: 0.6 });
    const feMat = new THREE.MeshStandardMaterial({ color: 0x2a2e32, roughness: 0.7 });
    const B = (arr, w, h, d, x, y, z, rx = 0) => { const g = new THREE.BoxGeometry(w, h, d); if (rx) g.rotateX(rx); g.translate(x, y, z); arr.push(g); };
    NEAR.forEach((b) => {
      const w = b.x1 - b.x0, cx = (b.x0 + b.x1) / 2, zf = ST.near;
      B(ledges, w - 0.1, 0.22, 0.28, cx, 4.32, zf + 0.12);
      B(ledges, w + 0.1, 0.5, 0.45, cx, b.h - 0.1, zf + 0.2);
      const floors = Math.floor((b.h - 4.6) / 3.2), cols = Math.max(2, Math.floor(w / 2.6));
      for (let f = 0; f < floors; f++) {
        if (f > 0) B(ledges, w - 0.1, 0.1, 0.16, cx, 4.6 + f * 3.2 + 0.25, zf + 0.07);
        for (let k = 0; k < cols; k++) if (rnd() < 0.16) { const ax = b.x0 + (k + 0.5) * w / cols; B(acs, 0.82, 0.56, 0.48, ax + (rnd() - 0.5) * 0.3, 5.2 + 3.2 * f - 0.42, zf + 0.26); B(acs, 0.06, 0.06, 0.5, ax - 0.3, 5.2 + 3.2 * f - 0.74, zf + 0.25); B(acs, 0.06, 0.06, 0.5, ax + 0.3, 5.2 + 3.2 * f - 0.74, zf + 0.25); }
      }
      if (b.s.startsWith('brick') && b.h >= 14 && floors >= 2) {
        const fx = b.x0 + w * 0.3, fw = 3.4, fd = 1.15;
        for (let f = 0; f < floors; f++) {
          const y = 5.0 + 3.2 * f;
          B(fe, fw, 0.06, fd, fx, y, zf + fd / 2);
          B(fe, fw, 0.05, 0.05, fx, y + 0.95, zf + fd); B(fe, 0.05, 0.05, fd, fx - fw / 2, y + 0.95, zf + fd / 2); B(fe, 0.05, 0.05, fd, fx + fw / 2, y + 0.95, zf + fd / 2);
          for (let r = -2; r <= 2; r++) B(fe, 0.03, 0.95, 0.03, fx + r * fw / 4, y + 0.48, zf + fd);
          if (f < floors - 1) { const Ll = Math.hypot(3.2, fw * 0.7); const g = new THREE.BoxGeometry(0.5, 0.05, Ll); g.rotateX(-Math.atan2(3.2, fw * 0.7)); g.rotateY(Math.PI / 2); g.translate(fx, y + 1.6, zf + fd * 0.55); fe.push(g); }
        }
        B(fe, 0.05, 3.0, 0.05, fx + fw / 2 - 0.25, 5.0 - 1.5, zf + fd - 0.1); B(fe, 0.05, 3.0, 0.05, fx + fw / 2 - 0.65, 5.0 - 1.5, zf + fd - 0.1);
      }
      if (b.sign) {
        const col = b.sign[1]; if (!aw[col]) { const c = canvas(64, 128), x = c.getContext('2d'); for (let i = 0; i < 8; i++) { x.fillStyle = i % 2 ? '#d9d4ca' : col; x.fillRect(i * 8, 0, 8, 128); } x.fillStyle = 'rgba(0,0,0,0.25)'; x.fillRect(0, 110, 64, 18); aw[col] = { mat: new THREE.MeshStandardMaterial({ map: ctex(c, true, true), roughness: 0.9, side: THREE.DoubleSide }), geos: [] }; }
        const aww = Math.min(w - 1.4, 7.5), g = new THREE.PlaneGeometry(aww, 1.45, 1, 1);
        const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * aww / 1.2);
        g.rotateX(-Math.PI / 2 + 0.42); g.translate(cx, 3.3, zf + 0.62); aw[col].geos.push(g);
        const val = new THREE.PlaneGeometry(aww, 0.28); const vu = val.attributes.uv; for (let i = 0; i < vu.count; i++) vu.setX(i, vu.getX(i) * aww / 1.2); val.translate(cx, 2.88, zf + 1.28); aw[col].geos.push(val);
      }
    });
    const add = (geos, mat) => { if (!geos.length) return; const m = new THREE.Mesh(mergeGeometries(geos), mat); m.receiveShadow = true; G.add(m); };
    add(ledges, stoneMat); add(acs, acMat); add(fe, feMat);
    for (const a of Object.values(aw)) add(a.geos, a.mat);
  }

  // ---------- rooftop billboard (painted, lit by the sun like everything else) ----------
  {
    const bw = 14, bh = 7, bx = 30, by = 13 + 1.2 + bh / 2, bz = ST.near - 2.2;
    const frameMat = new THREE.MeshStandardMaterial({ color: 0x1d1f22, roughness: 0.6 });
    const frame = new THREE.Mesh(new THREE.BoxGeometry(bw + 0.4, bh + 0.4, 0.3), frameMat); frame.position.set(bx, by, bz - 0.18); G.add(frame);
    const face = new THREE.Mesh(new THREE.PlaneGeometry(bw, bh), new THREE.MeshStandardMaterial({ map: logos.billboard, roughness: 0.75 })); face.position.set(bx, by, bz); G.add(face); out.billboard = face;
    for (const lx of [-7, 7]) { const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.12, 0.12, 2.2, 8), frameMat); pole.position.set(bx + lx * 0.8, 13 + 0.6, bz - 0.2); G.add(pole); }
  }

  // ---------- street lamps (day: switched off; the heads are plain metal) ----------
  const lampG = await A.loadGLTF('lamp.glb');
  const lampSrc = lampG.scene; lampSrc.updateMatrixWorld(true);
  const lb = new THREE.Box3().setFromObject(lampSrc);
  const lk = 3.3 / (lb.max.z - lb.min.z);
  lampSrc.scale.multiplyScalar(lk);
  lampSrc.position.set(-(lb.min.x + lb.max.x) / 2 * lk, 0, -lb.max.z * lk);
  lampSrc.traverse(o => { if (o.isMesh) { o.castShadow = false; for (const m of [o.material].flat()) { if (m.emissive) m.emissive.setRGB(0, 0, 0); m.envMapIntensity = 0; } } });
  const armLen = (lb.max.z - lb.min.z) * lk, headY = lb.max.y * lk;
  const POLE_H = 7.2;
  const armHolder = new THREE.Group(); armHolder.add(lampSrc); armHolder.rotation.y = Math.PI; armHolder.position.y = POLE_H - 0.25;
  const poleMat = new THREE.MeshStandardMaterial({ color: 0x2f3236, roughness: 0.6 });
  out.poleMat = poleMat;
  const lampPro = new THREE.Group(); lampPro.add(armHolder);
  const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.07, 0.13, POLE_H, 10), poleMat); pole.position.y = POLE_H / 2; lampPro.add(pole);
  const base = new THREE.Mesh(new THREE.CylinderGeometry(0.2, 0.24, 0.6, 10), poleMat); base.position.y = 0.3; lampPro.add(base);
  out.lampProto = lampPro;
  const placeLamp = (x, z, facing, y = SW) => {
    const l = lampPro.clone(); l.position.set(x, y, z); l.rotation.y = facing; G.add(l);
    const hy = POLE_H - 0.25 + headY - 0.25;
    const head = new THREE.Vector3(Math.sin(facing) * armLen * 0.9, hy, Math.cos(facing) * armLen * 0.9).add(l.position);
    L(head.clone().setY(hy - 0.8), new THREE.Color(1.0, 0.64, 0.33).multiplyScalar(3.3), 11, 'street');
    world.circle(x, z, 0.18, 7);
  };
  out.placeLamp = placeLamp;
  for (const x of [-58, -44, -30, -10, 6, 22, 38, 54, 80, 96]) placeLamp(x, ST.road0 - 0.45, 0);
  for (const x of [-60, -48, -28, -16, 4, 20, 36, 52, 78, 94]) placeLamp(x, ST.road1 + 0.45, Math.PI);

  // ---------- railway viaduct across the street (brick arches) + a passing train ----------
  {
    const X0 = ST.x1, X1 = ST.x1 + 9.6, H = 10.2, HS = 4.6, RV = 3.9, PW = 2.2;
    const vt = brickT.map.clone();
    vt.wrapS = vt.wrapT = THREE.RepeatWrapping; vt.repeat.set(1 / 3, 1 / 3); vt.needsUpdate = true;
    const vMat = new THREE.MeshStandardMaterial({ map: vt, color: 0x9a7466, roughness: 0.92 });
    const infillMat = new THREE.MeshStandardMaterial({ map: vt, color: 0x5a4842, roughness: 0.95, side: THREE.DoubleSide });
    const bays = [{ a: ST.road0, b: ST.far + 0.6, open: true }];
    for (let z = ST.near - 10 - PW; z - 10 > -295; z -= 10 + PW) bays.push({ a: z - 10, b: z });
    for (let z = ST.far + 0.6 + PW; z + 10 < 375; z += 10 + PW) bays.push({ a: z, b: z + 10 });
    const shape = new THREE.Shape(); shape.moveTo(-380, 0); shape.lineTo(300, 0); shape.lineTo(300, H); shape.lineTo(-380, H); shape.closePath();
    const archPts = (a, b) => { const cx = (a + b) / 2, r = (b - a) / 2, pts = [new THREE.Vector2(a, 0), new THREE.Vector2(a, HS)]; for (let i = 1; i < 16; i++) { const ang = Math.PI - i / 16 * Math.PI; pts.push(new THREE.Vector2(cx + r * Math.cos(ang), HS + RV * Math.sin(ang))); } pts.push(new THREE.Vector2(b, HS), new THREE.Vector2(b, 0)); return pts; };
    const infills = [];
    out.viaductBays = bays;
    for (const bay of bays) {
      const pts = archPts(-bay.b, -bay.a);
      shape.holes.push(new THREE.Path(pts));
      if (!bay.open) infills.push(new THREE.ShapeGeometry(new THREE.Shape(pts)));
    }
    const eg = new THREE.ExtrudeGeometry(shape, { depth: X1 - X0, bevelEnabled: false, curveSegments: 1 });
    eg.rotateY(Math.PI / 2); eg.translate(X0, 0, 0);
    const via = new THREE.Mesh(eg, vMat); via.receiveShadow = true; via.userData.bakeCell = 4; G.add(via);
    const inf = mergeGeometries(infills.flatMap(g => { const a = g.clone().rotateY(Math.PI / 2).translate(X0 + 0.7, 0, 0), b2 = g.clone().rotateY(Math.PI / 2).translate(X1 - 0.7, 0, 0); return [a, b2]; }));
    const infM = new THREE.Mesh(inf, infillMat); infM.userData.bakeCell = 4; G.add(infM);
    const dark = new THREE.MeshStandardMaterial({ color: 0x2c2a28, roughness: 0.9 });
    const steel = new THREE.MeshStandardMaterial({ color: 0x55585c, roughness: 0.55 });
    const deck = [];
    deck.push(new THREE.BoxGeometry(0.45, 1.1, 680).translate(X0 + 0.22, H + 0.55, -40), new THREE.BoxGeometry(0.45, 1.1, 680).translate(X1 - 0.22, H + 0.55, -40));
    deck.push(new THREE.BoxGeometry(X1 - X0 + 0.5, 0.25, 680).translate((X0 + X1) / 2, H - 0.05, -40));
    for (const dx of [-0.72, 0.72]) deck.push(new THREE.BoxGeometry(0.08, 0.14, 680).translate((X0 + X1) / 2 + dx, H + 0.22, -40));
    const masts = [];
    for (let z = -300; z < 380; z += 30) { masts.push(new THREE.BoxGeometry(0.22, 6.2, 0.22).translate(X0 + 0.6, H + 3.1, z), new THREE.BoxGeometry(X1 - X0 - 1, 0.16, 0.16).translate((X0 + X1) / 2, H + 5.6, z)); }
    const dm = new THREE.Mesh(mergeGeometries(deck.slice(0, 3)), dark); dm.userData.bakeCell = 6; G.add(dm);
    G.add(new THREE.Mesh(mergeGeometries([...deck.slice(3), ...masts]), steel));
    const wire = new THREE.Mesh(new THREE.BoxGeometry(0.03, 0.03, 680).translate((X0 + X1) / 2, H + 5.2, -40), steel); G.add(wire);
    out.viaduct = { X0, X1, H, HS, RV, PW };
    // viaduct piers on both sides of the street (the arch over the road is open)
    world.box(X0, X1, ST.near - 40, ST.road0, 30, -1, 'viaduct');
    world.box(X0, X1, ST.far + 0.6, ST.far + 80, 30, -1, 'viaduct');
    // the train: three cars (windows not lit: daytime, no glow)
    const tc = document.createElement('canvas'); tc.width = 512; tc.height = 128; const tx = tc.getContext('2d');
    tx.fillStyle = '#c9ccd0'; tx.fillRect(0, 0, 512, 128); tx.fillStyle = '#1f6b3a'; tx.fillRect(0, 84, 512, 18); tx.fillStyle = '#e6e8ea'; tx.fillRect(0, 0, 512, 10);
    for (let k = 0; k < 8; k++) { const wx = 20 + k * 62; tx.fillStyle = '#20262c'; tx.fillRect(wx, 28, 44, 40); }
    const trainMat = new THREE.MeshStandardMaterial({ map: ctex(tc), roughness: 0.5, metalness: 0.3 });
    const train = new THREE.Group(); train.userData.dynamic = true;
    for (let k = 0; k < 3; k++) { const car = new THREE.Mesh(new THREE.BoxGeometry(3.0, 3.6, 19), trainMat); car.position.set(0, 2.1, k * 20); train.add(car); const roof = new THREE.Mesh(new THREE.BoxGeometry(2.8, 0.25, 18.6), steel); roof.position.set(0, 4.0, k * 20); train.add(roof); }
    train.position.set((X0 + X1) / 2, H + 0.2, -500); train.visible = false; G.add(train);
    out.train = { obj: train, z: -500, v: 0, t: rnd() * 20 + 15, dir: 1, mat: trainMat };
  }

  // ---------- props ----------
  const green = new THREE.MeshStandardMaterial({ color: 0x1f4a35, roughness: 0.7 });
  const redM = new THREE.MeshStandardMaterial({ color: 0xa3201b, roughness: 0.6 });
  const metal = new THREE.MeshStandardMaterial({ color: 0x2c3034, roughness: 0.6 });
  for (const x of [-34, -2.5, 18, 42]) { const b = new THREE.Mesh(new THREE.CylinderGeometry(0.32, 0.28, 0.95, 14), green); b.position.set(x, SW + 0.47, ST.near + 0.6); G.add(b); world.circle(x, ST.near + 0.6, 0.34, 1.0); }
  for (const x of [-20, 26]) { const h = new THREE.Mesh(new THREE.CylinderGeometry(0.16, 0.2, 0.8, 10), redM); h.position.set(x, SW + 0.4, ST.road0 - 0.55); G.add(h); world.circle(x, ST.road0 - 0.55, 0.22, 0.85); }
  // bus shelter with the badge ad (in front of the kebab place)
  {
    const bx = 18, bz = ST.near + 1.9;
    const glass = new THREE.MeshStandardMaterial({ color: 0x9fb8c8, transparent: true, opacity: 0.22, roughness: 0.2, depthWrite: false });
    const roof = new THREE.Mesh(new THREE.BoxGeometry(4.2, 0.12, 1.6), metal); roof.position.set(bx, 2.6, bz); G.add(roof);
    for (const dx of [-2, 2]) { const p = new THREE.Mesh(new THREE.BoxGeometry(0.08, 2.5, 0.08), metal); p.position.set(bx + dx, 1.35, bz - 0.7); G.add(p); const p2 = p.clone(); p2.position.z = bz + 0.7; G.add(p2); }
    const back = new THREE.Mesh(new THREE.PlaneGeometry(4, 2.2), glass); back.position.set(bx, 1.4, bz - 0.72); back.userData.noBake = true; G.add(back);
    const adMat = new THREE.MeshStandardMaterial({ map: logos.badgeTall, roughness: 0.7 });
    const ad = new THREE.Mesh(new THREE.BoxGeometry(0.06, 1.9, 1.25), adMat); ad.position.set(bx + 2.02, 1.35, bz); G.add(ad);
    const bench = new THREE.Mesh(new THREE.BoxGeometry(2.6, 0.08, 0.45), metal); bench.position.set(bx, 0.6, bz - 0.4); G.add(bench);
    world.box(bx - 2.1, bx + 2.1, bz - 0.8, bz - 0.6, 2.6); world.box(bx + 1.95, bx + 2.1, bz - 0.8, bz + 0.8, 2.6);
    out.busStop = { x: bx, z: bz };
  }

  out.update = (dt, env, t) => {
    const tr = out.train;
    if (tr) {
      if (!tr.obj.visible) { tr.t -= dt; if (tr.t <= 0) { tr.dir = rnd() < 0.5 ? 1 : -1; tr.z = tr.dir > 0 ? -420 : 480; tr.obj.visible = true; tr.obj.rotation.y = tr.dir > 0 ? 0 : Math.PI; } }
      else { tr.z += tr.dir * 21 * dt; tr.obj.position.z = tr.z; if ((tr.dir > 0 && tr.z > 480) || (tr.dir < 0 && tr.z < -420)) { tr.obj.visible = false; tr.t = 35 + rnd() * 50; } }
    }
    if (out.district) out.district.update(dt, env, t);
  };
  return out;
}

function vertSign(font, text, color) {
  const c = canvas(256, 384), x = c.getContext('2d');
  x.font = `900 120px ${font}`; x.textAlign = 'center'; x.textBaseline = 'middle';
  x.shadowColor = color; x.shadowBlur = 4; x.fillStyle = color;
  [...text].forEach((ch, i) => x.fillText(ch, 128, 70 + i * 120));
  return ctex(c);
}

export function coneMaterial(color, strength) {
  return new THREE.ShaderMaterial({
    uniforms: { uColor: { value: new THREE.Color(color) }, uStrength: { value: strength } },
    vertexShader: `varying float vH; varying vec3 vN; varying vec3 vV;
      void main(){ vH = uv.y; vec4 mv = modelViewMatrix * vec4(position,1.0); vN = normalize(normalMatrix * normal); vV = normalize(-mv.xyz); gl_Position = projectionMatrix * mv; }`,
    fragmentShader: `uniform vec3 uColor; uniform float uStrength; varying float vH; varying vec3 vN; varying vec3 vV;
      void main(){ float edge = pow(abs(dot(vN, vV)), 2.0); float a = pow(vH, 1.4) * edge * uStrength; gl_FragColor = vec4(uColor * a, 1.0); }`,
    transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide, fog: false,
  });
}
