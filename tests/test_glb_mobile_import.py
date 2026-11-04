import base64, copy, json, struct, sys, tempfile, unittest
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from prepare_glb_mobile_import import prepare,compact_externalized
from audit_oboz_environment import glb

PNG=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aP4sAAAAASUVORK5CYII=')
def fixture(doc):
    b=PNG+b'\0'*((-len(PNG))%4);j=json.dumps(doc).encode();j+=b' '*((-len(j))%4)
    return struct.pack('<4sII',b'glTF',2,28+len(j)+len(b))+struct.pack('<II',len(j),0x4E4F534A)+j+struct.pack('<II',len(b),0x004E4942)+b
def document():
    return {'asset':{'version':'2.0','extras':{'author':'Fixture owner'}},'buffers':[{'byteLength':len(PNG)+(-len(PNG))%4}],
      'bufferViews':[{'buffer':0,'byteOffset':0,'byteLength':len(PNG)}],
      'images':[{'bufferView':0,'mimeType':'image/png'}],'textures':[{'source':0}],
      'materials':[{'pbrMetallicRoughness':{'baseColorTexture':{'index':0}}}],
      'nodes':[],'meshes':[],'accessors':[],'scenes':[{'nodes':[]}],'scene':0}

class MobileGlbTests(unittest.TestCase):
    def setUp(self):self.tmp=tempfile.TemporaryDirectory();self.path=Path(self.tmp.name)/'source.glb'
    def tearDown(self):self.tmp.cleanup()
    def save(self,d):self.path.write_bytes(fixture(d));return self.path.read_bytes()
    def test_lossless_source_geometry_and_attribution(self):
        d=document();before=self.save(d);r=prepare(self.path)
        after,b=glb(self.path.with_name(r['derived']).read_bytes());_,original_binary=glb(before)
        self.assertEqual(self.path.read_bytes(),before);self.assertEqual(b,original_binary)
        for k in ['asset','nodes','meshes','accessors','materials']:self.assertEqual(after[k],d[k])
        self.assertNotIn('bufferView',after['images'][0]);self.assertTrue(r['maps'][0]['color'])
        self.assertEqual((self.path.parent/after['images'][0]['uri']).read_bytes(),PNG)
    def test_data_texture_stays_linear(self):
        d=document();d['materials']=[{'normalTexture':{'index':0}}];self.save(d);r=prepare(self.path)
        self.assertFalse(r['maps'][0]['color']);self.assertIn('__lin',r['maps'][0]['file'])
    def test_conflicting_colour_spaces_stop_instead_of_guessing(self):
        d=document();d['materials'][0]['normalTexture']={'index':0};before=self.save(d)
        with self.assertRaisesRegex(ValueError,'shared'):prepare(self.path)
        self.assertEqual(self.path.read_bytes(),before);self.assertFalse(self.path.with_name('source__unity.glb').exists())
    def test_external_image_is_not_followed(self):
        d=document();d['images']=[{'uri':'../../unrelated.png'}];self.save(d)
        with self.assertRaisesRegex(ValueError,'not embedded'):prepare(self.path)
    def test_truncation_rejected(self):
        self.save(document());self.path.write_bytes(self.path.read_bytes()[:-1])
        with self.assertRaisesRegex(ValueError,'header/length'):prepare(self.path)
    def test_unknown_image_encoding_rejected(self):
        d=document();d['images'][0]['mimeType']='image/ktx2';self.save(d)
        with self.assertRaisesRegex(ValueError,'Unsupported'):prepare(self.path)
    def test_shared_textures_deduplicate_without_mixing_colour_spaces(self):
        self.save(document());a=prepare(self.path,'vegetation_textures')
        other=self.path.with_name('other.glb');other.write_bytes(self.path.read_bytes());b=prepare(other,'vegetation_textures')
        self.assertEqual(a['maps'][0]['file'],b['maps'][0]['file'])
        d=document();d['materials']=[{'normalTexture':{'index':0}}];other.write_bytes(fixture(d));c=prepare(other,'vegetation_textures')
        self.assertNotEqual(a['maps'][0]['file'],c['maps'][0]['file'])
        self.assertEqual(len(list((self.path.parent/'vegetation_textures').iterdir())),2)
    def test_shared_texture_directory_cannot_escape_source_parent(self):
        self.save(document())
        with self.assertRaisesRegex(ValueError,'direct child'):prepare(self.path,'../elsewhere_textures')
    def test_compaction_preserves_geometry_and_sparse_accessor_bytes(self):
        binary=b'OLDIMAGE'+b'VERTICES'+b'JOINTS00'+b'SPARSE00'
        d={'buffers':[{'byteLength':32}],'bufferViews':[{'buffer':0,'byteOffset':i*8,'byteLength':8} for i in range(4)],
          'images':[{'uri':'shared/color.png'}],'accessors':[{'bufferView':1,'sparse':{'indices':{'bufferView':3},'values':{'bufferView':2}}}]}
        packed,remap=compact_externalized(d,binary)
        self.assertEqual(packed,b'VERTICESJOINTS00SPARSE00');self.assertEqual(remap,{1:0,2:1,3:2})
        self.assertEqual(d['accessors'][0]['sparse']['indices']['bufferView'],2)
        self.assertEqual(d['accessors'][0]['sparse']['values']['bufferView'],1)
        self.assertEqual(d['buffers'][0]['byteLength'],24)

if __name__=='__main__':unittest.main()
