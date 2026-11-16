using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using O = BraxChampionsKasynoObsluga;

// MIASTO MISTRZÓW - the casinos' entrance and service (owner 16.11 20:29: "Porządna recepcja, konkretna, na maksa, kurwa, nie
// wiem, szatnia tam powinna być ... internet patrz i pokaż, jak się robi luksus"), as the entrances of the real luxury
// casinos (Casino de Monte-Carlo, Wynn, Bellagio): the guest passes the security arches between brass posts with red velvet
// ropes, the RECEPCJA (the players club desk: a walnut and white-marble counter, the back wall with the BRAXHORN CASINO
// logo in a gold frame), the SZATNIA (the coat check: a counter and two rails of coats on wooden hangers), the KASA (the
// cashier's cage: a counter, glass with brass bars, three windows). The owner's signs and logo (16.11 20:49) 1:1 on black
// lacquered plaques; the pit signs hang over the tables. The service works: BraxChampionsKasynoObsluga.
public static partial class BraxChampionsKasynoSprzet
{
    // his sheet (16.11 20:49, 2172 x 724): the parts (x, y from the top-left, w, h), found by their transparency
    public const int ArkRecepcja = 0, ArkSzatnia = 1, ArkKasa = 2, ArkVip = 3, ArkLogo = 4, ArkMonetaA = 5, ArkMonetaR = 6,
        ArkBlackjack = 7, ArkBakarat = 8, ArkPoker = 9, ArkRuletka = 10;
    static readonly int[,] Arkusz = { { 749, 3, 652, 199 }, { 1464, 2, 657, 200 }, { 749, 221, 652, 209 }, { 1462, 219, 660, 211 }, { 23, 354, 396, 355 },
        { 43, 17, 319, 320 }, { 384, 17, 314, 320 }, { 448, 491, 421, 172 }, { 883, 491, 412, 171 }, { 1309, 491, 414, 171 }, { 1737, 491, 414, 171 } };
    static readonly string[] ArkNazwy = { "RECEPCJA", "SZATNIA", "KASA", "SALON VIP", "BRAXHORN CASINO", "BRAXHORN", "BRAXHORN", "BLACKJACK", "BAKARAT", "POKER", "RULETKA" };
    static Material[] szyldyArk; public static bool SzyldyWlasciciela;
    static Material marmur, aksamit, mosiadz, ciemnyOrzech, kasetonLed, plastikSzary, ledZielony;
    static Material[] plaszcze;
    static List<(Mesh, Material)> czKurtka;
    public static float ArkProporcja(int i) => (float)Arkusz[i, 2] / Arkusz[i, 3];

