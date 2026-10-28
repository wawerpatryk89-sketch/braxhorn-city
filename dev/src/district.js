// Plansza 1 "Dzielnica BRAXHORN": everything around the main street (street.js) that makes a closed, compact
// district: the quay with the pier and the delivery point (west), the far-side frontage with the gate passage
// ("brama"), the block courtyard with the volleyball court (south), the closed roads to the forest (south-west)
// and to the house estate (east), plus the named points of the first mission and walking routes for future people.
// All static geometry is matte and gets its light from the bake (bake.js); only water and people are lit live.
import * as THREE from 'three';
import { mergeGeometries } from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import * as CT from './citytex.js';
import { ST, XW, XE, facadeTextures } from './street.js';
import { buildInteriors, INTERIOR_ZONES, INTERIOR_POINTS } from './interiors.js';
import { floatGeo } from './bake.js';

const SW = 0.15;
export const D = {
  quay: { x0: -84, x1: -64, z0: -6, z1: 112 },
  waterY: -1.7,
  pier: { x0: -112, x1: -84, z0: 70, z1: 76, top: 0.32 },
  frontZ: ST.far + 0.6,           // far-side building line (36.1)
  backZ: 48,                      // back of the far-side frontage (courtyard side)
  yard: { x0: -64, x1: 20, z0: 48, z1: 100 },
  court: { x: -10, z: 80, len: 18, wid: 9, x0: -22, x1: 2, z0: 72.5, z1: 87.5 },
  brama: { x0: -12, x1: -8, h: 4.2 },
  yardGate: { x: -64, z0: 74, z1: 80 },
  forestGate: { z: 113.5 },
  estateGate: { x: XE },
};

// named points of the first mission ("Dług w Braxhorn") and of the district; y = floor height
export const POINTS = {
  // gym (base)
  gym_start: { x: -1.5, y: 0, z: 2.8, face: Math.PI, stage: 'm1.start', note: 'start misji, środek siłowni' },
  gym_ring_access: { x: 9, y: 0, z: -1.2, face: Math.PI, stage: 'm1.warmup', note: 'schodki na ring (rozgrzewka)' },
  gym_ring_center: { x: 9, y: 0.95, z: -5.5, face: 0, stage: 'm1.warmup', note: 'środek ringu' },
  gym_talk: { x: 3.2, y: 0, z: 3.4, face: -2.4, stage: 'm1.brief|m1.return1|m1.end', note: 'rozmowa z trenerem: zlecenie, wypłata, koniec misji' },
  gym_trainer: { x: 4.3, y: 0, z: 2.4, face: 0.9, stage: 'm1', note: 'miejsce trenera (zleceniodawca)' },
  gym_door_in: { x: -6, y: 0, z: 9.6, face: 0, note: 'drzwi od środka' },
  gym_door_out: { x: -6, y: 0, z: 12.8, face: Math.PI, note: 'drzwi od zaułka' },
  street_gym: { x: -6.75, y: SW, z: 21.6, face: -Math.PI / 2, note: 'wylot zaułka siłowni na ulicę' },
  // shop (healing)
  shop_door: { x: -20.7, y: SW, z: 21.4, face: Math.PI, stage: 'm1.heal', note: 'wejście do sklepu z chodnika' },
  // quay: delivery pickup
  quay_entry: { x: -70, y: SW, z: 28.5, face: -Math.PI / 2, note: 'koniec ulicy przy nabrzeżu' },
  quay_delivery: { x: -77.4, y: SW, z: 83, face: -Math.PI / 2, stage: 'm1.pickup', note: 'punkt dostaw: odbiór torby ze sprzętem (+mała zapłata)' },
  quay_delivery_item: { x: -78.6, y: SW + 0.7, z: 81.2, stage: 'm1.pickup', note: 'torba ze sprzętem na palecie' },
  pier_end: { x: -108, y: 0.32, z: 73, face: -Math.PI / 2, note: 'koniec mola' },
  // courtyard + court: fight 1
  yard_gate_west: { x: -62.5, y: SW, z: 77, face: Math.PI / 2, stage: 'm1.return1', note: 'brama podwórka od nabrzeża (skrót)' },
  yard_brama_street: { x: -10, y: SW, z: 35.2, face: 0, note: 'brama na podwórko od ulicy (naprzeciw siłowni)' },
  yard_brama_yard: { x: -10, y: SW, z: 50, face: Math.PI, stage: 'm1.return1', note: 'wyjście z podwórka w stronę sklepu i siłowni' },
  fight1_center: { x: -10, y: SW, z: 80, face: 0, stage: 'm1.fight1', note: 'punkt starcia nr 1: boisko' },
  fight1_player: { x: -15, y: SW, z: 80, face: Math.PI / 2, stage: 'm1.fight1', note: 'gracz wchodzi na boisko' },
  fight1_enemy_wait: { x: -10, y: SW, z: 89.4, face: Math.PI, stage: 'm1.fight1', note: 'przeciwnik czeka na ławce' },
  fight1_enemy_enter: { x: -3.5, y: SW, z: 98.6, face: Math.PI, stage: 'm1.fight1', note: 'naturalne wejście przeciwnika: klatka schodowa bloku' },
  fight1_camera: { x: -10, y: 5.5, z: 66.5, note: 'miejsce kamery obejmującej obu zawodników (boisko od strony bramy)' },
  // closed future passages
  closed_forest: { x: -74, y: SW, z: 111, face: 0, stage: 'later.plansza3', note: 'zamknięta droga do lasu z bunkrami' },
  closed_estate: { x: 101, y: SW, z: 28.5, face: Math.PI / 2, stage: 'later.plansza2', note: 'zamknięty wjazd na osiedle domków' },
  ...INTERIOR_POINTS,
};

// walking routes for future pedestrians (polylines on the walkable floor, x/z)
export const ROUTES = {
  near_sidewalk: [[-64, 22.3], [-27, 22.3], [-15, 22.3], [2, 22.3], [60, 22.3]],
  far_sidewalk: [[-64, 34.6], [-10, 34.6], [60, 34.6], [100, 34.6]],
  quay: [[-70, 2], [-70, 30], [-72, 60], [-72, 100], [-74, 108]],
  pier: [[-80, 73], [-108, 73]],
  yard_loop: [[-10, 52], [-30, 58], [-50, 66], [-58, 77], [-50, 94], [-26, 92.5], [6, 92.5], [12, 70], [-2, 58], [-10, 52]],
  brama: [[-10, 34.6], [-10, 52]],
};

// zones: camera ceilings, interior light moods, what to hide (main.js)
export const ZONES = [
  { id: 'brama', minX: D.brama.x0, maxX: D.brama.x1, minZ: D.frontZ, maxZ: D.backZ, y0: -1, y1: 4, ceil: D.brama.h, mood: 'passage' },
  ...INTERIOR_ZONES,
];

