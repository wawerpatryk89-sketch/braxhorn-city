"""E1 premium pass (owner 10.11 22:30-22:47): live mirrors, the owner's own bar models, upholstery, the toilets rebuilt,
linear light slots, seats. Source checks only (no Unity)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def code(path):
    return re.sub(r"//.*", "", path.read_text(encoding="utf-8"))


class E1Premium(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pre = code(ED / "BraxChampionsE1Premium.cs")
        cls.e1 = code(ED / "BraxChampionsE1.cs")
        cls.fin = code(ED / "BraxChampionsE1Finish.cs")
        cls.all = cls.e1 + cls.fin + cls.pre

    def test_meta(self):
        self.assertTrue((ED / "BraxChampionsE1Premium.cs.meta").exists())

    def test_mirrors_are_live(self):
        # "lustro to lustro": no flat mirror plate anywhere in E1, every mirror the region's planar reflection
        self.assertNotIn("B(mirror", self.all)
        self.assertIn("BraxChampionsMirror.Wall(", self.pre)
        for m in ('LiveMirror("bar mirror wall"', 'LiveMirror("WC "+tag+" vanity"'):
            self.assertIn(m, self.pre)
        self.assertIn('LiveMirror("cloakroom "', self.fin)

    def test_owner_library_in_the_bar(self):
        for f in ("bar_polki_butelki_windupbird", "bar_butelki_helyeouka", "bar_hoker_skora_nosteam", "bar_lampa_markpeters",
                  "bar_szklo_saandy"):
            self.assertIn('"' + f + '"', self.pre)
        self.assertIn('const string OwnerPack="osiedle_wnetrza";', self.pre)
        restore = (ROOT / "unity-tools/mistrzowie/restore_private.py").read_text()
        self.assertIn("p.odszyfruj('osiedle_wnetrza')", restore)
        wf = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text()
        self.assertIn("unity-city/Prywatne/osiedle_wnetrza.tar.gz.enc", wf)
        self.assertTrue((ROOT / "unity-city/Prywatne/osiedle_wnetrza.tar.gz.enc").exists())
        credits = (ROOT / "unity-city/Assets/Source/Mistrzowie/E1/CREDITS.md").read_text()
        for a in ("windupbird", "Helyeouka", "NosTeam", "Mark Peters", "Saandy", "CC-BY-4.0"):
            self.assertIn(a, credits)

    def test_upholstery_and_no_brown(self):
        self.assertIn('LibraryAcg("e1_quilted_leather_oxblood","Leather013"', self.pre)
        self.assertIn('LibraryLeather("e1_leather_black_smooth"', self.pre)
        self.assertNotIn("Sofa(velvet", self.e1)
        self.assertIn("Sofa(leatherBlack,", self.e1)
        self.assertIn("sofa_02/sofa_02_2k.gltf", self.pre)
        self.assertNotIn("Ottoman_01", self.pre)          # Poly Haven's ottoman is brown leather

    def test_toilets_rebuilt(self):
        t = self.pre[self.pre.index("static void Toilets()"):self.pre.index("static void Slots(")]
        for s in ("marbleWallBlack", "marbleWallWhite", "hallFloor", "stoneBasin", "Door(\"E1_WC_\"", ",false,lacquer);",
                  "yc=FFL+2.85f"):
            self.assertIn(s, t)
        self.assertNotIn("whiteTile", t)
        self.assertIn("Toilets();", self.e1)
        self.assertNotIn("static void ToiletFittings(", self.fin)

    def test_ceilings_linear_slots(self):
        self.assertIn("Slots(-HW+.3f,-.15f,-HD+.3f,-14f,Ceiling,2.4f,true);", self.fin)
        self.assertIn("Slots(10.15f,HW-.3f,-HD+.3f,14.9f,Ceiling,2.4f,true,clearOfTables);", self.fin)
        self.assertNotIn("Downlight(LaneX(l)", self.fin)
        self.assertIn('w.SetColor("_GroundColor",new Color(.96f,.96f,.97f))', self.fin)   # white stays white

    def test_owner_23_20_floors_and_stools(self):
        # "podłoga ... całkowicie do zmiany": the board's "Drewno (strefy wewn.)" on the billiards / bar / lounge floor,
        # stone tiles in the cloakrooms; no backrests on the bar stools ("coś takiego nie istnieje")
        self.assertIn('woodFloor=Inside(Tex("interior_oak_planks","oak_wood_planks"', self.pre)
        self.assertIn("B(woodFloor,x0,x1,SlabTop,FFL,z0,14.9f);", self.e1)
        self.assertNotIn("B(carpetGraphite,x0,x1,SlabTop,FFL,-15.1f,14.9f);", self.e1)
        self.assertIn("B(cloakFloor,-30.1f,-22.6f,SlabTop,FFL,z0,z1);", self.e1)
        stool = self.pre[self.pre.index("static void Stool("):self.pre.index("static void Bar()")]
        self.assertNotIn("Arc(quiltRed", stool)

    def test_everything_sits(self):
        self.assertIn("AddComponent<BraxSeat>()", self.pre)
        for s in ('"HOKER"', '"SOFA"', '"PUFA"'):
            self.assertIn(s, self.pre)
        self.assertIn('"KANAPA"', self.e1)

    def test_smooth_lathe(self):
        mb = code(ED / "BraxChampionsMeshBatch.cs")
        self.assertIn("public void Lathe(Material m, Vector3 c, Vector2[] prof, int sides, Quaternion r", mb)
        self.assertIn("static void Bottle(", self.pre)
        self.assertIn("static void Stool(", self.pre)


if __name__ == "__main__":
    unittest.main()
