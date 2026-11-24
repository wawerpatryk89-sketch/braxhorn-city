using System.Collections.Generic;
using UnityEngine;
using T = BraxChampionsTorKladow;
using W = BraxWnetrzaKit;

/// <summary>THE QUAD CLUB round the rental booth (owner 24.11.2026 19:31 "bardziej luksusowo ... koło tego wypożyczalni", his
/// picks "Kłady na podestach" + "Taras nad torem"; the models: the motorsport clubhouses and the paddock hospitality of MotoGP -
/// raised terraces over the track, floor-to-ceiling glass, timber, lounge seating facing it).
///   - The showroom plaza before the booth: large granite tiles (Poly Haven Granite Tile 04, 2 m), the six quads on glossy black
///     podiums as in a car showroom - five in a row, the sixth (the lime Raptor) in front on a bigger one, turning
///     (BraxChampionsObrotnica: slowly while nobody rides it); under a new canopy: a 37 x 17 m flat black roof on five slim
///     columns, its underside timber (Hinoki Planks), BRAXHORN QUAD CLUB in gold along its front; the feather flags at its
///     front corners. A quad rides off its podium (28 cm) and back on as onto any kerb.
///   - The VIP terrace over the start: 28 x 9 m, its deck 2.6 m up on black steel (the start gate right below it, the
///     motocross field beyond), the decking Hinoki Planks, a glass balustrade 1.1 m with a teak handrail (the building
///     code's 1.1 m for a terrace), open-riser stairs down its west side (16 x 16.25 cm, 30 cm treads); along the glass five marble-topped bar
///     tables with teak stools (facing the track), behind them three lounges - a teak sofa and two armchairs with bouclé
///     cushions (Poly Haven Curly Teddy Natural, the owner's list), a marble coffee table, a 3 x 3 m cantilever parasol (Terlenka
///     canvas) - and four planters with young pines (the forest's own cards); BRAXHORN VIP on its fascia.
/// Real sizes (outdoor lounge furniture: a 3-seat sofa 210 x 85 cm, seat 44 cm; a bar table 70 x 70 x 105; a stool seat 76),
/// x K as everything. Nothing glows (no LED lines: the owner's rule).</summary>
public static class BraxChampionsKlubKladow
{
    static float K => BraxClaudeOsiedle.K;
    // the plaza before the booth (the booth's plinth from z 995.9), its canopy, the podiums
    public const float PlacX0 = -80f, PlacX1 = -44f, PlacZ0 = 976.5f, PlacZ1 = 995.8f, PlacH = .12f;
    public const float WiataH = 4.3f, WiataX0 = -80.5f, WiataX1 = -43.5f, WiataZ0 = 977.6f, WiataZ1 = 995.6f;
    public const float PodestR = 1.9f, PodestH = .28f, ObrotR = 2.3f, ObrotH = .32f, RzadZ = 990.6f, ObrotZ = 983.2f, Rozstaw = 5.6f, SrodekX = -62f;
    public const int Bohater = 3;                                  // the fleet's 4th: the lime Raptor, on the turntable
    // the terrace over the start (the start gate at x -72 below it)
    public const float TX0 = -91f, TX1 = -63f, TZ0 = 937.5f, TZ1 = 946.5f, TH = 2.6f, Balustrada = 1.1f;
    public const float SchodyZ0 = 940f, SchodyZ1 = 944f, Stopien = .1625f, Bieg = .3f;   // the stairs down its west side, 4 m wide
    public const int Stopni = 16;

    static Material granit, deski, teak, marmur, boucle, plotno, podest, szklo;
    static void Materialy()
    {
        granit = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_granit", "granite_tile_04", 2f, new Color(1.02f, 1f, .98f));
        deski = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_deski", "hinoki_planks", 1.89f, Color.white);
        teak = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_teak", "teak_veneer", 1f, Color.white);
        marmur = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_marmur", "marble_01", 1.5f, Color.white);
        boucle = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_boucle", "curly_teddy_natural", .336f, Color.white);
        plotno = BraxChampionsFoundation.SourceMaterial("tor_kladow_klub_plotno", "terlenka", .266f, new Color(1.08f, 1.06f, 1.02f));
        podest = BraxChampionsHarbour.Surface("tor_kladow_klub_podest", new Color(.022f, .022f, .026f), .12f, .25f);   // glossy black
        szklo = BraxChampionsHarbour.Surface("tor_kladow_klub_szklo", new Color(.72f, .80f, .84f, .22f), .04f, 0f, null, true);
    }

