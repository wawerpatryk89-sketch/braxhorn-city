using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - the BIG SIX "KOŁO FORTUNY" (owner 16.11, the casinos' tables; the game BraxChampionsKoloFortuny). The
// money wheel as the casinos have it: 2 m across on a walnut stand with gold trim, 54 segments in the classic colours
// (1 yellow, 2 blue, 5 violet, 10 green, 20 orange) and his two specials - his BRAXHORN logo and his coin, his art 1:1,
// turned into their segments - chrome pins between the segments, a leather clapper at the top; before it the betting table
// with seven boxes (the numbers, his logo, his coin) and their odds printed in its felt. Frame: origin on the floor, +z the
// players; the wheel's face to +z.
public static partial class BraxChampionsKasynoSprzet
{
    public const float BsR = 1.0f, BsY = 2.25f, BsZ = -.10f, BsStolZ = 2.0f, BsStolY = .90f;
    static Material koloTwarz, koloStolFilc;
    static readonly Color32[] KoloKolory = { new Color32(242, 202, 44, 255), new Color32(38, 92, 204, 255), new Color32(128, 48, 162, 255),
        new Color32(28, 138, 72, 255), new Color32(232, 112, 32, 255), new Color32(14, 12, 12, 255), new Color32(168, 18, 30, 255) };
    static int KoloIndeks(int v) => v == 1 ? 0 : v == 2 ? 1 : v == 5 ? 2 : v == 10 ? 3 : v == 20 ? 4 : v == BraxChampionsKoloFortuny.Logo ? 5 : 6;
    public static float KoloPoleX(int i) => 1.38f - i * .46f;                         // 1 on the players' left .. the coin on their right

    public static void KoloFortuny(Kasyno k, Vector3 at, float yaw)
    {
        if (!koloTwarz)
        {
            koloTwarz = BraxChampionsHarbour.Surface("kasyno_kolo_fortuny_tarcza", Color.white, .35f, 0f);
            koloTwarz.SetTexture("_MainTex", Png("kasyno_kolo_fortuny_tarcza", 2048, 2048, KoloTarcza(), true)); Wnetrze(koloTwarz);
            koloStolFilc = BraxChampionsHarbour.Surface("kasyno_kolo_fortuny_stol", Color.white, .9f, 0f);
            koloStolFilc.SetTexture("_MainTex", Png("kasyno_kolo_fortuny_stol", 2048, 512, KoloStol(), true)); Wnetrze(koloStolFilc);
        }
        var q = Quaternion.Euler(0f, yaw, 0f); var b = k.body;
        Vector3 L(float x, float y, float z) => at + q * new Vector3(x, y, z);
        // ---- the stand: a walnut back with gold edges, the pedestal, the warm light ring round the wheel
        b.RoundedBox(ciemnyOrzech, L(0f, 1.8f, -.42f), new Vector3(2.7f, 3.6f, .24f), .03f, q, 2);
        foreach (float sx in new[] { -1f, 1f }) b.Box(zlotoKola, L(sx * 1.37f, 1.8f, -.29f), new Vector3(.05f, 3.6f, .04f), q);
        b.Box(zlotoKola, L(0f, 3.62f, -.29f), new Vector3(2.79f, .05f, .04f), q);
        b.RoundedBox(czern, L(0f, .355f, -.25f), new Vector3(1.8f, .69f, .6f), .03f, q, 2);                         // 1 cm off the floor plane of the back
        b.Surface(kasetonLed, (u, v) => { float a = u * Mathf.PI * 2f, r = BsR + .08f + v * .03f; return L(Mathf.Cos(a) * r, BsY + Mathf.Sin(a) * r, -.29f); }, 72, 1, true);
        b.Cylinder(chrom, L(0f, BsY, -.30f), .09f, .20f, 24, q * Quaternion.Euler(90f, 0f, 0f), false);       // the axle
        W.Kolizja(L(0f, 1.8f, -.36f), new Vector3(2.7f, 3.6f, .36f), q);
        // ---- the wheel (it turns): face, walnut rim, back, the pins, the gold hub
        var stojak = new GameObject("KOŁO FORTUNY | " + k.nazwa).transform; stojak.SetParent(k.root, false); stojak.localPosition = at; stojak.localRotation = q;
        var kolo = new GameObject("koło (54 segmenty)").transform; kolo.SetParent(stojak, false);
        kolo.localPosition = new Vector3(0f, BsY, BsZ); kolo.localRotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.identity;
        var obr = new GameObject("obrót").transform; obr.SetParent(kolo, false);
        var kb = new BraxChampionsMeshBatch();
        // its face at local z = -.04 seen from local -z (the players): clockwise from there; uv: u right (+x), v up (+y)
        const int N = 96;
        for (int i = 0; i < N; i++)
        {
            float a0 = 2f * Mathf.PI * i / N, a1 = 2f * Mathf.PI * (i + 1) / N;
            Vector3 c = new Vector3(0f, 0f, -.04f), p0 = new Vector3(Mathf.Cos(a0) * BsR, Mathf.Sin(a0) * BsR, -.04f), p1 = new Vector3(Mathf.Cos(a1) * BsR, Mathf.Sin(a1) * BsR, -.04f);
            Vector2 U(Vector3 p) => new Vector2(.5f + p.x / (2f * BsR), .5f + p.y / (2f * BsR));
            if (Vector3.Dot(Vector3.Cross(p0 - c, p1 - c), Vector3.back) > 0f) kb.TriangleUV(koloTwarz, c, p0, p1, U(c), U(p0), U(p1)); else kb.TriangleUV(koloTwarz, c, p1, p0, U(c), U(p1), U(p0));
            Vector3 d = new Vector3(0f, 0f, .06f), e0 = new Vector3(p0.x, p0.y, .06f), e1 = new Vector3(p1.x, p1.y, .06f);
            if (Vector3.Dot(Vector3.Cross(e0 - d, e1 - d), Vector3.forward) > 0f) kb.Triangle(czern, d, e0, e1); else kb.Triangle(czern, d, e1, e0);
        }
        kb.Surface(ciemnyOrzech, (u, v) => { float a = u * Mathf.PI * 2f; return new Vector3(Mathf.Cos(a) * BsR, Mathf.Sin(a) * BsR, Mathf.Lerp(-.045f, .06f, v)); }, N, 1, true);
        kb.Surface(zlotoKola, (u, v) => { float a = u * Mathf.PI * 2f, r = BsR + .008f; return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Lerp(-.05f, -.03f, v)); }, N, 1, true);
        float al = 360f / BraxChampionsKoloFortuny.Segmenty;
        for (int i = 0; i < BraxChampionsKoloFortuny.Segmenty; i++)
        {
            // the pins on the segments' borders, standing out of the face (the clapper rides over them)
            float th = ((i + .5f) * al) * Mathf.Deg2Rad; var dir = new Vector3(-Mathf.Sin(th), Mathf.Cos(th), 0f);
            kb.Cylinder(chrom, dir * (BsR - .035f) + new Vector3(0f, 0f, -.04f), .007f, .055f, 8, Quaternion.Euler(-90f, 0f, 0f), true);
        }
        kb.Cylinder(zlotoKola, new Vector3(0f, 0f, -.045f), .16f, .05f, 32, Quaternion.Euler(-90f, 0f, 0f), true, false, .11f);
        kb.Build(obr, "koło fortuny | tarcza", Out + "/Meshes", false, 1f);
        // ---- the clapper at 12 o'clock: a leather flap on a brass pivot, its tip between the pins
        var klapka = new GameObject("klapka (skóra)").transform; klapka.SetParent(kolo, false); klapka.localPosition = new Vector3(0f, BsR + .07f, -.10f);
        var cb = new BraxChampionsMeshBatch();
        cb.Box(skora, new Vector3(0f, -.08f, 0f), new Vector3(.045f, .17f, .012f), Quaternion.identity);
        cb.Cylinder(mosiadz, new Vector3(0f, 0f, -.02f), .018f, .04f, 12, Quaternion.Euler(90f, 0f, 0f), true, true);
        cb.Build(klapka, "koło fortuny | klapka", Out + "/Meshes", false, 1f);
        b.Box(mosiadz, L(0f, BsY + BsR + .07f, -.15f), new Vector3(.06f, .06f, .30f), q);                     // the clapper's arm, from the stand to its pivot
        // ---- the betting table: walnut, a leather edge, its felt with the seven boxes; the chips shown while bet
        var st = new GameObject("KOŁO FORTUNY | stół zakładów").transform; st.SetParent(k.root, false);
        st.localPosition = L(0f, 0f, BsStolZ); st.localRotation = q;
        b.RoundedBox(ciemnyOrzech, L(0f, (BsStolY + .12f) * .5f - .03f, BsStolZ), new Vector3(3.3f, BsStolY - .18f, .95f), .03f, q, 2);
        b.RoundedBox(czern, L(0f, .06f, BsStolZ - .04f), new Vector3(3.1f, .12f, .80f), .01f, q, 1);                  // the black plinth, set back
        b.RoundedBox(skora, L(0f, BsStolY - .015f, BsStolZ + .47f), new Vector3(3.36f, .06f, .10f), .028f, q, 3);
        // the felt up: u along +x, v toward the players (+z), as the card tables' felts
        b.QuadUV(koloStolFilc, L(-1.62f, BsStolY + .001f, BsStolZ - .44f), L(-1.62f, BsStolY + .001f, BsStolZ + .42f), L(1.62f, BsStolY + .001f, BsStolZ + .42f), L(1.62f, BsStolY + .001f, BsStolZ - .44f),
            new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        W.Kolizja(L(0f, BsStolY * .5f, BsStolZ), new Vector3(3.36f, BsStolY, 1.0f), q);
        var pola = new Vector3[7]; var zet = new Transform[7];
        for (int i = 0; i < 7; i++)
        {
            pola[i] = new Vector3(KoloPoleX(i), BsStolY, .12f);
            var z = new GameObject("żeton " + BraxChampionsKoloFortuny.Nazwy[i]).transform; z.SetParent(st, false);
            z.localPosition = pola[i] + new Vector3(0f, .002f, -.1f); Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[i] = z;
        }
        var gra = k.root.gameObject.AddComponent<BraxChampionsKoloFortuny>();
        gra.stol = st; gra.pola = pola; gra.zetony = zet; gra.kolo = obr; gra.klapka = klapka; gra.stawka = Stawka;
        W.Obsada("krupier koła fortuny", L(1.5f, 0f, .7f), yaw + 200f);
        Debug.Log("KASYNO_KOLO_FORTUNY " + k.nazwa + " segments=54 (1x24 2x15 5x7 10x4 20x2 logo+coin owner) pays=1..20:1 specials=40:1 game=BraxChampionsKoloFortuny");
    }

    // the face (2048 x 2048, seen from the front, v up): segment i centred i x 6.67 degrees anticlockwise from the top
    static Color32[] KoloTarcza()
    {
        const int S = 2048; var px = new Color32[S * S]; float c0 = S * .5f, R = S * .5f - 2f;
        var uklad = BraxChampionsKoloFortuny.Uklad(); float al = 2f * Mathf.PI / BraxChampionsKoloFortuny.Segmenty;
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            float dx = x + .5f - c0, dy = y + .5f - c0, r = Mathf.Sqrt(dx * dx + dy * dy) / R;
            if (r > 1f) { px[y * S + x] = new Color32(20, 14, 10, 255); continue; }
            float th = Mathf.Atan2(-dx, dy); if (th < 0f) th += 2f * Mathf.PI;
            int seg = Mathf.RoundToInt(th / al) % BraxChampionsKoloFortuny.Segmenty;
            float doGranicy = Mathf.Abs(th / al - (Mathf.Floor(th / al - .5f) + .5f)) * al * r * R;   // px to the nearest border
            Color32 col;
            if (r < .18f) col = new Color32(22, 16, 12, 255);
            else if (r < .62f) col = (seg % 2 == 0) ? new Color32(30, 22, 16, 255) : new Color32(44, 30, 20, 255);       // the walnut star
            else if (r < .64f || r > .985f) col = Zloto;
            else col = KoloKolory[KoloIndeks(uklad[seg])];
            if (r >= .18f && r <= .985f && doGranicy < 2.2f) col = Zloto;                                                   // the gold lines
            px[y * S + x] = col;
        }
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        for (int i = 0; i < BraxChampionsKoloFortuny.Segmenty; i++)
        {
            float th = i * al, deg = th * Mathf.Rad2Deg; var d = new Vector2(-Mathf.Sin(th), Mathf.Cos(th));
            int v = uklad[i];
            if (v >= BraxChampionsKoloFortuny.Logo && src != null)
            {
                int ark = v == BraxChampionsKoloFortuny.Logo ? ArkLogo : ArkMonetaA;
                float rr = .81f * R; WklejObrot(px, S, S, src, sw, sh, Arkusz[ark, 0], sh - Arkusz[ark, 1] - Arkusz[ark, 3], Arkusz[ark, 2], Arkusz[ark, 3],
                    c0 + d.x * rr, c0 + d.y * rr, 118f, 118f * Arkusz[ark, 3] / Arkusz[ark, 2], deg);
            }
            else
            {
                var col = v == 1 ? new Color32(30, 20, 10, 255) : new Color32(250, 246, 236, 255);
                Tekst(px, S, S, v.ToString(), c0 + d.x * .80f * R, c0 + d.y * .80f * R, v >= 10 ? 74f : 92f, 96f, deg, col, 3f, new Color32(20, 14, 10, 255));
            }
        }
        return px;
    }
    // the betting table's felt (2048 x 512 over 3.24 x .86 m, seen by the players: u along +x... their right is -x): the boxes
    static Color32[] KoloStol()
    {
        const int w = 2048, h = 512; var px = new Color32[w * h]; var los = new System.Random(77);
        for (int i = 0; i < px.Length; i++) { int n = los.Next(-6, 7); px[i] = new Color32((byte)(18 + n), (byte)(60 + n), (byte)(42 + n), 255); }
        float X(float x) => (1.62f - x) / 3.24f * w; float Y(float z) => (.42f - z) / .86f * h;     // player view: X right = -x, Y up = away
        string[] kurs = { "1 : 1", "2 : 1", "5 : 1", "10 : 1", "20 : 1", "40 : 1", "40 : 1" };
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        for (int i = 0; i < 7; i++)
        {
            float x = KoloPoleX(i); int x0 = (int)X(x + .2f), x1 = (int)X(x - .2f), y0 = (int)Y(.30f), y1 = (int)Y(-.36f);   // X, Y fall with x, z
            for (int yy = y0; yy < y1; yy++) for (int xx = x0; xx < x1; xx++)
            {
                var o = px[yy * w + xx]; var kk = KoloKolory[i]; px[yy * w + xx] = new Color32((byte)((o.r + kk.r * 2) / 3), (byte)((o.g + kk.g * 2) / 3), (byte)((o.b + kk.b * 2) / 3), 255);
            }
            Ramka(px, w, h, x0, y0, x1, y1, 5, Zloto);
            if (i >= 5 && src != null)
            {
                int ark = i == 5 ? ArkLogo : ArkMonetaA; int lw = (x1 - x0) - 60, lh = Mathf.RoundToInt(lw * (float)Arkusz[ark, 3] / Arkusz[ark, 2]);
                Wklej(px, w, h, src, sw, sh, Arkusz[ark, 0], sh - Arkusz[ark, 1] - Arkusz[ark, 3], Arkusz[ark, 2], Arkusz[ark, 3], (x0 + x1) / 2 - lw / 2, (int)Y(-.02f) - lh / 2, lw, lh);
            }
            else Tekst(px, w, h, BraxChampionsKoloFortuny.Symbole[i].ToString(), (x0 + x1) * .5f, Y(-.02f), 120f, x1 - x0 - 40, 0f, new Color32(250, 246, 236, 255), 4f, new Color32(20, 14, 10, 255));
            Tekst(px, w, h, kurs[i], (x0 + x1) * .5f, Y(-.27f), 40f, x1 - x0 - 30, 0f, Zloto);
        }
        Tekst(px, w, h, "KOŁO FORTUNY", w * .5f, Y(.37f), 44f, 900f, 0f, Zloto);
        // turned over to the quad's uv (u along +x, v toward the players)
        System.Array.Reverse(px);
        return px;
    }
    // his picture into a turned rectangle (the wheel's specials): centre (cx, cy), size (ww, hh) px, turned deg anticlockwise
    static void WklejObrot(Color32[] dst, int w, int h, Color32[] src, int sw, int sh, int sx0, int sy0, int sww, int shh, float cx, float cy, float ww, float hh, float deg)
    {
        float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a), rad = Mathf.Sqrt(ww * ww + hh * hh) * .5f + 2f;
        for (int y = (int)(cy - rad); y <= (int)(cy + rad); y++) for (int x = (int)(cx - rad); x <= (int)(cx + rad); x++)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) continue;
            float dx = x + .5f - cx, dy = y + .5f - cy, u = dx * cs + dy * sn, v = -dx * sn + dy * cs;     // into the picture's frame
            if (Mathf.Abs(u) > ww * .5f || Mathf.Abs(v) > hh * .5f) continue;
            int qx = sx0 + Mathf.Clamp((int)((u / ww + .5f) * sww), 0, sww - 1), qy = sy0 + Mathf.Clamp((int)((v / hh + .5f) * shh), 0, shh - 1);
            if (qx < 0 || qy < 0 || qx >= sw || qy >= sh) continue;
            var s = src[qy * sw + qx]; float al = s.a / 255f; var o = dst[y * w + x];
            dst[y * w + x] = new Color32((byte)Mathf.Lerp(o.r, s.r, al), (byte)Mathf.Lerp(o.g, s.g, al), (byte)Mathf.Lerp(o.b, s.b, al), 255);
        }
    }
}
