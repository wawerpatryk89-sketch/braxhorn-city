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
            foreach (float s in new[] { -1f, 1f })
            {
                float xa = Mathf.Min(s * (RoomInner - .1f), s * (RoomOuter - .1f)), xb = Mathf.Max(s * (RoomInner - .1f), s * (RoomOuter - .1f));
                // "Athlete changing room floor | green Stone Tiles" (walk surface = paving below).
                W.MM(floor, xa, xb, .230f, RoomFloorTop, RoomFront, RoomBack);
                // "Rear changing room exterior partition | Wooden Panels"
                W.MM(woodWall, xa, xb, .15f, 3.65f, RoomBack - RoomWallHalf, RoomBack + RoomWallHalf, true);
                // "Changing room front privacy partition | Wooden Panels"
                W.MM(woodWall, xa, xb, .15f, 3.65f, RoomFront - RoomWallHalf, RoomFront + RoomWallHalf, true);
                // "Changing room enclosed ceiling", ceiling
                W.MM(ceiling, xa, xb, 3.555f, 3.685f, RoomFront, RoomBack);
                // "Changing room outer partition | Wooden Panels"
                W.MM(woodWall, s * RoomOuter - RoomWallHalf, s * RoomOuter + RoomWallHalf, .15f, 3.65f, RoomFront, RoomBack, true);
                // "Changing room inner wall 1 | Wooden Panels" + "inner wall 2": 4.8 m entrance between.
                W.MM(woodWall, s * RoomInner - RoomWallHalf, s * RoomInner + RoomWallHalf, .15f, 3.65f, RoomFront, 43.9f, true);
                W.MM(woodWall, s * RoomInner - RoomWallHalf, s * RoomInner + RoomWallHalf, .15f, 3.65f, 48.7f, RoomBack, true);
                // Wooden Panels head over the entrance (above 2.70 m, no collision).
                W.MM(woodWall, s * RoomInner - RoomWallHalf, s * RoomInner + RoomWallHalf, 2.70f, 3.65f, 43.9f, 48.7f);
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
        var mirror = LuxSolid("mirror", new Color(.80f, .82f, .85f), .04f, 1f);
        var ceramic = LuxSolid("white_ceramic", new Color(.93f, .93f, .91f), .12f, 0f);
        var plaque = LuxSolid("black_sign_plate", new Color(.035f, .037f, .042f), .30f, .2f);
        int lockers = 0, seats = 0;
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
                    float bx = s * (8.875f + i * 1.75f);
                    W.MM(ceramic, bx - .25f, bx + .25f, .88f, 1.02f, wall + .12f, wall + .50f);
                    W.Rura(gold, new Vector3(bx, .88f, wall + .07f), new Vector3(bx, 1.12f, wall + .07f), .014f, 8);
                    W.Rura(gold, new Vector3(bx, 1.12f, wall + .07f), new Vector3(bx, 1.12f, wall + .22f), .012f, 8);
                }
                W.MM(mirror, va, vb, 1.10f, 2.10f, wall, wall + .01f);
                W.MM(led, va, vb, 2.15f, 2.18f, wall, wall + .04f);

                // Island bench: teak base + black leather top, seats on both sides.
                W.MM(teak, mid - 3f, mid + 3f, RoomFloorTop, RoomFloorTop + .40f, 45.2f, 46.0f, true);
                W.MM(leather, mid - 3f, mid + 3f, RoomFloorTop + .40f, RoomFloorTop + SeatTop, 45.2f, 46.0f);
                foreach (float bx in new[] { mid - 1.5f, mid + 1.5f })
                {
                    W.Siedzisko(new Vector3(bx, RoomFloorTop + SeatTop, 45.35f), Vector3.back, "ŁAWKA SZATNIA " + room); seats++;
                    W.Siedzisko(new Vector3(bx, RoomFloorTop + SeatTop, 45.85f), Vector3.forward, "ŁAWKA SZATNIA " + room); seats++;
                }

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
            " interiorLightMarker=true scale=K_once");
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
            Box(rear, "ATHLETES | gold inlay along the walk", gold, new Vector3(x, .203f, 69.65f), new Vector3(.05f, .006f, 20.7f), false);
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
        GradedSurface(rear, "ATHLETES driveway | road-to-car-park graded ramp", asphalt, AthleteDriveX0, AthleteDriveX1, 118f, 125f, .20f, 0f, true);
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
