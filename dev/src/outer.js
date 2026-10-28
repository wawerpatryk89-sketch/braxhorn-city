// The city beyond the playable block: the main street continues into the distance in both directions,
// lined with procedural buildings (lit windows at night), plus a ring of tower blocks on the skyline.
// Everything is merged per material, so the whole outer city costs a handful of draw calls.
import * as THREE from 'three';
import { addLampField } from './env.js';
import { mergeGeometries } from '../three/examples/jsm/utils/BufferGeometryUtils.js';

const rng = (() => { let s = 98765; return () => { s = (s * 16807) % 2147483647; return s / 2147483647; }; })();
const R = (a, b) => a + rng() * (b - a);

// one facade tile = 12 m x 24 m: 4 window columns, shop band (street styles) + 3 m floors
function facadeTile(kind) {
  const W = kind.tower ? 256 : 512, H = W * 2, PX = W / 12;
  const c = document.createElement('canvas'); c.width = W; c.height = H; const x = c.getContext('2d');
  const e = document.createElement('canvas'); e.width = W; e.height = H; const ex = e.getContext('2d');
  ex.fillStyle = '#000'; ex.fillRect(0, 0, W, H);
  x.fillStyle = kind.base; x.fillRect(0, 0, W, H);
  if (kind.brick) {
    const bh = 0.075 * PX * 2, bw = 0.23 * PX * 2;
    for (let r = 0; r * bh < H; r++) for (let k = -1; k * bw < W; k++) { const v = 0.78 + rng() * 0.35; x.fillStyle = `rgba(${kind.brick[0] * v | 0},${kind.brick[1] * v | 0},${kind.brick[2] * v | 0},0.5)`; x.fillRect(k * bw + (r % 2) * bw / 2 + 1, r * bh + 1, bw - 1.5, bh - 1.5); }
  } else {
    for (let i = 0; i < 900; i++) { x.fillStyle = `rgba(0,0,0,${rng() * 0.06})`; x.fillRect(rng() * W, rng() * H, 2 + rng() * 20, 2 + rng() * 20); }
    if (kind.panels) { x.fillStyle = 'rgba(0,0,0,0.18)'; for (let f = 0; f < 8; f++) x.fillRect(0, H - (f * 3 + (kind.tower ? 0 : 4.2)) * PX, W, 2); for (let k = 0; k < 4; k++) x.fillRect(k * 3 * PX, 0, 2, H); }
  }
  for (let i = 0; i < 18; i++) { const gx = rng() * W; const g = x.createLinearGradient(0, 0, 0, H); g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.2)'); x.fillStyle = g; x.fillRect(gx, rng() * H * 0.4, 3 + rng() * 12, H); }
  const y0 = kind.tower ? 0 : 4.2, floors = kind.tower ? 8 : 6;
  for (let f = 0; f < floors; f++) for (let k = 0; k < 4; k++) {
    const ww = kind.ww * PX, wh = kind.wh * PX, wx = (k * 3 + 1.5) * PX - ww / 2, wy = H - (y0 + f * 3 + 0.9) * PX - wh;
    x.fillStyle = kind.frame; x.fillRect(wx - 3, wy - 3, ww + 6, wh + 6);
    const gl = x.createLinearGradient(wx, wy, wx + ww, wy + wh); gl.addColorStop(0, '#41505e'); gl.addColorStop(1, '#141a22'); x.fillStyle = gl; x.fillRect(wx, wy, ww, wh);
    if (!kind.tower) { x.fillStyle = '#b9b2a4'; x.fillRect(wx - 3, wy + wh + 2, ww + 6, 4); }
    const lit = rng();
    if (lit > 0.45) {
      ex.fillStyle = lit > 0.93 ? '#8fbcff' : lit > 0.75 ? '#ffd9a0' : '#ffb35c'; ex.globalAlpha = 0.45 + rng() * 0.55; ex.fillRect(wx, wy, ww, wh); ex.globalAlpha = 1;
      if (rng() > 0.55) { ex.fillStyle = 'rgba(0,0,0,0.65)'; ex.fillRect(wx + ww * rng() * 0.6, wy, ww * 0.3, wh); }
    }
  }
  if (!kind.tower) {
    // shop band with lit shopfronts and little signs
    const sy = H - 4.2 * PX;
    x.fillStyle = '#17191c'; x.fillRect(0, sy, W, 4.2 * PX);
    x.fillStyle = 'rgba(30,26,24,0.8)'; x.fillRect(0, sy - 0.3 * PX, W, 0.3 * PX);
    const cols = ['#ffcf8a', '#a8e8ff', '#c8ffd8', '#ffc0c8', '#ffe7a0'];
    for (let k = 0; k < 2; k++) {
      const gx = (k * 6 + 0.5) * PX, gw = 5 * PX, gy = sy + 1.2 * PX, gh = 2.9 * PX;
      const g = x.createLinearGradient(0, gy, 0, gy + gh); g.addColorStop(0, '#2c3a44'); g.addColorStop(1, '#0d1116'); x.fillStyle = g; x.fillRect(gx, gy, gw, gh);
      x.fillStyle = '#0b0c0e'; x.fillRect(gx + gw / 2 - 2, gy, 4, gh);
      if (rng() < 0.75) {
        const col = cols[Math.floor(rng() * cols.length)];
        const eg = ex.createLinearGradient(0, gy, 0, gy + gh); eg.addColorStop(0, col); eg.addColorStop(1, 'rgba(30,20,10,1)'); ex.globalAlpha = 0.4; ex.fillStyle = eg; ex.fillRect(gx, gy, gw, gh); ex.globalAlpha = 1;
        ex.fillStyle = col; ex.globalAlpha = 0.9; ex.fillRect(gx + gw * 0.15, sy + 0.35 * PX, gw * 0.7, 0.55 * PX); ex.globalAlpha = 1;
        x.fillStyle = '#222'; x.fillRect(gx + gw * 0.15, sy + 0.35 * PX, gw * 0.7, 0.55 * PX);
      }
    }
  }
  const t = new THREE.CanvasTexture(c), te = new THREE.CanvasTexture(e);
  for (const tt of [t, te]) { tt.wrapS = tt.wrapT = THREE.RepeatWrapping; tt.anisotropy = 4; }
  t.colorSpace = THREE.SRGBColorSpace; te.colorSpace = THREE.SRGBColorSpace;
  return { map: t, emissiveMap: te };
}

