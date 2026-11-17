using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - a playable CASINO HOLD'EM table (owner 16.11, the casinos' tables: "poker Casino Hold'em"; the game is
// BraxChampionsHoldem). The table as the casinos' Casino Hold'em layouts: a half-oval 2.5 x 1.05 m on the card table the
// card games share (StolKarciany: the leather armrest, the walnut dealer's edge with his chips in the steel tray, the deck's
// box, the discards), five spots - each an ANTE circle and a CALL box toward the dealer - the dealer's two cards and the five
// of the board before him. The print is IN the deep teal felt: the spots, the ante's pay table ("WYPŁATY ANTE"), "KRUPIER
// GRA OD PARY CZWÓREK", "CALL: PODWÓJNE ANTE" and the owner's BRAXHORN CASINO logo (his art 1:1). His cards, his chips.
public static partial class BraxChampionsKasynoSprzet
{
    public const float HpA = 1.25f, HpB = 1.05f, HpAnte = .68f, HpCall = .50f;
    static Material filcHp;
    static float HpKat(int m) => 28f + m * 31f;                                         // as the blackjack's: clear of the deck and the discards
    public static Vector3 HpMiejsce(int m, float f)
    {
        float t = HpKat(m) * Mathf.Deg2Rad; return new Vector3(HpA * f * Mathf.Cos(t), BjTop, HpB * f * Mathf.Sin(t));
    }

