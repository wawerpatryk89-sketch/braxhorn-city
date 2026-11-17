using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 3 on the seafront east of the promenade, after the owner's own photo (17.11 14:56, "No bo dobra,
// zrób takie": a warm beige Mediterranean villa) and 21:55 "dokończ wille". As in his photo: warm beige stucco, clay
// barrel-tile hip roofs at three heights, the two-storey house with the front door's arch in the middle, arched French
// windows with black iron rails either side, the balcony over the door on two white columns with its balustrade, the
// arched French window onto it, round windows (oculi) upstairs and in the tall east wing with its big arched walnut door,
// the low porch wing west with its arches on a column, chimneys at both ends; before it the travertine terrace and the
// formal pool in its dark granite border with the little tree on its island, lawns either side.
// On the site's pad (x -24..27, z -22.6..26, the view to -z over the sea; the road in at the back, local x 9):
//  - the house x -9..9, z 0..13 (the hill villa's shell: the floors, the stair, the upstairs rooms, the back door);
//  - the west wing x -17..-9, z -6..8: the porch (two arches front, one west) and the kitchen behind;
//  - the east wing x 9..17, z -5..13 (5 m to the eaves): the guest suite at the front, the garage for two cars behind
//    (its door to the drive at the back);
//  - the stair down the pad's front to the beach (x -21..-19).
public static partial class BraxChampionsWille
{
    const float NEw = 4.4f, NEe = 5.0f;
    public const float NPX0 = -7f, NPX1 = 7f, NPZ0 = -20.2f, NPZ1 = -13.7f;
    const float NSt0 = -21f, NSt1 = -19f;

    static void PlanNabrzeze()
    {
        pietro = KF1;
        var c = new BraxKatalog.Ctx { m = kat, root = g, cols = g, unit = 1f, water = null, inst = g, seat = (p, f, l) => { W.Siedzisko(p, f, l); Siedziska++; } };
        string tag = Nazwy[2];
        NTeren(); NBasen(); KDom(tynkBez, true); NSkrzydla(); NWnetrze(c, tag); KPietro(c, tag); NOgrod(tag);
    }

    // ================================================================ the terraces, lawns, the drive, the pad's front
    static void NTeren()
    {
        Out_(true);
        const float s = .3f;
        float bx0 = NPX0 - .45f, bx1 = NPX1 + .45f, bz0 = NPZ0 - .45f, bz1 = NPZ1 + .45f;     // the pool's dark border, outside
        MM(deck, -18f, 18f, -s, 0f, bz1, -6f);
        MM(deck, -18f, bx0, -s, 0f, PadZ0 + .3f, bz1); MM(deck, bx1, 18f, -s, 0f, PadZ0 + .3f, bz1); MM(deck, bx0, bx1, -s, 0f, PadZ0 + .3f, bz0);
        MM(deck, -9f, 9f, -s, 0f, -6f, 0f); MM(deck, 9f, 17f, -s, 0f, -6f, -5f);
        MM(lawn, PadX0, -18f, -s, 0f, PadZ0 + .3f, PadZ1); MM(lawn, 18f, PadX1, -s, 0f, PadZ0 + .3f, PadZ1);
        MM(lawn, -18f, -17f, -s, 0f, -6f, 8f); MM(lawn, 17f, 18f, -s, 0f, -6f, 13f);
        MM(lawn, -18f, -9f, -s, 0f, 8f, PadZ1); MM(lawn, -9f, -1.2f, -s, 0f, 13f, PadZ1); MM(lawn, 1.2f, 6f, -s, 0f, 13f, PadZ1);
        MM(paving, -1.2f, 1.2f, -s, 0f, 13f, PadZ1); MM(paving, 6f, 18f, -s, 0f, 13f, PadZ1);
        // the pad's front: the stone wall under its edge, a travertine coping, the white balustrade (open at the stair)
        MM(stone, PadX0, PadX1, -4f, -s, PadZ0, PadZ0 + .3f);
        MM(deck, PadX0, PadX1, -s, 0f, PadZ0, PadZ0 + .3f, false);
        Tralki(true, PadZ0 + .15f, PadX0 + .2f, NSt0 - .1f, 0f); Tralki(true, PadZ0 + .15f, NSt1 + .1f, PadX1 - .2f, 0f);
        SchodyPlaza(NSt0, NSt1);
        // the stone basin on the terrace west (his photo: the round fountain by the chairs)
        Fontanna(new Vector3(-13.5f, 0f, -10f), 1.0f);
    }