const STYLES = [
  { name: 'brick', base: '#6a3a2b', brick: [150, 80, 60], frame: '#1d1e22', ww: 1.3, wh: 1.7 },
  { name: 'render', base: '#8a8378', frame: '#2a2a2e', ww: 1.4, wh: 1.6 },
  { name: 'dark', base: '#3c3d42', frame: '#141518', ww: 1.6, wh: 1.7, panels: true },
  { name: 'panel', base: '#8c8f93', frame: '#2e3136', ww: 1.7, wh: 1.4, panels: true, tower: true },
  { name: 'tower', base: '#4a4d55', frame: '#1a1c20', ww: 2.2, wh: 2.1, panels: true, tower: true },
];

// geometry accumulator: quads with uv in metres / tile size, per-building tint as vertex colour
class Acc {
  constructor() { this.p = []; this.n = []; this.uv = []; this.c = []; this.i = []; }
  quad(a, b, c, d, nrm, uvs, col) {
    const o = this.p.length / 3;
    for (const v of [a, b, c, d]) this.p.push(v[0], v[1], v[2]);
    for (let k = 0; k < 4; k++) { this.n.push(nrm[0], nrm[1], nrm[2]); this.c.push(col.r, col.g, col.b); }
    for (const u of uvs) this.uv.push(u[0], u[1]);
    this.i.push(o, o + 1, o + 2, o, o + 2, o + 3);
  }
  geometry() {
    const g = new THREE.BufferGeometry();
    g.setAttribute('position', new THREE.Float32BufferAttribute(this.p, 3)); g.setAttribute('normal', new THREE.Float32BufferAttribute(this.n, 3));
    g.setAttribute('uv', new THREE.Float32BufferAttribute(this.uv, 2)); g.setAttribute('color', new THREE.Float32BufferAttribute(this.c, 3));
    g.setIndex(this.i); g.computeBoundingSphere(); return g;
  }
}

