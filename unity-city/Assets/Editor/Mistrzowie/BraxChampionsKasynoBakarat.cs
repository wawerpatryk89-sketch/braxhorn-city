using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - a playable baccarat (punto banco) table (owner 16.11, the casinos' tables: "bakarat"; the game is
// BraxChampionsBakarat). The midi-baccarat table of the casinos: a half-oval 2.6 x 1.1 m on the card table both games share
// (StolKarciany: the leather armrest, the walnut dealer's edge with his chips in the steel tray, the shoe, the discards), its
// felt burgundy with the print IN it: three gold rings - BANKIER 0,95:1, REMIS 8:1, GRACZ 1:1 - along the rail, the GRACZ and
// BANKIER card boxes before the dealer, the owner's BRAXHORN CASINO logo (his art 1:1).
public static partial class BraxChampionsKasynoSprzet
{
    public const float BkA = 1.30f, BkB = 1.10f, BkKolo = .62f;
    static Material filcBk;
    static readonly float[] BkKaty = { 40f, 90f, 140f };                 // BANKIER (the players' left), REMIS, GRACZ (their right)

    public static Vector3 BkPole(int i)
    {
        float t = BkKaty[i] * Mathf.Deg2Rad;
        return new Vector3(BkA * BkKolo * Mathf.Cos(t), BjTop, BkB * BkKolo * Mathf.Sin(t));
    }

    /// <summary>A baccarat table at `at` (the floor), turned by yaw (its players' side to local +z).</summary>
    public static void Bakarat(Kasyno k, Vector3 at, float yaw)
    {
        BlackjackMaterialy();
        if (!filcBk) { filcBk = BraxChampionsHarbour.Surface("kasyno_bakarat_sukno", Color.white, .92f, 0f); filcBk.SetTexture("_MainTex", Png("kasyno_bakarat_sukno", 2048, 1024, FilcBakarat(), true)); Wnetrze(filcBk); }
        var q = Quaternion.Euler(0f, yaw, 0f); int idx = k.bakarat.Count;
        var pola = new Vector3[3]; for (int i = 0; i < 3; i++) pola[i] = BkPole(i);
        StolKarciany(k, at, yaw, BkA, BkB, filcBk, pola);
        var stol = new GameObject("bakarat " + (idx + 1) + " | " + k.nazwa).transform;
        stol.SetParent(k.root, false); stol.localPosition = at; stol.localRotation = q;
        var zet = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var z = new GameObject("żeton " + BraxChampionsBakarat.Pola[i]).transform; z.SetParent(stol, false);
            z.localPosition = pola[i] + new Vector3(0f, .0008f, -.07f); Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[i] = z;
        }
        k.bakarat.Add(new BraxChampionsBakarat.Stol { stol = stol, but = new Vector3(-(BkA - .32f), BjTop + .02f, .24f), pola = pola,
            gracz = new Vector3(-.16f, BjTop, .30f), bankier = new Vector3(.16f, BjTop, .30f), zetony = zet });
        W.Obsada("krupier bakarata", at + q * new Vector3(0f, 0f, -.62f), yaw);
    }

    // the felt as the player sees it (the dealer at the top), turned over to the table's uv (StolKarciany: x -A..A, z -.1..A-.1)
    static Color32[] FilcBakarat()
    {
        const int w = 2048, h = 1024; var px = new Color32[w * h]; var los = new System.Random(4099);
        for (int i = 0; i < px.Length; i++)                                                 // the burgundy cloth, its fibres
        {
            int n = los.Next(-7, 8), y = i / w;
            px[i] = new Color32((byte)Mathf.Clamp(96 + n, 0, 255), (byte)Mathf.Clamp(16 + n / 2 + (y % 3 == 0 ? 1 : 0), 0, 255), (byte)Mathf.Clamp(26 + n / 2, 0, 255), 255);
        }
        const float A = BkA; float X(float x) => (A - x) / (2f * A) * w; float Y(float z) => (A - .1f - z) / A * h;
        const float mm = w / (2000f * A);
        string[] napis = { "BANKIER", "REMIS", "GRACZ" }, kurs = { "0,95 : 1", "8 : 1", "1 : 1" };
        for (int i = 0; i < 3; i++)
        {
            var c = BkPole(i); float deg = BkKaty[i] - 90f;                                // the text upright for the player at the box
            Pierscien(px, w, h, X(c.x), Y(c.z), 150f * mm, 6f * mm, Zloto);
            var r = new Vector2(Mathf.Cos(BkKaty[i] * Mathf.Deg2Rad), Mathf.Sin(BkKaty[i] * Mathf.Deg2Rad));
            // in the picture the dealer is up: the box's "up" is toward the table's centre, i.e. (x, z) of the box
            Vector2 up = new Vector2(r.x, r.y);
            Tekst(px, w, h, napis[i], X(c.x) + up.x * 40f * mm, Y(c.z) + up.y * 40f * mm, 36f * mm, 250f * mm, deg, Zloto);
            Tekst(px, w, h, kurs[i], X(c.x) - up.x * 30f * mm, Y(c.z) - up.y * 30f * mm, 26f * mm, 200f * mm, deg, new Color32(236, 232, 214, 255));
        }
        // the dealer's card boxes: GRACZ (x -), BANKIER (x +), gold frames with their names
        foreach (var (x, t) in new[] { (-.30f, "GRACZ"), (.30f, "BANKIER") })
        {
            Ramka(px, w, h, (int)X(x + .17f), (int)Y(.40f), (int)X(x - .17f), (int)Y(.20f), (int)(3f * mm), Zloto);
            Tekst(px, w, h, t, X(x), Y(.43f), 24f * mm, 300f * mm, 0f, Zloto);
        }
        var src = Wczytaj(Wlasciciel + "/szyldy/arkusz_szyldy_monety_logo.png", out int sw, out int sh);
        if (src != null)
        {
            int lw = Mathf.RoundToInt(120f * mm), lh = Mathf.RoundToInt(lw / ArkProporcja(ArkLogo));
            Wklej(px, w, h, src, sw, sh, Arkusz[ArkLogo, 0], sh - Arkusz[ArkLogo, 1] - Arkusz[ArkLogo, 3], Arkusz[ArkLogo, 2], Arkusz[ArkLogo, 3],
                Mathf.RoundToInt(X(0f) - lw * .5f), Mathf.RoundToInt(Y(.33f) - lh * .5f), lw, lh);
        }
        System.Array.Reverse(px);
        return px;
    }
}
