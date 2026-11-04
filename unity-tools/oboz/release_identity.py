#!/usr/bin/env python3
"""Prepare/verify an update of the same Android application; never build or publish."""
import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess

PACKAGE = 'com.braxhorn.oboz'
EPOCH = 1577836800
LIMIT = 2100000000


def version_code(now, previous=None):
    if previous is not None:
        if not isinstance(previous, dict) or previous.get('applicationId') != PACKAGE:
            raise ValueError('Previous APK belongs to another application')
        code = previous.get('versionCode')
        if type(code) is not int or not 0 < code < LIMIT:
            raise ValueError('Invalid previous versionCode')
        certificate = previous.get('signingCertificateSha256', '')
        if not isinstance(certificate, str) or not re.fullmatch(r'[0-9a-f]{64}', certificate):
            raise ValueError('Previous release has no verified signing certificate')
    else:
        code = 0
    candidate = max(int(now)-EPOCH, code+1)
    if not 0 < candidate <= LIMIT:
        raise ValueError('Android versionCode outside supported range')
    return candidate


def verify_metadata(badging, signing, code, previous=None):
    if type(code) is not int or not 0 < code <= LIMIT:
        raise ValueError('Invalid target versionCode')
    if previous is not None:
        version_code(EPOCH, previous)
    package = re.search(r"^package: name='([^']+)' versionCode='(\d+)'", badging, re.M)
    # Build Tools 37 uses scheme labels (e.g. "V3.0 Signer:") instead of
    # "Signer #1". Cryptographic verification must still succeed before parsing.
    certificates = []
    for line in signing.splitlines():
        if ' certificate SHA-256 digest:' not in line or line.startswith('Source Stamp Signer '):
            continue
        match = re.fullmatch(
            r'(?:Signer #\d+|V(?:1|2|3\.0|3\.1|4) Signer:) certificate SHA-256 digest: ([0-9a-fA-F]{64})',
            line)
        if not match:
            raise ValueError('Unrecognized signing certificate output')
        certificates.append(match.group(1))
    if not package or package.group(1) != PACKAGE or int(package.group(2)) != code:
        raise ValueError('Built APK identity/version does not match the requested update')
    if len(certificates) != 1 or not re.fullmatch(r'[0-9a-fA-F]{64}', certificates[0]):
        raise ValueError('APK must have exactly one verified signing certificate')
    certificate = certificates[0].lower()
    if previous and (previous['versionCode'] >= code or previous['signingCertificateSha256'] != certificate):
        raise ValueError('Update would change signing identity or fail to increase versionCode')
    return {'applicationId': PACKAGE, 'versionCode': code, 'signingCertificateSha256': certificate}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode', choices=['prepare', 'verify'])
    p.add_argument('--previous', type=Path)
    p.add_argument('--allow-first-install', action='store_true')
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--version', type=int)
    p.add_argument('--apk', type=Path)
    p.add_argument('--aapt')
    p.add_argument('--apksigner')
    args = p.parse_args()
    previous = json.loads(args.previous.read_text()) if args.previous else None
    if args.previous and previous is None:
        p.error('Previous release file cannot be null')
    if previous is None and not args.allow_first_install:
        p.error('Previous release identity required; first installation must be explicit')
    if args.mode == 'prepare':
        code = version_code(datetime.now(timezone.utc).timestamp(), previous)
        args.output.write_text(str(code)+'\n')
        print('BRAXHORN_AKTUALIZACJA applicationId='+PACKAGE+' versionCode='+str(code))
    else:
        if not args.apk or not args.aapt or not args.apksigner or not args.version:
            p.error('verify requires apk, aapt, apksigner and version')
        badging = subprocess.check_output([args.aapt,'dump','badging',str(args.apk)], text=True)
        signing = subprocess.check_output([args.apksigner,'verify','--print-certs',str(args.apk)], text=True)
        data = verify_metadata(badging, signing, args.version, previous)
        digest = hashlib.sha256()
        with args.apk.open('rb') as stream:
            for block in iter(lambda: stream.read(1024*1024), b''):
                digest.update(block)
        data['apkSha256'] = digest.hexdigest()
        args.output.write_text(json.dumps(data,indent=2)+'\n')
        print('BRAXHORN_AKTUALIZACJA podpis=zweryfikowany wersja=zgodna')


if __name__ == '__main__':
    main()

