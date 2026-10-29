# R1 city continuation — 29 October 2026

## Ownership and verified baseline

User explicitly assigns this executor ONLY R1 city in Unity 6. Gym and R2 estate have other executors. Work branch: `codex/city-r1-continuation-20261029`; baseline `58db7d5f6a25601c8120025f3153d0be8a5498a2`. Do not merge to main, change production, integrate newer gym/R2 assets, alter brand or substitute character assets.

Unity 6000.0.58f2, Built-in shaders, glTFast 6.20.0. Existing GameCI run 40946452434 compiled the baseline successfully. This is compilation evidence, not visual or functional acceptance. Source has WholeDistrict=true, 18 street buildings, nine elevator controllers; their physical routes still require tests. Metro remains stairs and vestibule, not the full station required by Master V3. Older CityReferenceRebuild.md describes an earlier two-building phase and is not current implementation evidence.

Assets remain the actual curated facade/PBR archives referenced by the baseline workflow; no new asset purchases, substitutions, or pipeline change. Protected gym annex and existing estate connection remain unchanged.

## First implementation and validation

BraxCityElevator: reopen at an occupied threshold, suspend on game/application pause, stop carrying a player who deliberately reset away, recover cabin/passenger to the departure landing if physical floor transport fails. Normal travel remains continuous physical movement. Recovery reports an error and must not be counted as a successful ride.

The sole shared-player addition is a read-only IsPaused property. No movement, animation, camera, rig, materials or gym logic is rewritten.

`check_lift.mjs` checks actual compiled Unity at the pinned SHA. Its initial placement is explicitly a QA fixture on the tower sidewalk, not evidence of a walk from the gym. Thereafter it opens the real street door, walks through the lobby into the car, rides up, exits onto the roof deck, returns to the car, rides down and walks back through the same street entrance. New-build coverage also checks game pause in transit. Screenshots and failures are preserved by CI. Physical iPhone remains NOT TESTED.

## Acceptance checkpoints

User requires a playable Vercel test preview at each 15% checkpoint (15, 30, 45, 60, 75, 90, final 100), with screenshots. Percentage describes work reviewed within this R1 continuation, never the entire five-region master. No 15% completion claimed before an inspected, playable first candidate.

First checkpoint: functional access and return for the existing flagship tower, actual street view and clear defects reported. Subsequent work: remaining building/roof routes, usable rooftop arenas, full metro station, remaining R1 mission spaces and grounds, then complete physical walkthrough and device review. Inspect each candidate before advancing visual work beyond its checkpoint.

Vercel project: braxhorn-city / prj_qeJX3ynFW1b21OYRsZlXKChzSRDh. Ordinary source-branch Vercel builds still serve the older public tree; use a dedicated preview branch containing the exact Unity WebGL artifact. Do not present a source-only branch preview as the new Unity version.

## Ceiling obstruction correction

Baseline run 40953176475 passed the flagship physical up/down route at source 58db7d5f. This original baseline used direct action commands, so does not validate touch-button visibility. Its actual roof screenshot was inspected.

Eight additional shafts intersected a full collidable lobby ceiling at y=3.76. Replace that ceiling with four slabs around the shared shaft footprint. The build now checks each additional passenger shaft for static collision obstructions. This is an editor geometry gate, not a substitute for physical riding and device testing. Existing architecture materials and all 18 footprints are retained.

R1 packaging also adds ZJEDŹ to visible contextual actions; the previous HTML hid the roof descent button. New browser tests click the visible Action button. Static UI regression passed; new-player runtime results remain pending.
