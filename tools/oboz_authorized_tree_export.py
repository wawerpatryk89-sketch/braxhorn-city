"""Extend the existing camp exporter with verified, authored pine/small-tree LODs.

Plain selection functions are testable without Blender. Export uses the pinned
official CI Blender 4.5.15; no geometry nodes, modifiers, decimation or LOD0 may
stand in for a missing original LOD. .blend sources remain outside Assets.
"""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
import re
import struct


def file_hash(path: Path, algorithm="sha256") -> str:
    result = hashlib.new(algorithm)
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def exported_triangles(path: Path) -> int:
    with path.open("rb") as stream:
        header=stream.read(20)
        if len(header)!=20 or header[:4]!=b"glTF" or header[16:20]!=b"JSON":
            raise ValueError("Export did not produce a GLB JSON header")
        length=struct.unpack_from("<I",header,12)[0]
        document=json.loads(stream.read(length))
    total=0
    for mesh in document.get("meshes",[]):
        for primitive in mesh.get("primitives",[]):
            if primitive.get("mode",4)!=4:continue
            accessor=primitive.get("indices",primitive.get("attributes",{}).get("POSITION"))
            if accessor is None:raise ValueError("Exported primitive has no geometry accessor")
            total+=document["accessors"][accessor]["count"]//3
    return total


def select_authored_lods(asset: dict, inventory: list[dict]) -> tuple[list[dict], list[str]]:
    pattern = re.compile(r"^" + re.escape(asset["id"]) + r"(?:_[a-z])?_LOD(?P<level>[12])$")
    candidates, warnings = {}, []
    for item in inventory:
        match = pattern.fullmatch(item["name"])
        if not match:
            continue
        level = int(match["level"])
        reason = None
        if item["type"] != "MESH": reason = "not an authored mesh"
        elif item["modifiers"]: reason = "has modifiers/procedural evaluation; not silently applied"
        elif item.get("triangles", 0) <= 0: reason = "empty mesh"
        elif item["triangles"] > asset["maxTrianglesPerAuthoredLod"]: reason = "exceeds explicit safe triangle bound"
        if reason:
            warnings.append(item["name"] + ": " + reason)
            continue
        base = item["name"].rsplit("_LOD", 1)[0]
        if asset.get("selectedPrototypes") and base not in asset["selectedPrototypes"]:
            continue
        candidates.setdefault(base, {})[level] = dict(item, lod=level, prototype=base)
    selected = []
    for prototype, levels in sorted(candidates.items()):
        required=set(asset.get("requiredLodLevels",[1,2]))
        if not required.issubset(levels):
            warnings.append(prototype + ": incomplete authored LOD1/LOD2 pair")
            continue
        selected.extend(levels[level] for level in sorted(required))
    if not selected:
        warnings.append(asset["id"] + ": no usable authored LOD pair; no LOD0 substitute exported")
    return selected, warnings


