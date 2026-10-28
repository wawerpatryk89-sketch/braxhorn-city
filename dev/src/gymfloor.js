// Matte floors for the gym (black rubber tiles with fine coloured flecks) and the alley outside (dry, cracked
// concrete). No mirror reflections: a real gym floor is dull rubber, not a wet street.
import * as THREE from 'three';
import { addLampField } from './env.js';

let seed = 4242;
const rnd = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };

function canvas(n) { const c = document.createElement('canvas'); c.width = c.height = n; return c; }
function heightToNormal(hc, k) {
  const W = hc.width, H = hc.height, src = hc.getContext('2d').getImageData(0, 0, W, H).data;
  const out = canvas(W), ox = out.getContext('2d'), img = ox.createImageData(W, H), d = img.data;
  const h = (x, y) => src[(((y + H) % H) * W + ((x + W) % W)) * 4] / 255;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const dx = (h(x + 1, y) - h(x - 1, y)) * k, dy = (h(x, y + 1) - h(x, y - 1)) * k, l = Math.hypot(dx, dy, 1), o = (y * W + x) * 4;
    d[o] = (-dx / l * 0.5 + 0.5) * 255; d[o + 1] = (dy / l * 0.5 + 0.5) * 255; d[o + 2] = (1 / l * 0.5 + 0.5) * 255; d[o + 3] = 255;
  }
  ox.putImageData(img, 0, 0); return out;
}
function tex(c, srgb, rep) { const t = new THREE.CanvasTexture(c); if (srgb) t.colorSpace = THREE.SRGBColorSpace; t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(rep[0], rep[1]); t.anisotropy = 8; return t; }

// 4 x 4 rubber tiles (1 m each) per texture
function rubberTextures(rep) {
  const N = 1024, T = N / 4, c = canvas(N), x = c.getContext('2d'), hc = canvas(N), hx = hc.getContext('2d');
  hx.fillStyle = '#909090'; hx.fillRect(0, 0, N, N);
  for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) {
    const v = 26 + rnd() * 6; x.fillStyle = `rgb(${v | 0},${v | 0},${(v + 2) | 0})`; x.fillRect(i * T, j * T, T, T);
  }
  // EPDM flecks
  const fl = [['#5d6064', 9000, 1.6], ['#8d9094', 2600, 1.4], ['#3e6f2c', 700, 1.5], ['#24262a', 6000, 2.2]];
  for (const [col, n, r] of fl) { x.fillStyle = col; for (let k = 0; k < n; k++) { const s = r * (0.6 + rnd() * 0.8); x.fillRect(rnd() * N, rnd() * N, s, s); } }
  // scuffs: soft lighter patches where people train
  for (let k = 0; k < 18; k++) { const g = x.createRadialGradient(0, 0, 0, 0, 0, 1); g.addColorStop(0, 'rgba(255,255,255,0.05)'); g.addColorStop(1, 'rgba(255,255,255,0)'); x.save(); x.translate(rnd() * N, rnd() * N); x.scale(60 + rnd() * 140, 40 + rnd() * 90); x.fillStyle = g; x.fillRect(-1, -1, 2, 2); x.restore(); }
  // seams
  x.fillStyle = 'rgba(0,0,0,0.85)'; hx.fillStyle = '#404040';
  for (let i = 0; i <= 4; i++) { x.fillRect(i * T - 1.5, 0, 3, N); x.fillRect(0, i * T - 1.5, N, 3); hx.fillRect(i * T - 2, 0, 4, N); hx.fillRect(0, i * T - 2, N, 4); }
  // fine rubber grain in the relief
  for (let k = 0; k < 40000; k++) { const v = 128 + (rnd() - 0.5) * 40 | 0; hx.fillStyle = `rgb(${v},${v},${v})`; hx.fillRect(rnd() * N, rnd() * N, 1.5, 1.5); }
  return { map: tex(c, true, rep), normalMap: tex(heightToNormal(hc, 1.6), false, rep) };
}

