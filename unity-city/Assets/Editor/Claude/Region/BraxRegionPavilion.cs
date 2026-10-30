using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// BRAXHORN CITY — pawilon „BRAXHORN SPORT” przy boiskach na podwórku:
// szatnia (szafki, ławki, prysznice) i sala walk z ringiem 6×6 m (miejsce bijatyk, zapas na 3 na 3).
// Stawiany w wolnym miejscu obok boisk (sprawdzane kolizje i płaski teren), wejście w stronę boisk.
// Materiały: Poly Haven (cladding, granit, tynk, płytki, guma, skóra, drewno). Drzwi działają jak
// w sklepie i klubie (BraxCityDoor), zarejestrowane w świecie gry.
public static class BraxRegionPavilion
{
    const float L = 18f, W = 11f, H = 3.6f, T = .3f;   // długość, głębokość, wysokość wnętrza, grubość ścian
    const float DoorX = -5f;                           // wejście (szatnia) w ścianie frontowej
    const float Split = -1f;                           // ściana działowa szatnia | sala walk

    static Transform root;
    static float ground;

    static void Box(string n, Vector3 at, Vector3 size, Material m, bool solid = false, float metres = 1f)
    { BraxRegion.Box(n, root, at, size, m, solid, metres); }

    public static void Apply(BraxCityWorld world, Transform cityRoot)
    {
        // 1) Boiska: środek i obrys (po jego boiskach), albo stałe miejsce z planu podwórka.
        var courts = cityRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CLAUDE boiska na podwórku");
        Bounds cb = new Bounds(new Vector3(10f, .15f, 80f), new Vector3(26f, 1f, 14f));
        if (courts)
        {
            var rs = courts.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0) { cb = rs[0].bounds; foreach (var r in rs.Skip(1)) cb.Encapsulate(r.bounds); }
        }
        float cy = cb.min.y;

