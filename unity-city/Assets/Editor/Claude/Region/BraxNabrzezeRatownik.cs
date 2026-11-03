using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// WIEŻA RATOWNIKA NA PLAŻY MIEJSKIEJ (właściciel 3.11 10:52: „pomaluj ładnie budkę ratownika… nawet nie da się na nią wejść.
// Zła skala”). Było: model CC0 lifeguard_tower.glb (Source/HarborBeach, BraxNabrzezePlaza.Wyposazenie) skalowany do 6.2 m
// bez × k, jedna kolizja-pudło 0.9 × obrys (nie da się wejść), kolory modelu. Model ZDJĘTY — wieża z kodu, wymiary × k:
//  • 4 słupy stalowe malowane na biało z krzyżulcami (X) na każdym boku, rama pokładu z kształtowników, pokład z desek;
//  • kabina z deskowania (czerwona z zewnątrz, biała w środku) z PRAWDZIWYMI otworami: drzwi 0.9 × 2.1 m × k od balkonu,
//    okna od morza i z boków (ościeża, ramy, szyba w środku grubości ściany — nic na powierzchni);
//  • balkon od morza z balustradą, dach płaski z blachy trapezowej na słupkach (okap nad balkonem), opaska dachu;
//  • schody stalowe z drewnianymi stopniami (podstopnica 0.23 m, nosek zaokrąglony) z poręczami po obu stronach i podestem;
//  • tablica RATOWNIK (napis W TEKSTURZE lica tablicy — nie nakładka), koło ratunkowe (model Poly Haven lifebuoy, bez
//    pobrania — z kodu), stołek w kabinie (metal_stool_01, jeśli jest), deska ratownicza kształtowana oparta o słup;
//  • kolizje: słupy, pokład, ściany kabiny z przerwą na drzwi, balustrady z przerwą na schody, schody (pochylnia), podest, dach.
public static class BraxNabrzezeRatownik
{
    static float K => BraxNabrzeze.K;
    static readonly Dictionary<Material, UlSiatka> partie = new Dictionary<Material, UlSiatka>();
    static UlSiatka S(Material m) { if (!partie.TryGetValue(m, out var s)) partie[m] = s = new UlSiatka(); return s; }
    static int nKol;

