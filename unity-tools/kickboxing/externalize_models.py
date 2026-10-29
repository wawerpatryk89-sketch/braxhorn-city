"""Externalize only new room GLB images for Unity's native texture importer.

No mesh, UV, normal, material value, node transform or image pixel is changed.
PNG bytes are copied verbatim; non-image bufferViews are packed verbatim with
4-byte alignment. The report verifies the complete JSON graph after reversing
storage-only edits, the geometry bytes, and decoded image pixels.

Run after fetch_assets.py and prepare_bag.py, before opening/building Unity:
    python3 unity-tools/kickboxing/externalize_models.py
Use --remove-source only for reproducible CI inputs after their .gltf callers
have been wired. Source GLBs are retained by default.
"""
import argparse
import copy
import hashlib
import io
import json
import pathlib
import struct
import uuid

from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
MODELS = ROOT / 'unity-city/Assets/Kickboxing/Models'
NAMES = ('Sink', 'Toilet', 'BagSwing')


def sha(data):
    return hashlib.sha256(data).hexdigest()


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(',', ':')).encode()


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if not path.exists() or path.read_bytes() != data:
        path.write_bytes(data)


def meta(path, folder=False):
    """Stable new-asset GUIDs; never replace a pre-existing Unity .meta."""
    target = pathlib.Path(str(path) + '.meta')
    if target.exists():
        return
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'braxhorn/kickboxing/' + str(path.relative_to(MODELS))).hex
    write(target, ('fileFormatVersion: 2\nguid: ' + guid + '\n' +
                   ('folderAsset: yes\n' if folder else '')).encode())


def read_glb(path):
    raw = path.read_bytes()
    if len(raw) < 28 or struct.unpack_from('<4sII', raw) != (b'glTF', 2, len(raw)):
        raise ValueError('Invalid GLB 2 container: ' + str(path))
    chunks = {}
    at = 12
    while at < len(raw):
        length, kind = struct.unpack_from('<I4s', raw, at)
        at += 8
        if kind in chunks or at + length > len(raw):
            raise ValueError('Invalid or duplicate GLB chunk')
        chunks[kind] = raw[at:at + length]
        at += length
    if set(chunks) != {b'JSON', b'BIN\0'}:
        raise ValueError('Expected one JSON and one BIN chunk')
    source = json.loads(chunks[b'JSON'])
    if len(source['buffers']) != 1 or 'uri' in source['buffers'][0]:
        raise ValueError('Expected one embedded source buffer')
    if 'EXT_meshopt_compression' in source.get('extensionsUsed', []):
        raise ValueError('Raw meshopt buffer references require a separate remapper')
    data = chunks[b'BIN\0'][:source['buffers'][0]['byteLength']]
    if len(data) != source['buffers'][0]['byteLength']:
        raise ValueError('Truncated binary buffer')
    return raw, source, data


def walk_views(value, mapping=None):
    found = set()
    if isinstance(value, dict):
        for key, child in value.items():
            if key == 'bufferView':
                found.add(child)
                if mapping is not None:
                    value[key] = mapping[child]
            else:
                found.update(walk_views(child, mapping))
    elif isinstance(value, list):
        for child in value:
            found.update(walk_views(child, mapping))
    return found


def image_usage(source):
    """glTF color slots use sRGB; normal/roughness/occlusion remain linear."""
    colors = set()
    linear = set()
    color_slots = {'baseColorTexture', 'emissiveTexture', 'diffuseTexture',
                   'specularGlossinessTexture', 'specularColorTexture', 'sheenColorTexture'}

    def visit(value):
        if isinstance(value, dict):
            for key, child in value.items():
                if key.endswith('Texture') and isinstance(child, dict) and 'index' in child:
                    index = source['textures'][child['index']]['source']
                    (colors if key in color_slots else linear).add(index)
                else:
                    visit(child)
        elif isinstance(value, list):
            for child in value:
                visit(child)
    visit(source.get('materials', []))
    if colors & linear:
        raise ValueError('One source image is used in both sRGB and linear slots; inspect before converting')
    return colors


def pixels(content):
    with Image.open(io.BytesIO(content)) as image:
        image.load()
        if image.format != 'PNG':
            raise ValueError('The selected room models must contain original PNG images')
        if max(image.size) > 2048:
            raise ValueError('Image exceeds the dedicated importer limit; do not silently resize')
        return {'width': image.width, 'height': image.height, 'mode': image.mode,
                'hasAlpha': 'A' in image.getbands() or 'transparency' in image.info,
                'pixelSha256': sha(canonical([image.mode, *image.size]) + image.tobytes())}


