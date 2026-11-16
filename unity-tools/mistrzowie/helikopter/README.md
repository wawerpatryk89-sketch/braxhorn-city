# BRAXHORN EC135 — the helicopter on the BRAXHORN TOWER

Owner 16.11.2026 22:00: *„Masz tutaj helikopter … Przemaluj go na nasz kolor, z napisem naszym, z naszym herbem. I go postaw
na dachu i zrób wejście. I żeby był funkcjonalny, żeby można było latać.”* 22:38: *„helikopter słabo widoczny jest, napis”* →
the letters 30 cm over 2.6 m, a brighter satin gold.

**Source model:** "EC 135" by **GRIP420** — https://sketchfab.com/3d-models/ec-135-ec30f802c6ac43a8843948fbadb674d2 —
licensed **CC-BY 4.0** (credit the author). Changes: the ADAC livery painted over (black, the BRAXHORN gold cheatline,
BRAXHORN on the tail boom, the owner's coin as the emblem), the rescue crew and the stretcher removed, the parts baked
static, the rotors on their own pivots, metres, the nose to +z, textures sized for the phone.

The repository is public: the owner's file and the result live only encrypted, in
`unity-city/Prywatne/braxhorn-mistrzowie-helikopter-20261116/` (`zrodlo/ec_135.glb` = his original,
`Helikopter/braxhorn_ec135.glb` + `_tekstury/braxhorn_ec135/*.png` = the result). In CI `install_owner_media.py` opens it
into `Assets/Source/Mistrzowie/Helikopter/` (never committed); `BraxChampionsWiezowce.Helikopter` puts it on the heliport
and `BraxChampionsHelikopterTextureImport` compresses its textures.

Rebuild (from a decrypted pack; run with `-I`, the scripts find their neighbours themselves):

    python3 -I export.py zrodlo/ec_135.glb <the owner's sheet arkusz_szyldy_monety_logo.png> \
        ../fonts/Montserrat-SemiBold-latin.ttf <out folder>

- `glb.py` — a minimal GLB reader; `bake.py` — the skinned parts baked by their joint matrices, scaled by the real rotor
  diameter 10.20 m, the origin on the deck under the main hub; `livery.py` — the old paint found in the texture and
  painted piano black (its baked shading kept), then the gold, the letters and the emblem projected from the sides onto
  the texels where the fuselage is (positions / normals rasterised into the UV space); `export.py` — writes the GLB with
  external textures (`__lin` = data maps, linear); `heli_inspect.py` — lists the source's joints and meshes.
