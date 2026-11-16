using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// MIASTO R1 — BEZ AUT I BEZ LUDZI (właściciel 16.11.2026 14:56: „Usuwasz wszystkie pojazdy (samochody) z gry i ludzi! Wszystkie te
// w powietrzu, na drzewach, w lesie też! Bo ich nie znalazłeś”; o autobusie: „mamy nowy ze środkiem, powinien być w repo, go
// bierzemy, nie tego MAN-a”).
//  • Kroki „ruch aut miasta (B)” i „ludzie miasta (B)” NIE są wykonywane (kod zostaje — wraca po decyzji właściciela). Z nimi znika:
//    ruch aut, auta zaparkowane (ulice, parking supermarketu, warsztaty, fawela), motocykle na parkingu, autobus MAN, przechodnie,
//    obsada lokali i recepcji, ludzie na ławkach, ludzie wyspy (kibice, plaża, bar, bramki) i faweli.
//  • Zamiatanie sceny (koniec kroku rejonu i drugi raz po supermarkecie): KAŻDY obiekt z autem (BraxAuto), sterownikiem ruchu
//    (BraxRuchAut), osobą (IBraxOsoba: pieszy, mieszkaniec) albo postacią ze szkieletem człowieka (Animator Humanoid; Animation +
//    skóra z kośćmi bioder) — usunięty, z listą: co i gdzie (log BRAXHORN_BEZ_AUT_I_LUDZI).
//  • ZOSTAJĄ: gracz (BraxGymPlayer) i zawodnicy areny z postaci globalnych właściciela (BraxArenaWalka / BraxArenaPokaz — jego
//    postacie, nie ludzie miasta), manekiny, posągi, figury; łodzie, statki, tramwaj (nie samochody); stoiska food truck na wyspie.
//  • Nowy autobus (BraxhornCityBus12 1.0.1, zatwierdzony 15.11, z wnętrzem: 33 miejsca, kierowca, drzwi i koła z osiami) — w repo
//    jest tylko jego karta (AssetLibrary/Unique/Vehicles/BraxhornCityBus12/manifest.json na main); plik modelu leży w prywatnej
//    bibliotece właściciela (BraxhornCityBus12.unitypackage). Wejdzie, gdy plik trafi do zaszyfrowanej paczki.
public static class BraxBezAutILudzi
{
    public static readonly bool Wlaczone = true;   // readonly (nie const) — bez ostrzeżeń „kod nieosiągalny” w logu buildu

    static readonly string[] Zostaja = { "manekin", "mannequin", "posag", "pomnik", "statua", "rzezba", "figura", "plakat", "mural" };
    static readonly string[] Biodra = { "hips", "mixamorig:hips", "mixamorig_hips", "bip01", "bip01 pelvis", "pelvis", "cc_base_hip" };

    static bool ChronionyGracz(Transform t)
    {
        for (var p = t; p; p = p.parent)
        {
            if (p.GetComponent<BraxGymPlayer>()) return true;
            if (p.GetComponent<BraxArenaWalka>() || p.GetComponent<BraxArenaPokaz>()) return true;
            string n = p.name.ToLowerInvariant();
            if (n == "braxhornplayer") return true;
            if (Zostaja.Any(n.Contains)) return true;
        }
        return false;
    }

    static bool Czlowiek(GameObject g)
    {
        var an = g.GetComponent<Animator>();
        if (an && an.avatar && an.avatar.isHuman) return true;
        if (g.GetComponent<Animation>() || an)
            foreach (var s in g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (s && s.bones != null && s.bones.Any(b => b && Biodra.Contains(b.name.ToLowerInvariant()))) return true;
        return false;
    }

    static string Gdzie(Transform t) => t.position.ToString("F1");

    /// <summary>Usuwa z całej sceny auta, sterowniki ruchu i ludzi (poza graczem i zawodnikami areny). Zwraca liczbę usuniętych.</summary>
    public static int Usun(Scene scena, string etap)
    {
        if (!Wlaczone || !scena.IsValid()) return 0;
        var auta = new List<string>(); var ludzie = new List<string>(); var inne = new List<string>();
        var doUsuniecia = new List<GameObject>();
        void Dodaj(GameObject g, List<string> lista, string opis)
        {
            if (!g || doUsuniecia.Contains(g) || ChronionyGracz(g.transform)) return;
            // nie usuwaj dwa razy w jednym poddrzewie (rodzic już na liście)
            for (var p = g.transform.parent; p; p = p.parent) if (doUsuniecia.Contains(p.gameObject)) return;
            doUsuniecia.RemoveAll(x => x && x.transform.IsChildOf(g.transform));
            doUsuniecia.Add(g); lista.Add(opis + " @" + Gdzie(g.transform));
        }
        var korzenie = scena.GetRootGameObjects();
        foreach (var r in korzenie)
        {
            if (!r) continue;
            // całe korzenie ruchu aut i ludzi miasta B (gdyby powstały)
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
                if (t.name == "CLAUDE miasto B: ruch aut" || t.name == "CLAUDE miasto B: ludzie") Dodaj(t.gameObject, inne, t.name);
            foreach (var a in r.GetComponentsInChildren<BraxAuto>(true)) if (a) Dodaj(a.gameObject, auta, a.name);
            foreach (var s in r.GetComponentsInChildren<BraxRuchAut>(true)) if (s) Dodaj(s.gameObject, inne, "sterownik ruchu aut: " + s.name);
            foreach (var mb in r.GetComponentsInChildren<MonoBehaviour>(true)) if (mb is IBraxOsoba) Dodaj(mb.gameObject, ludzie, mb.name);
            foreach (var an in r.GetComponentsInChildren<Animator>(true)) if (an && Czlowiek(an.gameObject)) Dodaj(an.gameObject, ludzie, an.name);
            foreach (var am in r.GetComponentsInChildren<Animation>(true)) if (am && Czlowiek(am.gameObject)) Dodaj(am.gameObject, ludzie, am.name);
        }
        foreach (var g in doUsuniecia) if (g) UnityEngine.Object.DestroyImmediate(g);
        var sb = new StringBuilder("BRAXHORN_BEZ_AUT_I_LUDZI " + etap + ": auta=" + auta.Count + " ludzie=" + ludzie.Count + " korzenie/sterowniki=" + inne.Count +
                                   " (decyzja właściciela 16.11 14:56; zostają: gracz, zawodnicy areny z postaci globalnych, manekiny, łodzie, tramwaj)");
        foreach (var x in auta.Take(60)) sb.Append(" | AUTO ").Append(x);
        foreach (var x in ludzie.Take(60)) sb.Append(" | CZŁOWIEK ").Append(x);
        foreach (var x in inne.Take(20)) sb.Append(" | ").Append(x);
        Debug.Log(sb.ToString());
        try { BraxRegion.Note("bez aut i ludzi (" + etap + "): usunięte auta " + auta.Count + ", ludzie " + ludzie.Count + ", korzenie/sterowniki " + inne.Count); } catch (Exception) { }
        return doUsuniecia.Count;
    }
}
