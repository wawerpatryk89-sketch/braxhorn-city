using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// A1 ATHLETES' WING (owner 9.11: "szatnie zawodników z tyłu, parking mały dla
// zawodników, wejścia — wszystko tak żeby było lux").
// Coordinates are A1 nominal metres (same frame as BraxChampionsArenaArrival);
// every size is a REAL dimension and the shared city scale K is applied once.
// Owner-selected finishes stay: Stone Tiles floor, Wooden Panels walls.
// No owner photo exists for the rooms: furniture follows real pro-team locker
// rooms (open wooden cubicles ~36 x 84 x 24 in, leather seat and back).
public static partial class BraxChampionsArenaArrival
{
    // Room shell (unchanged footprint): x 6.2..22 each side, z 38.5..52.5, ceiling 3.62.
    const float RoomInner = 6.2f, RoomOuter = 22f, RoomFront = 38.5f, RoomBack = 52.5f;
    const float RoomFloorTop = .248f, RoomWallHalf = .08f;
    // Pro-team open locker: .91 x 2.13 x .61 m (36 x 84 x 24 in).
    public const float LockerW = .91f, LockerH = 2.13f, LockerD = .61f;
    const float SeatTop = .47f; // seat surface 47 cm above floor

    static Material LuxSolid(string id, Color color, float roughness, float metallic)
    {
        var shader = Shader.Find(BraxCityMaterials.ShaderName);
        if (!shader) throw new InvalidOperationException("City PBR shader missing for athletes' wing");
        var m = new Material(shader) { name = "A1 athletes | " + id, enableInstancing = true };
        m.SetColor("_Color", color); m.SetFloat("_Roughness", roughness); m.SetFloat("_Metallic", metallic);
        m.SetFloat("_VertexTint", 0f); m.SetVector("_SunDirection", BraxCityMaterials.SunDirection);
        AssetDatabase.CreateAsset(m, Out + "A1_athletes_" + id + ".mat");
        return m;
    }

    // Build with the interior kit at world origin in NOMINAL metres, save the
    // merged meshes, then hang the group under the arrival root scaled by K.
    // (W writes world-space vertices; doing it under a K-scaled parent would
    // apply K twice.) Lights do not scale with transforms -> range x K here.
    static Transform BeginNominalGroup(string name)
    {
        var g = new GameObject(name).transform;
        g.position = Vector3.zero; g.rotation = Quaternion.identity; g.localScale = Vector3.one;
        W.Start(g);
        return g;
    }

    static void EndNominalGroup(Transform g, Transform arrivalRoot, string label)
    {
        W.Zapisz(label);
        g.SetParent(arrivalRoot, false);
        g.localPosition = Vector3.zero; g.localRotation = Quaternion.identity; g.localScale = Vector3.one * K;
        foreach (var seat in g.GetComponentsInChildren<BraxSeat>(true)) seat.surfaceY = seat.transform.position.y;
    }

