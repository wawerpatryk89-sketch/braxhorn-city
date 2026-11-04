using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// SALA BOKSERSKA (właściciel 1.11, 18:24: „dziwne krzesła w sali, szatnia chujowa, sala zjebana”). Budynek Codexa: civic.js
// boxing (−86, 64) → Unity lica ścian x 66.18…105.82, z 49.18…78.82, strop ~6.9 m; wejście z ulicy x 84–88 w ścianie z 79,
// wyjście x 84–88 w ścianie z 49 (oba do 3.1 m, drzwi Codexa zostają). Stare wnętrze wycięte razem z kolizjami: niebieskie
// „krzesła” z klocków, szafki-słupy, ławki, kamienne bloki, tablice, ring z cienkich listew, słupy i belki.
// Nowe: posadzka z czarnej gumy, ściany: panele do 1.2 m, czerwony pas, grafit; czarny strop. Ring 6 × 6 m na podeście 0.9 m
// z 4 grubymi białymi linami (BraxLina — te same co w siłowni A, reagują na Braxa) i schodkami od wejścia; 6 worków premium
// na łańcuchach (BraxWorek) pod stalową belką na dwóch słupach przy ścianie zachodniej; gruszka na platformie (speed bag)
// i lustro ścienne (zwykłe) przy ścianie wschodniej; ławki pod ścianą; tablica GRAFIK ZAJĘĆ. Szatnia od nowa w narożniku
// płd.-wsch. (x 96…105.8, z 49.2…61): szafki, ławka, 2 prysznice, WC z umywalką; drzwi 1.2 × 2.65 m, 2.5 m wolnego za drzwiami.
// Lampy ForceVertex (nic nie świeci), zasięg kamery w zamkniętej sali (BraxClaudeZasiegWnetrza).
public static partial class BraxClaudeOsiedle
{
    // Rzeczywista kontrola kapsułą po przebudowie osiedla.
    public static void SalaBokserskaPrzejscieKontrola()
    {
        Physics.SyncTransforms();
        const float y = .06f, x = 86f;
        float r = Mathf.Clamp(WzrostPatrykaWGrze * .15f, .26f, .35f);
        float h = WzrostPatrykaWGrze + .04f;
        var blokady = new List<string>();
        foreach (float z in new[] { 79.45f, 79.15f, 78.85f, 78.55f, 78.25f, 77.95f })
        {
            var low = new Vector3(x, y + r, z);
            var high = new Vector3(x, y + h - r, z);
            foreach (var c in Physics.OverlapCapsule(low, high, r,
                         ~((1 << 8) | (1 << 29)), QueryTriggerInteraction.Ignore))
            {
                if (!c || c.bounds.max.y < y + .4f || c.GetComponentInParent<BraxCityDoor>() != null) continue;
                string opis = c.name + " z=" + z.ToString("F2");
                if (!blokady.Contains(opis)) blokady.Add(opis);
            }
        }
        if (blokady.Count > 0)
            throw new System.InvalidOperationException("Sala bokserska — wejście zablokowane: " + string.Join("; ", blokady));
        Debug.Log("BRAXHORN_BOKS_WEJSCIE_OK kapsuła=" + r.ToString("F2"));
    }

