using System;
using System.Collections.Generic;
using UnityEngine;

// THE COUPON ON THE WHOLE SCREEN (owner 23.11 15:54: "jak podchodzisz do kuponów ... pokazuje się na ekranie sam kupon i
// skreślasz sam numerki"; 16:59: "podchodzisz, akcja, pokazuje się na cały ekran kupon do wypełnienia, skreślasz sobie
// samemu ... wybierasz mały lotek czy duży lotek, kupujesz kupon potem, idziesz do kasy ... jak najbardziej premium").
//   - AKCJA at the coupon desk of the kolektura (or at its counter) opens the coupon over the whole screen; the game waits
//     (the touch controls are paused the way their own menu pauses them, so no finger walks or punches meanwhile);
//   - DUŻY LOTEK: ten numbers of 60, 3 zł, the KUMULACJA; MAŁY LOTEK: five of 60 on the same draw, 2 zł
//     (BraxChampionsLototo.Zasady); the numbers are crossed with a finger, CHYBIŁ TRAFIŁ fills the rest at random,
//     WYCZYŚĆ clears;
//   - at the desk the coupon goes DO KASY: the player takes it to the counter, AKCJA there pays and the till prints the
//     receipt (Paragon: the draw, the numbers, the price, the coupon's number); at the counter it is paid at once.
// Drawn with IMGUI like the other screens of the region; one instance, made when first needed.
public sealed class BraxChampionsLototoKupon : MonoBehaviour
{
    static BraxChampionsLototoKupon inst;
    BraxChampionsLototo lot; bool otwarty, przyKasie, maly, pauza;
    readonly List<int> skreslone = new List<int>();
    readonly System.Random los = new System.Random();
    string[] paragon; float paragonDo, bladDo; string blad = "";
    GUIStyle sNum, sTyt, sMaly, sBtn, sPar; float styleS = -1f;

    static readonly Color Papier = new Color(.985f, .975f, .945f), Czerwony = new Color(.80f, .10f, .12f), Granat = new Color(.06f, .11f, .30f),
        Atrament = new Color(.08f, .20f, .62f, .92f), Zielony = new Color(.10f, .55f, .28f), Szary = new Color(.55f, .56f, .60f), Tlo = new Color(.02f, .03f, .08f, .94f),
        Zloto = new Color(.97f, .78f, .26f);

    public static bool Otwarty => inst && inst.otwarty;

    static BraxChampionsLototoKupon Inst()
    {
        if (inst) return inst;
        var g = new GameObject("LOTOTO kupon (cały ekran)"); DontDestroyOnLoad(g);
        inst = g.AddComponent<BraxChampionsLototoKupon>(); return inst;
    }

    /// <summary>Opens the coupon over the whole screen. przyKasie: paid at once (at the counter), else it goes to the till.</summary>
    public static void Otworz(BraxChampionsLototo lot, bool przyKasie)
    {
        var k = Inst(); if (k.otwarty || !lot) return;
        k.lot = lot; k.przyKasie = przyKasie; k.otwarty = true; k.blad = ""; k.skreslone.Clear();
        var z = lot.Z; k.maly = false;
        var czeka = lot.DoKasy;                                       // a coupon already filled and not paid: back on the desk
        if (czeka != null) { k.maly = z.Maly(czeka); k.skreslone.AddRange(czeka); }
        k.Pauza(true);
        Debug.Log("LOTOTO_KUPON otwarty przyKasie=" + przyKasie + " gra=" + (k.maly ? "MAŁY" : "DUŻY"));
    }

    /// <summary>The till's receipt in the corner for a few seconds.</summary>
    public static void Paragon(string[] linie, float sekundy = 6f) { var k = Inst(); k.paragon = linie; k.paragonDo = Time.unscaledTime + sekundy; }

    void Zamknij()
    {
        if (!otwarty) return;
        otwarty = false; Pauza(false);
    }

    // the touch controls pause the game and stop reading fingers while their menu is open: the coupon uses the same pause
    // (their own menu is drawn under the coupon and gets no taps); without them (the editor) the player is paused directly
    void Pauza(bool on)
    {
        if (pauza == on) return;
        pauza = on;
        var tc = FindAnyObjectByType<BraxTouchControls>();
        if (tc && tc.Gracz) { tc.Pauza(on); if (on) Input.simulateMouseWithTouches = true; return; }
        var p = FindAnyObjectByType<BraxGymPlayer>();
        if (p) p.SendMessage("Command", "pause", SendMessageOptions.DontRequireReceiver);
    }

    void Update()
    {
        if (otwarty && Input.GetKeyDown(KeyCode.Escape)) Zamknij();
        if (otwarty && !lot) Zamknij();
    }

