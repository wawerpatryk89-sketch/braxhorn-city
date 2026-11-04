import tempfile
import unittest
from pathlib import Path
from PIL import Image
from pack_materials import pack, linear_mean

class MaterialPackingTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
    def image(self,name,value,size=(4,4),mode='L'):
        path=self.root/name;Image.new(mode,size,value).save(path);return path
    def test_source_ao_roughness_metal_are_in_correct_channels(self):
        r=self.image('rough.png',91);a=self.image('ao.png',43);m=self.image('metal.png',172)
        out=self.root/'arm.png';pack(r,out,a,m,size=(4,4))
        with Image.open(out) as im:
            self.assertEqual(im.size,(4,4));self.assertEqual(set(im.getdata()),{(43,91,172)})
    def test_nonmetal_without_ao_is_explicitly_neutral(self):
        r=self.image('rough.png',81);out=self.root/'arm.png';pack(r,out,size=(4,4))
        with Image.open(out) as im:self.assertEqual(im.getpixel((0,0)),(255,81,0))
    def test_dimension_mismatch_preserves_old_output(self):
        r=self.image('rough.png',90);a=self.image('ao.png',20,size=(2,2));out=self.root/'arm.png';out.write_bytes(b'previous')
        with self.assertRaisesRegex(ValueError,'resizing forbidden'):pack(r,out,a,size=(4,4))
        self.assertEqual(out.read_bytes(),b'previous')
    def test_coloured_data_is_rejected(self):
        r=self.image('rough.png',(10,20,30),mode='RGB')
        with self.assertRaisesRegex(ValueError,'not grayscale'):pack(r,self.root/'arm.png',size=(4,4))
    def test_linear_calibration_leaves_source_untouched(self):
        p=self.image('color.png',(128,128,128),mode='RGB');before=p.read_bytes()
        for v in linear_mean(p):self.assertAlmostEqual(v,.21586,places=4)
        self.assertEqual(before,p.read_bytes())

if __name__=='__main__':unittest.main()
