using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// DWORZEC R1 — WINDY PRZY KŁADCE I WC (11.11.2026, właściciel 18:12–18:13: „Drzwi do WC to trzeba zrobić… Winda przy kładce…
// zrób, dokończ to”). Zasada „wszystko funkcjonalne jak w życiu”: windy jeżdżą (BraxWinda — przywołanie przyciskiem, panel
// w kabinie, drzwi kabiny i przystanku rozsuwają się razem), drzwi WC na zawiasach otwierają się przy podejściu i same
// zamykają (BraxCityDoor), w łazienkach kabiny z drzwiami, miski, umywalki, lustra, pisuary. Szyby wind przeszklone: czarny
// szkielet z ryglami na przystankach, kabina i drzwi w czarnych ramach (zasada szkła 11.11). Skala ×K: kabina 1.6 × 1.4 m,
// drzwi windy 1.1 × 2.1 m, drzwi WC 1.0 × 2.0 m, umywalka 0.85 m, miska 0.40 m, kabina WC 0.9 × 1.5 m (wszystko × 1.294).
internal static partial class BraxMiastoDworzec
{
    // ---------------- WC (skrzydło AUTOBUSY, za pylonem wschodnim hali)
    internal static readonly (float a, float b)[] WcPrzejscia = { (-138.05f, -136.75f), (-135.55f, -134.25f) };   // Panie, Panowie
    internal const float WcDrzwiH = 2.59f, WcX0 = -5.2f, WcX1 = 1.2f, WcZ0 = -139.3f, WcZ1 = -133.0f, WcSciana = -136.15f;

    // ---------------- windy
    const float KabW = 1.6f * 1.2944f, KabD = 1.4f * 1.2944f, WDrzW = 1.1f * 1.2944f, WDrzH = 2.1f * 1.2944f;
    /// <summary>Połowy obrysu szybu (szkielet zewn.): wzdłuż drzwi / w głąb. Szyby obu wind mają drzwi na ±x.</summary>
    // Szyb szerszy od kabiny: skrzydła drzwi (otwierane centralnie) chowają się w obrysie szybu (jak ściana przystanku w życiu).
    internal const float SzybWew = WDrzW - .02f > KabW * .5f + .1f ? WDrzW - .02f : KabW * .5f + .1f;
    internal const float SzybPolW = SzybWew + .08f, SzybPolD = KabD * .5f + .18f;
    internal static Rect SzybRect(Vector2 c) => Rect.MinMaxRect(c.x - SzybPolD, c.y - SzybPolW, c.x + SzybPolD, c.y + SzybPolW);
    internal static Rect[] OtworySzybow => new[] { SzybRect(WindaA), SzybRect(WindaB) };
    static Material przyciskOn, przyciskOff, przyciskSel, ceramika, plytkiSciana, plytkiPodloga, lustroM, chrom;

    static void MaterialyWindWC()
    {
        przyciskOn = BraxMiastoA.Swieci("Dworzec: przycisk windy (aktywny)", new Color(.85f, .92f, 1f), new Color(.5f, .8f, 1.6f));
        przyciskSel = BraxMiastoA.Swieci("Dworzec: przycisk windy (wybrany)", new Color(.6f, 1f, .6f), new Color(.4f, 1.8f, .5f));
        przyciskOff = T("przycisk windy (nieaktywny — stal)", .5f, new Color(.45f, .46f, .48f), .4f, .9f, "metal_plate_02", "metal_plate");
        ceramika = T("ceramika sanitarna (biała, szkliwiona)", 1.0f, new Color(1f, 1f, .99f), .12f, 0f, "white_stucco", "white_plaster_02");
        plytkiSciana = T("płytki ścienne WC (marmur jasny, 30 cm)", 1.2f, new Color(1f, 1f, .99f), .3f, 0f, "floor_tiles_02", "floor_tiles_06");
        plytkiPodloga = T("płytki podłogowe WC (gres)", 1.2f, new Color(.8f, .8f, .8f), .55f, 0f, "floor_tiles_06", "square_tiles");
        // armatura: chrom polerowany — czysty PBR metalu gładkiego (zasada 11.11 „zero klocków”: metal gładki bez fotoskanu)
        chrom = BraxCityMaterials.Surface("Dworzec: armatura — chrom polerowany", new Color(.88f, .89f, .9f), .08f, 1f);
        // lustro z prawdziwym odbiciem (zasada 10.11 „lustro to lustro”): shader Braxhorn/Lustro + BraxLustro (jak na faweli)
        var shL = Shader.Find("Braxhorn/Lustro");
        if (shL) { lustroM = new Material(shL) { name = "Dworzec: lustro (tafla z odbiciem)" }; Directory.CreateDirectory(BraxRegion.Out + "/Dworzec"); BraxZapisAssetu.Utworz(lustroM, BraxRegion.Out + "/Dworzec/lustro.mat"); }
        else { lustroM = BraxCityMaterials.Surface("Dworzec: lustro", new Color(.78f, .82f, .84f), .05f, .92f); Debug.LogWarning("DO POPRAWKI: BRAXHORN_DWORZEC_R1 brak shadera Braxhorn/Lustro — lustra WC bez odbicia płaskiego"); }
    }

