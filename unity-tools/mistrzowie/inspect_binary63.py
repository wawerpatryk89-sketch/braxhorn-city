import collections
import gc
import hashlib
import json
import os
import sys
import zipfile
from pathlib import Path

import UnityPy
from UnityPy.enums import TextureFormat

apk = Path(sys.argv[1])
with apk.open('rb') as stream:
    digest = hashlib.file_digest(stream, 'sha256').hexdigest()
if digest != 'c495589a9b1a0cc8d89b53ac9ce74c625e466063e2f3726616a1bedc428fa6c7':
    sys.exit('APK digest mismatch')

formats = collections.Counter()
sizes = collections.Counter()
textures = {}
surfaces = []
errors = []
members = []
with zipfile.ZipFile(apk) as archive:
    for member in archive.infolist():
        name = member.filename
        if member.is_dir():
            continue
        with archive.open(member) as stream:
            magic = stream.read(8)
        if not (magic.startswith(b'UnityFS') or name.endswith(('.assets', '.bundle', 'globalgamemanagers'))):
            continue
        members.append(name)
        try:
            env = UnityPy.load(archive.read(member))
            for obj in env.objects:
                kind = obj.type.name
                if kind not in ('Texture2D', 'Material'):
                    continue
                try:
                    data = obj.read()
                    if kind == 'Texture2D':
                        fmt = TextureFormat(data.m_TextureFormat).name
                        formats[fmt] += 1
                        sizes[fmt] += data.m_CompleteImageSize
                        textures[(os.path.basename(obj.assets_file.name).lower(), obj.path_id)] = data.m_Name
                    elif any(word in data.m_Name.lower() for word in ('asphalt','asfalt','paving','cycle_lane','cycle_mark')):
                        maps = {}
                        for slot, val in data.m_SavedProperties.m_TexEnvs:
                            if slot not in ('_MainTex','_BumpMap','_ARM'):
                                continue
                            ptr = val.m_Texture
                            file = (obj.assets_file.externals[ptr.m_FileID-1].path if ptr.m_FileID else obj.assets_file.name)
                            maps[slot] = [os.path.basename(file).lower(), ptr.m_PathID]
                        surfaces.append({'material':data.m_Name,'bundle':name,'maps':maps})
                except Exception as exc:
                    errors.append([name,kind,str(exc)[:200]])
            del env
            gc.collect()
        except Exception as exc:
            errors.append([name,'bundle',str(exc)[:200]])
for surface in surfaces:
    surface['resolved'] = {slot:textures.get(tuple(ptr)) for slot,ptr in surface['maps'].items()}
report = dict(apk_sha256=digest, members=members, texture_counts=formats, texture_bytes=sizes, surfaces=surfaces, errors=errors)
Path('audit63.json').write_text(json.dumps(report,indent=2))
print('members',len(members),'texture formats',dict(formats),'errors',len(errors))
for s in surfaces:
    print(s['material'],s['resolved'])
if not formats:
    sys.exit('No serialized textures read')
