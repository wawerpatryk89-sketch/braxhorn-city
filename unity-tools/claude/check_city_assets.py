#!/usr/bin/env python3
"""Collect independent city asset failures in one preflight; never suppress them."""
from pathlib import Path
import subprocess
import sys


def check(root=Path('.'), runner=subprocess.run):
    source = root / 'unity-city/Assets/Source'
    errors = []
    def required(relative):
        path = source / relative
        if not path.is_file() or path.stat().st_size == 0:
            errors.append('Brak lub pusty plik: ' + str(relative))
    required('Claude/Osiedle/detail/pokoje.png')
    if not any((source / ('Claude/PolyHaven/sky/kloofendal_' + size + '.jpg')).is_file()
               and (source / ('Claude/PolyHaven/sky/kloofendal_' + size + '.jpg')).stat().st_size > 0
               for size in ('4k', '8k')):
        errors.append('Brak nieba kloofendal 4K/8K')
    for i in range(1,25): required(f'Prywatne/Miasto/miasto_sklepy/lek_{i:02d}.glb')
    for i in range(1,5): required(f'Prywatne/Miasto/miasto_sklepy/odz_whey_{i}.glb')
    required('Prywatne/Miasto/miasto_wyposazenie/miasto_wyposazenie.glb')
    # Both model audits run even when an earlier file or audit is invalid.
    for script in ('check_tram_glb.py', 'check_metro_glb.py'):
        try:
            result = runner([sys.executable, 'unity-tools/claude/' + script], cwd=root, check=False)
            if result.returncode: errors.append(script + ': exit=' + str(result.returncode))
        except OSError as exc:
            errors.append(script + ': ' + str(exc))
    return errors


def main():
    errors = check()
    for error in errors: print('::error::BRAXHORN_MIASTO_PREFLIGHT ' + error)
    print('BRAXHORN_MIASTO_PREFLIGHT errors=' + str(len(errors)))
    return 1 if errors else 0

if __name__ == '__main__': sys.exit(main())
