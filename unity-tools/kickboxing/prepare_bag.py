"""Reproducible BRAXHORN bag asset derived from the Innerscene CC0 source.

Run with Blender 4.2+, factory startup and autoexec disabled:
  blender -b --factory-startup --disable-autoexec -t 4 --python
    unity-tools/kickboxing/prepare_bag.py -- --source /path/to/source.glb

The swing root is the suspension pivot. glTF Y is up: the body occupies
Y=-4.15..-2.35. Place the root at world Y=4.60 for 0.45 m floor clearance.
The static mount is separate; its upper contact face is Y=+0.202 m.
No source characters, gameplay scripts or downloaded executable code are used.
"""
import argparse
import hashlib
import json
import math
import struct
import sys
import uuid
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--source', required=True)
parser.add_argument('--out', default=str(ROOT/'unity-city/Assets/Kickboxing/Models'))
parser.add_argument('--preview-dir', default=str(ROOT.parent/'tmp/asset-audit'))
parser.add_argument('--skip-render', action='store_true')
ARGS = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else [])
SOURCE = Path(ARGS.source).resolve()
OUT = Path(ARGS.out).resolve(); OUT.mkdir(parents=True, exist_ok=True)
PREVIEW = Path(ARGS.preview_dir).resolve(); PREVIEW.mkdir(parents=True, exist_ok=True)
LOGO = ROOT/'public/assets/logo/mark.webp'
GREEN_HEX = '#8bff3a'
SOURCE_URL = 'https://www.innerscene.com/tools/library/3d-parts/boxing-gloves-and-bag-e3d766bc'
LICENSE_URL = 'https://www.innerscene.com/tools/library/terms'
BODY_BOTTOM, BODY_HEIGHT, BODY_RADIUS = -4.15, 1.80, .275
BODY_TOP = BODY_BOTTOM + BODY_HEIGHT

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def srgb(v):
    return v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4

def rgb(hexcode):
    h=hexcode.lstrip('#')
    return tuple(srgb(int(h[i:i+2],16)/255) for i in (0,2,4))+(1,)

def clean():
    bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)

def mesh_object(name, verts, faces, material, group):
    m=bpy.data.meshes.new(name); m.from_pydata(verts,[],faces);m.update()
    ob=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(ob)
    if material:m.materials.append(material)
    for p in m.polygons:p.use_smooth=True
    if group is not None:group.append(ob)
    return ob

def simple_mat(name, color, rough=.65, metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=color
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Metallic'].default_value=metal
    m.diffuse_color=color
    m.use_backface_culling=True
    return m

def add_normal(mat, image, strength=.5, repeat=(1,1)):
    nt=mat.node_tree
    uv=nt.nodes.new('ShaderNodeTexCoord')
    tex=nt.nodes.new('ShaderNodeTexImage');tex.image=image;tex.interpolation='Linear'
    normal=nt.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=strength
    nt.links.new(uv.outputs['UV'],tex.inputs['Vector'])
    nt.links.new(tex.outputs['Color'],normal.inputs['Color']);nt.links.new(normal.outputs['Normal'],nt.nodes.get('Principled BSDF').inputs['Normal'])

def put_uv(ob, vertex_uv):
    uv=ob.data.uv_layers.new(name='UVMap')
    for p in ob.data.polygons:
        for li in p.loop_indices:uv.data[li].uv=vertex_uv[ob.data.loops[li].vertex_index]

def tube(name, points, radius, material, group, sides=4):
    points=[Vector(p) for p in points];verts=[];faces=[]
    for i,p in enumerate(points):
        tangent=(points[min(i+1,len(points)-1)]-points[max(0,i-1)]).normalized()
        anchor=Vector((0,0,1)) if abs(tangent.z)<.85 else Vector((1,0,0))
        u=tangent.cross(anchor).normalized();v=tangent.cross(u).normalized()
        for k in range(sides):verts.append(p+radius*(u*math.cos(k*math.tau/sides)+v*math.sin(k*math.tau/sides)))
    for i in range(len(points)-1):
        for k in range(sides):
            a=i*sides+k;b=i*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple((len(points)-1)*sides+k for k in range(sides))])
    return mesh_object(name,verts,faces,material,group)