    static void WejscieMaterialy()
    {
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        SzyldyWlasciciela = src != null;
        if (src == null) BraxBuildQuality.Warn("DO POPRAWKI KASYNO_SZYLDY missing " + Wlasciciel + "/szyldy: the signs carry drawn names");
        szyldyArk = new Material[Arkusz.GetLength(0)];
        for (int i = 0; i < szyldyArk.Length; i++)
        {
            int w = 1024, h = Mathf.RoundToInt(1024f * Arkusz[i, 3] / Arkusz[i, 2]); var px = new Color32[w * h];
            BraxChampionsPiksele.Wypelnij(px, new Color32(10, 8, 9, 255));                // the black lacquered plaque under his sign
            if (src != null) Wklej(px, w, h, src, sw, sh, Arkusz[i, 0], sh - Arkusz[i, 1] - Arkusz[i, 3], Arkusz[i, 2], Arkusz[i, 3], 0, 0, w, h);
            else { Ramka(px, w, h, 8, 8, w - 8, h - 8, 6, Zloto); Tekst(px, w, h, ArkNazwy[i], w * .5f, h * .5f, h * .4f, w - 80, 0f, Zloto, 4f, new Color32(80, 10, 10, 255)); }
            szyldyArk[i] = Ekran("szyld_ark_" + i, Png("kasyno_szyld_" + ArkNazwy[i].ToLowerInvariant().Replace(' ', '_') + "_" + i, w, h, px, true), 1.0f);
        }
        marmur = BraxChampionsFoundation.LibraryMarbleWhite("kasyno_marmur_blat", new Color(.93f, .92f, .90f), 1.6f); marmur.SetFloat("_Roughness", .18f); Wnetrze(marmur);
        aksamit = BraxChampionsFoundation.Fotoskan("kasyno_aksamit_czerwony", "velour_velvet", .4f, new Color(.48f, .04f, .07f), BraxChampionsFoundation.MeanVelvet); Wnetrze(aksamit);
        mosiadz = Metal("mosiadz_polerowany", new Color(.96f, .76f, .42f), .18f);
        ciemnyOrzech = BraxChampionsFoundation.SourceMaterial("kasyno_orzech_ciemny", "smoked_walnut_veneer", 1.6f, new Color(.52f, .40f, .32f)); ciemnyOrzech.SetFloat("_Roughness", .40f); Wnetrze(ciemnyOrzech);
        kasetonLed = Swiatlo("led_cieply", new Color(1.6f, 1.15f, .62f));
        plastikSzary = Metal("bramka_szara", new Color(.30f, .31f, .33f), .45f, 0f);
        ledZielony = Swiatlo("led_zielony", new Color(.2f, 1.6f, .4f));
        // coats: wool in five colours (the photo-scanned cloth, tinted)
        Color[] kol = { new Color(.10f, .10f, .11f), new Color(.24f, .24f, .26f), new Color(.55f, .42f, .28f), new Color(.10f, .13f, .26f), new Color(.34f, .07f, .09f) };
        plaszcze = new Material[kol.Length];
        for (int i = 0; i < kol.Length; i++) { plaszcze[i] = BraxChampionsFoundation.Fotoskan("kasyno_plaszcz_" + i, "terlenka", .5f, kol[i], BraxChampionsFoundation.MeanTerlenka); Wnetrze(plaszcze[i]); }
        var b = new BraxChampionsMeshBatch(); Plaszcz(b, plaszcze[3], Vector3.zero, 0f); czKurtka = Czesc(b, "szatnia kurtka gracza");
    }

