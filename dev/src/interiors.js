// Interiors of plansza 1 that the player can walk into: the BRAXHORN NUTRITION shop (healing) on the near side
// next to the gym alley, and the club "NOVA" (dyskoteka) on the far side with its vestibule, dance hall, bar,
// back room, stairs and the cellar (fight 2). Static geometry is baked (bake.js): coloured light pools in the club,
// white light in the shop, one warm bulb in the cellar. Nothing glows; no live lights are added.
import * as THREE from 'three';
import * as CT from './citytex.js';
import { ST, facadeTextures } from './street.js';

const SW = 0.15;
export const SHOP = { x0: -26, x1: -14.5, z0: ST.near - 9, z1: ST.near, h: 13, ceil: 3.8 };
export const CLUB = { x0: -52, x1: -32, z0: ST.far + 0.6, z1: 62, h: 10.5, ceil: 4.8, cellarY: -3.6, cellarCeil: -0.35 };

export const INTERIOR_POINTS = {
  shop_inside: { x: -20.7, y: SW, z: 19.4, face: Math.PI, note: 'za drzwiami sklepu' },
  shop_counter: { x: -22.1, y: SW, z: 15.0, face: Math.PI, stage: 'm1.heal', note: 'miejsce zakupu: lada (cena, efekt leczenia, saldo)' },
  shop_clerk: { x: -22.1, y: SW, z: 12.85, face: 0, stage: 'm1.heal', note: 'sprzedawca za ladą' },
  shop_fridge: { x: -16.3, y: SW, z: 13.6, face: Math.PI, note: 'lodówka z napojami' },
  club_door: { x: -42, y: SW, z: 35.0, face: 0, stage: 'm1.club', note: 'wejście do dyskoteki z ulicy' },
  club_bouncer: { x: -39.3, y: SW, z: 38.2, face: Math.PI, stage: 'm1.club', note: 'miejsce przyszłej ochrony w przedsionku' },
  club_vestibule: { x: -42, y: SW, z: 39.0, face: 0, note: 'przedsionek' },
  club_hall_entry: { x: -42, y: SW, z: 43.0, face: 0, note: 'wejście na salę' },
  club_dance: { x: -44, y: SW, z: 48.5, face: 0, note: 'środek parkietu' },
  club_bar: { x: -36.7, y: SW, z: 48.0, face: Math.PI / 2, note: 'przy barze (gracz)' },
  club_barman: { x: -33.4, y: SW, z: 48.0, face: -Math.PI / 2, stage: 'm1.club', note: 'barman' },
  club_contact: { x: -50.0, y: SW, z: 46.4, face: Math.PI / 2, stage: 'm1.club', note: 'osoba związana ze zleceniem (loża)' },
  club_dj: { x: -49.2, y: SW + 0.4, z: 53.6, face: 0, note: 'DJ' },
  club_back_door: { x: -35.0, y: SW, z: 55.9, face: 0, stage: 'm1.club', note: 'przejście na zaplecze' },
  club_stairs_top: { x: -37.5, y: SW, z: 55.9, face: 0, stage: 'm1.club', note: 'schody do piwnicy' },
  cellar_entry: { x: -40.2, y: -3.6, z: 60.8, face: -Math.PI / 2, stage: 'm1.fight2', note: 'wejście do piwnicy' },
  fight2_center: { x: -45.5, y: -3.6, z: 52.0, face: 0, stage: 'm1.fight2', note: 'punkt starcia nr 2: piwnica pod klubem' },
  fight2_enemy_wait: { x: -49.4, y: -3.6, z: 47.2, face: Math.PI / 2, stage: 'm1.fight2', note: 'przeciwnik przy stole z torbą' },
  fight2_bag: { x: -50.4, y: -2.8, z: 47.0, stage: 'm1.fight2', note: 'torba z pieniędzmi siłowni (na stole)' },
  club_yard_exit: { x: -34.4, y: SW, z: 63.4, face: 0, stage: 'm1.club', note: 'wyjście z zaplecza na podwórko' },
  club_people: { spots: [[-46.5, 46.5], [-45, 50.2], [-42.2, 47], [-41, 50.5], [-44, 45.6], [-39.6, 48.6], [-37.8, 45.4], [-37.8, 51.2], [-50.4, 44], [-50.4, 48.6]], y: SW, note: 'miejsca dla przyszłych bawiących się ludzi' },
};

export const INTERIOR_ZONES = [
  { id: 'shop', minX: SHOP.x0 + 0.3, maxX: SHOP.x1 - 0.3, minZ: SHOP.z0 + 0.3, maxZ: SHOP.z1 - 0.3, y0: -1, y1: 3.5, ceil: SHOP.ceil, mood: 'shop' },
  { id: 'club_vest', minX: -46, maxX: -38, minZ: CLUB.z0 + 0.3, maxZ: 41.5, y0: -1, y1: 3, ceil: 3.4, mood: 'club', hideOut: true },
  { id: 'club_hall', minX: CLUB.x0 + 0.3, maxX: CLUB.x1 - 0.3, minZ: 41.5, maxZ: 55, y0: -1, y1: 4, ceil: CLUB.ceil, mood: 'club', hideOut: true },
  { id: 'club_back', minX: -38.6, maxX: CLUB.x1 - 0.3, minZ: 55, maxZ: CLUB.z1 - 0.3, y0: -1.2, y1: 3.2, ceil: 3.4, mood: 'back', hideOut: true },
  { id: 'cellar', minX: CLUB.x0 + 0.3, maxX: -36.4, minZ: 44, maxZ: CLUB.z1 - 0.3, y0: -6, y1: -1.2, ceil: CLUB.cellarCeil, mood: 'cellar', hideOut: true },
];

