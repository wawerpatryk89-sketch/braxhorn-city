using System.Linq;
using UnityEditor;
using UnityEngine;

// DRZWI LOKALU PREMIUM (MIASTO R1, 11.11.2026 — właściciel 19:33 „drzwi poprawiać, bo są straszne te drzwi do lokali. Nie
// otwierają się w ogóle”, 19:43 „te drzwi wszędzie masz do wymiany, słabe, z przerwami… Drzwi mają być konkretne”, 19:45 „jak są
// drzwi otwierane normalnie, to mają być klamki… każdy detal”). Zastępują drzwi „PUBLIC WORKING ENTRANCE” z przebudowy miasta
// (skrzydła-klocki 0.94 m, szczelina 12 cm między skrzydłami i 15 cm otwartego pasa nad nadprożem).
// Budowa w otworze elewacji (szer. W, wys. H od posadzki, ościeże w głąb muru):
//  • ościeżnica z czarnego aluminium 7 × 10 cm wpuszczona 5–15 cm w ościeże, słupki do ościeży (bez szczelin), nadproże i ślemię;
//  • naświetle stałe (szkło) nad ślemieniem do nadproża — otwór zamknięty w całości;
//  • dwa skrzydła na zawiasach: rama 6.5 cm (cokół 12 cm), szkło bezpieczne w środku, szczeliny 3 mm (jak w życiu);
//  • na każdym skrzydle pochwyt: pionowa rura stalowa szczotkowana Ø 3.2 cm × 1.0 m × K na dwóch wspornikach, z obu stron;
//  • krawędzie zaokrąglone (zasada „zero klocków” — profile r 4 mm), kolizje skrzydeł, BraxCityDoor (otwierają się przy podejściu,
//    same się zamykają). Skrzydła i zawiasy bez flag statycznych (ruchome).
// Układ lokalny: x wzdłuż elewacji, y w górę, z na zewnątrz (normalna elewacji); początek: środek otworu na posadzce w licu.
internal static class BraxDrzwiLokalu
{
    static float K => BraxClaudeOsiedle.K;
    static Material czarny, szklo, stal, dab, grafit;

    // 11.11 (właściciel 20:28: „nie wszystkie drzwi muszą być takie same… chociaż trzy modele drzwi… mogą być też nie szklane,
    // normalne drzwi”): 0 = szklane w czarnym aluminium z pochwytami, 1 = drewniane płycinowe pełne (dąb) z klamką i szyldem
    // ze stali (12.11: bez mosiądzu), 2 = stalowe grafitowe z wąskim pionowym przeszkleniem i klamką ze stali. Wszystkie: zawiasy widoczne (3 na skrzydło).
    // 20:34 („oszklone drzwi… takie balkonowe bardziej wyglądają… w sklepach mogą być sklepowe, otwierane na bok… do każdego lokalu
    // podobieraj takie, jakie potrzebuje”): 3 = automatyczne przesuwne (skrzydła odjeżdżają na boki w kieszeń muru, napęd w ślemieniu,
    // czujnik ruchu, pas oznakowania na szybie) — markety, apteki, poczta, hotel, fitness. Szklane rozwierane tylko jako zapas.
    public const int Szklane = 0, Drewniane = 1, Stalowe = 2, Przesuwne = 3;
    public static int ModelDla(string typ) => typ switch
    {
        "market" or "apteka" or "poczta" or "hotel" or "fitness" => Przesuwne,
        "kawiarnia" or "bistro" or "pizzeria" or "bar" or "ksiegarnia" or "piekarnia" or "cukiernia" or "kwiaty" or "fryzjer" or "warzywniak" => Drewniane,
        "kantor" or "warsztat" or "bank" or "optyk" => Stalowe,
        _ => Szklane,
    };

    static void Materialy()
    {
        if (czarny) return;
        czarny = BraxUliceKit.Tex("drzwi lokalu: czarne aluminium anodowane", 1.2f, new Color(.075f, .075f, .085f), new Color(.06f, .06f, .07f), .45f, "blue_metal_plate", "metal_plate");
        szklo = BraxUliceKit.SzkloPrzezroczyste;
        stal = BraxCityMaterials.Surface("drzwi lokalu: pochwyt stal szczotkowana", new Color(.8f, .81f, .82f), .25f, 1f);
        dab = BraxUliceKit.Tex("drzwi lokalu: dąb (okleina jak w WC dworca)", 1.2f, null, new Color(.6f, .45f, .3f), .5f, "oak_veneer_01", "teak_veneer");   // 20:30 „z tego drewna, co w łazience”
        grafit = BraxUliceKit.Tex("drzwi lokalu: stal malowana antracyt (mat)", 1.2f, new Color(.42f, .44f, .47f), new Color(.2f, .21f, .23f), .62f, "blue_metal_plate", "metal_plate");
    }

