// Sky, sun/moon, fog and the day/night cycle, plus a cheap "lamp field": up to LAMP_N point lights
// evaluated in every outdoor material's shader (street lamps, headlights, police flashers, neons)
// without real three.js lights.
import * as THREE from 'three';

export const LAMP_N = 16;
export const lampU = {
  lampPos: { value: Array.from({ length: LAMP_N }, () => new THREE.Vector4(0, -500, 0, 1)) },
  lampCol: { value: Array.from({ length: LAMP_N }, () => new THREE.Vector3()) },
  uAmb: { value: new THREE.Color(1, 1, 1) },
};

const LAMP_GLSL = /* glsl */`
uniform vec4 lampPos[${LAMP_N}]; uniform vec3 lampCol[${LAMP_N}]; uniform vec3 uAmb;
varying vec3 vWP;
vec3 lampField(vec3 p, vec3 n) {
  vec3 acc = vec3(0.0);
  for (int i = 0; i < ${LAMP_N}; i++) {
    vec3 d = lampPos[i].xyz - p; float dist = length(d);
    float f = clamp(1.0 - dist / lampPos[i].w, 0.0, 1.0); f *= f;
    float ndl = clamp(dot(n, d / max(dist, 1e-3)) * 0.7 + 0.3, 0.0, 1.0);
    acc += lampCol[i] * f * ndl;
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
      .replace('#include <project_vertex>', '#include <project_vertex>\nvWP = (modelMatrix * vec4(transformed, 1.0)).xyz;');
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

// ---------------- sky dome ----------------
function skyMaterial() {
  return new THREE.ShaderMaterial({
    uniforms: {
      sunDir: { value: new THREE.Vector3(0, 1, 0) }, moonDir: { value: new THREE.Vector3(0, -1, 0) },
      zen: { value: new THREE.Color() }, hor: { value: new THREE.Color() }, sunCol: { value: new THREE.Color() },
      cloudCol: { value: new THREE.Color() }, stars: { value: 0 }, time: { value: 0 }, sunSize: { value: 1 },
    },
    vertexShader: `varying vec3 vDir; void main(){ vDir = position; vec4 p = projectionMatrix * modelViewMatrix * vec4(position, 1.0); gl_Position = p.xyww; }`,
    fragmentShader: `
uniform vec3 sunDir, moonDir, zen, hor, sunCol, cloudCol; uniform float stars, time;
varying vec3 vDir;
float h1(vec3 p){ p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
float n2(vec2 p){ vec2 i = floor(p), f = fract(p); f = f * f * (3.0 - 2.0 * f);
  float a = h1(vec3(i, 1.0)), b = h1(vec3(i + vec2(1, 0), 1.0)), c = h1(vec3(i + vec2(0, 1), 1.0)), d = h1(vec3(i + vec2(1, 1), 1.0));
  return mix(mix(a, b, f.x), mix(c, d, f.x), f.y); }
float fbm(vec2 p){ float s = 0.0, a = 0.5; for (int i = 0; i < 4; i++) { s += a * n2(p); p *= 2.03; a *= 0.5; } return s; }
void main(){
  vec3 d = normalize(vDir); float h = d.y;
  vec3 col = mix(hor, zen, pow(clamp(h, 0.0, 1.0), 0.5));
  col = mix(col, hor * 0.45, smoothstep(0.0, -0.25, h));
  float sd = max(dot(d, sunDir), 0.0);
  col += sunCol * (smoothstep(0.9993, 0.9997, sd) * 18.0 + pow(sd, 14.0) * 0.45 + pow(sd, 3.0) * 0.12 * (1.0 - clamp(h, 0.0, 1.0)));
  float md = max(dot(d, moonDir), 0.0);
  col += vec3(0.75, 0.82, 1.0) * (smoothstep(0.99955, 0.99975, md) * 2.4 + pow(md, 90.0) * 0.06) * stars;
  vec3 sp = floor(d * 520.0); float s = h1(sp);
  col += vec3(0.85, 0.9, 1.0) * step(0.9975, s) * smoothstep(0.02, 0.3, h) * stars * (0.55 + 0.45 * sin(time * 2.0 + s * 300.0));
  vec2 uv = d.xz / max(h, 0.04) * 0.35 + vec2(time * 0.004, time * 0.0015);
  float c = smoothstep(0.5, 0.86, fbm(uv * 2.0)) * smoothstep(0.0, 0.22, h);
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
  nightZen: C(0.003, 0.005, 0.014), nightHor: C(0.03, 0.028, 0.045),
  sunDay: C(1.0, 0.95, 0.88), sunSet: C(1.0, 0.45, 0.16),
  cloudDay: C(0.95, 0.96, 1.0), cloudSet: C(0.9, 0.45, 0.32), cloudNight: C(0.02, 0.022, 0.03),
  fogDay: C(0.55, 0.62, 0.72), fogSet: C(0.55, 0.36, 0.28), fogNight: C(0.012, 0.014, 0.024),
  ambDay: C(1.0, 1.0, 1.0), ambSet: C(0.95, 0.72, 0.58), ambNight: C(0.075, 0.085, 0.13),
  hemiSkyDay: C(0.62, 0.72, 0.95), hemiSkyNight: C(0.07, 0.09, 0.17), hemiGround: C(0.18, 0.15, 0.12),
};

export class DayNight {
  constructor(scene, { hour = 18.5, dayLengthMin = 12 } = {}) {
    this.hour = hour; this.speed = 24 / (dayLengthMin * 60); this.fast = 1; this.time = 0;
    this.sky = new THREE.Mesh(new THREE.SphereGeometry(900, 32, 16), skyMaterial());
    this.sky.frustumCulled = false; this.sky.renderOrder = -10;
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
    u.stars.value = night;
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
    else { key.color.set(0x8fa6ff); key.intensity = 0.45 * o.night; }
  }
}
export { sm as smooth };
