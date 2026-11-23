using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 3 on the seafront east of the promenade, after the owner's own photo (17.11 14:56, "No bo dobra,
// zrób takie": a warm beige Mediterranean villa) and 21:55 "dokończ wille". As in his photo: warm beige stucco, clay
// barrel-tile hip roofs at three heights, the two-storey house with the front door's arch in the middle, arched French
// windows with black iron rails either side, the balcony over the door on two white columns with its balustrade, the
// arched French window onto it, round windows (oculi) upstairs and in the tall east wing with its big arched walnut door,
// the low porch wing west with its arches on a column, chimneys at both ends; before it the travertine terrace and the
// formal pool in its dark granite border, all water (no island, owner 18.11), lawns either side.
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
        N3Materialy();                                                   // 24.11 old brick, old stone walls, cream inside
        NTeren(); NBasen(); KDom(cegla, true); NSkrzydla(); N3Kamien(); NWnetrze(c, tag); KPietro(c, tag); NOgrod(tag); N3Plaza();
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
        MM(murOporowy, PadX0, PadX1, -4f, -s, PadZ0, PadZ0 + .3f);
        MM(deck, PadX0, PadX1, -s, 0f, PadZ0, PadZ0 + .3f, false);
        Tralki(true, PadZ0 + .15f, PadX0 + .2f, NSt0 - .1f, 0f); Tralki(true, PadZ0 + .15f, NSt1 + .1f, PadX1 - .2f, 0f);
        SchodyPlaza(NSt0, NSt1);
        // the stone basin on the terrace west (his photo: the round fountain by the chairs)
        Fontanna(new Vector3(-13.5f, 0f, -10f), 1.0f);
    }

    // ================================================================ the formal pool in its dark border
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
        // owner 18.11 04:48 "W basenie drzewo": no island and no tree in the water - the pool is all water
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
        // 18.11 (his zoom: a thin black grid) the wrought-iron rail of his photo 2: the round handrail, the bottom rail, square
        // bars 11 cm apart, between two middle rails a band of rings, a ball finial on each end post
        Out_(true);
        Vector3 P(float a, float y) => alongX ? new Vector3(a, y, plane) : new Vector3(plane, y, a);
        float l0 = a0 - .05f, l1 = a1 + .05f;
        b.Tube(stalCzarna, P(l0, sill + 1.0f), P(l1, sill + 1.0f), .022f, 10);
        void Rail(float y, float h, float d) { if (alongX) MM(stalCzarna, l0, l1, y, y + h, plane - d, plane + d, false); else MM(stalCzarna, plane - d, plane + d, y, y + h, l0, l1, false); }
        Rail(sill + .06f, .03f, .015f); Rail(sill + .76f, .02f, .01f); Rail(sill + .9f, .02f, .01f);
        foreach (float a in new[] { l0, l1 })
        {
            if (alongX) MM(stalCzarna, a - .015f, a + .015f, sill + .06f, sill + 1.0f, plane - .015f, plane + .015f, false);
            else MM(stalCzarna, plane - .015f, plane + .015f, sill + .06f, sill + 1.0f, a - .015f, a + .015f, false);
            b.Lathe(stalCzarna, P(a, sill + 1.0f), new[] { new Vector2(0f, 0f), new Vector2(.02f, .01f), new Vector2(.035f, .045f), new Vector2(.02f, .08f), new Vector2(0f, .085f) }, 10, Quaternion.identity, false, false);
        }
        var nrm = alongX ? Vector3.forward : Vector3.right; var along = alongX ? Vector3.right : Vector3.forward;
        for (float a = a0 + .06f; a < a1 - .03f; a += .11f)
        {
            if (alongX) MM(stalCzarna, a - .008f, a + .008f, sill + .09f, sill + .76f, plane - .008f, plane + .008f, false);
            else MM(stalCzarna, plane - .008f, plane + .008f, sill + .09f, sill + .76f, a - .008f, a + .008f, false);
            // the ring between this bar and the next, in the band
            if (a + .11f < a1 - .03f)
            {
                var cRing = P(a + .055f, sill + .84f);
                Vector3 Torus(float u, float v)
                {
                    float t = Mathf.PI * 2f * u, s = Mathf.PI * 2f * v, R = .045f, rr = .006f;
                    var rad = along * Mathf.Cos(t) + Vector3.up * Mathf.Sin(t);
                    return cRing + rad * (R + rr * Mathf.Cos(s)) + nrm * (rr * Mathf.Sin(s));
                }
                b.Surface(stalCzarna, Torus, 14, 4); b.Surface(stalCzarna, Torus, 14, 4, true);
            }
        }
        W.Kolizja(P((a0 + a1) * .5f, sill + .5f), alongX ? new Vector3(a1 - a0 + .1f, 1.0f, .06f) : new Vector3(.06f, 1.0f, a1 - a0 + .1f), Quaternion.identity);
    }

    // ================================================================ the wings
    static void NSkrzydla()
    {
        var zew = cegla;                                                 // 24.11 old brick (was the beige stucco)
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
        Out_(true); MM(tynkKrem, -16.7f, -9.3f, NEw - .3f, NEw, -5.7f, -1.06f, false);      // the porch's ceiling, cream between its walnut beams
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
        // 18.11 the big door's surround 10 cm without a keystone, the oculus over it without its ring: the round window sits
        // 11 cm over the door's crown (his photo 2)
        OknoLuk(true, -5f, 11.6f, 14.4f, 2.38f, 2.4f, 3.8f, -1f, zew, true, .1f); OknoOkragle(true, -5f, 13f, ry, rr, -1f, zew, 0f);
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
        MM(stoneIn, -8.5f, -8.1f, 0f, 1.25f, 1.4f, 4.8f); MM(marbleIn, -8.5f, -7.9f, 1.25f, 1.4f, 1.2f, 5.0f);      // 24.11 a marble mantel on the old brick
        MM(black, -8.5f, -8.48f, .25f, 1.0f, 2.3f, 3.9f, false); MM(fire, -8.48f, -8.46f, .3f, .5f, 2.5f, 3.7f, false);
        W.Lampa(tag + " | kominek", new Vector3(-7.6f, .6f, 3.1f), .8f, 5f * K, new Color(1f, .55f, .22f));
        BraxKatalog.Rug(c, new Vector3(-5.2f, 0f, 3.1f), Vector3.forward, 4.6f, 3.6f, true);
        BraxKatalog.Sofa(c, new Vector3(-3.2f, .012f, 3.1f), Vector3.left, 3.2f, 1, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(-5.3f, .012f, 3.1f), Vector3.left, 1.3f, .7f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 1.4f), 0f), new Vector3(-5.3f, .45f, 1.4f), Vector3.forward, "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 4.8f), 180f), new Vector3(-5.3f, .45f, 4.8f), Vector3.back, "FOTEL");
        // 24.11 the TV over the fireplace before the sofa (it had hung across the opening in the wall behind, x -6..-3.5)
        BraxKatalog.Tv(c, new Vector3(-8.5f + .02f, 2.2f, 3.1f), Vector3.right, 65f, 2, false, tag + " salon TV");
        BraxKatalog.FloorLamp(c, new Vector3(-8.0f, 0f, .9f));
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-8.1f, 0f, 6.9f), 70f);
        BraxKatalog.Rug(c, new Vector3(3.6f, 0f, 3.0f), Vector3.forward, 5.0f, 3.0f, false);
        BraxKatalog.DiningSet(c, new Vector3(2.8f, .012f, 3.0f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(4.4f, .012f, 3.0f), Vector3.back, "krzesło");
        foreach (float x in new[] { 2.8f, 4.4f }) ModelTop("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", "lampa nad stołem", new Vector3(x, KF1 - 1.3f, 2.55f), KF1 - .3f);
        Obrazek(3, new Vector3(2.8f, 2.0f, 7.54f), Vector3.back, 2.2f, 1.3f);                         // on the wall between the openings (1.2..4.4)
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
        // 24.11 the room's west wall over z 0..3.94 is the house's east wall - its old brick plastered cream inside (and the
        // wing's wall's end at z 0), 1 mm off the brick, its ends in the partition, the floor and the ceiling; the bed's head
        // on it (the bed had stood in the room's middle - the catalogue's p is its foot - and across the house's door, now
        // closed), the wardrobe out of the wall (it had stood 30 cm in it) on the garage's partition clear of its door, the TV
        // on the bathroom's partition, a print over the bed, an armchair and a lamp by the west window
        MM(renderIn, 9.001f, 9.012f, .001f, NEe - .28f, .012f, 3.96f, false); MM(renderIn, 9.001f, 9.299f, .001f, NEe - .28f, .001f, .012f, false);
        BraxKatalog.Rug(c, new Vector3(10.3f, 0f, 1.9f), Vector3.forward, 2.8f, 3.0f, true);
        BraxKatalog.Bed(c, new Vector3(9.012f + .03f + 2.17f, .012f, 1.9f), Vector3.right, 1.6f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(12.17f, 0f, 3.94f - .61f), Vector3.back, 2.0f, oak, BraxKatalog.Walnut, 2.3f, "szafa");
        BraxKatalog.Tv(c, new Vector3(13.3f - .06f - .02f, 1.35f, 2.6f), Vector3.left, 55f, 1, false, tag + " apartament gościnny TV");
        Obrazek(6, new Vector3(9.012f, 2.05f, 1.9f), Vector3.right, 1.6f, .8f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w apartamencie", new Vector3(10.4f, 0f, -3.6f), 45f), new Vector3(10.4f, .45f, -3.6f), new Vector3(.71f, 0f, .71f), "FOTEL");
        BraxKatalog.FloorLamp(c, new Vector3(9.8f, 0f, -4.3f));
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
            "MONIKA: Willa nad samym morzem ze starej cegły z kamiennymi narożnikami, pod dachówką: łukowe drzwi i okna, balkon na kolumnach, okrągłe okna, ganek z kuchnią, apartament gościnny, garaż na dwa auta, basen w ciemnej obwódce i własna prywatna plaża z kabaną, leżakami i palmami. Zapraszam do WAWER NIERUCHOMOŚCI w T12!");
    }
}
