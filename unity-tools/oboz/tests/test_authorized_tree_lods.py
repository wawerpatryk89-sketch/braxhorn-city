import importlib.util
import copy
import sys
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools'))
spec = importlib.util.spec_from_file_location("oboz_authorized_tree_export", ROOT / "tools/oboz_authorized_tree_export.py")
exporter = importlib.util.module_from_spec(spec)
spec.loader.exec_module(exporter)
finish_spec = importlib.util.spec_from_file_location("finish_oboz_authorized_trees", ROOT / "tools/finish_oboz_authorized_trees.py")
finisher = importlib.util.module_from_spec(finish_spec)
finish_spec.loader.exec_module(finisher)


class AuthoredTreeSelectionTests(unittest.TestCase):
    asset = {"id": "pine_tree_01", "maxTrianglesPerAuthoredLod": 1000000}

    @staticmethod
    def mesh(name, triangles=1000, modifiers=None):
        return {"name": name, "type": "MESH", "triangles": triangles, "modifiers": modifiers or []}

    def test_only_matching_original_lod_pairs_are_selected(self):
        inventory = [self.mesh("pine_tree_01_a_LOD0", 6951524),
                     self.mesh("pine_tree_01_a_LOD1", 50000), self.mesh("pine_tree_01_a_LOD2", 5000),
                     self.mesh("fir_tree_01_a_LOD1"), self.mesh("fir_tree_01_a_LOD2")]
        selected, warnings = exporter.select_authored_lods(self.asset, inventory)
        self.assertEqual([x["name"] for x in selected], ["pine_tree_01_a_LOD1", "pine_tree_01_a_LOD2"])
        self.assertEqual(warnings, [])

    def test_missing_pair_does_not_substitute_lod0(self):
        selected, warnings = exporter.select_authored_lods(self.asset,
            [self.mesh("pine_tree_01_a_LOD0", 6951524), self.mesh("pine_tree_01_a_LOD1")])
        self.assertEqual(selected, [])
        self.assertTrue(any("incomplete" in issue for issue in warnings))

    def test_procedural_modifiers_are_not_silently_applied(self):
        selected, warnings = exporter.select_authored_lods(self.asset,
            [self.mesh("pine_tree_01_a_LOD1", modifiers=["NODES"]), self.mesh("pine_tree_01_a_LOD2")])
        self.assertEqual(selected, [])
        self.assertTrue(any("modifiers" in issue for issue in warnings))

    def test_wrong_named_multimillion_mesh_is_rejected(self):
        selected, warnings = exporter.select_authored_lods(self.asset,
            [self.mesh("pine_tree_01_a_LOD1", 6951524), self.mesh("pine_tree_01_a_LOD2", 4000)])
        self.assertEqual(selected, [])
        self.assertTrue(any("triangle bound" in issue for issue in warnings))

    def test_small_tree_without_variant_suffix_is_supported(self):
        asset = dict(self.asset, id="tree_small_02")
        selected, warnings = exporter.select_authored_lods(asset,
            [self.mesh("tree_small_02_LOD1", 25000), self.mesh("tree_small_02_LOD2", 2000)])
        self.assertEqual([row["lod"] for row in selected], [1, 2])
        self.assertEqual(warnings, [])

    def test_verified_single_lod_small_tree_keeps_missing_lod2_explicit(self):
        asset=dict(self.asset,id='tree_small_02',requiredLodLevels=[1],selectedPrototypes=['tree_small_02'])
        selected,warnings=exporter.select_authored_lods(asset,[self.mesh('tree_small_02_LOD0',2000000),self.mesh('tree_small_02_LOD1',495533)])
        self.assertEqual([row['lod'] for row in selected],[1]);self.assertEqual(warnings,[])

    def test_selected_pine_b_does_not_export_a_or_c(self):
        asset=dict(self.asset,selectedPrototypes=['pine_tree_01_b'])
        inventory=[self.mesh('pine_tree_01_'+variant+'_LOD'+str(level)) for variant in 'abc' for level in (1,2)]
        selected,warnings=exporter.select_authored_lods(asset,inventory)
        self.assertEqual({row['prototype'] for row in selected},{'pine_tree_01_b'});self.assertEqual(warnings,[])