    // ============================================================================ WINDA
    /// <summary>Winda przeszklona: środek szybu (x, z) na poziomie najniższego przystanku, zwrot drzwi n (±x / ±z), przystanki.</summary>
    static void Winda(string nazwa, Vector3 srodek, Vector3 n, float[] przystanki, string[] nazwy, float stronaSlupka = 1f)
    {
        if (przyciskOn == null) MaterialyWindWC();
        var Wt = new GameObject("Winda " + nazwa).transform; Wt.SetParent(root, false); Wt.position = srodek; Wt.rotation = Quaternion.LookRotation(n, Vector3.up);
        float iw = KabW * .5f, id = KabD * .5f, hw = SzybWew, hd = id + .1f, t = .08f, y0 = srodek.y;
        int ns = przystanki.Length; float top = przystanki[ns - 1] + WDrzH + .9f;
        Vector3 Pw(float x, float y, float z) => Wt.TransformPoint(new Vector3(x, y - y0, z));
        void BL(Material m, float x0, float ya, float z0, float x1, float yb, float z1) { var a = Pw(x0, ya, z0); var b = Pw(x1, yb, z1); S(m).Box(Vector3.Min(a, b), Vector3.Max(a, b)); }
        void KL(string nn, float x0, float ya, float z0, float x1, float yb, float z1) { var a = Pw(x0, ya, z0); var b = Pw(x1, yb, z1); BraxMiastoA.Kol(Wt, "Winda " + nazwa + ": " + nn, Vector3.Min(a, b), Vector3.Max(a, b)); }
        Grupa("windy", 220f);
        // szkielet: słupki w narożach, rygle na przystankach i pod dachem, dach
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            BL(czarnyAlu, sx < 0 ? -hw - t : hw, y0 - .3f, sz < 0 ? -hd - t : hd, sx < 0 ? -hw : hw + t, top, sz < 0 ? -hd : hd + t);
        foreach (float yp in przystanki)
        {
            BL(czarnyAlu, -hw - t, yp - .12f, -hd - t, hw + t, yp, -hd); BL(czarnyAlu, -hw - t, yp - .12f, hd, hw + t, yp, hd + t);
            BL(czarnyAlu, -hw - t, yp - .12f, -hd - t, -hw, yp, hd + t); BL(czarnyAlu, hw, yp - .12f, -hd - t, hw + t, yp, hd + t);   // boczne na całą głębokość (krawędź szkła boku w ryglu)
        }
        BL(granat, -hw - .25f, top, -hd - .25f, hw + .25f, top + .35f, hd + .25f);
        // szkło: tył i boki na całej wysokości, przód między drzwiami (tafle w czarnych ramach — rygle co przystanek)
        BL(szklo, -hw, y0, -hd - t * .5f - .01f, hw, top, -hd - t * .5f + .01f);
        BL(szklo, -hw - t * .5f - .01f, y0, -hd, -hw - t * .5f + .01f, top, hd); BL(szklo, hw + t * .5f - .01f, y0, -hd, hw + t * .5f + .01f, top, hd);
        KL("szyb tył", -hw - t, y0, -hd - t, hw + t, top, -hd); KL("szyb L", -hw - t, y0, -hd, -hw, top, hd); KL("szyb P", hw, y0, -hd, hw + t, top, hd);
        var pL = new Transform[ns]; var pP = new Transform[ns]; var wez = new Vector3[ns];
        for (int i = 0; i < ns; i++)
        {
            float ya = przystanki[i], yb = i + 1 < ns ? przystanki[i + 1] : top;
            // przód: boki drzwi i nadproże (szkło w czarnej ramie), skrzydła przystanku
            float yg = i + 1 < ns ? yb - .12f : yb;   // szkło frontu kończy się pod ryglem następnego przystanku (nie przechodzi przez jego wierzch)
            BL(szklo, -hw, ya, hd + t * .5f - .01f, -WDrzW * .5f - .08f, yg, hd + t * .5f + .01f);
            BL(szklo, WDrzW * .5f + .08f, ya, hd + t * .5f - .01f, hw, yg, hd + t * .5f + .01f);
            BL(szklo, -WDrzW * .5f - .08f, ya + WDrzH + .12f, hd + t * .5f - .01f, WDrzW * .5f + .08f, yg, hd + t * .5f + .01f);
            BL(czarnyAlu, -WDrzW * .5f - .08f, ya, hd, -WDrzW * .5f, ya + WDrzH + .12f, hd + t);
            BL(czarnyAlu, WDrzW * .5f, ya, hd, WDrzW * .5f + .08f, ya + WDrzH + .12f, hd + t);
            BL(czarnyAlu, -WDrzW * .5f - .08f, ya + WDrzH, hd, WDrzW * .5f + .08f, ya + WDrzH + .12f, hd + t);
            KL("przód L p." + i, -hw - t, ya, hd, -WDrzW * .5f, yb, hd + t); KL("przód P p." + i, WDrzW * .5f, ya, hd, hw + t, yb, hd + t);
            KL("nadproże p." + i, -WDrzW * .5f, ya + WDrzH, hd, WDrzW * .5f, yb, hd + t);
            // słupek przywołania (stal) obok szybu, poza drogą skrzydeł, przycisk 2.5 cm przed licem
            float px = stronaSlupka * (hw + t + .3f), pz = hd + t + .25f;
            { var a0 = Pw(px - .07f, ya, pz - .06f); var a1 = Pw(px + .07f, ya + 1.25f, pz + .06f); S(stal).Poducha(Vector3.Min(a0, a1), Vector3.Max(a0, a1), .02f, 0f, 4); }   // słupek zaokrąglony
            KL("słupek przywołania p." + i, px - .07f, ya, pz - .06f, px + .07f, ya + 1.25f, pz + .06f);
            var hold = new GameObject("Drzwi przystanku " + i).transform; hold.SetParent(Wt, false); hold.localPosition = new Vector3(0f, ya - y0, hd + t + .03f);
            pL[i] = Skrzydlo(hold, "skrzydło L", -WDrzW * .25f); pP[i] = Skrzydlo(hold, "skrzydło P", WDrzW * .25f);
            var kb = new UlSiatka(); kb.Rura(new Vector3(px, ya - y0 + 1.05f, pz + .06f), new Vector3(px, ya - y0 + 1.05f, pz + .085f), .035f, 14);
            var gk = BraxMiastoA.Obiekt(Wt, "Winda " + nazwa + ": przycisk przywołania p." + i, kb, przyciskOn); GameObjectUtility.SetStaticEditorFlags(gk, 0);
            wez[i] = Pw(px, ya, hd + 1.3f);
        }
        // kabina: podłoga, tył ze stali, boki szklane w czarnych ramach, sufit z panelem LED, poręcz, panel przycisków
        var kab = new GameObject("Kabina windy " + nazwa).transform; kab.SetParent(Wt, false); kab.localPosition = Vector3.zero; kab.localRotation = Quaternion.identity;
        float ih = 2.4f * 1.2944f;
        var sP = new UlSiatka(); sP.Box(new Vector3(-iw, -.15f, -id), new Vector3(iw, 0f, id));
        // ściany kabiny: okleina dębowa (tył i front), sufit stalowy z oprawą LED
        var sD = new UlSiatka(); sD.Box(new Vector3(-iw, 0f, -id - .05f), new Vector3(iw, ih, -id));
        sD.Box(new Vector3(-iw, 0f, id - .03f), new Vector3(-WDrzW * .5f, ih, id)); sD.Box(new Vector3(WDrzW * .5f, 0f, id - .03f), new Vector3(iw, ih, id)); sD.Box(new Vector3(-WDrzW * .5f, WDrzH, id - .03f), new Vector3(WDrzW * .5f, ih, id));
        var sS = new UlSiatka(); sS.Box(new Vector3(-iw - .05f, ih, -id - .05f), new Vector3(iw + .05f, ih + .1f, id));
        // boki: szkło między słupkiem tylnym a ścianką frontu (bez wchodzenia w słupek), dolna płyta i pochwyt ramy w tym samym zakresie
        var sG = new UlSiatka(); sG.Box(new Vector3(-iw - .02f, .92f, -id + .05f), new Vector3(-iw, ih, id - .03f)); sG.Box(new Vector3(iw, .92f, -id + .05f), new Vector3(iw + .02f, ih, id - .03f));
        var sR = new UlSiatka(); foreach (float sx in new[] { -1f, 1f }) { sR.Box(new Vector3(sx * iw - .05f, 0f, -id), new Vector3(sx * iw + .05f, ih, -id + .05f)); sR.Box(new Vector3(sx * iw - .04f, 0f, -id + .05f), new Vector3(sx * iw + .04f, .86f, id - .03f)); sR.Box(new Vector3(sx * iw - .04f, .86f, -id + .05f), new Vector3(sx * iw + .04f, .92f, id - .03f)); }
        var sH = new UlSiatka(); foreach (float sx in new[] { -1f, 1f }) sH.Rura(new Vector3(sx * (iw - .06f), .95f * 1.2944f, -id + .2f), new Vector3(sx * (iw - .06f), .95f * 1.2944f, id - .35f), .025f, 10);
        var sL = new UlSiatka(); sL.Box(new Vector3(-iw + .25f, ih - .035f, -id + .25f), new Vector3(iw - .25f, ih - .005f, id - .25f));   // lico 3.5 cm pod sufitem
        var sK = new UlSiatka(); sK.Poducha(new Vector3(WDrzW * .5f + .05f, .95f, id - .06f), new Vector3(iw - .05f, 1.65f, id - .03f), .008f, 0f, 3);   // kaseta w obrębie ścianki (nie w szkle boku)
        foreach (var (s, m, nm) in new[] { (sP, posadzka, "podłoga"), (sD, dab, "ściany (okleina dębowa)"), (sS, stal, "sufit stalowy"), (sG, szklo, "boki szklane"), (sR, czarnyAlu, "ramy"), (sH, stal, "poręcze"), (sL, led, "panel LED"), (sK, czarnyAlu, "kaseta panelu") })
        { var g = BraxMiastoA.Obiekt(kab, "Kabina " + nazwa + ": " + nm, s, m, m != led && m != szklo); GameObjectUtility.SetStaticEditorFlags(g, 0); }
        var kc = kab.gameObject;
        void KK(string nm, Vector3 c, Vector3 r) { var b = new GameObject("Kabina: kolizja " + nm); b.transform.SetParent(kab, false); b.transform.localPosition = c; b.AddComponent<BoxCollider>().size = r; GameObjectUtility.SetStaticEditorFlags(b, 0); }
        KK("podłoga", new Vector3(0f, -.075f, 0f), new Vector3(KabW, .15f, KabD)); KK("tył", new Vector3(0f, ih * .5f, -id - .03f), new Vector3(KabW, ih, .06f));
        KK("L", new Vector3(-iw - .03f, ih * .5f, 0f), new Vector3(.06f, ih, KabD)); KK("P", new Vector3(iw + .03f, ih * .5f, 0f), new Vector3(.06f, ih, KabD));
        var dk = new GameObject("Drzwi kabiny").transform; dk.SetParent(kab, false); dk.localPosition = new Vector3(0f, 0f, id + .03f);   // przed licem frontu kabiny (bez wchodzenia w ściankę)
        var kL = Skrzydlo(dk, "skrzydło kabiny L", -WDrzW * .25f); var kP = Skrzydlo(dk, "skrzydło kabiny P", WDrzW * .25f);
        var przyciski = new Renderer[ns];
        for (int i = 0; i < ns; i++)
        {
            var b = new UlSiatka(); var c = new Vector3(WDrzW * .5f + .2f, 1.05f + i * .16f, id - .06f);
            b.Rura(c, c - new Vector3(0f, 0f, .025f), .045f, 12);   // 2.5 cm przed kasetą
            var go = BraxMiastoA.Obiekt(kab, "Przycisk " + i + " " + nazwy[i], b, przyciskOn); GameObjectUtility.SetStaticEditorFlags(go, 0);
            przyciski[i] = go.GetComponent<Renderer>();
        }
        GameObjectUtility.SetStaticEditorFlags(kc, 0);
        var wd = Wt.gameObject.AddComponent<BraxWinda>();
        wd.nazwa = nazwa; wd.kabina = kab; wd.kabinaL = kL; wd.kabinaP = kP;
        wd.pietraY = przystanki; wd.pietraNazwy = nazwy; wd.uzytkowe = new bool[ns]; for (int i = 0; i < ns; i++) wd.uzytkowe[i] = true;
        wd.przystanekL = pL; wd.przystanekP = pP; wd.wezwanie = wez;
        wd.szyb = new Vector2(srodek.x, srodek.z);
        wd.wnetrze = Mathf.Abs(n.x) > .5f ? new Vector2(id, iw) : new Vector2(iw, id);
        wd.przesuw = WDrzW * .5f - .04f; wd.start = 0;
        wd.przyciski = przyciski; wd.przyciskAktywny = przyciskOn; wd.przyciskWybrany = przyciskSel; wd.przyciskNieaktywny = przyciskOff;
        Zajmij(srodek.x - hw - 1.5f, srodek.z - hw - 1.5f, srodek.x + hw + 1.5f, srodek.z + hw + 1.5f);
        Opis("winda " + nazwa + ": przystanki " + string.Join(" / ", nazwy) + " (y " + string.Join(", ", System.Array.ConvertAll(przystanki, v => v.ToString("F2"))) + "), kabina " + KabW.ToString("F2") + " × " + KabD.ToString("F2") + " m, drzwi " + WDrzW.ToString("F2") + " × " + WDrzH.ToString("F2"));
    }

