using UnityEngine;

// MIASTO MISTRZÓW - KASYNO WAWER, the owner's hidden office (16.11 23:41 "wejście będzie nikomu nieznane ... przez szafę"):
// a wardrobe on the office block's hall wall is its door - a bookcase door: AKCJA "SZAFA" swings the whole wardrobe on its
// hinge into the office, AKCJA again closes it, from the hall or from inside. Its collider turns with it. The label says
// only "SZAFA": nobody sees a door. Without a hinge it is a plain AKCJA spot (the open safe: napis, komunikat).
// 17.11 the villas: the same AKCJA leaf for an up-and-over garage door (os = the hinge's right axis, the leaf hung from its
// top edge swings in and up under the ceiling) and a sliding glass door (przesuw = the hinge's local travel when open).
public sealed class BraxChampionsSzafa : MonoBehaviour, IBraxInterakcja
{
    public Transform zawias;                         // the hinge (the wardrobe its child), turns about its up axis
    public float katOtwarcia = -90f;                 // degrees
    public Vector3 os = Vector3.up;                  // the hinge's local axis it turns about
    public Vector3 przesuw = Vector3.zero;           // non-zero: the leaf slides by this (hinge local) instead of turning
    public Transform[] punkty = new Transform[0];    // where AKCJA works
    public float zasieg = 1.4f;                      // metres (x the transform's scale)
    public string napis = "SZAFA", komunikat = "TAJNE PRZEJŚCIE · BIURO PATRYKA";
    bool otwarta, pierwszy = true; float kat, cel; Quaternion spoczynek; Vector3 start;

    void Awake() { if (zawias) { spoczynek = zawias.localRotation; start = zawias.localPosition; } }
    void OnEnable() { BraxInterakcje.Dodaj(this); }
    void OnDisable() { BraxInterakcje.Usun(this); }

    bool Blisko(Vector3 g)
    {
        float s = Mathf.Max(.01f, transform.lossyScale.y);
        foreach (var p in punkty)
        {
            if (!p) continue; var d = g - p.position;
            if (Mathf.Abs(d.y) > 1.6f * s) continue; d.y = 0f;
            if (d.magnitude < zasieg * s) return true;
        }
        return false;
    }
    public string Kontekst(Vector3 g) => Blisko(g) ? (zawias && otwarta ? napis + " · ZAMKNIJ" : napis) : "";
    public bool Wykonaj(Vector3 g)
    {
        if (!Blisko(g)) return false;
        if (!zawias) { if (!string.IsNullOrEmpty(komunikat)) BraxChampionsGry.Pokaz(komunikat, 3f); return true; }
        otwarta = !otwarta; cel = otwarta ? katOtwarcia : 0f;
        if (otwarta && pierwszy) { pierwszy = false; if (!string.IsNullOrEmpty(komunikat)) BraxChampionsGry.Pokaz(komunikat, 2.8f); }
        return true;
    }
    public bool Otwarta => otwarta;
    /// <summary>Opens or closes it without AKCJA (23.11 the villas' garage doors: the car's remote, BraxChampionsBramaPilot).</summary>
    public void Ustaw(bool otworz) { if (!zawias || otwarta == otworz) return; otwarta = otworz; cel = otwarta ? katOtwarcia : 0f; }
    void Update()
    {
        if (!zawias || Mathf.Approximately(kat, cel)) return;
        kat = Mathf.MoveTowards(kat, cel, 75f * Time.deltaTime);
        if (przesuw.sqrMagnitude > 0f) zawias.localPosition = start + przesuw * (Mathf.Approximately(katOtwarcia, 0f) ? 0f : kat / katOtwarcia);
        else zawias.localRotation = spoczynek * Quaternion.AngleAxis(kat, os);
    }
}
