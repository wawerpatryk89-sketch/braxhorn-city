using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using T = BraxChampionsTorKladow;
using W = BraxWnetrzaKit;

/// <summary>THE PODIUM AND THE TOP 10 BOARD in the paddock's west by the finish line (owner 24.11.2026 20:31 "Tablica z
/// najlepszymi wynikami ... na turniej", his picks "Tablica TOP 10", "Turniej z rywalami", "Podium i puchar").
///   - The podium, as the motocross podiums stand: three boxes, the 1st in the middle (75 cm), the 2nd at the winner's right
///     (50 cm - the audience's left), the 3rd at his left (30 cm), each 3.2 x 3.2 m so a quad stands on it with its rider, the
///     big gold numbers on their fronts; on a granite platform (the club's Granite Tile 04) before a black backdrop with
///     BRAXHORN QUAD CUP in gold. The boxes the club's glossy black. After a race or the cup the race (BraxChampionsWyscig)
///     puts the first three on them facing the paddock and the player not among them before it (7 m, facing it).
///   - The cups on their presentation table beside it (a teak top on black): the gold (62 cm), the silver (56), the bronze
///     (50) - a loving cup on a black base, the metals' own measured colours (gold sRGB 255/226/155, silver 252/250/245, the
///     bronze a copper-tin), polished; moved by the race onto the steps for the ceremony, then back. Not static.
///   - The TOP 10 board west of it (BraxChampionsTablicaTop): 9.6 x 4.8 m (256 x 128 LEDs, 3.75 cm) on two steel legs, 2.2 m
///     up, facing the paddock as the podium does.
/// Real sizes x K. Nothing glows but the LED screen's picture (the race board's own screen material).</summary>
public static class BraxChampionsPodiumKladow
{
    static float K => BraxClaudeOsiedle.K;
    public const float PodiumX = -128f, PodiumZ = 962f, Stopien = 3.2f, Glebokosc = 3.2f, Plyta = .06f;   // the steps' front at z 962
    public static readonly float[] Wysokosc = { .75f, .5f, .3f };                    // the 1st, the 2nd, the 3rd
    public static readonly float[] Przesuniecie = { 0f, -Stopien, Stopien };         // x from the middle: the 2nd at the west (the audience's left)
    public const float PlytaX0 = -135f, PlytaX1 = -115.5f, PlytaZ0 = 960.5f, PlytaZ1 = 968f;
    public const float TloZ = 966.4f, TloX0 = -134f, TloX1 = -122f, TloH = 3.4f;
    public const float WidzZ = 953.5f;                                               // the player watching, 8.5 m before the steps
    public const float StolX0 = -120f, StolX1 = -117.6f, StolZ0 = 962.4f, StolZ1 = 963.2f, StolH = .76f;
    public static readonly float[] Puchar = { .62f, .56f, .50f };                   // the cups' heights with their bases
    public const float TopX = -146f, TopZ = 966.5f, TopW = 9.6f, TopH = 4.8f, TopNad = 2.2f;

    static Material teak, granit, podest, zlotoP, srebro, braz;
    static void Materialy()
    {
        granit = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_granit", "granite_tile_04", 2f, new Color(1.02f, 1f, .98f));
        teak = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_teak", "teak_veneer", 1f, Color.white);
        podest = BraxChampionsHarbour.Surface("tor_kladow_klub_podest", new Color(.022f, .022f, .026f), .12f, .25f);   // the club's glossy black
        zlotoP = BraxChampionsHarbour.Surface("tor_kladow_puchar_zloto", new Color(1f, .886f, .608f), .16f, 1f);
        srebro = BraxChampionsHarbour.Surface("tor_kladow_puchar_srebro", new Color(.988f, .98f, .96f), .14f, 1f);
        braz = BraxChampionsHarbour.Surface("tor_kladow_puchar_braz", new Color(.80f, .55f, .36f), .2f, 1f);
    }

    public static int Pucharow, Trojkaty;

