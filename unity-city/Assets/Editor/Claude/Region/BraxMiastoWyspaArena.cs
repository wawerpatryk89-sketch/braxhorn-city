using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// ARENA BRAXHORN PREMIUM (3.11; właściciel o starej: „z kwadratów, wypierdol”; o nowej: „Arenę można zbudować zajebistą, tak jak
// policja, szpital szklany… premium”). Standard szpitala i komisariatu (BraxRegionTylyB.Wiezowiec): ściana kurtynowa — szkło
// przezroczyste w głębi 0.18 m × k za licem lizen, lizeny aluminiowe co 1.5 m × k, rygle, pas stropowy ze szkła emaliowanego,
// cokół z granitu 0.4 m × k, attyka z gzymsem (narożniki fazowane — bez nakładania brył), okrągłe słupy narożne; drzwi
// automatyczne bez progu (podjazd 1:10). Ośmiobok R 24 m w miejscu starej areny (z mariny prowadzi tu deptak, wejście od zachodu).
// Dach: membrana PTFE między 8 kratownicami stalowymi (pas dolny, górny, krzyżulce) do pierścienia ściskanego ze świetlikiem.
// JEDNO wejście dla gości (2 pary drzwi automatycznych) → hol (kamień, kasy, bramki — model metro_bramka z paczki
// miasto_silownia_metro, bez paczki z kodu) → widownia. Pozostałe drzwi zamknięte: 2 wyjścia ewakuacyjne (płn., płd.)
// i drzwi służbowe do tunelu (wsch.), z kolizją i znakami WYJŚCIE.
// Trybuny: 6 sektorów, krzesełka kształtowane (profilowane siedzisko, gięte oparcie, noga z kotwą) — JEDNA siatka (2 materiały)
// powielona instancjami GPU, LODGroup na sektor; przejścia ze stopniami, balustrady szklane, barierki, loże VIP z fotelami
// klubowymi. Oktagon z kodu: model silownia_klatka z paczki to klatka do przysiadów (siłownia), NIE oktagon — nie nadaje się.
// Podest, fartuch, pas słupków, mata z wycięciem (strefy obok siebie, nie na sobie), słupki w osłonach, siatka z tekstury ogniw
// (maska alfa, nie nakładka), drzwiczki od tunelu ze schodkami. Telebim 4-stronny i rampa (kratownica skrzynkowa z reflektorami).
// Tunel zawodników, 2 szatnie z sufitem (LED we wnękach), szafki, ławki, stół do masażu, lustro; toalety damska i męska.
// Detale: listwy, ościeżnice, oprawy, gaśnice, znaki wyjścia, gniazdka (detale bez cieni, LODGroup). Kibice ≤ 150 na PRAWDZIWYCH
// siedziskach (format „osoba: siedzi · kibic · …” → BraxRegionRuchMiasta.LudzieZMiejsc). Log BRAXHORN_ARENA.
internal static partial class BraxMiastoWyspa
{
    static Vector3 apC; static float apY0, apF, apAp; static int apWej, apTun, apKol, apElem, apMiejsc, apKibice;
    const float ApHg = 12.0f, ApHa = 14.2f, ApColR = .35f, ApColRad = .44f, ApWew = .6f, ApSufit = 13.4f;
    static float ApG => .18f * K;
    static float ApTr1 => apY0 + 3.0f;
    static Material apSzklo, apAlu, apGranit, apPanel, apAkust, apBeton, apPosadzka, apKamien, apStal, apCzarny, apBialy, apLed, apLit, apBoki,
        apZnak, apTynk, apPlytki, apCeram, apChrom, apHpl, apSufitM, apLedSufit, apGuma, apDab, apCzerw, apLustro, apStrop;
    static readonly Dictionary<Material, UlSiatka> apDrobne = new Dictionary<Material, UlSiatka>();
    static UlSiatka D(Material m) { if (!apDrobne.TryGetValue(m, out var s)) apDrobne[m] = s = new UlSiatka(); return s; }

    static void ArenaPremium()
    {
        apDrobne.Clear(); apKol = apElem = apMiejsc = apKibice = 0;
        apC = AC; apY0 = apC.y; apC.y = 0f; apF = apY0 + .15f; apAp = ArenaR * Mathf.Cos(Mathf.PI / 8f);
        apWej = 3; float best = 1f;
        for (int i = 0; i < 8; i++) { var mid = ((Wk(i, ArenaR) + Wk(i + 1, ArenaR)) * .5f - apC).normalized; if (mid.x < best) { best = mid.x; apWej = i; } }
        apTun = (apWej + 4) % 8;
        ApMaterialy();
        void Krok(string n, System.Action a) { try { a(); } catch (System.Exception e) { BraxMiastoA.Log("WYSPA DO POPRAWKI arena premium — " + n + ": " + e); Debug.LogWarning("BRAXHORN_ARENA DO POPRAWKI " + n + ": " + e); } }
        Krok("bryła i fasada", ApBryla);
        Krok("dach z kratownicami", ApDach);
        int miejsc = 0; Krok("trybuny", () => miejsc = ApTrybuny());
        Krok("klatka", ApKlatka);
        Krok("rampa i telebim", ApRampa);
        Krok("hol (kasy, bramki)", ApHol);
        Krok("toalety", ApToalety);
        Krok("tunel i szatnie", ApSzatnie);
        Krok("plac przed wejściem", ApPlacPrzed);
        Krok("detale (zapis)", ApZapiszDrobne);
        BraxRegion.Warm(root, "Arena: światło nad klatką", apC + Vector3.up * (apY0 + 8f), 2.6f, 14f);
        foreach (int i in new[] { 1, 5, 2, 6, 0, 4 }) BraxRegion.Warm(root, "Arena: światło trybun", Wk(i, 14f) + Vector3.up * (apY0 + 9f), 1.3f, 14f);
        BraxRegion.Warm(root, "Arena: światło holu", ApHolP(18f, 0f, 4f), 1.2f, 10f);
        Miejsca["arena_srodek"] = apC + Vector3.up * (apF + .9f * K);
        int nKol = kolLicz();
        Debug.Log("BRAXHORN_ARENA premium: miejsc " + miejsc + " (krzesełka instancje GPU), kibiców " + apKibice + " (≤150), kolizji " + nKol + ", elementów " + apElem + ", miejsc ludzi/punktów " + apMiejsc + ", wejście bok " + apWej + ", tunel bok " + apTun);
        BraxMiastoA.Log("WYSPA arena PREMIUM: miejsc " + miejsc + ", kibiców " + apKibice + ", kolizji " + nKol + ", elementów " + apElem);
    }
    static int kolLicz() => kol ? kol.GetComponentsInChildren<Collider>(true).Length : apKol;

    // ---------------- geometria ośmioboku ----------------
    static Vector3 Wk(int i, float r) { float a = (i + .5f) * Mathf.PI / 4f; return new Vector3(apC.x + Mathf.Cos(a) * r, 0f, apC.z + Mathf.Sin(a) * r); }
    /// <summary>Narożnik ośmioboku przesunięty o d od lica (d &lt; 0 do środka) — fazowanie bez nakładania.</summary>
    static Vector3 Pk(int i, float d) => Wk(i, ArenaR + d / Mathf.Cos(Mathf.PI / 8f));
    static void ApBok(int i, out Vector3 a, out Vector3 b, out Vector3 Rr, out Vector3 N, out float dl)
    {
        a = Wk(i, ArenaR); b = Wk(i + 1, ArenaR); dl = Vector3.Distance(a, b); Rr = (b - a) / dl;
        N = Vector3.Cross(Rr, Vector3.up).normalized; if (Vector3.Dot(N, (a + b) * .5f - apC) < 0) N = -N;
    }
    static Vector3 ApHolP(float v, float u, float y) => new Vector3(apC.x - v, apF + y, apC.z + u);   // hol: v od środka na zachód, u = z

    static void KolA(string n, Vector3 srodek, Vector3 przod, Vector3 rozmiar)
    {
        var g = new GameObject("kolizja: " + n); g.transform.SetParent(kol, false); g.transform.position = srodek;
        przod.y = 0f; if (przod.sqrMagnitude < 1e-6f) przod = Vector3.forward;
        g.transform.rotation = Quaternion.LookRotation(przod.normalized, Vector3.up); g.AddComponent<BoxCollider>().size = rozmiar; apKol++;
    }
    static void KolAA(string n, Vector3 a, Vector3 b) { BraxMiastoA.Kol(kol, n, a, b); apKol++; }

    // ---------------- kształty (wyciągnięcie profilu, toczenie, prostokąt zaokrąglony) ----------------
    /// <summary>Profil zamknięty (a wzdłuż wprzod, b wzdłuż gora) wyciągnięty wzdłuż os na długość dl; dekle wachlarzem od środka.</summary>
    static void Wyciag(UlSiatka s, Vector2[] pr, Vector3 o, Vector3 os, Vector3 gora, Vector3 wprzod, float dl, bool dekle = true)
    {
        float pole = 0; for (int i = 0; i < pr.Length; i++) { var p = pr[i]; var q = pr[(i + 1) % pr.Length]; pole += p.x * q.y - q.x * p.y; }
        float zn = pole >= 0 ? 1f : -1f;
        Vector3 P(Vector2 p, float t) => o + wprzod * p.x + gora * p.y + os * t;
        var sr = Vector2.zero; foreach (var p in pr) sr += p; sr /= pr.Length;
        for (int i = 0; i < pr.Length; i++)
        {
            var p = pr[i]; var q = pr[(i + 1) % pr.Length]; var e = q - p; if (e.sqrMagnitude < 1e-10f) continue;
            var n2 = new Vector2(e.y, -e.x) * zn; var nn = (wprzod * n2.x + gora * n2.y).normalized;
            s.Quad(P(p, 0), P(q, 0), P(q, dl), P(p, dl), nn);
            if (dekle) { s.Tri(P(sr, 0), P(p, 0), P(q, 0), -os); s.Tri(P(sr, dl), P(q, dl), P(p, dl), os); }
        }
    }
    static Vector2[] PZ(float w, float h, float r, Vector2 sr, int seg = 3)
    {
        r = Mathf.Min(r, Mathf.Min(w, h) * .5f - .0005f); var l = new List<Vector2>();
        var c = new[] { new Vector2(w * .5f - r, h * .5f - r), new Vector2(-w * .5f + r, h * .5f - r), new Vector2(-w * .5f + r, -h * .5f + r), new Vector2(w * .5f - r, -h * .5f + r) };
        for (int k = 0; k < 4; k++) for (int j = 0; j <= seg; j++) { float a = (k * 90f + j * 90f / seg) * Mathf.Deg2Rad; l.Add(sr + c[k] + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); }
        return l.ToArray();
    }
    static Vector2[] Obrot(Vector2[] p, float stopnie, Vector2 wokol)
    {
        float c = Mathf.Cos(stopnie * Mathf.Deg2Rad), s = Mathf.Sin(stopnie * Mathf.Deg2Rad);
        return p.Select(q => { var d = q - wokol; return wokol + new Vector2(d.x * c - d.y * s, d.x * s + d.y * c); }).ToArray();
    }
    /// <summary>Bryła obrotowa: profil (r, y) od dołu przez bok po wierzch, oś pionowa w p; sx/sz — owal.</summary>
    static void Toczenie(UlSiatka s, Vector2[] pr, Vector3 p, int seg, float sx = 1f, float sz = 1f, Vector3? przod = null)
    {
        var f = przod ?? Vector3.forward; f.y = 0; f.Normalize(); var r = Vector3.Cross(Vector3.up, f);
        Vector3 Dd(float t) => r * (Mathf.Cos(t) * sx) + f * (Mathf.Sin(t) * sz);
        for (int k = 0; k < seg; k++)
        {
            float t0 = k * Mathf.PI * 2f / seg, t1 = (k + 1) * Mathf.PI * 2f / seg; var rad = Dd((t0 + t1) * .5f).normalized;
            for (int j = 0; j < pr.Length - 1; j++)
            {
                var a = pr[j]; var b = pr[j + 1]; var e = b - a; if (e.sqrMagnitude < 1e-10f || (a.x < 1e-4f && b.x < 1e-4f)) continue;
                var nn = rad * e.y - Vector3.up * e.x;
                s.Quad(p + Dd(t0) * a.x + Vector3.up * a.y, p + Dd(t1) * a.x + Vector3.up * a.y, p + Dd(t1) * b.x + Vector3.up * b.y, p + Dd(t0) * b.x + Vector3.up * b.y, nn);
            }
        }
    }

    // ---------------- materiały ----------------
    static void ApMaterialy()
    {
        apSzklo = BraxMiastoA.Mat("Arena premium: szyba fasady (przezroczysta)", new Color(.70f, .80f, .84f, .22f), .06f, .1f);
        apAlu = BraxMiastoA.Mat("Arena premium: aluminium lizen (anodowane)", new Color(.56f, .58f, .60f), .35f, .8f);
        apGranit = BraxMiastoA.Tex("Arena premium: granit cokołu", 1.2f, new Color(.55f, .55f, .54f), new Color(.45f, .45f, .45f), .7f, "granite_tile_02", "rough_block_wall");
        apPanel = BraxMiastoA.Mat("Arena premium: szkło emaliowane grafit (pas, attyka)", new Color(.16f, .19f, .22f), .3f, .3f);
        apAkust = BraxMiastoA.Mat("Arena premium: panele akustyczne ścian (grafit)", new Color(.12f, .125f, .13f), .9f);
        apStrop = BraxMiastoA.Mat("Arena premium: strop hali (czarny akustyczny)", new Color(.035f, .035f, .04f), .95f);
        apBeton = BraxMiastoA.Tex("Arena premium: beton prefabrykatów trybun", 2.4f, new Color(.80f, .80f, .78f), new Color(.55f, .55f, .54f), .85f, "concrete_wall_008", "concrete_floor_02");
        apPosadzka = BraxMiastoA.Mat("Arena premium: posadzka hali (żywica grafit)", new Color(.15f, .15f, .16f), .5f);
        apKamien = BraxMiastoA.Tex("Arena premium: posadzka holu (kamień)", 1.0f, new Color(.86f, .85f, .83f), new Color(.75f, .74f, .72f), .5f, "marble_01", "large_grey_tiles", "granite_tile_02");
        apStal = BraxMiastoA.Mat("Arena premium: stal kratownic (grafit)", new Color(.17f, .18f, .19f), .45f, .7f);
        apCzarny = BraxMiastoA.Mat("Arena premium: czarny mat (klatka, obudowy)", new Color(.04f, .04f, .045f), .5f, .3f);
        apBialy = BraxMiastoA.Mat("Arena premium: biel (osprzęt)", new Color(.93f, .93f, .91f), .4f);
        apLed = BraxMiastoA.Swieci("Arena: pas LED zielony (marka)", new Color(.45f, 1f, .2f), new Color(.8f, 2.2f, .3f));
        apLit = BraxMiastoA.Swieci("Arena: litery szyldu (podświetlane)", Color.white, new Color(2f, 2f, 2f));
        apBoki = BraxMiastoA.Mat("Arena: boki liter (grafit)", new Color(.05f, .05f, .06f), .45f, .6f);
        apZnak = BraxMiastoA.Swieci("Arena premium: znak wyjścia (zielony)", new Color(.05f, .7f, .25f), new Color(.1f, 1.4f, .45f));
        apTynk = BraxMiastoA.Mat("Arena premium: ściany wnętrz (farba biała)", new Color(.9f, .9f, .88f), .9f);
        apPlytki = BraxMiastoA.Tex("Arena WC: płytki ścian", .6f, new Color(.96f, .96f, .95f), new Color(.92f, .92f, .9f), .3f, "square_tiles", "rectangular_facade_tiles", "granite_tile");
        apCeram = BraxMiastoA.Mat("Arena premium: ceramika sanitarna", new Color(.96f, .96f, .95f), .15f);
        apChrom = BraxMiastoA.Mat("Arena premium: chrom", new Color(.84f, .85f, .87f), .12f, .95f);
        apHpl = BraxMiastoA.Mat("Arena premium: płyty HPL kabin (grafit)", new Color(.16f, .17f, .18f), .5f, .1f);
        apSufitM = BraxMiastoA.Mat("Arena premium: sufit podwieszany (biały)", new Color(.92f, .92f, .9f), .9f);
        apLedSufit = BraxMiastoA.Swieci("Arena premium: panel LED sufitowy", new Color(.95f, .95f, .92f), new Color(1.6f, 1.6f, 1.5f));
        apGuma = BraxMiastoA.Mat("Arena premium: posadzka sportowa (guma grafit)", new Color(.12f, .12f, .13f), .9f);
        apDab = BraxMiastoA.Mat("Arena premium: dąb olejowany (ławki, blaty)", new Color(.62f, .48f, .32f), .55f);
        apCzerw = BraxMiastoA.Mat("Arena premium: czerwień gaśnic (lakier)", new Color(.75f, .06f, .05f), .35f);
        apLustro = BraxMiastoA.Mat("Arena premium: lustro", new Color(.8f, .85f, .88f), .02f, 1f);
    }

