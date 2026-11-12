using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN (BraxChampionsAquapark): the lot, the pools and everything on the deck. Plan (local metres from the lot
// centre CX/CZ, +x east, +z north; the street Z70 and the entrance pavilion south, the tram stop on Z330 behind):
//   forecourt z -113..-96 | pavilion x -58..58, z -96..-64 | deck with the south teak terrace | LAGOON x -38..46, z -20..35
//   | slide tower x 23..33, z 53..63, red tube spiral round (10, 62), yellow round (43, 60), racers into the north bay
//   | KIDS' ZONE x -98..-56, z 22..66 west | LENIWA RZEKA x -97..-64, z -58..8 round a palm island (south-west)
//   | JACUZZI teak deck x 58..92, z -12..22 east | lawns with palms round the edge.
public static partial class BraxChampionsAquapark
{
    sealed class Pool { public string id; public List<Vector2> edge, cop; public float water, floor; public readonly List<List<Vector2>> islands = new List<List<Vector2>>(); }
    static Pool lagoon, kidsPool; static readonly List<Pool> spas = new List<Pool>();
    static List<Vector2> deckOuter, kidsArea, teakSouth, teakEast, plazaRect;
    static readonly List<List<Vector2>> lawns = new List<List<Vector2>>();
    static readonly List<(Vector2 c, float r)> palmPlanters = new List<(Vector2, float)>();

    // tower and slide anchors (local) shared with BraxChampionsAquaparkZjezdzalnie
    public const float TowerX0 = 23f, TowerX1 = 33f, TowerZ0 = 53f, TowerZ1 = 63f;

    static List<Vector2> W(IEnumerable<Vector2> local) => local.Select(q => new Vector2(CX + q.x, CZ + q.y)).ToList();

    static void Layout()
    {
        spas.Clear(); lawns.Clear(); palmPlanters.Clear();
        // the lagoon: one freeform shore, the north bay in front of the tower takes all the slides
        var lag = Blob(new[]{new Vector2(-30,-16),new Vector2(-10,-20),new Vector2(12,-19),new Vector2(32,-13),new Vector2(43,-2),new Vector2(46,12),
            new Vector2(43,26),new Vector2(35,33),new Vector2(24,35),new Vector2(12,34),new Vector2(2,31),new Vector2(-10,27),new Vector2(-22,24),
            new Vector2(-33,16),new Vector2(-38,2)}, .8f);
        lagoon = new Pool { id = "LAGUNA", edge = W(lag), water = DY - .12f, floor = DY - 1.30f };
        lagoon.cop = Offset(lagoon.edge, .45f);
        // an island in the lagoon with two palms (its shore wall in the pool's tiles, white marble coping, lawn on top)
        lagoon.islands.Add(W(Blob(new[]{new Vector2(2,2),new Vector2(9,0),new Vector2(13,5),new Vector2(10,11),new Vector2(3,10)}, .5f)));
        kidsArea = W(RoundRect(-98f, -56f, 22f, 66f, 6f));
        kidsPool = new Pool { id = "BRODZIK", edge = W(Blob(new[]{new Vector2(-87,37),new Vector2(-77,34.5f),new Vector2(-67.5f,37.5f),new Vector2(-65.5f,45),
            new Vector2(-69.5f,51.5f),new Vector2(-79,53.5f),new Vector2(-87.5f,48)}, .5f)), water = DY - .07f, floor = DY - .38f };
        kidsPool.cop = Offset(kidsPool.edge, .40f);
        teakSouth = W(RoundRect(-30f, 30f, -52f, -32f, 2.5f));
        teakEast = W(RoundRect(58f, 94f, -14f, 22f, 3f));
        foreach (var c in new[] { new Vector2(68f, 0f), new Vector2(82f, 10f) })
        {
            var e = Circle(new Vector2(CX + c.x, CZ + c.y), 2.05f, 48);
            var p = new Pool { id = "JACUZZI " + (spas.Count + 1), edge = Ccw(e), water = DY - .10f, floor = DY - .95f };
            p.cop = Offset(p.edge, .35f); spas.Add(p);
        }
        // lawns (palms, flowers): west strip, east strip, the north band split by the path to the tram gate, islands
        lawns.Add(W(RoundRect(62f, 99f, -60f, -22f, 5f)));
        lawns.Add(W(RoundRect(-99f, -75f, 76f, 109f, 5f)));
        lawns.Add(W(RoundRect(-63f, 99f, 80f, 109f, 5f)));
        lawns.Add(W(RoundRect(56f, 99f, 30f, 70f, 5f)));
        // the west: LENIWA RZEKA round a palm island (BraxChampionsAquaparkRzeka) where the big west lawn was
        RiverLayout();
        plazaRect = new List<Vector2> { P(-103, -113), P(103, -113), P(103, -96), P(-103, -96) };
        // the deck: the lot without the forecourt strip and the pavilion's footprint (a notch, never a touching hole)
        deckOuter = Ccw(new[] { P(-103, -96), P(-58, -96), P(-58, -64), P(58, -64), P(58, -96), P(103, -96), P(103, 113), P(-103, 113) });
        // palms in round granite planters on the deck (like the picture's beds): round the lagoon and the tower
        foreach (var (x, z) in new[] { (-44f, 30f), (-46f, -18f), (-20f, 38f), (52f, 30f), (52f, -16f), (-40f, -40f), (40f, -40f), (-4f, 44f), (50f, 76f), (-6f, 62f), (16f, 74f), (36f, 72f) })
            palmPlanters.Add((P(x, z), 1.35f));
    }

    // ================================================================ POOLS
    static void Pools3()
    {
        MakePool(lagoon, poolTile, poolFloor, water, true, true);
        Steps(lagoon, P(0f, -19.6f), 6.0f, 7);
        Steps(lagoon, P(-37.6f, 2f), 4.0f, 7);
        foreach (var l in new[] { P(-24f, -17.7f), P(24f, -16.5f), P(44.6f, 8f), P(-35f, 12f), P(-8f, 27.3f) }) Ladder(lagoon, l);
        MakePool(kidsPool, poolTile, poolFloor, waterKids, false, false);
        Steps(kidsPool, P(-77f, 34.6f), 3.0f, 2);
        foreach (var s in spas)
        {
            MakePool(s, poolTile, poolFloor, waterSpa, false, false);
            JacuzziBench(s);
        }
        Pools = 2 + spas.Count;
        RiverPool();
    }

    static void MakePool(Pool p, Material wall, Material floorMat, Material wat, bool lights, bool swim)
    {
        // marble coping flush with the deck, its rounded nose into the water, a navy mosaic waterline, white-aqua tiles
        Band(walk, coping, p.edge, p.cop, DY, DY);
        Skin(walk, coping, p.edge, DY - .05f, DY, true);
        Skin(walk, waterline, p.edge, Mathf.Max(p.floor, p.water - .18f), DY - .05f, true);
        if (p.water - .18f > p.floor) Skin(walk, wall, p.edge, p.floor, p.water - .18f, true);
        Fill(walk, floorMat, p.edge, p.islands, p.floor);
        // the water (no collider)
        Fill(m, wat, p.edge, p.islands, p.water);
        foreach (var isl in p.islands)
        {
            var top = Offset(isl, -.45f); var grass = Offset(isl, -.60f);
            Skin(walk, wall, isl, p.floor, p.water - .18f, false); Skin(walk, waterline, isl, p.water - .18f, DY - .05f, false); Skin(walk, coping, isl, DY - .05f, DY, false);
            Band(walk, coping, top, isl, DY, DY);
            Band(walk, plaza, grass, top, DY + .03f, DY + .03f); Skin(m, plaza, Ccw(grass), DY + .01f, DY + .03f, true); Skin(m, plaza, Ccw(top), DY, DY + .03f, false);
            Fill(walk, lawn, Ccw(grass), null, DY + .01f);
        }
        if (lights)
        {
            float run = 0f; var e = p.edge;
            for (int i = 0; i < e.Count; i++)
            {
                var a = e[i]; var b = e[(i + 1) % e.Count]; run += (b - a).magnitude;
                if (run < 7.5f) continue; run = 0f;
                var t = (b - a).normalized; var inw = new Vector3(-t.y, 0, t.x);              // CCW: left of the edge = inside
                var c = new Vector3(a.x, p.water - .55f, a.y) + inw * .012f;
                m.Cylinder(uplight, c, .13f, .012f, 16, Quaternion.FromToRotation(Vector3.up, inw), true, false);
                m.Cylinder(steel, c - inw * .006f, .16f, .008f, 16, Quaternion.FromToRotation(Vector3.up, inw), true, false);
            }
        }
        if (swim) SwimStrips(p, 3f);
    }

    static List<Vector2> Spans(IList<Vector2> poly, float x, List<List<Vector2>> holes = null)
    {
        var zs = new List<float>();
        foreach (var pl in new[] { poly }.Concat(holes ?? new List<List<Vector2>>()))
            for (int i = 0; i < pl.Count; i++) { var a = pl[i]; var b = pl[(i + 1) % pl.Count]; if ((a.x > x) != (b.x > x)) zs.Add(a.y + (x - a.x) * (b.y - a.y) / (b.x - a.x)); }
        zs.Sort(); var o = new List<Vector2>(); for (int i = 0; i + 1 < zs.Count; i += 2) o.Add(new Vector2(zs[i], zs[i + 1])); return o;
    }

    // the shared swimming (BraxSwimmingZone): trigger boxes 3 m wide across the water, kept .35 m inside the walls, from
    // the floor to 35 cm over the surface - the deck round the pool is never inside one
    static void SwimStrips(Pool p, float strip)
    {
        var surf = Group(root, p.id + " | lustro wody (pływanie)");
        float cx = p.edge.Average(q => q.x), cz = p.edge.Average(q => q.y);
        surf.localPosition = new Vector3(cx, p.water, cz) * K;
        var zones = Group(root, p.id + " | strefy pływania");
        float minx = p.edge.Min(q => q.x), maxx = p.edge.Max(q => q.x);
        for (float x = minx; x < maxx; x += strip)
        {
            float xa = x + .35f, xb = Mathf.Min(x + strip, maxx) - .35f; if (xb - xa < .6f) continue;
            var hz = p.islands.Select(i => Offset(i, .35f)).ToList();
            var ia = Spans(p.edge, xa, hz); var ib = Spans(p.edge, xb, hz); var ic = Spans(p.edge, (xa + xb) * .5f, hz);
            foreach (var s in ic)
            {
                float z0 = s.x, z1 = s.y; bool ok = true;
                foreach (var list in new[] { ia, ib })
                {
                    bool hit = false;
                    foreach (var t in list) if (t.x < z1 && t.y > z0) { z0 = Mathf.Max(z0, t.x); z1 = Mathf.Min(z1, t.y); hit = true; break; }
                    if (!hit) ok = false;
                }
                z0 += .35f; z1 -= .35f; if (!ok || z1 - z0 < .8f) continue;
                float y0 = p.floor + .05f, y1 = p.water + .35f;
                var g = new GameObject(p.id + " | woda " + SwimZones); g.transform.SetParent(zones, false);
                g.transform.localPosition = new Vector3((xa + xb) * .5f, (y0 + y1) * .5f, (z0 + z1) * .5f) * K;
                var bc = g.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(xb - xa, y1 - y0, z1 - z0) * K;
                var zone = g.AddComponent<BraxChampionsPoolSwimVolume>(); zone.waterSurface = surf; zone.basinId = p.id;
                SwimZones++;
            }
        }
    }

    // nearest edge point and the inward direction there
    static (Vector2 at, Vector2 inw, Vector2 along) EdgeFrame(Pool p, Vector2 near)
    {
        int bi = 0; float bd = float.MaxValue;
        for (int i = 0; i < p.edge.Count; i++) { float d = (p.edge[i] - near).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
        var a = p.edge[(bi - 2 + p.edge.Count) % p.edge.Count]; var b = p.edge[(bi + 2) % p.edge.Count];
        var t = (b - a).normalized; return (p.edge[bi], new Vector2(-t.y, t.x), t);
    }

    // wide stairs into the pool: n steps from the coping down to the floor, stainless handrails on both sides
    static void Steps(Pool p, Vector2 near, float width, int n)
    {
        var (at, inw, along) = EdgeFrame(p, near);
        float h = (DY - p.floor) / (n + 1), tread = .40f;
        var rot = Quaternion.LookRotation(new Vector3(inw.x, 0, inw.y), Vector3.up);
        for (int k = 0; k < n; k++)
        {
            float top = DY - (k + 1) * h, d0 = -.10f + k * tread, d1 = -.10f + (k + 1) * tread;
            var i3 = new Vector3(inw.x, 0, inw.y) * ((d0 + d1) * .5f);
            walk.Box(poolTile, new Vector3(at.x, (p.floor + top - .04f) * .5f, at.y) + i3, new Vector3(width, top - .04f - p.floor, d1 - d0), rot);
            // marble tread nosing on every step (4 cm), a navy anti-slip line at its edge
            walk.Box(coping, new Vector3(at.x, top - .02f, at.y) + new Vector3(inw.x, 0, inw.y) * ((d0 + d1) * .5f), new Vector3(width, .04f, d1 - d0), rot);
            m.Box(waterline, new Vector3(at.x, top + .002f, at.y) + new Vector3(inw.x, 0, inw.y) * (d1 - .06f), new Vector3(width - .1f, .004f, .05f), rot);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            var side = new Vector3(along.x, 0, along.y) * (width * .5f - .25f) * s; var i3 = new Vector3(inw.x, 0, inw.y);
            var top0 = new Vector3(at.x, DY + .9f, at.y) + side - i3 * .45f; var top1 = new Vector3(at.x, DY + .9f, at.y) + side + i3 * .15f;
            var bot = new Vector3(at.x, p.floor + 1.0f, at.y) + side + i3 * (n * tread - .3f);
            m.Tube(steel, top0, top1, .022f, 10); m.Tube(steel, top1, bot, .022f, 10);
            m.Tube(steel, top0, top0 - Vector3.up * .9f, .024f, 10); m.Cylinder(steel, top0 - Vector3.up * .9f, .05f, .012f, 12);
            m.Tube(steel, bot, new Vector3(bot.x, DY - (n - 1) * h - .02f, bot.z), .022f, 10);
        }
    }

    // a stainless pool ladder over the coping: two rails bent over the edge, three treads in the water
    static void Ladder(Pool p, Vector2 near)
    {
        var (at, inw, along) = EdgeFrame(p, near);
        var i3 = new Vector3(inw.x, 0, inw.y); var a3 = new Vector3(along.x, 0, along.y);
        foreach (float s in new[] { -.28f, .28f })
        {
            var b = new Vector3(at.x, DY, at.y) + a3 * s;
            var p0 = b - i3 * .50f; var p1 = p0 + Vector3.up * .78f; var p2 = b + i3 * .02f + Vector3.up * .90f; var p3 = b + i3 * .16f + Vector3.up * .55f;
            var p4 = b + i3 * .16f + Vector3.up * (p.water - DY - .95f);
            m.Tube(steel, p0, p1, .021f, 10); m.Tube(steel, p1, p2, .021f, 10); m.Tube(steel, p2, p3, .021f, 10); m.Tube(steel, p3, p4, .021f, 10);
            m.Cylinder(steel, p0, .045f, .01f, 12);
        }
        foreach (float y in new[] { -.25f, -.55f, -.85f })
        {
            var c = new Vector3(at.x, p.water + y, at.y) + i3 * .19f;
            m.Box(steel, c, new Vector3(.56f, .03f, .09f), Quaternion.LookRotation(i3, Vector3.up));
        }
    }

    // round bench inside every jacuzzi (seat 45 cm under the water), 6 places you sit in (BraxSeat), steps down, the bubbles
    static void JacuzziBench(Pool p)
    {
        Vector2 c = new Vector2(p.edge.Average(q => q.x), p.edge.Average(q => q.y));
        float r0 = 1.45f, r1 = 2.05f, seat = p.water - .45f;
        var inner = Circle(c, r0, 48);
        var outer = Circle(c, r1 - .002f, 48);
        Band(walk, coping, inner, outer, seat, seat);
        Skin(walk, poolTile, Ccw(inner), p.floor, seat, false);
        for (int k = 0; k < 6; k++)
        {
            float a = (k + .5f) * 60f * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var at = c + d * 1.72f;
            var g = new GameObject(p.id + " | miejsce " + (k + 1)); g.transform.SetParent(seats, false);
            g.transform.localPosition = new Vector3(at.x, seat + .02f, at.y) * K;
            g.transform.localRotation = Quaternion.LookRotation(new Vector3(-d.x, 0, -d.y));
            var s = g.AddComponent<BraxSeat>(); s.label = "JACUZZI"; s.surfaceY = (seat + .02f) * K; Seats++;
            // jet nozzle in the wall behind each place
            var j = new Vector3(c.x + d.x * 2.04f, seat + .25f, c.y + d.y * 2.04f);
            m.Cylinder(steel, j, .045f, .01f, 12, Quaternion.FromToRotation(Vector3.up, new Vector3(-d.x, 0, -d.y)), true, false);
        }
        // underwater ring light
        foreach (int k in new[] { 0, 1, 2 })
        {
            float a = (k * 120f) * Mathf.Deg2Rad; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            m.Cylinder(uplight, new Vector3(c.x, p.floor + .35f, c.y) + d * (r0 - .012f), .09f, .01f, 14, Quaternion.FromToRotation(Vector3.up, -d), true, false);
        }
    }

    // ================================================================ GROUND
    static void Ground()
    {
        var holes = new List<List<Vector2>> { lagoon.cop, river.cop, kidsArea, teakSouth, teakEast };
        foreach (var l in lawns) holes.Add(Offset(l, .15f));
        foreach (var (c, r) in palmPlanters) holes.Add(Circle(c, r + .14f, 32));
        Fill(walk, deck, deckOuter, holes, DY);
        Fill(walk, plaza, plazaRect, null, DY);
        // the kids' zone: soft rubber in teal with yellow islands, the splash pool's coping inside
        Fill(walk, rubber, kidsArea, new List<List<Vector2>> { kidsPool.cop }, DY);
        Skin(m, black, kidsArea, DY - .02f, DY, false);
        // teak terraces (flush): the south one, the east one round the two jacuzzi
        Fill(walk, teak, teakSouth, null, DY);
        Fill(walk, teak, teakEast, spas.Select(s => s.cop).ToList(), DY);
        // lawns 1 cm above the deck inside a 15 cm granite edging 3 cm proud
        foreach (var l in lawns)
        {
            var e = Offset(l, .15f);
            Fill(walk, lawn, l, null, DY + .01f);
            Band(walk, plaza, l, e, DY + .03f, DY + .03f);
            Skin(m, plaza, e, DY, DY + .03f, false); Skin(m, plaza, l, DY + .01f, DY + .03f, true);
        }
        // palm planters: granite ring 45 cm, mulch inside, the owner's palm in each
        var palmSrc = new[] { BraxMiastoAssety.Model("miasto_palmy", "palma"), BraxMiastoAssety.Model("miasto_palmy", "palma2") };
        int pi = 0;
        foreach (var (c, r) in palmPlanters)
        {
            var ring = Circle(c, r, 32); var outer = Circle(c, r + .14f, 32);
            Band(m, plaza, ring, outer, DY + .45f, DY + .45f);
            Skin(m, plaza, Ccw(outer), DY, DY + .45f, false); Skin(m, plaza, Ccw(ring), DY + .32f, DY + .45f, true);
            Fill(m, bark, Ccw(ring), null, DY + .32f);
            Col(c.x - r, c.x + r, DY, DY + .45f, c.y - r, c.y + r, "donica palmy");
            Palm(palmSrc, new Vector3(c.x, DY + .32f, c.y), pi++);
        }
        // two palms on the lagoon's island
        foreach (var isl in lagoon.islands)
        {
            var c = new Vector2(isl.Average(q => q.x), isl.Average(q => q.y));
            Palm(palmSrc, new Vector3(c.x - 1.6f, DY + .01f, c.y - .6f), pi++); Palm(palmSrc, new Vector3(c.x + 1.8f, DY + .01f, c.y + 1.0f), pi++);
        }
        // palms on the lawns: a loose double row along the lot's edge
        foreach (var l in lawns)
        {
            float minx = l.Min(q => q.x) + 3f, maxx = l.Max(q => q.x) - 3f, minz = l.Min(q => q.y) + 3f, maxz = l.Max(q => q.y) - 3f;
            bool along = maxx - minx > maxz - minz;
            float len = along ? maxx - minx : maxz - minz; int n = Mathf.Max(1, Mathf.FloorToInt(len / 11f));
            for (int i = 0; i <= n; i++)
            {
                float t = n == 0 ? .5f : i / (float)n;
                var q = along ? new Vector2(Mathf.Lerp(minx, maxx, t), (i % 2 == 0) ? minz + 1f : maxz - 1f) : new Vector2((i % 2 == 0) ? minx + 1f : maxx - 1f, Mathf.Lerp(minz, maxz, t));
                if (!Inside(l, q)) continue;
                Palm(palmSrc, new Vector3(q.x, DY + .01f, q.y), pi++);
            }
        }
        // flower planters along the forecourt and at the deck's doors
        foreach (float x in new[] { -90f, -78f, -66f, 66f, 78f, 90f }) BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x, CZ - 104f, x < 0 ? "flower_empodium" : "flower_gazania");
        foreach (float x in new[] { -24f, -14f, 14f, 24f }) BraxChampionsPromenadeFurniture.Planter(m, cols, models, CX + x, CZ - 60.5f, "flower_gazania");
    }

    static void Palm(GameObject[] src, Vector3 at, int i)
    {
        var s = src[i % 2]; if (!s) { OwnerMissing++; return; }
        var w = at * K;
        var o = BraxMiastoAssety.Postaw(models, s, "Palma " + (Palms + 1), w, (i * 137.508f) % 360f, 1f, false, true);
        if (!o) return;
        BraxChampionsPalmTrunk.OnSpot(o, w);
        BraxMiastoAssety.LodyZWezlow(o, new[] { .25f, .08f, .012f });
        var col = new GameObject("pień palmy " + (Palms + 1)); col.transform.SetParent(cols, false); col.transform.position = w + Vector3.up * 2f * K;
        var cc = col.AddComponent<CapsuleCollider>(); cc.radius = .3f * K; cc.height = 4f * K; Colliders++;
        Palms++;
    }
}
