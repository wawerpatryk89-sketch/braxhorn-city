"""Original metre-scale retail kit. Run from Blender in background mode.

Final models have bevels, real cap/neck/spout profiles and mapped printwork.
The render is an asset-authoring preview, never evidence of Unity execution.
"""
import bpy
import math
import json
import hashlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'unity-city/Assets/Nutrition/Models'
TEX = ROOT / 'unity-city/Assets/Nutrition/Textures'
REVIEW = ROOT / 'nutrition-asset-review'
OUT.mkdir(parents=True, exist_ok=True)
REVIEW.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def material(name, colour, rough=.45, metal=0, image=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*colour, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*colour, 1)
    bs.inputs['Roughness'].default_value = rough
    bs.inputs['Metallic'].default_value = metal
    if image:
        texture = m.node_tree.nodes.new('ShaderNodeTexImage')
        texture.image = bpy.data.images.load(str(TEX / image), check_existing=True)
        m.node_tree.links.new(texture.outputs['Color'], bs.inputs['Base Color'])
        if image in ['WalnutColour.jpg','LimestoneColour.jpg']:
            prefix=image.replace('Colour.jpg','')
            normals=m.node_tree.nodes.new('ShaderNodeTexImage')
            normals.image=bpy.data.images.load(str(TEX/(prefix+'Normal.png')),check_existing=True)
            normals.image.colorspace_settings.name='Non-Color'
            decode=m.node_tree.nodes.new('ShaderNodeNormalMap')
            m.node_tree.links.new(normals.outputs['Color'],decode.inputs['Color'])
            m.node_tree.links.new(decode.outputs['Normal'],bs.inputs['Normal'])
            roughness=m.node_tree.nodes.new('ShaderNodeTexImage')
            roughness.image=bpy.data.images.load(str(TEX/(prefix+'ARM.png')),check_existing=True)
            roughness.image.colorspace_settings.name='Non-Color'
            channels=m.node_tree.nodes.new('ShaderNodeSeparateRGB')
            m.node_tree.links.new(roughness.outputs['Color'],channels.inputs[0])
            m.node_tree.links.new(channels.outputs['G'],bs.inputs['Roughness'])
    return m


carbon = material('BH satin graphite polymer', (.055, .065, .068), .32)
cream = material('BH ivory moulded polymer', (.70, .675, .60), .39)
rubber = material('BH soft touch ribbed silicone', (.028, .035, .037), .72)
bronze = material('BH brushed warm anodised aluminium', (.40, .29, .15), .39, .72)
steel = material('BH powder-coated graphite steel', (.06, .075, .079), .47, .44)
printwork = material('BH wrap labels original atlas', (1, 1, 1), .52, image='BraxhornProductAtlas.png')
wood = material('BH satin oiled walnut joinery', (1, 1, 1), .5, image='WalnutColour.jpg')
stone = material('BH honed warm limestone furniture', (1, 1, 1), .68, image='LimestoneColour.jpg')
accent = material('BH terracotta identification tab', (.56, .19, .10), .48)
opal = material('BH opal luminaire diffuser', (.9, .83, .65), .38)
bs = opal.node_tree.nodes.get('Principled BSDF')
bs.inputs['Emission Color'].default_value = (.82, .74, .53, 1)
bs.inputs['Emission Strength'].default_value = 1.0


def group(name):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    return o


kit = group('BRAXHORN_NUTRITION_ORIGINAL_KIT')
parts = {}


def part(name):
    g = group(name)
    g.parent = kit
    parts[name] = g
    return g


