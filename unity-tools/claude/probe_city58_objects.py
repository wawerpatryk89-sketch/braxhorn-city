"""Read-only diagnosis of existing build 58, separate from Unity and delivery tools."""
import collections
import json
import pathlib
import re
import sys
import zipfile
import UnityPy
from UnityPy.helpers import TypeTreeHelper

TypeTreeHelper.read_typetree_boost = None
PROBE = 180994368  # x25 repeated in all five Android tombstones, not assumed to be the cause.


def main():
    target = pathlib.Path(sys.argv[2])
    target.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(sys.argv[1]) as apk:
        for name in ('level1', 'globalgamemanagers.assets'):
            parts = []
            for item in apk.infolist():
                match = re.fullmatch(r'assets/bin/Data/' + re.escape(name) + r'(?:\.split(\d+))?', item.filename)
                if match:
                    parts.append((int(match[1]) if match[1] is not None else -1, item))
            with (target / name).open('wb') as out:
                for _, item in sorted(parts, key=lambda p: p[0]):
                    out.write(apk.read(item))
    env = UnityPy.load(str(target))
    good, skipped, failures, probe = collections.Counter(), collections.Counter(), [], []
    scripts = {}
    for obj in env.objects:
        if pathlib.Path(obj.assets_file.name).name != 'level1':
            continue
        row = {'id': obj.path_id, 'class': obj.type.name, 'start': obj.byte_start, 'size': obj.byte_size}
        near = abs(obj.byte_start - PROBE) < 512 or obj.byte_start <= PROBE < obj.byte_start + obj.byte_size
        if near:
            probe.append(row.copy())
        try:
            if obj.type.name == 'MonoBehaviour':
                value = obj.parse_monobehaviour_head()
                script = value.m_Script.deref_parse_as_object()
                row['script'] = script.m_ClassName
                row['assembly'] = script.m_AssemblyName
                key = (script.m_AssemblyName, script.m_ClassName)
                stat = scripts.setdefault(key, {'assembly': key[0], 'class': key[1], 'count': 0, 'sizes': set()})
                stat['count'] += 1
                stat['sizes'].add(obj.byte_size)
                if near:
                    probe.append(row.copy())
                skipped['MonoBehaviour custom fields (no type tree)'] += 1
            else:
                value = obj.parse_as_object()
                good[obj.type.name] += 1
        except Exception as exc:
            row['error'] = type(exc).__name__ + ': ' + str(exc)
            if len(failures) < 100:
                failures.append(row)
    report = {'reader': UnityPy.__version__, 'scope': 'Independent parser, not Unity runtime validation',
              'parsed': dict(good), 'skipped': dict(skipped), 'failures_first_100': failures, 'register_probe': probe,
              'scripts': [dict(s, sizes=sorted(s['sizes'])) for _, s in sorted(scripts.items())]}
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
