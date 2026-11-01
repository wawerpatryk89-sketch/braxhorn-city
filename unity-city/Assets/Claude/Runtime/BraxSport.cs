using System;
using UnityEngine;

// BRAXHORN — rozrywka na osiedlu (właściciel 1.11, 18:16: „boiska mają być użytkowe; później misje”).
// Prosto: kosz = podnieś, kozłuj, rzuć; piłka nożna = popchnij, kopnij, podaj; siatkówka = odbicie przez siatkę;
// tenis = return piłek z wyrzutni. Bez sędziów, fauli i drużyn. Rozgrywkę prowadzą boiska (BraxSportBoisko),
// ruchy postaci BraxRuchySport, panel wyniku BraxSportHud.
// Pod misje: zdarzenia i stan wyniku poniżej (misja podpina się pod OnPunkt / OnGol / OnRet / OnKoniecAkcji).
public enum BraxRuch { Brak, Rzut, OdbicieDolne, OdbicieGorne, Forhend, Bekhend, Podanie }

public static class BraxSport
{
    // ---- zdarzenia dla misji ----
    public static event Action<string, int> OnPunkt;        // gra ("KOSZ", "PIŁKA", "SIATKÓWKA", "TENIS"), punkty za zagranie
    public static event Action<int> OnGol;                  // łączna liczba goli
    public static event Action<int> OnRet;                  // łączna liczba udanych returnów (tenis)
    public static event Action<int> OnKoniecAkcji;          // siatkówka: długość zakończonego ciągu odbić

    // ---- stan wyniku (od uruchomienia gry) ----
    public static int KoszPunkty, KoszTrafione, KoszProby, Gole, Returny, SiatkaCiag, SiatkaNajlepszy;

    // ---- stan dla ruchów postaci i panelu ----
    public static BraxSportBoisko Aktywne;                  // boisko, na którym stoi gracz (null = poza boiskami)
    public static Transform PilkaWRekach;                   // trzymana piłka (kosz) — ręce celują w nią
    public static bool Kozluje;                             // piłka kozłowana prawą ręką
    public static float RzutFaza;                           // 0..1 — piłka wędruje nad głowę przed rzutem
    public static bool Rakieta;                             // gracz na korcie — rakieta w prawej dłoni
    public static string Blysk = ""; public static float BlyskDo;

