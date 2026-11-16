using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// LOKALE W PARTERACH WIEŻOWCÓW NOWYCH DZIELNIC (MIASTO R1, 16.11.2026). Właściciel 17:38: „tą nową część miasta… trzeba jeszcze
// jakoś zagospodarować… coś pod grę… sklepy trzeba otworzyć… możesz kopiować, apteki”; 17:57: „buchmacherkę ma zrobić, biuro
// deweloperskie”; 17:55: „wszystko realistyczne… najlepsze materiały”.
// Dotąd parter wieżowca (BraxRegionFawelaWieze) miał witryny z obrazkiem pokoju (shader „pokój za szybą”) i atrapę portalu.
// Teraz wybrane przęsła parteru od Alei Centralnej i promenady to PRAWDZIWE LOKALE — ten sam generator co ulica główna
// i kamienice miasta B (BraxUlicePierzeje.WnetrzeKamienicy):
//  • przęsła lokalu (siatka filarów 3.6 m świata): witryny ze szkłem przezroczystym (dwustronnym), jedno przęsło = otwór drzwi
//    2.5 × 3.7 m z drzwiami lokalu (BraxDrzwiLokalu: przesuwne / drewniane / stalowe wg branży), cokolik przerwany w drzwiach;
//  • za witrynami lokal głębokości 7.05 m: posadzka, ściany, sufit 0.3 m nad witrynami, lada, towar, meble wg branży, lico
//    wewnętrzne ściany frontowej z ościeżami (od środka nie widać pustej bryły);
//  • szyld z liter przestrzennych na listwie nad witrynami (albo grafika firmy właściciela, jeśli jest w paczce grafik);
//  • kolizje: bryła wieżowca rozcięta — nad lokalem, parter poza lokalem, front lokalu bez otworu drzwi, posadzka lokalu
//    (płyty kwartału pod lokalem wycięte w BraxRegionMiastoB.NdKwartaly — bez dwóch powierzchni w jednej płaszczyźnie);
//  • gra: przy ladzie punkt AKCJI (KUP / ZAMÓW / POSTAW ZAKŁAD…), punkt świata przed drzwiami i za ladą (misje).
public static partial class BraxRegionFawela
{
    internal sealed class LokalWiezy
    {
        public readonly string bud, szyld, typ, idAkcji, akcja, odpowiedz; public readonly int strona, bay0, bayN, drzwi;
        public int numer => 301 + Array.IndexOf(LokaleWiez, this);   // numer lokalu (ziarno wnętrza, id punktów)
        public LokalWiezy(string bud, int strona, int bay0, int bayN, int drzwi, string szyld, string typ, string idAkcji, string akcja, string odpowiedz)
        { this.bud = bud; this.strona = strona; this.bay0 = bay0; this.bayN = bayN; this.drzwi = drzwi; this.szyld = szyld; this.typ = typ; this.idAkcji = idAkcji; this.akcja = akcja; this.odpowiedz = odpowiedz; }
    }

