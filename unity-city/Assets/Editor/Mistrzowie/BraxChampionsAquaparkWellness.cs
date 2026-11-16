using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// AQUAPARK BRAXHORN - SAUNY, JACUZZI I WYSTRÓJ (owner 16.11 08:25: "Sauny, gdzie są porobione sauny? Jacuzzi, gdzie masz
// porobione jacuzzi? ... Kwiatki jakieś, obrazy, cokolwiek, żeby tam nie było goło"; 08:26 "wszystko ma być funkcjonalne").
//   * SAUNA FIŃSKA (x 40.1..44.6) and ŁAŹNIA PAROWA (x 44.6..48.45) built into the sauna room's south-west corner, 3.6 m deep:
//     real rooms you walk into through a glass door (it opens on approach), sit on (BraxSeat on every bench place) -
//     Poly Haven hinoki planks inside and out, two-tier slatted benches with backrests, a stove with stones and a guard, a
//     bucket with a ladle, LED under the benches; the steam bath tiled, its glass misted;
//   * TWO JACUZZI in the jacuzzi hall: round raised spa pools (r 1.75 m water, black granite outside, white marble coping,
//     mosaic inside, a bench ring under the water with eight places, bubbling water, the water body, lights, steps up with a
//     stainless handrail) - you climb in and sit;
//   * pictures (Poly Haven frames) on every corridor and room wall, plants by the doors and in the corners, a clock.
public static partial class BraxChampionsAquapark
{
    public static int Saunas, IndoorSpas, Pictures;
    static readonly string MFrame1 = PT + "fancy_picture_frame_01/fancy_picture_frame_01_1k.gltf", MFrame2 = PT + "hanging_picture_frame_02/hanging_picture_frame_02_1k.gltf",
        MClock = PT + "wall_clock/wall_clock_1k.gltf";

    static Material hinoki, steamTile, haze;

    static void Wellness()
    {
        hinoki = Inside(Tex("sauna_hinoki_planks", "hinoki_planks", 1.89f, Color.white), 1.0f);       // real size 1.89 m
        steamTile = Inside(BraxChampionsFoundation.Fotoskan("aq_steam_bath_tiles", "long_white_tiles", 1.27f, new Color(150f / 255f, 186f / 255f, 190f / 255f), BraxChampionsFoundation.MeanLongWhiteTiles), 1.0f);
        haze = S("steam_haze", new Color(.93f, .95f, .96f, .30f), .9f, 0f, null, true);
        Sauna("SAUNA FIŃSKA 90°C", 40.095f, 44.55f, false);
        Sauna("ŁAŹNIA PAROWA 45°C", 44.65f, 48.45f, true);
        IndoorSpa("JACUZZI SPA 1", 21.0f, -82.6f);
        IndoorSpa("JACUZZI SPA 2", 33.0f, -82.6f);
        Decor();
    }

