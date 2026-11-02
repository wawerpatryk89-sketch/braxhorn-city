using System;
using UnityEngine;

// IKONY INTERFEJSU RYSOWANE KODEM (build 14; właściciel 20:49: „przyciski za duże i brzydkie → nowocześniejsze, półprzezroczyste
// koła z ikonami: pięść = CIOS, but = KOP, strzałka w górę = SKOK, dłoń = AKCJA; MENU jako mała ikona”).
// Każda ikona = biały kształt z odległości od konturu (SDF) w tekselach 128 × 128 z wygładzoną krawędzią i mipmapami —
// bez cudzych grafik i licencji. Tworzone raz, przy pierwszym użyciu (~15 ms na telefonie), potem tylko rysowane.
// Układ współrzędnych kształtu: −1…1, y w górę (środek ikony = 0,0).
public static class BraxIkony
{
    const int N = 128;
    static Texture2D piesc, but, skok, dlon, menu, wroc, zebatka, aparat, ramka, pinezka, slupki, mapa, info, kolo, pierscien, cien, pierscienCienki;

    public static Texture2D Piesc => piesc ? piesc : piesc = Rysuj("ikona pięść", SdfPiesc);
    public static Texture2D But => but ? but : but = Rysuj("ikona but", SdfBut);
    public static Texture2D Skok => skok ? skok : skok = Rysuj("ikona skok", SdfSkok);
    public static Texture2D Dlon => dlon ? dlon : dlon = Rysuj("ikona dłoń", SdfDlon);
    public static Texture2D Menu => menu ? menu : menu = Rysuj("ikona menu", SdfMenu);
    public static Texture2D Wroc => wroc ? wroc : wroc = Rysuj("ikona wróć", SdfWroc);
    public static Texture2D Zebatka => zebatka ? zebatka : zebatka = Rysuj("ikona ustawienia", SdfZebatka);
    public static Texture2D Aparat => aparat ? aparat : aparat = Rysuj("ikona zgłoś", SdfAparat);
    public static Texture2D Ramka => ramka ? ramka : ramka = Rysuj("ikona tryb zdjęć", SdfRamka);
    public static Texture2D Pinezka => pinezka ? pinezka : pinezka = Rysuj("ikona utknąłem", SdfPinezka);
    public static Texture2D Slupki => slupki ? slupki : slupki = Rysuj("ikona test", SdfSlupki);
    public static Texture2D Mapa => mapa ? mapa : mapa = Rysuj("ikona mapa", SdfMapa);
    public static Texture2D Info => info ? info : info = Rysuj("ikona autorzy", SdfInfo);
    public static Texture2D Kolo => kolo ? kolo : kolo = Rysuj("koło", p => p.magnitude - .985f);
    public static Texture2D Pierscien => pierscien ? pierscien : pierscien = Rysuj("pierścień", p => Mathf.Abs(p.magnitude - .955f) - .03f);
    public static Texture2D PierscienCienki => pierscienCienki ? pierscienCienki : pierscienCienki = Rysuj("pierścień cienki", p => Mathf.Abs(p.magnitude - .97f) - .016f);
    // miękki cień pod przyciskiem: krążek z rozmytą krawędzią (od 0.62 do 1.0 promienia)
    public static Texture2D Cien => cien ? cien : cien = Rysuj("cień", p => 0f, p => { float d = p.magnitude; return Mathf.Clamp01((1f - d) / .38f) * Mathf.Clamp01((1f - d) / .38f); });

    // ---- rasteryzacja ----
    static Texture2D Rysuj(string nazwa, Func<Vector2, float> sdf) => Rysuj(nazwa, sdf, null);
    static Texture2D Rysuj(string nazwa, Func<Vector2, float> sdf, Func<Vector2, float> alfa)
    {
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = nazwa, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 0 };
        var px = new Color32[N * N];
        float piks = 2f / N;   // jeden teksel w jednostkach kształtu
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                var p = new Vector2((x + .5f) / N * 2f - 1f, (y + .5f) / N * 2f - 1f);
                float a = alfa != null ? alfa(p) : Mathf.Clamp01(.5f - sdf(p) / piks);
                px[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        t.SetPixels32(px); t.Apply(true, true);
        return t;
    }