def cylinder(name, a,b,radius,material,group,segments=12):
    return tube(name,[a,b],radius,material,group,segments)

def box(name,center,size,material,group,bevel=.0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=center)
    o=bpy.context.object;o.name=name;o.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.data.materials.append(material)
    if bevel:
        mod=o.modifiers.new('Small manufactured edge radius','BEVEL');mod.width=bevel;mod.segments=2
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    group.append(o)
    return o

def extract_islands(ob):
    bm=bmesh.new();bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    unseen=set(bm.verts);result=[]
    while unseen:
        a=unseen.pop();todo=[a];vs={a}
        while todo:
            a=todo.pop()
            for edge in a.link_edges:
                b=edge.other_vert(a)
                if b in unseen:unseen.remove(b);vs.add(b);todo.append(b)
        vs=sorted(vs,key=lambda v:tuple(v.co));mapping={v:i for i,v in enumerate(vs)}
        fs=sorted({f for v in vs for f in v.link_faces},key=lambda f:tuple(mapping[v]for v in f.verts))
        coords=[ob.matrix_world@v.co for v in vs]
        result.append({'verts':coords,'faces':[tuple(mapping[v] for v in f.verts) for f in fs],
                       'materials':{f.material_index for f in fs},
                       'minz':min(v.z for v in coords),'maxz':max(v.z for v in coords)})
    result.sort(key=lambda g:(round(g['minz'],8),round(g['maxz'],8),tuple(round(sum(v[k]for v in g['verts'])/len(g['verts']),8)for k in range(3))))
    bm.free()
    return result

clean()
bpy.ops.import_scene.gltf(filepath=str(SOURCE))
source_bag=bpy.data.objects.get('Heavy bag');source_chain=bpy.data.objects.get('Chains, swivel and ceiling plate')
if not source_bag or not source_chain:raise RuntimeError('Expected exact CC0 source meshes are missing.')
source_verts=np.array([list(source_bag.matrix_world@v.co) for v in source_bag.data.vertices])
source_radius=float(np.max(np.linalg.norm(source_verts[:,:2],axis=1)))
source_height=float(source_verts[:,2].max()-source_verts[:,2].min())
islands=extract_islands(source_chain)
link_candidates=[g for g in islands if len(g['verts'])==60 and g['materials']=={0}]
if not link_candidates:raise RuntimeError('No original source chain-link component found.')
link_source=link_candidates[-1]
coords=np.array([list(v)for v in link_source['verts']]);center=coords.mean(axis=0)
evals,axes=np.linalg.eigh(np.cov((coords-center).T))
# Canonical source link: long axis Z, broad axis X, narrow wire-depth axis Y.
axes=np.column_stack((axes[:,1],axes[:,0],axes[:,2]))
canonical=(coords-center)@axes
if np.linalg.det(axes)<0:canonical[:,1]*=-1
canonical*=.062/np.ptp(canonical[:,2])
link_template=mesh_object('Original CC0 link, remeshed for repeat use',canonical,link_source['faces'],None,None)
bpy.context.view_layer.objects.active=link_template;link_template.select_set(True)
dec=link_template.modifiers.new('Preserve source silhouette; reduce hidden radial tessellation','DECIMATE');dec.ratio=.66
bpy.ops.object.modifier_apply(modifier=dec.name)
link_vertices=[v.co.copy()for v in link_template.data.vertices]
link_faces=[tuple(p.vertices)for p in link_template.data.polygons]
link_triangles=sum(len(f)-2 for f in link_faces)

