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
//  - the flat roofs (no gables): a thin slab with a white edge, gravel on top, no parapet, the hall's roof running on 2.5 m
//    over the terrace on its pine soffit; the glazed fronts in rectangular black steel frames;
//  - (22:32 "płaski prostokąt ... zobacz na internecie jak to robią") the upper floors of W and E cantilevered 3 m over the
//    terrace, pine louvres on their ends, outdoor rooms under them and on the garage's roof (W1Bryla);
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
    const float Dach = .32f;                       // the flat roofs' slab
    public static int W1Palmy;

    static void W1Materialy()
    {
        var F = (Func<string, string, float, Color, Vector3, Material>)BraxChampionsFoundation.Fotoskan;
        // the render: smooth warm white, inside and out (the scanned stucco read as raw concrete; the old paint had no grain)
        elewacja = Tynk("willa1_elewacja_biel", new Color(.96f, .95f, .92f), 2f); BraxChampionsInteriorLight.Facade(elewacja);
        render = elewacja;
        // the walls inside the island villa's cream (owner 23.11 23:10 "Pamiętaj, nie ma szarych ścian")
        renderIn = Inside(Tynk("willa1_sciany_krem", new Color(.93f, .88f, .79f), 2f), .80f);
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
    /// <summary>A flat roof over the walls x0..x1, z0..z1 (their tops at e): a thin slab (.32) with its white edge, out by
    /// the overhangs (west, east, front -z, back +z), gravel on it 25 cm in from the edge (22:32: the thin plane the modern
    /// villas float over their glass - no parapet); the overhangs' undersides in the soffit's pine; lining (if any) the
    /// ceiling under it between the walls.</summary>
    static void DachPlaski(float x0, float x1, float z0, float z1, float e, float ovW, float ovE, float ovS, float ovN, Material lining = null)
    {
        const float gr = Dach, pw = .25f;
        float a0 = x0 - ovW, a1 = x1 + ovE, c0 = z0 - ovS, c1 = z1 + ovN;
        Out_(true);
        MM(elewacja, a0, a1, e, e + gr, c0, c1);
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
    static void RamaProsta(float x0, float x1, float e, float z, float outward, float y0 = 0f)
    {
        Out_(true);
        const float w = .3f, d = .45f;
        float zc = z + outward * (d * .5f + .006f);
        foreach (float x in new[] { x0 + w * .5f - .06f, x1 - w * .5f + .06f })
        {
            b.Box(stalCzarna, new Vector3(x, (e - w + y0) * .5f, zc), new Vector3(w, e - w - y0, d), Quaternion.identity);
            if (y0 < .1f) W.Kolizja(new Vector3(x, (e - w + y0) * .5f, zc), new Vector3(w, e - w - y0, d), Quaternion.identity);
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
        // 23.11 23:10 "nie ma szarych ścian": the guest suite's west wall is the E wing's outside (its white render, read grey
        // indoors) - the cream plaster over it, 1 cm proud, its ends in the walls, floor and ceiling; clear of the door to
        // the E wing (11.6..12.5, its architraves 8 cm on either side cover the ends)
        foreach (var (z0, z1) in new[] { (4.28f, 10.16f), (10.24f, 11.54f), (12.56f, 13.72f) })
            MM(renderIn, 15.98f, 16.01f, y - .02f, top + .02f, z0, z1, false);
        MM(renderIn, 15.98f, 16.01f, y + 2.30f, top + .01f, 11.50f, 12.60f, false);       // over the door (its head's architrave)
        // the glass links (x -8..-5, 5..8, the stairs and landings): their side walls are the wings' and the hall's outsides
        // (the white render, grey indoors) - the cream plaster over them clear of the glass fronts (z -2.85, 11.85), round
        // the openings; under their roofs the ceiling (W1Wnetrze's links: Tynkuj; the downlights under it at 5.98)
        foreach (float sd in new[] { -1f, 1f })
        {
            float wing = sd * 8f, hall = sd * 5f, inW = -sd, inH = sd;        // the link lies between: off the wing toward the hall
            float gEnd = sd < 0f ? 8.96f : 11.72f;                             // the W link's powder room (z 9.06..11.7) has its own
            var drzwi = new Vector4(-2.4f, -1.4f, 0f, 2.4f);
            // 1 mm over the floors (no face on theirs), the two storeys meeting 5 cm under the landing's floor (in its slab),
            // the landing's door's opening down into the slab (no ledge on the floor's face)
            const float u = .001f, mid = AF1 - .05f;
            if (sd < 0f) Tynkuj(wing, inW, -2.80f, gEnd, u, mid, drzwi);
            else Tynkuj(wing, inW, -2.80f, gEnd, u, mid, drzwi, new Vector4(9.4f, 10.4f, 0f, 2.4f));
            Tynkuj(wing, inW, -2.80f, 11.80f, mid, 5.99f, new Vector4(7.6f, 8.5f, mid, AF1 + 2.3f));
            Tynkuj(hall, inH, -2.80f, gEnd, u, mid, sd < 0f ? new Vector4(5.6f, 8.6f, 0f, 3.0f) : new Vector4(7.5f, 10.5f, 0f, 3.0f));
            Tynkuj(hall, inH, -2.80f, 11.80f, mid, 5.99f);
            float xa = Mathf.Min(wing, hall), xb = Mathf.Max(wing, hall);
            MM(ceiling, xa + .004f, xb - .004f, 5.98f, 6.0f, -2.80f, 11.80f, false);
            // the hall's 3 m openings to the links (no doors): their reveals (the outer half of the wall the render) lined in
            // the plaster, 1 mm off the reveals, flush with the link's plaster, out 1.1 cm past the hall's face
            float o0 = sd < 0f ? 5.6f : 7.5f, o1 = sd < 0f ? 8.6f : 10.5f, oh = 3.0f;
            float r0 = Mathf.Min(hall + inH * .012f, hall - inH * (T + .011f)), r1 = Mathf.Max(hall + inH * .012f, hall - inH * (T + .011f));
            MM(renderIn, r0, r1, u, oh - .012f, o0 + .001f, o0 + .012f, false); MM(renderIn, r0, r1, u, oh - .012f, o1 - .012f, o1 - .001f, false);
            MM(renderIn, r0, r1, oh - .012f, oh - .001f, o0 + .001f, o1 - .001f, false);
        }
    }

    // ---------------------------------------------------------------- 22:32 "zobacz na internecie jak to robią"
    // What the hillside villas do (SAOTA's Hillside House, Los Angeles: the roof planes "pushed as far forward as possible"
    // into covered outdoor rooms; Studio Saxe's Villa Nuri: heavy stone at the base, lighter timber and glass as it rises,
    // planted terraces, the pool floating out): here the upper floors cantilevered over the terrace (AFront), the thin roof
    // planes, vertical pine louvres on the upper floors' ends, outdoor rooms under the cantilevers and on the garage's roof.
    static void W1Bryla()
    {
        // the louvres: vertical pine fins 6 x 10 cm every 16 cm, 2 cm off the render, clear of the windows
        Lamele(-16f, -1f, -8f - WS + .1f, 14f - .1f, new Vector2(-6.3f, -2.1f), new Vector2(7.5f, 9.3f));
        Lamele(16f, 1f, -8f - WS + .1f, 4f - .1f, new Vector2(-6.1f, -2.4f));
        // under the cantilevers: a teak sofa against the glass, two armchairs, a stone table, on a rug of the terrace's stone
        foreach (float cx in new[] { -12f, 12f })
        {
            var lc = new Vector3(cx, 0f, -9.9f);
            Lawka(lc + new Vector3(0f, 0f, 1.0f), Vector3.back, 2.4f);
            Lawka(lc + new Vector3(-1.75f, 0f, 0f), Vector3.right, .9f); Lawka(lc + new Vector3(1.75f, 0f, 0f), Vector3.left, .9f);
            Out_(true);
            b.RoundedBox(stone, lc + new Vector3(0f, .19f, -.1f), new Vector3(1.2f, .38f, .7f), .02f, Quaternion.identity, 2);
            W.Kolizja(lc + new Vector3(0f, .19f, -.1f), new Vector3(1.2f, .38f, .7f), Quaternion.identity);
        }
        // the garage's roof a terrace: pine decking, glass round it, a black steel pergola with pine slats, three loungers
        float y = AE + Dach;
        Out_(true);
        MM(soffit, 16.4f, 26.1f, y, y + .04f, 3.9f, 14.1f, false);
        // 2 cm in off the deck's edges; the east run's ends buried in the long runs' bases, theirs in its (no faces flush)
        Balustrada(true, 3.9f + .06f, 16.42f, 26.06f, y + .04f); Balustrada(true, 14.1f - .06f, 16.42f, 26.06f, y + .04f);
        Balustrada(false, 26.1f - .06f, 3.93f, 14.07f, y + .04f);
        float px0 = 17.0f, px1 = 21.6f, pz0 = 4.6f, pz1 = 9.4f, ph = y + .04f + 2.55f;
        foreach (float x in new[] { px0, px1 }) foreach (float z in new[] { pz0, pz1 })
            { MM(stalCzarna, x - .06f, x + .06f, y + .04f, ph, z - .06f, z + .06f); }
        foreach (float z in new[] { pz0, pz1 }) MM(stalCzarna, px0 - .2f, px1 + .2f, ph, ph + .16f, z - .05f, z + .05f, false);
        for (float x = px0 - .15f; x <= px1 + .16f; x += .3f) MM(soffit, x - .03f, x + .03f, ph + .16f, ph + .26f, pz0 - .25f, pz1 + .25f, false);
        Lawka(new Vector3((px0 + px1) * .5f, y + .04f, pz0 + 1.3f), Vector3.forward, 2.4f);
        Lezaki(new Vector3(18.0f, y + .04f, 12.3f), 3, 2.2f, Vector3.back);
        W.Lampa("dach garażu | pergola", new Vector3((px0 + px1) * .5f, ph - .2f, (pz0 + pz1) * .5f), .5f, 6f * K, new Color(1f, .86f, .64f));
    }
    /// <summary>The cream plaster (renderIn) over a wall's face on the plane x = px toward the room (s: the room's side), z
    /// z0..z1, y y0..y1, round the openings (x..y along z, z..w up): 1.1 cm thick, 1 mm off the face (no face on the wall's
    /// own, none on its reveals'), split on the openings' edges.</summary>
    static void Tynkuj(float px, float s, float z0, float z1, float y0, float y1, params Vector4[] holes)
    {
        Out_(false);
        var zs = new System.Collections.Generic.List<float> { z0, z1 }; var ys = new System.Collections.Generic.List<float> { y0, y1 };
        foreach (var o in holes) { zs.Add(Mathf.Clamp(o.x, z0, z1)); zs.Add(Mathf.Clamp(o.y, z0, z1)); ys.Add(Mathf.Clamp(o.z, y0, y1)); ys.Add(Mathf.Clamp(o.w, y0, y1)); }
        zs.Sort(); ys.Sort();
        float a = px + s * .001f, c = px + s * .012f;
        for (int i = 0; i + 1 < zs.Count; i++)
        {
            if (zs[i + 1] - zs[i] < .01f) continue;
            for (int j = 0; j + 1 < ys.Count; j++)
            {
                if (ys[j + 1] - ys[j] < .01f) continue;
                float cz = (zs[i] + zs[i + 1]) * .5f, cy = (ys[j] + ys[j + 1]) * .5f; bool hole = false;
                foreach (var o in holes) if (cz > o.x && cz < o.y && cy > o.z && cy < o.w) { hole = true; break; }
                if (!hole) MM(renderIn, Mathf.Min(a, c), Mathf.Max(a, c), ys[j], ys[j + 1], zs[i], zs[i + 1], false);
            }
        }
    }
    /// <summary>Vertical pine fins on the upper floor of a wall on the plane x = px (its outer face; s = outward), along z
    /// z0..z1, every 16 cm, skipping the gaps (the windows).</summary>
    static void Lamele(float px, float s, float z0, float z1, params Vector2[] gaps)
    {
        Out_(true);
        for (float z = z0 + .03f; z < z1 - .03f; z += .16f)
        {
            bool skip = false; foreach (var g2 in gaps) if (z > g2.x && z < g2.y) { skip = true; break; }
            if (skip) continue;
            float a = px + s * .02f, c = px + s * .12f;
            MM(soffit, Mathf.Min(a, c), Mathf.Max(a, c), AF1 + .05f, AE - .06f, z - .03f, z + .03f, false);
        }
    }
}
