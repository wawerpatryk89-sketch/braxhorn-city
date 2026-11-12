using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// AQUAPARK BRAXHORN - LENIWA RZEKA (BraxChampionsAquapark): west of the lagoon, where the big west lawn was. A ring channel
// 6.5 m wide and 1.2 m deep (the pool's own shell: marble coping, navy waterline, aqua tiles, underwater lights, swimming)
// round a palm island with boulders; three water arches jump from the island's rocks over the channel; the BRAXHORN swim
// rings drift with the current (BraxChampionsLeniwaRzeka): AKCJA by a ring = sit in it and float round the island,
// joystick / AKCJA = step out on the bank. Stairs with handrails from the east deck and the north deck, two ladders.
//   outer x -97..-64, z -58..8 (r 13) | island x -90.5..-70.5, z -51.5..1.5 (r 8) | flow anticlockwise seen from above.
public static partial class BraxChampionsAquapark
{
    static Pool river;
    const float RX0 = -97f, RX1 = -64f, RZ0 = -58f, RZ1 = 8f, RR = 13f, IX0 = -90.5f, IX1 = -70.5f, IZ0 = -51.5f, IZ1 = 1.5f, IR = 8f;
    public static int Rings;

    static void RiverLayout()
    {
        // RoundRect leaves its straight sides as single segments: split them every ~1 m (the stairs and ladders find their
        // place by the nearest edge point, the underwater lights go every 7.5 m along the edge)
        river = new Pool { id = "LENIWA RZEKA", edge = Dense(Ccw(W(RoundRect(RX0, RX1, RZ0, RZ1, RR))), 1f), water = DY - .12f, floor = DY - 1.32f };
        river.cop = Offset(river.edge, .45f);
        river.islands.Add(Dense(Ccw(W(RoundRect(IX0, IX1, IZ0, IZ1, IR))), 1f));
    }