def mesh(name, vertices, faces, parent, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.parent = parent
    data.materials.append(mat)
    return obj


def box(name, parent, at, size, mat, bevel=.006):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    obj = bpy.context.object
    obj.name = name
    obj.parent = parent
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Designed edge radius', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        mod.affect = 'EDGES'
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = obj.modifiers.new('Weighted furniture normals', 'WEIGHTED_NORMAL')
        mod.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj


def lathe(name, parent, profile, mat, segments=48, label=None, at=(0, 0, 0), ribs=0):
    vertices = []
    for r, z in profile:
        for j in range(segments + 1):
            theta = -1.5 * math.pi + j / segments * 2 * math.pi
            radius = r + (ribs * (.5 + .5 * math.cos(j / segments * 2 * math.pi * 32)) if r > .01 else 0)
            vertices.append((at[0] + radius * math.cos(theta), at[1] + radius * math.sin(theta), at[2] + z))
    faces = []
    for row in range(len(profile) - 1):
        for j in range(segments):
            a = row * (segments + 1) + j
            if profile[row][0] == 0:
                faces.append((a, a + segments + 2, a + segments + 1))
            elif profile[row + 1][0] == 0:
                faces.append((a, a + 1, a + segments + 1))
            else:
                faces.append((a, a + 1, a + segments + 2, a + segments + 1))
    obj = mesh(name, vertices, faces, parent, mat)
    uv = obj.data.uv_layers.new()
    height = max(.0001, profile[-1][1] - profile[0][1])
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
        for index in polygon.loop_indices:
            vertex = obj.data.loops[index].vertex_index
            row, j = divmod(vertex, segments + 1)
            u = j / segments
            v = (profile[row][1] - profile[0][1]) / height
            if label is not None:
                v = (3 - label + v) / 4
            uv.data[index].uv = (u, v)
    return obj


def tub(name, radius, height, label):
    p = part(name)
    body = carbon if label == 0 else cream
    lo, hi = height * .16, height * .78
    lathe('Injection moulded base and recessed foot', p, [(0, 0), (radius*.90, 0), (radius, .006), (radius, lo)], body)
    lathe('Continuous printed sleeve (not an overlapping decal)', p, [(radius, lo), (radius, hi)], printwork, label=label)
    lathe('Rounded shoulder and neck', p, [(radius, hi), (radius, height*.84), (radius*.97, height*.88), (radius*.83, height*.92), (radius*.83, height*.95)], body)
    lathe('Tamper evident neck seal', p, [(radius*.86, height*.90), (radius*.91, height*.905), (radius*.91, height*.925), (radius*.86, height*.93)], bronze)
    lathe('Knurled screw closure', p, [(radius*.92, height*.93), (radius*.98, height*.94), (radius*.98, height*.99), (radius*.94, height), (0, height)], rubber, segments=128, ribs=.00065)
    return p


tub('WheyTub', .088, .245, 0)
tub('CreatineJar', .060, .145, 1)
p = part('Shaker')
lathe('Moulded shaker base', p, [(0,0),(.036,0),(.041,.005),(.041,.033)], carbon)
lathe('Tapered printed shaker body', p, [(.041,.033),(.043,.09),(.047,.18)], printwork, label=2)
lathe('Shoulder and reinforced neck', p, [(.047,.18),(.049,.188),(.050,.194)], carbon)
lathe('Threaded ergonomic lid with grip', p, [(.050,.19),(.053,.194),(.053,.207),(.050,.214),(.04,.22),(.028,.226),(0,.227)], rubber, segments=96, ribs=.00045)
lathe('Drinking spout with protective flip cap', p, [(0,.222),(.014,.222),(.016,.228),(.016,.246),(.014,.250),(0,.250)], carbon, segments=32, at=(0,-.017,0))
box('Flip cap release tab',p,(0,-.035,.238),(.019,.016,.009),accent,.003)
# A real closed carry loop, not a floating texture or a rectangular handle.
vertices=[];faces=[]
for j in range(33):
    a=j/32*math.tau
    for k in range(9):
        b=k/8*math.tau
        vertices.append(((.020+.004*math.cos(b))*math.cos(a),.035+.004*math.sin(b),.228+(.020+.004*math.cos(b))*math.sin(a)))
for j in range(32):
    for k in range(8):
        a=j*9+k;faces.append((a,a+1,a+10,a+9))
loop=mesh('Integrated shaker carry loop',vertices,faces,p,rubber)
for face in loop.data.polygons:face.use_smooth=True

p=part('ShelvingBay')
for x in [-.78,.78]:
    for y in [-.15,.15]:box('Fine section steel upright',p,(x,y,1.18),(.026,.026,2.36),steel,.004)
for i in range(16):box('Oiled walnut acoustic backing slat',p,(-.738+i*.098,.16,1.23),(.072,.026,2.33),wood,.003)
for z in [.35,.86,1.37,1.88,2.36]:
    box('Honed stone display shelf',p,(0,0,z),(1.60,.37,.027),stone,.006)
    box('Fine bronze shelf fascia',p,(0,-.184,z-.018),(1.60,.013,.032),bronze,.003)
    for x in [-.74,.74]:box('Recessed steel shelf bracket',p,(x,.01,z-.052),(.022,.32,.06),steel,.004)

p=part('Counter')
box('Recessed graphite plinth',p,(0,0,.06),(2.52,.66,.12),steel,.025)
box('Crafted walnut counter carcass',p,(0,0,.52),(2.68,.76,.90),wood,.025)
for i in range(35):box('Hand-finished rounded vertical counter fluting',p,(-1.28+i*.075,-.391,.53),(.050,.030,.83),wood,.014)
box('Monolithic honed limestone countertop',p,(0,0,1.002),(2.82,.90,.065),stone,.014)
box('Fine counter bronze inlay',p,(0,-.412,.94),(2.66,.015,.015),bronze,.004)
box('Payment terminal angled base',p,(.91,.13,1.05),(.18,.15,.048),steel,.012)
screen=box('Payment terminal glass display',p,(.91,.13,1.105),(.17,.13,.018),carbon,.008)
screen.rotation_euler.x=.35
for x in [-.68,.68]:
    lathe('Counter display stone pedestal',p,[(0,1.035),(.115,1.035),(.12,1.04),(.12,1.10),(.112,1.105),(0,1.105)],stone,segments=48,at=(x,-.03,0))

p=part('DisplayIsland')
box('Central feature plinth',p,(0,0,.075),(1.52,.72,.15),steel,.025)
box('Central display walnut cabinet',p,(0,0,.49),(1.65,.82,.79),wood,.035)
box('Central honed limestone top',p,(0,0,.92),(1.74,.91,.07),stone,.02)
box('Central warm metal detail',p,(0,-.414,.85),(1.57,.012,.015),bronze,.003)

p=part('LinearPendant')
box('Linear powder coated housing',p,(0,0,0),(1.80,.075,.065),steel,.012)
box('Recessed opal light diffuser',p,(0,0,-.034),(1.74,.058,.012),opal,.004)
for x in [-.63,.63]:
    lathe('Pendant steel suspension wire',p,[(.0015,.03),(.0015,.68)],steel,segments=8,at=(x,0,0))

p=part('FloorTile')
box('Precisely jointed bevelled limestone floor tile',p,(0,0,-.01),(1.198,.598,.02),stone,.0015)

# Export only kit objects, preserving authored part names and metre scale.
bpy.ops.object.select_all(action='DESELECT')
for o in [kit]+list(kit.children_recursive):o.select_set(True)
bpy.context.view_layer.objects.active=kit
bpy.ops.export_scene.gltf(filepath=str(OUT/'NutritionKit.glb'), export_format='GLB', use_selection=True,
                          export_yup=True, export_apply=True, export_materials='EXPORT', export_cameras=False,
                          export_lights=False, export_animations=False)
for image in bpy.data.images:
    if image.source=='FILE':image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(REVIEW/'BRAXHORN-Nutrition-authored-source.blend'))