// a box building: walls with the facade tile, flat roof + coping in the roof accumulator
function addBuilding(acc, roof, x0, x1, z0, z1, h, tint, uOff = 0) {
  const TW = 12, TH = 24;
  const w = (a, b, nrm) => {
    const L = Math.hypot(b[0] - a[0], b[2] - a[2]);
    acc.quad([a[0], 0, a[2]], [b[0], 0, b[2]], [b[0], h, b[2]], [a[0], h, a[2]], nrm, [[uOff, 0], [uOff + L / TW, 0], [uOff + L / TW, h / TH], [uOff, h / TH]], tint);
  };
  w([x0, 0, z1], [x1, 0, z1], [0, 0, 1]);    // +z
  w([x1, 0, z0], [x0, 0, z0], [0, 0, -1]);   // -z
  w([x1, 0, z1], [x1, 0, z0], [1, 0, 0]);    // +x
  w([x0, 0, z0], [x0, 0, z1], [-1, 0, 0]);   // -x
  const k = 0.25, ch = 0.7, rc = { r: 1, g: 1, b: 1 };
  roof.quad([x0, h, z1], [x1, h, z1], [x1, h, z0], [x0, h, z0], [0, 1, 0], [[0, 0], [1, 0], [1, 1], [0, 1]], rc);
  // coping band
  for (const [a, b, n] of [[[x0 - k, z1 + k], [x1 + k, z1 + k], [0, 0, 1]], [[x1 + k, z0 - k], [x0 - k, z0 - k], [0, 0, -1]], [[x1 + k, z1 + k], [x1 + k, z0 - k], [1, 0, 0]], [[x0 - k, z0 - k], [x0 - k, z1 + k], [-1, 0, 0]]])
    roof.quad([a[0], h - 0.1, a[1]], [b[0], h - 0.1, b[1]], [b[0], h + ch, b[1]], [a[0], h + ch, a[1]], n, [[0, 0], [1, 0], [1, 1], [0, 1]], rc);
}

