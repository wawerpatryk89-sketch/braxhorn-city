using System;
using System.Collections.Generic;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - KASYNO WAWER, the owner's hidden office (16.11 23:41 "W tym kasynie Wawer ... prywatne pomieszczenie, do
// którego ... wejście będzie nikomu nieznane ... przez szafę"; 23:44 "dyskretne pomieszczenie, biuro chcę mieć dla siebie";
// 17.11 inside: "Monitoring sali na żywo, Sejf ze złotem"). The furniture of the office block the hall builds in its rear
// east corner (BraxChampionsCasinoT11.BiuroWlasciciela):
//  - on the block's hall wall three built-in walnut wardrobes; the middle one is the door, a bookcase door on its hinge
//    (BraxChampionsSzafa): AKCJA "SZAFA" swings it into the office - from the hall nobody sees a door;
//  - the monitoring wall: six screens with the live pictures of six cameras in the hall (BraxChampionsMonitoring - real
//    game cameras, never a film), the cameras in black housings on the hall's columns and front wall;
//  - the safe, open: the owner's gold - London Good Delivery bars (LBMA: 250 x 70 x 35 mm, about 12.4 kg) on two shelves, stacks of his
//    BRAXHORN CASINO coins (his coin art 1:1 on their faces) on the third;
//  - his desk (walnut, black leather top), his leather chair, his BRAXHORN CASINO logo (his art 1:1) framed behind it, a
//    velvet rug, a credenza under the screens, the lights.
public static partial class BraxChampionsKasynoSprzet
{
    public struct KameraHali { public Vector3 poz, cel; public string nazwa; }
    public static int SztabkiWSejfie;