    // ================================================================ the formal pool in its dark border, its little island
    static void NBasen()
    {
        Out_(true);
        const float fl = -1.5f;
        MM(poolTile, NPX0 - .3f, NPX0, fl - .2f, -.3f, NPZ0 - .3f, NPZ1 + .3f); MM(poolTile, NPX1, NPX1 + .3f, fl - .2f, -.3f, NPZ0 - .3f, NPZ1 + .3f);
        MM(poolTile, NPX0, NPX1, fl - .2f, -.3f, NPZ0 - .3f, NPZ0); MM(poolTile, NPX0, NPX1, fl - .2f, -.3f, NPZ1, NPZ1 + .3f);
        MM(poolTile, NPX0, NPX1, fl - .2f, fl, NPZ0, NPZ1);
        // the dark granite border 45 cm round the water (his photo), its top the terrace's
        MM(stone, NPX0 - .45f, NPX1 + .45f, -.3f, 0f, NPZ0 - .45f, NPZ0); MM(stone, NPX0 - .45f, NPX1 + .45f, -.3f, 0f, NPZ1, NPZ1 + .45f);
        MM(stone, NPX0 - .45f, NPX0, -.3f, 0f, NPZ0, NPZ1); MM(stone, NPX1, NPX1 + .45f, -.3f, 0f, NPZ0, NPZ1);
        // the steps in at the east end, toward the house
        for (int i = 0; i < 4; i++) MM(poolTile, NPX1 - .45f * (i + 1), NPX1, fl, -.32f - i * .32f, NPZ1 - 2.2f, NPZ1);
        // the island at the front middle: a granite box from the floor, soil on it, a little tree (the region's drzewo_verdant
        // to 3.2 m)
        MM(stone, -.8f, .8f, fl, .32f, NPZ0 + .3f, NPZ0 + 1.9f);
        MM(lawn, -.72f, .72f, .32f, .34f, NPZ0 + .38f, NPZ0 + 1.82f, false);
        var tree = BraxRegion.Model("drzewo_verdant");
        if (tree && BraxRegion.Put(g, tree, "WILLA 3 | drzewko na wysepce basenu (drzewo_verdant)", new Vector3(0f, .34f, NPZ0 + 1.1f), 35f, 1f, false, 3.2f, 0f, false)) Drzewa++;
        // the water, its body, the swim zone, two lights
        Plane(bw, waterMat, new Vector3(NPX0, Water, NPZ0), new Vector3(NPX1 - NPX0, 0, 0), new Vector3(0, 0, NPZ1 - NPZ0));
        Ton(NPX0 + .05f, NPX1 - .05f, fl + .05f, Water - .005f, NPZ0 + .05f, NPZ1 - .05f);
        var surf = new GameObject("basen | lustro wody").transform; surf.SetParent(g, false); surf.localPosition = new Vector3((NPX0 + NPX1) * .5f, Water, (NPZ0 + NPZ1) * .5f);
        var zone = new GameObject("basen | strefa pływania").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = new Vector3((NPX0 + NPX1) * .5f, (fl + .05f + Water + .35f) * .5f, (NPZ0 + NPZ1) * .5f);
        zone.size = new Vector3(NPX1 - NPX0 - .7f, Water + .35f - fl - .05f, NPZ1 - NPZ0 - .7f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = surf; sw.basinId = "willa_3_basen";
        foreach (float x in new[] { -4f, 4f }) b.Cylinder(BraxKatalog.Led, new Vector3(x, -.75f, NPZ1 - .001f), .12f, .012f, 16, Quaternion.Euler(90f, 0f, 0f), true);
    }

    // ================================================================ the house's front (the rest is the hill's shell)
    static void NDomFront(Material zew)
    {
        Out_(false); MM(floorGf, -8.7f, 8.7f, -.25f, 0f, .3f, 12.7f); MM(ceiling, -8.7f, 8.7f, KF1 - .3f, KF1 - .04f, .3f, 3f, false);
        Out_(true); MM(deck, -1f, 1f, -.25f, 0f, 12.7f, 13f);
        // ---- the ground front (z 0): the double walnut door under its glazed arch, arched French windows with iron rails
        // either side, a window at each end
        var dol = new[] { new Vector4(-1f, 1f, 0f, 3.4f), new Vector4(-5f, -3.4f, .3f, 3.0f), new Vector4(3.4f, 5f, .3f, 3.0f), new Vector4(-8f, -7f, .9f, 2.8f), new Vector4(7f, 8f, .9f, 2.8f) };
        ScianaOE(zew, true, 0f, -9f, 9f, 0f, KF1, -1f, false, RogX(-9f, 9f), dol);
        OknoLuk(true, 0f, -1f, 1f, 2.38f, 2.4f, 3.4f, -1f, zew);
        Out_(false);
        BraxChampionsInteriorDoor.Single("drzwi wejściowe L", "WILLA 3 | drzwi wejściowe (orzech, lewe)", true, .15f, -1f, 0f, 0f, 2.38f, walnut, steel, .07f);
        BraxChampionsInteriorDoor.Single("drzwi wejściowe P", "WILLA 3 | drzwi wejściowe (orzech, prawe)", true, .15f, 0f, 1f, 0f, 2.38f, walnut, steel, .07f);
        foreach (var o in new[] { dol[1], dol[2] }) { OknoLuk(true, 0f, o.x, o.y, o.z, o.w - (o.y - o.x) * .5f, o.w, -1f, zew); Balkonik(true, -.12f, o.x, o.y, o.z); }
        OknoLuk(true, 0f, -8f, -7f, .9f, 2.3f, 2.8f, -1f, zew); OknoLuk(true, 0f, 7f, 8f, .9f, 2.3f, 2.8f, -1f, zew);
        // two lanterns at the door
        Out_(true);
        foreach (float lx in new[] { -1.55f, 1.55f })
        {
            b.Box(black, new Vector3(lx, 2.35f, -.12f), new Vector3(.18f, .4f, .18f), Quaternion.identity);
            b.Box(downlight, new Vector3(lx, 2.35f, -.215f), new Vector3(.12f, .26f, .01f), Quaternion.identity);
            W.Lampa("wejście | latarnia", new Vector3(lx, 2.3f, -.6f), .5f, 4f * K, new Color(1f, .82f, .58f));
        }
        // ---- the upper front: the arched French window onto the balcony, an arched window either side, one at the west
        // end, two round windows east
        float oy = KF1 + 1.9f, orr = .42f;
        var gora = new[] { new Vector4(-.8f, .8f, KF1, KF1 + 2.95f), new Vector4(-4.67f, -3.37f, KF1 + .6f, KF1 + 2.75f), new Vector4(3.37f, 4.67f, KF1 + .6f, KF1 + 2.75f),
                           new Vector4(-8f, -7f, KF1 + .9f, KF1 + 2.7f), new Vector4(6.3f - orr, 6.3f + orr, oy - orr, oy + orr), new Vector4(7.7f - orr, 7.7f + orr, oy - orr, oy + orr) };
        ScianaOE(zew, true, 0f, -9f, 9f, KF1, KE, -1f, false, RogX(-9f, 9f), gora);
        for (int i = 0; i < 4; i++) { var o = gora[i]; OknoLuk(true, 0f, o.x, o.y, o.z, o.w - (o.y - o.x) * .5f, o.w, -1f, zew); }
        Balkonik(true, -.12f, -4.67f, -3.37f, KF1 + .6f); Balkonik(true, -.12f, 3.37f, 4.67f, KF1 + .6f);
        OknoOkragle(true, 0f, 6.3f, oy, orr, -1f, zew); OknoOkragle(true, 0f, 7.7f, oy, orr, -1f, zew);
        // ---- the balcony over the door on two white columns, its stone balustrade
        Out_(true);
        MM(deck, -2.8f, 2.8f, KF1 - .22f, KF1, -2.4f, 0f);
        Kolumna(new Vector3(-2.45f, 0f, -2.1f), KF1 - .22f, .22f); Kolumna(new Vector3(2.45f, 0f, -2.1f), KF1 - .22f, .22f);
        Tralki(true, -2.27f, -2.8f, 2.8f, KF1); Tralki(false, -2.67f, -2.4f, -.05f, KF1); Tralki(false, 2.67f, -2.4f, -.05f, KF1);
    }
    /// <summary>A black iron Juliet rail before a French window (a0..a1) at its sill, 1 m high, in front of the wall.</summary>
    static void Balkonik(bool alongX, float plane, float a0, float a1, float sill)
    {
        Out_(true);
        MM(stalCzarna, a0 - .05f, a1 + .05f, sill + .98f, sill + 1.02f, plane - .02f, plane + .02f, false);
        MM(stalCzarna, a0 - .05f, a1 + .05f, sill + .06f, sill + .09f, plane - .015f, plane + .015f, false);
        for (float a = a0 + .06f; a < a1 - .03f; a += .11f) MM(stalCzarna, a - .008f, a + .008f, sill + .09f, sill + .98f, plane - .008f, plane + .008f, false);
        W.Kolizja(new Vector3((a0 + a1) * .5f, sill + .5f, plane), new Vector3(a1 - a0 + .1f, 1.0f, .06f), Quaternion.identity);
    }

    // ================================================================ the wings
    static void NSkrzydla()
    {
        var zew = tynkBez;
        // ---- the west porch: two arches to the terrace on a column, one west; the kitchen behind (z -1..8)
        Out_(true); MM(deck, -16.7f, -9f, -.25f, 0f, -5.7f, -1.06f);
        Out_(false); MM(floorGf, -16.7f, -9f, -.25f, 0f, -.94f, 7.7f);
        var pf = new[] { new Vector4(-16.4f, -13.3f, 0f, 3.6f), new Vector4(-12.7f, -9.6f, 0f, 3.6f) };
        ScianaOE(zew, true, -6f, -17f, -9f, 0f, NEw, -1f, false, RogX(-17f, -9f), pf);
        foreach (var o in pf) Luk(true, -6f, o.x, o.y, 2.05f, 3.6f, -1f, zew, zew);
        Kolumna(new Vector3(-13f, 0f, -6.25f), 2.05f, .2f);
        ScianaOE(zew, false, -9f, -6f, 0f, 0f, NEw, 1f, false, new Vector4(-6f, 0f, -5.7f, 0f), new Vector4(-4.6f, -1.4f, 0f, 3.2f));
        Luk(false, -9f, -4.6f, -1.4f, 1.6f, 3.2f, 1f, zew, zew);
        var pw = new[] { new Vector4(-4.6f, -1.4f, 0f, 3.2f), new Vector4(2f, 3f, .9f, 2.8f), new Vector4(5f, 6f, .9f, 2.8f) };
        ScianaOE(zew, false, -17f, -6f, 8f, 0f, NEw, -1f, false, RogZ(-6f, 8f, false), pw);
        Luk(false, -17f, -4.6f, -1.4f, 1.6f, 3.2f, -1f, zew, zew);
        OknoLuk(false, -17f, 2f, 3f, .9f, 2.3f, 2.8f, -1f, zew); OknoLuk(false, -17f, 5f, 6f, .9f, 2.3f, 2.8f, -1f, zew);
        ScianaOE(zew, true, 8f, -17f, -9f, 0f, NEw, 1f, false, new Vector4(-16.85f, -9f, -16.7f, -9f), new Vector4(-14f, -13f, .9f, 2.8f));
        OknoLuk(true, 8f, -14f, -13f, .9f, 2.3f, 2.8f, 1f, zew);
        Scianka(true, -1f, -16.7f, -9.3f, 0f, NEw - .3f, new Vector2(-12.6f, -11.7f)); Drzwi("kuchnia", true, -1f, -12.6f, -11.7f, 0f);
        Out_(true); MM(zew, -16.7f, -9.3f, NEw - .3f, NEw, -5.7f, -1.06f, false);           // inside the porch's east wall (x -9.3..-9)
        for (float z = -5.2f; z < -1.3f; z += 1.3f) b.Box(walnut, new Vector3(-12.85f, NEw - .38f, z), new Vector3(7.4f, .16f, .16f), Quaternion.identity);   // their ends in the walls
        Out_(false); MM(ceiling, -16.7f, -9.3f, NEw - .3f, NEw, -.94f, 0f, false); MM(ceiling, -16.7f, -9f, NEw - .3f, NEw, 0f, 7.7f, false);
        Dach4(-17f, -9f, -6f, 8f, NEw, KPitchW, zew, .6f, 2);
        Komin(-15.4f, 6.2f, NEw, 7.4f, .8f, .6f);
        // ---- the east wing: its big arched walnut door and a round window over it, arched windows and round windows east,
        // the garage's door to the drive at the back
        Out_(false); MM(floorGf, 9f, 16.7f, -.25f, 0f, -4.7f, 3.94f); MM(concrete, 9f, 16.7f, -.25f, 0f, 4.06f, 12.7f);
        Out_(true); MM(concrete, 10.2f, 15.8f, -.25f, 0f, 12.7f, 13f);
        float ry = 4.28f, rr = .37f;                     // the round windows under the wing's ceiling (4.7), over the door (3.8)
        ScianaOE(zew, true, -5f, 9f, 17f, 0f, NEe, -1f, false, RogX(9f, 17f), new Vector4(11.6f, 14.4f, 0f, 3.8f), new Vector4(13f - rr, 13f + rr, ry - rr, ry + rr));
        OknoLuk(true, -5f, 11.6f, 14.4f, 2.38f, 2.4f, 3.8f, -1f, zew); OknoOkragle(true, -5f, 13f, ry, rr, -1f, zew);
        Out_(false);
        BraxChampionsInteriorDoor.Single("skrzydło wsch. drzwi L", "WILLA 3 | drzwi łukowe (orzech, lewe)", true, -4.85f, 11.6f, 13f, 0f, 2.38f, walnut, steel, .07f);
        BraxChampionsInteriorDoor.Single("skrzydło wsch. drzwi P", "WILLA 3 | drzwi łukowe (orzech, prawe)", true, -4.85f, 13f, 14.4f, 0f, 2.38f, walnut, steel, .07f);
        Out_(true);
        MM(deck, 11.35f, 14.65f, 0f, .12f, -5.4f, -5f);                                                     // the stone sill
        ScianaOE(zew, false, 9f, -5f, 0f, 0f, NEe, -1f, false, new Vector4(-5f, 0f, -4.7f, 0f), new Vector4(-3.4f, -2.2f, .9f, 2.8f));
        OknoLuk(false, 9f, -3.4f, -2.2f, .9f, 2.2f, 2.8f, -1f, zew);
        var ew = new[] { new Vector4(-3f, -2f, .9f, 2.8f), new Vector4(1f, 2f, .9f, 2.8f), new Vector4(-2.5f - rr, -2.5f + rr, ry - rr, ry + rr), new Vector4(1.5f - rr, 1.5f + rr, ry - rr, ry + rr), new Vector4(8f, 9f, 1.5f, 2.6f) };
        ScianaOE(zew, false, 17f, -5f, 13f, 0f, NEe, 1f, false, RogZ(-5f, 13f, false), ew);
        OknoLuk(false, 17f, -3f, -2f, .9f, 2.3f, 2.8f, 1f, zew); OknoLuk(false, 17f, 1f, 2f, .9f, 2.3f, 2.8f, 1f, zew); OknoLuk(false, 17f, 8f, 9f, 1.5f, 2.1f, 2.6f, 1f, zew);
        OknoOkragle(false, 17f, -2.5f, ry, rr, 1f, zew); OknoOkragle(false, 17f, 1.5f, ry, rr, 1f, zew);
        Balkonik(false, 17.12f, -3f, -2f, .9f); Balkonik(false, 17.12f, 1f, 2f, .9f);
        ScianaOE(zew, true, 13f, 9f, 17f, 0f, NEe, 1f, false, new Vector4(9f, 16.85f, 9f, 16.7f), new Vector4(10.2f, 15.8f, 0f, 2.6f));
        BramaGarazu("brama", 10.2f, 15.8f, 13f, 2.6f);
        Scianka(true, 4f, 9f, 16.7f, 0f, NEe - .3f, new Vector2(10f, 10.9f)); Drzwi("garaż", true, 4f, 10f, 10.9f, 0f);
        Out_(false); MM(ceiling, 9.3f, 16.7f, NEe - .3f, NEe, -4.7f, 0f, false); MM(ceiling, 9f, 16.7f, NEe - .3f, NEe, 0f, 12.7f, false);
        Dach4(9f, 17f, -5f, 13f, NEe, KPitchW, zew, .6f, 1);
        Komin(15.4f, 11f, NEe, 8.4f, .8f, .6f);
    }

    // ================================================================ inside: the ground floor, the kitchen, the east wing
    static void NWnetrze(BraxKatalog.Ctx c, string tag)
    {
        Out_(false);
        // ---- the salon (x -8.7..0, z .3..7.6): the stone fireplace on the west wall, sofas round it; the dining room east
        Scianka(true, 7.6f, -8.7f, 7.2f, 0f, KF1 - .3f, new Vector2(-6f, -3.5f), new Vector2(-1.2f, 1.2f), new Vector2(4.4f, 5.4f));
        MM(stoneIn, -8.7f, -8.5f, 0f, KF1 - .3f, 1.6f, 4.6f);
        MM(stoneIn, -8.5f, -8.1f, 0f, 1.25f, 1.4f, 4.8f); MM(stoneIn, -8.5f, -7.9f, 1.25f, 1.4f, 1.2f, 5.0f);
        MM(black, -8.5f, -8.48f, .25f, 1.0f, 2.3f, 3.9f, false); MM(fire, -8.48f, -8.46f, .3f, .5f, 2.5f, 3.7f, false);
        W.Lampa(tag + " | kominek", new Vector3(-7.6f, .6f, 3.1f), .8f, 5f * K, new Color(1f, .55f, .22f));
        BraxKatalog.Rug(c, new Vector3(-5.2f, 0f, 3.1f), Vector3.forward, 4.6f, 3.6f, true);
        BraxKatalog.Sofa(c, new Vector3(-3.2f, .012f, 3.1f), Vector3.left, 3.2f, 1, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(-5.3f, .012f, 3.1f), Vector3.left, 1.3f, .7f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 1.4f), 0f), new Vector3(-5.3f, .45f, 1.4f), Vector3.forward, "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 4.8f), 180f), new Vector3(-5.3f, .45f, 4.8f), Vector3.back, "FOTEL");
        BraxKatalog.Tv(c, new Vector3(-3.6f, 1.6f, 7.54f), Vector3.back, 75f, 2, false, tag + " salon TV");
        BraxKatalog.FloorLamp(c, new Vector3(-8.0f, 0f, .9f));
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-8.1f, 0f, 6.9f), 70f);
        BraxKatalog.Rug(c, new Vector3(3.6f, 0f, 3.0f), Vector3.forward, 5.0f, 3.0f, false);
        BraxKatalog.DiningSet(c, new Vector3(2.8f, .012f, 3.0f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(4.4f, .012f, 3.0f), Vector3.back, "krzesło");
        foreach (float x in new[] { 2.8f, 4.4f }) ModelTop("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", "lampa nad stołem", new Vector3(x, KF1 - 1.3f, 2.55f), KF1 - .3f);
        Obrazek(3, new Vector3(4.0f, 2.0f, 7.54f), Vector3.back, 2.2f, 1.3f);
        Oczka(-7.4f, 6.6f, 1.2f, 6.6f, KF1 - .3f, 2.8f);
        foreach (var lp in new[] { new Vector3(-5f, KF1 - .6f, 3f), new Vector3(3.6f, KF1 - .6f, 3f) }) W.Lampa(tag + " | salon", lp, 1f, 9f * K, new Color(1f, .9f, .78f));
        // ---- the library behind (x -8.7..-2) and the hall to the back door and the stair
        Scianka(false, -2f, 7.6f, 12.7f, 0f, KF1 - .3f, new Vector2(9.6f, 11.2f));
        BraxKatalog.Wardrobe(c, new Vector3(-8.1f, 0f, 10.2f), Vector3.right, 3.6f, walnut, BraxKatalog.Walnut, 2.6f, "regał z książkami");
        Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel do czytania", new Vector3(-4.6f, 0f, 9.4f), 220f), new Vector3(-4.6f, .42f, 9.4f), new Vector3(-.64f, 0f, -.77f), "FOTEL");
        BraxKatalog.FloorLamp(c, new Vector3(-3.6f, 0f, 8.4f));
        b.RoundedBox(walnut, new Vector3(1.5f, .42f, 12.25f), new Vector3(1.4f, .84f, .45f), .01f, Quaternion.identity, 1);
        W.Kolizja(new Vector3(1.5f, .42f, 12.25f), new Vector3(1.4f, .84f, .45f), Quaternion.identity);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(-1.4f, 0f, 12.2f), 30f);
        W.Lampa(tag + " | biblioteka", new Vector3(-5.4f, KF1 - .6f, 10.2f), .9f, 7f * K, new Color(1f, .9f, .78f));
        W.Lampa(tag + " | hol", new Vector3(2f, KF1 - .6f, 10.2f), .8f, 7f * K, new Color(1f, .9f, .78f));
        Oczka(-.6f, 4.2f, 9.2f, 11.6f, KF1 - .3f);
        // ---- the kitchen in the west wing: a low run on the west wall (no wall units), the island with three stools, the
        // porch's table and lounge
        BraxKatalog.Kitchen(c, new Vector3(-16.68f, 0f, -.4f), Vector3.forward, Vector3.right,
            new[] { "szuflady", "zlew", "zmywarka", "szuflady", "plyta", "szuflady", "piekarnik", "szuflady" }, walnut, BraxKatalog.BlackMatt, BraxKatalog.Stone, tag + " kuchnia", false);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(-13.6f, .45f, 2.6f), new Vector3(1.0f, .87f, 3.0f), .01f, Quaternion.identity, 1);
        b.RoundedBox(BraxKatalog.Stone, new Vector3(-13.6f, .895f, 2.6f), new Vector3(1.15f, .05f, 3.2f), .008f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(-13.6f, .46f, 2.6f), new Vector3(1.15f, .92f, 3.2f), Quaternion.identity);
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(-12.65f, 0f, 1.6f + i * 1.0f);
            b.Cylinder(steel, sp, .025f, .7f, 10, Quaternion.identity, false); b.Cylinder(steel, sp, .2f, .015f, 20, Quaternion.identity, true);
            b.Cylinder(BraxKatalog.Leather, sp + Vector3.up * .66f, .2f, .07f, 20, Quaternion.identity, true);
            W.Siedzisko(sp + Vector3.up * .73f, Vector3.left, "HOKER"); Siedziska++;
        }
        W.Lampa(tag + " | kuchnia", new Vector3(-13f, NEw - .6f, 3f), .9f, 9f * K, new Color(1f, .93f, .82f));
        BraxKatalog.DiningSet(c, new Vector3(-13.8f, .012f, -3.4f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(-12.2f, .012f, -3.4f), Vector3.back, "krzesło");
        W.Lampa(tag + " | ganek", new Vector3(-13f, NEw - .7f, -3.4f), .9f, 8f * K, new Color(1f, .86f, .64f));
        // ---- the east wing: the guest suite (its arched door, its bathroom), the garage
        Lazienka(13.36f, 16.7f, -.6f, 3.94f, 0f, new Vector3(0f, .4f, 1.2f), 0f, 0f);
        Scianka(false, 13.3f, -.6f, 3.94f, 0f, NEe - .3f, new Vector2(.4f, 1.2f)); Drzwi("łazienka gościnna", false, 13.3f, .4f, 1.2f, 0f);
        Scianka(true, -.6f, 13.3f, 16.7f, 0f, NEe - .3f);
        Prysznic(new Vector3(15.9f, 0f, 3.1f));
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.back, .8f, walnut, "umywalka"), new Vector3(15.0f, 0f, -.584f), Vector3.back, null);
        BraxKatalog.WcPan(c, new Vector3(16.684f, 0f, 1.2f), Vector3.left);
        BraxKatalog.Rug(c, new Vector3(11.2f, 0f, -1.4f), Vector3.forward, 3.0f, 2.6f, true);
        BraxKatalog.Bed(c, new Vector3(11.2f, .012f, -.2f), Vector3.back, 1.6f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(9.6f, 0f, 3.0f), Vector3.right, 1.8f, oak, BraxKatalog.Walnut, 2.3f, "szafa");
        W.Lampa(tag + " | apartament gościnny", new Vector3(12f, NEe - .6f, -2f), .8f, 7f * K, new Color(1f, .93f, .82f));
        MM(walnut, 16.2f, 16.7f, 0f, 2.0f, 4.5f, 7.5f);
        foreach (float x in new[] { 11.2f, 14.8f }) MM(black, x - .6f, x + .6f, 0f, .1f, 5.0f, 5.15f);
        W.Lampa("garaż | światło", new Vector3(13f, NEe - .6f, 8.5f), 1.1f, 9f * K);
    }

    // ================================================================ the garden: palms, trees, the terrace's chairs, the sale board
    static void NOgrod(string tag)
    {
        Out_(true);
        Lezaki(new Vector3(-11.4f, 0f, -17.5f), 2, 2.2f, Vector3.right); Lezaki(new Vector3(11.4f, 0f, -17.5f), 2, 2.2f, Vector3.left);
        // the terrace's chairs round low tables by the house (his photo: dark chairs on the travertine)
        Lawka(new Vector3(-6.2f, 0f, -3.2f), Vector3.back, 1.8f); Lawka(new Vector3(6.2f, 0f, -3.2f), Vector3.back, 1.8f);
        foreach (float x in new[] { -16.4f, -10.6f, 10.6f, 16.4f }) { b.Cylinder(black, new Vector3(x, 0f, -12.4f), .07f, .6f, 12, Quaternion.identity, true); W.Lampa(tag + " | taras", new Vector3(x, .7f, -12.2f), .5f, 6f * K, new Color(1f, .86f, .64f)); }
        foreach (var p in new[] { new Vector2(-21.5f, -18f), new Vector2(-21.5f, -6f), new Vector2(-21.5f, 6f), new Vector2(23.5f, -18f), new Vector2(23.5f, -6f), new Vector2(23.5f, 6f),
                                  new Vector2(21f, 20f), new Vector2(-21f, 22f) })
            Palma(p.x, p.y);
        foreach (var p in new[] { new Vector2(-20.5f, 13f), new Vector2(-12.5f, 19f), new Vector2(23f, 13.5f), new Vector2(-5f, 21f) }) Drzewo(p.x, p.y);
        TablicaSprzedazy(3, new Vector3(1.0f, 0f, 23.6f), Vector3.forward,
            "MONIKA: Willa nad samym morzem, w ciepłym beżu i dachówce: łukowe drzwi i okna, balkon na kolumnach, okrągłe okna, ganek z kuchnią, apartament gościnny, garaż na dwa auta, basen z wysepką i drzewkiem, schody na plażę. Zapraszam do WAWER NIERUCHOMOŚCI w T12!");
    }
}
