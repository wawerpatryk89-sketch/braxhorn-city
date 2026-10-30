# R2 segment — official CC0 source copies / scene acceptance pending

The first Unity build (5a51a11507c3c60d2965fbf0351bc73f6fb6079a, Actions 41079518782) compiled and recorded real WebGL, but visual review FAILED: source-export stucco normals appeared mottled and the approach lost its joint pattern. It is not an accepted 15% checkpoint.

The correction uses the EXACT named sets in the owner's requested library, obtained from their official CC0 publishers. This does not claim access to the private ChatGPT Library, nor a match to unrecorded owner-upload archive hashes.

- Poly Haven White Stucco 02: https://polyhaven.com/a/white_stucco_02 ; CC0 https://polyhaven.com/license
  - white_stucco_02_diff_4k.jpg: 94e56a2173851458eab45c7d54ae024b850e7deeb33059dad9fc836c8d3069e7
  - white_stucco_02_nor_gl_4k.exr: a91d368af8d7d9072dcdd5c0da84af50ac2f25e46c68703ec26358c83de1b9f8
  - white_stucco_02_rough_4k.jpg: 060ce3a82aeb97a7afaad14e91859ea418cf41d82eda2b3158e62f9d6719f6f7
- ambientCG PavingStones151: https://ambientcg.com/a/PavingStones151 ; CC0 https://docs.ambientcg.com/license/
  - Official PavingStones151_2K-JPG.zip: 41965f7604eda1de8866c68682815fa5c908bdc60e855a1ca3ec0991f0c00b7e
  - All ZIP CRCs passed. Color, NormalGL, Roughness and AmbientOcclusion are imported; executable Blender/USD content is not executed.
  - Publisher metadata specifies a 540 cm square; Unity repeat is 5.4 m.

prepare_segment.py checks every source hash before conversion; preserves full source bytes in r2-source-archive; produces 1K–2K copies, linear normals, nonmetal/smoothness packing, mipmaps and WebGL ETC2 compression. Generated source and binding reports do not set visual approval to true.

Only original dom-04 renderers 312, 321, 323, 327 and the clipped source-X 7..21 / Z -2..2 approach within renderer 275 are affected. Original collision meshes, player, rig, input and other houses remain unchanged by this pass.

Not imported: premium window/door GLBs, new planting, road archives. No full renovation acceptance.