    /// <summary>The office's inside x0..x1, z0..z1 (nominal metres, the hall's frame), its floor y; the north wall (to the
    /// hall, z0 - grubosc .. z0) has the wardrobe door's opening drzwiX0..drzwiX1; kamery: the hall's cameras.</summary>
    public static void BiuroWlasciciela(Transform root, float x0, float x1, float z0, float z1, float y, float grubosc,
        float drzwiX0, float drzwiX1, KameraHali[] kamery)
    {
        Przygotuj();
        var b = new BraxChampionsMeshBatch();
        var g = new GameObject("KASYNO WAWER | ukryte biuro Patryka (szafa, monitoring, sejf)").transform; g.SetParent(root, false);
        var zlotoSztabek = Metal("zloto_sztabki", new Color(1.0f, .77f, .34f), .24f);
        var monitorEkran = Ekran("monitoring_ekran", Png("kasyno_monitoring_start", 64, 36, Czern64x36(), false), 1.0f);
        float zw = z0 - grubosc;                                       // the wall's face to the hall

        // ---- the three wardrobes on the hall side of the north wall; the middle one swings into the office on its hinge
        float sW = drzwiX1 - drzwiX0, sD = .55f, sH = 2.45f;
        foreach (float sx in new[] { drzwiX0 - .05f - sW * .5f, drzwiX1 + .05f + sW * .5f })
        {
            Szafa(b, new Vector3(sx, y, zw - sD * .5f), sW, sD, sH);
            W.Kolizja(new Vector3(sx, y + sH * .5f, zw - sD * .5f), new Vector3(sW, sH, sD), Quaternion.identity);
        }
        var zawias = new GameObject("szafa | zawias (tajne przejście)").transform; zawias.SetParent(g, false);
        zawias.localPosition = new Vector3(drzwiX0, y, zw);
        // the door wardrobe in the hinge's frame (the hinge at its back west corner), built under it
        var sb = new BraxChampionsMeshBatch();
        Szafa(sb, new Vector3(sW * .5f, 0f, -sD * .5f), sW, sD, sH);
        sb.Box(mosiadz, new Vector3(sW - .14f, 1.05f, .012f), new Vector3(.03f, .30f, .02f), Quaternion.identity);   // its back: a brass pull
        var szafa = sb.Build(zawias, "szafa | drzwi do biura (BraxChampionsSzafa)", Out + "/Meshes", false, 1f);
        var col = szafa.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(sW * .5f, sH * .5f, -sD * .5f); col.size = new Vector3(sW, sH, sD);
        var drzwi = g.gameObject.AddComponent<BraxChampionsSzafa>();
        drzwi.zawias = zawias; drzwi.katOtwarcia = -90f;               // its far side swings into the office
        drzwi.punkty = new[] { Punkt(g, "szafa | AKCJA w sali", new Vector3(drzwiX0 + sW * .5f, y, zw - sD - .55f)),
                               Punkt(g, "szafa | AKCJA w biurze", new Vector3(drzwiX0 + sW * .5f + .35f, y, z0 + .75f)) };

        // ---- the floor's rug, the desk, his chair, his logo
        b.Box(aksamit, new Vector3((x0 + x1) * .5f, y + .006f, (z0 + z1) * .5f + .6f), new Vector3(x1 - x0 - 2.4f, .012f, z1 - z0 - 3.2f), Quaternion.identity);
        float dx = (x0 + x1) * .5f + .3f, dz = z1 - 2.2f;
        b.RoundedBox(ciemnyOrzech, new Vector3(dx, y + .37f, dz), new Vector3(2.2f, .74f, .95f), .015f, Quaternion.identity, 2);
        b.Box(skora, new Vector3(dx, y + .748f, dz), new Vector3(1.9f, .012f, .7f), Quaternion.identity);
        b.Box(mosiadz, new Vector3(dx, y + .745f, dz - .476f), new Vector3(2.2f, .02f, .006f), Quaternion.identity);
        W.Kolizja(new Vector3(dx, y + .38f, dz), new Vector3(2.2f, .76f, .95f), Quaternion.identity);
        Fotel(b, new Vector3(dx, y, dz + .85f), 180f);
        W.Lampa("biuro Patryka | lampa na biurku", new Vector3(dx - .8f, y + 1.15f, dz + .1f), .5f, 3f, new Color(1f, .86f, .62f));
        b.Cylinder(mosiadz, new Vector3(dx - .8f, y + .754f, dz + .1f), .08f, .02f, 16, Quaternion.identity, true);
        b.Tube(mosiadz, new Vector3(dx - .8f, y + .77f, dz + .1f), new Vector3(dx - .8f, y + 1.10f, dz + .1f), .012f, 8);
        b.Cylinder(czern, new Vector3(dx - .8f, y + 1.08f, dz + .1f), .14f, .12f, 18, Quaternion.identity, true, true, .09f);
        for (int i = 0; i < 5; i++) Moneta(b, new Vector3(dx + .45f + i * .05f, y + .754f, dz - .05f + (i % 2) * .03f), 1 + i % 2);
        float lw = 1.30f, lh = lw / ArkProporcja(ArkLogo); var lz = new Vector3(dx, y + 2.15f, z1 - .03f); var lq = Quaternion.Euler(0f, 180f, 0f);
        b.Box(mosiadz, lz + new Vector3(0f, 0f, .012f), new Vector3(lw + .12f, lh + .12f, .03f), Quaternion.identity);
        Obraz(b, szyldyArk[ArkLogo], lz - new Vector3(0f, 0f, .005f + Przed), lw, lh, lq);

        // ---- the monitoring wall on the west wall: six screens (3 x 2) over a walnut credenza, each its hall camera's
        // live picture
        float mx = x0 + .02f, mz = (z0 + z1) * .5f - .4f; var mq = Quaternion.Euler(0f, 90f, 0f);
        b.RoundedBox(ciemnyOrzech, new Vector3(x0 + .24f, y + .38f, mz), new Vector3(.48f, .76f, 3.6f), .015f, Quaternion.identity, 2);
        b.Box(marmur, new Vector3(x0 + .25f, y + .77f, mz), new Vector3(.52f, .025f, 3.66f), Quaternion.identity);
        W.Kolizja(new Vector3(x0 + .25f, y + .39f, mz), new Vector3(.52f, .78f, 3.66f), Quaternion.identity);
        b.Box(czern, new Vector3(mx + .03f, y + 1.68f, mz), new Vector3(.05f, 1.32f, 3.32f), Quaternion.identity);   // the screens' black wall
        var ekrany = new List<Renderer>(); var kam = new List<Camera>();
        for (int r = 0; r < 2; r++) for (int c = 0; c < 3; c++)
        {
            int i = r * 3 + c; if (kamery == null || i >= kamery.Length) break;
            var e = new GameObject("monitoring | ekran " + (i + 1) + " · " + kamery[i].nazwa); e.transform.SetParent(g, false);
            e.transform.localPosition = new Vector3(mx + .07f, y + 1.36f + r * .62f, mz + (c - 1) * 1.06f);   // 15 mm before the wall's face
            e.transform.localRotation = mq; e.transform.localScale = new Vector3(1.0f, .5625f, 1f);
            e.AddComponent<MeshFilter>().sharedMesh = quad; var mr = Cienie(e.AddComponent<MeshRenderer>()); mr.sharedMaterial = monitorEkran; ekrany.Add(mr);
            // the camera in the hall: a black housing on a bracket, the lens to its target
            var k = kamery[i]; var kq = Quaternion.LookRotation((k.cel - k.poz).normalized, Vector3.up);
            b.RoundedBox(czern, k.poz + kq * new Vector3(0f, 0f, -.10f), new Vector3(.12f, .11f, .26f), .02f, kq, 2);
            b.Cylinder(szklo, k.poz + kq * new Vector3(0f, 0f, .03f), .03f, .01f, 12, kq * Quaternion.Euler(90f, 0f, 0f), true);
            b.Box(czern, k.poz + kq * new Vector3(0f, .10f, -.16f), new Vector3(.04f, .14f, .04f), kq);
            var co = new GameObject("monitoring | kamera " + (i + 1) + " · " + k.nazwa); co.transform.SetParent(g, false);
            co.transform.localPosition = k.poz + kq * new Vector3(0f, 0f, .05f); co.transform.localRotation = kq;
            var cam = co.AddComponent<Camera>(); cam.enabled = false; cam.fieldOfView = 72f; cam.nearClipPlane = .1f; cam.farClipPlane = 140f;
            kam.Add(cam);
        }
        var mon = g.gameObject.AddComponent<BraxChampionsMonitoring>();
        mon.kamery = kam.ToArray(); mon.ekrany = ekrany.ToArray();
        mon.srodek = Punkt(g, "biuro | środek", new Vector3((x0 + x1) * .5f, y + 1f, (z0 + z1) * .5f)); mon.zasieg = Mathf.Max(x1 - x0, z1 - z0) * .75f;

        // ---- the safe on the east wall, open to the desk: black lacquered steel with brass edges, a red velvet lining, the
        // door on its north hinge with the wheel and the keypad, three shelves: the 1 kg bars on the two lower ones, the
        // coin stacks on the top one
        var sq = Quaternion.Euler(0f, -90f, 0f); var s0 = new Vector3(x1 - .42f, y, z0 + 3.0f);
        Vector3 S(float x, float yy, float z) => s0 + sq * new Vector3(x, yy, z);
        const float SW = 1.0f, SH = 1.8f, SD = .8f, T = .08f;
        b.Box(czern, S(0f, SH * .5f, -SD * .5f + T * .5f), new Vector3(SW, SH, T), sq);                                  // back
        foreach (float sx in new[] { -1f, 1f }) b.Box(czern, S(sx * (SW - T) * .5f, SH * .5f, 0f), new Vector3(T, SH, SD), sq);
        b.Box(czern, S(0f, T * .5f, 0f), new Vector3(SW, T, SD), sq); b.Box(czern, S(0f, SH - T * .5f, 0f), new Vector3(SW, T, SD), sq);
        foreach (float sx in new[] { -1f, 1f }) b.Box(mosiadz, S(sx * (SW * .5f - .012f), SH * .5f, SD * .5f + .004f), new Vector3(.024f, SH, .008f), sq);
        b.Box(aksamit, S(0f, SH * .5f, -SD * .5f + T + .014f), new Vector3(SW - 2f * T, SH - 2f * T, .004f), sq);          // the velvet lining (14 mm off the steel)
        float[] polki = { .42f, .90f, 1.36f };
        foreach (float py in polki) b.Box(chrom, S(0f, py - .012f, .03f), new Vector3(SW - 2f * T, .024f, SD - T - .06f), sq);   // 8 mm off the lining
        // the door, open 105 degrees on its hinge at the safe's north front edge (its local -x)
        var dq = sq * Quaternion.Euler(0f, -105f, 0f); var hinge = S(-SW * .5f, 0f, SD * .5f);
        Vector3 D(float x, float yy, float z) => hinge + dq * new Vector3(x, yy, z);
        b.Box(czern, D(SW * .5f, SH * .5f, .07f), new Vector3(SW, SH, .14f), dq);
        b.Box(mosiadz, D(SW * .5f, SH * .5f, .150f), new Vector3(SW - .08f, SH - .08f, .004f), dq);                      // a brass panel 1 cm proud
        b.Box(czern, D(SW * .5f, SH * .5f, .159f), new Vector3(SW - .14f, SH - .14f, .004f), dq);                       // its black field
        b.Cylinder(chrom, D(SW * .5f, SH * .55f, .161f), .10f, .03f, 24, dq * Quaternion.Euler(90f, 0f, 0f), true);
        for (int k = 0; k < 3; k++) { var a = Quaternion.AngleAxis(k * 60f, Vector3.forward); b.Box(chrom, D(SW * .5f, SH * .55f, .20f), new Vector3(.30f, .02f, .02f), dq * a); }   // the wheel's three spokes
        b.Box(chrom, D(SW * .5f, SH * .76f, .166f), new Vector3(.16f, .11f, .01f), dq);                                    // the keypad
        W.Kolizja(S(0f, SH * .5f, 0f), new Vector3(SW, SH, SD), sq); W.Kolizja(D(SW * .5f, SH * .5f, .07f), new Vector3(SW, SH, .14f), dq);
        // the bars: London Good Delivery bars (LBMA: 250 x 70 mm, 35 mm high, 5-25 degrees undercut; 350-430 oz, about 12.4 kg),
        // a loaf 250 x 70 at the bottom narrowing 15 degrees to the top; on each lower shelf three across, nine rows, and a
        // second layer of two by eight on them
        int bars = 0; const float BL = .250f, BW = .070f, BH = .035f; float cut = BH * Mathf.Tan(15f * Mathf.Deg2Rad);
        for (int p = 0; p < 2; p++)
        {
            float py = polki[p];
            for (int l = 0; l < 2; l++)
            {
                int nx = l == 0 ? 3 : 2, nz = l == 0 ? 9 : 8;
                for (int ix = 0; ix < nx; ix++) for (int iz = 0; iz < nz; iz++)
                {
                    float bx = (ix - (nx - 1) * .5f) * (BL + .005f), bz = .03f + (iz - (nz - 1) * .5f) * (BW + .002f);
                    Sztabka(b, zlotoSztabek, S(bx, py + l * BH, bz), sq, BL, BW, BH, cut); bars++;
                }
            }
        }
        SztabkiWSejfie = bars;
        for (int st = 0; st < 12; st++)
        {
            float cx = -.33f + (st % 4) * .22f, cz = -.20f + (st / 4) * .17f; int ile = 15 + (st * 7) % 16;          // 15-30 coins a stack
            for (int k = 0; k < ile; k++) Moneta(b, S(cx, polki[2] + k * .003f, cz), k == ile - 1 ? 1 + st % 2 : 0);
        }
        var sejf = Punkt(g, "sejf | AKCJA", S(0f, 0f, SD * .5f + .6f));
        var info = g.gameObject.AddComponent<BraxChampionsSzafa>(); info.zawias = null; info.punkty = new[] { sejf }; info.zasieg = 1.2f;
        info.napis = "SEJF"; info.komunikat = "SEJF PATRYKA · " + bars + " SZTABEK GOOD DELIVERY (OK. " + (bars * 12.4f / 1000f).ToString("0.0") + " T ZŁOTA) · MONETY BRAXHORN";

        // ---- the room's finish: a brass line round the walls under the ceiling, a warm LED cove in the ceiling's edge, brass
        // strips flanking his logo
        foreach (var (cx, cz, wx, wz) in new[] { ((x0 + x1) * .5f, z0 + .006f, x1 - x0, .012f), ((x0 + x1) * .5f, z1 - .006f, x1 - x0, .012f),
                                                  (x0 + .006f, (z0 + z1) * .5f, .012f, z1 - z0 - .024f), (x1 - .006f, (z0 + z1) * .5f, .012f, z1 - z0 - .024f) })
        {
            b.Box(mosiadz, new Vector3(cx, y + 3.80f, cz), new Vector3(wx, .035f, wz), Quaternion.identity);
            b.Box(kasetonLed, new Vector3(cx + (cx < (x0 + x1) * .5f - 1f ? .12f : cx > (x0 + x1) * .5f + 1f ? -.12f : 0f), 4.18f, cz + (cz < (z0 + z1) * .5f - 1f ? .12f : cz > (z0 + z1) * .5f + 1f ? -.12f : 0f)),
                  new Vector3(wx < .1f ? .05f : wx - .3f, .02f, wz < .1f ? .05f : wz - .3f), Quaternion.identity);
        }
        foreach (float sx in new[] { -1f, 1f }) b.Box(mosiadz, new Vector3(dx + sx * (lw * .5f + .35f), y + 2.0f, z1 - .008f), new Vector3(.03f, 3.2f, .016f), Quaternion.identity);

        // ---- the lights
        W.Lampa("biuro Patryka | światło", new Vector3((x0 + x1) * .5f, y + 3.7f, z0 + 2.2f), .9f, 7f, new Color(1f, .90f, .74f));
        W.Lampa("biuro Patryka | światło", new Vector3((x0 + x1) * .5f, y + 3.7f, z1 - 2.4f), .9f, 7f, new Color(1f, .90f, .74f));
        b.Build(g, "T11 hidden office | furniture, safe, gold, cameras", Out + "/Meshes", false, 1f);
        Debug.Log("KASYNO_WAWER_BIURO hidden office " + (x1 - x0).ToString("F1") + "x" + (z1 - z0).ToString("F1") + " m door=wardrobe (bookcase hinge) cameras=" + kam.Count +
            " screens=" + ekrany.Count + " safe=open gold_good_delivery_bars=" + bars + " coins=his art logo=" + (SzyldyWlasciciela ? "owner" : "drawn"));
    }

