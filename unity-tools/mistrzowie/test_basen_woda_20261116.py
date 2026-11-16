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


if __name__ == "__main__":
    unittest.main()
