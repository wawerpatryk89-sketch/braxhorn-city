"""G1 GALERIA GŁÓWNA + KINO (owner 11.11 16:33 board; 17:32-17:53 library goods only, no blocks, Poly Haven only, premium
signs; 18:26 "W galerii ma być tylko sklepy i kino", 18:27 the BRAXHORN HOME furniture store in the east wing, 18:28-18:32
no all-white interiors, no bare walls): the C# generator against the shared layout and the owner's rules. No Unity needed."""
import json
import re
import unittest
from pathlib import Path

import layout as L
from prepare_assets import POLYHAVEN_SLUGS

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city/Assets/Editor/Mistrzowie'
RT = ROOT / 'unity-city/Assets/Mistrzowie'
FILES = ['BraxChampionsG1.cs', 'BraxChampionsG1Bryla.cs', 'BraxChampionsG1Wnetrze.cs', 'BraxChampionsG1Sklepy.cs',
         'BraxChampionsG1Kino.cs', 'BraxChampionsG1Meble.cs', 'BraxChampionsG1Lazienki.cs']


def src(name=None):
    return (ED / name).read_text() if name else '\n'.join((ED / f).read_text() for f in FILES)


def consts():
    out = {}
    for decl in re.findall(r'public const float ([^;]+);', src('BraxChampionsG1.cs')):
        for part in decl.split(','):
            name, _, expr = part.partition('=')
            out[name.strip()] = eval(re.sub(r'(\d)f\b', r'\1', expr.strip()), {}, dict(out))
    return out