    static List<Vector2> Dense(List<Vector2> p, float step)
    {
        var o = new List<Vector2>();
        for (int i = 0; i < p.Count; i++)
        {
            var a = p[i]; var b = p[(i + 1) % p.Count]; int n = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / step));
            for (int k = 0; k < n; k++) o.Add(Vector2.Lerp(a, b, k / (float)n));
        }
        return o;
    }

    static void RiverPool()
    {
        MakePool(river, poolTile, poolFloor, water, true, true);
        Steps(river, P(RX1, -26f), 4.0f, 6);          // from the east deck (the lagoon side)
        Steps(river, P(-80.5f, RZ1), 4.0f, 6);        // from the north deck (the kids' zone side)
        foreach (var l in new[] { P(RX0, -30f), P(-80.5f, RZ0), P(RX1, -48f) }) Ladder(river, l);
        Pools++;
    }

    // the channel's centre line (closed, anticlockwise = the flow), every ~1 m, nominal world
    static List<Vector2> RiverLine() => Dense(W(RoundRect((RX0 + IX0) * .5f, (RX1 + IX1) * .5f, (RZ0 + IZ0) * .5f, (RZ1 + IZ1) * .5f, (RR + IR) * .5f, 1.0f)), 1f);

    static void RiverIsland()
    {
        var isl = river.islands[0];
        // palms: two loose rows along the island, boulders between them, three water arches from the rocks at its shore
        var palmSrc = new[] { BraxMiastoAssety.Model("miasto_palmy", "palma"), BraxMiastoAssety.Model("miasto_palmy", "palma2") };
        int pi = 7;
        float cx = (IX0 + IX1) * .5f;
        for (float z = IZ0 + 5f; z <= IZ1 - 4f; z += 7.5f)
        {
            int k = Mathf.RoundToInt((z - IZ0) / 7.5f);
            var q = P(cx + ((k % 2 == 0) ? -4.6f : 4.4f), z);
            if (Inside(isl, q)) Palm(palmSrc, new Vector3(q.x, DY + .01f, q.y), pi++);
        }
        foreach (var (x, z, sx, sy, sz, seed) in new[] { (-84f, -40f, 2.6f, 1.5f, 2.1f, 3), (-77f, -27f, 3.2f, 1.9f, 2.4f, 7), (-83.5f, -12f, 2.2f, 1.3f, 1.9f, 11),
                                                          (-76.5f, -3f, 1.8f, 1.1f, 1.6f, 13), (-81f, -46.5f, 1.6f, 1.0f, 1.4f, 17) })
        {
            Boulder(walk, V(x, DY + sy * .30f, z), new Vector3(sx, sy, sz), seed);
        }
        // the arches: from a rock on the island's shore over the channel, landing in its middle (the rings pass under them)
        var a = new List<Vector3>(); var b = new List<Vector3>();
        foreach (var (x, z, dx) in new[] { (IX0 + .9f, -36f, -1f), (IX0 + .9f, -14f, -1f), (IX1 - .9f, -40f, 1f) })
        {
            Boulder(walk, V(x, DY + .35f, z), new Vector3(1.5f, 1.1f, 2.2f), (int)(z * -3));
            for (int j = -1; j <= 1; j++)
            {
                var p0 = V(x + dx * .55f, DY + .78f, z + j * .55f);
                m.Cylinder(steel, p0, .055f, .02f, 12, Quaternion.FromToRotation(Vector3.up, new Vector3(dx, 1.2f, 0).normalized), true, false);
                a.Add(p0); b.Add(V(x + dx * (4.2f + .3f * j), river.water, z + j * .9f));
            }
        }
        BraxChampionsMeshBatch.FountainWater(root, "leniwa rzeka: łuki wodne (woda)", Vector3.zero, K, a.ToArray(), b.ToArray(), Enumerable.Repeat(1.7f, a.Count).ToArray(),
            Enumerable.Repeat(.05f, a.Count).ToArray(), Enumerable.Repeat(3, a.Count).ToArray(), Enumerable.Repeat(true, a.Count).ToArray(), new Vector3[0], new float[0], fountainWater, 0f);
        // the sign by the east stairs: a black granite plinth, LENIWA RZEKA in the region's 3D letters, facing the lagoon
        var sc = V(RX1 + 3.2f, DY, -31.5f);                                                // clear of the bank's step-off points (1.55 m out)
        m.BoxMinMax(blackGloss, sc.x - .22f, sc.x + .22f, DY, DY + .62f, sc.z - 1.85f, sc.z + 1.85f);
        Col(sc.x - .22f, sc.x + .22f, DY, DY + .62f, sc.z - 1.85f, sc.z + 1.85f, "tablica leniwej rzeki");
        BraxChampionsLetters3D.Text(m, BraxChampionsHarbour.SignFace, BraxChampionsHarbour.SignDark, "LENIWA RZEKA", new Vector3(sc.x + .23f, DY + .22f, sc.z), 90f, .24f, .04f, .10f, 3.4f);
    }

    // the rings: BRAXHORN swim rings in the brand's colours, glossy PVC, two grab handles; each its own object (they move)
    static void RiverRings()
    {
        var mats = new[] { S("ring_red", new Color(.86f, .06f, .08f), .22f, 0f), S("ring_yellow", new Color(.98f, .78f, .06f), .22f, 0f),
                           S("ring_aqua", new Color(.05f, .62f, .78f), .22f, 0f), S("ring_white", new Color(.95f, .96f, .97f), .22f, 0f) };
        var line = RiverLine();
        var loop = line.Select(q => new Vector3(q.x, river.water, q.y) * K).ToArray();
        // the bank beside every loop point: on the outer deck 1 m past the coping, on the deck's level
        var bank = new Vector3[line.Count];
        for (int i = 0; i < line.Count; i++)
        {
            var t = (line[(i + 1) % line.Count] - line[(i - 1 + line.Count) % line.Count]).normalized;
            var outw = new Vector2(t.y, -t.x);                                               // right of an anticlockwise loop = outward
            var q = line[i] + outw * ((RX1 - IX1) * .5f + .45f + 1.1f);
            bank[i] = new Vector3(q.x, DY + .05f, q.y) * K;
        }
        var g = new GameObject("LENIWA RZEKA (AKCJA przy kole = wsiądź, joystick = zejdź na brzeg)"); g.transform.SetParent(root, false);
        var rz = g.AddComponent<BraxChampionsLeniwaRzeka>();
        int n = 12; var kola = new Transform[n]; var st = new BraxSeat[n];
        var prof = new List<Vector2>();
        const float R = .52f, r = .21f;
        for (int k = 0; k <= 24; k++) { float th = -Mathf.PI * .5f + k * Mathf.PI * 2f / 24f; prof.Add(new Vector2(R + r * Mathf.Cos(th), r * Mathf.Sin(th))); }
        for (int i = 0; i < n; i++)
        {
            var ring = new GameObject("koło " + (i + 1)); ring.transform.SetParent(g.transform, false);
            var p = loop[(i * loop.Length) / n]; ring.transform.position = p;
            var rb = new BraxChampionsMeshBatch();
            rb.Lathe(mats[i % mats.Length], Vector3.zero, prof.ToArray(), 36, Quaternion.identity, false, false);
            var hm = i % mats.Length == 3 ? mats[0] : mats[3];                              // white handles, red on the white ring
            // two grab handles on the outer flanks: a low loop of rope-covered PVC following the ring
            foreach (float s in new[] { -1f, 1f })
            {
                float x0 = s * (R + r * .62f), x1 = s * (R + r * .62f + .055f), y0 = r * .70f, y1 = r * .70f + .035f;
                var q0 = new Vector3(x0, y0, -.15f); var q1 = new Vector3(x1, y1, -.09f); var q2 = new Vector3(x1, y1, .09f); var q3 = new Vector3(x0, y0, .15f);
                rb.Tube(hm, q0, q1, .02f, 8); rb.Tube(hm, q1, q2, .02f, 8); rb.Tube(hm, q2, q3, .02f, 8);
            }
            var built = rb.Build(ring.transform, "koło " + (i + 1) + " | PVC", Folder, true);
            // the rings move: no static batching / occlusion flags on them
            foreach (var t in built.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            var seat = new GameObject("koło " + (i + 1) + " | siedzisko"); seat.transform.SetParent(ring.transform, false);
            var s0 = seat.AddComponent<BraxSeat>(); s0.label = "KOŁO"; seat.SetActive(false);
            kola[i] = ring.transform; st[i] = s0; Rings++;
        }
        rz.petla = loop; rz.brzeg = bank; rz.kola = kola; rz.siedziska = st;
        rz.predkosc = 1.05f * K; rz.zasieg = 3.4f * K; rz.siedzenie = .10f * K; rz.kolysanie = .035f * K;
        var snd = new GameObject("leniwa rzeka: szum łuków wodnych"); snd.transform.SetParent(g.transform, false); snd.transform.position = V(-80.5f, DY + 1f, -25f) * K;
        var zr = snd.AddComponent<BraxZrodloPetli>(); zr.rodzaj = "woda"; zr.glosnosc = .45f; zr.zasieg = 30f * K;
    }
}
