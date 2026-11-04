import json
from pathlib import Path
import UnityPy

root = Path('serialization-probe/probe-report')
report = {}
for name in ('baseline', 'fixed'):
    env = UnityPy.load(str(root / name))
    components = []
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour':
            continue
        head = obj.parse_monobehaviour_head()
        script = head.m_Script.deref_parse_as_object()
        components.append({'class': script.m_ClassName, 'namespace': script.m_Namespace,
                           'bytes': obj.byte_size, 'assembly': script.m_AssemblyName})
    report[name] = components
print(json.dumps(report, indent=2))
(root / 'android-components.json').write_text(json.dumps(report, indent=2))
sea = [row for row in report['fixed'] if row['class'] == 'BraxMorzeZaKamera']
if len(sea) != 1 or sea[0]['bytes'] <= 32:
    raise RuntimeError('Fixed Android scene still has no sea field payload')
