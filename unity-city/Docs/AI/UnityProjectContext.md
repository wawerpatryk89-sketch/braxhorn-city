# Unity Gym: source and validation context

## Scope

This is the isolated Unity gym under `unity-city/`, not the browser city in `dev/` or `public/game.js`. Work for premium polish and camera-relative controls uses `codex/unity-gym-polish-controls-20261029`, based on `63075caa51f89b057774e7c484cac3e2d454177e`. Do not merge or push to `main`; publish only a reviewed test build under `/unity-gym/` on the test branch preview.

## Confirmed project

- Unity 6000.0.58f2; built-in rendering with custom surface/vertex-lit shaders.
- glTFast 6.20.0. No Unity MCP bridge in the package manifest; editor actions are performed by the existing GameCI build in GitHub Actions.
- Legacy `Animation` clips from the original Brax rig, driven by `BraxGymPlayer`.
- Input is browser pointer/keyboard events -> `BraxhornBridge.jslib` -> `BraxGymPlayer`. Left joystick moves; right pointer area orbits the camera. Camera orientation is never driven by player facing.
- glTFast mirrors glTF X; physics, bag pivots and original spawn use that same conversion.
- `BraxGymBuild.Build` imports the original gym, constructs original collision volumes and saves a generated scene under `Assets/PlayableGenerated/`. That folder and player output are generated, not authored scene changes.
- Original export: Actions run 40787817505, artifact `braxhorn-city-original-gym-a4a395c93a3ee20eae36ab5e4aee52e1220f2d3c`.
- Source character: `public/assets/chars/brax.glb.gz` and `public/assets/tex/brax_*`. Existing geometry, skeleton, UVs and branding remain the source of truth.
- Selected library model: original Poly Haven Sofa 02 already stored under `asset-library/sources/sofa_02/`; do not substitute random low-poly models.

## Integration and invariants

`prepare_playable.py`, `premium_prepare.py`, and `premium_refinements.py` prepare only generated asset data. They must not rewrite C#, shader, or browser source during a build. `BraxGymBuild` calls `BraxGymPremium.Apply` and `BraxHeroRefinements.Apply` explicitly.

Original material identities `BX_<id>_baked` must survive `AssetDatabase.CreateAsset`. Otherwise role-based material selection silently upgrades zero materials. A zero count is now a build error. Normal and roughness data use explicit normal/linear import; geometry and texture provenance are recorded in the generated reports.

## Validation

`.github/workflows/unity-gym-polish-test.yml` is read-only and constrained to the test branch. It compiles WebGL, checks that original assets/authored source were not modified by preparation, and runs `unity-tools/check_premium.mjs` with real Chromium WebGL2 and touch emulation. Review the resulting screenshots before issuing the separate publication request.

The prior premium baseline compiled but its browser test failed at a camera-yaw assertion because the telemetry contained axes from the previous frame. Its build log also reported zero upgraded original materials. Neither baseline is a visual PASS. Physical iPhone/Safari and Android device testing is distinct from Chromium touch emulation and must be reported separately.

## Estate work — 2026-11-04

The sections above describe the October gym task. The current estate repair starts from `codex/osiedle-integracja-final-patryk-20261104` at `87fcd04830e3786cb219575cc892610678ca6c8b` and is isolated on `codex/osiedle-drogi-klub-kierowca-20261104`. Read [the estate change and validation report](OsiedleNaprawy-20261104.md) for scope, source provenance, checks and unresolved device validation.

The estate uses Legacy `Animation` on the owner's Patryk skeleton. `EnteringCar` is now consumed by the player car controller. Actual steering-wheel geometry supplies two hand targets; IK rotates joints and preserves bone lengths. The requested showroom stair setback, road/roundabout changes and the owner's complete club booths are authored by editor generators, not by editing a generated scene. Local lighting must account for both Unity probes and the custom CityStaticPBR sun/ambient properties.

Private club GLB files are delivered through `import_club_assets.py` as encrypted source data and imported only for the estate. No production private key is added to git. The final estate generator aggregates required checks and throws on failure instead of treating a missing feature as a warning. Added Unity regression checks have not been executed in this environment. Local evidence is 17 import tests, 14 validator tests and a parser/scope check over 250 C# files, not a Unity compile or Android result.

Current repository `AGENTS.md` requires a separate explicit owner order before Unity/build/test dispatch. Commit and push alone do not authorize it. The region workflow allows this branch only with `rejon=osiedle` and remains manual-only. No build, release or device test was performed for this change.
