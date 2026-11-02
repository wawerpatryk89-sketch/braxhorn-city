using System.Collections.Generic;
using UnityEngine;

// WSPÓLNE RYSOWANIE INTERFEJSU (build 14): kolory, zaokrąglone prostokąty (9-slice z tekstury liczonej kodem), koła z ikoną,
// napisy z cieniem, style czcionek. Projekt nie ma pakietu uGUI — całość na IMGUI (OnGUI), jak dotychczasowe ekrany.
// Bez alokacji co klatkę: tekstury i style tworzone raz (pamięć podręczna wg rozmiaru), napisy z gotowych stringów.
// Współrzędne: IMGUI (y w dół). Strefa bezpieczna (wycięcie aparatu, zaokrąglone rogi): BezpiecznyProstokat().
public static class BraxGui
{
    // paleta: szkło grafitowe, biel z cieniem, akcent zielony BRAXHORN (jak strona i marka), czerwień trafienia
    public static readonly Color Szklo = new Color(.05f, .06f, .06f, .38f);
    public static readonly Color SzkloMocne = new Color(.06f, .07f, .07f, .78f);
    public static readonly Color Panel = new Color(.09f, .1f, .1f, .92f);
    public static readonly Color Obrys = new Color(1f, 1f, 1f, .55f);
    public static readonly Color Biel = new Color(1f, 1f, 1f, .94f);
    public static readonly Color Szary = new Color(.72f, .74f, .72f, .95f);
    public static readonly Color Ciemny = new Color(0f, 0f, 0f, .55f);
    public static readonly Color Akcent = new Color(.55f, 1f, .2f, 1f);
    public static readonly Color AkcentCiemny = new Color(.22f, .42f, .06f, .9f);
    public static readonly Color Trafienie = new Color(1f, .38f, .26f, .9f);

    public static Rect BezpiecznyProstokat()
    {
        var sa = Screen.safeArea;   // piksele, y od dołu
        if (sa.width < 10f || sa.height < 10f) sa = new Rect(0, 0, Screen.width, Screen.height);
        return new Rect(sa.x, Screen.height - sa.yMax, sa.width, sa.height);
    }

    public static void Prostokat(Rect r, Color c)
    {
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
    }

