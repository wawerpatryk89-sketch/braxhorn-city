// The level: an old brick boxing hall like the one in the reference video.
// Layout (metres): hall x -18..18, z -13..11, height 8.5. Ring at the back right, heavy bags on the left.
import * as THREE from 'three';
import { mergeGeometries } from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import * as T from './textures.js';

export const HALL = { minX: -18, maxX: 18, minZ: -13, maxZ: 11, height: 8.5 };
export const DOOR = { x0: -6.8, x1: -5.2, h: 3.0 };
export const ALLEY = { x0: -14.5, x1: 1.0, z1: 20.6 };
export const RING = { x: 9, z: -5.5, half: 3.5, apron: 4.2, h: 0.95, gapMin: 8.15, gapMax: 9.85 };

function worldUV(geo, scale, offset = 0) {
  const p = geo.attributes.position, n = geo.attributes.normal;
  const uv = new Float32Array(p.count * 2);
  for (let i = 0; i < p.count; i++) {
    const ax = Math.abs(n.getX(i)), ay = Math.abs(n.getY(i)), az = Math.abs(n.getZ(i));
    let u, v;
    if (ax >= ay && ax >= az) { u = p.getZ(i); v = p.getY(i); }
    else if (ay >= az) { u = p.getX(i); v = p.getZ(i); }
    else { u = p.getX(i); v = p.getY(i); }
    uv[i * 2] = u / scale + offset; uv[i * 2 + 1] = v / scale;
  }
  geo.setAttribute('uv', new THREE.BufferAttribute(uv, 2));
  return geo;
}

class Batch {
  constructor() { this.groups = new Map(); }
  add(mat, geo, opts = {}) {
    const key = mat.uuid + (opts.cast ? 'c' : '') + (opts.receive === false ? 'n' : '');
    if (!this.groups.has(key)) this.groups.set(key, { mat, geos: [], cast: !!opts.cast, receive: opts.receive !== false });
    const g = geo.index ? geo.toNonIndexed() : geo;
    for (const k of Object.keys(g.attributes)) if (!['position', 'normal', 'uv'].includes(k)) g.deleteAttribute(k);
    if (!g.attributes.uv) g.setAttribute('uv', new THREE.BufferAttribute(new Float32Array(g.attributes.position.count * 2), 2));
    this.groups.get(key).geos.push(g);
  }
  box(mat, cx, cy, cz, w, h, d, opts = {}) {
    const g = new THREE.BoxGeometry(w, h, d);
    if (opts.rotY) g.rotateY(opts.rotY);
    g.translate(cx, cy, cz);
    if (opts.uvScale) worldUV(g, opts.uvScale, opts.uvOffset || 0);
    this.add(mat, g, opts);
  }
  cyl(mat, x, y, z, rt, rb, h, seg = 16, opts = {}) {
    const g = new THREE.CylinderGeometry(rt, rb, h, seg, 1, !!opts.open);
    if (opts.rotX) g.rotateX(opts.rotX);
    if (opts.rotZ) g.rotateZ(opts.rotZ);
    if (opts.rotY) g.rotateY(opts.rotY);
    g.translate(x, y, z);
    this.add(mat, g, opts);
  }
  build(scene) {
    if (!this.groups.size) return;
    for (const { mat, geos, cast, receive } of this.groups.values()) {
      const m = new THREE.Mesh(mergeGeometries(geos), mat);
      m.castShadow = cast; m.receiveShadow = receive;
      scene.add(m);
    }
  }
}

function gradientTex(stops) {
  const c = document.createElement('canvas'); c.width = 4; c.height = 256;
  const x = c.getContext('2d'); const g = x.createLinearGradient(0, 0, 0, 256);
  for (const [o, col] of stops) g.addColorStop(o, col);
  x.fillStyle = g; x.fillRect(0, 0, 4, 256);
  const t = new THREE.CanvasTexture(c); return t;
}

function lightConeMaterial(color, strength) {
  return new THREE.ShaderMaterial({
    uniforms: { uColor: { value: new THREE.Color(color) }, uStrength: { value: strength } },
    vertexShader: `varying float vH; varying vec3 vN; varying vec3 vV;
      void main(){ vH = uv.y; vec4 mv = modelViewMatrix * vec4(position,1.0); vN = normalize(normalMatrix * normal); vV = normalize(-mv.xyz); gl_Position = projectionMatrix * mv; }`,
    fragmentShader: `uniform vec3 uColor; uniform float uStrength; varying float vH; varying vec3 vN; varying vec3 vV;
      void main(){ float edge = pow(abs(dot(vN, vV)), 2.5); float a = pow(vH, 1.6) * edge * uStrength; gl_FragColor = vec4(uColor * a, 1.0); }`,
    transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide, toneMapped: true,
  });
}