    // a gold bar: its base bw x bl at c (the bar's length along the frame's x), the sides undercut by cut to its top
    static void Sztabka(BraxChampionsMeshBatch b, Material m, Vector3 c, Quaternion q, float bl, float bw, float bh, float cut)
    {
        Vector3 V(float x, float y, float z) => c + q * new Vector3(x, y, z);
        float lx = bl * .5f, lz = bw * .5f, ux = lx - cut, uz = lz - cut;
        Vector3 b0 = V(-lx, 0, -lz), b1 = V(lx, 0, -lz), b2 = V(lx, 0, lz), b3 = V(-lx, 0, lz);
        Vector3 t0 = V(-ux, bh, -uz), t1 = V(ux, bh, -uz), t2 = V(ux, bh, uz), t3 = V(-ux, bh, uz);
        b.Quad(m, t0, t3, t2, t1);                                                       // the top (up)
        b.Quad(m, b1, b0, t0, t1); b.Quad(m, b2, b1, t1, t2); b.Quad(m, b3, b2, t2, t3); b.Quad(m, b0, b3, t3, t0);   // the four sloped sides (out)
    }

    static Transform Punkt(Transform g, string n, Vector3 p) { var t = new GameObject(n).transform; t.SetParent(g, false); t.localPosition = p; return t; }
    static Color32[] Czern64x36() { var p = new Color32[64 * 36]; BraxChampionsPiksele.Wypelnij(p, new Color32(6, 8, 10, 255)); return p; }

