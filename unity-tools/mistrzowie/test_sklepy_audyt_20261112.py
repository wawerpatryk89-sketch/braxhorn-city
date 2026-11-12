"""G1 shops after my own audit of every shop (12.11.2026 16:05) and the owner's 16:18-16:19 ("Optyk to ma hektar ... dla
optyka przeznaczyć mały sklepik"): the small shops, the salon, the islands, the gelato case, the TV shop, the bikes."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city/Assets/Editor/Mistrzowie'


def src(name):
    return (ED / name).read_text(encoding='utf-8')


def case(fit, trade):
    i = fit.index('case "' + trade + '"')
    j = fit.find('            case "', i + 10)
    return fit[i:j if j > 0 else fit.index('if(lib)BraxSklepyTowar.Zakoncz', i)]


class SmallShops(unittest.TestCase):
    def test_optician_florist_and_gelato_take_a_small_unit(self):
        plan = src('BraxChampionsG1Wnetrze.cs')
        for small, other in (('OPTYK WIZJA', 'BUTIK BRAXHORN'), ('KWIACIARNIA', 'ŚWIAT RTV'), ('LODY GELATO', 'SPORT OUTDOOR')):
            self.assertIn(f'Small("{small}","{other}"', plan)
        self.assertIn('const float W=6.5f', plan)                       # 6.5 m of the frontage
        self.assertIn('a.back=9f', plan)                                # a 9 m deep sales floor
        self.assertIn('public float back;', plan)

    def test_small_shop_has_a_back_wall_and_door(self):
        fit = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('if(u.back>0f&&u.back<D-1.5f)', fit)
        self.assertIn('D=u.back;', fit)
        self.assertIn('float fw=u.back>0f?Wd-1.75f:Wd-.3f;', fit)       # the feature wall clear of the door

    def test_optician_eye_test_room(self):
        fit = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('bool booth=u.trade=="optician"&&Droom>=4.2f;', fit)
        self.assertIn('TablicaWzroku(88,128)', fit)
        self.assertIn('SalonChair(chairP,-alongT,0f,"fotel do badania wzroku")', fit)
        self.assertIn('m.QuadUV(chart', fit)                           # the chart reads the right way round


class FittedShops(unittest.TestCase):
    def test_salon_is_a_salon(self):
        s = case(src('BraxChampionsG1Sklepy.cs'), 'salon')
        for k in ('G1Mirror(', 'SalonChair(', 'fotel do mycia głowy', 'FountainWater(', 'Battens(', 'HSofa('):
            self.assertIn(k, s)

    def test_islands_in_the_middle_of_cosmetics_and_optician(self):
        s = case(src('BraxChampionsG1Sklepy.cs'), 'cosmetics')
        self.assertIn('float[] rows=Wd>=9f', s)
        self.assertIn('nm+" | wyspa"', s)
        self.assertIn('nm+" | stół z oprawkami"', s)

    def test_gelato_case_and_tv_shop(self):
        fit = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('nm+" | lody"', case(fit, 'cafe'))
        self.assertIn('bool rtv=nm.Contains("RTV")', case(fit, 'electronics'))
        self.assertIn('nm+" TV ściana"', case(fit, 'electronics'))

    def test_all_five_motorbikes_shown(self):
        s = case(src('BraxChampionsG1Sklepy.cs'), 'moto')
        self.assertIn('int rest=mot.Length-mi;', s)
        self.assertIn('d+=1.0f,i++', s)                                # the bikes 1 m apart
        for pack in ('braxhorn-sklep-motory-01', 'braxhorn-sklep-rowery-01', 'braxhorn-sklep-meble-01'):
            self.assertTrue((ROOT / 'unity-city/Prywatne' / pack / 'manifest.json').exists(), pack)


if __name__ == '__main__':
    unittest.main()
