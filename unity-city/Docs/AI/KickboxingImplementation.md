# BRAXHORN Kickboxing — room and annex test

## Scope and baseline

The test branch continues production `26e5ef339b09df6c1077e9968200660ec89cdacf` and the existing Unity source tree `ba678928b2dcb2dff9e38bdb5306e090e76360cf`. It keeps the current city WebGL entry and `ios-stream-01` transport as its packaging templates. It does not publish production.

Unity remains **6000.0.58f2**, Built-in rendering, WebGL ETC2. Original city/estate export artifacts are reused, not exported or redesigned. `BraxGymPlayer`, `BraxCityBuild`, `BraxCityWorld`, `BraxCityDoor`, hero refinements, character preparation and all six original animations remain unchanged. The room-only finishing passes execute after the existing connected city is attached.

The owner explicitly authorized an annex in the later scope correction. The training hall retains its floor area. The annex occupies x[-25.5,-18], z[-3,8], with a 3.1 m interior ceiling. The existing street entrance remains at its original location. The reviewed city collision manifest has no solid city obstacle intersecting the annex footprint.

## Room layout

The measured plan is `KickboxingLayout.svg`. From the original entrance, the bags and long live mirror are on the left, the open graphite sparring mat in the foreground/centre, and the original raised ring at the back/right. A small reception sits beside the entrance. The billboard has its mounting structure; artwork installation remains pending explicit owner approval.

The right-wall portal leads through an enclosed dogleg corridor to separate locker and WC doors. The changing room includes eight steel lockers, a bench and coat hooks. The toilet room includes imported ceramic fixtures, a real mirror, dispenser, paper holder, closed walls and ceiling. Doors reuse the original city interaction component.

Architecture verification enumerates 15,120 sight segments from 560 hall viewpoints to 27 points in the WC bay, excluding door leaves as occluders. The builder fails if any is clear. Runtime diagnostics separately perform actual Physics.Linecast checks from the entrance, mat, ring and bags while all three backroom doors are open. These rays consider only the full opaque boundaries registered on layer 11; broad ring colliders, door leaves and furnishings cannot count as privacy evidence. The 12 runtime sample rays supplement the broader geometric sweep and do not replace it.

## Selected assets

Existing Library gym packs were inspected first. Their low-poly bags and simplified fixtures did not meet the requested realism and were not used as final replacements.

