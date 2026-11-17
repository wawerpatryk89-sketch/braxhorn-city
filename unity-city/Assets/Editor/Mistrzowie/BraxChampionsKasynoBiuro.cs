using System;
using UnityEngine;
using W = BraxWnetrzaKit;

// MIASTO MISTRZÓW - WAWER NIERUCHOMOŚCI, the real estate office of Patryk and Monika (owner 16.11 23:12: "To masz do Biura
// Nieruchomości" with his poster "Poznaj Monikę - Prowadzę biuro nieruchomości w Mieście Mistrzów"; 23:13: "U ciebie też biura
// i tylko wille kozaki zrobimy na sprzedaż 3"; ZASADA Patryk i Monika: the places the player visits most are theirs, Monika
// often there). The west wing of the T12 tower's ground floor (BraxChampionsWiezowce.Stacked), its own street door: as a
// premium agency - Monika's desk (walnut, white marble) with two client chairs, the architectural model of the three
// villas on the hill under a glass cover (1:200), the shelves of binders, a lounge, plants, her poster (his art 1:1) on a
// stand in the window and framed on the wall; AKCJA at the desk asks her about the villas.
public static partial class BraxChampionsKasynoSprzet
{
    public const string NieruchomosciWlasciciel = "Assets/Source/Mistrzowie/NieruchomosciWlasciciel";
    public static bool PlakatMoniki;

