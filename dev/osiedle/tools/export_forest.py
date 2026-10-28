"""Blender 4.2: selected Verdant Trail assets only, original files stay untouched.
Run: blender -b --factory-startup --python export_forest.py -- <upload-folder>
Runtime tree combines source wood and source island-tree leaf cards; topology is simplified.
"""
import bpy,sys,zipfile,pathlib,json,math,random
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[3]
UPLOAD=pathlib.Path(sys.argv[sys.argv.index('--')+1])
TEX=pathlib.Path('/tmp/braxhorn-forest-textures');TEX.mkdir(exist_ok=True)
for z in UPLOAD.glob('*.zip'):
 if not z.name.startswith('Archiwum'): continue
 with zipfile.ZipFile(z) as archive:
  for n in archive.namelist():
   name=pathlib.Path(n).name
   if name.startswith(('jacaranda_tree_','shrub_02_','rock_07_','island_tree_01_leaves_')) and name.endswith('.png') and not (TEX/name).exists():
    (TEX/name).write_bytes(archive.read(n))
print('TEXTURES',len(list(TEX.iterdir())),flush=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
names=['jacaranda_tree_trunk','jacaranda_tree_branches','shrub_02_a','rock_07.001','island_tree_01_leaves_a']
with bpy.data.libraries.load(str(ROOT/'dev/source-assets/verdant-trail/verdant_trail.blend')) as (a,b):b.objects=names
for o in b.objects: bpy.context.collection.objects.link(o)
# Relink existing image nodes (including source displacement) before evaluation.
for im in bpy.data.images:
 name=pathlib.Path(im.filepath).name
 candidates=[TEX/name,TEX/name.replace('_1k','')]
 for p in candidates:
  if p.exists():im.filepath=str(p);im.reload();break

def material(prefix,alpha=False):
 m=bpy.data.materials.new('Verdant_'+prefix);m.use_nodes=True
 nodes=m.node_tree.nodes;bs=nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.9
 def tex(kind,colour=False):
  paths=list(TEX.glob(prefix+'_'+kind+'*.png'))
  if not paths:
   if kind=='rough':return None
   raise RuntimeError('Missing '+prefix+' '+kind)
  im=bpy.data.images.load(str(paths[0]),check_existing=True)
  if not colour:im.colorspace_settings.name='Non-Color'
  n=nodes.new('ShaderNodeTexImage');n.image=im;return n
 d=tex('diff',True);m.node_tree.links.new(d.outputs['Color'],bs.inputs['Base Color'])
 rough=tex('rough')
 if rough:m.node_tree.links.new(rough.outputs['Color'],bs.inputs['Roughness'])
 normal=tex('nor_gl');nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.65;m.node_tree.links.new(normal.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
 if alpha:
  a=tex('alpha');m.node_tree.links.new(a.outputs['Color'],bs.inputs['Alpha']);m.surface_render_method='DITHERED';m.use_backface_culling=False
 return m
mats={p:material(p,p in ['island_tree_01_leaves','shrub_02']) for p in ['jacaranda_tree_trunk','jacaranda_tree_branches','island_tree_01_leaves','shrub_02','rock_07']}
# Jacaranda leaf colour/alpha files were not supplied. Use complete, UV-matched
# island-tree leaves from the SAME pack; keep source trunk and branch geometry.
leaf=bpy.data.objects['island_tree_01_leaves_a']
for obj in bpy.data.objects:
 if obj.name.startswith('jacaranda_tree_leaves'):
  old=max(v.co.x for v in obj.data.vertices)-min(v.co.x for v in obj.data.vertices)
  new=max(v.co.x for v in leaf.data.vertices)-min(v.co.x for v in leaf.data.vertices)
  obj.data=leaf.data.copy()
  for v in obj.data.vertices:v.co*=old/new
for o in bpy.data.objects:
 if o.type!='MESH':continue
 for i,m in enumerate(o.data.materials):
  prefix=m.name.split('.')[0]
  if prefix in mats:o.data.materials[i]=mats[prefix]
trunk=bpy.data.objects['jacaranda_tree_trunk'];branches=bpy.data.objects['jacaranda_tree_branches']
# Avoid subdivision/displacement expansion; normal maps retain the bark detail.
for m in list(trunk.modifiers):trunk.modifiers.remove(m)
for m in branches.modifiers:
 if m.type=='NODES':m['Input_4']=float(m.get('Input_4',30))*.25
bpy.context.view_layer.update()
# The library does not evaluate the scene's foliage collections on selective
# append. Assemble a deterministic canopy from supplied island-leaf cards.
# Place cards on the actual branch surface, using the original leaf UVs/alpha.
for m in list(branches.modifiers):
 if m.type=='NODES':branches.modifiers.remove(m)
bpy.context.view_layer.objects.active=branches
for m in list(branches.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)
bpy.context.view_layer.objects.active=leaf
mod=leaf.modifiers.new('Runtime leaf outline','DECIMATE');mod.ratio=.065
bpy.ops.object.modifier_apply(modifier=mod.name)
points=[branches.matrix_world@v.co for v in branches.data.vertices]
low=min(v.z for v in points);high=max(v.z for v in points)
points=[v for v in points if v.z>low+(high-low)*.36]
rng=random.Random(7401);rng.shuffle(points);cells=set();leaves=[]
for v in points:
 cell=tuple(round(a/.20) for a in v)
 if cell in cells:continue
 cells.add(cell)
 obj=leaf.copy();obj.data=leaf.data;bpy.context.collection.objects.link(obj)
 obj.location=v;obj.rotation_euler=(rng.uniform(-.75,.75),rng.uniform(-.75,.75),rng.uniform(0,math.tau));obj.scale*=rng.uniform(4.4,6.4);leaves.append(obj)
 if len(leaves)>=1300:break
bpy.ops.object.select_all(action='DESELECT')
for obj in leaves:obj.select_set(True)
bpy.context.view_layer.objects.active=leaves[0];bpy.ops.object.join();canopy=bpy.context.object;canopy.name='Canopy_from_island_leaf_cards'
print('CANOPY',len(leaves),len(canopy.data.polygons),flush=True)
# Preserve alpha-card geometry. Collapse only trunk and branches faces by material via vertex group.
for o in [trunk,branches]:
 group=o.vertex_groups.new(name='wood_only');ids=set()
 for poly in o.data.polygons:
  if 'leaves' not in o.data.materials[poly.material_index].name:ids.update(poly.vertices)
 group.add(list(ids),1,'REPLACE');mod=o.modifiers.new('Runtime wood budget','DECIMATE');mod.ratio=.10;mod.vertex_group=group.name;mod.vertex_group_factor=1
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)

def export(name,objects):
 bpy.ops.object.select_all(action='DESELECT')
 # Shared translation preserves alignment; no individual centering of tree parts.
 pts=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
 low=Vector(tuple(min(p[k] for p in pts) for k in range(3)));high=Vector(tuple(max(p[k] for p in pts) for k in range(3)))
 shift=Vector((-(low.x+high.x)/2,-(low.y+high.y)/2,-low.z))
 worlds={o:o.matrix_world.copy() for o in objects}
 for o in objects:o.parent=None;o.matrix_world=worlds[o];o.location+=shift;o.select_set(True)
 bpy.context.view_layer.update()
 path=ROOT/'dev/osiedle/assets'/f'{name}.glb'
 # Runtime texture budget, originals remain in uploaded archives.
 for im in bpy.data.images:
  if im.size[0]>1024 or im.size[1]>1024:
   f=1024/max(im.size);im.scale(max(1,int(im.size[0]*f)),max(1,int(im.size[1]*f)))
 bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_apply=True,export_animations=False,export_extras=False,export_image_format='AUTO')
 print('EXPORTED',name,path.stat().st_size,flush=True)
export('park_tree',[trunk,branches,canopy])
export('park_shrub',[bpy.data.objects['shrub_02_a']])
export('park_rock',[bpy.data.objects['rock_07.001']])
