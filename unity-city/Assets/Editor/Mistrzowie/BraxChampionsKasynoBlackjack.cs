using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - a playable blackjack table (owner 16.11: "blackjack grywalny w obu kasynach"; the game is
// BraxChampionsBlackjack). The table as the real ones (Gaming Partners / Paulson layouts): a half-oval 2.2 x 1.05 m, the felt
// at .80 m, a padded black leather armrest round the players' arc, the walnut dealer's edge with the steel chip tray let into
// it, the shoe on the dealer's left, the discard holder on his right, five betting circles. The felt's print is IN the felt
// (its own texture, not laid over it): the gold circles, "BLACKJACK PŁACI 3 DO 2", "KRUPIER STOI NA KAŻDYM 17" and the owner's
// BRAXHORN CASINO logo (his art 1:1 from his sign sheet). The cards are his card atlas (BraxChampionsKasynoGry), the chips
// his chips (Zeton). The table's frame: origin on the floor under the dealer's edge, +z to the players.
public static partial class BraxChampionsKasynoSprzet
{
    public const float BjA = 1.10f, BjB = .95f, BjTop = .80f, BjRail = .11f, BjRailH = .075f, BjKolo = .70f;
    static Material filcBj, kartyStol, akryl;

    static void BlackjackMaterialy()
    {
        if (filcBj) return;
        filcBj = BraxChampionsHarbour.Surface("kasyno_blackjack_sukno", Color.white, .92f, 0f);
        filcBj.SetTexture("_MainTex", Png("kasyno_blackjack_sukno", 2048, 1024, Filc(), true)); Wnetrze(filcBj);
        kartyStol = BraxChampionsHarbour.Surface("kasyno_karty_na_stole", Color.white, .55f, 0f);
        var atlas = kartyMat ? kartyMat.GetTexture("_MainTex") : null;                // the atlas Ekran() set (a preview has no file)
        if (atlas) kartyStol.SetTexture("_MainTex", atlas);
        else BraxBuildQuality.Warn("DO POPRAWKI KASYNO_BLACKJACK_KARTY the card atlas is missing: the cards show white");
        Wnetrze(kartyStol);
        akryl = BraxChampionsHarbour.Surface("kasyno_akryl", new Color(.85f, .9f, .95f, .25f), .05f, 0f, null, true);
    }

    // the betting circles on the felt (the table's frame): five on an arc inside the rail
    public static Vector3 BjMiejsce(int k)
    {
        float t = (28f + k * 31f) * Mathf.Deg2Rad;                                      // 28 .. 152 degrees: clear of the shoe and the discards
        return new Vector3(BjA * BjKolo * Mathf.Cos(t), BjTop, BjB * BjKolo * Mathf.Sin(t));
    }

    /// <summary>A blackjack table at `at` (the floor), turned by yaw (its players' side to local +z).</summary>
    public static void Blackjack(Kasyno k, Vector3 at, float yaw)
    {
        BlackjackMaterialy();
        var q = Quaternion.Euler(0f, yaw, 0f); int idx = k.blackjack.Count;
        var miejsca = new Vector3[5]; for (int m = 0; m < 5; m++) miejsca[m] = BjMiejsce(m);
        var siedziska = StolKarciany(k, at, yaw, BjA, BjB, filcBj, miejsca, "BLACKJACK");
        // the stand: the table, its circles, the chip stack on each (shown while bet), the dealer's spot
        var stol = new GameObject("blackjack " + (idx + 1) + " | " + k.nazwa).transform;
        stol.SetParent(k.root, false); stol.localPosition = at; stol.localRotation = q;
        var zet = new Transform[5];
        for (int m = 0; m < 5; m++)
        {
            var z = new GameObject("żeton gracza " + (m + 1)).transform; z.SetParent(stol, false);
            z.localPosition = miejsca[m] + new Vector3(0f, .0008f, 0f); Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[m] = z;
        }
        k.blackjack.Add(new BraxChampionsBlackjack.Stol { stol = stol, but = new Vector3(-(BjA - .32f), BjTop + .02f, .24f), miejsca = miejsca, krupier = new Vector3(-.12f, BjTop, .215f), zetony = zet, siedziska = siedziska });
        W.Obsada("krupier blackjacka", at + q * new Vector3(0f, 0f, -.62f), yaw);
    }

    /// <summary>The card table both games use (blackjack, bakarat): a half-oval A x B (its players' arc to local +z, the
    /// dealer's straight edge at z 0), the felt `filc` mapped over x -A..A, z -.1..A - .1 (a 2:1 picture), the leather
    /// armrest, the walnut apron and dealer's edge with the steel chip tray full of his chips, the shoe (dealer's left) and
    /// the discard holder (his right), two pedestals, a chair behind each player's spot. Its seat (owner 23.11 17:35 "siadasz
    /// przy pokerze ... widać tylko stół, karty") opens the table's game at that spot (BraxChampionsStanowisko): returned in the
    /// spots' order, labelled with the game's name.</summary>
    static BraxSeat[] StolKarciany(Kasyno k, Vector3 at, float yaw, float A, float B, Material filc, Vector3[] miejsca, string gra)
    {
        var siedziska = new BraxSeat[miejsca.Length]; int nr = 0;
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body;
        Vector3 L(float x, float y, float z) => at + q * new Vector3(x, y, z);
        Vector2 Arc(float t, float s) => new Vector2(A * s * Mathf.Cos(t), B * s * Mathf.Sin(t));
        Vector2 Out(float t) { var v = new Vector2(Mathf.Cos(t) / A, Mathf.Sin(t) / B); return v.normalized; }
        Vector2 UV(float x, float z) => new Vector2((x + A) / (2f * A), (z + .1f) / A);
        const int N = 36; float kIn = 1f - BjRail / A * .9f;
        // the felt: a fan from the dealer's edge, its print by its position; the walnut underside
        for (int i = 0; i < N; i++)
        {
            float t0 = Mathf.PI * i / N, t1 = Mathf.PI * (i + 1) / N; var a0 = Arc(t0, kIn); var a1 = Arc(t1, kIn);
            Vector3 c = L(0f, BjTop, 0f), p0 = L(a0.x, BjTop, a0.y), p1 = L(a1.x, BjTop, a1.y);
            if (Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), Vector3.up) > 0f) b.TriangleUV(filc, c, p0, p1, UV(0f, 0f), UV(a0.x, a0.y), UV(a1.x, a1.y));   // up
            else b.TriangleUV(filc, c, p1, p0, UV(0f, 0f), UV(a1.x, a1.y), UV(a0.x, a0.y));
            var b0 = Arc(t0, 1f); var b1 = Arc(t1, 1f);
            Vector3 d = L(0f, BjTop - .16f, 0f), e0 = L(b0.x, BjTop - .16f, b0.y), e1 = L(b1.x, BjTop - .16f, b1.y);
            if (Vector3.Dot(Vector3.Cross(e0 - d, e1 - d), Vector3.up) < 0f) b.Triangle(orzech, d, e0, e1); else b.Triangle(orzech, d, e1, e0);   // down
        }
        // the padded armrest: a half-round roll of black leather on the rim; the walnut apron under it
        b.Surface(skora, (u, v) => { float t = Mathf.PI * u, ph = Mathf.PI * v; var c = Arc(t, 1f - BjRail * .5f / A); var o = Out(t);
            float r = -Mathf.Cos(ph) * BjRail * .5f, h = Mathf.Sin(ph) * BjRailH; return L(c.x + o.x * r, BjTop - .005f + h, c.y + o.y * r); }, N, 6);
        b.Surface(orzech, (u, v) => { float t = Mathf.PI * u; var c = Arc(t, 1f); return L(c.x, BjTop - .16f + v * .155f, c.y); }, N, 1, true);
        // the dealer's edge: walnut, the steel chip tray let into it with his chips in its rows; the pedestals
        b.RoundedBox(orzech, L(0f, BjTop - .08f, -.08f), new Vector3(2f * A + .02f, .16f, .16f), .02f, q, 2);
        b.RoundedBox(stal, L(0f, BjTop + .006f, -.07f), new Vector3(.64f, .014f, .12f), .004f, q, 2);
        for (int r = 0; r < 8; r++) for (int s = 0; s < 5; s++)
            Zeton(b, r % 5, L(-.27f + r * .077f, BjTop + .013f + s * ZetonH, -.07f));
        foreach (float sx in new[] { -1f, 1f }) b.RoundedBox(czern, L(sx * A * .5f, (BjTop - .16f) * .5f, B * .32f), new Vector3(.16f, BjTop - .16f, B * .48f), .02f, q, 2);
        // the shoe (dealer's left, its mouth to the felt) and the clear discard holder (his right)
        var shoe = L(-(A - .32f), BjTop, .12f);
        b.RoundedBox(czern, shoe + q * new Vector3(0f, .055f, -.03f), new Vector3(.14f, .11f, .22f), .012f, q, 2);
        b.Box(czern, shoe + q * new Vector3(0f, .03f, .10f), new Vector3(.14f, .06f, .05f), q * Quaternion.Euler(25f, 0f, 0f));
        b.Box(akryl, L(A - .32f, BjTop + .05f, .10f), new Vector3(.12f, .10f, .16f), q);
        // the players' chairs: chrome pedestal, black leather seat and low back, behind each spot beyond the rail; their seats
        // (the leather top at .73 m, facing the dealer) open the game
        foreach (var c in miejsca)
        {
            var o = new Vector2(c.x / A, c.z / B).normalized; var rim = Arc(Mathf.Atan2(c.z / B, c.x / A), 1f);
            var p = new Vector3(rim.x, 0f, rim.y) + new Vector3(o.x, 0f, o.y) * .34f;
            var cq = q * Quaternion.LookRotation(new Vector3(-o.x, 0f, -o.y), Vector3.up); var cp = at + q * p;
            b.Cylinder(chrom, cp, .23f, .02f, 24, Quaternion.identity, true);
            b.Cylinder(chrom, cp, .03f, .66f, 12, Quaternion.identity, false);
            b.Cylinder(skora, cp + Vector3.up * .64f, .22f, .09f, 24, Quaternion.identity, true, true);
            b.RoundedBox(skora, cp + cq * new Vector3(0f, .92f, -.20f), new Vector3(.40f, .30f, .06f), .025f, cq, 2);
            b.Tube(chrom, cp + cq * new Vector3(0f, .70f, -.17f), cp + cq * new Vector3(0f, .80f, -.20f), .012f, 8);
            W.Kolizja(cp + Vector3.up * .45f, new Vector3(.36f, .9f, .36f), Quaternion.identity);
            siedziska[nr++] = W.Siedzisko(cp + Vector3.up * KrzesloSiedzisko, cq * Vector3.forward, gra);
        }
        W.Kolizja(L(0f, BjTop * .5f, B * .45f), new Vector3(2f * A, BjTop, B + .16f), q);
        return siedziska;
    }
    public const float KrzesloSiedzisko = .73f;

    // ---- the felt's print, composed as the player sees it (the dealer at the top) and turned over to the table's uv
    static Color32[] Filc()
    {
        const int w = 2048, h = 1024; var px = new Color32[w * h];
        var los = new System.Random(2031);
        for (int i = 0; i < px.Length; i++)                                                 // the green cloth, its fibres
        {
            int n = los.Next(-7, 8), y = i / w;
            px[i] = new Color32((byte)Mathf.Clamp(14 + n, 0, 255), (byte)Mathf.Clamp(78 + n + (y % 3 == 0 ? 2 : 0), 0, 255), (byte)Mathf.Clamp(52 + n, 0, 255), 255);
        }
        // player view: X right = the table's -x, Y up (from the bottom row) = the table's -z; 1 px = 1.074 mm
        const float A = BjA; float X(float x) => (A - x) / (2f * A) * w; float Y(float z) => (A - .1f - z) / A * h;
        const float mm = w / (2000f * A);
        for (int m = 0; m < 5; m++)                                                        // the five gold circles
        {
            var c = BjMiejsce(m); Pierscien(px, w, h, X(c.x), Y(c.z), 52f * mm, 4.2f * mm, Zloto);
        }
        Tekst(px, w, h, "BLACKJACK PŁACI 3 DO 2", X(0f), Y(.305f), 34f * mm, 760f * mm, 0f, Zloto);
        Tekst(px, w, h, "KRUPIER STOI NA KAŻDYM 17", X(0f), Y(.345f), 20f * mm, 560f * mm, 0f, new Color32(236, 232, 214, 255));
        // his BRAXHORN CASINO logo (his sign sheet, 1:1), 112 mm wide at the middle of the felt
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        if (src != null)
        {
            int lw = Mathf.RoundToInt(112f * mm), lh = Mathf.RoundToInt(lw / ArkProporcja(ArkLogo));
            Wklej(px, w, h, src, sw, sh, Arkusz[ArkLogo, 0], sh - Arkusz[ArkLogo, 1] - Arkusz[ArkLogo, 3], Arkusz[ArkLogo, 2], Arkusz[ArkLogo, 3],
                Mathf.RoundToInt(X(0f) - lw * .5f), Mathf.RoundToInt(Y(.45f) - lh * .5f), lw, lh);
        }
        // turned over: texture u along the table's +x, v along +z
        System.Array.Reverse(px);
        return px;
    }
    static void Pierscien(Color32[] px, int w, int h, float cx, float cy, float r, float t, Color32 col)
    {
        int x0 = Mathf.Max(0, (int)(cx - r - 2)), x1 = Mathf.Min(w - 1, (int)(cx + r + 2)), y0 = Mathf.Max(0, (int)(cy - r - 2)), y1 = Mathf.Min(h - 1, (int)(cy + r + 2));
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            float d = Mathf.Abs(Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy)) - (r - t * .5f)) - t * .5f;
            if (d > .5f) continue; float a = Mathf.Clamp01(.5f - d);
            var o = px[y * w + x]; px[y * w + x] = new Color32((byte)Mathf.Lerp(o.r, col.r, a), (byte)Mathf.Lerp(o.g, col.g, a), (byte)Mathf.Lerp(o.b, col.b, a), 255);
        }
    }
}
