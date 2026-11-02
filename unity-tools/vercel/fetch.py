"""Download only the exact successful CI artifact; credentials stay in GH_TOKEN."""
import argparse
import hashlib
import json
import pathlib
import re
import stat
import subprocess
import tempfile
import zipfile

REPO = 'wawerpatryk89-sketch/braxhorn-city'


def api(endpoint):
    return json.loads(subprocess.check_output(['gh', 'api', f'repos/{REPO}/{endpoint}']))


def fetch(run_id, commit, dest):
    if not re.fullmatch(r'[0-9]+', run_id) or not re.fullmatch(r'[a-f0-9]{40}', commit):
        raise ValueError('Run ID and full commit SHA required')
    run = api(f'actions/runs/{run_id}')
    if not (run['status'] == 'completed' and run['conclusion'] == 'success'
            and run['head_sha'] == commit and run['head_branch'] == 'claude/miasto-opus-20261031'
            and run['path'] == '.github/workflows/claude-region-build.yml'):
        raise ValueError('Source is not the exact successful Unity branch build')
    name = 'claude-region-player-' + commit
    artifacts = api(f'actions/runs/{run_id}/artifacts?per_page=100')['artifacts']
    selected = [a for a in artifacts if a['name'] == name and not a['expired']]
    if len(selected) != 1 or selected[0]['workflow_run']['head_sha'] != commit:
        raise ValueError('Exact Unity player artifact missing or expired')
    artifact = selected[0]
    expected = artifact.get('digest', '')
    if not re.fullmatch(r'sha256:[a-f0-9]{64}', expected):
        raise ValueError('GitHub archive checksum unavailable')
    if dest.exists():
        raise ValueError('Download destination already exists')
    with tempfile.TemporaryDirectory() as tmp:
        archive = pathlib.Path(tmp) / 'artifact.zip'
        with archive.open('wb') as f:
            subprocess.run(['gh', 'api', f'repos/{REPO}/actions/artifacts/{artifact["id"]}/zip'],
                           stdout=f, check=True)
        with archive.open('rb') as f:
            actual = hashlib.file_digest(f, 'sha256').hexdigest()
        if 'sha256:' + actual != expected:
            raise ValueError('Downloaded GitHub artifact checksum mismatch')
        with zipfile.ZipFile(archive) as z:
            for info in z.infolist():
                p = pathlib.PurePosixPath(info.filename)
                if p.is_absolute() or '..' in p.parts or '\\' in info.filename or stat.S_ISLNK(info.external_attr >> 16):
                    raise ValueError('Unsafe archive entry')
            z.extractall(dest)
    print('UNITY6_SOURCE_ARTIFACT_VERIFIED', run_id, commit, artifact['id'], expected)


if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--run', required=True)
    p.add_argument('--commit', required=True)
    p.add_argument('--output', type=pathlib.Path, required=True)
    a = p.parse_args()
    fetch(a.run, a.commit, a.output)