    /// <summary>The office in the box x0..x1, z0..z1 (nominal metres, root at the origin while building; the street at z0, its
    /// door at doorX), the floor at y; b = the batch of the static parts; mowca = where Monika stands.</summary>
    public static Transform Biuro(Transform root, BraxChampionsMeshBatch b, float x0, float x1, float z0, float z1, float y, float doorX)
    {
        Przygotuj();
        var g = new GameObject("WAWER NIERUCHOMOŚCI | biuro nieruchomości Patryka i Moniki").transform; g.SetParent(root, false);
        // her poster (his art 1:1), a back-lit print
        var src = Wczytaj(NieruchomosciWlasciciel + "/plakaty/wawer_nieruchomosci_monika.png", out int sw, out int sh);
        PlakatMoniki = src != null; Material plakat = null;
        if (src != null)
        {
            var pp = new Color32[1024 * 1536]; Wklej(pp, 1024, 1536, src, sw, sh, 0, 0, sw, sh, 0, 0, 1024, 1536);
            plakat = Ekran("nieruchomosci_plakat_monika", Png("nieruchomosci_plakat_monika", 1024, 1536, pp, true), .92f);
        }
        else BraxBuildQuality.Warn("DO POPRAWKI NIERUCHOMOSCI_PLAKAT missing " + NieruchomosciWlasciciel + "/plakaty (pack braxhorn-mistrzowie-nieruchomosci-20261116)");
        var bialy = Metal("makieta_biel", new Color(.92f, .92f, .90f), .7f, 0f);
        var trawa = Metal("makieta_trawa", new Color(.30f, .42f, .22f), .9f, 0f);
        var woda = Swiatlo("makieta_basen", new Color(.25f, .75f, 1.1f));
        var szybaM = Metal("makieta_szyby", new Color(.10f, .13f, .16f), .1f, 0f);
        var segregatory = new[] { Metal("segregator_granat", new Color(.08f, .12f, .28f), .5f, 0f), Metal("segregator_czern", new Color(.05f, .05f, .06f), .5f, 0f),
                                  Metal("segregator_bez", new Color(.62f, .55f, .44f), .5f, 0f) };
        // ---- Monika's desk facing the door, her chair behind it, two client chairs before it
        var desk = new Vector3(doorX, y, z0 + 6.2f);
        b.RoundedBox(ciemnyOrzech, desk + new Vector3(0f, .37f, 0f), new Vector3(2.2f, .74f, .85f), .015f, Quaternion.identity, 2);
        b.RoundedBox(marmur, desk + new Vector3(0f, .755f, 0f), new Vector3(2.3f, .03f, .95f), .008f, Quaternion.identity, 2);
        W.Kolizja(desk + new Vector3(0f, .38f, 0f), new Vector3(2.3f, .77f, .95f), Quaternion.identity);
        var ctx = new BraxKatalog.Ctx { m = b, root = root, cols = root, unit = 1f, water = null, inst = root, seat = (p, f, l) => { } };
        BraxKatalog.Monitor(ctx, desk + new Vector3(-.55f, .77f, .15f), Vector3.forward, 1, 24f);
        // a plan of a villa on the desk (a white sheet with its rooms drawn in grey)
        b.Box(bialy, desk + new Vector3(.35f, .772f, -.12f), new Vector3(.60f, .002f, .42f), Quaternion.identity);
        var monika = W.Obsada("Monika - WAWER NIERUCHOMOŚCI", desk + new Vector3(0f, 0f, .95f), 180f);   // (the chairs: the tower's models)
        W.Akcja("nieruchomosci_wille", "WAWER NIERUCHOMOŚCI · ZAPYTAJ O WILLĘ",
            "MONIKA: Mamy trzy wille: na wzgórzu nad areną, na własnej wyspie w zatoce i nad samym morzem - najpiękniejsze w Mieście Mistrzów. Sprzedaż ruszy wkrótce, zapraszam!",
            desk + new Vector3(0f, 0f, -.95f), monika, 1.8f);
        // ---- the model of the three villas' sites under glass on a walnut plinth (owner 17.11: "Na wzgórzu jedna, druga mała
        // wyspa z willą. Trzecia gdzieś tam" - BraxChampionsWilleDzialki): the hill, the island with its jetty, the seafront
        var mk = new Vector3(x0 + 2.6f, y, z0 + 5.0f);
        b.RoundedBox(ciemnyOrzech, mk + new Vector3(0f, .43f, 0f), new Vector3(1.9f, .86f, 1.3f), .02f, Quaternion.identity, 2);
        b.Box(bialy, mk + new Vector3(0f, .875f, 0f), new Vector3(1.8f, .03f, 1.2f), Quaternion.identity);
        var morze = Metal("makieta_morze", new Color(.10f, .32f, .46f), .15f, 0f);
        var piasekM = Metal("makieta_piasek", new Color(.86f, .80f, .64f), .9f, 0f);
        float top = .89f;
        // the sea along the front of the model (the island's and the seafront's), the sand of the seafront's beach
        b.Box(morze, mk + new Vector3(.30f, top + .004f, .30f), new Vector3(1.16f, .008f, .56f), Quaternion.identity);
        b.Box(piasekM, mk + new Vector3(.62f, top + .006f, -.02f), new Vector3(.52f, .012f, .10f), Quaternion.identity);
        b.Box(trawa, mk + new Vector3(.62f, top + .01f, -.30f), new Vector3(.52f, .02f, .46f), Quaternion.identity);
        // the hill: three grass terraces in the back left
        foreach (var (w, d, h) in new[] { (.62f, .90f, .05f), (.44f, .62f, .05f), (.28f, .36f, .05f) })
        { b.Box(trawa, mk + new Vector3(-.55f, top + h * .5f, -.12f), new Vector3(w, h, d), Quaternion.identity); top += h; }
        // the island: sand ring and lawn in the sea, its jetty
        b.Cylinder(piasekM, mk + new Vector3(.02f, .893f, .30f), .19f, .02f, 28, Quaternion.identity, true);
        b.Cylinder(trawa, mk + new Vector3(.02f, .913f, .30f), .15f, .02f, 28, Quaternion.identity, true);
        b.Box(ciemnyOrzech, mk + new Vector3(-.06f, .905f, .48f), new Vector3(.02f, .006f, .20f), Quaternion.identity);
        // the villas: on the hill's top, on the island, behind the beach (white blocks, a dark glass band, a pool)
        var wille = new[] { (new Vector3(-.55f, 1.04f, -.10f), 0), (new Vector3(.02f, .933f, .30f), 1), (new Vector3(.62f, .91f, -.22f), 2) };
        foreach (var (p, l) in wille)
        {
            var c = mk + p;
            b.Box(bialy, c + new Vector3(0f, .025f, 0f), new Vector3(.20f, .05f, .13f), Quaternion.identity);
            b.Box(bialy, c + new Vector3(.03f, .075f, -.01f), new Vector3(.16f, .05f, .11f), Quaternion.identity);
            b.Box(szybaM, c + new Vector3(0f, .025f, -.066f), new Vector3(.18f, .03f, .004f), Quaternion.identity);
            b.Box(woda, c + new Vector3(-.02f, .002f, -.11f), new Vector3(.11f, .004f, .055f), Quaternion.identity);
        }
        var rnd = new System.Random(12);
        for (int i = 0; i < 14; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = .12f + (float)rnd.NextDouble() * .16f;
            var t = mk + new Vector3(-.55f + Mathf.Cos(a) * r, .94f, -.12f + Mathf.Sin(a) * r * 1.4f);
            b.Cylinder(ciemnyOrzech, t, .004f, .03f, 5, Quaternion.identity, false);
            b.RoundedBox(trawa, t + new Vector3(0f, .045f, 0f), new Vector3(.035f, .04f, .035f), .015f, Quaternion.identity, 2);
        }
        b.Box(szklo, mk + new Vector3(0f, 1.12f, 0f), new Vector3(1.78f, .45f, 1.18f), Quaternion.identity);
        W.Kolizja(mk + new Vector3(0f, .67f, 0f), new Vector3(1.9f, 1.34f, 1.3f), Quaternion.identity);
        W.Akcja("nieruchomosci_makieta", "MAKIETA · TRZY WILLE: WZGÓRZE, WYSPA, NABRZEŻE",
            "Willa na wzgórzu nad areną, willa na wyspie z pomostem, willa nad morzem z własną plażą: basen, taras z widokiem, garaż. Zapytaj Monikę przy biurku.",
            mk + new Vector3(1.25f, 0f, 0f), null, 1.4f);
        // ---- the shelves of binders along the side wall (x0), the lounge, the plants
        for (int s = 0; s < 3; s++)
        {
            // an open walnut bookcase: the back, two sides, the top; four shelves with the binders standing on them
            float zc = z0 + 9.0f + s * 1.3f, bx = x0 + .02f;
            b.Box(ciemnyOrzech, new Vector3(bx + .015f, y + 1.1f, zc), new Vector3(.03f, 2.2f, 1.2f), Quaternion.identity);
            foreach (float sz in new[] { -.585f, .585f }) b.Box(ciemnyOrzech, new Vector3(bx + .20f, y + 1.1f, zc + sz), new Vector3(.40f, 2.2f, .03f), Quaternion.identity);
            b.Box(ciemnyOrzech, new Vector3(bx + .20f, y + 2.185f, zc), new Vector3(.40f, .03f, 1.14f), Quaternion.identity);
            for (int k = 0; k < 4; k++)
            {
                float yy = y + .35f + k * .5f;
                b.Box(ciemnyOrzech, new Vector3(bx + .20f, yy - .015f, zc), new Vector3(.36f, .03f, 1.14f), Quaternion.identity);
                for (int j = 0; j < 14; j++)
                    if (rnd.NextDouble() < .8) b.Box(segregatory[(j + k + s) % 3], new Vector3(bx + .19f, yy + .16f, zc - .50f + j * .072f), new Vector3(.28f, .32f, .062f), Quaternion.identity);
            }
            W.Kolizja(new Vector3(bx + .20f, y + 1.1f, zc), new Vector3(.40f, 2.2f, 1.2f), Quaternion.identity);
        }
        // ---- her poster: on a stand in the window, to the street (both faces), and framed in brass on the inner wall (x1)
        if (plakat)
        {
            var st = new Vector3(doorX + 2.6f, y, z0 + .9f);
            b.Box(czern, st + new Vector3(0f, .015f, 0f), new Vector3(.70f, .03f, .40f), Quaternion.identity);
            b.Box(czern, st + new Vector3(0f, 1.20f, 0f), new Vector3(1.10f, 1.65f, .04f), Quaternion.identity);
            foreach (float f in new[] { 0f, 180f })
                Obraz(b, plakat, st + Quaternion.Euler(0f, f, 0f) * new Vector3(0f, 1.20f, .02f + Przed), 1.0f, 1.5f, Quaternion.Euler(0f, f, 0f));
            W.Kolizja(st + new Vector3(0f, .85f, 0f), new Vector3(1.1f, 1.7f, .2f), Quaternion.identity);
            var fr = new Vector3(x1 - .05f, y + 1.9f, z0 + 6.2f); var fq = Quaternion.Euler(0f, -90f, 0f);
            b.Box(mosiadz, fr + fq * new Vector3(0f, 0f, -.01f), new Vector3(1.32f, 1.92f, .03f), fq);
            Obraz(b, plakat, fr + fq * new Vector3(0f, 0f, .005f + Przed), 1.2f, 1.8f, fq);
        }
        W.Lampa("WAWER NIERUCHOMOŚCI światło", new Vector3((x0 + x1) * .5f, y + 3.6f, z0 + 6f), 1.0f, 12f, new Color(.98f, .97f, .95f));
        W.Lampa("WAWER NIERUCHOMOŚCI światło", new Vector3((x0 + x1) * .5f, y + 3.6f, z0 + 15f), .9f, 12f, new Color(.98f, .97f, .95f));
        Debug.Log("WAWER_NIERUCHOMOSCI office x=" + x0.ToString("F1") + ".." + x1.ToString("F1") + " z=" + z0.ToString("F1") + ".." + z1.ToString("F1") +
            " desk+Monika model=3 villas on the hill poster=" + (PlakatMoniki ? "owner" : "missing") + " akcja=ZAPYTAJ O WILLĘ");
        return g;
    }
}
