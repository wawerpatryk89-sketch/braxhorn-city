"""Owner 16.11.2026 07:32-07:33: "Gdy ty widzisz podłogę wykończoną, bo ja nie widzę" (the white floors clipped to a flat
white), "od płotu ... po płytkę, która jest zjebana ... na dworze" (the deck), "woda ma być wodą, nie tylko pasek wody, jak
wejdę do wody, zanurkuję, to ma być woda po całym basenie" (the water body, the diving well, diving), "każdy detal"."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city/Assets"
ED = ASSETS / "Editor/Mistrzowie"
RT = ASSETS / "Mistrzowie"


def read(p):
    return p.read_text(encoding="utf-8")


class WaterBody(unittest.TestCase):
    def setUp(self):
        self.shader = read(RT / "BraxChampionsPoolVolume.shader")
        self.aqua = read(ED / "BraxChampionsAquapark.cs")
        self.teren = read(ED / "BraxChampionsAquaparkTeren.cs")

    def test_metas_have_unique_guids(self):
        for name in ("BraxChampionsPoolVolume.shader", "BraxChampionsPodWodaEkran.shader", "BraxChampionsPodWoda.cs"):
            guid = re.search(r"guid: ([0-9a-f]{32})", read(RT / (name + ".meta"))).group(1)
            hits = [p for p in ASSETS.rglob("*.meta") if guid in p.read_text(errors="ignore")]
            self.assertEqual(len(hits), 1, name)

    def test_volume_shader_draws_the_water_between_the_tiles_and_the_surface(self):
        self.assertIn('Shader "Braxhorn/ChampionsPoolVolume"', self.shader)
        self.assertIn('"Queue"="Transparent-10"', self.shader)
        for needle in ("ZWrite Off", "Cull Back", "Blend One OneMinusSrcAlpha", "v.uv.x * _K", "_WorldSpaceCameraPos"):
            self.assertIn(needle, self.shader)
        # the eye above the water: only the part of the ray under the surface; under the water: the whole ray
        self.assertIn("if (C.y <= S) under = len;", self.shader)
        self.assertIn("w.renderQueue=2990;", self.aqua)

    def test_every_pool_gets_its_water_body(self):
        for call in ("MakePool(lagoon, poolTile, poolFloor, water, true, true, waterBody);",
                     "MakePool(kidsPool, poolTile, poolFloor, waterKids, false, false, waterBodyKids);",
                     "MakePool(s, poolTile, poolFloor, waterSpa, false, false, waterBodySpa);"):
            self.assertIn(call, self.teren)
        self.assertIn("waterBody, RiverLine(), 1.05f * K);", read(ED / "BraxChampionsAquaparkRzeka.cs"))
        self.assertIn("if (vol) WaterBody(p, vol);", self.teren)
        # the skin 5 cm in front of the tiles (no z-fight with the bull mosaic at 2.5-3 cm or the lights)
        self.assertIn("const float g = .05f;", self.teren)

    def test_surface_thinner_and_seen_from_below(self):
        water = read(RT / "BraxChampionsPoolWater.shader")
        self.assertIn("Cull Off", water)
        self.assertIn("if(_WorldSpaceCameraPos.y<i.world.y)", water)
        self.assertIn('water=Water("aq_pool_water",new Color(.03f,.26f,.47f),new Color(.12f,.66f,.86f),.34f,.8f,0f);', self.aqua)

    def test_diving_well_4_m_and_swimming_down_into_it(self):
        self.assertIn("lagoon.pitFloor = DY - 4.20f;", self.teren)
        self.assertIn("float y0 = p.Deepest + .05f", self.teren)
        self.assertIn("Skin(walk, wall, pit, p.pitFloor, p.floor, true);", self.teren)   # walls collide (walk batch)


class Diving(unittest.TestCase):
    def test_dive_through_the_shared_public_api_only(self):
        zone = read(RT / "BraxChampionsPoolSwimVolume.cs")
        self.assertIn("public sealed class BraxChampionsPoolSwimVolume : BraxSwimmingZone", zone)
        self.assertIn("p.EnterSwimmingZone(GetInstanceID(), waterSurface.position.y - BraxChampionsPodWoda.Glebokosc);", zone)
        self.assertIn("base.OnTriggerStay(other);", zone)
        dive = read(RT / "BraxChampionsPodWoda.cs")
        self.assertIn('"NURKUJ"', dive)
        self.assertIn("Input.GetTouch(i)", dive)
        self.assertIn("KeyCode.C", dive)
        self.assertIn("ekran", dive)
        self.assertIn("go.AddComponent<BraxChampionsPodWoda>().ekran=Ekran();", read(ED / "BraxChampionsAquapark.cs"))
        veil = read(RT / "BraxChampionsPodWodaEkran.shader")
        self.assertIn("ZTest Always", veil)
        self.assertIn("_ProjectionParams.x", veil)


class Floors(unittest.TestCase):
    def test_clip_guard_keeps_the_stone_visible(self):
        f = read(ED / "BraxChampionsFoundation.cs")
        self.assertIn("if(top>.94f){float s=.94f/top;gr*=s;gg*=s;gb*=s;}", f)
        self.assertIn('{"floor_tiles_02",new Vector3(184,168,151)}', f)
        self.assertIn('Fotoskan(id,"floor_tiles_02",FloorTiles02Metres,', f)

    def test_deck_lobby_and_terraces_are_real_slabs_and_boards(self):
        aqua = read(ED / "BraxChampionsAquapark.cs")
        self.assertIn('deck=BraxChampionsFoundation.Fotoskan("aq_deck_limestone_slabs","floor_tiles_02",BraxChampionsFoundation.FloorTiles02Metres,', aqua)
        self.assertNotIn('deck=Tex("deck_white_porcelain","long_white_tiles"', aqua)
        self.assertIn('"wood_planks_grey",1.5f', aqua)
        self.assertIn("Fill(walk, teakDeck, teakEast", read(ED / "BraxChampionsAquaparkTeren.cs"))
        bud = read(ED / "BraxChampionsAquaparkBudynek.cs")
        self.assertIn('Fotoskan("aq_lobby_limestone_slabs", "marble_01", BraxChampionsFoundation.Marble01Metres', bud)
        prep = read(ROOT / "unity-tools/mistrzowie/prepare_assets.py")
        self.assertIn("'floor_tiles_02'", prep)
        self.assertIn("'wood_planks_grey'", prep)

    def test_solid_doors_finished(self):
        bud = read(ED / "BraxChampionsAquaparkBudynek.cs")
        self.assertIn("lm.BoxMinMax(doorWalnut, a0, a1, .02f, height - .02f, -.025f, .025f);", bud)
        self.assertNotIn("lm.BoxMinMax(renderIn, a0, a1", bud)
        self.assertIn("if (!glassLeaf) Casing(lx, lz, yaw, width, height);", bud)


class RiverAndPavilion(unittest.TestCase):
    """Owner 16.11 08:03: "ta rzeka ... ma płynąć ... z prądem", "budynek ma być premium ... w środku wykończony, na zewnątrz"."""

    def test_river_surface_flows_and_carries_the_swimmer(self):
        water = read(RT / "BraxChampionsPoolWater.shader")
        self.assertIn("_Flow", water)
        self.assertIn("float2 v=i.uv*_Flow;", water)
        teren = read(ED / "BraxChampionsAquaparkTeren.cs")
        self.assertIn("FlowFill(wat, p.edge, p.islands, p.water, flow, speed)", teren)
        rzeka = read(ED / "BraxChampionsAquaparkRzeka.cs")
        self.assertIn("MakePool(river, poolTile, poolFloor, riverWater ?? water, true, true, waterBody, RiverLine(), 1.05f * K);", rzeka)
        self.assertIn("rz.polSzerokosci = ((RX1 - IX1) * .5f + .3f) * K;", rzeka)
        rt = read(RT / "BraxChampionsLeniwaRzeka.cs")
        self.assertIn("if (jedzie < 0) { Prad(dt); return; }", rt)
        self.assertIn("gracz.body.Move(t.normalized * (predkosc * pradGracza * dt));", rt)
        self.assertIn('riverWater.SetFloat("_Flow",1f)', read(ED / "BraxChampionsAquapark.cs"))

    def test_lawns_get_real_3d_grass(self):
        teren = read(ED / "BraxChampionsAquaparkTeren.cs")
        self.assertIn("LawnGrass(lawns.Concat(", teren)
        self.assertIn("var draw = o.AddComponent<BraxClaudeTrawa>();", teren)

    def test_pavilion_finished_inside_and_out(self):
        bud = read(ED / "BraxChampionsAquaparkBudynek.cs")
        self.assertIn('Fotoskan("aq_pavilion_limestone_cladding", "floor_tiles_02"', bud)
        self.assertNotIn('Tex("pavilion_white_render"', bud)
        self.assertIn('"canopy_walnut_slats"', bud)
        self.assertIn("(BX0 + .2f, -14.1f, BZ0 + .2f, -90.1f)", bud)        # corridor slats
        self.assertIn("BL(doorWalnut, fx - .01f, fx + .01f, y0, y0 + .92f", bud)  # locker doors
        self.assertNotIn("top = FFL + 2.4f;", bud)                           # tiles floor to ceiling
        self.assertIn("zc0 + .095f, zc0 + .115f", bud)                       # mirror off the tiles


class RichFurnishing(unittest.TestCase):
    """Owner 16.11 08:10-08:13: "ja tam widzę jeszcze poprawki", "bogate wyposażenie, wypoczynek, sofy obite skórą w środku,
    restauracja w basenie ... perfekt"."""

    def setUp(self):
        self.r = read(ED / "BraxChampionsAquaparkRestauracja.cs")
        self.bud = read(ED / "BraxChampionsAquaparkBudynek.cs")

    def test_restaurant_bar_tables_banquettes_terrace(self):
        for needle in ('"RESTAURACJA LAGUNA"', "LibraryLeather(\"aq_restaurant_cognac_leather\"", "MStool", "MChair", "MLamp", "MSet", "Parasol(V(px, DY, pz), 0)"):
            self.assertIn(needle, self.r)
        self.assertIn("Restaurant(); Lounges();", self.bud)
        self.assertIn('Door("RESTAURACJA LAGUNA wyjście na taras", 0f, BZ1, 0f, 2.5f, 2.6f, true);', self.bud)
        meta = read(ED / "BraxChampionsAquaparkRestauracja.cs.meta")
        guid = re.search(r"guid: ([0-9a-f]{32})", meta).group(1)
        self.assertEqual(len([p for p in ASSETS.rglob("*.meta") if guid in p.read_text(errors="ignore")]), 1)

    def test_leather_lounges_are_real_poly_haven_models(self):
        self.assertIn('MSofa = PA + "sofa_02/sofa_02_2k.gltf"', self.r)
        self.assertIn('LeatherSofa(27f, -74.2f, 0f, "jacuzzi: sofa skórzana");', self.r)
        self.assertNotIn("m.RoundedBox(sofaLeather", self.bud)        # no box sofas any more

    def test_every_wall_face_closed_and_over_every_door(self):
        self.assertIn("if (g.h > 0f && top - g.h > .02f) BL(mat, g.a, g.b, g.h, top, za, zb);", self.bud)
        self.assertIn("PanelZ(tiles, -14f, -1f, e, zIn, BZ1 - eo - d);", self.bud)
        self.assertIn("RevealZ(walnut, -14f, -95.4f, -91.0f, FFL + 2.8f);", self.bud)
        self.assertIn('"aq_changing_grey_floor_tiles", "large_floor_tiles_02"', self.bud)
        self.assertIn("'large_floor_tiles_02'", read(ROOT / "unity-tools/mistrzowie/prepare_assets.py"))


