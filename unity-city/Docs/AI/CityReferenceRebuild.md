# BRAXHORN CITY — reference rebuild, 29 October 2026

## Source and integration boundary

Refreshed Unity source branch: `codex/unity-full-city-20261029`, commit `ba678928b2dcb2dff9e38bdb5306e090e76360cf`.
City work lives on `codex/city-reference-rebuild-20261029` in its own checkout. Other executors use separate worktrees. No Unity Library/Temp directory is shared.

The gym contractor's working tree has not been edited, exported over, or merged. Its finished integration commit has not yet been supplied. The existing source gym, doorway transform, controller body, rig, six original clips, shop and NOVA interiors are retained. Camera changes are a small integration patch in `BraxGymPlayer.cs`, explicitly requested by the user: complete vertical look range and stable planar movement at both poles.

The persistent authoring entry is `BraxCityRebuild.Apply`, called from `BraxCityBuild.AddArea` only for the city. Generated assets are disposable outputs. Original glTF, image files and materials are not overwritten.

The supplied `Braxhorn_City_Master(1).pdf` describes the eventual five-region game, including the station and future combat systems. This city-stage order remains the specific implementation boundary: eleven passive reserves, the metro descent/vestibule, preserved estate connection, and no active campaign. The future station is deliberately not labelled finished. The gym contractor's current annex was inspected read-only: x approximately -25.5..-18.6, z -3..8. The current city street footprint starts at z=11.6 and does not occupy that annex. Its finished integration commit still requires an explicit handoff; observing a work-in-progress commit is not an integration approval.

## Reference inspection

The complete attached 79.52 second recording was reviewed chronologically at 0.5 second intervals and at detailed selected views. It shows a mix of tall glass/metal towers and masonry fronts, recessed ground floors, generous paving slabs, dark street furniture and a detailed steel elevated railway. The strongest pedestrian reference is approximately 46–55 seconds. Its bloom, haze and glossy finish are not copied globally because the brief explicitly excludes those effects. The recording contains no palm, port or metro specification and is not a performance measurement of this project.

Baseline evidence comes from the actual existing Unity test build run `40855727591` and the original city export run `40840263799`. Source-only Three.js asset inspection is separately labelled and is never presented as a Unity game screenshot.

## Keep / rebuild / replace / missing

| Decision | Elements |
| --- | --- |
| Keep | Recognisable road and destination layout; original playable Brax, animations and touch directions; gym boundary and door ownership; shop/NOVA interiors and access; separate estate connection; actual mission anchor identities. |
| Rebuild | Street architecture as individual metre-scale storeys; exposed backs/returns/roofs; paving and kerbs; representative high-rise skyline; coordinated signal system; metro stair excavation and vestibule; waterfront/yard details. |
| Replace | Painted window substitutes on renovated fronts; unsuitable technical street props; the generic low-detail viaduct in the whole-district stage; primitive tree crowns after the replacement source has been reviewed. Source files remain archived. |
| Open asset review | Realistic broadleaf tree LODs and actual imported palms; comparison of all new materials in the Unity player; later product-specific artwork. |

## Asset provenance

- Poly Haven `modular_urban_apartments_facade`: original CC0 glTF 1K/2K, actual recessed windows, frames, doors/handles and cornices. Metric source bays are 3 by 3 metres.
- Poly Haven `modular_factory_facade`: original CC0 brick/industrial door and window modules, reserved for matching industrial parts of the district.
- Poly Haven `brick_wall_001`, `clean_asphalt`, `concrete_pavement_02`, `wooden_planks`: source PBR maps retained with official API metadata and verified file hashes.
- Source download audit run `40866427252`; small model inventories and original city bounds run `40867497981`; daylight source-module visual inspection run `40868345664`; expanded material archive run `40869502253`.
- Original user-supplied branding is under `Assets/CityRebuild/Brand`. `CityBrandSources.json` records the exact input file and SHA256. Logos and character appearance are not repainted or stretched. Dates/fight-event posters are deferred by the user's instruction. A small number of architectural advertisements is intended, not repeated billboard coverage of the district.

Only redundant tessellation in audited exactly planar wall panels is reduced. The optimizer verifies the 3 x 3 metre outline, rectangular aperture area and affine UV mapping before generating an equivalent surface at 0.5 metre sample spacing. Original window/door/cornice meshes are unchanged. No tree or facade is silently converted into a primitive placeholder.

## Stage and acceptance

`BraxCityRebuild.WholeDistrict` initially remains **false**. The first player candidate contains a complete representative architectural segment, a set-back tall tower, new street paving, signals, eleven passive reserves and a real metro descent. Remaining legacy areas are explicitly still under renovation. The whole-city rollout follows inspection of this real player candidate.

`city-rebuild.json` records the stage and rendered inventory. `CityRebuildSystems.json` contains all five main and six side reserves, their source anchors, position/direction, clear space, access, camera needs and preparation status. Technical anchors are not shown in the normal navigation menu.

Static lighting is saved into copies of the new city meshes using the isolated `Braxhorn/CityStaticPBR` shader; the gym's shared shaders, materials and global light are not changed. The build emits lighting sample and triangle statistics. This is not a claim that physical phone performance is accepted.

Required remaining evidence: actual comparable Unity views, walk through every rebuilt area, complete signal cycle, metro descent/return, collision and scene return checks, cold start/time to control, long session and background/resume; then physical iPhone/Android measurements with hardware/OS/browser/build/quality recorded. Desktop/WebKit automation is explicitly a desktop browser test.

Production deployment is outside this order. Each candidate must use a separate test preview. No main-domain publication is performed by this workflow.
