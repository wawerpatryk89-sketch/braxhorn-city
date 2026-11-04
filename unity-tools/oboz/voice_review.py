"""Audition the existing Piper engine; no paid API, voice cloning or game overwrite.

Engine matches osiedle_glosy.py at 5ea83909a1aa58b74fb9fa54cbe23a5087a3e087.
Downloads only pinned public Polish voice models. Models never enter the APK or
review artifact. Output is original camp dialogue for quality comparison.
"""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import urllib.request
import wave

REVISION = '6428fc718959cf446b00df24966c0185b7f7fcbe'
BASE = 'https://huggingface.co/rhasspy/piper-voices/resolve/' + REVISION + '/'
TEXTS = [
    'Dobra, kończymy rundę. Zmieńcie się przy workach, a ja sprawdzę, jak wam idzie.',
    'Jeszcze jedna runda? Tym razem spróbuję wejść bliżej.',
    'Jasne. Tylko spokojnie, ćwiczymy technikę. Gotowy?',
]
VOICES = ['pl_PL-darkman-medium', 'pl_PL-gosia-medium', 'pl_PL-bass-high', 'pl_PL-mc_speech-medium']


def download(url, path):
    with urllib.request.urlopen(url, timeout=90) as response, path.open('wb') as output:
        shutil.copyfileobj(response, output)


def sha(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main():
    from piper import PiperVoice, SynthesisConfig
    output = Path('build/CampVoiceReview'); output.mkdir(parents=True, exist_ok=True)
    report = {'engine': 'piper-tts==1.3.0', 'revision': REVISION, 'paidApiUsed': False,
              'qualityApproved': False, 'gameChanged': False, 'voices': []}
    with tempfile.TemporaryDirectory(prefix='camp-voice-review-') as temporary:
        temp = Path(temporary)
        index_path = temp/'voices.json'; download(BASE+'voices.json', index_path)
        index = json.loads(index_path.read_text())
        for key in VOICES:
            entry = index[key]; folder = temp/key; folder.mkdir()
            receipts = []
            prefix = 'pl/pl_PL/' + entry['name'] + '/' + entry['quality'] + '/'
            for suffix in [key+'.onnx', key+'.onnx.json', 'MODEL_CARD']:
                relative = prefix+suffix
                record = entry['files'][relative]
                path = folder/suffix; download(BASE+relative, path)
                if path.stat().st_size != record['size_bytes'] or hashlib.md5(path.read_bytes()).hexdigest() != record['md5_digest']:
                    raise ValueError('Pinned voice data mismatch: '+relative)
                receipts.append({'file': relative, 'sha256': sha(path), 'bytes': path.stat().st_size})
            # Preserve the model's own notice next to the original generated samples.
            shutil.copyfile(folder/'MODEL_CARD', output/(key+'-MODEL_CARD.txt'))
            voice = PiperVoice.load(str(folder/(key+'.onnx')))
            samples = []
            # Compare default prosody with moderate adjustment; no pitch distortion.
            for variant, config in [('default',None), ('natural', SynthesisConfig(length_scale=1.0,noise_scale=.55,noise_w_scale=.7))]:
                for number, text in enumerate(TEXTS):
                    raw = folder/'raw.wav'
                    with wave.open(str(raw),'wb') as stream:
                        voice.synthesize_wav(text, stream, syn_config=config)
                    target = output/f'{key}-{variant}-{number+1}.wav'
                    subprocess.run(['ffmpeg','-v','error','-y','-i',str(raw),'-af',
                        'highpass=f=65,loudnorm=I=-18:TP=-2:LRA=11', '-ar', str(voice.config.sample_rate),
                        '-ac','1','-c:a','pcm_s16le',str(target)],check=True)
                    with wave.open(str(target),'rb') as stream:
                        duration=stream.getnframes()/stream.getframerate()
                        if stream.getnchannels()!=1 or not .5<duration<20: raise ValueError('Invalid sample')
                    samples.append({'file':target.name,'text':text,'variant':variant,'seconds':duration,'sha256':sha(target)})
            report['voices'].append({'id':key,'models':receipts,'samples':samples})
            del voice
    (output/'voice-review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('PIPER_REVIEW voices=4 samples=24 paid_api=false quality_approved=false game_changed=false')


if __name__ == '__main__': main()
