import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile
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

    def archive_fixture(self):
        source=self.root/'source.zip'
        member='Grass001_4K-JPG_Color.jpg'
        with zipfile.ZipFile(source,'w') as z:z.writestr(member,self.payload)
        file=dict(self.entry,member=member);file.pop('url')
        entry={'asset':'Grass001','license':'CC0-1.0',
               'url':'https://ambientcg.com/get?file=Grass001_4K-JPG.zip',
               'bytes':source.stat().st_size,'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
               'files':[file]}
        return source,entry

    def test_archive_restore_then_cache_preserves_metadata(self):
        source,entry=self.archive_fixture()
        m.restore_archive(entry,self.root,source)
        target=m.material_path(entry['files'][0],self.root)
        meta=target.with_suffix('.jpg.meta');meta.write_text('guid: retained')
        with patch.object(m.subprocess,'run') as run:
            m.restore_archive(entry,self.root);run.assert_not_called()
        self.assertEqual(target.read_bytes(),self.payload)
        self.assertEqual(meta.read_text(),'guid: retained')

    def test_corrupt_archive_does_not_replace_existing_source(self):
        source,entry=self.archive_fixture()
        target=m.material_path(entry['files'][0],self.root);target.parent.mkdir(parents=True)
        target.write_bytes(b'keep existing')
        source.write_bytes(b'broken')
        with self.assertRaises(ValueError):m.restore_archive(entry,self.root,source)
        self.assertEqual(target.read_bytes(),b'keep existing')

    def test_bad_member_hash_does_not_replace_existing_source(self):
        source,entry=self.archive_fixture();entry['files'][0]['sha256']='0'*64
        target=m.material_path(entry['files'][0],self.root);target.parent.mkdir(parents=True)
        target.write_bytes(b'keep existing')
        with self.assertRaises(ValueError):m.restore_archive(entry,self.root,source)
        self.assertEqual(target.read_bytes(),b'keep existing')

    def test_archive_member_and_destination_traversal_rejected(self):
        for key,value in [('member','../Color.jpg'),('path','../outside.jpg')]:
            source,entry=self.archive_fixture();entry['files'][0][key]=value
            with self.subTest(key=key),self.assertRaises(ValueError):m.restore_archive(entry,self.root,source)

    def test_unapproved_archive_source_rejected(self):
        source,entry=self.archive_fixture();entry['url']='https://example.com/get?file=Grass001_4K-JPG.zip'
        with self.assertRaises(ValueError):m.restore_archive(entry,self.root,source)

if __name__=='__main__':unittest.main()

