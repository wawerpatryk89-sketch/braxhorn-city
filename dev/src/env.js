// Sky, sun/moon, fog and the day/night cycle, plus a cheap "lamp field": up to LAMP_N point lights
// evaluated in every outdoor material's shader (street lamps, headlights, police flashers, neons)
// without real three.js lights.
import * as THREE from 'three';

export const LAMP_N = 16;
export const lampU = {
  lampPos: { value: Array.from({ length: LAMP_N }, () => new THREE.Vector4(0, -500, 0, 1)) },
  lampCol: { value: Array.from({ length: LAMP_N }, () => new THREE.Vector3()) },
  lampDir: { value: Array.from({ length: LAMP_N }, () => new THREE.Vector4(0, -1, 0, -2)) }, // spot: xyz dir, w = cos(cutoff); w < -1: omni
  uAmb: { value: new THREE.Color(1, 1, 1) },
};

const LAMP_GLSL = /* glsl */`
uniform vec4 lampPos[${LAMP_N}]; uniform vec3 lampCol[${LAMP_N}]; uniform vec4 lampDir[${LAMP_N}]; uniform vec3 uAmb;
varying vec3 vWP;
vec3 lampField(vec3 p, vec3 n) {
  vec3 acc = vec3(0.0);
  for (int i = 0; i < ${LAMP_N}; i++) {
    vec3 d = lampPos[i].xyz - p; float dist = length(d);
    float f = clamp(1.0 - dist / lampPos[i].w, 0.0, 1.0); f *= f;
    vec3 l = d / max(dist, 1e-3);
    float ndl = clamp(dot(n, l) * 0.7 + 0.3, 0.0, 1.0);
    float spot = lampDir[i].w < -1.0 ? 1.0 : smoothstep(lampDir[i].w, lampDir[i].w + 0.1, dot(-l, lampDir[i].xyz));
    acc += lampCol[i] * f * ndl * spot;
  }
  return acc;
}`;

// mode 'baked': unlit photogrammetry (MeshBasicMaterial): colour * ambient + lamps
// mode 'lit'  : MeshStandard/Lambert: normal lighting + lamps on top
export function addLampField(mat, mode = 'lit') {
  if (!mat || mat.userData.lampField) return mat;
  mat.userData.lampField = mode;
  const prev = mat.onBeforeCompile;
  mat.onBeforeCompile = (sh, r) => {
    if (prev) prev(sh, r);
    Object.assign(sh.uniforms, lampU);
    sh.vertexShader = sh.vertexShader
      .replace('#include <common>', '#include <common>\nvarying vec3 vWP;')
      .replace('#include <project_vertex>', '#include <project_vertex>\n{ vec4 wp_ = vec4(transformed, 1.0);\n#ifdef USE_INSTANCING\nwp_ = instanceMatrix * wp_;\n#endif\nvWP = (modelMatrix * wp_).xyz; }');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\n' + LAMP_GLSL);
    if (mode === 'baked') {
      sh.fragmentShader = sh.fragmentShader.replace('#include <opaque_fragment>', `
{ vec3 nW = normalize(cross(dFdx(vWP), dFdy(vWP)));
  outgoingLight = outgoingLight * uAmb + diffuseColor.rgb * lampField(vWP, nW); }
#include <opaque_fragment>`);
    } else {
      sh.fragmentShader = sh.fragmentShader.replace('#include <opaque_fragment>', `
{ vec3 nW = inverseTransformDirection(normal, viewMatrix);
  outgoingLight += diffuseColor.rgb * lampField(vWP, nW); }
#include <opaque_fragment>`);
    }
  };
  mat.customProgramCacheKey = () => 'lamp-' + mode + (prev ? '-x' : '');
  mat.needsUpdate = true;
  return mat;
}


