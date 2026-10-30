"""Re-export the exact reviewed, packed Blender derivative; never accept changed upstream geometry."""
import hashlib,json,uuid
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]
review=ROOT/'reviewed-gym-source'
blend=review/'kickboxing-review/bag-source/bag-refined-before-export.blend'
assert hashlib.sha256(blend.read_bytes()).hexdigest()=='37b70dbd08cd1966e2add2e71b24b96b14e001133f40ce2661648eb608a12e9f'
provenance=review/'unity-city/Assets/Kickboxing/Models/BagSource.provenance.json'
assert hashlib.sha256(provenance.read_bytes()).hexdigest()=='67ae963d7246aad821fb07d4e9c1a02f454660cc9003117bade2cf2049c5bffb'
report=json.loads(provenance.read_text())
out=ROOT/'unity-city/Assets/Kickboxing/Models';out.mkdir(parents=True,exist_ok=True)
for filename,name,count in [('BagSwing.glb','BagSwing_PivotOrigin',16932),('BagMount.glb','BagMount_Fixed',1040)]:
 obj=bpy.data.objects.get(name)
 assert obj is not None and obj.type=='MESH',name
 assert sum(max(0,len(p.vertices)-2) for p in obj.data.polygons)==count,name
 bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
 bpy.ops.export_scene.gltf(filepath=str(out/filename),export_format='GLB',use_selection=True,export_extras=True,export_animations=False,export_image_format='AUTO')
 data=(out/filename).read_bytes()
 assert hashlib.sha256(data).hexdigest()==report['files'][filename]['sha256'],'Reviewed export bytes differ: '+filename
 meta=out/(filename+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'braxhorn-kickboxing/'+filename).hex+'\n')
(out/'BagSource.provenance.json').write_bytes(provenance.read_bytes())
print('EXACT_REVIEWED_BAG_GLB_BYTES_RESTORED_FROM_PINNED_BLEND')
