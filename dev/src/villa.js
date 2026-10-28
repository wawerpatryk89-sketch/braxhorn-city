// The hero's villa on the hill: a separate location (like the crypt) with a walkable inside (two storeys),
// pool, terrace and the garage. Collision comes from the model itself (meshcol.js).
import * as THREE from 'three';
import { addLampField } from './env.js';
import { buildMeshCollider } from './meshcol.js';

export const VILLA = { x: 420, z: 0, S: 0.15, floor0: 39.6 };

function waterNormal() {
  const S = 256, c = document.createElement('canvas'); c.width = c.height = S; const x = c.getContext('2d'); const img = x.createImageData(S, S), d = img.data;
  const h = new Float32Array(S * S);
  for (let k = 0; k < 7; k++) { const fx = 1 + Math.floor(Math.random() * 4), fz = 1 + Math.floor(Math.random() * 4), ph = Math.random() * 6.28, a = 1 / (k + 1); for (let j = 0; j < S; j++) for (let i = 0; i < S; i++) h[j * S + i] += Math.sin((i * fx + j * fz) / S * Math.PI * 2 + ph) * a; }
  for (let j = 0; j < S; j++) for (let i = 0; i < S; i++) {
    const dx = h[j * S + (i + 1) % S] - h[j * S + (i - 1 + S) % S], dz = h[((j + 1) % S) * S + i] - h[((j - 1 + S) % S) * S + i];
    const l = Math.hypot(dx * 3, dz * 3, 1), o = (j * S + i) * 4;
    d[o] = (-dx * 3 / l * 0.5 + 0.5) * 255; d[o + 1] = (-dz * 3 / l * 0.5 + 0.5) * 255; d[o + 2] = (1 / l * 0.5 + 0.5) * 255; d[o + 3] = 255;
  }
  x.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(c); t.wrapS = t.wrapT = THREE.RepeatWrapping; return t;
}

const up = (o, re) => { for (let p = o; p; p = p.parent) if (re.test(p.name || '')) return true; return false; };

export async function buildVilla({ scene, world, A, onStatus }) {
  const G = new THREE.Group(); G.name = 'villa'; G.visible = false; scene.add(G);
  const out = { group: G, lampSources: [], lights: [], animated: [] };
  const g = await A.loadGLTF('villa.glb');
  const root = g.scene;
  root.scale.setScalar(VILLA.S); root.position.set(VILLA.x, -VILLA.floor0 * VILLA.S, VILLA.z);
  G.add(root); root.updateMatrixWorld(true);
  // open doorways
  root.traverse(o => { if (/^Door\d*$/.test(o.name)) o.visible = false; });
  // materials: shadows, lamp field, glass, water
  const wn = waterNormal(); wn.repeat.set(3, 3);
  const glassMat = addLampField(new THREE.MeshStandardMaterial({ color: 0x9db8c8, roughness: 0.05, metalness: 0.9, transparent: true, opacity: 0.32, envMapIntensity: 1.6, depthWrite: false, emissive: 0xffc890, emissiveIntensity: 0 }));
  const waterMat = new THREE.MeshStandardMaterial({ color: 0x1d8fa8, roughness: 0.06, metalness: 0.2, normalMap: wn, normalScale: new THREE.Vector2(0.6, 0.6), transparent: true, opacity: 0.88, envMapIntensity: 1.8, emissive: 0x0aa5c8, emissiveIntensity: 0 });
  addLampField(waterMat, 'lit');
  out.glassMat = glassMat; out.waterMat = waterMat;
  root.traverse(o => {
    if (!o.isMesh) return;
    const ms = Array.isArray(o.material) ? o.material : [o.material];
    const isWater = up(o, /^Water/);
    o.material = (Array.isArray(o.material) ? ms : [ms[0]]).map(m => {
      if (isWater) return waterMat;
      if (m.transmission > 0.01 || (m.transparent && m.opacity < 0.6) || /glass|window/i.test(m.name)) return glassMat;
      if (m.isMeshPhysicalMaterial) { const n = new THREE.MeshStandardMaterial(); THREE.MeshStandardMaterial.prototype.copy.call(n, m); m = n; }
      addLampField(m, 'lit'); return m;
    });
    if (!Array.isArray(ms) || ms.length === 1) o.material = o.material[0];
    o.castShadow = !up(o, /Plane|Water|Cube029/); o.receiveShadow = true;
  });
  onStatus && onStatus('Liczę kolizje willi…');
  await new Promise(r => setTimeout(r, 20));
  const col = buildMeshCollider(root, { cell: 0.25, exclude: (o) => !o.visible || up(o, /Tree|Oak|Bonsai|ferns|trop_leav|plants|Water/), wallExclude: (o) => up(o, /rock/) });
  const b = new THREE.Box3().setFromObject(root);
  out.region = world.addRegion(b.min.x, b.max.x, b.min.z, b.max.z, col); out.region.off = true;
  out.col = col;
  // night lights: rooms, terrace, pool, garden
  const P = (x, y, z) => new THREE.Vector3(VILLA.x + x * VILLA.S, (y - VILLA.floor0) * VILLA.S, VILLA.z + z * VILLA.S);
  const warm = new THREE.Color(1.0, 0.72, 0.45);
  for (const [x, y, z, r, k] of [[-30, 56, -45, 8, 0.7], [0, 56, -45, 8, 0.7], [25, 56, -40, 7, 0.6], [-35, 56, -20, 6, 0.55], [-10, 77, -45, 8, 0.6], [20, 77, -45, 7, 0.55], [-35, 77, -25, 6, 0.55]])
    out.lampSources.push({ pos: P(x, y, z), col: warm.clone().multiplyScalar(k), r, kind: 'home' });
  out.lampSources.push({ pos: P(16, 36, -117), col: new THREE.Color(0.2, 0.9, 1.2), r: 9, kind: 'pool' });
  for (const [x, z] of [[-60, -95], [50, -95], [-60, 0], [45, 5], [-30, 20]]) out.lampSources.push({ pos: P(x, 48, z), col: warm.clone().multiplyScalar(0.7), r: 7, kind: 'garden' });
  // a real warm light inside for specular on the hero
  const pl = new THREE.PointLight(0xffc89a, 0, 14, 1.8); pl.position.copy(P(-15, 57, -40)); G.add(pl); out.lights.push(pl);
  // points of interest
  out.spawn = P(-8, 40, 10);             // in front of the house
  out.sofa = P(-15, 40, -45);
  out.shop = P(-38, 40, -27);           // kitchen island laptop
  out.car = P(-33, 24, 31);             // the Porsche on the driveway
  out.update = (dt, env, t) => {
    wn.offset.x = (t * 0.02) % 1; wn.offset.y = (t * 0.013) % 1;
    const n = env.lamps;
    glassMat.emissiveIntensity = 0.06 * n; waterMat.emissiveIntensity = 0.3 * n;
    pl.intensity = 9 * n;
  };
  return out;
}