// dry concrete with expansion joints, cracks and stains (6 x 6 m per texture)
function concreteTextures(rep) {
  const N = 1024, c = canvas(N), x = c.getContext('2d'), hc = canvas(N), hx = hc.getContext('2d');
  x.fillStyle = '#4b4945'; x.fillRect(0, 0, N, N); hx.fillStyle = '#808080'; hx.fillRect(0, 0, N, N);
  for (let k = 0; k < 26000; k++) { const v = 60 + rnd() * 40 | 0; x.fillStyle = `rgba(${v},${v - 2},${v - 6},0.35)`; const s = 1 + rnd() * 3; x.fillRect(rnd() * N, rnd() * N, s, s); }
  for (let k = 0; k < 30; k++) { const g = x.createRadialGradient(0, 0, 0, 0, 0, 1); g.addColorStop(0, `rgba(30,28,25,${0.12 + rnd() * 0.15})`); g.addColorStop(1, 'rgba(30,28,25,0)'); x.save(); x.translate(rnd() * N, rnd() * N); x.scale(30 + rnd() * 120, 20 + rnd() * 80); x.fillStyle = g; x.fillRect(-1, -1, 2, 2); x.restore(); }
  // joints every 2 m
  x.fillStyle = 'rgba(25,24,22,0.8)'; hx.fillStyle = '#383838';
  for (let i = 0; i <= 3; i++) { const p = i * N / 3; x.fillRect(p - 2, 0, 4, N); x.fillRect(0, p - 2, N, 4); hx.fillRect(p - 3, 0, 6, N); hx.fillRect(0, p - 3, N, 6); }
  // cracks
  x.strokeStyle = 'rgba(20,19,17,0.75)'; hx.strokeStyle = '#3a3a3a';
  for (let k = 0; k < 14; k++) {
    let px = rnd() * N, py = rnd() * N, a = rnd() * Math.PI * 2; x.lineWidth = hx.lineWidth = 1 + rnd() * 1.5;
    x.beginPath(); hx.beginPath(); x.moveTo(px, py); hx.moveTo(px, py);
    for (let s = 0; s < 18; s++) { a += (rnd() - 0.5) * 0.9; px += Math.cos(a) * 9; py += Math.sin(a) * 9; x.lineTo(px, py); hx.lineTo(px, py); }
    x.stroke(); hx.stroke();
  }
  for (let k = 0; k < 50000; k++) { const v = 128 + (rnd() - 0.5) * 60 | 0; hx.fillStyle = `rgb(${v},${v},${v})`; hx.fillRect(rnd() * N, rnd() * N, 2, 2); }
  return { map: tex(c, true, rep), normalMap: tex(heightToNormal(hc, 2.2), false, rep) };
}

export function createGymFloors({ hall, alley, wallT = 0.6 }) {
  const group = new THREE.Group(); group.name = 'floors';
  // hall (incl. the alcoves behind the back arches)
  const z0 = hall.minZ - 3.6, z1 = hall.maxZ + wallT, w = hall.maxX - hall.minX + 1.2, d = z1 - z0;
  const rt = rubberTextures([w / 4, d / 4]);
  const rubber = addLampField(new THREE.MeshStandardMaterial({ map: rt.map, normalMap: rt.normalMap, normalScale: new THREE.Vector2(0.6, 0.6), roughness: 0.9, metalness: 0, envMapIntensity: 0.15 }), 'lit');
  const g1 = new THREE.PlaneGeometry(w, d).rotateX(-Math.PI / 2); g1.translate(0, 0, (z0 + z1) / 2);
  const m1 = new THREE.Mesh(g1, rubber); m1.receiveShadow = true; group.add(m1);
  // alley
  const aw = alley.x1 - alley.x0 + 1.2, ad = alley.z1 + 0.6 - z1;
  const ct = concreteTextures([aw / 6, ad / 6]);
  const conc = addLampField(new THREE.MeshStandardMaterial({ map: ct.map, normalMap: ct.normalMap, normalScale: new THREE.Vector2(0.8, 0.8), roughness: 0.95, metalness: 0, envMapIntensity: 0.1 }), 'lit');
  const g2 = new THREE.PlaneGeometry(aw, ad).rotateX(-Math.PI / 2); g2.translate((alley.x0 + alley.x1) / 2, 0.002, z1 + ad / 2);
  const m2 = new THREE.Mesh(g2, conc); m2.receiveShadow = true; group.add(m2);
  // drain grate in the alley
  const grate = new THREE.Mesh(new THREE.PlaneGeometry(0.6, 0.9).rotateX(-Math.PI / 2), new THREE.MeshStandardMaterial({ color: 0x0e0f10, roughness: 0.8, metalness: 0.2 }));
  grate.position.set((alley.x0 + alley.x1) / 2 + 1.2, 0.006, alley.z1 - 3.5); group.add(grate);
  return { mesh: group, update() { }, setSize() { }, uniforms: { reflStrength: { value: 0 } } };
}
