"""Owner 16.11.2026 21:52-22:38: the heliport of the BRAXHORN TOWER ("lotnisko dla heli"), the way down ("jest zejście? ...
jakaś winda musiała być") and the helicopter ("Przemaluj go na nasz kolor, z napisem naszym, z naszym herbem. I go postaw na
dachu i zrób wejście. I żeby był funkcjonalny, żeby można było latać"; 22:38 "słabo widoczny ... napis").

The deck as built (ICAO Annex 14 vol. II): markings cut into its top, never laid over it (ZASADA migotanie 16.11); the
express lift's third stop in a glass pavilion under the deck; two stairs; the net, the windsock, the balustrade. The EC135
(GRIP420, CC-BY 4.0) repainted, encrypted (public repository), flown by BraxChampionsHelikopter / BraxChampionsLot (the
flight checked by automaty_sim) through the player's vehicle hook (a BraxChampionsCar in proxy mode)."""
import hashlib
import json
import pathlib
import re
import subprocess
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
TOWER = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")
HELI = (RT / "BraxChampionsHelikopter.cs").read_text(encoding="utf-8")
LOT = (RT / "BraxChampionsLot.cs").read_text(encoding="utf-8")
CAR = (RT / "BraxChampionsCar.cs").read_text(encoding="utf-8")
PACK = ROOT / "unity-city/Prywatne/braxhorn-mistrzowie-helikopter-20261116"


def section(src, start, end):
    return src[src.index(start):src.index(end)]


class Heliport(unittest.TestCase):
    PORT = section(TOWER, "    static void Heliport(", "    // ---------------- T09 SKY KASYNO")

    def test_deck_over_the_crown_with_room_under_it(self):
        self.assertIn("const float HeliHalf=10.8f,HeliGap=3.0f,HeliSlab=.40f,PavFloor=.03f,NetWidth=1.5f;", TOWER)
        self.assertIn("Heliport(roof+14f,F2);", TOWER)
        self.assertIn("W.KolizjaMM(-h,h,y0,y1,-h,h);", self.PORT)
        # FATO >= 1.5 D of the EC135 (D 12.16 m)
        self.assertGreaterEqual(2 * 10.8, 1.5 * 12.16)

    def test_markings_cut_never_laid_over(self):
        top = section(TOWER, "    static void HeliDeckTop(", "    static void HeliNet(")
        self.assertIn("const float r0=3.05f,r1=3.55f,band=.30f,hx=1.5f,hz=.9f,bar=.40f,cross=.20f;", top)
        # TDPM: inner diameter 0.5 D, 0.5 m wide; the H 3 x 1.8 m in 0.4 m strokes; the TLOF perimeter 0.3 m
        self.assertAlmostEqual(2 * 3.05, .5 * 12.16, delta=.05)
        self.assertAlmostEqual(3.55 - 3.05, .5)
        for m in ("Q(deck,", "Q(heliYellow,", "Q(paintWhite,"):
            self.assertIn(m, top)
        self.assertNotIn("Nakladka", self.PORT + top)
        # every piece in the deck's plane yt: no plate over it
        self.assertEqual(set(re.findall(r"new Vector3\([^;]*?,(y\w*),", top)), {"yt"})
        self.assertNotIn("yt+", top)

    def test_lift_has_a_third_stop_on_the_roof(self):
        self.assertIn("heliY=Shaft+t.floors*Storey+14f+PavFloor;\n        CasinoMaterials();ExpressLift(", TOWER)
        self.assertIn("static float[] Stops()=>float.IsNaN(heliY)?new[]{Fl,CasinoY}:new[]{Fl,CasinoY,heliY};", TOWER)
        self.assertIn("var ys=Stops();landL=new Transform[ys.Length];landR=new Transform[ys.Length];", TOWER)
        self.assertIn("w.wezwanie[i]=root.TransformPoint(new Vector3(liftX,ys[i]+1.2f,LiftZ0-.9f));", TOWER)
        self.assertIn("heliY=float.NaN;helis=0;", TOWER)                       # only the BRAXHORN TOWER has it

    def test_pavilion_and_two_stairs(self):
        pav = section(TOWER, "    static void HeliPavilion(", "    static void HeliStair(")
        self.assertIn("LiftCore(new Core(liftX-1.35f,LiftZ0,2.7f,2.6f),liftX,yf,top);", pav)
        self.assertIn('"LĄDOWISKO"', pav)
        self.assertIn("W.Kolizja(", pav)
        stair = section(TOWER, "    static void HeliStair(", "    // the BRAXHORN helicopter (owner 16.11 22:00)")
        self.assertIn("const int N=20;const float G=.28f;", stair)
        self.assertIn("W.Rampa(new Vector3(xt,yt,zc),new Vector3(xb,y,zc),width);", stair)
        # 20 risers over the 3.4 m from the slab to the deck's top: 17 cm, a comfortable stair (2R + G = 62 cm)
        rise = (3.0 + .40) / 20
        self.assertAlmostEqual(rise, .17)
        self.assertTrue(.60 <= 2 * rise + .28 <= .65)
        self.assertIn("HeliStair(StairTop,1f,-StairIn,-StairOut,-h,y,y1);", self.PORT)
        self.assertIn("HeliStair(-StairTop,-1f,StairIn,StairOut,h,y,y1);", self.PORT)

    def test_net_windsock_balustrade_lights(self):
        net = section(TOWER, "    static void HeliNet(", "    static Material HeliNetMaterial(")
        self.assertIn("spans.Add((-h,s0,side));spans.Add((s1,h,side));", net)   # open over the stairs
        mat = section(TOWER, "    static Material HeliNetMaterial(", "    static void Windsock(")
        self.assertIn("imp.mipmapEnabled=true;", mat)                              # from afar a band, no shimmering cords
        self.assertIn('m.SetFloat("_Transparent",1f);', mat)
        sock = section(TOWER, "    static void Windsock(", "    static void SlabBalustrade(")
        self.assertIn("for(int b=0;b<5;b++)", sock)
        self.assertIn("var m=b%2==0?sockOrange:paintWhite;", sock)                 # orange first and last
        self.assertIn("Mathf.Lerp(.30f,.15f,t0)", sock)
        self.assertIn("mouth+axis*2.4f*t0", sock)
        bal = section(TOWER, "    static void SlabBalustrade(", "    static void HeliPavilion(")
        self.assertIn("W.Kolizja(new Vector3(mid.x,y+.55f,mid.y),new Vector3(.12f,1.1f,L),q);", bal)
        self.assertIn("W.MM(ledGreen,", self.PORT)
        self.assertIn("if(Mathf.Abs(pz)>h&&PrzyLadowaniuSchodow(px,pz))continue;", self.PORT)


