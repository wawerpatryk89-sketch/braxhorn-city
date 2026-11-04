"""Original camp lines rendered offline by the same Piper 1.3.0 as the estate.

No paid service and no voice cloning. Technical checks do not approve acting.
"""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import wave
from voice_review import BASE, REVISION, download, sha

NPC = {
    'spar0': 'Jeszcze jedna runda? Tym razem spróbuję wejść bliżej.',
    'spar1': 'Dobra, ale spokojnie. Ćwiczymy technikę.',
    'spar2': 'Lepiej pilnujesz gardy. Spróbujmy teraz trochę szybciej.',
    'spar3': 'Jasne. Tylko daj mi chwilę złapać oddech.',
    'bag0': 'Po tej serii zamieniamy się miejscami?',
    'bag1': 'Pewnie. Dokończę kombinację i jestem gotowy.',
    'rest0': 'Jak ci poszła ostatnia runda?',
    'rest1': 'Nieźle. Muszę jeszcze popracować nad nogami.',
    'rest2': 'Dzisiaj mocny trening. Potem przyda się trochę odpoczynku.',
    'rest3': 'Zgadza się. Jeszcze worek i na dzisiaj wystarczy.',
    'ack0': 'Jasne, już idę.',
    'ack1': 'Dobra, zmieniam stanowisko.',
    'challenge0': 'Masz ochotę na lekką rundę?',
    'challenge1': 'Sprawdźmy technikę. Bez szarżowania.',
}
MONIKA = {
    'orderRest': 'Dobra robota. Teraz chwila odpoczynku.',
    'orderSpar': 'Przejdź do klatki. Zróbcie spokojną rundę techniczną.',
    'orderBag': 'Teraz twoja kolej przy worku. Pilnuj pracy nóg.',
    'coach0': 'Spokojnie, bez pośpiechu. Najpierw dokładność.',
    'coach1': 'Dobrze wam idzie. Pamiętajcie o oddechu.',
}


def main():
    from piper import PiperVoice, SynthesisConfig
    root = Path('build/CampDialogue'); root.mkdir(parents=True, exist_ok=True)
    report = {'engine': 'piper-tts==1.3.0', 'revision': REVISION, 'paidApiUsed': False,
              'qualityApproved': False, 'voices': [], 'clips': []}
    with tempfile.TemporaryDirectory(prefix='camp-dialogue-') as temporary:
        temp = Path(temporary)
        index_path = temp/'voices.json'; download(BASE+'voices.json', index_path)
        index = json.loads(index_path.read_text())
        for key in ['pl_PL-gosia-medium','pl_PL-darkman-medium','pl_PL-bass-high','pl_PL-mc_speech-medium']:
            entry = index[key]; folder = temp/key; folder.mkdir()
            prefix = 'pl/pl_PL/'+entry['name']+'/'+entry['quality']+'/'
            receipts = []
            for suffix in [key+'.onnx',key+'.onnx.json','MODEL_CARD']:
                relative = prefix+suffix; path = folder/suffix
                download(BASE+relative,path); record=index[key]['files'][relative]
                if path.stat().st_size!=record['size_bytes'] or hashlib.md5(path.read_bytes()).hexdigest()!=record['md5_digest']:
                    raise ValueError('Voice source mismatch: '+relative)
                receipts.append({'file':relative,'sha256':sha(path)})
            shutil.copyfile(folder/'MODEL_CARD',root/(key+'-MODEL_CARD.txt'))
            voice=PiperVoice.load(str(folder/(key+'.onnx')))
            config=SynthesisConfig(length_scale=1.0,noise_scale=.55,noise_w_scale=.7)
            lines=MONIKA if 'gosia' in key else NPC
            for name,text in lines.items():
                raw=folder/'raw.wav'
                with wave.open(str(raw),'wb') as stream: voice.synthesize_wav(text,stream,syn_config=config)
                target=root/(key+'-'+name+'.wav')
                subprocess.run(['ffmpeg','-v','error','-y','-i',str(raw),'-af',
                    'highpass=f=65,loudnorm=I=-18:TP=-2:LRA=11','-ar',str(voice.config.sample_rate),
                    '-ac','1','-c:a','pcm_s16le',str(target)],check=True)
                with wave.open(str(target),'rb') as stream:
                    duration=stream.getnframes()/stream.getframerate()
                    if stream.getnchannels()!=1 or not .4<duration<15: raise ValueError('Invalid voice clip')
                report['clips'].append({'id':name,'voice':key,'file':target.name,'text':text,'seconds':duration,'sha256':sha(target)})
            report['voices'].append({'id':key,'models':receipts})
    (root/'dialogue.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('CAMP_DIALOGUE clips='+str(len(report['clips']))+' paid_api=false quality_approved=false')


if __name__=='__main__': main()
