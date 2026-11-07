# BRAXHORN CITY — VERIFIED LARGE MOBILE GAME DELIVERY ARCHITECTURE

Date: 2026-11-07  
Scope: Unity 6 / Android mobile-first  
Status: binding engineering reference for all BRAXHORN regions.

This document is intentionally based on current official Unity / Android / Activision documentation. Do not add arbitrary bundle-size thresholds or claim a feature is production-ready without Android proof.

## 1. Verified platform facts

### Unity 6.0
- Unity 6000.0 officially releases `com.unity.addressables@2.7.6`.
- Unity 6000.0 officially releases `com.unity.addressables.android@1.0.9`.
- Addressables is Unity's recommended layer for organizing and asynchronously loading local or remote assets and their dependencies.
- Addressables for Android maps Addressables groups to Google Play Asset Delivery asset packs and supports Texture Compression Format Targeting.

Official:
- https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.addressables.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.addressables.android.html

### Google Play Asset Delivery
PAD has three delivery modes:
- `install-time`: available at first app launch and contributes to store install size;
- `fast-follow`: starts automatically after installation and does not block opening the app;
- `on-demand`: requested by the running game when needed.

Google Play hosts the asset packs, performs compression and supports automatic asset updates / delta patching for Play-distributed releases.

Current Google Play maximum compressed download limits are platform limits, not BRAXHORN targets:
- base module: 500 MB;
- individual asset pack: 1.5 GB;
- base + install-time packs cumulative: 4 GB;
- fast-follow/on-demand packs cumulative: 30 GB;
- maximum total: 34 GB.

Official:
- https://developer.android.com/guide/playcore/asset-delivery
- https://support.google.com/googleplay/android-developer/answer/9859372

### Large-game UX pattern
Call of Duty: Mobile officially uses an in-app Download Manager for content that is not required in the base Android app. Activision currently documents separate downloads such as a Battle Royale map and HD audio. This proves the user-facing pattern, not COD's private internal implementation.

Official:
- https://support.activision.com/cod-mobile/articles/call-of-duty-mobile-updates

## 2. BRAXHORN target architecture

There is one logical content architecture and two delivery backends.

### A. Development / sideload / non-Play
```
BRAXHORN Core Player
        |
        +--> Addressables remote catalog
                |
                +--> HTTPS CDN / Unity CCD
                        |
                        +--> shared content
                        +--> region-miasto-r1
                        +--> region-osiedle
                        +--> region-oboz
                        +--> region-miasto-mistrzow
                        +--> future regions
```

### B. Google Play production
```
Android App Bundle
        |
        +--> small base module / bootstrap
        |
        +--> Addressables for Android
                |
                +--> PAD install-time asset packs
                +--> PAD fast-follow asset packs
                +--> PAD on-demand asset packs
```

Do not build two independent content systems. Addressables is the common logical layer; CDN/CCD and PAD are delivery backends.

## 3. Core Player

The Core Player contains only content required to start the application safely:
- executable code / IL2CPP;
- bootstrap scene;
- essential UI;
- save/profile/settings;
- Content Manager;
- download/update status UI;
- error handling;
- minimal assets truly required before any region is downloaded.

Large worlds, interiors, vegetation, high-resolution regional textures and heavy regional audio do not belong in Core merely because the Unity project references them.

For PAD, remember that Unity's generated base module includes executable content and the first build scene. Therefore build index 0 must remain deliberately lightweight.

Official:
- https://docs.unity3d.com/2023.2/Documentation/Manual/android-asset-packs-in-unity.html
- https://developer.android.com/google/play/play-as-you-download/best-practices

## 4. Content ownership

Use explicit logical ownership:
- `core-local`;
- `shared-runtime`;
- `region-miasto-r1`;
- `region-osiedle`;
- `region-oboz`;
- `region-miasto-mistrzow`;
- later regions by the same naming rule.

An asset shared by multiple regions must not be left as an implicit dependency that gets copied into several bundles. Assign shared dependencies explicitly to a shared Addressables group when Analyze / Build Layout shows duplication.

Unity explicitly documents that implicit dependencies can be duplicated into multiple AssetBundles and loaded into memory as separate copies. Addressables Analyze provides duplicate-dependency checks.

Official:
- https://docs.unity.com/en-us/engine/6000.0/manual/assets-and-media/assets-managing-runtime/assets-avoid-duplication
- Addressables Analyze rule: `CheckBundleDupeDependencies`

## 5. Bundle grouping rule

There is no invented universal MB threshold.

