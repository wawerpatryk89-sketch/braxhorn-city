using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 1 on the hill over the arena, after the owner's own photo (17.11 14:56, "No bo dobra, zrób takie":
// "A luxurious Mediterranean villa perched on a scenic cliff"; 15:17 on the first try "nic z tego nie przypomina" - a long
// grey block). As in his photo: ivory stucco, clay barrel-tile hip roofs on timber corbels at three heights, a U round the
// pool terrace - the two-storey house in the middle (three arches on columns before the salon, arched French windows and a
// balcony across over them), the two one-storey wings coming forward either side with their open arched loggias, a
// belvedere tower behind; the free-form pool on the very edge of the hill over a rock face, palms and shrubs everywhere.
// On the site's pad (x -24..27, z -22.6..26, the view to -z over the city):
//  - the house x -9..9, z 0..13 (the upper floor 4.2, the eaves 7.8): the loggia, the salon, the dining room, the hall with
//    the front door and the stair; upstairs the master suite and a bedroom with their bathrooms;
//  - the west wing x -21..-9, z -12..8: the open pavilion (arches front, courtyard and west) and the kitchen;
//  - the east wing x 9..21, z -10..10: the loggia (summer dining), the guest suite, the garage for two cars;
//  - the tower x -14..-9, z 8..13: from the upper corridor a spiral stair to the belvedere (8.2 m).
public static partial class BraxChampionsWille
{
    const float KE = 7.8f, KF1 = 4.2f, KEw = 4.4f, KPitch = 25f, KPitchW = 21f;
    static readonly Vector2[] KBasen = BasenObrys(-1f, -17.8f, 14.5f, 3.0f, .6f, 56);

