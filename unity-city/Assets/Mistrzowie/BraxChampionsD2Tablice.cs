using System;
using UnityEngine;

// D2 station screens (owner 10.11 21:22 for every screen of the region: "monitorki mają ... wyświetlać ... wszystko ma być
// funkcjonalne"): LED departure boards painted with the region's 5 x 7 LED font (BraxChampionsPiksele), from the phone's
// clock and the running train (BraxChampionsD2Pociag):
//   - ODJAZDY (hall, waiting rooms): the next six trains - time, destination, platform, remark; the first row is the train
//     that is in / coming into the station;
//   - PERON 1 / PERON 2 (under the canopies, both faces): the train at this platform and its departure countdown, or the next;
//   - AUTOBUSY (bus island, terminal): eight stands, line, destination, minutes to departure.
// One texture per kind (shared), repainted once a second while the player is within 300 m.
public sealed class BraxChampionsD2Tablice : MonoBehaviour
{
    public Renderer[] odjazdy = new Renderer[0];
    public Renderer[] peron1 = new Renderer[0], peron2 = new Renderer[0];
    public Renderer[] autobusy = new Renderer[0];
    public BraxChampionsD2Pociag pociag;

    public const int OW = 320, OH = 128, PW = 256, PH = 64, AW = 320, AH = 128;
    public static readonly string[] Kierunki = { "KRAKÓW GŁÓWNY", "WARSZAWA CENTRALNA", "GDAŃSK GŁÓWNY", "WROCŁAW GŁÓWNY", "POZNAŃ GŁÓWNY", "KATOWICE", "ŁÓDŹ FABRYCZNA", "SZCZECIN GŁÓWNY" };
    public static readonly string[] Linie = { "101 OSIEDLE PÓŁNOC", "102 ARENA MISTRZÓW", "103 PORT / MARINA", "104 PLAŻA / PROMENADA", "105 CENTRUM", "106 HOTELE", "N1 LOTNISKO", "E1 EKSPRES MIASTO" };
    static readonly Color32 Bursztyn = new Color32(255, 176, 40, 255), Ciemny = new Color32(4, 6, 12, 255), Pasek = new Color32(20, 34, 80, 255);

    sealed class Ekran { public Texture2D tex; public Color32[] px; }
    Ekran eo, ep1, ep2, ea; float nastepny, sprawdz; bool blisko = true;

