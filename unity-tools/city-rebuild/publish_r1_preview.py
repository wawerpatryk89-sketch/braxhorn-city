"""Publish only an immutable R1 Unity candidate with matching physical-lift QA."""
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

REPO = 'wawerpatryk89-sketch/braxhorn-city'
BRANCH = 'codex/city-r1-continuation-20261029'


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def main():
    source = os.environ['GITHUB_SHA']
    run = os.environ['GITHUB_RUN_ID']
    require(os.environ['GITHUB_REPOSITORY'] == REPO, 'Wrong repository')
    require(os.environ['GITHUB_REF'] == 'refs/heads/' + BRANCH, 'Wrong source branch')
    require(re.fullmatch('[0-9a-f]{40}', source), 'Source must be full SHA')
    player, qa, out = Path('unity-preview'), Path('city-lift-review'), Path('preview-payload')
    require(not out.exists(), 'Refuse to replace existing payload')
    require(not any(p.is_symlink() for p in player.rglob('*')), 'Symlink in player')
    release = json.loads((player / 'release.json').read_text())
    report = json.loads((qa / 'report.json').read_text())
    require(release.get('scope') == 'isolated-city-rebuild-preview' and
            release.get('sourceCommit') == source and release.get('cityCommit') == source and
            str(release.get('sourceRun')) == run and release.get('productionPublication') is False,
            'Wrong compiled Unity candidate')
    require(report.get('sourceCommit') == source and report.get('passed') is True,
            'Matching physical lift test must pass')
    passed = {c['name'] for c in report['checks'] if c.get('passed') is True}
    require({'Passenger reaches roof', 'Passenger exits onto original lobby',
             'Lift holds passenger position while game is paused',
             'No emergency recovery or lost-floor event'} <= passed, 'Incomplete lift evidence')
    names = set()
    for name, digest in release['files'].items():
        require(isinstance(name, str) and re.fullmatch(r'[A-Za-z0-9_./-]+', name) and
                not name.startswith('/') and '..' not in name.split('/'), 'Invalid file path')
        file = player / name
        require(file.is_file() and not file.is_symlink() and file.stat().st_size < 99*1024*1024,
                'Missing/oversize compiled file: ' + name)
        with file.open('rb') as stream:
            require(hashlib.file_digest(stream, 'sha256').hexdigest() == digest,
                    'Compiled file hash mismatch: ' + name)
        names.add(name)
    require({p.relative_to(player).as_posix() for p in player.rglob('*') if p.is_file()} ==
            names | {'release.json'}, 'Unmanifested game file')
    shutil.copytree(player, out)
    write(out / 'vercel.json', {
        'framework': None, 'buildCommand': '', 'installCommand': '', 'outputDirectory': '.',
        'cleanUrls': False, 'headers': [{'source': p, 'headers': [
            {'key': 'Cache-Control', 'value': 'no-store'}]} for p in ['/', '/index.html', '/release.json']]})
    (out / 'robots.txt').write_text('User-agent: *\nDisallow: /\n')
    (out / 'evidence').mkdir()
    for name in ['00-start.png', '01-lobby.png', '03-inside-car.png', '04-top.png', '05-return.png']:
        file = qa / name
        require(file.is_file() and file.read_bytes()[:8] == b'\x89PNG\r\n\x1a\n',
                'Missing actual Unity screenshot: ' + name)
        shutil.copy2(file, out / 'evidence' / name)
    write(out / 'preview-qa-summary.json', {
        'status': 'CANDIDATE_REQUIRES_VISUAL_AND_OWNER_REVIEW', 'sourceCommit': source,
        'sourceRun': run, 'flagshipPhysicalLiftPassed': True, 'pausePassed': True,
        'initialLobbyPlacement': 'QA fixture; not a continuous street walk',
        'allNineLiftsRuntimePassed': False, 'physicalIphoneTested': False,
        'ownerAccepted': False, 'productionPublication': False,
        'scope': 'R1 city only; newer gym and R2 contractor branches not integrated'})
    preview = 'preview/city-r1-access-' + source[:12]
    token = os.environ.pop('GH_TOKEN')
    env = os.environ.copy()
    env.update(GIT_CONFIG_COUNT='1', GIT_CONFIG_KEY_0='http.https://github.com/.extraheader',
               GIT_CONFIG_VALUE_0='AUTHORIZATION: basic ' +
               base64.b64encode(('x-access-token:' + token).encode()).decode())

    def git(*args, auth=False):
        return subprocess.check_output(['git', '-c', 'core.hooksPath=/dev/null', *args],
                                       cwd=out, env=env if auth else None, text=True).strip()

    git('init', '--initial-branch=' + preview)
    git('config', 'user.name', 'github-actions[bot]')
    git('config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com')
    git('remote', 'add', 'origin', 'https://github.com/' + REPO + '.git')
    require(not git('ls-remote', '--heads', 'origin', 'refs/heads/' + preview, auth=True),
            'Existing preview must not be overwritten')
    git('add', '--all')
    git('commit', '-m', 'preview(city): Unity R1 access candidate ' + source[:12])
    commit = git('rev-parse', 'HEAD')
    git('push', 'origin', 'HEAD:refs/heads/' + preview, auth=True)
    receipt = {'previewBranch': preview, 'previewCommit': commit,
               'unitySourceCommit': source, 'productionChanged': False}
    write(Path('preview-receipt/publication.json'), receipt)
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