    public static int Klady;

    /// <summary>The plaza, the podiums with the six quads, the canopy, the flags; the terrace.</summary>
    public static void Zbuduj(Transform g, BraxChampionsMeshBatch b, float y, BraxChampionsTorCzas czas, AudioClip wolne, AudioClip jazda)
    {
        Materialy();
        BraxChampionsLasKladow.Dodatkowe.Clear();
        Plac(g, b, y, czas, wolne, jazda);
        Taras(b, y);
    }

    // ---------------------------------------------------------------- the showroom plaza
    static void Plac(Transform g, BraxChampionsMeshBatch b, float y, BraxChampionsTorCzas czas, AudioClip wolne, AudioClip jazda)
    {
        float yp = y + PlacH;
        b.BoxMinMax(granit, PlacX0, PlacX1, y - .08f, yp, PlacZ0, PlacZ1);
        W.Kolizja(new Vector3((PlacX0 + PlacX1) * .5f, (y - .08f + yp) * .5f, (PlacZ0 + PlacZ1) * .5f) * K, new Vector3(PlacX1 - PlacX0, PlacH + .08f, PlacZ1 - PlacZ0) * K, Quaternion.identity);
        // the podiums: a glossy black disc with a bevelled edge (no thin bright ring), its collider the square inside it
        var miejsca = new Vector3[BraxChampionsKlady.Flota.Length];
        for (int i = 0, k = 0; i < miejsca.Length; i++)
        {
            bool bohater = i == Bohater; float r = bohater ? ObrotR : PodestR, h = bohater ? ObrotH : PodestH;
            var c = bohater ? new Vector3(SrodekX, yp, ObrotZ) : new Vector3(SrodekX + (k++ - 2) * Rozstaw, yp, RzadZ);
            b.Lathe(podest, c, new[] { new Vector2(0f, 0f), new Vector2(r, 0f), new Vector2(r, h - .05f), new Vector2(r - .05f, h), new Vector2(0f, h) }, 48);
            W.Kolizja(new Vector3(c.x, yp + h * .5f, c.z) * K, new Vector3(r * 1.41f, h, r * 1.41f) * K, Quaternion.identity);
            miejsca[i] = c + Vector3.up * h;
        }
        // the quads on them, facing the track; the lime Raptor on the turntable
        Klady = 0;
        for (int i = 0; i < BraxChampionsKlady.Flota.Length; i++)
        {
            var f = BraxChampionsKlady.Flota[i];
            try
            {
                var go = BraxChampionsKlady.Postaw(g, f.model, f.kolor, f.napis, miejsca[i] * K, 180f, czas, wolne, jazda);
                if (!go) continue;
                Klady++;
                if (i == Bohater)
                {
                    var ob = new GameObject("TOR DO KŁADÓW | klub: obrotnica (" + f.napis + ")").AddComponent<BraxChampionsObrotnica>();
                    ob.transform.SetParent(g, false);
                    ob.klad = go.GetComponent<BraxChampionsKlad>(); ob.srodek = miejsca[i] * K; ob.promien = ObrotR * K;
                }
            }
            catch (System.Exception e) { BraxBuildQuality.Warn("DO POPRAWKI TOR_KLADOW_KLAD " + f.napis + ": " + e.Message); }
        }
        // the canopy: a flat black roof (its fascia 45 cm), its underside timber inset 10 cm, on five slim columns
        float yw = y + WiataH;
        b.BoxMinMax(T.czern, WiataX0, WiataX1, yw, yw + .45f, WiataZ0, WiataZ1);
        b.BoxMinMax(deski, WiataX0 + .1f, WiataX1 - .1f, yw - .06f, yw - .01f, WiataZ0 + .1f, WiataZ1 - .1f);
        foreach (var (cx, cz) in new[] { (WiataX0 + 2f, WiataZ0 + .5f), (WiataX1 - 2f, WiataZ0 + .5f), (WiataX0 + 2f, WiataZ1 - .5f), (SrodekX, WiataZ1 - .5f), (WiataX1 - 2f, WiataZ1 - .5f) })
        {
            b.BoxMinMax(T.czern, cx - .12f, cx + .12f, yp, yw - .06f, cz - .12f, cz + .12f);
            W.Kolizja(new Vector3(cx, (yp + yw) * .5f, cz) * K, new Vector3(.24f, WiataH - PlacH, .24f) * K, Quaternion.identity);
        }
        BraxChampionsLetters3D.Text(b, T.zloto, T.czern, "BRAXHORN QUAD CLUB", new Vector3(SrodekX, yw + .08f, WiataZ0 - .01f), 180f, .3f, .03f, .14f, WiataX1 - WiataX0 - 4f);
        // the two feather flags at the plaza's front corners (black, gold edge, orange)
        foreach (float fx in new[] { PlacX0 - 1.5f, PlacX1 + .7f })
        {
            float fz = PlacZ0 - .8f;
            b.Cylinder(T.stal, new Vector3(fx, y, fz), .03f, 4.2f, 8);
            b.BoxMinMax(T.czern, fx, fx + .75f, y + 1.45f, y + 4.1f, fz - .02f, fz + .02f);
            b.BoxMinMax(T.zloto, fx + .75f, fx + .82f, y + 1.2f, y + 4.1f, fz - .03f, fz + .03f);
            b.BoxMinMax(T.pomarancz, fx, fx + .75f, y + 1.2f, y + 1.45f, fz - .03f, fz + .03f);
        }
    }