    void Start()
    {
        eo = Nowy(OW, OH, odjazdy, "D2 odjazdy"); ep1 = Nowy(PW, PH, peron1, "D2 peron 1"); ep2 = Nowy(PW, PH, peron2, "D2 peron 2"); ea = Nowy(AW, AH, autobusy, "D2 autobusy");
        Odswiez();
    }
    static Ekran Nowy(int w, int h, Renderer[] na, string nazwa)
    {
        if (na == null || na.Length == 0) return null;
        Material baza = null; foreach (var r in na) if (r) { baza = r.sharedMaterial; break; }
        if (!baza) return null;
        var e = new Ekran { tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = nazwa, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }, px = new Color32[w * h] };
        var m = new Material(baza) { name = nazwa };
        m.SetTexture("_MainTex", e.tex); m.SetTexture("_EmissionMap", e.tex);
        foreach (var r in na) if (r) r.sharedMaterial = m;
        return e;
    }
    static void Zapisz(Ekran e) { e.tex.SetPixels32(e.px); e.tex.Apply(false); }

    void Update()
    {
        if (Time.unscaledTime >= sprawdz)
        {
            sprawdz = Time.unscaledTime + .5f;
            var cam = Camera.main;
            blisko = !cam || (cam.transform.position - transform.position).sqrMagnitude < 300f * 300f;
        }
        if (!blisko || Time.unscaledTime < nastepny) return;
        nastepny = Time.unscaledTime + 1f;
        Odswiez();
    }
    void Odswiez()
    {
        var now = DateTime.Now;
        int tor = pociag ? pociag.Tor : 0, kurs = pociag ? pociag.Kurs : 0;
        bool stoi = pociag && pociag.Stoi, wjezdza = pociag && pociag.Wjezdza;
        float doOdj = pociag ? pociag.DoOdjazdu : 120f;
        if (eo != null) { Odjazdy(eo.px, now, tor, kurs, doOdj, stoi, wjezdza); Zapisz(eo); }
        if (ep1 != null) { Peron(ep1.px, 0, now, tor, kurs, doOdj, stoi, wjezdza); Zapisz(ep1); }
        if (ep2 != null) { Peron(ep2.px, 1, now, tor, kurs, doOdj, stoi, wjezdza); Zapisz(ep2); }
        if (ea != null) { Autobusy(ea.px, now); Zapisz(ea); }
    }

    static string Hm(DateTime t) => t.Hour.ToString("00") + ":" + t.Minute.ToString("00");
    static void T(Color32[] px, int w, int h, string s, int x, int yTop, int sk, Color32 c) => BraxChampionsPiksele.Tekst(px, w, h, s, x, h - yTop - 7 * sk, sk, c);

    // the hall board: header, six rows (time, destination, platform, remark)
    public static void Odjazdy(Color32[] px, DateTime now, int tor, int kurs, float doOdj, bool stoi, bool wjezdza)
    {
        int w = OW, h = OH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 16, w, h, Pasek);
        T(px, w, h, "ODJAZDY · DEPARTURES", 4, 4, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, Hm(now) + ":" + now.Second.ToString("00"), w - 52, 4, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, "GODZ", 4, 19, 1, BraxChampionsPiksele.Cyjan); T(px, w, h, "KIERUNEK", 40, 19, 1, BraxChampionsPiksele.Cyjan);
        T(px, w, h, "PERON", 190, 19, 1, BraxChampionsPiksele.Cyjan); T(px, w, h, "UWAGI", 232, 19, 1, BraxChampionsPiksele.Cyjan);
        var first = now.AddSeconds(Mathf.Max(0f, doOdj));
        for (int i = 0; i < 6; i++)
        {
            var t = i == 0 ? first : first.AddMinutes(6 + i * 7 - (first.Minute % 5));
            int k = (kurs + i) % Kierunki.Length, peron = i == 0 ? tor + 1 : ((tor + i) % 2) + 1;
            string uw = i == 0 ? (stoi ? "ODJAZD " + Mathf.CeilToInt(doOdj) + " S" : wjezdza ? "WJEŻDŻA" : "NA CZAS") : (i == 3 ? "+5 MIN" : "NA CZAS");
            int y = 30 + i * 16; if (i % 2 == 1) BraxChampionsPiksele.Prost(px, w, h, 0, h - y - 12, w, h - y + 3, new Color32(10, 14, 26, 255));
            var c = i == 0 && stoi ? BraxChampionsPiksele.Zielony : Bursztyn;
            T(px, w, h, Hm(t), 4, y, 1, c); T(px, w, h, Kierunki[k], 40, y, 1, c);
            T(px, w, h, peron.ToString(), 202, y, 1, c); T(px, w, h, uw, 232, y, 1, i == 3 ? BraxChampionsPiksele.Czerwony : c);
        }
    }
    // a platform display: the platform number, the train and its countdown, or the next one
    public static void Peron(Color32[] px, int p, DateTime now, int tor, int kurs, float doOdj, bool stoi, bool wjezdza)
    {
        int w = PW, h = PH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, 0, 30, h, Pasek);
        T(px, w, h, (p + 1).ToString(), 6, 18, 3, BraxChampionsPiksele.Bialy);
        bool tu = tor == p && (stoi || wjezdza);
        int k = tu ? kurs % Kierunki.Length : (kurs + 1) % Kierunki.Length;
        var t = now.AddSeconds(tu ? doOdj : doOdj + 140f);
        T(px, w, h, Hm(t), 36, 6, 2, tu && stoi ? BraxChampionsPiksele.Zielony : Bursztyn);
        T(px, w, h, Kierunki[k], 36, 26, 1, BraxChampionsPiksele.Bialy);
        string s = tu ? (stoi ? "ODJAZD ZA " + (int)(doOdj / 60f) + ":" + ((int)doOdj % 60).ToString("00") : "POCIĄG WJEŻDŻA") : "NASTĘPNY POCIĄG";
        T(px, w, h, s, 36, 42, 1, tu ? BraxChampionsPiksele.Zielony : BraxChampionsPiksele.Cyjan);
        T(px, w, h, Hm(now), w - 34, 6, 1, BraxChampionsPiksele.Bialy);
    }
    // the bus board: stands 1-8, line + destination, minutes
    public static void Autobusy(Color32[] px, DateTime now)
    {
        int w = AW, h = AH; BraxChampionsPiksele.Wypelnij(px, Ciemny);
        BraxChampionsPiksele.Prost(px, w, h, 0, h - 16, w, h, Pasek);
        T(px, w, h, "AUTOBUSY · ODJAZDY", 4, 4, 1, BraxChampionsPiksele.Bialy);
        T(px, w, h, Hm(now), w - 34, 4, 1, BraxChampionsPiksele.Bialy);
        int minute = now.Hour * 60 + now.Minute;
        for (int i = 0; i < 8; i++)
        {
            int co = 8 + i * 2, za = co - (minute + i * 3) % co;
            int y = 20 + i * 13;
            T(px, w, h, (i + 1).ToString(), 4, y, 1, BraxChampionsPiksele.Cyjan);
            T(px, w, h, Linie[i], 18, y, 1, Bursztyn);
            T(px, w, h, za <= 1 ? ">>>" : za + " MIN", w - 46, y, 1, za <= 1 ? BraxChampionsPiksele.Zielony : Bursztyn);
        }
    }
}
