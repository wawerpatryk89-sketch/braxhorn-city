using System;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 2 on the island, finished (owner 23.11 20:11-20:13, his 11 screenshots of it: "za mało premium
// wygląda", "elewacja jakaś, kostka ... cegła czy coś na ścianę tutaj by się przydała ... w środku zrób porządek ... Wszystko
// wygląda jakby było gipsowane, ściany i sufity", "na niektórych ścianach jakoś można dać cegiełkę ozdobną"). Every surface
// from his own Poly Haven list (MATERIALY-WLASCICIELA, 23.11, all 4K):
//  - walls and ceilings: Plastered Wall 05 (a real trowelled lime plaster; the old paint, Beige Wall 001, has no grain at all:
//    its brightest 1 % is its mean - the "gips" he saw), white;
//  - feature walls: the fireplace wall in Rustic Stone Wall floor to ceiling, Whitewashed Brick on the WC and pantry blocks
//    (to the living room and the kitchen) and behind the second bedroom's bed, Dark Paneled Wood by the stair, behind the
//    master's and the third bedroom's beds, the study wall with oak shelves and books, the corridor's wainscot;
//  - the living floor's ceiling in oak planks (the soffit outside carried in through the glass), hung 25 mm under the plaster,
//    its downlights with it;
//  - floors: Wooden Floor 02 downstairs (the stair's treads too), Wooden Floor 01 upstairs, Rock Tile Floor (light granite
//    slabs) in the hall, the kitchen and the showers, Square Tiles 03 in the bathrooms; Rustic Stone Wall behind the master's
//    tub;
//  - outside: the ground floor's walls and the entrance in Rustic Stone Wall (warm sandstone under the ivory upper floor in
//    Plastered Wall 05), the terrace and the stair to the beach in Rock Tile Floor, the retaining walls in Rustic Stone Wall
//    too (21:02 "te szare ściany mi się kompletnie nie podobają": they were the grey Rock Wall 16 and Stone Wall 04, and the
//    plaster near-white, which the preview drew blue-grey);
//    the stair to the beach 4 m wide between stone cheek walls with steel handrails (it had glass panes standing out on the
//    slope); the steel light, stainless (it read black: a fully metallic surface in this shader reflects only the dark sky
//    hemisphere).
// Every lining starts 2 cm inside the wall it covers (no face on the wall's own: nothing to flicker) and carries no collider
// (the wall has it).
public static partial class BraxChampionsWille
{
    // the means of the new photo-scans (sRGB 0-255, measured 23.11 on the 1k maps; their brightest 1 %: Foundation.Jasne)
    public static readonly Vector3 MeanPlastered05 = new Vector3(69, 73, 77), MeanWhitewashedBrick = new Vector3(166, 153, 140),
        MeanRusticStone = new Vector3(119, 102, 76), MeanDarkPaneledWood = new Vector3(42, 21, 17), MeanWoodenFloor01 = new Vector3(177, 126, 78),
        MeanWoodenFloor02 = new Vector3(184, 136, 98), MeanSquareTiles03 = new Vector3(110, 97, 85),
        MeanRockTileFloor = new Vector3(148, 141, 131), MeanRoughLinen = new Vector3(145, 171, 205);
    public const float WSufitDol = .025f;        // the oak ceiling of the living floor, under the plaster
    static Material wCegla, wKamien, wPanel, wPlytki, wGranit, wDrewnoSufit, wDywan, wKsiazki0, wKsiazki1, wLen;

    // Plastered Wall 05 is a dark, very even plaster (its brightest 1 % only 12 % over its mean): brought to white with no cap
    // (Fotoskan caps the gain so a stone's light veins never clip; here there is nothing to clip)
    static Material Tynk(string id, Color target, float metres)
    {
        float G(float t, float m) => Mathf.Min(16f, Mathf.GammaToLinearSpace(Mathf.Clamp01(t)) / Mathf.GammaToLinearSpace(m / 255f));
        var tint = new Color(Mathf.LinearToGammaSpace(G(target.r, MeanPlastered05.x)), Mathf.LinearToGammaSpace(G(target.g, MeanPlastered05.y)),
                             Mathf.LinearToGammaSpace(G(target.b, MeanPlastered05.z)), 1f);
        return BraxChampionsFoundation.SourceMaterial(id, "plastered_wall_05", metres, tint);
    }
    /// <summary>The island's terrace and paths: Rock Tile Floor, light granite slabs.</summary>
    public static Material GranitTarasu() =>
        BraxChampionsFoundation.Fotoskan("willa2_taras_granit", "rock_tile_floor", 1.96f, new Color(.80f, .78f, .74f), MeanRockTileFloor);
    /// <summary>The island's retaining walls (the pad's, the site's, the stair's cheeks): Rustic Stone Wall, warm sandstone
    /// (owner 23.11 21:02 "te szare ściany mi się kompletnie nie podobają": they were Stone Wall 04, grey).</summary>
    public static Material KamienMuruWyspy() =>
        BraxChampionsFoundation.Fotoskan("willa2_mur_piaskowiec", "rustic_stone_wall", 1.52f, new Color(.72f, .63f, .50f), MeanRusticStone);

    // villa 2's finish (first in PlanWyspa: over Materials(2))
    static void WPremiumMaterialy()
    {
        var F = (Func<string, string, float, Color, Vector3, Material>)BraxChampionsFoundation.Fotoskan;
        // owner 23.11 21:02 ("te szare ściany mi się kompletnie nie podobają"): no grey wall in the house - the plaster warm
        // ivory inside and out, the ceilings warm white, the stone warm sandstone (Rustic Stone Wall, as the fireplace wall)
        renderIn = Inside(Tynk("willa2_sciany_krem", new Color(.93f, .88f, .79f), 2f), .80f);
        ceiling = Inside(Tynk("willa2_sufit_cieply", new Color(.97f, .95f, .90f), 2f), .95f);
        render = Tynk("willa2_elewacja_krem", new Color(.96f, .93f, .87f), 2f); BraxChampionsInteriorLight.Facade(render);
        stone = F("willa2_elewacja_piaskowiec", "rustic_stone_wall", 1.52f, new Color(.70f, .61f, .48f), MeanRusticStone); BraxChampionsInteriorLight.Facade(stone);
        deck = GranitTarasu();
        murOporowy = KamienMuruWyspy();
        floorGf = Inside(F("willa2_podloga_parter", "wooden_floor_02", 1.94f, new Color(.64f, .48f, .35f), MeanWoodenFloor02), .9f);
        floorUf = Inside(F("willa2_podloga_pietro", "wooden_floor_01", 1.99f, new Color(.62f, .45f, .30f), MeanWoodenFloor01), .9f);
        marbleIn = wPlytki = Inside(F("willa2_plytki", "square_tiles_03", 2f, new Color(.80f, .78f, .74f), MeanSquareTiles03), .9f);
        stoneIn = wKamien = Inside(F("willa2_kamien_sciana", "rustic_stone_wall", 1.52f, new Color(.64f, .58f, .49f), MeanRusticStone), .9f);
        wGranit = Inside(F("willa2_posadzka_granit", "rock_tile_floor", 1.96f, new Color(.82f, .80f, .76f), MeanRockTileFloor), .9f);
        wCegla = Inside(F("willa2_cegla_bielona", "whitewashed_brick", 2f, new Color(.76f, .72f, .67f), MeanWhitewashedBrick), .85f);
        wPanel = Inside(F("willa2_panele_drewno", "dark_paneled_wood", 1.4f, new Color(.28f, .17f, .12f), MeanDarkPaneledWood), .9f);
        wDrewnoSufit = Inside(BraxChampionsFoundation.SourceMaterial("willa2_sufit_dab", "oak_wood_planks", 2.4f, new Color(1.0f, .94f, .86f)), .9f);
        wDywan = Inside(F("willa2_dywan_welna", "curly_teddy_natural", .34f, new Color(.84f, .80f, .74f), BraxChampionsFoundation.MeanCurlyTeddy), .9f);
        wLen = Inside(F("willa2_zaslony_len", "rough_linen", .27f, new Color(.93f, .92f, .89f), MeanRoughLinen), .9f);
        // the book spines: leather (two browns) and linen (navy, cream)
        wKsiazki0 = Inside(BraxChampionsFoundation.SourceMaterial("willa2_ksiazki_skora", "brown_leather", .6f, new Color(.80f, .62f, .48f)), .9f);
        wKsiazki1 = Inside(BraxChampionsFoundation.SourceMaterial("willa2_ksiazki_len", "hessian_230", .4f, new Color(.42f, .52f, .80f)), .9f);
        // the steel: stainless, light (its reflection of the room's light, not the dark sky hemisphere only)
        steel = BraxChampionsHarbour.Surface("willa2_stal_nierdzewna", new Color(.78f, .78f, .79f), .25f, .55f);
        steel.SetFloat("_ReflectionStrength", 1f);
    }

    // a lining on a wall (from 2 cm inside it), no collider
    static void Okl(Material m, float x0, float x1, float y0, float y1, float z0, float z1) { Out_(false); MM(m, x0, x1, y0, y1, z0, z1, false); }

    // the finishes inside (after Wnetrze and Wyposazenie: the rooms and their furniture stand)
    static void WPremiumWnetrze()
    {
        Out_(false);
        float wx = GX0 + T, zf = 5f - .06f;                                   // the west wall's face; the WC and pantry blocks' south faces
        // ---- ground floor
        // the fireplace wall: stone from the glass to the stair, floor to ceiling (the breast and its bench are stone too)
        Okl(wKamien, wx - .02f, wx + .04f, -.02f, H0, GZ0 + .06f, -3.4f);
        Okl(wKamien, wx - .02f, wx + .04f, -.02f, H0, 2.2f, StairZ0 - .1f);
        // a warm light line in the oak ceiling over it (a wall washer)
        b.Box(downlight, new Vector3(wx + .10f, H0 - WSufitDol - .005f, (GZ0 + StairZ0) * .5f), new Vector3(.03f, .014f, StairZ0 - GZ0 - .4f), Quaternion.identity);   // 12 mm proud
        // whitewashed brick on the WC block (its door kept, the brick over it) and on the pantry block, round their corners
        Okl(wCegla, -5.60f, -5.0f, -.02f, H0, zf - .04f, zf + .02f); Okl(wCegla, -4.05f, -2.90f, -.02f, H0, zf - .04f, zf + .02f);
        Okl(wCegla, -5.0f, -4.05f, 2.31f, H0, zf - .04f, zf + .02f);
        Okl(wCegla, 8.90f, 10.55f, -.02f, H0, zf - .04f, zf + .02f); Okl(wCegla, 11.50f, GX1 - T + .02f, -.02f, H0, zf - .04f, zf + .02f);
        Okl(wCegla, 10.55f, 11.50f, 2.31f, H0, zf - .04f, zf + .02f);
        // dark wood by the stair: the WC block's side to the stair's landing
        Okl(wPanel, -5.60f, -5.54f, -.02f, H0, zf + .02f, GZ1 - T + .02f);
        // the hall's and the kitchen's floor in tiles, 6 mm over the oak (no face on the oak's own)
        Okl(wGranit, -3f + .06f, 9f - .06f, -.002f, .006f, 5f, GZ1 - T);                          // to the front door's threshold
        Okl(wGranit, 7.2f, GX1 - T + .02f, -.002f, .006f, GZ0 + .1f, 0f);                          // from the glass's threshold
        // the oak ceiling over the living room, the dining and the kitchen (25 mm under the plaster, its top in the slab)
        Okl(wDrewnoSufit, wx, GX1 - T, H0 - WSufitDol, H0 + .01f, GZ0 + .12f, zf);                 // from behind the glass's frames

        // ---- upper floor
        float y = FFL1, top = Roof, zs0 = 1f - .06f - .04f, zs1 = 1f - .06f + .02f;   // the bedrooms' wall to the corridor: its south face
        // bedroom 2 (west): whitewashed brick behind the bed, round its door
        Okl(wCegla, UX0 + T - .02f, -9.05f, y - .02f, top, zs0, zs1); Okl(wCegla, -9.05f, -8.05f, y + 2.31f, top, zs0, zs1); Okl(wCegla, -8.05f, -7f - .04f, y - .02f, top, zs0, zs1);
        // bedroom 3: dark wood behind the bed, round its door
        Okl(wPanel, -7f + .04f, -1.55f, y - .02f, top, zs0, zs1); Okl(wPanel, -1.55f, -.55f, y + 2.31f, top, zs0, zs1); Okl(wPanel, -.55f, 2f - .04f, y - .02f, top, zs0, zs1);
        // the master: dark wood behind the bed (the wall to its bathroom)
        Okl(wPanel, 9f - .04f, UX1 - T + .02f, y - .02f, top, zs0, zs1);
        // the master's study corner: the wall over the desk in dark wood, three oak shelves with books
        Okl(wPanel, 2f + .02f, 2f + .10f, y - .02f, top, -2.9f, zs0);
        var rnd = new System.Random(2);
        for (int s = 0; s < 3; s++)
        {
            float sy = y + 1.30f + s * .36f;
            b.RoundedBox(oak, new Vector3(2.24f, sy - .0125f, -1.0f), new Vector3(.28f, .025f, 1.5f), .004f, Quaternion.identity, 1);
            for (float z = -1.70f; z < -.32f;)
            {
                float w = .025f + (float)rnd.NextDouble() * .03f, h = .18f + (float)rnd.NextDouble() * .08f, d = .16f + (float)rnd.NextDouble() * .06f;
                if (rnd.NextDouble() < .12) { z += .06f; continue; }                    // a gap in the row
                var m = rnd.NextDouble() < .55 ? wKsiazki0 : rnd.NextDouble() < .5 ? wKsiazki1 : walnut;
                b.Box(m, new Vector3(2.10f + d * .5f + .01f, sy + h * .5f, z + w * .5f), new Vector3(d, h, w - .003f), Quaternion.identity);
                z += w;
            }
        }
        // the corridor: a dark wood wainscot (1.05 m) with an oak rail, both sides, round the doors
        void Lamperia(bool polnoc, float a0, float a1)
        {
            float f0 = polnoc ? 1f + .06f - .02f : 3f - .06f - .04f, f1 = polnoc ? 1f + .06f + .04f : 3f - .06f + .02f;    // from 2 cm inside
            Okl(wPanel, a0, a1, y - .02f, y + 1.05f, f0, f1);
            float r0 = polnoc ? f1 - .03f : f0 - .015f, r1 = polnoc ? f1 + .015f : f0 + .03f;
            Okl(oak, a0, a1, y + 1.05f, y + 1.09f, r0, r1);
        }
        Lamperia(true, UX0 + T - .02f, -9.05f); Lamperia(true, -8.05f, -1.55f); Lamperia(true, -.55f, 2f - .04f);
        Lamperia(false, UX0 + T - .02f, -15.25f); Lamperia(false, -14.35f, -13.4f + .04f);
        Lamperia(false, -5.04f, -1.55f); Lamperia(false, -.55f, 3.95f); Lamperia(false, 4.95f, 9f - .04f);
        // the master's bathroom: stone behind the free-standing tub (over its tiles)
        Okl(wKamien, 10.4f, 14.6f, y + .005f, y + 2.62f, 1.068f, 1.10f);                          // from inside the tiles

        // linen curtains drawn to the sides of the glass: each bedroom, the living room's two ends
        float zu = UGlass + .15f;
        foreach (var (a, c) in new[] { (-15.6f, -15.0f), (-7.75f, -7.15f), (-6.85f, -6.25f), (1.25f, 1.85f), (2.15f, 2.75f), (15.0f, 15.6f) })
            Zaslona(a, c, zu, y + .01f, top - .03f);
        foreach (var (a, c) in new[] { (-13.6f, -12.9f), (12.9f, 13.6f) })
            Zaslona(a, c, GZ0 + .15f, .01f, H0 - WSufitDol - .03f);
        // wall lights in the corridor over the wainscot: a black plate, its light thrown up and down (no lamp of its own)
        foreach (float x in new[] { -14.3f, -10.0f, -5.0f, .6f })
        {
            b.Box(black, new Vector3(x, y + 1.95f, 1.06f + .03f), new Vector3(.12f, .30f, .06f), Quaternion.identity);
            foreach (float dy in new[] { -.155f, .155f }) b.Box(downlight, new Vector3(x, y + 1.95f + dy, 1.06f + .045f), new Vector3(.08f, .012f, .03f), Quaternion.identity);
        }
    }

    /// <summary>A linen curtain drawn aside: pleated (12 cm folds, 5 cm deep) from just over the floor to its track under the
    /// ceiling, seen from both sides.</summary>
    static void Zaslona(float xa, float xb, float z, float y0, float y1)
    {
        Out_(false);
        int folds = Mathf.Max(2, Mathf.RoundToInt((xb - xa) / .12f));
        Vector3 P(float u, float v) => new Vector3(xa + u * (xb - xa), y0 + v * (y1 - y0), z + .025f * Mathf.Sin(u * folds * 2f * Mathf.PI));
        b.Surface(wLen, P, folds * 6, 1); b.Surface(wLen, P, folds * 6, 1, true);
        b.Box(alu, new Vector3((xa + xb) * .5f, y1 + .015f, z), new Vector3(xb - xa + .1f, .03f, .05f), Quaternion.identity);   // the track
    }

    /// <summary>A desk lamp: a round black base, a brushed steel stem and arm, a cone shade lit inside (a warm light).</summary>
    static void LampkaBiurko(Vector3 p)
    {
        Out_(false);
        b.Cylinder(black, p, .07f, .015f, 20, Quaternion.identity, true);
        b.Cylinder(steel, p + Vector3.up * .015f, .008f, .38f, 10, Quaternion.identity, true);
        b.Tube(steel, p + Vector3.up * .39f, p + new Vector3(.16f, .44f, 0f), .007f, 8);
        var shade = new[] { new Vector2(.020f, .0f), new Vector2(.075f, -.10f), new Vector2(.070f, -.10f), new Vector2(.016f, .0f) };
        b.Lathe(black, p + new Vector3(.17f, .47f, 0f), shade, 20, Quaternion.identity, true, false);
        b.Cylinder(downlight, p + new Vector3(.17f, .372f, 0f), .06f, .004f, 16, Quaternion.identity, true, true);
        W.Lampa(Nazwy[1] + " | lampka na biurku", p + new Vector3(.17f, .30f, 0f), .35f, 2.5f * K, new Color(1f, .86f, .64f));
    }

    /// <summary>A rug of real wool (Curly Teddy, natural), 12 mm on the floor.</summary>
    static void Dywan(Vector3 c, float sx, float sz)
    {
        Out_(false);
        b.RoundedBox(wDywan, c + Vector3.up * .006f, new Vector3(sx, .012f, sz), .005f, Quaternion.identity, 2);
    }
}
