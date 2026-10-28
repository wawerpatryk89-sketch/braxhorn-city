"""Export supplied Maddduck hospital props with matching PBR texture sets."""
import bpy,pathlib,sys,zipfile
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[3]
archive=pathlib.Path(sys.argv[sys.argv.index('--')+1]);temp=pathlib.Path('/tmp/braxhorn-hospital');temp.mkdir(exist_ok=True)
with zipfile.ZipFile(archive) as z:
 for n in z.namelist():
  if n.endswith('.blend'):(temp/'hospital.blend').write_bytes(z.read(n))
  if n.endswith('.png') and any(x in n.lower() for x in ['/bed/','/chairs_table_1/','/wheel_chair/']):
   (temp/pathlib.Path(n).name).write_bytes(z.read(n))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
with bpy.data.libraries.load(str(temp/'hospital.blend')) as(a,b):b.objects=['Bed','Chair','Wheel_chair']
for o in b.objects:bpy.context.collection.objects.link(o)
for mat in bpy.data.materials:
 prefix={'Bed':'Bed','Chairs_tables_1':'Chairs_tables_1','Wheel_chair':'Wheel_chair'}.get(mat.name)
 if not prefix:continue
 mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();bs=nodes.new('ShaderNodeBsdfPrincipled');out=nodes.new('ShaderNodeOutputMaterial');mat.node_tree.links.new(bs.outputs['BSDF'],out.inputs['Surface']);bs.inputs['Roughness'].default_value=.85
 for kind,socket in [('BaseColor','Base Color'),('Normal','Normal'),('Roughness','Roughness')]:
  paths=[p for p in temp.glob('*.png') if p.name.lower().startswith(prefix.lower()) and kind.lower() in p.stem.lower()]
  if not paths and kind=='BaseColor':paths=[p for p in temp.glob('*.png') if p.name.lower().startswith(prefix.lower()) and 'base_color' in p.stem.lower()]
  if not paths:raise RuntimeError('Missing '+prefix+' '+kind)
  im=bpy.data.images.load(str(paths[0]),check_existing=True)
  if max(im.size)>1024:
   f=1024/max(im.size);im.scale(int(im.size[0]*f),int(im.size[1]*f))
  if kind!='BaseColor':im.colorspace_settings.name='Non-Color'
  node=nodes.new('ShaderNodeTexImage');node.image=im
  if kind=='Normal':nm=nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.6;mat.node_tree.links.new(node.outputs['Color'],nm.inputs['Color']);mat.node_tree.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
  else:mat.node_tree.links.new(node.outputs['Color'],bs.inputs[socket])
bpy.context.view_layer.update()
for obj,name in zip(b.objects,['hospital_bed','hospital_chair','hospital_wheelchair']):
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
 # Normalise object pivot while preserving its original authored orientation.
 pts=[obj.matrix_world@Vector(c) for c in obj.bound_box];obj.location-=Vector(((max(v.x for v in pts)+min(v.x for v in pts))/2,(max(v.y for v in pts)+min(v.y for v in pts))/2,min(v.z for v in pts)))
 bpy.context.view_layer.update()
 bpy.ops.export_scene.gltf(filepath=str(ROOT/'dev/osiedle/assets'/f'{name}.glb'),export_format='GLB',use_selection=True,export_apply=True,export_animations=False)
 print('EXPORTED',name,flush=True)