// tileable value-noise fbm on the CPU (robust on mediump-only mobile GPUs, unlike shader hashes)
function cloudTex() {
  const S = 256, data = new Uint8Array(S * S * 4);
  const oct = [[8, 0.5], [16, 0.25], [32, 0.15], [64, 0.1]];
  const grids = oct.map(([g]) => { const a = new Float32Array(g * g); for (let i = 0; i < a.length; i++) a[i] = Math.random(); return a; });
  for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
    let v = 0;
    oct.forEach(([g, w], k) => {
      const fx = x / S * g, fy = y / S * g, ix = Math.floor(fx), iy = Math.floor(fy), tx = fx - ix, ty = fy - iy;
      const sx = tx * tx * (3 - 2 * tx), sy = ty * ty * (3 - 2 * ty), G = grids[k];
      const at = (i, j) => G[((j + g) % g) * g + ((i + g) % g)];
      v += w * ((at(ix, iy) * (1 - sx) + at(ix + 1, iy) * sx) * (1 - sy) + (at(ix, iy + 1) * (1 - sx) + at(ix + 1, iy + 1) * sx) * sy);
    });
    const o = (y * S + x) * 4; data[o] = data[o + 1] = data[o + 2] = Math.min(255, v * 255); data[o + 3] = 255;
  }
  const t = new THREE.DataTexture(data, S, S); t.wrapS = t.wrapT = THREE.RepeatWrapping; t.magFilter = THREE.LinearFilter; t.minFilter = THREE.LinearMipmapLinearFilter; t.generateMipmaps = true; t.needsUpdate = true;
  return t;
}
function starPoints(n = 1600) {
  const P = new Float32Array(n * 3), A = new Float32Array(n * 2);
  for (let i = 0; i < n; i++) {
    const y = Math.pow(Math.random(), 0.7) * 0.97 + 0.03, a = Math.random() * Math.PI * 2, r = Math.sqrt(1 - y * y);
    P[i * 3] = Math.cos(a) * r * 800; P[i * 3 + 1] = y * 800; P[i * 3 + 2] = Math.sin(a) * r * 800;
    const m = Math.random(); A[i * 2] = m < 0.92 ? 1.0 + m * 0.8 : 2.4 + (m - 0.92) * 14; A[i * 2 + 1] = Math.random() * 6.28;
  }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(P, 3)); g.setAttribute('aS', new THREE.BufferAttribute(A, 2));
  const m = new THREE.ShaderMaterial({
    uniforms: { stars: { value: 0 }, time: { value: 0 }, px: { value: 1 } },
    vertexShader: `attribute vec2 aS; uniform float time, px; varying float vA;
      void main(){ vec4 p = projectionMatrix * modelViewMatrix * vec4(position, 1.0); gl_Position = p.xyww; gl_PointSize = aS.x * px;
        vA = (0.6 + 0.4 * sin(time * (1.3 + fract(aS.y) * 2.0) + aS.y * 7.0)) * clamp(position.y / 160.0, 0.0, 1.0); }`,
    fragmentShader: `uniform float stars; varying float vA;
      void main(){ vec2 q = gl_PointCoord - 0.5; float a = smoothstep(0.5, 0.0, length(q)); gl_FragColor = vec4(vec3(0.86, 0.9, 1.0) * a * vA * stars, 1.0); }`,
    transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, fog: false,
  });
  const pts = new THREE.Points(g, m); pts.frustumCulled = false; pts.renderOrder = -9; return pts;
}

// ---------------- sky dome ----------------
function skyMaterial() {
  return new THREE.ShaderMaterial({
    uniforms: {
      sunDir: { value: new THREE.Vector3(0, 1, 0) }, moonDir: { value: new THREE.Vector3(0, -1, 0) },
      zen: { value: new THREE.Color() }, hor: { value: new THREE.Color() }, sunCol: { value: new THREE.Color() },
      cloudCol: { value: new THREE.Color() }, stars: { value: 0 }, time: { value: 0 }, sunSize: { value: 1 }, tCloud: { value: cloudTex() },
    },
    vertexShader: `varying vec3 vDir; void main(){ vDir = position; vec4 p = projectionMatrix * modelViewMatrix * vec4(position, 1.0); gl_Position = p.xyww; }`,
    fragmentShader: `
uniform vec3 sunDir, moonDir, zen, hor, sunCol, cloudCol; uniform float stars, time; uniform sampler2D tCloud;
varying vec3 vDir;
void main(){
  vec3 d = normalize(vDir); float h = d.y;
  vec3 col = mix(hor, zen, pow(clamp(h, 0.0, 1.0), 0.5));
  col = mix(col, hor * 0.45, smoothstep(0.0, -0.25, h));
  float sd = max(dot(d, sunDir), 0.0);
  col += sunCol * (smoothstep(0.9993, 0.9997, sd) * 18.0 + pow(sd, 14.0) * 0.45 + pow(sd, 3.0) * 0.12 * (1.0 - clamp(h, 0.0, 1.0)));
  float md = max(dot(d, moonDir), 0.0);
  col += vec3(0.75, 0.82, 1.0) * (smoothstep(0.99955, 0.99975, md) * 2.4 + pow(md, 90.0) * 0.06) * stars;
  vec2 uv = d.xz / max(h, 0.06) * 0.16;
  float n = texture2D(tCloud, uv + vec2(time * 0.0016, time * 0.0007)).r * 0.65 + texture2D(tCloud, uv * 2.7 + vec2(0.37 - time * 0.0011, 0.71)).r * 0.35;
  float c = smoothstep(0.5, 0.82, n) * smoothstep(0.0, 0.22, h);
  col = mix(col, cloudCol, c * 0.85);
  gl_FragColor = vec4(col, 1.0);
}`,
    side: THREE.BackSide, depthWrite: false, fog: false,
  });
}