export async function buildInteriors({ scene, world, font, logos, street, mats, Batch, M, kit }) {
  const G = new THREE.Group(); G.name = 'interiors'; scene.add(G);
  const B = new Batch(G);
  const out = { group: G, bakeLights: [], lampSources: [] };
  const BL = (x, y, z, col, k, r, lobe = 'down', size = 0.15) => out.bakeLights.push({ p: new THREE.Vector3(x, y, z), col: new THREE.Color(col).multiplyScalar(k), r, lobe, size });
  const LS = (x, y, z, col, k, r) => street.lampSources.push({ pos: new THREE.Vector3(x, y, z), col: new THREE.Color(col).multiplyScalar(k), r, kind: 'room' });
  const box = (...a) => world.box(...a);
  const IN = { bakeIndoor: true };

  const m = {
    tileF: M({ map: CT.floorTiles() }), wallW: M({ map: CT.plaster([214, 210, 200]) }), ceilW: M({ color: 0xd9d7d0 }),
    black: M({ color: 0x141516, roughness: 0.6 }), panel: M({ color: 0x1d1f21, roughness: 0.7 }), green: M({ color: 0x6fcf3a, roughness: 0.7 }),
    counterTop: M({ color: 0xb9bab4, roughness: 0.5 }), steel: M({ color: 0x8a8e92, roughness: 0.5 }), white: M({ color: 0xe6e6e0, roughness: 0.6 }),
    tubs: M({ map: CT.shelfGoods(font, 'tubs'), roughness: 0.6 }), boxes: M({ map: CT.shelfGoods(font, 'boxes'), roughness: 0.7 }), bottles: M({ map: CT.shelfGoods(font, 'bottles'), roughness: 0.5 }),
    glass: new THREE.MeshStandardMaterial({ color: 0x9fb2bf, transparent: true, opacity: 0.18, roughness: 0.15, depthWrite: false }),
    frame: M({ color: 0x232527, roughness: 0.55 }), mat: M({ color: 0x1b1c1d }),
    clubWall: M({ map: CT.plaster([44, 42, 48]), color: new THREE.Color(2.2, 2.2, 2.2) }), clubCeil: M({ color: 0x1a1a1e }), clubFloor: M({ map: CT.epoxy([46, 46, 50]), color: new THREE.Color(2.0, 2.0, 2.0) }), dance: M({ map: CT.danceFloor(), color: new THREE.Color(2.4, 2.4, 2.4) }),
    acoustic: M({ map: CT.corrugated([40, 38, 46]), color: new THREE.Color(2, 2, 2) }), velvet: M({ color: 0x4a1830, roughness: 0.95 }), wood: mats.wood, barTop: M({ color: 0x2b2420, roughness: 0.45 }),
    brick: mats.brickDark, concrete: M({ map: CT.epoxy([86, 84, 80]) }), pipe: M({ color: 0x5a5e62, roughness: 0.6 }), crate: M({ map: CT.planks(), color: 0xa08a68 }),
    keg: M({ color: 0x9a9fa4, roughness: 0.45 }), sofa: M({ color: 0x3a2d26, roughness: 0.95 }), mirror: M({ map: mirrorBallTex(), roughness: 0.6 }),
  };

  // ======================= BRAXHORN NUTRITION (shop) =======================
  {
    const S = SHOP, t = 0.3, zf = S.z1, zb = S.z0;
    // shell: side + back walls full height, roof, the upper facade
    B.box(mats.brick, S.x0, S.x0 + t, 0, S.h, zb, zf - 0.02, { uv: 3 }); B.box(mats.brick, S.x1 - t, S.x1, 0, S.h, zb, zf - 0.02, { uv: 3 });
    B.box(mats.brick, S.x0, S.x1, 0, S.h, zb, zb + t, { uv: 3 });
    B.box(mats.roof, S.x0, S.x1, S.h, S.h + 0.5, zb, zf, { uv: null });
    B.box(mats.brick, S.x0 + t, S.x1 - t, S.ceil, S.h, zb + t, zf - 0.3, { uv: 3 });                           // upper floors (solid)
    const ft = facadeTextures(S.x1 - S.x0, S.h, 'dark', null);
    { const g = new THREE.PlaneGeometry(S.x1 - S.x0, S.h - 4.4); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setY(i, 4.4 / S.h + uv.getY(i) * (S.h - 4.4) / S.h); g.translate((S.x0 + S.x1) / 2, 4.4 + (S.h - 4.4) / 2, zf); B.geo(M({ map: ft.map, roughness: 0.82 }), g); }
    B.box(mats.coping, S.x0 - 0.05, S.x1 + 0.05, S.h - 0.35, S.h + 0.15, zf, zf + 0.45, { uv: null });
    // ground-floor storefront (black frame, BRAXHORN fascia), glass windows, open door
    const zfr = zf - 0.3;
    const front = [[S.x0, S.x0 + 0.5], [-22.1, -21.6], [-19.8, -19.3], [S.x1 - 0.5, S.x1]];
    for (const [a, b] of front) B.box(m.black, a, b, 0, S.ceil + 0.6, zfr, zf, { uv: null });
    B.box(m.black, S.x0, S.x1, 3.2, 4.4, zfr, zf, { uv: null });                                              // fascia
    B.box(m.black, S.x0 + 0.5, -22.1, 0, 0.6, zfr, zf, { uv: null }); B.box(m.black, -19.3, S.x1 - 0.5, 0, 0.6, zfr, zf, { uv: null });  // plinths
    B.box(m.green, S.x0, S.x1, 4.36, 4.44, zf - 0.02, zf + 0.02, { uv: null });
    for (const [a, b] of [[S.x0 + 0.5, -22.1], [-19.3, S.x1 - 0.5]]) { const g = new THREE.PlaneGeometry(b - a, 2.6); g.translate((a + b) / 2, 0.6 + 1.3, zf - 0.15); const gm = new THREE.Mesh(g, m.glass); gm.userData.noBake = true; gm.userData.noOcclude = true; G.add(gm); }
    B.box(m.frame, -21.6, -19.8, 2.6, 3.2, zfr, zf, { uv: null });
    // door leaf open inward (glass in a black frame)
    { const dx = -21.55, dz = zfr; B.box(m.frame, dx, dx + 0.05, SW, 2.6, dz - 1.7, dz, { uv: null }); const g = new THREE.PlaneGeometry(1.6, 2.3); g.rotateY(Math.PI / 2); g.translate(dx + 0.03, SW + 1.25, dz - 0.85); const gm = new THREE.Mesh(g, m.glass); gm.userData.noBake = true; gm.userData.noOcclude = true; G.add(gm); }
    // sign: the brand logo on the fascia (printed, matte)
    { const lw = 7.2, lh = lw * logos.wide.image.height / logos.wide.image.width; const sg = new THREE.Mesh(new THREE.PlaneGeometry(lw, Math.min(1.05, lh)), M({ map: logos.wideA || logos.wide, transparent: true, roughness: 0.7 })); sg.position.set((S.x0 + S.x1) / 2, 3.8, zf + 0.02); G.add(sg); }
    // collisions: shell + storefront (door gap x -21.6..-19.8)
    box(S.x0, S.x0 + t, zb, zf, S.h, -1, 'bldg'); box(S.x1 - t, S.x1, zb, zf, S.h, -1, 'bldg'); box(S.x0, S.x1, zb, zb + t, S.h, -1, 'bldg');
    box(S.x0, -21.6, zfr, zf, S.h, -1, 'bldg'); box(-19.8, S.x1, zfr, zf, S.h, -1, 'bldg'); box(-21.6, -19.8, zfr, zf, S.h, 2.6, 'lintel');
    // interior: floor, walls, ceiling, light panels
    const ix0 = S.x0 + t, ix1 = S.x1 - t, iz0 = zb + t, iz1 = zfr;
    B.box(m.tileF, ix0, ix1, -0.2, SW, iz0, iz1 + 0.3, { uv: 1.2, ud: { ground: true, ...IN } });
    box(ix0, ix1, iz0, iz1 + 0.3, SW, -1, 'walk');
    B.box(m.wallW, ix0, ix0 + 0.02, SW, S.ceil, iz0, iz1, { uv: 3, ud: IN }); B.box(m.wallW, ix1 - 0.02, ix1, SW, S.ceil, iz0, iz1, { uv: 3, ud: IN }); B.box(m.wallW, ix0, ix1, SW, S.ceil, iz0, iz0 + 0.02, { uv: 3, ud: IN });
    B.box(m.panel, ix0, ix1, SW, 1.0, iz0 + 0.02, iz0 + 0.05, { uv: null, ud: IN });
    B.box(m.ceilW, ix0, ix1, S.ceil - 0.02, S.ceil, iz0, iz1, { uv: 2, ud: IN });
    for (const [x, z] of [[-23, 14.5], [-18, 14.5], [-23, 18.2], [-18, 18.2]]) { B.box(m.white, x - 0.6, x + 0.6, S.ceil - 0.08, S.ceil - 0.02, z - 0.3, z + 0.3, { uv: null, ud: IN }); BL(x, S.ceil - 0.15, z, 0xfff6ea, 3.2, 8, 'down', 0.3); }
    LS(-20.5, 3.2, 16.2, 0xfff4e6, 0.9, 7);
    // counter (lada) with the till; back shelving with tubs; wall shelves; centre gondola; fridge; door mat
    B.box(m.black, -24.6, -19.6, SW, SW + 1.0, 13.4, 14.3, { uv: null, ud: IN }); B.box(m.green, -24.6, -19.6, SW + 0.62, SW + 0.68, 14.3, 14.32, { uv: null, ud: IN });
    B.box(m.counterTop, -24.7, -19.5, SW + 1.0, SW + 1.06, 13.3, 14.4, { uv: null, ud: IN });
    B.box(m.panel, -21.0, -20.4, SW + 1.06, SW + 1.32, 13.55, 14.0, { uv: null, ud: IN }); B.box(m.panel, -23.6, -23.3, SW + 1.06, SW + 1.18, 13.9, 14.15, { uv: null, ud: IN });
    box(-24.7, -19.5, 13.3, 14.4, 1.1, -1, 'counter');
    const shelf = (x0, x1, z0, z1, h, goods, faceAxis) => {
      B.box(m.panel, x0, x1, SW, SW + h, z0, z1, { uv: null, ud: IN });
      const levels = Math.floor(h / 0.48);
      for (let k = 0; k < levels; k++) {
        const y = SW + 0.2 + k * 0.48;
        if (faceAxis === 'z+') { const g = new THREE.PlaneGeometry(x1 - x0 - 0.06, 0.4); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * (x1 - x0) / 2); g.translate((x0 + x1) / 2, y + 0.2, z1 + 0.005); B.geo(goods, g, IN); }
        else if (faceAxis === 'x+') { const g = new THREE.PlaneGeometry(z1 - z0 - 0.06, 0.4); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * (z1 - z0) / 2); g.rotateY(Math.PI / 2); g.translate(x1 + 0.005, y + 0.2, (z0 + z1) / 2); B.geo(goods, g, IN); }
        else if (faceAxis === 'x-') { const g = new THREE.PlaneGeometry(z1 - z0 - 0.06, 0.4); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * (z1 - z0) / 2); g.rotateY(-Math.PI / 2); g.translate(x0 - 0.005, y + 0.2, (z0 + z1) / 2); B.geo(goods, g, IN); }
        B.box(m.steel, x0, x1, y - 0.03, y, faceAxis === 'z+' ? z1 : z0, faceAxis === 'z+' ? z1 + 0.04 : z1, { uv: null, ud: IN });
      }
      box(x0, x1, z0, z1, SW + h, -1, 'shelf');
    };
    shelf(-25.6, -18.0, iz0 + 0.02, iz0 + 0.42, 2.3, m.tubs, 'z+');
    shelf(ix0 + 0.02, ix0 + 0.46, 15.0, 19.6, 2.2, m.tubs, 'x+');
    shelf(ix1 - 0.46, ix1 - 0.02, 15.2, 19.4, 2.2, m.boxes, 'x-');
    shelf(-22.4, -18.8, 16.6, 17.0, 1.4, m.tubs, 'z+'); shelf(-22.4, -18.8, 16.2, 16.6, 1.4, m.boxes, 'z+');
    { const g = new THREE.PlaneGeometry(3.6, 0.4); g.rotateY(Math.PI); g.translate(-20.6, SW + 0.42, 16.195); B.geo(m.boxes, g, IN); const g2 = g.clone(); g2.translate(0, 0.48, 0); B.geo(m.tubs, g2, IN); }
    // fridge: two glass doors showing bottles (matte; no inner light)
    { const fx0 = -17.7, fx1 = ix1 - 0.05, fz0 = iz0 + 0.02, fz1 = iz0 + 0.8;
      B.box(m.white, fx0, fx1, SW, SW + 2.15, fz0, fz1, { uv: null, ud: IN }); B.box(m.black, fx0, fx1, SW + 1.95, SW + 2.15, fz1, fz1 + 0.02, { uv: null, ud: IN });
      for (let k = 0; k < 4; k++) { const g = new THREE.PlaneGeometry(fx1 - fx0 - 0.2, 0.42); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * 1.3); g.translate((fx0 + fx1) / 2, SW + 0.3 + k * 0.42, fz1 + 0.012); B.geo(m.bottles, g, IN); }
      B.box(m.frame, (fx0 + fx1) / 2 - 0.03, (fx0 + fx1) / 2 + 0.03, SW, SW + 1.95, fz1, fz1 + 0.03, { uv: null, ud: IN });
      box(fx0, fx1, fz0, fz1, SW + 2.15, -1, 'fridge'); }
    B.box(m.mat, -22.0, -19.4, SW, SW + 0.015, 18.9, 20.1, { uv: null, ud: IN });
    // brand poster on the back wall above the shelves + small badge by the door
    { const pw = 2.4, ph = pw * logos.posterCrop.image.height / logos.posterCrop.image.width; const pm = new THREE.Mesh(new THREE.PlaneGeometry(pw, Math.min(ph, 1.1)), M({ map: logos.posterCrop })); pm.position.set(-21.8, SW + 3.0, iz0 + 0.03); G.add(pm); }
    { const bm = new THREE.Mesh(new THREE.PlaneGeometry(0.9, 0.9), M({ map: logos.badge })); bm.position.set(ix1 - 0.03, SW + 2.6, 13.6); bm.rotation.y = -Math.PI / 2; G.add(bm); }
  }

  // ======================= CLUB NOVA (dyskoteka) =======================
  {
    const C = CLUB, t = 0.3, zf = C.z0, zb = C.z1, CY = C.cellarY;
    const IX0 = C.x0 + t, IX1 = C.x1 - t, IZ0 = zf + t, IZ1 = zb - t;
    // ---------- exterior ----------
    B.box(mats.brick, C.x0, C.x0 + t, 0, C.h, zf, zb, { uv: 3 }); B.box(mats.brick, C.x1 - t, C.x1, 0, C.h, zf, zb, { uv: 3 });
    // back wall with the door to the yard (x -35.2..-33.6)
    B.box(mats.brick, C.x0, -35.2, 0, C.h, IZ1, zb, { uv: 3 }); B.box(mats.brick, -33.6, C.x1, 0, C.h, IZ1, zb, { uv: 3 }); B.box(mats.brick, -35.2, -33.6, 2.45, C.h, IZ1, zb, { uv: 3 });
    box(C.x0, -35.2, IZ1, zb, C.h, -1, 'bldg'); box(-33.6, C.x1, IZ1, zb, C.h, -1, 'bldg'); box(-35.2, -33.6, IZ1, zb, C.h, 2.45, 'lintel');
    box(C.x0, C.x0 + t, zf, zb, C.h, -10, 'bldg'); box(C.x1 - t, C.x1, zf, zb, C.h, -10, 'bldg');
    B.box(m.frame, -35.25, -33.55, 2.45, 2.55, zb, zb + 0.06, { uv: null });
    { const leaf = new THREE.BoxGeometry(1.55, 2.3, 0.06); leaf.translate(-0.78, 1.15 + SW, 0); leaf.rotateY(-1.9); leaf.translate(-33.6, 0, zb + 0.04); B.geo(m.frame, leaf); }
    B.box(mats.roof, C.x0, C.x1, C.h, C.h + 0.5, zf, zb, { uv: null });
    B.box(mats.brick, IX0, IX1, C.ceil, C.h, IZ0, IZ1, { uv: 3 });                       // upper floor (solid, not accessible)
    // street facade: graphite ground floor, upper facade from the generator, entrance with canopy, painted sign, posters
    const ft = facadeTextures(C.x1 - C.x0, C.h, 'dark', null);
    { const g = new THREE.PlaneGeometry(C.x1 - C.x0, C.h - 5.0); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setY(i, 5.0 / C.h + uv.getY(i) * (C.h - 5.0) / C.h); g.rotateY(Math.PI); g.translate((C.x0 + C.x1) / 2, 5.0 + (C.h - 5.0) / 2, zf); B.geo(M({ map: ft.map, roughness: 0.82 }), g); }
    const DX0 = -43.4, DX1 = -40.6;
    B.box(m.clubWall, C.x0, DX0, 0, 5.0, zf, zf + t, { uv: 2 }); B.box(m.clubWall, DX1, C.x1, 0, 5.0, zf, zf + t, { uv: 2 }); B.box(m.clubWall, DX0, DX1, 3.1, 5.0, zf, zf + t, { uv: 2 });
    box(C.x0, DX0, zf, zf + t, C.h, -1, 'bldg'); box(DX1, C.x1, zf, zf + t, C.h, -1, 'bldg'); box(DX0, DX1, zf, zf + t, C.h, 3.1, 'lintel');
    for (let x = C.x0 + 1; x < C.x1 - 0.5; x += 2.0) if (x < DX0 - 0.5 || x > DX1 + 0.5) B.box(m.black, x - 0.04, x + 0.04, 0.6, 4.6, zf - 0.04, zf, { uv: null });
    B.box(m.black, C.x0, C.x1, 4.85, 5.05, zf - 0.25, zf, { uv: null });
    B.box(m.black, DX0 - 0.12, DX1 + 0.12, 3.05, 3.2, zf - 0.08, zf + t, { uv: null }); B.box(m.black, DX0 - 0.12, DX0, 0, 3.1, zf - 0.08, zf + t, { uv: null }); B.box(m.black, DX1, DX1 + 0.12, 0, 3.1, zf - 0.08, zf + t, { uv: null });
    // doors open inward
    for (const [hx, sg] of [[DX0, 1], [DX1, -1]]) { const g = new THREE.BoxGeometry(1.35, 2.85, 0.06); g.translate(sg * 0.68, 1.43 + SW, 0); g.rotateY(sg * -1.55); g.translate(hx, 0, zf + t + 0.05); B.geo(m.frame, g); }
    // canopy
    B.box(m.black, -45.2, -38.8, 3.45, 3.62, zf - 1.7, zf, { uv: null });
    for (const x of [-45, -39]) { const g = new THREE.CylinderGeometry(0.02, 0.02, 1.9, 6); g.rotateX(-0.95); g.translate(x, 4.2, zf - 0.8); B.geo(m.steel, g); }
    const signT = CT.signBoard(font, 'NOVA', { fg: '#efe6d6', bg: '#2a1530', border: '#c9a24e', size: 190, w: 1024, h: 300, sub: 'D Y S K O T E K A' });
    const sgn = new THREE.Mesh(new THREE.BoxGeometry(6.4, 1.9, 0.18), [m.black, m.black, m.black, m.black, m.black, M({ map: signT, roughness: 0.65 })]); sgn.position.set(-42, 6.6, zf - 0.12); G.add(sgn);
    { const cs = new THREE.Mesh(new THREE.PlaneGeometry(4.6, 0.55), M({ map: CT.signBoard(font, 'CLUB NOVA', { fg: '#e9dfc9', bg: '#121213', size: 150, w: 1024, h: 128 }) })); cs.position.set(-42, 3.53, zf - 1.71); cs.rotation.y = Math.PI; G.add(cs); }
    const posters = [['PIĄTEK', 'NOC HOUSE', 300], ['SOBOTA', 'RETRO 90', 210], ['LIVE', 'DJ RÓG', 20], ['NIEDZ.', 'KARAOKE', 160]];
    posters.forEach(([a, b, h], i) => { const pm = new THREE.Mesh(new THREE.PlaneGeometry(0.9, 1.27), M({ map: CT.poster(font, a, b, h) })); pm.position.set([-47.8, -46.6, -37.4, -36.2][i], 1.9, zf - 0.02); pm.rotation.y = Math.PI; G.add(pm); });
    // queue: stanchions with a rope along the sidewalk
    for (let k = 0; k < 5; k++) { const x = -49.6 + k * 1.3; B.cyl(m.steel, x, SW + 0.5, zf - 1.2, 0.03, 0.03, 1.0, 8); B.cyl(m.steel, x, SW + 0.02, zf - 1.2, 0.16, 0.16, 0.04, 12); world.circle(x, zf - 1.2, 0.12, 1.1); if (k < 4) { const g = new THREE.CylinderGeometry(0.02, 0.02, 1.3, 6); g.rotateZ(Math.PI / 2); g.translate(x + 0.65, SW + 0.86, zf - 1.2); B.geo(m.velvet, g); } }

    // ---------- inside: floors ----------
    // vestibule + hall + back room floor at SW (slab: bottom -0.3 so the cellar below is free)
    B.box(m.clubFloor, IX0, IX1, -0.3, SW, IZ0, 55.3, { uv: 2, ud: { ground: true, ...IN } });
    B.box(m.clubFloor, -36.6, IX1, -0.3, SW, 55.3, IZ1, { uv: 2, ud: { ground: true, ...IN } });
    B.box(m.clubFloor, -38.6, -36.6, -0.3, SW, 55.3, 55.6, { uv: 2, ud: { ground: true, ...IN } });
    B.box(mats.slab, -35.2, -33.6, -0.2, SW, IZ1, zb + 0.1, { uv: 2, ud: { ground: true } }); world.box(-35.2, -33.6, IZ1 - 0.1, zb + 0.1, SW, -0.3, 'walk');
    world.box(-46, -38, IZ0 - 0.31, 41.8, SW, -0.3, 'walk');
    world.box(IX0, IX1, 41.5, 55.3, SW, -0.3, 'walk');
    world.box(-36.6, IX1, 55.3, IZ1, SW, -0.3, 'walk');                    // back room (the stairwell x -38.4..-36.6 is open)
    world.box(-38.4, -36.6, 55.3, 55.6, SW, -0.3, 'walk');
    // ---------- vestibule (przedsionek) ----------
    B.box(m.clubWall, -46.3, -46, SW, C.ceil, IZ0, 41.5, { uv: 2, ud: IN }); B.box(m.clubWall, -38, -37.7, SW, C.ceil, IZ0, 41.5, { uv: 2, ud: IN });
    B.box(mats.brick, IX0, -46.3, 0, C.ceil, IZ0, 41.5, { uv: 3, ud: IN }); B.box(mats.brick, -37.7, IX1, 0, C.ceil, IZ0, 41.5, { uv: 3, ud: IN });   // cloakroom / office: closed
    box(IX0, -46, IZ0, 41.5, C.h, -1, 'room'); box(-38, IX1, IZ0, 41.5, C.h, -1, 'room');
    B.box(m.clubCeil, -46, -38, 3.38, 3.4, IZ0, 41.5, { uv: null, ud: IN }); B.box(mats.brick, -46, -38, 3.4, C.ceil, IZ0, 41.5, { uv: 3, ud: IN });
    B.box(m.barTop, -45.95, -45.2, SW, SW + 1.05, 37.2, 40.6, { uv: null, ud: IN }); box(-46, -45.2, 37.2, 40.6, 1.1, -1, 'counter');      // cloakroom hatch counter
    B.box(m.black, -39.2, -38.6, SW, SW + 1.15, 37.6, 38.4, { uv: null, ud: IN }); box(-39.2, -38.6, 37.6, 38.4, 1.2, -1, 'podium');       // bouncer's podium
    BL(-42, 3.2, 38.8, 0xffd6a0, 2.4, 7, 'down', 0.1);
    // wall between vestibule and hall, opening x -43..-41
    B.box(m.clubWall, IX0, -43, SW, C.ceil, 41.5, 41.8, { uv: 2, ud: IN }); B.box(m.clubWall, -41, IX1, SW, C.ceil, 41.5, 41.8, { uv: 2, ud: IN }); B.box(m.clubWall, -43, -41, 3.0, C.ceil, 41.5, 41.8, { uv: 2, ud: IN });
    box(IX0, -43, 41.5, 41.8, C.h, -1, 'wall'); box(-41, IX1, 41.5, 41.8, C.h, -1, 'wall'); box(-43, -41, 41.5, 41.8, C.h, 3.0, 'lintel');
    // ---------- dance hall ----------
    const HZ0 = 41.8, HZ1 = 55;
    B.box(m.clubWall, IX0, IX0 + 0.02, SW, C.ceil, HZ0, HZ1, { uv: 2, ud: IN }); B.box(m.clubWall, IX1 - 0.02, IX1, SW, C.ceil, HZ0, HZ1, { uv: 2, ud: IN });
    B.box(m.acoustic, IX0 + 0.02, IX0 + 0.08, 1.2, 3.8, 43, 50, { uv: 1, uvv: 2.6, ud: IN });
    B.box(m.clubCeil, IX0, IX1, C.ceil - 0.02, C.ceil, HZ0, HZ1, { uv: null, ud: IN });
    // wall to the back room with the door x -36..-34, and a solid store room west of it
    B.box(m.clubWall, IX0, -36, SW, C.ceil, HZ1, HZ1 + 0.3, { uv: 2, ud: IN }); B.box(m.clubWall, -34, IX1, SW, C.ceil, HZ1, HZ1 + 0.3, { uv: 2, ud: IN }); B.box(m.clubWall, -36, -34, 2.5, C.ceil, HZ1, HZ1 + 0.3, { uv: 2, ud: IN });
    box(IX0, -36, HZ1, HZ1 + 0.3, C.h, -0.3, 'wall'); box(-34, IX1, HZ1, HZ1 + 0.3, C.h, -0.3, 'wall'); box(-36, -34, HZ1, HZ1 + 0.3, C.h, 2.5, 'lintel');
    B.box(mats.brick, IX0, -38.9, -0.3, C.ceil, HZ1 + 0.3, IZ1, { uv: 3, ud: IN }); box(IX0, -38.9, HZ1 + 0.3, IZ1, C.h, -0.3, 'room');
    // dance floor, mirror ball, truss with lamp cans (plain black bodies; their light is in the bake)
    const dg = new THREE.PlaneGeometry(9, 7).rotateX(-Math.PI / 2); dg.translate(-44, SW + 0.012, 48.5); B.geo(m.dance, dg, { ground: true, ...IN });
    { const g = new THREE.IcosahedronGeometry(0.45, 2); g.translate(-44, 3.9, 48.5); B.geo(m.mirror, g, IN); B.cyl(m.steel, -44, 4.4, 48.5, 0.01, 0.01, 0.8, 4, { ud: IN }); }
    for (const z of [45.2, 51.8]) { B.box(m.black, -49.5, -38.5, 4.35, 4.55, z - 0.1, z + 0.1, { uv: null, ud: IN }); for (let x = -48.5; x <= -39; x += 1.9) { const g = new THREE.CylinderGeometry(0.13, 0.11, 0.32, 10); g.rotateX(z < 48 ? 0.7 : -0.7); g.translate(x, 4.1, z); B.geo(m.black, g, IN); } }
    BL(-46.5, 4.2, 46.5, 0xff3fa6, 4.0, 9, 'down', 0.2); BL(-41.5, 4.2, 50.5, 0x3fb6ff, 4.0, 9, 'down', 0.2); BL(-44, 4.3, 48.5, 0xb67aff, 2.0, 10, 'down', 0.3); BL(-49.5, 4.0, 45.5, 0xffb070, 1.6, 7, 'down', 0.2); BL(-38.5, 4.0, 44.5, 0xff8a50, 1.4, 7, 'down', 0.2);
    LS(-46.5, 3.2, 46.5, 0xff4fae, 1.4, 7); LS(-41.5, 3.2, 50.5, 0x4fbaff, 1.4, 7); LS(-35, 3.0, 48, 0xffb060, 1.1, 6); LS(-49.5, 2.8, 46, 0xffa070, 0.7, 5);
    // bar: counter facing the hall, stools, back bar with bottles, warm light above
    B.box(m.black, -35.9, -34.9, SW, SW + 1.08, 43.4, 52.6, { uv: null, ud: IN }); B.box(m.barTop, -36.15, -34.85, SW + 1.08, SW + 1.15, 43.3, 52.7, { uv: null, ud: IN });
    B.box(m.wood, -35.92, -35.88, SW + 0.15, SW + 0.95, 43.5, 52.5, { uv: 1, ud: IN });
    box(-36.15, -34.85, 43.3, 52.7, 1.2, -1, 'bar');
    B.box(m.panel, IX1 - 0.45, IX1 - 0.02, SW, SW + 0.95, 43.4, 52.6, { uv: null, ud: IN }); box(IX1 - 0.45, IX1, 43.4, 52.6, 1.0, -1, 'backbar');
    for (let k = 0; k < 3; k++) { const g = new THREE.PlaneGeometry(9.2, 0.42); const uv = g.attributes.uv; for (let i = 0; i < uv.count; i++) uv.setX(i, uv.getX(i) * 4.6); g.rotateY(-Math.PI / 2); g.translate(IX1 - 0.04, SW + 1.35 + k * 0.5, 48); B.geo(m.bottles, g, IN); B.box(m.steel, IX1 - 0.3, IX1 - 0.02, SW + 1.12 + k * 0.5, SW + 1.15 + k * 0.5, 43.4, 52.6, { uv: null, ud: IN }); }
    for (let z = 44.2; z < 52.4; z += 1.15) { B.cyl(m.steel, -36.55, SW + 0.38, z, 0.03, 0.05, 0.76, 8, { ud: IN }); B.cyl(m.velvet, -36.55, SW + 0.79, z, 0.2, 0.2, 0.08, 14, { ud: IN }); world.circle(-36.55, z, 0.2, SW + 0.83); }
    for (const z of [45.5, 48, 50.5]) BL(-35.4, 3.6, z, 0xffb060, 2.4, 6, 'down', 0.1);
    // DJ booth on a platform (south-west corner) with speaker stacks
    B.box(m.black, IX0, -46.6, SW, SW + 0.4, 52.6, HZ1, { uv: null, ud: { ground: true, ...IN } }); world.box(IX0, -46.6, 52.6, HZ1, SW + 0.4, -0.3, 'stage');
    B.box(m.panel, -50.6, -47.6, SW + 0.4, SW + 1.45, 52.7, 53.3, { uv: null, ud: IN }); B.box(m.steel, -50.4, -47.8, SW + 1.45, SW + 1.5, 52.75, 53.25, { uv: null, ud: IN }); world.box(-50.6, -47.6, 52.7, 53.3, SW + 1.5, -0.3, 'dj');
    for (const x of [-46.1, -51.0]) { B.box(m.black, x - 0.5, x + 0.5, SW, SW + 2.1, 52.9, 53.8, { uv: null, ud: IN }); world.box(x - 0.5, x + 0.5, 52.9, 53.8, SW + 2.1, -0.3, 'speaker'); for (const y of [0.55, 1.45]) B.cyl(m.panel, x, SW + y, 52.88, 0.28, 0.28, 0.02, 16, { rx: Math.PI / 2, ud: IN }); }
    BL(-49, 3.2, 53.6, 0xff6a3a, 2.0, 6, 'down', 0.1);
    // booths (sofas + low tables) along the west wall: the contact person sits here
    for (const [z0, z1] of [[42.3, 45.2], [45.8, 48.7]]) {
      B.box(m.velvet, IX0 + 0.02, IX0 + 0.75, SW, SW + 0.45, z0, z1, { uv: null, ud: IN }); B.box(m.velvet, IX0 + 0.02, IX0 + 0.25, SW + 0.45, SW + 1.1, z0, z1, { uv: null, ud: IN });
      world.box(IX0, IX0 + 0.75, z0, z1, SW + 0.5, -0.3, 'sofa');
      B.cyl(m.barTop, IX0 + 1.5, SW + 0.5, (z0 + z1) / 2, 0.42, 0.42, 0.05, 16, { ud: IN }); B.cyl(m.steel, IX0 + 1.5, SW + 0.25, (z0 + z1) / 2, 0.04, 0.12, 0.5, 8, { ud: IN });
      world.circle(IX0 + 1.5, (z0 + z1) / 2, 0.42, SW + 0.55);
    }
    // high tables by the dance floor
    for (const [x, z] of [[-38.6, 45.4], [-38.6, 51.6]]) { B.cyl(m.barTop, x, SW + 1.1, z, 0.35, 0.35, 0.04, 14, { ud: IN }); B.cyl(m.steel, x, SW + 0.55, z, 0.04, 0.2, 1.1, 8, { ud: IN }); world.circle(x, z, 0.3, SW + 1.12); }
    // ---------- back room (zaplecze): crates, kegs, shelf, fluorescent tube; stairwell to the cellar ----------
    const BZ0 = HZ1 + 0.3;
    B.box(m.clubWall, -38.9, -38.6, SW, 3.4, BZ0, IZ1, { uv: 2, ud: IN });
    box(-38.9, -38.6, BZ0, IZ1, C.h, -0.3, 'wall');
    B.box(m.clubWall, -38.6, IX1, 3.38, 3.4, BZ0, IZ1, { uv: null, ud: IN }); B.box(mats.brick, -38.6, IX1, 3.4, C.ceil, BZ0, IZ1, { uv: 3, ud: IN });
    for (const [x, z, s] of [[-33.2, 56.2, 0.8], [-33.2, 57.1, 0.8], [-33.25, 56.65, 0.7]]) { const y = s === 0.7 ? SW + 0.8 : SW; B.box(m.crate, x - s / 2, x + s / 2, y, y + s, z - s / 2, z + s / 2, { uv: 1, ud: IN }); }
    world.box(-33.65, IX1, 55.8, 57.5, SW + 1.5, -0.3, 'crates');
    for (const [x, z] of [[-33.1, 59.0], [-33.1, 59.7], [-33.8, 59.35]]) { B.cyl(m.keg, x, SW + 0.3, z, 0.25, 0.25, 0.6, 14, { ud: IN }); world.circle(x, z, 0.26, SW + 0.62); }
    B.box(m.panel, -36.4, -35.9, SW, SW + 1.9, 60.2, IZ1, { uv: null, ud: IN }); box(-36.4, -35.9, 60.2, IZ1, SW + 1.9, -0.3, 'shelf');
    B.box(m.white, -36, -34, 3.3, 3.36, 58.4, 58.6, { uv: null, ud: IN }); BL(-35, 3.2, 58.5, 0xeaf2ff, 2.8, 7, 'down', 0.4); LS(-35, 3.0, 58.5, 0xeaf2ff, 0.8, 6);
    BL(-34.4, 2.9, zb + 0.6, 0xffe1b0, 0.7, 6, 'down', 0.1);       // small lamp over the yard door (outside)
    B.box(m.black, -34.65, -34.15, 2.75, 2.85, zb, zb + 0.35, { uv: null });
    // stairwell: 15 steps down to the cellar landing, railing on the open side
    const stepsN = 15, sz0 = 55.6, run = 0.313, rise = (SW - CY) / stepsN;
    world.pit(-38.4, -36.6, sz0, IZ1, CY);
    for (let i = 0; i < stepsN; i++) {
      const z0 = sz0 + i * run, z1 = z0 + run, top = SW - (i + 1) * rise;
      B.box(m.concrete, -38.4, -36.6, top - 0.18, top, z0, z1, { uv: 1, ud: { ground: true, ...IN } });
      B.box(m.concrete, -38.4, -36.6, CY - 0.1, top - 0.18, z0, z1, { uv: 1, ud: IN });
      world.box(-38.4, -36.6, z0, z1, top, -10, 'stair');
    }
    B.box(m.concrete, -38.4, -36.6, CY - 0.2, CY, sz0 + stepsN * run, IZ1, { uv: 1, ud: { ground: true, ...IN } });
    B.box(m.brick, -38.4, -36.6, CY, 0, IZ1 - 0.02, IZ1, { uv: 2, ud: IN }); box(-38.6, -36.35, IZ1, IZ1 + 1, 0, -10, 'cellar');
    B.box(m.clubWall, -36.62, -36.6, CY, SW, sz0, IZ1, { uv: 2, ud: IN });
    box(-36.6, -36.35, sz0 + 0.6, IZ1, SW + 1.1, -10, 'rail');
    for (let z = sz0 + 0.6; z <= IZ1; z += 0.9) B.cyl(m.steel, -36.45, SW + 0.5, z, 0.025, 0.025, 1.0, 6, { ud: IN });
    { const g = new THREE.CylinderGeometry(0.03, 0.03, IZ1 - sz0 - 0.6, 6); g.rotateX(Math.PI / 2); g.translate(-36.45, SW + 1.0, (sz0 + 0.6 + IZ1) / 2); B.geo(m.steel, g, IN); }
    { const L = Math.hypot(stepsN * run, SW - CY), a = Math.atan2(SW - CY, stepsN * run); const g = new THREE.CylinderGeometry(0.025, 0.025, L, 6); g.rotateX(Math.PI / 2 + a); g.translate(-38.3, (SW + CY) / 2 + 0.95, sz0 + stepsN * run / 2); B.geo(m.steel, g, IN); }
    // ---------- cellar (fight 2) ----------
    const CX0 = IX0, CX1 = -38.6, CZ0 = 44, CZ1 = IZ1;
    world.pit(CX0, CX1 + 0.2, CZ0, CZ1, CY);
    B.box(m.concrete, CX0, CX1, CY - 0.2, CY, CZ0, CZ1, { uv: 2, ud: { ground: true, ...IN } });
    B.box(m.brick, CX0, CX0 + 0.02, CY, C.cellarCeil, CZ0, CZ1, { uv: 2, ud: IN });
    B.box(m.brick, CX0, CX1, CY, C.cellarCeil, CZ0, CZ0 + 0.3, { uv: 2, ud: IN }); box(CX0, CX1, CZ0 - 2, CZ0 + 0.3, -0.3, -10, 'cellar');
    B.box(m.brick, CX0, CX1 + 0.2, CY, C.cellarCeil, CZ1 - 0.02, CZ1, { uv: 2, ud: IN });
    // east cellar wall with the opening to the stair landing (z 60.3..61.7)
    B.box(m.brick, CX1, CX1 + 0.2, CY, C.cellarCeil, CZ0, 60.3, { uv: 2, ud: IN }); box(CX1, CX1 + 0.2, CZ0, 60.3, -0.3, -10, 'cellar');
    B.box(m.brick, CX1, CX1 + 0.2, CY + 2.4, C.cellarCeil, 60.3, CZ1, { uv: 2, ud: IN }); box(CX1, CX1 + 0.2, 60.3, CZ1, -0.3, CY + 2.4, 'lintel');
    box(CX0 - 2, CX0, CZ0, CZ1, -0.3, -10, 'cellar'); box(CX0, CX1 + 0.2, CZ1, CZ1 + 2, -0.3, -10, 'cellar');
    B.box(m.concrete, CX0, CX1 + 0.2, C.cellarCeil, C.cellarCeil + 0.02, CZ0, CZ1, { uv: 2, ud: IN });
    for (const z of [47, 52, 57]) B.cyl(m.pipe, (CX0 + CX1) / 2, C.cellarCeil - 0.2, z, 0.09, 0.09, CX1 - CX0, 8, { rz: Math.PI / 2, ud: IN });
    for (const x of [-49, -42]) B.cyl(m.pipe, x, C.cellarCeil - 0.35, (CZ0 + CZ1) / 2, 0.06, 0.06, CZ1 - CZ0, 8, { rx: Math.PI / 2, ud: IN });
    // a hanging bulb, old sofa, table with the money bag, crates and beer crates along the walls (centre stays free)
    B.cyl(m.black, -45.5, C.cellarCeil - 0.25, 52, 0.08, 0.14, 0.18, 10, { ud: IN }); B.cyl(m.black, -45.5, C.cellarCeil - 0.08, 52, 0.006, 0.006, 0.3, 4, { ud: IN });
    BL(-45.5, C.cellarCeil - 0.45, 52, 0xffc27a, 3.6, 11, 'omni', 0.1); BL(-39.8, C.cellarCeil - 0.3, 60.9, 0xffc27a, 1.8, 6, 'omni', 0.05); BL(-48.5, C.cellarCeil - 0.3, 58.5, 0xffb060, 1.4, 6, 'omni', 0.05);
    LS(-45.5, CY + 2.6, 52, 0xffc27a, 1.2, 8);
    B.box(m.sofa, CX0 + 0.02, CX0 + 0.9, CY, CY + 0.45, 52.5, 55.5, { uv: null, ud: IN }); B.box(m.sofa, CX0 + 0.02, CX0 + 0.3, CY + 0.45, CY + 1.05, 52.5, 55.5, { uv: null, ud: IN }); world.box(CX0, CX0 + 0.9, 52.5, 55.5, CY + 0.5, -10, 'sofa');
    B.box(m.wood, -51.2, -49.8, CY + 0.72, CY + 0.78, 46.2, 47.8, { uv: 1, ud: IN }); for (const [x, z] of [[-51.1, 46.3], [-49.9, 46.3], [-51.1, 47.7], [-49.9, 47.7]]) B.box(m.wood, x - 0.04, x + 0.04, CY, CY + 0.72, z - 0.04, z + 0.04, { uv: null, ud: IN }); world.box(-51.2, -49.8, 46.2, 47.8, CY + 0.8, -10, 'table');
    for (const [x, z, s] of [[-50.9, 58.6, 0.9], [-50.9, 59.6, 0.9], [-49.9, 60.9, 0.8], [-40.2, 45.0, 0.8], [-41.1, 45.0, 0.8], [-40.6, 45.0, 0.6]]) { const y = (x === -40.6) ? CY + 0.8 : CY; B.box(m.crate, x - s / 2, x + s / 2, y, y + s, z - s / 2, z + s / 2, { uv: 1, ud: IN }); }
    world.box(-51.4, -49.4, 58.1, CZ1, CY + 1.0, -10, 'crates'); world.box(-41.6, -39.7, CZ0, 45.5, CY + 1.5, -10, 'crates');
    void IX0; void IX1;
  }

  B.build();
  out.update = () => { };
  return out;
}

function mirrorBallTex() {
  const S = 128, c = CT.canvas(S, S), x = c.getContext('2d');
  for (let i = 0; i < 16; i++) for (let j = 0; j < 16; j++) { const v = 120 + Math.random() * 90; x.fillStyle = `rgb(${v | 0},${v | 0},${v + 8 | 0})`; x.fillRect(i * 8 + 0.5, j * 8 + 0.5, 7, 7); }
  return CT.ctex(c);
}
