#!/usr/bin/env python3
"""Plansza Tripo z pojazdami (motory, rowery — właściciel 12.11 01:30–01:32, „globalnie dla całej gry”) stoi bokiem: przód
pojazdu na +x. split_sheet.py zapisuje sztuki „przodem do +Z”, więc przed cięciem obracamy całą planszę o -90° wokół Y
(pozycje i normalne; UV i tekstury bez zmian): obroc_arkusz.py <wejście.glb> <wyjście.glb>. Plik właściciela zostaje
nietknięty (do paczki idzie oryginał). Czysty Python + numpy."""
import sys,json,struct
import numpy as np
src,dst=sys.argv[1],sys.argv[2]
b=bytearray(open(src,'rb').read())
jl=struct.unpack('<I',b[12:16])[0]; g=json.loads(b[20:20+jl]); off=20+jl; b0=off+8
prim=g['meshes'][0]['primitives'][0]
for key in ('POSITION','NORMAL'):
    a=g['accessors'][prim['attributes'][key]]; bv=g['bufferViews'][a['bufferView']]
    assert bv.get('byteStride',12)==12, bv
    st=b0+bv.get('byteOffset',0)+a.get('byteOffset',0); n=a['count']
    arr=np.frombuffer(bytes(b[st:st+n*12]),np.float32).reshape(n,3).copy()
    x,z=arr[:,0].copy(),arr[:,2].copy(); arr[:,0]=-z; arr[:,2]=x   # obrót -90° wokół Y: przód pojazdu (+x) -> +z
    b[st:st+n*12]=arr.astype(np.float32).tobytes()
    if key=='POSITION': a['min']=arr.min(0).tolist(); a['max']=arr.max(0).tolist()
js=json.dumps(g,separators=(',',':')).encode(); js+=b' '*((4-len(js)%4)%4)
out=b[:12]+struct.pack('<I',len(js))+b'JSON'+js+b[off:]; struct.pack_into('<I',out,8,len(out))
open(dst,'wb').write(out)
