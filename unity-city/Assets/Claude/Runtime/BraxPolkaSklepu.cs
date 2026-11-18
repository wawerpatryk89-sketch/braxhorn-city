using System.Collections.Generic;
using UnityEngine;

// ZAKUPY Z PÓŁKI — TEST MECHANIZMU (właściciel 18.11, 16:12–16:14).
//
//   „Wystarczy, że zbliżenie kamery na półkę z towarem i ty możesz wybierać z tej półki
//    albo przesuwać w bok."
//   „Ceny i takie rzeczy, że naprawdę się będzie kupować, to się będzie robić później."
//
// Więc to jest SAM MECHANIZM: podejście, zbliżenie, przesuwanie, wybór, wzięcie. Bez konta,
// bez cen, bez kasy — te przychodzą później i dla całej gry naraz, nie dla jednej apteki.
//
// JAK TO JEST WPIĘTE. Kamerę w tej grze prowadzi w całości BraxGymPlayer.UpdateCamera i ma już
// jeden taki tryb: po wsiadaniu do auta kamera przechodzi na auto (pole autoGracza). Półka jest
// drugim takim trybem, zrobionym tak samo (pole polka) — nie obok, tylko w tym samym miejscu,
// bo dwa niezależne sterowania kamerą zawsze kończą się szarpaniem.
//
// DLACZEGO DOTYK ŁAPIE NAJBLIŻSZY PRODUKT, A NIE TRAFIONY. Pudełko leku ma ok. 10 cm, na ekranie
// telefonu to kilka milimetrów. Trafienie w nie promieniem z palca jest nierealne, więc bierzemy
// produkt NAJBLIŻSZY promieniowi (do 25 cm od niego). Bez tego cały mechanizm jest nie do użycia.
public sealed class BraxPolkaSklepu : MonoBehaviour, IBraxInterakcja
{
    // Jeden poziom regału: linia, wzdłuż której stoi towar.
    [System.Serializable]
    public sealed class Rzad
    {
        public Vector3 start;        // świat: początek rzędu (lewy koniec)
        public Vector3 wzdluz;       // świat: kierunek wzdłuż rzędu, jednostkowy
        public Vector3 przod;        // świat: z półki w stronę klienta, jednostkowy
        public float dlugosc;
        public Transform[] towar = new Transform[0];
        public string[] nazwy = new string[0];
    }

    public string sklep = "sklep";
    public Rzad[] rzedy = new Rzad[0];
    public float zasiegPodejscia = 2.6f;

    public const float OdlegloscKamery = 1.2f;   // ile kamera stoi od lica półki
    const float SzerokoscKadru = 1.2f;           // ile półki widać naraz — reszta palcem w bok
    const float PromienDotyku = .25f;            // jak blisko promienia musi być produkt
    const float Wysuniecie = .035f;              // o ile wybrany produkt wyjeżdża z półki

    int poziom, wybrany = -1;
    float przesuniecie;
    bool wTrybie;
    Vector3 wybranyDom;
    readonly List<string> koszyk = new List<string>();
    BraxGymPlayer gracz;

    // dotyk
    int palec = -1;
    Vector2 palecStart, palecOstatni;
    float palecCzas;
    bool palecCiagnie;

    public bool WTrybie => wTrybie;
    public int WKoszyku => koszyk.Count;

    // Rejestracja w spisie akcji — bez tego gracz nigdy nie zobaczy napisu „PÓŁKA".
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable()
    {
        if (wTrybie && gracz) { if (gracz.polka == this) gracz.polka = null; gracz.KameraOdNowa(); }
        wTrybie = false;
        BraxInterakcje.Usun(this);
    }

    // ---------------- podejście ----------------

    bool Dobry => rzedy != null && rzedy.Length > 0;

    float DoGracza(Vector3 p)
    {
        float naj = float.PositiveInfinity;
        foreach (var r in rzedy)
        {
            if (r == null || r.towar == null || r.towar.Length == 0) continue;
            float t = Mathf.Clamp(Vector3.Dot(p - r.start, r.wzdluz), 0f, r.dlugosc);
            naj = Mathf.Min(naj, Vector3.Distance(p, r.start + r.wzdluz * t));
        }
        return naj;
    }

