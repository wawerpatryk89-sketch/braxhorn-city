"""Owner 12.11.2026 13:58-14:03: the review of every Tripo model ("przejrzeć te modele ... co się nadaje, zostawiamy, co nie, to
nie i tyle ... czego nie da rady naprawić" - out; "te buty ... tragedia", "zegarki nie mają tarczy z tyłu", "i te piłki")."""
import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city' / 'Assets' / 'Editor' / 'Mistrzowie'
PACKS = ROOT / 'unity-city' / 'Prywatne'
TOOLS = ROOT / 'unity-tools' / 'sklep'


def src(name):
    return (ED / name).read_text(encoding='utf-8')


class PrzegladModeli(unittest.TestCase):
    def test_fused_shoe_pairs_are_not_put_out(self):
        s = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('const string ButyPaczka="sklep_buty_03"', s)
        self.assertIn('pack=="sklep_buty_01"||pack=="sklep_buty_02"', s)
        for arr in ('SportButy', 'Buty'):
            body = re.search(r'static readonly string\[\] ' + arr + r'=\{(.*?)\};', s, re.S).group(1)
            self.assertNotIn('sklep_buty_02/', body); self.assertNotIn('"buty_', body); self.assertNotIn('zapasnicze', body)
            self.assertIn('"but_czarny_biala_podeszwa"', body)

    def test_single_shoes_pack(self):
        m = json.loads((PACKS / 'braxhorn-sklep-buty-03' / 'manifest.json').read_text(encoding='utf-8'))
        self.assertEqual(m['target'], 'sklep_buty_03')
        glbs = sorted(k.split('/')[-1][:-4] for k in m['files'] if k.startswith('sklep_buty_03/') and k.endswith('.glb'))
        self.assertEqual(len(glbs), 14)
        s = src('BraxChampionsG1Sklepy.cs')
        for arr in ('SportButy', 'Buty'):
            body = re.search(r'static readonly string\[\] ' + arr + r'=\{(.*?)\};', s, re.S).group(1)
            for name in re.findall(r'"(but_[a-z_]+)"', body):
                self.assertIn(name, glbs, name)
        # every pack sits next to the others with the same recipient
        self.assertEqual(m['recipientCertificateSha256'], json.loads((PACKS / 'braxhorn-sklep-buty-02' / 'manifest.json').read_text())['recipientCertificateSha256'])

    def test_rejected_models(self):
        s = src('BraxChampionsG1Sklepy.cs')
        rej = re.search(r'static readonly HashSet<string> Odrzucone=new HashSet<string>\{(.*?)\};', s, re.S).group(1)
        for name in ('pilka_nozna', 'pilka_siatkowa', 'rekawice_bokserskie', 'buty_zapasnicze_czarno_zlote', 'perfumy_bursztyn',
                     'perfumy_czerwone_zlota_szyjka', 'perfumy_zolta_kula', 'perfumy_lawendowe_motyl', 'kolczyki_kola_zlote',
                     'kolczyki_wiszace_brylant', 'ubranie_koszula_biala', 'sport_spodnie_dresowe_lampasy'):
            self.assertIn('"' + name + '"', rej, name)
        self.assertIn('if(Odrzucony(pack,name))return false;', s)
        # and no list still offers them
        perf = re.search(r'static readonly string\[\] Perfumy=\{(.*?)\};', s, re.S).group(1)
        for name in ('perfumy_bursztyn', 'perfumy_czerwone_zlota_szyjka', 'perfumy_zolta_kula', 'perfumy_lawendowe_motyl'):
            self.assertNotIn(name, perf)
        self.assertEqual(len(re.findall(r'"[^"]+"', perf)), 16)
        self.assertNotIn('kolczyki', re.search(r'JDrobne=\{(.*?)\};', s, re.S).group(1))
        self.assertNotIn('"ubranie_koszula_biala"', re.search(r'UbraniaModele=new Dictionary<int,string\[\]>\{(.*?)\}\};', s, re.S).group(1))
        self.assertNotIn('"rekawice_bokserskie"', src('BraxChampionsG1Wnetrze.cs'))

    def test_gloves_lie_on_the_shop_rack(self):
        w = src('BraxChampionsG1Wnetrze.cs')
        self.assertIn('"sklep_walka_01/rekawice_bokserskie_braxhorn_czarno_zlote",new Vector3(rx+.35f+i*.55f,y+.75f,rz+.15f),Vector3.back,90f,.118f);', w)
        self.assertIn('static bool ShopModel(string pack,string name,Vector3 local,Vector3 faceLocal,float tilt=0f,float lift=0f)', src('BraxChampionsG1Sklepy.cs'))

    def test_balls_are_sewn_from_panels(self):
        t = src('BraxKatalogTowar.cs')
        self.assertIn('static void Football(BraxChampionsMeshBatch m, Vector3 o, float r)', t)
        self.assertIn('static void Volleyball(BraxChampionsMeshBatch m, Vector3 o, float r)', t)
        self.assertIn('if (kind == 0) { Football(c.m, o, r); return; }', t)
        self.assertIn('for (int i = 0; i < 12; i++)', t)                       # 12 pentagons, a hexagon per face of the icosahedron
        self.assertIn('Cut(i, j), Cut(j, i), Cut(j, k), Cut(k, j), Cut(k, i), Cut(i, k)', t)
        s = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('BraxKatalog.Ball(Kat(),q,i%3);', s)                     # the fallback ball is the kind that was asked for
        self.assertNotIn('BraxKatalog.Ball(Kat(),q,i);', s)

    def test_watch_wrapped_on_its_cushion(self):
        s = src('BraxChampionsG1Sklepy.cs')
        self.assertIn('if(kind==0){m.RoundedBox(gVelvetBlack,p+Vector3.up*.0885f-f*.046f,new Vector3(.075f,.177f,.06f)', s)

    def test_tools_and_lettering_lists(self):
        for f in ('przeglad_modeli.js', 'buty_pojedyncze.py', 'okruchy.py', 'napisy_precz.py'):
            self.assertTrue((TOOLS / f).exists(), f)
        seen = 0
        for f in sorted((TOOLS / 'napisy').glob('*.json')):
            spec = json.loads(f.read_text(encoding='utf-8')); self.assertEqual(spec['pack'], f.stem)
            pack = 'braxhorn-' + f.stem.replace('sklep_', 'sklep-').replace('_', '-')
            files = json.loads((PACKS / pack / 'manifest.json').read_text(encoding='utf-8'))['files']
            for pr in spec['prints']:
                self.assertIn(f.stem + '/' + pr['model'] + '.glb', files, pr['model'])
                self.assertIn(pr['view'], ('przod', 'tyl', 'lewy', 'prawy', 'gora', 'spod'))
                u0, v0, u1, v1 = pr['rect']; self.assertTrue(0 <= u0 < u1 <= 1 and 0 <= v0 < v1 <= 1, pr)
                seen += 1
        self.assertGreaterEqual(seen, 30)
        # the jersey's big BRAXHORN is right and stays
        self.assertNotIn('sport_koszulka_braxhorn_czarno_zlota', (TOOLS / 'napisy' / 'sklep_sport_01.json').read_text())


if __name__ == '__main__':
    unittest.main()