    /// <summary>Drzwi dwuskrzydłowe w otworze: p = środek otworu na posadzce w licu elewacji, n = normalna elewacji (na zewnątrz),
    /// W × H = otwór, gleb = głębokość ościeża (≥ 0.2 m). Zwraca komponent drzwi (id, label).</summary>
    public static BraxCityDoor Podwojne(Transform rodzic, string id, string label, Vector3 p, Vector3 n, float W, float H, float gleb = .3f, float progY = 0f, int model = Szklane)
    {
        Materialy();
        if (model == Przesuwne) { Przesuwane(rodzic, id, label, p, n, W, H, progY); return null; }
        var mRamy = model == Drewniane ? dab : model == Stalowe ? grafit : czarny;
        n.y = 0f; n.Normalize();
        var root = new GameObject("Drzwi lokalu: " + label).transform; root.SetParent(rodzic, false);
        root.position = p + Vector3.up * progY; root.rotation = Quaternion.LookRotation(n, Vector3.up); H -= progY;
        const float pr = .07f, zF0 = -.15f, zF1 = -.05f, zL0 = -.125f, zL1 = -.075f, sz = .003f;
        float hS = Mathf.Min(2.85f, H - .2f);           // wysokość skrzydeł (2.2 m × K), ślemię nad nimi
        float x0 = -W * .5f, x1 = W * .5f;
        // ościeżnica + ślemię + naświetle (stałe, w jednej siatce)
        var ram = new UlSiatka(); var szk = new UlSiatka();
        ram.Poducha(new Vector3(x0, -.02f, zF0), new Vector3(x0 + pr, H, zF1), .004f, 0f, 3);   // słupki 2 cm w posadzce
        ram.Poducha(new Vector3(x1 - pr, -.02f, zF0), new Vector3(x1, H, zF1), .004f, 0f, 3);
        ram.Poducha(new Vector3(x0 + pr, H - pr, zF0), new Vector3(x1 - pr, H, zF1), .004f, 0f, 3);
        ram.Poducha(new Vector3(x0 + pr, hS + sz, zF0), new Vector3(x1 - pr, hS + sz + pr, zF1), .004f, 0f, 3);
        if (H - pr - (hS + sz + pr) > .03f)
            foreach (var nn in new[] { Vector3.forward, Vector3.back })   // naświetle dwustronne (te same punkty, przeciwne normalne)
                szk.Quad(new Vector3(x0 + pr, hS + sz + pr, -.1f), new Vector3(x1 - pr, hS + sz + pr, -.1f), new Vector3(x1 - pr, H - pr, -.1f), new Vector3(x0 + pr, H - pr, -.1f), nn);
        BraxMiastoA.Obiekt(root, "Drzwi " + label + ": ościeżnica i ślemię", ram, mRamy, true);
        if (szk.Wierzcholki > 0) BraxMiastoA.Obiekt(root, "Drzwi " + label + ": naświetle", szk, szklo);
        foreach (float xs in new[] { x0 + pr * .5f, x1 - pr * .5f })
        {
            var c = new GameObject("kolizja: słupek ościeżnicy").AddComponent<BoxCollider>(); c.transform.SetParent(root, false);
            c.center = new Vector3(xs, H * .5f, (zF0 + zF1) * .5f); c.size = new Vector3(pr, H, zF1 - zF0);
        }
        // skrzydła
        float wL = (W - 2f * pr - 3f * sz) * .5f;
        var zawiasy = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float hx = i == 0 ? x0 + pr + sz : x1 - pr - sz;          // oś zawiasu
            float dir = i == 0 ? 1f : -1f;                             // skrzydło od zawiasu w stronę środka
            var z = new GameObject((i == 0 ? "Lewe" : "Prawe") + " skrzydło (zawias)").transform; z.SetParent(root, false);
            // oś zawiasu w płaszczyźnie skrzydła (z −0.1): po otwarciu na 90° skrzydło staje wzdłuż ościeża, nie wchodzi w mur
            z.localPosition = new Vector3(hx, 0f, -.1f);
            Vector3 L(float a, float y, float zz) => new Vector3(dir * a, y, zz + .1f);
            var rama = new UlSiatka(); var tafla = new UlSiatka(); var uchwyt = new UlSiatka(); var plyc = new UlSiatka();
            float pS = .065f, pD = .12f, y0 = .02f, y1 = hS - sz;
            void Pr(UlSiatka u, float a0, float a1, float b0, float b1, float z0 = zL0, float z1 = zL1, float r = .004f)
            { var A = L(a0, b0, z0); var B = L(a1, b1, z1); u.Poducha(Vector3.Min(A, B), Vector3.Max(A, B), r, 0f, 3); }
            void Szyba(float a0, float a1, float b0, float b1)
            {   // tafla dwustronna 1.2 cm w osi skrzydła (rama 5 cm obejmuje szybę — bez wspólnej płaszczyzny)
                Vector3 g0 = L(a0, b0, -.1f), g1 = L(a1, b0, -.1f), g2 = L(a1, b1, -.1f), g3 = L(a0, b1, -.1f);
                tafla.Quad(g0, g1, g2, g3, Vector3.forward); tafla.Quad(g0, g1, g2, g3, Vector3.back);
            }
            if (model == Drewniane)
            {   // DREWNIANE PŁYCINOWE (20:30 „płycinowe… z tego drewna, co w łazience… żeby nie były proste”): ramiak 11 cm, cokół 22 cm,
                // dwa ślemiona — trzy płyciny (górna wysoka, środkowa, dolna); płycina 2 cm cieńsza z obu stron, na niej pole podniesione
                // (fazowane, r 6 mm) wysunięte do lica ramiaka i listwa profilowana na obwodzie — wszystko z dębu (okleina jak w WC)
                float rs = .11f, rc = .22f, rr2 = .09f;
                float yA = y0 + (y1 - y0) * .34f, yB = y0 + (y1 - y0) * .52f;
                Pr(rama, 0f, rs, y0, y1, r: .006f); Pr(rama, wL - rs, wL, y0, y1, r: .006f);
                Pr(rama, rs, wL - rs, y0, y0 + rc, r: .006f); Pr(rama, rs, wL - rs, y1 - rs, y1, r: .006f);
                Pr(rama, rs, wL - rs, yA - rr2 * .5f, yA + rr2 * .5f, r: .006f); Pr(rama, rs, wL - rs, yB - rr2 * .5f, yB + rr2 * .5f, r: .006f);
                foreach (var (b0, b1) in new[] { (y0 + rc, yA - rr2 * .5f), (yA + rr2 * .5f, yB - rr2 * .5f), (yB + rr2 * .5f, y1 - rs) })
                {
                    Pr(plyc, rs, wL - rs, b0, b1, zL0 + .02f, zL1 - .02f, .003f);
                    foreach (float sg in new[] { 1f, -1f })
                    {
                        float zA = sg > 0 ? zL1 - .02f : zL0 + .02f, zB = sg > 0 ? zL1 - .002f : zL0 + .002f, zm0 = Mathf.Min(zA, zB), zm1 = Mathf.Max(zA, zB);
                        // listwa profilowana na obwodzie płyciny (w ramiaku)
                        Pr(rama, rs, rs + .025f, b0, b1, zm0, zm1, .004f); Pr(rama, wL - rs - .025f, wL - rs, b0, b1, zm0, zm1, .004f);
                        Pr(rama, rs + .025f, wL - rs - .025f, b0, b0 + .025f, zm0, zm1, .004f); Pr(rama, rs + .025f, wL - rs - .025f, b1 - .025f, b1, zm0, zm1, .004f);
                        // pole podniesione: 7 cm od listwy, fazowane krawędzie, lico w płaszczyźnie lica ramiaka (bez nakładania — inne pole)
                        float zP = sg > 0 ? zL1 : zL0;
                        if (b1 - b0 > .22f) Pr(plyc, rs + .095f, wL - rs - .095f, b0 + .095f, b1 - .095f, Mathf.Min(zA, zP), Mathf.Max(zA, zP), .006f);
                    }
                }
            }
            else if (model == Stalowe)
            {   // STALOWE GRAFITOWE: skrzydło pełne z blachy (5 cm), wąskie pionowe przeszklenie 16 cm przy krawędzi przymykowej w ramce
                float xs0 = wL - .34f, xs1 = xs0 + .16f, ys0 = y0 + 1.0f * K, ys1 = Mathf.Min(y1 - .25f, y0 + 1.95f * K);
                Pr(rama, 0f, xs0, y0, y1); Pr(rama, xs1, wL, y0, y1); Pr(rama, xs0, xs1, y0, ys0); Pr(rama, xs0, xs1, ys1, y1);
                Szyba(xs0, xs1, ys0, ys1);
                foreach (float sg in new[] { 1f, -1f })   // ramka przeszklenia (stal) wysunięta 2.2 cm
                {
                    float zA = sg > 0 ? zL1 : zL0, zB = zA + sg * .022f;   // 2.2 cm przed licem (zasada ≥ 2 cm)
                    Pr(uchwyt, xs0 - .02f, xs0 + .01f, ys0 - .02f, ys1 + .02f, Mathf.Min(zA, zB), Mathf.Max(zA, zB), .003f); Pr(uchwyt, xs1 - .01f, xs1 + .02f, ys0 - .02f, ys1 + .02f, Mathf.Min(zA, zB), Mathf.Max(zA, zB), .003f);
                    Pr(uchwyt, xs0 + .01f, xs1 - .01f, ys0 - .02f, ys0 + .01f, Mathf.Min(zA, zB), Mathf.Max(zA, zB), .003f); Pr(uchwyt, xs0 + .01f, xs1 - .01f, ys1 - .01f, ys1 + .02f, Mathf.Min(zA, zB), Mathf.Max(zA, zB), .003f);
                }
            }
            else
            {   // SZKLANE: rama aluminiowa 6.5 cm (cokół 12 cm), tafla w środku
                Pr(rama, 0f, pS, y0, y1); Pr(rama, wL - pS, wL, y0, y1);
                Pr(rama, pS, wL - pS, y0, y0 + pD); Pr(rama, pS, wL - pS, y1 - pS, y1);
                Szyba(pS, wL - pS, y0 + pD, y1 - pS);
            }
            if (model == Szklane)
            {   // pochwyt: pionowa rura na ramie przymykowej, po obu stronach skrzydła, na dwóch wspornikach wkręconych w ramę
                float xa = wL - pS * .5f, ya = .75f * K, yb = ya + 1.0f * K, rr = .016f * K;
                foreach (float sg in new[] { 1f, -1f })
                {
                    float zf = sg > 0 ? zL1 : zL0, zb = zf + sg * .065f;
                    uchwyt.Rura(L(xa, ya - .04f, zb), L(xa, yb + .04f, zb), rr, 14);
                    foreach (float yw in new[] { ya + .06f, yb - .06f }) uchwyt.Rura(L(xa, yw, zf - sg * .005f), L(xa, yw, zb), .009f * K, 10);
                }
            }
            else
            {   // KLAMKA: szyld (płytka 5 × 22 cm × K, 2.2 cm) z klamką — szyjka od szyldu i ramię w stronę zawiasu, zakończone kulką; obie strony
                var mK = stal; var kl = BraxUliceKit.S(mK);   // 12.11: bez mosiądzu (zasada 10.11 — okucia ze stali nierdzewnej)
                float xk = wL - .075f, yk = 1.05f * K;
                foreach (float sg in new[] { 1f, -1f })
                {
                    float zf = sg > 0 ? zL1 : zL0, zt = zf + sg * .022f;   // szyld 2.2 cm (lico ≥ 2 cm przed skrzydłem)
                    Pr(uchwyt, xk - .025f * K, xk + .025f * K, yk - .15f * K, yk + .07f * K, Mathf.Min(zf - sg * .002f, zt), Mathf.Max(zf - sg * .002f, zt), .004f);
                    uchwyt.Rura(L(xk, yk, zt), L(xk, yk, zt + sg * .055f), .009f * K, 12);                       // szyjka
                    uchwyt.Rura(L(xk + .008f, yk, zt + sg * .055f), L(xk - .13f * K, yk, zt + sg * .055f), .0095f * K, 12);   // ramię
                }
                BraxMiastoA.Obiekt(z, "Drzwi " + label + ": klamki", uchwyt, mK, true); uchwyt = new UlSiatka();
            }
            // zawiasy (3 na skrzydło): walce na osi zawiasu, częściowo w ościeżnicy
            var zaw = new UlSiatka();
            foreach (float yz in new[] { y0 + .25f, (y0 + y1) * .5f, y1 - .25f })
                zaw.Rura(new Vector3(-dir * .006f, yz - .07f, zF1 + .012f + .1f), new Vector3(-dir * .006f, yz + .07f, zF1 + .012f + .1f), .011f, 10);   // walec przed licem ościeżnicy
            BraxMiastoA.Obiekt(z, "Drzwi " + label + ": zawiasy", zaw, stal, true);
            BraxMiastoA.Obiekt(z, "Drzwi " + label + ": skrzydło", rama, mRamy, true);
            if (plyc.Wierzcholki > 0) BraxMiastoA.Obiekt(z, "Drzwi " + label + ": płyciny", plyc, mRamy, true);
            if (tafla.Wierzcholki > 0) BraxMiastoA.Obiekt(z, "Drzwi " + label + ": szyba skrzydła", tafla, szklo);
            if (uchwyt.Wierzcholki > 0) BraxMiastoA.Obiekt(z, "Drzwi " + label + ": pochwyty i ramki", uchwyt, stal, true);
            var col = z.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(dir * wL * .5f, (y0 + y1) * .5f, (zL0 + zL1) * .5f + .1f); col.size = new Vector3(wL, y1 - y0, zL1 - zL0);
            foreach (var t in z.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            zawiasy[i] = z;
        }
        GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);
        var d = root.gameObject.AddComponent<BraxCityDoor>(); d.id = id; d.label = label; d.leaves = zawiasy; d.sides = new[] { 1f, -1f };
        return d;
    }

    /// <summary>Drzwi automatyczne przesuwne w otworze (model 3): ościeżnica, napęd w ślemieniu z czujnikiem, naświetle, dwa skrzydła
    /// szklane w smukłej ramie odjeżdżające na boki (BraxDrzwiPrzesuwne — czujnik obecności, same się zamykają), pas oznakowania.</summary>
    static BraxDrzwiPrzesuwne Przesuwane(Transform rodzic, string id, string label, Vector3 p, Vector3 n, float W, float H, float progY)
    {
        n.y = 0f; n.Normalize();
        var root = new GameObject("Drzwi lokalu: " + label).transform; root.SetParent(rodzic, false);
        root.position = p + Vector3.up * progY; root.rotation = Quaternion.LookRotation(n, Vector3.up); H -= progY;
        const float pr = .07f, zF0 = -.15f, zF1 = -.05f, sz = .003f, zs = -.1f;
        float hS = Mathf.Min(2.6f, H - .45f), hN = hS + .26f;   // skrzydła, nad nimi kaseta napędu 26 cm
        float x0 = -W * .5f, x1 = W * .5f;
        var ram = new UlSiatka(); var szk = new UlSiatka(); var led = new UlSiatka();
        ram.Poducha(new Vector3(x0, -.02f, zF0), new Vector3(x0 + pr, H, zF1), .004f, 0f, 3);
        ram.Poducha(new Vector3(x1 - pr, -.02f, zF0), new Vector3(x1, H, zF1), .004f, 0f, 3);
        ram.Poducha(new Vector3(x0 + pr, H - pr, zF0), new Vector3(x1 - pr, H, zF1), .004f, 0f, 3);
        // kaseta napędu (aluminium, 26 cm, wysunięta 4 cm przed ościeżnicę od ulicy) i czujnik ruchu pod nią
        ram.Poducha(new Vector3(x0 + pr, hS + sz, zF0 - .02f), new Vector3(x1 - pr, hN, zF1 + .04f), .008f, 0f, 4);
        ram.Poducha(new Vector3(-.09f, hS - .05f, zF1 + .005f), new Vector3(.09f, hS + sz - .002f, zF1 + .045f), .012f, 0f, 4);
        led.Rura(new Vector3(0f, hS - .045f, zF1 + .025f), new Vector3(0f, hS - .071f, zF1 + .025f), .007f, 10);   // dioda czujnika 2 cm pod obudową
        if (H - pr - hN > .05f)
            foreach (var nn in new[] { Vector3.forward, Vector3.back })
                szk.Quad(new Vector3(x0 + pr, hN, zs), new Vector3(x1 - pr, hN, zs), new Vector3(x1 - pr, H - pr, zs), new Vector3(x0 + pr, H - pr, zs), nn);
        BraxMiastoA.Obiekt(root, "Drzwi " + label + ": ościeżnica i kaseta napędu", ram, czarny, true);
        if (szk.Wierzcholki > 0) BraxMiastoA.Obiekt(root, "Drzwi " + label + ": naświetle", szk, szklo);
        BraxMiastoA.Obiekt(root, "Drzwi " + label + ": czujnik (dioda)", led, BraxMiastoA.Swieci("drzwi automatyczne: dioda czujnika", new Color(.9f, .2f, .15f), new Color(1.6f, .2f, .1f)));
        foreach (float xs in new[] { x0 + pr * .5f, x1 - pr * .5f })
        {
            var c = new GameObject("kolizja: słupek ościeżnicy").AddComponent<BoxCollider>(); c.transform.SetParent(root, false);
            c.center = new Vector3(xs, H * .5f, (zF0 + zF1) * .5f); c.size = new Vector3(pr, H, zF1 - zF0);
        }
        var pas = BraxUliceKit.Mat("drzwi automatyczne: pas oznakowania (szkło mleczne)", new Color(.92f, .93f, .94f, .7f), .3f, 0f);
        float wL = (W - 2f * pr - sz) * .5f, pS = .045f, y0 = .02f, y1 = hS - sz, yP0 = y0 + 1.0f * K, yP1 = yP0 + .08f * K;
        var skrzydla = new Transform[2]; var przes = new Vector3[2]; var blok = new Collider[2];
        for (int i = 0; i < 2; i++)
        {
            float dir = i == 0 ? -1f : 1f;                                   // lewe skrzydło odjeżdża w lewo
            var sk = new GameObject((i == 0 ? "Lewe" : "Prawe") + " skrzydło (przesuwne)").transform; sk.SetParent(root, false);
            sk.localPosition = new Vector3(dir * (sz * .5f), 0f, zs);       // krawędź przymykowa w osi otworu
            Vector3 L(float a, float y, float zz) => new Vector3(dir * a, y, zz);
            var rm = new UlSiatka(); var gl = new UlSiatka(); var gp = new UlSiatka();
            void Pr(float a0, float a1, float b0, float b1) { var A = L(a0, b0, -.025f); var B = L(a1, b1, .025f); rm.Poducha(Vector3.Min(A, B), Vector3.Max(A, B), .004f, 0f, 3); }
            Pr(0f, pS, y0, y1); Pr(wL - pS, wL, y0, y1); Pr(pS, wL - pS, y0, y0 + .09f); Pr(pS, wL - pS, y1 - pS, y1);
            foreach (var (b0, b1, u) in new[] { (y0 + .09f, yP0, gl), (yP0, yP1, gp), (yP1, y1 - pS, gl) })
                foreach (var nn in new[] { Vector3.forward, Vector3.back })
                    u.Quad(L(pS, b0, 0f), L(wL - pS, b0, 0f), L(wL - pS, b1, 0f), L(pS, b1, 0f), nn);
            BraxMiastoA.Obiekt(sk, "Drzwi " + label + ": rama skrzydła przesuwnego", rm, czarny, true);
            BraxMiastoA.Obiekt(sk, "Drzwi " + label + ": szyba skrzydła", gl, szklo);
            BraxMiastoA.Obiekt(sk, "Drzwi " + label + ": pas oznakowania", gp, pas);
            var bc = sk.gameObject.AddComponent<BoxCollider>(); bc.center = new Vector3(dir * wL * .5f, (y0 + y1) * .5f, 0f); bc.size = new Vector3(wL, y1 - y0, .06f);
            foreach (var t in sk.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            skrzydla[i] = sk; przes[i] = new Vector3(dir * (wL - .04f), 0f, 0f); blok[i] = bc;   // w kieszeń muru za słupkiem
        }
        GameObjectUtility.SetStaticEditorFlags(root.gameObject, 0);
        var d = root.gameObject.AddComponent<BraxDrzwiPrzesuwne>();
        d.etykieta = label; d.skrzydla = skrzydla; d.przesuniecie = przes; d.blokady = blok; d.promien = 3.2f;
        return d;
    }

    /// <summary>Usuwa stare drzwi miasta o danym id (cała grupa „PUBLIC WORKING ENTRANCE”: skrzydła, ościeża, nadproże) i czyści
    /// rejestr świata. Zwraca liczbę usuniętych.</summary>
    public static int UsunStare(string id)
    {
        int n = 0;
        foreach (var d in Object.FindObjectsByType<BraxCityDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (d && d.id == id) { Object.DestroyImmediate(d.gameObject); n++; }
        var swiat = Object.FindFirstObjectByType<BraxCityWorld>(FindObjectsInactive.Include);
        if (swiat && swiat.doors != null) swiat.doors = swiat.doors.Where(x => x).ToArray();
        return n;
    }
}
