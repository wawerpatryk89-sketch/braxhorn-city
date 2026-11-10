using UnityEngine;

// Owner 10.11 20:45 ("rób to funkcjonalne, żeby było, a nie byle co"): the games of E1 Centrum Rozrywki - bowling
// (BraxChampionsKregle) and billiards (BraxChampionsBilard) - share this small state: the result panel at the top of the
// screen while the player is at a lane / table, the big flash (STRIKE!, SPARE!, WBITA!). The screens' pixel font and
// painters live in BraxChampionsPiksele.
// The panel draws like the estate's sport panel (BraxSportHud, R1): small, top-centre, only while playing.
public static class BraxChampionsGry
{
    public static string Panel = "";
    public static float PanelDo;                       // the panel shows until this time (refreshed by the game)
    public static string Blysk = ""; public static float BlyskDo;
    public static void Pokaz(string tekst, float sekundy = 2f) { Blysk = tekst; BlyskDo = Time.unscaledTime + sekundy; UpewnijHud(); }
    public static void Ustaw(string panel) { Panel = panel; PanelDo = Time.unscaledTime + .6f; UpewnijHud(); }

    static BraxChampionsGryHud hud;
    static void UpewnijHud() { if (!hud) hud = new GameObject("E1 gry: panel wyniku").AddComponent<BraxChampionsGryHud>(); }

    static BraxGymPlayer gracz; static float szukaj;
    public static BraxGymPlayer Gracz
    {
        get
        {
            if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + 1f; gracz = Object.FindAnyObjectByType<BraxGymPlayer>(); }
            return gracz;
        }
    }
    public static Vector3 Przod(BraxGymPlayer g)
    {
        var t = g.visual ? g.visual : g.transform; var f = t.forward; f.y = 0f;
        return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
    }
}
