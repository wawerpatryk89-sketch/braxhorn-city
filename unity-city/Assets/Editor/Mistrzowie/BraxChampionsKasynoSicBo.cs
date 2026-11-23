using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the SIC BO table (owner 16.11, the casinos' tables; the game BraxChampionsSicBo). A walnut table 2.4 x 1.1 m
// with a leather armrest on the players' sides, the felt's print IN the felt (the boxes MAŁE 4-10, DOWOLNA TRÓJKA, DUŻE 11-17 with
// their odds, his BRAXHORN CASINO logo), and on the dealer's side the shaker: a black base with a gold ring under a glass dome,
// three casino dice in it (ivory, the one and the four red). Frame: origin on the floor under the dealer's edge, +z the players.
public static partial class BraxChampionsKasynoSprzet
{
    public const float SbA = 1.2f, SbB = 1.1f, SbKostka = .025f, SbKopula = .14f, SbDno = .068f;
    static Material filcSb, kostkaMat;
    public static float SbPoleX(int i) => .75f - i * .75f;                              // MAŁE on the players' left, DUŻE on their right

    public static void SicBo(Kasyno k, Vector3 at, float yaw)
    {
        BlackjackMaterialy();
        if (!filcSb)
        {
            filcSb = BraxChampionsHarbour.Surface("kasyno_sicbo_sukno", Color.white, .92f, 0f); filcSb.SetTexture("_MainTex", Png("kasyno_sicbo_sukno", 2048, 1024, FilcSicBo(), true)); Wnetrze(filcSb);
            kostkaMat = BraxChampionsHarbour.Surface("kasyno_kostki", Color.white, .25f, 0f); kostkaMat.SetTexture("_MainTex", Png("kasyno_kostki", 384, 256, KostkiAtlas(), true)); Wnetrze(kostkaMat);
        }
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body; const float top = BjTop;
        Vector3 L(float x, float y, float z) => at + q * new Vector3(x, y, z);
        // the table: a black plinth, the walnut body, the felt, the leather armrest round the players' three sides
        b.RoundedBox(czern, L(0f, .06f, SbB * .5f), new Vector3(2f * SbA - .3f, .12f, SbB - .3f), .01f, q, 1);
        b.RoundedBox(orzech, L(0f, (top - .03f + .12f) * .5f, SbB * .5f), new Vector3(2f * SbA, top - .15f, SbB), .02f, q, 2);          // .12 .. .77, the felt over it
        Vector2 UV(float x, float z) => new Vector2((x + SbA) / (2f * SbA), (z + .1f) / SbA);
        float fx = SbA - .06f, z0 = .02f, z1 = SbB - .06f;
        b.QuadUV(filcSb, L(-fx, top, z0), L(-fx, top, z1), L(fx, top, z1), L(fx, top, z0), UV(-fx, z0), UV(-fx, z1), UV(fx, z1), UV(fx, z0));
        b.RoundedBox(skora, L(0f, top + .02f, SbB - .03f), new Vector3(2f * SbA + .06f, .07f, .10f), .03f, q, 3);
        foreach (float sx in new[] { -1f, 1f }) b.RoundedBox(skora, L(sx * (SbA - .02f), top + .02f, .52f), new Vector3(.10f, .07f, 1.0f), .03f, q, 3);   // z .02 .. 1.02, to the players' roll
        b.RoundedBox(orzech, L(0f, top - .005f, -.01f), new Vector3(2f * SbA, .05f, .06f), .01f, q, 2);                       // the dealer's edge, .775 .. .825
        W.Kolizja(L(0f, top * .5f, SbB * .5f), new Vector3(2f * SbA + .06f, top, SbB + .06f), q);
        // the shaker on the dealer's side: its base, the gold ring, the glass dome
        var kp = L(0f, top, .22f);
        b.Cylinder(czern, kp, .18f, .05f, 40, q, true);
        b.Cylinder(zlotoKola, kp + Vector3.up * .05f, .155f, .012f, 40, q, true);
        b.Cylinder(czern, kp + Vector3.up * .062f, SbKopula + .004f, .006f, 40, q, true);
        var prof = new Vector2[13]; for (int i = 0; i <= 12; i++) { float a = i / 12f * Mathf.PI * .5f; prof[i] = new Vector2(SbKopula * Mathf.Cos(a), SbKopula * Mathf.Sin(a)); }
        b.Lathe(akryl, kp + Vector3.up * SbDno, prof, 40, q, false, false);
        // the stand and the dice (children of the dome's floor, the game moves them)
        var stol = new GameObject("SIC BO | " + k.nazwa).transform; stol.SetParent(k.root, false); stol.localPosition = at; stol.localRotation = q;
        var dno = new GameObject("kopuła | dno").transform; dno.SetParent(stol, false); dno.localPosition = new Vector3(0f, top + SbDno, .22f);
        var kostki = new Transform[3]; var spo = new[] { new Vector3(-.045f, 0f, .02f), new Vector3(.045f, 0f, .025f), new Vector3(0f, 0f, -.045f) };
        for (int i = 0; i < 3; i++)
        {
            var d = new GameObject("kostka " + (i + 1)).transform; d.SetParent(dno, false); d.localPosition = spo[i] + Vector3.up * (SbKostka * .5f);
            d.localRotation = Quaternion.Euler(0f, 37f * i, 0f);
            var db = new BraxChampionsMeshBatch(); Kostka(db, SbKostka); db.Build(d, "kostka", Out + "/Meshes", false, 1f); kostki[i] = d;
        }
        var pola = new Vector3[3]; var zet = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            pola[i] = new Vector3(SbPoleX(i), top, .76f);
            var z = new GameObject("żeton " + BraxChampionsSicBo.Pola[i]).transform; z.SetParent(stol, false);
            // z .64, between the box's name and its range (23.11: this line's chip, hiding and keeping had slipped into the
            // comment - the chips were never made and the game never showed a bet)
            z.localPosition = pola[i] + new Vector3(0f, .002f, -.12f);
            Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[i] = z;
        }
        var gra = k.root.gameObject.AddComponent<BraxChampionsSicBo>();
        gra.stol = stol; gra.pola = pola; gra.zetony = zet; gra.kostki = kostki; gra.spoczynek = spo; gra.kostkaBok = SbKostka; gra.kopulaR = SbKopula; gra.stawka = Stawka;
        W.Obsada("krupier sic bo", L(0f, 0f, -.6f), yaw);
        Debug.Log("KASYNO_SICBO " + k.nazwa + " dice=3 bets=MALE/TROJKA/DUZE small_big=1:1(lose_on_triple) any_triple=30:1 game=BraxChampionsSicBo");
    }

    // a die: a cube centred on its transform, each face its value from the atlas (3 x 2 faces of 128 px), the faces as
    // BraxChampionsSicBo.Sciany (1 up, 2 forward, 3 right, 4 left, 5 back, 6 down: opposite faces sum to 7)
    static void Kostka(BraxChampionsMeshBatch b, float bok)
    {
        float h = bok * .5f;
        for (int v = 1; v <= 6; v++)
        {
            var n = BraxChampionsSicBo.Sciany[v - 1];
            var up = Mathf.Abs(n.y) > .5f ? Vector3.forward : Vector3.up;
            var r = Vector3.Cross(n, up) * h; var u = up * h; var c = n * h;
            int col = (v - 1) % 3, row = (v - 1) / 3; float u0 = col / 3f, u1 = (col + 1) / 3f, v0 = 1f - (row + 1) / 2f, v1 = 1f - row / 2f;
            b.QuadUV(kostkaMat, c - r - u, c - r + u, c + r + u, c + r - u, new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
        }
    }
    static Color32[] KostkiAtlas()
    {
        const int w = 384, h = 256, F = 128; var px = new Color32[w * h];
        var kosc = new Color32(242, 236, 220, 255); for (int i = 0; i < px.Length; i++) px[i] = kosc;
        float[][] oczka = { new[] { 0f, 0f }, new[] { -1f, 1f, 1f, -1f }, new[] { -1f, 1f, 0f, 0f, 1f, -1f }, new[] { -1f, 1f, 1f, 1f, -1f, -1f, 1f, -1f },
            new[] { -1f, 1f, 1f, 1f, 0f, 0f, -1f, -1f, 1f, -1f }, new[] { -1f, 1f, 1f, 1f, -1f, 0f, 1f, 0f, -1f, -1f, 1f, -1f } };
        for (int v = 1; v <= 6; v++)
        {
            int col = (v - 1) % 3, row = (v - 1) / 3; float cx = col * F + F * .5f, cy = h - (row * F + F * .5f);
            var kol = (v == 1 || v == 4) ? new Color32(196, 18, 30, 255) : new Color32(18, 16, 16, 255);    // the casino dice: the one and the four red
            float r = v == 1 ? 17f : 11f;
            for (int p = 0; p < oczka[v - 1].Length; p += 2)
            {
                float ox = cx + oczka[v - 1][p] * 30f, oy = cy + oczka[v - 1][p + 1] * 30f;
                for (int y = (int)(oy - r - 1); y <= (int)(oy + r + 1); y++) for (int x = (int)(ox - r - 1); x <= (int)(ox + r + 1); x++)
                {
                    float d = Mathf.Sqrt((x + .5f - ox) * (x + .5f - ox) + (y + .5f - oy) * (y + .5f - oy)) - r; if (d > .5f) continue;
                    float a = Mathf.Clamp01(.5f - d); var o = px[y * w + x];
                    px[y * w + x] = new Color32((byte)Mathf.Lerp(o.r, kol.r, a), (byte)Mathf.Lerp(o.g, kol.g, a), (byte)Mathf.Lerp(o.b, kol.b, a), 255);
                }
            }
            // the rounded edge's shade round each face
            for (int y = row * F; y < (row + 1) * F; y++) for (int x = col * F; x < (col + 1) * F; x++)
            {
                int ex = Mathf.Min(x - col * F, (col + 1) * F - 1 - x), ey = Mathf.Min(y - row * F, (row + 1) * F - 1 - y), e = Mathf.Min(ex, ey);
                if (e < 6) { int yy = h - 1 - y; var o = px[yy * w + x]; float s = .78f + .22f * e / 6f; px[yy * w + x] = new Color32((byte)(o.r * s), (byte)(o.g * s), (byte)(o.b * s), 255); }
            }
        }
        return px;
    }
    // a die's face from the dice atlas, upright in the player's view, s px square centred at (cx, cy)
    static Color32[] kostkiPx;
    static void KostkaNaFilcu(Color32[] px, int w, int h, float cx, float cy, int s, int v)
    {
        if (kostkiPx == null) kostkiPx = KostkiAtlas();
        int col = (v - 1) % 3, row = (v - 1) / 3, fx0 = col * 128, fy0 = 256 - (row + 1) * 128;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            int X = (int)(cx - s * .5f) + x, Y = (int)(cy - s * .5f) + y; if (X < 0 || Y < 0 || X >= w || Y >= h) continue;
            int r = 0, g = 0, b = 0, n = 0, k = 128 / s;
            for (int j = 0; j < k; j++) for (int i = 0; i < k; i++) { var c = kostkiPx[(fy0 + y * 128 / s + j) * 384 + fx0 + x * 128 / s + i]; r += c.r; g += c.g; b += c.b; n++; }
            px[Y * w + X] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
        }
    }
    // the felt as the player sees it (the dealer up), turned over to the quad's uv (u along +x, v toward the players)
    static Color32[] FilcSicBo()
    {
        const int w = 2048, h = 1024; var px = new Color32[w * h]; var los = new System.Random(911);
        for (int i = 0; i < px.Length; i++) { int n = los.Next(-7, 8); px[i] = new Color32((byte)Mathf.Clamp(12 + n, 0, 255), (byte)Mathf.Clamp(46 + n, 0, 255), (byte)Mathf.Clamp(92 + n, 0, 255), 255); }
        const float A = SbA; float X(float x) => (A - x) / (2f * A) * w; float Y(float z) => (A - .1f - z) / A * h;
        string[] gora = { "MAŁE", "DOWOLNA TRÓJKA", "DUŻE" }, srodek = { "4 - 10", "", "11 - 17" }, kurs = { "1 : 1", "30 : 1", "1 : 1" };
        for (int i = 0; i < 3; i++)
        {
            float x = SbPoleX(i); int x0 = (int)X(x + .34f), x1 = (int)X(x - .34f), y0 = (int)Y(.94f), y1 = (int)Y(.49f);
            Ramka(px, w, h, x0, y0, x1, y1, 7, Zloto);
            Tekst(px, w, h, gora[i], (x0 + x1) * .5f, Y(.575f), i == 1 ? 46f : 70f, x1 - x0 - 30, 0f, Zloto);
            if (srodek[i].Length > 0) Tekst(px, w, h, srodek[i], (x0 + x1) * .5f, Y(.71f), 64f, x1 - x0 - 30, 0f, new Color32(236, 232, 214, 255));
            else for (int t = 1; t <= 6; t++) for (int d = 0; d < 3; d++)                   // the six triples, as the real layouts draw them
                KostkaNaFilcu(px, w, h, (x0 + x1) * .5f + (t - 3.5f) * 92f + (d - 1) * 27f, Y(.71f), 24, t);
            Tekst(px, w, h, kurs[i], (x0 + x1) * .5f, Y(.85f), 48f, x1 - x0 - 30, 0f, Zloto);
        }
        Tekst(px, w, h, "TRÓJKA ZABIERA MAŁE I DUŻE", X(0f), Y(.455f), 26f, 900f, 0f, new Color32(236, 232, 214, 255));
        Tekst(px, w, h, "SIC BO", X(-.72f), Y(.22f), 70f, 500f, 0f, Zloto);
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        if (src != null)
        {
            int lw = 210, lh = Mathf.RoundToInt(lw / ArkProporcja(ArkLogo));
            Wklej(px, w, h, src, sw, sh, Arkusz[ArkLogo, 0], sh - Arkusz[ArkLogo, 1] - Arkusz[ArkLogo, 3], Arkusz[ArkLogo, 2], Arkusz[ArkLogo, 3],
                (int)X(.72f) - lw / 2, (int)Y(.22f) - lh / 2, lw, lh);
        }
        System.Array.Reverse(px);
        return px;
    }
}
