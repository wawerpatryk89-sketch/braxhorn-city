"""Prepare movable steering assemblies from the owner's plate-numbered GLBs, in Blender."""
import sys
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Vector

# Ring centres in the Blender world coordinates of the reviewed source files (metres).
# Metalik was authored at display-model scale.
RINGS = {
    1: (.438, -.458, 1.145), 2: (.438, -.458, 1.145), 3: (.56, -1.542, 1.862),
    4: (.46, -1.54, 1.63), 5: (.393, -.486, 1.185), 6: (.68, -5.065, 1.267),
    7: (.37, .086, .674), 8: (.37, -.289, .666), 9: (.37, -.034, .678),
    10: tuple(v / 4.75 for v in (.377, .021, .834)), 11: (.37, -.604, .979),
    12: (-.342, .484, .93), 13: (.38, -.32, .82), 14: (.38, -.32, .82),
}

def coords(o):
    xyz = np.empty(len(o.data.vertices)*3, np.float32)
    o.data.vertices.foreach_get("co", xyz)
    mat = np.array(o.matrix_world)
    return xyz.reshape(-1, 3) @ mat[:3, :3].T + mat[:3, 3]

def islands(o, pts):
    # UV seams and smooth-normal splits duplicate vertex indices at the same position.
    _, ids = np.unique(pts.round(6), axis=0, return_inverse=True)
    roots = list(range(int(ids.max())+1))
    def root(i):
        while roots[i] != i:
            roots[i] = roots[roots[i]]
            i = roots[i]
        return i
    for edge in o.data.edges:
        a, b = edge.vertices
        roots[root(ids[a])] = root(ids[b])
    return np.array([root(i) for i in ids])

def trim(mesh, keep):
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not keep[v.index]], context='VERTS')
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

def empty(name, location):
    obj = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = Vector(location)
    bpy.context.view_layer.update()
    return obj

def parent(obj, pivot):
    world = obj.matrix_world.copy()
    obj.parent = pivot
    obj.matrix_world = world

def wheel_key(name):
    text = name.lower().replace(' | assembly','').replace('_',' ').strip()
    patterns = {'fl':('fl','front l','front left'), 'fr':('fr','front r','front right'),
                'rl':('rl','rear l','rear left'), 'rr':('rr','rear r','rear right')}
    for key, aliases in patterns.items():
        if any(text in ('wheel '+alias, alias+' wheel') for alias in aliases):
            return key.upper()
    return None

def rig(src, target, nr, donor):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(src))
    center = np.array(RINGS[nr])
    factor = 4.75 if nr == 10 else 1.
    sign = -1 if nr == 12 else 1
    originals = list(bpy.context.scene.objects)
    wheels = {wheel_key(o.name):o for o in originals if wheel_key(o.name)}
    if len(wheels) != 4: raise ValueError(f'{src.name}: {wheels.keys()}')
    for key, obj in wheels.items():
        ms = [k for k in [obj,*obj.children_recursive] if k.type=='MESH']
        pts = np.concatenate([coords(k) for k in ms])
        pivot = empty('BX_Tire_'+key,(pts.min(0)+pts.max(0))*.5)
        parent(obj,pivot)
    if nr in (13,14):
        # These Tripo bodies have no separate driver controls. Use the same marque's GT6 assembly.
        before = set(bpy.context.scene.objects)
        bpy.ops.import_scene.gltf(filepath=str(donor))
        imported = set(bpy.context.scene.objects)-before
        for o in list(imported):
            if o.type == 'MESH' and o.name.startswith('Steering wheel |'):
                mat = o.matrix_world.copy()
                mat.translation += Vector(center-np.array(RINGS[7]))
                o.parent = None
                o.matrix_world = mat
                originals.append(o)
            else:
                continue
        for o in imported:
            if o not in originals: bpy.data.objects.remove(o,do_unlink=True)
        bpy.context.view_layer.update()

    pieces, ring = [], None
    for o in originals:
        if o.type != 'MESH': continue
        pts = coords(o)
        # Search only within the measured rim. Keep the fixed column, dashboard and stalks.
        rel = (pts-center)*factor
        radius = .265 if nr == 6 else .225
        if not (np.linalg.norm(rel,axis=1)<radius).any(): continue
        labels = islands(o,pts)
        chosen = []
        for key in np.unique(labels):
            sample = rel[labels==key]
            lo,hi = sample.min(0),sample.max(0)
            if max(abs(lo[0]),abs(hi[0])) > radius or np.linalg.norm(sample,axis=1).max()>radius+.012:
                continue
            span = hi-lo
            if span[0]>.26 and span[2]>.09 and np.linalg.norm((hi+lo)*.5)<.042:
                if ring is None or len(sample)>len(ring): ring = sample
            chosen.append(key)
        if not chosen: continue
        selected = np.isin(labels,chosen)
        if selected.all():
            pieces.append(o)
        else:
            part = o.copy()
            part.data = o.data.copy()
            o.users_collection[0].objects.link(part)
            part.matrix_world = o.matrix_world.copy()
            trim(part.data,selected)
            o.data = o.data.copy()
            trim(o.data,~selected)
            pieces.append(part)
    if ring is None or not pieces: raise ValueError(f'{src.name}: missing steering rim')
    normal = np.linalg.eigh(np.cov(ring.T))[1][:,0]
    if normal[1]*sign<0: normal = -normal
    steer = empty('BX_Steering',center)
    for part in pieces: parent(part,steer)
    axis = empty('BX_ColumnAxis',center + normal*.10/factor)
    parent(axis,steer)
    bpy.context.view_layer.update()
    bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',export_animations=False,export_extras=True)
    print('BRAX_FLOTA_RIG',nr,len(wheels),len(pieces),normal.tolist(),flush=True)

if __name__=='__main__':
    folder,out = map(Path,sys.argv[sys.argv.index('--')+1:])
    out.mkdir(exist_ok=True)
    cars = sorted(folder.glob('*.glb'))
    if len(cars)!=14: raise ValueError('Oczekiwano 14 oryginalnych GLB')
    donor = next(f for f in cars if f.stem.endswith('BX10007'))
    for f in cars: rig(f,out/f.name,int(f.stem[-2:]),donor)