class SaunasSpasTowerDecor(unittest.TestCase):
    """Owner 16.11 08:25-08:26: "Sauny, gdzie są porobione sauny? Jacuzzi ...? Co to są za tandetne bramki? Kwiatki, obrazy",
    "zjeżdżalnia funkcjonalna ... wchodziłem na górę, to spadałem w przepaść ... wszystko ma być funkcjonalne"."""

    def setUp(self):
        self.w = read(ED / "BraxChampionsAquaparkWellness.cs")
        self.bud = read(ED / "BraxChampionsAquaparkBudynek.cs")
        self.t = read(ED / "BraxChampionsAquaparkZjezdzalnie.cs")

    def test_walk_in_saunas_and_built_spa_pools(self):
        self.assertIn('Sauna("SAUNA FIŃSKA 90°C", 40.095f, 44.55f, false);', self.w)
        self.assertIn('Sauna("ŁAŹNIA PAROWA 45°C", 44.65f, 48.45f, true);', self.w)
        self.assertIn("Door(name, dc, zf, 0f, .8f, 2.05f, true);", self.w)          # a door that opens
        self.assertIn('Seat("SAUNA"', self.w)
        self.assertIn('IndoorSpa("JACUZZI SPA 1", 21.0f, -82.6f);', self.w)
        self.assertIn("WaterBody(new Pool", self.w)
        self.assertIn("Wellness();", self.bud)
        self.assertNotIn('Owner("jacuzzi_wanna"', self.bud)
        self.assertIn("'hinoki_planks'", read(ROOT / "unity-tools/mistrzowie/prepare_assets.py"))
        guid = re.search(r"guid: ([0-9a-f]{32})", read(ED / "BraxChampionsAquaparkWellness.cs.meta")).group(1)
        self.assertEqual(len([p for p in ASSETS.rglob("*.meta") if guid in p.read_text(errors="ignore")]), 1)

    def test_pictures_plants_and_real_gates(self):
        self.assertIn("MFrame1", self.w)
        self.assertIn("MClock", self.w)
        self.assertIn('"aq_gate_brushed_inox"', self.bud)
        self.assertIn("(-95.4f, -91.0f, FFL + 2.8f)", self.bud)                     # panels to the exact opening
        # grey steel is grey: the brass Metal048A map's hue divided out for grey tints
        self.assertIn("tint=new Color(N(tint.r,.991f),N(tint.g,.768f),N(tint.b,.332f),tint.a);", read(ED / "BraxChampionsFoundation.cs"))

    def test_tower_never_drops_you_into_the_void(self):
        self.assertIn('"klatka schodowa: szkło"', self.t)
        self.assertIn('"między biegami: szkło"', self.t)
        self.assertIn('"podest: balustrada"', self.t)
        self.assertIn("Rail(V(WellX1, L4, 61.0f), V(WellX1, L4, WellZ1), (0f, 0f));", self.t)
        self.assertIn('bar.AddComponent<BoxCollider>().size = new Vector3(halfW * 2f + .2f, 2.4f, .12f) * K;', self.t)
        self.assertIn('Col(s0, s1, y, y + 1.6f, a.z - .06f, a.z + .06f, "balustrada");', self.t)


