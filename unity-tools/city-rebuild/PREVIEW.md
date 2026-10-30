# Isolated Unity preview

The existing Vercel project is `braxhorn-city` (`prj_qeJX3ynFW1b21OYRsZlXKChzSRDh`), owned by `team_z2Zm14RoeGXMXDd5Dorzotxj`. Its connected GitHub repository is `wawerpatryk89-sketch/braxhorn-city` (repository ID `1436397588`). These identities were read from the authenticated connector on 2026-10-29.

The normal source branch preview still builds the repository's existing `public` tree using `dev/tools/assemble-assets.cjs`. It is **not** evidence that the newly compiled Unity city is live. The new path below serves the actual reviewed `city-rebuild-player-<SHA>` artifact at the root of an independent deployment.

## Sequence

1. Complete `[city-build]` for an exact source commit, then `[city-qa]` against that artifact. Inspect the actual Unity screenshots and movement recording. The QA result distinguishes desktop software rendering from a physical phone.
2. Edit `preview-request.json`: set `status` to `requested`, enter the completed source and QA run IDs and full commit SHAs, set `visualReview` to `reviewed-for-test`, list the actual inspected PNG filenames, and record any remaining issues. The default request is deliberately pending.
3. Push this small request on `codex/city-reference-rebuild-20261029` with `[city-preview]` in the commit message. This job verifies both completed jobs, hashes every downloaded candidate file, checks matching actual Unity QA, and verifies that the reviewed images exist.
4. CI creates a new root commit on `preview/city-rebuild-<first 12 characters of the Unity source SHA>`. The branch contains only the reviewed player and deployment metadata. It uses an ordinary create-only push and refuses an existing branch. It never edits `main`, the gym branch or the city source branch.
5. The existing Vercel Git integration normally creates a preview for the new branch. Obtain the **exact** deployment whose Git commit matches `publication.json` and whose target is preview (`target: null` in some Vercel responses). If no matching deployment exists, `vercel-preview-request.json` contains a connector request pinned to the new branch **and** exact publication SHA. Its omitted target means preview; do not add a production target, promote it or attach a production domain.
6. Check the deployment's `preview-publication.json` and `release.json` identities, the index's actual Unity loader and transport files, then start the live Unity player and check `BraxhornState.build` against the source SHA. A `READY` deployment or an HTTP 200 alone does not establish that the game starts.

## Deployment protection

The current project has Vercel Authentication enabled for `all_except_custom_domains`. The user's authenticated account can inspect the exact preview. If a usable phone review link is needed, use a scoped share link for this particular immutable deployment with a deliberate expiry, through the authenticated connector. Do not disable project-wide protection or expose an automation bypass credential.

The project and deployment reads work with the connector's existing account context. On 2026-10-29 the same IDs with an explicit `teamId` parameter returned 403/404, while hostname/project reads without that redundant parameter returned the verified project and team. Correct scope using the returned project identity; do not change accounts or infer authority from an error.

## What the receipt means

`city-rebuild-preview-publication-<request SHA>` is a small artifact containing exact source/QA identities, inspected image hashes, the publication commit and optional connector request. Its `liveUnityVerified` remains false until a separate live check is actually performed. The original Unity `release.json` and all player bytes are preserved; only `vercel.json` is adapted to serve the artifact root, and clearly named preview metadata is added. `deployment-files.json` records those final hosting bytes separately from the original candidate integrity manifest.

The preview is for review of the current candidate. Final city acceptance, physical phone performance and any pending gym integration remain explicitly open. Transmission chunks preserve the original Unity data bytes; they do not implement district streaming.
