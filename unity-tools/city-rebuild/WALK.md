# Continuous city walkthrough

`walk.mjs` is a separate, deliberately longer acceptance run. It does not replace or delay the first image/control QA job. Run it only after obtaining and inspecting the actual Unity candidate:

```sh
node unity-tools/city-rebuild/walk.mjs
```

It uses the same pinned Playwright 1.56.1 and Chromium runtime as `check.mjs`. `PLAYER_ROOT` defaults to `unity-preview`; `EXPECTED_BUILD_SHA` can require an exact Unity source commit. `WALK_OUT` defaults to `city-walk-review`. `WALK_PASSES=2` repeats the complete route to expose differences on a revisited city; the default is one circuit. `WALK_ROUTE` may select a revised route file; the report hashes its exact bytes.

The planned 87 checkpoints cover about 1,070 metres. They are sourced from the original exported collision/point manifest, the actual NOVA opening/stair geometry in `dev/src/interiors.js`, and `CityRebuildSystems.json`. They have **not** been physically validated merely by writing this route. The old viaduct requires the far sidewalk at Z=34.6; the route avoids the closed near sidewalk. The cellar route uses the real narrow landing at Z=60.9, and the delivery approach goes around the existing pallet collision.

The run begins at the ordinary gym spawn. The only accepted game calls are `MoveInput`, `LookInput`, door/scene interaction and the walk/run toggle. It never calls `ReviewView`, `TravelPoint`, reset or an arbitrary transform setter. A blocked waypoint, unexpected floor, fall or position discontinuity stops the run and preserves the precise failure. It does not skip an inaccessible region and claim success.

Outputs are `walk-report.json`, frame/position telemetry, the actual input command trace, checkpoint PNGs, browser errors and a continuous `continuous-walk.webm`. Natural scene loads to the estate and back remain in the recording. Water views pause for 20 simulation seconds on the promenade and pier. All checkpoint intervals are also recorded in wall-clock video seconds.

The browser is headless desktop Chromium using SwiftShader with a touch-sized 915×412 viewport. Its timings are diagnostic desktop observations. They are not a physical phone performance or thermal/memory acceptance result. The actual video and close-up images still require visual inspection.
