using System.Collections.Generic;
using UnityEngine;

// WSPÓLNE DANE BUILDU 14 (2.11, dyspozytor) — jedno miejsce wymiany informacji między etapami generatora osiedla, żeby
// wykonawcy nie zgadywali nawzajem swoich pozycji:
//  • MiejscaParkingowe — wypełnia etap dróg/parkingów (zatoki z liniami i krawężnikiem), czyta etap aut (auta zaparkowane);
//  • Przejscia — wypełnia etap dróg (zebry), czytają auta (zatrzymanie) i przechodnie (przechodzenie przez jezdnię);
//  • SkrzyzowaniaInfo — wypełnia etap dróg (rondo / zwykłe / ze światłami), czyta etap aut (pierwszeństwo, łuki);
//  • WzrostBohatera / K — miara skali świata: k = H / 1.80 m (zasada właściciela: „miarą świata jest postać gracza”).
// Konwencja nazw dla innych etapów: w każdym aucie w ruchu dziecko „BRAXHORN fotel kierowcy” (pozycja i obrót siedzenia
// kierowcy) — etap ludzi sadza tam kierowcę; „BRAXHORN fotel pasażera” analogicznie.
public static partial class BraxClaudeOsiedle
{
    public struct MiejsceParkingowe { public Vector3 srodek; public Vector3 przod; public Vector2 rozmiar; public bool przyScianie; }
    public struct Przejscie { public Vector3 srodek; public Vector3 wzdluzJezdni; public float szerokoscJezdni; public bool zeSwiatlami; }
    public enum RodzajSkrzyzowania { Zwykle, Rondo, ZeSwiatlami }
    public struct SkrzyzowanieInfo { public Vector3 srodek; public float promien; public RodzajSkrzyzowania rodzaj; }

    public static readonly List<MiejsceParkingowe> MiejscaParkingowe = new List<MiejsceParkingowe>();
    public static readonly List<Przejscie> Przejscia = new List<Przejscie>();
    public static readonly List<SkrzyzowanieInfo> SkrzyzowaniaInfo = new List<SkrzyzowanieInfo>();

    // Bieżący gracz wyznacza skalę: Patryk 2.27 jednostki reprezentuje 1.80 m.
    public const float WzrostBraxa = 2.33f;
    public static float WzrostBohatera = WzrostBraxa;
    public static float K => WzrostBohatera / 1.80f;

    public const string FotelKierowcy = "BRAXHORN fotel kierowcy", FotelPasazera = "BRAXHORN fotel pasażera";

    // Wzrost postaci gracza z bounds rendererów (BraxGymPlayer w scenie). Wołać przed etapami, które skalują świat.
    public static float ZmierzBohatera()
    {
        var p = Object.FindAnyObjectByType<BraxGymPlayer>();
        if (p)
        {
            bool any = false; Bounds b = default;
            foreach (var r in p.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any && b.size.y > 1.2f && b.size.y < 3.2f) WzrostBohatera = b.size.y;
        }
        float gracz = WzrostBohatera;                                         // zmierzony gracz (tylko do logu)
        if (WzrostZModelu > 1.2f && WzrostZModelu < 3.2f) gracz = WzrostZModelu;
        WzrostBohatera = WzrostZModelu > 1.2f ? WzrostZModelu : WzrostBraxa; // Patryk zachowuje wielkość; 1.80 m jest miarą proporcji
        Debug.Log("BRAXHORN_SKALA wzrost bohatera " + WzrostBohatera.ToString("F2") + " jednostek (odpowiednik 1.80 m), k = " + K.ToString("F3") + ", gracz w scenie " + gracz.ToString("F2") + " m");
        return WzrostBohatera;
    }

    static void WyczyscWspolne14() { MiejscaParkingowe.Clear(); Przejscia.Clear(); SkrzyzowaniaInfo.Clear(); }
}