    // Shell = the same walls/floor/ceiling as before (owner materials, same
    // colliders), now one merged interior mesh per material with the interior
    // light marker: no sun leaking "through" the ceiling onto indoor walls.
    static void AthleteRoomShells(Transform arrivalRoot, Material floor, Material woodWall, Material ceiling)
    {
        var g = BeginNominalGroup("A1 ATHLETES | changing rooms A+B shell | Stone Tiles + Wooden Panels");
        try
        {
            // Owner 10.11 13:05 (zero flicker): clean butt joints. The front and rear partitions run through to the outer
            // faces of the side walls; the side walls stand between the partitions; the ceiling sits between all four
            // walls and the floor ends inside the outer wall. No slab or wall ends in the plane of another face.
            const float h = RoomWallHalf;
            foreach (float s in new[] { -1f, 1f })
            {
                float Lo(float a, float b) => Mathf.Min(s * a, s * b);
                float Hi(float a, float b) => Mathf.Max(s * a, s * b);
                // "Athlete changing room floor | green Stone Tiles" (walk surface = paving below); 2 cm into the corridor
                // under the doorway, its outer end hidden inside the outer wall.
                W.MM(floor, Lo(RoomInner - .1f, RoomOuter), Hi(RoomInner - .1f, RoomOuter), .230f, RoomFloorTop, RoomFront, RoomBack);
                // "Rear changing room exterior partition | Wooden Panels" and "front privacy partition": full width
                W.MM(woodWall, Lo(RoomInner - h, RoomOuter + h), Hi(RoomInner - h, RoomOuter + h), .15f, 3.65f, RoomBack - h, RoomBack + h, true);
                W.MM(woodWall, Lo(RoomInner - h, RoomOuter + h), Hi(RoomInner - h, RoomOuter + h), .15f, 3.65f, RoomFront - h, RoomFront + h, true);
                // "Changing room enclosed ceiling", ceiling - between the four walls
                W.MM(ceiling, Lo(RoomInner + h, RoomOuter - h), Hi(RoomInner + h, RoomOuter - h), 3.555f, 3.685f, RoomFront + h, RoomBack - h);
                // "Changing room outer partition | Wooden Panels"
                W.MM(woodWall, s * RoomOuter - h, s * RoomOuter + h, .15f, 3.65f, RoomFront + h, RoomBack - h, true);
                // "Changing room inner wall 1 | Wooden Panels" + "inner wall 2": 4.8 m entrance between.
                W.MM(woodWall, s * RoomInner - h, s * RoomInner + h, .15f, 3.65f, RoomFront + h, 43.9f, true);
                W.MM(woodWall, s * RoomInner - h, s * RoomInner + h, .15f, 3.65f, 48.7f, RoomBack - h, true);
                // Wooden Panels head over the entrance (above 2.70 m, no collision).
                W.MM(woodWall, s * RoomInner - h, s * RoomInner + h, 2.70f, 3.65f, 43.9f, 48.7f);
            }
        }
        catch { W.Wyczysc(); UnityEngine.Object.DestroyImmediate(g.gameObject); throw; }
        EndNominalGroup(g, arrivalRoot, "mistrzowie_a1_athlete_rooms_shell");
    }

    // One open pro locker. Local frame: x across, y up, z from the wall (0)
    // to the open front (LockerD). Side panels are added by the run.
    static void ProLocker(Vector3 backAtFloor, Quaternion q, Material teak, Material leather,
        Material gold, Material led, bool seat, string label)
    {
        Vector3 P(float x, float y, float z) => backAtFloor + q * new Vector3(x, y, z);
        float inW = LockerW - .03f;
        W.OBox(teak, P(0, LockerH - .19f, LockerD * .5f), new Vector3(inW, .38f, LockerD), q, false, true);      // top cabinet
        W.OBox(gold, P(0, LockerH - .19f, LockerD + .003f), new Vector3(.30f, .06f, .006f), q);                   // name plate
        W.OBox(led, P(0, LockerH - .388f, LockerD - .03f), new Vector3(inW - .04f, .015f, .02f), q);              // LED under cabinet
        W.OBox(leather, P(0, 1.10f, .04f), new Vector3(inW - .02f, 1.20f, .04f), q);                              // leather back
        W.Rura(gold, P(-.40f, 1.62f, .32f), P(.40f, 1.62f, .32f), .012f, 8);                                     // hanging rod
        W.OBox(teak, P(0, .20f, .275f), new Vector3(inW, .40f, .55f), q, false, true);                            // drawer base
        W.OBox(gold, P(0, .30f, .555f), new Vector3(.20f, .02f, .02f), q);                                        // drawer pull
        W.OBox(leather, P(0, SeatTop - .035f, .30f), new Vector3(inW - .02f, .07f, .50f), q);                     // seat cushion
        if (seat) W.Siedzisko(P(0, SeatTop, .32f), q * Vector3.forward, label);
    }