class OwnAudit0846(unittest.TestCase):
    """Owner 08:46: "sam wyciągaj błędy ... małe palmy w doniczkach w środku", "te bramki nadal są spierdolone"."""

    def test_gate_wings_are_real_doors(self):
        bud = read(ED / "BraxChampionsAquaparkBudynek.cs")
        self.assertIn("static void GateWings(", bud)
        self.assertIn('var d = group.gameObject.AddComponent<BraxCityDoor>(); d.id = "aquapark_bramka_" + gateCount;', bud)
        self.assertIn("BraxChampionsMeshBatch.Collider(hinge, \"skrzydło kolizja\"", bud)

    def test_indoor_potted_palms_and_decks(self):
        w = read(ED / "BraxChampionsAquaparkWellness.cs")
        self.assertIn("static void IndoorPalm(float x, float z, int i)", w)
        self.assertIn("o.transform.localScale *= 2.3f * K / h;", w)
        wy = read(ED / "BraxChampionsAquaparkWyposazenie.cs")
        self.assertIn("Palm(deckPalms, new Vector3(c.x, DY + .32f, c.y), 50 + dp++);", wy)
        self.assertIn('PH(MStool, p,', wy)
        self.assertIn('rock=Tex("waterfall_rock_boulder","rock_boulder_dry",1.8f', read(ED / "BraxChampionsAquapark.cs"))
        self.assertIn("'rock_boulder_dry'", read(ROOT / "unity-tools/mistrzowie/prepare_assets.py"))


if __name__ == "__main__":
    unittest.main()
