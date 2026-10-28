import json, struct, sys, os
def info(p):
    with open(p,'rb') as f:
        magic, ver, length = struct.unpack('<III', f.read(12))
        clen, ctype = struct.unpack('<II', f.read(8))
        j = json.loads(f.read(clen))
    tris = 0
    for m in j.get('meshes', []):
        for pr in m['primitives']:
            if 'indices' in pr: tris += j['accessors'][pr['indices']]['count'] // 3
            else: tris += j['accessors'][pr['attributes']['POSITION']]['count'] // 3
    imgs = j.get('images', [])
    isz = sum(j['bufferViews'][i['bufferView']]['byteLength'] for i in imgs if 'bufferView' in i)
    skins = len(j.get('skins', [])); anims = [a.get('name','') for a in j.get('animations', [])]
    mats = len(j.get('materials', []))
    return f"{os.path.basename(p)[:48]:48s} {os.path.getsize(p)/1e6:6.1f}MB tris={tris/1e3:7.1f}k imgs={len(imgs)} ({isz/1e6:.1f}MB) mats={mats} skins={skins} anims={len(anims)} {anims[:4]} ext={j.get('extensionsUsed',[])}"
for p in sys.argv[1:]:
    try: print(info(p))
    except Exception as e: print(p, 'ERR', e)