def rgba_mip_bytes(width, height, compressed=False, alpha=False):
    total = 0
    while True:
        total += (((width + 3) // 4) * ((height + 3) // 4) * (16 if alpha else 8)
                  if compressed else width * height * 4)
        if width == height == 1:
            return total
        width, height = max(1, width // 2), max(1, height // 2)


def convert(name):
    path = MODELS / (name + '.glb')
    raw, source, binary = read_glb(path)
    output = copy.deepcopy(source)
    color_images = image_usage(source)
    images_dir = MODELS / 'Images'
    images_dir.mkdir(exist_ok=True)
    meta(images_dir, folder=True)
    image_records = []
    image_views = set()

    for index, image in enumerate(source.get('images', [])):
        if 'bufferView' not in image or image.get('mimeType') != 'image/png':
            raise ValueError('Only embedded original PNGs are accepted: ' + name)
        view_index = image['bufferView']
        view = source['bufferViews'][view_index]
        if view.get('buffer', 0) != 0:
            raise ValueError('Unexpected source buffer')
        start = view.get('byteOffset', 0)
        content = binary[start:start + view['byteLength']]
        if len(content) != view['byteLength']:
            raise ValueError('Truncated image bufferView')
        before = pixels(content)
        srgb = index in color_images
        filename = ('s_' if srgb else 'l_') + ('rgba_' if before['hasAlpha'] else 'rgb_') + sha(content) + '.png'
        destination = images_dir / filename
        if destination.exists() and destination.read_bytes() != content:
            raise ValueError('Existing content-addressed image differs')
        write(destination, content)
        meta(destination)
        after = pixels(destination.read_bytes())
        assert before == after and sha(content) == sha(destination.read_bytes())
        external = copy.deepcopy(image)
        external.pop('bufferView')
        external['uri'] = 'Images/' + filename
        output['images'][index] = external
        image_views.add(view_index)
        image_records.append({'sourceImage': index, 'name': image.get('name'),
                              'uri': external['uri'], 'srgb': srgb,
                              'encodedSha256Before': sha(content), 'encodedSha256After': sha(destination.read_bytes()),
                              'pixelSha256Before': before['pixelSha256'], 'pixelSha256After': after['pixelSha256'],
                              **{k: v for k, v in before.items() if k != 'pixelSha256'},
                              'bytes': len(content), 'pixelsUnchanged': True})

    # Preserve a bufferView even if an image and an accessor share it.
    needed = walk_views({k: v for k, v in source.items() if k != 'images'})
    discard = image_views - needed
    views, mapping, packed = [], {}, bytearray()
    original_parts = []
    for index, view in enumerate(source['bufferViews']):
        if index in discard:
            continue
        if view.get('buffer', 0) != 0:
            raise ValueError('Unexpected geometry source buffer')
        start = view.get('byteOffset', 0)
        content = binary[start:start + view['byteLength']]
        if len(content) != view['byteLength']:
            raise ValueError('Truncated geometry bufferView')
        original_parts.append(content)
        packed.extend(b'\0' * (-len(packed) % 4))
        new = copy.deepcopy(view)
        new.update(buffer=0, byteOffset=len(packed))
        packed.extend(content)
        mapping[index] = len(views)
        views.append(new)
    output.pop('bufferViews')
    walk_views(output, mapping)
    output['bufferViews'] = views
    bin_name = name + '.bin'
    output['buffers'] = [{'uri': bin_name, 'byteLength': len(packed)}]

    # Restore storage edits and prove the full source JSON graph is untouched.
    restored = copy.deepcopy(output)
    walk_views(restored, {new: old for old, new in mapping.items()})
    for key in ('images', 'buffers', 'bufferViews'):
        restored[key] = copy.deepcopy(source[key])
    assert restored == source, 'Non-storage glTF metadata changed'
    geometry_before = sha(b''.join(original_parts))
    geometry_after = sha(b''.join(packed[v['byteOffset']:v['byteOffset'] + v['byteLength']] for v in views))
    assert geometry_before == geometry_after
    triangles = sum(source['accessors'][p['indices']]['count'] // 3
                    for m in source['meshes'] for p in m['primitives'] if p.get('mode', 4) == 4)
    bin_path, gltf_path = MODELS / bin_name, MODELS / (name + '.gltf')
    write(bin_path, bytes(packed))
    write(gltf_path, json.dumps(output, separators=(',', ':')).encode() + b'\n')
    meta(bin_path)
    meta(gltf_path)
    # Verify the actual written binary and JSON, not just in-memory candidates.
    assert bin_path.read_bytes() == packed and json.loads(gltf_path.read_bytes()) == output
    unique = {x['uri']: x for x in image_records}.values()
    raw_bytes = sum(rgba_mip_bytes(x['width'], x['height']) for x in unique)
    native_bytes = sum(rgba_mip_bytes(x['width'], x['height'], True, x['hasAlpha']) for x in unique)
    return {'model': name, 'source': path.name, 'sourceGlbSha256': sha(raw),
            'output': gltf_path.name, 'meshCount': len(source['meshes']), 'triangles': triangles,
            'geometryBytes': len(packed), 'geometrySha256Before': geometry_before,
            'geometrySha256After': geometry_after, 'geometryUnchanged': True,
            'sourceGraphSha256': sha(canonical(source)), 'restoredGraphSha256': sha(canonical(restored)),
            'materialsNodesUvsAndAccessorsUnchanged': True, 'images': image_records,
            'imagePixelsChanged': False, 'resolutionChanged': False,
            'theoreticalTextureMipBytes': {'rgba32': raw_bytes, 'ETC2': native_bytes,
                'note': 'Analytical texture payload only; not a measured Unity/iPhone memory result.'}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('models', nargs='*', choices=NAMES)
    parser.add_argument('--remove-source', action='store_true', help='Remove reproducible input GLBs only after all integrity checks pass')
    args = parser.parse_args()
    names = args.models or NAMES
    reports = [convert(name) for name in names]
    receipt = MODELS / 'native-texture-preparation.json'
    write(receipt, json.dumps({'scope': 'Kickboxing Models only', 'models': reports}, indent=2).encode() + b'\n')
    meta(receipt)
    if args.remove_source:
        for name in names:
            (MODELS / (name + '.glb')).unlink()
    for report in reports:
        print('ROOM_MODEL_GEOMETRY_AND_PIXELS_PRESERVED', report['model'], report['triangles'],
              report['geometrySha256After'], report['theoreticalTextureMipBytes'])


if __name__ == '__main__':
    main()