def foliage_source_evidence(asset: dict, materials) -> dict:
    """Read the actual source graph; do not guess alpha from a filename alone."""
    name='pine_tree_01_twig' if asset['id']=='pine_tree_01' else 'tree_small_02_leaves'
    material=next((m for m in materials if m and m.name==name),None)
    if material is None or not material.use_nodes:
        raise ValueError('Missing original foliage material '+name)
    images=[n for n in material.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
    expected=Path(asset['foliageAlpha']).name
    alpha=next((n for n in images if Path(n.image.filepath).name==expected),None)
    if alpha is None:raise ValueError('Original alpha image node missing '+name)
    samplers={(n.interpolation,n.extension) for n in images}
    if samplers!={('Linear','REPEAT')}:
        raise ValueError('Source foliage samplers differ; cannot merge original RGB/alpha '+name)
    evidence=dict(material=name,alphaImage=expected,samplerInterpolation='Linear',samplerExtension='REPEAT',
        allImageSamplersIdentical=True,restoreExportedOpaqueAlpha=False,sourceCoverageVerified=False)
    if asset['id']=='pine_tree_01':
        # Official pine source uses a transparent mix after its surface shaders.
        # Blender glTF exports its Principled branch but loses this final mix.
        outputs=[n for n in material.node_tree.nodes if n.type=='OUTPUT_MATERIAL' and n.is_active_output]
        if len(outputs)!=1 or len(outputs[0].inputs['Surface'].links)!=1:
            raise ValueError('Unsupported original pine material output')
        mix=outputs[0].inputs['Surface'].links[0].from_node
        if mix.type!='MIX_SHADER' or len(mix.inputs[0].links)!=1 or mix.inputs[0].links[0].from_node!=alpha or \
            len(mix.inputs[1].links)!=1 or mix.inputs[1].links[0].from_node.type!='BSDF_TRANSPARENT' or \
            len(mix.inputs[2].links)!=1 or mix.inputs[2].links[0].from_node.type!='ADD_SHADER':
            raise ValueError('Original pine alpha mix differs; no guessed conversion')
        evidence.update(sourceCoverageVerified=True,restoreExportedOpaqueAlpha=True,
            sourceGraph='alpha Color -> final Mix Fac; transparent first, source surface second')
    else:
        links=[l for output in alpha.outputs for l in output.links]
        if len(links)!=1 or links[0].to_node.type!='GROUP' or links[0].to_socket.name!='Alpha':
            raise ValueError('Original small-tree alpha input differs')
        evidence.update(sourceCoverageVerified=True,sourceGraph='alpha Color -> source material group Alpha')
    return evidence


def export_authorized(source_root: Path, output: Path, report_path: Path, manifest_path: Path):
    import bpy
    from mathutils import Vector
    report = dict(title="OBÓZ TRENINGOWY — oryginalne LOD wskazanych drzew",schema=1,
                  blender=bpy.app.version_string,models=[],assets=[],warnings=[],
                  geometryDecimated=False,unityImported=False,apkBuilt=False,
                  sourceBlendFilesUnchanged=True,foliageColorPackingVerified=False,knownLimitations=[],
                  note="Original source LOD export only. Repair original RGB/alpha packing and externalize maps before Unity use.")
    def write():
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2) + "\n")
    def warn(text):
        report["warnings"].append(text)
        print("BRAXHORN_OBOZ_AUTHORED_TREE_WARNING " + text, flush=True)
    if tuple(bpy.app.version) != (4, 5, 15):
        warn("Expected pinned Blender 4.5.15; no tree exported with " + bpy.app.version_string)
        report["complete"] = False; write(); return report
    if any(part.casefold() == "assets" for part in source_root.resolve().parts) or any(part.casefold() == "assets" for part in output.resolve().parts):
        warn("Source and raw export must remain outside Unity Assets")
        report["complete"] = False; write(); return report
    document = json.loads(manifest_path.read_text())
    if document.get("schema") != 1 or {x.get("id") for x in document.get("assets", [])} != {"pine_tree_01", "tree_small_02"}:
        warn("Unexpected authorized-tree manifest")
        report["complete"] = False; write(); return report
    output.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.use_scripts_auto_execute = False
    for asset in document["assets"]:
        for limitation in asset.get("knownLimitations",[]):
            report["knownLimitations"].append(limitation)
            print("BRAXHORN_OBOZ_AUTHORED_TREE_WARNING "+limitation,flush=True)
        source_dir = source_root / asset["sourceSubdirectory"]
        source = source_dir / asset["blendFile"]
        item = dict(id=asset["id"],sourceBlend=str(source),inventory=[],exported=[],complete=False)
        report["assets"].append(item)
        try:
            # Verify ALL source dependencies, not just the .blend container.
            for entry in asset["files"]:
                path = source_dir / entry["relativePath"]
                if not path.is_file() or path.stat().st_size != entry["bytes"] or file_hash(path,"md5") != entry["md5"]:
                    raise ValueError("Missing/unverified original dependency: " + entry["relativePath"])
            before = file_hash(source)
            item["sourceBlendSha256"] = before
            bpy.ops.wm.open_mainfile(filepath=str(source.resolve()),load_ui=False,use_scripts=False)
            for original in bpy.data.objects:
                row = dict(name=original.name,type=original.type,modifiers=[m.type for m in original.modifiers])
                if original.type == "MESH":
                    row["vertices"] = len(original.data.vertices)
                    row["polygonCount"] = len(original.data.polygons)
                    # Calculate triangles only for named candidate LOD1/2. Do
                    # not evaluate a multi-million-polygon Geometry Nodes LOD0.
                    if re.fullmatch(re.escape(asset["id"]) + r"(?:_[a-z])?_LOD[12]", original.name) and not original.modifiers:
                        original.data.calc_loop_triangles()
                        row["triangles"] = len(original.data.loop_triangles)
                item["inventory"].append(row)
            selected, issues = select_authored_lods(asset,item["inventory"])
            for issue in issues: warn(issue)
            for selection in selected:
                original = bpy.data.objects[selection["name"]]
                coverage=foliage_source_evidence(asset,[slot.material for slot in original.material_slots])
                obj = original.copy() # shared original mesh; no mesh mutation
                world = original.matrix_world.copy()
                obj.parent = None
                bpy.context.scene.collection.objects.link(obj)
                obj.matrix_world = world
                obj.location = (0,0,0)
                obj.hide_set(False); obj.hide_viewport=False; obj.hide_render=False
                bpy.context.view_layer.update()
                bounds = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
                obj.location.z -= min(point.z for point in bounds)
                bpy.context.view_layer.update()
                bpy.ops.object.select_all(action="DESELECT")
                obj.select_set(True); bpy.context.view_layer.objects.active=obj
                path = output / (selection["name"] + ".glb")
                try:
                    bpy.ops.export_scene.gltf(filepath=str(path.resolve()),export_format="GLB",use_selection=True,
                        export_animations=False,export_skins=False,export_morph=False,export_cameras=False,
                        export_lights=False,export_yup=True,export_apply=False,export_materials="EXPORT",export_image_format="AUTO")
                    record = dict(sourceAsset=asset["id"],sourceBlend=asset["blendFile"],sourceObject=original.name,
                        file=path.name,vertices=len(original.data.vertices),triangles=selection["triangles"],
                        exportedTriangles=exported_triangles(path),
                        dimensions=list(obj.dimensions),sourceBlendSha256=before,sha256=file_hash(path),
                        geometryDecimated=False,authoredLod=selection["lod"],prototype=selection["prototype"],
                        foliageSourceEvidence=coverage,
                        unityImported=False,sourceMaterialNames=[slot.material.name if slot.material else None for slot in original.material_slots])
                    report["models"].append(record); item["exported"].append(record["file"])
                    if record["exportedTriangles"]!=record["triangles"]:
                        warn(record["file"]+": exported triangle count differs from authored mesh")
                    print(json.dumps(record),flush=True); write()
                finally:
                    bpy.data.objects.remove(obj,do_unlink=True)
            if file_hash(source) != before:
                report["sourceBlendFilesUnchanged"] = False
                raise ValueError("Source .blend changed during export")
            item["complete"] = (bool(selected) and len(item["exported"]) == len(selected) and not issues and
                all(row["exportedTriangles"] == row["triangles"] for row in report["models"] if row["sourceAsset"] == asset["id"]))
        except Exception as error:
            warn(asset["id"] + ": " + str(error))
        write()
    report["complete"] = all(item["complete"] for item in report["assets"]) and report["sourceBlendFilesUnchanged"] and not report["warnings"]
    write()
    return report
