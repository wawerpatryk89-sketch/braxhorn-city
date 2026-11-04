# Camp #9 Texture2D load failure — 2026-11-04

Base: `3c67436acde4a1b23a5c7345a2046eeff86131fe`.
Evidence: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41883412619/job/126761438416

The log confirms clean_asphalt/Color.jpg was imported repeatedly, including a final 46.59-second import, before LoadAssetAtPath<Texture2D> returned null in BraxObozMaterials.Map. This was not evidence of a missing download. The old code configured textureType and Android compression but left textureShape unspecified; public maps also had no camp preprocessing rule and received their intended settings only while building the scene.

The patch explicitly sets Texture2D for material maps and applies the same public PBR settings in OnPreprocessTexture and material authoring. It retains 4096 pixels, ASTC 4x4, quality 90, repeat/trilinear sampling and all source bytes, GUIDs and scene layout. Postprocessor version advances to invalidate stale importer results. Failure diagnostics now include actual shape and main asset type. Errors are not suppressed.

Root-cause confidence: unspecified shape/late import configuration is the supported hypothesis, NOT an engine-reproduced conclusion. The old log does not record the actual main asset type. Confirmation requires Unity 6000.0.58f2 with Android selected.

Validation completed:
- All 69 existing camp Python tests passed, including material delivery checksum/archive tests, release identity and APK preservation.
- 275 C# files parsed without syntax errors. Python AST, workflow YAML and bash syntax checked. All 11 directly referenced preparation scripts exist. git diff --check passed.
- No Unity executable is available locally. No semantic C# compilation, clean import, Play Mode, APK or phone test has been performed.

Required original-reproduction check on next authorized run:
1. Restore verified public-materials.json inputs. Run the existing ReviewCampLife job with a clean Library, then with a compatible restored cache.
2. Inspect the 12 public maps (clean_asphalt, concrete_pavers, Grass001; Color/NormalGL/Roughness/AmbientOcclusion). Each must load as Texture2D; only Color is sRGB and NormalGL is NormalMap. Android must retain 4096/ASTC_4x4/quality 90.
3. Run BraxObozEnvironment.Prepare twice. Both runs must retain material textures; the second must not reimport maps merely to change the same settings.
4. Complete existing navigation, Play Mode and pose checks, then APK compile and publication; verify separately on Android.

No workflow/request file was changed and no build was triggered by this repair.