class Helicopter(unittest.TestCase):
    GEN = section(TOWER, "    static void Helikopter(", "    // ---------------- T09 SKY KASYNO")

    def test_placed_on_the_ring_with_its_hook(self):
        self.assertIn('public const string HeliModel="Assets/Source/Mistrzowie/Helikopter/braxhorn_ec135.glb";', TOWER)
        self.assertIn("Helikopter(y1);", Heliport.PORT)
        self.assertIn("heli.localPosition=new Vector3(0f,yt,0f);heli.localRotation=Quaternion.Euler(0f,90f,0f);", self.GEN)
        self.assertIn("var car=kam.gameObject.AddComponent<BraxChampionsCar>();", self.GEN)
        self.assertIn("car.helikopter=h;", self.GEN)
        self.assertIn('var kol=Punkt("kolizje kadłuba",kam,-kam.localPosition);', self.GEN)   # the camera never hits it
        self.assertIn('BraxMiastoAssety.Znajdz(model.transform,"wirnik_glowny")', self.GEN)
        self.assertIn("BraxMiastoAssety.Przygotuj(model,false,true);", self.GEN)              # it moves: not static
        self.assertIn("ImportAssetOptions.ImportRecursive", self.GEN)                         # textures before the model
        imp = (ED / "BraxChampionsHelikopterTextureImport.cs").read_text(encoding="utf-8")
        self.assertIn('Folder = "Assets/Source/Mistrzowie/Helikopter/_tekstury/"', imp)
        self.assertIn('const string HeliTextures="Assets/Source/Mistrzowie/Helikopter/_tekstury/braxhorn_ec135";', TOWER)

    def test_only_one_car_still_and_the_helicopter_stays_in_the_main_scene(self):
        # 25.11 (#60): only a second copy of the owner car is forbidden - not the helicopter, not the bunker's classics
        self.assertIn(".Any(c => !c.helikopter && c.name == RootName))", (ED / "BraxChampionsCarBuilder.cs").read_text(encoding="utf-8"))
        self.assertIn(".Where(c=>!c.helikopter).ToArray();", (ED / "BraxChampionsBuild.cs").read_text(encoding="utf-8"))
        podzial = (ED / "BraxChampionsPodzial.cs").read_text(encoding="utf-8")
        self.assertLess(podzial.index("typeof(BraxChampionsHelikopter)"), podzial.index("typeof(BraxChampionsCar)"))

    def test_the_hook_in_proxy_mode(self):
        self.assertIn("public BraxChampionsHelikopter helikopter;", CAR)
        self.assertIn("if (Proxy) { helikopter.Steer(input); return; }", CAR)
        self.assertIn("if (Proxy) return helikopter.Kontekst(position);", CAR)
        self.assertIn("if (Proxy || !driver || transition || !physicsBody || !bodyCollider) return;", CAR)
        self.assertIn("if (Proxy) { helikopter.EmergencyExit(); return; }", CAR)

    def test_flies_as_an_ec135(self):
        self.assertIn("MaxSpeed = 65f", LOT)                  # 234 km/h, the EC135's cruise
        self.assertIn("Climb = 7.6f", LOT)                    # its rate of climb
        self.assertIn("var d = lot.Step(stick, col, g, dt);", HELI)
        self.assertIn("W GÓRĘ / W DÓŁ", HELI)                # the collective's two buttons
        self.assertIn("p.championsCar = kamera;", HELI)
        self.assertNotIn("AudioSource", HELI)                 # no made-up turbine sound: a real recording only
        sim = (TOOLS / "automaty_sim/automaty_sim.csproj").read_text(encoding="utf-8")
        self.assertIn("BraxChampionsLot.cs", sim)
        prog = (TOOLS / "automaty_sim/Program.cs").read_text(encoding="utf-8")
        for c in ("heli: climbs 7.6 m/s", "heli: cruise 234 km/h", "heli: settles on the ground under it",
                  "heli: no lift-off before the rotor is up", "heli: the turn carries the speed round (no skid)"):
            self.assertIn(c, prog)


