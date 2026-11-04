"""Render unchanged character sources with bpy; never a Unity/game screenshot.

The manifest points to original FBX and maps. Imported objects and temporary
materials exist only in memory. No source model, map or .blend is saved.
"""
import argparse, hashlib, json, math
from pathlib import Path
import bpy
from mathutils import Vector

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

def corrected_hands(meshes,mat):
    n=mat.node_tree.nodes;l=mat.node_tree.links;bs=n.get('Principled BSDF')
    base=bs.inputs['Base Color'].links[0].from_socket;rough=bs.inputs['Roughness'].links[0].from_socket;metal=bs.inputs['Metallic'].links[0].from_socket
    for obj in meshes:
        hand={v.index for v in obj.vertex_groups if 'LeftHand' in v.name or 'RightHand' in v.name}
        attr=obj.data.attributes.new('BraxHandWeight','FLOAT','POINT')
        for vertex in obj.data.vertices:attr.data[vertex.index].value=sum(g.weight for g in vertex.groups if g.group in hand)
    def math_node(op,a,b=None):
        node=n.new('ShaderNodeMath');node.operation=op
        for index,value in enumerate([a,b] if b is not None else [a]):
            if isinstance(value,(int,float)):node.inputs[index].default_value=value
            else:l.new(value,node.inputs[index])
        return node.outputs[0]
    def smooth(value,low,high):
        node=n.new('ShaderNodeMapRange');node.interpolation_type='SMOOTHSTEP';node.clamp=True
        l.new(value,node.inputs['Value']);node.inputs['From Min'].default_value=low;node.inputs['From Max'].default_value=high
        return node.outputs['Result']
    attr=n.new('ShaderNodeAttribute');attr.attribute_name='BraxHandWeight'
    channels=n.new('ShaderNodeSeparateColor');l.new(base,channels.inputs[0]);r,g,b=[channels.outputs[k] for k in ('Red','Green','Blue')]
    warm1=smooth(math_node('DIVIDE',math_node('SUBTRACT',r,g),math_node('MAXIMUM',r,.001)),.05,.20)
    warm2=smooth(math_node('DIVIDE',math_node('SUBTRACT',g,b),math_node('MAXIMUM',g,.001)),.02,.15)
    mask=math_node('MULTIPLY',math_node('MULTIPLY',warm1,warm2),smooth(attr.outputs['Fac'],.05,.65))
    lum=math_node('ADD',math_node('ADD',math_node('MULTIPLY',r,.2126),math_node('MULTIPLY',g,.7152)),math_node('MULTIPLY',b,.0722))
    charcoal=math_node('ADD',math_node('MULTIPLY',lum,.04),.015)
    mix=n.new('ShaderNodeMixRGB');l.new(mask,mix.inputs[0]);l.new(base,mix.inputs[1]);l.new(charcoal,mix.inputs[2]);l.new(mix.outputs[0],bs.inputs['Base Color'])
    l.new(math_node('MULTIPLY',metal,math_node('SUBTRACT',1,mask)),bs.inputs['Metallic'])
    l.new(math_node('ADD',math_node('MULTIPLY',rough,math_node('SUBTRACT',1,mask)),math_node('MULTIPLY',mask,.78)),bs.inputs['Roughness'])

def inspect(project, manifest, output, ids, hand_correction=False):
    output.mkdir(parents=True, exist_ok=True)
    results=[]
    for c in json.loads(manifest.read_text())['characters']:
        if ids and c['id'] not in ids: continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.preferences.filepaths.use_scripts_auto_execute=False
        files=[project/f['path'] for f in c['files']]
        before={str(p):sha(p) for p in files}
        fbx=next(p for p in files if p.suffix.lower()=='.fbx')
        bpy.ops.import_scene.fbx(filepath=str(fbx.resolve()), use_anim=False, use_custom_normals=True)
        meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
        if not meshes: raise ValueError('No source mesh '+c['id'])
        mat=bpy.data.materials.new('Source maps — inspection only');mat.use_nodes=True
        n=mat.node_tree.nodes;l=mat.node_tree.links;bs=n.get('Principled BSDF')
        for keyword,slot,space in [('color','Base Color','sRGB'),('roughness','Roughness','Non-Color'),('metallic','Metallic','Non-Color'),('normal','Normal','Non-Color')]:
            path=next(p for p in files if keyword in p.name.lower())
            tex=n.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(path.resolve()));tex.image.colorspace_settings.name=space
            if keyword=='normal':
                nm=n.new('ShaderNodeNormalMap');l.new(tex.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs['Normal'],bs.inputs[slot])
            else:l.new(tex.outputs['Color'],bs.inputs[slot])
        for o in meshes:o.data.materials.clear();o.data.materials.append(mat)
        if hand_correction:
            if c['id']!='gorr':raise ValueError('Hand correction is authorized for GORR only')
            corrected_hands(meshes,mat)
        points=[o.matrix_world@Vector(p) for o in meshes for p in o.bound_box]
        lo=Vector([min(p[i] for p in points) for i in range(3)]);hi=Vector([max(p[i] for p in points) for i in range(3)])
        center=(lo+hi)*.5;size=hi-lo;span=max(size.x,size.z)*1.16
        scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=16;scene.cycles.use_denoising=True
        scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
        scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
        scene.world=bpy.data.worlds.new('Inspection neutral world');scene.world.use_nodes=True
        scene.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.18,.18,.18,1)
        scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.4
        scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast';scene.view_settings.exposure=0
        for name,offset,energy in [('Key',(-2,-3,4),1000),('Fill',(3,-1,2),450),('Rim',(0,3,3),700)]:
            light=bpy.data.lights.new(name,'AREA');light.energy=energy*(span/2)**2;light.shape='DISK';light.size=span
            ob=bpy.data.objects.new(name,light);scene.collection.objects.link(ob);ob.location=center+Vector(offset)*span/2
            ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
        camera=bpy.data.cameras.new('Inspection camera');ob=bpy.data.objects.new('Inspection camera',camera);scene.collection.objects.link(ob)
        camera.type='ORTHO';camera.ortho_scale=span;scene.camera=ob
        views=[]
        for name,angle in [('front',0),('back',math.pi)]:
            ob.location=center+Vector((math.sin(angle)*span*3,-math.cos(angle)*span*3,span*.12))
            ob.rotation_euler=(center-ob.location).to_track_quat('-Z','Y').to_euler()
            dest=output/(c['id']+'_'+name+'.png');scene.render.filepath=str(dest.resolve());bpy.ops.render.render(write_still=True);views.append(dest.name)
        bones=[]
        for arm in [o for o in scene.objects if o.type=='ARMATURE']:
            for b in arm.data.bones:
                bones.append({'name':b.name,'head':list(arm.matrix_world@b.head_local),'tail':list(arm.matrix_world@b.tail_local)})
        if any(sha(Path(p))!=s for p,s in before.items()):raise ValueError('Source changed')
        results.append({'id':c['id'],'views':views,'sourceUnchanged':True,'sourceHashes':before,'handCorrectionPreview':hand_correction,'boundsMin':list(lo),'boundsMax':list(hi),'bones':bones,'unityRendered':False})
        (output/'source-inspection.json').write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n')
        print(json.dumps({'id':c['id'],'sourceUnchanged':True,'views':views}),flush=True)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--project',type=Path,required=True);p.add_argument('--manifest',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True);p.add_argument('--ids',nargs='*');p.add_argument('--hand-correction',action='store_true')
    a=p.parse_args();inspect(a.project,a.manifest,a.output,a.ids,a.hand_correction)
