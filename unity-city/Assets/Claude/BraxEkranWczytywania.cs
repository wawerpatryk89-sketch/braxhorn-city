using UnityEngine;

// EKRAN WCZYTYWANIA (build 14; właściciel 21:45 „usunąć ekran TEST MIASTA” + propozycja „ładny ekran wczytywania: logo, pasek,
// diagnostyka ukryta”). Rysowanie wspólne dla sceny startowej APK (BraxStart) i podglądu do zdjęć CI w przeglądarce
// (BraxTouchControls.Podglad("wczytywanie")). Ciemne tło z delikatnym gradientem i winietą, logo BRAXHORN CITY,
// nazwa rejonu, cienki pasek postępu z procentami, wskazówka u dołu. Diagnostyka (dziennik poprzedniego startu, dane
// telefonu) tylko na żądanie: przytrzymanie palca 3 s albo automatycznie, gdy poprzedni start się nie udał.
public static class BraxEkranWczytywania
{
    static Texture2D tlo;

    public static void Rysuj(float postep01, string status, string podtytul, string wskazowka, string diag, bool pokazDiag, bool podpowiedzDiag)
    {
        if (Event.current.type != EventType.Repaint) return;
        float W = Screen.width, H = Screen.height, s = Mathf.Min(W, H);
        var sa = BraxGui.BezpiecznyProstokat();
        BraxGui.Prostokat(new Rect(0, 0, W, H), new Color(.035f, .045f, .04f, 1f));
        BraxGui.Tekstura(new Rect(0, 0, W, H), Tlo(), Color.white);

        bool poziom = W >= H;
        float srodekY = sa.y + sa.height * (pokazDiag ? .2f : poziom ? .36f : .38f);
        // logo: BRAXHORN (biel, gruby) / linia akcentu / C I T Y (zielony)
        var stLogo = BraxGui.Styl(Mathf.RoundToInt(s * (pokazDiag ? .085f : .13f)), true, TextAnchor.MiddleCenter);
        float hLogo = s * (pokazDiag ? .1f : .15f);
        BraxGui.Napis(new Rect(0, srodekY - hLogo * .62f, W, hLogo), "BRAXHORN", stLogo, BraxGui.Biel);
        float lw = Mathf.Min(BraxGui.Szerokosc("BRAXHORN", stLogo), W * .8f);
        BraxGui.Prostokat(new Rect((W - lw) * .5f, srodekY + hLogo * .42f, lw, Mathf.Max(2f, s * .004f)), new Color(BraxGui.Akcent.r, BraxGui.Akcent.g, BraxGui.Akcent.b, .85f));
        var stCity = BraxGui.Styl(Mathf.RoundToInt(s * (pokazDiag ? .032f : .045f)), true, TextAnchor.MiddleCenter);
        BraxGui.Napis(new Rect(0, srodekY + hLogo * .48f, W, s * .07f), "C  I  T  Y", stCity, BraxGui.Akcent, false);
        float y = srodekY + hLogo * .48f + s * .07f;
        if (!string.IsNullOrEmpty(podtytul))
        {
            BraxGui.Napis(new Rect(0, y, W, s * .05f), podtytul, BraxGui.Styl(Mathf.RoundToInt(s * .027f), false, TextAnchor.MiddleCenter), BraxGui.Szary, false);
            y += s * .05f;
        }

        // pasek postępu
        float bw = Mathf.Min(sa.width * .82f, s * 1.3f), bx = (W - bw) * .5f, bh = Mathf.Max(4f, s * .011f);
        float by = pokazDiag ? y + s * .07f : sa.y + sa.height * (poziom ? .72f : .7f);
        var stMaly = BraxGui.Styl(Mathf.RoundToInt(s * .026f), true, TextAnchor.LowerLeft, true);
        float hStatus = BraxGui.Wysokosc(status, stMaly, bw * .8f);
        BraxGui.Napis(new Rect(bx, by - s * .012f - hStatus, bw * .8f, hStatus), status, stMaly, new Color(.86f, .87f, .84f, 1f), false);
        int proc = Mathf.RoundToInt(Mathf.Clamp01(postep01) * 100f);
        BraxGui.Napis(new Rect(bx + bw * .5f, by - s * .05f, bw * .5f, s * .038f), Procent(proc), BraxGui.Styl(Mathf.RoundToInt(s * .028f), true, TextAnchor.LowerRight), BraxGui.Akcent, false);
        BraxGui.Pigulka(new Rect(bx, by, bw, bh), new Color(1f, 1f, 1f, .12f));
        float fw = bw * Mathf.Clamp01(postep01);
        if (fw > bh) BraxGui.Pigulka(new Rect(bx, by, fw, bh), BraxGui.Akcent);

        // wskazówka
        if (!pokazDiag && !string.IsNullOrEmpty(wskazowka))
        {
            float tw = Mathf.Min(sa.width * .86f, s * 1.5f), tx = (W - tw) * .5f, ty = by + bh + s * .07f;
            BraxGui.Napis(new Rect(tx, ty, tw, s * .035f), BraxTeksty.T("start.wskazowka"), BraxGui.Styl(Mathf.RoundToInt(s * .022f), true, TextAnchor.MiddleCenter), new Color(BraxGui.Akcent.r, BraxGui.Akcent.g, BraxGui.Akcent.b, .85f), false);
            var stW = BraxGui.Styl(Mathf.RoundToInt(s * .028f), false, TextAnchor.UpperCenter, true);
            BraxGui.Napis(new Rect(tx, ty + s * .04f, tw, BraxGui.Wysokosc(wskazowka, stW, tw)), wskazowka, stW, new Color(.8f, .81f, .78f, 1f), false);
        }

        // diagnostyka
        if (pokazDiag)
        {
            float px = sa.x + s * .04f, pw = sa.width - s * .08f, py = by + bh + s * .045f, ph = sa.yMax - py - s * .03f;
            BraxGui.Zaokraglony(new Rect(px, py, pw, ph), s * .02f, new Color(0f, 0f, 0f, .55f));
            BraxGui.Napis(new Rect(px + s * .025f, py + s * .015f, pw, s * .04f), BraxTeksty.T("start.diag_tytul"), BraxGui.Styl(Mathf.RoundToInt(s * .024f), true, TextAnchor.MiddleLeft), BraxGui.Akcent, false);
            var stD = BraxGui.Styl(Mathf.Max(11, Mathf.RoundToInt(s * .021f)), false, TextAnchor.UpperLeft, true);
            BraxGui.Napis(new Rect(px + s * .025f, py + s * .06f, pw - s * .05f, ph - s * .07f), diag, stD, new Color(.92f, .9f, .86f, 1f), false);
        }
        else if (podpowiedzDiag)
            BraxGui.Napis(new Rect(sa.x, sa.yMax - s * .045f, sa.width - s * .03f, s * .04f), BraxTeksty.T("start.diag_podpowiedz"), BraxGui.Styl(Mathf.RoundToInt(s * .018f), false, TextAnchor.MiddleRight), new Color(1f, 1f, 1f, .22f), false);
    }

    static readonly string[] procenty = new string[101];
    static string Procent(int p) { p = Mathf.Clamp(p, 0, 100); return procenty[p] ?? (procenty[p] = p + " %"); }

    // tło: pionowy gradient grafitowy z lekką zielenią u dołu i winietą (64 × 64, rozciągnięte z filtrowaniem)
    static Texture2D Tlo()
    {
        if (tlo) return tlo;
        const int n = 64;
        tlo = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "tło ekranu wczytywania" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + .5f) / n, v = (y + .5f) / n;   // v = 0 dół
                float wn = Mathf.Clamp01(1f - ((u - .5f) * (u - .5f) * 1.6f + (v - .55f) * (v - .55f) * 2.2f));
                float zielen = Mathf.Clamp01(1f - v * 2.2f) * .5f;
                var c = new Color(.05f + .05f * wn, .06f + .055f * wn + .035f * zielen, .055f + .045f * wn, 1f);
                px[y * n + x] = c;
            }
        tlo.SetPixels32(px); tlo.Apply(false, true);
        return tlo;
    }
}
