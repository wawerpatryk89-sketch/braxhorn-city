#!/usr/bin/env python3
"""Owner 10.11 09:16: "kebaba tego musisz stworzyć, tego co się kręci ... do cięcia ... zrób go jak najlepiej".
Surface of a doner kebab cone (vertical rotisserie), wrapped once around the cone (u = around, v = bottom -> top):
stacked layers of seasoned meat - golden-brown roasted crust with dark caramelised / charred edges facing the grill,
lighter pink-beige freshly shaved bands where the knife has cut, fat glistening between layers, peppery speckles.
Deterministic (seeded); written to unity-city/Assets/Source/Mistrzowie/PromenadeFood/kebab_meat.jpg (committed).
"""
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "unity-city/Assets/Source/Mistrzowie/PromenadeFood/kebab_meat.jpg"
W, H = 1024, 1024
rng = np.random.default_rng(1016)


def smooth_noise(w, h, cell, seed):
    r = np.random.default_rng(seed)
    g = r.random((h // cell + 2, w // cell + 2))
    img = Image.fromarray((g * 255).astype(np.uint8)).resize((w + 2 * cell, h + 2 * cell), Image.BICUBIC)
    return np.asarray(img, np.float32)[cell:cell + h, cell:cell + w] / 255.0


u = np.linspace(0, 1, W, endpoint=False)[None, :]
v = np.linspace(0, 1, H)[:, None]
# layers: thin meat slices stacked on the skewer, wavy and of uneven thickness
wave = .004 * np.sin(u * 2 * np.pi * 3 + v * 9) + .0025 * np.sin(u * 2 * np.pi * 11 + 1.3)
layer = (v * 46 + wave * 46 + smooth_noise(W, H, 64, 3) * 1.6) % 1.0
seam = np.exp(-((layer - .5) ** 2) / .004)                      # fat / juice line between slices
crust = smooth_noise(W, H, 24, 5) * .6 + smooth_noise(W, H, 6, 7) * .4
# colours (sRGB 0..1)
roast = np.array([.45, .25, .12]); golden = np.array([.63, .39, .19]); char = np.array([.16, .085, .045])
shaved = np.array([.70, .50, .38]); fat = np.array([.86, .70, .46])
base = roast[None, None] * (1 - crust[..., None]) + golden[None, None] * crust[..., None]
# charred caramelised spots (the side that faces the burners)
ridge = np.exp(-((layer - .15) ** 2) / .006)               # the outer lip of each slice crisps first
charmask = np.clip((smooth_noise(W, H, 40, 9) * .7 + smooth_noise(W, H, 10, 19) * .3 - .55) * 3.0, 0, 1) * .55 + ridge * .45 * smooth_noise(W, H, 20, 23)
base = base * (1 - charmask[..., None] * .85) + char[None, None] * charmask[..., None] * .85
# freshly shaved vertical bands (where the knife went down the cone): lighter, pink-beige, fibrous
cut = np.clip((np.sin(u * 2 * np.pi * 5 + smooth_noise(W, H, 128, 11) * 2) - .55) * 3, 0, 1)
fibre = smooth_noise(W, H, 3, 13)[..., None] * .18
base = base * (1 - cut[..., None] * .7) + (shaved[None, None] - fibre) * cut[..., None] * .7
base = base * (1 - seam[..., None] * .35) + fat[None, None] * seam[..., None] * .35
# pepper / herb speckles
speck = rng.random((H, W)) > .9985
base[speck] = base[speck] * .35
img = Image.fromarray(np.clip(base * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.SMOOTH)
OUT.parent.mkdir(parents=True, exist_ok=True)
img.save(OUT, quality=90, optimize=True)
print("KEBAB_TEXTURE", OUT, img.size)
