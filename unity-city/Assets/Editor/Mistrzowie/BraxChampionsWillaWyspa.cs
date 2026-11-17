using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WILLA 2 on the island in the bay, after the owner's own photo (17.11 14:56, "No bo dobra, zrób takie":
// a white modern waterfront house of Miami) and his 14:02 "w tej na małej wyspie kort tenisowy, mały pomostek, miejsce na
// motorówkę, nie wiem, na mały jacht". As in his photo: two floors of black-framed glass, the upper floor's slab a white band
// with a glass balustrade, a thin white flat roof far over the walls in two heights, dark vertical slats on the upper floor,
// a covered dining terrace with a bar beside the house, the pool before the glass with a raised mosaic spa at its end, palms.
// The house is the region's glass villa (Bryla: 27 m of glass on both floors, the balcony, the rooms inside) in the scanned
// exterior stucco; no garage (no road reaches an island): the covered dining in its place.
// On the island's pad (x -48..27, z -22.6..26, the view to -z over the bay to the city):
//  - the tennis court west of the house: ITF 23.77 x 10.97 m with 6.40 m behind the baselines and 3.66 m beside the
//    sidelines (36.57 x 18.29 m), blue in a green surround, its lines cut in flush, the net 0.914 m in the middle;
//  - the stair down the pad's front to the beach and the jetty (the site's, BraxChampionsWilleDzialki.Pomost, with the
//    owner's Riva moored and its T head for a small yacht).
public static partial class BraxChampionsWille
{
    public const float WPadX0 = -48f;
    // the court: its centre, half its 36.57 x 18.29 m
    const float KCX = -37.1f, KCZ = 3.0f, KHX = 9.145f, KHZ = 18.285f;
    // the raised spa at the pool's east end (his photo: the mosaic box the pool's water spills from)
    const float SX0 = 3.3f, SX1 = 6.7f, SZ0 = -20.8f, SZ1 = -17.4f, SpaRim = .45f, SpaWater = .40f, SpaFloor = -.75f;
    // the stair from the pad's front down to the beach (the jetty's root at PomostX): 23 risers to the ground 3.95 m down
    const float WStX0 = 19f, WStX1 = 21f, WStDrop = 3.95f, WStTread = .30f; const int WStN = 23;
    static Material kortNiebieski, kortZielony, kortLinia;

    static void PlanWyspa()
    {
        pietro = FFL1;
        kortNiebieski = BraxChampionsFoundation.SourceMaterial("willa_kort_akryl_niebieski", "painted_concrete_02", 4f, new Color(.42f, .78f, 1.55f));
        kortZielony = BraxChampionsFoundation.SourceMaterial("willa_kort_akryl_zielony", "painted_concrete_02", 4f, new Color(.55f, 1.05f, .62f));
        kortLinia = BraxChampionsFoundation.SourceMaterial("willa_kort_linie", "painted_concrete_02", 4f, new Color(2.3f, 2.3f, 2.25f));
        Teren(); WTeren(); Basen(); WSpa(); Bryla(2); WMiami(); WJadalnia(); Wnetrze(2); Wyposazenie(2); Ogrod(2); WKort(); Palmy_(); WPalmy();
    }

