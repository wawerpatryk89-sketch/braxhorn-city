# Accepted gym and tower city — isolated Unity 6 renovation

Owner authorization: combine the accepted mural/ring/locker/WC gym with the tower/metro city, then renovate Nutrition → NOVA → BRAXHORN TOWER and rooftop → harbour/beach. Premium assets only; report missing sources. No production publication.

## Owner rule: finish one location before starting the next

Complete and verify the whole current location before moving to another: architecture and finishes, required source equipment and detail, entrance/exit, collisions, lighting and relevant interactions. Nutrition is the entire shop; NOVA includes its full equipment and owner-approved music; BRAXHORN TOWER includes lobby/interior, physical lift, rooftop and the finished viewing/fighting terrace.

Progress images are work-in-progress, not completion. Provide real Unity screenshots and video for review. Do not mark a location complete or proceed to the next until outstanding defects are resolved and the owner accepts it. If sources, music approval, access or other required materials are missing, report the specific blocker, keep the location open and continue only useful work within that same location. Do not substitute unfinished or low-quality assets to bypass a blocker.

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
- Nutrition whole-shop implementation, real regeneration, visual/runtime verification and owner acceptance.
- NOVA source equipment audit and visual renovation; integrated owner-approved music still awaits actual runtime and phone audio-unlock validation.
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

## Owner-approved NOVA music — Industrial Pulse

The owner supplied `Industrial Pulse.mp3` and explicitly requested trimming its slow intro and installing it in NOVA. The source is preserved byte-for-byte at `asset-library/sources/audio/Industrial-Pulse.original.mp3` (SHA-256 `e373ea0c75c6d0dd5a1cdee6e481b97c11bf79ef0afd6253f2ead00834ae9455`). The club edit removes 27.70 seconds, with a 30 ms entry fade and a short end fade; runtime duration is approximately 179.9 seconds. `Cyber Agresor` remains unselected.

`unity-tools/nova-audio/prepare.py` generates the MP3 under StreamingAssets. The real Unity city attaches one venue AudioSource and an audio controller to the existing player. It receives the existing pause/sound Command messages without editing BraxGymPlayer. Music starts automatically when approaching NOVA: full level in the hall, quieter in the vestibule and a gentle doorway spill onto the nearby street. Once loaded, its timeline keeps looping when Brax leaves earshot; distance changes volume rather than stopping the party. Global sound/application pause still applies; the clip is disposed with the city scene. The original player, camera and controls are unchanged.

`unity-tools/nova-audio/check.mjs` requires real browser audio output measured through a WebAudio analyser, tests mute/pause, leaving/re-entering NOVA and scene unload, and captures actual engine audio. Its position cuts are diagnostic and cannot be called a continuous walkthrough. Adding this track does not complete or accept NOVA's visual renovation.

Owner clarification: no separate music start button, no silence at the club threshold. The automatic party should be gently audible outside the real NOVA entrance. This does not authorize advancing to a different renovation location.

## Integration linker failure and correction

Run `41074406699`, attempt 2, reached the Unity 6000.0.58f2 WebGL link step and failed with `undefined symbol: BXRoomState`. The gym diagnostics C# component had been integrated but its matching WebGL plugin had been omitted. Restore `Assets/Plugins/WebGL/BraxRoomDiagnostics.jslib` and its original `.meta` byte-for-byte from the accepted gym commit `b2279bd24f7ce1660160c6fc3af119e9820a32f9`; do not replace the player or silence diagnostics.

`unity-tools/check_webgl_bridges.py` detects absent or duplicate project-owned BX bridge providers before the long Unity build. This is source preflight only; the real Unity linker and runtime checks remain required. Superseded runs of this isolated TEST workflow are cancelled when a corrected commit arrives, so a known broken older build does not block its replacement. This concurrency group is specific to the gym/city renovation workflow.

The preflight reproduced the missing `BXRoomState` failure before restoration, then passed for all four project bridges afterwards. The restored plugin and metadata match source Git blob SHAs `6f76f85c8a2fa9d1663c78a29ed70c3372eaf5c2` and `a2cfcc9018cd094ca202e4571da470f3984e178e`. These results do not assert a successful Unity build.

## Owner-reported visual defect: buildings and kerbs

Acceptance requires stable building facades and kerbs during actual camera/player movement. Test the route from the accepted gym entrance past Nutrition and toward NOVA/Tower from close and oblique views. Capture real Unity footage, including stationary views and movement along both kerbs; screenshots or successful compilation alone cannot demonstrate absence of flicker.

Hypotheses remain unconfirmed for the combined build: overlapping/coplanar source and rebuilt geometry, thin edge/texture aliasing, and lighting/shadow instability. The integrated city authoring already separates the kerb and pavement top surfaces (`CITY_KERB_PAVING_SEPARATION`), but its runtime visual effect is unverified. Inspect the original reproduction before selecting geometry, camera, material or lighting corrections. Keep gym, player, estate and production outside this fix's scope.

