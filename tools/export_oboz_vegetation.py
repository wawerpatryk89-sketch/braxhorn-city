"""Use Blender >=4.4 to export existing authored vegetation LODs, no decimation.

Run with the matching bpy Python. Original .blend archives are never written.
"""
import argparse, hashlib, json, sys
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def export(source_root, output, report_path):
    if bpy.app.version<(4,4,0):raise RuntimeError('Source files need Blender >=4.4')
    bpy.context.preferences.filepaths.use_scripts_auto_execute=False
    output.mkdir(parents=True,exist_ok=True);records=[]
    selections={
      'fir_tree_01_2k.blend':['fir_tree_01_'+v+'_LOD'+str(l) for v in ['a','b','c'] for l in [1,2]],
      'fir_sapling_medium_2k.blend':['fir_sapling_medium_'+v+'_LOD'+str(l) for v in ['a','b','c'] for l in [1,2]],
      'grass_bermuda_01_2k.blend':['grass_bermuda_01_medium_d','grass_bermuda_01_seedling_d'],
    }
    for filename,names in selections.items():
        found=list(source_root.rglob(filename))
        if len(found)!=1:raise ValueError('Missing/ambiguous '+filename)
        source=found[0];before=digest(source)
        bpy.ops.wm.open_mainfile(filepath=str(source.resolve()),load_ui=False,use_scripts=False)
        for name in names:
            original=bpy.data.objects.get(name)
            if original is None:raise ValueError('Missing authored object '+name)
            if original.type!='MESH' or len(original.modifiers):raise ValueError('Unexpected source geometry/modifier '+name)
            # A linked copy preserves the authored mesh; placement is normalized
            # so all LOD variants share the same bottom origin in Unity.
            obj=original.copy();bpy.context.scene.collection.objects.link(obj);obj.hide_set(False);obj.hide_viewport=False;obj.hide_render=False
            obj.location=(0,0,0);bpy.context.view_layer.update()
            bounds=[obj.matrix_world@Vector(c) for c in obj.bound_box]
            obj.location.z-=min(p.z for p in bounds);bpy.context.view_layer.update()
            bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
            path=output/(name+'.glb')
            bpy.ops.export_scene.gltf(filepath=str(path.resolve()),export_format='GLB',use_selection=True,
                export_animations=False,export_skins=False,export_morph=False,export_cameras=False,export_lights=False,
                export_yup=True,export_apply=False,export_materials='EXPORT',export_image_format='AUTO')
            original.data.calc_loop_triangles()
            record={'sourceBlend':filename,'sourceObject':name,'file':path.name,'vertices':len(original.data.vertices),
              'triangles':len(original.data.loop_triangles),'dimensions':list(obj.dimensions),'sourceBlendSha256':before,
              'sha256':digest(path),'geometryDecimated':False,'authoredLod':int(name[-1]) if '_LOD' in name else 0,
              'unityImported':False}
            records.append(record);print(json.dumps(record),flush=True)
            report_path.write_text(json.dumps({'blender':bpy.app.version_string,'models':records,'complete':False,'originalsUnchanged':False},ensure_ascii=False,indent=2)+'\n')
            bpy.data.objects.remove(obj,do_unlink=True)
        if digest(source)!=before:raise ValueError('Original blend changed')
    report_path.write_text(json.dumps({'blender':bpy.app.version_string,'models':records,'complete':True,'originalsUnchanged':True},ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--output',type=Path,required=True);p.add_argument('--report',type=Path,required=True)
    p.add_argument('--authorized-manifest',type=Path,help='Only exact owner-named trees and their authored LOD1/LOD2; sources/output outside Assets')
    argv=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else sys.argv[1:]
    a=p.parse_args(argv)
    if a.authorized_manifest:
        from oboz_authorized_tree_export import export_authorized
        export_authorized(a.sources,a.output,a.report,a.authorized_manifest)
    else:
        export(a.sources,a.output,a.report)