    // ================================================================ the pad west of the old one, the stair to the beach
    static void WTeren()
    {
        Out_(true);
        const float s = .3f;
        float cx0 = KCX - KHX, cx1 = KCX + KHX, cz0 = KCZ - KHZ, cz1 = KCZ + KHZ;
        // lawn round the court to the pad's edges
        MM(lawn, WPadX0, cx0, -s, 0f, PadZ0 + .3f, PadZ1); MM(lawn, cx1, PadX0, -s, 0f, PadZ0 + .3f, PadZ1);
        MM(lawn, cx0, cx1, -s, 0f, PadZ0 + .3f, cz0); MM(lawn, cx0, cx1, -s, 0f, cz1, PadZ1);
        // the front: the retaining wall, its stone edge, the glass balustrade (as the old pad's)
        MM(stone, WPadX0, PadX0, -4f, -s, PadZ0, PadZ0 + .3f);
        MM(stone, WPadX0, PadX0, -s, 0f, PadZ0, PadZ0 + .3f, false);
        Balustrada(true, PadZ0 + .15f, WPadX0 + .2f, PadX0 + .2f, 0f);
        // the stair down to the beach: solid limestone steps, glass balustrades either side, the walk as a ramp
        float riser = WStDrop / WStN, zFoot = PadZ0 - WStN * WStTread;
        for (int i = 0; i < WStN; i++)
        {
            float top = -WStDrop + (i + 1) * riser, za = zFoot + i * WStTread, zb = za + WStTread + (i == WStN - 1 ? 0f : .02f);
            MM(deck, WStX0, WStX1, -WStDrop - .35f, top, za, zb, false);
        }
        W.Rampa(new Vector3((WStX0 + WStX1) * .5f, 0f, PadZ0), new Vector3((WStX0 + WStX1) * .5f, -WStDrop, zFoot), WStX1 - WStX0);
        float run = WStN * WStTread, slope = Mathf.Atan2(WStDrop, run) * Mathf.Rad2Deg, len = Mathf.Sqrt(WStDrop * WStDrop + run * run);
        foreach (float x in new[] { WStX0 + .04f, WStX1 - .04f })
        {
            var mid = new Vector3(x, -WStDrop * .5f + .55f, (PadZ0 + zFoot) * .5f);
            b.Box(glass, mid, new Vector3(.018f, 1.0f, len), Quaternion.Euler(slope, 0f, 0f));
            b.Box(steel, mid + new Vector3(0f, .52f, 0f), new Vector3(.05f, .04f, len), Quaternion.Euler(slope, 0f, 0f));
            W.Kolizja(mid, new Vector3(.06f, 1.0f, len), Quaternion.Euler(slope, 0f, 0f));
        }
    }