| Asset | Source and author | Use and budget |
|---|---|---|
| Brick Wall 10 | [Poly Haven](https://polyhaven.com/a/brick_wall_10), Dimitrios Savva | Interior dark brick; 2K albedo/normal; 1K runtime control maps |
| Fabric Leather 01 | [Poly Haven](https://polyhaven.com/a/fabric_leather_01), Rob Tuytel | Selected leather source maps, 1K |
| Fine Grained Wood | [Poly Haven](https://polyhaven.com/a/fine_grained_wood), Rob Tuytel | Bench and reception worktop; 1K; 0.6 m repeat; grain follows the board |
| Rubber Tiles | [Poly Haven](https://polyhaven.com/a/rubber_tiles), Amal Kumar | Graphite rubber albedo, normal and roughness; 1K; 2 m world-sized repeat |
| Hanging Industrial Lamp | [Poly Haven](https://polyhaven.com/a/hanging_industrial_lamp), Kuutti Siitonen | Actual manufactured pendant mesh and 1K maps |
| Boxing gloves and bag | [Innerscene](https://www.innerscene.com/tools/library/3d-parts/boxing-gloves-and-bag-e3d766bc) | CC0 chain mesh and leather normal source for the reviewed reinforced bag derivative |
| Compact WC | [Innerscene](https://www.innerscene.com/tools/library/3d-parts/compact-wc-close-coupled-toilet-84214efd) | Actual rounded ceramic toilet model |
| Basin pedestal sink cabinet | [Innerscene](https://www.innerscene.com/tools/library/3d-parts/basin-pedestal-sink-cabinet-81678fc4) | Actual basin and cabinet, correctly grounded |
| BRAXHORN mark and wordmark | Existing project `public/assets/logo` | Original owner artwork, no AI reconstruction of the logo |
| Ring, hall shell and original entrance | Existing Unity gym export | Retained and selectively finished; mixed meshes are clipped rather than deleted wholesale |

Poly Haven assets use [CC0](https://polyhaven.com/license); the selected Innerscene sources use its [CC0 library terms](https://www.innerscene.com/tools/library/terms). No purchase or paid pack was made. Powered by Poly Haven. Download receipts include original URLs, authors, license URLs and hashes. The bag source is pinned to SHA-256 `70676b234ba42882e05f1dc31c42b5c248c046904b586eab18991080b016df8b`.

The bag has six leather panels, paired fine stitches, reinforced ends, piping, four webbing straps, metal eyelets and continuous real chain geometry. BagSwing has 16,932 triangles and seven shared material slots; the static mount has 1,040 triangles and two slots. Four instances share the meshes and textures. Body dimensions are approximately 0.55 by 1.8 m, with 0.444 m measured floor clearance. The original two-axis damped pendulum remains responsible for directional impact response; individual links are not simulated.

## Rendering and memory

The main mirror uses a real planar camera and oblique clip plane, includes the moving character layer, and excludes all mirror surfaces from its own render. Its target is 768×384 at up to 24 Hz, only when visible from the front and within 42 m. The separate WC mirror is 384×384 at up to 12 Hz within 9 m. Both restore render state and release their owned resources when disabled.

Neutral local lights illuminate only the room layer. The original global character/city lighting is retained. New surface control maps are imported compressed PNGs, not serialized uncompressed Texture2D assets. Obsolete disabled gym mesh/filter references are removed after the finishing pass. The original character and city geometry/texture preparation are not reduced or replaced.

The first actual runtime review found insufficient surface definition, old branding overlays and repeated apron lettering. The next finishing pass removes those source prints, gives the original ring a graphite woven canvas and black fabric apron with controlled single brand placements, and uses the reviewed rubber material at its measured scale. Neutral room-only indirect fill and bag task lights reveal the brick and leather. Small shared static contact masks ground the ring, reception and backroom fittings. World lettering uses depth testing so signs do not show through walls. The billboard mount is 18 by 6 m, matching the reviewed flat artwork's 3:1 ratio; the artwork is still not installed before owner approval.

`externalize_models.py` moves the original Sink, Toilet and BagSwing PNG bytes into native Unity texture assets. It verifies the complete glTF graph, unchanged geometry bytes and unchanged decoded image pixels. The dedicated postprocessor compresses only these new room textures with ETC2; it does not modify city or character settings. Its memory estimates are theoretical until the build report and runtime measurements confirm the actual imported formats.

`package.py` preserves the current native gzip algorithm, 65,536-byte maximum input chunks, sequential 32 MiB transport parts, exact expanded length and one Unity output allocation. It regenerates the manifest for the new compiled data and versions the transport URL. It never restores the earlier large JS inflation path.

## Validation contract

`unity-kickboxing-reference-test.yml` compiles the actual connected Unity WebGL player and runs `check.mjs`. The runner records actual screenshots and a WebM walkthrough; no generated imagery is used as runtime evidence. Failed route legs remain marked as failures and any review-camera repositioning is declared.

The acceptance route covers startup/reload, movement and camera, real CDP two-touch input, run/jump/punch/kick, physical ring stairs, bag impact and damping, live reflection, all backroom doors, room access, WC privacy, fixture collision, city exit and physical return. Reports retain frame timings and available memory counters.

At approximately 1 FPS in the first instrumented software-rendered run, fixed 45-second wall-time limits expired before long walks reached their destinations. Route budgets now account for physical distance and measured simulation progress, while unchanged movement, collision-stagnation checks, no-frame-progress checks and a 30-minute total limit remain enforced. This change allows functional traversal tests on the slow server; it is not a performance pass. The extra `00-matched-before-view.png` uses the exact original comparison view; `04b-mirror-character-detail.png` records a physical approach between the bags for a clearer reflection inspection.

**Desktop Chromium/SwiftShader evidence is not a physical iPhone test.** The required minimum 30 FPS on the target phone remains unverified until measured on that device. Billboard approval and visual inspection of the assembled Unity scene are separate gates; compilation alone is not visual acceptance.

## Test-only publication

`publish_test.py` can verify the completed player/review artifacts and create a new `deployment/braxhorn-kickboxing-<sourceSHA12>` branch containing only the tested `public/unity-gym` replacement. It never updates `main`. The exact source SHA, run ID, hashes, continuous walkthrough evidence and original QA results must agree. The final preview URL is reported only after the hosting deployment and actual live files are verified.