report={'kind':'Original owner-authorized BRAXHORN retail asset kit','blender':bpy.app.version_string,
        'metres':True,'parts':list(parts),'unityVisualAcceptance':False,
        'glbSha256':hashlib.sha256((OUT/'NutritionKit.glb').read_bytes()).hexdigest(),
        'glbBytes':(OUT/'NutritionKit.glb').stat().st_size,
        'sourceScript':'unity-tools/nutrition/create_models.py','previewIsUnityGameplay':False}
(OUT/'authored-models.json').write_text(json.dumps(report,indent=2)+'\n')

# Small asset preview to inspect geometry and print orientation before Unity import.
for o in parts.values():o.hide_render=True
for name,at in [('WheyTub',(-.16,0,0)),('CreatineJar',(.07,-.035,0)),('Shaker',(.22,.005,0))]:
    p=parts[name];p.hide_render=False;p.location=at
    for o in p.children_recursive:o.hide_render=False
for name in ['ShelvingBay','Counter','DisplayIsland','LinearPendant','FloorTile']:
    for o in parts[name].children_recursive:o.hide_render=True
floor=box('Asset studio surface',None,(0,0,-.025),(200,200,.05),stone,0)
world=bpy.context.scene.world
world.use_nodes=True
world.node_tree.nodes.get('Background').inputs[0].default_value=(.24,.27,.29,1)
world.node_tree.nodes.get('Background').inputs[1].default_value=.35
for name,loc,power,size in [('Studio softbox',(-.5,-.6,1.1),80,1.0),('Studio rim',(.7,.2,.7),55,.7)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);o.location=loc
    o.rotation_euler=(Vector((0,0,.12))-o.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.cameras.new('Asset authoring inspection camera')
o=bpy.data.objects.new('Asset authoring inspection camera',camera);bpy.context.collection.objects.link(o)
o.location=(.55,-1.15,.63);o.rotation_euler=(Vector((.035,0,.115))-o.location).to_track_quat('-Z','Y').to_euler()
camera.type='ORTHO';camera.ortho_scale=.72
scene=bpy.context.scene;scene.camera=o;scene.render.engine='CYCLES';scene.cycles.samples=32
scene.cycles.use_denoising=False;scene.render.resolution_x=1200;scene.render.resolution_y=840;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.filepath=str(REVIEW/'products-authoring-preview.png')
bpy.ops.render.render(write_still=True)
print('NUTRITION_AUTHORED_MODELS',json.dumps(report))