    // =====================================================================================================================
    // BRYŁA: fasada kurtynowa jak szpital, słupy narożne, attyka i gzyms fazowane, ściana wewnętrzna, posadzka, strop, kolizje
    static void ApBryla()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — bryła i fasada");
        float y0 = apY0, F = apF, g = ApG, cok = .4f * K, yG = y0 + ApHg, yA = y0 + ApHa, yC = y0 + ApSufit;
        float tr1 = ApTr1, sp0 = y0 + 6.0f, sp1 = y0 + 6.8f, tr2 = y0 + 9.4f, lzF = .14f * K, lzW = .155f, hd = 2.81f;
        float tg = Mathf.Tan(Mathf.PI / 8f);
        var szk = BraxUliceKit.S(apSzklo); var al = BraxUliceKit.S(apAlu); var pn = BraxUliceKit.S(apPanel); var gr = BraxUliceKit.S(apGranit);
        for (int i = 0; i < 8; i++)
        {
            ApBok(i, out var a, out var b, out var Rr, out var N, out float dl);
            Vector3 P(float s, float y, float d) => a + Rr * s + Vector3.up * y + N * d;
            const int nBay = 9; float bay = (dl - 2f * ApColR) / nBay;
            bool wej = i == apWej, ewak = i == (apWej + 2) % 8 || i == (apWej + 6) % 8, sluz = i == apTun;
            float S(int k) => ApColR + k * bay;
            bool Portal(int k) => wej && k >= 3 && k <= 5;
            bool DrzwiB(int k) => (ewak || sluz) && k == 4;
            // przęsła: szyby (2 cm w głębi g), rygle, pas stropowy (grafit)
            for (int k = 0; k < nBay; k++)
            {
                float u0 = S(k) + lzW * .5f + .001f, u1 = S(k + 1) - lzW * .5f - .001f, w = u1 - u0, um = (u0 + u1) * .5f;
                if (k == 0) u0 = .3f; if (k == nBay - 1) u1 = dl - .3f;   // skraj przęsła w słupie narożnym (bez szczeliny) w = u1 - u0; um = (u0 + u1) * .5f;
                void Szyba(float ya, float yb) { if (yb - ya > .02f) szk.OBox(P(um, (ya + yb) * .5f, -g - .01f), Rr, Vector3.up, N, w, yb - ya, .02f); }
                if (!(Portal(k) || DrzwiB(k))) Szyba(y0 + cok, tr1 - .04f);
                Szyba(tr1 + .04f, sp0); Szyba(sp1, tr2 - .04f); Szyba(tr2 + .04f, yG);
                foreach (float yt in new[] { tr1, tr2 }) al.OBox(P(um, yt, -g + .01f), Rr, Vector3.up, N, w, .08f, .1f);
                pn.OBox(P(um, (sp0 + sp1) * .5f, -g * .5f - .021f), Rr, Vector3.up, N, w, sp1 - sp0, g - .02f);
            }
            // lizeny (na całą wysokość szkła; nad wejściem od rygla)
            for (int k = 1; k < nBay; k++)
            {
                float yb = wej && (k == 4 || k == 5) ? tr1 + .04f : (wej && (k == 3 || k == 6)) || ((ewak || sluz) && (k == 4 || k == 5)) ? y0 : y0 + cok;
                al.OBox(P(S(k), (yb + yG) * .5f, (lzF - g - .03f) * .5f), Rr, Vector3.up, N, lzW, yG - yb, lzF + g + .03f);
            }
            // cokół granitowy z przerwami na drzwi
            var przerwy = new List<(float, float)>();
            if (wej) przerwy.Add((S(3), S(6))); if (ewak || sluz) przerwy.Add((S(4), S(5)));
            float c0 = 0f;
            foreach (var (pa, pb) in przerwy.Concat(new[] { (dl, dl) }))
            {
                if (pa - c0 > .05f) gr.OBox(P((c0 + pa) * .5f, y0 - .05f + (cok + .05f) * .5f, -.125f), Rr, Vector3.up, N, pa - c0, cok + .05f, .35f);
                c0 = pb;
            }
            // attyka (szkło emaliowane) i gzyms aluminiowy — pryzmy fazowane w narożnikach
            void Pryzma(UlSiatka sm, float d0, float d1, float ya, float yb, bool spod, bool tyl)
            {
                Vector3 A0 = Pk(i, d0), B0 = Pk(i + 1, d0), A1 = Pk(i, d1), B1 = Pk(i + 1, d1);
                sm.Quad(A1 + Vector3.up * ya, B1 + Vector3.up * ya, B1 + Vector3.up * yb, A1 + Vector3.up * yb, N);
                if (tyl) sm.Quad(A0 + Vector3.up * ya, B0 + Vector3.up * ya, B0 + Vector3.up * yb, A0 + Vector3.up * yb, -N);
                sm.Quad(A0 + Vector3.up * yb, B0 + Vector3.up * yb, B1 + Vector3.up * yb, A1 + Vector3.up * yb, Vector3.up);
                if (spod) sm.Quad(A0 + Vector3.up * ya, B0 + Vector3.up * ya, B1 + Vector3.up * ya, A1 + Vector3.up * ya, Vector3.down);
            }
            Pryzma(pn, -ApWew, lzF, yG, yA, true, true);
            Pryzma(al, -ApWew, lzF + .32f, yA, yA + .22f, true, false);
            BraxUliceKit.S(apLed).OBox(P(dl * .5f, yG + .35f, lzF + .02f), Rr, Vector3.up, N, dl - 2f * ApColRad - .2f, .08f, .04f, true);
            // słup narożny (aluminium polerowane) — zakrywa styk przęseł dwóch boków
            al.Rura(Wk(i, ArenaR) + Vector3.up * (y0 - .05f), Wk(i, ArenaR) + Vector3.up * (yA + .22f), ApColRad, 18);
            // ściana wewnętrzna (grafit akustyczny) 0.6 m za licem — oprócz boku wejścia (hol widać przez szkło)
            float dIn = dl - 2f * ApWew * tg; float sIn(float s) => s - ApWew * tg;
            var otwIn = new List<Rect>(); if (ewak || sluz) otwIn.Add(Rect.MinMaxRect(sIn(S(4) + lzW * .5f), F, sIn(S(5) - lzW * .5f), F + hd));
            if (!wej)
            {
                var A = Pk(i, -ApWew); var B = Pk(i + 1, -ApWew);
                BraxMiastoA.Sciana(BraxUliceKit.S(apAkust), A, Rr, N, dIn, F, yG, otwIn);
                var otwM = otwIn.Select(o => Rect.MinMaxRect(dIn - o.xMax, o.yMin, dIn - o.xMin, o.yMax)).ToList();
                BraxMiastoA.Sciana(BraxUliceKit.S(apAkust), B, -Rr, -N, dIn, F, yG, otwM, ApWew - g - .02f, BraxUliceKit.S(apAlu));
            }
            // kolizje ściany (między szkłem a ścianą wewn.) z przerwami na drzwi
            var otwK = new List<(float a, float b, float h)>();
            if (wej) otwK.Add((S(3) + lzW * .5f, S(6) - lzW * .5f, tr1 - y0)); if (ewak || sluz) otwK.Add((S(4) + lzW * .5f, S(5) - lzW * .5f, F + hd - y0));
            float k0 = 0f;
            foreach (var (oa, ob, oh) in otwK.OrderBy(o => o.a).Select(o => (o.a, o.b, o.h)).Concat(new[] { (dl, dl, 0f) }))
            {
                if (oa - k0 > .05f) KolA("ściana areny " + i, P((k0 + oa) * .5f, y0 + ApHa * .5f, -.42f), N, new Vector3(oa - k0, ApHa, .5f));
                if (ob - oa > .05f) KolA("nadproże areny " + i, P((oa + ob) * .5f, y0 + oh + (ApHa - oh) * .5f, -.42f), N, new Vector3(ob - oa, ApHa - oh, .5f));
                k0 = ob;
            }
            // posadzka: wachlarz do szkła; w sektorze wejścia hol z kamienia od linii bramek
            Vector3 Q0 = Pk(i, -.25f) + Vector3.up * F, Q1 = Pk(i + 1, -.25f) + Vector3.up * F, Cc = apC + Vector3.up * F;
            if (wej)
            {
                float f = 13.8f / (apAp - .25f); var M0 = Vector3.Lerp(Cc, Q0, f); var M1 = Vector3.Lerp(Cc, Q1, f);
                BraxUliceKit.S(apPosadzka).Tri(Cc, M1, M0, Vector3.up); BraxUliceKit.S(apKamien).Quad(M0, M1, Q1, Q0, Vector3.up);
            }
            else BraxUliceKit.S(apPosadzka).Tri(Cc, Q1, Q0, Vector3.up);
            BraxUliceKit.S(apStrop).Tri(apC + Vector3.up * yC, Pk(i, -ApWew) + Vector3.up * yC, Pk(i + 1, -ApWew) + Vector3.up * yC, Vector3.down);
            // DRZWI
            if (wej) ApWejscie(a, Rr, N, dl, S(3) + lzW * .5f, S(6) - lzW * .5f);
            if (ewak || sluz) ApDrzwiZamkniete(P((S(4) + S(5)) * .5f, F, -g - .01f), Rr, N, S(5) - S(4) - lzW, hd, sluz ? "DRZWI SŁUŻBOWE (tunel zawodników)" : "WYJŚCIE EWAKUACYJNE",
                Pk(i, -ApWew) + Rr * sIn((S(4) + S(5)) * .5f) + Vector3.up * (F + hd + .25f), -N, !sluz);
            apElem++;
        }
        // kolizja posadzki (wypukły ośmiobok), stropu
        var fm = new UlSiatka(); for (int i = 0; i < 8; i++) { var a = Pk(i, -.1f); var b = Pk(i + 1, -.1f); fm.Quad(a + Vector3.up * (y0 - .1f), b + Vector3.up * (y0 - .1f), b + Vector3.up * F, a + Vector3.up * F, Vector3.up); }
        var fmesh = fm.Zbuduj("A: arena premium — posadzka (kolizja)"); BraxZapisAssetu.Utworz(fmesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/arena_posadzka_kolizja.asset"));
        var kd = new GameObject("kolizja: posadzka areny"); kd.transform.SetParent(kol, false); var kdm = kd.AddComponent<MeshCollider>(); kdm.sharedMesh = fmesh; kdm.convex = true; apKol++;
        var ks = new GameObject("kolizja: strop areny"); ks.transform.SetParent(kol, false); ks.transform.position = apC + Vector3.up * (yC + .2f); ks.AddComponent<BoxCollider>().size = new Vector3(ArenaR * 2f, .4f, ArenaR * 2f); apKol++;
    }

