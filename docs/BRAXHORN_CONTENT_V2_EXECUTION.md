# BRAXHORN CITY — CONTENT DELIVERY V2 EXECUTION PLAN

Status: ACTIVE
Owner decision: 7.11.2026
Scope: all four production region branches.

## Objective

Stop treating every region change as a new monolithic APK. Migrate BRAXHORN to one professional content-delivery architecture:

Core Player -> Addressables -> shared-runtime + independent region content -> automatic download/update -> Android proof.

## Global rules

1. Existing owner-requested builds already running are allowed to finish. Do not cancel or restart them for V2.
2. After a region enters V2 migration, do not start another monolithic "normal release" build for that region unless the owner explicitly orders an emergency legacy build.
3. Never change approved visuals, geometry, UVs, materials, scale, animation appearance or source quality as part of V2 migration.
4. One worker per region branch at a time. Do not mix regions in one commit.
5. Common V2 runtime is authored once and reused. Do not create four incompatible downloaders or manifest formats.
6. "Addressables installed" is not PASS. PASS requires Android device proof.
7. All quality findings remain report-only unless they are real build/runtime integrity failures.

## Workstreams

### WS0 — COMMON RUNTIME / PILOT
Owner branch: `codex/oboz-przebudowa-pbr-20261105`

Deliver:
- one shared Content Manager API;
- automatic remote catalog check/update;
- byte-accurate download size;
- automatic dependency download;
- progress/state UI contract;
- staging + validation + activation;
- confirmed/previous/trial rollback state;
- cached offline startup;
- async scene load/unload;
- no production file picker / manual ZIP flow;
- Android pilot proof.

The implementation created in WS0 becomes the reference runtime for all other regions.

### WS1 — OSIEDLE MIGRATION
Branch: `claude/osiedle-42-poprawka`

Starts only after the WS0 common runtime API is stable.

Deliver:
- Addressables groups for region-osiedle + shared-runtime dependencies;
- baseline + content state;
- use the same common runtime;
- automatic remote delivery;
- update-without-Player proof;
- Android proof.

### WS2 — MIASTO MISTRZÓW MIGRATION
Branch: `gpt/miasto-mistrzow-kasyno-t11-20261105`

Starts after the currently active visual/build work is not being overwritten and after WS0 runtime API is stable.

Deliver the same V2 contract as WS1, with no visual redesign during migration.

### WS3 — MIASTO R1 MIGRATION
Branch: `claude/miasto-opus-20261031`

R1 currently has Addressables dependency aligned but lacks the full shared content runtime layer.

Deliver:
- import the approved shared V2 runtime;
- create region-miasto-r1 groups;
- explicit shared-runtime dependencies;
- baseline/content-state/update workflow;
- Android proof.

## Required phase gates per region

### GATE A — INVENTORY
Required evidence:
- exact branch HEAD;
- Unity version;
- Addressables version;
- actual Player scene list;
- current BuildPlayer call sites;
- Resources inventory;
- current APK/AAB bytes if available;
- biggest dependencies / duplication report;
- region entry scene GUID.

No world changes.

### GATE B — GROUP DESIGN
Required evidence:
- explicit group list;
- shared dependencies identified from Analyze/Build Layout;
- no large world content left accidentally in Core/Resources;
- no invented bundle-size target.

### GATE C — BASELINE
Required outputs:
- remote catalog + hash;
- bundle inventory;
- `addressables_content_state.bin`;
- source commit;
- Unity/Addressables versions;
- runtime compatibility id;
- scene GUIDs;
- Build Layout / duplicate-dependency report.

### GATE D — AUTOMATIC DELIVERY
Required behavior:
- no manual file picker in production path;
- automatic catalog/index check;
- download size in bytes;
- automatic dependency download;
- progress;
- error handling;
- cache;
- trial/previous/confirmed rollback state.

### GATE E — DATA-ONLY UPDATE
Required proof:
- modify one approved data/content item without runtime code change;
- run content update from previous baseline;
- report changed bundles and transfer bytes;
- do not rebuild Player;
- Android downloads and uses the update.

### GATE F — ANDROID ACCEPTANCE
PASS requires real device evidence:
1. clean install/start;
2. first region download;
3. progress;
4. interrupted network;
5. retry/resume-safe behavior;
6. region load;
7. restart using cached content;
8. offline cached launch;
9. content update without new Player where compatible;
10. invalid/corrupt update rejected;
11. previous version remains recoverable;
12. unload/reload region;
13. peak memory/load-time report.

## CI target state

Each region ends with:
- `source-preflight`
- `content-baseline`
- `content-update`
- `player-release`

No push-triggered long Player build.
Data-only content updates never call BuildPlayer.
Player release only for runtime changes or explicit owner order.

## Evidence format

Every contractor reports:
FACTS -> DIAGNOSIS -> DECISION -> ACTION -> PROOF -> REMAINING GAPS.

No words "done", "ready", "professional" or "production" without the gate evidence above.