@unittest.skipUnless(importlib.util.find_spec("png") and importlib.util.find_spec("numpy"), "PNG packing dependencies installed in source-preparation step")
class OriginalFoliagePixelsTests(unittest.TestCase):
    def test_original_8_and_16_bit_rgb_and_alpha_are_exact(self):
        import png
        for depth in (8,16):
            with self.subTest(depth=depth), tempfile.TemporaryDirectory() as folder:
                root=Path(folder);maximum=(1<<depth)-1
                color=[[maximum,17,29,31,maximum//2,0]]
                opacity=[[0,maximum]]
                with (root/'diff.png').open('wb') as file:
                    png.Writer(width=2,height=1,greyscale=False,bitdepth=depth).write(file,color)
                with (root/'alpha.png').open('wb') as file:
                    png.Writer(width=2,height=1,greyscale=True,bitdepth=depth).write(file,opacity)
                raw=finisher.pack_original_rgba(root/'diff.png',root/'alpha.png')
                width,height,rows,info=png.Reader(bytes=raw).asDirect()
                self.assertEqual((width,height,info['bitdepth'],info['planes']),(2,1,depth,4))
                self.assertEqual(list(next(iter(rows))),[maximum,17,29,0,31,maximum//2,0,maximum])

    def test_mismatched_source_precision_is_reported_without_resampling(self):
        import png
        with tempfile.TemporaryDirectory() as folder:
            root=Path(folder)
            with (root/'diff.png').open('wb') as file:
                png.Writer(width=1,height=1,greyscale=False,bitdepth=16).write(file,[[1000,2000,3000]])
            with (root/'alpha.png').open('wb') as file:
                png.Writer(width=1,height=1,greyscale=True,bitdepth=8).write(file,[[128]])
            with self.assertRaisesRegex(ValueError,'precision differ'):
                finisher.pack_original_rgba(root/'diff.png',root/'alpha.png')


class SourceAlphaCoverageTests(unittest.TestCase):
    def setUp(self):
        import base64,json,struct
        self.temporary=tempfile.TemporaryDirectory();self.addCleanup(self.temporary.cleanup)
        self.path=Path(self.temporary.name)/'pine_tree_01_b_LOD1.glb'
        self.png=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jOa0AAAAASUVORK5CYII=')
        self.binary=struct.pack('<9f',0,0,0,1,0,0,0,1,0)+self.png
        self.binary+=b'\0'*(-len(self.binary)%4)
        self.document={
          'asset':{'version':'2.0'},'scene':0,'scenes':[{'nodes':[0]}],
          'nodes':[{'name':'Original_tree','mesh':0,'translation':[2,3,4]}],
          'buffers':[{'byteLength':len(self.binary)}],
          'bufferViews':[{'buffer':0,'byteOffset':0,'byteLength':36},
                         {'buffer':0,'byteOffset':36,'byteLength':len(self.png)}],
          'accessors':[{'bufferView':0,'componentType':5126,'count':3,'type':'VEC3'}],
          'meshes':[{'name':'Original','primitives':[{'attributes':{'POSITION':0},'material':0}]}],
          'samplers':[{'minFilter':9987,'magFilter':9729,'wrapS':10497,'wrapT':10497}],
          'images':[{'bufferView':1,'mimeType':'image/png'}],
          'textures':[{'source':0,'sampler':0}],
          'materials':[{'name':'pine_tree_01_twig','pbrMetallicRoughness':{
              'baseColorTexture':{'index':0,'texCoord':0},'metallicFactor':.35,'roughnessFactor':.7},'doubleSided':True}]}
        self.original=self.write();self.path.write_bytes(self.original)
        self.evidence=dict(sourceCoverageVerified=True,allImageSamplersIdentical=True,
                           material='pine_tree_01_twig',restoreExportedOpaqueAlpha=True)

    def write(self):
        import json,struct
        doc=json.dumps(self.document).encode();doc+=b' '*(-len(doc)%4)
        return struct.pack('<4sII',b'glTF',2,28+len(doc)+len(self.binary))+struct.pack('<I4s',len(doc),b'JSON')+doc+struct.pack('<I4s',len(self.binary),b'BIN\0')+self.binary

    def test_verified_source_mix_restores_only_lost_alpha_without_geometry_edits(self):
        result=finisher.repair_model(self.path,'pine_tree_01',self.png,self.evidence)
        document,binary=finisher.read_glb(self.path.read_bytes())
        expected=copy.deepcopy(self.document['materials']);expected[0]['alphaMode']='BLEND'
        self.assertEqual(document['materials'],expected)
        for key in ('meshes','accessors','nodes','textures','samplers'):
            self.assertEqual(document[key],self.document[key])
        self.assertTrue(binary.startswith(self.binary))
        self.assertEqual(result['sourceAlphaCoverageRestored'],['pine_tree_01_twig'])

    def test_no_original_graph_evidence_means_no_guessed_transparency(self):
        with self.assertRaisesRegex(ValueError,'verified original foliage'):
            finisher.repair_model(self.path,'pine_tree_01',self.png,{})
        self.assertEqual(self.path.read_bytes(),self.original)

    def test_a_different_opaque_material_cannot_use_pine_repair_rule(self):
        self.document['materials'][0]['name']='tree_small_02_leaves'
        original=self.write();self.path.write_bytes(original)
        evidence=dict(self.evidence,material='tree_small_02_leaves')
        with self.assertRaisesRegex(ValueError,'without verified repair rule'):
            finisher.repair_model(self.path,'tree_small_02',self.png,evidence)
        self.assertEqual(self.path.read_bytes(),original)


if __name__=='__main__':unittest.main()
