# Braxhorn: isolated Unity cloud check

This folder verifies the Unity license and WebGL build toolchain. It is not a
migration of City and does not define the game's appearance. The original City
sources, models, animation choices, and production deployment are unchanged.

The GitHub Actions workflow runs only on `codex/unity-cloud-proof-20261028` or
when manually dispatched. It checks that the three required secrets are present,
validates license XML without displaying it, and invokes GameCI's pinned builder.

Unity 6000.0.58f2 creates a small scene through `CloudProofBuild.Build`. A rotating
cube and a touch/click button prove that the built C# code executes in WebGL.
The preview displays the source commit. `proof.json` also identifies the exact
source and Unity version. Unity creates its remaining default ProjectSettings
during first import. Built-in engine modules are the only project dependencies.

Only `build/WebGL` is included in the output artifact. License files and credentials
are never copied into project files or uploaded as artifacts. The workflow has
read-only repository permissions and does not deploy or push to `main`.

Success criteria: secrets validated, Unity license accepted, scripts compile,
WebGL build succeeds, downloaded build loads and its color button responds.
A green workflow by itself does not establish visual or touch-test success.
