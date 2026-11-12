"""APK 39 (12.11): G1 GALERIA was missing from the game - BraxKatalog asked SourceMaterial for "Leather037" (an ambientCG
library name, not a Poly Haven slug), the build found no Leather037_diff_4k.jpg and threw the whole gallery out. Every
literal slug that reaches BraxChampionsFoundation.SourceMaterial (directly or through a region helper: T, Tex, M, P, Fabric,
Premium, ...) must be a slug prepare_assets.py fetches (POLYHAVEN_SLUGS), or one missing map removes a whole building."""
import ast
import pathlib
import re
import unittest

HERE = pathlib.Path(__file__).resolve().parent
ED = HERE.parents[1] / 'unity-city' / 'Assets' / 'Editor' / 'Mistrzowie'


def fetched_slugs():
    t = ast.parse((HERE / 'prepare_assets.py').read_text(encoding='utf-8'))
    for n in ast.walk(t):
        if isinstance(n, ast.Assign) and any(isinstance(x, ast.Name) and x.id == 'POLYHAVEN_SLUGS' for x in n.targets):
            return set(ast.literal_eval(n.value))
    raise AssertionError('POLYHAVEN_SLUGS not found')


class TeksturyRejonu(unittest.TestCase):
    def test_every_source_material_slug_is_fetched(self):
        slugs = fetched_slugs()
        files = {f.name: f.read_text(encoding='utf-8') for f in sorted(ED.glob('*.cs'))}
        wrappers = {'SourceMaterial'}
        for _ in range(3):
            for s in files.values():
                for m in re.finditer(r'static\s+Material\s+(\w+)\s*\(\s*string\s+\w+\s*,\s*string\s+(\w+)[^)]*\)\s*(?:=>|\{)(.{0,400})', s, re.S):
                    if any(re.search(r'\b' + w + r'\([^,]+,\s*' + m.group(2) + r'\b', m.group(3)) for w in wrappers):
                        wrappers.add(m.group(1))
        self.assertIn('T', wrappers)
        bad = sorted({(f, w, c.group(1)) for f, s in files.items() for w in wrappers
                      for c in re.finditer(r'\b' + w + r'\(\s*"[^"]*"\s*,\s*"([^"]+)"', s) if c.group(1) not in slugs})
        self.assertEqual(bad, [])

    def test_catalogue_leather_is_poly_haven(self):
        s = (ED / 'BraxKatalogTowar.cs').read_text(encoding='utf-8')
        self.assertNotIn('"Leather037"', s)
        self.assertIn('T("leather_black", "leather_white"', s)


if __name__ == '__main__':
    unittest.main()
