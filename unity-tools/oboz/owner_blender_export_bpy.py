"""Blender-side script, not executed by Python outside Blender.

Usage: blender --background --python owner_blender_export_bpy.py --
    <original.blend> <output.glb> <source-textures-dir>
Only original mesh vertices, UVs, materials and maps. NEVER decimate.
"""
import bpy
import json
from pathlib import Path
import sys

def main():
    if "--" not in sys.argv: raise RuntimeError("Missing script -- arguments")
    args=sys.argv[sys.argv.index("--")+1:]
    if len(args)!=3: raise RuntimeError("Expected input.blend output.glb texture_directory")
    input_file,output_file,texture_dir=map(Path,args)
    bpy.ops.wm.open_mainfile(filepath=str(input_file))
    texture_dir=texture_dir.resolve()
    for image in bpy.data.images:
        if image.source!='FILE' or image.packed_file:continue
        name=Path(bpy.path.abspath(image.filepath)).name
        source=texture_dir/name
        if source.is_file():
            image.filepath=str(source)
            image.reload()
    meshes=[o for o in bpy.data.objects if o.type=='MESH' and not o.hide_render]
    if not meshes:meshes=[o for o in bpy.data.objects if o.type=='MESH']
    if not meshes:raise RuntimeError("No original mesh objects in owner .blend: "+str(input_file))
    # Do not change vertices, faces, UVs, armatures or modifiers.
    # Some original .blend meshes live in collections outside the active
    # ViewLayer. Merely calling select_set() raises RuntimeError in Blender 4.5.
    # Link the SAME source objects and their transform parents to a temporary,
    # visible collection in the current scene. Never save the source .blend.
    staging=bpy.data.collections.new("BRAXHORN_TEMP_SOURCE_EXPORT")
    bpy.context.scene.collection.children.link(staging)
    for obj in meshes:
        node=obj
        while node is not None:
            if staging.objects.get(node.name) is None:
                staging.objects.link(node)
            node=node.parent
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    for obj in meshes:
        # Visibility/selection changes exist only in the unsaved Blender session.
        obj.hide_viewport=False
        obj.hide_select=False
        obj.hide_set(False)
        if bpy.context.view_layer.objects.get(obj.name) is None:
            raise RuntimeError("Owner mesh absent from export ViewLayer: "+obj.name)
        obj.select_set(True)
    bpy.context.view_layer.objects.active=meshes[0]
    target=output_file.resolve()
    target.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=str(target),export_format='GLB',
        use_selection=True,export_apply=False,export_yup=True,
        export_materials='EXPORT')
    if not target.is_file() or target.stat().st_size<1024:
        raise RuntimeError("Missing complete GLB after Blender export")
    report={
        "source":str(input_file),"output":str(target),
        "sourceMeshObjects":len(meshes),
        "sourceVertices":sum(len(o.data.vertices) for o in meshes),
        "sourcePolygons":sum(len(o.data.polygons) for o in meshes),
        "meshes":[{"name":o.name,"vertices":len(o.data.vertices),
                   "faces":len(o.data.polygons)} for o in meshes],
        "decimationApplied":False,
        "originalBlenderFilesChanged":False,
    }
    report_path=target.with_suffix(".export-audit.json")
    report_path.write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("BRAXHORN_OBOZ_ORIGINAL_BLENDER_EXPORT=PASS",report)

if __name__=="__main__":
    main()