## Nutrition: owner brief and functional acceptance

The owner reports shaker models and possibly supplement packaging in the asset Library. The available repository asset catalogs `catalog-20261029.json` and `intake-20261029-02.json` contain no matching records. The owner subsequently explicitly authorized creating missing premium assets. This resolves the source blocker for the shop through original BRAXHORN authoring; the earlier Library models were not retrieved. Do not interpret the absence of catalog records as proof that the owner's Library has no such models.

Finish the complete Nutrition shop: entrance and glazing, floors/walls/ceiling, a coherent BRAXHORN interior, product shelving, a counter, lighting and usable circulation. Display the verified shakers and supplement packages. Original BRAXHORN product labels may identify protein, creatine and related sports products; fit labels to the actual source packaging without replacing it with primitive stand-ins. Keep originals intact. Product labels and fixtures must remain stable without coplanar overlays/flicker.

The shop is a gameplay location for replenishing the fighter's energy, not just a display. Use the existing action control at a clearly identified counter to replenish energy, with feedback showing the actual resulting value. Integrate with a single authoritative fighter-energy owner; do not add an unrelated decorative meter or silently substitute health for energy. The inspected Unity baseline had no energy component. Preserve the protected player controls and animations. Pricing, currency, combat penalties and movement restrictions are not added by this shop feature.

Acceptance includes entering and leaving the finished shop, seeing the installed source products and legible labels, triggering regeneration from below-full energy and observing its actual state change, safe repeat use at full energy, no activation outside the interaction range or during pause, and coherent state across district transitions. Record real Unity screenshots and a walkthrough demonstrating the complete location and regeneration. Nutrition remains open and unaccepted until its source-asset, visual and gameplay requirements are verified; do not advance to NOVA/Tower while this shop is incomplete.

## Combined Unity compilation succeeded; mobile packaging recovery

Run `41084834041`, source `77b68fedfdf2dda2c6a76a2cbaed5a8fe4666bc4`, successfully completed the actual Unity 6000.0.58f2 WebGL build. Packaging then failed because `public/unity-gym/city-transport.js` was absent. The already compiled player and its generated chunk manifest were retained in artifact `12993319899`; actual build/source receipts are in artifact `12994767366`.

Restore only the real mobile shell's `index.html`, `city-transport.js` and template `release.json` from accepted gym source `b2279bd24f7ce1660160c6fc3af119e9820a32f9`. No historical game binaries or alternative engine runtime are restored. `gym-city-package-recovery.yml` verifies compressed and expanded Unity data hashes and actual compilation identity, then packages those existing compiled bytes with the preserved native gzip loader. Runtime checks target the actual compiled source SHA, separately recorded from the recovery workflow commit. Compilation is confirmed; runtime, visual and phone acceptance remain pending until those checks run.

## Original Nutrition implementation — not yet runtime validated

Original kit: shaker with moulded lid/spout/carry loop, whey tub, creatine jar, walnut/stone shelving bay, fluted counter, feature display island, suspended linear luminaire and bevelled limestone tile. `NutritionKit.glb` is 1,754,248 bytes, SHA-256 `46ab54d2f64cd4b437df7903b4374308eb2f01e7e3da759eb98861079ef84678`; source audit measured 23,788 triangles, valid finite normals/UVs and no degenerate triangles. The editable packed `.blend`, generators and original texture artwork are retained. Asset renders are authoring previews, not Unity gameplay or premium acceptance.

The whole existing shop footprint receives floors, mineral walls, ceiling, shopfront glazing, depth-tested branding and eight detailed shelving bays with 137 product instances. The existing Nutrition doorway stays at its source location. Original shop geometry is clipped only inside the measured renovation bounds; new collision includes floor, walls, shopfront, counter and islands. Local PBR luminaire shading and offline room contact AO use new shop-only materials. No global gym lighting, rig, animation or BraxGymPlayer source changes are made.

`BraxFighterEnergy` owns one 0..100 resource across scene loads and saves it under a new PlayerPrefs key. Prototype expenditure follows actual running distance (0.12 per metre) and confirmed training-bag hits (1.5 per hit); diagnostic travel is excluded. These are configurable prototype rates, not accepted game balance. This feature does not change speed, input, animations, damage, or ability availability. The counter replenishes the actual resource, while an in-Unity HUD and physical counter readout display that same state. Pause/range/full-energy guards and city/estate reconnection require runtime tests.

`unity-tools/nutrition/check.mjs` exercises the actual compiled shop with declared diagnostic position/energy fixtures. Named entrance/counter/kerb legs use real player movement and collision. Screenshots and MP4 are genuine Unity captures; they are silent, not a physical-phone test, and require human inspection for premium quality and flicker. The location is still unaccepted until all criteria and owner review pass. Mobile shell validation now runs before Unity compilation to catch absent loader files early.