Group by:
1. lifecycle — assets loaded and unloaded together;
2. dependency graph — minimize implicit duplicates;
3. update frequency — frequently changed content should not force unrelated stable content to redownload;
4. runtime memory — avoid monolithic bundles that trap unused content in memory;
5. measured download/load behavior.

Use Addressables packing modes deliberately:
- `PackTogether`: content with the same lifecycle/update cadence;
- `PackSeparately`: independent entries/folders that should be independently rebuilt/loaded;
- `PackTogetherByLabel`: only when labels genuinely express lifecycle/update boundaries.

Android's Unity memory guidance explicitly warns against overly large AssetBundles because loading small content from a giant bundle can increase peak memory and prevent unused data from being released while any referenced asset remains alive.

Official:
- https://developer.android.com/games/engines/unity/unity-reduce-memory
- Addressables `BundledAssetGroupSchema.BundleMode` API documentation

## 6. Baseline build

A production baseline is created once for a compatible Player runtime.

Required outputs:
- remote content catalog;
- catalog hash;
- Addressables bundles;
- `addressables_content_state.bin`;
- source commit;
- Unity version;
- Addressables version;
- platform/build target;
- region identifiers and scene GUIDs;
- build-layout / duplicate-dependency report.

The state file must be retained for future content updates.

A remote catalog is mandatory for Addressables content updates to an already released application.

Official:
- Addressables `RemoteCatalogBuildPath` / `RemoteCatalogLoadPath`
- `ContentUpdateScript.GetContentStateDataPath`
- `AddressablesContentState`

## 7. Content update build

For data-only changes compatible with the current runtime:
1. restore the exact previous approved `addressables_content_state.bin`;
2. keep group update-restriction settings consistent with the baseline;
3. run content-update restriction checks;
4. build with `ContentUpdateScript.BuildContentUpdate` / Update a Previous Build;
5. compare Build Layout with the previous release;
6. publish only the new catalog/hash and newly generated changed bundles;
7. do not rebuild the Player merely because data changed.

Changes to runtime code, packages, PlayerSettings or other binary compatibility inputs require a new Player baseline.

Unity explicitly warns not to change group Update Restriction settings between the baseline and content-update build without performing a full build.

Official:
- Addressables Content Update Build Settings
- `ContentUpdateScript.BuildContentUpdate`

## 8. CDN / CCD release workflow

For non-Play delivery use HTTPS remote hosting. Unity CCD is one supported option; another professional CDN can be used.

Recommended release flow:
1. build Android Addressables content;
2. upload to development environment;
3. create immutable release;
4. run device QA against the development release;
5. promote the same release to staging;
6. run acceptance tests;
7. promote the same release to production;
8. move a stable badge / release pointer only after acceptance.

Unity CCD officially recommends separate buckets per build target across environments such as development, staging and production, and supports immutable releases plus movable badges.

Official:
- https://docs.unity.com/en-us/ccd
- https://docs.unity.com/en-us/ccd/faq
- https://docs.unity.com/en-us/ccd/use-addressables-with-ccd

## 9. Runtime startup sequence — CDN / remote Addressables

At application start, before region content is loaded:
1. initialize Addressables;
2. check remote catalogs with `Addressables.CheckForCatalogUpdates`;
3. if updates exist, update with `Addressables.UpdateCatalogs`;
4. do this before loading affected bundles because catalog update conflicts are harder once old bundles are in memory;
5. query required bytes using `Addressables.GetDownloadSizeAsync(regionKeyOrLabel)`;
6. if bytes > 0, show exact download size and user-visible progress;
7. pre-download dependencies using `Addressables.DownloadDependenciesAsync`;
8. monitor operation progress;
9. load the region scene with `Addressables.LoadSceneAsync(..., activateOnLoad:false)`;
10. activate only when required dependencies and scene load are ready;
11. when leaving, use `Addressables.UnloadSceneAsync` and release retained operation handles;
12. clean obsolete bundle cache only after the new catalog is active and no old content is required.

Unity states that `UpdateCatalogs` blocks other Addressables requests while it is running, so catalog updates belong at a controlled startup/update boundary rather than during active region loading.

Official:
- `Addressables.CheckForCatalogUpdates`
- `Addressables.UpdateCatalogs`
- `Addressables.GetDownloadSizeAsync`
- `Addressables.DownloadDependenciesAsync`
- `Addressables.LoadSceneAsync`
- `Addressables.UnloadSceneAsync`
- `Addressables.CleanBundleCache`

## 10. Runtime startup sequence — Google Play PAD

