using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;
using Z = BraxChampionsAutomatyZasady;
using A = BraxChampionsAutomaty;
using G = BraxChampionsAutomatyGry;
using KG = BraxChampionsAutomatyGry.KoloGra;

// MIASTO MISTRZÓW - the casinos' slot machines, built as the real ones (owner 16.11 18:12-18:22, five reference photos:
// an IGT upright with a topper, a Promatic double tower, a Promatic Pro Selection, a Merkur Magie with its curved top, a
// classic one-armed bandit; "jak najbardziej realistycznie", "te maszyny mają funkcjonować"; their names and logos are
// not copied - BRAXHORN). Two cabinets, each built once per theme and placed in whole banks in both casinos (SKY KASYNO
// on the BRAXHORN TOWER, KASYNO WAWER T11: "dwie roboty w jednym"):
//  - MECHANICAL "BRAXHORN CLASSIC" (an upright stepper, 0.66 x 0.56 m, 1.88 m with the candle): a lacquered steel base
//    with a brushed kick plate, the cabinet with the back-lit pay glass, the chrome coin tray, the button deck (CASH OUT,
//    BET ONE, MAX BET, the coin slot, the red SPIN), the red LED meter, the reel window in a chrome bezel with the glass and
//    the pay line, THREE REAL DRUMS behind it (20 stops, the strips from the owner's symbols), the chrome lever with the red
//    ball on the right, the top box's printed glass lit from behind (BRAXHORN), the two-colour candle;
//  - SCREEN "BRAXHORN VIDEO" (after the Pro Selection / Merkur uprights, 0.70 x 0.64 m, 2.06 m): a brushed foot plate, a
//    tapered lacquered base with LED edges, speakers and a bill acceptor, the button deck with a chrome bumper (five lit
//    buttons, the big SPIN), the main 27" screen tilted back 15 degrees where the game runs (five reels on the screen, the
//    meter), the upper 27" screen with the pay table, the curved topper with the jackpot screen, LED edges along the cheeks.
// Every machine has its slot stool (chrome base and column, a footrest ring, black leather seat and back). Real sizes:
// uprights 1.8-2.1 m, 27-28" wide, seat 0.68 m, the aisle behind the stools >= 0.92 m (dimensions.com, Suzohapp).
// Materials: lacquered steel, chrome and brushed steel on the region's photo-scanned metal (Metal048A), glass, black
// leather (Poly Haven leather_white); only LEDs and screens are plain light.
public static partial class BraxChampionsKasynoSprzet
{
    public const string Out = "Assets/PlayableGenerated/Mistrzowie/Kasyno";
    // the owner's pack braxhorn-mistrzowie-kasyno-grafiki-20261116 (install_owner_media.py): paski/<strip>.png - his six reel
    // strips (16.11 19:13), bet/*.png - the BRAXHORN BET posters, screen frame and logo
    public const string Wlasciciel = "Assets/Source/Mistrzowie/KasynoWlasciciel";
    public const int Stawka = 10;
    // machine to machine in a bank: the screen cabinets .70 wide almost touch; the mechanical ones leave room for the lever
    public const float RozstawEkran = .76f, RozstawMech = .82f, RozstawMieszany = .80f, MechD = .56f, EkranD = .64f, Taboret = .80f;
    // six themes, one per reel strip of the owner: the cabinet's lacquer and its light in the strip's colours, his strip on
    // the drums and the screen reels (owner 16.11 19:13: "maszyny różnorodne, żeby nie były wszystkie takie same")
    public static readonly string[] Motywy = { "SZAFIR", "AMETYST", "SZMARAGD", "FIOLET", "RUBIN", "ZŁOTO" };
    public static readonly string[] Paski = { "pasek_niebieski", "pasek_fioletowy", "pasek_zielony", "pasek_fiolet_zloty", "pasek_czerwony_cytryna", "pasek_czerwony" };
    static readonly Color[] Lakiery = { new Color(.03f, .06f, .30f), new Color(.22f, .03f, .32f), new Color(.02f, .15f, .06f),
        new Color(.055f, .055f, .06f), new Color(.42f, .025f, .035f), new Color(.055f, .055f, .06f) };
    static readonly Color[] Swiatla = { new Color(.30f, .70f, 1.8f), new Color(1.4f, .25f, 1.7f), new Color(.35f, 1.5f, .30f),
        new Color(1.0f, .35f, 1.7f), new Color(1.7f, .18f, .14f), new Color(1.6f, 1.05f, .35f) };
    // his strips (724 x 2172): the symbols top to bottom as the game's (BraxChampionsAutomatyZasady: 0 SEVEN, 1 DIAMOND,
    // 2 STAR, 3 BELL, 4 WATERMELON, 5 GRAPES, 6 CHERRIES; -1 a symbol of his the game does not use: horseshoe, plum, lemon)
    // and their centres in his pixels from the top
    static readonly int[][] PasekSymbole = { new[]{6,0,2,5,1,3,4,-1}, new[]{0,2,1,6,5,3,4}, new[]{0,2,1,6,3,5,4,-1}, new[]{0,2,1,6,5,3,4,-1},
        new[]{0,2,1,6,3,5,-1,4}, new[]{0,2,1,6,3,5,4} };
    static readonly int[][] PasekY = { new[]{208,508,783,1025,1233,1483,1725,1967}, new[]{283,633,908,1167,1442,1692,1967},
        new[]{217,500,733,967,1208,1433,1667,1917}, new[]{250,550,783,1025,1242,1442,1658,1850}, new[]{200,508,742,975,1200,1425,1667,1908},
        new[]{233,608,892,1158,1408,1683,1958} };

    // ---- the drums (mechanical): radius, width, centres (+x = the player's LEFT: reel 1 at +x)
    public const float BebenR = .175f, BebenSzer = .11f, BebenY = 1.19f, BebenZ = .015f;
    public static readonly float[] BebenX = { .125f, 0f, -.125f };
    // ---- the main screen (video): its centre on the screen's face and its tilt; the reels' cell
    static readonly Vector3 EkranS0 = new Vector3(0f, 1.18f, .25f); const float EkranTilt = -15f, Komorka = .09f;
    public static readonly float[] KolumnaX = { .196f, .098f, 0f, -.098f, -.196f };
    const float KolumnyY = .020f, LicznikY = -.144f;
    // a picture / a screen stands this far in front of the face it is on (the region's z-fight check: >= 6 mm), with the
    // depth offset of its material on top
    const float Przed = .007f;

    static Material[] lakier, swiatlo, szyld;
    static Material[] pasekBeben, ekranBebny, tabelaMech, tabelaEkran;
    static Material chrom, stal, czern, plastik, glosnik, szklo, linia, tloEkran,
        licznikMech, licznikEkran, jackpotMat, tablicaMat, kula, pCzerwony, pZielony, pBialy, pZolty, skora, swiecaCzerwona, swiecaBiala;
    static BraxChampionsMeshBatch[,] szablon;
    static Mesh[] bebenSiatki; static Mesh quad; static Mesh[,] kolumnaStart;
    static List<(Mesh, Material)> czRamie, czKula, czSpin, czSpinEkran, czRotor, czKulka, czZeton;
    static BraxChampionsMeshBatch szablonRuletki;
    static Material orzech, sukno, uklad, pierscien, kulkaMat, zloto, wysTlo, wysAtlas;
    static Color32[][] atlasPx;
    public static bool SymboleWlasciciela; public static int Brakujace;

    // ================================================================ one casino
    public sealed class Kasyno
    {
        public Transform root; public BraxChampionsMeshBatch body; public string nazwa;
        public readonly List<A.Maszyna> maszyny = new List<A.Maszyna>();
        public readonly List<Renderer> jackpoty = new List<Renderer>(), tablice = new List<Renderer>();
        public readonly List<BraxChampionsRuletka.Stol> ruletki = new List<BraxChampionsRuletka.Stol>();
        public int mech, ekran, banki, karuzele;
        public readonly int[] gry = new int[G.Ile];
    }
    /// <summary>Starts a casino's equipment under root (nominal metres, root at the origin while building); the cabinets
    /// go into body (the caller builds it), the moving parts and the displays under root.</summary>
    public static Kasyno Zacznij(Transform root, BraxChampionsMeshBatch body, string nazwa)
    {
        Przygotuj();
        return new Kasyno { root = root, body = body, nazwa = nazwa };
    }
    /// <summary>The casino's game: one component on root holding every machine and display.</summary>
    public static A Zakoncz(Kasyno k)
    {
        var c = k.root.gameObject.AddComponent<A>();
        c.nazwa = k.nazwa; c.stawka = Stawka; c.maszyny = k.maszyny.ToArray(); c.jackpoty = k.jackpoty.ToArray(); c.tablice = k.tablice.ToArray();
        if (k.ruletki.Count > 0)
        {
            var r = k.root.gameObject.AddComponent<BraxChampionsRuletka>(); r.nazwa = k.nazwa + " · RULETKA"; r.stawka = Stawka; r.stoly = k.ruletki.ToArray();
            r.torR = TorR; r.torY = TorY; r.kieszenR = KieszenR; r.kieszenY = KieszenY; r.kulkaR = KulkaR;
            Debug.Log("KASYNO_RULETKI " + k.nazwa + " tables=" + k.ruletki.Count + " wheel=European(37) bets=" + string.Join("/", BraxChampionsRuletka.Zasady.Pola) + " game=BraxChampionsRuletka");
        }
        var gry = new System.Text.StringBuilder(); for (int g = 0; g < G.Ile; g++) gry.Append((g > 0 ? "/" : "") + G.Nazwy[g].Replace(' ', '_') + "=" + k.gry[g]);
        Debug.Log("KASYNO_AUTOMATY " + k.nazwa + " machines=" + k.maszyny.Count + " mechanical=" + k.mech + " screen=" + k.ekran + " banks=" + k.banki +
            " carousels=" + k.karuzele + " games=" + gry + " toppers=" + (TopperyWlasciciela ? "owner" : "drawn") + " cards=" + (KartyWlasciciela ? "owner" : "drawn") +
            " stake=" + Stawka + " themes=" + Motywy.Length + " strips=" + (SymboleWlasciciela ? "owner" : "placeholder(missing " + Brakujace + ")") + " game=BraxChampionsAutomaty");
        return c;
    }