    static int LockerRun(Vector3 start, Vector3 along, Vector3 outward, int count,
        Material teak, Material leather, Material gold, Material led, string room)
    {
        var q = Quaternion.LookRotation(outward, Vector3.up);
        for (int i = 0; i <= count; i++)
        {
            var edge = start + along * (i * LockerW);
            W.OBox(teak, edge + Vector3.up * (RoomFloorTop + LockerH * .5f) + outward * (LockerD * .5f),
                new Vector3(.03f, LockerH, LockerD), q, false, true);                                             // side panels
        }
        for (int i = 0; i < count; i++)
            ProLocker(start + along * ((i + .5f) * LockerW) + Vector3.up * RoomFloorTop, q, teak, leather, gold, led,
                true, "ŁAWKA SZAFKI " + room + " " + (i + 1));
        // Uplight along the cabinet tops (warm cove toward the dark ceiling).
        var mid = start + along * (count * LockerW * .5f);
        W.OBox(led, mid + Vector3.up * (RoomFloorTop + LockerH + .01f) + outward * (LockerD - .04f),
            new Vector3(count * LockerW - .06f, .02f, .02f), q);
        // One honest collider per run: you sit via the seat action, not by walking in.
        W.Kolizja(mid + Vector3.up * (RoomFloorTop + LockerH * .5f) + outward * (LockerD * .5f),
            new Vector3(count * LockerW, LockerH, LockerD), q);
        return count;
    }