class G1Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.c = consts()
        cls.s = src()

    def test_every_file_has_its_meta_and_the_bowling_file_is_gone(self):
        for f in FILES:
            self.assertTrue((ED / (f + '.meta')).exists(), f)
        for f in ['BraxChampionsKino.cs', 'BraxChampionsKinoEkrany.cs']:
            self.assertTrue((RT / (f + '.meta')).exists(), f)
        self.assertFalse((ED / 'BraxChampionsG1Rozrywka.cs').exists())
        guids = [re.search(r'guid: (\w+)', (ED / (f + '.meta')).read_text()).group(1) for f in FILES]
        self.assertEqual(len(guids), len(set(guids)))

    def test_centre_and_street_mouths_match_layout(self):
        c = self.c
        self.assertEqual(L.G1_CENTRE, (c['G1CentreX'], c['G1CentreZ']))
        s = src('BraxChampionsG1.cs')
        z70 = re.findall(r'\("(\w+)",([-\d.]+)f,([-\d.]+)f\)', re.search(r'G1MouthsZ70=\{(.+?)\};', s).group(1))
        x800 = re.findall(r'\("(\w+)",([-\d.]+)f,([-\d.]+)f\)', re.search(r'G1MouthsX800=\{(.+?)\};', s).group(1))
        self.assertEqual([(i, float(a) + c['G1CentreX'], float(b) + c['G1CentreX']) for i, a, b in z70], L.G1_MOUTHS_Z70)
        self.assertEqual([(i, float(a) + c['G1CentreZ'], float(b) + c['G1CentreZ']) for i, a, b in x800], L.G1_MOUTHS_X800)

    def test_only_shops_and_the_cinema(self):
        # owner 18:26: no bowling, no billiards in the gallery (E1 has them); the east wing is BRAXHORN HOME
        for word in ('BraxChampionsKregle', 'BraxChampionsBilard', 'G1Bowling', 'G1Billiards', 'KRĘGLE', 'BILARD'):
            self.assertNotIn(word, self.s, word)
        self.assertIn('G1Home();', src('BraxChampionsG1.cs'))
        self.assertIn('"SOBCZAK MEBLE"', src('BraxChampionsG1Meble.cs'))   # owner 23:37: the furniture store is SOBCZAK MEBLE

    def test_poly_haven_only(self):
        # every texture slug is a fetched Poly Haven slug; no ambientCG library material (Leather037, Marble016/021,
        # Metal048A, Travertine) in G1
        for slug in set(re.findall(r'Tex\("[^"]+","([^"]+)"', self.s)):
            self.assertIn(slug, POLYHAVEN_SLUGS, slug)
        self.assertNotIn('BraxChampionsFoundation.Library', self.s)
        self.assertIsNone(re.search(r'\bLibrary(Leather|Marble|MarbleWhite|Metal|Travertine)\(', self.s))

    def test_no_white_interiors(self):
        # owner 18:28: shops and malls not all white - timber slat ceilings, oak / walnut fixtures, no white display blocks
        w = src('BraxChampionsG1Wnetrze.cs')
        unit = w[w.index('static void ShopUnit('):w.index('static void ShopFront(')]
        self.assertIn('SlatCeiling(ix0+.01f,ix1-.01f,iz0+.02f,iz1-.02f,yc,null)', unit)
        self.assertIn('Q(gWallWarm,', unit)
        self.assertNotIn('ceilWhite', unit)
        ceil = w[w.index('static void G1Ceilings()'):w.index('static void G1Galleries()')]
        self.assertNotIn('ceilWhite', ceil)
        fit = src('BraxChampionsG1Sklepy.cs')
        fit = fit[fit.index('static void ShopFitOut('):]
        self.assertNotIn('gWhitePanel', fit)
        self.assertIn('Table(gOak', fit)

    def test_no_bare_walls(self):
        # owner 18:32: the side walls finished (battens, cabinets, lit shelves) in the trades without wall fixtures
        fit = src('BraxChampionsG1Sklepy.cs')
        for trade in ('"electronics"', '"jewellery"', '"cafe"', '"salon"', '"service"', '"flowers"', '"home"'):
            i = fit.index('case ' + trade)
            j = fit.find('            case "', i + 10)
            j = j if j > 0 else fit.index('if(lib)BraxSklepyTowar.Zakoncz', i)
            self.assertIn('SideWall(', fit[i:j], trade)

    def test_furniture_is_built_properly(self):
        m = src('BraxChampionsG1Meble.cs')
        # sofas: legs, base, rolled arms, separate seat and back cushions, pillows; every seat a BraxSeat
        sofa = m[m.index('static void HSofa('):m.index('static void HCoffeeRound(')]
        for part in ('HLeg(', 'k.Lean(12f)', 'Seat(', 'accent'):
            self.assertIn(part, sofa)
        bed = m[m.index('static void HBed('):m.index('static void HNightstand(')]
        self.assertIn('channel', bed)
        self.assertIn('Seat(', bed)
        self.assertIn('Seat(', m[m.index('static void HDiningChair('):m.index('static void HBed(')])
        self.assertIn('Seat(', m[m.index('static void HOfficeChair('):m.index('static void HBench(')])
        # no flat RoundedBox-less boxes for the furniture: every piece rounded
        self.assertNotIn('m.Box(', m[:m.index('// ================================================================ BRAXHORN HOME')])
        # the materials: fabrics, leathers, woods from Poly Haven
        for slug in ('curly_teddy_natural', 'velour_velvet', 'brown_leather', 'oak_wood_planks', 'herringbone_parquet', 'hessian_230'):
            self.assertIn('"' + slug + '"', m)

    def test_functional(self):
        s = self.s
        self.assertIn('AddComponent<BraxChampionsKino>()', s)
        self.assertIn('AddComponent<BraxSchodyRuchome>()', s)
        self.assertIn('BuildingLift(', s + src('BraxChampionsD2Pietro.cs'))
        self.assertGreater(s.count('Door('), 5)
        self.assertGreater(s.count('Seat('), 5)
        rep = json.loads((ROOT / 'unity-city/Assets/StreamingAssets/Mistrzowie/Kino/repertuar.json').read_text())
        self.assertIn('filmy', rep)
        self.assertIn('"com.unity.modules.video"', (ROOT / 'unity-city/Packages/manifest.json').read_text())

    def test_the_owners_two_films(self):
        # owner 19:00-19:09: two films, his posters at the tills, free shows, every seat a seat
        kino = ROOT / 'unity-city/Assets/StreamingAssets/Mistrzowie/Kino'
        rep = json.loads((kino / 'repertuar.json').read_text())['filmy']
        self.assertEqual([f['plik'] for f in rep], ['bezprawie.mp4', 'bez_granic_1.mp4'])
        for f in rep:
            for key in ('plakat', 'baner'):
                self.assertTrue((kino / f[key]).exists(), f[key])
                self.assertTrue((kino / (f[key] + '.meta')).exists(), f[key])
        self.assertIn('*.mp4', (kino / '.gitignore').read_text())
        inst = (ROOT / 'unity-tools/mistrzowie/install_kino_films.py').read_text()
        for f in rep:
            self.assertRegex(inst, '"' + f['plik'] + r'": "[0-9a-f]{64}"')
        self.assertIn('install_kino_films.py', (ROOT / '.github/workflows/mistrzowie-apk.yml').read_text())
        rt = (RT / 'BraxChampionsKino.cs').read_text()
        self.assertIn('public bool zaDarmo = true;', rt)
        self.assertIn('public string baner', rt)
        k = src('BraxChampionsG1Kino.cs')
        self.assertIn('slots.Add(100+i)', k)
        self.assertIn('Seat(x,yk+.55f,z0+.66f', k)

    def test_glass_has_black_fittings(self):
        g = src('BraxChampionsG1.cs')
        self.assertRegex(g, r'frame=S\("g1_black_anodised_aluminium",new Color\(\.07f,\.07f,\.08f\)')
        self.assertNotIn('gold', g.lower().replace('no gold', ''))

    def test_signs_are_3d_on_carriers(self):
        s = self.s
        self.assertNotIn('TextMesh', s)
        self.assertGreater(s.count('PortalSign('), 3)
        # SALA n letters face the corridor (owner 17:52: no cheap / wrong lettering)
        self.assertIn('west?90f:-90f,.24f', src('BraxChampionsG1Kino.cs'))

    def test_site_has_no_open_edges(self):
        g = src('BraxChampionsG1.cs')
        self.assertIn('G1Edges();', g)
        self.assertIn('static void G1Edges()', g)


