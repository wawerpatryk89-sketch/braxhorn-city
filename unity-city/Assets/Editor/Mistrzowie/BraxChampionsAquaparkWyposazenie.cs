using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN (BraxChampionsAquapark): the rock waterfall, the kids' zone, the furniture of the deck and the fence.
public static partial class BraxChampionsAquapark
{
    // ================================================================ ROCK WATERFALL (the picture: rocks with plants between the
    // red tube and the racers, two falls into the pool)
    static float Hash(int a, int b) { unchecked { uint h = (uint)(a * 374761393 + b * 668265263); h = (h ^ (h >> 13)) * 1274126177u; return (h ^ (h >> 16)) / 4294967295f; } }
    static float Noise3(Vector3 p)
    {
        return Mathf.Sin(p.x * 1.7f + p.z * .9f) * .5f + Mathf.Sin(p.y * 2.3f + p.x * 1.1f) * .3f + Mathf.Sin(p.z * 2.9f - p.y * 1.4f) * .2f;
    }
    // a boulder: a squashed sphere with lumps, its base sunk into what it stands on
    static void Boulder(BraxChampionsMeshBatch b, Vector3 c, Vector3 size, int seed)
    {
        Vector3 Pt(float u, float v)
        {
            float lon = u * Mathf.PI * 2f, lat = (v - .5f) * Mathf.PI;
            var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
            float k = 1f + .16f * Noise3(d * 2.2f + Vector3.one * seed) + .06f * Noise3(d * 6.1f - Vector3.one * seed);
            if (d.y < -.2f) k *= .92f;
            return c + Vector3.Scale(d, size * .5f) * k;
        }
        b.Surface(rock, Pt, 30, 16);
    }

    static void Waterfall()
    {
        // the rockery on the north shore between the red tube (x 10) and the racers (x 24..28): it stands on the deck
        // behind the coping and leans over the water, three tiers, 3.6 m high
        var tiers = new (float x, float z, float y, Vector3 s)[]
        {
            (15.5f, 38.6f, 0f, new Vector3(4.2f, 2.6f, 3.6f)), (19.8f, 39.2f, 0f, new Vector3(3.8f, 2.2f, 3.4f)), (17.6f, 41.4f, .9f, new Vector3(4.6f, 2.9f, 3.8f)),
            (14.0f, 41.8f, 0f, new Vector3(3.0f, 2.0f, 3.0f)), (21.6f, 42.0f, 0f, new Vector3(3.2f, 2.4f, 3.2f)), (17.9f, 39.0f, 2.1f, new Vector3(3.2f, 1.8f, 2.8f)),
            (16.4f, 36.4f, -.6f, new Vector3(2.6f, 1.6f, 2.2f)), (19.6f, 36.6f, -.7f, new Vector3(2.4f, 1.5f, 2.0f)), (18.0f, 40.6f, 2.9f, new Vector3(2.6f, 1.6f, 2.4f)),
        };
        int i = 0;
        foreach (var t in tiers)
        {
            var c = V(t.x, DY + t.y + t.s.y * .38f, t.z);
            Boulder(walk, c, t.s, 11 + i++);
        }
        // the falls: a wide sheet off the top lip, a second off the middle tier, into the lagoon (the region's fountain water,
        // animated every frame by BraxChampionsFontanna), a pool of foam where they land
        var lip1 = V(17.9f, DY + 3.55f, 37.6f); var lip2 = V(17.0f, DY + 2.05f, 36.0f);
        var land1 = V(17.9f, lagoon.water, 33.2f); var land2 = V(17.0f, lagoon.water, 32.8f);
        var fall = BraxChampionsMeshBatch.FountainWater(root, "wodospad (woda)", Vector3.zero, K,
            new[] { lip1, lip2, V(19.4f, DY + 2.4f, 36.6f) }, new[] { land1, land2, V(19.6f, lagoon.water, 33.0f) },
            new[] { .12f, .10f, .08f }, new[] { 1.9f, 1.5f, .9f }, new[] { 16, 12, 8 }, new[] { true, true, true },
            new[] { new Vector3(17.6f + CX, lagoon.water, 32.9f + CZ) }, new[] { 2.4f }, fountainWater, 0f);
        // ferns and flowers in the rock's pockets
        foreach (var (x, z, y) in new[] { (14.6f, 40.4f, 1.9f), (21.0f, 41.0f, 2.1f), (18.6f, 42.6f, 3.6f), (16.2f, 42.4f, 2.8f) })
            BraxChampionsPromenadeFurniture.Flower(models, "flower_gazania", V(x, DY + y, z), 1.3f, x * 37f);
    }