# Copying a packed Blender Image can create a 0x0 image. Resolve the actual
# normal image buffer from the source GLB and load those authoritative bytes.
source_bytes=SOURCE.read_bytes();json_length=struct.unpack_from('<I',source_bytes,12)[0]
source_gltf=json.loads(source_bytes[20:20+json_length]);source_bin=source_bytes[28+json_length:]
source_material=next(m for m in source_gltf['materials']if m['name']=='Black vinyl')
normal_slot=source_material['normalTexture']['index'];normal_index=source_gltf['textures'][normal_slot]['source']
image_view=source_gltf['bufferViews'][source_gltf['images'][normal_index]['bufferView']]
off=image_view.get('byteOffset',0);source_normal_path=PREVIEW/'cc0-leather-original.jpg'
source_normal_path.write_bytes(source_bin[off:off+image_view['byteLength']])
normal_image=bpy.data.images.load(str(source_normal_path),check_existing=False)
normal_image.name='CC0_Leather_Normal_1024';normal_image.colorspace_settings.name='Non-Color'
_ = normal_image.pixels[0]
normal_image.scale(1024,1024);normal_image.file_format='PNG'
normal_image.filepath_raw=str(PREVIEW/'cc0-leather-normal-1024.png');normal_image.save();normal_image.pack()
if list(normal_image.size)!=[1024,1024] or not normal_image.has_data:raise RuntimeError('Source leather normal map failed to materialize.')

for o in list(bpy.context.scene.objects):bpy.data.objects.remove(o,do_unlink=True)
SWING=[];MOUNT=[]
leather=simple_mat('BX matte black leather',rgb('#171a1b'),.69)
caps=simple_mat('BX reinforced leather and webbing',rgb('#101313'),.64)
thread=simple_mat('BX graphite stitching',rgb('#707568'),.87)
steel=simple_mat('CC0 zinc suspension hardware',rgb('#a8aaac'),.31,1)
frame=simple_mat('BX black powder coated mounting steel',rgb('#202323'),.62,.75)
green=simple_mat('BRAXHORN original lime '+GREEN_HEX,rgb(GREEN_HEX),.62)
white=simple_mat('BX white textile lettering',rgb('#e6e9e1'),.8)
add_normal(leather,normal_image,.38,(3,5));add_normal(caps,normal_image,.33,(3,4))

def radial(t,theta):
    # Source diameter/height establish the remeshed bag's profile, with tension
    # at the sewn caps and modest gravity sag. Maximum deformation is 5 mm.
    scale=BODY_RADIUS/source_radius
    source_base=source_radius*scale
    shoulder=1-.055*math.exp(-t/.055)-.055*math.exp(-(1-t)/.055)
    gravity=.0042*math.exp(-((t-.24)/.20)**2)
    panels=.0018*math.cos(6*(theta+math.pi/2))*math.sin(math.pi*t)**2
    wrinkle=.0026*(math.sin(12*theta+t*42)+.36*math.sin(21*theta-t*60))*(math.exp(-t/.095)+math.exp(-(1-t)/.09))
    return source_base*shoulder+gravity+panels+wrinkle

def surface(t,theta,offset=0):
    r=radial(t,theta)+offset
    return Vector((r*math.cos(theta),r*math.sin(theta),BODY_BOTTOM+BODY_HEIGHT*t))

# Remesh the source-derived shell as six lightly tensioned leather panels.
N=48;ROWS=28;verts=[];faces=[];uvs=[]
for j in range(ROWS+1):
    t=j/ROWS
    for i in range(N+1):
        theta=math.tau*i/N
        verts.append(surface(t,theta));uvs.append((i/N*3,t*5))
for j in range(ROWS):
    for i in range(N):
        a=j*(N+1)+i;faces.append((a,a+1,a+1+N+1,a+N+1))
shell=mesh_object('Source-derived six-panel leather shell',verts,faces,leather,SWING);put_uv(shell,uvs)