    // ---------------------------------------------------------------- the VIP terrace over the start
    public static int Siedzisk;
    static void Taras(BraxChampionsMeshBatch b, float y)
    {
        float yd = y + TH, xm = (TX0 + TX1) * .5f;
        // the deck: a black steel slab (its fascia) and the decking on it (no face under the decking: nothing coplanar)
        b.BoxMinMax(T.czern, TX0, TX1, yd - .4f, yd - .05f, TZ0, TZ1);
        b.Box(deski, new Vector3(xm, yd - .025f, (TZ0 + TZ1) * .5f), new Vector3(TX1 - TX0 - .04f, .05f, TZ1 - TZ0 - .04f), Quaternion.identity, false);
        W.Kolizja(new Vector3(xm, yd - .2f, (TZ0 + TZ1) * .5f) * K, new Vector3(TX1 - TX0, .4f, TZ1 - TZ0) * K, Quaternion.identity);
        foreach (float cx in new[] { TX0 + .3f, -84f, -77f, -70f, TX1 - .3f })
            foreach (float cz in new[] { TZ0 + .3f, TZ1 - .3f })
            {
                b.BoxMinMax(T.czern, cx - .13f, cx + .13f, y, yd - .4f, cz - .13f, cz + .13f);
                W.Kolizja(new Vector3(cx, y + (TH - .4f) * .5f, cz) * K, new Vector3(.26f, TH - .4f, .26f) * K, Quaternion.identity);
            }
        BraxChampionsLetters3D.Text(b, T.zloto, T.czern, "BRAXHORN VIP", new Vector3(xm, yd - .33f, TZ0 - .01f), 180f, .2f, .02f, .16f, 12f);
        // the glass balustrade with its teak handrail: the front full length, the sides between, the back either side of the stairs
        float hr = yd + Balustrada;
        void Odcinek(float x0, float x1, float z0, float z1, bool wzdluzX)
        {
            if (wzdluzX) { b.BoxMinMax(szklo, x0 + .05f, x1 - .05f, yd, hr - .06f, (z0 + z1) * .5f - .01f, (z0 + z1) * .5f + .01f); }
            else { b.BoxMinMax(szklo, (x0 + x1) * .5f - .01f, (x0 + x1) * .5f + .01f, yd, hr - .06f, z0 + .05f, z1 - .05f); }
            b.BoxMinMax(teak, x0, x1, hr - .06f, hr, z0, z1);
            W.Kolizja(new Vector3((x0 + x1) * .5f, yd + Balustrada * .5f, (z0 + z1) * .5f) * K, new Vector3(Mathf.Max(x1 - x0, .1f), Balustrada, Mathf.Max(z1 - z0, .1f)) * K, Quaternion.identity);
        }
        Odcinek(TX0, TX1, TZ0, TZ0 + .08f, true);
        Odcinek(TX0, TX1, TZ1 - .08f, TZ1, true);
        Odcinek(TX0, TX0 + .08f, TZ0 + .08f, SchodyZ0, false);
        Odcinek(TX0, TX0 + .08f, SchodyZ1, TZ1 - .08f, false);
        Odcinek(TX1 - .08f, TX1, TZ0 + .08f, TZ1 - .08f, false);
        // the stairs: open risers, teak treads on two black stringers, a teak handrail each side; the walk up a ramp collider
        // (16 risers, 15 treads: 4.5 m down to the west)
        float dl = (Stopni - 1) * Bieg;
        for (int k = 0; k < Stopni - 1; k++)
        {
            float top = y + (k + 1) * Stopien, x1 = TX0 - (Stopni - 2 - k) * Bieg, x0 = x1 - Bieg;
            b.BoxMinMax(teak, x0, x1, top - .05f, top, SchodyZ0 + .15f, SchodyZ1 - .15f);
        }
        var dol = new Vector3(TX0 - dl, y, 0f); var gora = new Vector3(TX0, yd, 0f); var kier = (gora - dol).normalized; float dlug = (gora - dol).magnitude;
        var obr = Quaternion.LookRotation(kier, Vector3.up);
        foreach (float sz in new[] { SchodyZ0 + .07f, SchodyZ1 - .07f })
        {
            var mid = new Vector3(TX0 - dl * .5f, (y + yd) * .5f - .12f, sz);
            b.Box(T.czern, mid, new Vector3(.1f, .3f, dlug), obr);
            b.Tube(teak, new Vector3(TX0 - dl, y + .9f, sz), new Vector3(TX0, yd + .9f, sz), .03f, 10);
            for (int k = 0; k <= 2; k++) { float t = k / 2f; var p = Vector3.Lerp(new Vector3(TX0 - dl + .15f, y, sz), new Vector3(TX0 - .15f, yd, sz), t); b.BoxMinMax(T.czern, p.x - .02f, p.x + .02f, p.y, p.y + .88f, sz - .02f, sz + .02f); }
        }
        var srodekRampy = new Vector3(TX0 - dl * .5f, (y + yd) * .5f, (SchodyZ0 + SchodyZ1) * .5f) - obr * Vector3.up * .05f;
        W.Kolizja(srodekRampy * K, new Vector3(SchodyZ1 - SchodyZ0, .1f, dlug) * K, obr);

        // along the glass: five bar tables, two stools each on the deck's side, facing the track
        Siedzisk = 0;
        foreach (float x in new[] { -88f, -82.4f, -76.8f, -71.2f, -65.6f })
        {
            float z = TZ0 + .75f;
            b.Cylinder(T.czern, new Vector3(x, yd, z), .26f, .03f, 20);
            b.Cylinder(T.czern, new Vector3(x, yd + .03f, z), .045f, 1.0f, 10);
            b.BoxMinMax(marmur, x - .35f, x + .35f, yd + 1.03f, yd + 1.06f, z - .35f, z + .35f);
            W.Kolizja(new Vector3(x, yd + .53f, z) * K, new Vector3(.7f, 1.06f, .7f) * K, Quaternion.identity);
            foreach (float sx in new[] { -.38f, .38f })
            {
                var s = new Vector3(x + sx, yd, z + .62f);
                b.Cylinder(T.czern, s, .2f, .025f, 16);
                b.Cylinder(T.czern, s + Vector3.up * .025f, .03f, .69f, 8);
                b.RoundedBox(teak, s + Vector3.up * .74f, new Vector3(.42f, .05f, .42f), .015f);
                b.RoundedBox(boucle, s + Vector3.up * .785f, new Vector3(.4f, .04f, .4f), .015f);
                Siedzisk++;
            }
        }
        // three lounges: a teak sofa facing the track, two armchairs facing each other, a marble coffee table, a cantilever parasol
        foreach (float x in new[] { -85f, -77f, -69f })
        {
            Sofa(b, new Vector3(x, yd, 944.9f), 0f, 2.1f); Siedzisk += 3;
            Sofa(b, new Vector3(x - 1.75f, yd, 943.0f), -90f, .85f); Sofa(b, new Vector3(x + 1.75f, yd, 943.0f), 90f, .85f); Siedzisk += 2;   // facing each other
            b.BoxMinMax(teak, x - .55f, x + .55f, yd, yd + .3f, 942.7f, 943.3f);
            b.BoxMinMax(marmur, x - .6f, x + .6f, yd + .3f, yd + .34f, 942.65f, 943.35f);
            W.Kolizja(new Vector3(x, yd + .17f, 943f) * K, new Vector3(1.2f, .34f, .7f) * K, Quaternion.identity);
            Parasol(b, new Vector3(x + 2.1f, yd, 945.95f), new Vector3(x, yd, 943.4f));
        }
        // the four planters at the deck's corners, a young pine in each (the forest's own cards)
        int n = 0;
        foreach (var (px, pz) in new[] { (TX0 + .75f, TZ0 + .75f), (TX1 - .75f, TZ0 + .75f), (TX0 + .75f, TZ1 - .75f), (TX1 - .75f, TZ1 - .75f) })
        {
            b.BoxMinMax(T.czern, px - .5f, px + .5f, yd, yd + .9f, pz - .5f, pz + .5f);
            b.BoxMinMax(T.sciolka, px - .44f, px + .44f, yd + .9f, yd + .92f, pz - .44f, pz + .44f);
            W.Kolizja(new Vector3(px, yd + .45f, pz) * K, new Vector3(1f, .9f, 1f) * K, Quaternion.identity);
            BraxChampionsLasKladow.Dodatkowe.Add(new BraxChampionsLasKladow.Drzewo { x = px, z = pz, y = yd + .9f, karta = n % 2 == 0 ? 7 : 6, skala = 2f + .1f * n, obrot = 37f * n, odcien = 0f });
            n++;
        }
    }