    // strona: 0 = lico północne (+z, rząd C — promenada), 1 = lico południowe (−z, rząd A — Aleja Centralna); bay0 — pierwsze
    // przęsło lokalu w siatce parteru (s od lewej patrząc z wnętrza: strona 0 od zachodu, strona 1 od wschodu); drzwi — przęsło
    // drzwi liczone od bay0. Przęsło portalu holu wieżowca (środek lica) nigdy nie wchodzi do lokalu (sprawdzane).
    internal static readonly LokalWiezy[] LokaleWiez =
    {
        // rząd C — promenada (gastronomia, usługi) i BRAXHORN BET najbliżej przystanku ALEJA CENTRALNA
        new LokalWiezy("C5_U07", 0, 8, 4, 1, "BRAXHORN BET", "bukmacher", "bet_kasa", "POSTAW ZAKŁAD",
            "Kasa BRAXHORN BET: Liga Mistrzów — rozpiska i kursy na tablicy. Stawiasz tyle, ile masz na koncie."),
        new LokalWiezy("C5_U07", 0, 1, 3, 1, "KAWIARNIA", "kawiarnia", "aleja_kawiarnia", "ZAMÓW", "Barista: Espresso, cappuccino, coś słodkiego do kawy?"),
        new LokalWiezy("C5_U04", 0, 8, 3, 1, "PIZZERIA", "pizzeria", "aleja_pizzeria", "ZAMÓW", "Pizzerman: Margherita z pieca — dwanaście minut."),
        new LokalWiezy("C5_U09", 0, 1, 3, 1, "CUKIERNIA", "cukiernia", "aleja_cukiernia", "KUP", "Cukiernik: Torty, pączki, sernik — wszystko z dzisiaj."),
        new LokalWiezy("C5_U09", 0, 8, 3, 1, "BISTRO", "bistro", "aleja_bistro", "ZAMÓW", "Kucharz: Kurczak z rożna, frytki, surówka?"),
        new LokalWiezy("C5_U02", 0, 1, 3, 1, "FRYZJER", "fryzjer", "aleja_fryzjer", "ZAPISZ SIĘ", "Fryzjer: Strzyżenie i broda — wolne miejsce za kwadrans."),
        // rząd A — Aleja Centralna (sklepy, apteka, biuro dewelopera Patryka i Moniki)
        new LokalWiezy("C5_U12", 1, 1, 3, 1, "WAWER NIERUCHOMOŚCI", "deweloper", "deweloper_biuro", "ZAPYTAJ O MIESZKANIE",
            "Biuro sprzedaży: Mieszkania w wieżowcach przy Alei Centralnej — od kawalerki po apartament. Umowa i klucze u nas."),
        new LokalWiezy("C5_U12", 1, 10, 3, 1, "APTEKA", "apteka", "aleja_apteka", "KUP", "Farmaceutka: Plastry, maść na stłuczenia, elektrolity — co podać?"),
        new LokalWiezy("C5_U08", 1, 2, 4, 1, "DELIKATESY", "market", "aleja_delikatesy", "KUP", "Kasjerka: Pieczywo, nabiał, owoce, woda — płaci pan kartą?"),
        new LokalWiezy("C5_U08", 1, 25, 3, 1, "PIEKARNIA", "piekarnia", "aleja_piekarnia", "KUP", "Piekarz: Chleb na zakwasie prosto z pieca, bułki, drożdżówki."),
        new LokalWiezy("C5_U00", 1, 8, 3, 1, "OPTYK", "optyk", "aleja_optyk", "KUP", "Optyk: Okulary korekcyjne i przeciwsłoneczne — badanie wzroku na miejscu."),
        new LokalWiezy("C5_U03", 1, 5, 3, 1, "KWIACIARNIA", "kwiaty", "aleja_kwiaciarnia", "KUP", "Kwiaciarka: Bukiet na dziś? Róże, tulipany, piwonie."),
    };

    const float LokGl = 7.05f;                 // głębokość lokalu (jak hol ulicy głównej)
    const float LokTyl = LokGl - .124f;        // ściana tylna lokalu od lica (Wykonczenie: −(gl − .12) + .004)
    const float LokOds = .37f;                 // ściany boczne lokalu od osi filara (za filarem 0.88 m — niewidoczne od ulicy)
    const float LokDrzwiW = 2.5f, LokDrzwiH = 3.7f;
    static float LokSufit => WHp - 1.25f + .3f; // sufit lokalu nad posadzką: 0.3 m nad nadprożem witryn
    internal static int lokaleWiez;
    internal static readonly List<string> lokaleWiezZbudowane = new List<string>(), lokaleWiezPlan = new List<string>();
    internal static void LokaleReset() { lokaleWiez = 0; lokaleWiezZbudowane.Clear(); lokaleWiezPlan.Clear(); }

    /// <summary>Ściany prostokąta uskoku (kolejność jak w WiezaMurowana): 0 lico +z, 1 lico −z, 2 lico −x, 3 lico +x.</summary>
    static Fr[] ScianyProstokata(Rect rc) => new[]
    {
        Fr.Nowa(new Vector3(rc.xMin, 0f, rc.yMax), Vector3.forward, rc.width), Fr.Nowa(new Vector3(rc.xMax, 0f, rc.yMin), Vector3.back, rc.width),
        Fr.Nowa(new Vector3(rc.xMin, 0f, rc.yMin), Vector3.left, rc.height), Fr.Nowa(new Vector3(rc.xMax, 0f, rc.yMax), Vector3.right, rc.height)
    };