    /// <summary>Stawia wieżę: (x, z) środek słupów, przodem (balkon) na +x (morze). Zwraca poziom pokładu.</summary>
    public static float Buduj(Transform root, Transform kol, float bx, float bz, System.Func<float, float, float> teren, Dictionary<string, Vector3> miejsca)
    {
        partie.Clear(); nKol = 0;
        var T = new GameObject("Plaża: wieża ratownika (z kodu, × k)").transform; T.SetParent(root, false);
        var bialy = BraxNabrzeze.Kolor("Ratownik: stal malowana biała", new Color(.93f, .93f, .91f), .45f, .3f);
        var czerw = BraxNabrzeze.Mat("Ratownik: deskowanie kabiny (czerwone)", 1.2f, new Color(.78f, .14f, .11f), "wood_plank_wall", "wood_planks_grey", "wooden_planks");
        var wewn = BraxNabrzeze.Mat("Ratownik: deskowanie wewnątrz (białe)", 1.2f, new Color(.95f, .94f, .9f), "wood_plank_wall", "wood_planks_grey", "wooden_planks");
        var deski = BraxNabrzeze.Mat("Ratownik: deski pokładu i stopni", 1.4f, new Color(.92f, .86f, .78f), "wood_planks_grey", "wooden_planks", "wood_planks");
        var blacha = BraxNabrzeze.Kolor("Ratownik: blacha dachu (biała)", new Color(.9f, .9f, .88f), .45f, .5f);
        var szklo = BraxNabrzeze.Kolor("Ratownik: szyby", new Color(.62f, .72f, .76f, .3f), .06f, .1f);
        var ramy = BraxNabrzeze.Kolor("Ratownik: ramy okien (biały PCV)", new Color(.96f, .96f, .95f), .4f);
        var guma = BraxNabrzeze.Kolor("Ratownik: nakładki antypoślizgowe (grafit)", new Color(.12f, .12f, .13f), .9f);

        float g0 = teren(bx, bz), hp = 2.4f * K, yD = g0 + hp;
        Vector3 P(float u, float y, float v) => new Vector3(bx + u, y, bz + v);
        float uL = 1.5f * K, vL = 1.3f * K;                        // słupy
        float u0 = -2.3f, u1 = 2.3f, v0 = -2.0f, v1 = 2.0f;        // pokład
        float cu0 = -2.2f, cu1 = .5f, cv = 1.8f, hC = 2.95f;       // kabina (u od tyłu do frontu), wysokość ścian

        // ---------------- słupy, krzyżulce, rama pokładu, pokład ----------------
        var st = S(bialy);
        var nogi = new[] { new Vector2(-uL, -vL), new Vector2(uL, -vL), new Vector2(uL, vL), new Vector2(-uL, vL) };
        foreach (var ng in nogi)
        {
            float g = teren(bx + ng.x, bz + ng.y);
            st.Rura(P(ng.x, g - .4f, ng.y), P(ng.x, yD - .2f, ng.y), .075f, 12);
            Toczenie(st, new[] { new Vector2(0, g - .02f), new Vector2(.17f, g - .02f), new Vector2(.17f, g + .02f), new Vector2(.09f, g + .05f), new Vector2(0, g + .05f) }, P(ng.x, 0, ng.y), 12);   // stopa
            BraxNabrzeze.Kol(kol, "Wieża ratownika: słup", P(ng.x, (g + yD) * .5f, ng.y), new Vector3(.2f, yD - g, .2f)); nKol++;
        }
        for (int i = 0; i < 4; i++)
        {
            var a = nogi[i]; var b = nogi[(i + 1) % 4];
            float ga = teren(bx + a.x, bz + a.y) + .45f, gb = teren(bx + b.x, bz + b.y) + .45f, yt = yD - .45f;
            st.Rura(P(a.x, ga, a.y), P(b.x, yt, b.y), .03f, 8); st.Rura(P(b.x, gb, b.y), P(a.x, yt, a.y), .03f, 8);
            st.Rura(P(a.x, yt, a.y), P(b.x, yt, b.y), .035f, 8);
        }
        var rama = PZ(.12f, .18f, .015f, new Vector2(0, -.09f));
        foreach (float v in new[] { v0 + .06f, -vL, vL, v1 - .06f }) Wyciag(st, rama, P(u0, yD - .04f, v), Vector3.right, Vector3.up, Vector3.forward, u1 - u0);
        foreach (float u in new[] { u0 + .06f, -uL, 0f, uL, u1 - .06f }) Wyciag(st, rama, P(u, yD - .04f - .19f, v0), Vector3.forward, Vector3.up, Vector3.right, v1 - v0);
        var pk = S(deski); var deska = PZ(.14f, .04f, .006f, new Vector2(0, -.02f));
        for (float u = u0 + .07f; u < u1 - .06f; u += .152f) Wyciag(pk, deska, P(u, yD, v0), Vector3.forward, Vector3.up, Vector3.right, v1 - v0);
        BraxNabrzeze.KolMM(kol, "Wieża ratownika: pokład", P(u0, yD - .25f, v0), P(u1, yD, v1)); nKol++;

        // ---------------- kabina: ściany z otworami (drzwi, okna), ramy i szyby w środku grubości ----------------
        float t = .1f, yW0 = yD, yW1 = yD + hC, dH = 2.1f * K, dW = .9f * K;
        // ściana: lico zewn. (czerwone) i wewn. (białe), ościeża przy otworach, otwory w (s, y) liczone od początku A po R
        void Sciana(Vector3 A, Vector3 R, Vector3 N, float L, List<Rect> otw, string nazwa)
        {
            BraxMiastoA.Sciana(S(czerw), A + N * (t * .5f), R, N, L, yW0, yW1, otw, t, S(wewn));
            var Lm = new List<Rect>(); foreach (var o in otw) Lm.Add(Rect.MinMaxRect(L - o.xMax, o.yMin, L - o.xMin, o.yMax));
            BraxMiastoA.Sciana(S(wewn), A - N * (t * .5f) + R * L, -R, -N, L, yW0, yW1, Lm);
            foreach (var o in otw)
            {
                bool drzwi = o.yMin <= yW0 + .01f;
                if (!drzwi)
                {   // okno: rama 5 cm w świetle otworu, szyba 2 cm w środku grubości ściany
                    float s0 = o.xMin, s1 = o.xMax, y0 = o.yMin, y1 = o.yMax; var c = A + R * ((s0 + s1) * .5f) + Vector3.up * ((y0 + y1) * .5f);
                    S(ramy).OBox(c + Vector3.up * ((y1 - y0) * .5f - .025f), R, Vector3.up, N, s1 - s0, .05f, t * .7f);
                    S(ramy).OBox(c - Vector3.up * ((y1 - y0) * .5f - .025f), R, Vector3.up, N, s1 - s0, .05f, t * .7f);
                    S(ramy).OBox(c + R * ((s1 - s0) * .5f - .025f), R, Vector3.up, N, .05f, y1 - y0 - .1f, t * .7f);
                    S(ramy).OBox(c - R * ((s1 - s0) * .5f - .025f), R, Vector3.up, N, .05f, y1 - y0 - .1f, t * .7f);
                    if (s1 - s0 > 1.2f) S(ramy).OBox(c, R, Vector3.up, N, .05f, y1 - y0 - .1f, t * .7f);   // słupek środkowy
                    S(szklo).OBox(c, R, Vector3.up, N, s1 - s0 - .1f, y1 - y0 - .1f, .012f);
                    // parapet zewnętrzny (kształtowany, wystaje 4 cm przed lico)
                    Wyciag(S(bialy), new[] { new Vector2(0, 0), new Vector2(.08f, -.02f), new Vector2(.08f, -.04f), new Vector2(0, -.035f) }, A + R * (s0 - .03f) + Vector3.up * y0 + N * (t * .5f), R, Vector3.up, N, s1 - s0 + .06f);
                }
            }
            // kolizje: odcinki pełne + nadproże nad drzwiami
            float k0 = 0f;
            var ot = new List<Rect>(otw); ot.Sort((a, b) => a.xMin.CompareTo(b.xMin)); ot.Add(Rect.MinMaxRect(L, yW0, L, yW0));
            foreach (var o in ot)
            {
                bool drzwi = o.width > 0 && o.yMin <= yW0 + .01f;
                float e0 = drzwi ? o.xMin : (o.width > 0 ? o.xMin : L);
                if (!drzwi && o.width > 0) continue;   // okna: ściana pełna w kolizji
                if (e0 - k0 > .05f) { var c = A + R * ((k0 + e0) * .5f) + Vector3.up * ((yW0 + yW1) * .5f); BraxNabrzeze.KolRot(kol, "Wieża ratownika: " + nazwa, c, new Vector3(e0 - k0, yW1 - yW0, t + .04f), Quaternion.LookRotation(N)); nKol++; }
                if (drzwi) { var c = A + R * o.center.x + Vector3.up * ((o.yMax + yW1) * .5f); BraxNabrzeze.KolRot(kol, "Wieża ratownika: nadproże", c, new Vector3(o.width, yW1 - o.yMax, t + .04f), Quaternion.LookRotation(N)); nKol++; k0 = o.xMax; }
            }
        }
        // front (u = cu1, lico na +u): drzwi pośrodku, okna po bokach; A = narożnik v = −cv (R = +v)
        Sciana(P(cu1, 0, -cv), Vector3.forward, Vector3.right, 2f * cv, new List<Rect> {
            Rect.MinMaxRect(cv - dW * .5f, yW0, cv + dW * .5f, yW0 + dH),
            Rect.MinMaxRect(.25f, yD + 1.0f, cv - dW * .5f - .3f, yD + 2.3f), Rect.MinMaxRect(cv + dW * .5f + .3f, yD + 1.0f, 2f * cv - .25f, yD + 2.3f) }, "ściana frontowa");
        // tył (u = cu0, lico −u): okno małe; A = narożnik v = +cv (R = −v)
        Sciana(P(cu0, 0, cv), Vector3.back, Vector3.left, 2f * cv, new List<Rect> { Rect.MinMaxRect(cv - .55f, yD + 1.2f, cv + .55f, yD + 2.2f) }, "ściana tylna");
        // boki (v = ±cv): okna szerokie od strony morza
        float Lb = cu1 - cu0;
        Sciana(P(cu0, 0, -cv), Vector3.right, Vector3.back, Lb, new List<Rect> { Rect.MinMaxRect(Lb - 1.9f, yD + 1.0f, Lb - .35f, yD + 2.3f) }, "ściana boczna płd.");
        Sciana(P(cu1, 0, cv), Vector3.left, Vector3.forward, Lb, new List<Rect> { Rect.MinMaxRect(.35f, yD + 1.0f, 1.9f, yD + 2.3f) }, "ściana boczna płn.");
        // narożniki kabiny: listwy narożne (białe) zakrywają styk desek
        foreach (var (cu, cvv) in new[] { (cu0, -cv), (cu0, cv), (cu1, -cv), (cu1, cv) })
            Wyciag(S(bialy), PZ(.16f, .16f, .02f, Vector2.zero), P(cu, yW0, cvv), Vector3.up, Vector3.forward, Vector3.right, hC);
        // ościeżnica drzwi (biała opaska na licu zewnętrznym, 2 cm przed nim)
        {
            float vA = -dW * .5f, vB = dW * .5f; float uf = cu1 + t * .5f + .011f;
            S(bialy).OBox(P(uf, yD + dH * .5f, vA - .04f), Vector3.forward, Vector3.up, Vector3.right, .08f, dH, .022f, true);
            S(bialy).OBox(P(uf, yD + dH * .5f, vB + .04f), Vector3.forward, Vector3.up, Vector3.right, .08f, dH, .022f, true);
            S(bialy).OBox(P(uf, yD + dH + .04f, 0), Vector3.forward, Vector3.up, Vector3.right, dW + .16f, .08f, .022f, true);
        }
        // skrzydło drzwi otwarte do środka (przy ścianie frontowej), klamka
        {
            var drzwi = BraxNabrzeze.Kolor("Ratownik: drzwi (biały laminat)", new Color(.94f, .94f, .92f), .5f);
            var c = P(cu1 - t * .5f - dW * .5f, yD + dH * .5f, -dW * .5f + .03f);
            S(drzwi).OBox(c, Vector3.right, Vector3.up, Vector3.forward, dW - .03f, dH - .02f, .04f);
            var kl = c + Vector3.left * (dW * .5f - .12f) + Vector3.down * (dH * .5f - 1.05f * K);
            S(BraxNabrzeze.Kolor("Ratownik: klamka (stal)", new Color(.72f, .73f, .74f), .3f, .85f)).Rura(kl + Vector3.forward * .03f, kl + Vector3.forward * .1f, .01f, 6);
            BraxNabrzeze.KolRot(kol, "Wieża ratownika: skrzydło drzwi", c, new Vector3(dW, dH, .05f), Quaternion.LookRotation(Vector3.forward)); nKol++;
        }

        // ---------------- dach płaski z blachy trapezowej, opaska, słupki balkonu ----------------
        float yR = yD + hC, ru0 = cu0 - .35f, ru1 = u1 + .1f, rv = v1 + .1f;
        S(BraxNabrzeze.Kolor("Ratownik: podbitka dachu (biała)", new Color(.95f, .95f, .93f), .8f)).Quad(P(ru0, yR, -rv), P(ru1, yR, -rv), P(ru1, yR, rv), P(ru0, yR, rv), Vector3.down);
        var bs = S(blacha);
        for (float v = -rv; v < rv - .01f; v += .2f)
        {
            float a = v, b = Mathf.Min(v + .1f, rv), c = Mathf.Min(v + .2f, rv);
            bs.Quad(P(ru0, yR + .06f, a), P(ru1, yR + .06f, a), P(ru1, yR + .095f, b), P(ru0, yR + .095f, b), Vector3.up);
            bs.Quad(P(ru0, yR + .095f, b), P(ru1, yR + .095f, b), P(ru1, yR + .06f, c), P(ru0, yR + .06f, c), Vector3.up);
        }
        Wyciag(S(bialy), PZ(.06f, .22f, .01f, new Vector2(0, .05f)), P(ru0 - .03f, yR, -rv - .03f), Vector3.right, Vector3.up, Vector3.forward, ru1 - ru0 + .06f);
        Wyciag(S(bialy), PZ(.06f, .22f, .01f, new Vector2(0, .05f)), P(ru0 - .03f, yR, rv + .03f), Vector3.right, Vector3.up, Vector3.forward, ru1 - ru0 + .06f);
        Wyciag(S(bialy), PZ(.06f, .22f, .01f, new Vector2(0, .05f)), P(ru0, yR, -rv), Vector3.forward, Vector3.up, Vector3.right, 2f * rv);
        Wyciag(S(bialy), PZ(.06f, .22f, .01f, new Vector2(0, .05f)), P(ru1, yR, -rv), Vector3.forward, Vector3.up, Vector3.right, 2f * rv);
        foreach (float v in new[] { v0 + .12f, v1 - .12f }) st.Rura(P(u1 - .12f, yD, v), P(u1 - .12f, yR, v), .045f, 10);
        BraxNabrzeze.KolMM(kol, "Wieża ratownika: dach", P(ru0, yR, -rv), P(ru1, yR + .12f, rv)); nKol++;

        // ---------------- balustrady balkonu i podestu (przerwa na wejście ze schodów) ----------------
        float hb = 1.1f * K;
        void Balustrada(Vector3 a, Vector3 b, string nazwa)
        {
            float L = Vector3.Distance(a, b); int np = Mathf.Max(1, Mathf.CeilToInt(L / 1.0f));
            st.Rura(a + Vector3.up * hb, b + Vector3.up * hb, .03f, 10); st.Rura(a + Vector3.up * hb * .5f, b + Vector3.up * hb * .5f, .02f, 8);
            st.Rura(a + Vector3.up * .12f, b + Vector3.up * .12f, .02f, 8);
            for (int s = 0; s <= np; s++) { var p = Vector3.Lerp(a, b, s / (float)np); st.Rura(p, p + Vector3.up * hb, .025f, 8); }
            var dir = b - a; dir.y = 0;
            BraxNabrzeze.KolRot(kol, "Wieża ratownika: " + nazwa, (a + b) * .5f + Vector3.up * (hb * .5f), new Vector3(.1f, hb, L), Quaternion.LookRotation(dir.normalized)); nKol++;
        }
        float uSt0 = cu1 + .1f, uSt1 = uSt0 + 1.0f;   // wejście ze schodów na balkon (bok południowy)
        Balustrada(P(u1 - .06f, yD, v0 + .06f), P(u1 - .06f, yD, v1 - .06f), "balustrada frontu");
        Balustrada(P(cu1 + .05f, yD, v1 - .06f), P(u1 - .06f, yD, v1 - .06f), "balustrada boku płn.");
        Balustrada(P(uSt1, yD, v0 + .06f), P(u1 - .06f, yD, v0 + .06f), "balustrada boku płd.");

        // ---------------- schody stalowe z podestem (bok południowy, w górę ku morzu) ----------------
        float sv0 = v0 - 1.05f, sv1 = v0 - .05f, uTop = uSt0;
        float gStop = teren(bx + uTop - 3.3f, bz + (sv0 + sv1) * .5f);
        int n = Mathf.Max(8, Mathf.RoundToInt((yD - gStop) / .23f)); float rise = (yD - gStop) / n, run = .25f;
        float uFoot = uTop - (n - 1) * run;
        // podest (u uSt0…uSt1) na wysokości pokładu, deski i rama
        for (float u = uSt0 + .07f; u < uSt1 - .05f; u += .152f) Wyciag(pk, deska, P(u, yD, sv0), Vector3.forward, Vector3.up, Vector3.right, sv1 - sv0 + .05f);
        Wyciag(st, rama, P(uSt0, yD - .04f, sv0 + .06f), Vector3.right, Vector3.up, Vector3.forward, uSt1 - uSt0);
        Wyciag(st, rama, P(uSt1 - .06f, yD - .04f - .19f, sv0), Vector3.forward, Vector3.up, Vector3.right, sv1 - sv0);
        st.Rura(P(uSt1 - .1f, gStop - .3f, sv0 + .1f), P(uSt1 - .1f, yD - .2f, sv0 + .1f), .06f, 10);
        BraxNabrzeze.KolMM(kol, "Wieża ratownika: podest", P(uSt0 - .05f, yD - .25f, sv0), P(uSt1, yD, sv1 + .05f)); nKol++;
        Balustrada(P(uSt0 + .05f, yD, sv0 + .05f), P(uSt1 - .05f, yD, sv0 + .05f), "balustrada podestu");
        Balustrada(P(uSt1 - .05f, yD, sv0 + .05f), P(uSt1 - .05f, yD, v0 + .06f), "balustrada podestu (czoło)");
        // policzki (kształtownik), stopnie z noskiem i nakładką antypoślizgową
        var pol = PZ(.05f, .24f, .01f, Vector2.zero);
        Vector3 dolS = P(uFoot - run, gStop, 0), goraS = P(uTop, yD, 0); var kier = (goraS - dolS).normalized; float dlS = Vector3.Distance(dolS, goraS);
        var gB = Vector3.Cross(kier, Vector3.forward).normalized; if (gB.y < 0) gB = -gB;
        foreach (float v in new[] { sv0 + .03f, sv1 - .03f })
            Wyciag(st, pol, new Vector3(bx + uFoot - run, gStop - .05f, bz + v) - gB * .1f, kier, gB, Vector3.forward, dlS + .15f);
        for (int i = 0; i < n - 1; i++)
        {
            float u = uFoot + i * run, y = gStop + (i + 1) * rise;
            var prof = new[] { new Vector2(-run * .5f - .02f, 0), new Vector2(run * .5f - .03f, 0), new Vector2(run * .5f, -.012f), new Vector2(run * .5f, -.033f), new Vector2(run * .5f - .02f, -.045f), new Vector2(-run * .5f - .02f, -.045f) };
            Wyciag(pk, prof, P(u, y, sv0 + .06f), Vector3.forward, Vector3.up, Vector3.left, sv1 - sv0 - .12f);
            S(guma).OBox(P(u - run * .5f + .03f, y + .003f, (sv0 + sv1) * .5f), Vector3.right, Vector3.up, Vector3.forward, .04f, .006f, sv1 - sv0 - .2f, false, true);
        }
        // poręcze schodów (obustronne) na słupkach co 2 stopnie
        foreach (float v in new[] { sv0 + .03f, sv1 - .03f })
        {
            var a = new Vector3(bx + uFoot - run, gStop + .9f * K, bz + v); var b = new Vector3(bx + uTop, yD + .9f * K, bz + v);
            st.Rura(a, b, .028f, 10);
            for (int i = 0; i < n; i += 2) { var p = new Vector3(bx + uFoot + i * run - run, gStop + i * rise, bz + v); st.Rura(p, p + Vector3.up * .9f * K, .022f, 8); }
        }
        {   // kolizja schodów: pochylnia wzdłuż biegu (gracz wchodzi bez skakania)
            var c = (dolS + goraS) * .5f + new Vector3(0, -.06f, (sv0 + sv1) * .5f);
            BraxNabrzeze.KolRot(kol, "Wieża ratownika: schody", c, new Vector3(sv1 - sv0, .12f, dlS + .2f), Quaternion.LookRotation(kier, gB)); nKol++;
            foreach (float v in new[] { sv0 + .03f, sv1 - .03f }) { BraxNabrzeze.KolRot(kol, "Wieża ratownika: poręcz schodów", c + new Vector3(0, .55f * K, v - (sv0 + sv1) * .5f), new Vector3(.08f, 1.1f * K, dlS), Quaternion.LookRotation(kier, gB)); nKol++; }
        }

        // ---------------- tablica RATOWNIK (napis w teksturze lica), na balustradzie frontu i na ścianie tylnej ----------------
        string tex = TablicaTekstura();
        float tw = 1.7f, th = .42f;
        if (tex != null)
        {
            // front: tablica na balustradzie (lico na +u), tył i krawędzie osobnym materiałem — lico JEST obrazem
            var ld = P(u1 + .02f, yD + hb - th - .08f, -tw * .5f);
            BraxMiastoA.Obraz(T, "tablica RATOWNIK (front)", tex, ld, Vector3.forward, tw, th, Vector3.right, 0f);
            S(bialy).OBox(ld + Vector3.forward * (tw * .5f) + Vector3.up * (th * .5f) + Vector3.left * .011f, Vector3.forward, Vector3.up, Vector3.left, tw, th, .022f, true);   // płyta bez lica (licem jest obraz)
            // tył kabiny: tablica od strony plaży (bez tylnej ściany — przylega do ściany)
            var ld2 = P(cu0 - t * .5f - .025f, yD + 2.35f, tw * .5f);
            BraxMiastoA.Obraz(T, "tablica RATOWNIK (tył)", tex, ld2, Vector3.back, tw, th, Vector3.left, 0f);
            S(bialy).OBox(ld2 + Vector3.back * (tw * .5f) + Vector3.up * (th * .5f) + Vector3.right * .0125f, Vector3.back, Vector3.up, Vector3.right, tw, th, .025f, true);
        }
        // ---------------- koło ratunkowe (Poly Haven), stołek w kabinie, deska ratownicza ----------------
        int nModeli = 0;
        var kolo = BraxRegion.Model("lifebuoy");
        if (kolo)
        {
            var o = BraxRegion.Put(T, kolo, "Wieża ratownika: koło ratunkowe (model)", P(1.0f, yD + .2f, v1 + .14f), 0f, K, false);
            if (o) { var b = BraxRegion.Bounds(o); if (b.size.z > b.size.x) { o.transform.RotateAround(b.center, Vector3.up, 90f); } nModeli++; }
        }
        var stolek = BraxRegion.Model("metal_stool_01", "wooden_stool_01");
        if (stolek && BraxRegion.Put(T, stolek, "Wieża ratownika: stołek (model)", P(cu1 - .7f, yD, .9f), 90f, K, true)) nModeli++;
        {   // deska ratownicza: obrys „stadion” z noskiem, grubość 8 cm, oparta o słup frontu (pochylenie 70°)
            var red = BraxNabrzeze.Kolor("Ratownik: deska ratownicza (czerwona)", new Color(.8f, .1f, .08f), .4f);
            float L = 2.6f * K * .78f, w = .55f;
            var baza = P(uL + .75f, teren(bx + uL + .75f, bz + vL + .32f) + .02f, vL + .32f);
            var dl = Quaternion.AngleAxis(-20f, Vector3.forward) * Vector3.up;   // od pionu ku słupowi (−u)
            dl = new Vector3(-Mathf.Abs(dl.x), dl.y, 0).normalized;
            var poprz = Vector3.forward; var grub = Vector3.Cross(dl, poprz).normalized;
            Wyciag(S(red), PZ(L, w, w * .48f, new Vector2(L * .5f, 0), 4), baza, grub, poprz, dl, .08f);
            BraxNabrzeze.KolRot(kol, "Wieża ratownika: deska ratownicza", baza + dl * (L * .5f), new Vector3(w, L, .1f), Quaternion.LookRotation(grub, dl)); nKol++;
        }

        // ---------------- zapis siatek ----------------
        int nSiatek = 0;
        foreach (var kv in partie)
        {
            if (kv.Value.Wierzcholki == 0) continue;
            bool duze = kv.Key == czerw || kv.Key == blacha || kv.Key == deski;
            BraxNabrzeze.Obiekt(T, "Wieża ratownika: " + kv.Key.name, kv.Value.Zbuduj("Wieża ratownika: " + kv.Key.name), kv.Key, false, duze);
            nSiatek++;
        }
        partie.Clear();
        miejsca["plaza_ratownik"] = P(3.5f, teren(bx + 3.5f, bz), 0f);
        miejsca["plaza_ratownik_schody"] = P(uFoot - run - .8f, gStop, (sv0 + sv1) * .5f);
        miejsca["plaza_ratownik_platforma"] = P((cu1 + u1) * .5f, yD, 0f);
        BraxNabrzeze.Log("plaża: WIEŻA RATOWNIKA z kodu (model lifeguard_tower.glb zdjęty) — pokład " + (yD - g0).ToString("F2") + " m nad piaskiem (2.4 m × k), kabina "
            + (cu1 - cu0).ToString("F1") + " × " + (2f * cv).ToString("F1") + " m, drzwi " + dW.ToString("F2") + " × " + dH.ToString("F2") + " m, schody " + n + " stopni po "
            + rise.ToString("F3") + " m, kolizji " + nKol + ", siatek " + nSiatek + ", modeli PH " + nModeli + ", tablica " + (tex != null ? "tak" : "BRAK"));
        return yD;
    }

