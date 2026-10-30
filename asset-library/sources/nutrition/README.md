# BRAXHORN Nutrition — original authored retail kit

Owner authorization, 30 October 2026: create missing premium Nutrition shop products and equipment instead of waiting for unavailable Library binaries. This authorizes the original models here; it does not assert that the earlier Library shaker or supplement files were retrieved.

`BRAXHORN-Nutrition-authored-source.blend` preserves the original editable model source with packed original texture artwork. The eight named parts share their own local origin so Unity can instantiate them independently: WheyTub, CreatineJar, Shaker, ShelvingBay, Counter, DisplayIsland, LinearPendant and FloorTile. This is an asset library, not a playable game scene.

The Unity import is `unity-city/Assets/Nutrition/Models/NutritionKit.glb`, with its SHA-256/authoring receipt alongside. Original label and PBR map images are in `Assets/Nutrition/Textures`. No owner-provided model was replaced or overwritten. Labels use original BRAXHORN typography; font software is not embedded or redistributed. DejaVu font attribution is recorded in `authored-textures.json`.

To reproduce, install the project's Pillow/numpy versions and fonts-dejavu-core, run `python3 unity-tools/nutrition/prepare_textures.py`, then run Blender with `--background --factory-startup --python unity-tools/nutrition/create_models.py`. Recorded source was authored in Blender 4.3.2. Audit the actual result with `python3 unity-tools/nutrition/audit_assets.py`; a different exporter version can change bytes and requires an updated receipt and Unity import validation.

The authoring preview is an actual render of these models, not Unity gameplay. Premium visual acceptance requires the real Unity 6 shop build, runtime screenshots/video and owner review. None of the source receipts claims phone performance or owner acceptance.
