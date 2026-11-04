using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BRAXHORN CITY — pasażerowie tramwaju T1 (Unity 6).
// Używa wyłącznie istniejących modeli i animacji przechodniów Rocketbox, bez
// postaci z tworzonej od zera bryły. Najwyżej 2 pasażerów, tylko gdy T1 jest blisko kamery.
// Postój: jeden wysiada na chodnik, jeden dochodzi do drzwi i wsiada.
// Oddzielny komponent: brak szkieletów/animacji => jawny log, nie udajemy ludzi.
public sealed class BraxTramPasazerowie : MonoBehaviour
{
    public BraxTramwajT1 tram;
    public int limit = 2;
    readonly List<GameObject> wWagonie = new List<GameObject>();
    int obsluzony = -1;
    bool brakModeluZgloszony;
    int numer;

    void Update()
    {
        if (!tram || !tram.isActiveAndEnabled) return;
        if (!tram.Stoi) { obsluzony = -1; return; }
        if (!tram.MoznaWysiasc || obsluzony == tram.IndeksPrzystanku) return;
        obsluzony = tram.IndeksPrzystanku;

        // 60-90 m wystarczy: dalej nie duplikujemy skór i nie wybudzamy animacji.
        var cam = Camera.main;
        if (cam && (cam.transform.position - tram.transform.position).sqrMagnitude > 95f * 95f)
            return;
        if (!tram.maAnimowaneDrzwi)
        {
            Debug.LogWarning("BRAXHORN_TRAM_PASAZEROWIE: oryginalny CAF bez animowanych skrzydeł. " +
                             "Nie przenoszę ludzi przez zamkniętą ścianę modelu; do odbioru potrzebne ruchome drzwi.");
            return;
        }
        if (wWagonie.Count > 0)
        {
            var o = wWagonie[0]; wWagonie.RemoveAt(0);
            if (o) StartCoroutine(Wysiada(o));
        }
        if (wWagonie.Count < Mathf.Clamp(limit, 1, 3))
        {
            var o = NowaOsoba();
            if (o) StartCoroutine(Wsiada(o));
        }
    }

    GameObject NowaOsoba()
    {
        BraxPieszy p = null;
        foreach (var x in FindObjectsByType<BraxPieszy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!x || !x.anim || !x.GetComponentInChildren<SkinnedMeshRenderer>()) continue;
            p = x; break;
        }
        if (!p)
        {
            if (!brakModeluZgloszony) Debug.LogWarning("BRAXHORN_TRAM_T1_NPC: brak postaci Rocketbox/BraxPieszy w scenie; nie generuję atrap");
            brakModeluZgloszony = true; return null;
        }
        var go = Instantiate(p.gameObject);
        go.name = "T1: pasażer " + (++numer) + " (Rocketbox)";
        go.transform.SetParent(transform, false);
        go.transform.position = tram.WyjscieSwiat + tram.transform.right * .4f;
        // Pasażer T1 nie może dalej wykonywać trasy starego przechodnia ani
        // dublować fizyki postaci na ciasnym peronie.
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb) mb.enabled = false;
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            if (col) Destroy(col);
        return go;
    }

    Animation Anima(GameObject o) => o ? o.GetComponentInChildren<Animation>(true) : null;
    void Chod(GameObject o)
    {
        var anim = Anima(o);
        if (!anim) return;
        string key = anim["Walking"] != null ? "Walking" :
                     anim["Walk"] != null ? "Walk" : "";
        if (key.Length == 0) return;
        anim[key].wrapMode = WrapMode.Loop;
        anim.CrossFade(key, .2f);
    }
    void Stoj(GameObject o)
    {
        var anim = Anima(o);
        if (!anim) return;
        string key = anim["Idle"] != null ? "Idle" : "";
        if (key.Length == 0) return;
        anim[key].wrapMode = WrapMode.Loop;
        anim.CrossFade(key, .2f);
    }

    IEnumerator Krok(GameObject g, Vector3 koniec, float seconds)
    {
        if (!g) yield break;
        Vector3 start = g.transform.position;
        float t = 0f;
        Chod(g);
        while (g && tram && t < seconds)
        {
            t += Time.deltaTime;
            Vector3 dir = koniec - start; dir.y = 0f;
            if (dir.sqrMagnitude > .02f)
                g.transform.rotation = Quaternion.Slerp(g.transform.rotation,
                    Quaternion.LookRotation(dir), Mathf.Clamp01(Time.deltaTime * 6f));
            g.transform.position = Vector3.Lerp(start, koniec, Mathf.SmoothStep(0, 1, t / Mathf.Max(.1f, seconds)));
            yield return null;
        }
        if (g) Stoj(g);
    }
    IEnumerator Wsiada(GameObject o)
    {
        if (!o || !tram) yield break;
        yield return Krok(o, tram.DrzwiSwiat, 1.4f);
        if (!o || !tram || !tram.Stoi) { if (o) Destroy(o); yield break; }
        // Przejście przez drzwi w stronę prawdziwej części pasażerskiej.
        var p = tram.transform.TransformPoint(new Vector3(numer % 2 == 0 ? -2.4f : 2.4f, .30f, 0f));
        yield return Krok(o, p, 1.4f);
        if (!o || !tram) yield break;
        o.transform.SetParent(tram.transform, true);
        wWagonie.Add(o);
        Debug.Log("BRAXHORN_TRAM_T1_NPC_WSIEDLI " + o.name);
    }
    IEnumerator Wysiada(GameObject o)
    {
        if (!o || !tram) yield break;
        o.transform.SetParent(transform, true);
        yield return Krok(o, tram.DrzwiSwiat, 1.2f);
        if (!o || !tram) yield break;
        yield return Krok(o, tram.WyjscieSwiat + Vector3.up * .02f, 1.2f);
        if (o)
        {
            Debug.Log("BRAXHORN_TRAM_T1_NPC_WYSIEDLI " + o.name);
            Destroy(o, 18f); // nie gromadzimy 100 postaci przy 6 stacjach
        }
    }
}
