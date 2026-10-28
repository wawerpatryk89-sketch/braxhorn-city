// fight clips for Mixamo people (raw Mixamo animations, same compact format as people/clips.json.gz)
const extra = [
  ['Walking', '/extra/Walking.fbx'], ['Running', '/extra/Running.fbx'], ['Big Hit To Head', '/extra/Big%20Hit%20To%20Head.fbx'], ['Center Block', '/extra/Center%20Block.fbx'], ['Dodging', '/extra/Dodging.fbx'],
  ['Body Jab Cross', '/extra2/Body%20Jab%20Cross.fbx'], ['Dying', '/extra2/Dying.fbx'], ['Getting Up', '/extra2/Getting%20Up.fbx'], ['Illegal Elbow Punch', '/extra2/Illegal%20Elbow%20Punch.fbx'], ['Illegal Knee', '/extra2/Illegal%20Knee.fbx'],
  ['Martelo Do Chau', '/extra2/Martelo%20Do%20Chau.fbx'], ['Mma Kick (1) (2)', '/extra2/Mma%20Kick%20(1)%20(2).fbx'], ['Standing Melee Attack 360 High', '/extra2/Standing%20Melee%20Attack%20360%20High.fbx'],
  ['Standing Melee Attack 360 Low', '/extra2/Standing%20Melee%20Attack%20360%20Low.fbx'], ['Standing Melee Attack Downward', '/extra2/Standing%20Melee%20Attack%20Downward.fbx'],
  ['Standing Melee Attack Kick Ver. 2', '/extra2/Standing%20Melee%20Attack%20Kick%20Ver.%202.fbx'], ['Standing Melee Combo Attack Ver. 3', '/extra2/Standing%20Melee%20Combo%20Attack%20Ver.%203.fbx'],
  ['Standing React Large From Right', '/extra2/Standing%20React%20Large%20From%20Right.fbx'],
];
for (const [n, u] of extra) { if (S.clips[n] && S.animObjs[n]) continue; const o = await new FBXLoaderClass().loadAsync(u); const c = o.animations[0]; c.name = n; for (const t of c.tracks) t.name = t.name.replace(/^mixamorig\d+/, 'mixamorig'); S.clips[n] = c; S.animObjs[n] = o; }
const RM = new Set(['Kicking', 'Kicking (2)', 'Medium Step Forward (1)', 'Long Step Forward', 'Step Backward', 'Flying Back Death', 'Dodging', 'Shoulder Throw, Aggressor', 'Shoulder Throw, Victim', 'Dying', 'Getting Up']);
const NAMES = ['Idle', 'Walking', 'Running', 'Boxing', 'Boxing (2)', 'Jab Cross (1)', 'Hook', 'Mma Kick (1)', 'Kicking', 'Kicking (2)', 'Taunt', 'Warming Up', 'Medium Step Forward (1)', 'Long Step Forward', 'Step Backward',
  'Head Hit', 'Head Hit (2)', 'Hit To Body', 'Big Hit To Head', 'Flying Back Death', 'Kip Up (1)', 'Dodging', 'Center Block', 'Jump', 'Shoulder Throw, Victim',
  'Illegal Knee', 'Illegal Elbow Punch', 'Body Jab Cross', 'Martelo Do Chau', 'Mma Kick (1) (2)', 'Standing Melee Attack Downward', 'Standing Melee Attack 360 High', 'Standing Melee Combo Attack Ver. 3',
  'Standing Melee Attack 360 Low', 'Standing Melee Attack Kick Ver. 2', 'Standing React Large From Right', 'Dying', 'Getting Up'];
