"""minimal GLB reader: json, binary chunk, accessors as numpy, node world matrices"""
import json, struct, numpy as np
CT = {5120: np.int8, 5121: np.uint8, 5122: np.int16, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}
NC = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}

class Glb:
    def __init__(self, path):
        b = open(path, 'rb').read()
        cl, = struct.unpack('<I', b[12:16]); self.js = json.loads(b[20:20 + cl])
        off = 20 + cl; bl, = struct.unpack('<I', b[off:off + 4]); self.bin = b[off + 8:off + 8 + bl]
        self.parent = {}
        for i, n in enumerate(self.js['nodes']):
            for c in n.get('children', []): self.parent[c] = i

    def acc(self, i):
        a = self.js['accessors'][i]; v = self.js['bufferViews'][a['bufferView']]
        dt = CT[a['componentType']]; n = NC[a['type']]; cnt = a['count']
        off = v.get('byteOffset', 0) + a.get('byteOffset', 0); stride = v.get('byteStride', 0)
        isz = np.dtype(dt).itemsize * n
        if stride and stride != isz:
            raw = np.frombuffer(self.bin, np.uint8, count=stride * (cnt - 1) + isz, offset=off)
            arr = np.lib.stride_tricks.as_strided(raw, shape=(cnt, isz), strides=(stride, 1)).copy().view(dt).reshape(cnt, n)
        else:
            arr = np.frombuffer(self.bin, dt, count=cnt * n, offset=off).reshape(cnt, n).copy()
        if a.get('normalized'):
            arr = arr.astype(np.float32) / np.iinfo(dt).max
        return arr

    def image(self, i):
        v = self.js['bufferViews'][self.js['images'][i]['bufferView']]
        return self.bin[v.get('byteOffset', 0):v.get('byteOffset', 0) + v['byteLength']]

    @staticmethod
    def trs(n):
        if 'matrix' in n: return np.array(n['matrix'], dtype=np.float64).reshape(4, 4).T
        t = np.array(n.get('translation', [0, 0, 0]), dtype=np.float64); r = n.get('rotation', [0, 0, 0, 1]); s = np.array(n.get('scale', [1, 1, 1]), dtype=np.float64)
        x, y, z, w = r
        R = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                      [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                      [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
        M = np.eye(4); M[:3, :3] = R * s; M[:3, 3] = t; return M

    def world(self, i):
        M = self.trs(self.js['nodes'][i]); p = self.parent.get(i)
        while p is not None:
            M = self.trs(self.js['nodes'][p]) @ M; p = self.parent.get(p)
        return M