    // a built-in walnut wardrobe (its front to -z): plinth, body, two door leaves with their gap, brass pulls, the cornice
    static void Szafa(BraxChampionsMeshBatch b, Vector3 c, float w, float d, float h)
    {
        b.Box(czern, c + new Vector3(0f, .05f, .02f), new Vector3(w - .04f, .10f, d - .04f), Quaternion.identity);
        b.RoundedBox(ciemnyOrzech, c + new Vector3(0f, .10f + (h - .22f) * .5f, 0f), new Vector3(w, h - .22f, d), .01f, Quaternion.identity, 2);
        foreach (float s in new[] { -1f, 1f })
        {
            b.Box(ciemnyOrzech, c + new Vector3(s * w * .25f, .10f + (h - .22f) * .5f, -d * .5f - .008f), new Vector3(w * .5f - .03f, h - .30f, .016f), Quaternion.identity);
            b.Box(mosiadz, c + new Vector3(s * .06f, 1.15f, -d * .5f - .03f), new Vector3(.018f, .42f, .018f), Quaternion.identity);
        }
        b.RoundedBox(ciemnyOrzech, c + new Vector3(0f, h - .06f, -.01f), new Vector3(w + .06f, .12f, d + .04f), .02f, Quaternion.identity, 2);
        b.Box(mosiadz, c + new Vector3(0f, h - .125f, -d * .5f - .031f), new Vector3(w + .06f, .012f, .004f), Quaternion.identity);
    }

