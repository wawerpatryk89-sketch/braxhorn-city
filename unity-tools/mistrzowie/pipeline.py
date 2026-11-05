"""Cheap source/workflow compatibility checks; no Unity or network calls."""
import json
import os
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
BRANCH = 'codex/miasto-mistrzow-20261104'
REQUIRED = (
    'unity-tools/claude/prywatne.py',
    'unity-tools/claude/import_patryk_transfer.py',
    'unity-tools/claude/import_patryk_animations.py',
    'unity-tools/claude/tekstury_b5.py',
    'unity-tools/mistrzowie/prepare_landscape.py',
    'unity-tools/mistrzowie/prepare_brax.py',
    'public/assets/chars/brax.glb.gz',
    'public/assets/tex/brax_color.webp',
    'public/assets/tex/brax_normal.webp',
    'public/assets/tex/brax_orm.webp',
)


def configuration(root=ROOT):
    config = json.loads((root/'unity-tools/mistrzowie/unity-build.json').read_text())
    project = (root/'unity-city/ProjectSettings/ProjectVersion.txt').read_text()
    match = re.search(r'^m_EditorVersion: (\S+)$', project, re.M)
    if not match or match[1] != config['unity']:
        raise RuntimeError('Unity project and Champions build configuration disagree')
    if f"{config['unity']} ({config['revision']})" not in project:
        raise RuntimeError('Unity editor revision differs from the pinned build configuration')
    return config


def check_launcher(root=ROOT, environ=None):
    env = os.environ if environ is None else environ
    config = configuration(root)
    if env.get('GITHUB_ACTIONS') == 'true' and env.get('MISTRZOWIE_PIPELINE') != config['pipeline']:
        raise RuntimeError('Stary lub niezgodny workflow. W Run workflow wybierz Use workflow from: '
                           + BRANCH + '. Samo nowe SHA w formularzu main nie wystarczy.')
    return config


def check_checkout(root=ROOT):
    missing = [path for path in REQUIRED if not (root/path).is_file()]
    if missing:
        raise RuntimeError('Incomplete Champions checkout: ' + ', '.join(missing))


def main():
    config = check_launcher()
    check_checkout()
    if os.environ.get('GITHUB_OUTPUT'):
        with open(os.environ['GITHUB_OUTPUT'], 'a') as output:
            for key in ('unity', 'androidImage', 'visualImage'):
                output.write(key + '=' + config[key] + '\n')
    print('BRAX_MISTRZOWIE_PIPELINE_OK unity=' + config['unity'])


if __name__ == '__main__':
    main()