export function buildOuterCity({ ST, group, poleMat }) {
  const accs = STYLES.map(() => new Acc()), roof = new Acc();
  const lights = []; // distant street lamp heads
  const tint = () => { const v = R(0.82, 1.08); return { r: v * R(0.95, 1.05), g: v, b: v * R(0.92, 1.03) }; };
  const streetStyle = () => [0, 0, 1, 2][Math.floor(rng() * 4)];
  const FAR = ST.far + 0.6;
  // street rows: near side (facade at ST.near, facing +z) and far side (facade at FAR, facing -z)
  const row = (xa, xb, side) => {
    let x = xa; const dir = Math.sign(xb - xa);
    while ((xb - x) * dir > 4) {
      const w = Math.min(Math.abs(xb - x), R(9, 16)), floors = Math.floor(R(2, 6.99));
      const h = 4.2 + floors * 3, d = R(10, 14), s = streetStyle();
      const xa2 = dir > 0 ? x : x - w, xb2 = dir > 0 ? x + w : x;
      if (side < 0) addBuilding(accs[s], roof, xa2, xb2, ST.near - d, ST.near, h, tint(), rng());
      else addBuilding(accs[s], roof, xa2, xb2, FAR, FAR + d, h, tint(), rng());
      x += dir * w;
    }
  };
  row(-60, -300, -1); row(-60, -300, 1);
  row(84, 330, -1); row(119, 330, 1);
  // the west cross street: buildings on its west side, facing it
  for (let z = FAR; z < 140;) { const w = R(10, 16), h = 4.2 + Math.floor(R(2, 6.99)) * 3; addBuilding(accs[streetStyle()], roof, -74, -60, z, z + w, h, tint(), rng()); z += w; }
  // a second row behind the street rows (roofline variety)
  for (const [xa, xb, z0, z1] of [[-300, -70, ST.near - 34, ST.near - 16], [-300, -70, FAR + 16, FAR + 32], [92, 330, ST.near - 34, ST.near - 16], [125, 330, FAR + 16, FAR + 32]]) {
    for (let x = xa; x < xb;) { const w = R(12, 22), h = R(16, 34); addBuilding(accs[[2, 3][Math.floor(rng() * 2)]], roof, x, Math.min(xb, x + w), z0, z1, h, tint(), rng()); x += w + R(0, 3); }
  }
  // skyline towers around the whole block (outside the playable area and the scans)
  const cx = 5, cz = 28;
  for (let i = 0; i < 34; i++) {
    const a = i / 34 * Math.PI * 2 + R(-0.06, 0.06), r = R(120, 210);
    const x = cx + Math.cos(a) * r * 1.25, z = cz + Math.sin(a) * r;
    if (Math.abs(z - cz) < 36) continue; // keep the street corridor open
    const w = R(16, 26), d = R(16, 24), h = R(30, 82);
    addBuilding(accs[rng() < 0.5 ? 3 : 4], roof, x - w / 2, x + w / 2, z - d / 2, z + d / 2, h, tint(), rng());
    if (h > 55) lights.push({ pos: new THREE.Vector3(x, h + 1.2, z), kind: 'air' });
  }
  // fill the block behind the far-side scans
  for (let x = -44; x < 58;) { const w = R(12, 20), h = R(14, 30); addBuilding(accs[[0, 1, 2][Math.floor(rng() * 3)]], roof, x, Math.min(58, x + w), 112, 128, h, tint(), rng()); x += w; }
  // materials + meshes
  const out = { meshes: [], mats: [] };
  STYLES.forEach((st, k) => {
    if (!accs[k].p.length) return;
    const tx = facadeTile(st);
    const m = addLampField(new THREE.MeshStandardMaterial({ map: tx.map, emissiveMap: tx.emissiveMap, emissive: 0xffffff, emissiveIntensity: 0, roughness: 0.9, vertexColors: true }));
    const mesh = new THREE.Mesh(accs[k].geometry(), m); mesh.receiveShadow = false; group.add(mesh); out.meshes.push(mesh); out.mats.push(m);
  });
  const roofMat = addLampField(new THREE.MeshStandardMaterial({ color: 0x26272a, roughness: 0.95 }));
  const rm = new THREE.Mesh(roof.geometry(), roofMat); group.add(rm); out.meshes.push(rm);
  // distant street: lamp poles (merged) + glow sprites + lamp positions for the lamp field
  const poles = [];
  const poleGeo = new THREE.CylinderGeometry(0.08, 0.13, 7.2, 6);
  const armGeo = new THREE.BoxGeometry(0.08, 0.08, 2.2);
  const lamp = [];
  for (const [xa, xb] of [[-56, -300], [74, 330]]) {
    const dir = Math.sign(xb - xa);
    for (let x = xa; (xb - x) * dir > 0; x += dir * 16) for (const [z, f] of [[ST.road0 - 0.45, 1], [ST.road1 + 0.45, -1]]) {
      const xx = x + (f > 0 ? 0 : dir * 8);
      poles.push(poleGeo.clone().translate(xx, 3.6 + 0.15, z), armGeo.clone().translate(xx, 7.1, z + f * 1.0));
      lamp.push(new THREE.Vector3(xx, 6.9, z + f * 2.0));
    }
  }
  const pm = new THREE.Mesh(mergeGeometries(poles), poleMat); group.add(pm); out.meshes.push(pm);
  // ground under everything (seen through gaps between the scans and buildings)
  const gnd = new THREE.Mesh(new THREE.PlaneGeometry(1400, 1400).rotateX(-Math.PI / 2), addLampField(new THREE.MeshStandardMaterial({ color: 0x2e2d2a, roughness: 1 })));
  gnd.position.set(0, -0.04, 30); group.add(gnd);
  out.lampHeads = lamp; out.airLights = lights;
  out.update = (night) => { for (const m of out.mats) m.emissiveIntensity = 1.15 * night; };
  return out;
}
