# Accepted gym and tower city — isolated Unity 6 renovation

Owner authorization: combine the accepted mural/ring/locker/WC gym with the tower/metro city, then renovate Nutrition → NOVA → BRAXHORN TOWER and rooftop → harbour/beach. Premium assets only; report missing sources. No production publication.

## Pinned integration sources
- Base / Three runtime removal / durable original imports: `17f12b57ba38c23242bdcc825d27e0ba51ba8aba`.
- Accepted gym authoring, textures and reproduction tools: `b2279bd24f7ce1660160c6fc3af119e9820a32f9`.
- Latest tower-city authoring: `e583356db9ad08f1ce050d85df9209c8a97de754`.
- Shared `BraxGymPlayer`, hero refinements, bridge and animations remain byte-identical to the integration base.
- The latest tower source compiled, but run 41062626236 failed opening the apothecary lift during runtime QA. This remains an unresolved integration gate, not an accepted feature.

## Composition
`BraxGymBuild.BuildRenovation` saves BraxhornCity and BraxhornEstate through the existing district registry. City rebuild runs on the imported city only; the exact accepted gym finishing and equipment then run on the imported gym. No waterfront pruning is performed. Estate is included from the existing integration baseline, without new estate renovation changes.

## Separate estate work
Estate source baseline: branch `codex/estate-premium-renovation-20261030`, commit `80b4004f078269e59c598ada5e0920406dddc8ec`. Independent estate work must not change the shared player, gym, city architecture, generated outputs or this workflow. Integrate tested estate-only changes later; never overwrite the whole Unity project from that branch.

## Verification
Workflow `gym-city-renovation.yml` retrieves SHA-verified durable source imports, audited city-kit geometry/materials, reproduces the accepted gym assets, compiles Unity 6000.0.58f2, and executes the existing real gym/door/city-return tests. Reports, video and compiled player are retained as TEST artifacts. Green compilation is not visual acceptance or physical-phone validation.

## Remaining work — not implemented or accepted
- Combined build/runtime/visual verification and city flicker diagnosis.
- Nutrition details and material pass.
- NOVA source equipment audit and actual licensed music file, positional audio and phone audio-unlock validation.
- Tower interior and fighting terrace finish; physical elevator round trips.
- Source yacht/jet-ski/harbour/beach asset audit and installation. Library descriptions alone do not prove binary availability.
- Films for owner review, then physical phone performance checks.

## Owner-reported Library sources (not yet accessible in this session)

Root: `/Braxhorn city/`. These are reported paths, not verified imports or license receipts.

- NOVA: `Asset Library/3D Models/Entertainment/Nightclub/dj_mixer.glb`, `disco_ball_with_colored_lights.glb`; `Asset Library/3D Models/Clubs/Lighting/laser_light.glb`.
- Watercraft under `Asset Library/3D Models/Watercraft/`: `super-yacht02.glb`, `yacht.glb`, `frickies_yacht.glb`, `2002_grand_banks.glb`, `bateau_riva_boat.glb`, `jetski_collection.glb`.
- Music: `Cyber Agresor.m4a` and `Cyber Agresor(1).m4a`. Neither is approved for NOVA; do not select automatically.
- Preserve every original. Validate binary availability, mesh/material import, source license, scale and mobile budgets before installation. This session's tools cannot retrieve arbitrary binary Library-folder entries from a pathname alone.

## First build attempts

- Commit `172952ab1e90e325c128e5d0c0cfa2c7f94de516`: upstream bag source hash changed; source integrity gate stopped before Unity.
- Commit `646fb20880bb7d3926f6d464226ae6645a3fa2e3`: recovered the reviewed packed Blender derivative, verified its SHA-256 and exported byte-identical BagSwing/BagMount GLBs. Do not relax these checks to accept upstream replacements.
- Run `41074406699`, first attempt: Unity package-cache failure for `com.unity.nuget.mono-cecil` before scene build. Retried the failed job on a fresh runner. No compiled or visually accepted combined build is asserted by these source changes.