class G1Owner1947Tests(unittest.TestCase):
    # owner 11.11 19:47-20:18: black / red / white facade, MULTIPLEX lit red, no fin on the docks, one column, the hall's
    # LOMBARD GSM (no indoor tree), real toilets with running taps, fountains with moving water, the furnishing catalogue,
    # chairs' backs as wide as their seats, small tills on the promenade, a turning kebab
    def test_facade_black_red_white(self):
        g = src('BraxChampionsG1.cs'); b = src('BraxChampionsG1Bryla.cs')
        self.assertIn('"g1_letters_lit_red"', g)
        self.assertIn('gRedLit,gLetterSide,"MULTIPLEX"', b)
        self.assertIn('"g1_m02_white_composite"', g)
        self.assertIn('"rectangular_facade_tiles"', g)
        self.assertNotIn('{-45f,0f,27f,47f})Fin', b)

    def test_one_column_everywhere(self):
        b = src('BraxChampionsG1Bryla.cs')
        loop = b[b.index('// ---- columns on the 10 m grid'):b.index('// ---- the plinth')]
        self.assertIn('G1Column(x,z,G1F0,top)', loop)
        for old in ('gWhitePanel', 'hConcrete', 'gWood'):
            self.assertNotIn(old, loop)

    def test_hall_kiosk_and_no_indoor_tree(self):
        w = src('BraxChampionsG1Wnetrze.cs')
        hall = w[w.index('static void G1Hall()'):w.index('static void BarStool(')]
        self.assertIn('"LOMBARD GSM"', hall)
        self.assertNotIn('Tree(', hall)
        self.assertIn('PlanGalerii', w + (RT / 'BraxChampionsKinoEkrany.cs').read_text())
        self.assertIn('MenuLokalu(px,i,j)', w)

    def test_toilets_are_real(self):
        l = src('BraxChampionsG1Lazienki.cs')
        self.assertIn('static void UrinalReal(', l)
        self.assertIn('the drain at\n    // the LOWEST point', l)        # the R1 urinal geometry (owner 20:39), the drain at the bowl's bottom
        self.assertIn('FountainWater(', l)          # the sensor taps run
        self.assertIn('BraxChampionsMirror.Wall(', l)
        self.assertIn('"long_white_tiles"', l)

    def test_fountains_move(self):
        d2 = (ED / 'BraxChampionsD2Autobusy.cs').read_text(); pz = (ED / 'BraxChampionsPromenadeZones.cs').read_text()
        for f in (d2, pz):
            self.assertIn('FountainWater(', f)
        self.assertNotIn('m.Cylinder(waterJet', d2)
        self.assertNotIn('m.Cylinder(curtain', pz)
        self.assertTrue((RT / 'BraxChampionsFontanna.cs').exists() and (RT / 'BraxChampionsFontanna.cs.meta').exists())
        self.assertIn('MarkDynamic', (RT / 'BraxChampionsFontanna.cs').read_text())

    def test_catalogue(self):
        k = (ED / 'BraxKatalog.cs').read_text()
        for f in ('public static void Tv(', 'public static void Laptop(', 'public static void Monitor(', 'public static void Kitchen(',
                  'public static void Wardrobe(', 'public static void Dresser(', 'public static void MediaUnit(', 'public static void Vanity('):
            self.assertIn(f, k)
        self.assertIn('"BRAXHORN"', k)
        self.assertTrue((ED / 'BraxKatalog.cs.meta').exists())
        m = src('BraxChampionsG1Meble.cs')
        for f in ('BraxKatalog.Kitchen(', 'BraxKatalog.Wardrobe(', 'BraxKatalog.Dresser(', 'BraxKatalog.MediaUnit(', 'BraxKatalog.Vanity('):
            self.assertIn(f, m)

    def test_chair_backs_as_wide_as_seats(self):
        w = src('BraxChampionsG1Wnetrze.cs')
        self.assertIn('new Vector3(.46f,.36f,.05f)', w[w.index('static void G1Chair('):])

    def test_promenade_small_tills_and_kebab(self):
        e = (ED / 'BraxChampionsPromenadeEquipment.cs').read_text()
        self.assertNotIn('PackModel(root,"kasa"', e)
        self.assertIn('static void SmallTill(', e)
        self.assertIn('spin.stopniNaSekunde=30f', e)


