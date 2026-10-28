// The Crypt: a scanned vaulted undercroft far from the street, with an octagon cage in the middle,
// braziers, green neon and hard spotlights. The cage keeps fighters inside (polygon clamp).
import * as THREE from 'three';
import { addLampField } from './env.js';
import * as TX from './textures.js';

export const CRYPT = { x: 0, z: -320, R: 4.15, floor: 0.0 };

function canvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }

function matTexture(font, badge) {
  const S = 1024, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = '#14171a'; x.fillRect(0, 0, S, S);
  for (let i = 0; i < 9000; i++) { x.fillStyle = `rgba(255,255,255,${Math.random() * 0.025})`; x.fillRect(Math.random() * S, Math.random() * S, 2, 2); }
  x.save(); x.translate(S / 2, S / 2);
  const oct = (r) => { x.beginPath(); for (let k = 0; k < 8; k++) { const a = Math.PI / 8 + k * Math.PI / 4; const px = Math.cos(a) * r, py = Math.sin(a) * r; k ? x.lineTo(px, py) : x.moveTo(px, py); } x.closePath(); };
  x.strokeStyle = '#8bff3a'; x.lineWidth = 10; oct(470); x.stroke();
  x.strokeStyle = 'rgba(139,255,58,0.35)'; x.lineWidth = 4; oct(330); x.stroke();
  if (badge) { x.save(); x.beginPath(); x.arc(0, 0, 230, 0, Math.PI * 2); x.clip(); x.drawImage(badge, -230, -230, 460, 460); x.restore(); }
  x.fillStyle = 'rgba(228,232,225,0.85)'; x.font = `800 58px ${font}`; x.textAlign = 'center'; x.textBaseline = 'middle';
  for (let k = 0; k < 4; k++) { x.save(); x.rotate(k * Math.PI / 2); x.fillText(k % 2 ? 'FUEL A STRONGER YOU' : 'BRAXHORN CITY', 0, 395); x.restore(); }
  x.restore();
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 8; return t;
}

