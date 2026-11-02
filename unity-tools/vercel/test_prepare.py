import gzip
import hashlib
import json
import os
import pathlib
import subprocess
import tempfile
import unittest

from prepare import prepare, sha256, validate

SHA = '5623047e2713a25ffc885a2d3037b5f8727fd606'


class PackagingTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.source = pathlib.Path(self.tmp.name) / 'artifact'
        self.source.mkdir()
        (self.source / 'Build').mkdir()
        prefix = 'Build/' + SHA[:12] + '_WebGL'
        data = b'UnityWebData1.0\0' * 100
        compressed = gzip.compress(data)
        parts = []
        for i, block in enumerate([compressed[:20], compressed[20:]]):
            name = prefix + f'.data.unityweb.part{i:03d}'
            (self.source / name).write_bytes(block)
            parts.append({'path': name, 'bytes': len(block), 'sha256': hashlib.sha256(block).hexdigest()})
        transport = {'sourceCommit': SHA, 'dataPath': prefix + '.data.unityweb',
                     'bytes': len(compressed), 'sha256': hashlib.sha256(compressed).hexdigest(),
                     'uncompressedBytes': len(data), 'uncompressedSha256': hashlib.sha256(data).hexdigest(),
                     'parts': parts}
        (self.source / (prefix + '.loader.js')).write_text('function createUnityInstance(){};window.BraxCityFetch;')
        (self.source / (prefix + '.framework.js.unityweb')).write_bytes(gzip.compress(b'framework'))
        (self.source / (prefix + '.wasm.unityweb')).write_bytes(gzip.compress(b'\x00asm\x01\x00\x00\x00'))
        (self.source / 'logo.webp').write_bytes(b'fixture')
        (self.source / 'index.html').write_text(
            '<base href="/unity-gym/"><img src="logo.webp"><script src="city-transport.js?v=test"></script>'
            f'<script>script.src="{prefix}.loader.js";createUnityInstance(canvas,{{'
            f'dataUrl:"{prefix}.data.unityweb",frameworkUrl:"{prefix}.framework.js.unityweb",'
            f'codeUrl:"{prefix}.wasm.unityweb"}});</script>')
        (self.source / 'city-transport.js').write_text('const manifest = ' + json.dumps(transport) + ';\nDecompressionStream;')
        (self.source / 'transport.json').write_text(json.dumps(transport))
        (self.source / 'playable.json').write_text(json.dumps({'commit': SHA, 'unity': '6000.0.58f2'}))
        self.release = {'sourceCommit': SHA, 'unityVersion': '6000.0.58f2',
                        'productionChanged': False, 'transport': transport}
        self.receipt()

    def receipt(self):
        self.release['files'] = {p.relative_to(self.source).as_posix(): sha256(p)
                                 for p in self.source.rglob('*') if p.is_file() and p.name != 'release.json'}
        (self.source / 'release.json').write_text(json.dumps(self.release))

    def test_preserves_player_and_base_url(self):
        output = pathlib.Path(self.tmp.name) / 'site'
        prepare(self.source, output, SHA, '41351314859')
        target = output / 'unity-webgl/unity-gym'
        for p in self.source.rglob('*'):
            if p.is_file():
                self.assertEqual(p.read_bytes(), (target / p.relative_to(self.source)).read_bytes())
        config = json.loads((output / 'vercel.json').read_text())
        self.assertEqual(config['redirects'][0]['destination'], '/unity-gym/')
        self.assertFalse(any(h['key'] == 'Content-Encoding' for r in config['headers'] for h in r['headers']))
        command = ['node', str(output / 'verify-preview.cjs')]
        preview = subprocess.run(command, env={**os.environ, 'VERCEL_ENV': 'preview'}, capture_output=True)
        self.assertEqual(preview.returncode, 0, preview.stderr)
        production = subprocess.run(command, env={**os.environ, 'VERCEL_ENV': 'production'}, capture_output=True)
        self.assertNotEqual(production.returncode, 0)
        (target / 'logo.webp').write_bytes(b'corrupt')
        broken = subprocess.run(command, env={**os.environ, 'VERCEL_ENV': 'preview'}, capture_output=True)
        self.assertNotEqual(broken.returncode, 0)

    def test_rejects_stale_commit(self):
        with self.assertRaisesRegex(ValueError, 'commit mismatch'):
            validate(self.source, 'a' * 40)

    def test_rejects_missing_part(self):
        next((self.source / 'Build').glob('*.part000')).unlink()
        with self.assertRaisesRegex(ValueError, 'Missing file'):
            validate(self.source, SHA)

    def test_rejects_corrupt_data(self):
        next((self.source / 'Build').glob('*.part001')).write_bytes(b'bad')
        with self.assertRaisesRegex(ValueError, 'Checksum mismatch'):
            validate(self.source, SHA)

    def test_rejects_broken_entry_path(self):
        p = self.source / 'index.html'
        p.write_text(p.read_text().replace('src="logo.webp"', 'src="missing.webp"'))
        self.receipt()
        with self.assertRaisesRegex(ValueError, 'Broken asset'):
            validate(self.source, SHA)

    def test_rejects_changed_base_path(self):
        p = self.source / 'index.html'
        p.write_text(p.read_text().replace('/unity-gym/', '/wrong/'))
        self.receipt()
        with self.assertRaisesRegex(ValueError, 'base URL'):
            validate(self.source, SHA)

    def test_rejects_legacy_entry(self):
        p = self.source / 'index.html'
        p.write_text(p.read_text() + '<script src="game.js"></script>')
        self.receipt()
        with self.assertRaisesRegex(ValueError, 'Legacy runtime'):
            validate(self.source, SHA)

    def test_rejects_path_traversal(self):
        self.release['files']['../outside'] = '0' * 64
        (self.source / 'release.json').write_text(json.dumps(self.release))
        with self.assertRaisesRegex(ValueError, 'Unsafe artifact path'):
            validate(self.source, SHA)


if __name__ == '__main__':
    unittest.main()
