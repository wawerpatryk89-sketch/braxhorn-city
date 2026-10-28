// Wet, polished concrete floor with a planar reflection that is smeared vertically,
// which gives the long light streaks seen in the reference video.
import * as THREE from 'three';

export function createWetFloor({ width, depth, center, maps, wet, quality }) {
  const geo = new THREE.PlaneGeometry(width, depth, 1, 1);
  const repX = width / 4, repY = depth / 4;
  for (const k of ['map', 'roughnessMap', 'normalMap']) maps[k].repeat.set(repX, repY);
  const mat = new THREE.MeshStandardMaterial({
    map: maps.map, roughnessMap: maps.roughnessMap, normalMap: maps.normalMap,
    normalScale: new THREE.Vector2(0.55, 0.55), roughness: 0.62, metalness: 0.0, envMapIntensity: 0.35,
  });
  const mesh = new THREE.Mesh(geo, mat);
  mesh.rotation.x = -Math.PI / 2;
  mesh.position.copy(center);
  mesh.receiveShadow = true;
  mesh.name = 'floor';

  const enabled = quality.reflections;
  const res = quality.reflectionScale;
  const rt = new THREE.WebGLRenderTarget(2, 2, { type: THREE.HalfFloatType, samples: 0 });
  rt.texture.generateMipmaps = false;
  const textureMatrix = new THREE.Matrix4();
  const uniforms = {
    tRefl: { value: rt.texture }, tWet: { value: wet }, reflMatrix: { value: textureMatrix },
    reflTexel: { value: new THREE.Vector2(1 / 512, 1 / 512) }, reflStrength: { value: enabled ? 1.0 : 0.0 },
    streak: { value: quality.streak },
  };
  const taps = quality.streakTaps;
  mat.onBeforeCompile = (shader) => {
    Object.assign(shader.uniforms, uniforms);
    shader.vertexShader = shader.vertexShader
      .replace('#include <common>', '#include <common>\nuniform mat4 reflMatrix;\nvarying vec4 vReflUv;\nvarying vec2 vFloorUv;')
      .replace('#include <uv_vertex>', '#include <uv_vertex>\nvFloorUv = uv;')
      .replace('#include <project_vertex>', '#include <project_vertex>\nvReflUv = reflMatrix * vec4( transformed, 1.0 );');
    shader.fragmentShader = shader.fragmentShader
      .replace('#include <common>', `#include <common>
uniform sampler2D tRefl; uniform sampler2D tWet; uniform vec2 reflTexel; uniform float reflStrength; uniform float streak;
varying vec4 vReflUv; varying vec2 vFloorUv;`)
      .replace('#include <roughnessmap_fragment>', `#include <roughnessmap_fragment>
float wetK = texture2D( tWet, vFloorUv ).r;
roughnessFactor = mix( roughnessFactor, roughnessFactor * 0.35, wetK );`)
      .replace('#include <opaque_fragment>', `
{
  vec2 ruv = vReflUv.xy / vReflUv.w;
  vec2 bend = ( normal.xy - nonPerturbedNormal.xy ) * 0.028;
  ruv += bend * ( 0.4 + wetK );
  vec3 acc = vec3( 0.0 ); float wsum = 0.0;
  for ( int i = -${taps}; i <= ${taps}; i ++ ) {
    float fi = float( i ) / float( ${taps} );
    float w = exp( -fi * fi * 2.2 );
    vec2 o = vec2( fi * reflTexel.x * 1.5, fi * reflTexel.y * streak * ( 0.7 + 0.8 * wetK ) );
    acc += texture2D( tRefl, ruv + o ).rgb * w; wsum += w;
  }
  vec3 refl = acc / wsum;
  float cosT = clamp( dot( normal, normalize( vViewPosition ) ), 0.0, 1.0 );
  float fres = 0.05 + 0.95 * pow( 1.0 - cosT, 5.0 );
  float amt = reflStrength * mix( 0.22, 0.95, wetK ) * mix( 0.55, 1.0, fres );
  outgoingLight = outgoingLight * ( 1.0 - 0.45 * amt ) + refl * amt;
}
#include <opaque_fragment>`);
  };

  // --- planar mirror camera (same math as three's Reflector) ---
  const virtualCamera = new THREE.PerspectiveCamera();
  const reflectorPlane = new THREE.Plane(), normal = new THREE.Vector3(), reflectorWorldPosition = new THREE.Vector3(),
    cameraWorldPosition = new THREE.Vector3(), rotationMatrix = new THREE.Matrix4(), lookAtPosition = new THREE.Vector3(0, 0, -1),
    clipPlane = new THREE.Vector4(), view = new THREE.Vector3(), target = new THREE.Vector3(), q = new THREE.Vector4();

  function setSize(w, h) {
    const rw = Math.max(64, Math.round(w * res)), rh = Math.max(64, Math.round(h * res));
    rt.setSize(rw, rh);
    uniforms.reflTexel.value.set(1 / rw, 1 / rh);
  }

  function update(renderer, scene, camera) {
    if (!enabled) return;
    mesh.updateMatrixWorld();
    reflectorWorldPosition.setFromMatrixPosition(mesh.matrixWorld);
    cameraWorldPosition.setFromMatrixPosition(camera.matrixWorld);
    rotationMatrix.extractRotation(mesh.matrixWorld);
    normal.set(0, 0, 1).applyMatrix4(rotationMatrix);
    view.subVectors(reflectorWorldPosition, cameraWorldPosition);
    if (view.dot(normal) > 0) return;
    view.reflect(normal).negate().add(reflectorWorldPosition);
    rotationMatrix.extractRotation(camera.matrixWorld);
    lookAtPosition.set(0, 0, -1).applyMatrix4(rotationMatrix).add(cameraWorldPosition);
    target.subVectors(reflectorWorldPosition, lookAtPosition).reflect(normal).negate().add(reflectorWorldPosition);
    virtualCamera.position.copy(view);
    virtualCamera.up.set(0, 1, 0).applyMatrix4(rotationMatrix).reflect(normal);
    virtualCamera.lookAt(target);
    virtualCamera.far = camera.far;
    virtualCamera.updateMatrixWorld();
    virtualCamera.projectionMatrix.copy(camera.projectionMatrix);
    virtualCamera.layers.mask = 1 | 2; // layer 2 = reflection-only helpers, layer 3 excluded
    textureMatrix.set(0.5, 0, 0, 0.5, 0, 0.5, 0, 0.5, 0, 0, 0.5, 0.5, 0, 0, 0, 1);
    textureMatrix.multiply(virtualCamera.projectionMatrix).multiply(virtualCamera.matrixWorldInverse).multiply(mesh.matrixWorld);
    reflectorPlane.setFromNormalAndCoplanarPoint(normal, reflectorWorldPosition).applyMatrix4(virtualCamera.matrixWorldInverse);
    clipPlane.set(reflectorPlane.normal.x, reflectorPlane.normal.y, reflectorPlane.normal.z, reflectorPlane.constant);
    const pm = virtualCamera.projectionMatrix;
    q.x = (Math.sign(clipPlane.x) + pm.elements[8]) / pm.elements[0];
    q.y = (Math.sign(clipPlane.y) + pm.elements[9]) / pm.elements[5];
    q.z = -1.0;
    q.w = (1.0 + pm.elements[10]) / pm.elements[14];
    clipPlane.multiplyScalar(2.0 / clipPlane.dot(q));
    pm.elements[2] = clipPlane.x; pm.elements[6] = clipPlane.y; pm.elements[10] = clipPlane.z + 1.0 - 0.003; pm.elements[14] = clipPlane.w;
    mesh.visible = false;
    const cur = renderer.getRenderTarget();
    const sh = renderer.shadowMap.autoUpdate;
    renderer.shadowMap.autoUpdate = false;
    renderer.setRenderTarget(rt);
    renderer.state.buffers.depth.setMask(true);
    renderer.clear();
    renderer.render(scene, virtualCamera);
    renderer.shadowMap.autoUpdate = sh;
    renderer.setRenderTarget(cur);
    mesh.visible = true;
  }
  return { mesh, update, setSize, uniforms };
}