    // ================================================================ the raised mosaic spa at the pool's east end
    static void WSpa()
    {
        Out_(true);
        const float t = .22f;
        // the box: mosaic walls from its floor (under the deck: the site keeps the ground down here, StrefaBasenu) to the rim,
        // the travertine coping on top; on the pool side the wall stops at the water: it spills into the pool
        MM(poolTile, SX0, SX1, SpaFloor - .2f, SpaFloor, SZ0, SZ1);
        MM(poolTile, SX1 - t, SX1, SpaFloor, SpaRim, SZ0, SZ1);
        MM(poolTile, SX0, SX1 - t, SpaFloor, SpaRim, SZ0, SZ0 + t); MM(poolTile, SX0, SX1 - t, SpaFloor, SpaRim, SZ1 - t, SZ1);
        MM(poolTile, SX0, SX0 + t, SpaFloor, SpaWater - .01f, SZ0 + t, SZ1 - t);
        MM(deck, SX1 - t - .03f, SX1 + .03f, SpaRim, SpaRim + .05f, SZ0 - .03f, SZ1 + .03f, false);
        MM(deck, SX0 - .03f, SX1 - t - .03f, SpaRim, SpaRim + .05f, SZ0 - .03f, SZ0 + t, false);
        MM(deck, SX0 - .03f, SX1 - t - .03f, SpaRim, SpaRim + .05f, SZ1 - t, SZ1 + .03f, false);
        // the seat ledge round three sides, 45 cm under the water
        MM(poolTile, SX1 - t - .45f, SX1 - t, SpaFloor, SpaWater - .45f, SZ0 + t, SZ1 - t);
        MM(poolTile, SX0 + t, SX1 - t - .45f, SpaFloor, SpaWater - .45f, SZ0 + t, SZ0 + t + .45f);
        MM(poolTile, SX0 + t, SX1 - t - .45f, SpaFloor, SpaWater - .45f, SZ1 - t - .45f, SZ1 - t);
        Plane(bw, spaWater, new Vector3(SX0 + .001f, SpaWater, SZ0 + t), new Vector3(SX1 - t - SX0 - .001f, 0, 0), new Vector3(0, 0, SZ1 - SZ0 - 2f * t));
        b.Cylinder(BraxKatalog.Led, new Vector3((SX0 + SX1) * .5f, SpaFloor + .3f, SZ1 - t - .001f), .1f, .012f, 16, Quaternion.Euler(90f, 0f, 0f), true);
        var spa = new GameObject("spa | lustro wody").transform; spa.SetParent(g, false); spa.localPosition = new Vector3((SX0 + SX1) * .5f, SpaWater, (SZ0 + SZ1) * .5f);
        var zone = new GameObject("spa | strefa").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = new Vector3((SX0 + SX1) * .5f, (SpaFloor + .05f + SpaWater + .4f) * .5f, (SZ0 + SZ1) * .5f);
        zone.size = new Vector3(SX1 - SX0 - .6f, SpaWater + .4f - SpaFloor - .05f, SZ1 - SZ0 - .6f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = spa; sw.basinId = "willa_2_spa";
        foreach (var (p, f) in new[] { (new Vector3(SX1 - t - .25f, SpaWater - .45f + .02f, (SZ0 + SZ1) * .5f), Vector3.left),
                                      (new Vector3((SX0 + SX1) * .5f, SpaWater - .45f + .02f, SZ0 + t + .25f), Vector3.forward),
                                      (new Vector3((SX0 + SX1) * .5f, SpaWater - .45f + .02f, SZ1 - t - .25f), Vector3.back) })
        { W.Siedzisko(p, f, "SPA"); Siedziska++; }
        // two travertine steps up from the terrace on its house side
        MM(deck, SX0 + .6f, SX1 - .6f, 0f, .15f, SZ1, SZ1 + .35f); MM(deck, SX0 + .6f, SX1 - .6f, 0f, .30f, SZ1, SZ1 + .05f, false);
    }

    // ================================================================ the Miami lines: the thin roof in two heights, the slats
    static void WMiami()
    {
        Out_(true);
        // the east part of the upper floor rises 0.7 m over the parapet (his photo: the roof steps up on the right); both roofs a
        // thin white plate (25 cm) far over the walls - 2.4 m over the balcony, 1.4-2.2 m elsewhere, its underside white
        float r0 = RoofTop + .3f, step = .7f;
        MM(render, 2f, UX1, r0, r0 + step, UZ0, UZ1);
        MM(render, UX0 - 2.2f, 2f, r0, r0 + .25f, UZ0 - 2.4f, UZ1 + 1.4f);
        MM(render, 2f, UX1 + 2.2f, r0 + step, r0 + step + .25f, UZ0 - 2.4f, UZ1 + 1.4f);
        // the upper floor's slab edge a white band (his photo), over the oak of the cantilever's edge: front and both sides
        MM(render, UX0 - .02f, UX1 + .02f, H0, FFL1, UZ0 - .02f, UZ0, false);
        MM(render, UX0 - .02f, UX0, H0, FFL1, UZ0, UZ1, false); MM(render, UX1, UX1 + .02f, H0, FFL1, UZ0, CZ0, false);
        // dark vertical slats (charcoal aluminium 50 x 70 mm, every 140 mm): a screen before the upper floor's west glass and
        // the whole west side of the upper floor (to the court)
        void Lamele(bool alongX, float plane, float a0, float a1, float y0, float y1)
        {
            int n = Mathf.FloorToInt((a1 - a0) / .14f);
            for (int i = 0; i <= n; i++)
            {
                float a = a0 + i * (a1 - a0) / n;
                if (alongX) MM(alu, a - .025f, a + .025f, y0, y1, plane - .035f, plane + .035f, false);
                else MM(alu, plane - .035f, plane + .035f, y0, y1, a - .025f, a + .025f, false);
            }
            if (alongX) { MM(alu, a0 - .03f, a1 + .03f, y0, y0 + .06f, plane - .045f, plane + .045f, false); MM(alu, a0 - .03f, a1 + .03f, y1 - .06f, y1, plane - .045f, plane + .045f, false); }
            else { MM(alu, plane - .045f, plane + .045f, y0, y0 + .06f, a0 - .03f, a1 + .03f, false); MM(alu, plane - .045f, plane + .045f, y1 - .06f, y1, a0 - .03f, a1 + .03f, false); }
        }
        Lamele(true, UGlass - .16f, UX0 + T + .1f, -12.2f, FFL1 + .02f, Roof - .02f);                        // west of the balcony's door
        Lamele(false, UX0 - .06f, UGlass + .1f, UZ1 - .1f, FFL1 + .02f, Roof - .02f);
        W.Kolizja(new Vector3((UX0 + T + .1f - 12.2f) * .5f, (FFL1 + Roof) * .5f, UGlass - .16f), new Vector3(-12.2f - UX0 - T - .1f, Roof - FFL1, .08f), Quaternion.identity);
    }

    // ================================================================ the covered dining beside the house (where a garage was)
    static void WJadalnia()
    {
        Out_(true);
        const float s = .3f, x0 = 16f, x1 = 23.6f, z0 = -5f, z1 = 8.6f, y = 3.0f;
        MM(deck, GX1, CX1, -s, 0f, CZ0, CZ1);
        // the house's east wall there: stucco outside (the pantry's door z 6.5..7.45 to the outdoor kitchen)
        MM(render, GX1, GX1 + .02f, 0f, H0, CZ0, 6.5f, false); MM(render, GX1, GX1 + .02f, 0f, H0, 7.45f, GZ1, false);
        MM(render, GX1, GX1 + .02f, 2.26f, H0, 6.5f, 7.45f, false);
        // the canopy: a thin white plate on three black columns, its soffit oak (his photo: the wood ceiling over the dining)
        MM(render, x0, x1, y, y + .22f, z0, z1);
        MM(soffit, x0, x1, y - .02f, y, z0, z1, false);
        foreach (var c in new[] { new Vector2(x1 - .25f, z0 + .25f), new Vector2(x1 - .25f, z1 - .25f), new Vector2(x0 + .3f, z0 + .25f) })
        { b.Cylinder(stalCzarna, new Vector3(c.x, 0f, c.y), .07f, y - .02f, 16, Quaternion.identity, false); W.Kolizja(new Vector3(c.x, y * .5f, c.y), new Vector3(.16f, y, .16f), Quaternion.identity); }
        foreach (var lp in new[] { new Vector3(18f, y - .03f, -1f), new Vector3(21.5f, y - .03f, -1f), new Vector3(18f, y - .03f, 4f), new Vector3(21.5f, y - .03f, 4f) })
        { b.Cylinder(downlight, lp, .065f, .003f, 16, Quaternion.identity, false, true); W.Lampa("jadalnia | oczko", lp + Vector3.down * .3f, .4f, 5f * K, new Color(1f, .86f, .64f)); }
        // dining for eight: two oak tables end to end along the canopy, on the deck
        var c8 = new BraxKatalog.Ctx { m = kat, root = g, cols = g, unit = 1f, water = null, inst = g, seat = (p, f, l) => { W.Siedzisko(p, f, l); Siedziska++; } };
        BraxKatalog.DiningSet(c8, new Vector3(19.0f, .012f, -.4f), Vector3.right, "krzesło");
        BraxKatalog.DiningSet(c8, new Vector3(19.0f, .012f, 1.2f), Vector3.right, "krzesło");
        // the outdoor kitchen and bar on the back: a stone counter with a teak front, four stools
        b.RoundedBox(teak, new Vector3(20.2f, .46f, 7.7f), new Vector3(5.6f, .92f, .7f), .01f, Quaternion.identity, 1);
        b.RoundedBox(BraxKatalog.Stone, new Vector3(20.2f, .945f, 7.6f), new Vector3(5.8f, .05f, .95f), .008f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(20.2f, .48f, 7.65f), new Vector3(5.8f, .96f, .95f), Quaternion.identity);
        for (int i = 0; i < 4; i++)
        {
            var sp = new Vector3(18.4f + i * 1.2f, 0f, 6.6f);
            b.Cylinder(steel, sp, .025f, .7f, 10, Quaternion.identity, false);
            b.Cylinder(steel, sp, .2f, .015f, 20, Quaternion.identity, true);
            b.Cylinder(BraxKatalog.Leather, sp + Vector3.up * .66f, .2f, .07f, 20, Quaternion.identity, true);
            W.Siedzisko(sp + Vector3.up * .73f, Vector3.forward, "HOKER"); Siedziska++;
        }
    }

    // ================================================================ the tennis court
    static void WKort()
    {
        Out_(true);
        const float s = .3f;
        // ITF: the doubles court 23.77 x 10.97 m, the singles 8.23 m, the service lines 6.40 m from the net, lines 5 cm (the
        // baselines 10 cm), the centre mark 10 cm long; every measure to the lines' outer edges. The surface a grid of cells:
        // each cell blue court, green surround or white line - the lines cut in, nothing laid over
        float[] xs = { -KHX, -5.485f, -5.435f, -4.115f, -4.065f, -.025f, .025f, 4.065f, 4.115f, 5.435f, 5.485f, KHX };
        float[] zs = { -KHZ, -11.885f, -11.785f, -11.685f, -6.45f, -6.40f, 6.40f, 6.45f, 11.685f, 11.785f, 11.885f, KHZ };
        for (int i = 0; i + 1 < xs.Length; i++)
            for (int j = 0; j + 1 < zs.Length; j++)
            {
                float mx = Mathf.Abs((xs[i] + xs[i + 1]) * .5f), mz = Mathf.Abs((zs[j] + zs[j + 1]) * .5f);
                Material m;
                if (mx > 5.485f || mz > 11.885f) m = kortZielony;
                else if (mx > 5.435f || (mx > 4.065f && mx < 4.115f) || mz > 11.785f || (mz > 6.40f && mz < 6.45f && mx < 4.115f)
                         || (mx < .025f && (mz < 6.45f || (mz > 11.685f && mz < 11.785f)))) m = kortLinia;
                else m = kortNiebieski;
                MM(m, KCX + xs[i], KCX + xs[i + 1], -s, 0f, KCZ + zs[j], KCZ + zs[j + 1], false);
            }
        W.Kolizja(new Vector3(KCX, -s * .5f, KCZ), new Vector3(2f * KHX, s, 2f * KHZ), Quaternion.identity);
        // the net: posts 0.914 m outside the doubles sidelines, 1.07 m; the net 0.914 m in the middle, a cord grid of 10 cm, the
        // white headband on its top cable
        float px = 5.485f + .914f;
        foreach (float sx in new[] { -px, px }) b.Cylinder(stalCzarna, new Vector3(KCX + sx, 0f, KCZ), .04f, 1.07f, 12, Quaternion.identity, true);
        float Top(float x) => .914f + (1.07f - .914f) * (x / px) * (x / px);
        int nv = Mathf.RoundToInt(2f * px / .1f);
        for (int i = 1; i < nv; i++) { float x = -px + i * .1f; MM(black, KCX + x - .003f, KCX + x + .003f, .03f, Top(x) - .05f, KCZ - .003f, KCZ + .003f, false); }
        for (float yy = .08f; yy < .9f; yy += .1f) MM(black, KCX - px + .04f, KCX + px - .04f, yy - .003f, yy + .003f, KCZ - .003f, KCZ + .003f, false);
        for (int i = 0; i < 16; i++)
        {
            float xa = -px + i * 2f * px / 16f, xb = -px + (i + 1) * 2f * px / 16f;
            var a = new Vector3(KCX + xa, Top(xa) - .025f, KCZ); var c = new Vector3(KCX + xb, Top(xb) - .025f, KCZ);
            b.Box(signWhite, (a + c) * .5f, new Vector3((c - a).magnitude + .005f, .05f, .024f), Quaternion.FromToRotation(Vector3.right, c - a));   // 9 mm off the cords
        }
        W.Kolizja(new Vector3(KCX, .5f, KCZ), new Vector3(2f * px, 1f, .1f), Quaternion.identity);
        // the fence: 3 m behind the baselines, 1 m along the sides; black posts every ~3 m, a black cord mesh of 10 cm
        void Plot(bool alongX, float plane, float a0, float a1, float h)
        {
            int np = Mathf.CeilToInt((a1 - a0) / 3.05f);
            for (int i = 0; i <= np; i++)
            {
                float a = a0 + i * (a1 - a0) / np;
                var p = alongX ? new Vector3(a, 0f, plane) : new Vector3(plane, 0f, a);
                b.Cylinder(stalCzarna, p, .03f, h + .03f, 10, Quaternion.identity, true);
            }
            int nc = Mathf.RoundToInt((a1 - a0) / .1f);
            for (int i = 1; i < nc; i++)
            {
                float a = a0 + i * (a1 - a0) / nc;
                if (alongX) MM(black, a - .002f, a + .002f, .02f, h, plane - .002f, plane + .002f, false);
                else MM(black, plane - .002f, plane + .002f, .02f, h, a - .002f, a + .002f, false);
            }
            for (float yy = .1f; yy < h - .01f; yy += .1f)
            {
                if (alongX) MM(black, a0, a1, yy - .002f, yy + .002f, plane - .002f, plane + .002f, false);
                else MM(black, plane - .002f, plane + .002f, yy - .002f, yy + .002f, a0, a1, false);
            }
            if (alongX) { MM(stalCzarna, a0, a1, h - .02f, h + .02f, plane - .02f, plane + .02f, false); W.Kolizja(new Vector3((a0 + a1) * .5f, h * .5f, plane), new Vector3(a1 - a0, h, .1f), Quaternion.identity); }
            else { MM(stalCzarna, plane - .02f, plane + .02f, h - .02f, h + .02f, a0, a1, false); W.Kolizja(new Vector3(plane, h * .5f, (a0 + a1) * .5f), new Vector3(.1f, h, a1 - a0), Quaternion.identity); }
        }
        float fx0 = KCX - KHX + .05f, fx1 = KCX + KHX - .05f, fz0 = KCZ - KHZ + .05f, fz1 = KCZ + KHZ - .05f;
        Plot(true, fz0, fx0, fx1, 3f); Plot(true, fz1, fx0, fx1, 3f);
        // the sides: the east one open in its middle (the way in from the house)
        Plot(false, fx0, fz0, fz1, 1f);
        Plot(false, fx1, fz0, KCZ - 1.2f, 1f); Plot(false, fx1, KCZ + 1.2f, fz1, 1f);
        // a teak bench at the net on the west side; four lights on 6 m poles outside the side fences
        Lawka(new Vector3(KCX - 7.9f, 0f, KCZ + 2.2f), Vector3.right, 1.8f);
        foreach (float x in new[] { fx0 - .6f, fx1 + .6f }) foreach (float z in new[] { KCZ - 9f, KCZ + 9f })
        {
            float inward = x < KCX ? 1f : -1f;
            b.Cylinder(stalCzarna, new Vector3(x, 0f, z), .06f, 6f, 12, Quaternion.identity, true);
            b.Box(stalCzarna, new Vector3(x + inward * .25f, 6.0f, z), new Vector3(.5f, .14f, 1.1f), Quaternion.identity);
            W.Kolizja(new Vector3(x, 3f, z), new Vector3(.14f, 6f, .14f), Quaternion.identity);
            W.Lampa("kort | naświetlacz", new Vector3(x + inward * 1.2f, 5.7f, z), 1.2f, 18f * K, new Color(1f, .96f, .9f));
        }
    }

    // ================================================================ more palms: round the court, the dining, the stair
    static void WPalmy()
    {
        foreach (var p in new[] { new Vector2(-47f, -20f), new Vector2(-47f, -6f), new Vector2(-47f, 9f), new Vector2(-47f, 24f), new Vector2(-26.5f, -20f),
                                  new Vector2(-26.5f, 24f), new Vector2(-37f, -20.8f), new Vector2(-37f, 24.6f), new Vector2(24.8f, -6.5f) })
            Palma(p.x, p.y);
    }
}
