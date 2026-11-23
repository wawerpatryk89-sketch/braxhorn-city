using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the three villas for sale on their own land (owner 17.11: "Na wzgórzu jedna, druga mała wyspa z willą.
// Trzecia gdzieś tam"; the villas themselves: BraxChampionsWille). Each site is a fine ground patch with its collider round
// the villa's pad, in world nominal metres (x K once), its terrain grass (the region's lawn) and sand (the beach's coast_sand):
//  - WILLA 1, THE HILL north of the arena A1: a south-facing ridge (16 m over the far land), the pad cut into it at 18 m with
//    the view over the city and the sea; a 7 m private road (asphalt, granite edging) leaves road Z935 by a street mouth
//    across its north sidewalk (layout.WILLE_MOUTHS), climbs the hill's west flank at 7 % and comes round to the gate;
//  - WILLA 2, THE ISLAND alone in the open sea ~420 m off the shore (owner 23.11 19:53: it stood in the harbour, the cruise
//    quay and a 289 m ship through it - "ta wyspa w ogóle ma być oddalona kawałek"), south of the beach's east end, clear
//    of every ship, quay and pier by ~300 m: a small island built round the pad (about 145 x 120 m at the water), a lawn
//    hump, a sand beach all round, a lagoon of white sand round it and a timber jetty to the north (the boats tie up at its
//    head); the villa looks north over the bay to the city. Its sea is the beach's real sea with its own shore field
//    (BraxChampionsWyspaMorze): the waves run in to its beach from every side;
//  - WILLA 3, THE SEAFRONT just east of the region: the pad 5 m over the sea behind its own beach, a private road from road
//    X1130 across its east sidewalk (street mouth), east and round to the gate.
// The far land (BraxChampionsHorizon) is pressed under the patches (Docisk) and the patches dip under it at their rims; the
// player stays on the collided ground: post-and-rail fences (with a 3 m invisible wall) close the land patches against the
// far land, the sea patches have a shelving sea bed with an invisible wall under water (its top 0.9 m under the surface:
// the boats pass over it).
public static class BraxChampionsWilleDzialki
{
    public const float SeaY = BraxChampionsHorizon.SeaLevel;                 // -2.0
    public const float RoadHalf = 3.5f, Edging = .25f, RimDip = 1.5f, RimBand = 25f, FenceInset = 30f;
    static float K => BraxClaudeOsiedle.K;
    const string Out = "Assets/PlayableGenerated/Mistrzowie";
    static string Folder => Out + "/Meshes/Wille";

    public enum Rodzaj { Wzgorze, Wyspa, Nabrzeze }
    public sealed class Dzialka
    {
        public int nr; public string nazwa; public Rodzaj rodzaj;
        public Vector2 srodek; public float yaw, padY, spad;               // the villa's origin, its turn, the pad top, the falloff length
        public float x0, x1, z0, z1;                                       // the patch (world nominal)
        public Vector2[] droga; public float[] drogaY;                     // the road: dense polyline and its heights
        public bool woda;                                                  // its edges in the sea (the underwater wall)
    }

    // ---------------------------------------------------------------- the three sites
    public static readonly Dzialka[] Dzialki = Utworz();
    static Dzialka[] Utworz()
    {
        var hill = new Dzialka { nr = 1, nazwa = "WZGÓRZE", rodzaj = Rodzaj.Wzgorze, srodek = new Vector2(650f, 1190f), yaw = 0f, padY = 24f, spad = 16f,
            x0 = 470f, x1 = 830f, z0 = 1000f, z1 = 1400f };
        // north from the street mouth (the sidewalk's far edge z 952), flat to the region's edge, 9.5 % up the west flank
        // (the hill roads of Los Angeles: 10-15 %), round (R 25) to the east, round (R 20) south into the gate at the pad's
        // back (local x 9)
        Droga(hill, new[] { Odc(600.5f, 952f, 600.5f, 1211f), Luk(625.5f, 1211f, 25f, 180f, 90f), Odc(625.5f, 1236f, 639f, 1236f), Luk(639f, 1216f, 20f, 90f, 0f) },
            new[] { (0f, .16f), (48f, .16f), (298f, 24f) }, 24f);
        // 23.11 (owner: the island stood on the cruise quay): out in the open sea, its patch wholly in the far sea south of the
        // region's own (z -500), ~70 m of water round the island inside it (the surf dies out before its rim) - the far sea
        // leaves a hole of exactly the patch for the island's water (BraxChampionsHorizon)
        var island = new Dzialka { nr = 2, nazwa = "WYSPA", rodzaj = Rodzaj.Wyspa, srodek = new Vector2(700f, -640f), yaw = 180f, padY = 3.5f, spad = 18f,
            x0 = 572f, x1 = 852f, z0 = -780f, z1 = -510f, woda = true };
        var shore = new Dzialka { nr = 3, nazwa = "NABRZEŻE", rodzaj = Rodzaj.Nabrzeze, srodek = new Vector2(1272f, -98f), yaw = 0f, padY = 3.0f, spad = 12f,
            x0 = 1200f, x1 = 1350f, z0 = -215f, z1 = 60f, woda = true };
        // east from the street mouth on road X1130 (the sidewalk's far edge x 1147), flat to the region's edge, 1.8 % up,
        // round (R 25) to the south and straight into the gate at the pad's back (local x 9)
        Droga(shore, new[] { Odc(1147f, 3.5f, 1256f, 3.5f), Luk(1256f, -21.5f, 25f, 90f, 0f), Odc(1281f, -21.5f, 1281f, -72f) },
            new[] { (0f, .16f), (53f, .16f), (198.5f, 3.0f) }, 3.0f);
        return new[] { hill, island, shore };
    }
    static List<Vector2> Odc(float ax, float az, float bx, float bz)
    {
        var l = new List<Vector2>(); var a = new Vector2(ax, az); var b = new Vector2(bx, bz); int n = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
        for (int i = 0; i <= n; i++) l.Add(Vector2.Lerp(a, b, i / (float)n)); return l;
    }
    static List<Vector2> Luk(float cx, float cz, float r, float a0, float a1)
    {
        var l = new List<Vector2>(); int n = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(a1 - a0) * Mathf.Deg2Rad * r));
        for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n) * Mathf.Deg2Rad; l.Add(new Vector2(cx + r * Mathf.Cos(a), cz + r * Mathf.Sin(a))); }
        return l;
    }
    // the road's points (duplicates at the joints removed) and its heights: linear between (length, height) control points
    static void Droga(Dzialka d, List<Vector2>[] czesci, (float s, float y)[] profil, float koniec)
    {
        var p = new List<Vector2>();
        foreach (var c in czesci) foreach (var v in c) if (p.Count == 0 || Vector2.Distance(p[p.Count - 1], v) > .05f) p.Add(v);
        var y = new float[p.Count]; float s = 0f;
        for (int i = 0; i < p.Count; i++)
        {
            if (i > 0) s += Vector2.Distance(p[i - 1], p[i]);
            y[i] = koniec;
            for (int k = 0; k + 1 < profil.Length; k++)
                if (s <= profil[k + 1].s) { y[i] = Mathf.Lerp(profil[k].y, profil[k + 1].y, Mathf.InverseLerp(profil[k].s, profil[k + 1].s, s)); break; }
        }
        d.droga = p.ToArray(); d.drogaY = y;
    }
    public static float DlugoscDrogi(Dzialka d) { float s = 0f; for (int i = 1; i < d.droga.Length; i++) s += Vector2.Distance(d.droga[i - 1], d.droga[i]); return s; }

    // ---------------------------------------------------------------- the ground (world nominal)
    static float Smooth(float a, float b, float v) { float t = Mathf.Clamp01((v - a) / (b - a)); return t * t * (3 - 2 * t); }
    public static Vector2 Lokalnie(Dzialka d, float x, float z)
    {
        float dx = x - d.srodek.x, dz = z - d.srodek.y, c = Mathf.Cos(d.yaw * Mathf.Deg2Rad), s = Mathf.Sin(d.yaw * Mathf.Deg2Rad);
        return new Vector2(dx * c - dz * s, dx * s + dz * c);
    }
    // distance from the pad's rectangle (0 on and in it), in the villa's local metres
    static float OdPadu(Dzialka d, Vector2 l)
    {
        var p = BraxChampionsWille.Pad(d.nr);                                       // each villa its own pad (17.11)
        float ex = Mathf.Max(p.x - l.x, l.x - p.y, 0f), ez = Mathf.Max(p.z - l.y, l.y - p.w, 0f);
        return Mathf.Sqrt(ex * ex + ez * ez);
    }
    public static bool PodPadem(Dzialka d, float x, float z)
    {
        var l = Lokalnie(d, x, z);
        var p = BraxChampionsWille.Pad(d.nr);
        return l.x > p.x && l.x < p.y && l.y > p.z && l.y < p.w;
    }
    // the ground at the pad's level: its back and the back of its sides 5 cm under the lawn, its front 3.9 m under it (the
    // retaining wall's foot at -4 buried 10 cm), the sides falling between over the front 8 m (the side walls: Sciany)
    public const float Przod = -3.9f;
    public const float WyspaDno = 20f;                                         // the island's sea bed at its patch's rim (m under the sea)
    // the hill villa after the owner's photo (17.11 "perched on a scenic cliff"): its pool terrace on a 9 m rock face (the
    // villa builds the face, BraxChampionsWillaKlif)
    public const float PrzodKlif = -9f;
    /// <summary>The ground beside villa nr's pad at its local z, under the pad's top.</summary>
    public static float Uskok(int nr, float lz)
    {
        float z0 = BraxChampionsWille.Pad(nr).z;
        return Mathf.Lerp(nr == 1 ? PrzodKlif : Przod, -.05f, Smooth(z0, z0 + 8f, lz));
    }
    static float PrzyPadzie(Dzialka d, float lz) => d.padY + Uskok(d.nr, lz);
    // the site's own land, away from the pad
    static float Podloze(Dzialka d, float x, float z, float odPadu)
    {
        switch (d.rodzaj)
        {
            case Rodzaj.Wzgorze:
            {
                // a south-facing ridge 22 m over the far land: up from the region's edge to its crest behind the pad, down
                // north and to the sides; a little unevenness of real ground
                float s = z <= 1260f ? Smooth(1000f, 1260f, z) : 1f - Smooth(1260f, 1370f, z);
                float l = 1f - Smooth(80f, 170f, Mathf.Abs(x - 650f));
                float n = .85f + .3f * BraxChampionsHorizon.Fbm(x / 90f + 2.7f, z / 90f - 4.1f, 3);
                return BraxChampionsHorizon.FarHeight(x, z) + 22f * s * l * n;
            }
            case Rodzaj.Wyspa:
            {
                // the island round the pad (23.11, a private island's shore): the lawn ends 1 m over the sea, a 14 m beach to
                // the water (7 %), round it a lagoon of white sand - 1.2 m deep 16 m out, 2.2 m 44 m out: the clear turquoise
                // water over sand - and its edge dropping to the open sea's depth (15 m 71 m out, 20 m farther: the far sea's
                // deep blue at the patch's rim); its shore line wanders +-7 m (a real island is not the pad's rectangle)
                float e = odPadu - (4f + d.spad) + 14f * (BraxChampionsHorizon.Fbm(x / 38f + 5.3f, z / 38f - 2.9f, 3) - .5f);
                if (e <= 0f) return SeaY + 1f;
                if (e <= 14f) return Mathf.Lerp(SeaY + 1f, SeaY, e / 14f);
                if (e <= 30f) return Mathf.Lerp(SeaY, SeaY - 1.2f, (e - 14f) / 16f);
                if (e <= 58f) return Mathf.Lerp(SeaY - 1.2f, SeaY - 2.2f, Smooth(30f, 58f, e));
                if (e <= 85f) return Mathf.Lerp(SeaY - 2.2f, SeaY - 15f, Smooth(58f, 85f, e));
                return Mathf.Lerp(SeaY - 15f, SeaY - WyspaDno, Smooth(85f, 130f, e));
            }
            default:
            {
                // the coast: the far land inland, a 30 m beach to the water at the coast line, the shelf, the deep
                float dc = z - BraxChampionsHorizon.Coast(x);
                if (dc >= 30f) return Mathf.Max(BraxChampionsHorizon.FarHeight(x, z), SeaY + .9f);
                if (dc >= 0f) return Mathf.Lerp(SeaY, Mathf.Max(BraxChampionsHorizon.FarHeight(x, z), SeaY + .9f), dc / 30f);
                if (dc >= -25f) return Mathf.Lerp(SeaY, SeaY - 3.5f, Smooth(0f, 25f, -dc));
                return Mathf.Lerp(SeaY - 3.5f, SeaY - 10f, Smooth(25f, 70f, -dc));
            }
        }
    }
    // the road nearest (x,z): its height and the distance from its centre line
    static float Droga(Dzialka d, float x, float z, out float od)
    {
        od = float.MaxValue; float y = 0f; if (d.droga == null) return 0f;
        var p = new Vector2(x, z);
        for (int i = 0; i + 1 < d.droga.Length; i++)
        {
            Vector2 a = d.droga[i], b = d.droga[i + 1], ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-6f, ab.sqrMagnitude));
            float dd = Vector2.Distance(p, a + ab * t);
            if (dd < od) { od = dd; y = Mathf.Lerp(d.drogaY[i], d.drogaY[i + 1], t); }
        }
        return y;
    }
    static float DoKrawedzi(Dzialka d, float x, float z) => Mathf.Min(Mathf.Min(x - d.x0, d.x1 - x), Mathf.Min(z - d.z0, d.z1 - z));
    /// <summary>The site's ground at (x,z), world nominal: under the pad deep (the villa's slabs and its pool cover it).</summary>
    public static float Grunt(Dzialka d, float x, float z)
    {
        var l = Lokalnie(d, x, z);
        if (PodPadem(d, x, z))
        {
            // under the villa's slabs (bottoms at -.3): just under them, and under its front deep (the retaining wall's
            // foot, the trough); under the pool's shell 2.2 m down
            var pool = BraxChampionsWille.StrefaBasenu(d.nr);                 // each villa its own pool (owner 17.11)
            // (and at the front edge as deep as the ground before it: a vertex on the edge itself, rounded inside, must not
            // lift a grass slope over the cliff's foot - 17.11, the hill's 9 m rock face)
            if (l.x > pool.x - .6f && l.x < pool.y + .6f && l.y < pool.z + .6f) return Mathf.Min(d.padY - 2.2f, PrzyPadzie(d, l.y) - .3f);
            return Mathf.Min(PrzyPadzie(d, l.y) - .3f, d.padY - .35f);
        }
        float od = OdPadu(d, l);
        float h = Mathf.Lerp(PrzyPadzie(d, l.y), Podloze(d, x, z, od), Smooth(4f, 4f + d.spad, od));
        if (!d.woda || d.rodzaj == Rodzaj.Nabrzeze) h -= RimDip * (1f - Smooth(0f, RimBand, DoKrawedzi(d, x, z)));       // under the far land at the rim
        float y = Droga(d, x, z, out float dr);
        if (dr < RoadHalf + 1f + 8f) h = Mathf.Lerp(y - .12f, h, Smooth(0f, 8f, dr - RoadHalf - 1f));                 // the road's bed, 1:2 banks
        return h;
    }
    /// <summary>For BraxChampionsHorizon.Height: under a land patch (inside its rim band) the far land goes down 1 m under it.</summary>
    public static float Docisk(float x, float z)
    {
        float m = float.PositiveInfinity;
        foreach (var d in Dzialki)
        {
            if (d.rodzaj == Rodzaj.Wyspa) continue;                                                   // in the region's own sea
            if (x <= d.x0 || x >= d.x1 || z <= d.z0 || z >= d.z1) continue;
            // inside the rim band the far land covers the dipped rim, except along the road (its coarse triangles are
            // ~15 x 28 m there: 35 m either side keeps them under the road's cut)
            Droga(d, x, z, out float dr);
            if (DoKrawedzi(d, x, z) < RimBand && dr > 35f) continue;
            m = Mathf.Min(m, Grunt(d, x, z) - 1f);
        }
        return m;
    }

    // ---------------------------------------------------------------- the build
    public static int Zbudowane, Kolizje, Ogrodzenie;
    static Material trawa, piasek, asfalt, granit, chodnik, krawez, drewno, kamien, slupek;
    static void Materialy()
    {
        trawa = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/grass.mat");
        if (!trawa) trawa = BraxChampionsFoundation.LibraryAcg("wille_dzialka_trawa", "Grass004", Color.white, 1.4f);
        piasek = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/sand_dry.mat");
        if (!piasek) piasek = BraxChampionsFoundation.SourceMaterial("wille_dzialka_piasek", "coast_sand_02", 15f, new Color(1.59f, 1.68f, 1.81f));
        asfalt = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/asphalt.mat");
        if (!asfalt) asfalt = BraxChampionsFoundation.SourceMaterial("wille_droga_asfalt", "clean_asphalt", 2.1f, Color.white);
        chodnik = AssetDatabase.LoadAssetAtPath<Material>(Out + "/Materials/paving.mat");
        if (!chodnik) chodnik = BraxChampionsFoundation.SourceMaterial("wille_droga_chodnik", "concrete_pavement_02", 1.8f, Color.white);
        if (!BraxChampionsPromenadeFurniture.Teak) BraxChampionsPromenadeFurniture.Materials();
        krawez = BraxChampionsPromenadeFurniture.Kerb; drewno = BraxChampionsPromenadeFurniture.Teak;
        granit = BraxChampionsFoundation.SourceMaterial("wille_droga_granit", "granite_tile_03", 1.6f, new Color(.80f, .82f, .86f));
        kamien = BraxChampionsFoundation.LibraryTravertine("wille_mur_kamien_trawertyn", new Color(.88f, .85f, .80f), 1.2f);   // 18.11 light travertine (was dark granite tiles)
        slupek = BraxChampionsHarbour.Surface("wille_ogrodzenie_grafit", new Color(.16f, .17f, .18f), .45f, .6f);
    }

    /// <summary>The three sites (under parent, or at the scene root): ground, roads, walls, fences, jetty and the villa on each.</summary>
    public static void Zbuduj(Transform parent)
    {
        Materialy();
        System.IO.Directory.CreateDirectory(Folder);
        var root = new GameObject("MIASTO MISTRZÓW | WILLE NA SPRZEDAŻ | wzgórze, wyspa, nabrzeże (owner 17.11)").transform;
        root.SetParent(parent, false); root.position = Vector3.zero;
        Zbudowane = Kolizje = Ogrodzenie = 0;
        foreach (var d in Dzialki) Jedna(root, d);
        BraxChampionsHorizon.Docisk = Docisk;                                         // the far land goes under the land patches
        Debug.Log("MISTRZOWIE_WILLE_DZIALKI sites=" + Zbudowane + " colliders=" + Kolizje + " fence_m=" + Ogrodzenie + " far_land_pressed=true");
    }
    /// <summary>One site: its ground, road, walls, fence or jetty, and its villa (the preview builds them one by one).</summary>
    public static Transform Jedna(Transform root, Dzialka d)
    {
        if (!trawa) Materialy();
        {
            var g = new GameObject("WILLA " + d.nr + " | działka: " + d.nazwa).transform; g.SetParent(root, false);
            int k0 = W.Kolizje;
            W.Start(g); W.Grupa = g; W.Wnetrze = false;
            var b = new BraxChampionsMeshBatch();
            Teren(g, d);
            if (d.droga != null) DrogaMesh(g, b, d);
            Sciany(b, d);
            Zielen(g, d);
            if (d.rodzaj != Rodzaj.Wyspa) Plot(b, d);
            if (d.woda) MurPodWoda(d);
            if (d.rodzaj == Rodzaj.Wyspa) { Pomost(b, d); Lodz(g, d); BraxChampionsWyspaMorze.Zbuduj(g, d); PlazaWyspy(b, d); }
            b.Build(g, "WILLA " + d.nr + " | działka " + d.nazwa + " | detale", Folder, true, K);
            W.Zapisz("mistrzowie_wille_dzialka_" + d.nr);
            Kolizje += W.Kolizje - k0;
            BraxChampionsWille.Willa(g, d.nr, new Vector3(d.srodek.x, d.padY, d.srodek.y), d.yaw);
            Zbudowane++;
            Debug.Log("MISTRZOWIE_WILLA_DZIALKA " + d.nr + " " + d.nazwa + " centre=" + d.srodek + " pad=" + d.padY + " yaw=" + d.yaw +
                      " patch=" + (d.x1 - d.x0) + "x" + (d.z1 - d.z0) + " road=" + (d.droga != null ? DlugoscDrogi(d).ToString("F0") + "m" : "boat"));
            return g;
        }
    }

    // the ground: a 2 m grid (2.5 m on the hill), grass over the beach line, sand under it; its mesh is its collider
    static void Teren(Transform g, Dzialka d)
    {
        float krok = d.rodzaj == Rodzaj.Wzgorze ? 2.5f : 2f;
        // a regular grid plus lines on the pad's edges and 10 cm inside them (the villa's walls stand there): no cell
        // straddles an edge, so the ground meets the retaining walls' feet and goes under the slabs cleanly
        var xs = Siatka(d.x0, d.x1, krok, PadKrawedzie(d, true)); var zs = Siatka(d.z0, d.z1, krok, PadKrawedzie(d, false));
        int nx = xs.Count - 1, nz = zs.Count - 1;
        var v = new Vector3[(nx + 1) * (nz + 1)]; var uv = new Vector2[v.Length];
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float x = xs[i], z = zs[j];
                v[i * (nz + 1) + j] = new Vector3(x, Grunt(d, x, z), z) * K; uv[i * (nz + 1) + j] = new Vector2(x, z);
            }
        var tg = new List<int>(); var ts = new List<int>();
        var vl = new List<Vector3>(v); var uvl = new List<Vector2>(uv);
        float plaza = SeaY + .75f; bool piach = d.rodzaj != Rodzaj.Wzgorze;
        // 24.11 the seafront's private beach (owner: "I mają mieć prywatną plażę"): sand from 4.4 m before the pad's front (the
        // beach stair's foot on it) to the sea between the beach's two stone walls (BraxChampionsWille.N3Plaza), not only under
        // the height line; the sand / grass field the lower of the two, cut along its zero as before
        float Pole(Vector3 v)
        {
            float f = v.y / K - plaza;
            if (d.rodzaj == Rodzaj.Nabrzeze)
            {
                var l = Lokalnie(d, v.x / K, v.z / K);
                if (l.x > BraxChampionsWille.N3PlazaX0 && l.x < BraxChampionsWille.N3PlazaX1) f = Mathf.Min(f, l.y - BraxChampionsWille.N3PlazaZ);
            }
            return f;
        }
        // the grass / sand line follows the height exactly: a triangle across it is cut on it (the cut points shared by the
        // triangles either side, so the ground stays one closed surface)
        var ciecia = new Dictionary<long, int>();
        int Ciecie(int p, int q)
        {
            long key = p < q ? (long)p * 4000000L + q : (long)q * 4000000L + p;
            if (ciecia.TryGetValue(key, out int r)) return r;
            float hp = Pole(vl[p]), hq = Pole(vl[q]), t = Mathf.Clamp01(hp / (hp - hq));
            vl.Add(Vector3.Lerp(vl[p], vl[q], t)); uvl.Add(Vector2.Lerp(uvl[p], uvl[q], t));
            return ciecia[key] = vl.Count - 1;
        }
        void Trojkat(int p0, int p1, int p2)
        {
            bool g0 = !piach || Pole(vl[p0]) >= 0f, g1 = !piach || Pole(vl[p1]) >= 0f, g2 = !piach || Pole(vl[p2]) >= 0f;
            if (g0 == g1 && g1 == g2) { var l = g0 ? tg : ts; l.Add(p0); l.Add(p1); l.Add(p2); return; }
            // the lone vertex first, the order (and so the winding) kept
            int A, B, C; bool gA;
            if (g0 != g1 && g0 != g2) { A = p0; B = p1; C = p2; gA = g0; }
            else if (g1 != g0 && g1 != g2) { A = p1; B = p2; C = p0; gA = g1; }
            else { A = p2; B = p0; C = p1; gA = g2; }
            int ab = Ciecie(A, B), ac = Ciecie(A, C);
            var la = gA ? tg : ts; var lo = gA ? ts : tg;
            la.Add(A); la.Add(ab); la.Add(ac);
            lo.Add(ab); lo.Add(B); lo.Add(C); lo.Add(ab); lo.Add(C); lo.Add(ac);
        }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                int a = i * (nz + 1) + j, bq = (i + 1) * (nz + 1) + j, c = a + 1, e = bq + 1;
                if (PodPadem(d, xs[i] + .001f, zs[j] + .001f) && PodPadem(d, xs[i + 1] - .001f, zs[j + 1] - .001f) &&
                    PodPadem(d, xs[i] + .001f, zs[j + 1] - .001f) && PodPadem(d, xs[i + 1] - .001f, zs[j] + .001f)) continue;   // under the villa: nothing
                // x east, z north: (i,j) -> (i,j+1) -> (i+1,j) faces up
                Trojkat(a, c, bq); Trojkat(c, e, bq);
            }
        // the normals from the ground itself (one-sided across the walls' steps): no seam on the grass / sand line
        var nrm = new Vector3[vl.Count];
        for (int i = 0; i < vl.Count; i++)
        {
            float x = vl[i].x / K, z = vl[i].z / K, h = vl[i].y / K, ep = .5f;
            float D(float a1, float a2) => Mathf.Abs(a1) < Mathf.Abs(a2) ? a1 : a2;
            float gx = D((Grunt(d, x + ep, z) - h) / ep, (h - Grunt(d, x - ep, z)) / ep), gz = D((Grunt(d, x, z + ep) - h) / ep, (h - Grunt(d, x, z - ep)) / ep);
            nrm[i] = new Vector3(-gx, 1f, -gz).normalized;
        }
        // two meshes (grass, sand) over the same vertices, each its own collider
        foreach (var (tri, mat, nazwa) in new[] { (tg, trawa, "trawa"), (ts, piasek, "piasek") })
        {
            if (tri.Count == 0) continue;
            var mesh = new Mesh { name = "wille dzialka " + d.nr + " " + nazwa, indexFormat = vl.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = vl.ToArray(); mesh.uv = uvl.ToArray(); mesh.SetTriangles(tri, 0); mesh.normals = nrm;
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            string path = Folder + "/wille_dzialka_" + d.nr + "_" + nazwa + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject("WILLA " + d.nr + " | grunt: " + nazwa + " | kolizja"); go.transform.SetParent(g, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }
    }
    // the pad's edges in world x (or z), and 10 cm inside each (the sites turn the villa by 0 or 180 degrees)
    static List<float> PadKrawedzie(Dzialka d, bool wX)
    {
        var l = new List<float>(); float c = wX ? d.srodek.x : d.srodek.y, sg = Mathf.Abs(d.yaw - 180f) < 1f ? -1f : 1f;
        var pd = BraxChampionsWille.Pad(d.nr); float a = wX ? pd.x : pd.z, b = wX ? pd.y : pd.w;
        foreach (var (e, inward) in new[] { (a, 1f), (b, -1f) }) { l.Add(c + sg * e); l.Add(c + sg * (e + inward * .1f)); }
        return l;
    }
    static List<float> Siatka(float a, float b, float krok, List<float> extra)
    {
        var l = new List<float>(); for (float t = a; t < b - 1e-3f; t += krok) l.Add(t); l.Add(b);
        foreach (var e in extra) if (e > a && e < b) l.Add(e);
        l.Sort(); for (int i = l.Count - 1; i > 0; i--) if (l[i] - l[i - 1] < .05f) { if (extra.Contains(l[i - 1])) l.RemoveAt(i); else l.RemoveAt(i - 1); }
        return l;
    }

    // the road: 6.5 m of asphalt between granite edging stones, its own collider; the street mouth on the city road
    static void DrogaMesh(Transform g, BraxChampionsMeshBatch b, Dzialka d)
    {
        var p = d.droga; int n = p.Length;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float s = 0f, hw = RoadHalf - Edging;
        for (int i = 0; i < n; i++)
        {
            if (i > 0) s += Vector2.Distance(p[i - 1], p[i]);
            var dir = Kierunek(p, i); var nn = new Vector2(-dir.y, dir.x);
            foreach (float side in new[] { -1f, 1f })
            {
                var q = p[i] + nn * side * hw; v.Add(new Vector3(q.x, d.drogaY[i], q.y) * K); uv.Add(new Vector2(side * hw, s));
            }
            if (i > 0)
            {
                int a = (i - 1) * 2, c = i * 2;
                // left (-n) to right (+n) and on along the road: wound so the face is up
                Tri(v, t, a, a + 1, c); Tri(v, t, a + 1, c + 1, c);
            }
            // edging stones every 1 m on both sides
            if (i > 0)
                foreach (float side in new[] { -1f, 1f })
                {
                    var c0 = p[i - 1] + nn * side * (hw + Edging * .5f); var c1 = p[i] + nn * side * (hw + Edging * .5f);
                    var mid = (c0 + c1) * .5f; float yy = (d.drogaY[i - 1] + d.drogaY[i]) * .5f, len = Vector2.Distance(c0, c1), dy = d.drogaY[i] - d.drogaY[i - 1];
                    float yaw = Mathf.Atan2(c1.x - c0.x, c1.y - c0.y) * Mathf.Rad2Deg, pitch = -Mathf.Atan2(dy, len) * Mathf.Rad2Deg;
                    // a low kerb 8 cm over the asphalt, laid with the road's grade (stone to stone, 6 mm joints)
                    b.Box(granit, new Vector3(mid.x, yy - .06f, mid.y), new Vector3(Edging, .28f, Mathf.Max(.02f, Mathf.Sqrt(len * len + dy * dy) - .006f)), Quaternion.Euler(pitch, yaw, 0f));
                }
        }
        var mesh = new Mesh { name = "wille droga " + d.nr }; mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
        mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        string path = Folder + "/wille_droga_" + d.nr + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject("WILLA " + d.nr + " | droga (asfalt) | kolizja"); go.transform.SetParent(g, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = asfalt;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
        // the street mouth across the city sidewalk (layout.WILLE_MOUTHS): north for the hill, east for the seafront
        var m = Mouth(d);
        BraxChampionsDriveMouth.Build(g, m, asfalt, chodnik, krawez);
    }
    // the road's direction at point i: the central chord inside, at the two ends the tangent (1.5 x the last chord - 0.5 x the
    // one before: exact on an arc), so the end square to the road meets the gate's paving edge to edge (the hill's road ends on
    // an arc: its last chord 1.4 deg off would push an 8 cm sliver of asphalt over the drive's paving)
    static Vector2 Kierunek(Vector2[] p, int i)
    {
        int n = p.Length;
        if (n < 3 || (i > 0 && i < n - 1)) return (p[Mathf.Min(i + 1, n - 1)] - p[Mathf.Max(i - 1, 0)]).normalized;
        Vector2 c1 = i == 0 ? p[1] - p[0] : p[n - 1] - p[n - 2], c2 = i == 0 ? p[2] - p[1] : p[n - 2] - p[n - 3];
        return (1.5f * c1.normalized - .5f * c2.normalized).normalized;
    }
    static void Tri(List<Vector3> v, List<int> t, int a, int b, int c)
    {
        if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).y < 0f) { t.Add(a); t.Add(c); t.Add(b); } else { t.Add(a); t.Add(b); t.Add(c); }
    }
    public static BraxChampionsDriveMouth.Spec Mouth(Dzialka d)
    {
        if (d.rodzaj == Rodzaj.Wzgorze)
            return new BraxChampionsDriveMouth.Spec { id = "WILLA 1 street mouth (Z935 north sidewalk)", alongX = true, edge = 945f, dir = 1f,
                u0 = 597f, u1 = 604f, R = 3f, depth = 7f, topEnd = .16f, unit = K, assetDir = Folder };
        return new BraxChampionsDriveMouth.Spec { id = "WILLA 3 street mouth (X1130 east sidewalk)", alongX = false, edge = 1140f, dir = 1f,
            u0 = 0f, u1 = 7f, R = 3f, depth = 7f, topEnd = .16f, unit = K, assetDir = Folder };
    }

    // stone retaining walls along the pad's sides where the ground falls to the front (the villa has the front one)
    static void Sciany(BraxChampionsMeshBatch b, Dzialka d)
    {
        if (d.nr == 1) return;                                                  // the hill: the villa's own rock faces
        var pd = BraxChampionsWille.Pad(d.nr); float z0 = pd.z, z1 = z0 + 8.6f, y0 = d.padY - 4.6f, y1 = d.padY;
        var mur = d.rodzaj == Rodzaj.Wyspa ? BraxChampionsWille.KamienMuruWyspy() : kamien;     // 23.11 the island: Stone Wall 04 (owner's list)
        if (d.rodzaj == Rodzaj.Nabrzeze) mur = BraxChampionsWille.MurStaryNabrzeza();            // 24.11 the seafront: old stone ("stare, te mury")
        var q = Quaternion.Euler(0f, d.yaw, 0f); var c = new Vector3(d.srodek.x, 0f, d.srodek.y);
        foreach (float x in new[] { pd.x - .15f, pd.y + .15f })
        {
            var p = c + q * new Vector3(x, 0f, (z0 + z1) * .5f); p.y = (y0 + y1) * .5f;
            b.Box(mur, p, new Vector3(.3f, y1 - y0, z1 - z0), q);
            W.Kolizja(p * K, new Vector3(.3f, y1 - y0, z1 - z0) * K, q);
        }
    }

    // owner's photo of the cliff villa (17.11): the hill round it green - trees (the region's drzewo_verdant, as its streets)
    // and palms on its slopes beside, behind and under the pad (the villa plants its own garden on the pad), on the
    // site's ground, never on the road
    public static int ZielenPalmy, ZielenDrzewa;
    static void Zielen(Transform g, Dzialka d)
    {
        // the hill: a garden of trees and palms; the island (his Miami photo): palms, a few trees, on its grass only, clear of
        // the way from the stair to the jetty; 24.11 the seafront (owner 00:00 "do willi palmy"): an avenue of thick palms
        // along its drive as the hill's, palms on its lawns round the pad (none on the beach: the villa plants the beach's own)
        bool hill = d.rodzaj == Rodzaj.Wzgorze, shore = d.rodzaj == Rodzaj.Nabrzeze;
        int maxP = hill ? 26 : shore ? 24 : 22, maxD = hill ? 40 : 8; float maxOd = hill ? 27f : shore ? 30f : 22f;
        var ps = PomostStart(d);
        var palm = BraxMiastoAssety.Model("miasto_palmy", "palma");
        if (!palm) BraxBuildQuality.Warn("DO POPRAWKI WILLA_HILL_PALMS_MISSING: miasto_palmy/palma");
        var tree = BraxRegion.Model("drzewo_verdant");
        if (!tree) BraxBuildQuality.Warn("DO POPRAWKI WILLA_HILL_TREES_MISSING: drzewo_verdant");
        var pd = BraxChampionsWille.Pad(d.nr); var rnd = new System.Random(hill ? 1190 : shore ? 770 : 400);
        float R(float a, float c) => a + (float)rnd.NextDouble() * (c - a);
        float sn = Mathf.Sin(d.yaw * Mathf.Deg2Rad), cs = Mathf.Cos(d.yaw * Mathf.Deg2Rad);
        var placed = new List<Vector2>();
        int palmy = 0, drzewa = 0;
        // 23.11 (owner 22:22: "dojazd do willi, te grube palmy po obu stronach drogi. Ładny dojazd, piękny"): the hill's drive an
        // avenue - thick palms both sides every 12 m, 2.4 m off its edge, from the street to the pad
        if (hill) Aleja = 0; else if (shore) AlejaNabrzeze = 0;
        int aleja = 0;
        if ((hill || shore) && d.droga != null && palm)
        {
            float next = 10f, s = 0f, off = RoadHalf + 2.4f;
            for (int i = 1; i < d.droga.Length; i++)
            {
                Vector2 a = d.droga[i - 1], c = d.droga[i]; float len = Vector2.Distance(a, c);
                if (len < 1e-4f) continue;
                var t = (c - a) / len; var nn = new Vector2(-t.y, t.x);
                while (next <= s + len)
                {
                    var p = a + t * (next - s);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var w = p + nn * (side * off);
                        var dl = w - d.srodek; var l = new Vector2(dl.x * cs - dl.y * sn, dl.x * sn + dl.y * cs);
                        if (OdPadu(d, l) < 2f || !WPatchu(d, w, 6f) || Blisko(d, w, off - .3f)) continue;   // the pad, the rim, a bend's other leg
                        var at = new Vector3(w.x, Grunt(d, w.x, w.y), w.y) * K;
                        var o = BraxChampionsModel.Postaw(g, palm, "WILLA " + d.nr + " | " + d.nazwa + " | palma przy drodze", at, (next * 37f + side * 90f) % 360f, 1f, false, true);
                        if (!o) continue;
                        BraxMiastoAssety.LodyZWezlow(o, new[] { .25f, .08f, .012f });
                        BraxChampionsPalmTrunk.OnSpot(o, at);
                        placed.Add(w); aleja++;
                    }
                    next += 12f;
                }
                s += len;
            }
            maxP = Mathf.Max(8, maxP - aleja);                                  // the garden's own palms: fewer (the avenue is many)
            if (hill) Aleja = aleja; else AlejaNabrzeze = aleja;
        }
        for (int i = 0; i < 6000 && (palmy < maxP || drzewa < maxD); i++)
        {
            var l = new Vector2(R(pd.x - 32f, pd.y + 32f), R(pd.z - 30f, pd.w + 18f));
            float od = OdPadu(d, l); if (od < 4.5f || od > maxOd) continue;
            var w = d.srodek + new Vector2(l.x * cs + l.y * sn, -l.x * sn + l.y * cs);
            if ((hill || shore) && Blisko(d, w, RoadHalf + 4.5f)) continue;
            if (shore && (Grunt(d, w.x, w.y) < SeaY + 1.0f || l.x < -54f ||                              // the lawns: off the beach, inside its west wall
                          (l.y < BraxChampionsWille.N3PlazaZ + 2f && l.x > BraxChampionsWille.N3PlazaX0 - 1f && l.x < BraxChampionsWille.N3PlazaX1 + 1f))) continue;
            if (!hill && !shore && (Grunt(d, w.x, w.y) < SeaY + .55f || (Mathf.Abs(w.x - ps.x) < 6f && w.y > ps.y - 30f))) continue;
            bool palma = palmy < maxP && (drzewa >= maxD || rnd.NextDouble() < (hill ? .4 : .75));
            bool tooNear = false;
            foreach (var q in placed) if ((q - w).sqrMagnitude < 5f * 5f) { tooNear = true; break; }
            if (tooNear) continue;
            var at = new Vector3(w.x, Grunt(d, w.x, w.y), w.y) * K; float yaw = R(0f, 360f);
            if (palma)
            {
                palmy++; if (!palm) continue;
                var o = BraxChampionsModel.Postaw(g, palm, "WILLA " + d.nr + " | " + d.nazwa + " | palma", at, yaw, 1f, false, true);
                if (!o) continue;
                BraxMiastoAssety.LodyZWezlow(o, new[] { .25f, .08f, .012f });
                BraxChampionsPalmTrunk.OnSpot(o, at);
            }
            else
            {
                drzewa++; if (!tree) continue;
                if (!BraxRegion.Put(g, tree, "WILLA " + d.nr + " | " + d.nazwa + " | drzewo", at, yaw, 1f, false, 6.5f * K)) continue;
                W.Kolizja(at + Vector3.up * 1.5f * K, new Vector3(.5f, 3f, .5f) * K, Quaternion.identity);   // the trunk
            }
            placed.Add(w);
        }
        ZielenPalmy += palmy; ZielenDrzewa += drzewa;
        if (hill) Debug.Log("MISTRZOWIE_WILLA_ALEJA palms=" + Aleja + " garden_palms=" + palmy + " trees=" + drzewa + " (owner 23.11 22:22: thick palms both sides of the drive)");
        if (shore) Debug.Log("MISTRZOWIE_WILLA_ALEJA_NABRZEZE palms=" + AlejaNabrzeze + " garden_palms=" + palmy + " trees=" + drzewa + " (owner 24.11 00:00: \"do willi palmy\")");
    }

    // owner 17.11 14:02 ("mały pomostek, miejsce na motorówkę, nie wiem, na mały jacht"): his Riva moored along the jetty's
    // east side by its T head (BraxChampionsHarbour: the marina's own GLB at its real 8.5 m, BraxVessel to take it out to
    // sea); the T head's 9 m face stays free for a small yacht
    public static int Lodzie;
    static void Lodz(Transform g, Dzialka d)
    {
        var s = PomostStart(d); float zb = s.y + PomostDl;
        try { Lodzie += BraxChampionsHarbour.PrywatnaLodz(g, "W2", "motorboat", s.x + 3.0f, zb - 9f, 0f, 2.6f, s.x + .9f, zb - 9f, PomostY); }
        catch (Exception e) { BraxBuildQuality.Warn("DO POPRAWKI WILLA_2_RIVA: " + e.Message); }
    }

    // owner 23.11 20:11 ("za mało premium wygląda ta wyspa"): the island's beach furnished - a path of granite slabs from the
    // villa's stair to the jetty with low lights, three pairs of teak loungers under white parasols on the north beach (the
    // view to the city), an outdoor shower by the path. The loungers follow the beach's slope (7 %), their feet to the sea.
    public static int WyspaLezaki;
    static void PlazaWyspy(BraxChampionsMeshBatch b, Dzialka d)
    {
        var granit = BraxChampionsWille.GranitTarasu();
        var stal = BraxChampionsHarbour.Surface("wyspa_stal_nierdzewna", new Color(.78f, .78f, .79f), .25f, .55f); stal.SetFloat("_ReflectionStrength", 1f);
        var poducha = BraxChampionsFoundation.SourceMaterial("wyspa_poduchy_bawelna", "cotton_jersey", .6f, new Color(1.20f, 1.36f, 1.46f));
        var plotno = BraxChampionsFoundation.SourceMaterial("wyspa_parasol_plotno", "terlenka", .9f, new Color(1.21f, 1.36f, 1.53f));
        var granat = BraxChampionsFoundation.SourceMaterial("wyspa_parasol_granat", "terlenka", .9f, new Color(.17f, .30f, .60f));
        var led = BraxChampionsHarbour.Surface("wyspa_lampka_led", new Color(.9f, .9f, .88f), .3f, 0f, new Color(1.0f, .86f, .62f));
        // the path: 1 m granite slabs, 2.4 m wide, each 2 cm over the highest ground under it (no ground through a slab)
        var ps = PomostStart(d); float zStopa = d.srodek.y - BraxChampionsWille.Pad(d.nr).z + 6.9f;      // the stair's foot (6.9 m off the pad)
        for (float z = zStopa + .1f; z < ps.y - .1f; z += 1f)
        {
            float z1 = Mathf.Min(z + 1f, ps.y - .1f), top = float.MinValue;                                 // edge to edge
            foreach (float xx in new[] { ps.x - 1.2f, ps.x, ps.x + 1.2f }) foreach (float zz in new[] { z, z1 }) top = Mathf.Max(top, Grunt(d, xx, zz));
            top += .02f;
            b.BoxMinMax(granit, ps.x - 1.2f, ps.x + 1.2f, top - .12f, top, z, z1);
            W.Kolizja(new Vector3(ps.x, top - .06f, (z + z1) * .5f) * K, new Vector3(2.4f, .12f, z1 - z) * K, Quaternion.identity);
        }
        // low bollard lights either side of the path, every 4 m
        for (float z = zStopa + 1.5f; z < ps.y; z += 4f)
            foreach (float sx in new[] { -1.6f, 1.6f })
            {
                float y = Grunt(d, ps.x + sx, z);
                b.Cylinder(slupek, new Vector3(ps.x + sx, y - .05f, z), .06f, .55f, 12);
                b.Cylinder(led, new Vector3(ps.x + sx, y + .44f, z), .055f, .03f, 12, Quaternion.identity, true, true);
            }
        // the outdoor shower beside the path, on a granite slab
        {
            float sx = ps.x + 3.4f, sz = (zStopa + ps.y) * .5f, y = Grunt(d, sx, sz);
            b.BoxMinMax(granit, sx - .6f, sx + .6f, y - .08f, y + .04f, sz - .6f, sz + .6f);
            b.Cylinder(stal, new Vector3(sx, y + .04f, sz), .05f, 2.3f, 14);
            b.Tube(stal, new Vector3(sx, y + 2.30f, sz), new Vector3(sx - .42f, y + 2.30f, sz), .022f, 8);
            b.Cylinder(stal, new Vector3(sx - .42f, y + 2.22f, sz), .11f, .04f, 16, Quaternion.identity, true, true);
            W.Kolizja(new Vector3(sx, y + 1.15f, sz) * K, new Vector3(.14f, 2.3f, .14f) * K, Quaternion.identity);
        }
        // three pairs of loungers with a parasol, on the dry sand east of the jetty, looking north over the bay
        foreach (float cx in new[] { ps.x + 10f, ps.x + 17f, ps.x + 24f })
        {
            // the place: the first dry sand going north (lawn's edge at 1 m, the run-up's wet sand at .3 m over the sea)
            float cz = ps.y;
            for (float z = ps.y - 20f; z < ps.y + 30f; z += .25f) { float h = Grunt(d, cx, z); if (h < SeaY + .62f) { cz = z; break; } }
            foreach (float off in new[] { -.65f, .65f }) Lezak(b, d, poducha, stal, cx + off, cz);
            Parasol(b, d, stal, plotno, granat, cx, cz - .75f);
        }
    }
    // a teak lounger 1.95 x 0.65 m, its back toward the land (-z), its feet to the sea (+z), pitched with the sand under it
    static void Lezak(BraxChampionsMeshBatch b, Dzialka d, Material poducha, Material stal, float x, float z)
    {
        const float w = .65f, L = 1.95f, h = .32f;
        float yA = Grunt(d, x, z - L * .5f), yB = Grunt(d, x, z + L * .5f), pitch = -Mathf.Atan2(yB - yA, L) * Mathf.Rad2Deg;
        var q = Quaternion.Euler(pitch, 0f, 0f); var o = new Vector3(x, (yA + yB) * .5f, z);
        Vector3 P(float dx, float dy, float dz) => o + q * new Vector3(dx, dy, dz);
        foreach (float sx in new[] { -.3f, .3f })
        {
            b.Box(stal, P(sx, h, .25f), new Vector3(.035f, .05f, 1.2f), q);
            foreach (float zz in new[] { -.15f, .85f }) b.Box(stal, P(sx, h * .5f, zz), new Vector3(.035f, h, .035f), q);
        }
        b.Box(drewno, P(0f, h + .03f, .25f), new Vector3(w * .9f, .02f, 1.18f), q);
        b.Box(poducha, P(0f, h + .07f, .27f), new Vector3(w * .86f, .06f, 1.14f), q);
        var back = q * Quaternion.Euler(40f, 0f, 0f);                                                // up toward the land
        var bc = P(0f, h, -.35f) + back * new Vector3(0f, 0f, -.37f);
        b.Box(drewno, bc + back * new Vector3(0f, .03f, 0f), new Vector3(w * .9f, .02f, .72f), back);
        b.Box(poducha, bc + back * new Vector3(0f, .07f, 0f), new Vector3(w * .86f, .06f, .68f), back);
        W.Kolizja(P(0f, .27f, 0f) * K, new Vector3(w, .54f, L * .95f) * K, q);
        W.Siedzisko(P(0f, h + .10f, 0f) * K, Vector3.forward, "LEŻAK", true); WyspaLezaki++;
    }
    // a round white parasol, 16 ribs, 2.7 m canopy, a navy valance, its pole in the sand
    static void Parasol(BraxChampionsMeshBatch b, Dzialka d, Material stal, Material plotno, Material granat, float x, float z)
    {
        var p = new Vector3(x, Grunt(d, x, z), z);
        b.Cylinder(slupek, p - Vector3.up * .05f, .09f, .17f, 12);
        b.Cylinder(stal, p, .024f, 2.55f, 10);
        const int ribs = 16; const float r = 1.35f, eave = 2.18f, top = 2.62f;
        var apex = p + Vector3.up * top;
        for (int j = 0; j < ribs; j++)
        {
            float a0 = 2f * Mathf.PI * j / ribs, a1 = 2f * Mathf.PI * (j + 1) / ribs, s0 = j % 2 == 0 ? 0f : .05f, s1 = (j + 1) % 2 == 0 ? 0f : .05f;
            var p0 = p + new Vector3(r * Mathf.Cos(a0), eave - s0, r * Mathf.Sin(a0)); var p1 = p + new Vector3(r * Mathf.Cos(a1), eave - s1, r * Mathf.Sin(a1));
            b.Triangle(plotno, apex, p1, p0); b.Triangle(plotno, apex, p0, p1);
            b.Quad(granat, p0, p0 - Vector3.up * .10f, p1 - Vector3.up * .10f, p1); b.Quad(granat, p1, p1 - Vector3.up * .10f, p0 - Vector3.up * .10f, p0);
        }
        b.Cylinder(stal, apex - Vector3.up * .02f, .05f, .10f, 10);
        W.Kolizja(new Vector3(x, p.y + 1.25f, z) * K, new Vector3(.12f, 2.5f, .12f) * K, Quaternion.identity);
    }

    // post-and-rail fence (graphite steel) round a land patch FenceInset inside its edge, open to the region and at the
    // road; posts every 2.5 m on the ground, three rails; behind it an invisible wall 3 m high
    static void Plot(BraxChampionsMeshBatch b, Dzialka d)
    {
        float i0 = d.x0 + FenceInset, i1 = d.x1 - FenceInset, j0 = d.z0 + FenceInset, j1 = d.z1 - FenceInset;
        var linie = new List<(Vector2 a, Vector2 c)>();
        if (d.rodzaj == Rodzaj.Wzgorze)
        {
            // west, north, east; the south side is the region's edge (z 1000): open to the city
            linie.Add((new Vector2(i0, d.z0), new Vector2(i0, j1))); linie.Add((new Vector2(i0, j1), new Vector2(i1, j1))); linie.Add((new Vector2(i1, j1), new Vector2(i1, d.z0)));
        }
        else
        {
            // north and east of the seafront; the west side is the region's edge (x 1200), the south the sea
            float coast = BraxChampionsHorizon.Coast(i1);
            linie.Add((new Vector2(d.x0, j1), new Vector2(i1, j1))); linie.Add((new Vector2(i1, j1), new Vector2(i1, coast + 4f)));
        }
        foreach (var (a, c) in linie)
        {
            float len = Vector2.Distance(a, c); int n = Mathf.Max(1, Mathf.CeilToInt(len / 2.5f));
            for (int k = 0; k <= n; k++)
            {
                var p = Vector2.Lerp(a, c, k / (float)n);
                if (Blisko(d, p, RoadHalf + 1.5f)) continue;
                float y = Grunt(d, p.x, p.y);
                b.Box(slupek, new Vector3(p.x, y + .6f, p.y), new Vector3(.08f, 1.3f, .08f), Quaternion.identity);
                if (k == n) break;
                var p1 = Vector2.Lerp(a, c, (k + 1) / (float)n); if (Blisko(d, p1, RoadHalf + 1.5f)) continue;
                float y1 = Grunt(d, p1.x, p1.y); var mid = (p + p1) * .5f; float l = Vector2.Distance(p, p1);
                float yaw = Mathf.Atan2(p1.x - p.x, p1.y - p.y) * Mathf.Rad2Deg, pitch = -Mathf.Atan2(y1 - y, l) * Mathf.Rad2Deg;
                var rq = Quaternion.Euler(pitch, yaw, 0f);
                foreach (float hy in new[] { .35f, .75f, 1.15f }) b.Box(slupek, new Vector3(mid.x, (y + y1) * .5f + hy, mid.y), new Vector3(.04f, .05f, Mathf.Sqrt(l * l + (y1 - y) * (y1 - y))), rq);
                W.Kolizja(new Vector3(mid.x, (y + y1) * .5f + 1.5f, mid.y) * K, new Vector3(.2f, 3f + Mathf.Abs(y1 - y), l) * K, Quaternion.Euler(0f, yaw, 0f));
                Ogrodzenie += Mathf.RoundToInt(l);
            }
        }
    }
    static bool Blisko(Dzialka d, Vector2 p, float r) { Droga(d, p.x, p.y, out float od); return od < r; }
    static bool WPatchu(Dzialka d, Vector2 w, float m) => w.x > d.x0 + m && w.x < d.x1 - m && w.y > d.z0 + m && w.y < d.z1 - m;
    public static int Aleja;                                                    // the hill drive's avenue palms (23.11)
    public static int AlejaNabrzeze;                                            // the seafront drive's (24.11)

    // under the water at the sea edges of the patch: an invisible wall from the sea bed to 0.9 m under the surface
    static void MurPodWoda(Dzialka d)
    {
        float top = SeaY - .9f, bot = SeaY - 11f;
        if (d.rodzaj == Rodzaj.Wyspa) bot = SeaY - WyspaDno - 1f;                    // the island's bed goes down to 20 m at its rim
        float h = top - bot, y = (top + bot) * .5f;
        var sciany = new List<(float x0, float x1, float z0, float z1)>();
        if (d.rodzaj == Rodzaj.Wyspa)
        {
            sciany.Add((d.x0 + 2f, d.x1 - 2f, d.z0 + 2f, d.z0 + 2.4f)); sciany.Add((d.x0 + 2f, d.x1 - 2f, d.z1 - 2.4f, d.z1 - 2f));
            sciany.Add((d.x0 + 2f, d.x0 + 2.4f, d.z0 + 2f, d.z1 - 2f)); sciany.Add((d.x1 - 2.4f, d.x1 - 2f, d.z0 + 2f, d.z1 - 2f));
        }
        else
        {
            // the seafront: along its south edge and down its east and west sides to the coast
            float cw = BraxChampionsHorizon.Coast(d.x0), ce = BraxChampionsHorizon.Coast(d.x1);
            sciany.Add((d.x0 + 2f, d.x1 - 2f, d.z0 + 2f, d.z0 + 2.4f));
            sciany.Add((d.x0 + 2f, d.x0 + 2.4f, d.z0 + 2f, cw)); sciany.Add((d.x1 - 2.4f, d.x1 - 2f, d.z0 + 2f, ce));
        }
        foreach (var (x0, x1, z0, z1) in sciany)
            W.Kolizja(new Vector3((x0 + x1) * .5f, y, (z0 + z1) * .5f) * K, new Vector3(x1 - x0, h, Mathf.Abs(z1 - z0)) * K, Quaternion.identity);
    }

    // the island's jetty: teak deck 2.4 m wide, 1 m over the sea, on piles from the lawn's edge over the beach and the lagoon
    // (56 m: its head over 2 m of water, by the lagoon's edge), a T head 9 m wide with bollards; north of the villa's garage
    // side (the pool's view stays open)
    // the jetty's root on the lawn before the pad's front, at the villa's own landing (BraxChampionsWille.PomostX: its stair down)
    public static Vector2 PomostStart(Dzialka d) => new Vector2(d.srodek.x - BraxChampionsWille.PomostX(d.nr), d.srodek.y - BraxChampionsWille.Pad(d.nr).z + 4f + d.spad - 1f);
    public const float PomostDl = 56f, PomostY = SeaY + 1.0f;
    static void Pomost(BraxChampionsMeshBatch b, Dzialka d)
    {
        var s = PomostStart(d); float x = s.x, za = s.y, zb = s.y + PomostDl, y = PomostY;
        b.Box(drewno, new Vector3(x, y - .06f, (za + zb) * .5f), new Vector3(2.4f, .12f, PomostDl), Quaternion.identity);
        b.Box(drewno, new Vector3(x, y - .06f, zb - 1.5f), new Vector3(9f, .12f, 3f), Quaternion.identity);
        W.Kolizja(new Vector3(x, y - .06f, (za + zb) * .5f) * K, new Vector3(2.4f, .12f, PomostDl) * K, Quaternion.identity);
        W.Kolizja(new Vector3(x, y - .06f, zb - 1.5f) * K, new Vector3(9f, .12f, 3f) * K, Quaternion.identity);
        for (float z = za + 2f; z <= zb; z += 4f)
            foreach (float sx in new[] { -1.05f, 1.05f })
            {
                float g = Grunt(d, x + sx, z); if (g > y - .2f) continue;
                b.Cylinder(drewno, new Vector3(x + sx, g - .3f, z), .11f, y - .12f - (g - .3f), 10, Quaternion.identity, true);
            }
        foreach (float sx in new[] { -4.2f, 4.2f }) b.Cylinder(slupek, new Vector3(x + sx, y, zb - .4f), .11f, .38f, 12, Quaternion.identity, true);
    }
}
