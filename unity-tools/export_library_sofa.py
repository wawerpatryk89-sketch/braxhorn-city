"""Export the existing, verified CC0 library mesh. Blender runs with auto-exec disabled."""
import bpy, pathlib, json
root=pathlib.Path.cwd();source=root/'asset-library/sources/sofa_02';out=root/'unity-city/Assets/Source/LibrarySofa02.glb'
textures={p.name:p for p in (source/'textures').iterdir() if p.is_file()}
for image in bpy.data.images:
 if image.source!='FILE':continue
 filename=pathlib.Path(image.filepath).name
 if filename not in textures:raise RuntimeError('Missing original sofa texture '+filename)
 image.filepath=str(textures[filename]);image.reload()
 if max(image.size)>2048:
  ratio=2048/max(image.size);image.scale(round(image.size[0]*ratio),round(image.size[1]*ratio))
 image.pack()
bpy.ops.object.select_all(action='DESELECT')
models=[o for o in bpy.context.scene.objects if o.type=='MESH' and not o.hide_render]
if not models:raise RuntimeError('No original library sofa mesh')
for obj in models:obj.hide_set(False);obj.select_set(True)
triangles=sum(len(o.data.polygons)*2 for o in models)
if triangles>150000:raise RuntimeError('Library object exceeds reviewed geometry budget')
out.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.gltf(filepath=str(out),export_format='GLB',use_selection=True,export_cameras=False,export_lights=False,export_animations=False,export_apply=True)
if not out.exists() or out.stat().st_size<50000:raise RuntimeError('Empty library export')
print('ORIGINAL_LIBRARY_SOFA_EXPORTED',out.stat().st_size,triangles)
