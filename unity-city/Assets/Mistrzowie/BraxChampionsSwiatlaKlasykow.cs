using System.Collections.Generic;
using UnityEngine;

/// <summary>MIASTO MISTRZÓW - the classics' lamps at night (owner 25.11.2026 03:00: "światła ogólnie muszą świecić, bo
/// tam będzie też noc"). Each classic's body material carries its light map as the emission (headlamps white, rear
/// lamps red), black by day: the shader skips a black emission. When the game's night comes it sets Noc = true and
/// every classic's lamps glow (a property block per renderer: the shared material stays as it is).</summary>
public sealed class BraxChampionsSwiatlaKlasykow : MonoBehaviour
{
    public Renderer[] rendery;
    [ColorUsage(false, true)] public Color swiatlo = new Color(2.4f, 2.3f, 2.1f, 1f);

    static readonly List<BraxChampionsSwiatlaKlasykow> wszystkie = new List<BraxChampionsSwiatlaKlasykow>();
    static bool noc;
    static readonly int Emisja = Shader.PropertyToID("_EmissionColor");
    MaterialPropertyBlock blok;

    /// <summary>The night: the lamps of every classic on (true) or off.</summary>
    public static bool Noc
    {
        get => noc;
        set
        {
            if (noc == value) return;
            noc = value;
            for (int i = wszystkie.Count - 1; i >= 0; i--) if (wszystkie[i]) wszystkie[i].Ustaw();
        }
    }

    void OnEnable() { wszystkie.Add(this); if (noc) Ustaw(); }
    void OnDisable() { wszystkie.Remove(this); }

    void Ustaw()
    {
        if (rendery == null) return;
        if (blok == null) blok = new MaterialPropertyBlock();
        foreach (var r in rendery)
        {
            if (!r) continue;
            r.GetPropertyBlock(blok);
            blok.SetColor(Emisja, noc ? swiatlo : Color.black);
            r.SetPropertyBlock(blok);
        }
    }
}