    void Style(float s)
    {
        if (Mathf.Approximately(styleS, s) && sNum != null) return;
        styleS = s;
        sNum = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .040f) };
        sTyt = new GUIStyle(sNum) { fontSize = Mathf.RoundToInt(s * .056f) };
        sMaly = new GUIStyle(sNum) { fontSize = Mathf.RoundToInt(s * .026f), fontStyle = FontStyle.Bold };
        sBtn = new GUIStyle(sNum) { fontSize = Mathf.RoundToInt(s * .030f) };
        sPar = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .024f), wordWrap = false };
    }

    static void Pole(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = o; }
    static void Ramka(Rect r, Color c, float t) { Pole(new Rect(r.x, r.y, r.width, t), c); Pole(new Rect(r.x, r.yMax - t, r.width, t), c); Pole(new Rect(r.x, r.y, t, r.height), c); Pole(new Rect(r.xMax - t, r.y, t, r.height), c); }
    static void Napis(Rect r, string t, GUIStyle st, Color c) { st.normal.textColor = c; GUI.Label(r, t, st); }
    // a cross made with a pen: two strokes, a little uneven
    static void Krzyzyk(Rect r, Color c, float t)
    {
        var m = GUI.matrix; var ctr = r.center; float d = Mathf.Min(r.width, r.height) * 1.02f;
        GUIUtility.RotateAroundPivot(41f, ctr); Pole(new Rect(ctr.x - d * .5f, ctr.y - t * .5f, d, t), c); GUI.matrix = m;
        GUIUtility.RotateAroundPivot(-47f, ctr); Pole(new Rect(ctr.x - d * .5f, ctr.y - t * .5f, d * .96f, t), c); GUI.matrix = m;
    }
    bool Przycisk(Rect r, string t, Color tlo, Color tekst, bool aktywny = true)
    {
        Pole(r, aktywny ? tlo : new Color(tlo.r * .5f + .25f, tlo.g * .5f + .25f, tlo.b * .5f + .25f, tlo.a));
        Napis(r, t, sBtn, tekst);
        return aktywny && Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition);
    }

    void OnGUI()
    {
        float s = Mathf.Min(Screen.width, Screen.height); Style(s);
        if (!otwarty) { if (paragon != null && Time.unscaledTime < paragonDo) { GUI.depth = -15; RysujParagon(s); } return; }
        GUI.depth = -25;
        var z = lot ? lot.Z : null; if (z == null) { Zamknij(); return; }
        int typy = z.Typy(maly), cena = z.Cena(maly);
        Pole(new Rect(0, 0, Screen.width, Screen.height), Tlo);
        var safe = Screen.safeArea; safe.y = Screen.height - safe.yMax;                 // IMGUI: y down
        // the coupon: a sheet of paper, the brand's navy head, the numbers 10 x 6, the buttons on the right
        float H = safe.height * .94f, Wd = Mathf.Min(safe.width * .70f, H * 1.32f);
        var kart = new Rect(safe.x + (safe.width - Wd - s * .30f) * .5f, safe.y + (safe.height - H) * .5f, Wd, H);
        Pole(new Rect(kart.x + s * .008f, kart.y + s * .010f, kart.width, kart.height), new Color(0, 0, 0, .45f));   // its shadow
        Pole(kart, Papier);
        var head = new Rect(kart.x, kart.y, kart.width, H * .17f);
        Pole(head, Granat); Pole(new Rect(head.x, head.yMax - s * .006f, head.width, s * .006f), Zloto);
        Napis(new Rect(head.x, head.y + H * .01f, head.width, H * .095f), z.nazwa, sTyt, Zloto);
        var now = DateTime.Now; var nast = z.Nastepne(now);
        string pod = "LOSOWANIE " + BraxChampionsLototo.Zasady.Nazwa(nast) + " " + nast.ToString("dd.MM") + " " + z.godzina + ":00" +
                     (maly ? "" : " · KUMULACJA " + z.Kumulacja(nast).ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ') + " ZŁ");
        Napis(new Rect(head.x, head.y + H * .095f, head.width, H * .06f), pod, sMaly, Color.white);
        float y = head.yMax + H * .025f;
        // the game: DUŻY LOTEK / MAŁY LOTEK (only where the game has the small one)
        if (z.malyTypy > 0)
        {
            float tw = (kart.width - s * .09f) * .5f, th = H * .085f;
            var t1 = new Rect(kart.x + s * .03f, y, tw, th); var t2 = new Rect(t1.xMax + s * .03f, y, tw, th);
            foreach (var (r, m, txt) in new[] { (t1, false, "DUŻY LOTEK · " + z.typy + " Z " + z.pula + " · " + z.cena + " ZŁ"), (t2, true, "MAŁY LOTEK · " + z.malyTypy + " Z " + z.pula + " · " + z.malyCena + " ZŁ") })
            {
                bool wyb = maly == m;
                Pole(r, wyb ? Czerwony : Papier); Ramka(r, Czerwony, s * .004f);
                Napis(r, txt, sBtn, wyb ? Color.white : Czerwony);
                if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition) && maly != m)
                {
                    maly = m; blad = "";
                    while (skreslone.Count > z.Typy(maly)) skreslone.RemoveAt(skreslone.Count - 1);
                }
            }
            y += th + H * .025f;
        }
        // the grid
        const int kol = 10; int wier = (z.pula + kol - 1) / kol;
        float gx = kart.x + s * .03f, gw = kart.width - s * .06f, gh = kart.yMax - y - H * .11f;
        float c = Mathf.Min(gw / kol, gh / wier), ox = gx + (gw - c * kol) * .5f;
        for (int n = 1; n <= z.pula; n++)
        {
            int i = (n - 1) % kol, j = (n - 1) / kol;
            var r = new Rect(ox + i * c + c * .06f, y + j * c + c * .06f, c * .88f, c * .88f);
            bool sk = skreslone.Contains(n);
            Pole(r, sk ? new Color(.86f, .90f, 1f) : Color.white); Ramka(r, Czerwony, Mathf.Max(1f, s * .0026f));
            Napis(r, n.ToString(), sNum, Czerwony);
            if (sk) Krzyzyk(r, Atrament, Mathf.Max(2f, s * .0075f));
            if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition))
            {
                if (sk) skreslone.Remove(n);
                else if (skreslone.Count < typy) skreslone.Add(n);
                else { blad = "MOŻESZ SKREŚLIĆ " + typy + " LICZB"; bladDo = Time.unscaledTime + 1.6f; }
            }
        }
        // what is left to cross
        int zostalo = typy - skreslone.Count;
        string stan = Time.unscaledTime < bladDo && blad.Length > 0 ? blad : zostalo > 0 ? "SKREŚL JESZCZE " + zostalo + " Z " + typy : "KUPON GOTOWY · " + typy + " LICZB";
        Napis(new Rect(kart.x, kart.yMax - H * .10f, kart.width, H * .08f), stan, sBtn, zostalo > 0 ? Granat : Zielony);
        // the buttons, right of the coupon
        float bx = kart.xMax + s * .035f, bw = Mathf.Min(s * .26f, safe.xMax - bx - s * .02f), bh = H * .13f, by = kart.y + H * .04f, gap = H * .035f;
        if (Przycisk(new Rect(bx, by, bw, bh), "CHYBIŁ TRAFIŁ", Zloto, Granat))
        {
            while (skreslone.Count < typy) { int n = los.Next(1, z.pula + 1); if (!skreslone.Contains(n)) skreslone.Add(n); }
        }
        by += bh + gap;
        if (Przycisk(new Rect(bx, by, bw, bh), "WYCZYŚĆ", Color.white, Granat)) skreslone.Clear();
        by += bh + gap;
        bool gotowy = zostalo == 0;
        string cta = przyKasie ? "KUPUJĘ · " + cena + " ZŁ" : "DO KASY · " + cena + " ZŁ";
        if (Przycisk(new Rect(bx, by, bw, bh * 1.25f), cta, Zielony, Color.white, gotowy))
        {
            var w = skreslone.ToArray(); Array.Sort(w);
            if (przyKasie)
            {
                if (lot.Kup(w, out string b)) Zamknij(); else { blad = b; bladDo = Time.unscaledTime + 2.4f; }
            }
            else
            {
                lot.DoKasy = w; Zamknij();
                BraxChampionsGry.Pokaz("KUPON WYPEŁNIONY · ZAPŁAĆ PRZY KASIE · " + cena + " ZŁ", 3f);
            }
        }
        by += bh * 1.25f + gap;
        if (Przycisk(new Rect(bx, by, bw, bh), "ZAMKNIJ", new Color(.20f, .22f, .28f), Color.white)) Zamknij();
        // every tap belongs to the coupon (nothing under it reacts)
        if (Event.current.isMouse) Event.current.Use();
    }

    void RysujParagon(float s)
    {
        float w = s * .52f, lh = s * .034f, h = lh * (paragon.Length + 1.2f);
        var r = new Rect(Screen.width - w - s * .04f, s * .16f, w, h);
        Pole(new Rect(r.x + s * .006f, r.y + s * .008f, w, h), new Color(0, 0, 0, .35f));
        Pole(r, new Color(.99f, .99f, .97f));
        for (int i = 0; i < 14; i++) Pole(new Rect(r.x + i * w / 14f, r.yMax - s * .006f, w / 28f, s * .006f), new Color(.82f, .82f, .80f));   // the torn edge
        for (int i = 0; i < paragon.Length; i++)
        {
            var st = sPar; st.alignment = i == 0 || i == paragon.Length - 1 ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            Napis(new Rect(r.x + s * .02f, r.y + lh * (i + .6f) - lh * .5f, w - s * .04f, lh), paragon[i], st, i == 0 ? Czerwony : new Color(.12f, .12f, .14f));
        }
    }
}