    static void LuxDressingRooms(Transform arrivalRoot, Dictionary<string, Material> exterior)
    {
        // b33 phone: the shared exterior teak (white tint) read as plain BEIGE
        // boxes. Pro-team lux lockers: dark smoked walnut (same Poly Haven teak
        // veneer, tinted), deep black leather, warm LED - like NBA/UFC rooms.
        var teak = BraxChampionsFoundation.SourceMaterial("A1_athlete_locker_dark_walnut", "teak_veneer", 1.2f,
            new Color(.40f, .27f, .18f));
        teak.SetFloat("_Roughness", .38f);
        var gold = exterior["arena_gold"];
        var led = exterior["light"];
        var granite = exterior["dark_stone"];
        var leather = BraxChampionsFoundation.LibraryLeather("A1_athlete_locker_black_leather", new Color(.12f, .115f, .11f));
        // owner 10.11 12:33 (photo: lavender flat "mirror", white box basins): a live planar mirror with a polished edge
        // (BraxChampionsMirror) and round glazed vessel basins with a waste and gold mixer taps
        var ceramic = LuxSolid("white_glazed_porcelain", new Color(.95f, .95f, .94f), .06f, 0f);
        var basinInside = LuxSolid("porcelain_bowl_inside", new Color(.86f, .87f, .87f), .05f, 0f);
        var chrome = LuxSolid("polished_chrome_waste", new Color(.86f, .88f, .90f), .06f, 1f);
        var plaque = LuxSolid("black_sign_plate", new Color(.035f, .037f, .042f), .30f, .2f);
        int lockers = 0, seats = 0, gear = 0;
        var roomNames = new BraxChampionsMeshBatch();      // SZATNIA A / B: gold 3D letters on the black plaques
        var g = BeginNominalGroup("A1 ATHLETES | lux changing rooms A+B | teak lockers, black leather, gold");
        try
        {
            foreach (float s in new[] { -1f, 1f })
            {
                string room = s < 0 ? "A" : "B";
                float mid = s * 14f;
                // Back wall: 15 lockers facing the room (-Z), |x| 7.2..20.85.
                lockers += LockerRun(new Vector3(s * 7.2f, 0, RoomBack - RoomWallHalf), new Vector3(s, 0, 0), Vector3.back,
                    15, teak, leather, gold, led, room);
                // Outer wall: 13 lockers facing inward, z 39.4..51.23 (clear of the corner).
                lockers += LockerRun(new Vector3(s * (RoomOuter - RoomWallHalf), 0, 39.4f), Vector3.forward, new Vector3(-s, 0, 0),
                    13, teak, leather, gold, led, room);

                // Vanity on the front partition: 7 m teak cabinet, granite top,
                // four ceramic basins with gold taps, full-width mirror and LED.
                float va = Mathf.Min(s * 8f, s * 15f), vb = Mathf.Max(s * 8f, s * 15f), wall = RoomFront + RoomWallHalf;
                W.MM(teak, va, vb, RoomFloorTop, .84f, wall, wall + .58f, true);
                W.MM(granite, va - .02f, vb + .02f, .84f, .88f, wall, wall + .62f);
                for (int i = 0; i < 4; i++)
                {
                    float bx = s * (8.875f + i * 1.75f), bz = wall + .34f;
                    // the region's one washbasin (BraxUmywalka, owner 12.11 15:58): the G1 vessel bowl on the granite, a tall gold
                    // tap with its arc spout over the bowl, a soap dispenser (the stacked cylinders before it were not a basin)
                    BraxUmywalka.Postaw(W.Quad, (mm, a, b, rr) => W.Rura(mm, a, b, rr, 12, true), ceramic, gold, new Vector3(bx, .88f, bz), Vector3.back, .27f);
                }
                BraxChampionsMirror.Wall("A1 athletes vanity " + room, new Vector3((va + vb) * .5f, 1.62f, wall), Vector3.forward,
                    vb - va - .10f, 1.0f, gold, 10f, 16f, 1024, 160);
                W.MM(led, va, vb, 2.15f, 2.18f, wall, wall + .04f);

                // Island bench: teak base + black leather top, seats on both sides.
                W.MM(teak, mid - 3f, mid + 3f, RoomFloorTop, RoomFloorTop + .40f, 45.2f, 46.0f, true);
                W.MM(leather, mid - 3f, mid + 3f, RoomFloorTop + .40f, RoomFloorTop + SeatTop, 45.2f, 46.0f);
                foreach (float bx in new[] { mid - 1.5f, mid + 1.5f })
                {
                    W.Siedzisko(new Vector3(bx, RoomFloorTop + SeatTop, 45.35f), Vector3.back, "ŁAWKA SZATNIA " + room); seats++;
                    W.Siedzisko(new Vector3(bx, RoomFloorTop + SeatTop, 45.85f), Vector3.forward, "ŁAWKA SZATNIA " + room); seats++;
                }

                // Owner 12.11 12:07 ("w szatniach zawodników worek ma jeden wisieć do treningu, mają leżeć rękawice, tarczki ...
                // masz uzupełniać"): the owner's own gear models (global shop packs sklep_sprzet_01 / sklep_walka_01, real
                // metres) - a heavy bag on a chain from the ceiling in the free floor between the vanity and the bench,
                // gloves, head guards and a mouthguard box on the leather bench, a rope, kettlebells and dumbbells on the
                // floor by the outer lockers. (No focus pads exist in his packs yet.)
                gear += AthleteGear(g, s, mid, plaque, chrome);
                // Ceiling: six recessed warm downlights + two vertex lamps.
                foreach (float dx in new[] { -4.5f, 0f, 4.5f })
                foreach (float z in new[] { 42f, 49f })
                    W.Walec(led, new Vector3(mid + dx, 3.543f, z), .09f, .012f, 12);
                foreach (float dx in new[] { -3.5f, 3.5f })
                    W.Lampa("A1 athletes | changing room " + room + " warm light", new Vector3(mid + dx, 3.0f, 45.5f), 1.3f, 9f * K);

                // Entrance portal from the athletes' corridor: gold jambs, LED
                // under the panelled head, black plaque with the room name.
                float face = s * (RoomInner - RoomWallHalf - .01f);
                foreach (float z in new[] { 43.95f, 48.65f })
                    W.MM(gold, s * RoomInner - .15f, s * RoomInner + .15f, RoomFloorTop, 2.75f, z - .05f, z + .05f);
                W.MM(led, s * RoomInner - .12f, s * RoomInner + .12f, 2.67f, 2.70f, 44.0f, 48.6f);
                W.MM(plaque, Mathf.Min(face, face - s * .03f), Mathf.Max(face, face - s * .03f), 2.85f, 3.35f, 45.2f, 47.4f);
                // plaque front face at face - s*.03, letters 2 mm proud of it (owner 10.11: no TextMesh)
                BraxChampionsLetters3D.Text(roomNames, gold, granite, "SZATNIA " + room, new Vector3(face - s * .032f, 3.10f - .093f, 46.3f),
                    BraxChampionsLetters3D.YawFacing(new Vector3(-s, 0, 0)), .186f, .018f, .14f, 2.0f);
                // Corridor-side LED skirting along the panelled walls.
                foreach (var span in new[] { new Vector2(RoomFront, 43.85f), new Vector2(48.75f, RoomBack) })
                    W.MM(led, face - s * .02f, face, .30f, .32f, span.x, span.y);
            }
            seats += lockers;
            BraxChampionsLetters3D.Build(roomNames, g, "A1 ATHLETES | SZATNIA A / B", 1f);   // g carries K
        }
        catch { W.Wyczysc(); UnityEngine.Object.DestroyImmediate(g.gameObject); throw; }
        EndNominalGroup(g, arrivalRoot, "mistrzowie_a1_athlete_rooms_lux");
        Debug.Log("A1_ATHLETE_ROOMS_LUX rooms=2 lockers=" + lockers + " lockerSize=" + LockerW + "x" + LockerH + "x" + LockerD +
            " seats=" + seats + " vanity=7m_granite_4basins_mirror island=6m_leather floor=stone_tiles walls=wooden_panels" +
            " interiorLightMarker=true scale=K_once gear=" + gear);
    }

