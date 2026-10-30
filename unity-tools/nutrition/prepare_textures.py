"""Original BRAXHORN shop printwork and small, tileable material maps.

No owner-provided source is replaced. Run with the project's Pillow/numpy tools.
"""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'unity-city/Assets/Nutrition/Textures'
OUT.mkdir(parents=True, exist_ok=True)
FONT = Path('/usr/share/fonts/truetype/dejavu/DejaVuSansCondensed-Bold.ttf')
REGULAR = Path('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf')
if not FONT.is_file() or not REGULAR.is_file():
    raise RuntimeError('Install fonts-dejavu-core to reproduce original labels')


def font(size, regular=False):
    return ImageFont.truetype(str(REGULAR if regular else FONT), size)


def centre(draw, text, y, size, fill, x=1024, regular=False):
    draw.text((x, y), text, font=font(size, regular), fill=fill, anchor='mt')


def label_atlas():
    image = Image.new('RGB', (2048, 2048), '#e9e3d6')
    d = ImageDraw.Draw(image)
    styles = [
        ('WHEY', 'PROTEIN', '01 / CHOCOLATE', '2 KG', '#e9e3d6', '#20272a', '#b45d3e'),
        ('CREATINE', 'MONOHYDRATE', '02 / PURE SERIES', '500 G', '#20272a', '#ede6d5', '#c5a876'),
        ('HYDRATE', 'BRAXHORN', '03 / TRAINING SHAKER', '750 ML', '#273633', '#ece8dd', '#c7ac7e'),
    ]
    for index, (a, b, sub, weight, bg, fg, accent) in enumerate(styles):
        y = index * 512
        d.rectangle((0, y, 2047, y + 511), fill=bg)
        d.rectangle((0, y + 8, 2047, y + 17), fill=accent)
        d.rectangle((0, y + 491, 2047, y + 503), fill=accent)
        centre(d, 'B R A X H O R N', y + 39, 37, fg)
        centre(d, a, y + 106, 100 if len(a) < 8 else 85, fg)
        centre(d, b, y + 221, 38, fg)
        centre(d, sub, y + 300, 24, accent)
        centre(d, weight, y + 382, 40, fg)
        for x in [130, 1650]:
            d.line((x, y + 116, x + 260, y + 116), fill=accent, width=3)
            for k, line in enumerate(['BRAXHORN CITY', 'SPORT NUTRITION', 'TRAIN / REST / REPEAT', 'PERFORMANCE SERIES']):
                d.text((x, y + 147 + k * 40), line, font=font(15, True), fill=fg)
            for k in range(44):
                width = 1 if k % 3 else 3
                d.rectangle((x + k * 4, y + 352, x + k * 4 + width, y + 407), fill=fg)
    y = 1536
    d.rectangle((0, y, 2047, 2047), fill='#20272a')
    centre(d, 'BRAXHORN', y + 43, 106, '#eee5d5')
    centre(d, 'N U T R I T I O N', y + 170, 43, '#c9aa7b')
    d.line((620, y + 250, 1428, y + 250), fill='#c9aa7b', width=3)
    centre(d, 'TRAIN  /  REFUEL  /  REPEAT', y + 300, 30, '#eee5d5')
    image.save(OUT / 'BraxhornProductAtlas.png', optimize=True)


def noise(rng, size, cell):
    small = rng.random((cell, cell))
    return np.asarray(Image.fromarray(np.uint8(small * 255)).resize((size, size), Image.Resampling.BICUBIC), dtype=float) / 255


def materials():
    size = 512
    rng = np.random.default_rng(30102026)
    yy, xx = np.mgrid[:size, :size] / size
    mineral = .6 * noise(rng, size, 9) + .25 * noise(rng, size, 35) + .15 * noise(rng, size, 180)
    stone = (mineral - .5) * 22
    base = np.array([177, 172, 159])[None, None, :] + stone[..., None]
    base += (rng.random((size, size, 1)) - .5) * 5
    Image.fromarray(np.uint8(np.clip(base, 0, 255))).save(OUT / 'LimestoneColour.jpg', quality=94)
    height = mineral * .25
    grain = np.sin(xx * np.pi * 94 + noise(rng, size, 6) * 7 + np.sin(yy * 6.28) * .4)
    fine = np.sin(xx * np.pi * 370 + np.sin(yy * 6.28) * 2)
    wood = (grain * .5 + .5) * .38 + (fine * .5 + .5) * .12 + noise(rng, size, 48) * .12
    # Seamless periodic directional grain; colour variation remains restrained.
    wood *= .78 + .22 * np.cos(yy * np.pi * 2)
    rgb = np.array([64, 43, 28])[None, None, :] + wood[..., None] * np.array([85, 65, 42])
    Image.fromarray(np.uint8(np.clip(rgb, 0, 255))).save(OUT / 'WalnutColour.jpg', quality=94)
    for name, field, strength, rough in [('Limestone', height, .13, .72), ('Walnut', wood, .14, .55)]:
        dx = (np.roll(field, -1, 1) - np.roll(field, 1, 1)) * strength
        dy = (np.roll(field, -1, 0) - np.roll(field, 1, 0)) * strength
        vectors = np.stack([-dx, -dy, np.ones_like(dx)], axis=-1)
        vectors /= np.linalg.norm(vectors, axis=-1)[..., None]
        Image.fromarray(np.uint8(np.clip(vectors * .5 + .5, 0, 1) * 255)).save(OUT / (name + 'Normal.png'))
        arm = np.zeros((size, size, 3), dtype=np.uint8)
        arm[..., 0] = 255
        arm[..., 1] = np.uint8(np.clip(rough + (field - field.mean()) * .15, 0, 1) * 255)
        Image.fromarray(arm).save(OUT / (name + 'ARM.png'))


label_atlas()
materials()
receipt = {'authoring': 'Original procedural BRAXHORN artwork, owner-authorized 2026-10-30',
           'fonts': 'DejaVu Sans / DejaVu Sans Condensed, Bitstream Vera + DejaVu license; font software not redistributed',
           'fontSha256': hashlib.sha256(FONT.read_bytes()).hexdigest(),
           'sourcesPreserved': True, 'unityVisualAcceptance': False,
           'files': {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(OUT.iterdir()) if p.suffix in ['.png', '.jpg']}}
(OUT / 'authored-textures.json').write_text(json.dumps(receipt, indent=2) + '\n')
print('NUTRITION_ORIGINAL_TEXTURES', len(receipt['files']))
