using UnityEngine;

// EKRAN STARTOWY (właściciel 3.11, 01:10 — grafika „ring, zielony START”, wybór właściciela).
// 3.11 14:56 (właściciel): „ma się pokazać normalnie [od razu po uruchomieniu], ma lecieć muzyczka w tle, nie ma być gry
// [za nim], START ma działać, USTAWIENIA mają działać, ma być na pełen ekran”. Dlatego ekran jest pokazywany w SCENIE 0
// (BraxStart) — zanim telefon zacznie włączać miasto / osiedle; scena gry wczytuje się w tle, START ją włącza.
// Muzyka: Resources „BraxStartMuzyka” (piosenka willi z zaszyfrowanej paczki grafik), głośność z ustawień (Muzyka).
// USTAWIENIA: panel w miejscu (muzyka, efekty, grafika, język, POWRÓT) — zapis w BraxUstawienia / BraxGrafika / BraxTeksty.
// Obraz NA PEŁNY EKRAN (wypełnia ekran, nadmiar obcięty równo z obu stron — przyciski są w środkowej części obrazu).
// Przyciski START i USTAWIENIA są NAMALOWANE na obrazie — tu pola dotyku nad nimi + „oddech” START i wciśnięcie.
// W grze (scena 1) ekran już się nie pokazuje, gdy był w scenie 0 (PokazanyNaStarcie). IMGUI (moduł UI wyłączony).
public static class BraxEkranStartowy
{
    // prostokąty przycisków w pikselach obrazu źródłowego 1672 × 941 (x, y od góry, szer., wys.)
    static readonly Rect StartPx = new Rect(92f, 500f, 496f, 142f), UstawieniaPx = new Rect(182f, 683f, 375f, 74f);
    const float TW = 1672f, TH = 941f;

    public static bool Czeka { get; private set; }
    public static bool PokazanyNaStarcie;   // ustawiane przez BraxStart po START w scenie 0
    static bool sprawdzony;
    static Texture2D tex;
    static int wcisniety;   // 0 — nic, 1 — START, 2 — USTAWIENIA

    public static bool Zaladuj()
    {
        if (!tex) tex = Resources.Load<Texture2D>("BraxStart");
        return tex;
    }
    public static void Zwolnij() { tex = null; wcisniety = 0; }

    // dotyk / mysz nad obrazem: 1 = puszczono na START, 2 = puszczono na USTAWIENIA, 0 = nic
    public static int Dotyk()
    {
        Rect o = Obraz(Screen.width, Screen.height), st = Pole(o, StartPx), us = Pole(o, UstawieniaPx);
        int wynik = 0;
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            var g = new Vector2(t.position.x, Screen.height - t.position.y);
            if (t.phase == TouchPhase.Began) wcisniety = st.Contains(g) ? 1 : us.Contains(g) ? 2 : 0;
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                int w = wcisniety; wcisniety = 0;
                if (t.phase == TouchPhase.Ended && w == 1 && st.Contains(g)) wynik = 1;
                if (t.phase == TouchPhase.Ended && w == 2 && us.Contains(g)) wynik = 2;
            }
        }
        if (Input.touchCount == 0)
        {
            var g = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (Input.GetMouseButtonDown(0)) wcisniety = st.Contains(g) ? 1 : us.Contains(g) ? 2 : 0;
            if (Input.GetMouseButtonUp(0)) { int w = wcisniety; wcisniety = 0; if (w == 1 && st.Contains(g)) wynik = 1; if (w == 2 && us.Contains(g)) wynik = 2; }
        }
        return wynik;
    }

    // W GRZE (scena 1): tylko gdy ekran NIE był pokazany w scenie 0 (np. stara paczka bez sceny startowej)
    public static bool Aktualizuj(BraxTouchControls ster)
    {
        if (!sprawdzony)
        {
            sprawdzony = true;
            if (!PokazanyNaStarcie && !BraxTouchControls.Qa) Zaladuj();
            Czeka = !PokazanyNaStarcie && tex != null;
            Debug.Log("BRAXHORN_START ekran startowy w grze: " + (PokazanyNaStarcie ? "pominięty (był przed wczytywaniem)" : Czeka ? "pokazany" : "brak obrazu / podgląd CI"));
        }
        if (!Czeka) return false;
        ster.Pauza(true);
        int w = Dotyk();
        if (w != 0)
        {
            Czeka = false;
            Debug.Log("BRAXHORN_START wybrano: " + (w == 2 ? "USTAWIENIA" : "START"));
            if (w == 2) ster.OtworzEkran(BraxMenuGry.Ekran.Ustawienia); else ster.Pauza(false);
            Zwolnij(); Resources.UnloadUnusedAssets();
        }
        return Czeka;
    }

    public static void Rysuj() { if (Czeka) RysujObraz(true); }

    // obraz na pełny ekran (bez czarnych pasów), START „oddycha”; przyciski = true rysuje animację przycisków
    public static void RysujObraz(bool przyciski)
    {
        if (!tex || Event.current.type != EventType.Repaint) return;
        float W = Screen.width, H = Screen.height;
        Color c0 = GUI.color;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture); GUI.color = c0;
        Rect o = Obraz(W, H);
        GUI.DrawTexture(o, tex, ScaleMode.StretchToFill);
        if (!przyciski) return;
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

    // pełny ekran: skala = większa z (szer./szer. obrazu, wys./wys. obrazu); nadmiar obcięty równo; przyciski zawsze widoczne
    static Rect Obraz(float W, float H)
    {
        float sc = Mathf.Max(W / TW, H / TH);
        float w = TW * sc, h = TH * sc;
        float x = (W - w) * .5f, y = (H - h) * .5f;
        // przyciski leżą w lewej części obrazu — przy mocnym obcięciu boków przesuń obraz tak, by START mieścił się na ekranie
        float lewyStart = x + (StartPx.x - 20f) * sc; if (lewyStart < 0f) x = Mathf.Min(0f, x - lewyStart);
        float dolUst = y + (UstawieniaPx.yMax + 20f) * sc; if (dolUst > H) y = Mathf.Max(H - h, y - (dolUst - H));
        return new Rect(x, y, w, h);
    }

    static Rect Pole(Rect o, Rect px)
    {
        float kx = o.width / TW, ky = o.height / TH;
        return new Rect(o.x + px.x * kx, o.y + px.y * ky, px.width * kx, px.height * ky);
    }
}
