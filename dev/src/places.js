// "Rewiry": separate fight locations built from photogrammetry scans, reached by car from the city map.
// Each place: the scan (unlit, lit by the lamp field), mesh-derived collision, a playable stretch with
// invisible end walls, spawn/exit points and gang spawn spots. Places sit far from the street in world space.
import * as THREE from 'three';
import { addLampField } from './env.js';
import { buildMeshCollider } from './meshcol.js';

export const PLACES = {
  tunnel: {
    key: 'tunnel', file: 'tunnel.glb', title: 'Tunel Futatsugoya', sub: 'Opuszczona górska droga. Tu Czerwony Róg przeładowuje towar.',
    at: [0, 0, 1400], rotY: 0, mood: 'indoor', room: 'crypt',
    // playable stretch in scan-local coordinates (x0, x1, z0, z1); the tunnel is ~5 m wide, the portal at z = 197
    play: [-5, 4, 64, 196], spawn: [-0.4, 188], exit: [-0.4, 193], face: Math.PI,
    gang: [[0, 160], [1.0, 150], [-1.0, 140], [0.6, 130], [-0.6, 120]],
    lamps: { from: 70, to: 186, every: 13, y: 3.7, col: [1.0, 0.55, 0.22], k: 2.4, r: 8.5 },
    reward: 900, boss: { name: 'Szczur', title: 'Kurier Czerwonego Rogu', person: 'Big_Vegas', hp: 230, size: 1.1, dmg: 1.0, moves: ['jab', 'hook', 'kick', 'kicks', 'knee'], reward: 400,
      intro: 'Zgubiłeś się? Tunel jest płatny. Płacisz zębami.', outro: 'Dobra… towar jest twój…' },
    waves: [['Ch06_nonPBR', 'Ch08_nonPBR'], ['Ch13_nonPBR', 'Ch17_nonPBR', 'Ch18_nonPBR'], ['Ch27_nonPBR', 'Ch37_nonPBR', 'Remy']],
  },
  alley: {
    key: 'alley', file: 'alley.glb', title: 'Zaułek Freemana', sub: 'Wąski zaułek pełen graffiti w Nowym Jorku. Rewir Graficiarzy.',
    at: [-900, 0, 0], rotY: 0, mood: 'outdoor', room: 'street',
    play: [-4.5, 8, -23, 22.5], spawn: [2.4, 17.5], exit: [2.4, 19.5], face: Math.PI,
    gang: [[2.5, 8], [2.5, 2], [2.5, -4], [2.5, -10], [1.5, -17]],
    lamps: { from: -20, to: 16, every: 9, y: 3.6, col: [1.0, 0.78, 0.5], k: 1.7, r: 7 },
    reward: 1100, boss: { name: 'Szpachla', title: 'Szef Graficiarzy', person: 'The_Boss', hp: 260, size: 1.05, dmg: 1.05, moves: ['bat1', 'bat2', 'bat3', 'clubKick'], bat: true, reward: 500,
      intro: 'To moja galeria. A ty będziesz moim nowym muralem.', outro: 'Farba… wszędzie farba…' },
    waves: [['Ch38_nonPBR', 'Ch39_nonPBR'], ['Arissa', 'Lola_B_Styperek', 'Ch06_nonPBR'], ['Kachujin_G_Rosales', 'Ch17_nonPBR', 'Ch37_nonPBR']],
  },
  warehouse: {
    key: 'warehouse', file: 'warehouse.glb', title: 'Stary magazyn', sub: 'Opuszczona hala na obrzeżach. Tu Czerwony Róg trzyma kasę.', lit: true,
    at: [700, 0, -700], rotY: 0, mood: 'indoor', room: 'gym',
    play: [-13.6, 18.6, -12.3, -0.3], spawn: [-11.5, -6.3], exit: [-12.8, -6.3], face: Math.PI / 2,
    gang: [[0, -6], [3, -4], [6, -8], [9, -5], [12, -7]],
    lamps: { from: -10, to: 16, every: 6.5, y: 4.0, col: [1.0, 0.86, 0.66], k: 1.6, r: 8, axis: 'x', at: -6.3 },
    reward: 1300, boss: { name: 'Księgowy', title: 'Skarbnik Czerwonego Rogu', person: 'Ch16_nonPBR', hp: 290, size: 1.12, dmg: 1.1, moves: ['jab', 'hook', 'kick', 'kicks', 'elbow', 'knee'], reward: 600,
      intro: 'Każdy dolar w tej hali jest policzony. Ty też zaraz będziesz.', outro: 'Rachunek… się nie zgadza…' },
    waves: [['Ch12_nonPBR', 'Ch22_nonPBR'], ['Ch26_nonPBR', 'Ch31_nonPBR', 'Ch47_nonPBR'], ['Big_Vegas', 'The_Boss', 'Remy']],
  },
  fitness: {
    key: 'fitness', file: 'fitness.glb', title: 'Klub Złoty Róg', sub: 'Elegancki fitness klub. Za ladą recepcji Czerwony Róg pierze pieniądze.', lit: true, scale: 0.1,
    at: [-700, 0, 700], rotY: 0, mood: 'bright', room: 'gym',
    play: [40, 148, -54, 73], spawn: [126, 46], exit: [121, 51], face: -2.28,
    gang: [[90, 25], [78, 12], [100, 5], [70, -5], [95, -20]],
    lamps: { from: -40, to: 60, every: 25, y: 4.0, col: [1.0, 0.97, 0.92], k: 0.6, r: 9 },
    reward: 1500, boss: { name: 'Trener', title: 'Właściciel klubu Złoty Róg', person: 'Ch31_nonPBR', hp: 320, size: 1.15, dmg: 1.15, moves: ['jab', 'hook', 'kick', 'kicks', 'knee', 'mma2'], reward: 700,
      intro: 'Karnet się skończył. Wynocha albo cię wyniosą.', outro: 'Dobra… daję ci… darmowy miesiąc…' },
    waves: [['Arissa', 'Kachujin_G_Rosales'], ['Ch12_nonPBR', 'Ch26_nonPBR', 'Lola_B_Styperek'], ['Ch47_nonPBR', 'Ch22_nonPBR', 'Big_Vegas']],
  },
};

