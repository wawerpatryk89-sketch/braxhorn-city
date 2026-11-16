// The casinos' slot machines (runtime BraxChampionsAutomaty and its rules) compile in the tower and casino previews: the
// game's runtime pieces they touch, as stand-ins (only the generator runs in the preview; the game's spins are checked by
// automaty_sim in a dotnet run of the same files).
using System.Collections.Generic;
using UnityEngine;
public interface IBraxInterakcja { string Kontekst(Vector3 gracz); bool Wykonaj(Vector3 gracz); }
public static class BraxInterakcje
{
    public static readonly List<IBraxInterakcja> Lista = new List<IBraxInterakcja>();
    public static void Dodaj(IBraxInterakcja d) { if (!Lista.Contains(d)) Lista.Add(d); }
    public static void Usun(IBraxInterakcja d) { Lista.Remove(d); }
}
public sealed class BraxGymPlayer : MonoBehaviour { }
public static class BraxChampionsGry
{
    public static BraxGymPlayer Gracz; public static string Panel = "", Blysk = "";
    public static void Ustaw(string p) { Panel = p; }
    public static void Pokaz(string t, float s = 2f) { Blysk = t; }
}
namespace UnityEngine
{
    public static class PlayerPrefs
    {
        static readonly Dictionary<string, string> d = new Dictionary<string, string>();
        public static int GetInt(string k, int def = 0) => d.TryGetValue(k, out var v) && int.TryParse(v, out var i) ? i : def;
        public static void SetInt(string k, int v) { d[k] = v.ToString(); }
        public static string GetString(string k, string def = "") => d.TryGetValue(k, out var v) ? v : def;
        public static void SetString(string k, string v) { d[k] = v; }
        public static void Save() { }
        public static void DeleteAll() { d.Clear(); }
    }
}
