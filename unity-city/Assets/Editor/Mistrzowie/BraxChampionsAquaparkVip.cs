using UnityEngine;

// AQUAPARK BRAXHORN - VIP CABANAS (BraxChampionsAquapark): four cabanas in a row on the east lawn between the palms, open
// to the lagoon (west). Each: a silver-teak platform, four stainless posts, a white canvas pyramid roof with a red-trimmed
// valance and its number, the back and the rear half of the sides closed with canvas, the front curtains gathered and tied
// to the posts, a double daybed you lie on (two BraxSeat "CABANA"), cushions, a stainless side table with an ice bucket and
// rolled towels. Platforms walkable, posts and beds solid.
public static partial class BraxChampionsAquapark
{
    public static int Cabanas;

    static void VipCabanas()
    {
        int n = 0;
        foreach (float x in new[] { 62f, 72f, 82f, 92f }) Cabana(CX + x, CZ + 50f, ++n);
    }

    static void Cabana(float cx, float cz, int nr)
    {
        const float half = 2.3f, post = 2.15f, y0 = DY + .01f, deck = DY + .13f, eave = DY + 2.75f, apex = DY + 3.45f;
        // the platform (flush silver teak on a graphite plinth)
        walk.BoxMinMax(teak, cx - half, cx + half, deck - .03f, deck, cz - half, cz + half);
        m.BoxMinMax(graphite, cx - half + .02f, cx + half - .02f, y0, deck - .03f, cz - half + .02f, cz + half - .02f);
        // posts
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
        {
            var b = new Vector3(cx + sx * post, deck, cz + sz * post);
            m.Cylinder(steel, b, .05f, eave - deck + .02f, 12);
            m.Cylinder(steel, b, .085f, .02f, 14);
            Col(b.x - .07f, b.x + .07f, deck, eave, b.z - .07f, b.z + .07f, "cabana " + nr + " słupek");
        }
        // the roof: canvas pyramid both sides, a stainless finial, the valance round it (white, red trim) on a steel ring
        var ap = new Vector3(cx, apex, cz);
        var cs = new[] { new Vector3(cx - half, eave, cz - half), new Vector3(cx + half, eave, cz - half), new Vector3(cx + half, eave, cz + half), new Vector3(cx - half, eave, cz + half) };
        for (int i = 0; i < 4; i++)
        {
            var a = cs[i]; var b = cs[(i + 1) % 4];
            T(m, canvas, ap, a, b, Vector3.up); T(m, canvas, ap, b, a, Vector3.down);
        }
        m.Cylinder(steel, ap - Vector3.up * .02f, .03f, .22f, 10); m.Cylinder(steel, ap + Vector3.up * .2f, .055f, .06f, 12);
        foreach (var (x0, x1, z0, z1) in new[] { (cx - half, cx + half, cz - half - .02f, cz - half), (cx - half, cx + half, cz + half, cz + half + .02f),
                                                 (cx - half - .02f, cx - half, cz - half, cz + half), (cx + half, cx + half + .02f, cz - half, cz + half) })
        {
            m.BoxMinMax(canvas, x0, x1, eave - .26f, eave, z0, z1);
            m.BoxMinMax(canvasRed, x0 - .002f, x1 + .002f, eave - .30f, eave - .26f, z0 - .002f, z1 + .002f);
        }
        m.BoxMinMax(steel, cx - post, cx + post, eave - .04f, eave, cz - post - .02f, cz - post + .02f);
        m.BoxMinMax(steel, cx - post, cx + post, eave - .04f, eave, cz + post - .02f, cz + post + .02f);
        BraxChampionsLetters3D.Text(m, graphite, graphite, "CABANA " + nr, new Vector3(cx - half - .03f, eave - .19f, cz), -90f, .11f, .015f, .10f, 3.2f);
        // canvas walls: the back (east) closed, the sides closed on their rear half; the front curtains gathered at the posts
        m.BoxMinMax(canvas, cx + post - .01f, cx + post + .01f, deck + .02f, eave - .26f, cz - post, cz + post);
        foreach (float sz in new[] { -1f, 1f })
        {
            m.BoxMinMax(canvas, cx + .2f, cx + post, deck + .02f, eave - .26f, cz + sz * post - .01f, cz + sz * post + .01f);
            // the rear-half curtain's gathered edge
            m.Cylinder(canvas, new Vector3(cx + .2f, deck + .02f, cz + sz * post), .06f, eave - .28f - deck, 10);
            // the front pair: gathered and tied back to the front posts with a red band
            var g = new Vector3(cx - post + .12f, deck + .02f, cz + sz * (post - .12f));
            m.Cylinder(canvas, g, .13f, eave - .28f - deck, 14, Quaternion.identity, true, false, .09f);
            m.Cylinder(canvasRed, g + Vector3.up * 1.05f, .118f, .07f, 14);
        }
        // the daybed: teak base, a deep white mattress, three cushions at the back (red in the middle), feet to the lagoon
        m.BoxMinMax(teak, cx - .95f, cx + 1.9f, deck, deck + .32f, cz - 1.2f, cz + 1.2f);
        m.RoundedBox(cushion, new Vector3(cx + .47f, deck + .40f, cz), new Vector3(2.78f, .17f, 2.32f), .06f, 3);
        foreach (var (z, red) in new[] { (-.74f, false), (0f, true), (.74f, false) })
            m.RoundedBox(red ? towel : cushion, new Vector3(cx + 1.66f, deck + .70f, cz + z), new Vector3(.22f, .44f, .64f), .08f, Quaternion.Euler(0, 0, 14f), 3);
        Col(cx - .95f, cx + 1.9f, deck, deck + .48f, cz - 1.2f, cz + 1.2f, "cabana " + nr + " łóżko");
        foreach (float z in new[] { -.58f, .58f })
        {
            var gs = new GameObject("cabana " + nr + " | miejsce " + (z < 0 ? 1 : 2)); gs.transform.SetParent(seats, false);
            gs.transform.localPosition = new Vector3(cx + .55f, deck + .49f, cz + z) * K;
            gs.transform.localRotation = Quaternion.LookRotation(Vector3.left);
            var s = gs.AddComponent<BraxSeat>(); s.lie = true; s.label = "CABANA VIP"; s.surfaceY = (deck + .49f) * K; Seats++;
        }
        // the side table: stainless drum, an ice bucket with a bottle, rolled towels
        var t = new Vector3(cx - 1.45f, deck, cz + 1.62f);
        m.Cylinder(steel, t, .26f, .52f, 20); m.Cylinder(white, t + Vector3.up * .52f, .28f, .03f, 20);
        m.Lathe(steel, t + Vector3.up * .55f, new[] { new Vector2(.08f, 0f), new Vector2(.11f, .2f), new Vector2(.118f, .21f) }, 18);
        m.Lathe(blackGloss, t + Vector3.up * .58f, new[] { new Vector2(.035f, 0f), new Vector2(.037f, .2f), new Vector2(.014f, .28f), new Vector2(.012f, .33f) }, 12);
        foreach (float dz in new[] { -.13f, .13f }) m.Cylinder(towel, t + new Vector3(.17f, .61f, dz - .02f), .06f, .34f, 12, Quaternion.Euler(0, 0, 90f), true, true);
        Col(t.x - .27f, t.x + .27f, deck, deck + .58f, t.z - .27f, t.z + .27f, "cabana " + nr + " stolik");
        Cabanas++;
    }
}
