using System;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 1 on the hill over the arena, rebuilt modern (owner 23.11 21:24: "Następna willa. Ta na wzgórzu, za
// areną ... Wzdłuż willi mają iść grube palmy ... kompletnie trzeba przebudowę zrobić. Okna są chujowe, nie ma futryn, nie ma
// nic ... Elewacja do zmiany, w środku tak samo"; his choice of style: "Nowoczesna jak wyspa"; 21:59 his Poly Haven library:
// "ile ... jest zajebistych tekstur, na mury, na kostki, na podłogi, na wszystko"). The plan is BraxChampionsWillaWzgorze's
// (three volumes and two glass links, the double-height hall, the cinema, the gym, the garage for three cars): here
//  - its finish, all from his own list (MATERIALY-WLASCICIELA, 4K): the walls a warm white render (Plastered Wall 05), the
//    ground floors' ends and the garage's side in drystone (Rock Wall 04), the hall's entrance front in pine boards (Coated
//    Pine, also the soffits and the garage doors), the terrace in flagstones (Pavement 02), the forecourt in herringbone
//    (Herringbone Concrete Tile 03), the garden path in setts (Stone Pathway 02); indoors plank floors (Old Wooden Floor 03
//    downstairs, Wood Table upstairs), the fire wall in the drystone, brick (Brick Wall 02) and lacquered cherry panels;
//  - the flat roofs (no gables): a slab with a white edge and a low parapet, gravel on top, the hall's roof running on 2.5 m
//    over the terrace on its pine soffit; the glazed fronts in rectangular black steel frames;
//  - the windows in real frames (90 mm black profiles, BraxChampionsWille.Szyba), an aluminium sill outside and an oak board
//    inside each one with a sill;
//  - the pad on the hill's 9 m rock face (BraxChampionsWilleDzialki.PrzodKlif), the cantilevered pool hanging over it;
//  - thick palms (the region's "palma", palm_hp_4) in rows along the house: the west lawn, the east lawn, the back.
public static partial class BraxChampionsWille
{
    // the means of the new photo-scans (sRGB 0-255, measured 23.11 on the 1k maps; their brightest 1 %: Foundation.Jasne)
    public static readonly Vector3 MeanRockWall04 = new Vector3(105, 86, 65), MeanPavement02 = new Vector3(150, 128, 106),
        MeanHerringbone03 = new Vector3(92, 86, 78), MeanStonePathway02 = new Vector3(67, 55, 43), MeanCoatedPine = new Vector3(104, 57, 7),
        MeanOldWoodenFloor03 = new Vector3(132, 79, 28), MeanWoodTable = new Vector3(99, 63, 40), MeanBrickWall02 = new Vector3(162, 143, 118),
        MeanLacqueredCherry = new Vector3(47, 28, 15);
    static Material sciezka, oknoParapet;
    public static int W1Palmy;

    static void W1Materialy()
    {
        var F = (Func<string, string, float, Color, Vector3, Material>)BraxChampionsFoundation.Fotoskan;
        // the render: smooth warm white, inside and out (the scanned stucco read as raw concrete; the old paint had no grain)
        elewacja = Tynk("willa1_elewacja_biel", new Color(.96f, .95f, .92f), 2f); BraxChampionsInteriorLight.Facade(elewacja);
        render = elewacja;
        renderIn = Inside(Tynk("willa1_sciany_biel", new Color(.94f, .91f, .85f), 2f), .80f);
        ceiling = Inside(Tynk("willa1_sufit", new Color(.97f, .96f, .92f), 2f), .95f);
        // the drystone (Rock Wall 04, 2.27 m), warm; the same stone at the fire
        stone = F("willa1_kamien_mur", "rock_wall_04", 2.27f, new Color(.60f, .52f, .42f), MeanRockWall04); BraxChampionsInteriorLight.Facade(stone);
        stoneIn = Inside(F("willa1_kamien_kominek", "rock_wall_04", 2.27f, new Color(.60f, .52f, .42f), MeanRockWall04), .9f);
        // the pine boards (Coated Pine, 0.74 m): the hall's entrance front, every soffit, the garage doors
        soffit = F("willa1_drewno_sosna", "coated_pine", .74f, new Color(.58f, .40f, .24f), MeanCoatedPine); BraxChampionsInteriorLight.Facade(soffit);
        // the ground: flagstones on the terrace, herringbone on the forecourt, setts on the garden path
        deck = F("willa1_taras_plyty", "pavement_02", 2.12f, new Color(.76f, .70f, .62f), MeanPavement02);
        paving = F("willa1_podjazd_jodelka", "herringbone_concrete_tile_03", 2.14f, new Color(.68f, .64f, .58f), MeanHerringbone03);
        sciezka = F("willa1_sciezka_kostka", "stone_pathway_02", 1.95f, new Color(.56f, .51f, .44f), MeanStonePathway02);
        // the floors: wide old planks downstairs, table-top planks upstairs
        floorGf = Inside(F("willa1_podloga_parter", "old_wooden_floor_03", 1.2f, new Color(.60f, .43f, .28f), MeanOldWoodenFloor03), .9f);
        floorUf = Inside(F("willa1_podloga_pietro", "wood_table", 1.0f, new Color(.58f, .42f, .30f), MeanWoodTable), .9f);
        // the feature walls inside (W1Wnetrze): brick, lacquered cherry
        wCegla = Inside(F("willa1_cegla", "brick_wall_02", 2f, new Color(.66f, .56f, .47f), MeanBrickWall02), .85f);
        wPanel = Inside(F("willa1_panele_wisnia", "lacquered_cherry_wood", 1f, new Color(.32f, .20f, .13f), MeanLacqueredCherry), .9f);
        oknoParapet = alu;
    }

    // ---------------------------------------------------------------- the flat roof
    /// <summary>A flat roof over the walls x0..x1, z0..z1 (their tops at e): the slab .42 thick with its white edge, out by
    /// the overhangs (west, east, front -z, back +z), a .25 parapet .2 wide, gravel inside; the overhangs' undersides in the
    /// soffit's pine; lining (if any) the ceiling under it between the walls.</summary>
    static void DachPlaski(float x0, float x1, float z0, float z1, float e, float ovW, float ovE, float ovS, float ovN, Material lining = null)
    {
        const float gr = .42f, par = .25f, pw = .2f;
        float a0 = x0 - ovW, a1 = x1 + ovE, c0 = z0 - ovS, c1 = z1 + ovN;
        Out_(true);
        MM(elewacja, a0, a1, e, e + gr, c0, c1);
        MM(elewacja, a0, a1, e + gr, e + gr + par, c0, c0 + pw, false); MM(elewacja, a0, a1, e + gr, e + gr + par, c1 - pw, c1, false);
        MM(elewacja, a0, a0 + pw, e + gr, e + gr + par, c0 + pw, c1 - pw, false); MM(elewacja, a1 - pw, a1, e + gr, e + gr + par, c0 + pw, c1 - pw, false);
        MM(gravel, a0 + pw, a1 - pw, e + gr, e + gr + .02f, c0 + pw, c1 - pw, false);
        // the overhangs' undersides: pine boards 2 cm under the slab (their edges 2 cm in from the white edge)
        if (ovS > .05f) MM(soffit, a0 + .02f, a1 - .02f, e - .02f, e, c0 + .02f, z0, false);
        if (ovN > .05f) MM(soffit, a0 + .02f, a1 - .02f, e - .02f, e, z1, c1 - .02f, false);
        if (ovW > .05f) MM(soffit, a0 + .02f, x0, e - .02f, e, z0, z1, false);
        if (ovE > .05f) MM(soffit, x1, a1 - .02f, e - .02f, e, z0, z1, false);
        if (lining != null) { Out_(false); MM(lining, x0 + T, x1 - T, e - .03f, e, z0 + T, z1 - T, false); }
    }
    /// <summary>The black steel frame round a glazed front x0..x1 on the plane z (Forest Knoll's "large steel frames", 23.11
    /// rectangular): two posts from the ground to e and the head beam under the roof, .3 x .45, .45 proud.</summary>
    static void RamaProsta(float x0, float x1, float e, float z, float outward)
    {
        Out_(true);
        const float w = .3f, d = .45f;
        float zc = z + outward * (d * .5f + .006f);
        foreach (float x in new[] { x0 + w * .5f - .06f, x1 - w * .5f + .06f })
        {
            b.Box(stalCzarna, new Vector3(x, (e - w) * .5f, zc), new Vector3(w, e - w, d), Quaternion.identity);
            W.Kolizja(new Vector3(x, (e - w) * .5f, zc), new Vector3(w, e - w, d), Quaternion.identity);
        }
        b.Box(stalCzarna, new Vector3((x0 + x1) * .5f, e - w * .5f - .02f, zc), new Vector3(x1 - x0 + .12f, w, d), Quaternion.identity);
    }
    /// <summary>A window's aluminium sill outside (5 cm proud) and its oak board inside (4 cm proud), at the sill y of the
    /// opening a0..a1 in a wall on plane (outward: the outside's side, T thick): their tops 12-15 mm over the reveal's.</summary>
    static void Parapety(bool alongX, float plane, float outward, float a0, float a1, float y)
    {
        // wall frame: off measured into the wall from its outer face (negative: proud of it)
        void P(Material m, float o0, float o1, float y0, float y1, float e)
        {
            float n0 = plane - outward * o0, n1 = plane - outward * o1;
            if (alongX) MM(m, a0 - e, a1 + e, y0, y1, Mathf.Min(n0, n1), Mathf.Max(n0, n1), false);
            else MM(m, Mathf.Min(n0, n1), Mathf.Max(n0, n1), y0, y1, a0 - e, a1 + e, false);
        }
        // each clear of the joint of the wall's two skins (T/2 +- 4 mm) and of the glass's frame plane
        Out_(true); P(oknoParapet, -.05f, T * .5f - .012f, y - .04f, y + .012f, .03f);
        Out_(false); P(oak, T * .5f + .012f, T + .04f, y - .03f, y + .015f, .04f);
    }

    // ---------------------------------------------------------------- the rock face under the pad
    static void W1Klif()
    {
        float foot = BraxChampionsWilleDzialki.PrzodKlif - .6f, pod = PoolFloor - .45f;     // under the pool: its slab's underside
        const float sk = .324f;                                                               // the pool's slab beyond its walls
        KlifSciana(true, PadZ0, PadX0 - 1f, APoolX0 - sk, -1f, a => foot, 3);
        KlifSciana(true, PadZ0, APoolX0 - sk, APoolX1 + sk, -1f, a => foot, 4, pod);
        KlifSciana(true, PadZ0, APoolX1 + sk, PadX1 + 1f, -1f, a => foot, 5);
        KlifSciana(false, PadX0, PadZ0 - 1f, PadZ0 + 8.6f, -1f, z => BraxChampionsWilleDzialki.Uskok(1, z) - .6f, 11);
        KlifSciana(false, PadX1, PadZ0 - 1f, PadZ0 + 8.6f, 1f, z => BraxChampionsWilleDzialki.Uskok(1, z) - .6f, 17);
    }

    // ---------------------------------------------------------------- the garden: thick palms along the house, the path
    static void W1Ogrod()
    {
        Out_(true);
        // the path from the forecourt along the west lawn to the terrace (setts, 1.4 m; the lawn either side of it: ATeren)
        MM(sciezka, -19.2f, -17.8f, -.3f, 0f, -8f, 14f);
        // thick palms every 6 m: the west lawn from the terrace's edge to the back (none in the forecourt's paving, z 14..18),
        // the east lawn beside the terrace, two at the back
        W1Palmy = 0;
        foreach (float z in new[] { -20f, -14f, -8f, -2f, 4f, 10f, 22f }) { Palma(-21.6f, z); W1Palmy++; }
        foreach (float z in new[] { -20f, -14f, -8f, -2f }) { Palma(25.4f, z); W1Palmy++; }
        foreach (float x in new[] { -15f, -9.5f }) { Palma(x, 23.6f); W1Palmy++; }
    }

    // ---------------------------------------------------------------- inside: the feature walls
    static void W1Wnetrze(BraxKatalog.Ctx c, string tag)
    {
        Out_(false);
        float y = AF1;
        // every lining starts 2 cm inside the wall it covers (no face on the wall's own) and stands 4 cm proud
        float top = AE - .2f;
        float zs(float wall) => wall - .06f - .04f;
        float ze(float wall) => wall - .06f + .02f;
        // the cinema (W ground, z 3..13.7): lacquered cherry on both side walls (their faces x -15.7, -8.3) to 2.4 m
        MM(wPanel, -15.72f, -15.66f, -.02f, 2.4f, 3.06f, 13.72f, false);
        MM(wPanel, -8.34f, -8.28f, -.02f, 2.4f, 3.06f, 13.72f, false);
        // the study (E ground): its back wall (the partition z 2.5 to the gym, its door x 9.0..9.9) in brick
        MM(wCegla, 8.28f, 8.95f, -.02f, AF1 - .3f, zs(2.5f), ze(2.5f), false); MM(wCegla, 9.95f, 15.72f, -.02f, AF1 - .3f, zs(2.5f), ze(2.5f), false);
        MM(wCegla, 8.95f, 9.95f, 2.31f, AF1 - .3f, zs(2.5f), ze(2.5f), false);
        // the bedrooms' headboard walls (south faces; clear of their doors): brick in bedroom 1's, cherry in the master's and
        // the guest suite's
        MM(wCegla, -15.72f, -11.45f, y - .02f, top, zs(-.5f), ze(-.5f), false);
        MM(wPanel, 9.75f, 14.65f, y - .02f, top, zs(.4f), ze(.4f), false);
        MM(wPanel, 18.35f, 25.72f, y - .02f, top, zs(10.2f), ze(10.2f), false);
    }
}