    static BraxGymPlayer gracz; static float szukaj;
    public static BraxGymPlayer Gracz
    {
        get
        {
            if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + 1f; gracz = UnityEngine.Object.FindAnyObjectByType<BraxGymPlayer>(); }
            return gracz;
        }
    }

    public static string Gra => Aktywne ? Aktywne.Nazwa : "";
    public static string Wynik => Aktywne ? Aktywne.Panel : "";

    // Ruch do zagrania w chwili startu ciosu (BraxRuchySport pyta boisko, na którym stoi gracz).
    public static BraxRuch Zamiar(Vector3 p, Vector3 przod)
    {
        if (Aktywne) { var z = Aktywne.Zamiar(p, przod); if (z != BraxRuch.Brak) return z; }
        foreach (var b in BraxSportBoisko.Wszystkie) if (b && b != Aktywne && b.Na) { var z = b.Zamiar(p, przod); if (z != BraxRuch.Brak) return z; }   // nakładające się boiska
        return BraxRuch.Brak;
    }

    public static void Pokaz(string tekst, float sekundy = 2f) { Blysk = tekst; BlyskDo = Time.unscaledTime + sekundy; }

    internal static void Punkt(string gra, int pkt) { OnPunkt?.Invoke(gra, pkt); }
    internal static void Gol() { Gole++; Pokaz("GOL!"); OnGol?.Invoke(Gole); OnPunkt?.Invoke("PIŁKA", 1); }
    internal static void Ret() { Returny++; Pokaz("RETURN · " + Returny, 1.2f); OnRet?.Invoke(Returny); OnPunkt?.Invoke("TENIS", 1); }
    internal static void KoniecAkcji(int ciag)
    {
        SiatkaNajlepszy = Mathf.Max(SiatkaNajlepszy, ciag);
        Pokaz("KONIEC AKCJI · " + ciag + (ciag == 1 ? " ODBICIE" : ciag >= 2 && ciag <= 4 ? " ODBICIA" : " ODBIĆ"), 2.2f);
        OnKoniecAkcji?.Invoke(ciag);
    }

    static BraxSportHud hud;
    internal static void UpewnijHud() { if (!hud) hud = new GameObject("BRAXHORN sport: panel wyniku").AddComponent<BraxSportHud>(); }

    // ---------------- tekstury piłek malowane w kodzie (jedna na rodzaj, 128 × 64, bez emisji) ----------------
    static Texture2D[] tekstury = new Texture2D[4];
    public static Texture2D Tekstura(int rodzaj)   // 0 kosz, 1 nożna, 2 siatkówka, 3 tenis
    {
        if (tekstury[rodzaj]) return tekstury[rodzaj];
        const int W = 128, H = 64;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "piłka " + rodzaj };
        var px = new Color32[W * H];
        var rnd = new System.Random(77 + rodzaj);
        // wierzchołki dwudziestościanu (łaty piłki nożnej)
        float g = (1f + Mathf.Sqrt(5f)) * .5f;
        var ico = new[] { new Vector3(-1, g, 0), new Vector3(1, g, 0), new Vector3(-1, -g, 0), new Vector3(1, -g, 0), new Vector3(0, -1, g), new Vector3(0, 1, g),
                          new Vector3(0, -1, -g), new Vector3(0, 1, -g), new Vector3(g, 0, -1), new Vector3(g, 0, 1), new Vector3(-g, 0, -1), new Vector3(-g, 0, 1) };
        for (int i = 0; i < ico.Length; i++) ico[i] = ico[i].normalized;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + .5f) / W, v = (y + .5f) / H;
                float lon = u * Mathf.PI * 2f, lat = (v - .5f) * Mathf.PI;
                var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon));
                float n = (float)rnd.NextDouble();
                Color c;
                switch (rodzaj)
                {
                    case 0:   // kosz: pomarańcz z fakturą, czarne szwy (dwa koła wielkie + dwa łuki)
                        {
                            c = new Color(.80f, .34f, .09f) * (.9f + .1f * n);
                            float s1 = Mathf.Abs(d.x), s2 = Mathf.Abs(d.y), s3 = Mathf.Abs(Mathf.Abs(d.z) - .62f - .25f * d.x * d.x);
                            if (s1 < .035f || s2 < .035f || s3 < .03f) c = new Color(.05f, .04f, .04f);
                        }
                        break;
                    case 1:   // nożna: biała, czarne łaty (pięciokąty w wierzchołkach dwudziestościanu), szare szwy
                        {
                            float best = -1f, second = -1f;
                            foreach (var a in ico) { float q = Vector3.Dot(d, a); if (q > best) { second = best; best = q; } else if (q > second) second = q; }
                            c = new Color(.95f, .95f, .93f) * (.96f + .04f * n);
                            if (best > .93f) c = new Color(.07f, .07f, .08f);
                            else if (best - second < .012f || Mathf.Abs(best - .93f) < .006f) c = new Color(.55f, .55f, .55f);
                        }
                        break;
                    case 2:   // siatkówka: na każdej ścianie sześcianu trzy pasy (biały, niebieski, żółty), ciemne szwy
                        {
                            var ad = new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z));
                            int f = ad.x > ad.y && ad.x > ad.z ? 0 : ad.y > ad.z ? 1 : 2;
                            float main = f == 0 ? d.x : f == 1 ? d.y : d.z, other = f == 0 ? d.y : f == 1 ? d.z : d.x;
                            float tt = other / Mathf.Abs(main);                      // -1..1
                            int pas = Mathf.Clamp(Mathf.FloorToInt((tt + 1f) * 1.5f), 0, 2);
                            var kol = new[] { new Color(.95f, .95f, .92f), new Color(.10f, .28f, .70f), new Color(.98f, .80f, .10f) };
                            c = kol[(pas + f) % 3] * (.95f + .05f * n);
                            float brzeg = Mathf.Abs(Mathf.Repeat((tt + 1f) * 1.5f + .5f, 1f) - .5f);
                            if (brzeg > .47f || Mathf.Abs(Mathf.Abs(tt) - 1f) < .02f) c *= .55f;
                        }
                        break;
                    default:  // tenis: żółto-zielony filc, biały szew (krzywa „S” dookoła piłki)
                        {
                            c = new Color(.78f, .90f, .18f) * (.88f + .12f * n);
                            float szew = Mathf.Abs(d.y - .55f * Mathf.Cos(2f * Mathf.Atan2(d.z, d.x)) * Mathf.Sqrt(Mathf.Max(0f, 1f - d.y * d.y)));
                            if (szew < .045f) c = new Color(.95f, .96f, .92f);
                        }
                        break;
                }
                c.a = 1f; px[y * W + x] = c;
            }
        t.SetPixels32(px); t.Apply(true, true);
        return tekstury[rodzaj] = t;
    }

    // Materiał z cieniowaniem Standard (shader wzięty z materiału prymitywu — zawsze jest w buildzie). Bez emisji.
    public static Material Mat(Renderer wzor, Color kolor, Texture tex, float gladkosc)
    {
        var m = new Material(wzor.sharedMaterial) { color = kolor, mainTexture = tex };
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", gladkosc);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        m.DisableKeyword("_EMISSION");
        return m;
    }
}

// Panel wyniku: mały, góra-środek, tylko na boisku / korcie (styl przycisków BraxTouchControls).
public sealed class BraxSportHud : MonoBehaviour
{
    GUIStyle lab, big; float rozmiar;
    static readonly Color Ink = new Color(1f, 1f, 1f, .92f), Glass = new Color(.06f, .07f, .07f, .5f), Accent = new Color(.55f, 1f, .2f, .95f);

    void OnGUI()
    {
        var b = BraxSport.Aktywne;
        bool blysk = Time.unscaledTime < BraxSport.BlyskDo && !string.IsNullOrEmpty(BraxSport.Blysk);
        if (!b && !blysk) return;
        float s = Mathf.Min(Screen.width, Screen.height);
        if (lab == null || rozmiar != s)
        {
            rozmiar = s;
            lab = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(s * .034f), wordWrap = false };
            big = new GUIStyle(lab) { fontSize = Mathf.RoundToInt(s * .06f) };
        }
        var old = GUI.color;
        if (b)
        {
            string txt = b.Panel;
            float w = Mathf.Min(Screen.width - s * .1f, lab.CalcSize(new GUIContent(txt)).x + s * .08f), h = s * .07f;
            var r = new Rect((Screen.width - w) * .5f, s * .1f, w, h);
            GUI.color = Glass; GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Ink; lab.normal.textColor = Ink; GUI.Label(r, txt, lab);
        }
        if (blysk)
        {
            var r = new Rect(0, s * .18f, Screen.width, s * .1f);
            GUI.color = new Color(0, 0, 0, .55f); big.normal.textColor = new Color(0, 0, 0, .6f);
            GUI.Label(new Rect(r.x + s * .004f, r.y + s * .004f, r.width, r.height), BraxSport.Blysk, big);
            GUI.color = Accent; big.normal.textColor = Accent; GUI.Label(r, BraxSport.Blysk, big);
        }
        GUI.color = old;
    }
}