# Separate end caps remain visibly sewn to the main cylinder, with a mildly
# domed top and lower belly instead of a hard primitive end face.
for upper in (False,True):
    verts=[];faces=[];uvs=[];tt=1 if upper else 0
    for j in range(5):
        q=j/4
        for i in range(N+1):
            a=i*math.tau/N;r=radial(tt,a)*q
            z=(BODY_TOP if upper else BODY_BOTTOM)+(.010 if upper else -.006)*(1-q*q)
            verts.append((r*math.cos(a),r*math.sin(a),z));uvs.append(((.5+.5*q*math.cos(a))*3,(.5+.5*q*math.sin(a))*3))
    for j in range(4):
        for i in range(N):
            a=j*(N+1)+i;f=(a,a+1,a+N+2,a+N+1);faces.append(tuple(reversed(f)) if upper else f)
    o=mesh_object('Reinforced top leather cap' if upper else 'Reinforced lower leather cap',verts,faces,caps,SWING);put_uv(o,uvs)

def ring_band(name,t0,t1,material,offset=.0008):
    verts=[];faces=[];uv=[]
    for j,t in enumerate((t0,t1)):
        for i in range(N+1):verts.append(surface(t,math.tau*i/N,offset));uv.append((i/N,t*4))
    for i in range(N):faces.append((i,i+1,i+N+2,i+N+1))
    o=mesh_object(name,verts,faces,material,SWING);put_uv(o,uv)

ring_band('Sewn upper reinforcement cuff',.905,1,caps)
ring_band('Sewn lower reinforcement cuff',0,.095,caps)
ring_band('Thin BRAXHORN green upper welt',.896,.905,green,.002)
ring_band('Thin BRAXHORN green lower welt',.095,.104,green,.002)
for t in (.095,.905):
    tube('Raised leather piping',[surface(t,math.tau*i/48,.003)for i in range(49)],.0028,caps,SWING,4)

def stitch_quad(name,p,direction,across,length=.008,width=.0012,material=thread,group=SWING):
    p=Vector(p);d=Vector(direction).normalized()*length/2;a=Vector(across).normalized()*width/2
    face=(0,1,2,3) if a.cross(d).dot(Vector((p.x,p.y,0)))>=0 else (3,2,1,0)
    return [p-d-a,p-d+a,p+d+a,p+d-a],[face]

# Six longitudinal seams, two staggered rows of discrete small stitches each.
stitch_verts=[];stitch_faces=[]
for k in range(6):
    angle=-math.pi/2+(k+.5)*math.tau/6
    points=[surface(.105+j*.79/24,angle,.0022)for j in range(25)]
    tube('Raised folded panel seam '+str(k+1),points,.0016,caps,SWING,3)
    for side in (-1,1):
        for j in range(48):
            t=.115+j*.775/47;theta=angle+side*.010
            p=surface(t,theta,.0037)
            v,f=stitch_quad('',p,(0,0,1),(-math.sin(theta),math.cos(theta),0),.010,.00125)
            off=len(stitch_verts);stitch_verts+=v;stitch_faces.extend(tuple(x+off for x in face)for face in f)
for t in (.087,.113,.887,.913):
    for j in range(78):
        angle=j*math.tau/78;p=surface(t,angle,.0035)
        v,f=stitch_quad('',p,(-math.sin(angle),math.cos(angle),0),(0,0,1),.009,.0011)
        off=len(stitch_verts);stitch_verts+=v;stitch_faces.extend(tuple(x+off for x in face)for face in f)
mesh_object('Double fine saddle stitches',stitch_verts,stitch_faces,thread,SWING)

# Repeat an actual connected ring from the source chain, not drawn chain lines.
hardware_verts=[];hardware_faces=[];link_instances=0
def source_link(center,axis=(0,0,1),rotation=0,scale=1):
    global link_instances
    q=Vector((0,0,1)).rotation_difference(Vector(axis).normalized())
    turn=Matrix.Rotation(rotation,4,'Z')
    off=len(hardware_verts);c=Vector(center)
    hardware_verts.extend(c+q@(turn@(v*scale))for v in link_vertices)
    hardware_faces.extend(tuple(i+off for i in face)for face in link_faces)
    link_instances+=1

