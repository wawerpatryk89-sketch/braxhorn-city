# usage: mktex.py key fbm_dir
import sys, glob, os
from PIL import Image
Image.MAX_IMAGE_PIXELS = None
key, d = sys.argv[1], sys.argv[2]
f = lambda pat: (glob.glob(os.path.join(d, pat)) or [None])[0]
col = Image.open(f('*BaseColor*')).convert('RGB')
col.resize((2048, 2048), Image.LANCZOS).save(f'tex/{key}_color.webp', quality=86)
col.resize((1024, 1024), Image.LANCZOS).save(f'tex/{key}_color.jpg', quality=85)
Image.open(f('*Normal*')).convert('RGB').resize((1024, 1024), Image.LANCZOS).save(f'tex/{key}_normal.webp', quality=90)
r = Image.open(f('*oughness*')).convert('L').resize((1024, 1024), Image.LANCZOS)
m = Image.open(f('*etallic*')).convert('L').resize((1024, 1024), Image.LANCZOS)
Image.merge('RGB', (Image.new('L', (1024, 1024), 255), r, m)).save(f'tex/{key}_orm.webp', quality=90)
print(key, 'ok')