For production Google Play:
1. build an Android App Bundle, not the release APK workflow;
2. install `com.unity.addressables.android@1.0.9` for Unity 6000.0;
3. map Addressables groups to PAD asset packs;
4. keep only true first-launch essentials in base/install-time;
5. use fast-follow for content expected immediately after installation but not required to open the app;
6. use on-demand for regions entered later;
7. expose download state/progress in the game's own UI;
8. handle `WaitingForWifi` and mobile-data consent;
9. never assume a fast-follow/on-demand pack always exists at a permanent filesystem path;
10. handle the short window where an app update is installed but the corresponding asset patch has not finished applying.

Google states that PAD fast-follow/on-demand files can be moved or removed between sessions and that applications must handle assets temporarily being unavailable during updates.

Official:
- https://developer.android.com/guide/playcore/asset-delivery
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Android.AndroidAssetPacks.html

## 11. Texture delivery

Do not ship one universal uncompressed/fallback texture set when Google Play can target GPU formats.

For Google Play production, evaluate Texture Compression Format Targeting:
- ASTC for capable modern devices;
- ETC2 as a broad modern Android fallback.

Google Play currently reports ASTC support on >80% and ETC2 on >95% of Play devices and can serve only the texture set appropriate for the device from the App Bundle.

This is delivery-format optimization. It does not authorize changing approved source art, UVs, colors or visual design.

Official:
- https://developer.android.com/guide/playcore/asset-delivery/texture-compression

## 12. Memory and runtime rules

Measure peak memory, not only memory after unloading.

Android/Unity guidance notes that Unity memory pools can stay expanded after a peak allocation, so avoiding spikes is more important than assuming cleanup will return all pages to the OS.

Required:
- modular bundles by lifecycle;
- release Addressables handles;
- unload region scenes;
- no unnecessary Read/Write textures;
- mipmaps only where useful;
- platform texture compression;
- detect duplicated assets;
- profile with Unity tools plus Android `meminfo` / Perfetto / device telemetry.

Official:
- https://developer.android.com/games/engines/unity/unity-reduce-memory
- https://developer.android.com/games/optimize/memory-monitoring

## 13. Google Play size limits are ceilings, not goals

Do not design BRAXHORN to fill Google's maximum limits.

Use the platform limits only as hard publishing constraints. BRAXHORN's actual Core Player target must be chosen from measured install conversion, startup time, device storage, patch frequency and our real Build Report — not invented in advance.

## 14. CI/CD contract

Every region follows the same four pipeline roles:

### source-preflight
Fast checks only:
- source availability;
- GUID/meta integrity;
- script/static validation;
- no expensive full Player build.

### content-baseline
- exact Unity + package versions;
- build Addressables baseline;
- save content state;
- Analyze duplicate dependencies;
- save Build Layout;
- publish to DEV content environment.

### content-update
- restore exact approved state;
- run update restriction check;
- build Update a Previous Build;
- report exactly which bundles and bytes changed;
- publish to DEV;
- no Player rebuild for data-only update.

### player-release
Only when:
- runtime/code/package/Player configuration changed; or
- owner explicitly requests a new Player.

For Google Play production this means AAB/PAD, not treating a monolithic sideload APK as the final large-game distribution architecture.

## 15. Mandatory acceptance tests

A region is not production-ready until a real Android device verifies:
1. clean install / first startup;
2. accurate required download size;
3. visible download progress;
4. interrupted network and resume/retry behavior;
5. insufficient-storage handling;
6. successful region load;
7. app restart with cached content;
8. offline launch using already cached content;
9. catalog/content update without Player rebuild where runtime is compatible;
10. corrupted/incomplete content is rejected;
11. old stable content remains usable until replacement is valid;
12. asynchronous scene unload/reload without leaked Addressables handles;
13. peak RAM/VRAM and load time recorded;
14. no unintended Player/content or cross-region duplication.

## 16. Migration order for BRAXHORN

Do not convert all worlds blindly in one destructive refactor.

Use one shared implementation and migrate region by region:

1. finish the common Content Manager / update UI contract;
2. use Obóz as the first pilot because its Addressables/content-state foundation already exists;
3. remove manual ZIP/file-picker behavior from the production path;
4. prove remote automatic download/update on Android;
5. migrate Osiedle to the same runtime;
6. migrate Miasto Mistrzów to the same runtime;
7. migrate Miasto R1 after its Addressables baseline/group structure is established;
8. after CDN path is stable, add the Unity 6000.0 Addressables for Android package and map the same groups to PAD for Google Play;
9. only then call the architecture production-ready.

No branch may create its own incompatible downloader, manifest format or update semantics.