        // 2) Wolne, płaskie miejsce wokół boisk (obie orientacje), najbliżej boisk.
        Physics.SyncTransforms();
        Vector3? spot = null; bool alongX = true; float bestD = float.MaxValue;
        for (float d = 6f; d <= 40f && !spot.HasValue; d += 2f)
        {
            var tries = new List<(Vector3, bool)>();
            tries.Add((new Vector3(cb.center.x, 0, cb.max.z + d + W * .5f), true));
            tries.Add((new Vector3(cb.center.x, 0, cb.min.z - d - W * .5f), true));
            tries.Add((new Vector3(cb.max.x + d + W * .5f, 0, cb.center.z), false));
            tries.Add((new Vector3(cb.min.x - d - W * .5f, 0, cb.center.z), false));
            foreach (var slide in new[] { -8f, 8f })
            {
                tries.Add((new Vector3(cb.center.x + slide, 0, cb.max.z + d + W * .5f), true));
                tries.Add((new Vector3(cb.center.x + slide, 0, cb.min.z - d - W * .5f), true));
            }
            foreach (var (p, ax) in tries)
            {
                if (p.z > 14f && p.z < 42f) continue;                 // nie na ulicy
                var size = ax ? new Vector3(L + 3f, H + 1.2f, W + 3f) : new Vector3(W + 3f, H + 1.2f, L + 3f);
                RaycastHit g;
                if (!BraxRegion.Ground(p.x, p.z, cy - 1.5f, cy + 1.5f, out g)) continue;
                var center = new Vector3(p.x, g.point.y + .35f + size.y * .5f, p.z);
                if (!BraxRegion.Free(center, size)) continue;
                bool flat = true;
                for (float u = -.5f; u <= .5f && flat; u += .25f)
                    for (float v = -.5f; v <= .5f && flat; v += .25f)
                    {
                        RaycastHit h;
                        if (!BraxRegion.Ground(p.x + u * size.x, p.z + v * size.z, cy - 1.5f, cy + 1.5f, out h) || Mathf.Abs(h.point.y - g.point.y) > .3f) flat = false;
                    }
                if (!flat) continue;
                float dist = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(cb.center.x, 0, cb.center.z));
                if (dist < bestD) { bestD = dist; spot = new Vector3(p.x, g.point.y, p.z); alongX = ax; }
            }
        }
        if (!spot.HasValue) { BraxRegion.Note("pawilon: brak wolnego płaskiego miejsca przy boiskach"); return; }

        var pos = spot.Value; ground = pos.y;
        // Front (lokalne -Z) ma patrzeć na boiska.
        var toCourts = new Vector3(cb.center.x - pos.x, 0, cb.center.z - pos.z).normalized;
        var yawRot = Quaternion.LookRotation(-toCourts, Vector3.up);
        if (!alongX) yawRot = Quaternion.LookRotation(-toCourts, Vector3.up); // ta sama zasada: front w stronę boisk

        root = BraxRegion.Root(cityRoot, "CLAUDE pawilon BRAXHORN SPORT (szatnia + sala walk)");
        root.position = pos; root.rotation = yawRot;
        BraxRegion.Rep.pavilionAt = pos;

        // 3) Materiały (Poly Haven; pierwszy dostępny slug).
        var plinth = BraxRegion.Pbr("Pawilon: granitowy cokół", 1.2f, null, "granite_tile", "granite_tile_02");
        var cladding = BraxRegion.Pbr("Pawilon: elewacja panelowa", 1.6f, null, "exterior_wall_cladding_02", "exterior_wall_cladding");
        var accent = BraxRegion.Pbr("Pawilon: akcent drewniany", 1.2f, null, "wood_plank_wall", "wood_planks");
        var roof = BraxRegion.Pbr("Pawilon: dach", 1.5f, null, "concrete_floor_02", "concrete_pavement_02");
        var plaster = BraxRegion.Pbr("Pawilon: tynk wnętrza", 1.5f, null, "white_plaster_02", "rectangular_facade_tiles");
        var tiles = BraxRegion.Pbr("Pawilon: płytki szatni", 1.0f, null, "floor_tiles_06", "granite_tile");
        var shower = BraxRegion.Pbr("Pawilon: płytki prysznica", .8f, null, "square_tiles", "marble_01");
        var rubber = BraxRegion.Pbr("Pawilon: podłoga sali walk", 1.0f, null, "rubber_tiles", "wood_floor_deck");
        var canvas = BraxRegion.Pbr("Ring: płótno", 1.2f, new Color(.92f, .92f, .90f), "leather_white", "wood_planks");
        var skirt = BraxRegion.Pbr("Ring: fartuch ciemne drewno", 1.0f, null, "dark_wood", "wood_planks");
        var wood = BraxRegion.Pbr("Pawilon: drewno ławek", 1.0f, null, "wood_planks", "wooden_planks");
        var locker = BraxRegion.Pbr("Pawilon: szafki stalowe", 1.0f, new Color(.55f, .58f, .60f), "painted_metal_shutter", "metal_plate", "exterior_wall_cladding");
        var steel = BraxRegion.Fitting("Pawilon: szczotkowana stal", BraxRegion.Steel, .32f, .9f);
        var glass = BraxRegion.Fitting("Pawilon: szkło hartowane", BraxRegion.Glass, .05f, 0f);
        var rope = BraxRegion.Fitting("Ring: lina w oplocie", new Color(.86f, .84f, .78f), .9f, 0f);

        float x0 = -L * .5f, x1 = L * .5f, z0 = -W * .5f, z1 = W * .5f, top = H + .4f;

        // 4) Bryła: posadzka, cokół, ściany z cladding i pasem drewna, dach z attyką.
        Box("Pawilon: płyta posadzki", new Vector3(0, -.12f, 0), new Vector3(L + .6f, .34f, W + .6f), plinth, true);
        Box("Pawilon: cokół", new Vector3(0, .25f, 0), new Vector3(L + .2f, .5f, W + .2f), plinth, true);
        // ściana tylna, boczne
        Box("Pawilon: ściana tylna", new Vector3(0, top * .5f, z1 - T * .5f), new Vector3(L, top, T), cladding, true);
        Box("Pawilon: ściana zachodnia", new Vector3(x0 + T * .5f, top * .5f, 0), new Vector3(T, top, W), cladding, true);
        Box("Pawilon: ściana wschodnia", new Vector3(x1 - T * .5f, top * .5f, 0), new Vector3(T, top, W), cladding, true);
        // ściana frontowa z otworem drzwi (2.1 × 2.7) i oknami sali walk
        float dl = DoorX - 1.05f, dr = DoorX + 1.05f;
        Box("Pawilon: front lewy", new Vector3((x0 + dl) * .5f, top * .5f, z0 + T * .5f), new Vector3(dl - x0, top, T), cladding, true);
        Box("Pawilon: nadproże wejścia", new Vector3(DoorX, 2.7f + (top - 2.7f) * .5f, z0 + T * .5f), new Vector3(2.1f, top - 2.7f, T), cladding, true);
        float win0 = Split + 1.2f, win1 = x1 - 1.2f;   // pasmo okien sali walk
        Box("Pawilon: front środkowy", new Vector3((dr + win0) * .5f, top * .5f, z0 + T * .5f), new Vector3(win0 - dr, top, T), cladding, true);
        Box("Pawilon: front prawy", new Vector3((win1 + x1) * .5f, top * .5f, z0 + T * .5f), new Vector3(x1 - win1, top, T), cladding, true);
        Box("Pawilon: podokiennik sali", new Vector3((win0 + win1) * .5f, .55f, z0 + T * .5f), new Vector3(win1 - win0, 1.1f, T), cladding, true);
        Box("Pawilon: nadproże okien", new Vector3((win0 + win1) * .5f, 2.9f + (top - 2.9f) * .5f, z0 + T * .5f), new Vector3(win1 - win0, top - 2.9f, T), cladding, true);
        Box("Pawilon: szyby sali walk", new Vector3((win0 + win1) * .5f, 2.0f, z0 + T * .5f), new Vector3(win1 - win0, 1.8f, .03f), glass, true);
        for (float mx = win0; mx <= win1 + .01f; mx += (win1 - win0) / 4f)
            Box("Pawilon: słupek okienny", new Vector3(mx, 2.0f, z0 + T * .5f), new Vector3(.08f, 1.8f, T + .02f), steel);
        // pas drewna wokół budynku na wysokości 2.9–3.4 (akcent elewacji)
        Box("Pawilon: pas drewniany front", new Vector3(0, 3.35f, z0 - .02f), new Vector3(L + .1f, .5f, .06f), accent);
        Box("Pawilon: pas drewniany tył", new Vector3(0, 3.35f, z1 + .02f), new Vector3(L + .1f, .5f, .06f), accent);
        Box("Pawilon: pas drewniany zachód", new Vector3(x0 - .02f, 3.35f, 0), new Vector3(.06f, .5f, W + .1f), accent);
        Box("Pawilon: pas drewniany wschód", new Vector3(x1 + .02f, 3.35f, 0), new Vector3(.06f, .5f, W + .1f), accent);
        // dach i attyka
        Box("Pawilon: strop", new Vector3(0, H + .15f, 0), new Vector3(L, .3f, W), plaster, true);
        Box("Pawilon: dach", new Vector3(0, H + .42f, 0), new Vector3(L + .4f, .24f, W + .4f), roof, true);
        Box("Pawilon: attyka front", new Vector3(0, H + .7f, z0 - .1f), new Vector3(L + .4f, .36f, .2f), cladding);
        Box("Pawilon: attyka tył", new Vector3(0, H + .7f, z1 + .1f), new Vector3(L + .4f, .36f, .2f), cladding);
        Box("Pawilon: attyka zachód", new Vector3(x0 - .1f, H + .7f, 0), new Vector3(.2f, .36f, W + .4f), cladding);
        Box("Pawilon: attyka wschód", new Vector3(x1 + .1f, H + .7f, 0), new Vector3(.2f, .36f, W + .4f), cladding);
        // daszek nad wejściem
        Box("Pawilon: daszek wejścia", new Vector3(DoorX, 3.0f, z0 - .9f), new Vector3(4.2f, .14f, 1.8f), accent);
        Box("Pawilon: spód daszka", new Vector3(DoorX, 2.92f, z0 - .9f), new Vector3(4.0f, .02f, 1.7f), plaster);

        // 5) Wnętrze: podłogi, ściana działowa z przejściem, tynk na ścianach.
        float inner0 = x0 + T, inner1 = x1 - T, zin0 = z0 + T, zin1 = z1 - T;
        Box("Szatnia: podłoga z płytek", new Vector3((inner0 + Split) * .5f, .035f, 0), new Vector3(Split - inner0, .07f, zin1 - zin0), tiles, true);
        Box("Sala walk: podłoga gumowa", new Vector3((Split + inner1) * .5f, .035f, 0), new Vector3(inner1 - Split, .07f, zin1 - zin0), rubber, true);
        // ściana działowa: dwa segmenty, przejście 1.8 m przy z=1.0
        float pass0 = .1f, pass1 = 1.9f;
        Box("Pawilon: ściana działowa S", new Vector3(Split, H * .5f, (zin0 + pass0) * .5f), new Vector3(.2f, H, pass0 - zin0), plaster, true);
        Box("Pawilon: ściana działowa N", new Vector3(Split, H * .5f, (pass1 + zin1) * .5f), new Vector3(.2f, H, zin1 - pass1), plaster, true);
        Box("Pawilon: nadproże przejścia", new Vector3(Split, 2.3f + (H - 2.3f) * .5f, (pass0 + pass1) * .5f), new Vector3(.2f, H - 2.3f, pass1 - pass0), plaster, true);
        // tynk od wewnątrz (cienkie okładziny na ścianach zewnętrznych)
        Box("Szatnia: tynk ściana tylna", new Vector3((inner0 + Split) * .5f, H * .5f, zin1 - .01f), new Vector3(Split - inner0, H, .02f), plaster);
        Box("Szatnia: tynk ściana zachodnia", new Vector3(inner0 + .01f, H * .5f, 0), new Vector3(.02f, H, zin1 - zin0), plaster);
        Box("Szatnia: tynk front", new Vector3((inner0 + dl) * .5f, H * .5f, zin0 + .01f), new Vector3(dl - inner0, H, .02f), plaster);
        Box("Sala walk: tynk ściana tylna", new Vector3((Split + inner1) * .5f, H * .5f, zin1 - .01f), new Vector3(inner1 - Split, H, .02f), plaster);
        Box("Sala walk: tynk ściana wschodnia", new Vector3(inner1 - .01f, H * .5f, 0), new Vector3(.02f, H, zin1 - zin0), plaster);

        // 6) Szatnia: szafki, ławki, prysznice, wyposażenie.
        float lockerZ = zin1 - .25f;
        for (float lx = inner0 + .55f; lx < Split - .6f; lx += .5f)
        {
            Box("Szatnia: szafka", new Vector3(lx, .95f, lockerZ), new Vector3(.46f, 1.9f, .48f), locker, true);
            Box("Szatnia: drzwiczki szafki", new Vector3(lx, .95f, lockerZ - .25f), new Vector3(.42f, 1.84f, .02f), locker);
            Box("Szatnia: uchwyt", new Vector3(lx + .15f, 1.0f, lockerZ - .27f), new Vector3(.02f, .12f, .02f), steel);
        }
        Box("Szatnia: ławka środkowa", new Vector3((inner0 + Split) * .5f, .42f, 1.2f), new Vector3(Split - inner0 - 2.2f, .06f, .4f), wood, true);
        foreach (float bx in new[] { inner0 + 1.4f, (inner0 + Split) * .5f, Split - 1.4f })
            Box("Szatnia: noga ławki", new Vector3(bx, .2f, 1.2f), new Vector3(.08f, .4f, .34f), steel);
        // prysznice przy ścianie zachodniej (3 kabiny), płytki + deszczownice
        for (int i = 0; i < 3; i++)
        {
            float sz = zin0 + 1.0f + i * 1.3f;
            Box("Prysznic: ściana kabiny", new Vector3(inner0 + .9f, 1.1f, sz + .65f), new Vector3(1.6f, 2.2f, .06f), shower, true);
            Box("Prysznic: płytki tylne", new Vector3(inner0 + .03f, 1.1f, sz), new Vector3(.03f, 2.2f, 1.3f), shower);
            Box("Prysznic: brodzik", new Vector3(inner0 + .8f, .07f, sz), new Vector3(1.5f, .07f, 1.2f), shower, true);
            Box("Prysznic: deszczownica", new Vector3(inner0 + .45f, 2.05f, sz), new Vector3(.22f, .02f, .22f), steel);
            Box("Prysznic: rura", new Vector3(inner0 + .1f, 1.7f, sz), new Vector3(.03f, .8f, .03f), steel);
            Box("Prysznic: bateria", new Vector3(inner0 + .12f, 1.15f, sz), new Vector3(.16f, .06f, .06f), steel);
        }
        Box("Prysznic: ścianka końcowa", new Vector3(inner0 + .9f, 1.1f, zin0 + .35f), new Vector3(1.6f, 2.2f, .06f), shower, true);
        // modele z Poly Haven w szatni
        var bench = BraxRegion.Model("painted_wooden_bench", "modular_street_seating");
        if (bench) BraxRegion.Put(root, bench, "Szatnia: ławka pod oknem", root.TransformPoint(new Vector3((dr + Split) * .5f - .3f, .07f, zin0 + .55f)), root.eulerAngles.y + 180f, 1f, true, 1.2f);
        var shelves = BraxRegion.Model("steel_frame_shelves_01", "steel_frame_shelves_02", "worn_metal_rack");
        if (shelves) BraxRegion.Put(root, shelves, "Szatnia: regał na ręczniki", root.TransformPoint(new Vector3(Split - .6f, .07f, zin0 + 1.2f)), root.eulerAngles.y + 90f, 1f, true, 2.2f);
        var stool = BraxRegion.Model("metal_stool_01", "wooden_stool_01", "metal_stool_02");
        if (stool) foreach (float sx in new[] { inner0 + 3.2f, inner0 + 4.6f })
            BraxRegion.Put(root, stool, "Szatnia: taboret", root.TransformPoint(new Vector3(sx, .07f, zin1 - 1.1f)), root.eulerAngles.y + 20f, 1f, true, .9f);
        var bin = BraxRegion.Model("metal_trash_can");
        if (bin) BraxRegion.Put(root, bin, "Szatnia: kosz", root.TransformPoint(new Vector3(dl - .5f, .07f, zin0 + .5f)), root.eulerAngles.y, 1f, true, 1.0f);
        var clock = BraxRegion.Model("wall_clock");
        if (clock) Wall(clock, "Szatnia: zegar", new Vector3((inner0 + Split) * .5f, 2.6f, zin1 - .06f), 0f, .45f);
        var ext = BraxRegion.Model("korean_fire_extinguisher_01");
        if (ext) BraxRegion.Put(root, ext, "Pawilon: gaśnica", root.TransformPoint(new Vector3(Split + .35f, .07f, zin0 + .4f)), root.eulerAngles.y, 1f, false, .7f);
        var wet = BraxRegion.Model("WetFloorSign_01");
        if (wet) BraxRegion.Put(root, wet, "Szatnia: znak mokra podłoga", root.TransformPoint(new Vector3(inner0 + 2.4f, .07f, zin0 + 1.0f)), root.eulerAngles.y - 30f, 1f, false, 1.0f);

        // 7) Sala walk: ring 6×6 m (podest, płótno, słupki, liny), schodki, tablica, worki… (worek: brak w CC0).
        var rc = new Vector3((Split + inner1) * .5f + .3f, 0, 0);
        float rh = .5f, rs = 6.0f;
        Box("Ring: podest", rc + new Vector3(0, rh * .5f, 0), new Vector3(rs, rh, rs), skirt, true);
        Box("Ring: płótno", rc + new Vector3(0, rh + .02f, 0), new Vector3(rs + .3f, .04f, rs + .3f), canvas, true);
        Box("Ring: fartuch front", rc + new Vector3(0, rh * .5f, -rs * .5f - .02f), new Vector3(rs + .3f, rh, .03f), skirt);
        Box("Ring: fartuch tył", rc + new Vector3(0, rh * .5f, rs * .5f + .02f), new Vector3(rs + .3f, rh, .03f), skirt);
        Box("Ring: fartuch zachód", rc + new Vector3(-rs * .5f - .02f, rh * .5f, 0), new Vector3(.03f, rh, rs + .3f), skirt);
        Box("Ring: fartuch wschód", rc + new Vector3(rs * .5f + .02f, rh * .5f, 0), new Vector3(.03f, rh, rs + .3f), skirt);
        float half = rs * .5f - .15f;
        foreach (float px in new[] { -half, half })
            foreach (float pz in new[] { -half, half })
            {
                Box("Ring: słupek narożny", rc + new Vector3(px, rh + .75f, pz), new Vector3(.1f, 1.5f, .1f), steel, true);
                Box("Ring: poduszka słupka", rc + new Vector3(px, rh + .95f, pz), new Vector3(.16f, 1.0f, .16f), canvas);
            }
        foreach (float ry in new[] { .5f, .85f, 1.2f })
        {
            Box("Ring: lina", rc + new Vector3(0, rh + ry, -half), new Vector3(half * 2, .045f, .045f), rope);
            Box("Ring: lina", rc + new Vector3(0, rh + ry, half), new Vector3(half * 2, .045f, .045f), rope);
            Box("Ring: lina", rc + new Vector3(-half, rh + ry, 0), new Vector3(.045f, .045f, half * 2), rope);
            Box("Ring: lina", rc + new Vector3(half, rh + ry, 0), new Vector3(.045f, .045f, half * 2), rope);
        }
        // schodki do ringu od strony przejścia
        for (int s = 0; s < 3; s++)
            Box("Ring: schodek", rc + new Vector3(-rs * .5f - .45f - s * .3f, (rh / 3f) * (3 - s) - (rh / 6f), 1.0f), new Vector3(.3f, rh / 3f * (3 - s), 1.0f), skirt, true);
        // tablica wyników i dekoracje
        var board = BraxRegion.Model("standing_chalkboard_01");
        if (board) BraxRegion.Put(root, board, "Sala walk: tablica", root.TransformPoint(new Vector3(inner1 - .7f, .07f, zin1 - .8f)), root.eulerAngles.y + 200f, 1f, true, 1.8f);
        var dart = BraxRegion.Model("dartboard");
        if (dart) Wall(dart, "Sala walk: tarcza", new Vector3(inner1 - .06f, 1.7f, zin0 + 1.6f), 90f, .5f);
        var ball = BraxRegion.Model("football", "dirty_football");
        if (ball) BraxRegion.Put(root, ball, "Sala walk: piłka", root.TransformPoint(new Vector3(Split + 1.0f, .07f, zin1 - .6f)), root.eulerAngles.y, 1f, false, .3f);
        var stoolB = BraxRegion.Model("metal_stool_02", "metal_stool_01", "wooden_stool_01");
        if (stoolB) foreach (float sz in new[] { -2.0f, 2.0f })
            BraxRegion.Put(root, stoolB, "Sala walk: taboret narożnika", root.TransformPoint(rc + new Vector3(-rs * .5f - .6f, .07f, sz)), root.eulerAngles.y, 1f, true, .9f);

        // 8) Logo i napis na fascii frontowej, nad wejściem i oknami.
        Logo(new Vector3(DoorX, H + .62f, z0 - .25f), 3.4f);
        Lettering("BRAXHORN SPORT", new Vector3((win0 + win1) * .5f, H + .62f, z0 - .26f), .42f);
        Lettering("SZATNIA · SALA WALK", new Vector3((win0 + win1) * .5f, H + .20f, z0 - .26f), .18f);

        // 9) Światło: ciepłe, bez cieni i bez poświaty; oprawy jako modele na suficie.
        BraxRegion.Warm(root, "Szatnia: światło", root.TransformPoint(new Vector3((inner0 + Split) * .5f, H - .3f, 0)), 1.4f, 9f);
        BraxRegion.Warm(root, "Szatnia: światło prysznice", root.TransformPoint(new Vector3(inner0 + 1.2f, H - .4f, zin0 + 1.8f)), 1.0f, 6f);
        BraxRegion.Warm(root, "Sala walk: światło nad ringiem", root.TransformPoint(rc + new Vector3(0, H - .3f, 0)), 1.6f, 10f);
        BraxRegion.Warm(root, "Sala walk: światło przy oknach", root.TransformPoint(new Vector3((win0 + win1) * .5f, H - .4f, zin0 + 1.5f)), 1.0f, 7f);
        BraxRegion.Warm(root, "Pawilon: lampa nad wejściem", root.TransformPoint(new Vector3(DoorX, 2.75f, z0 - .8f)), .9f, 6f);
        var lamp = BraxRegion.Model("mounted_fluorescent_lights", "modern_ceiling_lamp_01");
        if (lamp)
            foreach (var at in new[] { new Vector3((inner0 + Split) * .5f, 0, -1.5f), new Vector3((inner0 + Split) * .5f, 0, 2.0f), rc + new Vector3(-1.8f, 0, 0), rc + new Vector3(1.8f, 0, 0) })
            {
                var o = BraxRegion.Put(root, lamp, "Pawilon: oprawa sufitowa", root.TransformPoint(new Vector3(at.x, H - .12f, at.z)), root.eulerAngles.y, 1f, false, .3f);
                if (o) o.transform.position += Vector3.down * .12f;
            }

        // 10) Drzwi wejściowe (szkło w stalowej ramie), ten sam mechanizm co sklep i klub.
        var entrance = new GameObject("Source-kit working entrance BRAXHORN SPORT").transform;
        entrance.SetParent(root, false);
        entrance.localPosition = new Vector3(DoorX, 0, z0 + T * .5f);
        entrance.localRotation = Quaternion.identity;
        var hinge = new GameObject("Actual hinged source door leaf").transform;
        hinge.SetParent(entrance, false);
        hinge.localPosition = new Vector3(-1.05f, 0, 0);
        BraxCityArchitecture.Box("Wejście: szyba drzwi", hinge, new Vector3(1.05f, 1.37f, 0), new Vector3(1.94f, 2.56f, .02f), glass);
        BraxCityArchitecture.Box("Wejście: rama góra", hinge, new Vector3(1.05f, 2.67f, 0), new Vector3(2.1f, .06f, .06f), steel);
        BraxCityArchitecture.Box("Wejście: rama dół", hinge, new Vector3(1.05f, .06f, 0), new Vector3(2.1f, .12f, .06f), steel);
        BraxCityArchitecture.Box("Wejście: rama lewa", hinge, new Vector3(.03f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), steel);
        BraxCityArchitecture.Box("Wejście: rama prawa", hinge, new Vector3(2.07f, 1.37f, 0), new Vector3(.06f, 2.62f, .06f), steel);
        foreach (float side in new[] { -.06f, .06f })
            BraxCityArchitecture.Box("Wejście: uchwyt", hinge, new Vector3(1.85f, 1.1f, side), new Vector3(.035f, 1.0f, .035f), steel);
        foreach (var t in hinge.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0); // ruchome skrzydło
        var collider = hinge.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(1.05f, 1.35f, 0);
        collider.size = new Vector3(2.1f, 2.7f, .12f);
        var door = entrance.gameObject.AddComponent<BraxCityDoor>();
        door.id = "SPORT_CLUB_ENTRY"; door.label = "BRAXHORN SPORT";
        door.leaves = new[] { hinge }; door.sides = new[] { 1f };
        if (world) { world.doors = world.doors.Concat(new[] { door }).ToArray(); BraxRegion.Rep.pavilionDoor = true; }
        // próg: płytki wychodzą na zewnątrz, żeby wejście nie miało krawędzi
        Box("Wejście: próg granitowy", new Vector3(DoorX, .03f, z0 - .5f), new Vector3(3.0f, .06f, 1.2f), plinth, true);

        // 11) Przed wejściem: donice, rośliny, ławka, lampa, kosz.
        var planter = BraxRegion.Model("planter_box_02", "planter_box_01", "planter_box_03");
        if (planter)
            foreach (float px in new[] { DoorX - 2.6f, DoorX + 2.6f })
                BraxRegion.Put(root, planter, "Pawilon: donica przy wejściu", root.TransformPoint(new Vector3(px, 0, z0 - 1.0f)), root.eulerAngles.y, 1f, true, 1.0f);
        var pot = BraxRegion.Model("potted_plant_02", "potted_plant_04", "potted_plant_01");
        if (pot)
            foreach (float px in new[] { DoorX - 1.5f, DoorX + 1.5f })
                BraxRegion.Put(root, pot, "Pawilon: roślina przy wejściu", root.TransformPoint(new Vector3(px, 0, z0 - .7f)), root.eulerAngles.y + 40f, 1f, true, 1.6f);
        var seat = BraxRegion.Model("modular_street_seating", "painted_wooden_bench");
        if (seat) BraxRegion.Put(root, seat, "Pawilon: ławka przed salą", root.TransformPoint(new Vector3((win0 + win1) * .5f, 0, z0 - 1.6f)), root.eulerAngles.y + 180f, 1f, true, 1.2f);
        var lampPost = BraxRegion.Model("street_lamp_02", "street_lamp_01");
        if (lampPost) BraxRegion.Put(root, lampPost, "Pawilon: latarnia", root.TransformPoint(new Vector3(x1 + 1.6f, 0, z0 - 1.2f)), root.eulerAngles.y, 1f, true, 5.5f);
        var binOut = BraxRegion.Model("metal_trash_can");
        if (binOut) BraxRegion.Put(root, binOut, "Pawilon: kosz zewnętrzny", root.TransformPoint(new Vector3(x0 + 1.2f, 0, z0 - 1.0f)), root.eulerAngles.y, 1f, true, 1.0f);

        BraxRegion.Rep.pavilion = true;
        BraxRegion.Note("pawilon BRAXHORN SPORT przy boiskach w " + pos + " (front w stronę boisk)");
    }

    // Model wiszący na ścianie (zegar, tarcza): środek w punkcie, yaw względem budynku.
    static void Wall(GameObject source, string label, Vector3 local, float yaw, float maxSize)
    {
        var o = (GameObject)UnityEngine.Object.Instantiate(source, root, false);
        o.name = label;
        o.transform.position = Vector3.zero;
        o.transform.rotation = root.rotation * Quaternion.Euler(0, yaw, 0) * o.transform.rotation;
        var b = BraxRegion.Bounds(o);
        float big = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (big > maxSize && big > .001f) { o.transform.localScale *= maxSize / big; b = BraxRegion.Bounds(o); }
        o.transform.position = root.TransformPoint(local) - b.center;
        foreach (var r in o.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static void Logo(Vector3 local, float width)
    {
        const string logo = "Assets/CityRebuild/Brand/brand-wide.png";
        if (!System.IO.File.Exists(logo)) return;
        AssetDatabase.ImportAsset(logo, ImportAssetOptions.ForceSynchronousImport);
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(logo);
        if (!t) return;
        var m = new Material(Shader.Find("Standard")) { name = "Pawilon: logo BRAXHORN" };
        m.SetTexture("_MainTex", t); m.SetFloat("_Mode", 1); m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cutoff", .4f); m.SetFloat("_Glossiness", .15f); m.renderQueue = 2450;
        AssetDatabase.CreateAsset(m, BraxRegion.Out + "/Materials/pavilion-logo.mat");
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); q.name = "Pawilon: logo BRAXHORN";
        UnityEngine.Object.DestroyImmediate(q.GetComponent<Collider>());
        q.transform.SetParent(root, false);
        q.transform.localPosition = local;
        q.transform.localRotation = Quaternion.identity;        // Quad patrzy w -Z = front budynku
        float aspect = (float)t.width / Mathf.Max(1, t.height);
        q.transform.localScale = new Vector3(width, width / aspect, 1);
        q.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    static void Lettering(string text, Vector3 local, float size)
    {
        var obj = new GameObject("Pawilon: napis " + text);
        obj.transform.SetParent(root, false);
        obj.transform.localPosition = local;
        obj.transform.localRotation = Quaternion.identity;      // TextMesh czytelny od strony -Z = front
        var tm = obj.AddComponent<TextMesh>();
        tm.text = text; tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
        tm.fontSize = 64; tm.characterSize = size * 10f / 64f; tm.color = new Color(.93f, .93f, .90f);
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = obj.GetComponent<MeshRenderer>();
        if (mr && tm.font) mr.sharedMaterial = tm.font.material;
    }
}
