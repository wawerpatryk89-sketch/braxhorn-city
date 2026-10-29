# Original city and estate migration — isolated test branch

Branch: `codex/unity-full-city-20261029`. Production source read-only: `260a5897472d42d099766da48e708859bba6c3dc`.
Actual full export run: 40840263799. City: 724 meshes, 521280 triangles, 151 source boxes. Estate: 553 meshes, 2427187 triangles, 1026 source boxes and 24 original door assemblies.

The exporter executes the original browser scene and retains original vertex positions, UVs, material textures, baked vertex colours, ground lightmap and estate door hinges. The player remains the original Brax rig with original clips. Native texture preparation externalizes identical source image bytes, retains dimensions, and configures ETC2 GPU compression for the mobile WebGL build. Geometry/accessor bytes are hashed before/after repacking.

Entry method: `BraxGymBuild.BuildCity`. Two scenes: `BraxhornCity` (original premium gym plus original district) and `BraxhornEstate`. The original GLTF X mirror is also applied to collision data, source points and door transforms. Negative-height floor partitions preserve the NOVA cellar. No fallback building meshes. Scene transitions unload the preceding scene rather than displaying both maps at once. Existing gym-only build remains `BraxGymBuild.Build`.

Scope of this version: environment exploration, original Brax movement/run/jump/punch/kick, gym training, original estate doors, map navigation and city/estate transitions.

NOT complete gameplay parity: estate NPC schedules/traffic and combat AI, bosses, story/economy and Nyxa selection still require their own port. Do not call this a complete game migration, a physical iPhone performance pass, or a production release. Runtime tests and screenshots must be inspected before any player publication. No workflow in this branch is authorized to write main.
