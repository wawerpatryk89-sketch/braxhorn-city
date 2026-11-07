# OBÓZ TRENINGOWY — V2 PILOT / COMMON RUNTIME OWNER

Branch: `codex/oboz-przebudowa-pbr-20261105`

## Role
You own the first real V2 implementation. Do not create an Obóz-only solution. The runtime API, manifest semantics and downloader behavior must be reusable unchanged by Osiedle, Miasto Mistrzów and Miasto R1.

## Do not do
- do not cancel/restart an already running owner-requested legacy build;
- do not change visual design, geometry, materials, source quality or approved assets;
- do not use manual ZIP/file picker as production UX;
- do not start a new APK/Player build without owner command;
- do not implement PAD first. Prove remote Addressables/CDN path on Android first.

## Work order

### O1 — Inventory
Run the report-only architecture audit and record:
- current HEAD;
- Unity/Addressables version;
- current BuildPlayer scenes;
- current embedded content;
- Resources inventory;
- manual picker path;
- current baseline/update code;
- existing content-state handling.

### O2 — Common runtime API
Create one reusable runtime namespace/API for:
- initialization;
- catalog update check;
- exact download-size query;
- dependency predownload;
- progress/state/error reporting;
- region scene load/unload;
- cached/offline behavior;
- trial/previous/confirmed rollback state.

No Obóz-specific hard-coded scene logic inside the common layer.

### O3 — Remove legacy production picker
The existing file picker may remain only behind a DEV/RECOVERY define or editor-only path.
Normal user flow must not require ZIP selection.

### O4 — Obóz groups
Define:
- core-local (bootstrap only);
- shared-runtime dependencies actually used elsewhere;
- region-oboz content groups based on lifecycle/dependencies/update cadence.

Use Analyze/Build Layout. Do not invent MB thresholds.

### O5 — Baseline
Produce:
- remote catalog/hash;
- bundles;
- addressables_content_state.bin;
- build layout;
- duplicate-dependency report;
- scene GUID;
- runtimeId;
- SHA/bytes manifest.

No new Player unless owner orders it.

### O6 — Data-only update proof
Change one approved data/content item without runtime code change.
Build Update a Previous Build.
Record changed bundle names and bytes.
Do not call BuildPlayer.

### O7 — Android proof
Only after owner orders the pilot Player:
- clean install;
- automatic Obóz download;
- progress;
- interrupted network;
- restart/cache;
- offline cached load;
- data-only update;
- corrupt update rejection;
- rollback;
- unload/reload;
- peak RAM/load time.

## Exit criterion
Only after O1–O7 PASS can the common runtime be tagged as the reference implementation for other regions.
