using System.Collections.Generic;
using UnityEngine;

// MENU PAUZY (build 14; właściciel: propozycje 21:20 zatwierdzone 21:27 + „MENU TESTOWE” 22:54). Rysowane przez
// BraxTouchControls (IMGUI, nad wszystkim). Ekrany:
//  • GŁÓWNE: WRÓĆ, USTAWIENIA, MAPA, ZGŁOŚ (zrzut + pozycja), UTKNĄŁEM (najbliższy chodnik), TRYB ZDJĘĆ (bez interfejsu
//    do dotknięcia), TEST, AUTORZY (licencje CC BY — musi zostać dostępne);
//  • USTAWIENIA: głośność muzyki i efektów osobno, czułość kamery, rozmiar przycisków (małe / średnie / duże), jakość grafiki
//    (niska / średnia / wysoka — domyślnie automatycznie wg telefonu, wybór ręczny zapamiętany), język (polski / English);
//  • TEST: licznik kl./s i pamięci, teleport do miejsc (BraxMiejscaTestowe), pora dnia i pogoda — „wkrótce” (bez logiki),
//    start rejonu od nowa. W wersji „do ludzi” ukryć: PokazTest = false.
//  • MAPA i AUTORZY — przeniesione z dawnego menu (bez zmian działania).
// Kafle i suwaki duże pod kciuk, układ 4 × 2 w poziomie i 2 × 4 w pionie, w strefie bezpiecznej ekranu.
// Napisy z BraxTeksty (PL / EN). Liczby na ekranie z pamięci podręcznej — bez alokacji co klatkę.
public sealed class BraxMenuGry
{
    public const bool PokazTest = true;
    public enum Ekran { Glowne, Ustawienia, Test, Teleport, Mapa, Autorzy }
    public Ekran ekran;
    readonly BraxTouchControls ster;
    float s; Rect sa;
    List<BraxMiejscaTestowe.Miejsce> miejsca; int strona, stron = 1, naStrone = 1;
    string stronaTekst = ""; int stronaKlucz = -1;
    string[] autorzy; int autorzyStrona; readonly List<string> autorzyStrony = new List<string>(); float autorzyS = -1; int autorzyWersja = -1;
    string notaGrafiki = ""; int notaKlucz = -1;
    static readonly string[] procenty = new string[101], czulosci = new string[26];

    public BraxMenuGry(BraxTouchControls s) { ster = s; }

    BraxMapa mapa; string mapaTytul = "";   // szukana raz przy otwarciu menu (FindObjectsByType alokuje)

    public void Otworz(Ekran e = Ekran.Glowne)
    {
        ekran = e; if (e == Ekran.Teleport) Miejsca();
        mapa = BraxMapa.Aktywna; if (!mapa || !mapa.obraz) mapa = null;
        mapaTytul = mapa ? mapa.tytul + "  ·  " + BraxTeksty.T("menu.mapa_ty") : "";
    }

    // wstecz (przycisk „wstecz” telefonu): z podekranu do głównego; z głównego — true = zamknij menu
    public bool Wstecz()
    {
        if (ekran == Ekran.Teleport) { ekran = Ekran.Test; return false; }
        if (ekran != Ekran.Glowne) { ekran = Ekran.Glowne; return false; }
        return true;
    }

