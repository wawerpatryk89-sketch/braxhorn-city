#!/usr/bin/env python3
"""Recover an already signed APK from Bee; never invoke Unity, Gradle or signing."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

from release_identity import PACKAGE, verify_metadata

CERTIFICATE = 'f3829d496e1bf4890ec64af78170c7e8b8696d1e49d4122df2b4fc1c80bd5ffe'


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def recover(candidate, receipt, output, source_sha, aapt, apksigner):
    candidate, receipt, output = Path(candidate), Path(receipt), Path(output)
    data = json.loads(receipt.read_text())
    if (not isinstance(data, dict) or not re.fullmatch(r'[0-9a-f]{40}', source_sha)
            or data.get('sourceCommit') != source_sha
            or data.get('applicationId') != PACKAGE
            or data.get('signingCertificateSha256') != CERTIFICATE
            or not isinstance(data.get('apkSha256'), str)
            or not re.fullmatch(r'[0-9a-f]{64}', data['apkSha256'])):
        raise ValueError('Receipt does not identify the requested signed camp APK')
    if candidate.is_symlink() or not candidate.is_file():
        raise ValueError('The original APK is missing from the restored Bee cache')
    expected = data['apkSha256']
    if digest(candidate) != expected:
        raise ValueError('Cached APK bytes differ from the completed build receipt')
    badging = subprocess.check_output([aapt, 'dump', 'badging', str(candidate)], text=True)
    signing = subprocess.check_output([apksigner, 'verify', '--print-certs', str(candidate)], text=True)
    actual = verify_metadata(badging, signing, data.get('versionCode'))
    if actual['signingCertificateSha256'] != CERTIFICATE:
        raise ValueError('Recovered APK has a different signing certificate')
    output.mkdir(parents=True, exist_ok=True)
    target = output / 'braxhorn-oboz.apk'
    if target.is_symlink() or target.exists() and digest(target) != expected:
        raise ValueError('Refusing to overwrite a different APK')
    with tempfile.TemporaryDirectory(prefix='.recover-', dir=output) as folder:
        copy = Path(folder) / target.name
        shutil.copyfile(candidate, copy)
        if digest(copy) != expected:
            raise ValueError('Recovered copy checksum mismatch')
        copy.replace(target)
    data['recoveredFromCache'] = True
    data['phoneTested'] = False
    (output / 'release-identity.json').write_text(json.dumps(data, indent=2) + '\n')
    return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('candidate', 'receipt', 'output', 'source-sha', 'aapt', 'apksigner'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    result = recover(args.candidate, args.receipt, args.output, args.source_sha, args.aapt, args.apksigner)
    print('BRAXHORN_APK_RECOVERED sha256=' + result['apkSha256'] + ' rebuilt=false phone_tested=false')


if __name__ == '__main__':
    main()
