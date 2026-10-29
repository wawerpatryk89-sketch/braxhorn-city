# Estate migration — source lock (Unity 6)

Status: SOURCE INVENTORY / NOT YET IMPORTED. No visual PASS claimed.

Source of truth: `dev/osiedle/layout.js`, `dev/osiedle/level.js`, `dev/osiedle/world.js`, `dev/osiedle/vegetation.js`, `dev/osiedle/entrances.js` and original `dev/osiedle/assets/*.glb`.

Do not use the rejected rebuild as a source. Preserve geometry, original model files, placements, textures, entrance logic, scale and existing estate layout. No cosmetic upgrades in the migration pass. No modifications to gym, city, main, or production.

Verified source layout: bounds X [-114,114], Z [-102,99], spawn (0,1.7,52). Original residential placements: dom-01 dom_suburban (-28,27) width 17 yaw +90°; dom-02 dom_nowoczesny (27,28) width 14 yaw -90°; dom-03 willa_nowoczesna (-29,0) width 17 yaw +90°; dom-04 dom_z_wnetrzem (28,0) width 14 yaw -90°; dom-07 dom_wnetrze_zewn (-28,59) width 20 yaw 0°; dom-08 dom_nowoczesny (28,59) width 15 yaw -90°. Central villa (0,-38), 24x16. Facility centers: casino (-86,-42), nightclub (-86,14), police (86,-42), hospital (86,14), boxing (-86,64), octagon (86,64). Streets Wiśniowa, Bydgoska, Poznańska.

Unity 6 import contract:
1. Preserve and hash original GLB files; import to isolated Unity scene, not via generated primitive replacements.
2. Convert each original model using the same normalization algorithm as `level.js` (target width divided by original mesh bounds X), respecting original pivots and rotations. Validate glTF coordinate conversion before placing.
3. Port procedural world, vegetation and doors separately; no claim of 1:1 until their source geometry and behavior are replicated.
4. Keep original collision openings and walking routes; record visual comparisons at 15/30/45/60/75/90/100 percent.
5. Require actual Unity scene screenshot, traversable build and user sign-off before PASS.

Outstanding: inventory binary GLBs and textures, import model geometry, procedural world, vegetation, doors and colliders; compile Unity 6; compare side-by-side and test on device.