    // ---- prymitywy SDF ----
    static float Prost(Vector2 p, Vector2 c, Vector2 pol, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x - c.x) - pol.x + r, Mathf.Abs(p.y - c.y) - pol.y + r);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }
    static float Kapsula(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }
    static float Okrag(Vector2 p, Vector2 c, float r) => (p - c).magnitude - r;
    static float Trojkat(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 e0 = b - a, e1 = c - b, e2 = a - c, v0 = p - a, v1 = p - b, v2 = p - c;
        Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
        Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
        Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
        float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
        float dx = Mathf.Min(Mathf.Min(pq0.sqrMagnitude, pq1.sqrMagnitude), pq2.sqrMagnitude);
        float dy = Mathf.Min(Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x)), s * (v2.x * e2.y - v2.y * e2.x));
        return -Mathf.Sqrt(dx) * Mathf.Sign(dy);
    }
    static float Suma(float a, float b) => Mathf.Min(a, b);
    static float Bez(float a, float b) => Mathf.Max(a, -b);          // a minus b
    static float Kontur(float d, float grubosc) => Mathf.Abs(d) - grubosc;   // obrys kształtu (do wycinania linii)

    // ---- ikony ----
    // Pięść (CIOS): cztery kłykcie palców u góry, korpus dłoni, kciuk w poprzek z wyciętym obrysem, nadgarstek.
    static float SdfPiesc(Vector2 p)
    {
        p *= 1.12f;
        float d = Prost(p, new Vector2(.02f, -.08f), new Vector2(.56f, .40f), .2f);
        for (int i = 0; i < 4; i++) d = Suma(d, Kapsula(p, new Vector2(-.42f + i * .28f, .16f), new Vector2(-.42f + i * .28f, .42f), .145f));
        for (int i = 1; i < 4; i++) d = Bez(d, Prost(p, new Vector2(-.56f + i * .28f, .38f), new Vector2(.022f, .2f), .01f));   // szczeliny między palcami
        float kciuk = Kapsula(p, new Vector2(-.58f, -.02f), new Vector2(.16f, -.1f), .14f);
        d = Bez(Suma(d, kciuk), Kontur(kciuk, .035f));
        d = Suma(d, Prost(p, new Vector2(.05f, -.74f), new Vector2(.36f, .17f), .07f));   // nadgarstek
        d = Bez(d, Prost(p, new Vector2(.05f, -.53f), new Vector2(.5f, .035f), .0f));     // przerwa nadgarstek–dłoń
        return d / 1.12f;
    }
    // But (KOP): cholewka, stopa z zaokrąglonym czubkiem, wycięta linia podeszwy i dwie sznurówki.
    static float SdfBut(Vector2 p)
    {
        float d = Prost(p, new Vector2(-.24f, .22f), new Vector2(.3f, .56f), .1f);
        d = Suma(d, Prost(p, new Vector2(.1f, -.42f), new Vector2(.66f, .25f), .22f));
        d = Bez(d, Prost(p, new Vector2(.1f, -.53f), new Vector2(.7f, .03f), 0f));        // podeszwa
        d = Bez(d, Kapsula(p, new Vector2(-.42f, .32f), new Vector2(-.08f, .38f), .028f));  // sznurówki
        d = Bez(d, Kapsula(p, new Vector2(-.42f, .1f), new Vector2(-.08f, .16f), .028f));
        return d;
    }
    // Skok: strzałka w górę + kreska ziemi.
    static float SdfSkok(Vector2 p)
    {
        float d = Trojkat(p, new Vector2(0f, .82f), new Vector2(-.6f, .12f), new Vector2(.6f, .12f)) - .05f;
        d = Suma(d, Prost(p, new Vector2(0f, -.22f), new Vector2(.17f, .4f), .05f));
        d = Suma(d, Prost(p, new Vector2(0f, -.8f), new Vector2(.62f, .065f), .065f));
        return d;
    }
    // Dłoń (AKCJA): wnętrze dłoni, cztery palce, odchylony kciuk.
    static float SdfDlon(Vector2 p)
    {
        float d = Prost(p, new Vector2(.04f, -.32f), new Vector2(.4f, .4f), .2f);
        float[] x = { -.27f, -.05f, .17f, .38f }, y = { .42f, .72f, .74f, .52f };
        for (int i = 0; i < 4; i++) d = Suma(d, Kapsula(p, new Vector2(x[i], -.15f), new Vector2(x[i], y[i]), .095f));
        d = Suma(d, Kapsula(p, new Vector2(-.3f, -.42f), new Vector2(-.72f, .02f), .1f));
        return d;
    }
    static float SdfMenu(Vector2 p)
    {
        float d = 9f;
        for (int i = -1; i <= 1; i++) d = Suma(d, Prost(p, new Vector2(0f, i * .42f), new Vector2(.62f, .085f), .085f));
        return d;
    }
    static float SdfWroc(Vector2 p) => Trojkat(p, new Vector2(.62f, 0f), new Vector2(-.42f, .62f), new Vector2(-.42f, -.62f)) - .06f;
    static float SdfZebatka(Vector2 p)
    {
        float a = Mathf.Atan2(p.y, p.x), r = p.magnitude;
        float zab = Mathf.Clamp(Mathf.Cos(a * 8f) * 2.2f, -1f, 1f);       // 8 zębów o prawie płaskich bokach
        float d = r - (.6f + .14f * zab);
        return Bez(d, Okrag(p, Vector2.zero, .24f));
    }
    static float SdfAparat(Vector2 p)
    {
        float d = Prost(p, new Vector2(0f, -.08f), new Vector2(.8f, .52f), .14f);
        d = Suma(d, Prost(p, new Vector2(-.18f, .5f), new Vector2(.28f, .12f), .06f));
        d = Bez(d, Kontur(Okrag(p, new Vector2(0f, -.08f), .3f), .07f));
        return d;
    }
    static float SdfRamka(Vector2 p)
    {
        // cztery narożniki kadru + mała kropka ostrości
        float d = 9f;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                var c = new Vector2(sx * .62f, sy * .5f);
                d = Suma(d, Kapsula(p, c, c - new Vector2(sx * .32f, 0f), .07f));
                d = Suma(d, Kapsula(p, c, c - new Vector2(0f, sy * .3f), .07f));
            }
        return Suma(d, Okrag(p, Vector2.zero, .12f));
    }
    static float SdfPinezka(Vector2 p)
    {
        float d = Suma(Okrag(p, new Vector2(0f, .22f), .5f), Trojkat(p, new Vector2(0f, -.86f), new Vector2(-.43f, -.05f), new Vector2(.43f, -.05f)));
        return Bez(d, Okrag(p, new Vector2(0f, .22f), .2f));
    }
    static float SdfSlupki(Vector2 p)
    {
        float d = Prost(p, new Vector2(-.5f, -.38f), new Vector2(.15f, .32f), .05f);
        d = Suma(d, Prost(p, new Vector2(0f, -.16f), new Vector2(.15f, .54f), .05f));
        d = Suma(d, Prost(p, new Vector2(.5f, .06f), new Vector2(.15f, .76f), .05f));
        return d;
    }
    static float SdfMapa(Vector2 p)
    {
        // złożona mapa: trzy pasy pochylone na zmianę
        float d = 9f;
        for (int i = 0; i < 3; i++)
        {
            float cx = -.5f + i * .5f, dy = i == 1 ? -.08f : .08f;
            d = Suma(d, Prost(p, new Vector2(cx, dy), new Vector2(.22f, .66f), .04f));
        }
        d = Bez(d, Prost(p, new Vector2(-.25f, 0f), new Vector2(.02f, .9f), 0f));
        d = Bez(d, Prost(p, new Vector2(.25f, 0f), new Vector2(.02f, .9f), 0f));
        return d;
    }
    static float SdfInfo(Vector2 p)
    {
        float d = Kontur(Okrag(p, Vector2.zero, .8f), .085f);
        d = Suma(d, Okrag(p, new Vector2(0f, .36f), .11f));
        d = Suma(d, Prost(p, new Vector2(0f, -.18f), new Vector2(.09f, .32f), .04f));
        return d;
    }
}
