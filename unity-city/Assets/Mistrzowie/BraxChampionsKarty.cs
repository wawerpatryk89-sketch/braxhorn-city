using UnityEngine;

// MIASTO MISTRZÓW - the casino's playing cards on the tables (blackjack, bakarat): a casino card 63 x 88 mm, two-sided, cut
// from the owner's card atlas (BraxChampionsAutomatyGry.PokerGra: his jack, queen, king and his back) - face up his face,
// face down his back. Built once and shared.
public static class BraxChampionsKarty
{
    public const float Szer = .063f, Dl = .088f;
    static Mesh[] siatki;
    public static Mesh[] Siatki()
    {
        if (siatki != null && siatki[0]) return siatki;
        const int AW = BraxChampionsAutomatyGry.PokerGra.AtlasW, AH = BraxChampionsAutomatyGry.PokerGra.AtlasH, KW = BraxChampionsAutomatyGry.PokerGra.KartaW, KH = BraxChampionsAutomatyGry.PokerGra.KartaH;
        BraxChampionsAutomatyGry.PokerGra.RewersPx(out int rx, out int ry);
        siatki = new Mesh[52];
        for (int c = 0; c < 52; c++)
        {
            BraxChampionsAutomatyGry.PokerGra.KartaPx(c, out int px, out int py);
            float w = Szer * .5f, h = Dl * .5f, d = .0003f;
            Vector2 U(int x, int y) => new Vector2((float)x / AW, (float)y / AH);
            var m = new Mesh { name = "karta " + c };
            // the face up (its top, the atlas' up, along local +z), the back down; clockwise seen from each side
            m.vertices = new[] { new Vector3(-w, d, -h), new Vector3(-w, d, h), new Vector3(w, d, h), new Vector3(w, d, -h),
                                 new Vector3(w, -d, -h), new Vector3(w, -d, h), new Vector3(-w, -d, h), new Vector3(-w, -d, -h) };
            m.uv = new[] { U(px, py), U(px, py + KH), U(px + KW, py + KH), U(px + KW, py),
                           U(rx, ry), U(rx, ry + KH), U(rx + KW, ry + KH), U(rx + KW, ry) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7 };
            m.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.down, Vector3.down, Vector3.down, Vector3.down };
            m.RecalculateBounds(); siatki[c] = m;
        }
        return siatki;
    }
}