    /// <summary>The podium, the cups, the TOP 10 board; the race's podium places, its watching place and its cups set.</summary>
    public static void Zbuduj(Transform g, BraxChampionsMeshBatch b, float y, BraxChampionsWyscig w, string folder)
    {
        Materialy();
        Pucharow = Trojkaty = 0;
        float yp = y + Plyta;
        // the platform
        b.BoxMinMax(granit, PlytaX0, PlytaX1, y - .08f, yp, PlytaZ0, PlytaZ1);
        W.Kolizja(new Vector3((PlytaX0 + PlytaX1) * .5f, (y - .08f + yp) * .5f, (PlytaZ0 + PlytaZ1) * .5f) * K, new Vector3(PlytaX1 - PlytaX0, Plyta + .08f, PlytaZ1 - PlytaZ0) * K, Quaternion.identity);
        // the three boxes (the side ones 1 cm into the middle one and 1 cm shallower at each end: no face shared; each 1 cm
        // into the platform), their numbers
        w.podium = new Vector3[3]; w.pucharyNaPodium = new Vector3[3];
        w.podiumObrot = Quaternion.Euler(0f, 180f, 0f);
        for (int i = 0; i < 3; i++)
        {
            float cx = PodiumX + Przesuniecie[i], h = Wysokosc[i];
            float x0 = cx - Stopien * .5f - (i == 2 ? .01f : 0f), x1 = cx + Stopien * .5f + (i == 1 ? .01f : 0f), z0 = PodiumZ + (i == 0 ? 0f : .01f), z1 = PodiumZ + Glebokosc - (i == 0 ? 0f : .01f);
            b.BoxMinMax(podest, x0, x1, yp - .01f, yp + h, z0, z1);
            W.Kolizja(new Vector3((x0 + x1) * .5f, yp + h * .5f, PodiumZ + Glebokosc * .5f) * K, new Vector3(x1 - x0, h, Glebokosc) * K, Quaternion.identity);
            BraxChampionsLetters3D.Text(b, T.zloto, podest, (i + 1).ToString(), new Vector3(cx, yp + h * .12f, z0 - .01f), 180f, h * .74f, .03f);
            w.podium[i] = new Vector3(cx, yp + h, PodiumZ + Glebokosc * .5f) * K;                     // the quad's root, on its wheels
            w.pucharyNaPodium[i] = new Vector3(cx - 1.15f, yp + h + .002f, PodiumZ + .45f) * K;      // its cup beside its front wheel
        }
        w.podiumWidz = new Vector3(PodiumX, y, WidzZ) * K; w.podiumWidzObrot = Quaternion.identity;   // facing north, at it
        // the backdrop: a black wall on the platform, BRAXHORN QUAD CUP in gold, the track's name under it
        b.BoxMinMax(T.czern, TloX0, TloX1, yp - .01f, yp + TloH, TloZ, TloZ + .2f);
        W.Kolizja(new Vector3((TloX0 + TloX1) * .5f, yp + TloH * .5f, TloZ + .1f) * K, new Vector3(TloX1 - TloX0, TloH, .2f) * K, Quaternion.identity);
        BraxChampionsLetters3D.Text(b, T.zloto, T.czern, "BRAXHORN QUAD CUP", new Vector3(PodiumX, yp + 2.45f, TloZ - .01f), 180f, .55f, .05f, .12f, TloX1 - TloX0 - 1.2f);
        BraxChampionsLetters3D.Text(b, T.biel, T.czern, "TOR KŁADÓW BRAXHORN", new Vector3(PodiumX, yp + 1.95f, TloZ - .01f), 180f, .22f, .03f, .14f, TloX1 - TloX0 - 4f);
        // the presentation table: a teak top on a black base
        b.BoxMinMax(T.czern, StolX0 + .08f, StolX1 - .08f, yp - .01f, yp + StolH - .041f, StolZ0 + .08f, StolZ1 - .08f);
        b.BoxMinMax(teak, StolX0, StolX1, yp + StolH - .04f, yp + StolH, StolZ0, StolZ1);
        W.Kolizja(new Vector3((StolX0 + StolX1) * .5f, yp + StolH * .5f, (StolZ0 + StolZ1) * .5f) * K, new Vector3(StolX1 - StolX0, StolH, StolZ1 - StolZ0) * K, Quaternion.identity);
        // the cups: the gold in the middle, the silver at its west, the bronze at its east, facing the paddock
        var mat = new[] { zlotoP, srebro, braz }; float[] px = { -118.8f, -119.5f, -118.1f };
        w.puchary = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var c = PucharNa(g, "TOR DO KŁADÓW | puchar " + (i + 1), new Vector3(px[i], yp + StolH + .002f, (StolZ0 + StolZ1) * .5f), Puchar[i], mat[i], folder);
            c.rotation = w.podiumObrot; w.puchary[i] = c; Pucharow++;
        }
        TablicaTop(g, b, y, w, folder);
    }

    /// <summary>A loving cup: a black square base, the foot, the stem with its knot, the bowl (open, its inside), two ear
    /// handles; h with its base. Its own object about the base's bottom (it moves), nothing of it static.</summary>
    static Transform PucharNa(Transform g, string nazwa, Vector3 dol, float h, Material metal, string folder)
    {
        var p = new GameObject(nazwa).transform; p.SetParent(g, false); p.localPosition = dol * K;
        var bb = new BraxChampionsMeshBatch();
        float s = h / .62f, podstawa = .09f * s, bok = .17f * s;
        bb.RoundedBox(T.czern, new Vector3(0f, podstawa * .5f, 0f), new Vector3(bok, podstawa, bok), .006f * s);
        // the cup over its base (real metres for the 62 cm one, x s): foot, stem, knot, the bowl up to the rim, then down its inside
        var prof = new[]
        {
            new Vector2(0f, 0f), new Vector2(.072f, 0f), new Vector2(.072f, .012f), new Vector2(.058f, .03f), new Vector2(.026f, .05f),
            new Vector2(.017f, .085f), new Vector2(.017f, .16f), new Vector2(.03f, .175f), new Vector2(.03f, .19f), new Vector2(.019f, .205f),
            new Vector2(.03f, .23f), new Vector2(.062f, .255f), new Vector2(.1f, .29f), new Vector2(.124f, .345f), new Vector2(.134f, .41f),
            new Vector2(.138f, .462f), new Vector2(.144f, .47f), new Vector2(.14f, .478f), new Vector2(.13f, .474f), new Vector2(.124f, .42f),
            new Vector2(.112f, .35f), new Vector2(.085f, .30f), new Vector2(0f, .28f)
        };
        for (int i = 0; i < prof.Length; i++) prof[i] *= s;
        bb.Lathe(metal, new Vector3(0f, podstawa, 0f), prof, 40, Quaternion.identity, false, false);
        // the ear handles at its east and west: a C (half an ellipse, 12 pieces) from the bowl's shoulder out and down to its belly
        foreach (float strona in new[] { -1f, 1f })
            for (int i = 0; i < 12; i++)
            {
                Vector3 U(int k) { float a = Mathf.PI * (.5f - k / 12f); return new Vector3(strona * (.118f + .088f * Mathf.Cos(a)) * s, podstawa + (.37f + .062f * Mathf.Sin(a)) * s, 0f); }
                bb.Tube(metal, U(i), U(i + 1), .011f * s, 8);
            }
        bb.Build(p, nazwa + " | siatka", folder, true, K);
        foreach (var tr in p.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(tr.gameObject, (StaticEditorFlags)0);
        Trojkaty += bb.Triangles;
        return p;
    }

    // the TOP 10 board: a black frame on two steel legs, the screen 3 cm before it, facing south (the paddock)
    static void TablicaTop(Transform g, BraxChampionsMeshBatch b, float y, BraxChampionsWyscig w, string folder)
    {
        float x0 = TopX - TopW * .5f, x1 = TopX + TopW * .5f, yb = y + TopNad, yt = yb + TopH;
        b.BoxMinMax(T.czern, x0 - .3f, x1 + .3f, yb - .3f, yt + .3f, TopZ - .15f, TopZ + .25f);
        foreach (float lx in new[] { TopX - 3f, TopX + 3f })
        {
            b.Cylinder(T.stal, new Vector3(lx, y - .4f, TopZ + .05f), .16f, TopNad + .2f, 12);
            W.Kolizja(new Vector3(lx, y + TopNad * .5f, TopZ + .05f) * K, new Vector3(.36f, TopNad + .4f, .36f) * K, Quaternion.identity);
        }
        W.Kolizja(new Vector3(TopX, (yb + yt) * .5f, TopZ + .05f) * K, new Vector3(TopW + .6f, TopH + .6f, .4f) * K, Quaternion.identity);
        var ekran = BraxChampionsHarbour.Surface("tor_kladow_tablica_led", Color.white, .35f, 0f, new Color(1.6f, 1.6f, 1.6f));   // the race board's screen
        var be = new BraxChampionsMeshBatch(); float zs = TopZ - .18f;
        // seen from the south (looking north) its left is west: u 0 at x0
        be.QuadUV(ekran, new Vector3(x0, yb, zs), new Vector3(x0, yt, zs), new Vector3(x1, yt, zs), new Vector3(x1, yb, zs), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        var te = be.Build(g, "TOR DO KŁADÓW | tablica TOP 10", folder, false, K);
        Trojkaty += be.Triangles;
        var t = new GameObject("TOR DO KŁADÓW | tablica TOP 10 (wyniki)").AddComponent<BraxChampionsTablicaTop>();
        t.transform.SetParent(g, false);
        var r = te.GetComponentsInChildren<MeshRenderer>(true); t.ekran = r.Length > 0 ? r[0] : null; t.wyscig = w;
    }
}
