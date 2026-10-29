"""Package the rebuilt Unity room using the current production iOS transport.

Only the compiled player and its manifest change. The proven native gzip
stream, mobile controls and city UI are carried forward from current main.
This script creates an isolated test artifact; it never publishes production.
"""
import hashlib
import json
import os
import pathlib
import re
import shutil
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'build/WebGL'
OUT = ROOT / 'unity-preview'
PUBLISHED = ROOT / 'public/unity-gym'
COMMIT = os.environ.get('GITHUB_SHA', 'local')
VERSION = COMMIT[:12]


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        while chunk := stream.read(1024 * 1024):
            h.update(chunk)
    return h.hexdigest()


def main():
    if OUT.exists():
        shutil.rmtree(OUT)
    shutil.copytree(SOURCE, OUT)
    build = OUT / 'Build'
    entries = {}
    for key, pattern in [('loader', '*.loader.js'), ('data', '*.data*'),
                         ('framework', '*.framework.js*'), ('wasm', '*.wasm*')]:
        matches = list(build.glob(pattern))
        if len(matches) != 1:
            raise RuntimeError((key, [p.name for p in matches]))
        source = matches[0]
        renamed = source.with_name(VERSION + '_' + source.name)
        source.rename(renamed)
        entries[key] = renamed

    loader = entries['loader']
    code = loader.read_text()
    replacements = {
        'fetch(e,t).then(function(e){return b.readBodyWithProgress':
        '(window.BraxCityFetch||fetch)(e,t).then(function(e){return b.readBodyWithProgress',
        'n=b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress':
        'n=window.BraxCityFetch?b.fetchWithProgress:(b.companyName&&b.productName?b.cachedFetch:b.fetchWithProgress)'
    }
    for old, new in replacements.items():
        if code.count(old) != 1:
            raise RuntimeError('Unity loader changed; refusing to drop existing iOS streaming hook: ' + old)
        code = code.replace(old, new, 1)
    loader.write_text(code)

    compressed = hashlib.sha256()
    expanded = hashlib.sha256()
    inflater = zlib.decompressobj(31)
    parts = []
    expanded_bytes = total = 0
    data = entries['data']
    with data.open('rb') as source:
        while payload := source.read(32 * 1024 * 1024):
            target = data.with_name(data.name + f'.part{len(parts):03d}')
            target.write_bytes(payload)
            compressed.update(payload)
            total += len(payload)
            for offset in range(0, len(payload), 65536):
                decoded = inflater.decompress(payload[offset:offset+65536])
                expanded_bytes += len(decoded)
                expanded.update(decoded)
            parts.append({'path': target.relative_to(OUT).as_posix(), 'bytes': len(payload), 'sha256': digest(target)})
    tail = inflater.flush()
    expanded.update(tail)
    expanded_bytes += len(tail)
    if not inflater.eof or inflater.unused_data:
        raise RuntimeError('Compiled Unity data is not one complete gzip stream.')
    manifest = {'dataPath': data.relative_to(OUT).as_posix(), 'bytes': total,
                'sha256': compressed.hexdigest(), 'uncompressedBytes': expanded_bytes,
                'uncompressedSha256': expanded.hexdigest(), 'parts': parts,
                'sourceCommit': COMMIT, 'assetQualityChanged': True}
    data.unlink()
    (OUT / 'transport.json').write_text(json.dumps(manifest, indent=2) + '\n')
    transport = (PUBLISHED / 'city-transport.js').read_text()
    if "const revision = 'ios-stream-01'" not in transport or 'DecompressionStream' not in transport:
        raise RuntimeError('Expected current iOS-safe loader template is missing.')
    transport, count = re.subn(r'  const manifest = .*?;\n',
                              '  const manifest = ' + json.dumps(manifest, separators=(',', ':')) + ';\n',
                              transport, count=1)
    if count != 1:
        raise RuntimeError('Transport manifest replacement failed.')
    # These are diagnostic labels only, not changes to the streaming algorithm.
    transport = transport.replace('originalAssetsChanged: false', 'originalAssetsChanged: true')
    transport = transport.replace('Original Unity asset bytes are unchanged.', 'Native stream algorithm preserved; room rebuilt on an isolated test branch.')
    (OUT / 'city-transport.js').write_text(transport)

    html = (PUBLISHED / 'index.html').read_text()
    previous = json.loads((PUBLISHED / 'release.json').read_text())['sourceCommit'][:12]
    if previous not in html:
        raise RuntimeError('Current city entry does not match its published source version.')
    html = html.replace(previous + '_WebGL', VERSION + '_WebGL').replace(previous, VERSION)
    html = html.replace('city-transport.js?v=ios-stream-01', 'city-transport.js?v=ios-stream-01-' + VERSION)
    html = re.sub(r'około \d+ MB danych Unity', f'około {round((total+entries["wasm"].stat().st_size)/1000000)} MB danych Unity', html)
    (OUT / 'index.html').write_text(html)
    shutil.copyfile(ROOT / 'public/assets/logo/wide_a.webp', OUT / 'logo.webp')
    for name in ['city-manifest.json', 'estate-manifest.json', 'source-provenance.json', 'native-texture-preparation.json']:
        shutil.copyfile(ROOT / 'unity-city/Assets/Source/City' / name, OUT / name)
    for name in ['unity-premium-report.json', 'material-review.json', 'geometry-review.json', 'source-refinements.json']:
        shutil.copyfile(ROOT / 'unity-city/Assets/Source/Premium' / name, OUT / name)
    for name, source in {
        'source-integrity.json': 'unity-city/Assets/Source/source-integrity.json',
        'kickboxing-layout.json': 'unity-city/Assets/PlayableGenerated/ReferenceRebuild/layout-report.json',
        'kickboxing-assets.json': 'unity-city/Assets/Kickboxing/asset-provenance.json',
        'bag-provenance.json': 'unity-city/Assets/Kickboxing/Models/BagSource.provenance.json',
        'kickboxing-native-textures.json': 'unity-city/Assets/Kickboxing/Models/native-texture-preparation.json',
    }.items():
        shutil.copyfile(ROOT / source, OUT / name)
    oxygen=json.loads((ROOT/'unity-tools/kickboxing/oxygen_selected_receipt.json').read_text())
    if oxygen.get('models')!=['pear_one','pear_two','bench_press','treadmill','exercise_bike','ladder','dumbbell_rack'] or oxygen.get('ringModelsImported')!=0:
        raise RuntimeError('Original Oxygen3D equipment pack is missing or contains extra ring geometry')
    (OUT/'oxygen-source.json').write_text(json.dumps(oxygen,indent=2)+'\n')
    (OUT / 'robots.txt').write_text('User-agent: *\nDisallow: /\n')
    (OUT / 'library-credits.txt').write_text(
        'BRAXHORN marks, Brax, existing city and original animations: project owner.\n'
        'Brick Wall 10: Dimitrios Savva / Poly Haven, CC0.\n'
        'Fabric Leather 01: Rob Tuytel / Poly Haven, CC0.\n'
        'Hanging Industrial Lamp: Kuutti Siitonen / Poly Haven, CC0.\n'
        'Fine Grained Wood: Rob Tuytel / Poly Haven, CC0, 0.6 m physical repeat.\n'
        'Rubber Tiles: Amal Kumar / Poly Haven, CC0, 2 m physical repeat.\n'
        'Approved BRAXHORN rear-wall mural: project artwork, matte paint blended over source brick (not a framed billboard).\n'
        'Boxing gloves and bag; Compact WC; Basin pedestal sink cabinet: Innerscene, CC0.\n'
        'Reviewed bag derivative: original chain mesh and normal map, reinforced leather body, seams, straps, hardware and original BRAXHORN branding.\n'
        'Oxygen3D / Oxygen — Game Ready Gym Environment Asset Pack, CC BY Attribution.\n'
        'Source: https://sketchfab.com/3d-models/game-ready-gym-environment-asset-pack-8aef4a478bbe49d483280968aff59064\n'
        'Changes: selected the owner requested equipment; optimised texture atlas for Unity mobile; did not import the original pack rings.\n'
        'Powered by Poly Haven. Full source and license URLs: kickboxing-assets.json.\n')
    files = {p.relative_to(OUT).as_posix(): digest(p) for p in OUT.rglob('*') if p.is_file()}
    release = {'scope': 'isolated-kickboxing-room-and-annex-test', 'sourceCommit': COMMIT,
               'productionBase': '26e5ef339b09df6c1077e9968200660ec89cdacf',
               'unitySourceBase': 'ba678928b2dcb2dff9e38bdb5306e090e76360cf',
               'unityVersion': '6000.0.58f2', 'renderPipeline': 'Built-in',
               'productionChanged': False, 'physicalIPhoneValidated': False,
               'billboardArtworkApproved': False, 'billboardArtworkInstalled': False,
               'muralArtworkApproved': True, 'muralArtworkInstalled': True,
               'muralSourceSha256': 'e714741664dbedddc379a97a87f7d8757856c73438eeb34af10df058846628e8',
               'muralOriginalApprovedPngSha256': 'c84fbc33ea871834a403a80169f09fa0db562254c0d2f082e655cbf4dafc73ef',
               'transport': manifest,
               'loadingFix': {'revision': 'ios-stream-01', 'nativeStreamingGzip': True,
                              'maxInputChunkBytes': 65536, 'algorithmPreserved': True,
                              'compressedBytes': total, 'uncompressedBytes': expanded_bytes},
               'oxygenOriginalSource': {'author':'Oxygen3D','license':'CC BY Attribution',
                 'archiveSha256':'c847f82ec2c0f07a7466812c303414135576a50f5432708518518fa6a6e302ad',
                 'selectedModelCount':7,'ringModelsImported':0,
                 'outputSha256':oxygen['outputSha256']},
               'files': files}
    (OUT / 'release.json').write_text(json.dumps(release, indent=2) + '\n')
    print('KICKBOXING_TEST_PACKAGE', COMMIT, total, expanded_bytes, len(parts))


if __name__ == '__main__':
    main()
