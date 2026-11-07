"""Bake four source-model views for distant forest, no generative imagery.

These are derived vegetation textures, never screenshots of a Unity scene.
Near/mid models retain their authored meshes. Source GLBs are read-only.
"""
import argparse,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector
from PIL import Image

def render(root,output,report,pattern='*_LOD2.glb'):
    output.mkdir(parents=True,exist_ok=True);records=[]
    for path in sorted(root.glob(pattern)):
        if path.name.endswith('__unity.glb'):continue
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.preferences.filepaths.use_scripts_auto_execute=False
        bpy.ops.import_scene.gltf(filepath=str(path.resolve()))
        # Capture albedo/coverage, not a permanently baked daylight exposure.
        # Unity applies its current sun/ambient to these distant views, so they
        # also darken at night without emission or a separate night texture.
        for material in bpy.data.materials:
            if not material.use_nodes:continue
            nodes=material.node_tree.nodes;links=material.node_tree.links
            principled=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
            out=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
            if not principled or not out:raise ValueError('Unexpected billboard material')
            emission=nodes.new('ShaderNodeEmission');transparent=nodes.new('ShaderNodeBsdfTransparent');mix=nodes.new('ShaderNodeMixShader')
            base=principled.inputs['Base Color'];alpha=principled.inputs['Alpha']
            if base.is_linked:links.new(base.links[0].from_socket,emission.inputs['Color'])
            else:emission.inputs['Color'].default_value=base.default_value
            if alpha.is_linked:links.new(alpha.links[0].from_socket,mix.inputs[0])
            else:mix.inputs[0].default_value=alpha.default_value
            links.new(transparent.outputs[0],mix.inputs[1]);links.new(emission.outputs[0],mix.inputs[2]);links.new(mix.outputs[0],out.inputs['Surface'])
        scene=bpy.context.scene;meshes=[o for o in scene.objects if o.type=='MESH']
        points=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
        lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
        # Square frames avoid clipping wide crowns in the atlas.
        # The quad stays centred on the trunk. An asymmetric crown therefore
        # needs twice its maximum radius, not merely its bounding-box width.
        # Using width alone clipped the actual small-tree side views.
        span=max(2*max(abs(lo.x),abs(hi.x),abs(lo.y),abs(hi.y)),hi.z-lo.z)*1.04
        center=Vector((0,0,(hi.z+lo.z)/2))
        scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=12;scene.cycles.use_denoising=True
        scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
        scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
        scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
        scene.world=bpy.data.worlds.new('Neutral billboard fill');scene.world.use_nodes=True
        scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.65
        light=bpy.data.lights.new('Diffuse source illumination','SUN');light.energy=1.5;light.angle=.5
        sun=bpy.data.objects.new('Diffuse source illumination',light);scene.collection.objects.link(sun);sun.rotation_euler=(math.radians(35),math.radians(-20),math.radians(-25))
        data=bpy.data.cameras.new('Atlas camera');camera=bpy.data.objects.new('Atlas camera',data);scene.collection.objects.link(camera)
        data.type='ORTHO';data.ortho_scale=span;scene.camera=camera
        frames=[]
        for index in range(4):
            angle=-index*math.pi/2
            camera.location=center+Vector((math.sin(angle),-math.cos(angle),0))*span*3
            camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler()
            frame=output/(path.stem+'_'+str(index)+'.png');scene.render.filepath=str(frame.resolve());bpy.ops.render.render(write_still=True)
            frames.append(frame)
        atlas=Image.new('RGBA',(2048,512));alpha_bounds=[]
        for i,frame in enumerate(frames):
            with Image.open(frame) as im:
                atlas.paste(im,(512*i,0))
                visible=im.getchannel('A').point(lambda a:255 if a>=90 else 0).getbbox()
                if visible is None:raise ValueError('Empty source-tree billboard view '+str(frame))
                if min(visible[0],visible[1])<=1 or max(visible[2],visible[3])>=511:
                    raise ValueError('Source-tree silhouette touches the atlas edge '+str(frame))
                alpha_bounds.append({'view':i,'alphaCutoff':.35,'pixels':list(visible)})
        dest=output/(path.stem.rsplit('_LOD',1)[0]+'_Billboard_Color.png');temp=dest.with_suffix('.tmp.png');atlas.save(temp)
        with Image.open(temp) as check:check.load()
        temp.replace(dest)
        for frame in frames:frame.unlink()
        if hashlib.sha256(path.read_bytes()).hexdigest()!=digest:raise ValueError('Source changed')
        records.append({'model':path.name,'sourceSha256':digest,'file':dest.name,'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),
          'span':span,'centerY':center.z,'sourceBoundsMeasured':True,'trunkCenteredSpan':True,
          'sourceBottomY':lo.z,'sourceTopY':hi.z,'sourceBoundsMin':list(lo),'sourceBoundsMax':list(hi),
          'viewAlphaBoundsPixels':alpha_bounds,'canvasBottomY':center.z-span*.5,
          'canvasMetric':'transparent camera frame; sourceBottomY is the tree foundation',
          'views':4,'size':[2048,512],'capture':'source albedo plus alpha; lighting applied in Unity shader','unityRendered':False,'sourceUnchanged':True})
        report.write_text(json.dumps({'billboards':records,'complete':False},indent=2)+'\n')
        print(json.dumps(records[-1]),flush=True)
    report.write_text(json.dumps({'billboards':records,'complete':True},indent=2)+'\n')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--report',type=Path,required=True)
    p.add_argument('--model-pattern',default='*_LOD2.glb',help='Exact authored source filename or pattern, e.g. tree_small_02_LOD1.glb; never a fabricated LOD')
    a=p.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]);render(a.sources,a.output,a.report,a.model_pattern)
