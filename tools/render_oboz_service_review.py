"""Source-mesh/PBR inspection images, explicitly not Unity screenshots."""
import argparse,json,math,hashlib
from pathlib import Path
import bpy
from mathutils import Vector

def render(project,output):
 output.mkdir(parents=True,exist_ok=True);reports=[]
 folder=project/'Assets/Source/Prywatne/Oboz/environment/services';maps=project/'Assets/Source/Prywatne/Oboz/environment/materials'
 for path in sorted(folder.glob('*.glb')):
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.use_scripts_auto_execute=False
  bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
  for material in bpy.data.materials:
   ident=material.name.replace('OBOZ_','').split('.')[0]
   if ident not in ['wood','floor','metal','towel']:continue
   bs=next(n for n in material.node_tree.nodes if n.type=='BSDF_PRINCIPLED');nodes=material.node_tree.nodes;links=material.node_tree.links
   for suffix,slot in [('Color','Base Color'),('Roughness','Roughness'),('Metalness','Metallic'),('NormalGL','Normal')]:
    files=list((maps/ident).glob('*_'+suffix+'.jpg'))
    if not files:continue
    tex=nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(files[0].resolve()));tex.image.colorspace_settings.name='sRGB' if suffix=='Color' else 'Non-Color'
    if suffix=='NormalGL':
     normal=nodes.new('ShaderNodeNormalMap');links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs['Normal'],bs.inputs[slot])
    else:links.new(tex.outputs['Color'],bs.inputs[slot])
  scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=24;scene.cycles.use_denoising=True
  scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
  scene.world=bpy.data.worlds.new('Neutral inspection');scene.world.use_nodes=True;bg=scene.world.node_tree.nodes['Background'];bg.inputs['Color'].default_value=(.55,.65,.78,1);bg.inputs['Strength'].default_value=.5
  scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast'
  light=bpy.data.lights.new('Inspection sun','SUN');light.energy=2.8;light.angle=.25;sun=bpy.data.objects.new('Inspection sun',light);scene.collection.objects.link(sun);sun.rotation_euler=(.45,-.45,-.4)
  for node in scene.objects:
   if node.name=='DoorPivot':node.rotation_mode='XYZ';node.rotation_euler.z=math.radians(-85)
  for item in json.loads(path.with_suffix('.json').read_text()).get('lights',[]):
   x,y,z=item['position'];data=bpy.data.lights.new('Authored fixture','POINT');data.energy=160;data.color=(1,.82,.6);data.shadow_soft_size=.12
   ob=bpy.data.objects.new('Authored fixture',data);scene.collection.objects.link(ob);ob.location=(-x,-z,y)
  camera=bpy.data.cameras.new('Source review camera');ob=bpy.data.objects.new('Source review camera',camera);scene.collection.objects.link(ob);scene.camera=ob;camera.lens=32
  views=[]
  for name,location,target in [('outside',(-10,13,8),(0,0,1.5)),('inside',(0,2.5,1.7),(0,-2,1.3))]:
   ob.location=Vector(location);ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
   dest=output/(path.stem+'_'+name+'.png');scene.render.filepath=str(dest.resolve());bpy.ops.render.render(write_still=True);views.append(dest.name)
  reports.append({'model':path.name,'sourceSha256':hashlib.sha256(path.read_bytes()).hexdigest(),'views':views,'unityRendered':False,'ownerAppearanceApproved':False})
  (output/'review.json').write_text(json.dumps(reports,indent=2)+'\n');print(json.dumps(reports[-1]),flush=True)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--project',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args();render(a.project,a.output)
