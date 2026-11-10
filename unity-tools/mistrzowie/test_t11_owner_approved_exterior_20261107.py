"""Issue #29 T11 casino exterior acceptance contracts.

Static/source regression only. It does NOT claim Unity visual acceptance, phone
acceptance or APK validation. The contract intentionally checks only T11 files.
"""
from pathlib import Path
import hashlib
import math
import re
import unittest

ROOT=Path(__file__).resolve().parents[2]
CASINO_PATH=ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11.cs"
CASINO=CASINO_PATH.read_text(encoding="utf-8")
SOURCE=(ROOT/"unity-tools/mistrzowie/prepare_casino_textures_2k.py").read_text(encoding="utf-8")
PBR=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoPbr2K.cs").read_text(encoding="utf-8")
# 9.11 owner board T11 replaces the Issue #29 "masonry, no windows, gold pillars" facade.
FACADE=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsCasinoT11Facade.cs").read_text(encoding="utf-8")

DOLLAR="Assets/Source/Mistrzowie/CasinoT11/OwnerDollar/dezyne_3d-dollar-sign-140.glb"
DOLLAR_SHA="49e4fa9149aa69637db6231d2e98e611b3a41806b13a185daf7ad18e1a078640"
SIGN="Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png"
SIGN_SHA="78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b"


def method_source(name):
    start=CASINO.index(f"    static void {name}(")
    match=re.search(r"\n    static (?:void|string|GameObject|Material|Texture2D|Bounds|Transform) ",CASINO[start+1:])
    end=len(CASINO) if not match else start+1+match.start()
    return CASINO[start:end]


def parse_vector2_array(name):
    match=re.search(
        rf"static readonly Vector2\[\] {re.escape(name)}=\{{(?P<body>.*?)\n    \}};",
        CASINO,re.S)
    if not match:
        raise AssertionError(f"Missing {name}")
    return [
        (float(a.rstrip("f")),float(b.rstrip("f")))
        for a,b in re.findall(
            r"new Vector2\((-?\d+(?:\.\d+)?f?),(-?\.?\d+(?:\.\d+)?f?)\)",
            match.group("body"))
    ]


def point_segment_distance(p,a,b):
    px,py=p; ax,ay=a; bx,by=b
    dx=bx-ax; dy=by-ay
    denom=dx*dx+dy*dy
    if denom==0:
        return math.hypot(px-ax,py-ay)
    t=max(0.0,min(1.0,((px-ax)*dx+(py-ay)*dy)/denom))
    qx=ax+t*dx; qy=ay+t*dy
    return math.hypot(px-qx,py-qy)


def boundary_distance(point,loop):
    return min(
        point_segment_distance(point,loop[i],loop[(i+1)%len(loop)])
        for i in range(len(loop))
    )


def polygon_area(loop):
    return .5*sum(
        loop[i][0]*loop[(i+1)%len(loop)][1]
        -loop[(i+1)%len(loop)][0]*loop[i][1]
        for i in range(len(loop))
    )