// ---------------- small kit ----------------
const M = (o) => new THREE.MeshStandardMaterial({ roughness: 0.9, metalness: 0, ...o });
function worldUV(g, su, sv = su, ou = 0, ov = 0) {
  const p = g.attributes.position, n = g.attributes.normal, uv = new Float32Array(p.count * 2);
  for (let i = 0; i < p.count; i++) {
    const ax = Math.abs(n.getX(i)), ay = Math.abs(n.getY(i)), az = Math.abs(n.getZ(i));
    let u, v;
    if (ax >= ay && ax >= az) { u = p.getZ(i) * Math.sign(n.getX(i) || 1) * -1; v = p.getY(i); }
    else if (ay >= az) { u = p.getX(i); v = p.getZ(i); }
    else { u = p.getX(i) * Math.sign(n.getZ(i) || 1); v = p.getY(i); }
    uv[i * 2] = u / su + ou; uv[i * 2 + 1] = v / sv + ov;
  }
  g.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); return g;
}
export class Batch {
  constructor(parent) { this.parent = parent; this.groups = new Map(); }
  add(mat, geo, ud = null) {
    const key = mat.uuid + (ud ? JSON.stringify(ud) : '');
    if (!this.groups.has(key)) this.groups.set(key, { mat, geos: [], ud });
    const g = geo.index ? geo.toNonIndexed() : geo;
    for (const k of Object.keys(g.attributes)) if (!['position', 'normal', 'uv'].includes(k)) g.deleteAttribute(k);
    if (!g.attributes.uv) g.setAttribute('uv', new THREE.BufferAttribute(new Float32Array(g.attributes.position.count * 2), 2));
    this.groups.get(key).geos.push(g);
  }
  // axis-aligned box; uv: metres per texture tile (world mapped) or null
  box(mat, x0, x1, y0, y1, z0, z1, { uv = 2, uvv = null, ud = null, rotY = 0, cx = null, cz = null } = {}) {
    const g = new THREE.BoxGeometry(Math.abs(x1 - x0), Math.abs(y1 - y0), Math.abs(z1 - z0));
    g.translate((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2);
    if (uv) worldUV(g, uv, uvv || uv);
    if (rotY) { g.translate(-cx, 0, -cz); g.rotateY(rotY); g.translate(cx, 0, cz); }
    this.add(mat, g, ud); return g;
  }
  cyl(mat, x, y, z, rt, rb, h, seg = 12, { ud = null, rx = 0, rz = 0, ry = 0 } = {}) {
    const g = new THREE.CylinderGeometry(rt, rb, h, seg); if (rx) g.rotateX(rx); if (rz) g.rotateZ(rz); if (ry) g.rotateY(ry); g.translate(x, y, z); this.add(mat, g, ud); return g;
  }
  geo(mat, g, ud = null) { this.add(mat, g, ud); }
  build() {
    const out = [];
    for (const { mat, geos, ud } of this.groups.values()) {
      const m = new THREE.Mesh(mergeGeometries(geos), mat); m.receiveShadow = true;
      if (ud) Object.assign(m.userData, ud);
      this.parent.add(m); out.push(m);
    }
    this.groups.clear(); return out;
  }
}
// a vertical facade plane covering x in [xa,xb], y in [ya,yb] of a facade texture that spans [bx0,bx1] x [0,h]
// facing -z (north, toward the main street) or +z / -x / +x
function facadePlane(B, mat, { bx0, bx1, h, xa, xb, ya = 0, yb = h, at, facing = '-z' }) {
  const w = bx1 - bx0, g = new THREE.BufferGeometry();
  const u = (x) => facing === '-z' || facing === '+x' ? (bx1 - x) / w : (x - bx0) / w;
  const P = [], U = [];
  const quad = [[xa, ya], [xb, ya], [xb, yb], [xa, ya], [xb, yb], [xa, yb]];
  for (const [x, y] of quad) {
    if (facing === '-z' || facing === '+z') P.push(x, y, at); else P.push(at, y, x);
    U.push(u(x), y / h);
  }
  g.setAttribute('position', new THREE.Float32BufferAttribute(P, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(U, 2));
  g.computeVertexNormals();
  // make it face the right way
  const want = { '-z': [0, 0, -1], '+z': [0, 0, 1], '-x': [-1, 0, 0], '+x': [1, 0, 0] }[facing], n = g.attributes.normal;
  if (n.getX(0) * want[0] + n.getZ(0) * want[2] < 0) { const idx = []; for (let i = 0; i < 6; i += 3) idx.push(i, i + 2, i + 1); g.setIndex(idx); const ng = g.toNonIndexed(); ng.computeVertexNormals(); B.geo(mat, ng); return; }
  B.geo(mat, g);
}

// ---------------- model pieces (blokowisko_city7 = panel blocks; props; garages; palms) ----------------
// matte copy of a model material: no glow, no metal, and only textures on the first uv set (we keep only 'uv')
export function cleanMat(x) {
  const s = x.clone();
  for (const k of ['map', 'normalMap', 'roughnessMap', 'metalnessMap', 'aoMap', 'lightMap', 'emissiveMap', 'alphaMap', 'bumpMap', 'displacementMap', 'specularMap', 'specularIntensityMap', 'specularColorMap', 'clearcoatMap', 'clearcoatNormalMap', 'clearcoatRoughnessMap', 'sheenColorMap', 'sheenRoughnessMap', 'transmissionMap', 'thicknessMap', 'iridescenceMap', 'iridescenceThicknessMap', 'anisotropyMap'])
    if (s[k] && (s[k].channel || 0) !== 0) s[k] = null;
  for (const k of ['aoMap', 'lightMap', 'emissiveMap']) s[k] = null;
  if (s.emissive) s.emissive.setRGB(0, 0, 0); s.envMapIntensity = 0; s.metalness = 0; s.roughness = Math.max(s.roughness ?? 0.9, 0.75);
  if (s.transmission) s.transmission = 0;
  return s;
}
function collect(root, test) { const out = []; root.updateMatrixWorld(true); root.traverse(o => { if (o.isMesh && test(o)) out.push(o); }); return out; }
// meshes -> one group in metres, optionally turned so its long facades run along x; base centred at origin
function piece(meshes, { S = 1, square = false } = {}) {
  let yaw = 0;
  if (square) {
    let best = 0; const a = new THREE.Vector3(), b = new THREE.Vector3(), c = new THREE.Vector3(), n = new THREE.Vector3();
    for (const m of meshes) {
      const p = m.geometry.attributes.position, idx = m.geometry.index, cnt = idx ? idx.count : p.count;
      for (let t = 0; t < cnt; t += 3) {
        const i0 = idx ? idx.getX(t) : t, i1 = idx ? idx.getX(t + 1) : t + 1, i2 = idx ? idx.getX(t + 2) : t + 2;
        a.fromBufferAttribute(p, i0).applyMatrix4(m.matrixWorld); b.fromBufferAttribute(p, i1).applyMatrix4(m.matrixWorld); c.fromBufferAttribute(p, i2).applyMatrix4(m.matrixWorld);
        n.crossVectors(b.sub(a), c.sub(a)); const area = n.length(); n.normalize();
        if (Math.abs(n.y) < 0.2 && area > best) { best = area; yaw = Math.atan2(n.x, n.z); }
      }
    }
  }
  const rot = new THREE.Matrix4().makeRotationY(-yaw), sc = new THREE.Matrix4().makeScale(S, S, S);
  const g = new THREE.Group(); const box = new THREE.Box3();
  for (const m of meshes) {
    const geo = floatGeo(m.geometry).applyMatrix4(new THREE.Matrix4().multiplyMatrices(sc, new THREE.Matrix4().multiplyMatrices(rot, m.matrixWorld)));
    const mats = [m.material].flat().map(cleanMat);
    const mm = new THREE.Mesh(geo, mats.length > 1 ? mats : mats[0]); g.add(mm);
    geo.computeBoundingBox(); box.union(geo.boundingBox);
  }
  let size = box.getSize(new THREE.Vector3());
  const ctr = box.getCenter(new THREE.Vector3());
  for (const m of g.children) m.geometry.translate(-ctr.x, -box.min.y, -ctr.z);
  if (square && size.z > size.x) { for (const m of g.children) m.geometry.rotateY(Math.PI / 2); size = new THREE.Vector3(size.z, size.y, size.x); }
  g.userData.size = size;
  return g;
}
function place(parent, pc, x, z, ry = 0, y = 0, ud = null) {
  const g = new THREE.Group(); for (const m of pc.children) { const mm = new THREE.Mesh(m.geometry, m.material); mm.receiveShadow = true; if (ud) Object.assign(mm.userData, ud); g.add(mm); }
  g.position.set(x, y, z); g.rotation.y = ry; parent.add(g); g.updateMatrixWorld(true); return g;
}
// footprint of a placed piece (for collision)
function footprint(pc, x, z, ry, pad = 0) {
  const s = pc.userData.size, c = Math.abs(Math.cos(ry)), n = Math.abs(Math.sin(ry));
  const hw = (s.x * c + s.z * n) / 2 + pad, hd = (s.x * n + s.z * c) / 2 + pad; return [x - hw, x + hw, z - hd, z + hd, s.y];
}

// ---------------- procedural trees (matte, simple but full) ----------------
function leafTex() {
  const S = 128, c = CT.canvas(S, S), x = c.getContext('2d');
  x.fillStyle = '#3c5a2a'; x.fillRect(0, 0, S, S);
  for (let i = 0; i < 700; i++) { const v = 50 + Math.random() * 60; x.fillStyle = `rgb(${v * 0.62 | 0},${v | 0},${v * 0.42 | 0})`; x.beginPath(); x.ellipse(Math.random() * S, Math.random() * S, 2 + Math.random() * 3, 1.2 + Math.random() * 2, Math.random() * 3, 0, 7); x.fill(); }
  return CT.ctex(c);
}
function addLeafTree(B, mats, x, z, h, y = 0, seed = 1) {
  B.cyl(mats.bark, x, y + h * 0.25, z, 0.14 * h / 8, 0.22 * h / 8, h * 0.5, 7);
  const r = h * 0.3;
  for (let k = 0; k < 4; k++) {
    const a = seed * 2.1 + k * 1.7, d = k ? r * 0.45 : 0;
    const g = new THREE.IcosahedronGeometry(r * (k ? 0.72 : 1), 1); g.translate(x + Math.cos(a) * d, y + h * 0.66 + (k ? r * 0.15 * (k % 2) : 0), z + Math.sin(a) * d); worldUV(g, 1.6); B.geo(mats.leaves, g);
  }
}
// ---------------- the district ----------------
export async function buildDistrict({ scene, world, A, font, logos, street }) {
  const G = new THREE.Group(); G.name = 'district'; street.group.add(G);
  const B = new Batch(G);
  const out = { group: G, points: POINTS, routes: ROUTES, zones: ZONES, lampSources: street.lampSources, update: () => { } };
  const box = (...a) => world.box(...a);

  // ---- materials ----
  const mats = {
    slab: M({ map: CT.slabs() }), slabWarm: M({ map: CT.slabs([176, 166, 150]) }),
    asphalt: M({ map: CT.asphalt(66) }), asphaltDark: M({ map: CT.asphalt(54) }),
    granite: M({ map: CT.granite() }), quayWall: M({ map: CT.granite(), color: 0x9a968e }),
    planks: M({ map: CT.planks() }), wood: M({ map: CT.planks(), color: 0x8a7a66 }),
    brick: M({ map: (() => { const t = streetBrick(); return t; })(), color: 0xb08f80 }), brickDark: M({ map: streetBrick(), color: 0x7a665c }),
    plasterGrey: M({ map: CT.plaster([150, 146, 138]) }), plasterWarm: M({ map: CT.plaster([176, 160, 132]) }), plasterDark: M({ map: CT.plaster([70, 70, 72]) }),
    roof: M({ color: 0x3a3836 }), coping: M({ color: 0x8a867e }),
    steelGreen: M({ color: 0x2f4a3c, roughness: 0.7 }), steel: M({ color: 0x4a4e52, roughness: 0.65 }), iron: M({ color: 0x1e2022, roughness: 0.7 }),
    rust: M({ map: CT.corrugated([120, 72, 48]) }), contBlue: M({ map: CT.corrugated([46, 78, 112]) }), contRed: M({ map: CT.corrugated([128, 44, 36]) }), contGreen: M({ map: CT.corrugated([52, 92, 64]) }), contGrey: M({ map: CT.corrugated([120, 124, 126]) }),
    fence: M({ map: CT.chainLink(), transparent: false, alphaTest: 0.5, side: THREE.DoubleSide, roughness: 0.6 }),
    yellow: M({ color: 0xd9a92b }), white: M({ color: 0xd8d6cf }), red: M({ color: 0xa8322a }), black: M({ color: 0x151617 }),
    grass: M({ map: CT.plaster([88, 104, 62]), color: 0xb8c4a0 }), dirt: M({ map: CT.plaster([104, 92, 72]) }),
    bark: M({ color: 0x4a3a2c }), leaves: M({ map: leafTex() }), window: M({ color: 0x2a3138, roughness: 0.4 }),
  };
  function streetBrick() { if (!streetBrick.t) { const c = CT.canvas(256, 256), x = c.getContext('2d'); const rows = 8, bh = 32, bw = 64; x.fillStyle = '#4c423a'; x.fillRect(0, 0, 256, 256); for (let r = 0; r < rows; r++) for (let k = -1; k < 5; k++) { const v = 0.75 + Math.random() * 0.35; x.fillStyle = `rgb(${142 * v | 0},${74 * v | 0},${54 * v | 0})`; x.fillRect(k * bw + (r % 2) * bw / 2 + 2, r * bh + 2, bw - 4, bh - 4); } streetBrick.t = CT.ctex(c); } return streetBrick.t; }

  // ================= QUAY (west) =================
  const Q = D.quay;
  B.box(mats.granite, Q.x0, XW, -0.4, SW, Q.z0, 60, { uv: 3, ud: { ground: true } }); B.box(mats.granite, Q.x0, Q.x1, -0.4, SW, 60, Q.z1, { uv: 3, ud: { ground: true } });
  box(Q.x0, XW, Q.z0, 60, SW, -1, 'walk'); box(Q.x0, Q.x1, 60, Q.z1, SW, -1, 'walk');
  // kerb line where the street meets the promenade, coping stones along the water
  B.box(mats.coping, Q.x0, Q.x0 + 0.6, SW, SW + 0.12, Q.z0, Q.z1, { uv: 1 });
  B.box(mats.quayWall, Q.x0 - 0.05, Q.x0 + 0.02, D.waterY - 2, SW, Q.z0, Q.z1, { uv: 3, ud: { bakeCell: 1.5 } });
  // railing along the edge (gap for the pier); collision is taller than the rail so nobody jumps into the sea
  {
    const rail = (z0, z1) => {
      for (let z = z0; z <= z1 + 0.01; z += 2) B.box(mats.steelGreen, Q.x0 + 0.22, Q.x0 + 0.3, SW + 0.12, SW + 1.12, z - 0.04, z + 0.04, { uv: null });
      B.box(mats.steelGreen, Q.x0 + 0.2, Q.x0 + 0.32, SW + 1.08, SW + 1.16, z0, z1, { uv: null });
      B.box(mats.steelGreen, Q.x0 + 0.23, Q.x0 + 0.29, SW + 0.6, SW + 0.65, z0, z1, { uv: null });
      box(Q.x0 - 1, Q.x0 + 0.35, z0, z1, 2.2, -1, 'rail');
    };
    rail(Q.z0, D.pier.z0); rail(D.pier.z1, Q.z1);
    // mooring bollards
    for (let z = Q.z0 + 6; z < Q.z1 - 4; z += 9) { if (z > D.pier.z0 - 2 && z < D.pier.z1 + 2) continue; B.cyl(mats.iron, Q.x0 + 1.0, SW + 0.35, z, 0.17, 0.22, 0.7, 10); B.cyl(mats.iron, Q.x0 + 1.0, SW + 0.72, z, 0.24, 0.24, 0.08, 10); world.circle(Q.x0 + 1.0, z, 0.24, 0.8); }
  }
  // water (lit live, matte: no reflections, no glints)
  {
    const wc = CT.canvas(256, 256), wx = wc.getContext('2d'); wx.fillStyle = '#3b5a63'; wx.fillRect(0, 0, 256, 256);
    for (let i = 0; i < 1400; i++) { const v = Math.random(); wx.fillStyle = v > 0.5 ? 'rgba(120,150,156,0.10)' : 'rgba(10,30,36,0.12)'; wx.fillRect(Math.random() * 256, Math.random() * 256, 8 + Math.random() * 30, 1.5 + Math.random() * 2); }
    const wt = CT.ctex(wc); wt.repeat.set(60, 60);
    const water = new THREE.Mesh(new THREE.PlaneGeometry(800, 800).rotateX(-Math.PI / 2), new THREE.MeshLambertMaterial({ map: wt, color: 0xb8c4c4 }));
    water.position.set(Q.x0 - 400, D.waterY, 60); water.userData.noBake = true; water.userData.noOcclude = true; G.add(water);
    out.water = wt;
  }
  // the pier: deck on piles, railings, bollards, a lamp at the end
  {
    const P = D.pier;
    B.box(mats.planks, P.x0, P.x1 + 0.3, P.top - 0.12, P.top, P.z0, P.z1, { uv: 2, ud: { ground: true } });
    box(P.x0, P.x1 + 0.4, P.z0, P.z1, P.top, -1, 'pier');
    for (let x = P.x0 + 1; x < P.x1; x += 4) for (const z of [P.z0 + 0.4, P.z1 - 0.4, (P.z0 + P.z1) / 2]) B.cyl(mats.wood, x, (D.waterY - 3 + P.top) / 2, z, 0.18, 0.2, P.top - D.waterY + 3, 8);
    B.box(mats.wood, P.x0, P.x1, P.top - 0.4, P.top - 0.12, P.z0 - 0.15, P.z0 + 0.1, { uv: 2 }); B.box(mats.wood, P.x0, P.x1, P.top - 0.4, P.top - 0.12, P.z1 - 0.1, P.z1 + 0.15, { uv: 2 });
    for (const z of [P.z0 + 0.15, P.z1 - 0.15]) {
      for (let x = P.x0 + 0.2; x <= P.x1; x += 2) B.box(mats.wood, x - 0.07, x + 0.07, P.top, P.top + 1.05, z - 0.07, z + 0.07, { uv: 1 });
      B.box(mats.wood, P.x0, P.x1, P.top + 0.98, P.top + 1.1, z - 0.08, z + 0.08, { uv: 2 });
      box(P.x0 - 0.5, P.x1, z - 0.3, z + 0.3, 2.2, -1, 'rail');
    }
    for (let z = P.z0 + 0.2; z <= P.z1; z += 2) B.box(mats.wood, P.x0 + 0.08, P.x0 + 0.22, P.top, P.top + 1.05, z - 0.07, z + 0.07, { uv: 1 });
    B.box(mats.wood, P.x0 + 0.06, P.x0 + 0.24, P.top + 0.98, P.top + 1.1, P.z0, P.z1, { uv: 2 });
    box(P.x0 - 2, P.x0 + 0.35, P.z0 - 1, P.z1 + 1, 2.2, -1, 'rail');
    for (const x of [P.x0 + 3, P.x0 + 15]) for (const z of [P.z0 + 0.6, P.z1 - 0.6]) { B.cyl(mats.iron, x, P.top + 0.3, z, 0.15, 0.19, 0.6, 10); world.circle(x, z, 0.2, P.top + 0.65); }
    street.placeLamp(P.x0 + 1.2, P.z1 - 0.5, Math.PI, P.top);
  }
  // delivery point: container office, pallets with parcels, hand truck, painted sign
  {
    const cx0 = -82.6, cx1 = -80.2, cz0 = 79.6, cz1 = 85.8;
    B.box(mats.contGreen, cx0, cx1, SW, SW + 2.6, cz0, cz1, { uv: 1, uvv: 2.6 });
    B.box(mats.roof, cx0 - 0.05, cx1 + 0.05, SW + 2.6, SW + 2.7, cz0 - 0.05, cz1 + 0.05, { uv: null });
    B.box(mats.iron, cx1, cx1 + 0.04, SW, SW + 2.2, 82.3, 83.3, { uv: null });                       // door (closed: the clerk hands parcels out of the window)
    B.box(mats.window, cx1, cx1 + 0.04, SW + 1.1, SW + 2.0, 84.0, 85.3, { uv: null });
    B.box(mats.wood, cx1, cx1 + 0.5, SW + 1.05, SW + 1.12, 84.0, 85.3, { uv: 1 });                    // window counter
    box(cx0, cx1, cz0, cz1, 2.8, -1, 'cont');
    const sign = new THREE.Mesh(new THREE.PlaneGeometry(3.6, 0.9), M({ map: CT.signBoard(font, 'PUNKT DOSTAW', { fg: '#e8e2c8', bg: '#22313a', border: '#e8e2c8', sub: 'ODBIÓR PRZESYŁEK' }) }));
    sign.rotation.y = Math.PI / 2; sign.position.set(cx1 + 0.03, SW + 2.25, 82.7); G.add(sign);
    for (const [px, pz] of [[-78.6, 81.2], [-78.6, 79.6], [-76.9, 80.4]]) {
      B.box(mats.wood, px - 0.6, px + 0.6, SW, SW + 0.14, pz - 0.5, pz + 0.5, { uv: 1 });
      world.box(px - 0.6, px + 0.6, pz - 0.5, pz + 0.5, SW + 0.7, -1, 'pallet');
    }
    out.parcelSpots = [[-78.6, 81.2], [-78.6, 79.6], [-76.9, 80.4]];
  }
  const kit = await loadKit(A);
  for (const [px, pz, ry] of [[-78.6, 79.6, 0.3], [-76.9, 80.4, 1.9]]) place(G, kit.boxes, px, pz, ry, SW + 0.14);
  // harbour store (north part of the quay; closed roller doors), fence + stacked containers beyond it
  {
    const x0 = XW, x1 = -52, z0 = Q.z0, z1 = ST.near, h = 11;
    B.box(mats.brickDark, x0, x1, 0, h, z0, z1, { uv: 3, ud: { bakeCell: 1.5 } });
    B.box(mats.roof, x0 - 0.2, x1, h, h + 0.4, z0 - 0.2, z1 + 0.2, { uv: null });
    box(x0, x1, z0, z1, h, -1, 'bldg');
    for (const z of [0.5, 9.5]) { B.box(mats.contGrey, x0 - 0.06, x0, 0, 4.2, z - 2.4, z + 2.4, { uv: 1, uvv: 4.2 }); B.box(mats.iron, x0 - 0.2, x0, 4.2, 4.5, z - 2.6, z + 2.6, { uv: null }); }
    for (const z of [-3.5, 5, 14, 18.5]) B.box(mats.window, x0 - 0.04, x0, 6.5, 8.3, z - 0.9, z + 0.9, { uv: null });
    const sign = new THREE.Mesh(new THREE.PlaneGeometry(7, 1.2), M({ map: CT.signBoard(font, 'MAGAZYN PORTOWY 3', { fg: '#dcd6c8', bg: '#2a2b2c' }) }));
    sign.rotation.y = -Math.PI / 2; sign.position.set(x0 - 0.05, 5.4, 5); G.add(sign);
    // north end of the quay: harbour fence with a locked gate, containers behind
    B.box(mats.fence, Q.x0, x0, SW, 3.2, Q.z0 - 0.03, Q.z0 + 0.03, { uv: 1 });
    for (let x = Q.x0; x <= x0; x += 3) B.box(mats.steel, x - 0.05, x + 0.05, SW, 3.3, Q.z0 - 0.05, Q.z0 + 0.05, { uv: null });
    box(Q.x0 - 1, x0, Q.z0 - 0.4, Q.z0, 3.3, -1, 'fence');
    const cont = [mats.contBlue, mats.contRed, mats.contGrey, mats.contGreen];
    let k = 0; for (let x = Q.x0 + 0.5; x < x0 - 2; x += 2.6) for (let lvl = 0; lvl < 2 + (k % 2); lvl++) { B.box(cont[(k + lvl) % 4], x, x + 2.45, lvl * 2.6, lvl * 2.6 + 2.55, Q.z0 - 13, Q.z0 - 1, { uv: 1, uvv: 2.6 }); k++; }
    const warn = new THREE.Mesh(new THREE.PlaneGeometry(2.6, 0.9), M({ map: CT.signBoard(font, 'TEREN PORTU', { fg: '#1b1b1b', bg: '#e1c23a', sub: 'WSTĘP WZBRONIONY' }) }));
    warn.position.set(-75, 1.8, Q.z0 + 0.06); G.add(warn);
  }
  // palms in planters along the promenade, benches between them
  {
    for (const [z, ry] of [[-1, 0], [44, 0], [92, Math.PI]]) { const p = place(G, kit.palms, -68.4, z, ry); p.traverse(o => { if (o.isMesh) o.userData.noOcclude = true; }); }
    for (const z of [3.5, 10.5, 38.5, 49, 88.5, 97]) world.circle(-68.4, z, 0.5, 1.2);
    for (const z of [18, 58, 104]) { const b = place(G, kit.bench, -67.2, z, -Math.PI / 2, SW); world.box(-67.8, -66.6, z - 0.9, z + 0.9, 0.5, -1, 'bench'); }
  }
  // the street meets the quay: bollards across the road end
  for (let z = ST.road0 + 0.6; z < ST.road1; z += 1.6) { place(G, kit.bollard, XW - 0.4, z, 0, 0); world.circle(XW - 0.4, z, 0.14, 0.8); }

  // ================= FAR-SIDE FRONTAGE (south of the street) =================
  const fz = D.frontZ, bz = D.backZ;
  const FAR = [
    { x0: XW, x1: -52, h: 18, s: 'render2', z1: 60, west: true },
    { x0: -32, x1: -18, h: 15, s: 'brick2', sign: ['PIEKARNIA', '#c9a46a'] },
    { x0: -18, x1: -2, h: 17, s: 'brick', brama: true },
    { x0: -2, x1: 10, h: 16, s: 'brick' },
    { x0: 10, x1: 22, h: 13, s: 'render', sign: ['FRYZJER', '#8fa9c9'] },
    { x0: 22, x1: 34, h: 18, s: 'dark' },
    { x0: 34, x1: 46, h: 12, s: 'brick', sign: ['KWIATY', '#9cc48a'] },
    { x0: 46, x1: ST.x1, h: 15, s: 'render2' },
    { x0: ST.x1 + 9.6, x1: 88, h: 12, s: 'brick2' },
    { x0: 88, x1: XE, h: 15, s: 'render' },
  ];
  const sideMat = mats.brick;
  for (const b of FAR) {
    const w = b.x1 - b.x0, z1 = b.z1 || bz;
    const ft = facadeTextures(w, b.h, b.s, null);
    const fm = M({ map: ft.map, roughness: 0.82 });
    const cell = { bakeCell: 1.2 };
    if (b.brama) {
      const { x0: px0, x1: px1, h: ph } = D.brama;
      B.box(sideMat, b.x0, px0, 0, b.h, fz + 0.02, z1, { uv: 3, ud: cell }); B.box(sideMat, px1, b.x1, 0, b.h, fz + 0.02, z1, { uv: 3, ud: cell });
      B.box(sideMat, px0, px1, ph, b.h, fz + 0.02, z1, { uv: 3, ud: cell });
      facadePlane(B, fm, { bx0: b.x0, bx1: b.x1, h: b.h, xa: b.x0, xb: px0, at: fz, facing: '-z' });
      facadePlane(B, fm, { bx0: b.x0, bx1: b.x1, h: b.h, xa: px1, xb: b.x1, at: fz, facing: '-z' });
      facadePlane(B, fm, { bx0: b.x0, bx1: b.x1, h: b.h, xa: px0, xb: px1, ya: ph, at: fz, facing: '-z' });
      box(b.x0, px0, fz, z1, b.h, -1, 'bldg'); box(px1, b.x1, fz, z1, b.h, -1, 'bldg');
      // the passage: plastered walls, cobbled floor, ceiling, a lamp, open iron gate leaves
      B.box(mats.plasterWarm, px0 - 0.02, px0, 0, ph, fz, z1, { uv: 3 }); B.box(mats.plasterWarm, px1, px1 + 0.02, 0, ph, fz, z1, { uv: 3 });
      B.box(mats.plasterGrey, px0, px1, ph - 0.02, ph, fz, z1, { uv: 3 });
      B.box(mats.slabWarm, px0, px1, -0.2, SW, fz, z1, { uv: 2, ud: { ground: true } });
      box(px0, px1, fz, z1, SW, -1, 'walk');
      // arch frame
      B.box(mats.coping, px0 - 0.3, px0, 0, ph + 0.3, fz - 0.18, fz, { uv: null }); B.box(mats.coping, px1, px1 + 0.3, 0, ph + 0.3, fz - 0.18, fz, { uv: null }); B.box(mats.coping, px0 - 0.3, px1 + 0.3, ph, ph + 0.3, fz - 0.18, fz, { uv: null });
      const bars = (x, zA, zB) => { for (let z = zA; z <= zB; z += 0.14) B.box(mats.iron, x - 0.02, x + 0.02, 0.1, 2.9, z - 0.015, z + 0.015, { uv: null }); B.box(mats.iron, x - 0.03, x + 0.03, 0.15, 0.22, zA, zB, { uv: null }); B.box(mats.iron, x - 0.03, x + 0.03, 2.8, 2.88, zA, zB, { uv: null }); };
      bars(px0 + 0.08, fz + 0.4, fz + 2.3); bars(px1 - 0.08, fz + 0.4, fz + 2.3);
      out.bramaLight = new THREE.Vector3((px0 + px1) / 2, ph - 0.3, (fz + z1) / 2);
      continue;
    }
    B.box(sideMat, b.x0, b.x1, 0, b.h, fz + 0.02, z1, { uv: 3, ud: cell });
    facadePlane(B, fm, { bx0: b.x0, bx1: b.x1, h: b.h, xa: b.x0, xb: b.x1, at: fz, facing: '-z' });
    if (b.west) { const fw = facadeTextures(z1 - fz, b.h, b.s, null); facadePlane(B, M({ map: fw.map, roughness: 0.82 }), { bx0: fz, bx1: z1, h: b.h, xa: fz, xb: z1, at: b.x0 - 0.01, facing: '-x' }); }
    box(b.x0, b.x1, fz, z1, b.h, -1, 'bldg');
    if (b.sign) {
      const [text, color] = b.sign; const sw = Math.min(w - 1.2, text.length * 0.62 + 1.6);
      const sm = new THREE.Mesh(new THREE.BoxGeometry(sw, sw / 4, 0.12), [mats.roof, mats.roof, mats.roof, mats.roof, mats.roof, M({ map: CT.signBoard(font, text, { fg: color, bg: '#1a1b1d', size: text.length > 9 ? 120 : 150 }), roughness: 0.7 })]);
      sm.position.set((b.x0 + b.x1) / 2, 4.5, fz - 0.12); G.add(sm);
    }
  }
  // cornices and ledges on the far side
  for (const b of FAR) {
    const w = b.x1 - b.x0, cx = (b.x0 + b.x1) / 2;
    B.box(mats.coping, b.x0, b.x1, 4.22, 4.44, fz - 0.28, fz, { uv: null });
    B.box(mats.coping, b.x0 - 0.05, b.x1 + 0.05, b.h - 0.35, b.h + 0.15, fz - 0.45, fz, { uv: null });
    B.box(mats.roof, b.x0, b.x1, b.h, b.h + 0.5, fz, fz + 0.35, { uv: null });
    B.box(mats.roof, b.x0, b.x1, b.h - 0.02, b.h + 0.02, fz, b.z1 || bz, { uv: null });
    if (b.west) B.box(mats.coping, b.x0 - 0.45, b.x0, b.h - 0.35, b.h + 0.15, fz, b.z1, { uv: null });
    void w; void cx;
  }

  // ================= COURTYARD (blokowisko) =================
  const Y = D.yard;
  for (const [x0, x1, z0, z1] of [[-32, Y.x1, bz, 62], [-52, Y.x1, 62, Y.z1], [Y.x0, -52, 60, Y.z1]]) { B.box(mats.asphaltDark, x0, x1, -0.3, SW, z0, z1, { uv: 4, ud: { ground: true } }); box(x0, x1, z0, z1, SW, -1, 'walk'); }
  // pavement strips along the buildings
  B.box(mats.slab, -32, Y.x1, SW, SW + 0.02, bz, bz + 2.4, { uv: 2, ud: { ground: true } });
  B.box(mats.slab, -52, -32, SW, SW + 0.02, 62, 64.4, { uv: 2, ud: { ground: true } });
  B.box(mats.slab, Y.x0, Y.x1, SW, SW + 0.02, Y.z1 - 3, Y.z1, { uv: 2, ud: { ground: true } });
  // west wall with the gate from the quay (open: the shortcut back from the delivery point)
  {
    const G0 = D.yardGate, wx0 = G0.x - 0.5, wx1 = G0.x;
    for (const [za, zb] of [[60, G0.z0], [G0.z1, Y.z1]]) { B.box(mats.brick, wx0, wx1, 0, 3.2, za, zb, { uv: 2 }); B.box(mats.coping, wx0 - 0.08, wx1 + 0.08, 3.2, 3.34, za, zb, { uv: null }); box(wx0, wx1, za, zb, 3.3, -1, 'wall'); }
    for (const z of [G0.z0, G0.z1]) { B.box(mats.brick, wx0 - 0.15, wx1 + 0.15, 0, 3.7, z - 0.35, z + 0.35, { uv: 1 }); box(wx0 - 0.15, wx1 + 0.15, z - 0.35, z + 0.35, 3.7, -1, 'wall'); }
    // gate leaves swung open against the wall (inside)
    for (const [za, zb] of [[G0.z0 - 2.9, G0.z0 - 0.35], [G0.z1 + 0.35, G0.z1 + 2.9]]) { for (let z = za; z <= zb; z += 0.15) B.box(mats.iron, wx1 + 0.12, wx1 + 0.16, 0.12, 2.5, z - 0.016, z + 0.016, { uv: null }); B.box(mats.iron, wx1 + 0.1, wx1 + 0.18, 2.45, 2.55, za, zb, { uv: null }); B.box(mats.iron, wx1 + 0.1, wx1 + 0.18, 0.15, 0.22, za, zb, { uv: null }); }
    B.box(mats.slab, Q.x1, wx1 + 1, SW, SW + 0.02, G0.z0, G0.z1, { uv: 2, ud: { ground: true } });
  }
  // the volleyball court: painted surface, tall ball-stop fences at both ends, net folded at the east fence
  {
    const C = D.court;
    const ct = CT.courtTex(C.x1 - C.x0, C.z1 - C.z0, { len: C.len, wid: C.wid });
    const cg = new THREE.PlaneGeometry(C.x1 - C.x0, C.z1 - C.z0).rotateX(-Math.PI / 2); cg.translate((C.x0 + C.x1) / 2, SW + 0.025, (C.z0 + C.z1) / 2);
    B.geo(M({ map: ct }), cg, { ground: true });
    for (const x of [C.x0 - 0.3, C.x1 + 0.3]) {
      B.box(mats.fence, x - 0.02, x + 0.02, SW, 4.2, C.z0 + 0.5, C.z1 - 0.5, { uv: 1 });
      for (let z = C.z0 + 0.5; z <= C.z1 - 0.49; z += 2.5) B.box(mats.steel, x - 0.05, x + 0.05, SW, 4.3, z - 0.05, z + 0.05, { uv: null });
      B.box(mats.steel, x - 0.04, x + 0.04, 4.15, 4.25, C.z0 + 0.5, C.z1 - 0.5, { uv: null });
      box(x - 0.15, x + 0.15, C.z0 + 0.5, C.z1 - 0.5, 4.3, -1, 'fence');
    }
    // net post sleeves (flush, the posts are out) + folded net and posts leaning on the east fence
    for (const z of [C.z - C.wid / 2 - 1, C.z + C.wid / 2 + 1]) B.cyl(mats.steel, C.x, SW + 0.03, z, 0.09, 0.09, 0.04, 10);
    for (const z of [C.z - 1.6, C.z + 1.6]) { const g = new THREE.CylinderGeometry(0.05, 0.05, 2.6, 8); g.rotateX(0.18); g.translate(C.x1 - 0.05, SW + 1.25, z); B.geo(mats.steel, g); }
    { const g = new THREE.CylinderGeometry(0.11, 0.11, 3.2, 10); g.rotateX(Math.PI / 2); g.translate(C.x1 - 0.2, SW + 0.12, C.z); B.geo(mats.white, g); world.box(C.x1 - 0.35, C.x1, C.z - 1.7, C.z + 1.7, SW + 0.25, -1, 'net'); }
    // benches along the south side: the waiting place
    for (const x of [-15, -5]) { place(G, kit.bench, x, 89.6, Math.PI, SW); world.box(x - 0.9, x + 0.9, 89.2, 90.1, SW + 0.5, -1, 'bench'); }
  }
  // carpet-beater frame (trzepak), sandbox, bins shelter by the club's back door, garages along the west wall
  {
    const tx = -48, tz = 90;
    for (const x of [tx - 1.6, tx + 1.6]) B.cyl(mats.steelGreen, x, SW + 1.05, tz, 0.045, 0.045, 2.1, 8);
    B.cyl(mats.steelGreen, tx, SW + 2.05, tz, 0.035, 0.035, 3.3, 8, { rz: Math.PI / 2 }); B.cyl(mats.steelGreen, tx, SW + 1.25, tz, 0.03, 0.03, 3.3, 8, { rz: Math.PI / 2 });
    world.circle(tx - 1.6, tz, 0.08, 2.2); world.circle(tx + 1.6, tz, 0.08, 2.2);
    // sandbox
    B.box(mats.wood, -40, -36, SW, SW + 0.3, 92, 92.15, { uv: 1 }); B.box(mats.wood, -40, -36, SW, SW + 0.3, 95.85, 96, { uv: 1 }); B.box(mats.wood, -40, -39.85, SW, SW + 0.3, 92, 96, { uv: 1 }); B.box(mats.wood, -36.15, -36, SW, SW + 0.3, 92, 96, { uv: 1 });
    B.box(mats.dirt, -39.85, -36.15, SW, SW + 0.2, 92.15, 95.85, { uv: 2, ud: { ground: true } });
    world.box(-40, -36, 92, 96, SW + 0.3, -1, 'sand');
    // bin shelter (brick U) with dumpsters, near the club's back door
    const sx0 = -31.4, sz0 = 63.4;
    B.box(mats.brick, sx0, sx0 + 0.25, 0, 1.9, sz0, sz0 + 3.2, { uv: 2 }); B.box(mats.brick, sx0 + 4.5, sx0 + 4.75, 0, 1.9, sz0, sz0 + 3.2, { uv: 2 }); B.box(mats.brick, sx0, sx0 + 4.75, 0, 1.9, sz0 + 3, sz0 + 3.25, { uv: 2 });
    box(sx0, sx0 + 4.75, sz0, sz0 + 3.25, 1.9, -1, 'shelter');
    place(G, kit.dumpster, sx0 + 1.2, sz0 + 1.6, 0, SW); place(G, kit.dumpster, sx0 + 3.5, sz0 + 1.6, 0, SW);
    place(G, kit.bags, -36.8, 63.2, 0.4, SW);
    // garages
    for (const [k, z] of [[0, 84.5], [1, 91.5]]) { const g = kit.garages[k]; place(G, g, -60.7, z, -Math.PI / 2, SW); const [a, b2, c, d] = footprint(g, -60.7, z, -Math.PI / 2); world.box(a, b2, c, d, 2.8, -1, 'garage'); }
    // lamps
    street.placeLamp(-46, 70, Math.PI / 2); street.placeLamp(8, 70, -Math.PI / 2); street.placeLamp(-24, 95, Math.PI);
    // a couple of trees
    const TB = new Batch(G);
    addLeafTree(TB, mats, -54, 68, 8.5, SW, 1); addLeafTree(TB, mats, 12, 94, 7.5, SW, 2); addLeafTree(TB, mats, -30, 96.5, 7, SW, 3);
    TB.build().forEach(m => { m.userData.noOcclude = true; });
    world.circle(-54, 68, 0.3, 4); world.circle(12, 94, 0.3, 4); world.circle(-30, 96.5, 0.3, 4);
  }
  // panel blocks around the courtyard (south and east) + as skyline behind the near side
  {
    const T = kit.tall, Mi = kit.mid;
    const blocks = [
      { pc: T, x: -45, z: 106.5, ry: 0, door: -45 },
      { pc: T, x: -5, z: 106.5, ry: 0, door: -3.5 },
      { pc: T, x: 26.5, z: 69, ry: Math.PI / 2 },
      { pc: Mi, x: 27, z: 98, ry: Math.PI / 2 },
    ];
    for (const b of blocks) {
      place(G, b.pc, b.x, b.z, b.ry, 0, { bakeCell: 2.5 });
      const [a, b2, c, d, h] = footprint(b.pc, b.x, b.z, b.ry); world.box(a, b2, c, d, h, -1, 'block');
      b.fp = [a, b2, c, d];
    }
    // brick walls closing any gap between the blocks and the frontage
    const s0 = blocks[0].fp, s1 = blocks[1].fp, e0 = blocks[2].fp, e1 = blocks[3].fp;
    const wall = (x0, x1, z0, z1, h = 4) => { if (x1 - x0 < 0.05 || z1 - z0 < 0.05) return; B.box(mats.brick, x0, x1, 0, h, z0, z1, { uv: 2 }); B.box(mats.coping, x0 - 0.05, x1 + 0.05, h, h + 0.14, z0 - 0.05, z1 + 0.05, { uv: null }); box(x0, x1, z0, z1, h, -1, 'wall'); };
    wall(Y.x0 - 0.5, s0[0], Y.z1, Y.z1 + 0.5); wall(s0[1], s1[0], Y.z1, Y.z1 + 0.5); wall(s1[1], Y.x1 + 14, Y.z1, Y.z1 + 0.5);
    wall(Math.min(e0[0], e1[0]) - 0.5, Math.min(e0[0], e1[0]), bz, Y.z1 + 0.5);
    out.blocks = blocks;
    // stairwell entrance of the south-east block (the fight-1 opponent comes out of here)
    const dx = -3.5, dz = s1[2];
    B.box(mats.iron, dx - 0.75, dx + 0.75, SW, SW + 2.3, dz - 0.06, dz, { uv: null });
    B.box(mats.coping, dx - 1.4, dx + 1.4, SW + 2.5, SW + 2.65, dz - 1.4, dz, { uv: null });
    B.box(mats.slab, dx - 1.4, dx + 1.4, SW, SW + 0.15, dz - 1.4, dz, { uv: 1, ud: { ground: true } });
    world.box(dx - 1.4, dx + 1.4, dz - 1.4, dz, SW + 0.15, -1, 'step');
    const ds = new THREE.Mesh(new THREE.PlaneGeometry(1.2, 0.35), M({ map: CT.signBoard(font, 'KLATKA B', { fg: '#e9e6dc', bg: '#2a3a4a', size: 120 }) })); ds.position.set(dx, SW + 2.35, dz - 0.07); ds.rotation.y = Math.PI; G.add(ds);
    // skyline: blocks behind the near side and beyond the viaduct (no gameplay, just closes the horizon)
    for (const [pc, x, z, ry] of [[T, -40, -42, 0], [Mi, -2, -46, 0.08], [T, 36, -44, -0.05], [Mi, 92, -10, Math.PI / 2], [T, 118, 70, Math.PI / 2], [Mi, -30, 150, 0]]) place(G, pc, x, z, ry, 0, { bakeCell: 4 });
  }

  // ================= CLOSED ROAD TO THE FOREST (south end of the quay) =================
  {
    const fz0 = Q.z1, x0 = Q.x0, x1 = Q.x1;
    B.box(mats.asphalt, x0 + 6, x1 - 6, -0.3, 0.02, fz0, fz0 + 40, { uv: 4, ud: { noBake: true } });
    B.box(mats.grass, -150, 60, -0.6, 0.0, fz0 + 0.5, 240, { uv: 3, ud: { noBake: true } });
    // fence across with a locked gate and a barrier
    const gz = D.forestGate.z;
    B.box(mats.fence, x0, x1, SW, 3.0, gz - 0.03, gz + 0.03, { uv: 1 });
    for (let x = x0; x <= x1 + 0.01; x += 2.5) B.box(mats.steel, x - 0.05, x + 0.05, SW, 3.1, gz - 0.05, gz + 0.05, { uv: null });
    box(x0 - 1, x1, gz - 0.3, gz + 0.3, 3.2, -1, 'fence');
    const bx = -76; B.box(mats.white, bx, bx + 0.4, SW, SW + 1.1, gz - 1.2, gz - 0.8, { uv: null });
    for (let k = 0; k < 8; k++) B.box(k % 2 ? mats.white : mats.red, bx + 0.4 + k * 0.9, bx + 1.3 + k * 0.9, SW + 0.95, SW + 1.08, gz - 1.06, gz - 0.94, { uv: null });
    const sg = new THREE.Mesh(new THREE.PlaneGeometry(3.4, 1.1), M({ map: CT.signBoard(font, 'LAS · BUNKRY', { fg: '#f0ead8', bg: '#3a5a34', border: '#f0ead8', sub: 'DROGA ZAMKNIĘTA' }) }));
    sg.position.set(-70, 2.1, gz - 0.08); sg.rotation.y = Math.PI; G.add(sg);
    B.box(mats.steel, -71.8, -71.7, SW, 2.7, gz - 0.12, gz - 0.06, { uv: null }); B.box(mats.steel, -68.3, -68.2, SW, 2.7, gz - 0.12, gz - 0.06, { uv: null });
    // the forest beyond: a real piece of the plansza-3 forest (las_droga), its dirt road continues our road
    {
      const fr = kit.forest; const g = new THREE.Group(); g.position.set(-74, 0, fz0 + 38); G.add(g);
      for (const src of fr.children) { const mm = new THREE.Mesh(src.geometry, src.material); mm.userData.noOcclude = true; mm.userData.noBake = true; g.add(mm); }
      g.updateMatrixWorld(true);
    }
  }

  // ================= CLOSED ENTRY TO THE HOUSE ESTATE (east end) =================
  {
    const gx = D.estateGate.x;
    B.box(mats.fence, gx - 0.03, gx + 0.03, 0, 3.0, ST.near, D.frontZ, { uv: 1 });
    for (let z = ST.near; z <= D.frontZ + 0.01; z += 2.6) B.box(mats.steel, gx - 0.05, gx + 0.05, 0, 3.1, z - 0.05, z + 0.05, { uv: null });
    box(gx, gx + 0.4, ST.near - 1, D.frontZ + 1, 3.2, -1, 'fence');
    const bz0 = ST.road0 + 0.2; B.box(mats.white, gx - 1.2, gx - 0.8, 0, 1.1, bz0, bz0 + 0.4, { uv: null });
    for (let k = 0; k < 9; k++) B.box(k % 2 ? mats.white : mats.red, gx - 1.06, gx - 0.94, 0.95, 1.08, bz0 + 0.4 + k * 0.95, bz0 + 1.35 + k * 0.95, { uv: null });
    const sg = new THREE.Mesh(new THREE.PlaneGeometry(4.2, 1.2), M({ map: CT.signBoard(font, 'OSIEDLE DOMKÓW', { fg: '#f0ead8', bg: '#2e4a66', border: '#f0ead8', sub: 'WJAZD ZAMKNIĘTY' }) }));
    sg.position.set(gx - 0.1, 2.2, ST.road1 + 1.6); sg.rotation.y = -Math.PI / 2; G.add(sg);
    // beyond the gate: only the road continues (the house estate is plansza 2, built separately and joined here)
    B.box(mats.asphalt, gx, gx + 40, -0.3, 0.0, ST.road0, ST.road1, { uv: 4, ud: { noBake: true } });
  }

  // ================= props along the street =================
  {
    // the club entrance and the bakery: litter bins; post box; hydrant; cones by the closed estate gate
    place(G, kit.postbox, -24.5, ST.near + 0.5, 0, SW); world.circle(-24.5, ST.near + 0.5, 0.25, 1.2);
    for (const z of [ST.road0 + 1.2, ST.road0 + 3, ST.road0 + 4.8, ST.road1 - 1.2]) place(G, kit.cone, D.estateGate.x - 2.5, z, 0, 0);
    place(G, kit.blocks, D.estateGate.x - 3.6, ST.road0 + 6.5, Math.PI / 2, 0);
  }

  B.build();
  const interiors = await buildInteriors({ scene, world, font, logos, street, mats, Batch, M, kit, parent: G });
  out.interiors = interiors;
  out.bakeLights = [...interiors.bakeLights, { p: out.bramaLight, col: new THREE.Color(1, 0.86, 0.66).multiplyScalar(1.2), r: 8, lobe: 'down', size: 0.1 }];
  out.update = (dt, env, t) => { if (out.water) out.water.offset.set(t * 0.004, t * 0.0025); interiors.update && interiors.update(dt, env, t); };
  return out;
}

// the forest chunk was cut around its road entry (origin); keep only what lies south of the gate (z > -6)
function forestPiece(root) {
  const g = new THREE.Group(); root.updateMatrixWorld(true); const v = new THREE.Vector3();
  root.traverse(o => {
    if (!o.isMesh) return;
    const geo = floatGeo(o.geometry).applyMatrix4(o.matrixWorld);
    const p = geo.attributes.position, idx = geo.index; if (!idx) return;
    const keep = []; for (let t = 0; t < idx.count; t += 3) { const a = idx.getX(t), b = idx.getX(t + 1), c = idx.getX(t + 2); if ((p.getZ(a) + p.getZ(b) + p.getZ(c)) / 3 > -6) keep.push(a, b, c); }
    if (!keep.length) return; geo.setIndex(keep);
    const m = cleanMat([o.material].flat()[0]);
    if (m.transparent) { m.transparent = false; m.alphaTest = Math.max(m.alphaTest || 0, 0.45); m.depthWrite = true; }
    g.add(new THREE.Mesh(geo, m)); void v;
  });
  return g;
}

// ---------------- models used by the district ----------------
async function loadKit(A) {
  const [blok, props, garages, palms, forest] = await Promise.all(['city/blok.glb', 'city/props.glb', 'city/garages.glb', 'city/palms.glb', 'city/forest.glb'].map(p => A.loadGLTF(p)));
  const S = 3.5;
  const byName = (root, names) => collect(root.scene, o => names.includes(o.name));
  const byMat = (root, names) => collect(root.scene, o => [o.material].flat().some(m => names.includes(m.name)));
  const kit = {
    tall: piece(byName(blok, ['mesh_17_instance_1']), { S, square: true }),
    mid: piece(byName(blok, ['mesh_5_instance_2', 'mesh_6_instance_2']), { S, square: true }),
    dumpster: piece(byMat(props, ['Big_Bin', 'Big_Bin_Lid'])),
    bags: piece(byMat(props, ['Trash_Bag_2nd'])),
    bench: piece(byMat(props, ['Wood', 'Bench_Supports'])),
    bollard: piece(byMat(props, ['Metal_Bolard'])),
    postbox: piece(byMat(props, ['Red_Post_Box'])),
    cone: piece(byMat(props, ['Orange_Cone_3'])),
    blocks: piece(byMat(props, ['Concrete_Block'])),
    boxes: piece(byMat(props, ['Small_Cardboard_Box', 'Medium_Cardboard_Box', 'Large_CardBoard_Box', 'Carboard_Box_With_Handles'])),
    binGreen: piece(byMat(props, ['Bin_Body_Green', 'Green_lid'])),
    palms: piece(collect(palms.scene, () => true)),
    garages: [piece(byMat(garages, ['garage_1'])), piece(byMat(garages, ['garage_4']))],
    forest: forestPiece(forest.scene),
  };
  return kit;
}
