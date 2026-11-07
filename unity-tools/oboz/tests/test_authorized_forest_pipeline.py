"""Offline source/installation checks; these do not claim Unity or phone results."""
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('authorized_forest_pipeline',ROOT/'unity-tools/oboz/prepare_authorized_forest.py')
pipeline=importlib.util.module_from_spec(spec);spec.loader.exec_module(pipeline)
CATALOG=json.loads((ROOT/'unity-tools/oboz/AUTHORIZED_TREE_CATALOG_20261106.json').read_text())


class CatalogContractTests(unittest.TestCase):
    def test_exact_named_sources_and_real_authored_lods(self):
        files=pipeline.catalog_files(CATALOG)
        self.assertEqual(len([row for row in files if row['kind']=='model']),3)
        self.assertEqual([p['name'] for p in CATALOG['prototypes']],['pine_tree_01_b','tree_small_02'])
        self.assertEqual([p['missingAuthoredLod2'] for p in CATALOG['prototypes']],[False,True])
        self.assertEqual(sum(lod['triangles'] for p in CATALOG['prototypes'] for lod in p['lods']),1277856)

    def test_incomplete_or_decimated_input_cannot_be_installed(self):
        for change in ({'complete':False},{'lod0Imported':True},{'geometryDecimated':True}):
            with self.subTest(change=change),self.assertRaises(ValueError):pipeline.catalog_files(dict(CATALOG,**change))

    def test_only_selected_variant_can_be_installed(self):
        document=copy.deepcopy(CATALOG);document['prototypes'][0]['name']='pine_tree_01_a'
        with self.assertRaises(ValueError):pipeline.catalog_files(document)

    def test_missing_lod2_cannot_be_hidden_or_faked(self):
        document=copy.deepcopy(CATALOG);document['prototypes'][1]['missingAuthoredLod2']=False
        with self.assertRaises(ValueError):pipeline.catalog_files(document)

    def test_no_uri_can_escape_tree_sources(self):
        for path in ('../source.blend','/tmp/source','a\\b','https://unapproved.test/map.png','C:/file'):
            with self.subTest(path=path),self.assertRaises(ValueError):pipeline.safe_relative(path)

    def test_all_final_source_files_have_unique_committed_metadata(self):
        paths=[row['file'] for row in pipeline.catalog_files(CATALOG)]+[pipeline.CATALOG_NAME]
        guids=set()
        for relative in paths:
            text=(ROOT/'unity-city'/pipeline.ASSET_ROOT/(relative+'.meta')).read_text()
            guid=next(line[6:] for line in text.splitlines() if line.startswith('guid: '))
            self.assertEqual(len(guid),32);self.assertNotIn(guid,guids);guids.add(guid)
            if relative.endswith(('.png','.jpg')):self.assertIn('textureShape: 1',text)


class InstallationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.models=self.root/'models';self.billboards=self.root/'billboards'
        self.target=self.root/'target';self.catalog=self.root/'catalog.json';self.report=self.root/'installed.json'
        self.document=copy.deepcopy(CATALOG)
        rows=[row for p in self.document['prototypes'] for row in p['lods']]+self.document['textures']
        for index,row in enumerate(rows):
            content=('verified-installation-fixture-'+str(index)).encode()
            row.update(bytes=len(content),sha256=hashlib.sha256(content).hexdigest())
            relative=row['file'];source=self.billboards/Path(relative).name if relative.startswith('billboards/') else self.models/relative
            source.parent.mkdir(parents=True,exist_ok=True);source.write_bytes(content)
            meta=self.target/(relative+'.meta');meta.parent.mkdir(parents=True,exist_ok=True)
            original=ROOT/'unity-city'/pipeline.ASSET_ROOT/(relative+'.meta');meta.write_bytes(original.read_bytes())
        for prototype in self.document['prototypes']:
            name='billboards/'+prototype['billboard']['file']
            prototype['billboard']['sha256']=next(row['sha256'] for row in rows if row['file']==name)
        (self.target/(pipeline.CATALOG_NAME+'.meta')).write_bytes(
            (ROOT/'unity-city'/pipeline.ASSET_ROOT/(pipeline.CATALOG_NAME+'.meta')).read_bytes())
        self.catalog.write_text(json.dumps(self.document))

    def install(self):return pipeline.install(self.catalog,self.models,self.billboards,self.target,self.report)

    def test_install_and_repeat_preserve_exact_bytes_and_guids(self):
        before={p.relative_to(self.target).as_posix():p.read_bytes() for p in self.target.rglob('*.meta')}
        first=self.install();second=self.install()
        self.assertEqual(first,second);self.assertTrue(first['complete']);self.assertFalse(first['apkBuilt'])
        self.assertEqual(len(first['files']),23)
        for row in first['files']:self.assertEqual(pipeline.digest(self.target/row['file']),row['sha256'])
        self.assertEqual(before,{p.relative_to(self.target).as_posix():p.read_bytes() for p in self.target.rglob('*.meta')})
        self.assertFalse(list(self.target.rglob('*.blend')))
        self.assertFalse(list(self.target.rglob('*LOD0*')))

    def test_tampered_file_is_rejected_before_any_model_is_copied(self):
        source=next(self.models.glob('*.glb'));source.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'Missing or changed'):self.install()
        self.assertFalse(list(self.target.rglob('*.glb')))

    def test_missing_stable_guid_blocks_install_without_new_guids(self):
        meta=next(self.target.glob('*.glb.meta'));meta.write_text('fileFormatVersion: 2\n')
        with self.assertRaisesRegex(ValueError,'GUID'):self.install()
        self.assertFalse(list(self.target.rglob('*.glb')))

    def test_failed_current_preparation_invalidates_only_catalog(self):
        self.install();model=next(self.target.glob('*.glb'));content=model.read_bytes()
        pipeline.invalidate(self.target,'source unavailable')
        self.assertFalse(json.loads((self.target/pipeline.CATALOG_NAME).read_text())['complete'])
        self.assertEqual(model.read_bytes(),content)
        self.assertTrue((self.target/(model.name+'.meta')).is_file())


if __name__=='__main__':unittest.main()
