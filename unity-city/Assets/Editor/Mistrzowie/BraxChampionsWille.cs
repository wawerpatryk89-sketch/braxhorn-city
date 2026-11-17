using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the three villas for sale (owner 16.11 23:13 "tylko wille kozaki zrobimy na sprzedaż 3", "naprawdę bogate,
// np. na wzgórzu"; sold by Monika in WAWER NIERUCHOMOŚCI, BraxChampionsKasynoSprzet.Biuro). A modern hillside villa as the real
// ones in the hills over Los Angeles are built (Standard Architecture 1471 Forest Knoll: ~930 m2, two-car garage, four
// bedrooms, white volumes, a cantilevered infinity pool; Fran Silvestre, Hollywood Hills: white plaster and grey stone, an
// L of glass framing the city): on its own flat pad cut into the hill, everything at grade, the view to local -z.
//
//  - GROUND FLOOR x -14..14, z -6..9, 3.5 m clear: one open room behind a 27 m glass front (one sliding panel parked open) -
//    the living room (sofa, the media wall, a rug, a lamp), the dining table for eight, the kitchen (a low run without wall
//    units - owner 12.11 "te kuchnie wielkie ... wypierdalasz" - and a stone island with stools); behind: the hall with the
//    pivot front door, a guest WC, a pantry with the door to the garage, the floating stair along the back wall;
//  - UPPER FLOOR x -16..16, z -8..8, cantilevered 2 m over the terrace (an oak soffit), its glass set back 1.5 m behind a
//    balcony with a glass balustrade: the master suite (bedroom, bathroom with a free-standing tub and a walk-in shower,
//    the walk-in wardrobe) and two bedrooms, each with its bathroom;
//  - the GARAGE x 14..23 for two cars, an up-and-over oak door on AKCJA (BraxChampionsSzafa, os = right);
//  - the TERRACE: limestone, the 16 x 5 m infinity pool (its front edge 15 mm under the water, the water falls into the
//    catch trough on the pad's edge), a raised jacuzzi, four loungers, an outdoor lounge; the drive and lawns behind;
//  - AKCJA "WILLA n · NA SPRZEDAŻ" at the board by the drive (Monika).
// Three finishes on the same plan: 1 white render and travertine, 2 the ground floor in grey stone, 3 the upper floor in
// graphite. Nominal metres, built at the origin, then the group goes onto its pad x K (once).
public static partial class BraxChampionsWille
{
    static float K => BraxClaudeOsiedle.K;
    public const string Out = "Assets/PlayableGenerated/Mistrzowie/Wille";
    public const int Ile = 3;
    public static readonly string[] Nazwy = { "WILLA 1", "WILLA 2", "WILLA 3" };

    // ---- the plan (nominal metres, the pad's frame)
    public const float GX0 = -14f, GX1 = 14f, GZ0 = -6f, GZ1 = 9f, H0 = 3.5f, FFL1 = 3.9f, T = .3f;
    public const float UX0 = -16f, UX1 = 16f, UZ0 = -8f, UZ1 = 8f, UGlass = -6.5f, H1 = 3.1f, Roof = FFL1 + H1, RoofTop = Roof + .45f;
    public const float CX0 = 14f, CX1 = 23f, CZ0 = -1f, CZ1 = 9f, CH = 3.0f;
    public const float PadX0 = -24f, PadX1 = 27f, PadZ0 = -22.6f, PadZ1 = 26f;
    public const float PoolX0 = -13f, PoolX1 = 3f, PoolZ0 = -21.6f, PoolZ1 = -16.6f, PoolFloor = -1.6f, Water = -.06f;
    public const float StairX0 = -13f, StairZ0 = 5.9f, StairZ1 = 7.5f, Riser = FFL1 / 22f, Tread = .29f;
    public static float StairX1 => StairX0 + 21 * Tread;
    static readonly Vector3 Jacuzzi = new Vector3(9.5f, 0f, -18.6f);

    static Material render, renderIn, stone, graphite, deck, floorGf, floorUf, ceiling, soffit, alu, glass, steel, poolTile,
        lawn, paving, gravel, concrete, waterMat, waterBody, spaWater, oak, walnut, cushion, teak, porcelain, chrome, mirrorFrame, black, signWhite,
        stoneIn, fire, downlight, marbleIn, bottle,
        elewacja, dachBlacha, stalCzarna,   // the new plans (owner 17.11): the scanned exterior stucco, standing-seam metal, black steel
        dachowka, tynkKrem, tynkBez, skala; // owner 17.11 photos ("zrób takie"): clay barrel tiles, cream and warm beige stucco, the cliff's rock
    public static int Zbudowane, Kolizje, Siedziska, Modele, Palmy, Drzewa;
    static BraxChampionsMeshBatch b, bw, kat;
    static Transform g;
    static int nrWilli;                  // the villa being built (the plans that share the old terrain)
    static float pietro = FFL1;          // the upper floor of the plan being built (Siedz's colliders)

    static Material Inside(Material x, float a = .85f) { BraxChampionsInteriorLight.Apply(x, a); return x; }

    static void Materials(int nr)
    {
        var F = (Func<string, string, float, Color, Vector3, Material>)BraxChampionsFoundation.Fotoskan;
        render = F("willa_tynk_bialy", BraxChampionsFoundation.InteriorPaintSlug, 2f, new Color(.93f, .92f, .89f), BraxChampionsFoundation.MeanBeigeWall);
        renderIn = Inside(F("willa_sciany_wnetrza", BraxChampionsFoundation.InteriorPaintSlug, 2f, new Color(.92f, .91f, .88f), BraxChampionsFoundation.MeanBeigeWall), BraxChampionsInteriorLight.Walls);
        ceiling = Inside(F("willa_sufit", BraxChampionsFoundation.InteriorPaintSlug, 2f, new Color(.95f, .95f, .93f), BraxChampionsFoundation.MeanBeigeWall), .9f);
        stone = F("willa_kamien_szary", "granite_tile_04", 1.2f, new Color(.47f, .45f, .42f), BraxChampionsFoundation.MeanGraniteTile04);
        graphite = F("willa_grafit", "brushed_concrete", 2.5f, new Color(.20f, .20f, .21f), BraxChampionsFoundation.MeanBrushedConcrete);
        deck = BraxChampionsFoundation.LibraryTravertine("willa_taras_trawertyn", new Color(.95f, .93f, .90f), 1.5f);
        floorGf = Inside(BraxChampionsFoundation.Fotoskan("willa_posadzka_kamien", "large_floor_tiles_02", 3.0f, new Color(.86f, .84f, .80f), BraxChampionsFoundation.MeanLargeFloorTiles02));
        soffit = BraxChampionsFoundation.SourceMaterial("willa_podsufitka_dab", "oak_wood_planks", 2.4f, new Color(1.0f, .94f, .86f));
        poolTile = F("willa_basen_mozaika", "long_white_tiles", .9f, new Color(.56f, .76f, .80f), BraxChampionsFoundation.MeanLongWhiteTiles);
        lawn = F("willa_trawnik", "sparse_grass", 2f, new Color(.33f, .43f, .20f), BraxChampionsFoundation.MeanSparseGrass);
        paving = F("willa_podjazd", "rectangular_paving", 2f, new Color(.62f, .61f, .58f), BraxChampionsFoundation.MeanRectPaving);
        gravel = F("willa_dach_zwir", "concrete_floor_02", 2f, new Color(.55f, .55f, .53f), BraxChampionsFoundation.MeanConcreteFloor02);
        concrete = Inside(F("willa_garaz_posadzka", "concrete_floor_02", 2f, new Color(.66f, .66f, .64f), BraxChampionsFoundation.MeanConcreteFloor02));
        alu = BraxChampionsHarbour.Surface("willa_alu_czarne", new Color(.045f, .045f, .05f), .35f, .8f);
        steel = BraxChampionsHarbour.Surface("willa_stal_szczotkowana", new Color(.62f, .62f, .63f), .3f, 1f);
        glass = BraxChampionsHarbour.Surface("willa_szyba", new Color(.60f, .68f, .72f, .20f), .03f, .1f, null, true);
        signWhite = BraxChampionsHarbour.Surface("willa_tablica_biala", new Color(.96f, .96f, .95f), .4f, 0f);
        stoneIn = Inside(F("willa_kominek_kamien", "granite_tile_04", 1.2f, new Color(.50f, .48f, .45f), BraxChampionsFoundation.MeanGraniteTile04), .9f);
        fire = BraxChampionsHarbour.Surface("willa_kominek_plomien", new Color(.20f, .07f, .02f), .6f, 0f, new Color(1.0f, .46f, .12f));
        downlight = BraxChampionsHarbour.Surface("willa_oczko_led", new Color(.9f, .9f, .88f), .3f, 0f, new Color(1.0f, .93f, .80f));
        marbleIn = Inside(BraxChampionsFoundation.LibraryMarbleWhite("willa_lazienka_kamien", new Color(.97f, .96f, .94f), 4f), .9f);
        bottle = BraxChampionsHarbour.Surface("willa_butelki", new Color(.05f, .12f, .06f), .08f, .1f);
        black = BraxChampionsHarbour.Surface("willa_czern_mat", new Color(.03f, .03f, .035f), .5f, 0f);
        // owner 17.11 ("Te ściany są wykończone?" on the old white: a flat interior paint, grey in the shade) - the new plans' walls
        // in the scanned exterior stucco of the apartments (Poly Haven white_stucco_02, warm clean white), the roofs dark
        // standing-seam metal, the gable frames black steel
        elewacja = BraxChampionsFoundation.SourceMaterial("willa_tynk_elewacja", "white_stucco_02", 2.0f, new Color(1.24f, 1.22f, 1.18f));
        dachBlacha = BraxChampionsHarbour.Surface("willa_dach_blacha_grafit", new Color(.115f, .118f, .125f), .42f, .75f);
        stalCzarna = BraxChampionsHarbour.Surface("willa_stal_czarna", new Color(.035f, .035f, .04f), .45f, .85f);
        dachowka = BraxChampionsFoundation.SourceMaterial("willa_dachowka", "clay_roof_tiles_02", 2.5f, new Color(1.0f, 1.0f, 1.0f));
        tynkKrem = BraxChampionsFoundation.SourceMaterial("willa_tynk_kremowy", "white_stucco_02", 2.0f, new Color(1.46f, 1.41f, 1.28f));   // 17.11: ivory as his photo (was grey-brown)
        // the hill villa's cliff (his photo: a pale warm rock face under the pool): Poly Haven cliff_side at its real 1.83 m
        skala = BraxChampionsFoundation.SourceMaterial("willa_skala", "cliff_side", 1.83f, new Color(1.12f, 1.08f, 1.02f));
        tynkBez = BraxChampionsFoundation.SourceMaterial("willa_tynk_bezowy", "white_stucco_02", 2.0f, new Color(1.34f, 1.12f, .88f));   // his photo 2: a light warm peach-beige
        if (nr == 2) render = elewacja;          // the island's Miami house in the scanned exterior stucco (17.11 "Te ściany są wykończone?")
        teak = BraxChampionsHarbour.Surface("willa_tik", new Color(.42f, .30f, .19f), .55f, 0f);
        cushion = BraxChampionsHarbour.Surface("willa_poduchy_lezakow", new Color(.92f, .91f, .88f), .85f, 0f);
        BraxKatalog.Materials(); BraxKatalog.HomeMaterials();
        floorUf = BraxKatalog.OakSolid; oak = BraxKatalog.OakSolid; walnut = BraxKatalog.Walnut;
        porcelain = BraxKatalog.Porcelain; chrome = BraxKatalog.Chrome; mirrorFrame = BraxKatalog.BlackMatt;
        waterMat = BraxChampionsAquapark.Water("willa_basen_woda", new Color(.03f, .28f, .46f), new Color(.14f, .68f, .84f), .32f, .6f, 0f);
        waterBody = BraxChampionsAquapark.Volume("willa_basen_toń", new Color(.012f, .15f, .30f), new Color(.07f, .52f, .70f), .22f, .9f);
        spaWater = BraxChampionsAquapark.Water("willa_jacuzzi_woda", new Color(.05f, .30f, .50f), new Color(.16f, .70f, .88f), .42f, 1.4f, 1f);
    }

    // the finish of each volume per villa: 1 all white, 2 the ground floor clad in grey stone, 3 the upper floor graphite
    static Material ScianaParter(int nr) => render;                                 // 2: his Miami photo - white to the ground
    static Material ScianaPietro(int nr) => nr == 3 ? graphite : render;

    // ================================================================ public: one villa on its pad
    /// <summary>Builds villa nr (1..3) and puts it on its pad: srodek = the pad's centre (nominal metres, its terrace level),
    /// yaw = the turn of its view (local -z). The group carries K.</summary>
    public static Transform Willa(Transform parent, int nr, Vector3 srodek, float yaw)
    {
        nr = Mathf.Clamp(nr, 1, Ile);
        nrWilli = nr;
        Materials(nr);
        g = new GameObject("MIASTO MISTRZÓW | " + Nazwy[nr - 1] + " na sprzedaż (WAWER NIERUCHOMOŚCI)").transform;
        g.position = Vector3.zero; g.rotation = Quaternion.identity; g.localScale = Vector3.one;
        W.Start(g); b = new BraxChampionsMeshBatch(); bw = new BraxChampionsMeshBatch(); kat = new BraxChampionsMeshBatch();
        int kol0 = W.Kolizje;
        try
        {
            pietro = FFL1;
            if (nr == 1) PlanKlif();                                        // owner 17.11: each villa its own (his photo: BraxChampionsWillaKlif)
            else if (nr == 2) PlanWyspa();                                  // his Miami photo, the court, the jetty (BraxChampionsWillaWyspa)
            else if (nr == 3) PlanNabrzeze();                               // his beige Mediterranean photo (BraxChampionsWillaNabrzeze)
            else { Teren(); Basen(); Jacuzzi_(); Bryla(nr); Garaz(); Wnetrze(nr); Wyposazenie(nr); Ogrod(nr); Palmy_(); }
            b.Build(g, Nazwy[nr - 1] + " | detale", Out + "/Meshes", true, 1f);
            kat.Build(g, Nazwy[nr - 1] + " | meble (katalog BRAXHORN HOME)", Out + "/Meshes", false, 1f);
            bw.Build(g, Nazwy[nr - 1] + " | woda", Out + "/Meshes", false, 1f);
        }
        catch { W.Wyczysc(); UnityEngine.Object.DestroyImmediate(g.gameObject); throw; }
        W.Zapisz("mistrzowie_" + W.Bezpieczna(Nazwy[nr - 1]));
        Kolizje = W.Kolizje - kol0;
        g.SetParent(parent, false);
        g.localPosition = srodek * K; g.localRotation = Quaternion.Euler(0f, yaw, 0f); g.localScale = Vector3.one * K;
        Zbudowane++;
        Debug.Log("MISTRZOWIE_WILLA " + Nazwy[nr - 1] + " at=" + srodek + " yaw=" + yaw + " colliders=" + Kolizje + " seats=" + Siedziska +
                  " pool=16x5 jacuzzi=1 garage=2 bedrooms=3 bathrooms=3+wc scale=K_once");
        return g;
    }

    // ================================================================ helpers
    static void Out_(bool exterior) { W.Grupa = g; W.Wnetrze = !exterior; }
    static void MM(Material m, float x0, float x1, float y0, float y1, float z0, float z1, bool col = true) => W.MM(m, x0, x1, y0, y1, z0, z1, col);
    // an outside wall in two skins, its outer face on plane (outward = the side the outside is on): the outer half in the
    // facade finish (in the sun), a 4 mm gap, the inner half painted (indoors)
    static void Sciana(Material zew, bool alongX, float plane, float a0, float a1, float y0, float y1, float outward)
    {
        float t = T * .5f, o0 = plane, o1 = plane - outward * t, i0 = plane - outward * (t + .004f), i1 = plane - outward * T;
        if (alongX)
        {
            Out_(true); MM(zew, a0, a1, y0, y1, Mathf.Min(o0, o1), Mathf.Max(o0, o1));
            Out_(false); MM(renderIn, a0, a1, y0, y1, Mathf.Min(i0, i1), Mathf.Max(i0, i1), false);
        }
        else
        {
            Out_(true); MM(zew, Mathf.Min(o0, o1), Mathf.Max(o0, o1), y0, y1, a0, a1);
            Out_(false); MM(renderIn, Mathf.Min(i0, i1), Mathf.Max(i0, i1), y0, y1, a0, a1, false);
        }
    }
    // a partition .12 thick with door openings (a0..a1 along it; each door [d0, d1] full height up to dh, a lintel above)
    static void Przegroda(bool alongX, float plane, float a0, float a1, float y0, float y1, params Vector2[] drzwi)
    {
        Out_(false); const float t = .06f; const float dh = 2.25f;
        float cur = a0;
        var list = new List<Vector2>(drzwi); list.Sort((p, q) => p.x.CompareTo(q.x));
        foreach (var d in list)
        {
            Seg(alongX, plane, t, cur, d.x, y0, y1);
            Seg(alongX, plane, t, d.x, d.y, y0 + dh, y1);        // the lintel over the door
            cur = d.y;
        }
        Seg(alongX, plane, t, cur, a1, y0, y1);
    }
    static void Seg(bool alongX, float plane, float t, float a0, float a1, float y0, float y1)
    {
        if (a1 - a0 < .01f || y1 - y0 < .01f) return;
        if (alongX) MM(renderIn, a0, a1, y0, y1, plane - t, plane + t); else MM(renderIn, plane - t, plane + t, y0, y1, a0, a1);
    }
    static void Drzwi(string id, bool alongX, float plane, float a0, float a1, float y0, float h = 2.25f)
    {
        Out_(false);
        BraxChampionsInteriorDoor.Single(id, id, alongX, plane, a0, a1, y0, h, oak, chrome);
    }
    // glazing between x0..x1 on the plane z (or x), frameless panes with slim black mullions every ~3.4 m; skip = open parts
    static void Szyba(bool alongX, float plane, float a0, float a1, float y0, float y1, params Vector2[] otwarte)
    {
        Out_(true);
        int n = Mathf.Max(1, Mathf.RoundToInt((a1 - a0) / 3.4f)); float w = (a1 - a0) / n;
        for (int i = 0; i < n; i++)
        {
            float p0 = a0 + i * w, p1 = p0 + w; bool open = false;
            foreach (var o in otwarte) if (p0 < o.y - .01f && p1 > o.x + .01f) open = true;
            if (!open)
            {
                if (alongX) MM(glass, p0 + .03f, p1 - .03f, y0 + .05f, y1 - .05f, plane - .012f, plane + .012f);
                else MM(glass, plane - .012f, plane + .012f, y0 + .05f, y1 - .05f, p0 + .03f, p1 - .03f);
            }
            // the mullion and the frame (black aluminium, 60 mm)
            if (alongX) { MM(alu, p0 - .03f, p0 + .03f, y0, y1, plane - .05f, plane + .05f, false); }
            else MM(alu, plane - .05f, plane + .05f, y0, y1, p0 - .03f, p0 + .03f, false);
        }
        if (alongX) { MM(alu, a1 - .03f, a1 + .03f, y0, y1, plane - .05f, plane + .05f, false); MM(alu, a0, a1, y0, y0 + .05f, plane - .05f, plane + .05f, false); MM(alu, a0, a1, y1 - .05f, y1, plane - .05f, plane + .05f, false); }
        else { MM(alu, plane - .05f, plane + .05f, y0, y1, a1 - .03f, a1 + .03f, false); MM(alu, plane - .05f, plane + .05f, y0, y0 + .05f, a0, a1, false); MM(alu, plane - .05f, plane + .05f, y1 - .05f, y1, a0, a1, false); }
    }
    static void Balustrada(bool alongX, float plane, float a0, float a1, float y)
    {
        Out_(true);
        if (alongX) { MM(glass, a0 + .01f, a1 - .01f, y, y + 1.05f, plane - .009f, plane + .009f); MM(steel, a0, a1, y + 1.05f, y + 1.09f, plane - .025f, plane + .025f, false); MM(alu, a0, a1, y - .08f, y + .04f, plane - .04f, plane + .04f, false); }
        else { MM(glass, plane - .009f, plane + .009f, y, y + 1.05f, a0 + .01f, a1 - .01f); MM(steel, plane - .025f, plane + .025f, y + 1.05f, y + 1.09f, a0, a1, false); MM(alu, plane - .04f, plane + .04f, y - .08f, y + .04f, a0, a1, false); }
    }
    static void Plane(BraxChampionsMeshBatch m, Material mat, Vector3 o, Vector3 u, Vector3 v) => m.Quad(mat, o, o + v, o + u + v, o + u);   // normal v x u
    // the water body under the surface: a box skin seen from inside (floor up, walls inward)
    static void Ton(float x0, float x1, float y0, float y1, float z0, float z1)
    {
        if (!waterBody) return;
        float dx = x1 - x0, dy = y1 - y0, dz = z1 - z0;
        Plane(bw, waterBody, new Vector3(x0, y0, z0), new Vector3(dx, 0, 0), new Vector3(0, 0, dz));
        Plane(bw, waterBody, new Vector3(x0, y0, z0), new Vector3(0, 0, dz), new Vector3(0, dy, 0));
        Plane(bw, waterBody, new Vector3(x1, y0, z1), new Vector3(0, 0, -dz), new Vector3(0, dy, 0));
        Plane(bw, waterBody, new Vector3(x1, y0, z0), new Vector3(-dx, 0, 0), new Vector3(0, dy, 0));
        Plane(bw, waterBody, new Vector3(x0, y0, z1), new Vector3(dx, 0, 0), new Vector3(0, dy, 0));
    }

    // ================================================================ the pad: terrace, lawns, drive, the retaining walls
    static void Teren()
    {
        Out_(true);
        const float s = .3f;          // slab
        // the limestone terrace round the pool (the pool and its trough left open)
        MM(deck, -21f, 24f, -s, 0f, PoolZ1, GZ0);                     // between the pool and the house, full width
        MM(deck, -21f, PoolX0, -s, 0f, PadZ0 + .3f, PoolZ1);          // west of the pool
        if (nrWilli == 2)                                             // east of the pool round the island's raised spa
        {
            MM(deck, PoolX1, SX0, -s, 0f, PadZ0 + .3f, PoolZ1); MM(deck, SX1, 24f, -s, 0f, PadZ0 + .3f, PoolZ1);
            MM(deck, SX0, SX1, -s, 0f, PadZ0 + .3f, SZ0); MM(deck, SX0, SX1, -s, 0f, SZ1, PoolZ1);
        }
        else MM(deck, PoolX1, 24f, -s, 0f, PadZ0 + .3f, PoolZ1);      // east of the pool (the jacuzzi stands on it)
        MM(deck, GX1, CX1, -s, 0f, GZ0, CZ0);                         // before the garage
        MM(deck, CX1, 24f, -s, 0f, GZ0, CZ1);
        MM(deck, -21f, GX0, -s, 0f, GZ0, 1f);                         // the west side path
        // lawns: the side strips and the back garden west of the drive
        MM(lawn, PadX0, -21f, -s, 0f, PadZ0 + .3f, PadZ1);
        MM(lawn, 24f, PadX1, -s, 0f, PadZ0 + .3f, PadZ1);
        MM(lawn, -21f, GX0, -s, 0f, 1f, GZ1);
        MM(lawn, -21f, -6f, -s, 0f, GZ1, PadZ1);
        // the drive: from the gate at the back of the pad to the garage and the front door
        MM(paving, -6f, 24f, -s, 0f, GZ1, PadZ1);
        // the trough's floor and the pad's front: the infinity edge spills into a catch trough on the pad's edge
        MM(poolTile, PoolX0, PoolX1, -.75f, -.6f, PadZ0 + .3f, PoolZ0 - .3f);
        MM(poolTile, PoolX0 - .3f, PoolX0, -.75f, -.3f, PadZ0 + .3f, PoolZ0 - .3f); MM(poolTile, PoolX1, PoolX1 + .3f, -.75f, -.3f, PadZ0 + .3f, PoolZ0 - .3f);
        MM(stone, PoolX0, PoolX1, -.75f, -.35f, PadZ0, PadZ0 + .3f);                     // the trough's outer lip, under the sightline
        MM(stone, PadX0, PoolX0, -4f, -s, PadZ0, PadZ0 + .3f);                           // the retaining wall under the pad's front
        MM(stone, PoolX0, PoolX1, -4f, -.75f, PadZ0, PadZ0 + .3f);
        MM(stone, PoolX1, PadX1, -4f, -s, PadZ0, PadZ0 + .3f);
        MM(stone, PadX0, -21f, -s, 0f, PadZ0, PadZ0 + .3f, false);                        // the lawn strips' front edge
        MM(stone, 24f, PadX1, -s, 0f, PadZ0, PadZ0 + .3f, false);
        MM(stone, -21f, PoolX0, -s, 0f, PadZ0, PadZ0 + .3f, false);
        MM(stone, PoolX1, 24f, -s, 0f, PadZ0, PadZ0 + .3f, false);
        // a glass balustrade on the pad's edge beside the pool (over the pool the edge stays open: the infinity view)
        Balustrada(true, PadZ0 + .15f, PadX0 + .2f, PoolX0 - .4f, 0f);
        if (nrWilli == 2) { Balustrada(true, PadZ0 + .15f, PoolX1 + .4f, WStX0 - .1f, 0f); Balustrada(true, PadZ0 + .15f, WStX1 + .1f, PadX1 - .2f, 0f); }   // the stair to the beach
        else Balustrada(true, PadZ0 + .15f, PoolX1 + .4f, PadX1 - .2f, 0f);
        // the trough's water (35 cm under the pool's) and a low safety rail-less lip: from the terrace the pool meets the sky
        Plane(bw, waterMat, new Vector3(PoolX0, -.45f, PadZ0 + .3f), new Vector3(PoolX1 - PoolX0, 0, 0), new Vector3(0, 0, PoolZ0 - .3f - (PadZ0 + .3f)));
    }

    // ================================================================ the infinity pool
    static void Basen()
    {
        Out_(true);
        // the tiled shell: walls under the coping (the deck's own edge is the coping), the infinity wall 15 mm under the water
        MM(poolTile, PoolX0 - .3f, PoolX0, PoolFloor - .2f, -.3f, PoolZ0 - .3f, PoolZ1);
        MM(poolTile, PoolX1, PoolX1 + .3f, PoolFloor - .2f, -.3f, PoolZ0 - .3f, PoolZ1);
        MM(poolTile, PoolX0, PoolX1, PoolFloor - .2f, -.3f, PoolZ1, PoolZ1 + .3f);
        MM(poolTile, PoolX0, PoolX1, PoolFloor - .2f, Water - .015f, PoolZ0 - .3f, PoolZ0);
        MM(poolTile, PoolX0, PoolX1, PoolFloor - .2f, PoolFloor, PoolZ0, PoolZ1);
        // the steps in at the east end (four treads)
        for (int i = 0; i < 4; i++)
        {
            float top = -.32f - i * .32f, z1 = PoolZ1, z0 = PoolZ1 - .45f * (i + 1);
            MM(poolTile, PoolX1 - 2.0f, PoolX1, PoolFloor, top, z0, z1);
        }
        MM(steel, PoolX1 - .45f, PoolX1 - .40f, Water, .95f, PoolZ1 + .25f, PoolZ1 + .30f, false);    // the handrail's post
        b.Tube(steel, new Vector3(PoolX1 - .425f, .95f, PoolZ1 + .275f), new Vector3(PoolX1 - .425f, .95f, PoolZ1 - .55f), .022f, 10);
        b.Tube(steel, new Vector3(PoolX1 - .425f, .95f, PoolZ1 - .55f), new Vector3(PoolX1 - .425f, -.55f, PoolZ1 - 1.35f), .022f, 10);
        // the water: its surface and its body; the swim zone
        Plane(bw, waterMat, new Vector3(PoolX0, Water, PoolZ0), new Vector3(PoolX1 - PoolX0, 0, 0), new Vector3(0, 0, PoolZ1 - PoolZ0));
        Ton(PoolX0 + .05f, PoolX1 - .05f, PoolFloor + .05f, Water - .005f, PoolZ0 + .05f, PoolZ1 - .05f);
        var surf = new GameObject("basen | lustro wody").transform; surf.SetParent(g, false); surf.localPosition = new Vector3((PoolX0 + PoolX1) * .5f, Water, (PoolZ0 + PoolZ1) * .5f);
        var zone = new GameObject("basen | strefa pływania").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = new Vector3((PoolX0 + PoolX1) * .5f, (PoolFloor + .05f + Water + .35f) * .5f, (PoolZ0 + PoolZ1) * .5f);
        zone.size = new Vector3(PoolX1 - PoolX0 - .7f, Water + .35f - PoolFloor - .05f, PoolZ1 - PoolZ0 - .7f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = surf; sw.basinId = "willa_basen";
        // two lights in the pool walls (night), warm-white
        foreach (float x in new[] { -9f, -1f }) b.Cylinder(BraxKatalog.Led, new Vector3(x, -.75f, PoolZ1 - .001f), .12f, .012f, 16, Quaternion.Euler(90f, 0f, 0f), true);
    }

    static void Jacuzzi_()
    {
        Out_(true);
        var c = Jacuzzi; const float R = 1.35f, r = 1.10f, top = .90f, wl = .82f;
        b.Cylinder(stone, c, R, top, 40, Quaternion.identity, false);                                  // the stone drum
        b.Surface(deck, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * Mathf.Lerp(R, r, v), top, Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Lerp(R, r, v)), 40, 1, true);   // the rim
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * r, Mathf.Lerp(.03f, top, v), Mathf.Sin(u * Mathf.PI * 2f) * r), 40, 2);                     // the inside, inward
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * Mathf.Lerp(r, .45f, v), .45f, Mathf.Sin(u * Mathf.PI * 2f) * Mathf.Lerp(r, .45f, v)), 40, 1, true);   // the seat ring
        b.Cylinder(poolTile, c + Vector3.up * .03f, .45f, .42f, 32, Quaternion.identity, false);
        b.Surface(poolTile, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * .45f * v, .03f, Mathf.Sin(u * Mathf.PI * 2f) * .45f * v), 32, 1);   // the floor (up)
        b.Surface(spaWater, (u, v) => c + new Vector3(Mathf.Cos(u * Mathf.PI * 2f) * (r - .01f) * v, wl, Mathf.Sin(u * Mathf.PI * 2f) * (r - .01f) * v), 40, 1);
        for (int i = 0; i < 12; i++)                                                                 // the drum's wall: a ring of colliders (you get in)
        {
            float a = i * 30f * Mathf.Deg2Rad, rm = (R + r) * .5f;
            W.Kolizja(c + new Vector3(Mathf.Cos(a) * rm, top * .5f, Mathf.Sin(a) * rm), new Vector3(.26f, top, .68f), Quaternion.Euler(0f, -i * 30f, 0f));
        }
        // two stone steps up from the terrace on its house side
        MM(stone, c.x - .7f, c.x + .7f, 0f, .30f, c.z + R - .05f, c.z + R + .45f);
        MM(stone, c.x - .7f, c.x + .7f, 0f, .60f, c.z + R - .2f, c.z + R + .05f);
        var spa = new GameObject("jacuzzi | lustro wody").transform; spa.SetParent(g, false); spa.localPosition = c + Vector3.up * wl;
        var zone = new GameObject("jacuzzi | strefa").AddComponent<BoxCollider>(); zone.transform.SetParent(g, false);
        zone.transform.localPosition = c + new Vector3(0f, (top + .5f + .05f) * .5f, 0f); zone.size = new Vector3(1.5f, top + .5f - .05f, 1.5f); zone.isTrigger = true;
        var sw = zone.gameObject.AddComponent<BraxChampionsPoolSwimVolume>(); sw.waterSurface = spa; sw.basinId = "willa_jacuzzi";
        W.Siedzisko(c + new Vector3(0f, .45f, -.78f), Vector3.forward, "JACUZZI"); W.Siedzisko(c + new Vector3(.78f, .45f, 0f), Vector3.left, "JACUZZI"); Siedziska += 2;
    }

    // ================================================================ the volumes
    static void Bryla(int nr)
    {
        var sp = ScianaParter(nr); var su = ScianaPietro(nr);
        // ---- ground floor: the slab, the walls, the glass front
        Out_(false); MM(floorGf, GX0 + T, GX1 - T, -.25f, 0f, GZ0 + .1f, GZ1 - T);
        Out_(true); MM(deck, GX0 + T, GX1 - T, -.25f, 0f, GZ0, GZ0 + .1f);             // the threshold under the glass, level
        MM(deck, .2f, 2.6f, -.25f, 0f, GZ1 - T, GZ1);                                  // and under the front door
        Sciana(sp, false, GX0, GZ0, GZ1, 0f, H0, -1f);                                 // west (outer face at x -14), round the back corner
        Sciana(sp, false, GX1, GZ0, CZ0, 0f, H0, 1f);                                  // east, before the garage
        // the wall to the garage (painted both sides, both indoors), the pantry's door to the garage z 6.5..7.45
        Out_(false); MM(renderIn, GX1 - T, GX1, 0f, H0, CZ0, 6.5f); MM(renderIn, GX1 - T, GX1, 0f, H0, 7.45f, GZ1 - T);
        MM(renderIn, GX1 - T, GX1, 2.25f, H0, 6.5f, 7.45f);
        // the back wall with the front door (x .2..1.8, 2.9 high), a black mullion, its glass side light (x 1.9..2.6)
        ScianaOkna(sp, GZ1, GX0 + T, .2f, 0f, H0, new Vector4(-13.2f, -7.2f, .6f, 3.0f)); Sciana(sp, true, GZ1, 2.6f, GX1, 0f, H0, 1f);
        Sciana(sp, true, GZ1, .2f, 2.6f, 2.95f, H0, 1f);
        Szyba(true, GZ1 - .15f, 1.93f, 2.57f, 0f, 2.95f);
        Out_(true); MM(alu, 1.8f, 1.9f, 0f, 2.95f, GZ1 - .25f, GZ1 - .05f);
        Out_(false); BraxChampionsInteriorDoor.Single("drzwi wejściowe", "WILLA | drzwi wejściowe (dąb, obrotowe)", true, GZ1 - .15f, .2f, 1.8f, 0f, 2.9f, oak, steel, .07f);
        // the front: 27.4 m of glass; one panel (x -6.9..-3.5) slid open behind its west neighbour (the way out to the terrace)
        Szyba(true, GZ0, GX0 + T + .031f, GX1 - T - .031f, 0f, H0, new Vector2(-6.5f, -4.0f));
        Out_(true); MM(glass, -10.3f, -7.0f, .05f, H0 - .05f, GZ0 + .075f, GZ0 + .099f); MM(alu, -7.03f, -6.97f, 0f, H0, GZ0 + .06f, GZ0 + .11f, false);
        // ---- the first floor slab: the ceiling over the rooms (indoors), the oak soffit of the cantilever, the white band over
        // the walls and the flat roof behind; the stair's void left open
        float vx0 = StairX0 - .2f, vx1 = StairX1 + .05f, vz0 = StairZ0 - .1f, vz1 = UZ1 - T;
        Out_(false); SlabRect(ceiling, GX0 + T, GX1 - T, H0, FFL1 - .04f, GZ0, UZ1 - T, vx0, vx1, vz0, vz1);
        Out_(true);
        MM(soffit, UX0, UX1, H0, FFL1 - .04f, UZ0, GZ0, false);                             // the cantilever over the terrace
        MM(soffit, UX0, GX0, H0, FFL1 - .04f, GZ0, UZ1, false);
        MM(soffit, GX1, UX1, H0, FFL1 - .04f, GZ0, CZ0, false);
        MM(render, GX0, GX0 + T, H0, FFL1 - .04f, GZ0, GZ1, false);                          // over the west wall
        MM(render, GX1 - T, GX1, H0, FFL1 - .04f, CZ0, GZ1, false);                          // over the east wall
        MM(render, GX1, UX1, H0, FFL1 - .04f, CZ0, UZ1, false);                              // over the garage's roof
        MM(render, GX1 - T, GX1, H0, FFL1 - .04f, GZ0, CZ0, false);
        MM(render, GX0 + T, GX1 - T, H0, FFL1 - .04f, UZ1 - T, GZ1, false);                  // the strip of flat roof behind
        // the floor finishes: oak indoors, limestone on the balcony
        Out_(false); SlabRect(floorUf, UX0 + T, UX1 - T, FFL1 - .04f, FFL1, UGlass + .05f, UZ1 - T, vx0, vx1, vz0, vz1);
        Out_(true); MM(deck, UX0, UX1, FFL1 - .04f, FFL1, UZ0, UGlass + .05f);
        MM(render, UX0 + T, UX1 - T, FFL1 - .04f, FFL1, UZ1 - T, UZ1, false);
        MM(render, UX0, UX0 + T, FFL1 - .04f, FFL1, UGlass + .05f, UZ1, false); MM(render, UX1 - T, UX1, FFL1 - .04f, FFL1, UGlass + .05f, UZ1, false);
        // ---- upper floor walls, its glass behind the balcony, the balcony's glass balustrade
        Sciana(su, false, UX0, UGlass, UZ1 - T, FFL1, Roof, -1f);
        Sciana(su, false, UX1, UGlass, UZ1 - T, FFL1, Roof, 1f);
        ScianaOkna(su, UZ1, UX0, UX1, FFL1, Roof, new Vector4(-15.3f, -13.8f, FFL1 + 1.5f, FFL1 + 2.6f), new Vector4(-12.8f, -7.4f, FFL1 + .9f, Roof - .3f),
            new Vector4(-4.4f, 1.4f, FFL1 + 1.5f, FFL1 + 2.6f), new Vector4(3.2f, 8.2f, FFL1 + 2.7f, Roof - .1f), new Vector4(12.2f, 15.2f, FFL1 + 1.5f, FFL1 + 2.6f));
        // the balcony's side walls (fins) frame the view
        Out_(true); MM(su, UX0, UX0 + T, FFL1, Roof, UZ0, UGlass); MM(su, UX1 - T, UX1, FFL1, Roof, UZ0, UGlass);
        Szyba(true, UGlass, UX0 + T, UX1 - T, FFL1, Roof, new Vector2(-12.0f, -9.0f), new Vector2(-5.0f, -2.0f), new Vector2(9.0f, 12.0f));
        Balustrada(true, UZ0 + .06f, UX0 + T, UX1 - T, FFL1);
        // ---- the roof: its slab (white ceiling inside), gravel, the parapet
        Out_(false); MM(ceiling, UX0 + T, UX1 - T, Roof, Roof + .3f, UGlass, UZ1 - T, false);
        Out_(true);
        MM(su, UX0, UX1, Roof, RoofTop, UZ0, UGlass);                                       // the eave over the balcony
        MM(su, UX0, UX0 + T, Roof, RoofTop, UGlass, UZ1, false); MM(su, UX1 - T, UX1, Roof, RoofTop, UGlass, UZ1, false);
        MM(su, UX0 + T, UX1 - T, Roof + .3f, RoofTop, UGlass, UZ1 - T);
        MM(su, UX0 + T, UX1 - T, Roof, RoofTop, UZ1 - T, UZ1, false);                       // over the back wall
        MM(gravel, UX0 + T, UX1 - T, RoofTop, RoofTop + .02f, UZ0 + T, UZ1 - T, false);
        MM(su, UX0, UX1, RoofTop, RoofTop + .3f, UZ0, UZ0 + T, false); MM(su, UX0, UX1, RoofTop, RoofTop + .3f, UZ1 - T, UZ1, false);
        MM(su, UX0, UX0 + T, RoofTop, RoofTop + .3f, UZ0 + T, UZ1 - T, false); MM(su, UX1 - T, UX1, RoofTop, RoofTop + .3f, UZ0 + T, UZ1 - T, false);
        // the house number in brushed steel by the front door
        BraxChampionsLetters3D.Text(b, steel, black, Nazwy[nr - 1], new Vector3(3.4f, 2.2f, GZ1 + .062f), BraxChampionsLetters3D.YawFacing(Vector3.forward), .16f, .012f, .12f);
        // the entrance: grey stone either side of the door, a cantilevered canopy with an oak soffit, two wall lights
        Out_(true);
        MM(stone, -1.2f, .2f, 0f, H0, GZ1, GZ1 + .05f); MM(stone, 2.6f, 4.4f, 0f, H0, GZ1, GZ1 + .05f);
        MM(render, -1.6f, 4.8f, 3.05f, 3.3f, GZ1 + .05f, GZ1 + 2.8f); MM(soffit, -1.55f, 4.75f, 3.03f, 3.05f, GZ1 + .05f, GZ1 + 2.75f, false);
        foreach (float lx in new[] { -.55f, 3.4f })
        {
            b.Box(black, new Vector3(lx, 2.05f, GZ1 + .11f), new Vector3(.12f, .36f, .12f), Quaternion.identity);
            b.Box(downlight, new Vector3(lx, 1.86f, GZ1 + .11f), new Vector3(.08f, .01f, .08f), Quaternion.identity);
            W.Lampa("wejście | kinkiet", new Vector3(lx, 1.7f, GZ1 + .4f), .5f, 4f * K, new Color(1f, .86f, .64f));
        }
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina przy wejściu", new Vector3(-2.2f, 0f, GZ1 + .8f), 40f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina przy wejściu", new Vector3(5.2f, 0f, GZ1 + .8f), 130f);
    }
    // an outside wall along x with window openings (x0, x1, sill, head): the wall round them, the glass in its middle
    static void ScianaOkna(Material zew, float plane, float a0, float a1, float y0, float y1, params Vector4[] okna)
    {
        var list = new List<Vector4>(okna); list.Sort((p, q) => p.x.CompareTo(q.x));
        float cur = a0;
        foreach (var o in list)
        {
            Sciana(zew, true, plane, cur, o.x, y0, y1, 1f);
            Sciana(zew, true, plane, o.x, o.y, y0, o.z, 1f);
            Sciana(zew, true, plane, o.x, o.y, o.w, y1, 1f);
            Szyba(true, plane - T * .5f, o.x + .031f, o.y - .031f, o.z, o.w);
            cur = o.y;
        }
        Sciana(zew, true, plane, cur, a1, y0, y1, 1f);
    }
    // a bathroom in stone: the floor 1 cm over the oak, the walls lined to 2.6 m 4 mm off the paint; door = (side 0: the
    // x0 wall, 1: the z0 wall; from, to); the back wall (z1) stops under its window wx0..wx1 at the sill (1.5 m)
    static void Lazienka(float x0, float x1, float z0, float z1, float y, Vector3 door, float wx0, float wx1)
    {
        Out_(false); const float h = 2.6f, o = .004f, t = .012f;
        MM(marbleIn, x0, x1, y, y + .01f, z0, z1, false);
        void SideX(float x, float s)                       // a wall at x, lining toward +s
        {
            float a = x + s * o, b2 = x + s * (o + t);
            if (door.x == 0f) { MM(marbleIn, Mathf.Min(a, b2), Mathf.Max(a, b2), y + .01f, y + h, z0, door.y); MM(marbleIn, Mathf.Min(a, b2), Mathf.Max(a, b2), y + .01f, y + h, door.z, z1); MM(marbleIn, Mathf.Min(a, b2), Mathf.Max(a, b2), y + 2.25f, y + h, door.y, door.z); }
            else MM(marbleIn, Mathf.Min(a, b2), Mathf.Max(a, b2), y + .01f, y + h, z0, z1);
        }
        SideX(x0, 1f);
        MM(marbleIn, x1 - o - t, x1 - o, y + .01f, y + h, z0, z1);
        float za = z0 + o, zb = z0 + o + t;
        if (door.x == 1f) { MM(marbleIn, x0 + o + t, door.y, y + .01f, y + h, za, zb); MM(marbleIn, door.z, x1 - o - t, y + .01f, y + h, za, zb); MM(marbleIn, door.y, door.z, y + 2.25f, y + h, za, zb); }
        else MM(marbleIn, x0 + o + t, x1 - o - t, y + .01f, y + h, za, zb);
        float zc = z1 - o - t, zd = z1 - o;
        if (wx1 > wx0) { MM(marbleIn, x0 + o + t, wx0, y + .01f, y + h, zc, zd); MM(marbleIn, wx1, x1 - o - t, y + .01f, y + h, zc, zd); MM(marbleIn, wx0, wx1, y + .01f, y + 1.5f, zc, zd); }
        else MM(marbleIn, x0 + o + t, x1 - o - t, y + .01f, y + h, zc, zd);
        // a heated towel rail on the side wall
        b.Tube(chrome, new Vector3(x1 - .07f, y + .9f, z0 + .6f), new Vector3(x1 - .07f, y + 1.6f, z0 + .6f), .012f, 8);
        b.Tube(chrome, new Vector3(x1 - .07f, y + .9f, z0 + 1.1f), new Vector3(x1 - .07f, y + 1.6f, z0 + 1.1f), .012f, 8);
        for (int i = 0; i < 6; i++) b.Tube(chrome, new Vector3(x1 - .07f, y + .95f + i * .12f, z0 + .6f), new Vector3(x1 - .07f, y + .95f + i * .12f, z0 + 1.1f), .009f, 8);
    }
    // a slab x0..x1, y0..y1, z0..z1 with a rectangular hole hx0..hx1, hz0..hz1 (the stair)
    static void SlabRect(Material m, float x0, float x1, float y0, float y1, float z0, float z1, float hx0, float hx1, float hz0, float hz1)
    {
        MM(m, x0, x1, y0, y1, z0, hz0);
        MM(m, x0, x1, y0, y1, hz1, z1);
        MM(m, x0, hx0, y0, y1, hz0, hz1);
        MM(m, hx1, x1, y0, y1, hz0, hz1);
    }

    // ================================================================ the garage (two cars), its up-and-over door on AKCJA
    static void Garaz()
    {
        Out_(false); MM(concrete, CX0, CX1 - T, -.25f, 0f, CZ0 + T, CZ1 - T);
        Out_(true); MM(concrete, 15.5f, 21.5f, -.25f, 0f, CZ1 - T, CZ1);                            // the threshold
        Sciana(render, false, CX1, CZ0 + T, CZ1 - T, 0f, CH, 1f);
        Sciana(render, true, CZ0, CX0, CX1, 0f, CH, -1f);
        Sciana(render, true, CZ1, CX0, 15.5f, 0f, CH, 1f); Sciana(render, true, CZ1, 21.5f, CX1, 0f, CH, 1f);
        Sciana(render, true, CZ1, 15.5f, 21.5f, 2.6f, CH, 1f);
        Out_(false); MM(ceiling, CX0, CX1 - T, CH, CH + .25f, CZ0 + T, CZ1 - T, false);
        Out_(true); MM(render, CX1 - T, CX1, CH, CH + .25f, CZ0, CZ1, false); MM(render, CX0, CX1 - T, CH, CH + .25f, CZ0, CZ0 + T, false);
        MM(render, CX0, CX1 - T, CH, CH + .25f, CZ1 - T, CZ1, false);
        MM(render, CX0, CX1, CH + .25f, CH + .46f, CZ0, CZ1); MM(gravel, CX0 + .2f, CX1 - .2f, CH + .46f, CH + .48f, CZ0 + .2f, CZ1 - .2f, false);
        // the door: an oak slat leaf 6 x 2.6 m hung from its top edge inside the opening; AKCJA swings it in and up
        var hinge = new GameObject("garaż | brama (zawias u góry)").transform; hinge.SetParent(g, false);
        hinge.localPosition = new Vector3(18.5f, 2.6f, CZ1 - .2f);
        var leaf = new BraxChampionsMeshBatch();
        for (int i = 0; i < 13; i++) leaf.Box(soffit, new Vector3(0f, -.1f - i * .2f, 0f), new Vector3(5.98f, .185f, .05f), Quaternion.identity);
        leaf.Box(alu, new Vector3(0f, -1.3f, -.04f), new Vector3(5.9f, 2.56f, .02f), Quaternion.identity);
        leaf.Box(steel, new Vector3(0f, -2.45f, .045f), new Vector3(.5f, .04f, .04f), Quaternion.identity);
        var leafT = leaf.Build(hinge, "garaż | brama (dąb)", Out + "/Meshes", true, 1f);
        var bc = leafT.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(0f, -1.3f, 0f); bc.size = new Vector3(6f, 2.6f, .1f);
        var drzwi = g.gameObject.AddComponent<BraxChampionsSzafa>();
        drzwi.zawias = hinge; drzwi.os = Vector3.right; drzwi.katOtwarcia = 90f; drzwi.napis = "BRAMA GARAŻU"; drzwi.komunikat = ""; drzwi.zasieg = 2.2f;
        drzwi.punkty = new[] { Punkt("garaż | AKCJA przed bramą", new Vector3(18.5f, 0f, CZ1 + 1.4f)), Punkt("garaż | AKCJA w garażu", new Vector3(18.5f, 0f, CZ1 - 3.2f)) };
        // inside: a tool wall, shelves with tyres, the charger, lights; two bays marked by their stops (cut in the floor: a
        // steel wheel stop on each bay, nothing painted over)
        Out_(false);
        MM(walnut, CX1 - T - .5f, CX1 - T, 0f, 2.0f, CZ0 + 1f, CZ0 + 6f);                  // the tall tool cabinets
        MM(steel, CX1 - T - .52f, CX1 - T - .5f, .1f, 1.95f, CZ0 + 1.05f, CZ0 + 5.95f, false);
        for (int i = 0; i < 3; i++) MM(steel, CX0 + .3f, CX0 + .8f, .5f + i * .6f, .53f + i * .6f, CZ0 + 1.0f, CZ0 + 3.0f);
        for (int i = 0; i < 4; i++) b.Cylinder(black, new Vector3(CX0 + .55f, .53f + (i % 2) * .6f + .12f, CZ0 + 1.45f + (i / 2) * 1.0f), .33f, .23f, 20, Quaternion.Euler(90f, 0f, 0f), true, true);
        foreach (float x in new[] { 16.2f, 20.4f }) MM(black, x - .6f, x + .6f, 0f, .1f, CZ0 + 1.2f, CZ0 + 1.35f);
        b.Box(black, new Vector3(CX1 - T - .1f, 1.3f, 2.5f), new Vector3(.12f, .4f, .26f), Quaternion.identity);   // the wall charger
        W.Lampa("garaż | światło", new Vector3(18.5f, CH - .2f, 4f), 1.1f, 9f * K);
    }

    static Transform Punkt(string nazwa, Vector3 p)
    {
        var t = new GameObject(nazwa).transform; t.SetParent(g, false); t.localPosition = p; return t;
    }

    // ================================================================ inside
    static void Wnetrze(int nr)
    {
        var c = new BraxKatalog.Ctx { m = kat, root = g, cols = g, unit = 1f, water = null, inst = g, seat = (p, f, l) => { W.Siedzisko(p, f, l); Siedziska++; } };
        string tag = Nazwy[nr - 1];
        Out_(false);
        // ---- ground floor partitions: the guest WC (x -5.5..-3, z 5..8.7) and the pantry (x 9..13.7, z 5..8.7)
        Przegroda(true, 5f, -5.5f, -3f, 0f, H0, new Vector2(-4.95f, -4.1f));
        Przegroda(false, -5.5f, 5.06f, GZ1 - T, 0f, H0);
        Przegroda(false, -3f, 5.06f, GZ1 - T, 0f, H0);
        Przegroda(true, 5f, 9f, GX1 - T, 0f, H0, new Vector2(10.6f, 11.45f));
        Przegroda(false, 9f, 5.06f, GZ1 - T, 0f, H0);
        Drzwi("WC", true, 5f, -4.95f, -4.1f, 0f); Drzwi("spiżarnia", true, 5f, 10.6f, 11.45f, 0f);
        Out_(false); MM(renderIn, GX1 - T - .01f, GX1 + .01f, 2.25f, H0, 6.5f, 7.45f, false);   // the lintel to the garage
        Drzwi("do garażu", false, GX1 - T * .5f, 6.5f, 7.45f, 0f);
        // ---- the floating stair (stone treads from the back wall) and its glass balustrade
        for (int i = 0; i < 22; i++)
        {
            float x0 = StairX0 + i * Tread, tt = (i + 1) * Riser;
            if (i == 21) x0 = StairX1 - Tread;
            MM(floorGf, x0, x0 + Tread + .02f, tt - .08f, tt, StairZ0, StairZ1, false);
        }
        float slope = Mathf.Atan2(FFL1, StairX1 - StairX0) * Mathf.Rad2Deg, run = Mathf.Sqrt(FFL1 * FFL1 + (StairX1 - StairX0) * (StairX1 - StairX0));
        b.Box(steel, new Vector3((StairX0 + StairX1) * .5f, FFL1 * .5f - .16f, StairZ1 + .02f), new Vector3(run, .3f, .04f), Quaternion.Euler(0f, 0f, slope));   // the steel stringer
        W.Rampa(new Vector3(StairX1, FFL1, (StairZ0 + StairZ1) * .5f), new Vector3(StairX0, 0f, (StairZ0 + StairZ1) * .5f), StairZ1 - StairZ0);
        b.Box(glass, new Vector3((StairX0 + StairX1) * .5f, FFL1 * .5f + .5f, StairZ0 - .02f), new Vector3(StairX1 - StairX0, 1.0f, .018f), Quaternion.Euler(0f, 0f, Mathf.Atan2(FFL1, StairX1 - StairX0) * Mathf.Rad2Deg));
        // the gallery over the stair's void (upper floor): glass on its open sides
        Balustrada(true, StairZ0 - .12f, StairX0 - .2f, StairX1 + .05f, FFL1);
        // ---- living: the media wall on the west wall, the sofa facing it, a rug, the table, a lamp; the view on the left
        var toE = Vector3.right;
        // the fireplace wall: grey stone floor to ceiling, a stone hearth bench, the linear fire, the 85" TV over it
        float fx = GX0 + T;
        MM(stoneIn, fx, fx + .12f, 0f, H0, -3.4f, 2.2f);
        MM(stoneIn, fx + .12f, fx + .62f, 0f, .40f, -3.4f, 2.2f);
        MM(black, fx + .12f, fx + .14f, .48f, .98f, -2.2f, 1.0f, false);
        MM(fire, fx + .14f, fx + .16f, .56f, .62f, -1.95f, .75f, false);
        W.Lampa(tag + " | kominek", new Vector3(fx + .8f, .7f, -.6f), .7f, 5f * K, new Color(1f, .55f, .22f));
        BraxKatalog.Tv(c, new Vector3(fx + .12f, 1.95f, -.6f), toE, 85f, nr % 4, false, tag + " salon TV");
        for (int i = 0; i < 3; i++) { W.Siedzisko(new Vector3(fx + .42f, .40f, -2.6f + i * 1.6f), toE, "ŁAWA PRZY KOMINKU"); Siedziska++; }
        BraxKatalog.Rug(c, new Vector3(-10.4f, 0f, -.6f), Vector3.forward, 4.4f, 3.8f, nr == 2);
        BraxKatalog.Sofa(c, new Vector3(-8.9f, .012f, -.6f), -toE, 3.2f, nr % 2, "sofa");
        BraxKatalog.FloorLamp(c, new Vector3(-7.6f, 0f, 1.7f));
        // dining for eight: two tables end to end, by the glass, on a rug
        BraxKatalog.Rug(c, new Vector3(1.5f, 0f, -2.75f), Vector3.forward, 4.6f, 3.0f, true);
        // a walnut credenza on the WC block under the big print
        b.RoundedBox(walnut, new Vector3(-4.25f, .38f, 5f - .06f - .23f), new Vector3(2.3f, .76f, .45f), .01f, Quaternion.identity, 1);
        b.Box(BraxKatalog.BlackMatt, new Vector3(-4.25f, .03f, 5f - .06f - .23f), new Vector3(2.2f, .06f, .4f), Quaternion.identity);
        W.Kolizja(new Vector3(-4.25f, .38f, 5f - .06f - .23f), new Vector3(2.3f, .76f, .45f), Quaternion.identity);
        BraxKatalog.DiningSet(c, new Vector3(.7f, .012f, -3.2f), Vector3.back, "krzesło");
        BraxKatalog.DiningSet(c, new Vector3(2.3f, .012f, -3.2f), Vector3.back, "krzesło");
        // the kitchen: a low run on the east wall (no wall units), the stone island with three stools
        BraxKatalog.Kitchen(c, new Vector3(GX1 - T - .002f, 0f, -5.4f), Vector3.forward, Vector3.left,
            new[] { "szuflady", "zlew", "zmywarka", "szuflady", "szuflady", "plyta", "szuflady", "piekarnik" }, walnut, BraxKatalog.BlackMatt, BraxKatalog.Stone, tag + " kuchnia", false);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(9.2f, .45f, -2.4f), new Vector3(3.0f, .87f, 1.0f), .01f, Quaternion.identity, 1);
        b.RoundedBox(BraxKatalog.Stone, new Vector3(9.2f, .895f, -2.4f), new Vector3(3.2f, .05f, 1.15f), .008f, Quaternion.identity, 2);
        W.Kolizja(new Vector3(9.2f, .46f, -2.4f), new Vector3(3.2f, .92f, 1.15f), Quaternion.identity);
        for (int i = 0; i < 3; i++)
        {
            var sp = new Vector3(8.2f + i * 1.0f, 0f, -1.35f);
            b.Cylinder(steel, sp, .025f, .7f, 10, Quaternion.identity, false);
            b.Cylinder(steel, sp, .2f, .015f, 20, Quaternion.identity, true);
            b.Cylinder(BraxKatalog.Leather, sp + Vector3.up * .66f, .2f, .07f, 20, Quaternion.identity, true);
            W.Siedzisko(sp + Vector3.up * .73f, Vector3.back, "HOKER"); Siedziska++;
        }
        // three pendants over the island, the ground floor's lights
        for (int i = 0; i < 3; i++)
        {
            var lp = new Vector3(8.2f + i * 1.0f, 1.75f, -2.4f);
            b.Tube(black, lp, new Vector3(lp.x, H0, lp.z), .004f, 4);
            b.Cylinder(BraxKatalog.BlackMatt, lp + Vector3.down * .2f, .14f, .2f, 20, Quaternion.identity, true, false, .05f);
        }
        foreach (var lp in new[] { new Vector3(-9f, H0 - .25f, -1f), new Vector3(1.5f, H0 - .25f, -3f), new Vector3(9.2f, 1.5f, -2.4f), new Vector3(-1f, H0 - .25f, 6.5f), new Vector3(-9f, H0 - .25f, 6.8f) })
            W.Lampa(tag + " | parter światło", lp, 1.0f, 9f * K, new Color(1f, .93f, .82f));
        // the hall: a walnut console with a mirror (live), opposite the door
        b.RoundedBox(walnut, new Vector3(-1.2f, .42f, GZ1 - T - .25f), new Vector3(1.8f, .84f, .45f), .01f, Quaternion.identity, 1);
        W.Kolizja(new Vector3(-1.2f, .42f, GZ1 - T - .25f), new Vector3(1.8f, .84f, .45f), Quaternion.identity);
        BraxChampionsMirror.Wall(tag + " hol lustro", new Vector3(-1.2f, 1.75f, GZ1 - T - .01f), Vector3.back, 1.2f, 1.4f, mirrorFrame);
        // the guest WC
        BraxKatalog.WcPan(c, new Vector3(-3.076f, 0f, 7.6f), Vector3.left);
        BraxKatalog.Place(c, "umywalka 0.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .6f, walnut, "umywalka"), new Vector3(-4.6f, 0f, GZ1 - T - .016f), Vector3.back, null);
        W.Lampa(tag + " | WC", new Vector3(-4.25f, H0 - .3f, 6.9f), .6f, 4f * K);
        // stone in the bathrooms (floors and walls to 2.6 m), the guest WC too
        Lazienka(9.06f, UX1 - T, 1.06f, UZ1 - T, FFL1, new Vector3(0f, 1.4f, 2.3f), 12.2f, 15.2f);
        Lazienka(-4.94f, 1.94f, 3.06f, UZ1 - T, FFL1, new Vector3(1f, -1.5f, -.6f), -4.4f, 1.4f);
        Lazienka(UX0 + T, -13.46f, 3.06f, UZ1 - T, FFL1, new Vector3(1f, -15.2f, -14.4f), -15.3f, -13.8f);
        Lazienka(-5.44f, -3.06f, 5.06f, GZ1 - T, 0f, new Vector3(1f, -4.95f, -4.1f), 0f, 0f);
        // the wine wall by the kitchen: a walnut cabinet, glass doors, 6 shelves of bottles, its warm light
        Out_(false);
        float wx0 = GX1 - T - .6f, wx1 = GX1 - T, wz0 = .4f, wz1 = 2.8f;
        MM(walnut, wx0, wx1, 0f, 2.45f, wz0, wz0 + .04f); MM(walnut, wx0, wx1, 0f, 2.45f, wz1 - .04f, wz1);
        MM(walnut, wx0, wx1, 0f, .1f, wz0 + .04f, wz1 - .04f); MM(walnut, wx0, wx1, 2.41f, 2.45f, wz0 + .04f, wz1 - .04f);
        MM(walnut, wx1 - .03f, wx1 - .002f, .1f, 2.41f, wz0 + .04f, wz1 - .04f, false);
        MM(glass, wx0 - .012f, wx0, .1f, 2.41f, wz0 + .04f, wz1 - .04f);
        for (int s = 0; s < 6; s++)
        {
            float sy = .32f + s * .35f;
            MM(oak, wx0 + .02f, wx1 - .03f, sy - .02f, sy, wz0 + .04f, wz1 - .04f, false);
            for (int k = 0; k < 10; k++) b.Cylinder(bottle, new Vector3(wx0 + .3f, sy, wz0 + .2f + k * .22f), .037f, .3f, 12, Quaternion.identity, true, false, .015f);
        }
        W.Lampa(tag + " | wino", new Vector3(wx0 - .3f, 1.3f, (wz0 + wz1) * .5f), .35f, 3f * K, new Color(1f, .82f, .55f));
        // the pantry: shelves
        for (int i = 0; i < 4; i++) MM(oak, 9.1f, 13.6f, .5f + i * .45f, .53f + i * .45f, GZ1 - T - .4f, GZ1 - T);

        // ---- upper floor: the corridor z 1..3, three bedrooms, the master suite
        float y = FFL1, top = Roof;
        Przegroda(true, 1f, UX0 + T, 2f, y, top, new Vector2(-9.0f, -8.1f), new Vector2(-1.5f, -.6f));   // bedrooms 2 / 3 to the corridor; the master's vestibule open
        Przegroda(false, -7f, UGlass + .05f, 1f, y, top);                                                 // bedroom 2 | 3
        Przegroda(false, 2f, UGlass + .05f, 1f, y, top);                                                  // bedroom 3 | master
        Przegroda(false, 2f, 1f, UZ1 - T, y, top, new Vector2(1.55f, 2.45f));                              // corridor | master vestibule
        Przegroda(true, 3f, -5f, 9f, y, top, new Vector2(-1.5f, -.6f), new Vector2(4.0f, 4.9f));          // bath 3 and the walk-in
        Przegroda(false, -5f, 3f, UZ1 - T, y, top);
        Przegroda(false, 9f, 1f, UZ1 - T, y, top, new Vector2(1.4f, 2.3f));                               // master bath door
        Przegroda(true, 1f, 9f, UX1 - T, y, top);                                                          // master bath | bedroom
        Przegroda(true, 3f, UX0 + T, -13.4f, y, top, new Vector2(-15.2f, -14.4f));                        // bath 2
        Przegroda(false, -13.4f, 3f, UZ1 - T, y, top);
        Drzwi("sypialnia 2", true, 1f, -9.0f, -8.1f, y); Drzwi("sypialnia 3", true, 1f, -1.5f, -.6f, y);
        Drzwi("apartament", false, 2f, 1.55f, 2.45f, y); Drzwi("łazienka 3", true, 3f, -1.5f, -.6f, y);
        Drzwi("garderoba", true, 3f, 4.0f, 4.9f, y); Drzwi("łazienka główna", false, 9f, 1.4f, 2.3f, y);
        Drzwi("łazienka 2", true, 3f, -15.2f, -14.4f, y);
        // bedrooms: the beds face the view (their foot to the glass)
        BraxKatalog.Rug(c, new Vector3(-11.4f, y, -1.6f), Vector3.forward, 2.8f, 2.6f, nr == 2);
        BraxKatalog.Rug(c, new Vector3(-2.5f, y, -1.6f), Vector3.forward, 2.8f, 2.6f, nr == 2);
        BraxKatalog.Rug(c, new Vector3(12.0f, y, -1.4f), Vector3.forward, 3.4f, 3.0f, true);
        BraxKatalog.Bed(c, new Vector3(-11.4f, y + .012f, -1.30f), Vector3.back, 1.6f, "łóżko");
        BraxKatalog.Bed(c, new Vector3(-2.5f, y + .012f, -1.30f), Vector3.back, 1.6f, "łóżko");
        BraxKatalog.Bed(c, new Vector3(12.0f, y + .012f, -1.30f), Vector3.back, 1.8f, "łóżko");
        BraxKatalog.Wardrobe(c, new Vector3(UX0 + T + .6f, y, -3.0f), Vector3.right, 2.4f, oak, BraxKatalog.Walnut, 2.4f, "szafa");
        BraxKatalog.Wardrobe(c, new Vector3(1.94f - .6f, y, -3.0f), Vector3.left, 2.4f, oak, BraxKatalog.Walnut, 2.4f, "szafa");
        BraxKatalog.Tv(c, new Vector3(UX1 - T, y + 1.35f, -2.2f), Vector3.left, 75f, (nr + 1) % 4, false, tag + " sypialnia TV");
        // the master's sitting corner at the glass (a sofa to the view, a table, a rug) and a desk on the partition
        BraxKatalog.Rug(c, new Vector3(4.8f, y, -4.7f), Vector3.forward, 3.0f, 2.4f, false);
        BraxKatalog.Sofa(c, new Vector3(4.8f, y + .012f, -4.3f), Vector3.back, 2.4f, (nr + 1) % 2, "sofa");
        BraxKatalog.CoffeeTable(c, new Vector3(4.8f, y + .012f, -5.6f), Vector3.back, 1.1f, .55f);
        Out_(false);
        b.RoundedBox(walnut, new Vector3(2.46f, y + .74f, -1.0f), new Vector3(.8f, .04f, 1.6f), .008f, Quaternion.identity, 2);
        foreach (float dz in new[] { -.74f, .74f }) b.Box(walnut, new Vector3(2.46f, y + .36f, -1.0f + dz), new Vector3(.76f, .72f, .04f), Quaternion.identity);
        W.Kolizja(new Vector3(2.46f, y + .38f, -1.0f), new Vector3(.8f, .76f, 1.6f), Quaternion.identity);
        BraxKatalog.Laptop(c, new Vector3(2.5f, y + .76f, -1.0f), Vector3.right, nr % 3);
        Siedz(Model("HotelSobczak/dining_chair_02/dining_chair_02_2k.gltf", "krzesło przy biurku", new Vector3(3.35f, y, -1.0f), -90f), new Vector3(3.35f, y + .47f, -1.0f), Vector3.left, "KRZESŁO");
        Model("HotelSobczak/desk_lamp_arm_01/desk_lamp_arm_01_1k.gltf", "lampka", new Vector3(2.3f, y + .76f, -1.6f), 90f);
        // the walk-in: wardrobes on both long walls
        BraxKatalog.Wardrobe(c, new Vector3(5.5f, y, UZ1 - T - .6f), Vector3.back, 5.6f, walnut, BraxKatalog.Walnut, 2.6f, "garderoba");
        // bathrooms: the master's double vanity, the free-standing tub at the window, the walk-in shower, the WC
        BraxKatalog.Place(c, "umywalka 1.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, 1.6f, walnut, "umywalka"), new Vector3(10.6f, y, UZ1 - T - .016f), Vector3.back, null);
        BraxChampionsMirror.Wall(tag + " łazienka lustro", new Vector3(10.6f, y + 1.6f, UZ1 - T - .022f), Vector3.back, 1.6f, 1.0f, mirrorFrame);
        Wanna(new Vector3(12.4f, y, 2.4f));
        Prysznic(new Vector3(UX1 - T - .7f, y, 5.0f));
        BraxKatalog.WcPan(c, new Vector3(9.076f, y, 6.6f), Vector3.right);
        BraxKatalog.WcPan(c, new Vector3(-4.924f, y, 6.6f), Vector3.right);
        BraxKatalog.Place(c, "umywalka 0.8 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .8f, walnut, "umywalka"), new Vector3(-.6f, y, UZ1 - T - .016f), Vector3.back, null);
        Prysznic(new Vector3(1.2f, y, 5.6f));
        BraxKatalog.WcPan(c, new Vector3(-13.476f, y, 6.8f), Vector3.left);
        BraxKatalog.Place(c, "umywalka 0.6 orzech", cc => BraxKatalog.Vanity(cc, Vector3.zero, Vector3.forward, .6f, walnut, "umywalka"), new Vector3(-14.6f, y, UZ1 - T - .016f), Vector3.back, null);
        foreach (var lp in new[] { new Vector3(-11.4f, top - .25f, -2.5f), new Vector3(-2.5f, top - .25f, -2.5f), new Vector3(9f, top - .25f, -2.5f),
                                   new Vector3(12.4f, top - .25f, 4.5f), new Vector3(-6f, top - .25f, 2f), new Vector3(5.5f, top - .25f, 5.4f), new Vector3(-1.5f, top - .25f, 5.4f) })
            W.Lampa(tag + " | piętro światło", lp, .9f, 8f * K, new Color(1f, .93f, .82f));
    }

    // a free-standing tub (white, its outside a soft oval; the filler in chrome)
    static void Wanna(Vector3 p)
    {
        var prof = new[] { new Vector2(.0f, .0f), new Vector2(.74f, .0f), new Vector2(.86f, .25f), new Vector2(.90f, .58f), new Vector2(.84f, .60f), new Vector2(.80f, .30f), new Vector2(.70f, .12f), new Vector2(.0f, .12f) };
        var oval = new Vector3(1f, 1f, .46f);
        var tub = new BraxChampionsMeshBatch(); tub.Lathe(porcelain, Vector3.zero, prof, 36, Quaternion.identity, false, false);
        var t = tub.Build(g, "łazienka | wanna wolnostojąca", Out + "/Meshes", false, 1f);
        t.localPosition = p; t.localScale = oval;
        W.Kolizja(p + new Vector3(0f, .3f, 0f), new Vector3(1.8f, .6f, .84f), Quaternion.identity);
        b.Tube(chrome, p + new Vector3(0f, 0f, .62f), p + new Vector3(0f, .95f, .62f), .02f, 10);
        b.Tube(chrome, p + new Vector3(0f, .95f, .62f), p + new Vector3(0f, .95f, .38f), .015f, 10);
    }
    // a walk-in shower: a glass screen, the rain head, a dark stone tray flush with the floor
    static void Prysznic(Vector3 p)
    {
        Out_(false);
        MM(BraxKatalog.StoneDark, p.x - .7f, p.x + .7f, p.y + .014f, p.y + .020f, p.z - .7f, p.z + .7f, false);
        MM(glass, p.x - .72f, p.x - .70f, p.y, p.y + 2.1f, p.z - .7f, p.z + .7f);
        b.Tube(chrome, p + new Vector3(.68f, 2.25f, 0f), p + new Vector3(.3f, 2.25f, 0f), .012f, 8);
        b.Cylinder(chrome, p + new Vector3(.2f, 2.22f, 0f), .15f, .02f, 24, Quaternion.identity, true, true);
    }

    // ================================================================ furnishing: Poly Haven pieces (real size), art, light
    // owner (osiedle audit 22:37): houses for sale must not be "pustki (kilka mebli, wielka pusta przestrzeń)" - every room
    // furnished to the hero's scale: seating groups, side tables, plants, art on the walls, lamps, the ceiling's downlights
    static void Wyposazenie(int nr)
    {
        string tag = Nazwy[nr - 1]; Out_(false); float y = FFL1;
        // ---- living: two armchairs facing each other across the round table, two lounge chairs at the glass, side tables
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-10.4f, .012f, -2.9f), 0f), new Vector3(-10.4f, .45f, -2.9f), Vector3.forward, "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel", new Vector3(-10.4f, .012f, 1.7f), 180f), new Vector3(-10.4f, .45f, 1.7f), Vector3.back, "FOTEL");
        Model("HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf", "stolik kawowy", new Vector3(-10.4f, .012f, -.6f), 0f);
        W.Kolizja(new Vector3(-10.4f, .2f, -.6f), new Vector3(1.0f, .4f, 1.0f), Quaternion.identity);
        foreach (float z in new[] { -2.5f, 1.3f }) Model("HotelSobczak/side_table_01/side_table_01_1k.gltf", "stolik boczny", new Vector3(-8.2f, .012f, z), 0f);
        foreach (float x in new[] { -11.2f, -8.6f })
            Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel przy szybie", new Vector3(x, 0f, -4.5f), 180f), new Vector3(x, .42f, -4.5f), Vector3.back, "FOTEL");
        Model("HotelSobczak/side_table_01/side_table_01_1k.gltf", "stolik przy szybie", new Vector3(-9.9f, 0f, -4.6f), 0f);
        Model("HotelSobczak/Ottoman_01/Ottoman_01_2k.gltf", "puf", new Vector3(-6.9f, 0f, 3.6f), 20f);
        // plants in the corners, by the stair, in the hall, by the kitchen
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(-13.1f, 0f, -5.3f), 30f);
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(-2.7f, 0f, -5.3f), 110f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-13.2f, 0f, 4.7f), 70f);
        Model("HotelSobczak/potted_plant_04/potted_plant_04_1k.gltf", "roślina", new Vector3(3.3f, 0f, 8.2f), 0f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(13.1f, 0f, 1.0f), 200f);
        Model("HotelSobczak/ceramic_vase_01/ceramic_vase_01_1k.gltf", "wazon", new Vector3(-1.7f, .84f, GZ1 - T - .25f), 0f);
        Model("HotelSobczak/ceramic_vase_02/ceramic_vase_02_1k.gltf", "wazon", new Vector3(fxHearth, .40f, 1.7f), 0f);
        // art: a large print on the WC block (to the living room), one by the kitchen, one in the hall
        Obrazek(nr, new Vector3(-4.25f, 1.75f, 5f - .06f), Vector3.back, 2.0f, 1.25f);
        Obrazek(nr + 1, new Vector3(11.3f, 1.75f, 5f - .06f), Vector3.back, 1.6f, 1.0f);
        Model("HotelSobczak/fancy_picture_frame_01/fancy_picture_frame_01_1k.gltf", "obraz w holu", new Vector3(5.5f, 1.6f, GZ1 - T - .03f), 180f, true);
        // two pendants over the dining table, one in the hall
        foreach (float x in new[] { .7f, 2.3f }) ModelTop("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", "lampa nad stołem", new Vector3(x, H0 - .9f, -2.75f), H0);
        ModelTop("HotelSobczak/modern_ceiling_lamp_01/modern_ceiling_lamp_01_1k.gltf", "lampa w holu", new Vector3(1.0f, H0 - 1.0f, 6.8f), H0);
        // the downlights of both ceilings (a 2.4 m grid, LED discs 4 mm under the plaster)
        for (float x = -12f; x <= 12.1f; x += 2.4f) for (float z = -4.8f; z <= 7.3f; z += 2.4f)
        {
            if (x < StairX1 + .3f && x > StairX0 - .5f && z > StairZ0 - .3f) continue;
            b.Cylinder(downlight, new Vector3(x, H0 - .007f, z), .065f, .003f, 16, Quaternion.identity, false, true);
        }
        for (float x = -14.4f; x <= 14.5f; x += 2.4f) for (float z = -5.4f; z <= 7.0f; z += 2.4f)
            b.Cylinder(downlight, new Vector3(x, Roof - .007f, z), .065f, .003f, 16, Quaternion.identity, false, true);
        // ---- upper floor: a lounge chair at the master's glass, armchairs in the bedrooms, prints over the beds, plants
        Siedz(Model("HotelSobczak/mid_century_lounge_chair/mid_century_lounge_chair_2k.gltf", "fotel w sypialni", new Vector3(14.5f, y, -5.0f), 200f), new Vector3(14.5f, y + .42f, -5.0f), new Vector3(-.34f, 0f, -.94f), "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w sypialni", new Vector3(-14.6f, y, -5.4f), 150f), new Vector3(-14.6f, y + .45f, -5.4f), new Vector3(.5f, 0f, -.87f), "FOTEL");
        Siedz(Model("ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "fotel w sypialni", new Vector3(-6.0f, y, -5.4f), 210f), new Vector3(-6.0f, y + .45f, -5.4f), new Vector3(-.5f, 0f, -.87f), "FOTEL");
        Obrazek(nr + 2, new Vector3(12.0f, y + 1.85f, 1f - .06f), Vector3.back, 2.2f, 1.1f);
        Obrazek(nr + 3, new Vector3(-11.4f, y + 1.85f, 1f - .06f), Vector3.back, 1.8f, 1.0f);
        Obrazek(nr + 4, new Vector3(-2.5f, y + 1.85f, 1f - .06f), Vector3.back, 1.8f, 1.0f);
        Obrazek(nr + 1, new Vector3(-12.0f, y + 1.6f, 1f + .06f), Vector3.forward, 1.6f, .9f);                 // the corridor
        Model("ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "roślina", new Vector3(15.1f, y, .2f), 0f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(-7.6f, y, -5.8f), 0f);
        Model("ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "roślina", new Vector3(1.3f, y, -5.8f), 0f);
        Model("HotelSobczak/potted_plant_04/potted_plant_04_1k.gltf", "roślina", new Vector3(15.1f, y, 7.1f), 0f);
    }
    static float fxHearth => GX0 + T + .4f;
    // a framed print (Poly Haven landscape photographs, the catalogue's) on a wall, the frame walnut
    static void Obrazek(int i, Vector3 c, Vector3 n, float w, float h)
    {
        var photo = BraxKatalog.Photo(i); n = n.normalized;
        bool alongX = Mathf.Abs(n.z) > .5f;
        b.Box(walnut, c + n * .015f, alongX ? new Vector3(w + .08f, h + .08f, .03f) : new Vector3(.03f, h + .08f, w + .08f), Quaternion.identity);
        if (!photo) return;
        // the picture 8 mm before the frame's face (a print on its stretcher), clockwise seen from the room (the viewer's right = n x up)
        var r = Vector3.Cross(n, Vector3.up).normalized * (w * .5f); var u = Vector3.up * (h * .5f); var p = c + n * .038f;
        b.QuadUV(photo, p - r - u, p - r + u, p + r + u, p + r - u, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
    }
    static void Siedz(GameObject o, Vector3 at, Vector3 f, string label)
    {
        if (!o) return;
        W.Siedzisko(at, f, label); Siedziska++;
        float y0 = at.y >= pietro ? pietro : 0f, h = Mathf.Max(.2f, at.y - y0);
        W.Kolizja(new Vector3(at.x, y0 + h * .5f, at.z), new Vector3(.75f, h, .75f), Quaternion.identity);
    }
    static GameObject Model(string path, string label, Vector3 at, float yaw, bool centre = false) => Place(path, label, at, yaw, centre ? 1 : 0, 0f);
    static GameObject ModelTop(string path, string label, Vector3 at, float ceilingY) => Place(path, label, at, 0f, 2, ceilingY);
    // snap 0: its bottom on at.y; 1: its centre on at; 2: its top under the ceiling
    static GameObject Place(string path, string label, Vector3 at, float yaw, int snap, float ceilingY)
    {
        string full = "Assets/Source/Mistrzowie/" + path;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(full);
        if (!source) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_MODEL_MISSING " + full); return null; }
        var go = PrefabUtility.InstantiatePrefab(source) as GameObject;
        if (!go) return null;
        go.name = "WILLA | " + label + " | Poly Haven CC0, real size";
        go.transform.SetParent(g, false);
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0) * go.transform.localRotation;
        go.transform.localScale = Vector3.one; go.transform.localPosition = at;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            go.transform.position += snap == 1 ? at - bb.center : snap == 2 ? Vector3.up * (ceilingY - bb.max.y) + new Vector3(at.x - bb.center.x, 0f, at.z - bb.center.z) : Vector3.up * (at.y - bb.min.y);
        }
        Modele++; return go;
    }
    // palms on the lawns (the region's own miasto_palmy, as the apartments' gardens)
    static void Palmy_()
    {
        var palm = BraxMiastoAssety.Model("miasto_palmy", "palma");
        if (!palm) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_PALMS_MISSING: miasto_palmy/palma"); return; }
        foreach (var p in new[] { new Vector2(-22.5f, -19f), new Vector2(-22.5f, -9f), new Vector2(-22.5f, 3f), new Vector2(-22.5f, 15f),
                                  new Vector2(25.5f, -19f), new Vector2(25.5f, -9f), new Vector2(25.5f, 3f), new Vector2(25.5f, 15f), new Vector2(-12f, 20f) })
        {
            var at = new Vector3(p.x, 0f, p.y);
            var o = BraxChampionsModel.Postaw(g, palm, "willa palma", at, (p.x * 11f + p.y * 5f) % 360f, 1f, false, true);
            if (!o) continue;
            o.transform.localScale = Vector3.one;                 // the group carries K
            BraxMiastoAssety.LodyZWezlow(o, new[] { .25f, .08f, .012f });
            BraxChampionsPalmTrunk.OnSpot(o, at);
            Palmy++;
        }
    }

    // ================================================================ the garden: loungers, the lounge, the sale board
    static void Ogrod(int nr)
    {
        Out_(true);
        // four loungers between the pool and the house, facing the view
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(-11.5f + i * 2.3f, 0f, -14.3f);
            b.RoundedBox(teak, p + new Vector3(0f, .17f, 0f), new Vector3(.72f, .08f, 2.0f), .02f, Quaternion.identity, 2);
            foreach (float sx in new[] { -.3f, .3f }) foreach (float sz in new[] { -.9f, .9f }) b.Box(teak, p + new Vector3(sx, .07f, sz), new Vector3(.05f, .14f, .05f), Quaternion.identity);
            b.RoundedBox(cushion, p + new Vector3(0f, .25f, -.25f), new Vector3(.68f, .08f, 1.45f), .03f, Quaternion.identity, 2);
            b.RoundedBox(cushion, p + new Vector3(0f, .48f, .72f), new Vector3(.68f, .08f, .62f), .03f, Quaternion.Euler(-48f, 0f, 0f), 2);
            W.Kolizja(p + new Vector3(0f, .15f, 0f), new Vector3(.72f, .3f, 2.0f), Quaternion.identity);
            W.Siedzisko(p + new Vector3(0f, .3f, 0f), Vector3.back, "LEŻAK", true); Siedziska++;
        }
        // the outdoor lounge under the cantilever: a teak sofa and two armchairs round a fire table
        var lc = new Vector3(14.5f, 0f, -9.5f);
        Lawka(lc + new Vector3(0f, 0f, 1.4f), Vector3.back, 2.4f); Lawka(lc + new Vector3(-1.8f, 0f, 0f), Vector3.right, .9f); Lawka(lc + new Vector3(1.8f, 0f, 0f), Vector3.left, .9f);
        b.RoundedBox(stone, lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), .02f, Quaternion.identity, 2);
        b.Box(BraxKatalog.StoneDark, lc + new Vector3(0f, .405f, 0f), new Vector3(1.0f, .01f, .4f), Quaternion.identity);
        W.Kolizja(lc + new Vector3(0f, .2f, 0f), new Vector3(1.4f, .4f, .8f), Quaternion.identity);
        // the terrace lights: low bollards along the pool, warm
        foreach (float x in new[] { -18f, 6f, 20f }) { b.Cylinder(black, new Vector3(x, 0f, -16.0f), .07f, .6f, 12, Quaternion.identity, true); W.Lampa(Nazwy[nr - 1] + " | taras", new Vector3(x, .7f, -15.8f), .5f, 6f * K, new Color(1f, .86f, .64f)); }
        // planters with olive trees by the glass
        if (nr != 2) foreach (float x in new[] { -20f, 22.5f }) { b.RoundedBox(graphite, new Vector3(x, .45f, -4f), new Vector3(1.2f, .9f, 1.2f), .03f, Quaternion.identity, 2); W.Kolizja(new Vector3(x, .45f, -4f), new Vector3(1.2f, .9f, 1.2f), Quaternion.identity); }
        // the sale board by the drive: white, on two black posts, 3D letters; AKCJA asks Monika
        var sb = new Vector3(-5.2f, 0f, PadZ1 - 2.5f);
        foreach (float sx in new[] { -.75f, .75f }) b.Box(black, sb + new Vector3(sx, .9f, 0f), new Vector3(.08f, 1.8f, .08f), Quaternion.identity);
        b.Box(signWhite, sb + new Vector3(0f, 1.45f, -.05f), new Vector3(1.8f, 1.0f, .04f), Quaternion.identity);
        b.Box(BraxKatalog.LedRed, sb + new Vector3(0f, 1.86f, -.0745f), new Vector3(1.8f, .14f, .004f), Quaternion.identity);   // 2.5 mm off the board
        BraxChampionsLetters3D.Text(b, black, black, "NA SPRZEDAŻ", sb + new Vector3(0f, 1.45f, -.072f), BraxChampionsLetters3D.YawFacing(Vector3.back), .15f, .01f, .1f, 1.6f);
        BraxChampionsLetters3D.Text(b, black, black, "WAWER NIERUCHOMOŚCI", sb + new Vector3(0f, 1.17f, -.072f), BraxChampionsLetters3D.YawFacing(Vector3.back), .07f, .006f, .1f, 1.6f);
        BraxChampionsLetters3D.Text(b, black, black, Nazwy[nr - 1], sb + new Vector3(0f, 1.01f, -.072f), BraxChampionsLetters3D.YawFacing(Vector3.back), .06f, .006f, .1f, 1.6f);
        W.Grupa = g;
        W.Akcja("willa_" + nr + "_sprzedaz", Nazwy[nr - 1] + " · NA SPRZEDAŻ",
            nr == 2 ? "MONIKA: Willa na własnej wyspie w zatoce, jak z Miami: szklany dom na dwa piętra, basen infinity ze spa, kort tenisowy, kryta jadalnia z barem, własny pomost z motorówką Riva i miejsce na mały jacht. Zapraszam do WAWER NIERUCHOMOŚCI na parterze T12!"
                    : "MONIKA: Ta willa jest na sprzedaż. Basen infinity, jacuzzi, garaż na dwa auta, apartament z widokiem na miasto. Zapraszam do WAWER NIERUCHOMOŚCI na parterze T12!",
            sb + new Vector3(0f, 0f, -1.2f));
        W.Kolizja(sb + new Vector3(0f, .9f, 0f), new Vector3(1.8f, 1.8f, .12f), Quaternion.identity);
    }
    static void Lawka(Vector3 p, Vector3 f, float w)
    {
        var q = Quaternion.LookRotation(f, Vector3.up);
        b.RoundedBox(teak, p + q * new Vector3(0f, .2f, -.35f), new Vector3(w, .4f, .8f), .02f, q, 2);
        b.RoundedBox(cushion, p + q * new Vector3(0f, .46f, -.38f), new Vector3(w - .1f, .12f, .7f), .04f, q, 2);
        b.RoundedBox(cushion, p + q * new Vector3(0f, .72f, -.72f), new Vector3(w - .1f, .45f, .12f), .04f, q, 2);
        W.Kolizja(p + q * new Vector3(0f, .35f, -.4f), new Vector3(w, .7f, .8f), q);
        int n = Mathf.Max(1, Mathf.RoundToInt(w / .8f));
        for (int i = 0; i < n; i++) { W.Siedzisko(p + q * new Vector3((i - (n - 1) * .5f) * .8f, .52f, -.25f), f, "SOFA OGRODOWA"); Siedziska++; }
    }
}
