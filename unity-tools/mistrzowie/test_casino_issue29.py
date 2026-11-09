from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[2]
SOURCE=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
# 9.11 owner board T11 supersedes the Issue #29 "masonry, no windows" variant.
FACADE=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11Facade.cs").read_text(encoding="utf-8")

def method(name,next_marker):
    start=SOURCE.index("static void "+name+"()")
    end=SOURCE.index(next_marker,start)
    return SOURCE[start:end]

class CasinoIssue29Contract(unittest.TestCase):
    def test_masonry_and_rotunda_contract(self):
        # Owner board: cream blocks + tall two-storey glazing, graphite pylons
        # with gold LED, glass rotunda with chandelier lobby.
        self.assertIn("ReferenceFacades();",SOURCE)
        self.assertNotIn("NO GLASS",SOURCE+FACADE)
        self.assertNotIn("facadeSandstone",SOURCE)
        self.assertIn('"floor_tiles_04"',SOURCE)
        self.assertIn("static void GlazedBay(",FACADE)
        self.assertIn('"T11 graphite entrance pylon "',FACADE)
        self.assertIn('"T11 pylon warm gold vertical LED "',FACADE)
        self.assertIn("premium fascia 48_OF_48",SOURCE)
        self.assertNotIn("static void FrontPanel",SOURCE)
        self.assertNotIn("static void SidePanel",SOURCE)
        self.assertIn("T11_ROTUNDA_LOBBY_OWNER_BOARD",FACADE)

    def test_mobile_exterior_gold_and_lighting(self):
        self.assertIn('gold=BraxChampionsFoundation.LibraryMetal(',SOURCE)
        self.assertIn('"T11_champagne_gold_trim"',SOURCE)
        exterior=method("PremiumExteriorLighting","// A source image")
        self.assertNotIn("AddComponent<Light>",exterior)
        self.assertNotIn("LightType.Point",exterior)
        self.assertIn("B1 real warm-white parking light",SOURCE)
        self.assertIn("exteriorPointLights=0",SOURCE)
        # Only the enclosed lobby gets two cheap vertex lights.
        self.assertEqual(FACADE.count("W.Lampa("),2)

    def test_owner_sign_sources_are_pinned(self):
        for digest in (
            "78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b",
            "ebf5e0ca3fa789cd709e0e621af399c0c10f31e96bb29cb44e4e5e66aa0adada",
            "6e756f193e3c764f63960f049524ea265017f95b58d95991885951ed1fbe9247",
            "43ea47f02a1f2e9e4f707bb75d16ec0a3fe79ec02762929ca591e317defa1088",
        ):
            self.assertIn(digest,SOURCE)
        self.assertIn("SignCarrierOuter",SOURCE)
        self.assertIn("PolygonRing",SOURCE)
        self.assertNotIn("Casino WAWER front deep graphite casing",SOURCE)
        self.assertIn("UNITY_SCREENSHOTS_ATTACHED=PENDING_REAL_UNITY_RENDER",SOURCE)

    def test_protected_geometry_is_retained(self):
        self.assertIn("24f,.11f,20f",SOURCE)
        # Owner 10.11 00:33: "A gdzie jest ten dolar złoty ... Czemu go nie ma teraz?" - back on the roof.
        self.assertIn("if(RoofDollarOnOwnerBoard)RoofDollar();",SOURCE)
        self.assertIn("const bool RoofDollarOnOwnerBoard = true;",FACADE)
        self.assertIn('Door("T11_CASINO_WAWER_PUBLIC","Public glass main entry",0,-36.28f,180f,5.6f)',SOURCE)
        self.assertIn("6 m delivery opening at x -23..-17",FACADE)
        self.assertIn("tower=LATER",SOURCE)

if __name__=="__main__":
    unittest.main()