    // tablica: czerwone pole, biały napis RATOWNIK (krój miasta), biała ramka — PNG w folderze nabrzeża
    static string TablicaTekstura()
    {
        try
        {
            string path = BraxNabrzeze.Out + "/ratownik_tablica.png"; const int W = 512, H = 128;
            var px = new Color[W * H]; var red = new Color(.78f, .08f, .07f); var bial = new Color(.97f, .97f, .95f);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) px[y * W + x] = (x < 6 || x >= W - 6 || y < 6 || y >= H - 6) ? bial : red;
            BraxUliceKit.NapisWTeksturze(px, W, H, "RATOWNIK", W * .5f, H * .5f - 30f, 60f, bial);
            Directory.CreateDirectory(BraxNabrzeze.Out);
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false); t.SetPixels(px); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 512; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ? path : null;
        }
        catch (System.Exception e) { BraxNabrzeze.Log("ratownik: tablica DO POPRAWKI " + e.Message); return null; }
    }

    static void Wyciag(UlSiatka s, Vector2[] pr, Vector3 o, Vector3 os, Vector3 gora, Vector3 wprzod, float dl, bool dekle = true) => BraxMiastoWyspa.Wyciag(s, pr, o, os, gora, wprzod, dl, dekle);
    static Vector2[] PZ(float w, float h, float r, Vector2 sr, int seg = 3) => BraxMiastoWyspa.PZ(w, h, r, sr, seg);
    static void Toczenie(UlSiatka s, Vector2[] pr, Vector3 p, int seg) => BraxMiastoWyspa.Toczenie(s, pr, p, seg);
}
