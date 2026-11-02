using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ZASIĘG DROBNYCH RZECZY NA OSIEDLU (build 14; właściciel 1.11, 20:49: „kwiaty, drobne rzeczy pojawiają się dopiero tuż
// przy postaci”). Przyczyna (policzona): progi LODGroup z buildu są względne do rozmiaru obiektu, a tryb NISKA ma lodBias
// 0.45 — rabata kwiatów 2 m (próg 0.12) znikała 7 m od kamery, krzew 1.6 m (0.035) 20 m, doniczka 0.5 m (0.02) 11 m,
// mimo że shader kwiatów gasi je płynnie dopiero 34–60 m.
// Tu, w scenie rejonu i w każdym doczytanym obszarze: ostatni próg każdej LODGroup na zewnątrz budynków przeliczany tak,
// żeby obiekt znikał dopiero w odległości zależnej od wielkości (drobne 55 m, do 2.5 m — 80 m, do 6 m — 120 m, drzewa
// 220 m; NISKA ×0.75, WYSOKA ×1.5) przy bieżącym lodBias i kącie kamery; kępy z własnym gaśnięciem w shaderze (_FadeEnd)
// — 3 m za końcem gaśnięcia (bez „wyskakiwania” i bez rysowania niewidocznej trawy). Progi tylko maleją (zasięg nigdy
// krótszy niż z buildu). Wnętrza (coś nad obiektem — strop, dach) bez zmian: meble za szybami nie dokładają rysowania.
// Przenikanie: fadeMode CrossFade + animateCrossFading (płynne tam, gdzie shader ma dithering LOD).
public sealed class BraxZasiegDrobnych : MonoBehaviour
{
    sealed class Wpis { public LODGroup g; public float[] prog; public float rozmiar; public Material kepa; public bool wnetrze; }
    readonly List<Wpis> wpisy = new List<Wpis>();
    // materiały kęp (trawa, kwiaty rabat): oryginalne gaśnięcie z buildu; NISKA ×0.75 (mniej rysowania na słabym telefonie)
    readonly Dictionary<Material, Vector2> gasniecie = new Dictionary<Material, Vector2>();
    float bias = -1f, fov = -1f, next;
    int tryb = -1;

    void OnEnable() { SceneManager.sceneLoaded += Wczytano; }
    void OnDisable() { SceneManager.sceneLoaded -= Wczytano; }
    void Start() { Zbierz(); Zastosuj("start"); }
    void Wczytano(Scene s, LoadSceneMode m) { Zbierz(); Zastosuj("obszar " + s.name); }

    void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 1f;
        var cam = Camera.main; float f = cam ? cam.fieldOfView : 55f;
        if (Mathf.Abs(QualitySettings.lodBias - bias) > .001f || (int)BraxGrafika.Aktualny != tryb || Mathf.Abs(f - fov) > .5f) Zastosuj("tryb " + BraxGrafika.Nazwy[(int)BraxGrafika.Aktualny]);
    }

    void Zbierz()
    {
        wpisy.RemoveAll(w => !w.g);
        var znane = new HashSet<LODGroup>();
        foreach (var w in wpisy) znane.Add(w.g);
        Physics.SyncTransforms();
        foreach (var g in FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!g || znane.Contains(g)) continue;
            var lods = g.GetLODs(); if (lods.Length == 0) continue;
            var p = new float[lods.Length];
            for (int i = 0; i < lods.Length; i++) p[i] = lods[i].screenRelativeTransitionHeight;
            var s = g.transform.lossyScale;
            float sk = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            // kępy trawy / kwiatów: gaśnięcie w shaderze (_FadeEnd) wyznacza zasięg
            Material kepa = null;
            foreach (var r in lods[lods.Length - 1].renderers)
                if (r && r.sharedMaterial && r.sharedMaterial.HasProperty("_FadeEnd") && r.sharedMaterial.HasProperty("_FadeStart")) kepa = r.sharedMaterial;
            if (kepa && !gasniecie.ContainsKey(kepa)) gasniecie[kepa] = new Vector2(kepa.GetFloat("_FadeStart"), kepa.GetFloat("_FadeEnd"));
            float fe = kepa ? 1f : 0f;
            // wnętrze: nad środkiem obiektu coś jest (strop, dach, płyta kamery) — zostaje próg z buildu
            Vector3 c = g.transform.TransformPoint(g.localReferencePoint);
            bool pod = fe <= 0f && Physics.Raycast(c + Vector3.up * .05f, Vector3.up, 40f, ~0, QueryTriggerInteraction.Ignore);
            wpisy.Add(new Wpis { g = g, prog = p, rozmiar = g.size * sk, kepa = kepa, wnetrze = pod });
        }
    }

    static float Cel(float rozmiar)
    {
        if (rozmiar < .8f) return 55f;
        if (rozmiar < 2.5f) return 80f;
        if (rozmiar < 6f) return 120f;
        if (rozmiar < 16f) return 220f;
        return 300f;
    }

    void Zastosuj(string powod)
    {
        bias = Mathf.Max(.05f, QualitySettings.lodBias);
        tryb = (int)BraxGrafika.Aktualny;
        var cam = Camera.main; fov = cam ? cam.fieldOfView : 55f;
        float tan2 = 2f * Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
        float k = tryb == 0 ? .75f : tryb == 2 ? 1.5f : 1f;
        foreach (var kv in gasniecie) if (kv.Key) { float kk = tryb == 0 ? .75f : 1f; kv.Key.SetFloat("_FadeStart", kv.Value.x * kk); kv.Key.SetFloat("_FadeEnd", kv.Value.y * kk); }
        int zmienione = 0, wnetrza = 0, kepy = 0;
        foreach (var w in wpisy)
        {
            if (!w.g) continue;
            if (w.wnetrze) { wnetrza++; continue; }
            var lods = w.g.GetLODs(); int last = lods.Length - 1;
            if (last < 0 || w.prog.Length != lods.Length || w.rozmiar < .01f) continue;
            float cel = w.kepa ? w.kepa.GetFloat("_FadeEnd") + 3f : Cel(w.rozmiar) * k;
            if (w.kepa) kepy++;
            // odległość znikania d = rozmiar · lodBias / (2·tg(fov/2) · próg) → próg dla celu
            float potrzebny = w.rozmiar * bias / (tan2 * cel);
            float nowy = Mathf.Clamp(Mathf.Min(w.prog[last], potrzebny), .0005f, 1f);
            if (w.kepa) nowy = Mathf.Clamp(potrzebny, .0005f, 1f);   // kępy: dokładnie za końcem gaśnięcia (w obie strony)
            lods[last].screenRelativeTransitionHeight = nowy;
            for (int i = last - 1; i >= 0; i--) lods[i].screenRelativeTransitionHeight = Mathf.Max(w.prog[i], lods[i + 1].screenRelativeTransitionHeight + .0005f);
            w.g.SetLODs(lods);
            w.g.fadeMode = LODFadeMode.CrossFade;
            w.g.animateCrossFading = true;
            zmienione++;
        }
        Debug.Log("BRAXHORN_ZASIEG " + powod + ": grup LOD " + wpisy.Count + ", przeliczone " + zmienione + " (kępy z gaśnięciem " + kepy + "), wnętrza bez zmian " + wnetrza +
                  ", lodBias " + bias.ToString("0.00") + ", drobne znikają od " + (Cel(.5f) * k).ToString("0") + " m, krzewy " + (Cel(2f) * k).ToString("0") + " m, drzewa " + (Cel(10f) * k).ToString("0") + " m");
    }
}
