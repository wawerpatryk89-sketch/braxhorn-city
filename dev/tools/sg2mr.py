# Convert KHR_materials_pbrSpecularGlossiness materials in a GLB to metal/rough (diffuse -> baseColor).
import json, struct, sys
src, dst = sys.argv[1], sys.argv[2]
b = open(src, 'rb').read()
magic, ver, length = struct.unpack('<III', b[:12])
off = 12; chunks = []
while off < length:
    clen, ctype = struct.unpack('<II', b[off:off + 8]); chunks.append([ctype, b[off + 8:off + 8 + clen]]); off += 8 + clen
J = json.loads(chunks[0][1].decode('utf8'))
n = 0
for m in J.get('materials', []):
    ext = m.get('extensions', {}).pop('KHR_materials_pbrSpecularGlossiness', None)
    if ext is None: continue
    n += 1
    pbr = m.setdefault('pbrMetallicRoughness', {})
    if 'diffuseTexture' in ext: pbr['baseColorTexture'] = ext['diffuseTexture']
    if 'diffuseFactor' in ext: pbr['baseColorFactor'] = ext['diffuseFactor']
    pbr['metallicFactor'] = 0.0
    pbr['roughnessFactor'] = max(0.35, 1.0 - ext.get('glossinessFactor', 0.2))
    if not m['extensions']: m.pop('extensions')
for k in ('extensionsUsed', 'extensionsRequired'):
    if k in J: J[k] = [e for e in J[k] if e != 'KHR_materials_pbrSpecularGlossiness']
    if k in J and not J[k]: J.pop(k)
js = json.dumps(J, separators=(',', ':')).encode('utf8'); js += b' ' * ((4 - len(js) % 4) % 4)
out = [struct.pack('<II', len(js), 0x4E4F534A) + js]
for ctype, data in chunks[1:]: out.append(struct.pack('<II', len(data), ctype) + data)
body = b''.join(out)
open(dst, 'wb').write(struct.pack('<III', magic, ver, 12 + len(body)) + body)
print('converted', n, 'materials')
