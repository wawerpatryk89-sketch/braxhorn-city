"""Pack source data maps before Unity import; no resizing or gamma conversion."""
from pathlib import Path
from PIL import Image
import numpy as np


def channel(path, size):
    with Image.open(path) as image:
        if image.size != size:
            raise ValueError(f'{path}: expected {size}, got {image.size}; resizing forbidden')
        rgb = image.convert('RGB')
        r, g, b = rgb.split()
        if r.tobytes() != g.tobytes() or r.tobytes() != b.tobytes():
            raise ValueError(f'{path}: data map is not grayscale')
        return r


def pack(roughness, output, ao=None, metal=None, size=(4096, 4096)):
    rough = channel(roughness, size)
    occlusion = channel(ao, size) if ao else Image.new('L', size, 255)
    metallic = channel(metal, size) if metal else Image.new('L', size, 0)
    result = Image.merge('RGB', (occlusion, rough, metallic))
    temp = Path(str(output) + '.tmp')
    result.save(temp, format='PNG')
    temp.replace(output)


def linear_mean(path):
    # Sample only for colour calibration; original texture bytes stay unchanged.
    with Image.open(path) as image:
        data = np.asarray(image.convert('RGB').resize((128, 128)), dtype=np.float32) / 255
    linear = np.where(data <= .04045, data / 12.92, ((data + .055) / 1.055) ** 2.4)
    return [float(x) for x in linear.mean(axis=(0, 1))]
