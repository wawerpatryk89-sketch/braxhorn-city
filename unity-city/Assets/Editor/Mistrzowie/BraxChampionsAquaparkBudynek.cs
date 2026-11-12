using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN (BraxChampionsAquapark): the ENTRANCE PAVILION on road Z70 (local x -58..58, z -96..-64, 116 x 32 m,
// 4.4 m inside, roof 6.6 m, attic 7.2 m). From the street: a glass lobby under an 8 m canopy, the 3D name AQUAPARK BRAXHORN on
// the black attic, white render with black fins on the wings.
//   LOBBY x -14..14: reception (white quartz, LED line), the black marble wall with the bull mark, sofas, the glass to the
//     aquapark; stainless turnstiles into the west corridor, the SPA door into the east one.
//   WEST: corridor z -96..-90, changing rooms KOBIETY (x -58..-36) and MĘŻCZYŹNI (x -36..-14): lockers, the owner's benches,
//     four shower cabins and two double vanities each (his library), WC cubicles, a glass door out to the pool deck.
//   EAST (SPA & WELLNESS): corridor; the JACUZZI HALL x 14..40 (the owner's four hot tubs, bubbling, you sit in them,
//     loungers at the glass, a door to the outdoor jacuzzi deck); the SAUNA room x 40..58, z -90..-80 (his two cabins);
//     MASSAGE A / B x 40..49 / 49..58, z -80..-64 (his tables, you lie on them).
// Every door opens on approach (BraxCityDoor); walls, desk, lockers, tubs have colliders.
public static partial class BraxChampionsAquapark
{
    const float BX0 = -58f, BX1 = 58f, BZ0 = -96f, BZ1 = -64f, FFL = DY + .02f, CY = FFL + 4.4f, RY = DY + 6.6f, AY = DY + 7.2f;
    static Material render, renderIn, terrazzo, wetTile, marbleBlack, marbleWhite, quartz, ceiling, frosted, lockerMat, plate, mirror, downlight, fin;
    static int doors;

    static Material Inside(Material x, float a) { BraxChampionsInteriorLight.Apply(x, a); return x; }

    static void BuildingMaterials()
    {
        render = Tex("pavilion_white_render", "white_stucco_02", 2.0f, new Color(1.06f, 1.06f, 1.08f));
        BraxChampionsInteriorLight.Facade(render);
        renderIn = Inside(Tex("pavilion_inside_render", "white_stucco_02", 2.0f, new Color(1.04f, 1.04f, 1.06f)), 1.05f);
        terrazzo = Inside(Tex("lobby_terrazzo", "terrazzo_tiles", 1.4f, new Color(1.10f, 1.38f, 1.74f)), 1.0f);      // the warm map neutralised: light grey
        wetTile = Inside(Tex("wet_antislip_tiles", "anti_skid_tiles", 1.0f, new Color(1.05f, 1.31f, 1.68f)), 1.0f);
        marbleBlack = Inside(BraxChampionsFoundation.LibraryMarble("aq_lobby_black_marble", new Color(1f, 1f, 1f), 1.8f), 1.0f);
        marbleWhite = Inside(BraxChampionsFoundation.LibraryMarbleWhite("aq_spa_white_marble", new Color(1f, 1f, 1f), 1.6f), 1.0f);
        quartz = Inside(S("reception_white_quartz", new Color(.95f, .95f, .96f), .18f, 0f), 1.0f);
        ceiling = Inside(S("pavilion_ceiling_white", new Color(.93f, .93f, .94f), .7f, 0f), 1.1f);
        frosted = S("pavilion_frosted_glass", new Color(.90f, .94f, .96f, .62f), .3f, 0f, null, true);
        lockerMat = Inside(S("locker_graphite_hpl", new Color(.16f, .17f, .19f), .42f, .05f), 1.0f);
        plate = S("locker_number_plate", new Color(.85f, .86f, .88f), .25f, .8f);
        mirror = S("vanity_mirror", new Color(.78f, .82f, .86f), .03f, 1f);
        downlight = S("pavilion_downlight", new Color(1f, .96f, .88f), .3f, 0f, new Color(1.25f, 1.18f, 1.02f));
        fin = S("pavilion_black_fin", new Color(.03f, .032f, .035f), .38f, .6f);
    }

    static void BL(Material mat, float x0, float x1, float y0, float y1, float z0, float z1, bool col = false) => B(mat, CX + x0, CX + x1, y0, y1, CZ + z0, CZ + z1, col);

    // wall along x at z (thickness t), gaps (a, b, head) in local x
    static void WallX(Material mat, float z, float x0, float x1, float y0, float y1, float t, params (float a, float b, float h)[] gaps)
    {
        float x = x0;
        foreach (var g in gaps.OrderBy(q => q.a)) { BL(mat, x, g.a, y0, y1, z - t * .5f, z + t * .5f, true); BL(mat, g.a, g.b, g.h, y1, z - t * .5f, z + t * .5f, true); x = g.b; }
        BL(mat, x, x1, y0, y1, z - t * .5f, z + t * .5f, true);
    }
    static void WallZ(Material mat, float x, float z0, float z1, float y0, float y1, float t, params (float a, float b, float h)[] gaps)
    {
        float z = z0;
        foreach (var g in gaps.OrderBy(q => q.a)) { BL(mat, x - t * .5f, x + t * .5f, y0, y1, z, g.a, true); BL(mat, x - t * .5f, x + t * .5f, g.h, y1, g.a, g.b, true); z = g.b; }
        BL(mat, x - t * .5f, x + t * .5f, y0, y1, z, z1, true);
    }

    // a door that opens on approach (BraxCityDoor): one leaf up to 1.5 m, two leaves above; glass in black frames or solid
    static void Door(string label, float lx, float lz, float yaw, float width, float height, bool glassLeaf)
    {
        var group = Group(root, label + " | drzwi (otwierają się przy podejściu)");
        group.localPosition = V(lx, FFL, lz) * K; group.localRotation = Quaternion.Euler(0, yaw, 0);
        int n = width > 1.5f ? 2 : 1; var leaves = new Transform[n]; var sides = new float[n];
        for (int i = 0; i < n; i++)
        {
            float side = n == 1 ? -1f : (i == 0 ? -1f : 1f), lw = width / n;
            var hinge = Group(group, "skrzydło " + (i + 1)); hinge.localPosition = new Vector3(side * width * .5f, 0, 0) * K;
            float a0 = Mathf.Min(0f, -side * lw) + .01f, a1 = Mathf.Max(0f, -side * lw) - .01f;
            var lm = new BraxChampionsMeshBatch();
            if (glassLeaf)
            {
                lm.BoxMinMax(black, a0, a1, height - .12f, height - .02f, -.04f, .04f); lm.BoxMinMax(black, a0, a1, .02f, .20f, -.04f, .04f);
                lm.BoxMinMax(black, a0, a0 + .08f, .20f, height - .12f, -.04f, .04f); lm.BoxMinMax(black, a1 - .08f, a1, .20f, height - .12f, -.04f, .04f);
                lm.BoxMinMax(glass, a0 + .08f, a1 - .08f, .20f, height - .12f, -.01f, .01f);
                float px = side < 0 ? a1 - .26f : a0 + .26f;
                foreach (float f in new[] { -1f, 1f }) lm.BoxMinMax(steel, px - .025f, px + .025f, .7f, 1.9f, f > 0 ? .04f : -.10f, f > 0 ? .10f : -.04f);
            }
            else
            {
                lm.BoxMinMax(renderIn, a0, a1, .02f, height - .02f, -.025f, .025f);
                float px = side < 0 ? a1 - .12f : a0 + .12f;
                foreach (float f in new[] { -1f, 1f }) lm.BoxMinMax(steel, px - .08f, px + .08f, 1.02f, 1.06f, f > 0 ? .025f : -.075f, f > 0 ? .075f : -.025f);
            }
            lm.Build(hinge, label + " skrzydło " + (i + 1), Folder, false);
            BraxChampionsMeshBatch.Collider(hinge, "skrzydło kolizja", new Vector3((a0 + a1) * .5f, height * .5f, 0), new Vector3(a1 - a0, height, .08f));
            foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            leaves[i] = hinge; sides[i] = n == 1 ? 1f : (i == 0 ? 1f : -1f);
        }
        var d = group.gameObject.AddComponent<BraxCityDoor>(); d.id = "aquapark_" + (++doors); d.label = label; d.leaves = leaves; d.sides = sides;
    }

    static void Building()
    {
        // ---------------- floors (walk), ceiling, roof
        var lobbyFloor = W(new[] { new Vector2(-14, BZ0), new Vector2(14, BZ0), new Vector2(14, BZ1), new Vector2(-14, BZ1) });
        Fill(walk, terrazzo, lobbyFloor, null, FFL);
        foreach (var (x0, x1) in new[] { (BX0, -14f), (14f, BX1) })
        {
            Fill(walk, terrazzo, W(new[] { new Vector2(x0, BZ0), new Vector2(x1, BZ0), new Vector2(x1, -90f), new Vector2(x0, -90f) }), null, FFL);
            Fill(walk, x0 > 0 ? marbleWhite : wetTile, W(new[] { new Vector2(x0, -90f), new Vector2(x1, -90f), new Vector2(x1, BZ1), new Vector2(x0, BZ1) }), null, FFL);
        }
        var all = W(new[] { new Vector2(BX0, BZ0), new Vector2(BX1, BZ0), new Vector2(BX1, BZ1), new Vector2(BX0, BZ1) });
        Skin(m, render, Ccw(all), DY - .30f, FFL, false);                                            // slab edge (sides only)
        Fill(m, ceiling, all, null, CY, true);
        // roof: light grey membrane (not a black slab seen from the tower and the hotels), three skylight ribbons over the
        // lobby, the changing rooms and the SPA, rooftop air handlers on black frames
        var roofMat = S("pavilion_roof_light_membrane", new Color(.66f, .68f, .71f), .86f, 0f);
        var lights = new List<List<Vector2>>();
        foreach (var (x0, x1) in new[] { (-10f, 10f), (-50f, -22f), (20f, 52f) }) lights.Add(W(new[] { new Vector2(x0, -86f), new Vector2(x1, -86f), new Vector2(x1, -74f), new Vector2(x0, -74f) }));
        Fill(m, roofMat, all, lights, RY);
        foreach (var l in lights)
        {
            Skin(m, black, Ccw(l), RY, RY + .45f, false);
            var lo = l.Min(q => q.x); var hi = l.Max(q => q.x); var z0 = l.Min(q => q.y); var z1 = l.Max(q => q.y);
            m.BoxMinMax(glass, lo + .05f, hi - .05f, RY + .40f, RY + .45f, z0 + .05f, z1 - .05f);
            for (float x = lo + 2f; x < hi - .5f; x += 2f) m.BoxMinMax(black, x - .04f, x + .04f, RY + .38f, RY + .46f, z0, z1);
        }
        foreach (float x in new[] { -38f, -30f, 34f, 42f })
        {
            BL(black, x - 1.6f, x + 1.6f, RY, RY + .25f, -71f, -67f);
            BL(S("roof_air_handler_grey", new Color(.55f, .57f, .60f), .5f, .6f), x - 1.5f, x + 1.5f, RY + .25f, RY + 1.85f, -70.8f, -67.2f);
        }
        for (float x = BX0 + 2.5f; x < BX1; x += 4f) for (float z = BZ0 + 2f; z < BZ1; z += 4f) m.Cylinder(downlight, V(x, CY - .01f, z), .11f, .01f, 14);
        // ---------------- envelope: attic band, wings in white render with black fins, the glass lobby, glass to the pool
        float t = .30f;
        // south: wings solid, lobby glass with the entrance doors
        WallX(render, BZ0, BX0, -14f, FFL, RY, t);
        WallX(render, BZ0, 14f, BX1, FFL, RY, t);
        GlassRun(-14f, 14f, BZ0, FFL, CY + .6f, new[] { (-3.2f, 3.2f) });
        BL(black, -14f, 14f, CY + .6f, RY, BZ0 - t * .5f, BZ0 + t * .5f, true);
        Door("AQUAPARK wejście główne", -1.85f, BZ0, 0f, 2.7f, 2.6f, true);
        Door("AQUAPARK wejście główne 2", 1.85f, BZ0, 0f, 2.7f, 2.6f, true);
        BL(black, -.5f, .5f, FFL, FFL + 2.6f, BZ0 - .09f, BZ0 + .09f, true);
        for (float x = BX0 + 2f; x < -15f; x += 3f) BL(fin, x - .06f, x + .06f, FFL, RY, BZ0 - t * .5f - .45f, BZ0 - t * .5f, false);
        for (float x = 16f; x < BX1 - 1f; x += 3f) BL(fin, x - .06f, x + .06f, FFL, RY, BZ0 - t * .5f - .45f, BZ0 - t * .5f, false);
        // north: wings render with their doors, the lobby and the jacuzzi hall in glass (the view of the aquapark)
        WallX(render, BZ1, BX0, -14f, FFL, RY, t, (-48.0f, -46.0f, FFL + 2.6f), (-26.0f, -24.0f, FFL + 2.6f));
        GlassRun(-14f, 14f, BZ1, FFL, CY, new (float, float)[0]); BL(black, -14f, 14f, CY, RY, BZ1 - t * .5f, BZ1 + t * .5f, true);
        GlassRun(14f, 40f, BZ1, FFL, CY, new[] { (26f, 28.4f) }); BL(black, 14f, 40f, CY, RY, BZ1 - t * .5f, BZ1 + t * .5f, true);
        WallX(render, BZ1, 40f, BX1, FFL, RY, t);
        Door("SZATNIA KOBIETY wyjście na basen", -47f, BZ1, 0f, 2.0f, 2.6f, true);
        Door("SZATNIA MĘŻCZYŹNI wyjście na basen", -25f, BZ1, 0f, 2.0f, 2.6f, true);
        Door("SPA wyjście na jacuzzi", 27.2f, BZ1, 0f, 2.4f, 2.6f, true);
        // ends
        WallZ(render, BX0, BZ0, BZ1, FFL, RY, t); WallZ(render, BX1, BZ0, BZ1, FFL, RY, t);
        // attic: black band 6.6..7.2 all round, the names
        foreach (var (x0, x1, z0, z1) in new[] { (BX0 - .2f, BX1 + .2f, BZ0 - .5f, BZ0 - .1f), (BX0 - .2f, BX1 + .2f, BZ1 + .1f, BZ1 + .5f) }) BL(black, x0, x1, RY - .4f, AY, z0, z1, true);
        foreach (float x in new[] { BX0 - .2f, BX1 - .2f }) BL(black, x, x + .4f, RY - .4f, AY, BZ0 - .5f, BZ1 + .5f);
        // the name on a black crown over the entrance (above the canopy: read from the street and the bus stop), the bull
        // mark beside it, a white LED line under the letters
        BL(black, -19f, 19f, RY - .4f, AY + 2.3f, BZ0 - .70f, BZ0 + .4f, true);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "AQUAPARK BRAXHORN", V(2.2f, AY + .35f, BZ0 - .72f), 180f, 1.25f, .24f, .14f, 31f);
        Bull(V(-15.4f, AY + .95f, BZ0 - .72f), 2.4f);
        BL(led, -17.5f, 17.5f, AY + .12f, AY + .17f, BZ0 - .73f, BZ0 - .70f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "SPA & WELLNESS", V(36f, CY - .9f, BZ0 - .62f), 180f, .62f, .12f, .12f, 18f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "BASENY • ZJEŻDŻALNIE • SZATNIE", V(-36f, CY - .9f, BZ0 - .62f), 180f, .50f, .10f, .12f, 30f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "AQUAPARK BRAXHORN", V(0f, RY - .38f, BZ1 + .52f), 0f, .9f, .2f, .14f, 30f);
        // entrance canopy 28 x 8 m at 5.4 m, two black columns, downlights, LED edge
        BL(white, -14f, 14f, FFL + 5.2f, FFL + 5.25f, -104f, BZ0 - .5f);
        BL(black, -14.2f, 14.2f, FFL + 5.25f, FFL + 5.85f, -104.2f, BZ0 - .5f, false);
        BL(led, -14.22f, 14.22f, FFL + 5.30f, FFL + 5.34f, -104.22f, -104.2f);
        foreach (float x in new[] { -12.5f, 12.5f }) BL(black, x - .25f, x + .25f, DY, FFL + 5.25f, -103.6f, -103.1f, true);
        for (float x = -12f; x <= 12.1f; x += 3f) for (float z = -103f; z <= -98f; z += 2.5f) m.Cylinder(downlight, V(x, FFL + 5.19f, z), .12f, .01f, 14);
        // ---------------- interior partitions
        float ti = .15f, hy = CY;
        // corridors (z -90) with doors / turnstile openings; wings split
        WallX(renderIn, -90f, BX0, -14f, FFL, hy, ti, (-47.6f, -46.4f, FFL + 2.3f), (-25.6f, -24.4f, FFL + 2.3f));
        WallX(renderIn, -90f, 14f, BX1, FFL, hy, ti, (25.8f, 28.2f, FFL + 2.6f), (48.65f, 49.85f, FFL + 2.3f));
        Door("SZATNIA KOBIETY", -47f, -90f, 0f, 1.2f, 2.3f, false); Door("SZATNIA MĘŻCZYŹNI", -25f, -90f, 0f, 1.2f, 2.3f, false);
        Door("SPA JACUZZI", 27f, -90f, 0f, 2.4f, 2.6f, true); Door("SAUNA", 49.25f, -90f, 0f, 1.2f, 2.3f, false);
        WallZ(renderIn, -14f, BZ0, BZ1, FFL, hy, ti, (-95.4f, -91.0f, FFL + 2.8f));                   // turnstiles in the opening
        WallZ(renderIn, 14f, BZ0, BZ1, FFL, hy, ti, (-94.4f, -92.0f, FFL + 2.6f));
        Door("SPA & WELLNESS wejście", 14f, -93.2f, 90f, 2.4f, 2.6f, true);
        WallZ(renderIn, -36f, -90f, BZ1, FFL, hy, ti);
        WallZ(renderIn, 40f, -90f, BZ1, FFL, hy, ti, (-73.2f, -72.0f, FFL + 2.3f));
        Door("MASAŻ A", 40f, -72.6f, 90f, 1.2f, 2.3f, false);
        WallX(renderIn, -80f, 40f, BX1, FFL, hy, ti, (52.9f, 54.1f, FFL + 2.3f));
        Door("MASAŻ B", 53.5f, -80f, 0f, 1.2f, 2.3f, false);
        WallZ(renderIn, 49f, -80f, BZ1, FFL, hy, ti);
        // ---------------- turnstiles (3 stainless gates in the west opening)
        foreach (float z in new[] { -94.6f, -93.2f, -91.8f })
        {
            BL(steel, -14.6f, -13.4f, FFL, FFL + 1.0f, z - .12f, z + .12f, true);
            BL(glass, -13.4f, -12.9f, FFL + .35f, FFL + .95f, z - .006f, z + .006f);
        }
        Lobby(); ChangingRoom("KOBIETY", BX0, -36f); ChangingRoom("MĘŻCZYŹNI", -36f, -14f); Spa();
    }

    // full-height glazing with black mullions every 2.4 m and a black head; gaps (doors) left open
    static void GlassRun(float x0, float x1, float z, float y0, float y1, (float a, float b)[] gaps)
    {
        float x = x0;
        var cuts = gaps.OrderBy(g => g.a).ToList(); cuts.Add((x1, x1));
        foreach (var g in cuts)
        {
            if (g.a - x > .05f)
            {
                BL(glass, x, g.a, y0 + .1f, y1 - .1f, z - .012f, z + .012f); Col(CX + x, CX + g.a, y0, y1, CZ + z - .1f, CZ + z + .1f, "szkło");
                int n = Mathf.Max(1, Mathf.RoundToInt((g.a - x) / 2.4f));
                for (int i = 0; i <= n; i++) { float mx = Mathf.Lerp(x, g.a, i / (float)n); BL(black, mx - .05f, mx + .05f, y0, y1, z - .09f, z + .09f); }
                BL(black, x, g.a, y0, y0 + .1f, z - .06f, z + .06f); BL(black, x, g.a, y1 - .1f, y1, z - .06f, z + .06f);
            }
            if (g.b > g.a) BL(black, g.a, g.b, FFL + 2.6f, y1, z - .09f, z + .09f);              // the transom over a door
            x = g.b;
        }
    }

    static void Lobby()
    {
        // reception: a curved white quartz desk with a LED line, the black marble wall with the bull mark behind it
        var c = V(0f, FFL, -83f);
        var outer = new List<Vector2>(); var inner = new List<Vector2>();
        for (int k = 0; k <= 24; k++) { float a = Mathf.Lerp(200f, 340f, k / 24f) * Mathf.Deg2Rad; outer.Add(new Vector2(c.x + Mathf.Cos(a) * 6.0f, c.z + 2.4f + Mathf.Sin(a) * 3.0f)); inner.Add(new Vector2(c.x + Mathf.Cos(a) * 5.3f, c.z + 2.4f + Mathf.Sin(a) * 2.4f)); }
        for (int k = 0; k + 1 < outer.Count; k++)
        {
            var o0 = outer[k]; var o1 = outer[k + 1]; var i0 = inner[k]; var i1 = inner[k + 1];
            Q(m, quartz, new Vector3(o0.x, FFL, o0.y), new Vector3(o1.x, FFL, o1.y), new Vector3(o1.x, FFL + 1.1f, o1.y), new Vector3(o0.x, FFL + 1.1f, o0.y), new Vector3(o0.x + o1.x - 2 * c.x, 0, o0.y + o1.y - 2 * (c.z + 2.4f)));
            Q(m, quartz, new Vector3(o0.x, FFL + 1.1f, o0.y), new Vector3(o1.x, FFL + 1.1f, o1.y), new Vector3(i1.x, FFL + 1.1f, i1.y), new Vector3(i0.x, FFL + 1.1f, i0.y), Vector3.up);
            Q(m, quartz, new Vector3(i0.x, FFL, i0.y), new Vector3(i1.x, FFL, i1.y), new Vector3(i1.x, FFL + 1.1f, i1.y), new Vector3(i0.x, FFL + 1.1f, i0.y), -new Vector3(o0.x + o1.x - 2 * c.x, 0, o0.y + o1.y - 2 * (c.z + 2.4f)));
            Q(m, led, new Vector3(o0.x, FFL + .08f, o0.y) * 1f, new Vector3(o1.x, FFL + .08f, o1.y), new Vector3(o1.x, FFL + .11f, o1.y), new Vector3(o0.x, FFL + .11f, o0.y), new Vector3(o0.x + o1.x - 2 * c.x, 0, o0.y + o1.y - 2 * (c.z + 2.4f)) );
        }
        Col(c.x - 6f, c.x + 6f, FFL, FFL + 1.1f, c.z - .7f, c.z + .4f, "recepcja");
        BL(marbleBlack, -8f, 8f, FFL, CY, -77.6f, -77.2f, true);
        Bull(V(0f, FFL + 2.6f, -77.62f), 2.2f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "AQUAPARK BRAXHORN", V(0f, FFL + 1.15f, -77.63f), 180f, .30f, .05f, .12f, 7f);
        // sofas by the glass (graphite leather), planters with palms in the corners
        foreach (float x in new[] { -9f, 9f })
        {
            var leather = BraxChampionsFoundation.LibraryLeather("aq_lobby_sofa_leather", new Color(.42f, .43f, .46f));
            m.RoundedBox(leather, V(x, FFL + .22f, -68.5f), new Vector3(3.2f, .44f, .95f), .08f, Quaternion.identity, 2);
            m.RoundedBox(leather, V(x, FFL + .55f, -68.0f), new Vector3(3.2f, .62f, .22f), .08f, Quaternion.identity, 2);
            Col(CX + x - 1.6f, CX + x + 1.6f, FFL, FFL + .8f, CZ - 69f, CZ - 67.9f, "sofa");
            for (int k = 0; k < 3; k++)
            {
                var g = new GameObject("sofa lobby " + x + " " + k); g.transform.SetParent(seats, false); g.transform.localPosition = V(x - 1.0f + k * 1.0f, FFL + .44f, -68.6f) * K;
                g.transform.localRotation = Quaternion.LookRotation(Vector3.back); var s = g.AddComponent<BraxSeat>(); s.label = "SOFA"; s.surfaceY = (FFL + .44f) * K; Seats++;
            }
        }
    }

    // a changing room x0..x1, z -90..-64: lockers on both side walls, benches, showers on the north wall each side of the
    // door, vanities with a mirror on the south wall, two WC cubicles in the south-east corner
    static void ChangingRoom(string name, float x0, float x1)
    {
        float zc0 = -90f, zc1 = BZ1;
        foreach (float wx in new[] { x0 + .15f, x1 - .65f })
        {
            BL(lockerMat, wx, wx + .5f, FFL, FFL + 2.0f, -86f, -70f, true);
            for (float z = -86f; z < -70.01f; z += .5f)
                for (int tier = 0; tier < 2; tier++)
                {
                    float y0 = FFL + .08f + tier * .97f;
                    float fx = wx < (x0 + x1) * .5f ? wx + .5f : wx;
                    BL(lockerMat, fx - .01f, fx + .01f, y0, y0 + .92f, z + .02f, z + .48f);
                    BL(plate, fx - .015f, fx + .015f, y0 + .70f, y0 + .78f, z + .18f, z + .32f);
                }
        }
        float mx = (x0 + x1) * .5f;
        // a double-sided locker island in the middle, benches either side of it
        BL(lockerMat, mx - .5f, mx + .5f, FFL, FFL + 1.75f, -86f, -78f, true);
        for (float z = -86f; z < -78.01f; z += .5f)
            foreach (float fx in new[] { mx - .5f, mx + .5f })
            {
                BL(lockerMat, fx - .01f, fx + .01f, FFL + .08f, FFL + 1.68f, z + .02f, z + .48f);
                BL(plate, fx - .015f, fx + .015f, FFL + 1.40f, FFL + 1.48f, z + .18f, z + .32f);
            }
        foreach (float dx in new[] { -2.3f, 2.3f }) foreach (float z in new[] { -84f, -80f, -75f })
            Owner("lawka_szatnia", V(mx + dx, FFL, z), 0f, true);
        // showers: four owner cabins on the north wall (two each side of the door to the deck)
        float door = mx;
        foreach (float dx in new[] { -6.2f, -4.6f, 4.6f, 6.2f }) Owner("prysznic_kabina", V(door + dx, FFL, BZ1 - .75f), 180f, true);
        // vanities and the mirror on the south wall, east of the corridor door
        foreach (float dx in new[] { 3.4f, 5.4f }) Owner("umywalka_podwojna", V(door + dx, FFL, zc0 + .45f), 0f, true);
        BL(mirror, door + 2.5f, door + 6.3f, FFL + 1.25f, FFL + 2.25f, zc0 + .08f, zc0 + .1f);
        // WC cubicles (south-west corner): HPL partitions, a wall-hung white pan, a door each
        for (int k = 0; k < 2; k++)
        {
            float cx0 = x0 + 1.0f + k * 1.6f, cx1 = cx0 + 1.6f;
            BL(lockerMat, cx1 - .04f, cx1, FFL + .15f, FFL + 2.1f, zc0, zc0 + 1.8f, true);
            BL(lockerMat, cx0, cx1, FFL + .15f, FFL + 2.1f, zc0 + 1.76f, zc0 + 1.8f);
            m.RoundedBox(white, V(cx0 + .8f, FFL + .40f, zc0 + .35f), new Vector3(.38f, .32f, .55f), .12f, Quaternion.identity, 3);
        }
        BL(lockerMat, x0, x0 + .04f, FFL + .15f, FFL + 2.1f, zc0, zc0 + 1.8f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "SZATNIA " + name, V(door, FFL + 2.7f, -90.09f), 180f, .22f, .03f, .1f, 6f);
    }

    static void Spa()
    {
        // JACUZZI HALL: the owner's four hot tubs in a 2 x 2 grid on white marble, loungers at the glass
        var tubs = new[] { (20.5f, -84.5f), (33.5f, -84.5f), (20.5f, -77.0f), (33.5f, -77.0f) };
        foreach (var (x, z) in tubs)
        {
            var o = Owner("jacuzzi_wanna", V(x, FFL, z), 0f, true);
            if (!o) continue;
            // the model's own flat water hidden, the region's bubbling water at its level; four places to sit in
            float wy = FFL + .77f; Vector3 wc = V(x, 0, z);
            foreach (var r in o.GetComponentsInChildren<MeshRenderer>(true))
                if (r.name.ToLowerInvariant().Contains("water")) { var bd = r.bounds; wy = bd.center.y / K; wc = bd.center / K; r.enabled = false; }
            var pool = Ccw(RoundRect(wc.x - .96f, wc.x + .96f, wc.z - .96f, wc.z + .96f, .2f, .3f));
            Fill(m, waterSpa, pool, null, wy);
            foreach (var (dx, dz, yaw) in new[] { (-.6f, -.6f, 45f), (.6f, -.6f, -45f), (-.6f, .6f, 135f), (.6f, .6f, -135f) })
            {
                var g = new GameObject("jacuzzi SPA miejsce"); g.transform.SetParent(seats, false); g.transform.localPosition = new Vector3(wc.x + dx, wy - .32f, wc.z + dz) * K;
                g.transform.localRotation = Quaternion.Euler(0, yaw, 0); var s = g.AddComponent<BraxSeat>(); s.label = "JACUZZI"; s.surfaceY = (wy - .32f) * K; Seats++;
            }
        }
        for (float x = 16f; x <= 38.1f; x += 2.2f) Lounger(V(x, FFL, -68.5f), 180f);
        // the black marble wall with the bull mark and the name, opposite the glass
        BL(marbleBlack, 14.08f, 14.32f, FFL, CY, -88.5f, -70.5f, true);
        Bull(V(14.33f, FFL + 2.45f, -79.5f), 2.4f, -90f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "SPA & WELLNESS", V(14.34f, FFL + 1.05f, -79.5f), 90f, .34f, .05f, .12f, 8f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "JACUZZI", V(27f, FFL + 2.95f, -90.09f), 180f, .26f, .04f, .1f, 4f);
        // SAUNA room: the owner's two cabins, a bench, the cold shower
        Owner("sauna_kabina", V(43.5f, FFL, -88.6f), 0f, true);
        Owner("sauna_kabina", V(46.4f, FFL, -88.6f), 0f, true);
        Owner("prysznic_kabina", V(56.6f, FFL, -88.8f), 0f, true);
        Owner("lawka_szatnia", V(50f, FFL, -82.8f), 90f, true);
        // MASSAGE A / B: the owner's tables (you lie on them), a cabinet, warm light
        Massage("stol_masaz_drewno", V(44.5f, FFL, -76f), 0f, .74f);
        Massage("stol_masaz_skladany_czarny", V(44.5f, FFL, -69f), 0f, .80f);
        Massage("stol_masaz_kosmetyczny", V(53.5f, FFL, -70f), 90f, .70f);
        Massage("lezanka_masaz_tajski", V(55.2f, FFL, -75.6f), 0f, .40f);
        Massage("stol_masaz_skladany_zielony", V(51.6f, FFL, -75.6f), 0f, .80f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "SAUNA", V(49.25f, FFL + 2.7f, -90.09f), 180f, .22f, .03f, .1f, 3f);
        // the massage rooms: a black marble wall with a warm LED line, flowers, a white cabinet with rolled towels
        foreach (var (x0, x1) in new[] { (40f, 49f), (49f, 58f) })
        {
            BL(marbleBlack, x0 + .4f, x1 - .4f, FFL, CY - .5f, BZ1 - .45f, BZ1 - .16f, true);
            BL(led, x0 + .4f, x1 - .4f, CY - .52f, CY - .48f, BZ1 - .48f, BZ1 - .45f);
            BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x0 + 1.1f, CZ + BZ1 - 1.3f, "flower_gazania");
            BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x1 - 1.1f, CZ + BZ1 - 1.3f, "flower_empodium");
            BL(white, x0 + .3f, x0 + 1.5f, FFL, FFL + .86f, -79.6f, -79.0f, true);
            for (int k = 0; k < 3; k++) m.Cylinder(towel, V(x0 + .55f + k * .35f, FFL + .98f, -79.55f), .11f, .5f, 14, Quaternion.Euler(90f, 0, 0), true, true);
        }
        foreach (float x in new[] { 52f, 55f }) BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x, CZ - 81.4f, "flower_empodium");
    }

    static void Massage(string plik, Vector3 at, float yaw, float top)
    {
        var o = Owner(plik, at, yaw, true); if (!o) return;
        var g = new GameObject("masaż " + plik); g.transform.SetParent(seats, false); g.transform.localPosition = new Vector3(at.x, at.y + top, at.z) * K;
        g.transform.localRotation = Quaternion.Euler(0, yaw, 0); var s = g.AddComponent<BraxSeat>(); s.lie = true; s.label = "STÓŁ DO MASAŻU"; s.surfaceY = (at.y + top) * K; Seats++;
    }
}
