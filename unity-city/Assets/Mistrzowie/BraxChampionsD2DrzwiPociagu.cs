using UnityEngine;

// D2 train doors (owner 11.11 15:31: "zrób normalnie wagony, żeby stał na razie i środek tak samo ... żeby można było wejść
// do pociągu"). One component per doorway:
//  * the platform doors: two plug leaves - out of the body first, then apart along it - and the sliding step out over the
//    gap to the platform edge (ahead of the leaves);
//  * the glass sliding doors inside (vestibule <-> saloon, the gangways between the coaches): two leaves apart, no step.
// They open when the player is within `promien` of the doorway and close 3 s after he has gone, with the city's door sound.
// Cheap: one distance check every 0.15 s, transforms touched only while the doors move.
[DisallowMultipleComponent]
public sealed class BraxChampionsD2DrzwiPociagu : MonoBehaviour
{
    public Transform[] skrzydla;                 // the leaves (children of this doorway)
    public Vector3[] wysuw, przesuw;             // per leaf (local units): the plug-out offset, then the slide
    public Transform stopien;                    // the sliding step (platform doors only)
    public Vector3 stopienWysuw;
    public float promien = 2.8f, czas = 1.6f;
    public Collider[] blokady;                   // the platform edge's invisible wall at this doorway: off while the doors are open
    public const float Zamykanie = 3f;

    Vector3[] zamk; Vector3 stopZ; float a, cel, pusto, nast; bool ruch;
    static BraxGymPlayer gracz; static float szukaj;

    void Awake()
    {
        zamk = new Vector3[skrzydla.Length];
        for (int i = 0; i < skrzydla.Length; i++) if (skrzydla[i]) zamk[i] = skrzydla[i].localPosition;
        if (stopien) stopZ = stopien.localPosition;
        nast = Time.time + Random.value * .15f;
    }

    static BraxGymPlayer Gracz()
    {
        if (gracz) return gracz;
        if (Time.unscaledTime < szukaj) return null;
        szukaj = Time.unscaledTime + 1f;
        gracz = FindAnyObjectByType<BraxGymPlayer>();
        return gracz;
    }

    public bool Otwarte => cel > .5f;

    void Update()
    {
        if (Time.time >= nast)
        {
            nast = Time.time + .15f;
            var g = Gracz();
            if (g)
            {
                Vector3 v = g.transform.position - transform.position; float dy = Mathf.Abs(v.y); v.y = 0f;
                bool blisko = v.magnitude < promien && dy < 2.5f;
                if (blisko)
                {
                    pusto = 0f;
                    if (cel < .5f) { cel = 1f; ruch = true; BraxDzwiekiSwiata.Drzwi(transform.position, true); }
                }
                else if (cel > .5f)
                {
                    pusto += .15f;
                    if (pusto >= Zamykanie) { cel = 0f; pusto = 0f; ruch = true; BraxDzwiekiSwiata.Drzwi(transform.position, false); }
                }
            }
        }
        if (!ruch) return;
        a = Mathf.MoveTowards(a, cel, Time.deltaTime / Mathf.Max(czas, .1f));
        float o = Mathf.Clamp01(a / .25f), p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((a - .25f) / .75f));
        for (int i = 0; i < skrzydla.Length; i++)
            if (skrzydla[i]) skrzydla[i].localPosition = zamk[i] + (wysuw != null && i < wysuw.Length ? wysuw[i] * o : Vector3.zero) + przesuw[i] * p;
        if (stopien) stopien.localPosition = stopZ + stopienWysuw * Mathf.Clamp01(a / .3f);
        if (blokady != null) { bool zam = a < .3f; foreach (var b in blokady) if (b && b.enabled != zam) b.enabled = zam; }
        if (a == cel) ruch = false;
    }
}
