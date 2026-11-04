"""Create a disposable small Unity project using exact runtime sources."""
import json
import pathlib
import shutil
import subprocess

BASE = '874f194624e4dfead5637a4e3dfd1d3285a9cbe1'
SOURCE = pathlib.Path('unity-city/Assets/Claude/Runtime')
target = pathlib.Path('serialization-probe')
(target / 'Assets/Runtime').mkdir(parents=True, exist_ok=True)
(target / 'Assets/Baseline').mkdir(parents=True, exist_ok=True)
(target / 'Assets/Editor').mkdir(parents=True, exist_ok=True)
(target / 'Packages').mkdir(parents=True, exist_ok=True)
(target / 'ProjectSettings').mkdir(parents=True, exist_ok=True)
for name in ('BraxAkwen.cs', 'BraxMorzeZaKamera.cs', 'BraxMiastoGranice.cs'):
    shutil.copy2(SOURCE / name, target / 'Assets/Runtime' / name)
shutil.copy2('unity-city/Assets/Editor/Claude/BraxBrakujaceSkrypty.cs', target / 'Assets/Editor')
shutil.copy2('unity-tools/claude/serialization-probe/BraxSerializationProbe.cs', target / 'Assets/Editor')
original = subprocess.check_output(['git', 'show', f'{BASE}:{SOURCE}/BraxAkwen.cs'], text=True)
# Only namespace isolation changes the baseline. Keep both original class names,
# their order and the original BraxAkwen.cs filename (the suspected defect).
imports, body = original.split('// AKWEN', 1)
(target / 'Assets/Baseline/BraxAkwen.cs').write_text(imports + '\nnamespace BraxProbeBaseline\n{\n// AKWEN' + body + '\n}\n')
(target / 'Packages/manifest.json').write_text(json.dumps({'dependencies': {
    'com.unity.modules.imgui': '1.0.0', 'com.unity.modules.audio': '1.0.0'}}, indent=2))
(target / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.0.58f2\nm_EditorVersionWithRevision: 6000.0.58f2 (92dee566b325)\n')
