"""Prepare a disposable project to compile all C# and test synthetic content."""
from pathlib import Path
import shutil

source, target = Path('unity-city'), Path('content-smoke')
if target.exists():
    raise SystemExit('Refusing to replace an existing smoke project')
for path in (source / 'Assets').rglob('*'):
    if path.suffix not in ('.cs', '.java') and path.name != 'link.xml':
        continue
    destination = target / path.relative_to(source)
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, destination)
    meta = Path(str(path) + '.meta')
    if meta.is_file():
        shutil.copy2(meta, Path(str(destination) + '.meta'))
for name in ('Packages/manifest.json', 'ProjectSettings/ProjectVersion.txt'):
    destination = target / name
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source / name, destination)
print('Synthetic Unity project prepared; private assets were not copied.')

probe = Path('unity-tools/updates/tests/BraxContentRuntimeProbe.cs')
shutil.copy2(probe, target/'Assets/Updates/BraxContentRuntimeProbe.cs')