export function buildGym(scene, world, quality, font, logos = null) {
  const root = new THREE.Group(); root.name = 'gym'; scene.add(root);
  const outerScene = scene; scene = root;
  const out = { lights: [], bags: [], animated: [], glowSprites: [], spawnBallsAt: [], root };
  const B = new Batch();
  const cut = { ceil: new THREE.Group(), px: new THREE.Group(), nx: new THREE.Group(), pz: new THREE.Group(), nz: new THREE.Group() };
  for (const g of Object.values(cut)) scene.add(g);
  out.cut = cut;
  const BC = { ceil: new Batch(), px: new Batch(), nx: new Batch(), pz: new Batch(), nz: new Batch() };

  // ---------------- materials ----------------
  const brickT = T.brick();
  brickT.map.repeat.set(1, 1); brickT.normalMap.repeat.set(1, 1);
  const brickMat = new THREE.MeshStandardMaterial({ map: brickT.map, normalMap: brickT.normalMap, normalScale: new THREE.Vector2(1.2, 1.2), roughness: 0.88, color: 0xd9c2b0, envMapIntensity: 0.3 });
  const brickDark = new THREE.MeshStandardMaterial({ map: brickT.map, normalMap: brickT.normalMap, roughness: 0.92, color: 0x8a7468, envMapIntensity: 0.2 });
  const woodT = T.wood(); woodT.repeat.set(1, 1);
  const ceilMat = new THREE.MeshStandardMaterial({ map: woodT, roughness: 0.85, color: 0x9a8070 });
  const beamMat = new THREE.MeshStandardMaterial({ map: woodT, roughness: 0.8, color: 0x6a4c38 });
  const plyMat = new THREE.MeshStandardMaterial({ map: woodT, roughness: 0.7, color: 0xe6c79a });
  const ironMat = new THREE.MeshStandardMaterial({ color: 0x1d2024, roughness: 0.45, metalness: 0.75 });
  const steelMat = new THREE.MeshStandardMaterial({ color: 0x6f757c, roughness: 0.35, metalness: 0.9 });
  const ductMat = new THREE.MeshStandardMaterial({ color: 0x4a4f55, roughness: 0.5, metalness: 0.8 });
  const redPaint = new THREE.MeshStandardMaterial({ color: 0xa3201b, roughness: 0.45, metalness: 0.2 });
  const navyPaint = new THREE.MeshStandardMaterial({ color: 0x1a2030, roughness: 0.5, metalness: 0.2 });
  const ropeMat = new THREE.MeshStandardMaterial({ color: 0xeae5da, roughness: 0.55 });
  const rubberMat = new THREE.MeshStandardMaterial({ color: 0x141414, roughness: 0.82 });
  const padRed = new THREE.MeshStandardMaterial({ color: 0x8c1814, roughness: 0.55 });
  const padBlack = new THREE.MeshStandardMaterial({ color: 0x15161a, roughness: 0.6 });
  const matRed = new THREE.MeshStandardMaterial({ color: 0x5a1512, roughness: 0.75 });
  const lockerMat = new THREE.MeshStandardMaterial({ color: 0x34433f, roughness: 0.5, metalness: 0.55 });
  const shelfStuff = [0x3b2f28, 0x2a2e33, 0x5b4632, 0x6b1d18, 0x283a2f].map(c => new THREE.MeshStandardMaterial({ color: c, roughness: 0.8 }));
  const shadeOut = new THREE.MeshStandardMaterial({ color: 0x24272b, roughness: 0.4, metalness: 0.85, side: THREE.DoubleSide });
  const shadeIn = new THREE.MeshBasicMaterial({ color: new THREE.Color(0xfff1dc).multiplyScalar(7) });
  const warmBulb = new THREE.MeshBasicMaterial({ color: new THREE.Color(0xffb35a).multiplyScalar(5) });
  const coneWarm = lightConeMaterial(0xffe2b8, 0.026);

  // ---------------- floor sides / walls ----------------
  const { minX, maxX, minZ, maxZ, height: H } = HALL;
  const wallT = 0.6;
  // left / right / front walls with pilasters
  BC.nx.box(brickMat, minX - wallT / 2, H / 2, (minZ + maxZ) / 2, wallT, H, maxZ - minZ + 1.2, { uvScale: 2.2 });
  BC.px.box(brickMat, maxX + wallT / 2, H / 2, (minZ + maxZ) / 2, wallT, H, maxZ - minZ + 1.2, { uvScale: 2.2 });
  const D = DOOR, A = ALLEY;
  BC.pz.box(brickMat, (minX - 0.6 + D.x0) / 2, H / 2, maxZ + wallT / 2, D.x0 - (minX - 0.6), H, wallT, { uvScale: 2.2 });
  BC.pz.box(brickMat, (D.x1 + maxX + 0.6) / 2, H / 2, maxZ + wallT / 2, maxX + 0.6 - D.x1, H, wallT, { uvScale: 2.2 });
  BC.pz.box(brickMat, (D.x0 + D.x1) / 2, (D.h + H) / 2, maxZ + wallT / 2, D.x1 - D.x0, H - D.h, wallT, { uvScale: 2.2 });
  // the alley outside the door
  BC.pz.box(brickDark, A.x0 - 0.3, H / 2 + 0.5, (maxZ + wallT + A.z1) / 2, 0.6, H + 1, A.z1 - maxZ - wallT, { uvScale: 2.2 });
  BC.pz.box(brickDark, A.x1 + 0.3, H / 2 + 0.5, (maxZ + wallT + A.z1) / 2, 0.6, H + 1, A.z1 - maxZ - wallT, { uvScale: 2.2 });
  for (const z of [-9, -3, 3, 9]) {
    BC.nx.box(brickMat, minX + 0.25, H / 2, z, 0.5, H, 0.9, { uvScale: 2.2 });
    BC.px.box(brickMat, maxX - 0.25, H / 2, z, 0.5, H, 0.9, { uvScale: 2.2 });
    world.boxC(minX + 0.25, z, 0.5, 0.9, H); world.boxC(maxX - 0.25, z, 0.5, 0.9, H);
  }
  world.box(minX - 3, minX, minZ - 3, maxZ + 3, H); world.box(maxX, maxX + 3, minZ - 3, maxZ + 3, H);
  world.box(minX - 3, DOOR.x0, maxZ, maxZ + wallT, H); world.box(DOOR.x1, maxX + 3, maxZ, maxZ + wallT, H);
  out.doorCol = world.box(DOOR.x0, DOOR.x1, maxZ, maxZ + wallT, DOOR.h, 0, 'door');
  world.box(minX - 3, ALLEY.x0, maxZ, ALLEY.z1, H + 1); world.box(ALLEY.x1, maxX + 3, maxZ, ALLEY.z1, H + 1);

  // back wall with 4 big arches (polygon walked along the bottom edge)
  const arches = [-13.5, -4.5, 4.5, 13.5], aw = 2.9, spring = 3.6;
  const shape = new THREE.Shape();
  shape.moveTo(minX - 0.6, 0); shape.lineTo(minX - 0.6, H); shape.lineTo(maxX + 0.6, H); shape.lineTo(maxX + 0.6, 0);
  for (let i = arches.length - 1; i >= 0; i--) {
    const cx = arches[i];
    shape.lineTo(cx + aw, 0); shape.lineTo(cx + aw, spring);
    shape.absarc(cx, spring, aw, 0, Math.PI, false);
    shape.lineTo(cx - aw, 0);
  }
  shape.lineTo(minX - 0.6, 0);
  const bw = new THREE.ExtrudeGeometry(shape, { depth: wallT, bevelEnabled: false, curveSegments: 28 });
  bw.translate(0, 0, minZ - wallT);
  bw.computeVertexNormals();
  worldUV(bw, 2.2);
  BC.nz.add(brickMat, bw);
  // arch brick rims (voussoirs) – a slightly protruding band
  for (const cx of arches) {
    const rim = new THREE.Shape();
    rim.absarc(cx, spring, aw + 0.42, 0, Math.PI, false); rim.lineTo(cx - aw, spring);
    rim.absarc(cx, spring, aw, Math.PI, 0, true); rim.lineTo(cx + aw + 0.42, spring);
    const rg = new THREE.ExtrudeGeometry(rim, { depth: 0.12, bevelEnabled: false, curveSegments: 28 });
    rg.translate(0, 0, minZ); rg.computeVertexNormals(); worldUV(rg, 1.1, 0.37);
    BC.nz.add(brickDark, rg);
  }
  // collision for back wall pieces between arches + alcove interiors
  const pier = [minX, ...arches.flatMap(c => [c - aw, c + aw]), maxX];
  for (let i = 0; i < pier.length; i += 2) world.box(pier[i], pier[i + 1], minZ - 3, minZ, H);

  // alcoves behind the arches
  const alcD = 2.4;
  arches.forEach((cx, i) => {
    const z0 = minZ - wallT, zb = z0 - alcD;
    BC.nz.box(brickDark, cx, H / 2, zb - 0.3, aw * 2 + 0.6, H, 0.6, { uvScale: 2.2 });
    BC.nz.box(brickDark, cx - aw - 0.3, H / 2, (z0 + zb) / 2, 0.6, H, alcD, { uvScale: 2.2 });
    BC.nz.box(brickDark, cx + aw + 0.3, H / 2, (z0 + zb) / 2, 0.6, H, alcD, { uvScale: 2.2 });
    BC.ceil.box(ceilMat, cx, spring + aw + 0.15, (z0 + zb) / 2, aw * 2 + 0.6, 0.3, alcD + 0.6, { uvScale: 3 });
    world.box(cx - aw - 0.6, cx + aw + 0.6, zb - 0.6, zb + 0.55, H);
    // shelves with gear
    if (i !== 3) {
      for (const sy of [0.9, 1.9, 2.9]) {
        B.box(beamMat, cx, sy, zb + 0.45, aw * 1.6, 0.06, 0.6, { uvScale: 2 });
        for (let k = 0; k < 7; k++) {
          const w = 0.25 + 0.3 * ((k * 37 + i * 11 + sy * 7) % 10) / 10, hh = 0.2 + 0.4 * ((k * 53 + i * 5) % 7) / 7;
          B.box(shelfStuff[(k + i + (sy * 10 | 0)) % shelfStuff.length], cx - aw * 0.7 + k * aw * 0.22, sy + 0.03 + hh / 2, zb + 0.45, w * 0.7, hh, 0.4, { cast: false });
        }
      }
      world.box(cx - aw * 0.8, cx + aw * 0.8, zb, zb + 0.8, 3.2);
    } else {
      // banners behind the ring
      if (logos) {
        const ban = new THREE.Mesh(new THREE.PlaneGeometry(5.4, 5.4 * logos.wide.image.height / logos.wide.image.width), new THREE.MeshBasicMaterial({ map: logos.wide, color: new THREE.Color(1, 1, 1).multiplyScalar(1.25) }));
        ban.position.set(cx, 4.9, zb + 0.06); cut.nz.add(ban);
        const bd = new THREE.Mesh(new THREE.PlaneGeometry(1.7, 1.7), new THREE.MeshBasicMaterial({ map: logos.badge }));
        bd.position.set(cx, 2.4, zb + 0.06); cut.nz.add(bd);
      } else for (const [bx, mat] of [[cx - 1.4, padRed], [cx, padBlack], [cx + 1.4, padRed]]) B.box(mat, bx, 4.6, zb + 0.05, 1.0, 2.6, 0.04);
    }
    // warm sconce in each alcove
    const sx = cx, sy = 3.4, sz = zb + 0.12;
    BC.nz.box(ironMat, sx, sy, sz, 0.18, 0.3, 0.12);
    BC.nz.cyl(warmBulb, sx, sy + 0.05, sz + 0.12, 0.07, 0.07, 0.2, 10);
    out.glowSprites.push({ pos: new THREE.Vector3(sx, sy + 0.05, sz + 0.3), color: 0xffa24a, size: 1.3, group: 'nz' });
    { const pl = new THREE.PointLight(0xff9a40, 18, 9, 2); pl.position.set(sx, sy + 0.2, sz + 0.9); pl.userData.tier = i === 1 ? 1 : 2; pl.visible = pl.userData.tier <= quality.pointLights; scene.add(pl); out.lights.push(pl); }
  });

  // sconces on side wall pilasters
  for (const z of [-9, -3, 3, 9]) for (const side of [-1, 1]) {
    const x = side < 0 ? minX + 0.55 : maxX - 0.55;
    const SB = side < 0 ? BC.nx : BC.px;
    SB.box(ironMat, x, 3.2, z, 0.12, 0.28, 0.2);
    SB.cyl(warmBulb, x - side * 0.12, 3.25, z, 0.06, 0.06, 0.18, 8);
    out.glowSprites.push({ pos: new THREE.Vector3(x - side * 0.25, 3.25, z), color: 0xffa24a, size: 1.0, group: side < 0 ? 'nx' : 'px' });
  }

  // small arched windows on the left wall (dim night light)
  const winMat = new THREE.MeshBasicMaterial({ color: new THREE.Color(0x2a3d5c).multiplyScalar(1.4) });
  for (const z of [-6, 0, 6]) {
    const ws = new THREE.Shape(); ws.moveTo(-0.7, 0); ws.lineTo(-0.7, 1.3); ws.absarc(0, 1.3, 0.7, Math.PI, 0, true); ws.lineTo(0.7, 0); ws.lineTo(-0.7, 0);
    const wg = new THREE.ShapeGeometry(ws, 16); wg.rotateY(Math.PI / 2); wg.translate(minX + 0.01, 4.6, z);
    BC.nx.add(winMat, wg, { receive: false });
    BC.nx.box(ironMat, minX + 0.04, 5.25, z, 0.04, 0.05, 1.4); BC.nx.box(ironMat, minX + 0.04, 5.3, z, 0.04, 2.0, 0.05);
  }

  // ---------------- ceiling ----------------
  const ceil = new THREE.PlaneGeometry(maxX - minX + 1.2, maxZ - minZ + 1.2); ceil.rotateX(Math.PI / 2); ceil.translate(0, H, (minZ + maxZ) / 2);
  worldUV(ceil, 3);
  BC.ceil.add(ceilMat, ceil);
  for (let z = minZ + 1.5; z < maxZ; z += 3) BC.ceil.box(beamMat, 0, H - 0.3, z, maxX - minX, 0.55, 0.38, { uvScale: 2.5 });
  for (const x of [-12, -6, 0, 6, 12]) BC.ceil.box(beamMat, x, H - 0.75, (minZ + maxZ) / 2, 0.3, 0.35, maxZ - minZ, { uvScale: 2.5 });
  for (const [z, y] of [[minZ + 1.2, 7.4], [maxZ - 1.4, 7.5]]) {
    BC.ceil.cyl(ductMat, 0, y, z, 0.42, 0.42, maxX - minX - 1, 20, { rotZ: Math.PI / 2 });
    for (let x = minX + 2; x < maxX; x += 4) BC.ceil.cyl(ironMat, x, y, z, 0.45, 0.45, 0.08, 20, { rotZ: Math.PI / 2 });
  }

  // ---------------- pendant lamps ----------------
  const lampSpots = [
    [-12, -8], [-4, -8], [2.5, -9.5],
    [-12, -1.5], [-4, -1.5], [2, -2],
    [-12, 5.5], [-4, 5.5], [4, 5.5], [12, 5.5],
    [7.2, -7.3], [10.8, -7.3], [7.2, -3.7], [10.8, -3.7],
  ];
  const shadeProfile = [];
  for (let i = 0; i <= 12; i++) { const t = i / 12; shadeProfile.push(new THREE.Vector2(0.08 + 0.52 * Math.pow(t, 1.4), -0.42 * t)); }
  const shadeGeo = new THREE.LatheGeometry(shadeProfile, 24);
  const discGeo = new THREE.CircleGeometry(0.56, 24); discGeo.rotateX(Math.PI / 2);
  const coneGeo = new THREE.CylinderGeometry(0.5, 2.6, 5.2, 24, 1, true);
  const lampY = (x, z) => (Math.abs(x - RING.x) < 3 && Math.abs(z - RING.z) < 3) ? 5.4 : 6.2;
  for (const [x, z] of lampSpots) {
    const y = lampY(x, z);
    const g = shadeGeo.clone(); g.translate(x, y, z); BC.ceil.add(shadeOut, g, { cast: false });
    const d = discGeo.clone(); d.translate(x, y - 0.4, z); BC.ceil.add(shadeIn, d, { receive: false });
    BC.ceil.cyl(ironMat, x, (y + H) / 2, z, 0.015, 0.015, H - y, 4);
    { const cone = new THREE.Mesh(coneGeo, coneWarm); cone.position.set(x, y - 0.4 - 2.6, z);
      cone.layers.set(2); cone.renderOrder = 5; cone.visible = !!quality.cones; cut.ceil.add(cone); out.cones = out.cones || []; out.cones.push(cone); }
    out.glowSprites.push({ pos: new THREE.Vector3(x, y - 0.45, z), color: 0xfff0d8, size: 1.5, group: 'ceil' });
  }
  // a few real spot lights under the lamps (no shadow; one shadow light follows the player)
  const spotDefs = [[9, -5.5, 5.4, 85, 0.95], [-4, -1.5, 6.2, 120, 0.9], [-12, -4.5, 6.2, 110, 0.95], [4, 5.5, 6.2, 95, 0.9], [-8, 6, 6.2, 85, 0.9], [2.5, -9, 6.2, 85, 0.9]];
  spotDefs.forEach(([x, z, y, I, ang], i) => {
    const s = new THREE.SpotLight(0xfff0dc, I * 1.9, 18, ang, 0.65, 1.5);
    s.position.set(x, y - 0.4, z); s.target.position.set(x, 0, z); s.userData.rank = i; s.visible = i < quality.spots; scene.add(s, s.target); out.lights.push(s);
  });

  // ---------------- boxing ring ----------------
  const R = RING;
  const ringTop = new THREE.MeshStandardMaterial({ map: T.ringCanvas(font, logos && logos.badgeImg), roughness: 0.9, color: 0xb8b8b8 });
  const skirtT = T.apronSkirt(font); skirtT.repeat.set(1, 1);
  const skirtMat = new THREE.MeshStandardMaterial({ map: skirtT, roughness: 0.7 });
  const plat = new THREE.Mesh(new THREE.BoxGeometry(R.apron * 2, 0.06, R.apron * 2), [padBlack, padBlack, ringTop, padBlack, padBlack, padBlack]);
  plat.position.set(R.x, R.h - 0.03, R.z); plat.receiveShadow = true; plat.castShadow = true; scene.add(plat);
  // skirt on four sides
  for (let s = 0; s < 4; s++) {
    const g = new THREE.PlaneGeometry(R.apron * 2, R.h - 0.06);
    const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * 2.2);
    const m = new THREE.Mesh(g, skirtMat);
    const a = s * Math.PI / 2;
    m.position.set(R.x + Math.sin(a) * R.apron, (R.h - 0.06) / 2, R.z + Math.cos(a) * R.apron);
    m.rotation.y = a; m.receiveShadow = true; scene.add(m);
  }
  world.box(R.x - R.apron, R.x + R.apron, R.z - R.apron, R.z + R.apron, R.h, 0, 'ring');
  // posts, pads, ropes
  const corners = [[-1, 1, redPaint], [1, 1, navyPaint], [1, -1, redPaint], [-1, -1, navyPaint]];
  const postH = 1.55;
  for (const [sx, sz, mat] of corners) {
    const px = R.x + sx * R.half, pz = R.z + sz * R.half;
    B.cyl(mat, px, R.h + postH / 2, pz, 0.07, 0.07, postH, 12, { cast: true });
    B.box(mat === redPaint ? padRed : padBlack, px - sx * 0.12, R.h + 0.85, pz - sz * 0.12, 0.22, 1.2, 0.22, { cast: true });
    world.circle(px, pz, 0.2, R.h + postH, R.h - 0.05);
  }
  const ropeH = [0.42, 0.82, 1.22];
  const ropeTube = (a, b) => {
    const mid = a.clone().add(b).multiplyScalar(0.5); mid.y -= 0.035 * a.distanceTo(b) / 7;
    return new THREE.TubeGeometry(new THREE.QuadraticBezierCurve3(a, mid, b), 24, 0.028, 6, false);
  };
  const P = (sx, sz, h, inset = 0.05) => new THREE.Vector3(R.x + sx * (R.half - inset), R.h + h, R.z + sz * (R.half - inset));
  for (const h of ropeH) {
    B.add(ropeMat, ropeTube(P(-1, -1, h), P(1, -1, h)), { cast: true });
    B.add(ropeMat, ropeTube(P(-1, -1, h), P(-1, 1, h)), { cast: true });
    B.add(ropeMat, ropeTube(P(1, -1, h), P(1, 1, h)), { cast: true });
    // front side (toward the hall) has a gate opening by the stairs
    B.add(ropeMat, ropeTube(P(-1, 1, h), new THREE.Vector3(R.gapMin, R.h + h, R.z + R.half - 0.05)), { cast: true });
    B.add(ropeMat, ropeTube(new THREE.Vector3(R.gapMax, R.h + h, R.z + R.half - 0.05), P(1, 1, h)), { cast: true });
  }
  for (const gx of [R.gapMin, R.gapMax]) {
    B.cyl(padBlack, gx, R.h + 0.7, R.z + R.half - 0.05, 0.06, 0.06, 1.4, 10, { cast: true });
    world.circle(gx, R.z + R.half - 0.05, 0.1, R.h + 1.4, R.h - 0.05);
  }
  const rb = R.h + 0.25, rt = R.h + 1.45, th = 0.09;
  world.box(R.x - R.half - th, R.x - R.half + th, R.z - R.half, R.z + R.half, rt, rb, 'rope');
  world.box(R.x + R.half - th, R.x + R.half + th, R.z - R.half, R.z + R.half, rt, rb, 'rope');
  world.box(R.x - R.half, R.x + R.half, R.z - R.half - th, R.z - R.half + th, rt, rb, 'rope');
  world.box(R.x - R.half, R.gapMin, R.z + R.half - th, R.z + R.half + th, rt, rb, 'rope');
  world.box(R.gapMax, R.x + R.half, R.z + R.half - th, R.z + R.half + th, rt, rb, 'rope');
  // stairs in front of the gate
  const sz0 = R.z + R.apron;
  const stepW = R.gapMax - R.gapMin;
  [[0.64, sz0, sz0 + 0.46], [0.32, sz0 + 0.46, sz0 + 0.92]].forEach(([top, z0, z1]) => {
    B.box(ironMat, (R.gapMin + R.gapMax) / 2, top / 2, (z0 + z1) / 2, stepW, top, z1 - z0, { cast: true });
    B.box(rubberMat, (R.gapMin + R.gapMax) / 2, top + 0.005, (z0 + z1) / 2, stepW - 0.04, 0.01, z1 - z0 - 0.04);
    world.box(R.gapMin, R.gapMax, z0, z1, top, 0, 'stairs');
  });
  for (const hx of [R.gapMin - 0.05, R.gapMax + 0.05]) {
    B.cyl(steelMat, hx, 0.75, sz0 + 0.9, 0.025, 0.025, 1.5, 8);
    B.cyl(steelMat, hx, 1.25, sz0 + 0.45, 0.022, 0.022, 1.05, 8, { rotX: 1.15 });
  }

  // ---------------- heavy bags on a steel beam (left side) ----------------
  const beamX = -14, beamY = 4.6;
  B.box(ironMat, beamX, beamY, -3, 0.22, 0.32, 15, { cast: false });
  for (const z of [-10, 4]) { B.box(ironMat, beamX, beamY / 2, z, 0.25, beamY, 0.25, { cast: true }); world.boxC(beamX, z, 0.3, 0.3, beamY); }
  BC.ceil.cyl(ironMat, beamX, (beamY + H) / 2, -3, 0.04, 0.04, H - beamY, 6);
  const bagDefs = [[-8.4, 0x18181b, 1.65], [-5.4, 0x6a1410, 1.5], [-2.4, 0x18181b, 1.7], [0.6, 0x3a2a20, 1.4]];
  for (const [z, col, len] of bagDefs) {
    const bag = new THREE.Group();
    bag.position.set(beamX, beamY, z);
    const bodyMat = new THREE.MeshStandardMaterial({ map: T.leather(col), roughness: 0.5, metalness: 0.0 });
    const chainLen = beamY - 0.5 - len - 0.7;
    const body = new THREE.Mesh(new THREE.CapsuleGeometry(0.3, len - 0.6, 6, 18), bodyMat);
    body.position.y = -chainLen - 0.25 - len / 2; body.castShadow = true; body.receiveShadow = true;
    bag.add(body);
    for (const by of [0.22, -0.22]) { const band = new THREE.Mesh(new THREE.CylinderGeometry(0.305, 0.305, 0.08, 18, 1, true), padBlack); band.position.y = body.position.y + by * len; bag.add(band); }
    const chain = new THREE.Mesh(new THREE.CylinderGeometry(0.018, 0.018, chainLen, 4), steelMat); chain.position.y = -chainLen / 2; bag.add(chain);
    for (let k = 0; k < 4; k++) {
      const a = k * Math.PI / 2 + 0.6;
      const s = new THREE.Mesh(new THREE.CylinderGeometry(0.01, 0.01, 0.42, 3).rotateX(Math.PI / 2), steelMat);
      s.position.set(Math.cos(a) * 0.12, -chainLen - 0.14, Math.sin(a) * 0.12); s.lookAt(new THREE.Vector3(0, -chainLen, 0).add(bag.position)); bag.add(s);
    }
    scene.add(bag);
    const L = chainLen + 0.25 + len / 2;
    const col2 = { x: beamX, z, r: 0.34, top: beamY - chainLen - 0.25, bottom: beamY - chainLen - 0.25 - len, mobile: true, noStand: true };
    const b = { group: bag, L, ax: 0, az: 0, vx: 0, vz: 0, col: col2, z, bottomY: col2.bottom, topY: col2.top, hits: 0 };
    col2.onPush = (dx, dz) => { b.vx += dx * 3; b.vz += dz * 3; };
    world.dynamic.push(col2);
    out.bags.push(b);
  }
  // rubber mats under the bags
  B.box(matRed, -13.2, 0.015, -3.9, 3.2, 0.03, 11.5);

  // ---------------- obstacles on the floor ----------------
  // plyo boxes (jumpable)
  const plyo = [[-7.2, 2.6, 1.1, 0.8, 0.5], [-6.1, 2.6, 0.9, 0.8, 0.8], [-6.6, 3.6, 0.8, 0.7, 0.32]];
  for (const [x, z, w, d, h] of plyo) { B.box(plyMat, x, h / 2, z, w, h, d, { cast: true, uvScale: 1.2 }); world.boxC(x, z, w, d, h); }
  // tyre stacks
  const tyre = new THREE.TorusGeometry(0.46, 0.17, 10, 22); tyre.rotateX(Math.PI / 2);
  for (const [x, z, n] of [[-10, 7.2, 3], [-8.7, 8.4, 2], [14.4, -11.4, 4]]) {
    for (let i = 0; i < n; i++) { const g = tyre.clone(); g.rotateY(i * 0.7); g.translate(x + (i % 2) * 0.04, 0.17 + i * 0.33, z); B.add(rubberMat, g, { cast: true }); }
    world.circle(x, z, 0.62, 0.34 + (n - 1) * 0.33);
  }
  // squat rack + bench + barbell (front right)
  const rx = 12, rz = 6.5;
  for (const [ox, oz] of [[-0.6, -0.6], [0.6, -0.6], [-0.6, 0.6], [0.6, 0.6]]) B.box(ironMat, rx + ox, 1.15, rz + oz, 0.08, 2.3, 0.08, { cast: true });
  for (const oz of [-0.6, 0.6]) B.box(ironMat, rx, 2.28, rz + oz, 1.28, 0.08, 0.08, { cast: true });
  for (const ox of [-0.6, 0.6]) B.box(ironMat, rx + ox, 0.1, rz, 0.08, 0.08, 1.28);
  B.cyl(steelMat, rx, 1.45, rz - 0.6, 0.02, 0.02, 2.2, 8, { rotZ: Math.PI / 2, cast: true });
  for (const sx of [-0.85, -0.95, 0.85, 0.95]) B.cyl(rubberMat, rx + sx, 1.45, rz - 0.6, 0.22, 0.22, 0.05, 20, { rotZ: Math.PI / 2, cast: true });
  world.boxC(rx, rz, 1.4, 1.4, 2.3);
  B.box(padBlack, rx, 0.42, rz + 1.6, 0.32, 0.08, 1.25, { cast: true }); B.box(ironMat, rx, 0.2, rz + 1.6, 0.12, 0.4, 1.0);
  world.boxC(rx, rz + 1.6, 0.4, 1.3, 0.46);
  // stacked plates
  for (let i = 0; i < 5; i++) B.cyl(rubberMat, 10.4, 0.03 + i * 0.06, 8.4, 0.23 - i * 0.012, 0.23 - i * 0.012, 0.055, 20, { cast: true });
  // dumbbell rack along the front wall
  const dr = { x: 2, z: 10.25, w: 4.2 };
  B.box(ironMat, dr.x, 0.45, dr.z, dr.w, 0.05, 0.62, { cast: true }); B.box(ironMat, dr.x, 0.85, dr.z + 0.08, dr.w, 0.05, 0.45, { cast: true });
  for (const ox of [-dr.w / 2 + 0.05, 0, dr.w / 2 - 0.05]) B.box(ironMat, dr.x + ox, 0.45, dr.z, 0.06, 0.9, 0.6, { cast: true });
  for (let i = 0; i < 11; i++) for (const [ty, tz, s] of [[0.56, dr.z - 0.1, 1], [0.96, dr.z + 0.08, 0.8]]) {
    const x = dr.x - dr.w / 2 + 0.25 + i * 0.36, r = (0.07 + i * 0.006) * s;
    B.cyl(steelMat, x, ty, tz, 0.016, 0.016, 0.3, 6, { rotX: Math.PI / 2 });
    for (const e of [-0.13, 0.13]) B.cyl(rubberMat, x, ty, tz + e, r, r, 0.07, 6, { rotX: Math.PI / 2, cast: true });
  }
  world.boxC(dr.x, dr.z, dr.w, 0.7, 1.05);
  // lockers along the right wall
  const lockerT = (() => { const c = document.createElement('canvas'); c.width = 128; c.height = 512; const x = c.getContext('2d'); x.fillStyle = '#7a8a86'; x.fillRect(0, 0, 128, 512); x.fillStyle = '#1b2220'; x.fillRect(0, 0, 4, 512); x.fillRect(124, 0, 4, 512); x.fillRect(0, 254, 128, 4); for (let k = 0; k < 6; k++) { x.fillRect(30, 30 + k * 12, 68, 5); x.fillRect(30, 290 + k * 12, 68, 5); } x.fillStyle = '#c9c4b5'; x.fillRect(100, 200, 8, 40); x.fillRect(100, 460, 8, 40); const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t; })();
  const lockerFace = new THREE.MeshStandardMaterial({ map: lockerT, roughness: 0.45, metalness: 0.55 });
  for (let i = 0; i < 12; i++) {
    const z = -0.5 + i * 0.6;
    const m = new THREE.Mesh(new THREE.BoxGeometry(0.55, 2.0, 0.58), [lockerMat, lockerFace, lockerMat, lockerMat, lockerMat, lockerMat]);
    m.position.set(maxX - 0.85, 1.0, z); m.rotation.y = Math.PI; m.castShadow = true; m.receiveShadow = true;
    scene.add(m);
  }
  world.box(maxX - 1.15, maxX - 0.5, -0.8, -0.5 + 11 * 0.6 + 0.3, 2.0);
  // benches along the left wall front
  for (const z of [6.2, 8.6]) { B.box(beamMat, minX + 0.95, 0.45, z, 0.42, 0.06, 2.0, { cast: true, uvScale: 1 }); for (const e of [-0.8, 0.8]) B.box(ironMat, minX + 0.95, 0.22, z + e, 0.36, 0.44, 0.05); world.boxC(minX + 0.95, z, 0.5, 2.0, 0.48); }
  // crates in the back left corner
  for (const [x, z, y, s] of [[-16.6, -11.8, 0, 1.0], [-15.4, -12.1, 0, 0.9], [-16.4, -11.8, 1.0, 0.8]]) {
    B.box(plyMat, x, y + s / 2, z, s, s, s, { cast: true, uvScale: 0.9 });
    if (y === 0) world.boxC(x, z, s, s, s + (x === -16.6 ? 0.8 : 0));
  }
  // water cooler (front left)
  B.box(new THREE.MeshStandardMaterial({ color: 0xd9d6cf, roughness: 0.5 }), -12.5, 0.55, 10.3, 0.4, 1.1, 0.4, { cast: true });
  B.cyl(new THREE.MeshStandardMaterial({ color: 0x5a86b8, roughness: 0.15, transparent: true, opacity: 0.75 }), -12.5, 1.32, 10.3, 0.15, 0.15, 0.45, 16, { cast: true });
  world.boxC(-12.5, 10.3, 0.45, 0.45, 1.6);
  // front steel door
  {
    const D = DOOR, dw = D.x1 - D.x0;
    const doorMat = new THREE.MeshStandardMaterial({ color: 0x2c3431, roughness: 0.45, metalness: 0.7 });
    const pivot = new THREE.Group(); pivot.position.set(D.x0, 0, maxZ + wallT - 0.04);
    const panel = new THREE.Mesh(new THREE.BoxGeometry(dw - 0.04, D.h - 0.04, 0.07), doorMat);
    panel.position.set(dw / 2, D.h / 2, 0); panel.castShadow = true; panel.receiveShadow = true; pivot.add(panel);
    for (const [y, z] of [[0.9, -0.07], [1.45, 0.07]]) { const bar = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.025, dw * 0.7, 8).rotateZ(Math.PI / 2), steelMat); bar.position.set(dw * 0.55, y, z); pivot.add(bar); }
    for (let k = 0; k < 5; k++) { const rib = new THREE.Mesh(new THREE.BoxGeometry(dw - 0.2, 0.03, 0.09), ironMat); rib.position.set(dw / 2, 0.35 + k * 0.6, 0); pivot.add(rib); }
    cut.pz.add(pivot);
    out.door = { pivot, angle: 0, target: 0 };
    // steel frame + exit sign
    BC.pz.box(ironMat, D.x0 - 0.06, D.h / 2, maxZ + wallT / 2, 0.12, D.h, wallT + 0.04);
    BC.pz.box(ironMat, D.x1 + 0.06, D.h / 2, maxZ + wallT / 2, 0.12, D.h, wallT + 0.04);
    BC.pz.box(ironMat, (D.x0 + D.x1) / 2, D.h + 0.06, maxZ + wallT / 2, dw + 0.24, 0.12, wallT + 0.04);
    const exitC = document.createElement('canvas'); exitC.width = 256; exitC.height = 96; const ex = exitC.getContext('2d');
    ex.fillStyle = '#0f6b3a'; ex.fillRect(0, 0, 256, 96); ex.fillStyle = '#e9fff2'; ex.font = `800 56px ${font}`; ex.textAlign = 'center'; ex.textBaseline = 'middle'; ex.fillText('WYJŚCIE', 128, 50);
    const exitT = new THREE.CanvasTexture(exitC); exitT.colorSpace = THREE.SRGBColorSpace;
    const sign = new THREE.Mesh(new THREE.PlaneGeometry(0.9, 0.34), new THREE.MeshBasicMaterial({ map: exitT, color: new THREE.Color(1, 1, 1).multiplyScalar(1.6) }));
    sign.position.set((D.x0 + D.x1) / 2, D.h + 0.45, maxZ - 0.02); sign.rotation.y = Math.PI; cut.pz.add(sign);
    out.glowSprites.push({ pos: new THREE.Vector3((D.x0 + D.x1) / 2, D.h + 0.45, maxZ - 0.15), color: 0x40ff9a, size: 1.1, group: 'pz' });
    // alley props
    const A = ALLEY, zIn = maxZ + wallT;
    const dumpMat = new THREE.MeshStandardMaterial({ color: 0x1f4a35, roughness: 0.6, metalness: 0.5 });
    B.box(dumpMat, A.x0 + 1.1, 0.65, A.z1 - 1.0, 1.9, 1.3, 1.2, { cast: true }); B.box(ironMat, A.x0 + 1.1, 1.33, A.z1 - 1.0, 2.0, 0.06, 1.3);
    world.boxC(A.x0 + 1.1, A.z1 - 1.0, 1.9, 1.2, 1.36);
    const bagMat = new THREE.MeshStandardMaterial({ color: 0x0d0e10, roughness: 0.35, metalness: 0.1 });
    for (const [x, z, r] of [[A.x0 + 2.4, A.z1 - 0.5, 0.35], [A.x0 + 2.9, A.z1 - 0.8, 0.3], [A.x0 + 2.6, A.z1 - 1.2, 0.28]]) { const g = new THREE.SphereGeometry(r, 10, 8); g.scale(1, 0.8, 1); g.translate(x, r * 0.75, z); B.add(bagMat, g, { cast: true }); }
    world.circle(A.x0 + 2.7, A.z1 - 0.8, 0.6, 0.6);
    // crates and a pallet stack on the right
    for (const [x, z, y, sz] of [[A.x1 - 0.8, zIn + 1.4, 0, 0.9], [A.x1 - 0.8, zIn + 2.4, 0, 0.9], [A.x1 - 0.85, zIn + 1.9, 0.9, 0.8]]) { B.box(plyMat, x, y + sz / 2, z, sz, sz, sz, { cast: true, uvScale: 0.9 }); }
    world.box(A.x1 - 1.3, A.x1, zIn + 0.9, zIn + 2.9, 1.7);
    // fire escape on the west alley wall
    const fx = A.x0 + 0.35;
    for (const y of [3.6, 6.6]) { BC.pz.box(ironMat, fx + 0.45, y, 16, 0.9, 0.06, 4.2); BC.pz.box(ironMat, fx + 0.88, y + 0.5, 16, 0.04, 0.04, 4.2); for (let k = -2; k <= 2; k++) BC.pz.box(ironMat, fx + 0.88, y + 0.25, 16 + k, 0.03, 0.5, 0.03); }
    for (let k = 0; k < 9; k++) BC.pz.box(ironMat, fx + 0.45, 3.6 + 0.33 * (k + 1), 14.8 + k * 0.28, 0.8, 0.04, 0.25);
    const lit = new THREE.MeshBasicMaterial({ color: new THREE.Color(0xffb766).multiplyScalar(0.75) });
    const cold = new THREE.MeshBasicMaterial({ color: new THREE.Color(0x7fa6e0).multiplyScalar(0.45) });
    const dark = new THREE.MeshStandardMaterial({ color: 0x0c1016, roughness: 0.2, metalness: 0.6 });
    let wi = 0;
    for (const y of [5.4, 8.2]) for (const z of [13.5, 18.5]) { const m = [lit, dark, cold, dark][(wi++ * 7) % 4]; BC.pz.box(m, A.x0 + 0.02, y, z, 0.04, 1.4, 1.0, { receive: false }); BC.pz.box(m, A.x1 - 0.02, y, z, 0.04, 1.4, 1.0, { receive: false }); }
    // second lamp on the back wall
    BC.pz.box(ironMat, A.x0 + 0.3, 4.4, 17.5, 0.5, 0.12, 0.5);
    BC.pz.cyl(new THREE.MeshBasicMaterial({ color: new THREE.Color(0xffd7a0).multiplyScalar(4) }), A.x0 + 0.35, 4.32, 17.5, 0.16, 0.16, 0.03, 12);
    out.glowSprites.push({ pos: new THREE.Vector3(A.x0 + 0.5, 4.25, 17.5), color: 0xffcf90, size: 1.6, group: 'pz' });
    const sl2 = new THREE.SpotLight(0xffd2a0, 160, 18, 1.1, 0.75, 1.5); sl2.position.set(A.x0 + 0.4, 4.3, 17.5); sl2.target.position.set(A.x0 + 4.5, 0, 16.5);
    sl2.userData.rank = -1; scene.add(sl2, sl2.target); out.lights.push(sl2);
    // neon on the right wall + wall lamp above the door
    const neonT = T.neonSign(font, 'BAR 24/7', '#3fd2ff');
    const nb = new THREE.Mesh(new THREE.PlaneGeometry(2.6, 0.65), new THREE.MeshBasicMaterial({ map: neonT, transparent: true, blending: THREE.AdditiveBlending, depthWrite: false, color: new THREE.Color(1, 1, 1).multiplyScalar(2.2) }));
    nb.position.set(A.x1 - 0.02, 3.4, zIn + 4.5); nb.rotation.y = -Math.PI / 2; cut.pz.add(nb);
    B.box(ironMat, (D.x0 + D.x1) / 2, D.h + 1.1, zIn + 0.25, 0.5, 0.12, 0.5);
    B.cyl(new THREE.MeshBasicMaterial({ color: new THREE.Color(0xdfe9ff).multiplyScalar(4) }), (D.x0 + D.x1) / 2, D.h + 1.02, zIn + 0.3, 0.16, 0.16, 0.03, 12);
    out.glowSprites.push({ pos: new THREE.Vector3((D.x0 + D.x1) / 2, D.h + 0.95, zIn + 0.35), color: 0xcfe0ff, size: 1.6 });
    const sl = new THREE.SpotLight(0xc8d8ff, 150, 16, 1.15, 0.75, 1.5); sl.position.set((D.x0 + D.x1) / 2, D.h + 1.0, zIn + 0.35); sl.target.position.set((D.x0 + D.x1) / 2, 0, zIn + 3.5);
    sl.userData.rank = -1; scene.add(sl, sl.target); out.lights.push(sl);
    const pl = new THREE.PointLight(0x46c8ff, 12, 8, 2); pl.position.set(A.x1 - 0.8, 3.4, zIn + 4.5); pl.userData.tier = 1; scene.add(pl); out.lights.push(pl);
  }

  // ---------------- wall fan between arches ----------------
  const fan = new THREE.Group(); fan.position.set(0, 5.3, minZ + 0.02);
  const fanR = 0.95;
  const frame = new THREE.Mesh(new THREE.TorusGeometry(fanR, 0.07, 8, 32), ironMat); fan.add(frame);
  const housing = new THREE.Mesh(new THREE.CylinderGeometry(fanR, fanR, 0.3, 32, 1, true), ironMat); housing.rotation.x = Math.PI / 2; housing.position.z = 0.1; fan.add(housing);
  const back = new THREE.Mesh(new THREE.CircleGeometry(fanR, 32), new THREE.MeshBasicMaterial({ color: 0x1a120c })); back.position.z = -0.04; fan.add(back);
  const blades = new THREE.Group(); blades.position.z = 0.1;
  for (let k = 0; k < 5; k++) { const bl = new THREE.Mesh(new THREE.BoxGeometry(0.22, fanR * 0.86, 0.02), steelMat); bl.position.y = fanR * 0.45; bl.rotation.y = 0.35; const p = new THREE.Group(); p.rotation.z = k * Math.PI * 2 / 5; p.add(bl); blades.add(p); }
  blades.add(new THREE.Mesh(new THREE.CylinderGeometry(0.12, 0.12, 0.12, 12).rotateX(Math.PI / 2), ironMat));
  fan.add(blades);
  for (const rr of [0.35, 0.65]) { const ring = new THREE.Mesh(new THREE.TorusGeometry(rr, 0.012, 4, 32), steelMat); ring.position.z = 0.24; fan.add(ring); }
  for (let k = 0; k < 8; k++) { const sp = new THREE.Mesh(new THREE.BoxGeometry(0.015, fanR * 2, 0.015), steelMat); sp.rotation.z = k * Math.PI / 8; sp.position.z = 0.25; fan.add(sp); }
  cut.nz.add(fan);
  out.animated.push((dt) => { blades.rotation.z -= dt * 7; });

  // ---------------- neon ----------------
  const neon = new THREE.Mesh(new THREE.PlaneGeometry(6.2, 1.55), new THREE.MeshBasicMaterial({ map: T.neonSign(font, 'BRAXHORN GYM', '#8bff3a'), transparent: true, blending: THREE.AdditiveBlending, depthWrite: false, color: new THREE.Color(1, 1, 1).multiplyScalar(2.4) }));
  neon.position.set(minX + 0.03, 6.0, -3.6); neon.rotation.y = Math.PI / 2; cut.nx.add(neon);
  out.neon = neon;
  const purple = new THREE.MeshBasicMaterial({ color: new THREE.Color(0x9b5cff).multiplyScalar(4) });
  BC.nx.cyl(purple, minX + 0.55, 3.6, 10.2, 0.045, 0.045, 7.0, 8);
  BC.nx.cyl(purple, minX + 0.55, 3.6, 9.35, 0.045, 0.045, 7.0, 8);
  const pp = new THREE.PointLight(0x8e4dff, 4, 5, 2); pp.position.set(minX + 1.2, 3.2, 9.6); pp.userData.tier = 1; scene.add(pp); out.lights.push(pp);
  { const np = new THREE.PointLight(0x8bff3a, 9, 7, 2); np.position.set(minX + 1.0, 5.6, -3.6); np.userData.tier = 2; np.visible = quality.pointLights >= 2; scene.add(np); out.lights.push(np); }

  // brand posters
  if (logos) {
    const pst = new THREE.Mesh(new THREE.PlaneGeometry(2.2, 2.2 * logos.posterCrop.image.height / logos.posterCrop.image.width), new THREE.MeshBasicMaterial({ map: logos.posterCrop }));
    pst.position.set(maxX - 0.05, 3.6, -6); pst.rotation.y = -Math.PI / 2; cut.px.add(pst);
    const bdg = new THREE.Mesh(new THREE.PlaneGeometry(2.0, 2.0), new THREE.MeshBasicMaterial({ map: logos.badge }));
    bdg.position.set(maxX - 0.05, 3.7, 9.6); bdg.rotation.y = -Math.PI / 2; cut.px.add(bdg);
    const mk = new THREE.Mesh(new THREE.PlaneGeometry(2.4, 2.4), new THREE.MeshBasicMaterial({ map: logos.markSpray, transparent: true, depthWrite: false, opacity: 0.9 }));
    mk.position.set(minX + 0.05, 3.2, 3.5); mk.rotation.y = Math.PI / 2; cut.nx.add(mk);
    const pst2 = new THREE.Mesh(new THREE.PlaneGeometry(1.6, 1.6 * logos.posterCrop.image.height / logos.posterCrop.image.width), new THREE.MeshBasicMaterial({ map: logos.posterCrop }));
    pst2.position.set(9, 3.4, maxZ - 0.02); pst2.rotation.y = Math.PI; cut.pz.add(pst2);
  }

  // medicine ball spawn points
  out.spawnBallsAt.push([-2.5, 2.2], [-1.8, 3.0], [3.2, 2.8], [5.5, 0.8]);

  B.build(scene);
  for (const k in BC) BC[k].build(cut[k]);

  // glow sprites (soft halos around bulbs)
  const halo = T.radial('rgba(255,255,255,1)', 'rgba(255,255,255,0)');
  for (const g of out.glowSprites) {
    const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: halo, color: new THREE.Color(g.color).multiplyScalar(0.32), blending: THREE.AdditiveBlending, depthWrite: false, transparent: true }));
    s.position.copy(g.pos); s.scale.setScalar(g.size); s.renderOrder = 6; (g.group ? cut[g.group] : scene).add(s);
  }
  return out;
}
