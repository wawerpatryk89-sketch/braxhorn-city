using System;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 3 on the seafront in old brick (owner 23.11 23:57-24.11 00:00: "willa nad morzem. Tam musi być tak
// samo zrobione porządnie ... I mają mieć prywatną plażę", "Zrób starą cegłę elewację i w ten sposób luksusową", "do willi
// palmy, zrób taki stare, te mury, ale taki luksus"). The house keeps its Mediterranean form (his photo of 17.11: hip roofs
// in clay tiles, arches, the balcony on columns, round windows); its finish now as the luxury old-brick villas (the Tuscan
// and Mediterranean-revival brick villas: old red brick walls, the openings, corners and floor line in pale stone, cream
// cornices under tiled eaves, walnut windows, black iron):
//  - the walls in old red brick (Poly Haven Medieval Red Brick, his list, 4K, its real 2 m tile), the chimneys too;
//  - the stone: travertine quoins on every outside corner, a string course at the upper floor's line; the travertine
//    surrounds, keystones and sills round every arched opening (BraxChampionsWilleKitSrodziemne) stay, the cornice and the
//    eaves' soffit cream;
//  - the pad's retaining walls in old stone (Castle Wall Variation, his list) under the travertine coping;
//  - inside, the walls the island villa's cream (owner 23.11 23:10: "nie ma szarych ścian");
//  - the private beach before it ("I mają mieć prywatną plażę", N3Plaza): from the beach stair a teak boardwalk to a teak
//    deck on the sand - a cabana with two daybeds under white canvas, a teak lounge, a shower - loungers under parasols
//    along the dry sand, palms on its back edge, and old stone walls closing it: the west wall from the drive down into the
//    sea (the region's beach beyond it, "PLAŻA PRYWATNA" on its face), the east one off the fence's end into the sea.
public static partial class BraxChampionsWille
{
    // the means of the photo-scans (sRGB 0-255, measured 24.11 on the 1k maps; their brightest 1 %: Foundation.Jasne)
    public static readonly Vector3 MeanMedievalRedBrick = new Vector3(130, 99, 80), MeanCastleWall = new Vector3(137, 127, 106);
    static Material cegla, murStary;

    /// <summary>The old stone of the seafront villa's walls (the pad's retaining walls; the site's side walls too).</summary>
    public static Material MurStaryNabrzeza() =>
        BraxChampionsFoundation.Fotoskan("willa3_mur_stary", "castle_wall_varriation", 2f, new Color(.66f, .58f, .46f), MeanCastleWall);

    static void N3Materialy()
    {
        var F = (Func<string, string, float, Color, Vector3, Material>)BraxChampionsFoundation.Fotoskan;
        // the old red brick in its own colour (the target its mean: the photo as it is, a warm old red with its lime mortar)
        cegla = F("willa3_cegla_stara", "medieval_red_brick", 2f, new Color(130f / 255f, 99f / 255f, 80f / 255f), MeanMedievalRedBrick);
        BraxChampionsInteriorLight.Facade(cegla);
        // the old stone walls, warmed to honey (the scan's grey-beige read cold: "nie ma szarych ścian")
        murStary = MurStaryNabrzeza(); BraxChampionsInteriorLight.Facade(murStary);
        murOporowy = murStary;
        // inside: the island villa's cream walls (its Tynk, the same target), its warm oak floor downstairs (the stone tiles read
        // grey), the fireplace in the old brick (its mantel marble: NWnetrze)
        renderIn = Inside(Tynk("willa3_sciany_krem", new Color(.93f, .88f, .79f), 2f), .80f);
        floorGf = Inside(F("willa3_podloga_parter", "wooden_floor_02", 1.94f, new Color(.64f, .48f, .35f), MeanWoodenFloor02), .9f);
        stoneIn = Inside(F("willa3_kominek_cegla", "medieval_red_brick", 2f, new Color(130f / 255f, 99f / 255f, 80f / 255f), MeanMedievalRedBrick), .9f);
    }

    // ---------------------------------------------------------------- the stone on the brick
    /// <summary>Travertine quoins on an outside corner (cx, cz) whose faces look sx (along x) and sz (along z): courses 30 cm
    /// with a 1 cm joint, alternately 45 / 25 cm along each face, 3 cm proud of the brick (their backs 1 cm in it), from y0 to
    /// y1. Each course two blocks: the one on the z face runs round the corner, the one on the x face stops at its back.</summary>
    static void Naroznik(float cx, float cz, float sx, float sz, float y0, float y1)
    {
        Out_(true);
        const float h = .30f, d = .03f, wpusc = .01f, dlugi = .45f, krotki = .25f;
        int i = 0;
        for (float y = y0; y + h <= y1 + .001f; y += h, i++)
        {
            float la = i % 2 == 0 ? dlugi : krotki, lb = i % 2 == 0 ? krotki : dlugi, ya = y + .005f, yb = y + h - .005f;
            float ax0 = cx - sx * la, ax1 = cx + sx * d, az0 = cz - sz * wpusc, az1 = cz + sz * d;
            MM(deck, Mathf.Min(ax0, ax1), Mathf.Max(ax0, ax1), ya, yb, Mathf.Min(az0, az1), Mathf.Max(az0, az1), false);
            float bx0 = cx - sx * wpusc, bx1 = cx + sx * d, bz0 = cz - sz * lb, bz1 = cz - sz * wpusc;
            MM(deck, Mathf.Min(bx0, bx1), Mathf.Max(bx0, bx1), ya, yb, Mathf.Min(bz0, bz1), Mathf.Max(bz0, bz1), false);
        }
    }

    /// <summary>The seafront villa's stone on its brick: the quoins on the outside corners (the house's front corners over the
    /// wings' eaves, its back west corner whole; the wings' free corners), the string course at the upper floor's line on the
    /// house's front and back (under the balcony's slab, over the ground floor's arches and lanterns).</summary>
    static void N3Kamien()
    {
        // the house (x -9..9, z 0..13, eaves KE): the front corners over the wings' eaves, the back west corner from the ground;
        // their last course into the cornice under the eaves (20 cm under the walls' top: no face on the wall's top)
        Naroznik(-9f, 0f, -1f, -1f, 4.8f, KE - .2f); Naroznik(9f, 0f, 1f, -1f, 5.4f, KE - .2f);
        Naroznik(-9f, 13f, -1f, 1f, 0f, KE - .2f);
        // the west wing (x -17..-9, z -6..8, eaves NEw): its front corners and its back west corner
        Naroznik(-17f, -6f, -1f, -1f, 0f, NEw); Naroznik(-9f, -6f, 1f, -1f, 0f, NEw); Naroznik(-17f, 8f, -1f, 1f, 0f, NEw);
        // the east wing (x 9..17, z -5..13, eaves NEe): its front corners and its back east corner
        Naroznik(9f, -5f, -1f, -1f, 0f, NEe); Naroznik(17f, -5f, 1f, -1f, 0f, NEe); Naroznik(17f, 13f, 1f, 1f, 0f, NEe);
        // the string course: 17 cm of travertine 4 cm proud, its top 3 cm under the upper floor (the balcony's slab over it)
        Out_(true);
        // (its ends 1 cm inside the house's corners where the wings' walls butt against it: no end face on the side walls'
        // planes; at the back it wraps the free west corner)
        MM(deck, -8.99f, 8.99f, KF1 - .2f, KF1 - .03f, -.04f, .01f, false);
        MM(deck, -9.04f, 8.99f, KF1 - .2f, KF1 - .03f, 12.99f, 13.04f, false);
    }

    // ---------------------------------------------------------------- the private beach (24.11)
    /// <summary>The site's ground under the villa's local (x, z), in the villa's frame (the pad's top 0).</summary>
    static float N3Grunt(float x, float z)
    {
        var d = BraxChampionsWilleDzialki.Dzialki[2];
        float sn = Mathf.Sin(d.yaw * Mathf.Deg2Rad), cs = Mathf.Cos(d.yaw * Mathf.Deg2Rad);
        return BraxChampionsWilleDzialki.Grunt(d, d.srodek.x + x * cs + z * sn, d.srodek.y - x * sn + z * cs) - d.padY;
    }
    static float N3Max(float x0, float x1, float z0, float z1)
    {
        float m = float.MinValue;
        for (int i = 0; i <= 4; i++) for (int j = 0; j <= 4; j++) m = Mathf.Max(m, N3Grunt(Mathf.Lerp(x0, x1, i / 4f), Mathf.Lerp(z0, z1, j / 4f)));
        return m;
    }
    static float N3Min(float x0, float x1, float z0, float z1)
    {
        float m = float.MaxValue;
        for (int i = 0; i <= 4; i++) for (int j = 0; j <= 4; j++) m = Mathf.Min(m, N3Grunt(Mathf.Lerp(x0, x1, i / 4f), Mathf.Lerp(z0, z1, j / 4f)));
        return m;
    }
    const float N3DeckX0 = -26f, N3DeckX1 = -14f, N3DeckZ0 = -37f, N3DeckZ1 = -32f;      // the beach deck (the stair's axis x -20)
    // the beach's sand (the site's ground, BraxChampionsWilleDzialki.Teren): between its walls' lines, from 4.4 m before the pad;
    // 24.11 the west wall at -46 (was -56): the site's outer 25 m (x 1200-1225 world) is its rim band, where its ground goes
    // under the far land - the far land's coarse mesh stood up to 0.6 m over the beach's sand by the old wall (measured on its
    // real triangles); now the whole beach (x 1226-1318) is the site's own ground, 92 m along the sea, the villa in its middle
    public const float N3PlazaX0 = -46f, N3PlazaX1 = 46f, N3PlazaZ = PadZ0 - 4.4f;
    const float N3Morze = -76f;                                                            // the walls' sea ends (the coast ~ -65)
    public static int N3Lezaki, N3Palmy;
    static void N3Plaza()
    {
        var d = BraxChampionsWilleDzialki.Dzialki[2];
        float morze = BraxChampionsWilleDzialki.SeaY - d.padY, sx = (NSt0 + NSt1) * .5f, stopa = PadZ0 - WStN * WStTread;
        N3Lezaki = N3Palmy = 0;
        Out_(true);
        // ---- the boardwalk: teak 1.6 m wide from the stair's foot to the deck, each metre 4 cm over the highest sand under it
        for (float z = stopa; z > N3DeckZ1 + .01f; z -= 1f)
        {
            float z1 = Mathf.Max(z - 1f, N3DeckZ1), top = N3Max(sx - .8f, sx + .8f, z1, z) + .04f;
            MM(teak, sx - .8f, sx + .8f, N3Min(sx - .8f, sx + .8f, z1, z) - .25f, top, z1, z);
        }
        // ---- the deck: teak over the sand (25 cm over its highest point), two steps down to the sand at the sea side
        float dt = N3Max(N3DeckX0, N3DeckX1, N3DeckZ0, N3DeckZ1) + .25f, dn = N3Min(N3DeckX0, N3DeckX1, N3DeckZ0, N3DeckZ1);
        MM(teak, N3DeckX0, N3DeckX1, dn - .3f, dt, N3DeckZ0, N3DeckZ1);
        float sand = N3Grunt(sx, N3DeckZ0 - .8f);
        for (int k = 1; k <= 2; k++)
        {
            float sy = dt - (dt - sand) * k / 3f;
            MM(teak, sx - 1.5f, sx + 1.5f, sand - .3f, sy, N3DeckZ0 - .36f * k, N3DeckZ0 - .36f * (k - 1));
        }
        // ---- the cabana (west half): four teak posts, a frame, a white canvas roof with its valance, the curtains tied at
        // the posts; two daybeds facing the sea, navy pillows
        float cx0 = -25.6f, cx1 = -21.2f, cz0 = -36.6f, cz1 = -32.4f, ch = dt + 2.5f;
        foreach (float x in new[] { cx0, cx1 }) foreach (float z in new[] { cz0, cz1 })
        {
            MM(teak, x - .06f, x + .06f, dt, ch, z - .06f, z + .06f);
            MM(cushion, x - .2f, x + .2f, dt + .6f, ch - .15f, z - .12f, z - .08f, false);             // the curtain, tied
        }
        foreach (float z in new[] { cz0, cz1 }) MM(teak, cx0 - .1f, cx1 + .1f, ch, ch + .14f, z - .05f, z + .05f, false);
        foreach (float x in new[] { cx0, cx1 }) MM(teak, x - .05f, x + .05f, ch + .14f, ch + .26f, cz0 - .1f, cz1 + .1f, false);
        MM(cushion, cx0 - .25f, cx1 + .25f, ch + .26f, ch + .3f, cz0 - .25f, cz1 + .25f, false);
        foreach (float z in new[] { cz0 - .25f, cz1 + .25f }) MM(cushion, cx0 - .25f, cx1 + .25f, ch + .02f, ch + .26f, z - .01f, z + .01f, false);
        foreach (float x in new[] { cx0 - .25f, cx1 + .25f }) MM(cushion, x - .01f, x + .01f, ch + .02f, ch + .26f, cz0 - .24f, cz1 + .24f, false);
        foreach (float x in new[] { -24.4f, -22.4f })
        {
            MM(teak, x - .55f, x + .55f, dt, dt + .3f, -35.9f, -33.4f);
            MM(cushion, x - .5f, x + .5f, dt + .3f, dt + .44f, -35.85f, -33.45f, false);
            foreach (float px in new[] { x - .25f, x + .25f })
                b.RoundedBox(recznik, new Vector3(px, dt + .58f, -33.62f), new Vector3(.42f, .3f, .12f), .05f, Quaternion.Euler(-15f, 0f, 0f), 2);
            W.Siedzisko(new Vector3(x, dt + .5f, -34.6f), Vector3.back, "LEŻANKA"); Siedziska++;
        }
        W.Lampa("plaża | kabana", new Vector3((cx0 + cx1) * .5f, ch - .3f, (cz0 + cz1) * .5f), .6f, 5f * K, new Color(1f, .86f, .64f));
        // ---- the lounge (east half): a teak sofa facing the sea, two armchairs, a low stone table
        Lawka(new Vector3(-17f, dt, -33.0f), Vector3.back, 2.4f);
        Lawka(new Vector3(-18.9f, dt, -34.6f), Vector3.right, .9f); Lawka(new Vector3(-15.1f, dt, -34.6f), Vector3.left, .9f);
        b.RoundedBox(deck, new Vector3(-17f, dt + .19f, -34.6f), new Vector3(1.2f, .38f, .7f), .02f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(-17f, dt + .19f, -34.6f), new Vector3(1.2f, .38f, .7f), Quaternion.identity);
        // ---- the shower beside the boardwalk, on a teak tray
        {
            float x = sx - 1.7f, z = -30.6f, y = N3Grunt(x, z);
            MM(teak, x - .55f, x + .55f, y - .2f, y + .06f, z - .55f, z + .55f);
            b.Cylinder(steel, new Vector3(x, y + .06f, z), .045f, 2.25f, 14);
            b.Tube(steel, new Vector3(x, y + 2.31f, z), new Vector3(x + .4f, y + 2.31f, z), .02f, 8);
            b.Cylinder(steel, new Vector3(x + .4f, y + 2.22f, z), .1f, .04f, 16, Quaternion.identity, true, true);
            W.Kolizja(new Vector3(x, y + 1.15f, z), new Vector3(.14f, 2.3f, .14f), Quaternion.identity);
        }
        // ---- loungers in pairs under a white parasol on the dry sand (4 cm under the sand's top at their feet), the sea before;
        // 24.11 7 m from the waterline (local z -52/-53), 0.45-0.55 m over the sea: the waves' run-up (BraxChampionsNabrzezeMorze,
        // up to 0.21 m) and the wet band never reach them
        foreach (float x in new[] { -40f, -32f, -6f, 6f, 18f, 30f })
        {
            float z = -45f, y = N3Grunt(x + .55f, z);
            Lezaki(new Vector3(x, y, z), 2, 1.1f, Vector3.back); N3Lezaki += 2;
            N3Parasol(x + .55f, z + 1.6f);
        }
        // ---- palms on the beach's back edge (clear of the deck, the stair and the boardwalk)
        foreach (float x in new[] { -40f, -30f, -9.5f, 9.5f, 20f, 30f, 40f })                       // framing the house (x -9..9), none before it
        { Palma(x, -29f, N3Grunt(x, -29f)); N3Palmy++; }
        // ---- the old stone walls closing it: the west wall from the drive down into the sea (1.8 m on the land, 0.9 over the
        // sea), the east one off the fence's end; "PLAŻA PRYWATNA" on the west wall's face toward the region's beach
        N3Mur(N3PlazaX0, 88f, N3Morze, 1.8f, morze);
        N3Mur(N3PlazaX1, N3PlazaZ, N3Morze, 1.2f, morze);
        float ty = N3Grunt(N3PlazaX0 - .6f, -31f) + 1.05f;
        BraxChampionsLetters3D.Text(b, steel, black, "PLAŻA PRYWATNA", new Vector3(N3PlazaX0 - .45f, ty, -31f), BraxChampionsLetters3D.YawFacing(Vector3.left), .26f, .02f);
        BraxChampionsLetters3D.Text(b, steel, black, Nazwy[2], new Vector3(N3PlazaX0 - .45f, ty - .42f, -31f), BraxChampionsLetters3D.YawFacing(Vector3.left), .13f, .015f);
    }
    /// <summary>An old stone wall along z at x, from z0 down toward the sea to z1: 90 cm thick, in 2 m lengths each up to h
    /// over the highest ground under it (at least 0.9 m over the sea), on a travertine coping 5 cm wider; its foot under the
    /// ground.</summary>
    static void N3Mur(float x, float z0, float z1, float h, float morze)
    {
        Out_(true);
        for (float z = z0; z > z1 + .01f; z -= 2f)
        {
            float za = Mathf.Max(z - 2f, z1), top = Mathf.Max(N3Max(x - .45f, x + .45f, za, z) + h, morze + .9f);
            MM(murStary, x - .45f, x + .45f, N3Min(x - .45f, x + .45f, za, z) - .6f, top, za, z);
            MM(deck, x - .5f, x + .5f, top, top + .06f, za, z, false);
        }
    }
    /// <summary>A white parasol (16 ribs, 2.7 m, a navy valance) on its pole in the sand at (x, z).</summary>
    static void N3Parasol(float x, float z)
    {
        Out_(true);
        var p = new Vector3(x, N3Grunt(x, z), z);
        b.Cylinder(black, p - Vector3.up * .05f, .09f, .17f, 12);
        b.Cylinder(steel, p, .024f, 2.55f, 10);
        const int ribs = 16; const float r = 1.35f, eave = 2.18f, topY = 2.62f;
        var apex = p + Vector3.up * topY;
        for (int j = 0; j < ribs; j++)
        {
            float a0 = 2f * Mathf.PI * j / ribs, a1 = 2f * Mathf.PI * (j + 1) / ribs, s0 = j % 2 == 0 ? 0f : .05f, s1 = (j + 1) % 2 == 0 ? 0f : .05f;
            var p0 = p + new Vector3(r * Mathf.Cos(a0), eave - s0, r * Mathf.Sin(a0)); var p1 = p + new Vector3(r * Mathf.Cos(a1), eave - s1, r * Mathf.Sin(a1));
            b.Triangle(cushion, apex, p1, p0); b.Triangle(cushion, apex, p0, p1);
            b.Quad(recznik, p0, p0 - Vector3.up * .10f, p1 - Vector3.up * .10f, p1); b.Quad(recznik, p1, p1 - Vector3.up * .10f, p0 - Vector3.up * .10f, p0);
        }
        b.Cylinder(steel, apex - Vector3.up * .02f, .05f, .10f, 10);
        W.Kolizja(new Vector3(x, p.y + 1.25f, z), new Vector3(.12f, 2.5f, .12f), Quaternion.identity);
    }
}