def chain_run(a,b,pitch=.043):
    a=Vector(a);b=Vector(b);d=b-a;n=max(2,round(d.length/pitch)+1)
    for i in range(n):source_link(a+d*i/(n-1),d,i%2*math.pi/2)

chain_run((0,0,-.035),(0,0,-1.665))
source_link((0,0,-1.705),(0,0,1),0,1.65)

# Four broad folded webbing straps bridge the cap into actual steel D rings.
strap_stitch_verts=[];strap_stitch_faces=[]
for k in range(4):
    theta=math.pi/4+k*math.pi/2;rad=Vector((math.cos(theta),math.sin(theta),0));tangent=Vector((-rad.y,rad.x,0))
    pts=[rad*.278+Vector((0,0,BODY_TOP-.35)),rad*.278+Vector((0,0,BODY_TOP-.19)),rad*.272+Vector((0,0,BODY_TOP-.04)),rad*.240+Vector((0,0,BODY_TOP+.025)),rad*.205+Vector((0,0,BODY_TOP+.060)),rad*.203+Vector((0,0,BODY_TOP+.105)),rad*.190+Vector((0,0,BODY_TOP+.118)),rad*.177+Vector((0,0,BODY_TOP+.100)),rad*.178+Vector((0,0,BODY_TOP+.060))]
    verts=[];faces=[];uv=[]
    for j,p in enumerate(pts):
        for side in (-1,1):verts.append(p+tangent*.035*side);uv.append(((side+1)/2,j/(len(pts)-1)))
    for j in range(len(pts)-1):faces.append((j*2,j*2+1,j*2+3,j*2+2))
    o=mesh_object('Wide reinforced folded suspension webbing '+str(k+1),verts,faces,caps,SWING);put_uv(o,uv)
    sol=o.modifiers.new('Actual 3 mm layered webbing','SOLIDIFY');sol.thickness=.003
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=sol.name)
    for side in (-1,1):
        tube('Bound webbing edge',[p+tangent*.033*side for p in pts],.0014,caps,SWING,3)
        for j in range(11):
            p=rad*.281+tangent*.027*side+Vector((0,0,BODY_TOP-.32+j*.025))
            v,f=stitch_quad('',p,(0,0,1),tangent,.008,.0013)
            off=len(strap_stitch_verts);strap_stitch_verts+=v;strap_stitch_faces.extend(tuple(x+off for x in face)for face in f)
    # Original source ring reused at a larger, load-bearing attachment scale.
    ring_center=rad*.195+Vector((0,0,BODY_TOP+.112))
    source_link(ring_center,(0,0,1),theta+math.pi/2,1.38)
    carabiner=rad*.195+Vector((0,0,BODY_TOP+.172))
    source_link(carabiner,(0,0,1),theta,.92)
    cylinder('Locking karabiner sleeve',carabiner+rad*.015+Vector((0,0,-.013)),carabiner+rad*.015+Vector((0,0,.013)),.006,steel,SWING,8)
    chain_run((0,0,-1.725),carabiner+Vector((0,0,.027)))
    for dz in (-.29,-.22):
        p=rad*.283+Vector((0,0,BODY_TOP+dz))
        cylinder('Webbing reinforcement rivet',p-rad*.001,p+rad*.003,.006,steel,SWING,8)
mesh_object('Suspension webbing double stitching',strap_stitch_verts,strap_stitch_faces,thread,SWING)
mesh_object('Source CC0 chain links, master ring and connection rings',hardware_verts,hardware_faces,steel,SWING)

# Original plate and swivel remain fixed above the pivot. Preserve their
# actual source forms; add visible, practical beam clamp hardware.
source_pivot=2.0350188
for g in islands:
    if g['materials']=={1} or g['minz']>2.026:
        verts=[v-Vector((0,0,source_pivot))for v in g['verts']]
        mesh_object('Original source ceiling plate' if g['materials']=={1} else 'Original source fixed swivel',verts,g['faces'],frame if g['materials']=={1} else steel,MOUNT)