export async function buildCrypt({ scene, world, A, font, logos }) {
  const G = new THREE.Group(); G.name = 'crypt'; G.visible = false; scene.add(G);
  const out = { group: G, lampSources: [], fires: [], spots: [], animated: [] };
  const O = new THREE.Vector3(CRYPT.x, 0, CRYPT.z);
  // ---- scan ----
  const g = await A.loadGLTF('crypt.glb');
  const root = g.scene;
  // drop the vault above ~3.6 m: the camera looks down into the cage from above and would see the backs of the ribs
  A.trimScan(root, { shell: 0.3, keep: new THREE.Box3(new THREE.Vector3(-1e9, -1e9, -1e9), new THREE.Vector3(1e9, 1.8 + 3.6, 1e9)),
    drop: [new THREE.Box3(new THREE.Vector3(3.9 - 5.6, 1.8 + 0.15, 1.5 - 5.6), new THREE.Vector3(3.9 + 5.6, 1e9, 1.5 + 5.6))] }); // and the columns inside the cage
  const inner = new THREE.Group(); inner.add(root); inner.rotation.y = 0.098;
  const ref = new THREE.Vector3(3.9, 1.8, 1.5).applyMatrix4(new THREE.Matrix4().makeRotationY(0.098));
  inner.position.copy(ref).negate();
  const outer = new THREE.Group(); outer.add(inner); outer.position.copy(O); G.add(outer);
  root.traverse(o => { if (o.isMesh) { o.layers.set(3); addLampField(o.material, 'baked'); } });
  out.scan = outer;
  // dark vault ceiling so it reads as underground
  const ceil = new THREE.Mesh(new THREE.PlaneGeometry(70, 60), new THREE.MeshBasicMaterial({ color: 0x050505 }));
  ceil.rotation.x = Math.PI / 2; ceil.position.set(O.x, 9.4, O.z); G.add(ceil);
  // ---- octagon ----
  const og = await A.loadGLTF('oct.glb');
  const oct = og.scene; const ob = new THREE.Box3().setFromObject(oct), os = ob.getSize(new THREE.Vector3()), oc = ob.getCenter(new THREE.Vector3());
  const k = 9.6 / Math.max(os.x, os.z);
  const ho = new THREE.Group(); ho.add(oct); oct.position.set(-oc.x, -ob.min.y, -oc.z); ho.scale.setScalar(k); ho.position.copy(O); G.add(ho);
  ho.updateMatrixWorld(true);
  // find the mat (largest horizontal textured surface) and give it our own canvas
  let best = null, bestA = 0;
  oct.traverse(o => { if (o.isMesh) { const b = new THREE.Box3().setFromObject(o), s = b.getSize(new THREE.Vector3()); const area = s.x * s.z; if (s.y < 0.4 && area > bestA) { bestA = area; best = o; } } });
  const matTex = matTexture(font, logos.badgeImg);
  oct.traverse(o => { if (o.isMesh) { addLampField(o.material, 'lit'); o.receiveShadow = true; } });
  if (best) { const m = best.material.clone(); m.map = matTex; m.color.set(0xffffff); m.roughness = 0.75; m.needsUpdate = true; best.material = m; addLampField(m, 'lit'); best.receiveShadow = true; out.matMesh = best; }
  // floor height of the cage = top of that mesh
  const fb = best ? new THREE.Box3().setFromObject(best) : new THREE.Box3(new THREE.Vector3(), new THREE.Vector3(0, 0.6, 0));
  CRYPT.floor = Math.max(0.05, fb.max.y);
  out.floorY = CRYPT.floor;
  out.R = CRYPT.R;
  world.box(O.x - 4.9, O.x + 4.9, O.z - 4.9, O.z + 4.9, CRYPT.floor, -1, 'octagon');
  // keep the crypt floor walkable around the cage
  world.box(O.x - 40, O.x + 40, O.z - 40, O.z + 40, 0.0, -1, 'cryptfloor');
  // neon ring on top of the cage
  const ring = new THREE.Mesh(new THREE.TorusGeometry(5.0, 0.05, 6, 8), new THREE.MeshBasicMaterial({ color: new THREE.Color(0x8bff3a).multiplyScalar(1.6) }));
  ring.rotation.x = Math.PI / 2; ring.rotation.z = Math.PI / 8; ring.position.set(O.x, CRYPT.floor + 2.05, O.z); G.add(ring);
  out.lampSources.push({ pos: new THREE.Vector3(O.x, CRYPT.floor + 2.3, O.z), col: new THREE.Color(0x8bff3a).multiplyScalar(0.22), r: 7, kind: 'neon' });
  // spotlights from above
  for (const [dx, dz] of [[-3, -3], [3, -3], [-3, 3], [3, 3]]) {
    const s = new THREE.SpotLight(0xfff2e0, 95, 16, 0.55, 0.55, 1.3); s.position.set(O.x + dx * 1.4, 6.8, O.z + dz * 1.4); s.target.position.set(O.x + dx * 0.25, CRYPT.floor, O.z + dz * 0.25);
    G.add(s, s.target); out.spots.push(s);
    const lamp = new THREE.Mesh(new THREE.CylinderGeometry(0.25, 0.35, 0.4, 12), new THREE.MeshStandardMaterial({ color: 0x111111, metalness: 0.8, roughness: 0.4 })); lamp.position.copy(s.position); G.add(lamp);
    const disc = new THREE.Mesh(new THREE.CircleGeometry(0.3, 16), new THREE.MeshBasicMaterial({ color: new THREE.Color(1, 0.96, 0.9).multiplyScalar(6) })); disc.rotation.x = Math.PI / 2; disc.position.copy(s.position).y -= 0.21; G.add(disc);
  }
  // braziers
  const halo = TX.radial('rgba(255,200,120,1)', 'rgba(255,120,40,0)');
  const iron = addLampField(new THREE.MeshStandardMaterial({ color: 0x1b1b1b, metalness: 0.8, roughness: 0.5 }));
  for (const [dx, dz] of [[-11, -8], [11, -8], [-11, 8], [11, 8], [0, -13], [0, 12]]) {
    const p = new THREE.Vector3(O.x + dx, 0, O.z + dz);
    const bowl = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 0.25, 0.35, 12), iron); bowl.position.copy(p).setY(1.05); G.add(bowl);
    const leg = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.12, 0.9, 6), iron); leg.position.copy(p).setY(0.45); G.add(leg);
    world.circle(p.x, p.z, 0.55, 1.3);
    const flames = [];
    for (let i = 0; i < 3; i++) { const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: halo, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, color: new THREE.Color(1, 0.6, 0.25) })); s.position.copy(p).setY(1.45 + i * 0.2); s.scale.setScalar(1.1 - i * 0.25); G.add(s); flames.push(s); }
    const L = { pos: p.clone().setY(1.8), col: new THREE.Color(1, 0.5, 0.18).multiplyScalar(2.2), r: 9, kind: 'fire', base: 2.2 };
    out.lampSources.push(L); out.fires.push({ flames, L, ph: Math.random() * 10 });
  }
  out.update = (dt, t) => {
    for (const f of out.fires) {
      const fl = 0.8 + 0.2 * Math.sin(t * 13 + f.ph) * Math.sin(t * 7.3 + f.ph * 2) + 0.08 * Math.random();
      f.L.col.setRGB(1, 0.5, 0.18).multiplyScalar(f.L.base * fl);
      f.flames.forEach((s, i) => { s.scale.setScalar((1.1 - i * 0.25) * (0.85 + 0.3 * fl)); s.position.y = 1.45 + i * 0.22 + 0.05 * Math.sin(t * 9 + i); });
    }
  };
  // clamp a fighter inside the cage (regular octagon, flat sides facing the axes)
  out.clampInside = (f) => {
    const R = CRYPT.R - f.radius * 0.6;
    let dx = f.pos.x - O.x, dz = f.pos.z - O.z;
    for (let k = 0; k < 8; k++) {
      const a = k * Math.PI / 4, nx = Math.cos(a), nz = Math.sin(a);
      const d = dx * nx + dz * nz;
      if (d > R) { dx -= nx * (d - R); dz -= nz * (d - R); }
    }
    f.pos.x = O.x + dx; f.pos.z = O.z + dz;
  };
  out.origin = O;
  return out;
}
