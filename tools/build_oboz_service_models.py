"""Author camp service buildings as beveled meshes and binding metadata.

No owner asset is modified. Metres before K; Unity-facing coordinates are
mirrored explicitly on Blender export for glTFast's coordinate conversion.
Materials are rebound to the recovered PBR maps by the Unity editor binder.
"""
import argparse,hashlib,json,math
from pathlib import Path
import bpy
from mathutils import Vector

MATERIALS={'wood':(.35,.22,.12,1),'floor':(.45,.30,.16,1),'metal':(.10,.11,.12,1),
 'canvas':(.356,.372,.227,1),'burgundy':(.478,.118,.118,1),'white':(.78,.77,.70,1),
 'glass':(.25,.4,.45,.2),'towel':(.12,.27,.45,1),'rubber':(.035,.035,.038,1),'lamp':(1,.7,.32,1)}
def p(v):return Vector((-v[0],-v[2],v[1]))
def meshmat(obj,name):obj.data.materials.append(bpy.data.materials['OBOZ_'+name])

class Builder:
 def __init__(self,name):
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.use_scripts_auto_execute=False
  self.name=name;self.colliders=[];self.doors=[];self.seats=[];self.facilities=[];self.static=[];self.lights=[]
  for ident,color in MATERIALS.items():
   mat=bpy.data.materials.new('OBOZ_'+ident);mat.diffuse_color=color;mat.use_nodes=True
   bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=.72
   if ident=='metal':bs.inputs['Metallic'].default_value=.65;bs.inputs['Roughness'].default_value=.45
   if ident=='glass':bs.inputs['Alpha'].default_value=.2
   if ident=='lamp':bs.inputs['Emission Color'].default_value=color;bs.inputs['Emission Strength'].default_value=2
 def box(self,name,at,size,material='wood',yaw=0,solid=False,parent=None,bevel=.015):
  bpy.ops.mesh.primitive_cube_add(size=1,location=p(at));o=bpy.context.object;o.name=name;o.scale=(size[0],size[2],size[1]);o.rotation_euler.z=-math.radians(yaw)
  bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);meshmat(o,material)
  if bevel:
   m=o.modifiers.new('Rounded manufactured edges','BEVEL');m.width=min(bevel,min(size)*.2);m.segments=2;bpy.ops.object.modifier_apply(modifier=m.name)
   for face in o.data.polygons:face.use_smooth=False
  # Per-metre UVs are set in the local mesh; preserve grain proportions.
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
  for face in o.data.polygons:
   axis=max(range(3),key=lambda i:abs(face.normal[i]));axes=[i for i in range(3) if i!=axis]
   for li in face.loop_indices:
    co=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]],co[axes[1]])
  if solid:self.colliders.append({'position':list(at),'size':list(size),'yaw':yaw})
  if parent:o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
  else:self.static.append(o)
  return o
 def rod(self,name,a,b,r=.04,material='metal',parent=None):
  pa,pb=p(a),p(b);bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=(pb-pa).length,location=(pa+pb)*.5)
  o=bpy.context.object;o.name=name;o.rotation_euler=(pb-pa).to_track_quat('Z','Y').to_euler();meshmat(o,material)
  for face in o.data.polygons:face.use_smooth=len(face.vertices)==4
  if parent:o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
  else:self.static.append(o)
  return o
 def seat(self,name,x,y,z,yaw=0,lie=False):self.seats.append({'name':name,'position':[x,y,z],'yaw':yaw,'lie':lie})
 def facility(self,kind,name,at,effect=None):self.facilities.append({'kind':kind,'name':name,'position':list(at),'effectPosition':list(effect or at)})
 def bench(self,x,z,length=2.8,yaw=0,label='ŁAWKA'):
  for offset in [-.14,0,.14]:self.box('Bench slat',(x,.48,z+offset),(length,.055,.12),'wood',yaw)
  for side in [-1,1]:self.box('Bench trestle',(x+side*(length*.38),.24,z),(.13,.43,.42),'metal',yaw)
  self.colliders.append({'position':[x,.25,z],'size':[length,.5,.45],'yaw':yaw});self.seat(label,x,.51,z,yaw)
 def table(self,x,z,length=3.4,width=1.1):
  for i in range(6):self.box('Table board',(x,.82,z-width/2+(i+.5)*width/6),(length,.065,width/6-.008),'floor')
  for a in [-1,1]:
   for b in [-1,1]:self.box('Table leg',(x+a*(length/2-.2),.4,z+b*(width/2-.15)),(.11,.78,.11),'wood')
  self.colliders.append({'position':[x,.43,z],'size':[length,.86,width],'yaw':0})
 def bed(self,x,z,label='ŁÓŻKO'):
  self.box('Bed frame',(x,.35,z),(1.05,.16,2.12),'wood',solid=True)
  self.box('Mattress',(x,.49,z),(.96,.19,2.02),'white',bevel=.06)
  self.box('Folded blanket',(x,.61,z+.30),(.92,.035,1.3),'towel',bevel=.01)
  self.box('Pillow',(x,.63,z-.73),(.68,.16,.38),'white',bevel=.06)
  for a in [-1,1]:
   for b in [-1,1]:self.box('Bed foot',(x+a*.43,.13,z+b*.88),(.1,.25,.1),'wood')
  self.seat(label,x,.59,z,0,True)
 def bowl(self,x,y,z,r=.13):
  # Revolved thin vessel, open top and visible inside; no solid sphere bowl.
  verts=[];faces=[];rings=[(r*.42,0),(r,.075),(r-.012,.075),(r*.42-.012,.012)]
  for rad,h in rings:
   for i in range(24):
    a=i*math.tau/24;verts.append(p((x+rad*math.cos(a),y+h,z+rad*math.sin(a))))
  for k in range(3):
   for i in range(24):faces.append((k*24+i,k*24+(i+1)%24,(k+1)*24+(i+1)%24,(k+1)*24+i))
  mesh=bpy.data.meshes.new('Vessel');mesh.from_pydata(verts,[],faces);mesh.update();o=bpy.data.objects.new('Bowl',mesh);bpy.context.collection.objects.link(o);meshmat(o,'metal');self.static.append(o)
 def shell(self,width,depth):
  self.width=width;self.depth=depth;front=-depth/2
  # Deck boards, raised only 16 cm; broad step keeps the entrance accessible.
  count=math.ceil(width/.19)
  for i in range(count):self.box('Floor plank',(-width/2+(i+.5)*width/count,.08,0),(width/count-.007,.16,depth),'floor',bevel=.008)
  self.colliders.append({'position':[0,.08,0],'size':[width,.16,depth],'yaw':0})
  self.box('Entrance step',(0,.04,front-.38),(1.75,.08,.75),'wood',solid=True)
  # Board walls with real apertures and separate simple colliders.
  for side in [-1,1]:
   for i in range(math.ceil(depth/.19)):
    z=-depth/2+(i+.5)*depth/math.ceil(depth/.19)
    sections=[(.16,1.0),(2.1,2.9)] if abs(z)<1.2 else [(.16,2.9)]
    for lo,hi in sections:self.box('Wall cladding',(side*width/2,(lo+hi)/2,z),(.12,hi-lo,.182),'wood',bevel=.008)
   self.colliders.extend([{'position':[side*width/2,.58,0],'size':[.14,.84,depth],'yaw':0},
    {'position':[side*width/2,2.5,0],'size':[.14,.8,depth],'yaw':0},
    {'position':[side*width/2,1.55,-(depth/2+1.2)/2],'size':[.14,1.1,depth/2-1.2],'yaw':0},
    {'position':[side*width/2,1.55,(depth/2+1.2)/2],'size':[.14,1.1,depth/2-1.2],'yaw':0}])
   self.box('Glass pane',(side*width/2,1.55,0),(.025,1.04,2.35),'glass')
   self.colliders.append({'position':[side*width/2,1.55,0],'size':[.06,1.1,2.4],'yaw':0})
   for z in [-1.25,0,1.25]:self.box('Window mullion',(side*width/2,1.55,z),(.19,1.22,.06),'wood')
   for y in [1.0,2.1]:self.box('Window sill',(side*width/2,y,0),(.21,.07,2.6),'wood')
  for i in range(math.ceil(width/.19)):
   x=-width/2+(i+.5)*width/math.ceil(width/.19)
   self.box('Rear cladding',(x,1.53,depth/2),(.182,2.74,.12),'wood',bevel=.008)
   if abs(x)>.78:self.box('Front cladding',(x,1.53,front),(.182,2.74,.12),'wood',bevel=.008)
   else:self.box('Entry lintel',(x,2.7,front),(.182,.4,.12),'wood',bevel=.008)
  self.colliders.append({'position':[0,1.53,depth/2],'size':[width,2.74,.14],'yaw':0})
  for side in [-1,1]:self.colliders.append({'position':[side*(width/2+.8)/2,1.53,front],'size':[width/2-.8,2.74,.14],'yaw':0})
  self.colliders.append({'position':[0,2.72,front],'size':[1.6,.36,.14],'yaw':0})
  # Gable roof made of individual slats, with ridge and exposed rafters.
  slope=.38;roof_half=width/2+.45;rise=roof_half*slope
  for side in [-1,1]:
   for i in range(math.ceil((depth+.8)/.24)):
    z=-(depth+.8)/2+(i+.5)*(depth+.8)/math.ceil((depth+.8)/.24)
    o=self.box('Roof board',(side*roof_half/2,2.94+rise/2,z),(roof_half*math.sqrt(1+slope*slope),.07,.23),'wood',bevel=.006)
    o.rotation_euler.y=-side*math.atan(slope)
   self.rod('Rafter',(0,2.94+rise,-depth/2),(side*roof_half,2.94,-depth/2),.07,'wood')
  self.rod('Roof ridge',(0,2.96+rise,-depth/2-.5),(0,2.96+rise,depth/2+.5),.08,'wood')
  # Filled triangular gables; no roof gap over the end walls.
  for z in [-depth/2,depth/2]:
   verts=[p((x,y,z+offset)) for offset in [-.055,.055] for x,y in [(-width/2,2.9),(width/2,2.9),(0,2.94+rise)]]
   mesh=bpy.data.meshes.new('Gable');mesh.from_pydata(verts,[],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]);mesh.update()
   import bmesh
   bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
   uv=mesh.uv_layers.new(name='UVMap')
   for face in mesh.polygons:
    for li in face.loop_indices:
     co=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(co.x,co.z)
   o=bpy.data.objects.new('Gable',mesh);bpy.context.collection.objects.link(o);meshmat(o,'wood');self.static.append(o)
  self.door(front)
  for x in [-width*.25,width*.25]:
   self.rod('Pendant cable',(x,2.85,0),(x,2.62,0),.009,'rubber')
   self.rod('Lamp rim',(x,2.60,0),(x,2.65,0),.18,'metal')
   bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.09,location=p((x,2.58,0)));o=bpy.context.object;o.name='Lamp bulb';meshmat(o,'lamp');self.static.append(o)
   self.lights.append({'position':[x,2.40,0],'range':7,'intensity':2.2})
 def door(self,z):
  pivot=bpy.data.objects.new('DoorPivot',None);bpy.context.collection.objects.link(pivot);pivot.location=p((-.74,.16,z-.02));bpy.context.view_layer.update()
  for i in range(8):self.box('Door plank',(-.70+(i+.5)*1.4/8,1.3,z-.02),(1.4/8-.006,2.25,.08),'wood',parent=pivot,bevel=.006)
  for y in [.4,2.2]:self.box('Door brace',(0,y,z-.075),(1.38,.11,.05),'metal',parent=pivot)
  self.rod('Door handle',(.54,1.23,z-.13),(.54,1.40,z-.13),.021,'metal',pivot)
  self.doors.append({'pivot':'DoorPivot','position':[0,.16,z],'colliderCenter':[.74,1.14,0],'colliderSize':[1.48,2.28,.11]})
 def save(self,output):
  # Join only static visual pieces; articulated doors retain their hierarchy.
  bpy.ops.object.select_all(action='DESELECT')
  for o in self.static:o.select_set(True)
  bpy.context.view_layer.objects.active=self.static[0];bpy.ops.object.join();bpy.context.object.name='StaticVisual'
  path=output/(self.name+'.glb');bpy.ops.export_scene.gltf(filepath=str(path.resolve()),export_format='GLB',export_animations=False,export_skins=False,export_morph=False,export_cameras=False,export_lights=False,export_yup=True)
  data={'id':self.name,'worldScale':1.294,'sourceUnits':'metres before K','colliders':self.colliders,'doors':self.doors,'seats':self.seats,'facilities':self.facilities,'lights':self.lights,'unityVerified':False,'ownerAppearanceApproved':False,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
  (output/(self.name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
  print(json.dumps({'model':self.name,'colliders':len(self.colliders),'doors':len(self.doors),'seats':len(self.seats),'facilities':len(self.facilities)}),flush=True)

def canteen():
 b=Builder('canteen');b.shell(12,9)
 for z in [-1.8,1.4]:
  for x in [-2.7,2.7]:
   b.table(x,z,3.6,1.0);b.bench(x,z-.93,3.6,0,'STOŁÓWKA');b.bench(x,z+.93,3.6,180,'STOŁÓWKA')
   for dx in [-1.1,0,1.1]:b.bowl(x+dx,.86,z)
 b.box('Service counter',(0,.58,3.6),(5.0,1.15,.65),'floor',solid=True)
 b.box('Service shelf',(0,1.8,4.18),(5.0,.06,.35),'wood')
 for x in [-1.5,-.5,.5,1.5]:b.bowl(x,1.16,3.6,.22)
 b.facility('Meal','POSIŁEK',(0,.2,2.5));b.facility('Drink','WODA',(-2,.2,2.5));return b
def showers():
 b=Builder('showers');b.shell(8,6)
 for x in [-2.7,-.9,.9,2.7]:
  b.box('Shower tray',(x,.2,1.65),(1.45,.08,1.5),'white',solid=True)
  b.rod('Shower pipe',(x,.65,2.50),(x,2.45,2.50),.025);b.rod('Shower arm',(x,2.45,2.50),(x,2.45,2.10),.025)
  b.rod('Shower head',(x,2.43,2.1),(x,2.46,2.1),.105)
  b.box('Mixer tap',(x,1.17,2.46),(.28,.08,.07),'metal')
  b.facility('Shower','PRYSZNIC',(x,.24,1.55),(x,2.4,2.10))
 for x in [-3.6,-1.8,0,1.8,3.6]:b.box('Stall partition',(x,1.25,1.55),(.045,2.08,1.9),'white',solid=True)
 b.bench(-2.4,-1.7,2.3,0,'PRZEBIERALNIA');b.bench(2.4,-1.7,2.3,0,'PRZEBIERALNIA')
 for x in [-2.6,-2.2,2.2,2.6]:b.box('Folded towel',(x,.55,-1.7),(.32,.08,.28),'towel',bevel=.025)
 return b
def sauna():
 b=Builder('sauna');b.shell(7,6)
 for z,y in [(1.0,.5),(1.7,.9),(2.4,1.3)]:
  for i in range(5):b.box('Sauna bench slat',(0,y,z-.28+i*.14),(5.4,.05,.12),'floor')
  b.colliders.append({'position':[0,y/2,z],'size':[5.4,y,.7],'yaw':0});b.seat('SAUNA',0,y+.03,z,180)
 b.box('Sauna heater',(2.4,.62,-1.2),(.7,.92,.7),'metal',solid=True)
 for i in range(9):
  x=2.15+(i%3)*.22;z=-1.43+(i//3)*.22
  bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.145,location=p((x,1.1,z)));o=bpy.context.object;o.name='Heater stone';o.scale=(1,.8,.65);meshmat(o,'rubber');b.static.append(o)
 b.facility('Sauna','PARA W SAUNIE',(1.7,.2,-1.0),(2.4,1.3,-1.2));return b
def medical():
 b=Builder('medical');b.shell(8,6)
 for x in [-2.4,2.4]:b.bed(x,.7,'LEŻANKA')
 b.box('Medical cabinet',(0,1.05,2.6),(1.7,1.8,.45),'white',solid=True)
 for x in [-.46,.46]:b.box('Cabinet door',(x,1.08,2.33),(.81,1.6,.04),'white');b.rod('Cabinet handle',(x,1.05,2.28),(x,1.23,2.28),.015)
 b.table(0,-1.1,2, .8);b.box('First aid case',(0,.98,-1.1),(.5,.23,.32),'white',bevel=.04)
 b.box('Medical cross vertical',(0,1.103,-1.1),(.05,.005,.20),'burgundy',bevel=0);b.box('Medical cross horizontal',(0,1.104,-1.1),(.20,.005,.05),'burgundy',bevel=0)
 b.facility('Medical','APTECZKA',(0,.2,-2.0));return b
def storage():
 b=Builder('storage');b.shell(8,6)
 for x in [-2.6,2.6]:
  for z in [-1.3,1.3]:
   for y in [.3,1.1,1.9]:
    b.box('Storage shelf',(x,y,z),(1.5,.055,2.0),'floor')
    for side in [-1,1]:
     b.box('Packing crate',(x+side*.38,y+.31,z),(.62,.55,.7),'wood',bevel=.015)
     for k in [-1,1]:b.box('Crate brace',(x+side*.38,y+.31,z+k*.36),(.68,.08,.035),'wood',yaw=0)
   for a in [-1,1]:
    for c in [-1,1]:b.box('Rack upright',(x+a*.72,1.1,z+c*.95),(.06,2.05,.06),'metal')
   b.colliders.append({'position':[x,1.15,z],'size':[1.6,2.25,2.05],'yaw':0})
 b.facility('Storage','MAGAZYN',(0,.2,-1.5));return b

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True);a=parser.parse_args();a.output.mkdir(parents=True,exist_ok=True)
 for make in [canteen,showers,sauna,medical,storage]:make().save(a.output)
