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
    static Material render, renderIn, terrazzo, wetTile, marbleBlack, marbleWhite, quartz, ceiling, frosted, lockerMat, plate, mirror, downlight, fin,
        doorWalnut, frameSteel;
    static int doors;

    static Material Inside(Material x, float a) { BraxChampionsInteriorLight.Apply(x, a); return x; }

    static void BuildingMaterials()
    {
        // 16.11 08:03 owner "budynek ma być premium ... na zewnątrz": the wings in large pale limestone panels (Poly Haven
        // floor_tiles_02 at its real 4 m: ~1 m panels, joints, tone changes), neutral light, instead of a white render
        render = BraxChampionsFoundation.Fotoskan("aq_pavilion_limestone_cladding", "floor_tiles_02", BraxChampionsFoundation.FloorTiles02Metres,
            new Color(226f / 255f, 224f / 255f, 219f / 255f), BraxChampionsFoundation.MeanFloorTiles02);
        BraxChampionsInteriorLight.Facade(render);
        renderIn = Inside(Tex("pavilion_inside_render", BraxChampionsFoundation.InteriorPaintSlug, 3.0f, BraxChampionsFoundation.PaintTint(new Color(1.04f, 1.04f, 1.06f))), 1.05f);
        // 16.11 07:32 owner "nie widzę podłogi wykończonej": the fine terrazzo read as a plain grey floor. The lobby is now
        // limestone slabs (Poly Haven marble_01 at its real 1.5 m: joints and slab tones visible), a light warm stone.
        terrazzo = Inside(BraxChampionsFoundation.Fotoskan("aq_lobby_limestone_slabs", "marble_01", BraxChampionsFoundation.Marble01Metres,
            new Color(222f / 255f, 219f / 255f, 212f / 255f), BraxChampionsFoundation.MeanMarble01), 1.0f);   // neutral: no beige (region rule)
        // the changing rooms' floor: large slate-grey porcelain squares (Poly Haven large_floor_tiles_02, real 3 m: ~1 m tiles,
        // joints and tone changes) - the grey anti-slip mosaic read as a bare concrete floor (owner's picture 16.11 08:10)
        wetTile = Inside(BraxChampionsFoundation.Fotoskan("aq_changing_grey_floor_tiles", "large_floor_tiles_02", 3.0f,
            new Color(150f / 255f, 151f / 255f, 148f / 255f), BraxChampionsFoundation.MeanLargeFloorTiles02), 1.0f);
        marbleBlack = Inside(BraxChampionsFoundation.LibraryMarble("aq_lobby_black_marble", new Color(1f, 1f, 1f), 1.8f), 1.0f);
        marbleWhite = Inside(BraxChampionsFoundation.LibraryMarbleWhite("aq_spa_white_marble", new Color(1f, 1f, 1f), 1.6f), 1.0f);
        quartz = Inside(S("reception_white_quartz", new Color(.95f, .95f, .96f), .18f, 0f), 1.0f);
        // 16.11 owner "basen masz skończyć premium ... Poly Haven": the ceiling in the interior paint (Poly Haven), no plain colour
        ceiling = Inside(Tex("pavilion_ceiling_white", BraxChampionsFoundation.InteriorPaintSlug, 3.0f, BraxChampionsFoundation.PaintTint(new Color(1.06f, 1.06f, 1.08f))), 1.1f);
        frosted = S("pavilion_frosted_glass", new Color(.90f, .94f, .96f, .62f), .3f, 0f, null, true);
        // lockers / WC partitions: graphite brushed metal carcass (was a plain grey colour), walnut veneer doors (Finish)
        lockerMat = Inside(BraxChampionsFoundation.LibraryMetal("aq_locker_graphite_brushed", new Color(.20f, .21f, .23f)), 1.0f);
        plate = S("locker_number_plate", new Color(.85f, .86f, .88f), .25f, .8f);
        mirror = S("vanity_mirror", new Color(.78f, .82f, .86f), .03f, 1f);
        downlight = S("pavilion_downlight", new Color(1f, .96f, .88f), .3f, 0f, new Color(1.25f, 1.18f, 1.02f));
        fin = BraxChampionsFoundation.LibraryMetal("aq_pavilion_black_fin_brushed", new Color(.07f, .07f, .075f));      // black brushed steel
        // 16.11 owner "każdy detal ma być wykończony": the solid doors were plain painted slabs in a bare plaster hole. Now flush
        // walnut veneer leaves (Poly Haven smoked_walnut_veneer, real 1 m) in black brushed-steel casings, long black pulls
        doorWalnut = Inside(Tex("door_walnut_veneer", "smoked_walnut_veneer", 1.0f, new Color(.92f, .92f, .92f)), 1.0f);
        frameSteel = Inside(BraxChampionsFoundation.LibraryMetal("aq_door_frame_black_steel", new Color(.10f, .10f, .11f)), 1.0f);
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
                lm.BoxMinMax(doorWalnut, a0, a1, .02f, height - .02f, -.025f, .025f);
                // a long black pull on both faces (60 cm, two stand-offs), 10 cm from the closing edge
                float px = side < 0 ? a1 - .10f : a0 + .10f;
                foreach (float f in new[] { -1f, 1f })
                {
                    float z0 = f > 0 ? .065f : -.085f, z1 = z0 + .02f;
                    lm.BoxMinMax(frameSteel, px - .012f, px + .012f, .75f, 1.35f, z0, z1);
                    foreach (float y in new[] { .82f, 1.28f }) lm.BoxMinMax(frameSteel, px - .008f, px + .008f, y - .008f, y + .008f, f > 0 ? .025f : -.065f, f > 0 ? .065f : -.025f);
                }
            }
            lm.Build(hinge, label + " skrzydło " + (i + 1), Folder, false);
            BraxChampionsMeshBatch.Collider(hinge, "skrzydło kolizja", new Vector3((a0 + a1) * .5f, height * .5f, 0), new Vector3(a1 - a0, height, .08f));
            foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            leaves[i] = hinge; sides[i] = n == 1 ? 1f : (i == 0 ? 1f : -1f);
        }
        var d = group.gameObject.AddComponent<BraxCityDoor>(); d.id = "aquapark_" + (++doors); d.label = label; d.leaves = leaves; d.sides = sides;
        if (!glassLeaf) Casing(lx, lz, yaw, width, height);
    }

    static int gateCount;
    // a speed-gate passage: two glass wings hinged at both sides (local x across the passage, local z along it = world x)
    static void GateWings(string label, float lx, float lz, float width, Material inox)
    {
        var group = Group(root, label + " | skrzydła szklane (otwierają się przy podejściu)");
        group.localPosition = V(lx, FFL, lz) * K; group.localRotation = Quaternion.Euler(0, 90f, 0);
        int n = width > .9f ? 2 : 1; var leaves = new Transform[n]; var sides = new float[n];
        for (int i = 0; i < n; i++)
        {
            float side = n == 1 ? -1f : (i == 0 ? -1f : 1f), lw = width / n - .02f;
            var hinge = Group(group, "skrzydło " + (i + 1)); hinge.localPosition = new Vector3(side * width * .5f, 0, 0) * K;
            float a0 = side < 0 ? .015f : -lw, a1 = side < 0 ? lw : -.015f;
            var lm = new BraxChampionsMeshBatch();
            lm.BoxMinMax(glass, a0, a1, .25f, 1.6f, -.008f, .008f);
            lm.BoxMinMax(inox, a0, a1, 1.6f, 1.625f, -.012f, .012f);
            float hx = side < 0 ? a0 : a1;
            lm.BoxMinMax(inox, hx - .02f, hx + .02f, .2f, 1.65f, -.02f, .02f);
            lm.Build(hinge, label + " skrzydło " + (i + 1), Folder, false);
            BraxChampionsMeshBatch.Collider(hinge, "skrzydło kolizja", new Vector3((a0 + a1) * .5f, .9f, 0), new Vector3(a1 - a0, 1.8f, .05f));
            foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            leaves[i] = hinge; sides[i] = n == 1 ? 1f : (i == 0 ? 1f : -1f);
        }
        var d = group.gameObject.AddComponent<BraxCityDoor>(); d.id = "aquapark_bramka_" + gateCount; d.label = label; d.leaves = leaves; d.sides = sides;
    }

    // the casing of a solid door in a 15 cm partition: 8 mm steel linings over the plaster reveals (jambs and head), 7 cm
    // architraves 2 cm proud on both faces (flush with the timber panelling, which stops 10 cm from the opening)
    static void Casing(float lx, float lz, float yaw, float w, float h)
    {
        var r = Quaternion.Euler(0, yaw, 0); var o = V(lx, FFL, lz); float hw = w * .5f;
        void Bx(float x0, float x1, float y0, float y1, float z0, float z1) =>
            m.Box(frameSteel, o + r * new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f), new Vector3(x1 - x0, y1 - y0, z1 - z0), r);
        foreach (float f in new[] { -1f, 1f })
        {
            float z0 = f > 0 ? .075f : -.095f, z1 = z0 + .02f;
            Bx(-hw - .07f, -hw, 0f, h + .07f, z0, z1); Bx(hw, hw + .07f, 0f, h + .07f, z0, z1); Bx(-hw, hw, h, h + .07f, z0, z1);
        }
        Bx(-hw, -hw + .008f, 0f, h, -.075f, .075f); Bx(hw - .008f, hw, 0f, h, -.075f, .075f); Bx(-hw, hw, h - .008f, h, -.075f, .075f);
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
        var roofMat = BraxChampionsFoundation.Fotoskan("pavilion_roof_light_membrane", "brushed_concrete", 2.5f, new Color(.66f, .68f, .71f), BraxChampionsFoundation.MeanBrushedConcrete); // 16.11 Poly Haven ("każda podłoga, ściana, sufit")
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
        GlassRun(-14f, 14f, BZ1, FFL, CY, new[] { (-1.25f, 1.25f) }); BL(black, -14f, 14f, CY, RY, BZ1 - t * .5f, BZ1 + t * .5f, true);
        Door("RESTAURACJA LAGUNA wyjście na taras", 0f, BZ1, 0f, 2.5f, 2.6f, true);
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
        // the canopy's soffit in walnut slats (as the lobby inside: the ceiling runs out through the glass), downlights under it
        var soffit = Tex("canopy_walnut_slats", "smoked_walnut_veneer", 1.0f, Color.white);
        BraxChampionsInteriorLight.Facade(soffit);
        for (float x = -13.94f; x + .06f < 14f; x += .15f) BL(soffit, x, x + .06f, FFL + 5.10f, FFL + 5.18f, -103.9f, BZ0 - .55f);
        for (float x = -12f; x <= 12.1f; x += 3f) for (float z = -103f; z <= -98f; z += 2.5f) m.Cylinder(downlight, V(x, FFL + 5.085f, z), .12f, .01f, 14);
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
        Finish(ti);
        // ---------------- turnstiles (3 stainless gates in the west opening)
        // SPEED GATES (owner 16.11 08:25 "co to są za tandetne bramki"): brushed stainless cabinets with rounded ends on a black
        // granite plinth, a black glass top with the card reader and green arrows lit
        var gateTop = S("gate_black_glass", new Color(.02f, .02f, .025f, .92f), .05f, 0f, null, true);
        var arrow = S("gate_arrow_green", new Color(.10f, .85f, .35f), .3f, 0f, new Color(.25f, 1.6f, .55f));
        var inox = Inside(BraxChampionsFoundation.LibraryMetal("aq_gate_brushed_inox", new Color(.86f, .87f, .89f)), 1.0f);
        BL(marbleBlack, -14.75f, -13.25f, FFL, FFL + .02f, -95.35f, -91.05f);
        foreach (float z in new[] { -94.6f, -93.2f, -91.8f })
        {
            BL(inox, -14.5f, -13.5f, FFL + .02f, FFL + 1.0f, z - .1f, z + .1f, true);
            foreach (float ex in new[] { -14.5f, -13.5f }) m.Cylinder(inox, V(ex, FFL + .02f, z), .1f, .98f, 20);
            BL(gateTop, -14.5f, -13.5f, FFL + 1.0f, FFL + 1.03f, z - .1f, z + .1f);
            foreach (float ex in new[] { -14.5f, -13.5f }) m.Cylinder(gateTop, V(ex, FFL + 1.0f, z), .1f, .03f, 20);
            BL(black, -14.38f, -14.18f, FFL + 1.03f, FFL + 1.045f, z - .06f, z + .06f);                    // card reader
            foreach (float ax in new[] { -13.95f, -13.75f }) BL(arrow, ax - .04f, ax + .04f, FFL + 1.03f, FFL + 1.034f, z - .015f, z + .015f);
        }
        // the wings WORK (owner 08:46 "te bramki nadal są spierdolone" - the glass you walked through): in each of the four
        // passages two frameless glass wings 1.6 m on stainless hinge posts at the cabinets, a real door (BraxCityDoor): they
        // swing open as you walk up to them, close by themselves behind you, and stop you while closed
        foreach (var (z0, z1) in new[] { (-95.35f, -94.7f), (-94.5f, -93.3f), (-93.1f, -91.9f), (-91.7f, -91.05f) })
            GateWings("BRAMKA WEJŚCIA " + (++gateCount), -14f, (z0 + z1) * .5f, z1 - z0, inox);
        Lobby(); ChangingRoom("KOBIETY", BX0, -36f); ChangingRoom("MĘŻCZYŹNI", -36f, -14f); Spa();
        Restaurant(); Lounges();                                                                 // 16.11 08:13 "bogate wyposażenie"
        Wellness();                                                                              // 16.11 08:25 saunas, jacuzzi, pictures, plants
    }

    // full-height glazing with black mullions every 2.4 m and a black head; gaps (doors) left open
    // PREMIUM FINISH (owner 16.11 01:36-01:41 "basen masz skończyć premium ... Poly Haven ... wszystko sprawdzasz, co
    // trzeba wykończyć"; the 16.11 preview: grey painted walls and a flat white ceiling over 116 m of rooms). All Poly Haven:
    //   - the LOBBY and the SPA wing under a walnut slat ceiling: 6 x 6 cm slats every 15 cm on black acoustic felt (hessian),
    //     warm downlights hanging just under the slats;
    //   - the LOBBY's side walls, the JACUZZI HALL, the SAUNA room and the MASSAGE rooms panelled in timber up to the slats
    //     (walnut; oak in the sauna room), 2 cm proud of the plaster, the door openings left free;
    //   - the CHANGING ROOMS' walls tiled (long white porcelain) to 2.4 m, 12 mm proud;
    //   - the SAUNA room's empty half: three loungers to rest after the sauna (you lie on them), plants.
    static void Finish(float ti)
    {
        // every map at its real size (Poly Haven dimensions: walnut veneer 1.0 m, hessian .27 m, oak planks 1.2 m, wall tiles
        // 1.27 m, wool .27 m)
        var slat = Inside(Tex("ceiling_walnut_slats", "smoked_walnut_veneer", 1.0f, Color.white), 1.0f);
        var felt = Inside(Tex("ceiling_black_felt", "hessian_230", .27f, new Color(.10f, .10f, .11f)), 1.0f);
        var walnut = Inside(Tex("spa_walnut_panels", "smoked_walnut_veneer", 1.0f, Color.white), 1.0f);
        var oak = Inside(Tex("sauna_oak_panels", "oak_wood_planks", 1.2f, Color.white), 1.0f);
        // the changing rooms' tiles a neutral white (the photo is cream: no beige - the region's rule), grout lines kept
        var tiles = Inside(BraxChampionsFoundation.Fotoskan("aq_changing_white_wall_tiles", "long_white_tiles", 1.27f,
            new Color(228f / 255f, 228f / 255f, 226f / 255f), BraxChampionsFoundation.MeanLongWhiteTiles), 1.0f);
        // slat ceilings (the felt 3 cm under the plaster, the slats under the felt)
        // 16.11 08:03 "w środku wykończony": the corridors and the changing rooms had a flat painted ceiling - now the whole
        // pavilion is under the walnut slats
        foreach (var (x0, x1, z0, z1) in new[] { (-13.9f, 13.9f, BZ0 + .2f, BZ1 - .2f), (14.1f, BX1 - .2f, -89.9f, BZ1 - .2f),
            (BX0 + .2f, -14.1f, BZ0 + .2f, -90.1f), (14.1f, BX1 - .2f, BZ0 + .2f, -90.1f), (BX0 + .2f, -36.1f, -89.9f, BZ1 - .2f), (-35.9f, -14.1f, -89.9f, BZ1 - .2f) })
        {
            BL(felt, x0, x1, CY - .04f, CY - .012f, z0, z1);
            for (float x = x0 + .06f; x + .06f < x1; x += .15f) BL(slat, x, x + .06f, CY - .10f, CY - .04f, z0, z1);
            for (float x = x0 + 2f; x < x1 - 1f; x += 4f) for (float z = z0 + 2f; z < z1 - 1f; z += 4f) m.Cylinder(downlight, V(x, CY - .125f, z), .09f, .012f, 14);
        }
        // EVERY WALL FACE FINISHED (owner 16.11 08:10 on the pictures: "ja tam widzę jeszcze poprawki" - raw grey wall showed at
        // every corner, over every door and on the walls nobody had panelled): timber / tiles 2 cm proud of each face, from wall
        // to wall (the x-runs reach the faces of the walls they meet, the z-runs start on the x-runs' faces: closed corners),
        // and over every opening up to the slats (h = the opening's head, + 7 cm casing on the solid doors). A 2 cm shadow gap
        // at the floor. Interior partitions 15 cm (faces +-.075), the envelope 30 cm (faces +-.15).
        float top = CY - .10f, e = ti * .5f, eo = .15f, d = .02f, sd = FFL + 2.37f, gd = FFL + 2.6f;
        void PanelX(Material mat, float z, float side, float half, float x0, float x1, params (float a, float b, float h)[] gaps)
        {
            float zf = z + side * half, za = Mathf.Min(zf, zf + side * d), zb = Mathf.Max(zf, zf + side * d), x = x0;
            foreach (var g in gaps.OrderBy(q => q.a))
            {
                if (g.a - x > .02f) BL(mat, x, g.a, FFL + .02f, top, za, zb);
                if (g.h > 0f && top - g.h > .02f) BL(mat, g.a, g.b, g.h, top, za, zb);
                x = g.b;
            }
            if (x1 - x > .02f) BL(mat, x, x1, FFL + .02f, top, za, zb);
        }
        void PanelZ(Material mat, float x, float side, float half, float z0, float z1, params (float a, float b, float h)[] gaps)
        {
            float xf = x + side * half, xa = Mathf.Min(xf, xf + side * d), xb = Mathf.Max(xf, xf + side * d), z = z0;
            foreach (var g in gaps.OrderBy(q => q.a))
            {
                if (g.a - z > .02f) BL(mat, xa, xb, FFL + .02f, top, z, g.a);
                if (g.h > 0f && top - g.h > .02f) BL(mat, xa, xb, g.h, top, g.a, g.b);
                z = g.b;
            }
            if (z1 - z > .02f) BL(mat, xa, xb, FFL + .02f, top, z, z1);
        }
        // the reveals of the open passages (turnstiles x -14, glass doors to the SPA / jacuzzi): lined, no raw plaster inside
        void RevealZ(Material mat, float x, float z0, float z1, float h)
        {
            BL(mat, x - e, x + e, FFL, h, z0, z0 + .008f); BL(mat, x - e, x + e, FFL, h, z1 - .008f, z1); BL(mat, x - e, x + e, h - .008f, h, z0, z1);
        }
        void RevealX(Material mat, float z, float x0, float x1, float h)
        {
            BL(mat, x0, x0 + .008f, FFL, h, z - e, z + e); BL(mat, x1 - .008f, x1, FFL, h, z - e, z + e); BL(mat, x0, x1, h - .008f, h, z - e, z + e);
        }
        float zIn = -90f + e + d, zOut = -90f - e - d;                                              // the z -90 runs' faces
        // LOBBY x -14..14: walnut on both side walls (the turnstiles' opening, the SPA door)
        PanelZ(walnut, -14f, +1f, e, BZ0 + eo, BZ1 - eo, (-95.4f, -91.0f, FFL + 2.8f));
        PanelZ(walnut, 14f, -1f, e, BZ0 + eo, BZ1 - eo, (-94.4f, -92.0f, gd));
        RevealZ(walnut, -14f, -95.4f, -91.0f, FFL + 2.8f); RevealZ(frameSteel, 14f, -94.4f, -92.0f, gd);
        // CORRIDORS z -96..-90 (walnut): the corridor wall, the street wall's inside, both end walls, the lobby walls' backs
        PanelX(walnut, -90f, -1f, e, BX0 + eo, -14f - e, (-47.67f, -46.33f, sd), (-25.67f, -24.33f, sd));
        PanelX(walnut, -90f, -1f, e, 14f + e, BX1 - eo, (25.8f, 28.2f, gd), (48.58f, 49.92f, sd));
        RevealX(frameSteel, -90f, 25.8f, 28.2f, gd);
        PanelX(walnut, BZ0, +1f, eo, BX0 + eo, -14f - e); PanelX(walnut, BZ0, +1f, eo, 14f + e, BX1 - eo);
        PanelZ(walnut, BX0, +1f, eo, BZ0 + eo + d, zOut); PanelZ(walnut, BX1, -1f, eo, BZ0 + eo + d, zOut);
        PanelZ(walnut, -14f, -1f, e, BZ0 + eo + d, zOut, (-95.4f, -91.0f, FFL + 2.8f));
        PanelZ(walnut, 14f, +1f, e, BZ0 + eo + d, zOut, (-94.4f, -92.0f, gd));
        // CHANGING ROOMS x -58..-36 / -36..-14 (white tiles floor to slats, every wall; the doors to the corridor and the deck)
        PanelX(tiles, -90f, +1f, e, BX0 + eo, -14f - e, (-47.67f, -46.33f, sd), (-36f - e, -36f + e, 0f), (-25.67f, -24.33f, sd));
        PanelX(tiles, BZ1, -1f, eo, BX0 + eo, -14f - e, (-48.0f, -46.0f, gd), (-36f - e, -36f + e, 0f), (-26.0f, -24.0f, gd));
        PanelZ(tiles, BX0, +1f, eo, zIn, BZ1 - eo - d);
        PanelZ(tiles, -36f, -1f, e, zIn, BZ1 - eo - d); PanelZ(tiles, -36f, +1f, e, zIn, BZ1 - eo - d);
        PanelZ(tiles, -14f, -1f, e, zIn, BZ1 - eo - d);
        // JACUZZI HALL x 14..40 (walnut; the glass to the deck north)
        PanelX(walnut, -90f, +1f, e, 14f + e, 40f - e, (25.8f, 28.2f, gd));
        PanelZ(walnut, 14f, +1f, e, zIn, BZ1 - eo);
        PanelZ(walnut, 40f, -1f, e, zIn, BZ1 - eo, (-73.27f, -71.93f, sd));
        // SAUNA ROOM x 40..58, z -90..-80 (oak all round)
        PanelX(oak, -90f, +1f, e, 40f + e, BX1 - eo, (48.58f, 49.92f, sd));
        PanelX(oak, -80f, -1f, e, 40f + e, BX1 - eo, (52.83f, 54.17f, sd));
        PanelZ(oak, 40f, +1f, e, zIn, -80f - e - d); PanelZ(oak, BX1, -1f, eo, zIn, -80f - e - d);
        // MASSAGE A x 40..49 / B x 49..58, z -80..-64 (walnut all round; the black marble wall stands in front of the north one)
        PanelX(walnut, -80f, +1f, e, 40f + e, 49f - e); PanelX(walnut, -80f, +1f, e, 49f + e, BX1 - eo, (52.83f, 54.17f, sd));
        PanelX(walnut, BZ1, -1f, eo, 40f + e, 49f - e); PanelX(walnut, BZ1, -1f, eo, 49f + e, BX1 - eo);
        float zM = -80f + e + d, zN = BZ1 - eo - d;
        PanelZ(walnut, 40f, +1f, e, zM, zN, (-73.27f, -71.93f, sd));
        PanelZ(walnut, 49f, -1f, e, zM, zN); PanelZ(walnut, 49f, +1f, e, zM, zN); PanelZ(walnut, BX1, -1f, eo, zM, zN);
        // the sauna room's rest corner: three loungers (BraxSeat lie) and two plants
        foreach (float x in new[] { 51.9f, 54.0f, 56.1f }) Lounger(V(x, FFL, -84.6f), 0f);
        BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + 41.4f, CZ - 81.0f, "flower_gazania");
        // 16.11 07:32 (the owner's picture of this room: an empty floor): the west half was bare. A second rest row of three
        // loungers facing the cabins' glass, walnut side tables with rolled towels between all loungers, and a walnut towel
        // rack by the door from the corridor (x 50.2..51.8 on the south wall: the shower at 56.6, the bench at 50 stay free)
        foreach (float x in new[] { 42.3f, 44.4f, 46.5f }) Lounger(V(x, FFL, -83.9f), 0f);
        var tableWood = Inside(Tex("spa_walnut_side_table", "smoked_walnut_veneer", 1.0f, Color.white), 1.0f);
        foreach (var (x, z) in new[] { (43.35f, -83.2f), (45.45f, -83.2f), (52.95f, -85.3f), (55.05f, -85.3f) })
        {
            BL(tableWood, x - .22f, x + .22f, FFL + .44f, FFL + .48f, z - .22f, z + .22f);
            BL(frameSteel, x - .02f, x + .02f, FFL, FFL + .44f, z - .02f, z + .02f);
            BL(frameSteel, x - .16f, x + .16f, FFL, FFL + .012f, z - .16f, z + .16f);
            m.Cylinder(towel, V(x + .14f, FFL + .535f, z), .055f, .28f, 12, Quaternion.Euler(0, 0, 90f), true, true);   // axis -x: x+.14 .. x-.14
            Col(CX + x - .22f, CX + x + .22f, FFL, FFL + .48f, CZ + z - .22f, CZ + z + .22f, "stolik");
        }
        BL(tableWood, 50.3f, 51.8f, FFL, FFL + .04f, -89.9f, -89.5f, true);
        foreach (float y in new[] { .45f, .95f, 1.45f }) BL(tableWood, 50.3f, 51.8f, FFL + y, FFL + y + .03f, -89.9f, -89.5f);
        foreach (float x in new[] { 50.3f, 51.77f }) BL(frameSteel, x, x + .03f, FFL, FFL + 1.6f, -89.9f, -89.5f);
        foreach (float y in new[] { .48f, .98f })
            for (int k = 0; k < 4; k++) m.Cylinder(towel, V(50.55f + k * .33f, FFL + y + .11f, -89.88f), .11f, .36f, 14, Quaternion.Euler(90f, 0, 0), true, true);
        // the lobby's entrance half (z -98..-86 was empty floor): two lounge corners by the side walls, off the walk from
        // the doors to the turnstiles and the SPA door - a graphite leather sofa, two armchairs, a walnut table on a wool rug
        var rug = Inside(Tex("aq_lobby_rug_wool", "poly_wool_herringbone", .27f, new Color(.55f, .56f, .60f)), 1.0f);
        foreach (float x in new[] { -9.2f, 9.2f }) BL(rug, x - 2.4f, x + 2.4f, FFL, FFL + .012f, -90.6f, -85.6f);
        // the leather sofas, armchairs and coffee tables on the rugs: Poly Haven models (BraxChampionsAquaparkRestauracja.Lounges)
        foreach (float x in new[] { -9.2f, 9.2f }) BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x, CZ - 84.9f, x < 0 ? "flower_gazania" : "flower_empodium");
    }

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
        // (the sofas by the glass gave way to RESTAURACJA LAGUNA: BraxChampionsAquaparkRestauracja)
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
                    BL(doorWalnut, fx - .01f, fx + .01f, y0, y0 + .92f, z + .02f, z + .48f);
                    BL(plate, fx - .015f, fx + .015f, y0 + .70f, y0 + .78f, z + .18f, z + .32f);
                }
        }
        float mx = (x0 + x1) * .5f;
        // a double-sided locker island in the middle, benches either side of it
        BL(lockerMat, mx - .5f, mx + .5f, FFL, FFL + 1.75f, -86f, -78f, true);
        for (float z = -86f; z < -78.01f; z += .5f)
            foreach (float fx in new[] { mx - .5f, mx + .5f })
            {
                BL(doorWalnut, fx - .01f, fx + .01f, FFL + .08f, FFL + 1.68f, z + .02f, z + .48f);
                BL(plate, fx - .015f, fx + .015f, FFL + 1.40f, FFL + 1.48f, z + .18f, z + .32f);
            }
        foreach (float dx in new[] { -2.3f, 2.3f }) foreach (float z in new[] { -84f, -80f, -75f })
            Owner("lawka_szatnia", V(mx + dx, FFL, z), 0f, true);
        // showers: four owner cabins on the north wall (two each side of the door to the deck)
        float door = mx;
        foreach (float dx in new[] { -6.2f, -4.6f, 4.6f, 6.2f })
        {
            Owner("prysznic_kabina", V(door + dx, FFL, BZ1 - .75f), 180f, false);
            ShowerWater(V(door + dx, FFL + 2.02f, BZ1 - .75f), FFL, "kabina " + name);
            // the cabin's two side walls and back are solid, its front is the door you walk in through
            foreach (float sx in new[] { -.64f, .64f }) Col(CX + door + dx + sx - .03f, CX + door + dx + sx + .03f, FFL, FFL + 2.2f, CZ + BZ1 - 1.27f, CZ + BZ1 - .2f, "kabina prysznic");
        }
        // vanities and the mirror on the south wall, east of the corridor door
        foreach (float dx in new[] { 3.4f, 5.4f }) Owner("umywalka_podwojna", V(door + dx, FFL, zc0 + .45f), 0f, true);
        BL(mirror, door + 2.5f, door + 6.3f, FFL + 1.25f, FFL + 2.25f, zc0 + .095f, zc0 + .115f);   // 1.5 cm proud of the tiles (no flicker)
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
        // JACUZZI HALL: two built raised spa pools (BraxChampionsAquaparkWellness.IndoorSpa - 16.11 08:25 "gdzie masz porobione
        // jacuzzi": the owner's four small tub models read as wooden boxes), loungers at the glass
        for (float x = 16f; x <= 38.1f; x += 2.2f) Lounger(V(x, FFL, -68.5f), 180f);
        // the black marble wall with the bull mark and the name, opposite the glass
        BL(marbleBlack, 14.08f, 14.32f, FFL, CY, -88.5f, -70.5f, true);
        Bull(V(14.33f, FFL + 2.45f, -79.5f), 2.4f, -90f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "SPA & WELLNESS", V(14.34f, FFL + 1.05f, -79.5f), 90f, .34f, .05f, .12f, 8f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "JACUZZI", V(27f, FFL + 2.95f, -90.09f), 180f, .26f, .04f, .1f, 4f);
        // SAUNA room: the owner's two cabins, a bench, the cold shower
        // the owner's two cabins (infrared) east of the corridor door - the walk-in SAUNA FIŃSKA and ŁAŹNIA PAROWA fill the
        // south-west corner (BraxChampionsAquaparkWellness.Sauna)
        Owner("sauna_kabina", V(53.0f, FFL, -88.6f), 0f, true);
        Owner("sauna_kabina", V(54.7f, FFL, -88.6f), 0f, true);
        Owner("prysznic_kabina", V(56.6f, FFL, -88.8f), 0f, false);
        ShowerWater(V(56.6f, FFL + 2.02f, -88.8f), FFL, "zimny prysznic SPA");
        foreach (float sx in new[] { -.64f, .64f }) Col(CX + 56.6f + sx - .03f, CX + 56.6f + sx + .03f, FFL, FFL + 2.2f, CZ - 89.3f, CZ - 88.2f, "kabina prysznic");
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
