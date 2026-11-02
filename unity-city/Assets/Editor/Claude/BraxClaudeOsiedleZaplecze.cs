using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ZAPLECZE KASYNA OD ZAUŁKA (film właściciela 1.11, 17:54: „nie wiem, co to w ogóle jest, ale to nic nie jest na pewno”).
// Tylne drzwi Codexa (x 84–88, z −57) prowadziły w szczelinę między skorupą budynku (brązowe ściany, three.js civic.js)
// a zaimportowaną salą gier (gameready_casino_scene: x 69–103, z −51.12…−34.88 — z zewnątrz czarne lica i pomarańczowe
// krawędzie modelu): pas za salą (z −56.83…−51.12) i dwie kiszki po bokach (x 66.2–69 / 103–105.8) aż do przedsionka.
// Teraz: kiszki zamknięte ścianami (i od przedsionka kolizją — wizualnie zamykała je już boazeria przedsionka); pas za salą
// wykończony: płytki, ściany malowane (czarne lica sali zakryte ścianą), sufit podwieszany 3.3 m, lampy ForceVertex.
// Układ: hol przy drzwiach (x 80.5–91.5, 2.5 m wolnego za drzwiami) → sala VIP poker (x 91.5–105.8: stół owalny z suknem,
// 8 krzeseł przodem do stołu, dywan, boazeria z tapetą, lampy nad stołem) i biuro kierownika (x 69.8–80.5: biurko, fotel,
// szafy, sofa, sejf) z WC (x 66.2–69.8, drzwi z biura). Stary stół pokerowy Codexa (skala ×1.4, krzesła bokiem) usunięty.
// ZAUŁEK (z −66…−57.2 między siłownią a kasynem): stalowe drzwi serwisowe zamiast drewnianych wrót, daszek, lampy ścienne
// (nie świecą), tabliczka TYLKO DLA PERSONELU, utwardzenie całego pasa (bez kęp trawy na płytach), kontenery na odpady,
// palety, rura spustowa, odwodnienie liniowe; samotne ławki i kamienne klocki Codexa usunięte.
public static partial class BraxClaudeOsiedle
{
    static void ZapleczeKasyna(Transform estate, Transform physics)
    {
        group = "Osiedle A / kasyno — zaplecze"; bezCieni.Add(group);
        const float X0 = 66.2f, X1 = 105.8f, ZB = -56.8f, ZS = -51.17f, FY = .06f, CY = 3.3f, wt = .12f, dH = 2.65f;
        // stary stół pokerowy z krzesłami (EstateMesh_429/430) i kamienne klocki Codexa w zaułku
        int gone = 0;
        foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true).ToList())
            if (mr && (mr.name == "EstateMesh_429" || mr.name == "EstateMesh_430")) { Object.DestroyImmediate(mr.gameObject); gone++; }
        CutVolumes(estate, physics, new List<Bounds> { MinMax(77.0f, 79.0f, -.01f, .5f, -60.6f, -59.4f), MinMax(93.0f, 95.0f, -.01f, .5f, -60.6f, -59.4f) });
        var plytki = PbrFloor; var farba = InteriorPaint;
        var sufit = Plain("sufit zaplecza", new Color(.9f, .9f, .88f), .1f);
        var orzech = Plain("boazeria orzech lakier", new Color(.20f, .10f, .05f), .7f);
        var bordo = Plain("tapeta bordo sali VIP", new Color(.30f, .05f, .07f), .3f);
        var zloto = Plain("złoto kasyna (mosiądz szczotkowany)", new Color(.80f, .62f, .25f), .72f, .9f);
        var dywan = TexMat("dywan_kasyna", 2f, .05f);
        var sukno = Plain("sukno stołu pokerowego", new Color(.06f, .30f, .16f), .1f);
        var porcelana = Plain("porcelana sanitarna", new Color(.95f, .95f, .94f), .85f);
        // kiszki po bokach sali gier: zamknięte ścianą od pasa (z ZS) i kolizją od przedsionka (x 66.2–68 / 104–105.8, z −34.6)
        BoxMinMax(farba, X0, 69.0f, FY, CY, ZS - .05f, ZS + .05f, true); BoxMinMax(farba, 103.0f, X1, FY, CY, ZS - .05f, ZS + .05f, true);
        BoxMinMax(farba, X0, 69.0f, CY, 6.9f, ZS - .05f, ZS + .05f, true); BoxMinMax(farba, 103.0f, X1, CY, 6.9f, ZS - .05f, ZS + .05f, true);
        Collider(new Vector3(67.1f, 3.4f, -34.62f), new Vector3(1.85f, 6.8f, .2f)); Collider(new Vector3(104.9f, 3.4f, -34.62f), new Vector3(1.85f, 6.8f, .2f));
        // pas za salą: posadzka, ściany, sufit
        Top(plytki, X0, X1, ZB, ZS, FY + .004f);
        PrzejscieSalaZaplecze(estate, physics, farba, X0, X1, FY, CY, ZS);                                    // zakrywa czarne lico sali gier; E27(1): przejście z sali (BraxClaudeOsiedleKasynoPrzejscie)
        BoxMinMax(farba, X0, 84f, FY, CY, ZB, ZB + .03f, false); BoxMinMax(farba, 88f, X1, FY, CY, ZB, ZB + .03f, false);
        BoxMinMax(farba, 84f, 88f, 3.1f, CY, ZB, ZB + .03f, false);
        BoxMinMax(farba, X0, X0 + .03f, FY, CY, ZB, ZS, false); BoxMinMax(farba, X1 - .03f, X1, FY, CY, ZB, ZS, false);
        BoxMinMax(sufit, X0, X1, CY, CY + .03f, ZB, ZS, false, true);
        // ściany działowe z drzwiami 1.2 × 2.65 m (ościeżnica 7 cm)
        void WallZ(float x, float dz)
        {
            BoxMinMax(farba, x - wt * .5f, x + wt * .5f, FY, CY, ZB, dz - .67f, true); BoxMinMax(farba, x - wt * .5f, x + wt * .5f, FY, CY, dz + .67f, ZS, true);
            BoxMinMax(farba, x - wt * .5f, x + wt * .5f, FY + dH + .07f, CY, dz - .67f, dz + .67f, false, true);
            foreach (float z in new[] { dz - .67f, dz + .6f }) BoxMinMax(Frame, x - .08f, x + .08f, FY, FY + dH + .07f, z, z + .07f, false);
            BoxMinMax(Frame, x - .08f, x + .08f, FY + dH, FY + dH + .07f, dz - .67f, dz + .67f, false, true);
        }
        const float xVip = 91.5f, xBiuro = 80.5f, xWc = 69.8f, dVip = -53.4f, dBiuro = -53.4f, dWc = -54.6f;
        WallZ(xVip, dVip); WallZ(xBiuro, dBiuro); WallZ(xWc, dWc);
        Sign("vip_poker", new Vector3(xVip - .07f, 2.98f, dVip), 1.9f, .38f, Vector3.left);
        Sign("biuro", new Vector3(xBiuro + .07f, 2.98f, dBiuro), 1.9f, .38f, Vector3.right);
        Sign("wc", new Vector3(xWc + .07f, 2.98f, dWc), .55f, .32f, Vector3.right);
        // HOL (x 80.5–91.5): ławka i wieszak przy ścianie sali, kosz; strefa 2.5 m za drzwiami (z −56.8…−54.3) wolna
        BoxMinMax(Plain("ławka holu dąb", new Color(.45f, .33f, .22f), .4f), 82.0f, 84.0f, .42f, .47f, ZS - .5f, ZS - .1f, false, true);
        foreach (float x in new[] { 82.2f, 83.8f }) BoxMinMax(Steel, x - .03f, x + .03f, FY, .42f, ZS - .45f, ZS - .15f, false);
        Collider(new Vector3(83f, .25f, ZS - .3f), new Vector3(2f, .5f, .4f));
        BoxMinMax(Steel, 86.5f, 89.5f, 1.75f, 1.8f, ZS - .1f, ZS - .03f, false);
        for (float x = 86.7f; x < 89.4f; x += .4f) BoxMinMax(Chrom, x - .012f, x + .012f, 1.62f, 1.78f, ZS - .16f, ZS - .1f, false);
        Bin(new Vector3(90.8f, FY, ZS - .35f));
        // SALA VIP (x 91.5–105.8): dywan, boazeria do 1.1 m ze złotą listwą, tapeta bordo, stół owalny 2.8 × 1.4 m, 8 krzeseł
        Top(dywan, xVip + .06f, X1 - .03f, ZB + .03f, ZS - .03f, FY + .01f);
        foreach (var (a0, a1, b0, b1) in new[] { (xVip + .06f, X1 - .03f, ZS - .05f, ZS - .03f), (xVip + .06f, X1 - .03f, ZB + .03f, ZB + .05f) })
        { BoxMinMax(orzech, a0, a1, FY, 1.1f, b0, b1, false); BoxMinMax(zloto, a0, a1, 1.1f, 1.14f, b0, b1, false, true); BoxMinMax(bordo, a0, a1, 1.14f, CY, b0, b1, false); }
        BoxMinMax(orzech, X1 - .05f, X1 - .03f, FY, 1.1f, ZB, ZS, false); BoxMinMax(bordo, X1 - .05f, X1 - .03f, 1.14f, CY, ZB, ZS, false);
        var tc = new Vector3(99.0f, FY, -54.0f); const float tH = .76f, tL = 1.4f, tR = .7f;
        foreach (float sx in new[] { -tL * .5f, tL * .5f }) { Cylinder(orzech, tc + new Vector3(sx, tH - .06f, 0), tR, .07f, 28, true, false); Cylinder(sukno, tc + new Vector3(sx, tH + .01f, 0), tR - .1f, .002f, 28, true, false); }
        BoxMinMax(orzech, tc.x - tL * .5f, tc.x + tL * .5f, tH - .06f, tH + .01f, tc.z - tR, tc.z + tR, false, true);
        BoxMinMax(sukno, tc.x - tL * .5f, tc.x + tL * .5f, tH + .01f, tH + .012f, tc.z - tR + .1f, tc.z + tR - .1f, false);
        Cylinder(Steel, tc, .45f, .05f, 20, true, false); Cylinder(Steel, tc + Vector3.up * .05f, .12f, tH - .11f, 14, false, false);
        Collider(tc + Vector3.up * .4f, new Vector3(tL + 2 * tR, .8f, 2 * tR));
        var chip = new[] { Plain("żeton czerwony", new Color(.62f, .05f, .07f), .5f), Plain("żeton czarny", new Color(.03f, .03f, .035f), .5f), Plain("żeton zielony", new Color(.05f, .35f, .18f), .5f) };
        int krzesel = 0;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f; var p = tc + new Vector3(Mathf.Cos(a) * (tL * .5f + tR + .5f), 0, Mathf.Sin(a) * (tR + .5f));
            float yaw = Mathf.Atan2(tc.x - p.x, tc.z - p.z) * Mathf.Rad2Deg;
            if (offChair && Put(offChair, p, yaw, .03f, true)) krzesel++;
            var on = tc + (p - tc) * .62f; on.y = FY + tH + .012f;
            for (int k = 0; k < 3; k++) Cylinder(chip[(i + k) % 3], on + new Vector3((k - 1) * .05f, 0, 0), .02f, .02f + .015f * ((i + k) % 3), 12, true, false);
        }
        if (plantPot) foreach (var pp in new[] { new Vector3(92.4f, FY, ZB + .5f), new Vector3(105.1f, FY, ZS - .5f) }) { var o = Put(plantPot, pp, pp.x * 5f, .03f, false); if (o) { o.transform.localScale *= 2.2f; var b = RB(o); o.transform.position += new Vector3(pp.x - b.center.x, pp.y - b.min.y, pp.z - b.center.z); b = RB(o); Collider(b.center, b.size); } }
        // BIURO KIEROWNIKA (x 69.8–80.5): biurko przodem do drzwi, fotel, szafy na akta, sofa, sejf, roślina
        if (offDesk) Put(offDesk, new Vector3(74.0f, FY, -54.3f), 90f, .03f, true);
        if (offArm) Put(offArm, new Vector3(72.9f, FY, -54.3f), 90f, .03f, true);
        if (offShelf) foreach (float z in new[] { -55.9f, -52.2f }) Put(offShelf, new Vector3(70.3f, FY, z), 90f, .03f, true);
        if (offSofa) Put(offSofa, new Vector3(77.5f, FY, ZS - .55f), 180f, .03f, true);
        BoxMinMax(Steel, 79.6f, 80.3f, FY, 1.0f, ZB + .05f, ZB + .65f, true); BoxMinMax(Chrom, 79.55f, 79.6f, .55f, .62f, ZB + .3f, ZB + .4f, false);   // sejf
        // WC (x 66.2–69.8, drzwi z biura): płytki do 1.6 m, miska przy ścianie sali, umywalka z lustrem przy ścianie tylnej
        BoxMinMax(PoolTiles, X0 + .03f, X0 + .045f, FY, 1.6f, ZB, ZS, false); BoxMinMax(PoolTiles, X0, xWc - .06f, FY, 1.6f, ZS - .045f, ZS - .03f, false);
        BoxMinMax(PoolTiles, X0, xWc - .06f, FY, 1.6f, ZB + .03f, ZB + .045f, false);
        Cylinder(porcelana, new Vector3(67.6f, FY, ZS - .45f), .2f, .38f, 20, true, false);
        BoxMinMax(porcelana, 67.4f, 67.8f, FY + .38f, FY + .41f, ZS - .67f, ZS - .25f, false, true);
        BoxMinMax(porcelana, 67.34f, 67.86f, .7f, 1.1f, ZS - .2f, ZS - .045f, false, true);
        Collider(new Vector3(67.6f, .4f, ZS - .38f), new Vector3(.45f, .8f, .65f));
        BoxMinMax(porcelana, 67.3f, 67.86f, .78f, .9f, ZB + .045f, ZB + .5f, true, true);
        BoxMinMax(Plain("lustro (srebrzone szkło)", new Color(.78f, .8f, .81f), .97f, 1f), 67.3f, 67.86f, 1.2f, 1.9f, ZB + .045f, ZB + .057f, false);
        // lampy: panele sufitowe (mleczne, nie świecą) + światła ForceVertex; nad stołem lampa wisząca
        foreach (var p in new[] { new Vector2(86f, -54f), new Vector2(75f, -54f), new Vector2(68f, -54f), new Vector2(95f, -54f), new Vector2(103f, -54f) })
        {
            BoxMinMax(LampGlass, p.x - .3f, p.x + .3f, CY - .03f, CY, p.y - .3f, p.y + .3f, false, true);
            PointLamp(new Vector3(p.x, CY - .5f, p.y), 7f, .9f, new Color(1f, .93f, .82f));
        }
        if (offLamp) HangLamp(offLamp, new Vector3(tc.x, 0, tc.z), CY, FY, CY + .03f);
        PointLamp(new Vector3(tc.x, 2.4f, tc.z), 5f, .8f, new Color(1f, .86f, .65f));
        var zw = new GameObject("zasięg kamery na zapleczu (BraxClaudeZasiegWnetrza)"); zw.transform.SetParent(Group(group), false);
        var zc = zw.AddComponent<BraxClaudeZasiegWnetrza>(); zc.zasieg = 60f; zc.sale = new[] { MinMax(X0, X1, -.5f, CY, ZB, ZS) };

        // ---------------- ZAUŁEK (z −66…−57.2) ----------------
        group = "Osiedle A / zaułek za kasynem";
        const float zK = -57.17f, zG = -66.0f;
        Top(Slabs, 64.6f, 107.4f, -61.5f, zK - .01f, .058f);                       // utwardzenie pasa przy kasynie (Hard/Paved: bez trawy 3D)
        Hard(55f, 107.4f, zG, zK); Paved(55f, 107.4f, zG, zK);
        // tylne drzwi: skrzydła Codexa (EstateMesh_494/495, ruchome) jako stalowe drzwi serwisowe, daszek, lampy, tabliczka
        var stal = Plain("drzwi stalowe serwisowe (grafit, mat)", new Color(.2f, .21f, .22f), .45f, .6f);
        int skrzydla = 0;
        foreach (var mr in estate.GetComponentsInChildren<MeshRenderer>(true))
            if (mr.name == "EstateMesh_494" || mr.name == "EstateMesh_495") { mr.sharedMaterials = Enumerable.Repeat(stal, mr.sharedMaterials.Length).ToArray(); skrzydla++; }
        BoxMinMax(Frame, 83.3f, 88.7f, 3.18f, 3.28f, zK - 1.3f, zK, false, true);                                  // daszek
        foreach (float x in new[] { 83.5f, 88.5f }) OBoxQ(Frame, new Vector3(x, 2.95f, zK - .6f), new Vector3(.05f, .05f, 1.25f), Quaternion.Euler(-22f, 0, 0));
        Sign("personel", new Vector3(86f, 3.62f, zK - .02f), 2.2f, .5f, Vector3.back);
        foreach (float x in new[] { 82.7f, 89.3f, 72.0f, 100.0f })
        {
            BoxMinMax(Frame, x - .1f, x + .1f, 2.55f, 2.6f, zK - .28f, zK, false, true);
            BoxMinMax(LampGlass, x - .08f, x + .08f, 2.3f, 2.55f, zK - .26f, zK - .1f, false, true);
            BoxMinMax(Frame, x - .1f, x + .1f, 2.27f, 2.3f, zK - .28f, zK - .08f, false, true);
        }
        // rura spustowa z dachu kasyna (7.3 m) z kolanem i wylewką nad odwodnieniem
        foreach (float x in new[] { 80.0f, 92.0f })
        {
            Cylinder(Steel, new Vector3(x, .25f, zK - .12f), .055f, 7.0f, 12, false, false);
            Tube(Steel, new Vector3(x, .3f, zK - .12f), new Vector3(x, .08f, zK - .45f), .055f, 12);
        }
        // odwodnienie liniowe wzdłuż pasa (ruszt stalowy)
        BoxMinMax(Steel, 66f, 106f, .058f, .066f, -61.62f, -61.42f, false);
        for (float x = 66.1f; x < 106f; x += .25f) BoxMinMax(Plain("szczelina rusztu", new Color(.02f, .02f, .02f), .1f), x, x + .12f, .066f, .067f, -61.58f, -61.46f, false);
        // kontenery na odpady 1100 l (zieleń, grafit) z pokrywą i kółkami — przy ścianie kasyna, na zachód od drzwi
        int kont = 0;
        foreach (var (x, kolor) in new[] { (73.2f, new Color(.10f, .28f, .14f)), (74.8f, new Color(.16f, .17f, .18f)), (76.4f, new Color(.10f, .28f, .14f)) })
        {
            var km = Plain("kontener " + kolor.r.ToString("0.00"), kolor, .35f);
            BoxMinMax(km, x - .68f, x + .68f, .2f, 1.3f, zK - 1.15f, zK - .1f, true, true);
            OBoxQ(km, new Vector3(x, 1.36f, zK - .6f), new Vector3(1.42f, .06f, 1.15f), Quaternion.Euler(-4f, 0, 0));
            foreach (float dx in new[] { -.55f, .55f }) foreach (float dz in new[] { -1.0f, -.25f }) Cylinder(GumaCzarna, new Vector3(x + dx, .058f, zK + dz), .09f, .15f, 10, true, false);
            BoxMinMax(Steel, x - .7f, x + .7f, 1.05f, 1.1f, zK - 1.18f, zK - 1.15f, false);
            kont++;
        }
        // palety drewniane (stos 4) i skrzynka po prawej od rury
        var paleta = Plain("paleta drewno surowe", new Color(.62f, .5f, .36f), .15f);
        for (int k = 0; k < 4; k++)
        {
            float y0 = .058f + k * .144f;
            foreach (float dz in new[] { -.05f, -.55f, -1.05f }) BoxMinMax(paleta, 95.0f, 96.2f, y0, y0 + .1f, zK + dz - .1f, zK + dz, false);
            for (float x = 95.05f; x < 96.2f; x += .15f) BoxMinMax(paleta, x, x + .1f, y0 + .1f, y0 + .122f, zK - 1.2f, zK - .02f, false);
        }
        Collider(new Vector3(95.6f, .34f, zK - .6f), new Vector3(1.2f, .6f, 1.2f));
        rep.notes.Add("kasyno — zaplecze: hol + sala VIP poker (stół owalny, " + krzesel + " krzeseł przodem do stołu) + biuro kierownika + WC, ściany i sufit 3.3 m, kiszki boczne zamknięte, stary stół Codexa usunięty ("
                      + gone + " siatek); zaułek: stalowe drzwi (" + skrzydla + "/2 skrzydła), daszek, 4 lampy ścienne, tabliczka, " + kont + " kontenery, palety, rury spustowe, odwodnienie, pas utwardzony (bez trawy)");
    }
}
