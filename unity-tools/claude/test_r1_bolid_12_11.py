"""MIASTO R1 (12.11.2026): naprawy po bolidzie 43141049997 (padł po 3 h 11 min, exit 137).

1. BraxMiastoMotory.Lod: „GetComponent<LODGroup>() ?? AddComponent” — w edytorze brak komponentu to fałszywy null Unity, „??”
   go przepuszcza → MissingComponentException w SetLODs. Padły dwa kroki: „pierzeje ulicy od nowa” (rower w warsztacie)
   i „ruch aut miasta (B)” (motocykl na parkingu).
2. LONDON: CreateAsset do Out/Meshes, zanim folder powstał (PersistMeshes na końcu) — „Parent directory must exist”.
3. Pamięć: 15 GB RAM + 14 GB swap nie wystarczyło (pełny import Library po zmianie manifestu + generowanie) — plik wymiany
   28 GB i pomiar pamięci co minutę z raportem szczytu po kompilacji.
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city/Assets"
WF = (ROOT / ".github/workflows/claude-region-build.yml").read_text(encoding="utf-8")


class FalszywyNull(unittest.TestCase):
    def test_bez_getcomponent_z_operatorem_null(self):
        zle = []
        wzor = re.compile(r"GetComponent(InChildren|InParent)?<\w+>\(\)\s*(\?\?|\?\.)")
        for cs in ASSETS.rglob("*.cs"):
            for i, l in enumerate(cs.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
                if wzor.search(l) and not l.lstrip().startswith("//"):
                    zle.append(cs.relative_to(ROOT).as_posix() + ":" + str(i))
        self.assertEqual(zle, [], "GetComponent ?? / ?. — fałszywy null Unity w edytorze")

    def test_lod_jednosladow(self):
        t = (ASSETS / "Editor/Claude/Region/BraxMiastoMotory.cs").read_text(encoding="utf-8")
        self.assertIn("var lod = o.GetComponent<LODGroup>(); if (!lod) lod = o.AddComponent<LODGroup>();", t)


class London(unittest.TestCase):
    def test_folder_siatek_przed_zapisem(self):
        t = (ASSETS / "Editor/Claude/Region/BraxRegionMiastoBLondon.cs").read_text(encoding="utf-8")
        i = t.index('Utworz(mesh, BraxRegion.Out + "/Meshes/london_sciana_kurtynowa.asset")')
        self.assertIn('Directory.CreateDirectory(BraxRegion.Out + "/Meshes")', t[i - 300:i])


class PamiecBuildu(unittest.TestCase):
    def test_wieksza_wymiana_i_pomiar(self):
        self.assertIn("sudo fallocate -l 28G /swapfile-unity", WF)
        self.assertIn('nohup "$RUNNER_TEMP/pamiec.sh"', WF)
        self.assertIn("BRAXHORN_PAMIEC_RAM szczyt", WF)

    def test_raport_pamieci_po_kompilacji_takze_przy_awarii(self):
        i = WF.index("id: unity_android"); j = WF.index("Pamiec RAM i swap w czasie buildu")
        self.assertLess(i, j)
        self.assertIn("if: always()", WF[j:j + 200])


if __name__ == "__main__":
    unittest.main()