class CasinoIssue29Acceptance(unittest.TestCase):
    def test_scope_remains_exterior_only_without_tower(self):
        # Board: tower 120 m is "docelowo" (later, separate building); lobby only.
        self.assertIn("No casino games, tables or office interiors yet.",CASINO)
        self.assertNotIn("static void Tower()",CASINO)
        self.assertNotIn("Office tower north/south curtain glass",CASINO)
        self.assertNotIn("SlotMachine",CASINO+FACADE)
        self.assertNotIn("AddCasinoTables",CASINO+FACADE)
        self.assertIn("tower=LATER",CASINO)
    def test_issue29_only_rotunda_is_glazed(self):
        # Owner board: two-storey glazing in every wing bay + glass rotunda.
        self.assertIn("for (int i = 0; i < 3; i++)",FACADE)   # 3 bays per front wing
        self.assertIn("for (int j = 0; j < 8; j++)",FACADE)   # 8 bays per side
        self.assertIn('" tall window glass lower"',FACADE)
        # 10.11 00:30: no board behind the glass; the windows show the real, open casino hall.
        self.assertNotIn('" interior warm backdrop"',FACADE)
        self.assertIn('Optional("kasyno T11: otwarta sala kasyna",CasinoHall);',CASINO)
        self.assertNotIn("NO GLASS",CASINO+FACADE)
        curved=method_source("CurvedFoyer")
        self.assertIn("T11 curved black glass | rotunda only",curved)          # 12:50 "zakaz szarego - albo czarne"
        self.assertIn("const int segments=48;",curved)
    def test_sandstone_05_is_real_metre_tiled_on_full_walls(self):
        # Cream blocks from the board = Poly Haven floor_tiles_04 (native 2K),
        # metre-tiled by B(); sandstone stays downloaded but unused.
        self.assertIn('"floor_tiles_04"',PBR)
        self.assertIn('"floor_tiles_04"',SOURCE)
        self.assertIn('"t11_cream_limestone_blocks","floor_tiles_04",4.0f',CASINO)
        self.assertIn("mat==cream || mat==graphite",CASINO)
        self.assertNotIn("facadeSandstone",CASINO)
    def test_front_side_and_rear_gold_structure_contract(self):
        # Cream piers (no gold pillars), graphite pylons with gold LED and cap.
        self.assertIn("const float PierW = 1.4f, CornerPierW = 2.0f, PierDepth = .62f, GlassRecess = .44f;",FACADE)
        self.assertIn('"T11 graphite entrance pylon " + wing, graphite, s * 18.25f, 7.90f, -28f, 2.75f, 15.80f, 2.35f',FACADE)
        self.assertIn("new[] { -36f, -25f, -14f, -3f, 8f, 19f, 30f, 36f }",FACADE)
        self.assertNotIn("GOLD beveled",FACADE)
        self.assertIn("_Metallic\",.98f",CASINO)
    def test_rear_delivery_opening_remains_six_metres_clear(self):
        self.assertIn("rear cream limestone wall LEFT of 6m delivery opening",FACADE)
        self.assertIn("-30.5f, YAttic * .5f, 27.84f, 15f, YAttic, .32f",FACADE)
        self.assertIn("rear cream limestone wall RIGHT of 6m delivery opening",FACADE)
        self.assertIn("10.5f, YAttic * .5f, 27.84f, 55f, YAttic, .32f",FACADE)
        # Left wall ends at x=-23, right wall starts at x=-17: 6 m clear.
        self.assertAlmostEqual((-30.5+7.5)-(10.5-27.5),-6.0)
        self.assertIn('Delivery corridor to rear loading door',CASINO)
    def test_rotunda_keeps_34_by_8_plan_and_48_fascia_segments(self):
        curved=method_source("CurvedFoyer")
        self.assertIn("17f*Mathf.Sin(t)",curved)
        self.assertIn("-28f-8f*Mathf.Cos(t)",curved)
        self.assertIn("for(int i=0;i<segments;i++)",curved)
        self.assertIn("if(i%3==0)",curved) # 16 vertical frames from 48 sectors
        # owner 10.11 13:03: the fascia band is gone; 16:36-16:39 ("to czarne coś ... te jajo, na czym jest szyld"):
        # the slim black roof and its gold edge line are gone too - a thin cream limestone coping, no name on it
        self.assertNotIn("fascia 48_OF_48",curved)
        self.assertNotIn("rotunda roof gold edge line",curved)
        for gone in ("2.50f,.25f,.035f","T11 rotunda upper rounded gold fascia trim","T11 rotunda warm 2700-3000K fine contour LED"):
            self.assertNotIn(gone,curved)
        cap=method_source("RotundaRoofCap")
        self.assertIn("const float yTop=10.95f,yBottom=10.80f,over=.15f;",cap)
        self.assertIn("sharedMaterial=cream;",cap)
        self.assertNotIn("sharedMaterial=roof;",cap)
        # the open void above the rotunda is closed with black glass in black frames
        self.assertIn('"T11 black glass wall above the rotunda",blackGlass',curved)

    def test_roof_skylight_and_single_original_dollar_are_preserved(self):
        self.assertIn("Actual transparent blue-violet rooftop skylight",CASINO)
        self.assertIn("24f,.11f,20f",CASINO)
        self.assertIn(DOLLAR,CASINO)
        self.assertIn(DOLLAR_SHA,CASINO)
        self.assertIn("const float x=0f,z=-21f,roofY=14.40f,nominalHeight=9.0f;",CASINO)
        # Not on the owner board 9.11: code kept, off until the owner decides.
        self.assertEqual(CASINO.count("RoofDollar();"),1)
        self.assertIn("if(RoofDollarOnOwnerBoard)RoofDollar();",CASINO)
        self.assertIn("PrefabUtility.InstantiatePrefab(source)",CASINO)
        file=ROOT/"unity-city"/DOLLAR
        if file.exists():
            self.assertEqual(hashlib.sha256(file.read_bytes()).hexdigest(),DOLLAR_SHA)

    def test_original_wawer_sign_is_pinned_and_not_replaced(self):
        self.assertIn(SIGN,CASINO)
        self.assertIn(SIGN_SHA,CASINO)
        self.assertIn("source=\"+width+\"x\"+height+\" expected=2048x512",CASINO)
        self.assertIn('material.SetTexture("_MainTex",picture)',CASINO)
        self.assertIn('material.SetTexture("_EmissionMap",picture)',CASINO)
        # Owner 9.11: the PNG Quad was turned 180 degrees = it faced INTO the building (empty carrier from the
        # street). The carrier now holds the 3D gold sign; the PNG stays only as a fallback and faces the street.
        self.assertNotIn('Quaternion.Euler(0,180,0)',method_source("MainSign"))
        self.assertIn('Place3DSign("WAWER_kasyno",signRoot',method_source("MainSign"))
        self.assertIn('new Vector3(17.2f,4.30f,1f)',method_source("MainSign"))
        self.assertNotIn("Casino WAWER front deep graphite casing",CASINO)
        self.assertNotIn("Casino WAWER external bronze mounting rim",CASINO)

    def test_sign_backplate_uses_verified_alpha_outline_and_007m_ring(self):
        outer=parse_vector2_array("SignCarrierOuter")
        inner=parse_vector2_array("SignCarrierInner")
        self.assertEqual(len(outer),len(inner))
        self.assertGreaterEqual(len(outer),16)
        self.assertGreater(abs(polygon_area(outer)),abs(polygon_area(inner)))
        widths=[boundary_distance(p,outer) for p in inner]
        self.assertGreaterEqual(min(widths),.069)
        self.assertLessEqual(max(widths),.071)
        self.assertIn("clearance 0.085..0.109m; gold ring 0.070m",CASINO)
        main=method_source("MainSign")
        self.assertIn("PolygonPrism(signRoot",main)
        self.assertIn("PolygonRing(signRoot",main)
        self.assertIn("signBacking,SignCarrierOuter,12.0f,-36.14f,.24f,true",main)
        self.assertIn("gold,SignCarrierOuter,SignCarrierInner,12.0f,-36.2725f,.025f",main)
        self.assertIn("new Vector3(0,12.0f,-36.34f)",main)

    def test_sign_depth_order_has_real_clearance(self):
        # Camera/front is -Z. Image is 0.055m ahead of ring front and
        # 0.080m ahead of carrier front; carrier back leaves >=0.05m to fascia.
        carrier_front=-36.14-.24/2
        carrier_back=-36.14+.24/2
        ring_front=-36.2725-.025/2
        image=-36.34
        self.assertGreaterEqual(carrier_front-image,.03)
        self.assertGreaterEqual(ring_front-image,.03)
        self.assertAlmostEqual(carrier_back,-36.02,places=3)
        self.assertNotIn("var edge=centre+outward",method_source("CurvedFoyer"))      # no gold edge line on the rotunda

    def test_reflections_are_baked_not_realtime(self):
        lighting=method_source("PremiumExteriorLighting")
        self.assertIn("ReflectionProbeMode.Baked",lighting)
        self.assertIn("probe.resolution=64",lighting)
        self.assertIn("Lightmapping.BakeReflectionProbe(probe,probePath)",lighting)
        self.assertIn("T11_Casino_Baked_Reflection.exr",lighting)
        self.assertIn("AddComponent<ReflectionProbe>",lighting)
        self.assertNotIn("AddComponent<Light>",lighting)
        self.assertNotIn("LightType.Point",lighting)
        self.assertNotIn("ReflectionProbeMode.Realtime",lighting)
        self.assertNotIn("ReflectionProbeRefreshMode.EveryFrame",lighting)
        self.assertIn("runtimeRefresh=NO",lighting)

    def test_access_and_garage_contract_unchanged(self):
        self.assertIn('Door("T11_CASINO_WAWER_PUBLIC","Public glass main entry",0,-36.28f,180f,5.6f)',CASINO)
        self.assertIn('GradedDrive(shell,"left_street_road_connection",asphalt,',CASINO)
        self.assertIn("-85f,-49.5f,-48f,-37f,0f,roadTop,true",CASINO)
        self.assertIn("UndergroundGarage();",CASINO)
        self.assertIn("const int baysPerRow=16;",CASINO)
        # Owner 9.11 23:46: "Tabliczka na środku wjazdu ma wypierdalać" - the TAXI board stood on the garage driveway.
        self.assertNotIn('RoadsideOriginalSign("TAXI"',CASINO)
        self.assertNotIn('RoadsideOriginalSign("PARKING"',CASINO)

    def test_logs_do_not_falsely_claim_visual_acceptance(self):
        self.assertIn("ORIGINAL_SIGN_PRESENT=SOURCE_CHECK_PASS",CASINO)
        self.assertIn("UNITY_SCREENSHOTS_ATTACHED=PENDING_REAL_UNITY_RENDER",CASINO)
        self.assertIn("phoneVisualReviewRequired=true",CASINO)
        self.assertNotIn("UNITY_SCREENSHOTS_ATTACHED=PASS",CASINO)


if __name__=="__main__":
    unittest.main()
