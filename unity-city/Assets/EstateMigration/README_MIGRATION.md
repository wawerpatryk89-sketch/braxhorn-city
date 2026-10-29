# BRAXHORN CITY / R2 OSIEDLE / UNITY 6

**STATUS: SOURCE PORT INCOMPLETE. NOT APPROVED.**

This is a separate Unity 6 project in the isolated Git branch
`work/unity6-estate-migration-20261029`; no shared city, gym or main updates.
Original Three.js estate remains source of truth. No automatic visual upgrades.

## Proven original input

- **Procedural original**: `EstateProceduralAndDoors.glb`, 8,094,192 bytes, produced by running the same world/civic/arena/entrance builders; no replacement grey cubes.
- **Raw placements**: `EstateSourceManifest.json` 21 GLB instances, 24 door records, 1,013 collision AABBs, walkable surfaces and 30 vegetation placements.
- **Original source**: `dev/osiedle/` and `dev/models/`.
- **Original character**: `public/assets/chars/brax.glb.gz`, loaded without replacing its rig; Brax texture is a lossless PNG conversion of the original WebP using Pillow.
- **Unity target**: 6000.0.58f2, glTFast importer, texture and meshopt dependencies.

## Actual launch

The folder `unity-city/` is the separate Unity project. Open it using Unity 6.
Select `BRAXHORN → Osiedle - zbuduj scene 1:1`. Run the generated
`Assets/EstateMigration/Scenes/EstateMigration.unity` in Play Mode.

To build WebGL with an activated Unity Editor:
`-batchmode -quit -projectPath unity-city -executeMethod Braxhorn.EstateMigration.Editor.EstateSceneBuilder.BuildWebGL`.

The source world is loaded from `Assets/StreamingAssets/Estate/`.
The importer reproduces original house rotations and normalizations
from `dev/osiedle/level.js` using the actual mesh bounds and target widths.
It uses source collision AABBs and individual original mesh colliders, and
animates original door leaf hinges. Touch movement and camera are included.

## Blockers - NO PASS until resolved

The exact original GLBs listed in the source are missing from both
`public/osiedle/assets` and `dev/osiedle/assets`:
- `gameready_casino_scene.glb` (original source manifest SHA256 starts a4212a70)
- `policestation.glb` (original SHA256 starts 1fe5504f)
- `park_tree.glb` (original SHA256 starts 55a46aa3)

Do not replace originals with new models. Original door-opening mesh modifications
for these missing source objects are likewise not verified. A previous Three.js
QA report does not replace a real Unity test.

**A successful GLB export or code commit is NOT a Unity compile, actual playable
Unity WebGL link, smartphone FPS measurement, or 1:1 visual match.**
Before completion, verify mesh importer behavior, UVs, tangent spaces, PBR
materials, collisions, avatar animations and walk routes. Capture real Unity
screenshots at 15/30/45/60/75/90/100% of *visually verified progress*.
Only user can approve the result.

## Build CI

GitHub Actions `unity-estate-webgl.yml` checks prerequisites and performs
WebGL compilation only when authorized Unity license secrets exist. When no
license is configured the job explicitly reports the blocker rather than
declaring success or publishing a fake preview.
