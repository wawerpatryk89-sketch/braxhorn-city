"""Offline proof that real Poly Haven textures and geometry survive glTF->GLB without edits."""
import json
import struct
import tempfile
from pathlib import Path
import unittest
from pack_casino_vegetation_glb import pack_embedded_original

ROOT=Path(__file__).resolve().parents[2]

class NativeVegetationEmbeddedGlb(unittest.TestCase):
    def test_lossless_embed_and_reread_material_images(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name)
            (root/"textures").mkdir()
            mesh=b"\x01\x02\x03\x04\x05\x06\x07\x08\x09\x10\x11\x12"
            jpg=b"\xff\xd8\xff\xd9"
            png=b"\x89PNG\r\n\x1a\n"
            (root/"model.bin").write_bytes(mesh)
            (root/"textures/diff.jpg").write_bytes(jpg)
            (root/"textures/nor.png").write_bytes(png)
            src=root/"original.gltf"
            original={
                "asset":{"version":"2.0"},
                "buffers":[{"byteLength":len(mesh),"uri":"model.bin"}],
                "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":len(mesh)}],
                "accessors":[{"bufferView":0,"componentType":5126,"count":1,"type":"VEC3"}],
                "meshes":[{"primitives":[{"attributes":{"POSITION":0},"material":0}]}],
                "nodes":[{"mesh":0}],"scenes":[{"nodes":[0]}],"scene":0,
                "images":[{"uri":"textures/diff.jpg"},{"uri":"textures/nor.png"}],
                "textures":[{"source":0},{"source":1}],
                "materials":[{"pbrMetallicRoughness":{"baseColorTexture":{"index":0}},
                              "normalTexture":{"index":1}}]
            }
            before=json.dumps(original).encode()
            src.write_bytes(before)
            dst=root/"converted.glb"
            receipt=pack_embedded_original(src,dst)
            data=dst.read_bytes()
            magic,version,size=struct.unpack_from("<4sII",data)
            self.assertEqual((magic,version,size),(b"glTF",2,len(data)))
            js_len,js_type=struct.unpack_from("<I4s",data,12)
            self.assertEqual(js_type,b"JSON")
            packed=json.loads(data[20:20+js_len])
            bin_pos=20+js_len
            bin_len,bin_type=struct.unpack_from("<I4s",data,bin_pos)
            self.assertEqual(bin_type,b"BIN\x00")
            binary=data[bin_pos+8:bin_pos+8+bin_len]
            self.assertEqual(binary[:len(mesh)],mesh)
            self.assertEqual(packed["materials"],original["materials"])
            self.assertEqual(packed["meshes"],original["meshes"])
            for i,img in enumerate(packed["images"]):
                view=packed["bufferViews"][img["bufferView"]]
                got=binary[view["byteOffset"]:view["byteOffset"]+view["byteLength"]]
                self.assertEqual(got,[jpg,png][i])
                self.assertNotIn("uri",img)
            self.assertEqual(src.read_bytes(),before)
            self.assertTrue(receipt["sourceImagesUnchanged"])
            self.assertTrue(receipt["binaryUnchanged"])
            self.assertEqual(pack_embedded_original(src,dst),receipt)

    def test_rejects_escape_and_missing_files(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name)
            source=root/"original.gltf"
            content={"asset":{"version":"2.0"},"meshes":[{}],
                     "buffers":[{"byteLength":4,"uri":"../../out.bin"}],
                     "images":[{"uri":"x.png"}]}
            source.write_text(json.dumps(content))
            with self.assertRaises(ValueError):
                pack_embedded_original(source,root/"out.glb")

    def test_build_uses_only_lossless_embedded_originals(self):
        src=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs").read_text()
        prep=(ROOT/"unity-tools/mistrzowie/prepare_casino_vegetation_2k.py").read_text()
        self.assertIn('shrub_03_2k_embedded.glb',src)
        self.assertIn('quiver_tree_02_2k_embedded.glb',src)
        self.assertNotIn('shrub_03_2k.gltf");',src)
        self.assertIn('unity-city/OriginalSources/',prep)
        self.assertIn('pack_embedded_original',prep)
        self.assertIn('meta(embedded)',prep)

if __name__=="__main__":
    unittest.main()
