using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// SZKŁO FASAD (9.11, właściciel z tarasu Tower: „czarne bryły budynków”, „każdy budynek w nowej części miasta ma być premium”).
// Materiały shadera Braxhorn/SzkloFasady (odbicie prawdziwego nieba, pokoje za szybą, słupki i pasy kondygnacji, mgiełka).
// Jeden materiał na zestaw (moduł, kondygnacja, poziom 0, barwa) — zapisany jako asset. Parametry tła nieba (tekstura,
// słońce, mgiełka) dostają w kroku horyzontu (BraxRegionHoryzont → UstawTloSzkla), kiedy niebo jest już zbudowane.
public static class BraxSzkloFasady
{
    public const string ShaderName = "Braxhorn/SzkloFasady";
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    public static Material Mat(string nazwa, float modul, float kondygnacja, float poziom0, Color barwa, float wnetrze = .55f, float swiatla = .35f, Material zapasowy = null)
    {
        if (cache.TryGetValue(nazwa, out var m) && m) return m;
        var sh = Shader.Find(ShaderName);
        if (!sh) { Debug.LogWarning("DO POPRAWKI: BRAXHORN_SZKLO brak shadera " + ShaderName + " — " + nazwa + " zostaje na materiale zapasowym"); return zapasowy; }
        m = new Material(sh) { name = "Szkło fasady: " + nazwa };
        m.SetColor("_Tint", barwa); m.SetFloat("_CellW", modul); m.SetFloat("_CellH", kondygnacja); m.SetFloat("_Floor0", poziom0);
        m.SetFloat("_Depth", 5f); m.SetFloat("_Wnetrze", wnetrze); m.SetFloat("_Swiatla", swiatla);
        m.SetFloat("_Rama", .08f); m.SetFloat("_Pas", Mathf.Min(.75f, kondygnacja * .2f));
        Directory.CreateDirectory(BraxRegion.Out + "/Materials");
        BraxZapisAssetu.Utworz(m, BraxRegion.Out + "/Materials/szklo_" + BraxUliceKit.Bezpieczna(nazwa) + ".mat");
        cache[nazwa] = m;
        return m;
    }

    /// <summary>Wszystkie materiały szkła fasad w scenie dostają tło nieba (wołane przez krok horyzontu po zbudowaniu nieba).</summary>
    public static int UstawTlo(System.Action<Material> ustaw)
    {
        var zrobione = new HashSet<Material>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
                if (m && m.shader && m.shader.name == ShaderName && zrobione.Add(m)) { ustaw(m); EditorUtility.SetDirty(m); }
        foreach (var m in cache.Values) if (m && zrobione.Add(m)) { ustaw(m); EditorUtility.SetDirty(m); }
        Debug.Log("BRAXHORN_SZKLO tło nieba w materiałach szkła fasad: " + zrobione.Count);
        return zrobione.Count;
    }
}
