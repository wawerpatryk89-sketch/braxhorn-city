#!/usr/bin/env python3
"""Finalize licensed Issue #30 BRAXHORN engine audio from one audited source file.

Input is a local copy of the verified Wikimedia Commons WebM. This script does
not download anything and never runs Unity or builds APK. It verifies the
original SHA-256, derives mono idle/rev OGG clips, writes attribution + manifest,
and updates the offline verifier/regression test.
"""
from array import array
from pathlib import Path
import hashlib
import json
import subprocess
import sys
import wave

ROOT = Path(__file__).resolve().parents[2]
EXPECTED_SOURCE_SHA = "15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6"
AUDIO = ROOT / "unity-city/Assets/Source/Mistrzowie/Audio/BraxCar"
MANIFEST = ROOT / "unity-tools/mistrzowie/champion_car_audio_sources.json"

def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda:f.read(1024*1024),b""): h.update(block)
    return h.hexdigest()

def run(*args):
    subprocess.run(list(args), check=True)

def write_wav(path, rate, samples):
    out=array("h",(max(-32767,min(32767,round(v*32767))) for v in samples))
    if sys.byteorder!="little": out.byteswap()
    with wave.open(str(path),"wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(rate); w.writeframes(out.tobytes())

def normalize(samples,target):
    peak=max(max(abs(v) for v in samples),1e-9)
    gain=target/peak
    return [max(-.98,min(.98,v*gain)) for v in samples]

def main(source):
    source=Path(source)
    if not source.is_file(): raise FileNotFoundError(source)
    if sha(source)!=EXPECTED_SOURCE_SHA: raise ValueError("Issue #30 source SHA-256 mismatch")
    work=source.parent/"issue30-derived"
    work.mkdir(parents=True,exist_ok=True)
    mono=work/"source_mono_22050.wav"
    run("ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(source),
        "-vn","-ac","1","-ar","22050","-c:a","pcm_s16le",str(mono))
    with wave.open(str(mono),"rb") as w:
        if (w.getnchannels(),w.getsampwidth(),w.getframerate())!=(1,2,22050):
            raise ValueError("Unexpected decoded PCM format")
        rate=w.getframerate()
        pcm=array("h"); pcm.frombytes(w.readframes(w.getnframes()))
        if sys.byteorder!="little": pcm.byteswap()
    x=[v/32768.0 for v in pcm]
    if len(x)<int(25*rate): raise ValueError("Audited source is too short")

    idle=x[int(12.0*rate):int(17.5*rate)]
    f=int(.5*rate)
    seam=[idle[-f+i]*(1-i/max(1,f-1))+idle[i]*(i/max(1,f-1)) for i in range(f)]
    idle=normalize(seam+idle[f:-f],.63)
    rev=normalize(x[int(21.75*rate):int(24.50*rate)],.79)
    fi,fo=int(.12*rate),int(.28*rate)
    for i in range(fi): rev[i]*=i/max(1,fi-1)
    for i in range(fo): rev[-fo+i]*=(fo-1-i)/max(1,fo-1)

    idle_wav,rev_wav=work/"idle.wav",work/"rev.wav"
    write_wav(idle_wav,rate,idle); write_wav(rev_wav,rate,rev)
    AUDIO.mkdir(parents=True,exist_ok=True)
    idle_ogg=AUDIO/"brax_engine_idle.ogg"
    rev_ogg=AUDIO/"brax_engine_rev.ogg"
    common=[
        "-metadata","artist=Sounds of Changes / Antti Makkonen",
        "-metadata","license=CC BY 3.0 Unported",
        "-metadata","source=https://commons.wikimedia.org/wiki/File:Starting_up_Lada_1200L_Zhiguli_Engine.webm",
    ]
    run("ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(idle_wav),
        "-c:a","libvorbis","-q:a","4","-metadata","title=BRAXHORN engine idle - derivative",
        *common,str(idle_ogg))
    run("ffmpeg","-hide_banner","-loglevel","error","-y","-i",str(rev_wav),
        "-c:a","libvorbis","-q:a","4","-metadata","title=BRAXHORN engine rev - derivative",
        *common,str(rev_ogg))
    if idle_ogg.stat().st_size<15000 or rev_ogg.stat().st_size<10000:
        raise ValueError("Derived OGG unexpectedly small")

    manifest={
      "schema":3,"status":"FINAL_LICENSED_AUDIO_COMMITTED","issue":30,
      "source":{
        "title":"Starting up Lada 1200L Zhiguli Engine",
        "creator":"Sounds of Changes; sound/photo/video: Antti Makkonen",
        "page_url":"https://commons.wikimedia.org/wiki/File:Starting_up_Lada_1200L_Zhiguli_Engine.webm",
        "direct_url":"https://upload.wikimedia.org/wikipedia/commons/3/33/Starting_up_Lada_1200L_Zhiguli_Engine.webm",
        "license":"CC BY 3.0 Unported","license_url":"https://creativecommons.org/licenses/by/3.0/",
        "source_sha256":EXPECTED_SOURCE_SHA,"source_bytes":source.stat().st_size
      },
      "clips":[
        {"role":"idle","unity_path":idle_ogg.relative_to(ROOT).as_posix(),"sha256":sha(idle_ogg),
         "bytes":idle_ogg.stat().st_size,"duration_seconds":len(idle)/rate,"sample_rate_hz":rate,
         "channels":1,"source_segment_seconds":[12.0,17.5],
         "processing":"mono; 22.05 kHz; 0.5 s circular seam crossfade; peak normalization; Vorbis"},
        {"role":"rev","unity_path":rev_ogg.relative_to(ROOT).as_posix(),"sha256":sha(rev_ogg),
         "bytes":rev_ogg.stat().st_size,"duration_seconds":len(rev)/rate,"sample_rate_hz":rate,
         "channels":1,"source_segment_seconds":[21.75,24.5],
         "processing":"mono; 22.05 kHz; 0.12 s fade-in; 0.28 s fade-out; peak normalization; Vorbis"}
      ],
      "note":"Issue #30 allowed another single coherent licensed recording when documented. Pixabay candidates were verified but their CDN blocked automated binary retrieval with HTTP 403; no mirrored/unverified Pixabay bytes were used."
    }
    MANIFEST.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    (AUDIO/"ATTRIBUTION.txt").write_text("""BRAXHORN CITY — AUTO BRAXHORN / Issue #30 — ENGINE AUDIO ATTRIBUTION

Source work: "Starting up Lada 1200L Zhiguli Engine"
Creator / attribution: Sounds of Changes; sound/photo/video: Antti Makkonen
Source: https://commons.wikimedia.org/wiki/File:Starting_up_Lada_1200L_Zhiguli_Engine.webm
License: Creative Commons Attribution 3.0 Unported (CC BY 3.0)
License: https://creativecommons.org/licenses/by/3.0/
Original source SHA-256: 15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6

Modifications for BRAXHORN CITY:
- downmixed to mono and resampled to 22.05 kHz;
- idle: 12.00–17.50 s, circular seam crossfade, level adjustment, Vorbis;
- rev: 21.75–24.50 s, fade-in/out, level adjustment, Vorbis.
No endorsement by the creator is implied.
""",encoding="utf-8")

    (ROOT/"unity-tools/mistrzowie/prepare_champion_car_audio.py").write_text("""#!/usr/bin/env python3
from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
DOC=json.loads((ROOT/'unity-tools/mistrzowie/champion_car_audio_sources.json').read_text('utf-8'))
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
if DOC.get('status')!='FINAL_LICENSED_AUDIO_COMMITTED': raise ValueError('audio manifest not final')
if DOC['source']['source_sha256']!='15a9fcf82f53c710f489b7d888557b4937ae3f50d968c22f2be342d29df398b6': raise ValueError('source hash mismatch')
for rec in DOC['clips']:
    p=ROOT/rec['unity_path']; data=p.read_bytes()
    if not data.startswith(b'OggS'): raise ValueError('not OGG: '+str(p))
    if len(data)!=rec['bytes'] or sha(p)!=rec['sha256']: raise ValueError('hash/size mismatch: '+rec['role'])
    if rec['sample_rate_hz']!=22050 or rec['channels']!=1: raise ValueError('mobile audio contract mismatch')
    print('A1_CAR_AUDIO_VERIFIED',rec['role'],rec['bytes'],rec['sha256'])
print('A1_CAR_AUDIO_FINAL_PASS clips=2 source=CC_BY_3_0 apk=false unity=false')
""",encoding="utf-8")

    (ROOT/"unity-tools/mistrzowie/test_issue30_car_audio.py").write_text("""from pathlib import Path
import hashlib,json,unittest
ROOT=Path(__file__).resolve().parents[2]
def txt(p): return (ROOT/p).read_text('utf-8')
class Issue30Audio(unittest.TestCase):
    def test_real_hashed_audio_and_attribution(self):
        doc=json.loads(txt('unity-tools/mistrzowie/champion_car_audio_sources.json'))
        self.assertEqual(doc['status'],'FINAL_LICENSED_AUDIO_COMMITTED')
        self.assertEqual(doc['source']['license'],'CC BY 3.0 Unported')
        self.assertIn('Antti Makkonen',doc['source']['creator'])
        for rec in doc['clips']:
            p=ROOT/rec['unity_path']; data=p.read_bytes()
            self.assertTrue(data.startswith(b'OggS')); self.assertGreater(len(data),10000)
            self.assertEqual(len(data),rec['bytes'])
            self.assertEqual(hashlib.sha256(data).hexdigest(),rec['sha256'])
        notice=txt('unity-city/Assets/Source/Mistrzowie/Audio/BraxCar/ATTRIBUTION.txt')
        self.assertIn('CC BY 3.0',notice); self.assertIn('Antti Makkonen',notice)
    def test_runtime_and_mobile_contract(self):
        b=txt('unity-city/Assets/Editor/Mistrzowie/BraxChampionsCarBuilder.cs')
        r=txt('unity-city/Assets/Mistrzowie/BraxChampionsCar.cs')
        self.assertIn('brax_engine_idle.ogg',b); self.assertIn('brax_engine_rev.ogg',b)
        self.assertIn('AudioCompressionFormat.Vorbis',b); self.assertIn('forceToMono = true',b)
        self.assertIn('AudioRolloffMode.Linear',r); self.assertIn('spatialBlend = 1f',r)
        self.assertIn('A1_CAR_ENGINE_REV',r); self.assertNotIn('AudioClip.Create("BRAXHORN',r)
        self.assertIn('LengthMetres = 4.95f',b); self.assertIn('car.maxSpeed = 120f / 3.6f',b)
        self.assertIn('maxSpeed = 120f / 3.6f',r)
if __name__=='__main__': unittest.main()
""",encoding="utf-8")
    print("ISSUE30_AUDIO_DERIVED",
          "idle",idle_ogg.stat().st_size,sha(idle_ogg),
          "rev",rev_ogg.stat().st_size,sha(rev_ogg))

if __name__=="__main__":
    if len(sys.argv)!=2: raise SystemExit("usage: finalize_issue30_audio.py SOURCE.webm")
    main(sys.argv[1])