    // his chair: a high-backed black leather executive chair on a chrome five-star base (yaw: where he looks)
    static void Fotel(BraxChampionsMeshBatch b, Vector3 c, float yaw)
    {
        var q = Quaternion.Euler(0f, yaw, 0f); Vector3 P(float x, float yy, float z) => c + q * new Vector3(x, yy, z);
        for (int i = 0; i < 5; i++) { var a = Quaternion.Euler(0f, i * 72f, 0f); b.Tube(chrom, P(0f, .07f, 0f), c + q * (a * new Vector3(0f, .05f, .33f)), .018f, 6); b.Cylinder(czern, c + q * (a * new Vector3(0f, 0f, .33f)), .03f, .05f, 8, q, true); }
        b.Cylinder(chrom, P(0f, .07f, 0f), .03f, .38f, 10, q, false);
        b.RoundedBox(skora, P(0f, .50f, 0f), new Vector3(.56f, .12f, .54f), .05f, q, 3);
        b.RoundedBox(skora, P(0f, 1.0f, .25f), new Vector3(.54f, .86f, .12f), .05f, q, 3);
        b.RoundedBox(skora, P(0f, 1.52f, .26f), new Vector3(.36f, .18f, .10f), .04f, q, 3);
        foreach (float s in new[] { -1f, 1f }) { b.RoundedBox(skora, P(s * .30f, .70f, -.02f), new Vector3(.07f, .06f, .38f), .02f, q, 2); b.Tube(chrom, P(s * .30f, .56f, .05f), P(s * .30f, .67f, .05f), .012f, 6); }
        W.Kolizja(P(0f, .55f, .05f), new Vector3(.6f, 1.1f, .6f), q);
    }

    // a BRAXHORN coin (40 mm, 3 mm, milled gold edge) lying flat at c; twarz 1 = his front art up, 2 = his back art up, 0 = edge only
    static void Moneta(BraxChampionsMeshBatch b, Vector3 c, int twarz)
    {
        const float R = .02f, H = .003f; const int N = 28;
        b.Cylinder(zlotoKola ? zlotoKola : mosiadz, c, R, H, N, Quaternion.identity, twarz == 0);
        if (twarz == 0) return;
        var m = szyldyArk[twarz == 1 ? ArkMonetaA : ArkMonetaR]; var top = c + new Vector3(0f, H + .0004f, 0f);
        for (int i = 0; i < N; i++)
        {
            float a0 = 2f * Mathf.PI * i / N, a1 = 2f * Mathf.PI * (i + 1) / N;
            Vector3 p0 = top + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * R, p1 = top + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * R;
            b.TriangleUV(m, top, p1, p0, new Vector2(.5f, .5f), new Vector2(.5f + .5f * Mathf.Cos(a1), .5f + .5f * Mathf.Sin(a1)), new Vector2(.5f + .5f * Mathf.Cos(a0), .5f + .5f * Mathf.Sin(a0)));   // seen from above: +x right, +z up
        }
    }
}