    // One owner model (real metres, base at its pivot) under the nominal group g at p (nominal), yaw; g carries K later.
    // tilt / roll: the model turned about its own x / z after the yaw (90 = a standing pair of gloves laid down; about the
    // axis the two gloves stand along, so they stay side by side on the bench and neither ends up in the air);
    // lift: how far its pivot (the base of the standing model) rises so the laid-down model rests on the surface
    static bool GearModel(Transform g, string pack, string name, Vector3 p, float yaw, float tilt = 0f, float lift = 0f, float roll = 0f)
    {
        var src = BraxMiastoAssety.Model(pack, name); if (!src) return false;
        var o = BraxMiastoAssety.Postaw(g, src, "A1 szatnia | " + name, Vector3.zero, yaw, 1f, true, true);
        if (!o) return false;
        o.transform.localPosition = p + Vector3.up * lift; o.transform.localRotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(tilt, 0, roll);
        o.transform.localScale = Vector3.one;
        return true;
    }

    // one welded oval chain link, 88 x 40 mm outside, of 10 mm round bar (a lifting chain for a 40 kg bag), standing in the plane of `across` and up:
    // two straight sides and two half-round ends (twelve short pieces of bar)
    static void ChainLink(Material m, Vector3 c, Vector3 across)
    {
        const float half = .024f, r = .015f, bar = .005f;
        var pts = new List<Vector3>();
        for (int i = 0; i <= 5; i++) { float a = Mathf.PI * i / 5f; pts.Add(c + across * (r * Mathf.Cos(a)) + Vector3.up * (half + r * Mathf.Sin(a))); }
        for (int i = 0; i <= 5; i++) { float a = Mathf.PI + Mathf.PI * i / 5f; pts.Add(c + across * (r * Mathf.Cos(a)) + Vector3.up * (-half + r * Mathf.Sin(a))); }
        for (int i = 0; i < pts.Count; i++) W.Rura(m, pts[i], pts[(i + 1) % pts.Count], bar, 8);
    }

