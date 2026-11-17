using UnityEditor;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - KASYNO WAWER, the SALON VIP (owner 16.11, the queue: "Salon VIP i złote monety właściciela w kasynach";
// his SALON VIP sign from his sheet). The casinos' private salon (a "salle privée") for the high limits, in the walnut block
// the hall builds in its rear west corner (BraxChampionsCasinoT11.SalonVip):
//  - outside: his SALON VIP sign over the door in a gold portal, red velvet ropes on brass posts and the hostess;
//  - the high-limit tables, the floor's games at their own stakes: BLACKJACK VIP (100) and BAKARAT VIP (200)
//    (BraxChampionsBlackjack / BraxChampionsBakarat, the same rules, the same casino credit), their croupiers;
//  - the bar on the rear wall: a walnut counter with a black marble top and a brass foot rail, four leather stools, the back
//    bar with three glass shelves of bottles before a brass panel, the barman; his BRAXHORN CASINO logo (his art 1:1) framed
//    over it;
//  - the lounge by the door: a leather sofa, two armchairs, a round coffee table on a velvet rug, plants (Poly Haven, CC0,
//    real size);
//  - the vitrine of his gold: his BRAXHORN coins (his coin art 1:1 on their faces) on red velvet under glass.
public static partial class BraxChampionsKasynoSprzet
{
    public const int VipBlackjack = 100, VipBakarat = 200;
    public static int SalonVipModele;
    static Material butelkaBursztyn, butelkaZielona, butelkaBiala;

    /// <summary>The salon's furniture and tables in its inside x0..x1, z0..z1 (the hall's frame, nominal metres) on its floor y;
    /// its door in the east wall (x1) between drzwiZ0 and drzwiZ1; fl the hall's floor outside (the ropes, the hostess).</summary>
    public static void SalonVip(Kasyno k, Transform g, float x0, float x1, float z0, float z1, float y, float drzwiZ0, float drzwiZ1, float fl, float scianaX)
    {
        Przygotuj(); BlackjackMaterialy();
        if (!butelkaBursztyn)
        {
            butelkaBursztyn = BraxChampionsHarbour.Surface("vip_butelka_bursztyn", new Color(.62f, .33f, .08f, .80f), .06f, 0f, null, true);
            butelkaZielona = BraxChampionsHarbour.Surface("vip_butelka_zielona", new Color(.12f, .36f, .16f, .80f), .06f, 0f, null, true);
            butelkaBiala = BraxChampionsHarbour.Surface("vip_butelka_biala", new Color(.86f, .90f, .92f, .45f), .04f, 0f, null, true);
            foreach (var m in new[] { butelkaBursztyn, butelkaZielona, butelkaBiala }) Wnetrze(m);
        }
        var b = k.body; SalonVipModele = 0;

        // ---- the high-limit tables against the west wall, their players to the room; the floor's games at their own stakes
        int bj = k.blackjack.Count, bk = k.bakarat.Count;
        Blackjack(k, new Vector3(x0 + 1.2f, y, 21.3f), 90f);
        Bakarat(k, new Vector3(x0 + 1.2f, y, 17.4f), 90f);
        if (k.blackjack.Count == bj + 1) { k.blackjack[bj].stawka = VipBlackjack; k.blackjack[bj].nazwa = "BLACKJACK VIP"; }
        if (k.bakarat.Count == bk + 1) { k.bakarat[bk].stawka = VipBakarat; k.bakarat[bk].nazwa = "BAKARAT VIP"; }

        // ---- the bar on the rear wall
        float bx = -30.0f, bz = z1;                                                     // the back bar's back on the wall
        b.RoundedBox(ciemnyOrzech, new Vector3(bx, y + .45f, bz - .225f), new Vector3(3.8f, .90f, .45f), .012f, Quaternion.identity, 2);
        b.Box(marmur, new Vector3(bx, y + .915f, bz - .23f), new Vector3(3.84f, .03f, .48f), Quaternion.identity);
        b.Box(mosiadz, new Vector3(bx, y + 1.75f, bz - .012f), new Vector3(3.6f, 1.6f, .02f), Quaternion.identity);        // the brass panel 2 mm off the wall
        float[] polki = { 1.30f, 1.72f, 2.14f };
        var kolory = new[] { butelkaBursztyn, butelkaZielona, butelkaBiala, butelkaBursztyn, butelkaBiala };
        int butelki = 0;
        foreach (float py in polki)
        {
            b.Box(szklo, new Vector3(bx, y + py - .006f, bz - .17f), new Vector3(3.5f, .012f, .26f), Quaternion.identity);
            foreach (float sx in new[] { -1.70f, 0f, 1.70f }) b.Box(mosiadz, new Vector3(bx + sx, y + py - .03f, bz - .13f), new Vector3(.02f, .035f, .22f), Quaternion.identity);
            for (int i = 0; i < 16; i++)
            {
                var p = new Vector3(bx - 1.6f + i * .213f, y + py, bz - .17f + ((i % 2) - .5f) * .05f);
                var m = kolory[(i + (int)(py * 10f)) % kolory.Length];
                b.Cylinder(m, p, .036f, .21f, 12, Quaternion.identity, true);
                b.Cylinder(m, p + Vector3.up * .21f, .036f, .05f, 12, Quaternion.identity, false, false, .014f);
                b.Cylinder(m, p + Vector3.up * .26f, .014f, .06f, 8, Quaternion.identity, true);
                b.Cylinder(czern, p + Vector3.up * .32f, .016f, .02f, 8, Quaternion.identity, true);
                butelki++;
            }
        }
        W.Kolizja(new Vector3(bx, y + 1.3f, bz - .23f), new Vector3(3.84f, 2.6f, .48f), Quaternion.identity);
        // the counter: its front to the room, the black marble top, a brass foot rail on brass brackets
        float cz = bz - 1.45f;
        b.RoundedBox(ciemnyOrzech, new Vector3(bx, y + .53f, cz), new Vector3(4.0f, 1.06f, .60f), .015f, Quaternion.identity, 2);
        b.RoundedBox(marmur, new Vector3(bx, y + 1.08f, cz - .03f), new Vector3(4.1f, .04f, .72f), .01f, Quaternion.identity, 2);
        b.Box(mosiadz, new Vector3(bx, y + .90f, cz - .303f), new Vector3(4.0f, .03f, .006f), Quaternion.identity);
        b.Tube(mosiadz, new Vector3(bx - 1.9f, y + .20f, cz - .42f), new Vector3(bx + 1.9f, y + .20f, cz - .42f), .025f, 12);
        foreach (float sx in new[] { -1.8f, -.6f, .6f, 1.8f }) b.Tube(mosiadz, new Vector3(bx + sx, y + .20f, cz - .42f), new Vector3(bx + sx, y + .20f, cz - .30f), .012f, 8);
        W.Kolizja(new Vector3(bx, y + .55f, cz - .03f), new Vector3(4.1f, 1.1f, .72f), Quaternion.identity);
        W.Obsada("barman salonu VIP", new Vector3(bx + .4f, y, bz - .78f), 180f);
        // four leather stools before it (their seats AKCJA "HOKER", out of the tables' reach)
        for (int i = 0; i < 4; i++)
        {
            var p = new Vector3(bx - 1.5f + i * 1.0f, y, cz - .78f);
            if (Mebel(g, "HotelSobczak/bar_chair_round_01/bar_chair_round_01_1k.gltf", "salon VIP | hoker", p, 0f))
                W.Siedzisko(p + Vector3.up * .78f, Vector3.forward, "HOKER");
        }
        // his logo over the bar in a brass frame
        float lw = 1.40f, lh = lw / ArkProporcja(ArkLogo); var lz = new Vector3(bx, y + 3.30f, bz - .03f);
        b.Box(mosiadz, lz + new Vector3(0f, 0f, .012f), new Vector3(lw + .12f, lh + .12f, .03f), Quaternion.identity);
        Obraz(b, szyldyArk[ArkLogo], lz - new Vector3(0f, 0f, .005f + Przed), lw, lh, Quaternion.Euler(0f, 180f, 0f));

        // ---- the lounge north of the door: the sofa on the east wall looking west, two armchairs before it, the table, the rug
        float lx = x1 - .55f, lzc = 23.4f;
        b.Box(aksamit, new Vector3(x1 - 1.55f, y + .006f, lzc), new Vector3(2.5f, .012f, 2.9f), Quaternion.identity);
        if (Mebel(g, "ArenaLobby/sofa_02/sofa_02_2k.gltf", "salon VIP | sofa", new Vector3(lx, y, lzc), -90f))
            foreach (float dz in new[] { -.4f, .4f }) W.Siedzisko(new Vector3(lx - .1f, y + .45f, lzc + dz), Vector3.left, "SOFA");     // a two-seater
        foreach (float dz in new[] { -.75f, .75f })
            if (Mebel(g, "ArenaLobby/modern_arm_chair_01/modern_arm_chair_01_2k.gltf", "salon VIP | fotel", new Vector3(x1 - 2.55f, y, lzc + dz), 90f))
                W.Siedzisko(new Vector3(x1 - 2.55f, y + .45f, lzc + dz), Vector3.right, "FOTEL");
        Mebel(g, "HotelSobczak/coffee_table_round_01/coffee_table_round_01_2k.gltf", "salon VIP | stolik", new Vector3(x1 - 1.55f, y + .012f, lzc), 0f);
        Mebel(g, "ArenaLobby/potted_plant_02/potted_plant_02_1k.gltf", "salon VIP | roślina", new Vector3(x1 - .45f, y, z1 - .45f), 30f);
        Mebel(g, "ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "salon VIP | roślina", new Vector3(x1 - .45f, y, drzwiZ1 + .45f), 120f);
        Mebel(g, "ArenaLobby/potted_plant_01/potted_plant_01_1k.gltf", "salon VIP | roślina", new Vector3(x0 + .45f, y, z1 - .45f), 200f);

        // ---- the vitrine of his gold south of the door: a walnut plinth, red velvet, his coins under glass
        var v = new Vector3(x1 - 1.0f, y, z0 + .75f); const float VW = 1.4f, VD = .6f, VH = .92f;
        b.RoundedBox(ciemnyOrzech, v + new Vector3(0f, VH * .5f, 0f), new Vector3(VW, VH, VD), .012f, Quaternion.identity, 2);
        b.Box(aksamit, v + new Vector3(0f, VH + .006f, 0f), new Vector3(VW - .06f, .012f, VD - .06f), Quaternion.identity);
        int monety = 0;
        for (int r = 0; r < 3; r++) for (int c = 0; c < 9; c++)
        {
            Moneta(b, v + new Vector3(-.56f + c * .14f, VH + .0125f, -.16f + r * .16f), 1 + (r + c) % 2); monety++;
        }
        foreach (float sx in new[] { -.62f, .62f }) for (int s = 0; s < 8; s++) { Moneta(b, v + new Vector3(sx, VH + .0125f + s * .0031f, .18f), s == 7 ? 1 : 0); monety++; }
        // the glass hood: four sides and its top, brass corners
        float gh = .28f, gy = VH + .012f;
        b.Box(szklo, v + new Vector3(0f, gy + gh, 0f), new Vector3(VW - .02f, .008f, VD - .02f), Quaternion.identity);
        foreach (float sz in new[] { -1f, 1f }) b.Box(szklo, v + new Vector3(0f, gy + gh * .5f, sz * (VD * .5f - .014f)), new Vector3(VW - .04f, gh, .006f), Quaternion.identity);
        foreach (float sx in new[] { -1f, 1f }) b.Box(szklo, v + new Vector3(sx * (VW * .5f - .014f), gy + gh * .5f, 0f), new Vector3(.006f, gh, VD - .04f), Quaternion.identity);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            b.Box(mosiadz, v + new Vector3(sx * (VW * .5f - .01f), gy + gh * .5f, sz * (VD * .5f - .01f)), new Vector3(.02f, gh, .02f), Quaternion.identity);
        W.Kolizja(v + new Vector3(0f, (VH + gh) * .5f, 0f), new Vector3(VW, VH + gh + .02f, VD), Quaternion.identity);
        W.Lampa("salon VIP | światło witryny", v + new Vector3(0f, VH + .9f, 0f), .6f, 2.2f * BraxClaudeOsiedle.K, new Color(1f, .86f, .62f));

        // ---- the warm lights: under the two chandeliers over the tables, over the bar, over the lounge
        float skala = BraxClaudeOsiedle.K; var cieply = new Color(1f, .88f, .66f);
        foreach (float z in new[] { 17.4f, 21.3f }) W.Lampa("salon VIP | żyrandol nad stołem", new Vector3(x0 + 1.55f, y + 2.97f, z), 1.1f, 5.5f * skala, cieply);
        W.Lampa("salon VIP | bar", new Vector3(bx, y + 3.17f, cz), .9f, 5.0f * skala, new Color(1f, .86f, .62f));
        W.Lampa("salon VIP | salonik", new Vector3(x1 - 1.4f, y + 3.17f, lzc), .8f, 5.0f * skala, cieply);

        // ---- outside: his SALON VIP sign over the door, the red velvet ropes on brass posts, the hostess
        float dzc = (drzwiZ0 + drzwiZ1) * .5f, sw = 2.2f, sh = sw / ArkProporcja(ArkVip);
        var sc = new Vector3(scianaX + .03f, fl + 3.62f, dzc); var sq = Quaternion.Euler(0f, 90f, 0f);
        b.RoundedBox(czern, sc + new Vector3(.015f, 0f, 0f), new Vector3(.04f, sh + .10f, sw + .10f), .01f, Quaternion.identity, 1);
        b.Box(mosiadz, sc + new Vector3(-.01f, 0f, 0f), new Vector3(.03f, sh + .16f, sw + .16f), Quaternion.identity);   // its rim, 5 mm off the wall
        Obraz(b, szyldyArk[ArkVip], sc + new Vector3(.035f + Przed, 0f, 0f), sw, sh, sq);
        float ox = scianaX + .35f;
        Liny(k, new[] { new Vector3(ox, fl, drzwiZ0 - .35f), new Vector3(ox + .9f, fl, drzwiZ0 - .35f), new Vector3(ox + 1.6f, fl, drzwiZ0 + .15f) });
        Liny(k, new[] { new Vector3(ox, fl, drzwiZ1 + .35f), new Vector3(ox + .9f, fl, drzwiZ1 + .35f), new Vector3(ox + 1.6f, fl, drzwiZ1 - .15f) });
        W.Obsada("hostessa salonu VIP", new Vector3(ox + 1.1f, fl, drzwiZ1 + .85f), 90f);

        Debug.Log("KASYNO_SALON_VIP " + k.nazwa + " tables=BLACKJACK_VIP(" + VipBlackjack + ")+BAKARAT_VIP(" + VipBakarat + ") bar=4_stools bottles=" + butelki +
            " lounge=sofa+2_armchairs coins_in_vitrine=" + monety + " sign=" + (SzyldyWlasciciela ? "owner" : "drawn") + " models=" + SalonVipModele);
    }

    // a Poly Haven model (CC0, real size) of the region's prepared sources, its bottom on at.y (at.x / at.z its centre), turned yaw
    static bool Mebel(Transform g, string path, string label, Vector3 at, float yaw)
    {
        string full = "Assets/Source/Mistrzowie/" + path;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(full);
        if (!source) { BraxBuildQuality.Warn("DO POPRAWKI KASYNO_SALON_VIP_MODEL_MISSING " + full); return false; }
        var go = PrefabUtility.InstantiatePrefab(source) as GameObject; if (!go) return false;
        go.name = label + " | Poly Haven CC0, real size";
        go.transform.SetParent(g, false);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * go.transform.localRotation;
        go.transform.localScale = Vector3.one; go.transform.localPosition = at;
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length > 0)
        {
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            go.transform.position += new Vector3(at.x - bb.center.x, at.y - bb.min.y, at.z - bb.center.z);
            W.Kolizja(new Vector3(at.x, at.y + bb.size.y * .5f, at.z), new Vector3(bb.size.x, bb.size.y, bb.size.z), Quaternion.identity);
        }
        SalonVipModele++; return true;
    }
}