    /// <summary>A teak seat (a sofa of len m or an armchair), its back away from where it faces (yaw 0: facing -z), bouclé
    /// cushions: 85 cm deep, the seat 44 cm up, the back 80 cm, 10 cm teak arms.</summary>
    static void Sofa(BraxChampionsMeshBatch b, Vector3 c, float yaw, float len)
    {
        var q = Quaternion.Euler(0f, yaw, 0f);
        Vector3 P(float x, float yy, float z) => c + q * new Vector3(x, yy, z);
        float hw = len * .5f;
        b.Box(teak, P(0f, .2f, 0f), new Vector3(len, .16f, .85f), q);                                   // the frame
        foreach (float sx in new[] { -1f, 1f })
        {
            b.Box(teak, P(sx * (hw - .05f), .36f, 0f), new Vector3(.1f, .48f, .85f), q);                // the arms
            foreach (float sz in new[] { -1f, 1f }) b.Box(teak, P(sx * (hw - .08f), .06f, sz * .34f), new Vector3(.08f, .12f, .08f), q);   // the feet
        }
        b.RoundedBox(boucle, P(0f, .36f, -.06f), new Vector3(len - .22f, .16f, .66f), .04f, q);         // the seat cushion
        b.RoundedBox(boucle, P(0f, .62f, .31f), new Vector3(len - .22f, .4f, .16f), .05f, q);           // the back cushion
        b.Box(teak, P(0f, .55f, .395f), new Vector3(len - .2f, .5f, .06f), q);                         // the back's teak
        W.Kolizja(P(0f, .4f, 0f) * K, new Vector3(len, .8f, .85f) * K, q);
    }