    /// <summary>Skrzydło drzwi windy (szkło w czarnej ramie) — przesuwane przez BraxWinda w lokalnej osi X uchwytu.</summary>
    static Transform Skrzydlo(Transform uchwyt, string n, float off)
    {
        var t = new GameObject(n).transform; t.SetParent(uchwyt, false); t.localPosition = new Vector3(off, 0f, 0f);
        float w = WDrzW * .25f;
        var r = new UlSiatka(); r.Box(new Vector3(-w, 0f, -.025f), new Vector3(-w + .05f, WDrzH, .025f)); r.Box(new Vector3(w - .05f, 0f, -.025f), new Vector3(w, WDrzH, .025f));
        r.Box(new Vector3(-w + .05f, 0f, -.025f), new Vector3(w - .05f, .1f, .025f)); r.Box(new Vector3(-w + .05f, WDrzH - .07f, -.025f), new Vector3(w - .05f, WDrzH, .025f));
        var g = new UlSiatka(); g.Box(new Vector3(-w + .05f, .1f, -.006f), new Vector3(w - .05f, WDrzH - .07f, .006f));
        var a = BraxMiastoA.Obiekt(t, n + " (rama)", r, czarnyAlu); var b = BraxMiastoA.Obiekt(t, n + " (szkło)", g, szklo);
        foreach (var go in new[] { a, b, t.gameObject }) GameObjectUtility.SetStaticEditorFlags(go, 0);
        var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0f, WDrzH * .5f, 0f); col.size = new Vector3(WDrzW * .5f, WDrzH, .06f);
        return t;
    }

    // ============================================================================ WC
    static void WC()
    {
        if (przyciskOn == null) MaterialyWindWC();
        Grupa("wnętrze: WC", 70f);
        float yc = Y(6.05f);   // sufit skrzydła (ściany WC do niego); w łazienkach sufit podwieszany na 3.0 m
        float ys = Y(3.0f);
        // posadzka, okładzina ścian, ściany działowe (płytki), przejścia przez pylon i ścianę skrzydła
        // posadzka WC w poziomie posadzki hali i skrzydła (skrzydło ma w tym miejscu wycięcie — bez płyty na płycie)
        foreach (var (a, b) in new[] { (WcZ0, WcSciana), (WcSciana + .15f, WcZ1) }) Plyta(plytkiPodloga, WcX0, a, WcX1, b, Y0, .3f);
        foreach (var (a, b) in WcPrzejscia) Plyta(plytkiPodloga, HX1, a, WcX0, b, Y0, .3f);
        B(plytkiSciana, WcX0, Y0, WcSciana, WcX1, yc, WcSciana + .15f);                 // ściana między Paniami a Panami
        B(plytkiSciana, WcX1, Y0, WcZ0, WcX1 + .15f, yc, WcZ1 + .15f);                 // ściana od poczekalni (wschód)
        B(plytkiSciana, WcX0, Y0, WcZ1, WcX1, yc, WcZ1 + .15f);                        // ściana od poczekalni (północ)
        Kol("WC: ściana działowa", V(WcX0, Y0, WcSciana), V(WcX1, yc, WcSciana + .15f));
        Kol("WC: ściana wsch.", V(WcX1, Y0, WcZ0), V(WcX1 + .15f, yc, WcZ1 + .15f)); Kol("WC: ściana płn.", V(WcX0, Y0, WcZ1), V(WcX1, yc, WcZ1 + .15f));
        // okładzina płytkami w pomieszczeniach (cienka warstwa 2 cm na ścianie tylnej skrzydła i zachodniej — bez nakładania płaszczyzn)
        B(plytkiSciana, WcX0, Y0, WcZ0, WcX1, ys, WcZ0 + .02f);
        foreach (var (z0, z1) in new[] { (WcZ0 + .02f, WcPrzejscia[0].a), (WcPrzejscia[0].b, WcSciana), (WcSciana + .15f, WcPrzejscia[1].a), (WcPrzejscia[1].b, WcZ1) })
            B(plytkiSciana, WcX0, Y0, z0, WcX0 + .02f, ys, z1);
        foreach (var (a, b) in WcPrzejscia) B(plytkiSciana, WcX0, Y(WcDrzwiH + .15f), a, WcX0 + .02f, ys, b);   // nad przejściami
        // sufit podwieszany (tynk, gładki) — od lica do lica ścian
        B(tynk, WcX0, ys, WcZ0, WcX1, ys + .15f, WcSciana, 8); B(tynk, WcX0, ys, WcSciana + .15f, WcX1, ys + .15f, WcZ1, 8);
        // drzwi na zawiasach od strony hali (otwierają się przy podejściu, same się zamykają) + piktogramy
        string[] nazwyWc = { "PANIE", "PANOWIE" };
        for (int k = 0; k < 2; k++)
        {
            var (a, b) = WcPrzejscia[k]; float zc = (a + b) * .5f, dw = b - a;
            // ościeżnica: słupki na licu pylonu (nie w kamieniu), nadproże do spodu przejścia
            B(czarnyAlu, HX1 - .06f, Y0, a - .08f, HX1, Y(WcDrzwiH + .15f), a);
            B(czarnyAlu, HX1 - .06f, Y0, b, HX1, Y(WcDrzwiH + .15f), b + .08f);
            B(czarnyAlu, HX1 - .06f, Y(WcDrzwiH), a, HX1 + .02f, Y(WcDrzwiH + .15f), b);
            DrzwiZawiasy("WC " + nazwyWc[k], V(HX1 + .04f, Y0, zc), Vector3.left, dw - .04f, WcDrzwiH - .02f, dab);
            // tabliczka na ścianie obok drzwi: koło (Panie) / trójkąt (Panowie), napis pod spodem
            Grupa("napisy", 120f);
            float zt2 = k == 0 ? a - .55f : b + .55f;
            S(niebieski).Poducha(V(HX1 - .04f, Y(1.55f), zt2 - .3f), V(HX1, Y(2.15f), zt2 + .3f), .04f, 0f, 4);
            // piktogram bryłą 2.5 cm (tył na tablicy, lico 2.5 cm przed nią): koło / trójkąt
            if (k == 0) S(litery).Rura(V(HX1 - .04f, Y(1.85f), zt2), V(HX1 - .065f, Y(1.85f), zt2), .2f, 32);
            else
            {
                Vector3 t0 = V(HX1 - .04f, Y(2.02f), zt2 - .2f), t1 = V(HX1 - .04f, Y(2.02f), zt2 + .2f), t2 = V(HX1 - .04f, Y(1.68f), zt2), dl = Vector3.left * .025f;
                var sl = S(litery);
                sl.Tri(t0 + dl, t1 + dl, t2 + dl, Vector3.left);
                sl.Quad(t0, t1, t1 + dl, t0 + dl, Vector3.up);
                sl.Quad(t1, t2, t2 + dl, t1 + dl, new Vector3(0f, -.2f, .34f));
                sl.Quad(t2, t0, t0 + dl, t2 + dl, new Vector3(0f, -.2f, -.34f));
            }
            BraxLiteryBebas.Napis(nazwyWc[k], V(HX1, Y(1.3f), zt2), Vector3.left, .16f, .025f, litery, grafit, .06f, .002f);
            Grupa("wnętrze: WC", 70f);
        }
        // wyposażenie obu łazienek: blat z umywalkami i lustro, suszarka; kabiny z drzwiami (miska wisząca, przycisk spłuczki);
        // u Panów dwa pisuary
        for (int k = 0; k < 2; k++)
        {
            float za = k == 0 ? WcZ0 + .02f : WcSciana + .15f, zb = k == 0 ? WcSciana : WcZ1;
            float zs = za, sg = 1f;         // umywalki przy ścianie południowej obu łazienek (wejście od zachodu wolne); sg: od ściany w głąb
            float bx0 = WcX0 + 1.6f, bx1 = WcX0 + 4.0f, gl = .55f * K, hb = .85f * K;
            S(marmurBlat).Poducha(V(bx0, Y(hb - .05f), Mathf.Min(zs, zs + sg * gl)), V(bx1, Y(hb), Mathf.Max(zs, zs + sg * gl)), .02f, 0f, 4);
            S(dab).Poducha(V(bx0, Y(hb - .32f), zs + .005f), V(bx1, Y(hb - .05f), zs + sg * (gl - .05f)), .02f, 0f, 4);   // 5 mm od ściany (tył nie leży przy tyle okładziny)
            foreach (float xu in new[] { bx0 + .6f, bx1 - .6f })
            {
                Umywalka(V(xu, Y(hb), zs + sg * gl * .5f));      // misa nablatowa z odpływem (właściciel 19:53: „czy są jak talerze”)
                S(stal).Rura(V(xu, Y(hb), zs + sg * .08f), V(xu, Y(hb + .28f), zs + sg * .08f), .018f, 8);
                S(stal).Rura(V(xu, Y(hb + .28f), zs + sg * .08f), V(xu, Y(hb + .28f), zs + sg * .22f), .016f, 8);
                S(stal).Rura(V(xu, Y(hb + .28f), zs + sg * .21f), V(xu, Y(hb + .245f), zs + sg * .21f), .011f, 10);   // wylewka z perlatorem
            }
            // lustro w czarnej ramie: rama obwodowa 3 cm, tafla w środku cofnięta 1.5 cm (bez tafli na ramie)
            // lustro: tafla z odbiciem 30 mm przed ścianą, czarna rama wokół tafli (nie na niej), lico ramy 1.5 cm przed taflą
            B(czarnyAlu, bx0 - .03f, Y(hb + .35f), zs, bx0, Y(hb + 1.35f), zs + .045f); B(czarnyAlu, bx1, Y(hb + .35f), zs, bx1 + .03f, Y(hb + 1.35f), zs + .045f);
            B(czarnyAlu, bx0, Y(hb + .35f), zs, bx1, Y(hb + .38f), zs + .045f); B(czarnyAlu, bx0, Y(hb + 1.32f), zs, bx1, Y(hb + 1.35f), zs + .045f);
            Lustro(nazwyWc[k], V((bx0 + bx1) * .5f, Y(hb + .85f), zs + .03f), Vector3.forward, bx1 - bx0, .94f);
            S(stal).Poducha(V(bx0 - .8f, Y(1.25f), zs), V(bx0 - .45f, Y(1.6f), zs + .2f), .03f, 0f, 4);   // suszarka do rąk
            Kol("WC: blat z umywalkami", V(bx0, Y0, Mathf.Min(zs, zs + sg * gl)), V(bx1, Y(hb), Mathf.Max(zs, zs + sg * gl)));
            // dwie kabiny przy ścianie wschodniej (głębokość 1.5 × K), ścianki z okleiny dębowej na czarnych nóżkach
            float kx0 = WcX1 - 1.5f * K, kd = (zb - za) / 2f;
            for (int c = 0; c < 2; c++)
            {
                float cz0 = za + c * kd, cz1 = cz0 + kd;
                if (c == 1) S(dab).Poducha(V(kx0, Y(.15f), cz0 - .02f), V(WcX1, Y(2.4f), cz0 + .02f), .01f, 0f, 3);
                // front kabiny: panele po bokach drzwi, nadproże, drzwi na zawiasach 0.62 × K
                float dwk = .62f * K, pan = (kd - dwk) * .5f;
                // ścianki i nadproże z okleiny z promieniem krawędzi 1 cm, nóżki stalowe okrągłe (zasada 11.11 „zero klocków”)
                S(dab).Poducha(V(kx0 - .02f, Y(.15f), cz0), V(kx0 + .02f, Y(2.4f), cz0 + pan), .01f, 0f, 3); S(dab).Poducha(V(kx0 - .02f, Y(.15f), cz1 - pan), V(kx0 + .02f, Y(2.4f), cz1), .01f, 0f, 3);
                S(dab).Poducha(V(kx0 - .02f, Y(2.2f), cz0 + pan), V(kx0 + .02f, Y(2.4f), cz1 - pan), .01f, 0f, 3);
                S(czarnyAlu).Walec(V(kx0, Y0, cz0 + .05f), .025f, .15f, 12); S(czarnyAlu).Walec(V(kx0, Y0, cz1 - .05f), .025f, .15f, 12);
                Kol("WC: front kabiny", V(kx0 - .02f, Y0, cz0), V(kx0 + .02f, Y(2.4f), cz0 + pan)); Kol("WC: front kabiny", V(kx0 - .02f, Y0, cz1 - pan), V(kx0 + .02f, Y(2.4f), cz1));
                DrzwiZawiasy("WC " + nazwyWc[k] + " kabina " + (c + 1), V(kx0, Y(.15f), (cz0 + cz1) * .5f), Vector3.left, dwk - .02f, 2.05f, dab);
                // miska wisząca przy ścianie wschodniej, przycisk spłuczki, uchwyt na papier
                float mz = (cz0 + cz1) * .5f;
                S(ceramika).Poducha(V(WcX1 - .55f * K, Y(.25f), mz - .19f * K), V(WcX1 - .02f, Y(.4f * K), mz + .19f * K), .12f, .01f, 6);
                S(ceramika).Poducha(V(WcX1 - .56f * K, Y(.4f * K), mz - .19f * K), V(WcX1 - .04f, Y(.4f * K + .03f), mz + .19f * K), .1f, 0f, 5);
                S(stal).Poducha(V(WcX1 - .025f, Y(1.0f * K), mz - .12f), V(WcX1, Y(1.0f * K + .16f), mz + .12f), .006f, 0f, 3);   // przycisk spłuczki 2.5 cm
                S(stal).Rura(V(WcX1 - .3f, Y(.75f), cz0 + .02f), V(WcX1 - .3f, Y(.75f), cz0 + .12f), .06f, 12);
                Kol("WC: miska", V(WcX1 - .55f * K, Y0, mz - .19f * K), V(WcX1, Y(.4f * K), mz + .19f * K));
                Kol("WC: ścianka kabiny", V(kx0, Y0, cz0), V(WcX1, Y(2.4f), cz0 + .04f));
            }
            Kol("WC: ścianka kabiny", V(kx0, Y0, zb - .04f), V(WcX1, Y(2.4f), zb));
            if (k == 1)
            {
                // dwa pisuary wiszące (rozstaw 1.0 m) i przegroda między nimi (okleina dębowa jak kabiny, uchwyty chrom)
                foreach (float xp in new[] { WcX0 + 2.15f, WcX0 + 3.15f })
                {
                    Pisuar(new Vector3(xp, Y0, zb), Vector3.right, Vector3.back);
                    Kol("WC: pisuar", V(xp - .18f * K, Y(.46f * K), zb - .34f * K), V(xp + .18f * K, Y(1.12f * K), zb));
                }
                float xg = WcX0 + 2.65f, gy0 = Y(.6f * K), gy1 = Y(1.5f * K), gz = .45f * K;
                S(dab).Poducha(V(xg - .0125f, gy0, zb - .03f - gz), V(xg + .0125f, gy1, zb - .03f), .01f, 0f, 3);
                foreach (float yu in new[] { gy0 + .12f, gy1 - .12f })
                    S(chrom).Rura(V(xg, yu, zb + .02f), V(xg, yu, zb - .06f), .022f, 16);   // obejma ścienna (płyta przechodzi przez nią)
                Kol("WC: przegroda pisuarów", V(xg - .0125f, gy0, zb - .03f - gz), V(xg + .0125f, gy1, zb));
            }
            B(led, WcX0 + .8f, ys - .035f, (za + zb) * .5f - .3f, WcX1 - 2.2f, ys - .005f, (za + zb) * .5f + .3f);   // oprawa pod sufitem podwieszanym
        }
        Zajmij(WcX0, WcZ0, WcX1 + .2f, WcZ1 + .2f);
        Opis("WC: Panie (2 kabiny, 2 umywalki) i Panowie (2 kabiny, 2 pisuary, 2 umywalki), drzwi na zawiasach od hali i w kabinach (otwierają się przy podejściu, same się zamykają)");
    }

    // PISUAR WISZĄCY (11.11, właściciel 19:39 „te takie dwie… pisuary, czy co to jest?”) — ceramika z kodu jak w życiu, wymiary
    // rzeczywiste × K: szer. 36 cm, wys. 66 cm (spód 46 cm, warga misy 58 cm nad posadzką), głębokość 33.5 cm przy wardze.
    // Jedna gładka powłoka: obrys przy ścianie (superelipsa) → boki wychodzą prostopadle ze ściany → zaokrąglona warga (r 1.8 cm)
    // → misa (krzywa Béziera od wargi: w głąb ku ścianie, potem w dół) zbiega lejem do odpływu w NAJNIŻSZYM punkcie — w dnie misy,
    // 7 cm PONIŻEJ dolnej wargi, poziomo (właściciel 19:52–19:59: „odpływ na dole… nie ma szans, żeby woda spłynęła”; woda
    // z każdego miejsca misy spływa w dół do odpływu) → studzienka 2 cm → sitko poziome (pierścienie chrom/grafit,
    // równo w jednej płaszczyźnie — bez nakładania). Nad pisuarem zawór spłukujący (chrom): rura z kołpaka, korpus, dźwignia,
    // przyłącze do ściany z rozetą. Bez tylnej ściany — korpus przylega do płytek (zasada zero migotania).
    // o: punkt na licu ściany na poziomie posadzki pod środkiem pisuaru; ex: wzdłuż ściany; en: od ściany do pomieszczenia.
    static void Pisuar(Vector3 o, Vector3 ex, Vector3 en)
    {
        const int M = 72;
        const float pO = 2.4f, pR = 2.6f, rl = .018f, rd = .024f, yD = .525f, zD = .175f;   // odpływ: Ø 4.8 cm × K, dno misy 52.5 cm
        var Bc = new Vector2(0f, .77f);             // środek wargi (kierunek zaokrąglenia do wnętrza)
        float Se(float c, float p) => Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), 2f / p);
        Vector2 Obrys(float th) { float c = Mathf.Cos(th), s = Mathf.Sin(th); return new Vector2(.18f * (1f - .06f * (1f - s)) * Se(c, pO), .79f + .33f * Se(s, pO)); }
        Vector2 Warga(float th) { float c = Mathf.Cos(th), s = Mathf.Sin(th); return new Vector2(.14f * (1f - .08f * (1f - s)) * Se(c, pR), .77f + .19f * Se(s, pR)); }
        float Zr(float th) => .25f - .085f * Mathf.Sin(th);
        Vector3 W(Vector2 q, float z) => o + (ex * q.x + Vector3.up * q.y + en * z) * K;
        // segmenty profilu: 0 boki (ściana → szczyt wargi), 1 warga, 2 misa, 3 studzienka odpływu
        Vector3 P(int seg, float th, float t)
        {
            Vector2 O = Obrys(th), R = Warga(th); float zr = Zr(th); var dIn = (Bc - R).normalized; var Rin = R + dIn * rl;
            switch (seg)
            {
                case 0: { float f = t * Mathf.PI * .5f; return W(O + (R - O) * (1f - Mathf.Cos(f)), -.004f + (zr + .004f) * Mathf.Sin(f)); }
                case 1: { float a = t * Mathf.PI * .5f; return W(R + dIn * (rl * Mathf.Sin(a)), zr - rl + rl * Mathf.Cos(a)); }
                case 2:
                {
                    // Bézier: od wargi w głąb (styczna −en jak koniec wargi), potem pionowo w dół do okręgu odpływu (styczna −y)
                    float sn = Mathf.Sin(th), z0 = zr - rl;
                    var B0 = new Vector3(Rin.x, Rin.y, z0);
                    var B1 = new Vector3(Rin.x, Rin.y, Mathf.Lerp(z0, .06f, .55f + .25f * sn));
                    var B3 = new Vector3(rd * Mathf.Cos(th), yD, zD - rd * sn);
                    var B2 = B3 + Vector3.up * ((Rin.y - yD) * .6f);
                    float u = 1f - t; var q = u * u * u * B0 + 3f * u * u * t * B1 + 3f * u * t * t * B2 + t * t * t * B3;
                    return W(new Vector2(q.x, q.y), q.z);
                }
                default: { float sn = Mathf.Sin(th); return W(new Vector2(rd * Mathf.Cos(th), Mathf.Lerp(yD, yD - .02f, t)), zD - rd * sn); }
            }
        }
        int[] podzial = { 10, 5, 16, 1 };
        var s = S(ceramika);
        // orientacja: na szczycie wargi (dół misy) normalna patrzy od ściany (+en)
        float Zn(int seg, float th, float t)
        {
            const float e = 1e-3f;
            var a = (P(seg, th + e, t) - P(seg, th - e, t)) / e;
            var b = (P(seg, th, Mathf.Min(1f, t + e)) - P(seg, th, Mathf.Max(0f, t - e))) / e;
            return Vector3.Dot(Vector3.Cross(a, b), en);
        }
        float znak = Zn(0, -Mathf.PI * .5f, 1f) >= 0f ? 1f : -1f;
        for (int seg = 0; seg < 4; seg++)
        {
            int nT = podzial[seg], k0 = s.v.Count; float dl = 0f;
            for (int j = 0; j <= M; j++)
            {
                float th = -Mathf.PI * .5f + j * Mathf.PI * 2f / M; dl = 0f;
                for (int q = 0; q <= nT; q++)
                {
                    float t = (float)q / nT; var p = P(seg, th, t);
                    if (q > 0) dl += Vector3.Distance(p, P(seg, th, (float)(q - 1) / nT));
                    const float e = 1e-3f;
                    // pochodne podzielone przez krok (różnice rzędu 1e-3 m: iloczyn wektorowy poniżej progu Normalize w Unity)
                    var a = (P(seg, th + e, t) - P(seg, th - e, t)) / e;
                    var b = (P(seg, th, Mathf.Min(1f, t + e)) - P(seg, th, Mathf.Max(0f, t - e))) / e;
                    var nn = Vector3.Cross(a, b) * znak; nn = nn.sqrMagnitude > 1e-8f ? nn.normalized : en;
                    var tg = a.sqrMagnitude > 1e-8f ? a.normalized : ex;
                    s.v.Add(p); s.n.Add(nn); s.t.Add(new Vector4(tg.x, tg.y, tg.z, 1f)); s.c.Add(s.kolor);
                    s.uv.Add(new Vector2(j * 1.4f * K / M, dl + seg * .3f));
                }
            }
            for (int j = 0; j < M; j++)
                for (int q = 0; q < nT; q++)
                {
                    int a0 = k0 + j * (nT + 1) + q, a1 = a0 + 1, b0 = a0 + nT + 1, b1 = b0 + 1;
                    var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                    if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                    else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
                }
        }
        // sitko odpływu poziome w dnie misy: koło dzielone na pierścienie chrom / grafit (jedna płaszczyzna, bez nakładania)
        var cS = W(new Vector2(0f, yD - .02f), zD);
        float[] pas = { 0f, .22f, .34f, .54f, .66f, 1f };
        for (int b = 0; b < pas.Length - 1; b++)
        {
            var sm = S(b % 2 == 0 ? chrom : grafit);
            if (b == 0) sm.Kolo(cS, rd * K * pas[1], M); else sm.Pierscien(cS, rd * K * pas[b], rd * K * pas[b + 1], 0f, 360f, M);
        }
        // zawór spłukujący nad pisuarem (chrom): rura z kołpaka (wchodzi 2.5 cm w ceramikę), korpus, dźwignia, przyłącze w ścianę z rozetą
        var ch = S(chrom); float zz = .07f;
        ch.Rura(W(new Vector2(0f, 1.08f), zz), W(new Vector2(0f, 1.33f), zz), .016f * K, 14);
        ch.Rura(W(new Vector2(0f, 1.30f), zz), W(new Vector2(0f, 1.40f), zz), .028f * K, 18);
        ch.PoduchaOkragla(W(new Vector2(0f, 1.40f), zz), .028f * K, .018f * K, .008f * K, 0f, 18);
        ch.Rura(W(new Vector2(.01f, 1.345f), zz), W(new Vector2(.10f, 1.345f), zz), .007f * K, 10);           // dźwignia (koniec w gałce)
        ch.Rura(W(new Vector2(.095f, 1.345f), zz), W(new Vector2(.125f, 1.345f), zz), .012f * K, 12);         // gałka
        ch.Rura(W(new Vector2(0f, 1.37f), zz), W(new Vector2(0f, 1.37f), -.02f), .016f * K, 14);
        ch.Rura(W(new Vector2(0f, 1.37f), -.025f), W(new Vector2(0f, 1.37f), .016f), .034f * K, 20);          // rozeta 2 cm przed płytkami
    }

    // UMYWALKA NABLATOWA (11.11, właściciel 19:53: „czy mają dziurę… odpływ, czy są jak talerze”) — misa ceramiczna toczona z kodu:
    // Ø 38 cm × K, wys. 13 cm × K; ścianka zewnętrzna od blatu (podstawa Ø 0.62) wybrzuszona do krawędzi, zaokrąglona krawędź
    // (r 1.2 cm), wnętrze schodzi do płaskiego dna 4.5 cm nad blatem, odpływ Ø 5.6 cm × K z sitkiem 2.5 cm nad blatem (pierścienie
    // chrom / grafit w jednej płaszczyźnie). Spód nie jest budowany — misa stoi na blacie (zasada zero migotania).
    static void Umywalka(Vector3 p)
    {
        float R = .19f * K, H = .13f * K, rr = .012f * K, rb = R * .62f, rd = .022f * K, yIn = .045f, ySit = .025f;
        const int seg = 48;
        var segmenty = new List<List<Vector2>>();
        List<Vector2> L(int n, System.Func<float, Vector2> f) { var l = new List<Vector2>(); for (int i = 0; i <= n; i++) l.Add(f((float)i / n)); return l; }
        segmenty.Add(L(10, u => { float a = u * Mathf.PI * .5f; return new Vector2(rb + (R - rb) * Mathf.Sin(a), (H - rr) * (1f - Mathf.Cos(a))); }));          // ścianka zewn.
        segmenty.Add(L(8, u => { float b = u * Mathf.PI; return new Vector2(R - rr + rr * Mathf.Cos(b), H - rr + rr * Mathf.Sin(b)); }));                   // krawędź
        segmenty.Add(L(12, u => { float a = (1f - u) * Mathf.PI * .5f; return new Vector2(rd + (R - 2f * rr - rd) * Mathf.Sin(a), yIn + (H - rr - yIn) * (1f - Mathf.Cos(a))); }));   // wnętrze i dno
        segmenty.Add(L(1, u => new Vector2(rd, Mathf.Lerp(yIn, ySit, u))));                                                                                  // studzienka
        var s = S(ceramika);
        foreach (var pr in segmenty) Toczony(s, p, pr, seg);
        float[] pas = { 0f, .22f, .34f, .54f, .66f, 1f };
        for (int b = 0; b < pas.Length - 1; b++)
        {
            var sm = S(b % 2 == 0 ? chrom : grafit);
            if (b == 0) sm.Kolo(p + Vector3.up * ySit, rd * pas[1], seg);
            else sm.Pierscien(p + Vector3.up * ySit, rd * pas[b], rd * pas[b + 1], 0f, 360f, seg);
        }
    }

    /// <summary>Bryła toczona wokół pionu przez p: profil (r, y) — normalna po prawej stronie kierunku profilu (ścianka zewn. w górę
    /// = na zewnątrz, wnętrze w dół = do osi); UV w metrach (obwód × długość profilu).</summary>
    static void Toczony(UlSiatka s, Vector3 p, List<Vector2> pr, int seg)
    {
        int P = pr.Count, k0 = s.v.Count; var dl = new float[P];
        for (int j = 1; j < P; j++) dl[j] = dl[j - 1] + Vector2.Distance(pr[j - 1], pr[j]);
        for (int i = 0; i <= seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg; var kier = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); var tan = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
            for (int j = 0; j < P; j++)
            {
                var d = pr[Mathf.Min(P - 1, j + 1)] - pr[Mathf.Max(0, j - 1)]; var n2 = new Vector2(d.y, -d.x).normalized;
                s.v.Add(p + kier * pr[j].x + Vector3.up * pr[j].y); s.n.Add((kier * n2.x + Vector3.up * n2.y).normalized);
                s.t.Add(new Vector4(tan.x, tan.y, tan.z, 1f)); s.c.Add(s.kolor); s.uv.Add(new Vector2(a * Mathf.Max(.05f, pr[j].x), dl[j]));
            }
        }
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < P - 1; j++)
            {
                int a0 = k0 + i * P + j, a1 = a0 + 1, b0 = a0 + P, b1 = b0 + 1;
                var nsr = s.n[a0] + s.n[a1] + s.n[b0] + s.n[b1];
                if (Vector3.Dot(Vector3.Cross(s.v[a1] - s.v[a0], s.v[b1] - s.v[a0]), nsr) >= 0f) s.i.AddRange(new[] { a0, a1, b1, a0, b1, b0 });
                else s.i.AddRange(new[] { a0, b1, a1, a0, b0, b1 });
            }
    }

    /// <summary>Tafla lustra (prostokąt w × h, środek c, lico w stronę n) — osobny obiekt z BraxLustro (patrzy w −Z obiektu).</summary>
    static void Lustro(string nazwa, Vector3 c, Vector3 n, float w, float h)
    {
        var g = new GameObject("Dworzec: lustro — " + nazwa).transform; g.SetParent(root, false); g.position = c; g.rotation = Quaternion.LookRotation(-n, Vector3.up);
        var s = new UlSiatka(); s.Quad(new Vector3(-w * .5f, -h * .5f, 0f), new Vector3(w * .5f, -h * .5f, 0f), new Vector3(w * .5f, h * .5f, 0f), new Vector3(-w * .5f, h * .5f, 0f), Vector3.back);
        var t = BraxMiastoA.Obiekt(g, "Dworzec: lustro — " + nazwa + " (tafla)", s, lustroM);
        t.AddComponent<BraxLustro>();
    }

    /// <summary>Drzwi na zawiasach (BraxCityDoor): środek dołu c, zwrot lica n (do strony, z której się podchodzi), szerokość w, wysokość h.</summary>
    static void DrzwiZawiasy(string nazwa, Vector3 c, Vector3 n, float w, float h, Material m)
    {
        var e = new GameObject("Drzwi: " + nazwa).transform; e.SetParent(root, false); e.position = c; e.rotation = Quaternion.LookRotation(n, Vector3.up);
        var z = new GameObject("zawias").transform; z.SetParent(e, false); z.localPosition = new Vector3(-w * .5f, 0f, 0f);
        var s = new UlSiatka(); s.Box(new Vector3(.01f, .01f, -.022f), new Vector3(w - .01f, h - .01f, .022f));
        var k = new UlSiatka(); k.Box(new Vector3(w - .14f, 1.0f * 1.2944f - .02f, .022f), new Vector3(w - .06f, 1.0f * 1.2944f + .02f, .07f)); k.Box(new Vector3(w - .14f, 1.0f * 1.2944f - .02f, -.07f), new Vector3(w - .06f, 1.0f * 1.2944f + .02f, -.022f));
        var g1 = BraxMiastoA.Obiekt(z, "Drzwi " + nazwa + ": skrzydło", s, m, true); var g2 = BraxMiastoA.Obiekt(z, "Drzwi " + nazwa + ": klamka", k, stal);
        foreach (var t in z.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        var col = z.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(w * .5f, h * .5f, 0f); col.size = new Vector3(w, h, .06f);
        var d = e.gameObject.AddComponent<BraxCityDoor>(); d.id = "R1_DWORZEC_" + BraxUliceKit.Bezpieczna(nazwa); d.label = nazwa; d.leaves = new[] { z }; d.sides = new[] { 1f };
        GameObjectUtility.SetStaticEditorFlags(e.gameObject, 0);
    }
}
