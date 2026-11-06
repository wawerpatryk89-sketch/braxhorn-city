"""Source and UV-math regressions for R1 Nutrition. These do not run Unity."""
from dataclasses import dataclass
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'unity-city/Assets/Editor/Claude/Region/BraxMiastoNutrition.cs'
TEXT = SOURCE.read_text(encoding='utf-8')
TOKEN = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|\b[\w]+\b|[^\s]')


def tokens(text):
    return [m.group() for m in TOKEN.finditer(text)
            if not m.group().startswith(('//', '/*'))]


def method(name):
    ts = tokens(TEXT)
    for i in range(len(ts) - 2):
        if ts[i] != name or ts[i + 1] != '(':
            continue
        depth = 1
        j = i + 2
        while depth:
            depth += (ts[j] == '(') - (ts[j] == ')')
            j += 1
        if ts[j] != '{':
            continue  # a call rather than a declaration
        start = j
        depth = 1
        j += 1
        while depth:
            depth += (ts[j] == '{') - (ts[j] == '}')
            j += 1
        return ts[start + 1:j - 1]
    raise AssertionError('Missing method: ' + name)


def calls(ts, name):
    result = []
    for i in range(len(ts) - 1):
        if ts[i] != name or ts[i + 1] != '(':
            continue
        stack = ['(']
        args, current = [], []
        for token in ts[i + 2:]:
            if token in ('(', '[', '{'):
                stack.append(token)
            elif token in (')', ']', '}'):
                stack.pop()
                if not stack:
                    args.append(current)
                    break
            if token == ',' and len(stack) == 1:
                args.append(current)
                current = []
            else:
                current.append(token)
        result.append(args)
    return result


@dataclass
class V2:
    x: float
    y: float
    def __add__(self, other): return V2(self.x + other.x, self.y + other.y)
    def __sub__(self, other): return V2(self.x - other.x, self.y - other.y)
    def __mul__(self, scalar): return V2(self.x * scalar, self.y * scalar)
    def __truediv__(self, scalar): return V2(self.x / scalar, self.y / scalar)


@dataclass
class YZ:
    y: float
    z: float


def source_uv_solver(a, b, c, uv):
    """Execute the arithmetic expressions present in the C# source, not a second formula."""
    scope = dict(a=a, e1=YZ(b.y-a.y, b.z-a.z), e2=YZ(c.y-a.y, c.z-a.z),
                 t1=uv[1]-uv[0], t2=uv[2]-uv[0], uvs=uv, ia=0)
    for variable in ('det', 'along', 'up'):
        match = re.search(r'\b(?:float|var)\s+' + variable + r'\s*=\s*([^;]+);', TEXT)
        if match is None:
            raise AssertionError('Missing UV arithmetic: ' + variable)
        expr = match.group(1)
        if not re.fullmatch(r'[A-Za-z0-9_.+*/() \-]+', expr):
            raise AssertionError('Unexpected UV expression')
        scope[variable] = eval(expr, {'__builtins__': {}}, scope)
    origin = uv[0] - scope['along'] * a.z - scope['up'] * a.y
    return scope['along'], scope['up'], origin


