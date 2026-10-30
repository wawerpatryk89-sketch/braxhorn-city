"""Export real user's exact Poly Haven CC0 Periwinkle Plant 2K from Blender.
Run with Blender 4.x after verified .blend and accompanying maps were staged.
This is real original geometry, not generated primitive foliage or image sprites.
"""
import bpy, pathlib, hashlib, math, json
root=pathlib.Path.cwd()
folder=root/"unity-city/Assets/Source/City/Dom04"
output=folder/"Dom04Periwinkle.glb"
base=bpy.path.abspath("//")
print("DOM04_PERIWINKLE_BLEND_SOURCE",bpy.data.filepath)
assert pathlib.Path(bpy.data.filepath).resolve()==(folder/"periwinkle_plant_2k.blend").resolve()
assert bpy.app.version[0]>=3,"Expected Blender 3+"
diff=folder/"periwinkle_plant_diff_2k.jpg"
alpha=folder/"periwinkle_plant_opacity_2k.png"
assert diff.is_file() and alpha.is_file()
meshes=[o for o in bpy.context.scene.objects if o.type=="MESH"]
if not meshes: raise RuntimeError("Real periwinkle source contains zero geometry")
print("DOM04_REAL_PERIWINKLE_OBJECTS", [(o.name,len(o.data.polygons)) for o in meshes])
# Keep the genuine source mesh with the highest detail; decimate it to a
# physically genuine but bounded mobile LOD (do not synthesise any leaves).
mesh=max(meshes,key=lambda o:len(o.data.polygons))
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.hide_set(False)
    o.hide_render=False
mesh.select_set(True)
bpy.context.view_layer.objects.active=mesh
orig=sum(max(1,len(p.vertices)-2) for p in mesh.data.polygons)
if orig<150:raise RuntimeError("Original periwinkle mesh is implausibly small")
if orig>18000:
    modifier=mesh.modifiers.new(name="Original periwinkle mobile LOD", type="DECIMATE")
    modifier.ratio=max(.08,min(1,14500/orig))
    bpy.ops.object.modifier_apply(modifier=modifier.name)
count=sum(max(1,len(p.vertices)-2) for p in mesh.data.polygons)
if count>20000:raise RuntimeError("Periwinkle exceeds real mobile LOD triangle budget "+str(count))
# Original source diffuse and opacity are reused without inventing a texture.
originalDiffuse=bpy.data.images.load(str(diff),check_existing=True)
originalAlpha=bpy.data.images.load(str(alpha),check_existing=True)
originalDiffuse.colorspace_settings.name="sRGB"
originalAlpha.colorspace_settings.name="Non-Color"
mat=bpy.data.materials.new(name="DOM04 original periwinkle pink flowers / CC0")
mat.use_nodes=True;mat.blend_method="CLIP";mat.alpha_threshold=.48;mat.use_backface_culling=False
nodes=mat.node_tree.nodes; nodes.clear()
out=nodes.new("ShaderNodeOutputMaterial")
principled=nodes.new("ShaderNodeBsdfPrincipled")
principled.inputs["Roughness"].default_value=.79
principled.inputs["Metallic"].default_value=0
diffnode=nodes.new("ShaderNodeTexImage");diffnode.image=originalDiffuse
alphanode=nodes.new("ShaderNodeTexImage");alphanode.image=originalAlpha
links=mat.node_tree.links
links.new(diffnode.outputs["Color"],principled.inputs["Base Color"])
links.new(alphanode.outputs["Color"],principled.inputs["Alpha"])
links.new(principled.outputs["BSDF"],out.inputs["Surface"])
mesh.data.materials.clear();mesh.data.materials.append(mat)
# Center real mesh around own ground-level anchor; retain proportions.
bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bpy.context.view_layer.update()
coords=[mesh.matrix_world@v.co for v in mesh.data.vertices]
mn=[min(p[i] for p in coords) for i in range(3)]
mx=[max(p[i] for p in coords) for i in range(3)]
size=[mx[i]-mn[i] for i in range(3)]
if any(v<=0 for v in size):raise RuntimeError("Source periwinkle bounds invalid "+str(size))
# Applied mesh geometry normalization improves root placement in Unity; not a model substitution.
centerX=(mx[0]+mn[0])*.5;centerY=(mx[1]+mn[1])*.5
for vertex in mesh.data.vertices:
    vertex.co.x-=centerX
    vertex.co.y-=centerY
    vertex.co.z-=mn[2]
mesh.location=(0,0,0)
mesh.rotation_euler=(0,0,0)
bpy.ops.export_scene.gltf(filepath=str(output),export_format="GLB",use_selection=True,export_apply=True,export_animations=False,export_cameras=False,export_lights=False)
if not output.exists() or output.stat().st_size<50000:raise RuntimeError("Periwinkle export missing")
receipt=dict(originalSource="periwinkle_plant_2k.blend",librarySource=True,geometryIsOriginal=True,originalTriangles=orig,outputTriangles=count,outputSha256=hashlib.sha256(output.read_bytes()).hexdigest(),sourceBounds=size,outputPath=str(output.relative_to(root)),visualAcceptance=False)
(root/"city-review").mkdir(exist_ok=True)
(root/"city-review/dom04-periwinkle-export.json").write_text(json.dumps(receipt,indent=2))
print("DOM04_ORIGINAL_LIBRARY_FLOWERS_EXPORTED",json.dumps(receipt))
