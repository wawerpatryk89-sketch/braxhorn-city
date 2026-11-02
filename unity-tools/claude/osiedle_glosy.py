#!/usr/bin/env python3
"""Generate Polish estate dialogue offline. Requires piper-tts==1.3.0 and ffmpeg.

Download pl_PL-darkman-medium.onnx[.json] and pl_PL-gosia-medium.onnx[.json]
from https://huggingface.co/rhasspy/piper-voices at revision
b4e358ffcb232591bf73a540e70f7de528e4174a into --models. Model cards identify
the Polish source datasets as CC0: https://github.com/OHF-Voice/voice-datasets
Models and Piper are development tools, not included in the game; outputs are Ogg.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import tempfile
import uuid
import wave


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'unity-city/Assets/Resources/OsiedleGlosy'


def dialogue():
    runtime = ROOT / 'unity-city/Assets/Claude/Runtime'
    text = (runtime / 'BraxDymki.cs').read_text()
    parts = [text[text.index('public static readonly string[] Powitania'):]]
    text = (runtime / 'BraxLudzie.cs').read_text()
    parts.append(text[text.index('public static readonly string[] Okrzyki'):])
    text = (runtime / 'BraxMieszkaniec.cs').read_text()
    text = text[text.index('string Powitanie()'):text.index('// AKCJA:')]
    parts += re.findall(r'new\[\] \{([^}]+)\}', text)
    texts = sorted(set(re.findall(r'"([^"\n]+)"', '\n'.join(parts))))
    return [{'tekst': t, 'id': hashlib.sha256(t.encode()).hexdigest()[:16]} for t in texts]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--models', type=Path)
    parser.add_argument('--check', action='store_true', help='Check dialogue coverage and decode every Ogg')
    args = parser.parse_args()
    entries = dialogue()
    if args.check:
        saved = json.loads((OUTPUT / 'indeks.json').read_text())['kwestie']
        assert saved == entries, 'Dialogue index is out of date'
        for e in entries:
            for gender in ('m', 'f'):
                path = OUTPUT / (e['id'] + '_' + gender + '.ogg')
                assert path.is_file() and Path(str(path) + '.meta').is_file(), path
                subprocess.run(['ffmpeg', '-v', 'error', '-xerror', '-i', str(path), '-f', 'null', '-'], check=True, capture_output=True)
        print(f'PASS: {len(entries)} phrases, {len(entries) * 2} decodable Polish recordings')
        return
    if not args.models:
        parser.error('--models is required for generation')
    from piper import PiperVoice
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / 'indeks.json').write_text(json.dumps({'kwestie': entries}, ensure_ascii=False, indent=2))
    with tempfile.TemporaryDirectory() as tmp:
        wav_path = Path(tmp) / 'voice.wav'
        for gender, model in (('m', 'darkman'), ('f', 'gosia')):
            voice = PiperVoice.load(str(args.models / f'pl_PL-{model}-medium.onnx'))
            for e in entries:
                destination = OUTPUT / (e['id'] + '_' + gender + '.ogg')
                if destination.exists() and destination.stat().st_size > 0:
                    continue
                with wave.open(str(wav_path), 'wb') as wav:
                    voice.synthesize_wav(e['tekst'], wav)
                subprocess.run(['ffmpeg', '-v', 'error', '-y', '-i', str(wav_path), '-c:a', 'libvorbis', '-q:a', '3', str(destination)], check=True)
    for path in OUTPUT.iterdir():
        if path.suffix in ('.ogg', '.json'):
            meta = Path(str(path) + '.meta')
            if not meta.exists():
                meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')


if __name__ == '__main__':
    main()