    public static void Tekstura(Rect r, Texture t, Color c)
    {
        if (!t) return;
        var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true); GUI.color = old;
    }

    // koło o środku c (IMGUI) i promieniu r
    public static void Kolo(Vector2 c, float r, Texture t, Color k) => Tekstura(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), t, k);

    // ---- zaokrąglone prostokąty (9-slice) ----
    static readonly Dictionary<int, GUIStyle> zaokr = new Dictionary<int, GUIStyle>();
    public static void Zaokraglony(Rect r, float promien, Color c)
    {
        int p = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(promien, Mathf.Min(r.width, r.height) * .5f)), 2, 96);
        if (!zaokr.TryGetValue(p, out var st)) zaokr[p] = st = StylZaokraglony(p, false);
        Rysuj(r, st, c);
    }
    static readonly Dictionary<int, GUIStyle> obrysy = new Dictionary<int, GUIStyle>();
    public static void ObrysZaokraglony(Rect r, float promien, Color c)
    {
        int p = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(promien, Mathf.Min(r.width, r.height) * .5f)), 2, 96);
        if (!obrysy.TryGetValue(p, out var st)) obrysy[p] = st = StylZaokraglony(p, true);
        Rysuj(r, st, c);
    }
    public static void Pigulka(Rect r, Color c) => Zaokraglony(r, r.height * .5f, c);

    static void Rysuj(Rect r, GUIStyle st, Color c)
    {
        if (Event.current.type != EventType.Repaint) return;
        var old = GUI.color; var oldB = GUI.backgroundColor;
        GUI.color = c; GUI.backgroundColor = Color.white;
        st.Draw(r, false, false, false, false);
        GUI.color = old; GUI.backgroundColor = oldB;
    }

    static GUIStyle StylZaokraglony(int p, bool obrys)
    {
        int n = p * 2 + 2;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = (obrys ? "obrys " : "zaokr ") + p };
        var px = new Color32[n * n];
        float grub = Mathf.Max(1.5f, p * .09f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // odległość od zaokrąglonego prostokąta wypełniającego całą teksturę (w pikselach)
                float qx = Mathf.Abs(x + .5f - n * .5f) - (n * .5f - p), qy = Mathf.Abs(y + .5f - n * .5f) - (n * .5f - p);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - p;
                float a = Mathf.Clamp01(.5f - d);
                if (obrys) a *= Mathf.Clamp01(d + grub + .5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        t.SetPixels32(px); t.Apply(false, true);
        var st = new GUIStyle { border = new RectOffset(p + 1, p + 1, p + 1, p + 1) };
        st.normal.background = t;
        return st;
    }

    // ---- napisy ----
    static readonly Dictionary<int, GUIStyle> style = new Dictionary<int, GUIStyle>();
    // rozmiar w px, pogrubienie, wyrównanie, zawijanie — jeden styl na kombinację (klucz liczbowy, bez alokacji)
    public static GUIStyle Styl(int rozmiar, bool gruby, TextAnchor wyr, bool zawijaj = false)
    {
        rozmiar = Mathf.Clamp(rozmiar, 8, 400);
        int klucz = rozmiar * 64 + (gruby ? 32 : 0) + (int)wyr * 2 + (zawijaj ? 1 : 0);
        if (!style.TryGetValue(klucz, out var st))
        {
            st = new GUIStyle(GUI.skin.label) { fontSize = rozmiar, fontStyle = gruby ? FontStyle.Bold : FontStyle.Normal, alignment = wyr, wordWrap = zawijaj, richText = false, clipping = TextClipping.Overflow };
            st.padding = new RectOffset(0, 0, 0, 0); st.margin = new RectOffset(0, 0, 0, 0);
            st.normal.textColor = Color.white;
            style[klucz] = st;
        }
        return st;
    }

    // napis z miękkim cieniem (czytelny na jasnym niebie i na ciemnym wnętrzu)
    public static void Napis(Rect r, string tekst, GUIStyle st, Color c, bool cien = true)
    {
        if (string.IsNullOrEmpty(tekst) || Event.current.type != EventType.Repaint) return;
        var old = GUI.color;
        if (cien)
        {
            float o = Mathf.Max(1f, st.fontSize * .06f);
            GUI.color = new Color(0f, 0f, 0f, .55f * c.a);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), tekst, st);
        }
        GUI.color = c; GUI.Label(r, tekst, st);
        GUI.color = old;
    }

    public static float Szerokosc(string tekst, GUIStyle st)
    {
        if (string.IsNullOrEmpty(tekst)) return 0f;
        tmp.text = tekst; return st.CalcSize(tmp).x;
    }
    public static float Wysokosc(string tekst, GUIStyle st, float szer)
    {
        if (string.IsNullOrEmpty(tekst)) return 0f;
        tmp.text = tekst; return st.CalcHeight(tmp, szer);
    }
    static readonly GUIContent tmp = new GUIContent();

    // przycisk kołowy HUD: miękki cień, szkło, cienki obrys, ikona (c = środek w IMGUI)
    public static void PrzyciskKolowy(Vector2 c, float r, Texture ikona, bool wcisniety, Color akcent, float alfa = 1f)
    {
        if (Event.current.type != EventType.Repaint) return;
        Kolo(c + new Vector2(0f, r * .06f), r * 1.16f, BraxIkony.Cien, new Color(0f, 0f, 0f, .28f * alfa));
        Kolo(c, r, BraxIkony.Kolo, wcisniety ? new Color(akcent.r, akcent.g, akcent.b, .62f * alfa) : new Color(Szklo.r, Szklo.g, Szklo.b, Szklo.a * alfa));
        Kolo(c, r, BraxIkony.PierscienCienki, new Color(1f, 1f, 1f, (wcisniety ? .95f : .6f) * alfa));
        if (ikona) Kolo(c, r * .5f, ikona, new Color(1f, 1f, 1f, .93f * alfa));
    }
}