    public void Rysuj(BraxGymPlayer g)
    {
        float W = Screen.width, H = Screen.height; s = Mathf.Min(W, H); sa = BraxGui.BezpiecznyProstokat();
        BraxGui.Prostokat(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, ekran == Ekran.Mapa ? .82f : .66f));
        switch (ekran)
        {
            case Ekran.Glowne: Glowne(g); break;
            case Ekran.Ustawienia: Ustawienia(); break;
            case Ekran.Test: Test(g); break;
            case Ekran.Teleport: Teleport(g); break;
            case Ekran.Mapa: Mapa(g); break;
            case Ekran.Autorzy: Autorzy(); break;
        }
    }

    // ---------------- ekrany ----------------
    void Glowne(BraxGymPlayer g)
    {
        float pad = s * .05f, top = sa.y + s * .04f;
        BraxGui.Napis(new Rect(sa.x + pad, top, sa.width, s * .04f), "BRAXHORN CITY", BraxGui.Styl(Mathf.RoundToInt(s * .026f), true, TextAnchor.MiddleLeft), BraxGui.Akcent, false);
        BraxGui.Napis(new Rect(sa.x + pad, top + s * .035f, sa.width, s * .085f), BraxTeksty.T("menu.pauza"), BraxGui.Styl(Mathf.RoundToInt(s * .065f), true, TextAnchor.MiddleLeft), BraxGui.Biel);
        bool poziom = sa.width > sa.height;
        int kol = poziom ? 4 : 2, wier = 8 / kol;
        float gap = s * .025f, gx = sa.x + pad, gw = sa.width - 2 * pad;
        if (gw > s * 2.3f) { gx += (gw - s * 2.3f) * .5f; gw = s * 2.3f; }
        float gy = top + s * .15f, gh = sa.yMax - pad * .8f - gy;
        float kw = (gw - (kol - 1) * gap) / kol, kh = Mathf.Min((gh - (wier - 1) * gap) / wier, s * .34f);
        int i = 0;
        Rect K() { var r = new Rect(gx + (i % kol) * (kw + gap), gy + (i / kol) * (kh + gap), kw, kh); i++; return r; }
        if (Kafel(K(), BraxTeksty.T("menu.wroc"), BraxIkony.Wroc, glowny: true)) { ster.Pauza(false); return; }
        if (Kafel(K(), BraxTeksty.T("menu.ustawienia"), BraxIkony.Zebatka)) ekran = Ekran.Ustawienia;
        bool jestMapa = mapa;
        if (Kafel(K(), BraxTeksty.T(jestMapa ? "menu.mapa" : "menu.mapa_wkrotce"), BraxIkony.Mapa, aktywny: jestMapa)) ekran = Ekran.Mapa;
        if (Kafel(K(), BraxTeksty.T("menu.zglos"), BraxIkony.Aparat)) ster.Zglos();
        if (Kafel(K(), BraxTeksty.T("menu.utknalem"), BraxIkony.Pinezka)) { var msg = BraxUtknalem.Ratuj(g); ster.Pauza(false); ster.Komunikat(msg, 4f); return; }
        if (Kafel(K(), BraxTeksty.T("menu.zdjecia"), BraxIkony.Ramka)) { ster.TrybZdjec(); return; }
        if (PokazTest && Kafel(K(), BraxTeksty.T("menu.test"), BraxIkony.Slupki)) ekran = Ekran.Test;
        if (Kafel(K(), BraxTeksty.T("menu.autorzy"), BraxIkony.Info)) { ekran = Ekran.Autorzy; autorzyStrona = 0; }
    }

    void Ustawienia()
    {
        float pad = s * .05f, top = Naglowek(BraxTeksty.T("menu.ustawienia"));
        float x = sa.x + pad, w = sa.width - 2 * pad; if (w > s * 2f) { x += (w - s * 2f) * .5f; w = s * 2f; }
        bool poziom = sa.width > sa.height * 1.2f;
        int wierszy = 8;   // grafika zajmuje dwa; + kamera za plecami
        float rh = Mathf.Min(s * .115f, (sa.yMax - pad * .6f - top) / (poziom ? wierszy : wierszy * 2f)), gap = rh * .14f;
        float lw = poziom ? w * .36f : w, cx = poziom ? x + lw : x, cw = poziom ? w - lw : w;
        float y = top;
        var stL = BraxGui.Styl(Mathf.RoundToInt(s * .03f), true, TextAnchor.MiddleLeft);
        Rect Wiersz(string etykieta, float wys = 1f)
        {
            if (poziom) { BraxGui.Napis(new Rect(x, y, lw, rh - gap), etykieta, stL, BraxGui.Biel); var r = new Rect(cx, y, cw, rh * wys - gap); y += rh * wys; return r; }
            BraxGui.Napis(new Rect(x, y, w, rh * .7f), etykieta, stL, BraxGui.Biel); y += rh * .7f;
            var r2 = new Rect(cx, y, cw, rh * wys - gap); y += rh * wys; return r2;
        }
        float v = Suwak(Wiersz(BraxTeksty.T("ust.muzyka")), BraxUstawienia.Muzyka, 0f, 1f, Procent(BraxUstawienia.Muzyka));
        if (v != BraxUstawienia.Muzyka) BraxUstawienia.Muzyka = v;
        v = Suwak(Wiersz(BraxTeksty.T("ust.efekty")), BraxUstawienia.Efekty, 0f, 1f, Procent(BraxUstawienia.Efekty));
        if (v != BraxUstawienia.Efekty) BraxUstawienia.Efekty = v;
        v = Suwak(Wiersz(BraxTeksty.T("ust.czulosc")), BraxUstawienia.Czulosc, .3f, 2.5f, Czulosc(BraxUstawienia.Czulosc));
        if (v != BraxUstawienia.Czulosc) BraxUstawienia.Czulosc = v;
        int p = Wybor(Wiersz(BraxTeksty.T("ust.przyciski")), BraxUstawienia.Przyciski, BraxTeksty.T("ust.male"), BraxTeksty.T("ust.srednie"), BraxTeksty.T("ust.duze"));
        if (p != BraxUstawienia.Przyciski) BraxUstawienia.Przyciski = p;
        var rg = Wiersz(BraxTeksty.T("ust.grafika"), 1.6f);
        int t = Wybor(new Rect(rg.x, rg.y, rg.width, rh - gap), (int)BraxGrafika.Aktualny, BraxTeksty.T("ust.niska"), BraxTeksty.T("ust.srednia"), BraxTeksty.T("ust.wysoka"));
        if (t != (int)BraxGrafika.Aktualny) { BraxGrafika.Ustaw((BraxGrafika.Tryb)t); ster.ResetStrazy(); }
        BraxGui.Napis(new Rect(rg.x + s * .01f, rg.y + rh - gap * .6f, rg.width, rg.height - rh + gap), NotaGrafiki(), BraxGui.Styl(Mathf.RoundToInt(s * .022f), false, TextAnchor.UpperLeft, true), BraxGui.Szary, false);
        int j = Wybor(Wiersz(BraxTeksty.T("ust.jezyk")), (int)BraxTeksty.Aktualny, BraxTeksty.T("ust.polski"), BraxTeksty.T("ust.english"), null);
        if (j != (int)BraxTeksty.Aktualny) BraxTeksty.Ustaw((BraxTeksty.Jezyk)j);
        int kp = Wybor(Wiersz(BraxTeksty.T("ust.kamera")), BraxUstawienia.KameraZaPlecami ? 1 : 0, BraxTeksty.T("test.wyl"), BraxTeksty.T("test.wl"), null);
        if ((kp == 1) != BraxUstawienia.KameraZaPlecami) BraxUstawienia.KameraZaPlecami = kp == 1;
    }

    void Test(BraxGymPlayer g)
    {
        float pad = s * .05f, top = Naglowek(BraxTeksty.T("menu.test"));
        float x = sa.x + pad, w = sa.width - 2 * pad; if (w > s * 2f) { x += (w - s * 2f) * .5f; w = s * 2f; }
        float rh = Mathf.Min(s * .13f, (sa.yMax - pad * .6f - top) / 4.2f), gap = s * .022f;
        bool poziom = sa.width > sa.height * 1.2f;
        float lw = poziom ? w * .42f : w * .5f;
        BraxGui.Napis(new Rect(x, top, lw, rh * .8f), BraxTeksty.T("test.licznik"), BraxGui.Styl(Mathf.RoundToInt(s * .03f), true, TextAnchor.MiddleLeft, true), BraxGui.Biel);
        int l = Wybor(new Rect(x + lw, top, Mathf.Min(w - lw, s * .6f), rh * .8f), BraxUstawienia.Licznik ? 1 : 0, BraxTeksty.T("test.wyl"), BraxTeksty.T("test.wl"), null);
        BraxUstawienia.Licznik = l == 1;
        float y = top + rh * .8f + gap * .5f;
        BraxGui.Napis(new Rect(x, y, w, rh * .45f), ster.LicznikTekst, BraxGui.Styl(Mathf.RoundToInt(s * .026f), false, TextAnchor.MiddleLeft), BraxGui.Akcent, false);
        y += rh * .5f + gap;
        float kw = (w - gap) * .5f;
        if (Kafel(new Rect(x, y, kw, rh), BraxTeksty.T("test.teleport"), BraxIkony.Pinezka)) { Miejsca(); ekran = Ekran.Teleport; }
        if (Kafel(new Rect(x + kw + gap, y, kw, rh), BraxTeksty.T("test.odnowa"), BraxIkony.Wroc)) { ster.Pauza(false); g.gameObject.SendMessage("Command", "reset", SendMessageOptions.DontRequireReceiver); ster.Komunikat(BraxTeksty.T("msg.reset"), 3f); return; }
        y += rh + gap;
        Kafel(new Rect(x, y, kw, rh), BraxTeksty.T("test.pora"), null, aktywny: false);
        Kafel(new Rect(x + kw + gap, y, kw, rh), BraxTeksty.T("test.pogoda"), null, aktywny: false);
    }

    void Miejsca()
    {
        miejsca = BraxMiejscaTestowe.Lista(ster.Gracz);
        strona = 0;
    }

    void Teleport(BraxGymPlayer g)
    {
        float pad = s * .05f, top = Naglowek(BraxTeksty.T("test.teleport"));
        if (miejsca == null) Miejsca();
        if (miejsca.Count == 0) { BraxGui.Napis(new Rect(sa.x, top, sa.width, s * .1f), BraxTeksty.T("test.brak_miejsc"), BraxGui.Styl(Mathf.RoundToInt(s * .032f), false, TextAnchor.MiddleCenter), BraxGui.Szary); return; }
        bool poziom = sa.width > sa.height * 1.2f;
        int kol = poziom ? 3 : 2;
        float gap = s * .02f, x = sa.x + pad, w = sa.width - 2 * pad; if (w > s * 2.2f) { x += (w - s * 2.2f) * .5f; w = s * 2.2f; }
        float nav = s * .1f, dol = sa.yMax - pad * .5f - nav, kh = s * .12f;
        int wier = Mathf.Max(1, Mathf.FloorToInt((dol - top - gap + gap) / (kh + gap)));
        naStrone = kol * wier; stron = Mathf.Max(1, (miejsca.Count + naStrone - 1) / naStrone); strona = Mathf.Clamp(strona, 0, stron - 1);
        float kw = (w - (kol - 1) * gap) / kol;
        var stK = BraxGui.Styl(Mathf.RoundToInt(s * .02f), true, TextAnchor.UpperLeft);
        var stN = BraxGui.Styl(Mathf.RoundToInt(s * .026f), true, TextAnchor.LowerLeft, true);
        for (int i = 0; i < naStrone; i++)
        {
            int k = strona * naStrone + i; if (k >= miejsca.Count) break;
            var m = miejsca[k];
            var r = new Rect(x + (i % kol) * (kw + gap), top + (i / kol) * (kh + gap), kw, kh);
            bool wc = Wcisniety(r);
            BraxGui.Zaokraglony(r, s * .018f, wc ? new Color(1f, 1f, 1f, .24f) : new Color(1f, 1f, 1f, .085f));
            BraxGui.Napis(new Rect(r.x + s * .02f, r.y + s * .012f, r.width - s * .04f, s * .03f), BraxMiejscaTestowe.NazwaKategorii(m), stK, BraxGui.Akcent, false);
            BraxGui.Napis(new Rect(r.x + s * .02f, r.y + s * .04f, r.width - s * .04f, r.height - s * .05f), m.nazwa, stN, BraxGui.Biel, false);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { ster.Pauza(false); BraxMiejscaTestowe.Przenies(g, m); ster.Komunikat(BraxTeksty.F("msg.teleport", m.nazwa), 3f); return; }
        }
        float bw = Mathf.Min(s * .5f, (w - 2 * gap) / 3f), by = dol + gap * .5f;
        if (Kafel(new Rect(x, by, bw, nav - gap * .5f), BraxTeksty.T("menu.poprzednia"), null, aktywny: strona > 0)) strona--;
        BraxGui.Napis(new Rect(x + bw, by, w - 2 * bw, nav - gap * .5f), StronaTekst(), BraxGui.Styl(Mathf.RoundToInt(s * .026f), false, TextAnchor.MiddleCenter), BraxGui.Szary, false);
        if (Kafel(new Rect(x + w - bw, by, bw, nav - gap * .5f), BraxTeksty.T("menu.dalej"), null, aktywny: strona < stron - 1)) strona++;
    }

    // Mapa: obraz rejonu dopasowany do ekranu, podpisy miejsc, strzałka gracza (pozycja + kierunek). Północ (+z) u góry.
    void Mapa(BraxGymPlayer g)
    {
        var m = mapa; if (!m || !m.obraz) { ekran = Ekran.Glowne; return; }
        float bh = s * .1f, top = sa.y + s * .03f, availH = sa.height - s * .03f - bh * 1.5f, availW = sa.width - s * .08f;
        float asp = (float)m.obraz.width / m.obraz.height, mh = Mathf.Min(availH, availW / asp), mw = mh * asp;
        var r = new Rect(sa.x + (sa.width - mw) * .5f, top, mw, mh);
        if (Event.current.type == EventType.Repaint)
        {
            GUI.DrawTexture(r, m.obraz, ScaleMode.StretchToFill);
            var stM = BraxGui.Styl(Mathf.RoundToInt(s * .026f), true, TextAnchor.MiddleCenter);
            foreach (var mi in m.miejsca)
            {
                if (mi == null) continue;
                var e = new Vector2(r.x + (mi.xz.x - m.swiat.x) / m.swiat.width * r.width, r.y + (1f - (mi.xz.y - m.swiat.y) / m.swiat.height) * r.height);
                float sw = BraxGui.Szerokosc(mi.nazwa, stM), sh = stM.fontSize * 1.3f;
                var lr = new Rect(e.x - sw * .5f - 4, e.y - sh * .5f, sw + 8, sh);
                BraxGui.Prostokat(lr, new Color(0, 0, 0, .55f));
                BraxGui.Napis(lr, mi.nazwa, stM, Color.white, false);
            }
            var uv = m.NaObraz(g.transform.position);
            var pe = new Vector2(r.x + uv.x * r.width, r.y + (1f - uv.y) * r.height);
            float ds = s * .035f;
            var mat = GUI.matrix;
            GUIUtility.RotateAroundPivot(g.transform.eulerAngles.y, pe);
            BraxGui.Prostokat(new Rect(pe.x - ds * .25f, pe.y - ds * 1.1f, ds * .5f, ds * .9f), BraxGui.Akcent);
            GUI.matrix = mat;
            BraxGui.Kolo(pe, ds * .5f, BraxIkony.Kolo, BraxGui.Akcent);
            BraxGui.Napis(new Rect(r.x, r.y + s * .01f, r.width, s * .05f), mapaTytul, BraxGui.Styl(Mathf.RoundToInt(s * .028f), true, TextAnchor.MiddleCenter), BraxGui.Biel);
        }
        if (Kafel(new Rect(sa.x + (sa.width - s * .6f) * .5f, sa.yMax - bh * 1.2f, s * .6f, bh), BraxTeksty.T("menu.zamknij"), null)) ekran = Ekran.Glowne;
    }

    // Autorzy: tekst z Resources/braxhorn_autorzy.txt (unity-tools/claude/autorzy.py zbiera CREDITS.md i metadane modeli
    // w każdym buildzie). Strony zamiast przewijania (IMGUI nie przewija palcem); podział na strony liczony raz na rozmiar ekranu.
    void Autorzy()
    {
        if (autorzy == null)
        {
            var ta = Resources.Load<TextAsset>("braxhorn_autorzy");
            autorzy = (ta ? ta.text : BraxTeksty.T("menu.autorzy_brak")).Replace("\r", "").Split('\n');
        }
        var st = BraxGui.Styl(Mathf.RoundToInt(s * .026f), false, TextAnchor.UpperLeft, true);
        float w = Mathf.Min(sa.width - s * .1f, s * 1.9f), x = sa.x + (sa.width - w) * .5f, top = sa.y + s * .05f, h = s * .1f, g = s * .025f;
        float areaH = sa.height - s * .05f - h * 1.9f;
        if (autorzyS != s || autorzyWersja != BraxTeksty.Wersja)
        {
            autorzyS = s; autorzyWersja = BraxTeksty.Wersja; autorzyStrony.Clear();
            int start = 0;
            while (start < autorzy.Length)
            {
                float used = 0; int end = start;
                while (end < autorzy.Length)
                {
                    float lh = BraxGui.Wysokosc(autorzy[end].Length == 0 ? " " : autorzy[end], st, w);
                    if (used + lh > areaH && end > start) break;
                    used += lh; end++;
                }
                autorzyStrony.Add(string.Join("\n", autorzy, start, end - start));
                start = end;
            }
            if (autorzyStrony.Count == 0) autorzyStrony.Add("");
        }
        autorzyStrona = Mathf.Clamp(autorzyStrona, 0, autorzyStrony.Count - 1);
        BraxGui.Napis(new Rect(x, top, w, areaH), autorzyStrony[autorzyStrona], st, new Color(.92f, .92f, .9f, 1f), false);
        float y = sa.yMax - h * 1.3f, bw = (w - 2 * g) / 3f;
        if (Kafel(new Rect(x, y, bw, h), BraxTeksty.T("menu.wstecz"), null, aktywny: autorzyStrona > 0)) autorzyStrona--;
        if (Kafel(new Rect(x + bw + g, y, bw, h), BraxTeksty.T("menu.dalej"), null, aktywny: autorzyStrona < autorzyStrony.Count - 1)) autorzyStrona++;
        if (Kafel(new Rect(x + 2 * (bw + g), y, bw, h), BraxTeksty.T("menu.zamknij"), null)) ekran = Ekran.Glowne;
    }

    // ---------------- kontrolki ----------------
    // nagłówek podekranu: przycisk „‹” (wstecz) i tytuł; zwraca y pod nagłówkiem
    float Naglowek(string tytul)
    {
        float pad = s * .05f, top = sa.y + s * .035f, h = s * .09f;
        var rb = new Rect(sa.x + pad, top, h * 2.4f, h);
        if (Kafel(rb, BraxTeksty.T("menu.wstecz"), null)) Wstecz();
        BraxGui.Napis(new Rect(sa.x, top, sa.width, h), tytul, BraxGui.Styl(Mathf.RoundToInt(s * .05f), true, TextAnchor.MiddleCenter), BraxGui.Biel);
        return top + h + s * .035f;
    }

    bool Wcisniety(Rect r) => Event.current.type == EventType.Repaint && Input.GetMouseButton(0) && r.Contains(Event.current.mousePosition);

    // kafel: zaokrąglone szkło, ikona nad napisem (wysoki kafel) albo obok (niski), cały obszar klikalny
    bool Kafel(Rect r, string tekst, Texture ikona, bool glowny = false, bool aktywny = true)
    {
        bool wc = aktywny && Wcisniety(r);
        float pr = s * .022f;
        Color tlo = glowny ? (wc ? new Color(.55f, 1f, .2f, .95f) : new Color(.36f, .7f, .1f, .9f)) : aktywny ? (wc ? new Color(1f, 1f, 1f, .26f) : new Color(1f, 1f, 1f, .1f)) : new Color(1f, 1f, 1f, .04f);
        BraxGui.Zaokraglony(r, pr, tlo);
        if (!glowny) BraxGui.ObrysZaokraglony(r, pr, new Color(1f, 1f, 1f, aktywny ? .16f : .07f));
        Color tk = glowny ? new Color(.04f, .07f, .03f, 1f) : aktywny ? BraxGui.Biel : new Color(1f, 1f, 1f, .38f);
        int fs = Mathf.RoundToInt(Mathf.Min(s * .034f, r.height * .3f));
        if (ikona && r.height > s * .15f)
        {
            float ik = Mathf.Min(r.height * .34f, r.width * .3f);
            BraxGui.Tekstura(new Rect(r.center.x - ik * .5f, r.y + r.height * .44f - ik * .82f, ik, ik), ikona, tk);
            BraxGui.Napis(new Rect(r.x + s * .01f, r.y + r.height * .56f, r.width - s * .02f, r.height * .36f), tekst, BraxGui.Styl(fs, true, TextAnchor.MiddleCenter, true), tk, !glowny);
        }
        else if (ikona)
        {
            float ik = r.height * .46f;
            BraxGui.Tekstura(new Rect(r.x + r.height * .3f, r.center.y - ik * .5f, ik, ik), ikona, tk);
            BraxGui.Napis(new Rect(r.x + r.height * .3f + ik + s * .015f, r.y, r.width - ik - r.height * .45f, r.height), tekst, BraxGui.Styl(fs, true, TextAnchor.MiddleLeft, true), tk, !glowny);
        }
        else BraxGui.Napis(new Rect(r.x + s * .01f, r.y, r.width - s * .02f, r.height), tekst, BraxGui.Styl(fs, true, TextAnchor.MiddleCenter, true), tk, !glowny);
        bool klik = GUI.Button(r, GUIContent.none, GUIStyle.none);   // zawsze (stałe numery kontrolek IMGUI), nieaktywny tylko nie reaguje
        return aktywny && klik;
    }

    // wybór 2–3 opcji (segmenty); zwraca wybrany indeks
    int Wybor(Rect r, int wartosc, string a, string b, string c)
    {
        int n = c == null ? 2 : 3;
        float g = s * .012f, sw = (r.width - (n - 1) * g) / n;
        int wynik = wartosc;
        for (int i = 0; i < n; i++)
        {
            var ri = new Rect(r.x + i * (sw + g), r.y, sw, r.height);
            bool on = wartosc == i, wc = Wcisniety(ri);
            BraxGui.Zaokraglony(ri, s * .018f, on ? new Color(.36f, .7f, .1f, .92f) : wc ? new Color(1f, 1f, 1f, .24f) : new Color(1f, 1f, 1f, .09f));
            if (!on) BraxGui.ObrysZaokraglony(ri, s * .018f, new Color(1f, 1f, 1f, .14f));
            BraxGui.Napis(ri, i == 0 ? a : i == 1 ? b : c, BraxGui.Styl(Mathf.RoundToInt(Mathf.Min(s * .03f, r.height * .36f)), true, TextAnchor.MiddleCenter), on ? new Color(.04f, .07f, .03f, 1f) : BraxGui.Biel, !on);
            if (GUI.Button(ri, GUIContent.none, GUIStyle.none)) wynik = i;
        }
        return wynik;
    }

    // suwak pod kciuk: tor, wypełnienie akcentem, gałka, wartość po prawej; dotknięcie lub przeciągnięcie w dowolnym miejscu toru
    float Suwak(Rect r, float v, float min, float max, string wartosc)
    {
        float vw = s * .17f;
        var tor = new Rect(r.x + s * .02f, r.y, r.width - vw - s * .04f, r.height);
        int id = GUIUtility.GetControlID(FocusType.Passive);
        var e = Event.current;
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (tor.Contains(e.mousePosition)) { GUIUtility.hotControl = id; v = Wartosc(tor, e.mousePosition.x, min, max); e.Use(); }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id) { v = Wartosc(tor, e.mousePosition.x, min, max); e.Use(); }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id) { GUIUtility.hotControl = 0; e.Use(); }
                break;
            case EventType.Repaint:
                float th = Mathf.Max(4f, r.height * .12f), ty = r.center.y - th * .5f, t = Mathf.InverseLerp(min, max, v);
                BraxGui.Pigulka(new Rect(tor.x, ty, tor.width, th), new Color(1f, 1f, 1f, .16f));
                if (t > .001f) BraxGui.Pigulka(new Rect(tor.x, ty, Mathf.Max(th, tor.width * t), th), BraxGui.Akcent);
                float kr = Mathf.Min(r.height * .3f, s * .03f);
                var kc = new Vector2(tor.x + tor.width * t, r.center.y);
                BraxGui.Kolo(kc, kr * 1.25f, BraxIkony.Cien, new Color(0, 0, 0, .35f));
                BraxGui.Kolo(kc, kr, BraxIkony.Kolo, GUIUtility.hotControl == id ? BraxGui.Akcent : Color.white);
                BraxGui.Napis(new Rect(r.xMax - vw, r.y, vw, r.height), wartosc, BraxGui.Styl(Mathf.RoundToInt(s * .03f), true, TextAnchor.MiddleRight), BraxGui.Biel);
                break;
        }
        return v;
    }
    static float Wartosc(Rect tor, float x, float min, float max) => Mathf.Lerp(min, max, Mathf.Clamp01((x - tor.x) / Mathf.Max(1f, tor.width)));

    // ---------------- napisy z pamięci podręcznej ----------------
    static string Procent(float v) { int p = Mathf.Clamp(Mathf.RoundToInt(v * 100f), 0, 100); return procenty[p] ?? (procenty[p] = p + " %"); }
    static string Czulosc(float v)
    {
        int k = Mathf.Clamp(Mathf.RoundToInt(v * 10f), 0, 25);
        return czulosci[k] ?? (czulosci[k] = (k / 10f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "×");
    }
    string StronaTekst()
    {
        int klucz = strona * 1000 + stron * 10 + BraxTeksty.Wersja % 10;
        if (klucz != stronaKlucz) { stronaKlucz = klucz; stronaTekst = BraxTeksty.F("menu.strona", strona + 1, stron); }
        return stronaTekst;
    }
    string NotaGrafiki()
    {
        int klucz = (int)BraxGrafika.Aktualny * 100 + (BraxGrafika.Reczny ? 10 : 0) + BraxTeksty.Wersja % 10;
        if (klucz != notaKlucz)
        {
            notaKlucz = klucz;
            string auto = BraxTeksty.T(BraxProfilTelefonu.Domyslny() == 0 ? "ust.niska" : BraxProfilTelefonu.Domyslny() == 2 ? "ust.wysoka" : "ust.srednia");
            notaGrafiki = BraxGrafika.Reczny ? BraxTeksty.F("ust.reczny", auto) : BraxTeksty.F("ust.auto", auto, BraxProfilTelefonu.Powod);
        }
        return notaGrafiki;
    }
}
