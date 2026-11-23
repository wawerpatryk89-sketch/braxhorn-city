using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 1 on the hill over the arena (owner 17.11: "A tą willę trzeba konkretniejszą zrobić", "każda ma
// być inna, zobacz na internecie"). After a real house: 1471 Forest Knoll, Los Angeles (Standard Architecture, 2020; dezeen
// 19.07.2020): ~930 m2 on a promontory over the city - three parallel white volumes under gabled dark metal roofs, the
// central one a double-height living hall (~9 m) panelled in wood, its gable end glazed in a big steel frame; lower volumes
// between them for the stairs and the powder room; four bedrooms upstairs, a garage with a bedroom over it; a cantilevered
// infinity pool past the edge of the land. Here (the pad of BraxChampionsWilleDzialki, the view to -z):
//  - W  x -16..-8, z -8..14: the library lounge and the home cinema / two bedrooms with their bathrooms, a dressing room;
//  - C  x -5..5,  z -10..14: one hall 6.6 m to the eaves and 9.5 m to the ridge under an oak-lined vault - the living room
//    at the fire wall, the dining table for twelve, the kitchen (a low run, no wall units) and its island, the entrance;
//  - E  x 8..16,  z -8..14: the study, the gym, the utility room / the master suite (bedroom, dressing room, bathroom);
//  - G  x 16..26, z 4..14: the garage for three cars (three oak up-and-over doors on AKCJA) / the guest suite over it;
//  - the glass links x -8..-5 and 5..8: the stairs (stone treads, glass balustrades), the powder room;
//  - the terrace: travertine, the 20 x 6 m infinity pool hanging 3 m past the pad's edge, the jacuzzi, six loungers, the
//    outdoor lounge; the forecourt and the sale board at the back.
// Walls in the scanned exterior stucco, the roofs standing-seam graphite metal (30 deg), the gable frames black steel.
// 23.11 the owner (21:24 "ta na wzgórzu, za areną ... kompletnie trzeba przebudowę zrobić ... okna są chujowe, nie ma futryn ...
// elewacja do zmiany, w środku tak samo"; his choice: "nowoczesna jak wyspa") - his 17.11 Mediterranean villa (BraxChampionsWillaKlif)
// gives way to this plan, rebuilt modern: the three volumes white boxes under flat roofs with white edges (no gables), the
// glazed fronts in rectangular black steel frames, the hall's roof running 2.5 m on over the terrace on an oak soffit; the
// finish from his own Poly Haven list (BraxChampionsWillaWzgorzeNowa): drystone on the ground floors' ends, pine cladding on
// the hall's entrance front, flagstones, herringbone, plank floors; the cantilevered pool hangs over the hill's rock face.
public static partial class BraxChampionsWille
{
    const float AE = 6.6f, AF1 = 3.6f;
    const float WS = 3f;     // 23.11 22:32 (owner: "płaski prostokąt ... zobacz na internecie jak to robią"): W's and E's upper floors
                             // cantilevered 3 m over the terrace - light boxes on the drystone base (the hall between set back)
    public const float APoolX0 = -10f, APoolX1 = 10f, APoolZ0 = -25.6f, APoolZ1 = -19.6f;

    /// <summary>Where the ground under a villa's pad must stay under its pool (BraxChampionsWilleDzialki.Grunt): x0, x1 and
    /// the pool's back edge (the pool reaches the pad's front).</summary>
    public static Vector3 StrefaBasenu(int nr)
    {
        if (nr == 2) return new Vector3(PoolX0, SX1 + .2f, PoolZ1);                         // the island's pool and its raised spa
        if (nr == 3) return new Vector3(NPX0 - .5f, NPX1 + .5f, NPZ1 + .5f);                 // the seafront's formal pool, its border
        if (nr == 1) return new Vector3(APoolX0 - .7f, APoolX1 + .7f, APoolZ1 + .3f);     // 23.11 the hill's cantilevered pool (its slab)
        return new Vector3(PoolX0, PoolX1, PoolZ1);
    }
    /// <summary>A villa's pad (x0, x1, z0, z1) in its frame and the local x of its jetty's root (the sites,
    /// BraxChampionsWilleDzialki): each plan may have its own.</summary>
    public static Vector4 Pad(int nr) => new Vector4(nr == 2 ? WPadX0 : PadX0, PadX1, PadZ0, PadZ1);   // 2: the tennis court west
    public static float PomostX(int nr) => 20f;

    static void PlanWzgorze()
    {
        pietro = AF1;
        var c = new BraxKatalog.Ctx { m = kat, root = g, cols = g, unit = 1f, water = null, inst = g, seat = (p, f, l) => { W.Siedzisko(p, f, l); Siedziska++; } };
        string tag = Nazwy[0];
        W1Materialy();                                                                   // 23.11 his list (BraxChampionsWillaWzgorzeNowa)
        ATeren(); AWille(); W1Bryla(); AWnetrze(c, tag); W1Wnetrze(c, tag); AOgrod(tag); W1Ogrod();
    }

    // ================================================================ the pad: terrace, pool, lawns, forecourt
    static void ATeren()
    {
        Out_(true);
        const float s = .3f;
        // travertine terrace before the houses; its strips beside the pool are the pool's coping (also over the cantilever)
        MM(deck, -19.5f, 23.5f, -s, 0f, APoolZ1, -10f);
        MM(deck, -19.5f, APoolX0, -s, 0f, PadZ0, APoolZ1); MM(deck, APoolX1, 23.5f, -s, 0f, PadZ0, APoolZ1);
        MM(lawn, PadX0, -19.5f, -s, 0f, PadZ0, -8f); MM(lawn, 23.5f, PadX1, -s, 0f, PadZ0, 4f);             // 23.11 the palms' lawns
        MM(deck, APoolX0 - .38f, APoolX0, -s, 0f, APoolZ0 - .9f, PadZ0); MM(deck, APoolX1, APoolX1 + .38f, -s, 0f, APoolZ0 - .9f, PadZ0);
        MM(deck, -19.5f, -5f, -s, 0f, -10f, -8f); MM(deck, 5f, 23.5f, -s, 0f, -10f, -8f);
        MM(deck, -8f, -5f, -s, 0f, -8f, -3f); MM(deck, 5f, 8f, -s, 0f, -8f, -3f);
        MM(deck, 16f, 23.5f, -s, 0f, -8f, 4f);
        MM(deck, -.9f, .9f, -.25f, 0f, 13.7f, 14f);                                  // under the front door
        // lawns: west of the houses, the strip east of the garage, the forecourt's west part
        MM(lawn, PadX0, -19.2f, -s, 0f, -8f, 14f); MM(lawn, -17.8f, -16f, -s, 0f, -8f, 14f); MM(lawn, 26f, PadX1, -s, 0f, 4f, 14f);   // the path between (W1Ogrod)
        MM(lawn, PadX0, -8f, -s, 0f, 18f, PadZ1);
        // the forecourt (the drive from the gate at the pad's back, x 9) and the links' back notches
        MM(paving, PadX0, PadX1, -s, 0f, 14f, 18f); MM(paving, -8f, PadX1, -s, 0f, 18f, PadZ1);
        MM(paving, -8f, -5f, -s, 0f, 12f, 14f); MM(paving, 5f, 8f, -s, 0f, 12f, 14f);
        // 23.11 the pad stands on the hill's 9 m rock face (BraxChampionsWilleDzialki.PrzodKlif, BraxChampionsWillaKlif's), the
        // pool's cantilever hanging over it: under the pool the face starts under its slab
        W1Klif();
        // glass on the pad's edge beside the pool and along the cantilever's sides
        Balustrada(true, PadZ0 + .15f, PadX0 + .2f, APoolX0 - .6f, 0f); Balustrada(true, PadZ0 + .15f, APoolX1 + .6f, PadX1 - .2f, 0f);
        Balustrada(false, APoolX0 - .35f, APoolZ0 - .85f, PadZ0 + .1f, 0f); Balustrada(false, APoolX1 + .35f, APoolZ0 - .85f, PadZ0 + .1f, 0f);
        BasenWiszacy("willa_1_basen", APoolX0, APoolX1, APoolZ0, APoolZ1);
        JacuzziW(new Vector3(14f, 0f, -16.2f));
    }

    // ================================================================ the volumes
    static void AWille()
    {
        // ---- floors (stone in W, C, E and the links, concrete in the garage)
        Out_(false);
        MM(floorGf, -15.7f, -8.3f, -.25f, 0f, -8f, 13.7f); MM(floorGf, -4.7f, 4.7f, -.25f, 0f, -10f, 13.7f); MM(floorGf, 8.3f, 15.7f, -.25f, 0f, -8f, 13.7f);
        MM(floorGf, -8f, -5f, -.25f, 0f, -3f, 11.7f); MM(floorGf, 5f, 8f, -.25f, 0f, -3f, 11.7f);
        MM(concrete, 16f, 25.7f, -.25f, 0f, 4.3f, 13.7f);
        Out_(true); MM(concrete, 16.6f, 25.4f, -.25f, 0f, 13.7f, 14f);

        // ---- W: the side walls (ground and upper parts), the glass front, the back
        ScianaOE(stone, false, -16f, -8f, 14f, 0f, AF1, -1f, true, RogZ(-8f, 14f, true), new Vector4(-5f, -1.5f, .5f, 2.9f));          // 23.11 drystone
        ScianaOE(elewacja, false, -16f, -8f - WS, 14f, AF1, AE, -1f, true, RogZ(-8f - WS, 14f, true), new Vector4(-6.2f, -2.2f, AF1 + .6f, AE - .5f), new Vector4(7.6f, 9.2f, AF1 + .9f, AE - .5f));
        ScianaOE(elewacja, false, -8f, -8f, 14f, 0f, AF1, 1f, false, RogZ(-8f, 14f, true), new Vector4(-2.4f, -1.4f, 0f, 2.4f));
        ScianaOE(elewacja, false, -8f, -8f - WS, 14f, AF1, AE, 1f, false, RogZ(-8f - WS, 14f, true), new Vector4(7.6f, 8.5f, AF1, AF1 + 2.3f));
        AFront(-16f, -8f, -8f);
        ScianaOE(stone, true, 14f, -16f, -8f, 0f, AF1, 1f, true, RogX(-16f, -8f));
        ScianaOE(soffit, true, 14f, -16f, -8f, AF1, AE, 1f, true, RogX(-16f, -8f), new Vector4(-15f, -13f, AF1 + 1.5f, AE - .5f), new Vector4(-12f, -10.6f, AF1 + .3f, AE - .5f));   // 22:32 pine over the stone
        APietro(-15.7f, -8.3f, -7.95f, 13.7f);
        DachPlaski(-16f, -8f, -8f - WS, 14f, AE, .5f, .5f, 1.0f, .5f);

        // ---- C: the double-height hall - the glass gable (lower panes, one open to the terrace; upper panes; the glass
        // triangle), its steel frame, the side walls with the openings to the links, the back with the entrance
        Szyba(true, -9.85f, -4.7f, 4.7f, 0f, AF1, new Vector2(-1.5f, 1.5f));
        Out_(true); MM(alu, -4.7f, 4.7f, AF1 - .03f, AF1 + .03f, -9.9f, -9.8f, false);
        Szyba(true, -9.85f, -4.7f, 4.7f, AF1, AE);
        RamaProsta(-5f, 5f, AE, -10f, -1f);
        ScianaOE(elewacja, false, -5f, -10f, 14f, 0f, AE, -1f, false, RogZ(-10f, 14f, true), new Vector4(5.6f, 8.6f, 0f, 3.0f));
        ScianaOE(elewacja, false, 5f, -10f, 14f, 0f, AE, 1f, false, RogZ(-10f, 14f, true), new Vector4(7.5f, 10.5f, 0f, 3.0f));
        // 23.11 the entrance front in pine boards
        ScianaOE(soffit, true, 14f, -5f, 5f, 0f, AE, 1f, false, RogX(-5f, 5f), new Vector4(-3.6f, -2.8f, .4f, AE - .6f), new Vector4(-.9f, .9f, 0f, 3.0f), new Vector4(2.8f, 3.6f, .4f, AE - .6f));
        Szyba(true, 13.85f, -3.569f, -2.831f, .4f, AE - .6f); Szyba(true, 13.85f, 2.831f, 3.569f, .4f, AE - .6f);
        Out_(false); BraxChampionsInteriorDoor.Single("drzwi wejściowe", "WILLA | drzwi wejściowe (dąb, obrotowe)", true, 13.85f, -.9f, .9f, 0f, 3.0f, oak, steel, .07f);
        // the flat roof running 2.5 m on over the terrace, oak (pine) under it inside and out; three glulam beams across the hall
        DachPlaski(-5f, 5f, -10f, 14f, AE, .5f, .5f, 2.5f, .5f, soffit);
        Out_(false);
        foreach (float z in new[] { -6f, 0f, 6f }) b.Box(walnut, new Vector3(0f, AE - .2f, z), new Vector3(9.4f, .3f, .24f), Quaternion.identity);
        Out_(true);
        foreach (float x in new[] { -3.6f, -1.2f, 1.2f, 3.6f }) b.Cylinder(downlight, new Vector3(x, AE - .027f, -11.3f), .065f, .003f, 16, Quaternion.identity, false, true);
        // the entrance canopy: black steel, an oak soffit, two wall lights
        Out_(true);
        MM(stalCzarna, -2.6f, 2.6f, 3.3f, 3.5f, 14f, 16.6f); MM(soffit, -2.55f, 2.55f, 3.28f, 3.3f, 14.02f, 16.55f, false);
        foreach (float lx in new[] { -1.6f, 1.6f })
        {
            b.Box(black, new Vector3(lx, 2.1f, 14.06f), new Vector3(.12f, .36f, .12f), Quaternion.identity);
            b.Box(downlight, new Vector3(lx, 1.91f, 14.06f), new Vector3(.08f, .01f, .08f), Quaternion.identity);
            W.Lampa("wejście | kinkiet", new Vector3(lx, 1.8f, 14.4f), .5f, 4f * K, new Color(1f, .86f, .64f));
        }
        // the back: oak cladding on the links' ground floor walls, wall lights on W and E
        foreach (var (xa, xb) in new[] { (-8f, -5f), (5f, 8f) }) MM(soffit, xa + .02f, xb - .02f, 0f, AF1 - .1f, 12f, 12.04f, false);
        foreach (float lx in new[] { -14f, -10f, 10f, 14f })
        {
            b.Box(black, new Vector3(lx, 2.6f, 14.06f), new Vector3(.12f, .36f, .12f), Quaternion.identity);
            b.Box(downlight, new Vector3(lx, 2.41f, 14.06f), new Vector3(.08f, .01f, .08f), Quaternion.identity);
            W.Lampa("elewacja | kinkiet", new Vector3(lx, 2.3f, 14.4f), .4f, 4f * K, new Color(1f, .86f, .64f));
        }
        BraxChampionsLetters3D.Text(b, steel, black, Nazwy[0], new Vector3(-2.2f, 2.3f, 14.012f), BraxChampionsLetters3D.YawFacing(Vector3.forward), .16f, .012f, .12f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina przy wejściu", new Vector3(-3.2f, 0f, 15f), 40f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina przy wejściu", new Vector3(3.2f, 0f, 15f), 130f);

        // ---- E: as W mirrored
        ScianaOE(elewacja, false, 8f, -8f, 14f, 0f, AF1, -1f, false, RogZ(-8f, 14f, true), new Vector4(-2.4f, -1.4f, 0f, 2.4f), new Vector4(9.4f, 10.4f, 0f, 2.4f));
        ScianaOE(elewacja, false, 8f, -8f - WS, 14f, AF1, AE, -1f, false, RogZ(-8f - WS, 14f, true), new Vector4(7.6f, 8.5f, AF1, AF1 + 2.3f));
        ScianaOE(stone, false, 16f, -8f, 14f, 0f, AF1, 1f, false, RogZ(-8f, 14f, true), new Vector4(-6f, -1f, .5f, 2.9f), new Vector4(11.6f, 12.5f, 0f, 2.25f));
        Szyba(false, 15.85f, -5.969f, -1.031f, .5f, 2.9f);
        ScianaOE(elewacja, false, 16f, -8f - WS, 14f, AF1, AE, 1f, false, RogZ(-8f - WS, 14f, true), new Vector4(-6f, -2.5f, AF1 + .6f, AE - .5f), new Vector4(11.6f, 12.5f, AF1, AF1 + 2.25f));
        Szyba(false, 15.85f, -5.969f, -2.531f, AF1 + .6f, AE - .5f);
        AFront(8f, 16f, -8f);
        ScianaOE(stone, true, 14f, 8f, 16f, 0f, AF1, 1f, true, RogX(8f, 16f), new Vector4(13f, 15f, 1.2f, 2.5f));
        ScianaOE(soffit, true, 14f, 8f, 16f, AF1, AE, 1f, true, RogX(8f, 16f), new Vector4(11f, 15f, AF1 + .9f, AE - .5f));
        APietro(8.3f, 15.7f, -7.95f, 13.7f);
        DachPlaski(8f, 16f, -8f - WS, 14f, AE, .5f, 0f, 1.0f, .5f);                    // east: the garage's roof abuts
        Out_(false);
        Drzwi("do garażu", false, 15.85f, 11.6f, 12.5f, 0f, 2.25f, .018f, .018f, T * .5f); Drzwi("apartament gościnny", false, 15.85f, 11.6f, 12.5f, AF1, 2.25f, .018f, .018f, T * .5f);

        // ---- G: the garage (three doors to the forecourt) and the guest suite over it
        ScianaOE(elewacja, true, 4f, 16f, 26f, 0f, AF1, -1f, true, new Vector4(16f, 25.85f, 16f, 25.7f), new Vector4(19f, 23f, 1.6f, 2.6f));
        ScianaOE(elewacja, true, 4f, 16f, 26f, AF1, AE, -1f, true, new Vector4(16f, 25.85f, 16f, 25.7f), new Vector4(18f, 24f, AF1 + .6f, AE - .5f));
        ScianaOE(stone, false, 26f, 4f, 14f, 0f, AF1, 1f, true, RogZ(4f, 14f, false));
        ScianaOE(elewacja, false, 26f, 4f, 14f, AF1, AE, 1f, true, RogZ(4f, 14f, false), new Vector4(5.5f, 9f, AF1 + .8f, AE - .6f));
        ScianaOE(stone, true, 14f, 16f, 26f, 0f, AF1, 1f, false, new Vector4(16f, 25.85f, 16f, 25.7f), new Vector4(16.6f, 19.2f, 0f, 2.5f), new Vector4(19.7f, 22.3f, 0f, 2.5f), new Vector4(22.8f, 25.4f, 0f, 2.5f));   // the doors in drystone
        ScianaOE(elewacja, true, 14f, 16f, 26f, AF1, AE, 1f, true, new Vector4(16f, 25.85f, 16f, 25.7f), new Vector4(17f, 20f, AF1 + .9f, AE - .5f), new Vector4(22f, 24f, AF1 + 1.5f, AE - .5f));
        Out_(false);
        MM(ceiling, 16f, 25.7f, AF1 - .3f, AF1 - .04f, 4.3f, 13.7f); MM(floorUf, 16f, 25.7f, AF1 - .04f, AF1, 4.3f, 13.7f);
        MM(ceiling, 16f, 25.7f, AE - .2f, AE, 4.3f, 13.7f, false);
        DachPlaski(16f, 26f, 4f, 14f, AE, 0f, .5f, .5f, .5f);
        BramaGarazu("brama 1", 16.6f, 19.2f, 14f, 2.5f); BramaGarazu("brama 2", 19.7f, 22.3f, 14f, 2.5f); BramaGarazu("brama 3", 22.8f, 25.4f, 14f, 2.5f);

        // ---- the links: glass to the terrace, the stairs, the landings, flat roofs under the eaves
        foreach (float s in new[] { -1f, 1f })
        {
            float xa = s < 0f ? -8f : 5f, xb = s < 0f ? -5f : 8f;
            Szyba(true, -2.85f, xa, xb, 0f, 6.0f);
            Sciana(elewacja, true, 12f, xa, xb, 0f, AF1, 1f); Szyba(true, 11.85f, xa, xb, AF1, 6.0f);
            Out_(true); MM(elewacja, xa + .004f, xb - .004f, 6.0f, 6.3f, -3f, 12f); MM(gravel, xa + .1f, xb - .1f, 6.3f, 6.32f, -2.9f, 11.9f, false);
            Out_(false);
            MM(ceiling, xa + .004f, xb - .004f, AF1 - .3f, AF1 - .04f, 5.2f, 11.7f); MM(floorUf, xa + .004f, xb - .004f, AF1 - .04f, AF1, 5.22f, 11.7f);   // the top tread to 5.22
            float t0 = s < 0f ? -7.95f : 6.75f, t1 = s < 0f ? -6.75f : 7.95f, open = s < 0f ? -6.75f : 6.75f;
            Schody(floorGf, t0, t1, -.5f, 0f, AF1, 20, .285f, open);
            if (s < 0f) Balustrada(true, 5.23f, -6.75f, -5.05f, AF1); else Balustrada(true, 5.23f, 5.05f, 6.75f, AF1);
            Oczka(xa + 1.5f, xa + 1.5f, -1.8f, 10.2f, 6.0f, 2.4f);
        }
        // the powder room under the west landing
        Scianka(true, 9f, -8f, -5f, 0f, AF1 - .3f, new Vector2(-6.4f, -5.6f)); Drzwi("WC", true, 9f, -6.4f, -5.6f, 0f);
        Lazienka(-8f, -5f, 9.06f, 11.7f, 0f, new Vector3(1f, -6.4f, -5.6f), 0f, 0f);
    }
    // the glazed front of a two-storey volume x0..x1 at z (23.11: no gable; 22:32 the upper floor cantilevered WS over the
    // terrace): the ground floor's glass (one panel open) under the cantilever; the cantilever's slab - its white edge, its
    // pine soffit with downlights - the upper floor's oak floor over it, the upper glass at its front, the render band under
    // the roof, the black steel frame round the upper glass
    static void AFront(float x0, float x1, float z)
    {
        float i0 = x0 + T, i1 = x1 - T, m = (x0 + x1) * .5f, zu = z - WS;
        Szyba(true, z + .15f, i0, i1, 0f, AF1 - .3f, new Vector2(m - 1.2f, m + 1.2f));
        Out_(true);
        // the slab to the room's ceiling: its band to the walls' line z, on 5 cm only between the walls (23.11: its ends had
        // run 5 cm along the ground walls' faces past z)
        MM(elewacja, x0, x1, AF1 - .3f, AF1 - .04f, zu, z); MM(elewacja, i0, i1, AF1 - .3f, AF1 - .04f, z, z + .05f);
        MM(elewacja, x0, i0, AF1 - .04f, AF1, zu, z, false); MM(elewacja, i1, x1, AF1 - .04f, AF1, zu, z, false);   // under the walls
        MM(elewacja, i0, i1, AF1 - .04f, AF1, zu, zu + .08f, false);                                 // under the upper glass's frame
        MM(soffit, x0 + .02f, x1 - .02f, AF1 - .32f, AF1 - .3f, zu + .02f, z + .05f, false);
        for (float x = x0 + 1.6f; x < x1 - 1.0f; x += 2.4f) for (float zz = zu + .8f; zz < z - .2f; zz += 1.4f)
                b.Cylinder(downlight, new Vector3(x, AF1 - .327f, zz), .065f, .003f, 16, Quaternion.identity, false, true);
        W.Lampa("pod wspornikiem | światło", new Vector3(m, AF1 - .8f, zu + WS * .5f), .6f, 7f * K, new Color(1f, .88f, .70f));
        Out_(false); MM(floorUf, i0, i1, AF1 - .04f, AF1, zu + .08f, z + .05f, false);              // the room's floor over the cantilever
        MM(ceiling, i0, i1, AE - .2f, AE, zu + T, z + .3f, false);                                   // its ceiling to the rest's (APietro: from z + .3)
        Out_(true);
        Szyba(true, zu + .15f, i0, i1, AF1, AE - .2f);
        MM(elewacja, i0, i1, AE - .2f, AE, zu, zu + T, false);
        RamaProsta(x0, x1, AE, zu, -1f, AF1 - .3f);
    }
    // the slab between the floors (the ground floor's ceiling, the upper oak floor) and the upper floor's flat ceiling
    static void APietro(float x0, float x1, float z0, float z1)
    {
        Out_(false);
        MM(ceiling, x0, x1, AF1 - .3f, AF1 - .04f, z0, z1); MM(floorUf, x0, x1, AF1 - .04f, AF1, z0, z1);
        MM(ceiling, x0, x1, AE - .2f, AE, z0 + .25f, z1, false);
    }

    // ================================================================ inside
    static void AWnetrze(BraxKatalog.Ctx c, string tag)
    {
        Out_(false);
        float y = AF1, top = AE - .2f;
        // ---- C: the living room at the fire wall (stone to 5 m, the hearth bench, the linear fire, the 85" TV over it)
        MM(stoneIn, -4.7f, -4.58f, 0f, 5.0f, -8.6f, -4.2f);
        MM(stoneIn, -4.58f, -4.08f, 0f, .40f, -8.6f, -4.2f);
        MM(black, -4.58f, -4.56f, .48f, .98f, -7.4f, -5.4f, false);
        MM(fire, -4.56f, -4.54f, .56f, .62f, -7.15f, -5.65f, false);
        W.Lampa(tag + " | kominek", new Vector3(-3.9f, .7f, -6.4f), .7f, 5f * K, new Color(1f, .55f, .22f));
        BraxKatalog.Tv(c, new Vector3(-4.58f, 2.2f, -6.4f), Vector3.right, 85f, 1, false, tag + " salon TV");
        for (int i = 0; i < 2; i++) { W.Siedzisko(new Vector3(-4.3f, .40f, -8.1f + i * 3.4f), Vector3.right, "ŁAWA PRZY KOMINKU"); Siedziska++; }
        BraxKatalog.Rug(c, new Vector3(-2.6f, 0f, -6.4f), Vector3.forward, 4.0f, 3.8f, false);
        BraxKatalog.Sofa(c, new Vector3(-.9f, .012f, -6.4f), Vector3.left, 3.2f, 1, "sofa");
        Model("HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf", "stolik kawowy", new Vector3(-2.6f, .012f, -6.4f), 0f);
        W.Kolizja(new Vector3(-2.6f, .2f, -6.4f), new Vector3(1.0f, .4f, 1.0f), Quaternion.identity);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-2.6f, .012f, -8.7f), 0f), new Vector3(-2.6f, .45f, -8.7f), Vector3.forward, "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-2.6f, .012f, -4.1f), 180f), new Vector3(-2.6f, .45f, -4.1f), Vector3.back, "FOTEL");
        foreach (float x in new[] { 2.0f, 3.6f })
            Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel przy szybie", new Vector3(x, 0f, -8.4f), 180f), new Vector3(x, .42f, -8.4f), Vector3.back, "FOTEL");
        Model("HotelSobczak/side_table_01/side_table_01_1k.gltf", "stolik przy szybie", new Vector3(2.8f, 0f, -8.6f), 0f);
        BraxKatalog.FloorLamp(c, new Vector3(-.4f, 0f, -8.4f));
        Obrazek(1, new Vector3(4.69f, 2.6f, -5.6f), Vector3.left, 3.2f, 2.0f);
        // dining for twelve (two tables end to end) on a rug; pendants on long cords from the vault
        BraxKatalog.Rug(c, new Vector3(-1.5f, 0f, 2.6f), Vector3.forward, 5.4f, 3.2f, true);
        BraxKatalog.DiningSet(c, new Vector3(-2.3f, .012f, 2.6f), Vector3.back, "krzesło");
        BraxKatalog.DiningSet(c, new Vector3(-.7f, .012f, 2.6f), Vector3.back, "krzesło");
        foreach (float x in new[] { -2.3f, -.7f }) Wisiorek(new Vector3(x, 2.2f, 2.6f));
        // the kitchen: a low run of ten modules on the east wall (no wall units), the stone island with three stools
        BraxKatalog.Kitchen(c, new Vector3(4.698f, 0f, -1.0f), Vector3.forward, Vector3.left,
            new[] { "szuflady", "szuflady", "zlew", "zmywarka", "szuflady", "szuflady", "plyta", "szuflady", "piekarnik", "szuflady" }, walnut, BraxKatalog.BlackMatt, BraxKatalog.Stone, tag + " kuchnia", false);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(2.4f, .45f, 2.0f), new Vector3(1.0f, .87f, 3.0f), .01f, Quaternion.identity, 1);
        b.RoundedBox(BraxKatalog.Stone, new Vector3(2.4f, .895f, 2.0f), new Vector3(1.15f, .05f, 3.2f), .008f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(2.4f, .46f, 2.0f), new Vector3(1.15f, .92f, 3.2f), Quaternion.identity);
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(1.45f, 0f, 1.0f + i * 1.0f);
            b.Cylinder(steel, sp, .025f, .7f, 10, Quaternion.identity, false);
            b.Cylinder(steel, sp, .2f, .015f, 20, Quaternion.identity, true);
            b.Cylinder(BraxKatalog.Leather, sp + Vector3.up * .66f, .2f, .07f, 20, Quaternion.identity, true);
            W.Siedzisko(sp + Vector3.up * .73f, Vector3.right, "HOKER"); Siedziska++;
            Wisiorek(new Vector3(2.4f, 1.85f, 1.0f + i * 1.0f));
        }
        // the hall: a walnut console and the mirror, the art, plants
        b.RoundedBox(walnut, new Vector3(-4.45f, .42f, 11.4f), new Vector3(.45f, .84f, 1.8f), .01f, Quaternion.identity, 1);
        W.Kolizja(new Vector3(-4.45f, .42f, 11.4f), new Vector3(.45f, .84f, 1.8f), Quaternion.identity);
        BraxChampionsMirror.Wall(tag + " hol lustro", new Vector3(-4.69f, 1.75f, 11.4f), Vector3.right, 1.4f, 1.2f, mirrorFrame);
        Model("HotelSobczak/ceramic_vase_01/ceramic_vase_01_1k.gltf", "wazon", new Vector3(-4.45f, .84f, 10.8f), 0f);
        Obrazek(2, new Vector3(4.69f, 2.0f, 12.0f), Vector3.left, 2.0f, 1.3f);
        Model("HotelSobczak/potted_plant_04/potted_plant_04_1k.gltf", "roślina", new Vector3(-3.6f, 0f, 13.0f), 0f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(4.0f, 0f, -9.2f), 200f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(-4.0f, 0f, -9.3f), 30f);
        foreach (var lp in new[] { new Vector3(-2f, 4.6f, -6f), new Vector3(-1.5f, 4.6f, 2.6f), new Vector3(2.4f, 3.6f, 2f), new Vector3(0f, 4.6f, 10.5f), new Vector3(0f, 5.4f, -2f) })
            W.Lampa(tag + " | salon światło", lp, 1.1f, 11f * K, new Color(1f, .93f, .82f));

        // ---- W ground: the library lounge (front) and the home cinema (back)
        Scianka(true, 3f, -15.7f, -8.3f, 0f, AF1 - .3f, new Vector2(-10.4f, -9.4f)); Drzwi("kino", true, 3f, -10.4f, -9.4f, 0f);
        BraxKatalog.Rug(c, new Vector3(-12.0f, 0f, -4.4f), Vector3.forward, 4.0f, 3.4f, true);
        BraxKatalog.Sofa(c, new Vector3(-12.0f, .012f, -3.2f), Vector3.back, 3.2f, 0, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(-12.0f, .012f, -4.6f), Vector3.back, 1.2f, .6f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-14.5f, .012f, -5.6f), 60f), new Vector3(-14.5f, .45f, -5.6f), new Vector3(.87f, 0f, .5f), "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-9.5f, .012f, -5.6f), -60f), new Vector3(-9.5f, .45f, -5.6f), new Vector3(-.87f, 0f, .5f), "FOTEL");
        BraxKatalog.Dresser(c, new Vector3(-15.7f + .47f, 0f, .2f), Vector3.right, 2.4f, 2, 3, walnut);           // p: its front (23.11: it stood in the wall)
        Obrazek(3, new Vector3(-15.69f, 2.1f, .2f), Vector3.right, 2.0f, 1.0f);
        BraxKatalog.FloorLamp(c, new Vector3(-14.6f, 0f, -2.6f));
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-15.1f, 0f, -7.3f), 70f);
        Oczka(-14.6f, -9.8f, -6.8f, 1.6f, AF1 - .3f);
        W.Lampa(tag + " | biblioteka", new Vector3(-12f, AF1 - .5f, -3f), .9f, 8f * K, new Color(1f, .93f, .82f));
        // the cinema: the 100" screen on the back wall, two rows of sofas
        BraxKatalog.Tv(c, new Vector3(-12.0f, 1.75f, 13.68f), Vector3.back, 100f, 2, false, tag + " kino");
        BraxKatalog.Sofa(c, new Vector3(-12.0f, .012f, 8.4f), Vector3.forward, 3.2f, 1, "sofa kinowa");
        BraxKatalog.Sofa(c, new Vector3(-12.0f, .012f, 5.4f), Vector3.forward, 3.2f, 1, "sofa kinowa");
        BraxKatalog.Rug(c, new Vector3(-12.0f, 0f, 7.0f), Vector3.forward, 5.0f, 5.0f, false);
        W.Lampa(tag + " | kino", new Vector3(-12f, AF1 - .5f, 6f), .45f, 7f * K, new Color(1f, .8f, .6f));
        Oczka(-14.6f, -9.8f, 4.2f, 12.6f, AF1 - .3f);

        // ---- E ground: the study (front), the gym, the utility room by the garage door
        Scianka(true, 2.5f, 8.3f, 15.7f, 0f, AF1 - .3f, new Vector2(9.0f, 9.9f)); Drzwi("gabinet", true, 2.5f, 9.0f, 9.9f, 0f, 2.25f, NaOkl);
        Scianka(true, 9f, 8.3f, 15.7f, 0f, AF1 - .3f, new Vector2(13f, 13.9f)); Drzwi("siłownia", true, 9f, 13f, 13.9f, 0f);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(12.0f, .74f, -4.4f), new Vector3(2.0f, .04f, .9f), .008f, Quaternion.identity, 2);
        foreach (float dx in new[] { -.96f, .96f }) b.Box(walnut, new Vector3(12.0f + dx, .36f, -4.4f), new Vector3(.04f, .72f, .86f), Quaternion.identity);
        W.Kolizja(new Vector3(12.0f, .38f, -4.4f), new Vector3(2.0f, .76f, .9f), Quaternion.identity);
        BraxKatalog.Laptop(c, new Vector3(12.0f, .76f, -4.5f), Vector3.forward, 1);
        Siedz(Model("HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf", "krzesło przy biurku", new Vector3(12.0f, 0f, -3.5f), 180f), new Vector3(12.0f, .47f, -3.5f), Vector3.back, "KRZESŁO");
        Model("HotelSobczak/desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf", "lampka", new Vector3(11.2f, .76f, -4.7f), 0f);
        BraxKatalog.Sofa(c, new Vector3(15.1f, .012f, .4f), Vector3.left, 2.4f, 0, "sofa");
        BraxKatalog.Dresser(c, new Vector3(8.3f + .47f, 0f, -.6f), Vector3.right, 2.4f, 2, 3, walnut);
        Obrazek(4, new Vector3(8.31f, 2.1f, -.6f), Vector3.right, 1.8f, 1.0f);
        Model("HotelSobczak/potted_plant_04/potted_plant_04_1k.gltf", "roślina", new Vector3(15.1f, 0f, -7.3f), 0f);
        Oczka(9.4f, 14.2f, -6.8f, 1.6f, AF1 - .3f);
        W.Lampa(tag + " | gabinet", new Vector3(12f, AF1 - .5f, -3f), .9f, 8f * K, new Color(1f, .93f, .82f));
        // the gym: a mirror wall, mats, dumbbells on a rack
        BraxChampionsMirror.Wall(tag + " siłownia lustro", new Vector3(15.69f, 1.3f, 5.8f), Vector3.left, 4.0f, 2.0f, mirrorFrame);
        MM(steel, 8.4f, 8.9f, 0f, .9f, 3.4f, 7.6f);
        for (int i = 0; i < 6; i++) BraxKatalog.Dumbbell(c, new Vector3(8.65f, .92f, 3.8f + i * .62f), Vector3.right, 6f + i * 4f);
        for (int i = 0; i < 3; i++) BraxKatalog.MatRoll(c, new Vector3(12.0f + i * .9f, .08f, 8.4f), Vector3.right, i == 1 ? BraxKatalog.BlackMatt : walnut);
        W.Lampa(tag + " | siłownia", new Vector3(12f, AF1 - .5f, 5.8f), .9f, 8f * K);
        // the utility room: a low run (sink, drawers), oak shelves
        BraxKatalog.Kitchen(c, new Vector3(9.0f, 0f, 13.698f), Vector3.right, Vector3.back, new[] { "szuflady", "zlew", "szuflady", "szuflady" }, oak, BraxKatalog.BlackMatt, BraxKatalog.Stone, tag + " pralnia", false);
        for (int i = 0; i < 4; i++) MM(oak, 15.3f, 15.7f, .6f + i * .45f, .63f + i * .45f, 9.3f, 11.3f);
        // the powder room under the west landing
        BraxKatalog.WcPan(c, new Vector3(-7.984f, 0f, 10.5f), Vector3.right);
        BraxKatalog.Place(c, "umywalka 0.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .6f, walnut, "umywalka"), new Vector3(-5.9f, 0f, 11.684f), Vector3.back, null);
        W.Lampa(tag + " | WC", new Vector3(-6.5f, AF1 - .6f, 10.4f), .5f, 4f * K);
        W.Lampa(tag + " | pralnia", new Vector3(12f, AF1 - .5f, 11.4f), .6f, 5f * K);

        // ---- W upper: bedroom 1 at the glass (its bathroom, its dressing room), bedroom 2 (its bathroom), the corridor
        // 23.11: bedroom 1's bed stood across this wall (the bed's p is its foot, not its middle) between three doors - the bath
        // is now reached through the walk-in (its door on the walk-in's west wall), the walk-in's door by the corridor's
        Scianka(true, -.5f, -15.7f, -8.3f, y, top, new Vector2(-11.4f, -10.6f), new Vector2(-9.8f, -8.9f));
        Scianka(false, -12.4f, -.5f, 2.8f, y, top, new Vector2(1.3f, 2.1f));
        Scianka(false, -10.4f, -.5f, 9.6f, y, top, new Vector2(3.4f, 4.3f));
        Scianka(true, 2.8f, -15.7f, -10.4f, y, top);
        Scianka(true, 9.6f, -15.7f, -10.4f, y, top, new Vector2(-14.5f, -13.7f));
        Scianka(false, -12.4f, 9.6f, 13.7f, y, top);
        Drzwi("łazienka 1", false, -12.4f, 1.3f, 2.1f, y); Drzwi("garderoba 1", true, -.5f, -11.4f, -10.6f, y, 2.25f, NaOkl); Drzwi("sypialnia 1", true, -.5f, -9.8f, -8.9f, y);
        Drzwi("sypialnia 2", false, -10.4f, 3.4f, 4.3f, y); Drzwi("łazienka 2", true, 9.6f, -14.5f, -13.7f, y);
        BraxKatalog.Rug(c, new Vector3(-13.6f, y, -2.6f), Vector3.forward, 3.4f, 2.8f, false);
        BraxKatalog.Bed(c, new Vector3(-13.6f, y + .012f, -2.80f), Vector3.back, 1.8f, "łóżko");          // the head 3 cm off the brick (-.60)
        BraxKatalog.Tv(c, new Vector3(-8.32f, y + 1.35f, -4.6f), Vector3.left, 65f, 3, false, tag + " sypialnia 1 TV");
        // 23.11 the 3 m out over the terrace: a bench at the bed's foot, two lounge chairs and a side table on a rug at the glass
        LawkaLozka(c, new Vector3(-13.6f, y, -2.80f - .30f), 1.5f);
        BraxKatalog.Rug(c, new Vector3(-12.0f, y, -9.2f), Vector3.forward, 3.6f, 2.6f, false);
        foreach (var (x, yaw) in new[] { (-13.1f, 160f), (-10.9f, 200f) })
            Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel przy szybie", new Vector3(x, y, -9.3f), yaw), new Vector3(x, y + .42f, -9.3f), new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad)), "FOTEL");
        Model("HotelSobczak/side_table_01/side_table_01_1k.gltf", "stolik przy szybie", new Vector3(-12.0f, y + .012f, -9.5f), 0f);
        BraxKatalog.FloorLamp(c, new Vector3(-14.5f, y, -9.9f));
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(-15.2f, y, -10.3f), 0f);
        Lazienka(-15.7f, -12.46f, -.44f, 2.74f, y, new Vector3(2f, 1.3f, 2.1f), 0f, 0f);
        Prysznic(new Vector3(-13.25f, y, .35f));
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .8f, walnut, "umywalka"), new Vector3(-15.684f, y, .6f), Vector3.right, null);
        BraxKatalog.WcPan(c, new Vector3(-15.0f, y, 2.724f), Vector3.back);
        BraxKatalog.Wardrobe(c, new Vector3(-11.4f, y, 2.14f), Vector3.back, 1.8f, oak, BraxKatalog.Walnut, 2.4f, "garderoba");
        BraxKatalog.Rug(c, new Vector3(-13.0f, y, 4.6f), Vector3.forward, 2.8f, 2.6f, true);
        BraxKatalog.Bed(c, new Vector3(-13.0f, y + .012f, 5.06f), Vector3.forward, 1.6f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(-11.06f, y, 7.6f), Vector3.left, 2.4f, oak, BraxKatalog.Walnut, 2.4f, "szafa");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w sypialni", new Vector3(-15.0f, y, 8.6f), 120f), new Vector3(-15.0f, y + .45f, 8.6f), new Vector3(.87f, 0f, -.5f), "FOTEL");
        Lazienka(-15.7f, -12.46f, 9.66f, 13.7f, y, new Vector3(1f, -14.5f, -13.7f), -15.0f, -13.0f);
        Prysznic(new Vector3(-13.25f, y, 12.9f));
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .8f, walnut, "umywalka"), new Vector3(-15.684f, y, 12.2f), Vector3.right, null);
        BraxKatalog.WcPan(c, new Vector3(-15.684f, y, 10.6f), Vector3.right);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w czytelni", new Vector3(-11.4f, y, 12.6f), 180f), new Vector3(-11.4f, y + .45f, 12.6f), Vector3.back, "FOTEL");
        BraxKatalog.FloorLamp(c, new Vector3(-12.0f, y, 13.3f));
        // 23.11 the prints over the beds (clear over the headboards, 1.48 m), the linings' faces; a TV in every bedroom
        Obrazek(5, new Vector3(-13.0f, y + 2.05f, 2.8f + .06f), Vector3.forward, 1.6f, .8f);
        Obrazek(8, new Vector3(-13.6f, y + 2.05f, -.5f - .06f - .04f), Vector3.back, 2.0f, .9f);
        BraxKatalog.Tv(c, new Vector3(-12.5f, y + 1.35f, 9.6f - .06f), Vector3.back, 55f, 1, false, tag + " sypialnia 2 TV");
        Oczka(-14.6f, -9.8f, -9.2f, -1.6f, top); Oczka(-14.4f, -12.0f, 3.8f, 8.6f, top); Oczka(-9.4f, -9.4f, .4f, 12.4f, top);
        foreach (var lp in new[] { new Vector3(-12f, top - .3f, -8.6f), new Vector3(-12f, top - .3f, -3.5f), new Vector3(-13f, top - .3f, 6f), new Vector3(-9.4f, top - .3f, 6f), new Vector3(-14f, top - .3f, 1f), new Vector3(-14f, top - .3f, 11.6f) })
            W.Lampa(tag + " | piętro W światło", lp, .9f, 8f * K, new Color(1f, .93f, .82f));

        // ---- E upper: the master suite - the bedroom at the glass, the dressing room, the bathroom; the corridor and the
        // passage to the guest suite over the garage
        Scianka(true, .4f, 8.3f, 15.7f, y, top, new Vector2(8.8f, 9.7f), new Vector2(14.7f, 15.6f));       // 23.11 the walk-in's door off the bed's wall
        Scianka(false, 10.2f, .4f, 10.0f, y, top);
        Scianka(true, 4.6f, 10.2f, 15.7f, y, top, new Vector2(13.6f, 14.5f));
        Scianka(true, 10.0f, 10.2f, 15.7f, y, top);
        Drzwi("sypialnia główna", true, .4f, 8.8f, 9.7f, y, 2.25f, NaOkl); Drzwi("garderoba", true, .4f, 14.7f, 15.6f, y, 2.25f, NaOkl); Drzwi("łazienka główna", true, 4.6f, 13.6f, 14.5f, y);
        BraxKatalog.Rug(c, new Vector3(12.2f, y, -2.4f), Vector3.forward, 3.6f, 3.0f, true);
        BraxKatalog.Bed(c, new Vector3(12.2f, y + .012f, -1.90f), Vector3.back, 1.8f, "łóżko");             // the head 3 cm off the cherry (.30)
        BraxKatalog.Tv(c, new Vector3(8.32f, y + 1.35f, -3.6f), Vector3.right, 75f, 0, false, tag + " sypialnia główna TV");
        // 23.11 the 3 m out over the terrace: a bench at the bed's foot; the sofa, its table and an armchair on a rug at the glass
        LawkaLozka(c, new Vector3(12.2f, y, -1.90f - .30f), 1.5f);
        BraxKatalog.Rug(c, new Vector3(12.0f, y, -8.6f), Vector3.forward, 3.4f, 2.6f, false);
        BraxKatalog.Sofa(c, new Vector3(12.0f, y + .012f, -7.6f), Vector3.back, 2.4f, 1, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(12.0f, y + .012f, -8.6f), Vector3.back, 1.1f, .55f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel przy szybie", new Vector3(14.4f, y, -9.4f), 300f), new Vector3(14.4f, y + .45f, -9.4f), new Vector3(-.87f, 0f, .5f), "FOTEL");
        BraxKatalog.FloorLamp(c, new Vector3(10.4f, y, -7.1f));
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(15.2f, y, -10.3f), 0f);
        // 23.11 the walk-in's wardrobe either side of the bathroom's door (one 5.2 m run had stood across it)
        BraxKatalog.Wardrobe(c, new Vector3(11.85f, y, 3.94f), Vector3.back, 3.0f, walnut, BraxKatalog.Walnut, 2.6f, "garderoba");
        BraxKatalog.Wardrobe(c, new Vector3(15.15f, y, 3.94f), Vector3.back, .8f, walnut, BraxKatalog.Walnut, 2.6f, "garderoba");
        BraxKatalog.Dresser(c, new Vector3(12.95f, y, 1.9f), Vector3.back, 1.6f, 3, 2, walnut);
        Lazienka(10.26f, 15.7f, 4.66f, 9.94f, y, new Vector3(1f, 13.6f, 14.5f), 0f, 0f);
        BraxKatalog.Place(c, "umywalka 1.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, 1.6f, walnut, "umywalka"), new Vector3(11.6f, y, 4.676f), Vector3.forward, null);
        BraxChampionsMirror.Wall(tag + " łazienka lustro", new Vector3(11.6f, y + 1.6f, 4.682f), Vector3.forward, 1.6f, 1.0f, mirrorFrame);
        Wanna(new Vector3(12.0f, y, 8.9f));
        Prysznic(new Vector3(15.0f, y, 5.5f));
        BraxKatalog.WcPan(c, new Vector3(15.684f, y, 8.4f), Vector3.left);
        Obrazek(6, new Vector3(10.14f, y + 1.6f, 6.0f), Vector3.left, 1.4f, .9f);
        Obrazek(9, new Vector3(12.2f, y + 2.05f, .4f - .06f - .04f), Vector3.back, 2.0f, .9f);
        Model("HotelSobczak/potted_plant_04/potted_plant_04_1k.gltf", "roślina", new Vector3(15.2f, y, 13.2f), 0f);
        Oczka(9.4f, 14.2f, -9.2f, -.8f, top); Oczka(9.25f, 9.25f, 1.4f, 12.6f, top); Oczka(11.4f, 14.8f, 11.0f, 13.0f, top);
        foreach (var lp in new[] { new Vector3(12f, top - .3f, -8.6f), new Vector3(12f, top - .3f, -3.5f), new Vector3(13f, top - .3f, 2.5f), new Vector3(13f, top - .3f, 7.3f), new Vector3(9.25f, top - .3f, 6f), new Vector3(13f, top - .3f, 11.8f) })
            W.Lampa(tag + " | piętro E światło", lp, .9f, 8f * K, new Color(1f, .93f, .82f));

        // ---- the guest suite over the garage
        Scianka(true, 10.2f, 16f, 25.7f, y, top, new Vector2(17.4f, 18.3f));
        Scianka(false, 21.2f, 10.2f, 13.7f, y, top, new Vector2(10.8f, 11.6f));
        Drzwi("sypialnia gościnna", true, 10.2f, 17.4f, 18.3f, y, 2.25f, NaOkl); Drzwi("łazienka gościnna", false, 21.2f, 10.8f, 11.6f, y);
        BraxKatalog.Rug(c, new Vector3(21.0f, y, 7.4f), Vector3.forward, 3.0f, 2.6f, true);
        BraxKatalog.Bed(c, new Vector3(21.0f, y + .012f, 7.90f), Vector3.back, 1.6f, "łóżko");              // the head 3 cm off the cherry (10.10)
        // 23.11 the wardrobe off the east window (it had stood in front of it), a chest of drawers under the TV
        BraxKatalog.Wardrobe(c, new Vector3(25.08f, y, 9.58f), Vector3.left, 1.0f, oak, BraxKatalog.Walnut, 2.4f, "szafa");
        BraxKatalog.Dresser(c, new Vector3(16f + .47f, y, 6.5f), Vector3.right, 1.6f, 2, 2, walnut);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w sypialni", new Vector3(17.2f, y, 5.2f), 210f), new Vector3(17.2f, y + .45f, 5.2f), new Vector3(-.5f, 0f, -.87f), "FOTEL");
        Lazienka(21.26f, 25.7f, 10.26f, 13.7f, y, new Vector3(0f, 10.8f, 11.6f), 22f, 24f);
        Prysznic(new Vector3(24.9f, y, 11.1f));
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .8f, walnut, "umywalka"), new Vector3(22.6f, y, 10.276f), Vector3.forward, null);
        BraxKatalog.WcPan(c, new Vector3(21.276f, y, 12.9f), Vector3.right);
        Obrazek(7, new Vector3(21.0f, y + 2.05f, 10.2f - .06f - .04f), Vector3.back, 1.6f, .8f);
        BraxKatalog.Tv(c, new Vector3(16.02f, y + 1.35f, 6.5f), Vector3.right, 55f, 2, false, tag + " sypialnia gościnna TV");
        Oczka(17.2f, 24.4f, 5.4f, 8.8f, top); Oczka(17.4f, 19.8f, 11.0f, 13.0f, top);
        foreach (var lp in new[] { new Vector3(21f, top - .3f, 6.5f), new Vector3(18.5f, top - .3f, 12f), new Vector3(23.5f, top - .3f, 12f) })
            W.Lampa(tag + " | apartament gościnny", lp, .8f, 7f * K, new Color(1f, .93f, .82f));

        // ---- the garage: tool cabinets, tyres, the wall charger, wheel stops (cut into the floor), the light
        MM(walnut, 25.2f, 25.7f, 0f, 2.0f, 5.0f, 9.0f);
        for (int i = 0; i < 3; i++) MM(steel, 16.3f, 16.8f, .5f + i * .6f, .53f + i * .6f, 5.0f, 7.0f);
        for (int i = 0; i < 4; i++) b.Cylinder(black, new Vector3(16.55f, .53f + (i % 2) * .6f + .12f, 5.45f + (i / 2) * 1.0f), .33f, .23f, 20, Quaternion.Euler(90f, 0f, 0f), true, true);
        foreach (float x in new[] { 17.9f, 21.0f, 24.1f }) MM(black, x - .6f, x + .6f, 0f, .1f, 5.6f, 5.75f);
        b.Box(black, new Vector3(25.6f, 1.3f, 10.5f), new Vector3(.12f, .4f, .26f), Quaternion.identity);
        W.Lampa("garaż | światło", new Vector3(21f, AF1 - .5f, 9f), 1.1f, 10f * K);
        W.Lampa("garaż | światło", new Vector3(17.5f, AF1 - .5f, 9f), .8f, 8f * K);
    }
    // a pendant on a long cord from the vault over (x, z), its shade's bottom at p.y
    static void Wisiorek(Vector3 p)
    {
        float vault = AE - .035f;                                                     // the hall's flat oak ceiling (23.11)
        b.Tube(black, p + Vector3.up * .2f, new Vector3(p.x, vault, p.z), .004f, 4);
        b.Cylinder(BraxKatalog.BlackMatt, p, .16f, .22f, 20, Quaternion.identity, true, false, .05f);
    }

    // ================================================================ the garden
    static void AOgrod(string tag)
    {
        Out_(true);
        Lezaki(new Vector3(-8.6f, 0f, -16.4f), 6, 2.3f, Vector3.back);
        // the outdoor lounge on the east terrace: a teak sofa and two armchairs round a fire table
        var lc = new Vector3(21.5f, 0f, -2.5f);
        Lawka(lc + new Vector3(0f, 0f, 1.4f), Vector3.back, 2.4f); Lawka(lc + new Vector3(-1.8f, 0f, 0f), Vector3.right, .9f); Lawka(lc + new Vector3(1.8f, 0f, 0f), Vector3.left, .9f);
        b.RoundedBox(stone, lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), .02f, Quaternion.identity, 2);
        b.Box(BraxKatalog.StoneDark, lc + new Vector3(0f, .405f, 0f), new Vector3(1.0f, .01f, .4f), Quaternion.identity);
        W.Kolizja(lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), Quaternion.identity);
        W.Lampa(tag + " | ogień", lc + new Vector3(0f, .7f, 0f), .5f, 5f * K, new Color(1f, .55f, .22f));
        // bollards along the terrace, plants by the glass
        foreach (float x in new[] { -20f, -12f, 6f, 20f }) { b.Cylinder(black, new Vector3(x, 0f, -12.4f), .07f, .6f, 12, Quaternion.identity, true); W.Lampa(tag + " | taras", new Vector3(x, .7f, -12.2f), .5f, 6f * K, new Color(1f, .86f, .64f)); }
        foreach (var p in new[] { new Vector3(-6.4f, 0f, -9.2f), new Vector3(6.4f, 0f, -9.2f), new Vector3(17.0f, 0f, -7.2f) })
            Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina na tarasie", p, p.x * 7f);
        TablicaSprzedazy(1, new Vector3(2.0f, 0f, 23.6f), Vector3.forward,
            "MONIKA: Willa na wzgórzu nad areną: trzy białe bryły pod płaskimi dachami, kamień i drewno, salon na dwie kondygnacje pod dębowym stropem, basen infinity wiszący nad skałą, jacuzzi, kino, siłownia, garaż na trzy auta, cztery sypialnie z łazienkami. Zapraszam do WAWER NIERUCHOMOŚCI w T12!");
    }
}