    static int AthleteGear(Transform g, float s, float mid, Material steelDark, Material bolt)
    {
        int n = 0;
        float seat = RoomFloorTop + SeatTop;
        // the heavy bag (the owner's model: 1.0 m body + four straps to a top swivel, 1.25 m in all). Owner 12.11 12:45-13:11
        // ("na czym to wisi?", "czy to wisi na dobrej wysokości?", "co to za łańcuch?"): hung as in a boxing gym - the body
        // from 0.85 to 1.85 m, on a steel chain (four welded oval links of 10 mm bar, each turned a quarter to the
        // next) from the eye of a welded wall bracket: a 10 mm plate 0.22 x 0.95 m on six anchor bolts, an 80 x 80 mm box
        // arm 1.0 m out, a 60 mm tubular brace meeting the arm 0.4 m before the eye. The model stands 5.5 deg off plumb with
        // its swivel off its axis: it is turned upright and placed so the swivel is exactly under the chain. In the free
        // front corner by the outer lockers, out of the walking line from the door.
        float wall = RoomFront + RoomWallHalf;
        const float bagYaw = 0f, bagTilt = 5.48f;
        var hook = new Vector3(s * 18.4f, RoomFloorTop + .85f + 1.25f, wall + 1.0f);          // the top of the bag's swivel
        var knob = Quaternion.Euler(bagTilt, 0, 0) * new Vector3(-.002f, 1.25f, -.078f);         // where the model has it
        var bag = hook - Quaternion.Euler(0, bagYaw, 0) * knob;
        if (GearModel(g, "sklep_sprzet_01", "worek_bokserski", bag, bagYaw, bagTilt))
        {
            n++;
            float x = hook.x, z = hook.z, arm = hook.y + .273f;                 // the arm's underside: four links over the swivel
            W.MM(steelDark, x - .11f, x + .11f, arm - .78f, arm + .17f, wall, wall + .010f);                             // wall plate
            foreach (float bx in new[] { -.075f, .075f }) foreach (float by in new[] { -.72f, -.32f, .12f })
            {
                W.Rura(bolt, new Vector3(x + bx, arm + by, wall + .010f), new Vector3(x + bx, arm + by, wall + .014f), .016f, 12);   // washer
                W.Rura(bolt, new Vector3(x + bx, arm + by, wall + .014f), new Vector3(x + bx, arm + by, wall + .026f), .010f, 6);    // hex head
            }
            W.MM(steelDark, x - .04f, x + .04f, arm, arm + .08f, wall + .010f, z + .10f);                               // box arm 80 x 80
            W.MM(steelDark, x - .042f, x + .042f, arm - .002f, arm + .082f, z + .10f, z + .106f);                       // black end cap
            W.Rura(steelDark, new Vector3(x, arm - .70f, wall + .03f), new Vector3(x, arm + .01f, z - .40f), .03f, 12);   // brace
            W.MM(steelDark, x - .004f, x + .004f, arm - .05f, arm, z - .03f, z + .03f);                                 // eye plate
            // the chain: link k hangs in the eye / in the link above, the last one holds the bag's swivel
            for (int k = 0; k < 4; k++)
                ChainLink(bolt, new Vector3(x, arm - .03f - .039f - k * .058f, z), k % 2 == 0 ? Vector3.right : Vector3.forward);
            W.Kolizja(new Vector3(x, hook.y - .62f, z + .04f), new Vector3(.38f, 1.25f, .42f), Quaternion.identity);
        }
        // on the island bench (top at the seat height, z 45.2..46.0): gloves, head guards, the mouthguard box. Owner 12.11 12:45
        // "jak te rękawice stoją": the glove models stand on their cuffs (a shop display) - on the bench they lie on their side,
        // put down after training (turned 90 deg about their own x, lifted by half their depth so they rest on the leather)
        foreach (var (pack, name, dx, dz, yaw, tilt, roll, lift) in new[] {
            ("sklep_walka_01", "rekawice_bokserskie_braxhorn_czarno_zlote", -2.35f, 45.40f, 75f, 90f, 0f, .118f),   // the pair stands along x
            // (the red pair of sklep_sprzet_01 is one rigid model of two leaning gloves: laid down, one glove hangs in the
            // air - owner 12.11 12:55 "rękawiczki wiszą w powietrzu" - so it is not put here)
            ("sklep_walka_01", "kask_z_krata_czarno_zloty", 2.0f, 45.6f, 160f, 0f, 0f, 0f),
            ("sklep_walka_01", s < 0 ? "kask_bokserski_czerwony" : "kask_bokserski_niebieski", 2.55f, 45.45f, 200f, 0f, 0f, 0f),
            ("sklep_walka_01", "ochraniacz_szczeki_w_pudelku", .35f, 45.6f, 10f, 0f, 0f, 0f) })
            if (GearModel(g, pack, name, new Vector3(mid + s * dx, seat + .002f, dz), yaw, tilt, lift, roll)) n++;
        // (owner 12.11 13:13 "hantelki leżą, miniaturki": the rope, kettlebells and dumbbells lay loose on the floor of a 16 m
        // room - real size, but lost and careless there; a changing room keeps the bag and the fight gear on the bench)
        return n;
    }

