# OSIEDLE — V2 MIGRATION TASK

Branch: `claude/osiedle-42-poprawka`

## Dependency
Do not invent a separate downloader. Wait for the common V2 runtime API from the Obóz pilot to stabilize, then port that exact runtime contract.

## Do not do
- do not redesign Osiedle;
- do not change approved textures/models/materials because of migration;
- do not start a monolithic legacy release after migration begins unless owner explicitly orders emergency legacy output;
- do not duplicate common runtime code with Osiedle-specific semantics.

## Work order
1. Inventory current Player scenes, embedded content, Resources and Addressables state.
2. Run Analyze/Build Layout and identify shared-runtime dependencies.
3. Import/reference the common V2 runtime without forking semantics.
4. Create region-osiedle Addressables groups by lifecycle/dependency/update cadence.
5. Build immutable baseline + content state + catalog/hash + manifest.
6. Prove data-only content update without BuildPlayer.
7. Wire automatic catalog/download/progress/cache/rollback.
8. Android acceptance only on owner command.
9. Report Player bytes, region bytes, changed bundles, duplicate dependencies, load time and peak memory.

## Exit criterion
PASS only after Android proves first download, cached restart, offline cached load, compatible content update and rollback.
