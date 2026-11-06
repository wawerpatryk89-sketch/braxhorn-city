"""Validate transfer on the real city's rig without launching Unity."""
import copy
import gzip
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
import prepare_brax as brax

class BraxTransferTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temp = tempfile.TemporaryDirectory()
        cls.out = Path(cls.temp.name)
        cls.report = brax.prepare(out=cls.out)
        cls.source, cls.source_buffer = brax.read_glb(gzip.decompress((brax.ROOT/'public/assets/chars/brax.glb.gz').read_bytes()))
        cls.result, cls.buffer = brax.read_glb((cls.out/'BraxOriginal.glb').read_bytes())

    @classmethod
    def tearDownClass(cls): cls.temp.cleanup()

    def test_original_rig_geometry_and_buffer_preserved(self):
        for key in ('nodes','skins','meshes','accessors'):
            source=copy.deepcopy(self.source.get(key))
            if key=='meshes':
                for mesh in source:
                    for primitive in mesh['primitives']: primitive['material']=0
            self.assertEqual(source,self.result.get(key))
        self.assertEqual(self.source_buffer,self.buffer[:len(self.source_buffer)])

    def test_all_six_city_clips_keep_original_channels(self):
        expected=[a for a in self.source['animations'] if a.get('name') in brax.REQUIRED]
        self.assertEqual(expected,self.result['animations'])
        self.assertEqual(len(expected),6)
        self.assertFalse(self.report['retargeted'])

    def test_original_glb_has_mixamo_car_rig_bones(self):
        import re
        names=[n.get("name","") for n in self.source.get("nodes",[])]
        def bone(value):
            value=value.replace("\\\\","/").rsplit("/",1)[-1].rsplit("|",1)[-1]
            value=value.rsplit(":",1)[-1].strip()
            value=re.sub(r"^mixamorig[0-9]*","",value,flags=re.I)
            return value.lstrip(":_.- ").lower()
        normalized={bone(name) for name in names}
        required=("Hips","Spine","LeftUpLeg","RightUpLeg",
                  "LeftLeg","RightLeg","LeftArm","RightArm")
        missing=[name for name in required if name.lower() not in normalized]
        print("BRAX_SOURCE_RIG_DIAGNOSTIC skinCount=",len(self.source.get("skins",[])),
              "nodeCount=",len(names),"missingMixamo=",missing,
              "originalNodeNames=",names[:90])
        self.assertTrue(self.source.get("skins"),
                        "Brax original GLB has NO skinned rig; cannot rebind real car FBX")
        self.assertFalse(missing,"Original Brax GLB missing Mixamo bones: "+
                         repr(missing)+"; names="+repr(names[:90]))

    def test_missing_run_is_found_before_writing_output(self):
        broken=copy.deepcopy(self.source)
        broken['animations']=[a for a in broken['animations'] if a.get('name')!='Running']
        target=self.out/'missing'
        with patch.object(brax,'read_glb',return_value=(broken,self.source_buffer)):
            with self.assertRaisesRegex(ValueError,'Running'): brax.prepare(out=target)
        self.assertFalse(target.exists())

    def test_source_maps_are_present(self):
        for name in ['BraxAlbedo.png','BraxNormal.png','BraxOrm.png']:
            with brax.Image.open(self.out/name) as im: self.assertGreaterEqual(min(im.size),1024)

if __name__=='__main__': unittest.main()