    /// <summary>A Casino Hold'em table at `at` (the floor), turned by yaw (its players' side to local +z).</summary>
    public static void Holdem(Kasyno k, Vector3 at, float yaw)
    {
        BlackjackMaterialy();
        if (!filcHp) { filcHp = BraxChampionsHarbour.Surface("kasyno_holdem_sukno", Color.white, .92f, 0f); filcHp.SetTexture("_MainTex", Png("kasyno_holdem_sukno", 4096, 2048, FilcHoldem(), true)); Wnetrze(filcHp); }
        var q = Quaternion.Euler(0f, yaw, 0f); int idx = k.holdem.Count;
        var ante = new Vector3[5]; var call = new Vector3[5];
        for (int m = 0; m < 5; m++) { ante[m] = HpMiejsce(m, HpAnte); call[m] = HpMiejsce(m, HpCall); }
        StolKarciany(k, at, yaw, HpA, HpB, filcHp, ante);
        var stol = new GameObject("casino hold'em " + (idx + 1) + " | " + k.nazwa).transform;
        stol.SetParent(k.root, false); stol.localPosition = at; stol.localRotation = q;
        var za = new Transform[5]; var zc = new Transform[5];
        for (int m = 0; m < 5; m++) { za[m] = HpStos(stol, "żeton ANTE " + (m + 1), ante[m]); zc[m] = HpStos(stol, "żetony CALL " + (m + 1), call[m]); }
        k.holdem.Add(new BraxChampionsHoldem.Stol { stol = stol, but = new Vector3(-(HpA - .32f), BjTop + .02f, .24f), miejsca = ante, call = call,
            krupier = new Vector3(-.035f, BjTop, .14f), plansza = new Vector3(0f, BjTop, .31f), ante = za, calle = zc });
        W.Obsada("krupier casino hold'em", at + q * new Vector3(0f, 0f, -.62f), yaw);
    }
    static Transform HpStos(Transform stol, string n, Vector3 p)
    {
        var z = new GameObject(n).transform; z.SetParent(stol, false); z.localPosition = p + new Vector3(0f, .0008f, 0f);
        Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); return z;
    }

    // the felt as the player sees it (the dealer at the top), turned over to the table's uv (StolKarciany: x -A..A, z -.1..A-.1)
    static Color32[] FilcHoldem()
    {
        const int w = 4096, h = 2048; var px = new Color32[w * h]; var los = new System.Random(6151);
        for (int i = 0; i < px.Length; i++)                                                 // the deep teal cloth, its fibres
        {
            int n = los.Next(-7, 8), y = i / w;
            px[i] = new Color32((byte)Mathf.Clamp(8 + n / 2, 0, 255), (byte)Mathf.Clamp(68 + n + (y % 3 == 0 ? 2 : 0), 0, 255), (byte)Mathf.Clamp(76 + n, 0, 255), 255);
        }
        const float A = HpA; float X(float x) => (A - x) / (2f * A) * w; float Y(float z) => (A - .1f - z) / A * h;
        const float mm = w / (2000f * A); var krem = new Color32(236, 232, 214, 255);
        for (int m = 0; m < 5; m++)
        {
            // each spot: the ANTE circle, the CALL box nearer the dealer; their words upright for the player at the spot
            float deg = HpKat(m) - 90f; var a = HpMiejsce(m, HpAnte); var c = HpMiejsce(m, HpCall);
            Pierscien(px, w, h, X(a.x), Y(a.z), 50f * mm, 4f * mm, Zloto);
            Tekst(px, w, h, "ANTE", X(a.x), Y(a.z), 15f * mm, 80f * mm, deg, Zloto);
            RamkaObrot(px, w, h, X(c.x), Y(c.z), 112f * mm, 70f * mm, deg, 4f * mm, Zloto);
            Tekst(px, w, h, "CALL", X(c.x), Y(c.z), 17f * mm, 90f * mm, deg, krem);
        }
        // the dealer's two and the board's five: thin gold outlines where they lie
        for (int n = 0; n < 2; n++) RamkaObrot(px, w, h, X(-.035f + n * .07f), Y(.14f), 68f * mm, 93f * mm, 0f, 1.6f * mm, Zloto);
        for (int n = 0; n < 5; n++) RamkaObrot(px, w, h, X((n - 2) * .07f), Y(.31f), 68f * mm, 93f * mm, 0f, 1.6f * mm, Zloto);
        // the ante's pay table on the player's right (the table's -x), between the dealer's cards and the deck
        string[] uklad = { "POKER KRÓLEWSKI", "POKER", "KARETA", "FULL", "KOLOR", "STRIT I NIŻEJ" }, kurs = { "100 : 1", "20 : 1", "10 : 1", "3 : 1", "2 : 1", "1 : 1" };
        Tekst(px, w, h, "WYPŁATY ANTE", X(-.50f), Y(.035f), 14f * mm, 400f * mm, 0f, Zloto);
        for (int i = 0; i < 6; i++)
        {
            float z = .060f + i * .0205f;
            Tekst(px, w, h, uklad[i], X(-.43f), Y(z), 12f * mm, 300f * mm, 0f, krem);
            Tekst(px, w, h, kurs[i], X(-.68f), Y(z), 12f * mm, 140f * mm, 0f, Zloto);
        }
        // on his left the game's name, the dealer's rule and the call; his BRAXHORN CASINO logo
        Tekst(px, w, h, "CASINO HOLD'EM", X(.38f), Y(.06f), 22f * mm, 340f * mm, 0f, Zloto);
        Tekst(px, w, h, "KRUPIER GRA OD PARY CZWÓREK", X(.38f), Y(.105f), 11f * mm, 340f * mm, 0f, krem);
        Tekst(px, w, h, "CALL: PODWÓJNE ANTE", X(.38f), Y(.14f), 11f * mm, 340f * mm, 0f, krem);
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        if (src != null)
        {
            int lw = Mathf.RoundToInt(110f * mm), lh = Mathf.RoundToInt(lw / ArkProporcja(ArkLogo));
            Wklej(px, w, h, src, sw, sh, Arkusz[ArkLogo, 0], sh - Arkusz[ArkLogo, 1] - Arkusz[ArkLogo, 3], Arkusz[ArkLogo, 2], Arkusz[ArkLogo, 3],
                Mathf.RoundToInt(X(.68f) - lw * .5f), Mathf.RoundToInt(Y(.095f) - lh * .5f), lw, lh);
        }
        System.Array.Reverse(px);
        return px;
    }
    // a rectangle's outline turned deg anticlockwise about its centre (cx, cy), ww x hh px, t px thick
    static void RamkaObrot(Color32[] px, int w, int h, float cx, float cy, float ww, float hh, float deg, float t, Color32 col)
    {
        float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a), rad = Mathf.Sqrt(ww * ww + hh * hh) * .5f + 2f;
        for (int y = Mathf.Max(0, (int)(cy - rad)); y <= Mathf.Min(h - 1, (int)(cy + rad)); y++)
            for (int x = Mathf.Max(0, (int)(cx - rad)); x <= Mathf.Min(w - 1, (int)(cx + rad)); x++)
            {
                float dx = x + .5f - cx, dy = y + .5f - cy, u = Mathf.Abs(dx * cs + dy * sn), v = Mathf.Abs(-dx * sn + dy * cs);
                float d = Mathf.Max(u - ww * .5f, v - hh * .5f);                            // < 0 inside the rectangle
                float e = Mathf.Abs(d + t * .5f) - t * .5f; if (e > .5f) continue;
                float al = Mathf.Clamp01(.5f - e); var o = px[y * w + x];
                px[y * w + x] = new Color32((byte)Mathf.Lerp(o.r, col.r, al), (byte)Mathf.Lerp(o.g, col.g, al), (byte)Mathf.Lerp(o.b, col.b, al), 255);
            }
    }
}