    /// <summary>A bank: n machines a row, two rows back to back along the bank's x (turned by yaw), their fronts to +z and
    /// -z of the bank; the linked jackpot board hangs over the spine on two cables up to hangTo. Owner 16.11 19:13 ("co druga,
    /// muszą być różnego typu"): every other machine is the other type and every machine another theme than its neighbours;
    /// start shifts the pattern from bank to bank.</summary>
    public static void Bank(Kasyno k, Vector3 c, float yaw, int n, int start, float hangTo) => Bank(k, c, yaw, n, start, hangTo, null);
    /// <summary>A bank playing the games in turn (owner 16.11 20:28 "różne gry w automatach"): gry is the cycle along a row
    /// (neighbours differ), the row behind runs it shifted by two; null = the classic and the video in turn.</summary>
    public static void Bank(Kasyno k, Vector3 c, float yaw, int n, int start, float hangTo, int[] gry)
    {
        var q = Quaternion.Euler(0f, yaw, 0f);
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < n; i++)
            {
                int gra = gry == null ? ((i + row + start) % 2 == 1 ? G.Video : G.Mech) : gry[(i + 2 * row + start) % gry.Length];
                int motyw = (start + i + 3 * row) % Motywy.Length;
                float D = G.Ekranowa(gra) ? EkranD : MechD, x = (i - (n - 1) * .5f) * RozstawMieszany, z = (D * .5f + .01f) * (row == 0 ? 1f : -1f);
                Automat(k, c + q * new Vector3(x, 0f, z), yaw + (row == 0 ? 0f : 180f), gra, motyw);
            }
        Tablica(k, c + new Vector3(0f, 2.95f, 0f), yaw, hangTo);
        k.banki++;
    }

    /// <summary>One machine with its stool at at (floor), turned by yaw (its front to the local +z).</summary>
    public static A.Maszyna Automat(Kasyno k, Vector3 at, float yaw, bool ekran, int motyw) => Automat(k, at, yaw, ekran ? G.Video : G.Mech, motyw);
    /// <summary>One machine of a game (BraxChampionsAutomatyGry) with its stool at at (floor), turned by yaw (its front to the
    /// local +z).</summary>
    public static A.Maszyna Automat(Kasyno k, Vector3 at, float yaw, int gra, int motyw)
    {
        motyw = Mathf.Clamp(motyw, 0, Motywy.Length - 1); gra = Mathf.Clamp(gra, 0, G.Ile - 1); bool ekran = G.Ekranowa(gra);
        var q = Quaternion.Euler(0f, yaw, 0f); int idx = k.maszyny.Count;
        k.body.Append(szablon[gra, motyw], at, q);
        var kor = new GameObject((ekran ? "automat ekranowy " : "automat mechaniczny ") + (idx + 1) + " | " + Motywy[motyw] + " | " + G.Nazwy[gra]).transform;
        kor.SetParent(k.root, false); kor.localPosition = at; kor.localRotation = q;
        var m = new A.Maszyna { korpus = kor, ekran = ekran, gra = gra, motyw = motyw, komorka = Komorka };
        k.gry[gra]++;
        if (gra == G.Poker || gra == G.Keno) { AutomatGry(k, m, at, q, idx); k.maszyny.Add(m); return m; }
        if (!ekran)
        {
            m.bebny = new Transform[3];
            for (int r = 0; r < 3; r++)
            {
                var t = new GameObject("bęben " + (r + 1)).transform; t.SetParent(kor, false);
                t.localPosition = new Vector3(BebenX[r], BebenY, BebenZ);
                t.localRotation = Quaternion.AngleAxis(((idx * 7 + r * 5) % Z.Stopy) * 18f, Vector3.right);
                Wstaw(new List<(Mesh, Material)> { (bebenSiatki[gra == G.Kolo && r == 2 ? 3 : r], pasekBeben[motyw]) }, t, "pasek");
                m.bebny[r] = t;
            }
            var lev = new GameObject("dźwignia").transform; lev.SetParent(kor, false); lev.localPosition = new Vector3(-.364f, 1.10f, .05f);
            Wstaw(czRamie, lev, "ramię"); Wstaw(czKula, lev, "kula"); m.dzwignia = lev;
            m.przycisk = Spin(kor, DeckMech, new Vector3(-.20f, 0f, .01f), czSpin);
            m.licznik = Ekranik(kor, "licznik", licznikMech, new Vector3(0f, 1.035f, .249f), .32f, .04f, Quaternion.identity);
            k.mech++;
            if (gra == G.Kolo) KoloNaAutomacie(m, kor, idx);
            float hk = gra == G.Kolo ? KoloC.y + KoloObudowaR + .04f : 1.88f;
            W.Kolizja(at + q * new Vector3(0f, hk * .5f, .05f), new Vector3(.68f, hk, .66f), q);
        }
        else
        {
            var qs = Quaternion.Euler(EkranTilt, 0f, 0f);
            var scr = new GameObject("ekran gry").transform; scr.SetParent(kor, false); scr.localPosition = EkranS0; scr.localRotation = qs;
            m.kolumny = new MeshFilter[5];
            for (int r = 0; r < 5; r++)
            {
                var o = new GameObject("bęben na ekranie " + (r + 1)); o.transform.SetParent(scr, false);
                o.transform.localPosition = new Vector3(KolumnaX[r], KolumnyY, .010f);
                var mf = o.AddComponent<MeshFilter>(); mf.sharedMesh = kolumnaStart[idx % 4, r];
                var mr = o.AddComponent<MeshRenderer>(); mr.sharedMaterial = ekranBebny[motyw]; Cienie(mr);
                m.kolumny[r] = mf;
            }
            m.licznik = Ekranik(scr, "licznik", licznikEkran, new Vector3(0f, LicznikY, Przed), .32f, .04f, Quaternion.identity);
            m.przycisk = Spin(kor, DeckEkran, new Vector3(-.22f, 0f, .02f), czSpinEkran);
            k.jackpoty.Add(Ekranik(kor, "jackpot", jackpotMat, new Vector3(0f, 1.89f, .20f + Przed), .48f, .16f, Quaternion.identity));
            k.ekran++;
            W.Kolizja(at + q * new Vector3(0f, 1.03f, .10f), new Vector3(.72f, 2.06f, .84f), q);
        }
        // the stool: the player stands behind it (BraxChampionsAutomaty: 1.0 m in front of the cabinet)
        W.Kolizja(at + q * new Vector3(0f, .37f, Taboret), new Vector3(.40f, .74f, .40f), q);
        k.maszyny.Add(m);
        return m;
    }

    // the jackpot board over a bank: a black frame with an LED face to each row, on two steel cables
    static void Tablica(Kasyno k, Vector3 c, float yaw, float hangTo)
    {
        var q = Quaternion.Euler(0f, yaw, 0f);
        k.body.RoundedBox(czern, c, new Vector3(2.56f, .72f, .16f), .02f, q, 1);
        k.tablice.Add(Ekranik(k.root, "tablica jackpot +z", tablicaMat, c + q * new Vector3(0f, 0f, .08f + Przed), 2.40f, .60f, q));
        k.tablice.Add(Ekranik(k.root, "tablica jackpot -z", tablicaMat, c + q * new Vector3(0f, 0f, -.08f - Przed), 2.40f, .60f, q * Quaternion.Euler(0f, 180f, 0f)));
        foreach (float s in new[] { -1f, 1f })
        {
            var a = c + q * new Vector3(s * 1.05f, .36f, 0f);
            if (hangTo > a.y + .05f) k.body.Tube(stal, a, new Vector3(a.x, hangTo, a.z), .004f, 6);
        }
    }

    // ================================================================ preparing (once per build)
    static readonly Quaternion DeckMechQ = Quaternion.Euler(9f, 0f, 0f), DeckEkranQ = Quaternion.Euler(10f, 0f, 0f);
    static readonly Vector3 DeckMechC = new Vector3(0f, .97f, .31f), DeckEkranC = new Vector3(0f, .95f, .40f);
    static readonly Vector3 DeckMechSize = new Vector3(.64f, .05f, .15f), DeckEkranSize = new Vector3(.68f, .05f, .24f);
    struct Deck { public Vector3 c, size; public Quaternion q; public Vector3 Top(float x, float z) => c + q * new Vector3(x, size.y * .5f, z); }
    static Deck DeckMech => new Deck { c = DeckMechC, size = DeckMechSize, q = DeckMechQ };
    static Deck DeckEkran => new Deck { c = DeckEkranC, size = DeckEkranSize, q = DeckEkranQ };

    static bool gotowe;
    public static void Przygotuj()
    {
        if (gotowe && chrom && pasekBeben != null && pasekBeben[0] && quad) return;
        BraxChampionsMeshBatch.Folder(Out); BraxChampionsMeshBatch.Folder(Out + "/Meshes"); BraxChampionsMeshBatch.Folder(Out + "/Textures");
        Directory.CreateDirectory(Out + "/Textures"); Directory.CreateDirectory(Out + "/Meshes");
        Materialy();
        quad = ZapiszSiatke(Kwadrat(), "kasyno_ekran_kwadrat");
        bebenSiatki = new Mesh[4]; for (int r = 0; r < 4; r++) bebenSiatki[r] = ZapiszSiatke(Beben(r), r < 3 ? "kasyno_beben_" + (r + 1) : "kasyno_beben_kolo_3");
        kolumnaStart = new Mesh[4, 5];
        for (int v = 0; v < 4; v++) for (int r = 0; r < 5; r++) kolumnaStart[v, r] = ZapiszSiatke(KolumnaStart(v, r), "kasyno_kolumna_" + (v + 1) + "_" + (r + 1));
        CzesciRuchome();
        CzesciKola();
        RuletkaCzesci();
        szablon = new BraxChampionsMeshBatch[G.Ile, Motywy.Length];
        for (int t = 0; t < Motywy.Length; t++)
        {
            szablon[G.Mech, t] = Mechaniczny(t, false); szablon[G.Kolo, t] = Mechaniczny(t, true);
            szablon[G.Video, t] = Ekranowy(t, G.Video); szablon[G.Poker, t] = Ekranowy(t, G.Poker); szablon[G.Keno, t] = Ekranowy(t, G.Keno);
        }
        gotowe = true;
    }

    // ---------------- materials
    static Material Lakier(string id, Color c)
    {
        // the region's metal scan as lacquered steel: a coloured tint has the map's brass hue divided out (LibraryMetal does it
        // for grey ones), dielectric paint, gloss
        float spread = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        if (spread >= .12f)
        {
            float N(float v, float m) => Mathf.LinearToGammaSpace(Mathf.Min(12f, Mathf.GammaToLinearSpace(Mathf.Max(0f, v)) / m));
            c = new Color(N(c.r, .991f), N(c.g, .768f), N(c.b, .332f), 1f);
        }
        var m = BraxChampionsFoundation.LibraryMetal("kasyno_" + id, c);
        m.SetFloat("_Metallic", 0f); m.SetFloat("_Roughness", .32f); Wnetrze(m); return m;
    }
    static Material Metal(string id, Color c, float rough, float metal = 1f)
    {
        var m = BraxChampionsFoundation.LibraryMetal("kasyno_" + id, c);
        m.SetFloat("_Roughness", rough); m.SetFloat("_Metallic", metal); Wnetrze(m); return m;
    }
    static void Wnetrze(Material m) { BraxChampionsInteriorLight.Apply(m, BraxChampionsInteriorLight.Walls); EditorUtility.SetDirty(m); }
    static Material Swiatlo(string id, Color e) => BraxChampionsHarbour.Surface("kasyno_" + id, new Color(Mathf.Min(1f, e.r * .6f), Mathf.Min(1f, e.g * .6f), Mathf.Min(1f, e.b * .6f)), .3f, 0f, e);
    // a screen / a back-lit print: its picture is its light; drawn just in front of the face it sits on (depth offset)
    static Material Ekran(string id, Texture2D t, float jasnosc)
    {
        var m = BraxChampionsHarbour.Surface("kasyno_" + id, Color.white, .18f, 0f, new Color(jasnosc, jasnosc, jasnosc * 1.03f));
        m.SetTexture("_MainTex", t); m.SetTexture("_EmissionMap", t); m = BraxChampionsHarbour.Nakladka(m); EditorUtility.SetDirty(m); return m;
    }
    static void Materialy()
    {
        lakier = new Material[Motywy.Length]; swiatlo = new Material[Motywy.Length];
        szyld = new Material[Motywy.Length];
        for (int t = 0; t < Motywy.Length; t++) { lakier[t] = Lakier("lakier_" + t, Lakiery[t]); swiatlo[t] = Swiatlo("led_" + t, Swiatla[t]); }
        chrom = Metal("chrom", new Color(.93f, .93f, .95f), .07f);
        stal = Metal("stal_szczotkowana", new Color(.72f, .72f, .74f), .38f);
        czern = Metal("czarny_polysk", new Color(.03f, .03f, .035f), .16f, 0f);
        plastik = Metal("czarny_mat", new Color(.045f, .045f, .05f), .60f, 0f);
        glosnik = Metal("siatka_glosnika", new Color(.07f, .07f, .08f), .55f, .7f);
        kula = Metal("kula_zywica", new Color(.62f, .03f, .04f), .10f, 0f);
        skora = BraxChampionsFoundation.LibraryLeather("kasyno_skora_czarna", new Color(.30f, .52f, .70f)); Wnetrze(skora);
        szklo = BraxChampionsHarbour.Surface("kasyno_szklo", new Color(.82f, .88f, .92f, .10f), .03f, 0f, null, true);
        linia = Swiatlo("linia_wygranej", new Color(1.6f, .08f, .06f));
        pCzerwony = Swiatlo("przycisk_czerwony", new Color(1.5f, .10f, .07f)); pZielony = Swiatlo("przycisk_zielony", new Color(.18f, 1.4f, .35f));
        pBialy = Swiatlo("przycisk_bialy", new Color(1.25f, 1.25f, 1.18f)); pZolty = Swiatlo("przycisk_zolty", new Color(1.5f, 1.05f, .12f));
        swiecaCzerwona = Swiatlo("swieca_czerwona", new Color(1.6f, .10f, .08f)); swiecaBiala = Swiatlo("swieca_biala", new Color(1.4f, 1.4f, 1.35f));
        // the symbols: a theme's atlas cut from the owner's strip, the pay tables made of it, the screens
        int n = Motywy.Length, brak = 0;
        pasekBeben = new Material[n]; ekranBebny = new Material[n]; tabelaMech = new Material[n]; tabelaEkran = new Material[n]; atlasPx = new Color32[n][];
        for (int t = 0; t < n; t++)
        {
            var atlas = Atlas(t, ref brak);
            pasekBeben[t] = BraxChampionsHarbour.Surface("kasyno_pasek_bebna_" + t, Color.white, .45f, 0f, new Color(.40f, .40f, .40f));
            pasekBeben[t].SetTexture("_MainTex", atlas); pasekBeben[t].SetTexture("_EmissionMap", atlas); EditorUtility.SetDirty(pasekBeben[t]);
            ekranBebny[t] = Ekran("ekran_bebny_" + t, atlas, 1.0f);
            tabelaMech[t] = Ekran("tabela_mechaniczna_" + t, Png("kasyno_tabela_mechaniczna_" + t, 1024, 512, TabelaMech(t), true), .85f);
            tabelaEkran[t] = Ekran("tabela_ekranowa_" + t, Png("kasyno_tabela_ekranowa_" + t, 1024, 576, TabelaEkran(t), true), 1.0f);
        }
        Brakujace = brak; SymboleWlasciciela = brak == 0;
        if (brak > 0) BraxBuildQuality.Warn("DO POPRAWKI KASYNO_PASKI_WLASCICIELA missing " + brak + "/" + n + " in " + Wlasciciel + "/paski: those themes show placeholder strips (the symbol's name)");
        tloEkran = Ekran("tlo_ekranu", Png("kasyno_tlo_ekranu", 1024, 576, TloEkranu(), true), 1.0f);
        var lm = new Color32[A.LicznikW * A.LicznikH]; A.PaintLicznik(lm, false, 0, 0, Stawka);
        licznikMech = Ekran("licznik_mechaniczny", Male("kasyno_licznik_mechaniczny", A.LicznikW, A.LicznikH, lm), 1.1f);
        var le = new Color32[A.LicznikW * A.LicznikH]; A.PaintLicznik(le, true, 0, 0, Stawka);
        licznikEkran = Ekran("licznik_ekranowy", Male("kasyno_licznik_ekranowy", A.LicznikW, A.LicznikH, le), 1.0f);
        var jp = new Color32[A.JackW * A.JackH]; A.PaintJackpot(jp, BraxChampionsKasyno.JackpotStart);
        jackpotMat = Ekran("jackpot", Male("kasyno_jackpot", A.JackW, A.JackH, jp), 1.1f);
        var tb = new Color32[A.TabW * A.TabH]; A.PaintTablica(tb, BraxChampionsKasyno.JackpotStart);
        tablicaMat = Ekran("tablica_jackpot", Male("kasyno_tablica_jackpot", A.TabW, A.TabH, tb), 1.2f);
        for (int t = 0; t < Motywy.Length; t++) szyld[t] = Ekran("szyld_" + t, Png("kasyno_szyld_" + t, 1024, 512, Szyld(t), true), 1.0f);
        GryMaterialy();
    }
    // the mechanical machine's top glass, printed and lit from behind (as the real ones): BRAXHORN in the region's sign letters
    // (the 3D letter kit, its faces rasterised) in silver with a dark edge on the theme's light; the kit missing - the LED font
    static Color32[] Szyld(int t)
    {
        const int w = 1024, h = 512; var px = new Color32[w * h]; var c = Swiatla[t];
        float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        for (int y = 0; y < h; y++)
        {
            float k = .55f + .45f * Mathf.Sin(Mathf.PI * y / h);               // brighter in the middle, as a tube behind the glass
            var col = new Color32((byte)(255 * Mathf.Clamp01(c.r / mx * k)), (byte)(255 * Mathf.Clamp01(c.g / mx * k)), (byte)(255 * Mathf.Clamp01(c.b / mx * k)), 255);
            for (int x = 0; x < w; x++) px[y * w + x] = col;
        }
        Ramka(px, w, h, 10, 10, w - 10, h - 10, 6, new Color32(20, 16, 12, 255));
        if (!Napis(px, w, h, "BRAXHORN", w * .5f, h * .5f, .52f * h, .86f * w, 0f, new Color32(232, 234, 240, 255), 7f, new Color32(18, 14, 10, 255)))
        {
            BraxBuildQuality.Warn("DO POPRAWKI KASYNO_SZYLD letter kit missing: the top glass carries BRAXHORN in the LED font");
            BraxChampionsPiksele.Srodek(px, w, h, "BRAXHORN", w / 2, h / 2 - 56, 16, new Color32(236, 238, 242, 255));
        }
        return px;
    }
    // lettering in the region's sign typeface (the 3D letter kit, Montserrat SemiBold: its faces rasterised) into a picture:
    // centred on (cx, cy), cap height cap px, at most maxW wide, turned by deg (counter-clockwise), a dark rim of rim px
    static bool Napis(Color32[] px, int w, int h, string text, float cx, float cy, float cap, float maxW, float deg, Color32 col, float rim, Color32 rimCol)
    {
        var b = new BraxChampionsMeshBatch(); var face = new Material(Shader.Find(BraxCityMaterials.ShaderName)) { name = "napis lico" }; var dark = new Material(face) { name = "napis bok" };
        try
        {
            float sz = BraxChampionsLetters3D.Text(b, face, dark, text.Replace('·', '•'), Vector3.zero, 0f, 1f, .01f, .10f);   // the kit's dot is the bullet
            var tri = b.Trojkaty(face);
            if (sz <= 0f || tri.Count == 0) return false;
            float s = Mathf.Min(cap, maxW / sz), ca = Mathf.Cos(deg * Mathf.Deg2Rad), sa = Mathf.Sin(deg * Mathf.Deg2Rad);
            // the kit's text runs toward -x (seen from its front): the reader's x is -x; centre the cap height on cy
            Vector2 P(Vector3 v) { float x = -v.x * s, y = (v.y - .36f) * s; return new Vector2(cx + x * ca - y * sa, cy + x * sa + y * ca); }
            if (rim > 0f) for (int i = 0; i + 2 < tri.Count; i += 3) Trojkat(px, w, h, P(tri[i]), P(tri[i + 1]), P(tri[i + 2]), rim, rimCol);
            for (int i = 0; i + 2 < tri.Count; i += 3) Trojkat(px, w, h, P(tri[i]), P(tri[i + 1]), P(tri[i + 2]), 0f, col);
            return true;
        }
        finally { UnityEngine.Object.DestroyImmediate(face); UnityEngine.Object.DestroyImmediate(dark); }
    }
    static void Trojkat(Color32[] px, int w, int h, Vector2 a, Vector2 bb, Vector2 cc, float grow, Color32 col)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(bb.x, cc.x)) - grow)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(bb.x, cc.x)) + grow));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(bb.y, cc.y)) - grow)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(bb.y, cc.y)) + grow));
        float E(Vector2 p, Vector2 q, Vector2 r) => (q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x);
        float area = E(a, bb, cc); if (Mathf.Abs(area) < 1e-6f) return; float sg = Mathf.Sign(area);
        float la = (bb - a).magnitude, lb = (cc - bb).magnitude, lc = (a - cc).magnitude;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + .5f, y + .5f);
                if (sg * E(bb, cc, p) >= -grow * lb && sg * E(cc, a, p) >= -grow * lc && sg * E(a, bb, p) >= -grow * la) px[y * w + x] = col;
            }
    }

    static Texture2D Png(string name, int w, int h, Color32[] px, bool mips)
    {
        string path = Out + "/Textures/" + name + ".png";
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.SetPixels32(px); t.Apply(false);
        File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Default; imp.sRGBTexture = true; imp.alphaSource = TextureImporterAlphaSource.None;
            imp.mipmapEnabled = mips; imp.streamingMipmaps = mips; imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Trilinear; imp.anisoLevel = 4; imp.maxTextureSize = 2048;
            var a = imp.GetPlatformTextureSettings("Android"); a.overridden = true; a.maxTextureSize = 2048; a.format = TextureImporterFormat.ASTC_4x4;
            imp.SetPlatformTextureSettings(a); imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    // the small LED pictures (the runtime repaints them): kept as they are, point-sampled
    static Texture2D Male(string name, int w, int h, Color32[] px)
    {
        string path = Out + "/Textures/" + name + ".asset";
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "kasyno " + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        t.SetPixels32(px); t.Apply(false);
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(t, path);
        return t;
    }
    static Color32[] Wczytaj(string path, out int w, out int h)
    {
        w = h = 0; if (!File.Exists(path)) return null;
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try { if (!t.LoadImage(File.ReadAllBytes(path))) return null; w = t.width; h = t.height; return t.GetPixels32(); }
        finally { UnityEngine.Object.DestroyImmediate(t); }
    }

    // a theme's reel atlas: the owner's strip cut into the game's seven symbols - each cell his band from the middle between
    // a symbol and the one above to the middle to the one below, his full width (side frame included), scaled to 512 x 256
    // (his art 1:1, nothing of ours on it); without his strip a plain placeholder (the symbol's name) and a warning
    static Texture2D Atlas(int t, ref int brak)
    {
        var px = new Color32[Z.AtlasW * Z.AtlasH]; BraxChampionsPiksele.Wypelnij(px, new Color32(0, 0, 0, 255));
        var src = Wczytaj(Wlasciciel + "/paski/" + Paski[t] + ".png", out int sw, out int sh);
        if (src == null) brak++;
        int x0 = 0, x1 = sw;
        if (src != null)
        {
            // his strip's width: the columns that are opaque somewhere (the glow outside his frame is transparent)
            x0 = sw; x1 = 0;
            for (int y = 0; y < sh; y += 4) for (int x = 0; x < sw; x++) if (src[y * sw + x].a > 200) { if (x < x0) x0 = x; if (x + 1 > x1) x1 = x + 1; }
        }
        var ys = PasekY[t]; var ks = PasekSymbole[t];
        for (int s = 0; s < Z.Symbole; s++)
        {
            Z.KomorkaPx(s, out int cx, out int cy);
            int j = Array.IndexOf(ks, s);
            if (src == null || j < 0 || sh < ys[ys.Length - 1]) { KomorkaZastepcza(px, cx, cy, s, t); continue; }
            float up = j > 0 ? (ys[j] - ys[j - 1]) * .5f : (ys[1] - ys[0]) * .5f, dn = j + 1 < ys.Length ? (ys[j + 1] - ys[j]) * .5f : (ys[j] - ys[j - 1]) * .5f;
            float top = ys[j] - up, bot = ys[j] + dn;              // his rows from the top
            for (int y = 0; y < Z.KomorkaH; y++)
                for (int x = 0; x < Z.KomorkaW; x++)
                {
                    // bilinear in his picture over black; the cell's top row is his band's top
                    float fx = x0 + (x + .5f) / Z.KomorkaW * (x1 - x0) - .5f, fyTop = top + (Z.KomorkaH - 1 - y + .5f) / Z.KomorkaH * (bot - top) - .5f;
                    float fy = sh - 1 - fyTop;                      // Unity rows go up
                    px[(cy + y) * Z.AtlasW + cx + x] = Probka(src, sw, sh, fx, fy);
                }
        }
        KomorkaKolo(px, t);                                       // the eighth cell: the wheel machine's KOŁO symbol
        atlasPx[t] = px;
        return Png("kasyno_bebny_atlas_" + t, Z.AtlasW, Z.AtlasH, px, true);
    }
    static Color32 Probka(Color32[] src, int w, int h, float fx, float fy)
    {
        int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, w - 2), iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, h - 2);
        float tx = Mathf.Clamp01(fx - ix), ty = Mathf.Clamp01(fy - iy);
        Color32 a = src[iy * w + ix], b = src[iy * w + ix + 1], c = src[(iy + 1) * w + ix], d = src[(iy + 1) * w + ix + 1];
        float C(byte p, byte q, byte r, byte s, byte pa, byte qa, byte ra, byte sa)
        {
            float top = Mathf.Lerp(p * pa / 255f, q * qa / 255f, tx), bot = Mathf.Lerp(r * ra / 255f, s * sa / 255f, tx);
            return Mathf.Lerp(top, bot, ty);                        // premultiplied: over black
        }
        return new Color32((byte)Mathf.Clamp(C(a.r, b.r, c.r, d.r, a.a, b.a, c.a, d.a), 0, 255), (byte)Mathf.Clamp(C(a.g, b.g, c.g, d.g, a.a, b.a, c.a, d.a), 0, 255),
                           (byte)Mathf.Clamp(C(a.b, b.b, c.b, d.b, a.a, b.a, c.a, d.a), 0, 255), 255);
    }
    static void KomorkaZastepcza(Color32[] px, int x0, int y0, int s, int t)
    {
        int W = Z.KomorkaW, H = Z.KomorkaH; var c = new Color32[W * H]; var l = Lakiery[t];
        BraxChampionsPiksele.Wypelnij(c, new Color32((byte)(40 + 160 * l.r), (byte)(40 + 160 * l.g), (byte)(40 + 160 * l.b), 255));
        string tx = s == 0 ? "7" : Z.Nazwy[s]; int sk = s == 0 ? 22 : Mathf.Max(2, Mathf.Min(6, 280 / Mathf.Max(1, BraxChampionsPiksele.Szer(tx, 1) + 1)));
        BraxChampionsPiksele.Srodek(c, W, H, tx, W / 2, (H - 7 * sk) / 2, sk, new Color32(240, 236, 228, 255));
        for (int y = 0; y < H; y++) Array.Copy(c, y * W, px, (y0 + y) * Z.AtlasW + x0, W);
    }
    // a symbol from a theme's atlas into a picture (the screen cell: the middle of the cell squeezed to a square, box
    // filter), x / y its lower left corner
    static void Symbolik(Color32[] px, int w, int h, int t, int s, int x, int y, int size)
    {
        var at = atlasPx[t]; Z.KomorkaPx(s, out int ax, out int ay);
        int SW = Mathf.RoundToInt(Z.Kwadrat * Z.KomorkaW), SH = Z.KomorkaH; ax += (Z.KomorkaW - SW) / 2;
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                int sx0 = i * SW / size, sx1 = Mathf.Max(sx0 + 1, (i + 1) * SW / size), sy0 = j * SH / size, sy1 = Mathf.Max(sy0 + 1, (j + 1) * SH / size);
                int cr = 0, cg = 0, cb = 0, k = 0;
                for (int yy = sy0; yy < sy1; yy++) for (int xx = sx0; xx < sx1; xx++) { var c = at[(ay + yy) * Z.AtlasW + ax + xx]; cr += c.r; cg += c.g; cb += c.b; k++; }
                int X = x + i, Y = y + j; if (X >= 0 && X < w && Y >= 0 && Y < h) px[Y * w + X] = new Color32((byte)(cr / k), (byte)(cg / k), (byte)(cb / k), 255);
            }
    }
    static void Ramka(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, int t, Color32 c)
    {
        BraxChampionsPiksele.Prost(px, w, h, x0, y0, x1, y0 + t, c); BraxChampionsPiksele.Prost(px, w, h, x0, y1 - t, x1, y1, c);
        BraxChampionsPiksele.Prost(px, w, h, x0, y0, x0 + t, y1, c); BraxChampionsPiksele.Prost(px, w, h, x1 - t, y0, x1, y1, c);
    }
    static readonly Color32 Zloto = new Color32(232, 186, 74, 255), Biel = new Color32(238, 240, 246, 255), Szary = new Color32(150, 156, 170, 255);
    // the mechanical cabinet's back-lit pay glass (1024 x 512): every win at the stake, made of the theme's symbols
    static Color32[] TabelaMech(int t)
    {
        const int w = 1024, h = 512; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, new Color32(12, 8, 10, 255));
        Ramka(px, w, h, 8, 8, w - 8, h - 8, 3, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "BRAXHORN · TABELA WYGRANYCH", w / 2, 462, 4, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "STAWKA " + Stawka + " · WYGRANE W KREDYTACH", w / 2, 434, 2, Szary);
        var wiersze = new List<(int[] s, string v)>
        {
            (new[]{0,0,0}, "JACKPOT"), (new[]{1,1,1}, (Z.Trzy[1]*Stawka).ToString()), (new[]{3,3,3}, (Z.Trzy[3]*Stawka).ToString()),
            (new[]{4,4,4}, (Z.Trzy[4]*Stawka).ToString()), (new[]{5,5,5}, (Z.Trzy[5]*Stawka).ToString()), (new[]{6,6,6}, (Z.Trzy[6]*Stawka).ToString()),
            (new[]{6,6,-1}, (Z.Wisnie2*Stawka).ToString()), (new[]{6,-1,-1}, (Z.Wisnia1*Stawka).ToString()),
        };
        for (int i = 0; i < wiersze.Count; i++)
        {
            int col = i / 4, row = i % 4, x = 40 + col * 500, y = 338 - row * 80;
            for (int k = 0; k < 3; k++)
            {
                int s = wiersze[i].s[k], sx = x + k * 72;
                if (s >= 0) { Symbolik(px, w, h, t, s, sx, y, 66); Ramka(px, w, h, sx - 2, y - 2, sx + 68, y + 68, 2, Zloto); }
                else Ramka(px, w, h, sx - 2, y - 2, sx + 68, y + 68, 2, Szary);   // any symbol
            }
            BraxChampionsPiksele.Tekst(px, w, h, "= " + wiersze[i].v, x + 228, y + 19, 4, i == 0 ? Zloto : Biel);
        }
        BraxChampionsPiksele.Srodek(px, w, h, "GWIAZDA NA ŚRODKOWYM BĘBNIE ZASTĘPUJE KAŻDY SYMBOL OPRÓCZ SIÓDEMKI", w / 2, 18, 2, Szary);
        return px;
    }
    // the screen machine's upper screen (1024 x 576): 3 / 4 / 5 of a kind on a line, at the stake, the theme's symbols
    static Color32[] TabelaEkran(int t)
    {
        const int w = 1024, h = 576; var px = TloGradient(w, h);
        Ramka(px, w, h, 8, 8, w - 8, h - 8, 3, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "BRAXHORN · 5 LINII", w / 2, 522, 4, Zloto);
        BraxChampionsPiksele.Srodek(px, w, h, "STAWKA " + Stawka + " · 3 / 4 / 5 TAKICH SAMYCH OD LEWEJ NA LINII", w / 2, 496, 2, Szary);
        int[] kol = { 110, 216, 336 };
        for (int c = 0; c < 2; c++) for (int k = 0; k < 3; k++) BraxChampionsPiksele.Tekst(px, w, h, (k + 3) + " x", 44 + c * 500 + kol[k], 468, 2, Szary);
        for (int s = 0; s < Z.Symbole; s++)
        {
            int col = s / 4, row = s % 4, x = 44 + col * 500, y = 376 - row * 94;
            Symbolik(px, w, h, t, s, x, y, 80); Ramka(px, w, h, x - 2, y - 2, x + 82, y + 82, 2, Zloto);
            for (int k = 0; k < 3; k++)
            {
                string v = s == 0 && k == 2 ? "JACKPOT" : (Z.Ekranowy[s, k] * Stawka / 5).ToString();
                BraxChampionsPiksele.Tekst(px, w, h, v, x + kol[k], y + 30, v.Length > 5 ? 2 : 3, s == 0 ? Zloto : Biel);
            }
        }
        BraxChampionsPiksele.Srodek(px, w, h, "GWIAZDA ZASTĘPUJE KAŻDY SYMBOL OPRÓCZ SIÓDEMKI · 5 x SIÓDEMKA = JACKPOT", w / 2, 24, 2, Szary);
        return px;
    }
    static Color32[] TloGradient(int w, int h)
    {
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++) { float t = (float)y / h; var c = new Color32((byte)(3 + 8 * t), (byte)(4 + 10 * t), (byte)(14 + 32 * t), 255); for (int x = 0; x < w; x++) px[y * w + x] = c; }
        return px;
    }
    // the main screen's picture round the reels (1024 x 576 for 0.598 x 0.336 m): the reel window framed in gold, the five
    // lines' markers at its sides, the meter's bar at the bottom; the reels themselves are the game's (in front of it)
    public const float EkranW = .598f, EkranH = .336f;
    static Color32[] TloEkranu()
    {
        const int w = 1024, h = 576; var px = TloGradient(w, h); float k = w / EkranW;
        int cx = w / 2, cy = h / 2 + Mathf.RoundToInt(KolumnyY * k), hw = Mathf.RoundToInt((2.5f * Komorka + 2f * .008f) * k), hh = Mathf.RoundToInt(1.5f * Komorka * k);
        BraxChampionsPiksele.Prost(px, w, h, cx - hw, cy - hh, cx + hw, cy + hh, new Color32(0, 0, 0, 255));
        Ramka(px, w, h, cx - hw - 8, cy - hh - 8, cx + hw + 8, cy + hh + 8, 4, Zloto);
        int cell = Mathf.RoundToInt(Komorka * k);
        int[] linia = { 1, 0, 2, 0, 2 }; int[] przes = { 0, 0, 0, -34, 34 };
        for (int l = 0; l < 5; l++)
        {
            int y = cy + (1 - linia[l]) * cell + przes[l];
            foreach (int x in new[] { cx - hw - 44, cx + hw + 16 })
            {
                BraxChampionsPiksele.Prost(px, w, h, x, y - 13, x + 28, y + 13, Zloto);
                BraxChampionsPiksele.Srodek(px, w, h, (l + 1).ToString(), x + 14, y - 7, 2, new Color32(20, 12, 4, 255));
            }
        }
        int by = h / 2 + Mathf.RoundToInt(LicznikY * k);
        BraxChampionsPiksele.Prost(px, w, h, 40, by - 24, w - 40, by + 24, new Color32(10, 10, 14, 255));
        Ramka(px, w, h, 40, by - 24, w - 40, by + 24, 2, Zloto);
        return px;
    }

    // ---------------- meshes
    sealed class Siatka
    {
        public readonly List<Vector3> v = new List<Vector3>(), n = new List<Vector3>(); public readonly List<Vector2> uv = new List<Vector2>(); public readonly List<int> t = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 na, Vector3 nb, Vector3 nc, Vector3 nd, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); n.Add(na); n.Add(nb); n.Add(nc); n.Add(nd); uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        public Mesh Mesh(string name)
        {
            var m = new Mesh { name = name }; m.vertices = v.ToArray(); m.normals = n.ToArray(); m.uv = uv.ToArray(); m.triangles = t.ToArray();
            m.RecalculateTangents(); m.RecalculateBounds(); return m;
        }
    }
    static Mesh ZapiszSiatke(Mesh m, string name)
    {
        string path = Out + "/Meshes/" + name + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old) { EditorUtility.CopySerialized(m, old); UnityEngine.Object.DestroyImmediate(m); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(m, path); return m;
    }
    // a display face: a unit quad facing +z, uv 0..1 read from the front (the reader's right is -x)
    static Mesh Kwadrat()
    {
        var s = new Siatka(); var f = Vector3.forward;
        s.Quad(new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0), f, f, f, f,
               new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
        return s.Mesh("kasyno ekran kwadrat");
    }
    // a drum's strip: 20 stops of the reel's strip round the drum, 3 facets a stop, smooth; stop j at j x 18 degrees above the
    // front (the drum turned by stop x 18 degrees about x brings stop to the window), the strip upright to the player
    static Mesh Beben(int r)
    {
        var s = new Siatka(); var pasek = r < 3 ? Z.Pasek(r, false) : KG.Pasek3; const int sub = 3; float w = BebenSzer * .5f;
        Vector3 P(float x, float a) => new Vector3(x, BebenR * Mathf.Sin(a), BebenR * Mathf.Cos(a));
        Vector3 N(float a) => new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a));
        for (int j = 0; j < Z.Stopy; j++)
        {
            Z.Komorka(pasek[j], false, out float u0, out float v0, out float u1, out float v1);
            for (int k = 0; k < sub; k++)
            {
                float a0 = (j - .5f + (float)k / sub) * 18f * Mathf.Deg2Rad, a1 = (j - .5f + (float)(k + 1) / sub) * 18f * Mathf.Deg2Rad;
                float t0 = v0 + (v1 - v0) * k / sub, t1 = v0 + (v1 - v0) * (k + 1) / sub;
                s.Quad(P(-w, a0), P(w, a0), P(w, a1), P(-w, a1), N(a0), N(a0), N(a1), N(a1), new Vector2(u1, t0), new Vector2(u0, t0), new Vector2(u0, t1), new Vector2(u1, t1));
            }
        }
        return s.Mesh(r < 3 ? "kasyno bęben " + (r + 1) : "kasyno bęben koło 3");
    }
    // a screen reel at rest (the generator's picture before the game starts)
    static Mesh KolumnaStart(int v, int r)
    {
        int[] stopy = { (3 + 7 * v) % 20, (11 + 5 * v) % 20, (6 + 9 * v) % 20, (15 + 3 * v) % 20, (8 + 11 * v) % 20 };
        var vv = new Vector3[16]; var uv = new Vector2[16]; Z.Kolumna(vv, uv, Komorka, Z.Pasek(r, true), stopy[r]);
        var m = new Mesh { name = "kasyno kolumna " + (v + 1) + "_" + (r + 1) }; m.vertices = vv; m.uv = uv; m.triangles = Z.KolumnaTrojkaty;
        var nr = new Vector3[16]; for (int i = 0; i < 16; i++) nr[i] = Vector3.forward; m.normals = nr; m.RecalculateBounds(); return m;
    }
    // the lever (pivot at its hub: the arm up, the red ball on top) and the SPIN buttons, built once, placed by reference
    static void CzesciRuchome()
    {
        var b = new BraxChampionsMeshBatch();
        b.Cylinder(chrom, new Vector3(.0f, 0f, 0f), .035f, .045f, 16, Quaternion.Euler(0f, 0f, 90f), true, true);     // the hub boss (toward -x)
        b.Tube(chrom, new Vector3(-.022f, 0f, 0f), new Vector3(-.022f, .40f, .035f), .013f, 10);
        b.Tube(chrom, new Vector3(-.022f, .40f, .035f), new Vector3(-.022f, .425f, .038f), .009f, 8);
        czRamie = Czesc(b, "dźwignia ramię");
        b.Lathe(kula, new Vector3(-.022f, .418f, .038f), Profil(.045f, 8), 14);
        czKula = Czesc(b, "dźwignia kula");
        b.Cylinder(pCzerwony, Vector3.zero, .028f, .016f, 14, Quaternion.identity, true);
        czSpin = Czesc(b, "spin mechaniczny");
        b.Cylinder(pZielony, Vector3.zero, .036f, .018f, 16, Quaternion.identity, true);
        czSpinEkran = Czesc(b, "spin ekranowy");
    }
    static Vector2[] Profil(float r, int n)
    {
        // a ball's half profile from the bottom pole to the top
        var p = new Vector2[n + 1];
        for (int i = 0; i <= n; i++) { float a = -Mathf.PI * .5f + Mathf.PI * i / n; p[i] = new Vector2(Mathf.Max(0f, r * Mathf.Cos(a)), r + r * Mathf.Sin(a)); }
        p[0].x = 0f; p[n].x = 0f; return p;
    }
    static List<(Mesh, Material)> Czesc(BraxChampionsMeshBatch b, string name)
    {
        var tmp = new GameObject("kasyno szablon " + name).transform;
        var g = b.Build(tmp, "kasyno " + name, Out + "/Meshes", false, 1f);
        var list = new List<(Mesh, Material)>();
        foreach (var mf in g.GetComponentsInChildren<MeshFilter>(true)) list.Add((mf.sharedMesh, mf.GetComponent<MeshRenderer>().sharedMaterial));
        UnityEngine.Object.DestroyImmediate(tmp.gameObject);
        return list;
    }
    static void Wstaw(List<(Mesh, Material)> parts, Transform parent, string name)
    {
        foreach (var (mesh, mat) in parts)
        {
            var o = new GameObject(name + " | " + mat.name); o.transform.SetParent(parent, false);
            o.AddComponent<MeshFilter>().sharedMesh = mesh; Cienie(o.AddComponent<MeshRenderer>()).sharedMaterial = mat;
        }
    }
    static MeshRenderer Cienie(MeshRenderer r) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return r; }
    static Transform Spin(Transform kor, Deck d, Vector3 at, List<(Mesh, Material)> czesc)
    {
        var t = new GameObject("przycisk SPIN").transform; t.SetParent(kor, false);
        t.localPosition = d.Top(at.x, at.z); t.localRotation = d.q; Wstaw(czesc, t, "SPIN"); return t;
    }
    static MeshRenderer Ekranik(Transform parent, string name, Material mat, Vector3 c, float w, float h, Quaternion q)
    {
        var o = new GameObject(name); o.transform.SetParent(parent, false);
        o.transform.localPosition = c; o.transform.localRotation = q; o.transform.localScale = new Vector3(w, h, 1f);
        o.AddComponent<MeshFilter>().sharedMesh = quad; var r = Cienie(o.AddComponent<MeshRenderer>()); r.sharedMaterial = mat; return r;
    }

    // ================================================================ roulette (owner 16.11 19:13: "ruletki prawdziwe")
    // A European single-zero table as the real ones (2.7 x 1.24 m, the 32" wheel at its end, the layout on green felt):
    // a walnut cabinet and top, black leather armrest on the players' side, the felt (Poly Haven velour_velvet, casino green)
    // with the printed layout - numbers 0-36 in the real red / black, dozens, columns, the six outside boxes - in the region's
    // sign typeface; the wheel: a walnut bowl with the ball track and eight chrome deflectors on the cone, the rotor with the
    // number ring in the real order, 37 pockets between chrome frets, the walnut cone and the chrome turret; the ivory ball; the
    // history board on a chrome pole. The game: BraxChampionsRuletka.
    public static readonly Vector3 Kolo = new Vector3(.88f, .78f, 0f);           // the wheel's centre on the table (local)
    public const float TorR = .37f, TorY = .050f, KieszenR = .217f, KieszenY = .006f, KulkaR = .0095f;
    const float UkladXW = .39f, UkladL = 1.65f, UkladZN = .435f, UkladD = .85f, UkladY = .775f;
    const int UkW = 2048, UkH = 1024, UkM = 40, UkKol = 140, UkZero = 180, UkRzad = 200, UkSiatka = 320;
    static float PoleX(int e) => UkladXW - (UkZero + 2 * UkKol * e + UkKol) / (float)UkW * UkladL;   // the six outside boxes
    static float PoleZ => UkladZN - 110f / UkH * UkladD;
    public static void Ruletka(Kasyno k, Vector3 at, float yaw)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); int idx = k.ruletki.Count;
        k.body.Append(szablonRuletki, at, q);
        var stol = new GameObject("ruletka " + (idx + 1) + " | europejska (37) | BRAXHORN").transform;
        stol.SetParent(k.root, false); stol.localPosition = at; stol.localRotation = q;
        var rot = new GameObject("rotor (koło z numerami)").transform; rot.SetParent(stol, false); rot.localPosition = Kolo;
        rot.localRotation = Quaternion.AngleAxis(-(idx * 47f % 360f), Vector3.up); Wstaw(czRotor, rot, "rotor");
        var ball = new GameObject("kulka").transform; ball.SetParent(stol, false);
        float a0 = (idx * 47f % 360f) * Mathf.Deg2Rad; ball.localPosition = Kolo + new Vector3(KieszenR * Mathf.Cos(a0), KieszenY + KulkaR, KieszenR * Mathf.Sin(a0));
        Wstaw(czKulka, ball, "kulka");
        var zet = new Transform[6]; var px = new float[6];
        for (int e = 0; e < 6; e++)
        {
            px[e] = PoleX(e);
            var z = new GameObject("żeton " + BraxChampionsRuletka.Zasady.Pola[e]).transform; z.SetParent(stol, false);
            z.localPosition = new Vector3(px[e], UkladY + Przed + .001f, PoleZ); Wstaw(czZeton, z, "żeton"); z.gameObject.SetActive(false); zet[e] = z;
        }
        // the display's live layer (the game rebuilds it after every spin); the generator's sample until then
        var wo = new GameObject("wyświetlacz ruletki (numery, statystyka)"); wo.transform.SetParent(stol, false);
        wo.transform.localPosition = WysC + new Vector3(0f, 0f, .0225f + 2f * Przed);
        var wm = new Mesh { name = "ruletka wyświetlacz (próbka generatora)" };
        var hist = new int[13]; var sto = new int[100]; BraxChampionsRuletka.Wyswietlacz.Poczatek(new System.Random(77 + idx), hist, sto);
        BraxChampionsRuletka.Wyswietlacz.Zbuduj(wm, hist, sto, sto.Length);
        var wf = wo.AddComponent<MeshFilter>(); wf.sharedMesh = wm; Cienie(wo.AddComponent<MeshRenderer>()).sharedMaterial = wysAtlas;
        k.ruletki.Add(new BraxChampionsRuletka.Stol { stol = stol, rotor = rot, kulka = ball, kolo = Kolo, zetony = zet, polaX = px, wyswietlacz = wf });
        W.Kolizja(at + q * new Vector3(0f, .43f, 0f), new Vector3(2.70f, .86f, 1.24f), q);
        W.Obsada("krupier ruletki", at + q * new Vector3(.20f, 0f, -1.0f), yaw);
    }
    static void RuletkaMaterialy()
    {
        orzech = BraxChampionsFoundation.SourceMaterial("kasyno_orzech", "smoked_walnut_veneer", 1.6f, new Color(.80f, .70f, .62f)); orzech.SetFloat("_Roughness", .45f); Wnetrze(orzech);
        sukno = BraxChampionsFoundation.Fotoskan("kasyno_sukno_zielone", "velour_velvet", .6f, new Color(.10f, .36f, .20f), BraxChampionsFoundation.MeanVelvet); Wnetrze(sukno);
        kulkaMat = Metal("kulka_kosc", new Color(.95f, .94f, .90f), .12f, 0f);
        uklad = BraxChampionsHarbour.Surface("kasyno_uklad_ruletki", Color.white, .6f, 0f, null, true);
        uklad.SetTexture("_MainTex", Png("kasyno_uklad_ruletki", UkW, UkH, Uklad(), true)); uklad = BraxChampionsHarbour.Nakladka(uklad); EditorUtility.SetDirty(uklad);
        pierscien = BraxChampionsHarbour.Surface("kasyno_pierscien_ruletki", Color.white, .30f, 0f);
        pierscien.SetTexture("_MainTex", Png("kasyno_pierscien_ruletki", 2048, 128, Pierscien(), true)); Wnetrze(pierscien);
        zloto = Metal("zloto_polerowane", new Color(1.0f, .78f, .36f), .22f);
        wysTlo = Ekran("wyswietlacz_ruletki_tlo", Png("kasyno_wyswietlacz_ruletki_tlo", 576, 1024, WysTloPx(), true), 1.0f);
        wysAtlas = Ekran("wyswietlacz_ruletki_atlas", Png("kasyno_wyswietlacz_ruletki_atlas", BraxChampionsRuletka.Wyswietlacz.AtlasW, BraxChampionsRuletka.Wyswietlacz.AtlasH, WysAtlasPx(), true), 1.0f);
    }
    // ---- the winning-number display (a portrait screen on a column, after the TCS John Huxley displays)
    static readonly Vector3 WysC = new Vector3(1.24f, 1.56f, -.50f);              // the screen's centre (its glass slab's middle)
    static readonly Color32 WysCzern = new Color32(6, 6, 9, 255), WysZloto = new Color32(232, 186, 74, 255), WysSzary = new Color32(150, 154, 166, 255);
    // the screen's fixed picture (576 x 1024 for .36 x .64 m): black glass, a gold line, BRAXHORN, the labels of the live parts
    static Color32[] WysTloPx()
    {
        const int w = 576, h = 1024; var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, WysCzern);
        Ramka(px, w, h, 12, 12, w - 12, h - 12, 3, WysZloto);
        float X(float x) => w * .5f + x * 1600f; float Y(float y) => h * .5f + y * 1600f;
        void T(string t, float x, float y, float cap, float maxW, Color32 c) { if (!Napis(px, w, h, t, X(x), Y(y), cap, maxW, 0f, c, 0f, c)) BraxChampionsPiksele.Srodek(px, w, h, t, (int)X(x), (int)Y(y) - 10, 3, c); }
        T("BRAXHORN", 0f, .288f, 46f, 470f, WysZloto);
        T("RULETKA EUROPEJSKA · STAWKA " + Stawka, 0f, .252f, 17f, 480f, WysSzary);
        T("POPRZEDNIE", 0f, BraxChampionsRuletka.Wyswietlacz.HistY0 + .036f, 15f, 300f, WysSzary);
        T("OSTATNIE 100", 0f, -.050f, 15f, 300f, WysSzary);
        string[] bar = { "CZERWONE", "ZERO", "CZARNE" };
        for (int b = 0; b < 3; b++) T(bar[b], -.100f, BraxChampionsRuletka.Wyswietlacz.BarY[b], 14f, 180f, Krem);   // one size for all three
        Ramka(px, w, h, (int)X(-.17f), (int)Y(-.155f), (int)X(.17f), (int)Y(-.060f), 2, new Color32(60, 50, 30, 255));
        T("GORĄCE", -.105f, BraxChampionsRuletka.Wyswietlacz.GoraceY, 17f, 140f, new Color32(255, 128, 64, 255));
        T("ZIMNE", -.105f, BraxChampionsRuletka.Wyswietlacz.ZimneY, 17f, 140f, new Color32(120, 190, 255, 255));
        T("KASYNO", 0f, -.300f, 15f, 300f, WysZloto);
        return px;
    }
    // the live layer's atlas (1024 x 1024): the 37 numbers in discs (gold rim, red / black / green, white number), the digits
    // and %, the bars' colours - all on the screen's black, so the layer needs no transparency
    static Color32[] WysAtlasPx()
    {
        int w = BraxChampionsRuletka.Wyswietlacz.AtlasW, h = BraxChampionsRuletka.Wyswietlacz.AtlasH, K = BraxChampionsRuletka.Wyswietlacz.Krazek;
        var px = new Color32[w * h]; BraxChampionsPiksele.Wypelnij(px, WysCzern);
        for (int n = 0; n < 37; n++)
        {
            BraxChampionsRuletka.Wyswietlacz.KrazekPx(n, out int x0, out int y0);
            var c = n == 0 ? new Color32(24, 150, 70, 255) : BraxChampionsRuletka.Zasady.Czerwony(n) ? new Color32(206, 24, 32, 255) : new Color32(22, 22, 26, 255);
            float cx = x0 + K * .5f, cy = y0 + K * .5f;
            for (int y = y0; y < y0 + K; y++)
                for (int x = x0; x < x0 + K; x++)
                {
                    float d = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy));
                    Color32 o = d <= 54f ? c : d <= 61f ? WysZloto : WysCzern;
                    float e = Mathf.Clamp01(61.5f - d);                                     // a smooth outer edge
                    px[y * w + x] = d > 61f ? Color32.Lerp(WysCzern, WysZloto, e) : o;
                }
            Napis(px, w, h, n.ToString(), cx, cy, 52f, 90f, 0f, new Color32(250, 250, 252, 255), 0f, Krem);
        }
        for (int d = 0; d <= 10; d++)
        {
            BraxChampionsRuletka.Wyswietlacz.CyfraPx(d, out int x0, out int y0);
            Napis(px, w, h, d == 10 ? "%" : d.ToString(), x0 + BraxChampionsRuletka.Wyswietlacz.CyfraW * .5f, y0 + BraxChampionsRuletka.Wyswietlacz.CyfraH * .5f, 52f, 56f, 0f, new Color32(240, 240, 244, 255), 0f, Krem);
        }
        Color32[] kol = { new Color32(206, 24, 32, 255), new Color32(170, 172, 182, 255), new Color32(24, 150, 70, 255), WysZloto };
        for (int k = 0; k < kol.Length; k++) { BraxChampionsRuletka.Wyswietlacz.KolorPx(k, out int x0, out int y0); BraxChampionsPiksele.Prost(px, w, h, x0, y0, x0 + 32, y0 + 32, kol[k]); }
        return px;
    }
    static readonly Color32 Krem = new Color32(236, 228, 206, 255), KCzerwony = new Color32(172, 20, 26, 255), KCzarny = new Color32(16, 16, 18, 255),
        KZielony = new Color32(18, 112, 56, 255);
    // the layout printed on the felt (2048 x 1024 for 1.65 x 0.85 m, transparent where the felt shows): u to the players'
    // right (the wheel's end on the left), v away from the players
    static Color32[] Uklad()
    {
        var px = new Color32[UkW * UkH]; const int L = 5;
        void Pole(int x0, int y0, int x1, int y1, Color32? fill) { if (fill.HasValue) BraxChampionsPiksele.Prost(px, UkW, UkH, x0, y0, x1, y1, fill.Value); Ramka(px, UkW, UkH, x0, y0, x1, y1, L, Krem); }
        void Txt(string t, float cx, float cy, float cap, float maxW, Color32 c) { if (!Napis(px, UkW, UkH, t, cx, cy, cap, maxW, 0f, c, 0f, c)) BraxChampionsPiksele.Srodek(px, UkW, UkH, t, (int)cx, (int)(cy - 3.5f * 6), 6, c); }
        int gx0 = UkZero, gy0 = UkSiatka;
        // zero over the three rows
        Pole(UkM, gy0, gx0, gy0 + 3 * UkRzad, KZielony); Txt("0", (UkM + gx0) * .5f, gy0 + 1.5f * UkRzad, 90f, 120f, Krem);
        for (int c = 0; c < 12; c++)
            for (int r = 0; r < 3; r++)
            {
                int n = 3 * c + r + 1, x0 = gx0 + c * UkKol, y0 = gy0 + r * UkRzad;
                Pole(x0, y0, x0 + UkKol, y0 + UkRzad, BraxChampionsRuletka.Zasady.Czerwony(n) ? KCzerwony : KCzarny);
                Txt(n.ToString(), x0 + UkKol * .5f, y0 + UkRzad * .5f, 74f, UkKol - 24f, Krem);
            }
        int cx0 = gx0 + 12 * UkKol;
        for (int r = 0; r < 3; r++) { Pole(cx0, gy0 + r * UkRzad, UkW - UkM, gy0 + (r + 1) * UkRzad, null); Txt("2:1", (cx0 + UkW - UkM) * .5f, gy0 + (r + .5f) * UkRzad, 56f, 120f, Krem); }
        string[] tuziny = { "1-12", "13-24", "25-36" };
        for (int d = 0; d < 3; d++) { int x0 = gx0 + d * 4 * UkKol; Pole(x0, 180, x0 + 4 * UkKol, gy0, null); Txt(tuziny[d], x0 + 2 * UkKol, 250, 62f, 400f, Krem); }
        string[] pola = { "1-18", "PARZYSTE", "", "", "NIEPARZYSTE", "19-36" };
        for (int e = 0; e < 6; e++)
        {
            int x0 = gx0 + e * 2 * UkKol; Pole(x0, UkM, x0 + 2 * UkKol, 180, null);
            if (e == 2 || e == 3)
            {
                // the red and the black diamond
                var c0 = new Vector2(x0 + UkKol, 110); var col = e == 2 ? KCzerwony : KCzarny;
                Trojkat(px, UkW, UkH, c0 + new Vector2(-90, 0), c0 + new Vector2(0, -48), c0 + new Vector2(90, 0), 3f, Krem);
                Trojkat(px, UkW, UkH, c0 + new Vector2(-90, 0), c0 + new Vector2(90, 0), c0 + new Vector2(0, 48), 3f, Krem);
                Trojkat(px, UkW, UkH, c0 + new Vector2(-84, 0), c0 + new Vector2(0, -43), c0 + new Vector2(84, 0), 0f, col);
                Trojkat(px, UkW, UkH, c0 + new Vector2(-84, 0), c0 + new Vector2(84, 0), c0 + new Vector2(0, 43), 0f, col);
            }
            else Txt(pola[e], x0 + UkKol, 110, 58f, 2 * UkKol - 30f, Krem);
        }
        return px;
    }
    // the rotor's ring (2048 x 128): 37 segments in the wheel's order (clockwise), the number band (upper half, white numbers on red /
    // black / green, their tops to the wheel's centre) and the pocket floors (lower half)
    static Color32[] Pierscien()
    {
        const int w = 2048, h = 128; var px = new Color32[w * h];
        for (int j = 0; j < 37; j++)
        {
            // segment j sits j x 360/37 counter-clockwise on the rotor (u grows counter-clockwise so the numbers read from
            // outside): the wheel's order runs clockwise, so it is read backwards from the zero
            int n = BraxChampionsRuletka.Zasady.Kolo[(37 - j) % 37]; int x0 = Mathf.RoundToInt(j * w / 37f), x1 = Mathf.RoundToInt((j + 1) * w / 37f);
            var c = n == 0 ? KZielony : BraxChampionsRuletka.Zasady.Czerwony(n) ? KCzerwony : KCzarny;
            var dark = new Color32((byte)(c.r * .7f), (byte)(c.g * .7f), (byte)(c.b * .7f), 255);
            BraxChampionsPiksele.Prost(px, w, h, x0, 0, x1, 64, dark); BraxChampionsPiksele.Prost(px, w, h, x0, 64, x1, 128, c);
            BraxChampionsPiksele.Prost(px, w, h, x0, 64, x0 + 2, 128, new Color32(200, 170, 90, 255));            // the brass line between numbers
            if (!Napis(px, w, h, n.ToString(), (x0 + x1) * .5f, 96f, 40f, (x1 - x0) - 8f, 0f, Krem, 0f, Krem))
                BraxChampionsPiksele.Srodek(px, w, h, n.ToString(), (x0 + x1) / 2, 86, 3, Krem);
        }
        return px;
    }
    static void RuletkaCzesci()
    {
        RuletkaMaterialy();
        const int N = 37; float al = 2f * Mathf.PI / N;
        Vector3 P(float r, float a, float y) => new Vector3(r * Mathf.Cos(a), y, r * Mathf.Sin(a));
        // ---- the rotor (turns): number ring, pocket floors, frets, the pockets' outer wall, the cone, the turret, a dark
        // disc under it all
        var b = new BraxChampionsMeshBatch();
        for (int j = 0; j < N; j++)
            for (int k = 0; k < 2; k++)
            {
                float a0 = (j - .5f + k * .5f) * al, a1 = a0 + .5f * al, u0 = (j + k * .5f) / N, u1 = u0 + .5f / N;
                b.QuadUV(pierscien, P(.250f, a0, .017f), P(.250f, a1, .017f), P(.290f, a1, .022f), P(.290f, a0, .022f),
                         new Vector2(u0, .98f), new Vector2(u1, .98f), new Vector2(u1, .52f), new Vector2(u0, .52f));
                b.QuadUV(pierscien, P(.190f, a0, KieszenY), P(.190f, a1, KieszenY), P(.2475f, a1, KieszenY), P(.2475f, a0, KieszenY),
                         new Vector2(u0, .05f), new Vector2(u1, .05f), new Vector2(u1, .45f), new Vector2(u0, .45f));
            }
        for (int j = 0; j < N; j++)
        {
            float a = (j + .5f) * al;
            b.Box(chrom, P(.219f, a, .0125f), new Vector3(.057f, .013f, .0028f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
        }
        b.Lathe(chrom, Vector3.zero, new[] { new Vector2(.2478f, .017f), new Vector2(.2478f, KieszenY) }, 74, Quaternion.identity, false, false);
        b.Lathe(chrom, Vector3.zero, new[] { new Vector2(.19f, KieszenY), new Vector2(.19f, .019f) }, 64, Quaternion.identity, false, false);
        b.Lathe(orzech, Vector3.zero, new[] { new Vector2(.19f, .019f), new Vector2(.13f, .040f), new Vector2(.06f, .066f), new Vector2(.02f, .071f), new Vector2(0f, .071f) }, 48);
        b.Cylinder(chrom, new Vector3(0f, .071f, 0f), .016f, .065f, 14, Quaternion.identity, true);
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * .5f; var e = P(.075f, a, .124f);
            b.Tube(chrom, new Vector3(0f, .118f, 0f), e, .006f, 8);
            b.Lathe(chrom, e + new Vector3(0f, -.011f, 0f), Profil(.011f, 6), 10);
        }
        b.Cylinder(plastik, new Vector3(0f, -.014f, 0f), .29f, .002f, 37, Quaternion.identity, true);
        czRotor = Czesc(b, "ruletka rotor");
        b.Lathe(kulkaMat, new Vector3(0f, -KulkaR, 0f), Profil(KulkaR, 8), 12);
        czKulka = Czesc(b, "ruletka kulka");
        // the stake of 10: two of the owner's 5 chips (16.11 20:46) stacked
        Zeton(b, 0, Vector3.zero); Zeton(b, 0, new Vector3(0f, ZetonH, 0f));
        czZeton = Czesc(b, "ruletka żetony 2 x 5");
        // ---- the table and the bowl (static, placed like the cabinets)
        var t = new BraxChampionsMeshBatch();
        t.RoundedBoxMinMax(orzech, -1.15f, 1.15f, 0f, .70f, -.42f, .42f, .03f, 2);
        t.RoundedBoxMinMax(orzech, -1.35f, 1.35f, .70f, .76f, -.62f, .62f, .025f, 2);
        t.BoxMinMax(sukno, -1.29f, .41f, .76f, UkladY, -.50f, .52f);
        t.RoundedBoxMinMax(skora, -1.33f, .43f, .76f, .815f, .52f, .62f, .025f, 2);
        t.RoundedBoxMinMax(skora, -1.35f, -1.29f, .76f, .815f, -.50f, .52f, .02f, 2);
        t.BoxMinMax(orzech, -1.29f, .41f, .76f, .785f, -.62f, -.50f);
        // the layout printed on the felt: u to the players' right, v away from them
        float y = UkladY + Przed, xW = UkladXW, xF = UkladXW - UkladL, zN = UkladZN, zF = UkladZN - UkladD;
        t.QuadUV(uklad, new Vector3(xF, y, zN), new Vector3(xW, y, zN), new Vector3(xW, y, zF), new Vector3(xF, y, zF),
                 new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1));
        // the bowl: outside wall, rim, the ball track, the cone down to the rotor; the deflectors
        t.Lathe(orzech, Kolo, new[] { new Vector2(.42f, -.02f), new Vector2(.42f, .068f), new Vector2(.405f, .075f), new Vector2(.393f, .068f),
            new Vector2(.386f, .056f), new Vector2(.37f, TorY), new Vector2(.30f, .025f), new Vector2(.293f, .022f) }, 64, Quaternion.identity, false, false);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + .5f) * Mathf.PI / 4f;
            t.Box(chrom, Kolo + P(.335f, a, .040f), new Vector3(.010f, .008f, .026f), Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f) * Quaternion.Euler(0f, 0f, 20f));
        }
        // the winning-number display behind the wheel (the dealer's side): a brushed column, a black glass slab with a polished
        // gold frame, the screen's fixed picture (the live layer is the game's)
        float bot = WysC.y - .335f;
        t.Cylinder(stal, new Vector3(WysC.x, .76f, WysC.z), .045f, .012f, 20, Quaternion.identity, true);
        t.Cylinder(stal, new Vector3(WysC.x, .772f, WysC.z), .022f, bot - .772f, 14, Quaternion.identity, false);
        t.RoundedBox(czern, WysC, new Vector3(.38f, .67f, .045f), .008f, Quaternion.identity, 1);
        float fz = WysC.z + .0225f;
        t.BoxMinMax(zloto, WysC.x - .19f, WysC.x + .19f, WysC.y + .325f, WysC.y + .335f, fz, fz + .012f);
        t.BoxMinMax(zloto, WysC.x - .19f, WysC.x + .19f, WysC.y - .335f, WysC.y - .325f, fz, fz + .012f);
        t.BoxMinMax(zloto, WysC.x + .18f, WysC.x + .19f, WysC.y - .325f, WysC.y + .325f, fz, fz + .012f);
        t.BoxMinMax(zloto, WysC.x - .19f, WysC.x - .18f, WysC.y - .325f, WysC.y + .325f, fz, fz + .012f);
        Obraz(t, wysTlo, new Vector3(WysC.x, WysC.y, fz + Przed), BraxChampionsRuletka.Wyswietlacz.W, BraxChampionsRuletka.Wyswietlacz.H, Quaternion.identity);
        szablonRuletki = t;
    }

    // ---------------- the cabinets (templates, machine-local: floor at y 0, front +z, the player's left +x)
    // a picture on a face: centre c, w x h, facing q * +z, the picture's u to the reader's right; uv sub-rectangle
    static void Obraz(BraxChampionsMeshBatch b, Material m, Vector3 c, float w, float h, Quaternion q, float u0 = 0, float v0 = 0, float u1 = 1, float v1 = 1)
    {
        Vector3 P(float x, float y) => c + q * new Vector3(x, y, 0f);
        // seen from the front the reader's right is -x: the picture's left edge (u0) at +x
        b.QuadUV(m, P(-w * .5f, -h * .5f), P(w * .5f, -h * .5f), P(w * .5f, h * .5f), P(-w * .5f, h * .5f),
                 new Vector2(u1, v0), new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1));
    }
    static void Stolek(BraxChampionsMeshBatch b)
    {
        // a casino slot stool facing the machine (-z), its back away from it (+z)
        var c = new Vector3(0f, 0f, Taboret);
        b.Cylinder(chrom, c, .23f, .025f, 18, Quaternion.identity, true, false, .21f);
        b.Cylinder(chrom, c + new Vector3(0f, .025f, 0f), .032f, .56f, 10, Quaternion.identity, false);
        const int N = 12;
        for (int i = 0; i < N; i++)
        {
            float a0 = Mathf.PI * 2 * i / N, a1 = Mathf.PI * 2 * (i + 1) / N;
            b.Tube(chrom, c + new Vector3(.17f * Mathf.Cos(a0), .26f, .17f * Mathf.Sin(a0)), c + new Vector3(.17f * Mathf.Cos(a1), .26f, .17f * Mathf.Sin(a1)), .009f, 6);
            if (i % 4 == 0) b.Tube(chrom, c + new Vector3(.035f * Mathf.Cos(a0), .26f, .035f * Mathf.Sin(a0)), c + new Vector3(.165f * Mathf.Cos(a0), .26f, .165f * Mathf.Sin(a0)), .007f, 6);
        }
        b.Cylinder(plastik, c + new Vector3(0f, .585f, 0f), .15f, .02f, 14, Quaternion.identity, false);
        b.Lathe(skora, c + new Vector3(0f, .605f, 0f), new[] { new Vector2(0f, 0f), new Vector2(.16f, 0f), new Vector2(.195f, .02f), new Vector2(.205f, .05f),
            new Vector2(.195f, .08f), new Vector2(.16f, .095f), new Vector2(0f, .10f) }, 16);
        b.Tube(chrom, c + new Vector3(0f, .64f, .12f), c + new Vector3(0f, .80f, .175f), .012f, 8);
        b.RoundedBox(skora, c + new Vector3(0f, .87f, .19f), new Vector3(.34f, .16f, .055f), .022f, Quaternion.Euler(8f, 0f, 0f), 1);
    }
    static BraxChampionsMeshBatch Mechaniczny(int t, bool kolo)
    {
        var b = new BraxChampionsMeshBatch(); var lak = lakier[t];
        // base (set back 2 cm) and its brushed kick plate
        b.RoundedBoxMinMax(lak, -.32f, .32f, 0f, .60f, -.27f, .22f, .012f, 2);
        b.BoxMinMax(stal, -.31f, .31f, .01f, .09f, .22f, .23f);
        // the cabinet: side panels, back, top; the reel compartment (black) behind the window
        b.RoundedBoxMinMax(lak, .31f, .33f, .60f, 1.42f, -.28f, .24f, .008f, 1);
        b.RoundedBoxMinMax(lak, -.33f, -.31f, .60f, 1.42f, -.28f, .24f, .008f, 1);
        b.BoxMinMax(lak, -.31f, .31f, .60f, 1.42f, -.28f, -.26f);
        b.BoxMinMax(lak, -.31f, .31f, 1.40f, 1.42f, -.26f, .24f);
        b.BoxMinMax(plastik, -.31f, .31f, .985f, 1.0f, -.26f, .20f);
        b.BoxMinMax(plastik, -.31f, .31f, 1.0f, 1.40f, -.19f, -.17f);
        // the front: below the window (the pay glass, the meter on it), round the window (the opening to the drums)
        b.BoxMinMax(lak, -.31f, .31f, .60f, 1.08f, .20f, .24f);
        b.BoxMinMax(lak, .20f, .31f, 1.08f, 1.30f, .20f, .24f); b.BoxMinMax(lak, -.31f, -.20f, 1.08f, 1.30f, .20f, .24f);
        b.BoxMinMax(lak, -.31f, .31f, 1.30f, 1.40f, .20f, .24f);
        // the reel window: chrome bezel, the glass, the red pay line printed on its back with a marker each side
        b.BoxMinMax(chrom, -.218f, .218f, 1.30f, 1.318f, .24f, .255f); b.BoxMinMax(chrom, -.218f, .218f, 1.062f, 1.08f, .24f, .255f);
        b.BoxMinMax(chrom, .20f, .218f, 1.08f, 1.30f, .24f, .255f); b.BoxMinMax(chrom, -.218f, -.20f, 1.08f, 1.30f, .24f, .255f);
        b.Quad(szklo, new Vector3(-.20f, 1.08f, .247f), new Vector3(.20f, 1.08f, .247f), new Vector3(.20f, 1.30f, .247f), new Vector3(-.20f, 1.30f, .247f));
        b.Quad(linia, new Vector3(-.20f, 1.188f, .238f), new Vector3(.20f, 1.188f, .238f), new Vector3(.20f, 1.192f, .238f), new Vector3(-.20f, 1.192f, .238f));
        b.Triangle(linia, new Vector3(.200f, 1.19f, .262f), new Vector3(.214f, 1.18f, .262f), new Vector3(.214f, 1.20f, .262f));
        b.Triangle(linia, new Vector3(-.200f, 1.19f, .262f), new Vector3(-.214f, 1.20f, .262f), new Vector3(-.214f, 1.18f, .262f));
        // the meter (its picture is the game's) in a black bezel; the pay glass in a chrome frame
        b.BoxMinMax(czern, -.172f, .172f, 1.055f, 1.061f, .24f, .256f); b.BoxMinMax(czern, -.172f, .172f, 1.007f, 1.015f, .24f, .256f);
        b.BoxMinMax(czern, .16f, .172f, 1.015f, 1.055f, .24f, .256f); b.BoxMinMax(czern, -.172f, -.16f, 1.015f, 1.055f, .24f, .256f);
        Obraz(b, kolo ? tabelaKolo[t] : tabelaMech[t], new Vector3(0f, .81f, .247f), .54f, .22f, Quaternion.identity);
        b.BoxMinMax(chrom, -.282f, .282f, .92f, .932f, .24f, .252f); b.BoxMinMax(chrom, -.282f, .282f, .688f, .70f, .24f, .252f);
        b.BoxMinMax(chrom, .27f, .282f, .70f, .92f, .24f, .252f); b.BoxMinMax(chrom, -.282f, -.27f, .70f, .92f, .24f, .252f);
        // the coin tray (chrome, open on top)
        b.BoxMinMax(chrom, -.20f, .20f, .615f, .625f, .24f, .34f); b.BoxMinMax(chrom, -.20f, .20f, .625f, .675f, .33f, .34f);
        b.BoxMinMax(chrom, .19f, .20f, .625f, .675f, .24f, .33f); b.BoxMinMax(chrom, -.20f, -.19f, .625f, .675f, .24f, .33f);
        // the button deck with its chrome bumper: CASH OUT, BET ONE, MAX BET, the coin slot (SPIN is the game's)
        var d = DeckMech;
        b.RoundedBox(lak, d.c, d.size, .012f, d.q, 2);
        b.Tube(chrom, d.c + d.q * new Vector3(.32f, 0f, .076f), d.c + d.q * new Vector3(-.32f, 0f, .076f), .009f, 8);
        Przyciski(b, d, new[] { (.24f, pZielony), (.155f, pBialy), (.07f, pZolty) });
        b.Box(chrom, d.Top(-.07f, .01f) + d.q * new Vector3(0f, .004f, 0f), new Vector3(.07f, .008f, .05f), d.q);
        b.Box(czern, d.Top(-.07f, .01f) + d.q * new Vector3(0f, .012f, 0f), new Vector3(.006f, .008f, .032f), d.q);
        // the lever's hub plate on the right side (the lever itself moves)
        b.Cylinder(chrom, new Vector3(-.33f, 1.10f, .05f), .065f, .022f, 16, Quaternion.Euler(0f, 0f, 90f), true);
        b.Cylinder(chrom, new Vector3(-.352f, 1.10f, .05f), .03f, .012f, 12, Quaternion.Euler(0f, 0f, 90f), true);
        if (kolo) { KoloNadstawka(b, t); Stolek(b); return b; }
        // the top box: the printed glass lit from behind (BRAXHORN) in a chrome frame; the candle
        b.RoundedBoxMinMax(lak, -.33f, .33f, 1.42f, 1.80f, -.26f, .22f, .03f, 2);
        Obraz(b, szyld[t], new Vector3(0f, 1.61f, .227f), .58f, .30f, Quaternion.identity);
        b.BoxMinMax(chrom, -.30f, .30f, 1.76f, 1.772f, .22f, .232f); b.BoxMinMax(chrom, -.30f, .30f, 1.448f, 1.46f, .22f, .232f);
        b.BoxMinMax(chrom, .29f, .30f, 1.46f, 1.76f, .22f, .232f); b.BoxMinMax(chrom, -.30f, -.29f, 1.46f, 1.76f, .22f, .232f);
        b.Cylinder(chrom, new Vector3(0f, 1.80f, -.02f), .05f, .025f, 14, Quaternion.identity, true, false, .045f);
        b.Cylinder(swiecaCzerwona, new Vector3(0f, 1.825f, -.02f), .04f, .065f, 14, Quaternion.identity, false);
        b.Cylinder(swiecaBiala, new Vector3(0f, 1.89f, -.02f), .04f, .065f, 14, Quaternion.identity, false);
        b.Cylinder(chrom, new Vector3(0f, 1.955f, -.02f), .043f, .012f, 14, Quaternion.identity, true, false, .03f);
        Stolek(b);
        return b;
    }
    static void Przyciski(BraxChampionsMeshBatch b, Deck d, (float x, Material m)[] list)
    {
        foreach (var (x, m) in list)
        {
            b.Box(chrom, d.Top(x, 0f) + d.q * new Vector3(0f, .004f, 0f), new Vector3(.062f, .008f, .042f), d.q);
            b.RoundedBox(m, d.Top(x, 0f) + d.q * new Vector3(0f, .012f, 0f), new Vector3(.05f, .010f, .03f), .003f, d.q, 1);
        }
    }
    // a cheek's profile (z, y): the screen machine's side panel follows the screens' faces
    static readonly Vector2[] Policzek = { new Vector2(-.30f, .62f), new Vector2(.31f, .62f), new Vector2(.31f, .99f), new Vector2(.215f, 1.375f), new Vector2(.215f, 1.80f), new Vector2(-.30f, 1.80f) };
    static void Ekstruzja(BraxChampionsMeshBatch b, Material m, Vector2[] zy, float x0, float x1)
    {
        Vector3 P(float x, Vector2 p) => new Vector3(x, p.y, p.x);
        int n = zy.Length;
        // the outer faces (+x at x1, -x at x0) as fans (the profile is convex), the edges between them
        for (int i = 1; i + 1 < n; i++)
        {
            b.Triangle(m, P(x1, zy[0]), P(x1, zy[i + 1]), P(x1, zy[i])); b.Triangle(m, P(x0, zy[0]), P(x0, zy[i]), P(x0, zy[i + 1]));
        }
        for (int i = 0; i < n; i++)
        {
            var a = zy[i]; var c = zy[(i + 1) % n];
            b.Quad(m, P(x0, a), P(x1, a), P(x1, c), P(x0, c));
        }
    }
    static BraxChampionsMeshBatch Ekranowy(int t, int gra)
    {
        var b = new BraxChampionsMeshBatch(); var lak = lakier[t]; var led = swiatlo[t];
        // foot plate, the tapered base with LED edges
        b.BoxMinMax(stal, -.36f, .36f, 0f, .015f, -.31f, .34f);
        Frustum(b, lak, -.29f, .29f, -.27f, .24f, .015f, -.33f, .33f, -.30f, .28f, .62f);
        foreach (float s in new[] { -1f, 1f })
            b.Tube(led, new Vector3(s * .296f, .02f, .246f), new Vector3(s * .336f, .62f, .286f), .006f, 8);
        // the body behind the screens, the cheeks (with LED edges along the screens), the lower front with the speakers and the
        // bill acceptor
        b.BoxMinMax(lak, -.33f, .33f, .62f, 1.80f, -.30f, -.28f);
        b.BoxMinMax(lak, -.33f, .33f, 1.78f, 1.80f, -.28f, .155f);
        b.BoxMinMax(lak, -.33f, .33f, .62f, .93f, -.28f, .28f);
        foreach (float s in new[] { -1f, 1f })
        {
            Ekstruzja(b, lak, Policzek, s < 0 ? -.35f : .33f, s < 0 ? -.33f : .35f);
            var e = new[] { new Vector3(s * .34f, .62f, .318f), new Vector3(s * .34f, .99f, .318f), new Vector3(s * .34f, 1.375f, .223f), new Vector3(s * .34f, 1.80f, .223f) };
            for (int i = 0; i + 1 < e.Length; i++) b.Tube(led, e[i], e[i + 1], .006f, 8);
            b.Cylinder(chrom, new Vector3(s * .20f, .74f, .28f), .064f, .008f, 16, Quaternion.Euler(90f, 0f, 0f), true);
            b.Cylinder(glosnik, new Vector3(s * .20f, .74f, .288f), .056f, .008f, 16, Quaternion.Euler(90f, 0f, 0f), true);
        }
        b.RoundedBoxMinMax(chrom, -.055f, .055f, .80f, .87f, .28f, .30f, .004f, 1);
        b.BoxMinMax(czern, -.038f, .038f, .832f, .838f, .30f, .307f);
        b.BoxMinMax(pZielony, -.008f, .008f, .848f, .856f, .30f, .307f);
        // the button deck: five lit buttons and the chrome bumper (SPIN is the game's)
        var d = DeckEkran;
        b.RoundedBox(lak, d.c, d.size, .015f, d.q, 2);
        b.Tube(chrom, d.c + d.q * new Vector3(.34f, -.005f, .122f), d.c + d.q * new Vector3(-.34f, -.005f, .122f), .013f, 8);
        Przyciski(b, d, new[] { (.27f, pBialy), (.19f, pBialy), (.11f, pBialy), (.03f, pZolty), (-.06f, pCzerwony) });
        // the main screen: a black glass slab tilted back, the picture round the reels (the reels and the meter are the game's)
        var qs = Quaternion.Euler(EkranTilt, 0f, 0f); var s0 = EkranS0;
        b.RoundedBox(czern, s0 + qs * new Vector3(0f, 0f, -.025f), new Vector3(.66f, .40f, .05f), .01f, qs, 1);
        float k = 1f / EkranW, hw = 2.5f * Komorka + 2f * .008f, hh = 1.5f * Komorka, cy = KolumnyY;
        // four strips of the picture round the reel window (the window itself is the black glass)
        float L = -EkranW * .5f, R = EkranW * .5f, B = -EkranH * .5f, T = EkranH * .5f;
        void Pas(float x0, float x1, float y0, float y1) => Obraz(b, tloEkran, s0 + qs * new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, Przed), x1 - x0, y1 - y0, qs,
            .5f - x1 * k, .5f + y0 / EkranH, .5f - x0 * k, .5f + y1 / EkranH);
        // the bottom strip leaves the meter's place open (the meter is the game's, as far in front of the glass)
        if (gra == G.Video)
        {
            Pas(L, R, B, LicznikY - .02f); Pas(L, R, LicznikY + .02f, cy - hh); Pas(.16f, R, LicznikY - .02f, LicznikY + .02f); Pas(L, -.16f, LicznikY - .02f, LicznikY + .02f);
            Pas(L, R, cy + hh, T); Pas(hw, R, cy - hh, cy + hh); Pas(L, -hw, cy - hh, cy + hh);
        }
        else
        {
            // poker / keno: the game's whole picture (its live layer in front of it), the meter's place open
            var tlo = gra == G.Poker ? tloPoker : tloKeno;
            void PasG(float x0, float x1, float y0, float y1) => Obraz(b, tlo, s0 + qs * new Vector3((x0 + x1) * .5f, (y0 + y1) * .5f, Przed), x1 - x0, y1 - y0, qs,
                .5f - x1 * k, .5f + y0 / EkranH, .5f - x0 * k, .5f + y1 / EkranH);
            PasG(L, R, B, LicznikY - .02f); PasG(L, R, LicznikY + .02f, T); PasG(.16f, R, LicznikY - .02f, LicznikY + .02f); PasG(L, -.16f, LicznikY - .02f, LicznikY + .02f);
        }
        // the upper screen (the pay table) in its slab; the filler between the two screens
        b.BoxMinMax(czern, -.33f, .33f, 1.40f, 1.77f, .155f, .205f);
        Obraz(b, gra == G.Poker ? tabelaPoker : gra == G.Keno ? tabelaKeno : tabelaEkran[t], new Vector3(0f, 1.585f, .205f + Przed), EkranW, EkranH, Quaternion.identity);
        b.BoxMinMax(czern, -.33f, .33f, 1.37f, 1.40f, .15f, .20f);
        // the topper: the jackpot screen (the game's) in a box with a curved top, LED along the curve each side
        b.BoxMinMax(lak, -.33f, .33f, 1.80f, 1.98f, -.24f, .20f);
        const int Luk = 10;
        b.Surface(lak, (u, v) => new Vector3(.33f - .66f * u, 1.98f + .10f * Mathf.Sin(Mathf.PI * v), -.02f + .22f * Mathf.Cos(Mathf.PI * v)), 1, Luk, true);
        foreach (float s in new[] { -1f, 1f })
            for (int i = 0; i < Luk; i++)
            {
                float a0 = Mathf.PI * i / Luk, a1 = Mathf.PI * (i + 1) / Luk;
                var p0 = new Vector3(s * .33f, 1.98f + .10f * Mathf.Sin(a0), -.02f + .22f * Mathf.Cos(a0)); var p1 = new Vector3(s * .33f, 1.98f + .10f * Mathf.Sin(a1), -.02f + .22f * Mathf.Cos(a1));
                var mid = new Vector3(s * .33f, 1.98f, -.02f);
                if (s > 0) b.Triangle(lak, mid, p1, p0); else b.Triangle(lak, mid, p0, p1);
                b.Tube(led, p0 + new Vector3(s * .006f, 0f, 0f), p1 + new Vector3(s * .006f, 0f, 0f), .005f, 6);
            }
        b.BoxMinMax(chrom, -.252f, .252f, 1.97f, 1.978f, .20f, .214f); b.BoxMinMax(chrom, -.252f, .252f, 1.802f, 1.81f, .20f, .214f);
        // poker / keno: the owner's topper of the game in the jackpot screen's place (the video machine's is the game's)
        if (gra != G.Video) Obraz(b, toppery[gra], new Vector3(0f, 1.89f, .20f + Przed), .48f, .16f, Quaternion.identity);
        Stolek(b);
        return b;
    }
    // a lacquered base narrower at the floor: the bottom rectangle (x0..x1, z0..z1 at y0) to the top one (at y1)
    static void Frustum(BraxChampionsMeshBatch b, Material m, float bx0, float bx1, float bz0, float bz1, float y0, float tx0, float tx1, float tz0, float tz1, float y1)
    {
        Vector3 B(float x, float z) => new Vector3(x, y0, z); Vector3 T(float x, float z) => new Vector3(x, y1, z);
        b.Quad(m, B(bx0, bz1), B(bx1, bz1), T(tx1, tz1), T(tx0, tz1));     // front (+z)
        b.Quad(m, B(bx1, bz0), B(bx0, bz0), T(tx0, tz0), T(tx1, tz0));     // back
        b.Quad(m, B(bx1, bz1), B(bx1, bz0), T(tx1, tz0), T(tx1, tz1));     // +x
        b.Quad(m, B(bx0, bz0), B(bx0, bz1), T(tx0, tz1), T(tx0, tz0));     // -x
    }
}