    // a sauna room x0..x1 against the south wall (inner face z -89.905), the glass front at z -86.3, ceiling 2.35 m
    static void Sauna(string name, float x0, float x1, bool steam)
    {
        const float zb = -89.905f, zf = -86.3f, h = 2.35f;
        var inner = steam ? steamTile : hinoki;
        // walls: the side walls 10 cm (hinoki outside, the room's finish inside), the lining on the back wall, the ceiling
        foreach (float wx in new[] { x0, x1 })
        {
            bool west = wx == x0;
            if (west && x0 < 40.2f) BL(inner, 40.095f, 40.115f, FFL, FFL + h, zb, zf);          // over the room's oak panels
            else { BL(hinoki, wx - .05f, wx + .05f, FFL, FFL + h + .07f, zb, zf + .05f, true); BL(inner, west ? wx + .05f : wx - .07f, west ? wx + .07f : wx - .05f, FFL, FFL + h, zb + .02f, zf); }
        }
        BL(inner, x0 + .02f, x1 - .02f, FFL, FFL + h, zb, zb + .02f);
        BL(hinoki, x0, x1, FFL + h, FFL + h + .07f, zb, zf + .05f);
        BL(inner, x0 + .02f, x1 - .02f, FFL + h - .02f, FFL + h, zb, zf);
        // the front: glass in black frames, a glass door .8 m in the middle, a hinoki fascia with the name over it
        float dc = (x0 + x1) * .5f, d0 = dc - .4f, d1 = dc + .4f;
        foreach (var (a, b) in new[] { (x0 + .05f, d0), (d1, x1 - .05f) })
        {
            BL(steam ? haze : glass, a + .03f, b - .03f, FFL + .05f, FFL + 2.05f, zf - .01f, zf + .01f);
            Col(CX + a, CX + b, FFL, FFL + h, CZ + zf - .05f, CZ + zf + .05f, "sauna: szyba");
            foreach (float mx in new[] { a, b }) BL(black, mx - .03f, mx + .03f, FFL, FFL + 2.1f, zf - .04f, zf + .04f);
            BL(black, a, b, FFL, FFL + .05f, zf - .04f, zf + .04f);
        }
        BL(black, x0 + .05f, x1 - .05f, FFL + 2.05f, FFL + 2.1f, zf - .04f, zf + .04f);
        BL(hinoki, x0 + .05f, x1 - .05f, FFL + 2.1f, FFL + h, zf - .05f, zf + .05f);
        Door(name, dc, zf, 0f, .8f, 2.05f, true);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, name, V(dc, FFL + 2.14f, zf + .06f), 0f, .13f, .02f, .08f, x1 - x0 - .4f);
        // two benches along the back wall: upper .95 m deep .6, lower .48 m deep .55 (slats 9 cm, 1 cm gaps), backrest boards
        var mat = steam ? steamTile : hinoki;
        float bx0 = x0 + .12f, bx1 = x1 - .12f;
        foreach (var (y, za, zz) in new[] { (FFL + .95f, zb + .02f, zb + .62f), (FFL + .48f, zb + .62f, zb + 1.17f) })
        {
            if (steam) BL(mat, bx0, bx1, FFL, y, za, zz, true);
            else
            {
                for (float z = za; z + .09f <= zz + .001f; z += .10f) BL(hinoki, bx0, bx1, y - .03f, y, z, z + .09f);
                BL(hinoki, bx0, bx1, y - .12f, y - .03f, zz - .05f, zz);                         // the apron
                foreach (float px in new[] { bx0 + .05f, (bx0 + bx1) * .5f, bx1 - .1f }) BL(hinoki, px, px + .05f, FFL, y - .03f, zz - .1f, zz - .05f);
                Col(CX + bx0, CX + bx1, FFL, y, CZ + za, CZ + zz, "sauna: ława");
                BL(led, bx0, bx1, y - .125f, y - .115f, zz - .06f, zz - .05f);
            }
            for (float sx = bx0 + .45f; sx < bx1 - .3f; sx += .75f) Seat("SAUNA", V(sx, y + .02f, (za + zz) * .5f + .06f), 0f);
        }
        if (!steam)
        {
            for (float y = FFL + 1.05f; y < FFL + 1.6f; y += .14f) BL(hinoki, bx0, bx1, y, y + .09f, zb + .02f, zb + .05f);
            // the stove in the front-east corner: black steel, the stones on top, a hinoki guard round it
            float sx = x1 - .55f, sz = zf - .5f;
            BL(black, sx - .25f, sx + .25f, FFL, FFL + .70f, sz - .25f, sz + .25f, true);
            BL(steel, sx - .27f, sx + .27f, FFL + .70f, FFL + .73f, sz - .27f, sz + .27f);
            var rnd = new System.Random(name.Length);
            for (int i = 0; i < 14; i++)
            {
                var p = V(sx + ((float)rnd.NextDouble() - .5f) * .4f, FFL + .78f + (i / 7) * .06f, sz + ((float)rnd.NextDouble() - .5f) * .4f);
                m.RoundedBox(rock, p, new Vector3(.11f, .07f, .09f) * (.8f + .4f * (float)rnd.NextDouble()), .03f, Quaternion.Euler(0, rnd.Next(360), 0), 1);
            }
            foreach (float gx in new[] { sx - .42f, sx + .42f }) BL(hinoki, gx - .02f, gx + .02f, FFL, FFL + .95f, sz - .42f, sz + .42f);
            BL(hinoki, sx - .42f, sx + .42f, FFL + .9f, FFL + .95f, sz - .44f, sz - .40f);
            // the bucket and the ladle on the lower bench, a sauna lamp in the corner
            var bk = V(bx0 + .35f, FFL + .48f, zb + .9f);
            m.Cylinder(hinoki, bk, .14f, .22f, 14, Quaternion.identity, false, true, .16f);
            m.Cylinder(steel, bk + Vector3.up * .05f, .162f, .02f, 14); m.Cylinder(steel, bk + Vector3.up * .17f, .172f, .02f, 14);
            m.Tube(hinoki, bk + new Vector3(.05f, .2f, 0f), bk + new Vector3(.40f, .34f, 0f), .015f, 6);
            m.Cylinder(downlight, V(x0 + .2f, FFL + h - .3f, zb + .05f), .08f, .02f, 12, Quaternion.FromToRotation(Vector3.up, Vector3.forward), true, false);
            // the duckboard floor in front of the benches
            for (float z = zb + 1.2f; z < zf - .15f; z += .12f) BL(hinoki, bx0, bx1 - .9f, FFL, FFL + .025f, z, z + .09f);
        }
        else
        {
            // the steam bath: an L of tiled seats, the steam outlet, the haze that fills it
            BL(steamTile, x1 - .62f, x1 - .07f, FFL, FFL + .48f, zb + 1.17f, zf - .9f, true);
            m.Cylinder(steel, V(x1 - .35f, FFL + .12f, zf - .55f), .07f, .05f, 12);
            BL(haze, x0 + .1f, x1 - .1f, FFL + .05f, FFL + h - .05f, zb + .1f, zf - .1f);
            Seat("ŁAŹNIA", V(x1 - .35f, FFL + .5f, zb + 1.6f), -90f);
        }
        Saunas++;
    }

    // a raised round spa pool on the hall's floor
    static void IndoorSpa(string name, float cx, float cz)
    {
        const float r0 = 1.75f, r1 = 2.05f, top = FFL + .95f, water = FFL + .85f, floor = FFL + .02f, seat = water - .45f;
        var c = new Vector2(CX + cx, CZ + cz);
        var inner = Circle(c, r0, 56); var outer = Circle(c, r1, 56); var nose = Circle(c, r1 + .05f, 56);
        Skin(walk, marbleBlack, Ccw(outer), FFL, top - .05f, false);
        Band(walk, coping, Ccw(inner), Ccw(nose), top, top); Skin(walk, coping, Ccw(nose), top - .05f, top, false);
        Skin(walk, waterline, Ccw(inner), water - .15f, top, true);
        Skin(walk, poolTile, Ccw(inner), floor, water - .15f, true);
        Fill(walk, poolFloor, Ccw(inner), null, floor);
        var benchIn = Circle(c, r0 - .45f, 56);
        Band(walk, coping, Ccw(benchIn), Ccw(Circle(c, r0 - .002f, 56)), seat, seat);
        Skin(walk, poolTile, Ccw(benchIn), floor, seat, true);
        Fill(m, waterSpa, Ccw(inner), null, water);
        if (waterBodySpa) WaterBody(new Pool { id = name, edge = Ccw(inner), water = water, floor = floor }, waterBodySpa);
        for (int k = 0; k < 8; k++)
        {
            float a = (k + .5f) * 45f * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var at = c + d * (r0 - .26f);
            var g = new GameObject(name + " | miejsce " + (k + 1)); g.transform.SetParent(seats, false);
            g.transform.localPosition = new Vector3(at.x, seat + .02f, at.y) * K; g.transform.localRotation = Quaternion.LookRotation(new Vector3(-d.x, 0, -d.y));
            var st = g.AddComponent<BraxSeat>(); st.label = "JACUZZI"; st.surfaceY = (seat + .02f) * K; Seats++;
            var j = new Vector3(c.x + d.x * (r0 - .01f), seat + .25f, c.y + d.y * (r0 - .01f));
            m.Cylinder(steel, j, .045f, .01f, 12, Quaternion.FromToRotation(Vector3.up, new Vector3(-d.x, 0, -d.y)), true, false);
        }
        foreach (int k in new[] { 0, 1, 2, 3 })
        {
            float a = k * 90f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            m.Cylinder(uplight, new Vector3(c.x, floor + .2f, c.y) + d * (r0 - .45f - .012f), .08f, .01f, 14, Quaternion.FromToRotation(Vector3.up, -d), true, false);
        }
        // steps up on the side facing the hall's middle (two risers of 32 cm, granite), a stainless handrail over the coping
        float dir = cx < 27f ? 1f : -1f;
        var s0 = new Vector3(c.x + dir * (r1 + .55f), 0, c.y);
        walk.Box(marbleBlack, new Vector3(s0.x, FFL + .16f, s0.z), new Vector3(.4f, .32f, 1.2f), Quaternion.identity);
        walk.Box(marbleBlack, new Vector3(s0.x - dir * .4f, FFL + .32f, s0.z), new Vector3(.4f, .64f, 1.2f), Quaternion.identity);
        var h0 = new Vector3(s0.x + dir * .25f, FFL + .95f, s0.z + .62f); var h1 = new Vector3(c.x + dir * (r1 - .2f), top + .9f, s0.z + .62f);
        m.Tube(steel, h0 + Vector3.down * .95f, h0, .022f, 10); m.Tube(steel, h0, h1, .022f, 10); m.Tube(steel, h1, h1 + Vector3.down * (h1.y - water + .2f), .022f, 10);
        IndoorSpas++;
    }

    // pictures, plants, a clock: every corridor and room has something on its walls and in its corners
    static void Decor()
    {
        // large framed pictures (the Poly Haven frames at 1.9x: ~1.1 x 0.9 m, a gallery size on a 4.4 m wall)
        void Pic(float x, float y, float z, float yaw, int i) { if (PH(i % 2 == 0 ? MFrame1 : MFrame2, V(x, y, z), yaw, "obraz", false, false, true, 1.9f)) Pictures++; }
        int n = 0;
        // corridors: the corridor wall (faces south, yaw 180) and the street wall's inside (faces north, yaw 0)
        foreach (float x in new[] { -53f, -42f, -31f, -19.5f, 20f, 33f, 42.5f, 55f }) Pic(x, FFL + 1.65f, -90.11f, 180f, n++);
        foreach (float x in new[] { -52f, -42f, -31f, -20f, 20f, 31f, 42f, 53f }) Pic(x, FFL + 1.65f, BZ0 + .185f, 0f, n++);
        // the restaurant over the banquettes (both side walls)
        foreach (float z in new[] { -74.2f, -70.6f, -67.0f }) { Pic(-13.89f, FFL + 1.75f, z, 90f, n++); Pic(13.89f, FFL + 1.75f, z, -90f, n++); }
        // the jacuzzi hall: the corridor wall (faces north) and the wall to the massage rooms (faces west)
        foreach (float x in new[] { 18f, 22.5f, 31.5f, 36f }) Pic(x, FFL + 1.8f, -89.89f, 0f, n++);
        foreach (float z in new[] { -86f, -79f, -67f }) Pic(39.89f, FFL + 1.8f, z, -90f, n++);
        // the sauna room's north wall (faces south) beside the door to massage B, the massage rooms' side walls
        foreach (float x in new[] { 43f, 46.5f, 56.5f }) Pic(x, FFL + 1.7f, -80.11f, 180f, n++);
        foreach (float z in new[] { -77f, -69f }) { Pic(40.11f, FFL + 1.6f, z, 90f, n++); Pic(48.89f, FFL + 1.6f, z, -90f, n++); Pic(49.11f, FFL + 1.6f, z, 90f, n++); }
        // plants: the corridors' ends and doors, the jacuzzi hall's corners, the changing rooms by the vanities, the lobby
        foreach (var (x, z) in new[] { (-57.3f, -90.8f), (57.3f, -90.8f), (-15.2f, -90.8f), (15.2f, -95.4f), (-45.3f, -89.3f), (-23.3f, -89.3f),
                                       (-7.3f, -84.2f), (7.3f, -84.2f) })
            PH(MPlant, V(x, FFL, z), x * 11f + z, "roślina", true);
        // 16.11 08:46 owner "małe palmy w doniczkach, żeby coś tam było w środku": young palms (his palm models at 2.3 m) in
        // tapered black granite pots - the corridors' ends, the lobby's entrance corners, the restaurant's corners by the glass,
        // the jacuzzi hall's four corners, the sauna room
        int k = 0;
        foreach (var (x, z) in new[] { (-57.2f, -95.3f), (57.2f, -95.3f), (-12.6f, -95.3f), (12.6f, -95.3f), (-13.2f, -64.8f), (13.2f, -64.8f),
                                       (15.0f, -89.3f), (39.1f, -89.3f), (39.1f, -64.8f), (15.0f, -64.8f), (57.2f, -80.8f) })
            IndoorPalm(x, z, k++);
        DecorEnd();
    }

    static GameObject[] palmSrc;
    public static int IndoorPalms;
    static void IndoorPalm(float x, float z, int i)
    {
        var b = V(x, FFL, z);
        m.Cylinder(marbleBlack, b, .30f, .58f, 28, Quaternion.identity, false, true, .37f);          // the tapered pot
        m.Cylinder(black, b + Vector3.up * .58f, .375f, .025f, 28, Quaternion.identity, true, false, .375f);   // its rim
        m.Cylinder(bark, b + Vector3.up * .545f, .35f, .012f, 28, Quaternion.identity, true, false);      // the bark mulch
        Col(CX + x - .36f, CX + x + .36f, FFL, FFL + .6f, CZ + z - .36f, CZ + z + .36f, "donica palmy");
        if (palmSrc == null) palmSrc = new[] { BraxMiastoAssety.Model("miasto_palmy", "palma"), BraxMiastoAssety.Model("miasto_palmy", "palma2") };
        var s = palmSrc[i % 2]; if (!s) { OwnerMissing++; return; }
        var soil = (b + Vector3.up * .56f) * K;
        var o = BraxChampionsModel.Postaw(models, s, "palma w donicy " + (i + 1), soil, (i * 97.3f) % 360f, 1f, true, true);
        if (!o) return;
        // its real height measured after the import, brought to a young palm's 2.3 m over the soil
        var rs = o.GetComponentsInChildren<Renderer>(true);
        float h = 0f; if (rs.Length > 0) { var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); h = bb.size.y; }
        if (h < .01f) h = 11f * K;                                                                    // (no bounds: the preview's stand-in)
        o.transform.localScale *= 2.3f * K / h;
        BraxChampionsPalmTrunk.OnSpot(o, soil);
        rs = o.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0) { var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds); o.transform.position += Vector3.up * (soil.y - bb.min.y); }
        IndoorPalms++;
    }

    static void DecorEnd()
    {
        PH(MClock, V(-17.5f, FFL + 2.45f, -90.11f), 180f, "zegar", false, false, true);
        PH(MClock, V(51.0f, FFL + 2.45f, -80.11f), 180f, "zegar", false, false, true);
    }
}