    // a coat on its wooden hanger, hanging from the rail at top (the rail along x, the coat across it)
    static void Plaszcz(BraxChampionsMeshBatch b, Material m, Vector3 top, float skret, float dlugosc = 1.04f)
    {
        var q = Quaternion.Euler(0f, skret, 0f);
        Vector3 P(float x, float y, float z) => top + q * new Vector3(x, y, z);
        b.Tube(ciemnyOrzech, P(0f, -.05f, -.21f), P(0f, -.05f, .21f), .008f, 6);                    // the hanger's bar
        b.Tube(chrom, P(0f, -.05f, 0f), P(0f, .012f, 0f), .003f, 5);                                         // its hook
        // the body: narrower and thinner at the shoulders, its hem as long as the coat (coats 11 cm apart never touch)
        float y0 = -.06f, y1 = -dlugosc;
        Vector3[] s = { P(-.032f, y0, -.20f), P(.032f, y0, -.20f), P(.032f, y0, .20f), P(-.032f, y0, .20f) };
        Vector3[] h = { P(-.045f, y1, -.26f), P(.045f, y1, -.26f), P(.045f, y1, .26f), P(-.045f, y1, .26f) };
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4; var mid = (s[i] + s[j] + h[i] + h[j]) * .25f; var outD = mid - (top + q * new Vector3(0f, (y0 + y1) * .5f, 0f));
            QuadPrzod(b, m, outD, s[i], s[j], h[j], h[i], new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0));
        }
        QuadPrzod(b, m, Vector3.up, s[0], s[1], s[2], s[3], Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
        QuadPrzod(b, m, Vector3.down, h[0], h[1], h[2], h[3], Vector2.zero, Vector2.right, Vector2.one, Vector2.up);
    }

    // a counter (front to +z of the frame): walnut, a gold kick band, gold fins, the white marble top with a warm light under it
    static void Lada(BraxChampionsMeshBatch b, Vector3 c, Quaternion q, float L, float D, float H)
    {
        Vector3 P(float x, float y, float z) => c + q * new Vector3(x, y, z);
        b.RoundedBox(ciemnyOrzech, P(0f, H * .5f - .025f, 0f), new Vector3(L, H - .05f, D), .015f, q, 2);
        b.Box(zlotoKola, P(0f, .06f, D * .5f + .004f), new Vector3(L - .04f, .08f, .012f), q);
        int n = Mathf.Max(3, Mathf.RoundToInt(L / .42f));
        for (int i = 0; i <= n; i++) b.Box(zlotoKola, P(-L * .5f + .08f + (L - .16f) * i / n, H * .52f, D * .5f + .006f), new Vector3(.024f, H * .62f, .012f), q);
        b.Box(kasetonLed, P(0f, H - .062f, D * .5f + .03f), new Vector3(L - .06f, .012f, .012f), q);
        b.RoundedBox(marmur, P(0f, H - .025f, .04f), new Vector3(L + .10f, .05f, D + .16f), .01f, q, 1);
    }

    /// <summary>The RECEPCJA at c (floor), its counter's front to the frame's +z (yaw): the counter, the hostess behind it,
    /// the back wall with the owner's logo and his RECEPCJA sign (wall = false: a podium with the sign on a brass pole).</summary>
    public static void Recepcja(Kasyno k, Vector3 c, float yaw, float L, bool sciana)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; Vector3 P(float x, float y, float z) => c + q * new Vector3(x, y, z);
        Lada(b, c, q, L, .70f, 1.10f);
        W.Kolizja(P(0f, .55f, 0f), new Vector3(L + .1f, 1.1f, .86f), q);
        if (sciana)
        {
            float sw = L + 2.0f, z = -1.6f;
            b.RoundedBox(ciemnyOrzech, P(0f, 1.80f, z), new Vector3(sw, 3.60f, .14f), .02f, q, 2);
            foreach (float s in new[] { -1f, 1f }) b.Box(kasetonLed, P(s * (sw * .5f - .12f), 1.8f, z + .075f), new Vector3(.03f, 3.2f, .012f), q);
            // the logo in a gold frame, the sign over it
            float lw = 1.30f, lh = lw / ArkProporcja(ArkLogo);
            b.Box(zlotoKola, P(0f, 1.95f, z + .085f), new Vector3(lw + .10f, lh + .10f, .03f), q);
            Obraz(b, szyldyArk[ArkLogo], P(0f, 1.95f, z + .10f + Przed), lw, lh, q);
            float rw = 2.1f; Obraz(b, szyldyArk[ArkRecepcja], P(0f, 3.15f, z + .07f + Przed), rw, rw / ArkProporcja(ArkRecepcja), q);
            W.Kolizja(P(0f, 1.8f, z), new Vector3(sw, 3.6f, .14f), q);
        }
        else SzyldNaSlupku(k, P(L * .5f + .45f, 0f, -.20f), yaw, ArkRecepcja, 1.10f, 2.35f);
        W.Obsada("recepcjonistka kasyna", P(0f, 0f, -.75f), yaw);
        Punkt(k, O.Recepcja, P(0f, 0f, .95f), null);
    }

    /// <summary>The SZATNIA at c (floor): the counter to +z, the attendant, behind it rails of coats on wooden hangers (rails:
    /// how many, rack = the depth behind the counter) and the back wall with the owner's SZATNIA sign; the player's jacket
    /// on the front hook while it hangs there.</summary>
    public static void Szatnia(Kasyno k, Vector3 c, float yaw, float L, int rails, bool sciana)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; Vector3 P(float x, float y, float z) => c + q * new Vector3(x, y, z);
        Lada(b, c, q, L, .60f, 1.05f);
        W.Kolizja(P(0f, .52f, 0f), new Vector3(L + .1f, 1.05f, .76f), q);
        var los = new System.Random(31 + (int)(c.x * 7f + c.z * 13f));
        float rl = L + .4f;
        for (int r = 0; r < rails; r++)
        {
            float z = -.95f - .75f * r, y = 1.78f;
            b.Tube(chrom, P(-rl * .5f, y, z), P(rl * .5f, y, z), .014f, 10);
            foreach (float s in new[] { -1f, 1f })
            {
                b.Tube(chrom, P(s * rl * .5f, 0f, z), P(s * rl * .5f, y + .02f, z), .02f, 10);
                b.Cylinder(chrom, P(s * rl * .5f, 0f, z), .14f, .015f, 14, q, true);
            }
            for (float x = -rl * .5f + .12f; x < rl * .5f - .20f; x += .11f)
                if (los.NextDouble() < .78) Plaszcz(b, plaszcze[los.Next(plaszcze.Length)], P(x, y, z), yaw + (float)(los.NextDouble() * 4 - 2), .92f + .02f * los.Next(9));
            W.Kolizja(P(0f, .9f, z), new Vector3(rl, 1.8f, .55f), q);
        }
        // the player's jacket on the first hook of the front rail (the game shows it while it hangs there)
        var kurtka = new GameObject("szatnia | kurtka gracza (na wieszaku)").transform; kurtka.SetParent(k.root, false);
        kurtka.localPosition = P(rl * .5f - .08f, 1.78f, -.95f); kurtka.localRotation = q; Wstaw(czKurtka, kurtka, "kurtka");
        if (sciana)
        {
            float z = -1.0f - .75f * rails, sw = L + 1.4f;
            b.RoundedBox(ciemnyOrzech, P(0f, 1.6f, z), new Vector3(sw, 3.2f, .14f), .02f, q, 2);
            float rw = 2.0f; Obraz(b, szyldyArk[ArkSzatnia], P(0f, 2.75f, z + .07f + Przed), rw, rw / ArkProporcja(ArkSzatnia), q);
            W.Kolizja(P(0f, 1.6f, z), new Vector3(sw, 3.2f, .14f), q);
        }
        else SzyldNaSlupku(k, P(L * .5f + .45f, 0f, -.20f), yaw, ArkSzatnia, 1.10f, 2.35f);
        W.Obsada("szatniarz", P(-L * .25f, 0f, -.55f), yaw);
        Punkt(k, O.Szatnia, P(0f, 0f, .90f), kurtka);
    }

    /// <summary>The KASA at c (floor): the cashier's cage with its windows to +z - the counter, glass with brass bars up to
    /// 2.3 m, the transaction slots, the fascia with the owner's KASA sign, the back wall; the cashiers.</summary>
    public static void Kasa(Kasyno k, Vector3 c, float yaw, int okna)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; Vector3 P(float x, float y, float z) => c + q * new Vector3(x, y, z);
        float L = 1.5f * okna;
        Lada(b, c, q, L, .70f, 1.10f);
        // the glass over the counter, brass bars, a brass frame; under the glass the windows' slots
        float g0 = 1.42f, g1 = 2.32f, gz = .02f;
        for (int i = 0; i < okna; i++)
        {
            float x0 = -L * .5f + i * 1.5f + .06f, x1 = x0 + 1.38f;
            Obraz(b, szklo, P((x0 + x1) * .5f, (g0 + g1) * .5f, gz), x1 - x0, g1 - g0, q);
            for (float x = x0 + .10f; x < x1 - .05f; x += .11f) b.Tube(mosiadz, P(x, g0, gz + .03f), P(x, g1, gz + .03f), .006f, 6);
            b.Tube(mosiadz, P(x0, (g0 + g1) * .5f, gz + .03f), P(x1, (g0 + g1) * .5f, gz + .03f), .006f, 6);
            b.Box(mosiadz, P((x0 + x1) * .5f, g0 - .02f, gz), new Vector3(x1 - x0, .04f, .06f), q);
            b.Box(czern, P((x0 + x1) * .5f, 1.16f, .30f), new Vector3(.42f, .03f, .30f), q);          // the slot's tray
        }
        for (int i = 0; i <= okna; i++) b.Box(ciemnyOrzech, P(-L * .5f + i * 1.5f, (g0 + g1) * .5f + .05f, gz), new Vector3(.10f, g1 - g0 + .1f, .12f), q);
        // the fascia and the sign; the back wall closes the cage
        b.RoundedBox(ciemnyOrzech, P(0f, 2.62f, -.25f), new Vector3(L + .2f, .60f, .90f), .02f, q, 2);
        b.Box(zlotoKola, P(0f, 2.33f, .20f), new Vector3(L + .2f, .03f, .03f), q);
        float rw = Mathf.Min(2.0f, L - .2f); Obraz(b, szyldyArk[ArkKasa], P(0f, 2.62f, .20f + Przed), rw, Mathf.Min(.56f, rw / ArkProporcja(ArkKasa)), q);
        b.RoundedBox(ciemnyOrzech, P(0f, 1.45f, -1.6f), new Vector3(L + .2f, 2.9f, .14f), .02f, q, 2);
        foreach (float s in new[] { -1f, 1f }) b.RoundedBox(ciemnyOrzech, P(s * (L * .5f + .03f), 1.45f, -.8f), new Vector3(.14f, 2.9f, 1.6f), .02f, q, 2);
        W.Kolizja(P(0f, 1.45f, -.75f), new Vector3(L + .3f, 2.9f, 1.9f), q);
        for (int i = 0; i < okna; i += 2) W.Obsada("kasjerka", P(-L * .5f + .75f + i * 1.5f, 0f, -.60f), yaw);
        for (int i = 0; i < okna; i++) Punkt(k, O.Kasa, P(-L * .5f + .75f + i * 1.5f, 0f, .95f), null);
    }

    /// <summary>The security at the entrance: two walk-through metal detector arches across the frame's x (their passage
    /// along z) at c, the guard beside them.</summary>
    public static void Bramka(Kasyno k, Vector3 c, float yaw)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; Vector3 P(float x, float y, float z) => c + q * new Vector3(x, y, z);
        foreach (float cx in new[] { -1.15f, 1.15f })
        {
            foreach (float s in new[] { -1f, 1f }) b.RoundedBox(plastikSzary, P(cx + s * .45f, 1.06f, 0f), new Vector3(.10f, 2.12f, .58f), .02f, q, 2);
            b.RoundedBox(plastikSzary, P(cx, 2.20f, 0f), new Vector3(1.00f, .18f, .62f), .03f, q, 2);
            b.Box(ledZielony, P(cx, 2.20f, .315f), new Vector3(.30f, .04f, .01f), q);
            b.Box(ledZielony, P(cx, 2.20f, -.315f), new Vector3(.30f, .04f, .01f), q);
            foreach (float s in new[] { -1f, 1f }) W.Kolizja(P(cx + s * .45f, 1.06f, 0f), new Vector3(.10f, 2.12f, .58f), q);
        }
        W.Obsada("ochroniarz kasyna", P(2.4f, 0f, .4f), yaw + 180f);
    }

    /// <summary>Brass posts with red velvet ropes between them (the rope sags 9 cm), posts at the given floor points.</summary>
    public static void Liny(Kasyno k, Vector3[] slupki)
    {
        var b = k.body;
        foreach (var p in slupki)
        {
            b.Cylinder(mosiadz, p, .17f, .03f, 18, Quaternion.identity, true, false, .15f);
            b.Tube(mosiadz, p + new Vector3(0f, .03f, 0f), p + new Vector3(0f, .92f, 0f), .025f, 10);
            b.Lathe(mosiadz, p + new Vector3(0f, .92f, 0f), Profil(.04f, 8), 12);
        }
        for (int i = 0; i + 1 < slupki.Length; i++)
        {
            var a = slupki[i] + new Vector3(0f, .86f, 0f); var c = slupki[i + 1] + new Vector3(0f, .86f, 0f);
            if ((c - a).magnitude > 3.2f) continue;                                          // a gap: the passage
            b.Tube(aksamit, a, c, .018f, 8, 10, .09f);
            W.Kolizja((a + c) * .5f - new Vector3(0f, .4f, 0f), new Vector3((c - a).magnitude, .9f, .10f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, c - a).normalized, Vector3.up));
        }
    }

    /// <summary>The owner's sign on a brass pole (a table's sign, a desk's sign): both faces, its bottom at y0 + h.</summary>
    public static void SzyldNaSlupku(Kasyno k, Vector3 at, float yaw, int ark, float w, float wys)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; float h = w / ArkProporcja(ark);
        b.Cylinder(mosiadz, at, .16f, .025f, 16, Quaternion.identity, true, false, .14f);
        b.Tube(mosiadz, at + new Vector3(0f, .025f, 0f), at + new Vector3(0f, wys, 0f), .02f, 10);
        var c = at + new Vector3(0f, wys + h * .5f, 0f);
        b.RoundedBox(czern, c, new Vector3(w + .06f, h + .06f, .04f), .01f, q, 1);
        Obraz(b, szyldyArk[ark], c + q * new Vector3(0f, 0f, .02f + Przed), w, h, q);
        Obraz(b, szyldyArk[ark], c + q * new Vector3(0f, 0f, -.02f - Przed), w, h, q * Quaternion.Euler(0f, 180f, 0f));
    }

    /// <summary>A pit sign hanging over the tables: the owner's sign on the four faces of a black box with gold edges, on
    /// four cables up to hangTo.</summary>
    public static void SzyldWiszacy(Kasyno k, Vector3 c, int ark, float w, float hangTo)
    {
        var b = k.body; float h = w / ArkProporcja(ark), hw = w * .5f + .02f;
        b.RoundedBox(czern, c, new Vector3(2f * hw, h + .08f, 2f * hw), .02f, Quaternion.identity, 1);
        foreach (float s in new[] { -1f, 1f })
            b.BoxMinMax(zlotoKola, c.x - hw - .02f, c.x + hw + .02f, c.y + s * (h * .5f + .04f) - .025f, c.y + s * (h * .5f + .04f) + .025f, c.z - hw - .02f, c.z + hw + .02f);
        for (int f = 0; f < 4; f++) { var q = Quaternion.Euler(0f, 90f * f, 0f); Obraz(b, szyldyArk[ark], c + q * new Vector3(0f, 0f, hw + Przed), w, h, q); }
        foreach (var o in new[] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) })
        {
            var a = c + new Vector3(o.x * (hw - .1f), h * .5f + .065f, o.z * (hw - .1f));
            if (hangTo > a.y + .05f) b.Tube(stal, a, new Vector3(a.x, hangTo, a.z), .004f, 6);
        }
    }

    static void Punkt(Kasyno k, int rodzaj, Vector3 at, Transform kurtka)
    {
        var t = new GameObject("obsługa | " + (rodzaj == O.Recepcja ? "recepcja" : rodzaj == O.Szatnia ? "szatnia" : "kasa") + " (miejsce gościa)").transform;
        t.SetParent(k.root, false); t.localPosition = at;
        k.punkty.Add(new O.Punkt { rodzaj = rodzaj, miejsce = t, kurtka = kurtka });
    }
}