const C = (r, g, b) => new THREE.Color(r, g, b);
const lerpC = (a, b, t, out = new THREE.Color()) => out.copy(a).lerp(b, t);
const sm = (a, b, x) => { const t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

// palette (linear HDR)
const P = {
  dayZen: C(0.16, 0.33, 0.78), dayHor: C(0.62, 0.74, 0.92),
  setZen: C(0.12, 0.16, 0.42), setHor: C(1.25, 0.52, 0.22),
  nightZen: C(0.004, 0.005, 0.011), nightHor: C(0.032, 0.03, 0.04),
  sunDay: C(1.0, 0.95, 0.88), sunSet: C(1.0, 0.45, 0.16),
  cloudDay: C(0.95, 0.96, 1.0), cloudSet: C(0.9, 0.45, 0.32), cloudNight: C(0.02, 0.022, 0.03),
  fogDay: C(0.55, 0.62, 0.72), fogSet: C(0.55, 0.36, 0.28), fogNight: C(0.014, 0.014, 0.019),
  ambDay: C(1.0, 1.0, 1.0), ambSet: C(0.95, 0.72, 0.58), ambNight: C(0.1, 0.1, 0.112),
  hemiSkyDay: C(0.62, 0.72, 0.95), hemiSkyNight: C(0.08, 0.085, 0.11), hemiGround: C(0.18, 0.15, 0.12),
};

export class DayNight {
  constructor(scene, { hour = 18.5, dayLengthMin = 12 } = {}) {
    this.hour = hour; this.speed = 24 / (dayLengthMin * 60); this.fast = 1; this.time = 0;
    this.sky = new THREE.Mesh(new THREE.SphereGeometry(900, 32, 16), skyMaterial());
    this.sky.frustumCulled = false; this.sky.renderOrder = -10;
    this.stars = starPoints(); this.sky.add(this.stars);
    scene.add(this.sky);
    this.sunDir = new THREE.Vector3(); this.moonDir = new THREE.Vector3();
    this.out = { day: 1, night: 0, golden: 0, lamps: 0, elev: 0 };
    this.fogColor = new THREE.Color(); this.amb = new THREE.Color();
  }
  get clock() { const h = Math.floor(this.hour), m = Math.floor((this.hour - h) * 60); return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`; }
  setHour(h) { this.hour = ((h % 24) + 24) % 24; }
  update(dt) {
    this.time += dt;
    this.hour = (this.hour + dt * this.speed * this.fast) % 24;
    // sun path: rises in the east (+x), sets in the west (-x), tilted towards the south (-z)
    const a = (this.hour - 6) / 12 * Math.PI;
    const elev = Math.sin(a) * 62 * Math.PI / 180;
    const az = Math.cos(a);
    this.sunDir.set(az * Math.cos(elev), Math.sin(elev), -0.45 * Math.cos(elev)).normalize();
    this.moonDir.set(-this.sunDir.x * 0.9, Math.max(0.35, -this.sunDir.y), 0.3).normalize();
    const e = elev * 180 / Math.PI;
    const day = sm(-5, 14, e), night = 1 - sm(-11, 1, e), golden = Math.max(0, 1 - Math.abs(e - 3) / 13) * (1 - night * 0.6);
    const o = this.out; o.day = day; o.night = night; o.golden = golden; o.elev = e; o.lamps = sm(5, -3, e);
    const u = this.sky.material.uniforms;
    u.sunDir.value.copy(this.sunDir); u.moonDir.value.copy(this.moonDir); u.time.value = this.time;
    const zen = lerpC(P.nightZen, P.dayZen, day), hor = lerpC(P.nightHor, P.dayHor, day);
    zen.lerp(P.setZen, golden * 0.55); hor.lerp(P.setHor, golden * 0.8);
    u.zen.value.copy(zen); u.hor.value.copy(hor);
    u.sunCol.value.copy(lerpC(P.sunDay, P.sunSet, golden)).multiplyScalar(sm(-4, 2, e));
    u.cloudCol.value.copy(lerpC(P.cloudNight, P.cloudDay, day)).lerp(P.cloudSet, golden * 0.7);
    u.stars.value = night; const su = this.stars.material.uniforms; su.stars.value = night * (1 - golden); su.time.value = this.time; this.stars.visible = night > 0.02;
    this.fogColor.copy(lerpC(P.fogNight, P.fogDay, day)).lerp(P.fogSet, golden * 0.6);
    this.amb.copy(lerpC(P.ambNight, P.ambDay, day)).lerp(P.ambSet, golden * 0.5);
    return o;
  }
  // hemisphere + key light parameters for the outdoors
  outdoorLights(hemi, key) {
    const o = this.out;
    hemi.color.copy(lerpC(P.hemiSkyNight, P.hemiSkyDay, o.day)).lerp(P.setHor, o.golden * 0.25);
    hemi.groundColor.copy(P.hemiGround).multiplyScalar(0.25 + 0.75 * o.day);
    hemi.intensity = 0.55 + 0.75 * o.day;
    if (o.elev > -2) { key.color.copy(lerpC(P.sunDay, P.sunSet, o.golden)); key.intensity = 2.8 * sm(-2, 8, o.elev); }
    else { key.color.set(0xb7c3e3); key.intensity = 0.42 * o.night; }
  }
}
export { sm as smooth };