class G1OwnerMediaTests(unittest.TestCase):
    # owner 11.11 19:00-20:47: his films and TV ads kept encrypted in the repo (public), staged in CI, looping on the TVs
    def test_packs_complete(self):
        import hashlib
        for name in ('mistrzowie-kino-filmy-20261111', 'mistrzowie-reklamy-20261111'):
            d = ROOT / 'unity-city/Prywatne' / name
            m = json.loads((d / 'manifest.json').read_text())
            self.assertTrue((d / 'recipient.crt').exists() and (d / 'recipient.pem.enc').exists())
            for part in m['parts']:
                self.assertLess(part['bytes'], 100 * 1024 * 1024)
                self.assertEqual(hashlib.sha256((d / part['name']).read_bytes()).hexdigest(), part['sha256'])
        films = json.loads((ROOT / 'unity-city/Prywatne/mistrzowie-kino-filmy-20261111/manifest.json').read_text())['files']
        self.assertEqual(films['bezprawie.mp4'], '5952685b1193f1b3f9f29b319a055e767d45a336fc41766a1db527a99a0656b1')

    def test_no_raw_video_in_git(self):
        import subprocess
        tracked = subprocess.run(['git', 'ls-files', '*.mp4'], cwd=ROOT, capture_output=True, text=True).stdout.split()
        self.assertFalse([t for t in tracked if 'StreamingAssets/Mistrzowie' in t])
        self.assertIn('*.mp4', (ROOT / 'unity-city/Assets/StreamingAssets/Mistrzowie/Reklamy/.gitignore').read_text())

    def test_every_tv_works(self):
        # owner 20:57: every TV can be switched on and off and plays the films (the global library, BraxTelewizor)
        k = (ED / 'BraxKatalog.cs').read_text()
        self.assertIn('AddComponent<BraxTelewizor>()', k)
        self.assertIn('WorkingScreen(c, Screen("tv_"', k)
        rt = (ROOT / 'unity-city/Assets/BraxhornWideo/BraxTelewizor.cs').read_text()
        for t in ('IBraxInterakcja', '"WŁĄCZ TV"', '"WYŁĄCZ TV"', 'BraxhornWideo/', 'loopPointReached'):
            self.assertIn(t, rt)
        self.assertTrue((ROOT / 'docs/zasady/ZASADA_TELEWIZORY_FILMY_20261111.md').exists())
        self.assertIn('owner_media()', (ROOT / 'unity-tools/mistrzowie/prepare_assets.py').read_text())