    // wejście dla gości: 2 pary drzwi automatycznych, słupek środkowy, podjazd 1:10, daszek szklany, litery ARENA BRAXHORN
    static void ApWejscie(Vector3 a, Vector3 Rr, Vector3 N, float dl, float p0, float p1)
    {
        float y0 = apY0, F = apF, g = ApG, hd = 2.81f, ms = .25f, dw = (p1 - p0 - ms) * .5f;
        Vector3 P(float s, float y, float d) => a + Rr * s + Vector3.up * y + N * d;
        var al = BraxUliceKit.S(apAlu);
        foreach (float sc in new[] { p0 + dw * .5f, p1 - dw * .5f })
            BraxMiastoA.DrzwiPrzesuwne(root, "ARENA BRAXHORN — wejście", P(sc, F, -g - .01f), Rr, dw, hd, BraxUliceKit.SzkloPrzezroczyste, apAlu);
        al.OBox(P((p0 + p1) * .5f, (y0 + ApTr1 - .04f) * .5f, -g + .03f), Rr, Vector3.up, N, ms, ApTr1 - .04f - y0, .26f);
        KolA("słupek drzwi wejścia", P((p0 + p1) * .5f, (y0 + ApTr1) * .5f, -g), N, new Vector3(ms, ApTr1 - y0, .3f));
        // podjazd 1:10 (granit), szerokość portalu
        var gr = BraxUliceKit.S(apGranit); float L = 1.6f;
        var m0 = P(p0, y0, -.25f); var m1 = P(p1, y0, -.25f);
        gr.Quad(m0 + N * (L + .25f), m1 + N * (L + .25f), m1 + Vector3.up * .15f, m0 + Vector3.up * .15f, (Vector3.up * L + N * .15f).normalized);
        gr.Tri(m0 + N * (L + .25f), m0 + Vector3.up * .15f, m0, -Rr); gr.Tri(m1 + N * (L + .25f), m1, m1 + Vector3.up * .15f, Rr);
        var r = new GameObject("kolizja: podjazd wejścia areny"); r.transform.SetParent(kol, false);
        r.transform.position = P((p0 + p1) * .5f, y0 + .05f, L * .5f - .1f); r.transform.rotation = Quaternion.LookRotation(N, Vector3.up) * Quaternion.Euler(Mathf.Atan2(.15f, L) * Mathf.Rad2Deg, 0, 0);
        r.AddComponent<BoxCollider>().size = new Vector3(p1 - p0, .1f, L + .5f); apKol++;
        // daszek: szkło na wspornikach stalowych (profil zbieżny) i cięgnach
        float yd = y0 + 3.9f, wD = p1 - p0 + 3.2f, wys = 3.6f, sm = (p0 + p1) * .5f;
        var st = BraxUliceKit.S(apStal);
        for (int q = 0; q <= 4; q++)
        {
            float s = sm - wD * .5f + .3f + q * (wD - .6f) / 4f;
            var prof = new[] { new Vector2(0f, -.34f), new Vector2(wys, -.08f), new Vector2(wys, 0f), new Vector2(0f, 0f) };
            Wyciag(st, prof, P(s - .06f, yd, .0f + lzFace()), Rr, Vector3.up, N, .12f);
            st.Rura(P(s, yd + 2.6f, lzFace()), P(s, yd + .02f, wys - .25f + lzFace()), .025f, 8);
        }
        D(BraxUliceKit.SzkloPrzezroczyste).OBox(P(sm, yd + .012f, wys * .5f + lzFace() + .02f), Rr, Vector3.up, N, wD, .024f, wys - .04f);
        D(apLed).OBox(P(sm, yd - .2f, wys + lzFace() + .021f), Rr, Vector3.up, N, wD - .6f, .06f, .04f, true);
        float cap = .42f, w = BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN", cap); if (w > wD - .6f) cap *= (wD - .6f) / w;
        float dL = wys - .45f + lzFace(); D(apAlu).OBox(P(sm, yd + .024f + (cap + .14f) * .5f, dL - .01f), Rr, Vector3.up, N, BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN", cap) + .3f, cap + .14f, .02f);
        BraxUliceKit.Napis3D("ARENA BRAXHORN", P(sm, yd + .03f + .05f, dL), N, cap, .08f, apLit, apBoki);
        // litery na attyce nad wejściem
        float capA = 1.3f, wA = BraxUliceKit.SzerokoscNapisu("ARENA BRAXHORN", capA); if (wA > dl - 2.4f) capA *= (dl - 2.4f) / wA;
        BraxUliceKit.Napis3D("ARENA BRAXHORN", P(dl * .5f, y0 + ApHg + .5f, lzFace()), N, capA, .22f, apLit, apBoki);
        // znak WYJŚCIE nad drzwiami od środka (na ryglu)
        ApZnakWyjscia(P(sm, ApTr1 + .3f, -g - .06f), -N);
        Miejsca["arena_wejscie"] = P(sm, y0, 3f); apMiejsc++;
        foreach (float s in new[] { sm - 2.4f, sm + 2.4f }) Czlowiek("Wejście ARENY", "ochrona", "stoi", P(s, y0, 2.2f), N, KlStoi);
    }
    static float lzFace() => .14f * K;

    // drzwi zamknięte (ewakuacyjne / służbowe): dwa skrzydła stalowe w ościeżnicy, pochwyt antypaniczny od środka, kolizja, znak
    static void ApDrzwiZamkniete(Vector3 c, Vector3 Rr, Vector3 N, float w, float h, string nazwa, Vector3 znak, Vector3 znakN, bool zZnakiem)
    {
        var lak = D(BraxMiastoA.Mat("Arena premium: drzwi stalowe (lakier grafit)", new Color(.2f, .21f, .22f), .45f, .3f)); var ch = D(apChrom);
        foreach (float sg in new[] { -1f, 1f })
        {
            var sc = c + Rr * (sg * w * .25f) + Vector3.up * (h * .5f);
            lak.OBox(sc, Rr, Vector3.up, N, w * .5f - .012f, h - .01f, .05f);
            var pz = sc - N * .1f + Vector3.down * (h * .5f - 1.0f * K);
            ch.Rura(pz - Rr * (w * .2f), pz + Rr * (w * .2f), .02f, 8);
            foreach (float e in new[] { -1f, 1f }) ch.Rura(pz + Rr * (e * w * .19f), pz + Rr * (e * w * .19f) + N * .075f, .012f, 6);
        }
        KolA(nazwa, c + Vector3.up * (h * .5f), N, new Vector3(w, h, .12f));
        if (zZnakiem) ApZnakWyjscia(znak, znakN);
        apElem++;
    }

    static void ApZnakWyjscia(Vector3 p, Vector3 n)
    {
        n.y = 0; n.Normalize(); var r = Vector3.Cross(Vector3.up, n);
        D(apZnak).OBox(p + n * .015f, r, Vector3.up, n, .44f, .18f, .03f, true);
        BraxUliceKit.Napis3D("WYJŚCIE", p + n * .03f + Vector3.down * .045f, n, .085f, .006f, apLit, apBoki);
        apElem++;
    }
    static void ApGasnica(Vector3 p, Vector3 n)   // p: punkt na ścianie (wys. uchwytu), n: lico
    {
        n.y = 0; n.Normalize(); var r = Vector3.Cross(Vector3.up, n); float s = K;
        var b = p + n * (.1f * s) + Vector3.down * (.5f * s);
        Toczenie(D(apCzerw), new[] { new Vector2(0, 0), new Vector2(.072f * s, 0), new Vector2(.078f * s, .02f * s), new Vector2(.078f * s, .40f * s), new Vector2(.06f * s, .47f * s), new Vector2(.022f * s, .5f * s), new Vector2(0, .5f * s) }, b, 14);
        D(apCzarny).Rura(b + Vector3.up * .5f * s, b + Vector3.up * .58f * s, .018f * s, 8);
        D(apCzarny).Rura(b + Vector3.up * .57f * s, b + Vector3.up * .57f * s + r * .1f * s, .012f * s, 6);
        D(apCzarny).Rura(b + Vector3.up * .54f * s - r * .02f, b + Vector3.up * .2f * s - r * .09f * s, .011f * s, 6);
        D(apChrom).Rura(p - n * .002f + Vector3.down * .2f * s, p + n * .03f * s + Vector3.down * .2f * s, .03f * s, 8);
        apElem++;
    }
    static void ApGniazdko(Vector3 p, Vector3 n)
    {
        n.y = 0; n.Normalize(); var r = Vector3.Cross(Vector3.up, n); float w = .085f * K;
        Wyciag(D(apBialy), PZ(w, w, .012f, Vector2.zero), p, n, Vector3.up, r, .011f);
        D(apBialy).Rura(p + n * .011f, p + n * .017f, w * .3f, 12);
        apElem++;
    }

    // ściana pomieszczenia w osiach świata (wzdX — wzdłuż x na z = cc, inaczej wzdłuż z na x = cc), otwory (od, do, wys.) w osi a;
    // lico „−” i „+” osobnymi materiałami, ościeża po stronie „+”, wierzch, czoła, kolizje, listwy i ościeżnice
    static void ApSciana(bool wzdX, float cc, float a0, float a1, float t, float y0, float y1, (float a, float b, float h)[] otw, Material mMinus, Material mPlus, bool listwy = true)
    {
        var rects = new List<Rect>(); if (otw != null) foreach (var o in otw) rects.Add(Rect.MinMaxRect(o.a - a0, y0, o.b - a0, y0 + o.h));
        Vector3 R = wzdX ? Vector3.right : Vector3.forward, Nn = wzdX ? Vector3.forward : Vector3.right;
        Vector3 A(float d) => wzdX ? new Vector3(a0, 0, cc + d) : new Vector3(cc + d, 0, a0);
        float L = a1 - a0;
        BraxMiastoA.Sciana(BraxUliceKit.S(mPlus), A(t * .5f), R, Nn, L, y0, y1, rects, t, BraxUliceKit.S(mPlus));
        BraxMiastoA.Sciana(BraxUliceKit.S(mMinus), A(-t * .5f), R, -Nn, L, y0, y1, rects);
        var sg = BraxUliceKit.S(mPlus);
        sg.Quad(A(-t * .5f) + Vector3.up * y1, A(t * .5f) + Vector3.up * y1, A(t * .5f) + R * L + Vector3.up * y1, A(-t * .5f) + R * L + Vector3.up * y1, Vector3.up);
        sg.Quad(A(-t * .5f) + Vector3.up * y0, A(t * .5f) + Vector3.up * y0, A(t * .5f) + Vector3.up * y1, A(-t * .5f) + Vector3.up * y1, -R);
        sg.Quad(A(-t * .5f) + R * L + Vector3.up * y0, A(t * .5f) + R * L + Vector3.up * y0, A(t * .5f) + R * L + Vector3.up * y1, A(-t * .5f) + R * L + Vector3.up * y1, R);
        Vector3 W(float a, float y, float d) => wzdX ? new Vector3(a, y, cc + d) : new Vector3(cc + d, y, a);
        float k0 = a0; var sorted = (otw ?? new (float, float, float)[0]).OrderBy(o => o.a).ToList();
        var lst = D(BraxMiastoA.Mat("Arena premium: listwy przypodłogowe (grafit)", new Color(.14f, .14f, .15f), .5f));
        var osc = D(BraxMiastoA.Mat("Arena premium: ościeżnice (stal szczotkowana)", new Color(.6f, .61f, .62f), .35f, .8f));
        foreach (var o in sorted.Concat(new[] { (a1, a1, 0f) }))
        {
            if (o.Item1 - k0 > .05f)
            {
                KolAA("ściana pomieszczenia", W(k0, y0, -t * .5f), W(o.Item1, y1, t * .5f));
                if (listwy) foreach (float sd in new[] { -1f, 1f }) lst.OBox(W((k0 + o.Item1) * .5f, y0 + .04f, sd * (t * .5f + .006f)), R, Vector3.up, Nn * sd, o.Item1 - k0, .08f, .012f, true);
            }
            if (o.Item2 - o.Item1 > .05f)
            {
                KolAA("nadproże", W(o.Item1, y0 + o.Item3, -t * .5f), W(o.Item2, y1, t * .5f));
                foreach (float sd in new[] { -1f, 1f })   // ościeżnica: opaski na obu licach (nie w płaszczyźnie ściany — 1 cm przed nią)
                {
                    var nn = Nn * sd; float dd = sd * (t * .5f + .006f);
                    osc.OBox(W(o.Item1 - .03f, y0 + o.Item3 * .5f, dd), R, Vector3.up, nn, .06f, o.Item3, .012f, true);
                    osc.OBox(W(o.Item2 + .03f, y0 + o.Item3 * .5f, dd), R, Vector3.up, nn, .06f, o.Item3, .012f, true);
                    osc.OBox(W((o.Item1 + o.Item2) * .5f, y0 + o.Item3 + .03f, dd), R, Vector3.up, nn, o.Item2 - o.Item1 + .12f, .06f, .012f, true);
                }
            }
            k0 = Mathf.Max(k0, o.Item2);
        }
        apElem++;
    }
    // drzwi wewnętrzne otwarte 90° (skrzydło przy ścianie, klamka), zawias w a, skrzydło w stronę sd (±1 wzdłuż normalnej)
    static void ApSkrzydlo(bool wzdX, float cc, float a, float w, float h, float sd, float kier, Material m)
    {
        Vector3 R = wzdX ? Vector3.right : Vector3.forward, Nn = wzdX ? Vector3.forward : Vector3.right;
        var zaw = wzdX ? new Vector3(a, apF, cc) : new Vector3(cc, apF, a);
        var c = zaw + R * (kier * .03f) + Nn * (sd * (w * .5f + .05f)) + Vector3.up * (h * .5f);
        D(m).OBox(c, Nn, Vector3.up, R, w - .02f, h - .02f, .04f);
        var kl = c + Nn * (sd * (w * .5f - .1f)) + Vector3.down * (h * .5f - 1.05f * K);
        foreach (float e in new[] { -1f, 1f }) { D(apChrom).Rura(kl, kl + R * (e * .06f), .01f, 6); D(apChrom).Rura(kl + R * (e * .06f), kl + R * (e * .06f) - Nn * (sd * .11f), .009f, 6); }
        KolA("skrzydło drzwi", c, R, new Vector3(w - .02f, h, .05f));
    }
    // sufit z wnękami na panele LED (płaszczyzna sufitu wycięta, panel 3 cm wyżej, ościeża wnęki) + daszek pomieszczenia (wierzch i opaska)
    static void ApSufitPom(float x0, float x1, float z0, float z1, float y, float yTop, List<Rect> panele, float ox0, float ox1, float oz0, float oz1)
    {
        var xs = new SortedSet<float> { x0, x1 }; var zs = new SortedSet<float> { z0, z1 };
        foreach (var p in panele) { xs.Add(p.xMin); xs.Add(p.xMax); zs.Add(p.yMin); zs.Add(p.yMax); }
        var X = xs.ToArray(); var Z = zs.ToArray(); var s = BraxUliceKit.S(apSufitM);
        for (int i = 0; i < X.Length - 1; i++) for (int j = 0; j < Z.Length - 1; j++)
        {
            var cc = new Vector2((X[i] + X[i + 1]) * .5f, (Z[j] + Z[j + 1]) * .5f); if (panele.Any(p => p.Contains(cc))) continue;
            s.Quad(new Vector3(X[i], y, Z[j]), new Vector3(X[i + 1], y, Z[j]), new Vector3(X[i + 1], y, Z[j + 1]), new Vector3(X[i], y, Z[j + 1]), Vector3.down);
        }
        foreach (var p in panele)
        {
            float yl = y + .03f;
            D(apLedSufit).Quad(new Vector3(p.xMin, yl, p.yMin), new Vector3(p.xMax, yl, p.yMin), new Vector3(p.xMax, yl, p.yMax), new Vector3(p.xMin, yl, p.yMax), Vector3.down);
            var o = D(apAlu);
            o.Quad(new Vector3(p.xMin, y, p.yMin), new Vector3(p.xMax, y, p.yMin), new Vector3(p.xMax, yl, p.yMin), new Vector3(p.xMin, yl, p.yMin), Vector3.forward);
            o.Quad(new Vector3(p.xMin, y, p.yMax), new Vector3(p.xMax, y, p.yMax), new Vector3(p.xMax, yl, p.yMax), new Vector3(p.xMin, yl, p.yMax), Vector3.back);
            o.Quad(new Vector3(p.xMin, y, p.yMin), new Vector3(p.xMin, y, p.yMax), new Vector3(p.xMin, yl, p.yMax), new Vector3(p.xMin, yl, p.yMin), Vector3.right);
            o.Quad(new Vector3(p.xMax, y, p.yMin), new Vector3(p.xMax, y, p.yMax), new Vector3(p.xMax, yl, p.yMax), new Vector3(p.xMax, yl, p.yMin), Vector3.left);
            apElem++;
        }
        var b = BraxUliceKit.S(apAkust);
        b.Quad(new Vector3(ox0, yTop, oz0), new Vector3(ox1, yTop, oz0), new Vector3(ox1, yTop, oz1), new Vector3(ox0, yTop, oz1), Vector3.up);
        b.Quad(new Vector3(ox0, y, oz0), new Vector3(ox1, y, oz0), new Vector3(ox1, yTop, oz0), new Vector3(ox0, yTop, oz0), Vector3.back);
        b.Quad(new Vector3(ox0, y, oz1), new Vector3(ox1, y, oz1), new Vector3(ox1, yTop, oz1), new Vector3(ox0, yTop, oz1), Vector3.forward);
        b.Quad(new Vector3(ox0, y, oz0), new Vector3(ox0, y, oz1), new Vector3(ox0, yTop, oz1), new Vector3(ox0, yTop, oz0), Vector3.left);
        b.Quad(new Vector3(ox1, y, oz0), new Vector3(ox1, y, oz1), new Vector3(ox1, yTop, oz1), new Vector3(ox1, yTop, oz0), Vector3.right);
        KolAA("dach pomieszczenia", new Vector3(ox0, y, oz0), new Vector3(ox1, yTop, oz1));
    }

    // =====================================================================================================================
    // DACH: membrana PTFE w siodło między 8 kratownicami (pas dolny po żebrze, pas górny, krzyżulce), belka obwodowa, pierścień, świetlik
    static void ApDach()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — dach");
        float yE = apY0 + 13.9f, rE = (apAp - ApWew - .02f) / Mathf.Cos(Mathf.PI / 8f), rP = 6.5f, hP = 3.6f, ug = .9f; const int ns = 8, nt = 8;
        var sm = BraxUliceKit.S(BraxMiastoA.Mat("Arena: membrana dachu (PTFE)", new Color(.93f, .93f, .91f), .5f)); var st = BraxUliceKit.S(apStal);
        for (int i = 0; i < 8; i++)
        {
            int ii = i;
            Vector3 Mb(float s, float t)
            {
                var q = Vector3.Lerp(Vector3.Lerp(Wk(ii, rE), Wk(ii + 1, rE), s), Vector3.Lerp(Wk(ii, rP), Wk(ii + 1, rP), s), t);
                q.y = yE + t * hP - ug * Mathf.Sin(Mathf.PI * s) * Mathf.Sin(Mathf.PI * t); return q;
            }
            for (int a = 0; a < ns; a++) for (int b = 0; b < nt; b++)
            {
                Vector3 p00 = Mb(a / (float)ns, b / (float)nt), p10 = Mb((a + 1) / (float)ns, b / (float)nt), p11 = Mb((a + 1) / (float)ns, (b + 1) / (float)nt), p01 = Mb(a / (float)ns, (b + 1) / (float)nt);
                var nn = Vector3.Cross(p10 - p00, p01 - p00).normalized; if (nn.y < 0f) nn = -nn; sm.Quad(p00, p10, p11, p01, nn);
            }
            // kratownica nad żebrem: pas dolny (na żebrze), pas górny, krzyżulce (Warren)
            const int np = 10; Vector3 Dn(float t) => Mb(0f, t) + Vector3.up * .14f; Vector3 Up(float t) => Dn(t) + Vector3.up * (1.5f - .7f * t);
            st.Rura(Dn(0), Dn(1), .1f, 8); st.Rura(Up(0), Up(1), .09f, 8);
            for (int k = 0; k < np; k++) { float t0 = k / (float)np, t1 = (k + 1) / (float)np; st.Rura(Dn(t0), Up(t0), .045f, 6, false); st.Rura(Up(t0), Dn(t1), .045f, 6, false); }
            st.Rura(Dn(1), Up(1), .045f, 6, false); st.Rura(Dn(0), Dn(0) + Vector3.down * .6f, .12f, 8);
            st.Rura(Wk(i, rE) + Vector3.up * (yE + .05f), Wk(i + 1, rE) + Vector3.up * (yE + .05f), .22f, 8);
            st.Rura(Wk(i, rP) + Vector3.up * (yE + hP + .05f), Wk(i + 1, rP) + Vector3.up * (yE + hP + .05f), .28f, 8);
            // świetlik: szkło na szprosach
            var top = apC + Vector3.up * (yE + hP + 1.7f);
            BraxUliceKit.S(apSzklo).Tri(Wk(i, rP) + Vector3.up * (yE + hP + .33f), Wk(i + 1, rP) + Vector3.up * (yE + hP + .33f), top, Vector3.up);
            st.Rura(Wk(i, rP) + Vector3.up * (yE + hP + .3f), top, .05f, 6);
        }
        apElem += 8;
    }