    /// <summary>Osie filarów na ścianie (siatka 3.6 m ŚWIATA — pokój za szybą nigdy nie ma ściany w środku okna).</summary>
    static List<float> LinieSiatki(Fr F)
    {
        const float Fil = .4f; float L = F.L;
        bool wzdX = Mathf.Abs(F.R.x) > .5f; float a0 = wzdX ? F.A.x : F.A.z, kier = wzdX ? F.R.x : F.R.z;
        var linie = new List<float>();
        for (int j = Mathf.FloorToInt(Mathf.Min(a0, a0 + kier * L) / WG) - 1; j <= Mathf.CeilToInt(Mathf.Max(a0, a0 + kier * L) / WG) + 1; j++)
        { float s = (j * WG - a0) / kier; if (s > Fil + .15f && s < L - Fil - .15f) linie.Add(s); }
        linie.Sort();
        return linie;
    }
    static int WejscieHolu(int si, int osi) => (si < 2 && osi >= 3) || (si >= 2 && osi >= 7) ? osi / 2 : -1;

    /// <summary>Lokal na ścianie: przęsła q0…q1 (włącznie), przęsło drzwi qd; false = nie mieści się albo zachodzi na portal holu.</summary>
    static bool Przesla(LokalWiezy l, int osi, int wejscie, out int q0, out int q1, out int qd)
    {
        q0 = l.bay0 < 0 ? osi + l.bay0 : l.bay0; q1 = q0 + l.bayN - 1; qd = q0 + l.drzwi;
        return q0 >= 0 && q1 < osi && l.bayN >= 2 && qd >= q0 && qd <= q1 && (wejscie < q0 || wejscie > q1);
    }

    sealed class LokalPlan { public LokalWiezy l; public Fr F; public int q0, q1, qd; public float s0, s1, sc; public List<Rect> otw = new List<Rect>(); }

    static Material lokSzklo, lokLico;
    static Material LokSzklo { get { if (!lokSzklo) lokSzklo = BraxUliceKit.SzkloPrzezroczyste; return lokSzklo; } }
    static Material LokLico { get { if (!lokLico) lokLico = T("wieże: lico wewnętrzne lokalu (gładź biała)", 2.4f, new Color(.96f, .95f, .93f), .9f, "white_plaster_02", "white_stucco"); return lokLico; } }

    /// <summary>Prostokąty posadzek lokali (świat x/z) dla wieżowca w obrysie w × d — płyty kwartału pod nimi są wycinane
    /// (BraxRegionMiastoB.NdKwartaly). Liczone tą samą siatką co WiezaMurowana.</summary>
    internal static List<(string klucz, Rect r)> PosadzkiLokali(string id, float x, float z, float w, float d)
    {
        var wynik = new List<(string, Rect)>();
        var sciany = ScianyProstokata(Rect.MinMaxRect(x - w * .5f, z - d * .5f, x + w * .5f, z + d * .5f));
        foreach (var l in LokaleWiez.Where(q => q.bud == id))
        {
            var F = sciany[l.strona]; var linie = LinieSiatki(F); int osi = Mathf.Max(0, linie.Count - 1);
            if (!Przesla(l, osi, WejscieHolu(l.strona, osi), out int q0, out int q1, out _)) continue;
            // wycięcie płyt 4 cm szersze pod ścianami bocznymi i tylną lokalu (16.11, kontrola migotania: krawędź płyty nie leży w licu
            // ściany lokalu — szczelina za ścianą, w bryle wieży, niewidoczna); od ulicy wycięcie w licu fasady jak było
            wynik.Add((id + "|" + l.szyld, RectF(F, linie[q0] + LokOds - .04f, linie[q1 + 1] - LokOds + .04f, .04f, LokTyl + .04f)));
        }
        return wynik;
    }

