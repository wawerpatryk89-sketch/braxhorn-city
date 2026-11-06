"""Author camp service buildings as beveled meshes and binding metadata.

No owner asset is modified. Metres before K; Unity-facing coordinates are
mirrored explicitly on Blender export for glTFast's coordinate conversion.
Materials are rebound to the recovered PBR maps by the Unity editor binder.
"""
import argparse,hashlib,json,math,sys
from pathlib import Path
import bpy
from mathutils import Vector

MATERIALS={'wood':(.35,.22,.12,1),'floor':(.45,.30,.16,1),'metal':(.10,.11,.12,1),
 'canvas':(.356,.372,.227,1),'burgundy':(.478,.118,.118,1),'white':(.78,.77,.70,1),
 'glass':(.25,.4,.45,.2),'towel':(.12,.27,.45,1),'rubber':(.035,.035,.038,1),'lamp':(1,.7,.32,1)}
FLOOR_HEIGHT=.16
TABLE_HEIGHT=.75
BENCH_HEIGHT=.45
DOOR_WIDTH=.90
DOOR_HEIGHT=2.10
def p(v):return Vector((-v[0],-v[2],v[1]))
def meshmat(obj,name):obj.data.materials.append(bpy.data.materials['OBOZ_'+name])

class Builder:
 def __init__(self,name):
  bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.use_scripts_auto_execute=False
  self.name=name;self.colliders=[];self.doors=[];self.seats=[];self.facilities=[];self.static=[];self.lights=[];self.furniture=[];self.missing_models=[]
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
 def assembly(self,name,at):
  parent=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(parent);parent.location=p(at);bpy.context.view_layer.update();return parent
 def furniture_root(self,kind,label,x,z,length,height,width):
  name=f'{kind}_{len(self.furniture):02d}'
  parent=self.assembly(name,(x,FLOOR_HEIGHT,z))
  self.furniture.append({'node':name,'kind':kind,'label':label,'position':[x,FLOOR_HEIGHT,z],
   'size':[length,height,width],'height':height})
  return parent
 def bench(self,x,z,length=2.8,yaw=0,label='ŁAWKA'):
  parent=self.furniture_root('Bench',label,x,z,length,BENCH_HEIGHT,.42)
  top=FLOOR_HEIGHT+BENCH_HEIGHT;leg_height=BENCH_HEIGHT-.055
  for offset in [-.14,0,.14]:self.box('Bench slat',(x,top-.055/2,z+offset),(length,.055,.12),'wood',yaw,parent=parent)
  for side in [-1,1]:self.box('Bench trestle',(x+side*(length*.38),FLOOR_HEIGHT+leg_height/2,z),(.13,leg_height,.42),'metal',yaw,parent=parent)
  for offset in [-length*.28,0,length*.28]:self.seat(label,x+offset,top,z,yaw)
 def table(self,x,z,length=3.4,width=1.1):
  parent=self.furniture_root('Table','STÓŁ Z DESEK',x,z,length,TABLE_HEIGHT,width)
  top=FLOOR_HEIGHT+TABLE_HEIGHT;leg_height=TABLE_HEIGHT-.065;boards=math.ceil(width/.18)
  for i in range(boards):self.box('Table board',(x,top-.065/2,z-width/2+(i+.5)*width/boards),(length,.065,width/boards-.008),'floor',parent=parent)
  for a in [-1,1]:
   for b in [-1,1]:self.box('Table leg',(x+a*(length/2-.2),FLOOR_HEIGHT+leg_height/2,z+b*(width/2-.15)),(.11,leg_height,.11),'wood',parent=parent)
 def front_wall(self,width,front):
  # Exact 0.90 x 2.10 m aperture, measured from the 0.16 m finished floor.
  # Trim boundary boards; do not cover a smaller door with the old 1.6 m hole.
  count=math.ceil(width/.19);opening=DOOR_WIDTH/2;top=FLOOR_HEIGHT+DOOR_HEIGHT
  for i in range(count):
   left=-width/2+i*width/count;right=-width/2+(i+1)*width/count
   for lo,hi in [(left,min(right,-opening)),(max(left,opening),right)]:
    if hi>lo:self.box('Front cladding',((lo+hi)/2,(FLOOR_HEIGHT+2.9)/2,front),(hi-lo-.007,2.9-FLOOR_HEIGHT,.12),'wood',bevel=.008)
   lo=max(left,-opening);hi=min(right,opening)
   if hi>lo:self.box('Entry lintel',((lo+hi)/2,(top+2.9)/2,front),(hi-lo,2.9-top,.12),'wood',bevel=.008)
  for side in [-1,1]:
   self.colliders.append({'position':[side*(width/2+opening)/2,(FLOOR_HEIGHT+2.9)/2,front],
    'size':[width/2-opening,2.9-FLOOR_HEIGHT,.14],'yaw':0})
   self.box('Door jamb',(side*(opening+.035),FLOOR_HEIGHT+DOOR_HEIGHT/2,front-.065),(.07,DOOR_HEIGHT,.07),'wood')
  self.colliders.append({'position':[0,(top+2.9)/2,front],'size':[DOOR_WIDTH,2.9-top,.14],'yaw':0})
  self.box('Door header',(0,top+.035,front-.065),(DOOR_WIDTH+.14,.07,.07),'wood')
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
  self.colliders.append({'position':[0,1.53,depth/2],'size':[width,2.74,.14],'yaw':0})
  self.front_wall(width,front)
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
  # Original lighting fixture models await the owner's H approval.
  self.missing_models.append('interior-lamps')
 def door(self,z):
  pivot=self.assembly('DoorPivot',(-DOOR_WIDTH/2,FLOOR_HEIGHT,z-.02))
  for i in range(6):
   left=-DOOR_WIDTH/2+i*DOOR_WIDTH/6+(0 if i==0 else .002)
   right=-DOOR_WIDTH/2+(i+1)*DOOR_WIDTH/6-(0 if i==5 else .002)
   self.box('Door plank',((left+right)/2,FLOOR_HEIGHT+DOOR_HEIGHT/2,z-.02),(right-left,DOOR_HEIGHT,.08),'wood',parent=pivot,bevel=.006)
  for y in [.24,1.85]:self.box('Door brace',(0,FLOOR_HEIGHT+y,z-.075),(.86,.10,.035),'metal',parent=pivot)
  self.rod('Door handle',(.34,FLOOR_HEIGHT+1.0,z-.13),(.34,FLOOR_HEIGHT+1.17,z-.13),.021,'metal',pivot)
  self.doors.append({'pivot':'DoorPivot','position':[0,FLOOR_HEIGHT,z],
   'colliderCenter':[DOOR_WIDTH/2,DOOR_HEIGHT/2,0],'colliderSize':[DOOR_WIDTH,DOOR_HEIGHT,.10]})
 def save(self,output):
  # Keep each complete piece of furniture independently measurable, with one
  # joined visual mesh per assembly. Never merge it into the building shell.
  for entry in self.furniture:
   parent=bpy.data.objects[entry['node']];pieces=[o for o in parent.children_recursive if o.type=='MESH']
   if not pieces:raise ValueError('Furniture has no geometry: '+entry['node'])
   bpy.ops.object.select_all(action='DESELECT')
   for o in pieces:o.select_set(True)
   bpy.context.view_layer.objects.active=pieces[0];bpy.ops.object.join();bpy.context.object.name=entry['node']+'_Visual'
  # Join only static visual pieces; articulated doors retain their hierarchy.
  bpy.ops.object.select_all(action='DESELECT')
  for o in self.static:o.select_set(True)
  bpy.context.view_layer.objects.active=self.static[0];bpy.ops.object.join();bpy.context.object.name='StaticVisual'
  path=output/(self.name+'.glb');bpy.ops.export_scene.gltf(filepath=str(path.resolve()),export_format='GLB',export_animations=False,export_skins=False,export_morph=False,export_cameras=False,export_lights=False,export_yup=True)
  data={'id':self.name,'revision':2,'worldScale':1.294,'sourceUnits':'metres before K','floorHeight':FLOOR_HEIGHT,
   'dimensions':{'doorWidth':DOOR_WIDTH,'doorHeight':DOOR_HEIGHT,'tableHeightAboveFloor':TABLE_HEIGHT,'benchSeatHeightAboveFloor':BENCH_HEIGHT},
   'colliders':self.colliders,'doors':self.doors,'furniture':self.furniture,'seats':self.seats,'facilities':self.facilities,'lights':self.lights,
   'missingModels':self.missing_models,'unityVerified':False,'ownerAppearanceApproved':False,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
  (output/(self.name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
  print(json.dumps({'model':self.name,'colliders':len(self.colliders),'doors':len(self.doors),'seats':len(self.seats),'facilities':len(self.facilities)}),flush=True)

def canteen():
 b=Builder('canteen');b.shell(12,9)
 for z in [-1.8,1.4]:
  for x in [-2.7,2.7]:
   b.table(x,z,3.6,1.0);b.bench(x,z-.93,3.6,0,'STOŁÓWKA');b.bench(x,z+.93,3.6,180,'STOŁÓWKA')
 # Only authored plank furniture belongs to this shell. The owner's original
 # kitchen, display, plates, trays and cutlery are placed separately in Unity.
 b.missing_models.extend(['canteen-bowls','canteen-mugs','canteen-chairs'])
 b.facility('Meal','POSIŁEK',(0,.2,2.5));b.facility('Drink','WODA',(-2,.2,2.5));return b
def sauna():
 b=Builder('sauna');b.shell(7,6)
 for z,height in [(1.0,.45),(1.7,.85),(2.4,1.25)]:
  parent=b.furniture_root('SaunaBench','ŁAWA SAUNY',0,z,5.4,height,.68)
  for i in range(5):b.box('Sauna bench slat',(0,FLOOR_HEIGHT+height-.025,z-.28+i*.14),(5.4,.05,.12),'floor',parent=parent)
  for x in [-2.4,0,2.4]:
   for side in [-1,1]:b.box('Sauna bench support',(x,FLOOR_HEIGHT+(height-.05)/2,z+side*.25),(.09,height-.05,.09),'wood',parent=parent)
  b.seat('SAUNA',0,FLOOR_HEIGHT+height,z,180)
 # No functional steam action until the owner accepts an actual heater.
 # Missing original models are an explicit report, never a box or nine spheres.
 b.missing_models.extend(['sauna-heater','cold-plunge-tub']);return b
def medical():
 b=Builder('medical');b.shell(8,6)
 b.table(0,-1.1,2,.8)
 # Original IDANAS beds and metal cabinet are placed in Unity.
 # The first-aid case is missing; no box prop or fake treatment action.
 b.missing_models.append('first-aid-case');return b
def storage():
 b=Builder('storage');b.shell(8,6)
 for x in [-2.6,2.6]:
  for z in [-1.3,1.3]:
   parent=b.furniture_root('StorageRack','REGAŁ MAGAZYNU',x,z,1.5,2.05,2.0)
   for y in [.3,1.1,1.9]:
    b.box('Storage shelf',(x,FLOOR_HEIGHT+y,z),(1.5,.055,2.0),'floor',parent=parent)
   for a in [-1,1]:
    for c in [-1,1]:b.box('Rack upright',(x+a*.72,FLOOR_HEIGHT+2.05/2,z+c*.95),(.06,2.05,.06),'metal',parent=parent)
 b.missing_models.append('storage-crates')
 b.facility('Storage','MAGAZYN',(0,.2,-1.5));return b

if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True)
 args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
 a=parser.parse_args(args);a.output.mkdir(parents=True,exist_ok=True)
 for make in [canteen,sauna,medical,storage]:make().save(a.output)