    // =====================================================================================================================
    // KRZESEŁKO TRYBUN: profilowane siedzisko, gięte oparcie (niecka), noga z kotwą i poprzeczką — jedna siatka, 2 podsiatki
    static Mesh ApKrzeselko()
    {
        var pl = new UlSiatka(); var me = new UlSiatka(); float h = .45f * K, bw = .44f * K * .5f, sw = .46f * K;
        // siedzisko: profil z niecką i zaokrąglonym noskiem (budżet telefonu: ~150 trójkątów na krzesełko)
        var prof = new[] { new Vector2(-.25f, h - .032f), new Vector2(-.2f, h - .01f), new Vector2(.12f, h - .002f), new Vector2(.22f, h), new Vector2(.262f, h - .024f), new Vector2(.245f, h - .056f), new Vector2(-.24f, h - .05f) };
        Wyciag(pl, prof, new Vector3(-sw * .5f, 0, 0), Vector3.right, Vector3.up, Vector3.forward, sw);
        // oparcie: powłoka gięta (wklęsła ku siedzącemu), grubość 3 cm, pochylona do tyłu
        const int nu = 6, nv = 2; float gr = .03f;
        Vector3 Op(float s, float t) => new Vector3(s * bw, h + .07f + t * .36f * K, -.23f - t * .11f + .055f * s * s);
        Vector3 Nf(float s, float t) { var ds = new Vector3(bw, 0, .11f * s); var dt = new Vector3(0, .36f * K, -.11f); return Vector3.Cross(ds, dt).normalized; }
        for (int i = 0; i < nu; i++) for (int j = 0; j < nv; j++)
        {
            float s0 = -1f + 2f * i / nu, s1 = -1f + 2f * (i + 1) / nu, t0 = j / (float)nv, t1 = (j + 1) / (float)nv;
            var n = Nf((s0 + s1) * .5f, (t0 + t1) * .5f);
            pl.Quad(Op(s0, t0), Op(s1, t0), Op(s1, t1), Op(s0, t1), n);
            pl.Quad(Op(s0, t0) - n * gr, Op(s1, t0) - n * gr, Op(s1, t1) - n * gr, Op(s0, t1) - n * gr, -n);
        }
        for (int i = 0; i < nu; i++)
        {
            float s0 = -1f + 2f * i / nu, s1 = -1f + 2f * (i + 1) / nu;
            foreach (var (t, nn) in new[] { (1f, Vector3.up), (0f, Vector3.down) }) pl.Quad(Op(s0, t), Op(s1, t), Op(s1, t) - Nf(s1, t) * gr, Op(s0, t) - Nf(s0, t) * gr, nn);
        }
        for (int j = 0; j < nv; j++)
        {
            float t0 = j / (float)nv, t1 = (j + 1) / (float)nv;
            foreach (var (s, nn) in new[] { (1f, Vector3.right), (-1f, Vector3.left) }) pl.Quad(Op(s, t0), Op(s, t1), Op(s, t1) - Nf(s, t1) * gr, Op(s, t0) - Nf(s, t0) * gr, nn);
        }
        // stal: noga z kotwą (płyta), poprzeczka pod siedziskiem, wspornik oparcia
        me.Rura(new Vector3(0, .012f, -.14f), new Vector3(0, h - .055f, -.07f), .032f, 6, false);
        me.Walec(new Vector3(0, 0, -.14f), .085f, .012f, 6);
        me.Rura(new Vector3(-sw * .38f, h - .068f, -.06f), new Vector3(sw * .38f, h - .068f, -.06f), .016f, 4, false);
        me.Rura(new Vector3(0, h - .06f, -.2f), Op(0, .25f) - Nf(0, .25f) * (gr + .012f), .018f, 4, false);
        var m1 = pl.Zbuduj("plastik"); var m2 = me.Zbuduj("stal");
        var m = new Mesh { name = "A: arena premium — krzesełko trybun (siedzisko + stal)" };
        m.CombineMeshes(new[] { new CombineInstance { mesh = m1, transform = Matrix4x4.identity }, new CombineInstance { mesh = m2, transform = Matrix4x4.identity } }, false, false);
        m.RecalculateBounds(); Object.DestroyImmediate(m1); Object.DestroyImmediate(m2);
        Directory.CreateDirectory(BraxMiastoA.Out + "/Wyspa");
        BraxZapisAssetu.Utworz(m, BraxMiastoA.Out + "/Wyspa/arena_krzeselko.asset");
        return m;
    }
    static Material ApInst(Material m) { if (m && !m.enableInstancing) { m.enableInstancing = true; EditorUtility.SetDirty(m); } return m; }

    // fotel klubowy (loże VIP): cokół, poducha siedziska, oparcie pochylone, podłokietniki — profile zaokrąglone
    static void ApFotelKlubowy(UlSiatka s, Vector3 p, Vector3 przod)
    {
        przod.y = 0; przod.Normalize(); var r = Vector3.Cross(Vector3.up, przod); float w = .84f, d = .82f, hs = .45f * K, h0 = hs - .2f;
        Wyciag(s, PZ(d - .1f, h0, .02f, new Vector2(.0f, h0 * .5f)), p - r * (w * .5f - .06f), r, Vector3.up, przod, w - .12f);
        Wyciag(s, PZ(d - .14f, .2f, .07f, new Vector2(.03f, h0 + .1f)), p - r * (w * .5f - .14f), r, Vector3.up, przod, w - .28f);
        Wyciag(s, Obrot(PZ(.2f, .6f, .08f, new Vector2(-d * .5f + .13f, hs + .26f)), 12f, new Vector2(-d * .5f + .13f, hs)), p - r * (w * .5f - .14f), r, Vector3.up, przod, w - .28f);
        foreach (float sg in new[] { -1f, 1f }) Wyciag(s, PZ(d - .04f, hs + .16f, .07f, new Vector2(0f, (hs + .16f) * .5f)), p + r * (sg < 0 ? -w * .5f : w * .5f - .14f), r, Vector3.up, przod, .14f);
    }