class G1ShopModelsTests(unittest.TestCase):
    def test_owner_clothes_pack_is_global(self):
        # owner 22:44: the Tripo clothes / bags / shoes for every shop in the game - one encrypted pack, one installer
        m = json.loads((ROOT / 'unity-city/Prywatne/braxhorn-sklep-ubrania-01/manifest.json').read_text())
        self.assertEqual(m['target'], 'sklep_ubrania_01')
        self.assertTrue(any(k.endswith('ubranie_trencz_bezowy_wieszak.glb') for k in m['files']))
        self.assertFalse(any(p.suffix in ('.glb', '.png', '.jpg', '.zip') for p in (ROOT / 'unity-city/Prywatne/braxhorn-sklep-ubrania-01').iterdir()))
        self.assertIn('install_sklep_modele', (ROOT / 'unity-tools/mistrzowie/install_owner_media.py').read_text())
        sk = (ED / 'BraxChampionsG1Sklepy.cs').read_text()
        self.assertIn('BraxMiastoAssety.Model(pack,n+"_wieszak")', sk)
        self.assertTrue((ROOT / 'docs/zasady/ZASADA_SKLEP_MODELE_GLOBALNE_20261111.md').exists())
        for pack, target in (('braxhorn-sklep-buty-01', 'sklep_buty_01'), ('braxhorn-sklep-torebki-01', 'sklep_torebki_01'),
                             ('braxhorn-sklep-ubrania-02', 'sklep_ubrania_02'), ('braxhorn-sklep-manekiny-01', 'sklep_manekiny_01'),
                             ('braxhorn-sklep-manekiny-02', 'sklep_manekiny_02'), ('braxhorn-sklep-sport-01', 'sklep_sport_01'),
                             ('braxhorn-sklep-bizuteria-01', 'sklep_bizuteria_01'), ('braxhorn-sklep-sprzet-01', 'sklep_sprzet_01'),
                             ('braxhorn-sklep-buty-02', 'sklep_buty_02'), ('braxhorn-sklep-walka-01', 'sklep_walka_01'),
                             ('braxhorn-sklep-silownia-01', 'silownia_sprzet_01')):
            mf = json.loads((ROOT / 'unity-city/Prywatne' / pack / 'manifest.json').read_text())
            self.assertEqual(mf['target'], target)
            self.assertFalse(any(p.suffix in ('.glb', '.png', '.jpg') for p in (ROOT / 'unity-city/Prywatne' / pack).iterdir()))
            for part in mf['parts']: self.assertTrue((ROOT / 'unity-city/Prywatne' / pack / part['name']).exists())
        self.assertIn('ShopModel(ButyPaczka', sk); self.assertIn('ShopModel(TorebkiPaczka', sk)
        # owner 23:03 "na manekinie też musisz dać ciuchy komplet": his dressed mannequins; 23:22 the white ones cut away
        self.assertIn('JModel(k,', sk); self.assertIn('ShopModel(Sprzet,Pilki', sk); self.assertIn('Sport+"sport_koszulka_braxhorn_czarno_zlota"', sk)
        self.assertIn('ModelMannequin(mset', sk); self.assertIn('Ubrania2+"ubranie_ramoneska_czarna"', sk)
        m2 = json.loads((ROOT / 'unity-city/Prywatne/braxhorn-sklep-manekiny-02/manifest.json').read_text())
        self.assertEqual(sum(k.endswith('.glb') and k.startswith('sklep_manekiny_02/') for k in m2['files']), 4)


if __name__ == '__main__':
    unittest.main()