    // Behind the arena (+Z): granite walk from the athletes' glass door to a
    // small private car park (8 bays) with its own driveway to road Z935.
    // Bays are the same real 3.05 x 5.2 m as the public car park (x K).
    public const float AthleteDriveX0 = -22.2f, AthleteDriveX1 = -15.8f;
    static void AthleteRearArrival(Transform root, Dictionary<string, Material> exterior,
        Material asphalt, Material paint, Material curb)
    {
        var rear = Group(root, "A1 ATHLETES | rear arrival: granite walk, private car park, driveway", Vector3.zero);
        var granite = exterior["dark_stone"]; var gold = exterior["gold"]; var light = exterior["light"]; var steel = exterior["steel"];
        // Granite walk from the door (z 59.3) under the glass portico to the car park (z 80).
        Box(rear, "ATHLETES | polished granite walk door -> car park", granite, new Vector3(0, .18f, 69.65f), new Vector3(6.0f, .04f, 20.7f), false);
        foreach (float x in new[] { -2.9f, 2.9f })
            Box(rear, "ATHLETES | gold inlay along the walk", gold, new Vector3(x, .206f, 69.65f), new Vector3(.05f, .012f, 20.7f), false);
        foreach (float x in new[] { -3.6f, 3.6f })
        foreach (float z in new[] { 68f, 72f, 76f })
        {
            Box(rear, "ATHLETES | black bollard light", granite, new Vector3(x, .62f, z), new Vector3(.16f, .84f, .16f));
            Box(rear, "ATHLETES | warm bollard head", light, new Vector3(x, 1.08f, z), new Vector3(.24f, .10f, .24f), false);
        }
        // Planters with clipped greenery either side of the walk (as at the public entrance).
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * 6.6f;
            Box(rear, "ATHLETES | black-stone planter", granite, new Vector3(x, .43f, 74.5f), new Vector3(4.6f, .54f, 2.0f));
            Box(rear, "ATHLETES | gold planter cap", gold, new Vector3(x, .715f, 74.5f), new Vector3(4.7f, .035f, 2.1f), false);
            for (int i = -1; i <= 1; i++) PlacePremiumShrub(rear, new Vector3(x + i * 1.45f, .25f, 74.5f), side < 0 ? 12f : -12f, 1.0f);
        }
        // Black granite stele with the entrance name, beside the door under the canopy.
        Box(rear, "ATHLETES | black granite name stele", granite, new Vector3(5.6f, 1.26f, 61.6f), new Vector3(1.6f, 2.2f, .30f));
        // Gold border = gold slab with a black granite inset on top of it.
        Box(rear, "ATHLETES | stele gold border", gold, new Vector3(5.6f, 1.26f, 61.76f), new Vector3(1.66f, 2.26f, .02f), false);
        Box(rear, "ATHLETES | stele black inset", granite, new Vector3(5.6f, 1.26f, 61.78f), new Vector3(1.50f, 2.10f, .02f), false);
        // the stele's names: solid letters on the black inset (front face z 61.79), gold / stainless, read from +Z
        var stele = new BraxChampionsMeshBatch();
        BraxChampionsLetters3D.Text(stele, gold, granite, "WEJŚCIE ZAWODNIKÓW", new Vector3(5.6f, 1.70f - .05f, 61.792f), 0f, .10f, .012f, .12f, 1.32f);
        BraxChampionsLetters3D.Text(stele, gold, granite, "ATHLETES ONLY", new Vector3(5.6f, 1.42f - .034f, 61.792f), 0f, .068f, .010f, .16f, 1.32f);
        BraxChampionsLetters3D.Text(stele, steel, granite, "BRAXHORN ARENA", new Vector3(5.6f, .95f - .031f, 61.792f), 0f, .062f, .010f, .22f, 1.32f);
        BraxChampionsLetters3D.Build(stele, rear, "ATHLETES | name stele");

