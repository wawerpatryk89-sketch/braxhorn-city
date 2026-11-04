using System.Collections;
using UnityEngine;

// Mały efekt życia peronu M1: 1–2 istniejące postacie Rocketbox podczas postoju.
// Pasażer porusza się DO faktycznych drzwi wizualnych modelu, po czym znika
// za ciemnym panelem. To INSZENIZACJA, nie pełny symulator AI ani sieć metra.
public sealed class BraxMetroM1Pasazerowie : MonoBehaviour
{
    public BraxMetroM1Ruch pociag;
    public float poziomPeronu = -11.2f;
    public float zPeron = 33.7f;
    public int ile = 2;
    int ostatniKurs = -1;
    bool ostrzezenie;
    BraxPieszy wzor;

    void Update()
    {
        if (!pociag || !pociag.isActiveAndEnabled || !pociag.DrzwiOtwarte ||
            ostatniKurs == pociag.NumerKursu) return;
        ostatniKurs = pociag.NumerKursu;
        var cam = Camera.main;
        if (cam && (cam.transform.position - pociag.transform.position).sqrMagnitude > 85f * 85f) return;
        if (!wzor)
        {
            foreach (var p in FindObjectsByType<BraxPieszy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (p && p.GetComponentInChildren<SkinnedMeshRenderer>(true) && p.anim)
                { wzor = p; break; }
        }
        if (!wzor)
        {
            if (!ostrzezenie) Debug.LogWarning("BRAXHORN_METRO_M1_PASAZEROWIE: brak istniejących postaci Rocketbox/BraxPieszy — nie generujemy atrap");
            ostrzezenie = true;
            return;
        }
        int count = Mathf.Clamp(ile, 1, 2);
        for (int i = 0; i < count; i++)
            StartCoroutine(Odegraj(i, i % 2 == 0));
    }

    GameObject Kopia()
    {
        if (!wzor) return null;
        var o = Instantiate(wzor.gameObject);
        o.name = "Metro M1: pasażer Rocketbox (efekt wejścia/wyjścia)";
        o.transform.SetParent(transform.parent, true);
        foreach (var c in o.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var mb in o.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        foreach (var a in o.GetComponentsInChildren<AudioSource>(true)) if (a) a.enabled = false;
        return o;
    }

    static void Animacja(GameObject o, string nazwa)
    {
        if (!o) return;
        var a = o.GetComponentInChildren<Animation>(true);
        if (!a) return;
        string klip = a[nazwa] != null ? nazwa : (a["Walking"] != null ? "Walking" : "");
        if (klip.Length > 0) { a[klip].wrapMode = WrapMode.Loop; a.CrossFade(klip, .15f); }
    }

    IEnumerator Dojdz(GameObject o, Vector3 cel, float sekund)
    {
        if (!o) yield break;
        var start = o.transform.position;
        var dir = cel - start; dir.y = 0f;
        Animacja(o, "Walking");
        float t = 0;
        while (o && pociag && pociag.DrzwiOtwarte && t < sekund)
        {
            t += Time.deltaTime;
            o.transform.position = Vector3.Lerp(start, cel, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / sekund)));
            if (dir.sqrMagnitude > .01f) o.transform.rotation = Quaternion.Slerp(o.transform.rotation,
                Quaternion.LookRotation(dir), Time.deltaTime * 6f);
            yield return null;
        }
    }

    IEnumerator Odegraj(int nr, bool wsiada)
    {
        yield return new WaitForSeconds(.2f + nr * 2.0f);
        if (!pociag || !pociag.DrzwiOtwarte) yield break;
        var o = Kopia();
        if (!o) yield break;
        int idx = pociag.DrzwiLiczba > 0 ? nr % pociag.DrzwiLiczba : 0;
        Vector3 drzwi = pociag.DrzwiSwiat(idx);
        drzwi.y = poziomPeronu;
        var chodnik = new Vector3(drzwi.x + (nr % 2 == 0 ? 2f : -2f), poziomPeronu, zPeron - .55f);
        var krawedz = new Vector3(drzwi.x, poziomPeronu, zPeron + .1f);
        // Wyjście z wagonu: postać zaczyna przy panelu na zewnątrz;
        // wsiadanie: znika za zasłoniętą wejściem powierzchnią modelu.
        o.transform.position = wsiada ? chodnik : krawedz;
        if (wsiada)
        {
            yield return Dojdz(o, krawedz, 1.8f);
            yield return Dojdz(o, new Vector3(drzwi.x, poziomPeronu, drzwi.z + .30f), 1.1f);
            Debug.Log("BRAXHORN_METRO_M1_NPC_WSIADA: kurs=" + pociag.NumerKursu);
        }
        else
        {
            yield return Dojdz(o, chodnik, 2.4f);
            Debug.Log("BRAXHORN_METRO_M1_NPC_WYSIADA: kurs=" + pociag.NumerKursu);
        }
        if (o) Destroy(o);
    }
}