for x in (-.087,.087):
    for y in (-.087,.087):
        cylinder('Steel mounting bolt shank',(x,y,.178),(x,y,.202),.007,steel,MOUNT,8)
        cylinder('Hex bolt head',(x,y,.173),(x,y,.181),.012,steel,MOUNT,6)
        cylinder('Mounting washer',(x,y,.180),(x,y,.183),.015,steel,MOUNT,12)
box('Beam contact backing strip',(0,0,.201),( .24,.10,.002),frame,MOUNT,.0007)

# Original logo pixels on a physical curved sewn patch, not a regenerated sign.
try:
    logo_image=bpy.data.images.load(str(LOGO),check_existing=False)
except RuntimeError:
    # WebP-to-PNG is a lossless format conversion; source pixels are unchanged.
    import subprocess
    png=PREVIEW/'mark-original-pixels.png'
    subprocess.run(['python3','-c','from PIL import Image; import sys; Image.open(sys.argv[1]).save(sys.argv[2])',str(LOGO),str(png)],check=True)
    logo_image=bpy.data.images.load(str(png),check_existing=False)
logo_image.name='Original BRAXHORN mark unaltered';logo_image.pack()
_ = logo_image.pixels[0]
logo_image.file_format='PNG';logo_image.filepath_raw=str(PREVIEW/'mark-original-pixels.png');logo_image.save()
logo_image=bpy.data.images.load(str(PREVIEW/'mark-original-pixels.png'),check_existing=False);logo_image.pack()
logo_mat=simple_mat('Original BRAXHORN mark on sewn leather patch',(1,1,1,1),.72)
node=logo_mat.node_tree.nodes.new('ShaderNodeTexImage');node.image=logo_image
logo_mat.node_tree.links.new(node.outputs['Color'],logo_mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
verts=[];faces=[];uv=[]
z0=BODY_TOP-.70;z1=z0+.27
for j,z in enumerate((z0,z1)):
    for i in range(13):
        theta=-math.pi/2+(i/12-.5)*.98;t=(z-BODY_BOTTOM)/BODY_HEIGHT
        verts.append(surface(t,theta,.005));uv.append((i/12,j))
for i in range(12):faces.append((i,i+1,i+14,i+13))
patch=mesh_object('Original brand mark curved patch',verts,faces,logo_mat,SWING);put_uv(patch,uv)

fontpath=Path('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf')
if not fontpath.exists():fontpath=Path('/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf')
bpy.ops.object.text_add(location=(0,-.283,BODY_TOP-.79),rotation=(math.pi/2,0,0))
text=bpy.context.object;text.name='BRAXHORN controlled brand lettering';text.data.body='BRAXHORN'
text.data.align_x='CENTER';text.data.size=.047;text.data.extrude=.0001;text.data.resolution_u=3
if fontpath.exists():text.data.font=bpy.data.fonts.load(str(fontpath))
text.data.materials.append(white)
bpy.context.view_layer.objects.active=text;bpy.ops.object.convert(target='MESH');text=bpy.context.object
mw=text.matrix_world.copy()
for v in text.data.vertices:
    p=mw@v.co;t=(p.z-BODY_BOTTOM)/BODY_HEIGHT;r=radial(t,-math.pi/2)
    p.y=-math.sqrt(max(.001,r*r-p.x*p.x))-.004
    v.co=p
text.matrix_world=Matrix.Identity(4);text.data.update();SWING.append(text)

def combine(name,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
    o=bpy.context.object;o.name=name
    # Geometry remains in authored world coordinates; root is exactly zero.
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
    return o

swing=combine('BagSwing_PivotOrigin',SWING);mount=combine('BagMount_Fixed',MOUNT)
for ob in (swing,mount):
    ob['source']=SOURCE_URL;ob['license']='CC0-1.0';ob['brand']='Original BRAXHORN mark.webp; exact source pixels'
    ob['geometry']='Source-derived leather body; original-source repeated chain links and fixed suspension hardware; authored stitching and reinforcement'

def triangles(ob):return sum(max(0,len(p.vertices)-2)for p in ob.data.polygons)
total=triangles(swing)+triangles(mount)
if total>18000:raise RuntimeError('Bag triangle budget exceeded: '+str(total))
bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW/'bag-refined-before-export.blend'))
for filename,ob in [('BagSwing.glb',swing),('BagMount.glb',mount)]:
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(OUT/filename),export_format='GLB',use_selection=True,export_extras=True,export_animations=False,export_image_format='AUTO')
    meta=OUT/(filename+'.meta')
    if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'braxhorn-kickboxing/'+filename).hex+'\n')