    /// <summary>A 3 x 3 m cantilever parasol: its weighted base and pole at p, the arm over to the canopy's centre c.</summary>
    static void Parasol(BraxChampionsMeshBatch b, Vector3 p, Vector3 c)
    {
        b.BoxMinMax(T.czern, p.x - .45f, p.x + .45f, p.y, p.y + .08f, p.z - .45f, p.z + .45f);
        b.Cylinder(T.czern, p + Vector3.up * .08f, .045f, 2.82f, 10);
        var szczyt = new Vector3(c.x, p.y + 2.85f, c.z); var ramie = new Vector3(p.x, p.y + 2.88f, p.z);
        b.Tube(T.czern, ramie, szczyt + Vector3.up * .03f, .035f, 8);
        float e = 1.5f, ye = p.y + 2.55f;
        var r = new[] { new Vector3(c.x - e, ye, c.z - e), new Vector3(c.x + e, ye, c.z - e), new Vector3(c.x + e, ye, c.z + e), new Vector3(c.x - e, ye, c.z + e) };
        for (int k = 0; k < 4; k++)
        {
            var a = r[k]; var d = r[(k + 1) % 4];
            b.Triangle(plotno, szczyt, d, a); b.Triangle(plotno, szczyt, a, d);                          // the canvas, both sides
            b.Quad(plotno, a, a - Vector3.up * .12f, d - Vector3.up * .12f, d); b.Quad(plotno, d, d - Vector3.up * .12f, a - Vector3.up * .12f, a);   // its valance
        }
        W.Kolizja(new Vector3(p.x, p.y + 1.45f, p.z) * K, new Vector3(.3f, 2.9f, .3f) * K, Quaternion.identity);
    }
}
