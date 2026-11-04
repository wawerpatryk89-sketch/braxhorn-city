# City #87 preparation failure — 2026-11-04

Base: `2aa14b7adb0c63046ed86fd3e14693f1b22163bb`.
Evidence: https://github.com/wawerpatryk89-sketch/braxhorn-city/actions/runs/41886960605/job/126774229233

The discovery API did not return the top-level CDN URL expected by the script. Android compilation was skipped. The fix removes discovery from build execution: use the exact versioned URLs published on the existing espresso asset page and the on-demand grinder page, with SHA-256 verified against downloaded GLBs. No replacement machine or scene was selected.

Sources:
- https://3dassets.dev/assets/bakery-and-cafe-lever-espresso-machine-9c0d50a4 — CDN 22110/v1; 224896 bytes; SHA-256 c3e09b2baa05eba3eb1fec2f460fd5b2c1fcc3da789694811fc591bea803db05
- https://3dassets.dev/assets/bakery-and-cafe-grinder-on-demand-4bcd3bea — CDN 22111/v1; 116500 bytes; SHA-256 1687e650e7890f2c403803cfe11fffc199f02cddf71ec16c8689887e91ef9edd

Private individual GLBs and named subtrees of the owner's equipment GLB retain priority, matching the Unity importer. Valid cached downloads work offline; invalid data fails before publication. Writes use a temporary file and rename. Both Android and WebGL preparation run eight regression tests before fetching. No workflow trigger changed.

Validation:
- Actual download and GLB validation passed for both pinned models.
- 8 café regression tests passed (mocked network and synthetic GLBs for fault cases).
- Existing Claude preparation/cache tests: 15 passed; island preparation: 5 passed; update/cache tools: 25 passed.
- 275 C# files parsed without syntax errors; city preflight passed. Python AST, workflow YAML and bash syntax checked. All 35 directly referenced preparation scripts exist.
- WebGL bridges and mobile shell template checks passed.

Not validated: Unity semantic compilation, Unity model rendering, APK creation, phone runtime. No Unity executable is available in this session. No build was started by this repair. Next authorized build must pass preparation, compilation and APK publication separately; do not treat these checks as a successful APK.
