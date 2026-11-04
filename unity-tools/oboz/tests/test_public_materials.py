import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('public_materials',Path(__file__).parents[1]/'public_materials.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)

class PublicMaterialTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
        self.payload=b'test texture bytes'
        self.entry={'path':'unity-city/Assets/Source/Public/Oboz/test/Color.jpg','url':'https://dl.polyhaven.org/file/ph-assets/Textures/jpg/test.jpg',
                    'bytes':len(self.payload),'sha256':hashlib.sha256(self.payload).hexdigest()}
    def test_cached_bytes_need_no_network_and_keep_guid(self):
        target=m.valid_destination(self.entry,self.root);target.parent.mkdir(parents=True);target.write_bytes(self.payload)
        with patch.object(m.subprocess,'run') as run:
            m.restore(self.entry,self.root);run.assert_not_called()
        meta=target.with_suffix('.jpg.meta');guid=meta.read_text()
        with patch.object(m.subprocess,'run'):
            m.restore(self.entry,self.root)
        self.assertEqual(meta.read_text(),guid)
    def test_bad_download_does_not_replace_existing_source(self):
        target=m.valid_destination(self.entry,self.root);target.parent.mkdir(parents=True);target.write_bytes(b'preserve original')
        def download(cmd,**kw):Path(cmd[-1]).write_bytes(b'corrupt download')
        with patch.object(m.subprocess,'run',side_effect=download),self.assertRaises(ValueError):m.restore(self.entry,self.root)
        self.assertEqual(target.read_bytes(),b'preserve original')
    def test_verified_download_keeps_existing_meta(self):
        target=m.valid_destination(self.entry,self.root);target.parent.mkdir(parents=True)
        meta=target.with_suffix('.jpg.meta');meta.write_text('guid: keep-me')
        def download(cmd,**kw):Path(cmd[-1]).write_bytes(self.payload)
        with patch.object(m.subprocess,'run',side_effect=download):m.restore(self.entry,self.root)
        self.assertEqual(target.read_bytes(),self.payload);self.assertEqual(meta.read_text(),'guid: keep-me')
    def test_unexpected_host_and_traversal_are_rejected(self):
        for change in ({'url':'http://dl.polyhaven.org/file/ph-assets/Textures/test'}, {'url':'https://example.com/test'}, {'path':'../outside.jpg'}):
            with self.subTest(change=change),self.assertRaises(ValueError):m.valid_destination(dict(self.entry,**change),self.root)

if __name__=='__main__':unittest.main()
