// Final pass: tone mapping + sRGB (like OutputPass) plus a light cinematic grade:
// saturation, gentle S-curve, lifted blacks, vignette and fine film grain. One full-screen pass in total.
import { OutputPass } from '../three/examples/jsm/postprocessing/OutputPass.js';
import * as THREE from 'three';

const FRAG = /* glsl */`
precision highp float;
uniform sampler2D tDiffuse;
uniform float uTime, uVig, uGrain, uSat, uCurve, uHurt;
uniform vec3 uLift, uTint;
#include <tonemapping_pars_fragment>
#include <colorspace_pars_fragment>
varying vec2 vUv;
float hash(vec2 p) { vec3 q = fract(vec3(p.xyx) * 0.1031); q += dot(q, q.yzx + 33.33); return fract((q.x + q.y) * q.z); }
void main() {
  vec4 c = texture2D(tDiffuse, vUv);
  #ifdef ACES_FILMIC_TONE_MAPPING
    c.rgb = ACESFilmicToneMapping(c.rgb);
  #elif defined( AGX_TONE_MAPPING )
    c.rgb = AgXToneMapping(c.rgb);
  #endif
  float l = dot(c.rgb, vec3(0.2126, 0.7152, 0.0722));
  c.rgb = mix(vec3(l), c.rgb, uSat) * uTint;
  #ifdef SRGB_TRANSFER
    c = sRGBTransferOETF(c);
  #endif
  c.rgb = clamp(c.rgb, 0.0, 1.0);
  c.rgb = mix(c.rgb, c.rgb * c.rgb * (3.0 - 2.0 * c.rgb), uCurve);
  c.rgb = uLift + c.rgb * (1.0 - uLift);
  vec2 q = vUv - 0.5; q.x *= 1.25;
  float v = 1.0 - smoothstep(0.18, 0.62, length(q)) * uVig;
  c.rgb *= v;
  float hl = dot(c.rgb, vec3(0.333));
  c.rgb = mix(c.rgb, vec3(hl) * vec3(1.3, 0.45, 0.4), uHurt * smoothstep(0.38, 0.78, length(q)));
  c.rgb += (hash(gl_FragCoord.xy + fract(uTime * 7.13) * 917.0) - 0.5) * uGrain;
  gl_FragColor = c;
}`;

export class GradePass extends OutputPass {
  constructor() {
    super();
    Object.assign(this.uniforms, {
      uTime: { value: 0 }, uVig: { value: 0.42 }, uGrain: { value: 0.028 }, uSat: { value: 1.08 }, uCurve: { value: 0.22 }, uHurt: { value: 0 },
      uLift: { value: new THREE.Color(0.012, 0.012, 0.014) }, uTint: { value: new THREE.Color(1, 1, 1) },
    });
    this.material.uniforms = this.uniforms;
    this.material.fragmentShader = FRAG;
    this.material.needsUpdate = true;
  }
}
