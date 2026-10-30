"""Run via Blender --background --disable-autoexec <original.blend> --python thisfile -- <asset>.
Original model/geometry/UVs, repath official textures and export GLB.
"""
import bpy,sys,pathlib
root=pathlib.Path.cwd()
name=sys.argv[sys.argv.index("--")+1] if "--" in sys.argv else ""
if name not in ("planter_box_01","periwinkle_plant"):raise RuntimeError("DOM04 original asset name required")
folder=root/"dom04-flower-originals"/name
assets=root/"unity-city/Assets/Source/City/Dom04"
assets.mkdir(parents=True,exist_ok=True)
images={p.name:p for p in (folder/"textures").iterdir() if p.is_file()}
for im in bpy.data.images:
    if im.source!="FILE":continue
    label=pathlib.Path(bpy.path.abspath(im.filepath)).name or pathlib.Path(im.name).name
    if label not in images:
        if im.packed_file:continue
        # A few Blender scene-generated images are not source model textures.
        if im.name in ("Render Result","Viewer Node"):continue
        raise RuntimeError("Missing actual source PBR map "+name+"/"+label)
    im.filepath=str(images[label]);im.reload()
    if max(im.size)>1024:
        k=1024/max(im.size);im.scale(max(1,round(im.size[0]*k)),max(1,round(im.size[1]*k)))
    im.pack()
objects=[o for o in bpy.context.scene.objects if o.type=="MESH" and not o.hide_render]
if not objects:raise RuntimeError("Original "+name+" BLEND has no visible source meshes")
tris=sum(len(p.vertices)-2 for o in objects for p in o.data.polygons)
if tris<=0:raise RuntimeError("No real mesh faces")
if tris>300000:raise RuntimeError("Unexpected model geometry size: "+name+" "+str(tris))
bpy.ops.object.select_all(action="DESELECT")
for o in objects:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=objects[0]
# Game-ready cap: original photogrammetry plant remains genuine source geometry.
if name=="periwinkle_plant" and tris>16000:
    ratio=16000/tris
    for o in objects:
        if len(o.data.polygons)>150:
            mod=o.modifiers.new("DOM04 source geometry mobile LOD","DECIMATE")
            mod.ratio=max(.07,min(1,ratio))
path=assets/("DOM04_"+name+".glb")
bpy.ops.export_scene.gltf(filepath=str(path),export_format="GLB",use_selection=True,
 export_cameras=False,export_lights=False,export_animations=False,export_apply=True)
if not path.exists() or path.stat().st_size<30000:raise RuntimeError("GLB export invalid "+name)
print("DOM04_REAL_LIBRARY_NAMED_MODEL_EXPORTED",name,"originalTriangles",tris,"GLBbytes",path.stat().st_size)