    // the free-form pool's outline: a superellipse (exponent 2.4) widening to the east, counter-clockwise from its east end
    static Vector2[] BasenObrys(float cx, float cz, float ax, float az, float widen, int n)
    {
        var p = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            float t = i * Mathf.PI * 2f / n, c = Mathf.Cos(t), s = Mathf.Sin(t);
            float x = ax * Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / 2.4f);
            float z = (az + widen * x / ax) * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 2f / 2.4f);
            p[i] = new Vector2(cx + x, cz + z);
        }
        return p;
    }

    static void PlanKlif()
    {
        pietro = KF1;
        var c = new BraxKatalog.Ctx { m = kat, root = g, cols = g, unit = 1f, water = null, inst = g, seat = (p, f, l) => { W.Siedzisko(p, f, l); Siedziska++; } };
        string tag = Nazwy[0];
        KTeren(); KDom(); KSkrzydla(); KWieza(); KWnetrze(c, tag); KOgrod(tag);
    }

    // ================================================================ the pad: the pool on the edge, the rock face, terraces
    // as his photo: the pool's terrace on the very edge of a rock cliff, a lawn and clipped hedges beside it, the hill
    // falling away under it. The site drops the ground before the pad 9 m (BraxChampionsWilleDzialki.PrzodKlif); the cliff
    // is built here, a real eroded rock face (Poly Haven cliff_side) on a jagged grid, flush under the terrace's edge
    static void KTeren()
    {
        Out_(true);
        const float s = .3f;
        BasenKsztalt("willa_1_basen", KBasen, new Vector4(-18.5f, 16.5f, PadZ0, -12.5f));
        // west of the pool terrace a lawn to the cliff's edge; east the jacuzzi's paving in a lawn
        MM(lawn, PadX0, -18.5f, -s, 0f, PadZ0, -12f);
        MM(lawn, 16.5f, PadX1, -s, 0f, PadZ0, -19f); MM(lawn, 16.5f, 18f, -s, 0f, -19f, -13f); MM(lawn, 24f, PadX1, -s, 0f, -19f, -13f);
        MM(deck, 18f, 24f, -s, 0f, -19f, -13f); MM(lawn, 16.5f, PadX1, -s, 0f, -13f, -10f);
        MM(deck, -18.5f, -9f, -s, 0f, -12.5f, -12f); MM(deck, -9f, 9f, -s, 0f, -12.5f, 0f); MM(deck, 9f, 16.5f, -s, 0f, -12.5f, -10f);
        MM(lawn, PadX0, -21f, -s, 0f, -12f, 8f); MM(lawn, 21f, PadX1, -s, 0f, -10f, 10f); MM(lawn, PadX0, -14f, -s, 0f, 8f, PadZ1);
        MM(paving, 9f, PadX1, -s, 0f, 10f, 13f); MM(paving, -14f, PadX1, -s, 0f, 13f, PadZ1);
        // the pool terrace's edge over the cliff (its paving has no thickness): a fascia of its stone to the cliff's top
        { Vector3 f0 = new Vector3(-18.5f, -s, PadZ0), f1 = new Vector3(-18.5f, 0f, PadZ0), f2 = new Vector3(16.5f, 0f, PadZ0), f3 = new Vector3(16.5f, -s, PadZ0);
          if (Vector3.Dot(Vector3.Cross(f1 - f0, f2 - f0), Vector3.back) < 0f) b.Quad(deck, f3, f2, f1, f0); else b.Quad(deck, f0, f1, f2, f3); }
        // the cliff: under the front edge 9 m down, along the sides as the ground there rises to the back
        KlifSciana(true, PadZ0, PadX0 - 1f, PadX1 + 1f, -1f, a => BraxChampionsWilleDzialki.PrzodKlif - .6f, 3);
        KlifSciana(false, PadX0, PadZ0 - 1f, PadZ0 + 8.6f, -1f, z => BraxChampionsWilleDzialki.Uskok(1, z) - .6f, 11);
        KlifSciana(false, PadX1, PadZ0 - 1f, PadZ0 + 8.6f, 1f, z => BraxChampionsWilleDzialki.Uskok(1, z) - .6f, 17);
        Tralki(true, PadZ0 + .15f, PadX0 + .2f, PadX1 - .2f, 0f);
        Fontanna(new Vector3(4f, 0f, 20f), 2.2f);
    }

    /// <summary>A rock face on plane (outward: the open side) over a0..a1, from the terrace's underside (-.3) down to
    /// foot(a): a grid of ~0.9 m cells pushed out up to 1.5 m by the rock's relief (fractal bulges and the strata's
    /// ledges), flush at the top so the terrace's edge sits on it; UVs in metres (the map's 1.83 m); colliders behind.</summary>
    static void KlifSciana(bool alongX, float plane, float a0, float a1, float outward, System.Func<float, float> foot, int seed)
    {
        Out_(true);
        const float top = -.3f;
        int nu = Mathf.Max(1, Mathf.CeilToInt((a1 - a0) / .9f));
        float low = top; for (int i = 0; i <= nu; i++) low = Mathf.Min(low, foot(a0 + (a1 - a0) * i / nu));
        int nv = Mathf.Max(2, Mathf.CeilToInt((top - low) / .85f));
        Vector3 P(int i, int j)
        {
            float a = a0 + (a1 - a0) * i / nu, y = Mathf.Lerp(top, foot(a), j / (float)nv);
            float amp = 1.5f * Mathf.Clamp01((top - y) / 1.6f);
            float n = BraxChampionsHorizon.Fbm(a / 2.6f + seed, y / 1.3f - seed * .7f, 4);
            float ledge = Mathf.Sin(y * 2.1f + Mathf.Sin(a * .37f + seed) * 1.6f);
            float o = amp * Mathf.Clamp01(1.25f * n - .15f + .22f * ledge);
            return alongX ? new Vector3(a, y, plane + outward * o) : new Vector3(plane + outward * o, y, a);
        }
        Vector2 U(Vector3 p) => new Vector2(alongX ? p.x : p.z, p.y);
        var face = alongX ? new Vector3(0f, 0f, outward) : new Vector3(outward, 0f, 0f);
        for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                Vector3 p0 = P(i, j), p1 = P(i + 1, j), p2 = P(i + 1, j + 1), p3 = P(i, j + 1);
                if (Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), face) < 0f) b.QuadUV(skala, p3, p2, p1, p0, U(p3), U(p2), U(p1), U(p0));
                else b.QuadUV(skala, p0, p1, p2, p3, U(p0), U(p1), U(p2), U(p3));
            }
        // the walls the player meets: inside the relief, 3 m long, the face's full height
        for (float a = a0; a < a1 - .01f; a += 3f)
        {
            float e = Mathf.Min(a + 3f, a1), f = Mathf.Min(foot(a), foot(e)), h = top - f, m = (a + e) * .5f;
            var c = alongX ? new Vector3(m, (top + f) * .5f, plane + outward * .3f) : new Vector3(plane + outward * .3f, (top + f) * .5f, m);
            W.Kolizja(c, alongX ? new Vector3(e - a, h, .6f) : new Vector3(.6f, h, e - a), Quaternion.identity);
        }
    }

    // ================================================================ the house
    static void KDom()
    {
        var zew = tynkKrem;
        Out_(false); MM(floorGf, -8.7f, 8.7f, -.25f, 0f, 3.3f, 12.7f);
        Out_(true); MM(deck, -8.7f, 8.7f, -.25f, 0f, 0f, 3.3f); MM(deck, -1f, 1f, -.25f, 0f, 12.7f, 13f);
        // ---- the ground front (z 0): three arches on two columns before the loggia, an arched window either side
        float ys = 2.15f;
        var arch = new[] { new Vector4(-5.7f, -2.34f, 0f, ys + 1.68f), new Vector4(-1.66f, 1.66f, 0f, ys + 1.66f), new Vector4(2.34f, 5.7f, 0f, ys + 1.68f) };
        var otw = new System.Collections.Generic.List<Vector4>(arch) { new Vector4(-2.34f, -1.66f, 0f, ys), new Vector4(1.66f, 2.34f, 0f, ys),
            new Vector4(-8f, -7f, .9f, 2.8f), new Vector4(7f, 8f, .9f, 2.8f) };
        ScianaOE(zew, true, 0f, -9f, 9f, 0f, KF1, -1f, false, RogX(-9f, 9f), otw.ToArray());
        foreach (var o in arch) Luk(true, 0f, o.x, o.y, ys, o.w, -1f, zew, zew);
        OknoLuk(true, 0f, -8f, -7f, .9f, 2.3f, 2.8f, -1f, zew); OknoLuk(true, 0f, 7f, 8f, .9f, 2.3f, 2.8f, -1f, zew);
        Kolumna(new Vector3(-2f, 0f, .15f), ys, .25f); Kolumna(new Vector3(2f, 0f, .15f), ys, .25f);
        // the loggia's timber ceiling, its back wall (z 3) with the salon's arched French doors
        Out_(true);
        MM(zew, -8.7f, 8.7f, KF1 - .3f, KF1 - .04f, .3f, 3f, false);
        for (float x = -8.0f; x < 8.1f; x += 1.6f) b.Box(walnut, new Vector3(x, KF1 - .38f, 1.65f), new Vector3(.16f, .16f, 2.7f), Quaternion.identity);
        var drzwi = new[] { new Vector4(-4.82f, -3.22f, 0f, 3.2f), new Vector4(-.8f, .8f, 0f, 3.2f), new Vector4(3.22f, 4.82f, 0f, 3.2f) };
        ScianaOE(zew, true, 3f, -9f, 9f, 0f, KF1 - .3f, -1f, false, new Vector4(-8.7f, 8.7f, -8.7f, 8.7f), drzwi);
        OknoLuk(true, 3f, -4.82f, -3.22f, 0f, 2.4f, 3.2f, -1f, zew); OknoLuk(true, 3f, -.8f, .8f, 0f, 2.4f, 3.2f, -1f, zew, false);
        OknoLuk(true, 3f, 3.22f, 4.82f, 0f, 2.4f, 3.2f, -1f, zew);
        // ---- the upper front: three arched French windows onto the balcony over the arches, a window either side
        var gora = new[] { new Vector4(-4.67f, -3.37f, KF1, KF1 + 2.95f), new Vector4(-.65f, .65f, KF1, KF1 + 2.95f), new Vector4(3.37f, 4.67f, KF1, KF1 + 2.95f),
                           new Vector4(-8f, -7f, KF1 + .9f, KF1 + 2.7f), new Vector4(7f, 8f, KF1 + .9f, KF1 + 2.7f) };
        ScianaOE(zew, true, 0f, -9f, 9f, KF1, KE, -1f, false, RogX(-9f, 9f), gora);
        foreach (var o in gora) OknoLuk(true, 0f, o.x, o.y, o.z, o.w - (o.y - o.x) * .5f, o.w, -1f, zew);
        Out_(true);
        MM(deck, -6.2f, 6.2f, KF1 - .22f, KF1, -1.25f, 0f);
        for (float x = -5.9f; x < 6.0f; x += 1.18f) b.Box(deck, new Vector3(x, KF1 - .42f, -.62f), new Vector3(.22f, .4f, 1.1f), Quaternion.identity);
        Tralki(true, -1.12f, -6.2f, 6.2f, KF1); Tralki(false, -6.07f, -1.25f, -.05f, KF1); Tralki(false, 6.07f, -1.25f, -.05f, KF1);
        // ---- the sides (the wings butt against the ground floor; the tower against the west) and the back
        ScianaOE(zew, false, -9f, 0f, 13f, 0f, KF1, -1f, false, RogZ(0f, 13f, false), new Vector4(5f, 6.2f, 0f, 2.25f));
        ScianaOE(zew, false, -9f, 0f, 13f, KF1, KE, -1f, false, RogZ(0f, 13f, false), new Vector4(2f, 3.1f, KF1 + .9f, KF1 + 2.7f), new Vector4(10f, 10.9f, KF1, KF1 + 2.25f));
        OknoLuk(false, -9f, 2f, 3.1f, KF1 + .9f, KF1 + 2.15f, KF1 + 2.7f, -1f, zew);
        ScianaOE(zew, false, 9f, 0f, 13f, 0f, KF1, 1f, false, RogZ(0f, 13f, false), new Vector4(1f, 2f, 0f, 2.25f), new Vector4(7.5f, 8.4f, 0f, 2.25f));
        ScianaOE(zew, false, 9f, 0f, 13f, KF1, KE, 1f, false, RogZ(0f, 13f, false), new Vector4(2f, 3.1f, KF1 + .9f, KF1 + 2.7f), new Vector4(9.5f, 10.6f, KF1 + .9f, KF1 + 2.7f));
        OknoLuk(false, 9f, 2f, 3.1f, KF1 + .9f, KF1 + 2.15f, KF1 + 2.7f, 1f, zew); OknoLuk(false, 9f, 9.5f, 10.6f, KF1 + .9f, KF1 + 2.15f, KF1 + 2.7f, 1f, zew);
        ScianaOE(zew, true, 13f, -9f, 9f, 0f, KF1, 1f, false, RogX(-9f, 9f), new Vector4(-1f, 1f, 0f, 3.4f), new Vector4(-4.6f, -3.6f, .9f, 2.9f), new Vector4(3.6f, 4.6f, .9f, 2.9f));
        OknoLuk(true, 13f, -1f, 1f, 0f, 2.4f, 3.4f, 1f, zew, false);
        OknoLuk(true, 13f, -4.6f, -3.6f, .9f, 2.4f, 2.9f, 1f, zew); OknoLuk(true, 13f, 3.6f, 4.6f, .9f, 2.4f, 2.9f, 1f, zew);
        Out_(false); BraxChampionsInteriorDoor.Single("drzwi wejściowe", "WILLA | drzwi wejściowe (orzech)", true, 12.85f, -1f, 1f, 0f, 2.4f, walnut, steel, .07f);
        ScianaOE(zew, true, 13f, -9f, 9f, KF1, KE, 1f, false, RogX(-9f, 9f), new Vector4(-4.6f, -3.6f, KF1 + .9f, KF1 + 2.85f), new Vector4(-.5f, .5f, KF1 + .9f, KF1 + 2.85f), new Vector4(3.6f, 4.6f, KF1 + .9f, KF1 + 2.85f));
        foreach (float x in new[] { -4.6f, -.5f, 3.6f }) OknoLuk(true, 13f, x, x + 1f, KF1 + .9f, KF1 + 2.35f, KF1 + 2.85f, 1f, zew);
        // the entrance: two lanterns, the house name in 3D letters, plants
        Out_(true);
        foreach (float lx in new[] { -1.7f, 1.7f })
        {
            b.Box(black, new Vector3(lx, 2.4f, 13.1f), new Vector3(.18f, .4f, .18f), Quaternion.identity);
            b.Box(downlight, new Vector3(lx, 2.4f, 13.2f), new Vector3(.12f, .26f, .01f), Quaternion.identity);
            W.Lampa("wejście | latarnia", new Vector3(lx, 2.3f, 13.6f), .5f, 4f * K, new Color(1f, .82f, .58f));
        }
        BraxChampionsLetters3D.Text(b, steel, black, Nazwy[0], new Vector3(-2.9f, 3.0f, 13.012f), BraxChampionsLetters3D.YawFacing(Vector3.forward), .14f, .012f, .12f);
        // ---- the floors (the stair's well), the upper ceiling, the roof, two chimneys; the stair up the east wall
        Out_(false);
        SlabRect(ceiling, -8.7f, 8.7f, KF1 - .3f, KF1 - .04f, 3f, 12.7f, 7.25f, 8.7f, 4.8f, 11.21f);
        SlabRect(floorUf, -8.7f, 8.7f, KF1 - .04f, KF1, .3f, 12.7f, 7.25f, 8.7f, 4.8f, 11.21f);
        MM(ceiling, -8.7f, 8.7f, KE - .2f, KE, .3f, 12.7f, false);
        Dach4(-9f, 9f, 0f, 13f, KE, KPitch, zew);
        Komin(-5.5f, 10f, KE, 11.2f); Komin(5.5f, 10f, KE, 11.2f);
        Schody(floorGf, 7.36f, 8.7f, 5.0f, 0f, KF1, 23, .27f, 7.36f);
        Balustrada(false, 7.22f, 8.2f, 11.21f, KF1);
    }

    // ================================================================ the wings
    static void KSkrzydla()
    {
        var zew = tynkKrem;
        // ---- the west wing: the open pavilion (three arches to the terrace, three to the courtyard, two to the west), the kitchen
        Out_(true); MM(deck, -20.7f, -9f, -.25f, 0f, -11.7f, -2.06f);
        Out_(false); MM(floorGf, -20.7f, -9f, -.25f, 0f, -1.94f, 7.7f);
        var fr = new[] { new Vector4(-20.4f, -17.2f, 0f, 3.9f), new Vector4(-16.6f, -13.4f, 0f, 3.9f), new Vector4(-12.8f, -9.6f, 0f, 3.9f) };
        ScianaOE(zew, true, -12f, -21f, -9f, 0f, KEw, -1f, false, RogX(-21f, -9f), fr);
        foreach (var o in fr) Luk(true, -12f, o.x, o.y, 2.3f, 3.9f, -1f, zew, zew);
        var dz = new[] { new Vector4(-11.4f, -8.2f, 0f, 3.9f), new Vector4(-7.6f, -4.4f, 0f, 3.9f), new Vector4(-3.8f, -.6f, 0f, 3.9f) };
        ScianaOE(zew, false, -9f, -12f, 0f, 0f, KEw, 1f, false, new Vector4(-12f, 0f, -11.7f, 0f), dz);
        foreach (var o in dz) Luk(false, -9f, o.x, o.y, 2.3f, 3.9f, 1f, zew, zew);
        var zw = new[] { new Vector4(-11.2f, -8.0f, 0f, 3.9f), new Vector4(-7.4f, -4.2f, 0f, 3.9f), new Vector4(1f, 2f, .9f, 2.8f), new Vector4(5f, 6f, .9f, 2.8f) };
        ScianaOE(zew, false, -21f, -12f, 8f, 0f, KEw, -1f, false, RogZ(-12f, 8f, false), zw);
        Luk(false, -21f, -11.2f, -8.0f, 2.3f, 3.9f, -1f, zew, zew); Luk(false, -21f, -7.4f, -4.2f, 2.3f, 3.9f, -1f, zew, zew);
        OknoLuk(false, -21f, 1f, 2f, .9f, 2.3f, 2.8f, -1f, zew); OknoLuk(false, -21f, 5f, 6f, .9f, 2.3f, 2.8f, -1f, zew);
        ScianaOE(zew, true, 8f, -21f, -9f, 0f, KEw, 1f, false, new Vector4(-20.85f, -9f, -20.7f, -9f), new Vector4(-18.5f, -17.5f, .9f, 2.8f));
        OknoLuk(true, 8f, -18.5f, -17.5f, .9f, 2.3f, 2.8f, 1f, zew);
        Scianka(true, -2f, -20.7f, -9f, 0f, KEw - .3f, new Vector2(-12.6f, -11.7f)); Drzwi("kuchnia", true, -2f, -12.6f, -11.7f, 0f);
        Out_(true); MM(zew, -20.7f, -9f, KEw - .3f, KEw, -11.7f, -2.06f, false);
        for (float z = -11f; z < -2.3f; z += 1.6f) b.Box(walnut, new Vector3(-14.85f, KEw - .38f, z), new Vector3(11.6f, .16f, .16f), Quaternion.identity);
        Out_(false); MM(ceiling, -20.7f, -9f, KEw - .3f, KEw, -1.94f, 7.7f, false);
        Dach4(-21f, -9f, -12f, 8f, KEw, KPitchW, zew, .6f, 2);
        Komin(-18.5f, 5.5f, KEw, 7.9f, .8f, .6f);
        // ---- the east wing: the loggia (arches front, courtyard and east), the guest suite, the garage for two cars
        Out_(true); MM(deck, 9f, 20.7f, -.25f, 0f, -9.7f, -2.06f);
        Out_(false); MM(floorGf, 9f, 20.7f, -.25f, 0f, -1.94f, 3.94f); MM(concrete, 9f, 20.7f, -.25f, 0f, 4.06f, 9.7f);
        Out_(true); MM(concrete, 11f, 19.5f, -.25f, 0f, 9.7f, 10f);
        var ef = new[] { new Vector4(9.6f, 12.8f, 0f, 3.9f), new Vector4(13.4f, 16.6f, 0f, 3.9f), new Vector4(17.2f, 20.4f, 0f, 3.9f) };
        ScianaOE(zew, true, -10f, 9f, 21f, 0f, KEw, -1f, false, RogX(9f, 21f), ef);
        foreach (var o in ef) Luk(true, -10f, o.x, o.y, 2.3f, 3.9f, -1f, zew, zew);
        var ed = new[] { new Vector4(-9.4f, -6.2f, 0f, 3.9f), new Vector4(-5.6f, -2.4f, 0f, 3.9f) };
        ScianaOE(zew, false, 9f, -10f, 0f, 0f, KEw, -1f, false, new Vector4(-10f, 0f, -9.7f, 0f), ed);
        foreach (var o in ed) Luk(false, 9f, o.x, o.y, 2.3f, 3.9f, -1f, zew, zew);
        var ee = new[] { new Vector4(-9.4f, -6.2f, 0f, 3.9f), new Vector4(-5.6f, -2.4f, 0f, 3.9f), new Vector4(.5f, 1.6f, .9f, 2.8f) };
        ScianaOE(zew, false, 21f, -10f, 10f, 0f, KEw, 1f, false, RogZ(-10f, 10f, false), ee);
        Luk(false, 21f, -9.4f, -6.2f, 2.3f, 3.9f, 1f, zew, zew); Luk(false, 21f, -5.6f, -2.4f, 2.3f, 3.9f, 1f, zew, zew);
        OknoLuk(false, 21f, .5f, 1.6f, .9f, 2.3f, 2.8f, 1f, zew);
        ScianaOE(zew, true, 10f, 9f, 21f, 0f, KEw, 1f, false, new Vector4(9f, 20.85f, 9f, 20.7f), new Vector4(11f, 14.5f, 0f, 2.5f), new Vector4(16f, 19.5f, 0f, 2.5f));
        BramaGarazu("brama 1", 11f, 14.5f, 10f, 2.5f); BramaGarazu("brama 2", 16f, 19.5f, 10f, 2.5f);
        Scianka(true, -2f, 9f, 20.7f, 0f, KEw - .3f, new Vector2(12f, 12.9f)); Drzwi("apartament gościnny", true, -2f, 12f, 12.9f, 0f);
        Scianka(true, 4f, 9f, 20.7f, 0f, KEw - .3f);
        Out_(true); MM(zew, 9f, 20.7f, KEw - .3f, KEw, -9.7f, -2.06f, false);
        for (float z = -9f; z < -2.3f; z += 1.6f) b.Box(walnut, new Vector3(14.85f, KEw - .38f, z), new Vector3(11.6f, .16f, .16f), Quaternion.identity);
        Out_(false); MM(ceiling, 9f, 20.7f, KEw - .3f, KEw, -1.94f, 9.7f, false);
        Dach4(9f, 21f, -10f, 10f, KEw, KPitchW, zew, .6f, 1);
    }

    // ================================================================ the belvedere tower
    static void KWieza()
    {
        var zew = tynkKrem; const float x0 = -14f, x1 = -9f, z0 = 8f, z1 = 13f, top = 11.0f, bf = 8.2f;
        ScianaOE(zew, false, x0, z0, z1, 0f, bf, -1f, false, RogZ(z0, z1, false), new Vector4(9.9f, 11.1f, 5.2f, 7.0f));
        OknoLuk(false, x0, 9.9f, 11.1f, 5.2f, 6.4f, 7.0f, -1f, zew);
        ScianaOE(zew, true, z1, x0, x1, 0f, bf, 1f, false, new Vector4(x0 + T * .5f, x1, x0 + T, x1), new Vector4(-12f, -11f, 0f, 2.6f));
        OknoLuk(true, z1, -12f, -11f, 0f, 2.1f, 2.6f, 1f, zew, false);
        ScianaOE(zew, true, z0, x0, x1, KEw, bf, -1f, false, new Vector4(x0 + T * .5f, x1, x0 + T, x1));
        ScianaOE(zew, false, x1, z0, z1, KE, bf, 1f, false, RogZ(z0, z1, false));
        foreach (var (along, plane, a0, a1, outw) in new[] { (false, x0, z0, z1, -1f), (false, x1, z0, z1, 1f), (true, z1, x0, x1, 1f), (true, z0, x0, x1, -1f) })
        {
            var ob = new[] { new Vector4(a0 + .5f, a0 + 2.3f, bf, 10.5f), new Vector4(a1 - 2.3f, a1 - .5f, bf, 10.5f) };
            ScianaOE(zew, along, plane, a0, a1, bf, top, outw, false, along ? RogX(a0, a1) : RogZ(a0, a1, false), ob);
            foreach (var o in ob) { Luk(along, plane, o.x, o.y, 9.6f, 10.5f, outw, zew, zew); Tralki(along, plane - outw * .15f, o.x + .05f, o.y - .05f, bf); }
        }
        Out_(false);
        MM(floorGf, x0 + T, x1, -.25f, 0f, z0, z1 - T);
        MM(ceiling, x0 + T, x1, KF1 - .3f, KF1 - .04f, z0, z1 - T); MM(floorUf, x0 + T, x1, KF1 - .04f, KF1, z0, z1 - T);
        SlabRect(ceiling, x0 + T, x1 - T, 7.5f, bf - .04f, z0 + T, z1 - T, -13.05f, -9.95f, 8.95f, 12.05f);
        Out_(true); SlabRect(deck, x0 + T, x1 - T, bf - .04f, bf, z0 + T, z1 - T, -13.05f, -9.95f, 8.95f, 12.05f);
        MM(zew, x0 + T, x1 - T, top - .3f, top, z0 + T, z1 - T, false);
        Balustrada(true, 8.92f, -13.05f, -10.5f, bf); Balustrada(true, 12.08f, -13.05f, -9.95f, bf); Balustrada(false, -13.08f, 8.95f, 12.05f, bf);
        Dach4(x0, x1, z0, z1, top, KPitch, zew, .55f);
        SchodySpiralne(new Vector3(-11.5f, 0f, 10.5f), KF1, bf, 22, 1.5f, 250f, -330f);
    }

    // ================================================================ inside
    static void KWnetrze(BraxKatalog.Ctx c, string tag)
    {
        Out_(false);
        float y = KF1, top = KE - .2f;
        // ---- the salon (z 3.3..8): the stone fireplace on the west wall, sofas round it, the TV wall, the loggia's arches
        Scianka(true, 8f, -8.7f, 7.2f, 0f, KF1 - .3f, new Vector2(-6f, -3f), new Vector2(-1.5f, 1.5f));
        Scianka(false, -2f, 8f, 12.7f, 0f, KF1 - .3f, new Vector2(9.5f, 11.5f));
        MM(stoneIn, -8.7f, -8.5f, 0f, KF1 - .3f, 4.2f, 7.2f);
        MM(stoneIn, -8.5f, -8.1f, 0f, 1.25f, 4.0f, 7.4f); MM(stoneIn, -8.5f, -7.9f, 1.25f, 1.4f, 3.8f, 7.6f);
        MM(black, -8.5f, -8.48f, .25f, 1.0f, 4.9f, 6.5f, false); MM(fire, -8.48f, -8.46f, .3f, .5f, 5.1f, 6.3f, false);
        W.Lampa(tag + " | kominek", new Vector3(-7.6f, .6f, 5.7f), .8f, 5f * K, new Color(1f, .55f, .22f));
        BraxKatalog.Rug(c, new Vector3(-5.2f, 0f, 5.7f), Vector3.forward, 4.6f, 3.6f, true);
        BraxKatalog.Sofa(c, new Vector3(-3.2f, .012f, 5.7f), Vector3.left, 3.2f, 1, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(-5.3f, .012f, 5.7f), Vector3.left, 1.3f, .7f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 4.0f), 0f), new Vector3(-5.3f, .45f, 4.0f), Vector3.forward, "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-5.3f, .012f, 7.4f), 180f), new Vector3(-5.3f, .45f, 7.4f), Vector3.back, "FOTEL");
        BraxKatalog.Rug(c, new Vector3(3.6f, 0f, 5.6f), Vector3.forward, 4.2f, 3.4f, false);
        BraxKatalog.Sofa(c, new Vector3(3.6f, .012f, 4.1f), Vector3.forward, 3.2f, 0, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(3.6f, .012f, 5.6f), Vector3.forward, 1.2f, .6f);
        BraxKatalog.Tv(c, new Vector3(3.6f, 1.6f, 7.94f), Vector3.back, 85f, 1, false, tag + " salon TV");
        BraxKatalog.FloorLamp(c, new Vector3(-8.0f, 0f, 3.8f)); BraxKatalog.FloorLamp(c, new Vector3(6.4f, 0f, 3.8f));
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-8.1f, 0f, 7.5f), 70f);
        Obrazek(1, new Vector3(-7.1f, 2.1f, 7.94f), Vector3.back, 2.0f, 1.2f);
        Oczka(-7.4f, 6.6f, 4.2f, 7.0f, KF1 - .3f, 2.8f);
        foreach (var lp in new[] { new Vector3(-5f, KF1 - .6f, 5.6f), new Vector3(0f, KF1 - .6f, 5.6f), new Vector3(4f, KF1 - .6f, 5.6f) }) W.Lampa(tag + " | salon", lp, 1f, 9f * K, new Color(1f, .9f, .78f));
        // ---- the dining room (x -8.7..-2, z 8..12.7), the hall to the front door
        BraxKatalog.Rug(c, new Vector3(-5.4f, 0f, 10.4f), Vector3.forward, 5.0f, 3.0f, true);
        BraxKatalog.DiningSet(c, new Vector3(-6.2f, .012f, 10.4f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(-4.6f, .012f, 10.4f), Vector3.back, "krzesło");
        foreach (float x in new[] { -6.2f, -4.6f }) ModelTop("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", "lampa nad stołem", new Vector3(x, KF1 - 1.3f, 10.4f), KF1 - .3f);
        Obrazek(2, new Vector3(-8.69f, 2.0f, 10.4f), Vector3.right, 2.2f, 1.3f);
        b.RoundedBox(walnut, new Vector3(-1.72f, .42f, 12.0f), new Vector3(.45f, .84f, 1.2f), .01f, Quaternion.identity, 1);
        W.Kolizja(new Vector3(-1.72f, .42f, 12.0f), new Vector3(.45f, .84f, 1.2f), Quaternion.identity);
        BraxChampionsMirror.Wall(tag + " hol lustro", new Vector3(-1.93f, 1.7f, 12.0f), Vector3.right, 1.0f, 1.2f, mirrorFrame);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(1.6f, 0f, 12.2f), 30f);
        W.Lampa(tag + " | jadalnia", new Vector3(-5.4f, KF1 - .6f, 10.4f), .9f, 8f * K, new Color(1f, .9f, .78f));
        W.Lampa(tag + " | hol", new Vector3(1f, KF1 - .6f, 10.4f), .8f, 7f * K, new Color(1f, .9f, .78f));
        Oczka(-.6f, 4.2f, 9.2f, 11.6f, KF1 - .3f);
        // ---- the kitchen in the west wing (a low run on the west wall, the island), the pavilion's table and lounge
        BraxKatalog.Kitchen(c, new Vector3(-20.68f, 0f, -1.4f), Vector3.forward, Vector3.right,
            new[] { "szuflady", "zlew", "zmywarka", "szuflady", "plyta", "szuflady", "piekarnik", "szuflady" }, walnut, BraxKatalog.BlackMatt, BraxKatalog.Stone, tag + " kuchnia", false);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(-17.6f, .45f, 1.0f), new Vector3(1.0f, .87f, 3.0f), .01f, Quaternion.identity, 1);
        b.RoundedBox(BraxKatalog.Stone, new Vector3(-17.6f, .895f, 1.0f), new Vector3(1.15f, .05f, 3.2f), .008f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(-17.6f, .46f, 1.0f), new Vector3(1.15f, .92f, 3.2f), Quaternion.identity);
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(-16.65f, 0f, i * 1.0f);
            b.Cylinder(steel, sp, .025f, .7f, 10, Quaternion.identity, false); b.Cylinder(steel, sp, .2f, .015f, 20, Quaternion.identity, true);
            b.Cylinder(BraxKatalog.Leather, sp + Vector3.up * .66f, .2f, .07f, 20, Quaternion.identity, true);
            W.Siedzisko(sp + Vector3.up * .73f, Vector3.left, "HOKER"); Siedziska++;
        }
        BraxKatalog.DiningSet(c, new Vector3(-14.0f, .012f, 4.6f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(-12.4f, .012f, 4.6f), Vector3.back, "krzesło");
        W.Lampa(tag + " | kuchnia", new Vector3(-16f, KEw - .6f, 2f), .9f, 9f * K, new Color(1f, .93f, .82f));
        BraxKatalog.DiningSet(c, new Vector3(-16.6f, .012f, -7.0f), Vector3.back, "krzesło"); BraxKatalog.DiningSet(c, new Vector3(-15.0f, .012f, -7.0f), Vector3.back, "krzesło");
        Out_(true);
        Lawka(new Vector3(-14.85f, 0f, -2.6f), Vector3.back, 2.4f); Lawka(new Vector3(-19.9f, 0f, -4.5f), Vector3.right, .9f);
        W.Lampa(tag + " | pawilon", new Vector3(-14.8f, KEw - .7f, -7f), .9f, 9f * K, new Color(1f, .86f, .64f));
        // ---- the east wing's loggia (a lounge), the guest suite, the garage
        var lc = new Vector3(15f, 0f, -6f);
        Lawka(lc + new Vector3(0f, 0f, 1.4f), Vector3.back, 2.4f); Lawka(lc + new Vector3(-1.8f, 0f, 0f), Vector3.right, .9f); Lawka(lc + new Vector3(1.8f, 0f, 0f), Vector3.left, .9f);
        b.RoundedBox(stone, lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), .02f, Quaternion.identity, 2);
        W.Kolizja(lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), Quaternion.identity);
        W.Lampa(tag + " | loggia", new Vector3(15f, KEw - .7f, -6f), .9f, 9f * K, new Color(1f, .86f, .64f));
        Out_(false);
        Lazienka(17.06f, 20.7f, -1.94f, 3.94f, 0f, new Vector3(0f, .6f, 1.4f), 0f, 0f);
        Scianka(false, 17f, -1.94f, 3.94f, 0f, KEw - .3f, new Vector2(.6f, 1.4f)); Drzwi("łazienka gościnna", false, 17f, .6f, 1.4f, 0f);
        BraxKatalog.Rug(c, new Vector3(13f, 0f, .8f), Vector3.forward, 3.0f, 2.6f, true);
        BraxKatalog.Bed(c, new Vector3(13f, .012f, 2.58f), Vector3.back, 1.6f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(9.9f, 0f, -.4f), Vector3.right, 1.6f, oak, BraxKatalog.Walnut, 2.3f, "szafa");
        Prysznic(new Vector3(19.9f, 0f, 3.1f));
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .8f, walnut, "umywalka"), new Vector3(18.6f, 0f, -1.924f), Vector3.forward, null);
        BraxKatalog.WcPan(c, new Vector3(20.684f, 0f, -.6f), Vector3.left);
        W.Lampa(tag + " | apartament gościnny", new Vector3(13f, KEw - .6f, 1f), .8f, 7f * K, new Color(1f, .93f, .82f));
        MM(walnut, 20.2f, 20.7f, 0f, 2.0f, 5.0f, 9.0f);
        foreach (float x in new[] { 12.75f, 17.75f }) MM(black, x - .6f, x + .6f, 0f, .1f, 4.6f, 4.75f);
        W.Lampa("garaż | światło", new Vector3(15f, KEw - .6f, 7f), 1.1f, 9f * K);

        // ---- upstairs: the master suite (x -8.7..0) and the bedroom (x 0..7.2) at the front, the bathrooms, the dressing
        // room, the corridor to the stair and the tower
        Scianka(false, 0f, .3f, 4.7f, y, top);
        Scianka(true, 4.7f, -8.7f, 7.2f, y, top, new Vector2(-7.8f, -7.0f), new Vector2(-4.0f, -3.2f), new Vector2(-1.2f, -.3f), new Vector2(2.0f, 2.9f), new Vector2(5.5f, 6.3f));
        Scianka(false, -5.2f, 4.7f, 8.2f, y, top); Scianka(false, -2f, 4.7f, 8.2f, y, top); Scianka(false, 4.4f, 4.7f, 8.2f, y, top); Scianka(false, 7.2f, 4.7f, 8.2f, y, top);
        Scianka(true, 8.2f, -8.7f, -2f, y, top); Scianka(true, 8.2f, 4.4f, 7.2f, y, top);
        Drzwi("łazienka główna", true, 4.7f, -7.8f, -7.0f, y); Drzwi("garderoba", true, 4.7f, -4.0f, -3.2f, y); Drzwi("sypialnia główna", true, 4.7f, -1.2f, -.3f, y);
        Drzwi("sypialnia 2", true, 4.7f, 2.0f, 2.9f, y); Drzwi("łazienka 2", true, 4.7f, 5.5f, 6.3f, y);
        BraxKatalog.Rug(c, new Vector3(-4.4f, y, 2.2f), Vector3.forward, 3.6f, 2.6f, true);
        BraxKatalog.Bed(c, new Vector3(-4.4f, y + .012f, 3.34f), Vector3.back, 1.8f, "łóżko");
        BraxKatalog.Tv(c, new Vector3(-8.68f, y + 1.35f, 2.4f), Vector3.right, 65f, 0, false, tag + " sypialnia główna TV");
        Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel w sypialni", new Vector3(-7.6f, y, 1.0f), 200f), new Vector3(-7.6f, y + .42f, 1.0f), new Vector3(-.34f, 0f, -.94f), "FOTEL");
        BraxKatalog.Rug(c, new Vector3(3.6f, y, 2.2f), Vector3.forward, 3.2f, 2.6f, false);
        BraxKatalog.Bed(c, new Vector3(3.6f, y + .012f, 3.34f), Vector3.back, 1.6f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(8.1f, y, 2.2f), Vector3.left, 2.4f, oak, BraxKatalog.Walnut, 2.4f, "szafa");
        Lazienka(-8.7f, -5.26f, 4.76f, 8.14f, y, new Vector3(1f, -7.8f, -7.0f), 0f, 0f);
        Wanna(new Vector3(-6.9f, y, 7.3f)); BraxKatalog.WcPan(c, new Vector3(-8.684f, y, 5.6f), Vector3.right);
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.left, .8f, walnut, "umywalka"), new Vector3(-5.276f, y, 5.6f), Vector3.left, null);
        BraxKatalog.Wardrobe(c, new Vector3(-3.6f, y, 7.54f), Vector3.back, 2.9f, walnut, BraxKatalog.Walnut, 2.4f, "garderoba");
        Lazienka(4.46f, 7.14f, 4.76f, 8.14f, y, new Vector3(1f, 5.5f, 6.3f), 0f, 0f);
        Prysznic(new Vector3(5.3f, y, 7.3f)); BraxKatalog.WcPan(c, new Vector3(7.124f, y, 7.4f), Vector3.left);
        BraxKatalog.Place(c, "umywalka 0.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.left, .6f, walnut, "umywalka"), new Vector3(7.124f, y, 5.6f), Vector3.left, null);
        Obrazek(3, new Vector3(-4.4f, y + 1.8f, 4.64f), Vector3.back, 1.8f, 1.0f); Obrazek(4, new Vector3(3.6f, y + 1.8f, 4.64f), Vector3.back, 1.6f, .9f);
        Obrazek(5, new Vector3(-1.94f, y + 1.6f, 6.4f), Vector3.right, 1.2f, .8f);
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel na korytarzu", new Vector3(-6.5f, y, 12.0f), 180f), new Vector3(-6.5f, y + .45f, 12.0f), Vector3.back, "FOTEL");
        Oczka(-7.4f, 6.6f, 1.0f, 3.8f, top, 2.8f); Oczka(-8.0f, 6.4f, 10.4f, 10.4f, top, 2.4f);
        foreach (var lp in new[] { new Vector3(-4.4f, top - .3f, 2f), new Vector3(3.6f, top - .3f, 2f), new Vector3(-7f, top - .3f, 6.4f), new Vector3(5.8f, top - .3f, 6.4f), new Vector3(0f, top - .3f, 10.4f) })
            W.Lampa(tag + " | piętro", lp, .9f, 8f * K, new Color(1f, .93f, .82f));
        // the tower: wine racks on the ground, lights on its three storeys
        for (int i = 0; i < 5; i++) MM(oak, -13.7f, -13.3f, .4f + i * .4f, .43f + i * .4f, 8.6f, 12.4f);
        W.Lampa(tag + " | wieża", new Vector3(-11.5f, 2.6f, 10.5f), .5f, 5f * K, new Color(1f, .82f, .55f));
        W.Lampa(tag + " | wieża piętro", new Vector3(-11.5f, 7.0f, 10.5f), .6f, 5f * K, new Color(1f, .9f, .78f));
        W.Lampa(tag + " | belweder", new Vector3(-11.5f, 10.2f, 10.5f), .6f, 6f * K, new Color(1f, .86f, .64f));
    }

    // ================================================================ the garden: as in his photo - palms and trees everywhere
    // the region's own real greenery: the owner's palms (miasto_palmy) and drzewo_verdant (Poly Haven leaves, LOD); on the
    // terrace's corners, the lawns beside and behind the house and under the cliff (the site plants the hill round it)
    static void KOgrod(string tag)
    {
        Out_(true);
        Lezaki(new Vector3(-6.6f, 0f, -9.0f), 3, 2.2f, Vector3.back); Lezaki(new Vector3(2.2f, 0f, -9.0f), 3, 2.2f, Vector3.back);
        JacuzziW(new Vector3(21f, 0f, -16f));
        foreach (float x in new[] { -17.6f, -9f, 9.5f, 15.6f }) { b.Cylinder(black, new Vector3(x, 0f, -12.9f), .07f, .6f, 12, Quaternion.identity, true); W.Lampa(tag + " | taras", new Vector3(x, .7f, -12.7f), .5f, 6f * K, new Color(1f, .86f, .64f)); }
        // trees: the lawn beside the pool, east of the jacuzzi, the back lawn (crowns 5.4 m: 0.7 m and more off the roofs)
        foreach (var p in new[] { new Vector2(-21.4f, -17.6f), new Vector2(24.6f, -11.8f), new Vector2(-19.0f, 19.5f), new Vector2(-21.0f, 24.0f) })
            Drzewo(p.x, p.y);
        // palms: round the pool and on the hill's edge, the courtyard's corners, the lawns
        foreach (var p in new[] { new Vector2(-22.5f, -21.5f), new Vector2(-18.9f, -13.4f), new Vector2(15.6f, -21.0f), new Vector2(26f, -18.5f),
                                  new Vector2(-8.2f, -11.6f), new Vector2(8.2f, -11.6f), new Vector2(-23f, -9f), new Vector2(-23f, 0f), new Vector2(-23f, 6.5f),
                                  new Vector2(23.5f, -6f), new Vector2(23.5f, 3f), new Vector2(23.5f, 8.5f), new Vector2(-20f, 13f), new Vector2(-15.5f, 24.5f), new Vector2(25.5f, 22f) })
            Palma(p.x, p.y);
        // under the cliff: palms and trees on the hill's ground before its foot (9 m down)
        float under = BraxChampionsWilleDzialki.PrzodKlif;
        foreach (float x in new[] { -20f, -6.5f, 7f, 21f }) Palma(x, PadZ0 - 3.4f, under);
        foreach (float x in new[] { -13f, 0.5f, 14f }) Drzewo(x, PadZ0 - 4.0f, under);
        TablicaSprzedazy(1, new Vector3(1.0f, 0f, 23.6f), Vector3.forward,
            "MONIKA: Willa na wzgórzu nad areną, jak na śródziemnomorskim klifie: dwa skrzydła z arkadami wokół basenu na krawędzi skały, dachówka, wieża z belwederem, jacuzzi, fontanna, garaż na dwa auta, trzy sypialnie z łazienkami. Zapraszam do WAWER NIERUCHOMOŚCI w T12!");
    }
}
