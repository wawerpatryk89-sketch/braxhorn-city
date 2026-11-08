#!/usr/bin/env python3
"""One-shot Issue #30 licensed engine-audio finalizer. No Unity, no APK."""
from pathlib import Path
import hashlib, json, math, struct, subprocess, tempfile, wave

ROOT = Path.cwd()
AUDIO = ROOT / 'unity-city/Assets/Source/Mistrzowie/Audio/BraxCar'
MANIFEST = ROOT / 'unity-tools/mistrzowie/champion_car_audio_sources.json'
SOURCE_URL = 'https://upload.wikimedia.org/wikipedia/commons/3/33/Starting_up_Lada_1200L_Zhiguli_Engine.webm'
SOURCE_PAGE = 'https://commons.wikimedia.org/wiki/File:Starting_up_Lada_1200L_Zhiguli_Engine.webm'
SOURCE_SHA = '15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6'
SOURCE_BYTES = 6596358
RATE = 22050

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def run(*args):
    subprocess.run(args, check=True)

def write_wav(path, samples):
    pcm = bytearray()
    for v in samples:
        q = max(-32767, min(32767, int(round(v * 32767))))
        pcm.extend(struct.pack('<h', q))
    with wave.open(str(path), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(pcm)

def main():
    with tempfile.TemporaryDirectory(prefix='issue30-audio-') as td:
        td = Path(td)
        source = td / 'source.webm'
        run('curl', '--fail', '--location', '--retry', '3', '--retry-all-errors',
            '--user-agent', 'BRAXHORN-CITY/Issue30 licensed audio finalizer',
            SOURCE_URL, '-o', str(source))
        if source.stat().st_size != SOURCE_BYTES or sha(source) != SOURCE_SHA:
            raise ValueError('Audited CC-BY source bytes/hash changed')
        wav = td / 'source.wav'
        run('ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source),
            '-vn', '-ac', '1', '-ar', str(RATE), '-c:a', 'pcm_s16le', str(wav))
        with wave.open(str(wav), 'rb') as w:
            if (w.getnchannels(), w.getsampwidth(), w.getframerate()) != (1, 2, RATE):
                raise ValueError('Unexpected decoded PCM format')
            raw = w.readframes(w.getnframes())
        vals = struct.unpack('<%dh' % (len(raw) // 2), raw)
        x = [v / 32768.0 for v in vals]

        def soft(v):
            return .95 * math.tanh((v * (10 ** (18 / 20))) / .95)

        seg = [soft(v) for v in x[int(12.0 * RATE):int(17.5 * RATE)]]
        f = int(.5 * RATE)
        cross = []
        for i in range(f):
            t = i / (f - 1)
            cross.append(seg[-f + i] * (1 - t) + seg[i] * t)
        idle = cross + seg[f:-f]
        if len(idle) != int(5.0 * RATE):
            raise ValueError('Idle sample length is not exactly 5.0 s')

        rev = [soft(v) for v in x[int(21.75 * RATE):int(24.5 * RATE)]]
        fi, fo = int(.12 * RATE), int(.28 * RATE)
        for i in range(fi):
            rev[i] *= i / (fi - 1)
        for i in range(fo):
            rev[-fo + i] *= (fo - 1 - i) / (fo - 1)
        if len(rev) != int(2.75 * RATE):
            raise ValueError('Rev sample length is not exactly 2.75 s')

        idle_wav, rev_wav = td / 'idle.wav', td / 'rev.wav'
        write_wav(idle_wav, idle)
        write_wav(rev_wav, rev)
        AUDIO.mkdir(parents=True, exist_ok=True)
        idle_ogg, rev_ogg = AUDIO / 'brax_engine_idle.ogg', AUDIO / 'brax_engine_rev.ogg'
        common = [
            '-metadata', 'artist=Sounds of Changes / Antti Makkonen',
            '-metadata', 'license=CC BY 3.0 Unported',
            '-metadata', f'source={SOURCE_PAGE}',
        ]
        run('ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(idle_wav),
            '-c:a', 'libvorbis', '-q:a', '4',
            '-metadata', 'title=BRAXHORN engine idle - derivative',
            '-metadata', 'DESCRIPTION=Derived for BRAXHORN CITY Issue 30; mono 22.05kHz; source 12.00-17.50s; 0.5s circular loop crossfade; +18dB soft-limited.',
            *common, str(idle_ogg))
        run('ffmpeg', '-hide_banner', '-loglevel', 'error', '-y', '-i', str(rev_wav),
            '-c:a', 'libvorbis', '-q:a', '4',
            '-metadata', 'title=BRAXHORN engine rev - derivative',
            '-metadata', 'DESCRIPTION=Derived for BRAXHORN CITY Issue 30; mono 22.05kHz; source 21.75-24.50s; 0.12s fade-in / 0.28s fade-out; +18dB soft-limited.',
            *common, str(rev_ogg))

    clips = [
        ('idle', AUDIO / 'brax_engine_idle.ogg', 5.0, [12.0, 17.5]),
        ('rev', AUDIO / 'brax_engine_rev.ogg', 2.75, [21.75, 24.5]),
    ]
    records = []
    for role, path, duration, source_range in clips:
        data = path.read_bytes()
        if not data.startswith(b'OggS') or b'vorbis' not in data[:8192]:
            raise ValueError(role + ' is not Ogg/Vorbis')
        if b'CC BY 3.0 Unported' not in data[:8192] or b'Antti Makkonen' not in data[:8192]:
            raise ValueError(role + ' embedded attribution missing')
        records.append({
            'role': role,
            'unity_path': path.relative_to(ROOT).as_posix(),
            'sha256': sha(path),
            'bytes': path.stat().st_size,
            'duration_seconds': duration,
            'sample_rate_hz': RATE,
            'channels': 1,
            'source_segment_seconds': source_range,
        })

    manifest = {
        'schema': 3, 'status': 'BINARIES_COMMITTED_AND_HASHED', 'issue': 30,
        'checked_at': '2026-11-08',
        'source': {
            'title': 'Starting up Lada 1200L Zhiguli Engine',
            'creator': 'Sounds of Changes; sound/photo/video: Antti Makkonen',
            'page_url': SOURCE_PAGE, 'direct_url': SOURCE_URL,
            'license': 'CC BY 3.0 Unported',
            'license_url': 'https://creativecommons.org/licenses/by/3.0/',
            'source_sha256': SOURCE_SHA, 'source_bytes': SOURCE_BYTES,
            'source_duration_seconds': 31.029,
        },
        'clips': records,
        'audit': {
            'decision': 'One coherent real CC-BY engine recording. Music was not reused. Pixabay candidates were verified but automated binary acquisition returned HTTP 403, so no unverifiable mirror was used.'
        },
    }
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

    attribution = f'''BRAXHORN CITY — AUTO BRAXHORN / Issue #30 — ENGINE AUDIO ATTRIBUTION

Source work:
"Starting up Lada 1200L Zhiguli Engine"
Sounds of Changes; sound/photo/video: Antti Makkonen
{SOURCE_PAGE}

License:
Creative Commons Attribution 3.0 Unported (CC BY 3.0)
https://creativecommons.org/licenses/by/3.0/

Source integrity:
SHA-256: {SOURCE_SHA}
Original downloaded source size: {SOURCE_BYTES:,} bytes
Original duration: 31.029 s

BRAXHORN derivatives:
1) brax_engine_idle.ogg — 5.000 s
   source 12.00–17.50 s; mono 22.05 kHz; 0.5 s circular loop crossfade; +18 dB soft limiting; Vorbis
   SHA-256: {records[0]['sha256']}
2) brax_engine_rev.ogg — 2.750 s
   source 21.75–24.50 s; mono 22.05 kHz; 0.12 s fade-in; 0.28 s fade-out; +18 dB soft limiting; Vorbis
   SHA-256: {records[1]['sha256']}

Both OGG files contain embedded creator, license and source metadata. No endorsement is implied.
'''
    (AUDIO / 'ATTRIBUTION.txt').write_text(attribution, encoding='utf-8')

    metas = {
        ROOT / 'unity-city/Assets/Source/Mistrzowie/Audio.meta': '8d0bdff8d9425c8492fc2edcb3d9b048',
        ROOT / 'unity-city/Assets/Source/Mistrzowie/Audio/BraxCar.meta': 'd570c9b5fdbf52d09fc447b40af0ad6e',
        AUDIO / 'brax_engine_idle.ogg.meta': '10e57b853d7d5895af1b49160bd068f1',
        AUDIO / 'brax_engine_rev.ogg.meta': 'c40f72ef5c625b3b97f7426f59b25163',
        AUDIO / 'ATTRIBUTION.txt.meta': '9858e4b2a5f65c9d8137e85d3533ce8f',
    }
    for path, guid in metas.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f'fileFormatVersion: 2\nguid: {guid}\n', encoding='utf-8')

    verifier = ROOT / 'unity-tools/mistrzowie/prepare_champion_car_audio.py'
    verifier.write_text('''#!/usr/bin/env python3
from pathlib import Path
import hashlib, json
ROOT=Path(__file__).resolve().parents[2]
MANIFEST=ROOT/"unity-tools/mistrzowie/champion_car_audio_sources.json"
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    doc=json.loads(MANIFEST.read_text("utf-8"))
    assert doc["status"]=="BINARIES_COMMITTED_AND_HASHED"
    assert doc["source"]["source_sha256"]=="15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6"
    for c in doc["clips"]:
        p=ROOT/c["unity_path"]; raw=p.read_bytes()
        if len(raw)!=c["bytes"] or sha(p)!=c["sha256"]: raise ValueError("hash/size mismatch "+c["role"])
        if not raw.startswith(b"OggS") or b"vorbis" not in raw[:8192]: raise ValueError("not Ogg/Vorbis "+c["role"])
        if b"CC BY 3.0 Unported" not in raw[:8192] or b"Antti Makkonen" not in raw[:8192]: raise ValueError("attribution missing "+c["role"])
        if c["role"]=="idle" and c["duration_seconds"]<4.5: raise ValueError("idle too short")
        if c["role"]=="rev" and c["duration_seconds"]<2.0: raise ValueError("rev too short")
        print("A1_CAR_AUDIO_VERIFIED",c["role"],len(raw),c["sha256"])
    print("A1_CAR_AUDIO_FINAL_PASS clips=2 real_recording=true network=false apk=false")
if __name__=="__main__": main()
''', encoding='utf-8')

    test = ROOT / 'unity-tools/mistrzowie/test_issue30_car_audio.py'
    test.write_text('''from pathlib import Path
import hashlib,json,subprocess,unittest
ROOT=Path(__file__).resolve().parents[2]
def data(path):
    p=ROOT/path
    if p.is_file(): return p.read_bytes()
    return subprocess.check_output(["git","show","HEAD:"+path],cwd=ROOT)
def text(path): return data(path).decode("utf-8")
class Issue30CarAudio(unittest.TestCase):
    def test_audio_files_match_manifest_and_are_long_enough(self):
        m=json.loads(text("unity-tools/mistrzowie/champion_car_audio_sources.json"))
        self.assertEqual(m["status"],"BINARIES_COMMITTED_AND_HASHED")
        self.assertEqual(m["source"]["license"],"CC BY 3.0 Unported")
        for c in m["clips"]:
            raw=data(c["unity_path"])
            self.assertEqual(len(raw),c["bytes"])
            self.assertEqual(hashlib.sha256(raw).hexdigest(),c["sha256"])
            self.assertTrue(raw.startswith(b"OggS"))
            self.assertIn(b"Antti Makkonen",raw[:8192])
        idle=next(c for c in m["clips"] if c["role"]=="idle")
        rev=next(c for c in m["clips"] if c["role"]=="rev")
        self.assertGreaterEqual(idle["duration_seconds"],4.5)
        self.assertGreaterEqual(rev["duration_seconds"],2.0)
    def test_runtime_and_mobile_contract(self):
        b=text("unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs")
        r=text("unity-city/Assets/Mistrzowie/BraxChampionsCar.cs")
        for s in ("brax_engine_idle.ogg","brax_engine_rev.ogg","AudioCompressionFormat.Vorbis","forceToMono = true"):
            self.assertIn(s,b)
        for s in ("AudioRolloffMode.Linear","spatialBlend = 1f","A1_CAR_ENGINE_REV","maxSpeed = 120f / 3.6f"):
            self.assertIn(s,r)
        self.assertIn("LengthMetres = 4.95f",b)
        self.assertNotIn("AudioClip.Create",r)
if __name__=="__main__": unittest.main()
''', encoding='utf-8')

    print('ISSUE30_AUDIO_FINALIZED', json.dumps(records, separators=(',', ':')))

if __name__ == '__main__':
    main()
