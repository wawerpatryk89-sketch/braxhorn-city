using UnityEngine;

// EKRAN STARTOWY (właściciel 3.11, 01:10 — grafika „ring, zielony START”, wybór właściciela). Pokazywany RAZ po uruchomieniu
// gry, gdy scena jest już wczytana i gracz znaleziony (ekran wczytywania był wcześniej): gra stoi w pauzie za obrazem,
// START = gra od razu, USTAWIENIA = menu ustawień (z menu wraca się do gry jak zwykle).
// Obraz: Resources „BraxStart” z zaszyfrowanej paczki grafik (prywatne.py → Assets/Source/Prywatne/Grafiki/Resources/).
// Brak obrazu (paczka nieodszyfrowana) albo podgląd CI (?qa=1) = brak ekranu, gra startuje jak dotąd.
// Obraz bez ucinania: dopasowany do wysokości ekranu (telefon 20:9), boki dopełnione rozciągniętym brzegiem obrazu
// przyciemnionym (jak winieta); na ekranach węższych niż obraz — dopasowany do szerokości, góra/dół czarne.
// Przyciski START i USTAWIENIA są NAMALOWANE na obrazie — tu tylko pola dotyku nad nimi + „oddech” przycisku START
// (powiększenie wycinka obrazu o 1.5 %) i wciśnięcie (pomniejszenie). IMGUI (moduł UI wyłączony).
public static class BraxEkranStartowy
{
    // prostokąty przycisków w pikselach obrazu źródłowego 1672 × 941 (x, y od góry, szer., wys.)
    static readonly Rect StartPx = new Rect(92f, 500f, 496f, 142f), UstawieniaPx = new Rect(182f, 683f, 375f, 74f);

    const float TW = 1672f, TH = 941f;   // przestrzeń pikseli obrazu źródłowego (niezależnie od rozmiaru po imporcie)

    public static bool Czeka { get; private set; }
    static bool sprawdzony;
    static Texture2D tex;
    static int wcisniety;   // 0 — nic, 1 — START, 2 — USTAWIENIA

    // wywoływane z BraxTouchControls.Update, gdy gracz już jest; true = ekran startowy aktywny (sterowanie gry wyłączone)
    public static bool Aktualizuj(BraxTouchControls ster)
    {
        if (!sprawdzony)
        {
            sprawdzony = true;
            if (!BraxTouchControls.Qa) tex = Resources.Load<Texture2D>("BraxStart");
            Czeka = tex != null;
            Debug.Log("BRAXHORN_START ekran startowy: " + (Czeka ? "pokazany (" + tex.width + "×" + tex.height + ")"
                : BraxTouchControls.Qa ? "pominięty (podgląd CI)" : "brak obrazu BraxStart — pominięty"));
        }
        if (!Czeka) return false;
        ster.Pauza(true);   // bez skutku, gdy już w pauzie
        Rect o = Obraz(Screen.width, Screen.height), st = Pole(o, StartPx), us = Pole(o, UstawieniaPx);
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            var g = new Vector2(t.position.x, Screen.height - t.position.y);
            if (t.phase == TouchPhase.Began) wcisniety = st.Contains(g) ? 1 : us.Contains(g) ? 2 : 0;
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                int w = wcisniety; wcisniety = 0;
                if (t.phase == TouchPhase.Ended && w == 1 && st.Contains(g)) { Zamknij(ster, false); break; }
                if (t.phase == TouchPhase.Ended && w == 2 && us.Contains(g)) { Zamknij(ster, true); break; }
            }
        }
        return Czeka;
    }

    static void Zamknij(BraxTouchControls ster, bool ustawienia)
    {
        Czeka = false; wcisniety = 0;
        Debug.Log("BRAXHORN_START wybrano: " + (ustawienia ? "USTAWIENIA" : "START"));
        if (ustawienia) ster.OtworzEkran(BraxMenuGry.Ekran.Ustawienia);
        else ster.Pauza(false);
        tex = null;
        Resources.UnloadUnusedAssets();   // obraz startowy nie jest już potrzebny w pamięci
    }

    // wywoływane z BraxTouchControls.OnGUI (GUI.depth -50), tylko gdy Czeka
    public static void Rysuj()
    {
        if (!tex || Event.current.type != EventType.Repaint) return;
        float W = Screen.width, H = Screen.height;
        Rect o = Obraz(W, H);
        Color c0 = GUI.color;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = c0;
        if (o.x > .5f)
        {   // boki: rozciągnięty brzeg obrazu + przyciemnienie (bez czarnych pasów i bez ucinania obrazu)
            Rect l = new Rect(0, o.y, o.x, o.height), p = new Rect(o.xMax, o.y, W - o.xMax, o.height);
            GUI.DrawTextureWithTexCoords(l, tex, new Rect(0f, 0f, .004f, 1f));
            GUI.DrawTextureWithTexCoords(p, tex, new Rect(.996f, 0f, .004f, 1f));
            GUI.color = new Color(0f, 0f, 0f, .55f);
            GUI.DrawTexture(l, Texture2D.whiteTexture); GUI.DrawTexture(p, Texture2D.whiteTexture);
            GUI.color = c0;
        }
        GUI.DrawTexture(o, tex, ScaleMode.StretchToFill);
        float oddech = 1f + .015f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 3.2f));
        Przycisk(o, StartPx, wcisniety == 1 ? .96f : oddech);
        if (wcisniety == 2) Przycisk(o, UstawieniaPx, .96f);
    }

    static void Przycisk(Rect o, Rect px, float skala)
    {
        Rect r = Pole(o, px);
        Rect s = new Rect(r.center.x - r.width * skala * .5f, r.center.y - r.height * skala * .5f, r.width * skala, r.height * skala);
        var uv = new Rect(px.x / TW, 1f - (px.y + px.height) / TH, px.width / TW, px.height / TH);
        GUI.DrawTextureWithTexCoords(s, tex, uv);
    }

    // obraz dopasowany do wysokości ekranu; gdy ekran węższy niż proporcje obrazu — do szerokości
    static Rect Obraz(float W, float H)
    {
        float sc = H / TH;
        if (TW * sc > W) sc = W / TW;
        float w = TW * sc, h = TH * sc;
        return new Rect((W - w) * .5f, (H - h) * .5f, w, h);
    }

    static Rect Pole(Rect o, Rect px)
    {
        float kx = o.width / TW, ky = o.height / TH;
        return new Rect(o.x + px.x * kx, o.y + px.y * ky, px.width * kx, px.height * ky);
    }
}