    // ================================================================ KIDS' ZONE
    static void Kids()
    {
        // the owner's two playground slides (Tripo, pack basen_01) standing in the splash pool, chutes into the water
        Owner("zjezdzalnia_dzieci_palma", V(-83.0f, kidsPool.floor, 40.5f), 60f, true);
        Owner("zjezdzalnia_dzieci_wieza", V(-71.0f, kidsPool.floor, 48.5f), 215f, true);
        // a mushroom fountain in the middle: a fibreglass cap on a stem, water falling all round its rim
        var mc = V(-76.5f, kidsPool.floor, 43.0f);
        m.Cylinder(white, mc, .16f, 2.15f, 16, Quaternion.identity, false);
        var cap = new List<Vector2>(); for (int k = 0; k <= 10; k++) { float t = k / 10f; cap.Add(new Vector2(Mathf.Lerp(1.65f, .05f, t * t), .25f * Mathf.Sin(t * Mathf.PI * .5f) + (t > .95f ? .02f : 0f))); }
        m.Lathe(canvasRed, mc + Vector3.up * 2.15f, cap.ToArray(), 32, Quaternion.identity, true, false);
        Col(mc.x - .2f, mc.x + .2f, kidsPool.floor, kidsPool.floor + 2.4f, mc.z - .2f, mc.z + .2f, "grzybek");
        var a = new List<Vector3>(); var bb = new List<Vector3>();
        for (int k = 0; k < 12; k++) { float ang = k * 30f * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)); a.Add(mc + Vector3.up * 2.12f + d * 1.62f); bb.Add(new Vector3(mc.x, kidsPool.water, mc.z) + d * 1.95f); }
        BraxChampionsMeshBatch.FountainWater(root, "grzybek (woda)", Vector3.zero, K, a.ToArray(), bb.ToArray(), Enumerable.Repeat(.04f, 12).ToArray(),
            Enumerable.Repeat(.5f, 12).ToArray(), Enumerable.Repeat(5, 12).ToArray(), Enumerable.Repeat(true, 12).ToArray(),
            new[] { new Vector3(mc.x, kidsPool.water, mc.z) }, new[] { 2.2f }, fountainWater, 0f);
        // the SPLASH PAD east of the pool: 9 ground jets arching from flush stainless nozzles, three shade sails over the zone
        var jets = new List<Vector3>(); var lands = new List<Vector3>();
        for (int k = 0; k < 9; k++)
        {
            float ja = k * 40f * Mathf.Deg2Rad; var c = V(-60.8f, DY, 50f); var d = new Vector3(Mathf.Cos(ja), 0, Mathf.Sin(ja));
            var p0 = c + d * .6f; jets.Add(p0); lands.Add(c + d * 2.4f);
            m.Cylinder(steel, p0, .07f, .01f, 12);
        }
        BraxChampionsMeshBatch.FountainWater(root, "plac wodny dzieci (woda)", Vector3.zero, K, jets.ToArray(), lands.ToArray(), Enumerable.Repeat(1.3f, 9).ToArray(),
            Enumerable.Repeat(.05f, 9).ToArray(), Enumerable.Repeat(3, 9).ToArray(), Enumerable.Repeat(true, 9).ToArray(), new Vector3[0], new float[0], fountainWater, 0f);
        Fill(m, rubberYellow, Ccw(Circle(P(-60.8f, 50f), 2.8f, 36)), null, DY + .006f);
        foreach (var (sa, sb, sc, col) in new[] { ((-94f, 30f), (-80f, 26f), (-90f, 42f), canvas), ((-70f, 58f), (-58f, 62f), (-63.5f, 46.5f), canvasRed), ((-94f, 62f), (-82f, 63f), (-92f, 50f), canvas) })
        {
            var pa = V(sa.Item1, DY + 3.6f, sa.Item2); var pb = V(sb.Item1, DY + 3.1f, sb.Item2); var pc = V(sc.Item1, DY + 3.9f, sc.Item2);
            T(m, col, pa, pb, pc, Vector3.up); T(m, col, pa, pc, pb, Vector3.down);
            foreach (var q in new[] { pa, pb, pc })
            {
                m.Cylinder(steel, new Vector3(q.x, DY, q.z), .06f, q.y - DY + .25f, 12);
                Col(q.x - .08f, q.x + .08f, DY, q.y, q.z - .08f, q.z + .08f, "żagiel słup");
            }
            m.Tube(steel, pa, pb, .008f, 6); m.Tube(steel, pb, pc, .008f, 6); m.Tube(steel, pc, pa, .008f, 6);
        }
        // yellow rubber islands, a low white fence with a gate round the zone, parents' benches and parasols
        foreach (var (x, z, r) in new[] { (-93f, 61f, 2.2f), (-60.5f, 27f, 2.0f) })
        {
            var isl = Circle(P(x, z), r, 28);
            Fill(m, rubberYellow, Ccw(isl), null, DY + .006f);
        }
        var fence = Offset(kidsArea, -.25f);
        float run = 0f; Vector2 last = fence[0];
        for (int i = 0; i < fence.Count; i++)
        {
            var p0 = fence[i]; var p1 = fence[(i + 1) % fence.Count];
            var mid = (p0 + p1) * .5f;
            if (Mathf.Abs(mid.x - (CX - 77f)) < 2.2f && mid.y < CZ + 30f) continue;          // the gate to the deck (south side)
            m.Box(white, new Vector3(mid.x, DY + .95f, mid.y), new Vector3((p1 - p0).magnitude + .01f, .06f, .06f), Quaternion.LookRotation(new Vector3(p1.x - p0.x, 0, p1.y - p0.y)) * Quaternion.Euler(0, 90, 0));
            m.Box(glass, new Vector3(mid.x, DY + .52f, mid.y), new Vector3((p1 - p0).magnitude, .78f, .012f), Quaternion.LookRotation(new Vector3(p1.x - p0.x, 0, p1.y - p0.y)) * Quaternion.Euler(0, 90, 0));
            ColRot(new Vector3(mid.x, DY + .5f, mid.y), new Vector3((p1 - p0).magnitude, 1.0f, .08f), Quaternion.LookRotation(new Vector3(p1.x - p0.x, 0, p1.y - p0.y)) * Quaternion.Euler(0, 90, 0), "płotek strefy dzieci");
            run += (p1 - p0).magnitude;
            if (run > 2.0f) { run = 0f; m.Cylinder(white, new Vector3(p0.x, DY, p0.y), .035f, .98f, 10); }
        }
        foreach (var (x, z) in new[] { (-95.5f, 40f), (-95.5f, 50f) })
            BraxChampionsPromenadeFurniture.BenchFramed(m, cols, CX + x, CZ + z, new Vector2(1, 0), "rodzice " + z);
        foreach (var (x, z) in new[] { (-60f, 40f), (-60f, 56f) }) { Parasol(V(x, DY, z), 1); Lounger(V(x + 1.1f, DY, z - .62f), 90f); Lounger(V(x + 1.1f, DY, z + .62f), 90f); }
        Owner("ratownik_krzeslo", V(-61.0f, DY, 33.5f), 250f, false);
    }

    // an owner model from the basen_01 pack (real metres): base at `at` (world nominal), yaw; a box collider round it
    static GameObject Owner(string plik, Vector3 at, float yaw, bool collide)
    {
        var src = BraxMiastoAssety.Model(Paczka, plik);
        if (!src) { OwnerMissing++; return null; }
        var o = BraxMiastoAssety.Postaw(models, src, "AQUAPARK | " + plik, at * K, yaw, 1f, true, true);
        if (!o) { OwnerMissing++; return null; }
        OwnerModels++;
        if (collide)
        {
            var rs = o.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) return o;
            var bd = rs[0].bounds; foreach (var r in rs) bd.Encapsulate(r.bounds);
            var g = new GameObject(plik + " | kolizja"); g.transform.SetParent(cols, false); g.transform.position = bd.center;
            g.AddComponent<BoxCollider>().size = bd.size * .92f; Colliders++;
        }
        return o;
    }

    // ================================================================ FURNITURE
    // a pool lounger (2.0 x 0.72 m, seat .34 m): graphite aluminium frame, silver teak slats, white cushion with a 10 cm
    // mattress, adjustable back at 38 deg, a red BRAXHORN towel folded on every other one; you lie on it (BraxSeat lie)
    static void Lounger(Vector3 o, float yaw)
    {
        const float w = .72f, L = 2.0f, h = .34f, lie = L * .62f, back = L - lie;
        var r = Quaternion.Euler(0, yaw, 0);
        Vector3 R(float x, float y, float z) => o + r * new Vector3(x, y, z);
        foreach (float side in new[] { -.47f, .47f })
        {
            float sx = side * w;
            m.Box(graphite, R(sx, h, -L * .5f + lie * .5f), new Vector3(.04f, .05f, lie), r);
            foreach (float zz in new[] { -L * .46f, -L * .5f + lie - .06f }) m.Box(graphite, R(sx, h * .5f, zz), new Vector3(.04f, h, .04f), r);
        }
        m.Cylinder(graphite, R(0, .06f, -L * .5f + .05f), .05f, .02f, 10);
        for (int i = 0; i < 9; i++) { float z = -L * .5f + .05f + i * (lie - .1f) / 8.5f; m.Box(teak, R(0, h + .03f, z), new Vector3(w * .9f, .022f, .10f), r); }
        var br = r * Quaternion.Euler(-38f, 0, 0);
        var bc = R(0, h, -L * .5f + lie) + br * new Vector3(0, 0, back * .5f);
        m.Box(teak, bc + br * new Vector3(0, .03f, 0), new Vector3(w * .9f, .022f, back * .96f), br);
        foreach (float side in new[] { -.47f, .47f }) m.Box(graphite, bc + br * new Vector3(side * w, 0, 0), new Vector3(.035f, .045f, back), br);
        m.RoundedBox(cushion, R(0, h + .095f, -L * .5f + lie * .5f + .02f), new Vector3(w * .86f, .10f, lie * .96f), .04f, r, 2);
        m.RoundedBox(cushion, bc + br * new Vector3(0, .095f, 0), new Vector3(w * .86f, .10f, back * .92f), .04f, br, 2);
        if (Loungers % 2 == 0) m.RoundedBox(towel, R(0, h + .16f, -L * .5f + .26f), new Vector3(w * .66f, .035f, .32f), .012f, r, 1);
        ColRot(R(0, h * .8f, 0), new Vector3(w, h * 1.6f, L * .95f), r, "leżak " + Loungers);
        var g = new GameObject("leżak " + (Loungers + 1)); g.transform.SetParent(seats, false);
        g.transform.localPosition = R(0, h + .15f, 0) * K; g.transform.localRotation = r * Quaternion.Euler(0, 180f, 0);
        var s = g.AddComponent<BraxSeat>(); s.lie = true; s.label = "LEŻAK"; s.surfaceY = (o.y + h + .15f) * K;
        Loungers++; Seats++;
    }

    // round parasol, 3.0 m canopy, 12 ribs, white with a red valance, graphite pole in a stainless foot
    static void Parasol(Vector3 b, int n)
    {
        m.Cylinder(graphite, b, .28f, .08f, 20, Quaternion.identity, true, false, .24f);
        m.Cylinder(steel, b, .026f, 2.55f, 10);
        const int ribs = 12; const float r = 1.5f, eave = 2.22f, top = 2.68f;
        var apex = b + Vector3.up * top;
        for (int j = 0; j < ribs; j++)
        {
            float a0 = 2f * Mathf.PI * j / ribs, a1 = 2f * Mathf.PI * (j + 1) / ribs;
            var p0 = b + new Vector3(r * Mathf.Cos(a0), eave, r * Mathf.Sin(a0)); var p1 = b + new Vector3(r * Mathf.Cos(a1), eave, r * Mathf.Sin(a1));
            var mid = (apex + (p0 + p1) * .5f) * .5f + Vector3.up * .03f;
            m.Triangle(canvas, apex, p1, p0); m.Triangle(canvas, apex, p0, p1);
            m.Quad(canvasRed, p0, p0 - Vector3.up * .12f, p1 - Vector3.up * .12f, p1); m.Quad(canvasRed, p1, p1 - Vector3.up * .12f, p0 - Vector3.up * .12f, p0);
            m.Tube(steel, apex - Vector3.up * .05f, p0 - Vector3.up * .02f, .008f, 6);
        }
        m.Cylinder(steel, apex - Vector3.up * .02f, .05f, .10f, 10);
        Col(b.x - .07f, b.x + .07f, b.y, b.y + 2.4f, b.z - .07f, b.z + .07f, "parasol");
        Parasols++;
    }

    static void Furniture()
    {
        // the south teak terrace: four rows of loungers facing the lagoon, a parasol between every pair
        for (int row = 0; row < 3; row++)
            for (float x = -26f; x <= 26.1f; x += 4.0f)
            {
                float z = -47.5f + row * 5.0f;
                Lounger(V(x - .55f, DY, z), 180f); Lounger(V(x + .55f, DY, z), 180f);
                Parasol(V(x, DY, z - 1.35f), 0);
            }
        // the east terrace on the lawn's edge and along the lagoon's east shore
        for (float z = -10f; z <= 24.1f; z += 3.8f) { Lounger(V(52.2f, DY, z), 90f); Lounger(V(52.2f, DY, z + 1.6f), 90f); if (((int)z) % 2 == 0) Parasol(V(54.6f, DY, z + .8f), 0); }
        // the west shore (by the kids' zone): loungers for the parents
        for (float z = -14f; z <= 18f; z += 4.2f) { Lounger(V(-47.5f, DY, z), -90f); Lounger(V(-47.5f, DY, z + 1.6f), -90f); Parasol(V(-50.2f, DY, z + .8f), 0); }
        // round the two jacuzzi on the teak: loungers looking at the lagoon
        foreach (var (x, z) in new[] { (61f, 12.6f), (61f, 14.2f), (90.5f, -9f), (90.5f, -7.4f), (90.5f, 17f), (90.5f, 18.6f) }) Lounger(V(x, DY, z), 90f);
        foreach (var (x, z) in new[] { (61.6f, 16.6f), (92.4f, -5.2f), (92.4f, 20.6f) }) Parasol(V(x, DY, z), 0);
        // lifeguard chairs (the owner's model) on both shores
        Owner("ratownik_krzeslo", V(-30f, DY, 21.6f), 150f, false);
        Owner("ratownik_krzeslo", V(47.2f, DY, 18f), 250f, false);
        // pool showers at the stairs and the kids' gate: stainless post, rain head, push button, a grate in the deck
        foreach (var (x, z) in new[] { (-6f, -27.2f), (6f, -27.2f), (-44.5f, -5f), (-77f, 18.5f), (60f, 25f), (36f, 46f) }) Shower(V(x, DY, z));
        // lamps round the deck (the promenade's lamp)
        foreach (var (x, z) in new[] { (-55f, -58f), (-30f, -58f), (0f, -58f), (30f, -58f), (55f, -58f), (-52f, -30f), (-52f, 0f), (-52f, 20f), (57f, -30f),
                     (57f, 0f), (-40f, 46f), (-10f, 50f), (10f, 52f), (52f, 47f), (-20f, 70f), (20f, 70f), (60f, 74f), (-60f, 72f), (-96f, -70f), (96f, -70f) })
        { BraxChampionsPromenadeFurniture.Lamp(m, cols, CX + x, CZ + z); Lamps++; }
        foreach (float x in new[] { -96f, -60f, 60f, 96f }) { BraxChampionsPromenadeFurniture.Lamp(m, cols, CX + x, CZ - 108f); Lamps++; }
        // benches on the forecourt facing the street, bins
        foreach (float x in new[] { -84f, -72f, 72f, 84f }) BraxChampionsPromenadeFurniture.BenchFramed(m, cols, CX + x, CZ - 99.5f, new Vector2(0, -1), "aquapark " + x);
        PoolBar();
    }

    static void Shower(Vector3 b)
    {
        m.Cylinder(steel, b + Vector3.up * .002f, .32f, .01f, 20);                                   // round stainless grate
        m.Cylinder(steel, b, .045f, 2.35f, 12);
        m.Tube(steel, b + Vector3.up * 2.35f, b + Vector3.up * 2.35f + Vector3.forward * .45f, .03f, 10);
        m.Cylinder(steel, b + Vector3.up * 2.28f + Vector3.forward * .45f, .14f, .03f, 16);
        m.Box(steel, b + Vector3.up * 1.1f + Vector3.back * .05f, new Vector3(.08f, .08f, .04f));
        Col(b.x - .06f, b.x + .06f, b.y, b.y + 2.3f, b.z - .06f, b.z + .06f, "prysznic");
        ShowerWater(b + Vector3.up * 2.26f + Vector3.forward * .45f, b.y, "prysznic basenowy");
    }

    // the shower runs (the region's fountain water in its sensor mode: only while you stand under / next to it - the rule
    // "wchodzę pod prysznic i ma lecieć woda"): a rain of streaks from the head onto the floor, splashing
    public static void ShowerWater(Vector3 head, float floorY, string name)
    {
        const int k = 10; var a = new Vector3[k]; var b = new Vector3[k];
        for (int i = 0; i < k; i++)
        {
            float ang = i * 2f * Mathf.PI / k; var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
            a[i] = head + d * .06f - Vector3.up * .03f; b[i] = new Vector3(head.x, floorY + .01f, head.z) + d * .24f;
        }
        BraxChampionsMeshBatch.FountainWater(root, name + " (woda, czujnik)", Vector3.zero, K, a, b, Enumerable.Repeat(.01f, k).ToArray(),
            Enumerable.Repeat(.016f, k).ToArray(), Enumerable.Repeat(3, k).ToArray(), Enumerable.Repeat(true, k).ToArray(), new Vector3[0], new float[0], fountainWater, 5.5f);
    }

    // the strolling routes: the lagoon's outline 3.2 m / 5.0 m out from the coping, its south half from the west shore round to
    // the east one (the north is the tower, the slides and the rocks), each walked both ways
    static List<Vector3[]> WalkRoutes()
    {
        var o = new List<Vector3[]>();
        foreach (float off in new[] { 3.2f, 5.0f })
        {
            var ring = Offset(lagoon.cop, off);
            int Near(Vector2 q) { int bi = 0; float bd = float.MaxValue; for (int i = 0; i < ring.Count; i++) { float d = (ring[i] - q).sqrMagnitude; if (d < bd) { bd = d; bi = i; } } return bi; }
            int a = Near(P(-44f, 16f)), b = Near(P(52f, 16f));
            var pts = new List<Vector3>(); float run = 99f; Vector2 last = ring[a];
            for (int i = a; ; i = (i + 1) % ring.Count)
            {
                run += (ring[i] - last).magnitude; last = ring[i];
                if (run >= 7.5f || i == b) { pts.Add(new Vector3(ring[i].x, 0f, ring[i].y)); run = 0f; }
                if (i == b) break;
            }
            o.Add(pts.ToArray()); var rev = pts.ToArray(); System.Array.Reverse(rev); o.Add(rev);
        }
        // the lazy river's east bank (between the river's sign and the west loungers), north to south and back
        var bankWalk = new List<Vector3>();
        for (float z = 4f; z >= -50f; z -= 7.5f) bankWalk.Add(V(-59f, 0f, z));
        o.Add(bankWalk.ToArray()); var back = bankWalk.ToArray(); System.Array.Reverse(back); o.Add(back);
        return o;
    }

    // round pool bar north-west of the lagoon: white quartz counter on a teak-clad drum, a ring canopy on six columns with a
    // warm LED ring, eight stools you sit on, the BRAXHORN POOL BAR name on the fascia
    static void PoolBar()
    {
        var c = V(-22f, DY, 46f);
        float r0 = 3.0f, r1 = 3.55f;
        var inner = Circle(new Vector2(c.x, c.z), r0, 48); var outer = Circle(new Vector2(c.x, c.z), r1, 48);
        Skin(m, teak, Ccw(outer), DY, DY + 1.05f, false); Skin(m, teak, Ccw(inner), DY, DY + 1.05f, true);
        Band(m, white, inner, Circle(new Vector2(c.x, c.z), r1 + .12f, 48), DY + 1.10f, DY + 1.10f);
        Skin(m, white, Ccw(Circle(new Vector2(c.x, c.z), r1 + .12f, 48)), DY + 1.05f, DY + 1.10f, false);
        Skin(m, led, Ccw(Circle(new Vector2(c.x, c.z), r1 + .005f, 48)), DY + .12f, DY + .15f, false);
        // back bar: shelves of bottles round a centre column
        m.Cylinder(black, c, 1.1f, 2.2f, 24); m.Cylinder(white, c + Vector3.up * 1.05f, 1.25f, .04f, 24);
        for (int k = 0; k < 6; k++) { float a = k * 60f * Mathf.Deg2Rad; var p = c + new Vector3(Mathf.Cos(a) * 4.6f, 0, Mathf.Sin(a) * 4.6f); m.Cylinder(black, p, .09f, 3.3f, 12); Col(p.x - .1f, p.x + .1f, DY, DY + 3.3f, p.z - .1f, p.z + .1f, "bar słup"); }
        var roofIn = Circle(new Vector2(c.x, c.z), .3f, 48); var roofOut = Circle(new Vector2(c.x, c.z), 5.4f, 48);
        Band(m, white, roofIn, roofOut, DY + 3.6f, DY + 3.3f); Band(m, white, roofOut, roofIn, DY + 3.25f, DY + 3.55f);
        Skin(m, black, Ccw(roofOut), DY + 3.25f, DY + 3.33f, false);
        Skin(m, led, Ccw(Circle(new Vector2(c.x, c.z), 5.405f, 48)), DY + 3.26f, DY + 3.29f, false);
        B(black, c.x - 2.9f, c.x + 2.9f, DY + 3.30f, DY + 3.50f, c.z - 4.75f, c.z - 4.55f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "BRAXHORN POOL BAR", c + new Vector3(0, 3.50f, -4.76f), 180f, .32f, .06f, .12f, 5.6f);
        for (int k = 0; k < 10; k++)
        {
            float a = (k * 36f + 18f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = c + d * 4.15f;
            m.Cylinder(steel, p, .2f, .02f, 16); m.Cylinder(steel, p, .035f, .72f, 10); m.Cylinder(white, p + Vector3.up * .72f, .2f, .06f, 20);
            var g = new GameObject("hoker bar " + (k + 1)); g.transform.SetParent(seats, false); g.transform.localPosition = (p + Vector3.up * .78f) * K;
            g.transform.localRotation = Quaternion.LookRotation(-d); var s = g.AddComponent<BraxSeat>(); s.label = "HOKER"; s.surfaceY = (DY + .78f) * K; Seats++;
        }
        Col(c.x - r1, c.x + r1, DY, DY + 1.1f, c.z - r1, c.z + r1, "bar lada");
    }

    // ================================================================ FENCE (glass on stainless posts, 1.4 m, round the lot except
    // the pavilion's front and the forecourt; a gate from the tram stop at the back)
    static void Fence()
    {
        float y0 = DY, top = DY + 1.40f;
        void Run(Vector2 a, Vector2 b)
        {
            float len = (b - a).magnitude; int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.0f)); var d = (b - a) / n;
            var rot = Quaternion.LookRotation(new Vector3(d.x, 0, d.y)) * Quaternion.Euler(0, 90, 0);
            for (int i = 0; i < n; i++)
            {
                var p0 = a + d * i; var mid = p0 + d * .5f;
                m.Box(glass, new Vector3(mid.x, (y0 + .10f + top - .06f) * .5f, mid.y), new Vector3(d.magnitude - .08f, top - .06f - y0 - .10f, .012f), rot);
                m.Cylinder(steel, new Vector3(p0.x, y0, p0.y), .03f, top - y0, 10);
            }
            m.Box(steel, new Vector3((a.x + b.x) * .5f, top, (a.y + b.y) * .5f), new Vector3(len, .04f, .06f), rot);
            m.Cylinder(steel, new Vector3(b.x, y0, b.y), .03f, top - y0, 10);
            ColRot(new Vector3((a.x + b.x) * .5f, (y0 + top) * .5f, (a.y + b.y) * .5f), new Vector3(len, top - y0, .1f), rot, "ogrodzenie");
        }
        float e = .6f;
        Run(P(-103 + e, -64f), P(-103 + e, 113 - e));
        Run(P(103 - e, -64f), P(103 - e, 113 - e));
        Run(P(-103 + e, 113 - e), P(-74f, 113 - e));                    // the tram gate x -74..-64 (local)
        Run(P(-64f, 113 - e), P(103 - e, 113 - e));
        Run(P(-103 + e, -64f), P(-58f, -64f));                            // the pavilion closes the lot's south
        Run(P(58f, -64f), P(103 - e, -64f));
        // the tram gate: two stainless pillars and the sign
        foreach (float x in new[] { -74f, -64f }) { var p = V(x, DY, 113 - e); m.Box(black, p + Vector3.up * 1.4f, new Vector3(.5f, 2.8f, .5f)); Col(p.x - .25f, p.x + .25f, DY, DY + 2.8f, p.z - .25f, p.z + .25f, "brama słup"); }
        B(black, CX - 74.25f, CX - 63.75f, DY + 2.80f, DY + 3.30f, CZ + 113 - e - .25f, CZ + 113 - e + .25f);
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "AQUAPARK BRAXHORN", V(-69f, DY + 2.86f, 113 - e + .26f), 0f, .32f, .06f, .12f, 9.4f);
    }
}
