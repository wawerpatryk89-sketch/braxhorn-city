"""Upload immutable camp assets to the existing draft; never create a release/tag."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

from assets import digest
from release_identity import PACKAGE
from recover_apk import CERTIFICATE

TAG = 'oboz-review-latest'


def publish(folder, dialogue, world, repo, source_sha):
    folder, dialogue, world = Path(folder), Path(dialogue), Path(world)
    receipt = folder / 'release-identity.json'
    data = json.loads(receipt.read_text())
    if (data.get('applicationId') != PACKAGE or data.get('sourceCommit') != source_sha
            or not re.fullmatch(r'[0-9a-f]{40}', source_sha)
            or data.get('signingCertificateSha256') != CERTIFICATE
            or type(data.get('versionCode')) is not int or data['versionCode'] <= 0):
        raise ValueError('Unverified camp APK identity')
    apk = folder / 'braxhorn-oboz.apk'
    if digest(apk) != data.get('apkSha256'):
        raise ValueError('APK changed after signature verification')
    if not dialogue.is_file():
        raise ValueError('Missing verified dialogue archive')
    release = json.loads(subprocess.check_output(
        ['gh', 'release', 'view', TAG, '--repo', repo, '--json', 'isDraft,assets'], text=True))
    if not release.get('isDraft'):
        raise ValueError('Camp delivery must remain a draft release')
    suffix = str(data['versionCode']) + '-' + source_sha[:7]
    data.update(releaseTag=TAG, apkAssetName='braxhorn-oboz-' + suffix + '.apk',
                dialogueAssetName='camp-dialogue-' + suffix + '.zip',
                dialogueSha256=digest(dialogue), dialogueEngine='piper-tts==1.3.0',
                contentUpdatesReady=False, phoneTested=False, voiceQualityApproved=False)
    payload = [(apk, data['apkAssetName']), (dialogue, data['dialogueAssetName'])]
    if world.is_file():
        data.update(worldAssetName='oboz-world-' + suffix + '.tar.gz.enc', worldSha256=digest(world))
        payload.append((world, data['worldAssetName']))
    else:
        data.pop('worldSha256', None)
        data.pop('worldAssetName', None)
    for name in ('build-receipt', 'scene-validation'):
        p = folder / (name + '.json')
        if not p.is_file(): raise ValueError('Missing build report: ' + name)
        payload.append((p, name + '-' + suffix + '.json'))
    identity_name = 'release-identity-' + suffix + '.json'
    names = {name for _, name in payload} | {identity_name}
    if names & {asset['name'] for asset in release.get('assets', [])}:
        raise ValueError('Immutable delivery asset already exists; inspect it before any retry')
    serialized = json.dumps(data, indent=2) + '\n'
    with tempfile.TemporaryDirectory(prefix='oboz-release-') as temp:
        temp = Path(temp)
        for original, name in payload:
            dest = temp / name
            try: os.link(original, dest)
            except OSError: shutil.copyfile(original, dest)
        (temp / identity_name).write_text(serialized)
        subprocess.run(['gh', 'release', 'upload', TAG, '--repo', repo,
                        *[str(temp / name) for name in sorted(names)]], check=True)
        # The only mutable pointer moves after every immutable asset uploaded.
        receipt.write_text(serialized)
        subprocess.run(['gh', 'release', 'upload', TAG, '--repo', repo,
                        '--clobber', str(receipt)], check=True)
    return data


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--folder', required=True)
    parser.add_argument('--dialogue', required=True)
    parser.add_argument('--world', required=True)
    args = parser.parse_args()
    result = publish(args.folder, args.dialogue, args.world, os.environ['GITHUB_REPOSITORY'], os.environ['GITHUB_SHA'])
    print('BRAXHORN_OBOZ_DELIVERY asset=' + result['apkAssetName'] + ' phone_tested=false')