const out = {}, miss = [];
const FPS = 24;
for (const n of NAMES) {
  const clip = S.clips[n]; if (!clip) { miss.push(n); continue; }
  const tr = []; let hipsY0 = 0;
  for (const t of clip.tracks) {
    const bone = t.name.split('.')[0], prop = t.name.split('.').pop();
    if (/Hand(Index|Middle|Ring|Pinky|Thumb)|_End|HeadTop/.test(bone)) continue;
    if (!(prop === 'quaternion' || (prop === 'position' && bone === 'mixamorigHips'))) continue;
    const it = t.createInterpolant(); const N = Math.max(2, Math.round(clip.duration * FPS) + 1);
    const times = [], vals = [];
    for (let i = 0; i < N; i++) { const tt = Math.min(clip.duration, i / FPS); times.push(+tt.toFixed(3)); const r = it.evaluate(tt); for (let k = 0; k < r.length; k++) vals.push(r[k]); }
    if (prop === 'position') {
      hipsY0 = vals[1];
      const x0 = vals[0], z0 = vals[2];
      if (n === 'Walking' || n === 'Running') { const x1 = vals[vals.length - 3], z1 = vals[vals.length - 1]; for (let i = 0; i < N; i++) { const a = times[i] / clip.duration; vals[i * 3] -= x0 + (x1 - x0) * a; vals[i * 3 + 2] -= z0 + (z1 - z0) * a; } }
      else if (RM.has(n)) { for (let i = 0; i < N; i++) { vals[i * 3] = 0; vals[i * 3 + 2] = 0; } } // root motion is applied from the trajectory meta
      else { for (let i = 0; i < N; i++) { vals[i * 3] -= x0; vals[i * 3 + 2] -= z0; } }
    } else {
      for (let i = 4; i < vals.length; i += 4) { const d = vals[i] * vals[i - 4] + vals[i + 1] * vals[i - 3] + vals[i + 2] * vals[i - 2] + vals[i + 3] * vals[i - 1]; if (d < 0) for (let k = 0; k < 4; k++) vals[i + k] = -vals[i + k]; }
    }
    tr.push({ n: t.name, t: times, v: vals.map(x => +x.toFixed(prop === 'position' ? 2 : 4)) });
  }
  let rest = 0; const ao = S.animObjs[n]; if (ao) { ao.updateMatrixWorld(true); ao.traverse(o => { if (o.isBone && /Hips$/.test(o.name) && !rest) { const r = (ao.userData.rest || {})[o.name]; rest = r ? r.p.y : o.position.y; } }); }
  out[n] = { d: +clip.duration.toFixed(3), hipsY0: +hipsY0.toFixed(2), rest: +rest.toFixed(2), tracks: tr };
}
// preview: a Mixamo person in a few fight poses
const obj = await new FBXLoaderClass().loadAsync('/people/Arissa.fbx');
obj.traverse(o => { if (o.isBone) o.name = o.name.replace(/^mixamorig\d+/, 'mixamorig'); });
const R = new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true }); R.setSize(1200, 400); R.outputColorSpace = THREE.SRGBColorSpace;
const scene = new THREE.Scene(); scene.background = new THREE.Color(0x404850); scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 2.5)); scene.add(obj);
const cam = new THREE.PerspectiveCamera(40, 3, 1, 5000); cam.position.set(250, 120, 420); cam.lookAt(0, 90, 0);
const mixer = new THREE.AnimationMixer(obj);
const imgs = [];
for (const [n, t] of [['Boxing', 0.5], ['Jab Cross (1)', 0.7], ['Mma Kick (1)', 0.6], ['Head Hit', 0.3], ['Standing Melee Attack Downward', 0.8]]) {
  const a = mixer.clipAction(S.clips[n]); a.reset().play(); mixer.setTime(t); obj.updateMatrixWorld(true); R.render(scene, cam);
  imgs.push({ __img: R.domElement.toDataURL('image/jpeg', 0.8), name: 'pf_' + imgs.length + '.jpg' }); a.stop();
}
const json = JSON.stringify(out);
const gz = await new Response(new Blob([json]).stream().pipeThrough(new CompressionStream('gzip'))).arrayBuffer();
const u = new Uint8Array(gz); let s = ''; for (let i = 0; i < u.length; i += 32768) s += String.fromCharCode.apply(null, u.subarray(i, i + 32768));
return { miss, n: Object.keys(out).length, gz: gz.byteLength, rest: Object.fromEntries(Object.entries(out).map(([k, v]) => [k, v.rest])), file: { __bin: btoa(s), path: '/home/claude/work/assets/people/fight.json.gz' }, imgs };