export async function buildPlace(def, { scene, world, A, onStatus }) {
  const G = new THREE.Group(); G.name = 'place-' + def.key; G.visible = false; scene.add(G);
  const out = { def, group: G, lampSources: [], glows: [] };
  const g = await A.loadGLTF(def.file);
  const root = g.scene;
  const holder = new THREE.Group(); holder.add(root); holder.rotation.y = def.rotY; holder.position.set(...def.at); if (def.scale) holder.scale.setScalar(def.scale); G.add(holder);
  holder.updateMatrixWorld(true);
  root.traverse(o => { if (o.isMesh) { addLampField(o.material, def.lit ? 'lit' : 'baked'); o.layers.set(3); if (def.lit) { o.receiveShadow = true; } } });
  const L = (x, y, z) => new THREE.Vector3(x, y, z).applyMatrix4(holder.matrixWorld);
  out.L = L;
  onStatus && onStatus('Liczę kolizje…');
  await new Promise(r => setTimeout(r, 20));
  const col = buildMeshCollider(root, { cell: 0.25 });
  // playable stretch: region + invisible walls around it
  const [x0, x1, z0, z1] = def.play;
  const a = L(x0, 0, z0), b = L(x1, 0, z1);
  const minX = Math.min(a.x, b.x), maxX = Math.max(a.x, b.x), minZ = Math.min(a.z, b.z), maxZ = Math.max(a.z, b.z);
  out.region = world.addRegion(minX - 6, maxX + 6, minZ - 6, maxZ + 6, col); out.region.off = true;
  out.bounds = { minX, maxX, minZ, maxZ };
  for (const [bx0, bx1, bz0, bz1] of [[minX - 1, minX, minZ - 1, maxZ + 1], [maxX, maxX + 1, minZ - 1, maxZ + 1], [minX - 1, maxX + 1, minZ - 1, minZ], [minX - 1, maxX + 1, maxZ, maxZ + 1]])
    world.box(bx0, bx1, bz0, bz1, 60, -60, 'placewall');
  out.col = col;
  // spawn / exit / gang spots on the floor
  // lowest floor in the cell = the road / ground (ceilings and roofs also count as "floors" in the collider)
  const lowest = (wx, wz) => { let best = Infinity; for (const [dx, dz] of [[0, 0], [0.3, 0], [-0.3, 0], [0, 0.3], [0, -0.3]]) { const i = col.idx(wx + dx, wz + dz); if (i >= 0 && col.floors[i] && col.floors[i].length) best = Math.min(best, col.floors[i][0]); } return best; };
  const floorAt = (x, z) => { const p = L(x, 0, z); const gy = lowest(p.x, p.z); return new THREE.Vector3(p.x, Number.isFinite(gy) ? gy : 0, p.z); };
  out.lowest = lowest;
  out.spawn = floorAt(...def.spawn); out.exit = floorAt(...def.exit);
  out.gang = def.gang.map(([x, z]) => floorAt(x, z));
  out.face = def.face + def.rotY;
  // lamps along the stretch (lamp field + small glows)
  const halo = (() => { const c = document.createElement('canvas'); c.width = c.height = 64; const x = c.getContext('2d'); const gr = x.createRadialGradient(32, 32, 0, 32, 32, 32); for (const [o, al] of [[0, 1], [0.1, 0.8], [0.3, 0.2], [1, 0]]) gr.addColorStop(o, `rgba(255,255,255,${al})`); x.fillStyle = gr; x.fillRect(0, 0, 64, 64); return new THREE.CanvasTexture(c); })();
  const lm = def.lamps;
  const cx = (x0 + x1) / 2;
  for (let z = lm.from; z <= lm.to; z += lm.every) {
    const f = lm.axis === 'x' ? floorAt(z, lm.at) : floorAt(cx, z);
    const p = new THREE.Vector3(f.x, f.y + lm.y, f.z);
    out.lampSources.push({ pos: p, col: new THREE.Color(...lm.col).multiplyScalar(lm.k), r: lm.r, kind: 'place' });
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: halo, color: new THREE.Color(...lm.col), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    s.position.copy(p).y += 0.3; s.scale.setScalar(1.3); G.add(s); out.glows.push(s);
  }
  out.update = (dt, env, t) => {
    const n = def.mood === 'indoor' ? 1 : env.lamps;
    for (const s of out.glows) s.material.opacity = 0.15 + 0.85 * n;
  };
  return out;
}