    // =====================================================================================================================
    // TRYBUNY (cięciwy ośmioboku jak w starej arenie — rzędy, przejścia i loże VIP), krzesełka instancjami, kibice ≤ 150
    static int ApTrybuny()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — trybuny");
        float F = apF; var c = apC; const int nb = 8; int wej = apWej, tunel = apTun;
        var tb = BraxUliceKit.S(apBeton); var tk = new UlSiatka();
        var stopM = BraxUliceKit.S(BraxMiastoA.Mat("Arena: stopnie przejść (beton jasny)", new Color(.56f, .57f, .58f), .85f));
        var nosek = D(BraxMiastoA.Mat("Arena premium: noski stopni (aluminium ryflowane)", new Color(.62f, .63f, .62f), .5f, .7f));
        float r0 = 9.5f, rz = .9f * K, hz = .42f * K; const int rzedy = 9, rzVip = 6;
        int Mod(int i) => ((i % nb) + nb) % nb;
        bool Jest(int i) => Mod(i) != wej && Mod(i) != tunel;
        bool Vip(int i) => Mod(i) == Mod(wej + 1) || Mod(i) == Mod(wej - 1);
        int Rzedow(int i) => !Jest(i) ? 0 : Vip(i) ? rzVip + 1 : rzedy;
        int Szczyt(int i, int j) => Vip(i) || Vip(j) ? rzVip : rzedy;
        float RA(int q) => r0 + q * rz;
        float RB(int i, int q) => Vip(i) && q == rzVip ? r0 + rzedy * rz : r0 + (q + 1) * rz;
        Vector3 P(float r, float ang, float y) => new Vector3(c.x + Mathf.Cos(ang) * r, y, c.z + Mathf.Sin(ang) * r);
        var por = D(apAlu); var szkP = D(BraxUliceKit.SzkloPrzezroczyste);
        var skora = D(BraxMiastoA.Mat("Arena: fotele lóż VIP (skóra)", new Color(.09f, .09f, .1f), .45f));
        var szron = D(BraxMiastoA.Mat("Arena: przegrody lóż (szkło satynowe)", new Color(.8f, .85f, .86f, .55f), .2f));
        var orzech = D(BraxMiastoA.Mat("Arena: stoliki lóż (orzech)", new Color(.32f, .2f, .12f), .45f));
        void Barierka(Vector3 a, Vector3 b, Vector3 przod)
        {
            float L = Vector3.Distance(a, b); int np = Mathf.Max(1, Mathf.CeilToInt(L / 1.6f));
            por.Rura(a + Vector3.up * 1.1f, b + Vector3.up * 1.1f, .03f, 8); por.Rura(a + Vector3.up * .55f, b + Vector3.up * .55f, .015f, 6);
            for (int s = 0; s <= np; s++) { var p = Vector3.Lerp(a, b, s / (float)np); por.Rura(p, p + Vector3.up * 1.1f, .025f, 6); }
            KolA("barierka trybun", (a + b) * .5f + Vector3.up * .55f, przod, new Vector3(L, 1.1f, .1f));
        }
        void Balustrada(Vector3 a, Vector3 b, Vector3 przod, float hb)   // szkło w stopce aluminiowej z poręczą
        {
            float L = Vector3.Distance(a, b); var rr = (b - a) / L;
            por.OBox((a + b) * .5f + Vector3.up * .05f, rr, Vector3.up, przod, L, .1f, .07f, false, true);
            szkP.OBox((a + b) * .5f + Vector3.up * (.1f + (hb - .14f) * .5f), rr, Vector3.up, przod, L - .02f, hb - .14f, .016f);
            por.Rura(a + Vector3.up * hb, b + Vector3.up * hb, .032f, 10);
            KolA("balustrada szklana", (a + b) * .5f + Vector3.up * hb * .5f, przod, new Vector3(L, hb, .08f));
        }
        var krz = ApKrzeselko();
        var mGraf = ApInst(BraxMiastoA.Mat("Arena premium: krzesełko (polipropylen grafit, instancje)", new Color(.09f, .09f, .1f), .5f));
        var mZiel = ApInst(BraxMiastoA.Mat("Arena premium: krzesełko (polipropylen zielony, instancje)", new Color(.22f, .6f, .14f), .5f));
        var mStal = ApInst(BraxMiastoA.Mat("Arena premium: noga krzesełka (stal, instancje)", new Color(.35f, .36f, .38f), .4f, .7f));
        var gK = new GameObject("Arena premium: krzesełka (instancje GPU, LOD na sektor)").transform; gK.SetParent(root, false);
        var widownia = new List<(Vector3 p, Vector3 f)>(); int nKrz = 0, nVip = 0;
        for (int i = 0; i < nb; i++)
        {
            if (!Jest(i)) continue;
            var sekt = new GameObject("sektor " + (char)('A' + Mod(i - wej - 1))).transform; sekt.SetParent(gK, false);
            float a0 = (i + .5f) * Mathf.PI * 2f / nb, a1 = (i + 1.5f) * Mathf.PI * 2f / nb, am = (a0 + a1) * .5f, ch = Mathf.Cos((a1 - a0) * .5f);
            var dm = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am));
            var e0 = Vector3.Cross(Vector3.up, new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0))).normalized;
            var e1 = -Vector3.Cross(Vector3.up, new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1))).normalized;
            int nq = Rzedow(i);
            for (int q = 0; q < nq; q++)
            {
                float ra = RA(q), rb = RB(i, q), yt = F + (q + 1) * hz;
                Vector3 A0 = P(ra, a0, yt), A1 = P(ra, a1, yt), B0 = P(rb, a0, yt), B1 = P(rb, a1, yt), A0d = P(ra, a0, yt - hz), A1d = P(ra, a1, yt - hz);
                tb.Quad(A0, A1, B1, B0, Vector3.up); tk.Quad(A0, A1, B1, B0, Vector3.up);
                tb.Quad(A0d, A1d, A1, A0, -dm); tk.Quad(A0d, A1d, A1, A0, -dm);
                if (q == nq - 1) { tb.Quad(P(rb, a0, F), P(rb, a1, F), B1, B0, dm); tk.Quad(P(rb, a0, F), P(rb, a1, F), B1, B0, dm); }
                bool k0 = Rzedow(i - 1) <= q, k1 = Rzedow(i + 1) <= q;
                if (k0) { tb.Quad(P(ra, a0, F), P(rb, a0, F), B0, A0, e0); tk.Quad(P(ra, a0, F), P(rb, a0, F), B0, A0, e0); Barierka(P(ra + .1f, a0, yt) - e0 * .08f, P(rb - .1f, a0, yt) - e0 * .08f, e0); }
                if (k1) { tb.Quad(P(ra, a1, F), P(rb, a1, F), B1, A1, e1); tk.Quad(P(ra, a1, F), P(rb, a1, F), B1, A1, e1); Barierka(P(ra + .1f, a1, yt) - e1 * .08f, P(rb - .1f, a1, yt) - e1 * .08f, e1); }
                float dl = Vector3.Distance(A0, A1); var rr = (A1 - A0) / dl;
                if (Vip(i) && q == rzVip)
                {
                    // LOŻE VIP: parapet szklany, przegrody z szkła satynowego, fotele klubowe, stoliki toczone
                    float gl = (rb - ra) * ch, m0 = Jest(i - 1) ? .85f : .25f, m1 = Jest(i + 1) ? .85f : .25f;
                    Balustrada(A0 + rr * m0 + dm * .1f, A1 - rr * m1 + dm * .1f, dm, 1.0f * K);
                    int nl = Mathf.Max(1, Mathf.FloorToInt((dl - m0 - m1) / 3.0f)); float wl = (dl - m0 - m1) / nl, lp = gl - 1.25f;
                    for (int L = 0; L < nl; L++)
                    {
                        float s0 = m0 + L * wl;
                        if (L > 0) { var pc = A0 + rr * s0 + dm * (.12f + lp * .5f) + Vector3.up * .8f * K; szron.OBox(pc, dm, Vector3.up, rr, lp, 1.6f * K, .03f); KolA("przegroda loży", pc, rr, new Vector3(lp, 1.6f * K, .08f)); }
                        foreach (float fx in new[] { .24f, .76f })
                        {
                            var sp = A0 + rr * (s0 + wl * fx) + dm * 1.0f;
                            ApFotelKlubowy(skora, sp, -dm);
                            BraxMiastoA.Siedzisko(root, "FOTEL VIP", sp + Vector3.up * .45f * K, -dm);
                            KolA("fotel VIP", sp + Vector3.up * .3f * K, dm, new Vector3(.8f, .6f * K, .8f));
                            Czlowiek("Loża VIP", "gość VIP", "siedzi", sp + Vector3.up * .45f * K, -dm, KlSiedzi); nVip++;
                        }
                        var st = A0 + rr * (s0 + wl * .5f) + dm * .75f;
                        Toczenie(orzech, new[] { new Vector2(0, 0), new Vector2(.2f, 0), new Vector2(.2f, .02f), new Vector2(.05f, .04f), new Vector2(.045f, .5f * K), new Vector2(.28f, .5f * K), new Vector2(.3f, .52f * K + .01f), new Vector2(.28f, .52f * K + .03f), new Vector2(0, .52f * K + .03f) }, st, 18);
                    }
                    Barierka(P(rb - .1f, a0, yt) + rr * .3f, P(rb - .1f, a1, yt) - rr * .3f, dm);
                    Miejsca["arena_loze_vip_" + i] = A0 + rr * dl * .5f + dm * (gl - .55f); apMiejsc++;
                    continue;
                }
                float mm0 = Jest(i - 1) && q <= Szczyt(i, i - 1) ? .8f : .2f, mm1 = Jest(i + 1) && q <= Szczyt(i, i + 1) ? .8f : .2f;
                float sp0 = .55f * K; int nf = Mathf.FloorToInt((dl - mm0 - mm1) / sp0); float rest = (dl - mm0 - mm1 - nf * sp0) * .5f;
                var mPl = q % 4 == 1 ? mZiel : mGraf;
                for (int f = 0; f < nf; f++)
                {
                    var sp = A0 + rr * (mm0 + rest + (f + .5f) * sp0) + dm * (rz * ch - .4f);
                    var go = new GameObject("krzesełko " + (q + 1) + "/" + (f + 1)); go.transform.SetParent(sekt, false);
                    go.transform.position = sp; go.transform.rotation = Quaternion.LookRotation(-dm, Vector3.up);
                    go.AddComponent<MeshFilter>().sharedMesh = krz; var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterials = new[] { mPl, mStal };
                    mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = true; mr.lightProbeUsage = LightProbeUsage.Off; mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccludeeStatic);   // BEZ BatchingStatic — batching zjadłby instancing
                    var siad = sp + Vector3.up * (.45f * K) - dm * .02f;
                    widownia.Add((siad, -dm)); nKrz++;
                    if (q == 0) BraxMiastoA.Siedzisko(go.transform, "KRZESEŁKO TRYBUN", siad, -dm);
                }
                if (q == 0 && nf > 0) Balustrada(A0 + rr * (mm0 - .1f) + dm * .04f, A1 - rr * (mm1 - .1f) + dm * .04f, dm, .82f * K);
                if (q == nq - 1) Barierka(B0 + rr * .1f - dm * .08f, B1 - rr * .1f - dm * .08f, dm);
            }
            var rs = sekt.GetComponentsInChildren<Renderer>(); if (rs.Length > 0)
            {
                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                var lod = sekt.gameObject.AddComponent<LODGroup>(); float roz = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
                lod.SetLODs(new[] { new LOD(Mathf.Clamp(roz / (2f * 75f * .52f), .01f, .6f), rs) }); lod.RecalculateBounds();
            }
            apElem++;
        }
        // PRZEJŚCIA na stykach sektorów: stopnie (pół rzędu) z noskami
        int nPrz = 0;
        for (int i = 0; i < nb; i++)
        {
            if (!Jest(i) || !Jest(i + 1)) continue;
            int szczyt = Szczyt(i, i + 1); nPrz++;
            float ab = (i + 1.5f) * Mathf.PI * 2f / nb; var dr = new Vector3(Mathf.Cos(ab), 0, Mathf.Sin(ab)); var tg = Vector3.Cross(Vector3.up, dr);
            void Stopien(float rA, float rB, float ys0, float ys1)
            {
                var cS = c + dr * ((rA + rB) * .5f) + Vector3.up * ((ys0 + ys1) * .5f);
                stopM.OBox(cS, tg, Vector3.up, dr, 1.3f, ys1 - ys0, rB - rA, false, true); tk.OBox(cS, tg, Vector3.up, dr, 1.3f, ys1 - ys0, rB - rA, false, true);
                nosek.OBox(c + dr * (rA + .025f) + Vector3.up * (ys1 + .004f), tg, Vector3.up, dr, 1.2f, .008f, .05f, false, true);
            }
            Stopien(r0 - rz * .5f, r0, F, F + hz * .5f);
            for (int q = 0; q < szczyt; q++) Stopien(RA(q) + rz * .5f, RA(q + 1), F + (q + 1) * hz, F + (q + 1.5f) * hz);
            Miejsca["arena_przejscie_" + i] = c + dr * (r0 - .6f) + Vector3.up * F; apMiejsc++;
        }
        var tkm = tk.Zbuduj("A: arena premium — trybuny (kolizja)"); BraxZapisAssetu.Utworz(tkm, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/arena_trybuny_kolizja.asset"));
        var tkg = new GameObject("kolizja: trybuny (bryła)"); tkg.transform.SetParent(kol, false); tkg.AddComponent<MeshCollider>().sharedMesh = tkm; apKol++;
        // KIBICE ≤ 150 — równomiernie po wszystkich krzesełkach (BraxRegionRuchMiasta.LudzieZMiejsc sadza każdego)
        int nKib = Mathf.Min(150, widownia.Count);
        for (int k = 0; k < nKib; k++) { var w = widownia[Mathf.Min(widownia.Count - 1, Mathf.FloorToInt((k + .37f) * widownia.Count / (float)nKib))]; Czlowiek("Trybuny ARENY", "kibic", "siedzi", w.p, w.f, KlKibic); }
        apKibice = nKib;
        BraxMiastoA.Log("WYSPA arena premium: trybuny — krzesełek " + nKrz + " (1 siatka, instancje), przejść " + nPrz + ", foteli VIP " + nVip + ", kibiców " + nKib);
        return nKrz + nVip;
    }

    // =====================================================================================================================
    // KLATKA (oktagon) z kodu: podest z fartuchem, pas słupków (stal), mata z wycięciem, słupki w osłonach, siatka ogniw, drzwiczki
    static Material ApSiatkaMat()
    {
        string path = BraxMiastoA.Out + "/Wyspa/arena_siatka_ogniwa.png";
        if (!File.Exists(path))
        {
            const int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false); var px = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int u = (x + y) % 32, v = ((x - y) % 32 + 32) % 32; bool drut = u < 3 || v < 3;
                px[y * n + x] = drut ? new Color(.62f, .63f, .64f, 1f) : new Color(.1f, .1f, .1f, 0f);
            }
            t.SetPixels(px); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            imp.alphaIsTransparency = true; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.wrapMode = TextureWrapMode.Repeat; imp.mipmapEnabled = true;
            imp.mipMapsPreserveCoverage = true; imp.alphaTestReferenceValue = .5f; imp.maxTextureSize = 64; imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return BraxMiastoA.ZShadera(BraxCityMaterials.ShaderName, "Arena premium: siatka klatki (ogniwa, maska alfa)", m =>
        {
            m.SetTexture("_MainTex", tex); m.SetColor("_Color", new Color(.2f, .2f, .21f, 1f)); m.SetFloat("_Roughness", .45f); m.SetFloat("_Metallic", .6f);
            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0f); m.renderQueue = 2450; m.SetOverrideTag("RenderType", "TransparentCutout");
        });
    }
    static Material ApLogoMaty()
    {
        try
        {
            string path = BraxMiastoA.Out + "/Wyspa/arena_mata_logo.png"; const int n = 512;
            var px = new Color[n * n]; var tlo = new Color(.2f, .52f, .12f); var bial = new Color(.95f, .95f, .93f);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = x - n * .5f, dy = y - n * .5f, r = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * n + x] = r > n * .44f && r < n * .46f ? bial : tlo;   // okrąg logo
            }
            BraxUliceKit.NapisWTeksturze(px, n, n, "BRAXHORN", n * .5f, n * .5f - 22f, 46f, bial);
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.SetPixels(px); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp) { imp.wrapMode = TextureWrapMode.Clamp; imp.maxTextureSize = 512; imp.textureCompression = TextureImporterCompression.Compressed; imp.SaveAndReimport(); }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (!tex) return null;
            return BraxMiastoA.ZShadera(BraxCityMaterials.ShaderName, "Arena premium: środek maty (logo BRAXHORN w teksturze)", m => { m.SetTexture("_MainTex", tex); m.SetColor("_Color", Color.white); m.SetFloat("_Roughness", .7f); m.SetFloat("_Metallic", 0f); });
        }
        catch (System.Exception e) { BraxMiastoA.Log("WYSPA arena: logo maty DO POPRAWKI " + e.Message); return null; }
    }
    static void ApKlatka()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — klatka");
        var c = apC; float F = apF, ri = 4.55f * K, rc = ri / Mathf.Cos(Mathf.PI / 8f), hp = .9f * K, hf = 1.8f * K, ps = .15f, fa = .6f;
        float yT = F + hp; int tunel = apTun, czerw = (tunel + 3) % 8, nieb = (tunel + 6) % 8;
        Vector3 Kw(int i, float r) { float a = (i + .5f) * Mathf.PI * 2f / 8f; return new Vector3(c.x + Mathf.Cos(a) * r, 0, c.z + Mathf.Sin(a) * r); }
        var cz = BraxUliceKit.S(apCzarny); var winyl = BraxUliceKit.S(BraxMiastoA.Mat("Arena premium: fartuch podestu (winyl czarny)", new Color(.05f, .05f, .055f), .7f));
        var stalP = BraxUliceKit.S(BraxMiastoA.Mat("Arena premium: pas słupków (stal grafit)", new Color(.2f, .2f, .21f), .5f, .6f));
        var mata = BraxMiastoA.Mat("Arena: mata klatki (winyl)", new Color(.88f, .88f, .86f), .7f);
        var mataZ = BraxMiastoA.Mat("Arena premium: środek maty (zieleń BRAXHORN)", new Color(.2f, .52f, .12f), .7f);
        var mm = new UlSiatka(); var mz = new UlSiatka(); float rZ = 2.4f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 Y(Vector3 v) => v + Vector3.up * yT;
            winyl.Quad(Kw(i, rc + fa) + Vector3.up * F, Kw(i + 1, rc + fa) + Vector3.up * F, Kw(i + 1, rc + fa) + Vector3.up * yT, Kw(i, rc + fa) + Vector3.up * yT, ((Kw(i, 1) + Kw(i + 1, 1)) * .5f - c).normalized);
            winyl.Quad(Y(Kw(i, rc + ps)), Y(Kw(i + 1, rc + ps)), Y(Kw(i + 1, rc + fa)), Y(Kw(i, rc + fa)), Vector3.up);       // fartuch
            stalP.Quad(Y(Kw(i, rc - ps)), Y(Kw(i + 1, rc - ps)), Y(Kw(i + 1, rc + ps)), Y(Kw(i, rc + ps)), Vector3.up);        // pas słupków
            mm.Quad(Y(Kw(i, rZ)), Y(Kw(i + 1, rZ)), Y(Kw(i + 1, rc - ps)), Y(Kw(i, rc - ps)), Vector3.up);                    // mata (pierścień)
            int kz = mz.v.Count; mz.Tri(new Vector3(c.x, yT, c.z), Y(Kw(i + 1, rZ)), Y(Kw(i, rZ)), Vector3.up);               // środek maty (logo w teksturze)
            for (int q = kz; q < mz.v.Count; q++) mz.uv[q] = new Vector2((mz.v[q].x - c.x) / (2f * rZ) + .5f, (mz.v[q].z - c.z) / (2f * rZ) + .5f);
            cz.Rura(Kw(i, rc + fa) + Vector3.up * (yT - .03f), Kw(i + 1, rc + fa) + Vector3.up * (yT - .03f), .045f, 8);     // obrzeże fartucha (wałek)
        }
        BraxMiastoA.Obiekt(root, "Klatka: mata (z wycięciem pod pas słupków)", mm, mata, true); BraxMiastoA.Obiekt(root, "Klatka: środek maty (logo BRAXHORN)", mz, ApLogoMaty() ?? mataZ, true);
        var siat = new UlSiatka(); var sMat = ApSiatkaMat(); const float oko = .13f;
        var gb = BraxUliceKit.S(BraxMiastoA.Mat("Arena: osłony klatki (pianka)", new Color(.06f, .06f, .06f), .8f));
        var gbC = BraxUliceKit.S(BraxMiastoA.Mat("Arena: osłona narożnika czerwonego", new Color(.72f, .07f, .07f), .7f));
        var gbN = BraxUliceKit.S(BraxMiastoA.Mat("Arena: osłona narożnika niebieskiego", new Color(.08f, .2f, .7f), .7f));
        void Panel(Vector3 p, Vector3 q, float ya, float yb)
        {
            int k = siat.v.Count; var nn = Vector3.Cross(q - p, Vector3.up).normalized;
            siat.Quad(p + Vector3.up * ya, q + Vector3.up * ya, q + Vector3.up * yb, p + Vector3.up * yb, nn);
            float L = Vector3.Distance(p, q);
            siat.uv[k] = new Vector2(0, ya / oko); siat.uv[k + 1] = new Vector2(L / oko, ya / oko); siat.uv[k + 2] = new Vector2(L / oko, yb / oko); siat.uv[k + 3] = new Vector2(0, yb / oko);
        }
        for (int i = 0; i < 8; i++)
        {
            var a = Kw(i, rc); var b = Kw(i + 1, rc); var dd = (b - a).normalized; float L = Vector3.Distance(a, b);
            var nrm = ((a + b) * .5f - c).normalized;
            (i == czerw ? gbC : i == nieb ? gbN : gb).Rura(a + Vector3.up * yT, a + Vector3.up * (yT + hf + .06f), .13f, 14);
            gb.Rura(a + dd * .12f + Vector3.up * (yT + hf), b - dd * .12f + Vector3.up * (yT + hf), .09f, 12);
            cz.Rura(a + dd * .12f + Vector3.up * (yT + .12f), b - dd * .12f + Vector3.up * (yT + .12f), .045f, 8);
            bool drzwi = i == tunel;
            if (!drzwi)
            {
                Panel(a + dd * .13f, b - dd * .13f, yT + .17f, yT + hf - .09f);
                KolA("siatka klatki", (a + b) * .5f + Vector3.up * (yT + hf * .5f), nrm, new Vector3(L, hf, .1f));
            }
            else
            {
                float dw = 1.0f * K; var m0 = a + dd * (L * .5f - dw * .5f); var m1 = a + dd * (L * .5f + dw * .5f);
                Panel(a + dd * .13f, m0, yT + .17f, yT + hf - .09f); Panel(m1, b - dd * .13f, yT + .17f, yT + hf - .09f);
                var open = m0 + (Quaternion.AngleAxis(-70f, Vector3.up) * dd) * (dw - .04f);
                Panel(m0 + (open - m0).normalized * .03f, open, yT + .1f, yT + hf - .1f);
                foreach (var (p, q) in new[] { (m0, open) }) { cz.Rura(p + Vector3.up * (yT + .08f), q + Vector3.up * (yT + .08f), .022f, 6); cz.Rura(p + Vector3.up * (yT + hf - .08f), q + Vector3.up * (yT + hf - .08f), .022f, 6); cz.Rura(q + Vector3.up * (yT + .08f), q + Vector3.up * (yT + hf - .08f), .022f, 6); }
                cz.Rura(m0 + Vector3.up * yT, m0 + Vector3.up * (yT + hf), .035f, 8); cz.Rura(m1 + Vector3.up * yT, m1 + Vector3.up * (yT + hf), .035f, 8);
                foreach (var (p, q) in new[] { (a, m0), (m1, b) }) KolA("siatka klatki", (p + q) * .5f + Vector3.up * (yT + hf * .5f), nrm, new Vector3(Vector3.Distance(p, q), hf, .1f));
                // schodki do drzwiczek: 4 stopnie z noskiem zaokrąglonym i poręcze
                var st = BraxUliceKit.S(apCzarny); var por = D(apChrom); var mid = (m0 + m1) * .5f;
                for (int s = 0; s < 4; s++)
                {
                    float hs = hp * (s + 1) / 4f, out0 = fa + (3 - s) * .35f;
                    var pr = new[] { new Vector2(0, 0), new Vector2(.35f, 0), new Vector2(.35f, hs - .03f), new Vector2(.34f, hs - .008f), new Vector2(.32f, hs), new Vector2(0, hs) };
                    Wyciag(st, pr, mid + nrm * out0 - dd * ((dw + .4f) * .5f) + Vector3.up * F, dd, Vector3.up, nrm, dw + .4f);
                    KolA("schodek klatki", mid + nrm * (out0 + .175f) + Vector3.up * (F + hs * .5f), nrm, new Vector3(dw + .4f, hs, .35f));
                }
                foreach (float sg in new[] { -1f, 1f })
                {
                    var b0 = mid + dd * (sg * (dw * .5f + .25f)) + nrm * (fa + 1.4f) + Vector3.up * F; var b1 = mid + dd * (sg * (dw * .5f + .25f)) + nrm * (fa + .05f) + Vector3.up * yT;
                    por.Rura(b0 + Vector3.up * .9f * K, b1 + Vector3.up * .9f * K, .02f, 8); por.Rura(b0, b0 + Vector3.up * .9f * K, .02f, 6); por.Rura(b1, b1 + Vector3.up * .9f * K, .02f, 6);
                }
                Miejsca["arena_klatka"] = mid + nrm * .3f + Vector3.up * yT; apMiejsc++;
                BraxMiastoA.Akcja(root, "arena_klatka", "WEJDŹ DO KLATKI", "Sędzia: Do narożnika — walka o pas mistrza ARENY BRAXHORN.", mid + nrm * (fa + 2.0f) + Vector3.up * F, 2.0f);
                Czlowiek("Klatka", "sędzia", "stoi", mid - nrm * 1.3f + Vector3.up * yT, -nrm, KlStoi);
            }
            KolA("słupek klatki", a + Vector3.up * (yT + hf * .5f), nrm, new Vector3(.26f, hf, .26f));
        }
        BraxMiastoA.Obiekt(root, "Klatka: siatka (ogniwa, maska alfa)", siat, sMat, false);
        var pm = new UlSiatka(); for (int i = 0; i < 8; i++) { var a = Kw(i, rc + fa); var b = Kw(i + 1, rc + fa); pm.Quad(a + Vector3.up * F, b + Vector3.up * F, b + Vector3.up * yT, a + Vector3.up * yT, Vector3.up); }
        var pmesh = pm.Zbuduj("A: arena premium — podest klatki (kolizja)"); BraxZapisAssetu.Utworz(pmesh, AssetDatabase.GenerateUniqueAssetPath(BraxMiastoA.Out + "/Wyspa/arena_podest_kolizja.asset"));
        var pc0 = new GameObject("kolizja: podest klatki"); pc0.transform.SetParent(kol, false); var mc = pc0.AddComponent<MeshCollider>(); mc.sharedMesh = pmesh; mc.convex = true; apKol++;
        // narożniki: stołek trenera (toczony) i wiaderko
        foreach (var (kat, czer) in new[] { (czerw, true), (nieb, false) })
        {
            var dk = (Kw(kat, 1f) - c).normalized; var bk = Vector3.Cross(Vector3.up, dk); var pS = new Vector3(c.x, F, c.z) + dk * (rc + fa + .75f);
            var mS = D(BraxMiastoA.Mat(czer ? "Arena: osłona narożnika czerwonego" : "Arena: osłona narożnika niebieskiego", czer ? new Color(.72f, .07f, .07f) : new Color(.08f, .2f, .7f), .7f));
            float hS = .44f * K;
            Toczenie(mS, new[] { new Vector2(0, hS), new Vector2(.2f, hS), new Vector2(.215f, hS + .03f), new Vector2(.2f, hS + .065f), new Vector2(0, hS + .07f) }, pS, 18);
            for (int l = 0; l < 3; l++) { var o = Quaternion.AngleAxis(l * 120f, Vector3.up) * dk; D(apChrom).Rura(pS + o * .2f, pS + o * .06f + Vector3.up * hS, .016f, 6); }
            Toczenie(D(apCzarny), new[] { new Vector2(0, 0), new Vector2(.13f, 0), new Vector2(.155f, .3f), new Vector2(.14f, .3f), new Vector2(.12f, .02f), new Vector2(0, .02f) }, pS + bk * .55f, 16);
            BraxMiastoA.Siedzisko(root, "STOŁEK TRENERA", pS + Vector3.up * (hS + .07f), -dk);
            KolA("stołek trenera", pS + Vector3.up * hS * .5f, dk, new Vector3(.4f, hS, .4f));
            Miejsca[czer ? "arena_naroznik_czerwony" : "arena_naroznik_niebieski"] = pS; apMiejsc++;
            Czlowiek(czer ? "Narożnik czerwony" : "Narożnik niebieski", "trener narożnika", "stoi", pS + dk * .55f - bk * .5f, -dk, KlStoi);
        }
        apElem += 10;
    }

    // =====================================================================================================================
    // RAMPA (kratownica skrzynkowa ośmiokątna z reflektorami na jarzmach) i TELEBIM 4-stronny nad klatką; podwieszenie do stropu
    static void ApRampa()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — rampa i telebim");
        var c = apC; float yR = apY0 + 10.6f, rT = 8.4f, a = .4f, yC = apY0 + ApSufit; var st = BraxUliceKit.S(apStal);
        var obud = D(apCzarny); var socz = D(BraxMiastoA.Swieci("Arena premium: soczewki reflektorów", new Color(1f, .97f, .9f), new Color(3f, 2.9f, 2.6f)));
        Vector3 Kw(int i, float r) { float t = (i + .5f) * Mathf.PI * 2f / 8f; return new Vector3(c.x + Mathf.Cos(t) * r, 0, c.z + Mathf.Sin(t) * r); }
        var cel = c + Vector3.up * (apF + .9f * K); int nRefl = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3[] pas = { Kw(i, rT - a * .5f) + Vector3.up * yR, Kw(i, rT + a * .5f) + Vector3.up * yR, Kw(i, rT - a * .5f) + Vector3.up * (yR + a), Kw(i, rT + a * .5f) + Vector3.up * (yR + a) };
            Vector3[] pas2 = { Kw(i + 1, rT - a * .5f) + Vector3.up * yR, Kw(i + 1, rT + a * .5f) + Vector3.up * yR, Kw(i + 1, rT - a * .5f) + Vector3.up * (yR + a), Kw(i + 1, rT + a * .5f) + Vector3.up * (yR + a) };
            for (int k = 0; k < 4; k++) st.Rura(pas[k], pas2[k], .025f, 6);
            const int np = 12;
            foreach (var (p0, p1) in new[] { (0, 1), (2, 3), (0, 2), (1, 3) })
                for (int s = 0; s < np; s++)
                {
                    float t0 = s / (float)np, t1 = (s + 1) / (float)np;
                    var A = Vector3.Lerp(pas[p0], pas2[p0], t0); var B = Vector3.Lerp(pas[p1], pas2[p1], (t0 + t1) * .5f);
                    st.Rura(A, B, .011f, 4, false); st.Rura(B, Vector3.Lerp(pas[p0], pas2[p0], t1), .011f, 4, false);
                }
            st.Rura(Kw(i, rT) + Vector3.up * (yR + a), Kw(i, rT) + Vector3.up * yC, .008f, 4);
            for (int q = 0; q < 2; q++)
            {
                var m = Vector3.Lerp(pas[0], pas2[0], (q + 1) / 3f) + Vector3.down * .02f; var dir = (cel - m).normalized;
                var yoke = m + Vector3.down * .1f; var bok = Vector3.Cross(dir, Vector3.up).normalized; var hb = yoke + Vector3.down * .2f;
                obud.Rura(m, yoke, .03f, 6);
                foreach (float sg in new[] { -1f, 1f }) { obud.Rura(yoke + bok * (sg * .15f), yoke - bok * 0f, .015f, 6); obud.Rura(yoke + bok * (sg * .15f), hb + bok * (sg * .15f), .015f, 6); }
                obud.Rura(hb - dir * .18f, hb + dir * .16f, .11f, 12); socz.Rura(hb + dir * .16f, hb + dir * .166f, .09f, 12);
                nRefl++;
            }
        }
        // telebim: ekrany (obraz JEST ścianą kostki), krawędzie wałkami, góra i dół czarne z pasami LED, podwieszenie
        var tc = c + Vector3.up * (apY0 + 8.9f); string zap = Zapowiedz(); float hw = 2.6f, hh = 1.6f;
        var cz = BraxUliceKit.S(apCzarny);
        cz.Box(new Vector3(tc.x - hw, tc.y - hh, tc.z - hw), new Vector3(tc.x + hw, tc.y + hh, tc.z + hw), 1 | 2 | 16 | 32);
        foreach (var ox in new[] { -1f, 1f }) foreach (var oz in new[] { -1f, 1f }) cz.Rura(new Vector3(tc.x + ox * hw, tc.y - hh - .04f, tc.z + oz * hw), new Vector3(tc.x + ox * hw, tc.y + hh + .04f, tc.z + oz * hw), .06f, 8);
        foreach (var (n, r) in new[] { (Vector3.forward, Vector3.left), (Vector3.back, Vector3.right), (Vector3.left, Vector3.back), (Vector3.right, Vector3.forward) })
        {
            BraxMiastoA.Obraz(root, "telebim nad klatką " + n, zap, tc + n * hw - r * (hw - .06f) - Vector3.up * hh, r, 2f * hw - .12f, 2f * hh, n, .9f);
            D(apLed).OBox(tc + n * (hw + .02f) + Vector3.up * (hh + .1f), r, Vector3.up, n, 2f * hw, .12f, .04f, true);
            D(apLed).OBox(tc + n * (hw + .02f) - Vector3.up * (hh + .1f), r, Vector3.up, n, 2f * hw, .12f, .04f, true);
            cz.OBox(tc + n * (hw - .1f) + Vector3.up * (hh + .1f), r, Vector3.up, n, 2f * hw - .2f, .2f, .2f, false, false);
            cz.OBox(tc + n * (hw - .1f) - Vector3.up * (hh + .1f), r, Vector3.up, n, 2f * hw - .2f, .2f, .2f, false, false);
        }
        foreach (var ox in new[] { -1f, 1f }) foreach (var oz in new[] { -1f, 1f }) st.Rura(new Vector3(tc.x + ox * (hw - .3f), tc.y + hh + .2f, tc.z + oz * (hw - .3f)), new Vector3(tc.x + ox * (hw - .3f), yC, tc.z + oz * (hw - .3f)), .01f, 4);
        BraxMiastoA.Log("WYSPA arena premium: rampa ośmiokątna (kratownica skrzynkowa " + a + " m), reflektorów " + nRefl + ", telebim 4 ekrany");
        apElem += 2 + nRefl;
    }

    // =====================================================================================================================
    // HOL: kasy (lada kształtowana, szyba, monitor, krzesło), bramki biletowe, przegrody szklane do trybun, gaśnice, gniazdka
    static void ApHol()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — hol");
        float F = apF, vB = 13.8f, uS = vB * Mathf.Tan(Mathf.PI / 8f) - .12f;
        // bramki: model metra (paczka) albo z kodu
        float zA, zB; bool model = false;
        try { model = BraxMiastoAssety.BramkiMetra(root, apC.x - vB, F, apC.z, 4, out zA, out zB); } catch (System.Exception e) { BraxMiastoA.Log("WYSPA arena: bramki z modelu DO POPRAWKI " + e.Message); zA = zB = 0f; }
        if (!model)
        {
            float pw = .62f * K, cw = .24f; int n = 4; float tot = n * pw + (n + 1) * cw; zA = apC.z - tot * .5f; zB = apC.z + tot * .5f;
            var ob = D(BraxMiastoA.Mat("Arena premium: obudowy bramek (stal nierdzewna)", new Color(.7f, .71f, .72f), .3f, .85f)); var gl = D(BraxUliceKit.SzkloPrzezroczyste);
            for (int k = 0; k <= n; k++)
            {
                float z = zA + k * (pw + cw) + cw * .5f; var p = new Vector3(apC.x - vB - .6f, F, z - cw * .5f);
                Wyciag(ob, PZ(1.2f, 1.0f * K, .1f, new Vector2(0f, .5f * K)), p, Vector3.forward, Vector3.up, Vector3.right, cw);
                D(apLed).OBox(new Vector3(apC.x - vB - .55f, F + 1.0f * K + .005f, z), Vector3.forward, Vector3.up, Vector3.right, cw * .5f, .01f, .5f, false, true);
                if (k < n) foreach (float sg in new[] { -1f, 1f }) gl.OBox(new Vector3(apC.x - vB - .1f, F + .55f * K, z + sg * (cw * .5f + .03f) + (sg > 0 ? 0 : 0)), Vector3.right, Vector3.up, Vector3.forward, .5f, .7f * K, .012f);
                KolAA("obudowa bramki", new Vector3(apC.x - vB - 1.2f, F, z - cw * .5f), new Vector3(apC.x - vB, F + 1.0f * K, z + cw * .5f));
            }
        }
        // przegrody szklane od bramek do trybun (słupki, szkło, poręcz)
        foreach (var (z0, z1) in new[] { (apC.z - uS, zA - .02f), (zB + .02f, apC.z + uS) })
        {
            if (z1 - z0 < .2f) continue;
            var a = new Vector3(apC.x - vB - .3f, F, z0); var b = new Vector3(apC.x - vB - .3f, F, z1); float hb = 1.1f * K;
            D(BraxUliceKit.SzkloPrzezroczyste).OBox((a + b) * .5f + Vector3.up * (hb * .5f), Vector3.forward, Vector3.up, Vector3.right, z1 - z0 - .1f, hb - .1f, .016f);
            D(apAlu).Rura(a + Vector3.up * hb, b + Vector3.up * hb, .03f, 10);
            for (int s = 0; s <= 3; s++) { var p = Vector3.Lerp(a, b, s / 3f); D(apAlu).Rura(p, p + Vector3.up * hb, .028f, 8); }
            KolAA("przegroda szklana holu", a + new Vector3(-.05f, 0, 0), b + new Vector3(.05f, hb, 0));
        }
        // KASY: 2 stanowiska przodem do przejścia (u = 0), lada z profilem (cokół cofnięty, front wybrzuszony, blat z noskiem)
        var lam = D(BraxMiastoA.Mat("Arena premium: front lady kas (corian grafit)", new Color(.13f, .135f, .14f), .35f, .1f));
        var blat = D(BraxMiastoA.Mat("Arena premium: blat kas (kamień jasny)", new Color(.86f, .85f, .82f), .3f));
        foreach (float su in new[] { -1f, 1f })
        {
            float uF = su * 3.3f; var ws = new Vector3(0, 0, -su);   // do gościa
            var prof = new[] { new Vector2(-.7f, 0), new Vector2(-.08f, 0), new Vector2(-.08f, .1f), new Vector2(-.03f, .14f), new Vector2(0f, .55f), new Vector2(-.02f, .95f * K), new Vector2(-.7f, .95f * K) };
            var start = new Vector3(apC.x - 16.9f, F, apC.z + uF);
            Wyciag(lam, prof, start, Vector3.right, Vector3.up, ws, 2.0f);
            Wyciag(blat, PZ(.42f, .04f, .015f, new Vector2(-.12f, .95f * K + .02f)), start - Vector3.right * .03f, Vector3.right, Vector3.up, ws, 2.06f);
            Wyciag(blat, PZ(.6f, .035f, .01f, new Vector2(-.5f, .76f * K)), start, Vector3.right, Vector3.up, ws, 2.0f);
            D(BraxUliceKit.SzkloPrzezroczyste).OBox(start + Vector3.right * 1.0f + ws * (-.14f) + Vector3.up * (.95f * K + .45f), Vector3.right, Vector3.up, ws, 1.9f, .78f, .012f);
            foreach (float x in new[] { .02f, 1.98f }) D(apChrom).Rura(start + Vector3.right * x + ws * -.14f + Vector3.up * (.95f * K + .04f), start + Vector3.right * x + ws * -.14f + Vector3.up * (.95f * K + .86f), .012f, 6);
            KolAA("lada kas", start, start + new Vector3(2.0f, .95f * K, su * .7f));
            for (int k = 0; k < 2; k++)
            {
                var sp = start + Vector3.right * (.5f + k * 1.0f) - ws * .45f;
                var mon = sp + Vector3.up * (.76f * K + .02f);
                D(apCzarny).Walec(mon, .09f, .012f, 10); D(apCzarny).Rura(mon, mon + Vector3.up * .22f, .015f, 6);
                Wyciag(D(apCzarny), new[] { new Vector2(-.015f, 0), new Vector2(.015f, 0), new Vector2(.01f, .3f), new Vector2(-.01f, .3f) }, mon + Vector3.up * .2f - Vector3.right * .26f, Vector3.right, Vector3.up, ws, .52f);
                var kr = sp - ws * .55f;   // krzesło obrotowe kasjera: podstawa 5-ramienna, kolumna, siedzisko i oparcie toczone/wyciągane
                for (int l = 0; l < 5; l++) { var d5 = Quaternion.AngleAxis(l * 72f, Vector3.up) * Vector3.forward; D(apCzarny).Rura(kr + Vector3.up * .06f, kr + d5 * .3f + Vector3.up * .04f, .018f, 6); }
                D(apChrom).Rura(kr + Vector3.up * .06f, kr + Vector3.up * .5f * K, .025f, 8);
                Toczenie(D(apCzarny), new[] { new Vector2(0, .5f * K), new Vector2(.22f, .5f * K), new Vector2(.24f, .5f * K + .04f), new Vector2(.22f, .5f * K + .08f), new Vector2(0, .5f * K + .08f) }, kr, 14);
                Wyciag(D(apCzarny), Obrot(PZ(.05f, .42f, .02f, new Vector2(-.25f, .5f * K + .35f)), 8f, new Vector2(-.25f, .5f * K + .1f)), kr - Vector3.Cross(Vector3.up, ws) * .2f, Vector3.Cross(Vector3.up, ws), Vector3.up, ws, .4f);
                Czlowiek("Kasa ARENY", "kasjer", "siedzi", kr + Vector3.up * (.5f * K + .08f), ws, KlSiedzi);
            }
            BraxMiastoA.Akcja(root, su < 0 ? "arena_kasa" : "arena_kasa_2", "KUP BILET", "Kasa ARENY BRAXHORN: bilety na galę mistrzowską — sektor A–F.", start + Vector3.right * 1.0f + ws * .9f, 1.6f);
            Czlowiek("Kasa ARENY", "kibic w kolejce", "stoi", start + Vector3.right * 1.2f + ws * .75f, -ws, KlStoi);
            // tablica KASY podwieszona
            var tp = start + Vector3.right * 1.0f + Vector3.up * 3.2f + ws * -.3f;
            D(apAlu).OBox(tp, Vector3.forward, Vector3.up, Vector3.left, 1.6f, .45f, .05f);
            BraxUliceKit.Napis3D("KASY", tp + Vector3.left * .025f + Vector3.down * .12f, Vector3.left, .24f, .02f, apLit, apBoki);
            foreach (float zz in new[] { -.6f, .6f }) D(apChrom).Rura(tp + Vector3.forward * zz + Vector3.up * .225f, new Vector3(tp.x, apY0 + ApSufit, tp.z + zz), .006f, 4);
        }
        Miejsca["arena_hol"] = ApHolP(18.5f, 0f, 0f); Miejsca["arena_bramki"] = ApHolP(vB + 1.5f, 0f, 0f); apMiejsc += 2;
        BraxMiastoA.Log("WYSPA arena premium: hol — bramki " + (model ? "z modelu metro_bramka" : "z kodu") + ", 2 kasy, przegrody szklane");
        apElem += 6;
    }

    // TOALETY damska (+z) i męska (−z) w narożnikach holu: ściany z płytek, drzwi otwarte, kabiny HPL, misy, pisuary, umywalki
    static void ApToalety()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — toalety");
        float F = apF, t = .12f, h = 3.0f;
        foreach (float su in new[] { 1f, -1f })
        {
            bool dam = su > 0; string nz = dam ? "WC DAMSKA" : "WC MĘSKA";
            float xi0 = apC.x - 21.38f, xi1 = apC.x - 18.3f;
            float zN = apC.z + su * 3.72f, zF = apC.z + su * 6.88f, zi0 = Mathf.Min(zN, zF), zi1 = Mathf.Max(zN, zF);
            float dA = xi1 - 1.45f, dB = xi1 - .3f;   // drzwi (od strony środka hali)
            var wewn = apPlytki; var zew = apAkust;
            // ściana z drzwiami (zN) i pozostałe; lico „+” to +z / +x
            ApSciana(true, zN - su * t * .5f, xi0 - t, xi1 + t, t, F, h + F, new[] { (dA, dB, 2.75f) }, su > 0 ? zew : wewn, su > 0 ? wewn : zew);
            ApSciana(true, zF + su * t * .5f, xi0 - t, xi1 + t, t, F, h + F, null, su > 0 ? wewn : zew, su > 0 ? zew : wewn);
            ApSciana(false, xi0 - t * .5f, zi0, zi1, t, F, h + F, null, zew, wewn);
            ApSciana(false, xi1 + t * .5f, zi0, zi1, t, F, h + F, null, wewn, zew);
            ApSkrzydlo(true, zN, dA, dB - dA, 2.75f, su, 1f, apHpl);
            ApSufitPom(xi0, xi1, zi0, zi1, F + h, F + h + .15f, new List<Rect> { Rect.MinMaxRect(xi0 + .8f, zi0 + 1.0f, xi0 + 2.0f, zi0 + 1.6f), Rect.MinMaxRect(xi1 - 1.3f, zi0 + 1.0f, xi1 - .7f, zi0 + 2.2f) }, xi0 - t, xi1 + t, zi0 - t, zi1 + t);
            BraxUliceKit.Napis3D(nz, new Vector3((dA + dB) * .5f, F + 2.95f, zN - su * (t + .005f)), new Vector3(0, 0, -su), .14f, .015f, apLit, apBoki);
            // kabiny przy ścianie zachodniej (xi0): przegrody HPL na nóżkach, drzwi uchylone, misa ustępowa toczona
            int nk = dam ? 2 : 1; float kw = .95f, kd = 1.45f;
            var hpl = D(apHpl);
            for (int k = 0; k < nk; k++)
            {
                float z0 = dam ? zi0 + .2f + k * kw : zi0 + .2f, z1 = z0 + kw; if (!dam && su < 0) { z0 = zi1 - .2f - kw; z1 = zi1 - .2f; } else if (su < 0) { z0 = zi1 - .2f - (k + 1) * kw; z1 = z0 + kw; }
                float xf = xi0 + kd;
                foreach (float zz in new[] { z0, z1 }) { hpl.OBox(new Vector3(xi0 + kd * .5f, F + .15f + 1.0f, zz), Vector3.right, Vector3.up, Vector3.forward, kd, 2.0f, .025f); KolAA("przegroda kabiny", new Vector3(xi0, F, zz - .02f), new Vector3(xf, F + 2.15f, zz + .02f)); }
                var dz = (Quaternion.AngleAxis(su * 30f, Vector3.up) * Vector3.forward) * (kw - .1f) * su;
                hpl.OBox(new Vector3(xf, F + 1.15f, z0 + .05f) + dz * .5f, dz.normalized, Vector3.up, Vector3.Cross(dz.normalized, Vector3.up), kw - .1f, 2.0f, .025f);
                foreach (float zz in new[] { z0, z1 }) D(apChrom).Rura(new Vector3(xf, F, zz), new Vector3(xf, F + .15f, zz), .015f, 6);
                D(apChrom).Rura(new Vector3(xf, F + 2.15f, z0), new Vector3(xf, F + 2.15f, z1), .018f, 6);
                var wc = new Vector3(xi0 + .38f, F, (z0 + z1) * .5f);
                Toczenie(D(apCeram), new[] { new Vector2(0, 0), new Vector2(.13f, 0), new Vector2(.14f, .05f), new Vector2(.17f, .3f), new Vector2(.19f, .39f), new Vector2(.18f, .41f), new Vector2(.15f, .41f), new Vector2(.13f, .37f), new Vector2(0, .3f) }, wc + Vector3.right * .12f, 16, .8f, 1.15f, Vector3.right);
                Wyciag(D(apCeram), PZ(.18f, .42f, .04f, new Vector2(-.0f, .62f)), wc + Vector3.left * .2f + Vector3.back * .2f + Vector3.right * .02f, Vector3.forward, Vector3.up, Vector3.right, .4f);
                KolAA("misa ustępowa", wc + new Vector3(-.25f, 0, -.2f), wc + new Vector3(.45f, .45f, .2f));
            }
            if (!dam)   // pisuary z przegrodą
                for (int k = 0; k < 2; k++)
                {
                    float zc = su < 0 ? zi1 - .2f - kw - .5f - k * .75f : zi0 + .2f + kw + .5f + k * .75f; var p = new Vector3(xi0, F, zc);
                    Wyciag(D(apCeram), new[] { new Vector2(.0f, .5f), new Vector2(.28f, .55f), new Vector2(.36f, .72f), new Vector2(.3f, 1.05f), new Vector2(.02f, 1.2f) }, p + Vector3.back * .19f, Vector3.forward, Vector3.up, Vector3.right, .38f);
                    if (k == 0) hpl.OBox(new Vector3(xi0 + .3f, F + 1.0f, zc + .375f * (su < 0 ? -1 : 1) * -1f), Vector3.right, Vector3.up, Vector3.forward, .55f, .9f, .02f);
                }
            // umywalki: blat na ścianie wschodniej (xi1 strona) — nie; przy ścianie zF: blat, misy nablatowe, baterie, lustro
            float zb = zF - su * .28f; var bl = D(BraxMiastoA.Mat("Arena WC: blat umywalek (konglomerat)", new Color(.2f, .2f, .21f), .35f, .1f));
            float bx0 = xi1 - 1.8f, bx1 = xi1 - .1f;
            Wyciag(bl, PZ(.55f, .05f, .012f, new Vector2(0, .85f)), new Vector3(bx0, F, zb), Vector3.right, Vector3.up, new Vector3(0, 0, -su), bx1 - bx0);
            KolAA("blat umywalek", new Vector3(bx0, F, Mathf.Min(zb - .3f, zb + .3f)), new Vector3(bx1, F + .88f, Mathf.Max(zb - .3f, zb + .3f)));
            for (int k = 0; k < 2; k++)
            {
                var um = new Vector3(bx0 + .45f + k * .8f, F + .875f, zb - su * .05f);
                Toczenie(D(apCeram), new[] { new Vector2(0, 0), new Vector2(.14f, 0), new Vector2(.2f, .1f), new Vector2(.2f, .13f), new Vector2(.18f, .13f), new Vector2(.17f, .04f), new Vector2(0, .03f) }, um, 18);
                var bt = um + new Vector3(0, 0, su * .22f); D(apChrom).Rura(bt, bt + Vector3.up * .3f, .015f, 8); D(apChrom).Rura(bt + Vector3.up * .3f, bt + Vector3.up * .3f - new Vector3(0, 0, su * .16f), .012f, 8);
            }
            D(apLustro).OBox(new Vector3((bx0 + bx1) * .5f, F + 1.65f, zF - su * .012f), Vector3.right, Vector3.up, new Vector3(0, 0, -su), bx1 - bx0 - .1f, 1.0f, .012f, true);
            D(apChrom).OBox(new Vector3((bx0 + bx1) * .5f, F + 1.13f, zF - su * .015f), Vector3.right, Vector3.up, new Vector3(0, 0, -su), bx1 - bx0 - .06f, .02f, .03f, true);
            ApGniazdko(new Vector3(bx1 - .2f, F + 1.1f, zF - su * .001f), new Vector3(0, 0, -su));
            BraxRegion.Warm(root, "Arena WC: światło (" + nz + ")", new Vector3((xi0 + xi1) * .5f, F + h - .4f, (zi0 + zi1) * .5f), .6f, 5f);
            Miejsca[dam ? "arena_wc_damska" : "arena_wc_meska"] = new Vector3(xi1 - .6f, F, zN - su * 1.2f); apMiejsc++;
            ApGasnica(new Vector3(xi1 + t + .002f, F + 1.2f, zN + su * .9f), Vector3.right);
            ApGniazdko(new Vector3(xi1 + t + .001f, F + .3f * K, zN + su * 2.2f), Vector3.right);
            apElem += 4;
        }
        BraxMiastoA.Log("WYSPA arena premium: toalety damska (2 kabiny) i męska (kabina, 2 pisuary), umywalki, lustra, sufit z LED");
    }

    // TUNEL ZAWODNIKÓW i 2 SZATNIE (blok przy ścianie wschodniej): sufit z wnękami LED, szafki z drzwiczkami, ławki, stół do masażu
    static void ApSzatnie()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — tunel i szatnie");
        float F = apF, t = .15f, h = 3.2f, x0 = apC.x + 15.97f, xW = apC.x + apAp - ApWew, x1 = xW - .1f, zt = 1.3f, zs = 6.4f, cz = apC.z;
        var graf = apAkust;
        // front (z otworem tunelu), boki zewnętrzne, ściany tunelu z drzwiami do szatni, okładzina tylna (z drzwiami służbowymi w tunelu)
        ApSciana(false, x0 + t * .5f, cz - zs - t, cz + zs + t, t, F, F + h, new[] { (cz - zt, cz + zt, 2.85f) }, graf, apTynk);
        foreach (float sg in new[] { -1f, 1f })
        {
            ApSciana(true, cz + sg * (zs + t * .5f), x0 + t, x1 + .095f, t, F, F + h, null, sg > 0 ? apTynk : graf, sg > 0 ? graf : apTynk);
            ApSciana(true, cz + sg * (zt + t * .5f), x0 + t, x1, t, F, F + h, new[] { (apC.x + 17.0f, apC.x + 18.15f, 2.75f) }, sg > 0 ? graf : apTynk, sg > 0 ? apTynk : graf);
            ApSkrzydlo(true, cz + sg * (zt + t * .5f), apC.x + 17.0f, 1.15f, 2.75f, sg, 1f, BraxMiastoA.Mat("Arena premium: drzwi szatni (laminat dąb)", new Color(.6f, .47f, .32f), .5f));
        }
        ApSciana(false, x1 + .0475f, cz - zs, cz + zs, .095f, F, F + h, new[] { (cz - .82f, cz + .82f, 2.81f) }, apTynk, graf, true);
        ApSufitPom(x0 + t, x1, cz - zs, cz + zs, F + h, F + h + .15f, new List<Rect>
        {
            Rect.MinMaxRect(x0 + .8f, cz - .2f, x1 - .8f, cz + .2f),
            Rect.MinMaxRect(x0 + 1.4f, cz + 2.6f, x0 + 2.6f, cz + 3.2f), Rect.MinMaxRect(x1 - 2.4f, cz + 3.6f, x1 - 1.2f, cz + 4.2f),
            Rect.MinMaxRect(x0 + 1.4f, cz - 3.2f, x0 + 2.6f, cz - 2.6f), Rect.MinMaxRect(x1 - 2.4f, cz - 4.2f, x1 - 1.2f, cz - 3.6f),
        }, x0, xW - .01f, cz - zs - t, cz + zs + t);
        // posadzka sportowa w bloku (nad posadzką hali: 2 cm, z fazą)
        BraxUliceKit.S(apGuma).Quad(new Vector3(x0 + t, F + .02f, cz - zs), new Vector3(x1, F + .02f, cz - zs), new Vector3(x1, F + .02f, cz + zs), new Vector3(x0 + t, F + .02f, cz + zs), Vector3.up);
        // portal tunelu: litery BRAXHORN nad wylotem, pas LED w tunelu
        BraxUliceKit.Napis3D("BRAXHORN", new Vector3(x0 - .005f, F + 2.9f, cz), Vector3.left, .22f, .03f, apLit, apBoki);
        foreach (float sg in new[] { -1f, 1f }) D(apLed).OBox(new Vector3((x0 + x1) * .5f, F + .35f, cz + sg * (zt - .006f)), Vector3.right, Vector3.up, new Vector3(0, 0, -sg), x1 - x0 - .6f, .04f, .012f, true);
        ApZnakWyjscia(new Vector3(x1 - .005f, F + 3.05f, cz), Vector3.left);
        // wybieg do klatki między barierkami
        float xk = apC.x + 4.55f * K / Mathf.Cos(Mathf.PI / 8f) + .6f + 1.45f;
        foreach (float sg in new[] { -1f, 1f })
        {
            var a = new Vector3(xk, F, cz + sg * 1.45f); var b = new Vector3(x0 - .1f, F, cz + sg * 1.45f); float hb = 1.0f * K;
            D(apAlu).Rura(a + Vector3.up * hb, b + Vector3.up * hb, .03f, 8); D(apAlu).Rura(a + Vector3.up * hb * .5f, b + Vector3.up * hb * .5f, .018f, 6);
            int np = Mathf.CeilToInt((b.x - a.x) / 1.6f); for (int s = 0; s <= np; s++) { var p = Vector3.Lerp(a, b, s / (float)np); D(apAlu).Rura(p, p + Vector3.up * hb, .026f, 6); }
            KolAA("barierka wybiegu", a + new Vector3(0, 0, -.05f), b + new Vector3(0, hb, .05f));
        }
        // wyposażenie szatni
        var lak = D(BraxMiastoA.Mat("Arena: szafki szatni (stal lakierowana)", new Color(.2f, .21f, .23f), .45f, .4f));
        var stol = D(BraxMiastoA.Mat("Arena premium: stół do masażu (skaj czarny)", new Color(.07f, .07f, .075f), .55f));
        foreach (float sg in new[] { 1f, -1f })
        {
            bool brax = sg > 0; string kto = brax ? "SZATNIA BRAXA" : "SZATNIA GOŚCI";
            float zw = cz + sg * zs;   // lico ściany zewnętrznej od środka
            var ws = new Vector3(0, 0, -sg);
            var lakD = D(brax ? BraxMiastoA.Mat("Arena premium: drzwiczki szafek Braxa (czerwień)", new Color(.55f, .07f, .06f), .4f, .3f) : BraxMiastoA.Mat("Arena premium: drzwiczki szafek gości (granat)", new Color(.08f, .15f, .42f), .4f, .3f));
            // szafki: korpus z daszkiem skośnym (profil), drzwiczki osobno ze szczelinami, uchwyty
            float lx0 = x0 + t + .5f, lx1 = x1 - .3f, dp = .5f, hl = 1.8f * K; int nl = Mathf.FloorToInt((lx1 - lx0) / .5f); float lw = (lx1 - lx0) / nl;
            Wyciag(lak, new[] { new Vector2(0, 0), new Vector2(dp - .06f, 0), new Vector2(dp - .06f, .1f), new Vector2(dp, .1f), new Vector2(dp, hl), new Vector2(0, hl + .18f) }, new Vector3(lx0, F + .02f, zw), Vector3.right, Vector3.up, ws, lx1 - lx0);
            for (int k = 0; k < nl; k++)
            {
                var dc = new Vector3(lx0 + (k + .5f) * lw, F + .02f + (.1f + hl) * .5f, zw - sg * (dp + .011f));
                lakD.OBox(dc, Vector3.right, Vector3.up, ws, lw - .006f, hl - .12f, .018f);
                D(apChrom).Rura(dc + ws * .01f + Vector3.right * (lw * .35f) + Vector3.up * .05f, dc + ws * .01f + Vector3.right * (lw * .35f) - Vector3.up * .08f, .009f, 6);
            }
            KolAA("szafki szatni", new Vector3(lx0, F, Mathf.Min(zw, zw - sg * dp)), new Vector3(lx1, F + hl + .2f, Mathf.Max(zw, zw - sg * dp)));
            // ławka: 3 listwy dębowe na stalowej ramie (rury)
            float zl = zw - sg * 1.25f, bx0 = x0 + 1.0f, bx1 = x1 - .8f, hb = .45f * K;
            for (int k = 0; k < 3; k++) Wyciag(D(apDab), PZ(.11f, .035f, .012f, new Vector2(0, hb - .018f)), new Vector3(bx0, F, zl + (k - 1) * .125f), Vector3.right, Vector3.up, Vector3.forward, bx1 - bx0);
            for (float x = bx0 + .2f; x < bx1; x += (bx1 - bx0 - .4f) / 2f)
            {
                foreach (float e in new[] { -.16f, .16f }) D(apStal).Rura(new Vector3(x, F, zl + e), new Vector3(x, hb + F - .036f, zl + e), .02f, 6);
                D(apStal).Rura(new Vector3(x, F + hb - .05f, zl - .18f), new Vector3(x, F + hb - .05f, zl + .18f), .018f, 6);
            }
            KolAA("ławka szatni", new Vector3(bx0, F, zl - .2f), new Vector3(bx1, F + hb, zl + .2f));
            BraxMiastoA.Siedzisko(root, "ŁAWKA — " + kto, new Vector3((bx0 + bx1) * .5f, F + hb, zl), -ws);
            // stół do masażu: blat tapicerowany (profil zaokrąglony), nogi, poprzeczki
            float tz = cz + sg * 3.0f, tx0 = x0 + 2.2f, tx1 = tx0 + 1.9f, th = .75f * K;
            Wyciag(stol, PZ(.7f, .1f, .04f, new Vector2(0, th - .05f)), new Vector3(tx0, F, tz), Vector3.right, Vector3.up, Vector3.forward, tx1 - tx0);
            foreach (float xx in new[] { tx0 + .12f, tx1 - .12f }) foreach (float e in new[] { -.27f, .27f }) D(apChrom).Rura(new Vector3(xx, F, tz + e), new Vector3(xx, F + th - .1f, tz + e), .022f, 8);
            D(apChrom).Rura(new Vector3(tx0 + .12f, F + .25f, tz), new Vector3(tx1 - .12f, F + .25f, tz), .018f, 6);
            KolAA("stół do masażu", new Vector3(tx0, F, tz - .35f), new Vector3(tx1, F + th, tz + .35f));
            // lustro z ramą na froncie (od środka), gaśnica, gniazdka
            D(apLustro).OBox(new Vector3(x0 + t + .008f, F + 1.5f, cz + sg * 4.4f), Vector3.forward, Vector3.up, Vector3.right, 1.1f, 1.5f, .012f, true);
            foreach (float e in new[] { -1f, 1f }) { D(apAlu).OBox(new Vector3(x0 + t + .012f, F + 1.5f, cz + sg * 4.4f + e * .57f), Vector3.forward, Vector3.up, Vector3.right, .04f, 1.58f, .02f, true); D(apAlu).OBox(new Vector3(x0 + t + .012f, F + 1.5f + e * .77f, cz + sg * 4.4f), Vector3.forward, Vector3.up, Vector3.right, 1.18f, .04f, .02f, true); }
            ApGasnica(new Vector3(apC.x + 18.6f, F + 1.2f, cz + sg * (zt + t + .002f)), new Vector3(0, 0, sg));
            ApGniazdko(new Vector3(x0 + t + .001f, F + .3f * K, cz + sg * 2.0f), Vector3.right);
            ApGniazdko(new Vector3(x1 - .001f, F + .3f * K, cz + sg * 2.4f), Vector3.left);
            BraxUliceKit.Napis3D(kto, new Vector3(apC.x + 17.575f, F + 2.95f, cz + sg * (zt - .005f)), new Vector3(0, 0, -sg), .12f, .015f, apLit, apBoki);
            BraxRegion.Warm(root, "Arena: światło " + kto.ToLowerInvariant(), new Vector3((x0 + x1) * .5f, F + h - .45f, cz + sg * 3.8f), 1.0f, 7f);
            Miejsca[brax ? "arena_szatnia_braxa" : "arena_szatnia_gosci"] = new Vector3((x0 + x1) * .5f, F, cz + sg * 3.8f); apMiejsc++;
            Czlowiek(kto, "trener", "stoi", new Vector3(tx1 + .5f, F, tz), Vector3.left, KlStoi);
            apElem += 6;
        }
        BraxRegion.Warm(root, "Arena: światło tunelu", new Vector3((x0 + x1) * .5f, F + 2.6f, cz), .9f, 6f);
        Miejsca["arena_tunel"] = new Vector3(x0 + .5f, F, cz); Miejsca["arena_szatnie"] = new Vector3((x0 + x1) * .5f, F, cz); apMiejsc += 2;
        BraxMiastoA.Log("WYSPA arena premium: tunel zawodników + szatnia Braxa i gości (szafki, ławki, stół do masażu, lustro, sufit z LED)");
    }

    // PLAC PRZED WEJŚCIEM (właściciel 10:47: bez food trucków, pojazdów i budek z brył): nawierzchnia (plac terenu), ławki,
    // latarnie, zieleń w donicach — w dwóch rzędach po bokach osi wejścia, z dala od deptaka
    static void ApPlacPrzed()
    {
        BraxUliceKit.UstawGrupe("Wyspa: arena premium — plac przed wejściem");
        float y = apY0, xw = apC.x - apAp; int n = 0;
        var gran = D(apGranit); var dab = D(apDab); var stal = D(apStal);
        var ziel = D(BraxMiastoA.Mat("Arena premium: krzewy w donicach (zieleń)", new Color(.18f, .36f, .12f), .9f));
        foreach (float sz in new[] { -1f, 1f })
            for (int k = 0; k < 3; k++)
            {
                var p = new Vector3(xw - 4.5f - k * 4.2f, y, apC.z + sz * 7f);
                // donica granitowa (profil fazowany) z krzewem (bryła toczona, nieregularna)
                Toczenie(gran, new[] { new Vector2(0, 0), new Vector2(.62f, 0), new Vector2(.7f, .06f), new Vector2(.7f, .52f), new Vector2(.62f, .55f), new Vector2(0, .55f) }, p, 16);
                Toczenie(ziel, new[] { new Vector2(0, .55f), new Vector2(.55f, .58f), new Vector2(.68f, .85f), new Vector2(.6f, 1.15f), new Vector2(.35f, 1.35f), new Vector2(0, 1.4f) }, p, 10, 1f, .92f);
                KolAA("donica", p + new Vector3(-.7f, 0, -.7f), p + new Vector3(.7f, .55f, .7f));
                // ławka: siedzisko z listew na wspornikach stalowych, przodem do osi
                var b = p + Vector3.right * 2.6f; var fw = new Vector3(0, 0, -sz); float hb = .45f * K;
                for (int s = 0; s < 4; s++) Wyciag(dab, PZ(.09f, .04f, .012f, new Vector2(0, hb - .02f)), b + Vector3.left * .9f + fw * (s - 1.5f) * .11f, Vector3.right, Vector3.up, Vector3.forward, 1.8f);
                foreach (float e in new[] { -.7f, .7f }) Wyciag(stal, new[] { new Vector2(-.24f, 0), new Vector2(.24f, 0), new Vector2(.2f, hb - .04f), new Vector2(-.2f, hb - .04f) }, b + Vector3.right * (e - .03f), Vector3.right, Vector3.up, fw, .06f);
                KolAA("ławka placu", b + new Vector3(-.9f, 0, -.25f), b + new Vector3(.9f, hb, .25f));
                // latarnia (słup stożkowy, oprawa, świeci)
                var l = p + Vector3.left * 2.6f;
                Toczenie(stal, new[] { new Vector2(0, 0), new Vector2(.12f, 0), new Vector2(.12f, .05f), new Vector2(.07f, .1f), new Vector2(.05f, 4.6f), new Vector2(0, 4.6f) }, l, 10);
                Toczenie(D(BraxMiastoA.Swieci("Arena premium: klosz latarni", new Color(1f, .95f, .85f), new Color(1.8f, 1.6f, 1.3f))), new[] { new Vector2(0, 4.45f), new Vector2(.22f, 4.5f), new Vector2(.24f, 4.62f), new Vector2(0, 4.66f) }, l, 14);
                KolA("latarnia placu", l + Vector3.up * 2.3f, Vector3.forward, new Vector3(.25f, 4.6f, .25f));
                n++;
            }
        BraxMiastoA.Log("WYSPA arena premium: plac przed wejściem — " + n + " × (donica z zielenią, ławka, latarnia); bez food trucków i budek");
        apElem += n * 3;
    }

    static void ApZapiszDrobne()
    {
        var g = new GameObject("Arena premium: detale (bez cieni, LOD)").transform; g.SetParent(root, false);
        var rs = new List<Renderer>();
        foreach (var kv in apDrobne)
        {
            if (kv.Value.Wierzcholki == 0) continue;
            var o = BraxMiastoA.Obiekt(g, "detale: " + kv.Key.name, kv.Value, kv.Key, false);
            GameObjectUtility.SetStaticEditorFlags(o, StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic);
            rs.Add(o.GetComponent<Renderer>());
        }
        if (rs.Count > 0)
        {
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var lod = g.gameObject.AddComponent<LODGroup>(); float roz = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            lod.SetLODs(new[] { new LOD(Mathf.Clamp(roz / (2f * 90f * .52f), .01f, .9f), rs.ToArray()) }); lod.RecalculateBounds();
        }
        apDrobne.Clear();
    }
}
