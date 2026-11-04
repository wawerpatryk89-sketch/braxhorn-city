import json
from pathlib import Path
import sys
import UnityPy

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_city_sea_payload import decode_payload

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
        if name == 'fixed' and script.m_ClassName == 'BraxMorzeZaKamera':
            components[-1]['fields'] = decode_payload(obj.get_raw_data())
    report[name] = components
print(json.dumps(report, indent=2))
(root / 'android-components.json').write_text(json.dumps(report, indent=2))
sea = [row for row in report['fixed'] if row['class'] == 'BraxMorzeZaKamera']
old_sea = [row for row in report['baseline'] if row['class'] == 'BraxMorzeZaKamera']
if len(old_sea) != 1 or old_sea[0]['bytes'] != 32:
    raise RuntimeError('Baseline defect was not reproduced')
if len(sea) != 1 or sea[0]['bytes'] != 68:
    raise RuntimeError('Fixed Android scene still has no sea field payload')
