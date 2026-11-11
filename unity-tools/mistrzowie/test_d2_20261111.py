"""D2 Dworzec kolejowo-autobusowy (owner 11.11 12:56 board, 13:08-13:09 railway out of the city): the C# generator and the
shared layout must agree, the railway must leave the region over road X70 without touching the street, and the CI must
restore the owner's bus. No Unity needed."""
import json
import re
import subprocess
import unittest
from pathlib import Path

import layout as L
from prepare_assets import POLYHAVEN_SLUGS

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / 'unity-city/Assets/Editor/Mistrzowie'
FILES = ['BraxChampionsD2.cs', 'BraxChampionsD2Budynek.cs', 'BraxChampionsD2Kolej.cs', 'BraxChampionsD2Autobusy.cs']


def consts():
    """public const float A=1f,B=-2.5f,...; (and expressions of earlier names) from all D2 files."""
    src = '\n'.join((ED / f).read_text() for f in FILES)
    out = {}
    for decl in re.findall(r'public const float ([^;]+);', src):
        for part in decl.split(','):
            name, _, expr = part.partition('=')
            expr = re.sub(r'(\d)f\b', r'\1', expr.strip())
            out[name.strip()] = eval(expr, {}, dict(out))
    return out, src


class D2LayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.c, cls.src = consts()

    def test_centre_and_site_match_layout(self):
        c = self.c
        self.assertEqual(L.D2_CENTRE, (c['CentreX'], c['CentreZ']))
        self.assertEqual(L.D2_SITE, (c['CentreX'] + c['SiteX0'], c['CentreZ'] + c['SiteZ0'], c['SiteX1'] - c['SiteX0'], c['SiteZ1'] - c['SiteZ0']))
        # inside the block X70..X400 / Z70..Z330 (sidewalks end at 87 / 383 / 313)
        x0, z0, w, d = L.D2_SITE
        self.assertGreaterEqual(x0, 87); self.assertLessEqual(x0 + w, 383); self.assertEqual(z0, 87); self.assertLessEqual(z0 + d, 313)

    def test_street_mouths_match_layout(self):
        m = re.search(r'MouthList=\{(.+?)\};', self.src, re.S).group(1)
        pairs = re.findall(r'\("(\w+)",([^,]+),([^)]+)\)', m)
        local = [(i, eval(re.sub(r'(\d)f\b', r'\1', a), {}, self.c), eval(re.sub(r'(\d)f\b', r'\1', b), {}, self.c)) for i, a, b in pairs]
        world = [(i, a + self.c['CentreX'], b + self.c['CentreX']) for i, a, b in local]
        self.assertEqual(world, L.D2_MOUTHS_X)
        # the arcs (R 3) keep clear of the zebras at the Z70 corners (x 92 and 378, 5 m wide) and of each other
        spans = sorted((a - 3, b + 3) for _, a, b in world)
        for a, b in spans:
            self.assertTrue(b < 92 - 2.5 - 1 or a > 92 + 2.5 + 1)
            self.assertTrue(b < 378 - 2.5 - 1 or a > 378 + 2.5 + 1)
        for (a0, b0), (a1, b1) in zip(spans, spans[1:]):
            self.assertLess(b0, a1)
        for _, a, b in world:
            self.assertGreaterEqual(b - a, 7.0)

    def test_railway_constants_match_layout(self):
        c = self.c
        self.assertEqual(L.D2_RAIL_TOP, c['RailTop'])
        self.assertEqual(L.D2_LINE_Z, c['CentreZ'] + c['LineZ'])
        self.assertEqual(L.D2_PORTAL_X, c['PortalWX'])
        self.assertEqual(L.D2_BRIDGE_X, (c['CentreX'] + c['BridgeX0'], c['CentreX'] + c['BridgeX1']))
        self.assertEqual(c['CentreX'] + c['RegionEdgeX'], 0.0)
        self.assertAlmostEqual((c['Track1Z'] + c['Track2Z']) / 2, c['LineZ'])
        self.assertAlmostEqual(c['Track2Z'] - c['Track1Z'], 4.5)

    def test_bridge_spans_road_X70_with_clearance(self):
        c = self.c
        a, b = L.D2_BRIDGE_X
        self.assertLess(a, 70 - 17)       # west sidewalk edge (53)
        self.assertGreater(b, 70 + 17)    # east sidewalk edge (87)
        self.assertGreaterEqual(c['DeckSoffit'] - 0.0, 4.7 + .5)   # road at y 0, skrajnia 4.7 m + reserve
        self.assertAlmostEqual(c['PlatformTop'] - c['RailTop'], .76)

    def test_line_clear_of_street_trees_and_stops(self):
        f = ROOT / 'unity-tools/mistrzowie/legacy_foundation.py'
        self.assertTrue(f.exists())
        z0, z1 = L.D2_LINE_Z - self.c['LineDeckHalf'], L.D2_LINE_Z + self.c['LineDeckHalf']
        for x, z in [(49, 170), (49, 200), (91, 170), (91, 200)]:      # street trees along X70 (legacy_foundation plants)
            self.assertGreaterEqual(min(abs(z - z0), abs(z - z1)), 6.0)
        for sid, x, z, axis in L.BUS_STOPS:
            if x == 70: self.assertGreater(abs(z - L.D2_LINE_Z), 30)

    def test_platforms_are_260_m_and_inside_the_site(self):
        c = self.c
        self.assertEqual(c['PlatX1'] - c['PlatX0'], 260)
        self.assertGreaterEqual(c['CentreX'] + c['PlatX0'], 95); self.assertLessEqual(c['CentreX'] + c['DeckEndX'], 375)
        self.assertLessEqual(c['CentreZ'] + c['DeckZ1'] + .6, c['CentreZ'] + c['SiteZ1'])

    def test_materials_board_and_no_gold(self):
        self.assertIn('blue_metal_plate', POLYHAVEN_SLUGS)
        fx = json.loads((ROOT / 'unity-tools/mistrzowie/pbr-provider-preflight.json').read_text())
        self.assertIn('blue_metal_plate', fx)
        for slug in ['granite_tile_03', 'clean_asphalt', 'concrete_pavement_02', 'teak_veneer']:
            self.assertIn(slug, POLYHAVEN_SLUGS)
        self.assertIn('LibraryTravertine', self.src)
        self.assertNotRegex(self.src.lower(), r'\bgold|złot')

    def test_wired_into_the_build_and_runtime(self):
        b = (ED / 'BraxChampionsBuildings.cs').read_text()
        self.assertIn('BraxChampionsD2.Generate(Out)', b)
        for f in FILES:
            self.assertTrue((ED / (f + '.meta')).exists(), f)
        for f in ['BraxChampionsD2Pociag.cs', 'BraxChampionsZegar.cs']:
            p = ROOT / 'unity-city/Assets/Mistrzowie' / f
            self.assertTrue(p.exists() and Path(str(p) + '.meta').exists(), f)
        h = (ED / 'BraxChampionsHorizon.cs').read_text()
        self.assertIn('RailHill(k)', h)
        self.assertIn('FarHeight', h)

    def test_ci_restores_the_owner_bus(self):
        r = (ROOT / 'unity-tools/mistrzowie/restore_private.py').read_text()
        self.assertIn("odszyfruj('miasto_pojazdy')", r)
        self.assertIn("D2_MODELS=['autobus']", r)
        w = (ROOT / '.github/workflows/mistrzowie-apk.yml').read_text()
        self.assertIn('unity-city/Prywatne/miasto_pojazdy.tar.gz.enc', w)
        self.assertTrue((ROOT / 'unity-city/Prywatne/miasto_pojazdy.tar.gz.enc').exists())
        self.assertTrue((ROOT / 'unity-city/Assets/Source/Mistrzowie/D2/CREDITS.md').exists())

    def test_no_generated_foundation_committed(self):
        out = subprocess.run(['git', 'ls-files', 'unity-city/Assets/Source/Mistrzowie/Foundation.json'], cwd=ROOT, capture_output=True, text=True).stdout
        self.assertEqual(out.strip(), '')


if __name__ == '__main__':
    unittest.main()