    static Rect RectF(Fr F, float s0, float s1, float d0, float d1)
    {
        var a = F.P(s0, 0f, d0); var b = F.P(s1, 0f, d1);
        return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z), Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z));
    }

    static IEnumerable<Rect> MinusRect(Rect a, Rect b)
    {
        float x0 = Mathf.Max(a.xMin, b.xMin), x1 = Mathf.Min(a.xMax, b.xMax), z0 = Mathf.Max(a.yMin, b.yMin), z1 = Mathf.Min(a.yMax, b.yMax);
        if (x1 - x0 < .005f || z1 - z0 < .005f) { yield return a; yield break; }
        if (z0 > a.yMin + .005f) yield return Rect.MinMaxRect(a.xMin, a.yMin, a.xMax, z0);
        if (z1 < a.yMax - .005f) yield return Rect.MinMaxRect(a.xMin, z1, a.xMax, a.yMax);
        if (x0 > a.xMin + .005f) yield return Rect.MinMaxRect(a.xMin, z0, x0, z1);
        if (x1 < a.xMax - .005f) yield return Rect.MinMaxRect(x1, z0, a.xMax, z1);
    }

    static void KolBox(Transform tr, string n, Vector3 a, Vector3 b)
    {
        var lo = Vector3.Min(a, b); var hi = Vector3.Max(a, b); if (hi.x - lo.x < .01f || hi.y - lo.y < .01f || hi.z - lo.z < .01f) return;
        var c = new GameObject(n).AddComponent<BoxCollider>(); c.transform.SetParent(tr, false); c.transform.position = (lo + hi) * .5f; c.size = hi - lo;
    }

    /// <summary>Lico wewnętrzne ściany frontowej lokalu (od środka) z otworami witryn i drzwi — na głębokości ościeży.</summary>
    static void LicoWewnetrzne(Bud b, LokalPlan p, float y0, float y1, float gl)
    {
        var smiec = new Bud();
        Sciana(smiec, p.F, p.s0 + LokOds, p.s1 - LokOds, y0, y1, p.otw.Where(o => o.xMax > p.s0 + LokOds && o.xMin < p.s1 - LokOds).ToList(), float.NaN, gl, b, gl);
    }

    /// <summary>Lokal po zbudowaniu bryły: drzwi, szyld, wnętrze (generator ulicy), punkty gry. Zwraca korzeń lokalu.</summary>
    static Transform ZbudujLokal(Transform korzen, string id, LokalPlan p, float yBase, float gl)
    {
        var l = p.l; var F = p.F; float k = K;
        var lr = new GameObject("Lokal " + l.szyld + " (" + id + ")").transform; lr.SetParent(korzen, false); lr.position = Vector3.zero;
        // drzwi w otworze przęsła drzwi (ościeżnica 5–15 cm w ościeżu 26 cm), próg 2.5 cm
        BraxDrzwiLokalu.Podwojne(lr, "R1_WIEZA_" + id + "_" + BraxUliceKit.Bezpieczna(l.szyld), l.szyld, F.P(p.sc, yBase, 0f), F.N, LokDrzwiW, LokDrzwiH, gl, .025f, BraxDrzwiLokalu.ModelDla(l.typ));
        // szyld: listwa z aluminium grafitowego przed filarami (filary parteru wysunięte 21 cm) w pasie nad witrynami
        float yOkna = yBase + WHp - 1.25f, yGzyms = yBase + WHp - .28f, yc = (yOkna + yGzyms) * .5f, sa = p.s0 + .3f, sb = p.s1 - .3f, sm = (sa + sb) * .5f;
        BraxUliceKit.UstawGrupe("Szyld " + l.szyld + " (" + id + ")", 160f);
        var listwa = BraxUliceKit.Mat("listwa montażowa szyldu (aluminium grafit)", new Color(.12f, .125f, .13f), .45f, .55f);
        BraxUliceKit.S(listwa).Box(F.P(sa, yc - .36f, -.215f), F.P(sb, yc + .36f, -.26f));
        string firma = l.typ == "apteka" ? "sobczak_apteka" : l.typ == "piekarnia" ? "sobczak_piekarnia" : l.typ == "cukiernia" ? "sobczak_cukiernia"
                     : l.typ == "kawiarnia" ? "sobczak_kawiarnia" : l.typ == "market" ? "sobczak_delikatesy" : null;   // grafiki firm właściciela (3.11)
        float wG = Mathf.Min(sb - sa - .6f, 3.4f), hG = wG / 5f;
        if (firma == null || !BraxMiastoBillboardy.Mural(lr, firma, F.P(sm, yc, -.27f), F.N, wG, hG))
        {
            var lico = BraxUliceKit.Mat("litery przestrzenne: lico " + l.szyld + " (podświetlenie nocą)",
                l.typ == "apteka" ? new Color(.10f, .55f, .28f) : l.typ == "bukmacher" ? new Color(.95f, .76f, .22f) : new Color(.95f, .93f, .86f), .45f, .05f);
            var boki = BraxUliceKit.Mat("litery przestrzenne: boki grafit", new Color(.06f, .06f, .065f), .5f, .4f);
            float cap = Mathf.Min(.44f, (sb - sa - .8f) / Mathf.Max(.01f, BraxUliceKit.SzerokoscNapisu(l.szyld, 1f)));
            BraxUliceKit.Napis3D(l.szyld, F.P(sm, yc - cap * .5f, -.26f), F.N, cap, .07f, lico, boki);
        }
        // wnętrze: układ generatora ulicy (A w licu na końcu s1, R = −F.R, d > 0 na zewnątrz), posadzka w poziomie płyt
        var A = F.P(p.s1, yBase - .114f, 0f);
        BraxUlicePierzeje.WnetrzeKamienicy(lr, l.szyld, l.typ, l.numer, A, -F.R, F.N, p.s1 - p.s0, LokGl + .55f, p.s1 - p.sc, LokSufit + .114f - .016f, LokOds);
        // punkty gry: przed drzwiami (świat) i AKCJA przy ladzie od strony klienta (punkt „za ladą” zapisuje generator)
        BraxUliceKit.Punkt("lokal_" + l.idAkcji + "_drzwi", "przed drzwiami: " + l.szyld + " (wieżowiec " + id + ", Aleja Centralna) — AKCJA = drzwi", F.P(p.sc, yBase, -1.2f));
        Vector3 klient = F.P(p.sc, yBase, 3.2f);
        var swiat = UnityEngine.Object.FindAnyObjectByType<BraxCityWorld>();
        string idLady = "ulica_kamienica_" + l.numer + "_" + BraxUliceKit.Bezpieczna(l.szyld) + "_lada";
        var lada = swiat && swiat.points != null ? swiat.points.FirstOrDefault(q => q != null && q.id == idLady) : null;
        if (lada != null) klient = lada.position + F.N * (.6f * k + 1.25f);
        BraxMiastoA.Akcja(lr, l.idAkcji, l.akcja, l.odpowiedz, klient, 1.6f);
        lokaleWiez++; lokaleWiezZbudowane.Add(id + "|" + l.szyld);
        Debug.Log("BRAXHORN_LOKALE_WIEZ " + id + ": " + l.szyld + " (" + l.typ + ", nr " + l.numer + ") — przęsła " + p.q0 + "…" + p.q1 + ", drzwi w przęśle " + p.qd
                  + ", szer. " + (p.s1 - p.s0).ToString("F1") + " m, głęb. " + LokGl.ToString("F2") + " m, drzwi (" + F.P(p.sc, yBase, 0f).ToString("F1") + "), AKCJA „" + l.akcja + "”"
                  + (lada != null ? " przy ladzie" : " (bez punktu lady — przy drzwiach)"));
        return lr;
    }

    /// <summary>Kolizje wieżowca z lokalami: nad lokalami cała bryła; parter — obrys bez lokali; front lokalu bez otworu drzwi;
    /// posadzka lokalu (płyty kwartału wycięte).</summary>
    static void KolizjeZLokalami(Transform korzen, string id, float x, float z, float w, float d, float yBase, float top, float gl, float wys, List<LokalPlan> plany)
    {
        var g = new GameObject("Budynek " + id + " kolizja (z lokalami w parterze)").transform; g.SetParent(korzen, false); g.position = Vector3.zero;
        float yL = yBase + LokSufit;
        var FP = Rect.MinMaxRect(x - w * .5f - wys, z - d * .5f - wys, x + w * .5f + wys, z + d * .5f + wys);
        KolBox(g, "bryła nad lokalami", new Vector3(FP.xMin, yL, FP.yMin), new Vector3(FP.xMax, top, FP.yMax));
        var reszta = new List<Rect> { FP };
        foreach (var p in plany) { var wc = RectF(p.F, p.s0 + LokOds, p.s1 - LokOds, -wys, LokTyl); reszta = reszta.SelectMany(r => MinusRect(r, wc)).ToList(); }
        foreach (var r in reszta) KolBox(g, "parter poza lokalami", new Vector3(r.xMin, yBase - .3f, r.yMin), new Vector3(r.xMax, yL, r.yMax));
        foreach (var p in plany)
        {
            var F = p.F; float sA = p.s0 + LokOds, sB = p.s1 - LokOds, dl = p.sc - LokDrzwiW * .5f, dp = p.sc + LokDrzwiW * .5f;
            KolBox(g, "front lokalu " + p.l.szyld + " (lewa)", F.P(sA, yBase - .3f, -wys), F.P(dl, yL, gl));
            KolBox(g, "front lokalu " + p.l.szyld + " (prawa)", F.P(dp, yBase - .3f, -wys), F.P(sB, yL, gl));
            KolBox(g, "nad drzwiami lokalu " + p.l.szyld, F.P(dl, yBase + LokDrzwiH, -wys), F.P(dp, yL, gl));
            KolBox(g, "posadzka lokalu " + p.l.szyld, F.P(sA, yBase - .3f, .04f), F.P(sB, yBase, LokTyl));
        }
    }
}