    public string Kontekst(Vector3 p)
    {
        if (wTrybie || !Dobry) return "";
        return DoGracza(p) <= zasiegPodejscia ? "PÓŁKA · " + sklep : "";
    }

    public bool Wykonaj(Vector3 p)
    {
        if (wTrybie || !Dobry || DoGracza(p) > zasiegPodejscia) return false;
        gracz = Object.FindFirstObjectByType<BraxGymPlayer>();
        if (!gracz || !gracz.view) return false;

        // poziom najbliżej wzroku gracza (oczy ok. 1,6 m), przesunięcie naprzeciw niego
        float oczy = p.y + 1.6f; poziom = 0; float naj = float.PositiveInfinity;
        for (int i = 0; i < rzedy.Length; i++)
        {
            if (rzedy[i] == null || rzedy[i].towar.Length == 0) continue;
            float d = Mathf.Abs(rzedy[i].start.y - oczy);
            if (d < naj) { naj = d; poziom = i; }
        }
        var r = rzedy[poziom];
        przesuniecie = Mathf.Clamp(Vector3.Dot(p - r.start, r.wzdluz), SzerokoscKadru * .5f, Mathf.Max(SzerokoscKadru * .5f, r.dlugosc - SzerokoscKadru * .5f));
        wybrany = -1; wTrybie = true; palec = -1; palecCiagnie = false;
        gracz.polka = this;
        Debug.Log("BRAX_POLKA wejście " + sklep + " poziomów=" + rzedy.Length + " poziom=" + poziom + " towaru=" + r.towar.Length);
        return true;
    }

    public void Wyjdz()
    {
        Odloz();
        wTrybie = false; palec = -1;
        if (gracz) { if (gracz.polka == this) gracz.polka = null; gracz.KameraOdNowa(); }
        Debug.Log("BRAX_POLKA wyjście " + sklep + " wzięto=" + koszyk.Count);
    }

    // ---------------- kamera ----------------

    Vector3 PunktNaPolce => rzedy[poziom].start + rzedy[poziom].wzdluz * przesuniecie + Vector3.up * .12f;