    static void SalaBokserska(Transform estate, Transform physics)
    {
        group = "Osiedle A / sala bokserska"; bezCieni.Add(group);
        const float X0 = 66.18f, X1 = 105.82f, Z0 = 49.18f, Z1 = 78.82f, yF = .06f, yC = 6.8f;
        var cut = MinMax(X0 + .04f, X1 - .04f, .06f, 6.84f, Z0 + .04f, Z1 - .04f);
        CutVolumes(estate, physics, new List<Bounds> { cut });
        // Przetnij tylko stare wysokie BoxCollider przecinające światło drzwi,
        // bo zwykłe CutVolumes celowo pomija kolizje wyższe niż 2,2 m.
        var otworyBoks = new List<Bounds> {
            MinMax(84.05f, 87.95f, .08f, 3.1f, Z1 - .45f, Z1 + .45f),
            MinMax(84.05f, 87.95f, .08f, 3.1f, Z0 - .45f, Z0 + .45f)
        };
        int wycieteZewn = 0;
        foreach (var rodzic in new[] { estate, physics })
        {
            if (!rodzic) continue;
            var stareKolizje = new List<BoxCollider>(rodzic.GetComponentsInChildren<BoxCollider>(true));
            foreach (var c in stareKolizje)
            {
                if (!c || !c.enabled || c.isTrigger || c.bounds.size.y <= 2.2f) continue;
                if (c.GetComponentInParent<BraxCityDoor>() != null) continue;
                if (!c.bounds.Intersects(otworyBoks[0]) && !c.bounds.Intersects(otworyBoks[1])) continue;
                if (BraxWycinanieSiatki.WytnijKolizje(c, otworyBoks)) wycieteZewn++;
            }
        }
        Debug.Log("BRAXHORN_BOKS_WEJSCIA przycięte_wysokie_stare_kolizje=" + wycieteZewn);
        int off = 0;
        if (physics) { var e = cut; e.Expand(new Vector3(0f, .14f, 0f)); foreach (var c in physics.GetComponentsInChildren<Collider>(true)) if (c.enabled && e.Contains(c.bounds.min) && e.Contains(c.bounds.max)) { c.enabled = false; off++; } }
        var guma = Plain("sala bokserska posadzka guma czarna", new Color(.06f, .06f, .065f), .2f);
        var panel = Plain("sala bokserska panele", new Color(.08f, .08f, .09f), .25f);
        var czerwien = Plain("czerwień BRAXHORN (pas)", new Color(.58f, .05f, .05f), .45f);
        var grafit = Plain("sala bokserska ściany grafit", new Color(.22f, .22f, .24f), .2f);
        var strop = Plain("sala bokserska strop czarny", new Color(.07f, .07f, .08f), .05f);
        var plotno = Plain("płótno ringu", new Color(.78f, .80f, .82f), .2f);
        var fartuch = Plain("fartuch ringu czarny", new Color(.03f, .03f, .035f), .45f);
        var dab = Plain("ławka dąb", new Color(.45f, .33f, .22f), .4f);
        Top(guma, X0, X1, Z0, Z1, yF);
        // ściany: panele do 1.2 m, czerwony pas, grafit; przerwy na drzwi x 84–88 (do 3.1 m) w ścianach z 49 i z 79
        foreach (var (a0, a1, z, nz) in new[] { (X0, 84f, Z0, 1f), (88f, X1, Z0, 1f), (X0, 84f, Z1, -1f), (88f, X1, Z1, -1f) })
        {
            float b0 = nz > 0 ? z : z - .03f, b1 = b0 + .03f;
            BoxMinMax(panel, a0, a1, yF, 1.2f, b0, b1, false); BoxMinMax(czerwien, a0, a1, 1.2f, 1.35f, b0, b1, false); BoxMinMax(grafit, a0, a1, 1.35f, yC, b0, b1, false);
        }
        foreach (float z in new[] { Z0, Z1 - .03f }) BoxMinMax(grafit, 84f, 88f, 3.1f, yC, z, z + .03f, false);
        foreach (float x in new[] { X0, X1 - .03f })
        { BoxMinMax(panel, x, x + .03f, yF, 1.2f, Z0, Z1, false); BoxMinMax(czerwien, x, x + .03f, 1.2f, 1.35f, Z0, Z1, false); BoxMinMax(grafit, x, x + .03f, 1.35f, yC, Z0, Z1, false); }
        BoxMinMax(strop, X0, X1, yC, yC + .04f, Z0, Z1, false, true);
        // RING 6 × 6 m (x 83–89, z 60–66), podest 0.9 m, liny w kwadracie 5.4 m, słupki narożne z obiciem, wejście od z 66 (schodki)
        const float rx = 86f, rz = 63f, rh = .9f, h = 3f, hl = 2.7f;
        BoxMinMax(fartuch, rx - h, rx + h, 0, rh - .02f, rz - h, rz + h, true);
        Top(plotno, rx - h + .02f, rx + h - .02f, rz - h + .02f, rz + h - .02f, rh);
        BoxMinMax(czerwien, rx - h - .005f, rx + h + .005f, rh - .14f, rh - .1f, rz - h - .005f, rz + h + .005f, false);
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
        {
            var c = new Vector3(rx + sx * hl, rh, rz + sz * hl);
            Cylinder(Steel, c, .08f, 1.62f, 14, true, false);
            Cylinder(sx * sz > 0 ? czerwien : Plain("obicie narożnika niebieskie", new Color(.08f, .16f, .45f), .45f), c + Vector3.up * .32f, .15f, 1.3f, 16, true, false);
            Collider(c + Vector3.up * .8f, new Vector3(.3f, 1.6f, .3f));
        }
        foreach (float ex in new[] { -.85f, .85f }) { var c = new Vector3(rx + ex, rh, rz + hl); Cylinder(Steel, c, .05f, 1.5f, 12, true, false); Collider(c + Vector3.up * .75f, new Vector3(.12f, 1.5f, .12f)); }
        for (int k = 0; k < 3; k++) BoxMinMax(fartuch, rx - 1f, rx + 1f, 0, rh * (3 - k) / 3f - .02f, rz + h + k * .4f, rz + h + (k + 1) * .4f, true);   // schodki 0.3 m
        var biala = Plain("lina ringu biała (gruba, połysk)", new Color(.94f, .94f, .92f), .55f);
        var wys = new[] { rh + .4f, rh + .72f, rh + 1.04f, rh + 1.36f };
        void Bok(Vector3 a0, Vector3 b0, Vector3 nz, string id, float ka, float kb)
        {
            var os = (b0 - a0).normalized; var a = a0 + os * ka; var b = b0 - os * kb;
            var go = new GameObject("Liny ringu (sala bokserska) — " + id); go.transform.SetParent(Group(group), false);
            go.AddComponent<MeshFilter>(); var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = biala; mr.shadowCastingMode = ShadowCastingMode.Off;
            var l = go.AddComponent<BraxLina>(); l.a = a; l.b = b; l.naZewnatrz = nz; l.wysokosci = wys; l.promien = .024f; l.maksUgiecie = .2f; l.Zbuduj();
            var mesh = go.GetComponent<MeshFilter>().sharedMesh; if (mesh) UnityEditor.AssetDatabase.CreateAsset(mesh, Out + "/liny_boks_" + id + ".asset");
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, 0);
            var c = (a0 + b0) * .5f + nz * .28f; float len = Vector3.Distance(a0, b0);
            var col = new GameObject("kolizja lin " + id); col.transform.SetParent(colliders, false); col.transform.position = new Vector3(c.x, rh + .85f, c.z);
            col.AddComponent<BoxCollider>().size = Mathf.Abs(nz.x) > .5f ? new Vector3(.18f, 1.7f, len) : new Vector3(len, 1.7f, .18f);
        }
        Vector3 P(float x, float z) => new Vector3(rx + x, 0, rz + z);
        Bok(P(-hl, -hl), P(hl, -hl), Vector3.back, "S", .1f, .1f); Bok(P(-hl, -hl), P(-hl, hl), Vector3.left, "W", .1f, .1f); Bok(P(hl, -hl), P(hl, hl), Vector3.right, "E", .1f, .1f);
        Bok(P(-hl, hl), P(-.85f, hl), Vector3.forward, "N1", .1f, .06f); Bok(P(.85f, hl), P(hl, hl), Vector3.forward, "N2", .06f, .1f);
        // WORKI: 6 pod belką (x 71.0, z 53…68) na dwóch słupach, mata pod workami
        int worki = 0; const float wx = 71.0f, wy = 4.44f;
        BoxMinMax(Plain("mata gumowa siłowni", new Color(.05f, .05f, .055f), .15f), wx - 1.6f, wx + 1.6f, yF, yF + .012f, 51.6f, 69.4f, false);
        foreach (float z in new[] { 51.3f, 69.7f }) BoxMinMax(Steel, wx - .1f, wx + .1f, 0, wy + .3f, z - .1f, z + .1f, true);
        BoxMinMax(Steel, wx - .11f, wx + .11f, wy, wy + .03f, 51.2f, 69.8f, false, true);
        BoxMinMax(Steel, wx - .025f, wx + .025f, wy + .03f, wy + .27f, 51.2f, 69.8f, false);
        BoxMinMax(Steel, wx - .11f, wx + .11f, wy + .27f, wy + .3f, 51.2f, 69.8f, false, true);
        foreach (float z in new[] { 53f, 56f, 59f, 62f, 65f, 68f }) if (Worek(new Vector3(wx, wy, z))) worki++;
        // GRUSZKA (speed bag) na platformie przy ścianie wschodniej, LUSTRO ścienne obok (zwykłe), mata do walki z cieniem
        var deska = Pbr("sklejka (skrzynie plyo)", "wood_floor", 1.2f, new Color(1f, .93f, .82f), .35f);
        float gxw = X1 - .03f, gzc = 72.5f;
        BoxMinMax(Steel, gxw - .6f, gxw, 2.38f, 2.42f, gzc - .05f, gzc + .05f, false);
        Cylinder(deska, new Vector3(gxw - .55f, 2.42f, gzc), .5f, .06f, 24, true, false);
        Cylinder(Chrom, new Vector3(gxw - .55f, 2.32f, gzc), .015f, .1f, 8, false, false);
        var gruszka = new MB(1); gruszka.Lathe(0, new Vector3(gxw - .55f, 2.3f, gzc), new[] { new Vector2(0, 0), new Vector2(.06f, -.03f), new Vector2(.11f, -.14f), new Vector2(.1f, -.24f), new Vector2(.05f, -.3f), new Vector2(0, -.31f) }, 16, .3f, Quaternion.identity);
        Wstaw(gruszka, "gruszka (speed bag)", new[] { worekMats != null && worekMats.Length > 0 ? worekMats[0] : czerwien }, Group(group), Vector3.zero);
        var lustro = Plain("lustro (srebrzone szkło)", new Color(.78f, .8f, .81f), .97f, 1f);
        BoxMinMax(lustro, X1 - .045f, X1 - .03f, .4f, 2.4f, 63.5f, 70.5f, false);
        BoxMinMax(Frame, X1 - .05f, X1 - .03f, 2.4f, 2.44f, 63.45f, 70.55f, false, true); BoxMinMax(Frame, X1 - .05f, X1 - .03f, .36f, .4f, 63.45f, 70.55f, false);
        Top(Plain("mata gumowa siłowni", new Color(.05f, .05f, .055f), .15f), X1 - 3.2f, X1 - .05f, 63.5f, 70.5f, yF + .01f);
        // ŁAWKI pod ścianą (zachodnia za workami — nie; przy wejściu z ulicy po bokach, poza strefą drzwi x 84–88)
        foreach (var (b0, b1) in new[] { (70.0f, 76.0f), (92.0f, 98.0f) })
        {
            LawkaX(dab, b0, b1, Z1 - .3f, .47f);   // runda 5: deski z fazą i przerwami, rama w kształcie U, stopki, poprzeczka (bez klocka)
        }
        Sign("grafik", new Vector3(80.0f, 2.2f, Z1 - .05f), 3.2f, .8f, Vector3.back);
        Sign("sala_bokserska", new Vector3(86f, 4.3f, Z0 + .05f), 6.4f, 1.2f, Vector3.forward);
        // SZATNIA (x 96…105.8, z 54.5…61) + PRYSZNICE (x 96…100.9, z 49.2…54.4) + WC (x 101…105.8): ściany 3.0 m, strop pokoi
        const float sx0 = 96f, sz1 = 61f, pz = 54.45f, px = 100.95f, CY = 3.0f;
        var tynk = InteriorPaint; var plytki = Pbr("płytki łazienki (gres 30 cm)", "large_grey_tiles", .6f, new Color(.96f, .96f, .98f), .6f);
        var posadzka = Pbr("posadzka szatni (gres)", "large_grey_tiles", 1.2f, new Color(.7f, .72f, .74f), .5f);
        var szafkaM = Plain("szafki szatni (grafit lakier)", new Color(.12f, .125f, .13f), .55f, .2f);
        var porcelana = Plain("porcelana sanitarna", new Color(.95f, .95f, .94f), .85f);
        void Drzwi(float x0, float x1, float z0, float z1, bool wzdluzX)   // otwór 1.2 × 2.65 m z ościeżnicą
        {
            if (wzdluzX) { foreach (float x in new[] { x0 - .07f, x1 }) BoxMinMax(Frame, x, x + .07f, 0, 2.72f, z0 - .03f, z1 + .03f, false); BoxMinMax(Frame, x0 - .07f, x1 + .07f, 2.65f, 2.72f, z0 - .03f, z1 + .03f, false, true); BoxMinMax(tynk, x0, x1, 2.72f, CY, z0, z1, false, true); }
            else { foreach (float z in new[] { z0 - .07f, z1 }) BoxMinMax(Frame, x0 - .03f, x1 + .03f, 0, 2.72f, z, z + .07f, false); BoxMinMax(Frame, x0 - .03f, x1 + .03f, 2.65f, 2.72f, z0 - .07f, z1 + .07f, false, true); BoxMinMax(tynk, x0, x1, 2.72f, CY, z0, z1, false, true); }
        }
        // ściana zachodnia szatni i łazienek (x sx0) z drzwiami do szatni (z 56.15…57.35); ściana północna (z sz1)
        BoxMinMax(tynk, sx0 - .06f, sx0 + .06f, 0, CY, Z0, 56.15f, true); BoxMinMax(tynk, sx0 - .06f, sx0 + .06f, 0, CY, 57.35f, sz1 + .06f, true); Drzwi(sx0 - .06f, sx0 + .06f, 56.15f, 57.35f, false);
        BoxMinMax(tynk, sx0, X1, 0, CY, sz1 - .06f, sz1 + .06f, true);
        BoxMinMax(tynk, sx0, X1, CY, CY + .05f, Z0, sz1 + .06f, false, true);                                          // strop pokoi
        Sign("szatnia", new Vector3(sx0 - .1f, 3.25f, 56.75f), 1.5f, .37f, Vector3.left);
        // przegroda z pz: drzwi do pryszniców (x 97.85…99.05) i do WC (x 102.8…104.0); ściana x px między nimi
        BoxMinMax(tynk, sx0, 97.85f, 0, CY, pz - .05f, pz + .05f, true); BoxMinMax(tynk, 99.05f, 102.8f, 0, CY, pz - .05f, pz + .05f, true); BoxMinMax(tynk, 104.0f, X1, 0, CY, pz - .05f, pz + .05f, true);
        Drzwi(97.85f, 99.05f, pz - .05f, pz + .05f, true); Drzwi(102.8f, 104.0f, pz - .05f, pz + .05f, true);
        BoxMinMax(tynk, px - .05f, px + .05f, 0, CY, Z0, pz - .05f, true);
        Sign("prysznice", new Vector3(98.45f, 2.86f, pz + .06f), 1.1f, .26f, Vector3.forward); Sign("wc", new Vector3(103.4f, 2.86f, pz + .06f), .5f, .26f, Vector3.forward);
        Top(posadzka, sx0 + .06f, X1, pz + .05f, sz1 - .06f, yF + .006f);
        int szafek = 0;
        var drzwiSzafki = Plain("drzwi szafki (lakier)", new Color(.16f, .28f, .5f), .45f, .3f);   // runda 5: szafki z drzwiami, fugą, żaluzją, numerem, zamkiem, okapem
        for (float z = 57.2f; z < sz1 - .6f; z += .42f) { Szafka(szafkaM, drzwiSzafki, new Vector3(X1 - .53f, 0, z), Vector3.left, .4f, .5f, 2.0f, szafek + 1); szafek++; }
        Collider(new Vector3(X1 - .28f, 1f, (57.0f + sz1 - .6f) * .5f), new Vector3(.52f, 2f, sz1 - .6f - 57.0f));
        for (float x = 99.0f; x < X1 - .6f; x += .42f) { Szafka(szafkaM, drzwiSzafki, new Vector3(x, 0, sz1 - .56f), Vector3.back, .4f, .5f, 2.0f, szafek + 1); szafek++; }
        Collider(new Vector3((98.8f + X1 - .6f) * .5f, 1f, sz1 - .31f), new Vector3(X1 - .6f - 98.8f, 2f, .52f));
        LawkaX(dab, 100.0f, 103.6f, 58.3f, .47f);
        // prysznice: płytki, 2 deszczownice, szklana ścianka; WC: miska, umywalka, lustro
        Top(plytki, sx0 + .06f, px - .05f, Z0, pz - .05f, yF + .01f); Top(plytki, px + .05f, X1, Z0, pz - .05f, yF + .01f);
        BoxMinMax(plytki, sx0 + .06f, px - .05f, 0, 2.2f, Z0 + .03f, Z0 + .045f, false); BoxMinMax(plytki, px + .05f, X1, 0, 1.6f, Z0 + .03f, Z0 + .045f, false);
        float sm = (sx0 + px) * .5f;
        BoxMinMax(GlassClear, sm - .01f, sm + .01f, .05f, 2.0f, Z0 + .05f, Z0 + 1.4f, false); Collider(new Vector3(sm, 1f, Z0 + .72f), new Vector3(.04f, 2f, 1.35f));
        foreach (float x in new[] { (sx0 + sm) * .5f, (sm + px) * .5f })
        { Tube(Chrom, new Vector3(x, 2.25f, Z0 + .05f), new Vector3(x, 2.25f, Z0 + .45f), .012f, 8); Krazek(Chrom, new Vector3(x, 2.22f, Z0 + .45f), Vector3.up, .13f, .02f, 20); BoxMinMax(Chrom, x - .1f, x + .1f, 1.05f, 1.15f, Z0 + .045f, Z0 + .08f, false); }
        float tx = 104.9f;
        Cylinder(porcelana, new Vector3(tx, 0, Z0 + .42f), .2f, .38f, 20, true, false);
        BoxMinMax(porcelana, tx - .2f, tx + .2f, .38f, .41f, Z0 + .22f, Z0 + .64f, false, true);
        BoxMinMax(porcelana, tx - .26f, tx + .26f, .7f, 1.1f, Z0 + .045f, Z0 + .2f, false, true);
        Collider(new Vector3(tx, .4f, Z0 + .35f), new Vector3(.45f, .8f, .65f));
        BoxMinMax(porcelana, px + .05f, px + .5f, .78f, .9f, 51.7f, 52.3f, true, true);
        BoxMinMax(lustro, px + .05f, px + .062f, 1.2f, 1.9f, 51.7f, 52.3f, false);
        // lampy: hala (pod stropem 6.8 — panele), szatnia i łazienki; ForceVertex
        foreach (var p in new[] { new Vector2(86f, 63f), new Vector2(71f, 56f), new Vector2(71f, 66f), new Vector2(80f, 73f), new Vector2(92f, 73f), new Vector2(102f, 67f), new Vector2(86f, 53f) })
        { BoxMinMax(LampGlass, p.x - .45f, p.x + .45f, yC - .04f, yC, p.y - .45f, p.y + .45f, false, true); PointLamp(new Vector3(p.x, 5.2f, p.y), 11f, 1.0f, new Color(1f, .96f, .9f)); }
        foreach (var p in new[] { new Vector2(101f, 57.8f), new Vector2(98.4f, 51.8f), new Vector2(103.4f, 51.8f) })
        { BoxMinMax(LampGlass, p.x - .3f, p.x + .3f, CY - .03f, CY, p.y - .3f, p.y + .3f, false, true); PointLamp(new Vector3(p.x, CY - .4f, p.y), 6f, .9f, new Color(1f, .96f, .9f)); }
        var zw = new GameObject("zasięg kamery w sali bokserskiej (BraxClaudeZasiegWnetrza)"); zw.transform.SetParent(Group(group), false);
        var zc = zw.AddComponent<BraxClaudeZasiegWnetrza>(); zc.zasieg = 60f; zc.sale = new[] { MinMax(X0, X1, -.5f, 6.9f, Z0, Z1) };
        rep.notes.Add("sala bokserska: stare wnętrze wycięte (kolizji wyłączonych: " + off + "), ring 6 × 6 m z 4 grubymi linami (BraxLina), " + worki + " worków premium (BraxWorek), gruszka, lustro, ławki, tablica GRAFIK, szatnia ("
                      + szafek + " szafek, ławka) + 2 prysznice + WC; drzwi 1.2 × 2.65 m, 2.5 m wolnego za drzwiami");
    }
}
