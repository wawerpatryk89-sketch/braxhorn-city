"""Fetch selected CC0 sources only; keep a byte/provenance receipt.

No Unity packages are installed. Poly Haven maps are fetched individually and
checked against its official API digest; original models are never executed.
"""
import hashlib
import json
import pathlib
import urllib.request
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / 'unity-city/Assets/Kickboxing'
TEXTURES = ASSETS / 'Textures'
MODELS = ASSETS / 'Models'
RECEIPT = []
HEADERS = {'User-Agent': 'Braxhorn-asset-review/1.0'}


def fetch(url):
    with urllib.request.urlopen(urllib.request.Request(url, headers=HEADERS), timeout=60) as response:
        return response.read()


def save_source(spec, destination, source, author, license_url):
    data = fetch(spec['url'])
    if spec.get('md5') and hashlib.md5(data).hexdigest() != spec['md5']:
        raise RuntimeError('Official source digest mismatch: ' + str(destination))
    if spec.get('sha256') and hashlib.sha256(data).hexdigest() != spec['sha256']:
        raise RuntimeError('Reviewed source was changed upstream: ' + str(destination))
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)
    RECEIPT.append({'file': str(destination.relative_to(ROOT)), 'bytes': len(data),
                    'sha256': hashlib.sha256(data).hexdigest(), 'source': source,
                    'download': spec['url'], 'author': author, 'license': 'CC0-1.0',
                    'license_url': license_url})


def polyhaven_maps(asset_id, label, size, author):
    data = json.loads(fetch('https://api.polyhaven.com/files/' + asset_id))
    diffuse = next(k for k in data if k.lower() in ('diff', 'diffuse'))
    for channel, suffix, ext in [(diffuse, 'Albedo', 'jpg'), ('nor_gl', 'Normal', 'png'), ('arm', 'ARM', 'jpg')]:
        save_source(data[channel][size][ext], TEXTURES / (label + suffix + '.' + ext),
                    'https://polyhaven.com/a/' + asset_id, author, 'https://polyhaven.com/license')


def model(asset_id, filename):
    save_source({'url': 'https://www.innerscene.com/api/library/' + asset_id + '/download'},
                MODELS / filename, 'https://www.innerscene.com/tools/library/3d-parts/' + asset_id,
                'Innerscene', 'https://www.innerscene.com/tools/library/terms')


def main():
    polyhaven_maps('brick_wall_10', 'Brick', '2k', 'Dimitrios Savva / Poly Haven')
    polyhaven_maps('fabric_leather_01', 'Leather', '1k', 'Rob Tuytel / Poly Haven')
    polyhaven_maps('fine_grained_wood', 'Wood', '1k', 'Rob Tuytel / Poly Haven')
    model('compact-wc-close-coupled-toilet-84214efd', 'Toilet.glb')
    model('basin-pedestal-sink-cabinet-81678fc4', 'Sink.glb')
    save_source({'url': 'https://www.innerscene.com/api/library/boxing-gloves-and-bag-e3d766bc/download',
                 'sha256': '70676b234ba42882e05f1dc31c42b5c248c046904b586eab18991080b016df8b'},
                ROOT / 'build-source/BoxingBagSource.glb',
                'https://www.innerscene.com/tools/library/3d-parts/boxing-gloves-and-bag-e3d766bc',
                'Innerscene', 'https://www.innerscene.com/tools/library/terms')
    # Exact project-owned pixels, losslessly re-encoded for Unity's importer.
    for source_name, target_name in [('mark.webp', 'Mark.png'), ('wide_a.webp', 'Wordmark.png')]:
        source_path = ROOT / 'public/assets/logo' / source_name
        with Image.open(source_path) as image:
            image.convert('RGBA').save(TEXTURES / target_name)
    # Keep the original separate glTF dependencies: no Blender round trip or
    # texture re-encoding is needed to import the real Poly Haven mesh in Unity.
    lamp = json.loads(fetch('https://api.polyhaven.com/files/hanging_industrial_lamp'))['gltf']['1k']['gltf']
    lamp_root = MODELS / 'PendantLamp'
    source = 'https://polyhaven.com/a/hanging_industrial_lamp'
    save_source(lamp, lamp_root / 'PendantLamp.gltf', source, 'Kuutti Siitonen / Poly Haven', 'https://polyhaven.com/license')
    for name, spec in lamp['include'].items():
        path = pathlib.PurePosixPath(name)
        if path.is_absolute() or '..' in path.parts:
            raise RuntimeError('Unsafe source path')
        save_source(spec, lamp_root / path, source, 'Kuutti Siitonen / Poly Haven', 'https://polyhaven.com/license')
    (ASSETS / 'asset-provenance.json').write_text(json.dumps({'assets': RECEIPT,
        'api_credit': 'Powered by Poly Haven', 'scope': 'Kickboxing room and annex only',
        'brand': {'owner': 'BRAXHORN', 'source': 'Existing public/assets/logo', 'pixelEdits': False}}, indent=2))
    print(json.dumps({'files': len(RECEIPT), 'bytes': sum(x['bytes'] for x in RECEIPT), 'receipt': str(ASSETS / 'asset-provenance.json')}))


if __name__ == '__main__':
    main()
