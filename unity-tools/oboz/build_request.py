"""Gate a single camp build requested by the owner; no dispatch or retry code."""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def validate(request, previous, *, event, ref, before_sha, parent_sha, private_sha,
             manual_first_install=False):
    original = ref == 'refs/heads/gpt/oboz'
    review_branch = ref == 'refs/heads/codex/oboz-realny-kompaktowy-20261105'
    # Nowa galaz moze wystartowac tylko przez reczny workflow_dispatch.
    # Push/build-request nie uruchamia kosztownego APK na galezi roboczej.
    if event not in ('push', 'workflow_dispatch') or not (original or review_branch and event == 'workflow_dispatch'):
        raise ValueError('Build is restricted to an explicit camp request')
    if (request.get('ownerRequested') is not True
            or request.get('privateInputsSha256') != private_sha
            or type(request.get('firstInstall')) is not bool
            or not isinstance(request.get('request'), str)
            or not re.fullmatch(r'[a-z0-9-]{8,160}', request['request'])):
        raise ValueError('Missing owner request or mismatched camp inputs')
    if event == 'workflow_dispatch':
        return manual_first_install
    # Only one new commit on the reviewed branch head can submit a request.
    # A repeated request, a branch creation, or a moved source base fails before Unity.
    base = request.get('sourceBaseCommit', '')
    if (not isinstance(base, str) or not re.fullmatch(r'[0-9a-f]{40}', base)
            or base == '0' * 40 or base != before_sha or base != parent_sha):
        raise ValueError('Requested camp base does not match the push parent')
    if not isinstance(previous, dict) or previous.get('request') == request['request']:
        raise ValueError('Build request was already submitted or previous request is missing')
    return request['firstInstall']


def main():
    path = Path('unity-tools/oboz/build-request.json')
    request = json.loads(path.read_text())
    event = os.environ['EVENT']
    previous, parent = None, ''
    if event == 'push':
        parent = subprocess.check_output(['git', 'rev-parse', 'HEAD^'], text=True).strip()
        previous = json.loads(subprocess.check_output(
            ['git', 'show', 'HEAD^:unity-tools/oboz/build-request.json'], text=True))
    private_sha = hashlib.sha256(Path('unity-tools/oboz/private-inputs.json').read_bytes()).hexdigest()
    first = validate(request, previous, event=event, ref=os.environ['GITHUB_REF'],
                     before_sha=os.environ.get('BEFORE_SHA', ''), parent_sha=parent,
                     private_sha=private_sha,
                     manual_first_install=os.environ.get('FIRST_INSTALL') == 'true')
    with open(os.environ['GITHUB_OUTPUT'], 'a') as output:
        output.write('first_install=' + str(first).lower() + '\n')
    print('BRAXHORN_OBOZ_REQUEST ' + request['request'] + ' source=' + os.environ['GITHUB_SHA'])


if __name__ == '__main__':
    main()
