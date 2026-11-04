"""Restore exactly the reviewed Piper generation; never generate again during APK builds."""
import hashlib
import json
import re
from pathlib import Path
import shutil
import subprocess
import tempfile
import uuid
import wave
import zipfile

REPO='wawerpatryk89-sketch/braxhorn-city'
ARTIFACT=13328337833
SHA256='35df4ccdafdff17c9ba1dd7b8119272d53c74fb84c0059f744849e389ac3d5a6'
RUN=41841011462


def unpack(archive, target):
    if hashlib.sha256(archive.read_bytes()).hexdigest()!=SHA256: raise ValueError('Dialogue ZIP mismatch')
    with zipfile.ZipFile(archive) as z:
        names=z.namelist()
        if len(names)!=len(set(names)) or len(names)!=52 or sum(i.file_size for i in z.infolist())>25_000_000:
            raise ValueError('Unexpected dialogue archive')
        for name in names:
            if Path(name).name!=name or name.startswith('.') or Path(name).suffix not in ('.json','.txt','.wav'):
                raise ValueError('Unsafe dialogue member')
        z.extractall(target)
    report=json.loads((target/'dialogue.json').read_text())
    if report['engine']!='piper-tts==1.3.0' or report['paidApiUsed'] or len(report['clips'])!=47:
        raise ValueError('Unexpected voice catalog')
    for clip in report['clips']:
        path=target/clip['file']
        if clip['file'] not in names or hashlib.sha256(path.read_bytes()).hexdigest()!=clip['sha256']:
            raise ValueError('Dialogue clip mismatch')
        with wave.open(str(path),'rb') as w:
            if w.getnchannels()!=1 or w.getsampwidth()!=2 or not .4<w.getnframes()/w.getframerate()<15:
                raise ValueError('Invalid dialogue PCM')
    return report


def main():
    target=Path('unity-city/Assets/Source/Prywatne/Oboz/audio');target.mkdir(parents=True,exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='camp-dialogue-restore-') as temp:
        temp=Path(temp);archive=temp/'camp-dialogue.zip'
        with archive.open('wb') as stream:
            downloaded=subprocess.run(['gh','api',f'repos/{REPO}/actions/artifacts/{ARTIFACT}/zip'],stdout=stream).returncode==0
        if not downloaded:
            # Subsequent builds can recover the same immutable recording from the prior draft release.
            subprocess.run(['gh','release','download','oboz-review-latest','--repo',REPO,'--pattern','release-identity.json','--dir',str(temp)],check=True)
            identity=json.loads((temp/'release-identity.json').read_text())
            tag=identity['releaseTag']
            if identity.get('dialogueSha256')!=SHA256 or (tag!='oboz-review-latest' and not re.fullmatch(r'oboz-review-[0-9]+',tag)):
                raise ValueError('Previous release does not contain the pinned recording')
            asset=identity.get('dialogueAssetName','camp-dialogue.zip')
            if not re.fullmatch(r'camp-dialogue(?:-[0-9]+-[0-9a-f]{7})?\.zip',asset):
                raise ValueError('Invalid pinned dialogue asset name')
            archive.unlink(missing_ok=True)
            subprocess.run(['gh','release','download',tag,'--repo',REPO,'--pattern',asset,'--dir',str(temp)],check=True)
            if asset!=archive.name: (temp/asset).replace(archive)
        stage=temp/'stage';stage.mkdir();report=unpack(archive,stage)
        for source in stage.iterdir():
            dest=target/source.name;shutil.copyfile(source,dest)
            meta=Path(str(dest)+'.meta')
            if not meta.exists():
                guid=uuid.uuid5(uuid.NAMESPACE_URL,'braxhorn://oboz/audio/'+source.name).hex
                meta.write_text('fileFormatVersion: 2\nguid: '+guid+'\n')
        # Retained in the versioned draft release so the recording is not lost with Actions retention.
        package=Path('build/ObozAudio');package.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(archive,package/'camp-dialogue.zip')
    credit=Path('unity-city/Assets/Claude/Resources/braxhorn_autorzy.txt')
    credit.parent.mkdir(parents=True,exist_ok=True)
    current=credit.read_text() if credit.exists() else ''
    marker='\nGŁOSY OBOZU — PIPER 1.3.0\n'
    current=current.split(marker)[0]
    notices='\n\n'.join(p.stem+'\n'+p.read_text() for p in sorted(target.glob('*-MODEL_CARD.txt')))
    credit.write_text(current+marker+'Oryginalne kwestie gry, synteza lokalna.\n'+notices+'\n')
    print(f'BRAXHORN_OBOZ_AUDIO restored=47 run={RUN} sha256={SHA256} paid_api=false quality_approved=false')


if __name__=='__main__': main()
