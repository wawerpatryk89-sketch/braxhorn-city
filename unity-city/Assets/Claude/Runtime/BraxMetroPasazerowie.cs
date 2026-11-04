using System.Collections;
using System.Linq;
using UnityEngine;

// METRO M1: lekka "sztuczka" z ludźmi na JEDNYM peronie. Bez NavMesh i całego
// rozkładu jazdy. Klonujemy 2 istniejących przechodniów tylko przy otwartych drzwiach,
// odgrywamy krótkie przejście przez prawdziwy otwór w burcie, potem zwalniamy obiekty.
// Jeśli model Rocketbox nie istnieje — raportujemy brak, nie tworzymy brył-atrap.
public sealed class BraxMetroPasazerowie : MonoBehaviour
{
    public BraxMetroKurs metro;
    public Transform stacja;
    int obsluzonyCykl = -1;
    bool brakZrodla;

    void Update()
    {
        if (!metro || !metro.isActiveAndEnabled || !metro.MoznaWsiadac) return;
        if (metro.NumerCyklu == obsluzonyCykl) return;
        obsluzonyCykl = metro.NumerCyklu;
        StartCoroutine(Obsluga());
    }

    IEnumerator Obsluga()
    {
        if (!metro || !metro.MoznaWsiadac) yield break;
        BraxPieszy zrodlo = null;
        foreach (var p in FindObjectsByType<BraxPieszy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (p && p.GetComponentInChildren<SkinnedMeshRenderer>(true))
            { zrodlo = p; break; }
        if (!zrodlo)
        {
            if (!brakZrodla)
                Debug.LogWarning("BRAXHORN_METRO_M1_PASAZEROWIE_BRAK_RIG: nie znaleziono gotowego przechodnia");
            brakZrodla = true;
            yield break;
        }

        // Pierwszy pasażer wysiada, drugi wsiada — peron jest na -Z od pociągu.
        var d0 = metro.Drzwi(0);
        var d1 = metro.Drzwi(Mathf.Min(1, metro.wejscia.Length - 1));
        var pasazerWys = Klon(zrodlo, "M1 — pasażer wysiadający");
        var pasazerWsi = Klon(zrodlo, "M1 — pasażer wsiadający");
        if (pasazerWys)
        {
            pasazerWys.transform.position = d0 + Vector3.forward * .7f;
            StartCoroutine(Przejdz(pasazerWys, d0 + Vector3.back * 1.55f, 2.25f, false));
        }
        if (pasazerWsi)
        {
            pasazerWsi.transform.position = d1 + Vector3.back * 1.7f;
            yield return Przejdz(pasazerWsi, d1 + Vector3.forward * .72f, 2.5f, true);
        }
        Debug.Log("BRAXHORN_METRO_M1_PASAZEROWIE cykl=" + metro.NumerCyklu +
                  " wysiada=" + (pasazerWys != null) + " wsiada=" + (pasazerWsi != null));
    }

    GameObject Klon(BraxPieszy zrodlo, string nazwa)
    {
        var g = Instantiate(zrodlo.gameObject);
        g.name = nazwa;
        g.transform.SetParent(stacja ? stacja : transform.parent, true);
        // Przechodzień nie może równocześnie wykonywać starego rozkładu chodzenia.
        foreach (var b in g.GetComponentsInChildren<MonoBehaviour>(true))
            if (b) b.enabled = false;
        foreach (var c in g.GetComponentsInChildren<Collider>(true))
            if (c) Destroy(c);
        // Blokada replikowania pieszego jako kolejnego źródła do kolejnych kursów.
        return g;
    }

    static void Animuj(GameObject g, bool chod)
    {
        if (!g) return;
        var anim = g.GetComponentInChildren<Animation>(true);
        if (anim)
        {
            string clip = chod ? (anim["Walking"] != null ? "Walking" :
                                  anim["Walk"] != null ? "Walk" : "") :
                                 (anim["Idle"] != null ? "Idle" : "");
            if (clip.Length > 0)
            {
                anim.enabled = true;
                anim[clip].wrapMode = WrapMode.Loop;
                anim.CrossFade(clip, .15f);
            }
        }
        var a = g.GetComponentInChildren<Animator>(true);
        if (a && a.runtimeAnimatorController)
        {
            a.enabled = true;
            // Nie narzucamy nazw stanów innemu kontrolerowi Rocketbox.
            a.SetFloat("Speed", chod ? 1f : 0f);
        }
    }

    IEnumerator Przejdz(GameObject g, Vector3 cel, float sekundy, bool doWagonu)
    {
        if (!g) yield break;
        var a = g.transform.position;
        Vector3 d = cel - a; d.y = 0f;
        if (d.sqrMagnitude > .01f) g.transform.rotation = Quaternion.LookRotation(d);
        Animuj(g, true);
        float czas = 0f;
        while (g && metro && metro.MoznaWsiadac && czas < sekundy)
        {
            czas += Time.deltaTime;
            float k = Mathf.Clamp01(czas / Mathf.Max(.1f, sekundy));
            g.transform.position = Vector3.Lerp(a, cel, k);
            yield return null;
        }
        if (!g) yield break;
        Animuj(g, false);
        if (doWagonu) Destroy(g);
        else Destroy(g, 8f);
    }
}