class NutritionWallFinishTests(unittest.TestCase):
    def test_delimiters_balanced(self):
        stack = []
        pairs = {')': '(', ']': '[', '}': '{'}
        for token in tokens(TEXT):
            if token in pairs.values():
                stack.append(token)
            elif token in pairs:
                self.assertTrue(stack, 'Unmatched closing delimiter')
                self.assertEqual(stack.pop(), pairs[token])
        self.assertEqual(stack, [])

    def test_one_wall_material_and_mural_opening(self):
        room, = calls(method('Wnetrze'), 'Pokoj')
        self.assertEqual(room[:3], [['posadzka'], ['sciany'], ['sufit']])
        self.assertEqual(room[3:9], [[x] for x in ('X0','X1','Z0','Z1','F','C')])
        self.assertIn('otwMural', room[9])
        self.assertIn('otwFrontOdSrodka', room[9])

    def test_original_mural_is_still_separate(self):
        image, = calls(method('Wnetrze'), 'Obraz')
        self.assertEqual(image[2], ['"Assets/CityRebuild/Brand/nutrition-mural.jpg"'])
        self.assertEqual(image[4:7], [['Vector3', '.', 'right'], ['mw'], ['mh']])
        self.assertEqual(image[3], ['new','Vector3','(','mx0',',','my0',',','Z0',')'])

    def test_white_wall_shelf_assembly_not_generated(self):
        ts = method('Wnetrze')
        self.assertEqual(calls(ts, 'Polka'), [])
        self.assertNotIn('"ściana produktów"', ts)
        # rz0/rz1/rd belonged only to the deleted full-length west shelf assembly.
        self.assertFalse({'rz0','rz1','rd','twN'}.intersection(ts))

    def test_other_fixtures_remain(self):
        ts = method('Wnetrze')
        for label in ('"lodówka"','"lada"','"blat shake baru"','"podest ekspozycyjny"'):
            self.assertEqual(sum(label in arg for call in calls(ts, 'Kol') for arg in call), 1)
        self.assertIn('"Nutrition: menu SHAKE BAR"', ts)
        self.assertIn('"Nutrition: kubeł whey na podeście"', ts)
        self.assertEqual(len(calls(ts, 'Akcja')), 2)

    def test_missing_reference_cannot_destroy_shop(self):
        ts = method('Apply')
        capture = ts.index('PlytkaZIstniejacejSciany')
        erase = ts.index('Wyczysc')
        self.assertLess(capture, erase)
        self.assertIn('return', ts[capture:erase])
        self.assertIn('!', ts[capture:erase])
        self.assertIn('Debug', method('BrakPlytki'))
        self.assertIn('LogWarning', method('BrakPlytki'))
        self.assertNotIn('throw', method('PlytkaZIstniejacejSciany'))

    def test_reuses_maps_without_recolour_or_download(self):
        ts = method('PlytkaZIstniejacejSciany')
        self.assertIn([['original']], calls(ts, 'Material'))
        self.assertEqual(calls(ts, 'SetColor'), [])
        self.assertEqual(calls(ts, 'SetTexture'), [])
        self.assertEqual(calls(ts, 'Texture2D'), [])
        for forbidden in ('UnityWebRequest','DownloadHandler','WebClient','HttpClient'):
            self.assertNotIn(forbidden, ts)

    def test_uv_scale_and_offset_are_preserved(self):
        a,b,c = YZ(.5,12), YZ(.5,14), YZ(2.5,14)
        for sx, sy, ox, oy in [(1/3,1/3,0,0),(-1/3,1/3,2,.25),(.25,.5,.1,-.2)]:
            with self.subTest(scale=(sx,sy), offset=(ox,oy)):
                uv = [V2(sx*p.z+ox,sy*p.y+oy) for p in (a,b,c)]
                along,up,origin = source_uv_solver(a,b,c,uv)
                self.assertAlmostEqual(along.x,sx)
                self.assertAlmostEqual(along.y,0)
                self.assertAlmostEqual(up.x,0)
                self.assertAlmostEqual(up.y,sy)
                for p,t in zip((a,b,c),uv):
                    recovered = along*p.z + up*p.y + origin
                    self.assertAlmostEqual(recovered.x,t.x)
                    self.assertAlmostEqual(recovered.y,t.y)

    def test_mural_and_front_openings_do_not_overlap_wall_cells(self):
        # Same rectangular cell partition used by the existing BraxMiastoA.Sciana.
        x0,x1,z0,z1,f,c = 14.7,25.8,11.8,20.2,.15,3.9
        w=5.6; h=w*941/1672; left=(x0+x1)/2-w/2
        walls=[(x1-x0,[(x1-left-w,.45,x1-left,.45+h)]),
               (z1-z0,[]),
               (x1-x0,[(19.8-x0,f,22.2-x0,f+2.85),
                        (15.25-x0,.62,19.35-x0,3.35),
                        (22.65-x0,.62,25.4-x0,3.35)]),
               (z1-z0,[])]
        for width,holes in walls:
            xs=sorted({0,width,*[v for h0 in holes for v in (h0[0],h0[2])]})
            ys=sorted({f,c,*[v for h0 in holes for v in (h0[1],h0[3])]})
            area=0
            for a,b in zip(xs,xs[1:]):
                for low,high in zip(ys,ys[1:]):
                    mid=((a+b)/2,(low+high)/2)
                    if any(l<=mid[0]<=r and lo<=mid[1]<=hi for l,lo,r,hi in holes):
                        continue
                    area+=(b-a)*(high-low)
                    for l,lo,r,hi in holes:
                        self.assertLessEqual(max(0,min(b,r)-max(a,l))*max(0,min(high,hi)-max(low,lo)),1e-8)
            self.assertAlmostEqual(area,width*(c-f)-sum((r-l)*(hi-lo) for l,lo,r,hi in holes))


if __name__ == '__main__':
    unittest.main()
