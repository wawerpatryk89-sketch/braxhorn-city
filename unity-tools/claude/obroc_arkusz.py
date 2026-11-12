#!/usr/bin/env python3
"""Obrót planszy / sztuki Tripo wokół osi Y przed pakowaniem (pozycje i normalne; UV i tekstury bez zmian).

  obroc_arkusz.py <wejście.glb> <wyjście.glb> [kąt° = -90] [--srodek]

• Plansza z pojazdami (motory, rowery — właściciel 12.11 01:30–01:32, „globalnie dla całej gry”) stoi bokiem: przód na +x;
  split_sheet.py zapisuje sztuki „przodem do +Z”, więc całą planszę obracamy o -90° (domyślnie): +x -> +z.
• Pojedyncza sztuka ustawiona na planszy skośnie (fotel z planszy mebli 12.11 01:51) — obrót o zmierzony kąt i --srodek:
  podstawa znowu w środku obrysu (x, z) na y = 0.
Kąt jak w Unity: dodatni obraca +z w stronę +x. Plik właściciela zostaje nietknięty (do paczki idzie oryginał). Python + numpy.
"""
import sys, json, struct, math
import numpy as np


def obroc(src, dst, kat=-90.0, srodek=False):
    b = bytearray(open(src, 'rb').read())
    jl = struct.unpack('<I', b[12:16])[0]; g = json.loads(b[20:20 + jl]); off = 20 + jl; b0 = off + 8
    t = math.radians(kat); c, s = math.cos(t), math.sin(t)
    for mesh in g['meshes']:
        for prim in mesh['primitives']:
            pos = None
            for key in ('POSITION', 'NORMAL'):
                a = g['accessors'][prim['attributes'][key]]; bv = g['bufferViews'][a['bufferView']]
                assert bv.get('byteStride', 12) == 12, bv
                st = b0 + bv.get('byteOffset', 0) + a.get('byteOffset', 0); n = a['count']
                arr = np.frombuffer(bytes(b[st:st + n * 12]), np.float32).reshape(n, 3).astype(np.float64)
                x, z = arr[:, 0].copy(), arr[:, 2].copy()
                arr[:, 0] = x * c + z * s; arr[:, 2] = -x * s + z * c
                if key == 'POSITION' and srodek:
                    lo, hi = arr.min(0), arr.max(0); arr[:, 0] -= (lo[0] + hi[0]) * .5; arr[:, 2] -= (lo[2] + hi[2]) * .5; arr[:, 1] -= lo[1]
                b[st:st + n * 12] = arr.astype(np.float32).tobytes()
                if key == 'POSITION': a['min'] = arr.min(0).tolist(); a['max'] = arr.max(0).tolist()
    js = json.dumps(g, separators=(',', ':')).encode(); js += b' ' * ((4 - len(js) % 4) % 4)
    out = b[:12] + struct.pack('<I', len(js)) + b'JSON' + js + b[off:]; struct.pack_into('<I', out, 8, len(out))
    open(dst, 'wb').write(out)


if __name__ == "__main__":
    a = [x for x in sys.argv[1:] if not x.startswith('--')]
    obroc(a[0], a[1], float(a[2]) if len(a) > 2 else -90.0, '--srodek' in sys.argv)