        // Private car park: asphalt top .20 (4 cm over the site slab / grass).
        Box(rear, "ATHLETES car park | asphalt, real ground collider", asphalt, new Vector3(-3.1f, .17f, 90f), new Vector3(38.2f, .06f, 20f));
        for (int i = 0; i <= 8; i++)
            Box(rear, "ATHLETES car park | bay line", paint, new Vector3(-12.2f + i * 3.05f, .204f, 97.4f), new Vector3(.09f, .008f, 5.2f), false);
        Box(rear, "ATHLETES car park | bay head line", paint, new Vector3(0, .204f, 94.8f), new Vector3(24.4f, .008f, .08f), false);
        for (int i = 0; i < 8; i++)
            Box(rear, "ATHLETES car park | wheel stop", curb, new Vector3(-12.2f + (i + .5f) * 3.05f, .275f, 99.35f), new Vector3(1.28f, .15f, .19f));
        // Driveway to road Z935: asphalt, then a graded ramp over the cut sidewalk (layout.A1_PARKING_ENTRANCES).
        float dx = (AthleteDriveX0 + AthleteDriveX1) * .5f, dw = AthleteDriveX1 - AthleteDriveX0;
        Box(rear, "ATHLETES driveway | asphalt", asphalt, new Vector3(dx, .17f, 109f), new Vector3(dw, .06f, 18f));
        // owner 10.11 17:38-17:40: a street mouth across the Z935 sidewalk (road edge local z 125, the plot towards -z)
        BraxChampionsDriveMouth.Build(rear, new BraxChampionsDriveMouth.Spec{id="A1 ATHLETES driveway street mouth",
            alongX=true, edge=125f, dir=-1f, u0=AthleteDriveX0, u1=AthleteDriveX1, R=MouthR, depth=7f, topEnd=.20f, unit=K, assetDir=MouthMeshes},
            asphalt, CityWalk(curb), CityKerb());
        // Kerbs: 15 cm over the asphalt, open at the walk (x -3..3) and the driveway.
        Box(rear, "ATHLETES car park | east kerb", curb, new Vector3(16.125f, .275f, 90f), new Vector3(.25f, .15f, 20f));
        Box(rear, "ATHLETES car park | north kerb", curb, new Vector3(.1f, .275f, 100.125f), new Vector3(31.8f, .15f, .25f));
        Box(rear, "ATHLETES car park | south kerb west", curb, new Vector3(-12.6f, .275f, 79.875f), new Vector3(19.2f, .15f, .25f));
        Box(rear, "ATHLETES car park | south kerb east", curb, new Vector3(9.5f, .275f, 79.875f), new Vector3(13f, .15f, .25f));
        Box(rear, "ATHLETES car park | west kerb + driveway", curb, new Vector3(AthleteDriveX0 - .125f, .275f, 99f), new Vector3(.25f, .15f, 38f));
        Box(rear, "ATHLETES driveway | east kerb", curb, new Vector3(AthleteDriveX1 + .125f, .275f, 109f), new Vector3(.25f, .15f, 18f));
        // Two 6 m LED lamp posts outside the kerbs (same family as the public car park).
        foreach (var p in new[] { new Vector2(-24.2f, 84f), new Vector2(18.2f, 96f) })
        {
            Box(rear, "ATHLETES car park | steel lamp post", steel, new Vector3(p.x, 3.18f, p.y), new Vector3(.19f, 6.0f, .19f));
            Box(rear, "ATHLETES car park | LED luminaire", light, new Vector3(p.x, 6.22f, p.y), new Vector3(1.35f, .10f, .55f), false);
            var lamp = new GameObject("ATHLETES car park | warm mobile light").AddComponent<Light>();
            lamp.transform.SetParent(rear, false);
            lamp.transform.localPosition = new Vector3(p.x, 6.0f, p.y) * K;
            lamp.type = LightType.Point; lamp.intensity = .95f; lamp.range = 20f * K;
            lamp.color = new Color(1f, .91f, .78f); lamp.shadows = LightShadows.None;
            lamp.renderMode = LightRenderMode.ForceVertex;
        }
        Debug.Log("A1_ATHLETE_REAR_ARRIVAL walk=granite_6x20.7_goldInlay bollards=6 planters=2 stele=1" +
            " carPark=8bays_3.05x5.2 driveway=x" + AthleteDriveX0 + ".." + AthleteDriveX1 + "_to_Z935 lamps=2 scale=K_once");
    }
}
