# MIASTO MISTRZÓW — V2 MIGRATION TASK

Branch: `gpt/miasto-mistrzow-kasyno-t11-20261105`

## Dependency
Current visual/build work must not be overwritten. V2 work is infrastructure-only and starts from the latest branch head. Use the verified common runtime from the Obóz pilot.

## Do not do
- do not mix arena/casino visual redesign with V2 migration;
- do not modify owner-approved murals, glass, columns, belt showcase, parking geometry or materials as part of delivery work;
- do not create a separate Mistrzowie downloader;
- do not launch a Player build without owner command.

## Work order
1. Inventory current Player scene list and embedded world dependencies.
2. Audit Addressables/runtime files already present versus actually wired.
3. Build dependency graph for arena/casino/city assets and shared-runtime candidates.
4. Create region-miasto-mistrzow Addressables groups.
5. Reuse common V2 runtime exactly.
6. Create baseline + content state + catalog/hash + manifest.
7. Prove data-only update without BuildPlayer.
8. Android acceptance on owner command.
9. Report duplicate dependencies, transfer bytes, load time and peak memory.

## Exit criterion
PASS only after the same Android acceptance suite as Obóz, with no visual regressions against owner references.
