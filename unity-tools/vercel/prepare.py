"""Validate a compiled Unity 6 artifact and stage a preview-only Vercel site.

Never builds Unity, uses public/, or trusts the stale checked-in unity-preview/.
The original player is mounted at its existing /unity-gym/ base URL unchanged.
"""
import argparse
import hashlib
import json
import pathlib
import re
import shutil
import zlib


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha256(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def safe_path(root, name):
    p = pathlib.PurePosixPath(name)
    require(bool(name) and not p.is_absolute() and '..' not in p.parts
            and '\\' not in name, 'Unsafe artifact path: ' + name)
    target = root / p
    require(target.resolve().is_relative_to(root.resolve()), 'Path escapes artifact')
    require(not target.is_symlink(), 'Symlink in artifact')
    return target


def read_json(root, name):
    return json.loads(safe_path(root, name).read_text())


def validate(source, commit):
    require(re.fullmatch('[0-9a-f]{40}', commit), 'Expected full source commit SHA')
    release = read_json(source, 'release.json')
    playable = read_json(source, 'playable.json')
    transport = read_json(source, 'transport.json')
    require(release['sourceCommit'] == playable['commit'] == transport['sourceCommit'] == commit,
            'Artifact source commit mismatch')
    require(release['unityVersion'] == playable['unity'] == '6000.0.58f2', 'Wrong Unity version')
    require(release['transport'] == transport, 'Release transport mismatch')
    require(release['productionChanged'] is False, 'Expected a test artifact')
    files = release['files']
    require(isinstance(files, dict) and files, 'Missing artifact checksums')
    for name, digest in files.items():
        path = safe_path(source, name)
        require(path.is_file() and path.stat().st_size > 0, 'Missing file: ' + name)
        require(sha256(path) == digest, 'Checksum mismatch: ' + name)
    actual = {p.relative_to(source).as_posix() for p in source.rglob('*') if p.is_file()}
    require(actual == set(files) | {'release.json'}, 'Unexpected/untracked artifact files')
    require(not any(p.is_symlink() for p in source.rglob('*')), 'Symlink in artifact')

    html = (source / 'index.html').read_text()
    script = (source / 'city-transport.js').read_text()
    require('createUnityInstance' in html, 'Missing Unity entry point')
    require(not re.search(r'(?:three(?:\.min)?\.js|game\.js|THREE\.)', html), 'Legacy runtime in entry')
    base = re.findall(r'<base\s+href=[\"\']([^\"\']+)', html)
    require(base == ['/unity-gym/'], 'Unexpected player base URL')
    embedded = re.search(r'const manifest = (\{[^\n]+\});', script)
    require(embedded and json.loads(embedded[1]) == transport, 'Embedded transport mismatch')
    require('DecompressionStream' in script, 'Missing native Unity data transport')
    refs = re.findall(r'(?:\bsrc\s*=|\bhref\s*=|\bdataUrl\s*:|\bframeworkUrl\s*:|\bcodeUrl\s*:)\s*[\"\']([^\"\']+)', html)
    for ref in refs:
        if ref == '/unity-gym/':
            continue
        path = ref.split('?', 1)[0].split('#', 1)[0]
        require(not re.match(r'^(?:[a-z]+:|/)', path, re.I), 'External/absolute asset: ' + ref)
        if path != transport['dataPath']:
            require(safe_path(source, path).is_file(), 'Broken asset reference: ' + ref)
        require(path in files or path == transport['dataPath'], 'Unverified reference: ' + ref)
    require(transport['dataPath'] in refs, 'HTML does not request the verified Unity data')

    prefix = commit[:12] + '_'
    for suffix in ['*.loader.js', '*.framework.js.unityweb', '*.wasm.unityweb']:
        matches = list((source / 'Build').glob(suffix))
        require(len(matches) == 1 and matches[0].name.startswith(prefix), 'Wrong Unity binary: ' + suffix)
    loader = next((source / 'Build').glob('*.loader.js')).read_text()
    require('window.BraxCityFetch' in loader and 'createUnityInstance' in loader, 'Unity streaming hook missing')
    wasm = next((source / 'Build').glob('*.wasm.unityweb'))
    with wasm.open('rb') as f:
        signature = zlib.decompressobj(31).decompress(f.read(65536), 8)
    require(signature == b'\x00asm\x01\x00\x00\x00', 'Not a gzip-compressed WebAssembly module')

    compressed = hashlib.sha256()
    expanded = hashlib.sha256()
    size = decoded_size = 0
    inflater = zlib.decompressobj(31)
    parts = transport['parts']
    require(parts, 'Missing Unity data parts')
    for i, part in enumerate(parts):
        require(part['path'] == transport['dataPath'] + f'.part{i:03d}', 'Unordered data parts')
        path = safe_path(source, part['path'])
        require(path.stat().st_size == part['bytes'] and sha256(path) == part['sha256'], 'Damaged part')
        with path.open('rb') as f:
            for block in iter(lambda: f.read(65536), b''):
                size += len(block)
                compressed.update(block)
                decoded = inflater.decompress(block)
                decoded_size += len(decoded)
                expanded.update(decoded)
    tail = inflater.flush()
    expanded.update(tail)
    decoded_size += len(tail)
    require(inflater.eof and not inflater.unused_data, 'Incomplete Unity gzip data stream')
    require(size == transport['bytes'] and compressed.hexdigest() == transport['sha256'], 'Data digest mismatch')
    require(decoded_size == transport['uncompressedBytes'] and expanded.hexdigest() == transport['uncompressedSha256'],
            'Decoded Unity data mismatch')
    return {'engine': 'Unity 6', 'unityVersion': playable['unity'], 'sourceCommit': commit,
            'compressedDataBytes': size, 'uncompressedDataBytes': decoded_size,
            'parts': len(parts), 'artifactFilesVerified': len(files), 'mount': '/unity-gym/'}


def config():
    return {
        '$schema': 'https://openapi.vercel.sh/vercel.json',
        'framework': None, 'installCommand': '',
        'buildCommand': 'node verify-preview.cjs', 'outputDirectory': 'unity-webgl',
        'git': {'deploymentEnabled': True},
        'redirects': [{'source': '/', 'destination': '/unity-gym/', 'permanent': False}],
        'headers': [
            {'source': '/(.*)', 'headers': [
                {'key': 'X-Robots-Tag', 'value': 'noindex, nofollow'},
                {'key': 'X-Content-Type-Options', 'value': 'nosniff'},
                {'key': 'Cache-Control', 'value': 'public, max-age=0, must-revalidate'}]},
            {'source': '/unity-gym/Build/(.*)', 'headers': [
                {'key': 'Cache-Control', 'value': 'public, max-age=31536000, immutable'}]},
            {'source': '/unity-gym/Build/(.*)\\.unityweb', 'headers': [
                {'key': 'Content-Type', 'value': 'application/octet-stream'}]},
            {'source': '/unity-gym/Build/(.*)\\.part([0-9]+)', 'headers': [
                {'key': 'Content-Type', 'value': 'application/octet-stream'}]},
            {'source': '/unity-gym/Build/(.*)\\.wasm', 'headers': [
                {'key': 'Content-Type', 'value': 'application/wasm'}]},
            {'source': '/unity-gym/(.*)\\.js', 'headers': [
                {'key': 'Content-Type', 'value': 'application/javascript; charset=utf-8'}]}
        ]
    }


def prepare(source, dest, commit, run):
    proof = validate(source, commit)
    require(not dest.exists(), 'Output already exists; refusing overwrite')
    target = dest / 'unity-webgl' / 'unity-gym'
    shutil.copytree(source, target)
    icon = source / 'TemplateData/favicon.ico'
    if icon.is_file():
        shutil.copyfile(icon, dest / 'unity-webgl/favicon.ico')
    proof.update(sourceRun=str(run), target='preview', productionChanged=False)
    proof['files'] = {p.relative_to(dest / 'unity-webgl').as_posix(): sha256(p)
                      for p in (dest / 'unity-webgl').rglob('*') if p.is_file()}
    (dest / 'unity-webgl' / 'deployment-proof.json').write_text(json.dumps(proof, indent=2) + '\n')
    (dest / 'vercel.json').write_text(json.dumps(config(), indent=2) + '\n')
    shutil.copyfile(pathlib.Path(__file__).with_name('verify-preview.cjs'), dest / 'verify-preview.cjs')
    print(json.dumps({k: v for k, v in proof.items() if k != 'files'}, indent=2))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=pathlib.Path, required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--commit', required=True)
    parser.add_argument('--run', required=True)
    args = parser.parse_args()
    prepare(args.source, args.output, args.commit, args.run)