class Pack(unittest.TestCase):
    def test_encrypted_with_his_original_and_the_result(self):
        man = json.loads((PACK / "manifest.json").read_text(encoding="utf-8"))
        files = sorted(man["files"])
        self.assertIn("Helikopter/braxhorn_ec135.glb", files)
        self.assertIn("zrodlo/ec_135.glb", files)
        self.assertEqual(len([f for f in files if f.startswith("Helikopter/_tekstury/braxhorn_ec135/")]), 18)
        self.assertEqual(man["recipientCertificateSha256"], "4a1bdb0377eaa7dd294f62e13bd6e12efd0bfa1a0249ad604ca9f2632b560fd5")
        self.assertIn("CC-BY 4.0", man["license"])
        self.assertIn("GRIP420", man["license"])
        for part in man["parts"]:
            f = PACK / part["name"]
            self.assertLessEqual(f.stat().st_size, 90 * 1024 * 1024)
            self.assertEqual(hashlib.sha256(f.read_bytes()).hexdigest(), part["sha256"])
        inst = (TOOLS / "install_owner_media.py").read_text(encoding="utf-8")
        self.assertIn('HELIKOPTER = "braxhorn-mistrzowie-helikopter-20261116"', inst)
        self.assertIn('for f in sorted((heli / "Helikopter").rglob("*")):', inst)      # zrodlo/ stays in the pack
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/Helikopter"], cwd=ROOT,
                                 capture_output=True, text=True).stdout
        self.assertEqual(tracked.strip(), "")

    def test_tools_and_credit(self):
        d = TOOLS / "helikopter"
        for f in ("glb.py", "bake.py", "livery.py", "export.py", "heli_inspect.py", "README.md"):
            self.assertTrue((d / f).exists(), f)
        readme = (d / "README.md").read_text(encoding="utf-8")
        self.assertIn("GRIP420", readme)
        self.assertIn("CC-BY 4.0", readme)
        liv = (d / "livery.py").read_text(encoding="utf-8")
        self.assertIn("tw = 2.60", liv)                       # 22:38: the letters 30 cm over 2.6 m
        self.assertIn("ROTOR_D = 10.20", (d / "bake.py").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
