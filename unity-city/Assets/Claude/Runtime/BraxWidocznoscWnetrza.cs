using System.Collections.Generic;
using UnityEngine;

// WNĘTRZE RYSOWANE TYLKO WTEDY, GDY MOŻE BYĆ WIDAĆ (wnętrza miasta 2.11). Gra nie ma wypiekanego zasłaniania: stacja metra
// pod ulicą (hala, peron, pociąg, tunel) byłaby rysowana zawsze, gdy kamera na ulicy patrzy w dół — mimo że zasłania ją
// jezdnia. Tu: renderery i lampy z poddrzewa tego obiektu są włączone tylko, gdy kamera albo gracz jest w jednej ze
// `strefy` (wnętrze + okolica wejść). Sprawdzanie co 0.25 s. Renderery zbierane w Start z własnych dzieci (bez odwołań
// zapisanych w scenie — przeniesienie części do innej sceny nic nie psuje). W buildzie wszystko włączone (krok Slim
// usuwa wyłączone renderery), wyłącza dopiero gra.
public sealed class BraxWidocznoscWnetrza : MonoBehaviour
{
    public Bounds[] strefy = new Bounds[0];
    public string nazwa = "wnętrze";

    readonly List<Renderer> rendery = new List<Renderer>();
    readonly List<Light> lampy = new List<Light>();
    int stan = -1; float nast; Camera kam; Transform gracz; float szukaj;

    void Start()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.enabled) rendery.Add(r);
        foreach (var l in GetComponentsInChildren<Light>(true)) if (l.enabled) lampy.Add(l);
        Sprawdz();
    }

    void Update()
    {
        if (Time.unscaledTime < nast) return;
        nast = Time.unscaledTime + .25f;
        Sprawdz();
    }

    void Sprawdz()
    {
        if (!kam || !kam.isActiveAndEnabled) kam = Camera.main;
        if (!gracz && Time.unscaledTime >= szukaj) { szukaj = Time.unscaledTime + 1f; var p = FindAnyObjectByType<BraxGymPlayer>(); if (p) gracz = p.transform; }
        bool w = !kam && !gracz;
        foreach (var b in strefy)
        {
            if (kam && b.Contains(kam.transform.position)) { w = true; break; }
            if (gracz && b.Contains(gracz.position)) { w = true; break; }
        }
        int s = w ? 1 : 0;
        if (s == stan) return;
        stan = s;
        foreach (var r in rendery) if (r) r.enabled = w;
        foreach (var l in lampy) if (l) l.enabled = w;
        Debug.Log("BRAXHORN_WNETRZE " + nazwa + (w ? " rysowane" : " ukryte (kamera daleko)"));
    }
}