report={'source':SOURCE_URL,'license':LICENSE_URL,'licenseId':'CC0-1.0','sourceSha256':sha(SOURCE),'sourceMeshNames':['Heavy bag','Chains, swivel and ceiling plate'],
        'sourceBodyDimensionsMetres':[source_radius*2,source_height],
        'removed':['Boxing glove left','Boxing glove right'],'bodyDimensionsMetres':[.55,1.80],
        'swingPivotLocal':[0,0,0],'bodyLocalY':[-4.15,-2.35],'floorClearanceAtPivotWorldY4_6':.45,
        'mountBeamContactLocalY':.202,'wholeBagTriangles':total,'swingTriangles':triangles(swing),'mountTriangles':triangles(mount),
        'sourceLinkTrianglesAfterReduction':link_triangles,'sourceLinkInstances':link_instances,
        'brandSource':'public/assets/logo/mark.webp','brandSha256':sha(LOGO),'brandGreen':GREEN_HEX,
        'textureLimit':1024,'files':{n:{'sha256':sha(OUT/n),'bytes':(OUT/n).stat().st_size}for n in ['BagSwing.glb','BagMount.glb']},
        'generatedBy':'unity-tools/kickboxing/prepare_bag.py','validation':'Geometry and Blender visual inspection only; Unity/device performance remains to be measured.'}
(OUT/'BagSource.provenance.json').write_text(json.dumps(report,indent=2)+'\n')
meta=OUT/'BagSource.provenance.json.meta'
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'braxhorn-kickboxing/BagSource.provenance.json').hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
print('BAG_ASSET_REPORT',json.dumps(report))

if not ARGS.skip_render:
    scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
    scene.world.color=(.18,.18,.18);scene.view_settings.view_transform='AgX'
    def light(loc,power,size):
        bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size
        o.rotation_euler=(Vector((0,0,-2.9))-o.location).to_track_quat('-Z','Y').to_euler()
    light((3,-4,0),1300,4);light((-3,-1,-1),900,3);light((1,3,-.5),1500,3)
    bpy.ops.mesh.primitive_plane_add(size=100,location=(0,0,-4.6));bpy.context.object.data.materials.append(simple_mat('Inspection floor',rgb('#5c6468'),.85))
    beam=box('Inspection-only support beam',(0,0,.31),(1.4,.32,.216),frame,[],.009)
    bpy.ops.object.camera_add();camera=bpy.context.object;camera.data.type='ORTHO';scene.camera=camera
    for name,loc,target,width,h in [('bag-refined-full',(2.6,-6.5,-.3),(0,0,-2.0),6.9,1100),('bag-refined-close',(1.3,-3.2,-2.45),(0,0,-3.15),2.7,1200),('bag-refined-top',(.9,-2.1,-1.7),(0,0,-2.26),1.6,1200)]:
        camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=width
        scene.render.resolution_x=1200;scene.render.resolution_y=round(h*1200/1400);scene.render.resolution_percentage=100
        scene.render.filepath=str(PREVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(PREVIEW/'bag-refined-inspection.blend'))