    public void UstawKamere(Camera cam, float dt)
    {
        if (!cam || !Dobry) return;
        var r = rzedy[poziom];
        Vector3 cel = PunktNaPolce;
        Vector3 poz = cel + r.przod * OdlegloscKamery;
        float w = 1f - Mathf.Exp(-dt * 9f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, poz, w);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(cel - poz, Vector3.up), w);
    }

    // ---------------- krok i dotyk ----------------

    /// <summary>Woła BraxGymPlayer zamiast zwykłego ruchu, gdy gracz stoi przy półce.</summary>
    public void Krok(float dt)
    {
        if (!wTrybie) return;
        if (!Dobry) { Wyjdz(); return; }
        if (Input.touchCount > 0) Palce();
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) Mysz();
        else if (palec >= 0) { palec = -1; palecCiagnie = false; }
    }

    void Palce()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (palec < 0 && t.phase == TouchPhase.Began) { Zaczyna(t.fingerId, t.position); continue; }
            if (t.fingerId != palec) continue;
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) Ciagnie(t.position);
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) Konczy(t.position);
        }
    }

    void Mysz()
    {
        if (palec < 0 && Input.GetMouseButtonDown(0)) Zaczyna(-2, Input.mousePosition);
        else if (palec == -2 && Input.GetMouseButtonUp(0)) Konczy(Input.mousePosition);
        else if (palec == -2) Ciagnie(Input.mousePosition);
    }

    void Zaczyna(int id, Vector2 p)
    {
        if (WPrzycisku(p)) return;            // przyciski obsługuje IMGUI
        palec = id; palecStart = palecOstatni = p; palecCzas = Time.unscaledTime; palecCiagnie = false;
    }

    void Ciagnie(Vector2 p)
    {
        var d = p - palecOstatni;
        if (!palecCiagnie && (p - palecStart).sqrMagnitude > Screen.height * Screen.height * .0004f) palecCiagnie = true;
        if (!palecCiagnie) { palecOstatni = p; return; }
        var r = rzedy[poziom];
        // w bok: palec w prawo = półka jedzie w prawo, czyli kamera w lewo — jak przesuwanie kartki
        float metryNaPiksel = SzerokoscKadru / Mathf.Max(1f, Screen.width * .9f);
        przesuniecie = Mathf.Clamp(przesuniecie - d.x * metryNaPiksel,
                                   Mathf.Min(SzerokoscKadru * .5f, r.dlugosc * .5f),
                                   Mathf.Max(SzerokoscKadru * .5f, r.dlugosc - SzerokoscKadru * .5f));
        // góra-dół: poziomy przeskakują zatrzaskowo, bo płynnie zawsze kończy się patrzeniem między półki
        if (Mathf.Abs(p.y - palecStart.y) > Screen.height * .12f && Mathf.Abs(p.y - palecStart.y) > Mathf.Abs(p.x - palecStart.x))
        {
            int kier = p.y > palecStart.y ? -1 : 1;     // palec w górę = wyższa półka
            ZmienPoziom(kier);
            palecStart = p;
        }
        palecOstatni = p;
    }

    void Konczy(Vector2 p)
    {
        bool stuk = !palecCiagnie && Time.unscaledTime - palecCzas < .5f;
        palec = -1; palecCiagnie = false;
        if (stuk) Wskaz(p);
    }

    void ZmienPoziom(int kier)
    {
        int n = poziom;
        for (int k = 0; k < rzedy.Length; k++)
        {
            n += kier;
            if (n < 0 || n >= rzedy.Length) return;
            if (rzedy[n] != null && rzedy[n].towar.Length > 0) break;
        }
        if (n == poziom) return;
        Odloz();
        var stary = rzedy[poziom]; var nowy = rzedy[n];
        // ten sam punkt wzdłuż regału, nie skok na początek
        float u = Mathf.Clamp01(stary.dlugosc > .01f ? przesuniecie / stary.dlugosc : .5f);
        poziom = n;
        przesuniecie = Mathf.Clamp(u * nowy.dlugosc, Mathf.Min(SzerokoscKadru * .5f, nowy.dlugosc * .5f),
                                   Mathf.Max(SzerokoscKadru * .5f, nowy.dlugosc - SzerokoscKadru * .5f));
    }

    // ---------------- wybór ----------------

    void Wskaz(Vector2 ekran)
    {
        if (!gracz || !gracz.view) return;
        var r = rzedy[poziom];
        var promien = gracz.view.ScreenPointToRay(ekran);
        int naj = -1; float najD = PromienDotyku;
        for (int i = 0; i < r.towar.Length; i++)
        {
            var t = r.towar[i]; if (!t) continue;
            Vector3 do_ = t.position - promien.origin;
            float wzdluzPromienia = Vector3.Dot(do_, promien.direction);
            if (wzdluzPromienia < .05f) continue;
            float d = Vector3.Distance(do_, promien.direction * wzdluzPromienia);
            if (d < najD) { najD = d; naj = i; }
        }
        if (naj < 0) { Odloz(); wybrany = -1; return; }
        if (naj == wybrany) { Wez(); return; }       // drugie dotknięcie bierze
        Odloz();
        wybrany = naj;
        var w = r.towar[naj];
        wybranyDom = w.position;
        w.position = wybranyDom + r.przod * Wysuniecie + Vector3.up * .012f;
    }

    void Odloz()
    {
        if (wybrany < 0) return;
        var r = rzedy[poziom];
        if (wybrany < r.towar.Length && r.towar[wybrany]) r.towar[wybrany].position = wybranyDom;
        wybrany = -1;
    }

    void Wez()
    {
        if (wybrany < 0) return;
        var r = rzedy[poziom];
        koszyk.Add(Nazwa(r, wybrany));
        Debug.Log("BRAX_POLKA wzięto " + Nazwa(r, wybrany) + " (razem " + koszyk.Count + ")");
        Odloz();
    }

    static string Nazwa(Rzad r, int i) => r.nazwy != null && i < r.nazwy.Length && !string.IsNullOrEmpty(r.nazwy[i]) ? r.nazwy[i] : "produkt";

    // ---------------- ekran ----------------

    Rect rWez, rWyjdz;
    GUIStyle stylTekst, stylPrzycisk;

    bool WPrzycisku(Vector2 p)
    {
        var g = new Vector2(p.x, Screen.height - p.y);
        return rWez.Contains(g) || rWyjdz.Contains(g);
    }

    void OnGUI()
    {
        if (!wTrybie || !Dobry) return;
        float s = Mathf.Min(Screen.width, Screen.height), m = s * .035f;
        if (stylTekst == null)
        {
            stylTekst = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .036f), wordWrap = false };
            stylPrzycisk = new GUIStyle(stylTekst) { fontSize = Mathf.RoundToInt(s * .040f) };
        }
        var r = rzedy[poziom];
        float szer = s * .40f, wys = s * .105f;
        rWez = new Rect(Screen.width * .5f - szer - m * .5f, Screen.height - wys - m, szer, wys);
        rWyjdz = new Rect(Screen.width * .5f + m * .5f, Screen.height - wys - m, szer, wys);

        var stary = GUI.color;
        // nazwa wybranego produktu
        if (wybrany >= 0)
        {
            var rn = new Rect(Screen.width * .5f - s * .46f, m, s * .92f, wys);
            GUI.color = new Color(0f, 0f, 0f, .55f); GUI.DrawTexture(rn, Texture2D.whiteTexture);
            GUI.color = Color.white; stylTekst.normal.textColor = Color.white;
            GUI.Label(rn, Nazwa(r, wybrany), stylTekst);
        }
        // licznik wziętych
        if (koszyk.Count > 0)
        {
            var rk = new Rect(Screen.width - s * .30f - m, m, s * .30f, wys * .8f);
            GUI.color = new Color(0f, 0f, 0f, .45f); GUI.DrawTexture(rk, Texture2D.whiteTexture);
            GUI.color = Color.white; stylTekst.normal.textColor = Color.white;
            GUI.Label(rk, "WZIĘTE: " + koszyk.Count, stylTekst);
        }
        // który poziom półki
        if (rzedy.Length > 1)
        {
            var rp = new Rect(m, m, s * .26f, wys * .8f);
            GUI.color = new Color(0f, 0f, 0f, .45f); GUI.DrawTexture(rp, Texture2D.whiteTexture);
            GUI.color = Color.white; stylTekst.normal.textColor = Color.white;
            GUI.Label(rp, "PÓŁKA " + (poziom + 1) + "/" + rzedy.Length, stylTekst);
        }

        GUI.color = wybrany >= 0 ? new Color(.16f, .55f, .24f, .92f) : new Color(.25f, .25f, .25f, .6f);
        GUI.DrawTexture(rWez, Texture2D.whiteTexture);
        GUI.color = Color.white; stylPrzycisk.normal.textColor = Color.white;
        GUI.Label(rWez, wybrany >= 0 ? "WEŹ" : "DOTKNIJ TOWAR", stylPrzycisk);
        if (wybrany >= 0 && Event.current.type == EventType.MouseUp && rWez.Contains(Event.current.mousePosition)) { Wez(); Event.current.Use(); }

        GUI.color = new Color(.35f, .12f, .12f, .92f); GUI.DrawTexture(rWyjdz, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(rWyjdz, "WYJDŹ", stylPrzycisk);
        if (Event.current.type == EventType.MouseUp && rWyjdz.Contains(Event.current.mousePosition)) { Wyjdz(); Event.current.Use(); }
        GUI.color = stary;
    }
}
