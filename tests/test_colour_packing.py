import io,sys,tempfile,unittest
from pathlib import Path
import png
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
from fix_oboz_vegetation_colour import pack

class ColourPackingTests(unittest.TestCase):
    def test_rgb_values_do_not_receive_a_second_gamma_conversion(self):
        with tempfile.TemporaryDirectory() as tmp:
            color=Path(tmp)/'color.png';alpha=Path(tmp)/'alpha.png'
            # Mid-range values caught the original linear-RGB-in-sRGB export.
            with color.open('wb') as f:png.Writer(2,1,greyscale=False,bitdepth=16).write(f,[[32768,20000,9000,65535,1234,42]])
            with alpha.open('wb') as f:png.Writer(2,1,greyscale=True,bitdepth=16).write(f,[[65535,16384]])
            width,height,rows,info=png.Reader(bytes=pack(color,alpha)).asDirect()
            self.assertEqual((width,height,info['bitdepth']),(2,1,16))
            self.assertEqual(list(next(iter(rows))),[32768,20000,9000,65535,65535,1234,42,16384])
    def test_unequal_map_dimensions_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            color=Path(tmp)/'color.png';alpha=Path(tmp)/'alpha.png'
            with color.open('wb') as f:png.Writer(1,1,greyscale=False,bitdepth=16).write(f,[[100,200,300]])
            with alpha.open('wb') as f:png.Writer(2,1,greyscale=True,bitdepth=16).write(f,[[65535,0]])
            with self.assertRaisesRegex(ValueError,'dimensions differ'):pack(color,alpha)

if __name__=='__main__':unittest.main()
