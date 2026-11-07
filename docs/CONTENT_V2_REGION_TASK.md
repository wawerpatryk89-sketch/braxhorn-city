# MIASTO R1 — V2 MIGRATION TASK

Branch: `claude/miasto-opus-20261031`

## Current fact
Addressables 2.7.6 dependency has been added, but R1 does not yet have the complete shared runtime/content build layer. Do not claim V2 because the package exists.

## Dependency
Reuse the verified common V2 runtime from the Obóz pilot.

## Do not do
- do not change bridge/road/store/parking visuals as part of migration;
- do not create replacement content to reduce size;
- do not rebuild the whole Player for data-only tests;
- do not create R1-specific update semantics.

## Work order
1. Inventory exact Player scene list, Resources, BuildPlayer call sites and current APK bytes.
2. Establish Addressables settings and import the common content runtime/build layer.
3. Run Analyze/Build Layout and identify shared-runtime dependencies.
4. Create region-miasto-r1 groups based on lifecycle/dependency/update cadence.
5. Move region data out of Core without changing source appearance.
6. Build baseline + content state + catalog/hash + manifest.
7. Prove data-only update without BuildPlayer.
8. Android acceptance on owner command.
9. Report Player/content duplication, transfer bytes, load time and peak RAM.

## Exit criterion
PASS only after R1 loads through the common V2 runtime and passes the same Android acceptance suite as the pilot.
